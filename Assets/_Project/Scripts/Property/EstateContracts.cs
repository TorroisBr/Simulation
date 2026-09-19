using System;

/// <summary>
/// Stable semantic identity for one explicitly opened estate.
/// </summary>
public sealed class EstateId : IEquatable<EstateId>
{
    private readonly string value;

    public string Value => value;

    public EstateId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("EstateId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out EstateId estateId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            estateId = null;
            return false;
        }

        estateId = new EstateId(value);
        return true;
    }

    public bool Equals(EstateId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(EstateId left, EstateId right)
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

    public static bool operator !=(EstateId left, EstateId right)
    {
        return (left == right) == false;
    }
}

public enum EstateFoundationFailureCode
{
    None = 0,
    InvalidStore = 1,
    InvalidEstateId = 2,
    InvalidPersonId = 3,
    InvalidEstateRecord = 4,
    InvalidTransition = 5,
    InvalidOpeningDay = 6,
    PersonNotRegistered = 7,
    PersonStillLiving = 8,
    DuplicateEstateId = 9,
    EstateAlreadyExistsForPerson = 10,
    StalePersonRegistration = 11,
    StaleEstateStore = 12,
    RevisionOverflow = 13
}

/// <summary>
/// Structured failure for explicit estate opening operations.
/// </summary>
public sealed class EstateFoundationFailure : IEquatable<EstateFoundationFailure>
{
    private static readonly EstateFoundationFailure none =
        new EstateFoundationFailure(EstateFoundationFailureCode.None, string.Empty);

    private EstateFoundationFailure(EstateFoundationFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static EstateFoundationFailure None => none;
    public EstateFoundationFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != EstateFoundationFailureCode.None;

    public static EstateFoundationFailure Create(
        EstateFoundationFailureCode code,
        string message)
    {
        return code == EstateFoundationFailureCode.None
            ? None
            : new EstateFoundationFailure(code, message);
    }

    public bool Equals(EstateFoundationFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateFoundationFailure);
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
/// Minimal estate identity linked to one deceased Person. The Person death day
/// remains factual truth on PersonRuntime; this record stores only the explicit
/// opening and stable deceased Person linkage.
/// </summary>
public sealed class EstateRecord : IEquatable<EstateRecord>
{
    public EstateRecord(EstateId estateId, PersonId deceasedPersonId, long openedAbsoluteDay)
    {
        EstateId = estateId ?? throw new ArgumentNullException(nameof(estateId));
        DeceasedPersonId = deceasedPersonId
            ?? throw new ArgumentNullException(nameof(deceasedPersonId));

        if (openedAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(
                nameof(openedAbsoluteDay),
                "OpenedAbsoluteDay cannot be negative.");
        }

        OpenedAbsoluteDay = openedAbsoluteDay;
    }

    public EstateId EstateId { get; }
    public PersonId DeceasedPersonId { get; }
    public long OpenedAbsoluteDay { get; }

    public bool Equals(EstateRecord other)
    {
        return other != null
            && EstateId == other.EstateId
            && DeceasedPersonId == other.DeceasedPersonId
            && OpenedAbsoluteDay == other.OpenedAbsoluteDay;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateRecord);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = EstateId != null ? EstateId.GetHashCode() : 0;
            hash = (hash * 397)
                ^ (DeceasedPersonId != null ? DeceasedPersonId.GetHashCode() : 0);
            return (hash * 397) ^ OpenedAbsoluteDay.GetHashCode();
        }
    }
}

/// <summary>
/// Optimistic proposal for explicitly opening an estate for one factual dead
/// Person. Applying it revalidates both the Person registration and store.
/// </summary>
public sealed class EstateOpeningTransition : IEquatable<EstateOpeningTransition>
{
    internal PersonRuntime ExpectedPerson { get; }
    public EstateId EstateId { get; }
    public PersonId DeceasedPersonId { get; }
    public long ExpectedDeathAbsoluteDay { get; }
    public long OpeningAbsoluteDay { get; }
    public long ExpectedEstateStoreRevision { get; }

    internal EstateOpeningTransition(
        PersonRuntime expectedPerson,
        EstateId estateId,
        long openingAbsoluteDay,
        long expectedEstateStoreRevision)
    {
        ExpectedPerson = expectedPerson;
        EstateId = estateId;
        DeceasedPersonId = expectedPerson?.PersonId;
        ExpectedDeathAbsoluteDay = expectedPerson?.DeathAbsoluteDay ?? -1L;
        OpeningAbsoluteDay = openingAbsoluteDay;
        ExpectedEstateStoreRevision = expectedEstateStoreRevision;
    }

    public bool Equals(EstateOpeningTransition other)
    {
        return other != null
            && EstateId == other.EstateId
            && DeceasedPersonId == other.DeceasedPersonId
            && ExpectedDeathAbsoluteDay == other.ExpectedDeathAbsoluteDay
            && OpeningAbsoluteDay == other.OpeningAbsoluteDay
            && ExpectedEstateStoreRevision == other.ExpectedEstateStoreRevision;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateOpeningTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = EstateId != null ? EstateId.GetHashCode() : 0;
            hash = (hash * 397)
                ^ (DeceasedPersonId != null ? DeceasedPersonId.GetHashCode() : 0);
            hash = (hash * 397) ^ ExpectedDeathAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ OpeningAbsoluteDay.GetHashCode();
            return (hash * 397) ^ ExpectedEstateStoreRevision.GetHashCode();
        }
    }
}
