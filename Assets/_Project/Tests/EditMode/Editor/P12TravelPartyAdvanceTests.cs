using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class P12TravelPartyAdvanceTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void TravelStateProviderUsesExactOnePerNpcOwnersAndOrdinalSections()
    {
        NpcRuntime zulu = new NpcRuntime("travel-state-zulu", null);
        NpcRuntime alpha = new NpcRuntime("travel-state-alpha", null);

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            NpcTravelStateCensusProvider.CreateProviders(new[] { zulu, alpha });

        Assert.That(providers.Select(provider => provider.GetCurrentCensus().SectionId), Is.EqualTo(new[]
        {
            NpcTravelStateCensusProvider.SectionIdFor(alpha.RuntimeId),
            NpcTravelStateCensusProvider.SectionIdFor(zulu.RuntimeId)
        }));
        foreach (NpcTravelStateCensusProvider provider in providers.Cast<NpcTravelStateCensusProvider>())
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            Assert.That(witness.SchemaVersion, Is.EqualTo(NpcTravelStateCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(provider.NpcOwner));
            Assert.That(witness.Cardinality, Is.EqualTo(1));
            Assert.That(witness.Revision, Is.EqualTo(provider.NpcOwner.TravelStateRevision));
        }

        Assert.Throws<ArgumentException>(() => NpcTravelStateCensusProvider.CreateProviders(new[] { alpha, alpha }));
        Assert.Throws<ArgumentException>(() => NpcTravelStateCensusProvider.CreateProviders(new[]
        {
            alpha,
            new NpcRuntime(alpha.RuntimeId, null)
        }));
    }

    [Test]
    public void TravelProgressMutatorsIncrementOnceForCommitsAndRejectAtSaturation()
    {
        CityRuntime origin = SimulationTestFactory.CreateCity("travel-state-origin", "travel-state-origin-location");
        CityRuntime destination = SimulationTestFactory.CreateCity("travel-state-destination", "travel-state-destination-location");
        NpcRuntime npc = new NpcRuntime("travel-state-mutator", null, origin, 0f);
        NpcTravelStateCensusProvider provider = new NpcTravelStateCensusProvider(npc);

        Assert.That(npc.AdvanceTravelDay(out _), Is.False);
        Assert.That(npc.CancelTravel(origin), Is.False);
        Assert.That(npc.ClearTravelStartedToday(), Is.True);
        Assert.That(npc.TravelStateRevision, Is.Zero);

        Assert.That(npc.StartTravel(destination, 3), Is.True);
        Assert.That(npc.TravelStateRevision, Is.EqualTo(1));
        Assert.That(npc.SetActiveTravelPartyId("party-one"), Is.True);
        Assert.That(npc.TravelStateRevision, Is.EqualTo(2));
        Assert.That(npc.SetActiveTravelPartyId("party-one"), Is.True);
        Assert.That(npc.TravelStateRevision, Is.EqualTo(2), "setting the same party identity is a no-op");
        Assert.That(npc.ClearTravelStartedToday(), Is.True);
        Assert.That(npc.TravelStateRevision, Is.EqualTo(3));
        Assert.That(npc.ClearTravelStartedToday(), Is.True);
        Assert.That(npc.TravelStateRevision, Is.EqualTo(3), "clearing an already-clear start flag is a no-op");
        Assert.That(npc.AdvanceTravelDay(out _), Is.False);
        Assert.That(npc.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(npc.TravelStateRevision, Is.EqualTo(4), "an intermediate decrement changes travel state");
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(4));
        Assert.That(npc.CancelTravel(origin), Is.True);
        Assert.That(npc.IsTraveling, Is.False);
        Assert.That(npc.CurrentCity, Is.SameAs(origin));
        Assert.That(npc.TravelStateRevision, Is.EqualTo(5));

        NpcRuntime saturatedStart = new NpcRuntime("travel-state-saturated-start", null, origin, 0f);
        SetTravelStateRevision(saturatedStart, long.MaxValue);
        Assert.That(saturatedStart.StartTravel(destination, 1), Is.False);
        Assert.That(saturatedStart.CurrentCity, Is.SameAs(origin));
        Assert.That(saturatedStart.IsTraveling, Is.False);
        Assert.That(origin.ImportantNpcs, Does.Contain(saturatedStart));

        NpcRuntime saturatedAdvance = new NpcRuntime("travel-state-saturated-advance", null, origin, 0f);
        Assert.That(saturatedAdvance.StartTravel(destination, 2), Is.True);
        SetTravelStateRevision(saturatedAdvance, long.MaxValue);
        Assert.That(saturatedAdvance.AdvanceTravelDay(out _), Is.False);
        Assert.That(saturatedAdvance.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(saturatedAdvance.CancelTravel(origin), Is.False);
        Assert.That(saturatedAdvance.IsTraveling, Is.True);
        Assert.That(saturatedAdvance.CurrentCity, Is.Null);
    }

    [Test]
    public void GroupStartReservesTravelRevisionCapacityBeforeAnyMemberOrEconomyWrite()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SetTravelStateRevision(fixture.Caio, long.MaxValue - 3L);
        float[] balances = fixture.Members.Select(member => member.Money).ToArray();

        Assert.That(fixture.System.TryStartTravelParty(CreatePartyContext(fixture), out _), Is.False);

        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Members.All(member => !member.IsTraveling
            && member.ActiveTravelPartyId == null
            && member.CurrentCity == fixture.World.A), Is.True);
        Assert.That(fixture.Members.Select(member => member.Money).ToArray(), Is.EqualTo(balances));
        Assert.That(fixture.Records.Events.Events, Is.Empty);
    }

    [Test]
    public void FailedGroupStartCompensatesWithinPreflightedTravelRevisionCapacity()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SetTravelStateRevision(fixture.Caio, long.MaxValue - 4L);
        typeof(SimulationRecordSequence).GetField("nextSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(fixture.Records.Sequence, long.MaxValue);
        float[] balances = fixture.Members.Select(member => member.Money).ToArray();

        LogAssert.Expect(UnityEngine.LogType.Error,
            "Cannot allocate domain EventId: Simulation record sequence is exhausted.");
        Assert.That(fixture.System.TryStartTravelParty(CreatePartyContext(fixture), out _), Is.False);

        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Parties.Revision, Is.EqualTo(2), "the failed attempt's add and compensation removal remain visible");
        Assert.That(fixture.Members.All(member => !member.IsTraveling
            && member.ActiveTravelPartyId == null
            && member.CurrentCity == fixture.World.A), Is.True);
        Assert.That(fixture.Members.Select(member => member.Money).ToArray(), Is.EqualTo(balances));
        Assert.That(fixture.Caio.TravelStateRevision, Is.EqualTo(long.MaxValue));
        Assert.That(fixture.Records.Events.Events, Is.Empty);
    }

    [Test]
    public void SynchronizedStartDayClearPreflightsAllMemberTravelRevisions()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(routeTravelDays: 1);
        Assert.That(fixture.System.TryStartTravelParty(CreatePartyContext(fixture), out _), Is.True);
        SetTravelStateRevision(fixture.Caio, long.MaxValue);

        Assert.That(fixture.System.AdvanceParties(), Is.Empty);

        Assert.That(fixture.Members.All(member => member.TravelStartedToday), Is.True,
            "one saturated member prevents partial synchronized start-day clearing");
        Assert.That(fixture.Parties.ActiveParties, Has.Count.EqualTo(1));
    }

    [Test]
    public void FinalArrivalPreflightsAllMemberTravelRevisionsBeforeChangingAnyOwner()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(routeTravelDays: 1);
        Assert.That(fixture.System.TryStartTravelParty(CreatePartyContext(fixture), out _), Is.True);
        Assert.That(fixture.System.AdvanceParties(), Is.Empty, "the start-day flag is cleared as one synchronized group");
        SetTravelStateRevision(fixture.Caio, long.MaxValue - 1L);
        int eventCount = fixture.Records.Events.Events.Count;
        long storeRevision = fixture.Parties.Revision;

        Assert.That(fixture.System.AdvanceParties(), Is.Empty);

        Assert.That(fixture.Members.All(member => member.IsTraveling && member.TravelDaysRemaining == 1), Is.True,
            "one member lacking both arrival revisions prevents every member's progress");
        Assert.That(fixture.World.B.ImportantNpcs, Is.Empty);
        Assert.That(fixture.Parties.Revision, Is.EqualTo(storeRevision));
        Assert.That(fixture.Records.Events.Events, Has.Count.EqualTo(eventCount));
    }

    [Test]
    public void SelectedDailyRosterReconciliationTracksCityPresenceAndSameIdReplacement()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("travel-state-roster-city", "travel-state-roster-location");
        NpcRuntime initial = new NpcRuntime("travel-state-roster-initial", null, city, 0f);
        SimulationRuntime runtime = CreateDailyRuntime(new[] { city }, new[] { initial });

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialFailure), Is.True,
            initialFailure.ToString());
        long cityRevisionBeforeUnregisteredAdd = city.ImportantNpcRevision;
        NpcRuntime firstOwner = new NpcRuntime("travel-state-roster-dynamic", null, city, 0f);
        Assert.That(city.ImportantNpcRevision, Is.EqualTo(cityRevisionBeforeUnregisteredAdd + 1));
        long cityRevisionBeforeRegistration = city.ImportantNpcRevision;
        Assert.That(runtime.TryRegisterNpc(firstOwner, out WorldNpcRegistryFailure addFailure), Is.True,
            addFailure.ToString());
        Assert.That(city.ImportantNpcRevision, Is.EqualTo(cityRevisionBeforeRegistration));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterAddFailure), Is.True,
            afterAddFailure.ToString());
        Assert.That(GetTravelStateProvider(runtime, firstOwner.RuntimeId).NpcOwner, Is.SameAs(firstOwner));

        Assert.That(runtime.TryUnregisterNpc(firstOwner.RuntimeId, out WorldNpcRegistryFailure removeFailure), Is.True,
            removeFailure.ToString());
        Assert.That(firstOwner.CurrentCity, Is.Null);
        Assert.That(city.ImportantNpcs.Any(npc => ReferenceEquals(npc, firstOwner)), Is.False);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterRemoveFailure), Is.True,
            afterRemoveFailure.ToString());
        Assert.That(GetTravelStateProvider(runtime, firstOwner.RuntimeId), Is.Null);

        NpcRuntime replacement = new NpcRuntime(firstOwner.RuntimeId, null, city, 0f);
        Assert.That(runtime.TryRegisterNpc(replacement, out WorldNpcRegistryFailure replacementFailure), Is.True,
            replacementFailure.ToString());
        Assert.That(GetTravelStateProvider(runtime, replacement.RuntimeId).NpcOwner, Is.SameAs(replacement));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterReplacementFailure), Is.True,
            afterReplacementFailure.ToString());

        NpcRuntime duplicate = new NpcRuntime(replacement.RuntimeId, null);
        Assert.That(runtime.TryRegisterNpc(duplicate, out WorldNpcRegistryFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(WorldNpcRegistryFailure.DuplicateRuntimeId));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterDuplicateFailure), Is.True,
            afterDuplicateFailure.ToString());
    }

    [Test]
    public void DailyTravelPartyAdvanceBatchesOwnersAndSequenceUnderNestedOperation()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(routeTravelDays: 1);
        Assert.That(fixture.System.TryStartTravelParty(CreatePartyContext(fixture), out _), Is.True);
        SimulationRuntime runtime = CreateDailyRuntime(
            new[] { fixture.World.A, fixture.World.B, fixture.World.C },
            fixture.Members,
            fixture.Records.Time,
            fixture.Travel,
            fixture.System,
            fixture.Records.Sequence,
            fixture.Records.Allocator);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        List<int> activeOperationsAtTravelOwnerCommit = new List<int>();
        WrapNpcTravelCommitted(fixture.Bruno, () =>
        {
            Assert.That(protocol.TryReadActiveOperationCount(out int active, out ContinuationCensusFailure failure), Is.True,
                failure.ToString());
            activeOperationsAtTravelOwnerCommit.Add(active);
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure busy), Is.False);
            Assert.That(busy, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));
        });

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long beforeStartDayClear, out ContinuationCensusFailure readBefore),
            Is.True, readBefore.ToString());
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(fixture.Members.All(member => !member.TravelStartedToday), Is.True);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterStartDayClear, out ContinuationCensusFailure readAfter),
            Is.True, readAfter.ToString());
        Assert.That(afterStartDayClear, Is.EqualTo(beforeStartDayClear + 1));
        Assert.That(activeOperationsAtTravelOwnerCommit, Is.All.EqualTo(2), "the nested TravelParty scope remains inside runtime.advance-day");

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long beforeArrival, out ContinuationCensusFailure readBeforeArrival),
            Is.True, readBeforeArrival.ToString());
        IOwnerSectionCensusProvider eventCounterProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(fixture.Records.Allocator);
        long eventCounterRevisionBeforeArrival = eventCounterProvider.GetCurrentCensus().Revision;
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure arrivalFailure), Is.True, arrivalFailure.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterArrival, out ContinuationCensusFailure readAfterArrival),
            Is.True, readAfterArrival.ToString());
        Assert.That(afterArrival, Is.EqualTo(beforeArrival + 1),
            "TravelParty, City, NPC, SpatialKnowledge, Event-counter, and record-sequence writes share one nested operation epoch");
        Assert.That(eventCounterProvider.GetCurrentCensus().Revision, Is.EqualTo(eventCounterRevisionBeforeArrival + 1),
            "the arrival event ID allocation is included in the selected allocator census");
        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Members.All(member => member.CurrentCity == fixture.World.B
            && member.ActiveTravelPartyId == null && !member.IsTraveling), Is.True);
        Assert.That(fixture.World.B.ImportantNpcs.Count, Is.EqualTo(fixture.Members.Count));
        Assert.That(fixture.Records.Events.Events.Last().EventType, Is.EqualTo(DomainEventType.TravelPartyArrived));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());

        long stableEpoch = afterArrival;
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure idleFailure), Is.True, idleFailure.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterIdle, out ContinuationCensusFailure readIdle),
            Is.True, readIdle.ToString());
        Assert.That(afterIdle, Is.EqualTo(stableEpoch), "an empty/no-op TravelParty advance does not notify a mutation epoch");
    }

    [Test]
    public void TravelPartyPreflightFailureDoesNotMutatePartyMembers()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(routeTravelDays: 3);
        Assert.That(fixture.System.TryStartTravelParty(CreatePartyContext(fixture), out _), Is.True);
        SimulationRuntime runtime = CreateDailyRuntime(
            new[] { fixture.World.A, fixture.World.B, fixture.World.C },
            fixture.Members,
            fixture.Records.Time,
            fixture.Travel,
            fixture.System,
            fixture.Records.Sequence);
        int beforeDays = fixture.Bruno.TravelDaysRemaining;
        long beforePartyRevision = fixture.Parties.Revision;
        SetTravelStateRevision(fixture.Caio, fixture.Caio.TravelStateRevision + 1);

        Assert.Throws<InvalidOperationException>(() => runtime.TryAdvanceDay(out _));

        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(beforeDays));
        Assert.That(fixture.Caio.TravelDaysRemaining, Is.EqualTo(beforeDays));
        Assert.That(fixture.Parties.Revision, Is.EqualTo(beforePartyRevision));
        Assert.That(fixture.Parties.ActiveParties, Has.Count.EqualTo(1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void TravelPartyExceptionReportsEarlierMemberProgressBeforeFaultingDailyRuntime()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(routeTravelDays: 3);
        Assert.That(fixture.System.TryStartTravelParty(CreatePartyContext(fixture), out _), Is.True);
        fixture.System.AdvanceParties(); // Clear the start-day flags before the runtime installs its baselines.
        SimulationRuntime runtime = CreateDailyRuntime(
            new[] { fixture.World.A, fixture.World.B, fixture.World.C },
            fixture.Members,
            fixture.Records.Time,
            fixture.Travel,
            fixture.System,
            fixture.Records.Sequence);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        long beforeEpoch = ReadProtocolEpoch(protocol);
        WrapNpcTravelAdmissionToThrow(fixture.Caio);

        Assert.Throws<InvalidOperationException>(() => runtime.TryAdvanceDay(out _));

        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(2), "the first member's committed progress is preserved");
        Assert.That(fixture.Caio.TravelDaysRemaining, Is.EqualTo(3), "the later member fails before its own mutation");
        Assert.That(fixture.Parties.ActiveParties, Has.Count.EqualTo(1));
        Assert.That(ReadProtocolEpoch(protocol), Is.EqualTo(beforeEpoch + 1),
            "the nested scope reports partial progress before the outer daily operation faults closed");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static SimulationRuntime CreateDailyRuntime(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        SimulationTime time = null,
        TravelSystem travelSystem = null,
        TravelPartySystem travelPartySystem = null,
        SimulationRecordSequence sequence = null,
        RuntimeIdAllocator runtimeIdAllocator = null)
    {
        SimulationRuntime runtime = new SimulationRuntime(
            time ?? new SimulationTime(),
            cities,
            npcs,
            economyEnabled: false,
            configuredActions: Array.Empty<NpcActionData>(),
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            travelSystem: travelSystem,
            travelPartySystem: travelPartySystem,
            recordSequence: sequence,
            runtimeIdAllocator: runtimeIdAllocator,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(System.Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        return runtime;
    }

    private static ActionExecutionContext CreatePartyContext(TravelPartyFixture fixture)
    {
        return new ActionExecutionContext("p12-travel-party-advance", new[]
        {
            new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
            new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer),
            new ActionExecutionParticipant(fixture.Marta.RuntimeId, ActionExecutionParticipantRole.Support)
        }, fixture.World.B.Location.RuntimeId, fixture.World.RouteAB.RuntimeId);
    }

    private static NpcTravelStateCensusProvider GetTravelStateProvider(SimulationRuntime runtime, string runtimeId)
    {
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        return protocol.NpcTravelStateFamilyProviders
            .Cast<NpcTravelStateCensusProvider>()
            .SingleOrDefault(provider => string.Equals(provider.RuntimeId, runtimeId, StringComparison.Ordinal));
    }

    private static ContinuationCensusProtocol GetProtocol(SimulationRuntime runtime)
    {
        return (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
    }

    private static long ReadProtocolEpoch(ContinuationCensusProtocol protocol)
    {
        return (long)typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(protocol);
    }

    private static void SetTravelStateRevision(NpcRuntime npc, long revision)
    {
        typeof(NpcRuntime).GetField("travelStateRevision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(npc, revision);
    }

    private static void WrapNpcTravelCommitted(NpcRuntime npc, Action observe)
    {
        FieldInfo field = typeof(NpcRuntime).GetField(
            "p12TravelStateMutationCommitted", BindingFlags.Instance | BindingFlags.NonPublic);
        Action<bool, IReadOnlyList<CityRuntime>> original =
            (Action<bool, IReadOnlyList<CityRuntime>>)field.GetValue(npc);
        field.SetValue(npc, (Action<bool, IReadOnlyList<CityRuntime>>)((changed, cities) =>
        {
            observe();
            original?.Invoke(changed, cities);
        }));
    }

    private static void WrapNpcTravelAdmissionToThrow(NpcRuntime npc)
    {
        FieldInfo field = typeof(NpcRuntime).GetField(
            "p12TravelStateMutationAdmission", BindingFlags.Instance | BindingFlags.NonPublic);
        Func<bool, IReadOnlyList<CityRuntime>, bool> original =
            (Func<bool, IReadOnlyList<CityRuntime>, bool>)field.GetValue(npc);
        field.SetValue(npc, (Func<bool, IReadOnlyList<CityRuntime>, bool>)((changed, cities) =>
        {
            if (changed) throw new InvalidOperationException("injected later TravelParty member failure");
            return original == null || original(changed, cities);
        }));
    }
}
