using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class P12EJusticeRecordsOwnerSnapshotTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void CaptureAndStagePreserveOrderedHistorySentenceLinksAndBothTargetIdentityForms()
    {
        CityRuntime firstCity = CreateCity("p12e-justice-city-a");
        CityRuntime secondCity = CreateCity("p12e-justice-city-b");
        NpcRuntime personTarget = CreateNpc("p12e-justice-person-target");
        NpcRuntime legacyTarget = CreateNpc("p12e-justice-legacy-target");
        NpcRuntime personGuard = CreateNpc("p12e-justice-person-guard");
        NpcRuntime legacyGuard = CreateNpc("p12e-justice-legacy-guard");
        PersonId targetPersonId = new PersonId("p12e-justice-person-id");
        PersonStore people = new PersonStore();
        PersonRuntime person = new PersonRuntime(targetPersonId, 0L);
        Assert.That(people.TryRegister(person, out PersonStoreFailure personFailure), Is.True, personFailure.ToString());
        Assert.That(people.TryBindMaterializedNpc(targetPersonId, personTarget.RuntimeId, out personFailure),
            Is.True, personFailure.ToString());
        Assert.That(personTarget.TryAssignPersonId(targetPersonId), Is.True);
        Assert.That(personTarget.TryBindPersonRuntime(person), Is.True);

        JusticeConfiguration justiceConfiguration = CreateJusticeConfiguration("p12e-justice-populated");
        JusticeSystem justice = justiceConfiguration.CreateOwner();
        WantedRecordRuntime resolvedHistory = justice.CreateOrIncreaseWarrant(personTarget, firstCity, 3.5f, 2);
        Assert.That(resolvedHistory, Is.Not.Null);
        resolvedHistory.Resolve();
        WantedRecordRuntime currentWarrant = justice.CreateOrIncreaseWarrant(personTarget, firstCity, 12.25f, 5);
        WantedRecordRuntime legacyWarrant = justice.CreateOrIncreaseWarrant(legacyTarget, secondCity, 8f, 3);
        Assert.That(currentWarrant, Is.Not.Null);
        Assert.That(legacyWarrant, Is.Not.Null);
        Assert.That(justice.Arrest(personGuard, personTarget, firstCity), Is.True);
        Assert.That(justice.Arrest(legacyGuard, legacyTarget, secondCity), Is.True);

        SimulationRuntime runtime = CreateRuntime(
            new[] { firstCity, secondCity },
            new[] { personTarget, legacyTarget, personGuard, legacyGuard },
            justice,
            people);
        CompleteDay(runtime);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EJusticeRecordsOwnerSnapshot snapshot,
            out P12EJusticeSnapshotFailure captureFailure), Is.True, captureFailure?.Message);

        Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(token.AbsoluteDay));
        Assert.That(snapshot.Records.SectionId, Is.EqualTo(P12EJusticeRecordsSnapshotSection.CurrentSectionId));
        Assert.That(snapshot.Records.SchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.Records.RecordCount, Is.EqualTo(5));
        Assert.That(snapshot.Records.WantedRows, Has.Count.EqualTo(3));
        Assert.That(snapshot.Records.SentenceRows, Has.Count.EqualTo(2));
        Assert.That(snapshot.Records.Revision, Is.EqualTo(justice.P12CrimeJusticeRevision));
        Assert.That(snapshot.Records.WantedRows[0].IsResolved, Is.True);
        Assert.That(snapshot.Records.WantedRows[0].Bounty, Is.EqualTo(3.5f));
        Assert.That(snapshot.Records.WantedRows[1].TargetPersonIdValue, Is.EqualTo(targetPersonId.Value));
        Assert.That(snapshot.Records.WantedRows[1].Bounty, Is.EqualTo(12.25f));
        Assert.That(snapshot.Records.WantedRows[2].TargetPersonIdValue, Is.Null);
        Assert.That(snapshot.Records.SentenceRows[0].WarrantOrdinal, Is.EqualTo(1));
        Assert.That(snapshot.Records.SentenceRows[1].WarrantOrdinal, Is.EqualTo(2));
        Assert.That(snapshot.Records.SentenceRows[0].WasArrestedToday, Is.False,
            "The accepted completed Daily-v1 boundary has already applied Justice's begin-day reset.");

        OwnerSectionCensusSnapshot justiceWitness = FindWitness(token.OwnerSections,
            P12CrimeJusticeCensusProvider.JusticeRecordsSectionId);
        OwnerSectionCensusSnapshot receiptWitness = FindWitness(token.OwnerSections,
            P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId);
        Assert.That(justiceWitness.OwnerInstanceIdentity, Is.SameAs(justice));
        Assert.That(justiceWitness.Cardinality, Is.EqualTo(snapshot.Records.RecordCount));
        Assert.That(justiceWitness.Revision, Is.EqualTo(snapshot.Records.Revision));
        Assert.That(receiptWitness.OwnerInstanceIdentity, Is.SameAs(justice));
        Assert.That(receiptWitness.Cardinality, Is.EqualTo(1));
        Assert.That(receiptWitness.Revision, Is.Zero);

        StagedRoots roots = CreateStagedRoots(
            new[] { firstCity, secondCity },
            new[] { personTarget, legacyTarget, personGuard, legacyGuard });
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        int statusCountBefore = roots.Npcs[0].CurrentStatus.Count;
        Assert.That(snapshot.TryStage(
            roots.Cities, roots.Npcs, roots.People,
            justiceConfiguration.FreeStatus, justiceConfiguration.WantedStatus,
            justiceConfiguration.ArrestedStatus, justiceConfiguration.HiddenStatus,
            records.EventRecorder, new SimulationLogger(null),
            out JusticeSystem staged, out P12EJusticeSnapshotFailure stageFailure),
            Is.True, stageFailure?.Message);

        List<WantedRecordRuntime> stagedWanted = ReadOwnerRows<WantedRecordRuntime>(staged, "wantedRecords");
        List<PrisonSentenceRuntime> stagedSentences = ReadOwnerRows<PrisonSentenceRuntime>(staged, "prisonSentences");
        Assert.That(staged.P12WantedRecordCount, Is.EqualTo(3));
        Assert.That(staged.P12PrisonSentenceCount, Is.EqualTo(2));
        Assert.That(staged.P12CrimeJusticeRevision, Is.EqualTo(snapshot.Records.Revision));
        Assert.That(staged.P12P18ReceiptCensusRevision, Is.Zero);
        Assert.That(stagedWanted[0].Bounty, Is.EqualTo(3.5f));
        Assert.That(stagedWanted[0].IsActive, Is.False);
        Assert.That(stagedWanted[1].Target, Is.SameAs(roots.Npcs[0]));
        Assert.That(stagedWanted[1].Target.PersonId.Value, Is.EqualTo(targetPersonId.Value));
        Assert.That(stagedWanted[2].Target, Is.SameAs(roots.Npcs[1]));
        Assert.That(stagedWanted[2].Target.PersonId, Is.Null);
        Assert.That(stagedSentences[0].Warrant, Is.SameAs(stagedWanted[1]));
        Assert.That(stagedSentences[1].Warrant, Is.SameAs(stagedWanted[2]));
        Assert.That(stagedSentences[0].Target, Is.SameAs(stagedWanted[1].Target));
        Assert.That(stagedSentences[1].City, Is.SameAs(stagedWanted[2].City));
        Assert.That(roots.Npcs[0].CurrentStatus.Count, Is.EqualTo(statusCountBefore),
            "Staging must not synchronize status or replay Justice effects.");
        Assert.That(records.Events.Events, Is.Empty, "Staging must not emit domain events.");
        Assert.That(RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(records.Allocator)
            .GetCurrentCensus().Revision, Is.Zero, "Staging must not allocate EventIds.");
        Assert.That(records.Sequence.CensusRevision, Is.Zero, "Staging must not allocate record sequences.");
        int admissionCalls = 0;
        int committedCalls = 0;
        Assert.That(staged.TryBindP12CrimeJusticeMutationBoundary(
            _ => { admissionCalls++; return true; }, _ => committedCalls++), Is.True,
            "Private staging must leave the new owner and its rows unbound for the later P12-G publication boundary.");
        Assert.That(admissionCalls, Is.Zero);
        Assert.That(committedCalls, Is.Zero);

        long capturedRevision = justice.P12CrimeJusticeRevision;
        CompleteDay(runtime);
        Assert.That(justice.P12CrimeJusticeRevision, Is.GreaterThan(capturedRevision));
        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EJusticeRecordsOwnerSnapshot stale, out _), Is.False);
        Assert.That(stale, Is.Null);
    }

    [Test]
    public void EmptyCaptureRequiresExactTokenVectorAndRejectsStaleOrNonzeroJusticeReceiptWitness()
    {
        CityRuntime city = CreateCity("p12e-justice-empty-city");
        NpcRuntime npc = CreateNpc("p12e-justice-empty-npc");
        JusticeConfiguration configuration = CreateJusticeConfiguration("p12e-justice-empty");
        JusticeSystem justice = configuration.CreateOwner();
        SimulationRuntime runtime = CreateRuntime(new[] { city }, new[] { npc }, justice, new PersonStore());
        CompleteDay(runtime);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());

        List<OwnerSectionCensusSnapshot> copiedVector = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, token, copiedVector,
            out P12EJusticeRecordsOwnerSnapshot rejected, out _), Is.False);
        Assert.That(rejected, Is.Null);

        OwnerSectionCensusSnapshot justiceSection = FindWitness(token.OwnerSections,
            P12CrimeJusticeCensusProvider.JusticeRecordsSectionId);
        int justiceIndex = IndexOfWitness(token.OwnerSections, justiceSection);
        List<IReadOnlyList<OwnerSectionCensusSnapshot>> alteredVectors =
            new List<IReadOnlyList<OwnerSectionCensusSnapshot>>();

        List<OwnerSectionCensusSnapshot> missingWitness = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        missingWitness.RemoveAt(justiceIndex);
        alteredVectors.Add(missingWitness);

        List<OwnerSectionCensusSnapshot> duplicatedWitness = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        duplicatedWitness.Add(justiceSection);
        alteredVectors.Add(duplicatedWitness);

        List<OwnerSectionCensusSnapshot> reorderedWitnesses = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        reorderedWitnesses.Reverse();
        alteredVectors.Add(reorderedWitnesses);

        List<OwnerSectionCensusSnapshot> wrongOwnerWitness = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        wrongOwnerWitness[justiceIndex] = new OwnerSectionCensusSnapshot(
            justiceSection.SectionId, justiceSection.SchemaVersion, justiceSection.Role,
            new JusticeConfiguration(
                configuration.FreeStatus, configuration.WantedStatus,
                configuration.ArrestedStatus, configuration.HiddenStatus).CreateOwner(),
            justiceSection.Cardinality, justiceSection.Revision);
        alteredVectors.Add(wrongOwnerWitness);

        foreach (IReadOnlyList<OwnerSectionCensusSnapshot> alteredVector in alteredVectors)
        {
            Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, token, alteredVector,
                out rejected, out _), Is.False);
            Assert.That(rejected, Is.Null);
        }

        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EJusticeRecordsOwnerSnapshot empty, out P12EJusticeSnapshotFailure failure),
            Is.True, failure?.Message);
        Assert.That(empty.Records.RecordCount, Is.Zero);
        Assert.That(empty.Records.WantedRows, Is.Empty);
        Assert.That(empty.Records.SentenceRows, Is.Empty);
        Assert.That(empty.Records.Revision, Is.Zero);

        StagedRoots roots = CreateStagedRoots(new[] { city }, new[] { npc });
        Assert.That(empty.TryStage(
            roots.Cities, roots.Npcs, roots.People,
            configuration.FreeStatus, configuration.WantedStatus,
            configuration.ArrestedStatus, configuration.HiddenStatus,
            null, new SimulationLogger(null),
            out JusticeSystem staged, out failure), Is.True, failure?.Message);
        Assert.That(staged.P12WantedRecordCount, Is.Zero);
        Assert.That(staged.P12PrisonSentenceCount, Is.Zero);
        Assert.That(staged.P12CrimeJusticeRevision, Is.Zero);
        Assert.That(staged.P12P18ReceiptCensusRevision, Is.Zero);

        CompleteDay(runtime);
        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EJusticeRecordsOwnerSnapshot staleBoundary, out _), Is.False);
        Assert.That(staleBoundary, Is.Null);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken currentToken,
            out tokenFailure), Is.True, tokenFailure.ToString());

        FieldInfo receiptRevision = typeof(JusticeSystem).GetField(
            "beginDayStepRevision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(receiptRevision, Is.Not.Null);
        receiptRevision.SetValue(justice, 1L);
        Assert.That(justice.P12P18ReceiptCensusRevision, Is.EqualTo(1L));
        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, currentToken, currentToken.OwnerSections,
            out P12EJusticeRecordsOwnerSnapshot nonzeroReceipt, out _), Is.False);
        Assert.That(nonzeroReceipt, Is.Null);

    }

    [Test]
    public void ReceiptWitnessValidatorRejectsNonzeroRevisionCardinalityAndWrongOwner()
    {
        JusticeConfiguration configuration = CreateJusticeConfiguration("p12e-justice-receipt-witness");
        JusticeSystem owner = configuration.CreateOwner();
        MethodInfo validate = typeof(P12EJusticeRecordsOwnerSnapshot).GetMethod(
            "TryMatchesReceiptWitness", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(validate, Is.Not.Null);

        OwnerSectionCensusSnapshot exactZero = new OwnerSectionCensusSnapshot(
            P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId,
            P12CrimeJusticeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
            owner, 1, 0L);
        Assert.That((bool)validate.Invoke(null, new object[] { exactZero, owner }), Is.True);

        OwnerSectionCensusSnapshot nonzeroRevision = new OwnerSectionCensusSnapshot(
            exactZero.SectionId, exactZero.SchemaVersion, exactZero.Role,
            owner, 1, 1L);
        Assert.That((bool)validate.Invoke(null, new object[] { nonzeroRevision, owner }), Is.False);

        OwnerSectionCensusSnapshot wrongCardinality = new OwnerSectionCensusSnapshot(
            exactZero.SectionId, exactZero.SchemaVersion, exactZero.Role,
            owner, 2, 0L);
        Assert.That((bool)validate.Invoke(null, new object[] { wrongCardinality, owner }), Is.False);

        OwnerSectionCensusSnapshot wrongOwner = new OwnerSectionCensusSnapshot(
            exactZero.SectionId, exactZero.SchemaVersion, exactZero.Role,
            configuration.CreateOwner(), 1, 0L);
        Assert.That((bool)validate.Invoke(null, new object[] { wrongOwner, owner }), Is.False);
        Assert.That((bool)validate.Invoke(null, new object[] { null, owner }), Is.False);
    }

    [Test]
    public void StageRejectsPersonBindingMismatchAndMalformedSentenceWarrantRelation()
    {
        CityRuntime city = CreateCity("p12e-justice-stage-city");
        NpcRuntime npc = CreateNpc("p12e-justice-stage-target");
        NpcRuntime mismatchNpc = CreateNpc(npc.RuntimeId);
        PersonId wrongPersonId = new PersonId("p12e-justice-wrong-person");
        PersonStore wrongPeople = new PersonStore();
        PersonRuntime wrongPerson = new PersonRuntime(wrongPersonId, 0L);
        Assert.That(wrongPeople.TryRegister(wrongPerson, out _), Is.True);
        Assert.That(wrongPeople.TryBindMaterializedNpc(wrongPersonId, mismatchNpc.RuntimeId, out _), Is.True);
        Assert.That(mismatchNpc.TryAssignPersonId(wrongPersonId), Is.True);
        Assert.That(mismatchNpc.TryBindPersonRuntime(wrongPerson), Is.True);
        JusticeConfiguration mismatchConfiguration = CreateJusticeConfiguration("p12e-justice-stage-mismatch");

        P12EJusticeRecordsOwnerSnapshot mismatch = Snapshot(
            new[] { Wanted(0, npc.RuntimeId, "p12e-justice-expected-person", city.RuntimeId, 5f, 2, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L);
        Assert.That(mismatch.TryStage(new[] { city }, new[] { mismatchNpc }, wrongPeople,
            mismatchConfiguration.FreeStatus,
            mismatchConfiguration.WantedStatus,
            mismatchConfiguration.ArrestedStatus,
            mismatchConfiguration.HiddenStatus,
            null, new SimulationLogger(null), out JusticeSystem rejected,
            out P12EJusticeSnapshotFailure failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidReference));
        Assert.That(wrongPeople.Persons, Has.Count.EqualTo(1));

        P12EJusticeRecordsOwnerSnapshot invalidLink = Snapshot(
            new[]
            {
                Wanted(0, npc.RuntimeId, null, city.RuntimeId, 5f, 2, false),
                Wanted(1, "another-target", null, "another-city", 3f, 1, false)
            },
            new[] { Sentence(0, npc.RuntimeId, null, city.RuntimeId, 1, 1, 0, true) }, 0L);
        Assert.That(invalidLink.TryStage(null, null, null, null, null, null, null, null, null,
            out rejected, out failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidRelation));

        P12EJusticeRecordsOwnerSnapshot invalidOrdinal = Snapshot(
            new[] { Wanted(1, npc.RuntimeId, null, city.RuntimeId, 5f, 2, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L);
        Assert.That(invalidOrdinal.TryStage(null, null, null, null, null, null, null, null, null,
            out rejected, out failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidIdentity));
    }

    [Test]
    public void StageRejectsDanglingPersonIdentityAndBothOneSidedMaterializedBindings()
    {
        CityRuntime city = CreateCity("p12e-justice-person-identity-city");
        JusticeConfiguration configuration = CreateJusticeConfiguration("p12e-justice-person-identity");

        PersonId danglingId = new PersonId("p12e-justice-dangling-person");
        NpcRuntime danglingNpc = CreateNpc("p12e-justice-dangling-npc");
        Assert.That(danglingNpc.TryAssignPersonId(danglingId), Is.True);
        P12EJusticeRecordsOwnerSnapshot dangling = Snapshot(
            new[] { Wanted(0, danglingNpc.RuntimeId, danglingId.Value, city.RuntimeId, 1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L);
        Assert.That(dangling.TryStage(new[] { city }, new[] { danglingNpc }, new PersonStore(),
            configuration.FreeStatus, configuration.WantedStatus,
            configuration.ArrestedStatus, configuration.HiddenStatus,
            null, new SimulationLogger(null), out JusticeSystem rejected,
            out P12EJusticeSnapshotFailure failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidReference));

        PersonId storeBoundId = new PersonId("p12e-justice-store-bound-person");
        NpcRuntime storeBoundNpc = CreateNpc("p12e-justice-store-bound-npc");
        PersonStore storeBoundPeople = new PersonStore();
        PersonRuntime storeBoundPerson = new PersonRuntime(storeBoundId, 0L);
        Assert.That(storeBoundPeople.TryRegister(storeBoundPerson, out _), Is.True);
        Assert.That(storeBoundPeople.TryBindMaterializedNpc(
            storeBoundId, storeBoundNpc.RuntimeId, out _), Is.True);
        Assert.That(storeBoundNpc.TryAssignPersonId(storeBoundId), Is.True);
        P12EJusticeRecordsOwnerSnapshot storeOnlyBinding = Snapshot(
            new[] { Wanted(0, storeBoundNpc.RuntimeId, storeBoundId.Value, city.RuntimeId, 1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L);
        Assert.That(storeOnlyBinding.TryStage(new[] { city }, new[] { storeBoundNpc }, storeBoundPeople,
            configuration.FreeStatus, configuration.WantedStatus,
            configuration.ArrestedStatus, configuration.HiddenStatus,
            null, new SimulationLogger(null), out rejected, out failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidReference));

        PersonId npcBoundId = new PersonId("p12e-justice-npc-bound-person");
        NpcRuntime npcBoundNpc = CreateNpc("p12e-justice-npc-bound-npc");
        PersonStore npcBoundPeople = new PersonStore();
        PersonRuntime npcBoundPerson = new PersonRuntime(npcBoundId, 0L);
        Assert.That(npcBoundPeople.TryRegister(npcBoundPerson, out _), Is.True);
        Assert.That(npcBoundNpc.TryAssignPersonId(npcBoundId), Is.True);
        Assert.That(npcBoundNpc.TryBindPersonRuntime(npcBoundPerson), Is.True);
        P12EJusticeRecordsOwnerSnapshot npcOnlyBinding = Snapshot(
            new[] { Wanted(0, npcBoundNpc.RuntimeId, npcBoundId.Value, city.RuntimeId, 1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L);
        Assert.That(npcOnlyBinding.TryStage(new[] { city }, new[] { npcBoundNpc }, npcBoundPeople,
            configuration.FreeStatus, configuration.WantedStatus,
            configuration.ArrestedStatus, configuration.HiddenStatus,
            null, new SimulationLogger(null), out rejected, out failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidReference));
    }

    [Test]
    public void CaptureRejectsPostBoundaryDanglingAndNpcOnlyPersonBindings()
    {
        JusticeConfiguration configuration = CreateJusticeConfiguration("p12e-justice-capture-person");

        AssertCaptureRejectsPersonBinding(configuration, CreateCity("p12e-justice-capture-dangling-city"),
            "p12e-justice-capture-dangling", PersonBinding.Dangling);
        AssertCaptureRejectsPersonBinding(configuration, CreateCity("p12e-justice-capture-npc-only-city"),
            "p12e-justice-capture-npc-only", PersonBinding.NpcOnly);
    }

    [Test]
    public void StageRejectsMalformedMetadataCardinalityScalarsAndNullRows()
    {
        AssertStageRejected(new P12EJusticeRecordsOwnerSnapshot(0L,
            new P12EJusticeRecordsSnapshotSection("wrong-section", 1, 0, 0L,
                Array.Empty<P12EJusticeWantedSnapshotRow>(), Array.Empty<P12EJusticeSentenceSnapshotRow>())),
            P12EJusticeSnapshotFailureCode.UnsupportedSchema);
        AssertStageRejected(new P12EJusticeRecordsOwnerSnapshot(0L,
            new P12EJusticeRecordsSnapshotSection(P12EJusticeRecordsSnapshotSection.CurrentSectionId, 2, 0, 0L,
                Array.Empty<P12EJusticeWantedSnapshotRow>(), Array.Empty<P12EJusticeSentenceSnapshotRow>())),
            P12EJusticeSnapshotFailureCode.UnsupportedSchema);
        AssertStageRejected(Snapshot(Array.Empty<P12EJusticeWantedSnapshotRow>(),
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), -1L), P12EJusticeSnapshotFailureCode.InvalidRevision);
        AssertStageRejected(new P12EJusticeRecordsOwnerSnapshot(0L,
            new P12EJusticeRecordsSnapshotSection(P12EJusticeRecordsSnapshotSection.CurrentSectionId, 1, -1, 0L,
                Array.Empty<P12EJusticeWantedSnapshotRow>(), Array.Empty<P12EJusticeSentenceSnapshotRow>())),
            P12EJusticeSnapshotFailureCode.InvalidCardinality);
        AssertStageRejected(new P12EJusticeRecordsOwnerSnapshot(0L,
            new P12EJusticeRecordsSnapshotSection(P12EJusticeRecordsSnapshotSection.CurrentSectionId, 1, 2, 0L,
                new[] { Wanted(0, "target", null, "city", 1f, 1, false) },
                Array.Empty<P12EJusticeSentenceSnapshotRow>())), P12EJusticeSnapshotFailureCode.InvalidCardinality);

        P12EJusticeRecordsSnapshotSection overflowSection = new P12EJusticeRecordsSnapshotSection(
            P12EJusticeRecordsSnapshotSection.CurrentSectionId, 1, 0, 0L,
            Array.Empty<P12EJusticeWantedSnapshotRow>(), Array.Empty<P12EJusticeSentenceSnapshotRow>());
        SetSnapshotBackingField(overflowSection, "<WantedRows>k__BackingField",
            new ReportedCountList<P12EJusticeWantedSnapshotRow>(int.MaxValue));
        SetSnapshotBackingField(overflowSection, "<SentenceRows>k__BackingField",
            new ReportedCountList<P12EJusticeSentenceSnapshotRow>(1));
        AssertStageRejected(new P12EJusticeRecordsOwnerSnapshot(0L, overflowSection),
            P12EJusticeSnapshotFailureCode.InvalidCardinality);

        AssertStageRejected(Snapshot(
            new[] { Wanted(0, "target", null, "city", -1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L), P12EJusticeSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Snapshot(
            new[] { Wanted(0, "target", null, "city", float.NaN, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L), P12EJusticeSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Snapshot(
            new[] { Wanted(0, "target", null, "city", 1f, 0, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L), P12EJusticeSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Snapshot(
            new P12EJusticeWantedSnapshotRow[] { null },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L), P12EJusticeSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Snapshot(
            Array.Empty<P12EJusticeWantedSnapshotRow>(),
            new P12EJusticeSentenceSnapshotRow[] { null }, 0L), P12EJusticeSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Snapshot(
            new[] { Wanted(0, "target", null, "city", 1f, 1, false) },
            new[] { Sentence(0, "target", null, "city", 0, -1, 0, false) }, 0L),
            P12EJusticeSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Snapshot(
            new[] { Wanted(0, "target", null, "city", 1f, 1, false) },
            new[] { Sentence(0, "target", null, "city", 0, 1, -1, false) }, 0L),
            P12EJusticeSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Snapshot(
            new[] { Wanted(0, "target", null, "city", 1f, 1, false) },
            new[] { Sentence(1, "target", null, "city", 0, 1, 0, false) }, 0L),
            P12EJusticeSnapshotFailureCode.InvalidIdentity);

        P12EJusticeWantedSnapshotRow repeated = Wanted(0, "target", null, "city", 1f, 1, false);
        AssertStageRejected(Snapshot(
            new[] { repeated, repeated }, Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L),
            P12EJusticeSnapshotFailureCode.InvalidIdentity);
    }

    [Test]
    public void CaptureAndStageRejectDuplicateOwnerObjectsAndMissingOrAmbiguousRoots()
    {
        CityRuntime city = CreateCity("p12e-justice-duplicate-city");
        NpcRuntime target = CreateNpc("p12e-justice-duplicate-target");
        JusticeConfiguration configuration = CreateJusticeConfiguration("p12e-justice-duplicate");
        JusticeSystem justice = configuration.CreateOwner();
        WantedRecordRuntime warrant = justice.CreateOrIncreaseWarrant(target, city, 2f, 2);
        List<WantedRecordRuntime> wantedRows = ReadOwnerRows<WantedRecordRuntime>(justice, "wantedRecords");
        wantedRows.Add(warrant);
        Assert.That(justice.TryCaptureP12EOwnerSnapshotRows(out IReadOnlyList<P12EJusticeWantedSnapshotRow> copiedWanted,
            out IReadOnlyList<P12EJusticeSentenceSnapshotRow> copiedSentences), Is.False);
        Assert.That(copiedWanted, Is.Null);
        Assert.That(copiedSentences, Is.Null);

        CityRuntime sentenceCity = CreateCity("p12e-justice-duplicate-sentence-city");
        NpcRuntime sentenceTarget = CreateNpc("p12e-justice-duplicate-sentence-target");
        NpcRuntime sentenceGuard = CreateNpc("p12e-justice-duplicate-sentence-guard");
        JusticeSystem duplicateSentenceJustice = configuration.CreateOwner();
        Assert.That(duplicateSentenceJustice.CreateOrIncreaseWarrant(
            sentenceTarget, sentenceCity, 1f, 1), Is.Not.Null);
        Assert.That(duplicateSentenceJustice.Arrest(sentenceGuard, sentenceTarget, sentenceCity), Is.True);
        List<PrisonSentenceRuntime> sentenceRows =
            ReadOwnerRows<PrisonSentenceRuntime>(duplicateSentenceJustice, "prisonSentences");
        Assert.That(sentenceRows, Has.Count.EqualTo(1));
        sentenceRows.Add(sentenceRows[0]);
        Assert.That(duplicateSentenceJustice.TryCaptureP12EOwnerSnapshotRows(
            out copiedWanted, out copiedSentences), Is.False);
        Assert.That(copiedWanted, Is.Null);
        Assert.That(copiedSentences, Is.Null);

        P12EJusticeRecordsOwnerSnapshot missingNpc = Snapshot(
            new[] { Wanted(0, target.RuntimeId, null, city.RuntimeId, 2f, 2, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L);
        StagedRoots noNpcRoots = CreateStagedRoots(new[] { city }, Array.Empty<NpcRuntime>());
        AssertStageRejectedWithRoots(missingNpc, noNpcRoots,
            configuration, P12EJusticeSnapshotFailureCode.InvalidReference);

        NpcRuntime duplicateNpcA = CreateNpc(target.RuntimeId);
        NpcRuntime duplicateNpcB = CreateNpc(target.RuntimeId);
        StagedRoots ambiguousNpcRoots = CreateStagedRoots(new[] { city }, new[] { duplicateNpcA, duplicateNpcB });
        AssertStageRejectedWithRoots(missingNpc, ambiguousNpcRoots,
            configuration, P12EJusticeSnapshotFailureCode.InvalidReference);

        P12EJusticeRecordsOwnerSnapshot missingCity = Snapshot(
            new[] { Wanted(0, target.RuntimeId, null, "p12e-justice-missing-city", 2f, 2, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>(), 0L);
        StagedRoots noCityRoots = CreateStagedRoots(Array.Empty<CityRuntime>(), new[] { target });
        AssertStageRejectedWithRoots(missingCity, noCityRoots,
            configuration, P12EJusticeSnapshotFailureCode.InvalidReference);

        CityRuntime duplicateCityA = CreateCity(city.RuntimeId);
        CityRuntime duplicateCityB = CreateCity(city.RuntimeId);
        StagedRoots ambiguousCityRoots = CreateStagedRoots(new[] { duplicateCityA, duplicateCityB }, new[] { target });
        AssertStageRejectedWithRoots(missingCity, ambiguousCityRoots,
            configuration, P12EJusticeSnapshotFailureCode.InvalidReference);
    }

    [Test]
    public void StagePreservesEverySentenceScalarAndExactRevisionWithoutDerivingFromCardinality()
    {
        CityRuntime city = CreateCity("p12e-justice-scalars-city");
        NpcRuntime target = CreateNpc("p12e-justice-scalars-target");
        JusticeConfiguration configuration = CreateJusticeConfiguration("p12e-justice-scalars");
        P12EJusticeRecordsOwnerSnapshot snapshot = Snapshot(
            new[] { Wanted(0, target.RuntimeId, null, city.RuntimeId, 7.25f, 6, true) },
            new[] { Sentence(0, target.RuntimeId, null, city.RuntimeId, 0, 0, 3, true) },
            long.MaxValue);
        StagedRoots roots = CreateStagedRoots(new[] { city }, new[] { target });

        Assert.That(snapshot.TryStage(
            roots.Cities, roots.Npcs, roots.People,
            configuration.FreeStatus, configuration.WantedStatus,
            configuration.ArrestedStatus, configuration.HiddenStatus,
            null, new SimulationLogger(null), out JusticeSystem staged,
            out P12EJusticeSnapshotFailure failure), Is.True, failure?.Message);

        List<WantedRecordRuntime> wanted = ReadOwnerRows<WantedRecordRuntime>(staged, "wantedRecords");
        List<PrisonSentenceRuntime> sentences = ReadOwnerRows<PrisonSentenceRuntime>(staged, "prisonSentences");
        Assert.That(staged.P12CrimeJusticeRevision, Is.EqualTo(long.MaxValue));
        Assert.That(staged.P12WantedRecordCount + staged.P12PrisonSentenceCount, Is.EqualTo(2));
        Assert.That(wanted[0].IsActive, Is.False);
        Assert.That(wanted[0].Bounty, Is.EqualTo(7.25f));
        Assert.That(wanted[0].SentenceDays, Is.EqualTo(6));
        Assert.That(sentences[0].RemainingDays, Is.Zero);
        Assert.That(sentences[0].FailedEscapeAttempts, Is.EqualTo(3));
        Assert.That(sentences[0].WasArrestedToday, Is.True);
        Assert.That(sentences[0].Warrant, Is.SameAs(wanted[0]));
    }

    private static SimulationRuntime CreateRuntime(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        JusticeSystem justice,
        PersonStore people)
    {
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), cities, npcs,
            justiceSystem: justice,
            personStore: people,
            configuration: SimulationConfigurationDefaults.Create(),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        return runtime;
    }

    private static void AssertCaptureRejectsPersonBinding(
        JusticeConfiguration configuration,
        CityRuntime city,
        string prefix,
        PersonBinding binding)
    {
        NpcRuntime npc = CreateNpc(prefix + "-npc");
        PersonId personId = new PersonId(prefix + "-person");
        PersonStore people = new PersonStore();
        PersonRuntime person = null;
        switch (binding)
        {
            case PersonBinding.Dangling:
                break;
            case PersonBinding.NpcOnly:
                person = new PersonRuntime(personId, 0L);
                Assert.That(people.TryRegister(person, out _), Is.True);
                break;
            default:
                Assert.Fail("Unsupported Person binding fixture.");
                return;
        }

        JusticeSystem justice = configuration.CreateOwner();
        Assert.That(justice.CreateOrIncreaseWarrant(npc, city, 1f, 1), Is.Not.Null);
        SimulationRuntime runtime = CreateRuntime(new[] { city }, new[] { npc }, justice, people);
        CompleteDay(runtime);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        Assert.That(npc.TryAssignPersonId(personId), Is.True);
        if (binding == PersonBinding.NpcOnly)
            Assert.That(npc.TryBindPersonRuntime(person), Is.True);
        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EJusticeRecordsOwnerSnapshot rejected, out P12EJusticeSnapshotFailure failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidReference));
    }

    private static void AssertStageRejected(
        P12EJusticeRecordsOwnerSnapshot snapshot,
        P12EJusticeSnapshotFailureCode expectedCode)
    {
        Assert.That(snapshot.TryStage(null, null, null, null, null, null, null, null, null,
            out JusticeSystem staged, out P12EJusticeSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expectedCode));
    }

    private static void AssertStageRejectedWithRoots(
        P12EJusticeRecordsOwnerSnapshot snapshot,
        StagedRoots roots,
        JusticeConfiguration configuration,
        P12EJusticeSnapshotFailureCode expectedCode)
    {
        int peopleCountBefore = roots.People.Persons.Count;
        int npcStatusCountBefore = 0;
        foreach (NpcRuntime npc in roots.Npcs) npcStatusCountBefore += npc.CurrentStatus.Count;
        Assert.That(snapshot.TryStage(roots.Cities, roots.Npcs, roots.People,
            configuration.FreeStatus, configuration.WantedStatus,
            configuration.ArrestedStatus, configuration.HiddenStatus,
            null, new SimulationLogger(null), out JusticeSystem staged,
            out P12EJusticeSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expectedCode));
        Assert.That(roots.People.Persons.Count, Is.EqualTo(peopleCountBefore));
        int npcStatusCountAfter = 0;
        foreach (NpcRuntime npc in roots.Npcs) npcStatusCountAfter += npc.CurrentStatus.Count;
        Assert.That(npcStatusCountAfter, Is.EqualTo(npcStatusCountBefore));
    }

    private static void SetSnapshotBackingField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static int IndexOfWitness(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        OwnerSectionCensusSnapshot target)
    {
        for (int i = 0; i < vector.Count; i++)
            if (ReferenceEquals(vector[i], target)) return i;
        Assert.Fail("Witness was not present in its own vector.");
        return -1;
    }

    private static void CompleteDay(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());
    }

    private static CityRuntime CreateCity(string id) => new CityRuntime(
        id,
        SimulationTestFactory.CreateCityData("definition-" + id),
        new SpatialLocationRuntime("location-" + id));

    private static NpcRuntime CreateNpc(string id) => new NpcRuntime(
        id, SimulationTestFactory.CreateNpc("definition-" + id));

    private static StagedRoots CreateStagedRoots(
        IReadOnlyList<CityRuntime> sourceCities,
        IReadOnlyList<NpcRuntime> sourceNpcs)
    {
        List<CityRuntime> cities = new List<CityRuntime>();
        foreach (CityRuntime city in sourceCities)
            cities.Add(CreateCityWithDefinition(city.RuntimeId, city.CityData));

        List<NpcRuntime> npcs = new List<NpcRuntime>();
        PersonStore people = new PersonStore();
        foreach (NpcRuntime source in sourceNpcs)
        {
            NpcRuntime npc = new NpcRuntime(source.RuntimeId, source.NpcData);
            if (source.PersonId != null)
            {
                PersonId id = source.PersonId;
                PersonRuntime person = new PersonRuntime(id, 0L);
                Assert.That(people.TryRegister(person, out PersonStoreFailure registerFailure), Is.True,
                    registerFailure.ToString());
                Assert.That(people.TryBindMaterializedNpc(id, npc.RuntimeId, out PersonStoreFailure bindFailure),
                    Is.True, bindFailure.ToString());
                Assert.That(npc.TryAssignPersonId(id), Is.True);
                Assert.That(npc.TryBindPersonRuntime(person), Is.True);
            }
            npcs.Add(npc);
        }
        return new StagedRoots(cities, npcs, people);
    }

    private static CityRuntime CreateCityWithDefinition(string id, CityData definition) =>
        new CityRuntime(id, definition, new SpatialLocationRuntime("staged-location-" + id));

    private static JusticeConfiguration CreateJusticeConfiguration(string prefix) => new JusticeConfiguration(
        SimulationTestFactory.CreateStatus(prefix + "-free"),
        SimulationTestFactory.CreateStatus(prefix + "-wanted"),
        SimulationTestFactory.CreateStatus(prefix + "-arrested"),
        SimulationTestFactory.CreateStatus(prefix + "-hidden"));

    private static P12EJusticeWantedSnapshotRow Wanted(
        int ordinal, string targetId, string personId, string cityId,
        float bounty, int sentenceDays, bool resolved) =>
        new P12EJusticeWantedSnapshotRow(ordinal, targetId, personId, cityId,
            bounty, sentenceDays, resolved);

    private static P12EJusticeSentenceSnapshotRow Sentence(
        int ordinal, string targetId, string personId, string cityId,
        int warrantOrdinal, int remaining, int failedEscapes, bool arrestedToday) =>
        new P12EJusticeSentenceSnapshotRow(ordinal, targetId, personId, cityId,
            warrantOrdinal, remaining, failedEscapes, arrestedToday);

    private static P12EJusticeRecordsOwnerSnapshot Snapshot(
        IEnumerable<P12EJusticeWantedSnapshotRow> wanted,
        IEnumerable<P12EJusticeSentenceSnapshotRow> sentences,
        long revision)
    {
        List<P12EJusticeWantedSnapshotRow> wantedRows = new List<P12EJusticeWantedSnapshotRow>(wanted);
        List<P12EJusticeSentenceSnapshotRow> sentenceRows = new List<P12EJusticeSentenceSnapshotRow>(sentences);
        return new P12EJusticeRecordsOwnerSnapshot(0L,
            new P12EJusticeRecordsSnapshotSection(
                P12EJusticeRecordsSnapshotSection.CurrentSectionId,
                P12EJusticeRecordsSnapshotSection.CurrentSchemaVersion,
                checked(wantedRows.Count + sentenceRows.Count),
                revision, wantedRows, sentenceRows));
    }

    private enum PersonBinding
    {
        Dangling,
        NpcOnly
    }

    private sealed class ReportedCountList<T> : IReadOnlyList<T>
    {
        public int Count { get; }
        public T this[int index] => throw new InvalidOperationException("The overflow fixture must fail before enumeration.");

        internal ReportedCountList(int count) => Count = count;
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static List<T> ReadOwnerRows<T>(JusticeSystem owner, string fieldName)
    {
        FieldInfo field = typeof(JusticeSystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (List<T>)field.GetValue(owner);
    }

    private static OwnerSectionCensusSnapshot FindWitness(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId)
    {
        foreach (OwnerSectionCensusSnapshot section in vector)
            if (section != null && string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) return section;
        Assert.Fail("Required witness is absent: " + sectionId);
        return null;
    }

    private sealed class JusticeConfiguration
    {
        internal NpcStatusData FreeStatus { get; }
        internal NpcStatusData WantedStatus { get; }
        internal NpcStatusData ArrestedStatus { get; }
        internal NpcStatusData HiddenStatus { get; }

        internal JusticeConfiguration(
            NpcStatusData freeStatus,
            NpcStatusData wantedStatus,
            NpcStatusData arrestedStatus,
            NpcStatusData hiddenStatus)
        {
            FreeStatus = freeStatus;
            WantedStatus = wantedStatus;
            ArrestedStatus = arrestedStatus;
            HiddenStatus = hiddenStatus;
        }

        internal JusticeSystem CreateOwner() => new JusticeSystem(
            FreeStatus, WantedStatus, ArrestedStatus, HiddenStatus,
            null, new SimulationLogger(null));
    }

    private sealed class StagedRoots
    {
        internal IReadOnlyList<CityRuntime> Cities { get; }
        internal IReadOnlyList<NpcRuntime> Npcs { get; }
        internal PersonStore People { get; }

        internal StagedRoots(
            IReadOnlyList<CityRuntime> cities,
            IReadOnlyList<NpcRuntime> npcs,
            PersonStore people)
        {
            Cities = cities;
            Npcs = npcs;
            People = people;
        }
    }
}
