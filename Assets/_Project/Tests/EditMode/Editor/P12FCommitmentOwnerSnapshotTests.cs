using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12FCommitmentOwnerSnapshotTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void EmptyOwnersCaptureAndStageWithExactLocalRevisions()
    {
        TravelPartyStore parties = new TravelPartyStore();
        ExpeditionStore expeditions = new ExpeditionStore();
        DailyCaptureEligibilityToken token = CreateToken(Sections(parties, expeditions));

        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(parties, token, token.OwnerSections,
            out P12FTravelPartyOwnerSnapshot partySnapshot, out P12FCommitmentSnapshotFailure partyFailure), Is.True);
        Assert.That(partyFailure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        Assert.That(partySnapshot.Parties, Is.Empty);
        Assert.That(TravelPartyStore.TryCreateFromOwnerSnapshot(partySnapshot, out TravelPartyStore stagedParties), Is.True);
        Assert.That(stagedParties.Revision, Is.EqualTo(0));

        Assert.That(P12FExpeditionOwnerSnapshot.TryCapture(expeditions, token, token.OwnerSections,
            out P12FExpeditionOwnerSnapshot expeditionSnapshot, out P12FCommitmentSnapshotFailure expeditionFailure), Is.True);
        Assert.That(expeditionFailure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        Assert.That(expeditionSnapshot.Expeditions, Is.Empty);
        Assert.That(ExpeditionStore.TryCreateFromOwnerSnapshot(expeditionSnapshot, out ExpeditionStore stagedExpeditions), Is.True);
        Assert.That(stagedExpeditions.Revision, Is.EqualTo(0));
    }

    [Test]
    public void TravelPartySnapshotPreservesOrderedMembershipCostsAndReciprocalNpcLinks()
    {
        TravelPartyStore source = new TravelPartyStore();
        TravelPartyRuntime party = new TravelPartyRuntime(
            "party-stable", "origin", "destination", "route",
            new[] { "traveler-b", "traveler-a" }, new[] { "escort-c" }, 4,
            "decision-origin", new[]
            {
                new TravelPartyMemberCost("traveler-b", 1.5f),
                new TravelPartyMemberCost("traveler-a", 2.5f),
                new TravelPartyMemberCost("escort-c", 0.75f)
            });
        Assert.That(source.Add(party), Is.True);
        DailyCaptureEligibilityToken token = CreateToken(Sections(source, new ExpeditionStore()));
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, token, token.OwnerSections,
            out P12FTravelPartyOwnerSnapshot snapshot, out _), Is.True);
        Assert.That(snapshot.Parties.Single().TravelerRuntimeIds, Is.EqualTo(new[] { "traveler-b", "traveler-a" }));
        Assert.That(snapshot.Parties.Single().EscortRuntimeIds, Is.EqualTo(new[] { "escort-c" }));
        Assert.That(snapshot.Parties.Single().MemberRuntimeIds, Is.EqualTo(new[] { "traveler-b", "traveler-a", "escort-c" }));
        Assert.That(snapshot.Parties.Single().MemberCosts.Select(cost => cost.Amount), Is.EqualTo(new[] { 1.5f, 2.5f, 0.75f }));

        RuntimeIdentityRegistry identities = CreateTravelIdentities();
        List<NpcRuntime> npcs = CreateNpcsForParty(party.MemberRuntimeIds, party.TravelPartyId);
        List<P12DNpcFRow> rows = CreateRows(party.MemberRuntimeIds, party.TravelPartyId);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryStage(snapshot, npcs, rows, identities,
            out TravelPartyStore staged, out P12FCommitmentSnapshotFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        TravelPartyRuntime restored = staged.GetById("party-stable");
        Assert.That(restored, Is.Not.SameAs(party));
        Assert.That(restored.TravelerRuntimeIds, Is.EqualTo(party.TravelerRuntimeIds));
        Assert.That(restored.MemberRuntimeIds, Is.EqualTo(party.MemberRuntimeIds));
        Assert.That(restored.OriginDecisionId, Is.EqualTo("decision-origin"));
        Assert.That(restored.TravelDaysTotal, Is.EqualTo(4));
        Assert.That(staged.Revision, Is.EqualTo(source.Revision));

        List<P12DNpcFRow> missingReciprocal = CreateRows(party.MemberRuntimeIds, null);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryStage(snapshot, npcs, missingReciprocal, identities,
            out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));
    }

    [Test]
    public void ExpeditionSnapshotPreservesCommitmentsObjectiveAndExplorationProgress()
    {
        ExpeditionStore source = new ExpeditionStore();
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-stable", "site-target", "origin", "destination", "route", "party-linked",
            "decision-origin", new[] { "member-a", "member-b" }, new[] { "member-b" }, new[] { "member-a" },
            ExpeditionState.AtSite,
            new ExpeditionObjectiveRuntime(ExpeditionObjectiveType.Explore, requiredProgress: 5, allowContinueAfterCompletion: false));
        Assert.That(source.Add(expedition), Is.True);
        Assert.That(expedition.TryBeginExploration(), Is.True);
        Assert.That(expedition.TrySetCurrentLocalPlace("place-current"), Is.True);
        Assert.That(expedition.TryTraverseLocalConnection("connection-1", "place-next", out _), Is.True);
        Assert.That(expedition.TryTraverseLocalConnection("connection-2", "place-current", out _), Is.True);
        DailyCaptureEligibilityToken token = CreateToken(Sections(new TravelPartyStore(), source));

        Assert.That(P12FExpeditionOwnerSnapshot.TryCapture(source, token, token.OwnerSections,
            out P12FExpeditionOwnerSnapshot snapshot, out P12FCommitmentSnapshotFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        P12FExpeditionOwnerSnapshotRecord row = snapshot.Expeditions.Single();
        Assert.That(row.ExpeditionId, Is.EqualTo("expedition-stable"));
        Assert.That(row.TargetSiteRuntimeId, Is.EqualTo("site-target"));
        Assert.That(row.OriginDecisionId, Is.EqualTo("decision-origin"));
        Assert.That(row.MemberRuntimeIds, Is.EqualTo(new[] { "member-a", "member-b" }));
        Assert.That(row.PerformerRuntimeIds, Is.EqualTo(new[] { "member-b" }));
        Assert.That(row.SupportRuntimeIds, Is.EqualTo(new[] { "member-a" }));
        Assert.That(row.Progress, Is.EqualTo(2));
        Assert.That(row.ObjectiveCompleted, Is.False);
        Assert.That(row.VisitedLocalPlaceRuntimeIds, Is.EqualTo(new[] { "place-current", "place-next" }));
        Assert.That(row.ObservedLocalConnectionRuntimeIds, Is.EqualTo(new[] { "connection-1", "connection-2" }));

        Assert.That(ExpeditionStore.TryCreateFromOwnerSnapshot(snapshot, out ExpeditionStore staged), Is.True);
        ExpeditionRuntime restored = staged.GetById("expedition-stable");
        Assert.That(restored, Is.Not.SameAs(expedition));
        Assert.That(restored.State, Is.EqualTo(ExpeditionState.Exploring));
        Assert.That(restored.TravelPartyId, Is.EqualTo("party-linked"));
        Assert.That(restored.Objective.Progress, Is.EqualTo(2));
        Assert.That(restored.Objective.AllowContinueAfterCompletion, Is.False);
        Assert.That(restored.CurrentLocalPlaceRuntimeId, Is.EqualTo("place-current"));
        Assert.That(restored.VisitedLocalPlaceRuntimeIds, Is.EqualTo(row.VisitedLocalPlaceRuntimeIds));
        Assert.That(restored.ObservedLocalConnectionRuntimeIds, Is.EqualTo(row.ObservedLocalConnectionRuntimeIds));
        Assert.That(staged.Revision, Is.EqualTo(source.Revision));
    }

    [Test]
    public void CaptureRejectsWrongTokenVectorOwnerAndChangedOwnerRevision()
    {
        TravelPartyStore source = new TravelPartyStore();
        ExpeditionStore expeditions = new ExpeditionStore();
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = Sections(source, expeditions);
        DailyCaptureEligibilityToken token = CreateToken(sections);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, token, new List<OwnerSectionCensusSnapshot>(sections), out _, out _), Is.False);

        DailyCaptureEligibilityToken incompleteToken = CreateToken(sections, completedCoreSequence: 0L);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, incompleteToken, sections, out _, out _), Is.False);

        List<OwnerSectionCensusSnapshot> wrongOwner = new List<OwnerSectionCensusSnapshot>(sections);
        wrongOwner[0] = Section(TravelPartyCensusProvider.SectionId, new TravelPartyStore(), 0, 0);
        DailyCaptureEligibilityToken wrongOwnerToken = CreateToken(wrongOwner);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, wrongOwnerToken, wrongOwner, out _, out _), Is.False);

        TravelPartyRuntime party = new TravelPartyRuntime("party-mutated", "origin", "destination", "route",
            new[] { "member" }, null, 1, null, new[] { new TravelPartyMemberCost("member", 0f) });
        Assert.That(source.Add(party), Is.True);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, token, sections, out _, out P12FCommitmentSnapshotFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidOwner));

        List<OwnerSectionCensusSnapshot> staleExpeditionSections = Sections(new TravelPartyStore(), expeditions);
        DailyCaptureEligibilityToken staleExpeditionToken = CreateToken(staleExpeditionSections);
        ExpeditionRuntime added = new ExpeditionRuntime("exp-added", "site", "origin", "target", "route",
            new[] { "npc" }, new[] { "npc" }, Array.Empty<string>());
        Assert.That(expeditions.Add(added), Is.True);
        Assert.That(P12FExpeditionOwnerSnapshot.TryCapture(expeditions, staleExpeditionToken,
            staleExpeditionSections, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidOwner));
    }

    private static DailyCaptureEligibilityToken CreateToken(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections, long completedCoreSequence = 1L) =>
        new DailyCaptureEligibilityToken(new object(), SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()), SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 0L, completedCoreSequence, 0L, sections);

    private static List<OwnerSectionCensusSnapshot> Sections(TravelPartyStore parties, ExpeditionStore expeditions) =>
        new List<OwnerSectionCensusSnapshot>
        {
            Section(TravelPartyCensusProvider.SectionId, parties, parties.ActiveParties.Count, parties.Revision),
            Section(ExpeditionCensusProvider.SectionId, expeditions, expeditions.ActiveExpeditions.Count, expeditions.Revision)
        };

    private static OwnerSectionCensusSnapshot Section(string id, object owner, int count, long revision) =>
        new OwnerSectionCensusSnapshot(id, id == TravelPartyCensusProvider.SectionId
            ? TravelPartyCensusProvider.SchemaVersion : ExpeditionCensusProvider.SchemaVersion,
            OwnerSectionRole.Required, owner, count, revision);

    private static RuntimeIdentityRegistry CreateTravelIdentities()
    {
        RuntimeIdentityRegistry identities = new RuntimeIdentityRegistry();
        SpatialLocationRuntime origin = new SpatialLocationRuntime("origin");
        SpatialLocationRuntime destination = new SpatialLocationRuntime("destination");
        Assert.That(identities.RegisterLocation(origin), Is.True);
        Assert.That(identities.RegisterLocation(destination), Is.True);
        Assert.That(identities.RegisterRoute(new SpatialRouteRuntime("route", origin, destination, 4)), Is.True);
        return identities;
    }

    private static List<NpcRuntime> CreateNpcsForParty(IEnumerable<string> memberIds, string partyId)
    {
        List<NpcRuntime> values = new List<NpcRuntime>();
        foreach (string id in memberIds)
        {
            NpcRuntime npc = new NpcRuntime(id, SimulationTestFactory.CreateNpc("def-" + id));
            Assert.That(npc.SetActiveTravelPartyId(partyId), Is.True);
            values.Add(npc);
        }
        return values;
    }

    private static List<P12DNpcFRow> CreateRows(IEnumerable<string> ids, string partyId) => ids.Select(id =>
        new P12DNpcFRow(id, null, 0, 0, null, false, null, partyId, 0L, null, null,
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L, Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<CommercialKnowledgeShareReceipt>(), 0L)).ToList();
}
