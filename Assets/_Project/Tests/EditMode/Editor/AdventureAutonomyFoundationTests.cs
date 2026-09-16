using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class AdventureAutonomyFoundationTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void AdventureCandidateEvaluationDoesNotMutateWorld()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        int placeCount = f.ContentStore.Places.Count;
        int topologyCount = f.Topology.Places.Count;

        f.Autonomy.BuildCandidates(f.Actor);

        Assert.That(f.ContentStore.Places, Has.Count.EqualTo(placeCount));
        Assert.That(f.Topology.Places, Has.Count.EqualTo(topologyCount));
    }

    [Test]
    public void AdventureCandidateEvaluationDoesNotMutateKnowledge()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        int siteCount = f.Actor.ExplorableSiteKnowledge.Observations.Count;
        int intelCount = f.Actor.AdventureSiteIntelKnowledge.OppositionObservations.Count;

        f.Autonomy.BuildCandidates(f.Actor);

        Assert.That(f.Actor.ExplorableSiteKnowledge.Observations, Has.Count.EqualTo(siteCount));
        Assert.That(f.Actor.AdventureSiteIntelKnowledge.OppositionObservations, Has.Count.EqualTo(intelCount));
    }

    [Test]
    public void AdventureCandidateEvaluationDoesNotConsumeExecutionRng()
    {
        Fixture f = new Fixture();
        f.KnowSite();

        AdventureCandidate first = f.Autonomy.ChooseCandidate(f.Autonomy.BuildCandidates(f.Actor));
        AdventureCandidate second = f.Autonomy.ChooseCandidate(f.Autonomy.BuildCandidates(f.Actor));

        Assert.That(second.Kind, Is.EqualTo(first.Kind));
        Assert.That(second.Utility, Is.EqualTo(first.Utility));
    }

    [Test]
    public void UnknownSiteIsNotAutonomousTarget()
    {
        Fixture f = new Fixture();
        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Is.Empty);
    }

    [Test]
    public void KnownSiteCanBecomeExploreCandidate()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Has.Some.Matches<AdventureCandidate>(c => c.Kind == AdventureCandidateKind.Explore));
    }

    [Test]
    public void EnabledScoutCanBecomeCandidateForKnownSite()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        AdventureAutonomySystem autonomy = new AdventureAutonomySystem(
            new AdventureAutonomySettings { enableScout = true, scoutUtility = 20f });

        Assert.That(autonomy.BuildCandidates(f.Actor), Has.Some.Matches<AdventureCandidate>(candidate =>
            candidate.Kind == AdventureCandidateKind.Scout
            && candidate.SiteRuntimeId == f.Site.RuntimeId));
    }

    [Test]
    public void UnknownSiteCannotBecomeScoutCandidate()
    {
        Fixture f = new Fixture();
        AdventureAutonomySystem autonomy = new AdventureAutonomySystem(
            new AdventureAutonomySettings { enableScout = true });

        Assert.That(autonomy.BuildCandidates(f.Actor), Has.None.Matches<AdventureCandidate>(candidate =>
            candidate.Kind == AdventureCandidateKind.Scout));
    }

    [Test]
    public void ScoutCandidateCreatesScoutObjective()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        AdventureAutonomySystem autonomy = new AdventureAutonomySystem(
            new AdventureAutonomySettings { enableScout = true, scoutUtility = 20f });
        AdventureCandidate candidate = autonomy.BuildCandidates(f.Actor)
            .First(candidate => candidate.Kind == AdventureCandidateKind.Scout);

        Assert.That(candidate.CreateObjective().ObjectiveType, Is.EqualTo(ExpeditionObjectiveType.Scout));
    }

    [Test]
    public void ScoutPlanningDoesNotReadUnknownTruth()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        PlaceOppositionRuntime hiddenOpposition = new PlaceOppositionRuntime("opposition-hidden");
        Assert.That(f.ContentStore.TryAddOpposition(f.Hidden, hiddenOpposition, out _), Is.True);
        AdventureAutonomySystem autonomy = new AdventureAutonomySystem(
            new AdventureAutonomySettings { enableScout = true, scoutUtility = 20f });

        IReadOnlyList<AdventureCandidate> candidates = autonomy.BuildCandidates(f.Actor);

        Assert.That(candidates, Has.Some.Matches<AdventureCandidate>(candidate => candidate.Kind == AdventureCandidateKind.Scout));
        Assert.That(candidates, Has.None.Matches<AdventureCandidate>(candidate =>
            candidate.Kind == AdventureCandidateKind.EliminateOpposition));
    }

    [Test]
    public void UnsupportedSecureIsNotAdvertisedAsAutonomousCandidate()
    {
        Fixture f = new Fixture();
        f.KnowSite();

        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Has.None.Matches<AdventureCandidate>(candidate =>
            candidate.Kind == AdventureCandidateKind.Secure));
    }

    [Test]
    public void NoCandidateKindSilentlyMapsSecureToExplore()
    {
        Fixture f = new Fixture();
        AdventureCandidate candidate = new AdventureCandidate(
            AdventureCandidateKind.Secure,
            f.Site.RuntimeId,
            f.Site.Location.RuntimeId,
            null,
            null,
            null,
            1f,
            new[] { new NpcDecisionParticipant(f.Actor.RuntimeId, NpcDecisionParticipantRole.Performer) },
            new[] { f.Actor.RuntimeId },
            Array.Empty<string>());

        Assert.That(() => candidate.CreateObjective(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void UnknownOppositionIsNotEliminateCandidate()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Has.None.Matches<AdventureCandidate>(c => c.Kind == AdventureCandidateKind.EliminateOpposition));
    }

    [Test]
    public void KnownOppositionCanBecomeEliminateCandidate()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        f.Actor.AdventureSiteIntelKnowledge.RecordObservation(f.OppositionObservation(1));
        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Has.Some.Matches<AdventureCandidate>(c => c.Kind == AdventureCandidateKind.EliminateOpposition));
    }

    [Test]
    public void UnknownNotableItemIsNotRetrieveCandidate()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Has.None.Matches<AdventureCandidate>(c => c.Kind == AdventureCandidateKind.RetrieveNotableItem));
    }

    [Test]
    public void KnownNotableItemCanBecomeRetrieveCandidate()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        f.Actor.AdventureSiteIntelKnowledge.RecordObservation(new AdventureNotableItemObservation(
            f.Site.RuntimeId, null, "notable-rumor", "item-relic", 1, 1, AdventureIntelSource.SharedByNpc, "npc-source"));
        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Has.Some.Matches<AdventureCandidate>(c => c.Kind == AdventureCandidateKind.RetrieveNotableItem));
    }

    [Test]
    public void StaleIntelMayRemainCandidateUntilExecutionValidation()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        f.Actor.AdventureSiteIntelKnowledge.RecordObservation(f.OppositionObservation(1));
        Assert.That(f.ContentStore.TryGet(f.Site, out _), Is.False);
        Assert.That(f.Autonomy.BuildCandidates(f.Actor), Has.Some.Matches<AdventureCandidate>(c => c.Kind == AdventureCandidateKind.EliminateOpposition));
    }

    [Test]
    public void CandidateUsesKnowledgeLocationNotTruthDiscovery()
    {
        Fixture f = new Fixture();
        SpatialLocationRuntime rumored = new SpatialLocationRuntime("location-rumored");
        f.Actor.SetCurrentPresence(rumored);
        f.Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            f.Site.RuntimeId, rumored.RuntimeId, 1, 1, ExplorableSiteKnowledgeSource.DirectObservation));

        AdventureCandidate candidate = f.Autonomy.ChooseCandidate(f.Autonomy.BuildCandidates(f.Actor));

        Assert.That(candidate.SiteLocationRuntimeId, Is.EqualTo(rumored.RuntimeId));
        Assert.That(candidate.SiteLocationRuntimeId, Is.Not.EqualTo(f.Site.Location.RuntimeId));
    }

    [Test]
    public void SoloPartyPolicySelectsOnlyDecisionMaker()
    {
        Fixture f = new Fixture();
        f.KnowSite();
        AdventureCandidate candidate = f.Autonomy.ChooseCandidate(f.Autonomy.BuildCandidates(f.Actor));
        Assert.That(candidate.PerformerRuntimeIds, Is.EqualTo(new[] { f.Actor.RuntimeId }));
        Assert.That(candidate.SupportRuntimeIds, Is.Empty);
    }

    [Test]
    public void InjectedPartyPolicyCanAddValidSupport()
    {
        Fixture f = new Fixture();
        NpcRuntime support = f.CreateNpc("npc-support");
        f.KnowSite();
        AdventureAutonomySystem system = new AdventureAutonomySystem(null, new SupportPolicy(support));

        AdventureCandidate candidate = system.ChooseCandidate(system.BuildCandidates(f.Actor, null, new[] { support }));

        Assert.That(candidate.SupportRuntimeIds, Does.Contain(support.RuntimeId));
    }

    [Test]
    public void UnavailableNpcCannotBeAddedAsSupport()
    {
        Fixture f = new Fixture();
        NpcRuntime support = f.CreateNpc("npc-support");
        support.TryApplyDeath();
        f.KnowSite();
        AdventureAutonomySystem system = new AdventureAutonomySystem(null, new SupportPolicy(support));

        AdventureCandidate candidate = system.ChooseCandidate(system.BuildCandidates(f.Actor, null, new[] { support }));

        Assert.That(candidate.SupportRuntimeIds, Does.Not.Contain(support.RuntimeId));
    }

    [Test]
    public void DecisionParticipantsPreserveRoles()
    {
        Fixture f = new Fixture();
        NpcRuntime support = f.CreateNpc("npc-support");
        f.KnowSite();
        AdventureAutonomySystem system = new AdventureAutonomySystem(null, new SupportPolicy(support));
        AdventureCandidate candidate = system.ChooseCandidate(system.BuildCandidates(f.Actor, null, new[] { support }));
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();

        NpcDecisionRecord decision = system.RecordStartDecision(f.Actor, candidate, records.DecisionRecorder);

        Assert.That(decision.DecisionParticipants, Has.Some.Matches<NpcDecisionParticipant>(p => p.RuntimeId == f.Actor.RuntimeId && p.Role == NpcDecisionParticipantRole.DecisionMaker));
        Assert.That(decision.DecisionParticipants, Has.Some.Matches<NpcDecisionParticipant>(p => p.RuntimeId == f.Actor.RuntimeId && p.Role == NpcDecisionParticipantRole.Performer));
        Assert.That(decision.DecisionParticipants, Has.Some.Matches<NpcDecisionParticipant>(p => p.RuntimeId == support.RuntimeId && p.Role == NpcDecisionParticipantRole.Support));
    }

    [Test]
    public void DirectObservationRecordsObservedOpposition()
    {
        Fixture f = new Fixture();
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("opposition-real");
        f.ContentStore.TryAddOpposition(f.Root, opposition, out _);
        f.ObserveRoot();
        Assert.That(f.Actor.AdventureSiteIntelKnowledge.KnowsOpposition(f.Site.RuntimeId, opposition.RuntimeId), Is.True);
    }

    [Test]
    public void DirectObservationRecordsObservedNotableItem()
    {
        Fixture f = new Fixture();
        ItemData item = SimulationTestFactory.CreateItem("item-notable");
        NotableItemRuntime notable = new NotableItemRuntime("notable-real", item);
        Assert.That(f.ContentStore.TryAddNotable(f.Root, notable, out string diagnostic), Is.True, diagnostic);
        f.ObserveRoot();
        Assert.That(f.Actor.AdventureSiteIntelKnowledge.KnowsNotableItem(f.Site.RuntimeId, notable.RuntimeId), Is.True);
    }

    [Test]
    public void DirectObservationRecordsObservedCommonResource()
    {
        Fixture f = new Fixture();
        ItemData item = SimulationTestFactory.CreateItem("item-ore");
        f.ContentStore.TryAddStack(f.Root, item, 4, PlaceContentPersistencePolicy.Durable, out _);
        f.ObserveRoot();
        Assert.That(f.Actor.AdventureSiteIntelKnowledge.KnowsCommonResource(f.Site.RuntimeId, item.DefinitionId), Is.True);
    }

    [Test]
    public void ObservingOneLocalPlaceDoesNotRevealWholeTopology()
    {
        Fixture f = new Fixture();
        f.ObserveRoot();
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, f.Hidden.RuntimeId), Is.False);
    }

    [Test]
    public void ObservingLocalPlaceCanRevealOutgoingVisibleConnections()
    {
        Fixture f = new Fixture();
        f.ObserveRoot();
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsConnection(f.Site.RuntimeId, f.VisibleConnection.RuntimeId), Is.True);
        Assert.That(f.Actor.LocalTopologyKnowledge.KnowsLocalPlace(f.Site.RuntimeId, f.Adjacent.RuntimeId), Is.True);
    }

    [Test]
    public void RemotePlaceContentIsNotObserved()
    {
        Fixture f = new Fixture();
        PlaceOppositionRuntime hiddenOpposition = new PlaceOppositionRuntime("opposition-hidden");
        f.ContentStore.TryAddOpposition(f.Hidden, hiddenOpposition, out _);
        f.ObserveRoot();
        Assert.That(f.Actor.AdventureSiteIntelKnowledge.KnowsOpposition(f.Site.RuntimeId, hiddenOpposition.RuntimeId), Is.False);
    }

    [Test]
    public void NewerIntelReplacesOlderIntel()
    {
        Fixture f = new Fixture();
        f.Actor.AdventureSiteIntelKnowledge.RecordObservation(f.OppositionObservation(1));
        f.Actor.AdventureSiteIntelKnowledge.RecordObservation(new AdventureOppositionObservation(
            f.Site.RuntimeId, null, "opposition-known", AdventureOppositionObservedState.Resolved,
            3, 3, AdventureIntelSource.DirectObservation));
        Assert.That(f.Actor.AdventureSiteIntelKnowledge.TryGetOpposition(f.Site.RuntimeId, "opposition-known", out AdventureOppositionObservation result), Is.True);
        Assert.That(result.ObservedState, Is.EqualTo(AdventureOppositionObservedState.Resolved));
    }

    [Test]
    public void KnowledgeQueryDoesNotMutateObservation()
    {
        Fixture f = new Fixture();
        AdventureOppositionObservation original = f.OppositionObservation(1);
        f.Actor.AdventureSiteIntelKnowledge.RecordObservation(original);
        int count = f.Actor.AdventureSiteIntelKnowledge.OppositionObservations.Count;
        f.Actor.AdventureSiteIntelKnowledge.TryGetOpposition(f.Site.RuntimeId, original.OppositionRuntimeId, out AdventureOppositionObservation queried);
        Assert.That(queried, Is.SameAs(original));
        Assert.That(f.Actor.AdventureSiteIntelKnowledge.OppositionObservations, Has.Count.EqualTo(count));
    }

    private sealed class SupportPolicy : IAdventurePartyAssemblyPolicy
    {
        private readonly NpcRuntime support;

        public SupportPolicy(NpcRuntime support)
        {
            this.support = support;
        }

        public IReadOnlyList<AdventurePartySelection> Assemble(NpcRuntime decisionMaker, IReadOnlyList<NpcRuntime> availableNpcs)
        {
            return new[]
            {
                new AdventurePartySelection(decisionMaker, NpcDecisionParticipantRole.Performer),
                new AdventurePartySelection(support, NpcDecisionParticipantRole.Support)
            };
        }
    }

    private sealed class Fixture
    {
        public RuntimeIdAllocator Allocator { get; } = new RuntimeIdAllocator();
        public RuntimeIdentityRegistry Registry { get; } = new RuntimeIdentityRegistry();
        public ExplorableSiteRuntime Site { get; }
        public NpcRuntime Actor { get; }
        public LocalTopologyRuntime Topology { get; }
        public LocalPlaceRuntime Root { get; }
        public LocalPlaceRuntime Adjacent { get; }
        public LocalPlaceRuntime Hidden { get; }
        public LocalTopologyConnectionRuntime VisibleConnection { get; }
        public PlaceContentStore ContentStore { get; }
        public AdventureAutonomySystem Autonomy { get; } = new AdventureAutonomySystem();
        public AdventureSiteIntelKnowledgeSystem IntelSystem { get; } = new AdventureSiteIntelKnowledgeSystem();

        public Fixture()
        {
            SpatialLocationRuntime location = new SpatialLocationRuntime("location-site");
            Site = new ExplorableSiteRuntime("site-runtime", SimulationTestFactory.CreateExplorableSite("site-def"), location);
            Registry.RegisterExplorableSite(Site);
            Actor = new NpcRuntime("npc-actor", SimulationTestFactory.CreateNpc("actor"));
            Actor.SetCurrentPresence(location);
            Registry.RegisterNpc(Actor);

            Topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(Site));
            Root = new LocalPlaceRuntime("local-root", "Root");
            Adjacent = new LocalPlaceRuntime("local-adjacent", "Adjacent");
            Hidden = new LocalPlaceRuntime("local-hidden", "Hidden");
            Topology.AddPlace(Root, null, true);
            Topology.AddPlace(Adjacent);
            Topology.AddPlace(Hidden);
            VisibleConnection = new LocalTopologyConnectionRuntime("connection-visible", Root, Adjacent, 1f);
            Topology.AddConnection(VisibleConnection);
            ContentStore = new PlaceContentStore(Allocator, Registry);
        }

        public void KnowSite()
        {
            Actor.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
                Site.RuntimeId,
                Site.Location.RuntimeId,
                1,
                1,
                ExplorableSiteKnowledgeSource.DirectObservation));
        }

        public NpcRuntime CreateNpc(string runtimeId)
        {
            NpcRuntime npc = new NpcRuntime(runtimeId, SimulationTestFactory.CreateNpc(runtimeId));
            npc.SetCurrentPresence(Actor.CurrentLocation);
            Registry.RegisterNpc(npc);
            return npc;
        }

        public AdventureOppositionObservation OppositionObservation(long day)
        {
            return new AdventureOppositionObservation(
                Site.RuntimeId,
                null,
                "opposition-known",
                AdventureOppositionObservedState.Active,
                day,
                day,
                AdventureIntelSource.InitialScenarioKnowledge);
        }

        public void ObserveRoot()
        {
            Assert.That(IntelSystem.RecordDirectObservation(Actor, Site, Root, Topology, ContentStore, 5), Is.True);
        }
    }
}
