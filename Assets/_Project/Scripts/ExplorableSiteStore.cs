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

    /// <summary>
    /// Copies this owner's ordered site identities, definition identities,
    /// legacy location references, and exact local revision. The surrounding
    /// P12 composition supplies capture-boundary authority and graph checks.
    /// </summary>
    internal ExplorableSiteOwnerSnapshot CaptureOwnerSnapshot()
    {
        List<ExplorableSiteOwnerSnapshotRecord> siteRecords = new List<ExplorableSiteOwnerSnapshotRecord>(sites.Count);
        foreach (ExplorableSiteRuntime site in sites)
        {
            siteRecords.Add(new ExplorableSiteOwnerSnapshotRecord(
                site.RuntimeId,
                site.SiteInstanceId,
                site.DefinitionId,
                site.Location.RuntimeId));
        }

        return new ExplorableSiteOwnerSnapshot(
            ExplorableSiteOwnerSnapshot.CurrentSchemaVersion,
            revision,
            siteRecords);
    }

    /// <summary>
    /// Rebuilds an unpublished owner from exact site facts using compatible
    /// current definitions and existing legacy locations. Only this store's
    /// ordered rows and RuntimeId index are reconstructed here.
    /// </summary>
    internal static bool TryCreateFromOwnerSnapshot(
        ExplorableSiteOwnerSnapshot snapshot,
        IEnumerable<ExplorableSiteData> compatibleDefinitions,
        IEnumerable<SpatialLocationRuntime> legacyLocations,
        out ExplorableSiteStore stagedStore,
        out ExplorableSiteSnapshotFailure failure)
    {
        stagedStore = null;
        if (snapshot == null || compatibleDefinitions == null || legacyLocations == null)
        {
            failure = ExplorableSiteSnapshotFailure.Create(
                ExplorableSiteSnapshotFailureCode.InvalidSnapshot,
                "A site snapshot, compatible definition set, and legacy location set are required.");
            return false;
        }

        if (snapshot.SchemaVersion != ExplorableSiteOwnerSnapshot.CurrentSchemaVersion)
        {
            failure = ExplorableSiteSnapshotFailure.Create(
                ExplorableSiteSnapshotFailureCode.UnsupportedSnapshotSchema,
                "The ExplorableSite owner snapshot schema is not supported.");
            return false;
        }

        if (snapshot.Sites == null || snapshot.Revision < 0 || snapshot.Revision != snapshot.Sites.Count)
        {
            failure = ExplorableSiteSnapshotFailure.Create(
                ExplorableSiteSnapshotFailureCode.InvalidSnapshot,
                "The ExplorableSite owner snapshot is missing its ordered rows or has an impossible revision.");
            return false;
        }

        Dictionary<string, ExplorableSiteData> definitionsById =
            new Dictionary<string, ExplorableSiteData>(StringComparer.Ordinal);
        foreach (ExplorableSiteData definition in compatibleDefinitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.IncompatibleDefinition,
                    "The compatible definition set contains a missing definition identity.");
                return false;
            }

            if (definitionsById.ContainsKey(definition.DefinitionId))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.DuplicateDefinitionIdentity,
                    "The compatible definition set contains duplicate definition identities.");
                return false;
            }

            definitionsById.Add(definition.DefinitionId, definition);
        }

        Dictionary<string, SpatialLocationRuntime> locationsById =
            new Dictionary<string, SpatialLocationRuntime>(StringComparer.Ordinal);
        foreach (SpatialLocationRuntime location in legacyLocations)
        {
            if (location == null || string.IsNullOrWhiteSpace(location.RuntimeId))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.InvalidLegacyLocation,
                    "The legacy location set contains a missing runtime identity.");
                return false;
            }

            if (locationsById.ContainsKey(location.RuntimeId))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.DuplicateLegacyLocationIdentity,
                    "The legacy location set contains duplicate runtime identities.");
                return false;
            }

            locationsById.Add(location.RuntimeId, location);
        }

        HashSet<string> siteRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> siteInstanceIds = new HashSet<string>(StringComparer.Ordinal);
        List<ExplorableSiteRuntime> resolvedSites = new List<ExplorableSiteRuntime>(snapshot.Sites.Count);
        foreach (ExplorableSiteOwnerSnapshotRecord siteRecord in snapshot.Sites)
        {
            if (siteRecord == null
                || string.IsNullOrWhiteSpace(siteRecord.RuntimeId)
                || string.IsNullOrWhiteSpace(siteRecord.SiteInstanceId)
                || string.IsNullOrWhiteSpace(siteRecord.DefinitionId)
                || string.IsNullOrWhiteSpace(siteRecord.LocationRuntimeId))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.InvalidSiteIdentity,
                    "The site snapshot contains a null row or empty identity/reference.");
                return false;
            }

            if (!siteRuntimeIds.Add(siteRecord.RuntimeId) || !siteInstanceIds.Add(siteRecord.SiteInstanceId))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.DuplicateSiteIdentity,
                    "The site snapshot contains duplicate runtime or site-instance identities.");
                return false;
            }


            if (!definitionsById.TryGetValue(siteRecord.DefinitionId, out ExplorableSiteData definition)
                || !string.Equals(definition.DefinitionId, siteRecord.DefinitionId, StringComparison.Ordinal))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.IncompatibleDefinition,
                    "A site definition identity cannot be resolved by the compatible definition set.");
                return false;
            }

            if (!locationsById.TryGetValue(siteRecord.LocationRuntimeId, out SpatialLocationRuntime location))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.MissingLegacyLocation,
                    "A site references a legacy location that is absent from the staged spatial owner.");
                return false;
            }

            if (string.Equals(siteRecord.RuntimeId, siteRecord.LocationRuntimeId, StringComparison.Ordinal))
            {
                failure = ExplorableSiteSnapshotFailure.Create(
                    ExplorableSiteSnapshotFailureCode.RuntimeIdentityCollision,
                    "A site runtime identity must differ from its legacy location identity.");
                return false;
            }

            resolvedSites.Add(new ExplorableSiteRuntime(
                siteRecord.RuntimeId,
                definition,
                location,
                siteRecord.SiteInstanceId));
        }

        ExplorableSiteStore staged = new ExplorableSiteStore();
        foreach (ExplorableSiteRuntime site in resolvedSites)
        {
            staged.sites.Add(site);
            staged.sitesByRuntimeId.Add(site.RuntimeId, site);
        }

        staged.revision = snapshot.Revision;
        stagedStore = staged;
        failure = ExplorableSiteSnapshotFailure.None;
        return true;
    }
    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
