using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public sealed class EstatePropertyFoundationTests
{
    [Test]
    public void IdsUseOrdinalValueSemanticsAndRejectInvalidValues()
    {
        Assert.That(new PropertyId("property-a"), Is.EqualTo(new PropertyId("property-a")));
        Assert.That(new PropertyId("property-a"), Is.Not.EqualTo(new PropertyId("PROPERTY-A")));
        Assert.Throws<ArgumentException>(() => new PropertyId(" "));
        Assert.That(new EstateId("estate-a"), Is.EqualTo(new EstateId("estate-a")));
        Assert.That(new EstateId("estate-a"), Is.Not.EqualTo(new EstateId("ESTATE-A")));
        Assert.Throws<ArgumentException>(() => new EstateId("\t"));
    }

    [Test]
    public void PropertyOwnership_IsIndependentOfExecutionRuntimeAndDeterministic()
    {
        PropertyOwnershipStore store = new PropertyOwnershipStore();
        PersonId owner = new PersonId("person-owner");
        PropertyOwnershipRecord zeta = new PropertyOwnershipRecord(
            new PropertyId("property-z"), owner);
        PropertyOwnershipRecord alpha = new PropertyOwnershipRecord(
            new PropertyId("property-a"), owner);

        Assert.That(store.TryRegister(zeta, out _), Is.True);
        Assert.That(store.TryRegister(alpha, out _), Is.True);
        Assert.That(store.Records[0], Is.SameAs(alpha));
        Assert.That(store.Records[1], Is.SameAs(zeta));
        Assert.That(store.GetOwnedBy(new PersonId("person-owner")), Has.Count.EqualTo(2));
    }

    [Test]
    public void PropertyOwnership_DuplicateRegistrationFailsAtomicallyAndStoresAreIsolated()
    {
        PropertyOwnershipStore first = new PropertyOwnershipStore();
        PropertyOwnershipStore second = new PropertyOwnershipStore();
        PropertyOwnershipRecord original = new PropertyOwnershipRecord(
            new PropertyId("shared-property"), new PersonId("first-owner"));

        Assert.That(first.TryRegister(original, out _), Is.True);
        Assert.That(first.TryRegister(
            new PropertyOwnershipRecord(
                new PropertyId("shared-property"), new PersonId("replacement-owner")),
            out PropertyFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PropertyFoundationFailureCode.DuplicatePropertyId));
        Assert.That(first.Count, Is.EqualTo(1));
        Assert.That(first.TryGet(new PropertyId("shared-property"), out PropertyOwnershipRecord preserved), Is.True);
        Assert.That(preserved, Is.SameAs(original));
        Assert.That(second.TryRegister(
            new PropertyOwnershipRecord(
                new PropertyId("shared-property"), new PersonId("second-owner")), out _), Is.True);
    }

    [Test]
    public void DeadPersonCanRetainPropertyOwnershipWithoutMaterializedExecutionState()
    {
        PersonRuntime deceased = new PersonRuntime(
            new PersonId("dead-owner"), 0L, 20L);
        PropertyOwnershipStore properties = new PropertyOwnershipStore();
        PropertyOwnershipRecord record = new PropertyOwnershipRecord(
            new PropertyId("dead-owner-home"), deceased.PersonId);

        Assert.That(properties.TryRegister(record, out _), Is.True);
        Assert.That(deceased.DeathAbsoluteDay, Is.EqualTo(20L));
        Assert.That(properties.GetOwnedBy(deceased.PersonId), Is.EqualTo(new[] { record }));
    }

    [Test]
    public void PersonDeathDoesNotCreateEstateAndOpeningRequiresExplicitDeadPersonCommand()
    {
        PersonStore people = new PersonStore();
        PersonRuntime living = new PersonRuntime(new PersonId("living-person"), 0L);
        EstateStore estates = new EstateStore();
        Assert.That(people.TryRegister(living, out _), Is.True);

        Assert.That(EstateOpeningSystem.TryProposeOpening(
            people, estates, new EstateId("living-estate"), living.PersonId, 10L,
            out EstateOpeningTransition livingTransition,
            out EstateFoundationFailure livingFailure), Is.False);
        Assert.That(livingTransition, Is.Null);
        Assert.That(livingFailure.Code, Is.EqualTo(EstateFoundationFailureCode.PersonStillLiving));
        Assert.That(estates.Count, Is.EqualTo(0));

        PersonRuntime deceased = new PersonRuntime(
            new PersonId("dead-person"), 0L, 30L);
        Assert.That(people.TryRegister(deceased, out _), Is.True);
        Assert.That(estates.Count, Is.EqualTo(0));
        Assert.That(EstateOpeningSystem.TryOpenEstate(
            people, estates, new EstateId("dead-estate"), deceased.PersonId, 35L,
            out EstateRecord estate, out EstateFoundationFailure creationFailure), Is.True,
            creationFailure.ToString());
        Assert.That(estate.DeceasedPersonId, Is.EqualTo(deceased.PersonId));
        Assert.That(estate.OpenedAbsoluteDay, Is.EqualTo(35L));
    }

    [Test]
    public void EstateOpeningRejectsBeforeDeathAndDuplicatePersonWithoutMutation()
    {
        PersonStore people = new PersonStore();
        PersonRuntime deceased = new PersonRuntime(
            new PersonId("estate-validation-person"), 0L, 20L);
        EstateStore estates = new EstateStore();
        Assert.That(people.TryRegister(deceased, out _), Is.True);

        Assert.That(EstateOpeningSystem.TryProposeOpening(
            people, estates, new EstateId("too-early"), deceased.PersonId, 19L,
            out _, out EstateFoundationFailure beforeDeathFailure), Is.False);
        Assert.That(beforeDeathFailure.Code, Is.EqualTo(EstateFoundationFailureCode.InvalidOpeningDay));
        Assert.That(estates.Count, Is.EqualTo(0));

        Assert.That(EstateOpeningSystem.TryOpenEstate(
            people, estates, new EstateId("first-estate"), deceased.PersonId, 20L,
            out _, out _), Is.True);
        Assert.That(EstateOpeningSystem.TryProposeOpening(
            people, estates, new EstateId("second-estate"), deceased.PersonId, 21L,
            out _, out EstateFoundationFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(EstateFoundationFailureCode.EstateAlreadyExistsForPerson));
        Assert.That(estates.Count, Is.EqualTo(1));
    }

    [Test]
    public void EstateOpeningTransitionRejectsStaleStoreAndCrossWorldPersonAtomically()
    {
        PersonStore firstPeople = new PersonStore();
        EstateStore firstEstates = new EstateStore();
        PersonRuntime firstPerson = new PersonRuntime(
            new PersonId("shared-dead-person"), 0L, 10L);
        Assert.That(firstPeople.TryRegister(firstPerson, out _), Is.True);
        Assert.That(EstateOpeningSystem.TryProposeOpening(
            firstPeople, firstEstates, new EstateId("first-estate"), firstPerson.PersonId, 11L,
            out EstateOpeningTransition staleTransition, out _), Is.True);

        PersonRuntime unrelated = new PersonRuntime(
            new PersonId("unrelated-dead-person"), 0L, 12L);
        Assert.That(firstPeople.TryRegister(unrelated, out _), Is.True);
        Assert.That(EstateOpeningSystem.TryOpenEstate(
            firstPeople, firstEstates, new EstateId("unrelated-estate"), unrelated.PersonId, 13L,
            out _, out _), Is.True);
        Assert.That(EstateOpeningSystem.TryApplyOpening(
            firstPeople, firstEstates, staleTransition,
            out EstateFoundationFailure staleFailure), Is.False);
        Assert.That(staleFailure.Code, Is.EqualTo(EstateFoundationFailureCode.StaleEstateStore));
        Assert.That(firstEstates.Count, Is.EqualTo(1));

        PersonStore secondPeople = new PersonStore();
        EstateStore secondEstates = new EstateStore();
        PersonRuntime secondPerson = new PersonRuntime(
            new PersonId("shared-dead-person"), 0L, 10L);
        Assert.That(secondPeople.TryRegister(secondPerson, out _), Is.True);
        Assert.That(EstateOpeningSystem.TryProposeOpening(
            firstPeople, secondEstates, new EstateId("cross-world-estate"), firstPerson.PersonId, 11L,
            out EstateOpeningTransition crossWorldTransition, out _), Is.True);
        Assert.That(EstateOpeningSystem.TryApplyOpening(
            secondPeople, secondEstates, crossWorldTransition,
            out EstateFoundationFailure crossWorldFailure), Is.False);
        Assert.That(crossWorldFailure.Code, Is.EqualTo(EstateFoundationFailureCode.StalePersonRegistration));
        Assert.That(secondEstates.Count, Is.EqualTo(0));
    }

    [Test]
    public void CoreContractsExposeOnlyImmutablePublicProperties()
    {
        Type[] types =
        {
            typeof(PropertyOwnershipRecord),
            typeof(EstateRecord),
            typeof(EstateOpeningTransition),
            typeof(PropertyFoundationFailure),
            typeof(EstateFoundationFailure)
        };

        foreach (Type type in types)
        {
            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(property.CanWrite, Is.False, type.Name + "." + property.Name);
            }
        }
    }

    [Test]
    public void CoreFilesAreUnityFreeAndDoNotReferenceExecutionRuntimeAdapters()
    {
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(), "Assets", "_Project", "Scripts", "Property");
        foreach (string file in Directory.GetFiles(directory, "*.cs"))
        {
            string source = File.ReadAllText(file);
            Assert.That(source, Does.Not.Contain("using UnityEngine;"), file);
            Assert.That(source, Does.Not.Contain("NpcRuntime"), file);
            Assert.That(source, Does.Not.Contain("SimulationRuntime"), file);
            Assert.That(source, Does.Not.Contain("DateTime.Now"), file);
        }
    }
}
