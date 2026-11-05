using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

internal enum DailyV1P12FCoreOwnerRestoreStep
{
    PoliticalKnowledge = 0,
    ScheduledDirectives = 1,
    ActorChoices = 2,
    TravelParties = 3,
    Expeditions = 4
}

/// <summary>Completed private Core owner stages whose host projections must be made in order.</summary>
internal enum DailyV1RestoredImageCoreOwnerStageBoundary : byte
{
    None = 0,
    P12DOwners = 1,
    RuntimeAuthorities = 2,
    P12EOwners = 3,
    P12FOwners = 4,
    CoreSessionComposition = 5
}

internal enum DailyV1RestoredImageOwnerVectorStage : byte
{
    Captured = 1
}

/// <summary>Private selected Core owner candidates staged together for P12-F.</summary>
internal sealed class DailyV1P12FCoreOwnerRestoreStage
{
    internal PoliticalKnowledgeStore PoliticalKnowledge { get; }
    internal ScheduledDirectiveStore ScheduledDirectives { get; }
    internal ActorChoiceStore ActorChoices { get; }
    internal TravelPartyStore TravelParties { get; }
    internal ExpeditionStore Expeditions { get; }

    internal DailyV1P12FCoreOwnerRestoreStage(
        PoliticalKnowledgeStore politicalKnowledge,
        ScheduledDirectiveStore scheduledDirectives,
        ActorChoiceStore actorChoices,
        TravelPartyStore travelParties,
        ExpeditionStore expeditions)
    {
        PoliticalKnowledge = politicalKnowledge ?? throw new ArgumentNullException(nameof(politicalKnowledge));
        ScheduledDirectives = scheduledDirectives ?? throw new ArgumentNullException(nameof(scheduledDirectives));
        ActorChoices = actorChoices ?? throw new ArgumentNullException(nameof(actorChoices));
        TravelParties = travelParties ?? throw new ArgumentNullException(nameof(travelParties));
        Expeditions = expeditions ?? throw new ArgumentNullException(nameof(expeditions));
    }
}

/// <summary>One ordered, unpublished Core owner staging operation for a restored image.</summary>
internal sealed class DailyV1RestoredImageCoreOwnerStages
{
    private readonly DailyV1Application application;
    private readonly P12AContinuationImage image;
    private readonly DailyV1EffectiveDefinitions definitions;
    private readonly DailyV1ActiveRuntimeSession expectedSession;
    private readonly DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation;
    private bool failed;

    internal P12ADailyV1Census Census { get; }
    internal P12ADailyV1ImageOwnerSnapshots Snapshots { get; }
    internal P12CStagedCoreContinuationRoot CoreRoots { get; }
    internal SimulationTime StagedTime { get; }
    internal DailyV1P12DCoreRestoreOwnerStage D { get; private set; }
    internal NpcDecisionStore DecisionStore { get; private set; }
    internal DailyV1DomainEventAuthority EventAuthority { get; private set; }
    internal P12ECoreOwnerRestoreStage E { get; private set; }
    internal DailyV1P12FCoreOwnerRestoreStage F { get; private set; }
    internal ExplorableSiteStore EmptyExplorableSites { get; private set; }
    internal DailyV1ActiveRuntimeSession CoreSession { get; private set; }
    internal SimulationCalendar CoreCalendar { get; private set; }

    internal P12AContinuationImage Image => image;
    internal DailyV1EffectiveDefinitions Definitions => definitions;
    internal DailyV1ActiveRuntimeSession ExpectedSession => expectedSession;
    internal DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation ReplacementOperation => replacementOperation;
    internal bool CanStageD => !failed && D == null && E == null && F == null;
    internal bool CanPrepareRuntimeAuthorities => !failed && D != null
        && DecisionStore == null && EventAuthority == null && E == null && F == null;
    internal bool CanStageE => !failed && D != null && DecisionStore != null
        && EventAuthority != null && E == null && F == null;
    internal bool CanStageF => !failed && D != null && E != null && F == null;
    internal bool IsComplete => !failed && D != null && DecisionStore != null
        && EventAuthority != null && E != null && F != null && EmptyExplorableSites != null;
    internal bool CanComposeCoreSession => IsComplete && CoreSession == null && CoreCalendar == null;

    internal DailyV1RestoredImageCoreOwnerStages(
        DailyV1Application application,
        P12AContinuationImage image,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        P12ADailyV1Census census,
        P12ADailyV1ImageOwnerSnapshots snapshots,
        P12CStagedCoreContinuationRoot coreRoots,
        SimulationTime stagedTime)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        this.image = image ?? throw new ArgumentNullException(nameof(image));
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.expectedSession = expectedSession;
        this.replacementOperation = replacementOperation;
        Census = census ?? throw new ArgumentNullException(nameof(census));
        Snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        CoreRoots = coreRoots ?? throw new ArgumentNullException(nameof(coreRoots));
        StagedTime = stagedTime ?? throw new ArgumentNullException(nameof(stagedTime));
    }

    internal bool IsFor(
        DailyV1Application candidateApplication,
        P12AContinuationImage candidateImage,
        DailyV1EffectiveDefinitions candidateDefinitions,
        P12CStagedCoreContinuationRoot candidateCoreRoots,
        DailyV1ActiveRuntimeSession candidateExpectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation candidateReplacementOperation) =>
        !failed
        && ReferenceEquals(application, candidateApplication)
        && ReferenceEquals(image, candidateImage)
        && ReferenceEquals(definitions, candidateDefinitions)
        && ReferenceEquals(CoreRoots, candidateCoreRoots)
        && ReferenceEquals(expectedSession, candidateExpectedSession)
        && ReferenceEquals(replacementOperation, candidateReplacementOperation);

    internal bool TryAcceptD(DailyV1P12DCoreRestoreOwnerStage staged)
    {
        if (!CanStageD || staged == null) return false;
        D = staged;
        return true;
    }

    internal bool TryAcceptE(P12ECoreOwnerRestoreStage staged)
    {
        if (!CanStageE || staged == null) return false;
        E = staged;
        return true;
    }

    internal bool TryAcceptRuntimeAuthorities(
        NpcDecisionStore decisionStore,
        DailyV1DomainEventAuthority eventAuthority)
    {
        if (!CanPrepareRuntimeAuthorities || decisionStore == null || eventAuthority == null
            || !eventAuthority.IsFor(CoreRoots.RuntimeIdAllocator, StagedTime,
                CoreRoots.RecordSequence, decisionStore))
            return false;
        DecisionStore = decisionStore;
        EventAuthority = eventAuthority;
        return true;
    }

    internal bool HasRuntimeAuthorities(
        NpcDecisionStore decisionStore,
        DailyV1DomainEventAuthority eventAuthority) =>
        !failed && D != null
        && ReferenceEquals(DecisionStore, decisionStore)
        && ReferenceEquals(EventAuthority, eventAuthority);

    internal bool MatchesCompositionInputs(
        DailyV1Application candidateApplication,
        P12AContinuationImage candidateImage,
        DailyV1EffectiveDefinitions candidateDefinitions,
        P12CStagedCoreContinuationRoot candidateCoreRoots,
        DailyV1ActiveRuntimeSession candidateExpectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation candidateReplacementOperation,
        SimulationTime candidateTime,
        DailyV1P12DCoreRestoreOwnerStage candidateD,
        P12ECoreOwnerRestoreStage candidateE,
        DailyV1P12FCoreOwnerRestoreStage candidateF,
        NpcDecisionStore candidateDecisionStore,
        DailyV1DomainEventAuthority candidateEventAuthority) =>
        IsFor(candidateApplication, candidateImage, candidateDefinitions, candidateCoreRoots,
            candidateExpectedSession, candidateReplacementOperation)
        && (CanComposeCoreSession || (CoreSession != null && CoreCalendar != null))
        && ReferenceEquals(StagedTime, candidateTime)
        && ReferenceEquals(D, candidateD)
        && ReferenceEquals(E, candidateE)
        && ReferenceEquals(F, candidateF)
        && HasRuntimeAuthorities(candidateDecisionStore, candidateEventAuthority);

    internal bool TryAcceptCoreSession(
        DailyV1ActiveRuntimeSession candidate,
        SimulationCalendar candidateCalendar,
        SimulationRuntimeAdmissionContext candidateAdmissionContext,
        out string diagnostic)
    {
        diagnostic = null;
        if (!CanComposeCoreSession || candidate == null || candidateCalendar == null || candidateAdmissionContext == null
            || !ReferenceEquals(candidate.Calendar, candidateCalendar)
            || !ReferenceEquals(candidate.Definitions, definitions)
            || !ReferenceEquals(candidate.EventAuthority, EventAuthority)
            || !ReferenceEquals(candidate.SimulationTime, StagedTime)
            || !ReferenceEquals(candidate.AdmissionContext, candidateAdmissionContext)
            || !candidateAdmissionContext.IsRestoredContinuation
            || !candidateAdmissionContext.IsOwnedByCurrentThread())
        {
            diagnostic = "The candidate session does not retain the exact image definitions, time, event authority, or restored admission identity.";
            return false;
        }

        DailyV1ExecutionBoundaryState state = candidate.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = candidate.OwnerGraph;
        if (state == null || !ReferenceEquals(state.SimulationTime, StagedTime)
            || !ReferenceEquals(state.AdmissionContext, candidateAdmissionContext)
            || !ReferenceEquals(state.WorldId, CoreRoots.WorldIdentity)
            || !ReferenceEquals(state.SelectedRuntimeIdAllocator, CoreRoots.RuntimeIdAllocator)
            || !ReferenceEquals(state.SelectedRecordSequence, CoreRoots.RecordSequence)
            || !ReferenceEquals(state.SelectedRandomSource, CoreRoots.DeterministicRandom)
            || !ReferenceEquals(state.SelectedDecisionStore, DecisionStore)
            || !ReferenceEquals(state.SelectedActorChoiceStore, F.ActorChoices))
        {
            diagnostic = "The candidate Core execution state does not retain the exact staged P12-C roots and P12-F decision owners.";
            return false;
        }

        if (graph == null
            || !ReferenceEquals(graph.Definitions, definitions)
            || !ReferenceEquals(graph.SpatialNetwork, D.SpatialNetwork)
            || !ReferenceEquals(graph.RuntimeIdentityIndex, D.RuntimeIdentityIndex)
            || !ReferenceEquals(graph.PersonStore, D.Persons)
            || !ReferenceEquals(graph.GenealogyStore, D.Genealogy)
            || !ReferenceEquals(graph.JusticeRecordsOwner, E.JusticeRecords)
            || !ReferenceEquals(graph.PoliticalKnowledgeStore, F.PoliticalKnowledge)
            || !ReferenceEquals(graph.ScheduledDirectives, F.ScheduledDirectives)
            || !ReferenceEquals(graph.TravelParties, F.TravelParties)
            || !ReferenceEquals(graph.Expeditions, F.Expeditions))
        {
            diagnostic = "The candidate Core owner graph does not retain the exact staged P12-D/E/F authority instances.";
            return false;
        }

        if (candidate.EventAuthority.DecisionOccurrenceReceipts == null
            || !ReferenceEquals(candidate.EventAuthority.DecisionOccurrenceReceipts.DecisionStore, DecisionStore))
        {
            diagnostic = "The candidate Core event authority is not bound to the staged decision store.";
            return false;
        }

        if (!SameReferences(graph.Cities, D.Cities) || !SameReferences(graph.Npcs, D.Npcs))
        {
            diagnostic = "The candidate Core owner graph changed the ordered P12-D City or NPC owner instances.";
            return false;
        }

        CoreSession = candidate;
        CoreCalendar = candidateCalendar;
        return true;
    }

    private static bool SameReferences<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected)
        where T : class
    {
        if (actual == null || expected == null || actual.Count != expected.Count) return false;
        for (int i = 0; i < actual.Count; i++)
            if (!ReferenceEquals(actual[i], expected[i])) return false;
        return true;
    }

    internal bool TryAcceptF(DailyV1P12FCoreOwnerRestoreStage staged, ExplorableSiteStore emptyExplorableSites)
    {
        if (!CanStageF || staged == null || emptyExplorableSites == null) return false;
        F = staged;
        EmptyExplorableSites = emptyExplorableSites;
        return true;
    }

    internal void Fail()
    {
        failed = true;
        D = null;
        DecisionStore = null;
        EventAuthority = null;
        E = null;
        F = null;
        EmptyExplorableSites = null;
        CoreSession = null;
        CoreCalendar = null;
    }
}
/// <summary>
/// Core-owned lifetime and operation gate for one selected Daily-v1
/// application. Host adapters may retain projections, but this instance owns
/// the exact active Core session and its serialized publication boundary.
/// </summary>
internal enum DailyV1ApplicationStartPreparationKind : byte
{
    RestoreRequired = 1,
    GenesisPrepared = 2
}

/// <summary>
/// Result of Core's single initial-slot admission decision. A restored image
/// is returned without constructing genesis and with an unstarted logger; a missing slot returns a private
/// Core genesis candidate with its logger begun. Neither state is published until the
/// runtime boundary is admitted by the application gate.
/// </summary>
internal sealed class DailyV1ApplicationStartPreparation
{
    internal DailyV1Application Application { get; }
    internal DailyV1ApplicationStartPreparationKind Kind { get; }
    internal P12AContinuationImage RestoreImage { get; }
    internal DailyV1GenesisComposition GenesisComposition { get; }
    internal DailyV1ActiveRuntimeSession CoreSession { get; }
    internal SimulationLogger Logger { get; }
    internal DailyV1ApplicationStartAdmission StartAdmission { get; }
    internal P12AResult PersistenceResult { get; }

