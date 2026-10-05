using System;
using System.Collections.Generic;

public sealed class ExplorableSiteStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly List<ExplorableSiteRuntime> sites = new List<ExplorableSiteRuntime>();
    private readonly Dictionary<string, ExplorableSiteRuntime> sitesByRuntimeId =
        new Dictionary<string, ExplorableSiteRuntime>(StringComparer.Ordinal);
    private readonly IReadOnlyList<ExplorableSiteRuntime> readOnlySites;
    private long revision;

    public IReadOnlyList<ExplorableSiteRuntime> Sites => readOnlySites;
    public int Count => sites.Count;
    public long Revision => revision;

    public ExplorableSiteStore()
    {
        readOnlySites = sites.AsReadOnly();
    }

    public bool Add(ExplorableSiteRuntime site)
    {
        return AddCore(site, null);
    }

    internal bool AddForP10Genesis(ExplorableSiteRuntime site, Action<string> completedMutation)
    {
        return AddCore(site, completedMutation);
    }

    private bool AddCore(ExplorableSiteRuntime site, Action<string> completedMutation)
    {
        if (!mutationGuardBinding.CanMutate) return false;

        if (site == null
            || string.IsNullOrWhiteSpace(site.RuntimeId) == true
            || sitesByRuntimeId.ContainsKey(site.RuntimeId) == true)
        {
            return false;
        }

        if (revision == long.MaxValue)
        {
            return false;
        }

        bool indexAdded = false;
        bool listAdded = false;
        bool revisionAdvanced = false;
        try
        {
            sitesByRuntimeId.Add(site.RuntimeId, site);
            indexAdded = true;
            completedMutation?.Invoke("ExplorableSiteStore.RuntimeIndex");

            sites.Add(site);
            listAdded = true;
            completedMutation?.Invoke("ExplorableSiteStore.OrderedSites");

            revision++;
            revisionAdvanced = true;
            completedMutation?.Invoke("ExplorableSiteStore.Revision");
            return true;
        }
        catch
        {
            if (revisionAdvanced) revision--;
            if (listAdded) sites.RemoveAt(sites.Count - 1);
            if (indexAdded) sitesByRuntimeId.Remove(site.RuntimeId);
            throw;
        }
    }

    internal void RollbackGenesisSite(ExplorableSiteRuntime site)
    {
        if (site == null || !sitesByRuntimeId.TryGetValue(site.RuntimeId, out ExplorableSiteRuntime current)
            || !ReferenceEquals(site, current) || sites.Count == 0 || revision <= 0
            || !ReferenceEquals(sites[sites.Count - 1], site))
            throw new InvalidOperationException("Cannot roll back the P10-B ExplorableSite store insertion.");
        sitesByRuntimeId.Remove(site.RuntimeId);
        sites.RemoveAt(sites.Count - 1);
        revision--;
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

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
