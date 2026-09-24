using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>A stable identity for a concrete crossing that exists in world truth.</summary>
public sealed class CrossingId : IEquatable<CrossingId>
{
    public string Value { get; }

    public CrossingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("CrossingId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(CrossingId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as CrossingId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(CrossingId left, CrossingId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(CrossingId left, CrossingId right) => (left == right) == false;
}

/// <summary>A stable identity for an explicit persistent traversal connection.</summary>
public sealed class ConnectionId : IEquatable<ConnectionId>
{
    public string Value { get; }

    public ConnectionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ConnectionId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(ConnectionId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as ConnectionId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(ConnectionId left, ConnectionId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(ConnectionId left, ConnectionId right) => (left == right) == false;
}

public sealed class BarrierId : IEquatable<BarrierId>
{
    public string Value { get; }
    public BarrierId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("BarrierId requires a non-empty value.", nameof(value));
        Value = value;
    }
    public bool Equals(BarrierId other) => other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as BarrierId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(BarrierId left, BarrierId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(BarrierId left, BarrierId right) => (left == right) == false;
}

/// <summary>
/// The unordered geometric interface between two distinct Hexes. Construction
/// canonicalizes identity; registration in finite geography establishes whether
/// that pair is actually adjacent.
/// </summary>
public sealed class HexBoundaryKey : IEquatable<HexBoundaryKey>, IComparable<HexBoundaryKey>
{
    public HexId FirstHexId { get; }
    public HexId SecondHexId { get; }

    public HexBoundaryKey(HexId firstHexId, HexId secondHexId)
    {
        if (firstHexId == null) throw new ArgumentNullException(nameof(firstHexId));
        if (secondHexId == null) throw new ArgumentNullException(nameof(secondHexId));
        if (firstHexId == secondHexId)
        {
            throw new ArgumentException("A Hex boundary requires two distinct HexIds.", nameof(secondHexId));
        }

        if (StringComparer.Ordinal.Compare(firstHexId.Value, secondHexId.Value) <= 0)
        {
            FirstHexId = firstHexId;
            SecondHexId = secondHexId;
        }
        else
        {
            FirstHexId = secondHexId;
            SecondHexId = firstHexId;
        }
    }

    public bool Contains(HexId hexId) => hexId != null
        && (FirstHexId == hexId || SecondHexId == hexId);

    public bool Equals(HexBoundaryKey other) => other != null
        && FirstHexId == other.FirstHexId && SecondHexId == other.SecondHexId;
    public int CompareTo(HexBoundaryKey other)
    {
        if (other == null) return 1;
        int firstComparison = StringComparer.Ordinal.Compare(FirstHexId.Value, other.FirstHexId.Value);
        return firstComparison != 0
            ? firstComparison
            : StringComparer.Ordinal.Compare(SecondHexId.Value, other.SecondHexId.Value);
    }
    public override bool Equals(object obj) => Equals(obj as HexBoundaryKey);
    public override int GetHashCode() => (FirstHexId.GetHashCode() * 397) ^ SecondHexId.GetHashCode();
    public override string ToString() => FirstHexId.Value + "|" + SecondHexId.Value;
}

public enum TraversalOptionKind
{
    Connection = 0,
    Crossing = 1,
    WildernessRule = 2
}

/// <summary>
/// Stable typed identity for an option over one boundary. It contains no
/// condition or availability state; those remain mutable factual/contextual data.
/// </summary>
public sealed class TraversalOptionRef : IEquatable<TraversalOptionRef>, IComparable<TraversalOptionRef>
{
    private TraversalOptionRef(
        TraversalOptionKind kind,
        ConnectionId connectionId,
        CrossingId crossingId,
        string ruleIdentity,
        string ruleVersion)
    {
        Kind = kind;
        ConnectionId = connectionId;
        CrossingId = crossingId;
        RuleIdentity = ruleIdentity;
        RuleVersion = ruleVersion;
    }

    public TraversalOptionKind Kind { get; }
    public ConnectionId ConnectionId { get; }
    public CrossingId CrossingId { get; }
    public string RuleIdentity { get; }
    public string RuleVersion { get; }

    public static TraversalOptionRef ForConnection(ConnectionId id) =>
        new TraversalOptionRef(TraversalOptionKind.Connection, id ?? throw new ArgumentNullException(nameof(id)), null, null, null);

    public static TraversalOptionRef ForCrossing(CrossingId id) =>
        new TraversalOptionRef(TraversalOptionKind.Crossing, null, id ?? throw new ArgumentNullException(nameof(id)), null, null);

    public static TraversalOptionRef ForWildernessRule(string identity, string version)
    {
        if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException("A wilderness traversal rule requires a stable identity.", nameof(identity));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("A wilderness traversal rule requires a stable version.", nameof(version));
        return new TraversalOptionRef(TraversalOptionKind.WildernessRule, null, null, identity, version);
    }

    public bool Equals(TraversalOptionRef other) => other != null
        && Kind == other.Kind
        && ConnectionId == other.ConnectionId
        && CrossingId == other.CrossingId
        && string.Equals(RuleIdentity, other.RuleIdentity, StringComparison.Ordinal)
        && string.Equals(RuleVersion, other.RuleVersion, StringComparison.Ordinal);

    public int CompareTo(TraversalOptionRef other)
    {
        if (other == null) return 1;
        int kindComparison = Kind.CompareTo(other.Kind);
        if (kindComparison != 0) return kindComparison;
        switch (Kind)
        {
            case TraversalOptionKind.Connection:
                return StringComparer.Ordinal.Compare(ConnectionId?.Value, other.ConnectionId?.Value);
            case TraversalOptionKind.Crossing:
                return StringComparer.Ordinal.Compare(CrossingId?.Value, other.CrossingId?.Value);
            case TraversalOptionKind.WildernessRule:
                int identityComparison = StringComparer.Ordinal.Compare(RuleIdentity, other.RuleIdentity);
                return identityComparison != 0
                    ? identityComparison
                    : StringComparer.Ordinal.Compare(RuleVersion, other.RuleVersion);
            default:
                return 0;
        }
    }
    public override bool Equals(object obj) => Equals(obj as TraversalOptionRef);
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Kind;
            hash = (hash * 397) ^ (ConnectionId == null ? 0 : ConnectionId.GetHashCode());
            hash = (hash * 397) ^ (CrossingId == null ? 0 : CrossingId.GetHashCode());
            hash = (hash * 397) ^ (RuleIdentity == null ? 0 : StringComparer.Ordinal.GetHashCode(RuleIdentity));
            return (hash * 397) ^ (RuleVersion == null ? 0 : StringComparer.Ordinal.GetHashCode(RuleVersion));
        }
    }
}

/// <summary>A concrete crossing physically associated with an adjacent boundary.</summary>
public sealed class CrossingRecord
{
    public CrossingId Id { get; }
    public HexBoundaryKey Boundary { get; }
    public HexId AnchorHexId { get; }
    public string ContentIdentity { get; }
    public string ContentRevision { get; }
    public decimal EffortMultiplier { get; }
    public IReadOnlyList<BarrierId> OvercomesBarrierIds { get; }

