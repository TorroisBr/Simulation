using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

internal enum DailyV1P12DSelectedCoreOwnerStageFailure
{
    None = 0,
    InvalidInput,
    InvalidCityIdentity,
    InvalidNpcIdentity,
    MissingSpatialIdentity,
    CityHydrationRejected,
    NpcHydrationRejected,
    InvalidCityMembership,
    InvalidSpatialOwnerSnapshot
}

/// <summary>
/// Exact selected Core owner instances staged for one P12-D image. This is a
/// private restore result, not another persisted representation or owner store.
/// </summary>
internal sealed class DailyV1P12DSelectedCoreOwnerStage
{
    private readonly ReadOnlyCollection<DailyV1CityRuntimeOwners> cities;
    private readonly ReadOnlyCollection<DailyV1NpcRuntimeOwners> npcs;

    internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities => cities;
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs => npcs;

    internal DailyV1P12DSelectedCoreOwnerStage(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcs)
    {
        this.cities = Array.AsReadOnly(cities.ToArray());
        this.npcs = Array.AsReadOnly(npcs.ToArray());
    }

    internal bool TryGetCity(string runtimeId, out DailyV1CityRuntimeOwners owner)
    {
        owner = cities.FirstOrDefault(candidate =>
            string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal));
        return owner != null;
    }

    internal bool TryGetNpc(string runtimeId, out DailyV1NpcRuntimeOwners owner)
    {
        owner = npcs.FirstOrDefault(candidate =>
            string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal));
        return owner != null;
    }
}

/// <summary>
/// Core-owned unpublished P12-D restore state: the exact spatial owner, its
/// restored global identity index, and the selected City/NPC owners staged
/// against those same spatial identities.
/// </summary>
internal sealed class DailyV1P12DCoreRestoreOwnerStage
{
    internal SpatialNetworkOwner SpatialNetwork { get; }
    internal DailyV1RuntimeIdentityIndex RuntimeIdentityIndex { get; }
    internal DailyV1P12DSelectedCoreOwnerStage SelectedOwners { get; }
    internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities => SelectedOwners?.Cities;
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs => SelectedOwners?.Npcs;

    internal DailyV1P12DCoreRestoreOwnerStage(
        SpatialNetworkOwner spatialNetwork,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        DailyV1P12DSelectedCoreOwnerStage selectedOwners)
    {
        SpatialNetwork = spatialNetwork ?? throw new ArgumentNullException(nameof(spatialNetwork));
        RuntimeIdentityIndex = runtimeIdentityIndex ?? throw new ArgumentNullException(nameof(runtimeIdentityIndex));
        SelectedOwners = selectedOwners ?? throw new ArgumentNullException(nameof(selectedOwners));
    }
}
/// <summary>
/// Hydrates the selected P12-D City/NPC owner vectors as one Core staging
/// operation. Unity supplies detached row values and stable host IDs; Core
/// resolves canonical definitions, creates the only authoritative owners and
/// rejects nonreciprocal City membership before any Unity projection exists.
/// </summary>
internal static class DailyV1P12DSelectedCoreOwnerStager
{
    /// <summary>
    /// Stages the selected P12-D spatial owner, Core identity index, and
    /// selected City/NPC owner graph directly from values decoded from one
    /// admitted image. No Unity owner or runtime projection is accepted.
    /// </summary>
    internal static bool TryCreateFromImageValues(
        DailyV1EffectiveDefinitions definitions,
        SpatialNetworkOwnerSnapshot spatialSnapshot,
        P12CRuntimeIdentityOwnerSnapshot runtimeIdentitySnapshot,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<P12DNpcDRow> npcDRows,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        P12FTravelPartyOwnerSnapshot travelPartySnapshot,
        out DailyV1P12DCoreRestoreOwnerStage stage,
        out DailyV1P12DSelectedCoreOwnerStageFailure failure)
    {
        stage = null;
        failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidInput;
        if (definitions == null || spatialSnapshot == null || runtimeIdentitySnapshot == null
            || cityInputs == null || npcDRows == null || npcFRows == null
            || travelPartySnapshot?.Parties == null)
            return false;

        if (!P12DSpatialNetworkOwnerHydrator.TryCreateOwner(
                spatialSnapshot, out SpatialNetworkOwner spatialNetwork, out _)
            || spatialNetwork == null)
        {
            failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidSpatialOwnerSnapshot;
            return false;
        }

        if (!DailyV1RuntimeIdentityIndex.TryCreateFromOwnerSnapshot(
                runtimeIdentitySnapshot, out DailyV1RuntimeIdentityIndex runtimeIdentityIndex)
            || runtimeIdentityIndex == null)
            return false;

        List<string> travelPartyIds = new List<string>(travelPartySnapshot.Parties.Count);
        foreach (P12FTravelPartyOwnerSnapshotRecord party in travelPartySnapshot.Parties)
            travelPartyIds.Add(party?.TravelPartyId);

        if (!TryCreateFromRows(
                definitions, cityInputs, npcDRows, npcFRows, spatialNetwork, travelPartyIds,
                out DailyV1P12DSelectedCoreOwnerStage selectedOwners, out failure)
            || selectedOwners == null
            || !MatchesRestoredIdentityGraph(
                runtimeIdentitySnapshot, runtimeIdentityIndex, spatialNetwork,
                selectedOwners, cityInputs, npcDRows))
        {
            stage = null;
            if (failure == DailyV1P12DSelectedCoreOwnerStageFailure.None)
                failure = DailyV1P12DSelectedCoreOwnerStageFailure.MissingSpatialIdentity;
            return false;
        }

        stage = new DailyV1P12DCoreRestoreOwnerStage(
            spatialNetwork, runtimeIdentityIndex, selectedOwners);
        failure = DailyV1P12DSelectedCoreOwnerStageFailure.None;
        return true;
    }

