using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Composes real selected-profile owners for EditMode tests that need to exercise
/// Daily-v1 admission, capture, restoration, or continuation.
/// </summary>
public sealed class DailyV1SelectedProfileFixture
{
    public SimulationRuntime Runtime { get; }
    public SimulationTime Time { get; }
    public RecordFixture Records { get; }
    public IReadOnlyList<CityRuntime> Cities { get; }
    public IReadOnlyList<NpcRuntime> Npcs { get; }
    public IReadOnlyList<string> RouteIds { get; }
    public RuntimeIdentityRegistry Identities { get; }
    public SpatialNetworkRuntime SpatialNetwork { get; }
    public SpatialAuthorityStore SpatialAuthority { get; }
    public PersonStore PersonStore { get; }
    public GenealogyStore GenealogyStore { get; }
    public ExplorableSiteStore SiteStore { get; }
    public ExplorableSiteKnowledgeSystem SiteKnowledge { get; }
    public EconomyTransactionService EconomyTransactions { get; }
    public ScheduledDirectiveStore DirectiveStore { get; }
    public ScheduledDirectiveSystem DirectiveSystem { get; }
    public JusticeSystem Justice { get; }
    public CrimeSystem Crime { get; }
    public TravelSystem Travel { get; }
    public TravelPartyStore TravelParties { get; }
    public TravelPartySystem TravelPartySystem { get; }
    public ExpeditionStore Expeditions { get; }
    public ExpeditionSystem ExpeditionSystem { get; }
    public NpcDecisionSystem DecisionSystem { get; }
    public IReadOnlyList<NpcActionData> ConfiguredActions { get; }

    private DailyV1SelectedProfileFixture(
        SimulationRuntime runtime,
        RecordFixture records,
        CityRuntime[] cities,
        NpcRuntime[] npcs,
        string[] routeIds,
        RuntimeIdentityRegistry identities,
        SpatialNetworkRuntime spatialNetwork,
        SpatialAuthorityStore spatialAuthority,
        PersonStore personStore,
        GenealogyStore genealogyStore,
        ExplorableSiteStore siteStore,
        ExplorableSiteKnowledgeSystem siteKnowledge,
        EconomyTransactionService economyTransactions,
        ScheduledDirectiveStore directiveStore,
        ScheduledDirectiveSystem directiveSystem,
        JusticeSystem justice,
        CrimeSystem crime,
        TravelSystem travel,
        TravelPartyStore travelParties,
        TravelPartySystem travelPartySystem,
        ExpeditionStore expeditions,
        ExpeditionSystem expeditionSystem,
        NpcDecisionSystem decisionSystem,
        NpcActionData[] configuredActions)
    {
        Runtime = runtime;
        Records = records;
        Time = records.Time;
        Cities = Array.AsReadOnly(cities);
        Npcs = Array.AsReadOnly(npcs);
        RouteIds = Array.AsReadOnly(routeIds);
        Identities = identities;
        SpatialNetwork = spatialNetwork;
        SpatialAuthority = spatialAuthority;
        PersonStore = personStore;
        GenealogyStore = genealogyStore;
        SiteStore = siteStore;
        SiteKnowledge = siteKnowledge;
        EconomyTransactions = economyTransactions;
        DirectiveStore = directiveStore;
        DirectiveSystem = directiveSystem;
        Justice = justice;
        Crime = crime;
        Travel = travel;
        TravelParties = travelParties;
        TravelPartySystem = travelPartySystem;
        Expeditions = expeditions;
        ExpeditionSystem = expeditionSystem;
        DecisionSystem = decisionSystem;
        ConfiguredActions = Array.AsReadOnly(configuredActions);
    }

