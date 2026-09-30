using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class InstitutionOfficeCensusTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
        {
            if (simulationObject != null)
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void RuntimeWitnessTracksInstitutionOfficeAndIncumbencyCommits()
    {
        TesteSimulacao simulation = CreateSelectedSampleSimulation();
        SimulationRuntime runtime = simulation.Runtime;
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            simulation.Bootstrap.InstitutionOfficeCensusProviders;

        AssertWitnesses(providers, runtime, 0, 0, 0, 0, 0L, 0L);
        PersonId incumbentId = new PersonId("institution-office-census-incumbent");
        PersonId successorId = new PersonId("institution-office-census-successor");
        Assert.That(runtime.TryRegisterPerson(
            new PersonRuntime(incumbentId, 0L),
            out PersonStoreFailure incumbentRegistrationFailure), Is.True, incumbentRegistrationFailure.ToString());
        Assert.That(runtime.TryRegisterPerson(
            new PersonRuntime(successorId, 0L),
            out PersonStoreFailure successorRegistrationFailure), Is.True, successorRegistrationFailure.ToString());
        AssertWitnesses(providers, runtime, 0, 0, 0, 0, 0L, 0L);

        InstitutionId institutionId = new InstitutionId("institution-office-census-institution");
        Assert.That(runtime.TryRegisterInstitution(
            new InstitutionRecord(institutionId),
            out InstitutionFoundationFailure institutionFailure), Is.True, institutionFailure.ToString());
        AssertWitnesses(providers, runtime, 1, 0, 0, 0, 1L, 0L);

        Assert.That(runtime.TryRegisterInstitution(
            new InstitutionRecord(institutionId),
            out InstitutionFoundationFailure duplicateInstitutionFailure), Is.False);
        Assert.That(duplicateInstitutionFailure.Code,
            Is.EqualTo(InstitutionFoundationFailureCode.DuplicateInstitutionId));
        AssertWitnesses(providers, runtime, 1, 0, 0, 0, 1L, 0L);

        OfficeId officeId = new OfficeId("institution-office-census-office");
        Assert.That(runtime.TryRegisterOffice(
            new OfficeRecord(officeId, new InstitutionId("institution-office-census-missing")),
            out InstitutionFoundationFailure missingParentFailure), Is.False);
        Assert.That(missingParentFailure.Code,
            Is.EqualTo(InstitutionFoundationFailureCode.InstitutionNotFoundForOffice));
        AssertWitnesses(providers, runtime, 1, 0, 0, 0, 1L, 0L);

        Assert.That(runtime.TryRegisterOffice(
            new OfficeRecord(officeId, institutionId),
            out InstitutionFoundationFailure officeFailure), Is.True, officeFailure.ToString());
        AssertWitnesses(providers, runtime, 1, 1, 0, 0, 1L, 1L);
        Assert.That(runtime.TryRegisterOffice(
            new OfficeRecord(officeId, institutionId),
            out InstitutionFoundationFailure duplicateOfficeFailure), Is.False);
        Assert.That(duplicateOfficeFailure.Code,
            Is.EqualTo(InstitutionFoundationFailureCode.DuplicateOfficeId));
        AssertWitnesses(providers, runtime, 1, 1, 0, 0, 1L, 1L);

        Assert.That(runtime.TryAssignIncumbent(
            officeId,
            new PersonId("institution-office-census-unregistered"),
            0L,
            out InstitutionFoundationFailure unregisteredFailure), Is.False);
        Assert.That(unregisteredFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.PersonNotRegistered));
        Assert.That(runtime.TryAssignIncumbent(
            officeId,
            incumbentId,
            -1L,
            out InstitutionFoundationFailure invalidDayFailure), Is.False);
        Assert.That(invalidDayFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.InvalidStartAbsoluteDay));
        AssertWitnesses(providers, runtime, 1, 1, 0, 0, 1L, 1L);

        Assert.That(runtime.TryAssignIncumbent(
            officeId,
            incumbentId,
            0L,
            out InstitutionFoundationFailure assignmentFailure), Is.True, assignmentFailure.ToString());
        AssertWitnesses(providers, runtime, 1, 1, 1, 1, 1L, 2L);
        Assert.That(runtime.TryAssignIncumbent(
            officeId,
            successorId,
            0L,
            out InstitutionFoundationFailure occupiedFailure), Is.False);
        Assert.That(occupiedFailure.Code, Is.EqualTo(InstitutionFoundationFailureCode.OfficeAlreadyOccupied));
        AssertWitnesses(providers, runtime, 1, 1, 1, 1, 1L, 2L);

        Assert.That(runtime.TryProposeInstitutionalVacancyRecognition(
            officeId,
            InstitutionalVacancyRecognitionReason.FactualDeath,
            out _,
            out InstitutionalVacancyRecognitionFailure livingFailure), Is.False);
        Assert.That(livingFailure,
            Is.EqualTo(InstitutionalVacancyRecognitionFailure.IncumbentNotFactuallyDead));
        AssertWitnesses(providers, runtime, 1, 1, 1, 1, 1L, 2L);

        Assert.That(runtime.TryProposeInstitutionalVacancyRecognition(
            officeId,
            InstitutionalVacancyRecognitionReason.ExplicitDecision,
            out InstitutionalVacancyRecognitionTransition staleTransition,
            out InstitutionalVacancyRecognitionFailure proposalFailure), Is.True, proposalFailure.ToString());

        Assert.That(runtime.TryVacateOffice(
            officeId,
            out InstitutionFoundationFailure vacancyFailure), Is.True, vacancyFailure.ToString());
        AssertWitnesses(providers, runtime, 1, 1, 0, 1, 1L, 3L);
        Assert.That(runtime.TryVacateOffice(
            officeId,
            out InstitutionFoundationFailure alreadyVacantFailure), Is.False);
        Assert.That(alreadyVacantFailure.Code,
            Is.EqualTo(InstitutionFoundationFailureCode.OfficeAlreadyVacant));
        AssertWitnesses(providers, runtime, 1, 1, 0, 1, 1L, 3L);

        Assert.That(runtime.TryAssignIncumbent(
            officeId,
            successorId,
            0L,
            out InstitutionFoundationFailure reassignmentFailure), Is.True, reassignmentFailure.ToString());
        AssertWitnesses(providers, runtime, 1, 1, 1, 2, 1L, 4L);

        Assert.That(runtime.TryApplyInstitutionalVacancyRecognition(
            staleTransition,
            out InstitutionalVacancyRecognitionFailure staleApplyFailure), Is.False);
        Assert.That(staleApplyFailure,
            Is.EqualTo(InstitutionalVacancyRecognitionFailure.StaleIncumbency));
        AssertWitnesses(providers, runtime, 1, 1, 1, 2, 1L, 4L);
    }

    [Test]
    public void PopulatedRuntimeClonePublishesInstalledOwnersAndPreservesSource()
    {
        PersonStore people = new PersonStore();
        PersonRuntime former = RegisterPerson(people, "institution-office-clone-former");
        PersonRuntime current = RegisterPerson(people, "institution-office-clone-current");
        InstitutionStore sourceInstitutions = new InstitutionStore();
        InstitutionId institutionId = new InstitutionId("institution-office-clone-institution");
        Assert.That(sourceInstitutions.TryRegister(
            new InstitutionRecord(institutionId),
            out InstitutionFoundationFailure institutionFailure), Is.True, institutionFailure.ToString());
        OfficeStore sourceOffices = new OfficeStore(sourceInstitutions);
        OfficeId officeId = new OfficeId("institution-office-clone-office");
        Assert.That(sourceOffices.TryRegister(
            new OfficeRecord(officeId, institutionId),
            out InstitutionFoundationFailure officeFailure), Is.True, officeFailure.ToString());
        Assert.That(sourceOffices.TryAssignIncumbent(
            officeId,
            former.PersonId,
            1L,
            out InstitutionFoundationFailure formerFailure), Is.True, formerFailure.ToString());
        Assert.That(sourceOffices.TryVacateOffice(
            officeId,
            5L,
            InstitutionalVacancyRecognitionReason.ExplicitDecision,
            out InstitutionFoundationFailure vacateFailure), Is.True, vacateFailure.ToString());
        Assert.That(sourceOffices.TryAssignIncumbent(
            officeId,
            current.PersonId,
            6L,
            out InstitutionFoundationFailure currentFailure), Is.True, currentFailure.ToString());

        InstitutionRecord sourceInstitution = sourceInstitutions.Institutions[0];
        OfficeRecord sourceOffice = sourceOffices.Offices[0];
        OfficeIncumbency sourceIncumbency = sourceOffices.Incumbencies[0];
        OfficeTenureRecord sourceClosedTenure = sourceOffices.TenureHistory[0];
        OfficeTenureRecord sourceOpenTenure = sourceOffices.TenureHistory[1];
        long sourceInstitutionRevision = sourceInstitutions.Revision;
        long sourceOfficeRevision = sourceOffices.Revision;

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(6L),
            System.Array.Empty<CityRuntime>(),
            null,
            economyEnabled: false,
            personStore: people,
            institutionStore: sourceInstitutions,
            officeStore: sourceOffices);
        IReadOnlyList<IOwnerSectionCensusProvider> providers = CreateProviders(runtime);

        InstitutionStore installedInstitutions = GetInstalledOwner<InstitutionStore>(runtime, "institutionStore");
        OfficeStore installedOffices = GetInstalledOwner<OfficeStore>(runtime, "officeStore");
        Assert.That(installedInstitutions, Is.Not.SameAs(sourceInstitutions));
        Assert.That(installedOffices, Is.Not.SameAs(sourceOffices));
        Assert.That(providers[0].GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(installedInstitutions));
        for (int i = 1; i < providers.Count; i++)
        {
            Assert.That(providers[i].GetCurrentCensus().OwnerInstanceIdentity,
                Is.SameAs(installedOffices));
        }

        AssertWitnesses(providers, runtime, 1, 1, 1, 2, 1L, 3L);
        Assert.That(installedOffices.Incumbencies[0], Is.EqualTo(sourceIncumbency));
        Assert.That(installedOffices.TenureHistory[0], Is.EqualTo(sourceClosedTenure));
        Assert.That(installedOffices.TenureHistory[1], Is.EqualTo(sourceOpenTenure));

        Assert.That(sourceInstitutions.Count, Is.EqualTo(1));
        Assert.That(sourceInstitutions.Revision, Is.EqualTo(sourceInstitutionRevision));
        Assert.That(sourceInstitutions.Institutions[0], Is.SameAs(sourceInstitution));
        Assert.That(sourceOffices.Count, Is.EqualTo(1));
        Assert.That(sourceOffices.IncumbencyCount, Is.EqualTo(1));
        Assert.That(sourceOffices.TenureCount, Is.EqualTo(2));
        Assert.That(sourceOffices.Revision, Is.EqualTo(sourceOfficeRevision));
        Assert.That(sourceOffices.Offices[0], Is.SameAs(sourceOffice));
        Assert.That(sourceOffices.Incumbencies[0], Is.SameAs(sourceIncumbency));
        Assert.That(sourceOffices.TenureHistory[0], Is.SameAs(sourceClosedTenure));
        Assert.That(sourceOffices.TenureHistory[1], Is.SameAs(sourceOpenTenure));
    }

    private TesteSimulacao CreateSelectedSampleSimulation()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("institution-office-census-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        simulation.Start();
        return simulation;
    }

    private static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(SimulationRuntime runtime)
    {
        return InstitutionOfficeCensusProvider.CreateProviders(
            GetInstalledOwner<InstitutionStore>(runtime, "institutionStore"),
            GetInstalledOwner<OfficeStore>(runtime, "officeStore"));
    }

    private static T GetInstalledOwner<T>(SimulationRuntime runtime, string fieldName)
        where T : class
    {
        FieldInfo field = typeof(SimulationRuntime).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Expected runtime owner field " + fieldName + ".");
        T owner = field.GetValue(runtime) as T;
        Assert.That(owner, Is.Not.Null, "Expected runtime owner " + fieldName + ".");
        return owner;
    }

    private static PersonRuntime RegisterPerson(PersonStore people, string id)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id), 0L);
        Assert.That(people.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static void AssertWitnesses(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        SimulationRuntime runtime,
        int institutionCount,
        int officeCount,
        int incumbencyCount,
        int tenureCount,
        long institutionRevision,
        long officeRevision)
    {
        Assert.That(providers, Has.Count.EqualTo(4));
        OwnerSectionCensusWitness institution = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness offices = providers[1].GetCurrentCensus();
        OwnerSectionCensusWitness incumbencies = providers[2].GetCurrentCensus();
        OwnerSectionCensusWitness tenures = providers[3].GetCurrentCensus();
        Assert.That(institution.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.InstitutionsSectionId));
        Assert.That(offices.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.OfficesSectionId));
        Assert.That(incumbencies.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.IncumbenciesSectionId));
        Assert.That(tenures.SectionId, Is.EqualTo(InstitutionOfficeCensusProvider.TenuresSectionId));
        Assert.That(institution.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(offices.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(incumbencies.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(tenures.SchemaVersion, Is.EqualTo(InstitutionOfficeCensusProvider.SchemaVersion));
        Assert.That(institution.OwnerInstanceIdentity,
            Is.SameAs(GetInstalledOwner<InstitutionStore>(runtime, "institutionStore")));
        Assert.That(offices.OwnerInstanceIdentity,
            Is.SameAs(GetInstalledOwner<OfficeStore>(runtime, "officeStore")));
        Assert.That(incumbencies.OwnerInstanceIdentity, Is.SameAs(offices.OwnerInstanceIdentity));
        Assert.That(tenures.OwnerInstanceIdentity, Is.SameAs(offices.OwnerInstanceIdentity));
        Assert.That(institution.Cardinality, Is.EqualTo(institutionCount));
        Assert.That(offices.Cardinality, Is.EqualTo(officeCount));
        Assert.That(incumbencies.Cardinality, Is.EqualTo(incumbencyCount));
        Assert.That(tenures.Cardinality, Is.EqualTo(tenureCount));
        Assert.That(institution.Revision, Is.EqualTo(institutionRevision));
        Assert.That(offices.Revision, Is.EqualTo(officeRevision));
        Assert.That(incumbencies.Revision, Is.EqualTo(officeRevision));
        Assert.That(tenures.Revision, Is.EqualTo(officeRevision));
    }
}
