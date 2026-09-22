using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

public enum BattleExecutionFailureCode
{
    None = 0,
    BattleNotRegistered = 1,
    BattleNotActive = 2,
    InvalidExecutionDay = 3,
    BattleNotStarted = 4,
    BattleLocationInvalid = 5,
    RequiresTwoSides = 6,
    InvalidParticipantBinding = 7,
    ParticipantForceMissing = 8,
    ParticipantForceTerminated = 9,
    ParticipantPositionMissing = 10,
    ParticipantPositionInvalid = 11,
    ParticipantSpatiallyIncompatible = 12,
    DuplicateParticipantForce = 13,
    DuplicateContingentProjection = 14,
    SideHasNoCombatElements = 15,
    InvalidSideCommander = 16,
    InvalidExecutionPlan = 17,
    ContextMalformed = 18,
    ContextStale = 19,
    ExecutionDayChanged = 20,
    BattleLocationChanged = 21,
    ParticipantBindingsChanged = 22,
    ForceTerminated = 23,
    ForcePositionChanged = 24,
    ForceNoLongerSpatiallyCompatible = 25,
    DirectContingentCompositionChanged = 26,
    ReferencedSpatialStateInvalid = 27
}

public sealed class BattleExecutionFailure : IEquatable<BattleExecutionFailure>
{
    private static readonly BattleExecutionFailure none =
        new BattleExecutionFailure(BattleExecutionFailureCode.None, string.Empty);

    private BattleExecutionFailure(BattleExecutionFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static BattleExecutionFailure None => none;
    public BattleExecutionFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != BattleExecutionFailureCode.None;

    internal static BattleExecutionFailure Create(
        BattleExecutionFailureCode code,
        string message)
    {
        return code == BattleExecutionFailureCode.None
            ? None
            : new BattleExecutionFailure(code, message);
    }

    public bool Equals(BattleExecutionFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as BattleExecutionFailure);
    public override int GetHashCode() => ((int)Code * 397)
        ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code
        + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

public sealed class BattleExecutionSideCommander
{
    public BattleSideId SideId { get; }
    public PersonId SideCommanderPersonId { get; }

    public BattleExecutionSideCommander(
        BattleSideId sideId,
        PersonId sideCommanderPersonId)
    {
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        SideCommanderPersonId = sideCommanderPersonId;
    }
}

/// <summary>
/// Ephemeral caller-supplied metadata for one execution attempt. It is not a
/// Battle order, operational group, political office, allegiance, or store
/// owned state.
/// </summary>
public sealed class BattleExecutionPlan
{
    private readonly IReadOnlyList<BattleExecutionSideCommander> sideCommanders;

    public BattleExecutionPlan(
        IEnumerable<BattleExecutionSideCommander> sideCommanders = null)
    {
        List<BattleExecutionSideCommander> values = sideCommanders == null
            ? new List<BattleExecutionSideCommander>()
            : new List<BattleExecutionSideCommander>(sideCommanders);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.SideId?.Value,
            right?.SideId?.Value));
        this.sideCommanders = new ReadOnlyCollection<BattleExecutionSideCommander>(values);
    }

    public static BattleExecutionPlan Empty => new BattleExecutionPlan();
    public IReadOnlyList<BattleExecutionSideCommander> SideCommanders => sideCommanders;
}

public sealed class BattleExecutionContingentSnapshot
{
    public ContingentId ContingentId { get; }
    public ArmedForceId ForceId { get; }
    public long Amount { get; }
    public ContingentOriginReference Origin { get; }
    public string ServiceType { get; }
    public IReadOnlyList<ArmedForceCharacteristic> Characteristics { get; }

    internal BattleExecutionContingentSnapshot(ContingentRecord contingent)
    {
        if (contingent == null) throw new ArgumentNullException(nameof(contingent));
        ContingentId = contingent.Id;
        ForceId = contingent.ForceId;
        Amount = contingent.Amount;
        Origin = contingent.Origin;
        ServiceType = contingent.ServiceType;
        Characteristics = new ReadOnlyCollection<ArmedForceCharacteristic>(
            new List<ArmedForceCharacteristic>(contingent.Characteristics));
    }

    public string StableKey => ContingentId.Value;
    public bool HasUsableCombatElements => Amount > 0L;
}

public sealed class BattleExecutionForceContext
{
    public ArmedForceId ForceId { get; }
    public SpatialReference CurrentPosition { get; }
    public IReadOnlyList<BattleExecutionContingentSnapshot> DirectContingents { get; }

    internal BattleExecutionForceContext(
        ArmedForceId forceId,
        SpatialReference currentPosition,
        IEnumerable<BattleExecutionContingentSnapshot> directContingents)
    {
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        CurrentPosition = currentPosition ?? throw new ArgumentNullException(nameof(currentPosition));
        List<BattleExecutionContingentSnapshot> values = directContingents == null
            ? new List<BattleExecutionContingentSnapshot>()
            : new List<BattleExecutionContingentSnapshot>(directContingents);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.ContingentId?.Value,
            right?.ContingentId?.Value));
        DirectContingents = new ReadOnlyCollection<BattleExecutionContingentSnapshot>(values);
    }

    public bool HasUsableDirectCombatElements
    {
        get
        {
            foreach (BattleExecutionContingentSnapshot contingent in DirectContingents)
            {
                if (contingent != null && contingent.HasUsableCombatElements) return true;
            }

            return false;
        }
    }
}

