using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum CityDailyEconomyStepKind { Production, Consumption, PriceRefresh }

[Serializable]
public sealed class CityDailyEconomyReceipt
{
    public string Identity { get; }
    public string Fingerprint { get; }
    public string BoundaryOccurrenceId { get; }
    public long AbsoluteDay { get; }
    public IReadOnlyList<CityProductionResult> ProductionResults { get; }
    public IReadOnlyList<CityConsumptionResult> ConsumptionResults { get; }
    public IReadOnlyList<CityDailyMaterialFlowSourceOutcome> SourceOutcomes { get; }
    public int? OpeningStock { get; }
    public int? PostProductionStock { get; }
    public long? PostProductionMarketRevision { get; }
    public int? ActualFreeConsumption { get; }
    public int? ClosingStock { get; }

    internal CityDailyEconomyReceipt(string identity, string fingerprint,
        IReadOnlyList<CityProductionResult> production, IReadOnlyList<CityConsumptionResult> consumption)
        : this(identity, fingerprint, production, consumption, string.Empty, 0L)
    {
    }

    internal CityDailyEconomyReceipt(string identity, string fingerprint,
        IReadOnlyList<CityProductionResult> production, IReadOnlyList<CityConsumptionResult> consumption,
        string boundaryOccurrenceId, long absoluteDay,
        IReadOnlyList<CityDailyMaterialFlowSourceOutcome> sourceOutcomes = null,
        int? openingStock = null, int? postProductionStock = null,
        long? postProductionMarketRevision = null,
        int? actualFreeConsumption = null, int? closingStock = null)
    {
        Identity = identity;
        Fingerprint = fingerprint;
        ProductionResults = production ?? Array.Empty<CityProductionResult>();
        ConsumptionResults = consumption ?? Array.Empty<CityConsumptionResult>();
        BoundaryOccurrenceId = boundaryOccurrenceId ?? string.Empty;
        AbsoluteDay = absoluteDay;
        SourceOutcomes = sourceOutcomes ?? Array.Empty<CityDailyMaterialFlowSourceOutcome>();
        OpeningStock = openingStock;
        PostProductionStock = postProductionStock;
        PostProductionMarketRevision = postProductionMarketRevision;
        ActualFreeConsumption = actualFreeConsumption;
        ClosingStock = closingStock;
    }
}

