using System;
using System.Collections.Generic;
using System.Threading;

public sealed partial class SimulationRuntime
{
    private const string TravelPartyStartCensusOperationId = "runtime.travel-party.start";

    private readonly IOwnerSectionCensusProvider runtimeIdAllocatorTravelPartyCounterCensusProvider;
    private P12TravelPartyStartOperationContext activeP12TravelPartyStartOperationContext;
    private int p12TravelPartyStartRequestActive;
    private Thread p12TravelPartyStartRequestThread;

    private sealed class P12TravelPartyStartOperationContext
    {
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public SimulationOperationScope ProtocolScope;

        public P12TravelPartyStartOperationContext(Thread ownerThread, SimulationOperationScope protocolScope)
        {
            OwnerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
            OwnerManagedThreadId = ownerThread.ManagedThreadId;
            ProtocolScope = protocolScope ?? throw new ArgumentNullException(nameof(protocolScope));
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private bool TryRegisterRuntimeIdAllocatorTravelPartyCounterCensusProvider(
        ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || runtimeIdAllocator == null
            || runtimeIdAllocatorTravelPartyCounterCensusProvider == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        try
        {
            OwnerSectionCensusWitness witness = runtimeIdAllocatorTravelPartyCounterCensusProvider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(
                    witness.SectionId,
                    RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || witness.Cardinality != 1
                || !ReferenceEquals(witness.OwnerInstanceIdentity, runtimeIdAllocator.CensusOwnerIdentity)
                || witness.Revision != runtimeIdAllocator.GetCensusRevision(RuntimeIdAllocatorCensusCounter.TravelParties)
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
                        RuntimeIdAllocatorCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
                    runtimeIdAllocatorTravelPartyCounterCensusProvider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryStartP12TravelParty(ActionExecutionContext context)
    {
        if (Interlocked.CompareExchange(ref p12TravelPartyStartRequestActive, 1, 0) != 0)
        {
            FaultRuntimeAdmission();
            return false;
        }

        p12TravelPartyStartRequestThread = Thread.CurrentThread;
        try
        {
            return travelPartySystem != null && travelPartySystem.TryStartTravelParty(context);
        }
        finally
        {
            P12TravelPartyStartOperationContext operation = activeP12TravelPartyStartOperationContext;
            if (operation != null) ExitP12TravelPartyStartOperation(operation);
            p12TravelPartyStartRequestThread = null;
            Interlocked.Exchange(ref p12TravelPartyStartRequestActive, 0);
        }
    }

    private bool TryAdmitP12TravelPartyStartPreparation(TravelPartyStartPreparation preparation)
    {
        if (runtimeAdmissionContext == null) return true;
        if (Volatile.Read(ref p12TravelPartyStartRequestActive) == 0)
        {
            // Direct TravelPartySystem entry is unsupported by this P12 operation.
            return false;
        }
        if (!ReferenceEquals(p12TravelPartyStartRequestThread, Thread.CurrentThread))
        {
            FaultRuntimeAdmission();
            return false;
        }
        return TryBeginP12TravelPartyStartOperation(preparation);
    }

    private bool TryBeginP12TravelPartyStartOperation(TravelPartyStartPreparation preparation)
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || preparation == null
            || travelPartySystem == null
            || runtimeIdAllocator == null
            || simulationRecordSequence == null
            || !ReferenceEquals(p12TravelPartyStartRequestThread, Thread.CurrentThread)
            || activeP12TravelPartyStartOperationContext != null
            || activeP12TravelPartyAdvanceOperationContext != null
            || activeP12SoloTravelStartOperationContext != null
            || activeP12MerchantOperationContext != null
            || activeNpcMembershipCensusContext != null
            || !travelPartySystem.UsesP12IdentityOwners(runtimeIdAllocator, simulationRecordSequence)
            || !travelPartySystem.Store.CanCommitMutations(2)
            || travelPartyCensusProvider == null
            || travelPartyStoreMutationBinding == null
            || !ReferenceEquals(travelPartyStoreMutationBinding.Owner, travelPartySystem.Store)
            || !ReferenceEquals(travelPartySystem.IdAllocator, runtimeIdAllocator)
            || runtimeIdAllocatorTravelPartyCounterCensusProvider == null
            || runtimeIdAllocatorEventCounterCensusProvider == null
            || simulationRecordSequenceCensusProvider == null)
        {
            FaultRuntimeAdmission();
            return false;
        }

        List<string> sectionIds = new List<string>();
        HashSet<string> distinctSectionIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            OwnerSectionCensusWitness partyWitness = travelPartyCensusProvider.GetCurrentCensus();
            if (partyWitness == null
                || !string.Equals(partyWitness.SectionId, TravelPartyCensusProvider.SectionId, StringComparison.Ordinal)
                || partyWitness.SchemaVersion != TravelPartyCensusProvider.SchemaVersion
                || !ReferenceEquals(partyWitness.OwnerInstanceIdentity, travelPartySystem.Store)
                || partyWitness.Cardinality != travelPartySystem.Store.ActiveParties.Count
                || partyWitness.Revision != travelPartySystem.Store.Revision)
                throw new InvalidOperationException();
            AddP12TravelPartyStartSection(sectionIds, distinctSectionIds, TravelPartyCensusProvider.SectionId);

            OwnerSectionCensusWitness partyCounterWitness =
                runtimeIdAllocatorTravelPartyCounterCensusProvider.GetCurrentCensus();
            long partyCounterRevision = runtimeIdAllocator.GetCensusRevision(RuntimeIdAllocatorCensusCounter.TravelParties);
            if (partyCounterWitness == null
                || !string.Equals(
                    partyCounterWitness.SectionId,
                    RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
                    StringComparison.Ordinal)
                || partyCounterWitness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || partyCounterWitness.Cardinality != 1
                || !ReferenceEquals(partyCounterWitness.OwnerInstanceIdentity, runtimeIdAllocator.CensusOwnerIdentity)
                || partyCounterWitness.Revision != partyCounterRevision
                || partyCounterRevision > long.MaxValue - 2L)
                throw new InvalidOperationException();
            AddP12TravelPartyStartSection(
                sectionIds,
                distinctSectionIds,
                RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId);

            OwnerSectionCensusWitness eventWitness = runtimeIdAllocatorEventCounterCensusProvider.GetCurrentCensus();
            long eventRevision = runtimeIdAllocator.GetCensusRevision(RuntimeIdAllocatorCensusCounter.Events);
            if (eventWitness == null
                || !string.Equals(eventWitness.SectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal)
                || eventWitness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || eventWitness.Cardinality != 1
                || !ReferenceEquals(eventWitness.OwnerInstanceIdentity, runtimeIdAllocator.CensusOwnerIdentity)
                || eventWitness.Revision != eventRevision)
                throw new InvalidOperationException();
            AddP12TravelPartyStartSection(
                sectionIds,
                distinctSectionIds,
                RuntimeIdAllocatorCensusProvider.EventsSectionId);

            OwnerSectionCensusWitness sequenceWitness = simulationRecordSequenceCensusProvider.GetCurrentCensus();
            long sequenceRevision = simulationRecordSequence.CensusRevision;
            if (sequenceWitness == null
                || !string.Equals(sequenceWitness.SectionId, SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal)
                || sequenceWitness.SchemaVersion != SimulationRecordSequenceCensusProvider.SchemaVersion
                || sequenceWitness.Cardinality != 1
                || !ReferenceEquals(sequenceWitness.OwnerInstanceIdentity, simulationRecordSequence.CensusOwnerIdentity)
                || sequenceWitness.Revision != sequenceRevision)
                throw new InvalidOperationException();
            AddP12TravelPartyStartSection(
                sectionIds,
                distinctSectionIds,
                SimulationRecordSequenceCensusProvider.SectionId);

            if (travelPartySystem.HasDomainEventRecorder
                && (eventRevision > long.MaxValue - 2L || sequenceRevision > long.MaxValue - 2L))
                throw new InvalidOperationException();

            if (preparation.Members == null
                || preparation.Costs == null
                || preparation.Members.Count == 0
                || preparation.Members.Count != preparation.Costs.Count
                || preparation.OriginLocation == null
                || preparation.Route == null)
                throw new InvalidOperationException();

            Dictionary<string, TravelPartyMemberCost> costsByRuntimeId =
                new Dictionary<string, TravelPartyMemberCost>(StringComparer.Ordinal);
            foreach (TravelPartyMemberCost cost in preparation.Costs)
            {
                if (cost == null
                    || string.IsNullOrWhiteSpace(cost.RuntimeId)
                    || costsByRuntimeId.ContainsKey(cost.RuntimeId))
                    throw new InvalidOperationException();
                costsByRuntimeId.Add(cost.RuntimeId, cost);
            }

            HashSet<string> memberRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<NpcRuntime> memberOwners = new HashSet<NpcRuntime>();
            Dictionary<CityRuntime, long> cityRevisionIncrements = new Dictionary<CityRuntime, long>();
            foreach (NpcRuntime member in preparation.Members)
            {
                if (member == null
                    || string.IsNullOrWhiteSpace(member.RuntimeId)
                    || !memberRuntimeIds.Add(member.RuntimeId)
                    || !memberOwners.Add(member)
                    || !npcRegistryById.TryGetValue(member.RuntimeId, out NpcRuntime installed)
                    || !ReferenceEquals(installed, member)
                    || !npcRuntimes.Contains(member)
                    || !ReferenceEquals(member.CurrentLocation, preparation.OriginLocation)
                    || !member.CanPreflightTravelStateMutations(4L)
                    || !costsByRuntimeId.TryGetValue(member.RuntimeId, out TravelPartyMemberCost cost))
                    throw new InvalidOperationException();

                if (!npcTravelStateMutationBindings.TryGetValue(
                        member,
                        out P12NpcOwnerMutationBinding travelStateBinding))
                    throw new InvalidOperationException();

                List<CityRuntime> changedCities = new List<CityRuntime>(2);
                CityRuntime currentCity = member.CurrentCity;
                if (currentCity != null && currentCity.ContainsImportantNpc(member))
                {
                    changedCities.Add(currentCity);
                    AddP12TravelPartyStartCityRevision(cityRevisionIncrements, currentCity);
                }
                if (preparation.OriginCity != null)
                {
                    if (!changedCities.Contains(preparation.OriginCity))
                        changedCities.Add(preparation.OriginCity);
                    AddP12TravelPartyStartCityRevision(cityRevisionIncrements, preparation.OriginCity);
                }

                string[] travelSections = GetP12TravelMutationSectionIds(
                    travelStateBinding,
                    true,
                    changedCities);
                if (travelSections == null || travelSections.Length == 0)
                    throw new InvalidOperationException();
                foreach (string sectionId in travelSections)
                    AddP12TravelPartyStartSection(sectionIds, distinctSectionIds, sectionId);

                if (!TryResolveNpcMoneyAccountSection(member, out string accountSectionId)
                    || !npcMoneyAccountMutationBindings.TryGetValue(
                        member.MoneyAccount,
                        out P12NpcOwnerMutationBinding accountBinding)
                    || !ReferenceEquals(accountBinding.Owner, member.MoneyAccount)
                    || accountBinding.SectionIds.Length != 1
                    || !string.Equals(accountBinding.SectionIds[0], accountSectionId, StringComparison.Ordinal))
                    throw new InvalidOperationException();
                if (cost.Amount > 0f && member.MoneyAccount.Revision > long.MaxValue - 2L)
                    throw new InvalidOperationException();
                AddP12TravelPartyStartSection(sectionIds, distinctSectionIds, accountSectionId);

                SpatialKnowledgeRuntime knowledge = member.ExistingSpatialKnowledge;
                if (knowledge == null
                    || !npcSpatialKnowledgeMutationBindings.TryGetValue(
                        knowledge,
                        out P12NpcOwnerMutationBinding knowledgeBinding)
                    || !ReferenceEquals(knowledgeBinding.Owner, knowledge)
                    || knowledgeBinding.SectionIds.Length != 2
                    || !IsCurrentP12MerchantOwnerBinding(knowledgeBinding))
                    throw new InvalidOperationException();
                long knowledgeIncrements = 0L;
                if (!knowledge.KnowsLocation(preparation.OriginLocation.RuntimeId)) knowledgeIncrements++;
                if (!knowledge.KnowsRoute(preparation.Route.RuntimeId)) knowledgeIncrements++;
                if (knowledge.Revision > long.MaxValue - knowledgeIncrements)
                    throw new InvalidOperationException();
                foreach (string sectionId in knowledgeBinding.SectionIds)
                    AddP12TravelPartyStartSection(sectionIds, distinctSectionIds, sectionId);
            }

            if (memberRuntimeIds.Count != costsByRuntimeId.Count)
                throw new InvalidOperationException();

            foreach (KeyValuePair<CityRuntime, long> requirement in cityRevisionIncrements)
            {
                if (!requirement.Key.CanApplyImportantNpcRevisionIncrements(requirement.Value))
                    throw new InvalidOperationException();
                if (!cities.Contains(requirement.Key)
                    || !cityNpcPresenceSectionIdsByOwner.TryGetValue(
                        requirement.Key,
                        out string citySectionId)
                    || !string.Equals(
                        citySectionId,
                        CityNpcPresenceCensusProvider.SectionIdFor(requirement.Key.RuntimeId),
                        StringComparison.Ordinal))
                    throw new InvalidOperationException();
                AddP12TravelPartyStartSection(sectionIds, distinctSectionIds, citySectionId);
            }

            if (!npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
                || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
                || !TryEnterRuntimeAdmissionOperation(
                    TravelPartyStartCensusOperationId,
                    out SimulationOperationScope protocolScope))
                throw new InvalidOperationException();

            activeP12TravelPartyStartOperationContext =
                new P12TravelPartyStartOperationContext(Thread.CurrentThread, protocolScope);
            return true;
        }
        catch
        {
            FaultRuntimeAdmission();
            return false;
        }
    }

    private static void AddP12TravelPartyStartSection(
        List<string> sectionIds,
        HashSet<string> distinctSectionIds,
        string sectionId)
    {
        if (string.IsNullOrWhiteSpace(sectionId)) throw new InvalidOperationException();
        if (distinctSectionIds.Add(sectionId)) sectionIds.Add(sectionId);
    }

    private static void AddP12TravelPartyStartCityRevision(
        Dictionary<CityRuntime, long> incrementsByCity,
        CityRuntime city)
    {
        if (city == null) throw new InvalidOperationException();
        incrementsByCity.TryGetValue(city, out long increments);
        incrementsByCity[city] = increments + 1L;
    }

    private bool CanCommitP12RuntimeIdTravelPartyCounterMutation()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || runtimeIdAllocator == null
            || runtimeIdAllocatorTravelPartyCounterCensusProvider == null
            || activeP12TravelPartyStartOperationContext == null
            || !ReferenceEquals(travelPartySystem?.IdAllocator, runtimeIdAllocator)
            || !CanCommitP12MutationSections(
                new[] { RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId })
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _))
        {
            FaultRuntimeAdmission();
            return false;
        }
        return true;
    }

    private void NotifyP12RuntimeIdTravelPartyCounterMutation()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || activeP12TravelPartyStartOperationContext == null
                || !NotifyP12MutationSections(
                    new[] { RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId }))
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    "The committed P12 TravelParty-ID allocation could not be reported to its census protocol.");
            }
        }
        catch (InvalidOperationException)
        {
            FaultRuntimeAdmission();
            throw;
        }
        catch (Exception exception)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The committed P12 TravelParty-ID allocation could not be reported to its census protocol.",
                exception);
        }
    }

    private void ExitP12TravelPartyStartOperation(P12TravelPartyStartOperationContext context)
    {
        if (context == null) return;
        try
        {
            if (!ReferenceEquals(activeP12TravelPartyStartOperationContext, context)
                || !context.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return;
            }

            if (context.ChangedSectionIds.Count != 0
                && !npcRosterCensusProtocol.NotifyCommittedMutations(context.ChangedSectionIds, out _))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
        finally
        {
            if (ReferenceEquals(activeP12TravelPartyStartOperationContext, context))
                activeP12TravelPartyStartOperationContext = null;
            SimulationOperationScope operation = context.ProtocolScope;
            context.ProtocolScope = null;
            operation?.Dispose();
        }
    }
}
