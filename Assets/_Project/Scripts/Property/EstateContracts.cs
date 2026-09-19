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
    InvalidStore,
    InvalidEstateId,
    InvalidPersonId,
    InvalidDeceasedPersonId,
    InvalidEstateRecord,
    InvalidTransition,
    InvalidOpeningDay,
    InvalidAbsoluteDay,
    PersonNotRegistered,
    PersonStillLiving,
    PersonNotDeceased,
    DeathAbsoluteDayInFuture,
    DuplicateEstateId,
    EstateAlreadyExistsForPerson,
    PersonAlreadyHasEstate,
    EstateNotFound,
    StalePersonRegistration,
    StaleEstateStore,
    StaleAbsoluteDay,
    RevisionOverflow
}

/// <summary>
/// Structured failure for expected estate registration and opening operations.
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
/// Immutable world-truth identity of an opened estate. This record contains no
/// inheritance decision, creditor state, tax state, or property transfer.
/// </summary>
public sealed class EstateRecord : IEquatable<EstateRecord>
{
    public EstateRecord(
        EstateId estateId,
        PersonId deceasedPersonId,
        long openedAbsoluteDay)
        : this(
            estateId,
            deceasedPersonId,
            openedAbsoluteDay,
            openedAbsoluteDay)
    {
    }

    public EstateRecord(
        EstateId estateId,
        PersonId deceasedPersonId,
        long deathAbsoluteDay,
        long createdAbsoluteDay)
    {
        EstateId = estateId ?? throw new ArgumentNullException(nameof(estateId));
        DeceasedPersonId = deceasedPersonId
            ?? throw new ArgumentNullException(nameof(deceasedPersonId));

        if (deathAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deathAbsoluteDay),
                "DeathAbsoluteDay cannot be negative.");
        }

        if (createdAbsoluteDay < deathAbsoluteDay)
        {
            throw new ArgumentException(
                "CreatedAbsoluteDay cannot precede DeathAbsoluteDay.",
                nameof(createdAbsoluteDay));
        }

        DeathAbsoluteDay = deathAbsoluteDay;
        CreatedAbsoluteDay = createdAbsoluteDay;
    }

    public EstateId EstateId { get; }

    public PersonId DeceasedPersonId { get; }

    public long DeathAbsoluteDay { get; }

    public long CreatedAbsoluteDay { get; }

    public long OpenedAbsoluteDay => CreatedAbsoluteDay;

    public bool Equals(EstateRecord other)
    {
        return other != null
            && EstateId == other.EstateId
            && DeceasedPersonId == other.DeceasedPersonId
            && DeathAbsoluteDay == other.DeathAbsoluteDay
            && CreatedAbsoluteDay == other.CreatedAbsoluteDay;
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
            hash = (hash * 397) ^ DeathAbsoluteDay.GetHashCode();
            return (hash * 397) ^ CreatedAbsoluteDay.GetHashCode();
        }
    }
}

/// <summary>
/// Optimistic proposal for explicitly creating an estate for one factual dead
/// Person. The expected Person reference and death day prevent applying a
/// proposal to another world or a stale Person registration.
/// </summary>
public sealed class EstateCreationTransition : IEquatable<EstateCreationTransition>
{
    internal PersonRuntime ExpectedPerson { get; }

    public EstateId EstateId { get; }

    public PersonId DeceasedPersonId { get; }

    public long ExpectedDeathAbsoluteDay { get; }

    public long CreatedAbsoluteDay { get; }

    public long OpeningAbsoluteDay => CreatedAbsoluteDay;

    public long ExpectedEstateStoreRevision { get; }

    internal EstateCreationTransition(
        PersonRuntime expectedPerson,
        EstateId estateId,
        long expectedDeathAbsoluteDay,
        long createdAbsoluteDay,
        long expectedEstateStoreRevision)
    {
        ExpectedPerson = expectedPerson;
        EstateId = estateId;
        DeceasedPersonId = expectedPerson?.PersonId;
        ExpectedDeathAbsoluteDay = expectedDeathAbsoluteDay;
        CreatedAbsoluteDay = createdAbsoluteDay;
        ExpectedEstateStoreRevision = expectedEstateStoreRevision;
    }

    public bool Equals(EstateCreationTransition other)
    {
        return other != null
            && EstateId == other.EstateId
            && DeceasedPersonId == other.DeceasedPersonId
            && ExpectedDeathAbsoluteDay == other.ExpectedDeathAbsoluteDay
            && CreatedAbsoluteDay == other.CreatedAbsoluteDay
            && ExpectedEstateStoreRevision == other.ExpectedEstateStoreRevision;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateCreationTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = EstateId != null ? EstateId.GetHashCode() : 0;
            hash = (hash * 397)
                ^ (DeceasedPersonId != null ? DeceasedPersonId.GetHashCode() : 0);
            hash = (hash * 397) ^ ExpectedDeathAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ CreatedAbsoluteDay.GetHashCode();
            return (hash * 397) ^ ExpectedEstateStoreRevision.GetHashCode();
        }
    }
}
