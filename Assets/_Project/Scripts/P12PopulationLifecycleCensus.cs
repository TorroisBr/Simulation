using System;
using System.Collections.Generic;
using System.Threading;

public sealed partial class SimulationRuntime
{
    private const string PopulationImmigrationCensusOperationId = "runtime.population.immigration";
    private const string PopulationEmigrationCensusOperationId = "runtime.population.emigration";
    private const string PopulationResidentDeathCensusOperationId = "runtime.population.resident-death";
    private const string PopulationResidenceMigrationCensusOperationId = "runtime.population.residence-migration";
    private const string PersonDeathCensusOperationId = "runtime.person.death";
    private const string PersonResidenceBindCensusOperationId = "runtime.person.residence-bind";

    private readonly Dictionary<SettlementPopulationRuntime, string> populationAggregateSectionIdsByOwner =
        new Dictionary<SettlementPopulationRuntime, string>();
    private readonly Dictionary<SettlementPopulationRuntime, string> populationReceiptSectionIdsByOwner =
        new Dictionary<SettlementPopulationRuntime, string>();
    private readonly HashSet<PersonRuntime> p12LifecycleBoundPeople = new HashSet<PersonRuntime>();
    private readonly HashSet<NpcRuntime> p12LifecycleBoundNpcs = new HashSet<NpcRuntime>();
    private volatile P12PopulationOperationContext activeP12PopulationOperationContext;

    private sealed class P12PopulationOperationContext
    {
        public readonly string OperationId;
        public readonly HashSet<string> ExpectedSectionIds;
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public readonly SimulationOperationScope OperationScope;
        public readonly ContinuationMutationEpochReservation EpochReservation;

