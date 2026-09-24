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

public sealed class BattleId : IEquatable<BattleId>
{
    public string Value { get; }

    public BattleId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("BattleId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(BattleId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as BattleId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(BattleId left, BattleId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(BattleId left, BattleId right) => (left == right) == false;
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

public sealed class BattleSideId : IEquatable<BattleSideId>
{
    public string Value { get; }

    public BattleSideId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("BattleSideId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(BattleSideId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as BattleSideId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(BattleSideId left, BattleSideId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(BattleSideId left, BattleSideId right) => (left == right) == false;
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

public sealed class BattleParticipantBindingId : IEquatable<BattleParticipantBindingId>
{
    public string Value { get; }

    public BattleParticipantBindingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("BattleParticipantBindingId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(BattleParticipantBindingId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as BattleParticipantBindingId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(BattleParticipantBindingId left, BattleParticipantBindingId right) => ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(BattleParticipantBindingId left, BattleParticipantBindingId right) => (left == right) == false;
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

public enum BattleLifecycleState
{
    Pending = 0,
    Active = 1,
    Resolved = 2
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

public sealed class BattleStateSide
{
    public BattleId BattleId { get; }
    public BattleSideId SideId { get; }
    public string DisplayName { get; }

    public BattleStateSide(BattleId battleId, BattleSideId sideId, string displayName = null)
    {
        BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
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

public sealed class BattleParticipantBinding
{
    public BattleParticipantBindingId BindingId { get; }
    public BattleId BattleId { get; }
    public BattleSideId SideId { get; }
    public ArmedForceId ArmedForceId { get; }

    public BattleParticipantBinding(
        BattleParticipantBindingId bindingId,
        BattleId battleId,
        BattleSideId sideId,
        ArmedForceId armedForceId)
    {
        BindingId = bindingId ?? throw new ArgumentNullException(nameof(bindingId));
        BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
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
    public bool IsActive => LifecycleState == WarLifecycleState.Active;

    public PersistentWarRecord(
        WarId id,
        long createdAbsoluteDay,
        ConflictId conflictId = null,
        WarLifecycleState lifecycleState = WarLifecycleState.Active,
        long? endedAbsoluteDay = null,
        IEnumerable<WarStateSide> sides = null,
        IEnumerable<WarParticipantBinding> participantBindings = null)
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
    }

    internal PersistentWarRecord WithParticipantBinding(WarParticipantBinding binding)
    {
        List<WarParticipantBinding> values = new List<WarParticipantBinding>(ParticipantBindings) { binding };
        return new PersistentWarRecord(Id, CreatedAbsoluteDay, ConflictId, LifecycleState, EndedAbsoluteDay, Sides, values);
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
            ParticipantBindings);
    }

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

public sealed class PersistentBattleRecord
{
    public BattleId Id { get; }
    public long CreatedAbsoluteDay { get; }
    public long? StartedAbsoluteDay { get; }
    public BattleLifecycleState LifecycleState { get; }
    public ConflictId ConflictId { get; }
    public WarId WarId { get; }
    public SpatialReference LocationReference { get; }
    public PersistentBattleTerminalOutcome TerminalOutcome { get; }
    public long? ResolvedAbsoluteDay => TerminalOutcome?.ResolvedAbsoluteDay;
    public IReadOnlyList<BattleStateSide> Sides { get; }
    public IReadOnlyList<BattleParticipantBinding> ParticipantBindings { get; }

    public PersistentBattleRecord(
        BattleId id,
        long createdAbsoluteDay,
        ConflictId conflictId = null,
        WarId warId = null,
        BattleLifecycleState lifecycleState = BattleLifecycleState.Pending,
        long? startedAbsoluteDay = null,
        IEnumerable<BattleStateSide> sides = null,
        IEnumerable<BattleParticipantBinding> participantBindings = null,
        SpatialReference locationReference = null)
        : this(
            id,
            createdAbsoluteDay,
            conflictId,
            warId,
            lifecycleState,
            startedAbsoluteDay,
            sides,
            participantBindings,
            locationReference,
            null)
    {
    }

    internal PersistentBattleRecord(
        BattleId id,
        long createdAbsoluteDay,
        ConflictId conflictId,
        WarId warId,
        BattleLifecycleState lifecycleState,
        long? startedAbsoluteDay,
        IEnumerable<BattleStateSide> sides,
        IEnumerable<BattleParticipantBinding> participantBindings,
        SpatialReference locationReference,
        PersistentBattleTerminalOutcome terminalOutcome)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        if (createdAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(createdAbsoluteDay));
        if (Enum.IsDefined(typeof(BattleLifecycleState), lifecycleState) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(lifecycleState));
        }

        if (startedAbsoluteDay.HasValue
            && (startedAbsoluteDay.Value < 0L || startedAbsoluteDay.Value < createdAbsoluteDay))
        {
            throw new ArgumentOutOfRangeException(nameof(startedAbsoluteDay));
        }

        CreatedAbsoluteDay = createdAbsoluteDay;
        StartedAbsoluteDay = startedAbsoluteDay;
        LifecycleState = lifecycleState;
        ConflictId = conflictId;
        WarId = warId;
        LocationReference = locationReference;
        TerminalOutcome = terminalOutcome;
        Sides = SortedCopy(sides, (left, right) => StringComparer.Ordinal.Compare(left?.SideId?.Value, right?.SideId?.Value));
        ParticipantBindings = SortedCopy(
            participantBindings,
            (left, right) => StringComparer.Ordinal.Compare(left?.BindingId?.Value, right?.BindingId?.Value));
    }

    internal PersistentBattleRecord WithStarted(long startedAbsoluteDay, SpatialReference locationReference)
    {
        return new PersistentBattleRecord(
            Id,
            CreatedAbsoluteDay,
            ConflictId,
            WarId,
            BattleLifecycleState.Active,
            startedAbsoluteDay,
            Sides,
            ParticipantBindings,
            locationReference,
            null);
    }

    internal PersistentBattleRecord WithParticipantBinding(BattleParticipantBinding binding)
    {
        List<BattleParticipantBinding> values = new List<BattleParticipantBinding>(ParticipantBindings) { binding };
        return new PersistentBattleRecord(
            Id,
            CreatedAbsoluteDay,
            ConflictId,
            WarId,
            LifecycleState,
            StartedAbsoluteDay,
            Sides,
            values,
            LocationReference,
            TerminalOutcome);
    }

    internal PersistentBattleRecord WithTerminalOutcome(PersistentBattleTerminalOutcome terminalOutcome)
    {
        return new PersistentBattleRecord(
            Id,
            CreatedAbsoluteDay,
            ConflictId,
            WarId,
            BattleLifecycleState.Resolved,
            StartedAbsoluteDay,
            Sides,
            ParticipantBindings,
            LocationReference,
            terminalOutcome);
    }

    private static IReadOnlyList<T> SortedCopy<T>(IEnumerable<T> source, Comparison<T> comparison)
    {
        List<T> values = source == null ? new List<T>() : new List<T>(source);
        values.Sort(comparison);
        return new ReadOnlyCollection<T>(values);
    }
}

/// <summary>
/// Stable, immutable D5+D6B2 provenance accepted with one persistent Battle
/// outcome. It deliberately contains no live rules, plans, or runtime objects.
/// </summary>
public sealed class PersistentBattleOutcomeProvenance
{
    public BattleResolutionProvenance D5Resolution { get; }
    public string D6B2PolicyFingerprint { get; }
    public string D6B2PlanSchemaVersion { get; }
    public string D6B2CoverageVersion { get; }
    public string D6B2PlanFingerprint { get; }

    internal PersistentBattleOutcomeProvenance(
        BattleResolutionProvenance d5Resolution,
        string d6b2PolicyFingerprint,
        string d6b2PlanSchemaVersion,
        string d6b2CoverageVersion,
        string d6b2PlanFingerprint)
    {
        D5Resolution = d5Resolution ?? throw new ArgumentNullException(nameof(d5Resolution));
        D6B2PolicyFingerprint = RequireStableIdentity(d6b2PolicyFingerprint, nameof(d6b2PolicyFingerprint));
        D6B2PlanSchemaVersion = RequireStableIdentity(d6b2PlanSchemaVersion, nameof(d6b2PlanSchemaVersion));
        D6B2CoverageVersion = RequireStableIdentity(d6b2CoverageVersion, nameof(d6b2CoverageVersion));
        D6B2PlanFingerprint = RequireStableIdentity(d6b2PlanFingerprint, nameof(d6b2PlanFingerprint));
    }

    private static string RequireStableIdentity(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Accepted Battle provenance must be explicit and non-empty.", parameterName);
        return value;
    }
}

/// <summary>The one immutable terminal Battle outcome persisted by its owning store.</summary>
public sealed class PersistentBattleTerminalOutcome
{
    public BattleId BattleId { get; }
    public BattleOutcomeType OutcomeType { get; }
    public BattleSideId WinningBattleSideId { get; }
    public long ResolvedAbsoluteDay { get; }
    public PersistentBattleOutcomeProvenance Provenance { get; }

    internal PersistentBattleTerminalOutcome(
        BattleId battleId,
        BattleOutcomeType outcomeType,
        BattleSideId winningBattleSideId,
        long resolvedAbsoluteDay,
        PersistentBattleOutcomeProvenance provenance)
    {
        BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
        if (!Enum.IsDefined(typeof(BattleOutcomeType), outcomeType))
            throw new ArgumentOutOfRangeException(nameof(outcomeType));
        if (outcomeType == BattleOutcomeType.Victory && winningBattleSideId == null)
            throw new ArgumentException("A Battle victory requires a registered winning side.", nameof(winningBattleSideId));
        if (outcomeType == BattleOutcomeType.Draw && winningBattleSideId != null)
            throw new ArgumentException("A Battle draw cannot have a winning side.", nameof(winningBattleSideId));
        if (resolvedAbsoluteDay < 0L)
            throw new ArgumentOutOfRangeException(nameof(resolvedAbsoluteDay));
        OutcomeType = outcomeType;
        WinningBattleSideId = winningBattleSideId;
        ResolvedAbsoluteDay = resolvedAbsoluteDay;
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
    }
}

internal sealed class PreparedBattleTerminalWrite
{
    internal PersistentBattleRecord ExpectedRecord { get; }
    internal PersistentBattleRecord TerminalRecord { get; }
    internal long ExpectedStoreRevision { get; }

    internal PreparedBattleTerminalWrite(
        PersistentBattleRecord expectedRecord,
        PersistentBattleRecord terminalRecord,
        long expectedStoreRevision)
    {
        ExpectedRecord = expectedRecord;
        TerminalRecord = terminalRecord;
        ExpectedStoreRevision = expectedStoreRevision;
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
