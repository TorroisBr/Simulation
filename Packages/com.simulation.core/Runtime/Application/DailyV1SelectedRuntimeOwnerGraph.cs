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
    internal SpatialNetworkOwner SpatialNetwork { get; }

    private DailyV1SelectedRuntimeOwnerGraph(
        DailyV1EffectiveDefinitions definitions,
        List<DailyV1CityRuntimeOwners> cities,
        List<DailyV1NpcRuntimeOwners> npcs,
        SpatialNetworkOwner spatialNetwork)
    {
        Definitions = definitions;
        this.cities = cities.AsReadOnly();
        this.npcs = npcs.AsReadOnly();
        SpatialNetwork = spatialNetwork;
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreate(definitions, cityOwners, npcOwners, null, out graph);
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (definitions == null || cityOwners == null || npcOwners == null) return false;

        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        if (spatialNetwork != null)
        {
            foreach (SpatialLocationRuntime location in spatialNetwork.Locations)
                if (location == null || string.IsNullOrWhiteSpace(location.RuntimeId)
                    || !runtimeIds.Add(location.RuntimeId)) return false;
            foreach (SpatialRouteRuntime route in spatialNetwork.Routes)
                if (route == null || string.IsNullOrWhiteSpace(route.RuntimeId)
                    || !runtimeIds.Add(route.RuntimeId)) return false;
        }
        foreach (DailyV1CityRuntimeOwners owner in cityOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || string.IsNullOrWhiteSpace(owner.LocationRuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Cities.TryGetValue(owner.Definition.DefinitionId, out DailyV1CityDefinition city)
                || !ReferenceEquals(city, owner.Definition)
                || (spatialNetwork != null && !spatialNetwork.ContainsLocationId(owner.LocationRuntimeId))
                || !runtimeIds.Add(owner.RuntimeId)
                || !locationIds.Add(owner.LocationRuntimeId)
                || (spatialNetwork == null && !runtimeIds.Add(owner.LocationRuntimeId))) return false;
            cities.Add(owner);
        }
        foreach (DailyV1NpcRuntimeOwners owner in npcOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Npcs.TryGetValue(owner.Definition.DefinitionId, out DailyV1NpcDefinition npc)
                || !ReferenceEquals(npc, owner.Definition)
                || !runtimeIds.Add(owner.RuntimeId)
                || !MatchesCity(cities, owner.MerchantTradePlan?.OriginCityRuntimeId)
                || !MatchesCity(cities, owner.MerchantTradePlan?.TargetCityRuntimeId)
                || !MatchesCityLocation(cities, owner.Travel?.CurrentCityRuntimeId,
                    owner.Travel?.CurrentLocationRuntimeId)
                || !MatchesCityLocation(cities, owner.Travel?.DestinationCityRuntimeId,
                    owner.Travel?.DestinationLocationRuntimeId)
                || (spatialNetwork != null
                    && (!ContainsLocationWhenPresent(spatialNetwork, owner.Travel?.CurrentLocationRuntimeId)
                        || !ContainsLocationWhenPresent(spatialNetwork, owner.Travel?.DestinationLocationRuntimeId)))) return false;
            npcs.Add(owner);
        }

        graph = new DailyV1SelectedRuntimeOwnerGraph(definitions, cities, npcs, spatialNetwork);
        return true;
    }

    private static bool ContainsLocationWhenPresent(SpatialNetworkOwner spatialNetwork, string locationRuntimeId)
    {
        return locationRuntimeId == null
            || (!string.IsNullOrWhiteSpace(locationRuntimeId)
                && spatialNetwork.ContainsLocationId(locationRuntimeId));
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateFromExactOwners(cityOwners, npcOwners, null, out graph);
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
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

        return definitions != null && TryCreate(definitions, cities, npcs, spatialNetwork, out graph);
    }

    internal static bool TryCreateInitialCityOwner(
        string runtimeId,
        string locationRuntimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        MarketCounterpartyRuntime suppliedCounterparty,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1CityRuntimeOwners.TryCreateInitial(
                runtimeId, locationRuntimeId, definitions, definition, suppliedCounterparty,
                out DailyV1CityRuntimeOwners candidate)
            || !TryCreate(definitions, new[] { candidate }, Array.Empty<DailyV1NpcRuntimeOwners>(), out _))
            return false;

        owners = candidate;
        return true;
    }

    internal static bool TryCreateBoundCityOwner(
        string runtimeId,
        string locationRuntimeId,
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
                runtimeId, locationRuntimeId, definitions, definition, population, market, counterparty,
                populationEconomy, npcPresence, out DailyV1CityRuntimeOwners candidate)
            || !TryCreate(definitions, new[] { candidate }, Array.Empty<DailyV1NpcRuntimeOwners>(), out _))
            return false;

        owners = candidate;
        return true;
    }

    private static bool MatchesCityLocation(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        string cityRuntimeId,
        string locationRuntimeId)
    {
        if (cityRuntimeId == null) return true;
        if (string.IsNullOrWhiteSpace(cityRuntimeId) || string.IsNullOrWhiteSpace(locationRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners city in cities)
            if (string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
                return string.Equals(city.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal);
        return false;
    }

    private static bool MatchesCity(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        string cityRuntimeId)
    {
        if (cityRuntimeId == null) return true;
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners city in cities)
            if (string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static bool TryCreateInitialNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        out DailyV1NpcRuntimeOwners owners)
    {
        return TryCreateInitialNpcOwner(runtimeId, definitions, definition, initialMoney,
            string.IsNullOrWhiteSpace(runtimeId) ? null : new SpatialKnowledgeRuntime(runtimeId), out owners);
    }

    internal static bool TryCreateInitialNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        SpatialKnowledgeRuntime spatialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1NpcRuntimeOwners.TryCreateInitial(
                runtimeId, definitions, definition, initialMoney, spatialKnowledge,
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
        return TryCreateBoundNpcOwner(runtimeId, definitions, definition, statuses,
            inventory, moneyAccount, commercialKnowledge, null, out owners);
    }

    internal static bool TryCreateBoundNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        DailyV1NpcMerchantTradePlanOwner merchantTradePlan,
        out DailyV1NpcRuntimeOwners owners)
    {
        return TryCreateBoundNpcOwner(runtimeId, definitions, definition, statuses,
            inventory, moneyAccount, commercialKnowledge, merchantTradePlan,
            string.IsNullOrWhiteSpace(runtimeId) ? null : new SpatialKnowledgeRuntime(runtimeId), out owners);
    }

    internal static bool TryCreateBoundNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        DailyV1NpcMerchantTradePlanOwner merchantTradePlan,
        SpatialKnowledgeRuntime spatialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (spatialKnowledge == null) return false;
        if (merchantTradePlan == null
            && (definitions == null || !DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
                definitions, out merchantTradePlan))) return false;
        bool created = DailyV1NpcRuntimeOwners.TryCreateBound(
            runtimeId, definitions, definition, statuses, inventory, moneyAccount,
            commercialKnowledge, merchantTradePlan, spatialKnowledge,
            out DailyV1NpcRuntimeOwners candidate);
        // A bound NPC can be staged before its City owners are composed (notably
        // during detached P12 restore). Cross-owner City joins are validated by
        // the complete runtime graph after all City authorities are available.
        if (!created || candidate == null || !ReferenceEquals(candidate.Definitions, definitions))
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

    /// <summary>Installs a selected merchant plan into its exact NPC owner.</summary>
    internal bool TryInstallMerchantTradePlan(
        DailyV1NpcRuntimeOwners npc,
        DailyV1ItemDefinition item,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        float purchasePricePerItem,
        string originDecisionId)
    {
        if (npc == null || item == null || !ContainsExactNpc(npc)
            || !Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(admittedItem, item)
            || !ContainsCityId(originCityRuntimeId) || !ContainsCityId(targetCityRuntimeId)
            || plannedAmount <= 0 || float.IsNaN(purchasePricePerItem)
            || float.IsInfinity(purchasePricePerItem) || purchasePricePerItem < 0f)
            return false;

        return npc.MerchantTradePlan.TryInstallState(item, originCityRuntimeId,
            targetCityRuntimeId, plannedAmount, plannedAmount, purchasePricePerItem, 0, 0,
            originDecisionId);
    }

    /// <summary>Plans and installs the selected merchant's direct trade route.</summary>
    internal bool TryCommitMerchantTradeTravelPlan(
        DailyV1NpcRuntimeOwners npc,
        string targetCityRuntimeId,
        float utility,
        string originDecisionId,
        Func<bool> admitMutation,
        out float expectedCost)
    {
        expectedCost = -1f;
        if (npc == null || !ContainsExactNpc(npc) || SpatialNetwork == null
            || float.IsNaN(utility) || float.IsInfinity(utility) || utility < 0f
            || string.IsNullOrWhiteSpace(targetCityRuntimeId)
            || !string.Equals(targetCityRuntimeId,
                npc.MerchantTradePlan?.TargetCityRuntimeId, StringComparison.Ordinal)
            || !SpatialNetwork.TryGetLocation(npc.Travel?.CurrentLocationRuntimeId,
                out SpatialLocationRuntime origin)
            || !TryFindCity(targetCityRuntimeId, out DailyV1CityRuntimeOwners target)
            || !SpatialNetwork.TryGetLocation(target.LocationRuntimeId,
                out SpatialLocationRuntime destination)
            || !SpatialNetwork.TryGetSingleDirectRoute(origin, destination,
                out SpatialRouteRuntime route, out _)) return false;

        float cost = Math.Max(1, route.TravelDays)
            * (Definitions.EffectiveConfiguration?.Travel?.TravelCostPerDay ?? 1f);
        if (float.IsNaN(cost) || float.IsInfinity(cost) || cost < 0f
            || !npc.TravelPlan.CanCommit(destination.RuntimeId, target.RuntimeId,
                NpcTravelReason.Trade, utility, cost, originDecisionId)
            || (!npc.TravelPlan.Matches(destination.RuntimeId, target.RuntimeId,
                    NpcTravelReason.Trade, utility, cost, originDecisionId)
                && (admitMutation == null || !admitMutation()))
            || !npc.TravelPlan.TryCommit(destination.RuntimeId, target.RuntimeId,
                NpcTravelReason.Trade, utility, cost, originDecisionId)) return false;
        expectedCost = cost;
        return true;
    }

    private bool TryFindCity(string runtimeId, out DailyV1CityRuntimeOwners city)
    {
        city = null;
        for (int i = 0; i < cities.Count; i++)
            if (string.Equals(cities[i].RuntimeId, runtimeId, StringComparison.Ordinal))
            { city = cities[i]; return true; }
        return false;
    }

    private bool ContainsExactNpc(DailyV1NpcRuntimeOwners candidate)
    {
        for (int i = 0; i < npcs.Count; i++)
            if (ReferenceEquals(npcs[i], candidate)) return true;
        return false;
    }

    private bool ContainsCityId(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        for (int i = 0; i < cities.Count; i++)
            if (string.Equals(cities[i].RuntimeId, runtimeId, StringComparison.Ordinal)) return true;
        return false;
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
        return TryCreate(Definitions, cities, updatedNpcs, SpatialNetwork, out graph);
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

        return removed && TryCreate(Definitions, cities, updatedNpcs, SpatialNetwork, out graph);
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

    internal bool ContainsExactCityOwner(DailyV1CityRuntimeOwners candidate)
    {
        if (candidate == null || !ReferenceEquals(candidate.Definitions, Definitions)) return false;
        foreach (DailyV1CityRuntimeOwners owner in cities)
            if (ReferenceEquals(owner, candidate)) return true;
        return false;
    }

    internal bool ContainsExactNpcOwner(DailyV1NpcRuntimeOwners candidate)
    {
        if (candidate == null || !ReferenceEquals(candidate.Definitions, Definitions)) return false;
        foreach (DailyV1NpcRuntimeOwners owner in npcs)
            if (ReferenceEquals(owner, candidate)) return true;
        return false;
    }

    /// <summary>
    /// Prepares the ordinary, unkeyed open-market sale against the exact City
    /// and NPC authorities in this graph. The host supplies the P12 operation
    /// admission only when the prepared plan is ready to commit.
    /// </summary>
    internal bool TryPrepareOpenMarketSale(
        DailyV1NpcRuntimeOwners npc,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity,
        out DailyV1SelectedOpenMarketSalePlan plan,
        out DailyV1SelectedOpenMarketSaleFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedOpenMarketSaleFailure.InvalidInput;
        if (!ContainsExactNpcOwner(npc) || market == null)
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in cities)
        {
            if (candidate != null && ReferenceEquals(candidate.Market, market))
            {
                city = candidate;
                break;
            }
        }

        return DailyV1SelectedOpenMarketSalePlan.TryPrepare(
            this, npc, city, market, item, requestedQuantity, out plan, out failure);
    }

    /// <summary>Prepares an ordinary selected open-market purchase on exact Core owners.</summary>
    internal bool TryPrepareOpenMarketPurchase(
        DailyV1NpcRuntimeOwners npc,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity,
        bool allowAccountBacked,
        out DailyV1SelectedOpenMarketPurchasePlan plan,
        out DailyV1SelectedOpenMarketPurchaseFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedOpenMarketPurchaseFailure.InvalidInput;
        if (!ContainsExactNpcOwner(npc) || market == null) return false;
        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in cities)
            if (candidate != null && ReferenceEquals(candidate.Market, market))
            { city = candidate; break; }
        return DailyV1SelectedOpenMarketPurchasePlan.TryPrepare(
            this, npc, city, market, item, requestedQuantity, allowAccountBacked,
            out plan, out failure);
    }

    internal bool ContainsCityRuntimeLocation(string cityRuntimeId, string locationRuntimeId)
    {
        if (cityRuntimeId == null) return true;
        if (string.IsNullOrWhiteSpace(cityRuntimeId) || string.IsNullOrWhiteSpace(locationRuntimeId))
            return false;
        foreach (DailyV1CityRuntimeOwners city in cities)
            if (string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
                return string.Equals(city.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal);
        return false;
    }

    /// <summary>
    /// Commits one prepared travel/presence operation only when its NPC and all
    /// affected City authorities are exact members of this selected runtime.
    /// The host admission callback runs before any owner revision or value changes.
    /// </summary>
    internal bool TryCommitTravelTransition(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan,
        Func<bool> preCommitAdmission)
    {
        if (!ContainsExactNpcOwner(npc) || plan == null || !plan.IsPreparedFor(npc, this)) return false;
        return plan.TryCommit(preCommitAdmission);
    }

    /// <summary>
    /// Advances the existing P8 TravelParty owner in its captured store order.
    /// Core resolves each exact selected NPC owner, checks synchronized travel,
    /// and performs the travel, discovery, party-identity, and store-completion
    /// mutations. The two host callbacks are restricted to the established
    /// pre-commit P12/projection preflight and publishing a committed Core
    /// owner into the existing Unity projection before the next member is read.
    /// The event callback publishes the noncausal event read model at its
    /// existing position before active-party identities are cleared.
    /// </summary>
    internal bool TryAdvanceTravelParties(
        TravelPartyStore partyStore,
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool, bool> preCommitAdmission,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool> publishCommitted,
        Func<TravelPartyRuntime, bool> publishArrivalEvent,
        out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> arrivals)
    {
        arrivals = null;
        if (partyStore == null || preCommitAdmission == null || publishCommitted == null)
            return false;

        using (partyStore.EnterMutationWindow())
        {
            List<DailyV1SelectedTravelPartyArrivalResult> completed =
                new List<DailyV1SelectedTravelPartyArrivalResult>();
            List<TravelPartyRuntime> activeParties = new List<TravelPartyRuntime>(partyStore.ActiveParties);
            Dictionary<string, DailyV1NpcRuntimeOwners> npcsById =
                new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
            foreach (DailyV1NpcRuntimeOwners npc in npcs)
                if (npc == null || !npcsById.TryAdd(npc.RuntimeId, npc)) return false;

            foreach (TravelPartyRuntime party in activeParties)
            {
                if (party == null || !party.IsActive
                    || !TryResolveSynchronizedPartyMembers(party, npcsById,
                        out List<DailyV1NpcRuntimeOwners> members))
                    continue;

                bool startedToday = false;
                foreach (DailyV1NpcRuntimeOwners member in members)
                    startedToday |= member.Travel.TravelStartedToday;

                if (startedToday)
                {
                    List<DailyV1SelectedTravelTransitionPlan> clearPlans =
                        new List<DailyV1SelectedTravelTransitionPlan>(members.Count);
                    bool canClear = true;
                    foreach (DailyV1NpcRuntimeOwners member in members)
                    {
                        if (!DailyV1SelectedTravelTransitionPlan.TryClearTravelStartedToday(
                                member, out DailyV1SelectedTravelTransitionPlan clearPlan)
                            || !preCommitAdmission(member, clearPlan, false))
                        {
                            canClear = false;
                            break;
                        }
                        clearPlans.Add(clearPlan);
                    }
                    if (!canClear) continue;
                    foreach (DailyV1SelectedTravelTransitionPlan clearPlan in clearPlans)
                    {
                        DailyV1NpcRuntimeOwners member = clearPlan.Npc;
                        if (!TryCommitTravelTransition(member, clearPlan, null))
                            throw new InvalidOperationException(
                                "A preflighted TravelParty start-day flag could not commit.");
                        publishCommitted(member, clearPlan, false);
                    }
                    continue;
                }

                bool willArrive = members[0].Travel.TravelDaysRemaining <= 1;
                DailyV1CityRuntimeOwners destinationCity = null;
                string destinationCityId = members[0].Travel.DestinationCityRuntimeId;
                if (destinationCityId != null)
                {
                    foreach (DailyV1CityRuntimeOwners city in cities)
                        if (string.Equals(city.RuntimeId, destinationCityId, StringComparison.Ordinal))
                        {
                            destinationCity = city;
                            break;
                        }
                    if (destinationCity == null) continue;
                }

                if (willArrive && !partyStore.CanCommitMutations(1)) continue;

                List<DailyV1SelectedTravelTransitionPlan> advancePlans =
                    new List<DailyV1SelectedTravelTransitionPlan>(members.Count);
                int destinationPresenceAdds = 0;
                bool canAdvance = true;
                foreach (DailyV1NpcRuntimeOwners member in members)
                {
                    if (!DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
                            member, destinationCity,
                            out DailyV1SelectedTravelTransitionPlan advancePlan,
                            out bool memberArrives)
                        || memberArrives != willArrive
                        || !preCommitAdmission(member, advancePlan, willArrive))
                    {
                        canAdvance = false;
                        break;
                    }
                    if (willArrive && destinationCity != null
                        && !destinationCity.NpcPresence.Contains(member.RuntimeId))
                        destinationPresenceAdds++;
                    advancePlans.Add(advancePlan);
                }
                if (!canAdvance || (destinationCity != null
                    && !destinationCity.NpcPresence.CanApplyRevisionIncrements(destinationPresenceAdds)))
                    continue;

                bool allMembersAdvanced = true;
                foreach (DailyV1SelectedTravelTransitionPlan advancePlan in advancePlans)
                {
                    DailyV1NpcRuntimeOwners member = advancePlan.Npc;
                    if (!TryCommitTravelTransition(member, advancePlan, null))
                    {
                        allMembersAdvanced = false;
                        continue;
                    }
                    publishCommitted(member, advancePlan, willArrive);
                }
                if (!allMembersAdvanced || !willArrive) continue;

                foreach (DailyV1NpcRuntimeOwners member in members)
                    member.SpatialKnowledge.DiscoverLocation(party.DestinationLocationRuntimeId);

                bool eventRecorded = publishArrivalEvent == null || publishArrivalEvent(party);
                foreach (DailyV1NpcRuntimeOwners member in members)
                {
                    if (!DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
                            member, null, out DailyV1SelectedTravelTransitionPlan clearPartyPlan)
                        || !preCommitAdmission(member, clearPartyPlan, false)
                        || !TryCommitTravelTransition(member, clearPartyPlan, null))
                        throw new InvalidOperationException(
                            "A preflighted arrived TravelParty member could not clear its party identity.");
                    publishCommitted(member, clearPartyPlan, false);
                }

                if (!partyStore.Complete(party.TravelPartyId))
                    throw new InvalidOperationException(
                        "A preflighted TravelParty arrival could not complete its Core owner.");

                completed.Add(new DailyV1SelectedTravelPartyArrivalResult(
                    party, members.AsReadOnly(), eventRecorded));
            }

            arrivals = completed.AsReadOnly();
            return true;
        }
    }

    private static bool TryResolveSynchronizedPartyMembers(
        TravelPartyRuntime party,
        IReadOnlyDictionary<string, DailyV1NpcRuntimeOwners> npcsById,
        out List<DailyV1NpcRuntimeOwners> members)
    {
        members = new List<DailyV1NpcRuntimeOwners>();
        if (party == null || npcsById == null || party.MemberRuntimeIds == null
            || party.MemberRuntimeIds.Count == 0)
            return false;

        foreach (string runtimeId in party.MemberRuntimeIds)
        {
            if (string.IsNullOrWhiteSpace(runtimeId)
                || !npcsById.TryGetValue(runtimeId, out DailyV1NpcRuntimeOwners member)
                || member == null || member.Lifecycle.LifeState != NpcLifeState.Alive)
                return false;
            members.Add(member);
        }

        DailyV1NpcRuntimeOwners first = members[0];
        if (!first.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(first.Travel.DestinationLocationRuntimeId)
            || first.Travel.TravelDaysRemaining <= 0
            || !string.Equals(first.Travel.ActiveTravelPartyId, party.TravelPartyId, StringComparison.Ordinal)
            || !string.Equals(first.Travel.DestinationLocationRuntimeId,
                party.DestinationLocationRuntimeId, StringComparison.Ordinal)
            || !string.Equals(first.Travel.TravelOriginDecisionId,
                party.OriginDecisionId, StringComparison.Ordinal))
            return false;

        foreach (DailyV1NpcRuntimeOwners member in members)
            if (!member.Travel.IsTraveling
                || !string.Equals(member.Travel.DestinationLocationRuntimeId,
                    first.Travel.DestinationLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(member.Travel.DestinationCityRuntimeId,
                    first.Travel.DestinationCityRuntimeId, StringComparison.Ordinal)
                || member.Travel.TravelDaysRemaining != first.Travel.TravelDaysRemaining
                || !string.Equals(member.Travel.ActiveTravelPartyId,
                    party.TravelPartyId, StringComparison.Ordinal)
                || !string.Equals(member.Travel.TravelOriginDecisionId,
                    party.OriginDecisionId, StringComparison.Ordinal))
                return false;
        return true;
    }

    /// <summary>
    /// Commits the exact selected NPC action definition/payload to its Core
    /// owner slot after host admission. The host still projects its existing
    /// action object and records the P12 epoch after this authoritative write.
    /// </summary>
    internal bool TryCommitCurrentActionSlot(
        DailyV1NpcRuntimeOwners npc,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload,
        Func<bool> preCommitAdmission)
    {
        DailyV1NpcActionSlotOwner slot = npc?.ActionSlot;
        if (!ContainsExactNpcOwner(npc)
            || slot == null
            || !slot.CanSetCurrentAction(action, payload))
            return false;

        if (preCommitAdmission != null)
        {
            try
            {
                if (!preCommitAdmission()) return false;
            }
            catch
            {
                return false;
            }
        }

        // Admission executes on the runtime owner thread. The exact slot and
        // admitted payload were validated above, so no fallible preparation
        // remains after the boundary accepts the mutation.
        if (!slot.TrySetCurrentAction(action, payload)) return false;
        slot.NotifyCurrentActionMutationCommitted();
        return true;
    }

    /// <summary>
    /// Runs the selected solo-travel portion of one daily turn in stable
    /// RuntimeId order. Core selects eligible owners, prepares each existing
    /// travel plan, and installs it into the exact travel/presence owners. The
    /// host supplies a read-only party-membership snapshot, a compatibility
    /// boundary preflight, and a per-owner projection/invalidation publisher.
    /// Core chooses and commits each travel result; the publisher reconciles its
    /// Unity projection and P12 mutation boundary before the next owner is read.
    /// </summary>
    internal bool TryAdvanceSoloTravelDay(
        IEnumerable<DailyV1NpcRuntimeOwners> exactNpcOwners,
        IReadOnlyCollection<string> managedTravelPartyNpcRuntimeIds,
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool, bool> preCommitAdmission,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool> publishCommitted,
        Action<DailyV1SelectedSoloTravelDayResult> publishResult,
        out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> results)
    {
        results = null;
        if (!MatchesExactOwnerInstances(npcs, exactNpcOwners)
            || managedTravelPartyNpcRuntimeIds == null
            || preCommitAdmission == null
            || publishCommitted == null
            || publishResult == null)
            return false;

        HashSet<string> managedIds = new HashSet<string>(managedTravelPartyNpcRuntimeIds, StringComparer.Ordinal);
        List<DailyV1NpcRuntimeOwners> ordered = new List<DailyV1NpcRuntimeOwners>(npcs);
        ordered.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        List<DailyV1SelectedSoloTravelDayResult> completed = new List<DailyV1SelectedSoloTravelDayResult>();

        foreach (DailyV1NpcRuntimeOwners npc in ordered)
        {
            if (npc.Lifecycle.LifeState != NpcLifeState.Alive
                || !npc.Travel.IsTraveling
                || !string.IsNullOrWhiteSpace(npc.Travel.ActiveTravelPartyId)
                || managedIds.Contains(npc.RuntimeId))
                continue;

            if (npc.Travel.TravelStartedToday)
            {
                if (DailyV1SelectedTravelTransitionPlan.TryClearTravelStartedToday(
                        npc, out DailyV1SelectedTravelTransitionPlan clearStartedPlan))
                {
                    bool committed = preCommitAdmission(npc, clearStartedPlan, false)
                        && TryCommitTravelTransition(npc, clearStartedPlan, null);
                    if (committed) publishCommitted(npc, clearStartedPlan, false);
                    DailyV1SelectedSoloTravelDayResult result = new DailyV1SelectedSoloTravelDayResult(
                        npc, clearStartedPlan, false, committed, false,
                        npc.Travel.TravelOriginDecisionId);
                    completed.Add(result);
                    publishResult(result);
                }
                continue;
            }

            string originDecisionId = npc.Travel.TravelOriginDecisionId;
            DailyV1CityRuntimeOwners destination = null;
            if (npc.Travel.DestinationCityRuntimeId != null)
                foreach (DailyV1CityRuntimeOwners city in cities)
                    if (string.Equals(city.RuntimeId, npc.Travel.DestinationCityRuntimeId,
                            StringComparison.Ordinal))
                    {
                        destination = city;
                        break;
                    }

            DailyV1SelectedTravelTransitionPlan advancePlan = null;
            bool willArrive = false;
            bool committedAdvance = false;
            if (DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
                    npc, destination,
                    out advancePlan,
                    out willArrive))
            {
                committedAdvance = preCommitAdmission(npc, advancePlan, willArrive)
                    && TryCommitTravelTransition(npc, advancePlan, null);
                if (committedAdvance) publishCommitted(npc, advancePlan, willArrive);
            }

            DailyV1SelectedSoloTravelDayResult advanceResult = new DailyV1SelectedSoloTravelDayResult(
                npc, advancePlan, shouldReport: true, committedAdvance,
                arrived: willArrive && committedAdvance, originDecisionId);
            completed.Add(advanceResult);
            if (advanceResult.Arrived)
            {
                // Arrival discovery is part of the selected domain transition,
                // not a Unity-side consequence callback. TravelPlan has already
                // installed the authoritative location on this exact NPC owner;
                // the SpatialKnowledge owner retains its existing P12 mutation
                // boundary and revision behavior.
                npc.SpatialKnowledge.DiscoverLocation(npc.Travel.CurrentLocationRuntimeId);
            }
            publishResult(advanceResult);
        }

        results = completed.AsReadOnly();
        return true;
    }

    /// <summary>
    /// Executes the existing selected-City daily economy in its established
    /// global pass order: local material flow, ordinary production, then
    /// consumption and price refresh per City. The host callbacks are projections
    /// only; all selected mutable owners and definition reads remain in Core.
    /// Returns false only during preflight, before any owner is mutated.
    /// </summary>
    internal bool TrySimulateDailyEconomyDay(
        IEnumerable<DailyV1CityRuntimeOwners> exactCityOwners,
        long absoluteDay,
        string calendarVersion,
        Action<DailyV1CityRuntimeOwners, SimulationLogCategory, string> logOutput)
    {
        if (!MatchesExactOwnerInstances(cities, exactCityOwners)) return false;

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (!city.HasLocalDailyMaterialFlow) continue;
            LocalDailyMaterialFlowResult result = city.SimulateSelectedLocalDailyMaterialFlow(
                city.Definition.Values.materialFlowLocationId,
                absoluteDay,
                calendarVersion,
                logOutput == null ? null : (category, message) => logOutput(city, category, message));
            city.RecordSelectedMaterialFlow(result);
        }

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (!city.HasLocalDailyMaterialFlow)
                city.SimulateSelectedProductionDay(
                    logOutput == null ? null : (category, message) => logOutput(city, category, message));
        }

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (!city.HasLocalDailyMaterialFlow)
                city.SimulateSelectedConsumptionDay(
                    logOutput == null ? null : (category, message) => logOutput(city, category, message));
            city.UpdateSelectedMarketPrices(
                logOutput == null ? null : (category, message) => logOutput(city, category, message));
        }

        return true;
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
/// Immutable host-projection receipt for one selected NPC's solo-travel turn.
/// A receipt is emitted only after Core has selected and evaluated that NPC;
/// its plan is committed when <see cref="WasCommitted"/> is true.
/// </summary>
internal sealed class DailyV1SelectedTravelPartyArrivalResult
{
    internal TravelPartyRuntime Party { get; }
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Members { get; }
    internal bool EventRecorded { get; }

