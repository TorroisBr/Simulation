using System;
using NUnit.Framework;

public sealed class InstitutionWorldIntegrationTests
{
    [Test]
    public void WorldOwnsClonedInstitutionAndOfficeStateWithoutMutableStoreExposure()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "world-owner");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = RegisterOffice(offices, institutionId, "council.chair");
        Assert.That(offices.TryAssignIncumbent(officeId, person.PersonId, 4L, out _), Is.True);

        SimulationRuntime world = CreateWorld(persons, institutions, offices);

        Assert.That(world.InstitutionRecords, Has.Count.EqualTo(1));
        Assert.That(world.OfficeRecords, Has.Count.EqualTo(1));
        Assert.That(world.OfficeIncumbencies, Has.Count.EqualTo(1));
        Assert.That(world.OfficeIncumbencies[0].Incumbent, Is.EqualTo(person.PersonId));
        Assert.That(typeof(SimulationRuntime).GetProperty("InstitutionStore"), Is.Null);
        Assert.That(typeof(SimulationRuntime).GetProperty("OfficeStore"), Is.Null);

        Assert.That(institutions.TryRegister(
            new InstitutionRecord(new InstitutionId("external")),
            out _), Is.True);
        Assert.That(offices.TryVacateOffice(officeId, out _), Is.True);
        Assert.That(world.InstitutionRecords, Has.Count.EqualTo(1));
        Assert.That(world.IsOfficeVacant(officeId), Is.False);

        Assert.That(offices.TryAssignIncumbent(officeId, person.PersonId, 7L, out _), Is.True);
        Assert.That(world.TryVacateOffice(officeId, out _), Is.True);
        Assert.That(offices.IsVacant(officeId), Is.False);
    }

    [Test]
    public void WorldRejectsInstitutionAndOfficeStoresFromDifferentPairs()
    {
        InstitutionStore firstInstitutions = new InstitutionStore();
        InstitutionStore secondInstitutions = new InstitutionStore();
        InstitutionId secondId = RegisterInstitution(secondInstitutions, "second");
        OfficeStore secondOffices = new OfficeStore(secondInstitutions);
        RegisterOffice(secondOffices, secondId, "second.office");

        Assert.Throws<ArgumentException>(() => CreateWorld(
            new PersonStore(),
            firstInstitutions,
            secondOffices));
    }

    [Test]
    public void WorldRejectsInjectedIncumbencyWithoutRegisteredPerson()
    {
        PersonStore persons = new PersonStore();
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = RegisterOffice(offices, institutionId, "council.chair");
        Assert.That(offices.TryAssignIncumbent(
            officeId,
            new PersonId("missing-person"),
            null,
            out _), Is.True);

        Assert.Throws<ArgumentException>(() => CreateWorld(persons, institutions, offices));
    }

    [Test]
    public void WorldAssignmentRequiresRegisteredPersonAndPreservesStateOnFailure()
    {
        PersonStore persons = new PersonStore();
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "council");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = RegisterOffice(offices, institutionId, "council.chair");
        SimulationRuntime world = CreateWorld(persons, institutions, offices);

        Assert.That(world.TryAssignIncumbent(
            officeId,
            new PersonId("missing-person"),
            10L,
            out InstitutionFoundationFailure missingFailure), Is.False);
        Assert.That(missingFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.PersonNotRegistered));
        Assert.That(world.IsOfficeVacant(officeId), Is.True);
        Assert.That(world.OfficeIncumbencies, Is.Empty);

        PersonRuntime person = RegisterPerson(world.PersonStore, "registered-person");
        Assert.That(world.TryAssignIncumbent(
            officeId,
            person.PersonId,
            10L,
            out InstitutionFoundationFailure assignmentFailure), Is.True,
            assignmentFailure.ToString());
        Assert.That(world.TryGetCurrentOfficeIncumbent(officeId, out PersonId incumbent), Is.True);
        Assert.That(incumbent, Is.EqualTo(person.PersonId));

        Assert.That(world.TryAssignIncumbent(
            officeId,
            new PersonId("another-person"),
            20L,
            out InstitutionFoundationFailure occupiedFailure), Is.False);
        Assert.That(occupiedFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.OfficeAlreadyOccupied));
        Assert.That(world.TryGetCurrentOfficeIncumbent(officeId, out PersonId preserved), Is.True);
        Assert.That(preserved, Is.EqualTo(person.PersonId));
    }

    [Test]
    public void WorldInstitutionAndOfficeQueriesAreDeterministic()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "query-person");
        SimulationRuntime world = CreateWorld(persons);
        InstitutionId zeta = new InstitutionId("zeta");
        InstitutionId alpha = new InstitutionId("alpha");
        Assert.That(world.TryRegisterInstitution(new InstitutionRecord(zeta), out _), Is.True);
        Assert.That(world.TryRegisterInstitution(new InstitutionRecord(alpha), out _), Is.True);
        OfficeId zetaOffice = new OfficeId("zeta.office");
        OfficeId alphaOffice = new OfficeId("alpha.office");
        Assert.That(world.TryRegisterOffice(new OfficeRecord(zetaOffice, zeta), out _), Is.True);
        Assert.That(world.TryRegisterOffice(new OfficeRecord(alphaOffice, alpha), out _), Is.True);
        Assert.That(world.TryAssignIncumbent(zetaOffice, person.PersonId, out _), Is.True);

        Assert.That(world.InstitutionRecords[0].Id, Is.EqualTo(alpha));
        Assert.That(world.InstitutionRecords[1].Id, Is.EqualTo(zeta));
        Assert.That(world.OfficeRecords[0].Id, Is.EqualTo(alphaOffice));
        Assert.That(world.OfficeRecords[1].Id, Is.EqualTo(zetaOffice));
        Assert.That(world.GetOfficesForInstitution(alpha), Has.Count.EqualTo(1));
        Assert.That(world.GetOfficesHeldBy(person.PersonId), Has.Count.EqualTo(1));
        Assert.That(world.GetOfficesHeldBy(person.PersonId)[0].Id, Is.EqualTo(zetaOffice));
        Assert.That(world.GetVacantOffices()[0].Id, Is.EqualTo(alphaOffice));
    }

    [Test]
    public void WorldsCloneInstitutionStateAndRemainIsolated()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "shared-person");
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = RegisterInstitution(institutions, "shared-institution");
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = RegisterOffice(offices, institutionId, "shared-office");

        SimulationRuntime first = CreateWorld(persons, institutions, offices);
        PersonStore secondPersons = new PersonStore();
        Assert.That(secondPersons.TryRegister(new PersonRuntime(person.PersonId), out _), Is.True);
        SimulationRuntime second = CreateWorld(secondPersons, institutions, offices);

        Assert.That(first.TryAssignIncumbent(officeId, person.PersonId, out _), Is.True);
        Assert.That(first.IsOfficeVacant(officeId), Is.False);
        Assert.That(second.IsOfficeVacant(officeId), Is.True);
        Assert.That(offices.IsVacant(officeId), Is.True);
    }

    private static SimulationRuntime CreateWorld(
        PersonStore persons,
        InstitutionStore institutions = null,
        OfficeStore offices = null)
    {
        return new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            economyEnabled: false,
            personStore: persons,
            institutionStore: institutions,
            officeStore: offices);
    }

    private static PersonRuntime RegisterPerson(PersonStore store, string value)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(value));
        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static InstitutionId RegisterInstitution(InstitutionStore store, string value)
    {
        InstitutionId id = new InstitutionId(value);
        Assert.That(store.TryRegister(new InstitutionRecord(id), out InstitutionFoundationFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.None));
        return id;
    }

    private static OfficeId RegisterOffice(OfficeStore store, InstitutionId institutionId, string value)
    {
        OfficeId id = new OfficeId(value);
        Assert.That(store.TryRegister(new OfficeRecord(id, institutionId), out InstitutionFoundationFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.None));
        return id;
    }
}