    internal DailyV1ApplicationStartPreparation(
        DailyV1Application application,
        DailyV1ApplicationStartPreparationKind kind,
        P12AContinuationImage restoreImage,
        DailyV1GenesisComposition genesisComposition,
        DailyV1ActiveRuntimeSession coreSession,
        SimulationLogger logger,
        DailyV1ApplicationStartAdmission startAdmission,
        P12AResult persistenceResult)
    {
        Application = application ?? throw new ArgumentNullException(nameof(application));
        if (!Enum.IsDefined(typeof(DailyV1ApplicationStartPreparationKind), kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if ((kind == DailyV1ApplicationStartPreparationKind.RestoreRequired
                && (restoreImage == null || genesisComposition != null || coreSession != null))
            || (kind == DailyV1ApplicationStartPreparationKind.GenesisPrepared
                && (restoreImage != null || genesisComposition == null || coreSession == null)))
            throw new ArgumentException("The initial start preparation does not match its selected admission path.");

        Kind = kind;
        RestoreImage = restoreImage;
        GenesisComposition = genesisComposition;
        CoreSession = coreSession;
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        StartAdmission = startAdmission ?? throw new ArgumentNullException(nameof(startAdmission));
        if (!ReferenceEquals(StartAdmission.Application, Application)
            || !ReferenceEquals(StartAdmission.PersistenceResult, persistenceResult)
            || (Kind == DailyV1ApplicationStartPreparationKind.RestoreRequired
                && (StartAdmission.Kind != DailyV1ApplicationStartAdmissionKind.RestoreRequired
                    || !ReferenceEquals(StartAdmission.RestoreImage, RestoreImage)))
            || (Kind == DailyV1ApplicationStartPreparationKind.GenesisPrepared
                && StartAdmission.Kind != DailyV1ApplicationStartAdmissionKind.GenesisRequired))
            throw new ArgumentException("The start preparation does not retain its exact admission token.", nameof(startAdmission));
        PersistenceResult = persistenceResult ?? throw new ArgumentNullException(nameof(persistenceResult));
    }
}

internal enum DailyV1ApplicationStartAdmissionKind : byte
{
    RestoreRequired = 1,
    GenesisRequired = 2
}

/// <summary>
/// Carries the exact initial P12 slot decision made by one application into
/// its matching restore projection or genesis preparation.
/// </summary>
internal sealed class DailyV1ApplicationStartAdmission
{
    internal DailyV1Application Application { get; }
    internal DailyV1ApplicationStartAdmissionKind Kind { get; }
    internal P12AContinuationImage RestoreImage { get; }
    internal P12AResult PersistenceResult { get; }

    internal DailyV1ApplicationStartAdmission(
        DailyV1Application application,
        DailyV1ApplicationStartAdmissionKind kind,
        P12AContinuationImage restoreImage,
        P12AResult persistenceResult)
    {
        Application = application ?? throw new ArgumentNullException(nameof(application));
        if (!Enum.IsDefined(typeof(DailyV1ApplicationStartAdmissionKind), kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (persistenceResult == null
            || (kind == DailyV1ApplicationStartAdmissionKind.RestoreRequired
                && (restoreImage == null || persistenceResult.Code != P12AResultCode.Loaded))
            || (kind == DailyV1ApplicationStartAdmissionKind.GenesisRequired
                && (restoreImage != null || persistenceResult.Code != P12AResultCode.NoSave)))
            throw new ArgumentException("The start admission does not match its exact initial slot result.");

        Kind = kind;
        RestoreImage = restoreImage;
        PersistenceResult = persistenceResult;
    }
}

internal sealed class DailyV1Application
{
    private readonly DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession> activeSessionGate =
        new DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>(
            session => session.ExecutionBoundaryState);
    private readonly Dictionary<DailyV1ActiveRuntimeSession, IDailyV1ApplicationHostProjection>
        activeHostProjections = new Dictionary<DailyV1ActiveRuntimeSession, IDailyV1ApplicationHostProjection>();
    private SimulationRuntimeAdmissionContext bootstrapAdmissionContext;
    private bool initialContinuationLoadAttempted;
    private bool initialContinuationLoadBlocksGenesis;
    private P12AResult initialContinuationLoadResult;
    private P12AContinuationImage initialContinuationImage;
    private bool initialGenesisPreparationAttempted;
    private DailyV1GenesisComposition preparedInitialGenesis;
    private DailyV1ActiveRuntimeSession preparedInitialSession;

    // Construction remains internal until the complete TryStart path can
    // create and return the application only after genesis or restore commits.
    internal DailyV1Application() { }

    /// <summary>
    /// Makes the one Core-owned initial admission decision before allocating a
    /// world identity. A present continuation is returned for detached restore;
    /// only a proven NoSave result may prepare genesis. Publication remains a
    /// separate serialized operation after the matching runtime boundary is
    /// ready, so this is not the public TryStart entry point.
    /// </summary>
    internal static bool TryPrepareStart(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host,
        out DailyV1ApplicationStartPreparation preparation,
        out P12AResult persistenceResult,
        out string diagnostic)
    {
        DailyV1Application application = new DailyV1Application();
        return application.TryPrepareStartOnExistingApplication(
            definitions, host, out preparation, out persistenceResult, out diagnostic);
    }
    internal bool TryPrepareStartOnExistingApplication(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host,
        out DailyV1ApplicationStartPreparation preparation,
        out P12AResult persistenceResult,
        out string diagnostic)
    {
        preparation = null;
        persistenceResult = new P12AResult(P12AResultCode.InvalidOwnerValue);
        diagnostic = null;
        if (definitions == null || host == null)
        {
            diagnostic = "The selected Daily-v1 definitions and explicit application host are required.";
            return false;
        }

        if (!TryAdmitInitialStart(
                definitions, host, out DailyV1ApplicationStartAdmission admission,
                out persistenceResult, out diagnostic))
            return false;

        if (admission.Kind == DailyV1ApplicationStartAdmissionKind.RestoreRequired)
        {
            try
            {
                SimulationLogger restoreLogger = CreateInitialLogger(definitions, host);
                preparation = new DailyV1ApplicationStartPreparation(
                    this, DailyV1ApplicationStartPreparationKind.RestoreRequired,
                    admission.RestoreImage, null, null, restoreLogger, admission, admission.PersistenceResult);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException
                || exception is OverflowException)
            {
                diagnostic = "Core could not initialize the admitted restore session logger: " + exception.Message;
                return false;
            }
        }

        if (admission.Kind != DailyV1ApplicationStartAdmissionKind.GenesisRequired)
        {
            diagnostic = "Initial continuation admission did not select a supported start path.";
            return false;
        }

        try
        {
            SimulationLogger logger = CreateInitialLogger(definitions, host);
            BeginInitialLogger(definitions, logger);

            if (!TryPrepareInitialGenesisStart(
                    admission, definitions, logger, host.StageCompleted,
                    out preparation, out diagnostic)
                || preparation == null
                || preparation.Kind != DailyV1ApplicationStartPreparationKind.GenesisPrepared
                || !ReferenceEquals(preparation.GenesisComposition.Definitions, definitions)
                || !ReferenceEquals(preparation.CoreSession.Definitions, definitions)
                || !ReferenceEquals(preparation.CoreSession.ExecutionBoundaryState.AdmissionContext,
                    bootstrapAdmissionContext))
            {
                if (string.IsNullOrWhiteSpace(diagnostic))
                    diagnostic = "Core genesis did not retain the admitted definitions and bootstrap identity.";
                preparation = null;
                return false;
            }
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            preparation = null;
            diagnostic = "Core could not prepare the admitted initial Daily-v1 genesis: " + exception.Message;
            return false;
        }
    }

    private static SimulationLogger CreateInitialLogger(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host)
    {
        DailyV1SimulationConfigData configuration = definitions.Configuration;
        Action<DailyV1LogSeverity, string> logOutput = host.LogOutput
            ?? ((severity, message) => { });
        DailyV1SimulationLogSettings selectedLogSettings = configuration?.logSettings;
        SimulationLogSettings logSettings = selectedLogSettings == null
            ? new SimulationLogSettings()
            : new SimulationLogSettings
            {
                showDay = selectedLogSettings.showDay,
                npcActions = selectedLogSettings.npcActions,
                trade = selectedLogSettings.trade,
                travel = selectedLogSettings.travel,
                crime = selectedLogSettings.crime,
                justice = selectedLogSettings.justice,
                economyProduction = selectedLogSettings.economyProduction,
                economyConsumption = selectedLogSettings.economyConsumption,
                market = selectedLogSettings.market
            };
        SimulationLogger logger = new SimulationLogger(
            logSettings, host.PersistentDataRoot, logOutput);
        return logger;
    }
    private static void BeginInitialLogger(
        DailyV1EffectiveDefinitions definitions,
        SimulationLogger logger)
    {
        DailyV1SimulationConfigData configuration = definitions.Configuration;
        logger.BeginSimulation(
            configuration?.simulationName,
            configuration?.enabledModules,
            definitions.Cities.Count,
            definitions.Npcs.Count);
    }

    internal bool TryCreateInitialGenesisLogger(
        DailyV1ApplicationStartAdmission admission,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host,
        out SimulationLogger logger,
        out string diagnostic)
    {
        logger = null;
        diagnostic = null;
        if (!IsForInitialGenesisAdmission(admission))
        {
            diagnostic = "Core genesis logger creation requires this application's exact NoSave admission.";
            return false;
        }
        if (definitions == null || host == null)
        {
            diagnostic = "The admitted definitions and explicit application host are required to create the Core logger.";
            return false;
        }

        try
        {
            logger = CreateInitialLogger(definitions, host);
            BeginInitialLogger(definitions, logger);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            logger = null;
            diagnostic = "Core could not initialize the admitted initial Daily-v1 logger: " + exception.Message;
            return false;
        }
    }

    internal bool TryPrepareInitialRestoreStart(
        DailyV1ApplicationStartAdmission admission,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host,
        out DailyV1ApplicationStartPreparation preparation,
        out string diagnostic)
    {
        preparation = null;
        diagnostic = null;
        if (admission == null
            || !IsForInitialRestoreAdmission(admission, admission.RestoreImage))
        {
            diagnostic = "Core restore preparation requires this application's exact loaded-image admission.";
            return false;
        }
        if (definitions == null || host == null)
        {
            diagnostic = "The admitted definitions and explicit application host are required to prepare the restore start.";
            return false;
        }

        try
        {
            SimulationLogger restoreLogger = CreateInitialLogger(definitions, host);
            preparation = new DailyV1ApplicationStartPreparation(
                this, DailyV1ApplicationStartPreparationKind.RestoreRequired,
                admission.RestoreImage, null, null, restoreLogger, admission, admission.PersistenceResult);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            preparation = null;
            diagnostic = "Core could not initialize the admitted restore session logger: " + exception.Message;
            return false;
        }
    }
    /// <summary>
    /// Captures the selected bootstrap owner-thread identity once, inside the
    /// Core application that will own the resulting active session.
    /// </summary>
    internal bool TryCaptureBootstrapAdmissionContext(
        out SimulationRuntimeAdmissionContext admissionContext)
    {
        lock (activeSessionGate.SyncRoot)
        {
            admissionContext = null;
            if (bootstrapAdmissionContext != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
                return false;

            bootstrapAdmissionContext = new SimulationRuntimeAdmissionContext(
                SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                Thread.CurrentThread,
                Thread.CurrentThread.ManagedThreadId);
            admissionContext = bootstrapAdmissionContext;
            return true;
        }
    }

    /// <summary>
    /// Prepares selected Daily-v1 genesis using Core-owned WorldId allocation.
    /// Production host adapters may observe the established stage sequence,
    /// but cannot supply or replace runtime identities. The allocator-injection
    /// overload below remains an internal deterministic test seam.
    /// </summary>
    internal bool TryPrepareInitialGenesis(
        DailyV1EffectiveDefinitions definitions,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryPrepareInitialGenesisCore(
            definitions, null, null, stageCompleted, out composition, out candidate, out diagnostic);

    /// <summary>Selected Unity host supplies its existing diagnostic sink; Core still creates all runtime roots.</summary>
    internal bool TryPrepareInitialGenesisWithHostLogger(
        DailyV1EffectiveDefinitions definitions,
        SimulationLogger diagnosticLogger,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryPrepareInitialGenesisCore(
            definitions, null, diagnosticLogger, stageCompleted,
            out composition, out candidate, out diagnostic);

    /// <summary>
    /// Performs the initial slot read once and binds its exact NoSave or
    /// restored-image result to this application instance.
    /// </summary>
    internal bool TryAdmitInitialStart(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host,
        out DailyV1ApplicationStartAdmission admission,
        out P12AResult result,
        out string diagnostic)
    {
        admission = null;
        result = new P12AResult(P12AResultCode.Busy);
        diagnostic = null;

        bool loaded = TryLoadInitialContinuationImageForStart(
            definitions, host, out P12AContinuationImage image, out result);
        if (loaded)
        {
            if (image == null || result == null || result.Code != P12AResultCode.Loaded)
            {
                diagnostic = "Initial continuation admission succeeded without its exact loaded image and result.";
                return false;
            }

            admission = new DailyV1ApplicationStartAdmission(
                this, DailyV1ApplicationStartAdmissionKind.RestoreRequired, image, result);
            return true;
        }

        if (result != null && result.Code == P12AResultCode.NoSave
            && TryGetInitialNoSaveAdmissionResult(out P12AResult admittedNoSaveResult)
            && ReferenceEquals(result, admittedNoSaveResult)
            && TryCreateInitialStartAdmissionFromNoSave(
                admittedNoSaveResult, out admission))
            return true;

        diagnostic = "Initial continuation admission did not prove an empty slot; genesis remains blocked."
            + (result == null ? string.Empty : " " + result.Code + ".");
        return false;
    }

    /// <summary>
    /// Binds a previously completed exact NoSave inspection to this
    /// application when Unity cannot construct its compatibility descriptor
    /// until after the normal bootstrap path continues.
    /// </summary>
    internal bool TryCreateInitialStartAdmissionFromNoSave(
        out DailyV1ApplicationStartAdmission admission,
        out string diagnostic)
    {
        admission = null;
        diagnostic = null;
        if (!TryGetInitialNoSaveAdmissionResult(out P12AResult result)
            || !TryCreateInitialStartAdmissionFromNoSave(result, out admission))
        {
            diagnostic = "Core cannot bind a genesis start without this application's completed NoSave result.";
            return false;
        }
        return true;
    }

    private bool TryCreateInitialStartAdmissionFromNoSave(
        P12AResult result,
        out DailyV1ApplicationStartAdmission admission)
    {
        admission = null;
        lock (activeSessionGate.SyncRoot)
        {
            if (result == null
                || !initialContinuationLoadAttempted
                || initialContinuationLoadBlocksGenesis
                || !ReferenceEquals(initialContinuationLoadResult, result)
                || result.Code != P12AResultCode.NoSave
                || bootstrapAdmissionContext == null
                || !bootstrapAdmissionContext.IsOwnedByCurrentThread()
                || initialGenesisPreparationAttempted
                || preparedInitialGenesis != null
                || preparedInitialSession != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
                return false;

            admission = new DailyV1ApplicationStartAdmission(
                this, DailyV1ApplicationStartAdmissionKind.GenesisRequired, null, result);
            return true;
        }
    }

    internal bool IsForInitialRestoreAdmission(
        DailyV1ApplicationStartAdmission admission,
        P12AContinuationImage image)
    {
        lock (activeSessionGate.SyncRoot)
        {
            return admission != null
                && image != null
                && ReferenceEquals(admission.Application, this)
                && admission.Kind == DailyV1ApplicationStartAdmissionKind.RestoreRequired
                && ReferenceEquals(admission.RestoreImage, image)
                && ReferenceEquals(initialContinuationImage, image)
                && initialContinuationLoadAttempted
                && initialContinuationLoadBlocksGenesis
                && ReferenceEquals(initialContinuationLoadResult, admission.PersistenceResult)
                && admission.PersistenceResult.Code == P12AResultCode.Loaded
                && bootstrapAdmissionContext != null
                && bootstrapAdmissionContext.IsOwnedByCurrentThread()
                && !initialGenesisPreparationAttempted
                && preparedInitialGenesis == null
                && preparedInitialSession == null
                && activeSessionGate.Current == null
                && activeSessionGate.ActiveOperationCount == 0;
        }
    }

    private bool IsForInitialGenesisAdmission(DailyV1ApplicationStartAdmission admission)
    {
        lock (activeSessionGate.SyncRoot)
        {
            return admission != null
                && ReferenceEquals(admission.Application, this)
                && admission.Kind == DailyV1ApplicationStartAdmissionKind.GenesisRequired
                && admission.RestoreImage == null
                && initialContinuationLoadAttempted
                && !initialContinuationLoadBlocksGenesis
                && ReferenceEquals(initialContinuationLoadResult, admission.PersistenceResult)
                && admission.PersistenceResult.Code == P12AResultCode.NoSave
                && bootstrapAdmissionContext != null
                && bootstrapAdmissionContext.IsOwnedByCurrentThread()
                && !initialGenesisPreparationAttempted
                && preparedInitialGenesis == null
                && preparedInitialSession == null
                && activeSessionGate.Current == null
                && activeSessionGate.ActiveOperationCount == 0;
        }
    }

    /// <summary>
    /// Packages the admitted NoSave genesis candidate only when the exact
    /// admission token belongs to this application instance.
    /// </summary>
    internal bool TryPrepareInitialGenesisStart(
        DailyV1ApplicationStartAdmission admission,
        DailyV1EffectiveDefinitions definitions,
        SimulationLogger diagnosticLogger,
        Action<string> stageCompleted,
        out DailyV1ApplicationStartPreparation preparation,
        out string diagnostic)
    {
        preparation = null;
        diagnostic = null;
        if (!IsForInitialGenesisAdmission(admission))
        {
            diagnostic = "Core genesis start preparation requires the exact NoSave admission owned by this application.";
            return false;
        }
        if (definitions == null || diagnosticLogger == null)
        {
            diagnostic = "The admitted definitions and Core logger are required to prepare the genesis start.";
            return false;
        }

        if (!TryGetInitialNoSaveAdmissionResult(out P12AResult admittedNoSaveResult)
            || !ReferenceEquals(admittedNoSaveResult, admission.PersistenceResult))
        {
            diagnostic = "Core genesis start preparation requires the exact admitted NoSave result.";
            return false;
        }

        if (!TryPrepareInitialGenesisWithHostLogger(
                definitions, diagnosticLogger, stageCompleted,
                out DailyV1GenesisComposition composition,
                out DailyV1ActiveRuntimeSession candidate,
                out diagnostic)
            || composition == null || candidate == null)
        {
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "Core did not prepare the exact admitted genesis composition and session.";
            return false;
        }

        if (!ReferenceEquals(composition.Definitions, definitions)
            || !ReferenceEquals(candidate.Definitions, definitions)
            || !ReferenceEquals(candidate.ExecutionBoundaryState.AdmissionContext,
                bootstrapAdmissionContext))
        {
            diagnostic = "Core genesis start preparation changed the admitted definitions or bootstrap identity.";
            return false;
        }

        if (!TryGetInitialNoSaveAdmissionResult(out P12AResult currentAdmissionResult)
            || !ReferenceEquals(admittedNoSaveResult, currentAdmissionResult))
        {
            diagnostic = "The Core continuation admission result changed while genesis was being prepared.";
            return false;
        }

        preparation = new DailyV1ApplicationStartPreparation(
            this, DailyV1ApplicationStartPreparationKind.GenesisPrepared,
            null, composition, candidate, diagnosticLogger,
            admission, admittedNoSaveResult);
        return true;
    }

    /// <summary>
    /// Packages the already-admitted NoSave genesis candidate for callers
    /// that have completed initial admission on this application instance.
    /// </summary>
    internal bool TryPrepareInitialGenesisStart(
        DailyV1EffectiveDefinitions definitions,
        SimulationLogger diagnosticLogger,
        Action<string> stageCompleted,
        out DailyV1ApplicationStartPreparation preparation,
        out string diagnostic)
    {
        preparation = null;
        diagnostic = null;
        if (!TryCreateInitialStartAdmissionFromNoSave(
                out DailyV1ApplicationStartAdmission admission, out diagnostic))
            return false;
        return TryPrepareInitialGenesisStart(
            admission, definitions, diagnosticLogger, stageCompleted,
            out preparation, out diagnostic);
    }
    private bool TryGetInitialNoSaveAdmissionResult(out P12AResult result)
    {
        lock (activeSessionGate.SyncRoot)
        {
            result = initialContinuationLoadResult;
            return initialContinuationLoadAttempted
                && !initialContinuationLoadBlocksGenesis
                && result != null
                && result.Code == P12AResultCode.NoSave;
        }
    }
    /// <summary>
    /// Deterministic identity-injection seam for Core tests. Production hosts
    /// use the overload without an allocator so identity authority remains in
    /// Core.
    /// </summary>
    internal bool TryPrepareInitialGenesis(
        DailyV1EffectiveDefinitions definitions,
        Func<WorldId> allocateWorldId,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryPrepareInitialGenesisCore(
            definitions, allocateWorldId, null, stageCompleted,
            out composition, out candidate, out diagnostic);

    private bool TryPrepareInitialGenesisCore(
        DailyV1EffectiveDefinitions definitions,
        Func<WorldId> allocateWorldId,
        SimulationLogger diagnosticLogger,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic)
    {
        composition = null;
        candidate = null;
        diagnostic = null;
        if (definitions == null)
        {
            diagnostic = "The selected Daily-v1 effective definitions are required.";
            return false;
        }

        SimulationRuntimeAdmissionContext admissionContext;
        lock (activeSessionGate.SyncRoot)
        {
            if (!initialContinuationLoadAttempted)
            {
                diagnostic = "Initial Daily-v1 genesis requires a completed continuation-slot admission that returned NoSave.";
                return false;
            }

            if (initialGenesisPreparationAttempted
                || initialContinuationLoadBlocksGenesis
                || preparedInitialGenesis != null
                || preparedInitialSession != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
            {
                diagnostic = "The Core application cannot capture a second bootstrap admission context or attempt another initial genesis session.";
                return false;
            }

            admissionContext = bootstrapAdmissionContext;
            if (admissionContext == null)
            {
                admissionContext = new SimulationRuntimeAdmissionContext(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    Thread.CurrentThread,
                    Thread.CurrentThread.ManagedThreadId);
                bootstrapAdmissionContext = admissionContext;
            }

            initialGenesisPreparationAttempted = true;
        }

        if (!DailyV1GenesisComposer.TryComposeSelectedOwnersWithHostLogger(
                definitions,
                admissionContext,
                allocateWorldId,
                stageCompleted,
                diagnosticLogger,
                out composition,
                out diagnostic))
        {
            composition = null;
            return false;
        }

        if (!DailyV1ActiveRuntimeSession.TryCreate(
                composition,
                composition.SimulationTime.AbsoluteDay,
                out candidate,
                out diagnostic))
        {
            composition = null;
            candidate = null;
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            if (activeSessionGate.Current != null
                || preparedInitialGenesis != null
                || preparedInitialSession != null)
            {
                composition = null;
                candidate = null;
                diagnostic = "The Core application changed while its initial genesis candidate was being composed.";
                return false;
            }

            preparedInitialGenesis = composition;
            preparedInitialSession = candidate;
        }

        return true;
    }

    /// <summary>
    /// Creates the Core session binding for a detached, already admitted
    /// restore candidate. The application checks that the restored execution
    /// state descends from this application's bootstrap and, for replacement,
    /// that the exact current session is still reserved by its owner thread.
    /// The Unity restore projection supplies no session authority.
    /// </summary>
    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic) =>
        TryCreateRestoredActiveRuntimeSession(
            executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
            null, null, replacementOperation, out candidate, out diagnostic);

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        SpatialAuthorityStore spatialAuthority,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic)
    {
        candidate = null;
        diagnostic = null;
        if (executionBoundaryState == null
            || executionBoundaryState.SelectedRuntimeIdAllocator == null
            || executionBoundaryState.SimulationTime == null
            || executionBoundaryState.SelectedRecordSequence == null
            || executionBoundaryState.SelectedDecisionStore == null)
        {
            diagnostic = "The restored Core session requires its exact execution identity and decision-store roots.";
            return false;
        }

        DailyV1DomainEventAuthority eventAuthority = DailyV1DomainEventAuthority.Create(
            executionBoundaryState.SelectedRuntimeIdAllocator,
            executionBoundaryState.SimulationTime,
            executionBoundaryState.SelectedRecordSequence, executionBoundaryState.SelectedDecisionStore);
        return TryCreateRestoredActiveRuntimeSession(
            executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
            p9Manifest, spatialAuthority, eventAuthority, replacementOperation,
            out candidate, out diagnostic);
    }

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        SpatialAuthorityStore spatialAuthority,
        DailyV1DomainEventAuthority eventAuthority,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out string diagnostic)
    {
        candidate = null;
        diagnostic = null;
        if (executionBoundaryState == null || expectedDefinitions == null)
        {
            diagnostic = "The restored Core session requires its exact execution boundary and effective definitions.";
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph ownerGraph = executionBoundaryState.SelectedRuntimeOwnerGraph;
        if (!executionBoundaryState.HasAdmittedSelectedDailyExecution
            || ownerGraph == null
            || !ReferenceEquals(ownerGraph.Definitions, expectedDefinitions)
            || eventAuthority == null
            || !eventAuthority.IsFor(
                executionBoundaryState.SelectedRuntimeIdAllocator,
                executionBoundaryState.SimulationTime,
                executionBoundaryState.SelectedRecordSequence, executionBoundaryState.SelectedDecisionStore))
        {
            diagnostic = "The restored Core execution boundary is not admitted for the exact selected definitions.";
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            SimulationRuntimeAdmissionContext admissionContext = bootstrapAdmissionContext;
            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (admissionContext == null
                || !admissionContext.IsOwnedByCurrentThread()
                || !IsBootstrapOrRestoredDescendant(executionBoundaryState.AdmissionContext)
                || !executionBoundaryState.AdmissionContext.IsOwnedByCurrentThread())
            {
                diagnostic = "The restored Core session does not descend from this application's current owner-thread bootstrap.";
                return false;
            }

            if (current == null)
            {
                if (replacementOperation != null
                    || activeSessionGate.ActiveOperationCount != 0
                    || initialGenesisPreparationAttempted
                    || preparedInitialGenesis != null
                    || preparedInitialSession != null)
                {
                    diagnostic = "Initial restoration requires an unused Core application with no active session or operation.";
                    return false;
                }
            }
            else if (replacementOperation == null
                || !ReferenceEquals(replacementOperation.ExpectedSession, current)
                || !replacementOperation.AuthorizesSessionReplacementCapture(
                    current.ExecutionBoundaryState)
                || activeSessionGate.ActiveOperationCount != 0)
            {
                diagnostic = "Restoration replacement requires the exact current Core session's owner-thread reservation.";
                return false;
            }

            bool sessionCreated = p9Manifest == null && spatialAuthority == null
                ? DailyV1ActiveRuntimeSession.TryCreate(
                    executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
                    eventAuthority, out candidate, out diagnostic)
                : DailyV1ActiveRuntimeSession.TryCreateRestored(
                    executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
                    p9Manifest, spatialAuthority, eventAuthority,
                    out candidate, out diagnostic);
            if (!sessionCreated)
            {
                candidate = null;
                return false;
            }

            if (current != null && !current.IsProfileCompatibleWith(candidate))
            {
                candidate = null;
                diagnostic = "The restored Core session is incompatible with the published profile.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Composes the selected Core owner graph, restored execution boundary,
    /// and unpublished application session in the owning application. A
    /// persistence host supplies already-staged Core owners and P12 roots; it
    /// cannot assemble or publish a competing Core session.
    /// </summary>
    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1EffectiveDefinitions definitions,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition calendarDefinition,
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        WorldId worldId,
        IReadOnlyList<DailyV1CityRuntimeOwners> cityOwners,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecords,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic) =>
        TryCreateRestoredActiveRuntimeSession(
            definitions, null, effectiveConfiguration, calendarDefinition,
            simulationTime, admissionContext, worldId, cityOwners, npcOwners,
            spatialNetwork, justiceRecords, travelParties, runtimeIdentityIndex,
            personStore, directives, expeditions, null, runtimeIdAllocator,
            randomSource, recordSequence, decisionStore, actorChoiceStore,
            lastEconomySnapshotDay, replacementOperation, out candidate,
            out calendar, out diagnostic);

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1EffectiveDefinitions definitions,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition calendarDefinition,
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        WorldId worldId,
        IReadOnlyList<DailyV1CityRuntimeOwners> cityOwners,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecords,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        SpatialAuthorityStore spatialAuthority,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        return TryCreateRestoredActiveRuntimeSession(
            definitions, p9Manifest, effectiveConfiguration, calendarDefinition,
            simulationTime, admissionContext, worldId, cityOwners, npcOwners,
            spatialNetwork, justiceRecords, travelParties, runtimeIdentityIndex,
            personStore, null, directives, expeditions, spatialAuthority,
            runtimeIdAllocator, randomSource, recordSequence, decisionStore,
            actorChoiceStore, lastEconomySnapshotDay, replacementOperation,
            out candidate, out calendar, out diagnostic);
    }

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1EffectiveDefinitions definitions,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition calendarDefinition,
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        WorldId worldId,
        IReadOnlyList<DailyV1CityRuntimeOwners> cityOwners,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecords,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        GenealogyStore genealogyStore,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        SpatialAuthorityStore spatialAuthority,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (runtimeIdAllocator == null || simulationTime == null
            || recordSequence == null || decisionStore == null)
        {
            diagnostic = "Restored Core session composition requires its exact runtime identity and decision-store roots.";
            return false;
        }

        DailyV1DomainEventAuthority eventAuthority = DailyV1DomainEventAuthority.Create(
            runtimeIdAllocator, simulationTime, recordSequence, decisionStore);
        return TryCreateRestoredActiveRuntimeSession(
            definitions, p9Manifest, effectiveConfiguration, calendarDefinition,
            simulationTime, admissionContext, worldId, cityOwners, npcOwners, spatialNetwork,
            justiceRecords, travelParties, runtimeIdentityIndex, personStore, genealogyStore, directives,
            expeditions, spatialAuthority, eventAuthority, runtimeIdAllocator, randomSource,
            recordSequence, decisionStore, actorChoiceStore, lastEconomySnapshotDay,
            replacementOperation, out candidate, out calendar, out diagnostic);
    }

    internal bool TryCreateRestoredActiveRuntimeSession(
        DailyV1EffectiveDefinitions definitions,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition calendarDefinition,
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        WorldId worldId,
        IReadOnlyList<DailyV1CityRuntimeOwners> cityOwners,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecords,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        GenealogyStore genealogyStore,
        ScheduledDirectiveStore directives,
        ExpeditionStore expeditions,
        SpatialAuthorityStore spatialAuthority,
        DailyV1DomainEventAuthority eventAuthority,
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (definitions == null
            || !ReferenceEquals(definitions.EffectiveConfiguration, effectiveConfiguration)
            || eventAuthority == null
            || !eventAuthority.IsFor(runtimeIdAllocator, simulationTime, recordSequence, decisionStore))
        {
            diagnostic = "Restored Core session composition requires the exact selected definitions and configuration.";
            return false;
        }

        if (!DailyV1RestoredExecutionBoundaryComposer.TryCreate(
                definitions,
                effectiveConfiguration,
                calendarDefinition,
                simulationTime,
                admissionContext,
                worldId,
                cityOwners,
                npcOwners,
                spatialNetwork,
                justiceRecords,
                travelParties,
                runtimeIdentityIndex,
                personStore,
                genealogyStore,
                directives,
                expeditions,
                runtimeIdAllocator,
                randomSource,
                recordSequence,
                decisionStore,
                actorChoiceStore,
                out DailyV1ExecutionBoundaryState executionBoundaryState,
                out SimulationCalendar candidateCalendar,
                out diagnostic))
            return false;

        if (!TryCreateRestoredActiveRuntimeSession(
                executionBoundaryState,
                definitions,
                lastEconomySnapshotDay,
                p9Manifest,
                spatialAuthority,
                eventAuthority,
                replacementOperation,
                out DailyV1ActiveRuntimeSession candidateSession,
                out diagnostic))
            return false;

        candidate = candidateSession;
        calendar = candidateCalendar;
        return true;
    }

    /// <summary>
    /// Composes one application session from the exact Core-owned P12-C/D/E/F
    /// stages. P12-C may be a detached image root or an authorized private
    /// boundary stage; Unity projections cannot reselect the owner instances
    /// used by the active Core graph.
    /// </summary>
    internal bool TryCreateRestoredActiveRuntimeSessionFromOwnerStages(
        DailyV1EffectiveDefinitions definitions,
        EffectiveSimulationConfiguration effectiveConfiguration,
        P12CStagedCoreContinuationRoot stagedRoots,
        DailyV1P12DCoreRestoreOwnerStage stagedD,
        P12ECoreOwnerRestoreStage stagedE,
        DailyV1P12FCoreOwnerRestoreStage stagedF,
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        NpcDecisionStore decisionStore,
        DailyV1DomainEventAuthority eventAuthority,
        long lastEconomySnapshotDay,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (definitions == null || effectiveConfiguration == null
            || !ReferenceEquals(definitions.EffectiveConfiguration, effectiveConfiguration)
            || definitions.Calendar == null)
        {
            diagnostic = "Core restore composition requires the exact admitted definitions and effective configuration.";
            return false;
        }
        if (stagedRoots == null)
        {
            diagnostic = "Core restore composition requires the staged P12-C Core root.";
            return false;
        }
        if (stagedRoots.WorldIdentity == null || stagedRoots.RuntimeIdAllocator == null
            || stagedRoots.RecordSequence == null || stagedRoots.SpatialAuthority == null
            || stagedRoots.GenesisManifest == null || stagedRoots.DeterministicRandom == null)
        {
            diagnostic = "Core restore composition requires a complete detached P12-C root set.";
            return false;
        }
        if (stagedD == null || stagedD.SpatialNetwork == null || stagedD.RuntimeIdentityIndex == null
            || stagedD.Persons == null || stagedD.Genealogy == null
            || stagedD.Cities == null || stagedD.Npcs == null)
        {
            diagnostic = "Core restore composition requires a complete P12-D owner graph.";
            return false;
        }
        if (stagedE == null || stagedE.Persons == null
            || !ReferenceEquals(stagedE.Persons, stagedD.Persons)
            || stagedE.JusticeRecords == null)
        {
            diagnostic = "Core restore composition requires P12-E owners bound to the exact P12-D PersonStore.";
            return false;
        }
        if (stagedF == null || stagedF.PoliticalKnowledge == null
            || !stagedF.PoliticalKnowledge.IsBoundToPersonStore(stagedD.Persons)
            || stagedF.ScheduledDirectives == null || stagedF.ActorChoices == null
            || stagedF.TravelParties == null || stagedF.Expeditions == null)
        {
            diagnostic = "Core restore composition requires complete P12-F owners bound to the exact P12-D PersonStore.";
            return false;
        }
        if (simulationTime == null || admissionContext == null
            || !admissionContext.IsRestoredContinuation
            || !admissionContext.IsOwnedByCurrentThread()
            || !IsBootstrapOrRestoredDescendant(admissionContext))
        {
            diagnostic = "Core restore composition requires the current-thread restored continuation admission context.";
            return false;
        }
        if (decisionStore == null || eventAuthority == null
            || !eventAuthority.IsFor(stagedRoots.RuntimeIdAllocator, simulationTime,
                stagedRoots.RecordSequence, decisionStore))
        {
            diagnostic = "Core restore composition requires event and decision authorities bound to the staged P12-C roots.";
            return false;
        }

        CalendarDefinition calendarDefinition = definitions.Calendar.monthLengths != null
            && definitions.Calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(definitions.Calendar.monthLengths),
                definitions.Calendar.daysPerWeek)
            : new CalendarDefinition(definitions.Calendar.monthsPerYear,
                definitions.Calendar.weeksPerMonth, definitions.Calendar.daysPerWeek);

        if (!TryCreateRestoredActiveRuntimeSession(
            definitions,
            stagedRoots.GenesisManifest,
            effectiveConfiguration,
            calendarDefinition,
            simulationTime,
            admissionContext,
            stagedRoots.WorldIdentity,
            stagedD.Cities,
            stagedD.Npcs,
            stagedD.SpatialNetwork,
            stagedE.JusticeRecords,
            stagedF.TravelParties,
            stagedD.RuntimeIdentityIndex,
            stagedD.Persons,
            stagedD.Genealogy,
            stagedF.ScheduledDirectives,
            stagedF.Expeditions,
            stagedRoots.SpatialAuthority,
            eventAuthority,
            stagedRoots.RuntimeIdAllocator,
            stagedRoots.DeterministicRandom,
            stagedRoots.RecordSequence,
            decisionStore,
            stagedF.ActorChoices,
            lastEconomySnapshotDay,
            replacementOperation,
            out candidate,
            out calendar,
            out diagnostic))
            return false;

        if (candidate == null
            || !candidate.OwnerGraph.TryBindPoliticalKnowledgeStore(stagedF.PoliticalKnowledge))
        {
            candidate = null;
            calendar = null;
            diagnostic = "The restored Core owner graph could not bind the exact P12-F PoliticalKnowledge owner to its P12-D PersonStore.";
            return false;
        }

        return true;
    }

    internal object ActiveSessionSyncRoot => activeSessionGate.SyncRoot;
    internal DailyV1ActiveRuntimeSession ActiveSession => activeSessionGate.Current;
    internal int ActiveOperationCount => activeSessionGate.ActiveOperationCount;

    internal bool TryBeginOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1OperationScopeKind scopeKind,
        out DailyV1ActiveRuntimeSession session) =>
        activeSessionGate.TryBeginOperation(expectedSession, scopeKind, out session);

    /// <summary>
    /// Starts the single Core-owned P12-G save operation and captures the
    /// exact current completed-boundary witness before detached staging. The
    /// returned operation keeps the save scope registered until disposal and
    /// supports repeated source recapture between P12 stages.
    /// </summary>
    internal bool TryBeginContinuationSaveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        out DailyV1P12GContinuationSaveOperation operation,
        out DailyCaptureEligibilityFailure failure)
    {
        operation = null;
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (expectedSession == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !expectedSession.ExecutionBoundaryState.IsHealthyDailyOwnerThreadBoundary()
            || expectedSession.RuntimeAdvanceBoundary == null
            || !ReferenceEquals(expectedSession.RuntimeAdvanceBoundary.ExecutionState,
                expectedSession.ExecutionBoundaryState))
            return false;

        if (!activeSessionGate.TryBeginOperation(
                expectedSession, DailyV1OperationScopeKind.ContinuationSave,
                out DailyV1ActiveRuntimeSession admittedSession)
            || !ReferenceEquals(admittedSession, expectedSession))
            return false;

        DailyV1P12GContinuationSaveOperation candidateOperation = null;
        bool witnessCaptured;
        try
        {
            witnessCaptured = TryCaptureContinuationSourceWitness(expectedSession,
                out DailyV1P12GSourceBoundaryWitness witness, out failure);
            if (witnessCaptured)
            {
                candidateOperation = new DailyV1P12GContinuationSaveOperation(
                    this, expectedSession, witness);

                // Bind the P12-D City image source to the exact Core owner
                // graph while the continuation-save lease is active. The
                // Unity projection may later stage a detached candidate, but
                // it cannot choose a different City state for this image.
                if (!DailyV1P12GCoreBoundCityOwnerSources.TryCapture(
                        candidateOperation,
                        out DailyV1P12GCoreBoundCityOwnerSources cityOwnerSources,
                        out DailyV1P12GCoreCitySourceCaptureFailure cityCaptureFailure,
                        out _)
                    || !candidateOperation.TryBindCoreCityOwnerSources(cityOwnerSources))
                {
                    witnessCaptured = false;
                    failure = cityCaptureFailure == DailyV1P12GCoreCitySourceCaptureFailure.StaleSourceBoundary
                        ? DailyCaptureEligibilityFailure.StaleToken
                        : DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                }

                if (witnessCaptured
                    && (!DailyV1P12GCoreBoundDemographicOwnerSources.TryCapture(
                            candidateOperation,
                            out DailyV1P12GCoreBoundDemographicOwnerSources demographicOwnerSources,
                            out DailyV1P12GCoreBoundOwnerSourceCaptureFailure demographicCaptureFailure,
                            out _)
                        || !candidateOperation.TryBindCoreDemographicOwnerSources(demographicOwnerSources)))
                {
                    witnessCaptured = false;
                    failure = demographicCaptureFailure ==
                        DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary
                        ? DailyCaptureEligibilityFailure.StaleToken
                        : DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                }

                if (witnessCaptured
                    && (!DailyV1P12GCoreBoundSpatialOwnerSources.TryCapture(
                            candidateOperation,
                            out DailyV1P12GCoreBoundSpatialOwnerSources spatialOwnerSources,
                            out DailyV1P12GCoreBoundOwnerSourceCaptureFailure spatialCaptureFailure,
                            out _)
                        || !candidateOperation.TryBindCoreSpatialOwnerSources(spatialOwnerSources)))
                {
                    witnessCaptured = false;
                    failure = spatialCaptureFailure ==
                        DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary
                        ? DailyCaptureEligibilityFailure.StaleToken
                        : DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                }

                if (witnessCaptured)
                {
                    bool coreNpcSourcesCaptured =
                        DailyV1P12GCoreBoundNpcOwnerSources.TryCapture(
                            candidateOperation,
                            out DailyV1P12GCoreBoundNpcOwnerSources npcOwnerSources,
                            out DailyV1P12GCoreBoundOwnerSourceCaptureFailure npcCaptureFailure,
                            out _);
                    if (coreNpcSourcesCaptured
                        && (npcOwnerSources == null
                            || !candidateOperation.TryBindCoreNpcOwnerSources(npcOwnerSources)))
                    {
                        witnessCaptured = false;
                        failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                    }
                    else if (!coreNpcSourcesCaptured
                        && npcCaptureFailure !=
                            DailyV1P12GCoreBoundOwnerSourceCaptureFailure.IncompleteSelectedNpcRoster)
                    {
                        witnessCaptured = false;
                        failure = npcCaptureFailure ==
                            DailyV1P12GCoreBoundOwnerSourceCaptureFailure.StaleSourceBoundary
                            ? DailyCaptureEligibilityFailure.StaleToken
                            : DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                    }
                }
            }
        }
        catch (Exception captureException)
        {
            try
            {
                if (candidateOperation != null && candidateOperation.IsActive)
                    candidateOperation.Dispose();
                else
                    activeSessionGate.EndOperation(DailyV1OperationScopeKind.ContinuationSave);
            }
            catch (Exception cleanupException)
            {
                // Do not replace the original capture/construction failure if
                // cleanup also fails. Keep the cleanup diagnostic attached.
                try
                {
                    captureException.Data["ContinuationSaveScopeCleanupException"] = cleanupException;
                }
                catch
                {
                    // Exception data is diagnostic only; never mask the original failure.
                }
            }

            ExceptionDispatchInfo.Capture(captureException).Throw();
            throw;
        }

        if (!witnessCaptured)
        {
            if (candidateOperation != null && candidateOperation.IsActive)
                candidateOperation.Dispose();
            else
                activeSessionGate.EndOperation(DailyV1OperationScopeKind.ContinuationSave);
            return false;
        }

        operation = candidateOperation;
        return true;
    }

    internal bool TryRevalidateContinuationSaveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1P12GSourceBoundaryWitness expectedWitness,
        out DailyCaptureEligibilityFailure failure)
    {
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (expectedSession == null || expectedWitness == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !expectedSession.AdmissionContext.IsOwnedByCurrentThread())
            return false;

        if (!TryCaptureContinuationSourceWitness(expectedSession,
                out DailyV1P12GSourceBoundaryWitness currentWitness, out failure))
            return false;

        if (!expectedWitness.Matches(currentWitness))
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Seals a completed P12 image using exact operation-bound Core sections.
    /// Unity may provide compatibility sections for owners that have not yet
    /// migrated. Core-owned City, demographic, spatial, and available NPC
    /// sections are checked and replaced by Core; a mixed roster keeps its
    /// complete Unity NPC row projection for the members not present in Core.
    /// </summary>
    internal bool TryComposeCoreOwnedContinuationImage(
        DailyV1P12GContinuationSaveOperation operation,
        P12ACompatibilityDescriptor unityCompatibility,
        P12ACompatibilityDescriptorV2 dotNetCompatibility,
        long politicalWorldRevision,
        IReadOnlyList<P12AOwnerSection> adapterEncodedOwnerSections,
        out P12AContinuationImage image,
        out P12ADailyV1SectionManifest manifest,
        out P12AResult result)
    {
        image = null;
        manifest = null;
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (operation == null || !operation.IsActive || !operation.IsOwnedBy(this)
            || !ReferenceEquals(activeSessionGate.Current, operation.Session)
            || adapterEncodedOwnerSections == null)
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12d.core-selected-npc-sources");
            return false;
        }

        if (!operation.TryEncodeCoreP12DOwnerSections(
                out IReadOnlyList<P12AOwnerSection> p12DOwnerSections,
                out bool includesNpcOwnerSections,
                out result))
            return false;
        if (includesNpcOwnerSections != (operation.CoreNpcOwnerSources != null))
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12d.core-npc-source-coverage");
            return false;
        }

        DailyCaptureEligibilityToken token = operation.SourceWitness?.Token;
        if (token == null)
        {
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        if (!operation.TryEncodeCoreP12EOwnerSections(
                token.AbsoluteDay,
                out IReadOnlyList<P12AOwnerSection> p12EOwnerSections,
                out result)
            || !operation.TryEncodeCoreP12FOwnerSections(
                out IReadOnlyList<P12AOwnerSection> p12FOwnerSections,
                out result))
            return false;

        if (!TryMergeCoreOwnedContinuationSections(
                p12DOwnerSections,
                p12EOwnerSections,
                p12FOwnerSections,
                adapterEncodedOwnerSections,
                out IReadOnlyList<P12AOwnerSection> encodedOwnerSections,
                out result))
            return false;

        return TryComposeContinuationImage(
            operation,
            unityCompatibility,
            dotNetCompatibility,
            politicalWorldRevision,
            encodedOwnerSections,
            out image,
            out manifest,
            out result);
    }

    /// <summary>
    /// Combines operation-bound Core sections with adapter sections for owners
    /// that remain outside the selected Core graph. Equivalent overlap is
    /// accepted only after exact metadata, row, revision, and payload checks.
    /// </summary>
    internal static bool TryMergeCoreOwnedContinuationSections(
        IReadOnlyList<P12AOwnerSection> coreP12DOwnerSections,
        IReadOnlyList<P12AOwnerSection> coreP12EOwnerSections,
        IReadOnlyList<P12AOwnerSection> coreP12FOwnerSections,
        IReadOnlyList<P12AOwnerSection> adapterEncodedOwnerSections,
        out IReadOnlyList<P12AOwnerSection> encodedOwnerSections,
        out P12AResult result)
    {
        encodedOwnerSections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (coreP12DOwnerSections == null || coreP12EOwnerSections == null
            || coreP12FOwnerSections == null || adapterEncodedOwnerSections == null)
            return false;

        Dictionary<string, P12AOwnerSection> coreSections =
            new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        if (!TryAddCoreSections(coreSections, coreP12DOwnerSections)
            || !TryAddCoreSections(coreSections, coreP12EOwnerSections)
            || !TryAddCoreSections(coreSections, coreP12FOwnerSections))
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12a.core-owner-section-identity");
            return false;
        }

        List<P12AOwnerSection> merged =
            new List<P12AOwnerSection>(adapterEncodedOwnerSections.Count + coreSections.Count);
        HashSet<string> adapterSectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerSection adapterSection in adapterEncodedOwnerSections)
        {
            if (adapterSection == null || string.IsNullOrWhiteSpace(adapterSection.Id)
                || !adapterSectionIds.Add(adapterSection.Id))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection,
                    adapterSection?.Id);
                return false;
            }

            if (coreSections.TryGetValue(adapterSection.Id, out P12AOwnerSection coreSection))
            {
                if (!AreEncodedOwnerSectionsEqual(coreSection, adapterSection))
                {
                    result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                        "p12a.core-owner-section-conflict", adapterSection.Id);
                    return false;
                }
                continue;
            }

            merged.Add(adapterSection);
        }

