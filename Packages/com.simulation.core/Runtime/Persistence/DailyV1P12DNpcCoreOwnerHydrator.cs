using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Core-only values needed to hydrate the selected NPC owner vector from the
/// existing P12-D rows. This is an in-memory staging boundary, not a second
/// persisted schema. Unity supplies the detached row values and host identity
/// sets; Core resolves canonical definitions and creates the sole live owners.
/// </summary>
internal sealed class DailyV1P12DNpcCoreOwnerHydrationInput
{
    internal P12DNpcDRow DRow { get; }
    internal P12DNpcActionValue Action { get; }
    internal int TravelDaysRemaining { get; }
    internal int TravelDaysTotal { get; }
    internal string TravelRouteRuntimeId { get; }
    internal bool TravelStartedToday { get; }
    internal string TravelOriginDecisionId { get; }
    internal string ActiveTravelPartyId { get; }
    internal long TravelRevision { get; }
    internal P12DNpcMerchantPlanValue MerchantPlan { get; }
    internal P12DNpcTravelPlanValue TravelPlan { get; }
    internal IReadOnlyList<string> KnownLocationRuntimeIds { get; }
    internal IReadOnlyList<string> KnownRouteRuntimeIds { get; }
    internal long SpatialKnowledgeRevision { get; }
    internal IReadOnlyList<P12DNpcCommercialMarketValue> CommercialMarkets { get; }
    internal IReadOnlyList<P12DNpcCommercialLiquidityValue> CommercialLiquidity { get; }
    internal IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> CommercialShareReceipts { get; }
    internal long CommercialKnowledgeRevision { get; }
    internal IReadOnlyList<ExplorableSiteKnowledgeObservation> SiteObservations { get; }
    internal long SiteKnowledgeRevision { get; }
    internal IReadOnlyList<AdventureOppositionObservation> OppositionObservations { get; }
    internal IReadOnlyList<AdventureNotableItemObservation> NotableItemObservations { get; }
    internal IReadOnlyList<AdventureCommonResourceObservation> CommonResourceObservations { get; }
    internal IReadOnlyList<AdventureAccessObservation> AccessObservations { get; }
    internal long AdventureKnowledgeRevision { get; }

    internal DailyV1P12DNpcCoreOwnerHydrationInput(
        P12DNpcDRow dRow,
        P12DNpcActionValue action,
        int travelDaysRemaining,
        int travelDaysTotal,
        string travelRouteRuntimeId,
        bool travelStartedToday,
        string travelOriginDecisionId,
        string activeTravelPartyId,
        long travelRevision,
        P12DNpcMerchantPlanValue merchantPlan,
        P12DNpcTravelPlanValue travelPlan,
        IEnumerable<string> knownLocationRuntimeIds,
        IEnumerable<string> knownRouteRuntimeIds,
        long spatialKnowledgeRevision,
        IEnumerable<P12DNpcCommercialMarketValue> commercialMarkets,
        IEnumerable<P12DNpcCommercialLiquidityValue> commercialLiquidity,
        IEnumerable<DailyV1CommercialKnowledgeShareReceipt> commercialShareReceipts,
        long commercialKnowledgeRevision,
        IEnumerable<ExplorableSiteKnowledgeObservation> siteObservations,
        long siteKnowledgeRevision,
        IEnumerable<AdventureOppositionObservation> oppositionObservations,
        IEnumerable<AdventureNotableItemObservation> notableItemObservations,
        IEnumerable<AdventureCommonResourceObservation> commonResourceObservations,
        IEnumerable<AdventureAccessObservation> accessObservations,
        long adventureKnowledgeRevision)
    {
        DRow = dRow;
        Action = action;
        TravelDaysRemaining = travelDaysRemaining;
        TravelDaysTotal = travelDaysTotal;
        TravelRouteRuntimeId = travelRouteRuntimeId;
        TravelStartedToday = travelStartedToday;
        TravelOriginDecisionId = travelOriginDecisionId;
        ActiveTravelPartyId = activeTravelPartyId;
        TravelRevision = travelRevision;
        MerchantPlan = merchantPlan;
        TravelPlan = travelPlan;
        KnownLocationRuntimeIds = Copy(knownLocationRuntimeIds);
        KnownRouteRuntimeIds = Copy(knownRouteRuntimeIds);
        SpatialKnowledgeRevision = spatialKnowledgeRevision;
        CommercialMarkets = Copy(commercialMarkets);
        CommercialLiquidity = Copy(commercialLiquidity);
        CommercialShareReceipts = Copy(commercialShareReceipts);
        CommercialKnowledgeRevision = commercialKnowledgeRevision;
        SiteObservations = Copy(siteObservations);
        SiteKnowledgeRevision = siteKnowledgeRevision;
        OppositionObservations = Copy(oppositionObservations);
        NotableItemObservations = Copy(notableItemObservations);
        CommonResourceObservations = Copy(commonResourceObservations);
        AccessObservations = Copy(accessObservations);
        AdventureKnowledgeRevision = adventureKnowledgeRevision;
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) =>
        new List<T>(source ?? throw new ArgumentNullException(nameof(source))).AsReadOnly();
}

