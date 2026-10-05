using System;
using System.Collections.Generic;

/// <summary>Bounded P10-A profile adapter for the first authored Ruin layout.</summary>
public static class P10RuinLocalTopologyGenesis
{
    public const string StageId = "p10.genesis.ruin-local-topology/v1";
    public const string ContractIdentity = "unity-authored-bootstrap/p10-ruin-local-topology-v1";
    public const int ContractSchemaVersion = 3;
    public const string CandidateOutputSchema = "P10LocalTopologyCandidate/v1";

    private static readonly P10LocalPlaceInput[] PlaceInputs =
    {
        new P10LocalPlaceInput("entrance", "Entrance", true),
        new P10LocalPlaceInput("courtyard", "Courtyard", false),
        new P10LocalPlaceInput("inner-chamber", "Inner Chamber", false)
    };

    private static readonly P10LocalConnectionInput[] ConnectionInputs =
    {
        new P10LocalConnectionInput("entrance-to-courtyard", "entrance", "courtyard", 1f),
        new P10LocalConnectionInput("courtyard-to-inner-chamber", "courtyard", "inner-chamber", 1f)
    };

    public static string CreateLegacySiteInstanceId(string definitionId) => LocalTopologySemanticOwnerReference.CreateP10ALegacySiteInstanceId(definitionId);

    public static string CreatePlaceSemanticId(string definitionId, string fixtureKey)
    {
        return WorldStateSnapshotValue.EncodeStableKey(
            "p10.local-topology/place/v1", definitionId, fixtureKey);
    }

    public static string CreateConnectionSemanticId(string definitionId, string fixtureKey)
    {
        return WorldStateSnapshotValue.EncodeStableKey(
            "p10.local-topology/connection/v1", definitionId, fixtureKey);
    }

    public static bool IsEnabled(SimulationConfigData config) => config != null && config.authoredP10RuinSite != null;

    public static P10RuinLocalTopologyCandidate CreateCandidate(
        SimulationConfigData config,
        SpatialAuthorityStore spatialAuthority)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (spatialAuthority == null) throw new ArgumentNullException(nameof(spatialAuthority));
        if (!config.useAuthoredGeographyProfile)
            throw new InvalidOperationException("P10 Ruin composition requires the selected P9-B geography profile.");
        ExplorableSiteData ruinDefinition = config.authoredP10RuinSite;
        if (ruinDefinition == null
            || string.IsNullOrWhiteSpace(ruinDefinition.DefinitionId)
            || ruinDefinition.kind != ExplorableSiteKind.Ruin)
            throw new InvalidOperationException("P10 requires one authored Ruin definition with a stable DefinitionId.");
        foreach (ExplorableSiteConfig existingSite in config.ExplorableSites)
            if (existingSite?.site != null
                && (existingSite.site.kind == ExplorableSiteKind.Ruin
                    || string.Equals(existingSite.site.DefinitionId, ruinDefinition.DefinitionId, StringComparison.Ordinal)))
                throw new InvalidOperationException("The P10 profile may compose exactly one Ruin, owned by its P10 profile input and not duplicated in P9 site/route inputs.");

        if (spatialAuthority.Locations.Count != 1)
            throw new InvalidOperationException("P10 requires the selected P9-B profile to supply exactly one canonical Location.");
        LocationRecord selectedInputLocation = spatialAuthority.Locations[0];
        if (selectedInputLocation?.Id == null
            || !string.Equals(selectedInputLocation.Id.Value, config.authoredLocationId, StringComparison.Ordinal)
            || !spatialAuthority.TryGet(selectedInputLocation.Id, out LocationRecord selectedLocation)
            || selectedLocation?.AnchorHexId == null
            || !spatialAuthority.TryGet(selectedLocation.AnchorHexId, out HexRecord selectedAnchor))
            throw new InvalidOperationException("P10 could not resolve the selected P9-B Location and its registered P8 anchor from the composed spatial authority.");

