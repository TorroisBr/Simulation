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
    private ExplorableSiteStore explorableSiteStore;

    private readonly List<INpcActionProvider> actionProviders = new List<INpcActionProvider>();
    private Dictionary<CityData, List<CityRuntime>> cityRuntimesByDefinition = new Dictionary<CityData, List<CityRuntime>>();
    private Dictionary<NpcData, List<NpcRuntime>> npcRuntimesByDefinition = new Dictionary<NpcData, List<NpcRuntime>>();
    private Dictionary<ExplorableSiteData, List<ExplorableSiteRuntime>> explorableSiteRuntimesByDefinition = new Dictionary<ExplorableSiteData, List<ExplorableSiteRuntime>>();
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
    private TravelPartyStore travelPartyStore;
    private TravelPartySystem travelPartySystem;
    private MerchantSystem merchantSystem;
    private CommercialKnowledgeSharingSystem commercialKnowledgeSharingSystem;
    private EconomyTransactionService economyTransactionService;
    private SimulationLogger logger;
    private SimulationRuntime simulationRuntime;
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
    public TravelPartyStore TravelParties => travelPartyStore;
    public TravelPartySystem GroupTravel => travelPartySystem;
    public SimulationRuntime Runtime => simulationRuntime;
    public ExplorableSiteStore ExplorableSites => explorableSiteStore;
    public long CurrentDay => simulationTime.AbsoluteDay;
    public SimulationDate CurrentDate => calendarDefinition.GetDate(CurrentDay);

    public bool TryStartTravelParty(ActionExecutionContext context)
    {
        return simulationRuntime != null
            ? simulationRuntime.TryStartTravelParty(context)
            : travelPartySystem != null && travelPartySystem.TryStartTravelParty(context);
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
        explorableSiteStore = new ExplorableSiteStore();
        explorableSiteRuntimesByDefinition.Clear();
        CreateExplorableSiteRuntimes();
        CreateSpatialRoutes();

        NpcRuntimeList.Clear();
        npcRuntimesByDefinition.Clear();
        CreateNpcRuntimes();
        CreateScheduledDirectives();
        scheduledDirectiveSystem = new ScheduledDirectiveSystem(scheduledDirectiveStore, runtimeIdentityRegistry, logger);
        economyTransactionService = new EconomyTransactionService();

        RebuildSystems();
        BootstrapInitialSpatialKnowledge();
        BootstrapInitialCommercialKnowledge();
        InitializeJusticeState();
        simulationRuntime = new SimulationRuntime(
            simulationTime,
            CityRuntimeList,
            NpcRuntimeList,
            enabledModules.IsEnabled(SimulationModule.Economy),
            ConfiguredActions,
            scheduledDirectiveSystem,
            justiceSystem,
            crimeSystem,
            npcDecisionSystem,
            travelSystem,
            travelPartySystem,
            merchantSystem,
            commercialKnowledgeSharingSystem,
            decisionRecorder,
            logger,
            enabledModules.IsEnabled(SimulationModule.GuardCrime));
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

    public bool TryGetExplorableSiteRuntime(string runtimeId, out ExplorableSiteRuntime siteRuntime)
    {
        if (runtimeIdentityRegistry != null)
        {
            return runtimeIdentityRegistry.TryGetExplorableSite(runtimeId, out siteRuntime);
        }

        siteRuntime = null;
        logger?.LogWarning($"ExplorableSite runtime resolution failed: identity registry is not initialized for RuntimeId '{runtimeId ?? "<empty>"}'.");
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
        if (simulationRuntime == null)
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

    private void CreateExplorableSiteRuntimes()
    {
        if (simulationConfig == null)
        {
            return;
        }

        foreach (ExplorableSiteConfig siteConfig in simulationConfig.ExplorableSites)
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

    private void RebuildSystems()
    {
        enabledModules = new SimulationModuleSet(simulationConfig, logger);
        logger = logger ?? new SimulationLogger(simulationConfig != null ? simulationConfig.LogSettings : null);
        float travelCostPerDay = simulationConfig != null ? simulationConfig.travelCostPerDay : 0f;
        economyTransactionService = economyTransactionService ?? new EconomyTransactionService();
        travelSystem = new TravelSystem(spatialNetwork, GetCityRuntimeByLocation, travelCostPerDay, domainEventRecorder, logger, economyTransactionService);
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
        justiceSystem = simulationConfig != null
            ? new JusticeSystem(simulationConfig.freeStatus, simulationConfig.wantedStatus, simulationConfig.arrestedStatus, simulationConfig.hiddenStatus, domainEventRecorder, logger)
            : null;
        crimeSystem = null;
        merchantSystem = null;
        commercialKnowledgeSharingSystem = null;
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
                logger,
                economyTransactionService);
            commercialKnowledgeSharingSystem = new CommercialKnowledgeSharingSystem(simulationTime, knowledgeSettings);
        }

        actionProviders.Add(new TravelActionProvider(travelSystem, merchantSystem));

        if (merchantSystem != null)
        {
            actionProviders.Add(merchantSystem);
        }

        if (enabledModules.IsEnabled(SimulationModule.Crime) == true && justiceSystem != null)
        {
            crimeSystem = new CrimeSystem(justiceSystem, travelSystem, simulationConfig.hiddenStatus, logger, economyTransactionService);
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
