using System.Collections.Generic;
using UnityEngine;

public sealed class WorldObserverDemoBootstrap : MonoBehaviour
{
    [SerializeField] private WorldObserverCanvasView observerView;
    [SerializeField] private WorldObserverTimeController timeController;
    [SerializeField] private bool initializeDemoWorldOnAwake = true;

    private readonly List<ScriptableObject> runtimeDefinitions = new List<ScriptableObject>();
    private bool initialized;
    private WorldCommandService worldCommandService;
    private PlaceContentStore contentStore;

    public WorldCommandService WorldCommandService => worldCommandService;
    public PlaceContentStore ContentStore => contentStore;
    public GMConsolePanel GmConsolePanel => observerView?.GmConsolePanel;

    public WorldObserverCanvasView ObserverView
    {
        get
        {
            EnsureReady();
            return observerView;
        }
    }

    public WorldObserverTimeController TimeController
    {
        get
        {
            EnsureReady();
            return timeController;
        }
    }

    private void Awake()
    {
        EnsureReady();
        if (initializeDemoWorldOnAwake)
        {
            InitializeDemoWorld();
        }
    }

    private void OnDestroy()
    {
        foreach (ScriptableObject definition in runtimeDefinitions)
        {
            if (definition != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(definition);
                }
                else
                {
                    DestroyImmediate(definition);
                }
            }
        }

