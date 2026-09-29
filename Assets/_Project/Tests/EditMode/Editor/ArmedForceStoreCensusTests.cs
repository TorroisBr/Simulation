using System.Collections.Generic;
using NUnit.Framework;

public sealed class ArmedForceStoreCensusTests
{
    [Test]
    public void FixedSectionsTrackInstalledCollectionsAndSharedRevision()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore store = new ArmedForceStore(persons);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = ArmedForceStoreCensusProvider.CreateProviders(store);
        AssertCensus(providers, store, 0, 0, 0, 0L);

        ArmedForceId forceId = new ArmedForceId("force-census");
        Assert.That(store.TryRegister(new ArmedForceRecord(forceId, "Census", 0L), out _), Is.True);
        AssertCensus(providers, store, 1, 0, 0, 1L);

        PersonId personId = new PersonId("person-census");
        Assert.That(persons.TryRegister(new PersonRuntime(personId), out _), Is.True);
        Assert.That(store.TryAddRelevantPerson(forceId, personId, "witness", out _, out _), Is.True);
        AssertCensus(providers, store, 1, 0, 1, 2L);

        ContingentId contingentId = new ContingentId("contingent-census");
        Assert.That(store.TryRegisterContingent(
            new ContingentRecord(
                contingentId,
                forceId,
                3L,
                new ContingentOriginReference("fixture", "census"),
                "service"),
            out _), Is.True);
        AssertCensus(providers, store, 1, 1, 1, 3L);

        Assert.That(store.TrySetOperationalLocation(forceId, "same-count-change", out _), Is.True);
        AssertCensus(providers, store, 1, 1, 1, 4L);

        Assert.That(store.TryRegister(new ArmedForceRecord(forceId, "Duplicate", 0L), out _), Is.False);
        AssertCensus(providers, store, 1, 1, 1, 4L);
    }

    private static void AssertCensus(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        ArmedForceStore owner,
        int forceCount,
        int contingentCount,
        int relevantPersonCount,
        long revision)
    {
        string[] sectionIds =
        {
            ArmedForceStoreCensusProvider.ForcesSectionId,
            ArmedForceStoreCensusProvider.ContingentsSectionId,
            ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId
        };
        int[] cardinalities = { forceCount, contingentCount, relevantPersonCount };
        Assert.That(providers.Count, Is.EqualTo(sectionIds.Length));
        for (int i = 0; i < providers.Count; i++)
        {
            OwnerSectionCensusWitness witness = providers[i].GetCurrentCensus();
            Assert.That(witness.SectionId, Is.EqualTo(sectionIds[i]));
            Assert.That(witness.SchemaVersion, Is.EqualTo(ArmedForceStoreCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
            Assert.That(witness.Cardinality, Is.EqualTo(cardinalities[i]));
            Assert.That(witness.Revision, Is.EqualTo(revision));
        }
    }
}
