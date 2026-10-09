using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12ECrimeSocialAppraisalSnapshotFailureCode
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerSectionVector,
    InvalidOwnerValues,
    UnsupportedSchema,
    InvalidRevision,
    InvalidCardinality,
    InvalidIdentity,
    DuplicateIdentity,
    InvalidReference,
    InvalidTimeline,
    InvalidOrdering,
    InvalidSupersession,
    StageFailed
}

internal sealed class P12ECrimeSocialAppraisalSnapshotFailure
{
    internal static readonly P12ECrimeSocialAppraisalSnapshotFailure None =
        new P12ECrimeSocialAppraisalSnapshotFailure(
            P12ECrimeSocialAppraisalSnapshotFailureCode.None, string.Empty);

    internal P12ECrimeSocialAppraisalSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private P12ECrimeSocialAppraisalSnapshotFailure(
        P12ECrimeSocialAppraisalSnapshotFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12ECrimeSocialAppraisalSnapshotFailure Create(
        P12ECrimeSocialAppraisalSnapshotFailureCode code,
        string message)
    {
        return code == P12ECrimeSocialAppraisalSnapshotFailureCode.None
            ? None
            : new P12ECrimeSocialAppraisalSnapshotFailure(code, message);
    }
}

internal sealed class P12ECrimeOutcomeSnapshotRow
{
    internal string OutcomeId { get; }
    internal string PerpetratorPersonId { get; }
    internal string VictimPersonId { get; }
    internal int LossAmount { get; }
    internal long OccurredAbsoluteDay { get; }
    internal string OccurrenceKey { get; }
    internal string OriginDecisionId { get; }

    internal P12ECrimeOutcomeSnapshotRow(
        string outcomeId, string perpetratorPersonId, string victimPersonId,
        int lossAmount, long occurredAbsoluteDay, string occurrenceKey, string originDecisionId)
    {
        OutcomeId = outcomeId;
        PerpetratorPersonId = perpetratorPersonId;
        VictimPersonId = victimPersonId;
        LossAmount = lossAmount;
        OccurredAbsoluteDay = occurredAbsoluteDay;
        OccurrenceKey = occurrenceKey;
        OriginDecisionId = originDecisionId;
    }
}

internal sealed class P12ECrimeKnowledgeSnapshotRow
{
    internal string EvaluatorPersonId { get; }
    internal string OutcomeId { get; }
    internal int Role { get; }
    internal bool KnowsLoss { get; }
    internal int AttributionKind { get; }
    internal string AttributionPersonId { get; }
    internal string AttributionInstitutionId { get; }
    internal string KnownInvestigatorPersonId { get; }
    internal string KnownInvestigatorInstitutionId { get; }
    internal int CognitiveBasisKind { get; }
    internal string CognitiveBasisReference { get; }
    internal string CognitiveBasisPersonId { get; }
    internal string CognitiveBasisInstitutionId { get; }
    internal long ObservedAbsoluteDay { get; }

    internal P12ECrimeKnowledgeSnapshotRow(
        string evaluatorPersonId, string outcomeId, int role, bool knowsLoss,
        int attributionKind, string attributionPersonId, string attributionInstitutionId,
        string knownInvestigatorPersonId, string knownInvestigatorInstitutionId,
        int cognitiveBasisKind, string cognitiveBasisReference,
        string cognitiveBasisPersonId, string cognitiveBasisInstitutionId,
        long observedAbsoluteDay)
    {
        EvaluatorPersonId = evaluatorPersonId;
        OutcomeId = outcomeId;
        Role = role;
        KnowsLoss = knowsLoss;
        AttributionKind = attributionKind;
        AttributionPersonId = attributionPersonId;
        AttributionInstitutionId = attributionInstitutionId;
        KnownInvestigatorPersonId = knownInvestigatorPersonId;
        KnownInvestigatorInstitutionId = knownInvestigatorInstitutionId;
        CognitiveBasisKind = cognitiveBasisKind;
        CognitiveBasisReference = cognitiveBasisReference;
        CognitiveBasisPersonId = cognitiveBasisPersonId;
        CognitiveBasisInstitutionId = cognitiveBasisInstitutionId;
        ObservedAbsoluteDay = observedAbsoluteDay;
    }
}

internal sealed class P12ESocialReactionSnapshotRow
{
    internal string ReactionId { get; }
    internal string EvaluatorPersonId { get; }
    internal string SourceDomain { get; }
    internal string SourceStableId { get; }
    internal int TargetKind { get; }
    internal string TargetStableId { get; }
    internal int AttributionKind { get; }
    internal string AttributionPersonId { get; }
    internal string AttributionInstitutionId { get; }
    internal int Valence { get; }
    internal int Salience { get; }
    internal int CognitiveBasisKind { get; }
    internal string CognitiveBasisReference { get; }
    internal string CognitiveBasisPersonId { get; }
    internal string CognitiveBasisInstitutionId { get; }
    internal long CreatedAbsoluteDay { get; }
    internal string SupersedesReactionId { get; }

