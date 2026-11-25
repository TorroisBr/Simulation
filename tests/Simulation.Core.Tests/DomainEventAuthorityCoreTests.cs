using System.Linq;
using NUnit.Framework;

public sealed class DomainEventAuthorityCoreTests
{
    private sealed class TravelPartyCensusProviderForCoreTest : IOwnerSectionCensusProvider
    {
        private readonly OwnerSectionCensusSnapshot snapshot;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly TravelPartyStore travelParties;

        internal TravelPartyCensusProviderForCoreTest(
            OwnerSectionCensusSnapshot snapshot,
            DailyV1SelectedRuntimeOwnerGraph graph,
            TravelPartyStore travelParties = null)
        {
            this.snapshot = snapshot ?? throw new System.ArgumentNullException(nameof(snapshot));
            this.graph = graph ?? throw new System.ArgumentNullException(nameof(graph));
            this.travelParties = travelParties;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (string.Equals(snapshot.SectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    System.StringComparison.Ordinal))
                return RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(
                    graph.RuntimeIdAllocator).GetCurrentCensus();
            if (string.Equals(snapshot.SectionId, SimulationRecordSequenceCensusProvider.SectionId,
                    System.StringComparison.Ordinal))
                return new SimulationRecordSequenceCensusProvider(
                    graph.RecordSequence).GetCurrentCensus();
            if (travelParties != null)
                return new TravelPartyCensusProvider(travelParties).GetCurrentCensus();
            if (TryGetLiveNpcOwner(out DailyV1NpcRuntimeOwners npc))
                return CreateLiveNpcWitness(npc);
            if (TryGetLiveCityOwner(out DailyV1CityRuntimeOwners city))
                return new OwnerSectionCensusWitness(
                    snapshot.SectionId, snapshot.SchemaVersion, city.NpcPresence,
                    city.NpcPresence.OrderedNpcRuntimeIds.Count, city.NpcPresence.Revision);
            return new OwnerSectionCensusWitness(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.OwnerInstanceIdentity,
                snapshot.Cardinality, snapshot.Revision);
        }

