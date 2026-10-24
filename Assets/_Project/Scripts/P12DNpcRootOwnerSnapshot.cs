using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

internal enum P12DNpcRootOwnerSnapshotFailure
{
    None = 0,
    InvalidContext,
    InvalidCaptureEvidence,
    InvalidNpcIdentity,
    InvalidOwner,
    InvalidOwnerSection,
    UnsupportedValue,
    InvalidDefinition,
    InvalidReference,
    InvalidPersonBinding,
    InvalidCityMembership,
    InvalidProfileExclusion,
    StaleCapture,
    InvalidDefinitionMap,
    InvalidSelectedItemBindings,
    InvalidNpcDefinition,
    InvalidSelectedNpcDefinition,
    InvalidStatusDefinition,
    InvalidSelectedStatusDefinition,
    InvalidInventoryItem,
    InvalidSelectedInventoryItem
}

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

    internal P12DNpcActionValue(
        string actionDefinitionId, string targetNpcRuntimeId, string targetCityRuntimeId,
        string targetItemDefinitionId, int amount, float expectedUnitPrice,
        float successChanceMultiplier, NpcTravelReason travelReason, float expectedNetValue,
        string originDecisionId, string stableOccurrenceKey,
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

    internal P12DNpcCommercialMarketValue(CommercialMarketObservation value)
    {
        LocationRuntimeId = value.LocationRuntimeId;
        ItemDefinitionId = value.ItemDefinitionId;
        ObservedPrice = value.ObservedPrice;
        ObservedStock = value.ObservedStock;
        ObservedDay = value.ObservedDay;
        ReceivedDay = value.ReceivedDay;
        Source = value.Source;
        SourceRuntimeId = value.SourceRuntimeId;
    }

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

    internal P12DNpcCommercialLiquidityValue(CommercialLiquidityObservation value)
    {
        LocationRuntimeId = value.LocationRuntimeId;
        Mode = value.LiquidityMode;
        PurchasingPower = value.ObservedPurchasingPower;
        ObservedDay = value.ObservedDay;
        ReceivedDay = value.ReceivedDay;
        Source = value.Source;
        SourceRuntimeId = value.SourceRuntimeId;
    }

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
    internal IReadOnlyList<CommercialKnowledgeShareReceipt> CommercialShareReceipts { get; }
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
        IEnumerable<CommercialKnowledgeShareReceipt> commercialShareReceipts,
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

internal sealed class P12DNpcDProjection
{
    internal P12DCityNpcProjectionCaptureEvidence Evidence { get; }
    internal DailyCaptureEligibilityToken Token { get; }
    internal object CaptureStamp { get; }
    internal IReadOnlyList<OwnerSectionCensusSnapshot> OwnerSectionVector { get; }
    internal IReadOnlyList<P12DNpcDRow> Rows { get; }
    internal P12DNpcDProjection(P12DCityNpcProjectionCaptureEvidence evidence,
        DailyCaptureEligibilityToken token, object captureStamp,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector, IEnumerable<P12DNpcDRow> rows)
    {
        Evidence = evidence;
        Token = token;
        CaptureStamp = captureStamp;
        OwnerSectionVector = ownerSectionVector;
        Rows = new ReadOnlyCollection<P12DNpcDRow>(new List<P12DNpcDRow>(rows));
    }
}

internal sealed class P12DNpcFProjection
{
    internal P12DCityNpcProjectionCaptureEvidence Evidence { get; }
    internal DailyCaptureEligibilityToken Token { get; }
    internal object CaptureStamp { get; }
    internal IReadOnlyList<OwnerSectionCensusSnapshot> OwnerSectionVector { get; }
    internal IReadOnlyList<P12DNpcFRow> Rows { get; }
    internal P12DNpcFProjection(P12DCityNpcProjectionCaptureEvidence evidence,
        DailyCaptureEligibilityToken token, object captureStamp,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector, IEnumerable<P12DNpcFRow> rows)
    {
        Evidence = evidence;
        Token = token;
        CaptureStamp = captureStamp;
        OwnerSectionVector = ownerSectionVector;
        Rows = new ReadOnlyCollection<P12DNpcFRow>(new List<P12DNpcFRow>(rows));
    }
}

internal sealed class P12DNpcStagedOwnerState
{
    internal string NpcDefinitionId { get; set; }
    internal DailyV1EffectiveDefinitions SelectedDefinitions { get; set; }
    internal DailyV1NpcDefinition SelectedNpcDefinition { get; set; }
    internal IReadOnlyList<DailyV1StatusDefinition> SelectedCurrentStatusDefinitions { get; set; }
    internal NpcData NpcData { get; set; }
    internal string PersonIdValue { get; set; }
    internal PersonRuntime PersonRuntime { get; set; }
    internal string ResidenceSettlementRuntimeId { get; set; }
    internal NpcLifeState LifeState { get; set; }
    internal NpcInjurySeverity InjurySeverity { get; set; }
    internal IReadOnlyList<NpcStatusData> CurrentStatus { get; set; }
    internal int HiddenDaysRemaining { get; set; }
    internal string CurrentActionDefinitionId { get; set; }
    internal NpcActionRuntime CurrentActionRuntime { get; set; }
    internal DailyV1NpcRuntimeOwners SelectedRuntimeOwners { get; set; }
    internal DailyV1NpcActionPayloadOwner SelectedCurrentActionPayload { get; set; }
    internal CityRuntime CurrentCity { get; set; }
    internal SpatialLocationRuntime CurrentLocation { get; set; }
    internal CityRuntime DestinationCity { get; set; }
    internal SpatialLocationRuntime DestinationLocation { get; set; }
    internal int TravelDaysRemaining { get; set; }
    internal int TravelDaysTotal { get; set; }
    internal string TravelRouteRuntimeId { get; set; }
    internal bool TravelStartedToday { get; set; }
    internal string TravelOriginDecisionId { get; set; }
    internal string ActiveTravelPartyId { get; set; }
    internal long TravelStateRevision { get; set; }
    internal InventoryRuntime Inventory { get; set; }
    internal MoneyAccountRuntime MoneyAccount { get; set; }
    internal MerchantTradePlanRuntime MerchantTradePlan { get; set; }
    internal DailyV1NpcMerchantTradePlanOwner SelectedMerchantTradePlanOwner { get; set; }
    internal NpcTravelPlanRuntime TravelPlan { get; set; }
    internal CommercialKnowledgeRuntime CommercialKnowledge { get; set; }
    internal SpatialKnowledgeRuntime SpatialKnowledge { get; set; }
    internal ExplorableSiteKnowledgeRuntime ExplorableSiteKnowledge { get; set; }
    internal LocalTopologyKnowledgeRuntime LocalTopologyKnowledge { get; set; }
    internal AdventureSiteIntelKnowledgeRuntime AdventureSiteIntelKnowledge { get; set; }
    internal long LifeStateRevision { get; set; }
    internal long ResidenceRevision { get; set; }
    internal long CrimeJusticeRevision { get; set; }
    internal long CurrentActionRevision { get; set; }
}

internal sealed class P12DNpcPreparedSelectedCoreOwner
{
    internal DailyV1NpcRuntimeOwners RuntimeOwners { get; set; }
    internal InventoryRuntime InventoryProjection { get; set; }
    internal CommercialKnowledgeRuntime CommercialKnowledgeProjection { get; set; }
    internal SpatialKnowledgeRuntime SpatialKnowledge { get; set; }
    internal IReadOnlyList<DailyV1StatusDefinition> StatusDefinitions { get; set; }
    internal DailyV1NpcActionPayloadOwner CurrentActionPayload { get; set; }
}


internal static class P12DNpcRootOwnerSnapshot
{
    private const int SchemaVersion = 1;

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        object captureStamp,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        out P12DNpcDProjection dProjection,
        out P12DNpcFProjection fProjection,
        out P12DNpcRootOwnerSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        dProjection = null;
        fProjection = null;
        failure = P12DNpcRootOwnerSnapshotFailure.InvalidContext;
        if (runtime == null || token == null || captureStamp == null || ownerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, ownerSectionVector)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L || token.AbsoluteDay < 0L || token.MutationEpoch < 0L)
            return false;

        if (!DailyCaptureStagingAttempt.IsCaptureCurrent(
                runtime, token, ownerSectionVector, stagingAttempt))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.StaleCapture;
            return false;
        }

        if (!TryRequireSection(token, ExplorableSiteCensusProvider.SectionId,
                ExplorableSiteCensusProvider.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
                owner: null, cardinality: 0, revision: 0L, requireOwner: false))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidProfileExclusion;
            return false;
        }

        IReadOnlyList<NpcRuntime> roster = runtime.NpcRuntimes;
        if (roster == null)
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidNpcIdentity;
            return false;
        }

        if (!P12DCityNpcProjectionCaptureEvidence.TryCreate(
                token, captureStamp, ownerSectionVector, roster,
                out P12DCityNpcProjectionCaptureEvidence evidence, out _))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidCaptureEvidence;
            return false;
        }

        List<P12DNpcDRow> dRows = new List<P12DNpcDRow>(roster.Count);
        List<P12DNpcFRow> fRows = new List<P12DNpcFRow>(roster.Count);
        HashSet<object> inventoryOwners = new HashSet<object>();
        HashSet<object> accountOwners = new HashSet<object>();
        HashSet<object> merchantPlanOwners = new HashSet<object>();
        HashSet<object> travelPlanOwners = new HashSet<object>();
        HashSet<object> commercialOwners = new HashSet<object>();
        HashSet<object> spatialOwners = new HashSet<object>();
        HashSet<object> siteKnowledgeOwners = new HashSet<object>();
        HashSet<object> adventureOwners = new HashSet<object>();
        HashSet<NpcActionRuntime> actionOwners = new HashSet<NpcActionRuntime>();
        HashSet<DailyV1NpcActionPayloadOwner> selectedActionPayloadOwners =
            new HashSet<DailyV1NpcActionPayloadOwner>();
        HashSet<CityRuntime> cityOwners = new HashSet<CityRuntime>();
        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < roster.Count; i++)
        {
            NpcRuntime npc = roster[i];
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcIds.Add(npc.RuntimeId) || npc.NpcData == null
                || string.IsNullOrWhiteSpace(npc.NpcData.DefinitionId))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidNpcIdentity;
                return false;
            }

            if (!TryCaptureNpc(npc, token, evidence,
                    inventoryOwners, accountOwners, merchantPlanOwners, travelPlanOwners,
                    commercialOwners, spatialOwners, siteKnowledgeOwners, adventureOwners,
                    actionOwners, selectedActionPayloadOwners, cityOwners,
                    out P12DNpcDRow dRow, out P12DNpcFRow fRow, out failure))
                return false;
            dRows.Add(dRow);
            fRows.Add(fRow);
        }

        if (!DailyCaptureStagingAttempt.IsCaptureCurrent(
                runtime, token, ownerSectionVector, stagingAttempt)
            || !evidence.AreReceiptOwnersStillExactZero())
        {
            failure = P12DNpcRootOwnerSnapshotFailure.StaleCapture;
            return false;
        }

        dProjection = new P12DNpcDProjection(evidence, token, captureStamp, ownerSectionVector, dRows);
        fProjection = new P12DNpcFProjection(evidence, token, captureStamp, ownerSectionVector, fRows);
        failure = P12DNpcRootOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryCaptureNpc(
        NpcRuntime npc,
        DailyCaptureEligibilityToken token,
        P12DCityNpcProjectionCaptureEvidence evidence,
        HashSet<object> inventoryOwners,
        HashSet<object> accountOwners,
        HashSet<object> merchantPlanOwners,
        HashSet<object> travelPlanOwners,
        HashSet<object> commercialOwners,
        HashSet<object> spatialOwners,
        HashSet<object> siteKnowledgeOwners,
        HashSet<object> adventureOwners,
        HashSet<NpcActionRuntime> actionOwners,
        HashSet<DailyV1NpcActionPayloadOwner> selectedActionPayloadOwners,
        HashSet<CityRuntime> cityOwners,
        out P12DNpcDRow dRow,
        out P12DNpcFRow fRow,
        out P12DNpcRootOwnerSnapshotFailure failure)
    {
        dRow = null;
        fRow = null;
        failure = P12DNpcRootOwnerSnapshotFailure.InvalidOwner;
        InventoryRuntime inventory = npc.ExistingInventory;
        MoneyAccountRuntime account = npc.MoneyAccount;
        MerchantTradePlanRuntime merchantPlan = npc.ExistingMerchantTradePlan;
        object merchantPlanAuthority = npc.MerchantTradePlanAuthority;
        NpcTravelPlanRuntime travelPlan = npc.ExistingTravelPlan;
        CommercialKnowledgeRuntime commercial = npc.ExistingCommercialKnowledge;
        SpatialKnowledgeRuntime spatial = npc.ExistingSpatialKnowledge;
        ExplorableSiteKnowledgeRuntime siteKnowledge = npc.ExistingExplorableSiteKnowledge;
        LocalTopologyKnowledgeRuntime localTopology = npc.ExistingLocalTopologyKnowledge;
        AdventureSiteIntelKnowledgeRuntime adventure = npc.ExistingAdventureSiteIntelKnowledge;
        IReadOnlyList<NpcStatusData> statuses = null;
        List<string> statusIds = new List<string>();
        if (npc.UsesSelectedDailyV1Definitions)
        {
            if (!npc.TryCopySelectedCurrentStatusDefinitionIds(statusIds))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidDefinition;
                return false;
            }
        }
        else
        {
            statuses = npc.ExistingCurrentStatus;
        }

        if (inventory == null || account == null || merchantPlan == null
            || merchantPlanAuthority == null || travelPlan == null
            || commercial == null || spatial == null || siteKnowledge == null
            || localTopology == null || adventure == null
            || (!npc.UsesSelectedDailyV1Definitions && statuses == null)
            || !inventoryOwners.Add(inventory) || !accountOwners.Add(account)
            || !merchantPlanOwners.Add(merchantPlanAuthority) || !travelPlanOwners.Add(travelPlan)
            || !commercialOwners.Add(commercial) || !spatialOwners.Add(spatial)
            || !siteKnowledgeOwners.Add(siteKnowledge) || !adventureOwners.Add(adventure)
            || !string.Equals(spatial.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(siteKnowledge.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(localTopology.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(adventure.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
            || localTopology.Revision != 0L
            || localTopology.PlaceObservations.Count != 0
            || localTopology.ConnectionObservations.Count != 0)
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidProfileExclusion;
            return false;
        }

        int liveInventoryCardinality = inventory.SelectedDailyV1Owner != null
            ? inventory.SelectedDailyV1Owner.Items.Count
            : inventory.Items.Count;
        if (!inventory.TryGetCensusCardinality(out int inventoryCardinality)
            || inventoryCardinality != liveInventoryCardinality
            || inventory.Revision < 0L || account.Revision < 0L
            || (float.IsNaN(account.Balance) || float.IsInfinity(account.Balance))
            || account.Balance < 0f
            || npc.LifeStateRevision < 0L || npc.ResidenceRevision < 0L
            || npc.P12CrimeJusticeRevision < 0L || npc.CurrentActionRevision < 0L
            || npc.TravelStateRevision < 0L || npc.HiddenDaysRemaining < 0
            || !Enum.IsDefined(typeof(NpcLifeState), npc.LifeState)
            || !NpcInjuryRules.IsValid(npc.InjurySeverity)
            || npc.TravelDaysRemaining < 0 || npc.TravelDaysTotal < 0
            || (npc.TravelDaysTotal > 0 && npc.TravelDaysRemaining > npc.TravelDaysTotal)
            || !npc.HasConsistentCurrentActionSlot
            || !npc.HasConsistentSelectedTravelState)
        {
            failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
            return false;
        }

        int currentActionCardinality = npc.UsesSelectedDailyV1Definitions
            ? (npc.SelectedRuntimeOwners?.ActionSlot?.CurrentAction == null ? 0 : 1)
            : npc.CurrentActionRuntime == null ? 0 : 1;
        if (!TryRequireNpcSections(npc, token, inventory, account, merchantPlanAuthority,
                merchantPlan.Revision,
                travelPlan, commercial, spatial, siteKnowledge, localTopology, adventure,
                currentActionCardinality,
                out failure))
            return false;

        PersonId personId = npc.PersonId;
        PersonRuntime boundPerson = npc.BoundPersonRuntime;
        if ((personId == null) != (boundPerson == null)
            || (boundPerson != null
                && (boundPerson.PersonId != personId
                    || !string.Equals(boundPerson.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal))))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidPersonBinding;
            return false;
        }

        if (npc.CurrentCity != null)
        {
            if (npc.CurrentCity.Location == null || !ReferenceEquals(npc.CurrentLocation, npc.CurrentCity.Location)
                || !TryRequireSection(token, CityNpcPresenceCensusProvider.SectionIdFor(npc.CurrentCity.RuntimeId),
                    CityNpcPresenceCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                    npc.CurrentCity, npc.CurrentCity.ImportantNpcs.Count,
                    npc.CurrentCity.ImportantNpcRevision))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidCityMembership;
                return false;
            }
        }

        if (!npc.UsesSelectedDailyV1Definitions)
        {
            foreach (NpcStatusData status in statuses)
            {
                if (status == null || string.IsNullOrWhiteSpace(status.DefinitionId))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidDefinition;
                    return false;
                }
                statusIds.Add(status.DefinitionId);
            }
        }

        List<P12DNpcInventoryRowValue> inventoryRows =
            new List<P12DNpcInventoryRowValue>(liveInventoryCardinality);
        HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
        if (inventory.SelectedDailyV1Owner != null)
        {
            DailyV1InventoryOwner selectedInventory = inventory.SelectedDailyV1Owner;
            foreach (DailyV1InventoryItemState item in selectedInventory.Items)
            {
                if (item == null || item.Item == null || string.IsNullOrWhiteSpace(item.Item.DefinitionId)
                    || !selectedInventory.Definitions.Items.TryGetValue(item.Item.DefinitionId,
                        out DailyV1ItemDefinition admittedItem)
                    || !ReferenceEquals(admittedItem, item.Item)
                    || item.Amount < 0 || float.IsNaN(item.AverageUnitCost)
                    || float.IsInfinity(item.AverageUnitCost) || item.AverageUnitCost < 0f
                    || !itemIds.Add(item.Item.DefinitionId))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                    return false;
                }
                inventoryRows.Add(new P12DNpcInventoryRowValue(
                    item.Item.DefinitionId, item.Amount, item.AverageUnitCost));
            }
        }
        else
        {
            foreach (InventoryItemRuntime item in inventory.Items)
            {
                if (item == null || item.Item == null || string.IsNullOrWhiteSpace(item.Item.DefinitionId)
                    || item.Amount < 0 || (float.IsNaN(item.AverageUnitCost) || float.IsInfinity(item.AverageUnitCost)) || item.AverageUnitCost < 0f
                    || !itemIds.Add(item.Item.DefinitionId))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                    return false;
                }
                inventoryRows.Add(new P12DNpcInventoryRowValue(
                    item.Item.DefinitionId, item.Amount, item.AverageUnitCost));
            }
        }

        if (!commercial.TryCopyOwnerSnapshot(
                out IReadOnlyList<CommercialMarketObservation> markets,
                out IReadOnlyList<CommercialLiquidityObservation> liquidity,
                out IReadOnlyList<CommercialKnowledgeShareReceipt> receipts))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidOwner;
            return false;
        }

        List<P12DNpcCommercialMarketValue> marketValues =
            new List<P12DNpcCommercialMarketValue>(markets.Count);
        foreach (CommercialMarketObservation observation in markets)
        {
            if (observation == null || observation.ItemDefinition == null
                || string.IsNullOrWhiteSpace(observation.ItemDefinitionId)
                || !string.Equals(observation.ItemDefinition.DefinitionId,
                    observation.ItemDefinitionId, StringComparison.Ordinal))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidDefinition;
                return false;
            }
            marketValues.Add(new P12DNpcCommercialMarketValue(observation));
        }

        List<P12DNpcCommercialLiquidityValue> liquidityValues =
            new List<P12DNpcCommercialLiquidityValue>(liquidity.Count);
        foreach (CommercialLiquidityObservation observation in liquidity)
        {
            if (observation == null)
            {
                failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                return false;
            }
            liquidityValues.Add(new P12DNpcCommercialLiquidityValue(observation));
        }

        P12DNpcActionValue actionValue = null;
        if (npc.UsesSelectedDailyV1Definitions)
        {
            DailyV1NpcActionSlotOwner actionSlot = npc.SelectedRuntimeOwners?.ActionSlot;
            if (actionSlot == null
                || !actionSlot.TryCapturePayload(out DailyV1NpcActionPayloadSnapshot snapshot))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                return false;
            }
            if (snapshot != null)
            {
                NpcActionRuntime projection = npc.CurrentActionRuntime;
                if (projection == null
                    || !ReferenceEquals(projection.SelectedOwner, snapshot.Owner)
                    || !projection.MatchesSelectedOwner()
                    || !actionOwners.Add(projection)
                    || !selectedActionPayloadOwners.Add(snapshot.Owner))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                    return false;
                }
                actionValue = new P12DNpcActionValue(
                    snapshot.ActionDefinitionId,
                    snapshot.TargetNpcRuntimeId,
                    snapshot.TargetCityRuntimeId,
                    snapshot.TargetItemDefinitionId,
                    snapshot.Amount,
                    snapshot.ExpectedUnitPrice,
                    snapshot.SuccessChanceMultiplier,
                    snapshot.TravelReason,
                    snapshot.ExpectedNetValue,
                    snapshot.OriginDecisionId,
                    snapshot.StableOccurrenceKey,
                    snapshot.CommercialDecisionEvidence,
                    snapshot.CommercialScoutingEvidence);
            }
            else if (npc.CurrentActionRuntime != null)
            {
                failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                return false;
            }
        }
        else
        {
            NpcActionRuntime action = npc.CurrentActionRuntime;
            if (action != null)
            {
                if (action.Action == null || string.IsNullOrWhiteSpace(action.Action.DefinitionId)
                    || !actionOwners.Add(action) || action.Amount < 0
                    || !IsFiniteNonNegative(action.ExpectedUnitPrice)
                    || !IsFiniteNonNegative(action.SuccessChanceMultiplier)
                    || float.IsNaN(action.ExpectedNetValue) || float.IsInfinity(action.ExpectedNetValue)
                    || !Enum.IsDefined(typeof(NpcTravelReason), action.TravelReason))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                    return false;
                }
                actionValue = new P12DNpcActionValue(
                    action.Action.DefinitionId,
                    action.TargetNpc?.RuntimeId,
                    action.TargetCity?.RuntimeId,
                    action.TargetItem?.DefinitionId,
                    action.Amount,
                    action.ExpectedUnitPrice,
                    action.SuccessChanceMultiplier,
                    action.TravelReason,
                    action.ExpectedNetValue,
                    action.OriginDecisionId,
                    action.StableOccurrenceKey,
                    action.CommercialDecisionEvidence,
                    action.CommercialScoutingEvidence);
            }
        }

        NpcTravelPlanState travelPlanState = travelPlan.CaptureOwnerState();
        MerchantTradePlanState merchantPlanState = merchantPlan.CaptureOwnerState();
        if (travelPlanState == null || merchantPlanState == null
            || travelPlan.Revision < 0L || merchantPlan.Revision < 0L)
        {
            failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
            return false;
        }

        P12DNpcTravelPlanValue travelPlanValue = new P12DNpcTravelPlanValue(
            travelPlanState.TargetLocation?.RuntimeId,
            travelPlanState.TargetCity?.RuntimeId,
            travelPlanState.Reason,
            travelPlanState.Utility,
            travelPlanState.ExpectedCost,
            travelPlanState.OriginDecisionId,
            travelPlan.Revision);
        P12DNpcMerchantPlanValue merchantPlanValue = new P12DNpcMerchantPlanValue(
            merchantPlanState.Item?.DefinitionId,
            merchantPlanState.OriginCity?.RuntimeId,
            merchantPlanState.TargetCity?.RuntimeId,
            merchantPlanState.PlannedAmount,
            merchantPlanState.RawRemainingAmount,
            merchantPlanState.PurchasePricePerItem,
            merchantPlanState.WaitDaysAtDestination,
            merchantPlanState.PendingTravelDays,
            merchantPlanState.OriginDecisionId,
            merchantPlan.Revision);

        List<ExplorableSiteKnowledgeObservation> siteObservations =
            new List<ExplorableSiteKnowledgeObservation>(siteKnowledge.Observations.Count);
        foreach (ExplorableSiteKnowledgeObservation observation in siteKnowledge.Observations)
        {
            if (observation == null)
            {
                failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                return false;
            }
            siteObservations.Add(new ExplorableSiteKnowledgeObservation(
                observation.SiteRuntimeId, observation.LocationRuntimeId,
                observation.ObservedDay, observation.ReceivedDay, observation.Source));
        }

        if (!commercial.TryReadCensus(out _, out _, out _, out long commercialRevision)
            || commercialRevision != commercial.Revision
            || siteKnowledge.Revision < 0L || spatial.Revision < 0L || adventure.Revision < 0L)
        {
            failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
            return false;
        }

        dRow = new P12DNpcDRow(
            npc.RuntimeId, npc.NpcData.DefinitionId, personId?.Value,
            npc.ResidenceSettlementRuntimeId, npc.LifeState, npc.InjurySeverity,
            statusIds, npc.HiddenDaysRemaining,
            npc.UsesSelectedDailyV1Definitions
                ? npc.SelectedRuntimeOwners.ActionSlot.CurrentAction?.DefinitionId
                : npc.CurrentAction?.DefinitionId,
            npc.CurrentCity?.RuntimeId, npc.CurrentLocation?.RuntimeId,
            npc.DestinationCity?.RuntimeId, npc.DestinationLocation?.RuntimeId,
            inventoryRows, inventory.Revision, account.Balance, account.Revision,
            npc.LifeStateRevision, npc.ResidenceRevision, npc.P12CrimeJusticeRevision,
            npc.CurrentActionRevision);

        fRow = new P12DNpcFRow(
            npc.RuntimeId, actionValue, npc.TravelDaysRemaining, npc.TravelDaysTotal,
            npc.TravelRouteRuntimeId, npc.TravelStartedToday, npc.TravelOriginDecisionId,
            npc.ActiveTravelPartyId, npc.TravelStateRevision, merchantPlanValue,
            travelPlanValue, siteObservations, siteKnowledge.Revision,
            new List<string>(spatial.KnownLocationRuntimeIds),
            new List<string>(spatial.KnownRouteRuntimeIds), spatial.Revision,
            adventure.OppositionObservations, adventure.NotableItemObservations,
            adventure.CommonResourceObservations, adventure.AccessObservations, adventure.Revision,
            marketValues, liquidityValues, receipts, commercial.Revision);
        failure = P12DNpcRootOwnerSnapshotFailure.None;
        return true;
    }


    internal static bool TryStage(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        P12DNpcDProjection dProjection,
        P12DNpcFProjection fProjection,
        IReadOnlyList<P12DCityMembershipLinker> cityMembershipLinkers,
        PersonStore stagedPersonStore,
        IReadOnlyList<NpcData> npcDefinitions,
        IReadOnlyList<NpcActionData> actionDefinitions,
        IReadOnlyList<NpcStatusData> statusDefinitions,
        IReadOnlyList<ItemData> itemDefinitions,
        IReadOnlyList<SpatialLocationRuntime> stagedLocations,
        IReadOnlyList<string> stagedRouteRuntimeIds,
        IReadOnlyList<string> stagedTravelPartyIds,
        out IReadOnlyList<NpcRuntime> stagedNpcRoster,
        out P12DNpcRootOwnerSnapshotFailure failure,
        DailyV1EffectiveDefinitions selectedDefinitions = null,
        DailyCaptureStagingAttempt stagingAttempt = null)
        => TryStageCore(sourceRuntime, token, dProjection, fProjection,
            cityMembershipLinkers, stagedPersonStore, npcDefinitions, actionDefinitions,
            statusDefinitions, itemDefinitions, stagedLocations, stagedRouteRuntimeIds,
            stagedTravelPartyIds, true, out stagedNpcRoster, out failure, selectedDefinitions, stagingAttempt);

    internal static bool TryStageForImage(
        IReadOnlyList<P12DNpcDRow> dRows,
        IReadOnlyList<P12DNpcFRow> fRows,
        IReadOnlyList<P12DCityMembershipLinker> cityMembershipLinkers,
        PersonStore stagedPersonStore,
        IReadOnlyList<NpcData> npcDefinitions,
        IReadOnlyList<NpcActionData> actionDefinitions,
        IReadOnlyList<NpcStatusData> statusDefinitions,
        IReadOnlyList<ItemData> itemDefinitions,
        IReadOnlyList<SpatialLocationRuntime> stagedLocations,
        IReadOnlyList<string> stagedRouteRuntimeIds,
        IReadOnlyList<string> stagedTravelPartyIds,
        IReadOnlyList<P12AOwnerSection> ownerSections,
        out IReadOnlyList<NpcRuntime> stagedNpcRoster,
        out P12DNpcRootOwnerSnapshotFailure failure,
        DailyV1EffectiveDefinitions selectedDefinitions = null)
    {
        stagedNpcRoster = null;
        failure = P12DNpcRootOwnerSnapshotFailure.InvalidCaptureEvidence;
        if (dRows == null || fRows == null || ownerSections == null) return false;
        Dictionary<string, P12AOwnerSection> byId = new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in ownerSections)
            if (section == null || !byId.TryAdd(section.Id, section)) return false;
        foreach (P12DNpcDRow row in dRows)
        {
            if (row == null || !byId.TryGetValue(
                    P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(row.RuntimeId),
                    out P12AOwnerSection local)
                || !byId.TryGetValue(
                    P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(row.RuntimeId),
                    out P12AOwnerSection merchant)
                || !P12ANpcReceiptEmptyOwnerCodec.TryDecode(local, merchant, out string decodedRuntimeId, out _)
                || !string.Equals(decodedRuntimeId, row.RuntimeId, StringComparison.Ordinal))
                return false;
        }
        return TryStageCore(null, null,
            new P12DNpcDProjection(null, null, null, null, dRows),
            new P12DNpcFProjection(null, null, null, null, fRows),
            cityMembershipLinkers, stagedPersonStore, npcDefinitions, actionDefinitions,
            statusDefinitions, itemDefinitions, stagedLocations, stagedRouteRuntimeIds,
            stagedTravelPartyIds, false, out stagedNpcRoster, out failure, selectedDefinitions, null);
    }

    private static bool TryStageCore(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        P12DNpcDProjection dProjection,
        P12DNpcFProjection fProjection,
        IReadOnlyList<P12DCityMembershipLinker> cityMembershipLinkers,
        PersonStore stagedPersonStore,
        IReadOnlyList<NpcData> npcDefinitions,
        IReadOnlyList<NpcActionData> actionDefinitions,
        IReadOnlyList<NpcStatusData> statusDefinitions,
        IReadOnlyList<ItemData> itemDefinitions,
        IReadOnlyList<SpatialLocationRuntime> stagedLocations,
        IReadOnlyList<string> stagedRouteRuntimeIds,
        IReadOnlyList<string> stagedTravelPartyIds,
        bool requireCaptureEvidence,
        out IReadOnlyList<NpcRuntime> stagedNpcRoster,
        out P12DNpcRootOwnerSnapshotFailure failure,
        DailyV1EffectiveDefinitions selectedDefinitions = null,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        stagedNpcRoster = null;
        failure = P12DNpcRootOwnerSnapshotFailure.InvalidCaptureEvidence;
        if ((requireCaptureEvidence && (sourceRuntime == null || token == null))
            || (!requireCaptureEvidence && (sourceRuntime != null || token != null))
            || dProjection == null || fProjection == null
            || cityMembershipLinkers == null || stagedPersonStore == null
            || npcDefinitions == null || actionDefinitions == null || statusDefinitions == null
            || itemDefinitions == null || stagedLocations == null
            || stagedRouteRuntimeIds == null || stagedTravelPartyIds == null
            || (requireCaptureEvidence && (!ReferenceEquals(dProjection.Token, token) || !ReferenceEquals(fProjection.Token, token)
                || !ReferenceEquals(dProjection.OwnerSectionVector, token.OwnerSections)
                || !ReferenceEquals(fProjection.OwnerSectionVector, token.OwnerSections)
                || !ReferenceEquals(dProjection.CaptureStamp, fProjection.CaptureStamp)
                || !ReferenceEquals(dProjection.OwnerSectionVector, fProjection.OwnerSectionVector)
                || dProjection.Evidence == null || !dProjection.Evidence.HasSameCaptureIdentity(fProjection.Evidence)))
            || (!requireCaptureEvidence && (dProjection.Token != null || fProjection.Token != null
                || dProjection.OwnerSectionVector != null || fProjection.OwnerSectionVector != null
                || dProjection.Evidence != null || fProjection.Evidence != null))
            || dProjection.Rows == null || fProjection.Rows == null
            || dProjection.Rows.Count != fProjection.Rows.Count
            || (requireCaptureEvidence && !DailyCaptureStagingAttempt.IsCaptureCurrent(
                sourceRuntime, token, token?.OwnerSections, stagingAttempt)))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.StaleCapture;
            return false;
        }

        if (!TryBuildUniqueMap(npcDefinitions, value => value?.DefinitionId, out Dictionary<string, NpcData> npcsByDefinition)
            || !TryBuildUniqueMap(actionDefinitions, value => value?.DefinitionId, out Dictionary<string, NpcActionData> actionsByDefinition)
            || !TryBuildUniqueMap(statusDefinitions, value => value?.DefinitionId, out Dictionary<string, NpcStatusData> statusesByDefinition)
            || !TryBuildUniqueMap(itemDefinitions, value => value?.DefinitionId, out Dictionary<string, ItemData> itemsByDefinition)
            || !TryBuildUniqueMap(stagedLocations, value => value?.RuntimeId, out Dictionary<string, SpatialLocationRuntime> locationsById))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidDefinitionMap;
            return false;
        }

        DailyV1DefinitionAdapter.SelectedBindings selectedItemBindings = null;
        if (selectedDefinitions != null
            && !DailyV1DefinitionAdapter.SelectedBindings.TryCreate(
                itemDefinitions.Cast<ScriptableObject>(), selectedDefinitions,
                out selectedItemBindings, out _))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedItemBindings;
            return false;
        }

        Dictionary<string, CityRuntime> citiesById = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
        for (int i = 0; i < cityMembershipLinkers.Count; i++)
        {
            P12DCityMembershipLinker linker = cityMembershipLinkers[i];
            CityRuntime city = linker?.StagedCity;
            if (linker == null || city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || city.Location == null || !citiesById.TryAdd(city.RuntimeId, city))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidCityMembership;
                return false;
            }
        }

        if (!TryBuildUniqueIdSet(stagedRouteRuntimeIds, out HashSet<string> routeIds)
            || !TryBuildUniqueIdSet(stagedTravelPartyIds, out HashSet<string> partyIds))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
            return false;
        }

        // Validate the complete row identity set first so Core action payloads
        // can resolve stable NPC ids without constructing Unity NpcRuntime
        // projections. Selected Core owners are then hydrated before any NPC
        // wrapper or action projection is created.
        HashSet<string> stagedNpcRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < dProjection.Rows.Count; i++)
        {
            P12DNpcDRow d = dProjection.Rows[i];
            P12DNpcFRow f = fProjection.Rows[i];
            if (d == null || f == null
                || !string.Equals(d.RuntimeId, f.RuntimeId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(d.RuntimeId)
                || !stagedNpcRuntimeIds.Add(d.RuntimeId)
                || (d.CurrentActionDefinitionId == null) != (f.Action == null)
                || (f.Action != null && !string.Equals(d.CurrentActionDefinitionId,
                    f.Action.ActionDefinitionId, StringComparison.Ordinal)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidNpcIdentity;
                return false;
            }
        }

        Dictionary<string, P12DNpcPreparedSelectedCoreOwner> preparedSelectedOwners =
            new Dictionary<string, P12DNpcPreparedSelectedCoreOwner>(StringComparer.Ordinal);
        if (selectedDefinitions != null)
        {
            for (int i = 0; i < dProjection.Rows.Count; i++)
            {
                P12DNpcDRow d = dProjection.Rows[i];
                P12DNpcFRow f = fProjection.Rows[i];
                if (!TryPrepareSelectedCoreOwner(d, f, selectedDefinitions,
                        actionsByDefinition, statusesByDefinition, itemsByDefinition,
                        locationsById, citiesById, routeIds, stagedNpcRuntimeIds,
                        selectedItemBindings,
                        out P12DNpcPreparedSelectedCoreOwner prepared, out failure)
                    || prepared == null
                    || !preparedSelectedOwners.TryAdd(d.RuntimeId, prepared))
                {
                    if (failure == P12DNpcRootOwnerSnapshotFailure.None)
                        failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                    return false;
                }
            }
        }

        List<NpcRuntime> candidates = new List<NpcRuntime>(dProjection.Rows.Count);
        Dictionary<string, NpcRuntime> candidatesById =
            new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);

        // Selected Core owners were hydrated above, before any Unity NPC
        // projection. Now create every root so action projections can resolve
        // typed NPC references only to this private staged roster.
        for (int i = 0; i < dProjection.Rows.Count; i++)
        {
            P12DNpcDRow d = dProjection.Rows[i];
            P12DNpcFRow f = fProjection.Rows[i];
            if (d == null || f == null || !string.Equals(d.RuntimeId, f.RuntimeId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(d.RuntimeId) || !candidatesById.TryAdd(d.RuntimeId, null)
                || (d.CurrentActionDefinitionId == null) != (f.Action == null)
                || (f.Action != null && !string.Equals(d.CurrentActionDefinitionId,
                    f.Action.ActionDefinitionId, StringComparison.Ordinal)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidNpcIdentity;
                return false;
            }

            if (!npcsByDefinition.TryGetValue(d.NpcDefinitionId, out NpcData npcData)
                || npcData == null || !string.Equals(npcData.DefinitionId, d.NpcDefinitionId, StringComparison.Ordinal))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidNpcDefinition;
                return false;
            }

            DailyV1NpcDefinition selectedNpcDefinition = null;
            if (selectedDefinitions != null
                && (!selectedDefinitions.Npcs.TryGetValue(d.NpcDefinitionId, out selectedNpcDefinition)
                    || selectedNpcDefinition == null
                    || !string.Equals(selectedNpcDefinition.DefinitionId, d.NpcDefinitionId, StringComparison.Ordinal)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedNpcDefinition;
                return false;
            }

            if (!ResolveNullable(d.CurrentCityRuntimeId, citiesById, out CityRuntime currentCity)
                || !ResolveNullable(d.CurrentLocationRuntimeId, locationsById, out SpatialLocationRuntime currentLocation)
                || !ResolveNullable(d.DestinationCityRuntimeId, citiesById, out CityRuntime destinationCity)
                || !ResolveNullable(d.DestinationLocationRuntimeId, locationsById, out SpatialLocationRuntime destinationLocation)
                || (currentCity != null && !ReferenceEquals(currentCity.Location, currentLocation))
                || (destinationCity != null && !ReferenceEquals(destinationCity.Location, destinationLocation))
                || (d.ResidenceSettlementRuntimeId != null
                    && !citiesById.ContainsKey(d.ResidenceSettlementRuntimeId)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                return false;
            }

            NpcRuntime npc = null;
            bool npcCreated = selectedDefinitions != null
                ? preparedSelectedOwners.TryGetValue(d.RuntimeId,
                    out P12DNpcPreparedSelectedCoreOwner prepared)
                    && NpcRuntime.TryCreateForStagedPresence(
                        d.RuntimeId, npcData, currentCity, currentLocation,
                        prepared.RuntimeOwners, out npc)
                : NpcRuntime.TryCreateForStagedPresence(
                    d.RuntimeId, npcData, currentCity, currentLocation, out npc);
            if (!npcCreated || npc == null)
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidNpcIdentity;
                return false;
            }
            candidatesById[d.RuntimeId] = npc;
            candidates.Add(npc);
        }

        for (int i = 0; i < dProjection.Rows.Count; i++)
        {
            P12DNpcDRow d = dProjection.Rows[i];
            P12DNpcFRow f = fProjection.Rows[i];
            NpcRuntime stagedNpc = candidatesById[d.RuntimeId];
            P12DNpcPreparedSelectedCoreOwner preparedCoreOwner = selectedDefinitions == null
                ? null : preparedSelectedOwners[d.RuntimeId];
            DailyV1NpcDefinition selectedNpcDefinition = null;
            if (selectedDefinitions != null
                && (!selectedDefinitions.Npcs.TryGetValue(d.NpcDefinitionId, out selectedNpcDefinition)
                    || selectedNpcDefinition == null
                    || !string.Equals(selectedNpcDefinition.DefinitionId, d.NpcDefinitionId, StringComparison.Ordinal)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedNpcDefinition;
                return false;
            }

            PersonRuntime stagedPerson = null;
            if (d.PersonIdValue != null)
            {
                if (!PersonId.TryCreate(d.PersonIdValue, out PersonId personId)
                    || !stagedPersonStore.TryGet(personId, out stagedPerson)
                    || stagedPerson == null
                    || !string.Equals(stagedPerson.MaterializedNpcRuntimeId, d.RuntimeId, StringComparison.Ordinal)
                    || !stagedPersonStore.TryGetByMaterializedNpcRuntimeId(d.RuntimeId, out PersonRuntime indexedPerson)
                    || !ReferenceEquals(indexedPerson, stagedPerson)
                    || !string.Equals(stagedPerson.ResidenceSettlementRuntimeId,
                        d.ResidenceSettlementRuntimeId, StringComparison.Ordinal))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidPersonBinding;
                    return false;
                }
            }
            else if (stagedPersonStore.TryGetByMaterializedNpcRuntimeId(d.RuntimeId, out _))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidPersonBinding;
                return false;
            }

            List<NpcStatusData> statuses = new List<NpcStatusData>(d.CurrentStatusDefinitionIds.Count);
            List<DailyV1StatusDefinition> selectedStatusDefinitions = selectedDefinitions == null
                ? null
                : new List<DailyV1StatusDefinition>(d.CurrentStatusDefinitionIds.Count);
            foreach (string statusId in d.CurrentStatusDefinitionIds)
            {
                if (string.IsNullOrWhiteSpace(statusId)
                    || !statusesByDefinition.TryGetValue(statusId, out NpcStatusData status))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidStatusDefinition;
                    return false;
                }
                if (selectedDefinitions != null)
                {
                    if (!selectedDefinitions.Statuses.TryGetValue(statusId,
                            out DailyV1StatusDefinition selectedStatus)
                        || selectedStatus == null
                        || !string.Equals(selectedStatus.DefinitionId, status.DefinitionId, StringComparison.Ordinal))
                    {
                        failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedStatusDefinition;
                        return false;
                    }
                    selectedStatusDefinitions.Add(selectedStatus);
                }
                statuses.Add(status);
            }

            List<InventoryItemRuntime> inventoryRows =
                selectedDefinitions == null ? new List<InventoryItemRuntime>(d.InventoryRows.Count) : null;
            List<DailyV1InventoryItemState> selectedInventoryRows = selectedDefinitions != null
                ? new List<DailyV1InventoryItemState>(d.InventoryRows.Count) : null;
            foreach (P12DNpcInventoryRowValue row in d.InventoryRows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.ItemDefinitionId))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidInventoryItem;
                    return false;
                }
                if (selectedDefinitions != null)
                {
                    if (!selectedDefinitions.Items.TryGetValue(row.ItemDefinitionId,
                            out DailyV1ItemDefinition itemNode)
                        || itemNode == null || row.Amount < 0
                        || float.IsNaN(row.AverageUnitCost) || float.IsInfinity(row.AverageUnitCost)
                        || row.AverageUnitCost < 0f)
                    {
                        failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedInventoryItem;
                        return false;
                    }
                    selectedInventoryRows.Add(new DailyV1InventoryItemState(
                        itemNode, row.Amount, row.AverageUnitCost));
                }
                else
                {
                    if (!itemsByDefinition.TryGetValue(row.ItemDefinitionId, out ItemData item))
                    {
                        failure = P12DNpcRootOwnerSnapshotFailure.InvalidInventoryItem;
                        return false;
                    }
                    inventoryRows.Add(new InventoryItemRuntime(item, row.Amount, row.AverageUnitCost));
                }
            }
            InventoryRuntime inventory = null;
            MoneyAccountRuntime moneyAccount = null;
            if (selectedDefinitions != null)
            {
                if (preparedCoreOwner?.RuntimeOwners?.Inventory == null
                    || preparedCoreOwner.RuntimeOwners.Inventory.Revision != d.InventoryRevision
                    || !MatchesSelectedInventoryRows(
                        preparedCoreOwner.RuntimeOwners.Inventory, selectedInventoryRows))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedInventoryItem;
                    return false;
                }
                inventory = preparedCoreOwner.InventoryProjection;
                moneyAccount = preparedCoreOwner.RuntimeOwners.MoneyAccount;
            }
            else if (!InventoryRuntime.TryCreateFromOwnerSnapshot(
                    inventoryRows, d.InventoryRevision, out inventory)
                || !MoneyAccountOwnerBridge.TryCreateFromOwnerSnapshot(
                    d.MoneyBalance, d.MoneyRevision, out moneyAccount))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
                return false;
            }

            P12DNpcMerchantPlanValue merchantValue = f.MerchantPlan;
            ItemData merchantItem = null;
            DailyV1ItemDefinition selectedMerchantItem = null;
            CityRuntime merchantOriginCity = null;
            CityRuntime merchantTargetCity = null;
            if (merchantValue == null
                || (merchantValue.ItemDefinitionId != null
                    && !itemsByDefinition.TryGetValue(merchantValue.ItemDefinitionId, out merchantItem))
                || (selectedDefinitions != null && merchantValue.ItemDefinitionId != null
                    && (!selectedDefinitions.Items.TryGetValue(merchantValue.ItemDefinitionId,
                            out selectedMerchantItem)
                        || selectedMerchantItem == null
                        || !string.Equals(selectedMerchantItem.DefinitionId,
                            merchantValue.ItemDefinitionId, StringComparison.Ordinal)))
                || !ResolveNullable(merchantValue.OriginCityRuntimeId, citiesById, out merchantOriginCity)
                || !ResolveNullable(merchantValue.TargetCityRuntimeId, citiesById, out merchantTargetCity)
                || !MerchantTradePlanRuntime.TryCreateFromOwnerSnapshot(
                    merchantItem, merchantOriginCity, merchantTargetCity,
                    merchantValue.PlannedAmount, merchantValue.RawRemainingAmount,
                    merchantValue.PurchasePricePerItem, merchantValue.WaitDaysAtDestination,
                    merchantValue.PendingTravelDays, merchantValue.OriginDecisionId,
                    merchantValue.Revision, out MerchantTradePlanRuntime stagedMerchantPlan))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                return false;
            }

            DailyV1NpcMerchantTradePlanOwner selectedMerchantPlanOwner = null;
            if (selectedDefinitions != null)
            {
                selectedMerchantPlanOwner = preparedCoreOwner.RuntimeOwners.MerchantTradePlan;
                if (selectedMerchantPlanOwner == null
                    || selectedMerchantPlanOwner.Revision != merchantValue.Revision
                    || !selectedMerchantPlanOwner.MatchesState(
                        selectedMerchantItem, merchantValue.OriginCityRuntimeId,
                        merchantValue.TargetCityRuntimeId, merchantValue.PlannedAmount,
                        merchantValue.RawRemainingAmount, merchantValue.PurchasePricePerItem,
                        merchantValue.WaitDaysAtDestination, merchantValue.PendingTravelDays,
                        merchantValue.OriginDecisionId))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                    return false;
                }

                if (selectedItemBindings == null
                    || !stagedMerchantPlan.TryBindSelectedOwner(
                        selectedMerchantPlanOwner, selectedItemBindings,
                        cityRuntimeId => cityRuntimeId != null
                            && citiesById.TryGetValue(cityRuntimeId, out CityRuntime selectedCity)
                            ? selectedCity : null))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                    return false;
                }
            }

            P12DNpcTravelPlanValue travelValue = f.TravelPlan;
            if (travelValue == null
                || !ResolveNullable(travelValue.TargetLocationRuntimeId, locationsById, out SpatialLocationRuntime planLocation)
                || !ResolveNullable(travelValue.TargetCityRuntimeId, citiesById, out CityRuntime planCity)
                || !NpcTravelPlanRuntime.TryCreateFromOwnerSnapshot(
                    planLocation, planCity, travelValue.Reason, travelValue.Utility,
                    travelValue.ExpectedCost, travelValue.OriginDecisionId, travelValue.Revision,
                    out NpcTravelPlanRuntime stagedTravelPlan))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                return false;
            }

            CommercialKnowledgeRuntime stagedCommercial = null;
            SpatialKnowledgeRuntime stagedSpatial = null;
            if (selectedDefinitions != null)
            {
                stagedCommercial = preparedCoreOwner.CommercialKnowledgeProjection;
                stagedSpatial = preparedCoreOwner.SpatialKnowledge;
            }
            if ((selectedDefinitions == null
                    && !TryCreateCommercialKnowledge(f, itemsByDefinition, null, null,
                        out stagedCommercial))
                || stagedCommercial == null
                || (selectedDefinitions != null
                    && !ReferenceEquals(stagedCommercial.SelectedOwner,
                        preparedCoreOwner.RuntimeOwners.CommercialKnowledge)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                return false;
            }

            if (!ExplorableSiteKnowledgeRuntime.TryCreateFromOwnerSnapshot(
                    d.RuntimeId, f.SiteObservations, f.SiteKnowledgeRevision,
                    out ExplorableSiteKnowledgeRuntime stagedSiteKnowledge)
                || (selectedDefinitions == null && !SpatialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
                    d.RuntimeId, f.KnownLocationRuntimeIds, f.KnownRouteRuntimeIds,
                    f.SpatialKnowledgeRevision, out stagedSpatial))
                || (stagedSpatial == null
                    || stagedSpatial.Revision != f.SpatialKnowledgeRevision
                    || !string.Equals(stagedSpatial.OwnerRuntimeId, d.RuntimeId, StringComparison.Ordinal))
                || !AdventureSiteIntelKnowledgeRuntime.TryCreateFromOwnerSnapshot(
                    d.RuntimeId, f.OppositionObservations, f.NotableItemObservations,
                    f.CommonResourceObservations, f.AccessObservations,
                    f.AdventureKnowledgeRevision, out AdventureSiteIntelKnowledgeRuntime stagedAdventure)
                || f.KnownLocationRuntimeIds.Any(id => !locationsById.ContainsKey(id))
                || f.KnownRouteRuntimeIds.Any(id => !routeIds.Contains(id))
                || (f.TravelRouteRuntimeId != null && !routeIds.Contains(f.TravelRouteRuntimeId))
                || (f.ActiveTravelPartyId != null && !partyIds.Contains(f.ActiveTravelPartyId)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                return false;
            }

            NpcActionRuntime actionRuntime = null;
            string actionDefinitionId = null;
            if (f.Action != null)
            {
                P12DNpcActionValue actionValue = f.Action;
                if (!actionsByDefinition.TryGetValue(actionValue.ActionDefinitionId, out NpcActionData actionDefinition)
                    || !ResolveNullable(actionValue.TargetNpcRuntimeId, candidatesById, out NpcRuntime targetNpc)
                    || !ResolveNullable(actionValue.TargetCityRuntimeId, citiesById, out CityRuntime targetCity)
                    || (actionValue.TargetItemDefinitionId != null
                        && !itemsByDefinition.TryGetValue(actionValue.TargetItemDefinitionId, out _))
                    || !TryValidateActionEvidence(actionValue, locationsById, routeIds, itemsByDefinition)
                    || !NpcActionRuntime.TryCreateFromOwnerSnapshot(
                        actionDefinition, targetNpc, targetCity,
                        actionValue.TargetItemDefinitionId == null ? null : itemsByDefinition[actionValue.TargetItemDefinitionId],
                        actionValue.Amount, actionValue.ExpectedUnitPrice,
                        actionValue.SuccessChanceMultiplier, actionValue.TravelReason,
                        actionValue.ExpectedNetValue, actionValue.OriginDecisionId,
                        actionValue.StableOccurrenceKey, actionValue.CommercialDecisionEvidence,
                        actionValue.CommercialScoutingEvidence, out actionRuntime))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                    return false;
                }
                if (preparedCoreOwner != null
                    && (preparedCoreOwner.CurrentActionPayload == null
                        || !actionRuntime.TryUseSelectedPayload(
                            preparedCoreOwner.CurrentActionPayload, selectedDefinitions)))
                {
                    failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                    return false;
                }
                actionDefinitionId = actionValue.ActionDefinitionId;
            }

            P12DNpcStagedOwnerState stagedState = new P12DNpcStagedOwnerState
            {
                NpcDefinitionId = d.NpcDefinitionId,
                SelectedDefinitions = selectedDefinitions,
                SelectedNpcDefinition = selectedNpcDefinition,
                SelectedCurrentStatusDefinitions = selectedStatusDefinitions,
                NpcData = npcsByDefinition[d.NpcDefinitionId],
                PersonIdValue = d.PersonIdValue,
                PersonRuntime = stagedPerson,
                ResidenceSettlementRuntimeId = stagedPerson == null ? d.ResidenceSettlementRuntimeId : null,
                LifeState = d.LifeState,
                InjurySeverity = d.InjurySeverity,
                CurrentStatus = statuses,
                HiddenDaysRemaining = d.HiddenDaysRemaining,
                CurrentActionDefinitionId = actionDefinitionId,
                CurrentActionRuntime = actionRuntime,
                SelectedRuntimeOwners = preparedCoreOwner?.RuntimeOwners,
                SelectedCurrentActionPayload = preparedCoreOwner?.CurrentActionPayload,
                CurrentCity = stagedNpc.CurrentCity,
                CurrentLocation = stagedNpc.CurrentLocation,
                DestinationCity = Resolve(d.DestinationCityRuntimeId, citiesById),
                DestinationLocation = Resolve(d.DestinationLocationRuntimeId, locationsById),
                TravelDaysRemaining = f.TravelDaysRemaining,
                TravelDaysTotal = f.TravelDaysTotal,
                TravelRouteRuntimeId = f.TravelRouteRuntimeId,
                TravelStartedToday = f.TravelStartedToday,
                TravelOriginDecisionId = f.TravelOriginDecisionId,
                ActiveTravelPartyId = f.ActiveTravelPartyId,
                TravelStateRevision = f.TravelStateRevision,
                Inventory = inventory,
                MoneyAccount = moneyAccount,
                MerchantTradePlan = stagedMerchantPlan,
                SelectedMerchantTradePlanOwner = selectedMerchantPlanOwner,
                TravelPlan = stagedTravelPlan,
                CommercialKnowledge = stagedCommercial,
                SpatialKnowledge = stagedSpatial,
                ExplorableSiteKnowledge = stagedSiteKnowledge,
                LocalTopologyKnowledge = new LocalTopologyKnowledgeRuntime(d.RuntimeId),
                AdventureSiteIntelKnowledge = stagedAdventure,
                LifeStateRevision = d.LifeStateRevision,
                ResidenceRevision = d.ResidenceRevision,
                CrimeJusticeRevision = d.CrimeJusticeRevision,
                CurrentActionRevision = d.CurrentActionRevision
            };

            if (!stagedNpc.TryInstallP12OwnerSnapshot(stagedState)
                || (stagedPerson != null && !stagedNpc.TryBindPersonRuntime(stagedPerson)))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidPersonBinding;
                return false;
            }
        }

        if (requireCaptureEvidence && (!DailyCaptureStagingAttempt.IsCaptureCurrent(
                sourceRuntime, token, token?.OwnerSections, stagingAttempt)
            || !dProjection.Evidence.AreReceiptOwnersStillExactZero()))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.StaleCapture;
            return false;
        }

        bool relationsFilled = requireCaptureEvidence
            ? P12DCityNpcRelationAssembler.TryFillMembershipsOnce(
                dProjection.Evidence, fProjection.Evidence, cityMembershipLinkers, candidates,
                out P12DCityNpcRelationAssemblyFailure assemblyFailure)
            : P12DCityNpcRelationAssembler.TryFillMembershipsFromImage(
                cityMembershipLinkers, candidates, out assemblyFailure);
        if (!relationsFilled)
        {
            failure = assemblyFailure == P12DCityNpcRelationAssemblyFailure.InvalidRelation
                || assemblyFailure == P12DCityNpcRelationAssemblyFailure.InvalidCitySet
                || assemblyFailure == P12DCityNpcRelationAssemblyFailure.InvalidNpcRoster
                ? P12DNpcRootOwnerSnapshotFailure.InvalidCityMembership
                : P12DNpcRootOwnerSnapshotFailure.InvalidCaptureEvidence;
            return false;
        }

        if (requireCaptureEvidence && !DailyCaptureStagingAttempt.IsCaptureCurrent(
                sourceRuntime, token, token?.OwnerSections, stagingAttempt))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.StaleCapture;
            return false;
        }

        stagedNpcRoster = new ReadOnlyCollection<NpcRuntime>(candidates);
        failure = P12DNpcRootOwnerSnapshotFailure.None;
        return true;
    }

    private static bool TryPrepareSelectedCoreOwner(
        P12DNpcDRow d,
        P12DNpcFRow f,
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyDictionary<string, NpcActionData> actionsByDefinition,
        IReadOnlyDictionary<string, NpcStatusData> statusesByDefinition,
        IReadOnlyDictionary<string, ItemData> itemsByDefinition,
        IReadOnlyDictionary<string, SpatialLocationRuntime> locationsById,
        IReadOnlyDictionary<string, CityRuntime> citiesById,
        HashSet<string> routeIds,
        HashSet<string> npcRuntimeIds,
        DailyV1DefinitionAdapter.SelectedBindings selectedItemBindings,
        out P12DNpcPreparedSelectedCoreOwner prepared,
        out P12DNpcRootOwnerSnapshotFailure failure)
    {
        prepared = null;
        failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedNpcDefinition;
        if (d == null || f == null || definitions == null
            || !definitions.Npcs.TryGetValue(d.NpcDefinitionId,
                out DailyV1NpcDefinition selectedNpcDefinition)
            || selectedNpcDefinition == null
            || !string.Equals(selectedNpcDefinition.DefinitionId, d.NpcDefinitionId, StringComparison.Ordinal))
            return false;

        List<DailyV1StatusDefinition> selectedStatuses =
            new List<DailyV1StatusDefinition>(d.CurrentStatusDefinitionIds.Count);
        foreach (string statusId in d.CurrentStatusDefinitionIds)
        {
            if (string.IsNullOrWhiteSpace(statusId)
                || !statusesByDefinition.TryGetValue(statusId, out NpcStatusData statusAsset)
                || statusAsset == null
                || !definitions.Statuses.TryGetValue(statusId, out DailyV1StatusDefinition statusNode)
                || statusNode == null
                || !string.Equals(statusNode.DefinitionId, statusAsset.DefinitionId, StringComparison.Ordinal))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedStatusDefinition;
                return false;
            }
            selectedStatuses.Add(statusNode);
        }

        List<DailyV1InventoryItemState> inventoryRows =
            new List<DailyV1InventoryItemState>(d.InventoryRows.Count);
        foreach (P12DNpcInventoryRowValue row in d.InventoryRows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ItemDefinitionId)
                || !itemsByDefinition.TryGetValue(row.ItemDefinitionId, out ItemData itemAsset)
                || itemAsset == null
                || !definitions.Items.TryGetValue(row.ItemDefinitionId, out DailyV1ItemDefinition itemNode)
                || itemNode == null
                || !string.Equals(itemNode.DefinitionId, itemAsset.DefinitionId, StringComparison.Ordinal)
                || row.Amount < 0 || float.IsNaN(row.AverageUnitCost)
                || float.IsInfinity(row.AverageUnitCost) || row.AverageUnitCost < 0f)
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidSelectedInventoryItem;
                return false;
            }
            inventoryRows.Add(new DailyV1InventoryItemState(
                itemNode, row.Amount, row.AverageUnitCost));
        }

        DailyV1InventoryOwner inventoryCandidate = new DailyV1InventoryOwner(definitions);
        if (!inventoryCandidate.TryCreatePreparedFromOwnerSnapshot(
                inventoryRows, d.InventoryRevision, out DailyV1InventoryOwner inventoryOwner)
            || !MoneyAccountOwnerBridge.TryCreateFromOwnerSnapshot(
                d.MoneyBalance, d.MoneyRevision, out MoneyAccountRuntime moneyAccount))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
            return false;
        }

        P12DNpcMerchantPlanValue merchant = f.MerchantPlan;
        DailyV1ItemDefinition merchantItem = null;
        if (merchant == null
            || (merchant.ItemDefinitionId != null
                && (!itemsByDefinition.TryGetValue(merchant.ItemDefinitionId, out ItemData merchantAsset)
                    || merchantAsset == null
                    || !definitions.Items.TryGetValue(merchant.ItemDefinitionId, out merchantItem)
                    || merchantItem == null
                    || !string.Equals(merchantItem.DefinitionId, merchantAsset.DefinitionId, StringComparison.Ordinal)))
            || !ResolveNullable(merchant.OriginCityRuntimeId, citiesById, out _)
            || !ResolveNullable(merchant.TargetCityRuntimeId, citiesById, out _)
            || !DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
                definitions, merchantItem, merchant.OriginCityRuntimeId, merchant.TargetCityRuntimeId,
                merchant.PlannedAmount, merchant.RawRemainingAmount,
                merchant.PurchasePricePerItem, merchant.WaitDaysAtDestination,
                merchant.PendingTravelDays, merchant.OriginDecisionId,
                merchant.Revision, out DailyV1NpcMerchantTradePlanOwner merchantPlanOwner))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
            return false;
        }

        if (!TryCreateCommercialKnowledge(f, itemsByDefinition, definitions,
                selectedItemBindings, out CommercialKnowledgeRuntime commercialProjection)
            || commercialProjection?.SelectedOwner == null
            || !SpatialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
                d.RuntimeId, f.KnownLocationRuntimeIds, f.KnownRouteRuntimeIds,
                f.SpatialKnowledgeRevision, out SpatialKnowledgeRuntime spatialKnowledge)
            || f.KnownLocationRuntimeIds.Any(id => !locationsById.ContainsKey(id))
            || f.KnownRouteRuntimeIds.Any(id => !routeIds.Contains(id)))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
            return false;
        }

        DailyV1ActionDefinition selectedAction = null;
        DailyV1NpcActionPayloadOwner actionPayload = null;
        if (f.Action != null)
        {
            P12DNpcActionValue action = f.Action;
            DailyV1ItemDefinition targetItem = null;
            if (!actionsByDefinition.TryGetValue(action.ActionDefinitionId, out NpcActionData actionAsset)
                || actionAsset == null
                || !definitions.Actions.TryGetValue(action.ActionDefinitionId, out selectedAction)
                || selectedAction == null
                || !string.Equals(selectedAction.DefinitionId, actionAsset.DefinitionId, StringComparison.Ordinal)
                || (action.TargetNpcRuntimeId != null && !npcRuntimeIds.Contains(action.TargetNpcRuntimeId))
                || !ResolveNullable(action.TargetCityRuntimeId, citiesById, out _)
                || (action.TargetItemDefinitionId != null
                    && (!itemsByDefinition.TryGetValue(action.TargetItemDefinitionId, out ItemData targetItemAsset)
                        || targetItemAsset == null
                        || !definitions.Items.TryGetValue(action.TargetItemDefinitionId, out targetItem)
                        || targetItem == null
                        || !string.Equals(targetItem.DefinitionId, targetItemAsset.DefinitionId, StringComparison.Ordinal)))
                || !TryValidateActionEvidence(action, locationsById, routeIds, itemsByDefinition)
                || !DailyV1NpcActionPayloadOwner.TryCreate(
                    definitions, selectedAction, action.TargetNpcRuntimeId,
                    action.TargetCityRuntimeId, targetItem, action.Amount,
                    action.ExpectedUnitPrice, action.SuccessChanceMultiplier,
                    action.TravelReason, action.ExpectedNetValue,
                    action.OriginDecisionId, action.StableOccurrenceKey,
                    action.CommercialDecisionEvidence, action.CommercialScoutingEvidence,
                    out actionPayload))
            {
                failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
                return false;
            }
        }

        P12DNpcTravelPlanValue travelPlan = f.TravelPlan;
        if (travelPlan == null
            || !ResolveNullable(travelPlan.TargetLocationRuntimeId, locationsById, out _)
            || !ResolveNullable(travelPlan.TargetCityRuntimeId, citiesById, out _))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.InvalidReference;
            return false;
        }

        DailyV1NpcRuntimeRestoreState restoreState = new DailyV1NpcRuntimeRestoreState
        {
            RuntimeId = d.RuntimeId,
            Definitions = definitions,
            Definition = selectedNpcDefinition,
            Statuses = selectedStatuses,
            Inventory = inventoryOwner,
            MoneyAccount = moneyAccount,
            CommercialKnowledge = commercialProjection.SelectedOwner,
            MerchantTradePlan = merchantPlanOwner,
            SpatialKnowledge = spatialKnowledge,
            CurrentAction = selectedAction,
            CurrentActionPayload = actionPayload,
            ActionRevision = d.CurrentActionRevision,
            LifeState = d.LifeState,
            InjurySeverity = d.InjurySeverity,
            LifeStateRevision = d.LifeStateRevision,
            CrimeJusticeRevision = d.CrimeJusticeRevision,
            HiddenDaysRemaining = d.HiddenDaysRemaining,
            CurrentLocationRuntimeId = d.CurrentLocationRuntimeId,
            CurrentCityRuntimeId = d.CurrentCityRuntimeId,
            DestinationLocationRuntimeId = d.DestinationLocationRuntimeId,
            DestinationCityRuntimeId = d.DestinationCityRuntimeId,
            TravelDaysRemaining = f.TravelDaysRemaining,
            TravelDaysTotal = f.TravelDaysTotal,
            TravelRouteRuntimeId = f.TravelRouteRuntimeId,
            TravelStartedToday = f.TravelStartedToday,
            TravelOriginDecisionId = f.TravelOriginDecisionId,
            ActiveTravelPartyId = f.ActiveTravelPartyId,
            TravelRevision = f.TravelStateRevision,
            TravelPlanTargetLocationRuntimeId = travelPlan.TargetLocationRuntimeId,
            TravelPlanTargetCityRuntimeId = travelPlan.TargetCityRuntimeId,
            TravelPlanReason = travelPlan.Reason,
            TravelPlanUtility = travelPlan.Utility,
            TravelPlanExpectedCost = travelPlan.ExpectedCost,
            TravelPlanOriginDecisionId = travelPlan.OriginDecisionId,
            TravelPlanRevision = travelPlan.Revision
        };
        if (!DailyV1NpcRuntimeOwners.TryCreateRestoredP12DOwner(
                restoreState, out DailyV1NpcRuntimeOwners runtimeOwners))
        {
            failure = P12DNpcRootOwnerSnapshotFailure.UnsupportedValue;
            return false;
        }

        prepared = new P12DNpcPreparedSelectedCoreOwner
        {
            RuntimeOwners = runtimeOwners,
            InventoryProjection = new InventoryRuntime(runtimeOwners.Inventory),
            CommercialKnowledgeProjection = commercialProjection,
            SpatialKnowledge = runtimeOwners.SpatialKnowledge,
            StatusDefinitions = selectedStatuses,
            CurrentActionPayload = actionPayload
        };
        failure = P12DNpcRootOwnerSnapshotFailure.None;
        return true;
    }

    private static bool MatchesSelectedInventoryRows(
        DailyV1InventoryOwner owner,
        IReadOnlyList<DailyV1InventoryItemState> expectedRows)
    {
        if (owner == null || expectedRows == null || owner.Items.Count != expectedRows.Count) return false;
        for (int i = 0; i < expectedRows.Count; i++)
        {
            DailyV1InventoryItemState actual = owner.Items[i];
            DailyV1InventoryItemState expected = expectedRows[i];
            if (actual == null || expected == null
                || !ReferenceEquals(actual.Item, expected.Item)
                || actual.Amount != expected.Amount
                || !actual.AverageUnitCost.Equals(expected.AverageUnitCost)) return false;
        }
        return true;
    }

    private static bool TryCreateCommercialKnowledge(
        P12DNpcFRow row,
        IReadOnlyDictionary<string, ItemData> itemsByDefinition,
        DailyV1EffectiveDefinitions selectedDefinitions,
        DailyV1DefinitionAdapter.SelectedBindings selectedItemBindings,
        out CommercialKnowledgeRuntime staged)
    {
        staged = null;
        List<CommercialMarketObservation> markets =
            new List<CommercialMarketObservation>(row.CommercialMarkets.Count);
        foreach (P12DNpcCommercialMarketValue value in row.CommercialMarkets)
        {
            if (value == null || !itemsByDefinition.TryGetValue(value.ItemDefinitionId, out ItemData item)
                || !Enum.IsDefined(typeof(CommercialKnowledgeSource), value.Source)
                || (value.Source == CommercialKnowledgeSource.SharedByNpc
                    && string.IsNullOrWhiteSpace(value.SourceRuntimeId)))
                return false;
            try
            {
                markets.Add(new CommercialMarketObservation(
                    value.LocationRuntimeId, item, value.ObservedPrice, value.ObservedStock,
                    value.ObservedDay, value.ReceivedDay, value.Source, value.SourceRuntimeId));
            }
            catch (ArgumentException) { return false; }
        }

        List<CommercialLiquidityObservation> liquidity =
            new List<CommercialLiquidityObservation>(row.CommercialLiquidity.Count);
        foreach (P12DNpcCommercialLiquidityValue value in row.CommercialLiquidity)
        {
            if (value == null || !Enum.IsDefined(typeof(CommercialKnowledgeSource), value.Source)
                || (value.Source == CommercialKnowledgeSource.SharedByNpc
                    && string.IsNullOrWhiteSpace(value.SourceRuntimeId))
                || (value.Mode == MarketLiquidityMode.AccountBacked
                    && (float.IsNaN(value.PurchasingPower) || float.IsInfinity(value.PurchasingPower)
                        || value.PurchasingPower < 0f)))
                return false;
            try
            {
                liquidity.Add(new CommercialLiquidityObservation(
                    value.LocationRuntimeId, value.Mode, value.PurchasingPower,
                    value.ObservedDay, value.ReceivedDay, value.Source, value.SourceRuntimeId));
            }
            catch (ArgumentException) { return false; }
        }

        if (!CommercialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
                markets, liquidity, row.CommercialShareReceipts,
                row.CommercialKnowledgeRevision, out staged)) return false;
        if (selectedDefinitions == null) return selectedItemBindings == null;
        return selectedItemBindings != null
            && staged.TryBindSelectedDailyV1Owner(selectedDefinitions, selectedItemBindings);
    }

    private static bool TryValidateActionEvidence(
        P12DNpcActionValue value,
        IReadOnlyDictionary<string, SpatialLocationRuntime> locations,
        HashSet<string> routes,
        IReadOnlyDictionary<string, ItemData> items)
    {
        CommercialDecisionEvidence decision = value.CommercialDecisionEvidence;
        if (decision != null)
        {
            if (!items.ContainsKey(decision.ItemDefinitionId)
                || !locations.ContainsKey(decision.CurrentLocationRuntimeId)
                || !locations.ContainsKey(decision.TradeOriginLocationRuntimeId)
                || !locations.ContainsKey(decision.TradeDestinationLocationRuntimeId)
                || (decision.PreviousDestinationLocationRuntimeId != null
                    && !locations.ContainsKey(decision.PreviousDestinationLocationRuntimeId))
                || !ValidateObservationEvidence(decision.OriginObservation, locations)
                || !ValidateObservationEvidence(decision.DestinationObservation, locations)
                || (decision.DestinationLiquidityObservation != null
                    && !locations.ContainsKey(decision.DestinationLiquidityObservation.LocationRuntimeId)))
                return false;
        }

        CommercialScoutingEvidence scouting = value.CommercialScoutingEvidence;
        return scouting == null
            || (locations.ContainsKey(scouting.TargetLocationRuntimeId)
                && routes.Contains(scouting.KnownRouteRuntimeId));
    }

    private static bool ValidateObservationEvidence(
        CommercialObservationEvidence value,
        IReadOnlyDictionary<string, SpatialLocationRuntime> locations) =>
        value == null || locations.ContainsKey(value.LocationRuntimeId);

    private static bool TryBuildUniqueMap<T>(
        IReadOnlyList<T> values,
        Func<T, string> identity,
        out Dictionary<string, T> result) where T : class
    {
        result = new Dictionary<string, T>(StringComparer.Ordinal);
        if (values == null) return false;
        for (int i = 0; i < values.Count; i++)
        {
            T value = values[i];
            string id = value == null ? null : identity(value);
            if (string.IsNullOrWhiteSpace(id) || !result.TryAdd(id, value)) return false;
        }
        return true;
    }

    private static bool TryBuildUniqueIdSet(
        IReadOnlyList<string> values,
        out HashSet<string> result)
    {
        result = new HashSet<string>(StringComparer.Ordinal);
        if (values == null) return false;
        foreach (string id in values)
            if (string.IsNullOrWhiteSpace(id) || !result.Add(id)) return false;
        return true;
    }

    private static bool ResolveNullable<T>(
        string id,
        IReadOnlyDictionary<string, T> values,
        out T value) where T : class
    {
        value = null;
        if (id == null) return true;
        return !string.IsNullOrWhiteSpace(id) && values != null && values.TryGetValue(id, out value);
    }

    private static T Resolve<T>(string id, IReadOnlyDictionary<string, T> values) where T : class =>
        id == null ? null : values[id];

    private static bool TryRequireNpcSections(
        NpcRuntime npc, DailyCaptureEligibilityToken token,
        InventoryRuntime inventory, MoneyAccountRuntime account,
        object merchantPlanAuthority, long merchantPlanRevision,
        NpcTravelPlanRuntime travelPlan,
        CommercialKnowledgeRuntime commercial, SpatialKnowledgeRuntime spatial,
        ExplorableSiteKnowledgeRuntime siteKnowledge, LocalTopologyKnowledgeRuntime local,
        AdventureSiteIntelKnowledgeRuntime adventure,
        int currentActionCardinality,
        out P12DNpcRootOwnerSnapshotFailure failure)
    {
        failure = P12DNpcRootOwnerSnapshotFailure.InvalidOwnerSection;
        if (!TryRequireSection(token, NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, false),
                NpcLifecycleCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                npc, 1, npc.LifeStateRevision)
            || !TryRequireSection(token, P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor(npc.RuntimeId),
                P12CrimeJusticeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                npc, 1, npc.P12CrimeJusticeRevision)
            || !TryRequireSection(token, NpcCurrentActionCensusProvider.SectionIdFor(npc.RuntimeId),
                NpcCurrentActionCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                npc, currentActionCardinality, npc.CurrentActionRevision)
            || !TryRequireSection(token, NpcTravelStateCensusProvider.SectionIdFor(npc.RuntimeId),
                NpcTravelStateCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                npc, 1, npc.TravelStateRevision)
            || !TryRequireSection(token, NpcInventoryCensusProvider.SectionPrefix + npc.RuntimeId,
                NpcInventoryCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                inventory, inventory.Items.Count, inventory.Revision)
            || !TryRequireSection(token, NpcMoneyAccountCensusProvider.SectionPrefix + npc.RuntimeId,
                NpcMoneyAccountCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                account, 1, account.Revision)
            || !TryRequireSection(token, NpcPlanCensusProvider.SectionIdFor(
                    NpcPlanCensusProvider.MerchantTradePlanKind, npc.RuntimeId),
                NpcPlanCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                 merchantPlanAuthority, 1, merchantPlanRevision)
            || !TryRequireSection(token, NpcPlanCensusProvider.SectionIdFor(
                    NpcPlanCensusProvider.TravelPlanKind, npc.RuntimeId),
                NpcPlanCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                travelPlan, 1, travelPlan.Revision))
            return false;

        if (npc.BoundPersonRuntime == null)
        {
            if (!TryRequireSection(token,
                    NpcLifecycleCensusProvider.SectionIdFor(npc.RuntimeId, true),
                    NpcLifecycleCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                    npc, 1, npc.ResidenceRevision))
                return false;
        }
        else
        {
            PersonRuntime person = npc.BoundPersonRuntime;
            if (!TryRequireSection(token, PersonLifeResidenceCensusProvider.SectionIdFor(person.PersonId),
                    PersonLifeResidenceCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                    person, 1, PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person)))
                return false;
        }

        if (!spatialOwners(spatial) || !siteOwners(siteKnowledge) || !adventureOwners(adventure)
            || !commercialOwners(commercial) || !localOwners(local))
            return false;

        IReadOnlyList<IOwnerSectionCensusProvider> receiptProviders =
            P12DNpcReceiptOwnerCensusProvider.CreateProviders(new[] { npc });
        foreach (IOwnerSectionCensusProvider provider in receiptProviders)
        {
            OwnerSectionCensusWitness witness;
            try { witness = provider.GetCurrentCensus(); }
            catch { return false; }
            if (!TryRequireSection(token, witness.SectionId, witness.SchemaVersion,
                    OwnerSectionRole.Required, witness.OwnerInstanceIdentity,
                    witness.Cardinality, witness.Revision)
                || witness.Cardinality != 0 || witness.Revision != 0L)
                return false;
        }

        failure = P12DNpcRootOwnerSnapshotFailure.None;
        return true;

        bool spatialOwners(SpatialKnowledgeRuntime owner) =>
            TryRequireSection(token, SpatialKnowledgeCensusProvider.LocationsSectionPrefix + npc.RuntimeId,
                SpatialKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.KnownLocationCount, owner.Revision)
            && TryRequireSection(token, SpatialKnowledgeCensusProvider.RoutesSectionPrefix + npc.RuntimeId,
                SpatialKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.KnownRouteCount, owner.Revision);

        bool siteOwners(ExplorableSiteKnowledgeRuntime owner) =>
            TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(0, npc.RuntimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.Observations.Count, owner.Revision);

        bool adventureOwners(AdventureSiteIntelKnowledgeRuntime owner) =>
            TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(3, npc.RuntimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.OppositionObservations.Count, owner.Revision)
            && TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(4, npc.RuntimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.NotableItemObservations.Count, owner.Revision)
            && TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(5, npc.RuntimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.CommonResourceObservations.Count, owner.Revision)
            && TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(6, npc.RuntimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.AccessObservations.Count, owner.Revision);

        bool commercialOwners(CommercialKnowledgeRuntime owner)
        {
            if (!owner.TryReadCensus(out int markets, out int liquidity, out int receipts, out long revision))
                return false;
            return TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(7, npc.RuntimeId),
                    NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                    owner, markets, revision)
                && TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(8, npc.RuntimeId),
                    NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                    owner, liquidity, revision)
                && TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(9, npc.RuntimeId),
                    NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                    owner, receipts, revision);
        }

        bool localOwners(LocalTopologyKnowledgeRuntime owner) =>
            TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(1, npc.RuntimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.PlaceObservations.Count, owner.Revision)
            && owner.PlaceObservations.Count == 0 && owner.Revision == 0L
            && TryRequireSection(token, NpcKnowledgeCensusProvider.SectionIdFor(2, npc.RuntimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                owner, owner.ConnectionObservations.Count, owner.Revision)
            && owner.ConnectionObservations.Count == 0 && owner.Revision == 0L;
    }

    private static bool TryRequireSection(
        DailyCaptureEligibilityToken token,
        string sectionId,
        int schemaVersion,
        OwnerSectionRole expectedRole,
        object owner,
        int cardinality,
        long revision,
        bool requireOwner = true)
    {
        if (token?.OwnerSections == null || string.IsNullOrWhiteSpace(sectionId)
            || schemaVersion <= 0 || cardinality < 0 || revision < 0L)
            return false;
        OwnerSectionCensusSnapshot match = null;
        foreach (OwnerSectionCensusSnapshot row in token.OwnerSections)
        {
            if (!string.Equals(row.SectionId, sectionId, StringComparison.Ordinal)) continue;
            if (match != null) return false;
            match = row;
        }
        return match != null && match.SchemaVersion == schemaVersion
            && match.Role == expectedRole
            && (!requireOwner || ReferenceEquals(match.OwnerInstanceIdentity, owner))
            && match.Cardinality == cardinality && match.Revision == revision;
    }

    private static bool IsFiniteNonNegative(float value) =>
        value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
}