public sealed class BattleExecutionSideContext
{
    public BattleSideId SideId { get; }
    public PersonId SideCommanderPersonId { get; }
    public IReadOnlyList<BattleExecutionForceContext> Forces { get; }

    internal BattleExecutionSideContext(
        BattleSideId sideId,
        PersonId sideCommanderPersonId,
        IEnumerable<BattleExecutionForceContext> forces)
    {
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        SideCommanderPersonId = sideCommanderPersonId;
        List<BattleExecutionForceContext> values = forces == null
            ? new List<BattleExecutionForceContext>()
            : new List<BattleExecutionForceContext>(forces);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.ForceId?.Value,
            right?.ForceId?.Value));
        Forces = new ReadOnlyCollection<BattleExecutionForceContext>(values);
    }

    public bool HasUsableDirectCombatElements
    {
        get
        {
            foreach (BattleExecutionForceContext force in Forces)
            {
                if (force != null && force.HasUsableDirectCombatElements) return true;
            }

            return false;
        }
    }
}

public sealed class BattleExecutionParticipantFingerprint
{
    internal BattleExecutionParticipantFingerprint(
        BattleParticipantBindingId bindingId,
        BattleSideId sideId,
        ArmedForceId forceId)
    {
        BindingId = bindingId;
        SideId = sideId;
        ForceId = forceId;
    }

    public BattleParticipantBindingId BindingId { get; }
    public BattleSideId SideId { get; }
    public ArmedForceId ForceId { get; }
}

public sealed class BattleExecutionContingentFingerprint
{
    internal BattleExecutionContingentFingerprint(ContingentRecord contingent)
    {
        ContingentId = contingent.Id;
        ForceId = contingent.ForceId;
        Amount = contingent.Amount;
        Origin = contingent.Origin;
        ServiceType = contingent.ServiceType;
        Characteristics = new ReadOnlyCollection<ArmedForceCharacteristic>(
            new List<ArmedForceCharacteristic>(contingent.Characteristics));
    }

    public ContingentId ContingentId { get; }
    public ArmedForceId ForceId { get; }
    public long Amount { get; }
    public ContingentOriginReference Origin { get; }
    public string ServiceType { get; }
    public IReadOnlyList<ArmedForceCharacteristic> Characteristics { get; }
}

public sealed class BattleExecutionForceFingerprint
{
    internal BattleExecutionForceFingerprint(
        ArmedForceRecord force,
        IEnumerable<BattleExecutionContingentFingerprint> contingents)
    {
        ForceId = force.Id;
        LifecycleState = force.LifecycleState;
        TerminatedAbsoluteDay = force.TerminatedAbsoluteDay;
        List<BattleExecutionContingentFingerprint> values = contingents == null
            ? new List<BattleExecutionContingentFingerprint>()
            : new List<BattleExecutionContingentFingerprint>(contingents);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.ContingentId?.Value,
            right?.ContingentId?.Value));
        DirectContingents = new ReadOnlyCollection<BattleExecutionContingentFingerprint>(values);
    }

    public ArmedForceId ForceId { get; }
    public ArmedForceLifecycleState LifecycleState { get; }
    public long? TerminatedAbsoluteDay { get; }
    public IReadOnlyList<BattleExecutionContingentFingerprint> DirectContingents { get; }
}

public sealed class BattleExecutionPositionFingerprint
{
    internal BattleExecutionPositionFingerprint(ArmedForceId forceId, SpatialReference position)
    {
        ForceId = forceId;
        PositionStableKey = position?.StableKey;
    }

    public ArmedForceId ForceId { get; }
    public string PositionStableKey { get; }
}

/// <summary>
/// Stable, semantic dependency capture for one ephemeral execution context.
/// Store revisions and unrelated world records are intentionally absent.
/// </summary>
public sealed class BattleExecutionDependencyFingerprint
{
    internal BattleExecutionDependencyFingerprint(
        BattleId battleId,
        BattleLifecycleState lifecycleState,
        long? startedAbsoluteDay,
        string battleLocationStableKey,
        IEnumerable<BattleSideId> sideIds,
        IEnumerable<BattleExecutionParticipantFingerprint> participants,
        IEnumerable<BattleExecutionForceFingerprint> forces,
        IEnumerable<BattleExecutionPositionFingerprint> positions,
        string planFingerprint)
    {
        BattleId = battleId;
        LifecycleState = lifecycleState;
        StartedAbsoluteDay = startedAbsoluteDay;
        BattleLocationStableKey = battleLocationStableKey;
        SideIds = SortedStrings(sideIds, side => side?.Value);
        Participants = SortedCopy(participants, (left, right) => StringComparer.Ordinal.Compare(
            left?.BindingId?.Value,
            right?.BindingId?.Value));
        Forces = SortedCopy(forces, (left, right) => StringComparer.Ordinal.Compare(
            left?.ForceId?.Value,
            right?.ForceId?.Value));
        Positions = SortedCopy(positions, (left, right) => StringComparer.Ordinal.Compare(
            left?.ForceId?.Value,
            right?.ForceId?.Value));
        PlanFingerprint = planFingerprint ?? string.Empty;
        StableKey = BuildStableKey();
    }

