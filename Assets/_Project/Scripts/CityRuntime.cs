using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using UnityEngine;

[Serializable]
public class CityRuntime
{
    [NonSerialized] private MutationGuardBinding runtimeMutationGuardBinding = new MutationGuardBinding();
    [SerializeField] private string runtimeId;
    [SerializeField] private CityData cityData;
    [NonSerialized] private SettlementPopulationRuntime population;
    [SerializeField] private MarketRuntime market;
    [NonSerialized] private MarketCounterpartyRuntime marketCounterparty;
    [NonSerialized] private PopulationEconomyRuntime populationEconomy;
    [NonSerialized] private SpatialLocationRuntime location;
    [NonSerialized] private List<NpcRuntime> importantNpcs = new List<NpcRuntime>();
    [NonSerialized] private ReadOnlyCollection<NpcRuntime> readOnlyImportantNpcs;
    [NonSerialized] private long importantNpcRevision;
    [NonSerialized] private SimulationLogger logger;
    [NonSerialized] private Dictionary<string, CityDailyEconomyReceipt> dailyEconomyReceipts =
        new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal);
    [NonSerialized] private long dailyEconomyReceiptRevision;

    private const string DailyEconomyVersion = "1";
    [NonSerialized] private LocalDailyMaterialFlowResult lastMaterialFlow;

    public string RuntimeId => runtimeId;
    public CityData CityData => cityData;
    public string DefinitionId => cityData != null ? cityData.DefinitionId : string.Empty;
    public SpatialLocationRuntime Location => location;
    public SettlementPopulationRuntime Population
    {
        get
        {
            if (population == null)
            {
                population = CreatePopulationRuntime();
            }

            return population;
        }
    }
    public int CurrentPopulation => Population.CurrentPopulation;
    public MarketRuntime Market
    {
        get
        {
            if (market == null)
            {
                marketCounterparty = NormalizeMarketCounterparty(marketCounterparty, runtimeId);
                market = new MarketRuntime(new List<MarketItemConfig>(), marketCounterparty);
            }

            return market;
        }
    }

    public MarketCounterpartyRuntime MarketCounterparty
    {
        get
        {
            if (marketCounterparty == null)
            {
                marketCounterparty = NormalizeMarketCounterparty(market != null ? market.Counterparty : null, runtimeId);
            }

            return marketCounterparty;
        }
    }
    public PopulationEconomyRuntime PopulationEconomy
    {
        get
        {
            if (populationEconomy == null)
            {
                populationEconomy = CreateConfiguredPopulationEconomy();
            }

            return populationEconomy;
        }
    }
    public IReadOnlyList<NpcRuntime> ImportantNpcs => readOnlyImportantNpcs
        ?? (readOnlyImportantNpcs = EnsureImportantNpcs().AsReadOnly());
    public long ImportantNpcRevision => importantNpcRevision;
    public string CityName => cityData != null ? cityData.cityName : "Cidade desconhecida";
    public bool HasLocalDailyMaterialFlow => cityData != null
        && (!string.IsNullOrWhiteSpace(cityData.settlementSemanticId)
            || !string.IsNullOrWhiteSpace(cityData.materialFlowLocationId)
            || !string.IsNullOrWhiteSpace(cityData.marketStoreSemanticId)
            || (cityData.productionConfigs != null && cityData.productionConfigs.Exists(source =>
                source != null && !string.IsNullOrWhiteSpace(source.productionSourceId))));
    public LocalDailyMaterialFlowResult LastMaterialFlow => lastMaterialFlow;

    internal void ValidateLocalDailyMaterialFlowAnchor(LegacySpatialAnchorBindingStore bindings, SpatialAuthorityStore spatial)
    {
        if (!HasLocalDailyMaterialFlow) return;
        if (string.IsNullOrWhiteSpace(cityData.settlementSemanticId)
            || string.IsNullOrWhiteSpace(cityData.materialFlowLocationId)
            || string.IsNullOrWhiteSpace(cityData.marketStoreSemanticId)
            || cityData.productionConfigs == null || cityData.productionConfigs.Count != 1)
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires settlement, LocationId, market store, and exactly one authored source.");
        CityProductionConfig source = cityData.productionConfigs[0];
        if (source == null || source.item == null || string.IsNullOrWhiteSpace(source.item.DefinitionId)
            || source.amountPerDay <= 0
            || string.IsNullOrWhiteSpace(source.productionSourceId) || string.IsNullOrWhiteSpace(source.contentRevision))
            throw new LocalDailyMaterialFlowRejectedException("P14-A source identity, item, positive quantity, and content revision are required.");
        if (PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free
            || cityData.marketItems == null || cityData.marketItems.Count != 1
            || cityData.marketItems[0] == null || cityData.marketItems[0].item == null
            || !string.Equals(cityData.marketItems[0].item.DefinitionId, source.item.DefinitionId, StringComparison.Ordinal))
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires exactly one market item row matching its source item and free population consumption.");
        SpatialAnchorOwnerId owner = new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, RuntimeId);
        if (bindings == null || !bindings.TryGet(owner, out LocationId bound)
            || !string.Equals(bound?.Value, cityData.materialFlowLocationId, StringComparison.Ordinal)
            || spatial == null || !spatial.TryGet(bound, out _))
            throw new LocalDailyMaterialFlowRejectedException("P14-A City anchor must resolve and match its authored stable LocationId.");
    }

    internal void SimulateLocalDailyMaterialFlow(long absoluteDay, string calendarVersion)
    {
        CityProductionConfig source = cityData.productionConfigs[0];
        MarketItemConfig itemConfig = cityData.marketItems?.Find(candidate => candidate != null
            && candidate.item != null && string.Equals(candidate.item.DefinitionId, source.item.DefinitionId, StringComparison.Ordinal));
        if (itemConfig == null || PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free)
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires a market item and free population consumption for the source item.");

        int opening = Market.GetAmount(source.item);
        int applied = Market.AddStock(source.item, source.amountPerDay);
        string rejection = applied == source.amountPerDay ? string.Empty : "AggregateStockOverflow";
        int requested = Math.Max(0, Mathf.RoundToInt(Population.CurrentPopulation / 1000f * itemConfig.consumptionPer1000Population));
        int actual = Market.RemoveStockUpTo(source.item, requested);
        int closing = Market.GetAmount(source.item);
        lastMaterialFlow = new LocalDailyMaterialFlowResult(cityData.settlementSemanticId, source.productionSourceId,
            cityData.marketStoreSemanticId, cityData.materialFlowLocationId, source.item.DefinitionId,
            source.contentRevision, "Economy.Enabled=true;PaymentMode=Free", "simulation-calendar", calendarVersion,
            true, Population.CurrentPopulation, itemConfig.consumptionPer1000Population, absoluteDay, opening,
            source.amountPerDay, applied, rejection, requested, actual, closing);
        logger?.Log(SimulationLogCategory.EconomyProduction, $"{CityName} source {source.productionSourceId}: {applied}/{source.amountPerDay}");
        if (actual > 0) logger?.Log(SimulationLogCategory.EconomyConsumption, $"{CityName} consumed {actual} {source.item.itemName}");
    }

    /// <summary>Creates one of the three city-owned resumable daily economy steps.</summary>
    public bool TryCreateDailyEconomyStep(
        DailyBoundaryOperation operation,
        int ordinal,
        CityDailyEconomyStepKind kind,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        step = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || ordinal < 0 || string.IsNullOrWhiteSpace(RuntimeId)
            || !Enum.IsDefined(typeof(CityDailyEconomyStepKind), kind)) return false;
        string config = CaptureDailyEconomyConfiguration();
        string ownerRevision = SpatialStableKey.Encode(RuntimeId, config);
        step = new BoundaryContinuationStep(ordinal, GetDailyEconomyStepId(kind),
            RuntimeId, "city-economy." + kind.ToString().ToLowerInvariant(),
            DailyEconomyVersion, ownerRevision, string.Empty);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Prepares market/account replacements and the receipt before any live mutation.</summary>
    public bool TryPrepareDailyEconomyStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        failure = TimelineFailure.ContinuationFailed;
        if (!TryGetDailyEconomyIdentity(manifest, step, out string identity, out string fingerprint)) return false;
        dailyEconomyReceipts ??= new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal);
        if (dailyEconomyReceipts.TryGetValue(identity, out CityDailyEconomyReceipt previous))
        {
            if (previous.Fingerprint != fingerprint) return false;
            prepared = new CityDailyEconomyCommit(this, previous, null, 0, 0,
                null, 0, 0, 0f, null, 0, 0, 0f, null, dailyEconomyReceiptRevision, true,
                step.OwnerRevision, -1L, -1, fingerprint);
            failure = TimelineFailure.None;
            return true;
        }
        if (dailyEconomyReceiptRevision == long.MaxValue) return false;

        long marketRevision = Market.Revision;
        PreparedMarketState marketState = Market.CreatePreparedSnapshot();
        long marketIncrements = 0;
        CityDailyEconomyStepKind kind = GetDailyEconomyStepKind(step.StepId);
        int preparedPopulation = kind == CityDailyEconomyStepKind.Consumption ? Population.CurrentPopulation : -1;
        long preparedPopulationRevision = kind == CityDailyEconomyStepKind.Consumption ? Population.Revision : -1L;
        bool paidConsumption = kind == CityDailyEconomyStepKind.Consumption
            && PopulationEconomy.PaymentMode == ConsumptionPaymentMode.AccountBacked;
        MoneyAccountRuntime populationAccount = paidConsumption ? PopulationEconomy.MoneyAccount : null;
        MoneyAccountRuntime settlementAccount = paidConsumption ? MarketCounterparty.MoneyAccount : null;
        long populationRevision = populationAccount != null ? populationAccount.Revision : 0;
        long settlementRevision = settlementAccount != null ? settlementAccount.Revision : 0;
        long populationIncrements = 0, settlementIncrements = 0;
        float populationBalance = populationAccount != null ? populationAccount.Balance : 0f;
        float settlementBalance = settlementAccount != null ? settlementAccount.Balance : 0f;
        List<CityProductionResult> productionResults = new List<CityProductionResult>();
        List<CityConsumptionResult> consumptionResults = new List<CityConsumptionResult>();
        if (kind == CityDailyEconomyStepKind.Production)
        {
            foreach (CityProductionConfig row in cityData != null ? cityData.productionConfigs ?? new List<CityProductionConfig>() : new List<CityProductionConfig>())
            {
                if (row?.item == null || row.amountPerDay <= 0) continue;
                MarketItemRuntime item = FindPreparedItem(marketState, row.item);
                if (item != null && !item.AddAmount(row.amountPerDay)) continue;
                if (item == null) marketState.Items.Add(new MarketItemRuntime(row.item, row.amountPerDay, 100));
                if (item != null) item.UpdatePrice();
                marketIncrements++;
                productionResults.Add(new CityProductionResult(RuntimeId, row.item.DefinitionId, row.amountPerDay, Market.StockOwnerRuntimeId));
            }
        }
        else if (kind == CityDailyEconomyStepKind.Consumption)
        {
            foreach (MarketItemConfig row in cityData != null ? cityData.marketItems ?? new List<MarketItemConfig>() : new List<MarketItemConfig>())
            {
                if (row?.item == null || row.consumptionPer1000Population <= 0f) continue;
                int desired = Mathf.RoundToInt(preparedPopulation / 1000f * row.consumptionPer1000Population);
                int consumed = 0;
                float unitPrice = 0f, paid = 0f;
                MarketItemRuntime item = FindPreparedItem(marketState, row.item);
                if (desired > 0 && item != null && item.Amount > 0)
                {
                    if (PopulationEconomy.PaymentMode == ConsumptionPaymentMode.Free)
                    {
                        consumed = Math.Min(desired, item.Amount);
                    }
                    else if (populationAccount != null && settlementAccount != null)
                    {
                        unitPrice = Mathf.Max(0.01f, item.CurrentPrice);
                        if (float.IsNaN(unitPrice) || float.IsInfinity(unitPrice)) unitPrice = 0f;
                        if (unitPrice <= 0f)
                        {
                            consumptionResults.Add(new CityConsumptionResult(RuntimeId,
                                PopulationEconomy.PopulationEconomicRuntimeId, row.item.DefinitionId,
                                desired, 0, PopulationEconomy.PaymentMode, 0f, 0f));
                            continue;
                        }
                        float affordableRaw = populationBalance / unitPrice;
                        int affordable = populationBalance >= unitPrice
                            ? (float.IsInfinity(affordableRaw) || affordableRaw >= int.MaxValue
                                ? int.MaxValue : Mathf.Max(0, Mathf.FloorToInt(affordableRaw))) : 0;
                        consumed = Math.Min(desired, Math.Min(item.Amount, affordable));
                        paid = unitPrice * consumed;
                        float nextPopulationBalance = populationBalance - paid;
                        float nextSettlementBalance = settlementBalance + paid;
                        if (consumed > 0 && (float.IsInfinity(paid) || float.IsNaN(paid)
                            || populationBalance < paid || nextPopulationBalance < 0f || nextPopulationBalance >= populationBalance
                            || float.IsInfinity(nextSettlementBalance) || nextSettlementBalance <= settlementBalance))
                        { consumed = 0; unitPrice = 0f; paid = 0f; }
                        if (consumed == 0) { unitPrice = 0f; paid = 0f; }
                        if (consumed > 0)
                        {
                            populationBalance = nextPopulationBalance; settlementBalance = nextSettlementBalance;
                            populationIncrements++; settlementIncrements++;
                        }
                    }
                    if (consumed > 0)
                    {
                        item.RemoveAmount(consumed); item.UpdatePrice(); marketIncrements++;
                    }
                }
                consumptionResults.Add(new CityConsumptionResult(RuntimeId,
                    PopulationEconomy.PopulationEconomicRuntimeId, row.item.DefinitionId,
                    desired, consumed, PopulationEconomy.PaymentMode, unitPrice, paid));
            }
        }
        else
        {
            bool changed = false;
            foreach (MarketItemRuntime item in marketState.Items)
            {
                if (item == null) continue;
                float previousPrice = item.CurrentPrice;
                item.UpdatePrice();
                changed |= previousPrice != item.CurrentPrice;
            }
            if (changed) marketIncrements++;
        }

        if (!Market.CanInstall(marketRevision, marketIncrements)
            || (populationAccount != null && !populationAccount.CanInstall(populationRevision, populationIncrements, populationBalance))
            || (settlementAccount != null && !settlementAccount.CanInstall(settlementRevision, settlementIncrements, settlementBalance))) return false;
        CityDailyEconomyReceipt receipt = new CityDailyEconomyReceipt(identity, fingerprint,
            productionResults.AsReadOnly(), consumptionResults.AsReadOnly());
        Dictionary<string, CityDailyEconomyReceipt> nextReceipts =
            new Dictionary<string, CityDailyEconomyReceipt>(dailyEconomyReceipts, StringComparer.Ordinal) { [identity] = receipt };
        prepared = new CityDailyEconomyCommit(this, receipt, marketState, marketRevision, marketIncrements,
            populationAccount, populationRevision, populationIncrements, populationBalance,
            settlementAccount, settlementRevision, settlementIncrements, settlementBalance,
            nextReceipts, dailyEconomyReceiptRevision, false, step.OwnerRevision,
            preparedPopulationRevision, preparedPopulation, fingerprint);
        failure = TimelineFailure.None;
        return true;
    }

    public bool TryResolveDailyEconomyReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out CityDailyEconomyReceipt receipt,
        out TimelineFailure failure)
    {
        receipt = null;
        if (!TryGetDailyEconomyIdentity(manifest, step, out string identity, out string fingerprint))
        { failure = TimelineFailure.ContinuationFailed; return false; }
        if (dailyEconomyReceipts != null && dailyEconomyReceipts.TryGetValue(identity, out CityDailyEconomyReceipt existing))
        {
            if (existing.Fingerprint != fingerprint) { failure = TimelineFailure.ContinuationFailed; return false; }
            receipt = existing;
        }
        failure = TimelineFailure.None;
        return receipt != null;
    }

    internal bool TryCommitDailyEconomy(
        CityDailyEconomyCommit commit, out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (commit == null) return false;
        if (commit.Replay)
        {
            if (dailyEconomyReceipts != null && dailyEconomyReceipts.TryGetValue(commit.Receipt.Identity, out CityDailyEconomyReceipt old)
                && old.Fingerprint == commit.Fingerprint) { commit.Completed = true; failure = TimelineFailure.None; return true; }
            return false;
        }
        if (dailyEconomyReceiptRevision != commit.ExpectedReceiptRevision || dailyEconomyReceiptRevision == long.MaxValue
            || commit.ExpectedOwnerRevision != SpatialStableKey.Encode(RuntimeId, CaptureDailyEconomyConfiguration())
            || (commit.ExpectedPopulationRevision >= 0L
                && (Population.Revision != commit.ExpectedPopulationRevision
                    || Population.CurrentPopulation != commit.ExpectedPopulation))
            || dailyEconomyReceipts == null || dailyEconomyReceipts.ContainsKey(commit.Receipt.Identity)
            || !Market.CanInstall(commit.MarketRevision, commit.MarketIncrements)
            || (commit.PopulationAccount != null && !commit.PopulationAccount.CanInstall(commit.PopulationRevision, commit.PopulationIncrements, commit.PopulationBalance))
            || (commit.SettlementAccount != null && !commit.SettlementAccount.CanInstall(commit.SettlementRevision, commit.SettlementIncrements, commit.SettlementBalance))) return false;
        Market.InstallPrepared(commit.MarketRevision, commit.MarketIncrements, commit.MarketState);
        if (commit.PopulationAccount != null) commit.PopulationAccount.InstallPrepared(commit.PopulationRevision, commit.PopulationIncrements, commit.PopulationBalance);
        if (commit.SettlementAccount != null) commit.SettlementAccount.InstallPrepared(commit.SettlementRevision, commit.SettlementIncrements, commit.SettlementBalance);
        dailyEconomyReceipts = commit.NextReceipts;
        dailyEconomyReceiptRevision++;
        commit.Completed = true;
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryGetDailyEconomyIdentity(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out string identity, out string fingerprint)
    {
        identity = fingerprint = null;
        if (manifest == null || step == null || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step) || step.OwnerId != RuntimeId
            || step.OperationVersion != DailyEconomyVersion || step.Disposition != "included"
            || step.OwnerRevision != SpatialStableKey.Encode(RuntimeId, CaptureDailyEconomyConfiguration())) return false;
        if (manifest.BoundaryOccurrenceId != SpatialStableKey.Encode(manifest.WorldId, manifest.ProfileId,
            manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture))) return false;
        CityDailyEconomyStepKind kind = GetDailyEconomyStepKind(step.StepId);
        if (step.StepId != GetDailyEconomyStepId(kind)
            || step.OperationKind != "city-economy." + kind.ToString().ToLowerInvariant()) return false;
        identity = SpatialStableKey.Encode(manifest.BoundaryOccurrenceId, step.StepId, RuntimeId);
        fingerprint = SpatialStableKey.Encode(manifest.BoundaryOccurrenceId, manifest.ContinuationId,
            manifest.ConfigurationIdentity, manifest.ContentIdentity, step.Ordinal.ToString(CultureInfo.InvariantCulture),
            step.StepId, step.OwnerId, step.OperationKind, step.OperationVersion, step.OwnerRevision, step.Payload);
        return true;
    }

    private string CaptureDailyEconomyConfiguration()
    {
        MarketLiquidityConfig liquidity = cityData != null ? cityData.MarketLiquidity : null;
        PopulationConsumptionConfig consumption = cityData != null ? cityData.PopulationConsumption : null;
        List<string> parts = new List<string> { RuntimeId, DefinitionId,
            cityData != null ? cityData.initialPopulation.ToString(CultureInfo.InvariantCulture) : "0",
            PopulationEconomy.PaymentMode.ToString(),
            liquidity != null ? liquidity.liquidityMode.ToString() : "Open",
            liquidity != null ? liquidity.initialPurchasingPower.ToString("R", CultureInfo.InvariantCulture) : "0",
            consumption != null ? consumption.paymentMode.ToString() : "Free",
            consumption != null ? consumption.initialPurchasingPower.ToString("R", CultureInfo.InvariantCulture) : "0" };
        foreach (CityProductionConfig row in cityData != null ? cityData.productionConfigs ?? new List<CityProductionConfig>() : new List<CityProductionConfig>())
        { parts.Add("p"); AddItemIdentity(parts, row?.item); parts.Add(row != null ? row.amountPerDay.ToString(CultureInfo.InvariantCulture) : "null"); }
        foreach (MarketItemConfig row in cityData != null ? cityData.marketItems ?? new List<MarketItemConfig>() : new List<MarketItemConfig>())
        { parts.Add("c"); AddItemIdentity(parts, row?.item); parts.Add(row != null ? row.initialAmount.ToString(CultureInfo.InvariantCulture) : "null"); parts.Add(row != null ? row.desiredAmount.ToString(CultureInfo.InvariantCulture) : "null"); parts.Add(row != null ? row.consumptionPer1000Population.ToString("R", CultureInfo.InvariantCulture) : "null"); }
        return SpatialStableKey.Encode(parts.ToArray());
    }

    private static void AddItemIdentity(List<string> parts, ItemData item)
    { parts.Add(item != null ? item.DefinitionId : ""); parts.Add(item != null ? item.itemName : ""); parts.Add(item != null ? item.basePrice.ToString("R", CultureInfo.InvariantCulture) : ""); }
    private string GetDailyEconomyStepId(CityDailyEconomyStepKind kind) => "city-economy-" + kind.ToString().ToLowerInvariant() + ":" + RuntimeId;
    private CityDailyEconomyStepKind GetDailyEconomyStepKind(string id) => id != null && id.StartsWith("city-economy-production:", StringComparison.Ordinal) ? CityDailyEconomyStepKind.Production : id != null && id.StartsWith("city-economy-consumption:", StringComparison.Ordinal) ? CityDailyEconomyStepKind.Consumption : CityDailyEconomyStepKind.PriceRefresh;
    private static MarketItemRuntime FindPreparedItem(PreparedMarketState state, ItemData item) => state.Items.Find(x => x != null && x.Item == item);

    public CityRuntime(
        string runtimeId,
        CityData cityData,
        SpatialLocationRuntime location,
        SimulationLogger logger = null,
        MarketCounterpartyRuntime marketCounterparty = null)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("CityRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        this.runtimeId = runtimeId;
        this.cityData = cityData;
        this.location = location;
        this.logger = logger ?? new SimulationLogger(null);
        this.marketCounterparty = marketCounterparty != null
            ? NormalizeMarketCounterparty(marketCounterparty, runtimeId)
            : CreateConfiguredMarketCounterparty(runtimeId, cityData);
        population = CreatePopulationRuntime();
        market = cityData != null
            ? new MarketRuntime(cityData.marketItems, this.marketCounterparty)
            : new MarketRuntime(new List<MarketItemConfig>(), this.marketCounterparty);
        populationEconomy = CreateConfiguredPopulationEconomy();
    }

    public IReadOnlyList<CityProductionResult> SimulateProductionDay()
    {
        List<CityProductionResult> results = new List<CityProductionResult>();

        if (cityData == null || cityData.productionConfigs == null)
        {
            return results.AsReadOnly();
        }

        foreach (CityProductionConfig production in cityData.productionConfigs)
        {
            if (production == null || production.item == null || production.amountPerDay <= 0)
            {
                continue;
            }

            int produced = Market.AddStock(production.item, production.amountPerDay);
            if (produced <= 0)
            {
                continue;
            }

            results.Add(new CityProductionResult(
                RuntimeId,
                production.item.DefinitionId,
                produced,
                Market.StockOwnerRuntimeId));
            logger?.Log(SimulationLogCategory.EconomyProduction, $"{CityName} produziu {produced} {production.item.itemName}");
        }

        return results.AsReadOnly();
    }

    public IReadOnlyList<CityConsumptionResult> SimulateConsumptionDay()
    {
        List<CityConsumptionResult> results = new List<CityConsumptionResult>();

        if (cityData == null || cityData.marketItems == null)
        {
            return results.AsReadOnly();
        }

        foreach (MarketItemConfig config in cityData.marketItems)
        {
            if (config == null || config.item == null || config.consumptionPer1000Population <= 0f)
            {
                continue;
            }

            int desiredConsumption = Mathf.RoundToInt(Population.CurrentPopulation / 1000f * config.consumptionPer1000Population);
            if (desiredConsumption <= 0)
            {
                results.Add(new CityConsumptionResult(
                    RuntimeId,
                    PopulationEconomy.PopulationEconomicRuntimeId,
                    config.item.DefinitionId,
                    desiredConsumption,
                    0,
                    PopulationEconomy.PaymentMode,
                    0f,
                    0f));
                continue;
            }

            int consumed;
            float unitPrice = 0f;
            float totalPaid = 0f;

            if (PopulationEconomy.PaymentMode == ConsumptionPaymentMode.Free)
            {
                consumed = Market.RemoveStockUpTo(config.item, desiredConsumption);
            }
            else
            {
                EconomyTransactionResult receipt = new EconomyTransactionService().TryExecutePopulationConsumption(
                    PopulationEconomy,
                    Market,
                    config.item,
                    desiredConsumption);
                consumed = receipt.Quantity;
                unitPrice = receipt.UnitPrice;
                totalPaid = receipt.TotalPrice;
            }

            results.Add(new CityConsumptionResult(
                RuntimeId,
                PopulationEconomy.PopulationEconomicRuntimeId,
                config.item.DefinitionId,
                desiredConsumption,
                consumed,
                PopulationEconomy.PaymentMode,
                unitPrice,
                totalPaid));

            if (consumed > 0)
            {
                logger?.Log(SimulationLogCategory.EconomyConsumption, $"{CityName} consumiu {consumed} {config.item.itemName}");
            }
        }

        return results.AsReadOnly();
    }

    public void UpdateMarketPrices()
    {
        Market.UpdatePrices();
        logger?.Log(SimulationLogCategory.Market, $"{CityName} atualizou os precos do mercado.");
    }

    public void AddImportantNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (npcRuntime.IsAlive == false)
        {
            return;
        }

        if (npcRuntime.IsTraveling == true)
        {
            return;
        }

        npcRuntime.SetCurrentPresence(Location, this);
    }

    public void RemoveImportantNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (!TryRemoveImportantNpcMembership(npcRuntime))
        {
            return;
        }

        if (npcRuntime.CurrentCity == this)
        {
            npcRuntime.ClearCurrentPresenceFromCity(this);
        }
    }

    internal bool ContainsImportantNpc(NpcRuntime npcRuntime)
    {
        return npcRuntime != null && EnsureImportantNpcs().Contains(npcRuntime);
    }

    internal bool CanAddImportantNpcMembership(NpcRuntime npcRuntime)
    {
        return npcRuntime != null
            && (ContainsImportantNpc(npcRuntime) || CanApplyImportantNpcRevisionIncrements(1L));
    }

    internal bool CanRemoveImportantNpcMembership(NpcRuntime npcRuntime)
    {
        return npcRuntime == null
            || !ContainsImportantNpc(npcRuntime)
            || CanApplyImportantNpcRevisionIncrements(1L);
    }

    internal bool CanApplyImportantNpcRevisionIncrements(long increments)
    {
        return increments >= 0L
            && importantNpcRevision <= long.MaxValue - increments;
    }

    internal bool TryAddImportantNpcMembership(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return false;
        }

        List<NpcRuntime> membership = EnsureImportantNpcs();
        if (membership.Contains(npcRuntime))
        {
            return true;
        }

        if (importantNpcRevision == long.MaxValue)
        {
            return false;
        }

        membership.Add(npcRuntime);
        importantNpcRevision++;
        return true;
    }

    internal bool TryRemoveImportantNpcMembership(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return true;
        }

        List<NpcRuntime> membership = EnsureImportantNpcs();
        if (!membership.Contains(npcRuntime))
        {
            return true;
        }

        if (importantNpcRevision == long.MaxValue)
        {
            return false;
        }

        membership.Remove(npcRuntime);
        importantNpcRevision++;
        return true;
    }

    private List<NpcRuntime> EnsureImportantNpcs()
    {
        if (importantNpcs == null)
        {
            importantNpcs = new List<NpcRuntime>();
            readOnlyImportantNpcs = null;
        }

        return importantNpcs;
    }

    internal bool CanBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return runtimeMutationGuardBinding.CanBindTo(guard)
            && Population.CanBindMutationGuard(guard);
    }

    internal bool TryBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return CanBindRuntimeMutationGuard(guard)
            && Population.TryBindMutationGuard(guard)
            && runtimeMutationGuardBinding.TryBindTo(guard);
    }

    private void EnsureRuntimeMutationGuardBinding()
    {
        if (runtimeMutationGuardBinding == null)
        {
            runtimeMutationGuardBinding = new MutationGuardBinding();
        }
    }

    private static MarketCounterpartyRuntime NormalizeMarketCounterparty(
        MarketCounterpartyRuntime counterparty,
        string cityRuntimeId)
    {
        if (counterparty == null)
        {
            return MarketCounterpartyRuntime.CreateOpen(cityRuntimeId);
        }

        if (counterparty.LiquidityMode == MarketLiquidityMode.Open
            && string.IsNullOrWhiteSpace(counterparty.CounterpartyRuntimeId) == true)
        {
            return MarketCounterpartyRuntime.CreateOpen(cityRuntimeId);
        }

        if (string.Equals(counterparty.CounterpartyRuntimeId, cityRuntimeId, StringComparison.Ordinal) == false)
        {
            throw new ArgumentException(
                "A CityRuntime market counterparty must use the CityRuntime RuntimeId.",
                nameof(counterparty));
        }

        return counterparty;
    }

    private static MarketCounterpartyRuntime CreateConfiguredMarketCounterparty(
        string cityRuntimeId,
        CityData configuredCity)
    {
        MarketLiquidityConfig liquidity = configuredCity != null
            ? configuredCity.MarketLiquidity
            : null;

        if (liquidity == null || liquidity.liquidityMode == MarketLiquidityMode.Open)
        {
            return MarketCounterpartyRuntime.CreateOpen(cityRuntimeId);
        }

        if (liquidity.liquidityMode != MarketLiquidityMode.AccountBacked)
        {
            throw new ArgumentOutOfRangeException(
                nameof(configuredCity),
                liquidity.liquidityMode,
                "CityData contains an unsupported MarketLiquidityMode.");
        }

        MoneyAccountRuntime account = new MoneyAccountRuntime(liquidity.initialPurchasingPower);
        return MarketCounterpartyRuntime.CreateAccountBacked(cityRuntimeId, account);
    }

    private PopulationEconomyRuntime CreateConfiguredPopulationEconomy()
    {
        PopulationConsumptionConfig consumption = cityData != null
            ? cityData.PopulationConsumption
            : new PopulationConsumptionConfig();
        return new PopulationEconomyRuntime(
            runtimeId,
            consumption,
            MarketCounterparty.LiquidityMode);
    }

    private SettlementPopulationRuntime CreatePopulationRuntime()
    {
        int initialPopulation = cityData != null ? Mathf.Max(0, cityData.initialPopulation) : 0;
        return new SettlementPopulationRuntime(runtimeId, initialPopulation);
    }
}

