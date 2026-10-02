/// <summary>Single public handoff for a fully constructed authored bootstrap.</summary>
using System;
using System.Collections.Generic;

public sealed class SimulationBootstrapComposition
{
    private readonly NpcDecisionRecorder decisionRecorder;
    private readonly EconomyTransactionService economyTransactionService;

    internal SimulationBootstrapComposition(
        WorldId worldId,
        SimulationGenesisManifest manifest,
        SimulationTime time,
        CalendarDefinition calendar,
        SpatialNetworkRuntime spatialNetwork,
        DomainEventStore events,
        HistoryStore history,
        ScheduledDirectiveStore directives,
        NpcDecisionStore decisions,
        NpcDecisionRecorder decisionRecorder,
        SimulationRecordSequence recordSequence,
        EconomyTransactionService economyTransactionService,
        NpcChronicleService chronicles,
        NpcChronicleFormatter chronicleFormatter,
        TravelPartyStore travelParties,
        TravelPartySystem groupTravel,
        SimulationRuntime runtime,
        RuntimeIdentityRegistry runtimeIdentityRegistry,
        RuntimeIdAllocator runtimeIdAllocator,
        ExplorableSiteStore sites,
        ExpeditionStore expeditions,
        ExpeditionSystem expeditionSystem)
    {
        WorldId = worldId ?? throw new ArgumentNullException(nameof(worldId));
        if (runtime == null)
            throw new ArgumentNullException(nameof(runtime));
        if (!ReferenceEquals(WorldId, runtime.WorldId))
            throw new ArgumentException(
                "Bootstrap composition and SimulationRuntime must share the exact WorldId instance.",
                nameof(runtime));
        if (directives == null) throw new ArgumentNullException(nameof(directives));
        if (expeditions == null || expeditionSystem == null || !ReferenceEquals(expeditionSystem.Store, expeditions))
            throw new ArgumentException("Bootstrap expedition owner and system must share the installed ExpeditionStore.");

        Manifest = manifest;
        SimulationTime = time;
        Calendar = calendar;
        SpatialNetwork = spatialNetwork;
        DomainEventStore = events;
        History = history;
        ScheduledDirectives = directives;
        ScheduledDirectiveCensusProvider = new ScheduledDirectiveCensusProvider(directives);
        Decisions = decisions;
        this.decisionRecorder = decisionRecorder ?? throw new System.ArgumentNullException(nameof(decisionRecorder));
        SimulationRecordSequenceCensusProvider = new SimulationRecordSequenceCensusProvider(recordSequence);
        this.economyTransactionService = economyTransactionService ?? throw new System.ArgumentNullException(nameof(economyTransactionService));
        NpcChronicles = chronicles;
        ChronicleFormatter = chronicleFormatter;
        if (travelParties == null || groupTravel == null || groupTravel.Store != travelParties)
        {
            throw new ArgumentException("Bootstrap travel systems must share the installed TravelPartyStore.");
        }
        TravelParties = travelParties;
        GroupTravel = groupTravel;
        TravelPartyCensusProvider = new TravelPartyCensusProvider(travelParties);
        Runtime = runtime;
        PersonStoreCensusProviders = PersonStoreCensusProvider.CreateProviders(Runtime.PersonStore);
        CityNpcPresenceCensusProviders = CityNpcPresenceCensusProvider.CreateProviders(
            Runtime.Cities,
            Runtime.NpcRuntimes);
        CityMarketCensusProviders = CityMarketCensusProvider.CreateProviders(Runtime.Cities);
        ActorChoiceInputCensusProvider = new ActorChoiceP11CensusProvider(Runtime.ActorChoiceStore);
        ActorChoiceTemporalCensusProvider = new ActorChoiceTemporalCensusProvider(Runtime.ActorChoiceStore);
        RuntimeIdentityCensusProviders = RuntimeIdentityRegistryCensusProvider.CreateProviders(runtimeIdentityRegistry);
        RuntimeIdAllocatorCensusProviders = RuntimeIdAllocatorCensusProvider.CreateProviders(runtimeIdAllocator);
        ArmedForceStoreCensusProviders = ArmedForceStoreCensusProvider.CreateProviders(Runtime.ArmedForceStore);
        ContingentManpowerCensusProvider = new ContingentManpowerCensusProvider(Runtime.ContingentManpowerStateStore);
        ArmedForceSpatialCensusProvider = new ArmedForceSpatialCensusProvider(Runtime.ArmedForceSpatialStateStore);
        ConflictCensusProvider = new PersistentConflictCensusProvider(Runtime.ConflictStore);
        WarCensusProvider = new PersistentWarCensusProvider(Runtime.WarStore);
        BattleCensusProvider = new PersistentBattleCensusProvider(Runtime.BattleStore);
        EstateCensusProvider = new EstateCensusProvider(Runtime.EstateStore);
        PropertyOwnershipCensusProviders = PropertyOwnershipCensusProvider.CreateProviders(Runtime.PropertyOwnershipStore);
        InstitutionOfficeCensusProviders = InstitutionOfficeCensusProvider.CreateProviders(
            Runtime.InstitutionStoreForWorldBoundary,
            Runtime.OfficeStoreForWorldBoundary);
        GenealogyCensusProvider = new GenealogyCensusProvider(Runtime.GenealogyStoreForWorldBoundary);
        ExplorableSites = sites;
        ExplorableSiteCensusProvider = new ExplorableSiteCensusProvider(ExplorableSites);
        SettlementPopulationCensusProviders = SettlementPopulationCensusProvider.CreateProviders(Runtime.Cities);
        SpatialNetworkCensusProviders = SpatialNetworkCensusProvider.CreateProviders(SpatialNetwork);
        Expeditions = expeditions;
        ExpeditionSystem = expeditionSystem;
        ExpeditionCensusProvider = new ExpeditionCensusProvider(expeditions);
    }