    public BattleId BattleId { get; }
    public BattleLifecycleState LifecycleState { get; }
    public long? StartedAbsoluteDay { get; }
    public string BattleLocationStableKey { get; }
    public IReadOnlyList<string> SideIds { get; }
    public IReadOnlyList<BattleExecutionParticipantFingerprint> Participants { get; }
    public IReadOnlyList<BattleExecutionForceFingerprint> Forces { get; }
    public IReadOnlyList<BattleExecutionPositionFingerprint> Positions { get; }
    public string PlanFingerprint { get; }
    public string StableKey { get; }

    private string BuildStableKey()
    {
        StringBuilder builder = new StringBuilder();
        Append(builder, BattleId?.Value);
        Append(builder, LifecycleState.ToString());
        Append(builder, StartedAbsoluteDay.HasValue ? Invariant(StartedAbsoluteDay.Value) : null);
        Append(builder, BattleLocationStableKey);
        foreach (string sideId in SideIds) Append(builder, sideId);
        foreach (BattleExecutionParticipantFingerprint participant in Participants)
        {
            Append(builder, participant?.BindingId?.Value);
            Append(builder, participant?.SideId?.Value);
            Append(builder, participant?.ForceId?.Value);
        }

        foreach (BattleExecutionForceFingerprint force in Forces)
        {
            Append(builder, force?.ForceId?.Value);
            Append(builder, force?.LifecycleState.ToString());
            Append(builder, force?.TerminatedAbsoluteDay.HasValue == true
                ? Invariant(force.TerminatedAbsoluteDay.Value)
                : null);
            foreach (BattleExecutionContingentFingerprint contingent in force?.DirectContingents
                ?? new List<BattleExecutionContingentFingerprint>())
            {
                Append(builder, contingent?.ContingentId?.Value);
                Append(builder, contingent?.ForceId?.Value);
                Append(builder, contingent == null ? null : Invariant(contingent.Amount));
                Append(builder, contingent?.Origin?.Domain);
                Append(builder, contingent?.Origin?.Value);
                Append(builder, contingent?.ServiceType);
                foreach (ArmedForceCharacteristic characteristic in contingent?.Characteristics
                    ?? new List<ArmedForceCharacteristic>())
                {
                    Append(builder, characteristic?.Key);
                    Append(builder, characteristic?.Value);
                }
            }
        }

        foreach (BattleExecutionPositionFingerprint position in Positions)
        {
            Append(builder, position?.ForceId?.Value);
            Append(builder, position?.PositionStableKey);
        }

        Append(builder, PlanFingerprint);
        return builder.ToString();
    }

    private static IReadOnlyList<string> SortedStrings<T>(
        IEnumerable<T> source,
        Func<T, string> selector)
    {
        List<string> values = new List<string>();
        if (source != null)
        {
            foreach (T value in source) values.Add(selector(value));
        }

        values.Sort(StringComparer.Ordinal);
        return new ReadOnlyCollection<string>(values);
    }

    private static IReadOnlyList<T> SortedCopy<T>(
        IEnumerable<T> source,
        Comparison<T> comparison)
    {
        List<T> values = source == null ? new List<T>() : new List<T>(source);
        values.Sort(comparison);
        return new ReadOnlyCollection<T>(values);
    }

    private static void Append(StringBuilder builder, string value)
    {
        if (value == null)
        {
            builder.Append("-;");
            return;
        }

        builder.Append(value.Length).Append(':').Append(value).Append(';');
    }

    private static string Invariant(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}

public sealed class BattleExecutionContext
{
    internal BattleExecutionContext(
        BattleId battleId,
        long executionAbsoluteDay,
        SpatialReference battleLocation,
        BattleExecutionPlan plan,
        IEnumerable<BattleExecutionSideContext> sides,
        BattleExecutionDependencyFingerprint dependencies)
    {
        BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
        ExecutionAbsoluteDay = executionAbsoluteDay;
        BattleLocation = battleLocation ?? throw new ArgumentNullException(nameof(battleLocation));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        List<BattleExecutionSideContext> values = sides == null
            ? new List<BattleExecutionSideContext>()
            : new List<BattleExecutionSideContext>(sides);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.SideId?.Value,
            right?.SideId?.Value));
        Sides = new ReadOnlyCollection<BattleExecutionSideContext>(values);
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    }

    public BattleId BattleId { get; }
    public long ExecutionAbsoluteDay { get; }
    public SpatialReference BattleLocation { get; }
    public BattleExecutionPlan Plan { get; }
    public IReadOnlyList<BattleExecutionSideContext> Sides { get; }
    public BattleExecutionDependencyFingerprint Dependencies { get; }
    public string StableKey => Dependencies.StableKey;
}

