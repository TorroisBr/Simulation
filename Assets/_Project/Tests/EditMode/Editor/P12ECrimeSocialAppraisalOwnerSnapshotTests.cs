using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12ECrimeSocialAppraisalOwnerSnapshotTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject instance in simulationObjects)
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
        simulationObjects.Clear();
    }

    [Test]
    public void EmptyCaptureStagesAllThreeExactOwnersAgainstTheSuppliedRoots()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        AdvanceAndGetToken(runtime, out DailyCaptureEligibilityToken token);

        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
            out P12ECrimeSocialAppraisalSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(token.AbsoluteDay));
        AssertSection(snapshot.Outcomes, P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, 0, 0L);
        AssertSection(snapshot.Knowledge, P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, 0, 0L);
        AssertSection(snapshot.Reactions, P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, 0, 0L);

        StagedRoots roots = CreateStagedRoots(Array.Empty<string>(), Array.Empty<string>(), token.AbsoluteDay);
        Assert.That(snapshot.TryStage(roots.People, roots.Institutions, roots.Time,
            out CrimeSocialAppraisalWorldState staged, out failure), Is.True, failure?.Message);
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged.PersonStore, Is.SameAs(roots.People));
        Assert.That(staged.InstitutionStore, Is.SameAs(roots.Institutions));
        Assert.That(staged.SimulationTime, Is.SameAs(roots.Time));
        Assert.That(staged.TheftOutcomes.Count, Is.Zero);
        Assert.That(staged.CrimeKnowledge.Count, Is.Zero);
        Assert.That(staged.SocialReactions.Count, Is.Zero);
        Assert.That(staged.CrimeKnowledge.PersonStore, Is.SameAs(roots.People));
        Assert.That(staged.CrimeKnowledge.OutcomeStore, Is.SameAs(staged.TheftOutcomes));
        Assert.That(staged.CrimeKnowledge.SimulationTime, Is.SameAs(roots.Time));
        Assert.That(staged.CrimeKnowledge.InstitutionStoreForWorldBoundary, Is.SameAs(roots.Institutions));
        Assert.That(staged.SocialReactions.PersonStore, Is.SameAs(roots.People));
        Assert.That(staged.SocialReactions.SimulationTime, Is.SameAs(roots.Time));
        Assert.That(staged.Integration, Is.Not.Null);
        Assert.That(staged, Is.Not.SameAs(runtime.CrimeSocialAppraisal));
    }

    [Test]
    public void PopulatedCapturePreservesAllOwnerValuesRevisionsAndReactionHistory()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        PersonId perpetrator = new PersonId("p12e-crime-perpetrator");
        PersonId victim = new PersonId("p12e-crime-victim");
        PersonId witness = new PersonId("p12e-crime-witness");
        InstitutionId investigator = new InstitutionId("p12e-crime-investigator");
        RegisterPerson(runtime, perpetrator);
        RegisterPerson(runtime, victim);
        RegisterPerson(runtime, witness);
        Assert.That(runtime.TryRegisterInstitution(new InstitutionRecord(investigator, "Investigator"),
            out InstitutionFoundationFailure institutionFailure), Is.True, institutionFailure.ToString());
        AdvanceAndGetToken(runtime, out DailyCaptureEligibilityToken emptyToken);
        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(runtime, emptyToken,
            emptyToken.OwnerSections, out P12ECrimeSocialAppraisalOwnerSnapshot emptySnapshot,
            out P12ECrimeSocialAppraisalSnapshotFailure failure), Is.True, failure?.Message);
        Assert.That(emptySnapshot.Outcomes.RecordCount, Is.Zero);
        Assert.That(emptySnapshot.Knowledge.RecordCount, Is.Zero);
        Assert.That(emptySnapshot.Reactions.RecordCount, Is.Zero);

        TheftOutcome outcome = CreateOutcome(perpetrator, victim, 0L, "theft-occurrence:opaque");
        Assert.That(runtime.CrimeSocialAppraisal.TheftOutcomes.TryRecord(outcome,
            out CrimeOutcomeStoreFailure outcomeFailure), Is.True, outcomeFailure.ToString());

        CrimeKnowledgeStore crimeKnowledge = runtime.CrimeSocialAppraisal.CrimeKnowledge;
        CrimeKnowledgeObservation initial = new CrimeKnowledgeObservation(
            victim, outcome.OutcomeId, CrimeKnowledgeRole.Victim, true,
            SocialPerceivedAttribution.Unknown(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectExperience, "initial:opaque"), 0L);
        CrimeKnowledgeObservation replacement = new CrimeKnowledgeObservation(
            victim, outcome.OutcomeId, CrimeKnowledgeRole.Victim, true,
            SocialPerceivedAttribution.BelievedPerson(perpetrator),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.ReceivedInformation,
                "replacement:opaque", sourcePersonId: witness), 0L);
        CrimeKnowledgeObservation current = new CrimeKnowledgeObservation(
            victim, outcome.OutcomeId, CrimeKnowledgeRole.Victim, true,
            SocialPerceivedAttribution.BelievedInstitution(investigator),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.InstitutionalRecord,
                "current:opaque", sourcePersonId: witness), 0L,
            knownInvestigatorInstitutionId: investigator);
        Assert.That(crimeKnowledge.TryRecord(initial, out CrimeKnowledgeStoreFailure knowledgeFailure),
            Is.True, knowledgeFailure.ToString());
        Assert.That(crimeKnowledge.TryRecord(replacement, out knowledgeFailure),
            Is.True, knowledgeFailure.ToString());
        Assert.That(crimeKnowledge.TryRecord(current, out knowledgeFailure),
            Is.True, knowledgeFailure.ToString());
        Assert.That(crimeKnowledge.Count, Is.EqualTo(1));
        Assert.That(crimeKnowledge.P12CensusRevision, Is.EqualTo(3L));
        CrimeKnowledgeObservation compensated = new CrimeKnowledgeObservation(
            perpetrator, outcome.OutcomeId, CrimeKnowledgeRole.Perpetrator, false,
            SocialPerceivedAttribution.NotApplicable(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "compensated:opaque"), 0L);
        Assert.That(crimeKnowledge.TryRecord(compensated, out knowledgeFailure),
            Is.True, knowledgeFailure.ToString());
        Assert.That(crimeKnowledge.TryRemove(perpetrator, outcome.OutcomeId), Is.True);
        Assert.That(crimeKnowledge.Count, Is.EqualTo(1));
        Assert.That(crimeKnowledge.P12CensusRevision, Is.EqualTo(5L));

        SocialReactionStore reactions = runtime.CrimeSocialAppraisal.SocialReactions;
        SocialSourceReference outcomeSource = new SocialSourceReference("crime", "source:opaque");
        SocialReaction first = CreateReaction(victim, outcomeSource,
            SocialReactionTarget.ForTheftOutcome(outcome.OutcomeId.Value),
            SocialPerceivedAttribution.Unknown(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "first:opaque"),
            SocialReactionValence.Negative, SocialReactionSalience.Medium, 0L, null);
        SocialReaction successor = CreateReaction(victim, outcomeSource,
            SocialReactionTarget.ForTheftOutcome(outcome.OutcomeId.Value),
            SocialPerceivedAttribution.BelievedPerson(perpetrator),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.ReceivedInformation,
                "successor:opaque", sourceInstitutionId: investigator),
            SocialReactionValence.Negative, SocialReactionSalience.High, 0L, first.ReactionId);
        SocialReaction personTarget = CreateReaction(witness,
            new SocialSourceReference("custom-domain", "person-source:opaque"),
            SocialReactionTarget.ForPerson(perpetrator), SocialPerceivedAttribution.NotApplicable(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.KnownFact, "person-target:opaque"),
            SocialReactionValence.Positive, SocialReactionSalience.Low, 0L, null);
        SocialReaction institutionTarget = CreateReaction(witness,
            new SocialSourceReference("custom-domain", "institution-source:opaque"),
            SocialReactionTarget.ForInstitution(investigator),
            SocialPerceivedAttribution.BelievedInstitution(investigator),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.InstitutionalRecord,
                "institution-target:opaque", sourcePersonId: victim),
            SocialReactionValence.Negative, SocialReactionSalience.Exceptional, 0L, null);
        RegisterReaction(reactions, first);
        RegisterReaction(reactions, successor);
        RegisterReaction(reactions, personTarget);
        RegisterReaction(reactions, institutionTarget);

        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(runtime, emptyToken,
            emptyToken.OwnerSections, out P12ECrimeSocialAppraisalOwnerSnapshot stale,
            out failure), Is.False);
        Assert.That(stale, Is.Null);
        AdvanceAndGetToken(runtime, out DailyCaptureEligibilityToken token);
        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
            out failure), Is.True, failure?.Message);
        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12ECrimeSocialAppraisalOwnerSnapshot repeated, out failure), Is.True, failure?.Message);
        Assert.That(Signature(snapshot), Is.EqualTo(Signature(repeated)));
        Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(token.AbsoluteDay));
        Assert.That(snapshot.Outcomes.RecordCount, Is.EqualTo(1));
        Assert.That(snapshot.Outcomes.Revision, Is.EqualTo(1L));
        Assert.That(snapshot.Outcomes.Records[0].OriginDecisionId, Is.EqualTo("decision:opaque"));
        Assert.That(snapshot.Knowledge.RecordCount, Is.EqualTo(1));
        Assert.That(snapshot.Knowledge.Revision, Is.EqualTo(5L));
        Assert.That(snapshot.Knowledge.Records[0].AttributionInstitutionId, Is.EqualTo(investigator.Value));
        Assert.That(snapshot.Knowledge.Records[0].CognitiveBasisReference, Is.EqualTo("current:opaque"));
        Assert.That(snapshot.Knowledge.Records[0].CognitiveBasisPersonId, Is.EqualTo(witness.Value));
        Assert.That(snapshot.Knowledge.Records[0].KnownInvestigatorInstitutionId, Is.EqualTo(investigator.Value));
        Assert.That(snapshot.Reactions.RecordCount, Is.EqualTo(4));
        Assert.That(snapshot.Reactions.Revision, Is.EqualTo(4L));

        StagedRoots roots = CreateStagedRoots(
            new[] { perpetrator.Value, victim.Value, witness.Value },
            new[] { investigator.Value }, token.AbsoluteDay);
        Assert.That(snapshot.TryStage(roots.People, roots.Institutions, roots.Time,
            out CrimeSocialAppraisalWorldState staged, out failure), Is.True, failure?.Message);
        Assert.That(staged.TheftOutcomes.P12CensusRevision, Is.EqualTo(1L));
        Assert.That(staged.CrimeKnowledge.P12CensusRevision, Is.EqualTo(5L));
        Assert.That(staged.CrimeKnowledge.Count, Is.EqualTo(1));
        Assert.That(staged.SocialReactions.P12CensusRevision, Is.EqualTo(4L));
        Assert.That(staged.TheftOutcomes.TryGet(outcome.OutcomeId, out TheftOutcome restoredOutcome), Is.True);
        AssertOutcomeEqual(outcome, restoredOutcome);
        Assert.That(staged.CrimeKnowledge.TryGet(victim, outcome.OutcomeId,
            out CrimeKnowledgeObservation restoredKnowledge), Is.True);
        AssertKnowledgeEqual(current, restoredKnowledge);
        Assert.That(staged.SocialReactions.HistoricalReactions, Has.Count.EqualTo(4));
        Assert.That(staged.SocialReactions.TryGet(first.ReactionId, out SocialReaction restoredFirst), Is.True);
        AssertReactionEqual(first, restoredFirst);
        Assert.That(staged.SocialReactions.TryGet(successor.ReactionId, out SocialReaction restoredSuccessor), Is.True);
        AssertReactionEqual(successor, restoredSuccessor);
        Assert.That(restoredSuccessor.SupersedesReactionId, Is.EqualTo(first.ReactionId));
        Assert.That(staged.SocialReactions.GetCurrentReactions(), Has.Count.EqualTo(3));
        Assert.That(staged.CrimeKnowledge.PersonStore, Is.SameAs(roots.People));
        Assert.That(staged.CrimeKnowledge.OutcomeStore, Is.SameAs(staged.TheftOutcomes));
        Assert.That(staged.SocialReactions.SimulationTime, Is.SameAs(roots.Time));
        Assert.That(staged.InstitutionStore, Is.SameAs(roots.Institutions));
    }

    [Test]
    public void CaptureRequiresTheOriginalVectorAndRejectsAWriteAfterTheToken()
    {
        SimulationRuntime runtime = CreateDailyRuntime();
        PersonId perpetrator = new PersonId("p12e-stale-perpetrator");
        PersonId victim = new PersonId("p12e-stale-victim");
        RegisterPerson(runtime, perpetrator);
        RegisterPerson(runtime, victim);
        AdvanceAndGetToken(runtime, out DailyCaptureEligibilityToken token);

        List<OwnerSectionCensusSnapshot> copied = new List<OwnerSectionCensusSnapshot>(token.OwnerSections);
        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(runtime, token, copied,
            out P12ECrimeSocialAppraisalOwnerSnapshot rejectedCopy,
            out P12ECrimeSocialAppraisalSnapshotFailure failure), Is.False);
        Assert.That(rejectedCopy, Is.Null);

        TheftOutcome outcome = CreateOutcome(perpetrator, victim, token.AbsoluteDay, "stale:after-token");
        Assert.That(runtime.CrimeSocialAppraisal.TheftOutcomes.TryRecord(outcome,
            out CrimeOutcomeStoreFailure outcomeFailure), Is.True, outcomeFailure.ToString());
        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(runtime, token, token.OwnerSections,
            out P12ECrimeSocialAppraisalOwnerSnapshot rejectedStale, out failure), Is.False);
        Assert.That(rejectedStale, Is.Null);
    }

    [Test]
    public void StagingRejectsDanglingTargetsAndSupersessionWithoutChangingRoots()
    {
        StagedRoots roots = CreateStagedRoots(new[] { "p12e-stage-evaluator" }, Array.Empty<string>(), 2L);
        PersonId evaluator = new PersonId("p12e-stage-evaluator");
        SocialSourceReference source = new SocialSourceReference("crime", "stage-source");
        SocialReactionTarget missingPersonTarget = SocialReactionTarget.ForPerson(new PersonId("missing-target"));
        SocialReaction danglingTarget = CreateReaction(evaluator, source, missingPersonTarget,
            SocialPerceivedAttribution.Unknown(), new SocialCognitiveBasis(SocialCognitiveBasisKind.KnownFact),
            SocialReactionValence.Negative, SocialReactionSalience.Low, 1L, null);
        P12ECrimeSocialAppraisalOwnerSnapshot danglingTargetSnapshot = CreateSnapshot(2L,
            Array.Empty<P12ECrimeOutcomeSnapshotRow>(), Array.Empty<P12ECrimeKnowledgeSnapshotRow>(),
            new[] { Copy(danglingTarget) });
        AssertStageFails(roots, danglingTargetSnapshot);

        SocialReactionId missingPredecessor = new SocialReactionId("reaction:missing-predecessor");
        SocialReaction danglingHistory = CreateReaction(evaluator, source,
            SocialReactionTarget.ForPerson(evaluator), SocialPerceivedAttribution.Unknown(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.DirectObservation, "history"),
            SocialReactionValence.Negative, SocialReactionSalience.Low, 1L, missingPredecessor);
        P12ECrimeSocialAppraisalOwnerSnapshot danglingHistorySnapshot = CreateSnapshot(2L,
            Array.Empty<P12ECrimeOutcomeSnapshotRow>(), Array.Empty<P12ECrimeKnowledgeSnapshotRow>(),
            new[] { Copy(danglingHistory) });
        AssertStageFails(roots, danglingHistorySnapshot);
    }

    [Test]
    public void StagingRejectsBadSectionMetadataStableIdsTimelinesAndKnowledgeRoles()
    {
        StagedRoots roots = CreateStagedRoots(
            new[] { "p12e-invalid-perpetrator", "p12e-invalid-victim" }, Array.Empty<string>(), 2L);
        PersonId perpetrator = new PersonId("p12e-invalid-perpetrator");
        PersonId victim = new PersonId("p12e-invalid-victim");
        TheftOutcome validOutcome = CreateOutcome(perpetrator, victim, 1L, "valid-outcome");
        P12ECrimeOutcomeSnapshotRow validOutcomeRow = Copy(validOutcome);

        AssertStageFails(roots, CreateSnapshotWithSections(2L,
            new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, 2, 0, 0L,
                Array.Empty<P12ECrimeOutcomeSnapshotRow>()), EmptyKnowledge(), EmptyReactions()),
            P12ECrimeSocialAppraisalSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(roots, CreateSnapshotWithSections(2L,
            new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, 1, 1, 1L,
                Array.Empty<P12ECrimeOutcomeSnapshotRow>()), EmptyKnowledge(), EmptyReactions()),
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidCardinality);
        AssertStageFails(roots, CreateSnapshotWithSections(2L,
            new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, 1, 0, -1L,
                Array.Empty<P12ECrimeOutcomeSnapshotRow>()), EmptyKnowledge(), EmptyReactions()),
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidRevision);

        P12ECrimeOutcomeSnapshotRow forgedId = new P12ECrimeOutcomeSnapshotRow(
            "forged-outcome-id", perpetrator.Value, victim.Value, validOutcome.LossAmount,
            validOutcome.OccurredAbsoluteDay, validOutcome.OccurrenceKey, validOutcome.OriginDecisionId);
        AssertStageFails(roots, CreateSnapshot(2L, new[] { forgedId },
            Array.Empty<P12ECrimeKnowledgeSnapshotRow>(), Array.Empty<P12ESocialReactionSnapshotRow>()),
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity);

        TheftOutcome futureOutcome = CreateOutcome(perpetrator, victim, 3L, "future-outcome");
        AssertStageFails(roots, CreateSnapshot(2L, new[] { Copy(futureOutcome) },
            Array.Empty<P12ECrimeKnowledgeSnapshotRow>(), Array.Empty<P12ESocialReactionSnapshotRow>()),
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidTimeline);

        CrimeKnowledgeObservation wrongRole = new CrimeKnowledgeObservation(
            perpetrator, validOutcome.OutcomeId, CrimeKnowledgeRole.Victim, true,
            SocialPerceivedAttribution.Unknown(),
            new SocialCognitiveBasis(SocialCognitiveBasisKind.KnownFact, "role-check"), 2L);
        AssertStageFails(roots, CreateSnapshot(2L, new[] { validOutcomeRow },
            new[] { Copy(wrongRole) }, Array.Empty<P12ESocialReactionSnapshotRow>()),
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference);
    }

    [Test]
    public void StagingRejectsMultipleReactionSuccessorsAndStableIdTampering()
    {
        StagedRoots roots = CreateStagedRoots(new[] { "p12e-reaction-evaluator" }, Array.Empty<string>(), 2L);
        PersonId evaluator = new PersonId("p12e-reaction-evaluator");
        SocialSourceReference source = new SocialSourceReference("crime", "successor-source");
        SocialReactionTarget target = SocialReactionTarget.ForPerson(evaluator);
        SocialPerceivedAttribution attribution = SocialPerceivedAttribution.Unknown();
        SocialCognitiveBasis basis = new SocialCognitiveBasis(SocialCognitiveBasisKind.KnownFact, "root");
        SocialReaction root = CreateReaction(evaluator, source, target, attribution, basis,
            SocialReactionValence.Negative, SocialReactionSalience.Low, 0L, null);
        SocialReaction firstSuccessor = CreateReaction(evaluator, source, target, attribution,
            new SocialCognitiveBasis(SocialCognitiveBasisKind.ReceivedInformation, "first"),
            SocialReactionValence.Negative, SocialReactionSalience.Medium, 1L, root.ReactionId);
        SocialReaction secondSuccessor = CreateReaction(evaluator, source, target, attribution,
            new SocialCognitiveBasis(SocialCognitiveBasisKind.ReceivedInformation, "second"),
            SocialReactionValence.Positive, SocialReactionSalience.High, 1L, root.ReactionId);
        List<P12ESocialReactionSnapshotRow> rows = new List<P12ESocialReactionSnapshotRow>
        {
            Copy(root), Copy(firstSuccessor), Copy(secondSuccessor)
        };
        rows.Sort((left, right) => StringComparer.Ordinal.Compare(left.ReactionId, right.ReactionId));
        AssertStageFails(roots, CreateSnapshot(2L, Array.Empty<P12ECrimeOutcomeSnapshotRow>(),
            Array.Empty<P12ECrimeKnowledgeSnapshotRow>(), rows),
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidSupersession);

        P12ESocialReactionSnapshotRow tampered = Copy(root);
        tampered = new P12ESocialReactionSnapshotRow("forged-reaction-id", tampered.EvaluatorPersonId,
            tampered.SourceDomain, tampered.SourceStableId, tampered.TargetKind, tampered.TargetStableId,
            tampered.AttributionKind, tampered.AttributionPersonId, tampered.AttributionInstitutionId,
            tampered.Valence, tampered.Salience, tampered.CognitiveBasisKind,
            tampered.CognitiveBasisReference, tampered.CognitiveBasisPersonId,
            tampered.CognitiveBasisInstitutionId, tampered.CreatedAbsoluteDay, tampered.SupersedesReactionId);
        AssertStageFails(roots, CreateSnapshot(2L, Array.Empty<P12ECrimeOutcomeSnapshotRow>(),
            Array.Empty<P12ECrimeKnowledgeSnapshotRow>(), new[] { tampered }),
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity);
    }

    private SimulationRuntime CreateDailyRuntime()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject instance = new GameObject("p12e-crime-social-snapshot-test");
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

    private static void AdvanceAndGetToken(SimulationRuntime runtime, out DailyCaptureEligibilityToken token)
    {
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
    }

    private static StagedRoots CreateStagedRoots(string[] personIds, string[] institutionIds, long day)
    {
        StagedRoots roots = new StagedRoots(day);
        foreach (string personId in personIds)
            Assert.That(roots.People.TryRegister(new PersonRuntime(new PersonId(personId), day),
                out PersonStoreFailure personFailure), Is.True, personFailure.ToString());
        foreach (string institutionId in institutionIds)
            Assert.That(roots.Institutions.TryRegister(new InstitutionRecord(new InstitutionId(institutionId)),
                out InstitutionFoundationFailure institutionFailure), Is.True, institutionFailure.ToString());
        return roots;
    }

    private static TheftOutcome CreateOutcome(PersonId perpetrator, PersonId victim, long day, string occurrence)
    {
        return new TheftOutcome(TheftOutcomeId.Create(perpetrator, victim, day, occurrence),
            perpetrator, victim, 17, day, occurrence, "decision:opaque");
    }

    private static SocialReaction CreateReaction(
        PersonId evaluator, SocialSourceReference source, SocialReactionTarget target,
        SocialPerceivedAttribution attribution, SocialCognitiveBasis basis,
        SocialReactionValence valence, SocialReactionSalience salience,
        long day, SocialReactionId supersedes)
    {
        SocialReactionId id = SocialReactionId.Create(evaluator, source, target, attribution, basis,
            valence, salience, day, supersedes);
        return new SocialReaction(id, evaluator, source, target, attribution, valence,
            salience, basis, day, supersedes);
    }

    private static void RegisterReaction(SocialReactionStore store, SocialReaction reaction)
    {
        Assert.That(store.TryRecord(reaction, out SocialReactionStoreFailure failure), Is.True, failure.ToString());
    }

    private static P12ECrimeSocialAppraisalOwnerSnapshot CreateSnapshot(
        long day,
        IReadOnlyList<P12ECrimeOutcomeSnapshotRow> outcomes,
        IReadOnlyList<P12ECrimeKnowledgeSnapshotRow> knowledge,
        IReadOnlyList<P12ESocialReactionSnapshotRow> reactions)
    {
        return CreateSnapshotWithSections(day,
            new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, 1, outcomes.Count, outcomes.Count, outcomes),
            new P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, 1, knowledge.Count, knowledge.Count, knowledge),
            new P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow>(
                P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, 1, reactions.Count, reactions.Count, reactions));
    }

    private static P12ECrimeSocialAppraisalOwnerSnapshot CreateSnapshotWithSections(
        long day,
        P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> outcomes,
        P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> knowledge,
        P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> reactions)
    {
        return new P12ECrimeSocialAppraisalOwnerSnapshot(day, outcomes, knowledge, reactions);
    }

    private static P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> EmptyKnowledge() =>
        new P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow>(
            P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, 1, 0, 0L,
            Array.Empty<P12ECrimeKnowledgeSnapshotRow>());

    private static P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> EmptyReactions() =>
        new P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow>(
            P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, 1, 0, 0L,
            Array.Empty<P12ESocialReactionSnapshotRow>());

    private static P12ECrimeOutcomeSnapshotRow Copy(TheftOutcome value)
    {
        return new P12ECrimeOutcomeSnapshotRow(value.OutcomeId.Value,
            value.PerpetratorPersonId.Value, value.VictimPersonId.Value, value.LossAmount,
            value.OccurredAbsoluteDay, value.OccurrenceKey, value.OriginDecisionId);
    }

    private static P12ECrimeKnowledgeSnapshotRow Copy(CrimeKnowledgeObservation value)
    {
        return new P12ECrimeKnowledgeSnapshotRow(value.EvaluatorPersonId.Value, value.OutcomeId.Value,
            (int)value.Role, value.KnowsLoss, (int)value.PerceivedPerpetrator.Kind,
            value.PerceivedPerpetrator.PersonId?.Value, value.PerceivedPerpetrator.InstitutionId?.Value,
            value.KnownInvestigatorPersonId?.Value, value.KnownInvestigatorInstitutionId?.Value,
            (int)value.CognitiveBasis.Kind, value.CognitiveBasis.Reference,
            value.CognitiveBasis.SourcePersonId?.Value, value.CognitiveBasis.SourceInstitutionId?.Value,
            value.ObservedAbsoluteDay);
    }

    private static P12ESocialReactionSnapshotRow Copy(SocialReaction value)
    {
        return new P12ESocialReactionSnapshotRow(value.ReactionId.Value, value.EvaluatorPersonId.Value,
            value.Source.Domain, value.Source.StableId, (int)value.Target.Kind, value.Target.StableId,
            (int)value.PerceivedAttribution.Kind, value.PerceivedAttribution.PersonId?.Value,
            value.PerceivedAttribution.InstitutionId?.Value, (int)value.Valence, (int)value.Salience,
            (int)value.CognitiveBasis.Kind, value.CognitiveBasis.Reference,
            value.CognitiveBasis.SourcePersonId?.Value, value.CognitiveBasis.SourceInstitutionId?.Value,
            value.CreatedAbsoluteDay, value.SupersedesReactionId?.Value);
    }

    private static void AssertStageFails(
        StagedRoots roots, P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        P12ECrimeSocialAppraisalSnapshotFailureCode? expectedCode = null)
    {
        int peopleBefore = roots.People.Persons.Count;
        int institutionsBefore = roots.Institutions.Count;
        Assert.That(snapshot.TryStage(roots.People, roots.Institutions, roots.Time,
            out CrimeSocialAppraisalWorldState staged,
            out P12ECrimeSocialAppraisalSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.Not.Null);
        if (expectedCode.HasValue) Assert.That(failure.Code, Is.EqualTo(expectedCode.Value));
        Assert.That(roots.People.Persons.Count, Is.EqualTo(peopleBefore));
        Assert.That(roots.Institutions.Count, Is.EqualTo(institutionsBefore));
    }

    private static void AssertSection<TRow>(
        P12ECrimeOwnerSnapshotSection<TRow> section, string id, int count, long revision) where TRow : class
    {
        Assert.That(section.SectionId, Is.EqualTo(id));
        Assert.That(section.SchemaVersion, Is.EqualTo(P12CrimeSocialAppraisalCensusProvider.SchemaVersion));
        Assert.That(section.RecordCount, Is.EqualTo(count));
        Assert.That(section.Revision, Is.EqualTo(revision));
        Assert.That(section.Records, Has.Count.EqualTo(count));
    }

    private static string Signature(P12ECrimeSocialAppraisalOwnerSnapshot snapshot)
    {
        List<string> values = new List<string>
        {
            snapshot.CapturedAbsoluteDay.ToString(),
            snapshot.Outcomes.RecordCount + "/" + snapshot.Outcomes.Revision,
            snapshot.Knowledge.RecordCount + "/" + snapshot.Knowledge.Revision,
            snapshot.Reactions.RecordCount + "/" + snapshot.Reactions.Revision
        };
        foreach (P12ECrimeOutcomeSnapshotRow row in snapshot.Outcomes.Records)
            values.Add(string.Join("|", row.OutcomeId, row.PerpetratorPersonId, row.VictimPersonId,
                row.LossAmount, row.OccurredAbsoluteDay, row.OccurrenceKey, row.OriginDecisionId));
        foreach (P12ECrimeKnowledgeSnapshotRow row in snapshot.Knowledge.Records)
            values.Add(string.Join("|", row.EvaluatorPersonId, row.OutcomeId, row.Role, row.KnowsLoss,
                row.AttributionKind, row.AttributionPersonId, row.AttributionInstitutionId,
                row.KnownInvestigatorPersonId, row.KnownInvestigatorInstitutionId,
                row.CognitiveBasisKind, row.CognitiveBasisReference, row.CognitiveBasisPersonId,
                row.CognitiveBasisInstitutionId, row.ObservedAbsoluteDay));
        foreach (P12ESocialReactionSnapshotRow row in snapshot.Reactions.Records)
            values.Add(string.Join("|", row.ReactionId, row.EvaluatorPersonId, row.SourceDomain,
                row.SourceStableId, row.TargetKind, row.TargetStableId, row.AttributionKind,
                row.AttributionPersonId, row.AttributionInstitutionId, row.Valence, row.Salience,
                row.CognitiveBasisKind, row.CognitiveBasisReference, row.CognitiveBasisPersonId,
                row.CognitiveBasisInstitutionId, row.CreatedAbsoluteDay, row.SupersedesReactionId));
        return string.Join("\n", values);
    }

    private static void AssertOutcomeEqual(TheftOutcome expected, TheftOutcome actual)
    {
        Assert.That(actual.OutcomeId, Is.EqualTo(expected.OutcomeId));
        Assert.That(actual.PerpetratorPersonId, Is.EqualTo(expected.PerpetratorPersonId));
        Assert.That(actual.VictimPersonId, Is.EqualTo(expected.VictimPersonId));
        Assert.That(actual.LossAmount, Is.EqualTo(expected.LossAmount));
        Assert.That(actual.OccurredAbsoluteDay, Is.EqualTo(expected.OccurredAbsoluteDay));
        Assert.That(actual.OccurrenceKey, Is.EqualTo(expected.OccurrenceKey));
        Assert.That(actual.OriginDecisionId, Is.EqualTo(expected.OriginDecisionId));
    }

    private static void AssertKnowledgeEqual(CrimeKnowledgeObservation expected, CrimeKnowledgeObservation actual)
    {
        Assert.That(actual.EvaluatorPersonId, Is.EqualTo(expected.EvaluatorPersonId));
        Assert.That(actual.OutcomeId, Is.EqualTo(expected.OutcomeId));
        Assert.That(actual.Role, Is.EqualTo(expected.Role));
        Assert.That(actual.KnowsLoss, Is.EqualTo(expected.KnowsLoss));
        Assert.That(actual.PerceivedPerpetrator, Is.EqualTo(expected.PerceivedPerpetrator));
        Assert.That(actual.KnownInvestigatorPersonId, Is.EqualTo(expected.KnownInvestigatorPersonId));
        Assert.That(actual.KnownInvestigatorInstitutionId, Is.EqualTo(expected.KnownInvestigatorInstitutionId));
        Assert.That(actual.CognitiveBasis, Is.EqualTo(expected.CognitiveBasis));
        Assert.That(actual.ObservedAbsoluteDay, Is.EqualTo(expected.ObservedAbsoluteDay));
        Assert.That(actual.StableKey, Is.EqualTo(expected.StableKey));
    }

    private static void AssertReactionEqual(SocialReaction expected, SocialReaction actual)
    {
        Assert.That(actual.ReactionId, Is.EqualTo(expected.ReactionId));
        Assert.That(actual.EvaluatorPersonId, Is.EqualTo(expected.EvaluatorPersonId));
        Assert.That(actual.Source, Is.EqualTo(expected.Source));
        Assert.That(actual.Target, Is.EqualTo(expected.Target));
        Assert.That(actual.PerceivedAttribution, Is.EqualTo(expected.PerceivedAttribution));
        Assert.That(actual.Valence, Is.EqualTo(expected.Valence));
        Assert.That(actual.Salience, Is.EqualTo(expected.Salience));
        Assert.That(actual.CognitiveBasis, Is.EqualTo(expected.CognitiveBasis));
        Assert.That(actual.CreatedAbsoluteDay, Is.EqualTo(expected.CreatedAbsoluteDay));
        Assert.That(actual.SupersedesReactionId, Is.EqualTo(expected.SupersedesReactionId));
    }

    private sealed class StagedRoots
    {
        internal PersonStore People { get; } = new PersonStore();
        internal InstitutionStore Institutions { get; } = new InstitutionStore();
        internal SimulationTime Time { get; }

        internal StagedRoots(long day)
        {
            Time = new SimulationTime(day);
        }
    }
}
