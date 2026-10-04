using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Prepares the selected P10-B Ruin into private candidate objects without publishing owners.</summary>
public static class P10BGeneratedRuinGenesis
{
    internal enum PublicationStep
    {
        LocationRegistered,
        SiteIdentityRegistered,
        SiteStored,
        AnchorBound,
        TopologySpatiallyBound,
        TopologyStored
    }

    // Test-only injection seam; production leaves this null. The callback runs after each
    // mutation so every journal boundary can be exercised without changing domain inputs.
    internal static System.Action<PublicationStep> PublicationStepCompletedForTests;
    public sealed class PreparedRuin
    {
        public RuinLocalTopologyGeneration.Request Request { get; }
        public RuinLocalTopologyGeneration.GeneratedTopology GeneratedTopology { get; }
        public ExplorableSiteRuntime Site { get; }
        public SpatialLocationRuntime RuntimeLocation { get; }
        public LocationRecord Location { get; }
        public HexRecord Anchor { get; }
        public LocalTopologyRuntime Topology { get; }
        public LegacySpatialAnchorBindingStore AnchorBindings { get; }
        internal PreparedRuin(RuinLocalTopologyGeneration.Request request, RuinLocalTopologyGeneration.GeneratedTopology generated,
            ExplorableSiteRuntime site, SpatialLocationRuntime runtimeLocation, LocationRecord location, HexRecord anchor,
            LocalTopologyRuntime topology, LegacySpatialAnchorBindingStore bindings)
        { Request=request; GeneratedTopology=generated; Site=site; RuntimeLocation=runtimeLocation; Location=location; Anchor=anchor; Topology=topology; AnchorBindings=bindings; }
    }

