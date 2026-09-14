using System;
using System.Collections.Generic;

public sealed class ExplorableSiteStore
{
    private readonly List<ExplorableSiteRuntime> sites = new List<ExplorableSiteRuntime>();
    private readonly Dictionary<string, ExplorableSiteRuntime> sitesByRuntimeId =
        new Dictionary<string, ExplorableSiteRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<ExplorableSiteRuntime> readOnlySites;

    public IReadOnlyList<ExplorableSiteRuntime> Sites => readOnlySites;

    public ExplorableSiteStore()
    {
        readOnlySites = sites.AsReadOnly();
    }

    public bool Add(ExplorableSiteRuntime site)
    {
        if (site == null
            || string.IsNullOrWhiteSpace(site.RuntimeId) == true
            || sitesByRuntimeId.ContainsKey(site.RuntimeId) == true)
        {
            return false;
        }

        sitesByRuntimeId.Add(site.RuntimeId, site);
        sites.Add(site);
        return true;
    }

    public ExplorableSiteRuntime GetByRuntimeId(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == false
            && sitesByRuntimeId.TryGetValue(runtimeId, out ExplorableSiteRuntime site) == true
            ? site
            : null;
    }

    public bool TryGetByRuntimeId(string runtimeId, out ExplorableSiteRuntime site)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false
            && sitesByRuntimeId.TryGetValue(runtimeId, out site) == true)
        {
            return true;
        }

        site = null;
        return false;
    }

    public IReadOnlyList<ExplorableSiteRuntime> GetForLocation(SpatialLocationRuntime location)
    {
        return GetForLocationRuntimeId(location?.RuntimeId);
    }

    public IReadOnlyList<ExplorableSiteRuntime> GetForLocationRuntimeId(string locationRuntimeId)
    {
        List<ExplorableSiteRuntime> result = new List<ExplorableSiteRuntime>();

        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true)
        {
            return result.AsReadOnly();
        }

        foreach (ExplorableSiteRuntime site in sites)
        {
            if (site?.Location != null
                && string.Equals(site.Location.RuntimeId, locationRuntimeId, StringComparison.Ordinal) == true)
            {
                result.Add(site);
            }
        }

        return result.AsReadOnly();
    }
}
