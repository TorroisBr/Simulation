using NUnit.Framework;

public sealed class EstateCensusTests
{
    [Test]
    public void RuntimeWitnessTracksExplicitEstateOpenAndRejectedDomainOperations()
    {
        PersonStore persons = new PersonStore();
        RegisterPerson(persons, "estate-census-deceased", 0L);
        RegisterPerson(persons, "estate-census-other-deceased", 0L);
        RegisterPerson(persons, "estate-census-living", null);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(1L),
            null,
            null,
            economyEnabled: false,
            personStore: persons);
        EstateCensusProvider provider = new EstateCensusProvider(runtime.EstateStore);
        AssertWitness(provider, runtime.EstateStore, 0, 0L);

        EstateId estateId = new EstateId("estate-census-record");
        PersonId deceasedId = new PersonId("estate-census-deceased");
        Assert.That(runtime.TryOpenEstate(
            estateId,
            deceasedId,
            1L,
            out EstateRecord opened,
            out EstateFoundationFailure openFailure), Is.True, openFailure.ToString());
        Assert.That(opened.EstateId, Is.EqualTo(estateId));
        AssertWitness(provider, runtime.EstateStore, 1, 1L);
        Assert.That(runtime.EstateStore.Records, Has.Count.EqualTo(1));
        Assert.That(runtime.EstateStore.TryGetByDeceasedPerson(deceasedId, out EstateRecord byDeceased), Is.True);
        Assert.That(byDeceased, Is.SameAs(opened));

        Assert.That(runtime.TryOpenEstate(
            estateId,
            new PersonId("estate-census-other-deceased"),
            1L,
            out _,
            out EstateFoundationFailure duplicateIdFailure), Is.False);
        Assert.That(duplicateIdFailure.Code, Is.EqualTo(EstateFoundationFailureCode.DuplicateEstateId));
        AssertWitness(provider, runtime.EstateStore, 1, 1L);

        Assert.That(runtime.TryOpenEstate(
            new EstateId("estate-census-second"),
            deceasedId,
            1L,
            out _,
            out EstateFoundationFailure duplicatePersonFailure), Is.False);
        Assert.That(duplicatePersonFailure.Code, Is.EqualTo(EstateFoundationFailureCode.EstateAlreadyExistsForPerson));
        AssertWitness(provider, runtime.EstateStore, 1, 1L);

        Assert.That(runtime.TryOpenEstate(
            new EstateId("estate-census-unknown-person"),
            new PersonId("estate-census-unknown"),
            1L,
            out _,
            out EstateFoundationFailure unknownPersonFailure), Is.False);
        Assert.That(unknownPersonFailure.Code, Is.EqualTo(EstateFoundationFailureCode.PersonNotRegistered));
        AssertWitness(provider, runtime.EstateStore, 1, 1L);

        Assert.That(runtime.TryOpenEstate(
            new EstateId("estate-census-living-person"),
            new PersonId("estate-census-living"),
            1L,
            out _,
            out EstateFoundationFailure livingPersonFailure), Is.False);
        Assert.That(livingPersonFailure.Code, Is.EqualTo(EstateFoundationFailureCode.PersonStillLiving));
        AssertWitness(provider, runtime.EstateStore, 1, 1L);
    }

    private static void RegisterPerson(PersonStore store, string id, long? deathAbsoluteDay)
    {
        Assert.That(store.TryRegister(
            new PersonRuntime(new PersonId(id), 0L, deathAbsoluteDay),
            out PersonStoreFailure failure), Is.True, failure.ToString());
    }

    private static void AssertWitness(
        EstateCensusProvider provider,
        EstateStore owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(EstateCensusProvider.SectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(EstateCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }
}
