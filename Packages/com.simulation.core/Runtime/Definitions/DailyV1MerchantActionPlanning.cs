using System;
using System.Collections.Generic;

/// <summary>Current market facts paired with an exact admitted Daily-v1 item node.</summary>
internal sealed class DailyV1MerchantLocalSaleFact
{
    internal DailyV1ItemDefinition Item { get; }
    internal float ObservedPrice { get; }
    internal float Freshness { get; }
    internal int AvailableAmount { get; }

    internal DailyV1MerchantLocalSaleFact(
        DailyV1ItemDefinition item,
        float observedPrice,
        float freshness,
        int availableAmount)
    {
        Item = item;
        ObservedPrice = observedPrice;
        Freshness = freshness;
        AvailableAmount = availableAmount;
    }
}

/// <summary>The existing local SellGoods choice, retaining its market-fact index and graph identity.</summary>
internal sealed class DailyV1MerchantLocalSalePlan
{
    internal int MarketFactIndex { get; }
    internal DailyV1ItemDefinition Item { get; }
    internal int Amount { get; }
    internal float ReferencePrice { get; }
    internal float LocalPrice { get; }
    internal float ProfitPerItem { get; }
    internal float Score { get; }

    internal DailyV1MerchantLocalSalePlan(
        int marketFactIndex,
        DailyV1ItemDefinition item,
        int amount,
        float referencePrice,
        float localPrice,
        float profitPerItem,
        float score)
    {
        MarketFactIndex = marketFactIndex;
        Item = item;
        Amount = amount;
        ReferencePrice = referencePrice;
        LocalPrice = localPrice;
        ProfitPerItem = profitPerItem;
        Score = score;
    }
}

/// <summary>Unity-read inputs for a selected merchant's trade-destination candidate.</summary>
internal sealed class DailyV1MerchantTradeCandidate
{
    internal DailyV1ItemDefinition Item { get; }
    internal string TargetRuntimeId { get; }
    internal int Amount { get; }
    internal float BuyPrice { get; }
    internal float SellPrice { get; }
    internal float TotalTravelCost { get; }
    internal int TotalTravelDays { get; }
    internal float OriginFreshness { get; }
    internal float DestinationFreshness { get; }

    internal DailyV1MerchantTradeCandidate(
        DailyV1ItemDefinition item,
        string targetRuntimeId,
        int amount,
        float buyPrice,
        float sellPrice,
        float totalTravelCost,
        int totalTravelDays,
        float originFreshness,
        float destinationFreshness)
    {
        Item = item;
        TargetRuntimeId = targetRuntimeId;
        Amount = amount;
        BuyPrice = buyPrice;
        SellPrice = sellPrice;
        TotalTravelCost = totalTravelCost;
        TotalTravelDays = totalTravelDays;
        OriginFreshness = originFreshness;
        DestinationFreshness = destinationFreshness;
    }
}

/// <summary>The selected merchant trade candidate with the existing score semantics.</summary>
internal sealed class DailyV1MerchantTradePlan
{
    internal int CandidateIndex { get; }
    internal DailyV1ItemDefinition Item { get; }
    internal string TargetRuntimeId { get; }
    internal int Amount { get; }
    internal float BuyPrice { get; }
    internal float SellPrice { get; }
    internal float ProfitPerItem { get; }
    internal float NetProfit { get; }
    internal float Score { get; }

    internal DailyV1MerchantTradePlan(
        int candidateIndex,
        DailyV1ItemDefinition item,
        string targetRuntimeId,
        int amount,
        float buyPrice,
        float sellPrice,
        float profitPerItem,
        float netProfit,
        float score)
    {
        CandidateIndex = candidateIndex;
        Item = item;
        TargetRuntimeId = targetRuntimeId;
        Amount = amount;
        BuyPrice = buyPrice;
        SellPrice = sellPrice;
        ProfitPerItem = profitPerItem;
        NetProfit = netProfit;
        Score = score;
    }
}