    public static PreparedRuin Prepare(SimulationConfigData config, string selectedP9Fingerprint,
        SpatialAuthorityStore spatialAuthority, RuntimeIdentityRegistry identityRegistry, out string diagnostic)
    {
        diagnostic = null;
        if (config == null || spatialAuthority == null || identityRegistry == null)
        { diagnostic = "P10-B preparation requires selected config, P8/P9 spatial authority, and runtime identity registry."; return null; }
        if (!string.Equals(config.GenesisProfileContractIdentity, SimulationGenesisPipeline.P10BGeneratedRuinProfileContractIdentity, StringComparison.Ordinal))
        { diagnostic = "The selected genesis profile is not P10-B."; return null; }
        ExplorableSiteData definition = config.authoredP10RuinSite;
        if (definition == null || definition.kind != ExplorableSiteKind.Ruin || string.IsNullOrWhiteSpace(definition.DefinitionId))
        { diagnostic = "P10-B requires exactly one Ruin definition with a stable DefinitionId."; return null; }
        if (string.IsNullOrWhiteSpace(config.P10BStableSiteKey))
        { diagnostic = "P10-B requires an explicit non-empty StableSiteKey."; return null; }
        if (spatialAuthority.LocationCount != 1 || !spatialAuthority.TryGet(new LocationId(config.authoredLocationId), out LocationRecord location)
            || location.AnchorHexId == null || !spatialAuthority.TryGet(location.AnchorHexId, out HexRecord anchor))
        { diagnostic = "P10-B could not resolve the selected canonical P9 Location and registered P8 anchor."; return null; }

        long seed = config.useFixedSimulationSeed ? config.simulationSeed : 0L;
        RuinLocalTopologyGeneration.Request request;
        RuinLocalTopologyGeneration.GeneratedTopology generated;
        try
        {
            request = new RuinLocalTopologyGeneration.Request(selectedP9Fingerprint, seed, config.P10BStableSiteKey, definition.DefinitionId, location.Id.Value);
            generated = RuinLocalTopologyGeneration.Generate(request);
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
        { diagnostic = exception.Message; return null; }

        var runtimeLocation = new SpatialLocationRuntime(location.Id.Value);
        if (request.SiteInstanceId == definition.DefinitionId || request.SiteInstanceId == location.Id.Value
            || !identityRegistry.IsRuntimeIdAvailable(request.SiteInstanceId)
            || !identityRegistry.IsRuntimeIdAvailable(runtimeLocation.RuntimeId))
        { diagnostic = "P10-B site or runtime Location identity collides with the global RuntimeIdentityRegistry."; return null; }

        var site = new ExplorableSiteRuntime(request.SiteInstanceId, definition, runtimeLocation, request.SiteInstanceId);
        var owner = new LocalTopologyOwnerReference(site.RuntimeId, LocalTopologyOwnerKind.ExplorableSite,
            runtimeLocation.RuntimeId, LocalTopologySemanticOwnerReference.ForSiteInstanceId(request.SiteInstanceId, location.Id));
        var topology = new LocalTopologyRuntime(owner, identityRegistry);
        var places = new Dictionary<string, LocalPlaceRuntime>(StringComparer.Ordinal);
        foreach (RuinLocalTopologyGeneration.Place generatedPlace in generated.Places)
        {
            LocalPlaceRuntime parent = string.IsNullOrEmpty(generatedPlace.ParentLocalKey) ? null : places[generatedPlace.ParentLocalKey];
            var place = new LocalPlaceRuntime(generatedPlace.RuntimeId, generatedPlace.LocalKey, semanticId: generatedPlace.RuntimeId);
            if (!topology.AddPlace(place, parent, generatedPlace.IsEntry))
            { diagnostic = "Could not materialize generated LocalPlace " + generatedPlace.LocalKey; return null; }
            places.Add(generatedPlace.LocalKey, place);
        }
        foreach (RuinLocalTopologyGeneration.Connection edge in generated.Connections)
        {
            var connection = new LocalTopologyConnectionRuntime(edge.RuntimeId, places[edge.OriginLocalKey], places[edge.DestinationLocalKey],
                edge.TraversalCost, semanticId: edge.RuntimeId);
            if (!topology.AddConnection(connection))
            { diagnostic = "Could not materialize generated LocalTopology edge " + edge.LocalKey; return null; }
        }
        if (!topology.TryValidateStructure(out diagnostic)) return null;

        var bindings = new LegacySpatialAnchorBindingStore(spatialAuthority,
            ownerId => ownerId != null && ownerId.Kind == SpatialAnchorOwnerKind.ExplorableSite
                && string.Equals(ownerId.Value, site.RuntimeId, StringComparison.Ordinal));
        if (!bindings.TryBindSite(site.RuntimeId, location.Id, out SpatialAnchorBindingFailure bindingFailure))
        { diagnostic = "P10-B anchor binding could not be prepared: " + bindingFailure; return null; }

        foreach (RuinLocalTopologyGeneration.Place place in generated.Places)
            if (!identityRegistry.IsRuntimeIdAvailable(place.RuntimeId)) { diagnostic = "Generated LocalPlace ID collides globally: " + place.RuntimeId; return null; }
        foreach (RuinLocalTopologyGeneration.Connection edge in generated.Connections)
            if (!identityRegistry.IsRuntimeIdAvailable(edge.RuntimeId)) { diagnostic = "Generated LocalConnection ID collides globally: " + edge.RuntimeId; return null; }
        return new PreparedRuin(request, generated, site, runtimeLocation, location, anchor, topology, bindings);
    }

    /// <summary>
    /// Publishes the prepared owners while the composition is still private. All identities and
    /// indexes are preflighted first; the caller exposes the world only after this method succeeds.
    /// </summary>
    public static bool TryCommitPreparedRuin(PreparedRuin prepared, RuntimeIdentityRegistry identityRegistry,
        SpatialNetworkRuntime spatialNetwork, ExplorableSiteStore siteStore, LocalTopologyStore topologyStore,
        SpatialAuthorityStore spatialAuthority, LegacySpatialAnchorBindingStore anchorBindings, out string diagnostic)
    {
        diagnostic = null;
        if (prepared == null || identityRegistry == null || spatialNetwork == null || siteStore == null
            || topologyStore == null || spatialAuthority == null || anchorBindings == null)
        { diagnostic = "P10-B publication requires the complete prepared candidate and composition stores."; return false; }
        if (!topologyStore.UsesIdentityRegistry(identityRegistry))
        { diagnostic = "P10-B topology store must share the prepared RuntimeIdentityRegistry."; return false; }
        if (!spatialAuthority.TryGet(prepared.Location.Id, out LocationRecord currentLocation)
            || currentLocation.AnchorHexId != prepared.Anchor.Id
            || !spatialAuthority.TryGet(prepared.Anchor.Id, out _)
            || siteStore.GetByRuntimeId(prepared.Site.RuntimeId) != null
            || !identityRegistry.IsRuntimeIdAvailable(prepared.Site.RuntimeId)
            || !identityRegistry.IsRuntimeIdAvailable(prepared.RuntimeLocation.RuntimeId)
            || topologyStore.TryGetTopologyForOwner(prepared.Topology.Owner.OwnerRuntimeId, out _)
            || topologyStore.TryGetTopologyForSemanticOwner(prepared.Topology.Owner.SemanticOwner, out _)
            || spatialAuthority.TryGetTopologyBinding(prepared.Topology.Owner.OwnerKind,
                prepared.Topology.Owner.OwnerRuntimeId, out _)
            || anchorBindings.Count != 0
            || !prepared.AnchorBindings.ValidateInvariants().IsValid
            || !prepared.Topology.TryValidateStructure(out diagnostic))
        {
            if (string.IsNullOrEmpty(diagnostic)) diagnostic = "P10-B publication preflight failed because an owner, identity, anchor, or topology changed after preparation.";
            return false;
        }
        foreach (LocalPlaceRuntime place in prepared.Topology.Places)
            if (!identityRegistry.IsRuntimeIdAvailable(place.RuntimeId))
            { diagnostic = "Generated LocalPlace ID is no longer globally available: " + place.RuntimeId; return false; }
        foreach (LocalTopologyConnectionRuntime edge in prepared.Topology.Connections)
            if (!identityRegistry.IsRuntimeIdAvailable(edge.RuntimeId))
            { diagnostic = "Generated LocalConnection ID is no longer globally available: " + edge.RuntimeId; return false; }

        var undo = new Stack<System.Action>();
        try
        {
            if (!spatialNetwork.RegisterLocation(prepared.RuntimeLocation))
                throw new InvalidOperationException("P10-B could not register the generated Location.");
            undo.Push(() => spatialNetwork.RollbackGenesisLocation(prepared.RuntimeLocation));
            Complete(PublicationStep.LocationRegistered);

            if (!identityRegistry.RegisterExplorableSite(prepared.Site))
                throw new InvalidOperationException("P10-B could not register the generated Ruin identity.");
            undo.Push(() => identityRegistry.RollbackGenesisExplorableSite(prepared.Site));
            Complete(PublicationStep.SiteIdentityRegistered);

            if (!siteStore.Add(prepared.Site))
                throw new InvalidOperationException("P10-B could not add the generated Ruin to its store.");
            undo.Push(() => siteStore.RollbackGenesisSite(prepared.Site));
            Complete(PublicationStep.SiteStored);

            if (!anchorBindings.TryBindSite(prepared.Site.RuntimeId, prepared.Location.Id, out SpatialAnchorBindingFailure anchorFailure))
                throw new InvalidOperationException("P10-B canonical Location anchor binding failed: " + anchorFailure);
            undo.Push(() => anchorBindings.RollbackGenesisBinding(prepared.Site.RuntimeId, prepared.Location.Id));
            Complete(PublicationStep.AnchorBound);

            if (!spatialAuthority.TryBindLocalTopology(prepared.Topology, prepared.Location.Id, out SpatialAuthorityFailure bindingFailure))
                throw new InvalidOperationException("P10-B LocalTopology could not bind to the selected P9 Location: " + bindingFailure);
            undo.Push(() => spatialAuthority.RollbackGenesisTopologyBinding(prepared.Topology.Owner, prepared.Location.Id));
            Complete(PublicationStep.TopologySpatiallyBound);

            if (!topologyStore.TryAddTopology(prepared.Topology, out diagnostic))
                throw new InvalidOperationException(diagnostic ?? "P10-B topology store rejected the prepared topology.");
            undo.Push(() => topologyStore.RollbackGenesisTopology(prepared.Topology));
            Complete(PublicationStep.TopologyStored);
            return true;
        }
        catch (Exception exception)
        {
            var rollbackFailures = new List<string>();
            while (undo.Count > 0)
            {
                try { undo.Pop()(); }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException.Message);
                }
            }
            if (rollbackFailures.Count > 0)
            {
                diagnostic = "P10-B publication failed and one or more rollback operations failed: "
                    + string.Join(" | ", rollbackFailures);
                return false;
            }
            diagnostic = "P10-B atomic publication failed; all completed store mutations were rolled back: " + exception.Message;
            return false;
        }
    }

    private static void Complete(PublicationStep step) => PublicationStepCompletedForTests?.Invoke(step);

    public static IReadOnlyList<string> CreateProvenanceRecords(PreparedRuin prepared)
    {
        if (prepared == null) throw new ArgumentNullException(nameof(prepared));
        var records = new List<string>
        {
            "p10b-stage|" + SimulationGenesisPipeline.P10BGeneratedRuinStageId,
            "p10b-generator|" + RuinLocalTopologyGeneration.GeneratorId + "|" + RuinLocalTopologyGeneration.GeneratorVersion,
            "p10b-rules|" + RuinLocalTopologyGeneration.RulesRevision,
            "p10b-seed|" + prepared.Request.EffectiveGenesisSeed.ToString(CultureInfo.InvariantCulture),
            "p10b-stable-site-key|" + prepared.Request.StableSiteKey,
            "p10b-site-instance-id|" + prepared.Request.SiteInstanceId,
            "p10b-definition-id|" + prepared.Request.DefinitionId,
            "p10b-location-id|" + prepared.Request.LocationId,
            "p10b-request-fingerprint|" + prepared.Request.RequestFingerprint,
            "p10b-graph-fingerprint|" + prepared.GeneratedTopology.GraphFingerprint,
            "p10b-profile-fingerprint|" + prepared.GeneratedTopology.ComposedProfileFingerprint,
            "p10b-output-owner|ExplorableSite/" + prepared.Request.SiteInstanceId + "|LocalTopologyStore/" + prepared.Request.SiteInstanceId,
            "edge:" + SimulationGenesisPipeline.GeographyStageId + ">" + SimulationGenesisPipeline.P10BGeneratedRuinStageId,
            "edge:" + SimulationGenesisPipeline.P10BGeneratedRuinStageId + ">p9.genesis.authored-actors/v1"
        };
        return records.AsReadOnly();
    }
}
