using NUnit.Framework;

public sealed class PersonCensusProviderTests
{
    [Test]
    public void MembershipAndMaterializationWitnessesComeFromSameCoreStoreRevision()
    {
        PersonStore store = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person/α"), 12L);
        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());

        OwnerSectionCensusWitness membership = store.GetMembershipCensusWitness();
        OwnerSectionCensusWitness binding = store.GetMaterializationBindingCensusWitness();
        Assert.That(membership.SectionId, Is.EqualTo("p12d.person.membership"));
        Assert.That(binding.SectionId, Is.EqualTo("p12d.person.materialization-binding"));
        Assert.That(membership.OwnerInstanceIdentity, Is.SameAs(store));
        Assert.That(binding.OwnerInstanceIdentity, Is.SameAs(store));
        Assert.That(membership.Revision, Is.EqualTo(binding.Revision));
        Assert.That(membership.Cardinality, Is.EqualTo(1));
        Assert.That(binding.Cardinality, Is.Zero);
    }

    [Test]
    public void ResidenceWitnessUsesStableLengthPrefixedPersonIdentityAndLiveRevision()
    {
        PersonRuntime person = new PersonRuntime(new PersonId("person/α"), 12L);
        OwnerSectionCensusWitness witness = person.GetLifeResidenceCensusWitness();

        Assert.That(witness.SectionId, Is.EqualTo("p12b.person-life-residence/8:person/α"));
        Assert.That(witness.SchemaVersion, Is.EqualTo(1));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(person));
        Assert.That(witness.Cardinality, Is.EqualTo(1));
        Assert.That(witness.Revision, Is.Zero);

        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(person, "city-1"), Is.True);
        OwnerSectionCensusWitness changed = person.GetLifeResidenceCensusWitness();
        Assert.That(changed.SectionId, Is.EqualTo(witness.SectionId));
        Assert.That(changed.OwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity));
        Assert.That(changed.Cardinality, Is.EqualTo(1));
        Assert.That(changed.Revision, Is.EqualTo(witness.Revision + 1L));
    }

}
