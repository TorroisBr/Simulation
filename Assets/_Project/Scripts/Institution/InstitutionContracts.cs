using System;

/// <summary>
/// Stable semantic identity for an institution in one simulation world.
/// </summary>
public sealed class InstitutionId : IEquatable<InstitutionId>
{
    private readonly string value;

    public string Value => value;

    public InstitutionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("InstitutionId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out InstitutionId institutionId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            institutionId = null;
            return false;
        }

        institutionId = new InstitutionId(value);
        return true;
    }

    public static bool TryCreate(
        string value,
        out InstitutionId institutionId,
        out InstitutionFoundationFailure failure)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            institutionId = null;
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidInstitutionId,
                "InstitutionId requires a non-empty value.");
            return false;
        }

        institutionId = new InstitutionId(value);
        failure = InstitutionFoundationFailure.None;
        return true;
    }

    public bool Equals(InstitutionId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as InstitutionId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(InstitutionId left, InstitutionId right)
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

    public static bool operator !=(InstitutionId left, InstitutionId right)
    {
        return (left == right) == false;
    }
}

/// <summary>
/// Stable semantic identity for an office within an institution.
/// </summary>
public sealed class OfficeId : IEquatable<OfficeId>
{
    private readonly string value;

    public string Value => value;

    public OfficeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("OfficeId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out OfficeId officeId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            officeId = null;
            return false;
        }

        officeId = new OfficeId(value);
        return true;
    }

    public static bool TryCreate(
        string value,
        out OfficeId officeId,
        out InstitutionFoundationFailure failure)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            officeId = null;
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidOfficeId,
                "OfficeId requires a non-empty value.");
            return false;
        }

        officeId = new OfficeId(value);
        failure = InstitutionFoundationFailure.None;
        return true;
    }

    public bool Equals(OfficeId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as OfficeId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(OfficeId left, OfficeId right)
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

    public static bool operator !=(OfficeId left, OfficeId right)
    {
        return (left == right) == false;
    }
}

public enum InstitutionFoundationFailureCode
{
    None = 0,
    InvalidInstitutionId,
    DuplicateInstitutionId,
    InstitutionNotFound,
    InvalidOfficeId,
    DuplicateOfficeId,
    OfficeNotFound,
    InstitutionNotFoundForOffice,
    InvalidInstitutionRecord,
    InvalidOfficeRecord,
    InvalidPersonId,
    OfficeAlreadyOccupied,
    OfficeAlreadyVacant,
    InvalidStartAbsoluteDay,
    PersonNotRegistered
}

/// <summary>
/// Structured failure returned by expected institution and office operations.
/// </summary>
public sealed class InstitutionFoundationFailure : IEquatable<InstitutionFoundationFailure>
{
    private static readonly InstitutionFoundationFailure none =
        new InstitutionFoundationFailure(InstitutionFoundationFailureCode.None, string.Empty);

    private InstitutionFoundationFailure(InstitutionFoundationFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static InstitutionFoundationFailure None => none;

    public InstitutionFoundationFailureCode Code { get; }

    public string Message { get; }

    public bool IsFailure => Code != InstitutionFoundationFailureCode.None;

    public static InstitutionFoundationFailure Create(
        InstitutionFoundationFailureCode code,
        string message)
    {
        if (code == InstitutionFoundationFailureCode.None)
        {
            return None;
        }

        return new InstitutionFoundationFailure(code, message);
    }

    public bool Equals(InstitutionFoundationFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as InstitutionFoundationFailure);
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

/// <summary>
/// Immutable institution identity and display metadata.
/// </summary>
public sealed class InstitutionRecord : IEquatable<InstitutionRecord>
{
    public InstitutionRecord(InstitutionId id, string displayName = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? string.Empty;
    }

    public InstitutionId Id { get; }

    public string DisplayName { get; }

    public bool Equals(InstitutionRecord other)
    {
        return other != null
            && Id == other.Id
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as InstitutionRecord);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Id != null ? Id.GetHashCode() : 0) * 397)
                ^ StringComparer.Ordinal.GetHashCode(DisplayName);
        }
    }
}

/// <summary>
/// Immutable office definition. Incumbency is controlled separately by OfficeStore.
/// </summary>
public sealed class OfficeRecord : IEquatable<OfficeRecord>
{
    public OfficeRecord(OfficeId id, InstitutionId institutionId, string displayName = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        InstitutionId = institutionId ?? throw new ArgumentNullException(nameof(institutionId));
        DisplayName = displayName ?? string.Empty;
    }

    public OfficeId Id { get; }

    public InstitutionId InstitutionId { get; }

    public string DisplayName { get; }

    public bool Equals(OfficeRecord other)
    {
        return other != null
            && Id == other.Id
            && InstitutionId == other.InstitutionId
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as OfficeRecord);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Id != null ? Id.GetHashCode() : 0;
            hash = (hash * 397) ^ (InstitutionId != null ? InstitutionId.GetHashCode() : 0);
            return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName);
        }
    }
}

/// <summary>
/// Immutable state for one occupied office. A vacant office has no instance of this type.
/// </summary>
public sealed class OfficeIncumbency : IEquatable<OfficeIncumbency>
{
    public OfficeIncumbency(OfficeId officeId, PersonId incumbent, long? startAbsoluteDay = null)
    {
        OfficeId = officeId ?? throw new ArgumentNullException(nameof(officeId));
        Incumbent = incumbent ?? throw new ArgumentNullException(nameof(incumbent));

        if (startAbsoluteDay.HasValue && startAbsoluteDay.Value < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(startAbsoluteDay), "StartAbsoluteDay cannot be negative.");
        }

        StartAbsoluteDay = startAbsoluteDay;
    }

    public OfficeId OfficeId { get; }

    public PersonId Incumbent { get; }

    public long? StartAbsoluteDay { get; }

    public bool Equals(OfficeIncumbency other)
    {
        return other != null
            && OfficeId == other.OfficeId
            && Incumbent == other.Incumbent
            && StartAbsoluteDay == other.StartAbsoluteDay;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as OfficeIncumbency);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = OfficeId != null ? OfficeId.GetHashCode() : 0;
            hash = (hash * 397) ^ (Incumbent != null ? Incumbent.GetHashCode() : 0);
            return (hash * 397) ^ (StartAbsoluteDay.HasValue ? StartAbsoluteDay.Value.GetHashCode() : 0);
        }
    }
}
