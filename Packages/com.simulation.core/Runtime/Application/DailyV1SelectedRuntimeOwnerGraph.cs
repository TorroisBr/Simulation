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
        DailyV1CityNpcPresenceOwner npcPresence,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1CityRuntimeOwners.TryCreateBound(
                runtimeId, definitions, definition, population, market, counterparty,
                populationEconomy, npcPresence, out DailyV1CityRuntimeOwners candidate)
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
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1NpcRuntimeOwners.TryCreateBound(
                runtimeId, definitions, definition, statuses, inventory, moneyAccount, commercialKnowledge,
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

    /// <summary>
    /// Creates the exact selected-owner graph for a roster that has admitted
    /// one additional NPC. The existing graph and all owner authorities stay
    /// immutable; only the runtime's graph reference changes at the membership
    /// boundary.
    /// </summary>
    internal bool TryCreateWithAddedNpc(
        DailyV1NpcRuntimeOwners owner,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (owner == null || !ReferenceEquals(owner.Definitions, Definitions)) return false;

        List<DailyV1NpcRuntimeOwners> updatedNpcs = new List<DailyV1NpcRuntimeOwners>(npcs)
        {
            owner
        };
        return TryCreate(Definitions, cities, updatedNpcs, out graph);
    }

    /// <summary>
    /// Creates the exact selected-owner graph for a roster after one NPC has
    /// been removed. Removal is by owner-instance identity, matching the
    /// runtime registry and alias rules.
    /// </summary>
    internal bool TryCreateWithoutNpc(
        DailyV1NpcRuntimeOwners owner,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (owner == null || !ReferenceEquals(owner.Definitions, Definitions)) return false;

        bool removed = false;
        List<DailyV1NpcRuntimeOwners> updatedNpcs = new List<DailyV1NpcRuntimeOwners>(npcs.Count);
        foreach (DailyV1NpcRuntimeOwners current in npcs)
        {
            if (!removed && ReferenceEquals(current, owner))
            {
                removed = true;
                continue;
            }
            updatedNpcs.Add(current);
        }

        return removed && TryCreate(Definitions, cities, updatedNpcs, out graph);
    }

    /// <summary>
    /// Resolves an admitted action's exact status effects against this runtime's
    /// selected definition graph. Target application is deliberately deferred
    /// until after the actor callback, matching the existing actuation order.
    /// </summary>
    internal bool TryCreateActionStatusPlan(
        DailyV1NpcRuntimeOwners actorOwner,
        DailyV1ActionDefinition action,
        bool targetExpected,
        DailyV1NpcRuntimeOwners targetOwner,
        out DailyV1SelectedActionStatusPlan plan)
    {
        plan = null;
        if (actorOwner == null
            || !ReferenceEquals(actorOwner.Definitions, Definitions)
            || !ContainsExactNpcOwner(actorOwner)
            || (!targetExpected && targetOwner != null)
            || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                Definitions, action,
                out IReadOnlyList<DailyV1StatusDefinition> actorStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> actorStatusToAdd,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd))
            return false;

        bool targetMutationExpected = targetExpected
            && (targetStatusToRemove.Count > 0 || targetStatusToAdd.Count > 0);
        if (targetMutationExpected
            && (targetOwner == null
                || !ReferenceEquals(targetOwner.Definitions, Definitions)
                || !ContainsExactNpcOwner(targetOwner)))
            return false;

        plan = new DailyV1SelectedActionStatusPlan(
            actorOwner,
            actorStatusToRemove,
            actorStatusToAdd,
            targetMutationExpected,
            targetMutationExpected ? targetOwner : null,
            targetStatusToRemove,
            targetStatusToAdd);
        return true;
    }

    private bool ContainsExactNpcOwner(DailyV1NpcRuntimeOwners candidate)
    {
        foreach (DailyV1NpcRuntimeOwners owner in npcs)
            if (ReferenceEquals(owner, candidate)) return true;
        return false;
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

/// <summary>
/// Builds the initial selected Daily-v1 City/NPC authorities from the admitted
/// immutable definition graph and the runtime identities allocated by genesis.
/// Unity supplies identity values and later projects the resulting owner
/// instances; it does not construct a parallel selected owner graph.
/// </summary>
internal sealed class DailyV1SelectedRuntimeOwnerGraphBuilder
{
    private readonly DailyV1EffectiveDefinitions definitions;
    private readonly List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
    private readonly List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
    private readonly Dictionary<string, List<DailyV1CityRuntimeOwners>> citiesByDefinition =
        new Dictionary<string, List<DailyV1CityRuntimeOwners>>(StringComparer.Ordinal);
    private readonly Dictionary<DailyV1CityRuntimeOwners, string> locationIdsByCityOwner =
        new Dictionary<DailyV1CityRuntimeOwners, string>();
    private readonly HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
    private bool built;

    internal DailyV1SelectedRuntimeOwnerGraphBuilder(DailyV1EffectiveDefinitions definitions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities => cities.AsReadOnly();
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs => npcs.AsReadOnly();

    /// <summary>
    /// Performs the selected City's identity allocation and authoritative
    /// owner construction in Core. The callback only admits the corresponding
    /// host location projection; it receives the already allocated identity
    /// and must not allocate another one.
    /// </summary>
    internal bool TryCreateConfiguredCities(
        RuntimeIdAllocator runtimeIds,
        Func<string, bool> tryRegisterLocationProjection)
    {
        if (built || runtimeIds == null || tryRegisterLocationProjection == null
            || definitions.Configuration == null)
            return false;

        List<string> selectedCityIds = new List<string>(
            definitions.Configuration.cities ?? Array.Empty<string>());
        selectedCityIds.Sort(StringComparer.Ordinal);
        foreach (string cityId in selectedCityIds)
        {
            if (string.IsNullOrWhiteSpace(cityId)) continue;
            if (!definitions.Cities.TryGetValue(cityId, out DailyV1CityDefinition definition)
                || definition == null
                || !string.Equals(definition.DefinitionId, cityId, StringComparison.Ordinal))
                return false;

            string locationRuntimeId = runtimeIds.AllocateLocationId();
            if (!tryRegisterLocationProjection(locationRuntimeId)) continue;

            string cityRuntimeId = runtimeIds.AllocateCityId();
            if (!TryAddInitialCity(cityRuntimeId, locationRuntimeId, definition, out _)) return false;
        }

        return true;
    }

    /// <summary>
    /// Allocates selected NPC identities and builds their exact definition,
    /// inventory, travel, and City-presence owners in Core. The optional
    /// resolver is a host projection check used to preserve existing Unity
    /// missing/ambiguous City diagnostics before each NPC identity is issued.
    /// </summary>
    internal bool TryCreateConfiguredNpcs(
        RuntimeIdAllocator runtimeIds,
        Func<string, string> resolveStartingCityProjection)
    {
        if (built || runtimeIds == null || definitions.Configuration == null) return false;

        Dictionary<string, DailyV1NpcSimulationConfig> selectedRows =
            new Dictionary<string, DailyV1NpcSimulationConfig>(StringComparer.Ordinal);
        foreach (DailyV1NpcSimulationConfig row in
            definitions.Configuration.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row == null || string.IsNullOrWhiteSpace(row.npc)) continue;
            if (!selectedRows.TryAdd(row.npc, row)) return false;
        }

        List<string> selectedNpcIds = new List<string>(selectedRows.Keys);
        selectedNpcIds.Sort(StringComparer.Ordinal);
        foreach (string npcId in selectedNpcIds)
        {
            if (!definitions.Npcs.TryGetValue(npcId, out DailyV1NpcDefinition definition)
                || definition == null
                || !string.Equals(definition.DefinitionId, npcId, StringComparison.Ordinal))
                return false;

            DailyV1NpcSimulationConfig configuration = selectedRows[npcId];
            DailyV1CityRuntimeOwners startingCity = null;
            if (!string.IsNullOrWhiteSpace(configuration.startingCity))
            {
                if (!definitions.Cities.ContainsKey(configuration.startingCity)) return false;
                startingCity = ResolveUniqueStartingCity(configuration.startingCity);
                string projectedCityRuntimeId = resolveStartingCityProjection == null
                    ? startingCity?.RuntimeId
                    : resolveStartingCityProjection(configuration.startingCity);
                if (!string.Equals(projectedCityRuntimeId, startingCity?.RuntimeId, StringComparison.Ordinal))
                    return false;
            }

            string npcRuntimeId = runtimeIds.AllocateNpcId();
            if (!TryAddInitialNpc(npcRuntimeId, definition, configuration, out _)) return false;
        }

        return true;
    }

    internal bool TryAddInitialCity(
        string runtimeId,
        string locationRuntimeId,
        DailyV1CityDefinition definition,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (built
            || string.IsNullOrWhiteSpace(runtimeId)
            || string.IsNullOrWhiteSpace(locationRuntimeId)
            || !runtimeIds.Add(runtimeId)) return false;
        if (!locationIds.Add(locationRuntimeId))
        {
            runtimeIds.Remove(runtimeId);
            return false;
        }
        if (!DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
                runtimeId, definitions, definition, null, out DailyV1CityRuntimeOwners candidate))
        {
            runtimeIds.Remove(runtimeId);
            locationIds.Remove(locationRuntimeId);
            return false;
        }

        cities.Add(candidate);
        locationIdsByCityOwner.Add(candidate, locationRuntimeId);
        if (!citiesByDefinition.TryGetValue(definition.DefinitionId, out List<DailyV1CityRuntimeOwners> matches))
        {
            matches = new List<DailyV1CityRuntimeOwners>();
            citiesByDefinition.Add(definition.DefinitionId, matches);
        }
        matches.Add(candidate);
        owners = candidate;
        return true;
    }

    internal bool TryRemoveInitialCity(DailyV1CityRuntimeOwners owners)
    {
        if (built || owners == null || !cities.Remove(owners)
            || !locationIdsByCityOwner.TryGetValue(owners, out string locationId)) return false;
        locationIdsByCityOwner.Remove(owners);
        locationIds.Remove(locationId);
        runtimeIds.Remove(owners.RuntimeId);
        if (citiesByDefinition.TryGetValue(owners.Definition.DefinitionId, out List<DailyV1CityRuntimeOwners> matches))
        {
            matches.Remove(owners);
            if (matches.Count == 0) citiesByDefinition.Remove(owners.Definition.DefinitionId);
        }
        return true;
    }

    internal bool TryAddInitialNpc(
        string runtimeId,
        DailyV1NpcDefinition definition,
        DailyV1NpcSimulationConfig configuration,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (built || string.IsNullOrWhiteSpace(runtimeId) || !runtimeIds.Add(runtimeId)) return false;
        if (!IsExactConfiguredNpc(definition, configuration)
            || (!string.IsNullOrWhiteSpace(configuration.startingCity)
                && !definitions.Cities.ContainsKey(configuration.startingCity))
            || !DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
                runtimeId, definitions, definition, configuration.initialMoney,
                out DailyV1NpcRuntimeOwners candidate)
            || !ApplyInitialInventory(candidate.Inventory, configuration))
        {
            runtimeIds.Remove(runtimeId);
            return false;
        }

        DailyV1CityRuntimeOwners startingCity = ResolveUniqueStartingCity(configuration.startingCity);
        if (startingCity != null)
        {
            if (!locationIdsByCityOwner.TryGetValue(startingCity, out string locationId)
                || !startingCity.NpcPresence.TryAdd(runtimeId)
                || !candidate.Travel.TryCommitState(
                    locationId, startingCity.RuntimeId,
                    null, null, 0, 0, null, false, null, null, false))
            {
                runtimeIds.Remove(runtimeId);
                return false;
            }
        }

        npcs.Add(candidate);
        owners = candidate;
        return true;
    }

    internal bool TryRemoveInitialNpc(DailyV1NpcRuntimeOwners owners)
    {
        if (built || owners == null || !npcs.Remove(owners)) return false;
        runtimeIds.Remove(owners.RuntimeId);
        return true;
    }

    internal DailyV1CityRuntimeOwners ResolveUniqueStartingCity(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)
            || !citiesByDefinition.TryGetValue(definitionId, out List<DailyV1CityRuntimeOwners> matches)
            || matches.Count != 1)
            return null;
        return matches[0];
    }

    internal bool TryGetLocationRuntimeId(DailyV1CityRuntimeOwners city, out string runtimeId)
    {
        runtimeId = null;
        return city != null && locationIdsByCityOwner.TryGetValue(city, out runtimeId);
    }

    internal bool TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (built || !MatchesConfiguredCityRows() || !MatchesConfiguredNpcRows()
            || !HasConsistentInitialPresence()) return false;
        if (!DailyV1SelectedRuntimeOwnerGraph.TryCreate(definitions, cities, npcs, out graph))
            return false;
        built = true;
        return true;
    }

    private bool IsExactConfiguredNpc(
        DailyV1NpcDefinition definition,
        DailyV1NpcSimulationConfig configuration)
    {
        if (definition == null || configuration == null
            || !string.Equals(configuration.npc, definition.DefinitionId, StringComparison.Ordinal)
            || !definitions.Npcs.TryGetValue(definition.DefinitionId, out DailyV1NpcDefinition admitted)
            || !ReferenceEquals(definition, admitted)) return false;

        DailyV1NpcSimulationConfig exact = null;
        foreach (DailyV1NpcSimulationConfig row in definitions.Configuration?.npcs
            ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row == null || !string.Equals(row.npc, definition.DefinitionId, StringComparison.Ordinal)) continue;
            if (exact != null) return false;
            exact = row;
        }
        return ReferenceEquals(configuration, exact);
    }

    private bool ApplyInitialInventory(
        DailyV1InventoryOwner inventory,
        DailyV1NpcSimulationConfig configuration)
    {
        if (inventory == null || configuration == null
            || !ReferenceEquals(inventory.Definitions, definitions)) return false;
        foreach (DailyV1NpcInitialInventoryItemConfig row in
            configuration.initialInventory ?? Array.Empty<DailyV1NpcInitialInventoryItemConfig>())
        {
            if (row == null || string.IsNullOrWhiteSpace(row.item)) continue;
            if (!definitions.Items.TryGetValue(row.item, out DailyV1ItemDefinition item)
                || !inventory.TryAddItem(item, row.amount, row.averageUnitCost)) return false;
        }
        return true;
    }

    private bool MatchesConfiguredCityRows()
    {
        List<string> expected = new List<string>(
            definitions.Configuration?.cities ?? Array.Empty<string>());
        expected.RemoveAll(string.IsNullOrWhiteSpace);
        expected.Sort(StringComparer.Ordinal);
        if (expected.Count != cities.Count) return false;
        for (int index = 0; index < expected.Count; index++)
            if (!string.Equals(expected[index], cities[index].Definition.DefinitionId, StringComparison.Ordinal))
                return false;
        return true;
    }

    private bool MatchesConfiguredNpcRows()
    {
        Dictionary<string, DailyV1NpcSimulationConfig> expected =
            new Dictionary<string, DailyV1NpcSimulationConfig>(StringComparer.Ordinal);
        foreach (DailyV1NpcSimulationConfig row in definitions.Configuration?.npcs
            ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row == null || string.IsNullOrWhiteSpace(row.npc)) continue;
            if (expected.ContainsKey(row.npc)) return false;
            expected.Add(row.npc, row);
        }
        List<string> ids = new List<string>(expected.Keys);
        ids.Sort(StringComparer.Ordinal);
        if (ids.Count != npcs.Count) return false;
        for (int index = 0; index < ids.Count; index++)
            if (!string.Equals(ids[index], npcs[index].Definition.DefinitionId, StringComparison.Ordinal))
                return false;
        return true;
    }

    private bool HasConsistentInitialPresence()
    {
        HashSet<string> seenNpcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (!locationIdsByCityOwner.ContainsKey(city)) return false;
            foreach (string npcRuntimeId in city.NpcPresence.OrderedNpcRuntimeIds)
            {
                if (!seenNpcIds.Add(npcRuntimeId)) return false;
                DailyV1NpcRuntimeOwners npc = npcs.Find(owner =>
                    string.Equals(owner.RuntimeId, npcRuntimeId, StringComparison.Ordinal));
                if (npc == null
                    || !string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    || !locationIdsByCityOwner.TryGetValue(city, out string locationId)
                    || !string.Equals(npc.Travel.CurrentLocationRuntimeId, locationId, StringComparison.Ordinal))
                    return false;
            }
        }

        foreach (DailyV1NpcRuntimeOwners npc in npcs)
        {
            if (npc.Travel.CurrentCityRuntimeId == null)
            {
                if (npc.Travel.CurrentLocationRuntimeId != null || seenNpcIds.Contains(npc.RuntimeId)) return false;
                continue;
            }
            if (!seenNpcIds.Contains(npc.RuntimeId)) return false;
        }
        return true;
    }
}

