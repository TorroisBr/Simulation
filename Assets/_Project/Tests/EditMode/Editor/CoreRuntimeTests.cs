using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

public sealed class CoreRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void RuntimeIdAllocator_UsesUniqueTypedNamespaces()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        string npcId = allocator.AllocateNpcId();
        string cityId = allocator.AllocateCityId();
        string decisionId = allocator.AllocateDecisionId();
        string eventId = allocator.AllocateEventId();

        Assert.That(npcId, Is.EqualTo("npc-000001"));
        Assert.That(cityId, Is.EqualTo("city-000001"));
        Assert.That(decisionId, Is.EqualTo("decision-000001"));
        Assert.That(eventId, Is.EqualTo("event-000001"));
        Assert.That(new[] { npcId, cityId, decisionId, eventId }, Is.Unique);
    }

    [Test]
    public void RecordSequence_SharesOneMonotonicSequenceAcrossDecisionsAndEvents()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        SimulationRecordSequenceCensusProvider provider = new SimulationRecordSequenceCensusProvider(fixture.Sequence);
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();

        Assert.That(initial.SectionId, Is.EqualTo(SimulationRecordSequenceCensusProvider.SectionId));
        Assert.That(initial.SchemaVersion, Is.EqualTo(SimulationRecordSequenceCensusProvider.SchemaVersion));
        Assert.That(initial.OwnerInstanceIdentity, Is.Not.SameAs(fixture.Sequence));
        Assert.That(initial.OwnerInstanceIdentity, Is.Not.InstanceOf<SimulationRecordSequence>());
        Assert.That(initial.Cardinality, Is.EqualTo(1));
        Assert.That(initial.Revision, Is.Zero);

        Assert.That(fixture.Sequence.Allocate(), Is.EqualTo(1L));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(fixture.Sequence.Allocate(), Is.EqualTo(2L));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(2L));
        Assert.That(fixture.Sequence.Allocate(), Is.EqualTo(3L));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(3L));
        Assert.That(new SimulationRecordSequenceCensusProvider(fixture.Sequence)
            .GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(initial.OwnerInstanceIdentity));
    }

    [Test]
    public void DecisionAndEventRecordersShareTheWitnessedSequenceOwner()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        SimulationRecordSequenceCensusProvider provider = new SimulationRecordSequenceCensusProvider(fixture.Sequence);

        NpcDecisionRecord decision = fixture.DecisionRecorder.Record(
            "actor", NpcDecisionType.Action, NpcDecisionOrigin.Autonomous,
            "action", null, null, null);
        Assert.That(decision, Is.Not.Null);
        Assert.That(decision.RecordSequence, Is.EqualTo(1L));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1L));

        Assert.That(fixture.EventRecorder.Record((eventId, day, sequence) =>
            new NpcArrivedEvent(eventId, day, sequence, "actor", "location")), Is.True);
        Assert.That(fixture.Events.Events.Count, Is.EqualTo(1));
        Assert.That(fixture.Events.Events[0].RecordSequence, Is.EqualTo(2L));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(2L));
    }

    [Test]
    public void DecisionOccurrenceReceiptFailureUsesDefaultRecorderLoggerWhenOmitted()
    {
        RuntimeIdAllocator runtimeIds = new RuntimeIdAllocator();
        SimulationTime simulationTime = new SimulationTime();
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        FieldInfo nextSequenceField = typeof(SimulationRecordSequence).GetField(
            "nextSequence", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(nextSequenceField, Is.Not.Null);
        nextSequenceField.SetValue(sequence, long.MaxValue);
        NpcDecisionStore decisions = new NpcDecisionStore();
        NpcDecisionRecorder recorder = new NpcDecisionRecorder(
            runtimeIds, simulationTime, sequence, decisions);

        LogAssert.Expect(LogType.Error,
            "Cannot allocate NPC decision record identity: Simulation record sequence is exhausted.");
        Assert.That(recorder.TryRecordOccurrenceOnce(
            "default-logger-operation",
            "default-logger-fingerprint",
            "npc-default-logger",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action-default-logger",
            null,
            null,
            out NpcDecisionRecord decision), Is.False);

        Assert.That(decision, Is.Null);
        OwnerSectionCensusWitness receipts = recorder.GetOccurrenceReceiptCensus();
        Assert.That(receipts.Cardinality, Is.Zero);
        Assert.That(receipts.Revision, Is.Zero);
        Assert.That(sequence.CaptureSnapshot().NextSequence, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void FailedRecordConstructionStillAdvancesTheConsumedSequenceRevision()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        SimulationRecordSequenceCensusProvider provider = new SimulationRecordSequenceCensusProvider(fixture.Sequence);
        LogAssert.Expect(LogType.Error, "Cannot create domain event: expected construction failure.");

        Assert.That(fixture.EventRecorder.Record((eventId, day, sequence) =>
            throw new System.ArgumentException("expected construction failure.")), Is.False);
        Assert.That(provider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(fixture.Events.Events, Is.Empty);
    }

    [Test]
    public void ExistingTravelDecisionIdentityDispatchesThroughRecorderWithoutAllocatingRecord()
    {
        CityData cityAsset = SimulationTestFactory.CreateCityData("travel-recorder-city");
        NpcActionData travelAsset = SimulationTestFactory.CreateAction(
            "travel-recorder-action", NpcActionType.Travel);
        NpcData npcAsset = SimulationTestFactory.CreateNpc("travel-recorder-npc");
        npcAsset.acoesPadrao.Add(new NPCDefaultAction { action = travelAsset, baseUtility = 1f });
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(cityAsset);
        config.Npcs.Add(new NpcSimulationConfig { npc = npcAsset, startingCity = cityAsset });
        config.Actions.Add(travelAsset);
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(config.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            config, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator runtimeIds = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(runtimeIds, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(runtimeIds,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actorOwner = builder.Npcs[0];
        DailyV1CityRuntimeOwners cityOwner = builder.Cities[0];
        Assert.That(graph.SpatialNetwork.TryGetLocation(cityOwner.LocationRuntimeId,
            out SpatialLocationRuntime location), Is.True);
        CityRuntime cityProjection = CityRuntime.CreateSelectedDailyV1(
            cityOwner.RuntimeId, cityOwner.Definition, definitions, bindings, cityOwner, location);
        NpcRuntime actor = NpcRuntime.CreateSelectedDailyV1(
            actorOwner.RuntimeId, actorOwner.Definition, definitions, bindings, actorOwner, cityProjection);
        Assert.That(actor.TryBindSelectedRuntimeOwnerGraphProvider(() => graph), Is.True);
        NpcActionRuntime action = new NpcActionRuntime(travelAsset);
        const string existingDecisionId = "decision/from-travel-plan";
        action.SetOriginDecisionId(existingDecisionId);
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();

        Assert.That(fixture.DecisionRecorder.TryRecordAndInstallSelectedAction(
            actor, action, NpcDecisionOrigin.Autonomous,
            out NpcDecisionRecord decision, out bool handled), Is.True);

        Assert.That(handled, Is.True);
        Assert.That(decision, Is.Null);
        Assert.That(action.OriginDecisionId, Is.EqualTo(existingDecisionId));
        Assert.That(fixture.Decisions.Decisions, Is.Empty);
        Assert.That(fixture.Sequence.CaptureSnapshot().NextSequence, Is.EqualTo(1L));
        Assert.That(actorOwner.ActionSlot.Revision, Is.EqualTo(1L));
        Assert.That(actorOwner.ActionSlot.CurrentAction,
            Is.SameAs(definitions.Actions[travelAsset.DefinitionId]));
        Assert.That(actorOwner.ActionSlot.CurrentPayload.OriginDecisionId, Is.EqualTo(existingDecisionId));

        NpcDecisionRecord next = fixture.DecisionRecorder.Record(
            actor.RuntimeId, NpcDecisionType.Action, NpcDecisionOrigin.Autonomous,
            "next-action", null, null, null);
        Assert.That(next.DecisionId, Is.EqualTo("decision-000001"));
        Assert.That(next.RecordSequence, Is.EqualTo(1L));
    }

    [Test]
    public void SequenceExhaustionDoesNotAdvanceTheWitness()
    {
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        SimulationRecordSequenceCensusProvider provider = new SimulationRecordSequenceCensusProvider(sequence);
        typeof(SimulationRecordSequence)
            .GetField("nextSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(sequence, long.MaxValue);
        OwnerSectionCensusWitness exhausted = provider.GetCurrentCensus();

        Assert.That(exhausted.Revision, Is.EqualTo(long.MaxValue - 1L));
        Assert.Throws<System.InvalidOperationException>(() => sequence.Allocate());
        Assert.That(provider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(exhausted.OwnerInstanceIdentity));
        Assert.That(provider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
    }

    [Test]
    public void RuntimeIdentityRegistry_ResolvesNpcAndCity()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-runtime", "location-runtime");
        NpcRuntime npc = new NpcRuntime("npc-runtime", SimulationTestFactory.CreateNpc("npc-definition"), city, 10f);
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();

        Assert.That(registry.RegisterCity(city), Is.True);
        Assert.That(registry.RegisterNpc(npc), Is.True);
        Assert.That(registry.TryGetCity(city.RuntimeId, out CityRuntime resolvedCity), Is.True);
        Assert.That(registry.TryGetNpc(npc.RuntimeId, out NpcRuntime resolvedNpc), Is.True);
        Assert.That(resolvedCity, Is.SameAs(city));
        Assert.That(resolvedNpc, Is.SameAs(npc));
    }

    [Test]
    public void RuntimeIdentityRegistry_RejectsDuplicateRuntimeIdAcrossTypes()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        NpcRuntime npc = new NpcRuntime("duplicate-runtime", SimulationTestFactory.CreateNpc("npc-definition"));
        CityRuntime city = SimulationTestFactory.CreateCity("duplicate-runtime", "location-runtime");

        Assert.That(registry.RegisterNpc(npc), Is.True);
        LogAssert.Expect(
            LogType.Error,
            "Duplicate RuntimeId 'duplicate-runtime' while registering City; it is already registered as NPC.");
        Assert.That(registry.RegisterCity(city), Is.False);
    }

    [Test]
    public void SelectedLocationProjectionCollisionRollsBackRegistryProjectionBeforeRethrow()
    {
        const string runtimeId = "location/selected-projection-collision";
        DailyV1RuntimeIdentityIndex identityIndex = new DailyV1RuntimeIdentityIndex();
        Assert.That(identityIndex.TryStageRegister(runtimeId, DailyV1RuntimeIdentityKind.Location), Is.True);
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry(null, identityIndex);
        SpatialLocationRuntime location = new SpatialLocationRuntime(runtimeId);
        SpatialLocationRuntime preexistingLookup = new SpatialLocationRuntime(runtimeId);
        System.Collections.Generic.Dictionary<string, SpatialLocationRuntime> selectedLocations =
            new System.Collections.Generic.Dictionary<string, SpatialLocationRuntime>
            {
                [runtimeId] = preexistingLookup
            };

        Assert.Throws<System.ArgumentException>(() =>
            registry.TryBindSelectedLocationProjection(location, selectedLocations));
        Assert.That(selectedLocations[runtimeId], Is.SameAs(preexistingLookup));
        LogAssert.Expect(LogType.Warning,
            "Location runtime resolution failed: RuntimeId 'location/selected-projection-collision' is registered as Location, not Location.");
        Assert.That(registry.TryGetLocation(runtimeId, out _), Is.False,
            "The registry projection must be rolled back before the callback exception escapes.");

        Assert.That(identityIndex.TryRemoveStaged(runtimeId, DailyV1RuntimeIdentityKind.Location), Is.True,
            "The caller that staged the shared Core identity retains rollback ownership.");
    }

    [Test]
    public void SimulationTime_AdvanceDayIncrementsAbsoluteDay()
    {
        SimulationTime time = new SimulationTime();

        Assert.That(time.AbsoluteDay, Is.EqualTo(0L));
        time.AdvanceDay();
        time.AdvanceDay();
        Assert.That(time.AbsoluteDay, Is.EqualTo(2L));
    }

    [Test]
    public void Calendar_DefaultFantasyCalendar_UsesOneDayDimensions()
    {
        CalendarDefinition calendar = CalendarDefinition.CreateDefault();

        SimulationDate date = calendar.GetDate(3L);

        Assert.That(date.Year, Is.EqualTo(3L));
        Assert.That(date.Month, Is.EqualTo(1));
        Assert.That(date.WeekOfMonth, Is.EqualTo(1));
        Assert.That(date.DayOfMonth, Is.EqualTo(1));
        Assert.That(date.DayOfWeek, Is.EqualTo(1));
    }

    [Test]
    public void Calendar_CustomFantasyCalendar_TransitionsMonthAndYear()
    {
        CalendarDefinition calendar = new CalendarDefinition(2, 2, 3);

        SimulationDate weekBoundary = calendar.GetDate(4L);
        SimulationDate monthBoundary = calendar.GetDate(6L);
        SimulationDate yearBoundary = calendar.GetDate(12L);

        Assert.That(weekBoundary.WeekOfMonth, Is.EqualTo(2));
        Assert.That(weekBoundary.DayOfMonth, Is.EqualTo(5));
        Assert.That(weekBoundary.DayOfWeek, Is.EqualTo(2));
        Assert.That(monthBoundary.Month, Is.EqualTo(2));
        Assert.That(monthBoundary.DayOfMonth, Is.EqualTo(1));
        Assert.That(monthBoundary.WeekOfMonth, Is.EqualTo(1));
        Assert.That(monthBoundary.DayOfWeek, Is.EqualTo(1));
        Assert.That(yearBoundary.Year, Is.EqualTo(1L));
        Assert.That(yearBoundary.Month, Is.EqualTo(1));
        Assert.That(yearBoundary.DayOfYear, Is.EqualTo(1L));
    }

    [Test]
    public void Calendar_InvalidDefinition_FallsBackToSafeDefault()
    {
        CalendarDefinition invalid = new CalendarDefinition(0, 2, 3);

        CalendarDefinition resolved = CalendarDefinition.CreateValidatedOrDefault(invalid, out string diagnostic);

        Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
        Assert.That(resolved.MonthsPerYear, Is.EqualTo(1));
        Assert.That(resolved.WeeksPerMonth, Is.EqualTo(1));
        Assert.That(resolved.DaysPerWeek, Is.EqualTo(1));
    }

    [Test]
    public void SpatialKnowledge_DiscoverLocationAndRouteAreIdempotent()
    {
        SpatialKnowledgeRuntime knowledge = new SpatialKnowledgeRuntime("npc-owner");

        Assert.That(knowledge.DiscoverLocation("location-a"), Is.True);
        Assert.That(knowledge.DiscoverLocation("location-a"), Is.False);
        Assert.That(knowledge.DiscoverRoute("route-a-b"), Is.True);
        Assert.That(knowledge.DiscoverRoute("route-a-b"), Is.False);
        Assert.That(knowledge.KnowsLocation("location-a"), Is.True);
        Assert.That(knowledge.KnowsRoute("route-a-b"), Is.True);
        Assert.That(knowledge.KnownLocationRuntimeIds.Count, Is.EqualTo(1));
        Assert.That(knowledge.KnownRouteRuntimeIds.Count, Is.EqualTo(1));
    }

    [Test]
    public void NpcRuntimes_HaveIndependentSpatialKnowledge()
    {
        NpcRuntime first = new NpcRuntime("npc-first", SimulationTestFactory.CreateNpc("npc-first-definition"));
        NpcRuntime second = new NpcRuntime("npc-second", SimulationTestFactory.CreateNpc("npc-second-definition"));

        first.SpatialKnowledge.DiscoverLocation("location-a");

        Assert.That(first.SpatialKnowledge.KnowsLocation("location-a"), Is.True);
        Assert.That(second.SpatialKnowledge.KnowsLocation("location-a"), Is.False);
    }

    [Test]
    public void KnownDirectDestinations_ExcludeTruthRoutesUnknownToNpc()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        NpcRuntime npc = new NpcRuntime("npc-perspective", SimulationTestFactory.CreateNpc("npc-definition"), world.A, 100f);
        npc.SpatialKnowledge.DiscoverLocation(world.A.Location.RuntimeId);
        npc.SpatialKnowledge.DiscoverLocation(world.B.Location.RuntimeId);
        npc.SpatialKnowledge.DiscoverRoute(world.RouteAB.RuntimeId);
        TravelSystem travel = world.CreateTravelSystem(new SimulationTime());

        SimulationInvariantValidator.ValidateSpatialKnowledge(npc.SpatialKnowledge, world.IdentityRegistry);
        var destinations = travel.GetKnownDirectDestinationCities(npc, world.A);

        Assert.That(destinations.Count, Is.EqualTo(1));
        Assert.That(destinations[0], Is.SameAs(world.B));
        Assert.That(destinations, Has.No.Member(world.C));
    }
}