    internal P12ESocialReactionSnapshotRow(
        string reactionId, string evaluatorPersonId, string sourceDomain, string sourceStableId,
        int targetKind, string targetStableId, int attributionKind,
        string attributionPersonId, string attributionInstitutionId,
        int valence, int salience, int cognitiveBasisKind, string cognitiveBasisReference,
        string cognitiveBasisPersonId, string cognitiveBasisInstitutionId,
        long createdAbsoluteDay, string supersedesReactionId)
    {
        ReactionId = reactionId;
        EvaluatorPersonId = evaluatorPersonId;
        SourceDomain = sourceDomain;
        SourceStableId = sourceStableId;
        TargetKind = targetKind;
        TargetStableId = targetStableId;
        AttributionKind = attributionKind;
        AttributionPersonId = attributionPersonId;
        AttributionInstitutionId = attributionInstitutionId;
        Valence = valence;
        Salience = salience;
        CognitiveBasisKind = cognitiveBasisKind;
        CognitiveBasisReference = cognitiveBasisReference;
        CognitiveBasisPersonId = cognitiveBasisPersonId;
        CognitiveBasisInstitutionId = cognitiveBasisInstitutionId;
        CreatedAbsoluteDay = createdAbsoluteDay;
        SupersedesReactionId = supersedesReactionId;
    }
}

internal sealed class P12ECrimeOwnerSnapshotSection<TRow> where TRow : class
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<TRow> Records { get; }

    internal P12ECrimeOwnerSnapshotSection(
        string sectionId, int schemaVersion, int recordCount, long revision, IEnumerable<TRow> records)
    {
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        Records = records == null
            ? null
            : new ReadOnlyCollection<TRow>(new List<TRow>(records));
    }
}

/// <summary>Detached capture and private reconstruction for the three existing crime/social owners.</summary>
internal sealed class P12ECrimeSocialAppraisalOwnerSnapshot
{
    internal long CapturedAbsoluteDay { get; }
    internal P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> Outcomes { get; }
    internal P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> Knowledge { get; }
    internal P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> Reactions { get; }

    internal P12ECrimeSocialAppraisalOwnerSnapshot(
        long capturedAbsoluteDay,
        P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow> outcomes,
        P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow> knowledge,
        P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow> reactions)
    {
        CapturedAbsoluteDay = capturedAbsoluteDay;
        Outcomes = outcomes;
        Knowledge = knowledge;
        Reactions = reactions;
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        out P12ECrimeSocialAppraisalSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12ECrimeSocialAppraisalSnapshotFailure.Create(
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires an admitted completed Daily-v1 runtime.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out _)
            || token == null)
        {
            return false;
        }

        return TryCapture(runtime, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        out P12ECrimeSocialAppraisalSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12ECrimeSocialAppraisalSnapshotFailure.Create(
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact completed P12-B token and owner-section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L)
        {
            return false;
        }

        CrimeSocialAppraisalWorldState world = runtime.CrimeSocialAppraisal;
        if (!HasExactWorldOwners(world) || world.SimulationTime.AbsoluteDay != token.AbsoluteDay)
        {
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidCaptureContext,
                "The admitted runtime does not expose the exact CrimeSocialAppraisal owner group at this boundary.", out failure);
        }

        OwnerSectionCensusSnapshot outcomeWitness = Find(sharedOwnerSectionVector,
            P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, out bool duplicateOutcomes);
        OwnerSectionCensusSnapshot knowledgeWitness = Find(sharedOwnerSectionVector,
            P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, out bool duplicateKnowledge);
        OwnerSectionCensusSnapshot reactionWitness = Find(sharedOwnerSectionVector,
            P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, out bool duplicateReactions);

