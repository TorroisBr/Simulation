using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1NpcKnowledgeOwnerTests
{
    [Test]
    public void ExplorableSiteOwnerPreservesOrderedSnapshotAndReplacementRevision()
    {
        DailyV1ExplorableSiteKnowledgeOwner owner = new DailyV1ExplorableSiteKnowledgeOwner("npc-1");
        ExplorableSiteKnowledgeObservation first = new ExplorableSiteKnowledgeObservation(
            "site-a", "location-a", 1L, 1L, ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
        ExplorableSiteKnowledgeObservation second = new ExplorableSiteKnowledgeObservation(
            "site-b", "location-b", 2L, 2L, ExplorableSiteKnowledgeSource.DirectObservation);
        Assert.That(owner.RecordObservation(first), Is.True);
        Assert.That(owner.RecordObservation(second), Is.True);
        Assert.That(owner.RecordObservation(new ExplorableSiteKnowledgeObservation(
            "site-a", "location-a", 1L, 3L, ExplorableSiteKnowledgeSource.InitialScenarioKnowledge)), Is.False);
        Assert.That(owner.RecordObservation(new ExplorableSiteKnowledgeObservation(
            "site-a", "location-a", 1L, 3L, ExplorableSiteKnowledgeSource.DirectObservation)), Is.True);

        Assert.That(owner.Revision, Is.EqualTo(3L));
        Assert.That(owner.Observations.Count, Is.EqualTo(2));
        Assert.That(owner.Observations[0].SiteRuntimeId, Is.EqualTo("site-a"));
        Assert.That(owner.Observations[0].Source, Is.EqualTo(ExplorableSiteKnowledgeSource.DirectObservation));
        Assert.That(owner.Observations[1].SiteRuntimeId, Is.EqualTo("site-b"));
        Assert.That(owner.KnowsSite("site-a"), Is.True);
        Assert.That(owner.TryCopyOwnerSnapshot(out IReadOnlyList<ExplorableSiteKnowledgeObservation> snapshot,
            out long revision), Is.True);
        Assert.That(owner.MatchesSnapshot(snapshot, revision), Is.True);
    }

    [Test]
    public void AdventureIntelOwnerPreservesCategoryOrderAndWriterReplacementSemantics()
    {
        DailyV1AdventureSiteIntelKnowledgeOwner owner = new DailyV1AdventureSiteIntelKnowledgeOwner("npc-1");
        AdventureOppositionObservation initial = new AdventureOppositionObservation(
            "site-a", null, "opposition-a", AdventureOppositionObservedState.Active,
            1L, 1L, AdventureIntelSource.SharedByNpc, "npc-2");
        AdventureOppositionObservation direct = new AdventureOppositionObservation(
            "site-a", null, "opposition-a", AdventureOppositionObservedState.Resolved,
            1L, 1L, AdventureIntelSource.DirectObservation);
        AdventureNotableItemObservation item = new AdventureNotableItemObservation(
            "site-a", null, "item-a", "item-definition-a", 2L, 2L, AdventureIntelSource.DirectObservation);
        Assert.That(owner.RecordObservation(initial), Is.True);
        Assert.That(owner.RecordObservation(item), Is.True);
        Assert.That(owner.RecordObservation(direct), Is.True);
        Assert.That(owner.RecordObservation(initial), Is.False);

        Assert.That(owner.Revision, Is.EqualTo(3L));
        Assert.That(owner.OppositionObservations, Has.Count.EqualTo(1));
        Assert.That(owner.OppositionObservations[0].ObservedState, Is.EqualTo(AdventureOppositionObservedState.Resolved));
        Assert.That(owner.NotableItemObservations, Has.Count.EqualTo(1));
        Assert.That(owner.NotableItemObservations[0].NotableItemRuntimeId, Is.EqualTo("item-a"));
        Assert.That(owner.KnowsOpposition("site-a", "opposition-a"), Is.False);
        Assert.That(owner.TryCopyOwnerSnapshot(out IReadOnlyList<AdventureOppositionObservation> opposition,
            out IReadOnlyList<AdventureNotableItemObservation> notable,
            out IReadOnlyList<AdventureCommonResourceObservation> resources,
            out IReadOnlyList<AdventureAccessObservation> access,
            out long revision), Is.True);
        Assert.That(owner.MatchesSnapshot(opposition, notable, resources, access, revision), Is.True);
    }

    [Test]
    public void AdventureIntelInitialSnapshotRejectsDuplicateIdentityWithoutPartialInstall()
    {
        DailyV1AdventureSiteIntelKnowledgeOwner owner = new DailyV1AdventureSiteIntelKnowledgeOwner("npc-1");
        AdventureAccessObservation first = new AdventureAccessObservation(
            "site-a", null, PlaceAccessState.Accessible, 1L, 1L, AdventureIntelSource.DirectObservation);
        AdventureAccessObservation duplicate = new AdventureAccessObservation(
            "site-a", null, PlaceAccessState.Contested, 2L, 2L, AdventureIntelSource.DirectObservation);
        Assert.That(owner.TryCreateInitialSnapshot(
            new AdventureOppositionObservation[0], new AdventureNotableItemObservation[0],
            new AdventureCommonResourceObservation[0], new[] { first, duplicate }, 2L), Is.False);
        Assert.That(owner.AccessObservations, Is.Empty);
        Assert.That(owner.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void LocalTopologyEmptyOwnerHasStableIdentityAndOnlyMatchesExactEmptyProjection()
    {
        DailyV1NpcLocalTopologyKnowledgeEmptyOwner owner =
            new DailyV1NpcLocalTopologyKnowledgeEmptyOwner("npc-empty-topology");

        Assert.That(owner.OwnerRuntimeId, Is.EqualTo("npc-empty-topology"));
        Assert.That(owner.PlaceObservationCount, Is.Zero);
        Assert.That(owner.ConnectionObservationCount, Is.Zero);
        Assert.That(owner.Revision, Is.Zero);
        Assert.That(owner.MatchesRuntimeId("npc-empty-topology"), Is.True);
        Assert.That(owner.MatchesRuntimeId("npc-other"), Is.False);
        Assert.That(owner.MatchesProjection("npc-empty-topology", "npc-empty-topology", 0, 0, 0L), Is.True);
        Assert.That(owner.MatchesProjection("npc-empty-topology", "npc-other", 0, 0, 0L), Is.False);
        Assert.That(owner.MatchesProjection("npc-empty-topology", "npc-empty-topology", 1, 0, 0L), Is.False);
        Assert.That(owner.MatchesProjection("npc-empty-topology", "npc-empty-topology", 0, 1, 0L), Is.False);
        Assert.That(owner.MatchesProjection("npc-empty-topology", "npc-empty-topology", 0, 0, 1L), Is.False);
        Assert.Throws<System.ArgumentException>(() =>
            new DailyV1NpcLocalTopologyKnowledgeEmptyOwner(" "));
    }
}
