using System;

/// <summary>The bounded failure surface of the selected population purchase operation.</summary>
internal enum DailyV1PopulationConsumptionFailure
{
    None,
    InvalidInput,
    InvalidQuantity,
    InvalidPrice,
    InsufficientFunds,
    InsufficientStock,
    CounterpartyCreditCapacity,
    TransactionCommitFailed
}

/// <summary>Host-neutral result data for one selected City population transaction.</summary>
internal sealed class DailyV1PopulationConsumptionOutcome
{
    internal bool Success { get; }
    internal DailyV1PopulationConsumptionFailure Failure { get; }
    internal int RequestedQuantity { get; }
    internal int Quantity { get; }
    internal float UnitPrice { get; }
    internal float TotalPrice { get; }
    internal string ItemDefinitionId { get; }
    internal string SourceRuntimeId { get; }
    internal string DestinationRuntimeId { get; }
    internal string ActorRuntimeId { get; }
    internal string ItemSourceRuntimeId { get; }
    internal string ItemDestinationRuntimeId { get; }

    private DailyV1PopulationConsumptionOutcome(
        bool success,
        DailyV1PopulationConsumptionFailure failure,
        int requestedQuantity,
        int quantity,
        float unitPrice,
        float totalPrice,
        string itemDefinitionId,
        string sourceRuntimeId,
        string destinationRuntimeId,
        string actorRuntimeId,
        string itemSourceRuntimeId,
        string itemDestinationRuntimeId)
    {
        Success = success;
        Failure = failure;
        RequestedQuantity = requestedQuantity;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalPrice = totalPrice;
        ItemDefinitionId = itemDefinitionId;
        SourceRuntimeId = sourceRuntimeId;
        DestinationRuntimeId = destinationRuntimeId;
        ActorRuntimeId = actorRuntimeId;
        ItemSourceRuntimeId = itemSourceRuntimeId;
        ItemDestinationRuntimeId = itemDestinationRuntimeId;
    }

    internal static DailyV1PopulationConsumptionOutcome Failed(
        DailyV1PopulationConsumptionFailure failure,
        string actorRuntimeId) => new DailyV1PopulationConsumptionOutcome(
            false, failure, 0, 0, 0f, 0f, null, null, null, actorRuntimeId, null, null);

    internal static DailyV1PopulationConsumptionOutcome Applied(
        int requestedQuantity,
        int quantity,
        float unitPrice,
        float totalPrice,
        string itemDefinitionId,
        string populationRuntimeId,
        string counterpartyRuntimeId) => new DailyV1PopulationConsumptionOutcome(
            true, DailyV1PopulationConsumptionFailure.None, requestedQuantity, quantity,
            unitPrice, totalPrice, itemDefinitionId, populationRuntimeId, counterpartyRuntimeId,
            populationRuntimeId, counterpartyRuntimeId, populationRuntimeId);
}

/// <summary>
/// Executes one selected account-backed City population consumption against the shared
/// City, Market, and MoneyAccount authorities. It contains no Unity adapter callback.
/// </summary>
internal static class DailyV1PopulationConsumptionOperation
{
    internal static DailyV1PopulationConsumptionOutcome Execute(
        PopulationEconomyRuntime population,
        MarketCounterpartyRuntime settlement,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity)
    {
        string actorRuntimeId = population?.PopulationEconomicRuntimeId;
        if (population == null || item == null)
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InvalidInput, actorRuntimeId);