public enum BattleExecutionValidationStatus
{
    Current = 0,
    Stale = 1,
    Invalid = 2
}

public enum BattleExecutionStalenessReason
{
    BattleNoLongerActive = 0,
    BattleLocationChanged = 1,
    ParticipantBindingsChanged = 2,
    ParticipantForceMissing = 3,
    ForceTerminated = 4,
    ForcePositionChanged = 5,
    ForceNoLongerSpatiallyCompatible = 6,
    DirectContingentCompositionChanged = 7,
    ExecutionDayChanged = 8,
    ReferencedSpatialStateInvalid = 9,
    BattleStartedDayChanged = 10
}

public sealed class BattleExecutionValidationReport
{
    internal BattleExecutionValidationReport(
        BattleExecutionValidationStatus status,
        IEnumerable<BattleExecutionStalenessReason> reasons,
        string message)
    {
        Status = status;
        List<BattleExecutionStalenessReason> values = reasons == null
            ? new List<BattleExecutionStalenessReason>()
            : new List<BattleExecutionStalenessReason>(reasons);
        values.Sort();
        Reasons = new ReadOnlyCollection<BattleExecutionStalenessReason>(values);
        Message = message ?? string.Empty;
    }

    public BattleExecutionValidationStatus Status { get; }
    public bool IsCurrent => Status == BattleExecutionValidationStatus.Current;
    public bool IsStale => Status == BattleExecutionValidationStatus.Stale;
    public bool IsInvalid => Status == BattleExecutionValidationStatus.Invalid;
    public IReadOnlyList<BattleExecutionStalenessReason> Reasons { get; }
    public string Message { get; }
}

/// <summary>
/// Builds and revalidates an ephemeral Battle execution context from explicit
/// current world stores. It never mutates those stores and never consumes RNG.
/// </summary>
public sealed class BattleExecutionContextBuilder
{
    private readonly PersistentBattleStore battleStore;
    private readonly ArmedForceStore armedForceStore;
    private readonly ArmedForceSpatialStateStore spatialStateStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly LocalTopologyStore localTopologyStore;
    private readonly PersonStore personStore;

    public BattleExecutionContextBuilder(
        PersistentBattleStore battleStore,
        ArmedForceStore armedForceStore,
        ArmedForceSpatialStateStore spatialStateStore,
        SpatialAuthorityStore spatialAuthorityStore,
        LocalTopologyStore localTopologyStore = null,
        PersonStore personStore = null)
    {
        this.battleStore = battleStore ?? throw new ArgumentNullException(nameof(battleStore));
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
        this.spatialStateStore = spatialStateStore ?? throw new ArgumentNullException(nameof(spatialStateStore));
        this.spatialAuthorityStore = spatialAuthorityStore ?? throw new ArgumentNullException(nameof(spatialAuthorityStore));
        this.localTopologyStore = localTopologyStore ?? battleStore.LocalTopologyStore ?? spatialStateStore.LocalTopologyStore;
        this.personStore = personStore ?? armedForceStore.PersonStore;

        if (!ReferenceEquals(battleStore.ArmedForceStore, armedForceStore))
        {
            throw new ArgumentException("BattleStore and ArmedForceStore must belong to the same world composition.", nameof(armedForceStore));
        }

        if (!ReferenceEquals(spatialStateStore.ArmedForceStore, armedForceStore))
        {
            throw new ArgumentException("Spatial state and ArmedForceStore must belong to the same world composition.", nameof(spatialStateStore));
        }

        if (!ReferenceEquals(spatialStateStore.SpatialAuthorityStore, spatialAuthorityStore))
        {
            throw new ArgumentException("Spatial state and SpatialAuthorityStore must belong to the same world composition.", nameof(spatialAuthorityStore));
        }

        if (!ReferenceEquals(battleStore.SpatialAuthorityStore, spatialAuthorityStore))
        {
            throw new ArgumentException("BattleStore and SpatialAuthorityStore must belong to the same world composition.", nameof(spatialAuthorityStore));
        }
    }

    public bool TryCreate(
        BattleId battleId,
        long executionAbsoluteDay,
        out BattleExecutionContext context,
        out BattleExecutionFailure failure)
    {
        return TryCreate(battleId, executionAbsoluteDay, null, out context, out failure);
    }