    public CrossingRecord(
        CrossingId id,
        HexBoundaryKey boundary,
        HexId anchorHexId,
        string contentIdentity,
        string contentRevision,
        IEnumerable<BarrierId> overcomesBarrierIds,
        decimal effortMultiplier = 1m)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        AnchorHexId = anchorHexId ?? throw new ArgumentNullException(nameof(anchorHexId));
        if (string.IsNullOrWhiteSpace(contentIdentity)) throw new ArgumentException("Crossing requires a stable content identity.", nameof(contentIdentity));
        if (string.IsNullOrWhiteSpace(contentRevision)) throw new ArgumentException("Crossing requires a stable content revision.", nameof(contentRevision));
        ContentIdentity = contentIdentity;
        ContentRevision = contentRevision;
        if (effortMultiplier <= 0m) throw new ArgumentOutOfRangeException(nameof(effortMultiplier));
        EffortMultiplier = effortMultiplier;
        List<BarrierId> barriers = new List<BarrierId>();
        if (overcomesBarrierIds != null)
        {
            foreach (BarrierId barrierId in overcomesBarrierIds)
            {
                if (barrierId == null) throw new ArgumentException("Crossing barrier references cannot be null.", nameof(overcomesBarrierIds));
                if (barriers.Contains(barrierId) == false) barriers.Add(barrierId);
            }
        }
        barriers.Sort((left, right) => StringComparer.Ordinal.Compare(left.Value, right.Value));
        OvercomesBarrierIds = new ReadOnlyCollection<BarrierId>(barriers);
    }
}

public enum PassageCondition
{
    Available = 0,
    Impaired = 1,
    Closed = 2
}

public enum BarrierCondition
{
    Active = 0,
    Removed = 1
}

/// <summary>A persistent barrier may affect more than one geometric boundary.</summary>
public sealed class BarrierRecord
{
    public BarrierId Id { get; }
    public string ContentIdentity { get; }
    public string ContentRevision { get; }
    public IReadOnlyList<HexBoundaryKey> Boundaries { get; }

    public BarrierRecord(BarrierId id, string contentIdentity, string contentRevision, IEnumerable<HexBoundaryKey> boundaries)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrWhiteSpace(contentIdentity)) throw new ArgumentException("Barrier requires a stable content identity.", nameof(contentIdentity));
        if (string.IsNullOrWhiteSpace(contentRevision)) throw new ArgumentException("Barrier requires a stable content revision.", nameof(contentRevision));
        ContentIdentity = contentIdentity;
        ContentRevision = contentRevision;
        List<HexBoundaryKey> values = new List<HexBoundaryKey>();
        if (boundaries != null)
        {
            foreach (HexBoundaryKey boundary in boundaries)
            {
                if (boundary == null) throw new ArgumentException("Barrier boundary references cannot be null.", nameof(boundaries));
                if (values.Contains(boundary) == false) values.Add(boundary);
            }
        }
        if (values.Count == 0) throw new ArgumentException("A barrier must span at least one registered boundary.", nameof(boundaries));
        values.Sort();
        Boundaries = new ReadOnlyCollection<HexBoundaryKey>(values);
    }
}