        if (population.PaymentMode != ConsumptionPaymentMode.AccountBacked
            || population.MoneyAccount == null
            || settlement == null
            || settlement.LiquidityMode != MarketLiquidityMode.AccountBacked
            || settlement.MoneyAccount == null
            || !string.Equals(population.CityRuntimeId, settlement.CounterpartyRuntimeId, StringComparison.Ordinal))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InvalidInput, actorRuntimeId);

        if (requestedQuantity <= 0)
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InvalidQuantity, actorRuntimeId);

        if (market == null)
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InsufficientStock, actorRuntimeId);

        if (!ReferenceEquals(market.Counterparty, settlement))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InvalidInput, actorRuntimeId);

        long expectedMarketRevision = market.Revision;
        DailyV1MarketItemState selectedItem = market.GetItem(item);
        if (selectedItem == null || selectedItem.Amount <= 0)
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InsufficientStock, actorRuntimeId);

        float unitPrice = selectedItem.CurrentPrice;
        if (!IsValidNonNegativeFiniteAmount(unitPrice))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InvalidPrice, actorRuntimeId);

        unitPrice = UnityMathfCompatibility.Max(0.01f, unitPrice);
        int affordableQuantity = GetAffordableQuantity(population.MoneyAccount.Balance, unitPrice);
        int quantity = UnityMathfCompatibility.Min(requestedQuantity, selectedItem.Amount, affordableQuantity);
        if (quantity <= 0)
        {
            DailyV1PopulationConsumptionFailure failure = selectedItem.Amount <= 0
                ? DailyV1PopulationConsumptionFailure.InsufficientStock
                : DailyV1PopulationConsumptionFailure.InsufficientFunds;
            return DailyV1PopulationConsumptionOutcome.Failed(failure, actorRuntimeId);
        }

        float totalPrice = unitPrice * quantity;
        if (!IsValidNonNegativeFiniteAmount(totalPrice)
            || !population.MoneyAccount.CanDebit(totalPrice))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.InsufficientFunds, actorRuntimeId);
        if (!settlement.MoneyAccount.CanCredit(totalPrice))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.CounterpartyCreditCapacity, actorRuntimeId);

        if (!market.TryCreatePreparedSnapshot(out DailyV1MarketPreparedState marketState))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.TransactionCommitFailed, actorRuntimeId);

        if (!market.TryCountPreparedItems(marketState, item, out int matchingRows)
            || matchingRows != 1
            || !market.TryGetPreparedItem(marketState, item, out DailyV1MarketItemState preparedItem)
            || preparedItem == null
            || preparedItem.Amount != selectedItem.Amount
            || preparedItem.DesiredAmount != selectedItem.DesiredAmount
            || BitConverter.ToInt32(BitConverter.GetBytes(preparedItem.CurrentPrice), 0)
                != BitConverter.ToInt32(BitConverter.GetBytes(selectedItem.CurrentPrice), 0))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.TransactionCommitFailed, actorRuntimeId);

        if (!market.TryApplyPreparedStockDelta(marketState, item, -quantity, selectedItem.DesiredAmount,
                out DailyV1MarketPreparedState replacement, out _)
            || replacement == null)
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.TransactionCommitFailed, actorRuntimeId);

        // Retain the existing successful account order and P12 mutation callbacks.
        if (!TryCommitMoneyTransfer(population.MoneyAccount, settlement.MoneyAccount, totalPrice))
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.TransactionCommitFailed, actorRuntimeId);

        // `marketState` was read before account callbacks. It remains applicable only if
        // no market write occurred while those callbacks ran.
        if (!market.CanInstallMaterialFlow(expectedMarketRevision, 1L)
            || !market.TryInstall(expectedMarketRevision, 1L, replacement))
        {
            TryCommitMoneyTransfer(settlement.MoneyAccount, population.MoneyAccount, totalPrice);
            return DailyV1PopulationConsumptionOutcome.Failed(
                DailyV1PopulationConsumptionFailure.TransactionCommitFailed, actorRuntimeId);
        }

        market.NotifyMaterialFlowInstalled();
        return DailyV1PopulationConsumptionOutcome.Applied(
            requestedQuantity, quantity, unitPrice, totalPrice, item.DefinitionId,
            population.PopulationEconomicRuntimeId, settlement.CounterpartyRuntimeId);
    }

    private static bool TryCommitMoneyTransfer(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount)
    {
        if (source == null || destination == null || ReferenceEquals(source, destination)
            || !IsValidNonNegativeFiniteAmount(amount)
            || !source.CanDebit(amount) || !destination.CanCredit(amount)) return false;
        if (!source.TryDebit(amount)) return false;
        if (destination.TryCredit(amount)) return true;
        source.TryCredit(amount);
        return false;
    }

    private static int GetAffordableQuantity(float balance, float unitPrice)
    {
        if (balance < unitPrice) return 0;
        float affordableAmount = balance / unitPrice;
        if (float.IsInfinity(affordableAmount) || affordableAmount >= int.MaxValue)
            return int.MaxValue;
        return UnityMathfCompatibility.Max(0, UnityMathfCompatibility.FloorToInt(affordableAmount));
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount) =>
        amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);
}