/// <summary>Core decision portion of selected Daily-v1 merchant local-sale planning.</summary>
internal static class DailyV1MerchantActionPlanning
{
    internal static bool TryPlanLocalSale(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition merchant,
        DailyV1InventoryOwner inventory,
        IReadOnlyList<DailyV1MerchantLocalSaleFact> marketFacts,
        int maximumTradeAmount,
        float minimumProfitPerItem,
        out DailyV1MerchantLocalSalePlan plan)
    {
        plan = null;
        if (!DailyV1NpcDefinitionRules.IsAdmittedNpc(definitions, merchant)
            || inventory == null || !ReferenceEquals(inventory.Definitions, definitions)
            || marketFacts == null || maximumTradeAmount <= 0)
            return false;

        for (int inventoryIndex = 0; inventoryIndex < inventory.Items.Count; inventoryIndex++)
        {
            DailyV1InventoryItemState inventoryRow = inventory.Items[inventoryIndex];
            if (inventoryRow == null)
                return false;

            if (inventoryRow.Amount <= 0)
                continue;

            if (inventoryRow.Item == null
                || !definitions.Items.TryGetValue(inventoryRow.Item.DefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(inventoryRow.Item, admittedItem))
                return false;

            int factIndex = FindExactMarketFact(marketFacts, inventoryRow.Item);
            if (factIndex == -2)
                return false;
            if (factIndex < 0)
                continue;
            DailyV1MerchantLocalSaleFact fact = marketFacts[factIndex];
            if (fact.Freshness <= 0f || fact.AvailableAmount <= 0)
                continue;

            int amount = UnityMathfCompatibility.Min(
                maximumTradeAmount,
                UnityMathfCompatibility.Min(inventoryRow.Amount, fact.AvailableAmount));
            if (amount <= 0)
                continue;

            float referencePrice = inventoryRow.AverageUnitCost > 0f
                ? inventoryRow.AverageUnitCost
                : inventoryRow.Item.Values.basePrice;
            float profitPerItem = fact.ObservedPrice - referencePrice;
            if (profitPerItem < UnityMathfCompatibility.Max(0f, minimumProfitPerItem))
                continue;

            float preferenceMultiplier = DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
                merchant, definitions, inventoryRow.Item, out float configuredMultiplier)
                ? UnityMathfCompatibility.Max(0.01f, configuredMultiplier)
                : 1f;
            float score = profitPerItem * amount
                * fact.Freshness
                * preferenceMultiplier;

            // For local sales all candidates share the same destination and have no target NPC.
            // This is the remaining ordering of MerchantSystem.ShouldPreferOpportunity.
            if (plan == null
                || score > plan.Score
                || (UnityMathfCompatibility.Approximately(score, plan.Score)
                    && string.CompareOrdinal(inventoryRow.Item.DefinitionId, plan.Item.DefinitionId) < 0))
            {
                plan = new DailyV1MerchantLocalSalePlan(
                    factIndex,
                    inventoryRow.Item,
                    amount,
                    referencePrice,
                    fact.ObservedPrice,
                    profitPerItem,
                    score);
            }
        }

        return true;
    }

    private static int FindExactMarketFact(
        IReadOnlyList<DailyV1MerchantLocalSaleFact> marketFacts,
        DailyV1ItemDefinition item)
    {
        for (int index = 0; index < marketFacts.Count; index++)
        {
            DailyV1MerchantLocalSaleFact fact = marketFacts[index];
            if (fact == null)
                continue;
            if (ReferenceEquals(fact.Item, item))
                return index;
            if (fact.Item != null && string.Equals(fact.Item.DefinitionId, item.DefinitionId, StringComparison.Ordinal))
                return -2;
        }
        return -1;
    }

    internal static bool TryPlanTradeOpportunity(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition merchant,
        IReadOnlyList<DailyV1MerchantTradeCandidate> candidates,
        float minimumProfitPerItem,
        out DailyV1MerchantTradePlan plan)
    {
        plan = null;
        if (!DailyV1NpcDefinitionRules.IsAdmittedNpc(definitions, merchant) || candidates == null)
            return false;

        for (int index = 0; index < candidates.Count; index++)
        {
            DailyV1MerchantTradeCandidate candidate = candidates[index];
            if (candidate == null)
                continue;

            if (candidate.Item == null
                || !definitions.Items.TryGetValue(candidate.Item.DefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(candidate.Item, admittedItem))
                return false;

            if (candidate.Amount <= 0 || candidate.BuyPrice <= 0f
                || candidate.OriginFreshness <= 0f || candidate.DestinationFreshness <= 0f
                || candidate.TotalTravelDays <= 0)
                continue;

            float profitPerItem = candidate.SellPrice - candidate.BuyPrice;
            float netProfit = profitPerItem * candidate.Amount - candidate.TotalTravelCost;
            if (profitPerItem < UnityMathfCompatibility.Max(0f, minimumProfitPerItem) || netProfit <= 0f)
                continue;

            float freshness = UnityMathfCompatibility.Min(
                candidate.OriginFreshness, candidate.DestinationFreshness);
            float preferenceMultiplier = DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
                merchant, definitions, candidate.Item, out float configuredMultiplier)
                ? UnityMathfCompatibility.Max(0.01f, configuredMultiplier)
                : 1f;
            float score = netProfit / UnityMathfCompatibility.Max(1, candidate.TotalTravelDays)
                * freshness
                * preferenceMultiplier;
            string candidateTargetRuntimeId = candidate.TargetRuntimeId ?? string.Empty;
            string currentTargetRuntimeId = plan?.TargetRuntimeId ?? string.Empty;
            int targetComparison = plan == null
                ? -1
                : string.CompareOrdinal(candidateTargetRuntimeId, currentTargetRuntimeId);

            // MerchantSystem.ShouldPreferOpportunity orders trade choices by score,
            // then target City RuntimeId, then selected item DefinitionId. The
            // selected BuyGoods path has no target NPC.
            if (plan == null
                || score > plan.Score
                || (UnityMathfCompatibility.Approximately(score, plan.Score)
                    && (targetComparison < 0
                        || (targetComparison == 0
                            && string.CompareOrdinal(candidate.Item.DefinitionId, plan.Item.DefinitionId) < 0))))
            {
                plan = new DailyV1MerchantTradePlan(
                    index,
                    candidate.Item,
                    candidate.TargetRuntimeId,
                    candidate.Amount,
                    candidate.BuyPrice,
                    candidate.SellPrice,
                    profitPerItem,
                    netProfit,
                    score);
            }
        }

        return true;
    }
}
