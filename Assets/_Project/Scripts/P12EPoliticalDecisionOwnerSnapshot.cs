using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12EPoliticalDecisionSnapshotFailureCode
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
    InvalidOutcome,
    StageFailed
}

internal sealed class P12EPoliticalDecisionSnapshotFailure
{
    internal static readonly P12EPoliticalDecisionSnapshotFailure None =
        new P12EPoliticalDecisionSnapshotFailure(P12EPoliticalDecisionSnapshotFailureCode.None, string.Empty);

    internal P12EPoliticalDecisionSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private P12EPoliticalDecisionSnapshotFailure(
        P12EPoliticalDecisionSnapshotFailureCode code,
        string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12EPoliticalDecisionSnapshotFailure Create(
        P12EPoliticalDecisionSnapshotFailureCode code,
        string message)
    {
        return code == P12EPoliticalDecisionSnapshotFailureCode.None
            ? None
            : new P12EPoliticalDecisionSnapshotFailure(code, message);
    }
}

/// <summary>Detached immutable value for one existing PoliticalDecisionRecord.</summary>
internal sealed class P12EPoliticalDecisionSnapshotRow
{
    internal string DecisionId { get; }
    internal int DeciderKind { get; }
    internal string DeciderId { get; }
    internal int DecisionKind { get; }
    internal IReadOnlyList<string> CandidatePersonIds { get; }
    internal int OutcomeKind { get; }
    internal string SelectedCandidatePersonId { get; }
    internal string ReferencedClaimId { get; }
    internal long ObservedAbsoluteDay { get; }
    internal long DecisionAbsoluteDay { get; }
    internal IReadOnlyList<string> EvidenceReferences { get; }
    internal IReadOnlyList<string> KnowledgeReferences { get; }
    internal long ExpectedWorldRevision { get; }
    internal long ExpectedKnowledgeRevision { get; }
    internal string OfficeId { get; }
    internal string RecognizingInstitutionId { get; }

    internal P12EPoliticalDecisionSnapshotRow(
        string decisionId,
        int deciderKind,
        string deciderId,
        int decisionKind,
        IEnumerable<string> candidatePersonIds,
        int outcomeKind,
        string selectedCandidatePersonId,
        string referencedClaimId,
        long observedAbsoluteDay,
        long decisionAbsoluteDay,
        IEnumerable<string> evidenceReferences,
        IEnumerable<string> knowledgeReferences,
        long expectedWorldRevision,
        long expectedKnowledgeRevision,
        string officeId,
        string recognizingInstitutionId)
    {
        DecisionId = decisionId;
        DeciderKind = deciderKind;
        DeciderId = deciderId;
        DecisionKind = decisionKind;
        CandidatePersonIds = Copy(candidatePersonIds);
        OutcomeKind = outcomeKind;
        SelectedCandidatePersonId = selectedCandidatePersonId;
        ReferencedClaimId = referencedClaimId;
        ObservedAbsoluteDay = observedAbsoluteDay;
        DecisionAbsoluteDay = decisionAbsoluteDay;
        EvidenceReferences = Copy(evidenceReferences);
        KnowledgeReferences = Copy(knowledgeReferences);
        ExpectedWorldRevision = expectedWorldRevision;
        ExpectedKnowledgeRevision = expectedKnowledgeRevision;
        OfficeId = officeId;
        RecognizingInstitutionId = recognizingInstitutionId;
    }

    internal P12EPoliticalDecisionSnapshotRow Copy()
    {
        return new P12EPoliticalDecisionSnapshotRow(DecisionId, DeciderKind, DeciderId,
            DecisionKind, CandidatePersonIds, OutcomeKind, SelectedCandidatePersonId,
            ReferencedClaimId, ObservedAbsoluteDay, DecisionAbsoluteDay, EvidenceReferences,
            KnowledgeReferences, ExpectedWorldRevision, ExpectedKnowledgeRevision,
            OfficeId, RecognizingInstitutionId);
    }

