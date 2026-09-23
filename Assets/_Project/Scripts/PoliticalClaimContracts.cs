using System;
using System.Collections.Generic;

public sealed class PoliticalClaimId : IEquatable<PoliticalClaimId>
{
    private readonly string value;

    public string Value => value;

    public PoliticalClaimId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("PoliticalClaimId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out PoliticalClaimId claimId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            claimId = null;
            return false;
        }

        claimId = new PoliticalClaimId(value);
        return true;
    }

    public bool Equals(PoliticalClaimId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalClaimId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(PoliticalClaimId left, PoliticalClaimId right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(PoliticalClaimId left, PoliticalClaimId right)
    {
        return (left == right) == false;
    }
}

public enum PoliticalClaimType
{
    OfficeEntitlement = 0,
    SuccessionEntitlement = 1,
    PropertyEntitlement = 2,
    InstitutionalAuthority = 3,
    StatusRecognition = 4,
    LineageEntitlement = 5
}

public enum PoliticalClaimTargetKind
{
    Office = 0,
    Property = 1,
    Institution = 2,
    Person = 3
}

public sealed class PoliticalClaimTarget : IEquatable<PoliticalClaimTarget>
{
    public PoliticalClaimTargetKind Kind { get; }
    public string TargetId { get; }

    private PoliticalClaimTarget(PoliticalClaimTargetKind kind, string targetId)
    {
        if (Enum.IsDefined(typeof(PoliticalClaimTargetKind), kind) == false)
        {
            throw new ArgumentException("Political claim target kind is invalid.", nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Political claim target requires a non-empty id.", nameof(targetId));
        }

        Kind = kind;
        TargetId = targetId;
    }

    public static PoliticalClaimTarget ForOffice(OfficeId officeId)
    {
        return new PoliticalClaimTarget(PoliticalClaimTargetKind.Office, RequireValue(officeId, nameof(officeId)));
    }

    public static PoliticalClaimTarget ForProperty(PropertyId propertyId)
    {
        return new PoliticalClaimTarget(PoliticalClaimTargetKind.Property, RequireValue(propertyId, nameof(propertyId)));
    }

    public static PoliticalClaimTarget ForInstitution(InstitutionId institutionId)
    {
        return new PoliticalClaimTarget(PoliticalClaimTargetKind.Institution, RequireValue(institutionId, nameof(institutionId)));
    }

    public static PoliticalClaimTarget ForPerson(PersonId personId)
    {
        return new PoliticalClaimTarget(PoliticalClaimTargetKind.Person, RequireValue(personId, nameof(personId)));
    }

    public bool Equals(PoliticalClaimTarget other)
    {
        return other != null
            && Kind == other.Kind
            && string.Equals(TargetId, other.TargetId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalClaimTarget);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(TargetId);
        }
    }

    private static string RequireValue(object value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value is OfficeId officeId)
        {
            return officeId.Value;
        }

        if (value is PropertyId propertyId)
        {
            return propertyId.Value;
        }

        if (value is InstitutionId institutionId)
        {
            return institutionId.Value;
        }

        return ((PersonId)value).Value;
    }
}

public enum PoliticalClaimBasis
{
    Genealogy = 0,
    OfficeIncumbency = 1,
    PropertyOwnership = 2,
    InstitutionalAppointment = 3,
    ExplicitDecision = 4,
    Other = 5
}

public enum PoliticalClaimStatus
{
    Active = 0,
    Resolved = 1,
    Withdrawn = 2,
    Rejected = 3
}

public enum PoliticalClaimRecognitionState
{
    Unrecognized = 0,
    Recognized = 1,
    Contested = 2,
    Rejected = 3
}

public enum PoliticalClaimFailureCode
{
    None = 0,
    InvalidClaimId = 1,
    DuplicateClaimId = 2,
    InvalidClaim = 3,
    InvalidClaimType = 4,
    InvalidClaimTarget = 5,
    ClaimTargetTypeMismatch = 6,
    InvalidCreationAbsoluteDay = 7,
    ClaimantNotRegistered = 8,
    ClaimTargetNotFound = 9,
    InvalidClaimStatus = 10,
    InvalidRecognitionState = 11,
    RecognitionRequiresInstitution = 12,
    RecognitionRequiresDay = 13,
    InvalidRecognitionAbsoluteDay = 14,
    ResolutionRequiresActiveClaim = 15,
    InvalidResolutionAbsoluteDay = 16,
    StaleClaim = 17,
    InvalidTransition = 18,
    RevisionOverflow = 19,
    RuntimeFaulted = 20
}

