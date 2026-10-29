using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class ConflictId : IEquatable<ConflictId>
{
    public string Value { get; }

    public ConflictId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ConflictId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(ConflictId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as ConflictId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(ConflictId left, ConflictId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(ConflictId left, ConflictId right) => (left == right) == false;
}

public sealed class WarId : IEquatable<WarId>
{
    public string Value { get; }

    public WarId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("WarId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(WarId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as WarId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(WarId left, WarId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(WarId left, WarId right) => (left == right) == false;
}

public sealed class ConflictSideId : IEquatable<ConflictSideId>
{
    public string Value { get; }

    public ConflictSideId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ConflictSideId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(ConflictSideId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as ConflictSideId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(ConflictSideId left, ConflictSideId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(ConflictSideId left, ConflictSideId right) => (left == right) == false;
}

public sealed class WarSideId : IEquatable<WarSideId>
{
    public string Value { get; }

    public WarSideId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("WarSideId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(WarSideId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as WarSideId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(WarSideId left, WarSideId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(WarSideId left, WarSideId right) => (left == right) == false;
}

public sealed class ConflictParticipantBindingId : IEquatable<ConflictParticipantBindingId>
{
    public string Value { get; }

    public ConflictParticipantBindingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ConflictParticipantBindingId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(ConflictParticipantBindingId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as ConflictParticipantBindingId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(ConflictParticipantBindingId left, ConflictParticipantBindingId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(ConflictParticipantBindingId left, ConflictParticipantBindingId right) => (left == right) == false;
}

public sealed class WarParticipantBindingId : IEquatable<WarParticipantBindingId>
{
    public string Value { get; }

    public WarParticipantBindingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("WarParticipantBindingId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(WarParticipantBindingId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as WarParticipantBindingId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(WarParticipantBindingId left, WarParticipantBindingId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(WarParticipantBindingId left, WarParticipantBindingId right) => (left == right) == false;
}

public sealed class WarStrategicParticipantId : IEquatable<WarStrategicParticipantId>
{
    public string Value { get; }
    public WarStrategicParticipantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("WarStrategicParticipantId requires a non-empty value.", nameof(value));
        Value = value;
    }
    public bool Equals(WarStrategicParticipantId other) => other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as WarStrategicParticipantId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(WarStrategicParticipantId left, WarStrategicParticipantId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(WarStrategicParticipantId left, WarStrategicParticipantId right) => !(left == right);
}

public sealed class WarActualGoalId : IEquatable<WarActualGoalId>
{
    public string Value { get; }
    public WarActualGoalId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("WarActualGoalId requires a non-empty value.", nameof(value));
        Value = value;
    }
    public bool Equals(WarActualGoalId other) => other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as WarActualGoalId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(WarActualGoalId left, WarActualGoalId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(WarActualGoalId left, WarActualGoalId right) => !(left == right);
}

public sealed class WarStrategicParticipant
{
    public WarStrategicParticipantId Id { get; }
    public WarId WarId { get; }
    public FactionId FactionId { get; }
    public WarSideId SideId { get; }
    public WarStrategicParticipant(WarStrategicParticipantId id, WarId warId, FactionId factionId, WarSideId sideId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        WarId = warId ?? throw new ArgumentNullException(nameof(warId));
        FactionId = factionId ?? throw new ArgumentNullException(nameof(factionId));
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
    }
}

public sealed class WarWithdrawalDemand
{
    public WarActualGoalId Id { get; }
    public WarId WarId { get; }
    public WarStrategicParticipantId OwnerParticipantId { get; }
    public WarStrategicParticipantId TargetParticipantId { get; }
    public WarParticipantBindingId TargetBindingId { get; }
    public HexId SourceHexId { get; }
    public long ActivatedAbsoluteDay { get; }
    public WarWithdrawalDemand(WarActualGoalId id, WarId warId, WarStrategicParticipantId ownerParticipantId,
        WarStrategicParticipantId targetParticipantId, WarParticipantBindingId targetBindingId, HexId sourceHexId,
        long activatedAbsoluteDay)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        WarId = warId ?? throw new ArgumentNullException(nameof(warId));
        OwnerParticipantId = ownerParticipantId ?? throw new ArgumentNullException(nameof(ownerParticipantId));
        TargetParticipantId = targetParticipantId ?? throw new ArgumentNullException(nameof(targetParticipantId));
        TargetBindingId = targetBindingId ?? throw new ArgumentNullException(nameof(targetBindingId));
        SourceHexId = sourceHexId ?? throw new ArgumentNullException(nameof(sourceHexId));
        if (activatedAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(activatedAbsoluteDay));
        ActivatedAbsoluteDay = activatedAbsoluteDay;
    }
}

public enum WarConcessionReason { Concession = 0 }

public sealed class WarTerminalConcession
{
    public string OperationId { get; }
    public WarStrategicParticipantId ConcedingParticipantId { get; }
    public WarConcessionReason Reason { get; }
    public WorldCommandOrigin AcceptedOrigin { get; }
    public string AuthorityId { get; }
    public long AcceptedAbsoluteDay { get; }
    public long AcceptedOrder { get; }
    public WarTerminalConcession(string operationId, WarStrategicParticipantId concedingParticipantId,
        WarConcessionReason reason, WorldCommandOrigin acceptedOrigin, string authorityId, long acceptedAbsoluteDay, long acceptedOrder)
    {
        if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("A stable concession operation ID is required.", nameof(operationId));
        if (string.IsNullOrWhiteSpace(authorityId)) throw new ArgumentException("Accepted concession authority is required.", nameof(authorityId));
        if (concedingParticipantId == null) throw new ArgumentNullException(nameof(concedingParticipantId));
        if (!Enum.IsDefined(typeof(WarConcessionReason), reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (acceptedAbsoluteDay < 0L || acceptedOrder < 0L) throw new ArgumentOutOfRangeException(nameof(acceptedAbsoluteDay));
        if (!Enum.IsDefined(typeof(WorldCommandOrigin), acceptedOrigin)) throw new ArgumentOutOfRangeException(nameof(acceptedOrigin));
        OperationId = operationId; ConcedingParticipantId = concedingParticipantId; Reason = reason;
        AcceptedOrigin = acceptedOrigin; AuthorityId = authorityId; AcceptedAbsoluteDay = acceptedAbsoluteDay; AcceptedOrder = acceptedOrder;
    }
}

public sealed class P17AWarStrategicSection
{
    public string ScenarioAuthorityId { get; }
    public IReadOnlyList<WarStrategicParticipant> Participants { get; }
    public WarWithdrawalDemand WithdrawalDemand { get; }
    public WarTerminalConcession TerminalConcession { get; }
    public P17AWarStrategicSection(string scenarioAuthorityId, IEnumerable<WarStrategicParticipant> participants,
        WarWithdrawalDemand withdrawalDemand, WarTerminalConcession terminalConcession = null)
    {
        if (string.IsNullOrWhiteSpace(scenarioAuthorityId)) throw new ArgumentException("A stable scenario/GM authority ID is required.", nameof(scenarioAuthorityId));
        if (withdrawalDemand == null) throw new ArgumentNullException(nameof(withdrawalDemand));
        List<WarStrategicParticipant> values = participants == null ? new List<WarStrategicParticipant>() : new List<WarStrategicParticipant>(participants);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
        ScenarioAuthorityId = scenarioAuthorityId;
        Participants = new ReadOnlyCollection<WarStrategicParticipant>(values);
        WithdrawalDemand = withdrawalDemand;
        TerminalConcession = terminalConcession;
    }
    internal P17AWarStrategicSection WithTerminalConcession(WarTerminalConcession concession) =>
        new P17AWarStrategicSection(ScenarioAuthorityId, Participants, WithdrawalDemand, concession);
}

public enum ConflictLifecycleState
{
    Active = 0,
    Ended = 1
}

public enum WarLifecycleState
{
    Active = 0,
    Ended = 1
}

public sealed class ConflictStateSide
{
    public ConflictId ConflictId { get; }
    public ConflictSideId SideId { get; }
    public string DisplayName { get; }

    public ConflictStateSide(ConflictId conflictId, ConflictSideId sideId, string displayName = null)
    {
        ConflictId = conflictId ?? throw new ArgumentNullException(nameof(conflictId));
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        DisplayName = displayName ?? string.Empty;
    }
}

public sealed class WarStateSide
{
    public WarId WarId { get; }
    public WarSideId SideId { get; }
    public string DisplayName { get; }

    public WarStateSide(WarId warId, WarSideId sideId, string displayName = null)
    {
        WarId = warId ?? throw new ArgumentNullException(nameof(warId));
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        DisplayName = displayName ?? string.Empty;
    }
}

public sealed class ConflictParticipantBinding
{
    public ConflictParticipantBindingId BindingId { get; }
    public ConflictId ConflictId { get; }
    public ConflictSideId SideId { get; }
    public ArmedForceId ArmedForceId { get; }

    public ConflictParticipantBinding(
        ConflictParticipantBindingId bindingId,
        ConflictId conflictId,
        ConflictSideId sideId,
        ArmedForceId armedForceId)
    {
        BindingId = bindingId ?? throw new ArgumentNullException(nameof(bindingId));
        ConflictId = conflictId ?? throw new ArgumentNullException(nameof(conflictId));
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        ArmedForceId = armedForceId ?? throw new ArgumentNullException(nameof(armedForceId));
    }
}

public sealed class WarParticipantBinding
{
    public WarParticipantBindingId BindingId { get; }
    public WarId WarId { get; }
    public WarSideId SideId { get; }
    public ArmedForceId ArmedForceId { get; }

    public WarParticipantBinding(
        WarParticipantBindingId bindingId,
        WarId warId,
        WarSideId sideId,
        ArmedForceId armedForceId)
    {
        BindingId = bindingId ?? throw new ArgumentNullException(nameof(bindingId));
        WarId = warId ?? throw new ArgumentNullException(nameof(warId));
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        ArmedForceId = armedForceId ?? throw new ArgumentNullException(nameof(armedForceId));
    }
}

public sealed class PersistentConflictRecord
{
    public ConflictId Id { get; }
    public long CreatedAbsoluteDay { get; }
    public ConflictLifecycleState LifecycleState { get; }
    public long? EndedAbsoluteDay { get; }
    public IReadOnlyList<ConflictStateSide> Sides { get; }
    public IReadOnlyList<ConflictParticipantBinding> ParticipantBindings { get; }
    public bool IsActive => LifecycleState == ConflictLifecycleState.Active;

    public PersistentConflictRecord(
        ConflictId id,
        long createdAbsoluteDay,
        ConflictLifecycleState lifecycleState = ConflictLifecycleState.Active,
        long? endedAbsoluteDay = null,
        IEnumerable<ConflictStateSide> sides = null,
        IEnumerable<ConflictParticipantBinding> participantBindings = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ValidateLifecycleArguments(createdAbsoluteDay, lifecycleState, endedAbsoluteDay);
        CreatedAbsoluteDay = createdAbsoluteDay;
        LifecycleState = lifecycleState;
        EndedAbsoluteDay = endedAbsoluteDay;
        Sides = SortedCopy(sides, (left, right) => StringComparer.Ordinal.Compare(left?.SideId?.Value, right?.SideId?.Value));
        ParticipantBindings = SortedCopy(
            participantBindings,
            (left, right) => StringComparer.Ordinal.Compare(left?.BindingId?.Value, right?.BindingId?.Value));
    }

    internal PersistentConflictRecord WithParticipantBinding(ConflictParticipantBinding binding)
    {
        List<ConflictParticipantBinding> values = new List<ConflictParticipantBinding>(ParticipantBindings) { binding };
        return new PersistentConflictRecord(Id, CreatedAbsoluteDay, LifecycleState, EndedAbsoluteDay, Sides, values);
    }

    internal PersistentConflictRecord WithEnded(long endedAbsoluteDay)
    {
        return new PersistentConflictRecord(
            Id,
            CreatedAbsoluteDay,
            ConflictLifecycleState.Ended,
            endedAbsoluteDay,
            Sides,
            ParticipantBindings);
    }

    private static void ValidateLifecycleArguments(
        long createdAbsoluteDay,
        ConflictLifecycleState lifecycleState,
        long? endedAbsoluteDay)
    {
        if (createdAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        if (Enum.IsDefined(typeof(ConflictLifecycleState), lifecycleState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(lifecycleState));
        }

        if (lifecycleState == ConflictLifecycleState.Active && endedAbsoluteDay.HasValue)
        {
            throw new ArgumentException("An active Conflict cannot have an end day.", nameof(endedAbsoluteDay));
        }

        if (lifecycleState == ConflictLifecycleState.Ended
            && (!endedAbsoluteDay.HasValue || endedAbsoluteDay.Value < createdAbsoluteDay))
        {
            throw new ArgumentException("An ended Conflict requires an end day not before creation.", nameof(endedAbsoluteDay));
        }
    }

    private static IReadOnlyList<T> SortedCopy<T>(IEnumerable<T> source, Comparison<T> comparison)
    {
        List<T> values = source == null ? new List<T>() : new List<T>(source);
        values.Sort(comparison);
        return new ReadOnlyCollection<T>(values);
    }
}

public sealed class PersistentWarRecord
{
    public WarId Id { get; }
    public long CreatedAbsoluteDay { get; }
    public WarLifecycleState LifecycleState { get; }
    public long? EndedAbsoluteDay { get; }
    public ConflictId ConflictId { get; }
    public IReadOnlyList<WarStateSide> Sides { get; }
    public IReadOnlyList<WarParticipantBinding> ParticipantBindings { get; }
    public P17AWarStrategicSection P17A { get; }
    public bool IsActive => LifecycleState == WarLifecycleState.Active;

    public PersistentWarRecord(
        WarId id,
        long createdAbsoluteDay,
        ConflictId conflictId = null,
        WarLifecycleState lifecycleState = WarLifecycleState.Active,
        long? endedAbsoluteDay = null,
        IEnumerable<WarStateSide> sides = null,
        IEnumerable<WarParticipantBinding> participantBindings = null,
        P17AWarStrategicSection p17A = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ValidateLifecycleArguments(createdAbsoluteDay, lifecycleState, endedAbsoluteDay);
        CreatedAbsoluteDay = createdAbsoluteDay;
        LifecycleState = lifecycleState;
        EndedAbsoluteDay = endedAbsoluteDay;
        ConflictId = conflictId;
        Sides = SortedCopy(sides, (left, right) => StringComparer.Ordinal.Compare(left?.SideId?.Value, right?.SideId?.Value));
        ParticipantBindings = SortedCopy(
            participantBindings,
            (left, right) => StringComparer.Ordinal.Compare(left?.BindingId?.Value, right?.BindingId?.Value));
        P17A = p17A;
    }

    internal PersistentWarRecord WithParticipantBinding(WarParticipantBinding binding)
    {
        List<WarParticipantBinding> values = new List<WarParticipantBinding>(ParticipantBindings) { binding };
        return new PersistentWarRecord(Id, CreatedAbsoluteDay, ConflictId, LifecycleState, EndedAbsoluteDay, Sides, values, P17A);
    }

    internal PersistentWarRecord WithEnded(long endedAbsoluteDay)
    {
        return new PersistentWarRecord(
            Id,
            CreatedAbsoluteDay,
            ConflictId,
            WarLifecycleState.Ended,
            endedAbsoluteDay,
            Sides,
            ParticipantBindings,
            P17A);
    }

    internal PersistentWarRecord WithP17A(P17AWarStrategicSection value) =>
        new PersistentWarRecord(Id, CreatedAbsoluteDay, ConflictId, LifecycleState, EndedAbsoluteDay, Sides, ParticipantBindings, value);

    internal PersistentWarRecord WithP17AEnded(long endedAbsoluteDay, P17AWarStrategicSection value) =>
        new PersistentWarRecord(Id, CreatedAbsoluteDay, ConflictId, WarLifecycleState.Ended, endedAbsoluteDay, Sides, ParticipantBindings, value);

    private static void ValidateLifecycleArguments(
        long createdAbsoluteDay,
        WarLifecycleState lifecycleState,
        long? endedAbsoluteDay)
    {
        if (createdAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        if (Enum.IsDefined(typeof(WarLifecycleState), lifecycleState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(lifecycleState));
        }

        if (lifecycleState == WarLifecycleState.Active && endedAbsoluteDay.HasValue)
        {
            throw new ArgumentException("An active War cannot have an end day.", nameof(endedAbsoluteDay));
        }

        if (lifecycleState == WarLifecycleState.Ended
            && (!endedAbsoluteDay.HasValue || endedAbsoluteDay.Value < createdAbsoluteDay))
        {
            throw new ArgumentException("An ended War requires an end day not before creation.", nameof(endedAbsoluteDay));
        }
    }

    private static IReadOnlyList<T> SortedCopy<T>(IEnumerable<T> source, Comparison<T> comparison)
    {
        List<T> values = source == null ? new List<T>() : new List<T>(source);
        values.Sort(comparison);
        return new ReadOnlyCollection<T>(values);
    }
}

public sealed class PersistentWarStoreSnapshot
{
    public long Revision { get; }
    public IReadOnlyList<PersistentWarRecord> Records { get; }
    public PersistentWarStoreSnapshot(long revision, IEnumerable<PersistentWarRecord> records)
    {
        if (revision < 0L) throw new ArgumentOutOfRangeException(nameof(revision));
        List<PersistentWarRecord> values = records == null ? new List<PersistentWarRecord>() : new List<PersistentWarRecord>(records);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
        Revision = revision;
        Records = new ReadOnlyCollection<PersistentWarRecord>(values);
    }
}

public enum PersistentStateFailureCode
{
    None = 0,
    InvalidRecord = 1,
    DuplicateIdentity = 2,
    NotRegistered = 3,
    StateEnded = 4,
    InvalidLifecycle = 5,
    InvalidDay = 6,
    RequiresTwoSides = 7,
    InvalidSide = 8,
    DuplicateSide = 9,
    SideParentMismatch = 10,
    InvalidBinding = 11,
    DuplicateBinding = 12,
    BindingParentMismatch = 13,
    SideNotRegistered = 14,
    ForceNotRegistered = 15,
    ForceNotActive = 16,
    ConflictNotRegistered = 17,
    WarNotRegistered = 18,
    ContradictoryReference = 19,
    BattleResolutionDeferred = 20,
    RevisionOverflow = 21,
    BattleLocationRequired = 22,
    BattleLocationInvalid = 23,
    BattleLocationImmutable = 24,
    RuntimeFaulted = 25
}

public sealed class PersistentStateFailure : IEquatable<PersistentStateFailure>
{
    private static readonly PersistentStateFailure none =
        new PersistentStateFailure(PersistentStateFailureCode.None, string.Empty);

    private PersistentStateFailure(PersistentStateFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static PersistentStateFailure None => none;
    public PersistentStateFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != PersistentStateFailureCode.None;

    public static PersistentStateFailure Create(PersistentStateFailureCode code, string message)
    {
        return code == PersistentStateFailureCode.None
            ? None
            : new PersistentStateFailure(code, message);
    }

    public bool Equals(PersistentStateFailure other) => other != null
        && Code == other.Code
        && string.Equals(Message, other.Message, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as PersistentStateFailure);
    public override int GetHashCode() => ((int)Code * 397)
        ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code
        + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

public sealed class PersistentStateInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;
    public bool HasErrors => !IsValid;

    internal PersistentStateInvariantReport(IEnumerable<string> violations)
    {
        List<string> values = violations == null
            ? new List<string>()
            : new List<string>(violations);
        values.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(values);
    }
}
