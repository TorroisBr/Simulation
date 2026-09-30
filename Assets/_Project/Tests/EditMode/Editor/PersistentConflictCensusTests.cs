using NUnit.Framework;

public sealed class PersistentConflictCensusTests
{
    [Test]
    public void WitnessTracksConflictMutationsAndRejectedOperations()
    {
        ArmedForceStore forces = new ArmedForceStore(new PersonStore());
        ArmedForceId forceId = new ArmedForceId("conflict-census-force");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "Census", 0L), out _), Is.True);

        PersistentConflictStore owner = new PersistentConflictStore(forces);
        PersistentConflictCensusProvider provider = new PersistentConflictCensusProvider(owner);
        AssertWitness(provider, owner, 0, 0L);

        ConflictId conflictId = new ConflictId("conflict-census");
        Assert.That(owner.TryAddParticipantBinding(
            new ConflictId("missing-conflict"),
            CreateBinding("missing-conflict-binding", new ConflictId("missing-conflict"), forceId),
            out _), Is.False);
        AssertWitness(provider, owner, 0, 0L);

        PersistentConflictRecord oneSide = new PersistentConflictRecord(
            conflictId,
            0L,
            sides: new[] { new ConflictStateSide(conflictId, new ConflictSideId("only")) });
        Assert.That(owner.TryRegister(oneSide, out _), Is.False);
        AssertWitness(provider, owner, 0, 0L);

        Assert.That(owner.TryRegister(CreateConflict(conflictId), out _), Is.True);
        AssertWitness(provider, owner, 1, 1L);

        Assert.That(owner.TryAddParticipantBinding(
            conflictId,
            CreateBinding("missing-force-binding", conflictId, new ArmedForceId("missing-force")),
            out _), Is.False);
        AssertWitness(provider, owner, 1, 1L);

        Assert.That(owner.TryAddParticipantBinding(
            conflictId,
            CreateBinding("conflict-census-binding", conflictId, forceId),
            out _), Is.True);
        AssertWitness(provider, owner, 1, 2L);

        Assert.That(owner.TryEnd(conflictId, 1L, out _), Is.True);
        AssertWitness(provider, owner, 1, 3L);

        Assert.That(owner.TryAddParticipantBinding(
            conflictId,
            CreateBinding("post-end-binding", conflictId, forceId),
            out _), Is.False);
        AssertWitness(provider, owner, 1, 3L);

        Assert.That(owner.TryRegister(CreateConflict(conflictId), out _), Is.False);
        AssertWitness(provider, owner, 1, 3L);
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

    private static ConflictParticipantBinding CreateBinding(
        string bindingId,
        ConflictId conflictId,
        ArmedForceId forceId)
    {
        return new ConflictParticipantBinding(
            new ConflictParticipantBindingId(bindingId),
            conflictId,
            new ConflictSideId("side-a"),
            forceId);
    }

    private static void AssertWitness(
        PersistentConflictCensusProvider provider,
        PersistentConflictStore owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(PersistentConflictCensusProvider.SectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(PersistentConflictCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }
}