        runtimeDefinitions.Clear();
    }

    public void EnsureReady()
    {
        if (observerView == null)
        {
            GameObject viewObject = new GameObject("ObserverView", typeof(RectTransform));
            viewObject.transform.SetParent(transform, false);
            observerView = viewObject.AddComponent<WorldObserverCanvasView>();
        }

        if (timeController == null)
        {
            GameObject controllerObject = new GameObject("ObserverTimeController");
            controllerObject.transform.SetParent(transform, false);
            timeController = controllerObject.AddComponent<WorldObserverTimeController>();
        }

        observerView.EnsureReady();
    }

    public void Initialize(WorldObserverQueryService service, SimulationRuntime runtime = null)
    {
        EnsureReady();
        timeController.Bind(runtime);
        observerView.Initialize(service, timeController);
        initialized = true;
    }

    public void InitializeDemoWorld()
    {
        if (initialized)
        {
            return;
        }

        EnsureReady();
        RuntimeIdentityRegistry identity = new RuntimeIdentityRegistry();
        SpatialLocationRuntime northLocation = new SpatialLocationRuntime("observer-demo-north");
        SpatialLocationRuntime southLocation = new SpatialLocationRuntime("observer-demo-south");
        SpatialLocationRuntime ruinLocation = new SpatialLocationRuntime("observer-demo-ruin-location");
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identity);
        network.RegisterLocation(northLocation);
        network.RegisterLocation(southLocation);
        network.RegisterLocation(ruinLocation);
        network.RegisterRoute(new SpatialRouteRuntime("observer-demo-north-south", northLocation, southLocation, 4));
        network.RegisterRoute(new SpatialRouteRuntime("observer-demo-south-north", southLocation, northLocation, 4));
        network.RegisterRoute(new SpatialRouteRuntime("observer-demo-north-ruin", northLocation, ruinLocation, 3));

        CityData northData = CreateCityDefinition("observer-demo-north-city", "Northwatch");
        CityData southData = CreateCityDefinition("observer-demo-south-city", "Southmere");
        CityRuntime north = new CityRuntime("observer-demo-north-city-runtime", northData, northLocation);
        CityRuntime south = new CityRuntime("observer-demo-south-city-runtime", southData, southLocation);
        identity.RegisterCity(north);
        identity.RegisterCity(south);

        ExplorableSiteData ruinData = ScriptableObject.CreateInstance<ExplorableSiteData>();
        ruinData.id = "observer-demo-ruin";
        ruinData.siteName = "Ancient Ruin";
        ruinData.kind = ExplorableSiteKind.Ruin;
        runtimeDefinitions.Add(ruinData);
        ExplorableSiteRuntime ruin = new ExplorableSiteRuntime("observer-demo-ruin-runtime", ruinData, ruinLocation);
        identity.RegisterExplorableSite(ruin);
        ExplorableSiteStore sites = new ExplorableSiteStore();
        sites.Add(ruin);

        LocalTopologyStore topologies = new LocalTopologyStore(identity);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForExplorableSite(ruin), identity);
        LocalPlaceRuntime gate = new LocalPlaceRuntime("observer-demo-gate", "Broken Gate");
        LocalPlaceRuntime crypt = new LocalPlaceRuntime("observer-demo-crypt", "Sunken Crypt");
        LocalPlaceRuntime vault = new LocalPlaceRuntime("observer-demo-vault", "Sealed Vault");
        topology.AddPlace(gate, null, true);
        topology.AddPlace(crypt, gate);
        topology.AddPlace(vault, crypt);
        topology.AddConnection(new LocalTopologyConnectionRuntime("observer-demo-gate-crypt", gate, crypt, 1f));
        topology.AddConnection(new LocalTopologyConnectionRuntime("observer-demo-crypt-vault", crypt, vault, 1f));
        topologies.Add(topology);

        RuntimeIdAllocator commandRuntimeIds = new RuntimeIdAllocator();
        PlaceContentStore content = new PlaceContentStore(commandRuntimeIds, identity);
        contentStore = content;
        content.GetOrCreate(ruin);
        SimulationTime time = new SimulationTime(12L);
        HistoryStore history = new HistoryStore();
        DomainEventStore events = new DomainEventStore(history, new HistoryPolicy());
        WorldCommandDefinitionCatalog definitions = new WorldCommandDefinitionCatalog();
        ItemData demoResource = ScriptableObject.CreateInstance<ItemData>();
        demoResource.id = "observer-demo-ore";
        demoResource.itemName = "Observer Demo Ore";
        demoResource.basePrice = 10f;
        runtimeDefinitions.Add(demoResource);
        definitions.RegisterItem(demoResource);
        worldCommandService = new WorldCommandService(
            runtimeIdAllocator: commandRuntimeIds,
            identityRegistry: identity,
            definitionResolver: definitions,
            simulationTime: time);
        ConflictResolutionService conflictService = new ConflictResolutionService(
            new ConflictResolver(new WorldObserverDemoCapabilityModel(), new SeededConflictRandomSource(12)),
            null,
            new DomainEventRecorder(commandRuntimeIds, time, new SimulationRecordSequence(), events));
        WorldCommandHandlerRegistration.RegisterCoreHandlers(
            worldCommandService,
            commandRuntimeIds,
            identity,
            content,
            topologies,
            conflictService,
            definitions,
            domainEventStore: events);
        SimulationRuntime runtime = new SimulationRuntime(time, new[] { north, south }, new NpcRuntime[0], economyEnabled: false);
        WorldObserverQueryService query = new WorldObserverQueryService(
            new[] { north, south },
            new NpcRuntime[0],
            network,
            sites,
            topologyStore: topologies,
            contentStore: content,
            eventStore: events,
            simulationTime: time);

        Initialize(query, runtime);
        observerView.BindGmConsole(new WorldObserverGmConsoleContext(
            worldCommandService,
            definitions,
            identity,
            new[] { north, south },
            new NpcRuntime[0],
            sites,
            topologies,
            content,
            () => observerView.SelectedPlaceRuntimeId,
            observerView.Refresh,
            new DeterministicWorldCommandNaturalLanguageTranslator()));
    }

    private CityData CreateCityDefinition(string id, string displayName)
    {
        CityData definition = ScriptableObject.CreateInstance<CityData>();
        definition.id = id;
        definition.cityName = displayName;
        definition.initialPopulation = 1000;
        runtimeDefinitions.Add(definition);
        return definition;
    }

    private sealed class WorldObserverDemoCapabilityModel : ICapabilityModel
    {
        public CapabilityEvaluationResult Evaluate(NpcRuntime participant, CapabilityEvaluationContext context = null)
        {
            return new CapabilityEvaluationResult(1f, 1f, null);
        }
    }
}
