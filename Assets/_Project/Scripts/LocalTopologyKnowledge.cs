using System;
using System.Collections.Generic;

public enum LocalTopologyKnowledgeSource
{
    InitialScenarioKnowledge,
    DirectObservation,
    SharedByNpc
}

[Serializable]
public sealed class LocalPlaceKnowledgeObservation
{
    private readonly string topologyOwnerRuntimeId;
    private readonly string localPlaceRuntimeId;
    private readonly string parentLocalPlaceRuntimeId;
    private readonly string placeTypeDefinitionId;
    private readonly string displayName;
    private readonly bool isEntryPoint;
    private readonly long observedDay;
    private readonly long receivedDay;
    private readonly LocalTopologyKnowledgeSource source;
    private readonly string sourceRuntimeId;

    public string TopologyOwnerRuntimeId => topologyOwnerRuntimeId;
    public string LocalPlaceRuntimeId => localPlaceRuntimeId;
    public string ParentLocalPlaceRuntimeId => parentLocalPlaceRuntimeId;
    public string PlaceTypeDefinitionId => placeTypeDefinitionId;
    public string DisplayName => displayName;
    public bool IsEntryPoint => isEntryPoint;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public LocalTopologyKnowledgeSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;

    public LocalPlaceKnowledgeObservation(
        string topologyOwnerRuntimeId,
        string localPlaceRuntimeId,
        string parentLocalPlaceRuntimeId,
        string placeTypeDefinitionId,
        string displayName,
        bool isEntryPoint,
        long observedDay,
        long receivedDay,
        LocalTopologyKnowledgeSource source,
        string sourceRuntimeId = null)
    {
        RequireId(topologyOwnerRuntimeId, nameof(topologyOwnerRuntimeId));
        RequireId(localPlaceRuntimeId, nameof(localPlaceRuntimeId));

        if (string.IsNullOrWhiteSpace(parentLocalPlaceRuntimeId) == false
            && string.Equals(parentLocalPlaceRuntimeId, localPlaceRuntimeId, StringComparison.Ordinal) == true)
        {
            throw new ArgumentException("A LocalPlace knowledge snapshot cannot be its own parent.", nameof(parentLocalPlaceRuntimeId));
        }

        if (observedDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(observedDay));
        }

        if (receivedDay < observedDay)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDay));
        }

        if (Enum.IsDefined(typeof(LocalTopologyKnowledgeSource), source) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        if (source == LocalTopologyKnowledgeSource.SharedByNpc
            && string.IsNullOrWhiteSpace(sourceRuntimeId) == true)
        {
            throw new ArgumentException("Shared local topology knowledge requires a source NPC RuntimeId.", nameof(sourceRuntimeId));
        }

        this.topologyOwnerRuntimeId = topologyOwnerRuntimeId;
        this.localPlaceRuntimeId = localPlaceRuntimeId;
        this.parentLocalPlaceRuntimeId = string.IsNullOrWhiteSpace(parentLocalPlaceRuntimeId) == true
            ? null
            : parentLocalPlaceRuntimeId;
        this.placeTypeDefinitionId = string.IsNullOrWhiteSpace(placeTypeDefinitionId) == true
            ? null
            : placeTypeDefinitionId;
        this.displayName = displayName ?? string.Empty;
        this.isEntryPoint = isEntryPoint;
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.source = source;
        this.sourceRuntimeId = string.IsNullOrWhiteSpace(sourceRuntimeId) == true ? null : sourceRuntimeId;
    }

    private static void RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Local topology knowledge requires a non-empty RuntimeId.", parameterName);
        }
    }
}

[Serializable]
public sealed class LocalConnectionKnowledgeObservation
{
    private readonly string topologyOwnerRuntimeId;
    private readonly string localConnectionRuntimeId;
    private readonly string originLocalPlaceRuntimeId;
    private readonly string destinationLocalPlaceRuntimeId;
    private readonly float traversalCost;
    private readonly string connectionTypeDefinitionId;
    private readonly long observedDay;
    private readonly long receivedDay;
    private readonly LocalTopologyKnowledgeSource source;
    private readonly string sourceRuntimeId;

