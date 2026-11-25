using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12FTravelPartyOwnerSnapshotCoreTests
{
    [Test]
    public void CaptureAndMaterializationPreserveOrderedOwnerValuesAndRevision()
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

        IReadOnlyList<OwnerSectionCensusSnapshot> sections = Sections(source);
        DailyCaptureEligibilityToken token = CreateToken(sections);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, token, sections,
            out P12FTravelPartyOwnerSnapshot snapshot, out P12FCommitmentSnapshotFailure captureFailure), Is.True);
        Assert.That(captureFailure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        Assert.That(snapshot.Revision, Is.EqualTo(source.Revision));

        P12FTravelPartyOwnerSnapshotRecord row = snapshot.Parties[0];
        Assert.That(row.TravelerRuntimeIds, Is.EqualTo(new[] { "traveler-b", "traveler-a" }));
        Assert.That(row.EscortRuntimeIds, Is.EqualTo(new[] { "escort-c" }));
        Assert.That(row.MemberRuntimeIds, Is.EqualTo(new[] { "traveler-b", "traveler-a", "escort-c" }));
        Assert.That(row.MemberCosts.Select(cost => cost.Amount), Is.EqualTo(new[] { 1.5f, 2.5f, 0.75f }));

        Assert.That(P12FTravelPartyOwnerSnapshot.TryCreateStoreFromOwnerSnapshot(snapshot,
            out TravelPartyStore restored, out P12FCommitmentSnapshotFailure stageFailure), Is.True);
        Assert.That(stageFailure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        Assert.That(restored, Is.Not.SameAs(source));
        Assert.That(restored.GetById("party-stable"), Is.Not.SameAs(party));
        Assert.That(restored.Revision, Is.EqualTo(source.Revision));
        Assert.That(restored.GetById("party-stable").MemberRuntimeIds, Is.EqualTo(party.MemberRuntimeIds));
        Assert.That(restored.GetById("party-stable").MemberCosts[1].Amount, Is.EqualTo(2.5f));
    }

    [Test]
    public void CaptureRejectsCopiedOwnerVectorAndStaleCensus()
    {
        TravelPartyStore source = new TravelPartyStore();
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = Sections(source);
        DailyCaptureEligibilityToken token = CreateToken(sections);

        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, token,
            new List<OwnerSectionCensusSnapshot>(sections), out _, out _), Is.False);

        Assert.That(source.Add(new TravelPartyRuntime(
            "party-after-token", "origin", "destination", "route", new[] { "member" },
            Array.Empty<string>(), 1, null, new[] { new TravelPartyMemberCost("member", 0f) })), Is.True);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(source, token, sections, out _,
            out P12FCommitmentSnapshotFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidOwner));
    }

    [Test]
    public void MaterializationRejectsMemberAliasedAcrossParties()
    {
        P12FTravelPartyOwnerSnapshot snapshot = new P12FTravelPartyOwnerSnapshot(1, 7,
            new[]
            {
                Record("party-a", "member-shared"),
                Record("party-b", "member-shared")
            });

        Assert.That(P12FTravelPartyOwnerSnapshot.TryCreateStoreFromOwnerSnapshot(snapshot,
            out TravelPartyStore staged, out P12FCommitmentSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));
    }

    [Test]
    public void ExpeditionCaptureRetainsProgressUnderExactOwnerCensus()
    {
        ExpeditionStore source = new ExpeditionStore();
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-stable", "site-target", "origin", "destination", "route", "party-linked",
            "decision-origin", new[] { "member-a", "member-b" }, new[] { "member-b" }, new[] { "member-a" },
            ExpeditionState.AtSite,
            new ExpeditionObjectiveRuntime(ExpeditionObjectiveType.Explore, requiredProgress: 5,
                allowContinueAfterCompletion: false));
        Assert.That(source.Add(expedition), Is.True);
        Assert.That(expedition.TryBeginExploration(), Is.True);
        Assert.That(expedition.TrySetCurrentLocalPlace("place-current"), Is.True);
        Assert.That(expedition.TryTraverseLocalConnection("connection-1", "place-next", out _), Is.True);

        IReadOnlyList<OwnerSectionCensusSnapshot> sections = new List<OwnerSectionCensusSnapshot>
        {
            new OwnerSectionCensusSnapshot(ExpeditionCensusProvider.SectionId,
                ExpeditionCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                source, source.ActiveExpeditions.Count, source.Revision)
        };
        DailyCaptureEligibilityToken token = CreateToken(sections);

        Assert.That(P12FExpeditionOwnerSnapshot.TryCapture(source, token, sections,
            out P12FExpeditionOwnerSnapshot snapshot, out P12FCommitmentSnapshotFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        Assert.That(snapshot.Revision, Is.EqualTo(source.Revision));
        Assert.That(snapshot.Expeditions, Has.Count.EqualTo(1));
        P12FExpeditionOwnerSnapshotRecord row = snapshot.Expeditions[0];
        Assert.That(P12FExpeditionOwnerSnapshot.IsValidRecord(row), Is.True);
        Assert.That(row.ExpeditionId, Is.EqualTo("expedition-stable"));
        Assert.That(row.TravelPartyId, Is.EqualTo("party-linked"));
        Assert.That(row.MemberRuntimeIds, Is.EqualTo(new[] { "member-a", "member-b" }));
        Assert.That(row.PerformerRuntimeIds, Is.EqualTo(new[] { "member-b" }));
        Assert.That(row.SupportRuntimeIds, Is.EqualTo(new[] { "member-a" }));
        Assert.That(row.Progress, Is.EqualTo(2));
        Assert.That(row.ObjectiveCompleted, Is.False);
        Assert.That(row.CurrentLocalPlaceRuntimeId, Is.EqualTo("place-next"));
        Assert.That(row.VisitedLocalPlaceRuntimeIds, Is.EqualTo(new[] { "place-current", "place-next" }));
        Assert.That(row.ObservedLocalConnectionRuntimeIds, Is.EqualTo(new[] { "connection-1" }));

        Assert.That(source.Add(new ExpeditionRuntime("exp-after-token", "site", "origin", "target", "route",
            new[] { "member-c" }, new[] { "member-c" }, Array.Empty<string>())), Is.True);
        Assert.That(P12FExpeditionOwnerSnapshot.TryCapture(source, token, sections, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidOwner));
    }

    private static P12FTravelPartyOwnerSnapshotRecord Record(string partyId, string memberId) =>
        new P12FTravelPartyOwnerSnapshotRecord(partyId, "origin", "destination", "route",
            new[] { memberId }, Array.Empty<string>(), new[] { memberId },
            new[] { new TravelPartyMemberCost(memberId, 0f) }, 1, null);

    private static DailyCaptureEligibilityToken CreateToken(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections) =>
        new DailyCaptureEligibilityToken(new object(), SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()), SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 0L, 1L, 0L, sections);

    private static IReadOnlyList<OwnerSectionCensusSnapshot> Sections(TravelPartyStore source) =>
        new List<OwnerSectionCensusSnapshot>
        {
            new OwnerSectionCensusSnapshot(TravelPartyCensusProvider.SectionId,
                TravelPartyCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                source, source.ActiveParties.Count, source.Revision)
        };
}