    private static bool MatchesRestoredIdentityGraph(
        P12CRuntimeIdentityOwnerSnapshot snapshot,
        DailyV1RuntimeIdentityIndex identityIndex,
        SpatialNetworkOwner spatialNetwork,
        DailyV1P12DSelectedCoreOwnerStage owners,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<P12DNpcDRow> npcRows)
    {
        if (snapshot == null || identityIndex == null || spatialNetwork == null || owners == null
            || cityInputs == null || npcRows == null
            || identityIndex.CensusRevision != snapshot.Revision
            || owners.Cities.Count != cityInputs.Count || owners.Npcs.Count != npcRows.Count
            || identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Location) != spatialNetwork.LocationCount
            || identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Route) != spatialNetwork.RouteCount
            || identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.City) != owners.Cities.Count
            || identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Npc) != owners.Npcs.Count)
            return false;

        foreach (SpatialLocationRuntime location in spatialNetwork.Locations)
            if (location == null
                || !identityIndex.HasIdentity(location.RuntimeId, DailyV1RuntimeIdentityKind.Location))
                return false;
        foreach (SpatialRouteRuntime route in spatialNetwork.Routes)
            if (route == null
                || !identityIndex.HasIdentity(route.RuntimeId, DailyV1RuntimeIdentityKind.Route))
                return false;
        for (int i = 0; i < cityInputs.Count; i++)
            if (cityInputs[i] == null || owners.Cities[i] == null
                || !string.Equals(cityInputs[i].CityRuntimeId, owners.Cities[i].RuntimeId,
                    StringComparison.Ordinal)
                || !identityIndex.HasIdentity(owners.Cities[i].RuntimeId, DailyV1RuntimeIdentityKind.City))
                return false;
        for (int i = 0; i < npcRows.Count; i++)
            if (npcRows[i] == null || owners.Npcs[i] == null
                || !string.Equals(npcRows[i].RuntimeId, owners.Npcs[i].RuntimeId,
                    StringComparison.Ordinal)
                || !identityIndex.HasIdentity(owners.Npcs[i].RuntimeId, DailyV1RuntimeIdentityKind.Npc))
                return false;
        return true;
    }
    internal static bool TryCreateFromRows(
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<P12DNpcDRow> npcDRows,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        SpatialNetworkOwner spatialNetwork,
        IEnumerable<string> travelPartyIds,
        out DailyV1P12DSelectedCoreOwnerStage stage,
        out DailyV1P12DSelectedCoreOwnerStageFailure failure)
    {
        stage = null;
        failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidInput;
        if (definitions == null || cityInputs == null || npcDRows == null || npcFRows == null
            || npcDRows.Count != npcFRows.Count || spatialNetwork == null || travelPartyIds == null)
            return false;

        List<DailyV1P12DNpcCoreOwnerHydrationInput> npcInputs =
            new List<DailyV1P12DNpcCoreOwnerHydrationInput>(npcDRows.Count);
        for (int i = 0; i < npcDRows.Count; i++)
        {
            if (!DailyV1P12DNpcCoreOwnerHydrationInput.TryCreateFromRows(
                    npcDRows[i], npcFRows[i], out DailyV1P12DNpcCoreOwnerHydrationInput input)
                || input == null)
            {
                failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidNpcIdentity;
                return false;
            }
            npcInputs.Add(input);
        }

        return TryCreate(
            definitions, cityInputs, npcInputs, spatialNetwork, travelPartyIds,
            out stage, out failure);
    }
    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<DailyV1P12DNpcCoreOwnerHydrationInput> npcInputs,
        SpatialNetworkOwner spatialNetwork,
        IEnumerable<string> travelPartyIds,
        out DailyV1P12DSelectedCoreOwnerStage stage,
        out DailyV1P12DSelectedCoreOwnerStageFailure failure)
    {
        stage = null;
        failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidInput;
        if (definitions == null || cityInputs == null || npcInputs == null
            || (cityInputs.Count == 0 && npcInputs.Count == 0) || spatialNetwork == null
            || !TryBuildIdSet(travelPartyIds, out HashSet<string> partyIds))
            return false;

        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (SpatialLocationRuntime location in spatialNetwork.Locations)
            if (location == null || !locationIds.Add(location.RuntimeId))
            {
                failure = DailyV1P12DSelectedCoreOwnerStageFailure.MissingSpatialIdentity;
                return false;
            }

        HashSet<string> routeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (SpatialRouteRuntime route in spatialNetwork.Routes)
            if (route == null || !routeIds.Add(route.RuntimeId))
            {
                failure = DailyV1P12DSelectedCoreOwnerStageFailure.MissingSpatialIdentity;
                return false;
            }

        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1P12DCityCoreOwnerHydrationInput input in cityInputs)
            if (input == null || string.IsNullOrWhiteSpace(input.CityRuntimeId)
                || !cityIds.Add(input.CityRuntimeId))
            {
                failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidCityIdentity;
                return false;
            }

        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1P12DNpcCoreOwnerHydrationInput input in npcInputs)
            if (input?.DRow == null || string.IsNullOrWhiteSpace(input.DRow.RuntimeId)
                || !npcIds.Add(input.DRow.RuntimeId))
            {
                failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidNpcIdentity;
                return false;
            }

        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>(cityInputs.Count);
        foreach (DailyV1P12DCityCoreOwnerHydrationInput input in cityInputs)
        {
            if (!locationIds.Contains(input.ResolvedLocationRuntimeId)
                || !DailyV1P12DCityCoreOwnerHydrator.TryCreate(
                    input, definitions, out DailyV1CityRuntimeOwners owner, out _)
                || owner == null
                || !ReferenceEquals(owner.Definitions, definitions)
                || !string.Equals(owner.RuntimeId, input.CityRuntimeId, StringComparison.Ordinal)
                || !string.Equals(owner.LocationRuntimeId, input.ResolvedLocationRuntimeId,
                    StringComparison.Ordinal))
            {
                failure = locationIds.Contains(input.ResolvedLocationRuntimeId)
                    ? DailyV1P12DSelectedCoreOwnerStageFailure.CityHydrationRejected
                    : DailyV1P12DSelectedCoreOwnerStageFailure.MissingSpatialIdentity;
                return false;
            }
            cities.Add(owner);
        }

        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>(npcInputs.Count);
        foreach (DailyV1P12DNpcCoreOwnerHydrationInput input in npcInputs)
        {
            if (!DailyV1P12DNpcCoreOwnerHydrator.TryCreate(
                    input, definitions, cityIds, locationIds, routeIds, partyIds, npcIds,
                    out DailyV1NpcRuntimeOwners owner, out _)
                || owner == null
                || !ReferenceEquals(owner.Definitions, definitions)
                || !string.Equals(owner.RuntimeId, input.DRow.RuntimeId, StringComparison.Ordinal))
            {
                failure = DailyV1P12DSelectedCoreOwnerStageFailure.NpcHydrationRejected;
                return false;
            }
            npcs.Add(owner);
        }

        if (!DailyV1P12DSelectedOwnerGraphStager.HasConsistentRestoredMembership(cities, npcs))
        {
            failure = DailyV1P12DSelectedCoreOwnerStageFailure.InvalidCityMembership;
            return false;
        }

        stage = new DailyV1P12DSelectedCoreOwnerStage(cities, npcs);
        failure = DailyV1P12DSelectedCoreOwnerStageFailure.None;
        return true;
    }

    private static bool TryBuildIdSet(IEnumerable<string> values, out HashSet<string> ids)
    {
        ids = new HashSet<string>(StringComparer.Ordinal);
        if (values == null) return false;
        foreach (string value in values)
            if (string.IsNullOrWhiteSpace(value) || !ids.Add(value)) return false;
        return true;
    }
}
