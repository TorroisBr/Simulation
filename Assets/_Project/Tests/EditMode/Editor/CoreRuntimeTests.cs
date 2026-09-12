using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

        Assert.That(fixture.Sequence.Allocate(), Is.EqualTo(1L));
        Assert.That(fixture.Sequence.Allocate(), Is.EqualTo(2L));
        Assert.That(fixture.Sequence.Allocate(), Is.EqualTo(3L));
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
        SimulationDate monthBoundary = calendar.GetDate(7L);
        SimulationDate yearBoundary = calendar.GetDate(13L);

        Assert.That(weekBoundary.WeekOfMonth, Is.EqualTo(2));
        Assert.That(weekBoundary.DayOfMonth, Is.EqualTo(1));
        Assert.That(weekBoundary.DayOfWeek, Is.EqualTo(1));
        Assert.That(monthBoundary.Month, Is.EqualTo(2));
        Assert.That(monthBoundary.DayOfMonth, Is.EqualTo(1));
        Assert.That(monthBoundary.WeekOfMonth, Is.EqualTo(1));
        Assert.That(monthBoundary.DayOfWeek, Is.EqualTo(1));
        Assert.That(yearBoundary.Year, Is.EqualTo(2L));
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
        Assert.That(knowledge.KnownLocationRuntimeIds, Has.Count.EqualTo(1));
        Assert.That(knowledge.KnownRouteRuntimeIds, Has.Count.EqualTo(1));
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

        Assert.That(destinations, Has.Count.EqualTo(1));
        Assert.That(destinations[0], Is.SameAs(world.B));
        Assert.That(destinations, Does.Not.Contain(world.C));
    }
}