    private static IReadOnlyList<string> Copy(IEnumerable<string> values)
    {
        if (values == null) return null;
        return new ReadOnlyCollection<string>(new List<string>(values));
    }
}

internal sealed class P12EPoliticalDecisionSnapshotSection
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal int RecordCount { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12EPoliticalDecisionSnapshotRow> Records { get; }

    internal P12EPoliticalDecisionSnapshotSection(
        string sectionId,
        int schemaVersion,
        int recordCount,
        long revision,
        IEnumerable<P12EPoliticalDecisionSnapshotRow> records)
    {
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        RecordCount = recordCount;
        Revision = revision;
        if (records == null)
        {
            Records = null;
            return;
        }

        List<P12EPoliticalDecisionSnapshotRow> detached = new List<P12EPoliticalDecisionSnapshotRow>();
        foreach (P12EPoliticalDecisionSnapshotRow row in records) detached.Add(row?.Copy());
        Records = new ReadOnlyCollection<P12EPoliticalDecisionSnapshotRow>(detached);
    }
}

/// <summary>Detached export and private staged reconstruction for the existing decision census section.</summary>
internal sealed class P12EPoliticalDecisionOwnerSnapshot
{
    internal const int CurrentSchemaVersion = PoliticalDecisionStoreCensusProvider.SchemaVersion;

    internal long CapturedAbsoluteDay { get; }
    internal P12EPoliticalDecisionSnapshotSection Decisions { get; }

    internal P12EPoliticalDecisionOwnerSnapshot(
        long capturedAbsoluteDay,
        P12EPoliticalDecisionSnapshotSection decisions)
    {
        CapturedAbsoluteDay = capturedAbsoluteDay;
        Decisions = decisions;
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EPoliticalDecisionOwnerSnapshot snapshot,
        out P12EPoliticalDecisionSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalDecisionSnapshotFailure.Create(
            P12EPoliticalDecisionSnapshotFailureCode.InvalidCaptureContext,
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
        out P12EPoliticalDecisionOwnerSnapshot snapshot,
        out P12EPoliticalDecisionSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalDecisionSnapshotFailure.Create(
            P12EPoliticalDecisionSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and its owner-section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            return false;
        }

        if (token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || token.AbsoluteDay < 0L
            || token.MutationEpoch < 0L)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidCaptureContext,
                "A successful completed Daily-v1 boundary token is required.", out failure);
        }

