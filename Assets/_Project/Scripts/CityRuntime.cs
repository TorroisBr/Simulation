using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using UnityEngine;

[Serializable]
public class CityRuntime
{
    [NonSerialized] private MutationGuardBinding runtimeMutationGuardBinding = new MutationGuardBinding();
    [NonSerialized] private Func<bool> p12PresenceMutationAdmission;
    [NonSerialized] private Action p12PresenceMutationCommitted;
    [SerializeField] private string runtimeId;
    [SerializeField] private CityData cityData;
    [NonSerialized] private DailyV1CityDefinition selectedDailyV1Definition;
    [NonSerialized] private DailyV1EffectiveDefinitions selectedDefinitionGraph;
    [NonSerialized] private DailyV1CityRuntimeOwners selectedRuntimeOwners;
    [NonSerialized] private Func<string, NpcRuntime> selectedNpcPresenceProjectionResolver;
    [NonSerialized] private SettlementPopulationRuntime population;
    [SerializeField] private MarketRuntime market;
    [NonSerialized] private MarketCounterpartyRuntime marketCounterparty;
    [NonSerialized] private PopulationEconomyRuntime populationEconomy;
    [NonSerialized] private SpatialLocationRuntime location;
    [NonSerialized] private List<NpcRuntime> importantNpcs = new List<NpcRuntime>();
    [NonSerialized] private ReadOnlyCollection<NpcRuntime> readOnlyImportantNpcs;
    [NonSerialized] private long importantNpcRevision;
    [NonSerialized] private SimulationLogger logger;
    [NonSerialized] private DailyV1DefinitionAdapter.SelectedBindings selectedDefinitionBindings;
    [NonSerialized] private Dictionary<string, CityDailyEconomyReceipt> dailyEconomyReceipts =
        new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal);
    [NonSerialized] private long dailyEconomyReceiptRevision;

    private const string DailyEconomyVersion = "1";
    [NonSerialized] private LocalDailyMaterialFlowResult lastMaterialFlow;
    [NonSerialized] private FiniteProductionSourceStore finiteProductionSources;

    public string RuntimeId => runtimeId;
    /// <summary>
    /// Compatibility bridge for Unity and the existing P12 owner codec. Selected
    /// construction retains the exact admitted asset binding, but does not take
    /// authored CityData as a constructor input or retain it as runtime state.
    /// </summary>
    public CityData CityData
    {
        get
        {
            if (selectedDailyV1Definition != null
                && TryResolveSelectedDailyV1CityAsset(selectedDailyV1Definition.DefinitionId, out CityData selectedAsset))
                return selectedAsset;
            return cityData;
        }
    }
    private bool HasConfiguredDefinition => selectedDailyV1Definition != null || cityData != null;
    public string DefinitionId => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.DefinitionId
        : cityData != null ? cityData.DefinitionId : string.Empty;
    internal DailyV1CityDefinition SelectedDailyV1Definition => selectedDailyV1Definition;
    internal DailyV1EffectiveDefinitions SelectedDailyV1DefinitionGraph => selectedDefinitionGraph;
    internal DailyV1CityRuntimeOwners SelectedRuntimeOwners => selectedRuntimeOwners;
    internal IReadOnlyList<DailyV1MarketItemConfig> SelectedDailyV1MarketInputs =>
        selectedDailyV1Definition?.Values?.marketItems;
    internal IReadOnlyList<DailyV1CityProductionConfig> SelectedDailyV1ProductionInputs =>
        selectedDailyV1Definition?.Values?.productionConfigs;
    internal IReadOnlyList<DailyV1CityConnection> SelectedDailyV1Connections =>
        selectedDailyV1Definition != null ? selectedDailyV1Definition.Values.connections : null;
    public SpatialLocationRuntime Location => location;
    public SettlementPopulationRuntime Population
    {
        get
        {
            if (selectedRuntimeOwners != null) return selectedRuntimeOwners.Population;
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
            if (selectedRuntimeOwners != null) return selectedRuntimeOwners.Counterparty;
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
            if (selectedRuntimeOwners != null) return selectedRuntimeOwners.PopulationEconomy;
            if (populationEconomy == null)
            {
                populationEconomy = CreateConfiguredPopulationEconomy();
            }

            return populationEconomy;
        }
    }
    public IReadOnlyList<NpcRuntime> ImportantNpcs
    {
        get
        {
            if (selectedRuntimeOwners != null && selectedNpcPresenceProjectionResolver != null)
            {
                if (!TryCreateSelectedNpcPresenceProjection(
                        selectedNpcPresenceProjectionResolver,
                        out IReadOnlyList<NpcRuntime> projection))
                    throw new InvalidOperationException(
                        "The Core City-presence owner could not resolve its exact registered Unity NPC projections.");
                return projection;
            }

            return readOnlyImportantNpcs ?? (readOnlyImportantNpcs = EnsureImportantNpcs().AsReadOnly());
        }
    }
    public long ImportantNpcRevision => selectedRuntimeOwners?.NpcPresence.Revision ?? importantNpcRevision;
    public string CityName => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.cityName
        : cityData != null ? cityData.cityName : "Cidade desconhecida";
    // Selected gameplay reads the canonical typed graph directly. These legacy
    // projections are used only by non-selected Unity compatibility paths.
    private List<MarketItemConfig> RuntimeMarketItems => selectedDailyV1Definition == null
        ? cityData != null ? cityData.marketItems : null
        : null;
    private List<CityProductionConfig> RuntimeProductionConfigs => selectedDailyV1Definition == null
        ? cityData != null ? cityData.productionConfigs : null
        : null;
    private MarketLiquidityMode RuntimeMarketLiquidityMode => selectedDailyV1Definition != null
        ? (MarketLiquidityMode)(selectedDailyV1Definition.Values.marketLiquidity?.liquidityMode?.Value ?? (int)MarketLiquidityMode.Open)
        : cityData != null ? cityData.MarketLiquidity.liquidityMode : MarketLiquidityMode.Open;
    private float RuntimeMarketLiquidityPurchasingPower => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.marketLiquidity?.initialPurchasingPower ?? 0f
        : cityData != null ? cityData.MarketLiquidity.initialPurchasingPower : 0f;
    private ConsumptionPaymentMode RuntimePopulationPaymentMode => selectedDailyV1Definition != null
        ? (ConsumptionPaymentMode)(selectedDailyV1Definition.Values.populationConsumption?.paymentMode?.Value ?? (int)ConsumptionPaymentMode.Free)
        : cityData != null ? cityData.PopulationConsumption.paymentMode : ConsumptionPaymentMode.Free;
    private float RuntimePopulationPurchasingPower => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.populationConsumption?.initialPurchasingPower ?? 0f
        : cityData != null ? cityData.PopulationConsumption.initialPurchasingPower : 0f;
    private int RuntimeInitialPopulation => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.initialPopulation
        : cityData != null ? cityData.initialPopulation : 0;
    private LocalMaterialFlowProfile RuntimeMaterialFlowProfile => selectedDailyV1Definition != null
        ? (LocalMaterialFlowProfile)(selectedDailyV1Definition.Values.materialFlowProfile?.Value ?? 0)
        : cityData != null ? cityData.materialFlowProfile : LocalMaterialFlowProfile.ExogenousDaily;
    private string RuntimeSettlementSemanticId => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.settlementSemanticId
        : cityData != null ? cityData.settlementSemanticId : null;
    private string RuntimeMaterialFlowLocationId => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.materialFlowLocationId
        : cityData != null ? cityData.materialFlowLocationId : null;
    private string RuntimeMarketStoreSemanticId => selectedDailyV1Definition != null
        ? selectedDailyV1Definition.Values.marketStoreSemanticId
        : cityData != null ? cityData.marketStoreSemanticId : null;
    internal string SettlementSemanticId => RuntimeSettlementSemanticId;
    internal string MaterialFlowLocationId => RuntimeMaterialFlowLocationId;
    public bool HasLocalDailyMaterialFlow => HasConfiguredDefinition
        && (finiteProductionSources != null
            || RuntimeMaterialFlowProfile != LocalMaterialFlowProfile.ExogenousDaily
            || !string.IsNullOrWhiteSpace(RuntimeSettlementSemanticId)
            || !string.IsNullOrWhiteSpace(RuntimeMaterialFlowLocationId)
            || !string.IsNullOrWhiteSpace(RuntimeMarketStoreSemanticId)
            || (selectedDailyV1Definition != null
                ? (selectedDailyV1Definition.Values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
                    .Any(source => source != null
                        && (!string.IsNullOrWhiteSpace(source.productionSourceId) || source.initialReserve != 0))
                : (RuntimeProductionConfigs != null && RuntimeProductionConfigs.Exists(source =>
                    source != null && (!string.IsNullOrWhiteSpace(source.productionSourceId) || source.initialReserve != 0)))));
    public LocalDailyMaterialFlowResult LastMaterialFlow => selectedRuntimeOwners != null
        ? selectedRuntimeOwners.LastMaterialFlow
        : lastMaterialFlow;
    public FiniteProductionSourceStore FiniteProductionSources => finiteProductionSources;

    internal bool TryGetInstalledSnapshotOwners(
        out SettlementPopulationRuntime installedPopulation,
        out MarketRuntime installedMarket,
        out MarketCounterpartyRuntime installedCounterparty,
        out PopulationEconomyRuntime installedPopulationEconomy,
        out SpatialLocationRuntime installedLocation)
    {
        installedPopulation = population;
        installedMarket = market;
        installedCounterparty = marketCounterparty;
        installedPopulationEconomy = populationEconomy;
        installedLocation = location;
        if (selectedDailyV1Definition != null
            && (selectedRuntimeOwners == null
                || !ReferenceEquals(selectedRuntimeOwners.Population, installedPopulation)
                || !ReferenceEquals(selectedRuntimeOwners.Market, installedMarket?.SelectedDailyV1Owner)
                || !ReferenceEquals(selectedRuntimeOwners.Counterparty, installedCounterparty)
                || !ReferenceEquals(selectedRuntimeOwners.PopulationEconomy, installedPopulationEconomy)))
            return false;
        return installedPopulation != null
            && installedMarket != null
            && installedCounterparty != null
            && installedPopulationEconomy != null
            && installedLocation != null;
    }

    internal bool TryCopyOwnerSnapshotMembership(
        out IReadOnlyList<string> npcRuntimeIds,
        out long membershipRevision)
    {
        npcRuntimeIds = null;
        membershipRevision = ImportantNpcRevision;
        IReadOnlyList<NpcRuntime> projection;
        if (selectedRuntimeOwners != null && selectedNpcPresenceProjectionResolver != null)
        {
            if (!TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out projection))
                return false;
        }
        else
        {
            projection = ImportantNpcs;
        }
        if (membershipRevision < 0L || location == null
            || !HasConsistentSelectedNpcPresence)
        {
            return false;
        }

        List<string> copiedIds = new List<string>(projection.Count);
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in projection)
        {
            if (npc == null
                || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !uniqueIds.Add(npc.RuntimeId)
                || !ReferenceEquals(npc.CurrentCity, this)
                || !ReferenceEquals(npc.CurrentLocation, location))
            {
                return false;
            }

            copiedIds.Add(npc.RuntimeId);
        }

        npcRuntimeIds = new ReadOnlyCollection<string>(copiedIds);
        return true;
    }

    internal bool HasConsistentSelectedNpcPresence
    {
        get
        {
            if (selectedRuntimeOwners == null) return true;
            DailyV1CityNpcPresenceOwner owner = selectedRuntimeOwners.NpcPresence;
            if (!string.Equals(owner.CityRuntimeId, runtimeId, StringComparison.Ordinal))
                return false;

            if (selectedNpcPresenceProjectionResolver != null)
            {
                return TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out IReadOnlyList<NpcRuntime> resolved)
                    && resolved.Count == owner.OrderedNpcRuntimeIds.Count;
            }

            List<NpcRuntime> projection = EnsureImportantNpcs();
            if (owner.OrderedNpcRuntimeIds.Count != projection.Count)
                return false;
            for (int i = 0; i < projection.Count; i++)
            {
                if (projection[i] == null
                    || !string.Equals(owner.OrderedNpcRuntimeIds[i], projection[i].RuntimeId, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }

    internal void GetDailyEconomyReceiptExclusionProof(out int receiptCount, out long receiptRevision)
    {
        if (selectedRuntimeOwners != null)
        {
            selectedRuntimeOwners.GetDailyEconomyReceiptExclusionProof(out receiptCount, out receiptRevision);
            return;
        }
        receiptCount = dailyEconomyReceipts != null ? dailyEconomyReceipts.Count : 0;
        receiptRevision = dailyEconomyReceiptRevision;
    }

    internal void ValidateLocalDailyMaterialFlowAnchor(LegacySpatialAnchorBindingStore bindings, SpatialAuthorityStore spatial)
    {
        if (!HasLocalDailyMaterialFlow) return;
        if (string.IsNullOrWhiteSpace(RuntimeSettlementSemanticId)
            || string.IsNullOrWhiteSpace(RuntimeMaterialFlowLocationId)
            || string.IsNullOrWhiteSpace(RuntimeMarketStoreSemanticId)
            || (selectedDailyV1Definition != null
                ? selectedDailyV1Definition.Values.productionConfigs == null
                : RuntimeProductionConfigs == null))
            throw new LocalDailyMaterialFlowRejectedException("P14 requires settlement, LocationId, market store, and authored source configuration.");
        if (!Enum.IsDefined(typeof(LocalMaterialFlowProfile), RuntimeMaterialFlowProfile))
            throw new LocalDailyMaterialFlowRejectedException("Unsupported P14 local material flow profile.");
        if (selectedDailyV1Definition != null)
        {
            ValidateSelectedLocalDailyMaterialFlowAnchor(bindings, spatial);
            return;
        }
        bool mixedSources = RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily;
        if (mixedSources)
        {
            string rejection;
            bool validMixedSourceCity = selectedDailyV1Definition != null
                ? FiniteSourceProfileAdmission.TryValidateMixedSourceCity(selectedDailyV1Definition.Values, out rejection)
                : FiniteSourceProfileAdmission.TryValidateMixedSourceCity(cityData, out rejection);
            if (!validMixedSourceCity)
                throw new LocalDailyMaterialFlowRejectedException(rejection);
        }
        else if (RuntimeProductionConfigs.Count != 1)
            throw new LocalDailyMaterialFlowRejectedException("P14-A/P14-B require exactly one authored source.");
        CityProductionConfig source = mixedSources
            ? RuntimeProductionConfigs.Find(row => row != null
                && row.sourceKind == CityProductionSourceKind.ExogenousDaily)
            : RuntimeProductionConfigs[0];
        if (source == null || source.item == null || string.IsNullOrWhiteSpace(source.item.DefinitionId)
            || source.amountPerDay <= 0
            || string.IsNullOrWhiteSpace(source.productionSourceId) || string.IsNullOrWhiteSpace(source.contentRevision))
            throw new LocalDailyMaterialFlowRejectedException("P14-A source identity, item, positive quantity, and content revision are required.");
        if (!mixedSources && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.ExogenousDaily && source.initialReserve != 0)
            throw new LocalDailyMaterialFlowRejectedException("Finite reserve data requires the finite-reserve profile.");
        if (!mixedSources && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily && source.initialReserve < 0)
            throw new LocalDailyMaterialFlowRejectedException("Finite source reserve cannot be negative.");
        if (PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free
            || RuntimeMarketItems == null || RuntimeMarketItems.Count != 1
            || RuntimeMarketItems[0] == null || RuntimeMarketItems[0].item == null
            || !string.Equals(RuntimeMarketItems[0].item.DefinitionId, source.item.DefinitionId, StringComparison.Ordinal))
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires exactly one market item row matching its source item and free population consumption.");
        SpatialAnchorOwnerId owner = new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, RuntimeId);
        if (bindings == null || !bindings.TryGet(owner, out LocationId bound)
            || !string.Equals(bound?.Value, RuntimeMaterialFlowLocationId, StringComparison.Ordinal)
            || spatial == null || !spatial.TryGet(bound, out _))
            throw new LocalDailyMaterialFlowRejectedException("P14-A City anchor must resolve and match its authored stable LocationId.");
    }

    private void ValidateSelectedLocalDailyMaterialFlowAnchor(
        LegacySpatialAnchorBindingStore bindings,
        SpatialAuthorityStore spatial)
    {
        DailyV1CityData city = selectedDailyV1Definition?.Values;
        if (city == null || selectedDefinitionGraph == null || city.productionConfigs == null)
            throw new LocalDailyMaterialFlowRejectedException("P14 requires selected City source configuration and its admitted definition graph.");

        bool mixedSources = RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily;
        if (mixedSources)
        {
            if (!FiniteSourceProfileAdmission.TryValidateMixedSourceCity(city, out string rejection))
                throw new LocalDailyMaterialFlowRejectedException(rejection);
        }
        else if (city.productionConfigs.Count != 1)
            throw new LocalDailyMaterialFlowRejectedException("P14-A/P14-B require exactly one authored source.");

        DailyV1CityProductionConfig source = mixedSources
            ? city.productionConfigs.FirstOrDefault(row => row != null
                && (CityProductionSourceKind)(row.sourceKind?.Value ?? 0) == CityProductionSourceKind.ExogenousDaily)
            : city.productionConfigs[0];
        if (source == null
            || !TryResolveSelectedItemDefinition(source.item, out DailyV1ItemDefinition sourceItem)
            || source.amountPerDay <= 0
            || string.IsNullOrWhiteSpace(source.productionSourceId)
            || string.IsNullOrWhiteSpace(source.contentRevision))
            throw new LocalDailyMaterialFlowRejectedException("P14-A source identity, admitted item, positive quantity, and content revision are required.");

        if (!mixedSources && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.ExogenousDaily
            && source.initialReserve != 0)
            throw new LocalDailyMaterialFlowRejectedException("Finite reserve data requires the finite-reserve profile.");
        if (!mixedSources && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily
            && source.initialReserve < 0)
            throw new LocalDailyMaterialFlowRejectedException("Finite source reserve cannot be negative.");

        DailyV1PopulationConsumptionConfig consumption = city.populationConsumption;
        DailyV1MarketOwner marketOwner = Market?.SelectedDailyV1Owner;
        if (consumption?.paymentMode == null
            || consumption.paymentMode.Value != (int)ConsumptionPaymentMode.Free
            || city.marketItems == null || city.marketItems.Count != 1
            || city.marketItems[0] == null
            || !string.Equals(city.marketItems[0].item, source.item, StringComparison.Ordinal)
            || marketOwner == null || marketOwner.Items.Count != 1
            || !ReferenceEquals(marketOwner.Items[0]?.Item, sourceItem))
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires one selected Market owner row matching its source item and free population consumption.");

        SpatialAnchorOwnerId owner = new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, RuntimeId);
        if (bindings == null || !bindings.TryGet(owner, out LocationId bound)
            || !string.Equals(bound?.Value, RuntimeMaterialFlowLocationId, StringComparison.Ordinal)
            || spatial == null || !spatial.TryGet(bound, out _))
            throw new LocalDailyMaterialFlowRejectedException("P14-A City anchor must resolve and match its admitted stable LocationId.");
    }

    internal void SimulateLocalDailyMaterialFlow(long absoluteDay, string calendarVersion)
    {
        if (selectedDailyV1Definition != null
            && RuntimeMaterialFlowProfile != LocalMaterialFlowProfile.MixedSourcesDaily)
        {
            SimulateSelectedLocalDailyMaterialFlow(absoluteDay, calendarVersion);
            return;
        }
        if (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
        {
            SimulateMixedSourceDailyMaterialFlow(absoluteDay, calendarVersion);
            return;
        }
        if (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.ExogenousDaily
            && HasAuthoredFiniteReserve())
            throw new LocalDailyMaterialFlowRejectedException("Finite reserve data requires the finite-reserve profile.");
        if (finiteProductionSources != null || (HasConfiguredDefinition
            && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily))
        {
            if (!IsFiniteSourceConfigurationCurrent())
                throw new LocalDailyMaterialFlowRejectedException("Finite source configuration must match its constructed source owner.");
        }

        CityProductionConfig source = RuntimeProductionConfigs[0];
        MarketItemConfig itemConfig = RuntimeMarketItems?.Find(candidate => candidate != null
            && candidate.item != null && string.Equals(candidate.item.DefinitionId, source.item.DefinitionId, StringComparison.Ordinal));
        if (itemConfig == null || PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free)
            throw new LocalDailyMaterialFlowRejectedException("P14-A requires a market item and free population consumption for the source item.");

        int opening = Market.GetAmount(source.item);
        int applied;
        string rejection;
        if (RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily)
        {
            if (finiteProductionSources == null)
                throw new LocalDailyMaterialFlowRejectedException("Finite source owner was not constructed for the selected profile.");
            FiniteSourceProductionResult production = new FiniteSourceProductionService().TryProduceDaily(
                finiteProductionSources,
                Market,
                RuntimeSettlementSemanticId,
                RuntimeMarketStoreSemanticId,
                RuntimeMaterialFlowLocationId,
                source.item,
                absoluteDay,
                finiteProductionSources.Source.Revision,
                Market.Revision);
            applied = production.Quantity;
            rejection = production.Status == FiniteSourceProductionStatus.Applied
                ? string.Empty
                : production.RejectionReason;
        }
        else
        {
            applied = Market.AddStock(source.item, source.amountPerDay);
            rejection = applied == source.amountPerDay ? string.Empty : "AggregateStockOverflow";
        }
        int requested = Math.Max(0, UnityMathfCompatibility.RoundToInt(Population.CurrentPopulation / 1000f * itemConfig.consumptionPer1000Population));
        int actual = Market.RemoveStockUpTo(source.item, requested);
        int closing = Market.GetAmount(source.item);
        lastMaterialFlow = new LocalDailyMaterialFlowResult(RuntimeSettlementSemanticId, source.productionSourceId,
            RuntimeMarketStoreSemanticId, RuntimeMaterialFlowLocationId, source.item.DefinitionId,
            source.contentRevision, "Economy.Enabled=true;PaymentMode=Free", "simulation-calendar", calendarVersion,
            true, Population.CurrentPopulation, itemConfig.consumptionPer1000Population, absoluteDay, opening,
            source.amountPerDay, applied, rejection, requested, actual, closing);
        logger?.Log(SimulationLogCategory.EconomyProduction, $"{CityName} source {source.productionSourceId}: {applied}/{source.amountPerDay}");
        if (actual > 0) logger?.Log(SimulationLogCategory.EconomyConsumption, $"{CityName} consumed {actual} {source.item.itemName}");
    }

    private void SimulateMixedSourceDailyMaterialFlow(long absoluteDay, string calendarVersion)
    {
        if (selectedRuntimeOwners != null)
        {
            lastMaterialFlow = selectedRuntimeOwners.SimulateSelectedLocalDailyMaterialFlow(
                RuntimeMaterialFlowLocationId, absoluteDay, calendarVersion,
                (category, message) => logger?.Log(category, message));
            selectedRuntimeOwners.RecordSelectedMaterialFlow(lastMaterialFlow);
            return;
        }

        string configurationBefore = CaptureDailyEconomyConfiguration();
        if (!TryPrepareMixedSourceProduction(absoluteDay,
                out P14CMixedSourceProductionPreparation preparation, out string rejection))
            throw new LocalDailyMaterialFlowRejectedException("P14-C daily production could not be prepared: " + rejection);
        if (!string.Equals(configurationBefore, CaptureDailyEconomyConfiguration(), StringComparison.Ordinal)
            || !preparation.CanInstall() || !preparation.Install())
            throw new LocalDailyMaterialFlowRejectedException("P14-C daily production became stale or was rejected before installation.");

        string representativeSourceId;
        string representativeContentRevision;
        string representativeItemId;
        string representativeItemName;
        float consumptionPer1000Population;
        int requested;
        int actual;
        int closing;
        if (selectedDailyV1Definition != null)
        {
            DailyV1CityProductionConfig representative = selectedDailyV1Definition.Values.productionConfigs
                .FirstOrDefault(row => row != null && (CityProductionSourceKind)(row.sourceKind?.Value ?? 0)
                    == CityProductionSourceKind.ExogenousDaily);
            DailyV1MarketItemConfig itemConfig = selectedDailyV1Definition.Values.marketItems[0];
            if (representative == null || itemConfig == null
                || !TryResolveSelectedItemDefinition(itemConfig.item, out DailyV1ItemDefinition itemNode))
                throw new LocalDailyMaterialFlowRejectedException("Selected P14-C item configuration is not bound to its canonical definition node.");
            requested = Math.Max(0, UnityMathfCompatibility.RoundToInt(Population.CurrentPopulation / 1000f
                * itemConfig.consumptionPer1000Population));
            actual = Market.RemoveSelectedStock(itemNode, requested);
            closing = Market.GetSelectedAmount(itemNode);
            representativeSourceId = representative.productionSourceId;
            representativeContentRevision = representative.contentRevision;
            representativeItemId = itemNode.DefinitionId;
            representativeItemName = itemNode.Values.itemName;
            consumptionPer1000Population = itemConfig.consumptionPer1000Population;
        }
        else
        {
            CityProductionConfig representative = RuntimeProductionConfigs.Find(row => row != null
                && row.sourceKind == CityProductionSourceKind.ExogenousDaily);
            MarketItemConfig itemConfig = RuntimeMarketItems[0];
            requested = Math.Max(0, UnityMathfCompatibility.RoundToInt(Population.CurrentPopulation / 1000f
                * itemConfig.consumptionPer1000Population));
            actual = Market.RemoveStockUpTo(itemConfig.item, requested);
            closing = Market.GetAmount(itemConfig.item);
            representativeSourceId = representative.productionSourceId;
            representativeContentRevision = representative.contentRevision;
            representativeItemId = representative.item.DefinitionId;
            representativeItemName = representative.item.itemName;
            consumptionPer1000Population = itemConfig.consumptionPer1000Population;
        }
        long configured = 0;
        List<string> rejectedSources = new List<string>();
        foreach (CityDailyMaterialFlowSourceOutcome outcome in preparation.Outcomes)
        {
            configured += outcome.ConfiguredQuantity;
            if (!string.IsNullOrEmpty(outcome.RejectionReason)
                && outcome.RejectionReason != "ReserveExhausted")
                rejectedSources.Add(outcome.ProductionSourceId + ":" + outcome.RejectionReason);
            if (outcome.AppliedQuantity > 0)
                logger?.Log(SimulationLogCategory.EconomyProduction,
                    $"{CityName} source {outcome.ProductionSourceId}: {outcome.AppliedQuantity}/{outcome.ConfiguredQuantity}");
        }

        lastMaterialFlow = new LocalDailyMaterialFlowResult(RuntimeSettlementSemanticId,
            representativeSourceId, RuntimeMarketStoreSemanticId, RuntimeMaterialFlowLocationId,
            representativeItemId, representativeContentRevision,
            "Economy.Enabled=true;PaymentMode=Free", "simulation-calendar", calendarVersion,
            true, Population.CurrentPopulation, consumptionPer1000Population, absoluteDay,
            preparation.OpeningStock, configured, preparation.AppliedQuantity,
            string.Join(";", rejectedSources), requested, actual, closing, preparation.Outcomes);
        if (actual > 0)
            logger?.Log(SimulationLogCategory.EconomyConsumption,
                $"{CityName} consumed {actual} {representativeItemName}");
    }

    /// <summary>Creates one of the three city-owned resumable daily economy steps.</summary>
    public bool TryCreateDailyEconomyStep(
        DailyBoundaryOperation operation,
        int ordinal,
        CityDailyEconomyStepKind kind,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        if (selectedRuntimeOwners != null)
            return selectedRuntimeOwners.TryCreateDailyEconomyStep(operation, ordinal, kind,
                out step, out failure);
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
        if (selectedRuntimeOwners != null)
            return selectedRuntimeOwners.TryPrepareDailyEconomyStep(manifest, step,
                out prepared, out failure);
        prepared = null;
        failure = TimelineFailure.ContinuationFailed;
        if (!TryGetDailyEconomyIdentity(manifest, step, out string identity, out string fingerprint)) return false;
        dailyEconomyReceipts ??= new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal);
        if (dailyEconomyReceipts.TryGetValue(identity, out CityDailyEconomyReceipt previous))
        {
            if (previous.Fingerprint != fingerprint) return false;
            prepared = new CityDailyEconomyCommit(this, previous, null, 0, 0,
                null, 0, 0, 0f, null, 0, 0, 0f, null, dailyEconomyReceiptRevision, true,
                step.OwnerRevision, -1L, -1, fingerprint, null, null);
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
        FiniteSourceProductionPreparation finiteProductionPreparation = null;
        P14CMixedSourceProductionPreparation mixedProductionPreparation = null;
        IReadOnlyList<CityDailyMaterialFlowSourceOutcome> p14cSourceOutcomes =
            Array.Empty<CityDailyMaterialFlowSourceOutcome>();
        int? p14cOpeningStock = null;
        int? p14cPostProductionStock = null;
        long? p14cPostProductionMarketRevision = null;
        int? p14cActualFreeConsumption = null;
        int? p14cClosingStock = null;
        if (kind == CityDailyEconomyStepKind.Production)
        {
            if (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.ExogenousDaily
                && HasAuthoredFiniteReserve())
                return false;
            bool mixedProfile = HasConfiguredDefinition
                && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily;
            bool finiteProfile = HasConfiguredDefinition
                && (RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily
                    || finiteProductionSources != null);
            if (mixedProfile)
            {
                if (!TryPrepareMixedSourceProduction(manifest.AbsoluteDay,
                        out mixedProductionPreparation, out _)) return false;
                marketState = mixedProductionPreparation.MarketState;
                marketIncrements = mixedProductionPreparation.MarketRevisionIncrements;
                p14cSourceOutcomes = mixedProductionPreparation.Outcomes;
                p14cOpeningStock = mixedProductionPreparation.OpeningStock;
                p14cPostProductionStock = mixedProductionPreparation.PostProductionStock;
                p14cPostProductionMarketRevision = mixedProductionPreparation.PostInstallMarketRevision;
                foreach (CityDailyMaterialFlowSourceOutcome outcome in mixedProductionPreparation.Outcomes)
                    if (outcome.AppliedQuantity > 0)
                    {
                        string itemDefinitionId;
                        if (selectedDailyV1Definition != null)
                        {
                            DailyV1CityProductionConfig sourceConfig = selectedDailyV1Definition.Values.productionConfigs
                                .FirstOrDefault(row => row != null
                                    && string.Equals(row.productionSourceId, outcome.ProductionSourceId, StringComparison.Ordinal));
                            if (sourceConfig == null
                                || !TryResolveSelectedItemDefinition(sourceConfig.item, out DailyV1ItemDefinition itemNode))
                                return false;
                            itemDefinitionId = itemNode.DefinitionId;
                        }
                        else
                        {
                            CityProductionConfig sourceConfig = RuntimeProductionConfigs.Find(row => row != null
                                && string.Equals(row.productionSourceId, outcome.ProductionSourceId, StringComparison.Ordinal));
                            if (sourceConfig?.item == null) return false;
                            itemDefinitionId = sourceConfig.item.DefinitionId;
                        }
                        productionResults.Add(new CityProductionResult(RuntimeId, itemDefinitionId,
                            outcome.AppliedQuantity, Market.StockOwnerRuntimeId));
                    }
            }
            else if (finiteProfile)
            {
                if (!IsFiniteSourceConfigurationCurrent())
                    return false;

                FiniteProductionSourceState sourceState = finiteProductionSources.Source;
                string sourceId;
                string sourceContentRevision;
                string sourceItemDefinitionId;
                ItemData legacySourceItem = null;
                DailyV1ItemDefinition selectedSourceItem = null;
                if (selectedDailyV1Definition != null)
                {
                    DailyV1CityProductionConfig sourceConfig = selectedDailyV1Definition.Values.productionConfigs[0];
                    if (sourceConfig == null
                        || !TryResolveSelectedItemDefinition(sourceConfig.item, out selectedSourceItem)) return false;
                    sourceId = sourceConfig.productionSourceId;
                    sourceContentRevision = sourceConfig.contentRevision;
                    sourceItemDefinitionId = selectedSourceItem.DefinitionId;
                }
                else
                {
                    CityProductionConfig sourceConfig = RuntimeProductionConfigs[0];
                    if (sourceConfig?.item == null) return false;
                    legacySourceItem = sourceConfig.item;
                    sourceId = sourceConfig.productionSourceId;
                    sourceContentRevision = sourceConfig.contentRevision;
                    sourceItemDefinitionId = sourceConfig.item.DefinitionId;
                }
                if (sourceState == null)
                    return false;

                FiniteSourceProductionService productionService = new FiniteSourceProductionService();
                bool preparedFiniteProduction = selectedSourceItem != null
                    ? productionService.TryPrepareDaily(finiteProductionSources, Market,
                        RuntimeSettlementSemanticId, RuntimeMarketStoreSemanticId,
                        RuntimeMaterialFlowLocationId, sourceId, sourceContentRevision,
                        selectedSourceItem, manifest.AbsoluteDay, sourceState.Revision,
                        marketRevision, out finiteProductionPreparation, out _)
                    : productionService.TryPrepareDaily(finiteProductionSources, Market,
                        RuntimeSettlementSemanticId, RuntimeMarketStoreSemanticId,
                        RuntimeMaterialFlowLocationId, sourceId, sourceContentRevision,
                        legacySourceItem, manifest.AbsoluteDay, sourceState.Revision,
                        marketRevision, out finiteProductionPreparation, out _);
                if (!preparedFiniteProduction)
                    return false;

                if (finiteProductionPreparation.Quantity > 0)
                {
                    marketState = finiteProductionPreparation.MarketState;
                    marketIncrements = 1;
                    productionResults.Add(new CityProductionResult(
                        RuntimeId, sourceItemDefinitionId,
                        finiteProductionPreparation.Quantity, Market.StockOwnerRuntimeId));
                }
                else
                {
                    finiteProductionPreparation = null;
                    marketState = null;
                }
            }
            else
            {
                if (selectedDailyV1Definition != null)
                {
                    foreach (DailyV1CityProductionConfig row in selectedDailyV1Definition.Values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
                    {
                        if (row == null || string.IsNullOrWhiteSpace(row.item) || row.amountPerDay <= 0) continue;
                        if (!TryResolveSelectedItemDefinition(row.item, out DailyV1ItemDefinition item)) return false;
                        if (!Market.TryApplyPreparedStockDelta(marketState, item, row.amountPerDay, 100,
                                out PreparedMarketState replacement, out _)) continue;
                        marketState = replacement;
                        marketIncrements++;
                        productionResults.Add(new CityProductionResult(RuntimeId, item.DefinitionId,
                            row.amountPerDay, Market.StockOwnerRuntimeId));
                    }
                }
                else
                {
                    foreach (CityProductionConfig row in HasConfiguredDefinition ? RuntimeProductionConfigs ?? new List<CityProductionConfig>() : new List<CityProductionConfig>())
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
            }

        }
        else if (kind == CityDailyEconomyStepKind.Consumption)
        {
            if (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
            {
                if (!TryGetDailyEconomyReceiptForManifest(manifest, CityDailyEconomyStepKind.Production,
                        out CityDailyEconomyReceipt productionReceipt)
                    || !productionReceipt.OpeningStock.HasValue
                    || !productionReceipt.PostProductionStock.HasValue
                    || !productionReceipt.PostProductionMarketRevision.HasValue
                    || productionReceipt.BoundaryOccurrenceId != manifest.BoundaryOccurrenceId
                    || productionReceipt.AbsoluteDay != manifest.AbsoluteDay
                    || Market.Revision != productionReceipt.PostProductionMarketRevision.Value
                    || (selectedDailyV1Definition != null
                        ? !TryResolveSelectedItemDefinition(selectedDailyV1Definition.Values.marketItems[0].item,
                                out DailyV1ItemDefinition receiptItem)
                            || Market.GetSelectedAmount(receiptItem) != productionReceipt.PostProductionStock.Value
                        : Market.GetAmount(RuntimeMarketItems[0].item) != productionReceipt.PostProductionStock.Value))
                    return false;
                p14cSourceOutcomes = productionReceipt.SourceOutcomes;
                p14cOpeningStock = productionReceipt.OpeningStock;
                p14cPostProductionStock = productionReceipt.PostProductionStock;
                p14cPostProductionMarketRevision = productionReceipt.PostProductionMarketRevision;
            }
            if (!TryCreateDailyMarketConsumptionInputs(out List<DailyMarketConsumptionInput> marketInputs)) return false;
            foreach (DailyMarketConsumptionInput row in marketInputs)
            {
                if (row.ItemDefinitionId == null || row.ConsumptionPer1000Population <= 0f) continue;
                int desired = UnityMathfCompatibility.RoundToInt(preparedPopulation / 1000f * row.ConsumptionPer1000Population);
                int consumed = 0;
                float unitPrice = 0f, paid = 0f;
                MarketItemRuntime legacyItem = null;
                DailyV1MarketItemState selectedItem = null;
                if (row.SelectedItem != null)
                {
                    Market.TryGetPreparedItem(marketState, row.SelectedItem, out selectedItem);
                }
                else if (row.LegacyItem != null) legacyItem = FindPreparedItem(marketState, row.LegacyItem);
                int itemAmount = selectedItem?.Amount ?? legacyItem?.Amount ?? 0;
                float itemPrice = selectedItem?.CurrentPrice ?? legacyItem?.CurrentPrice ?? 0f;
                if (desired > 0 && itemAmount > 0)
                {
                    if (PopulationEconomy.PaymentMode == ConsumptionPaymentMode.Free)
                    {
                        consumed = Math.Min(desired, itemAmount);
                    }
                    else if (populationAccount != null && settlementAccount != null)
                    {
                        unitPrice = UnityMathfCompatibility.Max(0.01f, itemPrice);
                        if (float.IsNaN(unitPrice) || float.IsInfinity(unitPrice)) unitPrice = 0f;
                        if (unitPrice <= 0f)
                        {
                            consumptionResults.Add(new CityConsumptionResult(RuntimeId,
                                PopulationEconomy.PopulationEconomicRuntimeId, row.ItemDefinitionId,
                                desired, 0, PopulationEconomy.PaymentMode, 0f, 0f));
                            continue;
                        }
                        float affordableRaw = populationBalance / unitPrice;
                        int affordable = populationBalance >= unitPrice
                            ? (float.IsInfinity(affordableRaw) || affordableRaw >= int.MaxValue
                                ? int.MaxValue : UnityMathfCompatibility.Max(0, UnityMathfCompatibility.FloorToInt(affordableRaw))) : 0;
                        consumed = Math.Min(desired, Math.Min(itemAmount, affordable));
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
                        if (row.SelectedItem != null)
                        {
                            if (!Market.TryApplyPreparedStockDelta(marketState, row.SelectedItem, -consumed,
                                    selectedItem.DesiredAmount, out PreparedMarketState replacement,
                                    out selectedItem)) return false;
                            marketState = replacement;
                        }
                        else
                        {
                            if (legacyItem == null || !legacyItem.RemoveAmount(consumed)) return false;
                            legacyItem.UpdatePrice();
                        }
                        marketIncrements++;
                    }
                }
                consumptionResults.Add(new CityConsumptionResult(RuntimeId,
                    PopulationEconomy.PopulationEconomicRuntimeId, row.ItemDefinitionId,
                    desired, consumed, PopulationEconomy.PaymentMode, unitPrice, paid));
            }
            if (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
            {
                if (PopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free) return false;
                int actualFree = 0;
                foreach (CityConsumptionResult result in consumptionResults)
                    actualFree = checked(actualFree + result.ConsumedQuantity);
                DailyV1MarketItemState selectedPreparedItem = null;
                MarketItemRuntime legacyPreparedItem = null;
                if (selectedDailyV1Definition != null)
                {
                    if (!TryResolveSelectedItemDefinition(selectedDailyV1Definition.Values.marketItems[0].item,
                            out DailyV1ItemDefinition item)) return false;
                    Market.TryGetPreparedItem(marketState, item, out selectedPreparedItem);
                }
                else legacyPreparedItem = FindPreparedItem(marketState, RuntimeMarketItems[0].item);
                p14cActualFreeConsumption = actualFree;
                p14cClosingStock = selectedPreparedItem?.Amount ?? legacyPreparedItem?.Amount ?? 0;
            }
        }
        else
        {
            if (selectedDailyV1Definition != null)
            {
                if (!Market.TryPreparePriceRefresh(marketState, out PreparedMarketState replacement,
                        out bool changed)) return false;
                marketState = replacement;
                if (changed) marketIncrements++;
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
        }

        bool mixedProfileOperation = HasConfiguredDefinition
            && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily;
        bool requiresDirectMarketMutationAdmission = marketIncrements > 0
            && mixedProductionPreparation == null
            && finiteProductionPreparation == null
            && (mixedProfileOperation || selectedDailyV1Definition != null);
        if (!Market.CanInstall(marketRevision, marketIncrements)
            || (mixedProductionPreparation != null && !mixedProductionPreparation.CanInstall())
            || (requiresDirectMarketMutationAdmission
                && !Market.CanInstallMaterialFlow(marketRevision, marketIncrements))
            || (populationAccount != null && !MoneyAccountOwnerBridge.CanInstall(populationAccount, populationRevision, populationIncrements, populationBalance))
            || (settlementAccount != null && !MoneyAccountOwnerBridge.CanInstall(settlementAccount, settlementRevision, settlementIncrements, settlementBalance))) return false;
        CityDailyEconomyReceipt receipt = new CityDailyEconomyReceipt(identity, fingerprint,
            productionResults.AsReadOnly(), consumptionResults.AsReadOnly(), manifest.BoundaryOccurrenceId,
            manifest.AbsoluteDay, p14cSourceOutcomes, p14cOpeningStock, p14cPostProductionStock,
            p14cPostProductionMarketRevision,
            p14cActualFreeConsumption, p14cClosingStock);
        Dictionary<string, CityDailyEconomyReceipt> nextReceipts =
            new Dictionary<string, CityDailyEconomyReceipt>(dailyEconomyReceipts, StringComparer.Ordinal) { [identity] = receipt };
        prepared = new CityDailyEconomyCommit(this, receipt, marketState, marketRevision, marketIncrements,
            populationAccount, populationRevision, populationIncrements, populationBalance,
            settlementAccount, settlementRevision, settlementIncrements, settlementBalance,
            nextReceipts, dailyEconomyReceiptRevision, false, step.OwnerRevision,
            preparedPopulationRevision, preparedPopulation, fingerprint,
            finiteProductionPreparation, mixedProductionPreparation);
        failure = TimelineFailure.None;
        return true;
    }

    private void SimulateSelectedLocalDailyMaterialFlow(long absoluteDay, string calendarVersion)
    {
        if (selectedRuntimeOwners == null)
            throw new LocalDailyMaterialFlowRejectedException("Selected City runtime owners are not installed.");
        lastMaterialFlow = selectedRuntimeOwners.SimulateSelectedLocalDailyMaterialFlow(
            RuntimeMaterialFlowLocationId, absoluteDay, calendarVersion,
            (category, message) => logger?.Log(category, message));
        selectedRuntimeOwners.RecordSelectedMaterialFlow(lastMaterialFlow);
    }

    public bool TryResolveDailyEconomyReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out CityDailyEconomyReceipt receipt,
        out TimelineFailure failure)
    {
        if (selectedRuntimeOwners != null)
            return selectedRuntimeOwners.TryResolveDailyEconomyReceipt(manifest, step,
                out receipt, out failure);
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

    private bool TryGetDailyEconomyReceiptForManifest(
        BoundaryContinuationManifest manifest,
        CityDailyEconomyStepKind kind,
        out CityDailyEconomyReceipt receipt)
    {
        if (selectedRuntimeOwners != null)
            return selectedRuntimeOwners.TryGetDailyEconomyReceiptForManifest(manifest, kind, out receipt);
        receipt = null;
        if (manifest?.Steps == null || dailyEconomyReceipts == null) return false;
        string stepId = GetDailyEconomyStepId(kind);
        foreach (BoundaryContinuationStep candidate in manifest.Steps)
        {
            if (candidate == null || !string.Equals(candidate.StepId, stepId, StringComparison.Ordinal)) continue;
            if (!TryGetDailyEconomyIdentity(manifest, candidate, out string identity, out string fingerprint)
                || !dailyEconomyReceipts.TryGetValue(identity, out CityDailyEconomyReceipt existing)
                || !string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)) return false;
            receipt = existing;
            return true;
        }
        return false;
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
            || (commit.MixedSource != null
                ? (!ReferenceEquals(commit.MarketState, commit.MixedSource.MarketState)
                    || !commit.MixedSource.CanInstall())
                : commit.FiniteSource == null
                    ? (!Market.CanInstall(commit.MarketRevision, commit.MarketIncrements)
                        || (commit.MarketIncrements > 0
                            && (selectedDailyV1Definition != null
                                || (HasConfiguredDefinition
                                    && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily))
                            && !Market.CanInstallMaterialFlow(commit.MarketRevision, commit.MarketIncrements)))
                    : (!ReferenceEquals(commit.MarketState, commit.FiniteSource.MarketState)
                        || !commit.FiniteSource.CanInstall()))
            || (commit.PopulationAccount != null && !MoneyAccountOwnerBridge.CanInstall(commit.PopulationAccount, commit.PopulationRevision, commit.PopulationIncrements, commit.PopulationBalance))
            || (commit.SettlementAccount != null && !MoneyAccountOwnerBridge.CanInstall(commit.SettlementAccount, commit.SettlementRevision, commit.SettlementIncrements, commit.SettlementBalance))) return false;
        if (commit.MixedSource != null)
        {
            if (!commit.MixedSource.Install()) return false;
        }
        else if (commit.FiniteSource != null)
        {
            if (!commit.FiniteSource.Install()) return false;
        }
        else if (commit.MarketState != null && commit.MarketIncrements > 0)
        {
            Market.InstallPrepared(commit.MarketRevision, commit.MarketIncrements, commit.MarketState);
            if (selectedDailyV1Definition != null
                || (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily))
                Market.NotifyMaterialFlowInstalled();
        }
        if (commit.PopulationAccount != null) MoneyAccountOwnerBridge.InstallPrepared(commit.PopulationAccount, commit.PopulationRevision, commit.PopulationIncrements, commit.PopulationBalance);
        if (commit.SettlementAccount != null) MoneyAccountOwnerBridge.InstallPrepared(commit.SettlementAccount, commit.SettlementRevision, commit.SettlementIncrements, commit.SettlementBalance);
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
        MarketLiquidityMode liquidityMode = RuntimeMarketLiquidityMode;
        float liquidityPurchasingPower = HasConfiguredDefinition ? RuntimeMarketLiquidityPurchasingPower : 0f;
        ConsumptionPaymentMode populationPaymentMode = RuntimePopulationPaymentMode;
        float populationPurchasingPower = HasConfiguredDefinition ? RuntimePopulationPurchasingPower : 0f;
        List<string> parts = new List<string> { RuntimeId, DefinitionId,
            HasConfiguredDefinition ? RuntimeMaterialFlowProfile.ToString() : LocalMaterialFlowProfile.ExogenousDaily.ToString(),
            HasConfiguredDefinition ? RuntimeSettlementSemanticId ?? string.Empty : string.Empty,
            HasConfiguredDefinition ? RuntimeMaterialFlowLocationId ?? string.Empty : string.Empty,
            HasConfiguredDefinition ? RuntimeMarketStoreSemanticId ?? string.Empty : string.Empty,
            HasConfiguredDefinition ? RuntimeInitialPopulation.ToString(CultureInfo.InvariantCulture) : "0",
            PopulationEconomy.PaymentMode.ToString(),
            liquidityMode.ToString(),
            liquidityPurchasingPower.ToString("R", CultureInfo.InvariantCulture),
            populationPaymentMode.ToString(),
            populationPurchasingPower.ToString("R", CultureInfo.InvariantCulture) };
        if (selectedDailyV1Definition != null)
        {
            DailyV1CityData values = selectedDailyV1Definition.Values;
            List<DailyV1CityProductionConfig> selectedSources = new List<DailyV1CityProductionConfig>(
                values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>());
            if (RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
                selectedSources.Sort((left, right) => StringComparer.Ordinal.Compare(
                    left?.productionSourceId ?? string.Empty, right?.productionSourceId ?? string.Empty));
            foreach (DailyV1CityProductionConfig row in selectedSources)
            {
                parts.Add("p"); AddSelectedItemIdentity(parts, row?.item);
                if (RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
                    parts.Add(row != null
                        ? ((CityProductionSourceKind)(row.sourceKind?.Value ?? 0)).ToString()
                        : "null-kind");
                parts.Add(row != null ? row.amountPerDay.ToString(CultureInfo.InvariantCulture) : "null");
                parts.Add(row != null ? row.initialReserve.ToString(CultureInfo.InvariantCulture) : "null");
                parts.Add(row != null ? row.productionSourceId ?? string.Empty : "null");
                parts.Add(row != null ? row.contentRevision ?? string.Empty : "null");
            }
            foreach (DailyV1MarketItemConfig row in values.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
            {
                parts.Add("c"); AddSelectedItemIdentity(parts, row?.item);
                parts.Add(row != null ? row.initialAmount.ToString(CultureInfo.InvariantCulture) : "null");
                parts.Add(row != null ? row.desiredAmount.ToString(CultureInfo.InvariantCulture) : "null");
                parts.Add(row != null ? row.consumptionPer1000Population.ToString("R", CultureInfo.InvariantCulture) : "null");
            }
            return SpatialStableKey.Encode(parts.ToArray());
        }
        IEnumerable<CityProductionConfig> sourceRows = HasConfiguredDefinition
            ? RuntimeProductionConfigs ?? new List<CityProductionConfig>()
            : new List<CityProductionConfig>();
        if (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
        {
            List<CityProductionConfig> orderedSources = new List<CityProductionConfig>(sourceRows);
            orderedSources.Sort((left, right) => StringComparer.Ordinal.Compare(
                left?.productionSourceId ?? string.Empty, right?.productionSourceId ?? string.Empty));
            sourceRows = orderedSources;
        }
        foreach (CityProductionConfig row in sourceRows)
        {
            parts.Add("p"); AddItemIdentity(parts, row?.item);
            if (HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily)
                parts.Add(row != null ? row.sourceKind.ToString() : "null-kind");
            parts.Add(row != null ? row.amountPerDay.ToString(CultureInfo.InvariantCulture) : "null");
            parts.Add(row != null ? row.initialReserve.ToString(CultureInfo.InvariantCulture) : "null");
            parts.Add(row != null ? row.productionSourceId ?? string.Empty : "null");
            parts.Add(row != null ? row.contentRevision ?? string.Empty : "null");
        }
        foreach (MarketItemConfig row in HasConfiguredDefinition ? RuntimeMarketItems ?? new List<MarketItemConfig>() : new List<MarketItemConfig>())
        { parts.Add("c"); AddItemIdentity(parts, row?.item); parts.Add(row != null ? row.initialAmount.ToString(CultureInfo.InvariantCulture) : "null"); parts.Add(row != null ? row.desiredAmount.ToString(CultureInfo.InvariantCulture) : "null"); parts.Add(row != null ? row.consumptionPer1000Population.ToString("R", CultureInfo.InvariantCulture) : "null"); }
        return SpatialStableKey.Encode(parts.ToArray());
    }

    private bool IsFiniteSourceConfigurationCurrent()
    {
        if (selectedDailyV1Definition != null)
            return IsSelectedFiniteSourceConfigurationCurrent();
        bool mixed = HasConfiguredDefinition && RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily;
        if (!HasConfiguredDefinition || (!mixed && RuntimeMaterialFlowProfile != LocalMaterialFlowProfile.FiniteReserveDaily)
            || finiteProductionSources == null || RuntimeProductionConfigs == null
            || (!mixed && RuntimeProductionConfigs.Count != 1)
            || (mixed && !(selectedDailyV1Definition != null
                ? FiniteSourceProfileAdmission.TryValidateMixedSourceCity(selectedDailyV1Definition.Values, out _)
                : FiniteSourceProfileAdmission.TryValidateMixedSourceCity(cityData, out _)))) return false;
        CityProductionConfig configuredSource = mixed
            ? RuntimeProductionConfigs.Find(row => row != null
                && row.sourceKind == CityProductionSourceKind.FiniteReserveDaily)
            : RuntimeProductionConfigs[0];
        FiniteProductionSourceState source = finiteProductionSources.Source;
        return configuredSource?.item != null
            && (!mixed || configuredSource.sourceKind == CityProductionSourceKind.FiniteReserveDaily)
            && source != null
            && configuredSource.amountPerDay == source.DailyOutputLimit
            && configuredSource.initialReserve == source.InitialReserve
            && string.Equals(configuredSource.productionSourceId, source.ProductionSourceId, StringComparison.Ordinal)
            && string.Equals(configuredSource.contentRevision, source.ContentRevision, StringComparison.Ordinal)
            && string.Equals(configuredSource.item.DefinitionId, source.ItemDefinitionId, StringComparison.Ordinal)
            && string.Equals(RuntimeSettlementSemanticId, source.SettlementSemanticId, StringComparison.Ordinal)
            && string.Equals(RuntimeMarketStoreSemanticId, source.MarketStoreSemanticId, StringComparison.Ordinal);
    }

    private bool IsSelectedFiniteSourceConfigurationCurrent()
    {
        DailyV1CityData city = selectedDailyV1Definition?.Values;
        bool mixed = RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily;
        if (city?.productionConfigs == null || finiteProductionSources == null
            || (!mixed && RuntimeMaterialFlowProfile != LocalMaterialFlowProfile.FiniteReserveDaily)
            || (!mixed && city.productionConfigs.Count != 1)
            || (mixed && !FiniteSourceProfileAdmission.TryValidateMixedSourceCity(city, out _))) return false;

        DailyV1CityProductionConfig configuredSource = null;
        if (mixed)
        {
            foreach (DailyV1CityProductionConfig row in city.productionConfigs)
                if (row != null && (CityProductionSourceKind)(row.sourceKind?.Value ?? 0)
                        == CityProductionSourceKind.FiniteReserveDaily)
                { configuredSource = row; break; }
        }
        else configuredSource = city.productionConfigs[0];

        FiniteProductionSourceState source = finiteProductionSources.Source;
        return configuredSource != null
            && (!mixed || (CityProductionSourceKind)(configuredSource.sourceKind?.Value ?? 0)
                == CityProductionSourceKind.FiniteReserveDaily)
            && TryResolveSelectedItemDefinition(configuredSource.item, out DailyV1ItemDefinition item)
            && source != null
            && configuredSource.amountPerDay == source.DailyOutputLimit
            && configuredSource.initialReserve == source.InitialReserve
            && string.Equals(configuredSource.productionSourceId, source.ProductionSourceId, StringComparison.Ordinal)
            && string.Equals(configuredSource.contentRevision, source.ContentRevision, StringComparison.Ordinal)
            && string.Equals(item.DefinitionId, source.ItemDefinitionId, StringComparison.Ordinal)
            && string.Equals(RuntimeSettlementSemanticId, source.SettlementSemanticId, StringComparison.Ordinal)
            && string.Equals(RuntimeMarketStoreSemanticId, source.MarketStoreSemanticId, StringComparison.Ordinal);
    }

    private bool TryPrepareMixedSourceProduction(
        long absoluteDay,
        out P14CMixedSourceProductionPreparation preparation,
        out string rejectionReason)
    {
        if (selectedDailyV1Definition != null)
            return TryPrepareSelectedMixedSourceProduction(absoluteDay, out preparation, out rejectionReason);

        preparation = null;
        rejectionReason = "InvalidMixedSourceProfile";
        bool validProfile = selectedDailyV1Definition != null
            ? FiniteSourceProfileAdmission.TryValidateMixedSourceCity(
                selectedDailyV1Definition.Values, out rejectionReason)
            : FiniteSourceProfileAdmission.TryValidateMixedSourceCity(cityData, out rejectionReason);
        if (!validProfile) return false;
        if (!IsFiniteSourceConfigurationCurrent())
        { rejectionReason = "FiniteSourceOwnerMismatch"; return false; }

        CityProductionConfig finiteConfig = RuntimeProductionConfigs.Find(row => row != null
            && row.sourceKind == CityProductionSourceKind.FiniteReserveDaily);
        FiniteProductionSourceState finiteState = finiteProductionSources?.Source;
        if (finiteConfig == null || finiteState == null)
        { rejectionReason = "FiniteSourceOwnerMissing"; return false; }
        if (finiteState.RemainingReserve < 0 || finiteState.RemainingReserve > finiteState.InitialReserve
            || finiteState.DailyOutputLimit <= 0)
        { rejectionReason = "InvalidFiniteSourceState"; return false; }

        long marketRevision = Market.Revision;
        PreparedMarketState marketState = Market.CreatePreparedSnapshot();
        string itemDefinitionId = finiteConfig.item.DefinitionId;
        List<MarketItemRuntime> matchingItems = marketState.Items.FindAll(row => row?.Item != null
            && string.Equals(row.Item.DefinitionId, itemDefinitionId, StringComparison.Ordinal));
        if (matchingItems.Count != 1)
        { rejectionReason = "MarketRowCardinality"; return false; }

        MarketItemRuntime preparedItem = matchingItems[0];
        int openingStock = preparedItem.Amount;
        long marketIncrements = 0;
        long totalApplied = 0;
        int finiteApplied = 0;
        List<CityDailyMaterialFlowSourceOutcome> outcomes = new List<CityDailyMaterialFlowSourceOutcome>(2);
        List<CityProductionConfig> ordered = new List<CityProductionConfig>(RuntimeProductionConfigs);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.productionSourceId ?? string.Empty, right?.productionSourceId ?? string.Empty));

        foreach (CityProductionConfig source in ordered)
        {
            int planned = source.sourceKind == CityProductionSourceKind.FiniteReserveDaily
                ? Math.Min(source.amountPerDay, finiteState.RemainingReserve)
                : source.amountPerDay;
            string sourceRejection = planned == 0 ? "ReserveExhausted" : string.Empty;
            int applied = 0;
            if (planned > 0)
            {
                if (preparedItem.Amount > int.MaxValue - planned)
                    sourceRejection = "AggregateStockOverflow";
                else if (!preparedItem.AddAmount(planned))
                    sourceRejection = "AggregateStockOverflow";
                else
                {
                    preparedItem.UpdatePrice();
                    applied = planned;
                    marketIncrements++;
                    totalApplied += applied;
                    if (source.sourceKind == CityProductionSourceKind.FiniteReserveDaily)
                        finiteApplied = applied;
                }
            }

            outcomes.Add(new CityDailyMaterialFlowSourceOutcome(
                source.productionSourceId, source.sourceKind, source.amountPerDay,
                planned, applied, sourceRejection));
        }

        if (marketIncrements > 0 && !Market.CanInstallMaterialFlow(marketRevision, marketIncrements))
        { rejectionReason = Market.Revision == marketRevision ? "MarketMutationRejected" : "StaleMarketRevision"; return false; }
        if (finiteApplied > 0 && !finiteProductionSources.CanInstall(finiteState.Revision, absoluteDay))
        {
            rejectionReason = finiteState.Revision == long.MaxValue ? "SourceRevisionExhausted"
                : absoluteDay <= finiteState.LastProcessedDay ? "BoundaryAlreadyProcessed" : "StaleSourceRevision";
            return false;
        }

        preparation = new P14CMixedSourceProductionPreparation(
            Market, finiteProductionSources, finiteState.Revision, absoluteDay, finiteApplied,
            marketState, marketRevision, marketIncrements, openingStock, preparedItem.Amount,
            checked((int)totalApplied), outcomes.AsReadOnly());
        rejectionReason = string.Empty;
        return true;
    }

    private bool TryPrepareSelectedMixedSourceProduction(
        long absoluteDay,
        out P14CMixedSourceProductionPreparation preparation,
        out string rejectionReason)
    {
        preparation = null;
        rejectionReason = "InvalidMixedSourceProfile";
        DailyV1CityData city = selectedDailyV1Definition?.Values;
        if (city == null || selectedDefinitionGraph == null
            || !FiniteSourceProfileAdmission.TryValidateMixedSourceCity(city, out rejectionReason)) return false;
        if (!IsFiniteSourceConfigurationCurrent())
        { rejectionReason = "FiniteSourceOwnerMismatch"; return false; }

        DailyV1CityProductionConfig finiteConfig = null;
        foreach (DailyV1CityProductionConfig source in city.productionConfigs)
            if (source != null && (CityProductionSourceKind)(source.sourceKind?.Value ?? 0)
                    == CityProductionSourceKind.FiniteReserveDaily)
            { finiteConfig = source; break; }
        FiniteProductionSourceState finiteState = finiteProductionSources?.Source;
        if (finiteConfig == null || finiteState == null
            || !TryResolveSelectedItemDefinition(finiteConfig.item, out DailyV1ItemDefinition finiteItem))
        { rejectionReason = "FiniteSourceOwnerMissing"; return false; }
        if (finiteState.RemainingReserve < 0 || finiteState.RemainingReserve > finiteState.InitialReserve
            || finiteState.DailyOutputLimit <= 0)
        { rejectionReason = "InvalidFiniteSourceState"; return false; }

        long marketRevision = Market.Revision;
        PreparedMarketState marketState = Market.CreatePreparedSnapshot();
        if (!Market.TryCountPreparedItems(marketState, finiteItem, out int matchingCount)
            || matchingCount != 1
            || !Market.TryGetPreparedItem(marketState, finiteItem, out DailyV1MarketItemState preparedItem)
            || preparedItem == null)
        { rejectionReason = "MarketRowCardinality"; return false; }

        int openingStock = preparedItem.Amount;
        long marketIncrements = 0;
        long totalApplied = 0;
        int finiteApplied = 0;
        List<CityDailyMaterialFlowSourceOutcome> outcomes = new List<CityDailyMaterialFlowSourceOutcome>(city.productionConfigs.Count);
        List<DailyV1CityProductionConfig> ordered = new List<DailyV1CityProductionConfig>(city.productionConfigs);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.productionSourceId ?? string.Empty, right?.productionSourceId ?? string.Empty));

        foreach (DailyV1CityProductionConfig source in ordered)
        {
            CityProductionSourceKind sourceKind = (CityProductionSourceKind)(source?.sourceKind?.Value ?? 0);
            int planned = sourceKind == CityProductionSourceKind.FiniteReserveDaily
                ? Math.Min(source.amountPerDay, finiteState.RemainingReserve)
                : source.amountPerDay;
            string sourceRejection = planned == 0 ? "ReserveExhausted" : string.Empty;
            int applied = 0;
            if (planned > 0)
            {
                if (!Market.TryApplyPreparedStockDelta(marketState, finiteItem, planned,
                        preparedItem.DesiredAmount, out PreparedMarketState updatedState,
                        out DailyV1MarketItemState updatedItem))
                    sourceRejection = "AggregateStockOverflow";
                else
                {
                    marketState = updatedState;
                    preparedItem = updatedItem;
                    applied = planned;
                    marketIncrements++;
                    totalApplied += applied;
                    if (sourceKind == CityProductionSourceKind.FiniteReserveDaily)
                        finiteApplied = applied;
                }
            }

            outcomes.Add(new CityDailyMaterialFlowSourceOutcome(
                source.productionSourceId, sourceKind, source.amountPerDay,
                planned, applied, sourceRejection));
        }

        if (marketIncrements > 0 && !Market.CanInstallMaterialFlow(marketRevision, marketIncrements))
        { rejectionReason = Market.Revision == marketRevision ? "MarketMutationRejected" : "StaleMarketRevision"; return false; }
        if (finiteApplied > 0 && !finiteProductionSources.CanInstall(finiteState.Revision, absoluteDay))
        {
            rejectionReason = finiteState.Revision == long.MaxValue ? "SourceRevisionExhausted"
                : absoluteDay <= finiteState.LastProcessedDay ? "BoundaryAlreadyProcessed" : "StaleSourceRevision";
            return false;
        }

        preparation = new P14CMixedSourceProductionPreparation(
            Market, finiteProductionSources, finiteState.Revision, absoluteDay, finiteApplied,
            marketState, marketRevision, marketIncrements, openingStock, preparedItem.Amount,
            checked((int)totalApplied), outcomes.AsReadOnly());
        rejectionReason = string.Empty;
        return true;
    }

    private bool HasAuthoredFiniteReserve() => HasConfiguredDefinition
        && (selectedDailyV1Definition != null
            ? (selectedDailyV1Definition.Values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
                .Any(row => row != null && row.initialReserve != 0)
            : RuntimeProductionConfigs != null
                && RuntimeProductionConfigs.Exists(row => row != null && row.initialReserve != 0));

    private bool TryResolveSelectedItemDefinition(string definitionId, out DailyV1ItemDefinition item)
    {
        item = null;
        return selectedDefinitionGraph != null && !string.IsNullOrWhiteSpace(definitionId)
            && selectedDefinitionGraph.Items.TryGetValue(definitionId, out item)
            && item != null;
    }

    private bool TryCreateDailyMarketConsumptionInputs(out List<DailyMarketConsumptionInput> inputs)
    {
        inputs = new List<DailyMarketConsumptionInput>();
        if (selectedDailyV1Definition != null)
        {
            foreach (DailyV1MarketItemConfig row in selectedDailyV1Definition.Values.marketItems
                ?? Array.Empty<DailyV1MarketItemConfig>())
            {
                if (row == null)
                {
                    inputs.Add(new DailyMarketConsumptionInput(null, null, null, 0f));
                    continue;
                }
                DailyV1ItemDefinition item = null;
                if (!string.IsNullOrWhiteSpace(row.item)
                    && !TryResolveSelectedItemDefinition(row.item, out item)) return false;
                inputs.Add(new DailyMarketConsumptionInput(item, null,
                    item?.DefinitionId, row.consumptionPer1000Population));
            }
            return true;
        }

        foreach (MarketItemConfig row in HasConfiguredDefinition
            ? RuntimeMarketItems ?? new List<MarketItemConfig>()
            : new List<MarketItemConfig>())
            inputs.Add(new DailyMarketConsumptionInput(null, row?.item,
                row?.item?.DefinitionId, row != null ? row.consumptionPer1000Population : 0f));
        return true;
    }

    private sealed class DailyMarketConsumptionInput
    {
        internal readonly DailyV1ItemDefinition SelectedItem;
        internal readonly ItemData LegacyItem;
        internal readonly string ItemDefinitionId;
        internal readonly float ConsumptionPer1000Population;

        internal DailyMarketConsumptionInput(DailyV1ItemDefinition selectedItem, ItemData legacyItem,
            string itemDefinitionId, float consumptionPer1000Population)
        {
            SelectedItem = selectedItem;
            LegacyItem = legacyItem;
            ItemDefinitionId = itemDefinitionId;
            ConsumptionPer1000Population = consumptionPer1000Population;
        }
    }

    private static void AddItemIdentity(List<string> parts, ItemData item)
    { parts.Add(item != null ? item.DefinitionId : ""); parts.Add(item != null ? item.itemName : ""); parts.Add(item != null ? item.basePrice.ToString("R", CultureInfo.InvariantCulture) : ""); }
    private void AddSelectedItemIdentity(List<string> parts, string definitionId)
    {
        parts.Add(definitionId ?? string.Empty);
        if (TryResolveSelectedItemDefinition(definitionId, out DailyV1ItemDefinition item))
        {
            parts.Add(item.Values.itemName ?? string.Empty);
            parts.Add(item.Values.basePrice.ToString("R", CultureInfo.InvariantCulture));
        }
        else
        {
            parts.Add(string.Empty);
            parts.Add(string.Empty);
        }
    }
    private string GetDailyEconomyStepId(CityDailyEconomyStepKind kind) => "city-economy-" + kind.ToString().ToLowerInvariant() + ":" + RuntimeId;
    private CityDailyEconomyStepKind GetDailyEconomyStepKind(string id) => id != null && id.StartsWith("city-economy-production:", StringComparison.Ordinal) ? CityDailyEconomyStepKind.Production : id != null && id.StartsWith("city-economy-consumption:", StringComparison.Ordinal) ? CityDailyEconomyStepKind.Consumption : CityDailyEconomyStepKind.PriceRefresh;
    private static MarketItemRuntime FindPreparedItem(PreparedMarketState state, ItemData item) => state.Items.Find(x => x != null && x.Item == item);

    private CityRuntime()
    {
    }

    internal static bool TryCreateFromOwnerSnapshot(
        string snapshotRuntimeId,
        CityData snapshotCityData,
        SpatialLocationRuntime snapshotLocation,
        MarketCounterpartyRuntime snapshotCounterparty,
        MarketRuntime snapshotMarket,
        PopulationEconomyRuntime snapshotPopulationEconomy,
        SettlementPopulationRuntime snapshotPopulation,
        long snapshotImportantNpcRevision,
        IReadOnlyList<string> orderedNpcRuntimeIds,
        P12DCityRootOwnerSnapshot.StagingCaptureEnvelope captureEnvelope,
        out CityRuntime city,
        out P12DCityMembershipLinker membershipLinker,
        DailyV1CityDefinition selectedDefinition = null,
        DailyV1EffectiveDefinitions selectedDefinitions = null)
    {
        city = null;
        membershipLinker = null;
        if (string.IsNullOrWhiteSpace(snapshotRuntimeId)
            || ((selectedDefinition == null) != (selectedDefinitions == null))
            || (selectedDefinition == null
                ? snapshotCityData == null || string.IsNullOrWhiteSpace(snapshotCityData.DefinitionId)
                : selectedDefinitions == null
                    || !selectedDefinitions.Cities.TryGetValue(selectedDefinition.DefinitionId, out DailyV1CityDefinition admitted)
                    || !ReferenceEquals(admitted, selectedDefinition)
                    || !AreSelectedDefinitionInputsAdmitted(selectedDefinition, selectedDefinitions))
            || snapshotLocation == null
            || string.IsNullOrWhiteSpace(snapshotLocation.RuntimeId)
            || snapshotCounterparty == null
            || snapshotCounterparty.LiquidityMode != MarketLiquidityMode.Open
            || snapshotCounterparty.MoneyAccount != null
            || !string.Equals(snapshotCounterparty.CounterpartyRuntimeId, snapshotRuntimeId, StringComparison.Ordinal)
            || snapshotMarket == null
            || !ReferenceEquals(snapshotMarket.InstalledCounterparty, snapshotCounterparty)
            || snapshotPopulationEconomy == null
            || !string.Equals(snapshotPopulationEconomy.CityRuntimeId, snapshotRuntimeId, StringComparison.Ordinal)
            || snapshotPopulationEconomy.PaymentMode != ConsumptionPaymentMode.Free
            || snapshotPopulationEconomy.MoneyAccount != null
            || !string.Equals(
                snapshotPopulationEconomy.PopulationEconomicRuntimeId,
                "population-" + snapshotRuntimeId,
                StringComparison.Ordinal)
            || (selectedDefinition != null
                && (snapshotMarket.SelectedDailyV1Owner == null
                    || !ReferenceEquals(snapshotMarket.SelectedDailyV1Owner.Definitions, selectedDefinitions)
                    || !ReferenceEquals(snapshotMarket.SelectedDailyV1Owner.Counterparty, snapshotCounterparty)))
            || snapshotPopulation == null
            || !string.Equals(snapshotPopulation.SettlementRuntimeId, snapshotRuntimeId, StringComparison.Ordinal)
            || snapshotImportantNpcRevision < 0L
            || orderedNpcRuntimeIds == null)
        {
            return false;
        }

        HashSet<string> uniqueNpcRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < orderedNpcRuntimeIds.Count; i++)
        {
            string npcRuntimeId = orderedNpcRuntimeIds[i];
            if (string.IsNullOrWhiteSpace(npcRuntimeId) || !uniqueNpcRuntimeIds.Add(npcRuntimeId))
            {
                return false;
            }
        }

        CityRuntime staged = new CityRuntime
        {
            runtimeId = snapshotRuntimeId,
            cityData = selectedDefinition == null ? snapshotCityData : null,
            selectedDailyV1Definition = selectedDefinition,
            selectedDefinitionGraph = selectedDefinitions,
            selectedRuntimeOwners = null,
            location = snapshotLocation,
            marketCounterparty = snapshotCounterparty,
            market = snapshotMarket,
            populationEconomy = snapshotPopulationEconomy,
            population = snapshotPopulation,
            importantNpcs = new List<NpcRuntime>(orderedNpcRuntimeIds.Count),
            importantNpcRevision = snapshotImportantNpcRevision,
            dailyEconomyReceipts = new Dictionary<string, CityDailyEconomyReceipt>(StringComparer.Ordinal),
            dailyEconomyReceiptRevision = 0L,
            lastMaterialFlow = null,
            finiteProductionSources = null
        };
        staged.readOnlyImportantNpcs = staged.importantNpcs.AsReadOnly();

        if (staged.HasLocalDailyMaterialFlow
            || staged.FiniteProductionSources != null
            || staged.LastMaterialFlow != null)
        {
            return false;
        }

        city = staged;
        membershipLinker = new P12DCityMembershipLinker(
            staged,
            staged.importantNpcs,
            orderedNpcRuntimeIds,
            snapshotImportantNpcRevision,
            captureEnvelope);
        return true;
    }

    public CityRuntime(
        string runtimeId,
        CityData cityData,
        SpatialLocationRuntime location,
        SimulationLogger logger = null,
        MarketCounterpartyRuntime marketCounterparty = null)
        : this(runtimeId, cityData, location, logger, marketCounterparty, null)
    {
    }

    internal static CityRuntime CreateSelectedDailyV1(
        string runtimeId,
        CityData cityData,
        DailyV1CityDefinition selectedDefinition,
        DailyV1EffectiveDefinitions selectedDefinitions,
        DailyV1DefinitionAdapter.SelectedBindings selectedBindings,
        SpatialLocationRuntime location,
        SimulationLogger logger = null,
        MarketCounterpartyRuntime marketCounterparty = null)
    {
        if (selectedDefinition == null)
            throw new ArgumentNullException(nameof(selectedDefinition));
        if (selectedDefinitions == null)
            throw new ArgumentNullException(nameof(selectedDefinitions));
        if (selectedBindings == null)
            throw new ArgumentNullException(nameof(selectedBindings));
        if (cityData == null
            || !selectedBindings.TryGet(cityData, out DailyV1CityDefinition assetNode)
            || !ReferenceEquals(assetNode, selectedDefinition))
            throw new ArgumentException("Selected City compatibility identity must resolve to the exact admitted graph node.", nameof(cityData));
        return CreateSelectedDailyV1(runtimeId, selectedDefinition, selectedDefinitions, selectedBindings,
            location, logger, marketCounterparty);
    }

    internal static CityRuntime CreateSelectedDailyV1(
        string runtimeId,
        DailyV1CityDefinition selectedDefinition,
        DailyV1EffectiveDefinitions selectedDefinitions,
        DailyV1DefinitionAdapter.SelectedBindings selectedBindings,
        SpatialLocationRuntime location,
        SimulationLogger logger = null,
        MarketCounterpartyRuntime marketCounterparty = null)
    {
        if (selectedDefinition == null)
            throw new ArgumentNullException(nameof(selectedDefinition));
        if (selectedDefinitions == null)
            throw new ArgumentNullException(nameof(selectedDefinitions));
        if (selectedBindings == null)
            throw new ArgumentNullException(nameof(selectedBindings));
        return new CityRuntime(runtimeId, null, location, logger, marketCounterparty,
            selectedDefinition, selectedDefinitions, selectedBindings);
    }

    /// <summary>
    /// Creates the selected runtime from the admitted Core definition graph.
    /// Unity authoring bindings are optional presentation/compatibility views;
    /// they are not inputs to City owner construction or selected material flow.
    /// </summary>
    internal static CityRuntime CreateSelectedDailyV1(
        string runtimeId,
        DailyV1CityDefinition selectedDefinition,
        DailyV1EffectiveDefinitions selectedDefinitions,
        SpatialLocationRuntime location,
        SimulationLogger logger = null,
        MarketCounterpartyRuntime marketCounterparty = null)
    {
        if (selectedDefinition == null)
            throw new ArgumentNullException(nameof(selectedDefinition));
        if (selectedDefinitions == null)
            throw new ArgumentNullException(nameof(selectedDefinitions));
        return new CityRuntime(runtimeId, null, location, logger, marketCounterparty,
            selectedDefinition, selectedDefinitions, null);
    }

    internal static CityRuntime CreateSelectedDailyV1(
        string runtimeId,
        DailyV1CityDefinition selectedDefinition,
        DailyV1EffectiveDefinitions selectedDefinitions,
        DailyV1DefinitionAdapter.SelectedBindings selectedBindings,
        DailyV1CityRuntimeOwners selectedOwners,
        SpatialLocationRuntime location,
        SimulationLogger logger = null)
    {
        if (selectedDefinition == null) throw new ArgumentNullException(nameof(selectedDefinition));
        if (selectedDefinitions == null) throw new ArgumentNullException(nameof(selectedDefinitions));
        if (selectedBindings == null) throw new ArgumentNullException(nameof(selectedBindings));
        if (selectedOwners == null) throw new ArgumentNullException(nameof(selectedOwners));
        return new CityRuntime(runtimeId, null, location, logger, selectedOwners.Counterparty,
            selectedDefinition, selectedDefinitions, selectedBindings, selectedOwners);
    }

    private CityRuntime(
        string runtimeId,
        CityData cityData,
        SpatialLocationRuntime location,
        SimulationLogger logger,
        MarketCounterpartyRuntime marketCounterparty,
        DailyV1CityDefinition selectedDefinition,
        DailyV1EffectiveDefinitions selectedDefinitions = null,
        DailyV1DefinitionAdapter.SelectedBindings selectedBindings = null,
        DailyV1CityRuntimeOwners initialSelectedOwners = null)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("CityRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }
        if (selectedDefinition != null
            && (selectedDefinitions == null
                || (selectedBindings != null
                    && (!selectedBindings.TryGetAsset(selectedDefinition, out CityData exactAsset)
                        || exactAsset == null
                        || !string.Equals(selectedDefinition.DefinitionId, exactAsset.DefinitionId, StringComparison.Ordinal)))
                || !selectedDefinitions.Cities.TryGetValue(selectedDefinition.DefinitionId, out DailyV1CityDefinition graphNode)
                || !ReferenceEquals(graphNode, selectedDefinition)
                || !AreSelectedDefinitionInputsAdmitted(selectedDefinition, selectedDefinitions)
                || (initialSelectedOwners != null
                    && (!string.Equals(initialSelectedOwners.RuntimeId, runtimeId, StringComparison.Ordinal)
                        || !string.Equals(initialSelectedOwners.LocationRuntimeId, location.RuntimeId, StringComparison.Ordinal)
                        || !ReferenceEquals(initialSelectedOwners.Definitions, selectedDefinitions)
                        || !ReferenceEquals(initialSelectedOwners.Definition, selectedDefinition)))))
        {
            throw new ArgumentException(
                "The selected City runtime must consume an exact admitted typed definition graph node and its admitted item identities.",
                nameof(selectedDefinition));
        }

        this.runtimeId = runtimeId;
        this.cityData = selectedDefinition == null ? cityData : null;
        selectedDailyV1Definition = selectedDefinition;
        selectedDefinitionGraph = selectedDefinitions;
        selectedDefinitionBindings = selectedBindings;
        this.location = location;
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
        if (selectedDefinition != null)
        {
            DailyV1CityRuntimeOwners selectedOwners = initialSelectedOwners;
            if (selectedOwners == null
                && !DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
                    runtimeId, location.RuntimeId, selectedDefinitions, selectedDefinition, marketCounterparty,
                    out selectedOwners))
                throw new ArgumentException(
                    "The selected City owners could not be created from the admitted Daily-v1 graph.",
                    nameof(selectedDefinition));
            selectedRuntimeOwners = selectedOwners;
            this.marketCounterparty = selectedOwners.Counterparty;
            population = selectedOwners.Population;
            market = new MarketRuntime(selectedOwners.Market);
            if (selectedBindings != null
                && !market.TryBindSelectedDailyV1Owner(selectedDefinitions, selectedBindings))
                throw new ArgumentException(
                    "The selected City Market could not bind its optional Unity compatibility projection.",
                    nameof(selectedBindings));
        }
        else
        {
            this.marketCounterparty = marketCounterparty != null
                ? NormalizeMarketCounterparty(marketCounterparty, runtimeId)
                : CreateConfiguredMarketCounterparty(runtimeId);
            population = CreatePopulationRuntime();
            market = HasConfiguredDefinition
                ? new MarketRuntime(RuntimeMarketItems, this.marketCounterparty)
                : new MarketRuntime(new List<MarketItemConfig>(), this.marketCounterparty);
        }
        if (HasConfiguredDefinition && (RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily
            || RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily
            || finiteProductionSources != null || HasAuthoredFiniteReserve()))
        {
            bool mixed = RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily;
            if (selectedDefinition != null)
            {
                DailyV1CityData selectedCity = selectedDefinition.Values;
                if (selectedCity.productionConfigs == null)
                    throw new LocalDailyMaterialFlowRejectedException("Finite-reserve profile requires selected source configuration.");
                if (mixed)
                {
                    if (!FiniteSourceProfileAdmission.TryValidateMixedSourceCity(selectedCity, out string mixedRejection))
                        throw new LocalDailyMaterialFlowRejectedException(mixedRejection);
                }
                else if (selectedCity.productionConfigs.Count != 1)
                    throw new LocalDailyMaterialFlowRejectedException("Finite-reserve profile requires exactly one authored source.");
            }
            else
            {
                if (RuntimeProductionConfigs == null)
                    throw new LocalDailyMaterialFlowRejectedException("Finite-reserve profile requires authored source configuration.");
                if (mixed)
                {
                    if (!FiniteSourceProfileAdmission.TryValidateMixedSourceCity(cityData, out string mixedRejection))
                        throw new LocalDailyMaterialFlowRejectedException(mixedRejection);
                }
                else if (RuntimeProductionConfigs.Count != 1)
                    throw new LocalDailyMaterialFlowRejectedException("Finite-reserve profile requires exactly one authored source.");
            }
            if (selectedDefinition != null)
            {
                if (selectedRuntimeOwners?.FiniteSource == null)
                    throw new LocalDailyMaterialFlowRejectedException("Finite-reserve state was not created by the selected Core City owner.");
                finiteProductionSources = new FiniteProductionSourceStore(selectedRuntimeOwners.FiniteSource);
            }
            else
            {
                CityProductionConfig finiteSource = mixed
                    ? RuntimeProductionConfigs.Find(row => row != null
                        && row.sourceKind == CityProductionSourceKind.FiniteReserveDaily)
                    : RuntimeProductionConfigs[0];
                if (finiteSource == null)
                    throw new LocalDailyMaterialFlowRejectedException("Finite-reserve source row is missing.");
                finiteProductionSources = new FiniteProductionSourceStore(
                    RuntimeMaterialFlowProfile,
                    finiteSource,
                    RuntimeSettlementSemanticId,
                    RuntimeMarketStoreSemanticId);
            }
        }
        populationEconomy = selectedRuntimeOwners != null
            ? selectedRuntimeOwners.PopulationEconomy
            : CreateConfiguredPopulationEconomy();
    }

    public IReadOnlyList<CityProductionResult> SimulateProductionDay()
    {
        List<CityProductionResult> results = new List<CityProductionResult>();

        if (HasConfiguredDefinition && (RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.FiniteReserveDaily
            || RuntimeMaterialFlowProfile == LocalMaterialFlowProfile.MixedSourcesDaily
            || finiteProductionSources != null || HasAuthoredFiniteReserve()))
            throw new LocalDailyMaterialFlowRejectedException(
                "Finite-reserve production requires a boundary-aware prepared daily economy operation.");

        if (!HasConfiguredDefinition || RuntimeProductionConfigs == null)
        {
            if (selectedDailyV1Definition != null && selectedRuntimeOwners != null)
            {
                foreach (DailyV1CityProductionConfig selectedProduction in
                    selectedDailyV1Definition.Values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
                {
                    if (selectedProduction == null || selectedProduction.amountPerDay <= 0
                        || !TryResolveSelectedItemDefinition(selectedProduction.item, out DailyV1ItemDefinition selectedItem))
                        continue;

                    int selectedProduced = Market.AddSelectedStock(selectedItem, selectedProduction.amountPerDay);
                    if (selectedProduced <= 0)
                        continue;

                    results.Add(new CityProductionResult(
                        RuntimeId, selectedItem.DefinitionId, selectedProduced, Market.StockOwnerRuntimeId));
                    logger?.Log(SimulationLogCategory.EconomyProduction,
                        $"{CityName} produziu {selectedProduced} {selectedItem.Values.itemName}");
                }
                return results.AsReadOnly();
            }
            return results.AsReadOnly();
        }

        foreach (CityProductionConfig production in RuntimeProductionConfigs)
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

        if (!HasConfiguredDefinition)
        {
            return results.AsReadOnly();
        }

        if (selectedDailyV1Definition != null)
        {
            foreach (DailyV1MarketItemConfig selectedConfig in
                selectedDailyV1Definition.Values.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
            {
                if (selectedConfig == null || selectedConfig.consumptionPer1000Population <= 0f
                    || !TryResolveSelectedItemDefinition(selectedConfig.item, out DailyV1ItemDefinition selectedItem))
                    continue;

                int desiredConsumption = UnityMathfCompatibility.RoundToInt(
                    Population.CurrentPopulation / 1000f * selectedConfig.consumptionPer1000Population);
                if (desiredConsumption <= 0)
                {
                    results.Add(new CityConsumptionResult(
                        RuntimeId,
                        PopulationEconomy.PopulationEconomicRuntimeId,
                        selectedItem.DefinitionId,
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
                    consumed = Market.RemoveSelectedStock(selectedItem, desiredConsumption);
                }
                else
                {
                    EconomyTransactionResult receipt = new EconomyTransactionService()
                        .TryExecutePopulationConsumption(PopulationEconomy, Market, selectedItem, desiredConsumption);
                    consumed = receipt.Quantity;
                    unitPrice = receipt.UnitPrice;
                    totalPaid = receipt.TotalPrice;
                }

                results.Add(new CityConsumptionResult(
                    RuntimeId,
                    PopulationEconomy.PopulationEconomicRuntimeId,
                    selectedItem.DefinitionId,
                    desiredConsumption,
                    consumed,
                    PopulationEconomy.PaymentMode,
                    unitPrice,
                    totalPaid));

                if (consumed > 0)
                    logger?.Log(SimulationLogCategory.EconomyConsumption,
                        $"{CityName} consumiu {consumed} {selectedItem.Values.itemName}");
            }
            return results.AsReadOnly();
        }

        if (RuntimeMarketItems == null)
            return results.AsReadOnly();

        foreach (MarketItemConfig config in RuntimeMarketItems)
        {
            if (config == null || config.item == null || config.consumptionPer1000Population <= 0f)
            {
                continue;
            }

            int desiredConsumption = UnityMathfCompatibility.RoundToInt(Population.CurrentPopulation / 1000f * config.consumptionPer1000Population);
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

    internal void LogSelectedDailyEconomy(SimulationLogCategory category, string message) =>
        logger?.Log(category, message);

    internal void ProjectSelectedLocalMaterialFlow(
        DailyV1CityRuntimeOwners expectedOwner,
        LocalDailyMaterialFlowResult result)
    {
        if (!ReferenceEquals(selectedRuntimeOwners, expectedOwner))
            throw new InvalidOperationException("The selected City material-flow projection is not bound to its exact Core owner.");
        lastMaterialFlow = result;
        expectedOwner.RecordSelectedMaterialFlow(result);
    }

    internal int ExecuteSelectedAccountBackedPopulationConsumption(
        DailyV1CityRuntimeOwners expectedOwner,
        DailyV1ItemDefinition item,
        int requestedQuantity)
    {
        if (!ReferenceEquals(selectedRuntimeOwners, expectedOwner)
            || expectedOwner == null
            || !ReferenceEquals(Population, expectedOwner.Population)
            || !ReferenceEquals(PopulationEconomy, expectedOwner.PopulationEconomy)
            || !Market.IsBoundToSelectedOwner(expectedOwner.Market)
            || item == null
            || !expectedOwner.Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admitted)
            || !ReferenceEquals(item, admitted))
            throw new InvalidOperationException("The selected population transaction is not bound to its exact Core City owners.");

        return new EconomyTransactionService().TryExecutePopulationConsumption(
            expectedOwner.PopulationEconomy, Market, item, requestedQuantity).Quantity;
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
        if (npcRuntime == null || !ContainsImportantNpc(npcRuntime))
        {
            return;
        }

        if (!CanCommitP12PresenceMutation()) return;
        if (selectedRuntimeOwners != null && npcRuntime.SelectedRuntimeOwners != null
            && ReferenceEquals(npcRuntime.CurrentCity, this))
        {
            if (!CanRemoveImportantNpcMembership(npcRuntime)
                || !npcRuntime.ClearCurrentPresenceFromCity(this))
                return;
            NotifyP12PresenceMutationCommitted();
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

        NotifyP12PresenceMutationCommitted();
    }

    internal bool ContainsImportantNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null || !ImportantNpcs.Contains(npcRuntime)) return false;
        return selectedRuntimeOwners == null
            || selectedRuntimeOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
    }

    /// <summary>
    /// Attaches the Unity object projection for membership already composed by
    /// the Core genesis graph. This does not write the owner or advance its
    /// P12 revision; the Core builder has already installed that authority.
    /// </summary>
    internal bool TryAttachInitialSelectedNpcProjection(NpcRuntime npcRuntime)
    {
        DailyV1CityRuntimeOwners cityOwners = selectedRuntimeOwners;
        DailyV1NpcRuntimeOwners npcOwners = npcRuntime?.SelectedRuntimeOwners;
        if (cityOwners == null || npcOwners == null
            || !ReferenceEquals(cityOwners.Definitions, npcOwners.Definitions)
            || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)
            || !cityOwners.NpcPresence.Contains(npcRuntime.RuntimeId)
            || !string.Equals(npcOwners.Travel.CurrentCityRuntimeId, RuntimeId, StringComparison.Ordinal)
            || !string.Equals(npcOwners.Travel.CurrentLocationRuntimeId, Location?.RuntimeId, StringComparison.Ordinal)
            || npcRuntime.IsTraveling || !npcRuntime.IsAlive)
            return false;

        if (selectedNpcPresenceProjectionResolver != null)
            return TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out IReadOnlyList<NpcRuntime> projection)
                && projection.Contains(npcRuntime)
                && IsExactSelectedNpcProjection(npcRuntime);

        List<NpcRuntime> membership = EnsureImportantNpcs();
        if (membership.Contains(npcRuntime)) return true;
        if (ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime)) return false;
        membership.Add(npcRuntime);
        return true;
    }

    /// <summary>
    /// Updates only the Unity object-reference projection after the Core
    /// selected presence owner has committed the corresponding stable ID.
    /// This method never writes the Core owner or advances its revision.
    /// </summary>
    internal bool TryApplySelectedNpcPresenceProjection(NpcRuntime npcRuntime, bool shouldContain)
    {
        DailyV1CityRuntimeOwners cityOwners = selectedRuntimeOwners;
        DailyV1NpcRuntimeOwners npcOwners = npcRuntime?.SelectedRuntimeOwners;
        if (cityOwners == null || npcOwners == null
            || !ReferenceEquals(cityOwners.Definitions, npcOwners.Definitions)
            || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)
            || cityOwners.NpcPresence.Contains(npcRuntime.RuntimeId) != shouldContain)
            return false;

        if (selectedNpcPresenceProjectionResolver != null)
        {
            if (!TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out IReadOnlyList<NpcRuntime> projection))
                return false;
            return shouldContain
                ? projection.Contains(npcRuntime) && IsExactSelectedNpcProjection(npcRuntime)
                : !projection.Any(member => string.Equals(
                    member?.RuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal));
        }

        List<NpcRuntime> membership = EnsureImportantNpcs();
        bool containsExactProjection = membership.Contains(npcRuntime);
        if (shouldContain)
        {
            if (containsExactProjection) return true;
            if (ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime)) return false;
            membership.Add(npcRuntime);
            readOnlyImportantNpcs = null;
            return true;
        }

        if (!containsExactProjection)
            return !ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime);
        membership.Remove(npcRuntime);
        readOnlyImportantNpcs = null;
        return true;
    }

    internal bool CanPrepareSelectedNpcPresenceProjection(NpcRuntime npcRuntime, bool shouldContain)
    {
        DailyV1CityRuntimeOwners cityOwners = selectedRuntimeOwners;
        DailyV1NpcRuntimeOwners npcOwners = npcRuntime?.SelectedRuntimeOwners;
        if (cityOwners == null || npcOwners == null
            || !ReferenceEquals(cityOwners.Definitions, npcOwners.Definitions)
            || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId))
            return false;
        if (selectedNpcPresenceProjectionResolver != null)
        {
            if (!TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out IReadOnlyList<NpcRuntime> projection))
                return false;
            bool ownerContainsNpc = cityOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
            bool projectionContainsNpc = projection.Contains(npcRuntime);
            bool projectionHasRuntimeId = projection.Any(member => string.Equals(
                member?.RuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal));
            return IsExactSelectedNpcProjection(npcRuntime)
                && (ownerContainsNpc
                    ? projectionContainsNpc
                    : !projectionHasRuntimeId);
        }
        bool containsExactProjection = EnsureImportantNpcs().Contains(npcRuntime);
        if (shouldContain)
            return containsExactProjection || !ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime);
        return !ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime);
    }

    internal bool HasConsistentSelectedNpcPresenceProjection(NpcRuntime npcRuntime)
    {
        DailyV1CityRuntimeOwners cityOwners = selectedRuntimeOwners;
        DailyV1NpcRuntimeOwners npcOwners = npcRuntime?.SelectedRuntimeOwners;
        if (cityOwners == null || npcOwners == null
            || !ReferenceEquals(cityOwners.Definitions, npcOwners.Definitions)
            || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)
            || ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime))
            return false;
        if (selectedNpcPresenceProjectionResolver != null)
            return cityOwners.NpcPresence.Contains(npcRuntime.RuntimeId)
                == ImportantNpcs.Contains(npcRuntime);
        return cityOwners.NpcPresence.Contains(npcRuntime.RuntimeId)
            == EnsureImportantNpcs().Contains(npcRuntime);
    }

    internal bool CanAddImportantNpcMembership(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null) return false;
        if (selectedRuntimeOwners != null)
        {
            if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)) return false;
            if (selectedNpcPresenceProjectionResolver != null)
            {
                if (!TryCreateSelectedNpcPresenceProjection(
                        selectedNpcPresenceProjectionResolver,
                        out IReadOnlyList<NpcRuntime> projection))
                    return false;
                bool ownerContainsNpc = selectedRuntimeOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
                return ownerContainsNpc
                    ? projection.Contains(npcRuntime) && IsExactSelectedNpcProjection(npcRuntime)
                    : IsExactSelectedNpcProjection(npcRuntime)
                        && !ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime)
                        && CanApplyImportantNpcRevisionIncrements(1L);
            }
            bool inProjection = EnsureImportantNpcs().Contains(npcRuntime);
            bool inOwner = selectedRuntimeOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
            if (inProjection != inOwner) return false;
            if (inProjection) return true;
            if (ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime)) return false;
        }
        else if (EnsureImportantNpcs().Contains(npcRuntime)) return true;
        return CanApplyImportantNpcRevisionIncrements(1L);
    }

    internal bool CanRemoveImportantNpcMembership(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null) return true;
        if (selectedRuntimeOwners != null)
        {
            if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)) return false;
            if (selectedNpcPresenceProjectionResolver != null)
            {
                if (!TryCreateSelectedNpcPresenceProjection(
                        selectedNpcPresenceProjectionResolver,
                        out IReadOnlyList<NpcRuntime> projection))
                    return false;
                bool ownerContainsNpc = selectedRuntimeOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
                return !ownerContainsNpc || (projection.Contains(npcRuntime)
                    && IsExactSelectedNpcProjection(npcRuntime)
                    && CanApplyImportantNpcRevisionIncrements(1L));
            }
            bool inProjection = EnsureImportantNpcs().Contains(npcRuntime);
            bool inOwner = selectedRuntimeOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
            if (inProjection != inOwner) return false;
            if (!inProjection) return true;
        }
        else if (!EnsureImportantNpcs().Contains(npcRuntime)) return true;
        return CanApplyImportantNpcRevisionIncrements(1L);
    }

    internal bool CanApplyImportantNpcRevisionIncrements(long increments)
    {
        return selectedRuntimeOwners != null
            ? selectedRuntimeOwners.NpcPresence.CanApplyRevisionIncrements(increments)
            : increments >= 0L && importantNpcRevision <= long.MaxValue - increments;
    }

    internal bool TryAddImportantNpcMembership(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return false;
        }

        if (selectedRuntimeOwners != null && selectedNpcPresenceProjectionResolver != null)
        {
            DailyV1CityNpcPresenceOwner selectedPresence = selectedRuntimeOwners.NpcPresence;
            if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)) return false;
            if (!TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out _))
                return false;
            if (selectedPresence.Contains(npcRuntime.RuntimeId))
                return IsExactSelectedNpcProjection(npcRuntime);
            return IsExactSelectedNpcProjection(npcRuntime)
                && selectedPresence.CanApplyRevisionIncrements(1L)
                && selectedPresence.TryAdd(npcRuntime.RuntimeId);
        }

        List<NpcRuntime> membership = EnsureImportantNpcs();
        if (membership.Contains(npcRuntime))
        {
            return selectedRuntimeOwners == null
                || selectedRuntimeOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
        }

        DailyV1CityNpcPresenceOwner presenceOwner = selectedRuntimeOwners?.NpcPresence;
        if (presenceOwner != null
            && (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)
                || presenceOwner.Contains(npcRuntime.RuntimeId)
                || ContainsDifferentImportantNpcWithSameRuntimeId(npcRuntime)))
            return false;

        if (presenceOwner == null && importantNpcRevision == long.MaxValue
            || presenceOwner != null && !presenceOwner.CanApplyRevisionIncrements(1L))
        {
            return false;
        }

        membership.Add(npcRuntime);
        if (presenceOwner != null)
        {
            if (presenceOwner.TryAdd(npcRuntime.RuntimeId)) return true;
            membership.Remove(npcRuntime);
            return false;
        }

        importantNpcRevision++;
        return true;
    }

    internal bool TryRemoveImportantNpcMembership(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return true;
        }

        if (selectedRuntimeOwners != null && selectedNpcPresenceProjectionResolver != null)
        {
            DailyV1CityNpcPresenceOwner selectedPresence = selectedRuntimeOwners.NpcPresence;
            if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)) return false;
            if (!TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out IReadOnlyList<NpcRuntime> projection))
                return false;
            if (!selectedPresence.Contains(npcRuntime.RuntimeId))
                return !projection.Any(member => string.Equals(
                    member?.RuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal));
            return IsExactSelectedNpcProjection(npcRuntime)
                && selectedPresence.CanApplyRevisionIncrements(1L)
                && selectedPresence.TryRemove(npcRuntime.RuntimeId);
        }

        List<NpcRuntime> membership = EnsureImportantNpcs();
        int membershipIndex = membership.IndexOf(npcRuntime);
        if (membershipIndex < 0)
        {
            return selectedRuntimeOwners == null
                || !selectedRuntimeOwners.NpcPresence.Contains(npcRuntime.RuntimeId);
        }

        DailyV1CityNpcPresenceOwner presenceOwner = selectedRuntimeOwners?.NpcPresence;
        if (presenceOwner != null
            && (!presenceOwner.Contains(npcRuntime.RuntimeId)
                || !presenceOwner.CanApplyRevisionIncrements(1L)))
            return false;
        if (presenceOwner == null && importantNpcRevision == long.MaxValue)
        {
            return false;
        }

        membership.RemoveAt(membershipIndex);
        if (presenceOwner != null)
        {
            if (presenceOwner.TryRemove(npcRuntime.RuntimeId)) return true;
            membership.Insert(membershipIndex, npcRuntime);
            return false;
        }

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

    internal bool CanBindSelectedNpcPresenceProjectionResolver(
        Func<string, NpcRuntime> resolver)
    {
        return resolver != null
            && selectedRuntimeOwners != null
            && (selectedNpcPresenceProjectionResolver == null
                || ReferenceEquals(selectedNpcPresenceProjectionResolver, resolver))
            && TryCreateSelectedNpcPresenceProjection(resolver, out _);
    }

    internal bool TryBindSelectedNpcPresenceProjectionResolver(
        Func<string, NpcRuntime> resolver)
    {
        if (!CanBindSelectedNpcPresenceProjectionResolver(resolver)) return false;
        selectedNpcPresenceProjectionResolver = resolver;
        importantNpcs = new List<NpcRuntime>();
        readOnlyImportantNpcs = null;
        return true;
    }

    private bool TryCreateSelectedNpcPresenceProjection(
        Func<string, NpcRuntime> resolver,
        out IReadOnlyList<NpcRuntime> projection)
    {
        projection = null;
        DailyV1CityNpcPresenceOwner owner = selectedRuntimeOwners?.NpcPresence;
        if (resolver == null || owner == null
            || !string.Equals(owner.CityRuntimeId, runtimeId, StringComparison.Ordinal))
            return false;

        List<NpcRuntime> resolved = new List<NpcRuntime>(owner.OrderedNpcRuntimeIds.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<NpcRuntime> npcInstances = new HashSet<NpcRuntime>();
        foreach (string npcRuntimeId in owner.OrderedNpcRuntimeIds)
        {
            if (string.IsNullOrWhiteSpace(npcRuntimeId) || !runtimeIds.Add(npcRuntimeId))
                return false;
            NpcRuntime npc = resolver(npcRuntimeId);
            if (npc == null
                || !string.Equals(npc.RuntimeId, npcRuntimeId, StringComparison.Ordinal)
                || !npcInstances.Add(npc))
                return false;
            resolved.Add(npc);
        }

        projection = new ReadOnlyCollection<NpcRuntime>(resolved);
        return true;
    }

    private bool IsExactSelectedNpcProjection(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)
            || selectedNpcPresenceProjectionResolver == null)
            return false;
        NpcRuntime resolved = selectedNpcPresenceProjectionResolver(npcRuntime.RuntimeId);
        return ReferenceEquals(resolved, npcRuntime)
            && string.Equals(resolved?.RuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal);
    }

    private bool ContainsDifferentImportantNpcWithSameRuntimeId(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null || string.IsNullOrWhiteSpace(npcRuntime.RuntimeId)) return false;
        IReadOnlyList<NpcRuntime> members;
        if (selectedRuntimeOwners != null && selectedNpcPresenceProjectionResolver != null)
        {
            if (!TryCreateSelectedNpcPresenceProjection(
                    selectedNpcPresenceProjectionResolver,
                    out members))
                return true;
        }
        else
        {
            members = EnsureImportantNpcs();
        }
        foreach (NpcRuntime member in members)
        {
            if (member != null && !ReferenceEquals(member, npcRuntime)
                && string.Equals(member.RuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    internal void BindP12PresenceMutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12PresenceMutationAdmission != null || p12PresenceMutationCommitted != null)
            throw new InvalidOperationException("CityRuntime is already bound to a P12 NPC-presence mutation boundary.");
        p12PresenceMutationAdmission = admission;
        p12PresenceMutationCommitted = committed;
    }

    internal bool UnbindP12PresenceMutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12PresenceMutationAdmission, admission)
            || !ReferenceEquals(p12PresenceMutationCommitted, committed)) return false;
        p12PresenceMutationAdmission = null;
        p12PresenceMutationCommitted = null;
        return true;
    }

    private bool CanCommitP12PresenceMutation()
    {
        if (p12PresenceMutationAdmission == null) return true;
        try { return p12PresenceMutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12PresenceMutationCommitted()
    {
        if (p12PresenceMutationCommitted == null) return;
        try { p12PresenceMutationCommitted(); }
        catch { }
    }

    internal bool CanBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return runtimeMutationGuardBinding.CanBindTo(guard)
            && PopulationOwnerBridge.CanBindMutationStatus(Population, guard);
    }

    internal bool TryBindRuntimeMutationGuard(AuthoritativeMutationGuard guard)
    {
        EnsureRuntimeMutationGuardBinding();
        return CanBindRuntimeMutationGuard(guard)
            && PopulationOwnerBridge.TryBindMutationStatus(Population, guard)
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

    private MarketCounterpartyRuntime CreateConfiguredMarketCounterparty(
        string cityRuntimeId)
    {
        MarketLiquidityMode liquidityMode = RuntimeMarketLiquidityMode;
        if (!HasConfiguredDefinition || liquidityMode == MarketLiquidityMode.Open)
        {
            return MarketCounterpartyRuntime.CreateOpen(cityRuntimeId);
        }

        if (liquidityMode != MarketLiquidityMode.AccountBacked)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cityRuntimeId),
                liquidityMode,
                "CityData contains an unsupported MarketLiquidityMode.");
        }

        MoneyAccountRuntime account = new MoneyAccountRuntime(RuntimeMarketLiquidityPurchasingPower);
        return MarketCounterpartyRuntime.CreateAccountBacked(cityRuntimeId, account);
    }
    private PopulationEconomyRuntime CreateConfiguredPopulationEconomy()
    {
        return new PopulationEconomyRuntime(
            runtimeId,
            RuntimePopulationPaymentMode,
            RuntimePopulationPurchasingPower,
            MarketCounterparty.LiquidityMode);
    }
    private SettlementPopulationRuntime CreatePopulationRuntime()
    {
        int configuredPopulation = RuntimeInitialPopulation;
        int initialPopulation = UnityMathfCompatibility.Max(0, configuredPopulation);
        return new SettlementPopulationRuntime(runtimeId, initialPopulation);
    }

    internal bool TryBindSelectedDailyV1Definition(
        DailyV1CityDefinition definition,
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (definition == null || definitions == null
            || (bindings != null
                && (!bindings.TryGetAsset(definition, out CityData assetIdentity)
                    || assetIdentity == null
                    || (cityData != null && !ReferenceEquals(assetIdentity, cityData))
                    || !string.Equals(definition.DefinitionId, assetIdentity.DefinitionId, StringComparison.Ordinal)))
            || !definitions.Cities.TryGetValue(definition.DefinitionId, out DailyV1CityDefinition graphNode)
            || !ReferenceEquals(graphNode, definition)
            || !AreSelectedDefinitionInputsAdmitted(definition, definitions))
            return false;
        if (selectedDailyV1Definition != null
            && (!ReferenceEquals(selectedDailyV1Definition, definition)
                || !ReferenceEquals(selectedDefinitionGraph, definitions)
                || (selectedDefinitionBindings != null && bindings != null
                    && !ReferenceEquals(selectedDefinitionBindings, bindings))))
            return false;
        if (selectedDailyV1Definition != null && selectedRuntimeOwners != null)
            return TryAttachSelectedDefinitionBindings(definitions, bindings);

        DailyV1CityRuntimeOwners stagedOwners = null;
        DailyV1CityNpcPresenceOwner stagedPresenceOwner = null;
        if (market != null && market.SelectedDailyV1Owner != null
            && !DailyV1CityNpcPresenceOwner.TryCreateBound(
                runtimeId,
                ImportantNpcs.Select(npc => npc?.RuntimeId),
                importantNpcRevision,
                out stagedPresenceOwner))
            return false;
        if (market != null && market.SelectedDailyV1Owner != null
            && !DailyV1SelectedRuntimeOwnerGraph.TryCreateBoundCityOwner(
                runtimeId, location?.RuntimeId, definitions, definition, population,
                market.SelectedDailyV1Owner, MarketCounterparty, PopulationEconomy,
                stagedPresenceOwner,
                out stagedOwners))
            return false;
        if (market != null && market.SelectedDailyV1Owner != null
            && !market.TryBindSelectedDailyV1Owner(definitions, bindings))
            return false;
        selectedDailyV1Definition = definition;
        selectedDefinitionGraph = definitions;
        selectedDefinitionBindings = bindings;
        selectedRuntimeOwners = stagedOwners;
        return true;
    }

    private bool TryAttachSelectedDefinitionBindings(
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (bindings == null || selectedDefinitionBindings != null)
            return true;
        if (market != null && market.SelectedDailyV1Owner != null
            && !market.TryBindSelectedDailyV1Owner(definitions, bindings))
            return false;
        selectedDefinitionBindings = bindings;
        return true;
    }

    internal bool TryResolveSelectedDailyV1CityAsset(string definitionId, out CityData city)
    {
        city = null;
        return selectedDailyV1Definition != null && selectedDefinitionGraph != null
            && selectedDefinitionBindings != null && !string.IsNullOrWhiteSpace(definitionId)
            && selectedDefinitionGraph.Cities.TryGetValue(definitionId, out DailyV1CityDefinition node)
            && selectedDefinitionBindings.TryGetAsset(node, out city)
            && city != null;
    }

    private static bool AreSelectedDefinitionInputsAdmitted(
        DailyV1CityDefinition definition,
        DailyV1EffectiveDefinitions definitions)
    {
        if (definition?.Values == null || definitions == null
            || !definitions.Cities.TryGetValue(definition.DefinitionId, out DailyV1CityDefinition admittedCity)
            || !ReferenceEquals(admittedCity, definition))
            return false;

        DailyV1CityData values = definition.Values;
        foreach (DailyV1MarketItemConfig row in values.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
        {
            if (row != null && !string.IsNullOrWhiteSpace(row.item)
                && !definitions.Items.ContainsKey(row.item)) return false;
        }
        foreach (DailyV1CityProductionConfig row in values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
        {
            if (row != null && !string.IsNullOrWhiteSpace(row.item)
                && !definitions.Items.ContainsKey(row.item)) return false;
        }
        return true;
    }
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
    internal readonly FiniteSourceProductionPreparation FiniteSource;
    internal readonly P14CMixedSourceProductionPreparation MixedSource;
    internal bool Completed;
    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
    internal CityDailyEconomyCommit(CityRuntime owner, CityDailyEconomyReceipt receipt,
        PreparedMarketState marketState, long marketRevision, long marketIncrements,
        MoneyAccountRuntime populationAccount, long populationRevision, long populationIncrements, float populationBalance,
        MoneyAccountRuntime settlementAccount, long settlementRevision, long settlementIncrements, float settlementBalance,
        Dictionary<string, CityDailyEconomyReceipt> nextReceipts, long expectedReceiptRevision, bool replay,
        string expectedOwnerRevision, long expectedPopulationRevision, int expectedPopulation, string fingerprint,
        FiniteSourceProductionPreparation finiteSource, P14CMixedSourceProductionPreparation mixedSource)
    {
        this.owner = owner; Receipt = receipt; MarketState = marketState; MarketRevision = marketRevision;
        MarketIncrements = marketIncrements; PopulationAccount = populationAccount; PopulationRevision = populationRevision;
        PopulationIncrements = populationIncrements; PopulationBalance = populationBalance; SettlementAccount = settlementAccount;
        SettlementRevision = settlementRevision; SettlementIncrements = settlementIncrements; SettlementBalance = settlementBalance;
        NextReceipts = nextReceipts; ExpectedReceiptRevision = expectedReceiptRevision; Replay = replay;
        ExpectedOwnerRevision = expectedOwnerRevision; ExpectedPopulationRevision = expectedPopulationRevision;
        ExpectedPopulation = expectedPopulation; Fingerprint = fingerprint; FiniteSource = finiteSource;
        MixedSource = mixedSource;
    }
    public bool TryCommit(out TimelineFailure failure)
    {
        if (Completed) { failure = TimelineFailure.None; return true; }
        return owner.TryCommitDailyEconomy(this, out failure);
    }
}
