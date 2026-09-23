using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Stable semantic identity for an armed force. This is not a runtime object
/// identity and must survive representation, location, command, and ordinary
/// organizational changes.
/// </summary>
public sealed class ArmedForceId : IEquatable<ArmedForceId>
{
    private readonly string value;

    public string Value => value;

    public ArmedForceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ArmedForceId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public static bool TryCreate(string value, out ArmedForceId forceId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            forceId = null;
            return false;
        }

        forceId = new ArmedForceId(value);
        return true;
    }

    public bool Equals(ArmedForceId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForceId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;

    public static bool operator ==(ArmedForceId left, ArmedForceId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ArmedForceId left, ArmedForceId right) => (left == right) == false;
}

/// <summary>
/// Stable identity for an aggregate contingent. A contingent is not one
/// runtime object per individual and does not imply population recruitment.
/// </summary>
public sealed class ContingentId : IEquatable<ContingentId>
{
    private readonly string value;

    public string Value => value;

    public ContingentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ContingentId requires a non-empty value.", nameof(value));
        }

        this.value = value;
    }

    public bool Equals(ContingentId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ContingentId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;

    public static bool operator ==(ContingentId left, ContingentId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ContingentId left, ContingentId right) => (left == right) == false;
}

/// <summary>
/// Stable identity for a force/person/role reference relation.
/// </summary>
public sealed class ArmedForcePersonReferenceId : IEquatable<ArmedForcePersonReferenceId>
{
    private readonly string value;

    public string Value => value;

    public ArmedForcePersonReferenceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "ArmedForcePersonReferenceId requires a non-empty value.",
                nameof(value));
        }

        this.value = value;
    }

    public static ArmedForcePersonReferenceId BuildStableId(
        ArmedForceId forceId,
        PersonId personId,
        string roleKey)
    {
        if (forceId == null) throw new ArgumentNullException(nameof(forceId));
        if (personId == null) throw new ArgumentNullException(nameof(personId));
        if (string.IsNullOrWhiteSpace(roleKey)) throw new ArgumentException(
            "A relevant Person reference requires a non-empty role key.",
            nameof(roleKey));

        return new ArmedForcePersonReferenceId(
            "force-person:"
            + Segment(forceId.Value)
            + Segment(personId.Value)
            + Segment(roleKey));
    }

    private static string Segment(string value)
    {
        return value.Length + ":" + value;
    }

    public bool Equals(ArmedForcePersonReferenceId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForcePersonReferenceId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
    public override string ToString() => value;
}

public enum ArmedForceLifecycleState
{
    Active = 0,
    Terminated = 1
}

/// <summary>
/// Current authoritative state of one force. Parent and operational state are
/// data; hierarchy validity is owned and checked by ArmedForceStore.
/// </summary>
public sealed class ArmedForceRecord : IEquatable<ArmedForceRecord>
{
    public ArmedForceId Id { get; }
    public string DisplayName { get; }
    public long CreatedAbsoluteDay { get; }
    public ArmedForceId ParentForceId { get; }
    /// <summary>
    /// Legacy opaque compatibility data only. It is not authoritative physical
    /// position; typed current position is owned by ArmedForceSpatialStateStore.
    /// </summary>
    public string OperationalLocationReference { get; }
    public PersonId CommanderPersonId { get; }
    public ArmedForceLifecycleState LifecycleState { get; }
    public long? TerminatedAbsoluteDay { get; }
    public bool IsDetached { get; }
    public bool IsActive => LifecycleState == ArmedForceLifecycleState.Active;

    public ArmedForceRecord(
        ArmedForceId id,
        string displayName,
        long createdAbsoluteDay,
        ArmedForceId parentForceId = null,
        string operationalLocationReference = null,
        PersonId commanderPersonId = null,
        ArmedForceLifecycleState lifecycleState = ArmedForceLifecycleState.Active,
        long? terminatedAbsoluteDay = null,
        bool isDetached = false)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        if (createdAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        }