        public P12PopulationOperationContext(
            string operationId,
            HashSet<string> expectedSectionIds,
            Thread ownerThread,
            SimulationOperationScope operationScope,
            ContinuationMutationEpochReservation epochReservation)
        {
            OperationId = operationId;
            ExpectedSectionIds = expectedSectionIds;
            OwnerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
            OwnerManagedThreadId = ownerThread.ManagedThreadId;
            OperationScope = operationScope ?? throw new ArgumentNullException(nameof(operationScope));
            EpochReservation = epochReservation ?? throw new ArgumentNullException(nameof(epochReservation));
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class P12PopulationOperationScope : IDisposable
    {
        private readonly SimulationRuntime owner;
        private readonly P12PopulationOperationContext context;
        private bool completed;
        private bool succeeded;
        private bool disposed;

        public P12PopulationOperationScope(
            SimulationRuntime owner,
            P12PopulationOperationContext context)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Complete(bool operationSucceeded)
        {
            if (disposed || completed)
            {
                owner?.FaultRuntimeAdmission();
                return;
            }
            completed = true;
            succeeded = operationSucceeded;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner?.ExitP12PopulationOperation(context, completed && succeeded);
        }
    }

    private bool IsP12PopulationProfileSupported()
    {
        return runtimeAdmissionContext != null
            && runtimeAdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && configuration?.NaturalMortality?.Enabled == false
            && configuration?.AggregateDemography?.Enabled == false;
    }

    private bool TryRegisterP12PopulationCensusProviders(ContinuationCensusProtocol protocol)
    {
        if (protocol == null || !IsP12PopulationProfileSupported())
            return false;

        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> populationProviders =
                SettlementPopulationCensusProvider.CreateProviders(cities);
            if (populationProviders == null || populationProviders.Count != cities.Count * 2)
                return false;

            HashSet<SettlementPopulationRuntime> aggregateOwners =
                new HashSet<SettlementPopulationRuntime>();
            HashSet<SettlementPopulationRuntime> receiptOwners =
                new HashSet<SettlementPopulationRuntime>();
            foreach (IOwnerSectionCensusProvider provider in populationProviders)
            {
                if (provider == null) return false;
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (witness == null
                    || witness.SchemaVersion != SettlementPopulationCensusProvider.SchemaVersion
                    || witness.Cardinality < 0
                    || !(witness.OwnerInstanceIdentity is SettlementPopulationRuntime owner))
                    return false;

                string expectedId = EncodePopulationSectionId(
                    witness.SectionId.StartsWith(SettlementPopulationCensusProvider.AggregateSectionPrefix, StringComparison.Ordinal)
                        ? SettlementPopulationCensusProvider.AggregateSectionPrefix
                        : SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix,
                    owner.SettlementRuntimeId);
                if (!string.Equals(witness.SectionId, expectedId, StringComparison.Ordinal))
                    return false;

                bool aggregate = witness.SectionId.StartsWith(
                    SettlementPopulationCensusProvider.AggregateSectionPrefix,
                    StringComparison.Ordinal);
                if (aggregate)
                {
                    if (witness.Cardinality != 1 || !aggregateOwners.Add(owner)) return false;
                    populationAggregateSectionIdsByOwner.Add(owner, witness.SectionId);
                }
                else
                {
                    if (!witness.SectionId.StartsWith(
                            SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix,
                            StringComparison.Ordinal)
                        || !receiptOwners.Add(owner)) return false;
                    populationReceiptSectionIdsByOwner.Add(owner, witness.SectionId);
                }

                OwnerSectionContract contract = new OwnerSectionContract(
                    witness.SectionId,
                    SettlementPopulationCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required);
                if (!protocol.RegisterExpectedSection(contract, out _)
                    || !protocol.RegisterCensusProvider(witness.SectionId, provider, out _))
                    return false;
            }

            if (aggregateOwners.Count != cities.Count
                || receiptOwners.Count != cities.Count
                || aggregateOwners.Count != receiptOwners.Count)
                return false;

            List<IOwnerSectionCensusProvider> lifecycleProviders =
                new List<IOwnerSectionCensusProvider>();
            lifecycleProviders.AddRange(
                PersonLifeResidenceCensusProvider.CreateProviders(personStore.Persons));
            lifecycleProviders.AddRange(
            NpcLifecycleCensusProvider.CreateProviders(npcRuntimeSnapshot));
            lifecycleProviders.AddRange(
                P12CrimeJusticeCensusProvider.CreateNpcStatusProviders(npcRuntimeSnapshot));
            lifecycleProviders.AddRange(
                NpcCurrentActionCensusProvider.CreateProviders(npcRuntimeSnapshot, ValidateP12CurrentActionReferences));
            if (!TryRegisterP12CrimeJusticeOwnerSections(protocol))
                return false;
            return protocol.RegisterLifecycleOwnerRosterFamily(lifecycleProviders, out _);
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private static string EncodePopulationSectionId(string prefix, string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            throw new ArgumentException("A population census section requires its City RuntimeId.", nameof(runtimeId));
        return prefix + runtimeId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":" + runtimeId;
    }

    private bool TryRegisterP12PopulationLifecycleOperations(ContinuationCensusProtocol protocol)
    {
        return protocol != null
            && protocol.RegisterExpectedOperation(PopulationImmigrationCensusOperationId, out _)
            && protocol.RegisterExpectedOperation(PopulationEmigrationCensusOperationId, out _)
            && protocol.RegisterExpectedOperation(PopulationResidentDeathCensusOperationId, out _)
            && protocol.RegisterExpectedOperation(PopulationResidenceMigrationCensusOperationId, out _)
            && protocol.RegisterExpectedOperation(PersonDeathCensusOperationId, out _)
            && protocol.RegisterExpectedOperation(PersonResidenceBindCensusOperationId, out _);
    }

    private bool TryBindP12LifecycleMutationBoundaries()
    {
        if (runtimeAdmissionContext == null) return true;
        foreach (PersonRuntime person in personStore.Persons)
        {
            if (person == null) return false;
            if (person.IsP12LifecycleBound)
            {
                if (!p12LifecycleBoundPeople.Contains(person)) return false;
                continue;
            }
            person.BindP12LifecycleMutationBoundary(
                CanCommitP12PersonLifecycleMutation,
                NotifyP12PersonLifecycleMutation);
            p12LifecycleBoundPeople.Add(person);
        }

        foreach (NpcRuntime npc in npcRuntimeSnapshot)
        {
            if (npc == null) return false;
            if (npc.IsP12LifecycleBound)
            {
                if (!p12LifecycleBoundNpcs.Contains(npc)) return false;
                continue;
            }
            npc.BindP12LifecycleMutationBoundary(
                CanCommitP12NpcLifecycleMutation,
                NotifyP12NpcLifecycleMutation);
            p12LifecycleBoundNpcs.Add(npc);
        }

        foreach (CityRuntime city in cities)
        {
            SettlementPopulationRuntime population = city?.Population;
            if (population == null
                || !populationAggregateSectionIdsByOwner.ContainsKey(population)
                || !populationReceiptSectionIdsByOwner.ContainsKey(population))
                return false;
            if (population.IsP12PopulationBound) continue;
            population.BindP12PopulationMutationBoundary(
                CanCommitP12PopulationMutation,
                NotifyP12PopulationMutation);
        }
        return TryBindP12CrimeJusticeMutationBoundaries()
            && TryBindP12CrimeSocialAppraisalMutationBoundaries();
    }

    private IReadOnlyList<IOwnerSectionCensusProvider> CreateCurrentP12LifecycleProviders()
    {
        if (runtimeAdmissionContext == null)
            return Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
        List<IOwnerSectionCensusProvider> providers = new List<IOwnerSectionCensusProvider>();
        providers.AddRange(PersonLifeResidenceCensusProvider.CreateProviders(personStore.Persons));
        providers.AddRange(NpcLifecycleCensusProvider.CreateProviders(npcRuntimeSnapshot));
        providers.AddRange(P12CrimeJusticeCensusProvider.CreateNpcStatusProviders(npcRuntimeSnapshot));
        providers.AddRange(
            NpcCurrentActionCensusProvider.CreateProviders(npcRuntimeSnapshot, ValidateP12CurrentActionReferences));
        return providers.AsReadOnly();
    }

    private bool TryAssessP12LifecycleOwnerRoster()
    {
        if (runtimeAdmissionContext == null) return true;
        return npcRosterCensusProtocol != null
            && npcRosterCensusProtocol.TryAssessLifecycleOwnerRoster(
                CreateCurrentP12LifecycleProviders(),
                out _);
    }

    private bool TryBeginP12PopulationOperation(
        string operationId,
        IEnumerable<string> sectionIds,
        Func<bool> localRevisionCapacity,
        out P12PopulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;
        if (!IsP12PopulationProfileSupported())
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (!TryAssessP12LifecycleOwnerRoster())
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (activeP12PopulationOperationContext != null
            || activeNpcMembershipCensusContext != null
            || localRevisionCapacity == null
            || !localRevisionCapacity())
            return false;

        HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            if (sectionIds == null)
                return false;
            foreach (string sectionId in sectionIds)
            {
                if (string.IsNullOrWhiteSpace(sectionId) || !expected.Add(sectionId))
                    return false;
            }
        }
        catch
        {
            return false;
        }
        if (expected.Count == 0 || !IsRuntimeAdmissionOwnerThreadCurrent())
            return false;

        if (!npcRosterCensusProtocol.TryEnterOperation(
                operationId,
                out SimulationOperationScope operationScope,
                out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        if (!npcRosterCensusProtocol.TryValidateUnchangedSections(expected, out _)
            || !npcRosterCensusProtocol.TryReserveMutationEpochCapacity(
                out ContinuationMutationEpochReservation reservation,
                out _))
        {
            FaultRuntimeAdmission();
            operationScope.Dispose();
            return false;
        }

        P12PopulationOperationContext context = new P12PopulationOperationContext(
            operationId,
            expected,
            Thread.CurrentThread,
            operationScope,
            reservation);
        activeP12PopulationOperationContext = context;
        scope = new P12PopulationOperationScope(this, context);
        return true;
    }

    private void ExitP12PopulationOperation(
        P12PopulationOperationContext context,
        bool operationSucceeded)
    {
        if (context == null) return;
        if (!context.IsOwnedByCurrentThread()
            || !ReferenceEquals(activeP12PopulationOperationContext, context))
        {
            FaultRuntimeAdmission();
            return;
        }

        foreach (string changed in context.ChangedSectionIds)
        {
            if (!context.ExpectedSectionIds.Contains(changed))
            {
                FaultRuntimeAdmission();
                break;
            }
        }

        if (operationSucceeded && context.ChangedSectionIds.Count > 0)
        {
            if (!npcRosterCensusProtocol.TryNotifyReservedCommittedMutations(
                    context.EpochReservation,
                    context.ChangedSectionIds,
                    out _))
                FaultRuntimeAdmission();
        }
        else
        {
            if (!operationSucceeded && context.ChangedSectionIds.Count > 0)
                FaultRuntimeAdmission();
            npcRosterCensusProtocol.ReleaseMutationEpochReservation(context.EpochReservation);
        }

        context.OperationScope.Dispose();
        activeP12PopulationOperationContext = null;
    }

    private bool CanCommitP12PersonLifecycleMutation(PersonRuntime person)
    {
        if (runtimeAdmissionContext == null) return true;
        if (person == null || !IsRuntimeAdmissionOwnerThreadCurrent()) return false;
        string sectionId = PersonLifeResidenceCensusProvider.SectionIdFor(person.PersonId);
        P12PopulationOperationContext populationContext = activeP12PopulationOperationContext;
        if (populationContext != null && populationContext.IsOwnedByCurrentThread())
            return populationContext.ExpectedSectionIds.Contains(sectionId);

        NpcMembershipCensusContext membershipContext = activeNpcMembershipCensusContext;
        return membershipContext != null
            && membershipContext.IsOwnedByCurrentThread()
            && membershipContext.AllowedLifecycleSectionIds.Contains(sectionId);
    }

    private void NotifyP12PersonLifecycleMutation(PersonRuntime person)
    {
        if (runtimeAdmissionContext == null) return;
        string sectionId = PersonLifeResidenceCensusProvider.SectionIdFor(person.PersonId);
        if (!TryRecordP12LifecycleMutation(sectionId))
            FaultRuntimeAdmission();
    }

    private bool CanCommitP12NpcLifecycleMutation(
        NpcRuntime npc,
        bool lifeStateChanged,
        bool residenceChanged,
        bool currentActionChanged,
        NpcActionRuntime nextActionRuntime)
    {
        if (runtimeAdmissionContext == null) return true;
        if (npc == null
            || !IsRuntimeAdmissionOwnerThreadCurrent()
            || string.IsNullOrWhiteSpace(npc.RuntimeId)
            || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime registeredNpc)
            || !ReferenceEquals(registeredNpc, npc))
            return false;

        string actionSectionId = NpcCurrentActionCensusProvider.SectionIdFor(npc.RuntimeId);
        if (nextActionRuntime != null
            && (lifeStateChanged || residenceChanged || !ValidateP12CurrentActionReferences(npc, nextActionRuntime)))
            return false;

        P12PopulationOperationContext populationContext = activeP12PopulationOperationContext;
        NpcMembershipCensusContext membershipContext = activeNpcMembershipCensusContext;
        HashSet<string> allowedSectionIds = populationContext != null && populationContext.IsOwnedByCurrentThread()
            ? populationContext.ExpectedSectionIds
            : membershipContext != null && membershipContext.IsOwnedByCurrentThread()
                ? membershipContext.AllowedLifecycleSectionIds
                : null;
        if (lifeStateChanged || residenceChanged)
        {
            if (allowedSectionIds == null
                || (lifeStateChanged
                    && !allowedSectionIds.Contains(NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, false))))
                return false;
            if (residenceChanged && npc.BoundPersonRuntime == null
                && !allowedSectionIds.Contains(NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, true)))
                return false;
        }
        if (currentActionChanged && allowedSectionIds != null)
        {
            if (!allowedSectionIds.Contains(actionSectionId)) return false;
        }
        else if (currentActionChanged)
        {
            if (lifeStateChanged || residenceChanged
                || populationContext != null
                || membershipContext != null
                || !npcRosterCensusProtocol.TryReadActiveOperationCount(
                    out int activeOperationCount,
                    out _)
                || activeOperationCount <= 0
                || !CanCommitP12MutationSections(new[] { actionSectionId }))
                return false;
        }
        return true;
    }

