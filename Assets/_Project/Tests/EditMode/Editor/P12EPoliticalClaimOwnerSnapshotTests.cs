using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12EPoliticalClaimOwnerSnapshotTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject instance in simulationObjects) if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
        simulationObjects.Clear();
    }

    [Test]
    public void EmptySectionsStageExplicitlyAndPreserveExactRevision()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(new List<P12EPoliticalClaimRow>(),
            new List<P12EPoliticalClaimRecognitionRow>(), 0L);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.RecognitionCount, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
        Assert.That(staged, Is.Not.SameAs(roots.Source));
    }

    [Test]
    public void ClaimsAndRecognitionStageEveryValueAndDoNotRecomputeRevisionFromCounts()
    {
        Roots roots = new Roots();
        List<P12EPoliticalClaimRow> claims = new List<P12EPoliticalClaimRow>
        {
            new P12EPoliticalClaimRow("claim-person", "claimant", (int)PoliticalClaimType.StatusRecognition,
                (int)PoliticalClaimTargetKind.Person, "target", (int)PoliticalClaimBasis.Genealogy, "basis", 2L,
                (int)PoliticalClaimStatus.Active, null, new[] { "a", "z" }),
            new P12EPoliticalClaimRow("claim-property", "claimant", (int)PoliticalClaimType.PropertyEntitlement,
                (int)PoliticalClaimTargetKind.Property, "property", (int)PoliticalClaimBasis.PropertyOwnership, "", 1L,
                (int)PoliticalClaimStatus.Resolved, 8L, new string[0]),
            new P12EPoliticalClaimRow("claim-office", "claimant", (int)PoliticalClaimType.OfficeEntitlement,
                (int)PoliticalClaimTargetKind.Office, "office", (int)PoliticalClaimBasis.OfficeIncumbency, "", 0L,
                (int)PoliticalClaimStatus.Active, null, new string[0]),
            new P12EPoliticalClaimRow("claim-institution", "claimant", (int)PoliticalClaimType.InstitutionalAuthority,
                (int)PoliticalClaimTargetKind.Institution, "institution", (int)PoliticalClaimBasis.Other, "", 0L,
                (int)PoliticalClaimStatus.Active, null, new string[0])
        };
        List<P12EPoliticalClaimRecognitionRow> recognitions = new List<P12EPoliticalClaimRecognitionRow>
        {
            Recognition("claim-person", "institution", new[]
            {
                new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Unrecognized, 3L, "old"),
                new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Recognized, 4L, "current")
            }),
            Recognition("claim-person", "institution-2", new[]
            {
                new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Contested, 5L, "second")
            })
        };
        const long exactRevision = 19L;
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(claims, recognitions, exactRevision);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(staged.Count, Is.EqualTo(4));
        Assert.That(staged.RecognitionCount, Is.EqualTo(2));
        Assert.That(staged.Revision, Is.EqualTo(exactRevision));
        Assert.That(staged.TryGet(new PoliticalClaimId("claim-person"), out PoliticalClaimRecord personClaim), Is.True);
        Assert.That(personClaim.Target.Kind, Is.EqualTo(PoliticalClaimTargetKind.Person));
        Assert.That(personClaim.EvidenceReferences, Is.EqualTo(new[] { "a", "z" }));
        Assert.That(staged.TryGet(new PoliticalClaimId("claim-property"), out PoliticalClaimRecord terminal), Is.True);
        Assert.That(terminal.ResolutionAbsoluteDay, Is.EqualTo(8L));
        Assert.That(staged.TryGetRecognition(new PoliticalClaimId("claim-person"), new InstitutionId("institution"),
            out PoliticalClaimRecognitionRecord current), Is.True);
        Assert.That(current.History, Has.Count.EqualTo(2));
        Assert.That(current.History[0].Reason, Is.EqualTo("old"));
        Assert.That(current.History[1].State, Is.EqualTo(PoliticalClaimRecognitionState.Recognized));
        Assert.That(current.Reason, Is.EqualTo("current"));
    }

    [Test]
    public void StageRejectsDuplicateRecognitionAndMalformedTerminalHistory()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimRow claim = new P12EPoliticalClaimRow("claim-person", "claimant",
            (int)PoliticalClaimType.StatusRecognition, (int)PoliticalClaimTargetKind.Person, "target",
            (int)PoliticalClaimBasis.Other, "", 0L, (int)PoliticalClaimStatus.Active, null, new string[0]);
        string recognitionId = PoliticalClaimRecognitionRecord.BuildRecognitionId(
            new PoliticalClaimId("claim-person"), new InstitutionId("institution"));
        P12EPoliticalClaimRecognitionRow terminalMismatch = new P12EPoliticalClaimRecognitionRow(
            "claim-person", "institution", recognitionId, (int)PoliticalClaimRecognitionState.Recognized,
            5L, "current", new[]
        {
            new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Rejected, 4L, "stale")
        });
        P12EPoliticalClaimRecognitionRow valid = Recognition("claim-person", "institution", new[]
        {
            new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Recognized, 5L, "current")
        });
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(new List<P12EPoliticalClaimRow>{ claim },
            new List<P12EPoliticalClaimRecognitionRow>{ valid, valid.Copy() }, 2L);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.DuplicateIdentity));
        snapshot = Snapshot(new List<P12EPoliticalClaimRow>{ claim }, new List<P12EPoliticalClaimRecognitionRow>{ terminalMismatch }, 1L);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
    }

    [Test]
    public void StageAcceptsMaxRevisionAndStoreRejectsNextMutationAtomically()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimOwnerSnapshot snapshot = Snapshot(new List<P12EPoliticalClaimRow>(),
            new List<P12EPoliticalClaimRecognitionRow>(), long.MaxValue);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
        PoliticalClaimRecord next = new PoliticalClaimRecord(new PoliticalClaimId("overflow"), new PersonId("claimant"),
            PoliticalClaimType.StatusRecognition, PoliticalClaimTarget.ForPerson(new PersonId("target")),
            PoliticalClaimBasis.Other, "", 0L, null);
        Assert.That(staged.TryRegister(next, out PoliticalClaimFailure writeFailure), Is.False);
        Assert.That(writeFailure.Code, Is.EqualTo(PoliticalClaimFailureCode.RevisionOverflow));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void StageRejectsMissingSectionsUnsupportedSchemaAndDisagreeingRevisions()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow> claims =
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                PoliticalClaimStoreCensusProvider.ClaimsSectionId, 1, 0, 1L,
                new List<P12EPoliticalClaimRow>(), row => row?.Copy());
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow> recognitions =
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1, 0, 2L,
                new List<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy());
        P12EPoliticalClaimOwnerSnapshot snapshot = new P12EPoliticalClaimOwnerSnapshot(claims, recognitions);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.InvalidRevision));

        recognitions = new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
            PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 2, 0, 1L,
            new List<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy());
        snapshot = new P12EPoliticalClaimOwnerSnapshot(claims, recognitions);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.UnsupportedSchema));

        claims = new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
            "p12e.political-claim.unknown", 1, 0, 1L, new List<P12EPoliticalClaimRow>(), row => row?.Copy());
        snapshot = new P12EPoliticalClaimOwnerSnapshot(claims, recognitions);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.UnsupportedSchema));

        snapshot = new P12EPoliticalClaimOwnerSnapshot(null, recognitions);
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, 20L,
            out staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.UnsupportedSchema));
    }

    [Test]
    public void CaptureUsesCurrentCompletedTokenAndBothWitnessesBindTheSameExactOwner()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());

        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(runtime, out P12EPoliticalClaimOwnerSnapshot snapshot,
            out P12EPoliticalClaimSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(snapshot.Claims.SectionId, Is.EqualTo(PoliticalClaimStoreCensusProvider.ClaimsSectionId));
        Assert.That(snapshot.Recognitions.SectionId, Is.EqualTo(PoliticalClaimStoreCensusProvider.RecognitionsSectionId));
        Assert.That(snapshot.Claims.SchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.Recognitions.SchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.Claims.RecordCount, Is.Zero);
        Assert.That(snapshot.Recognitions.RecordCount, Is.Zero);
        Assert.That(snapshot.Claims.Revision, Is.EqualTo(snapshot.Recognitions.Revision));

        OwnerSectionCensusSnapshot claimWitness = FindWitness(token.OwnerSections,
            PoliticalClaimStoreCensusProvider.ClaimsSectionId);
        OwnerSectionCensusSnapshot recognitionWitness = FindWitness(token.OwnerSections,
            PoliticalClaimStoreCensusProvider.RecognitionsSectionId);
        Assert.That(claimWitness.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(recognitionWitness.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(claimWitness.OwnerInstanceIdentity, Is.TypeOf<PoliticalClaimStore>());
        Assert.That(recognitionWitness.OwnerInstanceIdentity, Is.SameAs(claimWitness.OwnerInstanceIdentity));
        Assert.That(claimWitness.Revision, Is.EqualTo(recognitionWitness.Revision));
        Assert.That(claimWitness.Cardinality, Is.EqualTo(snapshot.Claims.RecordCount));
        Assert.That(recognitionWitness.Cardinality, Is.EqualTo(snapshot.Recognitions.RecordCount));

        List<OwnerSectionCensusSnapshot> altered = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        int claimIndex = altered.IndexOf(claimWitness);
        altered[claimIndex] = new OwnerSectionCensusSnapshot(claimWitness.SectionId, claimWitness.SchemaVersion,
            claimWitness.Role, claimWitness.OwnerInstanceIdentity, claimWitness.Cardinality + 1, claimWitness.Revision);
        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(runtime, token, altered,
            out P12EPoliticalClaimOwnerSnapshot rejected, out failure), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);

        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(new PersonId("capture-stale"), 0L), out _), Is.True);
        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out rejected, out failure), Is.False);
        Assert.That(rejected, Is.Null);
    }

    [Test]
    public void CaptureExportIsFieldCompleteSortedAndIndependentOfWriterInsertionOrder()
    {
        SimulationRuntime firstRuntime = CreateDailyRuntime();
        SimulationRuntime secondRuntime = CreateDailyRuntime();
        PopulateCaptureRuntime(firstRuntime, reverseInsertion: false);
        PopulateCaptureRuntime(secondRuntime, reverseInsertion: true);
        Assert.That(firstRuntime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvance), Is.True, firstAdvance.ToString());
        Assert.That(secondRuntime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure secondAdvance), Is.True, secondAdvance.ToString());
        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(firstRuntime, out P12EPoliticalClaimOwnerSnapshot first,
            out P12EPoliticalClaimSnapshotFailure firstFailure), Is.True, firstFailure?.Message);
        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(secondRuntime, out P12EPoliticalClaimOwnerSnapshot second,
            out P12EPoliticalClaimSnapshotFailure secondFailure), Is.True, secondFailure?.Message);
        Assert.That(Signature(first), Is.EqualTo(Signature(second)));
        Assert.That(first.Claims.Records[0].ClaimId, Is.EqualTo("claim.institution"));
        Assert.That(first.Claims.Records[1].ClaimId, Is.EqualTo("claim.office"));
        Assert.That(first.Claims.Records[2].ClaimId, Is.EqualTo("claim.person"));
        Assert.That(first.Claims.Records[3].ClaimId, Is.EqualTo("claim.property"));
        Assert.That(first.Claims.Records[2].EvidenceReferences, Is.EqualTo(new[] { "alpha", "zeta" }));
        Assert.That(first.Claims.Records[3].Status, Is.EqualTo((int)PoliticalClaimStatus.Resolved));
        Assert.That(first.Claims.Records[3].ResolutionAbsoluteDay, Is.EqualTo(0L));
        Assert.That(first.Recognitions.RecordCount, Is.EqualTo(2));
        Assert.That(first.Recognitions.Records[0].InstitutionId, Is.EqualTo("institution-a"));
        Assert.That(first.Recognitions.Records[1].InstitutionId, Is.EqualTo("institution-z"));
        Assert.That(first.Recognitions.Records[1].History, Has.Count.EqualTo(2));
        Assert.That(first.Recognitions.Records[1].History[0].State,
            Is.EqualTo((int)PoliticalClaimRecognitionState.Recognized));
        Assert.That(first.Recognitions.Records[1].History[1].State,
            Is.EqualTo((int)PoliticalClaimRecognitionState.Contested));

        Assert.That(first.TryStage(firstRuntime.PersonStore, firstRuntime.PropertyOwnershipStore,
            firstRuntime.InstitutionStoreForWorldBoundary, firstRuntime.OfficeStoreForWorldBoundary,
            1L, out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure stageFailure),
            Is.True, stageFailure?.Message);
        Assert.That(staged.Records, Is.EqualTo(firstRuntime.PoliticalClaimRecords));
        Assert.That(staged.RecognitionRecords, Is.EqualTo(firstRuntime.PoliticalClaimRecognitionRecords));

        Assert.That(firstRuntime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out _), Is.True);
        PoliticalClaimStore source = (PoliticalClaimStore)FindWitness(token.OwnerSections,
            PoliticalClaimStoreCensusProvider.ClaimsSectionId).OwnerInstanceIdentity;
        long sourceRevision = source.Revision;
        PoliticalClaimRecord[] sourceRows = new List<PoliticalClaimRecord>(source.Records).ToArray();
        PoliticalClaimRecognitionRecord[] sourceRecognitionRows = new List<PoliticalClaimRecognitionRecord>(source.RecognitionRecords).ToArray();
        PoliticalClaimStore publicationTarget = new PoliticalClaimStore();
        PoliticalClaimRecord publicationRecord = new PoliticalClaimRecord(new PoliticalClaimId("publication-target"),
            new PersonId("capture-claimant"), PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(new PersonId("capture-target")), PoliticalClaimBasis.Other, "", 0L, null);
        Assert.That(publicationTarget.TryRegister(publicationRecord, out _), Is.True);
        long publicationRevision = publicationTarget.Revision;
        P12EPoliticalClaimRecognitionRow validRecognition = first.Recognitions.Records[0];
        P12EPoliticalClaimRecognitionRow invalidRecognition = new P12EPoliticalClaimRecognitionRow(
            validRecognition.ClaimId, validRecognition.InstitutionId, validRecognition.RecognitionId,
            validRecognition.State, validRecognition.RecognitionAbsoluteDay, "injected mismatch",
            validRecognition.History);
        P12EPoliticalClaimOwnerSnapshot injectedFailure = new P12EPoliticalClaimOwnerSnapshot(first.Claims,
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1, 1, first.Recognitions.Revision,
                new[] { invalidRecognition }, row => row?.Copy()));
        Assert.That(injectedFailure.TryStage(firstRuntime.PersonStore, firstRuntime.PropertyOwnershipStore,
            firstRuntime.InstitutionStoreForWorldBoundary, firstRuntime.OfficeStoreForWorldBoundary, 1L,
            out PoliticalClaimStore failedCandidate, out P12EPoliticalClaimSnapshotFailure injectedFailureResult), Is.False);
        Assert.That(failedCandidate, Is.Null);
        Assert.That(source.Revision, Is.EqualTo(sourceRevision));
        Assert.That(source.Records, Is.EqualTo(sourceRows));
        Assert.That(source.RecognitionRecords, Is.EqualTo(sourceRecognitionRows));
        Assert.That(firstRuntime.PoliticalClaimRecords, Is.EqualTo(sourceRows));
        Assert.That(firstRuntime.PoliticalClaimRecognitionRecords, Is.EqualTo(sourceRecognitionRows));
        Assert.That(publicationTarget.Revision, Is.EqualTo(publicationRevision));
        Assert.That(publicationTarget.Records, Is.EqualTo(new[] { publicationRecord }));
        Assert.That(injectedFailureResult.Code, Is.EqualTo(P12EPoliticalClaimSnapshotFailureCode.InvalidRecognition));
    }

    [Test]
    public void StagingRejectsMalformedClaimFieldsReferencesAndCountsWithoutChangingSourceOrPublicationTarget()
    {
        Roots roots = new Roots();
        PoliticalClaimRecord sourceRecord = new PoliticalClaimRecord(new PoliticalClaimId("source"),
            new PersonId("claimant"), PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(new PersonId("target")), PoliticalClaimBasis.Other, "", 0L, null);
        Assert.That(roots.Source.TryRegister(sourceRecord, out _), Is.True);
        PoliticalClaimStore publicationTarget = new PoliticalClaimStore();
        PoliticalClaimRecord publicationRecord = new PoliticalClaimRecord(new PoliticalClaimId("published"),
            new PersonId("claimant"), PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(new PersonId("target")), PoliticalClaimBasis.Other, "", 0L, null);
        Assert.That(publicationTarget.TryRegister(publicationRecord, out _), Is.True);
        long sourceRevision = roots.Source.Revision;
        long publicationRevision = publicationTarget.Revision;

        P12EPoliticalClaimRow valid = BasicClaim();
        List<P12EPoliticalClaimRow> malformed = new List<P12EPoliticalClaimRow>
        {
            new P12EPoliticalClaimRow("claim", "missing-claimant", 4, 3, "target", 5, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "missing-person", 5, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 3, 2, "missing-institution", 5, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 0, 0, "missing-office", 5, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 2, 1, "missing-property", 5, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 999, 3, "target", 5, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 0, 3, "target", 5, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "target", 999, "", 0, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "target", 5, "", 21, 0, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "target", 5, "", 0, 999, null, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "target", 5, "", 0, 0, 1L, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "target", 5, "", 0, 1, -1L, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "target", 5, "", 0, 1, 21L, new string[0]),
            new P12EPoliticalClaimRow("claim", "claimant", 4, 3, "target", 5, "", 0, 0, null, new[] { "zeta", "alpha" })
        };
        foreach (P12EPoliticalClaimRow row in malformed)
            AssertStageFails(roots, Snapshot(new List<P12EPoliticalClaimRow> { row },
                new List<P12EPoliticalClaimRecognitionRow>(), 4L), 20L);

        AssertStageFails(roots, Snapshot(new List<P12EPoliticalClaimRow> { valid, valid.Copy() },
            new List<P12EPoliticalClaimRecognitionRow>(), 4L), 20L);
        AssertStageFails(roots, new P12EPoliticalClaimOwnerSnapshot(
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                PoliticalClaimStoreCensusProvider.ClaimsSectionId, 1, 1, 4L,
                new P12EPoliticalClaimRow[] { null }, row => row?.Copy()),
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1, 0, 4L,
                new List<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy())), 20L);
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow> mismatch =
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                PoliticalClaimStoreCensusProvider.ClaimsSectionId, 1, 2, 4L,
                new[] { valid }, row => row?.Copy());
        AssertStageFails(roots, new P12EPoliticalClaimOwnerSnapshot(mismatch,
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1, 0, 4L,
                new List<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy())), 20L);
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow> recognitionCountMismatch =
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1, 1, 4L,
                new List<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy());
        AssertStageFails(roots, new P12EPoliticalClaimOwnerSnapshot(
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                PoliticalClaimStoreCensusProvider.ClaimsSectionId, 1, 0, 4L,
                new List<P12EPoliticalClaimRow>(), row => row?.Copy()), recognitionCountMismatch), 20L);
        P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow> nullRows =
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1, 1, 4L,
                null, row => row?.Copy());
        AssertStageFails(roots, new P12EPoliticalClaimOwnerSnapshot(
            new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                PoliticalClaimStoreCensusProvider.ClaimsSectionId, 1, 0, 4L,
                new List<P12EPoliticalClaimRow>(), row => row?.Copy()), nullRows), 20L);

        Assert.That(roots.Source.Revision, Is.EqualTo(sourceRevision));
        Assert.That(roots.Source.Records, Is.EqualTo(new[] { sourceRecord }));
        Assert.That(publicationTarget.Revision, Is.EqualTo(publicationRevision));
        Assert.That(publicationTarget.Records, Is.EqualTo(new[] { publicationRecord }));
    }

    [Test]
    public void StagingRejectsMalformedRecognitionReferencesDuplicatesAndHistory()
    {
        Roots roots = new Roots();
        P12EPoliticalClaimRow claim = BasicClaim();
        P12EPoliticalClaimRecognitionRow valid = Recognition("claim", "institution", new[]
        {
            new P12EPoliticalClaimRecognitionHistoryRow((int)PoliticalClaimRecognitionState.Recognized, 4L, "ok")
        });
        List<P12EPoliticalClaimRecognitionRow> malformed = new List<P12EPoliticalClaimRecognitionRow>
        {
            new P12EPoliticalClaimRecognitionRow("missing-claim", "institution",
                PoliticalClaimRecognitionRecord.BuildRecognitionId(new PoliticalClaimId("missing-claim"), new InstitutionId("institution")),
                valid.State, valid.RecognitionAbsoluteDay, valid.Reason, valid.History),
            new P12EPoliticalClaimRecognitionRow("claim", "missing-institution",
                PoliticalClaimRecognitionRecord.BuildRecognitionId(new PoliticalClaimId("claim"), new InstitutionId("missing-institution")),
                valid.State, valid.RecognitionAbsoluteDay, valid.Reason, valid.History),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", valid.RecognitionId,
                valid.State, valid.RecognitionAbsoluteDay, valid.Reason, null),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", valid.RecognitionId,
                valid.State, valid.RecognitionAbsoluteDay, valid.Reason,
                new P12EPoliticalClaimRecognitionHistoryRow[] { null }),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", valid.RecognitionId,
                999, 4L, "ok", valid.History),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", "wrong-key",
                valid.State, valid.RecognitionAbsoluteDay, valid.Reason, valid.History),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", valid.RecognitionId,
                valid.State, 21L, "ok", new[] { new P12EPoliticalClaimRecognitionHistoryRow(valid.State, 21L, "ok") }),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", valid.RecognitionId,
                valid.State, 4L, "ok", new[]
                {
                    new P12EPoliticalClaimRecognitionHistoryRow(valid.State, 5L, "first"),
                    new P12EPoliticalClaimRecognitionHistoryRow(valid.State, 4L, "ok")
                }),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", valid.RecognitionId,
                valid.State, 4L, "ok", new[] { new P12EPoliticalClaimRecognitionHistoryRow(valid.State, 3L, "ok") }),
            new P12EPoliticalClaimRecognitionRow("claim", "institution", valid.RecognitionId,
                valid.State, 4L, "different", valid.History)
        };
        foreach (P12EPoliticalClaimRecognitionRow row in malformed)
            AssertStageFails(roots, Snapshot(new List<P12EPoliticalClaimRow> { claim },
                new List<P12EPoliticalClaimRecognitionRow> { row }, 4L), 20L);
        AssertStageFails(roots, Snapshot(new List<P12EPoliticalClaimRow> { claim },
            new List<P12EPoliticalClaimRecognitionRow> { valid, valid.Copy() }, 4L), 20L);
    }

    private SimulationRuntime CreateDailyRuntime()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject instance = new GameObject("p12e-political-claim-snapshot-test");
        simulationObjects.Add(instance);
        TesteSimulacao simulation = instance.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();
        Assert.That(simulation.Runtime, Is.Not.Null);
        return simulation.Runtime;
    }

    private static void PopulateCaptureRuntime(SimulationRuntime runtime, bool reverseInsertion)
    {
        PersonId claimant = new PersonId("capture-claimant");
        PersonId target = new PersonId("capture-target");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(claimant, 0L), out PersonStoreFailure claimantFailure), Is.True, claimantFailure.ToString());
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(target, 0L), out PersonStoreFailure targetFailure), Is.True, targetFailure.ToString());
        InstitutionId institutionA = new InstitutionId("institution-a");
        InstitutionId institutionZ = new InstitutionId("institution-z");
        Assert.That(runtime.TryRegisterInstitution(new InstitutionRecord(institutionA), out InstitutionFoundationFailure institutionFailure), Is.True, institutionFailure.ToString());
        Assert.That(runtime.TryRegisterInstitution(new InstitutionRecord(institutionZ), out institutionFailure), Is.True, institutionFailure.ToString());
        OfficeId office = new OfficeId("capture-office");
        Assert.That(runtime.TryRegisterOffice(new OfficeRecord(office, institutionA), out InstitutionFoundationFailure officeFailure), Is.True, officeFailure.ToString());
        PropertyId property = new PropertyId("capture-property");
        Assert.That(runtime.TryRegisterPropertyOwnership(new PropertyOwnershipRecord(property, claimant), out PropertyFoundationFailure propertyFailure), Is.True, propertyFailure.ToString());

        long day = runtime.SimulationTime.AbsoluteDay;
        PoliticalClaimRecord[] claims =
        {
            new PoliticalClaimRecord(new PoliticalClaimId("claim.person"), claimant,
                PoliticalClaimType.StatusRecognition, PoliticalClaimTarget.ForPerson(target), PoliticalClaimBasis.Genealogy,
                "person basis", day, new[] { "zeta", "alpha", "alpha", " " }),
            new PoliticalClaimRecord(new PoliticalClaimId("claim.institution"), claimant,
                PoliticalClaimType.InstitutionalAuthority, PoliticalClaimTarget.ForInstitution(institutionA), PoliticalClaimBasis.Other,
                "institution basis", day, null),
            new PoliticalClaimRecord(new PoliticalClaimId("claim.office"), claimant,
                PoliticalClaimType.OfficeEntitlement, PoliticalClaimTarget.ForOffice(office), PoliticalClaimBasis.OfficeIncumbency,
                "office basis", day, null),
            new PoliticalClaimRecord(new PoliticalClaimId("claim.property"), claimant,
                PoliticalClaimType.PropertyEntitlement, PoliticalClaimTarget.ForProperty(property), PoliticalClaimBasis.PropertyOwnership,
                "property basis", day, new[] { "deed" })
        };
        if (reverseInsertion) Array.Reverse(claims);
        foreach (PoliticalClaimRecord claim in claims)
            Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure), Is.True, claimFailure.ToString());

        if (reverseInsertion)
        {
            AddRecognition(runtime, new PoliticalClaimId("claim.person"), institutionA, PoliticalClaimRecognitionState.Recognized, "other institution");
            AddRecognition(runtime, new PoliticalClaimId("claim.person"), institutionZ, PoliticalClaimRecognitionState.Recognized, "recognized");
            AddRecognition(runtime, new PoliticalClaimId("claim.person"), institutionZ, PoliticalClaimRecognitionState.Contested, "contested");
        }
        else
        {
            AddRecognition(runtime, new PoliticalClaimId("claim.person"), institutionZ, PoliticalClaimRecognitionState.Recognized, "recognized");
            AddRecognition(runtime, new PoliticalClaimId("claim.person"), institutionZ, PoliticalClaimRecognitionState.Contested, "contested");
            AddRecognition(runtime, new PoliticalClaimId("claim.person"), institutionA, PoliticalClaimRecognitionState.Recognized, "other institution");
        }

        Assert.That(runtime.TryProposePoliticalClaimResolution(new PoliticalClaimId("claim.property"),
            PoliticalClaimStatus.Resolved, out PoliticalClaimResolutionTransition resolution,
            out PoliticalClaimFailure resolutionFailure), Is.True, resolutionFailure.ToString());
        Assert.That(runtime.TryApplyPoliticalClaimResolution(resolution, out resolutionFailure), Is.True, resolutionFailure.ToString());
    }

    private static void AddRecognition(SimulationRuntime runtime, PoliticalClaimId claim,
        InstitutionId institution, PoliticalClaimRecognitionState state, string reason)
    {
        Assert.That(runtime.TryProposePoliticalClaimRecognition(claim, institution, state, reason,
            out PoliticalClaimRecognitionTransition transition, out PoliticalClaimFailure failure), Is.True, failure.ToString());
        Assert.That(runtime.TryApplyPoliticalClaimRecognition(transition, out failure), Is.True, failure.ToString());
    }

    private static OwnerSectionCensusSnapshot FindWitness(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector, string id)
    {
        foreach (OwnerSectionCensusSnapshot item in vector)
            if (string.Equals(item.SectionId, id, StringComparison.Ordinal)) return item;
        Assert.Fail("Required owner witness is absent: " + id);
        return null;
    }

    private static P12EPoliticalClaimRow BasicClaim() => new P12EPoliticalClaimRow("claim", "claimant",
        (int)PoliticalClaimType.StatusRecognition, (int)PoliticalClaimTargetKind.Person, "target",
        (int)PoliticalClaimBasis.Other, "", 0L, (int)PoliticalClaimStatus.Active, null, new string[0]);

    private static void AssertStageFails(Roots roots, P12EPoliticalClaimOwnerSnapshot snapshot, long capturedDay)
    {
        Assert.That(snapshot.TryStage(roots.People, roots.Properties, roots.Institutions, roots.Offices, capturedDay,
            out PoliticalClaimStore staged, out P12EPoliticalClaimSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.Not.EqualTo(P12EPoliticalClaimSnapshotFailureCode.None));
    }

    private static string Signature(P12EPoliticalClaimOwnerSnapshot snapshot)
    {
        StringBuilder value = new StringBuilder();
        value.Append(snapshot.Claims.Revision).Append('|');
        foreach (P12EPoliticalClaimRow row in snapshot.Claims.Records)
        {
            Append(value, row.ClaimId); Append(value, row.ClaimantPersonId); Append(value, row.ClaimType.ToString());
            Append(value, row.TargetKind.ToString()); Append(value, row.TargetId); Append(value, row.Basis.ToString());
            Append(value, row.BasisDescription); Append(value, row.CreatedAbsoluteDay.ToString());
            Append(value, row.Status.ToString()); Append(value, row.ResolutionAbsoluteDay?.ToString());
            foreach (string evidence in row.EvidenceReferences) Append(value, evidence);
            value.Append(';');
        }
        foreach (P12EPoliticalClaimRecognitionRow row in snapshot.Recognitions.Records)
        {
            Append(value, row.ClaimId); Append(value, row.InstitutionId); Append(value, row.RecognitionId);
            Append(value, row.State.ToString()); Append(value, row.RecognitionAbsoluteDay.ToString()); Append(value, row.Reason);
            foreach (P12EPoliticalClaimRecognitionHistoryRow item in row.History)
            { Append(value, item.State.ToString()); Append(value, item.Day.ToString()); Append(value, item.Reason); }
            value.Append(';');
        }
        return value.ToString();
    }

    private static void Append(StringBuilder builder, string field)
    {
        if (field == null) { builder.Append("-1:"); return; }
        builder.Append(field.Length).Append(':').Append(field);
    }

    private static P12EPoliticalClaimRecognitionRow Recognition(string claim, string institution,
        IEnumerable<P12EPoliticalClaimRecognitionHistoryRow> history)
    {
        string id = PoliticalClaimRecognitionRecord.BuildRecognitionId(new PoliticalClaimId(claim), new InstitutionId(institution));
        P12EPoliticalClaimRecognitionHistoryRow last = null;
        foreach (var item in history) last = item;
        return new P12EPoliticalClaimRecognitionRow(claim, institution, id, last.State, last.Day, last.Reason, history);
    }

    private static P12EPoliticalClaimOwnerSnapshot Snapshot(List<P12EPoliticalClaimRow> claims,
        List<P12EPoliticalClaimRecognitionRow> recognitions, long revision) => new P12EPoliticalClaimOwnerSnapshot(
        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(PoliticalClaimStoreCensusProvider.ClaimsSectionId,
            1, claims.Count, revision, claims, row => row?.Copy()),
        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(PoliticalClaimStoreCensusProvider.RecognitionsSectionId,
            1, recognitions.Count, revision, recognitions, row => row?.Copy()));

    private sealed class Roots
    {
        internal readonly PersonStore People = new PersonStore();
        internal readonly InstitutionStore Institutions = new InstitutionStore();
        internal readonly OfficeStore Offices;
        internal readonly PropertyOwnershipStore Properties;
        internal readonly PoliticalClaimStore Source = new PoliticalClaimStore();
        internal Roots()
        {
            RegisterPerson("claimant"); RegisterPerson("target");
            Assert.That(Institutions.TryRegister(new InstitutionRecord(new InstitutionId("institution"), "institution"), out _), Is.True);
            Assert.That(Institutions.TryRegister(new InstitutionRecord(new InstitutionId("institution-2"), "institution-2"), out _), Is.True);
            Offices = new OfficeStore(Institutions);
            Assert.That(Offices.TryRegister(new OfficeRecord(new OfficeId("office"), new InstitutionId("institution"), "office"), out _), Is.True);
            Properties = new PropertyOwnershipStore(People);
            Assert.That(Properties.TryRegister(new PropertyOwnershipRecord(new PropertyId("property"), new PersonId("claimant")), out _), Is.True);
        }
        private void RegisterPerson(string id)
        {
            Assert.That(People.TryRegister(new PersonRuntime(new PersonId(id), 0L), out PersonStoreFailure failure), Is.True, failure.ToString());
        }
    }
}