        if (Enum.IsDefined(typeof(ArmedForceLifecycleState), lifecycleState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(lifecycleState));
        }

        if (lifecycleState == ArmedForceLifecycleState.Active && terminatedAbsoluteDay.HasValue)
        {
            throw new ArgumentException(
                "An active ArmedForce cannot have a termination day.",
                nameof(terminatedAbsoluteDay));
        }

        if (lifecycleState == ArmedForceLifecycleState.Terminated
            && terminatedAbsoluteDay.HasValue == false)
        {
            throw new ArgumentException(
                "A terminated ArmedForce requires a termination day.",
                nameof(terminatedAbsoluteDay));
        }

        if (terminatedAbsoluteDay.HasValue
            && (terminatedAbsoluteDay.Value < 0L
                || terminatedAbsoluteDay.Value < createdAbsoluteDay))
        {
            throw new ArgumentOutOfRangeException(nameof(terminatedAbsoluteDay));
        }

        if (isDetached && parentForceId == null)
        {
            throw new ArgumentException(
                "A detached force must remain structurally subordinate to a parent force.",
                nameof(isDetached));
        }

        DisplayName = displayName ?? string.Empty;
        CreatedAbsoluteDay = createdAbsoluteDay;
        ParentForceId = parentForceId;
        OperationalLocationReference = NormalizeOptional(operationalLocationReference);
        CommanderPersonId = commanderPersonId;
        LifecycleState = lifecycleState;
        TerminatedAbsoluteDay = terminatedAbsoluteDay;
        IsDetached = isDetached;
    }

    internal ArmedForceRecord WithParent(ArmedForceId parentForceId)
    {
        return new ArmedForceRecord(
            Id,
            DisplayName,
            CreatedAbsoluteDay,
            parentForceId,
            OperationalLocationReference,
            CommanderPersonId,
            LifecycleState,
            TerminatedAbsoluteDay,
            false);
    }

    internal ArmedForceRecord WithOperationalLocation(string locationReference)
    {
        return new ArmedForceRecord(
            Id,
            DisplayName,
            CreatedAbsoluteDay,
            ParentForceId,
            locationReference,
            CommanderPersonId,
            LifecycleState,
            TerminatedAbsoluteDay,
            IsDetached);
    }

    internal ArmedForceRecord WithCommander(PersonId commanderPersonId)
    {
        return new ArmedForceRecord(
            Id,
            DisplayName,
            CreatedAbsoluteDay,
            ParentForceId,
            OperationalLocationReference,
            commanderPersonId,
            LifecycleState,
            TerminatedAbsoluteDay,
            IsDetached);
    }

    internal ArmedForceRecord WithDetached(bool isDetached)
    {
        return new ArmedForceRecord(
            Id,
            DisplayName,
            CreatedAbsoluteDay,
            ParentForceId,
            OperationalLocationReference,
            CommanderPersonId,
            LifecycleState,
            TerminatedAbsoluteDay,
            isDetached);
    }

    internal ArmedForceRecord WithTerminated(long terminatedAbsoluteDay)
    {
        return new ArmedForceRecord(
            Id,
            DisplayName,
            CreatedAbsoluteDay,
            ParentForceId,
            OperationalLocationReference,
            CommanderPersonId,
            ArmedForceLifecycleState.Terminated,
            terminatedAbsoluteDay,
            IsDetached);
    }

    private static string NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool Equals(ArmedForceRecord other)
    {
        return other != null
            && Id == other.Id
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && CreatedAbsoluteDay == other.CreatedAbsoluteDay
            && ParentForceId == other.ParentForceId
            && string.Equals(
                OperationalLocationReference,
                other.OperationalLocationReference,
                StringComparison.Ordinal)
            && CommanderPersonId == other.CommanderPersonId
            && LifecycleState == other.LifecycleState
            && TerminatedAbsoluteDay == other.TerminatedAbsoluteDay
            && IsDetached == other.IsDetached;
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForceRecord);
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Open-ended origin/provenance reference. Domain and value are content-owned
/// strings; this foundation does not assume civilian, human, levy, or any
/// other setting-specific source.
/// </summary>
public sealed class ContingentOriginReference : IEquatable<ContingentOriginReference>
{
    public string Domain { get; }
    public string Value { get; }

