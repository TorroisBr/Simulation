using System;
using System.Collections.Generic;
using System.Threading;

public sealed partial class SimulationRuntime
{
    private readonly Dictionary<SettlementPopulationRuntime, string> populationAggregateSectionIdsByOwner =
        new Dictionary<SettlementPopulationRuntime, string>();
    private readonly Dictionary<SettlementPopulationRuntime, string> populationReceiptSectionIdsByOwner =
        new Dictionary<SettlementPopulationRuntime, string>();
    private readonly HashSet<PersonRuntime> p12LifecycleBoundPeople = new HashSet<PersonRuntime>();
    private readonly HashSet<NpcRuntime> p12LifecycleBoundNpcs = new HashSet<NpcRuntime>();
    private volatile P12PopulationOperationContext activeP12PopulationOperationContext;
    private volatile CoreSelectedPersonDeathProjectionContext activeCoreSelectedPersonDeathProjectionContext;
    private string lastCoreSelectedPersonDeathFailure;
    private string lastCoreSelectedPersonDeathProjectionFailure;
    internal string LastCoreSelectedPersonDeathFailure => lastCoreSelectedPersonDeathFailure;
    internal string LastCoreSelectedPersonDeathProjectionFailure =>
        lastCoreSelectedPersonDeathProjectionFailure;

    private sealed class CoreSelectedPersonDeathProjectionContext
    {
        public readonly DailyV1SelectedPersonDeathPlan Plan;
        public readonly string OperationId;
        public readonly bool CurrentActionChanged;
        public readonly DailyV1MutationEpochReservation EpochReservation;
        public readonly PersonRuntime Person;
        public readonly NpcRuntime MaterializedNpc;
        public readonly CityRuntime ResidenceCity;
        public readonly SettlementPopulationRuntime Population;
        public readonly HashSet<string> ExpectedSectionIds;
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly NpcRuntime.CoreSelectedPersonDeathProjectionSnapshot NpcProjectionSnapshot;
        public readonly ContinuationCensusProtocol.CompatibilityMirrorBaselineSnapshot MirrorBaselineSnapshot;
        public readonly long PoliticalWorldRevisionBefore;
        public readonly long PoliticalTruthFingerprintBefore;
        public readonly bool HasPoliticalTruthFingerprintBefore;
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public bool CompatibilityBaselinesPrepared;
        public bool PermissionRevoked;

