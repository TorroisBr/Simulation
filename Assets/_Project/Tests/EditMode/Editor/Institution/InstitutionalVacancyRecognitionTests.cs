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
        OfficeStore store = CreateStore(out OfficeId officeId);
        PersonId personId = new PersonId("person-dead");
        PersonRuntime living = new PersonRuntime(personId, 0L);
        PersonRuntime dead = new PersonRuntime(personId, 0L, 4L);
        Assert.That(store.TryAssignIncumbent(officeId, personId, 1L, out _), Is.True);

        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryProposeForFactualDeath(
                store,
                officeId,
                living,
                5L,
                out _,
                out InstitutionalVacancyRecognitionFailure livingFailure),
            Is.False);
        Assert.That(livingFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.IncumbentNotFactuallyDead));

        Assert.That(
            InstitutionalVacancyRecognitionSystem.TryProposeForFactualDeath(
                store,
                officeId,
                dead,
                5L,
                out InstitutionalVacancyRecognitionTransition transition,
                out InstitutionalVacancyRecognitionFailure deadFailure),
            Is.True);
        Assert.That(deadFailure, Is.EqualTo(InstitutionalVacancyRecognitionFailure.None));
        Assert.That(transition.Reason, Is.EqualTo(InstitutionalVacancyRecognitionReason.FactualDeath));
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
}
