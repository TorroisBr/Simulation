using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Stable semantic identity for one runtime-created structure.</summary>
public sealed class StructureId : IEquatable<StructureId>, IComparable<StructureId>
{
    public string Value { get; }

    public StructureId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("StructureId requires a non-empty value.", nameof(value));
        Value = value;
    }

    public bool Equals(StructureId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as StructureId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public int CompareTo(StructureId other) => other == null ? 1
        : StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
    public static bool operator ==(StructureId left, StructureId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(StructureId left, StructureId right) => (left == right) == false;
}

/// <summary>The sole immutable proving definition admitted by P15-A.</summary>
public sealed class StructureDefinitionReference : IEquatable<StructureDefinitionReference>
{
    public const string P15AProvingDefinitionId = "P15A-ProvingStructure";
    public const string P15AProvingDefinitionRevision = "v1";
    private static readonly StructureDefinitionReference p15AProving =
        new StructureDefinitionReference(P15AProvingDefinitionId, P15AProvingDefinitionRevision);

    public string DefinitionId { get; }
    public string Revision { get; }
    public static StructureDefinitionReference P15AProving => p15AProving;

    public StructureDefinitionReference(string definitionId, string revision)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            throw new ArgumentException("Structure definition identity is required.", nameof(definitionId));
        if (string.IsNullOrWhiteSpace(revision))
            throw new ArgumentException("Structure definition revision is required.", nameof(revision));
        DefinitionId = definitionId;
        Revision = revision;
    }

    public bool Equals(StructureDefinitionReference other) => other != null
        && string.Equals(DefinitionId, other.DefinitionId, StringComparison.Ordinal)
        && string.Equals(Revision, other.Revision, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as StructureDefinitionReference);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(DefinitionId)
        ^ (StringComparer.Ordinal.GetHashCode(Revision) * 397);
    public override string ToString() => DefinitionId + "/" + Revision;
}

/// <summary>Immutable retained truth for one instantaneous structure creation.</summary>
public sealed class StructureRecord
{
    public StructureId Id { get; }
    public StructureDefinitionReference Definition { get; }
    public LocationId LocationId { get; }
    public long CreatedAtBoundary { get; }
    public long CreationOrder { get; }

    public StructureRecord(
        StructureId id,
        StructureDefinitionReference definition,
        LocationId locationId,
        long createdAtBoundary,
        long creationOrder)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        LocationId = locationId ?? throw new ArgumentNullException(nameof(locationId));
        CreatedAtBoundary = createdAtBoundary;
        CreationOrder = creationOrder;
    }
}

public enum StructureStoreFailureCode
{
    None = 0,
    RuntimeFaulted = 1,
    InitialPublicationIncomplete = 2,
    InvalidBoundary = 3,
    InvalidCreationOrder = 4,
    InvalidDefinition = 5,
    LocationNotRegistered = 6,
    AnchorNotRegistered = 7,
    DuplicateStructureId = 8,
    DuplicateCreationOrder = 9,
    RevisionOverflow = 10,
    InvalidInvariant = 11,
    InvalidSemanticState = 12,
    StructureLimitReached = 13,
    ProfileNotSelected = 14
}

public sealed class StructureStoreFailure : IEquatable<StructureStoreFailure>
{
    private static readonly StructureStoreFailure none =
        new StructureStoreFailure(StructureStoreFailureCode.None, string.Empty);

    private StructureStoreFailure(StructureStoreFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public StructureStoreFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != StructureStoreFailureCode.None;
    public static StructureStoreFailure None => none;
    public static StructureStoreFailure Create(StructureStoreFailureCode code, string message) =>
        code == StructureStoreFailureCode.None ? None : new StructureStoreFailure(code, message);

    public bool Equals(StructureStoreFailure other) => other != null && Code == other.Code
        && string.Equals(Message, other.Message, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as StructureStoreFailure);
    public override int GetHashCode() => ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code + (Message.Length == 0 ? string.Empty : ": " + Message);
}

public sealed class StructureStoreInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;

    internal StructureStoreInvariantReport(IEnumerable<string> violations)
    {
        List<string> copy = violations == null ? new List<string>() : new List<string>(violations);
        copy.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(copy);
    }
}

/// <summary>
/// Exact semantic boundary for future export/hydration. Records are sorted by
/// StructureId; Revision is retained so a clone/staged owner preserves the
/// owner's mutation boundary exactly.
/// </summary>
public sealed class StructureStoreSemanticState
{
    public long Revision { get; }
    public IReadOnlyList<StructureRecord> Records { get; }

    internal StructureStoreSemanticState(long revision, IEnumerable<StructureRecord> records)
    {
        Revision = revision;
        List<StructureRecord> copy = records == null
            ? new List<StructureRecord>()
            : new List<StructureRecord>(records);
        copy.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
        Records = new ReadOnlyCollection<StructureRecord>(copy);
    }
}