/// <summary>Stable option facts; current condition is held separately by the authority.</summary>
public sealed class PassageOptionRecord
{
    public TraversalOptionRef Option { get; }
    public HexBoundaryKey Boundary { get; }
    public string ContentIdentity { get; }
    public string ContentRevision { get; }
    public decimal EffortMultiplier { get; }
    public IReadOnlyList<BarrierId> OvercomesBarrierIds { get; }

    public PassageOptionRecord(
        TraversalOptionRef option,
        HexBoundaryKey boundary,
        string contentIdentity,
        string contentRevision,
        IEnumerable<BarrierId> overcomesBarrierIds = null,
        decimal effortMultiplier = 1m)
    {
        Option = option ?? throw new ArgumentNullException(nameof(option));
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        if (string.IsNullOrWhiteSpace(contentIdentity)) throw new ArgumentException("Passage option requires stable content identity.", nameof(contentIdentity));
        if (string.IsNullOrWhiteSpace(contentRevision)) throw new ArgumentException("Passage option requires a stable content revision.", nameof(contentRevision));
        ContentIdentity = contentIdentity;
        ContentRevision = contentRevision;
        if (effortMultiplier <= 0m) throw new ArgumentOutOfRangeException(nameof(effortMultiplier));
        EffortMultiplier = effortMultiplier;
        List<BarrierId> barriers = new List<BarrierId>();
        if (overcomesBarrierIds != null)
        {
            foreach (BarrierId barrierId in overcomesBarrierIds)
            {
                if (barrierId == null) throw new ArgumentException("Passage option barrier references cannot be null.", nameof(overcomesBarrierIds));
                if (barriers.Contains(barrierId) == false) barriers.Add(barrierId);
            }
        }
        barriers.Sort((left, right) => StringComparer.Ordinal.Compare(left.Value, right.Value));
        OvercomesBarrierIds = new ReadOnlyCollection<BarrierId>(barriers);
    }
}

/// <summary>Explicit, deterministic context for a contextual effort estimate.</summary>
public sealed class TraversalCostContext
{
    public string MovementProfileIdentity { get; }
    public string MovementProfileRevision { get; }
    public decimal EffortPerDistanceUnit { get; }
    public decimal TerrainEffortMultiplier { get; }
    public decimal ImpairedPassageEffortMultiplier { get; }

    public TraversalCostContext(
        string movementProfileIdentity,
        string movementProfileRevision,
        decimal effortPerDistanceUnit,
        decimal terrainEffortMultiplier,
        decimal impairedPassageEffortMultiplier)
    {
        if (string.IsNullOrWhiteSpace(movementProfileIdentity)) throw new ArgumentException("Movement profile requires a stable identity.", nameof(movementProfileIdentity));
        if (string.IsNullOrWhiteSpace(movementProfileRevision)) throw new ArgumentException("Movement profile requires a stable revision.", nameof(movementProfileRevision));
        if (effortPerDistanceUnit <= 0m) throw new ArgumentOutOfRangeException(nameof(effortPerDistanceUnit));
        if (terrainEffortMultiplier <= 0m) throw new ArgumentOutOfRangeException(nameof(terrainEffortMultiplier));
        if (impairedPassageEffortMultiplier <= 0m) throw new ArgumentOutOfRangeException(nameof(impairedPassageEffortMultiplier));
        MovementProfileIdentity = movementProfileIdentity;
        MovementProfileRevision = movementProfileRevision;
        EffortPerDistanceUnit = effortPerDistanceUnit;
        TerrainEffortMultiplier = terrainEffortMultiplier;
        ImpairedPassageEffortMultiplier = impairedPassageEffortMultiplier;
    }
}

public sealed class PassageEvaluation
{
    public HexBoundaryKey Boundary { get; }
    public HexId FromHexId { get; }
    public HexId ToHexId { get; }
    public TraversalOptionRef Option { get; }
    public PassageCondition Condition { get; }
    public bool IsAvailable { get; }
    public IReadOnlyList<BarrierId> BlockingBarrierIds { get; }
    public decimal DistancePerNeighborStep { get; }
    public string DistanceUnit { get; }
    public string FromTerrainDefinitionId { get; }
    public string FromTerrainRevision { get; }
    public string ToTerrainDefinitionId { get; }
    public string ToTerrainRevision { get; }
    public decimal TerrainEffortMultiplier { get; }
    public decimal EstimatedEffort { get; }
    public decimal OptionEffortMultiplier { get; }
    public string MovementProfileIdentity { get; }
    public string MovementProfileRevision { get; }

