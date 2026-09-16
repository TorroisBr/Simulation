using System;
using System.Collections.Generic;

public enum LocalTopologyOwnerKind
{
    City,
    ExplorableSite
}

[Serializable]
public sealed class LocalTopologyOwnerReference
{
    private readonly string ownerRuntimeId;
    private readonly LocalTopologyOwnerKind ownerKind;
    private readonly string macroLocationRuntimeId;

    public string OwnerRuntimeId => ownerRuntimeId;
    public LocalTopologyOwnerKind OwnerKind => ownerKind;
    public string MacroLocationRuntimeId => macroLocationRuntimeId;

    public LocalTopologyOwnerReference(
        string ownerRuntimeId,
        LocalTopologyOwnerKind ownerKind,
        string macroLocationRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Local topology owners require a non-empty owner RuntimeId.", nameof(ownerRuntimeId));
        }

        if (Enum.IsDefined(typeof(LocalTopologyOwnerKind), ownerKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerKind));
        }

        if (string.IsNullOrWhiteSpace(macroLocationRuntimeId) == true)
        {
            throw new ArgumentException("Local topology owners require a non-empty macro location RuntimeId.", nameof(macroLocationRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
        this.ownerKind = ownerKind;
        this.macroLocationRuntimeId = macroLocationRuntimeId;
    }

    public static LocalTopologyOwnerReference ForCity(CityRuntime cityRuntime)
    {
        if (cityRuntime == null)
        {
            throw new ArgumentNullException(nameof(cityRuntime));
        }

        return new LocalTopologyOwnerReference(
            cityRuntime.RuntimeId,
            LocalTopologyOwnerKind.City,
            cityRuntime.Location?.RuntimeId);
    }

    public static LocalTopologyOwnerReference ForExplorableSite(ExplorableSiteRuntime siteRuntime)
    {
        if (siteRuntime == null)
        {
            throw new ArgumentNullException(nameof(siteRuntime));
        }

        return new LocalTopologyOwnerReference(
            siteRuntime.RuntimeId,
            LocalTopologyOwnerKind.ExplorableSite,
            siteRuntime.Location?.RuntimeId);
    }
}

[Serializable]
public sealed class LocalPlaceRuntime
{
    private readonly string runtimeId;
    private readonly string displayName;
    private readonly LocalPlaceTypeData typeDefinition;
    private LocalTopologyRuntime owningTopology;
    private LocalPlaceRuntime parent;

    public string RuntimeId => runtimeId;
    public string DisplayName => displayName;
    public LocalPlaceTypeData TypeDefinition => typeDefinition;
    public string TypeDefinitionId => typeDefinition != null ? typeDefinition.DefinitionId : string.Empty;
    public LocalPlaceRuntime Parent => parent;
    public LocalTopologyRuntime OwningTopology => owningTopology;

    public LocalPlaceRuntime(
        string runtimeId,
        string displayName = null,
        LocalPlaceTypeData typeDefinition = null)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("LocalPlaceRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
        this.displayName = displayName ?? string.Empty;
        this.typeDefinition = typeDefinition;
    }

    public LocalPlaceRuntime(
        RuntimeIdAllocator idAllocator,
        string displayName = null,
        LocalPlaceTypeData typeDefinition = null)
        : this(
            (idAllocator ?? throw new ArgumentNullException(nameof(idAllocator))).AllocateLocalPlaceId(),
            displayName,
            typeDefinition)
    {
    }

    internal bool AttachToTopology(LocalTopologyRuntime topology)
    {
        if (topology == null || (owningTopology != null && owningTopology != topology) == true)
        {
            return false;
        }

        owningTopology = topology;
        return true;
    }

    internal void SetParentReference(LocalPlaceRuntime parent)
    {
        this.parent = parent;
    }
}

[Serializable]
public sealed class LocalTopologyConnectionRuntime
{
    private readonly string runtimeId;
    private readonly LocalPlaceRuntime origin;
    private readonly LocalPlaceRuntime destination;
    private readonly float traversalCost;
    private readonly LocalConnectionTypeData typeDefinition;
    private LocalTopologyRuntime owningTopology;

    public string RuntimeId => runtimeId;
    public LocalPlaceRuntime Origin => origin;
    public LocalPlaceRuntime Destination => destination;
    public float TraversalCost => traversalCost;
    public LocalConnectionTypeData TypeDefinition => typeDefinition;
    public string TypeDefinitionId => typeDefinition != null ? typeDefinition.DefinitionId : string.Empty;
    public LocalTopologyRuntime OwningTopology => owningTopology;

    public LocalTopologyConnectionRuntime(
        string runtimeId,
        LocalPlaceRuntime origin,
        LocalPlaceRuntime destination,
        float traversalCost,
        LocalConnectionTypeData typeDefinition = null)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("LocalTopologyConnectionRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        if (origin == null)
        {
            throw new ArgumentNullException(nameof(origin));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (origin == destination)
        {
            throw new ArgumentException("Local topology connections cannot connect a place to itself.", nameof(destination));
        }

        if (IsValidTraversalCost(traversalCost) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(traversalCost));
        }

        this.runtimeId = runtimeId;
        this.origin = origin;
        this.destination = destination;
        this.traversalCost = traversalCost;
        this.typeDefinition = typeDefinition;
    }

    public LocalTopologyConnectionRuntime(
        RuntimeIdAllocator idAllocator,
        LocalPlaceRuntime origin,
        LocalPlaceRuntime destination,
        float traversalCost,
        LocalConnectionTypeData typeDefinition = null)
        : this(
            (idAllocator ?? throw new ArgumentNullException(nameof(idAllocator))).AllocateLocalConnectionId(),
            origin,
            destination,
            traversalCost,
            typeDefinition)
    {
    }

    public static bool IsValidTraversalCost(float value)
    {
        return value > 0f && float.IsNaN(value) == false && float.IsInfinity(value) == false;
    }

    internal bool AttachToTopology(LocalTopologyRuntime topology)
    {
        if (topology == null || (owningTopology != null && owningTopology != topology) == true)
        {
            return false;
        }

        owningTopology = topology;
        return true;
    }
}

public sealed class LocalTopologyRuntime
{
    private readonly LocalTopologyOwnerReference owner;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly List<LocalPlaceRuntime> places = new List<LocalPlaceRuntime>();
    private readonly List<LocalTopologyConnectionRuntime> connections = new List<LocalTopologyConnectionRuntime>();
    private readonly List<LocalPlaceRuntime> entryPoints = new List<LocalPlaceRuntime>();
    private readonly Dictionary<string, LocalPlaceRuntime> placesByRuntimeId =
        new Dictionary<string, LocalPlaceRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalTopologyConnectionRuntime> connectionsByRuntimeId =
        new Dictionary<string, LocalTopologyConnectionRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<LocalPlaceRuntime, List<LocalPlaceRuntime>> childrenByParent =
        new Dictionary<LocalPlaceRuntime, List<LocalPlaceRuntime>>();
    private readonly Dictionary<LocalPlaceRuntime, List<LocalTopologyConnectionRuntime>> outgoingConnections =
        new Dictionary<LocalPlaceRuntime, List<LocalTopologyConnectionRuntime>>();

    public LocalTopologyOwnerReference Owner => owner;
    public IReadOnlyList<LocalPlaceRuntime> Places => places.AsReadOnly();
    public IReadOnlyList<LocalTopologyConnectionRuntime> Connections => connections.AsReadOnly();
    public IReadOnlyList<LocalPlaceRuntime> EntryPoints => entryPoints.AsReadOnly();
    public int NodeCount => places.Count;
    public int ConnectionCount => connections.Count;
    public int EntryPointCount => entryPoints.Count;
    public int MaxDepth
    {
        get
        {
            int maxDepth = 0;
            foreach (LocalPlaceRuntime place in places)
            {
                maxDepth = Math.Max(maxDepth, GetDepth(place));
            }

            return maxDepth;
        }
    }

    internal RuntimeIdentityRegistry IdentityRegistry => identityRegistry;

    public LocalTopologyRuntime(
        LocalTopologyOwnerReference owner,
        RuntimeIdentityRegistry identityRegistry = null)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.identityRegistry = identityRegistry;
    }

    public bool AddPlace(
        LocalPlaceRuntime place,
        LocalPlaceRuntime parent = null,
        bool isEntryPoint = false)
    {
        if (place == null
            || places.Contains(place) == true
            || placesByRuntimeId.ContainsKey(place.RuntimeId) == true
            || place.OwningTopology != null
            || (parent != null && ContainsPlace(parent) == false)
            || parent == place)
        {
            return false;
        }

        if (identityRegistry != null && identityRegistry.RegisterLocalPlace(place) == false)
        {
            return false;
        }

        if (place.AttachToTopology(this) == false)
        {
            return false;
        }

        places.Add(place);
        placesByRuntimeId.Add(place.RuntimeId, place);
        childrenByParent.Add(place, new List<LocalPlaceRuntime>());
        outgoingConnections.Add(place, new List<LocalTopologyConnectionRuntime>());

        if (parent != null)
        {
            place.SetParentReference(parent);
            childrenByParent[parent].Add(place);
        }

        if (isEntryPoint == true)
        {
            entryPoints.Add(place);
        }

        return true;
    }

    public bool TrySetParent(
        LocalPlaceRuntime place,
        LocalPlaceRuntime parent,
        out string diagnostic)
    {
        diagnostic = null;

        if (ContainsPlace(place) == false)
        {
            diagnostic = "The child LocalPlace must belong to this topology.";
            return false;
        }

        if (parent == place)
        {
            diagnostic = "A LocalPlace cannot be its own parent.";
            return false;
        }

        if (parent != null && ContainsPlace(parent) == false)
        {
            diagnostic = "The parent LocalPlace must belong to this topology.";
            return false;
        }

        if (WouldCreateCycle(place, parent) == true)
        {
            diagnostic = "The requested LocalPlace parent would create a hierarchy cycle.";
            return false;
        }

        LocalPlaceRuntime previousParent = place.Parent;
        if (previousParent == parent)
        {
            return true;
        }

        if (previousParent != null)
        {
            childrenByParent[previousParent].Remove(place);
        }

        place.SetParentReference(parent);

        if (parent != null)
        {
            childrenByParent[parent].Add(place);
        }

        return true;
    }

    public bool AddEntryPoint(LocalPlaceRuntime place)
    {
        if (ContainsPlace(place) == false || entryPoints.Contains(place) == true)
        {
            return false;
        }

        entryPoints.Add(place);
        return true;
    }

    public bool AddConnection(LocalTopologyConnectionRuntime connection)
    {
        if (connection == null
            || connections.Contains(connection) == true
            || connectionsByRuntimeId.ContainsKey(connection.RuntimeId) == true
            || connection.Origin == connection.Destination
            || ContainsPlace(connection.Origin) == false
            || ContainsPlace(connection.Destination) == false
            || LocalTopologyConnectionRuntime.IsValidTraversalCost(connection.TraversalCost) == false
            || connection.OwningTopology != null)
        {
            return false;
        }

        if (identityRegistry != null && identityRegistry.RegisterLocalConnection(connection) == false)
        {
            return false;
        }

        if (connection.AttachToTopology(this) == false)
        {
            return false;
        }

        connections.Add(connection);
        connectionsByRuntimeId.Add(connection.RuntimeId, connection);
        outgoingConnections[connection.Origin].Add(connection);
        return true;
    }

    public bool ContainsPlace(LocalPlaceRuntime place)
    {
        return place != null
            && place.OwningTopology == this
            && places.Contains(place);
    }

    public bool ContainsPlace(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == false
            && placesByRuntimeId.ContainsKey(runtimeId);
    }

    public bool ContainsConnection(LocalTopologyConnectionRuntime connection)
    {
        return connection != null
            && connection.OwningTopology == this
            && connections.Contains(connection);
    }

    public bool TryGetPlace(string runtimeId, out LocalPlaceRuntime place)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && placesByRuntimeId.TryGetValue(runtimeId, out place) == true)
        {
            return true;
        }

        place = null;
        return false;
    }

    public bool TryGetConnection(string runtimeId, out LocalTopologyConnectionRuntime connection)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && connectionsByRuntimeId.TryGetValue(runtimeId, out connection) == true)
        {
            return true;
        }

        connection = null;
        return false;
    }

    public LocalPlaceRuntime GetParent(LocalPlaceRuntime place)
    {
        return ContainsPlace(place) == true ? place.Parent : null;
    }

    public IReadOnlyList<LocalPlaceRuntime> GetChildren(LocalPlaceRuntime parent)
    {
        if (ContainsPlace(parent) == true && childrenByParent.TryGetValue(parent, out List<LocalPlaceRuntime> children) == true)
        {
            return children.AsReadOnly();
        }

        return Array.Empty<LocalPlaceRuntime>();
    }

    public IReadOnlyList<LocalPlaceRuntime> GetAncestors(LocalPlaceRuntime place)
    {
        List<LocalPlaceRuntime> ancestors = new List<LocalPlaceRuntime>();

        if (ContainsPlace(place) == false)
        {
            return ancestors.AsReadOnly();
        }

        HashSet<LocalPlaceRuntime> visited = new HashSet<LocalPlaceRuntime>();
        LocalPlaceRuntime current = place.Parent;
        while (current != null && visited.Add(current) == true)
        {
            ancestors.Add(current);
            current = current.Parent;
        }

        return ancestors.AsReadOnly();
    }

    public int GetDepth(LocalPlaceRuntime place)
    {
        if (ContainsPlace(place) == false)
        {
            return -1;
        }

        int depth = 0;
        HashSet<LocalPlaceRuntime> visited = new HashSet<LocalPlaceRuntime>();
        LocalPlaceRuntime current = place.Parent;
        while (current != null && visited.Add(current) == true)
        {
            depth++;
            current = current.Parent;
        }

        return current == null ? depth : -1;
    }

    public bool IsDescendantOf(LocalPlaceRuntime descendant, LocalPlaceRuntime ancestor)
    {
        if (ContainsPlace(descendant) == false || ContainsPlace(ancestor) == false || descendant == ancestor)
        {
            return false;
        }

        LocalPlaceRuntime current = descendant.Parent;
        HashSet<LocalPlaceRuntime> visited = new HashSet<LocalPlaceRuntime>();
        while (current != null && visited.Add(current) == true)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    public bool IsEntryPoint(LocalPlaceRuntime place)
    {
        return ContainsPlace(place) == true && entryPoints.Contains(place);
    }

    public IReadOnlyList<LocalTopologyConnectionRuntime> GetOutgoingConnections(LocalPlaceRuntime origin)
    {
        if (ContainsPlace(origin) == true
            && outgoingConnections.TryGetValue(origin, out List<LocalTopologyConnectionRuntime> outgoing) == true)
        {
            return outgoing.AsReadOnly();
        }

        return Array.Empty<LocalTopologyConnectionRuntime>();
    }

    public bool TryFindShortestPath(
        string startLocalPlaceRuntimeId,
        string targetLocalPlaceRuntimeId,
        out LocalTopologyPath path)
    {
        path = null;

        if (TryGetPlace(startLocalPlaceRuntimeId, out LocalPlaceRuntime start) == false
            || TryGetPlace(targetLocalPlaceRuntimeId, out LocalPlaceRuntime target) == false)
        {
            return false;
        }

        if (start == target)
        {
            path = new LocalTopologyPath(
                start.RuntimeId,
                target.RuntimeId,
                new[] { start.RuntimeId },
                Array.Empty<string>(),
                0f);
            return true;
        }

        Dictionary<string, float> distances = new Dictionary<string, float>(StringComparer.Ordinal);
        Dictionary<string, string> previousPlaceRuntimeIds = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> previousConnectionRuntimeIds = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> settledPlaceRuntimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (LocalPlaceRuntime place in places)
        {
            distances.Add(place.RuntimeId, float.PositiveInfinity);
        }

        distances[start.RuntimeId] = 0f;

        while (true)
        {
            LocalPlaceRuntime current = SelectNextUnsettledPlace(distances, settledPlaceRuntimeIds);
            if (current == null)
            {
                break;
            }

            settledPlaceRuntimeIds.Add(current.RuntimeId);
            if (current == target)
            {
                break;
            }

            foreach (LocalTopologyConnectionRuntime connection in GetOutgoingConnections(current))
            {
                if (connection == null || settledPlaceRuntimeIds.Contains(connection.Destination.RuntimeId) == true)
                {
                    continue;
                }

                float candidateDistance = distances[current.RuntimeId] + connection.TraversalCost;
                if (float.IsInfinity(candidateDistance) == true
                    || candidateDistance >= distances[connection.Destination.RuntimeId])
                {
                    continue;
                }

                distances[connection.Destination.RuntimeId] = candidateDistance;
                previousPlaceRuntimeIds[connection.Destination.RuntimeId] = current.RuntimeId;
                previousConnectionRuntimeIds[connection.Destination.RuntimeId] = connection.RuntimeId;
            }
        }

        if (float.IsInfinity(distances[target.RuntimeId]) == true)
        {
            return false;
        }

        List<string> reversePlaceRuntimeIds = new List<string>();
        List<string> reverseConnectionRuntimeIds = new List<string>();
        string currentRuntimeId = target.RuntimeId;
        reversePlaceRuntimeIds.Add(currentRuntimeId);

        while (string.Equals(currentRuntimeId, start.RuntimeId, StringComparison.Ordinal) == false)
        {
            if (previousPlaceRuntimeIds.TryGetValue(currentRuntimeId, out string previousPlaceRuntimeId) == false
                || previousConnectionRuntimeIds.TryGetValue(currentRuntimeId, out string previousConnectionRuntimeId) == false)
            {
                return false;
            }

            reverseConnectionRuntimeIds.Add(previousConnectionRuntimeId);
            currentRuntimeId = previousPlaceRuntimeId;
            reversePlaceRuntimeIds.Add(currentRuntimeId);
        }

        reversePlaceRuntimeIds.Reverse();
        reverseConnectionRuntimeIds.Reverse();
        path = new LocalTopologyPath(
            start.RuntimeId,
            target.RuntimeId,
            reversePlaceRuntimeIds,
            reverseConnectionRuntimeIds,
            distances[target.RuntimeId]);
        return true;
    }

    public bool TryValidate(out string diagnostic)
    {
        diagnostic = null;

        if (owner == null
            || Enum.IsDefined(typeof(LocalTopologyOwnerKind), owner.OwnerKind) == false
            || string.IsNullOrWhiteSpace(owner.OwnerRuntimeId) == true
            || string.IsNullOrWhiteSpace(owner.MacroLocationRuntimeId) == true)
        {
            diagnostic = "Local topology owner reference is invalid.";
            return false;
        }

        if (identityRegistry != null && IsOwnerReferenceConsistent(out diagnostic) == false)
        {
            return false;
        }

        HashSet<string> placeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (LocalPlaceRuntime place in places)
        {
            if (place == null
                || string.IsNullOrWhiteSpace(place.RuntimeId) == true
                || place.OwningTopology != this
                || placeIds.Add(place.RuntimeId) == false
                || placesByRuntimeId.TryGetValue(place.RuntimeId, out LocalPlaceRuntime mappedPlace) == false
                || mappedPlace != place)
            {
                diagnostic = "Local topology contains an invalid or duplicate LocalPlace.";
                return false;
            }

            if (place.Parent != null && ContainsPlace(place.Parent) == false)
            {
                diagnostic = $"LocalPlace '{place.RuntimeId}' has a foreign parent.";
                return false;
            }

            if (GetDepth(place) < 0)
            {
                diagnostic = $"LocalPlace '{place.RuntimeId}' participates in a hierarchy cycle.";
                return false;
            }
        }

        HashSet<string> connectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (LocalTopologyConnectionRuntime connection in connections)
        {
            if (connection == null
                || string.IsNullOrWhiteSpace(connection.RuntimeId) == true
                || connection.OwningTopology != this
                || connectionIds.Add(connection.RuntimeId) == false
                || connectionsByRuntimeId.TryGetValue(connection.RuntimeId, out LocalTopologyConnectionRuntime mappedConnection) == false
                || mappedConnection != connection
                || ContainsPlace(connection.Origin) == false
                || ContainsPlace(connection.Destination) == false
                || LocalTopologyConnectionRuntime.IsValidTraversalCost(connection.TraversalCost) == false)
            {
                diagnostic = "Local topology contains an invalid connection.";
                return false;
            }
        }

        foreach (LocalPlaceRuntime entryPoint in entryPoints)
        {
            if (ContainsPlace(entryPoint) == false)
            {
                diagnostic = "Local topology contains an entry point that is outside the topology.";
                return false;
            }
        }

        return true;
    }

    private bool WouldCreateCycle(LocalPlaceRuntime place, LocalPlaceRuntime parent)
    {
        HashSet<LocalPlaceRuntime> visited = new HashSet<LocalPlaceRuntime>();
        LocalPlaceRuntime current = parent;
        while (current != null && visited.Add(current) == true)
        {
            if (current == place)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private LocalPlaceRuntime SelectNextUnsettledPlace(
        Dictionary<string, float> distances,
        HashSet<string> settledPlaceRuntimeIds)
    {
        LocalPlaceRuntime selected = null;
        float selectedDistance = float.PositiveInfinity;

        // The places list is insertion ordered, so equal-cost frontier choices are stable.
        foreach (LocalPlaceRuntime place in places)
        {
            if (place == null
                || settledPlaceRuntimeIds.Contains(place.RuntimeId) == true
                || distances.TryGetValue(place.RuntimeId, out float distance) == false
                || distance >= selectedDistance)
            {
                continue;
            }

            selected = place;
            selectedDistance = distance;
        }

        return selected;
    }

    private bool IsOwnerReferenceConsistent(out string diagnostic)
    {
        diagnostic = null;

        if (owner.OwnerKind == LocalTopologyOwnerKind.City)
        {
            if (identityRegistry.TryGetCityWithoutLogging(owner.OwnerRuntimeId, out CityRuntime city) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' is not a registered City.";
                return false;
            }

            if (city.Location == null
                || string.Equals(city.Location.RuntimeId, owner.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' does not match its City macro location.";
                return false;
            }

            return true;
        }

        if (owner.OwnerKind == LocalTopologyOwnerKind.ExplorableSite)
        {
            if (identityRegistry.TryGetExplorableSiteWithoutLogging(owner.OwnerRuntimeId, out ExplorableSiteRuntime site) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' is not a registered ExplorableSite.";
                return false;
            }

            if (site.Location == null
                || string.Equals(site.Location.RuntimeId, owner.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' does not match its ExplorableSite macro location.";
                return false;
            }

            return true;
        }

        diagnostic = "Local topology owner kind is invalid.";
        return false;
    }
}

public sealed class LocalTopologyStore
{
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly List<LocalTopologyRuntime> topologies = new List<LocalTopologyRuntime>();
    private readonly Dictionary<string, LocalTopologyRuntime> topologiesByOwnerRuntimeId =
        new Dictionary<string, LocalTopologyRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<LocalTopologyRuntime> readOnlyTopologies;

    public IReadOnlyList<LocalTopologyRuntime> Topologies => readOnlyTopologies;

    public LocalTopologyStore(RuntimeIdentityRegistry identityRegistry)
    {
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        readOnlyTopologies = topologies.AsReadOnly();
    }

    public bool Add(LocalTopologyRuntime topology)
    {
        return TryAddTopology(topology, out _);
    }

    public bool TryAddTopology(LocalTopologyRuntime topology, out string diagnostic)
    {
        diagnostic = null;

        if (topology == null)
        {
            diagnostic = "Local topology is null.";
            return false;
        }

        if (topology.IdentityRegistry != identityRegistry)
        {
            diagnostic = "Local topology must use this store's RuntimeIdentityRegistry.";
            return false;
        }

        if (topology.TryValidate(out diagnostic) == false)
        {
            return false;
        }

        LocalTopologyOwnerReference owner = topology.Owner;
        if (topologiesByOwnerRuntimeId.ContainsKey(owner.OwnerRuntimeId) == true)
        {
            diagnostic = $"Owner '{owner.OwnerRuntimeId}' already has an active local topology.";
            return false;
        }

        if (IsOwnerReferenceConsistent(owner, out diagnostic) == false)
        {
            return false;
        }

        topologiesByOwnerRuntimeId.Add(owner.OwnerRuntimeId, topology);
        topologies.Add(topology);
        return true;
    }

    public bool HasTopologyForOwner(string ownerRuntimeId)
    {
        return string.IsNullOrWhiteSpace(ownerRuntimeId) == false
            && topologiesByOwnerRuntimeId.ContainsKey(ownerRuntimeId);
    }

    public bool TryGetTopologyForOwner(string ownerRuntimeId, out LocalTopologyRuntime topology)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == false
            && topologiesByOwnerRuntimeId.TryGetValue(ownerRuntimeId, out topology) == true)
        {
            return true;
        }

        topology = null;
        return false;
    }

    public bool TryGetLocalPlace(string runtimeId, out LocalPlaceRuntime place)
    {
        return identityRegistry.TryGetLocalPlace(runtimeId, out place);
    }

    public bool TryGetLocalConnection(string runtimeId, out LocalTopologyConnectionRuntime connection)
    {
        return identityRegistry.TryGetLocalConnection(runtimeId, out connection);
    }

    private bool IsOwnerReferenceConsistent(
        LocalTopologyOwnerReference owner,
        out string diagnostic)
    {
        diagnostic = null;

        if (owner.OwnerKind == LocalTopologyOwnerKind.City)
        {
            if (identityRegistry.TryGetCityWithoutLogging(owner.OwnerRuntimeId, out CityRuntime city) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' is not a registered City.";
                return false;
            }

            if (city.Location == null
                || string.Equals(city.Location.RuntimeId, owner.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' does not match its City macro location.";
                return false;
            }

            return true;
        }

        if (owner.OwnerKind == LocalTopologyOwnerKind.ExplorableSite)
        {
            if (identityRegistry.TryGetExplorableSiteWithoutLogging(owner.OwnerRuntimeId, out ExplorableSiteRuntime site) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' is not a registered ExplorableSite.";
                return false;
            }

            if (site.Location == null
                || string.Equals(site.Location.RuntimeId, owner.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = $"Local topology owner '{owner.OwnerRuntimeId}' does not match its ExplorableSite macro location.";
                return false;
            }

            return true;
        }

        diagnostic = "Local topology owner kind is invalid.";
        return false;
    }
}
