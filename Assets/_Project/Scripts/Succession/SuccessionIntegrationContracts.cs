using System;

public enum OfficeSuccessionFailureCode
{
    None = 0,
    InvalidWorld = 1,
    InvalidOfficeId = 2,
    OfficeNotFound = 3,
    OfficeNotVacant = 4,
    FormerIncumbentMissing = 5,
    InvalidSelectedCandidate = 6,
    CandidateNotEligible = 7,
    InvalidStartDay = 8,
    InvalidTransition = 9,
    StaleWorldDay = 10,
    StaleCandidateSet = 11,
    CandidateNotRegistered = 12,
    CandidateNotLiving = 13,
    AssignmentFailed = 14,
    StaleOffice = 15
}

public sealed class OfficeSuccessionFailure : IEquatable<OfficeSuccessionFailure>
{
    private static readonly OfficeSuccessionFailure none =
        new OfficeSuccessionFailure(OfficeSuccessionFailureCode.None, string.Empty);

    private OfficeSuccessionFailure(OfficeSuccessionFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static OfficeSuccessionFailure None => none;
    public OfficeSuccessionFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != OfficeSuccessionFailureCode.None;

    public static OfficeSuccessionFailure Create(
        OfficeSuccessionFailureCode code,
        string message)
    {
        return code == OfficeSuccessionFailureCode.None
            ? None
            : new OfficeSuccessionFailure(code, message);
    }

    public bool Equals(OfficeSuccessionFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as OfficeSuccessionFailure);
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

public sealed class OfficeSuccessionTransition : IEquatable<OfficeSuccessionTransition>
{
    internal PersonRuntime ExpectedCandidate { get; }
    internal SuccessionCandidateSnapshot ExpectedCandidates { get; }

    public OfficeId OfficeId { get; }
    public PersonId SubjectPersonId { get; }
    public PersonId SelectedCandidateId { get; }
    public long ExpectedWorldDay { get; }
    public long StartAbsoluteDay { get; }
    public string ExpectedCandidateFingerprint { get; }

    internal OfficeSuccessionTransition(
        OfficeId officeId,
        PersonId subjectPersonId,
        PersonRuntime expectedCandidate,
        SuccessionCandidateSnapshot expectedCandidates,
        long expectedWorldDay,
        long startAbsoluteDay)
    {
        OfficeId = officeId;
        SubjectPersonId = subjectPersonId;
        ExpectedCandidate = expectedCandidate;
        ExpectedCandidates = expectedCandidates;
        SelectedCandidateId = expectedCandidate?.PersonId;
        ExpectedWorldDay = expectedWorldDay;
        StartAbsoluteDay = startAbsoluteDay;
        ExpectedCandidateFingerprint = expectedCandidates?.DiscoveryFingerprint;
    }

    public bool Equals(OfficeSuccessionTransition other)
    {
        return other != null
            && OfficeId == other.OfficeId
            && SubjectPersonId == other.SubjectPersonId
            && SelectedCandidateId == other.SelectedCandidateId
            && ExpectedWorldDay == other.ExpectedWorldDay
            && StartAbsoluteDay == other.StartAbsoluteDay
            && string.Equals(
                ExpectedCandidateFingerprint,
                other.ExpectedCandidateFingerprint,
                StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as OfficeSuccessionTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = OfficeId != null ? OfficeId.GetHashCode() : 0;
            hash = (hash * 397) ^ (SubjectPersonId != null ? SubjectPersonId.GetHashCode() : 0);
            hash = (hash * 397) ^ (SelectedCandidateId != null ? SelectedCandidateId.GetHashCode() : 0);
            hash = (hash * 397) ^ ExpectedWorldDay.GetHashCode();
            hash = (hash * 397) ^ StartAbsoluteDay.GetHashCode();
            return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ExpectedCandidateFingerprint ?? string.Empty);
        }
    }
}

public enum EstateSuccessionFailureCode
{
    None = 0,
    InvalidWorld = 1,
    InvalidEstateId = 2,
    EstateNotFound = 3,
    InvalidPropertyId = 4,
    PropertyNotFound = 5,
    PropertyNotOwnedByEstate = 6,
    DeceasedPersonNotFactuallyDead = 7,
    InvalidSelectedCandidate = 8,
    CandidateNotEligible = 9,
    InvalidTransferDay = 10,
    InvalidTransition = 11,
    StaleWorldDay = 12,
    StaleEstate = 13,
    StaleCandidateSet = 14,
    CandidateNotRegistered = 15,
    PropertyTransferFailed = 16
}

public sealed class EstateSuccessionFailure : IEquatable<EstateSuccessionFailure>
{
    private static readonly EstateSuccessionFailure none =
        new EstateSuccessionFailure(EstateSuccessionFailureCode.None, string.Empty);