    internal PassageEvaluation(
        HexBoundaryKey boundary,
        HexId fromHexId,
        HexId toHexId,
        TraversalOptionRef option,
        PassageCondition condition,
        bool isAvailable,
        IEnumerable<BarrierId> blockingBarrierIds,
        SpatialWorldScaleContext scale,
        HexRecord fromHex,
        HexRecord toHex,
        TraversalCostContext cost,
        decimal optionEffortMultiplier)
    {
        Boundary = boundary;
        FromHexId = fromHexId;
        ToHexId = toHexId;
        Option = option;
        Condition = condition;
        IsAvailable = isAvailable;
        List<BarrierId> blockers = new List<BarrierId>(blockingBarrierIds ?? Array.Empty<BarrierId>());
        blockers.Sort((left, right) => StringComparer.Ordinal.Compare(left.Value, right.Value));
        BlockingBarrierIds = new ReadOnlyCollection<BarrierId>(blockers);
        DistancePerNeighborStep = scale.DistancePerNeighborStep;
        DistanceUnit = scale.Unit;
        FromTerrainDefinitionId = fromHex.TerrainDefinitionId.Value;
        FromTerrainRevision = fromHex.AuthoredRevisionToken;
        ToTerrainDefinitionId = toHex.TerrainDefinitionId.Value;
        ToTerrainRevision = toHex.AuthoredRevisionToken;
        TerrainEffortMultiplier = cost.TerrainEffortMultiplier;
        OptionEffortMultiplier = optionEffortMultiplier;
        EstimatedEffort = scale.DistancePerNeighborStep * cost.EffortPerDistanceUnit * cost.TerrainEffortMultiplier
            * optionEffortMultiplier
            * (condition == PassageCondition.Impaired ? cost.ImpairedPassageEffortMultiplier : 1m);
        MovementProfileIdentity = cost.MovementProfileIdentity;
        MovementProfileRevision = cost.MovementProfileRevision;
    }
}

public sealed class PassageOptionState
{
    public HexBoundaryKey Boundary { get; }
    public TraversalOptionRef Option { get; }
    public PassageCondition Condition { get; }
    public bool IsCrossing { get; }
    public string ContentIdentity { get; }
    public string ContentRevision { get; }
    public decimal EffortMultiplier { get; }
    public IReadOnlyList<BarrierId> OvercomesBarrierIds { get; }

    internal PassageOptionState(
        HexBoundaryKey boundary,
        TraversalOptionRef option,
        PassageCondition condition,
        bool isCrossing,
        string contentIdentity,
        string contentRevision,
        IEnumerable<BarrierId> overcomesBarrierIds,
        decimal effortMultiplier)
    {
        Boundary = boundary;
        Option = option;
        Condition = condition;
        IsCrossing = isCrossing;
        ContentIdentity = contentIdentity;
        ContentRevision = contentRevision;
        EffortMultiplier = effortMultiplier;
        OvercomesBarrierIds = new ReadOnlyCollection<BarrierId>(new List<BarrierId>(overcomesBarrierIds ?? Array.Empty<BarrierId>()));
    }
}

public sealed class BarrierState
{
    public BarrierRecord Barrier { get; }
    public BarrierCondition Condition { get; }
    public string ContentIdentity => Barrier.ContentIdentity;
    public string ContentRevision => Barrier.ContentRevision;
    internal BarrierState(BarrierRecord barrier, BarrierCondition condition)
    {
        Barrier = barrier;
        Condition = condition;
    }
}

/// <summary>
/// Factual passage options, barriers, and their mutable conditions. This is a
/// child of SpatialAuthorityStore and uses that authority's guard and revision.
/// </summary>
public sealed class SpatialPassageAuthority
{
    private sealed class OptionKey : IEquatable<OptionKey>
    {
        public HexBoundaryKey Boundary { get; }
        public TraversalOptionRef Option { get; }
        public OptionKey(HexBoundaryKey boundary, TraversalOptionRef option)
        {
            Boundary = boundary;
            Option = option;
        }
        public bool Equals(OptionKey other) => other != null && Boundary.Equals(other.Boundary) && Option.Equals(other.Option);
        public override bool Equals(object obj) => Equals(obj as OptionKey);
        public override int GetHashCode() => (Boundary.GetHashCode() * 397) ^ Option.GetHashCode();
    }

    private readonly SpatialAuthorityStore spatialAuthority;
    private readonly Dictionary<OptionKey, PassageOptionRecord> options = new Dictionary<OptionKey, PassageOptionRecord>();
    private readonly Dictionary<OptionKey, PassageCondition> optionConditions = new Dictionary<OptionKey, PassageCondition>();
    private readonly Dictionary<string, BarrierRecord> barriers = new Dictionary<string, BarrierRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, BarrierCondition> barrierConditions = new Dictionary<string, BarrierCondition>(StringComparer.Ordinal);
    private readonly Dictionary<string, PassageCondition> crossingConditions = new Dictionary<string, PassageCondition>(StringComparer.Ordinal);

    internal SpatialPassageAuthority(SpatialAuthorityStore spatialAuthority)
    {
        this.spatialAuthority = spatialAuthority ?? throw new ArgumentNullException(nameof(spatialAuthority));
    }

    public IReadOnlyList<PassageOptionRecord> Options => SortedOptions();
    public IReadOnlyList<BarrierRecord> Barriers => SortedBarriers();
    public IReadOnlyList<PassageOptionState> OptionStates => SortedOptionStates();
    public IReadOnlyList<BarrierState> BarrierStates => SortedBarrierStates();

    public bool TryGetPassageCondition(
        HexBoundaryKey boundary,
        TraversalOptionRef option,
        out PassageCondition condition,
        out SpatialAuthorityFailure failure)
    {
        condition = PassageCondition.Available;
        if (TryValidateBoundary(boundary, out failure) == false
            || TryValidateOption(boundary, option, out OptionKey key, out failure) == false) return false;
        if (option.Kind == TraversalOptionKind.Crossing)
        {
            condition = crossingConditions.TryGetValue(option.CrossingId.Value, out PassageCondition crossingCondition)
                ? crossingCondition
                : PassageCondition.Available;
        }
        else
        {
            condition = optionConditions[key];
        }
        failure = SpatialAuthorityFailure.None;
        return true;
    }