        return new P10RuinLocalTopologyCandidate(ruinDefinition, selectedLocation, selectedAnchor, PlaceInputs, ConnectionInputs);
    }

    public static bool TryCompose(
        P10RuinLocalTopologyCandidate candidate,
        RuntimeIdAllocator idAllocator,
        RuntimeIdentityRegistry identityRegistry,
        SpatialNetworkRuntime spatialNetwork,
        ExplorableSiteStore siteStore,
        SpatialAuthorityStore spatialAuthority,
        LocalTopologyStore localTopologyStore,
        out ExplorableSiteRuntime siteRuntime,
        out LocalTopologyRuntime topology,
        out LegacySpatialAnchorBindingStore legacyAnchorBindings,
        out string diagnostic)
    {
        siteRuntime = null;
        topology = null;
        legacyAnchorBindings = null;
        diagnostic = null;
        if (candidate == null || idAllocator == null || identityRegistry == null || spatialNetwork == null
            || siteStore == null || spatialAuthority == null || localTopologyStore == null)
        {
            diagnostic = "P10 composition requires the complete P9/P8 genesis authorities and runtime identity services.";
            return false;
        }
        if (!spatialAuthority.TryGet(candidate.Location.Id, out LocationRecord selectedLocation)
            || selectedLocation.AnchorHexId != candidate.Anchor.Id
            || !spatialAuthority.TryGet(candidate.Anchor.Id, out _))
        {
            diagnostic = "P10 candidate Location/anchor no longer matches the composed P8 spatial authority.";
            return false;
        }
        foreach (ExplorableSiteRuntime existing in siteStore.Sites)
            if (existing != null && (existing.Definition.kind == ExplorableSiteKind.Ruin
                || string.Equals(existing.DefinitionId, candidate.SiteDefinition.DefinitionId, StringComparison.Ordinal)))
            {
                diagnostic = "P10 Ruin cardinality or DefinitionId conflicts with a composed ExplorableSite.";
                return false;
            }

        SpatialLocationRuntime legacyLocation = new SpatialLocationRuntime(idAllocator.AllocateLocationId());
        ExplorableSiteRuntime pendingSite;
        try
        {
            pendingSite = new ExplorableSiteRuntime(idAllocator, candidate.SiteDefinition, legacyLocation, CreateLegacySiteInstanceId(candidate.SiteDefinition.DefinitionId));
        }
        catch (ArgumentException exception)
        {
            diagnostic = exception.Message;
            return false;
        }

        if (!spatialNetwork.RegisterLocation(legacyLocation)
            || !identityRegistry.RegisterExplorableSite(pendingSite)
            || !siteStore.Add(pendingSite))
        {
            diagnostic = "P10 could not register the Ruin through the existing site/runtime identity owners.";
            return false;
        }

        legacyAnchorBindings = new LegacySpatialAnchorBindingStore(
            spatialAuthority,
            owner => owner != null
                && owner.Kind == SpatialAnchorOwnerKind.ExplorableSite
                && siteStore.TryGetByRuntimeId(owner.Value, out _));
        if (!legacyAnchorBindings.TryBindSite(pendingSite.RuntimeId, candidate.Location.Id, out SpatialAnchorBindingFailure anchorFailure))
        {
            diagnostic = "P10 Ruin P8 anchor binding failed: " + anchorFailure;
            return false;
        }

        LocalTopologySemanticOwnerReference semanticOwner =
            LocalTopologySemanticOwnerReference.ForP10ALegacy(candidate.SiteDefinition.DefinitionId, candidate.Location.Id);
        topology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForSemanticExplorableSite(pendingSite, candidate.Location.Id),
            identityRegistry);

        Dictionary<string, LocalPlaceRuntime> placesByKey = new Dictionary<string, LocalPlaceRuntime>(StringComparer.Ordinal);
        foreach (P10LocalPlaceInput placeInput in candidate.Places)
        {
            string stableId = CreatePlaceSemanticId(candidate.SiteDefinition.DefinitionId, placeInput.Key);
            LocalPlaceRuntime place = new LocalPlaceRuntime(
                idAllocator.AllocateLocalPlaceId(), placeInput.DisplayName, semanticId: stableId);
            if (!topology.AddPlace(place, isEntryPoint: placeInput.IsEntryPoint))
            {
                diagnostic = "P10 Ruin topology rejected a semantic place: " + placeInput.Key;
                return false;
            }
            placesByKey.Add(placeInput.Key, place);
        }

        foreach (P10LocalConnectionInput connectionInput in candidate.Connections)
        {
            if (!placesByKey.TryGetValue(connectionInput.OriginKey, out LocalPlaceRuntime origin)
                || !placesByKey.TryGetValue(connectionInput.DestinationKey, out LocalPlaceRuntime destination))
            {
                diagnostic = "P10 Ruin topology connection references an absent semantic place.";
                return false;
            }
            string stableId = CreateConnectionSemanticId(candidate.SiteDefinition.DefinitionId, connectionInput.Key);
            LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
                idAllocator.AllocateLocalConnectionId(), origin, destination, connectionInput.TraversalCost,
                semanticId: stableId);
            if (!topology.AddConnection(connection))
            {
                diagnostic = "P10 Ruin topology rejected an explicit local connection: " + connectionInput.Key;
                return false;
            }
        }

        if (!topology.TryValidate(out diagnostic)) return false;
        if (!localTopologyStore.TryAddTopology(topology, out diagnostic)) return false;
        if (!spatialAuthority.TryBindLocalTopology(topology, candidate.Location.Id, out SpatialAuthorityFailure topologyBindingFailure))
        {
            diagnostic = "P10 LocalTopology P8 binding failed: " + topologyBindingFailure;
            return false;
        }

        siteRuntime = pendingSite;
        return true;
    }

    public static IReadOnlyList<string> CreateProvenanceRecords(P10RuinLocalTopologyCandidate candidate)
    {
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));
        var records = new List<string>
        {
            "combined-profile|" + ContractIdentity,
            "combined-schema|" + ContractSchemaVersion,
            "selected-p9-geography-profile|" + SimulationGenesisPipeline.GeographyProfileContractIdentity,
            "p10-stage|" + StageId,
            "p10-stage-version|1",
            "p10-stage-input|" + CandidateOutputSchema + "|authored-site-definition|p9-spatial-authority",
            "p10-stage-output|" + CandidateOutputSchema + "|ExplorableSiteStore|LocalTopologyStore|LegacySpatialAnchorBindingStore|SpatialAuthorityStore",
            "p10-output-owner|ExplorableSite/" + candidate.SiteDefinition.DefinitionId + "|Location/" + candidate.Location.Id.Value,
            "p10-selected-location|" + candidate.Location.Id.Value,
            "p10-location-anchor|" + candidate.Location.Id.Value + "|" + candidate.Anchor.Id.Value,
            "edge:p9.genesis.authored-world/v1>" + StageId,
            "edge:" + SimulationGenesisPipeline.GeographyStageId + ">" + StageId,
            "edge:" + StageId + ">p9.genesis.validate-profile/v1",
            "combined-stage-order|" + WorldStateSnapshotValue.EncodeStableKey(
                "p9.genesis.resolve-profile/v1", "p9.genesis.authored-world/v1",
                SimulationGenesisPipeline.GeographyStageId, "p9.genesis.authored-actors/v1",
                StageId, "p9.genesis.validate-profile/v1", "p9.genesis.publish/v1")
        };
        foreach (P10LocalPlaceInput place in candidate.Places)
            records.Add("p10-place|" + CreatePlaceSemanticId(candidate.SiteDefinition.DefinitionId, place.Key)
                + "|" + place.Key + "|" + place.DisplayName + "|entry=" + place.IsEntryPoint);
        foreach (P10LocalConnectionInput connection in candidate.Connections)
            records.Add("p10-connection|" + CreateConnectionSemanticId(candidate.SiteDefinition.DefinitionId, connection.Key)
                + "|" + connection.Key + "|" + connection.OriginKey + "|" + connection.DestinationKey
                + "|cost=" + connection.TraversalCost.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        return records.AsReadOnly();
    }
}
public sealed class P10RuinLocalTopologyCandidate
{
    public ExplorableSiteData SiteDefinition { get; }
    public LocationRecord Location { get; }
    public HexRecord Anchor { get; }
    public IReadOnlyList<P10LocalPlaceInput> Places { get; }
    public IReadOnlyList<P10LocalConnectionInput> Connections { get; }