    internal DailyV1SelectedTravelPartyArrivalResult(
        TravelPartyRuntime party,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        bool eventRecorded)
    {
        Party = party ?? throw new ArgumentNullException(nameof(party));
        Members = members ?? throw new ArgumentNullException(nameof(members));
        EventRecorded = eventRecorded;
    }
}

internal sealed class DailyV1SelectedSoloTravelDayResult
{
    internal DailyV1NpcRuntimeOwners Npc { get; }
    internal DailyV1SelectedTravelTransitionPlan Plan { get; }
    internal bool ShouldReport { get; }
    internal bool WasCommitted { get; }
    internal bool Arrived { get; }
    internal string OriginDecisionId { get; }

    internal DailyV1SelectedSoloTravelDayResult(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan,
        bool shouldReport,
        bool wasCommitted,
        bool arrived,
        string originDecisionId)
    {
        Npc = npc ?? throw new ArgumentNullException(nameof(npc));
        Plan = plan;
        ShouldReport = shouldReport;
        WasCommitted = wasCommitted;
        Arrived = arrived;
        OriginDecisionId = originDecisionId;
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
    private readonly SpatialNetworkOwner spatialNetwork;
    private readonly List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
    private readonly List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
    private readonly Dictionary<string, List<DailyV1CityRuntimeOwners>> citiesByDefinition =
        new Dictionary<string, List<DailyV1CityRuntimeOwners>>(StringComparer.Ordinal);
    private readonly HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
    private bool built;

    internal DailyV1SelectedRuntimeOwnerGraphBuilder(
        DailyV1EffectiveDefinitions definitions,
        SpatialNetworkOwner spatialNetwork = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.spatialNetwork = spatialNetwork ?? new SpatialNetworkOwner();
    }

    internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities => cities.AsReadOnly();
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs => npcs.AsReadOnly();
    internal SpatialNetworkOwner SpatialNetwork => spatialNetwork;

    /// <summary>
    /// Performs the selected City's identity allocation and authoritative
    /// owner construction in Core. The callback only admits the corresponding
    /// host location projection; it receives the already allocated identity
    /// and must not allocate another one.
    /// </summary>
    internal bool TryCreateConfiguredCities(
        RuntimeIdAllocator runtimeIds,
        Func<SpatialLocationRuntime, bool> tryRegisterLocationIdentity,
        Action<SpatialLocationRuntime> rollbackLocationIdentity = null)
    {
        if (built || runtimeIds == null || tryRegisterLocationIdentity == null
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
            SpatialLocationRuntime location = new SpatialLocationRuntime(locationRuntimeId);
            if (!spatialNetwork.TryRegisterLocation(
                    location,
                    () => tryRegisterLocationIdentity(location),
                    () => rollbackLocationIdentity?.Invoke(location),
                    null))
                continue;

            string cityRuntimeId = runtimeIds.AllocateCityId();
            if (!TryAddInitialCity(cityRuntimeId, locationRuntimeId, definition, out _))
            {
                Action rollback = rollbackLocationIdentity == null
                    ? (Action)(() => { })
                    : () => rollbackLocationIdentity(location);
                if (!spatialNetwork.TryRollbackGenesisLocation(location, rollback))
                    throw new InvalidOperationException(
                        "A failed selected City genesis insertion could not roll back its spatial location.");
                return false;
            }
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
        SpatialLocationRuntime registeredLocationHere = null;
        if (!spatialNetwork.ContainsLocationId(locationRuntimeId))
        {
            SpatialLocationRuntime location = new SpatialLocationRuntime(locationRuntimeId);
            if (!spatialNetwork.TryRegisterLocation(location, () => true, () => { }, null))
            {
                runtimeIds.Remove(runtimeId);
                locationIds.Remove(locationRuntimeId);
                return false;
            }
            registeredLocationHere = location;
        }
        if (!DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
                runtimeId, locationRuntimeId, definitions, definition, null,
                out DailyV1CityRuntimeOwners candidate))
        {
            runtimeIds.Remove(runtimeId);
            locationIds.Remove(locationRuntimeId);
            if (registeredLocationHere != null
                && !spatialNetwork.TryRollbackGenesisLocation(registeredLocationHere, () => { }))
                throw new InvalidOperationException(
                    "A failed selected City owner creation could not roll back its spatial location.");
            return false;
        }

        cities.Add(candidate);
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
        if (built || owners == null || !cities.Remove(owners)) return false;
        locationIds.Remove(owners.LocationRuntimeId);
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
            if (!startingCity.NpcPresence.TryAdd(runtimeId)
                || !candidate.Travel.TryCommitState(
                    startingCity.LocationRuntimeId, startingCity.RuntimeId,
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
        if (city == null || !cities.Contains(city)) return false;
        runtimeId = city.LocationRuntimeId;
        return !string.IsNullOrWhiteSpace(runtimeId);
    }

    internal bool TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (built || !MatchesConfiguredCityRows() || !MatchesConfiguredNpcRows()
            || !HasConsistentInitialPresence()) return false;
        if (!DailyV1SelectedRuntimeOwnerGraph.TryCreate(definitions, cities, npcs, spatialNetwork, out graph))
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
            if (string.IsNullOrWhiteSpace(city.LocationRuntimeId)) return false;
            foreach (string npcRuntimeId in city.NpcPresence.OrderedNpcRuntimeIds)
            {
                if (!seenNpcIds.Add(npcRuntimeId)) return false;
                DailyV1NpcRuntimeOwners npc = npcs.Find(owner =>
                    string.Equals(owner.RuntimeId, npcRuntimeId, StringComparison.Ordinal));
                if (npc == null
                    || !string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(npc.Travel.CurrentLocationRuntimeId,
                        city.LocationRuntimeId, StringComparison.Ordinal))
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