public sealed class PoliticalClaimFailure : IEquatable<PoliticalClaimFailure>
{
    private static readonly PoliticalClaimFailure none =
        new PoliticalClaimFailure(PoliticalClaimFailureCode.None, string.Empty);

    private PoliticalClaimFailure(PoliticalClaimFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static PoliticalClaimFailure None => none;
    public PoliticalClaimFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != PoliticalClaimFailureCode.None;

    public static PoliticalClaimFailure Create(PoliticalClaimFailureCode code, string message)
    {
        return code == PoliticalClaimFailureCode.None
            ? None
            : new PoliticalClaimFailure(code, message);
    }

    public bool Equals(PoliticalClaimFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalClaimFailure);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
        }
    }

    public override string ToString()
    {
        return Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
    }
}

public sealed class PoliticalClaimRecord : IEquatable<PoliticalClaimRecord>
{
    public PoliticalClaimId ClaimId { get; }
    public PersonId ClaimantPersonId { get; }
    public PoliticalClaimType ClaimType { get; }
    public PoliticalClaimTarget Target { get; }
    public PoliticalClaimBasis Basis { get; }
    public string BasisDescription { get; }
    public long CreatedAbsoluteDay { get; }
    public PoliticalClaimStatus Status { get; }
    public long? ResolutionAbsoluteDay { get; }
    public IReadOnlyList<string> EvidenceReferences { get; }

    public PoliticalClaimRecord(
        PoliticalClaimId claimId,
        PersonId claimantPersonId,
        PoliticalClaimType claimType,
        PoliticalClaimTarget target,
        PoliticalClaimBasis basis,
        string basisDescription,
        long createdAbsoluteDay,
        IEnumerable<string> evidenceReferences,
        PoliticalClaimStatus status = PoliticalClaimStatus.Active,
        PoliticalClaimRecognitionState recognitionState = PoliticalClaimRecognitionState.Unrecognized,
        InstitutionId recognizingInstitutionId = null,
        long? recognitionAbsoluteDay = null,
        string recognitionReason = null,
        long? resolutionAbsoluteDay = null)
    {
        ClaimId = claimId ?? throw new ArgumentNullException(nameof(claimId));
        ClaimantPersonId = claimantPersonId ?? throw new ArgumentNullException(nameof(claimantPersonId));
        if (Enum.IsDefined(typeof(PoliticalClaimType), claimType) == false)
        {
            throw new ArgumentException("Claim type is invalid.", nameof(claimType));
        }

        Target = target ?? throw new ArgumentNullException(nameof(target));
        if (IsTargetCompatible(claimType, target.Kind) == false)
        {
            throw new ArgumentException("Claim type and target kind are incompatible.", nameof(target));
        }

        if (Enum.IsDefined(typeof(PoliticalClaimBasis), basis) == false)
        {
            throw new ArgumentException("Claim basis is invalid.", nameof(basis));
        }

        if (createdAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        }

        if (Enum.IsDefined(typeof(PoliticalClaimStatus), status) == false
            || Enum.IsDefined(typeof(PoliticalClaimRecognitionState), recognitionState) == false)
        {
            throw new ArgumentException("Claim state is invalid.");
        }

        if (status == PoliticalClaimStatus.Active && resolutionAbsoluteDay.HasValue)
        {
            throw new ArgumentException("An active claim cannot carry a resolution day.", nameof(resolutionAbsoluteDay));
        }

        if (status != PoliticalClaimStatus.Active
            && (resolutionAbsoluteDay.HasValue == false
                || resolutionAbsoluteDay.Value < createdAbsoluteDay
                || resolutionAbsoluteDay.Value < 0L))
        {
            throw new ArgumentException("A terminal claim requires a valid resolution day.", nameof(resolutionAbsoluteDay));
        }

        if (recognitionState != PoliticalClaimRecognitionState.Unrecognized
            || recognizingInstitutionId != null
            || recognitionAbsoluteDay.HasValue
            || string.IsNullOrWhiteSpace(recognitionReason) == false)
        {
            throw new ArgumentException(
                "Political claim recognition is institution-scoped and must be registered through PoliticalClaimStore.",
                nameof(recognitionState));
        }

        if (recognitionAbsoluteDay.HasValue
            && (recognitionAbsoluteDay.Value < createdAbsoluteDay || recognitionAbsoluteDay.Value < 0L))
        {
            throw new ArgumentOutOfRangeException(nameof(recognitionAbsoluteDay));
        }

        List<string> evidence = new List<string>();
        if (evidenceReferences != null)
        {
            foreach (string reference in evidenceReferences)
            {
                if (string.IsNullOrWhiteSpace(reference) == false && evidence.Contains(reference) == false)
                {
                    evidence.Add(reference);
                }
            }
        }

        evidence.Sort(StringComparer.Ordinal);
        EvidenceReferences = evidence.AsReadOnly();
        BasisDescription = basisDescription ?? string.Empty;
        ClaimType = claimType;
        Basis = basis;
        CreatedAbsoluteDay = createdAbsoluteDay;
        Status = status;
        ResolutionAbsoluteDay = resolutionAbsoluteDay;
    }

