using System;
using NUnit.Framework;

public sealed class SuccessionIntegrationTests
{
    [Test]
    public void EstateSuccessionTransfersExplicitlySelectedChildAndRecordsHistory()
    {
        SimulationRuntime world = CreateEstateWorld(
            out EstateId estateId,
            out PropertyId propertyId,
            out PersonId deceasedId,
            out PersonId selectedCandidateId,
            out _);

        Assert.That(world.PropertyOwnershipStore.TransferHistory, Is.Empty);
        Assert.That(world.PropertyOwnershipRecords[0].OwnerPersonId, Is.EqualTo(deceasedId));
        Assert.That(world.TryProposeEstateSuccession(
                estateId,
                propertyId,
                selectedCandidateId,
                world.CurrentDay,
                out EstateSuccessionTransition transition,
                out EstateSuccessionFailure proposalFailure),
            Is.True,
            proposalFailure.ToString());
        Assert.That(world.PropertyOwnershipStore.TransferHistory, Is.Empty);

        Assert.That(world.TryApplyEstateSuccession(
                transition,
                out EstateSuccessionFailure applyFailure),
            Is.True,
            applyFailure.ToString());
        Assert.That(world.PropertyOwnershipStore.TryGet(
            propertyId,
            out PropertyOwnershipRecord ownership), Is.True);
        Assert.That(ownership.OwnerPersonId, Is.EqualTo(selectedCandidateId));
        Assert.That(world.PropertyOwnershipStore.TransferHistory, Has.Count.EqualTo(1));
        Assert.That(world.PropertyOwnershipStore.TransferHistory[0].PreviousOwnerPersonId,
            Is.EqualTo(deceasedId));
        Assert.That(world.PropertyOwnershipStore.TransferHistory[0].NewOwnerPersonId,
            Is.EqualTo(selectedCandidateId));
        Assert.That(world.EstateRecords, Has.Count.EqualTo(1));

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(
            new WorldStateSnapshotContext(
                simulationTime: world.SimulationTime,
                calendar: world.Calendar,
                personStore: world.PersonStore,
                parentages: world.GenealogyRecords,
                propertyOwnershipStore: world.PropertyOwnershipStore,
                estateStore: world.EstateStore));
        Assert.That(snapshot.PropertyTransferCount, Is.EqualTo(1));
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain(
            "PROPERTY_TRANSFER|estate-property|estate-deceased|estate-child-selected|10000"));
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True);
    }

    [Test]
    public void EstateSuccessionRejectsAProposalAfterSelectedCandidateDies()
    {
        SimulationRuntime world = CreateEstateWorld(
            out EstateId estateId,
            out PropertyId propertyId,
            out _,
            out PersonId selectedCandidateId,
            out _);

        Assert.That(world.TryProposeEstateSuccession(
                estateId,
                propertyId,
                selectedCandidateId,
                world.CurrentDay,
                out EstateSuccessionTransition transition,
                out EstateSuccessionFailure proposalFailure),
            Is.True,
            proposalFailure.ToString());
        Assert.That(world.TryApplyPersonDeath(
                selectedCandidateId,
                out _,
                out PersonDeathLifecycleFailure deathFailure),
            Is.True,
            deathFailure.ToString());

        Assert.That(world.TryApplyEstateSuccession(
                transition,
                out EstateSuccessionFailure applyFailure), Is.False);
        Assert.That(applyFailure.Code, Is.EqualTo(EstateSuccessionFailureCode.StaleCandidateSet));
        Assert.That(world.PropertyOwnershipStore.TransferHistory, Is.Empty);
        Assert.That(world.PropertyOwnershipRecords[0].OwnerPersonId, Is.Not.EqualTo(selectedCandidateId));
    }

    [Test]
    public void OfficeSuccessionRequiresRecognizedVacancyAndAssignsExplicitChild()
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = new InstitutionId("succession-institution");
        Assert.That(institutions.TryRegister(
            new InstitutionRecord(institutionId), out _), Is.True);
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = new OfficeId("succession-office");
        Assert.That(offices.TryRegister(
            new OfficeRecord(officeId, institutionId), out _), Is.True);

        PersonStore people = new PersonStore();
        PersonRuntime deceased = Register(people, "office-deceased", 0L, 10_000L);
        PersonRuntime child = Register(people, "office-child", 0L);
        Assert.That(offices.TryAssignIncumbent(
            officeId,
            deceased.PersonId,
            1L,
            out _), Is.True);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(
            deceased.PersonId,
            child.PersonId,
            out _), Is.True);

        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(10_000L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people,
            genealogyStore: genealogy,
            institutionStore: institutions,
            officeStore: offices);

        Assert.That(world.TryProposeInstitutionalVacancyRecognition(
                officeId,
                InstitutionalVacancyRecognitionReason.FactualDeath,
                out InstitutionalVacancyRecognitionTransition vacancyTransition,
                out InstitutionalVacancyRecognitionFailure vacancyProposalFailure),
            Is.True,
            vacancyProposalFailure.ToString());
        Assert.That(world.TryApplyInstitutionalVacancyRecognition(
                vacancyTransition,
                out InstitutionalVacancyRecognitionFailure vacancyApplyFailure),
            Is.True,
            vacancyApplyFailure.ToString());
        Assert.That(world.IsOfficeVacant(officeId), Is.True);

        Assert.That(world.TryProposeOfficeSuccession(
                officeId,
                child.PersonId,
                10_000L,
                out OfficeSuccessionTransition successionTransition,
                out OfficeSuccessionFailure successionProposalFailure),
            Is.True,
            successionProposalFailure.ToString());
        Assert.That(world.TryApplyOfficeSuccession(
                successionTransition,
                out OfficeSuccessionFailure successionApplyFailure),
            Is.True,
            successionApplyFailure.ToString());
        Assert.That(world.TryGetCurrentOfficeIncumbent(
            officeId,
            out PersonId currentIncumbent), Is.True);
        Assert.That(currentIncumbent, Is.EqualTo(child.PersonId));
        Assert.That(world.OfficeTenureHistory, Has.Count.EqualTo(2));
        Assert.That(world.OfficeTenureHistory[0].IsClosed, Is.True);
        Assert.That(world.OfficeTenureHistory[1].IsOpen, Is.True);
    }

    [Test]
    public void OfficeSuccessionCannotBypassInstitutionalVacancyRecognition()
    {
        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = new InstitutionId("occupied-institution");
        Assert.That(institutions.TryRegister(
            new InstitutionRecord(institutionId), out _), Is.True);
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = new OfficeId("occupied-office");
        Assert.That(offices.TryRegister(
            new OfficeRecord(officeId, institutionId), out _), Is.True);
        PersonStore people = new PersonStore();
        PersonRuntime incumbent = Register(people, "occupied-incumbent", 0L);
        PersonRuntime child = Register(people, "occupied-child", 0L);
        Assert.That(offices.TryAssignIncumbent(
            officeId,
            incumbent.PersonId,
            1L,
            out _), Is.True);
        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(
            incumbent.PersonId,
            child.PersonId,
            out _), Is.True);

        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(5L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people,
            genealogyStore: genealogy,
            institutionStore: institutions,
            officeStore: offices);

        Assert.That(world.TryProposeOfficeSuccession(
                officeId,
                child.PersonId,
                5L,
                out _,
                out OfficeSuccessionFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(OfficeSuccessionFailureCode.OfficeNotVacant));
    }

    private static SimulationRuntime CreateEstateWorld(
        out EstateId estateId,
        out PropertyId propertyId,
        out PersonId deceasedId,
        out PersonId selectedCandidateId,
        out PersonId otherCandidateId)
    {
        PersonStore people = new PersonStore();
        PersonRuntime deceased = Register(people, "estate-deceased", 0L, 10_000L);
        PersonRuntime selectedCandidate = Register(people, "estate-child-selected", 0L);
        PersonRuntime otherCandidate = Register(people, "estate-child-other", 0L);
        deceasedId = deceased.PersonId;
        selectedCandidateId = selectedCandidate.PersonId;
        otherCandidateId = otherCandidate.PersonId;

        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(
            deceased.PersonId,
            selectedCandidate.PersonId,
            out _), Is.True);
        Assert.That(genealogy.TryAddParentage(
            deceased.PersonId,
            otherCandidate.PersonId,
            out _), Is.True);

        propertyId = new PropertyId("estate-property");
        PropertyOwnershipStore properties = new PropertyOwnershipStore();
        Assert.That(properties.TryRegister(
            new PropertyOwnershipRecord(propertyId, deceased.PersonId),
            out _), Is.True);

        estateId = new EstateId("estate-record");
        EstateStore estates = new EstateStore(people);
        Assert.That(EstateOpeningSystem.TryOpenEstate(
            people,
            estates,
            estateId,
            deceased.PersonId,
            10_000L,
            out _,
            out EstateFoundationFailure estateFailure), Is.True, estateFailure.ToString());

        return new SimulationRuntime(
            new SimulationTime(10_000L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people,
            genealogyStore: genealogy,
            propertyOwnershipStore: properties,
            estateStore: estates);
    }

    private static PersonRuntime Register(
        PersonStore people,
        string id,
        long birthAbsoluteDay,
        long? deathAbsoluteDay = null)
    {
        PersonRuntime person = new PersonRuntime(
            new PersonId(id),
            birthAbsoluteDay,
            deathAbsoluteDay);
        Assert.That(people.TryRegister(person, out _), Is.True);
        return person;
    }
}
