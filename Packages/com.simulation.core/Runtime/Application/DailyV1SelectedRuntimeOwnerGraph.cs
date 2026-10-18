using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Read-only join of the exact selected City and NPC owner compositions held
/// by one runtime. The graph indexes the existing authorities and owns no
/// mutable simulation state itself.
/// </summary>
internal sealed class DailyV1SelectedRuntimeOwnerGraph
{
    private readonly ReadOnlyCollection<DailyV1CityRuntimeOwners> cities;
    private readonly ReadOnlyCollection<DailyV1NpcRuntimeOwners> npcs;

    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities => cities;
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs => npcs;

    private DailyV1SelectedRuntimeOwnerGraph(
        DailyV1EffectiveDefinitions definitions,
        List<DailyV1CityRuntimeOwners> cities,
        List<DailyV1NpcRuntimeOwners> npcs)
    {
        Definitions = definitions;
        this.cities = cities.AsReadOnly();
        this.npcs = npcs.AsReadOnly();
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (definitions == null || cityOwners == null || npcOwners == null) return false;

        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners owner in cityOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Cities.TryGetValue(owner.Definition.DefinitionId, out DailyV1CityDefinition city)
                || !ReferenceEquals(city, owner.Definition)
                || !runtimeIds.Add(owner.RuntimeId)) return false;
            cities.Add(owner);
        }
        foreach (DailyV1NpcRuntimeOwners owner in npcOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Npcs.TryGetValue(owner.Definition.DefinitionId, out DailyV1NpcDefinition npc)
                || !ReferenceEquals(npc, owner.Definition)
                || !runtimeIds.Add(owner.RuntimeId)) return false;
            npcs.Add(owner);
        }

        graph = new DailyV1SelectedRuntimeOwnerGraph(definitions, cities, npcs);
        return true;
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (cityOwners == null || npcOwners == null) return false;

        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        DailyV1EffectiveDefinitions definitions = null;
        foreach (DailyV1CityRuntimeOwners owner in cityOwners)
        {
            if (owner == null || owner.Definitions == null) return false;
            if (definitions == null) definitions = owner.Definitions;
            else if (!ReferenceEquals(definitions, owner.Definitions)) return false;
            cities.Add(owner);
        }
        foreach (DailyV1NpcRuntimeOwners owner in npcOwners)
        {
            if (owner == null || owner.Definitions == null) return false;
            if (definitions == null) definitions = owner.Definitions;
            else if (!ReferenceEquals(definitions, owner.Definitions)) return false;
            npcs.Add(owner);
        }

        return definitions != null && TryCreate(definitions, cities, npcs, out graph);
    }

    internal static bool TryCreateInitialCityOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        MarketCounterpartyRuntime suppliedCounterparty,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1CityRuntimeOwners.TryCreateInitial(
                runtimeId, definitions, definition, suppliedCounterparty,
                out DailyV1CityRuntimeOwners candidate)
            || !TryCreate(definitions, new[] { candidate }, Array.Empty<DailyV1NpcRuntimeOwners>(), out _))
            return false;

        owners = candidate;
        return true;
    }

    internal static bool TryCreateBoundCityOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        SettlementPopulationRuntime population,
        DailyV1MarketOwner market,
        MarketCounterpartyRuntime counterparty,
        PopulationEconomyRuntime populationEconomy,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1CityRuntimeOwners.TryCreateBound(
                runtimeId, definitions, definition, population, market, counterparty,
                populationEconomy, out DailyV1CityRuntimeOwners candidate)
            || !TryCreate(definitions, new[] { candidate }, Array.Empty<DailyV1NpcRuntimeOwners>(), out _))
            return false;

        owners = candidate;
        return true;
    }

    internal static bool TryCreateInitialNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1NpcRuntimeOwners.TryCreateInitial(
                runtimeId, definitions, definition, initialMoney,
                out DailyV1NpcRuntimeOwners candidate)
            || !TryCreate(definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { candidate }, out _))
            return false;

        owners = candidate;
        return true;
    }

    internal static bool TryCreateBoundNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1NpcRuntimeOwners.TryCreateBound(
                runtimeId, definitions, definition, statuses, inventory, moneyAccount,
                out DailyV1NpcRuntimeOwners candidate)
            || !TryCreate(definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { candidate }, out _))
            return false;

        owners = candidate;
        return true;
    }

    internal bool MatchesExactOwnerInstances(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners)
    {
        return MatchesExactOwnerInstances(cities, cityOwners)
            && MatchesExactOwnerInstances(npcs, npcOwners);
    }

    private static bool MatchesExactOwnerInstances<T>(
        IReadOnlyList<T> expected,
        IEnumerable<T> actual)
        where T : class
    {
        if (expected == null || actual == null) return false;
        int index = 0;
        foreach (T owner in actual)
        {
            if (index >= expected.Count || !ReferenceEquals(expected[index], owner)) return false;
            index++;
        }

        return index == expected.Count;
    }
}