    internal PoliticalClaimRecord WithStatus(PoliticalClaimStatus status, long resolutionAbsoluteDay)
    {
        return new PoliticalClaimRecord(
            ClaimId,
            ClaimantPersonId,
            ClaimType,
            Target,
            Basis,
            BasisDescription,
            CreatedAbsoluteDay,
            EvidenceReferences,
            status,
            PoliticalClaimRecognitionState.Unrecognized,
            null,
            null,
            null,
            resolutionAbsoluteDay);
    }

    public bool Equals(PoliticalClaimRecord other)
    {
        return other != null
            && ClaimId == other.ClaimId
            && ClaimantPersonId == other.ClaimantPersonId
            && ClaimType == other.ClaimType
            && Equals(Target, other.Target)
            && Basis == other.Basis
            && string.Equals(BasisDescription, other.BasisDescription, StringComparison.Ordinal)
            && CreatedAbsoluteDay == other.CreatedAbsoluteDay
            && Status == other.Status
            && ResolutionAbsoluteDay == other.ResolutionAbsoluteDay
            && SequenceEqual(EvidenceReferences, other.EvidenceReferences);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalClaimRecord);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ClaimId.GetHashCode();
            hash = (hash * 397) ^ ClaimantPersonId.GetHashCode();
            hash = (hash * 397) ^ (int)ClaimType;
            hash = (hash * 397) ^ Target.GetHashCode();
            hash = (hash * 397) ^ (int)Basis;
            hash = (hash * 397) ^ CreatedAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ (int)Status;
            return (hash * 397) ^ (ResolutionAbsoluteDay?.GetHashCode() ?? 0);
        }
    }

    public static bool IsTargetCompatible(PoliticalClaimType claimType, PoliticalClaimTargetKind targetKind)
    {
        if (claimType == PoliticalClaimType.OfficeEntitlement
            || claimType == PoliticalClaimType.SuccessionEntitlement)
        {
            return targetKind == PoliticalClaimTargetKind.Office;
        }

        if (claimType == PoliticalClaimType.PropertyEntitlement)
        {
            return targetKind == PoliticalClaimTargetKind.Property;
        }

        if (claimType == PoliticalClaimType.InstitutionalAuthority)
        {
            return targetKind == PoliticalClaimTargetKind.Institution;
        }

        if (claimType == PoliticalClaimType.StatusRecognition
            || claimType == PoliticalClaimType.LineageEntitlement)
        {
            return targetKind == PoliticalClaimTargetKind.Person;
        }

        return false;
    }

    private static bool SequenceEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left == null || right == null || left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            if (string.Equals(left[index], right[index], StringComparison.Ordinal) == false)
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class PoliticalClaimRecognitionHistoryEntry : IEquatable<PoliticalClaimRecognitionHistoryEntry>
{
    public PoliticalClaimRecognitionState State { get; }
    public long RecognitionAbsoluteDay { get; }
    public string Reason { get; }

    public PoliticalClaimRecognitionHistoryEntry(
        PoliticalClaimRecognitionState state,
        long recognitionAbsoluteDay,
        string reason)
    {
        if (Enum.IsDefined(typeof(PoliticalClaimRecognitionState), state) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (recognitionAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(recognitionAbsoluteDay));
        }

        State = state;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
        Reason = reason ?? string.Empty;
    }

    public bool Equals(PoliticalClaimRecognitionHistoryEntry other)
    {
        return other != null
            && State == other.State
            && RecognitionAbsoluteDay == other.RecognitionAbsoluteDay
            && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalClaimRecognitionHistoryEntry);
    public override int GetHashCode() => ((int)State * 397) ^ RecognitionAbsoluteDay.GetHashCode();
}