    internal P10RuinLocalTopologyCandidate(
        ExplorableSiteData siteDefinition,
        LocationRecord location,
        HexRecord anchor,
        IEnumerable<P10LocalPlaceInput> places,
        IEnumerable<P10LocalConnectionInput> connections)
    {
        SiteDefinition = siteDefinition ?? throw new ArgumentNullException(nameof(siteDefinition));
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
        Places = Array.AsReadOnly(new List<P10LocalPlaceInput>(places ?? throw new ArgumentNullException(nameof(places))).ToArray());
        Connections = Array.AsReadOnly(new List<P10LocalConnectionInput>(connections ?? throw new ArgumentNullException(nameof(connections))).ToArray());
    }
}

public sealed class P10LocalPlaceInput
{
    public string Key { get; }
    public string DisplayName { get; }
    public bool IsEntryPoint { get; }

    internal P10LocalPlaceInput(string key, string displayName, bool isEntryPoint)
    {
        Key = key;
        DisplayName = displayName;
        IsEntryPoint = isEntryPoint;
    }
}

public sealed class P10LocalConnectionInput
{
    public string Key { get; }
    public string OriginKey { get; }
    public string DestinationKey { get; }
    public float TraversalCost { get; }

    internal P10LocalConnectionInput(string key, string originKey, string destinationKey, float traversalCost)
    {
        Key = key;
        OriginKey = originKey;
        DestinationKey = destinationKey;
        TraversalCost = traversalCost;
    }
}
