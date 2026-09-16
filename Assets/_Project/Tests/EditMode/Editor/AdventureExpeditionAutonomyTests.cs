using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class AdventureExpeditionAutonomyTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void AutonomousNpcCanStartExploreExpeditionForKnownSite()
    {
        Fixture f = new Fixture();
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.True);
        Assert.That(f.Expeditions.ActiveExpeditions, Has.Count.EqualTo(1));
    }

    [Test]
    public void AutonomousNpcCannotStartForUnknownSite()
    {
        Fixture f = new Fixture(knowSite: false);
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.False);
        Assert.That(f.Expeditions.ActiveExpeditions, Is.Empty);
    }

    [Test]
    public void AutonomousStartRequiresKnownRoute()
    {
        Fixture f = new Fixture(knowRoute: false);
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.False);
    }

    [Test]
    public void AutonomousDecisionIsRecordedBeforeExpeditionMutation()
    {
        Fixture f = new Fixture();
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.True);
        ExpeditionRuntime expedition = f.Expeditions.ActiveExpeditions[0];
        Assert.That(f.Records.Decisions.TryGetDecision(expedition.OriginDecisionId, out NpcDecisionRecord decision), Is.True);
        Assert.That(decision.DecisionType, Is.EqualTo(NpcDecisionType.ExpeditionStart));
    }

    [Test]
    public void FailedExecutionPreservesDecisionWithoutSuccessEvent()
    {
        Fixture f = new Fixture();
        SpatialLocationRuntime rumorLocation = new SpatialLocationRuntime("location-rumor");
        f.Network.RegisterLocation(rumorLocation);
        SpatialRouteRuntime rumorRoute = new SpatialRouteRuntime("route-rumor", f.Origin, rumorLocation, 1);
        f.Network.RegisterRoute(rumorRoute);
        f.Actor.SpatialKnowledge.DiscoverRoute(rumorRoute.RuntimeId);
        f.Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            f.Site.RuntimeId, rumorLocation.RuntimeId, 2, 2, ExplorableSiteKnowledgeSource.DirectObservation));
        int eventCount = f.Records.Events.Events.Count;

        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.False);
        int decisionCount = f.Records.Decisions.Decisions.Count;
        Assert.That(decisionCount, Is.GreaterThan(0));
        Assert.That(f.Records.Events.Events, Has.Count.EqualTo(eventCount));
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.False);
        Assert.That(f.Records.Decisions.Decisions, Has.Count.EqualTo(decisionCount));
    }

    [Test]
    public void NewExpeditionMemberSkipsNormalNpcActionSameDay()
    {
        Fixture f = new Fixture();
        SimulationRuntime runtime = f.CreateSimulationRuntime();
        runtime.AdvanceDay();
        Assert.That(f.Actor.CurrentActionRuntime, Is.Null);
        Assert.That(f.Expeditions.IsNpcOnActiveExpedition(f.Actor.RuntimeId), Is.True);
    }

    [Test]
    public void ActiveExpeditionMemberStillSkipsNormalAction()
    {
        Fixture f = new Fixture();
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(3));
        SimulationRuntime runtime = f.CreateSimulationRuntime();
        runtime.AdvanceDay();
        Assert.That(f.Actor.CurrentActionRuntime, Is.Null);
    }

    [Test]
    public void AutonomousExpeditionBeginsExplorationAfterArrival()
    {
        Fixture f = new Fixture();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Exploring));
        Assert.That(f.Records.Decisions.Decisions, Has.Some.Matches<NpcDecisionRecord>(d => d.DecisionType == NpcDecisionType.ExpeditionExplore));
    }

    [Test]
    public void ArrivalAtDetailedSiteStillDoesNotRevealTopology()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(
            out LocalPlaceRuntime root,
            out LocalPlaceRuntime middle,
            out LocalPlaceRuntime target,
            true);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(
            ExpeditionObjectiveRuntime.Explore(),
            ExpeditionState.AtSite);

        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, root.RuntimeId), Is.False);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, middle.RuntimeId), Is.False);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, target.RuntimeId), Is.False);
        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.Null);
        Assert.That(topology.IsPublished, Is.True);
    }

    [Test]
    public void BeginningAutonomousExplorationRevealsPublishedEntryPoints()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(
            out LocalPlaceRuntime root,
            out LocalPlaceRuntime middle,
            out LocalPlaceRuntime target,
            true);
        LocalTopologyConnectionRuntime rootConnection = topology.Connections[0];
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, root.RuntimeId), Is.True);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsConnection(f.Site.RuntimeId, rootConnection.RuntimeId), Is.True);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, middle.RuntimeId), Is.True);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, target.RuntimeId), Is.False);
    }

    [Test]
    public void BeginningExplorationDoesNotRevealWholeTopology()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(
            out LocalPlaceRuntime root,
            out LocalPlaceRuntime middle,
            out LocalPlaceRuntime target,
            true);
        LocalTopologyConnectionRuntime hiddenConnection = topology.Connections[1];
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, root.RuntimeId), Is.True);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, middle.RuntimeId), Is.True);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, target.RuntimeId), Is.False);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsConnection(f.Site.RuntimeId, hiddenConnection.RuntimeId), Is.False);
    }

    [Test]
    public void FreshNpcCanEnterDetailedSiteWithoutPreseededLocalKnowledge()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime root, out _, out _, true);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(
            ExpeditionObjectiveRuntime.Explore(2),
            ExpeditionState.AtSite);

        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.Null);
        f.Records.Time.AdvanceDay();
        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(root.RuntimeId));
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, root.RuntimeId), Is.True);
        Assert.That(topology.IsPublished, Is.True);
    }

    [Test]
    public void MultipleEntryPointsBecomeInitiallyObservable()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime first, out _, out _, false);
        LocalPlaceRuntime second = new LocalPlaceRuntime("local-entry-second", "Second Entry");
        Assert.That(f.Topologies.TryAddPlace(topology, second, true, out string diagnostic), Is.True, diagnostic);
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, first.RuntimeId), Is.True);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, second.RuntimeId), Is.True);
    }

    [Test]
    public void TopologyWithoutEntryPointDoesNotTeleportExpedition()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForExplorableSite(f.Site),
            f.Registry);
        LocalPlaceRuntime hiddenRoot = new LocalPlaceRuntime("local-no-entry", "No Entry");
        topology.AddPlace(hiddenRoot);
        Assert.That(f.Topologies.TryAddTopology(topology, out string diagnostic), Is.True, diagnostic);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(
            ExpeditionObjectiveRuntime.Explore(2),
            ExpeditionState.AtSite);

        f.Autonomy.AdvanceActiveExpeditions();
        f.Records.Time.AdvanceDay();
        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.Null);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(f.Actor.CurrentLocation, Is.Null);
        Assert.That(f.Actor.IsTraveling, Is.True);
    }

    [Test]
    public void EntryBootstrapUsesDirectObservationSource()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime root, out _, out _, true);
        LocalTopologyConnectionRuntime connection = topology.Connections[0];
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(), ExpeditionState.AtSite);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(f.Actor.LocalTopologyKnowledge.TryGetPlaceObservation(
            f.Site.RuntimeId, root.RuntimeId, out LocalPlaceKnowledgeObservation place), Is.True);
        Assert.That(place.Source, Is.EqualTo(LocalTopologyKnowledgeSource.DirectObservation));
        Assert.That(f.Actor.LocalTopologyKnowledge.TryGetConnectionObservation(
            f.Site.RuntimeId, connection.RuntimeId, out LocalConnectionKnowledgeObservation observedConnection), Is.True);
        Assert.That(observedConnection.Source, Is.EqualTo(LocalTopologyKnowledgeSource.DirectObservation));
    }

    [Test]
    public void AbstractSiteAutonomouslyAdvances()
    {
        Fixture f = new Fixture();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(2));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.ExplorationProgress, Is.EqualTo(1));
    }

    [Test]
    public void DetailedSiteUsesKnownTopologyOnly()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime root, out _, out _, false);
        f.KnowPlace(topology, root);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(2));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(root.RuntimeId));
    }

    [Test]
    public void DetailedSiteDoesNotUseUnknownTruthConnection()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime root, out _, out LocalPlaceRuntime target, true);
        f.KnowPlace(topology, root);
        f.KnowPlace(topology, target);
        f.KnowCommonResource("item-target", target.RuntimeId);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Retrieve("item-target"));
        expedition.TrySetCurrentLocalPlace(root.RuntimeId);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(root.RuntimeId));
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
    }

    [Test]
    public void AutonomyPrefersUnvisitedKnownProgressOverRevisit()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishBranchingTopology(
            out LocalPlaceRuntime root,
            out LocalPlaceRuntime visited,
            out LocalPlaceRuntime unvisited,
            out LocalTopologyConnectionRuntime toVisited,
            out LocalTopologyConnectionRuntime toUnvisited);
        f.KnowConnection(topology, toVisited);
        f.KnowConnection(topology, toUnvisited);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(5));
        expedition.TrySetCurrentLocalPlace(visited.RuntimeId);
        expedition.TrySetCurrentLocalPlace(root.RuntimeId);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(unvisited.RuntimeId));
    }

    [Test]
    public void KnownPathCanGuideTowardKnownTarget()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime root, out LocalPlaceRuntime middle, out LocalPlaceRuntime target, true);
        foreach (LocalTopologyConnectionRuntime connection in topology.Connections)
        {
            f.KnowConnection(topology, connection);
        }
        f.KnowCommonResource("item-target", target.RuntimeId);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Retrieve("item-target"));
        expedition.TrySetCurrentLocalPlace(root.RuntimeId);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(middle.RuntimeId));
    }

    [Test]
    public void NoKnownPathDoesNotTeleportTowardTarget()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime root, out _, out LocalPlaceRuntime target, true);
        f.KnowPlace(topology, root);
        f.KnowPlace(topology, target);
        f.KnowCommonResource("item-target", target.RuntimeId);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Retrieve("item-target"));
        expedition.TrySetCurrentLocalPlace(root.RuntimeId);

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.CurrentLocalPlaceRuntimeId, Is.EqualTo(root.RuntimeId));
    }

    [Test]
    public void KnownCurrentOppositionCanGenerateConflictDecision()
    {
        Fixture f = new Fixture();
        PlaceOppositionRuntime opposition = f.AddKnownOpposition();
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Eliminate(opposition.RuntimeId));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(f.Records.Decisions.Decisions, Has.Some.Matches<NpcDecisionRecord>(d => d.DecisionType == NpcDecisionType.ExpeditionResolveOpposition));
        Assert.That(f.Autonomy.LastConflict, Is.Not.Null);
    }

    [Test]
    public void UnknownOppositionCannotGenerateConflictDecision()
    {
        Fixture f = new Fixture();
        PlaceOppositionRuntime opposition = f.AddOpposition(false);
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Eliminate(opposition.RuntimeId));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(f.Records.Decisions.Decisions, Has.None.Matches<NpcDecisionRecord>(d => d.DecisionType == NpcDecisionType.ExpeditionResolveOpposition));
        Assert.That(f.Autonomy.LastConflict, Is.Null);
    }

    [Test]
    public void AutonomousConflictUsesNoForcedOutcome()
    {
        Fixture f = new Fixture();
        PlaceOppositionRuntime opposition = f.AddKnownOpposition();
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Eliminate(opposition.RuntimeId));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(f.Autonomy.LastConflictConstraints, Is.Null);
        Assert.That(f.Autonomy.LastConflictResult.OutcomeSource, Is.EqualTo(ConflictOutcomeSource.Simulated));
    }

    [Test]
    public void AutonomousConflictParticipantsBelongToExpedition()
    {
        Fixture f = new Fixture();
        NpcRuntime support = f.CreateSupport();
        PlaceOppositionRuntime opposition = f.AddKnownOpposition();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Eliminate(opposition.RuntimeId), support: support);
        f.Autonomy.AdvanceActiveExpeditions();
        ConflictSide side = f.Autonomy.LastConflict.Sides[0];
        Assert.That(side.Participants, Has.All.Matches<ConflictParticipantReference>(p => expedition.MemberRuntimeIds.Contains(p.SourceId)));
    }

    [Test]
    public void AutonomousConflictCannotIntroduceAggregateAlly()
    {
        Fixture f = new Fixture();
        PlaceOppositionRuntime opposition = f.AddKnownOpposition();
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Eliminate(opposition.RuntimeId));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(f.Autonomy.LastConflict.Sides[0].Participants, Has.None.Matches<ConflictParticipantReference>(p => p.IsAggregate));
    }

    [Test]
    public void KnownAccessibleCommonResourceCanBeRetrieved()
    {
        Fixture f = new Fixture();
        ItemData item = f.AddKnownResource("item-ore", 2);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Retrieve(item.DefinitionId));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(f.Actor.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void KnownExactNotableItemCanBeRetrieved()
    {
        Fixture f = new Fixture();
        NotableItemRuntime notable = f.AddKnownNotable();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.RetrieveNotable(notable.RuntimeId));
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(notable.CustodianNpcRuntimeId, Is.EqualTo(f.Actor.RuntimeId));
        Assert.That(expedition.IsObjectiveComplete, Is.True);
    }

    [Test]
    public void StaleRetrieveKnowledgeFailsWithoutDuplication()
    {
        Fixture f = new Fixture();
        f.KnowCommonResource("item-missing", null);
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Retrieve("item-missing"));
        f.Autonomy.AdvanceActiveExpeditions();
        f.Autonomy.AdvanceActiveExpeditions();
        int retrieveDecisions = 0;
        foreach (NpcDecisionRecord decision in f.Records.Decisions.Decisions)
        {
            if (decision.DecisionType == NpcDecisionType.ExpeditionRetrieve) retrieveDecisions++;
        }
        Assert.That(retrieveDecisions, Is.EqualTo(1));
        Assert.That(f.Actor.Inventory.Items, Is.Empty);
    }

    [Test]
    public void FailedExecutionIsNotRetriedRepeatedlySameDay()
    {
        Fixture f = new Fixture();
        f.KnowCommonResource("item-missing", null);
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Retrieve("item-missing"));

        f.Autonomy.AdvanceActiveExpeditions();
        int decisionsAfterFailure = f.Records.Decisions.Decisions.Count;
        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(f.Records.Decisions.Decisions, Has.Count.EqualTo(decisionsAfterFailure));
    }

    [Test]
    public void FailedExecutionCanRetryOnNextDay()
    {
        Fixture f = new Fixture(includeReturnRoute: false);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore());
        expedition.TryMarkObjectiveComplete();

        f.Autonomy.AdvanceActiveExpeditions();
        int returnDecisionsAfterFailure = f.Records.Decisions.Decisions.Count(
            decision => decision.DecisionType == NpcDecisionType.ExpeditionReturn);
        SpatialRouteRuntime returnRoute = new SpatialRouteRuntime(
            "route-return-next-day", f.SiteLocation, f.Origin, 1);
        f.Network.RegisterRoute(returnRoute);
        f.Actor.SpatialKnowledge.DiscoverRoute(returnRoute.RuntimeId);
        f.Records.Time.AdvanceDay();
        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(returnDecisionsAfterFailure, Is.EqualTo(1));
        Assert.That(f.Records.Decisions.Decisions.Count(
            decision => decision.DecisionType == NpcDecisionType.ExpeditionReturn), Is.EqualTo(2));
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
    }

    [Test]
    public void FailedStartCanRetryNextDayAfterTruthChanges()
    {
        Fixture f = new Fixture();
        SpatialLocationRuntime rumorLocation = new SpatialLocationRuntime("location-rumor-next-day");
        f.Network.RegisterLocation(rumorLocation);
        SpatialRouteRuntime rumorRoute = new SpatialRouteRuntime(
            "route-rumor-next-day", f.Origin, rumorLocation, 1);
        f.Network.RegisterRoute(rumorRoute);
        f.Actor.SpatialKnowledge.DiscoverRoute(rumorRoute.RuntimeId);
        f.Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            f.Site.RuntimeId,
            rumorLocation.RuntimeId,
            0,
            0,
            ExplorableSiteKnowledgeSource.DirectObservation));

        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.False);
        f.Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            f.Site.RuntimeId,
            f.Site.Location.RuntimeId,
            1,
            1,
            ExplorableSiteKnowledgeSource.DirectObservation));
        f.Records.Time.AdvanceDay();

        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.True);
    }

    [Test]
    public void FailedReturnCanRetryNextDayAfterRouteBecomesKnown()
    {
        Fixture f = new Fixture(includeReturnRoute: false);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore());
        expedition.TryMarkObjectiveComplete();

        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Exploring));
        SpatialRouteRuntime returnRoute = new SpatialRouteRuntime(
            "route-return-revealed", f.SiteLocation, f.Origin, 1);
        f.Network.RegisterRoute(returnRoute);
        f.Actor.SpatialKnowledge.DiscoverRoute(returnRoute.RuntimeId);
        f.Records.Time.AdvanceDay();

        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(f.Actor.IsTraveling, Is.True);
    }

    [Test]
    public void PreviousFailedDecisionRemainsInHistoryAfterRetry()
    {
        Fixture f = new Fixture();
        SpatialLocationRuntime rumorLocation = new SpatialLocationRuntime("location-rumor-history");
        f.Network.RegisterLocation(rumorLocation);
        SpatialRouteRuntime rumorRoute = new SpatialRouteRuntime(
            "route-rumor-history", f.Origin, rumorLocation, 1);
        f.Network.RegisterRoute(rumorRoute);
        f.Actor.SpatialKnowledge.DiscoverRoute(rumorRoute.RuntimeId);
        f.Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            f.Site.RuntimeId,
            rumorLocation.RuntimeId,
            0,
            0,
            ExplorableSiteKnowledgeSource.DirectObservation));

        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.False);
        f.Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            f.Site.RuntimeId,
            f.Site.Location.RuntimeId,
            1,
            1,
            ExplorableSiteKnowledgeSource.DirectObservation));
        f.Records.Time.AdvanceDay();
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.True);

        Assert.That(f.Records.Decisions.Decisions, Has.Count.EqualTo(2));
        Assert.That(f.Records.Decisions.Decisions[0].DecisionType, Is.EqualTo(NpcDecisionType.ExpeditionStart));
        Assert.That(f.Records.Decisions.Decisions[1].DecisionType, Is.EqualTo(NpcDecisionType.ExpeditionStart));
    }

    [Test]
    public void DailyResetDoesNotClearActiveExpeditionReservationIncorrectly()
    {
        Fixture f = new Fixture();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(3));

        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(f.Autonomy.IsReservedToday(f.Actor.RuntimeId), Is.True);
        f.Records.Time.AdvanceDay();
        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.IsActive, Is.True);
        Assert.That(f.Autonomy.IsReservedToday(f.Actor.RuntimeId), Is.True);
    }

    [Test]
    public void AutonomousScoutCanProgressUsingExistingExplorationSemantics()
    {
        Fixture f = new Fixture();
        AdventureAutonomySystem candidateSystem = new AdventureAutonomySystem(
            new AdventureAutonomySettings
            {
                enableScout = true,
                exploreUtility = 1f,
                scoutUtility = 20f
            });
        f.ReplaceAutonomy(candidateSystem);

        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.True);
        ExpeditionRuntime expedition = f.Expeditions.ActiveExpeditions[0];
        Assert.That(expedition.Objective.ObjectiveType, Is.EqualTo(ExpeditionObjectiveType.Scout));
        f.PartySystem.AdvanceParties();
        f.PartySystem.AdvanceParties();
        IReadOnlyList<NpcRuntime> arrived = f.PartySystem.AdvanceParties();
        f.ExpeditionSystem.ReconcileAfterTravel(arrived);
        f.Autonomy.AdvanceActiveExpeditions();
        f.Records.Time.AdvanceDay();
        f.Autonomy.AdvanceActiveExpeditions();

        Assert.That(expedition.ExplorationProgress, Is.EqualTo(1));
    }

    [Test]
    public void ObjectiveCompletionCanLeadToReturnDecision()
    {
        Fixture f = new Fixture();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore());
        expedition.TryMarkObjectiveComplete();
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(f.Records.Decisions.Decisions, Has.Some.Matches<NpcDecisionRecord>(d => d.DecisionType == NpcDecisionType.ExpeditionReturn));
    }

    [Test]
    public void NoUsefulKnownProgressCanLeadToReturn()
    {
        Fixture f = new Fixture();
        LocalTopologyRuntime topology = f.PublishTopology(out LocalPlaceRuntime root, out _, out _, false);
        f.KnowPlace(topology, root);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(3));
        expedition.TrySetCurrentLocalPlace(root.RuntimeId);
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
    }

    [Test]
    public void SevereInjuryCanLeadToReturnWhenConfigured()
    {
        Fixture f = new Fixture();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(3));
        f.Actor.TryApplyInjury(NpcInjurySeverity.SeriouslyInjured);
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
    }

    [Test]
    public void ReturnStillRequiresRealKnownMacroRoute()
    {
        Fixture f = new Fixture();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore());
        expedition.TryMarkObjectiveComplete();
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(f.Actor.IsTraveling, Is.True);
        Assert.That(f.Actor.TravelRouteRuntimeId, Is.EqualTo(f.ReturnRoute.RuntimeId));
    }

    [Test]
    public void MissingReturnRouteDoesNotTeleport()
    {
        Fixture f = new Fixture(includeReturnRoute: false);
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore());
        expedition.TryMarkObjectiveComplete();
        f.Autonomy.AdvanceActiveExpeditions();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Exploring));
        Assert.That(f.Actor.CurrentLocation, Is.SameAs(f.Site.Location));
        Assert.That(f.Actor.IsTraveling, Is.False);
    }

    [Test]
    public void AutonomousCompletedExpeditionLeavesActiveStore()
    {
        Fixture f = new Fixture();
        ExpeditionRuntime expedition = f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore());
        expedition.TryMarkObjectiveComplete();
        f.Autonomy.AdvanceActiveExpeditions();
        f.PartySystem.AdvanceParties();
        IReadOnlyList<NpcRuntime> arrived = f.PartySystem.AdvanceParties();
        f.ExpeditionSystem.ReconcileAfterTravel(arrived);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Completed));
        Assert.That(f.Expeditions.ActiveExpeditions.Any(e => e.ExpeditionId == expedition.ExpeditionId), Is.False);
    }

    [Test]
    public void AutonomousDecisionsPreserveDecisionMakerSupportRoles()
    {
        Fixture f = new Fixture();
        NpcRuntime support = f.CreateSupport();
        f.CreateActiveExpedition(ExpeditionObjectiveRuntime.Explore(2), support: support);
        f.Autonomy.AdvanceActiveExpeditions();
        NpcDecisionRecord decision = f.Records.Decisions.Decisions[f.Records.Decisions.Decisions.Count - 1];
        Assert.That(decision.DecisionParticipants, Has.Some.Matches<NpcDecisionParticipant>(p => p.RuntimeId == f.Actor.RuntimeId && p.Role == NpcDecisionParticipantRole.DecisionMaker));
        Assert.That(decision.DecisionParticipants, Has.Some.Matches<NpcDecisionParticipant>(p => p.RuntimeId == support.RuntimeId && p.Role == NpcDecisionParticipantRole.Support));
    }

    [Test]
    public void TwoNpcsCannotBeAutonomouslyReservedIntoIncompatibleActivitiesSameDay()
    {
        Fixture f = new Fixture();
        NpcRuntime support = f.CreateSupport();
        AdventureAutonomySystem candidateSystem = new AdventureAutonomySystem(null, new PairPolicy(support));
        f.ReplaceAutonomy(candidateSystem);
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(f.Actor, f.Npcs), Is.True);
        Assert.That(f.Autonomy.TryStartAutonomousExpedition(support, f.Npcs), Is.False);
        Assert.That(f.Expeditions.ActiveExpeditions, Has.Count.EqualTo(1));
    }

    [Test]
    public void CandidateEvaluationRemainsPure()
    {
        Fixture f = new Fixture();
        int decisions = f.Records.Decisions.Decisions.Count;
        int expeditions = f.Expeditions.ActiveExpeditions.Count;
        f.CandidateSystem.BuildCandidates(f.Actor, f.Network, f.Npcs);
        Assert.That(f.Records.Decisions.Decisions, Has.Count.EqualTo(decisions));
        Assert.That(f.Expeditions.ActiveExpeditions, Has.Count.EqualTo(expeditions));
    }

    [Test]
    public void RecordingRemainsNonMutating()
    {
        Fixture f = new Fixture();
        AdventureCandidate candidate = f.CandidateSystem.ChooseCandidate(f.CandidateSystem.BuildCandidates(f.Actor, f.Network, f.Npcs));
        NpcDecisionRecord decision = f.CandidateSystem.RecordStartDecision(f.Actor, candidate, f.Records.DecisionRecorder);
        Assert.That(decision, Is.Not.Null);
        Assert.That(f.Expeditions.ActiveExpeditions, Is.Empty);
        Assert.That(f.Actor.IsTraveling, Is.False);
    }

    private sealed class PairPolicy : IAdventurePartyAssemblyPolicy
    {
        private readonly NpcRuntime support;
        public PairPolicy(NpcRuntime support) { this.support = support; }
        public IReadOnlyList<AdventurePartySelection> Assemble(NpcRuntime decisionMaker, IReadOnlyList<NpcRuntime> availableNpcs)
        {
            return new[]
            {
                new AdventurePartySelection(decisionMaker, NpcDecisionParticipantRole.Performer),
                new AdventurePartySelection(support, NpcDecisionParticipantRole.Support)
            };
        }
    }

    private sealed class ItemResolver : IAdventureItemDefinitionResolver
    {
        private readonly Dictionary<string, ItemData> items = new Dictionary<string, ItemData>(StringComparer.Ordinal);
        public void Add(ItemData item) { items[item.DefinitionId] = item; }
        public bool TryResolveItem(string itemDefinitionId, out ItemData item) => items.TryGetValue(itemDefinitionId ?? string.Empty, out item);
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        public CapabilityEvaluationResult Evaluate(NpcRuntime participant, CapabilityEvaluationContext context = null)
        {
            return new CapabilityEvaluationResult(100f, 100f, null);
        }
    }

    private sealed class Fixture
    {
        public RecordFixture Records { get; } = SimulationTestFactory.CreateRecordFixture();
        public RuntimeIdentityRegistry Registry { get; } = new RuntimeIdentityRegistry();
        public SpatialLocationRuntime Origin { get; } = new SpatialLocationRuntime("location-origin");
        public SpatialLocationRuntime SiteLocation { get; } = new SpatialLocationRuntime("location-site");
        public SpatialNetworkRuntime Network { get; }
        public SpatialRouteRuntime OutboundRoute { get; }
        public SpatialRouteRuntime ReturnRoute { get; }
        public ExplorableSiteRuntime Site { get; }
        public ExplorableSiteStore Sites { get; } = new ExplorableSiteStore();
        public NpcRuntime Actor { get; }
        public List<NpcRuntime> Npcs { get; } = new List<NpcRuntime>();
        public TravelSystem Travel { get; }
        public TravelPartyStore Parties { get; } = new TravelPartyStore();
        public TravelPartySystem PartySystem { get; }
        public ExpeditionStore Expeditions { get; } = new ExpeditionStore();
        public PlaceContentStore Content { get; }
        public LocalTopologyStore Topologies { get; }
        public ExpeditionSystem ExpeditionSystem { get; }
        public AdventureAutonomySystem CandidateSystem { get; private set; }
        public AdventureExpeditionAutonomySystem Autonomy { get; private set; }
        public ItemResolver Items { get; } = new ItemResolver();
        private int expeditionSequence;

        public Fixture(bool knowSite = true, bool knowRoute = true, bool includeReturnRoute = true)
        {
            Network = new SpatialNetworkRuntime(Registry);
            Network.RegisterLocation(Origin);
            Network.RegisterLocation(SiteLocation);
            OutboundRoute = new SpatialRouteRuntime("route-outbound", Origin, SiteLocation, 2);
            Network.RegisterRoute(OutboundRoute);
            if (includeReturnRoute)
            {
                ReturnRoute = new SpatialRouteRuntime("route-return", SiteLocation, Origin, 1);
                Network.RegisterRoute(ReturnRoute);
            }

            Site = new ExplorableSiteRuntime("site-runtime", SimulationTestFactory.CreateExplorableSite("site-def"), SiteLocation);
            Registry.RegisterExplorableSite(Site);
            Sites.Add(Site);
            Actor = new NpcRuntime("npc-actor", SimulationTestFactory.CreateNpc("actor"));
            Actor.SetCurrentPresence(Origin);
            Registry.RegisterNpc(Actor);
            Npcs.Add(Actor);
            Actor.SpatialKnowledge.DiscoverLocation(Origin.RuntimeId);
            Actor.SpatialKnowledge.DiscoverLocation(SiteLocation.RuntimeId);
            if (knowRoute) Actor.SpatialKnowledge.DiscoverRoute(OutboundRoute.RuntimeId);
            if (ReturnRoute != null) Actor.SpatialKnowledge.DiscoverRoute(ReturnRoute.RuntimeId);
            if (knowSite)
            {
                Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
                    Site.RuntimeId, SiteLocation.RuntimeId, 0, 0, ExplorableSiteKnowledgeSource.InitialScenarioKnowledge));
            }

            Travel = new TravelSystem(Network, _ => null, 0f, Records.EventRecorder, null);
            PartySystem = new TravelPartySystem(
                Parties, Records.Allocator, Registry, Travel, Records.Time, Records.Sequence, Records.EventRecorder);
            Content = new PlaceContentStore(Records.Allocator, Registry);
            Topologies = new LocalTopologyStore(Registry);
            ExpeditionSystem = new ExpeditionSystem(
                Expeditions,
                Records.Allocator,
                Registry,
                Sites,
                PartySystem,
                Parties,
                new ExplorableSiteKnowledgeSystem(),
                Records.Time,
                Records.EventRecorder,
                null,
                Content,
                Topologies);
            CandidateSystem = new AdventureAutonomySystem();
            CreateAutonomy(CandidateSystem);
        }

        public void ReplaceAutonomy(AdventureAutonomySystem candidateSystem)
        {
            CandidateSystem = candidateSystem;
            CreateAutonomy(candidateSystem);
        }

        private void CreateAutonomy(AdventureAutonomySystem candidateSystem)
        {
            ConflictResolutionService conflictService = new ConflictResolutionService(
                new ConflictResolver(new FixedCapabilityModel(), new SequenceConflictRandomSource(0.5f, 0.5f)),
                null,
                Records.EventRecorder);
            Autonomy = new AdventureExpeditionAutonomySystem(
                candidateSystem,
                ExpeditionSystem,
                Expeditions,
                Sites,
                Topologies,
                Content,
                Registry,
                Network,
                Records.DecisionRecorder,
                Records.Time,
                conflictService,
                null,
                Items);
        }

        public NpcRuntime CreateSupport()
        {
            NpcRuntime support = new NpcRuntime("npc-support-" + Npcs.Count, SimulationTestFactory.CreateNpc("support-" + Npcs.Count));
            support.SetCurrentPresence(Actor.CurrentLocation);
            Registry.RegisterNpc(support);
            support.SpatialKnowledge.DiscoverRoute(OutboundRoute.RuntimeId);
            support.SpatialKnowledge.DiscoverLocation(Origin.RuntimeId);
            support.SpatialKnowledge.DiscoverLocation(SiteLocation.RuntimeId);
            if (ReturnRoute != null) support.SpatialKnowledge.DiscoverRoute(ReturnRoute.RuntimeId);
            support.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
                Site.RuntimeId, SiteLocation.RuntimeId, 0, 0, ExplorableSiteKnowledgeSource.InitialScenarioKnowledge));
            Npcs.Add(support);
            return support;
        }

        public ExpeditionRuntime CreateActiveExpedition(
            ExpeditionObjectiveRuntime objective,
            ExpeditionState state = ExpeditionState.Exploring,
            NpcRuntime support = null)
        {
            Actor.CancelTravel(Origin, null);
            Actor.SetCurrentPresence(SiteLocation);
            List<string> members = new List<string> { Actor.RuntimeId };
            List<string> supports = new List<string>();
            if (support != null)
            {
                support.CancelTravel(Origin, null);
                support.SetCurrentPresence(SiteLocation);
                members.Add(support.RuntimeId);
                supports.Add(support.RuntimeId);
            }

            ExpeditionRuntime expedition = new ExpeditionRuntime(
                "autonomy-expedition-" + (++expeditionSequence),
                Site.RuntimeId,
                Origin.RuntimeId,
                SiteLocation.RuntimeId,
                OutboundRoute.RuntimeId,
                null,
                "decision-origin",
                members,
                new[] { Actor.RuntimeId },
                supports,
                state,
                objective);
            Assert.That(Expeditions.Add(expedition), Is.True);
            return expedition;
        }

        public LocalTopologyRuntime PublishTopology(
            out LocalPlaceRuntime root,
            out LocalPlaceRuntime middle,
            out LocalPlaceRuntime target,
            bool connect)
        {
            LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(Site), Registry);
            root = new LocalPlaceRuntime("local-root", "Root");
            middle = new LocalPlaceRuntime("local-middle", "Middle");
            target = new LocalPlaceRuntime("local-target", "Target");
            topology.AddPlace(root, null, true);
            topology.AddPlace(middle);
            topology.AddPlace(target);
            if (connect)
            {
                topology.AddConnection(new LocalTopologyConnectionRuntime("connection-root-middle", root, middle, 1f));
                topology.AddConnection(new LocalTopologyConnectionRuntime("connection-middle-target", middle, target, 1f));
            }
            Assert.That(Topologies.TryAddTopology(topology, out string diagnostic), Is.True, diagnostic);
            return topology;
        }

        public LocalTopologyRuntime PublishBranchingTopology(
            out LocalPlaceRuntime root,
            out LocalPlaceRuntime visited,
            out LocalPlaceRuntime unvisited,
            out LocalTopologyConnectionRuntime toVisited,
            out LocalTopologyConnectionRuntime toUnvisited)
        {
            LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(Site), Registry);
            root = new LocalPlaceRuntime("local-root", "Root");
            visited = new LocalPlaceRuntime("local-visited", "Visited");
            unvisited = new LocalPlaceRuntime("local-unvisited", "Unvisited");
            topology.AddPlace(root, null, true);
            topology.AddPlace(visited);
            topology.AddPlace(unvisited);
            toVisited = new LocalTopologyConnectionRuntime("connection-visited", root, visited, 1f);
            toUnvisited = new LocalTopologyConnectionRuntime("connection-unvisited", root, unvisited, 1f);
            topology.AddConnection(toVisited);
            topology.AddConnection(toUnvisited);
            Assert.That(Topologies.TryAddTopology(topology, out string diagnostic), Is.True, diagnostic);
            return topology;
        }

        public void KnowPlace(LocalTopologyRuntime topology, LocalPlaceRuntime place)
        {
            new LocalTopologyKnowledgeSystem().RecordInitialScenarioKnowledge(Actor, topology, place);
        }

        public void KnowConnection(LocalTopologyRuntime topology, LocalTopologyConnectionRuntime connection)
        {
            new LocalTopologyKnowledgeSystem().RecordInitialScenarioKnowledge(Actor, topology, connection);
        }

        public void KnowCommonResource(string itemDefinitionId, string localPlaceRuntimeId)
        {
            Actor.AdventureSiteIntelKnowledge.RecordObservation(new AdventureCommonResourceObservation(
                Site.RuntimeId, localPlaceRuntimeId, itemDefinitionId, 1, null, 0, 0, AdventureIntelSource.InitialScenarioKnowledge));
        }

        public PlaceOppositionRuntime AddOpposition(bool known)
        {
            PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("opposition-runtime");
            opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("bandits", 1f, 1));
            Content.TryAddOpposition(Site, opposition, out _);
            if (known)
            {
                Actor.AdventureSiteIntelKnowledge.RecordObservation(new AdventureOppositionObservation(
                    Site.RuntimeId, null, opposition.RuntimeId, AdventureOppositionObservedState.Active,
                    0, 0, AdventureIntelSource.InitialScenarioKnowledge));
            }
            return opposition;
        }

        public PlaceOppositionRuntime AddKnownOpposition() => AddOpposition(true);

        public ItemData AddKnownResource(string id, int amount)
        {
            ItemData item = SimulationTestFactory.CreateItem(id);
            Items.Add(item);
            Content.TryAddStack(Site, item, amount, PlaceContentPersistencePolicy.Durable, out _);
            KnowCommonResource(id, null);
            return item;
        }

        public NotableItemRuntime AddKnownNotable()
        {
            ItemData item = SimulationTestFactory.CreateItem("item-notable");
            NotableItemRuntime notable = new NotableItemRuntime("notable-runtime", item);
            Assert.That(Content.TryAddNotable(Site, notable, out string diagnostic), Is.True, diagnostic);
            Actor.AdventureSiteIntelKnowledge.RecordObservation(new AdventureNotableItemObservation(
                Site.RuntimeId, null, notable.RuntimeId, item.DefinitionId,
                0, 0, AdventureIntelSource.InitialScenarioKnowledge));
            return notable;
        }

        public SimulationRuntime CreateSimulationRuntime()
        {
            NpcActionData normal = SimulationTestFactory.CreateAction("normal-action", NpcActionType.Normal);
            return new SimulationRuntime(
                Records.Time,
                Array.Empty<CityRuntime>(),
                Npcs,
                economyEnabled: false,
                configuredActions: new[] { normal },
                npcDecisionSystem: new NpcDecisionSystem(null),
                travelSystem: Travel,
                travelPartySystem: PartySystem,
                decisionRecorder: Records.DecisionRecorder,
                explorableSiteStore: Sites,
                explorableSiteKnowledgeSystem: new ExplorableSiteKnowledgeSystem(),
                expeditionSystem: ExpeditionSystem,
                placeContentStore: Content,
                adventureExpeditionAutonomySystem: Autonomy);
        }
    }
}