        OwnerSectionCensusSnapshot witness = Find(sharedOwnerSectionVector,
            PoliticalDecisionStoreCensusProvider.SectionId, out bool duplicate);
        PoliticalDecisionStore owner = witness?.OwnerInstanceIdentity as PoliticalDecisionStore;
        if (duplicate || owner == null || !Matches(witness, owner, owner.Count, owner.Revision))
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOwnerSectionVector,
                "Exactly one Required witness must identify the installed PoliticalDecisionStore.", out failure);
        }

        IReadOnlyList<PoliticalDecisionRecord> ownerRows = owner.Records;
        int count = owner.Count;
        long revision = owner.Revision;
        if (ownerRows == null || count < 0 || revision < 0L || revision != count || ownerRows.Count != count)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOwnerValues,
                "The append-only store must have matching row count, cardinality, and local revision.", out failure);
        }

        List<P12EPoliticalDecisionSnapshotRow> rows = new List<P12EPoliticalDecisionSnapshotRow>(count);
        foreach (PoliticalDecisionRecord record in ownerRows)
        {
            if (record == null || record.DecisionId == null || record.Decider == null || record.Outcome == null
                || record.CandidatePersonIds == null || record.EvidenceReferences == null
                || record.KnowledgeReferences == null)
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOwnerValues,
                    "A decision record or one of its immutable value lists is missing.", out failure);
            }

            string deciderId = GetDeciderId(record.Decider);
            if (deciderId == null)
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOwnerValues,
                    "A decision decider must have one valid typed stable identity.", out failure);
            }

            List<string> candidates = new List<string>(record.CandidatePersonIds.Count);
            foreach (PersonId candidate in record.CandidatePersonIds)
            {
                if (candidate == null)
                {
                    return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOwnerValues,
                        "Candidate Person identities cannot contain null.", out failure);
                }
                candidates.Add(candidate.Value);
            }

            List<string> evidence = new List<string>(record.EvidenceReferences);
            List<string> knowledge = new List<string>(record.KnowledgeReferences);
            rows.Add(new P12EPoliticalDecisionSnapshotRow(
                record.DecisionId.Value,
                (int)record.Decider.Kind,
                deciderId,
                (int)record.DecisionKind,
                candidates,
                (int)record.Outcome.Kind,
                record.Outcome.SelectedCandidatePersonId?.Value,
                record.Outcome.ReferencedClaimId?.Value,
                record.ObservedAbsoluteDay,
                record.DecisionAbsoluteDay,
                evidence,
                knowledge,
                record.ExpectedWorldRevision,
                record.ExpectedKnowledgeRevision,
                record.OfficeId?.Value,
                record.RecognizingInstitutionId?.Value));
        }

        P12EPoliticalDecisionOwnerSnapshot candidateSnapshot = new P12EPoliticalDecisionOwnerSnapshot(
            token.AbsoluteDay,
            new P12EPoliticalDecisionSnapshotSection(
                PoliticalDecisionStoreCensusProvider.SectionId,
                CurrentSchemaVersion,
                count,
                revision,
                rows));

        if (!candidateSnapshot.TryValidate(out failure)) return false;
        if (owner.Count != count || owner.Revision != revision
            || !Matches(witness, owner, count, revision)
            || !runtime.TryValidateCompletedDailyCaptureToken(token, out _))
        {
            snapshot = null;
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOwnerSectionVector,
                "The PoliticalDecision owner or completed-boundary token changed during capture.", out failure);
        }

        snapshot = candidateSnapshot;
        failure = P12EPoliticalDecisionSnapshotFailure.None;
        return true;
    }

    internal bool TryStage(
        PersonStore stagedPersons,
        InstitutionStore stagedInstitutions,
        OfficeStore stagedOffices,
        PoliticalClaimStore stagedClaims,
        out PoliticalDecisionStore staged,
        out P12EPoliticalDecisionSnapshotFailure failure)
    {
        staged = null;
        if (!TryValidate(out failure)) return false;
        if (stagedPersons == null || stagedInstitutions == null || stagedOffices == null || stagedClaims == null)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidReference,
                "Exact staged Person, Institution, Office, and PoliticalClaim roots are required.", out failure);
        }

        try
        {
            List<PoliticalDecisionRecord> records = new List<PoliticalDecisionRecord>(Decisions.RecordCount);
            foreach (P12EPoliticalDecisionSnapshotRow row in Decisions.Records)
            {
                PoliticalKnowledgeHolder decider = CreateDecider(row.DeciderKind, row.DeciderId);
                List<PersonId> candidates = new List<PersonId>(row.CandidatePersonIds.Count);
                foreach (string candidateIdText in row.CandidatePersonIds)
                {
                    PersonId personId = new PersonId(candidateIdText);
                    if (!stagedPersons.TryGet(personId, out _))
                    {
                        return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidReference,
                            "A candidate references a Person absent from the exact staged PersonStore.", out failure);
                    }
                    candidates.Add(personId);
                }

                PoliticalDecisionOutcome outcome = CreateOutcome(row);
                if (outcome.SelectedCandidatePersonId != null
                    && !stagedPersons.TryGet(outcome.SelectedCandidatePersonId, out _))
                {
                    return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidReference,
                        "The selected candidate is absent from the exact staged PersonStore.", out failure);
                }
                if (outcome.ReferencedClaimId != null
                    && !stagedClaims.TryGet(outcome.ReferencedClaimId, out _))
                {
                    return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidReference,
                        "The referenced claim is absent from the exact staged PoliticalClaimStore.", out failure);
                }

                OfficeId officeId = row.OfficeId == null ? null : new OfficeId(row.OfficeId);
                if (officeId != null && !stagedOffices.TryGet(officeId, out _))
                {
                    return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidReference,
                        "The decision references an Office absent from the exact staged OfficeStore.", out failure);
                }

                InstitutionId recognizingInstitutionId = row.RecognizingInstitutionId == null
                    ? null
                    : new InstitutionId(row.RecognizingInstitutionId);
                if (recognizingInstitutionId != null
                    && !stagedInstitutions.TryGet(recognizingInstitutionId, out _))
                {
                    return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidReference,
                        "The decision references an Institution absent from the exact staged InstitutionStore.", out failure);
                }

                records.Add(new PoliticalDecisionRecord(
                    new PoliticalDecisionId(row.DecisionId),
                    decider,
                    (PoliticalDecisionKind)row.DecisionKind,
                    candidates,
                    outcome,
                    row.ObservedAbsoluteDay,
                    row.DecisionAbsoluteDay,
                    row.EvidenceReferences,
                    row.KnowledgeReferences,
                    row.ExpectedWorldRevision,
                    row.ExpectedKnowledgeRevision,
                    officeId,
                    recognizingInstitutionId));
            }

            if (!PoliticalDecisionStore.TryCreateFromP12EOwnerSnapshot(
                    stagedPersons, records, Decisions.RecordCount, Decisions.Revision,
                    out PoliticalDecisionStore candidate))
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.StageFailed,
                    "The private PoliticalDecisionStore factory rejected the validated rows.", out failure);
            }

            staged = candidate;
            failure = P12EPoliticalDecisionSnapshotFailure.None;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException
            || exception is NullReferenceException)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.StageFailed,
                "Private staging rejected malformed PoliticalDecision values: " + exception.Message, out failure);
        }
    }

    private bool TryValidate(out P12EPoliticalDecisionSnapshotFailure failure)
    {
        failure = P12EPoliticalDecisionSnapshotFailure.Create(
            P12EPoliticalDecisionSnapshotFailureCode.InvalidIdentity,
            "PoliticalDecision owner snapshot is malformed.");
        if (CapturedAbsoluteDay < 0L)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidTimeline,
                "Captured day must be nonnegative.", out failure);
        }
        if (Decisions == null
            || !string.Equals(Decisions.SectionId, PoliticalDecisionStoreCensusProvider.SectionId, StringComparison.Ordinal)
            || Decisions.SchemaVersion != CurrentSchemaVersion)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.UnsupportedSchema,
                "The existing Required schema-v1 PoliticalDecision section is required.", out failure);
        }
        if (Decisions.Revision < 0L || Decisions.Revision != Decisions.RecordCount)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidRevision,
                "The exact append-only revision must be nonnegative and equal the record count.", out failure);
        }
        if (Decisions.Records == null || Decisions.RecordCount < 0
            || Decisions.Records.Count != Decisions.RecordCount)
        {
            return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidCardinality,
                "Declared decision cardinality must equal the detached row count.", out failure);
        }

        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
        P12EPoliticalDecisionSnapshotRow previous = null;
        foreach (P12EPoliticalDecisionSnapshotRow row in Decisions.Records)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.DecisionId)
                || string.IsNullOrWhiteSpace(row.DeciderId)
                || !Enum.IsDefined(typeof(PoliticalKnowledgeHolderKind), row.DeciderKind)
                || !Enum.IsDefined(typeof(PoliticalDecisionKind), row.DecisionKind)
                || !Enum.IsDefined(typeof(PoliticalDecisionOutcomeKind), row.OutcomeKind))
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidIdentity,
                    "Decision identity, typed decider, decision kind, or outcome kind is invalid.", out failure);
            }
            if (!identities.Add(row.DecisionId))
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.DuplicateIdentity,
                    "Decision IDs must be unique under ordinal comparison.", out failure);
            }
            if (row.ObservedAbsoluteDay < 0L || row.DecisionAbsoluteDay < row.ObservedAbsoluteDay
                || row.DecisionAbsoluteDay > CapturedAbsoluteDay
                || row.ExpectedWorldRevision < 0L || row.ExpectedKnowledgeRevision < 0L)
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidTimeline,
                    "Decision dates must fit the captured boundary and expected revisions must be nonnegative.", out failure);
            }
            if (previous != null && CompareRows(previous, row) > 0)
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOrdering,
                    "Decision rows must preserve DecisionAbsoluteDay/ordinal DecisionId order.", out failure);
            }
            if (!HasCanonicalStrings(row.CandidatePersonIds, allowEmpty: true)
                || !HasCanonicalStrings(row.EvidenceReferences, allowEmpty: true)
                || !HasCanonicalStrings(row.KnowledgeReferences, allowEmpty: true))
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOrdering,
                    "Candidate and reference lists must be nonblank, unique, and already ordinal-sorted.", out failure);
            }
            if (!HasValidOutcomeAndKind(row))
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOutcome,
                    "Outcome fields and optional office/institution identities do not match the decision kind.", out failure);
            }
            if (row.OutcomeKind == (int)PoliticalDecisionOutcomeKind.CandidateSelected
                && !Contains(row.CandidatePersonIds, row.SelectedCandidatePersonId))
            {
                return Fail(P12EPoliticalDecisionSnapshotFailureCode.InvalidOutcome,
                    "The selected candidate must occur in the canonical candidate list.", out failure);
            }
            previous = row;
        }

        failure = P12EPoliticalDecisionSnapshotFailure.None;
        return true;
    }

    private static bool HasValidOutcomeAndKind(P12EPoliticalDecisionSnapshotRow row)
    {
        bool selected = !string.IsNullOrWhiteSpace(row.SelectedCandidatePersonId);
        bool claim = !string.IsNullOrWhiteSpace(row.ReferencedClaimId);
        bool officeDecision = row.DecisionKind == (int)PoliticalDecisionKind.SuccessionSelection
            || row.DecisionKind == (int)PoliticalDecisionKind.OfficeSelection;
        bool claimDecision = row.DecisionKind == (int)PoliticalDecisionKind.ClaimRecognitionProposal;

        if (row.SelectedCandidatePersonId != null && !selected) return false;
        if (row.ReferencedClaimId != null && !claim) return false;
        if (selected != (row.OutcomeKind == (int)PoliticalDecisionOutcomeKind.CandidateSelected)) return false;
        if (claim != (row.OutcomeKind == (int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed)) return false;
        if (officeDecision != !string.IsNullOrWhiteSpace(row.OfficeId)) return false;
        if (!officeDecision && row.OfficeId != null) return false;
        if (claimDecision != !string.IsNullOrWhiteSpace(row.RecognizingInstitutionId)) return false;
        if (!claimDecision && row.RecognizingInstitutionId != null) return false;
        if (officeDecision
            && row.OutcomeKind != (int)PoliticalDecisionOutcomeKind.CandidateSelected
            && row.OutcomeKind != (int)PoliticalDecisionOutcomeKind.NoSelection
            && row.OutcomeKind != (int)PoliticalDecisionOutcomeKind.Rejected)
        {
            return false;
        }
        return !claimDecision || row.OutcomeKind == (int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed;
    }

    private static bool HasCanonicalStrings(IReadOnlyList<string> values, bool allowEmpty)
    {
        if (values == null) return false;
        if (!allowEmpty && values.Count == 0) return false;
        string previous = null;
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (previous != null && StringComparer.Ordinal.Compare(previous, value) >= 0) return false;
            previous = value;
        }
        return true;
    }

    private static bool Contains(IReadOnlyList<string> values, string value)
    {
        if (values == null || value == null) return false;
        for (int index = 0; index < values.Count; index++)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static int CompareRows(P12EPoliticalDecisionSnapshotRow left, P12EPoliticalDecisionSnapshotRow right)
    {
        int result = left.DecisionAbsoluteDay.CompareTo(right.DecisionAbsoluteDay);
        return result != 0
            ? result
            : StringComparer.Ordinal.Compare(left.DecisionId, right.DecisionId);
    }

    private static PoliticalDecisionOutcome CreateOutcome(P12EPoliticalDecisionSnapshotRow row)
    {
        switch ((PoliticalDecisionOutcomeKind)row.OutcomeKind)
        {
            case PoliticalDecisionOutcomeKind.NoSelection:
                return PoliticalDecisionOutcome.None();
            case PoliticalDecisionOutcomeKind.CandidateSelected:
                return PoliticalDecisionOutcome.Candidate(new PersonId(row.SelectedCandidatePersonId));
            case PoliticalDecisionOutcomeKind.ClaimRecognitionProposed:
                return PoliticalDecisionOutcome.RecognizeClaim(new PoliticalClaimId(row.ReferencedClaimId));
            case PoliticalDecisionOutcomeKind.Rejected:
                return PoliticalDecisionOutcome.Rejected();
            default:
                throw new ArgumentOutOfRangeException(nameof(row.OutcomeKind));
        }
    }

    private static PoliticalKnowledgeHolder CreateDecider(int kind, string id)
    {
        switch ((PoliticalKnowledgeHolderKind)kind)
        {
            case PoliticalKnowledgeHolderKind.Person:
                return PoliticalKnowledgeHolder.ForPerson(new PersonId(id));
            case PoliticalKnowledgeHolderKind.Institution:
                return PoliticalKnowledgeHolder.ForInstitution(new InstitutionId(id));
            case PoliticalKnowledgeHolderKind.Faction:
                return PoliticalKnowledgeHolder.ForFaction(new FactionId(id));
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static string GetDeciderId(PoliticalKnowledgeHolder decider)
    {
        if (decider == null || !Enum.IsDefined(typeof(PoliticalKnowledgeHolderKind), decider.Kind)) return null;
        switch (decider.Kind)
        {
            case PoliticalKnowledgeHolderKind.Person:
                return decider.PersonId?.Value;
            case PoliticalKnowledgeHolderKind.Institution:
                return decider.InstitutionId?.Value;
            case PoliticalKnowledgeHolderKind.Faction:
                return decider.FactionId?.Value;
            default:
                return null;
        }
    }

    private static OwnerSectionCensusSnapshot Find(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId,
        out bool duplicate)
    {
        OwnerSectionCensusSnapshot result = null;
        duplicate = false;
        if (vector == null) return null;
        foreach (OwnerSectionCensusSnapshot section in vector)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal)) continue;
            if (result != null) duplicate = true;
            result = section;
        }
        return result;
    }

    private static bool Matches(
        OwnerSectionCensusSnapshot section,
        PoliticalDecisionStore owner,
        int count,
        long revision)
    {
        return section != null
            && owner != null
            && string.Equals(section.SectionId, PoliticalDecisionStoreCensusProvider.SectionId, StringComparison.Ordinal)
            && section.SchemaVersion == CurrentSchemaVersion
            && section.Role == OwnerSectionRole.Required
            && ReferenceEquals(section.OwnerInstanceIdentity, owner)
            && section.Cardinality == count
            && section.Revision == revision;
    }

    private static bool Fail(
        P12EPoliticalDecisionSnapshotFailureCode code,
        string message,
        out P12EPoliticalDecisionSnapshotFailure failure)
    {
        failure = P12EPoliticalDecisionSnapshotFailure.Create(code, message);
        return false;
    }
}