/// <summary>
/// Core-owned selected City daily transaction. Unity CityRuntime delegates its
/// selected P18 steps here while retaining the existing legacy-profile adapter.
/// </summary>
internal sealed partial class DailyV1CityRuntimeOwners
{
    private const string DailyEconomyOperationVersion = "1";
    private Dictionary<string, CityDailyEconomyReceipt> dailyEconomyReceipts =
        new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal);
    private long dailyEconomyReceiptRevision;

    internal void GetDailyEconomyReceiptExclusionProof(out int count, out long revision)
    {
        count = dailyEconomyReceipts != null ? dailyEconomyReceipts.Count : 0;
        revision = dailyEconomyReceiptRevision;
    }

    internal bool TryCreateDailyEconomyStep(DailyBoundaryOperation operation, int ordinal,
        CityDailyEconomyStepKind kind, out BoundaryContinuationStep step, out TimelineFailure failure)
    {
        step = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || ordinal < 0 || string.IsNullOrWhiteSpace(RuntimeId)
            || !Enum.IsDefined(typeof(CityDailyEconomyStepKind), kind)) return false;

        string ownerRevision = StableKeyEncoding.Encode(RuntimeId, CaptureDailyEconomyConfiguration());
        step = new BoundaryContinuationStep(ordinal, GetStepId(kind), RuntimeId,
            "city-economy." + kind.ToString().ToLowerInvariant(), DailyEconomyOperationVersion,
            ownerRevision, string.Empty);
        failure = TimelineFailure.None;
        return true;
    }

    internal bool TryPrepareDailyEconomyStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        failure = TimelineFailure.ContinuationFailed;
        if (!TryGetIdentity(manifest, step, out string identity, out string fingerprint)) return false;
        dailyEconomyReceipts ??= new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal);
        if (dailyEconomyReceipts.TryGetValue(identity, out CityDailyEconomyReceipt prior))
        {
            if (!string.Equals(prior.Fingerprint, fingerprint, StringComparison.Ordinal)) return false;
            prepared = new DailyV1CityEconomyStepCommit(this, prior, null, Market.Revision, 0,
                null, 0, 0, 0f, null, 0, 0, 0f, dailyEconomyReceiptRevision,
                true, step.OwnerRevision, -1, -1, fingerprint, 0, long.MinValue, false, false, -1L);
            failure = TimelineFailure.None;
            return true;
        }
        if (dailyEconomyReceiptRevision == long.MaxValue) return false;

        CityDailyEconomyStepKind kind = GetKind(step.StepId);
        long marketRevision = Market.Revision;
        if (!Market.TryCreatePreparedSnapshot(out DailyV1MarketPreparedState marketState)) return false;
        long marketIncrements = 0;
        int preparedPopulation = kind == CityDailyEconomyStepKind.Consumption
            ? Population.CurrentPopulation : -1;
        long preparedPopulationRevision = kind == CityDailyEconomyStepKind.Consumption
            ? Population.Revision : -1L;
        bool paidConsumption = kind == CityDailyEconomyStepKind.Consumption
            && PopulationEconomy.PaymentMode == ConsumptionPaymentMode.AccountBacked;
        MoneyAccountRuntime populationAccount = paidConsumption ? PopulationEconomy.MoneyAccount : null;
        MoneyAccountRuntime settlementAccount = paidConsumption ? Counterparty.MoneyAccount : null;
        long populationAccountRevision = populationAccount?.Revision ?? 0L;
        long settlementAccountRevision = settlementAccount?.Revision ?? 0L;
        long populationAccountIncrements = 0L;
        long settlementAccountIncrements = 0L;
        float populationBalance = populationAccount?.Balance ?? 0f;
        float settlementBalance = settlementAccount?.Balance ?? 0f;
        List<CityProductionResult> production = new List<CityProductionResult>();
        List<CityConsumptionResult> consumption = new List<CityConsumptionResult>();
        List<CityDailyMaterialFlowSourceOutcome> sourceOutcomes =
            Array.Empty<CityDailyMaterialFlowSourceOutcome>().ToList();
        int? openingStock = null;
        int? postProductionStock = null;
        long? postProductionMarketRevision = null;
        int? actualFreeConsumption = null;
        int? closingStock = null;
        int finiteQuantity = 0;
        long finiteRevision = FiniteSource?.Revision ?? 0L;
        long finiteDay = manifest.AbsoluteDay;
        bool mixedProduction = false;
        bool finiteSourceFirst = false;
        bool hasMarketReplacement = true;

        if (kind == CityDailyEconomyStepKind.Production)
        {
            LocalMaterialFlowProfile profile = GetProfile();
            if (profile == LocalMaterialFlowProfile.ExogenousDaily
                && (Definition.Values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
                    .Any(source => source != null && source.initialReserve != 0)) return false;
            if (profile == LocalMaterialFlowProfile.MixedSourcesDaily)
            {
                if (!TryPrepareMixedProduction(manifest.AbsoluteDay, marketRevision, marketState,
                        out marketState, out marketIncrements, out finiteQuantity, out finiteRevision,
                        out int mixedOpeningStock, out int mixedPostProductionStock, out sourceOutcomes)) return false;
                openingStock = mixedOpeningStock;
                postProductionStock = mixedPostProductionStock;
                postProductionMarketRevision = checked(marketRevision + marketIncrements);
                mixedProduction = true;
                foreach (CityDailyMaterialFlowSourceOutcome outcome in sourceOutcomes)
                    if (outcome.AppliedQuantity > 0)
                        production.Add(new CityProductionResult(RuntimeId, FiniteSource.Item.DefinitionId,
                            outcome.AppliedQuantity, Counterparty.CounterpartyRuntimeId));
                hasMarketReplacement = true;
            }
            else if (profile == LocalMaterialFlowProfile.FiniteReserveDaily)
            {
                if (!TryMatchFiniteSource(false, out DailyV1CityProductionConfig source)) return false;
                if (FiniteSource.RemainingReserve < 0 || FiniteSource.RemainingReserve > FiniteSource.InitialReserve
                    || FiniteSource.DailyOutputLimit <= 0
                    || manifest.AbsoluteDay <= FiniteSource.LastProcessedDay) return false;
                finiteRevision = FiniteSource.Revision;
                finiteQuantity = Math.Min(FiniteSource.DailyOutputLimit, FiniteSource.RemainingReserve);
                if (!Market.TryCreateFiniteSourceScenario(Definition.Values.materialFlowLocationId,
                        FiniteSource.CaptureSnapshot(), out FiniteSourceScenarioRuntime scenario,
                        out FiniteSourceScenarioSnapshot currentSnapshot, out _)
                    || !scenario.TryPrepareAdvance(currentSnapshot, manifest.AbsoluteDay,
                        out PreparedFiniteSourceAdvance advance, out _)
                    || advance.Quantity != finiteQuantity) return false;
                if (advance.Status == FiniteSourceOperationStatus.Applied)
                {
                    if (!scenario.TryApplyPrepared(currentSnapshot, advance,
                            out FiniteSourceScenarioSnapshot nextSnapshot, out _)
                        || !Market.TryPrepareFiniteStockIncrease(currentSnapshot, advance, nextSnapshot,
                            out marketState, out _)
                        || !FiniteSource.CanInstall(finiteRevision, manifest.AbsoluteDay)) return false;
                    marketIncrements = 1;
                    production.Add(new CityProductionResult(RuntimeId, FiniteSource.Item.DefinitionId,
                        finiteQuantity, Counterparty.CounterpartyRuntimeId));
                    finiteSourceFirst = true;
                }
                else if (advance.Status == FiniteSourceOperationStatus.Exhausted)
                    hasMarketReplacement = false;
                else return false;
            }
            else
            {
                foreach (DailyV1CityProductionConfig source in Definition.Values.productionConfigs
                    ?? Array.Empty<DailyV1CityProductionConfig>())
                {
                    if (source == null || string.IsNullOrWhiteSpace(source.item) || source.amountPerDay <= 0) continue;
                    if (!Definitions.Items.TryGetValue(source.item, out DailyV1ItemDefinition item)) return false;
                    if (!Market.TryApplyPreparedStockDelta(marketState, item, source.amountPerDay, 100,
                            out DailyV1MarketPreparedState replacement, out _)) continue;
                    marketState = replacement;
                    marketIncrements++;
                    production.Add(new CityProductionResult(RuntimeId, item.DefinitionId,
                        source.amountPerDay, Counterparty.CounterpartyRuntimeId));
                }
            }
        }
        else if (kind == CityDailyEconomyStepKind.Consumption)
        {
            if (GetProfile() == LocalMaterialFlowProfile.MixedSourcesDaily)
            {
                if (!TryGetDailyEconomyReceiptForManifest(manifest, CityDailyEconomyStepKind.Production,
                        out CityDailyEconomyReceipt productionReceipt)
                    || !productionReceipt.OpeningStock.HasValue
                    || !productionReceipt.PostProductionStock.HasValue
                    || !productionReceipt.PostProductionMarketRevision.HasValue
                    || !string.Equals(productionReceipt.BoundaryOccurrenceId, manifest.BoundaryOccurrenceId, StringComparison.Ordinal)
                    || productionReceipt.AbsoluteDay != manifest.AbsoluteDay
                    || Market.Revision != productionReceipt.PostProductionMarketRevision.Value
                    || Definition.Values.marketItems == null || Definition.Values.marketItems.Count == 0
                    || !TryResolveItem(Definition.Values.marketItems[0]?.item, out DailyV1ItemDefinition productionItem)
                    || Market.GetAmount(productionItem) != productionReceipt.PostProductionStock.Value) return false;
                sourceOutcomes = productionReceipt.SourceOutcomes.ToList();
                openingStock = productionReceipt.OpeningStock;
                postProductionStock = productionReceipt.PostProductionStock;
                postProductionMarketRevision = productionReceipt.PostProductionMarketRevision;
            }

            foreach (DailyV1MarketItemConfig row in Definition.Values.marketItems
                ?? Array.Empty<DailyV1MarketItemConfig>())
            {
                if (row == null || string.IsNullOrWhiteSpace(row.item)
                    || row.consumptionPer1000Population <= 0f) continue;
                if (!TryResolveItem(row.item, out DailyV1ItemDefinition item)) return false;
                int desired = Math.Max(0, UnityMathfCompatibility.RoundToInt(
                    preparedPopulation / 1000f * row.consumptionPer1000Population));
                int consumed = 0;
                float unitPrice = 0f;
                float paid = 0f;
                Market.TryGetPreparedItem(marketState, item, out DailyV1MarketItemState preparedItem);
                int amount = preparedItem?.Amount ?? 0;
                float price = preparedItem?.CurrentPrice ?? 0f;
                if (desired > 0 && amount > 0)
                {
                    if (PopulationEconomy.PaymentMode == ConsumptionPaymentMode.Free)
                        consumed = Math.Min(desired, amount);
                    else if (populationAccount != null && settlementAccount != null)
                    {
                        unitPrice = UnityMathfCompatibility.Max(0.01f, price);
                        if (float.IsNaN(unitPrice) || float.IsInfinity(unitPrice)) unitPrice = 0f;
                        if (unitPrice <= 0f)
                        {
                            consumption.Add(new CityConsumptionResult(RuntimeId,
                                PopulationEconomy.PopulationEconomicRuntimeId, row.item, desired, 0,
                                PopulationEconomy.PaymentMode, 0f, 0f));
                            continue;
                        }
                        float affordableRaw = populationBalance / unitPrice;
                        int affordable = populationBalance >= unitPrice
                            ? (float.IsInfinity(affordableRaw) || affordableRaw >= int.MaxValue
                                ? int.MaxValue
                                : UnityMathfCompatibility.Max(0, UnityMathfCompatibility.FloorToInt(affordableRaw)))
                            : 0;
                        consumed = Math.Min(desired, Math.Min(amount, affordable));
                        paid = unitPrice * consumed;
                        float nextPopulationBalance = populationBalance - paid;
                        float nextSettlementBalance = settlementBalance + paid;
                        if (consumed > 0 && (float.IsInfinity(paid) || float.IsNaN(paid)
                            || populationBalance < paid || nextPopulationBalance < 0f
                            || nextPopulationBalance >= populationBalance
                            || float.IsInfinity(nextSettlementBalance) || nextSettlementBalance <= settlementBalance))
                        { consumed = 0; unitPrice = 0f; paid = 0f; }
                        if (consumed == 0) { unitPrice = 0f; paid = 0f; }
                        if (consumed > 0)
                        {
                            populationBalance = nextPopulationBalance;
                            settlementBalance = nextSettlementBalance;
                            populationAccountIncrements++;
                            settlementAccountIncrements++;
                        }
                    }
                    if (consumed > 0)
                    {
                        if (preparedItem == null
                            || !Market.TryApplyPreparedStockDelta(marketState, item, -consumed,
                                preparedItem.DesiredAmount, out marketState, out _)) return false;
                        marketIncrements++;
                    }
                }
                consumption.Add(new CityConsumptionResult(RuntimeId,
                    PopulationEconomy.PopulationEconomicRuntimeId, row.item, desired, consumed,
                    PopulationEconomy.PaymentMode, unitPrice, paid));
            }
            if (GetProfile() == LocalMaterialFlowProfile.MixedSourcesDaily)
            {
                if (PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free) return false;
                int actual = 0;
                foreach (CityConsumptionResult result in consumption)
                    actual = checked(actual + result.ConsumedQuantity);
                if (Definition.Values.marketItems == null || Definition.Values.marketItems.Count == 0
                    || !TryResolveItem(Definition.Values.marketItems[0]?.item, out DailyV1ItemDefinition closingItem)) return false;
                Market.TryGetPreparedItem(marketState, closingItem, out DailyV1MarketItemState closingRow);
                actualFreeConsumption = actual;
                closingStock = closingRow?.Amount ?? 0;
            }
        }
        else
        {
            if (!Market.TryPreparePriceRefresh(marketState,
                    out DailyV1MarketPreparedState replacement, out bool changed)) return false;
            marketState = replacement;
            if (changed) marketIncrements++;
        }

        if (!Market.CanInstall(marketRevision, marketIncrements)
            || (marketIncrements > 0 && !Market.CanInstallMaterialFlow(marketRevision, marketIncrements))
            || (mixedProduction && FiniteSource.Revision != finiteRevision)
            || (mixedProduction && finiteQuantity > 0 && !FiniteSource.CanInstall(finiteRevision, finiteDay))
            || (finiteSourceFirst && !FiniteSource.CanInstall(finiteRevision, finiteDay))
            || (populationAccount != null && !MoneyAccountOwnerBridge.CanInstall(populationAccount,
                populationAccountRevision, populationAccountIncrements, populationBalance))
            || (settlementAccount != null && !MoneyAccountOwnerBridge.CanInstall(settlementAccount,
                settlementAccountRevision, settlementAccountIncrements, settlementBalance))) return false;

        CityDailyEconomyReceipt receipt = new CityDailyEconomyReceipt(identity, fingerprint,
            production.AsReadOnly(), consumption.AsReadOnly(), manifest.BoundaryOccurrenceId,
            manifest.AbsoluteDay, sourceOutcomes.AsReadOnly(), openingStock, postProductionStock,
            postProductionMarketRevision, actualFreeConsumption, closingStock);
        prepared = new DailyV1CityEconomyStepCommit(this, receipt,
            hasMarketReplacement ? marketState : null, marketRevision, marketIncrements,
            populationAccount, populationAccountRevision, populationAccountIncrements, populationBalance,
            settlementAccount, settlementAccountRevision, settlementAccountIncrements, settlementBalance,
            dailyEconomyReceiptRevision, false, step.OwnerRevision, preparedPopulationRevision,
            preparedPopulation, fingerprint, finiteQuantity, finiteDay, mixedProduction, finiteSourceFirst,
            (mixedProduction || finiteQuantity > 0) ? finiteRevision : -1L);
        failure = TimelineFailure.None;
        return true;
    }

    internal bool TryResolveDailyEconomyReceipt(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out CityDailyEconomyReceipt receipt, out TimelineFailure failure)
    {
        receipt = null;
        if (!TryGetIdentity(manifest, step, out string identity, out string fingerprint))
        { failure = TimelineFailure.ContinuationFailed; return false; }
        if (dailyEconomyReceipts != null && dailyEconomyReceipts.TryGetValue(identity, out CityDailyEconomyReceipt existing))
        {
            if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            { failure = TimelineFailure.ContinuationFailed; return false; }
            receipt = existing;
        }
        failure = TimelineFailure.None;
        return receipt != null;
    }

    internal bool TryGetDailyEconomyReceiptForManifest(BoundaryContinuationManifest manifest,
        CityDailyEconomyStepKind kind, out CityDailyEconomyReceipt receipt)
    {
        receipt = null;
        if (manifest?.Steps == null || dailyEconomyReceipts == null) return false;
        string stepId = GetStepId(kind);
        foreach (BoundaryContinuationStep candidate in manifest.Steps)
        {
            if (candidate == null || !string.Equals(candidate.StepId, stepId, StringComparison.Ordinal)) continue;
            if (!TryGetIdentity(manifest, candidate, out string identity, out string fingerprint)
                || !dailyEconomyReceipts.TryGetValue(identity, out CityDailyEconomyReceipt existing)
                || !string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)) return false;
            receipt = existing;
            return true;
        }
        return false;
    }

    internal bool TryCommitDailyEconomy(DailyV1CityEconomyStepCommit commit, out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (commit == null) return false;
        if (commit.Replay)
        {
            if (dailyEconomyReceipts != null
                && dailyEconomyReceipts.TryGetValue(commit.Receipt.Identity, out CityDailyEconomyReceipt old)
                && string.Equals(old.Fingerprint, commit.Fingerprint, StringComparison.Ordinal))
            { commit.Completed = true; failure = TimelineFailure.None; return true; }
            return false;
        }
        if (dailyEconomyReceiptRevision != commit.ExpectedReceiptRevision
            || dailyEconomyReceiptRevision == long.MaxValue
            || !string.Equals(commit.ExpectedOwnerRevision,
                StableKeyEncoding.Encode(RuntimeId, CaptureDailyEconomyConfiguration()), StringComparison.Ordinal)
            || (commit.ExpectedPopulationRevision >= 0
                && (Population.Revision != commit.ExpectedPopulationRevision
                    || Population.CurrentPopulation != commit.ExpectedPopulation))
            || dailyEconomyReceipts == null
            || dailyEconomyReceipts.ContainsKey(commit.Receipt.Identity)
            || !Market.CanInstall(commit.MarketRevision, commit.MarketIncrements)
            || (commit.MarketIncrements > 0
                && (commit.MarketState == null
                    || !Market.CanInstallMaterialFlow(commit.MarketRevision, commit.MarketIncrements)))
            || (commit.MixedProduction && FiniteSource.Revision != commit.FiniteRevision)
            || (commit.FiniteQuantity > 0 && !FiniteSource.CanInstall(commit.FiniteRevision, commit.AbsoluteDay))
            || (commit.PopulationAccount != null && !MoneyAccountOwnerBridge.CanInstall(
                commit.PopulationAccount, commit.PopulationAccountRevision,
                commit.PopulationAccountIncrements, commit.PopulationBalance))
            || (commit.SettlementAccount != null && !MoneyAccountOwnerBridge.CanInstall(
                commit.SettlementAccount, commit.SettlementAccountRevision,
                commit.SettlementAccountIncrements, commit.SettlementBalance))) return false;

        if (commit.MarketIncrements > 0 && !commit.FiniteSourceFirst
            && !Market.TryInstall(commit.MarketRevision, commit.MarketIncrements, commit.MarketState)) return false;
        if (commit.FiniteQuantity > 0
            && !FiniteSource.TryInstall(commit.FiniteRevision, commit.AbsoluteDay, commit.FiniteQuantity)) return false;
        if (commit.MarketIncrements > 0 && commit.FiniteSourceFirst
            && !Market.TryInstall(commit.MarketRevision, commit.MarketIncrements, commit.MarketState)) return false;
        if (commit.MarketIncrements > 0) Market.NotifyMaterialFlowInstalled();
        if (commit.PopulationAccount != null) MoneyAccountOwnerBridge.InstallPrepared(
            commit.PopulationAccount, commit.PopulationAccountRevision,
            commit.PopulationAccountIncrements, commit.PopulationBalance);
        if (commit.SettlementAccount != null) MoneyAccountOwnerBridge.InstallPrepared(
            commit.SettlementAccount, commit.SettlementAccountRevision,
            commit.SettlementAccountIncrements, commit.SettlementBalance);

        dailyEconomyReceipts = new Dictionary<string, CityDailyEconomyReceipt>(
            dailyEconomyReceipts, StringComparer.Ordinal) { [commit.Receipt.Identity] = commit.Receipt };
        dailyEconomyReceiptRevision++;
        commit.Completed = true;
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryPrepareMixedProduction(long day, long marketRevision,
        DailyV1MarketPreparedState initialMarketState, out DailyV1MarketPreparedState marketState,
        out long marketIncrements, out int finiteApplied, out long finiteRevision,
        out int openingStock, out int postProductionStock,
        out List<CityDailyMaterialFlowSourceOutcome> outcomes)
    {
        marketState = initialMarketState;
        marketIncrements = 0;
        finiteApplied = 0;
        finiteRevision = FiniteSource?.Revision ?? 0L;
        openingStock = postProductionStock = 0;
        outcomes = new List<CityDailyMaterialFlowSourceOutcome>();
        DailyV1CityData city = Definition.Values;
        if (FiniteSource == null || !DailyV1MixedSourceProfileAdmission.TryValidate(city, out _)
            || !TryMatchFiniteSource(true, out _)
            || FiniteSource.RemainingReserve < 0 || FiniteSource.RemainingReserve > FiniteSource.InitialReserve
            || FiniteSource.DailyOutputLimit <= 0
            || !Market.TryCountPreparedItems(marketState, FiniteSource.Item, out int count) || count != 1
            || !Market.TryGetPreparedItem(marketState, FiniteSource.Item, out DailyV1MarketItemState item)
            || item == null) return false;
        openingStock = item.Amount;
        List<DailyV1CityProductionConfig> ordered = new List<DailyV1CityProductionConfig>(city.productionConfigs);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.productionSourceId ?? string.Empty, right?.productionSourceId ?? string.Empty));
        long totalApplied = 0L;
        foreach (DailyV1CityProductionConfig source in ordered)
        {
            CityProductionSourceKind sourceKind = (CityProductionSourceKind)(source?.sourceKind?.Value ?? 0);
            int planned = sourceKind == CityProductionSourceKind.FiniteReserveDaily
                ? Math.Min(source.amountPerDay, FiniteSource.RemainingReserve)
                : source.amountPerDay;
            string rejection = planned == 0 ? "ReserveExhausted" : string.Empty;
            int applied = 0;
            if (planned > 0)
            {
                if (!Market.TryApplyPreparedStockDelta(marketState, FiniteSource.Item, planned,
                        item.DesiredAmount, out DailyV1MarketPreparedState replacement,
                        out DailyV1MarketItemState updated)) rejection = "AggregateStockOverflow";
                else
                {
                    marketState = replacement;
                    item = updated;
                    applied = planned;
                    marketIncrements++;
                    totalApplied += applied;
                    if (sourceKind == CityProductionSourceKind.FiniteReserveDaily) finiteApplied = applied;
                }
            }
            outcomes.Add(new CityDailyMaterialFlowSourceOutcome(source.productionSourceId,
                sourceKind, source.amountPerDay, planned, applied, rejection));
        }
        if (marketIncrements > 0 && !Market.CanInstallMaterialFlow(marketRevision, marketIncrements)) return false;
        if (finiteApplied > 0 && !FiniteSource.CanInstall(finiteRevision, day)) return false;
        postProductionStock = item.Amount;
        return totalApplied <= int.MaxValue;
    }

    private bool TryMatchFiniteSource(bool mixed, out DailyV1CityProductionConfig configured)
    {
        configured = null;
        DailyV1CityData city = Definition.Values;
        if (FiniteSource == null || city.productionConfigs == null) return false;
        foreach (DailyV1CityProductionConfig row in city.productionConfigs)
        {
            if (row == null) continue;
            CityProductionSourceKind kind = (CityProductionSourceKind)(row.sourceKind?.Value ?? 0);
            if (mixed && kind != CityProductionSourceKind.FiniteReserveDaily) continue;
            if (!mixed && row != city.productionConfigs[0]) continue;
            if (row.amountPerDay != FiniteSource.DailyOutputLimit
                || row.initialReserve != FiniteSource.InitialReserve
                || !string.Equals(row.productionSourceId, FiniteSource.ProductionSourceId, StringComparison.Ordinal)
                || !string.Equals(row.contentRevision, FiniteSource.ContentRevision, StringComparison.Ordinal)
                || !TryResolveItem(row.item, out DailyV1ItemDefinition item)
                || !ReferenceEquals(item, FiniteSource.Item)
                || !string.Equals(city.settlementSemanticId, FiniteSource.SettlementSemanticId, StringComparison.Ordinal)
                || !string.Equals(city.marketStoreSemanticId, FiniteSource.MarketStoreSemanticId, StringComparison.Ordinal)) return false;
            if (configured != null) return false;
            configured = row;
            if (!mixed) break;
        }
        return configured != null;
    }

    private bool TryGetIdentity(BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        out string identity, out string fingerprint)
    {
        identity = fingerprint = null;
        if (manifest == null || step == null || manifest.Steps == null
            || step.Ordinal < 0 || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || !string.Equals(step.OwnerId, RuntimeId, StringComparison.Ordinal)
            || !string.Equals(step.OperationVersion, DailyEconomyOperationVersion, StringComparison.Ordinal)
            || !string.Equals(step.Disposition, "included", StringComparison.Ordinal)
            || !string.Equals(step.OwnerRevision,
                StableKeyEncoding.Encode(RuntimeId, CaptureDailyEconomyConfiguration()), StringComparison.Ordinal)
            || !string.Equals(manifest.BoundaryOccurrenceId, StableKeyEncoding.Encode(
                manifest.WorldId, manifest.ProfileId, manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture)),
                StringComparison.Ordinal)) return false;
        CityDailyEconomyStepKind kind = GetKind(step.StepId);
        if (!string.Equals(step.StepId, GetStepId(kind), StringComparison.Ordinal)
            || !string.Equals(step.OperationKind, "city-economy." + kind.ToString().ToLowerInvariant(), StringComparison.Ordinal)) return false;
        identity = StableKeyEncoding.Encode(manifest.BoundaryOccurrenceId, step.StepId, RuntimeId);
        fingerprint = StableKeyEncoding.Encode(manifest.BoundaryOccurrenceId, manifest.ContinuationId,
            manifest.ConfigurationIdentity, manifest.ContentIdentity,
            step.Ordinal.ToString(CultureInfo.InvariantCulture), step.StepId, step.OwnerId,
            step.OperationKind, step.OperationVersion, step.OwnerRevision, step.Payload);
        return true;
    }

    private string CaptureDailyEconomyConfiguration()
    {
        DailyV1CityData city = Definition.Values;
        MarketLiquidityMode liquidityMode = (MarketLiquidityMode)(city.marketLiquidity?.liquidityMode?.Value
            ?? (int)MarketLiquidityMode.Open);
        float liquidityPower = city.marketLiquidity?.initialPurchasingPower ?? 0f;
        ConsumptionPaymentMode populationMode = (ConsumptionPaymentMode)(city.populationConsumption?.paymentMode?.Value
            ?? (int)ConsumptionPaymentMode.Free);
        float populationPower = city.populationConsumption?.initialPurchasingPower ?? 0f;
        List<string> parts = new List<string> { RuntimeId, Definition.DefinitionId,
            GetProfile().ToString(), city.settlementSemanticId ?? string.Empty,
            city.materialFlowLocationId ?? string.Empty, city.marketStoreSemanticId ?? string.Empty,
            city.initialPopulation.ToString(CultureInfo.InvariantCulture), PopulationEconomy.PaymentMode.ToString(),
            liquidityMode.ToString(), liquidityPower.ToString("R", CultureInfo.InvariantCulture),
            populationMode.ToString(), populationPower.ToString("R", CultureInfo.InvariantCulture) };
        List<DailyV1CityProductionConfig> sources = new List<DailyV1CityProductionConfig>(
            city.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>());
        if (GetProfile() == LocalMaterialFlowProfile.MixedSourcesDaily)
            sources.Sort((left, right) => StringComparer.Ordinal.Compare(
                left?.productionSourceId ?? string.Empty, right?.productionSourceId ?? string.Empty));
        foreach (DailyV1CityProductionConfig row in sources)
        {
            parts.Add("p");
            AddSelectedItemIdentity(parts, row?.item);
            if (GetProfile() == LocalMaterialFlowProfile.MixedSourcesDaily)
                parts.Add(row != null ? ((CityProductionSourceKind)(row.sourceKind?.Value ?? 0)).ToString() : "null-kind");
            parts.Add(row != null ? row.amountPerDay.ToString(CultureInfo.InvariantCulture) : "null");
            parts.Add(row != null ? row.initialReserve.ToString(CultureInfo.InvariantCulture) : "null");
            parts.Add(row != null ? row.productionSourceId ?? string.Empty : "null");
            parts.Add(row != null ? row.contentRevision ?? string.Empty : "null");
        }
        foreach (DailyV1MarketItemConfig row in city.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
        {
            parts.Add("c");
            AddSelectedItemIdentity(parts, row?.item);
            parts.Add(row != null ? row.initialAmount.ToString(CultureInfo.InvariantCulture) : "null");
            parts.Add(row != null ? row.desiredAmount.ToString(CultureInfo.InvariantCulture) : "null");
            parts.Add(row != null ? row.consumptionPer1000Population.ToString("R", CultureInfo.InvariantCulture) : "null");
        }
        return StableKeyEncoding.Encode(parts.ToArray());
    }

    private void AddSelectedItemIdentity(List<string> parts, string itemId)
    {
        parts.Add(itemId ?? string.Empty);
        if (TryResolveItem(itemId, out DailyV1ItemDefinition item))
        { parts.Add(item.Values.itemName ?? string.Empty); parts.Add(item.Values.basePrice.ToString("R", CultureInfo.InvariantCulture)); }
        else { parts.Add(string.Empty); parts.Add(string.Empty); }
    }

    private bool TryResolveItem(string itemId, out DailyV1ItemDefinition item)
    {
        item = null;
        return !string.IsNullOrWhiteSpace(itemId) && Definitions.Items.TryGetValue(itemId, out item) && item != null;
    }

    private LocalMaterialFlowProfile GetProfile() =>
        (LocalMaterialFlowProfile)(Definition.Values.materialFlowProfile?.Value ?? (int)LocalMaterialFlowProfile.ExogenousDaily);

    private string GetStepId(CityDailyEconomyStepKind kind) =>
        "city-economy-" + kind.ToString().ToLowerInvariant() + ":" + RuntimeId;

    private CityDailyEconomyStepKind GetKind(string id) =>
        id != null && id.StartsWith("city-economy-production:", StringComparison.Ordinal)
            ? CityDailyEconomyStepKind.Production
            : id != null && id.StartsWith("city-economy-consumption:", StringComparison.Ordinal)
                ? CityDailyEconomyStepKind.Consumption
                : CityDailyEconomyStepKind.PriceRefresh;
}