        public CoreSelectedPersonDeathProjectionContext(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            DailyV1MutationEpochReservation epochReservation,
            PersonRuntime person,
            NpcRuntime materializedNpc,
            CityRuntime residenceCity,
            SettlementPopulationRuntime population,
            IEnumerable<string> sectionIds,
            NpcRuntime.CoreSelectedPersonDeathProjectionSnapshot npcProjectionSnapshot,
            ContinuationCensusProtocol.CompatibilityMirrorBaselineSnapshot mirrorBaselineSnapshot,
            long politicalWorldRevisionBefore,
            long politicalTruthFingerprintBefore,
            bool hasPoliticalTruthFingerprintBefore)
        {
            Plan = plan;
            OperationId = operationId;
            CurrentActionChanged = currentActionChanged;
            EpochReservation = epochReservation;
            Person = person;
            MaterializedNpc = materializedNpc;
            ResidenceCity = residenceCity;
            Population = population;
            ExpectedSectionIds = new HashSet<string>(sectionIds, StringComparer.Ordinal);
            NpcProjectionSnapshot = npcProjectionSnapshot;
            MirrorBaselineSnapshot = mirrorBaselineSnapshot;
            PoliticalWorldRevisionBefore = politicalWorldRevisionBefore;
            PoliticalTruthFingerprintBefore = politicalTruthFingerprintBefore;
            HasPoliticalTruthFingerprintBefore = hasPoliticalTruthFingerprintBefore;
            OwnerThread = Thread.CurrentThread;
            OwnerManagedThreadId = OwnerThread.ManagedThreadId;
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

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

    private sealed class P12PopulationOperationScope : IDailyV1SelectedPersonDeathP12Scope
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

        public bool Complete(bool operationSucceeded)
        {
            if (disposed || completed)
            {
                owner?.FaultRuntimeAdmission();
                return false;
            }
            completed = true;
            succeeded = operationSucceeded;
            return true;
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

    private bool TryBindP12LifecycleMutationBoundaries()
    {
        if (runtimeAdmissionContext == null) return true;
        foreach (PersonRuntime person in personStore.Persons)
        {
            if (person == null) return false;
            if (PersonRuntimeOwnerBridge.IsP12LifecycleBound(person))
            {
                if (!p12LifecycleBoundPeople.Contains(person)) return false;
                continue;
            }
            PersonRuntimeOwnerBridge.BindP12LifecycleMutationBoundary(
                person,
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
            if (PopulationOwnerBridge.IsP12PopulationBound(population)) continue;
            PopulationOwnerBridge.BindP12PopulationMutationBoundary(
                population,
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
            || activeCoreSelectedPersonDeathProjectionContext != null
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

    /// <summary>
    /// Runs the Core-owned Person-death transaction with this runtime's live
    /// P12 census scope and Unity projection bindings.
    /// </summary>
    internal bool TryExecuteCoreSelectedPersonDeath(
        DailyV1SelectedPersonDeathPlan plan,
        out string failure)
    {
        return TryExecuteCoreSelectedPersonDeath(
            plan,
            DailyV1RuntimeProtocolOperationContracts.PersonDeath,
            out failure);
    }

    internal bool TryExecuteCoreSelectedPersonDeath(
        DailyV1SelectedPersonDeathPlan plan,
        string operationContractId,
        out string failure)
    {
        bool applied = DailyV1SelectedPersonDeathOperation.TryExecute(
            SelectedRuntimeOwnerGraph,
            personStore,
            plan,
            operationContractId,
            CreateSelectedPersonDeathBoundary(plan),
            out failure);
        lastCoreSelectedPersonDeathFailure = applied ? null : failure;
        if (!applied && !string.IsNullOrWhiteSpace(lastCoreSelectedPersonDeathProjectionFailure))
            lastCoreSelectedPersonDeathFailure += " (" + lastCoreSelectedPersonDeathProjectionFailure + ")";
        return applied;
    }

    internal IDailyV1SelectedPersonDeathBoundary CreateSelectedPersonDeathBoundary() =>
        new SelectedPersonDeathP12Boundary(this);

    private IDailyV1SelectedPersonDeathBoundary CreateSelectedPersonDeathBoundary(
        DailyV1SelectedPersonDeathPlan plan)
    {
        if (!IsCoreOwnedSelectedPersonDeathPlan(plan))
            return CreateSelectedPersonDeathBoundary();

        return new DailyV1CoreSelectedPersonDeathBoundary(
            dailyExecutionBoundary,
            SelectedRuntimeOwnerGraph,
            new SelectedPersonDeathProjectionBoundary(this));
    }

    private bool IsCoreOwnedSelectedPersonDeathPlan(
        DailyV1SelectedPersonDeathPlan plan)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (plan?.Person?.PersonId == null
            || dailyExecutionBoundary?.HasAdmittedSelectedDailyExecution != true
            || !IsP12PopulationProfileSupported()
            || !ReferenceEquals(graph, dailyExecutionBoundary.SelectedRuntimeOwnerGraph)
            || !ReferenceEquals(graph?.PersonStore, personStore)
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person))
            return false;

        if (plan.MaterializedNpc != null)
        {
            if (!graph.ContainsExactNpcOwner(plan.MaterializedNpc)
                || !npcRegistryById.TryGetValue(
                    plan.MaterializedNpc.RuntimeId, out NpcRuntime selectedNpc)
                || selectedNpc == null
                || !ReferenceEquals(selectedNpc.SelectedRuntimeOwners, plan.MaterializedNpc)
                || !ReferenceEquals(selectedNpc.BoundPersonRuntime, plan.Person))
                return false;
        }
        else
        {
            // A Unity NPC outside the Core owner graph can still project this
            // Person. Keep that compatibility authority on the existing Unity
            // census boundary instead of committing only the Core Person row.
            foreach (NpcRuntime npc in npcRuntimeSnapshot)
                if (npc != null && ReferenceEquals(npc.BoundPersonRuntime, plan.Person))
                    return false;
        }

        if (plan.IsResident
            && (!graph.ContainsExactCityOwner(plan.ResidenceCity)
                || !TryResolveCityByRuntimeId(
                    plan.ResidenceCity.RuntimeId, out CityRuntime selectedCity)
                || selectedCity == null
                || !ReferenceEquals(selectedCity.SelectedRuntimeOwners, plan.ResidenceCity)
                || !ReferenceEquals(selectedCity.Population, plan.ResidenceCity.Population)))
            return false;

        return true;
    }

    private bool TryValidateCoreSelectedPersonDeathProjection(
        DailyV1SelectedPersonDeathPlan plan,
        out bool actionChanged,
        out string failure)
    {
        actionChanged = false;
        failure = "RuntimeFaulted";
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (plan == null || graph == null
            || !ReferenceEquals(graph.PersonStore, personStore)
            || !ReferenceEquals(SelectedRuntimeOwnerGraph, graph)
            || CurrentDay != plan.AbsoluteDay
            || plan.Person == null || plan.Person.PersonId == null
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person))
            return false;

        if (plan.MaterializedNpc != null)
        {
            if (!graph.ContainsExactNpcOwner(plan.MaterializedNpc)
                || !npcRegistryById.TryGetValue(
                    plan.MaterializedNpc.RuntimeId, out NpcRuntime materializedNpc)
                || materializedNpc == null
                || !ReferenceEquals(materializedNpc.SelectedRuntimeOwners, plan.MaterializedNpc)
                || !ReferenceEquals(materializedNpc.BoundPersonRuntime, plan.Person)
                || !materializedNpc.HasConsistentCurrentActionSlot)
                return false;
            actionChanged = materializedNpc.CurrentActionRuntime != null;
            if (!materializedNpc.CanPublishCoreSelectedPersonDeath(
                    plan.MaterializedNpc, actionChanged)
                || !materializedNpc.CanAdvanceP12LifecycleRevisions(
                    1, 0, actionChanged ? 1 : 0)
                || !materializedNpc.CanCommitSelectedPersonDeathAfterValidation(
                    NpcInjurySeverity.None, out bool projectionActionChanged)
                || projectionActionChanged != actionChanged)
                return false;
        }

        if (plan.IsResident
            && (!graph.ContainsExactCityOwner(plan.ResidenceCity)
                || !TryResolveCityByRuntimeId(
                    plan.ResidenceCity.RuntimeId, out CityRuntime settlement)
                || settlement == null
                || !ReferenceEquals(settlement.SelectedRuntimeOwners, plan.ResidenceCity)
                || !ReferenceEquals(settlement.Population, plan.ResidenceCity.Population)))
            return false;

        failure = null;
        return true;
    }

    private bool TryBeginCoreSelectedPersonDeathProjectionMutation(
        DailyV1SelectedPersonDeathPlan plan,
        string operationId,
        bool currentActionChanged,
        IReadOnlyList<string> sectionIds)
    {
        if (runtimeAdmissionContext == null)
            return activeCoreSelectedPersonDeathProjectionContext == null;

        DailyV1MutationEpochReservation epochReservation =
            dailyExecutionBoundary?.MutationEpochState.ActiveReservation;

        if (!IsP12PopulationProfileSupported()
            || !IsRuntimeAdmissionOwnerThreadCurrent()
            || (!string.Equals(operationId,
                    DailyV1RuntimeProtocolOperationContracts.PersonDeath, StringComparison.Ordinal)
                && !string.Equals(operationId,
                    DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath, StringComparison.Ordinal))
            || plan?.Person?.PersonId == null
            || epochReservation == null
            || currentActionChanged != (plan.ExpectedAction != null)
            || sectionIds == null
            || activeCoreSelectedPersonDeathProjectionContext != null
            || activeP12PopulationOperationContext != null
            || activeNpcMembershipCensusContext != null
            || dailyExecutionBoundary?.HasAdmittedSelectedDailyExecution != true
            || dailyExecutionBoundary.ActiveRuntimeOperationCount <= 0
            || !DailyV1SelectedPersonDeathOperation.TryCreateP12SectionIds(
                plan, out IReadOnlyList<string> expectedSectionIds)
            || !HasExactCorePersonDeathSectionIds(expectedSectionIds, sectionIds)
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person)
            || !p12LifecycleBoundPeople.Contains(plan.Person)
            || !PersonRuntimeOwnerBridge.IsP12LifecycleBound(plan.Person))
            return false;

        NpcRuntime materializedNpc = null;
        if (plan.MaterializedNpc != null)
        {
            if (!npcRegistryById.TryGetValue(plan.MaterializedNpc.RuntimeId, out materializedNpc)
                || materializedNpc == null
                || !ReferenceEquals(materializedNpc.SelectedRuntimeOwners, plan.MaterializedNpc)
                || !ReferenceEquals(materializedNpc.BoundPersonRuntime, plan.Person)
                || !p12LifecycleBoundNpcs.Contains(materializedNpc)
                || !materializedNpc.IsP12LifecycleBound
                || !ContainsCorePersonDeathSectionId(sectionIds,
                    NpcLifecycleCensusProvider.SectionIdFor(materializedNpc.RuntimeId, false))
                || currentActionChanged != ContainsCorePersonDeathSectionId(
                    sectionIds,
                    NpcCurrentActionCensusProvider.SectionIdFor(materializedNpc.RuntimeId)))
                return false;
        }

        CityRuntime residenceCity = null;
        SettlementPopulationRuntime population = null;
        if (plan.IsResident)
        {
            if (plan.ResidenceCity?.RuntimeId == null
                || !TryResolveCityByRuntimeId(plan.ResidenceCity.RuntimeId, out residenceCity)
                || residenceCity == null
                || !ReferenceEquals(residenceCity.SelectedRuntimeOwners, plan.ResidenceCity)
                || !ReferenceEquals(residenceCity.Population, plan.ResidenceCity.Population)
                || (population = residenceCity.Population) == null
                || !populationAggregateSectionIdsByOwner.TryGetValue(
                    population, out string aggregateSectionId)
                || !ContainsCorePersonDeathSectionId(sectionIds, aggregateSectionId))
                return false;
        }

        if (npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _))
            return false;

        NpcRuntime.CoreSelectedPersonDeathProjectionSnapshot npcProjectionSnapshot = null;
        if (materializedNpc != null
            && (npcProjectionSnapshot = materializedNpc.CaptureCoreSelectedPersonDeathProjectionSnapshot(
                    plan.MaterializedNpc)) == null)
            return false;
        ContinuationCensusProtocol.CompatibilityMirrorBaselineSnapshot mirrorBaselineSnapshot =
            npcRosterCensusProtocol.CaptureCompatibilityMirrorBaselineSnapshot();
        if (mirrorBaselineSnapshot == null) return false;

        activeCoreSelectedPersonDeathProjectionContext =
            new CoreSelectedPersonDeathProjectionContext(
                plan, operationId, currentActionChanged, epochReservation, plan.Person,
                materializedNpc, residenceCity, population, sectionIds,
                npcProjectionSnapshot, mirrorBaselineSnapshot,
                politicalWorldRevision, lastPoliticalTruthFingerprint,
                hasPoliticalTruthFingerprint);
        lastCoreSelectedPersonDeathProjectionFailure = null;
        return true;
    }

    private bool TryCompleteCoreSelectedPersonDeathProjectionMutation(
        DailyV1SelectedPersonDeathPlan plan,
        string operationId,
        bool currentActionChanged,
        IReadOnlyList<string> sectionIds,
        bool operationSucceeded)
    {
        if (runtimeAdmissionContext == null)
            return activeCoreSelectedPersonDeathProjectionContext == null;

        CoreSelectedPersonDeathProjectionContext context =
            activeCoreSelectedPersonDeathProjectionContext;
        if (operationSucceeded)
        {
            if (context == null
                || !context.IsOwnedByCurrentThread()
                || !ReferenceEquals(context.Plan, plan)
                || !string.Equals(context.OperationId, operationId, StringComparison.Ordinal)
                || context.CurrentActionChanged != currentActionChanged
                || !HasExactCorePersonDeathSectionIds(sectionIds, context.ExpectedSectionIds))
            {
                FaultRuntimeAdmission();
                return false;
            }

            if (context.CompatibilityBaselinesPrepared)
            {
                // The baseline was already prepared and installed before Core
                // completed the reserved epoch. This branch only revokes the
                // exact operation-scoped writer context.
                if (!ReferenceEquals(activeCoreSelectedPersonDeathProjectionContext, context))
                {
                    lastCoreSelectedPersonDeathProjectionFailure = "PreparedProjectionContextChanged";
                    FaultRuntimeAdmission();
                    return false;
                }
                activeCoreSelectedPersonDeathProjectionContext = null;
                return true;
            }

            if (!context.ChangedSectionIds.SetEquals(context.ExpectedSectionIds)
                || npcRosterCensusProtocol == null)
            {
                // This is still a prepared, reversible Core transaction. The
                // caller restores the exact owner state and the original
                // mirror snapshot before the reservation is released.
                lastCoreSelectedPersonDeathProjectionFailure = "ChangedSectionsOrMirrorProtocolMismatch";
                return false;
            }

            if (!npcRosterCensusProtocol.TryPrepareCompatibilityMirrorBaselineUpdate(
                    sectionIds,
                    context.EpochReservation,
                    out ContinuationCensusProtocol.CompatibilityMirrorBaselineUpdate update,
                    out string rejection))
            {
                lastCoreSelectedPersonDeathProjectionFailure =
                    "MirrorBaselineStagingRejected:" + rejection;
                return false;
            }
            if (!npcRosterCensusProtocol.TryPublishCompatibilityMirrorBaselineUpdate(
                    update, context.EpochReservation))
            {
                lastCoreSelectedPersonDeathProjectionFailure = "MirrorBaselinePublicationRejected";
                return false;
            }

            context.CompatibilityBaselinesPrepared = true;
            context.PermissionRevoked = true;
            return true;
        }

        CoreSelectedPersonDeathProjectionContext failedContext =
            activeCoreSelectedPersonDeathProjectionContext;
        try
        {
            if (context == null
                || !ReferenceEquals(context, failedContext)
                || !context.IsOwnedByCurrentThread()
                || !ReferenceEquals(context.Plan, plan)
                || !string.Equals(context.OperationId, operationId, StringComparison.Ordinal)
                || context.CurrentActionChanged != currentActionChanged
                || !HasExactCorePersonDeathSectionIds(sectionIds, context.ExpectedSectionIds))
            {
                FaultRuntimeAdmission();
                return false;
            }

            bool projectionRestored = context.MaterializedNpc == null
                || context.MaterializedNpc.TryRestoreCoreSelectedPersonDeathProjection(
                    context.NpcProjectionSnapshot);
            politicalWorldRevision = context.PoliticalWorldRevisionBefore;
            lastPoliticalTruthFingerprint = context.PoliticalTruthFingerprintBefore;
            hasPoliticalTruthFingerprint = context.HasPoliticalTruthFingerprintBefore;
            bool baselinesRestored = npcRosterCensusProtocol != null
                && npcRosterCensusProtocol.TryRestoreCompatibilityMirrorBaselineSnapshot(
                    context.MirrorBaselineSnapshot);
            bool unchanged = baselinesRestored
                && npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _);
            if (!projectionRestored || !unchanged)
            {
                FaultRuntimeAdmission();
                return false;
            }

            return true;
        }
        catch
        {
            FaultRuntimeAdmission();
            return false;
        }
        finally
        {
            if (ReferenceEquals(activeCoreSelectedPersonDeathProjectionContext, failedContext))
                activeCoreSelectedPersonDeathProjectionContext = null;
        }
    }

