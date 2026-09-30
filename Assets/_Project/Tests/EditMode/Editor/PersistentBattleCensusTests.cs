using NUnit.Framework;

public sealed class PersistentBattleCensusTests
{
    [Test]
    public void WitnessTracksBattleRowsAndRevisionWithoutCountingLifecycleAsRows()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("battle-census-force");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "Census", 0L), out _), Is.True);

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId hexId = new HexId("battle-census-hex");
        Assert.That(spatial.TryRegisterHex(new HexRecord(hexId), out _), Is.True);

        PersistentBattleStore owner = new PersistentBattleStore(forces, conflicts, wars, spatial);
        PersistentBattleCensusProvider provider = new PersistentBattleCensusProvider(owner);
        AssertWitness(provider, owner, 0, 0L);

        BattleId battleId = new BattleId("battle-census");
        PersistentBattleRecord battle = CreateBattle(battleId);
        Assert.That(owner.TryRegister(battle, out _), Is.True);
        AssertWitness(provider, owner, 1, 1L);

        Assert.That(owner.TryRegister(battle, out _), Is.False,
            "A duplicate BattleId must not change the owner witness.");
        AssertWitness(provider, owner, 1, 1L);

        Assert.That(owner.TryAddParticipantBinding(
            battleId,
            new BattleParticipantBinding(
                new BattleParticipantBindingId("battle-census-binding"),
                battleId,
                new BattleSideId("side-a"),
                forceId),
            out _), Is.True);
        AssertWitness(provider, owner, 1, 2L);

        Assert.That(owner.TryStart(battleId, 0L, SpatialReference.ForHex(hexId), out _), Is.True);
        AssertWitness(provider, owner, 1, 3L);
    }

    private static PersistentBattleRecord CreateBattle(BattleId id)
    {
        return new PersistentBattleRecord(
            id,
            0L,
            sides: new[]
            {
                new BattleStateSide(id, new BattleSideId("side-a")),
                new BattleStateSide(id, new BattleSideId("side-b"))
            });
    }

    private static void AssertWitness(
        PersistentBattleCensusProvider provider,
        PersistentBattleStore owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(PersistentBattleCensusProvider.SectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(PersistentBattleCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }
}
