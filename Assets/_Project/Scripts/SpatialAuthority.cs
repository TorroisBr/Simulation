using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class HexId : IEquatable<HexId>
{
    public string Value { get; }

    public HexId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("HexId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(HexId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as HexId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(HexId left, HexId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(HexId left, HexId right) => (left == right) == false;
}

public sealed class LocationId : IEquatable<LocationId>
{
    public string Value { get; }

    public LocationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("LocationId requires a non-empty value.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(LocationId other) => other != null
        && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as LocationId);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(LocationId left, LocationId right) => ReferenceEquals(left, right)
        || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    public static bool operator !=(LocationId left, LocationId right) => (left == right) == false;
}

public sealed class HexRecord
{
    public HexId Id { get; }
    public HexCoordinate? Coordinate { get; }
    public TerrainReference TerrainReference { get; }
    public TerrainDefinitionId TerrainDefinitionId => TerrainReference?.TerrainDefinitionId;
    public string AuthoredRevisionToken => TerrainReference?.AuthoredRevisionToken;
    public bool IsGeographic => Coordinate.HasValue && TerrainReference != null;

    public HexRecord(HexId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public HexRecord(HexId id, HexCoordinate coordinate, TerrainReference terrainReference)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TerrainReference = terrainReference ?? throw new ArgumentNullException(nameof(terrainReference));
        Coordinate = coordinate;
    }
}

public sealed class LocationRecord
{
    public LocationId Id { get; }
    public HexId AnchorHexId { get; }

    public LocationRecord(LocationId id, HexId anchorHexId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        AnchorHexId = anchorHexId ?? throw new ArgumentNullException(nameof(anchorHexId));
    }
}

public enum SpatialReferenceKind
{
    Hex = 0,
    Location = 1,
    SubLocation = 2,
    Crossing = 3
}

/// <summary>
/// A typed physical reference. SubLocation uses the existing local-topology
/// owner and place identities; it is not an arbitrary opaque string.
/// </summary>
public sealed class SpatialReference : IEquatable<SpatialReference>
{
    private SpatialReference(
        SpatialReferenceKind kind,
        HexId hexId,
        LocationId locationId,
        CrossingId crossingId,
        LocalTopologyOwnerKind? topologyOwnerKind,
        string topologyOwnerRuntimeId,
        string subLocationRuntimeId)
    {
        if (Enum.IsDefined(typeof(SpatialReferenceKind), kind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
        HexId = hexId;
        LocationId = locationId;
        CrossingId = crossingId;
        TopologyOwnerKind = topologyOwnerKind;
        TopologyOwnerRuntimeId = topologyOwnerRuntimeId;
        SubLocationRuntimeId = subLocationRuntimeId;
    }

    public SpatialReferenceKind Kind { get; }
    public HexId HexId { get; }
    public LocationId LocationId { get; }
    public CrossingId CrossingId { get; }
    public LocalTopologyOwnerKind? TopologyOwnerKind { get; }
    public string TopologyOwnerRuntimeId { get; }
    public string SubLocationRuntimeId { get; }

    public string StableKey
    {
        get
        {
            switch (Kind)
            {
                case SpatialReferenceKind.Hex:
                    return "hex:" + HexId.Value;
                case SpatialReferenceKind.Location:
                    return "location:" + LocationId.Value;
                case SpatialReferenceKind.Crossing:
                    return "crossing:" + CrossingId.Value;
                case SpatialReferenceKind.SubLocation:
                    return "sublocation:" + TopologyOwnerKind.Value + ":"
                        + TopologyOwnerRuntimeId + ":" + SubLocationRuntimeId;
                default:
                    return string.Empty;
            }
        }
    }

    public static SpatialReference ForHex(HexId hexId)
    {
        return new SpatialReference(
            SpatialReferenceKind.Hex,
            hexId ?? throw new ArgumentNullException(nameof(hexId)),
            null,
            null,
            null,
            null,
            null);
    }

    public static SpatialReference ForLocation(LocationId locationId)
    {
        return new SpatialReference(
            SpatialReferenceKind.Location,
            null,
            locationId ?? throw new ArgumentNullException(nameof(locationId)),
            null,
            null,
            null,
            null);
    }

    public static SpatialReference ForSubLocation(
        LocalTopologyOwnerKind topologyOwnerKind,
        string topologyOwnerRuntimeId,
        string subLocationRuntimeId)
    {
        if (Enum.IsDefined(typeof(LocalTopologyOwnerKind), topologyOwnerKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(topologyOwnerKind));
        }

        if (string.IsNullOrWhiteSpace(topologyOwnerRuntimeId))
        {
            throw new ArgumentException("SubLocation references require a topology owner RuntimeId.", nameof(topologyOwnerRuntimeId));
        }

        if (string.IsNullOrWhiteSpace(subLocationRuntimeId))
        {
            throw new ArgumentException("SubLocation references require a local place RuntimeId.", nameof(subLocationRuntimeId));
        }

        return new SpatialReference(
            SpatialReferenceKind.SubLocation,
            null,
            null,
            null,
            topologyOwnerKind,
            topologyOwnerRuntimeId,
            subLocationRuntimeId);
    }

    public static SpatialReference ForCrossing(CrossingId crossingId)
    {
        return new SpatialReference(
            SpatialReferenceKind.Crossing,
            null,
            null,
            crossingId ?? throw new ArgumentNullException(nameof(crossingId)),
            null,
            null,
            null);
    }

    public static SpatialReference ForSubLocation(
        LocalTopologyOwnerReference topologyOwner,
        string subLocationRuntimeId)
    {
        if (topologyOwner == null)
        {
            throw new ArgumentNullException(nameof(topologyOwner));
        }

        return ForSubLocation(
            topologyOwner.OwnerKind,
            topologyOwner.OwnerRuntimeId,
            subLocationRuntimeId);
    }

    public bool Equals(SpatialReference other)
    {
        return other != null
            && Kind == other.Kind
            && HexId == other.HexId
            && LocationId == other.LocationId
            && CrossingId == other.CrossingId
            && TopologyOwnerKind == other.TopologyOwnerKind
            && string.Equals(TopologyOwnerRuntimeId, other.TopologyOwnerRuntimeId, StringComparison.Ordinal)
            && string.Equals(SubLocationRuntimeId, other.SubLocationRuntimeId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => Equals(obj as SpatialReference);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableKey);
    public override string ToString() => StableKey;
}

public sealed class SpatialLocalTopologyBinding
{
    public LocalTopologyOwnerKind OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public LocationId LocationId { get; }

    public string StableKey => OwnerKind + ":" + OwnerRuntimeId;

    public SpatialLocalTopologyBinding(
        LocalTopologyOwnerKind ownerKind,
        string ownerRuntimeId,
        LocationId locationId)
    {
        if (Enum.IsDefined(typeof(LocalTopologyOwnerKind), ownerKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerKind));
        }

        if (string.IsNullOrWhiteSpace(ownerRuntimeId))
        {
            throw new ArgumentException("Local topology bindings require an owner RuntimeId.", nameof(ownerRuntimeId));
        }

        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        LocationId = locationId ?? throw new ArgumentNullException(nameof(locationId));
    }
}

public sealed class SpatialResolution
{
    public SpatialReference Reference { get; }
    public HexRecord Hex { get; }
    public LocationRecord Location { get; }
    public CrossingRecord Crossing { get; }
    public LocalTopologyRuntime LocalTopology { get; }
    public LocalPlaceRuntime SubLocation { get; }

    internal SpatialResolution(
        SpatialReference reference,
        HexRecord hex,
        LocationRecord location,
        CrossingRecord crossing = null,
        LocalTopologyRuntime localTopology = null,
        LocalPlaceRuntime subLocation = null)
    {
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        Hex = hex;
        Location = location;
        Crossing = crossing;
        LocalTopology = localTopology;
        SubLocation = subLocation;
    }
}

public enum SpatialAuthorityFailureCode
{
    None = 0,
    InvalidHex = 1,
    DuplicateHexId = 2,
    InvalidLocation = 3,
    DuplicateLocationId = 4,
    AnchorHexNotRegistered = 5,
    InvalidSpatialReference = 6,
    HexNotRegistered = 7,
    LocationNotRegistered = 8,
    InvalidTopologyBinding = 9,
    DuplicateTopologyBinding = 10,
    TopologyLocationNotRegistered = 11,
    TopologyNotRegistered = 12,
    SubLocationNotRegistered = 13,
    RevisionOverflow = 14,
    InvalidInvariant = 15,
    RuntimeFaulted = 16,
    GeographyNotPresent = 17,
    InvalidGeography = 18,
    DuplicateHexCoordinate = 19,
    SpatialAuthorityNotEmpty = 20,
    GeographicHexRequiresComposition = 21,
    InvalidCrossing = 22,
    DuplicateCrossingId = 23,
    CrossingBoundaryNotAdjacent = 24,
    CrossingAnchorNotInBoundary = 25,
    CrossingNotRegistered = 26,
    BoundaryNotAdjacent = 27,
    InvalidPassageOption = 28,
    InvalidPassageCondition = 29,
    DuplicatePassageOption = 30,
    PassageOptionNotRegistered = 31,
    PassageOptionBoundaryMismatch = 32,
    InvalidBarrier = 33,
    DuplicateBarrierId = 34,
    BarrierNotRegistered = 35,
    InvalidTraversalContext = 36,
    ContextualEffortOverflow = 37
}

public sealed class SpatialAuthorityFailure : IEquatable<SpatialAuthorityFailure>
{
    private static readonly SpatialAuthorityFailure none =
        new SpatialAuthorityFailure(SpatialAuthorityFailureCode.None, string.Empty);

    private SpatialAuthorityFailure(SpatialAuthorityFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    public static SpatialAuthorityFailure None => none;
    public SpatialAuthorityFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != SpatialAuthorityFailureCode.None;

    public static SpatialAuthorityFailure Create(SpatialAuthorityFailureCode code, string message)
    {
        return code == SpatialAuthorityFailureCode.None
            ? None
            : new SpatialAuthorityFailure(code, message);
    }

    public bool Equals(SpatialAuthorityFailure other) => other != null
        && Code == other.Code
        && string.Equals(Message, other.Message, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as SpatialAuthorityFailure);
    public override int GetHashCode() => ((int)Code * 397)
        ^ StringComparer.Ordinal.GetHashCode(Message);
    public override string ToString() => Code
        + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

public sealed class SpatialAuthorityInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;

    internal SpatialAuthorityInvariantReport(IEnumerable<string> violations)
    {
        List<string> values = violations == null
            ? new List<string>()
            : new List<string>(violations);
        values.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(values);
    }
}

/// <summary>
/// Authoritative world-bound Hex/Location identity, finite factual geography,
/// and the smallest explicit bridge from existing LocalTopology/SubLocation
/// identities to a Location. It does not provide grid generation, traversal,
/// or travel time.
/// </summary>
public sealed class SpatialAuthorityStore : IAuthoritativeMutationGuardBindable
{
    private static readonly HexCoordinate[] NeighborDeltas =
    {
        new HexCoordinate(1, 0),
        new HexCoordinate(1, -1),
        new HexCoordinate(0, -1),
        new HexCoordinate(-1, 0),
        new HexCoordinate(-1, 1),
        new HexCoordinate(0, 1)
    };

    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly Dictionary<string, HexRecord> hexesById =
        new Dictionary<string, HexRecord>(StringComparer.Ordinal);
    private readonly Dictionary<HexCoordinate, HexRecord> geographicHexesByCoordinate =
        new Dictionary<HexCoordinate, HexRecord>();
    private readonly Dictionary<string, LocationRecord> locationsById =
        new Dictionary<string, LocationRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, CrossingRecord> crossingsById =
        new Dictionary<string, CrossingRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, SpatialLocalTopologyBinding> topologyBindingsByKey =
        new Dictionary<string, SpatialLocalTopologyBinding>(StringComparer.Ordinal);
    private SpatialWorldScaleContext scaleContext;
    private string coordinateConventionVersion;
    private string coordinateCanonicalOrder;
    private long revision;

    public SpatialAuthorityStore()
    {
        PassageAuthority = new SpatialPassageAuthority(this);
    }

    public long Revision => revision;
    public int HexCount => hexesById.Count;
    public int LocationCount => locationsById.Count;
    public int CrossingCount => crossingsById.Count;
    public SpatialPassageAuthority PassageAuthority { get; private set; }
    public int LocalTopologyBindingCount => topologyBindingsByKey.Count;
    public bool HasGeography => scaleContext != null;
    public SpatialWorldScaleContext ScaleContext => scaleContext;
    public string CoordinateConventionVersion => coordinateConventionVersion;
    public string CoordinateCanonicalOrder => coordinateCanonicalOrder;

    public IReadOnlyList<HexRecord> Hexes => SortedHexes();
    public IReadOnlyList<LocationRecord> Locations => SortedLocations();
    public IReadOnlyList<CrossingRecord> Crossings => SortedCrossings();
    public IReadOnlyList<SpatialLocalTopologyBinding> LocalTopologyBindings => SortedBindings();

    /// <summary>
    /// Atomically composes the finite, manually authored P8-A geography into
    /// an otherwise empty authority. Legacy identity-only stores remain
    /// scale-free and are not implicitly promoted to a geographic grid.
    /// </summary>
    public bool TryComposeGeography(
        SpatialGeographyDefinition definition,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!mutationGuardBinding.CanMutate)
        {
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        }

        if (definition == null || definition.ScaleContext == null || definition.Hexes == null
            || definition.Hexes.Count == 0
            || string.IsNullOrWhiteSpace(definition.CoordinateConventionVersion)
            || string.IsNullOrWhiteSpace(definition.CoordinateCanonicalOrder))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidGeography, "Geography requires a scale context, a coordinate convention, and at least one Hex.", out failure);
        }

        if (hexesById.Count != 0 || locationsById.Count != 0 || topologyBindingsByKey.Count != 0
            || scaleContext != null || geographicHexesByCoordinate.Count != 0)
        {
            return Fail(SpatialAuthorityFailureCode.SpatialAuthorityNotEmpty, "Finite geography must be composed into an empty spatial authority.", out failure);
        }

        Dictionary<string, HexRecord> pendingHexes = new Dictionary<string, HexRecord>(StringComparer.Ordinal);
        Dictionary<HexCoordinate, HexRecord> pendingCoordinates = new Dictionary<HexCoordinate, HexRecord>();
        foreach (HexRecord hex in definition.Hexes)
        {
            if (hex == null || hex.Id == null || string.IsNullOrWhiteSpace(hex.Id.Value)
                || !hex.IsGeographic || !hex.Coordinate.HasValue
                || hex.TerrainReference == null
                || hex.TerrainDefinitionId == null
                || string.IsNullOrWhiteSpace(hex.TerrainDefinitionId.Value)
                || string.IsNullOrWhiteSpace(hex.AuthoredRevisionToken))
            {
                return Fail(SpatialAuthorityFailureCode.InvalidGeography, "Every geographic Hex requires a stable ID, axial coordinate, terrain definition ID, and authored revision token.", out failure);
            }

            if (pendingHexes.ContainsKey(hex.Id.Value))
            {
                return Fail(SpatialAuthorityFailureCode.DuplicateHexId, "HexId is duplicated in the authored geography.", out failure);
            }

            HexCoordinate coordinate = hex.Coordinate.Value;
            if (pendingCoordinates.ContainsKey(coordinate))
            {
                return Fail(SpatialAuthorityFailureCode.DuplicateHexCoordinate, "Axial coordinate is duplicated in the authored geography.", out failure);
            }

            pendingHexes.Add(hex.Id.Value, hex);
            pendingCoordinates.Add(coordinate, hex);
        }

        Dictionary<string, LocationRecord> pendingLocations = new Dictionary<string, LocationRecord>(StringComparer.Ordinal);
        foreach (LocationRecord location in definition.Locations ?? Array.Empty<LocationRecord>())
        {
            if (location == null || location.Id == null || location.AnchorHexId == null
                || string.IsNullOrWhiteSpace(location.Id.Value)
                || string.IsNullOrWhiteSpace(location.AnchorHexId.Value))
            {
                return Fail(SpatialAuthorityFailureCode.InvalidLocation, "Authored Locations require a stable identity and exactly one anchor Hex.", out failure);
            }

            if (pendingLocations.ContainsKey(location.Id.Value))
            {
                return Fail(SpatialAuthorityFailureCode.DuplicateLocationId, "LocationId is duplicated in the authored geography.", out failure);
            }

            if (pendingHexes.ContainsKey(location.AnchorHexId.Value) == false)
            {
                return Fail(SpatialAuthorityFailureCode.AnchorHexNotRegistered, "Authored Location anchor Hex is absent from the finite geography.", out failure);
            }

            pendingLocations.Add(location.Id.Value, location);
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        foreach (KeyValuePair<string, HexRecord> entry in pendingHexes)
        {
            HexRecord copy = CloneHex(entry.Value);
            hexesById.Add(entry.Key, copy);
            geographicHexesByCoordinate.Add(copy.Coordinate.Value, copy);
        }

        foreach (KeyValuePair<string, LocationRecord> entry in pendingLocations)
        {
            locationsById.Add(entry.Key, CloneLocation(entry.Value));
        }

        scaleContext = CloneScaleContext(definition.ScaleContext);
        coordinateConventionVersion = definition.CoordinateConventionVersion;
        coordinateCanonicalOrder = definition.CoordinateCanonicalOrder;
        revision++;
        return true;
    }

    public bool TryRegisterHex(HexRecord hex, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!mutationGuardBinding.CanMutate)
        {
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        }

        if (hex == null || hex.Id == null || string.IsNullOrWhiteSpace(hex.Id.Value))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidHex, "Hex requires a stable identity.", out failure);
        }

        if (scaleContext != null || hex.IsGeographic)
        {
            return Fail(SpatialAuthorityFailureCode.GeographicHexRequiresComposition, "Geographic Hexes must be registered together through finite geography composition.", out failure);
        }

        if (hexesById.ContainsKey(hex.Id.Value))
        {
            return Fail(SpatialAuthorityFailureCode.DuplicateHexId, "HexId is already registered.", out failure);
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        hexesById.Add(hex.Id.Value, hex);
        revision++;
        return true;
    }

    /// <summary>
    /// Returns only the registered geographic Hexes at the six axial
    /// neighboring coordinates, in lexicographic (q, r) order.
    /// </summary>
    public bool TryGetGeometricNeighbors(
        HexId id,
        out IReadOnlyList<HexRecord> neighbors,
        out SpatialAuthorityFailure failure)
    {
        neighbors = new ReadOnlyCollection<HexRecord>(new List<HexRecord>());
        failure = SpatialAuthorityFailure.None;
        if (scaleContext == null)
        {
            return Fail(SpatialAuthorityFailureCode.GeographyNotPresent, "This spatial authority contains no finite geography.", out failure);
        }

        if (id == null || hexesById.TryGetValue(id.Value, out HexRecord hex) == false)
        {
            return Fail(SpatialAuthorityFailureCode.HexNotRegistered, "Hex is not registered in the finite geography.", out failure);
        }

        if (hex.IsGeographic == false || hex.Coordinate.HasValue == false)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidGeography, "Registered geography contains a Hex without axial coordinates.", out failure);
        }

        List<HexRecord> found = new List<HexRecord>(NeighborDeltas.Length);
        foreach (HexCoordinate delta in NeighborDeltas)
        {
            if (hex.Coordinate.Value.TryOffset(delta, out HexCoordinate candidate)
                && geographicHexesByCoordinate.TryGetValue(candidate, out HexRecord neighbor))
            {
                found.Add(neighbor);
            }
        }

        found.Sort((left, right) => left.Coordinate.Value.CompareTo(right.Coordinate.Value));
        neighbors = new ReadOnlyCollection<HexRecord>(found);
        return true;
    }

    /// <summary>
    /// Resolves the unordered identity for an adjacent pair in the registered
    /// finite geography. This reports geometric adjacency only, never passage.
    /// </summary>
    public bool TryGetGeometricBoundary(
        HexId fromHexId,
        HexId toHexId,
        out HexBoundaryKey boundary,
        out SpatialAuthorityFailure failure)
    {
        boundary = null;
        failure = SpatialAuthorityFailure.None;
        if (fromHexId == null || toHexId == null || fromHexId == toHexId)
        {
            return Fail(SpatialAuthorityFailureCode.BoundaryNotAdjacent, "A geometric boundary requires two distinct registered HexIds.", out failure);
        }

        if (TryGetGeometricNeighbors(fromHexId, out IReadOnlyList<HexRecord> neighbors, out failure) == false)
        {
            return false;
        }

        bool isNeighbor = false;
        foreach (HexRecord neighbor in neighbors)
        {
            if (neighbor.Id == toHexId)
            {
                isNeighbor = true;
                break;
            }
        }

        if (!isNeighbor)
        {
            return Fail(SpatialAuthorityFailureCode.BoundaryNotAdjacent, "The registered Hexes are not geometric neighbors.", out failure);
        }

        boundary = new HexBoundaryKey(fromHexId, toHexId);
        return true;
    }

    /// <summary>Registers the stable Crossing identity and its factual boundary anchor.</summary>
    public bool TryRegisterCrossing(CrossingRecord crossing, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!mutationGuardBinding.CanMutate)
        {
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        }

        if (crossing == null || crossing.Id == null || crossing.Boundary == null || crossing.AnchorHexId == null)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidCrossing, "Crossing requires a stable identity, boundary, and anchor Hex.", out failure);
        }

        if (crossingsById.ContainsKey(crossing.Id.Value))
        {
            return Fail(SpatialAuthorityFailureCode.DuplicateCrossingId, "CrossingId is already registered.", out failure);
        }

        if (!crossing.Boundary.Contains(crossing.AnchorHexId))
        {
            return Fail(SpatialAuthorityFailureCode.CrossingAnchorNotInBoundary, "Crossing anchor Hex must be one of its boundary endpoints.", out failure);
        }

        if (TryGetGeometricBoundary(crossing.Boundary.FirstHexId, crossing.Boundary.SecondHexId,
            out HexBoundaryKey registeredBoundary, out failure) == false)
        {
            if (failure.Code == SpatialAuthorityFailureCode.BoundaryNotAdjacent)
            {
                failure = SpatialAuthorityFailure.Create(SpatialAuthorityFailureCode.CrossingBoundaryNotAdjacent,
                    "Crossing boundary is not adjacent in registered finite geography.");
            }
            return false;
        }

        if (!PassageAuthority.ValidateCrossingBarrierReferences(crossing.OvercomesBarrierIds, crossing.Boundary, out failure)) return false;

        if (CanAdvanceRevision(out failure) == false) return false;
        crossingsById.Add(crossing.Id.Value, new CrossingRecord(
            new CrossingId(crossing.Id.Value),
            registeredBoundary,
            new HexId(crossing.AnchorHexId.Value),
            crossing.ContentIdentity,
            crossing.ContentRevision,
            crossing.OvercomesBarrierIds,
            crossing.EffortMultiplier));
        revision++;
        return true;
    }

    public bool TryGet(CrossingId id, out CrossingRecord crossing)
    {
        if (id != null && crossingsById.TryGetValue(id.Value, out crossing)) return true;
        crossing = null;
        return false;
    }

    public bool TryRegisterLocation(LocationRecord location, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!mutationGuardBinding.CanMutate)
        {
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        }

        if (location == null || location.Id == null || location.AnchorHexId == null
            || string.IsNullOrWhiteSpace(location.Id.Value)
            || string.IsNullOrWhiteSpace(location.AnchorHexId.Value))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidLocation, "Location requires a stable identity and exactly one anchor Hex.", out failure);
        }

        if (locationsById.ContainsKey(location.Id.Value))
        {
            return Fail(SpatialAuthorityFailureCode.DuplicateLocationId, "LocationId is already registered.", out failure);
        }

        if (hexesById.ContainsKey(location.AnchorHexId.Value) == false)
        {
            return Fail(SpatialAuthorityFailureCode.AnchorHexNotRegistered, "Location anchor Hex is not registered.", out failure);
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        locationsById.Add(location.Id.Value, location);
        revision++;
        return true;
    }

    public bool TryBindLocalTopology(
        LocalTopologyRuntime topology,
        LocationId locationId,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!mutationGuardBinding.CanMutate)
        {
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        }

        if (topology == null || topology.Owner == null || locationId == null)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidTopologyBinding, "A topology binding requires a topology, owner, and LocationId.", out failure);
        }

        if (locationsById.TryGetValue(locationId.Value, out LocationRecord location) == false
            || hexesById.ContainsKey(location.AnchorHexId.Value) == false)
        {
            return Fail(SpatialAuthorityFailureCode.TopologyLocationNotRegistered, "The topology binding Location is not registered with a valid anchor.", out failure);
        }

        if (topology.TryValidate(out string topologyDiagnostic) == false)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidTopologyBinding, topologyDiagnostic, out failure);
        }

        string key = TopologyKey(topology.Owner.OwnerKind, topology.Owner.OwnerRuntimeId);
        if (topologyBindingsByKey.ContainsKey(key))
        {
            return Fail(SpatialAuthorityFailureCode.DuplicateTopologyBinding, "The local topology owner is already spatially bound.", out failure);
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        topologyBindingsByKey.Add(
            key,
            new SpatialLocalTopologyBinding(topology.Owner.OwnerKind, topology.Owner.OwnerRuntimeId, locationId));
        revision++;
        return true;
    }

    public bool TryGet(HexId id, out HexRecord hex)
    {
        if (id != null && hexesById.TryGetValue(id.Value, out hex)) return true;
        hex = null;
        return false;
    }

    public bool TryGet(LocationId id, out LocationRecord location)
    {
        if (id != null && locationsById.TryGetValue(id.Value, out location)) return true;
        location = null;
        return false;
    }

    public bool TryGetTopologyBinding(
        LocalTopologyOwnerKind ownerKind,
        string ownerRuntimeId,
        out SpatialLocalTopologyBinding binding)
    {
        if (Enum.IsDefined(typeof(LocalTopologyOwnerKind), ownerKind)
            && string.IsNullOrWhiteSpace(ownerRuntimeId) == false
            && topologyBindingsByKey.TryGetValue(TopologyKey(ownerKind, ownerRuntimeId), out binding))
        {
            return true;
        }

        binding = null;
        return false;
    }

    public bool TryResolve(
        SpatialReference reference,
        LocalTopologyStore localTopologyStore,
        out SpatialResolution resolution,
        out SpatialAuthorityFailure failure)
    {
        resolution = null;
        failure = SpatialAuthorityFailure.None;
        if (reference == null || Enum.IsDefined(typeof(SpatialReferenceKind), reference.Kind) == false)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidSpatialReference, "SpatialReference is invalid.", out failure);
        }

        if (reference.Kind == SpatialReferenceKind.Hex)
        {
            if (TryGet(reference.HexId, out HexRecord hex) == false)
            {
                return Fail(SpatialAuthorityFailureCode.HexNotRegistered, "SpatialReference Hex is not registered.", out failure);
            }

            resolution = new SpatialResolution(reference, hex, null);
            return true;
        }

        if (reference.Kind == SpatialReferenceKind.Location)
        {
            if (TryGet(reference.LocationId, out LocationRecord location) == false)
            {
                return Fail(SpatialAuthorityFailureCode.LocationNotRegistered, "SpatialReference Location is not registered.", out failure);
            }

            TryGet(location.AnchorHexId, out HexRecord anchorHex);
            resolution = new SpatialResolution(reference, anchorHex, location);
            return true;
        }

        if (reference.Kind == SpatialReferenceKind.Crossing)
        {
            if (reference.CrossingId == null || TryGet(reference.CrossingId, out CrossingRecord crossing) == false)
            {
                return Fail(SpatialAuthorityFailureCode.CrossingNotRegistered, "SpatialReference Crossing is not registered.", out failure);
            }

            TryGet(crossing.AnchorHexId, out HexRecord crossingAnchor);
            resolution = new SpatialResolution(reference, crossingAnchor, null, crossing);
            return true;
        }

        if (reference.TopologyOwnerKind.HasValue == false
            || string.IsNullOrWhiteSpace(reference.TopologyOwnerRuntimeId)
            || string.IsNullOrWhiteSpace(reference.SubLocationRuntimeId))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidSpatialReference, "SubLocation SpatialReference is incomplete.", out failure);
        }

        if (TryGetTopologyBinding(
                reference.TopologyOwnerKind.Value,
                reference.TopologyOwnerRuntimeId,
                out SpatialLocalTopologyBinding binding) == false)
        {
            return Fail(SpatialAuthorityFailureCode.TopologyNotRegistered, "SubLocation topology owner has no spatial binding.", out failure);
        }

        if (localTopologyStore == null
            || localTopologyStore.TryGetTopologyForOwner(reference.TopologyOwnerRuntimeId, out LocalTopologyRuntime topology) == false
            || topology.Owner.OwnerKind != reference.TopologyOwnerKind.Value)
        {
            return Fail(SpatialAuthorityFailureCode.TopologyNotRegistered, "SubLocation topology is absent from the supplied LocalTopologyStore.", out failure);
        }

        if (topology.TryValidate(out string topologyDiagnostic) == false
            || topology.TryGetPlace(reference.SubLocationRuntimeId, out LocalPlaceRuntime place) == false)
        {
            return Fail(SpatialAuthorityFailureCode.SubLocationNotRegistered, topologyDiagnostic ?? "SubLocation is absent from its owning topology.", out failure);
        }

        if (TryGet(binding.LocationId, out LocationRecord boundLocation) == false
            || TryGet(boundLocation.AnchorHexId, out HexRecord boundHex) == false)
        {
            return Fail(SpatialAuthorityFailureCode.LocationNotRegistered, "SubLocation binding resolves to an absent Location or Hex.", out failure);
        }

        resolution = new SpatialResolution(reference, boundHex, boundLocation, localTopology: topology, subLocation: place);
        return true;
    }

    internal SpatialAuthorityStore Clone()
    {
        SpatialAuthorityStore copy = new SpatialAuthorityStore();
        foreach (HexRecord hex in Hexes)
        {
            HexRecord clone = CloneHex(hex);
            copy.hexesById.Add(hex.Id.Value, clone);
            if (clone.IsGeographic)
            {
                copy.geographicHexesByCoordinate.Add(clone.Coordinate.Value, clone);
            }
        }

        foreach (LocationRecord location in Locations)
        {
            copy.locationsById.Add(
                location.Id.Value,
                new LocationRecord(new LocationId(location.Id.Value), new HexId(location.AnchorHexId.Value)));
        }

        foreach (CrossingRecord crossing in Crossings)
        {
            copy.crossingsById.Add(crossing.Id.Value, new CrossingRecord(
                new CrossingId(crossing.Id.Value),
                new HexBoundaryKey(new HexId(crossing.Boundary.FirstHexId.Value), new HexId(crossing.Boundary.SecondHexId.Value)),
                new HexId(crossing.AnchorHexId.Value),
                crossing.ContentIdentity,
                crossing.ContentRevision,
                crossing.OvercomesBarrierIds,
                crossing.EffortMultiplier));
        }

        foreach (SpatialLocalTopologyBinding binding in LocalTopologyBindings)
        {
            copy.topologyBindingsByKey.Add(
                binding.StableKey,
                new SpatialLocalTopologyBinding(
                    binding.OwnerKind,
                    binding.OwnerRuntimeId,
                    new LocationId(binding.LocationId.Value)));
        }

        copy.scaleContext = scaleContext == null ? null : CloneScaleContext(scaleContext);
        copy.coordinateConventionVersion = coordinateConventionVersion;
        copy.coordinateCanonicalOrder = coordinateCanonicalOrder;
        copy.revision = revision;
        copy.PassageAuthority = PassageAuthority.CloneFor(copy);
        return copy;
    }

    internal bool TryCommitPassageMutation(Action applyMutation, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (!mutationGuardBinding.CanMutate)
        {
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        }
        if (applyMutation == null)
        {
            return Fail(SpatialAuthorityFailureCode.InvalidPassageOption, "Passage mutation has no validated operation.", out failure);
        }
        if (CanAdvanceRevision(out failure) == false) return false;
        applyMutation();
        revision++;
        return true;
    }

    internal bool TryCheckPassageMutationGuard(out SpatialAuthorityFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        failure = SpatialAuthorityFailure.None;
        return true;
    }

    public SpatialAuthorityInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        if (revision < 0L)
        {
            violations.Add("Revision cannot be negative.");
        }

        foreach (KeyValuePair<string, HexRecord> entry in hexesById)
        {
            if (entry.Value == null || entry.Value.Id == null
                || !string.Equals(entry.Key, entry.Value.Id.Value, StringComparison.Ordinal))
            {
                violations.Add("Hex index contains an invalid record for '" + entry.Key + "'.");
            }
        }

        if (scaleContext == null)
        {
            if (geographicHexesByCoordinate.Count != 0
                || string.IsNullOrEmpty(coordinateConventionVersion) == false
                || string.IsNullOrEmpty(coordinateCanonicalOrder) == false)
            {
                violations.Add("Geographic index or coordinate convention exists without a world-local scale context.");
            }

            foreach (KeyValuePair<string, HexRecord> entry in hexesById)
            {
                if (entry.Value != null && entry.Value.IsGeographic)
                {
                    violations.Add("Geographic Hex exists without a world-local scale context: " + entry.Key + ".");
                }
            }
        }
        else
        {
            if (hexesById.Count == 0)
            {
                violations.Add("Geography scale context exists without any registered geographic Hexes.");
            }

            if (string.IsNullOrWhiteSpace(scaleContext.ResolvedConventionId)
                || string.IsNullOrWhiteSpace(scaleContext.SourceIdentity)
                || string.IsNullOrWhiteSpace(scaleContext.SourceVersion)
                || scaleContext.DistancePerNeighborStep <= 0m
                || string.IsNullOrWhiteSpace(scaleContext.Unit))
            {
                violations.Add("World-local scale context is incomplete or invalid.");
            }

            if (!string.Equals(coordinateConventionVersion, HexCoordinate.ConventionVersion, StringComparison.Ordinal)
                || !string.Equals(coordinateCanonicalOrder, HexCoordinate.CanonicalOrder, StringComparison.Ordinal))
            {
                violations.Add("Geography coordinate convention is absent or unsupported.");
            }

            HashSet<HexCoordinate> coordinates = new HashSet<HexCoordinate>();
            foreach (KeyValuePair<string, HexRecord> entry in hexesById)
            {
                HexRecord hex = entry.Value;
                if (hex == null || !hex.IsGeographic || !hex.Coordinate.HasValue
                    || hex.TerrainDefinitionId == null
                    || string.IsNullOrWhiteSpace(hex.TerrainDefinitionId.Value)
                    || string.IsNullOrWhiteSpace(hex.AuthoredRevisionToken))
                {
                    violations.Add("Geographic Hex is missing its coordinate or terrain reference ID/revision token: " + entry.Key + ".");
                    continue;
                }

                HexCoordinate coordinate = hex.Coordinate.Value;
                if (!coordinates.Add(coordinate))
                {
                    violations.Add("Geographic Hex coordinate is duplicated: " + coordinate + ".");
                }

                if (!geographicHexesByCoordinate.TryGetValue(coordinate, out HexRecord indexedHex)
                    || !string.Equals(indexedHex?.Id?.Value, hex.Id.Value, StringComparison.Ordinal))
                {
                    violations.Add("Geographic coordinate index does not match Hex " + entry.Key + ".");
                }
            }

            if (geographicHexesByCoordinate.Count != hexesById.Count)
            {
                violations.Add("Geographic coordinate index does not match the finite Hex set.");
            }
        }

        foreach (KeyValuePair<string, LocationRecord> entry in locationsById)
        {
            if (entry.Value == null || entry.Value.Id == null || entry.Value.AnchorHexId == null
                || !string.Equals(entry.Key, entry.Value.Id.Value, StringComparison.Ordinal)
                || !hexesById.ContainsKey(entry.Value.AnchorHexId.Value))
            {
                violations.Add("Location index contains an invalid or unanchored record for '" + entry.Key + "'.");
            }
        }

        foreach (KeyValuePair<string, CrossingRecord> entry in crossingsById)
        {
            CrossingRecord crossing = entry.Value;
            if (crossing == null || crossing.Id == null || crossing.Boundary == null || crossing.AnchorHexId == null
                || !string.Equals(entry.Key, crossing.Id.Value, StringComparison.Ordinal)
                || !crossing.Boundary.Contains(crossing.AnchorHexId)
                || !hexesById.ContainsKey(crossing.Boundary.FirstHexId.Value)
                || !hexesById.ContainsKey(crossing.Boundary.SecondHexId.Value)
                || !hexesById.ContainsKey(crossing.AnchorHexId.Value))
            {
                violations.Add("Crossing index contains an invalid or unanchored record for '" + entry.Key + "'.");
                continue;
            }

            if (TryGetGeometricBoundary(crossing.Boundary.FirstHexId, crossing.Boundary.SecondHexId,
                out _, out _) == false)
            {
                violations.Add("Crossing boundary is not adjacent in registered finite geography: " + entry.Key + ".");
            }
        }

        foreach (KeyValuePair<string, SpatialLocalTopologyBinding> entry in topologyBindingsByKey)
        {
            SpatialLocalTopologyBinding binding = entry.Value;
            if (binding == null || binding.LocationId == null
                || !string.Equals(entry.Key, binding.StableKey, StringComparison.Ordinal)
                || !locationsById.ContainsKey(binding.LocationId.Value))
            {
                violations.Add("Local topology spatial binding is invalid for '" + entry.Key + "'.");
            }
        }

        return new SpatialAuthorityInvariantReport(violations);
    }

    public SpatialAuthorityInvariantReport ValidateInvariants(LocalTopologyStore localTopologyStore)
    {
        SpatialAuthorityInvariantReport baseReport = ValidateInvariants();
        List<string> violations = new List<string>(baseReport.Violations);
        if (topologyBindingsByKey.Count == 0)
        {
            return new SpatialAuthorityInvariantReport(violations);
        }

        if (localTopologyStore == null)
        {
            violations.Add("Local topology bindings require a LocalTopologyStore for owner validation.");
            return new SpatialAuthorityInvariantReport(violations);
        }

        foreach (SpatialLocalTopologyBinding binding in topologyBindingsByKey.Values)
        {
            if (binding == null)
            {
                continue;
            }

            if (localTopologyStore.TryGetTopologyForOwner(binding.OwnerRuntimeId, out LocalTopologyRuntime topology) == false)
            {
                violations.Add("Spatial topology binding owner is absent from LocalTopologyStore: " + binding.StableKey + ".");
                continue;
            }

            if (topology.Owner == null || topology.Owner.OwnerKind != binding.OwnerKind)
            {
                violations.Add("Spatial topology binding owner kind does not match LocalTopologyStore: " + binding.StableKey + ".");
                continue;
            }

            if (topology.TryValidate(out string diagnostic) == false)
            {
                violations.Add("Spatial topology binding owner is invalid: " + binding.StableKey + " (" + diagnostic + ").");
            }
        }

        return new SpatialAuthorityInvariantReport(violations);
    }

    private bool CanAdvanceRevision(out SpatialAuthorityFailure failure)
    {
        if (revision == long.MaxValue)
        {
            return Fail(SpatialAuthorityFailureCode.RevisionOverflow, "Spatial authority revision cannot advance further.", out failure);
        }

        failure = SpatialAuthorityFailure.None;
        return true;
    }

    private static bool Fail(
        SpatialAuthorityFailureCode code,
        string message,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.Create(code, message);
        return false;
    }

    private static string TopologyKey(LocalTopologyOwnerKind ownerKind, string ownerRuntimeId)
    {
        return ownerKind + ":" + ownerRuntimeId;
    }

    private static HexRecord CloneHex(HexRecord source)
    {
        return source.IsGeographic
            ? new HexRecord(
                new HexId(source.Id.Value),
                source.Coordinate.Value,
                new TerrainReference(
                    new TerrainDefinitionId(source.TerrainDefinitionId.Value),
                    source.AuthoredRevisionToken))
            : new HexRecord(new HexId(source.Id.Value));
    }

    private static LocationRecord CloneLocation(LocationRecord source)
    {
        return new LocationRecord(new LocationId(source.Id.Value), new HexId(source.AnchorHexId.Value));
    }

    private static SpatialWorldScaleContext CloneScaleContext(SpatialWorldScaleContext source)
    {
        return new SpatialWorldScaleContext(
            source.ResolvedConventionId,
            source.SourceIdentity,
            source.SourceVersion,
            source.DistancePerNeighborStep,
            source.Unit);
    }

    private IReadOnlyList<HexRecord> SortedHexes()
    {
        List<HexRecord> result = new List<HexRecord>(hexesById.Values);
        result.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
        return new ReadOnlyCollection<HexRecord>(result);
    }

    private IReadOnlyList<LocationRecord> SortedLocations()
    {
        List<LocationRecord> result = new List<LocationRecord>(locationsById.Values);
        result.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
        return new ReadOnlyCollection<LocationRecord>(result);
    }

    private IReadOnlyList<CrossingRecord> SortedCrossings()
    {
        List<CrossingRecord> result = new List<CrossingRecord>(crossingsById.Values);
        result.Sort((left, right) => StringComparer.Ordinal.Compare(left?.Id?.Value, right?.Id?.Value));
        return new ReadOnlyCollection<CrossingRecord>(result);
    }

    private IReadOnlyList<SpatialLocalTopologyBinding> SortedBindings()
    {
        List<SpatialLocalTopologyBinding> result = new List<SpatialLocalTopologyBinding>(topologyBindingsByKey.Values);
        result.Sort((left, right) => StringComparer.Ordinal.Compare(left?.StableKey, right?.StableKey));
        return new ReadOnlyCollection<SpatialLocalTopologyBinding>(result);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