internal sealed class DailyV1CityEconomyStepCommit : IBoundaryContinuationStepCommit
{
    private readonly DailyV1CityRuntimeOwners owner;
    internal CityDailyEconomyReceipt Receipt { get; }
    internal DailyV1MarketPreparedState MarketState { get; }
    internal long MarketRevision { get; }
    internal long MarketIncrements { get; }
    internal MoneyAccountRuntime PopulationAccount { get; }
    internal long PopulationAccountRevision { get; }
    internal long PopulationAccountIncrements { get; }
    internal float PopulationBalance { get; }
    internal MoneyAccountRuntime SettlementAccount { get; }
    internal long SettlementAccountRevision { get; }
    internal long SettlementAccountIncrements { get; }
    internal float SettlementBalance { get; }
    internal long ExpectedReceiptRevision { get; }
    internal bool Replay { get; }
    internal string ExpectedOwnerRevision { get; }
    internal long ExpectedPopulationRevision { get; }
    internal int ExpectedPopulation { get; }
    internal string Fingerprint { get; }
    internal int FiniteQuantity { get; }
    internal long AbsoluteDay { get; }
    internal bool MixedProduction { get; }
    internal bool FiniteSourceFirst { get; }
    internal long FiniteRevision { get; }
    internal bool Completed { get; set; }
    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();

