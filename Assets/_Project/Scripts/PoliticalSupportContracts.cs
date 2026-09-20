using System;

public sealed class PoliticalSupportRelationId : IEquatable<PoliticalSupportRelationId>
{
    private readonly string value;

    public string Value => value;

    public PoliticalSupportRelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("PoliticalSupportRelationId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out PoliticalSupportRelationId relationId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            relationId = null;
            return false;
        }

        relationId = new PoliticalSupportRelationId(value);
        return true;
    }

    public bool Equals(PoliticalSupportRelationId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalSupportRelationId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;

    public static bool operator ==(PoliticalSupportRelationId left, PoliticalSupportRelationId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(PoliticalSupportRelationId left, PoliticalSupportRelationId right) => (left == right) == false;
}

public enum PoliticalSupportSourceKind
{
    Person = 0,
    Faction = 1
}

public sealed class PoliticalSupportSource : IEquatable<PoliticalSupportSource>
{
    private readonly PersonId personId;
    private readonly FactionId factionId;

    public PoliticalSupportSourceKind Kind { get; }
    public PersonId PersonId => personId;
    public FactionId FactionId => factionId;
    public string Value => Kind == PoliticalSupportSourceKind.Person ? personId.Value : factionId.Value;

    private PoliticalSupportSource(PoliticalSupportSourceKind kind, PersonId personId, FactionId factionId)
    {
        if (Enum.IsDefined(typeof(PoliticalSupportSourceKind), kind) == false)
        {
            throw new ArgumentException("Political support source kind is invalid.", nameof(kind));
        }

        if (kind == PoliticalSupportSourceKind.Person && personId == null)
        {
            throw new ArgumentNullException(nameof(personId));
        }

        if (kind == PoliticalSupportSourceKind.Faction && factionId == null)
        {
            throw new ArgumentNullException(nameof(factionId));
        }

        Kind = kind;
        this.personId = personId;
        this.factionId = factionId;
    }

    public static PoliticalSupportSource ForPerson(PersonId personId)
    {
        return new PoliticalSupportSource(PoliticalSupportSourceKind.Person, personId, null);
    }

    public static PoliticalSupportSource ForFaction(FactionId factionId)
    {
        return new PoliticalSupportSource(PoliticalSupportSourceKind.Faction, null, factionId);
    }

    public bool Equals(PoliticalSupportSource other)
    {
        return other != null
            && Kind == other.Kind
            && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalSupportSource);
    public override int GetHashCode() => ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Kind + ":" + Value;
}

public enum PoliticalSupportTargetKind
{
    PoliticalClaim = 0,
    SuccessionCandidate = 1
}

public sealed class PoliticalSupportTarget : IEquatable<PoliticalSupportTarget>
{
    private readonly PoliticalClaimId politicalClaimId;
    private readonly PersonId successionCandidatePersonId;

    public PoliticalSupportTargetKind Kind { get; }
    public PoliticalClaimId PoliticalClaimId => politicalClaimId;
    public PersonId SuccessionCandidatePersonId => successionCandidatePersonId;
    public string Value => Kind == PoliticalSupportTargetKind.PoliticalClaim
        ? politicalClaimId.Value
        : successionCandidatePersonId.Value;

    private PoliticalSupportTarget(
        PoliticalSupportTargetKind kind,
        PoliticalClaimId politicalClaimId,
        PersonId successionCandidatePersonId)
    {
        if (Enum.IsDefined(typeof(PoliticalSupportTargetKind), kind) == false)
        {
            throw new ArgumentException("Political support target kind is invalid.", nameof(kind));
        }

        if (kind == PoliticalSupportTargetKind.PoliticalClaim && politicalClaimId == null)
        {
            throw new ArgumentNullException(nameof(politicalClaimId));
        }

        if (kind == PoliticalSupportTargetKind.SuccessionCandidate && successionCandidatePersonId == null)
        {
            throw new ArgumentNullException(nameof(successionCandidatePersonId));
        }

        Kind = kind;
        this.politicalClaimId = politicalClaimId;
        this.successionCandidatePersonId = successionCandidatePersonId;
    }

    public static PoliticalSupportTarget ForPoliticalClaim(PoliticalClaimId politicalClaimId)
    {
        return new PoliticalSupportTarget(PoliticalSupportTargetKind.PoliticalClaim, politicalClaimId, null);
    }

    public static PoliticalSupportTarget ForClaim(PoliticalClaimId politicalClaimId)
    {
        return ForPoliticalClaim(politicalClaimId);
    }

    public static PoliticalSupportTarget ForSuccessionCandidate(PersonId personId)
    {
        return new PoliticalSupportTarget(PoliticalSupportTargetKind.SuccessionCandidate, null, personId);
    }

    public bool Equals(PoliticalSupportTarget other)
    {
        return other != null
            && Kind == other.Kind
            && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalSupportTarget);
    public override int GetHashCode() => ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Kind + ":" + Value;
}

public enum PoliticalSupportDisposition
{
    Support = 0,
    Oppose = 1
}

public sealed class PoliticalSupportRelationRecord : IEquatable<PoliticalSupportRelationRecord>
{
    public PoliticalSupportRelationId RelationId { get; }
    public PoliticalSupportSource Source { get; }
    public PoliticalSupportTarget Target { get; }
    public PoliticalSupportDisposition Disposition { get; }
    public long StartedAbsoluteDay { get; }
    public long? EndedAbsoluteDay { get; }
    public bool IsActive => EndedAbsoluteDay.HasValue == false;

    public PoliticalSupportRelationRecord(
        PoliticalSupportRelationId relationId,
        PoliticalSupportSource source,
        PoliticalSupportTarget target,
        PoliticalSupportDisposition disposition,
        long startedAbsoluteDay,
        long? endedAbsoluteDay = null)
    {
        RelationId = relationId ?? throw new ArgumentNullException(nameof(relationId));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        if (Enum.IsDefined(typeof(PoliticalSupportDisposition), disposition) == false)
        {
            throw new ArgumentException("Political support disposition is invalid.", nameof(disposition));
        }

        if (startedAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(startedAbsoluteDay));
        }

        if (endedAbsoluteDay.HasValue
            && (endedAbsoluteDay.Value < startedAbsoluteDay || endedAbsoluteDay.Value < 0L))
        {
            throw new ArgumentOutOfRangeException(nameof(endedAbsoluteDay));
        }

        Disposition = disposition;
        StartedAbsoluteDay = startedAbsoluteDay;
        EndedAbsoluteDay = endedAbsoluteDay;
    }

    internal PoliticalSupportRelationRecord WithEnd(long endedAbsoluteDay)
    {
        return new PoliticalSupportRelationRecord(
            RelationId,
            Source,
            Target,
            Disposition,
            StartedAbsoluteDay,
            endedAbsoluteDay);
    }

    public bool Equals(PoliticalSupportRelationRecord other)
    {
        return other != null
            && RelationId == other.RelationId
            && Equals(Source, other.Source)
            && Equals(Target, other.Target)
            && Disposition == other.Disposition
            && StartedAbsoluteDay == other.StartedAbsoluteDay
            && EndedAbsoluteDay == other.EndedAbsoluteDay;
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalSupportRelationRecord);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = RelationId.GetHashCode();
            hash = (hash * 397) ^ Source.GetHashCode();
            hash = (hash * 397) ^ Target.GetHashCode();
            hash = (hash * 397) ^ (int)Disposition;
            return (hash * 397) ^ StartedAbsoluteDay.GetHashCode();
        }
    }
}

