using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private SimulationConfigData simulationConfig;
    [SerializeField] private int daysToSimulate = 1;

    private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
    private List<CityRuntime> cityRuntimeList = new List<CityRuntime>();
    private ExplorableSiteStore explorableSiteStore;

    private readonly List<INpcActionProvider> actionProviders = new List<INpcActionProvider>();
    private Dictionary<CityData, List<CityRuntime>> cityRuntimesByDefinition = new Dictionary<CityData, List<CityRuntime>>();
    private Dictionary<NpcData, List<NpcRuntime>> npcRuntimesByDefinition = new Dictionary<NpcData, List<NpcRuntime>>();
    private Dictionary<ExplorableSiteData, List<ExplorableSiteRuntime>> explorableSiteRuntimesByDefinition = new Dictionary<ExplorableSiteData, List<ExplorableSiteRuntime>>();
    private ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem;
    private ExpeditionStore expeditionStore;
    private Dictionary<SpatialLocationRuntime, CityRuntime> cityRuntimeByLocation = new Dictionary<SpatialLocationRuntime, CityRuntime>();
    private RuntimeIdAllocator runtimeIdAllocator;
    private RuntimeIdentityRegistry runtimeIdentityRegistry;
    private SpatialNetworkRuntime spatialNetwork;
    private SpatialAuthorityStore genesisSpatialAuthority;
    private DomainEventStore domainEventStore;
    private HistoryStore historyStore;
    private DomainEventRecorder domainEventRecorder;
    private SimulationRecordSequence recordSequence;
    private NpcDecisionStore decisionStore;
    private NpcDecisionRecorder decisionRecorder;
    private NpcChronicleService npcChronicleService;
    private NpcChronicleFormatter npcChronicleFormatter;
    private ScheduledDirectiveStore scheduledDirectiveStore;
    private ScheduledDirectiveSystem scheduledDirectiveSystem;
    private SimulationModuleSet enabledModules;
    private JusticeSystem justiceSystem;
    private CrimeSystem crimeSystem;
    private NpcDecisionSystem npcDecisionSystem;
    private TravelSystem travelSystem;
    private TravelPartyStore travelPartyStore;
    private TravelPartySystem travelPartySystem;
    private ExpeditionSystem expeditionSystem;
    private MerchantSystem merchantSystem;
    private CommercialKnowledgeSharingSystem commercialKnowledgeSharingSystem;
    private EconomyTransactionService economyTransactionService;
    private SimulationLogger logger;
    private SimulationRuntime simulationRuntime;
    private SimulationTime simulationTime = new SimulationTime();
    private CalendarDefinition calendarDefinition = CalendarDefinition.CreateDefault();
    private EffectiveSimulationConfiguration effectiveConfiguration;
    private IAuthoritativeRandomSource authoritativeRandomSource;
    private long lastEconomySnapshotDay;
    private SimulationBootstrapComposition publishedComposition;


    public SimulationBootstrapComposition Bootstrap => publishedComposition;
    public string FullLog => publishedComposition != null && logger != null ? logger.FullLog : string.Empty;
    public SimulationTime SimulationTime => publishedComposition?.SimulationTime;
    public CalendarDefinition Calendar => publishedComposition?.Calendar;
    public SpatialNetworkRuntime SpatialNetwork => publishedComposition?.SpatialNetwork;
    public DomainEventStore DomainEventStore => publishedComposition?.DomainEventStore;
    public HistoryStore History => publishedComposition?.History;
    public ScheduledDirectiveStore ScheduledDirectives => publishedComposition?.ScheduledDirectives;
    public NpcDecisionStore Decisions => publishedComposition?.Decisions;
    public NpcChronicleService NpcChronicles => publishedComposition?.NpcChronicles;
    public NpcChronicleFormatter ChronicleFormatter => publishedComposition?.ChronicleFormatter;
    public TravelPartyStore TravelParties => publishedComposition?.TravelParties;
    public TravelPartySystem GroupTravel => publishedComposition?.GroupTravel;
    public SimulationRuntime Runtime => publishedComposition?.Runtime;
    public ExplorableSiteStore ExplorableSites => publishedComposition?.ExplorableSites;
    public ExpeditionStore Expeditions => publishedComposition?.Expeditions;
    public ExpeditionSystem ExpeditionRuntime => publishedComposition?.ExpeditionSystem;
    public ExpeditionSystem ExpeditionSystem => publishedComposition?.ExpeditionSystem;
    public long CurrentDay => publishedComposition != null ? publishedComposition.SimulationTime.AbsoluteDay : 0;
    public SimulationDate CurrentDate => publishedComposition != null
        ? publishedComposition.Runtime.Calendar.GetDate(CurrentDay)
        : default(SimulationDate);

    public bool TryStartTravelParty(ActionExecutionContext context)
    {
        return publishedComposition != null && publishedComposition.Runtime.TryStartTravelParty(context);
    }

    public bool TryStartExpedition(
        ExplorableSiteRuntime targetSite,
        ActionExecutionContext context,
        out ExpeditionRuntime expedition)
    {
        if (publishedComposition == null || publishedComposition.ExpeditionSystem == null)
        {
            expedition = null;
            return false;
        }

        return publishedComposition.ExpeditionSystem.TryStartExpedition(targetSite, context, out expedition);
    }

    public void Start()
    {
        InitializeSimulation();
    }

    public void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            Simulate(Mathf.Max(1, daysToSimulate));
        }
    }

    private void InitializeSimulation(System.Action<string> stageCompleted = null)
    {
        if (publishedComposition != null) return;
        string profileFingerprint = null;
        System.Collections.Generic.IReadOnlyList<string> profileProvenanceRecords = null;
        SimulationGenesisPipeline.ExecuteStages(stageId =>
        {
            switch (stageId)
            {
                case "p9.genesis.resolve-profile/v1":
                    SimulationGenesisPipeline.ValidateProfile(simulationConfig);
                    simulationTime = new SimulationTime();
                    authoritativeRandomSource = new DeterministicRandomSource(simulationConfig.useFixedSimulationSeed ? simulationConfig.simulationSeed : 0);
                    lastEconomySnapshotDay = 0;
                    logger = new SimulationLogger(simulationConfig.LogSettings);
                    logger.BeginSimulation(simulationConfig.simulationName, simulationConfig.EnabledModules, simulationConfig.Cities.Count, simulationConfig.Npcs.Count);
                    calendarDefinition = ResolveCalendarDefinition();
                    enabledModules = new SimulationModuleSet(simulationConfig, logger);
                    effectiveConfiguration = ResolveRuntimeConfiguration();
                    genesisSpatialAuthority = new SpatialAuthorityStore();
                    profileFingerprint = SimulationGenesisPipeline.CreateFingerprint(
                        simulationConfig, effectiveConfiguration, calendarDefinition, out profileProvenanceRecords);
                    AppendScenarioDiagnostics(effectiveConfiguration);
                    runtimeIdAllocator = new RuntimeIdAllocator();
                    explorableSiteKnowledgeSystem = new ExplorableSiteKnowledgeSystem();
                    recordSequence = new SimulationRecordSequence();
                    historyStore = new HistoryStore();
                    domainEventStore = new DomainEventStore(historyStore, new HistoryPolicy(), logger);
                    domainEventRecorder = new DomainEventRecorder(runtimeIdAllocator, simulationTime, recordSequence, domainEventStore, logger);
                    decisionStore = new NpcDecisionStore(logger);
                    decisionRecorder = new NpcDecisionRecorder(runtimeIdAllocator, simulationTime, recordSequence, decisionStore, logger);
                    npcChronicleService = new NpcChronicleService(decisionStore, domainEventStore);
                    npcChronicleFormatter = new NpcChronicleFormatter(ResolveNpcDisplayName, ResolveLocationDisplayName, ResolveItemDisplayName, ResolveActionDisplayName);
                    scheduledDirectiveStore = new ScheduledDirectiveStore(simulationTime, logger);
                    runtimeIdentityRegistry = new RuntimeIdentityRegistry(logger);
                    spatialNetwork = new SpatialNetworkRuntime(runtimeIdentityRegistry, logger);
                    break;
                case "p9.genesis.authored-world/v1":
                    CityRuntimeList.Clear();
                    cityRuntimesByDefinition.Clear();
                    cityRuntimeByLocation.Clear();
                    CreateCityRuntimes();
                    explorableSiteStore = new ExplorableSiteStore();
                    explorableSiteRuntimesByDefinition.Clear();
                    CreateExplorableSiteRuntimes();
                    CreateSpatialRoutes();
                    break;
                case SimulationGenesisPipeline.GeographyStageId:
                    ComposeAuthoredGeography();
                    break;
                case "p9.genesis.authored-actors/v1":
                    NpcRuntimeList.Clear();
                    npcRuntimesByDefinition.Clear();
                    CreateNpcRuntimes();
                    CreateScheduledDirectives();
                    scheduledDirectiveSystem = new ScheduledDirectiveSystem(scheduledDirectiveStore, runtimeIdentityRegistry, logger);
                    economyTransactionService = new EconomyTransactionService();
                    expeditionStore = new ExpeditionStore();
                    break;
                case "p9.genesis.validate-profile/v1":
                    RebuildSystems(effectiveConfiguration);
                    BootstrapInitialSpatialKnowledge();
                    BootstrapInitialExplorableSiteKnowledge();
                    BootstrapInitialCommercialKnowledge();
                    InitializeJusticeState();
                    simulationRuntime = new SimulationRuntime(
                        simulationTime: simulationTime, cities: CityRuntimeList, npcRuntimes: NpcRuntimeList,
                        configuredActions: ConfiguredActions, scheduledDirectiveSystem: scheduledDirectiveSystem,
                        justiceSystem: justiceSystem, crimeSystem: crimeSystem, npcDecisionSystem: npcDecisionSystem,
                        travelSystem: travelSystem, travelPartySystem: travelPartySystem, merchantSystem: merchantSystem,
                        commercialKnowledgeSharingSystem: commercialKnowledgeSharingSystem, decisionRecorder: decisionRecorder,
                        logger: logger, explorableSiteStore: explorableSiteStore,
                        explorableSiteKnowledgeSystem: explorableSiteKnowledgeSystem, expeditionSystem: expeditionSystem,
                        configuration: effectiveConfiguration, randomSource: authoritativeRandomSource,
                        calendarDefinition: calendarDefinition,
                        spatialAuthorityStore: genesisSpatialAuthority);
                    ValidateCandidateProfile();
                    break;
                case "p9.genesis.publish/v1":
                    publishedComposition = new SimulationBootstrapComposition(
                        new SimulationGenesisManifest(simulationConfig, effectiveConfiguration, calendarDefinition, profileFingerprint, profileProvenanceRecords), simulationTime, calendarDefinition, spatialNetwork, domainEventStore,
                        historyStore, scheduledDirectiveStore, decisionStore, decisionRecorder, recordSequence, economyTransactionService, npcChronicleService,
                        npcChronicleFormatter, travelPartyStore, travelPartySystem, simulationRuntime,
                        explorableSiteStore, expeditionStore, expeditionSystem);
                    break;
                default:
                    throw new System.InvalidOperationException("Undeclared authored genesis stage: " + stageId);
            }
            stageCompleted?.Invoke(stageId);
        }, simulationConfig != null && simulationConfig.useAuthoredGeographyProfile);
    }

    private void ComposeAuthoredGeography()
    {
        SimulationConfigData input = simulationConfig;
        decimal scale = decimal.Parse(input.authoredDistancePerNeighborStep, NumberStyles.Number, CultureInfo.InvariantCulture);
        var definition = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext(input.authoredScaleConventionId, input.authoredScaleSourceIdentity,
                input.authoredScaleSourceVersion, scale, input.authoredScaleUnit),
            new[] { new HexRecord(new HexId(input.authoredHexId), new HexCoordinate(input.authoredHexQ, input.authoredHexR),
                new TerrainReference(new TerrainDefinitionId(input.authoredTerrainDefinitionId), input.authoredTerrainRevisionToken)) },
            new[] { new LocationRecord(new LocationId(input.authoredLocationId), new HexId(input.authoredHexId)) });
        if (!genesisSpatialAuthority.TryComposeGeography(definition, out SpatialAuthorityFailure failure))
            throw new System.InvalidOperationException("Authored geography stage failed atomically: " + failure);
    }

    public string GetFullLog()
    {
        return FullLog;
    }

    private void ValidateCandidateProfile()
    {
        if (CityRuntimeList.Count != simulationConfig.Cities.Count
            || NpcRuntimeList.Count != simulationConfig.Npcs.Count
            || explorableSiteStore.Sites.Count != simulationConfig.ExplorableSites.Count
            || scheduledDirectiveStore.Directives.Count != simulationConfig.ScheduledDirectives.Count)
            throw new System.InvalidOperationException("Authored bootstrap output inventory does not match its selected profile.");

        int expectedRouteCount = 0;
        foreach (CityData city in simulationConfig.Cities)
        {
            expectedRouteCount += city.connections.Count;
            CityRuntime runtime = GetSingleCityRuntimeByDefinition(city);
            if (runtime == null || runtime.CurrentPopulation != city.initialPopulation
                || runtime.CityData != city || runtime.Market.Items.Count != city.marketItems.Count
                || runtime.CityData.productionConfigs.Count != city.productionConfigs.Count
                || runtime.MarketCounterparty.LiquidityMode != city.MarketLiquidity.liquidityMode
                || runtime.PopulationEconomy.PaymentMode != city.PopulationConsumption.paymentMode
                || runtime.MarketCounterparty.CounterpartyRuntimeId != runtime.RuntimeId
                || (city.MarketLiquidity.liquidityMode == MarketLiquidityMode.AccountBacked
                    && (runtime.MarketCounterparty.MoneyAccount == null
                        || runtime.MarketCounterparty.MoneyAccount.Balance != city.MarketLiquidity.initialPurchasingPower))
                || (city.PopulationConsumption.paymentMode == ConsumptionPaymentMode.AccountBacked
                    && (runtime.PopulationEconomy.MoneyAccount == null
                        || runtime.PopulationEconomy.MoneyAccount.Balance != city.PopulationConsumption.initialPurchasingPower)))
                throw new System.InvalidOperationException("Authored city owner output is incomplete.");
            for (int i = 0; i < city.marketItems.Count; i++)
            {
                MarketItemConfig input = city.marketItems[i];
                MarketItemRuntime output = runtime.Market.Items[i];
                if (input.item != output.Item || input.initialAmount != output.Amount || input.desiredAmount != output.DesiredAmount)
                    throw new System.InvalidOperationException("Authored city market output does not match its selected row.");
            }
            for (int i = 0; i < city.productionConfigs.Count; i++)
            {
                CityProductionConfig input = city.productionConfigs[i];
                CityProductionConfig output = runtime.CityData.productionConfigs[i];
                if (input.item != output.item || input.amountPerDay != output.amountPerDay)
                    throw new System.InvalidOperationException("Authored city production inputs do not match their selected rows.");
            }
        }
        expectedRouteCount += simulationConfig.ExplorableSites.Count * 2;
        if (spatialNetwork.Routes.Count != expectedRouteCount)
            throw new System.InvalidOperationException("Authored spatial route output inventory is incomplete.");

        foreach (NpcSimulationConfig row in simulationConfig.Npcs)
        {
            NpcRuntime runtime = GetSingleNpcRuntimeByDefinition(row.npc);
            CityRuntime expectedCity = GetSingleCityRuntimeByDefinition(row.startingCity);
            var expectedStatuses = new System.Collections.Generic.List<NpcStatusData>(row.npc.statusPadrao);
            bool hasActiveWarrant = false;
            foreach (InitialWantedRecordConfig warrant in simulationConfig.InitialWarrants)
                if (warrant.target == row.npc) { hasActiveWarrant = true; break; }
            if (hasActiveWarrant && simulationConfig.wantedStatus != null && !expectedStatuses.Contains(simulationConfig.wantedStatus))
                expectedStatuses.Add(simulationConfig.wantedStatus);
            if (runtime == null || runtime.NpcData != row.npc || runtime.Money != row.initialMoney
                || runtime.CurrentCity != expectedCity || runtime.CurrentLocation != expectedCity.Location
                || runtime.CurrentStatus.Count != expectedStatuses.Count)
                throw new System.InvalidOperationException("Authored NPC owner output is incomplete.");
            for (int statusIndex = 0; statusIndex < expectedStatuses.Count; statusIndex++)
                if (runtime.CurrentStatus[statusIndex] != expectedStatuses[statusIndex])
                    throw new System.InvalidOperationException("Authored NPC status output does not preserve authored/domain order.");
            // Job, default actions, traits and capability values remain authored inputs on the
            // preserved NpcData owner; P9-A does not copy them into a second runtime owner.
            if (runtime.NpcData.job != row.npc.job
                || runtime.NpcData.acoesPadrao.Count != row.npc.acoesPadrao.Count
                || runtime.NpcData.traits.Count != row.npc.traits.Count
                || runtime.NpcData.capabilityValues.Count != row.npc.capabilityValues.Count)
                throw new System.InvalidOperationException("Authored NPC definition inputs were not preserved by their owner.");
            for (int actionIndex = 0; actionIndex < row.npc.acoesPadrao.Count; actionIndex++)
                if (runtime.NpcData.acoesPadrao[actionIndex].action != row.npc.acoesPadrao[actionIndex].action
                    || runtime.NpcData.acoesPadrao[actionIndex].baseUtility != row.npc.acoesPadrao[actionIndex].baseUtility)
                    throw new System.InvalidOperationException("Authored NPC default actions were not preserved by their owner.");
            var expectedInventoryItems = new System.Collections.Generic.HashSet<ItemData>();
            var expectedInventory = new InventoryRuntime();
            foreach (NpcInitialInventoryItemConfig item in row.InitialInventory)
            {
                if (item.amount > 0) expectedInventoryItems.Add(item.item);
                expectedInventory.AddItem(item.item, item.amount, item.averageUnitCost);
            }
            var validatedInventoryItems = new System.Collections.Generic.HashSet<ItemData>();
            foreach (NpcInitialInventoryItemConfig item in row.InitialInventory)
            {
                if (!validatedInventoryItems.Add(item.item)) continue;
                int expectedAmount = 0;
                foreach (NpcInitialInventoryItemConfig candidate in row.InitialInventory)
                    if (candidate.item == item.item) expectedAmount = checked(expectedAmount + candidate.amount);
                if (runtime.Inventory.GetAmount(item.item) != expectedAmount)
                    throw new System.InvalidOperationException("Authored NPC inventory output does not match its selected rows.");
                if (runtime.Inventory.GetAverageUnitCost(item.item) != expectedInventory.GetAverageUnitCost(item.item))
                    throw new System.InvalidOperationException("Authored NPC inventory costs do not match their ordered selected rows.");
            }
            if (runtime.Inventory.Items.Count != expectedInventoryItems.Count)
                throw new System.InvalidOperationException("Authored NPC inventory contains unexpected output rows.");
            foreach (ExplorableSiteData known in row.InitialKnownExplorableSites)
            {
                if (!explorableSiteRuntimesByDefinition.TryGetValue(known, out List<ExplorableSiteRuntime> sites)
                    || sites.Count != 1 || !runtime.ExplorableSiteKnowledge.KnowsSite(sites[0].RuntimeId))
                    throw new System.InvalidOperationException("Authored initial Knowledge output is incomplete.");
                if (!runtime.ExplorableSiteKnowledge.TryGetObservation(sites[0].RuntimeId, out ExplorableSiteKnowledgeObservation observation)
                    || observation.Source != ExplorableSiteKnowledgeSource.InitialScenarioKnowledge
                    || observation.ObservedDay != 0 || observation.ReceivedDay != 0
                    || observation.LocationRuntimeId != sites[0].Location.RuntimeId)
                    throw new System.InvalidOperationException("Authored initial Knowledge provenance does not match its selected site.");
            }
            int expectedKnowledgeCount = new System.Collections.Generic.HashSet<ExplorableSiteData>(row.InitialKnownExplorableSites).Count;
            if (runtime.ExplorableSiteKnowledge.Observations.Count != expectedKnowledgeCount)
                throw new System.InvalidOperationException("Authored initial Knowledge contains unexpected observations.");
        }

        foreach (InitialWantedRecordConfig row in simulationConfig.InitialWarrants)
        {
            NpcRuntime target = GetSingleNpcRuntimeByDefinition(row.target);
            List<WantedRecordRuntime> records = justiceSystem?.GetActiveWarrants(target);
            CityRuntime city = GetSingleCityRuntimeByDefinition(row.city);
            WantedRecordRuntime record = records?.Find(value => value.City == city);
            float expectedBounty = 0f;
            int expectedSentenceDays = 0;
            foreach (InitialWantedRecordConfig candidate in simulationConfig.InitialWarrants)
                if (candidate.target == row.target && candidate.city == row.city)
                {
                    expectedBounty += candidate.bounty;
                    expectedSentenceDays += candidate.sentenceDays;
                }
            if (record == null || record.Bounty != expectedBounty || record.SentenceDays != expectedSentenceDays)
                throw new System.InvalidOperationException("Authored warrant owner output is incomplete.");
        }
        if (historyStore.HistoricalEvents.Count != 0)
            throw new System.InvalidOperationException("Genesis must not create simulated history before the first boundary.");
    }

    public bool TryGetNpcRuntime(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (publishedComposition != null && runtimeIdentityRegistry != null)
        {
            return runtimeIdentityRegistry.TryGetNpc(runtimeId, out npcRuntime);
        }

        npcRuntime = null;
        logger?.LogWarning($"NPC runtime resolution failed: identity registry is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public bool TryGetCityRuntime(string runtimeId, out CityRuntime cityRuntime)
    {
        if (publishedComposition != null && runtimeIdentityRegistry != null)
        {
            return runtimeIdentityRegistry.TryGetCity(runtimeId, out cityRuntime);
        }

        cityRuntime = null;
        logger?.LogWarning($"City runtime resolution failed: identity registry is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public bool TryGetSpatialLocation(string runtimeId, out SpatialLocationRuntime location)
    {
        if (publishedComposition != null && spatialNetwork != null)
        {
            return spatialNetwork.TryGetLocation(runtimeId, out location);
        }

        location = null;
        logger?.LogWarning($"Location runtime resolution failed: spatial network is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public bool TryGetSpatialRoute(string runtimeId, out SpatialRouteRuntime route)
    {
        if (publishedComposition != null && spatialNetwork != null)
        {
            return spatialNetwork.TryGetRoute(runtimeId, out route);
        }

        route = null;
        logger?.LogWarning($"Route runtime resolution failed: spatial network is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public bool TryGetExplorableSiteRuntime(string runtimeId, out ExplorableSiteRuntime siteRuntime)
    {
        if (publishedComposition != null && runtimeIdentityRegistry != null)
        {
            return runtimeIdentityRegistry.TryGetExplorableSite(runtimeId, out siteRuntime);
        }

        siteRuntime = null;
        logger?.LogWarning($"ExplorableSite runtime resolution failed: identity registry is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public IReadOnlyList<NpcChronicleEntry> GetNpcChronicle(string npcRuntimeId)
    {
        return publishedComposition != null && npcChronicleService != null
            ? npcChronicleService.GetChronicle(npcRuntimeId)
            : System.Array.Empty<NpcChronicleEntry>();
    }

    private CalendarDefinition ResolveCalendarDefinition()
    {
        CalendarDefinition configuredCalendar = simulationConfig != null ? simulationConfig.Calendar : null;
        if (configuredCalendar == null)
        {
            return CalendarDefinition.CreateDefault();
        }
        if (!configuredCalendar.TryValidate(out string diagnostic))
            throw new System.InvalidOperationException("Authored CalendarDefinition is invalid: " + diagnostic);
        return configuredCalendar;
    }

    private EffectiveSimulationConfiguration ResolveRuntimeConfiguration()
    {
        if (simulationConfig == null)
        {
            return SimulationConfigurationResolver.ResolveOrThrow();
        }

        return SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: simulationConfig.CreateConfigurationOverrides());
    }

    private void Simulate(int daysToSimulate)
    {
        if (publishedComposition == null || simulationRuntime == null)
        {
            return;
        }

        for (int i = 0; i < daysToSimulate; i++)
        {
            simulationRuntime.AdvanceDay();
            AppendNpcStateSummary();
            AppendEconomySnapshotIfNeeded(false);
        }

        if (simulationConfig != null
            && simulationConfig.includeEconomySnapshots == true
            && daysToSimulate >= Mathf.Max(1, simulationConfig.economySnapshotIntervalDays))
        {
            AppendEconomySnapshotIfNeeded(true);
        }

        SaveSimulationLog();
    }

    private void AppendScenarioDiagnostics(EffectiveSimulationConfiguration configuration)
    {
        if (simulationConfig == null || simulationConfig.includeEconomySnapshots == false)
        {
            return;
        }

        logger.AddReportLine("Max Merchant Trade Amount: " + configuration.MerchantTrade.MaxTradeAmount);
        logger.AddReportLine("Travel Cost Per Day: " + configuration.Travel.TravelCostPerDay.ToString("0.##"));
        logger.AddReportLine("Merchant Trade Repositioning: " + (configuration.MerchantTrade.AllowAutonomousTradeRepositioning ? "ON" : "OFF"));

        if (simulationConfig.useFixedSimulationSeed == true)
        {
            logger.AddReportLine("Simulation Seed: " + simulationConfig.simulationSeed);
        }

        logger.AddReportLine(string.Empty);
        logger.AddReportLine("ROADS");

        HashSet<string> roadKeys = new HashSet<string>();

        foreach (CityData city in simulationConfig.Cities)
        {
            if (city == null || city.connections == null)
            {
                continue;
            }

            foreach (CityConnection connection in city.connections)
            {
                if (connection == null || connection.destination == null || connection.destination == city)
                {
                    continue;
                }

                string roadKey = CreateRoadKey(city, connection.destination);

                if (roadKeys.Add(roadKey) == true)
                {
                    logger.AddReportLine($"{city.cityName} <-> {connection.destination.cityName} : {connection.travelDays}d");
                }
            }
        }

        logger.AddReportLine(string.Empty);
    }

    private string CreateRoadKey(CityData first, CityData second)
    {
        string firstKey = !string.IsNullOrEmpty(first.id) ? first.id : first.cityName;
        string secondKey = !string.IsNullOrEmpty(second.id) ? second.id : second.cityName;
        return string.CompareOrdinal(firstKey, secondKey) < 0 ? firstKey + "|" + secondKey : secondKey + "|" + firstKey;
    }

    private void AppendEconomySnapshotIfNeeded(bool forceFinal)
    {
        if (simulationConfig == null || simulationConfig.includeEconomySnapshots == false || CurrentDay <= 0 || CurrentDay == lastEconomySnapshotDay)
        {
            return;
        }

        int interval = Mathf.Max(1, simulationConfig.economySnapshotIntervalDays);

        if (CurrentDay % interval != 0 && forceFinal == false)
        {
            return;
        }

        logger.AddReportLine(string.Empty);
        logger.AddReportLine("=== ECONOMY SNAPSHOT - DAY " + CurrentDay + " ===");

        foreach (CityRuntime cityRuntime in CityRuntimeList)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            logger.AddReportLine(string.Empty);
            logger.AddReportLine(cityRuntime.CityName);

            foreach (MarketItemRuntime marketItem in cityRuntime.Market.Items)
            {
                if (marketItem == null || marketItem.Item == null)
                {
                    continue;
                }

                logger.AddReportLine($"{marketItem.Item.itemName}: stock {marketItem.Amount} / desired {marketItem.DesiredAmount} | ${marketItem.CurrentPrice:0.##}");
            }
        }

        lastEconomySnapshotDay = CurrentDay;
    }

    private void AppendNpcStateSummary()
    {
        logger.AddReportLine(string.Empty);
        logger.AddReportLine($"--- ESTADO AO FIM DO DIA {CurrentDay} ---");

        foreach (NpcRuntime npcRuntime in NpcRuntimeList)
        {
            if (npcRuntime != null)
            {
                logger.AddReportLine(CreateNpcStateLine(npcRuntime));
            }
        }
    }

    private string CreateNpcStateLine(NpcRuntime npcRuntime)
    {
        string location = CreateNpcLocationText(npcRuntime);
        string status = CreateNpcStatusText(npcRuntime);
        string line = $"{npcRuntime.NpcName} | {location} | ${npcRuntime.Money:0.##} | {status}";

        if (npcRuntime.NpcData != null && npcRuntime.NpcData.job != null && npcRuntime.NpcData.job.jobType == NpcJobType.Merchant)
        {
            if (npcRuntime.MerchantTradePlan.IsActive == true && npcRuntime.MerchantTradePlan.Item != null)
            {
                line += $" | Trade: {npcRuntime.MerchantTradePlan.RemainingAmount} {npcRuntime.MerchantTradePlan.Item.itemName}";
            }
            else if (npcRuntime.NpcData.job.merchantBehavior == MerchantBehavior.Local)
            {
                line += $" | Estoque: {CreateLocalInventoryText(npcRuntime)}";
            }
        }

        string warrantText = CreateWarrantText(npcRuntime);

        if (string.IsNullOrEmpty(warrantText) == false)
        {
            line += " | Mandados: " + warrantText;
        }

        return line;
    }

    private string CreateNpcLocationText(NpcRuntime npcRuntime)
    {
        if (npcRuntime.IsTraveling == true)
        {
            string destinationName = npcRuntime.DestinationCity != null
                ? npcRuntime.DestinationCity.CityName
                : ResolveLocationDisplayName(npcRuntime.DestinationLocation?.RuntimeId) ?? "destino desconhecido";
            string dayText = npcRuntime.TravelDaysRemaining == 1 ? "dia restante" : "dias restantes";
            return $"VIAJANDO -> {destinationName} | {npcRuntime.TravelDaysRemaining} {dayText}";
        }

        return npcRuntime.CurrentCity != null ? npcRuntime.CurrentCity.CityName : "SEM CIDADE";
    }

    private string CreateNpcStatusText(NpcRuntime npcRuntime)
    {
        if (npcRuntime.CurrentStatus == null || npcRuntime.CurrentStatus.Count == 0)
        {
            return "SEM STATUS";
        }

        StringBuilder statusBuilder = new StringBuilder();

        foreach (NpcStatusData status in npcRuntime.CurrentStatus)
        {
            if (status == null)
            {
                continue;
            }

            if (statusBuilder.Length > 0)
            {
                statusBuilder.Append(", ");
            }

            statusBuilder.Append(string.IsNullOrEmpty(status.statusName) == true ? "STATUS DESCONHECIDO" : status.statusName);
        }

        return statusBuilder.Length > 0 ? statusBuilder.ToString() : "SEM STATUS";
    }

    private string CreateLocalInventoryText(NpcRuntime npcRuntime)
    {
        StringBuilder inventoryBuilder = new StringBuilder();
        int itemTypeCount = 0;

        foreach (InventoryItemRuntime inventoryItem in npcRuntime.Inventory.Items)
        {
            if (inventoryItem == null || inventoryItem.Item == null || inventoryItem.Amount <= 0)
            {
                continue;
            }

            if (inventoryBuilder.Length > 0)
            {
                inventoryBuilder.Append(", ");
            }

            inventoryBuilder.Append(inventoryItem.Amount);
            inventoryBuilder.Append(" ");
            inventoryBuilder.Append(inventoryItem.Item.itemName);

            if (inventoryItem.AverageUnitCost > 0f)
            {
                inventoryBuilder.Append($" @{inventoryItem.AverageUnitCost:0.##}");
            }

            itemTypeCount++;

            if (itemTypeCount >= 3)
            {
                break;
            }
        }

        return inventoryBuilder.Length > 0 ? inventoryBuilder.ToString() : "vazio";
    }

    private string CreateWarrantText(NpcRuntime npcRuntime)
    {
        if (justiceSystem == null)
        {
            return string.Empty;
        }

        List<WantedRecordRuntime> warrants = justiceSystem.GetActiveWarrants(npcRuntime);

        if (warrants == null || warrants.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder warrantBuilder = new StringBuilder();

        foreach (WantedRecordRuntime warrant in warrants)
        {
            if (warrant == null)
            {
                continue;
            }

            if (warrantBuilder.Length > 0)
            {
                warrantBuilder.Append(", ");
            }

            string cityName = warrant.City != null ? warrant.City.CityName : "cidade desconhecida";
            warrantBuilder.Append($"{cityName}({warrant.Bounty:0.##})");
        }

        return warrantBuilder.ToString();
    }

    private void SaveSimulationLog()
    {
        string simulationName = simulationConfig != null ? simulationConfig.simulationName : "Simulation";
        logger.SaveToFile(simulationName + "-Run.txt");
    }

    private List<NpcRuntime> NpcRuntimeList
    {
        get
        {
            if (npcRuntimeList == null)
            {
                npcRuntimeList = new List<NpcRuntime>();
            }

            return npcRuntimeList;
        }
    }

    private List<CityRuntime> CityRuntimeList
    {
        get
        {
            if (cityRuntimeList == null)
            {
                cityRuntimeList = new List<CityRuntime>();
            }

            return cityRuntimeList;
        }
    }

    private List<NpcActionData> ConfiguredActions
    {
        get
        {
            if (simulationConfig == null)
            {
                return null;
            }

            return simulationConfig.Actions;
        }
    }

    private void CreateCityRuntimes()
    {
        if (simulationConfig == null)
        {
            logger.LogWarning("Nenhum SimulationConfigData configurado. A simulacao iniciara sem cidades nem NPCs.");
            return;
        }

        List<CityData> orderedCities = new List<CityData>(simulationConfig.Cities);
        orderedCities.Sort((left, right) => System.StringComparer.Ordinal.Compare(left?.DefinitionId, right?.DefinitionId));
        foreach (CityData cityData in orderedCities)
        {
            if (cityData == null)
            {
                continue;
            }

            SpatialLocationRuntime location = new SpatialLocationRuntime(runtimeIdAllocator.AllocateLocationId());

            if (spatialNetwork.RegisterLocation(location) == false)
            {
                continue;
            }

            CityRuntime cityRuntime = new CityRuntime(runtimeIdAllocator.AllocateCityId(), cityData, location, logger);

            if (runtimeIdentityRegistry.RegisterCity(cityRuntime) == false)
            {
                continue;
            }

            CityRuntimeList.Add(cityRuntime);
            AddCityRuntimeByDefinition(cityData, cityRuntime);
            cityRuntimeByLocation.Add(location, cityRuntime);
        }
    }

    private void CreateSpatialRoutes()
    {
        foreach (CityRuntime originCity in CityRuntimeList)
        {
            if (originCity == null || originCity.CityData == null || originCity.CityData.connections == null)
            {
                continue;
            }

            List<CityConnection> orderedConnections = new List<CityConnection>(originCity.CityData.connections);
            orderedConnections.Sort((left, right) =>
            {
                int destination = System.StringComparer.Ordinal.Compare(left?.destination?.DefinitionId, right?.destination?.DefinitionId);
                return destination != 0 ? destination : System.Nullable.Compare(left?.travelDays, right?.travelDays);
            });
            foreach (CityConnection connection in orderedConnections)
            {
                if (connection == null)
                {
                    logger.LogWarning($"Skipping invalid spatial route from city '{originCity.CityName}': connection is null.");
                    continue;
                }

                if (connection.destination == null)
                {
                    logger.LogWarning($"Skipping invalid spatial route from city '{originCity.CityName}': destination definition is null.");
                    continue;
                }

                CityRuntime destinationCity = GetSingleCityRuntimeByDefinition(connection.destination);

                if (destinationCity == null)
                {
                    continue;
                }

                if (destinationCity == originCity)
                {
                    logger.LogWarning($"Skipping invalid self route for city '{originCity.CityName}'.");
                    continue;
                }

                SpatialRouteRuntime route = new SpatialRouteRuntime(
                    runtimeIdAllocator.AllocateRouteId(),
                    originCity.Location,
                    destinationCity.Location,
                    connection.travelDays);
                spatialNetwork.RegisterRoute(route);
            }
        }
    }

    private void CreateExplorableSiteRuntimes()
    {
        if (simulationConfig == null)
        {
            return;
        }

        List<ExplorableSiteConfig> orderedSites = new List<ExplorableSiteConfig>(simulationConfig.ExplorableSites);
        orderedSites.Sort((left, right) => System.StringComparer.Ordinal.Compare(left?.site?.DefinitionId, right?.site?.DefinitionId));
        foreach (ExplorableSiteConfig siteConfig in orderedSites)
        {
            if (siteConfig == null)
            {
                logger.LogWarning("Skipping null explorable site configuration.");
                continue;
            }

            if (siteConfig.site == null)
            {
                logger.LogWarning("Skipping explorable site configuration: site definition is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(siteConfig.site.DefinitionId) == true)
            {
                logger.LogWarning("Skipping explorable site configuration: site DefinitionId is empty.");
                continue;
            }

            ExplorableSiteRuntime siteRuntime;

            try
            {
                SpatialLocationRuntime location = new SpatialLocationRuntime(runtimeIdAllocator.AllocateLocationId());
                siteRuntime = new ExplorableSiteRuntime(
                    runtimeIdAllocator,
                    siteConfig.site,
                    location);

                if (runtimeIdentityRegistry.RegisterExplorableSite(siteRuntime) == false)
                {
                    logger.LogWarning($"Skipping explorable site '{siteConfig.site.DefinitionId}': runtime identity registration failed.");
                    continue;
                }

                if (spatialNetwork.RegisterLocation(location) == false)
                {
                    logger.LogWarning($"Skipping explorable site '{siteConfig.site.DefinitionId}': location registration failed.");
                    continue;
                }

                if (explorableSiteStore.Add(siteRuntime) == false)
                {
                    logger.LogWarning($"Skipping explorable site '{siteConfig.site.DefinitionId}': site store registration failed.");
                    continue;
                }

                AddExplorableSiteRuntimeByDefinition(siteConfig.site, siteRuntime);
                CreateExplorableSiteRoutes(siteConfig, siteRuntime);
            }
            catch (System.ArgumentException exception)
            {
                logger.LogWarning($"Skipping explorable site '{siteConfig.site.DefinitionId}': {exception.Message}");
            }
            catch (System.InvalidOperationException exception)
            {
                logger.LogWarning($"Skipping explorable site '{siteConfig.site.DefinitionId}': {exception.Message}");
            }
        }
    }

    private void CreateExplorableSiteRoutes(ExplorableSiteConfig siteConfig, ExplorableSiteRuntime siteRuntime)
    {
        if (siteConfig == null || siteRuntime == null || siteConfig.anchorCity == null)
        {
            return;
        }

        CityRuntime anchorCity = GetSingleCityRuntimeByDefinition(siteConfig.anchorCity);

        if (anchorCity == null || anchorCity.Location == null || siteRuntime.Location == null)
        {
            logger.LogWarning($"Explorable site '{siteRuntime.DefinitionId}' remains isolated because its anchor city could not be resolved.");
            return;
        }

        int travelDays = siteConfig.travelDaysFromAnchor;
        SpatialRouteRuntime anchorToSite = new SpatialRouteRuntime(
            runtimeIdAllocator.AllocateRouteId(),
            anchorCity.Location,
            siteRuntime.Location,
            travelDays);

        if (spatialNetwork.RegisterRoute(anchorToSite) == false)
        {
            logger.LogWarning($"Could not register route from city '{anchorCity.CityName}' to explorable site '{siteRuntime.DefinitionId}'.");
            return;
        }

        SpatialRouteRuntime siteToAnchor = new SpatialRouteRuntime(
            runtimeIdAllocator.AllocateRouteId(),
            siteRuntime.Location,
            anchorCity.Location,
            travelDays);

        if (spatialNetwork.RegisterRoute(siteToAnchor) == false)
        {
            logger.LogWarning($"Could not register return route from explorable site '{siteRuntime.DefinitionId}' to city '{anchorCity.CityName}'.");
        }
    }

    private void CreateNpcRuntimes()
    {
        if (simulationConfig == null)
        {
            return;
        }

        List<NpcSimulationConfig> orderedNpcs = new List<NpcSimulationConfig>(simulationConfig.Npcs);
        orderedNpcs.Sort((left, right) => System.StringComparer.Ordinal.Compare(left?.npc?.DefinitionId, right?.npc?.DefinitionId));
        foreach (NpcSimulationConfig npcConfig in orderedNpcs)
        {
            if (npcConfig == null || npcConfig.npc == null)
            {
                continue;
            }

            CityRuntime startingCity = GetSingleCityRuntimeByDefinition(npcConfig.startingCity);
            NpcRuntime npcRuntime = new NpcRuntime(runtimeIdAllocator.AllocateNpcId(), npcConfig.npc, startingCity, npcConfig.initialMoney);

            if (runtimeIdentityRegistry.RegisterNpc(npcRuntime) == false)
            {
                startingCity?.RemoveImportantNpc(npcRuntime);
                continue;
            }

            ApplyInitialInventory(npcRuntime, npcConfig);
            NpcRuntimeList.Add(npcRuntime);
            AddNpcRuntimeByDefinition(npcConfig.npc, npcRuntime);
        }
    }

    private void ApplyInitialInventory(NpcRuntime npcRuntime, NpcSimulationConfig npcConfig)
    {
        if (npcRuntime == null || npcConfig == null)
        {
            return;
        }

        foreach (NpcInitialInventoryItemConfig inventoryConfig in npcConfig.InitialInventory)
        {
            if (inventoryConfig == null)
            {
                continue;
            }

            npcRuntime.Inventory.AddItem(inventoryConfig.item, inventoryConfig.amount, inventoryConfig.averageUnitCost);
        }
    }

    private void BootstrapInitialCommercialKnowledge()
    {
        if (merchantSystem == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in NpcRuntimeList)
        {
            merchantSystem.BootstrapInitialKnowledge(npcRuntime);
        }
    }

    private void BootstrapInitialSpatialKnowledge()
    {
        if (spatialNetwork == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in NpcRuntimeList)
        {
            SpatialLocationRuntime startingLocation = npcRuntime?.CurrentCity?.Location;

            if (startingLocation == null)
            {
                continue;
            }

            // Temporary scenario bootstrap: local location, outgoing direct routes and their destinations only.
            npcRuntime.SpatialKnowledge.DiscoverLocation(startingLocation.RuntimeId);

            foreach (SpatialRouteRuntime route in spatialNetwork.GetOutgoingRoutes(startingLocation))
            {
                if (route == null)
                {
                    continue;
                }

                if (explorableSiteStore != null
                    && explorableSiteStore.GetForLocation(route.Destination).Count > 0)
                {
                    continue;
                }

                npcRuntime.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
                npcRuntime.SpatialKnowledge.DiscoverLocation(route.Destination?.RuntimeId);
            }
        }
    }

    private void CreateScheduledDirectives()
    {
        if (simulationConfig == null)
        {
            return;
        }

        foreach (ScheduledDirectiveConfig directiveConfig in simulationConfig.ScheduledDirectives)
        {
            if (directiveConfig == null)
            {
                logger.LogWarning("Skipping null scheduled directive configuration.");
                continue;
            }

            if (directiveConfig.actor == null)
            {
                logger.LogWarning("Skipping scheduled directive configuration: actor definition is null.");
                continue;
            }

            if (directiveConfig.action == null || directiveConfig.action.actionType != NpcActionType.EscapePrison)
            {
                logger.LogWarning("Skipping scheduled directive configuration: EscapePrison requires a matching action definition.");
                continue;
            }

            NpcRuntime actorRuntime = GetSingleNpcRuntimeByDefinition(directiveConfig.actor);

            if (actorRuntime == null)
            {
                continue;
            }

            try
            {
                ScheduledDirective directive = new ScheduledDirective(
                    runtimeIdAllocator.AllocateDirectiveId(),
                    directiveConfig.absoluteDay,
                    directiveConfig.mode,
                    directiveConfig.operation,
                    actorRuntime.RuntimeId,
                    directiveConfig.action);
                scheduledDirectiveStore.Add(directive);
            }
            catch (System.ArgumentException exception)
            {
                logger.LogError("Cannot create scheduled directive: " + exception.Message);
            }
            catch (System.InvalidOperationException exception)
            {
                logger.LogError("Cannot allocate DirectiveId: " + exception.Message);
            }
        }
    }

    private void RebuildSystems(EffectiveSimulationConfiguration configuration)
    {
        logger = logger ?? new SimulationLogger(simulationConfig != null ? simulationConfig.LogSettings : null);
        economyTransactionService = economyTransactionService ?? new EconomyTransactionService();
        travelSystem = new TravelSystem(
            spatialNetwork,
            GetCityRuntimeByLocation,
            configuration.Travel,
            domainEventRecorder,
            logger,
            economyTransactionService);
        travelPartyStore = new TravelPartyStore();
        travelPartySystem = new TravelPartySystem(
            travelPartyStore,
            runtimeIdAllocator,
            runtimeIdentityRegistry,
            travelSystem,
            simulationTime,
            recordSequence,
            domainEventRecorder,
            logger,
            economyTransactionService);
        expeditionSystem = new ExpeditionSystem(
            expeditionStore,
            runtimeIdAllocator,
            runtimeIdentityRegistry,
            explorableSiteStore,
            travelPartySystem,
            travelPartyStore,
            explorableSiteKnowledgeSystem,
            simulationTime,
            domainEventRecorder,
            logger);
        justiceSystem = simulationConfig != null
            ? new JusticeSystem(simulationConfig.freeStatus, simulationConfig.wantedStatus, simulationConfig.arrestedStatus, simulationConfig.hiddenStatus, domainEventRecorder, logger)
            : null;
        crimeSystem = null;
        merchantSystem = null;
        commercialKnowledgeSharingSystem = null;
        actionProviders.Clear();

        if (configuration.MerchantTrade.Enabled == true)
        {
            merchantSystem = new MerchantSystem(
                configuration.MerchantTrade,
                configuration.CommercialKnowledge,
                travelSystem,
                simulationTime,
                decisionRecorder,
                logger,
                economyTransactionService);
            commercialKnowledgeSharingSystem = new CommercialKnowledgeSharingSystem(
                simulationTime,
                configuration.CommercialKnowledge);
        }

        actionProviders.Add(new TravelActionProvider(travelSystem, merchantSystem));

        if (merchantSystem != null)
        {
            actionProviders.Add(merchantSystem);
        }

        if (justiceSystem != null)
        {
            crimeSystem = new CrimeSystem(
                justiceSystem,
                travelSystem,
                simulationConfig.hiddenStatus,
                logger,
                economyTransactionService,
                configuration.Crime,
                authoritativeRandomSource,
                simulationTime);

            // Crime infrastructure owns temporal state such as an existing hidden
            // timer even while the domain is disabled. It is not an action provider
            // until Crime.Enabled is true, so infrastructure availability cannot
            // re-enable normal or autonomous crime origination.
            if (configuration.Crime.Enabled == true)
            {
                actionProviders.Add(crimeSystem);
            }
        }

        if (configuration.GuardCrime.Enabled == true && justiceSystem != null)
        {
            actionProviders.Add(new GuardSystem(
                justiceSystem,
                simulationConfig.hiddenStatus,
                configuration.GuardCrime));
        }

        npcDecisionSystem = new NpcDecisionSystem(actionProviders, authoritativeRandomSource);
    }

    private void InitializeJusticeState()
    {
        if (justiceSystem == null)
        {
            return;
        }

        justiceSystem.CreateInitialWarrants(simulationConfig, GetSingleNpcRuntimeByDefinition, GetSingleCityRuntimeByDefinition);
        justiceSystem.SyncWantedStatuses(NpcRuntimeList);
    }

    private void AddCityRuntimeByDefinition(CityData cityData, CityRuntime cityRuntime)
    {
        if (cityRuntimesByDefinition.TryGetValue(cityData, out List<CityRuntime> runtimes) == false)
        {
            runtimes = new List<CityRuntime>();
            cityRuntimesByDefinition.Add(cityData, runtimes);
        }

        runtimes.Add(cityRuntime);
    }

    private void AddNpcRuntimeByDefinition(NpcData npcData, NpcRuntime npcRuntime)
    {
        if (npcRuntimesByDefinition.TryGetValue(npcData, out List<NpcRuntime> runtimes) == false)
        {
            runtimes = new List<NpcRuntime>();
            npcRuntimesByDefinition.Add(npcData, runtimes);
        }

        runtimes.Add(npcRuntime);
    }

    private void BootstrapInitialExplorableSiteKnowledge()
    {
        if (simulationConfig == null || explorableSiteKnowledgeSystem == null)
        {
            return;
        }

        foreach (NpcSimulationConfig npcConfig in simulationConfig.Npcs)
        {
            if (npcConfig == null || npcConfig.npc == null)
            {
                continue;
            }

            NpcRuntime npcRuntime = GetSingleNpcRuntimeByDefinition(npcConfig.npc);

            if (npcRuntime == null)
            {
                continue;
            }

            foreach (ExplorableSiteData siteDefinition in npcConfig.InitialKnownExplorableSites)
            {
                if (siteDefinition == null)
                {
                    logger.LogWarning("Skipping initial explorable site knowledge: site definition is null.");
                    continue;
                }

                if (explorableSiteRuntimesByDefinition.TryGetValue(
                    siteDefinition,
                    out List<ExplorableSiteRuntime> siteRuntimes) == false
                    || siteRuntimes.Count == 0)
                {
                    logger.LogWarning($"Explorable site definition '{FormatExplorableSiteDefinition(siteDefinition)}' has no runtime instance.");
                    continue;
                }

                if (siteRuntimes.Count > 1)
                {
                    logger.LogError($"Explorable site definition '{FormatExplorableSiteDefinition(siteDefinition)}' is ambiguous: {siteRuntimes.Count} runtime instances exist. Resolve by RuntimeId instead.");
                    continue;
                }

                explorableSiteKnowledgeSystem.RecordInitialScenarioKnowledge(
                    npcRuntime,
                    siteRuntimes[0],
                    simulationTime.AbsoluteDay);
            }
        }
    }

    private void AddExplorableSiteRuntimeByDefinition(
        ExplorableSiteData siteData,
        ExplorableSiteRuntime siteRuntime)
    {
        if (explorableSiteRuntimesByDefinition.TryGetValue(siteData, out List<ExplorableSiteRuntime> runtimes) == false)
        {
            runtimes = new List<ExplorableSiteRuntime>();
            explorableSiteRuntimesByDefinition.Add(siteData, runtimes);
        }

        runtimes.Add(siteRuntime);
    }

    private CityRuntime GetCityRuntimeByLocation(SpatialLocationRuntime location)
    {
        if (location == null)
        {
            return null;
        }

        cityRuntimeByLocation.TryGetValue(location, out CityRuntime cityRuntime);
        return cityRuntime;
    }

    private CityRuntime GetSingleCityRuntimeByDefinition(CityData cityData)
    {
        if (cityData == null)
        {
            return null;
        }

        if (cityRuntimesByDefinition.TryGetValue(cityData, out List<CityRuntime> runtimes) == false || runtimes.Count == 0)
        {
            logger.LogWarning($"City definition '{FormatCityDefinition(cityData)}' has no runtime instance.");
            return null;
        }

        if (runtimes.Count > 1)
        {
            logger.LogError($"City definition '{FormatCityDefinition(cityData)}' is ambiguous: {runtimes.Count} runtime instances exist. Resolve by RuntimeId instead.");
            return null;
        }

        return runtimes[0];
    }

    private NpcRuntime GetSingleNpcRuntimeByDefinition(NpcData npcData)
    {
        if (npcData == null)
        {
            return null;
        }

        if (npcRuntimesByDefinition.TryGetValue(npcData, out List<NpcRuntime> runtimes) == false || runtimes.Count == 0)
        {
            logger.LogWarning($"NPC definition '{FormatNpcDefinition(npcData)}' has no runtime instance.");
            return null;
        }

        if (runtimes.Count > 1)
        {
            logger.LogError($"NPC definition '{FormatNpcDefinition(npcData)}' is ambiguous: {runtimes.Count} runtime instances exist. Resolve by RuntimeId instead.");
            return null;
        }

        return runtimes[0];
    }

    private string ResolveNpcDisplayName(string runtimeId)
    {
        return runtimeIdentityRegistry != null
            && runtimeIdentityRegistry.TryGetNpc(runtimeId, out NpcRuntime npcRuntime) == true
                ? npcRuntime.NpcName
                : null;
    }

    private string ResolveLocationDisplayName(string runtimeId)
    {
        if (spatialNetwork == null
            || spatialNetwork.TryGetLocation(runtimeId, out SpatialLocationRuntime location) == false)
        {
            return null;
        }

        if (cityRuntimeByLocation.TryGetValue(location, out CityRuntime cityRuntime) == true)
        {
            return cityRuntime.CityName;
        }

        if (explorableSiteStore != null)
        {
            foreach (ExplorableSiteRuntime siteRuntime in explorableSiteStore.GetForLocation(location))
            {
                if (siteRuntime?.Definition != null)
                {
                    return string.IsNullOrWhiteSpace(siteRuntime.Definition.DisplayName) == false
                        ? siteRuntime.Definition.DisplayName
                        : siteRuntime.DefinitionId;
                }
            }
        }

        return null;
    }

    private string ResolveItemDisplayName(string definitionId)
    {
        foreach (CityRuntime cityRuntime in CityRuntimeList)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            foreach (MarketItemRuntime marketItem in cityRuntime.Market.Items)
            {
                if (marketItem?.Item != null
                    && string.Equals(marketItem.Item.DefinitionId, definitionId, System.StringComparison.Ordinal) == true)
                {
                    return marketItem.Item.itemName;
                }
            }
        }

        return null;
    }

    private string ResolveActionDisplayName(string definitionId)
    {
        if (simulationConfig == null)
        {
            return null;
        }

        foreach (NpcActionData action in simulationConfig.Actions)
        {
            if (action != null && string.Equals(action.DefinitionId, definitionId, System.StringComparison.Ordinal) == true)
            {
                return action.actionName;
            }
        }

        return null;
    }

    private static string FormatCityDefinition(CityData cityData)
    {
        return string.IsNullOrEmpty(cityData.id) == false ? cityData.id : cityData.cityName;
    }

    private static string FormatNpcDefinition(NpcData npcData)
    {
        return string.IsNullOrEmpty(npcData.id) == false ? npcData.id : npcData.name;
    }

    private static string FormatExplorableSiteDefinition(ExplorableSiteData siteData)
    {
        return string.IsNullOrEmpty(siteData.id) == false ? siteData.id : siteData.siteName;
    }
}