    private static bool HasExactCorePersonDeathSectionIds(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual)
    {
        if (expected == null || actual == null || expected.Count == 0
            || expected.Count != actual.Count)
            return false;
        for (int index = 0; index < expected.Count; index++)
            if (string.IsNullOrWhiteSpace(expected[index])
                || !string.Equals(expected[index], actual[index], StringComparison.Ordinal))
                return false;
        return true;
    }

    private static bool ContainsCorePersonDeathSectionId(
        IReadOnlyList<string> sectionIds,
        string expectedSectionId)
    {
        if (sectionIds == null || string.IsNullOrWhiteSpace(expectedSectionId))
            return false;
        for (int index = 0; index < sectionIds.Count; index++)
            if (string.Equals(sectionIds[index], expectedSectionId, StringComparison.Ordinal))
                return true;
        return false;
    }

    private static bool HasExactCorePersonDeathSectionIds(
        IReadOnlyList<string> actual,
        HashSet<string> expected)
    {
        if (actual == null || expected == null || actual.Count == 0
            || actual.Count != expected.Count)
            return false;
        HashSet<string> actualSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in actual)
            if (string.IsNullOrWhiteSpace(sectionId) || !actualSet.Add(sectionId))
                return false;
        return actualSet.SetEquals(expected);
    }