public enum PoliticalSupportFailureCode
{
    None = 0,
    InvalidRelationId = 1,
    DuplicateRelationId = 2,
    InvalidRelation = 3,
    InvalidDisposition = 4,
    InvalidStartedAbsoluteDay = 5,
    InvalidEndAbsoluteDay = 6,
    SourcePersonNotRegistered = 7,
    SourceFactionNotRegistered = 8,
    TargetClaimNotRegistered = 9,
    TargetCandidateNotRegistered = 10,
    DuplicateActiveRelation = 11,
    RelationNotRegistered = 12,
    RelationAlreadyEnded = 13,
    InvalidTransition = 14,
    StaleRelation = 15,
    WrongSupportStore = 16,
    RevisionOverflow = 17,
    ActiveRelationAlreadyExists = DuplicateActiveRelation,
    StaleWorldDay = 18
}

public sealed class PoliticalSupportFailure : IEquatable<PoliticalSupportFailure>
{
    private static readonly PoliticalSupportFailure none =
        new PoliticalSupportFailure(PoliticalSupportFailureCode.None, string.Empty);

    private PoliticalSupportFailure(PoliticalSupportFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static PoliticalSupportFailure None => none;
    public PoliticalSupportFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != PoliticalSupportFailureCode.None;

    public static PoliticalSupportFailure Create(PoliticalSupportFailureCode code, string message)
    {
        return code == PoliticalSupportFailureCode.None
            ? None
            : new PoliticalSupportFailure(code, message);
    }

    public bool Equals(PoliticalSupportFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as PoliticalSupportFailure);
    public override int GetHashCode() => ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}
