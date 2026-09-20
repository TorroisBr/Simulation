using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;

public sealed class PoliticalLegitimacyDecisionFoundationTests
{
    [Test]
    public void LegitimacyIsPureAndKeepsTruthKnowledgeAndRecognitionSeparate()
    {
        PoliticalLegitimacyPolicy policy = CreatePolicy();
        PoliticalLegitimacyInputs inputs = new PoliticalLegitimacyInputs(
            new PersonId("person.candidate"),
            true,
            true,
            true,
            PoliticalClaimRecognitionState.Recognized,
            3,
            1);

        PoliticalLegitimacyAssessment first = PoliticalLegitimacy.Assess(inputs, policy);
        PoliticalLegitimacyAssessment second = PoliticalLegitimacy.Assess(inputs, policy);

        Assert.That(first.Score, Is.EqualTo(24L));
        Assert.That(first.MeetsThreshold, Is.True);
        Assert.That(second.Score, Is.EqualTo(first.Score));
        Assert.That(typeof(PoliticalLegitimacyInputs).GetProperty("PoliticalClaim") , Is.Null);
        Assert.That(typeof(PoliticalLegitimacyAssessment).GetProperty("PoliticalClaimStore"), Is.Null);
        Assert.That(typeof(PoliticalLegitimacy).GetMethods(), Has.None.Matches<System.Reflection.MethodInfo>(method => method.Name.StartsWith("Set", StringComparison.Ordinal)));
    }

    [Test]
    public void LegitimacyWeightsApplyCurrentFlagsRecognitionAndSupportOpposition()
    {
        PoliticalLegitimacyPolicy policy = new PoliticalLegitimacyPolicy(
            10L, 5L, 7L, 20L, 4L, 0L, -8L, 2L, -3L, 25L);
        PoliticalLegitimacyAssessment recognized = PoliticalLegitimacy.Assess(
            new PoliticalLegitimacyInputs(
                new PersonId("person.a"), true, true, true,
                PoliticalClaimRecognitionState.Recognized, 2, 1), policy);
        PoliticalLegitimacyAssessment contested = PoliticalLegitimacy.Assess(
            new PoliticalLegitimacyInputs(
                new PersonId("person.b"), true, true, true,
                PoliticalClaimRecognitionState.Contested, 2, 1), policy);
        PoliticalLegitimacyAssessment rejected = PoliticalLegitimacy.Assess(
            new PoliticalLegitimacyInputs(
                new PersonId("person.c"), true, true, true,
                PoliticalClaimRecognitionState.Rejected, 2, 1), policy);

        Assert.That(recognized.Score, Is.EqualTo(43L));
        Assert.That(contested.Score, Is.EqualTo(27L));
        Assert.That(rejected.Score, Is.EqualTo(15L));
        Assert.That(recognized.MeetsThreshold, Is.True);
        Assert.That(rejected.MeetsThreshold, Is.False);
    }

