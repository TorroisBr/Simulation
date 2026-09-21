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
    public FactionMembershipPolicy MembershipPolicy { get; }
    public bool ExpulsionAllowed { get; }

    public FactionRecord(
        FactionId id,
        string displayName,
        long createdAbsoluteDay,
        FactionMembershipPolicy membershipPolicy = FactionMembershipPolicy.LeaveAndRejoin,
        bool expulsionAllowed = true)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        if (createdAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        if (Enum.IsDefined(typeof(FactionMembershipPolicy), membershipPolicy) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(membershipPolicy));
        }
        DisplayName = displayName ?? string.Empty;
        CreatedAbsoluteDay = createdAbsoluteDay;
        MembershipPolicy = membershipPolicy;
        ExpulsionAllowed = expulsionAllowed;
    }

    public bool Equals(FactionRecord other)
    {
        return other != null && Id == other.Id
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && CreatedAbsoluteDay == other.CreatedAbsoluteDay
            && MembershipPolicy == other.MembershipPolicy
            && ExpulsionAllowed == other.ExpulsionAllowed;
    }

    public override bool Equals(object obj) => Equals(obj as FactionRecord);
    public override int GetHashCode() => Id.GetHashCode();
}

public enum FactionMembershipPolicy
{
    CannotLeave = 0,
    LeaveNoRejoin = 1,
    LeaveAndRejoin = 2
}

public sealed class FactionAffiliationId : IEquatable<FactionAffiliationId>
{
    public string Value { get; }

    public FactionAffiliationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("FactionAffiliationId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(FactionAffiliationId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as FactionAffiliationId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
}

public sealed class FactionAffiliationRecord : IEquatable<FactionAffiliationRecord>
{
    public FactionAffiliationId AffiliationId { get; }
    public FactionId FactionId { get; }
    public PersonId PersonId { get; }
    public long JoinedAbsoluteDay { get; }
    public long? EndedAbsoluteDay { get; }
    public bool IsActive => EndedAbsoluteDay.HasValue == false;

    public FactionAffiliationRecord(
        FactionId factionId,
        PersonId personId,
        long joinedAbsoluteDay,
        long? endedAbsoluteDay = null,
        FactionAffiliationId affiliationId = null)
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
        AffiliationId = affiliationId ?? BuildStableId(factionId, personId, joinedAbsoluteDay, 0L);
    }

    public static FactionAffiliationId BuildStableId(
        FactionId factionId,
        PersonId personId,
        long joinedAbsoluteDay,
        long allocationSequence)
    {
        if (factionId == null) throw new ArgumentNullException(nameof(factionId));
        if (personId == null) throw new ArgumentNullException(nameof(personId));
        if (joinedAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(joinedAbsoluteDay));
        if (allocationSequence < 0L) throw new ArgumentOutOfRangeException(nameof(allocationSequence));

        return new FactionAffiliationId(
            "affiliation:" + factionId.Value.Length + ":" + factionId.Value
            + personId.Value.Length + ":" + personId.Value
            + "@" + joinedAbsoluteDay + "#" + allocationSequence);
    }

    internal FactionAffiliationRecord WithEnd(long endedAbsoluteDay)
    {
        return new FactionAffiliationRecord(FactionId, PersonId, JoinedAbsoluteDay, endedAbsoluteDay, AffiliationId);
    }

    public bool Equals(FactionAffiliationRecord other)
    {
        return other != null && AffiliationId.Equals(other.AffiliationId)
            && FactionId == other.FactionId && PersonId == other.PersonId
            && JoinedAbsoluteDay == other.JoinedAbsoluteDay && EndedAbsoluteDay == other.EndedAbsoluteDay;
    }

    public override bool Equals(object obj) => Equals(obj as FactionAffiliationRecord);
    public override int GetHashCode() => AffiliationId.GetHashCode();
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
    WrongFactionStore = 14,
    VoluntaryLeaveNotAllowed = 15,
    ExpulsionNotAllowed = 16
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