    private EstateSuccessionFailure(EstateSuccessionFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static EstateSuccessionFailure None => none;
    public EstateSuccessionFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != EstateSuccessionFailureCode.None;

    public static EstateSuccessionFailure Create(
        EstateSuccessionFailureCode code,
        string message)
    {
        return code == EstateSuccessionFailureCode.None
            ? None
            : new EstateSuccessionFailure(code, message);
    }

    public bool Equals(EstateSuccessionFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateSuccessionFailure);
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

public sealed class EstateSuccessionTransition : IEquatable<EstateSuccessionTransition>
{
    internal EstateRecord ExpectedEstate { get; }
    internal PersonRuntime ExpectedCandidate { get; }
    internal SuccessionCandidateSnapshot ExpectedCandidates { get; }
    internal PropertyOwnershipTransferTransition ExpectedPropertyTransfer { get; }

    public EstateId EstateId { get; }
    public PropertyId PropertyId { get; }
    public PersonId DeceasedPersonId { get; }
    public PersonId SelectedCandidateId { get; }
    public long ExpectedWorldDay { get; }
    public long TransferAbsoluteDay { get; }
    public long ExpectedEstateStoreRevision { get; }
    public string ExpectedCandidateFingerprint { get; }

    internal EstateSuccessionTransition(
        EstateRecord expectedEstate,
        PropertyId propertyId,
        PersonRuntime expectedCandidate,
        SuccessionCandidateSnapshot expectedCandidates,
        PropertyOwnershipTransferTransition expectedPropertyTransfer,
        long expectedWorldDay,
        long expectedEstateStoreRevision)
    {
        ExpectedEstate = expectedEstate;
        ExpectedCandidate = expectedCandidate;
        ExpectedCandidates = expectedCandidates;
        ExpectedPropertyTransfer = expectedPropertyTransfer;
        EstateId = expectedEstate?.EstateId;
        PropertyId = propertyId;
        DeceasedPersonId = expectedEstate?.DeceasedPersonId;
        SelectedCandidateId = expectedCandidate?.PersonId;
        ExpectedWorldDay = expectedWorldDay;
        TransferAbsoluteDay = expectedPropertyTransfer?.TransferAbsoluteDay ?? -1L;
        ExpectedEstateStoreRevision = expectedEstateStoreRevision;
        ExpectedCandidateFingerprint = expectedCandidates?.DiscoveryFingerprint;
    }

    public bool Equals(EstateSuccessionTransition other)
    {
        return other != null
            && EstateId == other.EstateId
            && PropertyId == other.PropertyId
            && DeceasedPersonId == other.DeceasedPersonId
            && SelectedCandidateId == other.SelectedCandidateId
            && ExpectedWorldDay == other.ExpectedWorldDay
            && TransferAbsoluteDay == other.TransferAbsoluteDay
            && ExpectedEstateStoreRevision == other.ExpectedEstateStoreRevision
            && string.Equals(
                ExpectedCandidateFingerprint,
                other.ExpectedCandidateFingerprint,
                StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateSuccessionTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = EstateId != null ? EstateId.GetHashCode() : 0;
            hash = (hash * 397) ^ (PropertyId != null ? PropertyId.GetHashCode() : 0);
            hash = (hash * 397) ^ (DeceasedPersonId != null ? DeceasedPersonId.GetHashCode() : 0);
            hash = (hash * 397) ^ (SelectedCandidateId != null ? SelectedCandidateId.GetHashCode() : 0);
            hash = (hash * 397) ^ ExpectedWorldDay.GetHashCode();
            hash = (hash * 397) ^ TransferAbsoluteDay.GetHashCode();
            return (hash * 397) ^ ExpectedEstateStoreRevision.GetHashCode();
        }
    }
}