internal enum DailyV1P12DNpcCoreOwnerHydrationFailure
{
    None = 0,
    InvalidInput,
    InvalidNpcIdentity,
    InvalidNpcDefinition,
    InvalidStatusDefinition,
    InvalidInventoryItem,
    InvalidReference,
    UnsupportedValue
}

/// <summary>
/// Hydrates a selected P12-D NPC owner vector using only Core definitions,
/// persisted values, and stable runtime identities. Unity authoring objects
/// are resolved later by the adapter against these exact owner instances.
/// </summary>
internal static class DailyV1P12DNpcCoreOwnerHydrator
{
    internal static bool TryCreateHydrationInput(
        P12DNpcDRow d,
        P12DNpcFRow f,
        out DailyV1P12DNpcCoreOwnerHydrationInput input)
    {
        input = null;
        if (d == null || f == null
            || !string.Equals(d.RuntimeId, f.RuntimeId, StringComparison.Ordinal)
            || f.CommercialShareReceipts == null)
            return false;

        List<DailyV1CommercialKnowledgeShareReceipt> receipts =
            new List<DailyV1CommercialKnowledgeShareReceipt>(f.CommercialShareReceipts.Count);
        foreach (P12DNpcCommercialShareReceiptValue receipt in f.CommercialShareReceipts)
            receipts.Add(receipt == null ? null : new DailyV1CommercialKnowledgeShareReceipt(
                receipt.OperationIdentity, receipt.Fingerprint, receipt.BoundaryOccurrenceId,
                receipt.SenderRuntimeId, receipt.SenderPersonId, receipt.ReceiverRuntimeId,
                receipt.ReceiverPersonId, receipt.SourceSnapshotFingerprint,
                receipt.AppliedObservationCount));

        input = new DailyV1P12DNpcCoreOwnerHydrationInput(
            d, f.Action, f.TravelDaysRemaining, f.TravelDaysTotal,
            f.TravelRouteRuntimeId, f.TravelStartedToday, f.TravelOriginDecisionId,
            f.ActiveTravelPartyId, f.TravelStateRevision, f.MerchantPlan, f.TravelPlan,
            f.KnownLocationRuntimeIds, f.KnownRouteRuntimeIds, f.SpatialKnowledgeRevision,
            f.CommercialMarkets, f.CommercialLiquidity, receipts,
            f.CommercialKnowledgeRevision,
            f.SiteObservations, f.SiteKnowledgeRevision,
            f.OppositionObservations, f.NotableItemObservations,
            f.CommonResourceObservations, f.AccessObservations,
            f.AdventureKnowledgeRevision);
        return true;
    }

    internal static bool TryCreate(
        DailyV1P12DNpcCoreOwnerHydrationInput input,
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<string> cityRuntimeIds,
        IEnumerable<string> locationRuntimeIds,
        IEnumerable<string> routeRuntimeIds,
        IEnumerable<string> travelPartyIds,
        IEnumerable<string> npcRuntimeIds,
        out DailyV1NpcRuntimeOwners owners,
        out DailyV1P12DNpcCoreOwnerHydrationFailure failure)
    {
        owners = null;
        failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidInput;
        if (input == null || definitions == null || input.DRow == null
            || input.KnownLocationRuntimeIds == null || input.KnownRouteRuntimeIds == null
            || input.CommercialMarkets == null || input.CommercialLiquidity == null
            || input.CommercialShareReceipts == null
            || input.SiteObservations == null || input.OppositionObservations == null
            || input.NotableItemObservations == null || input.CommonResourceObservations == null
            || input.AccessObservations == null
            || !TryBuildIdSet(cityRuntimeIds, out HashSet<string> cities)
            || !TryBuildIdSet(locationRuntimeIds, out HashSet<string> locations)
            || !TryBuildIdSet(routeRuntimeIds, out HashSet<string> routes)
            || !TryBuildIdSet(travelPartyIds, out HashSet<string> parties)
            || !TryBuildIdSet(npcRuntimeIds, out HashSet<string> npcs))
            return false;

        P12DNpcDRow d = input.DRow;
        if (string.IsNullOrWhiteSpace(d.RuntimeId)
            || !npcs.Contains(d.RuntimeId)
            || (d.CurrentActionDefinitionId == null) != (input.Action == null)
            || (input.Action != null
                && !string.Equals(d.CurrentActionDefinitionId,
                    input.Action.ActionDefinitionId, StringComparison.Ordinal)))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidNpcIdentity;
            return false;
        }