        int outcomeCount = world.TheftOutcomes.Count;
        long outcomeRevision = world.TheftOutcomes.P12CensusRevision;
        int knowledgeCount = world.CrimeKnowledge.Count;
        long knowledgeRevision = world.CrimeKnowledge.P12CensusRevision;
        int reactionCount = world.SocialReactions.Count;
        long reactionRevision = world.SocialReactions.P12CensusRevision;
        if (duplicateOutcomes || duplicateKnowledge || duplicateReactions
            || !Matches(outcomeWitness, P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId,
                world.TheftOutcomes, outcomeCount, outcomeRevision)
            || !Matches(knowledgeWitness, P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId,
                world.CrimeKnowledge, knowledgeCount, knowledgeRevision)
            || !Matches(reactionWitness, P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId,
                world.SocialReactions, reactionCount, reactionRevision))
        {
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOwnerSectionVector,
                "Exactly one Required schema-v1 witness must identify each installed CrimeSocialAppraisal store.", out failure);
        }

        try
        {
            List<P12ECrimeOutcomeSnapshotRow> outcomeRows = new List<P12ECrimeOutcomeSnapshotRow>(outcomeCount);
            foreach (TheftOutcome outcome in world.TheftOutcomes.Outcomes)
                outcomeRows.Add(Copy(outcome));

            List<P12ECrimeKnowledgeSnapshotRow> knowledgeRows = new List<P12ECrimeKnowledgeSnapshotRow>(knowledgeCount);
            foreach (CrimeKnowledgeObservation observation in world.CrimeKnowledge.CurrentObservations)
                knowledgeRows.Add(Copy(observation));

            List<P12ESocialReactionSnapshotRow> reactionRows = new List<P12ESocialReactionSnapshotRow>(reactionCount);
            foreach (SocialReaction reaction in world.SocialReactions.HistoricalReactions)
                reactionRows.Add(Copy(reaction));

            if (outcomeRows.Count != outcomeCount || knowledgeRows.Count != knowledgeCount
                || reactionRows.Count != reactionCount
                || !Matches(outcomeWitness, P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId,
                    world.TheftOutcomes, world.TheftOutcomes.Count, world.TheftOutcomes.P12CensusRevision)
                || !Matches(knowledgeWitness, P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId,
                    world.CrimeKnowledge, world.CrimeKnowledge.Count, world.CrimeKnowledge.P12CensusRevision)
                || !Matches(reactionWitness, P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId,
                    world.SocialReactions, world.SocialReactions.Count, world.SocialReactions.P12CensusRevision)
                || world.TheftOutcomes.Count != outcomeCount || world.TheftOutcomes.P12CensusRevision != outcomeRevision
                || world.CrimeKnowledge.Count != knowledgeCount || world.CrimeKnowledge.P12CensusRevision != knowledgeRevision
                || world.SocialReactions.Count != reactionCount || world.SocialReactions.P12CensusRevision != reactionRevision
                || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
            {
                return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOwnerSectionVector,
                    "The completed token or one of its exact owner stamps changed while values were copied.", out failure);
            }

            P12ECrimeSocialAppraisalOwnerSnapshot candidate = new P12ECrimeSocialAppraisalOwnerSnapshot(
                token.AbsoluteDay,
                new P12ECrimeOwnerSnapshotSection<P12ECrimeOutcomeSnapshotRow>(
                    P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId,
                    P12CrimeSocialAppraisalCensusProvider.SchemaVersion,
                    outcomeCount, outcomeRevision, outcomeRows),
                new P12ECrimeOwnerSnapshotSection<P12ECrimeKnowledgeSnapshotRow>(
                    P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId,
                    P12CrimeSocialAppraisalCensusProvider.SchemaVersion,
                    knowledgeCount, knowledgeRevision, knowledgeRows),
                new P12ECrimeOwnerSnapshotSection<P12ESocialReactionSnapshotRow>(
                    P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId,
                    P12CrimeSocialAppraisalCensusProvider.SchemaVersion,
                    reactionCount, reactionRevision, reactionRows));

            if (!candidate.TryValidate(world.PersonStore, world.InstitutionStore, world.SimulationTime,
                    out failure, out _, out _, out _))
            {
                return false;
            }
            if (!runtime.TryValidateCompletedDailyCaptureToken(token, out _))
            {
                return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOwnerSectionVector,
                    "The completed token became stale before capture finished.", out failure);
            }

            snapshot = candidate;
            failure = P12ECrimeSocialAppraisalSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is NullReferenceException
            || exception is OverflowException)
        {
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOwnerValues,
                "A CrimeSocialAppraisal owner exposed malformed values: " + exception.Message, out failure);
        }
    }

    internal bool TryStage(
        PersonStore stagedPersons,
        InstitutionStore stagedInstitutions,
        SimulationTime stagedTime,
        out CrimeSocialAppraisalWorldState staged,
        out P12ECrimeSocialAppraisalSnapshotFailure failure)
    {
        staged = null;
        if (!TryValidate(stagedPersons, stagedInstitutions, stagedTime, out failure,
                out List<TheftOutcome> outcomes,
                out List<CrimeKnowledgeObservation> knowledge,
                out List<SocialReaction> topologicalReactions))
        {
            return false;
        }

        try
        {
            CrimeSocialAppraisalWorldState candidate = new CrimeSocialAppraisalWorldState(
                stagedPersons, stagedInstitutions, stagedTime);
            if (!candidate.TheftOutcomes.TryRestoreFromP12EOwnerSnapshot(outcomes, Outcomes.Revision)
                || !candidate.CrimeKnowledge.TryRestoreFromP12EOwnerSnapshot(knowledge, Knowledge.Revision)
                || !candidate.SocialReactions.TryRestoreFromP12EOwnerSnapshot(topologicalReactions, Reactions.Revision))
            {
                return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.StageFailed,
                    "A private CrimeSocialAppraisal owner factory rejected the validated snapshot.", out failure);
            }

            staged = candidate;
            failure = P12ECrimeSocialAppraisalSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is NullReferenceException
            || exception is OverflowException)
        {
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.StageFailed,
                "Private CrimeSocialAppraisal reconstruction rejected malformed values: " + exception.Message, out failure);
        }
    }

    private bool TryValidate(
        PersonStore persons,
        InstitutionStore institutions,
        SimulationTime time,
        out P12ECrimeSocialAppraisalSnapshotFailure failure,
        out List<TheftOutcome> outcomes,
        out List<CrimeKnowledgeObservation> knowledge,
        out List<SocialReaction> topologicalReactions)
    {
        failure = P12ECrimeSocialAppraisalSnapshotFailure.Create(
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOwnerValues,
            "The detached CrimeSocialAppraisal snapshot is malformed.");
        outcomes = null;
        knowledge = null;
        topologicalReactions = null;
        if (persons == null || institutions == null || time == null)
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                "Exact staged PersonStore, InstitutionStore, and SimulationTime roots are required.", out failure);
        if (CapturedAbsoluteDay < 0L || time.AbsoluteDay != CapturedAbsoluteDay)
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidTimeline,
                "The staged simulation day must equal the captured boundary day.", out failure);
        if (!ValidateSection(Outcomes, P12CrimeSocialAppraisalCensusProvider.OutcomesSectionId, out failure)
            || !ValidateSection(Knowledge, P12CrimeSocialAppraisalCensusProvider.KnowledgeSectionId, out failure)
            || !ValidateSection(Reactions, P12CrimeSocialAppraisalCensusProvider.ReactionsSectionId, out failure))
            return false;

        try
        {
            outcomes = new List<TheftOutcome>(Outcomes.RecordCount);
            Dictionary<string, TheftOutcome> outcomesById = new Dictionary<string, TheftOutcome>(StringComparer.Ordinal);
            string previousOutcomeId = null;
            foreach (P12ECrimeOutcomeSnapshotRow row in Outcomes.Records)
            {
                if (row == null || !NonEmpty(row.OutcomeId) || !NonEmpty(row.PerpetratorPersonId)
                    || !NonEmpty(row.VictimPersonId) || row.LossAmount <= 0
                    || !NonEmpty(row.OccurrenceKey))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity,
                        "A theft outcome has a missing identity or invalid scalar value.", out failure);
                if (row.OccurredAbsoluteDay < 0L || row.OccurredAbsoluteDay > CapturedAbsoluteDay)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidTimeline,
                        "A theft outcome must be nonnegative and no later than the captured boundary.", out failure);
                if (previousOutcomeId != null && StringComparer.Ordinal.Compare(previousOutcomeId, row.OutcomeId) >= 0)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOrdering,
                        "Theft outcomes must retain the owner's unique ordinal ID order.", out failure);
                previousOutcomeId = row.OutcomeId;

                PersonId perpetrator = new PersonId(row.PerpetratorPersonId);
                PersonId victim = new PersonId(row.VictimPersonId);
                if (!persons.TryGet(perpetrator, out _) || !persons.TryGet(victim, out _))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "A theft outcome endpoint is absent from the exact staged PersonStore.", out failure);
                TheftOutcomeId expectedId = TheftOutcomeId.Create(
                    perpetrator, victim, row.OccurredAbsoluteDay, row.OccurrenceKey);
                if (!string.Equals(expectedId.Value, row.OutcomeId, StringComparison.Ordinal))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity,
                        "A theft outcome ID does not match its deterministic semantic key.", out failure);
                TheftOutcome outcome = new TheftOutcome(expectedId, perpetrator, victim,
                    row.LossAmount, row.OccurredAbsoluteDay, row.OccurrenceKey, row.OriginDecisionId);
                if (outcomesById.ContainsKey(row.OutcomeId))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.DuplicateIdentity,
                        "Theft outcome IDs must be unique.", out failure);
                outcomesById.Add(row.OutcomeId, outcome);
                outcomes.Add(outcome);
            }

            knowledge = new List<CrimeKnowledgeObservation>(Knowledge.RecordCount);
            HashSet<string> knowledgeKeys = new HashSet<string>(StringComparer.Ordinal);
            string previousKnowledgeKey = null;
            foreach (P12ECrimeKnowledgeSnapshotRow row in Knowledge.Records)
            {
                if (row == null || !NonEmpty(row.EvaluatorPersonId) || !NonEmpty(row.OutcomeId)
                    || !Enum.IsDefined(typeof(CrimeKnowledgeRole), row.Role)
                    || !Enum.IsDefined(typeof(SocialPerceivedAttributionKind), row.AttributionKind)
                    || !Enum.IsDefined(typeof(SocialCognitiveBasisKind), row.CognitiveBasisKind))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity,
                        "A crime knowledge row has an invalid identity, enum, or date.", out failure);
                if (row.ObservedAbsoluteDay < 0L || row.ObservedAbsoluteDay > CapturedAbsoluteDay)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidTimeline,
                        "Crime knowledge must be nonnegative and no later than the captured boundary.", out failure);
                if (!outcomesById.TryGetValue(row.OutcomeId, out TheftOutcome outcome))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "Crime knowledge references an absent theft outcome.", out failure);

                PersonId evaluator = new PersonId(row.EvaluatorPersonId);
                if (!persons.TryGet(evaluator, out _))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "Crime knowledge evaluator is absent from the exact staged PersonStore.", out failure);
                if (row.ObservedAbsoluteDay < outcome.OccurredAbsoluteDay)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidTimeline,
                        "Crime knowledge cannot predate its theft outcome.", out failure);

                if (!TryCreateAttribution(row.AttributionKind, row.AttributionPersonId,
                        row.AttributionInstitutionId, persons, institutions,
                        out SocialPerceivedAttribution attribution))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "Crime knowledge attribution has an invalid union or unresolved endpoint.", out failure);
                if (row.KnownInvestigatorPersonId != null && row.KnownInvestigatorInstitutionId != null)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "Crime knowledge cannot name both a Person and Institution investigator.", out failure);
                PersonId investigatorPerson = OptionalPerson(row.KnownInvestigatorPersonId);
                InstitutionId investigatorInstitution = OptionalInstitution(row.KnownInvestigatorInstitutionId);
                if ((investigatorPerson != null && !persons.TryGet(investigatorPerson, out _))
                    || (investigatorInstitution != null && !institutions.TryGet(investigatorInstitution, out _)))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "A known investigator is absent from the exact staged owner roots.", out failure);
                if ((row.CognitiveBasisPersonId != null && row.CognitiveBasisInstitutionId != null)
                    || !TryCreateBasis(row.CognitiveBasisKind, row.CognitiveBasisReference,
                        row.CognitiveBasisPersonId, row.CognitiveBasisInstitutionId,
                        persons, institutions, out SocialCognitiveBasis basis))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "Crime knowledge cognitive provenance has an invalid union or unresolved endpoint.", out failure);
                if (!row.KnowsLoss && row.AttributionKind != (int)SocialPerceivedAttributionKind.NotApplicable)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity,
                        "Perceived perpetrator attribution requires knowledge of the loss.", out failure);

                CrimeKnowledgeRole role = (CrimeKnowledgeRole)row.Role;
                if ((role == CrimeKnowledgeRole.Victim && evaluator != outcome.VictimPersonId)
                    || (role == CrimeKnowledgeRole.Perpetrator && evaluator != outcome.PerpetratorPersonId))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "Crime knowledge role does not match the factual outcome endpoint.", out failure);

                CrimeKnowledgeObservation observation = new CrimeKnowledgeObservation(
                    evaluator, outcome.OutcomeId, role, row.KnowsLoss, attribution, basis,
                    row.ObservedAbsoluteDay, investigatorPerson, investigatorInstitution);
                string key = KnowledgeKey(observation.EvaluatorPersonId, observation.OutcomeId);
                if (!knowledgeKeys.Add(key))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.DuplicateIdentity,
                        "Only one current knowledge row may exist for each evaluator/outcome key.", out failure);
                if (previousKnowledgeKey != null && StringComparer.Ordinal.Compare(previousKnowledgeKey, key) >= 0)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOrdering,
                        "Current crime knowledge rows must retain the owner's deterministic key order.", out failure);
                previousKnowledgeKey = key;
                knowledge.Add(observation);
            }

            Dictionary<string, SocialReaction> reactionsById = new Dictionary<string, SocialReaction>(StringComparer.Ordinal);
            string previousReactionId = null;
            foreach (P12ESocialReactionSnapshotRow row in Reactions.Records)
            {
                if (row == null || !NonEmpty(row.ReactionId) || !NonEmpty(row.EvaluatorPersonId)
                    || !NonEmpty(row.SourceDomain) || !NonEmpty(row.SourceStableId)
                    || !Enum.IsDefined(typeof(SocialReactionTargetKind), row.TargetKind)
                    || !NonEmpty(row.TargetStableId)
                    || !Enum.IsDefined(typeof(SocialReactionValence), row.Valence)
                    || !Enum.IsDefined(typeof(SocialReactionSalience), row.Salience)
                    || !Enum.IsDefined(typeof(SocialCognitiveBasisKind), row.CognitiveBasisKind))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity,
                        "A social reaction row has an invalid identity, enum, or date.", out failure);
                if (row.CreatedAbsoluteDay < 0L || row.CreatedAbsoluteDay > CapturedAbsoluteDay)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidTimeline,
                        "A social reaction must be nonnegative and no later than the captured boundary.", out failure);
                if (previousReactionId != null && StringComparer.Ordinal.Compare(previousReactionId, row.ReactionId) >= 0)
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOrdering,
                        "Historical social reactions must retain the owner's unique ordinal ID order.", out failure);
                previousReactionId = row.ReactionId;

                PersonId evaluator = new PersonId(row.EvaluatorPersonId);
                if (!persons.TryGet(evaluator, out _))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "A social reaction evaluator is absent from the exact staged PersonStore.", out failure);
                if (!TryCreateTarget(row.TargetKind, row.TargetStableId, persons, institutions, outcomesById,
                        out SocialReactionTarget target)
                    || !TryCreateAttribution(row.AttributionKind, row.AttributionPersonId,
                        row.AttributionInstitutionId, persons, institutions,
                        out SocialPerceivedAttribution reactionAttribution)
                    || !TryCreateBasis(row.CognitiveBasisKind, row.CognitiveBasisReference,
                        row.CognitiveBasisPersonId, row.CognitiveBasisInstitutionId,
                        persons, institutions, out SocialCognitiveBasis reactionBasis))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidReference,
                        "A social reaction target or typed provenance endpoint is unresolved.", out failure);

                SocialSourceReference source = new SocialSourceReference(row.SourceDomain, row.SourceStableId);
                SocialReactionId supersedes = row.SupersedesReactionId == null
                    ? null : new SocialReactionId(row.SupersedesReactionId);
                SocialReactionId expectedId = SocialReactionId.Create(evaluator, source, target,
                    reactionAttribution, reactionBasis, (SocialReactionValence)row.Valence,
                    (SocialReactionSalience)row.Salience, row.CreatedAbsoluteDay, supersedes);
                if (!string.Equals(expectedId.Value, row.ReactionId, StringComparison.Ordinal))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidIdentity,
                        "A social reaction ID does not match its deterministic semantic key.", out failure);

                SocialReaction reaction = new SocialReaction(expectedId, evaluator, source, target,
                    reactionAttribution, (SocialReactionValence)row.Valence,
                    (SocialReactionSalience)row.Salience, reactionBasis,
                    row.CreatedAbsoluteDay, supersedes);
                if (reactionsById.ContainsKey(row.ReactionId))
                    return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.DuplicateIdentity,
                        "Social reaction IDs must be unique.", out failure);
                reactionsById.Add(row.ReactionId, reaction);
            }

            if (!TryOrderAndValidateSupersession(reactionsById, out topologicalReactions))
                return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidSupersession,
                    "Social reaction supersession history has dangling, mismatched, reused, future, or cyclic links.", out failure);

            failure = P12ECrimeSocialAppraisalSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is NullReferenceException
            || exception is OverflowException)
        {
            outcomes = null;
            knowledge = null;
            topologicalReactions = null;
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOwnerValues,
                "CrimeSocialAppraisal facts failed domain construction: " + exception.Message, out failure);
        }
    }

    private bool ValidateSection<TRow>(
        P12ECrimeOwnerSnapshotSection<TRow> section,
        string expectedId,
        out P12ECrimeSocialAppraisalSnapshotFailure failure) where TRow : class
    {
        if (section == null || !string.Equals(section.SectionId, expectedId, StringComparison.Ordinal))
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidOwnerSectionVector,
                "A required CrimeSocialAppraisal section is missing or has the wrong identity.", out failure);
        if (section.SchemaVersion != P12CrimeSocialAppraisalCensusProvider.SchemaVersion)
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.UnsupportedSchema,
                "The CrimeSocialAppraisal section schema is unsupported.", out failure);
        if (section.Revision < 0L)
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidRevision,
                "A CrimeSocialAppraisal local revision cannot be negative.", out failure);
        if (section.RecordCount < 0 || section.Records == null || section.Records.Count != section.RecordCount)
            return Fail(P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidCardinality,
                "A CrimeSocialAppraisal section cardinality does not match its rows.", out failure);
        failure = P12ECrimeSocialAppraisalSnapshotFailure.None;
        return true;
    }

    private static bool HasExactWorldOwners(CrimeSocialAppraisalWorldState world)
    {
        return world != null && world.PersonStore != null && world.InstitutionStore != null
            && world.SimulationTime != null && world.TheftOutcomes != null
            && world.CrimeKnowledge != null && world.SocialReactions != null && world.Integration != null
            && ReferenceEquals(world.TheftOutcomes.PersonStore, world.PersonStore)
            && ReferenceEquals(world.TheftOutcomes.SimulationTime, world.SimulationTime)
            && ReferenceEquals(world.CrimeKnowledge.PersonStore, world.PersonStore)
            && ReferenceEquals(world.CrimeKnowledge.OutcomeStore, world.TheftOutcomes)
            && ReferenceEquals(world.CrimeKnowledge.SimulationTime, world.SimulationTime)
            && ReferenceEquals(world.CrimeKnowledge.InstitutionStoreForWorldBoundary, world.InstitutionStore)
            && ReferenceEquals(world.SocialReactions.PersonStore, world.PersonStore)
            && ReferenceEquals(world.SocialReactions.SimulationTime, world.SimulationTime);
    }

    private static P12ECrimeOutcomeSnapshotRow Copy(TheftOutcome value)
    {
        return value == null ? null : new P12ECrimeOutcomeSnapshotRow(
            value.OutcomeId?.Value, value.PerpetratorPersonId?.Value, value.VictimPersonId?.Value,
            value.LossAmount, value.OccurredAbsoluteDay, value.OccurrenceKey, value.OriginDecisionId);
    }

    private static P12ECrimeKnowledgeSnapshotRow Copy(CrimeKnowledgeObservation value)
    {
        return value == null ? null : new P12ECrimeKnowledgeSnapshotRow(
            value.EvaluatorPersonId?.Value, value.OutcomeId?.Value, (int)value.Role, value.KnowsLoss,
            value.PerceivedPerpetrator == null ? -1 : (int)value.PerceivedPerpetrator.Kind,
            value.PerceivedPerpetrator?.PersonId?.Value, value.PerceivedPerpetrator?.InstitutionId?.Value,
            value.KnownInvestigatorPersonId?.Value, value.KnownInvestigatorInstitutionId?.Value,
            value.CognitiveBasis == null ? -1 : (int)value.CognitiveBasis.Kind,
            value.CognitiveBasis?.Reference, value.CognitiveBasis?.SourcePersonId?.Value,
            value.CognitiveBasis?.SourceInstitutionId?.Value, value.ObservedAbsoluteDay);
    }

    private static P12ESocialReactionSnapshotRow Copy(SocialReaction value)
    {
        return value == null ? null : new P12ESocialReactionSnapshotRow(
            value.ReactionId?.Value, value.EvaluatorPersonId?.Value,
            value.Source?.Domain, value.Source?.StableId,
            value.Target == null ? -1 : (int)value.Target.Kind, value.Target?.StableId,
            value.PerceivedAttribution == null ? -1 : (int)value.PerceivedAttribution.Kind,
            value.PerceivedAttribution?.PersonId?.Value, value.PerceivedAttribution?.InstitutionId?.Value,
            (int)value.Valence, (int)value.Salience,
            value.CognitiveBasis == null ? -1 : (int)value.CognitiveBasis.Kind,
            value.CognitiveBasis?.Reference, value.CognitiveBasis?.SourcePersonId?.Value,
            value.CognitiveBasis?.SourceInstitutionId?.Value,
            value.CreatedAbsoluteDay, value.SupersedesReactionId?.Value);
    }

    private static bool TryCreateAttribution(
        int kind, string personId, string institutionId,
        PersonStore persons, InstitutionStore institutions,
        out SocialPerceivedAttribution attribution)
    {
        attribution = null;
        if (!Enum.IsDefined(typeof(SocialPerceivedAttributionKind), kind)) return false;
        switch ((SocialPerceivedAttributionKind)kind)
        {
            case SocialPerceivedAttributionKind.NotApplicable:
                if (personId != null || institutionId != null) return false;
                attribution = SocialPerceivedAttribution.NotApplicable();
                return true;
            case SocialPerceivedAttributionKind.Unknown:
                if (personId != null || institutionId != null) return false;
                attribution = SocialPerceivedAttribution.Unknown();
                return true;
            case SocialPerceivedAttributionKind.BelievedPerson:
                if (!NonEmpty(personId) || institutionId != null) return false;
                PersonId person = new PersonId(personId);
                if (!persons.TryGet(person, out _)) return false;
                attribution = SocialPerceivedAttribution.BelievedPerson(person);
                return true;
            case SocialPerceivedAttributionKind.BelievedInstitution:
                if (!NonEmpty(institutionId) || personId != null) return false;
                InstitutionId institution = new InstitutionId(institutionId);
                if (!institutions.TryGet(institution, out _)) return false;
                attribution = SocialPerceivedAttribution.BelievedInstitution(institution);
                return true;
            default:
                return false;
        }
    }

    private static bool TryCreateBasis(
        int kind, string reference, string personId, string institutionId,
        PersonStore persons, InstitutionStore institutions,
        out SocialCognitiveBasis basis)
    {
        basis = null;
        if (!Enum.IsDefined(typeof(SocialCognitiveBasisKind), kind)
            || (personId != null && institutionId != null)) return false;
        PersonId person = OptionalPerson(personId);
        InstitutionId institution = OptionalInstitution(institutionId);
        if ((person != null && !persons.TryGet(person, out _))
            || (institution != null && !institutions.TryGet(institution, out _))) return false;
        basis = new SocialCognitiveBasis((SocialCognitiveBasisKind)kind, reference, person, institution);
        return true;
    }

    private static bool TryCreateTarget(
        int kind, string stableId,
        PersonStore persons, InstitutionStore institutions,
        IReadOnlyDictionary<string, TheftOutcome> outcomes,
        out SocialReactionTarget target)
    {
        target = null;
        if (!Enum.IsDefined(typeof(SocialReactionTargetKind), kind) || !NonEmpty(stableId)) return false;
        switch ((SocialReactionTargetKind)kind)
        {
            case SocialReactionTargetKind.TheftOutcome:
                if (!outcomes.ContainsKey(stableId)) return false;
                target = SocialReactionTarget.ForTheftOutcome(stableId);
                return true;
            case SocialReactionTargetKind.Person:
                PersonId person = new PersonId(stableId);
                if (!persons.TryGet(person, out _)) return false;
                target = SocialReactionTarget.ForPerson(person);
                return true;
            case SocialReactionTargetKind.Institution:
                InstitutionId institution = new InstitutionId(stableId);
                if (!institutions.TryGet(institution, out _)) return false;
                target = SocialReactionTarget.ForInstitution(institution);
                return true;
            default:
                return false;
        }
    }

    private static bool TryOrderAndValidateSupersession(
        IReadOnlyDictionary<string, SocialReaction> reactionsById,
        out List<SocialReaction> ordered)
    {
        ordered = new List<SocialReaction>();
        Dictionary<string, string> successorById = new Dictionary<string, string>(StringComparer.Ordinal);
        SortedSet<string> roots = new SortedSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, SocialReaction> item in reactionsById)
        {
            SocialReaction reaction = item.Value;
            if (reaction.SupersedesReactionId == null)
            {
                roots.Add(item.Key);
                continue;
            }
            string predecessorId = reaction.SupersedesReactionId.Value;
            if (!reactionsById.TryGetValue(predecessorId, out SocialReaction predecessor)
                || reaction.CreatedAbsoluteDay < predecessor.CreatedAbsoluteDay
                || reaction.EvaluatorPersonId != predecessor.EvaluatorPersonId
                || !reaction.Source.Equals(predecessor.Source)
                || !reaction.Target.Equals(predecessor.Target)
                || successorById.ContainsKey(predecessorId))
                return false;
            successorById.Add(predecessorId, item.Key);
        }

        Queue<string> pending = new Queue<string>(roots);
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            string id = pending.Dequeue();
            if (!visited.Add(id) || !reactionsById.TryGetValue(id, out SocialReaction reaction)) return false;
            ordered.Add(reaction);
            if (successorById.TryGetValue(id, out string successor)) pending.Enqueue(successor);
        }
        return visited.Count == reactionsById.Count;
    }

    private static string KnowledgeKey(PersonId evaluator, TheftOutcomeId outcome)
    {
        return evaluator.Value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + evaluator.Value
            + outcome.Value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + outcome.Value;
    }

    private static PersonId OptionalPerson(string value) => value == null ? null : new PersonId(value);
    private static InstitutionId OptionalInstitution(string value) => value == null ? null : new InstitutionId(value);
    private static bool NonEmpty(string value) => !string.IsNullOrWhiteSpace(value);

    private static OwnerSectionCensusSnapshot Find(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector, string sectionId, out bool duplicate)
    {
        OwnerSectionCensusSnapshot result = null;
        duplicate = false;
        foreach (OwnerSectionCensusSnapshot section in vector)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
            if (result != null) duplicate = true;
            result = section;
        }
        return result;
    }

    private static bool Matches(
        OwnerSectionCensusSnapshot witness, string sectionId, object owner, int count, long revision)
    {
        return witness != null && owner != null
            && string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
            && witness.SchemaVersion == P12CrimeSocialAppraisalCensusProvider.SchemaVersion
            && witness.Role == OwnerSectionRole.Required
            && ReferenceEquals(witness.OwnerInstanceIdentity, owner)
            && count >= 0 && revision >= 0L
            && witness.Cardinality == count && witness.Revision == revision;
    }

    private static bool Fail(
        P12ECrimeSocialAppraisalSnapshotFailureCode code,
        string message,
        out P12ECrimeSocialAppraisalSnapshotFailure failure)
    {
        failure = P12ECrimeSocialAppraisalSnapshotFailure.Create(code, message);
        return false;
    }
}