    public string TopologyOwnerRuntimeId => topologyOwnerRuntimeId;
    public string LocalConnectionRuntimeId => localConnectionRuntimeId;
    public string OriginLocalPlaceRuntimeId => originLocalPlaceRuntimeId;
    public string DestinationLocalPlaceRuntimeId => destinationLocalPlaceRuntimeId;
    public float TraversalCost => traversalCost;
    public string ConnectionTypeDefinitionId => connectionTypeDefinitionId;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public LocalTopologyKnowledgeSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;

    public LocalConnectionKnowledgeObservation(
        string topologyOwnerRuntimeId,
        string localConnectionRuntimeId,
        string originLocalPlaceRuntimeId,
        string destinationLocalPlaceRuntimeId,
        float traversalCost,
        string connectionTypeDefinitionId,
        long observedDay,
        long receivedDay,
        LocalTopologyKnowledgeSource source,
        string sourceRuntimeId = null)
    {
        RequireId(topologyOwnerRuntimeId, nameof(topologyOwnerRuntimeId));
        RequireId(localConnectionRuntimeId, nameof(localConnectionRuntimeId));
        RequireId(originLocalPlaceRuntimeId, nameof(originLocalPlaceRuntimeId));
        RequireId(destinationLocalPlaceRuntimeId, nameof(destinationLocalPlaceRuntimeId));

        if (string.Equals(originLocalPlaceRuntimeId, destinationLocalPlaceRuntimeId, StringComparison.Ordinal) == true)
        {
            throw new ArgumentException("A LocalConnection knowledge snapshot cannot connect a place to itself.", nameof(destinationLocalPlaceRuntimeId));
        }

        if (LocalTopologyConnectionRuntime.IsValidTraversalCost(traversalCost) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(traversalCost));
        }

        if (observedDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(observedDay));
        }

        if (receivedDay < observedDay)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDay));
        }

        if (Enum.IsDefined(typeof(LocalTopologyKnowledgeSource), source) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        if (source == LocalTopologyKnowledgeSource.SharedByNpc
            && string.IsNullOrWhiteSpace(sourceRuntimeId) == true)
        {
            throw new ArgumentException("Shared local topology knowledge requires a source NPC RuntimeId.", nameof(sourceRuntimeId));
        }

        this.topologyOwnerRuntimeId = topologyOwnerRuntimeId;
        this.localConnectionRuntimeId = localConnectionRuntimeId;
        this.originLocalPlaceRuntimeId = originLocalPlaceRuntimeId;
        this.destinationLocalPlaceRuntimeId = destinationLocalPlaceRuntimeId;
        this.traversalCost = traversalCost;
        this.connectionTypeDefinitionId = string.IsNullOrWhiteSpace(connectionTypeDefinitionId) == true
            ? null
            : connectionTypeDefinitionId;
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.source = source;
        this.sourceRuntimeId = string.IsNullOrWhiteSpace(sourceRuntimeId) == true ? null : sourceRuntimeId;
    }

    private static void RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Local topology knowledge requires a non-empty RuntimeId.", parameterName);
        }
    }
}

[Serializable]
public sealed class LocalTopologyKnowledgeRuntime
{
    private readonly string ownerRuntimeId;
    private readonly List<LocalPlaceKnowledgeObservation> placeObservations =
        new List<LocalPlaceKnowledgeObservation>();
    private readonly List<LocalConnectionKnowledgeObservation> connectionObservations =
        new List<LocalConnectionKnowledgeObservation>();
    private readonly IReadOnlyList<LocalPlaceKnowledgeObservation> readOnlyPlaceObservations;
    private readonly IReadOnlyList<LocalConnectionKnowledgeObservation> readOnlyConnectionObservations;

    public string OwnerRuntimeId => ownerRuntimeId;
    public IReadOnlyList<LocalPlaceKnowledgeObservation> PlaceObservations => readOnlyPlaceObservations;
    public IReadOnlyList<LocalConnectionKnowledgeObservation> ConnectionObservations => readOnlyConnectionObservations;

    public LocalTopologyKnowledgeRuntime(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Local topology knowledge requires an NPC owner RuntimeId.", nameof(ownerRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
        readOnlyPlaceObservations = placeObservations.AsReadOnly();
        readOnlyConnectionObservations = connectionObservations.AsReadOnly();
    }

    public bool KnowsLocalPlace(string topologyOwnerRuntimeId, string localPlaceRuntimeId)
    {
        return TryGetPlaceObservation(topologyOwnerRuntimeId, localPlaceRuntimeId, out _);
    }

