using System;
using NUnit.Framework;

public sealed class InstitutionalVacancyRecognitionTests
{
    [Test]
    public void OfficeStore_PreservesClosedTenureAfterExplicitVacancyRecognition()
    {
        OfficeStore store = CreateStore(out OfficeId officeId);
        PersonId personId = new PersonId("person-a");

        Assert.That(store.TryAssignIncumbent(officeId, personId, 10L, out _), Is.True);
        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryPropose(
                store,
                officeId,
                15L,
                InstitutionalVacancyRecognitionReason.Resignation,
                out InstitutionalVacancyRecognitionTransition transition,
                out InstitutionalVacancyRecognitionFailure proposalFailure),
            Is.True);
        Assert.That(proposalFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.None));
        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryApply(
                store,
                transition,
                out InstitutionalVacancyRecognitionFailure applyFailure),
            Is.True);
        Assert.That(applyFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.None));
        Assert.That(store.IsVacant(officeId), Is.True);
        Assert.That(store.TenureHistory, Has.Count.EqualTo(1));
        Assert.That(store.TenureHistory[0].Incumbent, Is.EqualTo(personId));
        Assert.That(store.TenureHistory[0].EndAbsoluteDay, Is.EqualTo(15L));
        Assert.That(store.TenureHistory[0].EndReason, Is.EqualTo(InstitutionalVacancyRecognitionReason.Resignation));
    }

    [Test]
    public void FactualDeathRecognitionRequiresCurrentPersonDeathTruth()
    {
        PersonId personId = new PersonId("person-dead");
        OfficeStore livingStore = CreateStore(out OfficeId livingOfficeId);
        PersonStore livingPeople = new PersonStore();
        PersonRuntime living = new PersonRuntime(personId, 0L);
        Assert.That(livingPeople.TryRegister(living, out _), Is.True);
        Assert.That(livingStore.TryAssignIncumbent(livingOfficeId, personId, 1L, out _), Is.True);
        SimulationRuntime livingWorld = CreateWorld(livingPeople, livingStore, 5L);

        Assert.That(
            livingWorld.TryProposeInstitutionalVacancyRecognition(
                livingOfficeId,
                InstitutionalVacancyRecognitionReason.FactualDeath,
                out _,
                out InstitutionalVacancyRecognitionFailure livingFailure),
            Is.False);
        Assert.That(livingFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.IncumbentNotFactuallyDead));

        OfficeStore deadOfficeStore = CreateStore(out OfficeId deadOfficeId);
        PersonStore deadPeople = new PersonStore();
        PersonRuntime dead = new PersonRuntime(personId, 0L, 4L);
        Assert.That(deadPeople.TryRegister(dead, out _), Is.True);
        Assert.That(deadOfficeStore.TryAssignIncumbent(deadOfficeId, personId, 1L, out _), Is.True);
        SimulationRuntime deadWorld = CreateWorld(deadPeople, deadOfficeStore, 5L);
        Assert.That(
            deadWorld.TryProposeInstitutionalVacancyRecognition(
                deadOfficeId,
                InstitutionalVacancyRecognitionReason.FactualDeath,
                out InstitutionalVacancyRecognitionTransition transition,
                out InstitutionalVacancyRecognitionFailure deadFailure),
            Is.True);
        Assert.That(deadFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.None));
        Assert.That(transition.Reason, Is.EqualTo(InstitutionalVacancyRecognitionReason.FactualDeath));
    }

    [Test]
    public void LegacyVacancyClosesTenureEvenWithoutAnExplicitEndDay()
    {
        OfficeStore store = CreateStore(out OfficeId officeId);
        Assert.That(store.TryAssignIncumbent(officeId, new PersonId("person"), 1L, out _), Is.True);
        Assert.That(store.TryVacateOffice(officeId, out _), Is.True);
        Assert.That(store.TenureHistory, Has.Count.EqualTo(1));
        Assert.That(store.TenureHistory[0].IsClosed, Is.True);
        Assert.That(store.TenureHistory[0].EndAbsoluteDay, Is.Null);
    }

    [Test]
    public void ApplyingStaleRecognitionDoesNotVacateReplacementIncumbent()
    {
        OfficeStore store = CreateStore(out OfficeId officeId);
        PersonId first = new PersonId("person-first");
        PersonId second = new PersonId("person-second");
        Assert.That(store.TryAssignIncumbent(officeId, first, 1L, out _), Is.True);
        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryPropose(
                store,
                officeId,
                2L,
                InstitutionalVacancyRecognitionReason.ExplicitDecision,
                out InstitutionalVacancyRecognitionTransition transition,
                out _),
            Is.True);
        Assert.That(store.TryVacateOffice(officeId, out _), Is.True);
        Assert.That(store.TryAssignIncumbent(officeId, second, 3L, out _), Is.True);

        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryApply(
                store,
                transition,
                out InstitutionalVacancyRecognitionFailure failure),
            Is.False);
        Assert.That(failure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.StaleIncumbency));
        Assert.That(store.TryGetCurrentIncumbent(officeId, out PersonId current), Is.True);
        Assert.That(current, Is.EqualTo(second));
    }

    [Test]
    public void SamePersonAndStartDayStillRejectsStaleRecognitionByIncumbencyIdentity()
    {
        OfficeStore store = CreateStore(out OfficeId officeId);
        PersonId person = new PersonId("same-person");
        Assert.That(store.TryAssignIncumbent(officeId, person, 1L, out _), Is.True);
        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryPropose(
                store,
                officeId,
                2L,
                InstitutionalVacancyRecognitionReason.ExplicitDecision,
                out InstitutionalVacancyRecognitionTransition transition,
                out _),
            Is.True);
        Assert.That(store.TryVacateOffice(officeId, out _), Is.True);
        Assert.That(store.TryAssignIncumbent(officeId, person, 1L, out _), Is.True);

        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryApply(
                store,
                transition,
                out InstitutionalVacancyRecognitionFailure failure),
            Is.False);
        Assert.That(failure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.StaleIncumbency));
        Assert.That(store.IsVacant(officeId), Is.False);
    }

    [Test]
    public void SimulationRuntimeClonesTenureHistoryAndOwnsExplicitRecognition()
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = new InstitutionId("institution-world");
        Assert.That(institutions.TryRegister(new InstitutionRecord(institutionId), out _), Is.True);
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = new OfficeId("office-world");
        Assert.That(offices.TryRegister(new OfficeRecord(officeId, institutionId), out _), Is.True);
        PersonStore people = new PersonStore();
        PersonRuntime deceased = new PersonRuntime(new PersonId("deceased-world"), 0L, 4L);
        Assert.That(people.TryRegister(deceased, out _), Is.True);
        Assert.That(offices.TryAssignIncumbent(officeId, deceased.PersonId, 1L, out _), Is.True);
        Assert.That(offices.TryVacateOffice(
            officeId,
            4L,
            InstitutionalVacancyRecognitionReason.FactualDeath,
            out _), Is.True);

        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(5L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people,
            institutionStore: institutions,
            officeStore: offices);

        Assert.That(world.OfficeTenureHistory, Has.Count.EqualTo(1));
        Assert.That(world.OfficeTenureHistory[0].IsClosed, Is.True);

        Assert.That(world.TryAssignIncumbent(officeId, deceased.PersonId, 5L, out _), Is.True);
        Assert.That(world.TryProposeInstitutionalVacancyRecognition(
            officeId,
            InstitutionalVacancyRecognitionReason.FactualDeath,
            out InstitutionalVacancyRecognitionTransition transition,
            out InstitutionalVacancyRecognitionFailure proposalFailure), Is.True);
        Assert.That(proposalFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.None));
        Assert.That(world.TryApplyInstitutionalVacancyRecognition(
            transition,
            out InstitutionalVacancyRecognitionFailure applyFailure), Is.True);
        Assert.That(applyFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.None));
        Assert.That(world.IsOfficeVacant(officeId), Is.True);
        Assert.That(world.OfficeTenureHistory, Has.Count.EqualTo(2));
    }

    [Test]
    public void RepeatedVacancyRecognitionDoesNotThrowAndPreservesHistory()
    {
        OfficeStore store = CreateStore(out OfficeId officeId);
        Assert.That(store.TryVacateOffice(officeId, out InstitutionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(InstitutionFoundationFailureCode.OfficeAlreadyVacant));
        Assert.That(store.TenureHistory, Is.Empty);
    }

    private static OfficeStore CreateStore(out OfficeId officeId)
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = new InstitutionId("institution");
        Assert.That(institutions.TryRegister(new InstitutionRecord(institutionId), out _), Is.True);
        OfficeStore offices = new OfficeStore(institutions);
        officeId = new OfficeId("office");
        Assert.That(offices.TryRegister(new OfficeRecord(officeId, institutionId), out _), Is.True);
        return offices;
    }

    private static SimulationRuntime CreateWorld(
        PersonStore people,
        OfficeStore offices,
        long currentDay)
    {
        return new SimulationRuntime(
            new SimulationTime(currentDay),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people,
            officeStore: offices);
    }
}
