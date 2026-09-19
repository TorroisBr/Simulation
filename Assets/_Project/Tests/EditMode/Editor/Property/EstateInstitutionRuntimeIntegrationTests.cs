using System;
using NUnit.Framework;

public sealed class EstateInstitutionRuntimeIntegrationTests
{
    [Test]
    public void RuntimeClonesWorldOwnedPropertyAndEstateStoresWithoutNpcDependency()
    {
        PersonStore people = new PersonStore();
        PersonRuntime deceased = new PersonRuntime(new PersonId("dead-owner"), 0L, 4L);
        Assert.That(people.TryRegister(deceased, out _), Is.True);

        PropertyOwnershipStore properties = new PropertyOwnershipStore();
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(
                new PropertyId("property-1"),
                deceased.PersonId),
            out _), Is.True);

        EstateStore estates = new EstateStore(people);
        Assert.That(EstateOpeningSystem.TryOpenEstate(
            people,
            estates,
            new EstateId("estate-1"),
            deceased.PersonId,
            5L,
            out _,
            out EstateFoundationFailure estateFailure), Is.True, estateFailure.ToString());

        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(5L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people,
            propertyOwnershipStore: properties,
            estateStore: estates);

        Assert.That(world.PropertyOwnershipStore, Is.Not.SameAs(properties));
        Assert.That(world.EstateStore, Is.Not.SameAs(estates));
        Assert.That(world.PropertyOwnershipRecords, Has.Count.EqualTo(1));
        Assert.That(world.EstateRecords, Has.Count.EqualTo(1));
        Assert.That(world.NpcRuntimes, Is.Empty);
        Assert.That(world.GetPropertiesOwnedBy(deceased.PersonId), Has.Count.EqualTo(1));
    }

    [Test]
    public void RuntimeRejectsEstateStoreBoundToAnotherPersonWorld()
    {
        PersonStore worldPeople = new PersonStore();
        PersonStore otherPeople = new PersonStore();
        EstateStore foreignEstates = new EstateStore(otherPeople);

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(0L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: worldPeople,
            estateStore: foreignEstates));
    }

    [Test]
    public void RuntimeRequiresExplicitDeathGatedEstateOpeningAndCurrentDay()
    {
        PersonStore people = new PersonStore();
        PersonRuntime living = new PersonRuntime(new PersonId("living"), 0L);
        Assert.That(people.TryRegister(living, out _), Is.True);
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(10L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people);

        Assert.That(world.TryOpenEstate(
            new EstateId("living-estate"),
            living.PersonId,
            10L,
            out _,
            out EstateFoundationFailure livingFailure), Is.False);
        Assert.That(livingFailure.Code, Is.EqualTo(EstateFoundationFailureCode.PersonStillLiving));

        Assert.That(world.TryApplyPersonDeath(living.PersonId, out _, out _), Is.True);
        Assert.That(world.TryOpenEstate(
            new EstateId("living-estate"),
            living.PersonId,
            11L,
            out _,
            out EstateFoundationFailure futureFailure), Is.False);
        Assert.That(futureFailure.Code, Is.EqualTo(EstateFoundationFailureCode.InvalidOpeningDay));
        Assert.That(world.TryOpenEstate(
            new EstateId("living-estate"),
            living.PersonId,
            10L,
            out EstateRecord estate,
            out EstateFoundationFailure openingFailure), Is.True, openingFailure.ToString());
        Assert.That(estate.DeceasedPersonId, Is.EqualTo(living.PersonId));
    }

    [Test]
    public void DiagnosticsExportDiffAndInvariantsIncludePropertyAndEstateFacts()
    {
        PersonStore people = new PersonStore();
        PersonRuntime deceased = new PersonRuntime(new PersonId("diagnostic-dead"), 0L, 3L);
        Assert.That(people.TryRegister(deceased, out _), Is.True);
        PropertyOwnershipStore properties = new PropertyOwnershipStore();
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("diagnostic-property"), deceased.PersonId),
            out _), Is.True);
        EstateStore estates = new EstateStore(people);
        Assert.That(EstateOpeningSystem.TryOpenEstate(
            people,
            estates,
            new EstateId("diagnostic-estate"),
            deceased.PersonId,
            4L,
            out _,
            out _), Is.True);

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(
                simulationTime: new SimulationTime(4L),
                personStore: people,
                propertyOwnershipStore: properties,
                estateStore: estates));

        Assert.That(snapshot.PropertyOwnershipCount, Is.EqualTo(1));
        Assert.That(snapshot.EstateCount, Is.EqualTo(1));
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain(
            "PROPERTY_OWNERSHIP|diagnostic-property|diagnostic-dead"));
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain(
            "ESTATE|diagnostic-estate|diagnostic-dead|4"));
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True);

        PropertyOwnershipStore changedProperties = new PropertyOwnershipStore();
        Assert.That(changedProperties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("diagnostic-property"), deceased.PersonId),
            out _), Is.True);
        EstateStore changedEstates = new EstateStore(people);
        Assert.That(EstateOpeningSystem.TryOpenEstate(
            people,
            changedEstates,
            new EstateId("diagnostic-estate"),
            deceased.PersonId,
            4L,
            out _,
            out _), Is.True);
        Assert.That(changedProperties.TryRegister(
            new PropertyOwnershipRecord(new PropertyId("diagnostic-property-2"), deceased.PersonId),
            out _), Is.True);

        WorldStateSnapshot changed = WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(
                simulationTime: new SimulationTime(4L),
                personStore: people,
                propertyOwnershipStore: changedProperties,
                estateStore: changedEstates));
        WorldStateDiff diff = WorldStateDiff.Compare(snapshot, changed);
        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(
            difference => difference.Section == "PropertyOwnership"
                && difference.Identity == "diagnostic-property-2"
                && difference.ChangeKind == WorldStateDifferenceChangeKind.Added));
    }
}