    public bool TryCreate(
        BattleId battleId,
        long executionAbsoluteDay,
        BattleExecutionPlan plan,
        out BattleExecutionContext context,
        out BattleExecutionFailure failure)
    {
        context = null;
        plan = plan ?? BattleExecutionPlan.Empty;

        if (battleId == null || !battleStore.TryGet(battleId, out PersistentBattleRecord battle))
        {
            return Fail(BattleExecutionFailureCode.BattleNotRegistered, "The BattleId is not registered.", out failure);
        }

        if (battle.LifecycleState != BattleLifecycleState.Active)
        {
            return Fail(BattleExecutionFailureCode.BattleNotActive, "Only an active Battle can produce an execution context.", out failure);
        }

        if (executionAbsoluteDay < 0L
            || !battle.StartedAbsoluteDay.HasValue
            || executionAbsoluteDay < battle.StartedAbsoluteDay.Value)
        {
            return Fail(BattleExecutionFailureCode.InvalidExecutionDay, "Execution day must be non-negative and not before Battle start.", out failure);
        }

        SpatialAuthorityFailure battleLocationFailure = SpatialAuthorityFailure.None;
        if (battle.LocationReference == null
            || spatialAuthorityStore.TryResolve(
                battle.LocationReference,
                localTopologyStore,
                out _,
                out battleLocationFailure) == false)
        {
            return Fail(BattleExecutionFailureCode.BattleLocationInvalid, "The active Battle location is invalid or unresolved: " + battleLocationFailure + ".", out failure);
        }

        if (battle.Sides == null || battle.Sides.Count < 2)
        {
            return Fail(BattleExecutionFailureCode.RequiresTwoSides, "An execution context requires at least two Battle sides.", out failure);
        }

        if (ValidatePlan(battle, plan, out failure) == false)
        {
            return false;
        }

        List<BattleExecutionSideContext> sideContexts = new List<BattleExecutionSideContext>();
        List<BattleExecutionForceFingerprint> forceFingerprints = new List<BattleExecutionForceFingerprint>();
        List<BattleExecutionPositionFingerprint> positionFingerprints = new List<BattleExecutionPositionFingerprint>();
        List<BattleExecutionParticipantFingerprint> participants = new List<BattleExecutionParticipantFingerprint>();
        HashSet<string> seenForces = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> seenContingents = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (BattleStateSide side in battle.Sides)
        {
            if (side == null || side.SideId == null || !sideIds.Add(side.SideId.Value))
            {
                return Fail(BattleExecutionFailureCode.InvalidParticipantBinding, "The Battle contains an invalid or duplicate side.", out failure);
            }

            List<BattleExecutionForceContext> forces = new List<BattleExecutionForceContext>();
            foreach (BattleParticipantBinding binding in battle.ParticipantBindings ?? new List<BattleParticipantBinding>())
            {
                if (binding == null || binding.SideId != side.SideId)
                {
                    continue;
                }

                if (binding.BindingId == null || binding.ArmedForceId == null)
                {
                    return Fail(BattleExecutionFailureCode.InvalidParticipantBinding, "The Battle contains an incomplete participant binding.", out failure);
                }

                if (!seenForces.Add(binding.ArmedForceId.Value))
                {
                    return Fail(BattleExecutionFailureCode.DuplicateParticipantForce, "An ArmedForceId may be bound to a Battle only once.", out failure);
                }

                if (!armedForceStore.TryGet(binding.ArmedForceId, out ArmedForceRecord force))
                {
                    return Fail(BattleExecutionFailureCode.ParticipantForceMissing, "A Battle participant ArmedForceId is not registered.", out failure);
                }

                if (!force.IsActive)
                {
                    return Fail(BattleExecutionFailureCode.ParticipantForceTerminated, "A terminated ArmedForce cannot produce an execution context.", out failure);
                }

                if (!spatialStateStore.TryGetPosition(force.Id, out SpatialReference position)
                    || position == null)
                {
                    return Fail(BattleExecutionFailureCode.ParticipantPositionMissing, "Every explicit Battle participant requires a current typed physical position.", out failure);
                }

                if (spatialAuthorityStore.TryResolve(position, localTopologyStore, out _, out SpatialAuthorityFailure positionFailure) == false)
                {
                    return Fail(BattleExecutionFailureCode.ParticipantPositionInvalid, "A participant position is invalid or unresolved: " + positionFailure + ".", out failure);
                }

                if (!spatialStateStore.TryCheckCompatibility(
                        force.Id,
                        battle.LocationReference,
                        out bool compatible,
                        out ArmedForceSpatialFailure compatibilityFailure))
                {
                    return Fail(BattleExecutionFailureCode.ParticipantPositionInvalid, "A participant spatial compatibility query failed: " + compatibilityFailure + ".", out failure);
                }

                if (!compatible)
                {
                    return Fail(BattleExecutionFailureCode.ParticipantSpatiallyIncompatible, "A participant is not physically compatible with the Battle location.", out failure);
                }

                List<BattleExecutionContingentSnapshot> contingentSnapshots = new List<BattleExecutionContingentSnapshot>();
                List<BattleExecutionContingentFingerprint> contingentFingerprints = new List<BattleExecutionContingentFingerprint>();
                foreach (ContingentRecord contingent in armedForceStore.GetContingents(force.Id))
                {
                    if (contingent == null || contingent.Id == null)
                    {
                        return Fail(BattleExecutionFailureCode.DuplicateContingentProjection, "A direct contingent projection contains an invalid identity.", out failure);
                    }

                    if (!seenContingents.Add(contingent.Id.Value))
                    {
                        return Fail(BattleExecutionFailureCode.DuplicateContingentProjection, "A ContingentId may be projected into an execution context only once.", out failure);
                    }

                    contingentSnapshots.Add(new BattleExecutionContingentSnapshot(contingent));
                    contingentFingerprints.Add(new BattleExecutionContingentFingerprint(contingent));
                }

                forces.Add(new BattleExecutionForceContext(force.Id, position, contingentSnapshots));
                forceFingerprints.Add(new BattleExecutionForceFingerprint(force, contingentFingerprints));
                positionFingerprints.Add(new BattleExecutionPositionFingerprint(force.Id, position));
                participants.Add(new BattleExecutionParticipantFingerprint(binding.BindingId, binding.SideId, binding.ArmedForceId));
            }

            PersonId commander = FindCommander(plan, side.SideId);
            BattleExecutionSideContext sideContext = new BattleExecutionSideContext(side.SideId, commander, forces);
            if (!sideContext.HasUsableDirectCombatElements)
            {
                return Fail(BattleExecutionFailureCode.SideHasNoCombatElements, "Every Battle side requires at least one direct contingent with Amount greater than zero.", out failure);
            }

            sideContexts.Add(sideContext);
        }

        BattleExecutionDependencyFingerprint dependencies = BuildFingerprint(
            battle,
            participants,
            forceFingerprints,
            positionFingerprints,
            plan);
        context = new BattleExecutionContext(
            battle.Id,
            executionAbsoluteDay,
            battle.LocationReference,
            plan,
            sideContexts,
            dependencies);
        failure = BattleExecutionFailure.None;
        return true;
    }

