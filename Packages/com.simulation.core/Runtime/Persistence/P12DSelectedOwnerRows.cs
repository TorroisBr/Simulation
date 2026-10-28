using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal sealed class P12DNpcInventoryRowValue
{
    internal string ItemDefinitionId { get; }
    internal int Amount { get; }
    internal float AverageUnitCost { get; }

    internal P12DNpcInventoryRowValue(string itemDefinitionId, int amount, float averageUnitCost)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        AverageUnitCost = averageUnitCost;
    }
}

internal sealed class P12DNpcDRow
{
    internal string RuntimeId { get; }
    internal string NpcDefinitionId { get; }
    internal string PersonIdValue { get; }
    internal string ResidenceSettlementRuntimeId { get; }
    internal NpcLifeState LifeState { get; }
    internal NpcInjurySeverity InjurySeverity { get; }
    internal IReadOnlyList<string> CurrentStatusDefinitionIds { get; }
    internal int HiddenDaysRemaining { get; }
    internal string CurrentActionDefinitionId { get; }
    internal string CurrentCityRuntimeId { get; }
    internal string CurrentLocationRuntimeId { get; }
    internal string DestinationCityRuntimeId { get; }
    internal string DestinationLocationRuntimeId { get; }
    internal IReadOnlyList<P12DNpcInventoryRowValue> InventoryRows { get; }
    internal long InventoryRevision { get; }
    internal float MoneyBalance { get; }
    internal long MoneyRevision { get; }
    internal long LifeStateRevision { get; }
    internal long ResidenceRevision { get; }
    internal long CrimeJusticeRevision { get; }
    internal long CurrentActionRevision { get; }

    internal P12DNpcDRow(string runtimeId, string npcDefinitionId, string personIdValue,
        string residenceSettlementRuntimeId, NpcLifeState lifeState, NpcInjurySeverity injurySeverity,
        IEnumerable<string> currentStatusDefinitionIds, int hiddenDaysRemaining,
        string currentActionDefinitionId, string currentCityRuntimeId, string currentLocationRuntimeId,
        string destinationCityRuntimeId, string destinationLocationRuntimeId,
        IEnumerable<P12DNpcInventoryRowValue> inventoryRows, long inventoryRevision,
        float moneyBalance, long moneyRevision, long lifeStateRevision, long residenceRevision,
        long crimeJusticeRevision, long currentActionRevision)
    {
        RuntimeId = runtimeId;
        NpcDefinitionId = npcDefinitionId;
        PersonIdValue = personIdValue;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        LifeState = lifeState;
        InjurySeverity = injurySeverity;
        CurrentStatusDefinitionIds = Copy(currentStatusDefinitionIds);
        HiddenDaysRemaining = hiddenDaysRemaining;
        CurrentActionDefinitionId = currentActionDefinitionId;
        CurrentCityRuntimeId = currentCityRuntimeId;
        CurrentLocationRuntimeId = currentLocationRuntimeId;
        DestinationCityRuntimeId = destinationCityRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        InventoryRows = Copy(inventoryRows);
        InventoryRevision = inventoryRevision;
        MoneyBalance = moneyBalance;
        MoneyRevision = moneyRevision;
        LifeStateRevision = lifeStateRevision;
        ResidenceRevision = residenceRevision;
        CrimeJusticeRevision = crimeJusticeRevision;
        CurrentActionRevision = currentActionRevision;
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) =>
        new ReadOnlyCollection<T>(new List<T>(source ?? throw new ArgumentNullException(nameof(source))));
}

/// <summary>Detached P12-D action values consumed by the selected Core owner.</summary>
internal sealed class P12DNpcActionValue
{
    internal string ActionDefinitionId { get; }
    internal string TargetNpcRuntimeId { get; }
    internal string TargetCityRuntimeId { get; }
    internal string TargetItemDefinitionId { get; }
    internal int Amount { get; }
    internal float ExpectedUnitPrice { get; }
    internal float SuccessChanceMultiplier { get; }
    internal NpcTravelReason TravelReason { get; }
    internal float ExpectedNetValue { get; }
    internal string OriginDecisionId { get; }
    internal string StableOccurrenceKey { get; }
    internal CommercialDecisionEvidence CommercialDecisionEvidence { get; }
    internal CommercialScoutingEvidence CommercialScoutingEvidence { get; }

    internal P12DNpcActionValue(string actionDefinitionId, string targetNpcRuntimeId,
        string targetCityRuntimeId, string targetItemDefinitionId, int amount,
        float expectedUnitPrice, float successChanceMultiplier, NpcTravelReason travelReason,
        float expectedNetValue, string originDecisionId, string stableOccurrenceKey,
        CommercialDecisionEvidence commercialDecisionEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence)
    {
        ActionDefinitionId = actionDefinitionId;
        TargetNpcRuntimeId = targetNpcRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        TargetItemDefinitionId = targetItemDefinitionId;
        Amount = amount;
        ExpectedUnitPrice = expectedUnitPrice;
        SuccessChanceMultiplier = successChanceMultiplier;
        TravelReason = travelReason;
        ExpectedNetValue = expectedNetValue;
        OriginDecisionId = originDecisionId;
        StableOccurrenceKey = stableOccurrenceKey;
        CommercialDecisionEvidence = commercialDecisionEvidence;
        CommercialScoutingEvidence = commercialScoutingEvidence;
    }
}