        private bool TryGetLiveNpcOwner(out DailyV1NpcRuntimeOwners npc)
        {
            npc = null;
            string runtimeId = null;
            if (snapshot.SectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix,
                    System.StringComparison.Ordinal))
                runtimeId = snapshot.SectionId.Substring(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix.Length);
            else if (snapshot.SectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix,
                    System.StringComparison.Ordinal))
                runtimeId = snapshot.SectionId.Substring(
                    DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix.Length);
            else if (snapshot.SectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix,
                    System.StringComparison.Ordinal))
                runtimeId = snapshot.SectionId.Substring(
                    DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix.Length);
            return !string.IsNullOrWhiteSpace(runtimeId)
                && graph.TryGetNpcOwner(runtimeId, out npc)
                && npc != null && graph.ContainsExactNpcOwner(npc);
        }

        private OwnerSectionCensusWitness CreateLiveNpcWitness(DailyV1NpcRuntimeOwners npc)
        {
            if (snapshot.SectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix,
                    System.StringComparison.Ordinal))
                return new OwnerSectionCensusWitness(
                    snapshot.SectionId, snapshot.SchemaVersion, npc,
                    1, npc.Travel.Revision);
            bool locations = snapshot.SectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix,
                System.StringComparison.Ordinal);
            return new OwnerSectionCensusWitness(
                snapshot.SectionId, snapshot.SchemaVersion, npc.SpatialKnowledge,
                locations ? npc.SpatialKnowledge.KnownLocationRuntimeIds.Count
                    : npc.SpatialKnowledge.KnownRouteRuntimeIds.Count,
                npc.SpatialKnowledge.Revision);
        }

        private bool TryGetLiveCityOwner(out DailyV1CityRuntimeOwners city)
        {
            city = null;
            string prefix = DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix;
            if (!snapshot.SectionId.StartsWith(prefix, System.StringComparison.Ordinal))
                return false;
            string runtimeId = snapshot.SectionId.Substring(prefix.Length);
            if (string.IsNullOrWhiteSpace(runtimeId)) return false;
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            {
                if (candidate == null
                    || !string.Equals(candidate.RuntimeId, runtimeId, System.StringComparison.Ordinal))
                    continue;
                if (city != null) return false;
                city = candidate;
            }
            return city != null && graph.ContainsExactCityOwner(city)
                && city.NpcPresence != null;
        }
    }

    [Test]
    public void SelectedGenesisAndPreparedSessionRetainOneCoreEventHistoryAuthority()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, null, out DailyV1GenesisComposition composition,
            out DailyV1ActiveRuntimeSession session, out string diagnostic), Is.True, diagnostic);

        DailyV1DomainEventAuthority authority = composition.EventAuthority;
        Assert.That(authority, Is.Not.Null);
        Assert.That(authority.IsFor(
            composition.RuntimeIdAllocator, composition.SimulationTime,
            composition.RecordSequence, composition.DecisionStore), Is.True);
        Assert.That(authority.EventStore.UsesHistoryStore(authority.History), Is.True);
        Assert.That(authority.Recorder.UsesEventStore(authority.EventStore), Is.True);
        Assert.That(authority.EventStore.Events, Is.Empty);
        Assert.That(authority.History.HistoricalEvents, Is.Empty);
        Assert.That(session.EventAuthority, Is.SameAs(authority));
        Assert.That(session.EventAuthority.EventStore, Is.SameAs(authority.EventStore));
        Assert.That(session.EventAuthority.History, Is.SameAs(authority.History));
        HistoryStore conflictingHistory = new HistoryStore();
        DomainEventStore conflictingStore = new DomainEventStore(
            conflictingHistory, new HistoryPolicy());
        DomainEventRecorder conflictingRecorder = new DomainEventRecorder(
            composition.RuntimeIdAllocator, composition.SimulationTime,
            composition.RecordSequence, conflictingStore);
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedTravelEventRecorders(
            conflictingRecorder, conflictingRecorder), Is.False,
            "Core session creation must bind its exact recorder before another same-identity store can claim travel recording.");
        Assert.That(authority.TryBindSelectedTravelEventRecorders(
            composition.ExecutionBoundaryState), Is.True,
            "The Unity host must validate Core's exact recorder idempotently.");
    }

    [Test]
    public void DecisionOccurrenceReceiptAuthorityOwnsReplayFingerprintAndP12Census()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        SimulationTime time = new SimulationTime(12L);
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        NpcDecisionStore decisions = new NpcDecisionStore();
        DailyV1DecisionOccurrenceReceiptAuthority authority =
            DailyV1DecisionOccurrenceReceiptAuthority.Create(
                allocator, time, sequence, decisions);

        OwnerSectionCensusWitness initial = authority.GetCensus();
        Assert.That(initial.SectionId, Is.EqualTo("p12f.npc-decision-occurrence-receipts"));
        Assert.That(initial.SchemaVersion, Is.EqualTo(1));
        Assert.That(initial.Cardinality, Is.Zero);
        Assert.That(initial.Revision, Is.Zero);
        Assert.That(initial.OwnerInstanceIdentity, Is.Not.Null);
        Assert.That(authority.IsFor(allocator, time, sequence, decisions), Is.True);
        Assert.That(authority.IsFor(
            allocator, time, sequence, new NpcDecisionStore()), Is.False);

        Assert.That(authority.TryRecordOccurrenceOnce(
            "merchant.trade.once",
            "fingerprint-v1",
            "npc-receipt-owner",
            NpcDecisionType.TradeSale,
            NpcDecisionOrigin.Autonomous,
            "action-sell",
            "location-receipt-target",
            null,
            out NpcDecisionRecord first), Is.True);
        Assert.That(first, Is.Not.Null);
        Assert.That(first.DecisionId, Is.EqualTo("decision-000001"));
        Assert.That(first.AbsoluteDay, Is.EqualTo(12L));
        Assert.That(first.RecordSequence, Is.EqualTo(1L));
        Assert.That(decisions.Decisions, Has.Count.EqualTo(1));

        Assert.That(authority.TryRecordOccurrenceOnce(
            "merchant.trade.once",
            "fingerprint-v1",
            "npc-receipt-owner",
            NpcDecisionType.TradeSale,
            NpcDecisionOrigin.Autonomous,
            "action-sell",
            "location-receipt-target",
            null,
            out NpcDecisionRecord replay), Is.True);
        Assert.That(replay, Is.SameAs(first));
        Assert.That(decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(sequence.CaptureSnapshot().NextSequence, Is.EqualTo(2L));

        Assert.That(authority.TryRecordOccurrenceOnce(
            "merchant.trade.once",
            "fingerprint-v2",
            "npc-receipt-owner",
            NpcDecisionType.TradeSale,
            NpcDecisionOrigin.Autonomous,
            "action-sell",
            "location-receipt-target",
            null,
            out NpcDecisionRecord conflict), Is.False);
        Assert.That(conflict, Is.Null);
        Assert.That(decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(sequence.CaptureSnapshot().NextSequence, Is.EqualTo(2L));
        Assert.That(allocator.AllocateDecisionId(), Is.EqualTo("decision-000002"));

        OwnerSectionCensusWitness completed = authority.GetCensus();
        Assert.That(completed.OwnerInstanceIdentity, Is.SameAs(initial.OwnerInstanceIdentity));
        Assert.That(completed.Cardinality, Is.EqualTo(1));
        Assert.That(completed.Revision, Is.EqualTo(1L));
    }
    [Test]
    public void RecorderUsesExactAllocatorClockAndSequenceOwnersAndRecordsArrivalIdentity()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        SimulationTime time = new SimulationTime(17L);
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        HistoryStore history = new HistoryStore();
        DomainEventStore store = new DomainEventStore(history, new HistoryPolicy());
        DomainEventRecorder recorder = new DomainEventRecorder(
            allocator, time, sequence, store);

        Assert.That(recorder.UsesIdentityOwners(allocator, time, sequence), Is.True);
        Assert.That(recorder.UsesIdentityOwners(
            allocator, new SimulationTime(17L), sequence), Is.False);
        Assert.That(recorder.UsesIdentityOwners(
            allocator, time, new SimulationRecordSequence()), Is.False);

        Assert.That(recorder.Record((eventId, absoluteDay, recordSequence) =>
            new NpcArrivedEvent(
                eventId, absoluteDay, recordSequence,
                "npc-core-arrival", "location-core-arrival", "decision-core-arrival")), Is.True);

        NpcArrivedEvent recorded = store.Events[0] as NpcArrivedEvent;
        Assert.That(recorded, Is.Not.Null);
        Assert.That(recorded.EventId, Is.EqualTo("event-000001"));
        Assert.That(recorded.AbsoluteDay, Is.EqualTo(17L));
        Assert.That(recorded.RecordSequence, Is.EqualTo(1L));
        Assert.That(recorded.OriginDecisionId, Is.EqualTo("decision-core-arrival"));
        Assert.That(recorded.ActorRuntimeId, Is.EqualTo("npc-core-arrival"));
        Assert.That(recorded.DestinationLocationRuntimeId, Is.EqualTo("location-core-arrival"));
        Assert.That(recorded.GetParticipants(), Has.Count.EqualTo(1));
        Assert.That(recorded.GetParticipants()[0].RuntimeId, Is.EqualTo("npc-core-arrival"));
        Assert.That(recorded.GetParticipants()[0].Role, Is.EqualTo(DomainEventParticipantRole.Actor));
        Assert.That(history.HistoricalEvents, Is.Empty,
            "Arrival events remain the noncausal event read model, not retained causal history.");
        Assert.That(sequence.CaptureSnapshot().NextSequence, Is.EqualTo(2L));
        Assert.That(allocator.AllocateEventId(), Is.EqualTo("event-000002"));
    }

    [Test]
    public void TravelPartyArrivalEventPreservesParticipantSnapshotAndRoleOrdering()
    {
        TravelPartyArrivedEvent arrival = new TravelPartyArrivedEvent(
            "event-party-arrived",
            8L,
            31L,
            "party-arrived",
            "location-origin",
            "location-destination",
            "route-arrival",
            4,
            new[] { "traveler-b", "traveler-a" },
            new[] { "escort-c" },
            "decision-party-arrival");

        Assert.That(arrival.EventType, Is.EqualTo(DomainEventType.TravelPartyArrived));
        Assert.That(arrival.AbsoluteDay, Is.EqualTo(8L));
        Assert.That(arrival.RecordSequence, Is.EqualTo(31L));
        Assert.That(arrival.TravelerRuntimeIds, Is.EqualTo(new[] { "traveler-b", "traveler-a" }));
        Assert.That(arrival.EscortRuntimeIds, Is.EqualTo(new[] { "escort-c" }));
        Assert.That(arrival.OriginDecisionId, Is.EqualTo("decision-party-arrival"));
        Assert.That(arrival.GetParticipants().Count, Is.EqualTo(3));
        Assert.That(arrival.GetParticipants()[0].Role, Is.EqualTo(DomainEventParticipantRole.Actor));
        Assert.That(arrival.GetParticipants()[1].Role, Is.EqualTo(DomainEventParticipantRole.Actor));
        Assert.That(arrival.GetParticipants()[2].Role, Is.EqualTo(DomainEventParticipantRole.Support));
    }

    [Test]
    public void RejectedArrivalFactoryPreservesExistingAllocationOrder()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        SimulationTime time = new SimulationTime(2L);
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        DomainEventStore store = new DomainEventStore(new HistoryStore(), new HistoryPolicy());
        DomainEventRecorder recorder = new DomainEventRecorder(allocator, time, sequence, store);

        Assert.That(recorder.Record((eventId, absoluteDay, recordSequence) =>
            new NpcArrivedEvent(eventId, absoluteDay, recordSequence, "npc-invalid-arrival", null)), Is.False);
        Assert.That(store.Events, Is.Empty);
        Assert.That(sequence.CaptureSnapshot().NextSequence, Is.EqualTo(2L));
        Assert.That(allocator.AllocateEventId(), Is.EqualTo("event-000002"));
    }

    [Test]
    public void AdmittedCoreSessionRejectsPrematurePartyArrivalRecordInsideItsAdvanceAndP12Scopes()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners member = graph.Npcs[0];
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, member.Travel.CurrentCityRuntimeId,
                System.StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.First(city =>
            !ReferenceEquals(city, origin));
        const string partyId = "core-selected-party-arrival";
        const string decisionId = "core-selected-party-arrival-decision";
        const string routeId = "core-selected-party-arrival-route";
        TravelPartyRuntime party = new TravelPartyRuntime(
            partyId,
            origin.LocationRuntimeId,
            destination.LocationRuntimeId,
            routeId,
            new[] { member.RuntimeId },
            System.Array.Empty<string>(),
            2,
            decisionId,
            new[] { new TravelPartyMemberCost(member.RuntimeId, 1f) });
        Assert.That(graph.TravelParties.Add(party), Is.True);

        DailyV1DomainEventAuthority authority = composition.EventAuthority;
        HistoryStore history = authority.History;
        DomainEventStore store = authority.EventStore;
        DomainEventRecorder recorder = authority.Recorder;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        Assert.That(state.HasRegisteredRuntimeProtocolOperationContracts, Is.False);
        System.Collections.Generic.IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: true,
                hasTravelSystem: false);
        foreach (string operationContractId in operationContractIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True,
                operationContractId);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
        Assert.That(state.TryBindSelectedTravelEventRecorders(recorder, null), Is.True);
        Assert.That(state.TryRecordSelectedTravelPartyArrival(
            party, out bool eventRecordedOutsideP12), Is.False,
            "The arrival write requires both the advance lease and active P12 party operation.");
        Assert.That(eventRecordedOutsideP12, Is.False);

        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);
            bool coreOperationWasVisibleToCapture = false;
            bool scopeResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                new NoOpTravelPartyAdvanceProtocol(),
                () =>
                {
                    coreOperationWasVisibleToCapture =
                        state.ActiveRuntimeOperationCount == 2
                        && !state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None);
                    return !state.TryRecordSelectedTravelPartyArrival(
                        party, out bool wasRecorded) && !wasRecorded;
                });
            Assert.That(scopeResult, Is.True,
                "The Core party operation accepts a rejected premature event probe as a no-op.");
            Assert.That(coreOperationWasVisibleToCapture, Is.True,
                "The Core-owned party operation must be nested in the daily operation and block capture.");
            Assert.That(state.ActiveRuntimeOperationCount, Is.EqualTo(1),
                "The nested party operation must close while the outer daily operation remains active.");
            Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);

        Assert.That(store.Events, Is.Empty,
            "A party-arrival record cannot be emitted until the owner graph reflects arrival and completion.");
        Assert.That(history.HistoricalEvents, Is.Empty);
        Assert.That(composition.RecordSequence.CaptureSnapshot().NextSequence, Is.EqualTo(1L),
            "The rejected pre-completion probe does not consume a record identity.");
    }

    [Test]
    public void TravelPartyAdvanceUsesCoreStoreBaselineAndTracksOneCompletionPerCommit()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        Assert.That(graph.Npcs, Is.Not.Empty);
        string memberRuntimeId = graph.Npcs[0].RuntimeId;
        TravelPartyRuntime party = new TravelPartyRuntime(
            "core-travel-party-baseline",
            "origin-location",
            "destination-location",
            "route",
            new[] { memberRuntimeId },
            System.Array.Empty<string>(),
            2,
            null,
            new[] { new TravelPartyMemberCost(memberRuntimeId, 0f) });
        Assert.That(graph.TravelParties.Add(party), Is.True);
        Assert.That(graph.TravelParties.Revision, Is.EqualTo(1L));

        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        long initialMutationEpoch = state.MutationEpochState.CurrentEpoch;
        System.Collections.Generic.IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: true,
                hasTravelSystem: false);
        foreach (string operationContractId in operationContractIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True,
                operationContractId);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);

        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    string[] travelPartySection = { TravelPartyCensusProvider.SectionId };
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        travelPartySection, out bool admissionHandled), Is.True);
                    Assert.That(admissionHandled, Is.True);

                    Assert.That(graph.TravelParties.Complete(party.TravelPartyId), Is.True);
                    Assert.That(state.TryRecordSelectedP12TravelPartyMutation(
                        travelPartySection, out bool commitHandled), Is.True);
                    Assert.That(commitHandled, Is.True);

                    // This test simulates the optional store callback with an
                    // explicit first receipt; this composition does not bind
                    // that callback. The stage then reports the same completed
                    // row after Complete returns, so the duplicate remains
                    // idempotent and cannot publish a second epoch.
                    Assert.That(state.TryRecordSelectedP12TravelPartyMutation(
                        travelPartySection, out bool duplicateCommitHandled), Is.True);
                    Assert.That(duplicateCommitHandled, Is.True);

                    // A later admission in this same batch observes the
                    // updated Core baseline without another host owner check.
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        travelPartySection, out admissionHandled), Is.True);
                    Assert.That(admissionHandled, Is.True);
                    return true;
                });

            Assert.That(operationResult, Is.True);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(graph.TravelParties.ActiveParties, Is.Empty);
        Assert.That(graph.TravelParties.Revision, Is.EqualTo(2L));
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(initialMutationEpoch + 1L));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1),
            "Only the initial full-vector baseline should be validated through the host protocol.");
        Assert.That(protocol.LastRefreshedMutationSectionIds,
            Does.Contain(TravelPartyCensusProvider.SectionId));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
    }

    [Test]
    public void DetachedCoreTravelPortsValidateExactCutAndPublishOneCoreEpoch()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        DailyV1ApplicationHostTests.AdmitMissingSlot(application);
        Assert.That(application.TryPrepareInitialGenesis(
            definitions, null, out DailyV1GenesisComposition composition,
            out DailyV1ActiveRuntimeSession session, out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        Assert.That(graph.Npcs, Is.Not.Empty);
        DailyV1NpcRuntimeOwners member = graph.Npcs[0];
        string memberRuntimeId = member.RuntimeId;
        DailyV1CityRuntimeOwners originCity = FindCityWithMember(graph, memberRuntimeId);
        Assert.That(originCity, Is.Not.Null);
        SpatialRouteRuntime route = graph.SpatialNetwork.Routes
            .OrderBy(candidate => candidate.RuntimeId, System.StringComparer.Ordinal)
            .FirstOrDefault(candidate => candidate?.Origin != null
                && candidate.Destination != null
                && string.Equals(candidate.Origin.RuntimeId, originCity.LocationRuntimeId,
                    System.StringComparison.Ordinal)
                && graph.Cities.Any(city => city != null
                    && !ReferenceEquals(city, originCity)
                    && string.Equals(city.LocationRuntimeId, candidate.Destination.RuntimeId,
                        System.StringComparison.Ordinal)));
        Assert.That(route, Is.Not.Null,
            "The fixture needs a composed direct route from the member's exact origin City to another Core City.");
        DailyV1CityRuntimeOwners destinationCity = graph.Cities.Single(city => city != null
            && string.Equals(city.LocationRuntimeId, route.Destination.RuntimeId,
                System.StringComparison.Ordinal));
        Assert.That(destinationCity, Is.Not.Null,
            "The fixture needs a distinct Core-owned city for an in-progress party arrival.");
        bool destinationWasAlreadyKnown =
            member.SpatialKnowledge.KnowsLocation(destinationCity.LocationRuntimeId);
        long initialTravelRevision = member.Travel.Revision;
        long initialSpatialRevision = member.SpatialKnowledge.Revision;
        int initialKnownLocationCount = member.SpatialKnowledge.KnownLocationRuntimeIds.Count;
        int initialDestinationPresenceCardinality =
            destinationCity.NpcPresence.OrderedNpcRuntimeIds.Count;
        long initialDestinationPresenceRevision = destinationCity.NpcPresence.Revision;
        string partyId = "detached-core-travel-party";
        string routeId = route.RuntimeId;
        string decisionId = "detached-core-travel-decision";
        TravelPartyRuntime party = new TravelPartyRuntime(
            partyId,
            originCity.LocationRuntimeId,
            destinationCity.LocationRuntimeId,
            routeId,
            new[] { memberRuntimeId },
            System.Array.Empty<string>(),
            route.TravelDays,
            decisionId,
            new[] { new TravelPartyMemberCost(memberRuntimeId, 0f) });
        Assert.That(member.Travel.TryInstallState(
            currentLocationRuntimeId: null,
            currentCityRuntimeId: null,
            destinationLocationRuntimeId: destinationCity.LocationRuntimeId,
            destinationCityRuntimeId: destinationCity.RuntimeId,
            travelDaysRemaining: 1,
            travelDaysTotal: route.TravelDays,
            travelRouteRuntimeId: routeId,
            travelStartedToday: false,
            travelOriginDecisionId: decisionId,
            activeTravelPartyId: partyId,
            revision: initialTravelRevision + 1L), Is.True);
        Assert.That(originCity.NpcPresence.TryRemove(memberRuntimeId), Is.True,
            "The fixture begins after the existing travel-start transition removed origin presence.");
        Assert.That(graph.TravelParties.Add(party), Is.True);
        Assert.That(graph.TryValidateRestoredTravelPartyBindings(out string partyBindingDiagnostic),
            Is.True, partyBindingDiagnostic);

        System.Collections.Generic.IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: true,
                hasTravelSystem: false);
        foreach (string operationContractId in operationContractIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True,
                operationContractId);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);

        OwnerSectionCensusSnapshot[] censusSections =
            DailyV1ApplicationSessionGateTests.CreateCoreBoundCompleteOwnerSectionVector(session);
        System.Collections.Generic.List<DailyV1OwnerSectionContractIdentity> contracts =
            new System.Collections.Generic.List<DailyV1OwnerSectionContractIdentity>(censusSections.Length);
        System.Collections.Generic.List<DailyV1OwnerSectionCensusProviderRegistration> providers =
            new System.Collections.Generic.List<DailyV1OwnerSectionCensusProviderRegistration>(censusSections.Length);
        foreach (OwnerSectionCensusSnapshot section in censusSections)
        {
            TravelPartyCensusProviderForCoreTest provider =
                new TravelPartyCensusProviderForCoreTest(
                    section,
                    graph,
                    string.Equals(section.SectionId, TravelPartyCensusProvider.SectionId,
                        System.StringComparison.Ordinal) ? graph.TravelParties : null);
            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                section.SectionId, section.SchemaVersion, section.Role));
            providers.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                section.SectionId, section.SchemaVersion, section.Role, provider,
                section.OwnerInstanceIdentity, section.Cardinality, section.Revision,
                hasBaseline: true));
        }
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
            contracts, providers), Is.True);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        Assert.That(state.TrySealSelectedOwnerSectionCensusRegistry(providers), Is.True);
        Assert.That(state.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);

        Assert.That(DailyV1CoreSelectedTravelDayHostPorts.TryCreate(
            state, graph, out DailyV1CoreSelectedTravelDayHostPorts hostPorts), Is.True);
        Assert.That(hostPorts.Boundary, Is.Not.Null);
        Assert.That(hostPorts.P12Protocol, Is.TypeOf<DailyV1CoreSelectedP12TravelPartyAdvanceProtocol>());
        Assert.That(DailyV1CoreSelectedTravelDayExecutionContext.TryCreate(
            state, graph, hostPorts,
            out IDailyV1SelectedTravelDayExecutionContext context), Is.True);
        Assert.That(context.Boundary, Is.SameAs(hostPorts.Boundary));
        Assert.That(context.P12Protocol, Is.SameAs(hostPorts.P12Protocol));
        Assert.That(session.EventAuthority.TryBindSelectedTravelEventRecorders(state), Is.True);
        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);

        bool dailyAdvanceStarted = false;
        bool dailyAdvanceScopeStarted = true;
        long initialEpoch = state.MutationEpochState.CurrentEpoch;
        string cityPresenceSection = DailyV1SelectedOwnerSectionCensusContract
            .CityImportantNpcSectionIdFor(destinationCity.RuntimeId);
        Assert.That(state.TryNotifyCoreSelectedTravelMutation(
            graph, new[] { cityPresenceSection }), Is.False,
            "A direct travel mutation receipt is rejected outside the admitted daily execution window.");
        try
        {
            dailyAdvanceStarted = state.TryBeginSelectedRuntimeDailyAdvanceOperation();
            Assert.That(dailyAdvanceStarted, Is.True);
            bool decisionMutationBoundariesBound = state.TryBindSelectedCoreDecisionMutationBoundaries(
                graph.RuntimeIdAllocator, graph.RecordSequence);
            Assert.That(decisionMutationBoundariesBound, Is.True,
                $"turn={state.IsSelectedDailyAdvanceCoreTurnActive}; roots={state.SelectedRuntimeExecutionRootsBound}; "
                + $"admitted={state.HasAdmittedSelectedDailyExecution}; fault={state.IsSelectedRuntimeCensusProtocolFaulted}; "
                + $"writer={state.LastCoreDecisionMutationAdmissionRejection}");

            Assert.That(DailyV1SelectedTravelDayStage.TryAdvance(
                graph,
                hostPorts.Boundary,
                state,
                hostPorts.P12Protocol,
                legacyPartyOperation: null,
                advanceSoloTravel: false,
                out System.Collections.Generic.IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> arrivals,
                out System.Collections.Generic.IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults),
                Is.True);
            Assert.That(arrivals, Has.Count.EqualTo(1));
            Assert.That(arrivals[0].Party, Is.SameAs(party));
            Assert.That(arrivals[0].EventRecorded, Is.True);
            Assert.That(session.EventAuthority.EventStore.Events, Has.Count.EqualTo(1),
                "The Core party arrival recorder publishes the actual arrival event.");
            TravelPartyArrivedEvent arrivalEvent =
                session.EventAuthority.EventStore.Events[0] as TravelPartyArrivedEvent;
            Assert.That(arrivalEvent, Is.Not.Null);
            Assert.That(arrivalEvent.EventId, Is.EqualTo("event-000001"));
            Assert.That(arrivalEvent.AbsoluteDay, Is.EqualTo(composition.SimulationTime.AbsoluteDay));
            Assert.That(arrivalEvent.RecordSequence, Is.EqualTo(1L));
            Assert.That(arrivalEvent.TravelPartyId, Is.EqualTo(partyId));
            Assert.That(arrivalEvent.OriginLocationRuntimeId, Is.EqualTo(originCity.LocationRuntimeId));
            Assert.That(arrivalEvent.DestinationLocationRuntimeId, Is.EqualTo(destinationCity.LocationRuntimeId));
            Assert.That(arrivalEvent.RouteRuntimeId, Is.EqualTo(routeId));
            Assert.That(arrivalEvent.TravelDaysTotal, Is.EqualTo(route.TravelDays));
            Assert.That(arrivalEvent.TravelerRuntimeIds, Is.EqualTo(new[] { memberRuntimeId }));
            Assert.That(arrivalEvent.EscortRuntimeIds, Is.Empty);
            Assert.That(arrivalEvent.OriginDecisionId, Is.EqualTo(decisionId));
            Assert.That(arrivalEvent.GetParticipants(), Has.Count.EqualTo(1));
            Assert.That(arrivalEvent.GetParticipants()[0].RuntimeId, Is.EqualTo(memberRuntimeId));
            Assert.That(arrivalEvent.GetParticipants()[0].Role, Is.EqualTo(DomainEventParticipantRole.Actor));
            Assert.That(soloResults, Is.Empty);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                try
                {
                    if (dailyAdvanceScopeStarted)
                        Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
                }
                finally
                {
                    lease.Dispose();
                }
            }
        }

        Assert.That(graph.TravelParties.ActiveParties, Is.Empty);
        Assert.That(graph.TravelParties.Revision, Is.EqualTo(2L));
        Assert.That(member.Travel.ActiveTravelPartyId, Is.Null);
        Assert.That(member.Travel.CurrentCityRuntimeId, Is.EqualTo(destinationCity.RuntimeId));
        Assert.That(member.Travel.Revision, Is.EqualTo(initialTravelRevision + 3L));
        Assert.That(member.SpatialKnowledge.KnowsLocation(destinationCity.LocationRuntimeId), Is.True);
        Assert.That(member.SpatialKnowledge.Revision,
            Is.EqualTo(initialSpatialRevision + (destinationWasAlreadyKnown ? 0L : 1L)));
        Assert.That(member.SpatialKnowledge.KnownLocationRuntimeIds.Count,
            Is.EqualTo(initialKnownLocationCount + (destinationWasAlreadyKnown ? 0 : 1)));
        Assert.That(originCity.NpcPresence.Contains(memberRuntimeId), Is.False);
        Assert.That(destinationCity.NpcPresence.Contains(memberRuntimeId), Is.True);
        Assert.That(destinationCity.NpcPresence.OrderedNpcRuntimeIds.Count,
            Is.EqualTo(initialDestinationPresenceCardinality + 1));
        Assert.That(destinationCity.NpcPresence.Revision,
            Is.EqualTo(initialDestinationPresenceRevision + 1L));
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False,
            state.LastSelectedOwnerSectionCensusRefreshRejection);
        OwnerSectionCensusWitness eventWitness = RuntimeIdAllocatorCensusProvider
            .CreateEventCounterProvider(graph.RuntimeIdAllocator).GetCurrentCensus();
        OwnerSectionCensusWitness sequenceWitness = new SimulationRecordSequenceCensusProvider(
            graph.RecordSequence).GetCurrentCensus();
        TestContext.Progress.WriteLine(
            $"epoch initial={initialEpoch}; final={state.MutationEpochState.CurrentEpoch}; "
            + $"partyRevision={graph.TravelParties.Revision}; eventRevision={eventWitness.Revision}; "
            + $"sequenceRevision={sequenceWitness.Revision}; censusFault={state.IsSelectedRuntimeCensusProtocolFaulted}; "
            + state.LastSelectedOwnerSectionCensusRefreshRejection);
        if (state.MutationEpochState.CurrentEpoch != initialEpoch + 3L)
            throw new System.InvalidOperationException(
                $"epoch initial={initialEpoch}, final={state.MutationEpochState.CurrentEpoch}, "
                + $"partyRevision={graph.TravelParties.Revision}, eventRevision={eventWitness.Revision}, "
                + $"sequenceRevision={sequenceWitness.Revision}, censusFault={state.IsSelectedRuntimeCensusProtocolFaulted}");
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
        string[] travelOperationSections = censusSections
            .Select(section => section.SectionId)
            .Where(sectionId => string.Equals(sectionId, TravelPartyCensusProvider.SectionId,
                    System.StringComparison.Ordinal)
                || sectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                    System.StringComparison.Ordinal)
                || sectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix,
                    System.StringComparison.Ordinal)
                || sectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix,
                    System.StringComparison.Ordinal)
                || sectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix,
                    System.StringComparison.Ordinal)
                || string.Equals(sectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    System.StringComparison.Ordinal)
                || string.Equals(sectionId, SimulationRecordSequenceCensusProvider.SectionId,
                    System.StringComparison.Ordinal))
            .ToArray();
        Assert.That(state.TryValidateCoreSelectedTravelPartyAdvanceOwnerSections(
            graph, travelOperationSections), Is.True,
            "The exact travel operation cut remains current after the production arrival path.");
    }

    [Test]
    public void TravelPartyAdvanceUsesCoreSelectedNpcTravelAndSpatialBaselines()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        long initialTravelRevision = npc.Travel.Revision;
        long initialSpatialRevision = npc.SpatialKnowledge.Revision;
        int initialLocationCount = npc.SpatialKnowledge.KnownLocationRuntimeIds.Count;
        int initialRouteCount = npc.SpatialKnowledge.KnownRouteRuntimeIds.Count;
        string travelSection = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
            12, npc.RuntimeId);
        string locationsSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + npc.RuntimeId;
        string routesSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + npc.RuntimeId;

        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    string[] travelMutation = { travelSection };
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        travelMutation, out bool handled), Is.True);
                    Assert.That(handled, Is.True);
                    Assert.That(AdvanceTravelOwnerRevisionForP12Test(npc.Travel), Is.True);
                    Assert.That(state.TryRecordSelectedP12TravelPartyMutation(
                        travelMutation, out handled), Is.True);
                    Assert.That(handled, Is.True);

                    // The same selected owner can commit multiple transitions
                    // inside one serialized party batch. Each accepted receipt
                    // advances its local Core baseline for the next member step.
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        travelMutation, out handled), Is.True);
                    Assert.That(AdvanceTravelOwnerRevisionForP12Test(npc.Travel), Is.True);
                    Assert.That(state.TryRecordSelectedP12TravelPartyMutation(
                        travelMutation, out handled), Is.True);
                    Assert.That(handled, Is.True);

                    string[] spatialMutation = { locationsSection, routesSection };
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        spatialMutation, out handled), Is.True);
                    Assert.That(handled, Is.True);
                    Assert.That(npc.SpatialKnowledge.DiscoverLocation(
                        "core-travel-party-p12-spatial"), Is.True);
                    Assert.That(state.TryRecordSelectedP12TravelPartyMutation(
                        spatialMutation, out handled), Is.True);
                    Assert.That(handled, Is.True);

                    // The paired census rows share one revision and remain
                    // admitted from the newly acknowledged Core baseline.
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        spatialMutation, out handled), Is.True);
                    Assert.That(handled, Is.True);
                    return true;
                });

            Assert.That(operationResult, Is.True);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(npc.Travel.Revision, Is.EqualTo(initialTravelRevision + 2L));
        Assert.That(npc.SpatialKnowledge.Revision, Is.EqualTo(initialSpatialRevision + 1L));
        Assert.That(npc.SpatialKnowledge.KnownLocationRuntimeIds.Count, Is.EqualTo(initialLocationCount + 1));
        Assert.That(npc.SpatialKnowledge.KnownRouteRuntimeIds.Count, Is.EqualTo(initialRouteCount));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1),
            "Only the initial complete owner-vector validation should use the host unchanged-row protocol.");
        Assert.That(protocol.LastRefreshedMutationSectionIds, Does.Contain(travelSection));
        Assert.That(protocol.LastRefreshedMutationSectionIds, Does.Contain(locationsSection));
        Assert.That(protocol.LastRefreshedMutationSectionIds, Does.Contain(routesSection));
    }

    [Test]
    public void TravelPartyAdvanceUsesCoreCityPresenceBaselineAndTracksMembershipReceipts()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = FindCityWithMember(graph, npc.RuntimeId);
        Assert.That(city, Is.Not.Null);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        string sectionId =
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId);
        int initialCardinality = city.NpcPresence.OrderedNpcRuntimeIds.Count;
        long initialRevision = city.NpcPresence.Revision;

        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out bool handled), Is.True);
                    Assert.That(handled, Is.True);
                    Assert.That(city.NpcPresence.TryRemove(npc.RuntimeId), Is.True);
                    Assert.That(state.TryRecordSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out handled), Is.True);
                    Assert.That(handled, Is.True);

                    // A second membership receipt in the same serialized
                    // operation validates against the baseline just advanced.
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out handled), Is.True);
                    Assert.That(city.NpcPresence.TryAdd(npc.RuntimeId), Is.True);
                    Assert.That(state.TryRecordSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out handled), Is.True);
                    Assert.That(handled, Is.True);
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out handled), Is.True);
                    Assert.That(handled, Is.True);
                    return true;
                });

            Assert.That(operationResult, Is.True);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(city.NpcPresence.OrderedNpcRuntimeIds.Count, Is.EqualTo(initialCardinality));
        Assert.That(city.NpcPresence.Revision, Is.EqualTo(initialRevision + 2L));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1),
            "The complete host vector is validated at operation entry; selected City presence receipts are checked by Core.");
        Assert.That(protocol.LastRefreshedMutationSectionIds, Does.Contain(sectionId));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [Test]
    public void TravelPartyAdvanceRejectsUnrecordedSelectedCityPresenceDrift()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = FindCityWithMember(graph, npc.RuntimeId);
        Assert.That(city, Is.Not.Null);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        string sectionId =
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId);
        bool admissionAllowed = true;
        bool admissionHandled = false;

        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    Assert.That(city.NpcPresence.TryRemove(npc.RuntimeId), Is.True);
                    admissionAllowed = state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out admissionHandled);
                    return admissionAllowed;
                });

            Assert.That(operationResult, Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(admissionHandled, Is.True);
        Assert.That(admissionAllowed, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1),
            "Core rejects changed selected City presence before delegating to the host's unchanged-row check.");
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [Test]
    public void TravelPartyAdvanceRejectsCityPresenceReceiptWithMultipleMembershipRevisions()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = FindCityWithMember(graph, npc.RuntimeId);
        Assert.That(city, Is.Not.Null);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        string sectionId =
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId);
        bool receiptRecorded = true;
        bool receiptHandled = false;

        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out bool handled), Is.True);
                    Assert.That(city.NpcPresence.TryRemove(npc.RuntimeId), Is.True);
                    Assert.That(city.NpcPresence.TryAdd(npc.RuntimeId), Is.True);
                    receiptRecorded = state.TryRecordSelectedP12TravelPartyMutation(
                        new[] { sectionId }, out receiptHandled);
                    return receiptRecorded;
                });

            Assert.That(operationResult, Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(receiptHandled, Is.True);
        Assert.That(receiptRecorded, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [Test]
    public void TravelPartyAdvanceUsesCoreEventAndRecordSequenceReceipts()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RuntimeIdAllocator allocator = graph.RuntimeIdAllocator;
        SimulationRecordSequence sequence = state.SelectedRecordSequence;
        Assert.That(allocator, Is.Not.Null);
        Assert.That(sequence, Is.Not.Null);
        string eventSection = RuntimeIdAllocatorCensusProvider.EventsSectionId;
        string sequenceSection = SimulationRecordSequenceCensusProvider.SectionId;
        long initialEventRevision = RuntimeIdAllocatorCensusProvider
            .CreateEventCounterProvider(allocator).GetCurrentCensus().Revision;
        long initialSequenceRevision = new SimulationRecordSequenceCensusProvider(sequence)
            .GetCurrentCensus().Revision;

        RuntimeIdAllocatorOwnerBridge.BindP12EventIdMutationBoundary(
            allocator,
            () => TryAdmitSelectedTravelPartyMutation(state, eventSection),
            () => RecordSelectedTravelPartyMutation(state, eventSection));
        SimulationRecordSequenceOwnerBridge.BindP12MutationBoundary(
            sequence,
            () => TryAdmitSelectedTravelPartyMutation(state, sequenceSection),
            () => RecordSelectedTravelPartyMutation(state, sequenceSection));

        var eventStore = new DomainEventStore(new HistoryStore(), new HistoryPolicy());
        var recorder = new DomainEventRecorder(
            allocator, state.SimulationTime, sequence, eventStore);
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        bool recorded = ExecuteTravelPartyAdvanceForTest(
            state, graph, protocol,
            () => recorder.Record((eventId, day, record) => new TravelPartyArrivedEvent(
                eventId, day, record, "travel-party-test", "origin", "destination", "route", 1,
                new[] { graph.Npcs[0].RuntimeId }, System.Array.Empty<string>())));

        Assert.That(recorded, Is.True);
        Assert.That(eventStore.Events.Count, Is.EqualTo(1));
        Assert.That(eventStore.Events[0], Is.TypeOf<TravelPartyArrivedEvent>());
        Assert.That(RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(allocator)
            .GetCurrentCensus().Revision, Is.EqualTo(initialEventRevision + 1L));
        Assert.That(new SimulationRecordSequenceCensusProvider(sequence)
            .GetCurrentCensus().Revision, Is.EqualTo(initialSequenceRevision + 1L));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1));
        Assert.That(protocol.LastRefreshedMutationSectionIds, Does.Contain(eventSection));
        Assert.That(protocol.LastRefreshedMutationSectionIds, Does.Contain(sequenceSection));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [TestCase("event")]
    [TestCase("record-sequence")]
    public void TravelPartyAdvanceRejectsUnreceiptedEventIdentityOwnerDrift(string ownerKind)
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RuntimeIdAllocator allocator = graph.RuntimeIdAllocator;
        SimulationRecordSequence sequence = state.SelectedRecordSequence;
        string sectionId = ownerKind == "event"
            ? RuntimeIdAllocatorCensusProvider.EventsSectionId
            : SimulationRecordSequenceCensusProvider.SectionId;
        bool admissionAllowed = true;
        bool admissionHandled = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();

        bool result = ExecuteTravelPartyAdvanceForTest(
            state, graph, protocol,
            () =>
            {
                MutateSelectedTravelPartyIdentityOwnerForTest(
                    ownerKind, allocator, sequence);
                admissionAllowed = state.TryCanCommitSelectedP12TravelPartyMutation(
                    new[] { sectionId }, out admissionHandled);
                return admissionAllowed;
            });

        Assert.That(result, Is.False);
        Assert.That(admissionHandled, Is.True);
        Assert.That(admissionAllowed, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [TestCase("event")]
    [TestCase("record-sequence")]
    public void TravelPartyAdvanceRejectsIdentityOwnerReceiptWithMultipleRevisions(string ownerKind)
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RuntimeIdAllocator allocator = graph.RuntimeIdAllocator;
        SimulationRecordSequence sequence = state.SelectedRecordSequence;
        string sectionId = ownerKind == "event"
            ? RuntimeIdAllocatorCensusProvider.EventsSectionId
            : SimulationRecordSequenceCensusProvider.SectionId;
        bool receiptAllowed = true;
        bool receiptHandled = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();

        bool result = ExecuteTravelPartyAdvanceForTest(
            state, graph, protocol,
            () =>
            {
                Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                    new[] { sectionId }, out bool admissionHandled), Is.True);
                Assert.That(admissionHandled, Is.True);
                MutateSelectedTravelPartyIdentityOwnerForTest(
                    ownerKind, allocator, sequence);
                MutateSelectedTravelPartyIdentityOwnerForTest(
                    ownerKind, allocator, sequence);
                receiptAllowed = state.TryRecordSelectedP12TravelPartyMutation(
                    new[] { sectionId }, out receiptHandled);
                return receiptAllowed;
            });

        Assert.That(result, Is.False);
        Assert.That(receiptHandled, Is.True);
        Assert.That(receiptAllowed, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
    }

    [Test]
    public void TravelPartyAdvanceRejectsUnrecordedSelectedNpcTravelBaselineDrift()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        string travelSection = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
            12, npc.RuntimeId);
        bool admissionAllowed = true;
        bool admissionHandled = false;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    // Simulate a revision write that escaped the scoped P12
                    // receipt. Core must reject it locally before another
                    // host unchanged-row check can accept the stale baseline.
                    Assert.That(AdvanceTravelOwnerRevisionForP12Test(npc.Travel), Is.True);
                    admissionAllowed = state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { travelSection }, out admissionHandled);
                    return admissionAllowed;
                });

            Assert.That(operationResult, Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(admissionHandled, Is.True);
        Assert.That(admissionAllowed, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1),
            "After the initial complete vector, Core must detect its selected travel owner drift locally.");
    }

    [Test]
    public void TravelPartyAdvanceRejectsUnrecordedSelectedSpatialBaselineDrift()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        string locationsSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + npc.RuntimeId;
        string routesSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + npc.RuntimeId;
        bool admissionAllowed = true;
        bool admissionHandled = false;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    // A spatial discovery changes a shared revision represented
                    // by both P12 rows. Without its receipt, the next admission
                    // must reject both rows from Core's prior exact baseline.
                    Assert.That(npc.SpatialKnowledge.DiscoverLocation(
                        "unrecorded-core-spatial-drift"), Is.True);
                    admissionAllowed = state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { locationsSection, routesSection }, out admissionHandled);
                    return admissionAllowed;
                });

            Assert.That(operationResult, Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(admissionHandled, Is.True);
        Assert.That(admissionAllowed, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1),
            "After initial complete-vector validation, Core must detect its selected spatial owner drift locally.");
    }

    [Test]
    public void TravelPartyAdvanceRejectsSpatialMutationReceiptMissingPairedRow()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        string locationsSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + npc.RuntimeId;
        string routesSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + npc.RuntimeId;
        bool receiptRecorded = true;
        bool receiptHandled = false;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { locationsSection, routesSection }, out bool handled), Is.True);
                    Assert.That(handled, Is.True);
                    Assert.That(npc.SpatialKnowledge.DiscoverLocation(
                        "core-spatial-partial-receipt"), Is.True);
                    receiptRecorded = state.TryRecordSelectedP12TravelPartyMutation(
                        new[] { locationsSection }, out receiptHandled);
                    return receiptRecorded;
                });

            Assert.That(operationResult, Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(receiptHandled, Is.True);
        Assert.That(receiptRecorded, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1));
    }

    private static bool AdvanceTravelOwnerRevisionForP12Test(DailyV1NpcTravelOwner travel) =>
        travel != null
        && travel.TryCommitState(
            travel.CurrentLocationRuntimeId,
            travel.CurrentCityRuntimeId,
            travel.DestinationLocationRuntimeId,
            travel.DestinationCityRuntimeId,
            travel.TravelDaysRemaining,
            travel.TravelDaysTotal,
            travel.TravelRouteRuntimeId,
            !travel.TravelStartedToday,
            travel.TravelOriginDecisionId,
            travel.ActiveTravelPartyId,
            travelStateChanged: true);

    [Test]
    public void TravelPartyAdvanceRejectsUnrecordedCoreStoreBaselineDrift()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        Assert.That(graph.Npcs, Is.Not.Empty);
        string memberRuntimeId = graph.Npcs[0].RuntimeId;
        TravelPartyRuntime unrecordedParty = new TravelPartyRuntime(
            "unrecorded-travel-party-drift",
            "origin-location",
            "destination-location",
            "route",
            new[] { memberRuntimeId },
            System.Array.Empty<string>(),
            2,
            null,
            new[] { new TravelPartyMemberCost(memberRuntimeId, 0f) });

        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        bool admissionAllowed = true;
        bool admissionHandled = false;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    // Simulate a store write whose commit notification did
                    // not reach this Core transaction. The next P12 admission
                    // must detect that drift before accepting another write.
                    Assert.That(graph.TravelParties.Add(unrecordedParty), Is.True);
                    admissionAllowed = state.TryCanCommitSelectedP12TravelPartyMutation(
                        new[] { TravelPartyCensusProvider.SectionId }, out admissionHandled);
                    return admissionAllowed;
                });

            Assert.That(operationResult, Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(admissionHandled, Is.True);
        Assert.That(admissionAllowed, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(protocol.UnchangedSectionValidationCalls, Is.EqualTo(1),
            "The stale Core-owned row must be rejected locally after initial full-vector validation.");
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
    }

    [Test]
    public void TravelPartyAdvanceFaultsWhenOneCommitReceiptHidesMultipleCompletions()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        Assert.That(graph.Npcs.Count, Is.GreaterThanOrEqualTo(2));
        string firstMemberRuntimeId = graph.Npcs[0].RuntimeId;
        string secondMemberRuntimeId = graph.Npcs[1].RuntimeId;
        TravelPartyRuntime firstParty = new TravelPartyRuntime(
            "travel-party-double-completion-a", "origin-a", "destination-a", "route-a",
            new[] { firstMemberRuntimeId }, System.Array.Empty<string>(), 2, null,
            new[] { new TravelPartyMemberCost(firstMemberRuntimeId, 0f) });
        TravelPartyRuntime secondParty = new TravelPartyRuntime(
            "travel-party-double-completion-b", "origin-b", "destination-b", "route-b",
            new[] { secondMemberRuntimeId }, System.Array.Empty<string>(), 2, null,
            new[] { new TravelPartyMemberCost(secondMemberRuntimeId, 0f) });
        Assert.That(graph.TravelParties.Add(firstParty), Is.True);
        Assert.That(graph.TravelParties.Add(secondParty), Is.True);

        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        RegisterTravelPartyAdvanceOperations(state);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        bool dailyAdvanceStarted = false;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        bool commitRecorded = true;
        bool commitHandled = false;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);

            bool operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                graph,
                protocol,
                () =>
                {
                    string[] travelPartySection = { TravelPartyCensusProvider.SectionId };
                    Assert.That(state.TryCanCommitSelectedP12TravelPartyMutation(
                        travelPartySection, out bool admissionHandled), Is.True);
                    Assert.That(admissionHandled, Is.True);

                    // Two owner revisions are deliberately presented as one
                    // committed-section receipt. Core must fail closed.
                    Assert.That(graph.TravelParties.Complete(firstParty.TravelPartyId), Is.True);
                    Assert.That(graph.TravelParties.Complete(secondParty.TravelPartyId), Is.True);
                    commitRecorded = state.TryRecordSelectedP12TravelPartyMutation(
                        travelPartySection, out commitHandled);
                    return commitRecorded;
                });

            Assert.That(operationResult, Is.False);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(commitHandled, Is.True);
        Assert.That(commitRecorded, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(graph.TravelParties.ActiveParties, Is.Empty);
        Assert.That(graph.TravelParties.Revision, Is.EqualTo(4L));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
    }

    private static DailyV1CityRuntimeOwners FindCityWithMember(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string npcRuntimeId)
    {
        if (graph?.Cities == null || string.IsNullOrWhiteSpace(npcRuntimeId)) return null;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city?.NpcPresence != null && city.NpcPresence.Contains(npcRuntimeId))
                return city;
        return null;
    }

    private static void RegisterTravelPartyAdvanceOperations(
        DailyV1ExecutionBoundaryState state)
    {
        System.Collections.Generic.IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: true,
                hasTravelSystem: false);
        foreach (string operationContractId in operationContractIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True,
                operationContractId);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
    }

    private static bool ExecuteTravelPartyAdvanceForTest(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph,
        NoOpTravelPartyAdvanceProtocol protocol,
        System.Func<bool> operation)
    {
        RegisterTravelPartyAdvanceOperations(state);
        if (!state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease)) return false;
        bool dailyAdvanceStarted = false;
        bool operationResult = false;
        bool dailyAdvanceEnded = true;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            if (dailyAdvanceStarted)
                operationResult = state.ExecuteSelectedP12TravelPartyAdvance(
                    graph, protocol, operation);
        }
        finally
        {
            try
            {
                if (dailyAdvanceStarted)
                    dailyAdvanceEnded = state.TryEndRuntimeProtocolOperation();
            }
            finally
            {
                lease.Dispose();
            }
        }
        return dailyAdvanceStarted && operationResult && dailyAdvanceEnded;
    }

    private static bool TryAdmitSelectedTravelPartyMutation(
        DailyV1ExecutionBoundaryState state,
        string sectionId)
    {
        bool handled;
        return state.TryCanCommitSelectedP12TravelPartyMutation(
                new[] { sectionId }, out handled)
            && handled;
    }

    private static void RecordSelectedTravelPartyMutation(
        DailyV1ExecutionBoundaryState state,
        string sectionId)
    {
        if (!state.TryRecordSelectedP12TravelPartyMutation(
                new[] { sectionId }, out bool handled)
            || !handled)
            throw new System.InvalidOperationException(
                "The Core TravelParty mutation receipt was rejected.");
    }

    private static void MutateSelectedTravelPartyIdentityOwnerForTest(
        string ownerKind,
        RuntimeIdAllocator allocator,
        SimulationRecordSequence sequence)
    {
        if (ownerKind == "event")
        {
            allocator.AllocateEventId();
            return;
        }
        sequence.Allocate();
    }

    [Test]
    public void TravelPartyAdvanceRejectsWhenCoreOperationInventoryIsNotSealed()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        var protocol = new NoOpTravelPartyAdvanceProtocol();
        bool callbackInvoked = false;
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        try
        {
            Assert.That(state.ExecuteSelectedP12TravelPartyAdvance(
                composition.OwnerGraph,
                protocol,
                () => { callbackInvoked = true; return true; }), Is.False);
        }
        finally
        {
            lease.Dispose();
        }

        Assert.That(callbackInvoked, Is.False);
        Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
    }

    [Test]
    public void TravelPartyAdvanceRejectsWhileMutationEpochReservationIsActive()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        System.Collections.Generic.IReadOnlyList<string> operationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true,
                hasTravelPartySystem: true,
                hasTravelSystem: false);
        foreach (string operationContractId in operationContractIds)
            Assert.That(state.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True,
                operationContractId);
        Assert.That(state.TrySealRuntimeProtocolOperationInventory(), Is.True);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);

        bool dailyAdvanceStarted = false;
        bool reservationReleased = false;
        DailyV1MutationEpochReservation reservation = null;
        try
        {
            dailyAdvanceStarted = state.TryBeginRuntimeProtocolOperation(DailyV1RuntimeProtocolOperationContracts.DailyAdvance);
            Assert.That(dailyAdvanceStarted, Is.True);
            Assert.That(state.MutationEpochState.TryReserve(out reservation), Is.True);
            Assert.That(reservation, Is.Not.Null);

            int operationCountBefore = state.ActiveRuntimeOperationCount;
            var protocol = new NoOpTravelPartyAdvanceProtocol();
            bool callbackInvoked = false;
            bool result = state.ExecuteSelectedP12TravelPartyAdvance(
                composition.OwnerGraph,
                protocol,
                () => { callbackInvoked = true; return true; });

            Assert.That(result, Is.False);
            Assert.That(callbackInvoked, Is.False,
                "The operation callback must not run while the shared mutation epoch has a reserved commit.");
            Assert.That(state.ActiveRuntimeOperationCount, Is.EqualTo(operationCountBefore),
                "Rejected admission must leave the enclosing daily scope unchanged.");
            Assert.That(protocol.FaultClosedCalls, Is.EqualTo(1));
            Assert.That(state.MutationEpochState.HasActiveReservation, Is.True,
                "Rejecting the operation must not consume another operation's reservation.");

            reservationReleased = state.MutationEpochState.TryRelease(reservation);
            Assert.That(reservationReleased, Is.True);
            Assert.That(state.ActiveRuntimeOperationCount, Is.EqualTo(operationCountBefore));
        }
        finally
        {
            if (reservation != null && !reservationReleased)
                Assert.That(state.MutationEpochState.TryRelease(reservation), Is.True);
            try
            {
                if (dailyAdvanceStarted)
                    Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
            finally
            {
                lease.Dispose();
            }
        }

        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.TryAssessCaptureScope(DailyV1CaptureScopeAllowance.None), Is.True);
    }

    [Test]
    public void AdmittedCoreSessionRecordsCommittedSoloArrivalWithItsExactClockAndOwners()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners origin = null;
        DailyV1CityRuntimeOwners destination = null;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (string.Equals(city.RuntimeId, npc.Travel.CurrentCityRuntimeId,
                    System.StringComparison.Ordinal))
                origin = city;
            else if (destination == null)
                destination = city;
        }
        Assert.That(origin, Is.Not.Null);
        Assert.That(destination, Is.Not.Null);

        const string decisionId = "core-selected-solo-arrival-decision";
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, origin.LocationRuntimeId,
            destination.LocationRuntimeId, destination,
            requestedTravelDays: 1, originDecisionId: decisionId,
            routeId: "core-selected-solo-arrival-route",
            out DailyV1SelectedTravelTransitionPlan startPlan), Is.True);
        Assert.That(graph.TryCommitTravelTransition(npc, startPlan, null), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryClearTravelStartedToday(
            npc, out DailyV1SelectedTravelTransitionPlan clearPlan), Is.True);
        Assert.That(graph.TryCommitTravelTransition(npc, clearPlan, null), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
            npc, destination, out DailyV1SelectedTravelTransitionPlan arrivalPlan,
            out bool arriving), Is.True);
        Assert.That(arriving, Is.True);
        Assert.That(graph.TryCommitTravelTransition(npc, arrivalPlan, null), Is.True);
        npc.SpatialKnowledge.DiscoverLocation(npc.Travel.CurrentLocationRuntimeId);

        DailyV1SelectedSoloTravelDayResult result =
            new DailyV1SelectedSoloTravelDayResult(
                npc, arrivalPlan, shouldReport: true, wasCommitted: true,
                arrived: true, decisionId);
        DailyV1DomainEventAuthority authority = composition.EventAuthority;
        HistoryStore history = authority.History;
        DomainEventStore store = authority.EventStore;
        DomainEventRecorder wrongClockRecorder = new DomainEventRecorder(
            composition.RuntimeIdAllocator, new SimulationTime(0L),
            composition.RecordSequence, store);
        DomainEventRecorder exactRecorder = authority.Recorder;

        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedTravelEventRecorders(
            wrongClockRecorder, null), Is.False,
            "A recorder that shares allocator/sequence but substitutes the session clock is rejected.");
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedTravelEventRecorders(
            null, exactRecorder), Is.True);
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedTravelEventRecorders(
            null, exactRecorder), Is.True, "Exact recorder binding is idempotent.");

        Assert.That(composition.ExecutionBoundaryState.TryAcquireAdvanceLease(
            out DailyV1AdvanceLease lease), Is.True);
        try
        {
            Assert.That(composition.ExecutionBoundaryState.TryRecordSelectedSoloTravelArrival(
                result, out bool eventRecorded), Is.True);
            Assert.That(eventRecorded, Is.True);
        }
        finally
        {
            lease.Dispose();
        }

        Assert.That(store.Events, Has.Count.EqualTo(1));
        NpcArrivedEvent recorded = store.Events[0] as NpcArrivedEvent;
        Assert.That(recorded, Is.Not.Null);
        Assert.That(recorded.ActorRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(recorded.DestinationLocationRuntimeId,
            Is.EqualTo(npc.Travel.CurrentLocationRuntimeId));
        Assert.That(recorded.OriginDecisionId, Is.EqualTo(decisionId));
        Assert.That(recorded.AbsoluteDay, Is.EqualTo(composition.SimulationTime.AbsoluteDay));
        Assert.That(recorded.EventId, Is.EqualTo("event-000001"));
        Assert.That(recorded.RecordSequence, Is.EqualTo(1L));
        Assert.That(composition.RecordSequence.CaptureSnapshot().NextSequence, Is.EqualTo(2L));
    }


    private sealed class NoOpTravelPartyAdvanceProtocol
        : IDailyV1SelectedP12TravelPartyAdvanceProtocol
    {
        internal int UnchangedSectionValidationCalls { get; private set; }
        internal string[] LastRefreshedMutationSectionIds { get; private set; }

        public bool TryValidateOwnerSections(
            DailyV1SelectedRuntimeOwnerGraph graph,
            System.Collections.Generic.IReadOnlyList<string> expectedSectionIds) =>
            graph != null && expectedSectionIds != null;

        public bool TryValidateUnchangedSections(
            System.Collections.Generic.IReadOnlyList<string> sectionIds)
        {
            UnchangedSectionValidationCalls++;
            return sectionIds != null;
        }

        public bool TryRefreshCommittedMutationBaselines(
            System.Collections.Generic.IReadOnlyCollection<string> sectionIds)
        {
            LastRefreshedMutationSectionIds = sectionIds?.ToArray();
            return sectionIds != null;
        }

        public bool NotifyCommittedMutations(
            System.Collections.Generic.IReadOnlyCollection<string> sectionIds)
        {
            LastRefreshedMutationSectionIds = sectionIds?.ToArray();
            return sectionIds != null;
        }

        public int FaultClosedCalls { get; private set; }
        public void FaultClosed() => FaultClosedCalls++;
    }
}