    private bool TryPublishCoreSelectedPersonDeathProjection(
        DailyV1SelectedPersonDeathPlan plan,
        bool actionChanged)
    {
        if (plan?.MaterializedNpc == null)
            return plan != null;
        if (!npcRegistryById.TryGetValue(
                plan.MaterializedNpc.RuntimeId, out NpcRuntime materializedNpc)
            || materializedNpc == null
            || !ReferenceEquals(materializedNpc.SelectedRuntimeOwners, plan.MaterializedNpc)
            || !ReferenceEquals(materializedNpc.BoundPersonRuntime, plan.Person)
            || !materializedNpc.PublishCoreSelectedPersonDeath(
                plan.MaterializedNpc, actionChanged))
        {
            FaultRuntimeAdmission();
            return false;
        }
        return true;
    }

    private void FaultCoreSelectedPersonDeathRuntime() => FaultRuntimeAdmission();

    private void AdvanceCoreSelectedPersonDeathWorldRevision() =>
        AdvancePoliticalWorldRevision();

    private sealed class SelectedPersonDeathProjectionBoundary :
        IDailyV1SelectedPersonDeathProjectionBoundary
    {
        private readonly SimulationRuntime owner;

        internal SelectedPersonDeathProjectionBoundary(SimulationRuntime owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public bool TryBeginPersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds) =>
            owner.TryBeginCoreSelectedPersonDeathProjectionMutation(
                plan, operationId, currentActionChanged, sectionIds);

        public bool TryCompletePersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds,
            bool operationSucceeded) =>
            owner.TryCompleteCoreSelectedPersonDeathProjectionMutation(
                plan, operationId, currentActionChanged, sectionIds, operationSucceeded);

