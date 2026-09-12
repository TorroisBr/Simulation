using System;
using System.Collections.Generic;
using UnityEngine;

public static class SimulationTestFactory
{
    private static readonly List<UnityEngine.Object> createdDefinitions = new List<UnityEngine.Object>();

    public static ItemData CreateItem(string id, float basePrice = 10f)
    {
        ItemData item = Track(ScriptableObject.CreateInstance<ItemData>());
        item.id = id;
        item.itemName = id + " display";
        item.basePrice = basePrice;
        return item;
    }

    public static NpcJobData CreateJob(NpcJobType jobType = NpcJobType.None, MerchantBehavior merchantBehavior = MerchantBehavior.Traveling)
    {
        NpcJobData job = Track(ScriptableObject.CreateInstance<NpcJobData>());
        job.jobName = jobType + " job";
        job.jobType = jobType;
        job.merchantBehavior = merchantBehavior;
        return job;
    }

    public static NpcData CreateNpc(string id, NpcJobType jobType = NpcJobType.None, MerchantBehavior merchantBehavior = MerchantBehavior.Traveling)
    {
        NpcData npc = Track(ScriptableObject.CreateInstance<NpcData>());
        npc.id = id;
        npc.name = id + " display";
        npc.job = CreateJob(jobType, merchantBehavior);
        return npc;
    }

    public static NpcActionData CreateAction(string id, NpcActionType actionType, NpcActionCategory category = NpcActionCategory.General)
    {
        NpcActionData action = Track(ScriptableObject.CreateInstance<NpcActionData>());
        action.id = id;
        action.actionName = id + " display";
        action.actionType = actionType;
        action.actionCategory = category;
        action.baseUtility = 10f;
        return action;
    }

    public static CityData CreateCityData(string id, params MarketItemConfig[] marketItems)
    {
        CityData city = Track(ScriptableObject.CreateInstance<CityData>());
        city.id = id;
        city.cityName = id + " display";

        if (marketItems != null)
        {
            city.marketItems.AddRange(marketItems);
        }

        return city;
    }

    public static CityRuntime CreateCity(string runtimeId, string locationRuntimeId, params MarketItemConfig[] marketItems)
    {
        CityData cityData = CreateCityData("definition-" + runtimeId, marketItems);
        return new CityRuntime(runtimeId, cityData, new SpatialLocationRuntime(locationRuntimeId));
    }

    public static MarketItemConfig CreateMarketItem(ItemData item, int initialAmount = 100, int desiredAmount = 100)
    {
        return new MarketItemConfig
        {
            item = item,
            initialAmount = initialAmount,
            desiredAmount = desiredAmount
        };
    }

    public static CommercialMarketObservation CreateObservation(
        string locationRuntimeId,
        ItemData item,
        float price,
        int stock,
        long observedDay,
        long receivedDay,
        CommercialKnowledgeSource source = CommercialKnowledgeSource.DirectObservation,
        string sourceRuntimeId = null)
    {
        return new CommercialMarketObservation(
            locationRuntimeId,
            item,
            price,
            stock,
            observedDay,
            receivedDay,
            source,
            sourceRuntimeId);
    }

    public static SpatialRouteRuntime CreateRoute(string runtimeId, CityRuntime origin, CityRuntime destination, int travelDays = 1)
    {
        return new SpatialRouteRuntime(runtimeId, origin.Location, destination.Location, travelDays);
    }

    public static SpatialNetworkRuntime CreateNetwork(
        RuntimeIdentityRegistry identityRegistry,
        IEnumerable<CityRuntime> cities,
        IEnumerable<SpatialRouteRuntime> routes)
    {
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identityRegistry);

        if (cities != null)
        {
            foreach (CityRuntime city in cities)
            {
                if (city != null)
                {
                    network.RegisterLocation(city.Location);
                }
            }
        }

        if (routes != null)
        {
            foreach (SpatialRouteRuntime route in routes)
            {
                if (route != null)
                {
                    network.RegisterRoute(route);
                }
            }
        }