        if (string.IsNullOrWhiteSpace(d.NpcDefinitionId)
            || !definitions.Npcs.TryGetValue(d.NpcDefinitionId, out DailyV1NpcDefinition definition)
            || definition == null
            || !string.Equals(definition.DefinitionId, d.NpcDefinitionId, StringComparison.Ordinal))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidNpcDefinition;
            return false;
        }

        List<DailyV1StatusDefinition> statuses = new List<DailyV1StatusDefinition>(d.CurrentStatusDefinitionIds.Count);
        foreach (string statusId in d.CurrentStatusDefinitionIds)
        {
            if (string.IsNullOrWhiteSpace(statusId)
                || !definitions.Statuses.TryGetValue(statusId, out DailyV1StatusDefinition status)
                || status == null
                || !string.Equals(status.DefinitionId, statusId, StringComparison.Ordinal))
            {
                failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidStatusDefinition;
                return false;
            }
            statuses.Add(status);
        }

        List<DailyV1InventoryItemState> inventoryRows =
            new List<DailyV1InventoryItemState>(d.InventoryRows.Count);
        foreach (P12DNpcInventoryRowValue row in d.InventoryRows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ItemDefinitionId)
                || !definitions.Items.TryGetValue(row.ItemDefinitionId, out DailyV1ItemDefinition item)
                || item == null
                || !string.Equals(item.DefinitionId, row.ItemDefinitionId, StringComparison.Ordinal)
                || row.Amount < 0 || float.IsNaN(row.AverageUnitCost)
                || float.IsInfinity(row.AverageUnitCost) || row.AverageUnitCost < 0f)
            {
                failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidInventoryItem;
                return false;
            }
            inventoryRows.Add(new DailyV1InventoryItemState(item, row.Amount, row.AverageUnitCost));
        }

        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        if (!inventory.TryCreatePreparedFromOwnerSnapshot(
                inventoryRows, d.InventoryRevision, out DailyV1InventoryOwner restoredInventory)
            || !MoneyAccountOwnerBridge.TryCreateFromOwnerSnapshot(
                d.MoneyBalance, d.MoneyRevision, out MoneyAccountRuntime moneyAccount))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.UnsupportedValue;
            return false;
        }

        if (!TryCreateMerchantPlan(input.MerchantPlan, definitions, cities,
                out DailyV1NpcMerchantTradePlanOwner merchantPlan)
            || !TryCreateCommercialKnowledge(input, definitions, locations,
                out DailyV1CommercialKnowledgeOwner commercialKnowledge)
            || !SpatialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
                d.RuntimeId, input.KnownLocationRuntimeIds, input.KnownRouteRuntimeIds,
                input.SpatialKnowledgeRevision, out SpatialKnowledgeRuntime spatialKnowledge)
            || input.KnownLocationRuntimeIds.Any(id => !locations.Contains(id))
            || input.KnownRouteRuntimeIds.Any(id => !routes.Contains(id)))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidReference;
            return false;
        }

        DailyV1ExplorableSiteKnowledgeOwner siteKnowledge =
            new DailyV1ExplorableSiteKnowledgeOwner(d.RuntimeId);
        DailyV1AdventureSiteIntelKnowledgeOwner adventureKnowledge =
            new DailyV1AdventureSiteIntelKnowledgeOwner(d.RuntimeId);
        if (!siteKnowledge.TryCreateInitialSnapshot(input.SiteObservations, input.SiteKnowledgeRevision)
            || !adventureKnowledge.TryCreateInitialSnapshot(
                input.OppositionObservations, input.NotableItemObservations,
                input.CommonResourceObservations, input.AccessObservations,
                input.AdventureKnowledgeRevision))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.UnsupportedValue;
            return false;
        }

        DailyV1ActionDefinition selectedAction = null;
        DailyV1NpcActionPayloadOwner actionPayload = null;
        if (input.Action != null
            && !TryCreateActionPayload(input.Action, definitions, cities, locations,
                routes, npcs, out selectedAction, out actionPayload))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidReference;
            return false;
        }

        P12DNpcTravelPlanValue travelPlan = input.TravelPlan;
        if (travelPlan == null
            || !ContainsNullable(locations, travelPlan.TargetLocationRuntimeId)
            || !ContainsNullable(cities, travelPlan.TargetCityRuntimeId)
            || !ContainsNullable(routes, input.TravelRouteRuntimeId)
            || !ContainsNullable(parties, input.ActiveTravelPartyId)
            || !ContainsNullable(cities, d.CurrentCityRuntimeId)
            || !ContainsNullable(locations, d.CurrentLocationRuntimeId)
            || !ContainsNullable(cities, d.DestinationCityRuntimeId)
            || !ContainsNullable(locations, d.DestinationLocationRuntimeId)
            || !ContainsNullable(cities, d.ResidenceSettlementRuntimeId))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.InvalidReference;
            return false;
        }

        DailyV1NpcRuntimeRestoreState restoreState = new DailyV1NpcRuntimeRestoreState
        {
            RuntimeId = d.RuntimeId,
            Definitions = definitions,
            Definition = definition,
            Statuses = statuses,
            Inventory = restoredInventory,
            MoneyAccount = moneyAccount,
            CommercialKnowledge = commercialKnowledge,
            MerchantTradePlan = merchantPlan,
            SpatialKnowledge = spatialKnowledge,
            ExplorableSiteKnowledge = siteKnowledge,
            AdventureSiteIntelKnowledge = adventureKnowledge,
            CurrentAction = selectedAction,
            CurrentActionPayload = actionPayload,
            ActionRevision = d.CurrentActionRevision,
            LifeState = d.LifeState,
            InjurySeverity = d.InjurySeverity,
            LifeStateRevision = d.LifeStateRevision,
            ResidenceSettlementRuntimeId = d.ResidenceSettlementRuntimeId,
            ResidenceRevision = d.ResidenceRevision,
            CrimeJusticeRevision = d.CrimeJusticeRevision,
            HiddenDaysRemaining = d.HiddenDaysRemaining,
            CurrentLocationRuntimeId = d.CurrentLocationRuntimeId,
            CurrentCityRuntimeId = d.CurrentCityRuntimeId,
            DestinationLocationRuntimeId = d.DestinationLocationRuntimeId,
            DestinationCityRuntimeId = d.DestinationCityRuntimeId,
            TravelDaysRemaining = input.TravelDaysRemaining,
            TravelDaysTotal = input.TravelDaysTotal,
            TravelRouteRuntimeId = input.TravelRouteRuntimeId,
            TravelStartedToday = input.TravelStartedToday,
            TravelOriginDecisionId = input.TravelOriginDecisionId,
            ActiveTravelPartyId = input.ActiveTravelPartyId,
            TravelRevision = input.TravelRevision,
            TravelPlanTargetLocationRuntimeId = travelPlan.TargetLocationRuntimeId,
            TravelPlanTargetCityRuntimeId = travelPlan.TargetCityRuntimeId,
            TravelPlanReason = travelPlan.Reason,
            TravelPlanUtility = travelPlan.Utility,
            TravelPlanExpectedCost = travelPlan.ExpectedCost,
            TravelPlanOriginDecisionId = travelPlan.OriginDecisionId,
            TravelPlanRevision = travelPlan.Revision
        };
        if (!DailyV1NpcRuntimeOwners.TryCreateRestoredP12DOwner(restoreState, out owners))
        {
            failure = DailyV1P12DNpcCoreOwnerHydrationFailure.UnsupportedValue;
            return false;
        }

        failure = DailyV1P12DNpcCoreOwnerHydrationFailure.None;
        return true;
    }

    private static bool TryCreateMerchantPlan(
        P12DNpcMerchantPlanValue value,
        DailyV1EffectiveDefinitions definitions,
        HashSet<string> cities,
        out DailyV1NpcMerchantTradePlanOwner owner)
    {
        owner = null;
        if (value == null
            || !ContainsNullable(cities, value.OriginCityRuntimeId)
            || !ContainsNullable(cities, value.TargetCityRuntimeId)) return false;
        DailyV1ItemDefinition item = null;
        if (value.ItemDefinitionId != null
            && (!definitions.Items.TryGetValue(value.ItemDefinitionId, out item)
                || item == null
                || !string.Equals(item.DefinitionId, value.ItemDefinitionId, StringComparison.Ordinal)))
            return false;
        return DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
            definitions, item, value.OriginCityRuntimeId, value.TargetCityRuntimeId,
            value.PlannedAmount, value.RawRemainingAmount, value.PurchasePricePerItem,
            value.WaitDaysAtDestination, value.PendingTravelDays, value.OriginDecisionId,
            value.Revision, out owner);
    }

    private static bool TryCreateCommercialKnowledge(
        DailyV1P12DNpcCoreOwnerHydrationInput input,
        DailyV1EffectiveDefinitions definitions,
        HashSet<string> locations,
        out DailyV1CommercialKnowledgeOwner owner)
    {
        owner = null;
        List<DailyV1CommercialMarketObservation> markets =
            new List<DailyV1CommercialMarketObservation>(input.CommercialMarkets.Count);
        foreach (P12DNpcCommercialMarketValue value in input.CommercialMarkets)
        {
            if (value == null || !ContainsNullable(locations, value.LocationRuntimeId)
                || string.IsNullOrWhiteSpace(value.ItemDefinitionId)
                || !definitions.Items.TryGetValue(value.ItemDefinitionId, out DailyV1ItemDefinition item)
                || item == null || !TryMapSource(value.Source, out DailyV1CommercialKnowledgeSource source))
                return false;
            try
            {
                markets.Add(new DailyV1CommercialMarketObservation(
                    value.LocationRuntimeId, item, value.ObservedPrice, value.ObservedStock,
                    value.ObservedDay, value.ReceivedDay, source, value.SourceRuntimeId));
            }
            catch (ArgumentException) { return false; }
        }

        List<DailyV1CommercialLiquidityObservation> liquidity =
            new List<DailyV1CommercialLiquidityObservation>(input.CommercialLiquidity.Count);
        foreach (P12DNpcCommercialLiquidityValue value in input.CommercialLiquidity)
        {
            if (value == null || !ContainsNullable(locations, value.LocationRuntimeId)
                || !TryMapSource(value.Source, out DailyV1CommercialKnowledgeSource source))
                return false;
            try
            {
                liquidity.Add(new DailyV1CommercialLiquidityObservation(
                    value.LocationRuntimeId, value.Mode, value.PurchasingPower,
                    value.ObservedDay, value.ReceivedDay, source, value.SourceRuntimeId));
            }
            catch (ArgumentException) { return false; }
        }

        return DailyV1CommercialKnowledgeOwner.TryCreateFromOwnerSnapshot(
            definitions, markets, liquidity, input.CommercialShareReceipts,
            input.CommercialKnowledgeRevision, out owner);
    }

    private static bool TryCreateActionPayload(
        P12DNpcActionValue value,
        DailyV1EffectiveDefinitions definitions,
        HashSet<string> cities,
        HashSet<string> locations,
        HashSet<string> routes,
        HashSet<string> npcs,
        out DailyV1ActionDefinition action,
        out DailyV1NpcActionPayloadOwner payload)
    {
        action = null;
        payload = null;
        if (value == null || string.IsNullOrWhiteSpace(value.ActionDefinitionId)
            || !definitions.Actions.TryGetValue(value.ActionDefinitionId, out action)
            || action == null
            || !string.Equals(action.DefinitionId, value.ActionDefinitionId, StringComparison.Ordinal)
            || !ContainsNullable(npcs, value.TargetNpcRuntimeId)
            || !ContainsNullable(cities, value.TargetCityRuntimeId)) return false;

        DailyV1ItemDefinition targetItem = null;
        if (value.TargetItemDefinitionId != null
            && (!definitions.Items.TryGetValue(value.TargetItemDefinitionId, out targetItem)
                || targetItem == null
                || !string.Equals(targetItem.DefinitionId, value.TargetItemDefinitionId, StringComparison.Ordinal)))
            return false;

        if (!TryValidateActionEvidence(value, definitions, locations, routes)
            || !DailyV1NpcActionPayloadOwner.TryCreate(
                definitions, action, value.TargetNpcRuntimeId, value.TargetCityRuntimeId,
                targetItem, value.Amount, value.ExpectedUnitPrice, value.SuccessChanceMultiplier,
                value.TravelReason, value.ExpectedNetValue, value.OriginDecisionId,
                value.StableOccurrenceKey, value.CommercialDecisionEvidence,
                value.CommercialScoutingEvidence, out payload)) return false;
        return true;
    }

    private static bool TryValidateActionEvidence(
        P12DNpcActionValue value,
        DailyV1EffectiveDefinitions definitions,
        HashSet<string> locations,
        HashSet<string> routes)
    {
        CommercialDecisionEvidence decision = value.CommercialDecisionEvidence;
        if (decision != null
            && (string.IsNullOrWhiteSpace(decision.ItemDefinitionId)
                || !definitions.Items.ContainsKey(decision.ItemDefinitionId)
                || !ContainsNullable(locations, decision.CurrentLocationRuntimeId)
                || !ContainsNullable(locations, decision.TradeOriginLocationRuntimeId)
                || !ContainsNullable(locations, decision.TradeDestinationLocationRuntimeId)
                || !ContainsNullable(locations, decision.PreviousDestinationLocationRuntimeId)
                || !ValidateObservationEvidence(decision.OriginObservation, locations)
                || !ValidateObservationEvidence(decision.DestinationObservation, locations)
                || (decision.DestinationLiquidityObservation != null
                    && !ContainsNullable(locations, decision.DestinationLiquidityObservation.LocationRuntimeId))))
            return false;

        CommercialScoutingEvidence scouting = value.CommercialScoutingEvidence;
        return scouting == null
            || (ContainsNullable(locations, scouting.TargetLocationRuntimeId)
                && ContainsNullable(routes, scouting.KnownRouteRuntimeId));
    }

    private static bool ValidateObservationEvidence(
        CommercialObservationEvidence value, HashSet<string> locations) =>
        value == null || ContainsNullable(locations, value.LocationRuntimeId);

    private static bool TryMapSource(
        CommercialKnowledgeSource source,
        out DailyV1CommercialKnowledgeSource selected)
    {
        switch (source)
        {
            case CommercialKnowledgeSource.DirectObservation:
                selected = DailyV1CommercialKnowledgeSource.DirectObservation;
                return true;
            case CommercialKnowledgeSource.InitialScenarioKnowledge:
                selected = DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge;
                return true;
            case CommercialKnowledgeSource.SharedByNpc:
                selected = DailyV1CommercialKnowledgeSource.SharedByNpc;
                return true;
            default:
                selected = default;
                return false;
        }
    }

    private static bool TryBuildIdSet(IEnumerable<string> values, out HashSet<string> result)
    {
        result = new HashSet<string>(StringComparer.Ordinal);
        if (values == null) return false;
        foreach (string value in values)
            if (string.IsNullOrWhiteSpace(value) || !result.Add(value)) return false;
        return true;
    }

    private static bool ContainsNullable(HashSet<string> values, string id) =>
        id == null || (!string.IsNullOrWhiteSpace(id) && values != null && values.Contains(id));
}

