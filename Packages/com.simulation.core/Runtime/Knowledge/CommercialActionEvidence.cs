using System;

/// <summary>Source identity attached to a factual commercial observation.</summary>
public enum CommercialKnowledgeSource
{
    DirectObservation,
    InitialScenarioKnowledge,
    SharedByNpc
}

[Serializable]
public sealed class CommercialObservationEvidence
{
    private readonly string locationRuntimeId;
    private readonly float knownPrice;
    private readonly int knownStock;
    private readonly long observedDay;
    private readonly long receivedDay;
    private readonly float freshness;
    private readonly CommercialKnowledgeSource source;
    private readonly string sourceRuntimeId;

    public string LocationRuntimeId => locationRuntimeId;
    public float KnownPrice => knownPrice;
    public int KnownStock => knownStock;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public float Freshness => freshness;
    public CommercialKnowledgeSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;

    public CommercialObservationEvidence(
        string locationRuntimeId,
        float knownPrice,
        int knownStock,
        long observedDay,
        long receivedDay,
        float freshness,
        CommercialKnowledgeSource source,
        string sourceRuntimeId)
    {
        this.locationRuntimeId = RequireId(locationRuntimeId, nameof(locationRuntimeId));
        this.knownPrice = Math.Max(0f, knownPrice);
        this.knownStock = Math.Max(0, knownStock);
        this.observedDay = Math.Max(0L, observedDay);
        this.receivedDay = Math.Max(this.observedDay, receivedDay);
        this.freshness = Math.Max(0f, Math.Min(1f, freshness));
        this.source = source;
        this.sourceRuntimeId = source == CommercialKnowledgeSource.SharedByNpc
            ? RequireId(sourceRuntimeId, nameof(sourceRuntimeId))
            : null;
    }

    public static CommercialObservationEvidence Capture(
        string locationRuntimeId,
        float knownPrice,
        int knownStock,
        long observedDay,
        long receivedDay,
        float freshness,
        CommercialKnowledgeSource source,
        string sourceRuntimeId)
    {
        return string.IsNullOrWhiteSpace(locationRuntimeId)
            ? null
            : new CommercialObservationEvidence(locationRuntimeId, knownPrice, knownStock,
                observedDay, receivedDay, freshness, source, sourceRuntimeId);
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Commercial observation evidence requires stable IDs.", parameterName);
        return value;
    }
}

[Serializable]
public sealed class CommercialLiquidityEvidence
{
    private readonly string locationRuntimeId;
    private readonly MarketLiquidityMode liquidityMode;
    private readonly float observedPurchasingPower;
    private readonly long observedDay;
    private readonly long receivedDay;
    private readonly float freshness;
    private readonly CommercialKnowledgeSource source;
    private readonly string sourceRuntimeId;

    public string LocationRuntimeId => locationRuntimeId;
    public MarketLiquidityMode LiquidityMode => liquidityMode;
    public float ObservedPurchasingPower => observedPurchasingPower;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public float Freshness => freshness;
    public CommercialKnowledgeSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;

    private CommercialLiquidityEvidence(string locationRuntimeId, MarketLiquidityMode liquidityMode,
        float observedPurchasingPower, long observedDay, long receivedDay, float freshness,
        CommercialKnowledgeSource source, string sourceRuntimeId)
    {
        this.locationRuntimeId = RequireId(locationRuntimeId, nameof(locationRuntimeId));
        this.liquidityMode = liquidityMode;
        this.observedPurchasingPower = observedPurchasingPower;
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.freshness = Math.Max(0f, Math.Min(1f, freshness));
        this.source = source;
        this.sourceRuntimeId = source == CommercialKnowledgeSource.SharedByNpc
            ? RequireId(sourceRuntimeId, nameof(sourceRuntimeId))
            : null;
    }

    public static CommercialLiquidityEvidence Capture(string locationRuntimeId,
        MarketLiquidityMode liquidityMode, float observedPurchasingPower, long observedDay,
        long receivedDay, float freshness, CommercialKnowledgeSource source, string sourceRuntimeId)
    {
        return string.IsNullOrWhiteSpace(locationRuntimeId)
            ? null
            : new CommercialLiquidityEvidence(locationRuntimeId, liquidityMode, observedPurchasingPower,
                observedDay, receivedDay, freshness, source, sourceRuntimeId);
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Commercial liquidity evidence requires a LocationRuntimeId.", parameterName);
        return value;
    }
}

[Serializable]
public sealed class CommercialDecisionEvidence
{
    private readonly string itemDefinitionId;
    private readonly string currentLocationRuntimeId;
    private readonly string tradeOriginLocationRuntimeId;
    private readonly string tradeDestinationLocationRuntimeId;
    private readonly string previousDestinationLocationRuntimeId;
    private readonly CommercialObservationEvidence originObservation;
    private readonly CommercialObservationEvidence destinationObservation;
    private readonly CommercialLiquidityEvidence destinationLiquidityObservation;
    private readonly int expectedQuantity;
    private readonly float expectedPurchaseUnitPrice;
    private readonly float expectedSaleUnitPrice;
    private readonly float expectedTravelCost;
    private readonly float expectedGrossProfit;
    private readonly float expectedNetProfit;
    private readonly float expectedScore;

