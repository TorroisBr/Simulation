using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12EFactionCensusProviderCoreTests
{
    [Test]
    public void ProvidersKeepExactFactionOwnerAndSeparateCardinalitiesAtSharedRevision()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("p12-core-faction-provider-person"), 0L);
        Assert.That(persons.TryRegister(person, out PersonStoreFailure personFailure), Is.True,
            personFailure.ToString());

        FactionStore owner = new FactionStore(persons);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = FactionStoreCensusProvider.CreateProviders(owner);
        Assert.That(providers, Has.Count.EqualTo(2));
        AssertWitness(providers[0], owner, FactionStoreCensusProvider.FactionsSectionId, 0);
        AssertWitness(providers[1], owner, FactionStoreCensusProvider.AffiliationsSectionId, 0);

        FactionId factionId = new FactionId("p12-core-faction-provider-faction");
        Assert.That(owner.TryRegister(new FactionRecord(factionId, "Faction", 0L),
            out FactionFoundationFailure factionFailure), Is.True, factionFailure.ToString());
        AssertWitness(providers[0], owner, FactionStoreCensusProvider.FactionsSectionId, 1);
        AssertWitness(providers[1], owner, FactionStoreCensusProvider.AffiliationsSectionId, 0);

        Assert.That(owner.TryRegisterAffiliation(new FactionAffiliationRecord(factionId, person.PersonId, 0L),
            out FactionFoundationFailure affiliationFailure), Is.True, affiliationFailure.ToString());
        AssertWitness(providers[0], owner, FactionStoreCensusProvider.FactionsSectionId, 1);
        AssertWitness(providers[1], owner, FactionStoreCensusProvider.AffiliationsSectionId, 1);
    }

    private static void AssertWitness(
        IOwnerSectionCensusProvider provider,
        FactionStore expectedOwner,
        string expectedSectionId,
        int expectedCardinality)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(expectedSectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(FactionStoreCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(expectedOwner));
        Assert.That(witness.Cardinality, Is.EqualTo(expectedCardinality));
        Assert.That(witness.Revision, Is.EqualTo(expectedOwner.Revision));
    }
}