    public string ProfileContractIdentity => Manifest.ContractIdentity;
    public string ProfileFingerprint => Manifest.Fingerprint;
    /// <summary>The stable identity allocated for this composed world continuation.</summary>
    public WorldId WorldId { get; }
    public SimulationGenesisManifest Manifest { get; }
    public SimulationTime SimulationTime { get; }
    public CalendarDefinition Calendar { get; }
    public SpatialNetworkRuntime SpatialNetwork { get; }
    public DomainEventStore DomainEventStore { get; }
    public HistoryStore History { get; }
    public ScheduledDirectiveStore ScheduledDirectives { get; }
    /// <summary>Passive witness for the exact scheduled-directive store installed in this bootstrap.</summary>
    public IOwnerSectionCensusProvider ScheduledDirectiveCensusProvider { get; }
    public NpcDecisionStore Decisions { get; }
    /// <summary>Passive witness for the shared causal event/decision sequence.</summary>
    public IOwnerSectionCensusProvider SimulationRecordSequenceCensusProvider { get; }
    /// <summary>Returns an owner-issued live census witness, not a decision-record export.</summary>
    public OwnerSectionCensusWitness GetNpcDecisionOccurrenceReceiptCensus()
    {
        return decisionRecorder.GetOccurrenceReceiptCensus();
    }

    /// <summary>Returns an owner-issued live census witness, not a receipt export.</summary>
    public OwnerSectionCensusWitness GetEconomyKeyedSaleReceiptCensus()
    {
        return economyTransactionService.GetKeyedSaleReceiptCensus();
    }