    public ContingentOriginReference(string domain, string value)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentException("A contingent origin requires a domain.", nameof(domain));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A contingent origin requires a value.", nameof(value));
        }

        Domain = domain;
        Value = value;
    }

    public bool Equals(ContingentOriginReference other)
    {
        return other != null
            && string.Equals(Domain, other.Domain, StringComparison.Ordinal)
            && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ContingentOriginReference);
    public override int GetHashCode() => (StringComparer.Ordinal.GetHashCode(Domain) * 397)
        ^ StringComparer.Ordinal.GetHashCode(Value);
}

/// <summary>
/// Extensible key/value characteristic for aggregate composition. Keys are
/// domain/content-defined and are stored in deterministic ordinal order.
/// </summary>
public sealed class ArmedForceCharacteristic : IEquatable<ArmedForceCharacteristic>
{
    public string Key { get; }
    public string Value { get; }

    public ArmedForceCharacteristic(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A force characteristic requires a key.", nameof(key));
        }

        Key = key;
        Value = value ?? string.Empty;
    }

    public bool Equals(ArmedForceCharacteristic other)
    {
        return other != null
            && string.Equals(Key, other.Key, StringComparison.Ordinal)
            && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForceCharacteristic);
    public override int GetHashCode() => (StringComparer.Ordinal.GetHashCode(Key) * 397)
        ^ StringComparer.Ordinal.GetHashCode(Value);
}

public sealed class ContingentRecord : IEquatable<ContingentRecord>
{
    public ContingentId Id { get; }
    public ArmedForceId ForceId { get; }
    public long Amount { get; }
    public ContingentOriginReference Origin { get; }
    public string ServiceType { get; }
    public IReadOnlyList<ArmedForceCharacteristic> Characteristics { get; }

    public ContingentRecord(
        ContingentId id,
        ArmedForceId forceId,
        long amount,
        ContingentOriginReference origin,
        string serviceType,
        IEnumerable<ArmedForceCharacteristic> characteristics = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        if (amount < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            throw new ArgumentException("A contingent requires an open-ended service type.", nameof(serviceType));
        }

        Amount = amount;
        ServiceType = serviceType;
        Characteristics = BuildCharacteristics(characteristics);
    }

    internal ContingentRecord WithComposition(
        long amount,
        ContingentOriginReference origin,
        string serviceType,
        IEnumerable<ArmedForceCharacteristic> characteristics)
    {
        return new ContingentRecord(Id, ForceId, amount, origin, serviceType, characteristics);
    }

    private static IReadOnlyList<ArmedForceCharacteristic> BuildCharacteristics(
        IEnumerable<ArmedForceCharacteristic> source)
    {
        List<ArmedForceCharacteristic> values = source == null
            ? new List<ArmedForceCharacteristic>()
            : new List<ArmedForceCharacteristic>(source);
        foreach (ArmedForceCharacteristic value in values)
        {
            if (value == null)
            {
                throw new ArgumentException(
                    "A contingent characteristic cannot be null.",
                    nameof(source));
            }
        }

        values.Sort((left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));
        for (int index = 1; index < values.Count; index++)
        {
            if (string.Equals(values[index - 1].Key, values[index].Key, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A contingent cannot contain duplicate characteristic keys.",
                    nameof(source));
            }
        }

        return new ReadOnlyCollection<ArmedForceCharacteristic>(values);
    }