    public bool KnowsConnection(string topologyOwnerRuntimeId, string localConnectionRuntimeId)
    {
        return TryGetConnectionObservation(topologyOwnerRuntimeId, localConnectionRuntimeId, out _);
    }

    public bool TryGetPlaceObservation(
        string topologyOwnerRuntimeId,
        string localPlaceRuntimeId,
        out LocalPlaceKnowledgeObservation observation)
    {
        int index = FindPlaceObservationIndex(topologyOwnerRuntimeId, localPlaceRuntimeId);
        if (index >= 0)
        {
            observation = placeObservations[index];
            return true;
        }

        observation = null;
        return false;
    }

    public bool TryGetConnectionObservation(
        string topologyOwnerRuntimeId,
        string localConnectionRuntimeId,
        out LocalConnectionKnowledgeObservation observation)
    {
        int index = FindConnectionObservationIndex(topologyOwnerRuntimeId, localConnectionRuntimeId);
        if (index >= 0)
        {
            observation = connectionObservations[index];
            return true;
        }

        observation = null;
        return false;
    }

    public bool RecordPlaceObservation(LocalPlaceKnowledgeObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindPlaceObservationIndex(
            observation.TopologyOwnerRuntimeId,
            observation.LocalPlaceRuntimeId);

        if (existingIndex < 0)
        {
            placeObservations.Add(observation);
            return true;
        }

        if (ShouldReplace(placeObservations[existingIndex], observation) == false)
        {
            return false;
        }

        placeObservations[existingIndex] = observation;
        return true;
    }

    public string NpcRuntimeId => ownerRuntimeId;

    public bool RecordConnectionObservation(LocalConnectionKnowledgeObservation observation)
    {
        if (observation == null
            || KnowsLocalPlace(observation.TopologyOwnerRuntimeId, observation.OriginLocalPlaceRuntimeId) == false
            || KnowsLocalPlace(observation.TopologyOwnerRuntimeId, observation.DestinationLocalPlaceRuntimeId) == false)
        {
            return false;
        }

        int existingIndex = FindConnectionObservationIndex(
            observation.TopologyOwnerRuntimeId,
            observation.LocalConnectionRuntimeId);

        if (existingIndex < 0)
        {
            connectionObservations.Add(observation);
            return true;
        }

        if (ShouldReplace(connectionObservations[existingIndex], observation) == false)
        {
            return false;
        }

        connectionObservations[existingIndex] = observation;
        return true;
    }