    public string ItemDefinitionId => itemDefinitionId;
    public string CurrentLocationRuntimeId => currentLocationRuntimeId;
    public string TradeOriginLocationRuntimeId => tradeOriginLocationRuntimeId;
    public string TradeDestinationLocationRuntimeId => tradeDestinationLocationRuntimeId;
    public string PreviousDestinationLocationRuntimeId => previousDestinationLocationRuntimeId;
    public CommercialObservationEvidence OriginObservation => originObservation;
    public CommercialObservationEvidence DestinationObservation => destinationObservation;
    public CommercialLiquidityEvidence DestinationLiquidityObservation => destinationLiquidityObservation;
    public int ExpectedQuantity => expectedQuantity;
    public float ExpectedPurchaseUnitPrice => expectedPurchaseUnitPrice;
    public float ExpectedSaleUnitPrice => expectedSaleUnitPrice;
    public float ExpectedTravelCost => expectedTravelCost;
    public float ExpectedGrossProfit => expectedGrossProfit;
    public float ExpectedNetProfit => expectedNetProfit;
    public float ExpectedScore => expectedScore;

    public CommercialDecisionEvidence(string itemDefinitionId, string currentLocationRuntimeId,
        string tradeOriginLocationRuntimeId, string tradeDestinationLocationRuntimeId,
        string previousDestinationLocationRuntimeId, CommercialObservationEvidence originObservation,
        CommercialObservationEvidence destinationObservation, int expectedQuantity,
        float expectedPurchaseUnitPrice, float expectedSaleUnitPrice, float expectedTravelCost,
        float expectedGrossProfit, float expectedNetProfit, float expectedScore,
        CommercialLiquidityEvidence destinationLiquidityObservation = null)
    {
        this.itemDefinitionId = RequireId(itemDefinitionId, nameof(itemDefinitionId));
        this.currentLocationRuntimeId = RequireId(currentLocationRuntimeId, nameof(currentLocationRuntimeId));
        this.tradeOriginLocationRuntimeId = RequireId(tradeOriginLocationRuntimeId, nameof(tradeOriginLocationRuntimeId));
        this.tradeDestinationLocationRuntimeId = RequireId(tradeDestinationLocationRuntimeId, nameof(tradeDestinationLocationRuntimeId));
        this.previousDestinationLocationRuntimeId = NormalizeOptionalId(previousDestinationLocationRuntimeId);
        this.originObservation = originObservation;
        this.destinationObservation = destinationObservation;
        this.destinationLiquidityObservation = destinationLiquidityObservation;
        this.expectedQuantity = Math.Max(0, expectedQuantity);
        this.expectedPurchaseUnitPrice = Math.Max(0f, expectedPurchaseUnitPrice);
        this.expectedSaleUnitPrice = Math.Max(0f, expectedSaleUnitPrice);
        this.expectedTravelCost = Math.Max(0f, expectedTravelCost);
        this.expectedGrossProfit = expectedGrossProfit;
        this.expectedNetProfit = expectedNetProfit;
        this.expectedScore = expectedScore;
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Commercial decision evidence requires stable IDs.", parameterName);
        return value;
    }

    private static string NormalizeOptionalId(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}

[Serializable]
public sealed class CommercialScoutingEvidence
{
    private readonly string targetLocationRuntimeId;
    private readonly string knownRouteRuntimeId;
    private readonly int unknownObservationCount;
    private readonly int staleObservationCount;
    private readonly bool hasOldestObservation;
    private readonly long oldestObservationDay;
    private readonly float expectedTravelCost;
    private readonly int expectedTravelDays;
    private readonly float expectedScore;

    public string TargetLocationRuntimeId => targetLocationRuntimeId;
    public string KnownRouteRuntimeId => knownRouteRuntimeId;
    public int UnknownObservationCount => unknownObservationCount;
    public int StaleObservationCount => staleObservationCount;
    public bool HasOldestObservation => hasOldestObservation;
    public long OldestObservationDay => oldestObservationDay;
    public float ExpectedTravelCost => expectedTravelCost;
    public int ExpectedTravelDays => expectedTravelDays;
    public float ExpectedScore => expectedScore;

    public CommercialScoutingEvidence(string targetLocationRuntimeId, string knownRouteRuntimeId,
        int unknownObservationCount, int staleObservationCount, bool hasOldestObservation,
        long oldestObservationDay, float expectedTravelCost, int expectedTravelDays, float expectedScore)
    {
        this.targetLocationRuntimeId = RequireId(targetLocationRuntimeId, nameof(targetLocationRuntimeId));
        this.knownRouteRuntimeId = RequireId(knownRouteRuntimeId, nameof(knownRouteRuntimeId));
        this.unknownObservationCount = Math.Max(0, unknownObservationCount);
        this.staleObservationCount = Math.Max(0, staleObservationCount);
        this.hasOldestObservation = hasOldestObservation;
        this.oldestObservationDay = hasOldestObservation ? Math.Max(0L, oldestObservationDay) : 0L;
        this.expectedTravelCost = Math.Max(0f, expectedTravelCost);
        this.expectedTravelDays = Math.Max(1, expectedTravelDays);
        this.expectedScore = Math.Max(0f, expectedScore);
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Commercial scouting evidence requires stable IDs.", parameterName);
        return value;
    }
}