        foreach (KeyValuePair<string, P12AOwnerSection> coreSection in coreSections)
            merged.Add(coreSection.Value);

        encodedOwnerSections = merged.AsReadOnly();
        result = null;
        return true;
    }

    private static bool TryAddCoreSections(
        Dictionary<string, P12AOwnerSection> destination,
        IReadOnlyList<P12AOwnerSection> source)
    {
        if (destination == null || source == null) return false;
        foreach (P12AOwnerSection section in source)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.Id)
                || !destination.TryAdd(section.Id, section))
                return false;
        }
        return true;
    }

    private static bool AreEncodedOwnerSectionsEqual(
        P12AOwnerSection left,
        P12AOwnerSection right)
    {
        if (left == null || right == null || left.Id != right.Id
            || left.Major != right.Major || left.Minor != right.Minor
            || left.Role != right.Role || left.AliasGroup != right.AliasGroup
            || left.Revision != right.Revision || left.Rows.Count != right.Rows.Count)
            return false;

        for (int index = 0; index < left.Rows.Count; index++)
        {
            P12AOwnerRow leftRow = left.Rows[index];
            P12AOwnerRow rightRow = right.Rows[index];
            if (leftRow.OwnerKey != rightRow.OwnerKey
                || leftRow.Revision != rightRow.Revision
                || !P12AContinuationCodec.BytesEqual(leftRow.Payload, rightRow.Payload))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Seals the typed owner sections into one immutable Daily-v1 image under
    /// the exact Core continuation-save operation. The owner vector comes
    /// from that operation's completed-boundary witness; a host cannot
    /// substitute a token, census vector, or owner authority.
    /// </summary>
    internal bool TryComposeContinuationImage(
        DailyV1P12GContinuationSaveOperation operation,
        P12ACompatibilityDescriptor unityCompatibility,
        P12ACompatibilityDescriptorV2 dotNetCompatibility,
        long politicalWorldRevision,
        IReadOnlyList<P12AOwnerSection> encodedOwnerSections,
        out P12AContinuationImage image,
        out P12ADailyV1SectionManifest manifest,
        out P12AResult result)
    {
        image = null;
        manifest = null;
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (operation == null || !operation.IsActive || !operation.IsOwnedBy(this)
            || !ReferenceEquals(activeSessionGate.Current, operation.Session))
            return false;

        DailyV1ActiveRuntimeSession session = operation.Session;
        DailyCaptureEligibilityToken token = operation.SourceWitness?.Token;
        DailyV1P12CRuntimeRoots roots = session.P12CRoots;
        if (session.AdmissionContext == null
            || !session.AdmissionContext.IsOwnedByCurrentThread())
        {
            result = new P12AResult(P12AResultCode.WrongThread);
            return false;
        }

        if (token == null || roots == null || !roots.IsFor(session))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                "p12g.application-image-session-roots");
            return false;
        }

        if ((unityCompatibility == null) == (dotNetCompatibility == null)
            || encodedOwnerSections == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                "p12a.application-image-compatibility");
            return false;
        }

        if (ContainsP12CRuntimeIdentitySections(encodedOwnerSections))
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12a.application-image-runtime-identities");
            return false;
        }

        if (!operation.TryRevalidate(out _))
            return false;

        if (!operation.TryCaptureCoreCausalRootSources(
                out DailyV1P12GCoreBoundCausalRootSources causalSources,
                out DailyV1P12GCoreBoundCausalRootCaptureFailure causalFailure,
                out _)
            || causalSources == null || !causalSources.IsFor(operation))
        {
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12c.continuation-roots." + causalFailure);
            return false;
        }

        P12ACausalRootImageSnapshots rootSnapshots = causalSources.Snapshots;
        if (rootSnapshots == null
            || !ReferenceEquals(rootSnapshots.P9Manifest, roots.Manifest)
            || !string.Equals(rootSnapshots.P9Manifest.SelectedP9ContractIdentity,
                roots.Manifest.SelectedP9ContractIdentity, StringComparison.Ordinal)
            || !P12ACSpatialAuthorityCodec.TryComputeFingerprint(
                rootSnapshots.SpatialAuthority, out string p8Fingerprint))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                "p12c.application-image-root-binding");
            return false;
        }

        string descriptorP9Fingerprint = unityCompatibility != null
            ? unityCompatibility.P9Fingerprint
            : dotNetCompatibility.P9Fingerprint;
        string descriptorP8Fingerprint = unityCompatibility != null
            ? unityCompatibility.P8Fingerprint
            : dotNetCompatibility.P8Fingerprint;
        string descriptorProfileId = unityCompatibility != null
            ? unityCompatibility.ProfileId
            : dotNetCompatibility.ProfileId;
        int descriptorProfileVersion = unityCompatibility != null
            ? unityCompatibility.ProfileVersion
            : dotNetCompatibility.ProfileVersion;
        if (!string.Equals(descriptorP9Fingerprint, roots.Manifest.Fingerprint,
                StringComparison.Ordinal)
            || !string.Equals(descriptorP8Fingerprint, p8Fingerprint,
                StringComparison.Ordinal)
            || !string.Equals(descriptorProfileId, P12AContinuationHeader.Profile,
                StringComparison.Ordinal)
            || descriptorProfileVersion != P12AContinuationHeader.ProfileVersion)
        {
            result = new P12AResult(P12AResultCode.IncompatibleContentOrConfiguration,
                "p12a.application-image-descriptor-roots");
            return false;
        }

        if (dotNetCompatibility != null && dotNetCompatibility.HostKind != 1)
        {
            result = new P12AResult(P12AResultCode.IncompatibleBuildOrRuntime,
                "p12a.application-image-host-kind");
            return false;
        }

        P12AContinuationHeader header = unityCompatibility != null
            ? new P12AContinuationHeader(
                token.WorldId.Value,
                token.AbsoluteDay,
                token.CompletedCoreSequence,
                session.ReportState.LastEconomySnapshotDay,
                roots.Manifest.SelectedP9ContractIdentity,
                roots.Manifest.Fingerprint,
                "P8-A",
                p8Fingerprint,
                politicalWorldRevision,
                unityCompatibility)
            : P12AContinuationHeader.ForDotNetV2(
                token.WorldId.Value,
                token.AbsoluteDay,
                token.CompletedCoreSequence,
                session.ReportState.LastEconomySnapshotDay,
                roots.Manifest.SelectedP9ContractIdentity,
                roots.Manifest.Fingerprint,
                "P8-A",
                p8Fingerprint,
                politicalWorldRevision,
                dotNetCompatibility);
        if (!header.IsValid
            || !string.Equals(header.WorldId, session.WorldId.Value, StringComparison.Ordinal)
            || !string.Equals(header.WorldId, token.WorldId.Value, StringComparison.Ordinal)
            || header.Day != token.AbsoluteDay
            || header.Sequence != token.CompletedCoreSequence
            || header.LastEconomySnapshotDay != session.ReportState.LastEconomySnapshotDay)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                "p12a.application-image-header");
            return false;
        }

        if (!P12ACausalRootImageCodec.TryEncode(
                header,
                rootSnapshots.WorldIdentity,
                rootSnapshots.Allocator,
                rootSnapshots.RecordSequence,
                rootSnapshots.SpatialAuthority,
                rootSnapshots.P9Manifest,
                rootSnapshots.RandomRoot,
                out P12ACausalRootPayload causalRoots,
                out IReadOnlyList<P12AOwnerSection> causalSections,
                out result))
            return false;

        if (!operation.TryEncodeCoreP12CRuntimeIdentitySections(
                out IReadOnlyList<P12AOwnerSection> coreRuntimeIdentitySections,
                out result))
            return false;

        if (!operation.TryRevalidate(out _))
            return false;

        List<P12AOwnerSection> encodedRequiredOwnerSections = new List<P12AOwnerSection>(
            causalSections.Count + coreRuntimeIdentitySections.Count + encodedOwnerSections.Count);
        encodedRequiredOwnerSections.AddRange(causalSections);
        encodedRequiredOwnerSections.AddRange(coreRuntimeIdentitySections);
        encodedRequiredOwnerSections.AddRange(encodedOwnerSections);
        if (!P12ADailyV1ImageComposer.TryCompose(
                header,
                causalRoots,
                operation.SourceWitness.TokenOwnerSections,
                encodedRequiredOwnerSections,
                out image,
                out manifest,
                out result))
        {
            image = null;
            manifest = null;
            return false;
        }

        // Composition traverses every typed section. Do not return a sealed
        // image if the exact completed boundary changed during that work.
        if (!operation.TryRevalidate(out _))
        {
            image = null;
            manifest = null;
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        result = null;
        return true;
    }

    private static bool ContainsP12CRuntimeIdentitySections(
        IReadOnlyList<P12AOwnerSection> sections)
    {
        if (sections == null) return false;
        foreach (P12AOwnerSection section in sections)
            if (section?.Id != null
                && section.Id.StartsWith("p12c.runtime-identities.", StringComparison.Ordinal))
                return true;
        return false;
    }

    /// <summary>
    /// Persists a P12-G image only while the exact Core continuation-save
    /// operation that authorized its capture remains active. This is the
    /// Core-owned census/encode/atomic-store half of the Unity migration
    /// route; image capture itself remains in the P12-G owner coordinator
    /// until its selected owner staging closure is moved into Core.
    /// </summary>
    internal bool TryPersistContinuationImage(
        DailyV1P12GContinuationSaveOperation operation,
        P12AContinuationImage image,
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        out P12AResult result)
    {
        result = new P12AResult(P12AResultCode.CaptureRejected);
        if (operation == null || !operation.IsActive || !operation.IsOwnedBy(this)
            || !ReferenceEquals(activeSessionGate.Current, operation.Session))
            return false;

        if (!operation.Session.AdmissionContext.IsOwnedByCurrentThread())
        {
            result = new P12AResult(P12AResultCode.WrongThread);
            return false;
        }

        if (image == null || image.Header == null || !image.Header.IsValid
            || string.IsNullOrWhiteSpace(persistentDataRoot) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!operation.TryRevalidate(out _))
            return false;

        if (!P12ADailyV1Census.TryCreateForImage(
                image, out P12ADailyV1Census census, out result)
            || !P12AContinuationCodec.TryEncode(
                image, census.Manifest, out byte[] bytes, out result))
            return false;

        // Encoding traverses every owner section. Confirm that the same
        // completed boundary still authorizes the bytes immediately before
        // the externally supplied storage operations begin.
        if (!operation.TryRevalidate(out _))
        {
            result = new P12AResult(P12AResultCode.CaptureRejected);
            return false;
        }

        if (image.Header.Compatibility != null && image.Header.CompatibilityV2 == null)
            return P12AContinuationStore.TrySave(bytes, census.Manifest,
                image.Header.Compatibility, persistentDataRoot, fileOperations, out result);

        if (image.Header.Compatibility == null && image.Header.CompatibilityV2 != null)
            return P12AContinuationStore.TrySaveDotNetV2(bytes, census.Manifest,
                image.Header.CompatibilityV2, persistentDataRoot, fileOperations, out result);

        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        return false;
    }

    internal void EndContinuationSaveOperation()
    {
        activeSessionGate.EndOperation(DailyV1OperationScopeKind.ContinuationSave);
    }

    private static bool TryCaptureContinuationSourceWitness(
        DailyV1ActiveRuntimeSession session,
        out DailyV1P12GSourceBoundaryWitness witness,
        out DailyCaptureEligibilityFailure failure)
    {
        witness = null;
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (session == null || session.RuntimeAdvanceBoundary == null
            || !session.ExecutionBoundaryState.TryCaptureContinuationSaveBoundaryEvidence(
                session.RuntimeAdvanceBoundary,
                out DailyCaptureEligibilityToken token,
                out IReadOnlyList<OwnerSectionCensusSnapshot> currentSections,
                out long mutationEpoch,
                out failure))
            return false;

        if (!DailyV1P12GSourceBoundaryWitness.TryCreate(
                token, currentSections, mutationEpoch, out witness))
        {
            failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
            return false;
        }

        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    internal void EndOperation(DailyV1OperationScopeKind scopeKind) =>
        activeSessionGate.EndOperation(scopeKind);

    internal bool TryPublishInitial(DailyV1ActiveRuntimeSession candidate)
    {
        if (candidate == null || candidate.AdmissionContext?.IsRestoredContinuation == true)
            return false;

        // A Core session is not active until its exact execution boundary is
        // bound. Publishing an unbound genesis value creates a session that
        // cannot advance and leaves the application with no valid runtime.
        if (candidate?.RuntimeAdvanceBoundary == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            if (preparedInitialSession != null)
            {
                if (!ReferenceEquals(preparedInitialSession, candidate)
                    || preparedInitialGenesis == null
                    || !ReferenceEquals(preparedInitialGenesis.ExecutionBoundaryState,
                        candidate.ExecutionBoundaryState)
                    || !ReferenceEquals(preparedInitialGenesis.Definitions,
                        candidate.Definitions))
                    return false;

                if (!IsBootstrapOrRestoredDescendant(candidate.AdmissionContext)
                    || !activeSessionGate.TryPublishInitial(candidate))
                    return false;

                preparedInitialGenesis = null;
                preparedInitialSession = null;
                return true;
            }

            if (initialGenesisPreparationAttempted
                || bootstrapAdmissionContext == null
                || !IsBootstrapOrRestoredDescendant(candidate.AdmissionContext))
                return false;
            return activeSessionGate.TryPublishInitial(candidate);
        }
    }

    /// <summary>
    /// Publishes the exact private genesis session produced by this
    /// application's admitted NoSave start preparation together with its host
    /// projection. The preparation token prevents a host from substituting a
    /// separately composed session between Core's slot decision and the
    /// single active-session publication.
    /// </summary>
    internal bool TryPublishPreparedInitialWithHostProjection(
        DailyV1ApplicationStartPreparation preparation,
        IDailyV1ApplicationHostProjection hostProjection,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        out string diagnostic)
    {
        diagnostic = null;
        lock (activeSessionGate.SyncRoot)
        {
            DailyV1ActiveRuntimeSession candidate = preparation?.CoreSession;
            P12AResult admittedResult = preparation?.StartAdmission?.PersistenceResult;
            if (preparation == null
                || !ReferenceEquals(preparation.Application, this)
                || preparation.Kind != DailyV1ApplicationStartPreparationKind.GenesisPrepared
                || preparation.StartAdmission == null
                || !ReferenceEquals(preparation.StartAdmission.Application, this)
                || preparation.StartAdmission.Kind != DailyV1ApplicationStartAdmissionKind.GenesisRequired
                || !ReferenceEquals(preparation.PersistenceResult, admittedResult)
                || admittedResult == null
                || admittedResult.Code != P12AResultCode.NoSave
                || !initialContinuationLoadAttempted
                || initialContinuationLoadBlocksGenesis
                || !ReferenceEquals(initialContinuationLoadResult, admittedResult)
                || !initialGenesisPreparationAttempted
                || !ReferenceEquals(preparedInitialGenesis, preparation.GenesisComposition)
                || !ReferenceEquals(preparedInitialSession, candidate)
                || candidate == null
                || !ReferenceEquals(candidate.GenesisComposition, preparation.GenesisComposition)
                || !ReferenceEquals(candidate.Definitions, preparation.GenesisComposition?.Definitions)
                || !ReferenceEquals(candidate.AdmissionContext, bootstrapAdmissionContext)
                || candidate.AdmissionContext?.IsOwnedByCurrentThread() != true
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
            {
                diagnostic = "Initial genesis publication requires this application's exact private NoSave preparation and unused session gate.";
                return false;
            }

            if (!TryPublishInitialWithHostProjection(candidate, hostProjection, hostPorts))
            {
                diagnostic = "The exact prepared genesis could not bind its host boundary and publish atomically.";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Binds and publishes the Core session and its opaque host projection as
    /// one application-owned transaction. The projection is not consulted for
    /// domain decisions; it exists only for a host such as Unity to expose its
    /// already-created presentation/compatibility view of this exact session.
    /// </summary>
    internal bool TryPublishInitialWithHostProjection(
        DailyV1ActiveRuntimeSession candidate,
        IDailyV1ApplicationHostProjection hostProjection,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts)
    {
        if (candidate == null
            || candidate.AdmissionContext?.IsRestoredContinuation == true
            || hostProjection == null
            || hostPorts == null)
            return false;

        lock (activeSessionGate.SyncRoot)
        {
            if (activeSessionGate.Current != null
                || activeHostProjections.ContainsKey(candidate)
                || !TryBindRuntimeAdvanceBoundary(candidate, hostPorts))
                return false;

            activeHostProjections.Add(candidate, hostProjection);
            if (TryPublishInitial(candidate)) return true;

            activeHostProjections.Remove(candidate);
            return false;
        }
    }

    /// <summary>Returns the projection registered with the exact published Core session.</summary>
    internal bool TryGetPublishedHostProjection(
        DailyV1ActiveRuntimeSession session,
        out IDailyV1ApplicationHostProjection hostProjection)
    {
        hostProjection = null;
        if (session == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            if (!ReferenceEquals(activeSessionGate.Current, session)
                || !activeHostProjections.TryGetValue(session, out hostProjection))
            {
                hostProjection = null;
                return false;
            }
            return true;
        }
    }

    internal bool TryBeginExclusiveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation) =>
        activeSessionGate.TryBeginExclusiveOperation(expectedSession, out operation);

    /// <summary>
    /// Reads the continuation image for the exact published session while its
    /// replacement reservation remains held. The host chooses only its
    /// explicit storage root, file operations, and current-host descriptor;
    /// Core revalidates the source completed boundary immediately before the
    /// read so a stale reservation cannot authorize detached staging.
    /// </summary>
    internal bool TryLoadContinuationImageForReplacement(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        out P12AContinuationImage image,
        out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (expectedSession == null || replacementOperation == null
            || ((unityV1Identity == null) == (dotNetV2Identity == null))
            || string.IsNullOrWhiteSpace(persistentDataRoot) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        DailyV1ExecutionBoundaryState state = expectedSession.ExecutionBoundaryState;
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary = expectedSession.RuntimeAdvanceBoundary;
        if (!replacementOperation.IsOwnedByCurrentThread()
            || !ReferenceEquals(replacementOperation.ExpectedSession, expectedSession)
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !replacementOperation.AuthorizesSessionReplacementCapture(state)
            || activeSessionGate.ActiveOperationCount != 0
            || boundary == null
            || !ReferenceEquals(boundary.ExecutionState, state)
            || !state.IsHealthyDailyOwnerThreadBoundary()
            || !state.HasValidCompletedDailyBoundaryAdmission(
                boundary, sessionReplacement: true))
            return false;

        if (unityV1Identity != null)
            return P12AContinuationStore.TryLoad(
                null, unityV1Identity, persistentDataRoot, fileOperations,
                out image, out result);

        return P12AContinuationStore.TryLoadDotNetV2(
            null, dotNetV2Identity, persistentDataRoot, fileOperations,
            out image, out result);
    }

    /// <summary>
    /// Inspects the initial continuation slot when the host cannot provide a
    /// truthful compatibility identity. Only a missing slot admits genesis;
    /// a present or inaccessible slot keeps this application fail-closed.
    /// </summary>
    internal bool TryInspectInitialContinuationSlotForStart(
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        out bool exists,
        out P12AResult result)
    {
        exists = false;
        result = new P12AResult(P12AResultCode.Busy);
        if (string.IsNullOrWhiteSpace(persistentDataRoot) || fileOperations == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            lock (activeSessionGate.SyncRoot)
            {
                if (!initialContinuationLoadAttempted
                    && !initialGenesisPreparationAttempted
                    && preparedInitialGenesis == null
                    && preparedInitialSession == null
                    && activeSessionGate.Current == null
                    && activeSessionGate.ActiveOperationCount == 0)
                {
                    initialContinuationLoadAttempted = true;
                    initialContinuationLoadBlocksGenesis = true;
                    initialContinuationLoadResult = result;
                }
            }
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            if (initialContinuationLoadAttempted
                || initialGenesisPreparationAttempted
                || preparedInitialGenesis != null
                || preparedInitialSession != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
                return false;

            if (bootstrapAdmissionContext == null)
            {
                bootstrapAdmissionContext = new SimulationRuntimeAdmissionContext(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    Thread.CurrentThread,
                    Thread.CurrentThread.ManagedThreadId);
            }

            if (!bootstrapAdmissionContext.IsOwnedByCurrentThread())
            {
                initialContinuationLoadBlocksGenesis = true;
                result = new P12AResult(P12AResultCode.WrongThread);
                initialContinuationLoadResult = result;
                return false;
            }

            initialContinuationLoadAttempted = true;
            initialContinuationLoadBlocksGenesis = true;
        }

        bool inspected = P12AContinuationStore.TryHasSlot(
            persistentDataRoot, fileOperations, out exists, out result);
        lock (activeSessionGate.SyncRoot)
        {
            initialContinuationLoadBlocksGenesis = !inspected
                || exists
                || result == null
                || result.Code != P12AResultCode.NoSave;
            initialContinuationLoadResult = result;
            initialContinuationImage = null;
        }
        return inspected;
    }
    /// <summary>
    /// Admits the initial continuation slot before genesis. A missing slot is
    /// the only outcome that permits the caller to continue into genesis;
    /// every present-but-invalid, incompatible, or inaccessible slot blocks
    /// genesis for this application instance. The selected owner-thread context
    /// is captured by this Core application and no runtime identity is allocated
    /// by this read.
    /// </summary>
    internal bool TryLoadInitialContinuationImageForStart(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ApplicationHost host,
        out P12AContinuationImage image,
        out P12AResult result)
    {
        image = null;
        result = new P12AResult(P12AResultCode.Busy);
        bool invalidHost = host == null
            || string.IsNullOrWhiteSpace(host.PersistentDataRoot)
            || host.FileOperations == null
            || (host.UnityV1Identity == null && definitions == null);
        if (invalidHost)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            lock (activeSessionGate.SyncRoot)
            {
                // Invalid input is still a failed initial-load attempt. Only a
                // completed NoSave result may let this application fall through
                // to genesis.
                if (!initialContinuationLoadAttempted
                    && !initialGenesisPreparationAttempted
                    && preparedInitialGenesis == null
                    && preparedInitialSession == null
                    && activeSessionGate.Current == null
                    && activeSessionGate.ActiveOperationCount == 0)
                {
                    initialContinuationLoadAttempted = true;
                    initialContinuationLoadBlocksGenesis = true;
                    initialContinuationLoadResult = result;
                }
            }
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            if (initialContinuationLoadAttempted
                || initialGenesisPreparationAttempted
                || preparedInitialGenesis != null
                || preparedInitialSession != null
                || activeSessionGate.Current != null
                || activeSessionGate.ActiveOperationCount != 0)
                return false;

            if (bootstrapAdmissionContext == null)
            {
                bootstrapAdmissionContext = new SimulationRuntimeAdmissionContext(
                    SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                    Thread.CurrentThread,
                    Thread.CurrentThread.ManagedThreadId);
            }

            if (!bootstrapAdmissionContext.IsOwnedByCurrentThread())
            {
                result = new P12AResult(P12AResultCode.WrongThread);
                return false;
            }

            initialContinuationLoadAttempted = true;
            initialContinuationLoadBlocksGenesis = true;
            initialContinuationLoadResult = result;
        }

        bool loaded;
        if (host.UnityV1Identity != null)
        {
            loaded = P12AContinuationStore.TryLoad(
                null, host.UnityV1Identity, host.PersistentDataRoot,
                host.FileOperations, out image, out result);
        }
        else if (!DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
                definitions, host.DotNetIdentity,
                out P12ACompatibilityDescriptorV2 dotNetIdentity, out _))
        {
            result = new P12AResult(P12AResultCode.IncompatibleContentOrConfiguration);
            loaded = false;
        }
        else
        {
            loaded = P12AContinuationStore.TryLoadDotNetV2(
                null, dotNetIdentity, host.PersistentDataRoot,
                host.FileOperations, out image, out result);
        }

        lock (activeSessionGate.SyncRoot)
        {
            // No-save is the only safe fall-through to genesis. A loaded image
            // must be restored, and any storage/compatibility failure must fail
            // closed rather than creating a replacement world.
            initialContinuationLoadBlocksGenesis =
                loaded || result == null || result.Code != P12AResultCode.NoSave;
            initialContinuationLoadResult = result;
            initialContinuationImage = loaded ? image : null;
        }

        if (!loaded && (result == null || result.Code != P12AResultCode.NoSave))
            image = null;
        return loaded;
    }

    /// <summary>
    /// Admits a persisted owner image's host identity and complete Daily-v1
    /// census under the exact bootstrap context owned by this application.
    /// The typed decoder remains a separate Core step for callers that need to
    /// distinguish census admission from value decoding.
    /// </summary>
    internal bool TryAdmitRestoredContinuationImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        SimulationRuntimeAdmissionContext admissionContext,
        out P12ADailyV1Census census,
        out P12AResult result)
    {
        census = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (image == null || ((unityV1Identity == null) == (dotNetV2Identity == null))
            || admissionContext == null)
            return false;

        lock (activeSessionGate.SyncRoot)
        {
            if (!ReferenceEquals(bootstrapAdmissionContext, admissionContext)
                || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || admissionContext.IsRestoredContinuation
                || !admissionContext.IsOwnedByCurrentThread())
            {
                result = new P12AResult(P12AResultCode.WrongThread);
                return false;
            }

            if (image.Header == null || !image.Header.IsValid)
            {
                result = new P12AResult(P12AResultCode.CorruptOrTruncated);
                return false;
            }

            if (unityV1Identity != null)
            {
                if (image.Header.Compatibility == null
                    || !unityV1Identity.Matches(image.Header.Compatibility.CanonicalBytes))
                {
                    result = new P12AResult(P12AResultCode.IncompatibleContentOrConfiguration);
                    return false;
                }
            }
            else if (image.Header.CompatibilityV2 == null
                || P12ACompatibilityAdmission.AdmitDotNetV2(
                    image.Header.CompatibilityV2.CanonicalBytes,
                    dotNetV2Identity, out _) != P12AResultCode.Decoded)
            {
                result = new P12AResult(P12AResultCode.IncompatibleBuildOrRuntime);
                return false;
            }

            if (!P12ADailyV1Census.TryCreateForImage(image, out census, out result))
            {
                census = null;
                return false;
            }
        }

        result = new P12AResult(P12AResultCode.Decoded);
        return true;
    }

    /// <summary>
    /// Admits and decodes one persisted owner image within the Core
    /// application's bootstrap identity. The compatibility adapter may stage
    /// the returned detached values, but cannot bypass Core's host/census gate
    /// or choose a different image decoder.
    /// </summary>
    internal bool TryAdmitAndDecodeRestoredContinuationImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        SimulationRuntimeAdmissionContext admissionContext,
        out bool admissionPassed,
        out P12ADailyV1Census census,
        out P12ADailyV1ImageOwnerSnapshots snapshots,
        out P12AResult result)
    {
        admissionPassed = false;
        census = null;
        snapshots = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (!TryAdmitRestoredContinuationImage(
                image, unityV1Identity, dotNetV2Identity, admissionContext,
                out census, out result))
            return false;

        admissionPassed = true;
        if (!P12ADailyV1ImageOwnerDecoder.TryDecode(image, out snapshots, out result)
            || snapshots == null)
        {
            snapshots = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Owns the first detached restore transaction boundary: the application
    /// verifies its exact initial/replacement staging authority, admits the
    /// persisted image against its captured bootstrap identity, decodes the
    /// typed owner values, and privately stages the Core P12-C roots before
    /// returning any restore result to a host adapter. The returned roots are
    /// unpublished and do not replace or mutate the current session.
    /// </summary>
    internal bool TryAdmitDecodeAndStageRestoredContinuationImage(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        SimulationRuntimeAdmissionContext admissionContext,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out bool admissionPassed,
        out P12ADailyV1Census census,
        out P12ADailyV1ImageOwnerSnapshots snapshots,
        out P12CStagedCoreContinuationRoot stagedRoots,
        out P12AResult result,
        Action imageDecoded = null,
        Action<string> stageCompleted = null)
    {
        admissionPassed = false;
        census = null;
        snapshots = null;
        stagedRoots = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (!TryAdmitAndDecodeRestoredContinuationImage(
                image,
                unityV1Identity,
                dotNetV2Identity,
                admissionContext,
                out admissionPassed,
                out census,
                out snapshots,
                out result))
            return false;

        imageDecoded?.Invoke();

        if (!TryStageRestoredContinuationRootsFromSnapshots(
                snapshots.C,
                expectedSession,
                replacementOperation,
                out stagedRoots,
                out result,
                stageCompleted)
            || stagedRoots == null)
        {
            stagedRoots = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Keeps the restored image-to-target owner-vector decision inside the
    /// application that owns the exact initial or replacement staging
    /// reservation. The Unity runtime supplies only its captured census.
    /// </summary>
    internal bool TryValidateRestoredImageOwnerVector(
        P12ADailyV1Census imageCensus,
        IReadOnlyList<OwnerSectionCensusSnapshot> targetOwnerSections,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic)
    {
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
        {
            failure = P12GDailyV1OwnerVectorFailure.OwnerVectorChanged;
            diagnostic = "The application no longer holds the exact restored-continuation staging authority.";
            return false;
        }

        return DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            imageCensus, targetOwnerSections, out failure, out diagnostic);
    }

    /// <summary>
    /// Runs the restored image-to-target owner-vector gate as one application
    /// operation. The host contributes only its live census port; this
    /// application verifies the exact staging reservation and owns the
    /// comparison against the persisted image before target composition can
    /// proceed to restored-boundary finalization.
    /// </summary>
    internal bool TryCaptureAndValidateRestoredImageOwnerVector(
        DailyV1ActiveRuntimeSession candidateSession,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        P12ADailyV1Census imageCensus,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        Action<DailyV1RestoredImageOwnerVectorStage> stageObserver,
        out IReadOnlyList<OwnerSectionCensusSnapshot> targetOwnerSections,
        out long targetMutationEpoch,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic)
    {
        targetOwnerSections = null;
        targetMutationEpoch = 0L;
        failure = P12GDailyV1OwnerVectorFailure.None;
        diagnostic = null;

        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
        {
            failure = P12GDailyV1OwnerVectorFailure.OwnerVectorChanged;
            diagnostic = "The application no longer holds the exact restored-continuation staging authority.";
            return false;
        }

        if (candidateSession == null || hostPorts == null
            || candidateSession.ExecutionBoundaryState == null
            || !ReferenceEquals(candidateSession.ExecutionBoundaryState, hostPorts.ExecutionState))
        {
            failure = P12GDailyV1OwnerVectorFailure.OwnerVectorChanged;
            diagnostic = "The private restored candidate does not share its exact Core execution state with the host census port.";
            return false;
        }

        if (!hostPorts.TryCaptureUnadmittedRestoredDailyOwnerVector(
                out targetOwnerSections, out targetMutationEpoch, out diagnostic)
            || targetOwnerSections == null)
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target did not produce a complete quiescent owner vector.";
            return false;
        }

        if (!hostPorts.HasExactCoreOwnedSelectedCensusSources)
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            diagnostic = "The restored image requires exact Core-owned selected census sources before composition.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                candidateSession.OwnerGraph,
                candidateSession.ExecutionBoundaryState.SelectedActorChoiceStore,
                targetOwnerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeTargetSections,
                out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target host remainder could not be completed from the exact Core-owned P12-F graph.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRemoveCoreOwnedSelectedRuntimeSections(
                completeTargetSections,
                candidateSession.OwnerGraph,
                out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections,
                out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The restored image host vector could not be reduced to its exact host-owned remainder.";
            return false;
        }
        targetOwnerSections = hostOwnedSections;

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryValidateExcludedNpcReceiptSections(
                candidateSession.OwnerGraph, targetOwnerSections, out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target could not validate the exact-empty P12-D NPC receipt owners.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedPersonStoreSections(
                candidateSession.OwnerGraph,
                targetOwnerSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> personStoreSections,
                out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target could not compose its Core-owned PersonStore census.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedIdentitySpatialSections(
                candidateSession.OwnerGraph,
                personStoreSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> identitySpatialSections,
                out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target could not compose its Core-owned identity and spatial census.";
            return false;
        }

        // Restore the exact Core identity, spatial and City witnesses before
        // the structural P12-F completeness gate, just as final recapture does.
        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryAddCoreOwnedCitySections(
                candidateSession.OwnerGraph,
                identitySpatialSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> citySections,
                out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target could not compose its Core-owned City census.";
            return false;
        }

        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcKnowledgeAndPlanSections(
                candidateSession.OwnerGraph,
                citySections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> npcKnowledgeAndPlanSections,
                out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target could not bind its exact NPC Knowledge and Plan census owners.";
            return false;
        }
        if (!DailyV1SelectedRuntimeOwnerCensusSource.TryComposeCoreOwnedP12FSections(
                candidateSession.OwnerGraph,
                candidateSession.ExecutionBoundaryState.SelectedActorChoiceStore,
                npcKnowledgeAndPlanSections,
                out IReadOnlyList<OwnerSectionCensusSnapshot> completeTargetOwnerSections,
                out diagnostic))
        {
            targetOwnerSections = null;
            targetMutationEpoch = 0L;
            failure = P12GDailyV1OwnerVectorFailure.MissingTargetVector;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The private target could not compose its complete Core-owned P12-F census.";
            return false;
        }
        targetOwnerSections = completeTargetOwnerSections;

        stageObserver?.Invoke(DailyV1RestoredImageOwnerVectorStage.Captured);
        if (!TryValidateRestoredImageOwnerVector(
                imageCensus, targetOwnerSections, expectedSession, replacementOperation,
                out failure, out diagnostic))
            return false;

        return true;
    }

    /// <summary>
    /// Finalizes the private restored Daily-v1 boundary while this application
    /// owns the exact initial or replacement staging authority. Core binds and
    /// revalidates the selected execution boundary, publishes the restored
    /// continuation token, and marks factual-read state before the host can
    /// publish its compatibility projection.
    /// </summary>
    internal bool TryFinalizeRestoredCandidateBoundary(
        DailyV1ActiveRuntimeSession candidateSession,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        WorldId expectedWorldId,
        long preservedAbsoluteDay,
        long preservedCompletedCoreSequence,
        IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetSections,
        long validatedMutationEpoch,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        Action<DailyV1RestoredBoundaryFinalizationStage> stageObserver,
        out IReadOnlyList<OwnerSectionCensusSnapshot> recapturedOwnerSections,
        out long recapturedMutationEpoch,
        out DailyV1RestoredBoundaryFinalizationFailure failure,
        out string diagnostic)
    {
        recapturedOwnerSections = null;
        recapturedMutationEpoch = 0L;
        failure = DailyV1RestoredBoundaryFinalizationFailure.None;
        diagnostic = null;

        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.StagingAuthorityUnavailable;
            diagnostic = "The application no longer holds the exact restored-continuation staging authority.";
            return false;
        }

        if (candidateSession == null || hostPorts == null
            || !ReferenceEquals(candidateSession.ExecutionBoundaryState, hostPorts.ExecutionState)
            || !IsBootstrapOrRestoredDescendant(candidateSession.AdmissionContext)
            || !TryBindRuntimeAdvanceBoundary(candidateSession, hostPorts))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.CandidateHostBoundaryUnavailable;
            diagnostic = "The private restored candidate could not bind its exact Core application and host boundary.";
            return false;
        }

        if (candidateSession.RuntimeAdvanceBoundary == null
            || !ReferenceEquals(candidateSession.RuntimeAdvanceBoundary.ExecutionState,
                candidateSession.ExecutionBoundaryState))
        {
            failure = DailyV1RestoredBoundaryFinalizationFailure.CandidateHostBoundaryUnavailable;
            diagnostic = "The private restored candidate has no exact Core runtime evidence boundary.";
            return false;
        }

        return candidateSession.ExecutionBoundaryState.TryFinalizeRestoredDailyBoundary(
            candidateSession.RuntimeAdvanceBoundary,
            hostPorts,
            expectedWorldId,
            preservedAbsoluteDay,
            preservedCompletedCoreSequence,
            validatedTargetSections,
            validatedMutationEpoch,
            stageObserver,
            out recapturedOwnerSections,
            out recapturedMutationEpoch,
            out failure,
            out diagnostic);
    }

    /// <summary>
    /// Binds the exact already-validated initial image target vector to its
    /// private Core session. The application consumes this proof later while
    /// finalizing the restored boundary and publishing the Core session plus
    /// host projection under one gate.
    /// </summary>
    internal bool TryBindInitialRestoredImageCommitProof(
        DailyV1ApplicationStartAdmission initialStartAdmission,
        DailyV1RestoredImageCoreOwnerStages stages,
        DailyV1ActiveRuntimeSession candidateSession,
        IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetSections,
        long validatedMutationEpoch,
        Action<DailyV1RestoredBoundaryFinalizationStage> stageObserver,
        out string diagnostic)
    {
        diagnostic = null;
        lock (activeSessionGate.SyncRoot)
        {
            if (stages == null
                || !IsForInitialRestoreAdmission(initialStartAdmission, stages.Image)
                || !stages.IsFor(this, stages.Image, stages.Definitions, stages.CoreRoots,
                    candidateExpectedSession: null, candidateReplacementOperation: null)
                || !stages.IsComplete
                || !ReferenceEquals(stages.CoreSession, candidateSession)
                || candidateSession == null
                || !HasRestoredContinuationStagingAuthority(null, null)
                || !string.Equals(stages.CoreRoots.WorldIdentity?.Value,
                    stages.Image.Header?.WorldId, StringComparison.Ordinal)
                || stages.Image.Header?.Day != candidateSession.SimulationTime?.AbsoluteDay)
            {
                diagnostic = "Initial restore proof requires the exact admitted image and its complete private Core C/D/E/F session.";
                return false;
            }

            if (!TryValidateRestoredImageOwnerVector(
                    stages.Census, validatedTargetSections, null, null,
                    out P12GDailyV1OwnerVectorFailure vectorFailure,
                    out string vectorDiagnostic))
            {
                diagnostic = "The initial restored Core session has no valid image-to-target owner-vector proof: "
                    + (string.IsNullOrWhiteSpace(vectorDiagnostic)
                        ? vectorFailure.ToString()
                        : vectorDiagnostic);
                return false;
            }

            DailyV1InitialRestoredImageCommitProof proof;
            try
            {
                proof = new DailyV1InitialRestoredImageCommitProof(
                    stages.Image,
                    stages.CoreRoots.WorldIdentity,
                    stages.Image.Header.Day,
                    stages.Image.Header.Sequence,
                    validatedTargetSections,
                    validatedMutationEpoch,
                    stageObserver);
            }
            catch (ArgumentException)
            {
                diagnostic = "The initial restored image target-vector proof could not be retained.";
                return false;
            }

            if (!candidateSession.TryBindInitialRestoreCommitProof(proof))
            {
                diagnostic = "The exact initial image proof could not be bound once to its private restored Core session.";
                return false;
            }

            return true;
        }
    }
    /// <summary>
    /// Stages the detached P12-C roots for an image through this application.
    /// Replacement staging is bound to the exact published session and its
    /// owner-thread reservation; initial restoration is accepted only by an
    /// unused application on its captured bootstrap thread. The host receives
    /// only the staged Core root and may create a Unity compatibility
    /// projection, but does not choose or reconstruct the authoritative roots.
    /// </summary>
    internal bool TryStageRestoredContinuationRoots(
        P12AContinuationImage image,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12CStagedCoreContinuationRoot stagedRoots,
        out P12AResult result,
        Action<string> stageCompleted = null)
    {
        stagedRoots = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (image == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!P12ACausalRootImageCodec.TryDecode(
                image, out P12ACausalRootImageSnapshots snapshots, out result)
            || snapshots == null)
            return false;

        return TryStageRestoredContinuationRootsFromSnapshots(
            snapshots,
            expectedSession,
            replacementOperation,
            out stagedRoots,
            out result,
            stageCompleted);
    }

    /// <summary>
    /// Stages the Core P12-C roots from the immutable C values already
    /// decoded as part of the complete typed image. This avoids re-decoding
    /// image sections and keeps staging under the exact application
    /// reservation.
    /// </summary>
    internal bool TryStageRestoredContinuationRootsFromSnapshots(
        P12ACausalRootImageSnapshots snapshots,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12CStagedCoreContinuationRoot stagedRoots,
        out P12AResult result,
        Action<string> stageCompleted = null)
    {
        stagedRoots = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (snapshots == null || snapshots.WorldIdentity == null
            || snapshots.Allocator == null || snapshots.RecordSequence == null
            || snapshots.SpatialAuthority == null || snapshots.P9Manifest == null
            || snapshots.RandomRoot == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!P12CSelectedCoreRootStager.TryStage(
                snapshots.WorldIdentity,
                snapshots.Allocator,
                snapshots.RecordSequence,
                snapshots.SpatialAuthority,
                snapshots.P9Manifest,
                snapshots.RandomRoot,
                isDetached: true,
                stageObserver: stageCompleted,
                out stagedRoots,
                out _)
            || stagedRoots == null
            || !stagedRoots.IsDetached)
        {
            stagedRoots = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12c.continuation-roots");
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>
    /// Stages the selected Core-owned P12-D City/NPC owner vectors under the
    /// same bootstrap or exact-session replacement authority as the P12-C
    /// roots. Unity supplies detached value rows and the already staged legacy
    /// spatial owner; Core creates and validates the authoritative selected
    /// City/NPC owners before the host builds compatibility projections.
    /// </summary>
    internal bool TryStageRestoredP12DOwners(
        P12CStagedCoreContinuationRoot stagedRoots,
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<DailyV1P12DNpcCoreOwnerHydrationInput> npcInputs,
        SpatialNetworkOwner spatialNetwork,
        IEnumerable<string> travelPartyIds,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12DSelectedCoreOwnerStage stagedOwners,
        out P12AResult result)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached || definitions == null
            || cityInputs == null || npcInputs == null || spatialNetwork == null
            || travelPartyIds == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12DSelectedCoreOwnerStager.TryCreate(
                definitions, cityInputs, npcInputs, spatialNetwork, travelPartyIds,
                out stagedOwners, out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12d.selected-core-owners");
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>
    /// Stages selected P12-D owners directly from detached image rows under the
    /// same bootstrap or exact-session replacement authority as the P12-C roots.
    /// Core owns D/F row pairing, hydration-input conversion, definition binding,
    /// and selected City/NPC owner creation before host projections are built.
    /// </summary>
    internal bool TryStageRestoredP12DOwnersFromImageRows(
        P12CStagedCoreContinuationRoot stagedRoots,
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cityInputs,
        IReadOnlyList<P12DNpcDRow> npcDRows,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        SpatialNetworkOwner spatialNetwork,
        IEnumerable<string> travelPartyIds,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12DSelectedCoreOwnerStage stagedOwners,
        out P12AResult result)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached || definitions == null
            || cityInputs == null || npcDRows == null || npcFRows == null
            || spatialNetwork == null || travelPartyIds == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12DSelectedCoreOwnerStager.TryCreateFromRows(
                definitions, cityInputs, npcDRows, npcFRows, spatialNetwork, travelPartyIds,
                out stagedOwners, out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12d.selected-core-owners");
            return false;
        }

        result = null;
        return true;
    }
    /// <summary>
    /// Stages the selected P12-D spatial and City/NPC Core owner graph from
    /// values already decoded from one image. The application validates its
    /// exact initial or replacement reservation before creating any owners;
    /// Unity receives only the unpublished Core owners for projection.
    /// </summary>
    internal bool TryStageRestoredP12DCoreOwnerGraphFromImageSnapshots(
        P12CStagedCoreContinuationRoot stagedRoots,
        P12ADailyV1ImageOwnerSnapshots snapshots,
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12DCoreRestoreOwnerStage stagedOwners,
        out P12AResult result)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached || snapshots == null
            || definitions == null || snapshots.SpatialNetwork == null
            || snapshots.RuntimeIdentities == null || snapshots.Persons == null
            || snapshots.Genealogy == null || snapshots.Sites == null || snapshots.Cities == null
            || snapshots.NpcDRows == null || snapshots.NpcFRows == null
            || snapshots.TravelParties == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12DSelectedCoreOwnerStager.TryCreateFromImageValues(
                definitions,
                snapshots.SpatialNetwork,
                snapshots.RuntimeIdentities,
                snapshots.Persons,
                snapshots.Genealogy,
                snapshots.Sites,
                snapshots.Cities,
                snapshots.NpcDRows,
                snapshots.NpcFRows,
                snapshots.TravelParties,
                out stagedOwners,
                out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12d.selected-core-owners");
            return false;
        }

        result = null;
        return true;
    }
    /// <summary>
    /// Stages the selected Core-owned P12-E institution, property, political,
    /// justice, and military owners under the same bootstrap or exact-session
    /// replacement authority as the P12-C/D roots. Unity supplies only the
    /// already decoded immutable owner snapshots and staged P12-C/D roots;
    /// Core creates the authoritative P12-E owners.
    /// </summary>
    internal bool TryStageRestoredP12EOwners(
        P12CStagedCoreContinuationRoot stagedRoots,
        P12EInstitutionOfficeOwnerSnapshot institutionOffice,
        PropertyEstateOwnerSnapshot propertyEstate,
        P12EFactionOwnerSnapshot faction,
        P12EPoliticalClaimOwnerSnapshot politicalClaims,
        P12EPoliticalSupportOwnerSnapshot politicalSupport,
        P12EPoliticalDecisionOwnerSnapshot politicalDecisions,
        P12EJusticeRecordsOwnerSnapshot justiceRecords,
        P12EMilitaryOwnerSnapshot military,
        PersistentConflictOwnerSnapshot conflicts,
        PersistentWarOwnerSnapshot wars,
        PersistentBattleOwnerSnapshot battles,
        bool localTopologyComposed,
        PersonStore persons,
        IReadOnlyList<DailyV1CityRuntimeOwners> stagedCities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedNpcs,
        long capturedDay,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out P12ECoreOwnerRestoreStage stagedOwners,
        out P12AResult result,
        Action<P12ECoreOwnerRestoreStep> stageObserver = null)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached
            || persons == null || stagedCities == null || stagedNpcs == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!DailyV1P12ECoreOwnerRestoreStager.TryStage(
                institutionOffice, propertyEstate, faction, politicalClaims,
                politicalSupport, politicalDecisions, justiceRecords, military, conflicts, wars,
                battles, localTopologyComposed,
                persons, stagedRoots.SpatialAuthority, stagedCities, stagedNpcs,
                capturedDay, stageObserver,
                out stagedOwners, out _)
            || stagedOwners == null)
        {
            stagedOwners = null;
            result = new P12AResult(
                P12AResultCode.OwnerGraphRejected,
                "p12e.core-owners");
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>
    /// Stages the selected Core-owned P12-F knowledge, directive, actor-choice,
    /// travel-party, and expedition owners under the same bootstrap or exact
    /// session-replacement authority as the detached P12-C/D/E graph.
    /// </summary>
    internal bool TryStageRestoredP12FOwners(
        P12CStagedCoreContinuationRoot stagedRoots,
        P12FPoliticalKnowledgeOwnerSnapshot politicalKnowledge,
        P12FScheduledDirectiveOwnerSnapshot directives,
        P12FActorChoiceSnapshot actorChoices,
        P12FTravelPartyOwnerSnapshot travelParties,
        P12FExpeditionOwnerSnapshot expeditions,
        PersonStore persons,
        InstitutionStore institutions,
        PoliticalClaimStore politicalClaims,
        FactionStore factions,
        OfficeStore offices,
        PropertyOwnershipStore properties,
        long capturedDay,
        SimulationTime stagedTime,
        DailyV1EffectiveDefinitions selectedDefinitions,
        IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedSelectedNpcOwners,
        DailyV1RuntimeIdentityIndex stagedIdentities,
        SpatialNetworkOwner stagedSpatialNetwork,
        ExplorableSiteStore stagedExplorableSites,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyV1P12FCoreOwnerRestoreStage stagedOwners,
        out P12AResult result,
        Action<DailyV1P12FCoreOwnerRestoreStep> stageObserver = null)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            return false;

        if (stagedRoots == null || !stagedRoots.IsDetached
            || politicalKnowledge == null || directives == null || actorChoices == null
            || travelParties == null || expeditions == null || persons == null
            || institutions == null || politicalClaims == null || factions == null
            || offices == null || properties == null || stagedTime == null
            || capturedDay != stagedTime.AbsoluteDay || capturedDay < 0L
            || selectedDefinitions == null || detachedNpcRows == null
            || stagedSelectedNpcOwners == null || stagedIdentities == null
            || stagedSpatialNetwork == null || stagedExplorableSites == null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        try
        {
            List<string> stagedNpcRuntimeIds = new List<string>(detachedNpcRows.Count);
            HashSet<string> seenNpcRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12DNpcFRow row in detachedNpcRows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.RuntimeId)
                    || !seenNpcRuntimeIds.Add(row.RuntimeId))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue);
                    return false;
                }
                stagedNpcRuntimeIds.Add(row.RuntimeId);
            }

            if (!politicalKnowledge.TryStageForImage(persons, institutions, politicalClaims,
                    factions, offices, properties, capturedDay,
                    out PoliticalKnowledgeStore stagedPoliticalKnowledge, out _)
                || stagedPoliticalKnowledge == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.political-knowledge");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.PoliticalKnowledge);

            if (!directives.TryStageForImage(stagedTime, selectedDefinitions,
                    out ScheduledDirectiveStore stagedDirectives, out _)
                || stagedDirectives == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.scheduled-directives");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.ScheduledDirectives);

            if (!P12FActorChoiceSnapshot.TryStage(actorChoices, persons,
                    out ActorChoiceStore stagedActorChoices, out _)
                || stagedActorChoices == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.actor-choices");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.ActorChoices);

            if (!DailyV1P12FTravelPartyOwnerStager.TryStage(travelParties,
                    stagedSelectedNpcOwners, detachedNpcRows, stagedSpatialNetwork,
                    out TravelPartyStore stagedTravelParties, out _)
                || stagedTravelParties == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.travel-parties");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.TravelParties);

            if (!P12FExpeditionOwnerSnapshotStager.TryStage(expeditions,
                    stagedNpcRuntimeIds, detachedNpcRows, stagedSelectedNpcOwners,
                    stagedIdentities, stagedSpatialNetwork, stagedExplorableSites,
                    stagedTravelParties, out ExpeditionStore stagedExpeditions, out _)
                || stagedExpeditions == null)
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12f.expeditions");
                return false;
            }
            stageObserver?.Invoke(DailyV1P12FCoreOwnerRestoreStep.Expeditions);

            stagedOwners = new DailyV1P12FCoreOwnerRestoreStage(
                stagedPoliticalKnowledge, stagedDirectives, stagedActorChoices,
                stagedTravelParties, stagedExpeditions);
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is NullReferenceException)
        {
            stagedOwners = null;
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12f.core-owners");
            return false;
        }
    }

    /// <summary>
    /// Starts one application-owned image restore by admitting, decoding, and
    /// privately staging P12-C. D, E, and F remain explicit ordered steps so
    /// the host can project each exact Core stage at the established boundary.
    /// </summary>
    internal bool TryBeginRestoredImageCoreOwnerStages(
        P12AContinuationImage image,
        P12ACompatibilityDescriptor unityV1Identity,
        P12ACompatibilityDescriptorV2 dotNetV2Identity,
        DailyV1EffectiveDefinitions selectedDefinitions,
        SimulationRuntimeAdmissionContext bootstrapAdmissionContext,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out bool admissionPassed,
        out P12ADailyV1Census census,
        out P12ADailyV1ImageOwnerSnapshots snapshots,
        out P12CStagedCoreContinuationRoot coreRoots,
        out DailyV1RestoredImageCoreOwnerStages stagedOwnerStages,
        out P12AResult result,
        Action imageDecoded = null,
        Action<string> stageCompleted = null,
        DailyV1ApplicationStartAdmission initialStartAdmission = null)
    {
        admissionPassed = false;
        census = null;
        snapshots = null;
        coreRoots = null;
        stagedOwnerStages = null;
        result = new P12AResult(P12AResultCode.Busy);
        bool isInitialApplicationRestore = expectedSession == null && replacementOperation == null;
        if (isInitialApplicationRestore
            ? initialStartAdmission == null || !IsForInitialRestoreAdmission(initialStartAdmission, image)
            : initialStartAdmission != null)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p12.initial-start-admission");
            return false;
        }

        if (image == null || selectedDefinitions == null
            || bootstrapAdmissionContext == null
            || !HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        try
        {
            if (!TryAdmitDecodeAndStageRestoredContinuationImage(
                    image, unityV1Identity, dotNetV2Identity, bootstrapAdmissionContext,
                    expectedSession, replacementOperation,
                    out admissionPassed, out census, out snapshots, out coreRoots,
                    out result, imageDecoded, stageCompleted)
                || !admissionPassed || census == null || snapshots == null || coreRoots == null)
                return false;

            stagedOwnerStages = new DailyV1RestoredImageCoreOwnerStages(
                this, image, selectedDefinitions, expectedSession, replacementOperation,
                census, snapshots, coreRoots, new SimulationTime(image.Header.Day));
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is NullReferenceException)
        {
            stagedOwnerStages = null;
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12.core.restored-image-roots");
            return false;
        }
    }

    /// <summary>Stages P12-D Core owners after the admitted P12-C root was projected.</summary>
    internal bool TryStageRestoredImageP12DOwners(
        DailyV1RestoredImageCoreOwnerStages stages,
        out DailyV1P12DCoreRestoreOwnerStage stagedOwners,
        out P12AResult result)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (stages == null
            || !stages.IsFor(this, stages.Image, stages.Definitions, stages.CoreRoots,
                stages.ExpectedSession, stages.ReplacementOperation)
            || !stages.CanStageD
            || !HasRestoredContinuationStagingAuthority(stages.ExpectedSession, stages.ReplacementOperation))
        {
            stages?.Fail();
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        if (!TryStageRestoredP12DCoreOwnerGraphFromImageSnapshots(
                stages.CoreRoots, stages.Snapshots, stages.Definitions,
                stages.ExpectedSession, stages.ReplacementOperation,
                out stagedOwners, out result)
            || stagedOwners == null || !stages.TryAcceptD(stagedOwners))
        {
            stages.Fail();
            stagedOwners = null;
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>
    /// Creates the restored image's exact decision/event/history roots after
    /// P12-D and before P12-E. The host supplies only its already-bound logger;
    /// the Core application owns the stores and identity-bound record authority.
    /// </summary>
    internal bool TryPrepareRestoredImageCoreRuntimeAuthorities(
        DailyV1RestoredImageCoreOwnerStages stages,
        SimulationLogger diagnosticLogger,
        out NpcDecisionStore decisionStore,
        out DailyV1DomainEventAuthority eventAuthority,
        out P12AResult result)
    {
        decisionStore = null;
        eventAuthority = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (stages == null
            || !stages.IsFor(this, stages.Image, stages.Definitions, stages.CoreRoots,
                stages.ExpectedSession, stages.ReplacementOperation)
            || !stages.CanPrepareRuntimeAuthorities
            || !HasRestoredContinuationStagingAuthority(stages.ExpectedSession, stages.ReplacementOperation)
            || diagnosticLogger == null)
        {
            stages?.Fail();
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        try
        {
            NpcDecisionStore candidateDecisionStore = new NpcDecisionStore(diagnosticLogger);
            DailyV1DomainEventAuthority candidateEventAuthority = DailyV1DomainEventAuthority.Create(
                stages.CoreRoots.RuntimeIdAllocator,
                stages.StagedTime,
                stages.CoreRoots.RecordSequence,
                candidateDecisionStore,
                diagnosticLogger);
            if (!stages.TryAcceptRuntimeAuthorities(candidateDecisionStore, candidateEventAuthority))
            {
                stages.Fail();
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12.core.restored-image-record-authority");
                return false;
            }

            decisionStore = candidateDecisionStore;
            eventAuthority = candidateEventAuthority;
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is NullReferenceException)
        {
            stages.Fail();
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12.core.restored-image-record-authority");
            return false;
        }
    }

    /// <summary>
    /// Composes and retains the restored Core session from one exact ordered
    /// image-stage token. The application derives the continuation identity
    /// itself; the Unity restore coordinator receives the resulting session
    /// only as a projection input.
    /// </summary>
    internal bool TryComposeRestoredImageCoreSession(
        DailyV1RestoredImageCoreOwnerStages stages,
        long lastEconomySnapshotDay,
        out DailyV1ActiveRuntimeSession candidate,
        out SimulationCalendar calendar,
        out string diagnostic)
    {
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (stages == null
            || !stages.IsFor(this, stages.Image, stages.Definitions, stages.CoreRoots,
                stages.ExpectedSession, stages.ReplacementOperation)
            || !stages.CanComposeCoreSession
            || !HasRestoredContinuationStagingAuthority(stages.ExpectedSession, stages.ReplacementOperation))
        {
            stages?.Fail();
            diagnostic = "The Core application no longer owns the exact complete restored-image staging operation.";
            return false;
        }

        if (!TryCreateRestoredContinuationAdmissionContext(
                stages.ExpectedSession, stages.ReplacementOperation,
                out SimulationRuntimeAdmissionContext restoredAdmissionContext,
                out diagnostic)
            || restoredAdmissionContext == null)
        {
            stages.Fail();
            return false;
        }

        if (!TryCreateRestoredActiveRuntimeSessionFromOwnerStages(
                stages.Definitions,
                stages.Definitions.EffectiveConfiguration,
                stages.CoreRoots,
                stages.D,
                stages.E,
                stages.F,
                stages.StagedTime,
                restoredAdmissionContext,
                stages.DecisionStore,
                stages.EventAuthority,
                lastEconomySnapshotDay,
                stages.ReplacementOperation,
                out DailyV1ActiveRuntimeSession candidateSession,
                out SimulationCalendar candidateCalendar,
                out diagnostic))
        {
            stages.Fail();
            return false;
        }

        if (candidateSession == null
            || !stages.TryAcceptCoreSession(
                candidateSession, candidateCalendar, restoredAdmissionContext, out diagnostic))
        {
            stages.Fail();
            candidate = null;
            calendar = null;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The Core application could not bind the composed session to its exact restored-image stages.";
            return false;
        }

        candidate = candidateSession;
        calendar = candidateCalendar;
        return true;
    }

    /// <summary>
    /// Owns the ordered private image restore stages after P12-C: Core P12-D,
    /// runtime record authorities, Core P12-E, Core P12-F, then the Core session.
    /// The host may project each completed stage before Core advances to the next;
    /// it receives only the stage identity and cannot replace Core owner values.
    /// The resulting session remains private and unfinalized for the later commit gate.
    /// </summary>
    internal bool TryStageRestoredImageCoreOwnerStagesAndComposeSession(
        DailyV1RestoredImageCoreOwnerStages stages,
        SimulationLogger diagnosticLogger,
        long lastEconomySnapshotDay,
        Func<DailyV1RestoredImageCoreOwnerStageBoundary, bool> projectCompletedStage,
        Action<P12ECoreOwnerRestoreStep> p12EOwnerStageObserver,
        Action<DailyV1P12FCoreOwnerRestoreStep> p12FOwnerStageObserver,
        out DailyV1RestoredImageCoreOwnerStageBoundary failedBoundary,
        out P12AResult result,
        out string diagnostic)
    {
        failedBoundary = DailyV1RestoredImageCoreOwnerStageBoundary.None;
        result = new P12AResult(P12AResultCode.Busy);
        diagnostic = null;
        if (stages == null
            || !stages.IsFor(this, stages.Image, stages.Definitions, stages.CoreRoots,
                stages.ExpectedSession, stages.ReplacementOperation)
            || !HasRestoredContinuationStagingAuthority(stages.ExpectedSession, stages.ReplacementOperation)
            || diagnosticLogger == null)
        {
            stages?.Fail();
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            diagnostic = "The Core application no longer owns the exact private restored-image operation.";
            return false;
        }

        try
        {
            failedBoundary = DailyV1RestoredImageCoreOwnerStageBoundary.P12DOwners;
            if (!TryStageRestoredImageP12DOwners(stages, out _, out result))
            {
                diagnostic = "Core could not stage the P12-D image owners.";
                stages.Fail();
                return false;
            }
            if (projectCompletedStage != null
                && !projectCompletedStage(DailyV1RestoredImageCoreOwnerStageBoundary.P12DOwners))
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12.core.restored-image-p12d-projection");
                diagnostic = "The host rejected the completed private P12-D projection.";
                stages.Fail();
                return false;
            }

            failedBoundary = DailyV1RestoredImageCoreOwnerStageBoundary.RuntimeAuthorities;
            if (!TryPrepareRestoredImageCoreRuntimeAuthorities(
                    stages, diagnosticLogger, out _, out _, out result))
            {
                diagnostic = "Core could not prepare the restored decision and event authorities.";
                stages.Fail();
                return false;
            }

            failedBoundary = DailyV1RestoredImageCoreOwnerStageBoundary.P12EOwners;
            if (!TryStageRestoredImageP12EOwners(
                    stages, out _, out result, p12EOwnerStageObserver))
            {
                diagnostic = "Core could not stage the P12-E image owners.";
                stages.Fail();
                return false;
            }
            if (projectCompletedStage != null
                && !projectCompletedStage(DailyV1RestoredImageCoreOwnerStageBoundary.P12EOwners))
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12.core.restored-image-p12e-projection");
                diagnostic = "The host rejected the completed private P12-E projection.";
                stages.Fail();
                return false;
            }

            failedBoundary = DailyV1RestoredImageCoreOwnerStageBoundary.P12FOwners;
            if (!TryStageRestoredImageP12FOwners(
                    stages, stages.D.EmptyExplorableSites, out _, out result,
                    p12FOwnerStageObserver))
            {
                diagnostic = "Core could not stage the P12-F image owners.";
                stages.Fail();
                return false;
            }
            if (projectCompletedStage != null
                && !projectCompletedStage(DailyV1RestoredImageCoreOwnerStageBoundary.P12FOwners))
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12.core.restored-image-p12f-projection");
                diagnostic = "The host rejected the completed private P12-F projection.";
                stages.Fail();
                return false;
            }

            failedBoundary = DailyV1RestoredImageCoreOwnerStageBoundary.CoreSessionComposition;
            if (!TryComposeRestoredImageCoreSession(
                    stages, lastEconomySnapshotDay, out _, out _, out diagnostic))
            {
                result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                    "p12.core.restored-image-session");
                if (string.IsNullOrWhiteSpace(diagnostic))
                    diagnostic = "Core could not compose the private restored-image session.";
                stages.Fail();
                return false;
            }

            failedBoundary = DailyV1RestoredImageCoreOwnerStageBoundary.None;
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException
            || exception is NullReferenceException)
        {
            stages.Fail();
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12.core.restored-image-owner-stages");
            diagnostic = "The private Core restored-image owner sequence failed: " + exception.Message;
            return false;
        }
    }

    /// <summary>Stages P12-E Core owners from the exact P12-C/D portion of the operation.</summary>
    internal bool TryStageRestoredImageP12EOwners(
        DailyV1RestoredImageCoreOwnerStages stages,
        out P12ECoreOwnerRestoreStage stagedOwners,
        out P12AResult result,
        Action<P12ECoreOwnerRestoreStep> stageObserver = null)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (stages == null
            || !stages.IsFor(this, stages.Image, stages.Definitions, stages.CoreRoots,
                stages.ExpectedSession, stages.ReplacementOperation)
            || !stages.CanStageE
            || !HasRestoredContinuationStagingAuthority(stages.ExpectedSession, stages.ReplacementOperation))
        {
            stages?.Fail();
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        P12ADailyV1ImageOwnerSnapshots snapshots = stages.Snapshots;
        DailyV1P12DCoreRestoreOwnerStage d = stages.D;
        if (!TryStageRestoredP12EOwners(
                stages.CoreRoots, snapshots.InstitutionOffice, snapshots.PropertyEstate,
                snapshots.Faction, snapshots.PoliticalClaims, snapshots.PoliticalSupport,
                snapshots.PoliticalDecisions, snapshots.Justice, snapshots.Military,
                snapshots.Conflicts, snapshots.Wars, snapshots.Battles,
                false, d.Persons, d.Cities, d.Npcs, stages.Image.Header.Day,
                stages.ExpectedSession, stages.ReplacementOperation,
                out stagedOwners, out result, stageObserver)
            || stagedOwners == null || !stages.TryAcceptE(stagedOwners))
        {
            stages.Fail();
            stagedOwners = null;
            return false;
        }

        result = null;
        return true;
    }

    /// <summary>Stages P12-F Core owners against the exact D empty-site store and E owners.</summary>
    internal bool TryStageRestoredImageP12FOwners(
        DailyV1RestoredImageCoreOwnerStages stages,
        ExplorableSiteStore stagedEmptyExplorableSites,
        out DailyV1P12FCoreOwnerRestoreStage stagedOwners,
        out P12AResult result,
        Action<DailyV1P12FCoreOwnerRestoreStep> stageObserver = null)
    {
        stagedOwners = null;
        result = new P12AResult(P12AResultCode.Busy);
        if (stages == null
            || !stages.IsFor(this, stages.Image, stages.Definitions, stages.CoreRoots,
                stages.ExpectedSession, stages.ReplacementOperation)
            || !stages.CanStageF
            || !HasRestoredContinuationStagingAuthority(stages.ExpectedSession, stages.ReplacementOperation)
            || stagedEmptyExplorableSites == null
            || stagedEmptyExplorableSites.Count != 0 || stagedEmptyExplorableSites.Revision != 0L)
        {
            stages?.Fail();
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        P12ADailyV1ImageOwnerSnapshots snapshots = stages.Snapshots;
        DailyV1P12DCoreRestoreOwnerStage d = stages.D;
        P12ECoreOwnerRestoreStage e = stages.E;
        if (!TryStageRestoredP12FOwners(
                stages.CoreRoots, snapshots.PoliticalKnowledge, snapshots.Directives,
                snapshots.ActorChoices, snapshots.TravelParties, snapshots.Expeditions,
                d.Persons, e.Institutions, e.PoliticalClaims, e.Factions, e.Offices,
                e.PropertyOwnership, stages.Image.Header.Day, stages.StagedTime,
                stages.Definitions, snapshots.NpcFRows, d.Npcs,
                d.RuntimeIdentityIndex, d.SpatialNetwork, stagedEmptyExplorableSites,
                stages.ExpectedSession, stages.ReplacementOperation,
                out stagedOwners, out result, stageObserver)
            || stagedOwners == null || !stages.TryAcceptF(stagedOwners, stagedEmptyExplorableSites))
        {
            stages.Fail();
            stagedOwners = null;
            return false;
        }

        result = null;
        return true;
    }

    private bool HasRestoredContinuationStagingAuthority(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation)
    {
        bool initialRestore = expectedSession == null && replacementOperation == null;
        if (initialRestore)
        {
            return bootstrapAdmissionContext != null
                && bootstrapAdmissionContext.IsOwnedByCurrentThread()
                && activeSessionGate.Current == null
                && activeSessionGate.ActiveOperationCount == 0
                && !initialGenesisPreparationAttempted
                && preparedInitialGenesis == null
                && preparedInitialSession == null;
        }

        DailyV1ExecutionBoundaryState sourceState = expectedSession?.ExecutionBoundaryState;
        IDailyV1RuntimeAdvanceEvidenceBoundary sourceBoundary = expectedSession?.RuntimeAdvanceBoundary;
        return expectedSession != null && replacementOperation != null
            && replacementOperation.IsOwnedByCurrentThread()
            && ReferenceEquals(replacementOperation.ExpectedSession, expectedSession)
            && ReferenceEquals(activeSessionGate.Current, expectedSession)
            && replacementOperation.AuthorizesSessionReplacementCapture(sourceState)
            && activeSessionGate.ActiveOperationCount == 0
            && sourceBoundary != null
            && ReferenceEquals(sourceBoundary.ExecutionState, sourceState)
            && sourceState.IsHealthyDailyOwnerThreadBoundary()
            && sourceState.HasValidCompletedDailyBoundaryAdmission(
                sourceBoundary, sessionReplacement: true);
    }

    /// <summary>
    /// Creates the private admission identity for a restored Core candidate.
    /// The application derives it from its own captured bootstrap lineage and
    /// only while it owns the initial or exact replacement staging authority;
    /// a Unity host cannot choose a competing continuation context.
    /// </summary>
    internal bool TryCreateRestoredContinuationAdmissionContext(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out SimulationRuntimeAdmissionContext restoredContext,
        out string diagnostic)
    {
        restoredContext = null;
        diagnostic = null;
        lock (activeSessionGate.SyncRoot)
        {
            if (!HasRestoredContinuationStagingAuthority(expectedSession, replacementOperation))
            {
                diagnostic = "The Core application no longer holds the exact restored-continuation staging authority.";
                return false;
            }

            SimulationRuntimeAdmissionContext bootstrap = bootstrapAdmissionContext;
            if (bootstrap == null
                || bootstrap.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || bootstrap.IsRestoredContinuation
                || !bootstrap.IsOwnedByCurrentThread())
            {
                diagnostic = "The Core application has no current-thread Daily-v1 bootstrap identity for restoration.";
                return false;
            }

            if (expectedSession != null
                && !IsBootstrapOrRestoredDescendant(expectedSession.AdmissionContext))
            {
                diagnostic = "The exact replacement session is outside this application's bootstrap lineage.";
                return false;
            }

            try
            {
                restoredContext = bootstrap.CreateRestoredContinuationContext();
            }
            catch (InvalidOperationException exception)
            {
                diagnostic = "The Core application could not derive the restored admission identity: "
                    + exception.Message;
                return false;
            }

            if (restoredContext == null
                || ReferenceEquals(restoredContext, bootstrap)
                || !restoredContext.IsRestoredContinuation
                || !restoredContext.IsOwnedByCurrentThread()
                || restoredContext.ExpectedOwnerThreadId != bootstrap.ExpectedOwnerThreadId
                || !restoredContext.IsRestoredContinuationOf(bootstrap))
            {
                restoredContext = null;
                diagnostic = "The Core application could not establish a fresh restored identity in its bootstrap lineage.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Associates an unpublished or replacement Core session with the exact
    /// host boundary built for its execution state. Once published, the
    /// session's boundary is immutable; advances resolve it from the session
    /// rather than accepting a host-selected boundary on each call.
    /// </summary>
    internal bool TryBindRuntimeAdvanceBoundary(
        DailyV1ActiveRuntimeSession candidateSession,
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts)
    {
        if (candidateSession == null || hostPorts == null
            || !ReferenceEquals(hostPorts.ExecutionState,
                candidateSession.ExecutionBoundaryState))
            return false;
        lock (activeSessionGate.SyncRoot)
        {
            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (ReferenceEquals(current, candidateSession))
            {
                return candidateSession.TryBindSelectedRuntimeAdvanceHostPorts(hostPorts)
                    && candidateSession.ExecutionBoundaryState.TryBindApplicationAdvanceAuthority(
                        this, candidateSession);
            }
            if (!IsBootstrapOrRestoredDescendant(candidateSession.AdmissionContext)
                || activeSessionGate.ActiveOperationCount != 0
                || (current != null && !current.IsProfileCompatibleWith(candidateSession)))
                return false;
            return candidateSession.TryBindSelectedRuntimeAdvanceHostPorts(hostPorts)
                && candidateSession.ExecutionBoundaryState.TryBindApplicationAdvanceAuthority(
                    this, candidateSession);
        }
    }

    /// <summary>
    /// Advances only the exact published Core session. The application gate
    /// reserves that session against queries, saves, and replacement while
    /// the existing Core runtime protocol owns its single DailyAdvance scope,
    /// lease, completed-boundary accounting, and P12 token publication.
    /// </summary>
    internal bool TryAdvanceDay(
        DailyV1ActiveRuntimeSession expectedSession,
        out SimulationRuntimeAdvanceFailure failure)
    {
        IDailyV1RuntimeAdvanceEvidenceBoundary runtimeBoundary = expectedSession?.RuntimeAdvanceBoundary;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        if (expectedSession == null || runtimeBoundary == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !ReferenceEquals(runtimeBoundary.ExecutionState,
                expectedSession.ExecutionBoundaryState))
            return false;

        if (!activeSessionGate.TryBeginRuntimeAdvance(
                expectedSession,
                out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation operation))
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (operation)
        {
            if (!ReferenceEquals(operation.ExpectedSession, expectedSession)
                || !operation.IsOwnedByCurrentThread()
                || !ReferenceEquals(activeSessionGate.Current, expectedSession))
                return false;

            return runtimeBoundary is DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary
                && expectedSession.ExecutionBoundaryState.TryAdvanceSelectedDailyV1Day(
                    coreBoundary, out failure);
        }
    }

    /// <summary>
    /// Advances the application's current session without requiring a host to
    /// retain or select a Core session reference. The operation gate still
    /// validates that the captured session remains the exact published one.
    /// </summary>
    internal bool TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure)
    {
        DailyV1ActiveRuntimeSession expectedSession = activeSessionGate.Current;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        return expectedSession != null && TryAdvanceDay(expectedSession, out failure);
    }

    /// <summary>
    /// Advances the exact published session for one serialized multi-day
    /// operation. The execution boundary owns one DailyAdvance scope and lease
    /// for the entire span, including completed-boundary accounting and its
    /// final P12 capture-token publication.
    /// </summary>
    internal bool TryAdvanceDays(
        DailyV1ActiveRuntimeSession expectedSession,
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
        IDailyV1RuntimeAdvanceEvidenceBoundary runtimeBoundary = expectedSession?.RuntimeAdvanceBoundary;
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        if (expectedSession == null || runtimeBoundary == null
            || !ReferenceEquals(activeSessionGate.Current, expectedSession)
            || !ReferenceEquals(runtimeBoundary.ExecutionState,
                expectedSession.ExecutionBoundaryState))
            return false;

        if (!activeSessionGate.TryBeginRuntimeAdvance(
                expectedSession,
                out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation operation))
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (operation)
        {
            if (!ReferenceEquals(operation.ExpectedSession, expectedSession)
                || !operation.IsOwnedByCurrentThread()
                || !ReferenceEquals(activeSessionGate.Current, expectedSession))
                return false;

            return runtimeBoundary is DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary
                && expectedSession.ExecutionBoundaryState.TryAdvanceSelectedDailyV1Days(
                    coreBoundary, dayCount, out daysAdvanced, out failure);
        }
    }

    /// <summary>Advances the application's current session as one serialized operation.</summary>
    internal bool TryAdvanceDays(
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
        DailyV1ActiveRuntimeSession expectedSession = activeSessionGate.Current;
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        return expectedSession != null
            && TryAdvanceDays(expectedSession, dayCount, out daysAdvanced, out failure);
    }

    internal bool TryPublishReplacement(
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation,
        DailyV1ActiveRuntimeSession candidate)
    {
        if (candidate?.RuntimeAdvanceBoundary == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            return bootstrapAdmissionContext != null
                && IsBootstrapOrRestoredDescendant(candidate.AdmissionContext)
                && activeSessionGate.TryPublishReplacement(operation, candidate);
        }
    }

    internal bool TryPublishReplacement(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate)
    {
        if (candidate?.RuntimeAdvanceBoundary == null) return false;
        lock (activeSessionGate.SyncRoot)
        {
            return bootstrapAdmissionContext != null
                && IsBootstrapOrRestoredDescendant(candidate.AdmissionContext)
                && activeSessionGate.TryPublishReplacement(expectedSession, candidate);
        }
    }

    /// <summary>
    /// Commits one already staged P12 restored candidate. All admission,
    /// provenance, owner-thread, source-boundary, profile, and reservation
    /// checks are made against the exact Core sessions under the same lock as
    /// the sole active-session exchange. The host boundary supplies only a
    /// fresh owner census; Core verifies the token, identities, revisions,
    /// epoch, provenance, and replacement reservation before publication.
    /// </summary>
    internal bool TryCommitRestoredCandidate(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out string diagnostic) =>
        TryCommitRestoredCandidateCore(
            expectedSession, candidate, replacementOperation,
            hostProjectionTransaction: false,
            initialRestoreAdmissionVerified: false, out diagnostic);

    private bool TryCommitRestoredCandidateCore(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        bool hostProjectionTransaction,
        bool initialRestoreAdmissionVerified,
        out string diagnostic)
    {
        diagnostic = null;
        if (candidate == null)
        {
            diagnostic = "The Core application cannot commit a null restored session.";
            return false;
        }

        if (expectedSession == null && !initialRestoreAdmissionVerified)
        {
            diagnostic = "Initial Core restore publication requires the exact initial slot admission.";
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            if (hostProjectionTransaction)
            {
                if (!activeHostProjections.ContainsKey(candidate)
                    || (expectedSession == null
                        ? activeHostProjections.Count != 1
                        : !activeHostProjections.ContainsKey(expectedSession)
                            || activeHostProjections.Count != 2))
                {
                    diagnostic = "The Core restore commit does not have the exact staged host projection set.";
                    return false;
                }
            }
            else if (activeHostProjections.Count != 0)
            {
                diagnostic = "A session with a host projection must be replaced through the application projection transaction.";
                return false;
            }

            SimulationRuntimeAdmissionContext candidateAdmission = candidate.AdmissionContext;
            if (bootstrapAdmissionContext == null
                || !bootstrapAdmissionContext.IsOwnedByCurrentThread()
                || candidateAdmission == null
                || !candidateAdmission.IsRestoredContinuation
                || !candidateAdmission.IsOwnedByCurrentThread()
                || !IsBootstrapOrRestoredDescendant(candidateAdmission)
                || !candidate.ExecutionBoundaryState.IsHealthyDailyOwnerThreadBoundary()
                || !candidate.ExecutionBoundaryState.HasValidRestoredDailyBoundaryAdmission(
                    candidate.RuntimeAdvanceBoundary)
                || candidate.ExecutionBoundaryState.ActiveOperationCount != 0)
            {
                diagnostic = "The restored candidate is not a healthy, idle, exact Core continuation admitted on this application's owner thread.";
                return false;
            }

            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (current == null)
            {
                if (expectedSession != null
                    || replacementOperation != null
                    || activeSessionGate.ActiveOperationCount != 0
                    || initialGenesisPreparationAttempted
                    || preparedInitialGenesis != null
                    || preparedInitialSession != null
                || !initialRestoreAdmissionVerified
                    || !activeSessionGate.TryPublishInitial(candidate))
                {
                    diagnostic = "Initial restore requires an unused Core application and a candidate with no replacement reservation.";
                    return false;
                }

                return true;
            }

            if (expectedSession == null
                || !ReferenceEquals(expectedSession, current)
                || replacementOperation == null
                || !ReferenceEquals(replacementOperation.ExpectedSession, current)
                || !replacementOperation.AuthorizesSessionReplacementCapture(
                    current.ExecutionBoundaryState)
                || activeSessionGate.ActiveOperationCount != 0
                || !current.ExecutionBoundaryState.IsHealthyDailyOwnerThreadBoundary()
                || !current.ExecutionBoundaryState.HasValidCompletedDailyBoundaryAdmission(
                    current.RuntimeAdvanceBoundary, sessionReplacement: true)
                || !current.IsProfileCompatibleWith(candidate)
                || !activeSessionGate.TryPublishReplacement(
                    replacementOperation, candidate))
            {
                diagnostic = "Restored replacement requires the exact healthy current Core session, its owner-thread reservation, and a compatible admitted candidate.";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Commits an admitted initial restore into the Core application without
    /// requiring a host projection. The host boundary still supplies its exact
    /// runtime execution ports; the active session and completed-boundary
    /// admission are published by Core under the same session gate.
    /// </summary>
    internal bool TryCommitInitialRestoredCoreCandidate(
        DailyV1ApplicationStartAdmission initialStartAdmission,
        P12AContinuationImage image,
        DailyV1ActiveRuntimeSession candidate,
        IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
        out string diagnostic)
    {
        diagnostic = null;
        lock (activeSessionGate.SyncRoot)
        {
            if (!IsForInitialRestoreAdmission(initialStartAdmission, image))
            {
                diagnostic = "Initial Core restore publication requires this application's exact admitted image token.";
                return false;
            }

            if (candidate == null
                || !candidate.TryGetInitialRestoreCommitProof(
                    image, out DailyV1InitialRestoredImageCommitProof proof)
                || candidateHostPorts == null
                || !ReferenceEquals(candidate.ExecutionBoundaryState,
                    candidateHostPorts.ExecutionState))
            {
                diagnostic = "Initial Core restore publication requires the exact private candidate, image proof, and host boundary.";
                return false;
            }

            bool finalized;
            DailyV1RestoredBoundaryFinalizationFailure finalizationFailure;
            try
            {
                finalized = TryFinalizeRestoredCandidateBoundary(
                    candidate,
                    candidateHostPorts,
                    proof.WorldId,
                    proof.AbsoluteDay,
                    proof.CompletedCoreSequence,
                    proof.TargetOwnerSections,
                    proof.TargetMutationEpoch,
                    expectedSession: null,
                    replacementOperation: null,
                    proof.StageObserver,
                    out _,
                    out _,
                    out finalizationFailure,
                    out diagnostic);
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException
                || exception is NullReferenceException)
            {
                diagnostic = "The Core application could not finalize the private restored boundary: "
                    + exception.Message;
                return false;
            }

            if (!finalized)
            {
                if (string.IsNullOrWhiteSpace(diagnostic))
                    diagnostic = "The Core application rejected initial restored-boundary finalization: "
                        + finalizationFailure + ".";
                return false;
            }

            bool committed = TryCommitRestoredCandidateCore(null, candidate, null, false, true, out diagnostic);
            if (committed) candidate.ClearInitialRestoreCommitProof(proof);
            return committed;
        }
    }

    /// <summary>
    /// Verifies the exact initial-slot restore admission while holding the same
    /// gate used to publish the reconstructed Core session and host projection.
    /// </summary>
    internal bool TryCommitInitialRestoredCandidateWithHostProjection(
        DailyV1ApplicationStartAdmission initialStartAdmission,
        P12AContinuationImage image,
        DailyV1ActiveRuntimeSession candidate,
        IDailyV1ApplicationHostProjection candidateHostProjection,
        IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
        out string diagnostic)
    {
        diagnostic = null;
        lock (activeSessionGate.SyncRoot)
        {
            if (!IsForInitialRestoreAdmission(initialStartAdmission, image))
            {
                diagnostic = "Initial restore publication requires this application's exact admitted image token.";
                return false;
            }

            if (candidate == null
                || !candidate.TryGetInitialRestoreCommitProof(
                    image, out DailyV1InitialRestoredImageCommitProof proof)
                || candidateHostProjection == null
                || candidateHostPorts == null
                || !ReferenceEquals(candidate.ExecutionBoundaryState,
                    candidateHostPorts.ExecutionState))
            {
                diagnostic = "Initial restore publication requires the exact private candidate, image proof, host projection, and host boundary.";
                return false;
            }

            bool finalized;
            DailyV1RestoredBoundaryFinalizationFailure finalizationFailure;
            try
            {
                finalized = TryFinalizeRestoredCandidateBoundary(
                    candidate,
                    candidateHostPorts,
                    proof.WorldId,
                    proof.AbsoluteDay,
                    proof.CompletedCoreSequence,
                    proof.TargetOwnerSections,
                    proof.TargetMutationEpoch,
                    expectedSession: null,
                    replacementOperation: null,
                    proof.StageObserver,
                    out _,
                    out _,
                    out finalizationFailure,
                    out diagnostic);
            }
            catch (Exception exception) when (exception is ArgumentException
                || exception is InvalidOperationException || exception is OverflowException
                || exception is NullReferenceException)
            {
                diagnostic = "The Core application could not finalize the private restored boundary: "
                    + exception.Message;
                return false;
            }

            if (!finalized)
            {
                if (string.IsNullOrWhiteSpace(diagnostic))
                    diagnostic = "The Core application rejected initial restored-boundary finalization: "
                        + finalizationFailure + ".";
                return false;
            }

            bool committed = TryCommitRestoredCandidateWithHostProjectionCore(
                null, candidate, null, candidateHostProjection, candidateHostPorts,
                null, initialRestoreAdmissionVerified: true, out diagnostic);
            if (committed) candidate.ClearInitialRestoreCommitProof(proof);
            return committed;
        }
    }

    /// <summary>
    /// Commits a detached Core replacement together with the corresponding
    /// host projection under the application's one session gate. This keeps a
    /// Unity adapter from maintaining a second publication authority while
    /// still allowing the host to construct its view before the exchange.
    /// </summary>
    internal bool TryCommitRestoredCandidateWithHostProjection(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate,
        IDailyV1ApplicationHostProjection expectedHostProjection,
        IDailyV1ApplicationHostProjection candidateHostProjection,
        IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out string diagnostic)
    {
        return TryCommitRestoredCandidateWithHostProjectionCore(
            expectedSession, candidate, expectedHostProjection, candidateHostProjection,
            candidateHostPorts, replacementOperation,
            initialRestoreAdmissionVerified: false, out diagnostic);
    }

    private bool TryCommitRestoredCandidateWithHostProjectionCore(
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ActiveRuntimeSession candidate,
        IDailyV1ApplicationHostProjection expectedHostProjection,
        IDailyV1ApplicationHostProjection candidateHostProjection,
        IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        bool initialRestoreAdmissionVerified,
        out string diagnostic)
    {
        diagnostic = null;
        if (candidate == null || candidateHostProjection == null || candidateHostPorts == null)
        {
            diagnostic = "A restored Core candidate, its host projection, and its exact host boundary are required.";
            return false;
        }
        if (expectedSession == null && !initialRestoreAdmissionVerified)
        {
            diagnostic = "Initial Core restore publication requires the exact initial slot admission.";
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
            DailyV1ActiveRuntimeSession current = activeSessionGate.Current;
            if (expectedSession == null)
            {
                if (current != null || expectedHostProjection != null
                    || replacementOperation != null || activeHostProjections.ContainsKey(candidate))
                {
                    diagnostic = "Initial restore requires an unused Core application and no prior host projection.";
                    return false;
                }
            }
            else if (!ReferenceEquals(current, expectedSession)
                || expectedHostProjection == null
                || !activeHostProjections.TryGetValue(expectedSession,
                    out IDailyV1ApplicationHostProjection currentProjection)
                || !ReferenceEquals(currentProjection, expectedHostProjection))
            {
                diagnostic = "Replacement restore requires the exact current Core session and its registered host projection.";
                return false;
            }

            if (activeHostProjections.ContainsKey(candidate)
                || !TryBindRuntimeAdvanceBoundary(candidate, candidateHostPorts))
            {
                diagnostic = "The restored Core candidate could not bind its exact runtime boundary or already has a projection.";
                return false;
            }

            activeHostProjections.Add(candidate, candidateHostProjection);
            if (!TryCommitRestoredCandidateCore(
                    expectedSession, candidate, replacementOperation,
                    hostProjectionTransaction: true,
                    initialRestoreAdmissionVerified: initialRestoreAdmissionVerified,
                    out diagnostic))
            {
                activeHostProjections.Remove(candidate);
                return false;
            }

            if (expectedSession != null)
                activeHostProjections.Remove(expectedSession);
            return true;
        }
    }

    private bool IsBootstrapOrRestoredDescendant(
        SimulationRuntimeAdmissionContext candidate)
    {
        if (ReferenceEquals(candidate, bootstrapAdmissionContext)) return true;
        return candidate != null
            && candidate.IsRestoredContinuationOf(bootstrapAdmissionContext);
    }
}