    /// <summary>
    /// Creates a Daily-v1 fixture at absolute day zero. Supplied owners and
    /// services are retained by reference; missing roster capacity is filled
    /// with ordinary CityRuntime and NpcRuntime instances.
    /// </summary>
    public static DailyV1SelectedProfileFixture Create(
        string identityPrefix,
        IReadOnlyList<CityRuntime> cities = null,
        IReadOnlyList<NpcRuntime> npcs = null,
        PersonStore personStore = null,
        GenealogyStore genealogyStore = null,
        ExplorableSiteStore siteStore = null,
        NpcDecisionSystem decisionSystem = null,
        IReadOnlyList<NpcActionData> configuredActions = null,
        Func<RecordFixture, TravelSystem, MerchantSystem> merchantSystemFactory = null,
        Func<RecordFixture, TravelSystem, MerchantSystem, NpcDecisionSystem> decisionSystemFactory = null,
        Action<TravelPartySystem> configureTravelPartySystem = null,
        EffectiveSimulationConfiguration configuration = null,
        bool economyEnabled = false,
        Action<RecordFixture> configureRecords = null,
        int routeTravelDays = 1,
        EconomyTransactionService economyTransactionService = null,
        MerchantSystem merchantSystem = null)
    {
        return Create(
            new SimulationTime(0L),
            identityPrefix,
            cities,
            npcs,
            personStore,
            genealogyStore,
            siteStore,
            decisionSystem,
            configuredActions,
            merchantSystemFactory,
            decisionSystemFactory,
            configureTravelPartySystem,
            configuration,
            economyEnabled,
            configureRecords,
            routeTravelDays,
            economyTransactionService,
            merchantSystem);
    }