internal enum DailyV1SelectedActionStatusPlanResult
{
    Applied,
    ActorRejected,
    TargetRejected
}

/// <summary>
/// Immutable actor-first/target-second status mutation plan. The adapter
/// callbacks still own host-specific admission, revision, epoch and
/// compatibility projection behavior through each existing NPC owner writer.
/// </summary>
internal sealed class DailyV1SelectedActionStatusPlan
{
    internal delegate bool ApplyOwnerStatusChanges(
        DailyV1NpcRuntimeOwners owner,
        IReadOnlyList<DailyV1StatusDefinition> statusToRemove,
        IReadOnlyList<DailyV1StatusDefinition> statusToAdd);

    private readonly DailyV1NpcRuntimeOwners actorOwner;
    private readonly IReadOnlyList<DailyV1StatusDefinition> actorStatusToRemove;
    private readonly IReadOnlyList<DailyV1StatusDefinition> actorStatusToAdd;
    private readonly bool targetExpected;
    private readonly DailyV1NpcRuntimeOwners targetOwner;
    private readonly IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove;
    private readonly IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd;

    internal DailyV1SelectedActionStatusPlan(
        DailyV1NpcRuntimeOwners actorOwner,
        IReadOnlyList<DailyV1StatusDefinition> actorStatusToRemove,
        IReadOnlyList<DailyV1StatusDefinition> actorStatusToAdd,
        bool targetExpected,
        DailyV1NpcRuntimeOwners targetOwner,
        IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
        IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd)
    {
        this.actorOwner = actorOwner ?? throw new ArgumentNullException(nameof(actorOwner));
        this.actorStatusToRemove = actorStatusToRemove ?? throw new ArgumentNullException(nameof(actorStatusToRemove));
        this.actorStatusToAdd = actorStatusToAdd ?? throw new ArgumentNullException(nameof(actorStatusToAdd));
        this.targetExpected = targetExpected;
        this.targetOwner = targetOwner;
        this.targetStatusToRemove = targetStatusToRemove ?? throw new ArgumentNullException(nameof(targetStatusToRemove));
        this.targetStatusToAdd = targetStatusToAdd ?? throw new ArgumentNullException(nameof(targetStatusToAdd));
    }

    internal DailyV1SelectedActionStatusPlanResult Apply(
        ApplyOwnerStatusChanges applyActor,
        ApplyOwnerStatusChanges applyTarget)
    {
        if (applyActor == null
            || !applyActor(actorOwner, actorStatusToRemove, actorStatusToAdd))
            return DailyV1SelectedActionStatusPlanResult.ActorRejected;

        if (!targetExpected) return DailyV1SelectedActionStatusPlanResult.Applied;

        return applyTarget != null
            && applyTarget(targetOwner, targetStatusToRemove, targetStatusToAdd)
            ? DailyV1SelectedActionStatusPlanResult.Applied
            : DailyV1SelectedActionStatusPlanResult.TargetRejected;
    }
}