internal enum DailyV1P12DNpcCoreOwnerExportFailure
{
    None,
    InvalidInput,
    InvalidOwner,
    UnsupportedValue
}

/// <summary>
/// Projects detached P12-D/F NPC rows from the exact selected Core NPC owner.
/// When a complete selected graph is available, membership and definition
/// identity are checked against it. Mixed legacy rosters may have no complete
/// graph; their caller still validates the selected census witness identities
/// and projections while persisted values come from the live Core owners.
/// </summary>
internal static class DailyV1P12DNpcCoreOwnerExporter
{
    internal static bool TryCreate(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        PersonRuntime boundPerson,
        out P12DNpcDRow dRow,
        out P12DNpcFRow fRow,
        out DailyV1P12DNpcCoreOwnerExportFailure failure)
    {
        dRow = null;
        fRow = null;
        failure = DailyV1P12DNpcCoreOwnerExportFailure.InvalidInput;
        if (npc == null
            || (graph != null && (!graph.ContainsExactNpcOwner(npc)
                || !ReferenceEquals(graph.Definitions, npc.Definitions)))
            || npc.Definition == null || npc.Status == null || npc.Inventory == null
            || npc.MoneyAccount == null || npc.Residence == null
            || npc.Lifecycle == null || npc.ActionSlot == null || npc.Travel == null
            || npc.TravelPlan == null || npc.HiddenState == null
            || npc.MerchantTradePlan == null || npc.CommercialKnowledge == null
            || npc.SpatialKnowledge == null || npc.ExplorableSiteKnowledge == null
            || npc.AdventureSiteIntelKnowledge == null
            || npc.LocalTopologyKnowledgeEmptyOwner == null
            || !string.Equals(npc.Residence.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(npc.Travel.NpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(npc.SpatialKnowledge.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(npc.ExplorableSiteKnowledge.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !npc.LocalTopologyKnowledgeEmptyOwner.MatchesRuntimeId(npc.RuntimeId)
            || !string.Equals(npc.AdventureSiteIntelKnowledge.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1P12DNpcCoreOwnerExportFailure.InvalidOwner;
            return false;
        }

        MoneyAccountRuntime account = npc.MoneyAccount;
        DailyV1NpcTravelOwner travel = npc.Travel;
        DailyV1NpcTravelPlanOwner travelPlan = npc.TravelPlan;
        DailyV1NpcMerchantTradePlanOwner merchantPlan = npc.MerchantTradePlan;
        DailyV1CommercialKnowledgeOwner commercial = npc.CommercialKnowledge;
        DailyV1ExplorableSiteKnowledgeOwner site = npc.ExplorableSiteKnowledge;
        DailyV1AdventureSiteIntelKnowledgeOwner adventure = npc.AdventureSiteIntelKnowledge;
        if (npc.Inventory.Revision < 0L || account.Revision < 0L
            || float.IsNaN(account.Balance) || float.IsInfinity(account.Balance) || account.Balance < 0f
            || npc.Lifecycle.LifeStateRevision < 0L || npc.Residence.Revision < 0L
            || npc.CrimeJusticeRevision < 0L || npc.ActionSlot.Revision < 0L
            || travel.Revision < 0L || travelPlan.Revision < 0L || merchantPlan.Revision < 0L
            || commercial.Revision < 0L || site.Revision < 0L
            || adventure.Revision < 0L || npc.SpatialKnowledge.Revision < 0L
            || npc.HiddenState.RemainingDays < 0
            || travel.TravelDaysRemaining < 0 || travel.TravelDaysTotal < 0
            || (travel.TravelDaysTotal > 0 && travel.TravelDaysRemaining > travel.TravelDaysTotal)
            || npc.LocalTopologyKnowledgeEmptyOwner.Revision != 0L
            || npc.LocalTopologyKnowledgeEmptyOwner.PlaceObservationCount != 0
            || npc.LocalTopologyKnowledgeEmptyOwner.ConnectionObservationCount != 0
            || !Enum.IsDefined(typeof(NpcLifeState), npc.Lifecycle.LifeState)
            || !NpcInjuryRules.IsValid(npc.Lifecycle.InjurySeverity)
            || !npc.Status.TryCopyDefinitionIds(new List<string>())
            || !npc.ActionSlot.TryCapturePayload(out DailyV1NpcActionPayloadSnapshot actionSnapshot))
        {
            failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
            return false;
        }

        List<string> statusIds = new List<string>();
        if (!npc.Status.TryCopyDefinitionIds(statusIds))
        {
            failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
            return false;
        }

        List<P12DNpcInventoryRowValue> inventoryRows =
            new List<P12DNpcInventoryRowValue>(npc.Inventory.Items.Count);
        HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1InventoryItemState item in npc.Inventory.Items)
        {
            if (item == null || item.Item == null || string.IsNullOrWhiteSpace(item.Item.DefinitionId)
                || !npc.Definitions.Items.TryGetValue(item.Item.DefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(admittedItem, item.Item)
                || item.Amount < 0 || float.IsNaN(item.AverageUnitCost)
                || float.IsInfinity(item.AverageUnitCost) || item.AverageUnitCost < 0f
                || !itemIds.Add(item.Item.DefinitionId))
            {
                failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
                return false;
            }
            inventoryRows.Add(new P12DNpcInventoryRowValue(
                item.Item.DefinitionId, item.Amount, item.AverageUnitCost));
        }

        P12DNpcActionValue actionValue = null;
        if (actionSnapshot != null)
        {
            actionValue = new P12DNpcActionValue(
                actionSnapshot.ActionDefinitionId,
                actionSnapshot.TargetNpcRuntimeId,
                actionSnapshot.TargetCityRuntimeId,
                actionSnapshot.TargetItemDefinitionId,
                actionSnapshot.Amount,
                actionSnapshot.ExpectedUnitPrice,
                actionSnapshot.SuccessChanceMultiplier,
                actionSnapshot.TravelReason,
                actionSnapshot.ExpectedNetValue,
                actionSnapshot.OriginDecisionId,
                actionSnapshot.StableOccurrenceKey,
                actionSnapshot.CommercialDecisionEvidence,
                actionSnapshot.CommercialScoutingEvidence);
        }

        P12DNpcTravelPlanValue travelPlanValue = new P12DNpcTravelPlanValue(
            travelPlan.TargetLocationRuntimeId,
            travelPlan.TargetCityRuntimeId,
            travelPlan.Reason,
            travelPlan.Utility,
            travelPlan.ExpectedCost,
            travelPlan.OriginDecisionId,
            travelPlan.Revision);
        P12DNpcMerchantPlanValue merchantPlanValue = new P12DNpcMerchantPlanValue(
            merchantPlan.ItemDefinition?.DefinitionId,
            merchantPlan.OriginCityRuntimeId,
            merchantPlan.TargetCityRuntimeId,
            merchantPlan.PlannedAmount,
            merchantPlan.RawRemainingAmount,
            merchantPlan.PurchasePricePerItem,
            merchantPlan.WaitDaysAtDestination,
            merchantPlan.PendingTravelDays,
            merchantPlan.OriginDecisionId,
            merchantPlan.Revision);

        if (!commercial.TryCopyOwnerSnapshot(
                out IReadOnlyList<DailyV1CommercialMarketObservation> markets,
                out IReadOnlyList<DailyV1CommercialLiquidityObservation> liquidity,
                out IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> receipts)
            || markets == null || liquidity == null || receipts == null)
        {
            failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
            return false;
        }

        List<P12DNpcCommercialMarketValue> marketValues =
            new List<P12DNpcCommercialMarketValue>(markets.Count);
        foreach (DailyV1CommercialMarketObservation observation in markets)
        {
            if (observation == null || observation.ItemDefinition == null
                || !npc.Definitions.Items.TryGetValue(observation.ItemDefinitionId,
                    out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(admittedItem, observation.ItemDefinition)
                || !Enum.IsDefined(typeof(DailyV1CommercialKnowledgeSource), observation.Source))
            {
                failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
                return false;
            }
            marketValues.Add(new P12DNpcCommercialMarketValue(
                observation.LocationRuntimeId, observation.ItemDefinitionId,
                observation.ObservedPrice, observation.ObservedStock,
                observation.ObservedDay, observation.ReceivedDay,
                (CommercialKnowledgeSource)(int)observation.Source, observation.SourceRuntimeId));
        }

        List<P12DNpcCommercialLiquidityValue> liquidityValues =
            new List<P12DNpcCommercialLiquidityValue>(liquidity.Count);
        foreach (DailyV1CommercialLiquidityObservation observation in liquidity)
        {
            if (observation == null
                || !Enum.IsDefined(typeof(DailyV1CommercialKnowledgeSource), observation.Source))
            {
                failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
                return false;
            }
            liquidityValues.Add(new P12DNpcCommercialLiquidityValue(
                observation.LocationRuntimeId, observation.LiquidityMode,
                observation.ObservedPurchasingPower, observation.ObservedDay,
                observation.ReceivedDay,
                (CommercialKnowledgeSource)(int)observation.Source, observation.SourceRuntimeId));
        }

        List<P12DNpcCommercialShareReceiptValue> receiptValues =
            new List<P12DNpcCommercialShareReceiptValue>(receipts.Count);
        foreach (DailyV1CommercialKnowledgeShareReceipt receipt in receipts)
        {
            if (receipt == null)
            {
                receiptValues.Add(null);
                continue;
            }
            if (!P12DNpcCommercialShareReceiptValue.TryCreateFromOwnerSnapshot(
                    receipt.OperationIdentity, receipt.Fingerprint, receipt.BoundaryOccurrenceId,
                    receipt.SenderRuntimeId, receipt.SenderPersonId, receipt.ReceiverRuntimeId,
                    receipt.ReceiverPersonId, receipt.SourceSnapshotFingerprint,
                    receipt.AppliedObservationCount,
                    out P12DNpcCommercialShareReceiptValue detachedReceipt))
            {
                failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
                return false;
            }
            receiptValues.Add(detachedReceipt);
        }

        if (!site.TryCopyOwnerSnapshot(
                out IReadOnlyList<ExplorableSiteKnowledgeObservation> siteSnapshot,
                out long siteRevision)
            || siteSnapshot == null)
        {
            failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
            return false;
        }

        List<ExplorableSiteKnowledgeObservation> siteObservations =
            new List<ExplorableSiteKnowledgeObservation>(siteSnapshot.Count);
        foreach (ExplorableSiteKnowledgeObservation observation in siteSnapshot)
        {
            if (observation == null)
            {
                failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
                return false;
            }
            siteObservations.Add(new ExplorableSiteKnowledgeObservation(
                observation.SiteRuntimeId, observation.LocationRuntimeId,
                observation.ObservedDay, observation.ReceivedDay, observation.Source));
        }

        if (!adventure.TryCopyOwnerSnapshot(
                out IReadOnlyList<AdventureOppositionObservation> opposition,
                out IReadOnlyList<AdventureNotableItemObservation> notableItems,
                out IReadOnlyList<AdventureCommonResourceObservation> resources,
                out IReadOnlyList<AdventureAccessObservation> access,
                out long adventureRevision)
            || opposition == null || notableItems == null || resources == null || access == null
            || npc.SpatialKnowledge.KnownLocationRuntimeIds == null
            || npc.SpatialKnowledge.KnownRouteRuntimeIds == null)
        {
            failure = DailyV1P12DNpcCoreOwnerExportFailure.UnsupportedValue;
            return false;
        }

        PersonRuntime person = boundPerson;
        string personIdValue = null;
        string residenceId = npc.Residence.SettlementRuntimeId;
        if (person != null && (person.PersonId == null
            || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal)))
        {
            failure = DailyV1P12DNpcCoreOwnerExportFailure.InvalidOwner;
            return false;
        }
        if (graph?.PersonStore != null)
        {
            bool hasGraphPerson = graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                npc.RuntimeId, out PersonRuntime graphPerson);
            if ((person != null && (!hasGraphPerson || !ReferenceEquals(person, graphPerson)))
                || (person == null && hasGraphPerson))
            {
                failure = DailyV1P12DNpcCoreOwnerExportFailure.InvalidOwner;
                return false;
            }
            person = graphPerson;
        }
        if (person != null)
        {
            personIdValue = person.PersonId.Value;
            residenceId = person.ResidenceSettlementRuntimeId;
        }

        long residenceRevision = person != null
            ? PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person)
            : npc.Residence.Revision;
        dRow = new P12DNpcDRow(
            npc.RuntimeId,
            npc.Definition.DefinitionId,
            personIdValue,
            residenceId,
            npc.Lifecycle.LifeState,
            npc.Lifecycle.InjurySeverity,
            statusIds,
            npc.HiddenState.RemainingDays,
            npc.ActionSlot.CurrentAction?.DefinitionId,
            travel.CurrentCityRuntimeId,
            travel.CurrentLocationRuntimeId,
            travel.DestinationCityRuntimeId,
            travel.DestinationLocationRuntimeId,
            inventoryRows,
            npc.Inventory.Revision,
            account.Balance,
            account.Revision,
            npc.Lifecycle.LifeStateRevision,
            residenceRevision,
            npc.CrimeJusticeRevision,
            npc.ActionSlot.Revision);

        fRow = new P12DNpcFRow(
            npc.RuntimeId,
            actionValue,
            travel.TravelDaysRemaining,
            travel.TravelDaysTotal,
            travel.TravelRouteRuntimeId,
            travel.TravelStartedToday,
            travel.TravelOriginDecisionId,
            travel.ActiveTravelPartyId,
            travel.Revision,
            merchantPlanValue,
            travelPlanValue,
            siteObservations,
            siteRevision,
            new List<string>(npc.SpatialKnowledge.KnownLocationRuntimeIds),
            new List<string>(npc.SpatialKnowledge.KnownRouteRuntimeIds),
            npc.SpatialKnowledge.Revision,
            opposition,
            notableItems,
            resources,
            access,
            adventureRevision,
            marketValues,
            liquidityValues,
            receiptValues,
            commercial.Revision);

        failure = DailyV1P12DNpcCoreOwnerExportFailure.None;
        return true;
    }
}
