using System;
using System.Collections.Generic;

/// <summary>
/// Unity-facing projection of a Core spatial resolution. Core owns Hex,
/// Location, Crossing, and their revision; this adapter adds existing P10
/// topology objects without duplicating P8 state.
/// </summary>
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

/// <summary>
/// Preserves existing Unity/P10 source APIs while delegating all P8 spatial
/// identity, binding, revision, and location resolution to Core's authority.
/// </summary>
public static class SpatialAuthorityTopologyAdapter
{
    public static bool TryBindLocalTopology(
        this SpatialAuthorityStore authority,
        LocalTopologyRuntime topology,
        LocationId locationId,
        out SpatialAuthorityFailure failure) => TryBindLocalTopologyCore(authority, topology, locationId, null, out failure);

    internal static bool TryBindLocalTopologyForP10Genesis(
        this SpatialAuthorityStore authority,
        LocalTopologyRuntime topology,
        LocationId locationId,
        Action<string> completedMutation,
        out SpatialAuthorityFailure failure) => TryBindLocalTopologyCore(authority, topology, locationId, completedMutation, out failure);

    private static bool TryBindLocalTopologyCore(
        SpatialAuthorityStore authority,
        LocalTopologyRuntime topology,
        LocationId locationId,
        Action<string> completedMutation,
        out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.None;
        if (authority == null)
            return Fail(SpatialAuthorityFailureCode.InvalidTopologyBinding, "A spatial authority is required.", out failure);
        if (!authority.CanMutateSpatialAuthority)
            return Fail(SpatialAuthorityFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (topology == null || topology.Owner == null || locationId == null)
            return Fail(SpatialAuthorityFailureCode.InvalidTopologyBinding, "A topology binding requires a topology, owner, and LocationId.", out failure);
        if (topology.Owner.SemanticOwner != null && topology.Owner.SemanticOwner.LocationId != locationId)
            return Fail(SpatialAuthorityFailureCode.InvalidTopologyBinding,
                "The semantic topology owner LocationId must match its P8 spatial binding.", out failure);
        if (!authority.TryGet(locationId, out LocationRecord location)
            || !authority.TryGet(location.AnchorHexId, out _))
            return Fail(SpatialAuthorityFailureCode.TopologyLocationNotRegistered,
                "The topology binding Location is not registered with a valid anchor.", out failure);
        if (topology.TryValidate(out string topologyDiagnostic) == false)
            return Fail(SpatialAuthorityFailureCode.InvalidTopologyBinding, topologyDiagnostic, out failure);

        return authority.TryBindTopologyOwner(
            topology.Owner.OwnerKind,
            topology.Owner.OwnerRuntimeId,
            locationId,
            completedMutation,
            out failure);
    }

    internal static void RollbackGenesisTopologyBinding(
        this SpatialAuthorityStore authority,
        LocalTopologyOwnerReference owner,
        LocationId locationId)
    {
        if (authority == null) throw new ArgumentNullException(nameof(authority));
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        authority.RollbackGenesisTopologyBinding(owner.OwnerKind, owner.OwnerRuntimeId, locationId);
    }

    public static bool TryResolve(
        this SpatialAuthorityStore authority,
        SpatialReference reference,
        LocalTopologyStore localTopologyStore,
        out SpatialResolution resolution,
        out SpatialAuthorityFailure failure)
    {
        resolution = null;
        failure = SpatialAuthorityFailure.None;
        if (authority == null)
            return Fail(SpatialAuthorityFailureCode.InvalidSpatialReference, "SpatialAuthorityStore is required.", out failure);
        if (reference == null || !Enum.IsDefined(typeof(SpatialReferenceKind), reference.Kind))
            return Fail(SpatialAuthorityFailureCode.InvalidSpatialReference, "SpatialReference is invalid.", out failure);

        if (reference.Kind != SpatialReferenceKind.SubLocation)
        {
            if (!authority.TryResolveReference(reference, out SpatialAuthorityResolution coreResolution, out failure))
                return false;
            resolution = new SpatialResolution(
                coreResolution.Reference,
                coreResolution.Hex,
                coreResolution.Location,
                coreResolution.Crossing);
            return true;
        }

        if (!reference.TopologyOwnerKind.HasValue
            || string.IsNullOrWhiteSpace(reference.TopologyOwnerRuntimeId)
            || string.IsNullOrWhiteSpace(reference.SubLocationRuntimeId))
            return Fail(SpatialAuthorityFailureCode.InvalidSpatialReference, "SubLocation SpatialReference is incomplete.", out failure);

        if (!authority.TryGetTopologyBinding(reference.TopologyOwnerKind.Value, reference.TopologyOwnerRuntimeId,
                out SpatialLocalTopologyBinding binding))
            return Fail(SpatialAuthorityFailureCode.TopologyNotRegistered, "SubLocation topology owner has no spatial binding.", out failure);

        if (localTopologyStore == null
            || !localTopologyStore.TryGetTopologyForOwner(reference.TopologyOwnerRuntimeId, out LocalTopologyRuntime topology)
            || topology.Owner.OwnerKind != reference.TopologyOwnerKind.Value)
            return Fail(SpatialAuthorityFailureCode.TopologyNotRegistered,
                "SubLocation topology is absent from the supplied LocalTopologyStore.", out failure);

        if (topology.Owner.SemanticOwner != null && topology.Owner.SemanticOwner.LocationId != binding.LocationId)
            return Fail(SpatialAuthorityFailureCode.InvalidTopologyBinding,
                "SubLocation semantic owner does not match its canonical P8 Location binding.", out failure);

        if (!topology.TryValidate(out string topologyDiagnostic)
            || !topology.TryGetPlace(reference.SubLocationRuntimeId, out LocalPlaceRuntime place))
            return Fail(SpatialAuthorityFailureCode.SubLocationNotRegistered,
                topologyDiagnostic ?? "SubLocation is absent from its owning topology.", out failure);

        if (!authority.TryGet(binding.LocationId, out LocationRecord boundLocation)
            || !authority.TryGet(boundLocation.AnchorHexId, out HexRecord boundHex))
            return Fail(SpatialAuthorityFailureCode.LocationNotRegistered,
                "SubLocation binding resolves to an absent Location or Hex.", out failure);

        resolution = new SpatialResolution(reference, boundHex, boundLocation, localTopology: topology, subLocation: place);
        return true;
    }

    public static SpatialAuthorityInvariantReport ValidateInvariants(
        this SpatialAuthorityStore authority,
        LocalTopologyStore localTopologyStore)
    {
        SpatialAuthorityInvariantReport baseReport = authority.ValidateInvariants();
        List<string> violations = new List<string>(baseReport.Violations);
        if (authority.LocalTopologyBindingCount == 0)
            return new SpatialAuthorityInvariantReport(violations);
        if (localTopologyStore == null)
        {
            violations.Add("Local topology bindings require a LocalTopologyStore for owner validation.");
            return new SpatialAuthorityInvariantReport(violations);
        }

        foreach (SpatialLocalTopologyBinding binding in authority.LocalTopologyBindings)
        {
            if (binding == null) continue;
            if (!localTopologyStore.TryGetTopologyForOwner(binding.OwnerRuntimeId, out LocalTopologyRuntime topology))
            {
                violations.Add("Spatial topology binding owner is absent from LocalTopologyStore: " + binding.StableKey + ".");
                continue;
            }
            if (topology.Owner == null || topology.Owner.OwnerKind != binding.OwnerKind)
            {
                violations.Add("Spatial topology binding owner kind does not match LocalTopologyStore: " + binding.StableKey + ".");
                continue;
            }
            if (topology.Owner.SemanticOwner != null && topology.Owner.SemanticOwner.LocationId != binding.LocationId)
                violations.Add("Spatial topology binding LocationId does not match the semantic owner: " + binding.StableKey + ".");
            if (!topology.TryValidate(out string diagnostic))
                violations.Add("Spatial topology binding owner is invalid: " + binding.StableKey + " (" + diagnostic + ").");
        }
        return new SpatialAuthorityInvariantReport(violations);
    }

    private static bool Fail(SpatialAuthorityFailureCode code, string message, out SpatialAuthorityFailure failure)
    {
        failure = SpatialAuthorityFailure.Create(code, message);
        return false;
    }
}

/// <summary>
/// Supplies only the P10 SubLocation projection needed by Core military
/// spatial owners. P8 authority and all mutation/revision ownership remain in
/// SpatialAuthorityStore.
/// </summary>
internal sealed class LocalTopologyArmedForceSubLocationResolver : IArmedForceSubLocationResolver
{
    internal LocalTopologyStore LocalTopologyStore { get; }

    internal LocalTopologyArmedForceSubLocationResolver(LocalTopologyStore localTopologyStore)
    {
        LocalTopologyStore = localTopologyStore ?? throw new ArgumentNullException(nameof(localTopologyStore));
    }

    public bool TryResolve(
        SpatialAuthorityStore authority,
        SpatialReference reference,
        out SpatialAuthorityResolution resolution,
        out SpatialAuthorityFailure failure)
    {
        resolution = null;
        if (!authority.TryResolve(reference, LocalTopologyStore,
                out SpatialResolution projection, out failure))
            return false;

        resolution = new SpatialAuthorityResolution(
            projection.Reference,
            projection.Hex,
            projection.Location,
            projection.Crossing);
        return true;
    }
}
