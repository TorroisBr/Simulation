using NUnit.Framework;

public sealed class PersistentWarCensusTests
{
    [Test]
    public void WitnessTracksWarMutationsOptionalConflictAndRejectedOperations()
    {
        ArmedForceStore forces = new ArmedForceStore(new PersonStore());
        ArmedForceId forceId = new ArmedForceId("war-census-force");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "Census", 0L), out _), Is.True);

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("war-census-conflict");
        Assert.That(conflicts.TryRegister(CreateConflict(conflictId), out _), Is.True);

        PersistentWarStore owner = new PersistentWarStore(forces, conflicts);
        PersistentWarCensusProvider provider = new PersistentWarCensusProvider(owner);
        AssertWitness(provider, owner, 0, 0L);

        WarId warId = new WarId("war-census");
        Assert.That(owner.TryRegister(CreateWar(warId, new ConflictId("missing-conflict")), out _), Is.False);
        AssertWitness(provider, owner, 0, 0L);

        Assert.That(owner.TryRegister(CreateWar(warId, conflictId), out _), Is.True);
        AssertWitness(provider, owner, 1, 1L);

        Assert.That(owner.TryAddParticipantBinding(
            warId,
            CreateBinding("missing-force-binding", warId, new ArmedForceId("missing-force")),
            out _), Is.False);
        AssertWitness(provider, owner, 1, 1L);

        Assert.That(owner.TryAddParticipantBinding(
            warId,
            CreateBinding("war-census-binding", warId, forceId),
            out _), Is.True);
        AssertWitness(provider, owner, 1, 2L);

        Assert.That(owner.TryEnd(warId, 1L, out _), Is.True);
        AssertWitness(provider, owner, 1, 3L);

        Assert.That(owner.TryAddParticipantBinding(
            warId,
            CreateBinding("post-end-binding", warId, forceId),
            out _), Is.False);
        AssertWitness(provider, owner, 1, 3L);

        Assert.That(owner.TryRegister(CreateWar(warId, conflictId), out _), Is.False);
        AssertWitness(provider, owner, 1, 3L);

        WarId unlinkedWarId = new WarId("war-census-unlinked");
        Assert.That(owner.TryRegister(CreateWar(unlinkedWarId), out _), Is.True,
            "ConflictId is optional; an unlinked War remains valid.");
        AssertWitness(provider, owner, 2, 4L);
    }

    private static PersistentConflictRecord CreateConflict(ConflictId id)
    {
        return new PersistentConflictRecord(
            id,
            0L,
            sides: new[]
            {
                new ConflictStateSide(id, new ConflictSideId("side-a")),
                new ConflictStateSide(id, new ConflictSideId("side-b"))
            });
    }

    private static PersistentWarRecord CreateWar(WarId id, ConflictId conflictId = null)
    {
        return new PersistentWarRecord(
            id,
            0L,
            conflictId: conflictId,
            sides: new[]
            {
                new WarStateSide(id, new WarSideId("side-a")),
                new WarStateSide(id, new WarSideId("side-b"))
            });
    }

    private static WarParticipantBinding CreateBinding(string bindingId, WarId warId, ArmedForceId forceId)
    {
        return new WarParticipantBinding(
            new WarParticipantBindingId(bindingId),
            warId,
            new WarSideId("side-a"),
            forceId);
    }

    private static void AssertWitness(
        PersistentWarCensusProvider provider,
        PersistentWarStore owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(PersistentWarCensusProvider.SectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(PersistentWarCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }
}
