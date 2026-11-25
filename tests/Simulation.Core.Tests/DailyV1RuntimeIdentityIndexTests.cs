using NUnit.Framework;

public sealed class DailyV1RuntimeIdentityIndexTests
{
    [Test]
    public void StagedNoOpDoesNotChangeRevisionAndCommittedIdentityDoes()
    {
        DailyV1RuntimeIdentityIndex index = new DailyV1RuntimeIdentityIndex();

        Assert.That(index.CensusRevision, Is.Zero);
        Assert.That(index.GetCardinality(DailyV1RuntimeIdentityKind.Npc), Is.Zero);
        Assert.That(index.TryAdvanceRevision(), Is.True);
        Assert.That(index.TryRollbackRevision(), Is.True);
        Assert.That(index.CensusRevision, Is.Zero,
            "A begin/complete operation with no committed identity change leaves the census revision unchanged.");

        Assert.That(index.TryStageRegister("npc-1", DailyV1RuntimeIdentityKind.Npc), Is.True);
        Assert.That(index.CensusRevision, Is.Zero,
            "Staging is not published as a committed identity mutation.");
        Assert.That(index.TryAdvanceRevision(), Is.True);
        Assert.That(index.CensusRevision, Is.EqualTo(1L));
        Assert.That(index.GetCardinality(DailyV1RuntimeIdentityKind.Npc), Is.EqualTo(1));
        Assert.That(index.HasIdentity("npc-1", DailyV1RuntimeIdentityKind.Npc), Is.True);
    }

    [Test]
    public void GlobalIdentityCollisionAndRollbackPreserveExactCountsAndRevision()
    {
        DailyV1RuntimeIdentityIndex index = new DailyV1RuntimeIdentityIndex();
        Assert.That(index.TryStageRegister("same-id", DailyV1RuntimeIdentityKind.Location), Is.True);
        Assert.That(index.TryAdvanceRevision(), Is.True);

        Assert.That(index.TryStageRegister("same-id", DailyV1RuntimeIdentityKind.City), Is.False);
        Assert.That(index.CensusRevision, Is.EqualTo(1L));
        Assert.That(index.GetCardinality(DailyV1RuntimeIdentityKind.City), Is.Zero);
        Assert.That(index.HasIdentity("same-id", DailyV1RuntimeIdentityKind.Location), Is.True);

        Assert.That(index.TryRemoveStaged("same-id", DailyV1RuntimeIdentityKind.Location), Is.True);
        Assert.That(index.TryRollbackRevision(), Is.True);
        Assert.That(index.CensusRevision, Is.Zero);
        Assert.That(index.Contains("same-id"), Is.False);
        Assert.That(index.CaptureRegisteredValues(), Is.Empty);
    }

    [Test]
    public void P12IdentityWitnessesShareTheCoreOwnerAndMutationRevision()
    {
        DailyV1RuntimeIdentityIndex index = new DailyV1RuntimeIdentityIndex();
        Assert.That(index.TryStageRegister("npc-1", DailyV1RuntimeIdentityKind.Npc), Is.True);
        Assert.That(index.TryStageRegister("city-1", DailyV1RuntimeIdentityKind.City), Is.True);
        Assert.That(index.TryStageRegister("location-1", DailyV1RuntimeIdentityKind.Location), Is.True);
        Assert.That(index.TryStageRegister("route-1", DailyV1RuntimeIdentityKind.Route), Is.True);
        Assert.That(index.TryAdvanceRevision(), Is.True);

        OwnerSectionCensusWitness[] witnesses =
        {
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.Npc),
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.City),
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.Location),
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.Route),
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.ExplorableSite),
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.LocalPlace),
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.LocalConnection),
            index.GetOwnerSectionCensusWitness(DailyV1RuntimeIdentityKind.NotableItem)
        };

        foreach (OwnerSectionCensusWitness witness in witnesses)
        {
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(index));
            Assert.That(witness.Revision, Is.EqualTo(1L));
        }
        Assert.That(witnesses[0].SectionId, Is.EqualTo("p12c.runtime-identities.npcs"));
        Assert.That(witnesses[0].Cardinality, Is.EqualTo(1));
        Assert.That(witnesses[1].Cardinality, Is.EqualTo(1));
        Assert.That(witnesses[2].Cardinality, Is.EqualTo(1));
        Assert.That(witnesses[3].Cardinality, Is.EqualTo(1));
        for (int i = 4; i < witnesses.Length; i++)
            Assert.That(witnesses[i].Cardinality, Is.Zero,
                "Daily-v1 explicit-empty identity families remain backed by real zero-cardinality Core witnesses.");
    }
}