        public bool TryValidatePersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            out bool currentActionChanged,
            out string failure) =>
            owner.TryValidateCoreSelectedPersonDeathProjection(
                plan, out currentActionChanged, out failure);

        public bool TryPublishPersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged) =>
            owner.TryPublishCoreSelectedPersonDeathProjection(plan, currentActionChanged);

        public void FaultPersonDeathRuntime() =>
            owner.FaultCoreSelectedPersonDeathRuntime();

        public void AdvancePersonDeathWorldRevision() =>
            owner.AdvanceCoreSelectedPersonDeathWorldRevision();
    }

    private bool TryBeginCoreSelectedPersonDeathOperation(
        DailyV1SelectedPersonDeathPlan plan,
        bool actionChanged,
        string operationId,
        IReadOnlyList<string> sectionIds,
        Func<bool> localRevisionCapacity,
        out IDailyV1SelectedPersonDeathP12Scope scope)
    {
        scope = null;
        NpcRuntime materializedNpc = null;
        if (plan?.MaterializedNpc != null
            && (!npcRegistryById.TryGetValue(
                    plan.MaterializedNpc.RuntimeId, out materializedNpc)
                || materializedNpc == null
                || !ReferenceEquals(materializedNpc.SelectedRuntimeOwners, plan.MaterializedNpc)
                || !ReferenceEquals(materializedNpc.BoundPersonRuntime, plan.Person)))
            return false;

        Func<bool> completeRevisionCapacity = () =>
            localRevisionCapacity != null
            && localRevisionCapacity()
            && (materializedNpc == null
                || (materializedNpc.CanAdvanceP12LifecycleRevisions(
                        1, 0, actionChanged ? 1 : 0)
                    && materializedNpc.CanCommitSelectedPersonDeathAfterValidation(
                        NpcInjurySeverity.None, out bool currentActionChanged)
                    && currentActionChanged == actionChanged));

        if (!TryBeginP12PopulationOperation(
                operationId, sectionIds, completeRevisionCapacity,
                out P12PopulationOperationScope populationScope))
            return false;
        scope = populationScope;
        return true;
    }

    private sealed class SelectedPersonDeathP12Boundary :
        IDailyV1SelectedPersonDeathBoundary
    {
        private readonly SimulationRuntime owner;

        internal SelectedPersonDeathP12Boundary(SimulationRuntime owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public bool TryBeginPersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds) =>
            owner.TryBeginCoreSelectedPersonDeathProjectionMutation(
                plan, operationId, currentActionChanged, sectionIds);

        public bool TryCompletePersonDeathProjectionMutation(
            DailyV1SelectedPersonDeathPlan plan,
            string operationId,
            bool currentActionChanged,
            IReadOnlyList<string> sectionIds,
            bool operationSucceeded) =>
            owner.TryCompleteCoreSelectedPersonDeathProjectionMutation(
                plan, operationId, currentActionChanged, sectionIds, operationSucceeded);

        public bool TryValidatePersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            out bool currentActionChanged,
            out string failure) =>
            owner.TryValidateCoreSelectedPersonDeathProjection(
                plan, out currentActionChanged, out failure);

        public bool TryBeginPersonDeathOperation(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged,
            string operationId,
            IReadOnlyList<string> sectionIds,
            Func<bool> localRevisionCapacity,
            out IDailyV1SelectedPersonDeathP12Scope scope) =>
            owner.TryBeginCoreSelectedPersonDeathOperation(
                plan, currentActionChanged, operationId, sectionIds, localRevisionCapacity, out scope);

        public bool TryPublishPersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged) =>
            owner.TryPublishCoreSelectedPersonDeathProjection(plan, currentActionChanged);

        public void FaultPersonDeathRuntime() =>
            owner.FaultCoreSelectedPersonDeathRuntime();

        public void AdvancePersonDeathWorldRevision() =>
            owner.AdvanceCoreSelectedPersonDeathWorldRevision();
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
        CoreSelectedPersonDeathProjectionContext coreDeathContext =
            activeCoreSelectedPersonDeathProjectionContext;
        if (coreDeathContext != null)
            return !coreDeathContext.PermissionRevoked
                && coreDeathContext.IsOwnedByCurrentThread()
                && ReferenceEquals(coreDeathContext.Person, person)
                && coreDeathContext.ExpectedSectionIds.Contains(sectionId);

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
        CoreSelectedPersonDeathProjectionContext coreDeathContext =
            activeCoreSelectedPersonDeathProjectionContext;
        HashSet<string> allowedSectionIds;
        if (coreDeathContext != null)
        {
            if (coreDeathContext.PermissionRevoked
                || !coreDeathContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(coreDeathContext.MaterializedNpc, npc)
                || !lifeStateChanged
                || residenceChanged
                || currentActionChanged != coreDeathContext.CurrentActionChanged
                || nextActionRuntime != null)
                return false;
            allowedSectionIds = coreDeathContext.ExpectedSectionIds;
        }
        else
        {
            allowedSectionIds = populationContext != null && populationContext.IsOwnedByCurrentThread()
                ? populationContext.ExpectedSectionIds
                : membershipContext != null && membershipContext.IsOwnedByCurrentThread()
                    ? membershipContext.AllowedLifecycleSectionIds
                    : null;
        }
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
            if (activeCoreSelectedPersonDeathProjectionContext != null
                || activeP12PopulationOperationContext != null
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
        CoreSelectedPersonDeathProjectionContext coreDeathContext =
            activeCoreSelectedPersonDeathProjectionContext;
        if (coreDeathContext != null)
        {
            return !coreDeathContext.PermissionRevoked
                && coreDeathContext.IsOwnedByCurrentThread()
                && coreDeathContext.Plan.IsResident == aggregateChanged
                && !receiptChanged
                && ReferenceEquals(coreDeathContext.Population, owner)
                && populationAggregateSectionIdsByOwner.TryGetValue(
                    owner, out string coreAggregateId)
                && coreDeathContext.ExpectedSectionIds.Contains(coreAggregateId);
        }

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
        CoreSelectedPersonDeathProjectionContext coreDeathContext =
            activeCoreSelectedPersonDeathProjectionContext;
        if (coreDeathContext != null)
        {
            string coreAggregateId = null;
            bool aggregateSectionAuthorized = !aggregateChanged
                || (populationAggregateSectionIdsByOwner.TryGetValue(
                        owner, out coreAggregateId)
                    && coreDeathContext.ExpectedSectionIds.Contains(coreAggregateId));
            if (coreDeathContext.PermissionRevoked
                || !coreDeathContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(coreDeathContext.Population, owner)
                || coreDeathContext.Plan.IsResident != aggregateChanged
                || receiptChanged
                || !aggregateSectionAuthorized)
            {
                FaultRuntimeAdmission();
                return;
            }
            if (aggregateChanged)
                coreDeathContext.ChangedSectionIds.Add(coreAggregateId);
            return;
        }

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
        CoreSelectedPersonDeathProjectionContext coreDeathContext =
            activeCoreSelectedPersonDeathProjectionContext;
        if (coreDeathContext != null)
        {
            if (coreDeathContext.PermissionRevoked
                || !coreDeathContext.IsOwnedByCurrentThread()
                || !coreDeathContext.ExpectedSectionIds.Contains(sectionId))
                return false;
            coreDeathContext.ChangedSectionIds.Add(sectionId);
            return true;
        }

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
            localRevisionCapacity = () => PopulationOwnerBridge.CanAdvanceP12AggregateRevision(settlement.Population)
                && PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(person, residentDeath ? 2 : 1)
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
        localRevisionCapacity = () => PopulationOwnerBridge.CanAdvanceP12AggregateRevision(settlement.Population)
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
            localRevisionCapacity = () => PopulationOwnerBridge.CanAdvanceP12AggregateRevision(origin.Population)
                && PopulationOwnerBridge.CanAdvanceP12AggregateRevision(destination.Population)
                && PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(person, 1);
            return true;
        }

        sectionIds.Add(NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, true));
        localRevisionCapacity = () => PopulationOwnerBridge.CanAdvanceP12AggregateRevision(origin.Population)
            && PopulationOwnerBridge.CanAdvanceP12AggregateRevision(destination.Population)
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
