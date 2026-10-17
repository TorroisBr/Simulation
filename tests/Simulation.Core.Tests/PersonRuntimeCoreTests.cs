using NUnit.Framework;

[TestFixture]
public sealed class PersonRuntimeCoreTests
{
    [Test]
    public void LifeAndResidenceWritesRetainAdmissionAndLocalRevisionSemantics()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("person-core"), 10L);
        bool admitted = false;
        int committed = 0;
        PersonRuntimeOwnerBridge.BindP12LifecycleMutationBoundary(
            person,
            _ => admitted,
            _ => committed++);

        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(person, "city-a"), Is.False);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.Zero);
        Assert.That(committed, Is.Zero);

        admitted = true;
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(person, "city-a"), Is.True);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(1L));
        Assert.That(committed, Is.EqualTo(1));

        Assert.That(PersonRuntimeOwnerBridge.TryRecordDeath(person, 9L), Is.False);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(1L));
        Assert.That(PersonRuntimeOwnerBridge.TryRecordDeath(person, 12L), Is.True);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(2L));
        Assert.That(committed, Is.EqualTo(2));
        Assert.That(person.IsDeadAt(12L), Is.True);
    }

    [Test]
    public void StagedOwnerBridgePreservesExactValuesAndSaturatedRevision()
    {
        PersonRuntime staged = PersonRuntimeOwnerBridge.CreateForOwnerSnapshot(
            new PersonId("person-restored"),
            3L,
            8L,
            "city-restored",
            "npc-restored",
            long.MaxValue);

        Assert.That(staged.PersonId.Value, Is.EqualTo("person-restored"));
        Assert.That(staged.BirthAbsoluteDay, Is.EqualTo(3L));
        Assert.That(staged.DeathAbsoluteDay, Is.EqualTo(8L));
        Assert.That(staged.ResidenceSettlementRuntimeId, Is.EqualTo("city-restored"));
        Assert.That(staged.MaterializedNpcRuntimeId, Is.EqualTo("npc-restored"));
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(staged), Is.EqualTo(long.MaxValue));
        PersonRuntimeOwnerBridge.BindP12LifecycleMutationBoundary(staged, _ => true, _ => { });
        Assert.That(PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(staged), Is.False);
        Assert.That(PersonRuntimeOwnerBridge.TryRecordDeath(staged, 9L), Is.False);
        Assert.That(staged.DeathAbsoluteDay, Is.EqualTo(8L));
    }
}
