using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ExpeditionExplorationCorrectionTests
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
    public void DetailedExplorationMustEnterThroughEntryPoint()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3), ExpeditionState.AtSite);

        Assert.That(fixture.System.TryBeginExploration(fixture.Expedition, out string beginReason), Is.True, beginReason);
        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Entry, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(fixture.Entry.RuntimeId));
    }

    [Test]
    public void DetailedExplorationCannotStartAtNonEntryPoint()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));

        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Hall, out _), Is.False);
        Assert.That(fixture.Expedition.CurrentLocalPlaceRuntimeId, Is.Null);
        Assert.That(fixture.Expedition.ExplorationProgress, Is.Zero);
    }

    [Test]
    public void DirectExploreCannotTeleportBetweenLocalPlaces()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();

        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Hall, out _), Is.False);
        Assert.That(fixture.Expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(fixture.Entry.RuntimeId));
    }

    [Test]
    public void DirectedConnectionMovesExpeditionBetweenPlaces()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();

        Assert.That(fixture.System.TryTraverseLocalConnection(fixture.Expedition, fixture.EntryToHall, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(fixture.Hall.RuntimeId));
        SimulationInvariantValidator.ValidateExpedition(
            fixture.Expedition,
            fixture.World.IdentityRegistry,
            fixture.Topologies,
            fixture.Store);
    }

    [Test]
    public void ReverseTraversalWithoutReverseConnectionFails()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();
        Assert.That(fixture.System.TryTraverseLocalConnection(fixture.Expedition, fixture.EntryToHall, out _), Is.True);

        Assert.That(fixture.System.TryTraverseLocalConnection(fixture.Expedition, fixture.EntryToHall, out _), Is.False);
        Assert.That(fixture.Expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(fixture.Hall.RuntimeId));
    }

    [Test]
    public void RevisitingLocalPlaceDoesNotIncreaseExploreProgress()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();
        int progress = fixture.Expedition.ExplorationProgress;

        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Entry, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.ExplorationProgress, Is.EqualTo(progress));
    }

    [Test]
    public void FirstVisitIncreasesProgressOnlyOnce()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));

        fixture.Enter();
        fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Entry, out _);

        Assert.That(fixture.Expedition.ExplorationProgress, Is.EqualTo(1));
        Assert.That(fixture.Expedition.VisitedLocalPlaceRuntimeIds, Has.Count.EqualTo(1));
    }

    [Test]
    public void FailedTraversalDoesNotRevealDestinationKnowledge()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();

        Assert.That(fixture.System.TryTraverseLocalConnection(fixture.Expedition, fixture.HallToTreasury, out _), Is.False);
        Assert.That(fixture.Member.LocalTopologyKnowledge.KnowsLocalPlace(fixture.Site.RuntimeId, fixture.Treasury.RuntimeId), Is.False);
        Assert.That(fixture.Member.LocalTopologyKnowledge.KnowsConnection(fixture.Site.RuntimeId, fixture.HallToTreasury.RuntimeId), Is.False);
    }

    [Test]
    public void FailedDirectTeleportDoesNotRevealTargetKnowledge()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();

        Assert.That(fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Treasury, out _), Is.False);
        Assert.That(fixture.Member.LocalTopologyKnowledge.KnowsLocalPlace(fixture.Site.RuntimeId, fixture.Treasury.RuntimeId), Is.False);
    }

    [Test]
    public void SiteLevelResourceMustBelongToTargetSite()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());
        ItemData item = SimulationTestFactory.CreateItem("wrong-site-resource");
        PlaceContentOwnerReference wrongOwner = PlaceContentOwnerReference.ForExplorableSite(fixture.World.UnrelatedSite);
        Assert.That(fixture.Content.TryAddStack(wrongOwner, item, 1, PlaceContentPersistencePolicy.Durable, out _), Is.True);

        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, wrongOwner, item, 1, out _), Is.False);
        Assert.That(fixture.Member.Inventory.GetAmount(item), Is.Zero);
    }

    [Test]
    public void LocalResourceRequiresExpeditionAtThatLocalPlace()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();
        ItemData item = SimulationTestFactory.CreateItem("remote-local-resource");
        PlaceContentOwnerReference owner = fixture.Owner(fixture.Hall);
        Assert.That(fixture.Content.TryAddStack(owner, item, 1, PlaceContentPersistencePolicy.Durable, out _), Is.True);

        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, owner, item, 1, out _), Is.False);
        Assert.That(fixture.Member.Inventory.GetAmount(item), Is.Zero);
    }

    [Test]
    public void InaccessibleResourceCannotBeRetrieved()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());
        ItemData item = SimulationTestFactory.CreateItem("inaccessible-resource");
        PlaceContentOwnerReference owner = fixture.SiteOwner;
        Assert.That(fixture.Content.TryAddStack(owner, item, 1, PlaceContentPersistencePolicy.Durable, out _), Is.True);
        Assert.That(fixture.Content.GetOrCreate(owner).MarkInaccessible(), Is.True);

        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, owner, item, 1, out _), Is.False);
        Assert.That(fixture.Content.GetOrCreate(owner).GetAmount(item), Is.EqualTo(1));
    }

    [Test]
    public void ContestedTreasureCannotBeRetrievedBeforeOppositionResolution()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());
        ItemData treasure = SimulationTestFactory.CreateItem("contested-treasure");
        PlaceOppositionRuntime opposition = fixture.AddOpposition(fixture.SiteOwner, "treasure-guards");
        Assert.That(fixture.Content.TryAddStack(fixture.SiteOwner, treasure, 1, PlaceContentPersistencePolicy.Durable, out _), Is.True);

        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, fixture.SiteOwner, treasure, 1, out _), Is.False);
        Assert.That(opposition.IsActive, Is.True);
    }

    [Test]
    public void ResolvedOppositionCanUnlockExistingTreasure()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());
        ItemData treasure = SimulationTestFactory.CreateItem("unlocked-treasure");
        PlaceOppositionRuntime opposition = fixture.AddOpposition(fixture.SiteOwner, "unlock-guards");
        fixture.Content.TryAddStack(fixture.SiteOwner, treasure, 1, PlaceContentPersistencePolicy.Durable, out _);

        Assert.That(fixture.Resolve(fixture.SiteOwner, opposition, "unlock-conflict"), Is.True);
        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, fixture.SiteOwner, treasure, 1, out string reason), Is.True, reason);
        Assert.That(fixture.Member.Inventory.GetAmount(treasure), Is.EqualTo(1));
    }

    [Test]
    public void LocalOppositionRequiresExpeditionAtItsLocalPlace()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();
        PlaceContentOwnerReference owner = fixture.Owner(fixture.Entry);
        PlaceOppositionRuntime opposition = fixture.AddOpposition(owner, "entry-opposition");

        Assert.That(fixture.Resolve(owner, opposition, "entry-conflict"), Is.True);
        Assert.That(opposition.IsResolved, Is.True);
    }

    [Test]
    public void RemoteLocalOppositionCannotBeResolved()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();
        PlaceContentOwnerReference owner = fixture.Owner(fixture.Hall);
        PlaceOppositionRuntime opposition = fixture.AddOpposition(owner, "remote-opposition");

        Assert.That(fixture.Resolve(owner, opposition, "remote-conflict"), Is.False);
        Assert.That(opposition.IsActive, Is.True);
    }

    [Test]
    public void RetrieveCommonDefinitionStillWorks()
    {
        ItemData item = SimulationTestFactory.CreateItem("common-relic");
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.RetrieveDefinition(item.DefinitionId));
        fixture.Content.TryAddStack(fixture.SiteOwner, item, 2, PlaceContentPersistencePolicy.Durable, out _);

        Assert.That(fixture.System.TryRetrieveTargetResource(fixture.Expedition, fixture.SiteOwner, item, 2, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void RetrieveNotableTransfersExactItemToPerformer()
    {
        ItemData definition = SimulationTestFactory.CreateItem("notable-relic");
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());
        NotableItemRuntime notable = fixture.AddNotable(fixture.SiteOwner, definition);

        Assert.That(fixture.System.TryRetrieveNotableItem(fixture.Expedition, notable.RuntimeId, out NotableItemRuntime retrieved, out string reason), Is.True, reason);
        Assert.That(retrieved, Is.SameAs(notable));
        Assert.That(notable.CustodianNpcRuntimeId, Is.EqualTo(fixture.Member.RuntimeId));
        Assert.That(fixture.Content.GetNotableItemsAtPlace(fixture.SiteOwner), Is.Empty);
    }

    [Test]
    public void RetrieveNotableObjectiveUsesRuntimeIdentityNotDisplayName()
    {
        ItemData definition = SimulationTestFactory.CreateItem("same-display-definition");
        CorrectionFixture seed = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());
        NotableItemRuntime target = seed.Content.CreateNotableItem(definition);
        NotableItemRuntime lookalike = seed.Content.CreateNotableItem(definition);
        seed.Content.TryAddNotable(seed.SiteOwner, target, out _);
        seed.Content.TryAddNotable(seed.SiteOwner, lookalike, out _);
        seed.ReplaceExpedition(ExpeditionObjectiveRuntime.RetrieveNotable(target.RuntimeId));

        Assert.That(seed.System.TryRetrieveNotableItem(seed.Expedition, lookalike.RuntimeId, out _, out string wrongReason), Is.True, wrongReason);
        Assert.That(seed.Expedition.IsObjectiveComplete, Is.False);
        Assert.That(seed.System.TryRetrieveNotableItem(seed.Expedition, target.RuntimeId, out _, out string targetReason), Is.True, targetReason);
        Assert.That(seed.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void RetrieveWrongNotableDoesNotCompleteObjective()
    {
        ItemData definition = SimulationTestFactory.CreateItem("wrong-notable-definition");
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());
        NotableItemRuntime target = fixture.Content.CreateNotableItem(definition);
        NotableItemRuntime wrong = fixture.Content.CreateNotableItem(definition);
        fixture.Content.TryAddNotable(fixture.SiteOwner, target, out _);
        fixture.Content.TryAddNotable(fixture.SiteOwner, wrong, out _);
        fixture.ReplaceExpedition(ExpeditionObjectiveRuntime.RetrieveNotable(target.RuntimeId));

        Assert.That(fixture.System.TryRetrieveNotableItem(fixture.Expedition, wrong.RuntimeId, out _, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.False);
    }

    [Test]
    public void FailedNotableTransferDoesNotCompleteObjective()
    {
        ItemData definition = SimulationTestFactory.CreateItem("remote-notable-definition");
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();
        NotableItemRuntime notable = fixture.AddNotable(fixture.Owner(fixture.Hall), definition);
        fixture.ReplaceExpedition(ExpeditionObjectiveRuntime.RetrieveNotable(notable.RuntimeId));
        fixture.Enter();

        Assert.That(fixture.System.TryRetrieveNotableItem(fixture.Expedition, notable.RuntimeId, out _, out _), Is.False);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.False);
        Assert.That(notable.Owner.OwnerRuntimeId, Is.EqualTo(fixture.Hall.RuntimeId));
    }

    [Test]
    public void ExpeditionExplorationStartedEventRecordedAfterSuccessfulStart()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);

        Assert.That(fixture.System.TryBeginExploration(fixture.Expedition, out string reason), Is.True, reason);
        ExpeditionExplorationStartedEvent recorded = fixture.SingleEvent<ExpeditionExplorationStartedEvent>();
        Assert.That(recorded.ExpeditionId, Is.EqualTo(fixture.Expedition.ExpeditionId));
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Exploring));
    }

    [Test]
    public void ExpeditionAdvancedEventRecordedForNewProgress()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));

        fixture.Enter();

        ExpeditionAdvancedEvent recorded = fixture.SingleEvent<ExpeditionAdvancedEvent>();
        Assert.That(recorded.Progress, Is.EqualTo(1));
        Assert.That(recorded.CurrentLocalPlaceRuntimeId, Is.EqualTo(fixture.Entry.RuntimeId));
    }

    [Test]
    public void RevisitWithoutProgressDoesNotCreateFakeAdvancedEvent()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(3));
        fixture.Enter();

        fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Entry, out _);

        Assert.That(fixture.CountEvents<ExpeditionAdvancedEvent>(), Is.EqualTo(1));
    }

    [Test]
    public void ObjectiveCompletedEventRecordedOnce()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore());

        fixture.Enter();
        fixture.System.TryExploreLocalPlace(fixture.Expedition, fixture.Entry, out _);

        Assert.That(fixture.CountEvents<ExpeditionObjectiveCompletedEvent>(), Is.EqualTo(1));
    }

    [Test]
    public void ReturnStartedEventRecordedAfterRealTravelStarts()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);
        fixture.AddReturnKnowledge();

        Assert.That(fixture.System.TryBeginReturn(fixture.Expedition, out string reason), Is.True, reason);
        ExpeditionReturnStartedEvent recorded = fixture.SingleEvent<ExpeditionReturnStartedEvent>();
        Assert.That(fixture.Member.IsTraveling, Is.True);
        Assert.That(recorded.ExpeditionId, Is.EqualTo(fixture.Expedition.ExpeditionId));
    }

    [Test]
    public void CompletedEventPreservesParticipantsAndExpeditionId()
    {
        CorrectionFixture fixture = new CorrectionFixture(
            ExpeditionObjectiveRuntime.Explore(),
            ExpeditionState.Returning,
            includeSupport: true);
        fixture.MoveMembersToOrigin();

        Assert.That(fixture.System.ReconcileAfterTravel(), Has.Count.EqualTo(1));
        ExpeditionCompletedEvent recorded = fixture.SingleEvent<ExpeditionCompletedEvent>();
        Assert.That(recorded.ExpeditionId, Is.EqualTo(fixture.Expedition.ExpeditionId));
        Assert.That(recorded.PerformerRuntimeIds, Is.EqualTo(new[] { fixture.Member.RuntimeId }));
        Assert.That(recorded.SupportRuntimeIds, Is.EqualTo(new[] { fixture.Support.RuntimeId }));
        Assert.That(recorded.GetParticipants(), Has.Count.EqualTo(2));
        Assert.That(fixture.Store.GetById(fixture.Expedition.ExpeditionId), Is.Null);
        SimulationInvariantValidator.ValidateExpedition(
            fixture.Expedition,
            fixture.World.IdentityRegistry,
            fixture.Topologies,
            fixture.Store);
    }

    [Test]
    public void CompletedExpeditionDoesNotInventDecision()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.Returning);
        fixture.MoveMembersToOrigin();

        fixture.System.ReconcileAfterTravel();

        Assert.That(fixture.SingleEvent<ExpeditionCompletedEvent>().OriginDecisionId, Is.Null);
    }

    [Test]
    public void OriginDecisionIdIsPreservedWhenPresent()
    {
        CorrectionFixture fixture = new CorrectionFixture(
            ExpeditionObjectiveRuntime.Explore(),
            ExpeditionState.AtSite,
            originDecisionId: "decision-expedition-review");

        fixture.System.TryBeginExploration(fixture.Expedition, out _);

        Assert.That(fixture.SingleEvent<ExpeditionExplorationStartedEvent>().OriginDecisionId, Is.EqualTo("decision-expedition-review"));
    }

    [Test]
    public void RecordingFailureDoesNotUndoWorldMutation()
    {
        FailingEventRecorder recorder = new FailingEventRecorder();
        CorrectionFixture fixture = new CorrectionFixture(
            ExpeditionObjectiveRuntime.Explore(),
            ExpeditionState.AtSite,
            eventRecorder: recorder);
        LogAssert.Expect(LogType.Warning, new Regex("exploration-started event could not be recorded"));

        Assert.That(fixture.System.TryBeginExploration(fixture.Expedition, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Exploring));
        Assert.That(recorder.Attempts, Is.EqualTo(1));
    }

    [Test]
    public void ExistingAbstractExplorationStillWorksWithoutTopology()
    {
        CorrectionFixture fixture = new CorrectionFixture(
            ExpeditionObjectiveRuntime.Explore(2),
            ExpeditionState.Exploring,
            detailed: false);

        Assert.That(fixture.System.TryContinueExploration(fixture.Expedition, out string firstReason), Is.True, firstReason);
        Assert.That(fixture.System.TryContinueExploration(fixture.Expedition, out string secondReason), Is.True, secondReason);
        Assert.That(fixture.Expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void ExistingReturnRegressionsRemainGreen()
    {
        CorrectionFixture fixture = new CorrectionFixture(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);
        fixture.AddReturnKnowledge();

        Assert.That(fixture.System.TryBeginReturn(fixture.Expedition, out string reason), Is.True, reason);
        Assert.That(fixture.Expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(fixture.Member.DestinationLocation, Is.SameAs(fixture.World.CityA.Location));
        Assert.That(fixture.Member.CurrentLocation, Is.Null);
    }

    private sealed class CorrectionFixture
    {
        public SpatialTravelFixture World { get; }
        public NpcRuntime Member { get; }
        public NpcRuntime Support { get; }
        public PlaceContentStore Content { get; }
        public LocalTopologyStore Topologies { get; }
        public LocalTopologyRuntime Topology { get; }
        public LocalPlaceRuntime Entry { get; }
        public LocalPlaceRuntime Hall { get; }
        public LocalPlaceRuntime Treasury { get; }
        public LocalTopologyConnectionRuntime EntryToHall { get; }
        public LocalTopologyConnectionRuntime HallToTreasury { get; }
        public ExpeditionStore Store { get; private set; }
        public ExpeditionSystem System { get; private set; }
        public ExpeditionRuntime Expedition { get; private set; }
        public ExplorableSiteRuntime Site => World.Site;
        public PlaceContentOwnerReference SiteOwner => PlaceContentOwnerReference.ForExplorableSite(Site);

        public CorrectionFixture(
            ExpeditionObjectiveRuntime objective,
            ExpeditionState state = ExpeditionState.Exploring,
            bool detailed = true,
            bool includeSupport = false,
            string originDecisionId = null,
            IDomainEventRecorder eventRecorder = null)
        {
            World = new SpatialTravelFixture();
            Member = World.CreateNpc("correction-performer", World.CityA, 100f);
            Member.SetCurrentPresence(World.Site.Location);
            if (includeSupport == true)
            {
                Support = World.CreateNpc("correction-support", World.CityA, 100f);
                Support.SetCurrentPresence(World.Site.Location);
            }

            Content = new PlaceContentStore(World.Records.Allocator, World.IdentityRegistry);
            if (detailed == true)
            {
                Topologies = new LocalTopologyStore(World.IdentityRegistry);
                Topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(World.Site), World.IdentityRegistry);
                Entry = new LocalPlaceRuntime("correction-entry", "Entry");
                Hall = new LocalPlaceRuntime("correction-hall", "Hall");
                Treasury = new LocalPlaceRuntime("correction-treasury", "Treasury");
                EntryToHall = new LocalTopologyConnectionRuntime("correction-entry-hall", Entry, Hall, 1f);
                HallToTreasury = new LocalTopologyConnectionRuntime("correction-hall-treasury", Hall, Treasury, 1f);
                Assert.That(Topology.AddPlace(Entry, null, true), Is.True);
                Assert.That(Topology.AddPlace(Hall, Entry), Is.True);
                Assert.That(Topology.AddPlace(Treasury, Hall), Is.True);
                Assert.That(Topology.AddConnection(EntryToHall), Is.True);
                Assert.That(Topology.AddConnection(HallToTreasury), Is.True);
                Assert.That(Topologies.Add(Topology), Is.True);
            }

            Store = new ExpeditionStore();
            System = CreateSystem(eventRecorder ?? World.Records.EventRecorder);
            Expedition = CreateExpedition(objective, state, originDecisionId);
            Assert.That(Store.Add(Expedition), Is.True);
        }

        public void ReplaceExpedition(ExpeditionObjectiveRuntime objective)
        {
            Store = new ExpeditionStore();
            System = CreateSystem(World.Records.EventRecorder);
            Expedition = CreateExpedition(objective, ExpeditionState.Exploring, null);
            Assert.That(Store.Add(Expedition), Is.True);
        }

        public void Enter()
        {
            Assert.That(System.TryExploreLocalPlace(Expedition, Entry, out string reason), Is.True, reason);
        }

        public PlaceContentOwnerReference Owner(LocalPlaceRuntime place)
        {
            return PlaceContentOwnerReference.ForLocalPlace(place);
        }

        public NotableItemRuntime AddNotable(PlaceContentOwnerReference owner, ItemData definition)
        {
            NotableItemRuntime notable = Content.CreateNotableItem(definition);
            Assert.That(Content.TryAddNotable(owner, notable, out string reason), Is.True, reason);
            return notable;
        }

        public PlaceOppositionRuntime AddOpposition(PlaceContentOwnerReference owner, string runtimeId)
        {
            PlaceOppositionRuntime opposition = new PlaceOppositionRuntime(runtimeId);
            opposition.AddAggregateParticipant(new AggregateParticipantSnapshot(runtimeId + "-aggregate", 1f));
            Assert.That(Content.TryAddOpposition(owner, opposition, out string reason), Is.True, reason);
            return opposition;
        }

        public bool Resolve(PlaceContentOwnerReference owner, PlaceOppositionRuntime opposition, string conflictId)
        {
            ConflictResolutionService resolver = new ConflictResolutionService(
                new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));
            return System.TryResolvePlaceOpposition(
                Expedition,
                owner,
                opposition,
                opposition.CreateConflict(conflictId, new[] { Member }),
                resolver,
                out _,
                out _);
        }

        public void AddReturnKnowledge()
        {
            Member.SpatialKnowledge.DiscoverLocation(World.Site.Location.RuntimeId);
            Member.SpatialKnowledge.DiscoverLocation(World.CityA.Location.RuntimeId);
            Member.SpatialKnowledge.DiscoverRoute(World.SiteToCityRoute.RuntimeId);
            if (Support != null)
            {
                Support.SpatialKnowledge.DiscoverLocation(World.Site.Location.RuntimeId);
                Support.SpatialKnowledge.DiscoverLocation(World.CityA.Location.RuntimeId);
                Support.SpatialKnowledge.DiscoverRoute(World.SiteToCityRoute.RuntimeId);
            }
        }

        public void MoveMembersToOrigin()
        {
            Member.SetCurrentPresence(World.CityA.Location, World.CityA);
            Support?.SetCurrentPresence(World.CityA.Location, World.CityA);
        }

        public T SingleEvent<T>() where T : DomainEvent
        {
            T found = null;
            foreach (DomainEvent domainEvent in World.Records.Events.Events)
            {
                if (domainEvent is T typed)
                {
                    Assert.That(found, Is.Null, "Expected only one event of type " + typeof(T).Name);
                    found = typed;
                }
            }

            Assert.That(found, Is.Not.Null);
            return found;
        }

        public int CountEvents<T>() where T : DomainEvent
        {
            int count = 0;
            foreach (DomainEvent domainEvent in World.Records.Events.Events)
            {
                if (domainEvent is T)
                {
                    count++;
                }
            }

            return count;
        }

        private ExpeditionRuntime CreateExpedition(
            ExpeditionObjectiveRuntime objective,
            ExpeditionState state,
            string originDecisionId)
        {
            List<string> members = new List<string> { Member.RuntimeId };
            List<string> supports = new List<string>();
            if (Support != null)
            {
                members.Add(Support.RuntimeId);
                supports.Add(Support.RuntimeId);
            }

            return new ExpeditionRuntime(
                "correction-expedition",
                World.Site.RuntimeId,
                World.CityA.Location.RuntimeId,
                World.Site.Location.RuntimeId,
                World.SiteRoute.RuntimeId,
                state == ExpeditionState.Preparing ? null : "completed-travel-party",
                originDecisionId,
                members,
                new[] { Member.RuntimeId },
                supports,
                state,
                objective);
        }

        private ExpeditionSystem CreateSystem(IDomainEventRecorder eventRecorder)
        {
            TravelPartyStore parties = new TravelPartyStore();
            TravelPartySystem travelParties = new TravelPartySystem(
                parties,
                World.Records.Allocator,
                World.IdentityRegistry,
                World.Travel,
                World.Records.Time,
                World.Records.Sequence,
                World.Records.EventRecorder);
            World.SetTravelPartySystem(travelParties);
            return new ExpeditionSystem(
                Store,
                World.Records.Allocator,
                World.IdentityRegistry,
                World.Sites,
                travelParties,
                parties,
                World.Knowledge,
                World.Records.Time,
                eventRecorder,
                null,
                Content,
                Topologies);
        }
    }

    private sealed class FailingEventRecorder : IDomainEventRecorder
    {
        public int Attempts { get; private set; }

        public bool Record(Func<string, long, long, DomainEvent> createEvent)
        {
            Attempts++;
            return false;
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