    /// <summary>
    /// Creates a Daily-v1 fixture using the exact supplied SimulationTime
    /// instance for runtime advancement and all time-aware record services.
    /// </summary>
    public static DailyV1SelectedProfileFixture Create(
        SimulationTime simulationTime,
        string identityPrefix,
        IReadOnlyList<CityRuntime> cities = null,
        IReadOnlyList<NpcRuntime> npcs = null,
        PersonStore personStore = null,
        GenealogyStore genealogyStore = null,
        ExplorableSiteStore siteStore = null,
        NpcDecisionSystem decisionSystem = null,
        IReadOnlyList<NpcActionData> configuredActions = null,
        Func<RecordFixture, TravelSystem, MerchantSystem> merchantSystemFactory = null,
        Func<RecordFixture, TravelSystem, MerchantSystem, NpcDecisionSystem> decisionSystemFactory = null,
        Action<TravelPartySystem> configureTravelPartySystem = null,
        EffectiveSimulationConfiguration configuration = null,
        bool economyEnabled = false,
        Action<RecordFixture> configureRecords = null,
        int routeTravelDays = 1,
        EconomyTransactionService economyTransactionService = null,
        MerchantSystem merchantSystem = null)
    {
        if (simulationTime == null) throw new ArgumentNullException(nameof(simulationTime));
        if (string.IsNullOrWhiteSpace(identityPrefix))
            throw new ArgumentException("A stable fixture identity prefix is required.", nameof(identityPrefix));

        CityRuntime[] cityOwners = CompleteCityRoster(identityPrefix, cities);
        NpcRuntime[] npcOwners = CompleteNpcRoster(identityPrefix, npcs);
        RuntimeIdentityRegistry identities = new RuntimeIdentityRegistry();
        foreach (CityRuntime city in cityOwners)
        {
            if (!identities.RegisterCity(city))
                throw new InvalidOperationException("The supplied CityRuntime identities are not uniquely registerable.");
        }
        foreach (NpcRuntime npc in npcOwners)
        {
            if (!identities.RegisterNpc(npc))
                throw new InvalidOperationException("The supplied NpcRuntime identities are not uniquely registerable.");
        }

        SpatialNetworkRuntime spatialNetwork = new SpatialNetworkRuntime(identities);
        foreach (CityRuntime city in cityOwners)
        {
            if (!spatialNetwork.RegisterLocation(city.Location))
                throw new InvalidOperationException("The selected-profile City locations are not uniquely registerable.");
        }

        List<string> routeIds = new List<string>();
        for (int i = 0; i + 1 < cityOwners.Length; i++)
        {
            SpatialLocationRuntime from = cityOwners[i].Location;
            SpatialLocationRuntime to = cityOwners[i + 1].Location;
            string forwardId = identityPrefix + "-route-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-forward";
            string reverseId = identityPrefix + "-route-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-reverse";
            if (!spatialNetwork.RegisterRoute(new SpatialRouteRuntime(forwardId, from, to, routeTravelDays))
                || !spatialNetwork.RegisterRoute(new SpatialRouteRuntime(reverseId, to, from, routeTravelDays)))
            {
                throw new InvalidOperationException("The selected-profile travel routes could not be registered.");
            }
            routeIds.Add(forwardId);
            routeIds.Add(reverseId);
        }

        PersonStore resolvedPersonStore = personStore ?? new PersonStore();
        GenealogyStore resolvedGenealogyStore = genealogyStore ?? new GenealogyStore();
        ExplorableSiteStore resolvedSiteStore = siteStore ?? new ExplorableSiteStore();
        NpcActionData[] actions = configuredActions == null
            ? Array.Empty<NpcActionData>()
            : configuredActions.ToArray();
        RecordFixture records = SimulationTestFactory.CreateRecordFixtureWithTime(simulationTime);
        configureRecords?.Invoke(records);
        EconomyTransactionService economyTransactions = economyTransactionService ?? new EconomyTransactionService();
        if (decisionSystemFactory != null && decisionSystem != null)
            throw new ArgumentException("Supply either a decision system or a profile-bound factory, not both.", nameof(decisionSystemFactory));
        TravelSystem travel = new TravelSystem(
            spatialNetwork,
            location => cityOwners.FirstOrDefault(city => ReferenceEquals(city.Location, location)),
            1f,
            records.EventRecorder,
            null,
            economyTransactions);
        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(simulationTime);
        ScheduledDirectiveSystem directiveSystem = new ScheduledDirectiveSystem(directiveStore, identities);
        JusticeSystem justice = new JusticeSystem(null, null, null, null, records.EventRecorder, null);
        CrimeSystem crime = new CrimeSystem(
            justice,
            travel,
            null,
            transactionService: economyTransactions,
            configuration: new EffectiveCrimeConfiguration(false, false),
            simulationTime: simulationTime);
        TravelPartyStore travelParties = new TravelPartyStore();
        TravelPartySystem travelPartySystem = new TravelPartySystem(
            travelParties,
            records.Allocator,
            identities,
            travel,
            simulationTime,
            records.Sequence,
            records.EventRecorder,
            transactionService: economyTransactions);
        configureTravelPartySystem?.Invoke(travelPartySystem);
        if (merchantSystem != null && merchantSystemFactory != null)
            throw new ArgumentException("Supply either a MerchantSystem or a profile-bound factory, not both.", nameof(merchantSystemFactory));
        MerchantSystem resolvedMerchantSystem = merchantSystem ?? merchantSystemFactory?.Invoke(records, travel);
        NpcDecisionSystem resolvedDecisionSystem = decisionSystemFactory == null
            ? decisionSystem
            : decisionSystemFactory(records, travel, resolvedMerchantSystem);
        ExpeditionStore expeditions = new ExpeditionStore();
        ExplorableSiteKnowledgeSystem siteKnowledge = new ExplorableSiteKnowledgeSystem();
        ExpeditionSystem expeditionSystem = new ExpeditionSystem(
            expeditions,
            records.Allocator,
            identities,
            resolvedSiteStore,
            travelPartySystem,
            travelParties,
            siteKnowledge,
            simulationTime,
            records.EventRecorder);
        SpatialAuthorityStore spatialAuthority = CreateSpatialAuthority(identityPrefix, cityOwners);

        SimulationRuntime runtime = new SimulationRuntime(
            simulationTime,
            cityOwners,
            npcOwners,
            economyEnabled: configuration == null ? (bool?)economyEnabled : null,
            configuredActions: actions,
            configuration: configuration,
            scheduledDirectiveSystem: directiveSystem,
            justiceSystem: justice,
            crimeSystem: crime,
            npcDecisionSystem: resolvedDecisionSystem,
            merchantSystem: resolvedMerchantSystem,
            travelSystem: travel,
            travelPartySystem: travelPartySystem,
            decisionRecorder: records.DecisionRecorder,
            explorableSiteStore: resolvedSiteStore,
            explorableSiteKnowledgeSystem: siteKnowledge,
            expeditionSystem: expeditionSystem,
            spatialAuthorityStore: spatialAuthority,
            personStore: resolvedPersonStore,
            genealogyStore: resolvedGenealogyStore,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            economyTransactionService: economyTransactions,
            requireP12ReceiptCensusOwners: true,
            runtimeIdentityRegistry: identities,
            spatialNetworkRuntime: spatialNetwork,
            requireP12RuntimeIdentitySpatialCensusOwners: true,
            worldId: new WorldId(Guid.NewGuid()));

        return new DailyV1SelectedProfileFixture(
            runtime,
            records,
            cityOwners,
            npcOwners,
            routeIds.ToArray(),
            identities,
            spatialNetwork,
            spatialAuthority,
            resolvedPersonStore,
            resolvedGenealogyStore,
            resolvedSiteStore,
            siteKnowledge,
            economyTransactions,
            directiveStore,
            directiveSystem,
            justice,
            crime,
            travel,
            travelParties,
            travelPartySystem,
            expeditions,
            expeditionSystem,
            resolvedDecisionSystem,
            actions);
    }

