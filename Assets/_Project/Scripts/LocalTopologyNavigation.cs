using System;
using System.Collections.Generic;

public sealed class LocalTopologyPath
{
    private readonly string startLocalPlaceRuntimeId;
    private readonly string destinationLocalPlaceRuntimeId;
    private readonly IReadOnlyList<string> localPlaceRuntimeIds;
    private readonly IReadOnlyList<string> localConnectionRuntimeIds;
    private readonly float totalCost;

    public string StartLocalPlaceRuntimeId => startLocalPlaceRuntimeId;
    public string DestinationLocalPlaceRuntimeId => destinationLocalPlaceRuntimeId;
    public IReadOnlyList<string> LocalPlaceRuntimeIds => localPlaceRuntimeIds;
    public IReadOnlyList<string> LocalConnectionRuntimeIds => localConnectionRuntimeIds;
    public float TotalCost => totalCost;

    public LocalTopologyPath(
        string startLocalPlaceRuntimeId,
        string destinationLocalPlaceRuntimeId,
        IEnumerable<string> localPlaceRuntimeIds,
        IEnumerable<string> localConnectionRuntimeIds,
        float totalCost)
    {
        if (string.IsNullOrWhiteSpace(startLocalPlaceRuntimeId) == true)
        {
            throw new ArgumentException("Local topology paths require a start LocalPlace RuntimeId.", nameof(startLocalPlaceRuntimeId));
        }

        if (string.IsNullOrWhiteSpace(destinationLocalPlaceRuntimeId) == true)
        {
            throw new ArgumentException("Local topology paths require a destination LocalPlace RuntimeId.", nameof(destinationLocalPlaceRuntimeId));
        }

        if (localPlaceRuntimeIds == null)
        {
            throw new ArgumentNullException(nameof(localPlaceRuntimeIds));
        }

        if (localConnectionRuntimeIds == null)
        {
            throw new ArgumentNullException(nameof(localConnectionRuntimeIds));
        }

        if (totalCost < 0f || float.IsNaN(totalCost) == true || float.IsInfinity(totalCost) == true)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCost));
        }

        List<string> placeIds = new List<string>(localPlaceRuntimeIds);
        List<string> connectionIds = new List<string>(localConnectionRuntimeIds);

        if (placeIds.Count == 0
            || string.Equals(placeIds[0], startLocalPlaceRuntimeId, StringComparison.Ordinal) == false
            || string.Equals(placeIds[placeIds.Count - 1], destinationLocalPlaceRuntimeId, StringComparison.Ordinal) == false
            || connectionIds.Count != Math.Max(0, placeIds.Count - 1))
        {
            throw new ArgumentException("Local topology path node and connection sequences are inconsistent.");
        }

        this.startLocalPlaceRuntimeId = startLocalPlaceRuntimeId;
        this.destinationLocalPlaceRuntimeId = destinationLocalPlaceRuntimeId;
        this.localPlaceRuntimeIds = placeIds.AsReadOnly();
        this.localConnectionRuntimeIds = connectionIds.AsReadOnly();
        this.totalCost = totalCost;
    }
}

[Serializable]
public sealed class LocalTopologyBlueprintNode
{
    public string LocalKey { get; set; }
    public string DisplayName { get; set; }
    public LocalPlaceTypeData TypeDefinition { get; set; }
    public string ParentLocalKey { get; set; }
    public bool IsEntryPoint { get; set; }

    public LocalTopologyBlueprintNode(
        string localKey,
        string displayName = null,
        LocalPlaceTypeData typeDefinition = null,
        string parentLocalKey = null,
        bool isEntryPoint = false)
    {
        LocalKey = localKey;
        DisplayName = displayName;
        TypeDefinition = typeDefinition;
        ParentLocalKey = parentLocalKey;
        IsEntryPoint = isEntryPoint;
    }
}

[Serializable]
public sealed class LocalTopologyBlueprintConnection
{
    public string OriginLocalKey { get; set; }
    public string DestinationLocalKey { get; set; }
    public float TraversalCost { get; set; }
    public LocalConnectionTypeData TypeDefinition { get; set; }