internal sealed class P12DNpcTravelPlanValue
{
    internal string TargetLocationRuntimeId { get; }
    internal string TargetCityRuntimeId { get; }
    internal NpcTravelReason Reason { get; }
    internal float Utility { get; }
    internal float ExpectedCost { get; }
    internal string OriginDecisionId { get; }
    internal long Revision { get; }

    internal P12DNpcTravelPlanValue(string targetLocationRuntimeId, string targetCityRuntimeId,
        NpcTravelReason reason, float utility, float expectedCost, string originDecisionId, long revision)
    {
        TargetLocationRuntimeId = targetLocationRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        Reason = reason;
        Utility = utility;
        ExpectedCost = expectedCost;
        OriginDecisionId = originDecisionId;
        Revision = revision;
    }
}

internal sealed class P12DNpcMerchantPlanValue
{
    internal string ItemDefinitionId { get; }
    internal string OriginCityRuntimeId { get; }
    internal string TargetCityRuntimeId { get; }
    internal int PlannedAmount { get; }
    internal int RawRemainingAmount { get; }
    internal float PurchasePricePerItem { get; }
    internal int WaitDaysAtDestination { get; }
    internal int PendingTravelDays { get; }
    internal string OriginDecisionId { get; }
    internal long Revision { get; }

    internal P12DNpcMerchantPlanValue(string itemDefinitionId, string originCityRuntimeId,
        string targetCityRuntimeId, int plannedAmount, int rawRemainingAmount,
        float purchasePricePerItem, int waitDaysAtDestination, int pendingTravelDays,
        string originDecisionId, long revision)
    {
        ItemDefinitionId = itemDefinitionId;
        OriginCityRuntimeId = originCityRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        PlannedAmount = plannedAmount;
        RawRemainingAmount = rawRemainingAmount;
        PurchasePricePerItem = purchasePricePerItem;
        WaitDaysAtDestination = waitDaysAtDestination;
        PendingTravelDays = pendingTravelDays;
        OriginDecisionId = originDecisionId;
        Revision = revision;
    }
}

internal sealed class P12DNpcCommercialMarketValue
{
    internal string LocationRuntimeId { get; }
    internal string ItemDefinitionId { get; }
    internal float ObservedPrice { get; }
    internal int ObservedStock { get; }
    internal long ObservedDay { get; }
    internal long ReceivedDay { get; }
    internal CommercialKnowledgeSource Source { get; }
    internal string SourceRuntimeId { get; }

    internal P12DNpcCommercialMarketValue(string locationRuntimeId, string itemDefinitionId,
        float observedPrice, int observedStock, long observedDay, long receivedDay,
        CommercialKnowledgeSource source, string sourceRuntimeId)
    {
        LocationRuntimeId = locationRuntimeId;
        ItemDefinitionId = itemDefinitionId;
        ObservedPrice = observedPrice;
        ObservedStock = observedStock;
        ObservedDay = observedDay;
        ReceivedDay = receivedDay;
        Source = source;
        SourceRuntimeId = sourceRuntimeId;
    }
}

internal sealed class P12DNpcCommercialLiquidityValue
{
    internal string LocationRuntimeId { get; }
    internal MarketLiquidityMode Mode { get; }
    internal float PurchasingPower { get; }
    internal long ObservedDay { get; }
    internal long ReceivedDay { get; }
    internal CommercialKnowledgeSource Source { get; }
    internal string SourceRuntimeId { get; }

    internal P12DNpcCommercialLiquidityValue(string locationRuntimeId, MarketLiquidityMode mode,
        float purchasingPower, long observedDay, long receivedDay,
        CommercialKnowledgeSource source, string sourceRuntimeId)
    {
        LocationRuntimeId = locationRuntimeId;
        Mode = mode;
        PurchasingPower = purchasingPower;
        ObservedDay = observedDay;
        ReceivedDay = receivedDay;
        Source = source;
        SourceRuntimeId = sourceRuntimeId;
    }
}

internal sealed class P12DCityMarketItemSnapshot
{
    internal string ItemDefinitionId { get; }
    internal int Amount { get; }
    internal int DesiredAmount { get; }
    internal float CurrentPrice { get; }

    internal P12DCityMarketItemSnapshot(
        string itemDefinitionId,
        int amount,
        int desiredAmount,
        float currentPrice)
    {
        ItemDefinitionId = itemDefinitionId;
        Amount = amount;
        DesiredAmount = desiredAmount;
        CurrentPrice = currentPrice;
    }
}