    public bool TryValidateCurrent(
        BattleExecutionContext context,
        long currentAbsoluteDay,
        out BattleExecutionValidationReport report,
        out BattleExecutionFailure failure)
    {
        report = null;
        if (context == null || !ValidateContextShape(context))
        {
            report = new BattleExecutionValidationReport(
                BattleExecutionValidationStatus.Invalid,
                null,
                "The supplied BattleExecutionContext is malformed.");
            failure = BattleExecutionFailure.Create(
                BattleExecutionFailureCode.ContextMalformed,
                report.Message);
            return false;
        }

        List<BattleExecutionStalenessReason> reasons = new List<BattleExecutionStalenessReason>();
        if (currentAbsoluteDay != context.ExecutionAbsoluteDay)
        {
            reasons.Add(BattleExecutionStalenessReason.ExecutionDayChanged);
        }

        if (!battleStore.TryGet(context.BattleId, out PersistentBattleRecord battle))
        {
            reasons.Add(BattleExecutionStalenessReason.BattleNoLongerActive);
        }
        else
        {
            if (battle.LifecycleState != BattleLifecycleState.Active)
            {
                reasons.Add(BattleExecutionStalenessReason.BattleNoLongerActive);
            }

            if (battle.StartedAbsoluteDay != context.Dependencies.StartedAbsoluteDay
                || battle.LocationReference?.StableKey != context.Dependencies.BattleLocationStableKey)
            {
                if (battle.StartedAbsoluteDay != context.Dependencies.StartedAbsoluteDay)
                {
                    reasons.Add(BattleExecutionStalenessReason.BattleStartedDayChanged);
                }

                if (battle.LocationReference?.StableKey != context.Dependencies.BattleLocationStableKey)
                {
                    reasons.Add(BattleExecutionStalenessReason.BattleLocationChanged);
                }
            }

            if (BuildBattleBindingKey(battle) != BuildParticipantBindingKey(context.Dependencies))
            {
                reasons.Add(BattleExecutionStalenessReason.ParticipantBindingsChanged);
            }

            if (BuildBattleSideKey(battle) != BuildParticipantSideKey(context.Dependencies))
            {
                reasons.Add(BattleExecutionStalenessReason.ParticipantBindingsChanged);
            }

            if (battle.LocationReference == null
                || spatialAuthorityStore.TryResolve(battle.LocationReference, localTopologyStore, out _, out _) == false)
            {
                reasons.Add(BattleExecutionStalenessReason.ReferencedSpatialStateInvalid);
            }
        }

        foreach (BattleExecutionForceFingerprint capturedForce in context.Dependencies.Forces)
        {
            if (capturedForce == null || capturedForce.ForceId == null)
            {
                reasons.Add(BattleExecutionStalenessReason.ParticipantForceMissing);
                continue;
            }

            if (!armedForceStore.TryGet(capturedForce.ForceId, out ArmedForceRecord currentForce))
            {
                reasons.Add(BattleExecutionStalenessReason.ParticipantForceMissing);
                continue;
            }

            if (!currentForce.IsActive)
            {
                reasons.Add(BattleExecutionStalenessReason.ForceTerminated);
                continue;
            }

            if (BuildContingentFingerprintKey(armedForceStore.GetContingents(currentForce.Id))
                != BuildContingentFingerprintKey(capturedForce.DirectContingents))
            {
                reasons.Add(BattleExecutionStalenessReason.DirectContingentCompositionChanged);
            }

            SpatialReference currentPosition;
            if (!spatialStateStore.TryGetPosition(currentForce.Id, out currentPosition)
                || currentPosition == null)
            {
                reasons.Add(BattleExecutionStalenessReason.ForcePositionChanged);
                continue;
            }

            BattleExecutionPositionFingerprint capturedPosition = FindPositionFingerprint(
                context.Dependencies.Positions,
                currentForce.Id);
            if (capturedPosition == null
                || !string.Equals(capturedPosition.PositionStableKey, currentPosition.StableKey, StringComparison.Ordinal))
            {
                reasons.Add(BattleExecutionStalenessReason.ForcePositionChanged);
            }

            if (spatialAuthorityStore.TryResolve(currentPosition, localTopologyStore, out _, out _) == false)
            {
                reasons.Add(BattleExecutionStalenessReason.ReferencedSpatialStateInvalid);
                continue;
            }

            bool compatible = false;
            if (battle != null && battle.LocationReference != null
                && spatialStateStore.TryCheckCompatibility(
                    currentForce.Id,
                    battle.LocationReference,
                    out compatible,
                    out _) == false)
            {
                reasons.Add(BattleExecutionStalenessReason.ReferencedSpatialStateInvalid);
            }
            else if (battle != null && battle.LocationReference != null && !compatible)
            {
                reasons.Add(BattleExecutionStalenessReason.ForceNoLongerSpatiallyCompatible);
            }
        }

        SortDistinct(reasons);
        if (reasons.Count == 0)
        {
            report = new BattleExecutionValidationReport(
                BattleExecutionValidationStatus.Current,
                reasons,
                "The BattleExecutionContext remains current.");
            failure = BattleExecutionFailure.None;
            return true;
        }

        report = new BattleExecutionValidationReport(
            BattleExecutionValidationStatus.Stale,
            reasons,
            "The BattleExecutionContext is stale because one or more captured dependencies changed.");
        failure = BattleExecutionFailure.Create(
            BattleExecutionFailureCode.ContextStale,
            report.Message);
        return false;
    }