    private void NotifyP12NpcLifecycleMutation(
        NpcRuntime npc,
        bool lifeStateChanged,
        bool residenceChanged,
        bool currentActionChanged)
    {
        if (runtimeAdmissionContext == null) return;
        if (lifeStateChanged
            && !TryRecordP12LifecycleMutation(
                NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, false)))
            FaultRuntimeAdmission();
        if (residenceChanged && npc.BoundPersonRuntime == null
            && !TryRecordP12LifecycleMutation(
                NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, true)))
            FaultRuntimeAdmission();
        if (currentActionChanged)
        {
            if (activeP12PopulationOperationContext != null
                || activeNpcMembershipCensusContext != null)
            {
                if (!TryRecordP12LifecycleMutation(
                        NpcCurrentActionCensusProvider.SectionIdFor(npc.RuntimeId)))
                    FaultRuntimeAdmission();
            }
            else if (!NotifyP12MutationSections(new[]
                    { NpcCurrentActionCensusProvider.SectionIdFor(npc.RuntimeId) }))
            {
                FaultRuntimeAdmission();
            }
        }
    }

    private bool CanCommitP12PopulationMutation(
        SettlementPopulationRuntime owner,
        bool aggregateChanged,
        bool receiptChanged)
    {
        if (runtimeAdmissionContext == null) return true;
        if (owner == null || !IsRuntimeAdmissionOwnerThreadCurrent()) return false;
        P12PopulationOperationContext context = activeP12PopulationOperationContext;
        if (context == null || !context.IsOwnedByCurrentThread()) return false;
        return (!aggregateChanged
                || (populationAggregateSectionIdsByOwner.TryGetValue(owner, out string aggregateId)
                    && context.ExpectedSectionIds.Contains(aggregateId)))
            && (!receiptChanged
                || (populationReceiptSectionIdsByOwner.TryGetValue(owner, out string receiptId)
                    && context.ExpectedSectionIds.Contains(receiptId)));
    }

    private void NotifyP12PopulationMutation(
        SettlementPopulationRuntime owner,
        bool aggregateChanged,
        bool receiptChanged)
    {
        if (runtimeAdmissionContext == null) return;
        P12PopulationOperationContext context = activeP12PopulationOperationContext;
        if (context == null || !context.IsOwnedByCurrentThread())
        {
            FaultRuntimeAdmission();
            return;
        }
        if (aggregateChanged)
        {
            if (!populationAggregateSectionIdsByOwner.TryGetValue(owner, out string aggregateId))
                FaultRuntimeAdmission();
            else
                context.ChangedSectionIds.Add(aggregateId);
        }
        if (receiptChanged)
        {
            if (!populationReceiptSectionIdsByOwner.TryGetValue(owner, out string receiptId))
                FaultRuntimeAdmission();
            else
                context.ChangedSectionIds.Add(receiptId);
        }
    }

    private bool TryRecordP12LifecycleMutation(string sectionId)
    {
        P12PopulationOperationContext populationContext = activeP12PopulationOperationContext;
        if (populationContext != null && populationContext.IsOwnedByCurrentThread())
        {
            if (!populationContext.ExpectedSectionIds.Contains(sectionId)) return false;
            populationContext.ChangedSectionIds.Add(sectionId);
            return true;
        }

        NpcMembershipCensusContext membershipContext = activeNpcMembershipCensusContext;
        if (membershipContext != null && membershipContext.IsOwnedByCurrentThread())
        {
            if (!membershipContext.AllowedLifecycleSectionIds.Contains(sectionId)) return false;
            membershipContext.ChangedLifecycleSectionIds.Add(sectionId);
            return true;
        }
        return false;
    }

    private bool TryBuildPersonLifecycleSectionIds(
        PersonRuntime person,
        bool resident,
        NpcRuntime materializedNpc,
        CityRuntime settlement,
        out List<string> sectionIds)
    {
        sectionIds = new List<string>();
        if (person == null) return false;
        sectionIds.Add(PersonLifeResidenceCensusProvider.SectionIdFor(person.PersonId));
        if (materializedNpc != null)
        {
            sectionIds.Add(NpcLifecycleCensusProvider.SectionIdFor(materializedNpc.RuntimeId, false));
            if (materializedNpc.CurrentActionRuntime != null)
                sectionIds.Add(NpcCurrentActionCensusProvider.SectionIdFor(materializedNpc.RuntimeId));
        }
        if (resident)
        {
            if (settlement?.Population == null
                || !populationAggregateSectionIdsByOwner.TryGetValue(
                    settlement.Population,
                    out string populationId))
                return false;
            sectionIds.Add(populationId);
        }
        return true;
    }

    private bool TryBuildP12NpcPopulationOperation(
        NpcRuntime npc,
        CityRuntime settlement,
        bool residentDeath,
        out List<string> sectionIds,
        out Func<bool> localRevisionCapacity)
    {
        sectionIds = new List<string>();
        localRevisionCapacity = null;
        if (runtimeAdmissionContext == null
            || npc == null
            || settlement == null
            || !npcRegistryById.TryGetValue(npc.RuntimeId ?? string.Empty, out NpcRuntime registeredNpc)
            || !ReferenceEquals(registeredNpc, npc)
            || !cities.Exists(candidate => ReferenceEquals(candidate, settlement))
            || settlement.Population == null
            || !populationAggregateSectionIdsByOwner.TryGetValue(
                settlement.Population,
                out string populationSectionId))
            return false;

        sectionIds.Add(populationSectionId);
        PersonRuntime person = npc.BoundPersonRuntime;
        if (person != null)
        {
            if (person.PersonId == null
                || !personStore.TryGet(person.PersonId, out PersonRuntime registeredPerson)
                || !ReferenceEquals(registeredPerson, person))
                return false;
            sectionIds.Add(PersonLifeResidenceCensusProvider.SectionIdFor(person.PersonId));
            if (residentDeath)
                sectionIds.Add(NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, false));
            bool actionChanged = npc.CurrentActionRuntime != null;
            if (actionChanged)
                sectionIds.Add(NpcCurrentActionCensusProvider.SectionIdFor(npc.RuntimeId));
            localRevisionCapacity = () => settlement.Population.CanAdvanceP12AggregateRevision
                && person.CanAdvanceP12LifeResidenceRevision(residentDeath ? 2 : 1)
                && (!residentDeath || npc.CanAdvanceP12LifecycleRevisions(
                    1, 0, actionChanged ? 1 : 0));
            return true;
        }

        sectionIds.Add(NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, true));
        if (residentDeath)
            sectionIds.Add(NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, false));
        bool npcActionChanged = npc.CurrentActionRuntime != null;
        if (npcActionChanged)
            sectionIds.Add(NpcCurrentActionCensusProvider.SectionIdFor(npc.RuntimeId));
        localRevisionCapacity = () => settlement.Population.CanAdvanceP12AggregateRevision
            && npc.CanAdvanceP12LifecycleRevisions(
                residentDeath ? 1 : 0, 1, npcActionChanged ? 1 : 0);
        return true;
    }

    private bool TryBuildP12ResidenceMigrationOperation(
        NpcRuntime npc,
        CityRuntime origin,
        CityRuntime destination,
        out List<string> sectionIds,
        out Func<bool> localRevisionCapacity)
    {
        sectionIds = new List<string>();
        localRevisionCapacity = null;
        if (runtimeAdmissionContext == null
            || npc == null
            || origin == null
            || destination == null
            || !npcRegistryById.TryGetValue(npc.RuntimeId ?? string.Empty, out NpcRuntime registeredNpc)
            || !ReferenceEquals(registeredNpc, npc)
            || !cities.Exists(candidate => ReferenceEquals(candidate, origin))
            || !cities.Exists(candidate => ReferenceEquals(candidate, destination))
            || origin.Population == null
            || destination.Population == null
            || !populationAggregateSectionIdsByOwner.TryGetValue(origin.Population, out string originSectionId)
            || !populationAggregateSectionIdsByOwner.TryGetValue(destination.Population, out string destinationSectionId))
            return false;

        sectionIds.Add(originSectionId);
        sectionIds.Add(destinationSectionId);
        PersonRuntime person = npc.BoundPersonRuntime;
        if (person != null)
        {
            if (person.PersonId == null
                || !personStore.TryGet(person.PersonId, out PersonRuntime registeredPerson)
                || !ReferenceEquals(registeredPerson, person))
                return false;
            sectionIds.Add(PersonLifeResidenceCensusProvider.SectionIdFor(person.PersonId));
            localRevisionCapacity = () => origin.Population.CanAdvanceP12AggregateRevision
                && destination.Population.CanAdvanceP12AggregateRevision
                && person.CanAdvanceP12LifeResidenceRevision(1);
            return true;
        }

        sectionIds.Add(NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, true));
        localRevisionCapacity = () => origin.Population.CanAdvanceP12AggregateRevision
            && destination.Population.CanAdvanceP12AggregateRevision
            && npc.CanAdvanceP12LifecycleRevisions(0, 1);
        return true;
    }

    private bool TryResolveCityByRuntimeId(string runtimeId, out CityRuntime city)
    {
        city = null;
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (CityRuntime candidate in cities)
        {
            if (candidate != null
                && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                city = candidate;
                return true;
            }
        }
        return false;
    }

    private bool IsP12NamedBirthUnsupported => runtimeAdmissionContext != null;

    internal bool IsP12PopulationRuntime => runtimeAdmissionContext != null;
}
