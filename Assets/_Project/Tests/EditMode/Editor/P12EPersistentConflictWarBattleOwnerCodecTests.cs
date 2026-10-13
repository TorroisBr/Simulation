using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EPersistentConflictWarBattleOwnerCodecTests
{
    [Test]
    public void PopulatedSnapshots_RoundTripConflictWarBattleRelationshipsAndProvenance()
    {
        PersistentConflictOwnerSnapshot conflicts = new PersistentConflictOwnerSnapshot(1, 1, 5L,
            new[] { new PersistentConflictOwnerSnapshotRecord("conflict-1", 3L,
                ConflictLifecycleState.Active, null,
                new[] { new PersistentConflictOwnerSideSnapshot("conflict-1", "conflict-side-1", "Side One") },
                new[] { new PersistentConflictOwnerBindingSnapshot("conflict-binding-1", "conflict-1",
                    "conflict-side-1", "force-1") }) });
        PersistentWarOwnerSnapshot wars = new PersistentWarOwnerSnapshot(1, 1, 6L,
            new[] { new PersistentWarOwnerSnapshotRecord("war-1", 4L, WarLifecycleState.Active, null,
                "conflict-1",
                new[] { new PersistentWarOwnerSideSnapshot("war-1", "war-side-1", "War Side") },
                new[] { new PersistentWarOwnerBindingSnapshot("war-binding-1", "war-1",
                    "war-side-1", "force-1") }) });
        PersistentBattleOwnerSnapshot battles = new PersistentBattleOwnerSnapshot(1, 1, 7L,
            new[] { new PersistentBattleOwnerSnapshotRecord("battle-1", 8L, 9L,
                BattleLifecycleState.Resolved, "conflict-1", "war-1",
                new PersistentBattleOwnerSpatialReferenceSnapshot(SpatialReferenceKind.Location,
                    null, "location-1", null, null, null, null),
                new[] { new PersistentBattleOwnerSideSnapshot("battle-1", "battle-side-1", "Battle Side") },
                new[] { new PersistentBattleOwnerBindingSnapshot("battle-binding-1", "battle-1",
                    "battle-side-1", "force-1") },
                new PersistentBattleOwnerOutcomeSnapshot("battle-1", BattleOutcomeType.Victory,
                    "battle-side-1", 10L,
                    new PersistentBattleOwnerProvenanceSnapshot("policy", "profile", "projection",
                        "causal", "source", "capability", "random", "resolver", "d6-policy",
                        "d6-schema", "d6-coverage", "d6-plan"))) });

        Assert.That(P12EPersistentConflictWarBattleOwnerCodec.TryEncode(conflicts, wars, battles,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult encodeResult), Is.True,
            encodeResult?.SectionCode);
        Assert.That(sections.Select(section => section.Id), Is.EqualTo(new[]
        {
            PersistentConflictCensusProvider.SectionId,
            PersistentWarCensusProvider.SectionId,
            PersistentBattleCensusProvider.SectionId
        }));
        Assert.That(P12EPersistentConflictWarBattleOwnerCodec.TryDecode(sections,
            out PersistentConflictOwnerSnapshot decodedConflicts,
            out PersistentWarOwnerSnapshot decodedWars,
            out PersistentBattleOwnerSnapshot decodedBattles,
            out P12AResult decodeResult), Is.True, decodeResult?.SectionCode);

        Assert.That(decodedConflicts.Revision, Is.EqualTo(5L));
        Assert.That(decodedConflicts.Records[0].Sides[0].SideIdValue, Is.EqualTo("conflict-side-1"));
        Assert.That(decodedConflicts.Records[0].ParticipantBindings[0].ArmedForceIdValue, Is.EqualTo("force-1"));
        Assert.That(decodedWars.Records[0].ConflictIdValue, Is.EqualTo("conflict-1"));
        Assert.That(decodedWars.Records[0].ParticipantBindings[0].BindingIdValue, Is.EqualTo("war-binding-1"));
        PersistentBattleOwnerSnapshotRecord battle = decodedBattles.Records[0];
        Assert.That(battle.StartedAbsoluteDay, Is.EqualTo(9L));
        Assert.That(battle.ConflictIdValue, Is.EqualTo("conflict-1"));
        Assert.That(battle.WarIdValue, Is.EqualTo("war-1"));
        Assert.That(battle.LocationReference.Kind, Is.EqualTo(SpatialReferenceKind.Location));
        Assert.That(battle.LocationReference.LocationIdValue, Is.EqualTo("location-1"));
        Assert.That(battle.TerminalOutcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Victory));
        Assert.That(battle.TerminalOutcome.Provenance.D6B2PlanFingerprint, Is.EqualTo("d6-plan"));

        Assert.That(P12EPersistentConflictWarBattleOwnerCodec.TryEncode(decodedConflicts, decodedWars,
            decodedBattles, out IReadOnlyList<P12AOwnerSection> reencoded, out _), Is.True);
        for (int i = 0; i < sections.Count; i++)
        {
            Assert.That(reencoded[i].Revision, Is.EqualTo(sections[i].Revision));
            Assert.That(reencoded[i].Rows[0].OwnerKey, Is.EqualTo(sections[i].Rows[0].OwnerKey));
            Assert.That(reencoded[i].Rows[0].Payload, Is.EqualTo(sections[i].Rows[0].Payload));
        }
    }

    [Test]
    public void Decode_RejectsMissingDuplicateAndWrongOwnerSections()
    {
        var emptyConflicts = new PersistentConflictOwnerSnapshot(1, 0, 0L,
            new PersistentConflictOwnerSnapshotRecord[0]);
        var emptyWars = new PersistentWarOwnerSnapshot(1, 0, 0L, new PersistentWarOwnerSnapshotRecord[0]);
        var emptyBattles = new PersistentBattleOwnerSnapshot(1, 0, 0L, new PersistentBattleOwnerSnapshotRecord[0]);
        Assert.That(P12EPersistentConflictWarBattleOwnerCodec.TryEncode(emptyConflicts, emptyWars,
            emptyBattles, out IReadOnlyList<P12AOwnerSection> sections, out _), Is.True);
        Assert.That(P12EPersistentConflictWarBattleOwnerCodec.TryDecode(sections.Take(2).ToArray(),
            out _, out _, out _, out _), Is.False);
        Assert.That(P12EPersistentConflictWarBattleOwnerCodec.TryDecode(
            new[] { sections[0], sections[1], sections[1] }, out _, out _, out _, out _), Is.False);
    }
}