    public LocalTopologyBlueprintConnection(
        string originLocalKey,
        string destinationLocalKey,
        float traversalCost,
        LocalConnectionTypeData typeDefinition = null)
    {
        OriginLocalKey = originLocalKey;
        DestinationLocalKey = destinationLocalKey;
        TraversalCost = traversalCost;
        TypeDefinition = typeDefinition;
    }
}

public sealed class LocalTopologyBlueprint
{
    private readonly LocalTopologyOwnerReference owner;
    private readonly List<LocalTopologyBlueprintNode> nodes = new List<LocalTopologyBlueprintNode>();
    private readonly List<LocalTopologyBlueprintConnection> connections = new List<LocalTopologyBlueprintConnection>();
    private readonly List<string> entryPointKeys = new List<string>();
    private readonly IReadOnlyList<LocalTopologyBlueprintNode> readOnlyNodes;
    private readonly IReadOnlyList<LocalTopologyBlueprintConnection> readOnlyConnections;
    private readonly IReadOnlyList<string> readOnlyEntryPointKeys;

    public LocalTopologyOwnerReference Owner => owner;
    public IReadOnlyList<LocalTopologyBlueprintNode> Nodes => readOnlyNodes;
    public IReadOnlyList<LocalTopologyBlueprintConnection> Connections => readOnlyConnections;
    public IReadOnlyList<string> EntryPointKeys => readOnlyEntryPointKeys;

    public LocalTopologyBlueprint(LocalTopologyOwnerReference owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        readOnlyNodes = nodes.AsReadOnly();
        readOnlyConnections = connections.AsReadOnly();
        readOnlyEntryPointKeys = entryPointKeys.AsReadOnly();
    }

    public bool AddNode(LocalTopologyBlueprintNode node)
    {
        if (node == null)
        {
            return false;
        }

        nodes.Add(node);
        return true;
    }

    public bool AddConnection(LocalTopologyBlueprintConnection connection)
    {
        if (connection == null)
        {
            return false;
        }

        connections.Add(connection);
        return true;
    }

    public bool AddEntryPoint(string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey) == true)
        {
            return false;
        }

        entryPointKeys.Add(localKey);
        return true;
    }
}

public sealed class LocalTopologyBuilder
{
    private readonly RuntimeIdAllocator idAllocator;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly LocalTopologyStore topologyStore;

    public LocalTopologyBuilder(
        RuntimeIdAllocator idAllocator,
        RuntimeIdentityRegistry identityRegistry,
        LocalTopologyStore topologyStore)
    {
        this.idAllocator = idAllocator ?? throw new ArgumentNullException(nameof(idAllocator));
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.topologyStore = topologyStore ?? throw new ArgumentNullException(nameof(topologyStore));
    }

    public bool TryBuild(
        LocalTopologyBlueprint blueprint,
        out LocalTopologyRuntime topology,
        out string diagnostic)
    {
        topology = null;
        diagnostic = null;

        if (ValidateBlueprint(blueprint, out Dictionary<string, LocalTopologyBlueprintNode> nodesByKey, out diagnostic) == false)
        {
            return false;
        }

        if (topologyStore.HasTopologyForOwner(blueprint.Owner.OwnerRuntimeId) == true)
        {
            diagnostic = $"Owner '{blueprint.Owner.OwnerRuntimeId}' already has an active local topology.";
            return false;
        }

        List<string> localPlaceRuntimeIds = new List<string>();
        foreach (LocalTopologyBlueprintNode node in blueprint.Nodes)
        {
            string runtimeId = idAllocator.AllocateLocalPlaceId();
            localPlaceRuntimeIds.Add(runtimeId);
        }

        List<string> localConnectionRuntimeIds = new List<string>();
        foreach (LocalTopologyBlueprintConnection _ in blueprint.Connections)
        {
            string runtimeId = idAllocator.AllocateLocalConnectionId();
            localConnectionRuntimeIds.Add(runtimeId);
        }

        topology = new LocalTopologyRuntime(blueprint.Owner, identityRegistry);
        Dictionary<string, LocalPlaceRuntime> placesByKey = new Dictionary<string, LocalPlaceRuntime>(StringComparer.Ordinal);

        for (int i = 0; i < blueprint.Nodes.Count; i++)
        {
            LocalTopologyBlueprintNode node = blueprint.Nodes[i];
            LocalPlaceRuntime place = new LocalPlaceRuntime(
                localPlaceRuntimeIds[i],
                node.DisplayName,
                node.TypeDefinition);

            if (topology.AddPlace(place) == false)
            {
                topology = null;
                diagnostic = $"Could not materialize LocalPlace for blueprint key '{node.LocalKey}'.";
                return false;
            }

            placesByKey.Add(node.LocalKey, place);
        }

        foreach (LocalTopologyBlueprintNode node in blueprint.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.ParentLocalKey) == true)
            {
                continue;
            }

