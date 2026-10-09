using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12EPoliticalSupportOwnerSnapshotTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject instance in simulationObjects)
        {
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
        }
        simulationObjects.Clear();
    }

    [Test]
    public void CaptureUsesExactCompletedTokenAndRoundTripsEveryTypedRelationAndHistoryRow()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        PersonId sourcePerson = new PersonId("p12e-support-source-person");
        PersonId candidatePerson = new PersonId("p12e-support-candidate-person");
        FactionId sourceFaction = new FactionId("p12e-support-source-faction");
        PoliticalClaimId targetClaim = new PoliticalClaimId("p12e-support-target-claim");

        RegisterPerson(runtime, sourcePerson);
        RegisterPerson(runtime, candidatePerson);
        Assert.That(runtime.TryRegisterFaction(
            new FactionRecord(sourceFaction, "Support fixture", runtime.CurrentDay),
            out FactionFoundationFailure factionFailure), Is.True, factionFailure.ToString());
        PoliticalClaimRecord claim = CreateClaim(targetClaim, sourcePerson, candidatePerson, runtime.CurrentDay);
        Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure),
            Is.True, claimFailure.ToString());

        PoliticalSupportRelationRecord endedHistory = new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.person-claim.old"),
            PoliticalSupportSource.ForPerson(sourcePerson),
            PoliticalSupportTarget.ForPoliticalClaim(targetClaim),
            PoliticalSupportDisposition.Support,
            runtime.CurrentDay);
        RegisterSupport(runtime, endedHistory);
        Assert.That(runtime.TryProposePoliticalSupportEnd(endedHistory.RelationId,
            out PoliticalSupportEndTransition end, out PoliticalSupportFailure endProposalFailure),
            Is.True, endProposalFailure.ToString());
        Assert.That(runtime.TryApplyPoliticalSupportEnd(end, out PoliticalSupportFailure endFailure),
            Is.True, endFailure.ToString());

        PoliticalSupportRelationRecord secondEndedHistory = new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.person-claim.middle"),
            PoliticalSupportSource.ForPerson(sourcePerson),
            PoliticalSupportTarget.ForPoliticalClaim(targetClaim),
            PoliticalSupportDisposition.Support,
            runtime.CurrentDay);
        RegisterSupport(runtime, secondEndedHistory);
        Assert.That(runtime.TryProposePoliticalSupportEnd(secondEndedHistory.RelationId,
            out end, out endProposalFailure), Is.True, endProposalFailure.ToString());
        Assert.That(runtime.TryApplyPoliticalSupportEnd(end, out endFailure), Is.True, endFailure.ToString());

        RegisterSupport(runtime, new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.person-claim.current"),
            PoliticalSupportSource.ForPerson(sourcePerson),
            PoliticalSupportTarget.ForPoliticalClaim(targetClaim),
            PoliticalSupportDisposition.Oppose,
            runtime.CurrentDay));
        RegisterSupport(runtime, new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.person-candidate"),
            PoliticalSupportSource.ForPerson(sourcePerson),
            PoliticalSupportTarget.ForSuccessionCandidate(candidatePerson),
            PoliticalSupportDisposition.Support,
            runtime.CurrentDay));
        RegisterSupport(runtime, new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.faction-claim"),
            PoliticalSupportSource.ForFaction(sourceFaction),
            PoliticalSupportTarget.ForPoliticalClaim(targetClaim),
            PoliticalSupportDisposition.Oppose,
            runtime.CurrentDay));
        RegisterSupport(runtime, new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.faction-candidate"),
            PoliticalSupportSource.ForFaction(sourceFaction),
            PoliticalSupportTarget.ForSuccessionCandidate(candidatePerson),
            PoliticalSupportDisposition.Support,
            runtime.CurrentDay));

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());

        Assert.That(P12EPoliticalSupportOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EPoliticalSupportOwnerSnapshot snapshot, out P12EPoliticalSupportSnapshotFailure captureFailure),
            Is.True, captureFailure?.Message);
        Assert.That(P12EPoliticalSupportOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EPoliticalSupportOwnerSnapshot repeated, out captureFailure), Is.True, captureFailure?.Message);
        Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(token.AbsoluteDay));
        Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(1L));
        Assert.That(snapshot.Relations.SectionId, Is.EqualTo(PoliticalSupportStoreCensusProvider.RelationsSectionId));
        Assert.That(snapshot.Relations.SchemaVersion, Is.EqualTo(PoliticalSupportStoreCensusProvider.SchemaVersion));
        Assert.That(snapshot.Relations.RecordCount, Is.EqualTo(6));
        Assert.That(snapshot.Relations.Revision, Is.EqualTo(8L));
        Assert.That(Signature(snapshot), Is.EqualTo(Signature(repeated)));

        OwnerSectionCensusSnapshot witness = FindWitness(token.OwnerSections,
            PoliticalSupportStoreCensusProvider.RelationsSectionId);
        Assert.That(witness.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(witness.OwnerInstanceIdentity, Is.TypeOf<PoliticalSupportStore>());
        Assert.That(witness.Cardinality, Is.EqualTo(snapshot.Relations.RecordCount));
        Assert.That(witness.Revision, Is.EqualTo(snapshot.Relations.Revision));
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);

        Roots stagedRoots = CreateStagedRoots(sourcePerson, candidatePerson, sourceFaction, claim);
        Assert.That(snapshot.TryStage(stagedRoots.People, stagedRoots.Factions, stagedRoots.Claims,
            out PoliticalSupportStore staged, out P12EPoliticalSupportSnapshotFailure stageFailure),
            Is.True, stageFailure?.Message);
        Assert.That(staged.Count, Is.EqualTo(6));
        Assert.That(staged.Revision, Is.EqualTo(8L));
        Assert.That(Signature(staged.Records), Is.EqualTo(Signature(snapshot.Relations.Records)));
        Assert.That(staged.GetForPair(PoliticalSupportSource.ForPerson(sourcePerson),
            PoliticalSupportTarget.ForPoliticalClaim(targetClaim)), Has.Count.EqualTo(3));
        Assert.That(staged.TryGet(endedHistory.RelationId, out PoliticalSupportRelationRecord restoredEnded), Is.True);
        Assert.That(restoredEnded.EndedAbsoluteDay, Is.EqualTo(0L));
        Assert.That(restoredEnded.IsActive, Is.False);
        Assert.That(staged.TryGet(secondEndedHistory.RelationId, out PoliticalSupportRelationRecord restoredSecondEnded), Is.True);
        Assert.That(restoredSecondEnded.IsActive, Is.False);
        Assert.That(staged.TryGet(new PoliticalSupportRelationId("support.person-claim.current"),
            out PoliticalSupportRelationRecord restoredCurrent), Is.True);
        Assert.That(restoredCurrent.IsActive, Is.True);
        Assert.That(staged.TryGetActive(PoliticalSupportSource.ForPerson(sourcePerson),
            PoliticalSupportTarget.ForPoliticalClaim(targetClaim), out PoliticalSupportRelationRecord active), Is.True);
        Assert.That(active.RelationId.Value, Is.EqualTo("support.person-claim.current"));

        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(new PersonId("p12e-support-after-token"), runtime.CurrentDay),
            out PersonStoreFailure latePersonFailure), Is.True, latePersonFailure.ToString());
        Assert.That(P12EPoliticalSupportOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EPoliticalSupportOwnerSnapshot stale, out captureFailure), Is.False);
        Assert.That(stale, Is.Null);
        Assert.That(snapshot.TryStage(stagedRoots.People, stagedRoots.Factions, stagedRoots.Claims,
            out PoliticalSupportStore stillStaged, out stageFailure), Is.True, stageFailure?.Message);
        Assert.That(stillStaged.Revision, Is.EqualTo(8L));
    }

    [Test]
    public void CaptureRejectsCopiedOwnerVectorAndSnapshotRetainsItsOwnCapturedDay()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());

        List<OwnerSectionCensusSnapshot> copiedVector = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        Assert.That(P12EPoliticalSupportOwnerSnapshot.TryCapture(runtime, token, copiedVector,
            out P12EPoliticalSupportOwnerSnapshot rejected, out P12EPoliticalSupportSnapshotFailure failure), Is.False);
        Assert.That(rejected, Is.Null);

        Assert.That(P12EPoliticalSupportOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EPoliticalSupportOwnerSnapshot empty, out failure), Is.True, failure?.Message);
        Assert.That(empty.CapturedAbsoluteDay, Is.EqualTo(1L));
        Assert.That(empty.Relations.RecordCount, Is.Zero);
        Assert.That(empty.Relations.Revision, Is.Zero);
        Roots roots = new Roots(registerDefaults: false);
        Assert.That(empty.TryStage(roots.People, roots.Factions, roots.Claims,
            out PoliticalSupportStore staged, out failure), Is.True, failure?.Message);
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);

        P12EPoliticalSupportOwnerSnapshot futureRow = Snapshot(1L, 0L, new[]
        {
            Row("future", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
                (int)PoliticalSupportDisposition.Support, 2L, null)
        });
        roots.RegisterPerson("person.source");
        roots.RegisterPerson("person.candidate");
        AssertStageFails(roots, futureRow, P12EPoliticalSupportSnapshotFailureCode.InvalidTimeline);
    }

    [Test]
    public void StagePreservesAnExactRevisionThatDiffersFromRowCountAndAcceptsSaturation()
    {
        Roots roots = new Roots();
        roots.RegisterPerson("person.other");
        P12EPoliticalSupportOwnerSnapshot snapshot = Snapshot(long.MaxValue, long.MaxValue, new[]
        {
            Row("one", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
                (int)PoliticalSupportDisposition.Support, 0L, null)
        });

        Assert.That(snapshot.TryStage(roots.People, roots.Factions, roots.Claims,
            out PoliticalSupportStore staged, out P12EPoliticalSupportSnapshotFailure failure),
            Is.True, failure?.Message);
        Assert.That(staged.Count, Is.EqualTo(1));
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(staged.TryRegister(new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("overflow"),
            PoliticalSupportSource.ForPerson(new PersonId("person.source")),
            PoliticalSupportTarget.ForSuccessionCandidate(new PersonId("person.other")),
            PoliticalSupportDisposition.Oppose, 0L), out PoliticalSupportFailure writeFailure), Is.False);
        Assert.That(writeFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.RevisionOverflow));
        Assert.That(staged.Count, Is.EqualTo(1));
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void StageRejectsMalformedSectionsRowsDuplicatesAndUnsortedData()
    {
        Roots roots = new Roots();
        P12EPoliticalSupportOwnerSnapshot valid = Snapshot(0L, 1L, new[]
        {
            Row("a", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "claim.target",
                (int)PoliticalSupportDisposition.Support, 0L, null)
        });
        AssertStageFails(roots, new P12EPoliticalSupportOwnerSnapshot(0L,
            Section("wrong.section", 1, 1, 0L, new P12EPoliticalSupportSnapshotRow[0])),
            P12EPoliticalSupportSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(roots, new P12EPoliticalSupportOwnerSnapshot(0L,
            Section(PoliticalSupportStoreCensusProvider.RelationsSectionId, 2, 1, 0L,
                new P12EPoliticalSupportSnapshotRow[0])), P12EPoliticalSupportSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(roots, new P12EPoliticalSupportOwnerSnapshot(0L,
            Section(PoliticalSupportStoreCensusProvider.RelationsSectionId, 1, 0, -1L,
                new P12EPoliticalSupportSnapshotRow[0])), P12EPoliticalSupportSnapshotFailureCode.InvalidRevision);
        AssertStageFails(roots, new P12EPoliticalSupportOwnerSnapshot(0L,
            Section(PoliticalSupportStoreCensusProvider.RelationsSectionId, 1, 2, 0L,
                new P12EPoliticalSupportSnapshotRow[0])), P12EPoliticalSupportSnapshotFailureCode.InvalidCardinality);
        AssertStageFails(roots, new P12EPoliticalSupportOwnerSnapshot(0L,
            Section(PoliticalSupportStoreCensusProvider.RelationsSectionId, 1, 0, 0L, null)),
            P12EPoliticalSupportSnapshotFailureCode.InvalidCardinality);
        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
                (int)PoliticalSupportDisposition.Support, 0L, null)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidIdentity);
        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("bad-kind", 99, "person.source", (int)PoliticalSupportTargetKind.SuccessionCandidate,
                "person.candidate", (int)PoliticalSupportDisposition.Support, 0L, null)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidIdentity);
        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("bad-disposition", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate", 99, 0L, null)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidIdentity);

        P12EPoliticalSupportSnapshotRow active = Row("active-a",
            (int)PoliticalSupportSourceKind.Person, "person.source",
            (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
            (int)PoliticalSupportDisposition.Support, 0L, null);
        AssertStageFails(roots, Snapshot(0L, 2L, new[] { active, active.Copy() }),
            P12EPoliticalSupportSnapshotFailureCode.DuplicateIdentity);
        AssertStageFails(roots, Snapshot(0L, 2L, new[] { active,
            Row("active-b", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
                (int)PoliticalSupportDisposition.Oppose, 0L, null) }),
            P12EPoliticalSupportSnapshotFailureCode.DuplicateIdentity);

        P12EPoliticalSupportSnapshotRow later = Row("z", (int)PoliticalSupportSourceKind.Person,
            "person.source", (int)PoliticalSupportTargetKind.PoliticalClaim, "claim.target",
            (int)PoliticalSupportDisposition.Support, 0L, 0L);
        P12EPoliticalSupportSnapshotRow earlier = Row("a", (int)PoliticalSupportSourceKind.Person,
            "person.source", (int)PoliticalSupportTargetKind.PoliticalClaim, "claim.target",
            (int)PoliticalSupportDisposition.Support, 0L, null);
        AssertStageFails(roots, Snapshot(0L, 2L, new[] { later, earlier }),
            P12EPoliticalSupportSnapshotFailureCode.InvalidOrdering);
        Assert.That(valid, Is.Not.Null);
        Assert.That(roots.Claims.Count, Is.EqualTo(1));
        Assert.That(roots.People.Persons.Count, Is.EqualTo(3));
    }

    [Test]
    public void StageChecksTypedReferencesAndLengthPrefixedPairIdentity()
    {
        Roots roots = new Roots();
        AssertStageFails(roots, Snapshot(0L, 0L, new[]
        {
            Row("dangling-person", (int)PoliticalSupportSourceKind.Person, "missing-person",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
                (int)PoliticalSupportDisposition.Support, 0L, null)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidReference);
        AssertStageFails(roots, Snapshot(0L, 0L, new[]
        {
            Row("dangling-faction", (int)PoliticalSupportSourceKind.Faction, "missing-faction",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "claim.target",
                (int)PoliticalSupportDisposition.Support, 0L, null)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidReference);
        AssertStageFails(roots, Snapshot(0L, 0L, new[]
        {
            Row("dangling-claim", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "missing-claim",
                (int)PoliticalSupportDisposition.Support, 0L, null)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidReference);
        AssertStageFails(roots, Snapshot(0L, 0L, new[]
        {
            Row("ended-before-start", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
                (int)PoliticalSupportDisposition.Support, 2L, 1L)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidTimeline);
        AssertStageFails(roots, Snapshot(0L, 0L, new[]
        {
            Row("ended-after-capture", (int)PoliticalSupportSourceKind.Person, "person.source",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.candidate",
                (int)PoliticalSupportDisposition.Support, 0L, 1L)
        }), P12EPoliticalSupportSnapshotFailureCode.InvalidTimeline);

        roots.RegisterPerson("a");
        roots.RegisterPerson("a:1");
        roots.RegisterPerson("shared");
        roots.RegisterPerson("candidate:1");
        roots.RegisterClaim("b");
        roots.RegisterClaim("1:b");
        roots.RegisterClaim("claim:shared");
        roots.RegisterFaction("shared");
        P12EPoliticalSupportOwnerSnapshot collisionSafe = Snapshot(0L, 0L, new[]
        {
            Row("separator-b", (int)PoliticalSupportSourceKind.Person, "a",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "1:b",
                (int)PoliticalSupportDisposition.Support, 0L, null),
            Row("separator-a", (int)PoliticalSupportSourceKind.Person, "a:1",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "b",
                (int)PoliticalSupportDisposition.Support, 0L, null),
            Row("typed-person", (int)PoliticalSupportSourceKind.Person, "shared",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "claim:shared",
                (int)PoliticalSupportDisposition.Support, 0L, null),
            Row("typed-faction", (int)PoliticalSupportSourceKind.Faction, "shared",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "claim:shared",
                (int)PoliticalSupportDisposition.Support, 0L, null)
        });
        Assert.That(collisionSafe.TryStage(roots.People, roots.Factions, roots.Claims,
            out PoliticalSupportStore staged, out P12EPoliticalSupportSnapshotFailure stageFailure),
            Is.True, stageFailure?.Message);
        Assert.That(staged.Count, Is.EqualTo(4));
        Assert.That(PoliticalSupportStore.PairKeyForP12EOwnerSnapshot(
            PoliticalSupportSource.ForPerson(new PersonId("a:1")),
            PoliticalSupportTarget.ForPoliticalClaim(new PoliticalClaimId("b"))),
            Is.Not.EqualTo(PoliticalSupportStore.PairKeyForP12EOwnerSnapshot(
                PoliticalSupportSource.ForPerson(new PersonId("a")),
                PoliticalSupportTarget.ForPoliticalClaim(new PoliticalClaimId("1:b")))));
        Assert.That(PoliticalSupportStore.PairKeyForP12EOwnerSnapshot(
            PoliticalSupportSource.ForPerson(new PersonId("shared")),
            PoliticalSupportTarget.ForPoliticalClaim(new PoliticalClaimId("claim:shared"))),
            Is.Not.EqualTo(PoliticalSupportStore.PairKeyForP12EOwnerSnapshot(
                PoliticalSupportSource.ForFaction(new FactionId("shared")),
                PoliticalSupportTarget.ForPoliticalClaim(new PoliticalClaimId("claim:shared")))));
    }

    private SimulationRuntime CreateDailyRuntime()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject instance = new GameObject("p12e-political-support-snapshot-test");
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

    private static void RegisterPerson(SimulationRuntime runtime, PersonId id)
    {
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(id, runtime.CurrentDay),
            out PersonStoreFailure failure), Is.True, failure.ToString());
    }

    private static void RegisterSupport(SimulationRuntime runtime, PoliticalSupportRelationRecord record)
    {
        Assert.That(runtime.TryRegisterPoliticalSupport(record, out PoliticalSupportFailure failure),
            Is.True, failure.ToString());
    }

    private static PoliticalClaimRecord CreateClaim(
        PoliticalClaimId claimId,
        PersonId claimant,
        PersonId target,
        long day)
    {
        return new PoliticalClaimRecord(claimId, claimant, PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(target), PoliticalClaimBasis.Other, "support snapshot target", day,
            Array.Empty<string>());
    }

    private static Roots CreateStagedRoots(
        PersonId source,
        PersonId candidate,
        FactionId faction,
        PoliticalClaimRecord claim)
    {
        Roots roots = new Roots(registerDefaults: false);
        roots.RegisterPerson(source.Value);
        roots.RegisterPerson(candidate.Value);
        Assert.That(roots.Factions.TryRegister(new FactionRecord(faction, "Support fixture", 0L),
            out FactionFoundationFailure factionFailure), Is.True, factionFailure.ToString());
        Assert.That(roots.Claims.TryRegister(claim, out PoliticalClaimFailure claimFailure),
            Is.True, claimFailure.ToString());
        return roots;
    }

    private static void AssertStageFails(
        Roots roots,
        P12EPoliticalSupportOwnerSnapshot snapshot,
        P12EPoliticalSupportSnapshotFailureCode expectedCode)
    {
        int personCount = roots.People.Persons.Count;
        int factionCount = roots.Factions.Count;
        int claimCount = roots.Claims.Count;
        Assert.That(snapshot.TryStage(roots.People, roots.Factions, roots.Claims,
            out PoliticalSupportStore staged, out P12EPoliticalSupportSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expectedCode));
        Assert.That(roots.People.Persons.Count, Is.EqualTo(personCount));
        Assert.That(roots.Factions.Count, Is.EqualTo(factionCount));
        Assert.That(roots.Claims.Count, Is.EqualTo(claimCount));
    }

    private static OwnerSectionCensusSnapshot FindWitness(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId)
    {
        foreach (OwnerSectionCensusSnapshot section in vector)
        {
            if (section != null && string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) return section;
        }
        Assert.Fail("Required witness is absent: " + sectionId);
        return null;
    }

    private static P12EPoliticalSupportSnapshotRow Row(
        string relationId,
        int sourceKind,
        string sourceId,
        int targetKind,
        string targetId,
        int disposition,
        long started,
        long? ended)
    {
        return new P12EPoliticalSupportSnapshotRow(relationId, sourceKind, sourceId,
            targetKind, targetId, disposition, started, ended);
    }

    private static P12EPoliticalSupportSnapshotSection Section(
        string sectionId,
        int schema,
        int count,
        long revision,
        IEnumerable<P12EPoliticalSupportSnapshotRow> rows)
    {
        return new P12EPoliticalSupportSnapshotSection(sectionId, schema, count, revision, rows);
    }

    private static P12EPoliticalSupportOwnerSnapshot Snapshot(
        long capturedDay,
        long revision,
        IEnumerable<P12EPoliticalSupportSnapshotRow> rows)
    {
        List<P12EPoliticalSupportSnapshotRow> copy = new List<P12EPoliticalSupportSnapshotRow>(rows);
        return new P12EPoliticalSupportOwnerSnapshot(capturedDay,
            Section(PoliticalSupportStoreCensusProvider.RelationsSectionId,
                PoliticalSupportStoreCensusProvider.SchemaVersion, copy.Count, revision, copy));
    }

    private static string Signature(P12EPoliticalSupportOwnerSnapshot snapshot)
    {
        StringBuilder value = new StringBuilder();
        value.Append(snapshot.CapturedAbsoluteDay).Append('|')
            .Append(snapshot.Relations.SectionId).Append('|')
            .Append(snapshot.Relations.SchemaVersion).Append('|')
            .Append(snapshot.Relations.RecordCount).Append('|')
            .Append(snapshot.Relations.Revision).Append('|');
        foreach (P12EPoliticalSupportSnapshotRow row in snapshot.Relations.Records)
        {
            Append(value, row.RelationId);
            value.Append(row.SourceKind).Append('|'); Append(value, row.SourceId);
            value.Append(row.TargetKind).Append('|'); Append(value, row.TargetId);
            value.Append(row.Disposition).Append('|').Append(row.StartedAbsoluteDay).Append('|')
                .Append(row.EndedAbsoluteDay?.ToString() ?? "-").Append(';');
        }
        return value.ToString();
    }

    private static string Signature(IReadOnlyList<PoliticalSupportRelationRecord> records)
    {
        StringBuilder value = new StringBuilder();
        foreach (PoliticalSupportRelationRecord row in records)
        {
            Append(value, row.RelationId.Value);
            value.Append((int)row.Source.Kind).Append('|'); Append(value, row.Source.Value);
            value.Append((int)row.Target.Kind).Append('|'); Append(value, row.Target.Value);
            value.Append((int)row.Disposition).Append('|').Append(row.StartedAbsoluteDay).Append('|')
                .Append(row.EndedAbsoluteDay?.ToString() ?? "-").Append(';');
        }
        return value.ToString();
    }

    private static string Signature(IReadOnlyList<P12EPoliticalSupportSnapshotRow> rows)
    {
        StringBuilder value = new StringBuilder();
        foreach (P12EPoliticalSupportSnapshotRow row in rows)
        {
            Append(value, row.RelationId);
            value.Append(row.SourceKind).Append('|'); Append(value, row.SourceId);
            value.Append(row.TargetKind).Append('|'); Append(value, row.TargetId);
            value.Append(row.Disposition).Append('|').Append(row.StartedAbsoluteDay).Append('|')
                .Append(row.EndedAbsoluteDay?.ToString() ?? "-").Append(';');
        }
        return value.ToString();
    }

    private static void Append(StringBuilder builder, string value)
    {
        builder.Append(value.Length).Append(':').Append(value).Append('|');
    }

    private sealed class Roots
    {
        internal readonly PersonStore People = new PersonStore();
        internal readonly FactionStore Factions;
        internal readonly PoliticalClaimStore Claims = new PoliticalClaimStore();

        internal Roots(bool registerDefaults = true)
        {
            Factions = new FactionStore(People);
            if (!registerDefaults) return;
            RegisterPerson("person.claimant");
            RegisterPerson("person.source");
            RegisterPerson("person.candidate");
            RegisterFaction("faction.source");
            RegisterClaim("claim.target");
        }

        internal void RegisterPerson(string id)
        {
            Assert.That(People.TryRegister(new PersonRuntime(new PersonId(id), 0L),
                out PersonStoreFailure failure), Is.True, failure.ToString());
        }

        internal void RegisterFaction(string id)
        {
            Assert.That(Factions.TryRegister(new FactionRecord(new FactionId(id), id, 0L),
                out FactionFoundationFailure failure), Is.True, failure.ToString());
        }

        internal void RegisterClaim(string id)
        {
            PoliticalClaimRecord claim = CreateClaim(new PoliticalClaimId(id),
                new PersonId("person.claimant"), new PersonId("person.candidate"), 0L);
            Assert.That(Claims.TryRegister(claim, out PoliticalClaimFailure failure),
                Is.True, failure.ToString());
        }
    }
}
