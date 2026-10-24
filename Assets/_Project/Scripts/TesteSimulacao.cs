using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Immutable reference set for the currently published TesteSimulacao session.</summary>
internal sealed class SimulationActiveSession
{
    private readonly Dictionary<SpatialLocationRuntime, CityRuntime> cityRuntimeByLocation;

    public SimulationBootstrapComposition Composition { get; }
    public SimulationRuntime Runtime => Composition.Runtime;
    public SimulationLogger Logger { get; }
    public JusticeSystem Justice { get; }
    public RuntimeIdentityRegistry IdentityRegistry { get; }
    public SpatialNetworkRuntime SpatialNetwork { get; }
    public ExplorableSiteStore ExplorableSites => Composition.ExplorableSites;
    public SimulationConfigData Configuration { get; }
    public SimulationRuntimeAdmissionContext AdmissionContext { get; }
    public IAuthoritativeRandomSource RandomSource { get; }
    public DailyV1SessionReportState ReportState { get; }
    /// <summary>
    /// Exact Core-owned selected runtime/session binding. Unity references in
    /// this object are host projections; selected operation scopes resolve to
    /// the Core execution boundary retained here.
    /// </summary>
    internal DailyV1ActiveRuntimeSession CoreRuntimeSession { get; }
    internal DailyV1ExecutionBoundaryState ExecutionBoundaryState =>
        CoreRuntimeSession?.ExecutionBoundaryState ?? Runtime.ExecutionBoundaryState;

    public SimulationActiveSession(
        SimulationBootstrapComposition composition,
        SimulationLogger logger,
        JusticeSystem justice,
        RuntimeIdentityRegistry identityRegistry,
        SpatialNetworkRuntime spatialNetwork,
        IReadOnlyDictionary<SpatialLocationRuntime, CityRuntime> cityRuntimeByLocation,
        SimulationConfigData configuration,
        SimulationRuntimeAdmissionContext admissionContext,
        IAuthoritativeRandomSource randomSource,
        long lastEconomySnapshotDay)
    {
        Composition = composition ?? throw new System.ArgumentNullException(nameof(composition));
        Logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        Justice = justice;
        IdentityRegistry = identityRegistry ?? throw new System.ArgumentNullException(nameof(identityRegistry));
        SpatialNetwork = spatialNetwork ?? throw new System.ArgumentNullException(nameof(spatialNetwork));
        Configuration = configuration;
        RandomSource = randomSource ?? throw new System.ArgumentNullException(nameof(randomSource));
        DailyV1ExecutionBoundaryState executionBoundaryState = Runtime.ExecutionBoundaryState;
        if (executionBoundaryState.HasAdmittedSelectedDailyExecution)
        {
            if (!DailyV1ActiveRuntimeSession.TryCreate(
                    executionBoundaryState,
                    Composition.SelectedDefinitions,
                    lastEconomySnapshotDay,
                    out DailyV1ActiveRuntimeSession coreRuntimeSession,
                    out string activeSessionDiagnostic))
                throw new System.ArgumentException(activeSessionDiagnostic, nameof(composition));
            CoreRuntimeSession = coreRuntimeSession;
        }
        // Compatibility bootstrap paths may use selected definitions for
        // fingerprinting while deliberately running without selected Daily-v1
        // admission. They retain their existing host session behavior; only a
        // fully admitted selected runtime receives the Core active-session binding.

        ReportState = CoreRuntimeSession?.ReportState
            ?? new DailyV1SessionReportState(lastEconomySnapshotDay);

        AdmissionContext = CoreRuntimeSession?.AdmissionContext ?? admissionContext;

        if (!ReferenceEquals(Composition.SpatialNetwork, SpatialNetwork)
            || !ReferenceEquals(Composition.ExplorableSites, ExplorableSites)
            || !Runtime.HasSameP12RuntimeIdentitySpatialOwners(IdentityRegistry, SpatialNetwork, ExplorableSites)
            || !Runtime.HasSameJusticeSystemOwner(Justice)
            || !Runtime.HasSameLoggerOwner(Logger)
            || !Runtime.HasSameRuntimeAdmissionContext(AdmissionContext))
            throw new System.ArgumentException("The active-session references must be the exact owners installed in its bootstrap composition.");

        this.cityRuntimeByLocation = new Dictionary<SpatialLocationRuntime, CityRuntime>();
        if (cityRuntimeByLocation != null)
        {
            foreach (KeyValuePair<SpatialLocationRuntime, CityRuntime> pair in cityRuntimeByLocation)
            {
                if (pair.Key == null || pair.Value == null || this.cityRuntimeByLocation.ContainsKey(pair.Key))
                    throw new System.ArgumentException("The active-session City-location index must contain unique, non-null runtime owners.", nameof(cityRuntimeByLocation));
                this.cityRuntimeByLocation.Add(pair.Key, pair.Value);
            }
        }
    }

    public bool TryGetCityByLocation(SpatialLocationRuntime location, out CityRuntime city)
    {
        if (location == null)
        {
            city = null;
            return false;
        }

        return cityRuntimeByLocation.TryGetValue(location, out city);
    }

    public bool IsProfileCompatibleWith(SimulationActiveSession candidate)
    {
        return candidate != null
            && ReferenceEquals(Configuration, candidate.Configuration)
            && string.Equals(Composition.ProfileContractIdentity, candidate.Composition.ProfileContractIdentity, System.StringComparison.Ordinal)
            && string.Equals(Composition.ProfileFingerprint, candidate.Composition.ProfileFingerprint, System.StringComparison.Ordinal)
            && AdmissionContext != null
            && candidate.AdmissionContext != null
            && AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && candidate.AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && (CoreRuntimeSession == null
                ? candidate.CoreRuntimeSession == null
                : CoreRuntimeSession.IsProfileCompatibleWith(candidate.CoreRuntimeSession));
    }
}

public class TesteSimulacao : MonoBehaviour
{
    [SerializeField] private SimulationConfigData simulationConfig;
    [SerializeField] private int daysToSimulate = 1;
    [SerializeField] private SimulationRuntimeAdmissionProfile runtimeAdmissionProfile;

    private List<NpcRuntime> npcRuntimeList = new List<NpcRuntime>();
    private List<CityRuntime> cityRuntimeList = new List<CityRuntime>();
    private ExplorableSiteStore explorableSiteStore;
    private LocalTopologyStore genesisLocalTopologyStore;
    private LegacySpatialAnchorBindingStore genesisLegacySpatialAnchorBindingStore;
    private P10RuinLocalTopologyCandidate p10RuinCandidate;
    private ExplorableSiteRuntime p10RuinRuntime;
    private P10BGeneratedRuinGenesis.PreparedRuin p10BPreparedRuin;

    private readonly List<INpcActionProvider> actionProviders = new List<INpcActionProvider>();
    private Dictionary<CityData, List<CityRuntime>> cityRuntimesByDefinition = new Dictionary<CityData, List<CityRuntime>>();
    private Dictionary<NpcData, List<NpcRuntime>> npcRuntimesByDefinition = new Dictionary<NpcData, List<NpcRuntime>>();
    private Dictionary<string, List<ExplorableSiteRuntime>> explorableSiteRuntimesByDefinition = new Dictionary<string, List<ExplorableSiteRuntime>>(System.StringComparer.Ordinal);
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
    [System.NonSerialized] private DailyV1DefinitionAdapter.SelectedBindings selectedDefinitionBindings;
    private System.Func<WorldId> worldIdentityAllocator = CreateWorldIdentity;
    private WorldId unpublishedWorldId;
    private long lastEconomySnapshotDay;
    private SimulationBootstrapComposition draftComposition;
    private DailyV1EffectiveDefinitions selectedDefinitions;
    [System.NonSerialized] private DailyV1SelectedRuntimeOwnerGraphBuilder selectedRuntimeOwnerGraphBuilder;
    [System.NonSerialized] private DailyV1SelectedRuntimeOwnerGraph selectedInitialRuntimeOwnerGraph;
    private bool UsesSelectedDefinitionBindings => selectedDefinitions != null && selectedDefinitionBindings != null;
    // The selected runtime session reference is Core-owned. Unity wrappers are
    // projections keyed by that exact session and are never the selected
    // publication authority. The separate compatibility gate is used only for
    // legacy, unadmitted bootstrap sessions; both paths are mutually exclusive.
    private readonly DailyV1Application dailyV1Application = new DailyV1Application();
    private readonly DailyV1ApplicationSessionGate<SimulationActiveSession> compatibilitySessionGate =
        new DailyV1ApplicationSessionGate<SimulationActiveSession>(
            session => session.ExecutionBoundaryState);
    private readonly Dictionary<DailyV1ActiveRuntimeSession, SimulationActiveSession> activeSessionProjections =
        new Dictionary<DailyV1ActiveRuntimeSession, SimulationActiveSession>();
    private SimulationActiveSession activeSession => CaptureActiveSession();
    private SimulationRuntimeAdmissionContext runtimeAdmissionContext;
    private SimulationOperationScope bootstrapPublicationScope;
    private bool bootstrapStartAttempted;
    private bool bootstrapFailed;


    private SimulationBootstrapComposition PublicComposition => CaptureActiveSession()?.Composition;

    public SimulationBootstrapComposition Bootstrap => PublicComposition;
    public string FullLog
    {
        get
        {
            SimulationActiveSession session = CaptureActiveSession();
            return session != null ? session.Logger.FullLog : string.Empty;
        }
    }
    public SimulationTime SimulationTime => CaptureActiveSession()?.Composition.SimulationTime;
    public CalendarDefinition Calendar => CaptureActiveSession()?.Composition.Calendar;
    public SpatialNetworkRuntime SpatialNetwork => CaptureActiveSession()?.SpatialNetwork;
    public DomainEventStore DomainEventStore => CaptureActiveSession()?.Composition.DomainEventStore;
    public HistoryStore History => CaptureActiveSession()?.Composition.History;
    public ScheduledDirectiveStore ScheduledDirectives => CaptureActiveSession()?.Composition.ScheduledDirectives;
    public NpcDecisionStore Decisions => CaptureActiveSession()?.Composition.Decisions;
    public NpcChronicleService NpcChronicles => CaptureActiveSession()?.Composition.NpcChronicles;
    public NpcChronicleFormatter ChronicleFormatter => CaptureActiveSession()?.Composition.ChronicleFormatter;
    public TravelPartyStore TravelParties => CaptureActiveSession()?.Composition.TravelParties;
    public TravelPartySystem GroupTravel => CaptureActiveSession()?.Composition.GroupTravel;
    public SimulationRuntime Runtime => CaptureActiveSession()?.Runtime;
    public ExplorableSiteStore ExplorableSites => CaptureActiveSession()?.ExplorableSites;
    public ExpeditionStore Expeditions => CaptureActiveSession()?.Composition.Expeditions;
    public ExpeditionSystem ExpeditionRuntime => CaptureActiveSession()?.Composition.ExpeditionSystem;
    public ExpeditionSystem ExpeditionSystem => CaptureActiveSession()?.Composition.ExpeditionSystem;
    public long CurrentDay => CaptureActiveSession()?.Composition.SimulationTime.AbsoluteDay ?? 0L;
    public SimulationDate CurrentDate => CaptureActiveSession() is SimulationActiveSession session
        ? session.Runtime.Calendar.GetDate(session.Composition.SimulationTime.AbsoluteDay)
        : default(SimulationDate);

