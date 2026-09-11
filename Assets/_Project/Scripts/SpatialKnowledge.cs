using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpatialKnowledgeRuntime
{
    [SerializeField] private string ownerRuntimeId;
    [SerializeField] private List<string> knownLocationRuntimeIds = new List<string>();
    [SerializeField] private List<string> knownRouteRuntimeIds = new List<string>();

    public string OwnerRuntimeId => ownerRuntimeId;
    public IReadOnlyList<string> KnownLocationRuntimeIds => KnownLocations;
    public IReadOnlyList<string> KnownRouteRuntimeIds => KnownRoutes;

    private List<string> KnownLocations => knownLocationRuntimeIds ?? (knownLocationRuntimeIds = new List<string>());
    private List<string> KnownRoutes => knownRouteRuntimeIds ?? (knownRouteRuntimeIds = new List<string>());

    public SpatialKnowledgeRuntime(string ownerRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(ownerRuntimeId) == true)
        {
            throw new ArgumentException("Spatial knowledge requires an owner RuntimeId.", nameof(ownerRuntimeId));
        }

        this.ownerRuntimeId = ownerRuntimeId;
    }

    public bool KnowsLocation(string locationRuntimeId)
    {
        return ContainsId(KnownLocations, locationRuntimeId);
    }

    public bool KnowsRoute(string routeRuntimeId)
    {
        return ContainsId(KnownRoutes, routeRuntimeId);
    }

    public bool DiscoverLocation(string locationRuntimeId)
    {
        return DiscoverId(KnownLocations, locationRuntimeId);
    }

    public bool DiscoverRoute(string routeRuntimeId)
    {
        return DiscoverId(KnownRoutes, routeRuntimeId);
    }

    private static bool ContainsId(List<string> ids, string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            return false;
        }

        foreach (string knownId in ids)
        {
            if (string.Equals(knownId, runtimeId, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool DiscoverId(List<string> ids, string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true || ContainsId(ids, runtimeId) == true)
        {
            return false;
        }

        ids.Add(runtimeId);
        return true;
    }
}
