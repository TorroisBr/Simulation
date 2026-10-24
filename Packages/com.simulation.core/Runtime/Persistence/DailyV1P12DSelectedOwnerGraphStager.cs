using System;
using System.Collections.Generic;
using System.Linq;

internal enum DailyV1P12DSelectedOwnerGraphFailure
{
    None = 0,
    InvalidInput,
    InvalidCityOwner,
    InvalidNpcOwner,
    InvalidCityMembership,
    GraphRejected
}

/// <summary>
/// Composes the exact selected Core owners retained by private P12-D staging
/// into the restored runtime graph. Unity projections may provide those owner
/// references, but this Core boundary owns graph admission and deterministic
/// owner ordering.
/// </summary>
internal static class DailyV1P12DSelectedOwnerGraphStager
{
    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1CityRuntimeOwners> cityOwners,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecords,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore persons,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1P12DSelectedOwnerGraphFailure failure)
    {
        graph = null;
        failure = DailyV1P12DSelectedOwnerGraphFailure.InvalidInput;
        if (definitions == null || cityOwners == null || npcOwners == null
            || (cityOwners.Count == 0 && npcOwners.Count == 0)
            || spatialNetwork == null || justiceRecords == null || travelParties == null
            || runtimeIdentityIndex == null || persons == null || directives == null || expeditions == null)
            return false;

        DailyV1CityRuntimeOwners[] orderedCities = cityOwners
            .OrderBy(owner => owner?.RuntimeId ?? string.Empty, StringComparer.Ordinal)
            .ToArray();
        DailyV1NpcRuntimeOwners[] orderedNpcs = npcOwners
            .OrderBy(owner => owner?.RuntimeId ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners owner in orderedCities)
        {
            if (owner == null || !ReferenceEquals(owner.Definitions, definitions)
                || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || string.IsNullOrWhiteSpace(owner.LocationRuntimeId)
                || !cityIds.Add(owner.RuntimeId))
            {
                failure = DailyV1P12DSelectedOwnerGraphFailure.InvalidCityOwner;
                return false;
            }
        }

        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners owner in orderedNpcs)
        {
            if (owner == null || !ReferenceEquals(owner.Definitions, definitions)
                || string.IsNullOrWhiteSpace(owner.RuntimeId) || !npcIds.Add(owner.RuntimeId))
            {
                failure = DailyV1P12DSelectedOwnerGraphFailure.InvalidNpcOwner;
                return false;
            }
        }

        // P12-D restore admission is stricter than runtime-operation graph
        // construction: the detached City membership list and each NPC's
        // restored current City/Location must be reciprocal before the graph
        // can be returned. Keep this check here rather than in the generic
        // graph factory, whose callers include in-flight operation plans.
        if (!HasConsistentRestoredMembership(orderedCities, orderedNpcs))
        {
            failure = DailyV1P12DSelectedOwnerGraphFailure.InvalidCityMembership;
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
                orderedCities, orderedNpcs, spatialNetwork, justiceRecords, travelParties,
                runtimeIdentityIndex, persons, out DailyV1SelectedRuntimeOwnerGraph candidate)
            || candidate == null
            || !ReferenceEquals(candidate.Definitions, definitions)
            || !ReferenceEquals(candidate.SpatialNetwork, spatialNetwork)
            || !ReferenceEquals(candidate.JusticeRecordsOwner, justiceRecords)
            || !ReferenceEquals(candidate.TravelParties, travelParties)
            || !ReferenceEquals(candidate.RuntimeIdentityIndex, runtimeIdentityIndex)
            || !ReferenceEquals(candidate.PersonStore, persons)
            || candidate.Cities.Count != orderedCities.Length
            || candidate.Npcs.Count != orderedNpcs.Length
            || !HasExactOrder(candidate.Cities, orderedCities)
            || !HasExactOrder(candidate.Npcs, orderedNpcs)
            || !candidate.TryBindScheduledDirectives(directives)
            || !candidate.TryBindExpeditionStore(expeditions))
        {
            failure = DailyV1P12DSelectedOwnerGraphFailure.GraphRejected;
            return false;
        }

        graph = candidate;
        failure = DailyV1P12DSelectedOwnerGraphFailure.None;
        return true;
    }

    private static bool HasConsistentRestoredMembership(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcs)
    {
        Dictionary<string, DailyV1CityRuntimeOwners> citiesById =
            new Dictionary<string, DailyV1CityRuntimeOwners>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (city?.NpcPresence == null
                || !string.Equals(city.NpcPresence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                || city.NpcPresence.OrderedNpcRuntimeIds == null
                || !citiesById.TryAdd(city.RuntimeId, city))
                return false;
        }

        Dictionary<string, DailyV1NpcRuntimeOwners> npcsById =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in npcs)
        {
            if (npc?.Travel == null
                || !string.Equals(npc.Travel.NpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                || !npcsById.TryAdd(npc.RuntimeId, npc))
                return false;
        }

        HashSet<string> admittedMembers = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            HashSet<string> cityMembers = new HashSet<string>(StringComparer.Ordinal);
            foreach (string npcRuntimeId in city.NpcPresence.OrderedNpcRuntimeIds)
            {
                if (string.IsNullOrWhiteSpace(npcRuntimeId)
                    || !cityMembers.Add(npcRuntimeId)
                    || !admittedMembers.Add(npcRuntimeId)
                    || !npcsById.TryGetValue(npcRuntimeId, out DailyV1NpcRuntimeOwners npc)
                    || !string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(npc.Travel.CurrentLocationRuntimeId,
                        city.LocationRuntimeId, StringComparison.Ordinal))
                    return false;
            }
        }

        foreach (DailyV1NpcRuntimeOwners npc in npcs)
        {
            string cityRuntimeId = npc.Travel.CurrentCityRuntimeId;
            if (cityRuntimeId == null)
            {
                if (npc.Travel.CurrentLocationRuntimeId != null || admittedMembers.Contains(npc.RuntimeId))
                    return false;
                continue;
            }

            if (!citiesById.TryGetValue(cityRuntimeId, out DailyV1CityRuntimeOwners city)
                || !string.Equals(npc.Travel.CurrentLocationRuntimeId,
                    city.LocationRuntimeId, StringComparison.Ordinal)
                || !admittedMembers.Contains(npc.RuntimeId))
                return false;
        }

        return true;
    }

    private static bool HasExactOrder<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected)
        where T : class
    {
        if (actual == null || expected == null || actual.Count != expected.Count) return false;
        for (int i = 0; i < actual.Count; i++)
            if (!ReferenceEquals(actual[i], expected[i])) return false;
        return true;
    }
}
