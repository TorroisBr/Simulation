using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum BattleOutcomeType
{
    Victory = 0,
    Draw = 1
}

/// <summary>Minimal immutable D5 resolution identity retained with an accepted Battle outcome.</summary>
public sealed class BattleResolutionProvenance
{
    internal BattleResolutionProvenance(
        string policyFingerprint,
        string numericExecutionProfileKey,
        string projectionVersion,
        string causalResolutionFingerprint,
        string sourceContextFingerprint,
        string capabilityRuleKey,
        string randomAuthorityRuleKey,
        string resolverSettingsIdentity)
    {
        PolicyFingerprint = policyFingerprint;
        NumericExecutionProfileKey = numericExecutionProfileKey;
        ProjectionVersion = projectionVersion;
        CausalResolutionFingerprint = causalResolutionFingerprint;
        SourceContextFingerprint = sourceContextFingerprint;
        CapabilityRuleKey = capabilityRuleKey;
        RandomAuthorityRuleKey = randomAuthorityRuleKey;
        ResolverSettingsIdentity = resolverSettingsIdentity;
    }

    public string PolicyFingerprint { get; }
    public string NumericExecutionProfileKey { get; }
    public string ProjectionVersion { get; }
    public string CausalResolutionFingerprint { get; }
    public string SourceContextFingerprint { get; }
    public string CapabilityRuleKey { get; }
    public string RandomAuthorityRuleKey { get; }
    public string ResolverSettingsIdentity { get; }
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

public enum BattleLifecycleState
{
    Pending = 0,
    Active = 1,
    Resolved = 2
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