    private bool ValidatePlan(
        PersistentBattleRecord battle,
        BattleExecutionPlan plan,
        out BattleExecutionFailure failure)
    {
        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleStateSide side in battle.Sides ?? new List<BattleStateSide>())
        {
            if (side?.SideId != null) sideIds.Add(side.SideId.Value);
        }

        HashSet<string> assignedSides = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleExecutionSideCommander assignment in plan.SideCommanders)
        {
            if (assignment == null
                || assignment.SideId == null
                || !sideIds.Contains(assignment.SideId.Value)
                || !assignedSides.Add(assignment.SideId.Value)
                || (assignment.SideCommanderPersonId != null
                    && (personStore == null || !personStore.TryGet(assignment.SideCommanderPersonId, out _))))
            {
                return Fail(BattleExecutionFailureCode.InvalidSideCommander, "A side commander assignment must reference a Battle side and an existing PersonId.", out failure);
            }
        }

        failure = BattleExecutionFailure.None;
        return true;
    }

    private static PersonId FindCommander(BattleExecutionPlan plan, BattleSideId sideId)
    {
        foreach (BattleExecutionSideCommander assignment in plan.SideCommanders)
        {
            if (assignment != null && assignment.SideId == sideId)
            {
                return assignment.SideCommanderPersonId;
            }
        }

        return null;
    }

    private static BattleExecutionDependencyFingerprint BuildFingerprint(
        PersistentBattleRecord battle,
        IEnumerable<BattleExecutionParticipantFingerprint> participants,
        IEnumerable<BattleExecutionForceFingerprint> forces,
        IEnumerable<BattleExecutionPositionFingerprint> positions,
        BattleExecutionPlan plan)
    {
        List<BattleSideId> sideIds = new List<BattleSideId>();
        foreach (BattleStateSide side in battle.Sides ?? new List<BattleStateSide>())
        {
            if (side?.SideId != null) sideIds.Add(side.SideId);
        }

        return new BattleExecutionDependencyFingerprint(
            battle.Id,
            battle.LifecycleState,
            battle.StartedAbsoluteDay,
            battle.LocationReference?.StableKey,
            sideIds,
            participants,
            forces,
            positions,
            BuildPlanFingerprint(plan));
    }

    private static string BuildPlanFingerprint(BattleExecutionPlan plan)
    {
        StringBuilder builder = new StringBuilder();
        foreach (BattleExecutionSideCommander assignment in plan?.SideCommanders
            ?? new List<BattleExecutionSideCommander>())
        {
            Append(builder, assignment?.SideId?.Value);
            Append(builder, assignment?.SideCommanderPersonId?.Value);
        }

        return builder.ToString();
    }

    private static string BuildBattleBindingKey(PersistentBattleRecord battle)
    {
        StringBuilder builder = new StringBuilder();
        foreach (BattleParticipantBinding binding in battle?.ParticipantBindings
            ?? new List<BattleParticipantBinding>())
        {
            Append(builder, binding?.BindingId?.Value);
            Append(builder, binding?.SideId?.Value);
            Append(builder, binding?.ArmedForceId?.Value);
        }

        return builder.ToString();
    }

    private static string BuildParticipantBindingKey(BattleExecutionDependencyFingerprint dependencies)
    {
        StringBuilder builder = new StringBuilder();
        foreach (BattleExecutionParticipantFingerprint binding in dependencies?.Participants
            ?? new List<BattleExecutionParticipantFingerprint>())
        {
            Append(builder, binding?.BindingId?.Value);
            Append(builder, binding?.SideId?.Value);
            Append(builder, binding?.ForceId?.Value);
        }

        return builder.ToString();
    }

    private static string BuildBattleSideKey(PersistentBattleRecord battle)
    {
        StringBuilder builder = new StringBuilder();
        foreach (BattleStateSide side in battle?.Sides
            ?? new List<BattleStateSide>())
        {
            Append(builder, side?.SideId?.Value);
        }

        return builder.ToString();
    }

    private static string BuildParticipantSideKey(BattleExecutionDependencyFingerprint dependencies)
    {
        StringBuilder builder = new StringBuilder();
        foreach (string sideId in dependencies?.SideIds
            ?? new List<string>())
        {
            Append(builder, sideId);
        }

        return builder.ToString();
    }

    private static string BuildContingentFingerprintKey(IEnumerable<ContingentRecord> contingents)
    {
        List<ContingentRecord> values = contingents == null
            ? new List<ContingentRecord>()
            : new List<ContingentRecord>(contingents);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.Id?.Value,
            right?.Id?.Value));
        StringBuilder builder = new StringBuilder();
        foreach (ContingentRecord contingent in values)
        {
            Append(builder, contingent?.Id?.Value);
            Append(builder, contingent?.ForceId?.Value);
            Append(builder, contingent == null ? null : Invariant(contingent.Amount));
            Append(builder, contingent?.Origin?.Domain);
            Append(builder, contingent?.Origin?.Value);
            Append(builder, contingent?.ServiceType);
            foreach (ArmedForceCharacteristic characteristic in contingent?.Characteristics
                ?? new List<ArmedForceCharacteristic>())
            {
                Append(builder, characteristic?.Key);
                Append(builder, characteristic?.Value);
            }
        }

        return builder.ToString();
    }

    private static string BuildContingentFingerprintKey(
        IEnumerable<BattleExecutionContingentFingerprint> contingents)
    {
        StringBuilder builder = new StringBuilder();
        foreach (BattleExecutionContingentFingerprint contingent in contingents
            ?? new List<BattleExecutionContingentFingerprint>())
        {
            Append(builder, contingent?.ContingentId?.Value);
            Append(builder, contingent?.ForceId?.Value);
            Append(builder, contingent == null ? null : Invariant(contingent.Amount));
            Append(builder, contingent?.Origin?.Domain);
            Append(builder, contingent?.Origin?.Value);
            Append(builder, contingent?.ServiceType);
            foreach (ArmedForceCharacteristic characteristic in contingent?.Characteristics
                ?? new List<ArmedForceCharacteristic>())
            {
                Append(builder, characteristic?.Key);
                Append(builder, characteristic?.Value);
            }
        }

        return builder.ToString();
    }

    private static BattleExecutionPositionFingerprint FindPositionFingerprint(
        IEnumerable<BattleExecutionPositionFingerprint> positions,
        ArmedForceId forceId)
    {
        foreach (BattleExecutionPositionFingerprint position in positions
            ?? new List<BattleExecutionPositionFingerprint>())
        {
            if (position?.ForceId == forceId) return position;
        }

        return null;
    }

    private static bool ValidateContextShape(BattleExecutionContext context)
    {
        if (context.BattleId == null
            || context.BattleLocation == null
            || context.Plan == null
            || context.Dependencies == null
            || context.Dependencies.BattleId != context.BattleId
            || context.Dependencies.BattleLocationStableKey != context.BattleLocation.StableKey)
        {
            return false;
        }

        HashSet<string> sides = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleExecutionSideContext side in context.Sides ?? new List<BattleExecutionSideContext>())
        {
            if (side == null || side.SideId == null || !sides.Add(side.SideId.Value)) return false;
            HashSet<string> forces = new HashSet<string>(StringComparer.Ordinal);
            foreach (BattleExecutionForceContext force in side.Forces ?? new List<BattleExecutionForceContext>())
            {
                if (force == null || force.ForceId == null || !forces.Add(force.ForceId.Value)) return false;
                HashSet<string> contingents = new HashSet<string>(StringComparer.Ordinal);
                foreach (BattleExecutionContingentSnapshot contingent in force.DirectContingents)
                {
                    if (contingent == null
                        || contingent.ContingentId == null
                        || contingent.ForceId != force.ForceId
                        || !contingents.Add(contingent.ContingentId.Value)) return false;
                }
            }
        }

        return context.Sides.Count >= 2;
    }

    private static void SortDistinct(List<BattleExecutionStalenessReason> reasons)
    {
        reasons.Sort();
        for (int index = reasons.Count - 1; index > 0; index--)
        {
            if (reasons[index] == reasons[index - 1]) reasons.RemoveAt(index);
        }
    }

    private static void Append(StringBuilder builder, string value)
    {
        if (value == null)
        {
            builder.Append("-;");
            return;
        }

        builder.Append(value.Length).Append(':').Append(value).Append(';');
    }

    private static string Invariant(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool Fail(
        BattleExecutionFailureCode code,
        string message,
        out BattleExecutionFailure failure)
    {
        failure = BattleExecutionFailure.Create(code, message);
        return false;
    }
}