public sealed class PoliticalClaimRecognitionRecord : IEquatable<PoliticalClaimRecognitionRecord>
{
    public PoliticalClaimId ClaimId { get; }
    public InstitutionId InstitutionId { get; }
    public string RecognitionId => BuildRecognitionId(ClaimId, InstitutionId);
    public PoliticalClaimRecognitionState State { get; }
    public long RecognitionAbsoluteDay { get; }
    public string Reason { get; }
    public IReadOnlyList<PoliticalClaimRecognitionHistoryEntry> History { get; }

    public PoliticalClaimRecognitionRecord(
        PoliticalClaimId claimId,
        InstitutionId institutionId,
        PoliticalClaimRecognitionState state,
        long recognitionAbsoluteDay,
        string reason,
        IEnumerable<PoliticalClaimRecognitionHistoryEntry> history = null)
    {
        ClaimId = claimId ?? throw new ArgumentNullException(nameof(claimId));
        InstitutionId = institutionId ?? throw new ArgumentNullException(nameof(institutionId));
        if (Enum.IsDefined(typeof(PoliticalClaimRecognitionState), state) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (recognitionAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(recognitionAbsoluteDay));
        }

        List<PoliticalClaimRecognitionHistoryEntry> entries =
            new List<PoliticalClaimRecognitionHistoryEntry>();
        if (history != null)
        {
            foreach (PoliticalClaimRecognitionHistoryEntry entry in history)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Recognition history cannot contain null entries.", nameof(history));
                }

                entries.Add(entry);
            }
        }

        PoliticalClaimRecognitionHistoryEntry current =
            new PoliticalClaimRecognitionHistoryEntry(state, recognitionAbsoluteDay, reason);
        if (entries.Count == 0 || entries[entries.Count - 1].Equals(current) == false)
        {
            entries.Add(current);
        }

        ClaimId = claimId;
        InstitutionId = institutionId;
        State = state;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
        Reason = reason ?? string.Empty;
        History = new System.Collections.ObjectModel.ReadOnlyCollection<PoliticalClaimRecognitionHistoryEntry>(entries);
    }

    public bool Equals(PoliticalClaimRecognitionRecord other)
    {
        if (other == null
            || ClaimId != other.ClaimId
            || InstitutionId != other.InstitutionId
            || State != other.State
            || RecognitionAbsoluteDay != other.RecognitionAbsoluteDay
            || string.Equals(Reason, other.Reason, StringComparison.Ordinal) == false
            || History.Count != other.History.Count)
        {
            return false;
        }

        for (int index = 0; index < History.Count; index++)
        {
            if (History[index].Equals(other.History[index]) == false)
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalClaimRecognitionRecord);
    public override int GetHashCode() => ((ClaimId.GetHashCode() * 397) ^ InstitutionId.GetHashCode()) ^ (int)State;

    public static string BuildRecognitionId(PoliticalClaimId claimId, InstitutionId institutionId)
    {
        if (claimId == null || institutionId == null)
        {
            return null;
        }

        return claimId.Value.Length + ":" + claimId.Value
            + institutionId.Value.Length + ":" + institutionId.Value;
    }
}