    private static CityRuntime[] CompleteCityRoster(
        string identityPrefix,
        IReadOnlyList<CityRuntime> suppliedCities)
    {
        List<CityRuntime> result = suppliedCities == null
            ? new List<CityRuntime>()
            : new List<CityRuntime>(suppliedCities);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CityRuntime city in result)
        {
            if (city == null || city.Location == null
                || !runtimeIds.Add(city.RuntimeId)
                || !locationIds.Add(city.Location.RuntimeId))
            {
                throw new ArgumentException("Supplied Cities must have distinct runtime and Location identities.", nameof(suppliedCities));
            }
        }

        int index = 0;
        while (result.Count < 2)
        {
            string cityId = NextIdentity(identityPrefix + "-city", index++, runtimeIds);
            string locationId = NextIdentity(identityPrefix + "-location", index++, locationIds);
            result.Add(SimulationTestFactory.CreateCity(cityId, locationId));
        }
        return result.ToArray();
    }

    private static NpcRuntime[] CompleteNpcRoster(
        string identityPrefix,
        IReadOnlyList<NpcRuntime> suppliedNpcs)
    {
        List<NpcRuntime> result = suppliedNpcs == null
            ? new List<NpcRuntime>()
            : new List<NpcRuntime>(suppliedNpcs);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in result)
        {
            if (npc == null || !runtimeIds.Add(npc.RuntimeId))
                throw new ArgumentException("Supplied NPCs must have distinct runtime identities.", nameof(suppliedNpcs));
        }

        int index = 0;
        while (result.Count < 10)
        {
            string npcId = NextIdentity(identityPrefix + "-npc", index, runtimeIds);
            string definitionId = identityPrefix + "-npc-definition-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            index++;
            result.Add(new NpcRuntime(npcId, SimulationTestFactory.CreateNpc(definitionId)));
        }
        return result.ToArray();
    }

    private static string NextIdentity(string stem, int index, HashSet<string> reserved)
    {
        string candidate = stem + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int suffix = 0;
        while (!reserved.Add(candidate))
        {
            suffix++;
            candidate = stem + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    private static SpatialAuthorityStore CreateSpatialAuthority(
        string identityPrefix,
        IReadOnlyList<CityRuntime> cities)
    {
        string hexId = identityPrefix + "-authority-hex";
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext(identityPrefix + "-scale", "fixture", "v1", 1m, "step"),
            new[]
            {
                new HexRecord(
                    new HexId(hexId),
                    new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain." + identityPrefix), "v1"))
            },
            // The accepted selected profile has two runtime City/Location
            // identities but one authored P8 geography Location. Keep the
            // second runtime location in the spatial-network fixture only;
            // do not manufacture an additional P8 fact.
            cities.Take(1).Select(city => new LocationRecord(
                new LocationId(city.Location.RuntimeId), new HexId(hexId))).ToArray());
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        if (!authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure))
            throw new InvalidOperationException("Could not compose selected-profile fixture geography: " + failure);
        return authority;
    }
}