    public bool TryStartTravelParty(ActionExecutionContext context)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session)) return false;
        try
        {
            return session.Runtime.TryStartTravelParty(context);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    public bool TryStartExpedition(
        ExplorableSiteRuntime targetSite,
        ActionExecutionContext context,
        out ExpeditionRuntime expedition)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session))
        {
            expedition = null;
            return false;
        }
        try
        {
            if (session.Composition.ExpeditionSystem == null)
            {
                expedition = null;
                return false;
            }
            return session.Composition.ExpeditionSystem.TryStartExpedition(targetSite, context, out expedition);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    public void Start()
    {
        if (bootstrapFailed
            || (runtimeAdmissionProfile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                && bootstrapStartAttempted))
            return;

        if (runtimeAdmissionProfile != SimulationRuntimeAdmissionProfile.None
            && runtimeAdmissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            bootstrapFailed = true;
            throw new System.InvalidOperationException("The selected runtime-admission profile is not supported.");
        }

        if (runtimeAdmissionProfile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            bootstrapStartAttempted = true;
            if (!dailyV1Application.TryCaptureBootstrapAdmissionContext(
                    out runtimeAdmissionContext))
            {
                bootstrapFailed = true;
                throw new System.InvalidOperationException(
                    "The Core Daily-v1 application could not capture its one-time bootstrap admission context.");
            }
        }

        if (runtimeAdmissionProfile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && TryLoadSavedDailyV1BeforeGenesis())
            return;

        InitializeSimulation();
    }

    private bool TryLoadSavedDailyV1BeforeGenesis()
        => TryLoadSavedDailyV1BeforeGenesis(UnityEngine.Application.persistentDataPath);

    internal bool TryLoadSavedDailyV1ContinuationFromPathForTest(string persistentDataPath)
        => TryLoadSavedDailyV1BeforeGenesis(persistentDataPath);

    internal bool TryLoadSavedDailyV1ContinuationFromPathForTest(string persistentDataPath,
        IAtomicFileSlotOperations fileOperations)
        => TryLoadSavedDailyV1BeforeGenesis(persistentDataPath, fileOperations);

    private bool TryLoadSavedDailyV1BeforeGenesis(string persistentDataPath)
        => TryLoadSavedDailyV1BeforeGenesis(persistentDataPath, SystemAtomicFileSlotOperations.Instance);

    private bool TryLoadSavedDailyV1BeforeGenesis(string persistentDataPath,
        IAtomicFileSlotOperations fileOperations)
    {
        if (!P12AContinuationStore.TryHasSlot(persistentDataPath, fileOperations, out bool exists, out _))
        {
            bootstrapFailed = true;
            UnityEngine.Debug.LogError("P12-A continuation slot inspection failed; genesis was not started.");
            return true;
        }
        if (!exists) return false;

        if (!TryCreateCurrentDailyV1CompatibilityDescriptor(
                out P12ACompatibilityDescriptor compatibility, out string compatibilityDiagnostic))
        {
            bootstrapFailed = true;
            UnityEngine.Debug.LogError("P12-A continuation is present but the current host profile cannot prove compatibility; genesis was not started. "
                + compatibilityDiagnostic);
            return true;
        }
        if (!P12AContinuationStore.TryLoad(null, compatibility, persistentDataPath, fileOperations,
                out P12AContinuationImage image, out P12AResult loadResult))
        {
            bootstrapFailed = true;
            UnityEngine.Debug.LogError("P12-A continuation load failed; genesis was not started. "
                + (loadResult?.Code.ToString() ?? P12AResultCode.StorageFailure.ToString()));
            return true;
        }

        EffectiveSimulationConfiguration effective;
        CalendarDefinition calendar;
        DailyV1EffectiveDefinitions currentDefinitions;
        try
        {
            SimulationGenesisPipeline.ValidateProfile(simulationConfig);
            ValidateP14SourceAdmission(simulationConfig, runtimeAdmissionContext);
            effective = ResolveRuntimeConfiguration();
            calendar = ResolveCalendarDefinition();
            currentDefinitions = SimulationGenesisPipeline.CreateSelectedDefinitions(simulationConfig, effective, calendar);
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            bootstrapFailed = true;
            UnityEngine.Debug.LogError("P12-A continuation host profile failed validation; genesis was not started. "
                + exception.Message);
            return true;
        }

        if (!P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                image, compatibility, simulationConfig, effective, calendar, currentDefinitions,
                runtimeAdmissionContext, out SimulationActiveSession candidate,
                out P12GDailyV1RestoreFailure restoreFailure, out string restoreDiagnostic))
        {
            bootstrapFailed = true;
            UnityEngine.Debug.LogError("P12-A detached continuation restoration failed; genesis was not started. "
                + restoreFailure + ": " + restoreDiagnostic);
            return true;
        }
        if (!TryPublishRestoredSession(null, candidate))
        {
            bootstrapFailed = true;
            UnityEngine.Debug.LogError("P12-A detached continuation passed private validation but bootstrap publication failed; genesis was not started.");
            return true;
        }

        ReleaseBootstrapAliases();

        return true;
    }

    /// <summary>Saves the current completed Daily-v1 boundary to the single local continuation slot.</summary>
    public bool TrySaveDailyV1Continuation(out string resultCode)
        => TrySaveDailyV1ContinuationAtPath(UnityEngine.Application.persistentDataPath, out resultCode);

    internal bool TrySaveDailyV1ContinuationToPathForTest(string persistentDataPath, out string resultCode)
        => TrySaveDailyV1ContinuationAtPath(persistentDataPath, out resultCode);

    private bool TrySaveDailyV1ContinuationAtPath(string persistentDataPath, out string resultCode)
    {
        resultCode = P12AResultCode.Busy.ToString();
        if (!TryBeginActiveSessionOperation(
                DailyV1OperationScopeKind.ContinuationSave,
                out SimulationActiveSession session))
            return false;
        try
        {
            bool saved = TrySaveCompletedDailyV1Boundary(session, persistentDataPath, out P12AResult result);
            resultCode = result?.Code.ToString() ?? (saved
                ? P12AResultCode.Saved.ToString()
                : P12AResultCode.CaptureRejected.ToString());
            return saved;
        }
        finally
        {
            EndActiveSessionOperation(DailyV1OperationScopeKind.ContinuationSave);
        }
    }
    private bool TrySaveCompletedDailyV1Boundary(SimulationActiveSession session, out P12AResult result)
        => TrySaveCompletedDailyV1Boundary(session, UnityEngine.Application.persistentDataPath, out result);

    private bool TrySaveCompletedDailyV1Boundary(
        SimulationActiveSession session,
        string persistentDataPath,
        out P12AResult result)
    {
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (session == null
            || session.AdmissionContext == null
            || session.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !session.AdmissionContext.IsOwnedByCurrentThread())
        {
            result = new P12AResult(P12AResultCode.WrongThread);
            return false;
        }
        if (!session.Runtime.IsHealthyDailyOwnerThreadBoundary())
        {
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        if (!P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureImage(
                session,
                runtimeAdmissionContext,
                out P12AContinuationImage image,
                out _,
                out P12GDailyV1RestoreFailure captureFailure,
                out string captureDiagnostic))
        {
            result = new P12AResult(P12AResultCode.CaptureRejected);
            session.Logger.LogError("P12-A continuation save failed: " + result.Code
                + " (" + captureFailure + ": " + captureDiagnostic + ").");
            return false;
        }

        if (image == null
            || !P12ADailyV1Census.TryCreateForImage(
                image,
                out P12ADailyV1Census census,
                out result)
            || !P12AContinuationCodec.TryEncode(
                image,
                census.Manifest,
                out byte[] bytes,
                out result))
        {
            if (result == null) result = new P12AResult(P12AResultCode.CaptureRejected);
            session.Logger.LogError("P12-A continuation save failed: " + result.Code + ".");
            return false;
        }

        if (!P12AContinuationStore.TrySave(bytes, census.Manifest, image.Header.Compatibility, persistentDataPath, out result))
        {
            session.Logger.LogError("P12-A continuation save failed: " + result.Code + ".");
            return false;
        }

        return true;
    }

    public void Update()
    {
        if (bootstrapFailed)
            return;

        SimulationActiveSession session = CaptureActiveSession();
        if (session?.AdmissionContext != null && !session.AdmissionContext.IsOwnedByCurrentThread())
        {
            session.Runtime.FaultRuntimeAdmission();
            return;
        }

        if (session != null && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            SimulateWithSession(session, Mathf.Max(1, daysToSimulate));
        }
    }

    private void InitializeSimulation(System.Action<string> stageCompleted = null)
    {
        bool selectedAdmissionProfile = runtimeAdmissionProfile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1;
        if (bootstrapFailed || CaptureActiveSession() != null || draftComposition != null) return;
        if (selectedAdmissionProfile && runtimeAdmissionContext == null)
        {
            bootstrapFailed = true;
            throw new System.InvalidOperationException(
                "The selected daily runtime-admission profile must be initialized through Start so its owner thread is captured.");
        }
        if (runtimeAdmissionContext != null && !IsBootstrapStartThreadCurrent())
        {
            bootstrapFailed = true;
            simulationRuntime?.FaultRuntimeAdmission();
            throw new System.InvalidOperationException(
                "Simulation bootstrap must remain on the Unity Start thread captured for the selected profile.");
        }

        string profileFingerprint = null;
        string selectedP9ProfileFingerprint = null;
        System.Collections.Generic.IReadOnlyList<string> profileProvenanceRecords = null;
        bool includeP10BGeneratedRuin = SimulationGenesisPipeline.IsP10BGeneratedRuinEnabled(simulationConfig);
        bool includeP10RuinLocalTopology = P10RuinLocalTopologyGenesis.IsEnabled(simulationConfig)
            && !includeP10BGeneratedRuin;
        bool includeAuthoredGeography = simulationConfig != null && simulationConfig.useAuthoredGeographyProfile;
        EffectiveSimulationConfiguration stagedEffectiveConfiguration = null;
        CalendarDefinition stagedCalendarDefinition = null;
        DailyV1EffectiveDefinitions stagedSelectedDefinitions = null;
        DailyV1DefinitionAdapter.SelectedBindings stagedSelectedDefinitionBindings = null;
        DailyV1GenesisComposition selectedCoreGenesis = null;
        HashSet<string> coreGenesisCompletedStages = new HashSet<string>(System.StringComparer.Ordinal);
        Thread initializeThread = Thread.CurrentThread;
        int initializeThreadId = initializeThread.ManagedThreadId;
        try
        {
            SimulationGenesisPipeline.ExecuteStages(stageId =>
            {
                switch (stageId)
                {
                    case "p9.genesis.resolve-profile/v1":
                        // This admission guard is intentionally the first profile operation. It must
                        // precede WorldId allocation, RuntimeIdAllocator creation, profile construction,
                        // and every owner or candidate draft so a rejected profile leaves no identity.
                        if (includeP10BGeneratedRuin
                            && runtimeAdmissionContext != null
                            && runtimeAdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
                            throw new System.InvalidOperationException("P10-B generated Ruin topology is not admitted by the Unity bootstrap daily profile.");
                        if (includeP10RuinLocalTopology
                            && runtimeAdmissionContext != null
                            && runtimeAdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
                            throw new System.InvalidOperationException("P10-A Ruin/LocalTopology is not admitted by the Unity bootstrap daily profile.");
                        ValidateP14SourceAdmission(simulationConfig, runtimeAdmissionContext);
                        SimulationGenesisPipeline.ValidateProfile(simulationConfig);
                        if (selectedAdmissionProfile)
                        {
                            // The typed graph is the selected profile's immutable definition input.
                            // Build and fingerprint it before allocating WorldId, runtime IDs, RNG, or
                            // mutable owners so an invalid graph cannot leave a partially admitted world.
                            stagedEffectiveConfiguration = ResolveRuntimeConfiguration();
                            stagedCalendarDefinition = ResolveCalendarDefinition();
                            stagedSelectedDefinitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
                                simulationConfig, stagedEffectiveConfiguration, stagedCalendarDefinition,
                                out stagedSelectedDefinitionBindings);
                            if (!SimulationGenesisPipeline.TryValidateSelectedDefinitionBindings(
                                    stagedSelectedDefinitions, out string definitionBindingDiagnostic))
                                throw new System.InvalidOperationException(
                                    "The selected Daily-v1 definition graph is not closed: " + definitionBindingDiagnostic);
                            profileFingerprint = DailyV1DefinitionIdentity.CreateP9Fingerprint(
                                stagedSelectedDefinitions, out profileProvenanceRecords);
                            selectedDefinitions = stagedSelectedDefinitions;
                            selectedDefinitionBindings = stagedSelectedDefinitionBindings;
                            selectedP9ProfileFingerprint = profileFingerprint;

                            effectiveConfiguration = stagedEffectiveConfiguration;
                            calendarDefinition = stagedCalendarDefinition;
                            lastEconomySnapshotDay = 0L;
                            logger = UnitySimulationLoggerAdapter.CreateSelected(
                                stagedSelectedDefinitions.Configuration.logSettings);
                            List<SimulationModule> selectedModules = new List<SimulationModule>();
                            foreach (DailyV1EnumValue module in stagedSelectedDefinitions.Configuration.enabledModules
                                ?? System.Array.Empty<DailyV1EnumValue>())
                                if (module != null) selectedModules.Add((SimulationModule)module.Value);
                            logger.BeginSimulation(stagedSelectedDefinitions.Configuration.simulationName,
                                selectedModules,
                                stagedSelectedDefinitions.Configuration.cities?.Count ?? 0,
                                stagedSelectedDefinitions.Configuration.npcs?.Count ?? 0);
                            enabledModules = new SimulationModuleSet(
                                stagedSelectedDefinitions.Configuration.enabledModules);
                            AppendScenarioDiagnostics(effectiveConfiguration);

                            if (!DailyV1GenesisComposer.TryComposeSelectedOwners(
                                    stagedSelectedDefinitions,
                                    runtimeAdmissionContext,
                                    worldIdentityAllocator,
                                    completedStage =>
                                    {
                                        if (!coreGenesisCompletedStages.Add(completedStage))
                                            throw new System.InvalidOperationException(
                                                "Core Daily-v1 genesis completed a stage more than once.");
                                        stageCompleted?.Invoke(completedStage);
                                    },
                                    out selectedCoreGenesis,
                                    out string coreGenesisDiagnostic))
                                throw new System.InvalidOperationException(
                                    "Core Daily-v1 genesis failed: " + coreGenesisDiagnostic);

                            profileFingerprint = selectedCoreGenesis.P9ProfileFingerprint;
                            selectedP9ProfileFingerprint = profileFingerprint;
                            profileProvenanceRecords = selectedCoreGenesis.P9ProvenanceRecords;
                            unpublishedWorldId = selectedCoreGenesis.WorldId;
                            if (!selectedCoreGenesis.ExecutionBoundaryState.TryBindSelectedRuntimeOwnerGraph(
                                    selectedCoreGenesis.OwnerGraph))
                                throw new System.InvalidOperationException(
                                    "Core Daily-v1 genesis did not retain its exact selected owner graph.");
                            ProjectSelectedCoreGenesis(selectedCoreGenesis);
                            break;
                        }
                        unpublishedWorldId = worldIdentityAllocator?.Invoke();
                        if (unpublishedWorldId == null)
                            throw new System.InvalidOperationException("World identity allocation returned no identity.");
                        simulationTime = new SimulationTime();
                        DailyV1SimulationConfigData selectedConfig = stagedSelectedDefinitions?.Configuration;
                        bool useFixedSeed = selectedConfig != null
                            ? selectedConfig.useFixedSimulationSeed
                            : simulationConfig.useFixedSimulationSeed;
                        int selectedSeed = selectedConfig != null
                            ? selectedConfig.simulationSeed
                            : simulationConfig.simulationSeed;
                        authoritativeRandomSource = new DeterministicRandomSource(useFixedSeed ? selectedSeed : 0);
                        lastEconomySnapshotDay = 0;
                        logger = selectedConfig != null
                            ? UnitySimulationLoggerAdapter.CreateSelected(selectedConfig.logSettings)
                            : UnitySimulationLoggerAdapter.Create(simulationConfig.LogSettings);
                        if (selectedConfig != null)
                        {
                            List<SimulationModule> selectedModules = new List<SimulationModule>();
                            foreach (DailyV1EnumValue module in selectedConfig.enabledModules ?? System.Array.Empty<DailyV1EnumValue>())
                                if (module != null) selectedModules.Add((SimulationModule)module.Value);
                            logger.BeginSimulation(selectedConfig.simulationName, selectedModules,
                                selectedConfig.cities?.Count ?? 0, selectedConfig.npcs?.Count ?? 0);
                        }
                        else
                        {
                            logger.BeginSimulation(simulationConfig.simulationName, simulationConfig.EnabledModules,
                                simulationConfig.Cities.Count, simulationConfig.Npcs.Count);
                        }
                        calendarDefinition = stagedCalendarDefinition ?? ResolveCalendarDefinition();
                        enabledModules = stagedSelectedDefinitions != null
                            ? new SimulationModuleSet(stagedSelectedDefinitions.Configuration.enabledModules)
                            : new SimulationModuleSet(simulationConfig, logger);
                        effectiveConfiguration = stagedEffectiveConfiguration ?? ResolveRuntimeConfiguration();
                        genesisSpatialAuthority = new SpatialAuthorityStore();
                        if (stagedSelectedDefinitions == null)
                        {
                            profileFingerprint = SimulationGenesisPipeline.CreateFingerprint(
                                simulationConfig, effectiveConfiguration, calendarDefinition,
                                out profileProvenanceRecords, out selectedDefinitions);
                            selectedP9ProfileFingerprint = profileFingerprint;
                        }
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
                        scheduledDirectiveStore = new ScheduledDirectiveStore(simulationTime, logger);
                        runtimeIdentityRegistry = new RuntimeIdentityRegistry(logger);
                        spatialNetwork = new SpatialNetworkRuntime(runtimeIdentityRegistry, logger);
                        break;
                    case "p9.genesis.authored-world/v1":
                        if (selectedCoreGenesis != null) break;
                        CityRuntimeList.Clear();
                        cityRuntimesByDefinition.Clear();
                        cityRuntimeByLocation.Clear();
                        CreateCityRuntimes(stagedSelectedDefinitions, stagedSelectedDefinitionBindings);
                        explorableSiteStore = new ExplorableSiteStore();
                        explorableSiteRuntimesByDefinition.Clear();
                        CreateExplorableSiteRuntimes();
                        CreateSpatialRoutes();
                        break;
                    case SimulationGenesisPipeline.GeographyStageId:
                        if (selectedCoreGenesis != null) break;
                        ComposeAuthoredGeography();
                        break;
                    case SimulationGenesisPipeline.P10BGeneratedRuinStageId:
                        p10BPreparedRuin = P10BGeneratedRuinGenesis.Prepare(
                            simulationConfig, selectedP9ProfileFingerprint, selectedDefinitions, genesisSpatialAuthority,
                            runtimeIdentityRegistry, out string p10BDiagnostic);
                        if (p10BPreparedRuin == null)
                            throw new System.InvalidOperationException("P10-B generated Ruin preparation failed: " + p10BDiagnostic);
                        genesisLocalTopologyStore = new LocalTopologyStore(runtimeIdentityRegistry);
                        genesisLegacySpatialAnchorBindingStore = new LegacySpatialAnchorBindingStore(genesisSpatialAuthority);
                        profileFingerprint = SimulationGenesisPipeline.CreateP10BCombinedFingerprint(
                            selectedP9ProfileFingerprint, profileProvenanceRecords, p10BPreparedRuin,
                            out System.Collections.Generic.IReadOnlyList<string> p10BProvenanceRecords);
                        profileProvenanceRecords = p10BProvenanceRecords;
                        break;
                    case "p9.genesis.authored-actors/v1":
                        if (selectedCoreGenesis != null) break;
                        NpcRuntimeList.Clear();
                        npcRuntimesByDefinition.Clear();
                        CreateNpcRuntimes(stagedSelectedDefinitions, stagedSelectedDefinitionBindings);
                        CreateScheduledDirectives();
                        scheduledDirectiveSystem = new ScheduledDirectiveSystem(scheduledDirectiveStore, runtimeIdentityRegistry, logger);
                        economyTransactionService = new EconomyTransactionService();
                        expeditionStore = new ExpeditionStore();
                        break;
                    case P10RuinLocalTopologyGenesis.StageId:
                        ComposeP10RuinLocalTopology();
                        profileFingerprint = SimulationGenesisPipeline.CreateP10CombinedFingerprint(
                            selectedP9ProfileFingerprint,
                            profileProvenanceRecords,
                            p10RuinCandidate,
                            out System.Collections.Generic.IReadOnlyList<string> combinedProvenanceRecords);
                        profileProvenanceRecords = combinedProvenanceRecords;
                        break;
                    case "p9.genesis.validate-profile/v1":
                        RebuildSystems(effectiveConfiguration,
                            selectedCoreGenesis?.OwnerGraph?.JusticeRecordsOwner);
                        if (selectedCoreGenesis == null)
                        {
                            BootstrapInitialSpatialKnowledge();
                            BootstrapInitialExplorableSiteKnowledge();
                            BootstrapInitialCommercialKnowledge();
                            InitializeJusticeState();
                        }
                        if (selectedCoreGenesis == null
                            && selectedRuntimeOwnerGraphBuilder != null
                            && !selectedRuntimeOwnerGraphBuilder.TryBuild(
                                justiceSystem?.CoreOwner, travelPartyStore, scheduledDirectiveStore,
                                out selectedInitialRuntimeOwnerGraph))
                            throw new System.InvalidOperationException(
                                "The selected Core City/NPC/Justice owner graph does not exactly match the admitted Daily-v1 genesis composition.");
                        if (selectedCoreGenesis != null)
                        {
                            selectedInitialRuntimeOwnerGraph = selectedCoreGenesis.OwnerGraph;
                            if (!selectedInitialRuntimeOwnerGraph.TryBindExpeditionStore(expeditionStore))
                                throw new System.InvalidOperationException(
                                    "The Core selected owner graph could not bind the exact empty P10 ExpeditionStore.");
                        }
                        simulationRuntime = new SimulationRuntime(
                            simulationTime: simulationTime, cities: CityRuntimeList, npcRuntimes: NpcRuntimeList,
                            configuredActions: ConfiguredActions, scheduledDirectiveSystem: scheduledDirectiveSystem,
                            justiceSystem: justiceSystem, crimeSystem: crimeSystem, npcDecisionSystem: npcDecisionSystem,
                            travelSystem: travelSystem, travelPartySystem: travelPartySystem, merchantSystem: merchantSystem,
                            commercialKnowledgeSharingSystem: commercialKnowledgeSharingSystem, decisionRecorder: decisionRecorder,
                            logger: logger, explorableSiteStore: explorableSiteStore,
                            explorableSiteKnowledgeSystem: explorableSiteKnowledgeSystem, expeditionSystem: expeditionSystem,
                            configuration: effectiveConfiguration, randomSource: authoritativeRandomSource,
                            personStore: selectedCoreGenesis?.PersonStore,
                            actorChoiceStore: selectedCoreGenesis?.ActorChoiceStore,
                            calendarDefinition: calendarDefinition,
                            spatialAuthorityStore: genesisSpatialAuthority,
                            localTopologyStore: genesisLocalTopologyStore,
                            legacySpatialAnchorBindingStore: genesisLegacySpatialAnchorBindingStore,
                            runtimeAdmissionContext: runtimeAdmissionContext,
                            recordSequence: recordSequence,
                            worldId: unpublishedWorldId,
                            runtimeIdAllocator: runtimeIdAllocator,
                            economyTransactionService: economyTransactionService,
                            requireP12ReceiptCensusOwners: runtimeAdmissionContext != null,
                            runtimeIdentityRegistry: runtimeIdentityRegistry,
                            spatialNetworkRuntime: spatialNetwork,
                            requireP12RuntimeIdentitySpatialCensusOwners: runtimeAdmissionContext != null,
                            selectedExecutionBoundaryState: selectedCoreGenesis?.ExecutionBoundaryState,
                            selectedCalendar: selectedCoreGenesis?.Calendar);
                        if (selectedInitialRuntimeOwnerGraph != null
                            && !simulationRuntime.TryAdoptInitialSelectedRuntimeOwnerGraph(
                                selectedInitialRuntimeOwnerGraph))
                            throw new System.InvalidOperationException(
                                "The constructed runtime did not adopt the exact unpublished selected Core owner graph.");
                        if (selectedInitialRuntimeOwnerGraph != null
                            && !TryProjectInitialSelectedJusticeRows(selectedInitialRuntimeOwnerGraph))
                            throw new System.InvalidOperationException(
                                "The selected Justice rows could not bind their exact Unity reference projections.");
                        simulationRuntime.BindP12EconomyTransactionService(economyTransactionService);
                        if (runtimeAdmissionContext != null
                            && !simulationRuntime.TryBeginBootstrapPublicationScope(out bootstrapPublicationScope))
                        {
                            throw new System.InvalidOperationException(
                                "The P12 bootstrap publication scope could not begin after the initial census baseline.");
                        }
                        ValidateCandidateProfile();
                        break;
                    case "p9.genesis.publish/v1":
                        if (p10BPreparedRuin != null
                            && !P10BGeneratedRuinGenesis.TryCommitPreparedRuin(
                                p10BPreparedRuin, runtimeIdentityRegistry, spatialNetwork,
                                explorableSiteStore, genesisLocalTopologyStore, simulationRuntime.SpatialAuthorityStore,
                                simulationRuntime.LegacySpatialAnchorBindingStore, out string p10BCommitDiagnostic))
                            throw new System.InvalidOperationException("P10-B atomic topology publication failed: " + p10BCommitDiagnostic);
                        if (p10BPreparedRuin != null)
                            AddExplorableSiteRuntimeByDefinition(p10BPreparedRuin.Site.DefinitionId, p10BPreparedRuin.Site);
                        npcChronicleFormatter = CreateNpcChronicleFormatter(
                            runtimeIdentityRegistry,
                            spatialNetwork,
                            cityRuntimeByLocation,
                            explorableSiteStore,
                            CityRuntimeList,
                            simulationConfig,
                            selectedDefinitions);
                        draftComposition = new SimulationBootstrapComposition(
                            unpublishedWorldId,
                            new SimulationGenesisManifest(
                                simulationConfig, effectiveConfiguration, calendarDefinition,
                                selectedP9ProfileFingerprint, profileFingerprint, profileProvenanceRecords,
                                includeP10RuinLocalTopology,
                                includeP10BGeneratedRuin,
                                stagedSelectedDefinitions), simulationTime, calendarDefinition, spatialNetwork, domainEventStore,
                            historyStore, scheduledDirectiveStore, decisionStore, decisionRecorder, recordSequence, economyTransactionService, npcChronicleService,
                            npcChronicleFormatter, travelPartyStore, travelPartySystem, simulationRuntime,
                            runtimeIdentityRegistry,
                            runtimeIdAllocator,
                            explorableSiteStore, expeditionStore, expeditionSystem, selectedDefinitions);
                        break;
                    default:
                        throw new System.InvalidOperationException("Undeclared authored genesis stage: " + stageId);
                }
                if (!coreGenesisCompletedStages.Contains(stageId))
                    stageCompleted?.Invoke(stageId);
            }, includeAuthoredGeography, includeP10RuinLocalTopology, includeP10BGeneratedRuin);

            if (draftComposition == null)
                throw new System.InvalidOperationException("Simulation genesis returned without creating its private composition draft.");
            if (!ReferenceEquals(Thread.CurrentThread, initializeThread)
                || Thread.CurrentThread.ManagedThreadId != initializeThreadId)
                throw new System.InvalidOperationException("Simulation genesis completed on a thread other than its composition-entry thread.");
            if (runtimeAdmissionContext != null && !IsBootstrapStartThreadCurrent())
                throw new System.InvalidOperationException("Simulation bootstrap completed on a thread other than its captured Unity Start thread.");

            if (bootstrapPublicationScope != null)
            {
                bootstrapPublicationScope.Dispose();
                bootstrapPublicationScope = null;
                ContinuationCensusFailure closeFailure = ContinuationCensusFailure.OwnerCoverageIncomplete;
                if (simulationRuntime == null
                    || !simulationRuntime.TryAssessNpcRosterCensus(out closeFailure))
                    throw new System.InvalidOperationException(
                        "The P12 bootstrap publication scope did not close with healthy registered-owner quiescence: " + closeFailure);
            }

            if (simulationRuntime == null || !simulationRuntime.TryMarkWorldPublishedForFactualRead())
                throw new System.InvalidOperationException(
                    "The FR-B factual-read surface could not bind to a healthy completed world publication.");

            SimulationActiveSession candidateSession = new SimulationActiveSession(
                draftComposition,
                logger,
                justiceSystem,
                runtimeIdentityRegistry,
                spatialNetwork,
                cityRuntimeByLocation,
                simulationConfig,
                runtimeAdmissionContext,
                authoritativeRandomSource,
                lastEconomySnapshotDay);

            unpublishedWorldId = null;
            draftComposition = null;
            if (!TryPublishInitialActiveSession(candidateSession))
                throw new System.InvalidOperationException("A simulation session was already published for this bootstrap owner.");
            ReleaseBootstrapAliases();
        }
        catch
        {
            bootstrapFailed = true;
            unpublishedWorldId = null;
            draftComposition = null;
            if (runtimeAdmissionContext != null)
                simulationRuntime?.FaultRuntimeAdmission();
            throw;
        }
        finally
        {
            if (bootstrapPublicationScope != null)
            {
                simulationRuntime?.FaultRuntimeAdmission();
                bootstrapPublicationScope.Dispose();
                bootstrapPublicationScope = null;
            }
        }
    }

    private bool IsBootstrapStartThreadCurrent()
    {
        return runtimeAdmissionContext != null
            && runtimeAdmissionContext.IsOwnedByCurrentThread();
    }

    private SimulationActiveSession CaptureActiveSession()
    {
        lock (dailyV1Application.ActiveSessionSyncRoot)
        {
            DailyV1ActiveRuntimeSession coreSession = dailyV1Application.ActiveSession;
            if (coreSession != null)
                return activeSessionProjections.TryGetValue(coreSession, out SimulationActiveSession projection)
                    ? projection
                    : null;

            return compatibilitySessionGate.Current;
        }
    }

    private bool TryBeginActiveSessionOperation(out SimulationActiveSession session) =>
        TryBeginActiveSessionOperation(
            expectedSession: null,
            DailyV1OperationScopeKind.Application,
            out session);

    private bool TryBeginActiveSessionOperation(
        DailyV1OperationScopeKind scopeKind,
        out SimulationActiveSession session) =>
        TryBeginActiveSessionOperation(expectedSession: null, scopeKind, out session);

    private bool TryBeginActiveSessionOperation(
        SimulationActiveSession expectedSession,
        out SimulationActiveSession session) =>
        TryBeginActiveSessionOperation(
            expectedSession,
            DailyV1OperationScopeKind.Application,
            out session);

    private bool TryBeginActiveSessionOperation(
        SimulationActiveSession expectedSession,
        DailyV1OperationScopeKind scopeKind,
        out SimulationActiveSession session)
    {
        lock (dailyV1Application.ActiveSessionSyncRoot)
        {
            session = null;
            DailyV1ActiveRuntimeSession currentCoreSession = dailyV1Application.ActiveSession;
            DailyV1ActiveRuntimeSession expectedCoreSession = expectedSession?.CoreRuntimeSession;
            if (currentCoreSession != null || expectedCoreSession != null)
            {
                if (expectedSession != null && expectedCoreSession == null
                    || currentCoreSession == null
                    || !activeSessionProjections.TryGetValue(currentCoreSession, out session)
                    || !dailyV1Application.TryBeginOperation(
                        expectedCoreSession, scopeKind, out DailyV1ActiveRuntimeSession admittedCoreSession))
                {
                    session = null;
                    return false;
                }
                if (!ReferenceEquals(admittedCoreSession, currentCoreSession))
                {
                    dailyV1Application.EndOperation(scopeKind);
                    session = null;
                    return false;
                }
                return true;
            }

            if (dailyV1Application.ActiveSession != null
                || !compatibilitySessionGate.TryBeginOperation(expectedSession, scopeKind, out session))
            {
                session = null;
                return false;
            }
            return true;
        }
    }

    private void EndActiveSessionOperation(
        DailyV1OperationScopeKind scopeKind = DailyV1OperationScopeKind.Application)
    {
        lock (dailyV1Application.ActiveSessionSyncRoot)
        {
            if (dailyV1Application.ActiveSession != null)
                dailyV1Application.EndOperation(scopeKind);
            else
                compatibilitySessionGate.EndOperation(scopeKind);
        }
    }

    private bool TryPublishInitialActiveSession(SimulationActiveSession candidateSession)
    {
        if (candidateSession == null) return false;
        lock (dailyV1Application.ActiveSessionSyncRoot)
        {
            if (dailyV1Application.ActiveSession != null || compatibilitySessionGate.Current != null)
                return false;

            DailyV1ActiveRuntimeSession coreSession = candidateSession.CoreRuntimeSession;
            if (coreSession == null)
                return compatibilitySessionGate.TryPublishInitial(candidateSession);

            if (activeSessionProjections.ContainsKey(coreSession)) return false;
            activeSessionProjections.Add(coreSession, candidateSession);
            if (dailyV1Application.TryPublishInitial(coreSession)) return true;

            activeSessionProjections.Remove(coreSession);
            return false;
        }
    }

    /// <summary>Single-reference exchange seam for a privately built, restored Daily-v1 session.</summary>
    internal bool TryPublishRestoredSession(
        SimulationActiveSession expectedSession,
        SimulationActiveSession candidateSession)
    {
        return TryPublishRestoredSession(expectedSession, candidateSession, null);
    }

    private bool TryPublishRestoredSession(
        SimulationActiveSession expectedSession,
        SimulationActiveSession candidateSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation exclusiveOperation)
    {
        if (candidateSession == null)
            return false;
        if (candidateSession.AdmissionContext == null
            || !candidateSession.AdmissionContext.IsOwnedByCurrentThread()
            || !candidateSession.AdmissionContext.IsRestoredContinuation
            || !ReferenceEquals(candidateSession.Configuration, simulationConfig)
            || !candidateSession.Runtime.IsHealthyDailyOwnerThreadBoundary()
            || !candidateSession.Runtime.HasValidRestoredDailyBoundaryAdmission())
            return false;

        lock (dailyV1Application.ActiveSessionSyncRoot)
        {
            if (candidateSession.CoreRuntimeSession == null)
            {
                if (exclusiveOperation != null || dailyV1Application.ActiveSession != null
                    || compatibilitySessionGate.ActiveOperationCount != 0)
                    return false;

                if (expectedSession == null)
                {
                    if (compatibilitySessionGate.Current != null || runtimeAdmissionContext == null
                        || runtimeAdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                        || runtimeAdmissionContext.IsRestoredContinuation
                        || !runtimeAdmissionContext.IsOwnedByCurrentThread())
                        return false;
                    return compatibilitySessionGate.TryPublishInitial(candidateSession);
                }

                return ReferenceEquals(compatibilitySessionGate.Current, expectedSession)
                    && expectedSession.AdmissionContext != null
                    && expectedSession.AdmissionContext.IsOwnedByCurrentThread()
                    && expectedSession.IsProfileCompatibleWith(candidateSession)
                    && expectedSession.Runtime.IsHealthyDailyOwnerThreadBoundary()
                    && expectedSession.Runtime.HasValidCompletedDailyBoundaryAdmission()
                    && compatibilitySessionGate.TryPublishReplacement(expectedSession, candidateSession);
            }

            DailyV1ActiveRuntimeSession candidateCoreSession = candidateSession.CoreRuntimeSession;
            DailyV1ActiveRuntimeSession expectedCoreSession = expectedSession?.CoreRuntimeSession;
            if (compatibilitySessionGate.Current != null
                || dailyV1Application.ActiveOperationCount != 0)
                return false;

            if (exclusiveOperation != null
                && !ReferenceEquals(exclusiveOperation.ExpectedSession, expectedCoreSession))
                return false;

            if (expectedSession == null)
            {
                if (dailyV1Application.ActiveSession != null || runtimeAdmissionContext == null
                    || runtimeAdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                    || runtimeAdmissionContext.IsRestoredContinuation
                    || !runtimeAdmissionContext.IsOwnedByCurrentThread())
                    return false;
                return TryPublishCoreInitialWithProjection(candidateCoreSession, candidateSession);
            }

            if (expectedCoreSession == null
                || !ReferenceEquals(dailyV1Application.ActiveSession, expectedCoreSession)
                || expectedSession.AdmissionContext == null
                || !expectedSession.AdmissionContext.IsOwnedByCurrentThread()
                || !expectedSession.IsProfileCompatibleWith(candidateSession)
                || !expectedSession.Runtime.IsHealthyDailyOwnerThreadBoundary()
                || !expectedSession.Runtime.HasValidCompletedDailyBoundaryAdmission())
                return false;

            return TryPublishCoreReplacementWithProjection(
                expectedCoreSession, candidateCoreSession, expectedSession, candidateSession, exclusiveOperation);
        }
    }

    private bool TryPublishCoreInitialWithProjection(
        DailyV1ActiveRuntimeSession coreSession,
        SimulationActiveSession projection)
    {
        if (coreSession == null || projection == null || activeSessionProjections.ContainsKey(coreSession))
            return false;
        activeSessionProjections.Add(coreSession, projection);
        if (dailyV1Application.TryPublishInitial(coreSession)) return true;
        activeSessionProjections.Remove(coreSession);
        return false;
    }

    private bool TryPublishCoreReplacementWithProjection(
        DailyV1ActiveRuntimeSession expectedCoreSession,
        DailyV1ActiveRuntimeSession candidateCoreSession,
        SimulationActiveSession expectedProjection,
        SimulationActiveSession candidateProjection,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation exclusiveOperation)
    {
        if (expectedCoreSession == null || candidateCoreSession == null || candidateProjection == null
            || !activeSessionProjections.TryGetValue(expectedCoreSession, out SimulationActiveSession currentProjection)
            || !ReferenceEquals(currentProjection, expectedProjection)
            || activeSessionProjections.ContainsKey(candidateCoreSession))
            return false;

        // Register the host projection before the Core exchange. Both writes
        // are under the same gate lock, so readers can observe neither a
        // published Core session without its adapter nor a stale projection.
        activeSessionProjections.Add(candidateCoreSession, candidateProjection);
        bool published = exclusiveOperation != null
            ? dailyV1Application.TryPublishReplacement(exclusiveOperation, candidateCoreSession)
            : dailyV1Application.TryPublishReplacement(expectedCoreSession, candidateCoreSession);
        if (!published)
        {
            activeSessionProjections.Remove(candidateCoreSession);
            return false;
        }

        activeSessionProjections.Remove(expectedCoreSession);
        return true;
    }

    /// <summary>
    /// Restores the selected Daily-v1 boundary while holding the serialized
    /// active-session gate, then publishes exactly one fully validated session.
    /// </summary>
    internal bool TryRestoreDailyContinuation(
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuation(null, out failure, out diagnostic);
    }

    /// <summary>Loads and privately reconstructs the durable Daily-v1 slot.</summary>
    public bool TryLoadDailyV1Continuation(out string resultCode)
        => TryLoadDailyV1ContinuationAtPath(UnityEngine.Application.persistentDataPath, out resultCode);

    internal bool TryLoadDailyV1ContinuationFromPathForTest(string persistentDataPath, out string resultCode)
        => TryLoadDailyV1ContinuationAtPath(persistentDataPath, out resultCode);

    private bool TryLoadDailyV1ContinuationAtPath(string persistentDataPath, out string resultCode)
    {
        resultCode = P12AResultCode.Busy.ToString();
        SimulationActiveSession expectedSession = CaptureActiveSession();
        if (expectedSession == null
            || expectedSession.CoreRuntimeSession == null
            || !dailyV1Application.TryBeginExclusiveOperation(
                expectedSession.CoreRuntimeSession,
                out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation exclusiveOperation))
            return false;

        using (exclusiveOperation)
        {
            if (dailyV1Application.ActiveOperationCount != 0
                || expectedSession.AdmissionContext == null
                || expectedSession.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || runtimeAdmissionContext == null || runtimeAdmissionContext.IsRestoredContinuation
                || !runtimeAdmissionContext.IsOwnedByCurrentThread()
                || !expectedSession.Runtime.IsHealthyDailyOwnerThreadBoundary()
                || !expectedSession.Runtime.HasValidCompletedDailyBoundaryAdmission())
            {
                resultCode = P12AResultCode.Busy.ToString();
                return false;
            }

            if (!TryCreateCurrentDailyV1CompatibilityDescriptor(
                    out P12ACompatibilityDescriptor compatibility, out _))
            {
                resultCode = P12AResultCode.IncompatibleContentOrConfiguration.ToString();
                return false;
            }
            if (!P12AContinuationStore.TryLoad(null, compatibility, persistentDataPath,
                    out P12AContinuationImage image, out P12AResult loadResult))
            {
                resultCode = loadResult?.Code.ToString() ?? P12AResultCode.StorageFailure.ToString();
                return false;
            }

            if (!P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                    image, compatibility, expectedSession.Configuration,
                    expectedSession.Runtime.Configuration, expectedSession.Composition.Calendar,
                    expectedSession.Composition.SelectedDefinitions,
                    runtimeAdmissionContext, out SimulationActiveSession candidate,
                    out P12GDailyV1RestoreFailure restoreFailure, out string restoreDiagnostic))
            {
                resultCode = P12AResultCode.OwnerGraphRejected.ToString();
                expectedSession.Logger.LogError("P12-A detached load failed: " + restoreFailure + ": " + restoreDiagnostic);
                return false;
            }

            if (!TryPublishRestoredSession(expectedSession, candidate, exclusiveOperation))
            {
                resultCode = P12AResultCode.AdmissionRejected.ToString();
                expectedSession.Logger.LogError("P12-A detached load passed private validation but publication failed.");
                return false;
            }

            ReleaseBootstrapAliases();
            resultCode = P12AResultCode.Loaded.ToString();
            return true;
        }
    }
    internal bool TryRestoreDailyContinuation(
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(stageObserver, null, out failure, out diagnostic);
    }

    internal bool TryRestoreDailyContinuationForTest(
        System.Action<SimulationActiveSession> privateCandidateObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(null, privateCandidateObserver, out failure, out diagnostic);
    }

    internal bool TryRestoreDailyContinuationWithGenealogySnapshotForTest(
        System.Func<GenealogyOwnerSnapshot, GenealogyOwnerSnapshot> snapshotOverride,
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(
            stageObserver, null, out failure, out diagnostic, snapshotOverride);
    }

    internal bool TryRestoreDailyContinuationWithFactionSnapshotForTest(
        System.Func<P12EFactionOwnerSnapshot, P12EFactionOwnerSnapshot> snapshotOverride,
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(
            stageObserver, null, out failure, out diagnostic,
            factionSnapshotOverrideForTest: snapshotOverride);
    }

    internal bool TryRestoreDailyContinuationWithPropertyEstateSnapshotForTest(
        System.Func<PropertyEstateOwnerSnapshot, PropertyEstateOwnerSnapshot> snapshotOverride,
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(
            stageObserver, null, out failure, out diagnostic, null, snapshotOverride);
    }

    internal bool TryRestoreDailyContinuationWithPoliticalSupportSnapshotForTest(
        System.Func<P12EPoliticalSupportOwnerSnapshot, P12EPoliticalSupportOwnerSnapshot> snapshotOverride,
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(
            stageObserver, null, out failure, out diagnostic, null, null, null, snapshotOverride);
    }

    internal bool TryRestoreDailyContinuationWithPoliticalClaimSnapshotForTest(
        System.Func<P12EPoliticalClaimOwnerSnapshot, P12EPoliticalClaimOwnerSnapshot> snapshotOverride,
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(
            stageObserver, null, out failure, out diagnostic, null, null, snapshotOverride);
    }

    internal bool TryRestoreDailyContinuationWithActorChoiceSnapshotForTest(
        System.Func<P12FActorChoiceSnapshot, P12FActorChoiceSnapshot> snapshotOverride,
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic)
    {
        return TryRestoreDailyContinuationCore(
            stageObserver, null, out failure, out diagnostic,
            actorChoiceSnapshotOverrideForTest: snapshotOverride);
    }

    private bool TryRestoreDailyContinuationCore(
        System.Action<P12GDailyV1RestoreStage> stageObserver,
        System.Action<SimulationActiveSession> privateCandidateObserver,
        out P12GDailyV1RestoreFailure failure,
        out string diagnostic,
        System.Func<GenealogyOwnerSnapshot, GenealogyOwnerSnapshot> genealogySnapshotOverrideForTest = null,
        System.Func<PropertyEstateOwnerSnapshot, PropertyEstateOwnerSnapshot> propertyEstateSnapshotOverrideForTest = null,
        System.Func<P12EPoliticalClaimOwnerSnapshot, P12EPoliticalClaimOwnerSnapshot> politicalClaimSnapshotOverrideForTest = null,
        System.Func<P12EPoliticalSupportOwnerSnapshot, P12EPoliticalSupportOwnerSnapshot> politicalSupportSnapshotOverrideForTest = null,
        System.Func<P12EFactionOwnerSnapshot, P12EFactionOwnerSnapshot> factionSnapshotOverrideForTest = null,
        System.Func<P12FActorChoiceSnapshot, P12FActorChoiceSnapshot> actorChoiceSnapshotOverrideForTest = null)
    {
        SimulationActiveSession expectedSession = CaptureActiveSession();
        if (expectedSession == null
            || expectedSession.CoreRuntimeSession == null
            || !dailyV1Application.TryBeginExclusiveOperation(
                expectedSession.CoreRuntimeSession,
                out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation exclusiveOperation))
        {
            failure = P12GDailyV1RestoreFailure.InvalidSourceSession;
            diagnostic = "Daily-v1 restore requires the current healthy completed-boundary session while idle on its owner thread.";
            return false;
        }

        using (exclusiveOperation)
        {
            if (dailyV1Application.ActiveOperationCount != 0
                || expectedSession.AdmissionContext == null
                || expectedSession.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !expectedSession.AdmissionContext.IsOwnedByCurrentThread()
                || !expectedSession.Runtime.IsHealthyDailyOwnerThreadBoundary()
                || !expectedSession.Runtime.HasValidCompletedDailyBoundaryAdmission())
            {
                failure = P12GDailyV1RestoreFailure.InvalidSourceSession;
                diagnostic = "Daily-v1 restore requires the current healthy completed-boundary session while idle on its owner thread.";
                return false;
            }

            if (!P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    expectedSession,
                    runtimeAdmissionContext,
                    out SimulationActiveSession candidateSession,
                    out failure,
                    out diagnostic,
                    stageObserver,
                    privateCandidateObserver,
                    genealogySnapshotOverrideForTest,
                    propertyEstateSnapshotOverrideForTest,
                    politicalClaimSnapshotOverrideForTest,
                    politicalSupportSnapshotOverrideForTest,
                    factionSnapshotOverrideForTest,
                    actorChoiceSnapshotOverrideForTest,
                    replacementOperation: exclusiveOperation))
                return false;

            try
            {
                stageObserver?.Invoke(P12GDailyV1RestoreStage.BeforePublication);
            }
            catch (Exception exception) when (exception is InvalidOperationException
                || exception is ArgumentException)
            {
                failure = P12GDailyV1RestoreFailure.PublicationFailed;
                diagnostic = "The private candidate was rejected before the active-session exchange: "
                    + exception.Message;
                return false;
            }

            if (!TryPublishRestoredSession(expectedSession, candidateSession, exclusiveOperation))
            {
                failure = P12GDailyV1RestoreFailure.PublicationFailed;
                diagnostic = "The restored session failed the existing single-reference publication gate.";
                return false;
            }

            failure = P12GDailyV1RestoreFailure.None;
            diagnostic = null;
            return true;
        }
    }

    private void ReleaseBootstrapAliases()
    {
        npcRuntimeList = null;
        cityRuntimeList = null;
        explorableSiteStore = null;
        genesisLocalTopologyStore = null;
        genesisLegacySpatialAnchorBindingStore = null;
        p10RuinCandidate = null;
        p10RuinRuntime = null;
        p10BPreparedRuin = null;
        actionProviders.Clear();
        cityRuntimesByDefinition = null;
        npcRuntimesByDefinition = null;
        explorableSiteRuntimesByDefinition = null;
        explorableSiteKnowledgeSystem = null;
        expeditionStore = null;
        cityRuntimeByLocation = null;
        runtimeIdAllocator = null;
        runtimeIdentityRegistry = null;
        spatialNetwork = null;
        genesisSpatialAuthority = null;
        domainEventStore = null;
        historyStore = null;
        domainEventRecorder = null;
        recordSequence = null;
        decisionStore = null;
        decisionRecorder = null;
        npcChronicleService = null;
        npcChronicleFormatter = null;
        scheduledDirectiveStore = null;
        scheduledDirectiveSystem = null;
        enabledModules = null;
        justiceSystem = null;
        crimeSystem = null;
        npcDecisionSystem = null;
        travelSystem = null;
        travelPartyStore = null;
        travelPartySystem = null;
        expeditionSystem = null;
        merchantSystem = null;
        commercialKnowledgeSharingSystem = null;
        economyTransactionService = null;
        logger = null;
        simulationRuntime = null;
        simulationTime = null;
        calendarDefinition = null;
        effectiveConfiguration = null;
        authoritativeRandomSource = null;
        bootstrapPublicationScope = null;
        lastEconomySnapshotDay = 0L;
    }

    private static WorldId CreateWorldIdentity()
    {
        return new WorldId(System.Guid.NewGuid());
    }

    private void ComposeP10RuinLocalTopology()
    {
        p10RuinCandidate = P10RuinLocalTopologyGenesis.CreateCandidate(simulationConfig, genesisSpatialAuthority, selectedDefinitions);
        genesisLocalTopologyStore = new LocalTopologyStore(runtimeIdentityRegistry);
        if (!P10RuinLocalTopologyGenesis.TryCompose(
                p10RuinCandidate,
                runtimeIdAllocator,
                runtimeIdentityRegistry,
                spatialNetwork,
                explorableSiteStore,
                genesisSpatialAuthority,
                genesisLocalTopologyStore,
                out p10RuinRuntime,
                out _,
                out genesisLegacySpatialAnchorBindingStore,
                out string diagnostic))
            throw new System.InvalidOperationException("P10 Ruin topology genesis stage failed before publication: " + diagnostic);
        AddExplorableSiteRuntimeByDefinition(p10RuinRuntime.DefinitionId, p10RuinRuntime);
    }

    private void ComposeAuthoredGeography()
    {
        DailyV1SimulationConfigData selected = UsesSelectedDefinitionBindings
            ? selectedDefinitions.Configuration
            : null;
        string scaleText = selected != null ? selected.authoredDistancePerNeighborStep : simulationConfig.authoredDistancePerNeighborStep;
        string scaleConvention = selected != null ? selected.authoredScaleConventionId : simulationConfig.authoredScaleConventionId;
        string scaleSource = selected != null ? selected.authoredScaleSourceIdentity : simulationConfig.authoredScaleSourceIdentity;
        string scaleVersion = selected != null ? selected.authoredScaleSourceVersion : simulationConfig.authoredScaleSourceVersion;
        string scaleUnit = selected != null ? selected.authoredScaleUnit : simulationConfig.authoredScaleUnit;
        string hexId = selected != null ? selected.authoredHexId : simulationConfig.authoredHexId;
        int hexQ = selected != null ? selected.authoredHexQ : simulationConfig.authoredHexQ;
        int hexR = selected != null ? selected.authoredHexR : simulationConfig.authoredHexR;
        string terrainId = selected != null ? selected.authoredTerrainDefinitionId : simulationConfig.authoredTerrainDefinitionId;
        string terrainRevision = selected != null ? selected.authoredTerrainRevisionToken : simulationConfig.authoredTerrainRevisionToken;
        string locationId = selected != null ? selected.authoredLocationId : simulationConfig.authoredLocationId;
        decimal scale = decimal.Parse(scaleText, NumberStyles.Number, CultureInfo.InvariantCulture);
        var definition = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext(scaleConvention, scaleSource,
                scaleVersion, scale, scaleUnit),
            new[] { new HexRecord(new HexId(hexId), new HexCoordinate(hexQ, hexR),
                new TerrainReference(new TerrainDefinitionId(terrainId), terrainRevision)) },
            new[] { new LocationRecord(new LocationId(locationId), new HexId(hexId)) });
        if (!genesisSpatialAuthority.TryComposeGeography(definition, out SpatialAuthorityFailure failure))
            throw new System.InvalidOperationException("Authored geography stage failed atomically: " + failure);
        BindAuthoredMaterialFlowCityAnchors();
    }

    private void BindAuthoredMaterialFlowCityAnchors()
    {
        foreach (CityRuntime city in CityRuntimeList)
        {
            if (city == null || !city.HasLocalDailyMaterialFlow
                || string.IsNullOrWhiteSpace(city.MaterialFlowLocationId))
                continue;

            if (genesisLegacySpatialAnchorBindingStore == null)
                genesisLegacySpatialAnchorBindingStore = new LegacySpatialAnchorBindingStore(genesisSpatialAuthority);

            LocationId locationId = new LocationId(city.MaterialFlowLocationId);
            if (!genesisLegacySpatialAnchorBindingStore.TryBindCity(city.RuntimeId, locationId, out SpatialAnchorBindingFailure failure))
                throw new System.InvalidOperationException(
                    "P14 local material-flow City anchor composition failed: " + failure);
        }
    }

    public string GetFullLog()
    {
        return FullLog;
    }

    private static void ValidateP14SourceAdmission(
        SimulationConfigData config,
        SimulationRuntimeAdmissionContext admissionContext)
    {
        if (config == null) return;

        if (P10RuinLocalTopologyGenesis.IsEnabled(config)
            && FiniteSourceProfileAdmission.HasAuthoredMaterialFlowCity(config.Cities))
            throw new System.InvalidOperationException(
                "P14/P10 combined bootstrap profile is deferred until it can assign distinct canonical P8 Location anchors to the City and Ruin.");

        if (!FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
                config.Cities, out string cardinalityRejection))
            throw new System.InvalidOperationException(
                (FiniteSourceProfileAdmission.HasMixedSourceProfile(config.Cities)
                    ? "P14-C mixed-source profile rejected before CityRuntime construction: "
                    : "P14-B finite source profile rejected before CityRuntime construction: ")
                + cardinalityRejection);

        if (admissionContext == null
            || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            return;

        if (FiniteSourceProfileAdmission.HasFiniteReserveProfile(config.Cities))
            throw new System.InvalidOperationException(
                "P14-B finite source profile is not admitted by the Unity bootstrap daily profile.");

        if (FiniteSourceProfileAdmission.HasMixedSourceProfile(config.Cities))
            throw new System.InvalidOperationException(
                "P14-C mixed-source profile is not admitted by the Unity bootstrap daily profile.");

        if (FiniteSourceProfileAdmission.HasAuthoredMaterialFlowCity(config.Cities))
            throw new System.InvalidOperationException(
                "P14-A authored material-flow profile is not admitted by the Unity bootstrap daily profile.");
    }

    private bool IsCanonicalP10RuinDefinition(ExplorableSiteRuntime site)
    {
        return site != null
            && simulationConfig?.authoredP10RuinSite != null
            && selectedDefinitions != null
            && selectedDefinitions.ExplorableSites.TryGetValue(
                simulationConfig.authoredP10RuinSite.DefinitionId,
                out DailyV1ExplorableSiteDefinition expected)
            && ReferenceEquals(site.Definition, expected);
    }

    private void ValidateCandidateProfile()
    {
        bool p10b = SimulationGenesisPipeline.IsP10BGeneratedRuinEnabled(simulationConfig);
        int p10SiteCount = P10RuinLocalTopologyGenesis.IsEnabled(simulationConfig) && !p10b ? 1 : 0;
        DailyV1SimulationConfigData selectedConfiguration = UsesSelectedDefinitionBindings
            ? selectedDefinitions.Configuration
            : null;
        int expectedCityCount = selectedConfiguration?.cities?.Count ?? simulationConfig.Cities.Count;
        int expectedNpcCount = selectedConfiguration?.npcs?.Count ?? simulationConfig.Npcs.Count;
        int expectedSiteCount = selectedConfiguration?.explorableSites?.Count ?? simulationConfig.ExplorableSites.Count;
        int expectedDirectiveCount = selectedConfiguration?.scheduledDirectives?.Count ?? simulationConfig.ScheduledDirectives.Count;
        if (CityRuntimeList.Count != expectedCityCount
            || NpcRuntimeList.Count != expectedNpcCount
            || explorableSiteStore.Sites.Count != expectedSiteCount + p10SiteCount
            || scheduledDirectiveStore.Directives.Count != expectedDirectiveCount)
            throw new System.InvalidOperationException("Authored bootstrap output inventory does not match its selected profile.");

        if (p10SiteCount == 1)
        {
            if (p10RuinCandidate == null || p10RuinRuntime == null
                || !IsCanonicalP10RuinDefinition(p10RuinRuntime)
                || p10RuinRuntime.Definition.Kind.Value != (int)ExplorableSiteKind.Ruin
                || !simulationRuntime.SpatialAuthorityStore.TryGet(p10RuinCandidate.Location.Id, out LocationRecord selectedLocation)
                || selectedLocation.AnchorHexId != p10RuinCandidate.Anchor.Id
                || !simulationRuntime.LegacySpatialAnchorBindingStore.TryGet(
                    new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.ExplorableSite, p10RuinRuntime.RuntimeId),
                    out LocationId boundLocation)
                || boundLocation != p10RuinCandidate.Location.Id)
                throw new System.InvalidOperationException("P10 Ruin identity or selected P8 Location anchor handoff is incomplete.");

            LocalTopologySemanticOwnerReference semanticOwner = new LocalTopologySemanticOwnerReference(
                p10RuinRuntime.DefinitionId, p10RuinCandidate.Location.Id);
            if (genesisLocalTopologyStore == null
                || !genesisLocalTopologyStore.TryGetTopologyForSemanticOwner(semanticOwner, out LocalTopologyRuntime topology)
                || !genesisLocalTopologyStore.TryResolveSemanticOwnerRuntimeId(semanticOwner, out string resolvedRuntimeId)
                || !string.Equals(resolvedRuntimeId, p10RuinRuntime.RuntimeId, System.StringComparison.Ordinal)
                || topology.Places.Count != 3
                || topology.EntryPointCount != 1
                || topology.Connections.Count != 2
                || !simulationRuntime.SpatialAuthorityStore.ValidateInvariants(genesisLocalTopologyStore).IsValid)
                throw new System.InvalidOperationException("P10 semantic owner resolution or composed finite LocalTopology validation failed.");

            LocalPlaceRuntime entry = topology.EntryPoints[0];
            if (!simulationRuntime.SpatialAuthorityStore.TryResolve(
                    SpatialReference.ForSubLocation(topology.Owner, entry.RuntimeId),
                    genesisLocalTopologyStore,
                    out SpatialResolution resolution,
                    out SpatialAuthorityFailure resolutionFailure)
                || resolution?.Location?.Id != p10RuinCandidate.Location.Id)
                throw new System.InvalidOperationException("P10 entry point did not resolve through the selected P8 Location: " + resolutionFailure);
        }

        List<CityData> selectedCityIdentityBridges = null;
        int expectedRouteCount = 0;
        if (selectedConfiguration != null)
        {
            selectedCityIdentityBridges = new List<CityData>();
            foreach (string cityId in selectedConfiguration.cities ?? System.Array.Empty<string>())
            {
                if (!selectedDefinitions.Cities.TryGetValue(cityId, out DailyV1CityDefinition cityNode)
                    || cityNode?.Values == null
                    || selectedDefinitionBindings == null
                    || !selectedDefinitionBindings.TryGetAsset(cityNode, out CityData identityBridge)
                    || identityBridge == null
                    || !string.Equals(identityBridge.DefinitionId, cityId, System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException("The selected City configuration references an unavailable effective definition.");
                selectedCityIdentityBridges.Add(identityBridge);
                expectedRouteCount += cityNode.Values.connections?.Count ?? 0;
            }
        }
        else
        {
            foreach (CityData city in simulationConfig.Cities)
                expectedRouteCount += city.connections.Count;
        }
        int cityValidationCount = selectedCityIdentityBridges?.Count ?? simulationConfig.Cities.Count;
        for (int cityIndex = 0; cityIndex < cityValidationCount; cityIndex++)
        {
            CityData city = selectedCityIdentityBridges == null
                ? simulationConfig.Cities[cityIndex]
                : null;
            CityData cityIdentity = selectedCityIdentityBridges == null
                ? city
                : selectedCityIdentityBridges[cityIndex];
            DailyV1CityDefinition selectedCityDefinition = null;
            if (selectedConfiguration != null
                && (!selectedDefinitions.Cities.TryGetValue(cityIdentity.DefinitionId, out selectedCityDefinition)
                    || selectedCityDefinition?.Values == null))
                throw new System.InvalidOperationException("A selected City owner is missing its admitted effective definition.");
            int expectedInitialPopulation = selectedCityDefinition != null
                ? selectedCityDefinition.Values.initialPopulation
                : city.initialPopulation;
            CityRuntime runtime = GetSingleCityRuntimeByDefinition(cityIdentity);
            if (runtime == null)
                throw new System.InvalidOperationException("Authored city owner output is incomplete.");
            DailyV1CityData selectedCityValues = selectedCityDefinition?.Values;
            int admittedMarketInputCount = selectedCityValues != null
                ? selectedCityValues.marketItems?.Count ?? 0
                : city.marketItems.Count;
            int admittedProductionInputCount = selectedCityValues != null
                ? selectedCityValues.productionConfigs?.Count ?? 0
                : city.productionConfigs.Count;
            MarketLiquidityMode expectedLiquidityMode = selectedCityValues != null
                ? (MarketLiquidityMode)(selectedCityValues.marketLiquidity?.liquidityMode?.Value ?? (int)MarketLiquidityMode.Open)
                : city.MarketLiquidity.liquidityMode;
            float expectedMarketPurchasingPower = selectedCityValues?.marketLiquidity?.initialPurchasingPower
                ?? city.MarketLiquidity.initialPurchasingPower;
            ConsumptionPaymentMode expectedPopulationPaymentMode = selectedCityValues != null
                ? (ConsumptionPaymentMode)(selectedCityValues.populationConsumption?.paymentMode?.Value ?? (int)ConsumptionPaymentMode.Free)
                : city.PopulationConsumption.paymentMode;
            float expectedPopulationPurchasingPower = selectedCityValues?.populationConsumption?.initialPurchasingPower
                ?? city.PopulationConsumption.initialPurchasingPower;
            if (runtime.CurrentPopulation != expectedInitialPopulation
                || runtime.CityData != cityIdentity || runtime.Market.Items.Count != admittedMarketInputCount
                || (selectedCityDefinition == null && runtime.CityData.productionConfigs.Count != city.productionConfigs.Count)
                || runtime.MarketCounterparty.LiquidityMode != expectedLiquidityMode
                || runtime.PopulationEconomy.PaymentMode != expectedPopulationPaymentMode
                || runtime.MarketCounterparty.CounterpartyRuntimeId != runtime.RuntimeId
                || (expectedLiquidityMode == MarketLiquidityMode.AccountBacked
                    && (runtime.MarketCounterparty.MoneyAccount == null
                        || runtime.MarketCounterparty.MoneyAccount.Balance != expectedMarketPurchasingPower))
                || (expectedPopulationPaymentMode == ConsumptionPaymentMode.AccountBacked
                    && (runtime.PopulationEconomy.MoneyAccount == null
                        || runtime.PopulationEconomy.MoneyAccount.Balance != expectedPopulationPurchasingPower)))
                throw new System.InvalidOperationException("Authored city owner output is incomplete.");
            for (int i = 0; i < admittedMarketInputCount; i++)
            {
                MarketItemRuntime output = runtime.Market.Items[i];
                if (selectedCityValues != null)
                {
                    DailyV1MarketItemConfig admittedInput = selectedCityValues.marketItems[i];
                    bool bindingMatches = selectedDefinitionBindings != null
                        && TryResolveSelectedItem(admittedInput?.item, out ItemData admittedItem)
                        && ReferenceEquals(output.Item, admittedItem);
                    if (admittedInput == null
                        || !string.Equals(output.Item?.DefinitionId, admittedInput.item, System.StringComparison.Ordinal)
                        || output.Amount != admittedInput.initialAmount
                        || output.DesiredAmount != admittedInput.desiredAmount
                        || (selectedDefinitionBindings != null && !bindingMatches))
                        throw new System.InvalidOperationException("Selected City market inputs do not match the admitted effective definition.");
                }
                else
                {
                    MarketItemConfig input = city.marketItems[i];
                    if (input.item != output.Item || input.initialAmount != output.Amount || input.desiredAmount != output.DesiredAmount)
                        throw new System.InvalidOperationException("Authored city market output does not match its selected row.");
                }
            }
            if (selectedCityDefinition != null
                && (!ReferenceEquals(runtime.SelectedRuntimeOwners?.Definition, selectedCityDefinition)
                    || !ReferenceEquals(runtime.SelectedRuntimeOwners?.Definitions, selectedDefinitions)
                    || (selectedCityValues.productionConfigs ?? System.Array.Empty<DailyV1CityProductionConfig>())
                        .Any(row => row != null && !string.IsNullOrWhiteSpace(row.item)
                            && !selectedDefinitions.Items.ContainsKey(row.item))))
                throw new System.InvalidOperationException("Selected City production inputs are not owned by the exact admitted definition graph.");
        }
        expectedRouteCount += expectedSiteCount * 2;
        if (spatialNetwork.Routes.Count != expectedRouteCount)
            throw new System.InvalidOperationException("Authored spatial route output inventory is incomplete.");

        List<NpcData> selectedNpcIdentityBridges = null;
        if (selectedConfiguration != null)
        {
            selectedNpcIdentityBridges = new List<NpcData>();
            foreach (DailyV1NpcSimulationConfig selectedRow in
                selectedConfiguration.npcs ?? System.Array.Empty<DailyV1NpcSimulationConfig>())
            {
                if (selectedRow == null || string.IsNullOrWhiteSpace(selectedRow.npc)
                    || !selectedDefinitions.Npcs.TryGetValue(selectedRow.npc, out DailyV1NpcDefinition selectedNode)
                    || selectedNode == null
                    || selectedDefinitionBindings == null
                    || !selectedDefinitionBindings.TryGetAsset(selectedNode, out NpcData identityBridge)
                    || identityBridge == null
                    || !string.Equals(identityBridge.DefinitionId, selectedRow.npc, System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException(
                        "A selected NPC validation row did not resolve through its admitted definition and identity binding.");
                selectedNpcIdentityBridges.Add(identityBridge);
            }
        }

        int npcValidationCount = selectedNpcIdentityBridges?.Count ?? simulationConfig.Npcs.Count;
        for (int npcIndex = 0; npcIndex < npcValidationCount; npcIndex++)
        {
            NpcSimulationConfig row = selectedNpcIdentityBridges == null
                ? simulationConfig.Npcs[npcIndex]
                : null;
            NpcData npcIdentity = selectedNpcIdentityBridges == null
                ? row.npc
                : selectedNpcIdentityBridges[npcIndex];
            NpcRuntime runtime = GetSingleNpcRuntimeByDefinition(npcIdentity);
            DailyV1NpcSimulationConfig selectedNpcConfiguration = null;
            DailyV1NpcDefinition selectedNpcDefinition = null;
            if (selectedConfiguration != null)
            {
                foreach (DailyV1NpcSimulationConfig candidate in selectedConfiguration.npcs ?? System.Array.Empty<DailyV1NpcSimulationConfig>())
                {
                    if (candidate == null || !string.Equals(candidate.npc, npcIdentity.DefinitionId, System.StringComparison.Ordinal))
                        continue;
                    if (selectedNpcConfiguration != null)
                        throw new System.InvalidOperationException("The selected NPC configuration is ambiguous by definition identity.");
                    selectedNpcConfiguration = candidate;
                }
                if (selectedNpcConfiguration == null)
                    throw new System.InvalidOperationException("A selected NPC owner is missing its admitted effective configuration.");
                if (!selectedDefinitions.Npcs.TryGetValue(npcIdentity.DefinitionId, out selectedNpcDefinition)
                    || selectedNpcDefinition?.Values == null)
                    throw new System.InvalidOperationException("A selected NPC owner is missing its admitted effective definition.");
            }
            string expectedStartingCityId = selectedNpcConfiguration != null
                ? selectedNpcConfiguration.startingCity
                : row.startingCity?.DefinitionId;
            float expectedInitialMoney = selectedNpcConfiguration != null
                ? selectedNpcConfiguration.initialMoney
                : row.initialMoney;
            CityRuntime expectedCity = GetSingleCityRuntimeByDefinition(expectedStartingCityId);
            var expectedStatuses = new System.Collections.Generic.List<NpcStatusData>();
            if (selectedNpcDefinition != null)
            {
                foreach (string statusId in selectedNpcDefinition.Values.statusPadrao ?? System.Array.Empty<string>())
                {
                    if (!TryResolveSelectedStatus(statusId, out NpcStatusData statusAsset))
                        throw new System.InvalidOperationException("A selected NPC status did not resolve through its admitted definition graph.");
                    expectedStatuses.Add(statusAsset);
                }
            }
            else
            {
                expectedStatuses.AddRange(row.npc.statusPadrao);
            }
            bool hasActiveWarrant = selectedConfiguration != null
                ? (selectedConfiguration.initialWarrants ?? System.Array.Empty<DailyV1InitialWantedRecordConfig>())
                    .Any(warrant => warrant != null
                        && string.Equals(warrant.target, npcIdentity.DefinitionId, System.StringComparison.Ordinal))
                : simulationConfig.InitialWarrants.Any(warrant => warrant != null && warrant.target == row.npc);
            string wantedStatusId = selectedConfiguration != null
                ? selectedConfiguration.wantedStatus
                : simulationConfig.wantedStatus?.DefinitionId;
            if (hasActiveWarrant && !string.IsNullOrWhiteSpace(wantedStatusId))
            {
                NpcStatusData wantedStatus = null;
                if (selectedConfiguration != null)
                {
                    if (!TryResolveSelectedStatus(wantedStatusId, out wantedStatus))
                        throw new System.InvalidOperationException("The selected wanted status did not resolve through its admitted definition graph.");
                }
                else
                {
                    wantedStatus = simulationConfig.wantedStatus;
                }
                if (wantedStatus != null && !expectedStatuses.Contains(wantedStatus))
                    expectedStatuses.Add(wantedStatus);
            }
            if (runtime == null || runtime.NpcData != npcIdentity || runtime.Money != expectedInitialMoney
                || runtime.CurrentCity != expectedCity || runtime.CurrentLocation != expectedCity.Location
                || runtime.CurrentStatus.Count != expectedStatuses.Count)
                throw new System.InvalidOperationException("Authored NPC owner output is incomplete.");
            for (int statusIndex = 0; statusIndex < expectedStatuses.Count; statusIndex++)
                if (runtime.CurrentStatus[statusIndex] != expectedStatuses[statusIndex])
                    throw new System.InvalidOperationException("Authored NPC status output does not preserve authored/domain order.");
            // The P12 bridge deliberately retains the exact authoring object identity.
            // Selected behavior and content are validated against the admitted node, not
            // reread from its mutable ScriptableObject fields after admission.
            if (selectedNpcDefinition != null)
            {
                if (!ReferenceEquals(runtime.SelectedDailyV1Definition, selectedNpcDefinition))
                    throw new System.InvalidOperationException("The selected NPC owner does not retain its exact admitted definition node.");
            }
            else if (runtime.NpcData.job != row.npc.job
                || runtime.NpcData.acoesPadrao.Count != row.npc.acoesPadrao.Count
                || runtime.NpcData.traits.Count != row.npc.traits.Count
                || runtime.NpcData.capabilityValues.Count != row.npc.capabilityValues.Count)
            {
                throw new System.InvalidOperationException("Authored NPC definition inputs were not preserved by their owner.");
            }
            if (selectedNpcDefinition == null)
            {
                for (int actionIndex = 0; actionIndex < row.npc.acoesPadrao.Count; actionIndex++)
                    if (runtime.NpcData.acoesPadrao[actionIndex].action != row.npc.acoesPadrao[actionIndex].action
                        || runtime.NpcData.acoesPadrao[actionIndex].baseUtility != row.npc.acoesPadrao[actionIndex].baseUtility)
                        throw new System.InvalidOperationException("Authored NPC default actions were not preserved by their owner.");
            }
            var expectedInventoryItems = new System.Collections.Generic.HashSet<ItemData>();
            var expectedInventory = new InventoryRuntime();
            var admittedInventoryItems = new System.Collections.Generic.List<ItemData>();
            var admittedInventoryAmounts = new System.Collections.Generic.List<int>();
            var admittedInventoryCosts = new System.Collections.Generic.List<float>();
            if (selectedNpcConfiguration != null)
            {
                foreach (DailyV1NpcInitialInventoryItemConfig item in
                    selectedNpcConfiguration.initialInventory ?? System.Array.Empty<DailyV1NpcInitialInventoryItemConfig>())
                {
                    if (item == null) continue;
                    ItemData itemAsset = null;
                    if (!string.IsNullOrWhiteSpace(item.item) && !TryResolveSelectedItem(item.item, out itemAsset))
                        throw new System.InvalidOperationException("A selected NPC inventory item did not resolve through its admitted definition graph.");
                    admittedInventoryItems.Add(itemAsset);
                    admittedInventoryAmounts.Add(item.amount);
                    admittedInventoryCosts.Add(item.averageUnitCost);
                }
            }
            else
                foreach (NpcInitialInventoryItemConfig item in row.InitialInventory)
                {
                    admittedInventoryItems.Add(item.item);
                    admittedInventoryAmounts.Add(item.amount);
                    admittedInventoryCosts.Add(item.averageUnitCost);
                }
            for (int itemIndex = 0; itemIndex < admittedInventoryItems.Count; itemIndex++)
            {
                ItemData item = admittedInventoryItems[itemIndex];
                int amount = admittedInventoryAmounts[itemIndex];
                if (amount > 0) expectedInventoryItems.Add(item);
                expectedInventory.AddItem(item, amount, admittedInventoryCosts[itemIndex]);
            }
            var validatedInventoryItems = new System.Collections.Generic.HashSet<ItemData>();
            foreach (ItemData item in admittedInventoryItems)
            {
                if (!validatedInventoryItems.Add(item)) continue;
                int expectedAmount = 0;
                for (int candidateIndex = 0; candidateIndex < admittedInventoryItems.Count; candidateIndex++)
                    if (admittedInventoryItems[candidateIndex] == item)
                        expectedAmount = checked(expectedAmount + admittedInventoryAmounts[candidateIndex]);
                if (runtime.Inventory.GetAmount(item) != expectedAmount)
                    throw new System.InvalidOperationException("Authored NPC inventory output does not match its selected rows.");
                if (runtime.Inventory.GetAverageUnitCost(item) != expectedInventory.GetAverageUnitCost(item))
                    throw new System.InvalidOperationException("Authored NPC inventory costs do not match their ordered selected rows.");
            }
            if (runtime.Inventory.Items.Count != expectedInventoryItems.Count)
                throw new System.InvalidOperationException("Authored NPC inventory contains unexpected output rows.");
            List<string> expectedKnownSiteIds = selectedNpcConfiguration != null
                ? new List<string>(selectedNpcConfiguration.initialKnownExplorableSites ?? System.Array.Empty<string>())
                : row.InitialKnownExplorableSites.Select(known => known?.DefinitionId).ToList();
            foreach (string knownSiteId in expectedKnownSiteIds)
            {
                if (!explorableSiteRuntimesByDefinition.TryGetValue(knownSiteId, out List<ExplorableSiteRuntime> sites)
                    || sites.Count != 1 || !runtime.ExplorableSiteKnowledge.KnowsSite(sites[0].RuntimeId))
                    throw new System.InvalidOperationException("Authored initial Knowledge output is incomplete.");
                if (!runtime.ExplorableSiteKnowledge.TryGetObservation(sites[0].RuntimeId, out ExplorableSiteKnowledgeObservation observation)
                    || observation.Source != ExplorableSiteKnowledgeSource.InitialScenarioKnowledge
                    || observation.ObservedDay != 0 || observation.ReceivedDay != 0
                    || observation.LocationRuntimeId != sites[0].Location.RuntimeId)
                    throw new System.InvalidOperationException("Authored initial Knowledge provenance does not match its selected site.");
            }
            System.Collections.Generic.HashSet<string> expectedKnowledgeIds =
                new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            foreach (string knownSiteId in expectedKnownSiteIds)
                if (!string.IsNullOrWhiteSpace(knownSiteId)) expectedKnowledgeIds.Add(knownSiteId);
            if (runtime.ExplorableSiteKnowledge.Observations.Count != expectedKnowledgeIds.Count)
                throw new System.InvalidOperationException("Authored initial Knowledge contains unexpected observations.");
        }

        if (selectedConfiguration != null)
        {
            IReadOnlyList<DailyV1InitialWantedRecordConfig> selectedWarrants =
                selectedConfiguration.initialWarrants ?? System.Array.Empty<DailyV1InitialWantedRecordConfig>();
            foreach (DailyV1InitialWantedRecordConfig row in selectedWarrants)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.target) || string.IsNullOrWhiteSpace(row.city))
                    continue;
                NpcRuntime target = GetSingleNpcRuntimeByDefinition(row.target);
                List<WantedRecordRuntime> records = justiceSystem?.GetActiveWarrants(target);
                CityRuntime city = GetSingleCityRuntimeByDefinition(row.city);
                WantedRecordRuntime record = records?.Find(value => value.City == city);
                float expectedBounty = 0f;
                int expectedSentenceDays = 0;
                foreach (DailyV1InitialWantedRecordConfig candidate in selectedWarrants)
                    if (candidate != null
                        && string.Equals(candidate.target, row.target, System.StringComparison.Ordinal)
                        && string.Equals(candidate.city, row.city, System.StringComparison.Ordinal))
                    {
                        expectedBounty += candidate.bounty;
                        expectedSentenceDays += candidate.sentenceDays;
                    }
                if (record == null || record.Bounty != expectedBounty || record.SentenceDays != expectedSentenceDays)
                    throw new System.InvalidOperationException("Authored warrant owner output is incomplete.");
            }
        }
        else
        {
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
        }
        if (p10SiteCount == 1 && !p10b)
        {
            if (p10RuinCandidate == null || p10RuinRuntime == null
                || !IsCanonicalP10RuinDefinition(p10RuinRuntime)
                || p10RuinRuntime.Definition.Kind.Value != (int)ExplorableSiteKind.Ruin
                || !simulationRuntime.SpatialAuthorityStore.TryGet(p10RuinCandidate.Location.Id, out LocationRecord selectedLocation)
                || selectedLocation.AnchorHexId != p10RuinCandidate.Anchor.Id
                || !simulationRuntime.LegacySpatialAnchorBindingStore.TryGet(
                    new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.ExplorableSite, p10RuinRuntime.RuntimeId),
                    out LocationId boundLocation)
                || boundLocation != p10RuinCandidate.Location.Id)
                throw new System.InvalidOperationException("P10 Ruin identity or selected P8 Location anchor handoff is incomplete.");

            var semanticOwner = new LocalTopologySemanticOwnerReference(p10RuinRuntime.DefinitionId, p10RuinCandidate.Location.Id);
            LocalTopologyStore topologyStore = simulationRuntime.LocalTopologyStore;
            if (topologyStore == null
                || !topologyStore.TryGetTopologyForSemanticOwner(semanticOwner, out LocalTopologyRuntime topology)
                || !topologyStore.TryResolveSemanticOwnerRuntimeId(semanticOwner, out string resolvedRuntimeId)
                || !string.Equals(resolvedRuntimeId, p10RuinRuntime.RuntimeId, System.StringComparison.Ordinal)
                || topology.Places.Count != 3
                || topology.EntryPoints.Count != 1
                || topology.Connections.Count != 2
                || !simulationRuntime.SpatialAuthorityStore.ValidateInvariants(topologyStore).IsValid)
                throw new System.InvalidOperationException("P10 Ruin LocalTopology was not completely published through its selected P8 anchor.");

            if (!simulationRuntime.SpatialAuthorityStore.TryResolve(
                    SpatialReference.ForSubLocation(topology.Owner, topology.EntryPoints[0].RuntimeId),
                    topologyStore, out SpatialResolution resolution, out SpatialAuthorityFailure failure)
                || resolution?.Location?.Id != p10RuinCandidate.Location.Id)
                throw new System.InvalidOperationException("P10 Ruin entry point does not resolve through the canonical P8 Location: " + failure);
        }
        if (p10b)
        {
            int generatedEntryCount = 0;
            if (p10BPreparedRuin != null)
                foreach (RuinLocalTopologyGeneration.Place place in p10BPreparedRuin.GeneratedTopology.Places)
                    if (place.IsEntry) generatedEntryCount++;
            if (p10BPreparedRuin == null
                || !IsCanonicalP10RuinDefinition(p10BPreparedRuin.Site)
                || p10BPreparedRuin.Site.SiteInstanceId != p10BPreparedRuin.Request.SiteInstanceId
                || p10BPreparedRuin.GeneratedTopology.Places.Count < 6
                || p10BPreparedRuin.GeneratedTopology.Places.Count > 10
                || generatedEntryCount != 1
                || p10BPreparedRuin.Topology.IsPublished
                || explorableSiteStore.TryGetByRuntimeId(p10BPreparedRuin.Site.RuntimeId, out _)
                || genesisLocalTopologyStore == null
                || genesisLocalTopologyStore.Topologies.Count != 0
                || !simulationRuntime.SpatialAuthorityStore.ValidateInvariants(genesisLocalTopologyStore).IsValid)
                throw new System.InvalidOperationException("P10-B prepared topology is invalid or became visible before atomic publication.");
        }
        if (historyStore.HistoricalEvents.Count != 0)
            throw new System.InvalidOperationException("Genesis must not create simulated history before the first boundary.");
    }

    public bool TryGetNpcRuntime(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session))
        {
            npcRuntime = null;
            return false;
        }
        try
        {
            return session.IdentityRegistry.TryGetNpc(runtimeId, out npcRuntime);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    public bool TryGetCityRuntime(string runtimeId, out CityRuntime cityRuntime)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session))
        {
            cityRuntime = null;
            return false;
        }
        try
        {
            return session.IdentityRegistry.TryGetCity(runtimeId, out cityRuntime);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    public bool TryGetSpatialLocation(string runtimeId, out SpatialLocationRuntime location)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session))
        {
            location = null;
            return false;
        }
        try
        {
            return session.SpatialNetwork.TryGetLocation(runtimeId, out location);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    public bool TryGetSpatialRoute(string runtimeId, out SpatialRouteRuntime route)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session))
        {
            route = null;
            return false;
        }
        try
        {
            return session.SpatialNetwork.TryGetRoute(runtimeId, out route);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    public bool TryGetExplorableSiteRuntime(string runtimeId, out ExplorableSiteRuntime siteRuntime)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session))
        {
            siteRuntime = null;
            return false;
        }
        try
        {
            return session.IdentityRegistry.TryGetExplorableSite(runtimeId, out siteRuntime);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    public IReadOnlyList<NpcChronicleEntry> GetNpcChronicle(string npcRuntimeId)
    {
        if (!TryBeginActiveSessionOperation(out SimulationActiveSession session))
            return System.Array.Empty<NpcChronicleEntry>();
        try
        {
            return session.Composition.NpcChronicles.GetChronicle(npcRuntimeId);
        }
        finally
        {
            EndActiveSessionOperation();
        }
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

    private bool TryCreateCurrentDailyV1CompatibilityDescriptor(
        out P12ACompatibilityDescriptor compatibility, out string diagnostic)
    {
        compatibility = null;
        diagnostic = "The current host configuration is not an admitted Daily-v1 profile.";
        if (runtimeAdmissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || simulationConfig == null || !simulationConfig.useAuthoredGeographyProfile
            || P10RuinLocalTopologyGenesis.IsEnabled(simulationConfig)
            || SimulationGenesisPipeline.IsP10BGeneratedRuinEnabled(simulationConfig)
            || !IsBootstrapStartThreadCurrent()) return false;

        try
        {
            SimulationGenesisPipeline.ValidateProfile(simulationConfig);
            ValidateP14SourceAdmission(simulationConfig, runtimeAdmissionContext);
            CalendarDefinition calendar = ResolveCalendarDefinition();
            EffectiveSimulationConfiguration effective = ResolveRuntimeConfiguration();
            string profileFingerprint = SimulationGenesisPipeline.CreateFingerprint(
                simulationConfig, effective, calendar,
                out System.Collections.Generic.IReadOnlyList<string> provenance,
                out DailyV1EffectiveDefinitions selectedDefinitions);
            if (!SimulationGenesisPipeline.TryValidateSelectedDefinitionBindings(
                    selectedDefinitions, out string definitionBindingDiagnostic))
            {
                diagnostic = "The selected Daily-v1 definition graph is not closed: " + definitionBindingDiagnostic;
                return false;
            }
            SimulationGenesisManifest genesisManifest = new SimulationGenesisManifest(
                simulationConfig, effective, calendar, profileFingerprint, profileFingerprint,
                provenance, false, false);
            if (!P12CP9GenesisManifestSnapshot.TryCapture(runtimeAdmissionProfile,
                    genesisManifest, out P12CP9GenesisManifestSnapshot p9Manifest, out _))
            {
                diagnostic = "The current authored P9-B profile identity is invalid.";
                return false;
            }

            decimal distance = decimal.Parse(simulationConfig.authoredDistancePerNeighborStep,
                NumberStyles.Number, CultureInfo.InvariantCulture);
            P12CSpatialAuthoritySnapshot p8 = new P12CSpatialAuthoritySnapshot(
                P12CSpatialAuthoritySnapshot.CurrentSchemaId,
                P12CSpatialAuthoritySnapshot.CurrentSchemaVersion,
                runtimeAdmissionProfile,
                SimulationGenesisPipeline.GeographyProfileContractIdentity,
                2,
                1L,
                HexCoordinate.ConventionVersion,
                HexCoordinate.CanonicalOrder,
                new[] { new P12CSpatialHexSnapshot(simulationConfig.authoredHexId,
                    simulationConfig.authoredHexQ, simulationConfig.authoredHexR,
                    simulationConfig.authoredTerrainDefinitionId,
                    simulationConfig.authoredTerrainRevisionToken) },
                new[] { new P12CSpatialLocationSnapshot(simulationConfig.authoredLocationId,
                    simulationConfig.authoredHexId) },
                new P12CSpatialScaleSnapshot(simulationConfig.authoredScaleConventionId,
                    simulationConfig.authoredScaleSourceIdentity,
                    simulationConfig.authoredScaleSourceVersion, distance,
                    simulationConfig.authoredScaleUnit));
            if (!P12ACSpatialAuthorityCodec.TryComputeFingerprint(p8,
                    out string p8Fingerprint))
            {
                diagnostic = "The current authored P8-A geography identity is invalid.";
                return false;
            }
            if (!P12ACompatibilityDescriptorFactory.TryCreate(simulationConfig, effective, calendar,
                    selectedDefinitions,
                    profileFingerprint, p9Manifest, p8Fingerprint, out compatibility))
            {
                diagnostic = "The current runtime compatibility descriptor could not be formed.";
                return false;
            }
            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is FormatException)
        {
            diagnostic = "The current Daily-v1 compatibility inputs are invalid.";
            compatibility = null;
            return false;
        }
    }

    private void SimulateWithSession(SimulationActiveSession expectedSession, int daysToSimulate)
    {
        for (int i = 0; i < daysToSimulate; i++)
        {
            if (!ReferenceEquals(CaptureActiveSession(), expectedSession)) return;
            DailyV1ActiveRuntimeSession coreSession = expectedSession.CoreRuntimeSession;
            if (coreSession == null)
                expectedSession.Runtime.AdvanceDay();
            else
                expectedSession.Runtime.AdvanceDayThroughApplication(
                    dailyV1Application, coreSession);

            SimulationActiveSession session;
            if (!TryBeginActiveSessionOperation(expectedSession, out session)) return;
            try
            {
                AppendNpcStateSummary(session);
                AppendEconomySnapshotIfNeeded(session, false);
            }
            finally
            {
                EndActiveSessionOperation();
            }

            if (!TryBeginActiveSessionOperation(
                    expectedSession,
                    DailyV1OperationScopeKind.ContinuationSave,
                    out session)) return;
            try
            {
                TrySaveCompletedDailyV1Boundary(session, out _);
            }
            finally
            {
                EndActiveSessionOperation(DailyV1OperationScopeKind.ContinuationSave);
            }
        }

        if (!TryBeginActiveSessionOperation(expectedSession, out SimulationActiveSession finalSession)) return;
        try
        {
            if (finalSession.Configuration != null
                && finalSession.Configuration.includeEconomySnapshots == true
                && daysToSimulate >= Mathf.Max(1, finalSession.Configuration.economySnapshotIntervalDays))
                AppendEconomySnapshotIfNeeded(finalSession, true);

            SaveSimulationLog(finalSession);
        }
        finally
        {
            EndActiveSessionOperation();
        }
    }

    private void Simulate(int daysToSimulate)
    {
        SimulationActiveSession session = CaptureActiveSession();
        if (session != null)
            SimulateWithSession(session, daysToSimulate);
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

    private void AppendEconomySnapshotIfNeeded(SimulationActiveSession session, bool forceFinal)
    {
        SimulationConfigData configuration = session.Configuration;
        long currentDay = session.Composition.SimulationTime.AbsoluteDay;
        if (configuration == null
            || configuration.includeEconomySnapshots == false
            || currentDay <= 0
            || currentDay == session.ReportState.LastEconomySnapshotDay)
        {
            return;
        }

        int interval = Mathf.Max(1, configuration.economySnapshotIntervalDays);

        if (currentDay % interval != 0 && forceFinal == false)
        {
            return;
        }

        session.Logger.AddReportLine(string.Empty);
        session.Logger.AddReportLine("=== ECONOMY SNAPSHOT - DAY " + currentDay + " ===");

        foreach (CityRuntime cityRuntime in session.Runtime.Cities)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            session.Logger.AddReportLine(string.Empty);
            session.Logger.AddReportLine(cityRuntime.CityName);

            foreach (MarketItemRuntime marketItem in cityRuntime.Market.Items)
            {
                if (marketItem == null || marketItem.Item == null)
                {
                    continue;
                }

                session.Logger.AddReportLine($"{marketItem.Item.itemName}: stock {marketItem.Amount} / desired {marketItem.DesiredAmount} | ${marketItem.CurrentPrice:0.##}");
            }
        }

        session.ReportState.MarkEconomySnapshotDay(currentDay);
    }

    private void AppendNpcStateSummary(SimulationActiveSession session)
    {
        long currentDay = session.Composition.SimulationTime.AbsoluteDay;
        session.Logger.AddReportLine(string.Empty);
        session.Logger.AddReportLine($"--- ESTADO AO FIM DO DIA {currentDay} ---");

        foreach (NpcRuntime npcRuntime in session.Runtime.NpcRuntimes)
        {
            if (npcRuntime != null)
            {
                session.Logger.AddReportLine(CreateNpcStateLine(session, npcRuntime));
            }
        }
    }

    private string CreateNpcStateLine(SimulationActiveSession session, NpcRuntime npcRuntime)
    {
        string location = CreateNpcLocationText(session, npcRuntime);
        string status = CreateNpcStatusText(npcRuntime);
        string line = $"{npcRuntime.NpcName} | {location} | ${npcRuntime.Money:0.##} | {status}";

        if (npcRuntime.HasJobType(NpcJobType.Merchant))
        {
            if (npcRuntime.MerchantTradePlan.IsActive == true && npcRuntime.MerchantTradePlan.Item != null)
            {
                line += $" | Trade: {npcRuntime.MerchantTradePlan.RemainingAmount} {npcRuntime.MerchantTradePlan.Item.itemName}";
            }
            else if (npcRuntime.HasMerchantBehavior(MerchantBehavior.Local))
            {
                line += $" | Estoque: {CreateLocalInventoryText(npcRuntime)}";
            }
        }

        string warrantText = CreateWarrantText(session, npcRuntime);

        if (string.IsNullOrEmpty(warrantText) == false)
        {
            line += " | Mandados: " + warrantText;
        }

        return line;
    }

    private string CreateNpcLocationText(SimulationActiveSession session, NpcRuntime npcRuntime)
    {
        if (npcRuntime.IsTraveling == true)
        {
            string destinationName = npcRuntime.DestinationCity != null
                ? npcRuntime.DestinationCity.CityName
                : ResolveLocationDisplayName(session, npcRuntime.DestinationLocation?.RuntimeId) ?? "destino desconhecido";
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

    private string CreateWarrantText(SimulationActiveSession session, NpcRuntime npcRuntime)
    {
        if (session.Justice == null)
        {
            return string.Empty;
        }

        List<WantedRecordRuntime> warrants = session.Justice.GetActiveWarrants(npcRuntime);

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

    private void SaveSimulationLog(SimulationActiveSession session)
    {
        string simulationName = session.Configuration != null ? session.Configuration.simulationName : "Simulation";
        session.Logger.SaveToFile(simulationName + "-Run.txt");
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
            if (UsesSelectedDefinitionBindings)
            {
                if (selectedDefinitionBindings == null || selectedDefinitions.Configuration == null)
                    throw new System.InvalidOperationException(
                        "Selected action construction requires the graph's exact Unity compatibility bindings.");
                List<NpcActionData> selectedActions = new List<NpcActionData>();
                foreach (string actionId in selectedDefinitions.Configuration.actions ?? System.Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(actionId))
                    {
                        selectedActions.Add(null);
                        continue;
                    }
                    if (!selectedDefinitions.Actions.TryGetValue(actionId, out DailyV1ActionDefinition actionNode)
                        || actionNode == null
                        || !selectedDefinitionBindings.TryGetAsset(actionNode, out NpcActionData actionAsset)
                        || actionAsset == null)
                        throw new System.InvalidOperationException(
                            "A configured action did not resolve through its exact admitted typed definition node.");
                    selectedActions.Add(actionAsset);
                }
                return selectedActions;
            }

            if (simulationConfig == null)
            {
                return null;
            }

            return simulationConfig.Actions;
        }
    }

    private void CreateCityRuntimes(
        DailyV1EffectiveDefinitions selectedDefinitions = null,
        DailyV1DefinitionAdapter.SelectedBindings selectedBindings = null)
    {
        if (selectedDefinitions != null || selectedBindings != null)
        {
            if (selectedDefinitions == null || selectedBindings == null
                || selectedDefinitions.Configuration == null)
                throw new System.InvalidOperationException(
                    "Selected City construction requires the admitted typed graph and its captured configuration.");

            selectedRuntimeOwnerGraphBuilder = new DailyV1SelectedRuntimeOwnerGraphBuilder(
                selectedDefinitions, spatialNetwork.SelectedOwner,
                runtimeIdentityRegistry.CoreIdentityIndex);
            selectedInitialRuntimeOwnerGraph = null;
            Dictionary<string, SpatialLocationRuntime> selectedLocationsByRuntimeId =
                new Dictionary<string, SpatialLocationRuntime>(System.StringComparer.Ordinal);
            if (!selectedRuntimeOwnerGraphBuilder.TryCreateConfiguredCities(
                    runtimeIdAllocator,
                    selectedLocation => runtimeIdentityRegistry.TryBindSelectedLocationProjection(
                        selectedLocation, selectedLocationsByRuntimeId),
                    selectedLocation => runtimeIdentityRegistry.RollbackSelectedLocationProjection(
                        selectedLocation, selectedLocationsByRuntimeId)))
                throw new System.InvalidOperationException(
                    "The selected City owner graph could not construct the exact admitted Daily-v1 owner set.");

            foreach (DailyV1CityRuntimeOwners selectedCityOwners in selectedRuntimeOwnerGraphBuilder.Cities)
            {
                if (!selectedRuntimeOwnerGraphBuilder.TryGetLocationRuntimeId(selectedCityOwners, out string locationRuntimeId)
                    || !selectedLocationsByRuntimeId.TryGetValue(locationRuntimeId, out SpatialLocationRuntime selectedLocation))
                    throw new System.InvalidOperationException(
                        "A selected Core City owner has no matching registered Unity location projection.");
                string cityId = selectedCityOwners.Definition.DefinitionId;
                CityRuntime selectedCity = CityRuntime.CreateSelectedDailyV1(
                    selectedCityOwners.RuntimeId, selectedCityOwners.Definition,
                    selectedDefinitions, selectedBindings, selectedCityOwners, selectedLocation, logger);
                if (!selectedCity.TryBindSelectedDailyV1Definition(
                        selectedCityOwners.Definition, selectedDefinitions, selectedBindings))
                    throw new System.InvalidOperationException(
                        "The selected City Unity/P12 identity projection could not attach to its Core-owned runtime.");
                if (!runtimeIdentityRegistry.BindSelectedCityProjection(selectedCity))
                {
                    selectedRuntimeOwnerGraphBuilder.TryRemoveInitialCity(selectedCityOwners);
                    continue;
                }

                CityRuntimeList.Add(selectedCity);
                CityData p12IdentityBridge = selectedCity.CityData;
                if (p12IdentityBridge == null
                    || !string.Equals(p12IdentityBridge.DefinitionId, cityId, System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException(
                        "The selected City could not retain its exact P12 compatibility identity binding.");
                AddCityRuntimeByDefinition(p12IdentityBridge, selectedCity);
                cityRuntimeByLocation.Add(selectedLocation, selectedCity);
            }
            return;
        }

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

    /// <summary>
    /// Creates Unity-facing lookup/serialization views over the exact Core P9
    /// owners already composed by DailyV1GenesisComposer. This method must not
    /// allocate runtime identities or reconstruct authoritative owner state.
    /// </summary>
    private void ProjectSelectedCoreGenesis(DailyV1GenesisComposition composition)
    {
        if (composition == null || selectedDefinitionBindings == null
            || !ReferenceEquals(selectedDefinitions, composition.Definitions))
            throw new System.InvalidOperationException(
                "The selected Unity bootstrap requires the exact admitted Core genesis composition.");

        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        if (graph == null || !ReferenceEquals(graph.Definitions, selectedDefinitions)
            || graph.PersonStore == null
            || !ReferenceEquals(graph.PersonStore, composition.PersonStore)
            || !ReferenceEquals(graph.ScheduledDirectives, composition.OwnerGraph.ScheduledDirectives)
            || !ReferenceEquals(graph.TravelParties, composition.TravelPartyStore))
            throw new System.InvalidOperationException(
                "The Core genesis owner graph does not retain its exact admitted runtime roots.");

        runtimeIdAllocator = composition.RuntimeIdAllocator;
        simulationTime = composition.SimulationTime;
        authoritativeRandomSource = composition.RandomSource;
        recordSequence = composition.RecordSequence;
        decisionStore = composition.DecisionStore;
        scheduledDirectiveStore = graph.ScheduledDirectives;
        travelPartyStore = composition.TravelPartyStore;
        effectiveConfiguration = composition.EffectiveConfiguration;
        genesisSpatialAuthority = composition.SpatialAuthority;
        genesisLegacySpatialAnchorBindingStore = composition.LegacySpatialAnchorBindings;
        selectedInitialRuntimeOwnerGraph = graph;
        selectedRuntimeOwnerGraphBuilder = null;

        historyStore = new HistoryStore();
        domainEventStore = new DomainEventStore(historyStore, new HistoryPolicy(), logger);
        domainEventRecorder = new DomainEventRecorder(
            runtimeIdAllocator, simulationTime, recordSequence, domainEventStore, logger);
        decisionRecorder = new NpcDecisionRecorder(
            runtimeIdAllocator, simulationTime, recordSequence, decisionStore, logger);
        npcChronicleService = new NpcChronicleService(decisionStore, domainEventStore);
        explorableSiteStore = new ExplorableSiteStore();
        explorableSiteKnowledgeSystem = new ExplorableSiteKnowledgeSystem();
        expeditionStore = composition.Expeditions;
        economyTransactionService = new EconomyTransactionService();

        runtimeIdentityRegistry = new RuntimeIdentityRegistry(logger, graph.RuntimeIdentityIndex);
        spatialNetwork = new SpatialNetworkRuntime(runtimeIdentityRegistry, logger, graph.SpatialNetwork);
        foreach (SpatialLocationRuntime location in graph.SpatialNetwork.Locations)
            if (location == null || !runtimeIdentityRegistry.BindSelectedLocationProjection(location))
                throw new System.InvalidOperationException(
                    "A Core P9 Location could not bind its exact Unity identity projection.");
        foreach (SpatialRouteRuntime route in graph.SpatialNetwork.Routes)
            if (route == null || !runtimeIdentityRegistry.BindSelectedRouteProjection(route))
                throw new System.InvalidOperationException(
                    "A Core P9 Route could not bind its exact Unity identity projection.");

        CityRuntimeList.Clear();
        NpcRuntimeList.Clear();
        cityRuntimesByDefinition.Clear();
        npcRuntimesByDefinition.Clear();
        cityRuntimeByLocation.Clear();
        explorableSiteRuntimesByDefinition.Clear();

        foreach (DailyV1CityRuntimeOwners owners in graph.Cities)
        {
            if (owners == null || owners.Definition == null
                || !graph.SpatialNetwork.TryGetLocation(owners.LocationRuntimeId,
                    out SpatialLocationRuntime location))
                throw new System.InvalidOperationException(
                    "A Core P9 City owner has no exact Core-owned Location.");
            CityRuntime city = CityRuntime.CreateSelectedDailyV1(
                owners.RuntimeId, owners.Definition, selectedDefinitions,
                selectedDefinitionBindings, owners, location, logger);
            if (!city.TryBindSelectedDailyV1Definition(
                    owners.Definition, selectedDefinitions, selectedDefinitionBindings)
                || !runtimeIdentityRegistry.BindSelectedCityProjection(city))
                throw new System.InvalidOperationException(
                    "A Core P9 City could not bind its exact Unity/P12 projection.");
            CityData bridge = city.CityData;
            if (bridge == null || !string.Equals(bridge.DefinitionId,
                    owners.Definition.DefinitionId, System.StringComparison.Ordinal))
                throw new System.InvalidOperationException(
                    "A Core P9 City did not retain its exact P12 compatibility identity bridge.");
            CityRuntimeList.Add(city);
            AddCityRuntimeByDefinition(bridge, city);
            cityRuntimeByLocation.Add(location, city);
        }

        Dictionary<string, CityRuntime> selectedCityProjections = new Dictionary<string, CityRuntime>(
            System.StringComparer.Ordinal);
        foreach (CityRuntime city in CityRuntimeList)
            if (city == null || !selectedCityProjections.TryAdd(city.RuntimeId, city))
                throw new System.InvalidOperationException(
                    "Core P9 City projections must have unique runtime identities.");

        foreach (DailyV1NpcRuntimeOwners owners in graph.Npcs)
        {
            if (owners == null || owners.Definition == null)
                throw new System.InvalidOperationException("A Core P9 NPC owner is incomplete.");
            CityRuntime startingCity = null;
            if (!string.IsNullOrWhiteSpace(owners.Travel.CurrentCityRuntimeId)
                && !selectedCityProjections.TryGetValue(owners.Travel.CurrentCityRuntimeId,
                    out startingCity))
                throw new System.InvalidOperationException(
                    "A Core P9 NPC has no matching Unity City projection for its admitted presence.");

            NpcRuntime npc = NpcRuntime.CreateSelectedDailyV1(
                owners.RuntimeId, owners.Definition, selectedDefinitions,
                selectedDefinitionBindings, owners, startingCity);
            Func<string, CityRuntime> resolveCity = runtimeId =>
                !string.IsNullOrWhiteSpace(runtimeId)
                && selectedCityProjections.TryGetValue(runtimeId, out CityRuntime city)
                    ? city : null;
            if (!npc.TryBindSelectedDailyV1Definition(
                    owners.Definition, selectedDefinitions, selectedDefinitionBindings, resolveCity)
                || !runtimeIdentityRegistry.BindSelectedNpcProjection(npc))
                throw new System.InvalidOperationException(
                    "A Core P9 NPC could not bind its exact Unity/P12 projection.");
            NpcData bridge = npc.NpcData;
            if (bridge == null || !string.Equals(bridge.DefinitionId,
                    owners.Definition.DefinitionId, System.StringComparison.Ordinal))
                throw new System.InvalidOperationException(
                    "A Core P9 NPC did not retain its exact P12 compatibility identity bridge.");
            NpcRuntimeList.Add(npc);
            AddNpcRuntimeByDefinition(bridge, npc);
        }

        scheduledDirectiveSystem = new ScheduledDirectiveSystem(
            scheduledDirectiveStore, runtimeIdentityRegistry, logger);
    }

    private void CreateSpatialRoutes()
    {
        if (selectedRuntimeOwnerGraphBuilder != null)
        {
            if (!selectedRuntimeOwnerGraphBuilder.TryCreateConfiguredCityRoutes(
                    runtimeIdAllocator,
                    runtimeIdentityRegistry.BindSelectedRouteProjection,
                    runtimeIdentityRegistry.RollbackSelectedRouteProjection,
                    message => logger?.LogWarning(message),
                    message => logger?.LogError(message)))
                throw new System.InvalidOperationException(
                    "The selected Core owner graph could not construct the exact admitted Daily-v1 City routes.");
            return;
        }

        foreach (CityRuntime originCity in CityRuntimeList)
        {
            if (originCity != null && originCity.SelectedDailyV1Definition != null)
            {
                IReadOnlyList<DailyV1CityConnection> selectedConnections = originCity.SelectedDailyV1Connections;
                if (selectedConnections == null)
                    continue;

                List<DailyV1CityConnection> orderedSelectedConnections = new List<DailyV1CityConnection>(selectedConnections);
                orderedSelectedConnections.Sort((left, right) =>
                {
                    int destination = System.StringComparer.Ordinal.Compare(left?.destination, right?.destination);
                    return destination != 0
                        ? destination
                        : System.Nullable.Compare(left != null ? (int?)left.travelDays : null,
                            right != null ? (int?)right.travelDays : null);
                });
                foreach (DailyV1CityConnection connection in orderedSelectedConnections)
                {
                    if (connection == null)
                    {
                        logger.LogWarning($"Skipping invalid spatial route from city '{originCity.CityName}': connection is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(connection.destination))
                    {
                        logger.LogWarning($"Skipping invalid spatial route from city '{originCity.CityName}': destination definition is null.");
                        continue;
                    }

                    CityRuntime destinationCity = GetSingleCityRuntimeByDefinition(connection.destination);
                    if (destinationCity == null)
                        continue;
                    RegisterSpatialRoute(originCity, destinationCity, connection.travelDays);
                }
                continue;
            }

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

                RegisterSpatialRoute(originCity, destinationCity, connection.travelDays);
            }
        }
    }

    private void RegisterSpatialRoute(CityRuntime originCity, CityRuntime destinationCity, int travelDays)
    {
        if (destinationCity == originCity)
        {
            logger.LogWarning($"Skipping invalid self route for city '{originCity.CityName}'.");
            return;
        }
        SpatialRouteRuntime route = new SpatialRouteRuntime(
            runtimeIdAllocator.AllocateRouteId(), originCity.Location, destinationCity.Location, travelDays);
        spatialNetwork.RegisterRoute(route);
    }

    private void CreateExplorableSiteRuntimes()
    {
        if (UsesSelectedDefinitionBindings)
        {
            DailyV1SimulationConfigData selectedConfiguration = selectedDefinitions.Configuration;
            if (selectedConfiguration == null)
                throw new System.InvalidOperationException(
                    "Selected ExplorableSite construction requires the admitted configuration graph.");

            List<DailyV1ExplorableSiteConfig> selectedSites = new List<DailyV1ExplorableSiteConfig>(
                selectedConfiguration.explorableSites ?? System.Array.Empty<DailyV1ExplorableSiteConfig>());
            selectedSites.Sort((left, right) => System.StringComparer.Ordinal.Compare(left?.site, right?.site));
            foreach (DailyV1ExplorableSiteConfig selectedSite in selectedSites)
            {
                if (selectedSite == null || string.IsNullOrWhiteSpace(selectedSite.site))
                {
                    logger.LogWarning("Skipping selected explorable site configuration with no site identity.");
                    continue;
                }
                if (!selectedDefinitions.ExplorableSites.TryGetValue(
                        selectedSite.site, out DailyV1ExplorableSiteDefinition selectedSiteDefinition)
                    || selectedSiteDefinition == null
                    || !string.Equals(selectedSiteDefinition.DefinitionId, selectedSite.site, System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException(
                        "The selected typed definition graph does not contain the exact configured ExplorableSite node.");

                try
                {
                    SpatialLocationRuntime location = new SpatialLocationRuntime(runtimeIdAllocator.AllocateLocationId());
                    ExplorableSiteRuntime siteRuntime = new ExplorableSiteRuntime(
                        runtimeIdAllocator, selectedSiteDefinition, location);
                    if (!runtimeIdentityRegistry.RegisterExplorableSite(siteRuntime))
                    {
                        logger.LogWarning($"Skipping explorable site '{selectedSite.site}': runtime identity registration failed.");
                        continue;
                    }
                    if (!spatialNetwork.RegisterLocation(location))
                    {
                        logger.LogWarning($"Skipping explorable site '{selectedSite.site}': location registration failed.");
                        continue;
                    }
                    if (!explorableSiteStore.Add(siteRuntime))
                    {
                        logger.LogWarning($"Skipping explorable site '{selectedSite.site}': site store registration failed.");
                        continue;
                    }
                    AddExplorableSiteRuntimeByDefinition(selectedSiteDefinition.DefinitionId, siteRuntime);
                    CreateExplorableSiteRoutes(selectedSite.anchorCity, selectedSite.travelDaysFromAnchor,
                        selectedSite.site, siteRuntime);
                }
                catch (System.ArgumentException exception)
                {
                    logger.LogWarning($"Skipping explorable site '{selectedSite.site}': {exception.Message}");
                }
                catch (System.InvalidOperationException exception)
                {
                    logger.LogWarning($"Skipping explorable site '{selectedSite.site}': {exception.Message}");
                }
            }
            return;
        }

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

            if (selectedDefinitions == null
                || !selectedDefinitions.ExplorableSites.TryGetValue(siteConfig.site.DefinitionId,
                    out DailyV1ExplorableSiteDefinition siteDefinition))
                throw new System.InvalidOperationException("The selected typed definition graph does not contain the authored ExplorableSite node.");

            ExplorableSiteRuntime siteRuntime;

            try
            {
                SpatialLocationRuntime location = new SpatialLocationRuntime(runtimeIdAllocator.AllocateLocationId());
                siteRuntime = new ExplorableSiteRuntime(
                    runtimeIdAllocator,
                    siteDefinition,
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

                AddExplorableSiteRuntimeByDefinition(siteDefinition.DefinitionId, siteRuntime);
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
        if (siteConfig == null)
            return;
        CreateExplorableSiteRoutes(siteConfig.anchorCity?.DefinitionId, siteConfig.travelDaysFromAnchor,
            siteConfig.site?.DefinitionId, siteRuntime);
    }

    private void CreateExplorableSiteRoutes(
        string anchorCityDefinitionId,
        int travelDays,
        string siteDefinitionId,
        ExplorableSiteRuntime siteRuntime)
    {
        if (siteRuntime == null || string.IsNullOrWhiteSpace(anchorCityDefinitionId))
        {
            return;
        }

        CityRuntime anchorCity = GetSingleCityRuntimeByDefinition(anchorCityDefinitionId);

        if (anchorCity == null || anchorCity.Location == null || siteRuntime.Location == null)
        {
            logger.LogWarning($"Explorable site '{siteRuntime.DefinitionId}' remains isolated because its anchor city could not be resolved.");
            return;
        }

        SpatialRouteRuntime anchorToSite = new SpatialRouteRuntime(
            runtimeIdAllocator.AllocateRouteId(),
            anchorCity.Location,
            siteRuntime.Location,
            travelDays);

        if (spatialNetwork.RegisterRoute(anchorToSite) == false)
        {
            logger.LogWarning($"Could not register route from city '{anchorCity.CityName}' to explorable site '{siteDefinitionId ?? siteRuntime.DefinitionId}'.");
            return;
        }

        SpatialRouteRuntime siteToAnchor = new SpatialRouteRuntime(
            runtimeIdAllocator.AllocateRouteId(),
            siteRuntime.Location,
            anchorCity.Location,
            travelDays);

        if (spatialNetwork.RegisterRoute(siteToAnchor) == false)
        {
            logger.LogWarning($"Could not register return route from explorable site '{siteDefinitionId ?? siteRuntime.DefinitionId}' to city '{anchorCity.CityName}'.");
        }
    }

    private void CreateNpcRuntimes(
        DailyV1EffectiveDefinitions selectedDefinitions = null,
        DailyV1DefinitionAdapter.SelectedBindings selectedBindings = null)
    {
        if (selectedDefinitions != null || selectedBindings != null)
        {
            if (selectedDefinitions == null || selectedBindings == null
                || selectedDefinitions.Configuration == null)
                throw new System.InvalidOperationException(
                    "Selected NPC construction requires the admitted typed graph and its captured configuration.");

            // NpcRuntime retains this resolver for its Unity compatibility projection.
            // Capture the exact published CityRuntime objects rather than closing over
            // CityRuntimeList: ReleaseBootstrapAliases clears that temporary alias after
            // publication, while selected NPC owners continue to need stable projections.
            Dictionary<string, CityRuntime> selectedCityProjections =
                new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
            foreach (CityRuntime city in CityRuntimeList)
            {
                if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                    || selectedCityProjections.ContainsKey(city.RuntimeId))
                    throw new System.InvalidOperationException(
                        "Selected NPC projection binding requires unique published City runtime identities.");
                selectedCityProjections.Add(city.RuntimeId, city);
            }
            Func<string, CityRuntime> resolveSelectedCityProjection = cityRuntimeId =>
                !string.IsNullOrWhiteSpace(cityRuntimeId)
                && selectedCityProjections.TryGetValue(cityRuntimeId, out CityRuntime city)
                    ? city
                    : null;

            if (selectedRuntimeOwnerGraphBuilder == null
                || !selectedRuntimeOwnerGraphBuilder.TryCreateConfiguredNpcs(
                    runtimeIdAllocator,
                    cityDefinitionId => GetSingleCityRuntimeByDefinition(cityDefinitionId)?.RuntimeId))
                throw new System.InvalidOperationException(
                    "The selected Core NPC owner graph could not construct the exact admitted Daily-v1 owner set.");

            foreach (DailyV1NpcRuntimeOwners selectedNpcOwners in selectedRuntimeOwnerGraphBuilder.Npcs)
            {
                string npcId = selectedNpcOwners.Definition.DefinitionId;
                CityRuntime startingCity = null;
                if (!string.IsNullOrWhiteSpace(selectedNpcOwners.Travel.CurrentCityRuntimeId))
                {
                    startingCity = CityRuntimeList.Find(city => city != null
                        && string.Equals(city.RuntimeId, selectedNpcOwners.Travel.CurrentCityRuntimeId,
                            System.StringComparison.Ordinal));
                    if (startingCity == null)
                        throw new System.InvalidOperationException(
                            "A Core-owned selected NPC City binding has no matching Unity City projection.");
                }
                NpcRuntime selectedNpc = NpcRuntime.CreateSelectedDailyV1(
                    selectedNpcOwners.RuntimeId, selectedNpcOwners.Definition,
                    selectedDefinitions, selectedBindings, selectedNpcOwners, startingCity);
                if (!selectedNpc.TryBindSelectedDailyV1Definition(
                        selectedNpcOwners.Definition, selectedDefinitions, selectedBindings,
                        resolveSelectedCityProjection))
                    throw new System.InvalidOperationException(
                        "The selected NPC Unity/P12 identity projection could not attach to its Core-owned runtime.");
                if (!runtimeIdentityRegistry.BindSelectedNpcProjection(selectedNpc))
                {
                    startingCity?.RemoveImportantNpc(selectedNpc);
                    selectedRuntimeOwnerGraphBuilder.TryRemoveInitialNpc(selectedNpc.SelectedRuntimeOwners);
                    continue;
                }

                NpcData p12IdentityBridge = selectedNpc.NpcData;
                if (p12IdentityBridge == null
                    || !string.Equals(p12IdentityBridge.DefinitionId, npcId, System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException(
                        "The selected NPC could not retain its exact P12 compatibility identity binding.");
                NpcRuntimeList.Add(selectedNpc);
                AddNpcRuntimeByDefinition(p12IdentityBridge, selectedNpc);
            }

            return;
        }

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
            NpcRuntime npcRuntime = new NpcRuntime(
                runtimeIdAllocator.AllocateNpcId(), npcConfig.npc, startingCity, npcConfig.initialMoney);
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

    private static void ApplyInitialInventory(
        NpcRuntime npcRuntime,
        DailyV1NpcSimulationConfig npcConfig,
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (npcRuntime == null || npcConfig == null || definitions == null || bindings == null)
            return;
        foreach (DailyV1NpcInitialInventoryItemConfig inventoryConfig in
            npcConfig.initialInventory ?? System.Array.Empty<DailyV1NpcInitialInventoryItemConfig>())
        {
            if (inventoryConfig == null) continue;
            if (string.IsNullOrWhiteSpace(inventoryConfig.item)) continue;
            if (!definitions.Items.TryGetValue(inventoryConfig.item, out DailyV1ItemDefinition item))
                throw new System.InvalidOperationException(
                    "A selected NPC inventory item did not resolve through the exact admitted typed definition graph.");
            if (!npcRuntime.TryAddSelectedDailyV1InventoryItem(item, inventoryConfig.amount, inventoryConfig.averageUnitCost))
                throw new System.InvalidOperationException(
                    "A selected NPC inventory row could not be installed through its canonical Daily-v1 item node.");
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

        if (selectedRuntimeOwnerGraphBuilder != null
            && (explorableSiteStore == null || explorableSiteStore.Count == 0))
        {
            if (!selectedRuntimeOwnerGraphBuilder.TryBootstrapInitialCommercialKnowledge(
                    simulationTime?.AbsoluteDay ?? 0L))
                throw new System.InvalidOperationException(
                    "The selected Core owner graph could not seed initial commercial knowledge.");
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

        if (selectedRuntimeOwnerGraphBuilder != null
            && (explorableSiteStore == null || explorableSiteStore.Count == 0))
        {
            if (!selectedRuntimeOwnerGraphBuilder.TryBootstrapInitialSpatialKnowledge())
                throw new System.InvalidOperationException(
                    "The selected Core owner graph could not seed initial spatial knowledge.");
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
        if (UsesSelectedDefinitionBindings)
        {
            if (selectedRuntimeOwnerGraphBuilder == null
                || !selectedRuntimeOwnerGraphBuilder.TryCreateConfiguredScheduledDirectives(
                    runtimeIdAllocator, scheduledDirectiveStore,
                    message => logger?.LogWarning(message),
                    message => logger?.LogError(message)))
                throw new System.InvalidOperationException(
                    "The selected Core owner graph could not construct its admitted scheduled directives.");
            return;
        }

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
                    directiveConfig.action.DefinitionId,
                    (int)directiveConfig.action.actionType);
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

    private void RebuildSystems(
        EffectiveSimulationConfiguration configuration,
        DailyV1JusticeRecordsOwner selectedJusticeOwner = null)
    {
        logger = logger ?? (UsesSelectedDefinitionBindings
            ? UnitySimulationLoggerAdapter.CreateSelected(selectedDefinitions.Configuration?.logSettings)
            : UnitySimulationLoggerAdapter.Create(simulationConfig != null ? simulationConfig.LogSettings : null));
        economyTransactionService = economyTransactionService ?? new EconomyTransactionService();
        travelSystem = new TravelSystem(
            spatialNetwork,
            CreateCityRuntimeByLocationResolver(cityRuntimeByLocation),
            configuration.Travel,
            domainEventRecorder,
            logger,
            economyTransactionService);
        travelPartyStore = selectedInitialRuntimeOwnerGraph?.TravelParties
            ?? travelPartyStore
            ?? new TravelPartyStore();
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
        NpcStatusData freeStatus = ResolveSelectedStatus(selectedDefinitions?.Configuration?.freeStatus,
            simulationConfig != null ? simulationConfig.freeStatus : null);
        NpcStatusData wantedStatus = ResolveSelectedStatus(selectedDefinitions?.Configuration?.wantedStatus,
            simulationConfig != null ? simulationConfig.wantedStatus : null);
        NpcStatusData arrestedStatus = ResolveSelectedStatus(selectedDefinitions?.Configuration?.arrestedStatus,
            simulationConfig != null ? simulationConfig.arrestedStatus : null);
        NpcStatusData hiddenStatus = ResolveSelectedStatus(selectedDefinitions?.Configuration?.hiddenStatus,
            simulationConfig != null ? simulationConfig.hiddenStatus : null);
        justiceSystem = UsesSelectedDefinitionBindings || simulationConfig != null
            ? new JusticeSystem(freeStatus, wantedStatus, arrestedStatus, hiddenStatus,
                domainEventRecorder, logger, selectedJusticeOwner)
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
                hiddenStatus,
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
                hiddenStatus,
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

        if (UsesSelectedDefinitionBindings)
        {
            if (selectedRuntimeOwnerGraphBuilder == null
                || !selectedRuntimeOwnerGraphBuilder.TryCreateConfiguredInitialJusticeState(
                    justiceSystem.CoreOwner))
                throw new System.InvalidOperationException(
                    "The selected Core owner graph could not construct its admitted initial Justice state: "
                    + (selectedRuntimeOwnerGraphBuilder?.LastInitialJusticeStateFailure ?? "builder unavailable") + ".");

            foreach (NpcRuntime npc in NpcRuntimeList)
                if (npc == null || !npc.TryRefreshSelectedStatusProjectionForGenesis(npc.SelectedRuntimeOwners))
                    throw new System.InvalidOperationException(
                        "The selected NPC status compatibility projection could not refresh after Core Justice genesis.");

            return;
        }

        justiceSystem.CreateInitialWarrants(simulationConfig,
            GetSingleNpcRuntimeByDefinition, GetSingleCityRuntimeByDefinition);
        justiceSystem.SyncWantedStatuses(NpcRuntimeList);
    }

    private bool TryProjectInitialSelectedJusticeRows(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph == null || justiceSystem == null
            || !ReferenceEquals(graph.JusticeRecordsOwner, justiceSystem.CoreOwner))
            return false;

        foreach (DailyV1JusticeWantedRecordOwner row in graph.JusticeRecordsOwner.WantedRecords)
        {
            if (row == null) return false;
            NpcRuntime target = null;
            int targetMatches = 0;
            foreach (NpcRuntime candidate in NpcRuntimeList)
            {
                if (candidate == null || !string.Equals(candidate.RuntimeId,
                    row.TargetRuntimeId, System.StringComparison.Ordinal)) continue;
                target = candidate;
                targetMatches++;
            }

            CityRuntime city = null;
            int cityMatches = 0;
            foreach (CityRuntime candidate in CityRuntimeList)
            {
                if (candidate == null || !string.Equals(candidate.RuntimeId,
                    row.CityRuntimeId, System.StringComparison.Ordinal)) continue;
                city = candidate;
                cityMatches++;
            }

            if (targetMatches != 1 || cityMatches != 1
                || !justiceSystem.TryProjectSelectedCoreWantedRecord(row, target, city))
                return false;
        }

        return true;
    }

    private NpcStatusData ResolveSelectedStatus(string selectedStatusId, NpcStatusData legacyStatus)
    {
        if (!UsesSelectedDefinitionBindings)
            return legacyStatus;
        if (string.IsNullOrWhiteSpace(selectedStatusId))
            return null;
        if (selectedDefinitionBindings == null
            || !selectedDefinitions.Statuses.TryGetValue(selectedStatusId, out DailyV1StatusDefinition statusNode)
            || statusNode == null
            || !selectedDefinitionBindings.TryGetAsset(statusNode, out NpcStatusData statusAsset)
            || statusAsset == null)
            throw new System.InvalidOperationException(
                "A selected justice status did not resolve through its exact admitted typed definition.");
        return statusAsset;
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
        if (explorableSiteKnowledgeSystem == null)
        {
            return;
        }

        if (selectedDefinitions != null && selectedDefinitions.Configuration != null)
        {
            foreach (DailyV1NpcSimulationConfig selectedNpc in
                selectedDefinitions.Configuration.npcs ?? System.Array.Empty<DailyV1NpcSimulationConfig>())
            {
                if (selectedNpc == null || string.IsNullOrWhiteSpace(selectedNpc.npc))
                    continue;

                NpcRuntime selectedNpcRuntime = GetSingleNpcRuntimeByDefinition(selectedNpc.npc);
                if (selectedNpcRuntime == null)
                    continue;

                foreach (string siteDefinitionId in
                    selectedNpc.initialKnownExplorableSites ?? System.Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(siteDefinitionId))
                    {
                        logger.LogWarning("Skipping initial selected explorable site knowledge: site definition is null.");
                        continue;
                    }
                    if (!explorableSiteRuntimesByDefinition.TryGetValue(
                            siteDefinitionId, out List<ExplorableSiteRuntime> selectedSiteRuntimes)
                        || selectedSiteRuntimes.Count == 0)
                    {
                        logger.LogWarning($"Explorable site definition '{siteDefinitionId}' has no runtime instance.");
                        continue;
                    }
                    if (selectedSiteRuntimes.Count > 1)
                    {
                        logger.LogError($"Explorable site definition '{siteDefinitionId}' is ambiguous: {selectedSiteRuntimes.Count} runtime instances exist. Resolve by RuntimeId instead.");
                        continue;
                    }

                    explorableSiteKnowledgeSystem.RecordInitialScenarioKnowledge(
                        selectedNpcRuntime, selectedSiteRuntimes[0], simulationTime.AbsoluteDay);
                }
            }
            return;
        }

        if (simulationConfig == null)
            return;

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
                    siteDefinition.DefinitionId,
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
        string definitionId,
        ExplorableSiteRuntime siteRuntime)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) return;
        if (explorableSiteRuntimesByDefinition.TryGetValue(definitionId, out List<ExplorableSiteRuntime> runtimes) == false)
        {
            runtimes = new List<ExplorableSiteRuntime>();
            explorableSiteRuntimesByDefinition.Add(definitionId, runtimes);
        }

        runtimes.Add(siteRuntime);
    }

    private static System.Func<SpatialLocationRuntime, CityRuntime> CreateCityRuntimeByLocationResolver(
        IReadOnlyDictionary<SpatialLocationRuntime, CityRuntime> cityRuntimeByLocation)
    {
        Dictionary<SpatialLocationRuntime, CityRuntime> locationIndex =
            new Dictionary<SpatialLocationRuntime, CityRuntime>();
        if (cityRuntimeByLocation != null)
        {
            foreach (KeyValuePair<SpatialLocationRuntime, CityRuntime> pair in cityRuntimeByLocation)
                if (pair.Key != null && pair.Value != null && !locationIndex.ContainsKey(pair.Key))
                    locationIndex.Add(pair.Key, pair.Value);
        }

        return location => location != null && locationIndex.TryGetValue(location, out CityRuntime cityRuntime)
            ? cityRuntime
            : null;
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

    private CityRuntime GetSingleCityRuntimeByDefinition(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return null;

        CityRuntime match = null;
        int count = 0;
        foreach (CityRuntime city in CityRuntimeList)
        {
            if (city != null && string.Equals(city.DefinitionId, definitionId, System.StringComparison.Ordinal))
            {
                match = city;
                count++;
            }
        }

        if (count == 0)
        {
            logger.LogWarning($"City definition '{definitionId}' has no runtime instance.");
            return null;
        }
        if (count > 1)
        {
            logger.LogError($"City definition '{definitionId}' is ambiguous: {count} runtime instances exist. Resolve by RuntimeId instead.");
            return null;
        }
        return match;
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

    private NpcRuntime GetSingleNpcRuntimeByDefinition(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
            return null;

        NpcRuntime match = null;
        int count = 0;
        foreach (NpcRuntime npc in NpcRuntimeList)
        {
            if (npc != null && string.Equals(npc.DefinitionId, definitionId, System.StringComparison.Ordinal))
            {
                match = npc;
                count++;
            }
        }

        if (count == 0)
        {
            logger.LogWarning($"NPC definition '{definitionId}' has no runtime instance.");
            return null;
        }
        if (count > 1)
        {
            logger.LogError($"NPC definition '{definitionId}' is ambiguous: {count} runtime instances exist. Resolve by RuntimeId instead.");
            return null;
        }
        return match;
    }

    internal static NpcChronicleFormatter CreateNpcChronicleFormatter(
        RuntimeIdentityRegistry identityRegistry,
        SpatialNetworkRuntime spatialNetwork,
        IReadOnlyDictionary<SpatialLocationRuntime, CityRuntime> cityRuntimeByLocation,
        ExplorableSiteStore explorableSites,
        IReadOnlyList<CityRuntime> cities,
        SimulationConfigData configuration,
        DailyV1EffectiveDefinitions selectedDefinitions = null)
    {
        Dictionary<SpatialLocationRuntime, CityRuntime> locationIndex =
            new Dictionary<SpatialLocationRuntime, CityRuntime>();
        if (cityRuntimeByLocation != null)
        {
            foreach (KeyValuePair<SpatialLocationRuntime, CityRuntime> pair in cityRuntimeByLocation)
                if (pair.Key != null && pair.Value != null && !locationIndex.ContainsKey(pair.Key))
                    locationIndex.Add(pair.Key, pair.Value);
        }

        List<CityRuntime> citySnapshot = cities == null
            ? new List<CityRuntime>()
            : new List<CityRuntime>(cities);
        return new NpcChronicleFormatter(
            runtimeId => ResolveNpcDisplayName(identityRegistry, runtimeId),
            runtimeId => ResolveLocationDisplayName(spatialNetwork, locationIndex, explorableSites, runtimeId),
            definitionId => selectedDefinitions != null
                ? ResolveSelectedItemDisplayName(selectedDefinitions, definitionId)
                : ResolveItemDisplayName(citySnapshot, definitionId),
            definitionId => selectedDefinitions != null
                ? ResolveSelectedActionDisplayName(selectedDefinitions, definitionId)
                : ResolveActionDisplayName(configuration, definitionId));
    }

    private static string ResolveNpcDisplayName(RuntimeIdentityRegistry identityRegistry, string runtimeId)
    {
        return identityRegistry != null
            && identityRegistry.TryGetNpc(runtimeId, out NpcRuntime npcRuntime)
                ? npcRuntime.NpcName
                : null;
    }

    private static string ResolveLocationDisplayName(SimulationActiveSession session, string runtimeId)
    {
        return session == null
            ? null
            : ResolveLocationDisplayName(
                session.SpatialNetwork,
                session,
                session.ExplorableSites,
                runtimeId);
    }

    private static string ResolveLocationDisplayName(
        SpatialNetworkRuntime spatialNetwork,
        SimulationActiveSession sessionCityIndex,
        ExplorableSiteStore explorableSites,
        string runtimeId)
    {
        if (spatialNetwork == null
            || !spatialNetwork.TryGetLocation(runtimeId, out SpatialLocationRuntime location))
            return null;

        if (sessionCityIndex != null
            && sessionCityIndex.TryGetCityByLocation(location, out CityRuntime cityRuntime))
            return cityRuntime.CityName;

        if (explorableSites != null)
        {
            foreach (ExplorableSiteRuntime siteRuntime in explorableSites.GetForLocation(location))
            {
                if (siteRuntime?.Definition != null)
                    return string.IsNullOrWhiteSpace(siteRuntime.Definition.DisplayName)
                        ? siteRuntime.DefinitionId
                        : siteRuntime.Definition.DisplayName;
            }
        }

        return null;
    }

    private static string ResolveLocationDisplayName(
        SpatialNetworkRuntime spatialNetwork,
        IReadOnlyDictionary<SpatialLocationRuntime, CityRuntime> cityRuntimeByLocation,
        ExplorableSiteStore explorableSites,
        string runtimeId)
    {
        if (spatialNetwork == null
            || !spatialNetwork.TryGetLocation(runtimeId, out SpatialLocationRuntime location))
            return null;

        if (cityRuntimeByLocation != null
            && cityRuntimeByLocation.TryGetValue(location, out CityRuntime cityRuntime))
            return cityRuntime.CityName;

        if (explorableSites != null)
        {
            foreach (ExplorableSiteRuntime siteRuntime in explorableSites.GetForLocation(location))
            {
                if (siteRuntime?.Definition != null)
                    return string.IsNullOrWhiteSpace(siteRuntime.Definition.DisplayName)
                        ? siteRuntime.DefinitionId
                        : siteRuntime.Definition.DisplayName;
            }
        }

        return null;
    }

    private static string ResolveItemDisplayName(IReadOnlyList<CityRuntime> cities, string definitionId)
    {
        if (cities == null) return null;
        foreach (CityRuntime cityRuntime in cities)
        {
            if (cityRuntime == null) continue;
            foreach (MarketItemRuntime marketItem in cityRuntime.Market.Items)
                if (marketItem?.Item != null
                    && string.Equals(marketItem.Item.DefinitionId, definitionId, System.StringComparison.Ordinal))
                    return marketItem.Item.itemName;
        }
        return null;
    }

    private static string ResolveSelectedItemDisplayName(
        DailyV1EffectiveDefinitions definitions,
        string definitionId)
    {
        return definitions != null && !string.IsNullOrWhiteSpace(definitionId)
            && definitions.Items.TryGetValue(definitionId, out DailyV1ItemDefinition item)
            ? item.Values.itemName
            : null;
    }

    private bool TryResolveSelectedItem(string definitionId, out ItemData item)
    {
        item = null;
        return !string.IsNullOrWhiteSpace(definitionId)
            && selectedDefinitions != null
            && selectedDefinitionBindings != null
            && selectedDefinitions.Items.TryGetValue(definitionId, out DailyV1ItemDefinition itemDefinition)
            && selectedDefinitionBindings.TryGetAsset(itemDefinition, out item)
            && item != null;
    }

    private bool TryResolveSelectedStatus(string definitionId, out NpcStatusData status)
    {
        status = null;
        return !string.IsNullOrWhiteSpace(definitionId)
            && selectedDefinitions != null
            && selectedDefinitionBindings != null
            && selectedDefinitions.Statuses.TryGetValue(definitionId, out DailyV1StatusDefinition statusDefinition)
            && selectedDefinitionBindings.TryGetAsset(statusDefinition, out status)
            && status != null;
    }

    private static string ResolveActionDisplayName(SimulationConfigData configuration, string definitionId)
    {
        if (configuration == null) return null;
        foreach (NpcActionData action in configuration.Actions)
            if (action != null
                && string.Equals(action.DefinitionId, definitionId, System.StringComparison.Ordinal))
                return action.actionName;
        return null;
    }

    private static string ResolveSelectedActionDisplayName(
        DailyV1EffectiveDefinitions definitions,
        string definitionId)
    {
        return definitions != null && !string.IsNullOrWhiteSpace(definitionId)
            && definitions.Actions.TryGetValue(definitionId, out DailyV1ActionDefinition action)
            ? action.Values.actionName
            : null;
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