public enum CityDailyEconomyStepKind { Production, Consumption, PriceRefresh }

[Serializable]
public sealed class CityDailyEconomyReceipt
{
    public string Identity { get; }
    public string Fingerprint { get; }
    public IReadOnlyList<CityProductionResult> ProductionResults { get; }
    public IReadOnlyList<CityConsumptionResult> ConsumptionResults { get; }
    internal CityDailyEconomyReceipt(string identity, string fingerprint,
        IReadOnlyList<CityProductionResult> production, IReadOnlyList<CityConsumptionResult> consumption)
    { Identity = identity; Fingerprint = fingerprint; ProductionResults = production; ConsumptionResults = consumption; }
}

internal sealed class CityDailyEconomyCommit : IBoundaryContinuationStepCommit
{
    private readonly CityRuntime owner;
    internal readonly CityDailyEconomyReceipt Receipt;
    internal readonly PreparedMarketState MarketState;
    internal readonly long MarketRevision, MarketIncrements;
    internal readonly MoneyAccountRuntime PopulationAccount, SettlementAccount;
    internal readonly long PopulationRevision, PopulationIncrements, SettlementRevision, SettlementIncrements;
    internal readonly float PopulationBalance, SettlementBalance;
    internal readonly Dictionary<string, CityDailyEconomyReceipt> NextReceipts;
    internal readonly long ExpectedReceiptRevision;
    internal readonly bool Replay;
    internal readonly string ExpectedOwnerRevision;
    internal readonly long ExpectedPopulationRevision;
    internal readonly int ExpectedPopulation;
    internal readonly string Fingerprint;
    internal bool Completed;
    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
    internal CityDailyEconomyCommit(CityRuntime owner, CityDailyEconomyReceipt receipt,
        PreparedMarketState marketState, long marketRevision, long marketIncrements,
        MoneyAccountRuntime populationAccount, long populationRevision, long populationIncrements, float populationBalance,
        MoneyAccountRuntime settlementAccount, long settlementRevision, long settlementIncrements, float settlementBalance,
        Dictionary<string, CityDailyEconomyReceipt> nextReceipts, long expectedReceiptRevision, bool replay,
        string expectedOwnerRevision, long expectedPopulationRevision, int expectedPopulation, string fingerprint)
    {
        this.owner = owner; Receipt = receipt; MarketState = marketState; MarketRevision = marketRevision;
        MarketIncrements = marketIncrements; PopulationAccount = populationAccount; PopulationRevision = populationRevision;
        PopulationIncrements = populationIncrements; PopulationBalance = populationBalance; SettlementAccount = settlementAccount;
        SettlementRevision = settlementRevision; SettlementIncrements = settlementIncrements; SettlementBalance = settlementBalance;
        NextReceipts = nextReceipts; ExpectedReceiptRevision = expectedReceiptRevision; Replay = replay;
        ExpectedOwnerRevision = expectedOwnerRevision; ExpectedPopulationRevision = expectedPopulationRevision;
        ExpectedPopulation = expectedPopulation; Fingerprint = fingerprint;
    }
    public bool TryCommit(out TimelineFailure failure)
    {
        if (Completed) { failure = TimelineFailure.None; return true; }
        return owner.TryCommitDailyEconomy(this, out failure);
    }
}