    public NpcChronicleService NpcChronicles { get; }
    public NpcChronicleFormatter ChronicleFormatter { get; }
    public TravelPartyStore TravelParties { get; }
    public TravelPartySystem GroupTravel { get; }
    public IOwnerSectionCensusProvider TravelPartyCensusProvider { get; }
    public SimulationRuntime Runtime { get; }
    /// <summary>Coherent factual-read surface for the selected daily profile; null when that profile is not composed.</summary>
    public FactualReadCoordinator FactualReads => Runtime.FactualReads;
    /// <summary>Fixed passive witnesses for the installed Person registry and materialization bindings.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> PersonStoreCensusProviders { get; }
    /// <summary>Fixed passive witnesses for each City's NPC-presence projection.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> CityNpcPresenceCensusProviders { get; }
    /// <summary>Fixed passive witnesses for each installed City's Market stock rows.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> CityMarketCensusProviders { get; }
    /// <summary>Passive witness for retained P11 actor-choice history.</summary>
    public IOwnerSectionCensusProvider ActorChoiceInputCensusProvider { get; }
    /// <summary>Passive exact-zero witness for excluded P18 temporal actor choices.</summary>
    public IOwnerSectionCensusProvider ActorChoiceTemporalCensusProvider { get; }
    /// <summary>Fixed passive witnesses for the runtime's typed identity indexes.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> RuntimeIdentityCensusProviders { get; }
    /// <summary>Fixed passive witnesses for the runtime ID allocator's typed counters.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> RuntimeIdAllocatorCensusProviders { get; }
    /// <summary>Fixed passive witnesses for the runtime's armed-force owner sections.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> ArmedForceStoreCensusProviders { get; }
    /// <summary>Passive witness for the runtime's separate contingent-manpower state owner.</summary>
    public IOwnerSectionCensusProvider ContingentManpowerCensusProvider { get; }
    /// <summary>Passive witness for the runtime's separate armed-force position owner.</summary>
    public IOwnerSectionCensusProvider ArmedForceSpatialCensusProvider { get; }
    /// <summary>Passive witness for the runtime's persistent conflict owner.</summary>
    public IOwnerSectionCensusProvider ConflictCensusProvider { get; }
    /// <summary>Passive witness for the runtime's persistent war owner.</summary>
    public IOwnerSectionCensusProvider WarCensusProvider { get; }
    /// <summary>Passive witness for the runtime's persistent battle owner.</summary>
    public IOwnerSectionCensusProvider BattleCensusProvider { get; }
    /// <summary>Passive witness for the runtime's Estate owner.</summary>
    public IOwnerSectionCensusProvider EstateCensusProvider { get; }
    /// <summary>Fixed passive witnesses for current property ownership and retained transfer history.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> PropertyOwnershipCensusProviders { get; }
    /// <summary>Fixed passive witnesses for institution, office, incumbency and tenure owners.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> InstitutionOfficeCensusProviders { get; }
    /// <summary>Passive witness for the runtime's installed direct-parentage owner.</summary>
    public GenealogyCensusProvider GenealogyCensusProvider { get; }
    /// <summary>The P8-owned spatial truth authority published with the genesis handoff.</summary>
    public SpatialAuthorityStore SpatialAuthority => Runtime.SpatialAuthorityStore;
    public ExplorableSiteStore ExplorableSites { get; }
    /// <summary>Passive witness for the authored bootstrap's installed
    /// ExplorableSite owner.</summary>
    public ExplorableSiteCensusProvider ExplorableSiteCensusProvider { get; }
    /// <summary>Fixed passive witnesses for the installed population owners
    /// of the composed Cities.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> SettlementPopulationCensusProviders { get; }
    /// <summary>Fixed passive witnesses for the installed legacy spatial-network Locations and Routes.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> SpatialNetworkCensusProviders { get; }
    /// <summary>Fixed passive per-NPC witnesses for spatial Knowledge locations and routes.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> SpatialKnowledgeCensusProviders =>
        Runtime.SpatialKnowledgeCensusProviders;
    /// <summary>Fixed passive per-NPC witnesses for the selected local and commercial Knowledge owners.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> NpcKnowledgeCensusProviders =>
        Runtime.NpcKnowledgeCensusProviders;
    public ExpeditionStore Expeditions { get; }
    public ExpeditionSystem ExpeditionSystem { get; }
    /// <summary>Passive witness for the exact expedition store shared by its system.</summary>
    public IOwnerSectionCensusProvider ExpeditionCensusProvider { get; }
}