    public bool TryGetBarrierCondition(BarrierId barrierId, out BarrierCondition condition)
    {
        if (barrierId != null && barrierConditions.TryGetValue(barrierId.Value, out condition)) return true;
        condition = default(BarrierCondition);
        return false;
    }

    public bool TryRegisterConnection(
        PassageOptionRecord option,
        PassageCondition initialCondition,
        out SpatialAuthorityFailure failure)
    {
        if (!spatialAuthority.TryCheckPassageMutationGuard(out failure)) return false;
        if (option == null || option.Option == null || option.Option.Kind != TraversalOptionKind.Connection)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidPassageOption, "Connection registration requires a Connection traversal option.", out failure);
        }
        return TryRegisterOption(option, initialCondition, failure: out failure);
    }

    public bool TryRegisterWildernessRule(
        PassageOptionRecord option,
        PassageCondition initialCondition,
        out SpatialAuthorityFailure failure)
    {
        if (!spatialAuthority.TryCheckPassageMutationGuard(out failure)) return false;
        if (option == null || option.Option == null || option.Option.Kind != TraversalOptionKind.WildernessRule)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidPassageOption, "Wilderness registration requires a versioned wilderness traversal rule.", out failure);
        }
        return TryRegisterOption(option, initialCondition, out failure);
    }

    private bool TryRegisterOption(
        PassageOptionRecord option,
        PassageCondition initialCondition,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!spatialAuthority.TryCheckPassageMutationGuard(out failure)) return false;
        if (!IsValidCondition(initialCondition))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidPassageCondition, "Passage option condition is invalid.", out failure);
        }
        if (option == null || option.Boundary == null || option.Option == null
            || option.Option.Kind == TraversalOptionKind.Crossing
            || Enum.IsDefined(typeof(TraversalOptionKind), option.Option.Kind) == false)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidPassageOption, "Passage option is invalid or is a Crossing, which must be registered as a Crossing fact.", out failure);
        }
        if (option.OvercomesBarrierIds.Count != 0)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidPassageOption, "Only a concrete Crossing may overcome a registered Barrier.", out failure);
        }
        if (TryValidateBoundary(option.Boundary, out failure) == false) return false;
        OptionKey key = new OptionKey(option.Boundary, option.Option);
        if (options.ContainsKey(key))
        {
            return Fail(SpatialAuthorityFailureCode.DuplicatePassageOption, "Traversal option is already registered on this boundary.", out failure);
        }
        if (ValidateBarrierReferences(option.OvercomesBarrierIds, option.Boundary, out failure) == false) return false;

        PassageOptionRecord stableCopy = CloneOption(option);
        return spatialAuthority.TryCommitPassageMutation(() =>
        {
            options.Add(key, stableCopy);
            optionConditions.Add(key, initialCondition);
        }, out failure);
    }

    public bool TryRegisterBarrier(
        BarrierRecord barrier,
        BarrierCondition initialCondition,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!spatialAuthority.TryCheckPassageMutationGuard(out failure)) return false;
        if (barrier == null || barrier.Id == null || barrier.Boundaries == null || barrier.Boundaries.Count == 0
            || !Enum.IsDefined(typeof(BarrierCondition), initialCondition))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidBarrier, "Barrier requires a stable identity, boundaries, and a valid condition.", out failure);
        }
        if (barriers.ContainsKey(barrier.Id.Value))
        {
            return Fail(SpatialAuthorityFailureCode.DuplicateBarrierId, "BarrierId is already registered.", out failure);
        }
        foreach (HexBoundaryKey boundary in barrier.Boundaries)
        {
            if (TryValidateBoundary(boundary, out failure) == false) return false;
        }

        BarrierRecord stableCopy = CloneBarrier(barrier);
        return spatialAuthority.TryCommitPassageMutation(() =>
        {
            barriers.Add(barrier.Id.Value, stableCopy);
            barrierConditions.Add(barrier.Id.Value, initialCondition);
        }, out failure);
    }

    internal bool ValidateCrossingBarrierReferences(
        IReadOnlyList<BarrierId> ids,
        HexBoundaryKey boundary,
        out SpatialAuthorityFailure failure) => ValidateBarrierReferences(ids, boundary, out failure);

    public bool TryGet(BarrierId id, out BarrierRecord barrier)
    {
        if (id != null && barriers.TryGetValue(id.Value, out barrier)) return true;
        barrier = null;
        return false;
    }

    public bool TryGetTraversalOptions(
        HexBoundaryKey boundary,
        out IReadOnlyList<TraversalOptionRef> traversalOptions,
        out SpatialAuthorityFailure failure)
    {
        traversalOptions = new ReadOnlyCollection<TraversalOptionRef>(new List<TraversalOptionRef>());
        if (TryValidateBoundary(boundary, out failure) == false) return false;

        List<TraversalOptionRef> values = new List<TraversalOptionRef>();
        foreach (PassageOptionRecord option in options.Values)
        {
            if (option.Boundary.Equals(boundary)) values.Add(option.Option);
        }
        foreach (CrossingRecord crossing in spatialAuthority.Crossings)
        {
            if (crossing.Boundary.Equals(boundary)) values.Add(TraversalOptionRef.ForCrossing(crossing.Id));
        }
        values.Sort();
        traversalOptions = new ReadOnlyCollection<TraversalOptionRef>(values);
        failure = SpatialAuthorityFailure.None;
        return true;
    }

    public bool TryChangePassageCondition(
        HexBoundaryKey boundary,
        TraversalOptionRef option,
        PassageCondition condition,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!spatialAuthority.TryCheckPassageMutationGuard(out failure)) return false;
        if (!IsValidCondition(condition)) return Fail(SpatialAuthorityFailureCode.InvalidPassageCondition, "Passage condition is invalid.", out failure);
        if (TryValidateBoundary(boundary, out failure) == false) return false;
        if (TryValidateOption(boundary, option, out OptionKey key, out failure) == false) return false;

        if (option.Kind == TraversalOptionKind.Crossing)
        {
            string crossingId = option.CrossingId.Value;
            return spatialAuthority.TryCommitPassageMutation(() => crossingConditions[crossingId] = condition, out failure);
        }
        return spatialAuthority.TryCommitPassageMutation(() => optionConditions[key] = condition, out failure);
    }

    public bool TryChangeBarrierCondition(
        BarrierId barrierId,
        BarrierCondition condition,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!spatialAuthority.TryCheckPassageMutationGuard(out failure)) return false;
        if (barrierId == null || !Enum.IsDefined(typeof(BarrierCondition), condition))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidBarrier, "Barrier condition mutation requires a stable BarrierId and a valid condition.", out failure);
        }
        if (!barriers.ContainsKey(barrierId.Value))
        {
            return Fail(SpatialAuthorityFailureCode.BarrierNotRegistered, "BarrierId is not registered.", out failure);
        }
        return spatialAuthority.TryCommitPassageMutation(() => barrierConditions[barrierId.Value] = condition, out failure);
    }

    public bool TryEvaluatePassage(
        HexId fromHexId,
        HexId toHexId,
        TraversalOptionRef option,
        TraversalCostContext costContext,
        out PassageEvaluation evaluation,
        out SpatialAuthorityFailure failure)
    {
        evaluation = null;
        if (spatialAuthority.TryGetGeometricBoundary(fromHexId, toHexId, out HexBoundaryKey boundary, out failure) == false) return false;
        if (costContext == null) return Fail(SpatialAuthorityFailureCode.InvalidTraversalContext, "Passage evaluation requires explicit movement/cost context.", out failure);
        if (TryValidateOption(boundary, option, out OptionKey key, out failure) == false) return false;

        PassageCondition condition;
        IReadOnlyList<BarrierId> overcomes;
        decimal optionEffortMultiplier;
        if (option.Kind == TraversalOptionKind.Crossing)
        {
            condition = crossingConditions.TryGetValue(option.CrossingId.Value, out PassageCondition crossingCondition)
                ? crossingCondition
                : PassageCondition.Available;
            spatialAuthority.TryGet(option.CrossingId, out CrossingRecord crossing);
            overcomes = crossing.OvercomesBarrierIds;
            optionEffortMultiplier = crossing.EffortMultiplier;
        }
        else
        {
            condition = optionConditions[key];
            overcomes = options[key].OvercomesBarrierIds;
            optionEffortMultiplier = options[key].EffortMultiplier;
        }

        List<BarrierId> blockers = new List<BarrierId>();
        foreach (BarrierRecord barrier in barriers.Values)
        {
            if (!Contains(barrier.Boundaries, boundary)
                || barrierConditions[barrier.Id.Value] != BarrierCondition.Active
                || Contains(overcomes, barrier.Id)) continue;
            blockers.Add(barrier.Id);
        }

        if (spatialAuthority.ScaleContext == null)
        {
            return Fail(SpatialAuthorityFailureCode.GeographyNotPresent, "Contextual passage evaluation requires the P8-A world scale context.", out failure);
        }
        if (!spatialAuthority.TryGet(fromHexId, out HexRecord fromHex)
            || !spatialAuthority.TryGet(toHexId, out HexRecord toHex)
            || fromHex.TerrainReference == null || toHex.TerrainReference == null)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidGeography, "Passage evaluation requires terrain references for both registered endpoint Hexes.", out failure);
        }
        try
        {
            evaluation = new PassageEvaluation(
                boundary,
                fromHexId,
                toHexId,
                option,
                condition,
                condition != PassageCondition.Closed && blockers.Count == 0,
                blockers,
                spatialAuthority.ScaleContext,
                fromHex,
                toHex,
                costContext,
                optionEffortMultiplier);
        }
        catch (OverflowException)
        {
            return Fail(SpatialAuthorityFailureCode.ContextualEffortOverflow, "Contextual effort inputs overflowed the supported decimal range.", out failure);
        }
        failure = SpatialAuthorityFailure.None;
        return true;
    }

    public SpatialAuthorityInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        if (options.Count != optionConditions.Count)
            violations.Add("Passage option records and current conditions do not have matching membership.");
        if (barriers.Count != barrierConditions.Count)
            violations.Add("Barrier records and current conditions do not have matching membership.");
        foreach (KeyValuePair<OptionKey, PassageOptionRecord> entry in options)
        {
            if (entry.Value == null || entry.Value.Option == null || entry.Value.Boundary == null
                || !Enum.IsDefined(typeof(TraversalOptionKind), entry.Value.Option.Kind)
                || string.IsNullOrWhiteSpace(entry.Value.ContentIdentity)
                || string.IsNullOrWhiteSpace(entry.Value.ContentRevision)
                || entry.Value.EffortMultiplier <= 0m
                || !entry.Key.Boundary.Equals(entry.Value.Boundary) || !entry.Key.Option.Equals(entry.Value.Option)
                || !optionConditions.TryGetValue(entry.Key, out PassageCondition condition) || !IsValidCondition(condition))
            {
                violations.Add("Passage option state is invalid: " + entry.Key.Option + ".");
                continue;
            }
            if (TryValidateBoundary(entry.Value.Boundary, out _) == false)
                violations.Add("Passage option boundary is not registered and adjacent: " + entry.Value.Boundary + ".");
            foreach (BarrierId barrierId in entry.Value.OvercomesBarrierIds)
            {
                violations.Add("Non-Crossing passage option claims to overcome BarrierId " + barrierId + " on " + entry.Value.Boundary + ".");
            }
        }
        foreach (KeyValuePair<string, BarrierRecord> entry in barriers)
        {
            if (entry.Value == null || entry.Value.Id == null || !string.Equals(entry.Key, entry.Value.Id.Value, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(entry.Value.ContentIdentity) || string.IsNullOrWhiteSpace(entry.Value.ContentRevision)
                || !barrierConditions.TryGetValue(entry.Key, out BarrierCondition condition)
                || !Enum.IsDefined(typeof(BarrierCondition), condition))
            {
                violations.Add("Barrier state is invalid: " + entry.Key + ".");
                continue;
            }
            foreach (HexBoundaryKey boundary in entry.Value.Boundaries)
            {
                if (TryValidateBoundary(boundary, out _) == false)
                    violations.Add("Barrier boundary is not registered and adjacent: " + entry.Key + ":" + boundary + ".");
            }
        }
        foreach (KeyValuePair<string, PassageCondition> entry in crossingConditions)
        {
            if (!spatialAuthority.TryGet(new CrossingId(entry.Key), out _) || !IsValidCondition(entry.Value))
                violations.Add("Crossing condition refers to an absent CrossingId or invalid state: " + entry.Key + ".");
        }
        foreach (CrossingRecord crossing in spatialAuthority.Crossings)
        {
            if (crossing == null || crossing.Id == null || crossing.Boundary == null
                || string.IsNullOrWhiteSpace(crossing.ContentIdentity) || string.IsNullOrWhiteSpace(crossing.ContentRevision)
                || crossing.EffortMultiplier <= 0m)
            {
                violations.Add("Crossing content, boundary, or effort identity is invalid.");
                continue;
            }
            foreach (BarrierId barrierId in crossing.OvercomesBarrierIds)
            {
                if (barrierId == null || !barriers.ContainsKey(barrierId.Value))
                    violations.Add("Crossing references an absent BarrierId: " + crossing.Id.Value + ":" + barrierId + ".");
            }
        }
        return new SpatialAuthorityInvariantReport(violations);
    }

    internal SpatialPassageAuthority CloneFor(SpatialAuthorityStore clonedSpatialAuthority)
    {
        SpatialPassageAuthority copy = new SpatialPassageAuthority(clonedSpatialAuthority);
        foreach (PassageOptionRecord option in Options)
        {
            PassageOptionRecord clonedOption = CloneOption(option);
            OptionKey key = new OptionKey(clonedOption.Boundary, clonedOption.Option);
            copy.options.Add(key, clonedOption);
            copy.optionConditions.Add(key, optionConditions[new OptionKey(option.Boundary, option.Option)]);
        }
        foreach (BarrierRecord barrier in Barriers)
        {
            copy.barriers.Add(barrier.Id.Value, CloneBarrier(barrier));
            copy.barrierConditions.Add(barrier.Id.Value, barrierConditions[barrier.Id.Value]);
        }
        foreach (KeyValuePair<string, PassageCondition> entry in crossingConditions)
            copy.crossingConditions.Add(entry.Key, entry.Value);
        return copy;
    }

    private bool TryValidateOption(HexBoundaryKey boundary, TraversalOptionRef option, out OptionKey key, out SpatialAuthorityFailure failure)
    {
        key = null;
        failure = SpatialAuthorityFailure.None;
        if (option == null || !Enum.IsDefined(typeof(TraversalOptionKind), option.Kind))
            return Fail(SpatialAuthorityFailureCode.InvalidPassageOption, "Traversal option identity is invalid.", out failure);
        key = new OptionKey(boundary, option);
        if (option.Kind == TraversalOptionKind.Crossing)
        {
            if (option.CrossingId == null || spatialAuthority.TryGet(option.CrossingId, out CrossingRecord crossing) == false)
                return Fail(SpatialAuthorityFailureCode.PassageOptionNotRegistered, "Crossing option is not registered in this spatial authority.", out failure);
            if (!crossing.Boundary.Equals(boundary))
                return Fail(SpatialAuthorityFailureCode.PassageOptionBoundaryMismatch, "Crossing option belongs to a different boundary.", out failure);
            return true;
        }
        if (!options.ContainsKey(key))
            return Fail(SpatialAuthorityFailureCode.PassageOptionNotRegistered, "Traversal option is not registered on this boundary.", out failure);
        return true;
    }

    private bool ValidateBarrierReferences(
        IReadOnlyList<BarrierId> ids,
        HexBoundaryKey boundary,
        out SpatialAuthorityFailure failure)
    {
        foreach (BarrierId id in ids)
        {
            if (id == null || !barriers.TryGetValue(id.Value, out BarrierRecord barrier))
                return Fail(SpatialAuthorityFailureCode.BarrierNotRegistered, "Passage option may only reference registered barriers.", out failure);
            if (!Contains(barrier.Boundaries, boundary))
                return Fail(SpatialAuthorityFailureCode.InvalidPassageOption, "Passage option may only overcome barriers that intersect its boundary.", out failure);
        }
        failure = SpatialAuthorityFailure.None;
        return true;
    }

    private bool TryValidateBoundary(HexBoundaryKey boundary, out SpatialAuthorityFailure failure)
    {
        if (boundary == null) return Fail(SpatialAuthorityFailureCode.BoundaryNotAdjacent, "Boundary is required.", out failure);
        if (spatialAuthority.TryGetGeometricBoundary(
            boundary.FirstHexId, boundary.SecondHexId, out HexBoundaryKey registered, out failure) == false) return false;
        if (!registered.Equals(boundary)) return Fail(SpatialAuthorityFailureCode.BoundaryNotAdjacent, "Boundary key does not match registered endpoints.", out failure);
        return true;
    }

    private IReadOnlyList<PassageOptionRecord> SortedOptions()
    {
        List<PassageOptionRecord> values = new List<PassageOptionRecord>(options.Values);
        values.Sort((left, right) =>
        {
            int boundaryOrder = left.Boundary.CompareTo(right.Boundary);
            return boundaryOrder != 0 ? boundaryOrder : left.Option.CompareTo(right.Option);
        });
        return new ReadOnlyCollection<PassageOptionRecord>(values);
    }

    private IReadOnlyList<BarrierRecord> SortedBarriers()
    {
        List<BarrierRecord> values = new List<BarrierRecord>(barriers.Values);
        values.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value));
        return new ReadOnlyCollection<BarrierRecord>(values);
    }

    private IReadOnlyList<PassageOptionState> SortedOptionStates()
    {
        List<PassageOptionState> values = new List<PassageOptionState>();
        foreach (PassageOptionRecord option in options.Values)
        {
            OptionKey key = new OptionKey(option.Boundary, option.Option);
            values.Add(new PassageOptionState(option.Boundary, option.Option, optionConditions[key], false,
                option.ContentIdentity, option.ContentRevision, option.OvercomesBarrierIds, option.EffortMultiplier));
        }
        foreach (CrossingRecord crossing in spatialAuthority.Crossings)
        {
            PassageCondition condition = crossingConditions.TryGetValue(crossing.Id.Value, out PassageCondition stored)
                ? stored
                : PassageCondition.Available;
            values.Add(new PassageOptionState(crossing.Boundary, TraversalOptionRef.ForCrossing(crossing.Id), condition,
                true, crossing.ContentIdentity, crossing.ContentRevision, crossing.OvercomesBarrierIds, crossing.EffortMultiplier));
        }
        values.Sort((left, right) =>
        {
            int boundaryOrder = left.Boundary.CompareTo(right.Boundary);
            return boundaryOrder != 0 ? boundaryOrder : left.Option.CompareTo(right.Option);
        });
        return new ReadOnlyCollection<PassageOptionState>(values);
    }

    private IReadOnlyList<BarrierState> SortedBarrierStates()
    {
        List<BarrierState> values = new List<BarrierState>();
        foreach (BarrierRecord barrier in SortedBarriers())
            values.Add(new BarrierState(barrier, barrierConditions[barrier.Id.Value]));
        return new ReadOnlyCollection<BarrierState>(values);
    }

    private static PassageOptionRecord CloneOption(PassageOptionRecord source) => new PassageOptionRecord(
        source.Option, new HexBoundaryKey(source.Boundary.FirstHexId, source.Boundary.SecondHexId),
        source.ContentIdentity, source.ContentRevision, source.OvercomesBarrierIds, source.EffortMultiplier);

    private static BarrierRecord CloneBarrier(BarrierRecord source) => new BarrierRecord(
        new BarrierId(source.Id.Value), source.ContentIdentity, source.ContentRevision, source.Boundaries);

    private static bool IsValidCondition(PassageCondition condition) => Enum.IsDefined(typeof(PassageCondition), condition);

    private static bool Contains<T>(IReadOnlyList<T> values, T sought)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(values[index], sought)) return true;
        }
        return false;
    }

    private static bool Fail(SpatialAuthorityFailureCode code, string message, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.Create(code, message);
        return false;
    }
}
