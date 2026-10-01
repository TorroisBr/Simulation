using System;
using System.Reflection;
using NUnit.Framework;

public sealed class ExpeditionExplorationTests
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
    public void ExpeditionCanBeginExplorationAfterSiteArrival()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.AtSite, ExpeditionObjectiveRuntime.Explore());

        Assert.That(fixture.System.TryBeginExploration(fixture.Expedition, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Exploring));
    }

    [Test]
    public void ExpeditionCannotExploreBeforeArrival()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.TravelingToSite, ExpeditionObjectiveRuntime.Explore());

        Assert.That(fixture.System.TryBeginExploration(fixture.Expedition, out _), Is.False);
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
    }

    [Test]
    public void SiteWithoutTopologyUsesAbstractProgress()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.AtSite, ExpeditionObjectiveRuntime.Explore(2));
        Assert.That(fixture.System.TryBeginExploration(fixture.Expedition, out string beginReason), Is.True, beginReason);

        Assert.That(fixture.System.TryContinueExploration(fixture.Expedition, out string firstReason), Is.True, firstReason);
        Assert.That(fixture.System.TryContinueExploration(fixture.Expedition, out string secondReason), Is.True, secondReason);
        Assert.That(fixture.Expedition.ExplorationProgress, Is.EqualTo(2));
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void SiteWithTopologyUsesLocalTopologyProgress()
    {
        ExplorationFixture fixture = CreateDetailedFixture();
        Assert.That(fixture.System.TryBeginExploration(fixture.Expedition, out string beginReason), Is.True, beginReason);

        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Root, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.VisitedLocalPlaceRuntimeIds, Contains.Item(fixture.Root.RuntimeId));
        Assert.That(fixture.Expedition.ExplorationProgress, Is.EqualTo(1));
    }

    [Test]
    public void ExploringNodeRevealsOnlyObservedTopologyKnowledge()
    {
        ExplorationFixture fixture = CreateDetailedFixture();
        fixture.System.TryBeginExploration(fixture.Expedition, out _);

        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Root, out string reason), Is.True, reason);
        Assert.That(fixture.Member.LocalTopologyKnowledge.KnowsLocalPlace(fixture.Site.RuntimeId, fixture.Root.RuntimeId), Is.True);
        Assert.That(fixture.Member.LocalTopologyKnowledge.KnowsLocalPlace(fixture.Site.RuntimeId, fixture.Hidden.RuntimeId), Is.False);
    }

    [Test]
    public void UnknownHiddenNodeRemainsUnknown()
    {
        ExplorationFixture fixture = CreateDetailedFixture();
        fixture.System.TryBeginExploration(fixture.Expedition, out _);
        fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Root, out _);

        Assert.That(fixture.Member.LocalTopologyKnowledge.PlaceObservations, Has.Count.EqualTo(1));
        Assert.That(fixture.Member.LocalTopologyKnowledge.KnowsLocalPlace(fixture.Site.RuntimeId, fixture.Hidden.RuntimeId), Is.False);
    }

    [Test]
    public void DirectedLocalConnectionsAreRespected()
    {
        ExplorationFixture fixture = CreateDetailedFixture();
        fixture.System.TryBeginExploration(fixture.Expedition, out _);
        fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Root, out _);

        Assert.That(fixture.System.TryTraverseLocalConnection(fixture.Expedition, fixture.Connection, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(fixture.Hidden.RuntimeId));
        Assert.That(fixture.Member.CurrentLocation, Is.SameAs(fixture.Site.Location));
    }

    [Test]
    public void ExpeditionCannotTraverseNonexistentConnection()
    {
        ExplorationFixture fixture = CreateDetailedFixture();
        fixture.System.TryBeginExploration(fixture.Expedition, out _);
        fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Root, out _);
        LocalTopologyConnectionRuntime nonexistent = new LocalTopologyConnectionRuntime(
            "not-published-connection", fixture.Root, fixture.Hidden, 1f);

        Assert.That(fixture.System.TryTraverseLocalConnection(fixture.Expedition, nonexistent, out _), Is.False);
        Assert.That(fixture.Expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(fixture.Root.RuntimeId));
    }

    [Test]
    public void ExplorationCanEncounterPlaceOpposition()
    {
        ExplorationFixture fixture = CreateContentFixture(ExpeditionObjectiveRuntime.Explore());
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("site-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("band", 10f, 4));
        Assert.That(fixture.ContentStore.TryAddOpposition(fixture.Site, opposition, out string addReason), Is.True, addReason);

        Assert.That(fixture.ContentStore.GetOrCreate(fixture.Site).ActiveOppositions, Contains.Item(opposition));
    }

    [Test]
    public void OppositionConflictUses4FResolver()
    {
        ExplorationFixture fixture = CreateContentFixture(ExpeditionObjectiveRuntime.Eliminate("site-opposition"));
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("site-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("band", 1f));
        fixture.ContentStore.TryAddOpposition(fixture.Site, opposition, out _);
        Conflict conflict = opposition.CreateConflict("exploration-conflict", new[] { fixture.Member });
        ConflictResolutionService resolver = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));

        Assert.That(fixture.System.TryResolvePlaceOpposition(
            fixture.Expedition,
            PlaceContentOwnerReference.ForExplorableSite(fixture.Site),
            opposition,
            conflict,
            resolver,
            out ConflictResolutionResult result,
            out string reason), Is.True, reason);
        Assert.That(result.WinningSideId, Is.EqualTo("expedition"));
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void ConflictConsequencesPersistAfterExplorationStep()
    {
        ExplorationFixture fixture = CreateContentFixture(ExpeditionObjectiveRuntime.Explore());
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("persistent-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("band", 1f));
        fixture.ContentStore.TryAddOpposition(fixture.Site, opposition, out _);
        Conflict conflict = opposition.CreateConflict("persistent-conflict", new[] { fixture.Member });
        ConflictResolutionService resolver = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));
        fixture.System.TryBeginExploration(fixture.Expedition, out _);

        Assert.That(fixture.System.TryResolvePlaceOpposition(
            fixture.Expedition, PlaceContentOwnerReference.ForExplorableSite(fixture.Site), opposition,
            conflict, resolver, out _, out string reason), Is.True, reason);
        Assert.That(fixture.ContentStore.GetOrCreate(fixture.Site).ActiveOppositions, Is.Empty);
        Assert.That(fixture.ContentStore.GetOrCreate(fixture.Site).SiteState, Is.EqualTo(PlaceSiteState.Cleared));
    }

    [Test]
    public void RetrieveObjectiveCompletesWhenTargetResourceObtained()
    {
        ItemData relic = SimulationTestFactory.CreateItem("retrieve-relic");
        ExplorationFixture fixture = CreateContentFixture(ExpeditionObjectiveRuntime.Retrieve(relic.DefinitionId));
        fixture.ContentStore.TryAddStack(fixture.Site, relic, 2, PlaceContentPersistencePolicy.Durable, out _);
        fixture.System.TryBeginExploration(fixture.Expedition, out _);

        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, relic, 2, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
        Assert.That(fixture.Member.Inventory.GetAmount(relic), Is.EqualTo(2));
        Assert.That(fixture.ContentStore.GetOrCreate(fixture.Site).GetAmount(relic), Is.EqualTo(0));
    }

    [Test]
    public void EliminateObjectiveCompletesWhenTargetOppositionResolved()
    {
        ExplorationFixture fixture = CreateContentFixture(ExpeditionObjectiveRuntime.Eliminate("eliminate-opposition"));
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("eliminate-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("band", 1f));
        fixture.ContentStore.TryAddOpposition(fixture.Site, opposition, out _);
        fixture.System.TryBeginExploration(fixture.Expedition, out _);
        ConflictResolutionService resolver = CreateWinningResolver();

        Assert.That(fixture.System.TryResolvePlaceOpposition(
            fixture.Expedition, PlaceContentOwnerReference.ForExplorableSite(fixture.Site), opposition,
            opposition.CreateConflict("eliminate-conflict", new[] { fixture.Member }), resolver,
            out _, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void SaturatedRetrieveObjectiveDoesNotConsumePlaceContentOrInventory()
    {
        ItemData item = SimulationTestFactory.CreateItem("retrieve-saturated");
        ExplorationFixture fixture = CreateContentFixture(ExpeditionObjectiveRuntime.Retrieve(item.DefinitionId));
        Assert.That(fixture.ContentStore.TryAddStack(
            fixture.Site, item, 2, PlaceContentPersistencePolicy.Durable, out _), Is.True);
        SetExpeditionRevision(fixture.System.Store, long.MaxValue);
        int contentBefore = fixture.ContentStore.GetOrCreate(fixture.Site).GetAmount(item);
        int inventoryBefore = fixture.Member.Inventory.GetAmount(item);
        int eventsBefore = fixture.World.Records.Events.Events.Count;

        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, item, 1, out string reason), Is.False);
        Assert.That(reason, Does.Contain("busy or revision capacity"));
        Assert.That(fixture.ContentStore.GetOrCreate(fixture.Site).GetAmount(item), Is.EqualTo(contentBefore));
        Assert.That(fixture.Member.Inventory.GetAmount(item), Is.EqualTo(inventoryBefore));
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.False);
        Assert.That(fixture.System.Store.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(fixture.World.Records.Events.Events.Count, Is.EqualTo(eventsBefore));
    }

    [Test]
    public void SaturatedEliminateObjectiveDoesNotResolveOppositionOrEmitEvents()
    {
        ExplorationFixture fixture = CreateContentFixture(ExpeditionObjectiveRuntime.Eliminate("saturated-opposition"));
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("saturated-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("band", 1f));
        Assert.That(fixture.ContentStore.TryAddOpposition(fixture.Site, opposition, out _), Is.True);
        Conflict conflict = opposition.CreateConflict("saturated-conflict", new[] { fixture.Member });
        ConflictResolutionService resolver = CreateWinningResolver();
        SetExpeditionRevision(fixture.System.Store, long.MaxValue);
        int eventsBefore = fixture.World.Records.Events.Events.Count;

        Assert.That(fixture.System.TryResolvePlaceOpposition(
            fixture.Expedition,
            PlaceContentOwnerReference.ForExplorableSite(fixture.Site),
            opposition,
            conflict,
            resolver,
            out _,
            out string reason), Is.False);
        Assert.That(reason, Does.Contain("busy or revision capacity"));
        Assert.That(opposition.IsResolved, Is.False);
        Assert.That(fixture.ContentStore.GetOrCreate(fixture.Site).ActiveOppositions, Contains.Item(opposition));
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.False);
        Assert.That(fixture.System.Store.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(fixture.World.Records.Events.Events.Count, Is.EqualTo(eventsBefore));
    }

    [Test]
    public void ExploreObjectiveCanCompleteAbstractly()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.AtSite, ExpeditionObjectiveRuntime.Explore());
        fixture.System.TryBeginExploration(fixture.Expedition, out _);

        Assert.That(fixture.System.TryContinueExploration(fixture.Expedition, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void ExploreObjectiveCanCompleteWithDetailedTopology()
    {
        ExplorationFixture fixture = CreateDetailedFixture();
        fixture.System.TryBeginExploration(fixture.Expedition, out _);

        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Root, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void ObjectiveCompletionDoesNotForceImmediateReturn()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.AtSite, ExpeditionObjectiveRuntime.Explore());
        fixture.System.TryBeginExploration(fixture.Expedition, out _);
        fixture.System.TryContinueExploration(fixture.Expedition, out _);

        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Exploring));
    }

    [Test]
    public void ExpeditionCanContinueAfterObjectiveWhenAllowed()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.AtSite, ExpeditionObjectiveRuntime.Explore());
        fixture.System.TryBeginExploration(fixture.Expedition, out _);
        fixture.System.TryContinueExploration(fixture.Expedition, out _);

        Assert.That(fixture.System.TryContinueExploration(fixture.Expedition, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Exploring));
    }

    [Test]
    public void ExpeditionCanChooseReturnAfterObjective()
    {
        ExplorationFixture fixture = CreateReturningFixture();
        fixture.Expedition.TryMarkObjectiveComplete();

        Assert.That(fixture.System.TryBeginReturn(fixture.Expedition, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Returning));
    }

    [Test]
    public void ReturnUsesRealMacroRoute()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime member = fixture.CreateNpc("return-member", fixture.CityA, 10f);
        AddKnowledgeForBothDirections(member, fixture);
        fixture.Knowledge.RecordInitialScenarioKnowledge(member, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);
        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(member, fixture.Site, fixture.SiteRoute),
            out ExpeditionRuntime expedition), Is.True);

        SimulationRuntime runtime = CreateSimulationRuntime(fixture, member, system);
        runtime.AdvanceDays(fixture.SiteRoute.TravelDays + 1);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        Assert.That(system.TryBeginReturn(expedition, out string returnReason), Is.True, returnReason);
        Assert.That(member.IsTraveling, Is.True);
        Assert.That(member.DestinationLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(member.CurrentLocation, Is.Not.SameAs(fixture.CityA.Location));
        runtime.AdvanceDays(fixture.SiteToCityRoute.TravelDays + 1);

        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Completed));
        Assert.That(expedition.IsActive, Is.False);
        Assert.That(member.CurrentLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(system.Store.ActiveExpeditions, Is.Empty);
    }

    [Test]
    public void MissingReturnRouteDoesNotTeleportMembers()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.AtSite, ExpeditionObjectiveRuntime.Explore());
        SpatialLocationRuntime before = fixture.Member.CurrentLocation;

        Assert.That(fixture.System.TryBeginReturn(fixture.Expedition, out _), Is.False);
        Assert.That(fixture.Member.CurrentLocation, Is.SameAs(before));
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.AtSite));
    }

    [Test]
    public void CompletedExpeditionIsNoLongerActive()
    {
        ExplorationFixture fixture = CreateReturningFixture();
        fixture.Member.SetCurrentPresence(fixture.City.Location, fixture.City);
        Assert.That(fixture.Expedition.TryBeginReturn(), Is.True);
        Assert.That(fixture.System.ReconcileAfterTravel(), Has.Count.EqualTo(1));

        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Completed));
        Assert.That(fixture.Expedition.IsActive, Is.False);
        Assert.That(fixture.System.IsNpcOnActiveExpedition(fixture.Member.RuntimeId), Is.False);
    }

    [Test]
    public void NewExpeditionCanUseSameSiteLater()
    {
        ExplorationFixture fixture = CreateReturningFixture();
        fixture.Member.SetCurrentPresence(fixture.City.Location, fixture.City);
        fixture.Expedition.TryBeginReturn();
        fixture.System.ReconcileAfterTravel();
        ExpeditionRuntime next = new ExpeditionRuntime(
            "expedition-next",
            fixture.Site.RuntimeId,
            fixture.City.Location.RuntimeId,
            fixture.Site.Location.RuntimeId,
            "return-route",
            new[] { fixture.Member.RuntimeId },
            new[] { fixture.Member.RuntimeId },
            Array.Empty<string>(),
            objective: ExpeditionObjectiveRuntime.Explore());

        Assert.That(fixture.System.Store.Add(next), Is.True);
        Assert.That(fixture.System.Store.ActiveExpeditions, Contains.Item(next));
    }

    [Test]
    public void EvaluationQueriesDoNotMutateExploration()
    {
        ExplorationFixture fixture = CreateDirectFixture(ExpeditionState.Exploring, ExpeditionObjectiveRuntime.Explore(3));
        int progressBefore = fixture.Expedition.ExplorationProgress;
        int visitedBefore = fixture.Expedition.VisitedLocalPlaceRuntimeIds.Count;

        Assert.That(fixture.System.CanContinueExploration(fixture.Expedition, out _), Is.True);
        Assert.That(fixture.Expedition.ExplorationProgress, Is.EqualTo(progressBefore));
        Assert.That(fixture.Expedition.VisitedLocalPlaceRuntimeIds, Has.Count.EqualTo(visitedBefore));
    }

    [Test]
    public void RecordingDoesNotMutateProgressOrRandomState()
    {
        ExplorationFixture fixture = CreateDetailedFixture();
        fixture.System.TryBeginExploration(fixture.Expedition, out _);
        fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Root, out _);
        int progressBefore = fixture.Expedition.ExplorationProgress;
        int connectionsBefore = fixture.Expedition.ObservedLocalConnectionRuntimeIds.Count;

        Assert.That(fixture.Member.LocalTopologyKnowledge.KnowsLocalPlace(fixture.Site.RuntimeId, fixture.Root.RuntimeId), Is.True);
        Assert.That(fixture.Expedition.ExplorationProgress, Is.EqualTo(progressBefore));
        Assert.That(fixture.Expedition.ObservedLocalConnectionRuntimeIds, Has.Count.EqualTo(connectionsBefore));
    }

    private static ExplorationFixture CreateDirectFixture(
        ExpeditionState state,
        ExpeditionObjectiveRuntime objective,
        bool includeContentStore = false)
    {
        SpatialTravelFixture world = new SpatialTravelFixture();
        NpcRuntime member = world.CreateNpc("direct-explorer", world.CityA, 100f);
        PlaceContentStore contentStore = includeContentStore
            ? new PlaceContentStore(world.Records.Allocator, world.IdentityRegistry)
            : null;
        ExpeditionSystem system = CreateSystem(world, contentStore);
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "direct-expedition",
            world.Site.RuntimeId,
            world.CityA.Location.RuntimeId,
            world.Site.Location.RuntimeId,
            world.SiteRoute.RuntimeId,
            state == ExpeditionState.TravelingToSite || state == ExpeditionState.Returning ? "direct-party" : null,
            null,
            new[] { member.RuntimeId },
            new[] { member.RuntimeId },
            Array.Empty<string>(),
            state,
            objective);
        Assert.That(system.Store.Add(expedition), Is.True);
        return new ExplorationFixture(world, system, member, expedition, contentStore);
    }

    private static ExplorationFixture CreateDetailedFixture()
    {
        SpatialTravelFixture world = new SpatialTravelFixture();
        NpcRuntime member = world.CreateNpc("detailed-explorer", world.CityA, 100f);
        PlaceContentStore contentStore = new PlaceContentStore(world.Records.Allocator, world.IdentityRegistry);
        RuntimeIdentityRegistry registry = world.IdentityRegistry;
        LocalTopologyStore topologyStore = new LocalTopologyStore(registry);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(world.Site), registry);
        LocalPlaceRuntime root = new LocalPlaceRuntime("explore-root", "Entry");
        LocalPlaceRuntime hidden = new LocalPlaceRuntime("explore-hidden", "Hidden");
        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            "explore-connection", root, hidden, 1f);
        Assert.That(topology.AddPlace(root, null, true), Is.True);
        Assert.That(topology.AddPlace(hidden, root), Is.True);
        Assert.That(topology.AddConnection(connection), Is.True);
        Assert.That(topologyStore.Add(topology), Is.True);
        member.SetCurrentPresence(world.Site.Location);
        ExpeditionSystem system = CreateSystem(world, contentStore, topologyStore);
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "detailed-expedition",
            world.Site.RuntimeId,
            world.CityA.Location.RuntimeId,
            world.Site.Location.RuntimeId,
            world.SiteRoute.RuntimeId,
            null,
            null,
            new[] { member.RuntimeId },
            new[] { member.RuntimeId },
            Array.Empty<string>(),
            ExpeditionState.AtSite,
            ExpeditionObjectiveRuntime.Explore());
        Assert.That(system.Store.Add(expedition), Is.True);
        ExplorationFixture fixture = new ExplorationFixture(world, system, member, expedition, contentStore)
        {
            Root = root,
            Hidden = hidden,
            Connection = connection
        };
        return fixture;
    }

    private static ExplorationFixture CreateContentFixture(ExpeditionObjectiveRuntime objective)
    {
        return CreateDirectFixture(ExpeditionState.Exploring, objective, includeContentStore: true);
    }

    private static ExplorationFixture CreateReturningFixture()
    {
        SpatialTravelFixture world = new SpatialTravelFixture();
        NpcRuntime member = world.CreateNpc("returning-explorer", world.CityA, 100f);
        member.SetCurrentPresence(world.Site.Location);
        member.SpatialKnowledge.DiscoverLocation(world.Site.Location.RuntimeId);
        member.SpatialKnowledge.DiscoverLocation(world.CityA.Location.RuntimeId);
        member.SpatialKnowledge.DiscoverRoute(world.SiteToCityRoute.RuntimeId);
        ExpeditionSystem system = CreateSystem(world);
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "returning-expedition",
            world.Site.RuntimeId,
            world.CityA.Location.RuntimeId,
            world.Site.Location.RuntimeId,
            world.SiteRoute.RuntimeId,
            "completed-party",
            null,
            new[] { member.RuntimeId },
            new[] { member.RuntimeId },
            Array.Empty<string>(),
            ExpeditionState.AtSite,
            ExpeditionObjectiveRuntime.Explore());
        Assert.That(system.Store.Add(expedition), Is.True);
        return new ExplorationFixture(world, system, member, expedition);
    }

    private static ConflictResolutionService CreateWinningResolver()
    {
        return new ConflictResolutionService(
                new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));
    }

    private static void SetExpeditionRevision(ExpeditionStore store, long value)
    {
        typeof(ExpeditionStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(store, value);
    }

    private static ExpeditionSystem CreateSystem(
        SpatialTravelFixture fixture,
        PlaceContentStore contentStore = null,
        LocalTopologyStore topologyStore = null)
    {
        TravelPartyStore parties = new TravelPartyStore();
        TravelPartySystem partySystem = new TravelPartySystem(
            parties,
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Travel,
            fixture.Records.Time,
            fixture.Records.Sequence,
            fixture.Records.EventRecorder);
        fixture.SetTravelPartySystem(partySystem);
        return new ExpeditionSystem(
            new ExpeditionStore(),
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Sites,
            partySystem,
            parties,
            fixture.Knowledge,
            fixture.Records.Time,
            fixture.Records.EventRecorder,
            null,
            contentStore,
            topologyStore);
    }

    private static ActionExecutionContext CreateContext(
        NpcRuntime member,
        ExplorableSiteRuntime site,
        SpatialRouteRuntime route)
    {
        return new ActionExecutionContext(
            "expedition-return-test",
            new[] { new ActionExecutionParticipant(member.RuntimeId, ActionExecutionParticipantRole.Performer) },
            site.Location.RuntimeId,
            route.RuntimeId);
    }

    private static SimulationRuntime CreateSimulationRuntime(
        SpatialTravelFixture fixture,
        NpcRuntime member,
        ExpeditionSystem system)
    {
        return new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { member },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);
    }

    private static void AddKnowledgeForBothDirections(NpcRuntime member, SpatialTravelFixture fixture)
    {
        member.SpatialKnowledge.DiscoverLocation(fixture.CityA.Location.RuntimeId);
        member.SpatialKnowledge.DiscoverLocation(fixture.Site.Location.RuntimeId);
        member.SpatialKnowledge.DiscoverRoute(fixture.SiteRoute.RuntimeId);
        member.SpatialKnowledge.DiscoverRoute(fixture.SiteToCityRoute.RuntimeId);
    }

    private sealed class ExplorationFixture
    {
        public SpatialTravelFixture World { get; }
        public ExpeditionSystem System { get; set; }
        public NpcRuntime Member { get; }
        public ExpeditionRuntime Expedition { get; }
        public LocalPlaceRuntime Root { get; set; }
        public LocalPlaceRuntime Hidden { get; set; }
        public LocalTopologyConnectionRuntime Connection { get; set; }
        public PlaceContentStore ContentStore { get; set; }
        public ExplorableSiteRuntime Site => World.Site;
        public CityRuntime City => World.CityA;

        public ExplorationFixture(
            SpatialTravelFixture world,
            ExpeditionSystem system,
            NpcRuntime member,
            ExpeditionRuntime expedition,
            PlaceContentStore contentStore = null)
        {
            World = world;
            System = system;
            Member = member;
            Expedition = expedition;
            ContentStore = contentStore ?? new PlaceContentStore(world.Records.Allocator, world.IdentityRegistry);
        }
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        private readonly float capability;

        public FixedCapabilityModel(float capability)
        {
            this.capability = capability;
        }

        public CapabilityEvaluationResult Evaluate(NpcRuntime participant, CapabilityEvaluationContext context = null)
        {
            return new CapabilityEvaluationResult(capability, capability, null);
        }
    }
}