    public bool Equals(ContingentRecord other)
    {
        if (other == null
            || Id != other.Id
            || ForceId != other.ForceId
            || Amount != other.Amount
            || !Equals(Origin, other.Origin)
            || !string.Equals(ServiceType, other.ServiceType, StringComparison.Ordinal)
            || Characteristics.Count != other.Characteristics.Count)
        {
            return false;
        }

        for (int index = 0; index < Characteristics.Count; index++)
        {
            if (!Equals(Characteristics[index], other.Characteristics[index])) return false;
        }

        return true;
    }

    public override bool Equals(object obj) => Equals(obj as ContingentRecord);
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed class ArmedForcePersonReference : IEquatable<ArmedForcePersonReference>
{
    public ArmedForcePersonReferenceId Id { get; }
    public ArmedForceId ForceId { get; }
    public PersonId PersonId { get; }
    public string RoleKey { get; }

    public ArmedForcePersonReference(
        ArmedForceId forceId,
        PersonId personId,
        string roleKey,
        ArmedForcePersonReferenceId referenceId = null)
    {
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        PersonId = personId ?? throw new ArgumentNullException(nameof(personId));
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            throw new ArgumentException("A relevant Person reference requires a role key.", nameof(roleKey));
        }

        RoleKey = roleKey;
        Id = referenceId ?? ArmedForcePersonReferenceId.BuildStableId(forceId, personId, roleKey);
    }

    public bool Equals(ArmedForcePersonReference other)
    {
        return other != null
            && Id.Equals(other.Id)
            && ForceId == other.ForceId
            && PersonId == other.PersonId
            && string.Equals(RoleKey, other.RoleKey, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForcePersonReference);
    public override int GetHashCode() => Id.GetHashCode();
}

public enum ArmedForceFoundationFailureCode
{
    None = 0,
    InvalidForce = 1,
    DuplicateForceId = 2,
    ForceNotRegistered = 3,
    ForceTerminated = 4,
    ParentNotRegistered = 5,
    ParentTerminated = 6,
    SelfParent = 7,
    ParentWouldCreateCycle = 8,
    ForceDetached = 9,
    ForceNotDetached = 10,
    DetachedRoot = 11,
    InvalidDay = 12,
    AlreadyTerminated = 13,
    ActiveChildrenPreventTermination = 14,
    PersonNotRegistered = 15,
    InvalidRelevantPerson = 16,
    DuplicateRelevantPerson = 17,
    InvalidContingent = 18,
    DuplicateContingentId = 19,
    ContingentForceMismatch = 20,
    ContingentForceNotActive = 21,
    ContingentNotRegistered = 22,
    RevisionOverflow = 23,
    AggregateAmountOverflow = 24,
    InvalidInvariant = 25,
    ContingentOriginMutation = 26,
    ContingentServiceTypeMutation = 27,
    ManpowerAuthorityRequired = 28,
    ManpowerAmountMutationRequired = 29,
    ForceHasManagedManpower = 30,
    ForceCustodiesManagedManpower = 31
}

public sealed class ArmedForceFoundationFailure : IEquatable<ArmedForceFoundationFailure>
{
    private static readonly ArmedForceFoundationFailure none =
        new ArmedForceFoundationFailure(ArmedForceFoundationFailureCode.None, string.Empty);

    private ArmedForceFoundationFailure(ArmedForceFoundationFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static ArmedForceFoundationFailure None => none;
    public ArmedForceFoundationFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != ArmedForceFoundationFailureCode.None;

    public static ArmedForceFoundationFailure Create(
        ArmedForceFoundationFailureCode code,
        string message)
    {
        return code == ArmedForceFoundationFailureCode.None
            ? None
            : new ArmedForceFoundationFailure(code, message);
    }

    public bool Equals(ArmedForceFoundationFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForceFoundationFailure);
    public override int GetHashCode() => ((int)Code * 397)
        ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code
        + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

public sealed class ArmedForceInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;
    public bool HasErrors => !IsValid;

    internal ArmedForceInvariantReport(IEnumerable<string> violations)
    {
        List<string> values = violations == null
            ? new List<string>()
            : new List<string>(violations);
        values.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(values);
    }
}