            if (topology.TrySetParent(
                placesByKey[node.LocalKey],
                placesByKey[node.ParentLocalKey],
                out diagnostic) == false)
            {
                topology = null;
                return false;
            }
        }

        List<string> entryPointKeys = new List<string>();
        foreach (LocalTopologyBlueprintNode node in blueprint.Nodes)
        {
            if (node.IsEntryPoint == true && entryPointKeys.Contains(node.LocalKey) == false)
            {
                entryPointKeys.Add(node.LocalKey);
            }
        }

        foreach (string entryPointKey in blueprint.EntryPointKeys)
        {
            if (entryPointKeys.Contains(entryPointKey) == false)
            {
                entryPointKeys.Add(entryPointKey);
            }
        }

        foreach (string entryPointKey in entryPointKeys)
        {
            if (topology.AddEntryPoint(placesByKey[entryPointKey]) == false)
            {
                topology = null;
                diagnostic = $"Could not materialize blueprint entry point '{entryPointKey}'.";
                return false;
            }
        }

        for (int i = 0; i < blueprint.Connections.Count; i++)
        {
            LocalTopologyBlueprintConnection blueprintConnection = blueprint.Connections[i];
            LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
                localConnectionRuntimeIds[i],
                placesByKey[blueprintConnection.OriginLocalKey],
                placesByKey[blueprintConnection.DestinationLocalKey],
                blueprintConnection.TraversalCost,
                blueprintConnection.TypeDefinition);

            if (topology.AddConnection(connection) == false)
            {
                topology = null;
                diagnostic = $"Could not materialize blueprint connection '{blueprintConnection.OriginLocalKey}' -> '{blueprintConnection.DestinationLocalKey}'.";
                return false;
            }
        }

        if (topology.TryValidate(out diagnostic) == false)
        {
            topology = null;
            return false;
        }

        if (topologyStore.TryAddTopology(topology, out diagnostic) == false)
        {
            topology = null;
            return false;
        }

        return true;
    }

    public LocalTopologyRuntime Build(LocalTopologyBlueprint blueprint)
    {
        if (TryBuild(blueprint, out LocalTopologyRuntime topology, out string diagnostic) == false)
        {
            throw new InvalidOperationException(diagnostic ?? "Could not build local topology blueprint.");
        }

        return topology;
    }

    private bool ValidateBlueprint(
        LocalTopologyBlueprint blueprint,
        out Dictionary<string, LocalTopologyBlueprintNode> nodesByKey,
        out string diagnostic)
    {
        nodesByKey = new Dictionary<string, LocalTopologyBlueprintNode>(StringComparer.Ordinal);
        diagnostic = null;

        if (blueprint == null || blueprint.Owner == null)
        {
            diagnostic = "Local topology blueprint requires an owner reference.";
            return false;
        }

        if (IsOwnerReferenceConsistent(blueprint.Owner, out diagnostic) == false)
        {
            return false;
        }

        foreach (LocalTopologyBlueprintNode node in blueprint.Nodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.LocalKey) == true)
            {
                diagnostic = "Local topology blueprint nodes require unique non-empty local keys.";
                return false;
            }

            if (nodesByKey.ContainsKey(node.LocalKey) == true)
            {
                diagnostic = $"Local topology blueprint contains duplicate local key '{node.LocalKey}'.";
                return false;
            }

            if (node.TypeDefinition != null && string.IsNullOrWhiteSpace(node.TypeDefinition.DefinitionId) == true)
            {
                diagnostic = $"Local topology blueprint node '{node.LocalKey}' has an invalid place type definition.";
                return false;
            }

            nodesByKey.Add(node.LocalKey, node);
        }

        foreach (LocalTopologyBlueprintNode node in blueprint.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.ParentLocalKey) == false
                && nodesByKey.ContainsKey(node.ParentLocalKey) == false)
            {
                diagnostic = $"Local topology blueprint node '{node.LocalKey}' references missing parent '{node.ParentLocalKey}'.";
                return false;
            }
        }

        foreach (LocalTopologyBlueprintNode node in blueprint.Nodes)
        {
            HashSet<string> hierarchyPath = new HashSet<string>(StringComparer.Ordinal);
            LocalTopologyBlueprintNode current = node;
            while (current != null && hierarchyPath.Add(current.LocalKey) == true)
            {
                current = string.IsNullOrWhiteSpace(current.ParentLocalKey) == false
                    ? nodesByKey[current.ParentLocalKey]
                    : null;
            }

            if (current != null)
            {
                diagnostic = $"Local topology blueprint hierarchy contains a cycle at '{current.LocalKey}'.";
                return false;
            }
        }

        HashSet<string> explicitEntryPointKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string entryPointKey in blueprint.EntryPointKeys)
        {
            if (string.IsNullOrWhiteSpace(entryPointKey) == true
                || nodesByKey.ContainsKey(entryPointKey) == false)
            {
                diagnostic = $"Local topology blueprint references unknown entry point '{entryPointKey}'.";
                return false;
            }

            if (explicitEntryPointKeys.Add(entryPointKey) == false)
            {
                diagnostic = $"Local topology blueprint contains duplicate entry point '{entryPointKey}'.";
                return false;
            }
        }

        foreach (LocalTopologyBlueprintConnection connection in blueprint.Connections)
        {
            if (connection == null
                || string.IsNullOrWhiteSpace(connection.OriginLocalKey) == true
                || string.IsNullOrWhiteSpace(connection.DestinationLocalKey) == true
                || nodesByKey.ContainsKey(connection.OriginLocalKey) == false
                || nodesByKey.ContainsKey(connection.DestinationLocalKey) == false)
            {
                diagnostic = "Local topology blueprint connections require known non-empty origin and destination keys.";
                return false;
            }

            if (string.Equals(connection.OriginLocalKey, connection.DestinationLocalKey, StringComparison.Ordinal) == true)
            {
                diagnostic = "Local topology blueprint connections cannot connect a node to itself.";
                return false;
            }

            if (LocalTopologyConnectionRuntime.IsValidTraversalCost(connection.TraversalCost) == false)
            {
                diagnostic = $"Local topology blueprint connection '{connection.OriginLocalKey}' -> '{connection.DestinationLocalKey}' has an invalid traversal cost.";
                return false;
            }

            if (connection.TypeDefinition != null && string.IsNullOrWhiteSpace(connection.TypeDefinition.DefinitionId) == true)
            {
                diagnostic = "Local topology blueprint connection has an invalid connection type definition.";
                return false;
            }
        }

        return true;
    }

    private bool IsOwnerReferenceConsistent(
        LocalTopologyOwnerReference owner,
        out string diagnostic)
    {
        diagnostic = null;

        if (owner.OwnerKind == LocalTopologyOwnerKind.City)
        {
            if (identityRegistry.TryGetCityWithoutLogging(owner.OwnerRuntimeId, out CityRuntime city) == false
                || city.Location == null
                || string.Equals(city.Location.RuntimeId, owner.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = $"Local topology blueprint owner '{owner.OwnerRuntimeId}' is not a coherent registered City reference.";
                return false;
            }

            return true;
        }

        if (owner.OwnerKind == LocalTopologyOwnerKind.ExplorableSite)
        {
            if (identityRegistry.TryGetExplorableSiteWithoutLogging(owner.OwnerRuntimeId, out ExplorableSiteRuntime site) == false
                || site.Location == null
                || string.Equals(site.Location.RuntimeId, owner.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = $"Local topology blueprint owner '{owner.OwnerRuntimeId}' is not a coherent registered ExplorableSite reference.";
                return false;
            }

            return true;
        }

        diagnostic = "Local topology blueprint owner kind is invalid.";
        return false;
    }
}