    internal DailyV1CityEconomyStepCommit(DailyV1CityRuntimeOwners owner,
        CityDailyEconomyReceipt receipt, DailyV1MarketPreparedState marketState,
        long marketRevision, long marketIncrements,
        MoneyAccountRuntime populationAccount, long populationAccountRevision,
        long populationAccountIncrements, float populationBalance,
        MoneyAccountRuntime settlementAccount, long settlementAccountRevision,
        long settlementAccountIncrements, float settlementBalance,
        long expectedReceiptRevision, bool replay, string expectedOwnerRevision,
        long expectedPopulationRevision, int expectedPopulation, string fingerprint,
        int finiteQuantity, long absoluteDay, bool mixedProduction, bool finiteSourceFirst,
        long finiteRevision)
    {
        this.owner = owner;
        Receipt = receipt;
        MarketState = marketState;
        MarketRevision = marketRevision;
        MarketIncrements = marketIncrements;
        PopulationAccount = populationAccount;
        PopulationAccountRevision = populationAccountRevision;
        PopulationAccountIncrements = populationAccountIncrements;
        PopulationBalance = populationBalance;
        SettlementAccount = settlementAccount;
        SettlementAccountRevision = settlementAccountRevision;
        SettlementAccountIncrements = settlementAccountIncrements;
        SettlementBalance = settlementBalance;
        ExpectedReceiptRevision = expectedReceiptRevision;
        Replay = replay;
        ExpectedOwnerRevision = expectedOwnerRevision;
        ExpectedPopulationRevision = expectedPopulationRevision;
        ExpectedPopulation = expectedPopulation;
        Fingerprint = fingerprint;
        FiniteQuantity = finiteQuantity;
        AbsoluteDay = absoluteDay;
        MixedProduction = mixedProduction;
        FiniteSourceFirst = finiteSourceFirst;
        FiniteRevision = finiteRevision;
    }

    public bool TryCommit(out TimelineFailure failure)
    {
        if (Completed) { failure = TimelineFailure.None; return true; }
        return owner.TryCommitDailyEconomy(this, out failure);
    }
}
