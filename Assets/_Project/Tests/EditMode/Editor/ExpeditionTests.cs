using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ExpeditionTests
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
    public void RuntimeIdentityAllocator_UsesIndependentExpeditionNamespace()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(allocator.AllocateExpeditionId(), Is.EqualTo("expedition-000001"));
        Assert.That(allocator.AllocateExpeditionId(), Is.EqualTo("expedition-000002"));
        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-000001"));
    }

    [Test]
    public void ExpeditionRuntimeRequiresStableIdentityAndTargetData()
    {
        Assert.Throws<ArgumentException>(() => CreateRuntime(null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", targetSiteRuntimeId: null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", originLocationRuntimeId: null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", targetLocationRuntimeId: null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", outboundRouteRuntimeId: null));
    }

    [Test]
    public void ExpeditionRuntimeRequiresPerformersAndUniqueRoleSnapshots()
    {
        Assert.Throws<ArgumentException>(() => new ExpeditionRuntime(
            "expedition-1",
            "site-1",
            "location-a",
            "location-site",
            "route-a-site",
            null,
            null,
            new[] { "npc-support" },
            Array.Empty<string>(),
            new[] { "npc-support" }));

        Assert.Throws<ArgumentException>(() => new ExpeditionRuntime(
            "expedition-1",
            "site-1",
            "location-a",
            "location-site",
            "route-a-site",
            null,
            null,
            new[] { "npc-performer", "npc-performer" },
            new[] { "npc-performer" },
            Array.Empty<string>()));

        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-1",
            "site-1",
            "location-a",
            "location-site",
            "route-a-site",
            null,
            null,
            new[] { "npc-performer", "npc-support" },
            new[] { "npc-performer" },
            new[] { "npc-support" });

        Assert.That((expedition.MemberRuntimeIds as IList<string>)?.IsReadOnly, Is.True);
        Assert.That((expedition.PerformerRuntimeIds as IList<string>)?.IsReadOnly, Is.True);
        Assert.That((expedition.SupportRuntimeIds as IList<string>)?.IsReadOnly, Is.True);
    }

    [Test]
    public void ExpeditionStorePreservesOrderAndRejectsDuplicatesAndOverlappingMembers()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime first = CreateRuntime("expedition-1", memberRuntimeId: "npc-1");
        ExpeditionRuntime duplicate = CreateRuntime("expedition-1", memberRuntimeId: "npc-2");
        ExpeditionRuntime overlapping = CreateRuntime("expedition-2", memberRuntimeId: "npc-1");

        Assert.That(store.Add(first), Is.True);
        Assert.That(store.Add(duplicate), Is.False);
        Assert.That(store.Add(overlapping), Is.False);
        Assert.That(store.ActiveExpeditions.Count, Is.EqualTo(1));
        Assert.That(store.ActiveExpeditions[0], Is.SameAs(first));
        Assert.That(store.TryGetExpeditionForNpc("npc-1", out ExpeditionRuntime found), Is.True);
        Assert.That(found, Is.SameAs(first));
        Assert.That(store.TryGetExpeditionForNpc("npc-unknown", out _), Is.False);
    }

    [Test]
    public void ExpeditionStartRequiresTargetSiteRouteAndParticipantPresenceTruth()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("expedition-performer", fixture.CityA, 10f);
        ExpeditionTestContext context = CreateContext(fixture, performer);
        ExpeditionSystem system = CreateSystem(fixture);

        Assert.That(system.TryStartExpedition(null, context.ActionContext, out _), Is.False);
        Assert.That(system.TryStartExpedition(fixture.Site, new ActionExecutionContext(
            "expedition",
            context.ActionContext.Participants,
            fixture.CityB.Location.RuntimeId,
            fixture.SiteRoute.RuntimeId), out _), Is.False);

        NpcRuntime elsewhere = fixture.CreateNpc("expedition-elsewhere", fixture.CityB, 10f);
        ActionExecutionContext differentOrigin = CreateContext(
            fixture,
            performer,
            elsewhere).ActionContext;
        Assert.That(system.TryStartExpedition(fixture.Site, differentOrigin, out _), Is.False);
    }

    [Test]
    public void ExpeditionPerformerMustKnowSiteAndAllMembersMustKnowRoute()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("knowledge-performer", fixture.CityA, 10f);
        ExpeditionSystem system = CreateSystem(fixture);
        ExpeditionTestContext context = CreateContext(fixture, performer);

        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        Assert.That(system.TryStartExpedition(fixture.Site, context.ActionContext, out _), Is.False);

        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site, fixture.Records.Time.AbsoluteDay);
        Assert.That(system.TryStartExpedition(fixture.Site, context.ActionContext, out ExpeditionRuntime expedition), Is.True);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
    }

    [Test]
    public void ExpeditionSupportMayLackSemanticSiteKnowledgeButNeedsSpatialKnowledge()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("supported-performer", fixture.CityA, 10f);
        NpcRuntime support = fixture.CreateNpc("supported-support", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        AddSpatialKnowledge(support, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site, fixture.Records.Time.AbsoluteDay);
        ExpeditionSystem system = CreateSystem(fixture);

        ExpeditionTestContext context = CreateContext(fixture, performer, support);
        Assert.That(system.TryStartExpedition(fixture.Site, context.ActionContext, out ExpeditionRuntime expedition), Is.True);
        Assert.That(support.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.False);
        Assert.That(expedition.SupportRuntimeIds.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExpeditionStartIsAtomicWhenTravelMoneyIsInsufficient()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("poor-performer", fixture.CityA, 1f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        TravelPartyStore parties = new TravelPartyStore();
        ExpeditionSystem system = CreateSystem(fixture, parties);
        float originalMoney = performer.Money;

        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(fixture, performer).ActionContext,
            out _), Is.False);
        Assert.That(system.Store.ActiveExpeditions.Count, Is.EqualTo(0));
        Assert.That(parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(performer.CurrentCity, Is.SameAs(fixture.CityA));
        Assert.That(performer.Money, Is.EqualTo(originalMoney));
        Assert.That(performer.DestinationLocation, Is.Null);
    }

    [Test]
    public void SuccessfulExpeditionCreatesTravelPartyAndStartedEventExactlyOnce()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("started-performer", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);

        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(fixture, performer, originDecisionId: "decision-expedition").ActionContext,
            out ExpeditionRuntime expedition), Is.True);

        SimulationInvariantValidator.ValidateExpedition(expedition, fixture.IdentityRegistry);
        Assert.That(expedition.ExpeditionId, Is.EqualTo("expedition-000001"));
        Assert.That(expedition.TravelPartyId, Is.Not.Null.And.Not.Empty);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        Assert.That(fixture.TravelParties.ActiveParties.Count, Is.EqualTo(1));
        Assert.That(CountEvents<ExpeditionStartedEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(CountEvents<TravelPartyStartedEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(CountEventsWithDecision(fixture.Records.Events.Events, "decision-expedition"), Is.EqualTo(2));
        Assert.That(performer.Money, Is.EqualTo(8f));
        Assert.That(fixture.IdentityRegistry.TryGetNpc(expedition.ExpeditionId, out _), Is.False);
    }

    [Test]
    public void ExpeditionRemainsTravelingUntilAllMembersArriveAndThenBecomesAtSite()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("lifecycle-performer", fixture.CityA, 10f);
        NpcRuntime support = fixture.CreateNpc("lifecycle-support", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        AddSpatialKnowledge(support, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        TravelPartyStore parties = new TravelPartyStore();
        ExpeditionSystem system = CreateSystem(fixture, parties);
        ExpeditionTestContext expeditionContext = CreateContext(fixture, performer, support);

        Assert.That(system.TryStartExpedition(fixture.Site, expeditionContext.ActionContext, out ExpeditionRuntime expedition), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { performer, support },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);

        runtime.AdvanceDay();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        runtime.AdvanceDay();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        runtime.AdvanceDay();

        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        Assert.That(parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(support.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(support.CurrentCity, Is.Null);
        Assert.That(performer.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.True);
        Assert.That(support.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.True);
        Assert.That(CountEvents<ExpeditionArrivedAtSiteEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(CountEvents<TravelPartyArrivedEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(fixture.Site.DefinitionId, Is.EqualTo("spatial-site"));
    }

    [Test]
    public void AtSiteExpeditionMembersDoNotTakeAutonomousActionsOrAutoReturn()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("suppressed-performer", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);
        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(fixture, performer).ActionContext,
            out ExpeditionRuntime expedition), Is.True);
        NpcActionData action = SimulationTestFactory.CreateAction("should-not-run", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { performer },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            decisionRecorder: fixture.Records.DecisionRecorder,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);

        runtime.AdvanceDays(3);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        int decisionsAfterArrival = fixture.Records.Decisions.Decisions.Count;
        runtime.AdvanceDay();

        Assert.That(fixture.Records.Decisions.Decisions.Count, Is.EqualTo(decisionsAfterArrival));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
    }

    private static ExpeditionSystem CreateSystem(
        SpatialTravelFixture fixture,
        TravelPartyStore parties = null)
    {
        parties = parties ?? new TravelPartyStore();
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
            fixture.Records.EventRecorder);
    }

    private static ExpeditionTestContext CreateContext(
        SpatialTravelFixture fixture,
        NpcRuntime performer,
        NpcRuntime support = null,
        string originDecisionId = null)
    {
        List<ActionExecutionParticipant> participants = new List<ActionExecutionParticipant>
        {
            new ActionExecutionParticipant(performer.RuntimeId, ActionExecutionParticipantRole.Performer)
        };

        if (support != null)
        {
            participants.Add(new ActionExecutionParticipant(support.RuntimeId, ActionExecutionParticipantRole.Support));
        }

        return new ExpeditionTestContext(
            new ActionExecutionContext(
                "expedition-action",
                participants,
                fixture.Site.Location.RuntimeId,
                fixture.SiteRoute.RuntimeId,
                originDecisionId));
    }

    private static void AddSpatialKnowledge(
        NpcRuntime npc,
        SpatialTravelFixture fixture,
        bool includeRoute)
    {
        npc.SpatialKnowledge.DiscoverLocation(fixture.CityA.Location.RuntimeId);
        npc.SpatialKnowledge.DiscoverLocation(fixture.Site.Location.RuntimeId);

        if (includeRoute)
        {
            npc.SpatialKnowledge.DiscoverRoute(fixture.SiteRoute.RuntimeId);
        }
    }

    private static ExpeditionRuntime CreateRuntime(
        string expeditionId,
        string targetSiteRuntimeId = "site-1",
        string originLocationRuntimeId = "location-a",
        string targetLocationRuntimeId = "location-site",
        string outboundRouteRuntimeId = "route-a-site",
        string memberRuntimeId = "npc-performer")
    {
        return new ExpeditionRuntime(
            expeditionId,
            targetSiteRuntimeId,
            originLocationRuntimeId,
            targetLocationRuntimeId,
            outboundRouteRuntimeId,
            null,
            null,
            new[] { memberRuntimeId },
            new[] { memberRuntimeId },
            Array.Empty<string>());
    }

    private static int CountEvents<T>(IReadOnlyList<DomainEvent> events) where T : DomainEvent
    {
        int count = 0;

        foreach (DomainEvent domainEvent in events)
        {
            if (domainEvent is T)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountEventsWithDecision(
        IReadOnlyList<DomainEvent> events,
        string originDecisionId)
    {
        int count = 0;

        foreach (DomainEvent domainEvent in events)
        {
            if (domainEvent != null && domainEvent.OriginDecisionId == originDecisionId)
            {
                count++;
            }
        }

        return count;
    }
}

internal sealed class ExpeditionTestContext
{
    public ActionExecutionContext ActionContext { get; }

    public ExpeditionTestContext(ActionExecutionContext actionContext)
    {
        ActionContext = actionContext;
    }
}
