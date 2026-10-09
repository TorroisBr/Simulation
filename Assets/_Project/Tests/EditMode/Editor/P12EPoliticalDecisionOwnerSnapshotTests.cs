using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12EPoliticalDecisionOwnerSnapshotTests
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
    public void RequiredEmptyOwnerCapturesAndStagesAtExactZero()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());

        Assert.That(P12EPoliticalDecisionOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EPoliticalDecisionOwnerSnapshot snapshot, out P12EPoliticalDecisionSnapshotFailure failure),
            Is.True, failure?.Message);
        Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(token.AbsoluteDay));
        Assert.That(snapshot.Decisions.SectionId, Is.EqualTo(PoliticalDecisionStoreCensusProvider.SectionId));
        Assert.That(snapshot.Decisions.SchemaVersion, Is.EqualTo(PoliticalDecisionStoreCensusProvider.SchemaVersion));
        Assert.That(snapshot.Decisions.RecordCount, Is.Zero);
        Assert.That(snapshot.Decisions.Revision, Is.Zero);

        StagedRoots roots = new StagedRoots();
        Assert.That(snapshot.TryStage(roots.People, roots.Institutions, roots.Offices, roots.Claims,
            out PoliticalDecisionStore staged, out failure), Is.True, failure?.Message);
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);

        List<OwnerSectionCensusSnapshot> copiedVector = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        Assert.That(P12EPoliticalDecisionOwnerSnapshot.TryCapture(runtime, token, copiedVector,
            out P12EPoliticalDecisionOwnerSnapshot rejected, out failure), Is.False);
        Assert.That(rejected, Is.Null);
    }

    [Test]
    public void StageRoundTripsTypedDecisionHistoryAndBindsRowsToStagedPersons()
    {
        PersonId deciderPerson = new PersonId("p12e-decision-person-decider");
        PersonId candidateA = new PersonId("p12e-decision-candidate-a");
        PersonId candidateB = new PersonId("p12e-decision-candidate-b");
        InstitutionId institution = new InstitutionId("p12e-decision-institution");
        OfficeId office = new OfficeId("p12e-decision-office");
        PoliticalClaimId claim = new PoliticalClaimId("p12e-decision-claim");

        StagedRoots roots = new StagedRoots();
        roots.RegisterPerson(deciderPerson);
        roots.RegisterPerson(candidateA);
        roots.RegisterPerson(candidateB);
        roots.RegisterInstitution(institution);
        roots.RegisterOffice(office, institution);
        roots.RegisterClaim(CreateClaim(claim, deciderPerson, candidateA));
        P12EPoliticalDecisionOwnerSnapshot snapshot = Snapshot(1L, 4L, new[]
        {
            Row("decision.a", (int)PoliticalKnowledgeHolderKind.Person, deciderPerson.Value,
                (int)PoliticalDecisionKind.SuccessionSelection, new[] { candidateA.Value, candidateB.Value },
                (int)PoliticalDecisionOutcomeKind.CandidateSelected, candidateA.Value, null, 0L, 0L,
                new[] { "evidence.a", "evidence.z" }, new[] { "knowledge.a", "knowledge.z" },
                4L, 9L, office.Value, null),
            Row("decision.b", (int)PoliticalKnowledgeHolderKind.Institution, institution.Value,
                (int)PoliticalDecisionKind.OfficeSelection, new[] { candidateA.Value },
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                new[] { "evidence.b" }, new[] { "knowledge.b" }, 5L, 10L, office.Value, null),
            Row("decision.c", (int)PoliticalKnowledgeHolderKind.Faction, "p12e-decision-faction",
                (int)PoliticalDecisionKind.ClaimRecognitionProposal, new[] { candidateB.Value },
                (int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed, null, claim.Value, 0L, 1L,
                new[] { "evidence.c" }, new[] { "knowledge.c" }, 6L, 11L, null, institution.Value),
            Row("decision.d", (int)PoliticalKnowledgeHolderKind.Person, deciderPerson.Value,
                (int)PoliticalDecisionKind.Other, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.Rejected, null, null, 0L, 1L,
                Array.Empty<string>(), Array.Empty<string>(), 7L, 12L, null, null)
        });
        Assert.That(snapshot.Decisions.RecordCount, Is.EqualTo(4));
        Assert.That(snapshot.Decisions.Revision, Is.EqualTo(4L));

        Assert.That(snapshot.TryStage(roots.People, roots.Institutions, roots.Offices, roots.Claims,
            out PoliticalDecisionStore staged, out P12EPoliticalDecisionSnapshotFailure failure),
            Is.True, failure?.Message);
        Assert.That(staged.Count, Is.EqualTo(4));
        Assert.That(staged.Revision, Is.EqualTo(snapshot.Decisions.Revision));

        Assert.That(staged.TryGet(new PoliticalDecisionId("decision.a"), out PoliticalDecisionRecord restoredPerson), Is.True);
        Assert.That(restoredPerson.Decider, Is.EqualTo(PoliticalKnowledgeHolder.ForPerson(deciderPerson)));
        Assert.That(restoredPerson.DecisionKind, Is.EqualTo(PoliticalDecisionKind.SuccessionSelection));
        Assert.That(restoredPerson.CandidatePersonIds, Is.EqualTo(new[] { candidateA, candidateB }));
        Assert.That(restoredPerson.CandidateFingerprint, Is.EqualTo(
            PoliticalDecisionRecord.BuildCandidateFingerprint(new[] { candidateA, candidateB })));
        Assert.That(restoredPerson.Outcome, Is.EqualTo(PoliticalDecisionOutcome.Candidate(candidateA)));
        Assert.That(restoredPerson.EvidenceReferences, Is.EqualTo(new[] { "evidence.a", "evidence.z" }));
        Assert.That(restoredPerson.KnowledgeReferences, Is.EqualTo(new[] { "knowledge.a", "knowledge.z" }));
        Assert.That(restoredPerson.ExpectedWorldRevision, Is.EqualTo(4L));
        Assert.That(restoredPerson.ExpectedKnowledgeRevision, Is.EqualTo(9L));
        Assert.That(restoredPerson.OfficeId, Is.EqualTo(office));
        Assert.That(restoredPerson.IsCompatibleWithPersonStore(roots.People), Is.True);
        Assert.That(restoredPerson.IsCompatibleWithPersonStore(new PersonStore()), Is.False);

        Assert.That(staged.TryGet(new PoliticalDecisionId("decision.b"), out PoliticalDecisionRecord restoredOffice), Is.True);
        Assert.That(restoredOffice.Decider.Kind, Is.EqualTo(PoliticalKnowledgeHolderKind.Institution));
        Assert.That(restoredOffice.DecisionKind, Is.EqualTo(PoliticalDecisionKind.OfficeSelection));
        Assert.That(restoredOffice.Outcome.Kind, Is.EqualTo(PoliticalDecisionOutcomeKind.NoSelection));
        Assert.That(restoredOffice.ExpectedWorldRevision, Is.EqualTo(5L));

        Assert.That(staged.TryGet(new PoliticalDecisionId("decision.c"), out PoliticalDecisionRecord restoredClaim), Is.True);
        Assert.That(restoredClaim.Decider.Kind, Is.EqualTo(PoliticalKnowledgeHolderKind.Faction));
        Assert.That(restoredClaim.Decider.FactionId.Value, Is.EqualTo("p12e-decision-faction"));
        Assert.That(restoredClaim.DecisionKind, Is.EqualTo(PoliticalDecisionKind.ClaimRecognitionProposal));
        Assert.That(restoredClaim.Outcome.ReferencedClaimId, Is.EqualTo(claim));
        Assert.That(restoredClaim.RecognizingInstitutionId, Is.EqualTo(institution));
        Assert.That(restoredClaim.DecisionAbsoluteDay, Is.EqualTo(1L));
        Assert.That(staged.TryGet(new PoliticalDecisionId("decision.d"), out PoliticalDecisionRecord restoredOther), Is.True);
        Assert.That(restoredOther.DecisionKind, Is.EqualTo(PoliticalDecisionKind.Other));
        Assert.That(restoredOther.Outcome.Kind, Is.EqualTo(PoliticalDecisionOutcomeKind.Rejected));
    }

    [Test]
    public void CaptureRejectsExpiredTokenAndOwnerRevisionDrift()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvance), Is.True, firstAdvance.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        OwnerSectionCensusSnapshot witness = FindWitness(token.OwnerSections,
            PoliticalDecisionStoreCensusProvider.SectionId);
        PoliticalDecisionStore owner = witness.OwnerInstanceIdentity as PoliticalDecisionStore;
        Assert.That(owner, Is.Not.Null);

        PoliticalDecisionRecord appended = new PoliticalDecisionRecord(
            new PoliticalDecisionId("decision.drift"),
            PoliticalKnowledgeHolder.ForPerson(new PersonId("p12e-decision-drift-person")),
            PoliticalDecisionKind.Other, Array.Empty<PersonId>(), PoliticalDecisionOutcome.None(),
            0L, 0L, Array.Empty<string>(), Array.Empty<string>(), 0L, 0L);
        Assert.That(owner.TryRegister(appended, out PoliticalDecisionFailure appendFailure),
            Is.True, appendFailure.ToString());
        Assert.That(owner.Count, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(1L));
        Assert.That(P12EPoliticalDecisionOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12EPoliticalDecisionOwnerSnapshot staleOwner, out P12EPoliticalDecisionSnapshotFailure failure), Is.False);
        Assert.That(staleOwner, Is.Null);

        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.False);
    }

    [Test]
    public void StageRejectsNoncanonicalListsMalformedOutcomesAndUnresolvedRootReferences()
    {
        StagedRoots roots = new StagedRoots();
        roots.RegisterPerson(new PersonId("person.a"));
        roots.RegisterPerson(new PersonId("person.b"));
        roots.RegisterInstitution(new InstitutionId("institution.a"));
        roots.RegisterClaim(CreateClaim(new PoliticalClaimId("claim.a"),
            new PersonId("person.a"), new PersonId("person.b")));

        AssertStageFails(roots, new P12EPoliticalDecisionOwnerSnapshot(0L,
            new P12EPoliticalDecisionSnapshotSection("wrong.section", 1, 0, 0L,
                Array.Empty<P12EPoliticalDecisionSnapshotRow>())),
            P12EPoliticalDecisionSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(roots, new P12EPoliticalDecisionOwnerSnapshot(0L,
            new P12EPoliticalDecisionSnapshotSection(PoliticalDecisionStoreCensusProvider.SectionId,
                2, 0, 0L, Array.Empty<P12EPoliticalDecisionSnapshotRow>())),
            P12EPoliticalDecisionSnapshotFailureCode.UnsupportedSchema);
        roots.RegisterOffice(new OfficeId("office.a"), new InstitutionId("institution.a"));

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("ordered", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, new[] { "person.b", "person.a" },
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidOrdering);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("bad-outcome", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, new[] { "person.a" },
                (int)PoliticalDecisionOutcomeKind.CandidateSelected, "person.missing", null, 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidOutcome);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("missing-person", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, new[] { "person.missing" },
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidReference);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("missing-office", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.OfficeSelection, new[] { "person.a" },
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, "office.missing", null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidReference);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("future", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 1L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidTimeline);

        AssertStageFails(roots, Snapshot(0L, 2L, new[]
        {
            Row("duplicate", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, null),
            Row("duplicate", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, null)
        }), P12EPoliticalDecisionSnapshotFailureCode.DuplicateIdentity);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("duplicate-candidate", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, new[] { "person.a", "person.a" },
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidOrdering);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("noncanonical-reference", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.Other, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null, 0L, 0L,
                new[] { "evidence.z", "evidence.a" }, Array.Empty<string>(), 0L, 0L, null, null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidOrdering);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("missing-claim", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.ClaimRecognitionProposal, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed, null, "claim.missing", 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, "institution.a")
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidReference);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("missing-recognizer", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.ClaimRecognitionProposal, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed, null, "claim.a", 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, null, "institution.missing")
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidReference);

        AssertStageFails(roots, Snapshot(0L, 1L, new[]
        {
            Row("office-claim-outcome", (int)PoliticalKnowledgeHolderKind.Person, "person.a",
                (int)PoliticalDecisionKind.OfficeSelection, Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed, null, "claim.a", 0L, 0L,
                Array.Empty<string>(), Array.Empty<string>(), 0L, 0L, "office.a", null)
        }), P12EPoliticalDecisionSnapshotFailureCode.InvalidOutcome);
    }

    [Test]
    public void StageRequiresExactAppendOnlyRevisionAndPreservesDeferredFGValues()
    {
        StagedRoots roots = new StagedRoots();
        roots.RegisterPerson(new PersonId("person.candidate"));
        P12EPoliticalDecisionOwnerSnapshot invalidRevision = Snapshot(0L, 2L, new[]
        {
            Row("one", (int)PoliticalKnowledgeHolderKind.Faction, "future-faction",
                (int)PoliticalDecisionKind.Other, new[] { "person.candidate" },
                (int)PoliticalDecisionOutcomeKind.CandidateSelected, "person.candidate", null, 0L, 0L,
                new[] { "evidence.a" }, new[] { "knowledge.unresolved" }, 7L, 9L, null, null)
        });
        AssertStageFails(roots, invalidRevision, P12EPoliticalDecisionSnapshotFailureCode.InvalidRevision);

        P12EPoliticalDecisionOwnerSnapshot deferred = Snapshot(0L, 1L, new[]
        {
            Row("one", (int)PoliticalKnowledgeHolderKind.Faction, "future-faction",
                (int)PoliticalDecisionKind.Other, new[] { "person.candidate" },
                (int)PoliticalDecisionOutcomeKind.CandidateSelected, "person.candidate", null, 0L, 0L,
                new[] { "evidence.a" }, new[] { "knowledge.unresolved" }, 7L, 9L, null, null)
        });
        Assert.That(deferred.TryStage(roots.People, roots.Institutions, roots.Offices, roots.Claims,
            out PoliticalDecisionStore staged, out P12EPoliticalDecisionSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(staged.TryGet(new PoliticalDecisionId("one"), out PoliticalDecisionRecord row), Is.True);
        Assert.That(row.Decider.Kind, Is.EqualTo(PoliticalKnowledgeHolderKind.Faction));
        Assert.That(row.Decider.FactionId.Value, Is.EqualTo("future-faction"));
        Assert.That(row.KnowledgeReferences, Is.EqualTo(new[] { "knowledge.unresolved" }));
        Assert.That(row.ExpectedKnowledgeRevision, Is.EqualTo(9L));
        Assert.That(row.ExpectedWorldRevision, Is.EqualTo(7L));
    }

    private SimulationRuntime CreateDailyRuntime()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject instance = new GameObject("p12e-political-decision-snapshot-test");
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

    private static PoliticalClaimRecord CreateClaim(PoliticalClaimId id, PersonId claimant, PersonId target)
    {
        return new PoliticalClaimRecord(id, claimant, PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(target), PoliticalClaimBasis.Other, "P12-E snapshot fixture", 0L,
            Array.Empty<string>());
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

    private static P12EPoliticalDecisionSnapshotRow Row(
        string decisionId,
        int deciderKind,
        string deciderId,
        int decisionKind,
        IEnumerable<string> candidates,
        int outcomeKind,
        string selected,
        string claim,
        long observed,
        long decided,
        IEnumerable<string> evidence,
        IEnumerable<string> knowledge,
        long expectedWorld,
        long expectedKnowledge,
        string office,
        string recognizingInstitution)
    {
        return new P12EPoliticalDecisionSnapshotRow(decisionId, deciderKind, deciderId,
            decisionKind, candidates, outcomeKind, selected, claim, observed, decided,
            evidence, knowledge, expectedWorld, expectedKnowledge, office, recognizingInstitution);
    }

    private static P12EPoliticalDecisionOwnerSnapshot Snapshot(
        long capturedDay,
        long revision,
        IEnumerable<P12EPoliticalDecisionSnapshotRow> rows)
    {
        List<P12EPoliticalDecisionSnapshotRow> copy = new List<P12EPoliticalDecisionSnapshotRow>(rows);
        return new P12EPoliticalDecisionOwnerSnapshot(capturedDay,
            new P12EPoliticalDecisionSnapshotSection(PoliticalDecisionStoreCensusProvider.SectionId,
                PoliticalDecisionStoreCensusProvider.SchemaVersion, copy.Count, revision, copy));
    }

    private static void AssertStageFails(
        StagedRoots roots,
        P12EPoliticalDecisionOwnerSnapshot snapshot,
        P12EPoliticalDecisionSnapshotFailureCode expectedCode)
    {
        int peopleBefore = roots.People.Persons.Count;
        int institutionsBefore = roots.Institutions.Count;
        int officesBefore = roots.Offices.Count;
        int claimsBefore = roots.Claims.Count;
        Assert.That(snapshot.TryStage(roots.People, roots.Institutions, roots.Offices, roots.Claims,
            out PoliticalDecisionStore staged, out P12EPoliticalDecisionSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expectedCode));
        Assert.That(roots.People.Persons.Count, Is.EqualTo(peopleBefore));
        Assert.That(roots.Institutions.Count, Is.EqualTo(institutionsBefore));
        Assert.That(roots.Offices.Count, Is.EqualTo(officesBefore));
        Assert.That(roots.Claims.Count, Is.EqualTo(claimsBefore));
    }

    private sealed class StagedRoots
    {
        internal readonly PersonStore People = new PersonStore();
        internal readonly InstitutionStore Institutions = new InstitutionStore();
        internal readonly OfficeStore Offices;
        internal readonly PoliticalClaimStore Claims = new PoliticalClaimStore();

        internal StagedRoots()
        {
            Offices = new OfficeStore(Institutions);
        }

        internal void RegisterPerson(PersonId id)
        {
            Assert.That(People.TryRegister(new PersonRuntime(id, 0L), out PersonStoreFailure failure),
                Is.True, failure.ToString());
        }

        internal void RegisterInstitution(InstitutionId id)
        {
            Assert.That(Institutions.TryRegister(new InstitutionRecord(id, id.Value),
                out InstitutionFoundationFailure failure), Is.True, failure.ToString());
        }

        internal void RegisterOffice(OfficeId id, InstitutionId institutionId)
        {
            Assert.That(Offices.TryRegister(new OfficeRecord(id, institutionId, id.Value),
                out InstitutionFoundationFailure failure), Is.True, failure.ToString());
        }

        internal void RegisterClaim(PoliticalClaimRecord claim)
        {
            Assert.That(Claims.TryRegister(claim, out PoliticalClaimFailure failure), Is.True, failure.ToString());
        }
    }
}