        return network;
    }

    public static RecordFixture CreateRecordFixture(long absoluteDay = 0L)
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        SimulationTime time = new SimulationTime(absoluteDay);
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        NpcDecisionStore decisions = new NpcDecisionStore();
        NpcDecisionRecorder decisionRecorder = new NpcDecisionRecorder(
            allocator,
            time,
            sequence,
            decisions);
        HistoryStore history = new HistoryStore();
        DomainEventStore events = new DomainEventStore(history, new HistoryPolicy());
        DomainEventRecorder eventRecorder = new DomainEventRecorder(
            allocator,
            time,
            sequence,
            events);

        return new RecordFixture(
            allocator,
            time,
            sequence,
            decisions,
            decisionRecorder,
            history,
            events,
            eventRecorder,
            new NpcChronicleService(decisions, events));
    }

    public static MerchantSystem CreateMerchantSystem(
        TravelSystem travelSystem,
        SimulationTime simulationTime,
        NpcDecisionRecorder decisionRecorder = null,
        CommercialKnowledgeSettings knowledgeSettings = null,
        bool allowTradeRepositioning = false)
    {
        return new MerchantSystem(
            5,
            1f,
            allowTradeRepositioning,
            travelSystem,
            simulationTime,
            knowledgeSettings ?? new CommercialKnowledgeSettings(),
            decisionRecorder);
    }

    public static void CleanupDefinitions()
    {
        for (int i = createdDefinitions.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object definition = createdDefinitions[i];

            if (definition != null)
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        createdDefinitions.Clear();
    }

    private static T Track<T>(T definition) where T : UnityEngine.Object
    {
        definition.hideFlags = HideFlags.HideAndDontSave;
        createdDefinitions.Add(definition);
        return definition;
    }
}

public sealed class RecordFixture
{
    public RuntimeIdAllocator Allocator { get; }
    public SimulationTime Time { get; }
    public SimulationRecordSequence Sequence { get; }
    public NpcDecisionStore Decisions { get; }
    public NpcDecisionRecorder DecisionRecorder { get; }
    public HistoryStore History { get; }
    public DomainEventStore Events { get; }
    public DomainEventRecorder EventRecorder { get; }
    public NpcChronicleService Chronicle { get; }

    public RecordFixture(
        RuntimeIdAllocator allocator,
        SimulationTime time,
        SimulationRecordSequence sequence,
        NpcDecisionStore decisions,
        NpcDecisionRecorder decisionRecorder,
        HistoryStore history,
        DomainEventStore events,
        DomainEventRecorder eventRecorder,
        NpcChronicleService chronicle)
    {
        Allocator = allocator;
        Time = time;
        Sequence = sequence;
        Decisions = decisions;
        DecisionRecorder = decisionRecorder;
        History = history;
        Events = events;
        EventRecorder = eventRecorder;
        Chronicle = chronicle;
    }
}

public sealed class ThreeCityFixture
{
    public CityRuntime A { get; }
    public CityRuntime B { get; }
    public CityRuntime C { get; }
    public SpatialRouteRuntime RouteAB { get; }
    public SpatialRouteRuntime RouteAC { get; }
    public RuntimeIdentityRegistry IdentityRegistry { get; }
    public SpatialNetworkRuntime Network { get; }
    public Dictionary<SpatialLocationRuntime, CityRuntime> CitiesByLocation { get; }

    public ThreeCityFixture(bool includeRouteAC = true)
    {
        A = SimulationTestFactory.CreateCity("city-a", "location-a");
        B = SimulationTestFactory.CreateCity("city-b", "location-b");
        C = SimulationTestFactory.CreateCity("city-c", "location-c");
        RouteAB = SimulationTestFactory.CreateRoute("route-a-b", A, B, 1);
        RouteAC = SimulationTestFactory.CreateRoute("route-a-c", A, C, 2);
        IdentityRegistry = new RuntimeIdentityRegistry();
        CitiesByLocation = new Dictionary<SpatialLocationRuntime, CityRuntime>
        {
            { A.Location, A },
            { B.Location, B },
            { C.Location, C }
        };
        Network = SimulationTestFactory.CreateNetwork(
            IdentityRegistry,
            new[] { A, B, C },
            includeRouteAC ? new[] { RouteAB, RouteAC } : new[] { RouteAB });
    }

    public TravelSystem CreateTravelSystem(SimulationTime time, DomainEventRecorder eventRecorder = null)
    {
        return new TravelSystem(
            Network,
            location => CitiesByLocation.TryGetValue(location, out CityRuntime city) ? city : null,
            1f,
            eventRecorder,
            null);
    }
}
