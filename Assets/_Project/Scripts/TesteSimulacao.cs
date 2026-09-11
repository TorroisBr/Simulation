using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private SimulationConfigData simulationConfig;
    [SerializeField] private int daysToSimulate = 1;
    [SerializeField] private int maxMerchantTradeAmount = 5;
    [SerializeField] private float minimumProfitPerItem = 1f;

    private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
    private List<CityRuntime> cityRuntimeList = new List<CityRuntime>();

    private readonly List<INpcActionProvider> actionProviders = new List<INpcActionProvider>();
    private Dictionary<CityData, List<CityRuntime>> cityRuntimesByDefinition = new Dictionary<CityData, List<CityRuntime>>();
    private Dictionary<NpcData, List<NpcRuntime>> npcRuntimesByDefinition = new Dictionary<NpcData, List<NpcRuntime>>();
    private Dictionary<SpatialLocationRuntime, CityRuntime> cityRuntimeByLocation = new Dictionary<SpatialLocationRuntime, CityRuntime>();
    private RuntimeIdAllocator runtimeIdAllocator;
    private RuntimeIdentityRegistry runtimeIdentityRegistry;
    private SpatialNetworkRuntime spatialNetwork;
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
    private MerchantSystem merchantSystem;
    private SimulationLogger logger;
    private SimulationTime simulationTime = new SimulationTime();
    private CalendarDefinition calendarDefinition = CalendarDefinition.CreateDefault();
    private long lastEconomySnapshotDay;

    public string FullLog => logger != null ? logger.FullLog : string.Empty;
    public SimulationTime SimulationTime => simulationTime;
    public CalendarDefinition Calendar => calendarDefinition;
    public SpatialNetworkRuntime SpatialNetwork => spatialNetwork;
    public DomainEventStore DomainEventStore => domainEventStore;
    public HistoryStore History => historyStore;
    public ScheduledDirectiveStore ScheduledDirectives => scheduledDirectiveStore;
    public NpcDecisionStore Decisions => decisionStore;
    public NpcChronicleService NpcChronicles => npcChronicleService;
    public NpcChronicleFormatter ChronicleFormatter => npcChronicleFormatter;
    public long CurrentDay => simulationTime.AbsoluteDay;
    public SimulationDate CurrentDate => calendarDefinition.GetDate(CurrentDay);

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

    private void InitializeSimulation()
    {
        if (simulationConfig != null && simulationConfig.useFixedSimulationSeed == true)
        {
            Random.InitState(simulationConfig.simulationSeed);
        }

        simulationTime = new SimulationTime();
        lastEconomySnapshotDay = 0;
        logger = new SimulationLogger(simulationConfig != null ? simulationConfig.LogSettings : null);
        logger.BeginSimulation(
            simulationConfig != null ? simulationConfig.simulationName : "Unnamed",
            simulationConfig != null ? simulationConfig.EnabledModules : null,
            simulationConfig != null ? simulationConfig.Cities.Count : 0,
            simulationConfig != null ? simulationConfig.Npcs.Count : 0);
        AppendScenarioDiagnostics();
        calendarDefinition = ResolveCalendarDefinition();
        enabledModules = new SimulationModuleSet(simulationConfig, logger);
        runtimeIdAllocator = new RuntimeIdAllocator();
        recordSequence = new SimulationRecordSequence();
        historyStore = new HistoryStore();
        domainEventStore = new DomainEventStore(historyStore, new HistoryPolicy(), logger);
        domainEventRecorder = new DomainEventRecorder(runtimeIdAllocator, simulationTime, recordSequence, domainEventStore, logger);
        decisionStore = new NpcDecisionStore(logger);
        decisionRecorder = new NpcDecisionRecorder(runtimeIdAllocator, simulationTime, recordSequence, decisionStore, logger);
        npcChronicleService = new NpcChronicleService(decisionStore, domainEventStore);
        npcChronicleFormatter = new NpcChronicleFormatter(
            ResolveNpcDisplayName,
            ResolveLocationDisplayName,
            ResolveItemDisplayName,
            ResolveActionDisplayName);
        scheduledDirectiveStore = new ScheduledDirectiveStore(simulationTime, logger);
        runtimeIdentityRegistry = new RuntimeIdentityRegistry(logger);
        spatialNetwork = new SpatialNetworkRuntime(runtimeIdentityRegistry, logger);

        CityRuntimeList.Clear();
        cityRuntimesByDefinition.Clear();
        cityRuntimeByLocation.Clear();
        CreateCityRuntimes();
        CreateSpatialRoutes();

        NpcRuntimeList.Clear();
        npcRuntimesByDefinition.Clear();
        CreateNpcRuntimes();
        CreateScheduledDirectives();
        scheduledDirectiveSystem = new ScheduledDirectiveSystem(scheduledDirectiveStore, runtimeIdentityRegistry, logger);

        RebuildSystems();
        BootstrapInitialCommercialKnowledge();
        InitializeJusticeState();
    }

    public string GetFullLog()
    {
        return FullLog;
    }

    public bool TryGetNpcRuntime(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (runtimeIdentityRegistry != null)
        {
            return runtimeIdentityRegistry.TryGetNpc(runtimeId, out npcRuntime);
        }

        npcRuntime = null;
        logger?.LogWarning($"NPC runtime resolution failed: identity registry is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public bool TryGetCityRuntime(string runtimeId, out CityRuntime cityRuntime)
    {
        if (runtimeIdentityRegistry != null)
        {
            return runtimeIdentityRegistry.TryGetCity(runtimeId, out cityRuntime);
        }

        cityRuntime = null;
        logger?.LogWarning($"City runtime resolution failed: identity registry is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public bool TryGetSpatialLocation(string runtimeId, out SpatialLocationRuntime location)
    {
        if (spatialNetwork != null)
        {
            return spatialNetwork.TryGetLocation(runtimeId, out location);
        }

        location = null;
        logger?.LogWarning($"Location runtime resolution failed: spatial network is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public bool TryGetSpatialRoute(string runtimeId, out SpatialRouteRuntime route)
    {
        if (spatialNetwork != null)
        {
            return spatialNetwork.TryGetRoute(runtimeId, out route);
        }

        route = null;
        logger?.LogWarning($"Route runtime resolution failed: spatial network is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
        return false;
    }

    public IReadOnlyList<NpcChronicleEntry> GetNpcChronicle(string npcRuntimeId)
    {
        return npcChronicleService != null
            ? npcChronicleService.GetChronicle(npcRuntimeId)
            : System.Array.Empty<NpcChronicleEntry>();
    }

    private CalendarDefinition ResolveCalendarDefinition()
    {
        CalendarDefinition configuredCalendar = simulationConfig != null ? simulationConfig.Calendar : null;
        CalendarDefinition resolvedCalendar = CalendarDefinition.CreateValidatedOrDefault(configuredCalendar, out string diagnostic);

        if (string.IsNullOrEmpty(diagnostic) == false)
        {
            logger.LogWarning(diagnostic);
        }

        return resolvedCalendar;
    }

    private void Simulate(int daysToSimulate)
    {
        for (int i = 0; i < daysToSimulate; i++)
        {
            simulationTime.AdvanceDay();
            logger.BeginDay(CurrentDay);
            BeginSimulationDay();
            scheduledDirectiveSystem?.PrepareDay(CurrentDay);

            if (enabledModules.IsEnabled(SimulationModule.Economy) == true)
            {
                SimulateEconomyDay();
            }

            foreach (NpcRuntime npcRuntime in NpcRuntimeList)
            {
                if (npcRuntime == null)
                {
                    continue;
                }

                if (npcRuntime.IsTraveling == true)
                {
                    TryProcessScheduledDirective(npcRuntime);
                    continue;
                }

                EvaluateStatus(npcRuntime);
                merchantSystem?.AdvanceNpcTradeState(npcRuntime);

                if (TryProcessScheduledDirective(npcRuntime) == true)
                {
                    continue;
                }

                EvaluateAction(npcRuntime);
                TryExecuteCurrentAction(npcRuntime);
            }

            travelSystem.AdvanceTravels(NpcRuntimeList);
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

    private void AppendScenarioDiagnostics()
    {
        if (simulationConfig == null || simulationConfig.includeEconomySnapshots == false)
        {
            return;
        }

        logger.AddReportLine("Max Merchant Trade Amount: " + maxMerchantTradeAmount);
        logger.AddReportLine("Travel Cost Per Day: " + simulationConfig.travelCostPerDay.ToString("0.##"));
        logger.AddReportLine("Merchant Trade Repositioning: " + (simulationConfig.allowMerchantTradeRepositioning ? "ON" : "OFF"));

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
            string destinationName = npcRuntime.DestinationCity != null ? npcRuntime.DestinationCity.CityName : "destino desconhecido";
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

        foreach (CityData cityData in simulationConfig.Cities)
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

            foreach (CityConnection connection in originCity.CityData.connections)
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

    private void CreateNpcRuntimes()
    {
        if (simulationConfig == null)
        {
            return;
        }

        foreach (NpcSimulationConfig npcConfig in simulationConfig.Npcs)
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

    private void RebuildSystems()
    {
        enabledModules = new SimulationModuleSet(simulationConfig, logger);
        logger = logger ?? new SimulationLogger(simulationConfig != null ? simulationConfig.LogSettings : null);
        float travelCostPerDay = simulationConfig != null ? simulationConfig.travelCostPerDay : 0f;
        travelSystem = new TravelSystem(spatialNetwork, GetCityRuntimeByLocation, travelCostPerDay, domainEventRecorder, logger);
        justiceSystem = simulationConfig != null
            ? new JusticeSystem(simulationConfig.freeStatus, simulationConfig.wantedStatus, simulationConfig.arrestedStatus, simulationConfig.hiddenStatus, domainEventRecorder, logger)
            : null;
        crimeSystem = null;
        merchantSystem = null;
        actionProviders.Clear();

        if (enabledModules.IsEnabled(SimulationModule.Merchant) == true)
        {
            bool allowTradeRepositioning = simulationConfig != null && simulationConfig.allowMerchantTradeRepositioning == true;
            CommercialKnowledgeSettings knowledgeSettings = simulationConfig != null ? simulationConfig.CommercialKnowledge : null;
            merchantSystem = new MerchantSystem(
                maxMerchantTradeAmount,
                minimumProfitPerItem,
                allowTradeRepositioning,
                travelSystem,
                simulationTime,
                knowledgeSettings,
                decisionRecorder,
                logger);
        }

        actionProviders.Add(new TravelActionProvider(travelSystem, merchantSystem));

        if (merchantSystem != null)
        {
            actionProviders.Add(merchantSystem);
        }

        if (enabledModules.IsEnabled(SimulationModule.Crime) == true && justiceSystem != null)
        {
            crimeSystem = new CrimeSystem(justiceSystem, travelSystem, simulationConfig.hiddenStatus, logger);
            actionProviders.Add(crimeSystem);
        }

        if (enabledModules.IsEnabled(SimulationModule.GuardCrime) == true && justiceSystem != null)
        {
            actionProviders.Add(new GuardSystem(justiceSystem, simulationConfig.hiddenStatus));
        }

        npcDecisionSystem = new NpcDecisionSystem(actionProviders);
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

    private void BeginSimulationDay()
    {
        if (justiceSystem != null)
        {
            justiceSystem.BeginDay();
        }

        if (crimeSystem != null)
        {
            crimeSystem.AdvanceHiddenStatuses(NpcRuntimeList);
        }

        if (enabledModules.IsEnabled(SimulationModule.GuardCrime) == true && justiceSystem != null)
        {
            justiceSystem.AdvanceSentences(NpcRuntimeList);
        }

        if (justiceSystem != null)
        {
            justiceSystem.SyncWantedStatuses(NpcRuntimeList);
        }

        AdvanceMerchantPlanUrgency();
    }

    private void AdvanceMerchantPlanUrgency()
    {
        foreach (NpcRuntime npcRuntime in NpcRuntimeList)
        {
            if (npcRuntime == null || npcRuntime.IsTraveling == true)
            {
                continue;
            }

            MerchantTradePlanRuntime tradePlan = npcRuntime.MerchantTradePlan;

            if (tradePlan.IsActive == true && tradePlan.TargetCity != null && tradePlan.TargetCity != npcRuntime.CurrentCity)
            {
                tradePlan.IncrementPendingTravelDay();
            }
        }
    }

    private void SimulateEconomyDay()
    {
        foreach (CityRuntime cityRuntime in CityRuntimeList)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            cityRuntime.SimulateProductionDay();
        }

        foreach (CityRuntime cityRuntime in CityRuntimeList)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            cityRuntime.SimulateConsumptionDay();
            cityRuntime.UpdateMarketPrices();
        }
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime chosenAction = npcDecisionSystem.ChooseAction(npcRuntime, ConfiguredActions);
        decisionRecorder?.RecordChosenAction(npcRuntime, chosenAction, NpcDecisionOrigin.Autonomous);
        npcRuntime.SetCurrentActionRuntime(chosenAction);
    }

    private NpcActionResult TryExecuteCurrentAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime actionRuntime = npcRuntime.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npcRuntime.CurrentAction;

        if (action == null)
        {
            return null;
        }

        LogChosenTargetAction(npcRuntime, actionRuntime);
        NpcActionResult actionResult = TryExecuteAction(npcRuntime, actionRuntime, action);

        if (actionResult != null && string.IsNullOrEmpty(actionResult.Message) == false)
        {
            logger.Log(SimulationLogCategory.NpcAction, actionResult.Message);
        }

        if (actionResult != null && actionResult.Success == true)
        {
            ApplySuccessStatusChanges(npcRuntime, actionRuntime, action);
        }

        return actionResult;
    }

    private bool TryProcessScheduledDirective(NpcRuntime npcRuntime)
    {
        if (scheduledDirectiveSystem == null || scheduledDirectiveSystem.TryTakeDirective(npcRuntime, out ScheduledDirective directive) == false)
        {
            return false;
        }

        npcRuntime.SetCurrentActionRuntime(null);

        if (directive.Mode == ScheduledDirectiveMode.RequestAction)
        {
            ProcessRequestedActionDirective(npcRuntime, directive);
        }
        else if (directive.Mode == ScheduledDirectiveMode.ForceOutcome)
        {
            ProcessForcedOutcomeDirective(npcRuntime, directive);
        }
        else
        {
            SkipDirective(directive, "Directive mode is not supported.");
        }

        return true;
    }

    private void ProcessRequestedActionDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        NpcActionRuntime requestedAction = npcDecisionSystem != null
            ? npcDecisionSystem.CreateRequestedAction(npcRuntime, directive.Action)
            : null;

        if (requestedAction == null)
        {
            SkipDirective(directive, "Actor is not in a compatible state for the requested action.");
            return;
        }

        decisionRecorder?.RecordChosenAction(npcRuntime, requestedAction, NpcDecisionOrigin.ScheduledDirective);
        npcRuntime.SetCurrentActionRuntime(requestedAction);
        NpcActionResult result = TryExecuteCurrentAction(npcRuntime);

        if (result != null && result.Success == true)
        {
            directive.MarkSucceeded(CurrentDay);
            return;
        }

        string reason = result != null && string.IsNullOrEmpty(result.Message) == false
            ? result.Message
            : "Requested action was attempted and failed.";
        directive.MarkFailed(CurrentDay, reason);
    }

    private void ProcessForcedOutcomeDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        if (directive.Operation != ScheduledDirectiveOperation.EscapePrison || justiceSystem == null)
        {
            SkipDirective(directive, "Escape domain operation is unavailable.");
            return;
        }

        if (justiceSystem.IsArrested(npcRuntime) == false)
        {
            SkipDirective(directive, "Actor is not arrested; escape outcome is incompatible with current state.");
            return;
        }

        CrimeActionSettings settings = directive.Action != null && directive.Action.crimeSettings != null
            ? directive.Action.crimeSettings
            : new CrimeActionSettings();

        npcRuntime.SetCurrentActionRuntime(new NpcActionRuntime(directive.Action));

        if (justiceSystem.ApplyEscapeSuccess(npcRuntime, settings.escapeBountyPenalty) == false)
        {
            directive.MarkFailed(CurrentDay, "Canonical escape transition rejected the forced outcome.");
            return;
        }

        ApplySuccessStatusChanges(npcRuntime, npcRuntime.CurrentActionRuntime, directive.Action);
        directive.MarkSucceeded(CurrentDay);
    }

    private void SkipDirective(ScheduledDirective directive, string reason)
    {
        if (directive == null)
        {
            return;
        }

        directive.MarkSkipped(CurrentDay, reason);
        logger.LogWarning($"Scheduled directive '{directive.DirectiveId}' was skipped: {reason}");
    }

    private NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        INpcActionProvider actionProvider = action.actionType == NpcActionType.Normal
            ? null
            : npcDecisionSystem.GetProviderForAction(action);

        if (action.actionType != NpcActionType.Normal && actionProvider == null)
        {
            return NpcActionResult.Failed();
        }

        if (RollActionSuccess(action, actionRuntime) == false)
        {
            if (actionProvider is INpcActionFailureHandler failureHandler)
            {
                NpcActionResult failureResult = failureHandler.HandleActionFailure(npcRuntime, actionRuntime);

                if (failureResult != null)
                {
                    return failureResult;
                }
            }

            return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
        }

        if (action.actionType == NpcActionType.Normal)
        {
            return NpcActionResult.Succeeded(CreateNormalActionMessage(npcRuntime, action));
        }

        return actionProvider.TryExecuteAction(npcRuntime, actionRuntime);
    }

    private bool RollActionSuccess(NpcActionData action, NpcActionRuntime actionRuntime)
    {
        if (action == null || action.canFail == false)
        {
            return true;
        }

        float contextualMultiplier = actionRuntime != null ? actionRuntime.SuccessChanceMultiplier : 1f;
        float effectiveChance = Mathf.Clamp01(action.baseSuccessChance * Mathf.Max(0f, contextualMultiplier));
        return Random.value <= effectiveChance;
    }

    private void ApplySuccessStatusChanges(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        ApplyStatusChanges(npcRuntime, action.statusToRemove, action.statusToAdd);

        if (actionRuntime != null && actionRuntime.TargetNpc != null)
        {
            ApplyStatusChanges(actionRuntime.TargetNpc, action.targetStatusToRemove, action.targetStatusToAdd);
        }
    }

    private void ApplyStatusChanges(NpcRuntime npcRuntime, List<NpcStatusData> statusToRemove, List<NpcStatusData> statusToAdd)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (statusToRemove != null)
        {
            foreach (NpcStatusData status in statusToRemove)
            {
                npcRuntime.RemoveStatus(status);
            }
        }

        if (statusToAdd != null)
        {
            foreach (NpcStatusData status in statusToAdd)
            {
                npcRuntime.AddStatus(status);
            }
        }
    }

    private void LogChosenTargetAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || actionRuntime == null || actionRuntime.Action == null || actionRuntime.TargetNpc == null)
        {
            if (npcRuntime == null || actionRuntime == null || actionRuntime.Action == null || actionRuntime.TargetCity == null)
            {
                return;
            }

            logger.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetCity.CityName}.");
            return;
        }

        logger.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetNpc.NpcName}.");
    }

    private string CreateFailureMessage(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";
        string targetName = actionRuntime != null && actionRuntime.TargetNpc != null ? $" {actionRuntime.TargetNpc.NpcName}" : string.Empty;
        string targetCityName = actionRuntime != null && actionRuntime.TargetCity != null ? $" {actionRuntime.TargetCity.CityName}" : string.Empty;
        return $"{actorName} tentou {GetActionName(action)}{targetName}{targetCityName}, mas falhou.";
    }

    private string CreateNormalActionMessage(NpcRuntime npcRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";

        if (action != null && string.IsNullOrEmpty(action.normalActionLogText) == false)
        {
            return $"{actorName} {action.normalActionLogText}";
        }

        return $"{actorName} realizou {GetActionName(action)}.";
    }

    private string GetActionName(NpcActionData action)
    {
        if (action == null)
        {
            return "acao desconhecida";
        }

        return string.IsNullOrEmpty(action.actionName) == false ? action.actionName : action.actionType.ToString();
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
            || spatialNetwork.TryGetLocation(runtimeId, out SpatialLocationRuntime location) == false
            || cityRuntimeByLocation.TryGetValue(location, out CityRuntime cityRuntime) == false)
        {
            return null;
        }

        return cityRuntime.CityName;
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
}
