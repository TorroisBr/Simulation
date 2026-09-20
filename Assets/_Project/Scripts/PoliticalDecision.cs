using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

public sealed class PoliticalDecisionId : IEquatable<PoliticalDecisionId>
{
    private readonly string value;

    public string Value => value;

    public PoliticalDecisionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("PoliticalDecisionId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public bool Equals(PoliticalDecisionId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalDecisionId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;

    public static bool operator ==(PoliticalDecisionId left, PoliticalDecisionId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(PoliticalDecisionId left, PoliticalDecisionId right) => (left == right) == false;
}

public enum PoliticalDecisionKind
{
    SuccessionSelection = 0,
    ClaimRecognitionProposal = 1,
    OfficeSelection = 2,
    Other = 3
}

public enum PoliticalDecisionOutcomeKind
{
    NoSelection = 0,
    CandidateSelected = 1,
    ClaimRecognitionProposed = 2,
    Rejected = 3
}

/// <summary>
/// Typed optional outcome. This describes a proposal/selection only; it does
/// not assign an office, recognize a claim, or mutate any world store.
/// </summary>
public sealed class PoliticalDecisionOutcome : IEquatable<PoliticalDecisionOutcome>
{
    public PoliticalDecisionOutcomeKind Kind { get; }
    public PersonId SelectedCandidatePersonId { get; }
    public PoliticalClaimId ReferencedClaimId { get; }
    public bool HasSelection => SelectedCandidatePersonId != null || ReferencedClaimId != null;

    private PoliticalDecisionOutcome(
        PoliticalDecisionOutcomeKind kind,
        PersonId selectedCandidatePersonId,
        PoliticalClaimId referencedClaimId)
    {
        if (Enum.IsDefined(typeof(PoliticalDecisionOutcomeKind), kind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (kind == PoliticalDecisionOutcomeKind.CandidateSelected && selectedCandidatePersonId == null)
        {
            throw new ArgumentNullException(nameof(selectedCandidatePersonId));
        }

        if (kind == PoliticalDecisionOutcomeKind.ClaimRecognitionProposed && referencedClaimId == null)
        {
            throw new ArgumentNullException(nameof(referencedClaimId));
        }

        if (kind != PoliticalDecisionOutcomeKind.CandidateSelected && selectedCandidatePersonId != null)
        {
            throw new ArgumentException("Only a candidate selection may carry a candidate PersonId.", nameof(selectedCandidatePersonId));
        }

        if (kind != PoliticalDecisionOutcomeKind.ClaimRecognitionProposed && referencedClaimId != null)
        {
            throw new ArgumentException("Only a claim recognition proposal may carry a claim id.", nameof(referencedClaimId));
        }

        Kind = kind;
        SelectedCandidatePersonId = selectedCandidatePersonId;
        ReferencedClaimId = referencedClaimId;
    }

    public static PoliticalDecisionOutcome None()
    {
        return new PoliticalDecisionOutcome(PoliticalDecisionOutcomeKind.NoSelection, null, null);
    }

    public static PoliticalDecisionOutcome Candidate(PersonId candidatePersonId)
    {
        return new PoliticalDecisionOutcome(PoliticalDecisionOutcomeKind.CandidateSelected, candidatePersonId, null);
    }

    public static PoliticalDecisionOutcome RecognizeClaim(PoliticalClaimId claimId)
    {
        return new PoliticalDecisionOutcome(PoliticalDecisionOutcomeKind.ClaimRecognitionProposed, null, claimId);
    }

    public static PoliticalDecisionOutcome Rejected()
    {
        return new PoliticalDecisionOutcome(PoliticalDecisionOutcomeKind.Rejected, null, null);
    }

    public bool Equals(PoliticalDecisionOutcome other)
    {
        return other != null
            && Kind == other.Kind
            && SelectedCandidatePersonId == other.SelectedCandidatePersonId
            && ReferencedClaimId == other.ReferencedClaimId;
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalDecisionOutcome);
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Kind;
            hash = (hash * 397) ^ (SelectedCandidatePersonId == null ? 0 : SelectedCandidatePersonId.GetHashCode());
            return (hash * 397) ^ (ReferencedClaimId == null ? 0 : ReferencedClaimId.GetHashCode());
        }
    }
}

/// <summary>
/// Immutable knowledge-driven decision proposal/selection metadata.
/// </summary>
public sealed class PoliticalDecisionRecord
{
    public PoliticalDecisionId DecisionId { get; }
    public PoliticalKnowledgeHolder Decider { get; }
    public PoliticalDecisionKind DecisionKind { get; }
    public IReadOnlyList<PersonId> CandidatePersonIds { get; }
    public string CandidateFingerprint { get; }
    public PoliticalDecisionOutcome Outcome { get; }
    public PoliticalDecisionOutcome SelectedOutcome => Outcome;
    public long ObservedAbsoluteDay { get; }
    public long DecisionAbsoluteDay { get; }
    public IReadOnlyList<string> EvidenceReferences { get; }
    public IReadOnlyList<string> KnowledgeReferences { get; }
    public long ExpectedWorldRevision { get; }
    public long ExpectedKnowledgeRevision { get; }
    public long WorldRevision => ExpectedWorldRevision;
    public long KnowledgeRevision => ExpectedKnowledgeRevision;
    public OfficeId OfficeId { get; }
    public InstitutionId RecognizingInstitutionId { get; }

    public PoliticalDecisionRecord(
        PoliticalDecisionId decisionId,
        PoliticalKnowledgeHolder decider,
        PoliticalDecisionKind decisionKind,
        IEnumerable<PersonId> candidatePersonIds,
        PoliticalDecisionOutcome outcome,
        long observedAbsoluteDay,
        long decisionAbsoluteDay,
        IEnumerable<string> evidenceReferences,
        IEnumerable<string> knowledgeReferences,
        long expectedWorldRevision,
        long expectedKnowledgeRevision,
        OfficeId officeId = null,
        InstitutionId recognizingInstitutionId = null)
    {
        DecisionId = decisionId ?? throw new ArgumentNullException(nameof(decisionId));
        Decider = decider ?? throw new ArgumentNullException(nameof(decider));
        if (Enum.IsDefined(typeof(PoliticalDecisionKind), decisionKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(decisionKind));
        }

        if (observedAbsoluteDay < 0L || decisionAbsoluteDay < observedAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(nameof(decisionAbsoluteDay));
        }

        if (expectedWorldRevision < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedWorldRevision));
        }

        if (expectedKnowledgeRevision < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedKnowledgeRevision));
        }

        Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        List<PersonId> candidates = CanonicalizeCandidates(candidatePersonIds);
        if (Outcome.SelectedCandidatePersonId != null && Contains(candidates, Outcome.SelectedCandidatePersonId) == false)
        {
            throw new ArgumentException("A selected candidate must be present in CandidatePersonIds.", nameof(outcome));
        }

        if ((decisionKind == PoliticalDecisionKind.SuccessionSelection
                || decisionKind == PoliticalDecisionKind.OfficeSelection)
            && officeId == null)
        {
            throw new ArgumentNullException(nameof(officeId));
        }

        if (decisionKind != PoliticalDecisionKind.SuccessionSelection
            && decisionKind != PoliticalDecisionKind.OfficeSelection
            && officeId != null)
        {
            throw new ArgumentException(
                "Only office decisions may identify an office.",
                nameof(officeId));
        }

        if (decisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
            && Outcome.Kind != PoliticalDecisionOutcomeKind.ClaimRecognitionProposed)
        {
            throw new ArgumentException(
                "Claim recognition decisions must carry a claim recognition proposal.",
                nameof(outcome));
        }

        if (decisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
            && recognizingInstitutionId == null)
        {
            throw new ArgumentNullException(nameof(recognizingInstitutionId));
        }

        if (decisionKind != PoliticalDecisionKind.ClaimRecognitionProposal
            && recognizingInstitutionId != null)
        {
            throw new ArgumentException(
                "Only claim recognition decisions may identify a recognizing institution.",
                nameof(recognizingInstitutionId));
        }

        if ((decisionKind == PoliticalDecisionKind.SuccessionSelection
                || decisionKind == PoliticalDecisionKind.OfficeSelection)
            && Outcome.Kind != PoliticalDecisionOutcomeKind.CandidateSelected
            && Outcome.Kind != PoliticalDecisionOutcomeKind.Rejected
            && Outcome.Kind != PoliticalDecisionOutcomeKind.NoSelection)
        {
            throw new ArgumentException(
                "Office decisions must carry a candidate selection, no selection, or rejection.",
                nameof(outcome));
        }

        CandidatePersonIds = new ReadOnlyCollection<PersonId>(candidates);
        CandidateFingerprint = BuildCandidateFingerprint(candidates);
        ObservedAbsoluteDay = observedAbsoluteDay;
        DecisionAbsoluteDay = decisionAbsoluteDay;
        EvidenceReferences = CreateReferences(evidenceReferences);
        KnowledgeReferences = CreateReferences(knowledgeReferences);
        ExpectedWorldRevision = expectedWorldRevision;
        ExpectedKnowledgeRevision = expectedKnowledgeRevision;
        OfficeId = officeId;
        RecognizingInstitutionId = recognizingInstitutionId;
    }

    public bool IsStaleFor(long currentAbsoluteDay, long currentWorldRevision, long currentKnowledgeRevision)
    {
        if (currentAbsoluteDay < 0L || currentWorldRevision < 0L || currentKnowledgeRevision < 0L)
        {
            throw new ArgumentOutOfRangeException();
        }

        return currentAbsoluteDay != DecisionAbsoluteDay
            || currentWorldRevision != ExpectedWorldRevision
            || currentKnowledgeRevision != ExpectedKnowledgeRevision;
    }

    public static string BuildCandidateFingerprint(IEnumerable<PersonId> candidatePersonIds)
    {
        return BuildCandidateFingerprint(CanonicalizeCandidates(candidatePersonIds));
    }

    private static string BuildCandidateFingerprint(IReadOnlyList<PersonId> candidates)
    {
        StringBuilder fingerprint = new StringBuilder("candidates:");
        fingerprint.Append(candidates.Count).Append(':');
        foreach (PersonId candidate in candidates)
        {
            fingerprint.Append(candidate.Value.Length).Append(':').Append(candidate.Value).Append(';');
        }

        return fingerprint.ToString();
    }

    private static List<PersonId> CanonicalizeCandidates(IEnumerable<PersonId> candidatePersonIds)
    {
        if (candidatePersonIds == null)
        {
            throw new ArgumentNullException(nameof(candidatePersonIds));
        }

        List<PersonId> candidates = new List<PersonId>();
        foreach (PersonId candidate in candidatePersonIds)
        {
            if (candidate == null)
            {
                throw new ArgumentException("Candidate PersonIds cannot contain null.", nameof(candidatePersonIds));
            }

            if (Contains(candidates, candidate))
            {
                throw new ArgumentException("Candidate PersonIds cannot contain duplicates.", nameof(candidatePersonIds));
            }

            candidates.Add(candidate);
        }

        candidates.Sort((left, right) => StringComparer.Ordinal.Compare(left.Value, right.Value));
        return candidates;
    }

    private static bool Contains(IReadOnlyList<PersonId> candidates, PersonId candidate)
    {
        for (int index = 0; index < candidates.Count; index++)
        {
            if (candidates[index] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> CreateReferences(IEnumerable<string> references)
    {
        List<string> result = new List<string>();
        if (references != null)
        {
            foreach (string reference in references)
            {
                if (string.IsNullOrWhiteSpace(reference) == false && result.Contains(reference) == false)
                {
                    result.Add(reference);
                }
            }
        }

        result.Sort(StringComparer.Ordinal);
        return new ReadOnlyCollection<string>(result);
    }
}

public enum PoliticalDecisionFailureCode
{
    None = 0,
    InvalidDecision = 1,
    DuplicateDecisionId = 2,
    RevisionOverflow = 3,
    StaleDecision = 4
}

public sealed class PoliticalDecisionFailure
{
    private static readonly PoliticalDecisionFailure none =
        new PoliticalDecisionFailure(PoliticalDecisionFailureCode.None, string.Empty);

    private PoliticalDecisionFailure(PoliticalDecisionFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static PoliticalDecisionFailure None => none;
    public PoliticalDecisionFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != PoliticalDecisionFailureCode.None;

    public static PoliticalDecisionFailure Create(PoliticalDecisionFailureCode code, string message)
    {
        return code == PoliticalDecisionFailureCode.None ? None : new PoliticalDecisionFailure(code, message);
    }

    public override string ToString() => Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

/// <summary>
/// Deterministic append-only history of political decisions. It stores
/// proposals/selections and captured stale checks; it owns no execution path.
/// </summary>
public sealed class PoliticalDecisionStore
{
    private readonly Dictionary<string, PoliticalDecisionRecord> recordsById =
        new Dictionary<string, PoliticalDecisionRecord>(StringComparer.Ordinal);
    private PersonStore boundPersonStore;
    private long revision;

    public int Count => recordsById.Count;
    public long Revision => revision;

    internal bool TryBindToPersonStore(PersonStore personStore)
    {
        if (personStore == null)
        {
            return false;
        }

        if (boundPersonStore != null
            && ReferenceEquals(boundPersonStore, personStore) == false)
        {
            return false;
        }

        boundPersonStore = personStore;
        return true;
    }

    internal bool IsCompatibleWithPersonStore(PersonStore personStore)
    {
        return personStore != null
            && (boundPersonStore == null || ReferenceEquals(boundPersonStore, personStore));
    }

    public IReadOnlyList<PoliticalDecisionRecord> Records
    {
        get
        {
            List<PoliticalDecisionRecord> records = new List<PoliticalDecisionRecord>(recordsById.Values);
            records.Sort(CompareRecords);
            return new ReadOnlyCollection<PoliticalDecisionRecord>(records);
        }
    }

    public bool TryRegister(PoliticalDecisionRecord record, out PoliticalDecisionFailure failure)
    {
        if (record == null || record.DecisionId == null || record.Decider == null || record.Outcome == null)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A political decision record is required.");
            return false;
        }

        if (recordsById.ContainsKey(record.DecisionId.Value))
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.DuplicateDecisionId,
                "The political decision id is already registered.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.RevisionOverflow,
                "The political decision store revision cannot advance further.");
            return false;
        }

        recordsById.Add(record.DecisionId.Value, record);
        revision++;
        failure = PoliticalDecisionFailure.None;
        return true;
    }

    public bool TryGet(PoliticalDecisionId decisionId, out PoliticalDecisionRecord record)
    {
        record = null;
        return decisionId != null && recordsById.TryGetValue(decisionId.Value, out record);
    }

    public PoliticalDecisionStore Clone()
    {
        PoliticalDecisionStore clone = new PoliticalDecisionStore();
        foreach (KeyValuePair<string, PoliticalDecisionRecord> entry in recordsById)
        {
            clone.recordsById.Add(entry.Key, entry.Value);
        }

        clone.revision = revision;
        clone.boundPersonStore = boundPersonStore;
        return clone;
    }

    private static int CompareRecords(PoliticalDecisionRecord left, PoliticalDecisionRecord right)
    {
        int day = left.DecisionAbsoluteDay.CompareTo(right.DecisionAbsoluteDay);
        return day != 0
            ? day
            : StringComparer.Ordinal.Compare(left.DecisionId.Value, right.DecisionId.Value);
    }
}
