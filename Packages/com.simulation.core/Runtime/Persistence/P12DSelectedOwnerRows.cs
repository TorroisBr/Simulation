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
