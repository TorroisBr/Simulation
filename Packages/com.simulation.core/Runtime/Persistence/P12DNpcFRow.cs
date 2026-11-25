using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Detached P12-D NPC values used by both the Core hydrator and Unity projection adapter.</summary>
internal sealed class P12DNpcFRow
{
    internal string RuntimeId { get; }
    internal P12DNpcActionValue Action { get; }
    internal int TravelDaysRemaining { get; }
    internal int TravelDaysTotal { get; }
    internal string TravelRouteRuntimeId { get; }
    internal bool TravelStartedToday { get; }
    internal string TravelOriginDecisionId { get; }
    internal string ActiveTravelPartyId { get; }
    internal long TravelStateRevision { get; }
    internal P12DNpcMerchantPlanValue MerchantPlan { get; }
    internal P12DNpcTravelPlanValue TravelPlan { get; }
    internal IReadOnlyList<ExplorableSiteKnowledgeObservation> SiteObservations { get; }
    internal long SiteKnowledgeRevision { get; }
    internal IReadOnlyList<string> KnownLocationRuntimeIds { get; }
    internal IReadOnlyList<string> KnownRouteRuntimeIds { get; }
    internal long SpatialKnowledgeRevision { get; }
    internal IReadOnlyList<AdventureOppositionObservation> OppositionObservations { get; }
    internal IReadOnlyList<AdventureNotableItemObservation> NotableItemObservations { get; }
    internal IReadOnlyList<AdventureCommonResourceObservation> CommonResourceObservations { get; }
    internal IReadOnlyList<AdventureAccessObservation> AccessObservations { get; }
    internal long AdventureKnowledgeRevision { get; }
    internal IReadOnlyList<P12DNpcCommercialMarketValue> CommercialMarkets { get; }
    internal IReadOnlyList<P12DNpcCommercialLiquidityValue> CommercialLiquidity { get; }
    internal IReadOnlyList<P12DNpcCommercialShareReceiptValue> CommercialShareReceipts { get; }
    internal long CommercialKnowledgeRevision { get; }

    internal P12DNpcFRow(string runtimeId, P12DNpcActionValue action,
        int travelDaysRemaining, int travelDaysTotal, string travelRouteRuntimeId,
        bool travelStartedToday, string travelOriginDecisionId, string activeTravelPartyId,
        long travelStateRevision, P12DNpcMerchantPlanValue merchantPlan,
        P12DNpcTravelPlanValue travelPlan,
        IEnumerable<ExplorableSiteKnowledgeObservation> siteObservations, long siteKnowledgeRevision,
        IEnumerable<string> knownLocations, IEnumerable<string> knownRoutes, long spatialKnowledgeRevision,
        IEnumerable<AdventureOppositionObservation> opposition,
        IEnumerable<AdventureNotableItemObservation> notableItems,
        IEnumerable<AdventureCommonResourceObservation> resources,
        IEnumerable<AdventureAccessObservation> access, long adventureKnowledgeRevision,
        IEnumerable<P12DNpcCommercialMarketValue> commercialMarkets,
        IEnumerable<P12DNpcCommercialLiquidityValue> commercialLiquidity,
        IEnumerable<P12DNpcCommercialShareReceiptValue> commercialShareReceipts,
        long commercialKnowledgeRevision)
    {
        RuntimeId = runtimeId;
        Action = action;
        TravelDaysRemaining = travelDaysRemaining;
        TravelDaysTotal = travelDaysTotal;
        TravelRouteRuntimeId = travelRouteRuntimeId;
        TravelStartedToday = travelStartedToday;
        TravelOriginDecisionId = travelOriginDecisionId;
        ActiveTravelPartyId = activeTravelPartyId;
        TravelStateRevision = travelStateRevision;
        MerchantPlan = merchantPlan;
        TravelPlan = travelPlan;
        SiteObservations = Copy(siteObservations);
        SiteKnowledgeRevision = siteKnowledgeRevision;
        KnownLocationRuntimeIds = Copy(knownLocations);
        KnownRouteRuntimeIds = Copy(knownRoutes);
        SpatialKnowledgeRevision = spatialKnowledgeRevision;
        OppositionObservations = Copy(opposition);
        NotableItemObservations = Copy(notableItems);
        CommonResourceObservations = Copy(resources);
        AccessObservations = Copy(access);
        AdventureKnowledgeRevision = adventureKnowledgeRevision;
        CommercialMarkets = Copy(commercialMarkets);
        CommercialLiquidity = Copy(commercialLiquidity);
        CommercialShareReceipts = Copy(commercialShareReceipts);
        CommercialKnowledgeRevision = commercialKnowledgeRevision;
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) =>
        new ReadOnlyCollection<T>(new List<T>(source ?? throw new ArgumentNullException(nameof(source))));
}
