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
    InvalidCityMembership
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
/// Hydrates the selected P12-D City/NPC owner vectors as one Core staging
/// operation. Unity supplies detached row values and stable host IDs; Core
/// resolves canonical definitions, creates the only authoritative owners and
/// rejects nonreciprocal City membership before any Unity projection exists.
/// </summary>
internal static class DailyV1P12DSelectedCoreOwnerStager
{
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