    public IReadOnlyList<LocalPlaceKnowledgeObservation> GetKnownChildren(
        string topologyOwnerRuntimeId,
        string parentLocalPlaceRuntimeId)
    {
        List<LocalPlaceKnowledgeObservation> children = new List<LocalPlaceKnowledgeObservation>();

        foreach (LocalPlaceKnowledgeObservation observation in placeObservations)
        {
            if (observation != null
                && string.Equals(observation.TopologyOwnerRuntimeId, topologyOwnerRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(observation.ParentLocalPlaceRuntimeId, parentLocalPlaceRuntimeId, StringComparison.Ordinal) == true)
            {
                children.Add(observation);
            }
        }

        return children.AsReadOnly();
    }

    public bool TryFindKnownPath(
        string topologyOwnerRuntimeId,
        string startLocalPlaceRuntimeId,
        string targetLocalPlaceRuntimeId,
        out LocalTopologyPath path)
    {
        path = null;

        if (TryGetPlaceObservation(topologyOwnerRuntimeId, startLocalPlaceRuntimeId, out _) == false
            || TryGetPlaceObservation(topologyOwnerRuntimeId, targetLocalPlaceRuntimeId, out _) == false)
        {
            return false;
        }

        if (string.Equals(startLocalPlaceRuntimeId, targetLocalPlaceRuntimeId, StringComparison.Ordinal) == true)
        {
            path = new LocalTopologyPath(
                startLocalPlaceRuntimeId,
                targetLocalPlaceRuntimeId,
                new[] { startLocalPlaceRuntimeId },
                Array.Empty<string>(),
                0f);
            return true;
        }

        Dictionary<string, float> distances = new Dictionary<string, float>(StringComparer.Ordinal);
        Dictionary<string, string> previousPlaceRuntimeIds = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> previousConnectionRuntimeIds = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> settledPlaceRuntimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (LocalPlaceKnowledgeObservation observation in placeObservations)
        {
            if (observation != null
                && string.Equals(observation.TopologyOwnerRuntimeId, topologyOwnerRuntimeId, StringComparison.Ordinal) == true
                && distances.ContainsKey(observation.LocalPlaceRuntimeId) == false)
            {
                distances.Add(observation.LocalPlaceRuntimeId, float.PositiveInfinity);
            }
        }

        distances[startLocalPlaceRuntimeId] = 0f;

        while (true)
        {
            string currentRuntimeId = SelectNextUnsettledPlace(
                topologyOwnerRuntimeId,
                distances,
                settledPlaceRuntimeIds);
            if (currentRuntimeId == null)
            {
                break;
            }

            settledPlaceRuntimeIds.Add(currentRuntimeId);
            if (string.Equals(currentRuntimeId, targetLocalPlaceRuntimeId, StringComparison.Ordinal) == true)
            {
                break;
            }

            foreach (LocalConnectionKnowledgeObservation connection in connectionObservations)
            {
                if (connection == null
                    || string.Equals(connection.TopologyOwnerRuntimeId, topologyOwnerRuntimeId, StringComparison.Ordinal) == false
                    || string.Equals(connection.OriginLocalPlaceRuntimeId, currentRuntimeId, StringComparison.Ordinal) == false
                    || settledPlaceRuntimeIds.Contains(connection.DestinationLocalPlaceRuntimeId) == true)
                {
                    continue;
                }

                float candidateDistance = distances[currentRuntimeId] + connection.TraversalCost;
                if (float.IsInfinity(candidateDistance) == true
                    || candidateDistance >= distances[connection.DestinationLocalPlaceRuntimeId])
                {
                    continue;
                }

                distances[connection.DestinationLocalPlaceRuntimeId] = candidateDistance;
                previousPlaceRuntimeIds[connection.DestinationLocalPlaceRuntimeId] = currentRuntimeId;
                previousConnectionRuntimeIds[connection.DestinationLocalPlaceRuntimeId] = connection.LocalConnectionRuntimeId;
            }
        }

        if (distances.TryGetValue(targetLocalPlaceRuntimeId, out float targetDistance) == false
            || float.IsInfinity(targetDistance) == true)
        {
            return false;
        }

        List<string> reversePlaceRuntimeIds = new List<string>();
        List<string> reverseConnectionRuntimeIds = new List<string>();
        string pathCurrentRuntimeId = targetLocalPlaceRuntimeId;
        reversePlaceRuntimeIds.Add(pathCurrentRuntimeId);

        while (string.Equals(pathCurrentRuntimeId, startLocalPlaceRuntimeId, StringComparison.Ordinal) == false)
        {
            if (previousPlaceRuntimeIds.TryGetValue(pathCurrentRuntimeId, out string previousPlaceRuntimeId) == false
                || previousConnectionRuntimeIds.TryGetValue(pathCurrentRuntimeId, out string previousConnectionRuntimeId) == false)
            {
                return false;
            }

            reverseConnectionRuntimeIds.Add(previousConnectionRuntimeId);
            pathCurrentRuntimeId = previousPlaceRuntimeId;
            reversePlaceRuntimeIds.Add(pathCurrentRuntimeId);
        }

        reversePlaceRuntimeIds.Reverse();
        reverseConnectionRuntimeIds.Reverse();
        path = new LocalTopologyPath(
            startLocalPlaceRuntimeId,
            targetLocalPlaceRuntimeId,
            reversePlaceRuntimeIds,
            reverseConnectionRuntimeIds,
            targetDistance);
        return true;
    }

    private int FindPlaceObservationIndex(string topologyOwnerRuntimeId, string localPlaceRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(topologyOwnerRuntimeId) == true
            || string.IsNullOrWhiteSpace(localPlaceRuntimeId) == true)
        {
            return -1;
        }

        for (int i = 0; i < placeObservations.Count; i++)
        {
            LocalPlaceKnowledgeObservation candidate = placeObservations[i];
            if (candidate != null
                && string.Equals(candidate.TopologyOwnerRuntimeId, topologyOwnerRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(candidate.LocalPlaceRuntimeId, localPlaceRuntimeId, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindConnectionObservationIndex(string topologyOwnerRuntimeId, string localConnectionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(topologyOwnerRuntimeId) == true
            || string.IsNullOrWhiteSpace(localConnectionRuntimeId) == true)
        {
            return -1;
        }

        for (int i = 0; i < connectionObservations.Count; i++)
        {
            LocalConnectionKnowledgeObservation candidate = connectionObservations[i];
            if (candidate != null
                && string.Equals(candidate.TopologyOwnerRuntimeId, topologyOwnerRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(candidate.LocalConnectionRuntimeId, localConnectionRuntimeId, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private string SelectNextUnsettledPlace(
        string topologyOwnerRuntimeId,
        Dictionary<string, float> distances,
        HashSet<string> settledPlaceRuntimeIds)
    {
        string selectedRuntimeId = null;
        float selectedDistance = float.PositiveInfinity;

        // Observation insertion order is the stable tie-break for equal known path costs.
        foreach (LocalPlaceKnowledgeObservation observation in placeObservations)
        {
            if (observation == null
                || string.Equals(observation.TopologyOwnerRuntimeId, topologyOwnerRuntimeId, StringComparison.Ordinal) == false
                || settledPlaceRuntimeIds.Contains(observation.LocalPlaceRuntimeId) == true
                || distances.TryGetValue(observation.LocalPlaceRuntimeId, out float distance) == false
                || distance >= selectedDistance)
            {
                continue;
            }

            selectedRuntimeId = observation.LocalPlaceRuntimeId;
            selectedDistance = distance;
        }

        return selectedRuntimeId;
    }

    private static bool ShouldReplace(
        LocalPlaceKnowledgeObservation existing,
        LocalPlaceKnowledgeObservation incoming)
    {
        if (existing == null)
        {
            return true;
        }

        return ShouldReplace(
            existing.ObservedDay,
            existing.ReceivedDay,
            existing.Source,
            incoming.ObservedDay,
            incoming.ReceivedDay,
            incoming.Source);
    }

    private static bool ShouldReplace(
        LocalConnectionKnowledgeObservation existing,
        LocalConnectionKnowledgeObservation incoming)
    {
        if (existing == null)
        {
            return true;
        }

        return ShouldReplace(
            existing.ObservedDay,
            existing.ReceivedDay,
            existing.Source,
            incoming.ObservedDay,
            incoming.ReceivedDay,
            incoming.Source);
    }

    private static bool ShouldReplace(
        long existingObservedDay,
        long existingReceivedDay,
        LocalTopologyKnowledgeSource existingSource,
        long incomingObservedDay,
        long incomingReceivedDay,
        LocalTopologyKnowledgeSource incomingSource)
    {
        if (incomingObservedDay != existingObservedDay)
        {
            return incomingObservedDay > existingObservedDay;
        }

        int incomingPriority = GetSourcePriority(incomingSource);
        int existingPriority = GetSourcePriority(existingSource);
        if (incomingPriority != existingPriority)
        {
            return incomingPriority > existingPriority;
        }

        return incomingReceivedDay > existingReceivedDay;
    }

    private static int GetSourcePriority(LocalTopologyKnowledgeSource source)
    {
        if (source == LocalTopologyKnowledgeSource.DirectObservation)
        {
            return 3;
        }

        if (source == LocalTopologyKnowledgeSource.SharedByNpc)
        {
            return 2;
        }

        return 1;
    }
}

public sealed class LocalTopologyKnowledgeSystem
{
    public bool RecordInitialScenarioKnowledge(
        NpcRuntime npcRuntime,
        LocalTopologyRuntime topology,
        LocalPlaceRuntime place,
        long observedDay = 0L)
    {
        return RecordPlaceFromTruth(
            npcRuntime,
            topology,
            place,
            observedDay,
            observedDay,
            LocalTopologyKnowledgeSource.InitialScenarioKnowledge,
            null);
    }

    public bool RecordDirectObservation(
        NpcRuntime npcRuntime,
        LocalTopologyRuntime topology,
        LocalPlaceRuntime place,
        long observedDay)
    {
        return RecordPlaceFromTruth(
            npcRuntime,
            topology,
            place,
            observedDay,
            observedDay,
            LocalTopologyKnowledgeSource.DirectObservation,
            null);
    }

    public bool RecordInitialScenarioKnowledge(
        NpcRuntime npcRuntime,
        LocalTopologyRuntime topology,
        LocalTopologyConnectionRuntime connection,
        long observedDay = 0L)
    {
        return RecordConnectionFromTruth(
            npcRuntime,
            topology,
            connection,
            observedDay,
            observedDay,
            LocalTopologyKnowledgeSource.InitialScenarioKnowledge,
            null);
    }

    public bool RecordDirectObservation(
        NpcRuntime npcRuntime,
        LocalTopologyRuntime topology,
        LocalTopologyConnectionRuntime connection,
        long observedDay)
    {
        return RecordConnectionFromTruth(
            npcRuntime,
            topology,
            connection,
            observedDay,
            observedDay,
            LocalTopologyKnowledgeSource.DirectObservation,
            null);
    }

    public bool RecordSharedKnowledge(
        NpcRuntime npcRuntime,
        LocalPlaceKnowledgeObservation observation)
    {
        return npcRuntime != null
            && observation != null
            && observation.Source == LocalTopologyKnowledgeSource.SharedByNpc
            && npcRuntime.LocalTopologyKnowledge.RecordPlaceObservation(observation);
    }

    public bool RecordSharedKnowledge(
        NpcRuntime npcRuntime,
        LocalConnectionKnowledgeObservation observation)
    {
        return npcRuntime != null
            && observation != null
            && observation.Source == LocalTopologyKnowledgeSource.SharedByNpc
            && npcRuntime.LocalTopologyKnowledge.RecordConnectionObservation(observation);
    }

    private static bool RecordPlaceFromTruth(
        NpcRuntime npcRuntime,
        LocalTopologyRuntime topology,
        LocalPlaceRuntime place,
        long observedDay,
        long receivedDay,
        LocalTopologyKnowledgeSource source,
        string sourceRuntimeId)
    {
        if (npcRuntime == null
            || topology == null
            || place == null
            || topology.ContainsPlace(place) == false)
        {
            return false;
        }

        LocalPlaceKnowledgeObservation observation = CreatePlaceObservation(
            topology,
            place,
            observedDay,
            receivedDay,
            source,
            sourceRuntimeId);
        return npcRuntime.LocalTopologyKnowledge.RecordPlaceObservation(observation);
    }

    private static bool RecordConnectionFromTruth(
        NpcRuntime npcRuntime,
        LocalTopologyRuntime topology,
        LocalTopologyConnectionRuntime connection,
        long observedDay,
        long receivedDay,
        LocalTopologyKnowledgeSource source,
        string sourceRuntimeId)
    {
        if (npcRuntime == null
            || topology == null
            || connection == null
            || topology.ContainsConnection(connection) == false)
        {
            return false;
        }

        // Endpoint snapshots are recorded explicitly as part of observing the connection.
        npcRuntime.LocalTopologyKnowledge.RecordPlaceObservation(CreatePlaceObservation(
            topology,
            connection.Origin,
            observedDay,
            receivedDay,
            source,
            sourceRuntimeId));
        npcRuntime.LocalTopologyKnowledge.RecordPlaceObservation(CreatePlaceObservation(
            topology,
            connection.Destination,
            observedDay,
            receivedDay,
            source,
            sourceRuntimeId));

        return npcRuntime.LocalTopologyKnowledge.RecordConnectionObservation(
            new LocalConnectionKnowledgeObservation(
                topology.Owner.OwnerRuntimeId,
                connection.RuntimeId,
                connection.Origin.RuntimeId,
                connection.Destination.RuntimeId,
                connection.TraversalCost,
                connection.TypeDefinitionId,
                observedDay,
                receivedDay,
                source,
                sourceRuntimeId));
    }

    private static LocalPlaceKnowledgeObservation CreatePlaceObservation(
        LocalTopologyRuntime topology,
        LocalPlaceRuntime place,
        long observedDay,
        long receivedDay,
        LocalTopologyKnowledgeSource source,
        string sourceRuntimeId)
    {
        return new LocalPlaceKnowledgeObservation(
            topology.Owner.OwnerRuntimeId,
            place.RuntimeId,
            place.Parent?.RuntimeId,
            place.TypeDefinitionId,
            place.DisplayName,
            topology.IsEntryPoint(place),
            observedDay,
            receivedDay,
            source,
            sourceRuntimeId);
    }
}
