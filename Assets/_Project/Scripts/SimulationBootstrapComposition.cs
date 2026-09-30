using System.Collections.Generic;

/// <summary>Single public handoff for a fully constructed authored bootstrap.</summary>
public sealed class SimulationBootstrapComposition
{
    private readonly NpcDecisionRecorder decisionRecorder;
    private readonly EconomyTransactionService economyTransactionService;

    internal SimulationBootstrapComposition(
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
        Manifest = manifest;
        SimulationTime = time;
        Calendar = calendar;
        SpatialNetwork = spatialNetwork;
        DomainEventStore = events;
        History = history;
        ScheduledDirectives = directives;
        Decisions = decisions;
        this.decisionRecorder = decisionRecorder ?? throw new System.ArgumentNullException(nameof(decisionRecorder));
        SimulationRecordSequenceCensusProvider = new SimulationRecordSequenceCensusProvider(recordSequence);
        this.economyTransactionService = economyTransactionService ?? throw new System.ArgumentNullException(nameof(economyTransactionService));
        NpcChronicles = chronicles;
        ChronicleFormatter = chronicleFormatter;
        TravelParties = travelParties;
        GroupTravel = groupTravel;
        Runtime = runtime;
        ActorChoiceInputCensusProvider = new ActorChoiceP11CensusProvider(Runtime.ActorChoiceStore);
        ActorChoiceTemporalCensusProvider = new ActorChoiceTemporalCensusProvider(Runtime.ActorChoiceStore);
        RuntimeIdentityCensusProviders = RuntimeIdentityRegistryCensusProvider.CreateProviders(runtimeIdentityRegistry);
        RuntimeIdAllocatorCensusProviders = RuntimeIdAllocatorCensusProvider.CreateProviders(runtimeIdAllocator);
        ArmedForceStoreCensusProviders = ArmedForceStoreCensusProvider.CreateProviders(Runtime.ArmedForceStore);
        ContingentManpowerCensusProvider = new ContingentManpowerCensusProvider(Runtime.ContingentManpowerStateStore);
        ArmedForceSpatialCensusProvider = new ArmedForceSpatialCensusProvider(Runtime.ArmedForceSpatialStateStore);
        ConflictCensusProvider = new PersistentConflictCensusProvider(Runtime.ConflictStore);
        ExplorableSites = sites;
        Expeditions = expeditions;
        ExpeditionSystem = expeditionSystem;
    }

    public string ProfileContractIdentity => Manifest.ContractIdentity;
    public string ProfileFingerprint => Manifest.Fingerprint;
    public SimulationGenesisManifest Manifest { get; }
    public SimulationTime SimulationTime { get; }
    public CalendarDefinition Calendar { get; }
    public SpatialNetworkRuntime SpatialNetwork { get; }
    public DomainEventStore DomainEventStore { get; }
    public HistoryStore History { get; }
    public ScheduledDirectiveStore ScheduledDirectives { get; }
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
    public SimulationRuntime Runtime { get; }
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
    /// <summary>The P8-owned spatial truth authority published with the genesis handoff.</summary>
    public SpatialAuthorityStore SpatialAuthority => Runtime.SpatialAuthorityStore;
    public ExplorableSiteStore ExplorableSites { get; }
    public ExpeditionStore Expeditions { get; }
    public ExpeditionSystem ExpeditionSystem { get; }
}
