using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public sealed class SpatialKnowledgeRuntime
{
    [SerializeField] private string ownerRuntimeId;
    [SerializeField] private List<string> knownLocationRuntimeIds = new List<string>();
    [SerializeField] private List<string> knownRouteRuntimeIds = new List<string>();
    [SerializeField] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;
    private ReadOnlyCollection<string> knownLocationRuntimeIdsView;
    private ReadOnlyCollection<string> knownRouteRuntimeIdsView;

    public string OwnerRuntimeId => ownerRuntimeId;
    public IReadOnlyList<string> KnownLocationRuntimeIds =>
        knownLocationRuntimeIdsView ?? (knownLocationRuntimeIdsView = KnownLocations.AsReadOnly());
    public IReadOnlyList<string> KnownRouteRuntimeIds =>
        knownRouteRuntimeIdsView ?? (knownRouteRuntimeIdsView = KnownRoutes.AsReadOnly());
    public long Revision => revision;
    internal int KnownLocationCount => KnownLocations.Count;
    internal int KnownRouteCount => KnownRoutes.Count;

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

    internal bool TryPrepareDiscoverLocations(IReadOnlyList<string> locationRuntimeIds,
        out SpatialKnowledgeDiscoveryInstall prepared)
    {
        prepared = null;
        if (locationRuntimeIds == null) return false;

        List<string> nextLocations = new List<string>(KnownLocations);
        long nextRevision = revision;
        foreach (string locationRuntimeId in locationRuntimeIds)
        {
            if (string.IsNullOrWhiteSpace(locationRuntimeId)
                || ContainsId(nextLocations, locationRuntimeId)) continue;
            if (nextRevision == long.MaxValue) return false;
            nextLocations.Add(locationRuntimeId);
            nextRevision++;
        }

        // Reserve capacity before the paired actor-local commit so installation
        // only copies already prepared values into this existing list instance.
        KnownLocations.Capacity = Math.Max(KnownLocations.Capacity, nextLocations.Count);
        prepared = new SpatialKnowledgeDiscoveryInstall(this, revision, nextRevision,
            new List<string>(KnownLocations), nextLocations);
        return true;
    }

    internal bool CanInstall(SpatialKnowledgeDiscoveryInstall prepared) => prepared != null
        && prepared.Owner == this
        && prepared.ExpectedRevision == revision
        && Matches(KnownLocations, prepared.ExpectedLocationIds)
        && (prepared.NextRevision == revision || CanCommitP12Mutation());

    internal void InstallPrepared(SpatialKnowledgeDiscoveryInstall prepared)
    {
        if (prepared == null || prepared.Owner != this || prepared.ExpectedRevision != revision
            || prepared.NextRevision == revision || !CanCommitP12Mutation()) return;
        KnownLocations.Clear();
        KnownLocations.AddRange(prepared.NextLocationIds);
        revision = prepared.NextRevision;
        NotifyP12MutationCommitted();
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("SpatialKnowledgeRuntime is already bound to a P12 mutation boundary.");
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
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

    private bool DiscoverId(List<string> ids, string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true || ContainsId(ids, runtimeId) == true)
        {
            return false;
        }

        if (revision == long.MaxValue)
        {
            return false;
        }

        if (!CanCommitP12Mutation()) return false;

        ids.Add(runtimeId);
        revision++;
        NotifyP12MutationCommitted();
        return true;
    }

    private static bool Matches(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++)
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal)) return false;
        return true;
    }
}

internal sealed class SpatialKnowledgeDiscoveryInstall
{
    internal SpatialKnowledgeRuntime Owner { get; }
    internal long ExpectedRevision { get; }
    internal long NextRevision { get; }
    internal IReadOnlyList<string> ExpectedLocationIds { get; }
    internal IReadOnlyList<string> NextLocationIds { get; }

    internal SpatialKnowledgeDiscoveryInstall(SpatialKnowledgeRuntime owner,
        long expectedRevision, long nextRevision,
        IReadOnlyList<string> expectedLocationIds, IReadOnlyList<string> nextLocationIds)
    {
        Owner = owner;
        ExpectedRevision = expectedRevision;
        NextRevision = nextRevision;
        ExpectedLocationIds = expectedLocationIds;
        NextLocationIds = nextLocationIds;
    }

    internal bool CanInstall(SpatialKnowledgeRuntime owner) => owner != null
        && owner.CanInstall(this);
}
