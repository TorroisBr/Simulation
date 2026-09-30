using System.Collections.Generic;
using NUnit.Framework;

public sealed class PropertyOwnershipCensusTests
{
    [Test]
    public void RuntimeWitnessTracksRegistrationTransferAndRejectedOperations()
    {
        PersonStore people = new PersonStore();
        PersonRuntime owner = Register(people, "property-census-owner");
        PersonRuntime successor = Register(people, "property-census-successor");
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(6L),
            System.Array.Empty<CityRuntime>(),
            null,
            economyEnabled: false,
            personStore: people);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            PropertyOwnershipCensusProvider.CreateProviders(runtime.PropertyOwnershipStore);

        AssertWitnesses(providers, runtime.PropertyOwnershipStore, 0, 0, 0L);
        PropertyId propertyId = new PropertyId("property-census-home");
        Assert.That(runtime.TryRegisterPropertyOwnership(
            new PropertyOwnershipRecord(propertyId, owner.PersonId),
            out PropertyFoundationFailure registerFailure), Is.True, registerFailure.ToString());
        AssertWitnesses(providers, runtime.PropertyOwnershipStore, 1, 0, 1L);

        Assert.That(runtime.TryTransferProperty(
            propertyId,
            successor.PersonId,
            6L,
            out PropertyTransferFailure transferFailure), Is.True, transferFailure.ToString());
        AssertWitnesses(providers, runtime.PropertyOwnershipStore, 1, 1, 2L);
        Assert.That(runtime.PropertyOwnershipStore.TryGet(propertyId, out PropertyOwnershipRecord current), Is.True);
        Assert.That(current.OwnerPersonId, Is.EqualTo(successor.PersonId));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory, Has.Count.EqualTo(1));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory[0].PreviousOwnerPersonId,
            Is.EqualTo(owner.PersonId));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory[0].NewOwnerPersonId,
            Is.EqualTo(successor.PersonId));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory[0].TransferAbsoluteDay, Is.EqualTo(6L));

        Assert.That(runtime.TryRegisterPropertyOwnership(
            new PropertyOwnershipRecord(propertyId, successor.PersonId),
            out PropertyFoundationFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(PropertyFoundationFailureCode.DuplicatePropertyId));
        AssertWitnesses(providers, runtime.PropertyOwnershipStore, 1, 1, 2L);

        Assert.That(runtime.TryTransferProperty(
            propertyId,
            successor.PersonId,
            6L,
            out PropertyTransferFailure sameOwnerFailure), Is.False);
        Assert.That(sameOwnerFailure.Code, Is.EqualTo(PropertyTransferFailureCode.SameOwner));
        AssertWitnesses(providers, runtime.PropertyOwnershipStore, 1, 1, 2L);
    }

    [Test]
    public void RuntimeClonePreservesPopulatedOwnershipHistoryAndRevisionWithoutChangingSource()
    {
        PersonStore people = new PersonStore();
        PersonRuntime owner = Register(people, "property-clone-owner");
        PersonRuntime successor = Register(people, "property-clone-successor");
        PropertyOwnershipStore source = new PropertyOwnershipStore(people);
        PropertyId propertyId = new PropertyId("property-clone-home");
        Assert.That(source.TryRegister(
            new PropertyOwnershipRecord(propertyId, owner.PersonId),
            out PropertyFoundationFailure registerFailure), Is.True, registerFailure.ToString());
        Assert.That(PropertyTransferSystem.TryTransfer(
            people,
            source,
            propertyId,
            successor.PersonId,
            6L,
            out _,
            out PropertyTransferFailure transferFailure), Is.True, transferFailure.ToString());
        Assert.That(source.Count, Is.EqualTo(1));
        Assert.That(source.TransferHistory, Has.Count.EqualTo(1));
        Assert.That(source.Revision, Is.EqualTo(2L));
        PropertyOwnershipRecord sourceOwnership = source.OwnershipRecords[0];
        PropertyOwnershipTransferHistoryRecord sourceHistory = source.TransferHistory[0];

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(6L),
            System.Array.Empty<CityRuntime>(),
            null,
            economyEnabled: false,
            personStore: people,
            propertyOwnershipStore: source);

        Assert.That(runtime.PropertyOwnershipStore, Is.Not.SameAs(source));
        Assert.That(runtime.PropertyOwnershipStore.Count, Is.EqualTo(1));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory, Has.Count.EqualTo(1));
        Assert.That(runtime.PropertyOwnershipStore.Revision, Is.EqualTo(source.Revision));
        Assert.That(runtime.PropertyOwnershipStore.OwnershipRecords[0], Is.EqualTo(sourceOwnership));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory[0].PropertyId, Is.EqualTo(sourceHistory.PropertyId));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory[0].PreviousOwnerPersonId,
            Is.EqualTo(sourceHistory.PreviousOwnerPersonId));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory[0].NewOwnerPersonId,
            Is.EqualTo(sourceHistory.NewOwnerPersonId));
        Assert.That(runtime.PropertyOwnershipStore.TransferHistory[0].TransferAbsoluteDay,
            Is.EqualTo(sourceHistory.TransferAbsoluteDay));
        Assert.That(source.Count, Is.EqualTo(1));
        Assert.That(source.TransferHistory, Has.Count.EqualTo(1));
        Assert.That(source.Revision, Is.EqualTo(2L));
        Assert.That(source.OwnershipRecords[0], Is.SameAs(sourceOwnership));
        Assert.That(source.TransferHistory[0], Is.SameAs(sourceHistory));
    }

    private static PersonRuntime Register(PersonStore people, string id)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id), 0L);
        Assert.That(people.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static void AssertWitnesses(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        PropertyOwnershipStore owner,
        int ownershipCount,
        int transferHistoryCount,
        long revision)
    {
        Assert.That(providers, Has.Count.EqualTo(2));
        OwnerSectionCensusWitness ownership = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness history = providers[1].GetCurrentCensus();
        Assert.That(ownership.SectionId, Is.EqualTo(PropertyOwnershipCensusProvider.OwnershipSectionId));
        Assert.That(ownership.SchemaVersion, Is.EqualTo(PropertyOwnershipCensusProvider.SchemaVersion));
        Assert.That(history.SectionId, Is.EqualTo(PropertyOwnershipCensusProvider.TransferHistorySectionId));
        Assert.That(history.SchemaVersion, Is.EqualTo(PropertyOwnershipCensusProvider.SchemaVersion));
        Assert.That(ownership.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(history.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(ownership.Cardinality, Is.EqualTo(ownershipCount));
        Assert.That(history.Cardinality, Is.EqualTo(transferHistoryCount));
        Assert.That(ownership.Revision, Is.EqualTo(revision));
        Assert.That(history.Revision, Is.EqualTo(revision));
    }
}
