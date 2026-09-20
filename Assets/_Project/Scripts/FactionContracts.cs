using System;

public sealed class FactionId : IEquatable<FactionId>
{
    private readonly string value;

    public string Value => value;

    public FactionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("FactionId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out FactionId factionId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            factionId = null;
            return false;
        }

        factionId = new FactionId(value);
        return true;
    }

    public bool Equals(FactionId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as FactionId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;

    public static bool operator ==(FactionId left, FactionId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(FactionId left, FactionId right) => (left == right) == false;
}

public sealed class FactionRecord : IEquatable<FactionRecord>
{
    public FactionId Id { get; }
    public string DisplayName { get; }
    public long CreatedAbsoluteDay { get; }

    public FactionRecord(FactionId id, string displayName, long createdAbsoluteDay)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        if (createdAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        DisplayName = displayName ?? string.Empty;
        CreatedAbsoluteDay = createdAbsoluteDay;
    }

    public bool Equals(FactionRecord other)
    {
        return other != null && Id == other.Id
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && CreatedAbsoluteDay == other.CreatedAbsoluteDay;
    }

    public override bool Equals(object obj) => Equals(obj as FactionRecord);
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed class FactionAffiliationRecord : IEquatable<FactionAffiliationRecord>
{
    public FactionId FactionId { get; }
    public PersonId PersonId { get; }
    public long JoinedAbsoluteDay { get; }
    public long? EndedAbsoluteDay { get; }
    public bool IsActive => EndedAbsoluteDay.HasValue == false;

    public FactionAffiliationRecord(
        FactionId factionId,
        PersonId personId,
        long joinedAbsoluteDay,
        long? endedAbsoluteDay = null)
    {
        FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));
        if (joinedAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(joinedAbsoluteDay));
        if (endedAbsoluteDay.HasValue
            && (endedAbsoluteDay.Value < joinedAbsoluteDay || endedAbsoluteDay.Value < 0L))
        {
            throw new ArgumentOutOfRangeException(nameof(endedAbsoluteDay));
        }

        JoinedAbsoluteDay = joinedAbsoluteDay;
        EndedAbsoluteDay = endedAbsoluteDay;
    }

    internal FactionAffiliationRecord WithEnd(long endedAbsoluteDay)
    {
        return new FactionAffiliationRecord(FactionId, PersonId, JoinedAbsoluteDay, endedAbsoluteDay);
    }

    public bool Equals(FactionAffiliationRecord other)
    {
        return other != null && FactionId == other.FactionId && PersonId == other.PersonId
            && JoinedAbsoluteDay == other.JoinedAbsoluteDay && EndedAbsoluteDay == other.EndedAbsoluteDay;
    }

    public override bool Equals(object obj) => Equals(obj as FactionAffiliationRecord);
    public override int GetHashCode() => (FactionId.GetHashCode() * 397) ^ PersonId.GetHashCode();
}

public enum FactionFoundationFailureCode
{
    None = 0,
    InvalidFaction = 1,
    DuplicateFactionId = 2,
    FactionNotRegistered = 3,
    PersonNotRegistered = 4,
    InvalidCreationAbsoluteDay = 5,
    InvalidJoinAbsoluteDay = 6,
    InvalidEndAbsoluteDay = 7,
    DuplicateAffiliation = 8,
    AffiliationNotRegistered = 9,
    AffiliationAlreadyEnded = 10,
    InvalidTransition = 11,
    StaleAffiliation = 12,
    RevisionOverflow = 13,
    WrongFactionStore = 14
}

public sealed class FactionFoundationFailure : IEquatable<FactionFoundationFailure>
{
    private static readonly FactionFoundationFailure none =
        new FactionFoundationFailure(FactionFoundationFailureCode.None, string.Empty);

    private FactionFoundationFailure(FactionFoundationFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static FactionFoundationFailure None => none;
    public FactionFoundationFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != FactionFoundationFailureCode.None;

    public static FactionFoundationFailure Create(FactionFoundationFailureCode code, string message)
    {
        return code == FactionFoundationFailureCode.None ? None : new FactionFoundationFailure(code, message);
    }

    public bool Equals(FactionFoundationFailure other)
    {
        return other != null && Code == other.Code && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as FactionFoundationFailure);
    public override int GetHashCode() => ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}