    [Test]
    public void CandidateInputsRejectInvalidCountsAndRecognitionValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PoliticalLegitimacyInputs(
            new PersonId("person.invalid"), true, true, true,
            PoliticalClaimRecognitionState.Unrecognized, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PoliticalLegitimacyInputs(
            new PersonId("person.invalid"), true, true, true,
            PoliticalClaimRecognitionState.Unrecognized, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PoliticalLegitimacyInputs(
            new PersonId("person.invalid"), true, true, true,
            (PoliticalClaimRecognitionState)99, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PoliticalLegitimacy.Assess(null, CreatePolicy()));
    }

    [Test]
    public void CandidateFingerprintIsCanonicalForSeveralCandidates()
    {
        PersonId first = new PersonId("person.zed");
        PersonId second = new PersonId("person.anna");
        PersonId third = new PersonId("person.middle");

        string fromUnsorted = PoliticalDecisionRecord.BuildCandidateFingerprint(new[] { first, second, third });
        string fromSorted = PoliticalDecisionRecord.BuildCandidateFingerprint(new[] { second, third, first });

        Assert.That(fromUnsorted, Is.EqualTo(fromSorted));
        Assert.That(fromUnsorted, Is.EqualTo("candidates:3:11:person.anna;13:person.middle;10:person.zed;"));
        Assert.Throws<ArgumentException>(() => PoliticalDecisionRecord.BuildCandidateFingerprint(
            new[] { first, new PersonId("person.zed") }));
    }

    [Test]
    public void DecisionRecordCapturesTypedKnowledgeAndStaleSafeMetadataReadOnly()
    {
        PoliticalDecisionRecord record = CreateDecision("decision.one", 2L, 4L);

        Assert.That(record.Decider, Is.EqualTo(PoliticalKnowledgeHolder.ForInstitution(new InstitutionId("institution.court"))));
        Assert.That(record.DecisionKind, Is.EqualTo(PoliticalDecisionKind.SuccessionSelection));
        Assert.That(record.CandidatePersonIds, Is.TypeOf<ReadOnlyCollection<PersonId>>());
        Assert.That(record.CandidatePersonIds[0].Value, Is.EqualTo("person.a"));
        Assert.That(record.Outcome.Kind, Is.EqualTo(PoliticalDecisionOutcomeKind.CandidateSelected));
        Assert.That(record.Outcome.SelectedCandidatePersonId.Value, Is.EqualTo("person.b"));
        Assert.That(record.IsStaleFor(4L, 7L, 3L), Is.False);
        Assert.That(record.IsStaleFor(5L, 7L, 3L), Is.True);
        Assert.That(record.IsStaleFor(4L, 8L, 3L), Is.True);
        Assert.That(record.IsStaleFor(4L, 7L, 4L), Is.True);
        Assert.Throws<NotSupportedException>(() => ((IList<PersonId>)record.CandidatePersonIds).Add(new PersonId("person.c")));
    }

    [Test]
    public void DecisionRejectsInvalidOrStaleShapedInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new PoliticalDecisionRecord(
            new PoliticalDecisionId("decision.invalid"),
            PoliticalKnowledgeHolder.ForPerson(new PersonId("person.decider")),
            PoliticalDecisionKind.SuccessionSelection,
            null,
            PoliticalDecisionOutcome.None(),
            0L, 0L, null, null, 0L, 0L));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateDecision("decision.invalid-day", -1L, 0L));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateDecision("decision.invalid-order", 4L, 2L));
        Assert.Throws<ArgumentException>(() => new PoliticalDecisionRecord(
            new PoliticalDecisionId("decision.invalid-selection"),
            PoliticalKnowledgeHolder.ForPerson(new PersonId("person.decider")),
            PoliticalDecisionKind.SuccessionSelection,
            new[] { new PersonId("person.a") },
            PoliticalDecisionOutcome.Candidate(new PersonId("person.not-listed")),
            0L, 0L, null, null, 0L, 0L));
    }

    [Test]
    public void DecisionStoreRetainsDeterministicHistoryAndCloneIsIndependent()
    {
        PoliticalDecisionStore original = new PoliticalDecisionStore();
        PoliticalDecisionRecord later = CreateDecision("decision.z", 1L, 5L);
        PoliticalDecisionRecord earlier = CreateDecision("decision.a", 1L, 3L);

        Assert.That(original.TryRegister(later, out PoliticalDecisionFailure laterFailure), Is.True, laterFailure.ToString());
        Assert.That(original.TryRegister(earlier, out PoliticalDecisionFailure earlierFailure), Is.True, earlierFailure.ToString());
        Assert.That(original.Records[0].DecisionId.Value, Is.EqualTo("decision.a"));
        Assert.That(original.Records[1].DecisionId.Value, Is.EqualTo("decision.z"));
        Assert.That(original.TryRegister(earlier, out PoliticalDecisionFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(PoliticalDecisionFailureCode.DuplicateDecisionId));

        PoliticalDecisionStore clone = original.Clone();
        Assert.That(clone.Revision, Is.EqualTo(original.Revision));
        Assert.That(clone.TryRegister(CreateDecision("decision.clone", 1L, 6L), out _), Is.True);
        Assert.That(original.Count, Is.EqualTo(2));
        Assert.That(clone.Count, Is.EqualTo(3));
        Assert.That(original.Records, Is.TypeOf<ReadOnlyCollection<PoliticalDecisionRecord>>());
        Assert.Throws<NotSupportedException>(() => ((IList<PoliticalDecisionRecord>)original.Records).Add(later));
    }

    [Test]
    public void DecisionOutcomeIsProposalMetadataAndDoesNotExecuteRecognitionOrSuccession()
    {
        PoliticalDecisionOutcome none = PoliticalDecisionOutcome.None();
        PoliticalDecisionOutcome candidate = PoliticalDecisionOutcome.Candidate(new PersonId("person.a"));
        PoliticalDecisionOutcome claim = PoliticalDecisionOutcome.RecognizeClaim(new PoliticalClaimId("claim.a"));

        Assert.That(none.HasSelection, Is.False);
        Assert.That(candidate.HasSelection, Is.True);
        Assert.That(claim.HasSelection, Is.True);
        Assert.That(typeof(PoliticalDecisionStore).GetMethods(), Has.None.Matches<System.Reflection.MethodInfo>(method =>
            method.Name.Contains("Apply") || method.Name.Contains("Execute") || method.Name.Contains("Recogniz")));
    }

    private static PoliticalLegitimacyPolicy CreatePolicy()
    {
        return new PoliticalLegitimacyPolicy(5L, 4L, 3L, 10L, 2L, 0L, -5L, 1L, -1L, 12L);
    }

    private static PoliticalDecisionRecord CreateDecision(string id, long observedDay, long decisionDay)
    {
        return new PoliticalDecisionRecord(
            new PoliticalDecisionId(id),
            PoliticalKnowledgeHolder.ForInstitution(new InstitutionId("institution.court")),
            PoliticalDecisionKind.SuccessionSelection,
            new[] { new PersonId("person.b"), new PersonId("person.a") },
            PoliticalDecisionOutcome.Candidate(new PersonId("person.b")),
            observedDay,
            decisionDay,
            new[] { "evidence.z", "evidence.a" },
            new[] { "knowledge.z", "knowledge.a" },
            7L,
            3L);
    }
}
