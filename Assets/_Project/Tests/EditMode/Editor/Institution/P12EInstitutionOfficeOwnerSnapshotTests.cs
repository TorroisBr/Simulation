using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12EInstitutionOfficeOwnerSnapshotTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
            if (simulationObject != null) UnityEngine.Object.DestroyImmediate(simulationObject);
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void EmptyRequiredOwners_CaptureAndStageExactZeroWithBoundPairAndRevisions()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());

        Assert.That(P12EInstitutionOfficeOwnerSnapshot.TryCapture(runtime,
            out P12EInstitutionOfficeOwnerSnapshot snapshot,
            out P12EInstitutionOfficeSnapshotFailure captureFailure), Is.True, captureFailure.Message);
        Assert.That(snapshot.Institutions.RecordCount, Is.Zero);
        Assert.That(snapshot.Offices.RecordCount, Is.Zero);
        Assert.That(snapshot.Incumbencies.RecordCount, Is.Zero);
        Assert.That(snapshot.Tenures.RecordCount, Is.Zero);
        Assert.That(snapshot.Institutions.Revision, Is.Zero);
        Assert.That(snapshot.Offices.Revision, Is.Zero);
        Assert.That(snapshot.Incumbencies.Revision, Is.Zero);
        Assert.That(snapshot.Tenures.Revision, Is.Zero);

        Assert.That(snapshot.TryCreateStagedOwners(runtime.PersonStore,
            out InstitutionStore stagedInstitutions, out OfficeStore stagedOffices,
            out P12EInstitutionOfficeSnapshotFailure stageFailure), Is.True, stageFailure.Message);
        Assert.That(stagedInstitutions.Count, Is.Zero);
        Assert.That(stagedOffices.Count, Is.Zero);
        Assert.That(stagedOffices.IncumbencyCount, Is.Zero);
        Assert.That(stagedOffices.TenureCount, Is.Zero);
        Assert.That(stagedInstitutions.Revision, Is.Zero);
        Assert.That(stagedOffices.Revision, Is.Zero);
        Assert.That(stagedOffices.InstitutionStoreForWorldBoundary, Is.SameAs(stagedInstitutions));
        Assert.That(stagedOffices.CanBindMutationGuard(new AuthoritativeMutationGuard()), Is.True);
    }

    [Test]
    public void PopulatedCapture_PreservesRepeatedEqualClosedTenuresAndOpenIncumbencyExactly()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        PersonId firstPerson = new PersonId("p12e-office-first-person");
        PersonId secondPerson = new PersonId("p12e-office-second-person");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(firstPerson, 0L), out PersonStoreFailure firstFailure),
            Is.True, firstFailure.ToString());
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(secondPerson, 0L), out PersonStoreFailure secondFailure),
            Is.True, secondFailure.ToString());

        InstitutionId institutionId = new InstitutionId("p12e-office-institution");
        OfficeId officeId = new OfficeId("p12e-office-office");
        Assert.That(runtime.TryRegisterInstitution(new InstitutionRecord(institutionId, "Council"), out _), Is.True);
        Assert.That(runtime.TryRegisterOffice(new OfficeRecord(officeId, institutionId, "Speaker"), out _), Is.True);
        for (int repeat = 0; repeat < 2; repeat++)
        {
            Assert.That(runtime.TryAssignIncumbent(officeId, firstPerson, 10L, out _), Is.True);
            Assert.That(runtime.TryVacateOffice(officeId, out _), Is.True);
        }
        Assert.That(runtime.TryAssignIncumbent(officeId, secondPerson, null, out _), Is.True);
        IReadOnlyList<OfficeTenureRecord> sourceTenures = runtime.OfficeStoreForWorldBoundary.TenureHistoryInMutationOrder;
        Assert.That(sourceTenures, Has.Count.EqualTo(3));
        Assert.That(sourceTenures[0].Equals(sourceTenures[1]), Is.True,
            "Supported equal closed tenure occurrences must remain two separate history rows.");

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(P12EInstitutionOfficeOwnerSnapshot.TryCapture(runtime,
            out P12EInstitutionOfficeOwnerSnapshot snapshot,
            out P12EInstitutionOfficeSnapshotFailure captureFailure), Is.True, captureFailure.Message);
        Assert.That(snapshot.Institutions.Records[0].DisplayName, Is.EqualTo("Council"));
        Assert.That(snapshot.Offices.Records[0].InstitutionIdValue, Is.EqualTo(institutionId.Value));
        Assert.That(snapshot.Incumbencies.Records[0].PersonIdValue, Is.EqualTo(secondPerson.Value));
        Assert.That(snapshot.Incumbencies.Records[0].StartAbsoluteDay, Is.Null);
        Assert.That(snapshot.Tenures.Records, Has.Count.EqualTo(3));
        AssertTenureEqual(snapshot.Tenures.Records[0], snapshot.Tenures.Records[1]);
        Assert.That(snapshot.Tenures.Records[0].IsClosed, Is.True);
        Assert.That(snapshot.Tenures.Records[0].EndReason,
            Is.EqualTo((int)InstitutionalVacancyRecognitionReason.ExplicitDecision));
        Assert.That(snapshot.Tenures.Records[2].IsClosed, Is.False);
        Assert.That(snapshot.Tenures.Records[2].StartAbsoluteDay, Is.Null);
        Assert.That(snapshot.Institutions.Revision, Is.EqualTo(1L));
        Assert.That(snapshot.Offices.Revision, Is.EqualTo(6L));
        Assert.That(snapshot.Incumbencies.Revision, Is.EqualTo(6L));
        Assert.That(snapshot.Tenures.Revision, Is.EqualTo(6L));

        Assert.That(snapshot.TryCreateStagedOwners(runtime.PersonStore,
            out InstitutionStore stagedInstitutions, out OfficeStore stagedOffices,
            out P12EInstitutionOfficeSnapshotFailure stageFailure), Is.True, stageFailure.Message);
        Assert.That(stagedInstitutions.Revision, Is.EqualTo(1L));
        Assert.That(stagedOffices.Revision, Is.EqualTo(6L));
        Assert.That(stagedOffices.TryGetIncumbency(officeId, out OfficeIncumbency stagedIncumbency), Is.True);
        Assert.That(stagedIncumbency.Incumbent.Value, Is.EqualTo(secondPerson.Value));
        IReadOnlyList<OfficeTenureRecord> stagedTenures = stagedOffices.TenureHistoryInMutationOrder;
        Assert.That(stagedTenures, Has.Count.EqualTo(3));
        Assert.That(stagedTenures[0].Equals(stagedTenures[1]), Is.True);
        Assert.That(stagedTenures[0].EndReason, Is.EqualTo(InstitutionalVacancyRecognitionReason.ExplicitDecision));
        Assert.That(stagedTenures[2].IsOpen, Is.True);
        Assert.That(stagedOffices.InstitutionStoreForWorldBoundary, Is.SameAs(stagedInstitutions));
    }

    [Test]
    public void Capture_IsDetachedAndRepeatedCaptureIsDeterministic()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        PersonId person = new PersonId("p12e-office-detached-person");
        InstitutionId institution = new InstitutionId("p12e-office-detached-institution");
        OfficeId office = new OfficeId("p12e-office-detached-office");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(person, 0L), out _), Is.True);
        Assert.That(runtime.TryRegisterInstitution(new InstitutionRecord(institution, "Before"), out _), Is.True);
        Assert.That(runtime.TryRegisterOffice(new OfficeRecord(office, institution, "Before"), out _), Is.True);
        Assert.That(runtime.TryAssignIncumbent(office, person, 4L, out _), Is.True);
        Assert.That(runtime.TryAdvanceDay(out _), Is.True);
        Assert.That(P12EInstitutionOfficeOwnerSnapshot.TryCapture(runtime, out var first, out var firstFailure),
            Is.True, firstFailure.Message);
        Assert.That(P12EInstitutionOfficeOwnerSnapshot.TryCapture(runtime, out var second, out var secondFailure),
            Is.True, secondFailure.Message);
        Assert.That(first.Institutions.Records[0].DisplayName, Is.EqualTo(second.Institutions.Records[0].DisplayName));
        Assert.That(first.Offices.Records[0].DisplayName, Is.EqualTo(second.Offices.Records[0].DisplayName));
        Assert.That(first.Tenures.Records[0].StartAbsoluteDay, Is.EqualTo(second.Tenures.Records[0].StartAbsoluteDay));

        Assert.That(runtime.TryVacateOffice(office, out _), Is.True);
        Assert.That(first.Tenures.Records[0].IsClosed, Is.False);
        Assert.That(first.Tenures.Records[0].EndAbsoluteDay, Is.Null);
        Assert.That(first.Offices.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void StageRejectsDuplicateOrDanglingRowsWithoutReturningEitherOwner()
    {
        PersonStore persons = new PersonStore();
        Assert.That(persons.TryRegister(new PersonRuntime(new PersonId("p12e-stage-person"), 0L), out _), Is.True);
        P12EInstitutionOfficeOwnerSnapshot duplicate = Snapshot(
            new[] { new P12EInstitutionSnapshotRecord("i", "A"), new P12EInstitutionSnapshotRecord("i", "B") },
            Array.Empty<P12EOfficeSnapshotRecord>(), Array.Empty<P12EOfficeIncumbencySnapshotRecord>(),
            Array.Empty<P12EOfficeTenureSnapshotRecord>(), 0L, 0L);
        AssertStageFails(duplicate, persons, P12EInstitutionOfficeSnapshotFailureCode.DuplicateIdentity);

        P12EInstitutionOfficeOwnerSnapshot dangling = Snapshot(
            new[] { new P12EInstitutionSnapshotRecord("i", "A") },
            new[] { new P12EOfficeSnapshotRecord("o", "missing", "Office") },
            Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), Array.Empty<P12EOfficeTenureSnapshotRecord>(), 0L, 0L);
        AssertStageFails(dangling, persons, P12EInstitutionOfficeSnapshotFailureCode.InvalidRelation);
    }

    [Test]
    public void StageRejectsMissingPersonAndInconsistentOpenTenureWithoutPartialOwners()
    {
        PersonStore persons = new PersonStore();
        P12EInstitutionOfficeOwnerSnapshot missingPerson = Snapshot(
            new[] { new P12EInstitutionSnapshotRecord("i", "A") },
            new[] { new P12EOfficeSnapshotRecord("o", "i", "Office") },
            new[] { new P12EOfficeIncumbencySnapshotRecord("o", "missing-person", 1L) },
            new[] { new P12EOfficeTenureSnapshotRecord("o", "missing-person", 1L, null, null, false) }, 1L, 2L);
        AssertStageFails(missingPerson, persons, P12EInstitutionOfficeSnapshotFailureCode.InvalidRelation);

        Assert.That(persons.TryRegister(new PersonRuntime(new PersonId("person"), 0L), out _), Is.True);
        P12EInstitutionOfficeOwnerSnapshot orphanOpen = Snapshot(
            new[] { new P12EInstitutionSnapshotRecord("i", "A") },
            new[] { new P12EOfficeSnapshotRecord("o", "i", "Office") },
            Array.Empty<P12EOfficeIncumbencySnapshotRecord>(),
            new[] { new P12EOfficeTenureSnapshotRecord("o", "person", 1L, null, null, false) }, 1L, 1L);
        AssertStageFails(orphanOpen, persons, P12EInstitutionOfficeSnapshotFailureCode.InvalidTenure);
    }

    [Test]
    public void StageRejectsUnsupportedClosedReasonAndRetainsEqualClosedOccurrences()
    {
        PersonStore persons = new PersonStore();
        Assert.That(persons.TryRegister(new PersonRuntime(new PersonId("person"), 0L), out _), Is.True);
        P12EInstitutionOfficeOwnerSnapshot invalidReason = Snapshot(
            new[] { new P12EInstitutionSnapshotRecord("i", "A") },
            new[] { new P12EOfficeSnapshotRecord("o", "i", "Office") },
            Array.Empty<P12EOfficeIncumbencySnapshotRecord>(),
            new[] { new P12EOfficeTenureSnapshotRecord("o", "person", 1L, 2L, 999, true) }, 1L, 1L);
        AssertStageFails(invalidReason, persons, P12EInstitutionOfficeSnapshotFailureCode.InvalidTenure);

        P12EInstitutionOfficeOwnerSnapshot validRepeatedClosed = Snapshot(
            new[] { new P12EInstitutionSnapshotRecord("i", "A") },
            new[] { new P12EOfficeSnapshotRecord("o", "i", "Office") },
            Array.Empty<P12EOfficeIncumbencySnapshotRecord>(),
            new[]
            {
                new P12EOfficeTenureSnapshotRecord("o", "person", 1L, 2L,
                    (int)InstitutionalVacancyRecognitionReason.Resignation, true),
                new P12EOfficeTenureSnapshotRecord("o", "person", 1L, 2L,
                    (int)InstitutionalVacancyRecognitionReason.Resignation, true)
            }, 1L, 3L);
        Assert.That(validRepeatedClosed.TryCreateStagedOwners(persons,
            out _, out OfficeStore stagedOffices, out P12EInstitutionOfficeSnapshotFailure failure),
            Is.True, failure.Message);
        Assert.That(stagedOffices.TenureHistoryInMutationOrder, Has.Count.EqualTo(2));
        Assert.That(stagedOffices.TenureHistoryInMutationOrder[0].Equals(
            stagedOffices.TenureHistoryInMutationOrder[1]), Is.True);
    }

    [Test]
    public void CaptureRejectsRuntimeWithoutCompletedBoundary()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(P12EInstitutionOfficeOwnerSnapshot.TryCapture(runtime,
            out P12EInstitutionOfficeOwnerSnapshot snapshot,
            out P12EInstitutionOfficeSnapshotFailure failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EInstitutionOfficeSnapshotFailureCode.InvalidCaptureContext));
    }

    [Test]
    public void CaptureRejectsDuplicateWrongOwnerCardinalityRevisionSchemaAndRoleWitnesses()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(runtime.TryAdvanceDay(out _), Is.True);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken currentToken, out DailyCaptureEligibilityFailure tokenFailure),
            Is.True, tokenFailure.ToString());
        OwnerSectionCensusSnapshot source = FindSection(
            currentToken.OwnerSections, InstitutionOfficeCensusProvider.InstitutionsSectionId);

        List<OwnerSectionCensusSnapshot> duplicate = new List<OwnerSectionCensusSnapshot>(currentToken.OwnerSections);
        duplicate.Add(source);
        AssertSyntheticCaptureRejected(runtime, currentToken, duplicate,
            P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector);

        AssertSyntheticCaptureRejected(runtime, currentToken,
            ReplaceSection(currentToken.OwnerSections, source, new OwnerSectionCensusSnapshot(
                source.SectionId, source.SchemaVersion, source.Role, new object(), source.Cardinality, source.Revision)),
            P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector);
        AssertSyntheticCaptureRejected(runtime, currentToken,
            ReplaceSection(currentToken.OwnerSections, source, new OwnerSectionCensusSnapshot(
                source.SectionId, source.SchemaVersion, source.Role, source.OwnerInstanceIdentity,
                source.Cardinality + 1, source.Revision)),
            P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector);
        AssertSyntheticCaptureRejected(runtime, currentToken,
            ReplaceSection(currentToken.OwnerSections, source, new OwnerSectionCensusSnapshot(
                source.SectionId, source.SchemaVersion + 1, source.Role, source.OwnerInstanceIdentity,
                source.Cardinality, source.Revision)),
            P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector);
        AssertSyntheticCaptureRejected(runtime, currentToken,
            ReplaceSection(currentToken.OwnerSections, source, new OwnerSectionCensusSnapshot(
                source.SectionId, source.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
                source.OwnerInstanceIdentity, source.Cardinality, source.Revision)),
            P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector);
        AssertSyntheticCaptureRejected(runtime, currentToken,
            ReplaceSection(currentToken.OwnerSections, source, new OwnerSectionCensusSnapshot(
                source.SectionId, source.SchemaVersion, source.Role, source.OwnerInstanceIdentity,
                source.Cardinality, source.Revision + 1L)),
            P12EInstitutionOfficeSnapshotFailureCode.InvalidOwnerSectionVector);
    }

    private static P12EInstitutionOfficeOwnerSnapshot Snapshot(
        IReadOnlyList<P12EInstitutionSnapshotRecord> institutions,
        IReadOnlyList<P12EOfficeSnapshotRecord> offices,
        IReadOnlyList<P12EOfficeIncumbencySnapshotRecord> incumbencies,
        IReadOnlyList<P12EOfficeTenureSnapshotRecord> tenures,
        long institutionRevision,
        long officeRevision)
    {
        return new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, institutions.Count,
                institutionRevision, institutions),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, offices.Count, officeRevision, offices),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, incumbencies.Count, officeRevision, incumbencies),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, tenures.Count, officeRevision, tenures));
    }

    private static P12EInstitutionOfficeSnapshotSection<T> Section<T>(string id, int count, long revision, IEnumerable<T> rows) =>
        new P12EInstitutionOfficeSnapshotSection<T>(id, 1, count, revision, rows, null);

    private static OwnerSectionCensusSnapshot FindSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections, string id)
    {
        foreach (OwnerSectionCensusSnapshot section in sections)
            if (section.SectionId == id) return section;
        Assert.Fail("Required section is missing: " + id);
        return null;
    }

    private static List<OwnerSectionCensusSnapshot> ReplaceSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        OwnerSectionCensusSnapshot oldValue,
        OwnerSectionCensusSnapshot replacement)
    {
        List<OwnerSectionCensusSnapshot> copy = new List<OwnerSectionCensusSnapshot>(sections);
        int index = copy.IndexOf(oldValue);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        copy[index] = replacement;
        return copy;
    }

    private static void AssertSyntheticCaptureRejected(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken currentToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> alteredSections,
        P12EInstitutionOfficeSnapshotFailureCode expectedCode)
    {
        DailyCaptureEligibilityToken synthetic = new DailyCaptureEligibilityToken(
            currentToken.RuntimeInstanceIdentity,
            currentToken.AdmissionContext,
            currentToken.ConfigurationIdentity,
            currentToken.CalendarIdentity,
            currentToken.CompositionProfile,
            currentToken.WorldId,
            currentToken.AbsoluteDay,
            currentToken.CompletedCoreSequence,
            currentToken.MutationEpoch,
            alteredSections);
        Assert.That(P12EInstitutionOfficeOwnerSnapshot.TryCapture(runtime, synthetic, alteredSections,
            out P12EInstitutionOfficeOwnerSnapshot snapshot,
            out P12EInstitutionOfficeSnapshotFailure failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expectedCode));
    }

    private static void AssertStageFails(
        P12EInstitutionOfficeOwnerSnapshot snapshot,
        PersonStore persons,
        P12EInstitutionOfficeSnapshotFailureCode expectedCode)
    {
        Assert.That(snapshot.TryCreateStagedOwners(persons, out InstitutionStore institutions,
            out OfficeStore offices, out P12EInstitutionOfficeSnapshotFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(expectedCode));
        Assert.That(institutions, Is.Null);
        Assert.That(offices, Is.Null);
    }

    private static void AssertTenureEqual(
        P12EOfficeTenureSnapshotRecord left,
        P12EOfficeTenureSnapshotRecord right)
    {
        Assert.That(left.OfficeIdValue, Is.EqualTo(right.OfficeIdValue));
        Assert.That(left.PersonIdValue, Is.EqualTo(right.PersonIdValue));
        Assert.That(left.StartAbsoluteDay, Is.EqualTo(right.StartAbsoluteDay));
        Assert.That(left.EndAbsoluteDay, Is.EqualTo(right.EndAbsoluteDay));
        Assert.That(left.EndReason, Is.EqualTo(right.EndReason));
        Assert.That(left.IsClosed, Is.EqualTo(right.IsClosed));
    }

    private SimulationRuntime CreateDailyRuntime()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("p12e-institution-office-snapshot-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();
        Assert.That(simulation.Runtime, Is.Not.Null);
        return simulation.Runtime;
    }
}
