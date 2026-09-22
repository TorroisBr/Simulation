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

    public HexRecord(HexId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
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
    SubLocation = 2
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
        TopologyOwnerKind = topologyOwnerKind;
        TopologyOwnerRuntimeId = topologyOwnerRuntimeId;
        SubLocationRuntimeId = subLocationRuntimeId;
    }

    public SpatialReferenceKind Kind { get; }
    public HexId HexId { get; }
    public LocationId LocationId { get; }
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
            topologyOwnerKind,
            topologyOwnerRuntimeId,
            subLocationRuntimeId);
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
    public LocalTopologyRuntime LocalTopology { get; }
    public LocalPlaceRuntime SubLocation { get; }

    internal SpatialResolution(
        SpatialReference reference,
        HexRecord hex,
        LocationRecord location,
        LocalTopologyRuntime localTopology = null,
        LocalPlaceRuntime subLocation = null)
    {
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        Hex = hex;
        Location = location;
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
    InvalidInvariant = 15
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
/// Authoritative world-bound Hex/Location identity and the smallest explicit
/// bridge from existing LocalTopology/SubLocation identities to a Location.
/// It does not provide grid generation, adjacency, traversal, terrain, or time.
/// </summary>
public sealed class SpatialAuthorityStore
{
    private readonly Dictionary<string, HexRecord> hexesById =
        new Dictionary<string, HexRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocationRecord> locationsById =
        new Dictionary<string, LocationRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, SpatialLocalTopologyBinding> topologyBindingsByKey =
        new Dictionary<string, SpatialLocalTopologyBinding>(StringComparer.Ordinal);
    private long revision;

    public long Revision => revision;
    public int HexCount => hexesById.Count;
    public int LocationCount => locationsById.Count;
    public int LocalTopologyBindingCount => topologyBindingsByKey.Count;

    public IReadOnlyList<HexRecord> Hexes => SortedHexes();
    public IReadOnlyList<LocationRecord> Locations => SortedLocations();
    public IReadOnlyList<SpatialLocalTopologyBinding> LocalTopologyBindings => SortedBindings();

    public bool TryRegisterHex(HexRecord hex, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (hex == null || hex.Id == null || string.IsNullOrWhiteSpace(hex.Id.Value))
        {
            return Fail(SpatialAuthorityFailureCode.InvalidHex, "Hex requires a stable identity.", out failure);
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

    public bool TryRegisterLocation(LocationRecord location, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
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

        resolution = new SpatialResolution(reference, boundHex, boundLocation, topology, place);
        return true;
    }

    internal SpatialAuthorityStore Clone()
    {
        SpatialAuthorityStore copy = new SpatialAuthorityStore();
        foreach (HexRecord hex in Hexes)
        {
            copy.hexesById.Add(hex.Id.Value, new HexRecord(new HexId(hex.Id.Value)));
        }

        foreach (LocationRecord location in Locations)
        {
            copy.locationsById.Add(
                location.Id.Value,
                new LocationRecord(new LocationId(location.Id.Value), new HexId(location.AnchorHexId.Value)));
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

        copy.revision = revision;
        return copy;
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

        foreach (KeyValuePair<string, LocationRecord> entry in locationsById)
        {
            if (entry.Value == null || entry.Value.Id == null || entry.Value.AnchorHexId == null
                || !string.Equals(entry.Key, entry.Value.Id.Value, StringComparison.Ordinal)
                || !hexesById.ContainsKey(entry.Value.AnchorHexId.Value))
            {
                violations.Add("Location index contains an invalid or unanchored record for '" + entry.Key + "'.");
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

    private IReadOnlyList<SpatialLocalTopologyBinding> SortedBindings()
    {
        List<SpatialLocalTopologyBinding> result = new List<SpatialLocalTopologyBinding>(topologyBindingsByKey.Values);
        result.Sort((left, right) => StringComparer.Ordinal.Compare(left?.StableKey, right?.StableKey));
        return new ReadOnlyCollection<SpatialLocalTopologyBinding>(result);
    }
}