/// <summary>
/// Sole P15-A authority for existence, identity, definition, placement and
/// creation boundary of inert structures. It does not own spatial placement.
/// </summary>
public sealed class StructureStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly SpatialAuthorityStore spatialAuthority;
    private readonly Dictionary<string, StructureRecord> recordsById =
        new Dictionary<string, StructureRecord>(StringComparer.Ordinal);
    private long revision;

    public StructureStore(SpatialAuthorityStore spatialAuthority)
    {
        this.spatialAuthority = spatialAuthority ?? throw new ArgumentNullException(nameof(spatialAuthority));
    }

    private StructureStore(SpatialAuthorityStore spatialAuthority, long stagedRevision)
        : this(spatialAuthority)
    {
        revision = stagedRevision;
    }

    public long Revision => revision;
    public int Count => recordsById.Count;
    public IReadOnlyList<StructureRecord> Records => SortedRecords();

    /// <summary>
    /// Creates one explicit record at the current completed runtime boundary.
    /// Equal-boundary order is supplied by the caller's serialized causal order.
    /// </summary>
    public bool TryCreateStructure(
        StructureRecord candidate,
        long currentBoundary,
        bool initialPublicationComplete,
        out StructureStoreFailure failure)
    {
        failure = StructureStoreFailure.None;
        if (!mutationGuardBinding.CanMutate)
            return Fail(StructureStoreFailureCode.RuntimeFaulted, "The SimulationRuntime mutation guard is faulted.", out failure);
        if (!initialPublicationComplete)
            return Fail(StructureStoreFailureCode.InitialPublicationIncomplete, "Initial world publication must complete before runtime structure creation.", out failure);
        if (candidate == null || candidate.Id == null || candidate.LocationId == null)
            return Fail(StructureStoreFailureCode.InvalidInvariant, "A structure requires explicit identity, definition and Location.", out failure);
        if (candidate.CreatedAtBoundary <= 0L || candidate.CreatedAtBoundary != currentBoundary)
            return Fail(StructureStoreFailureCode.InvalidBoundary, "Creation must occur at the current boundary after simulation has begun.", out failure);
        if (candidate.CreationOrder < 0L)
            return Fail(StructureStoreFailureCode.InvalidCreationOrder, "Creation order cannot be negative.", out failure);
        if (!IsSupportedDefinition(candidate.Definition))
            return Fail(StructureStoreFailureCode.InvalidDefinition, "Only P15A-ProvingStructure/v1 is admitted by this bounded store.", out failure);
        if (recordsById.ContainsKey(candidate.Id.Value))
            return Fail(StructureStoreFailureCode.DuplicateStructureId, "StructureId has already been created.", out failure);
        if (recordsById.Count >= 1)
            return Fail(StructureStoreFailureCode.StructureLimitReached, "The P15-A proving owner permits one structure creation.", out failure);
        if (HasCreationOrder(candidate.CreatedAtBoundary, candidate.CreationOrder))
            return Fail(StructureStoreFailureCode.DuplicateCreationOrder, "The boundary creation order is already retained.", out failure);
        if (revision == long.MaxValue)
            return Fail(StructureStoreFailureCode.RevisionOverflow, "Structure owner revision cannot advance further.", out failure);

        long expectedOwnerRevision = revision;
        long expectedSpatialRevision = spatialAuthority.Revision;
        if (!LocationResolves(candidate.LocationId, out StructureStoreFailureCode locationFailure))
            return Fail(locationFailure, "The existing Location and its anchor Hex must resolve through SpatialAuthorityStore.", out failure);

        StructureRecord prepared = CloneRecord(candidate);

        // Commit rechecks both authorities' revisions and the location edge.
        // This owner performs no spatial write, so successful commit is one store mutation.
        if (!mutationGuardBinding.CanMutate)
            return Fail(StructureStoreFailureCode.RuntimeFaulted, "The SimulationRuntime mutation guard faulted before commit.", out failure);
        if (revision != expectedOwnerRevision || spatialAuthority.Revision != expectedSpatialRevision)
            return Fail(StructureStoreFailureCode.InvalidInvariant, "Owner or spatial state changed during structure preflight.", out failure);
        if (!LocationResolves(prepared.LocationId, out locationFailure))
            return Fail(locationFailure, "The Location became invalid before structure commit.", out failure);
        if (recordsById.ContainsKey(prepared.Id.Value) || HasCreationOrder(prepared.CreatedAtBoundary, prepared.CreationOrder))
            return Fail(StructureStoreFailureCode.InvalidInvariant, "Structure identity or causal order changed during preflight.", out failure);

        recordsById.Add(prepared.Id.Value, prepared);
        revision++;
        return true;
    }

    public bool TryGet(StructureId id, out StructureRecord record)
    {
        if (id != null && recordsById.TryGetValue(id.Value, out record))
            return true;
        record = null;
        return false;
    }

    public StructureStoreSemanticState CaptureSemanticState() =>
        new StructureStoreSemanticState(revision, SortedRecords());

    public StructureStore Clone(SpatialAuthorityStore targetSpatialAuthority)
    {
        if (targetSpatialAuthority == null) throw new ArgumentNullException(nameof(targetSpatialAuthority));
        StructureStoreSemanticState state = CaptureSemanticState();
        if (!TryCreateStagedCopy(targetSpatialAuthority, state, long.MaxValue, out StructureStore clone, out StructureStoreFailure failure))
            throw new InvalidOperationException("StructureStore clone failed: " + failure);
        return clone;
    }

    public StructureStoreInvariantReport ValidateInvariants(long currentBoundary)
    {
        List<string> violations = new List<string>();
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> orders = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, StructureRecord> pair in recordsById)
        {
            StructureRecord record = pair.Value;
            if (record == null || record.Id == null || !string.Equals(pair.Key, record.Id.Value, StringComparison.Ordinal))
            {
                violations.Add("structure-index");
                continue;
            }
            if (!ids.Add(record.Id.Value)) violations.Add("duplicate-id:" + record.Id.Value);
            if (!IsSupportedDefinition(record.Definition)) violations.Add("definition:" + record.Id.Value);
            if (record.CreatedAtBoundary <= 0L || record.CreatedAtBoundary > currentBoundary)
                violations.Add("creation-boundary:" + record.Id.Value);
            if (record.CreationOrder < 0L) violations.Add("creation-order:" + record.Id.Value);
            if (!orders.Add(OrderKey(record.CreatedAtBoundary, record.CreationOrder)))
                violations.Add("duplicate-creation-order:" + record.CreatedAtBoundary + ":" + record.CreationOrder);
            if (record.LocationId == null || !LocationResolves(record.LocationId, out _))
                violations.Add("location:" + record.Id.Value);
        }
        if (recordsById.Count > 1)
            violations.Add("structure-limit");
        if (revision < 0L || revision != recordsById.Count)
            violations.Add("owner-revision");
        return new StructureStoreInvariantReport(violations);
    }

    private bool TryCreateStagedCopy(
        SpatialAuthorityStore targetSpatialAuthority,
        StructureStoreSemanticState state,
        long currentBoundary,
        out StructureStore staged,
        out StructureStoreFailure failure)
    {
        staged = null;
        failure = StructureStoreFailure.None;
        if (targetSpatialAuthority == null || state == null || state.Revision < 0L || state.Records == null)
            return Fail(StructureStoreFailureCode.InvalidSemanticState, "Staged structure state is incomplete.", out failure);
        StructureStore candidate = new StructureStore(targetSpatialAuthority, state.Revision);
        foreach (StructureRecord record in state.Records)
        {
            if (record == null || record.Id == null || candidate.recordsById.ContainsKey(record.Id.Value))
                return Fail(StructureStoreFailureCode.InvalidSemanticState, "Staged state contains a null or duplicate StructureId.", out failure);
            if (record.LocationId == null || !IsSupportedDefinition(record.Definition)
                || record.CreatedAtBoundary <= 0L || record.CreationOrder < 0L)
                return Fail(StructureStoreFailureCode.InvalidSemanticState, "Staged state contains an unsupported definition, location or creation boundary.", out failure);
            candidate.recordsById.Add(record.Id.Value, CloneRecord(record));
        }
        if (!candidate.ValidateInvariants(currentBoundary).IsValid)
            return Fail(StructureStoreFailureCode.InvalidSemanticState, "Staged structure relationships or invariants do not validate.", out failure);
        staged = candidate;
        return true;
    }

    private bool LocationResolves(LocationId id, out StructureStoreFailureCode failure)
    {
        failure = StructureStoreFailureCode.LocationNotRegistered;
        if (id == null || !spatialAuthority.TryGet(id, out LocationRecord location)) return false;
        if (spatialAuthority.TryGet(location.AnchorHexId, out HexRecord anchor) && anchor != null) return true;
        failure = StructureStoreFailureCode.AnchorNotRegistered;
        return false;
    }

    private bool HasCreationOrder(long boundary, long order)
    {
        foreach (StructureRecord record in recordsById.Values)
            if (record.CreatedAtBoundary == boundary && record.CreationOrder == order) return true;
        return false;
    }

    private IReadOnlyList<StructureRecord> SortedRecords()
    {
        List<StructureRecord> result = new List<StructureRecord>(recordsById.Values);
        result.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
        return new ReadOnlyCollection<StructureRecord>(result);
    }

    private static bool IsSupportedDefinition(StructureDefinitionReference definition) =>
        StructureDefinitionReference.P15AProving.Equals(definition);

    private static string OrderKey(long boundary, long order) => boundary + ":" + order;

    private static StructureRecord CloneRecord(StructureRecord source) => new StructureRecord(
        new StructureId(source.Id.Value),
        StructureDefinitionReference.P15AProving,
        new LocationId(source.LocationId.Value),
        source.CreatedAtBoundary,
        source.CreationOrder);

    private static bool Fail(StructureStoreFailureCode code, string message, out StructureStoreFailure failure)
    {
        failure = StructureStoreFailure.Create(code, message);
        return false;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
