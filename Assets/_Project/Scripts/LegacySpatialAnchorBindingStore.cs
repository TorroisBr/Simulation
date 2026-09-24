using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum SpatialAnchorOwnerKind { City = 0, ExplorableSite = 1 }

/// <summary>Stable semantic identity of a legacy domain object receiving a spatial anchor.</summary>
public sealed class SpatialAnchorOwnerId : IEquatable<SpatialAnchorOwnerId>, IComparable<SpatialAnchorOwnerId>
{
    public SpatialAnchorOwnerKind Kind { get; }
    public string Value { get; }
    public SpatialAnchorOwnerId(SpatialAnchorOwnerKind kind, string value)
    {
        if (!Enum.IsDefined(typeof(SpatialAnchorOwnerKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A spatial anchor owner requires a stable domain identity.", nameof(value));
        Kind = kind; Value = value;
    }
    public string StableKey => ((int)Kind).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + Value;
    public bool Equals(SpatialAnchorOwnerId other) => other != null && Kind == other.Kind && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as SpatialAnchorOwnerId);
    public override int GetHashCode() => ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Value);
    public int CompareTo(SpatialAnchorOwnerId other) => other == null ? 1 : StringComparer.Ordinal.Compare(StableKey, other.StableKey);
}

public sealed class SpatialAnchorBinding
{
    public SpatialAnchorOwnerId OwnerId { get; }
    public LocationId LocationId { get; }
    public string StableKey => OwnerId.StableKey;
    internal SpatialAnchorBinding(SpatialAnchorOwnerId owner, LocationId location) { OwnerId = owner; LocationId = location; }
}

public enum SpatialAnchorBindingFailureCode { None = 0, RuntimeFaulted = 1, InvalidBinding = 2, OwnerAlreadyBound = 3, LocationAlreadyBound = 4, LocationNotRegistered = 5, RevisionOverflow = 6 }

public sealed class SpatialAnchorBindingFailure
{
    private SpatialAnchorBindingFailure(SpatialAnchorBindingFailureCode code, string message) { Code = code; Message = message ?? string.Empty; }
    public static SpatialAnchorBindingFailure None { get; } = new SpatialAnchorBindingFailure(SpatialAnchorBindingFailureCode.None, string.Empty);
    public SpatialAnchorBindingFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != SpatialAnchorBindingFailureCode.None;
    internal static SpatialAnchorBindingFailure Create(SpatialAnchorBindingFailureCode code, string message) => new SpatialAnchorBindingFailure(code, message);
    public override string ToString() => Code + (Message.Length == 0 ? string.Empty : ": " + Message);
}

/// <summary>
/// Explicit one-to-one bridge from stable City/Site identity to the neutral P8-A Location anchor.
/// It creates no access, containment, or traversal relationship.
/// </summary>
public sealed class LegacySpatialAnchorBindingStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly Dictionary<string, SpatialAnchorBinding> byOwner = new Dictionary<string, SpatialAnchorBinding>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> ownerByLocation = new Dictionary<string, string>(StringComparer.Ordinal);
    private long revision;

    public LegacySpatialAnchorBindingStore(SpatialAuthorityStore spatialAuthorityStore)
    { this.spatialAuthorityStore = spatialAuthorityStore ?? throw new ArgumentNullException(nameof(spatialAuthorityStore)); }
    public long Revision => revision;
    public int Count => byOwner.Count;
    public IReadOnlyList<SpatialAnchorBinding> Bindings
    {
        get { List<SpatialAnchorBinding> result = new List<SpatialAnchorBinding>(byOwner.Values); result.Sort((a, b) => a.OwnerId.CompareTo(b.OwnerId)); return new ReadOnlyCollection<SpatialAnchorBinding>(result); }
    }
    public bool TryGet(SpatialAnchorOwnerId owner, out LocationId locationId)
    { locationId = null; if (owner == null || !byOwner.TryGetValue(owner.StableKey, out SpatialAnchorBinding binding)) return false; locationId = binding.LocationId; return true; }

    public bool TryBindCity(string stableCityId, LocationId locationId, out SpatialAnchorBindingFailure failure) =>
        TryBind(new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, stableCityId), locationId, out failure);
    public bool TryBindSite(string stableSiteId, LocationId locationId, out SpatialAnchorBindingFailure failure) =>
        TryBind(new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.ExplorableSite, stableSiteId), locationId, out failure);

    public bool TryBind(SpatialAnchorOwnerId owner, LocationId locationId, out SpatialAnchorBindingFailure failure)
    {
        if (!mutationGuardBinding.CanMutate) return Fail(SpatialAnchorBindingFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (owner == null || locationId == null) return Fail(SpatialAnchorBindingFailureCode.InvalidBinding, "Stable domain owner and LocationId are required.", out failure);
        if (!spatialAuthorityStore.TryGet(locationId, out _)) return Fail(SpatialAnchorBindingFailureCode.LocationNotRegistered, "LocationId must resolve in the supplied SpatialAuthorityStore.", out failure);
        if (byOwner.TryGetValue(owner.StableKey, out SpatialAnchorBinding current))
            return current.LocationId == locationId ? Succeed(out failure) : Fail(SpatialAnchorBindingFailureCode.OwnerAlreadyBound, "A City/Site owner can have exactly one stable Location anchor.", out failure);
        if (ownerByLocation.ContainsKey(locationId.Value)) return Fail(SpatialAnchorBindingFailureCode.LocationAlreadyBound, "Each Location anchor can map to only one City/Site owner.", out failure);
        if (revision == long.MaxValue) return Fail(SpatialAnchorBindingFailureCode.RevisionOverflow, "Spatial anchor binding revision cannot advance.", out failure);
        SpatialAnchorBinding binding = new SpatialAnchorBinding(new SpatialAnchorOwnerId(owner.Kind, owner.Value), new LocationId(locationId.Value));
        byOwner.Add(owner.StableKey, binding); ownerByLocation.Add(locationId.Value, owner.StableKey); revision++;
        failure = SpatialAnchorBindingFailure.None; return true;
    }

    public SpatialAnchorBindingInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        if (revision < 0) violations.Add("Spatial anchor binding revision is negative.");
        if (byOwner.Count != ownerByLocation.Count) violations.Add("Spatial anchor owner and Location indexes have different counts.");
        foreach (SpatialAnchorBinding binding in Bindings)
        {
            if (binding?.OwnerId == null || binding.LocationId == null) { violations.Add("Spatial anchor binding is incomplete."); continue; }
            if (!spatialAuthorityStore.TryGet(binding.LocationId, out _)) violations.Add("Spatial anchor binding references an unregistered Location: " + binding.LocationId.Value + ".");
            if (!ownerByLocation.TryGetValue(binding.LocationId.Value, out string ownerKey) || !string.Equals(ownerKey, binding.OwnerId.StableKey, StringComparison.Ordinal)) violations.Add("Spatial anchor Location index is inconsistent for " + binding.OwnerId.StableKey + ".");
        }
        return new SpatialAnchorBindingInvariantReport(violations);
    }

    internal LegacySpatialAnchorBindingStore Clone(SpatialAuthorityStore targetSpatial)
    {
        LegacySpatialAnchorBindingStore copy = new LegacySpatialAnchorBindingStore(targetSpatial ?? throw new ArgumentNullException(nameof(targetSpatial)));
        foreach (SpatialAnchorBinding binding in Bindings)
        {
            if (!targetSpatial.TryGet(new LocationId(binding.LocationId.Value), out _)) throw new ArgumentException("Spatial anchor Location is absent from target SpatialAuthorityStore.", nameof(targetSpatial));
            copy.byOwner.Add(binding.OwnerId.StableKey, new SpatialAnchorBinding(new SpatialAnchorOwnerId(binding.OwnerId.Kind, binding.OwnerId.Value), new LocationId(binding.LocationId.Value)));
            copy.ownerByLocation.Add(binding.LocationId.Value, binding.OwnerId.StableKey);
        }
        copy.revision = revision; return copy;
    }

    private static bool Succeed(out SpatialAnchorBindingFailure failure) { failure = SpatialAnchorBindingFailure.None; return true; }
    private static bool Fail(SpatialAnchorBindingFailureCode code, string message, out SpatialAnchorBindingFailure failure) { failure = SpatialAnchorBindingFailure.Create(code, message); return false; }
    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

public sealed class SpatialAnchorBindingInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;
    internal SpatialAnchorBindingInvariantReport(IEnumerable<string> values) { List<string> result = values == null ? new List<string>() : new List<string>(values); result.Sort(StringComparer.Ordinal); Violations = new ReadOnlyCollection<string>(result); }
}
