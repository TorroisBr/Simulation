using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Host ports for the selected Daily-v1 stage. Core owns the selected
/// Crime/Justice day-start, Merchant plan urgency, economy, local-knowledge,
/// ordered actor-turn, directive-preparation, and travel algorithms, along
/// with demographic ordering, aggregate-owner writes, and selected Person-death
/// P12 admission. The host contributes sample providers and optional Unity
/// projection/compatibility facts. Place content and autonomous expedition
/// production are outside this accepted profile.
/// </summary>
internal interface IDailyV1SelectedDailyExecutionBoundary
{
    void BeginLoggerDay(long absoluteDay);
    bool HasExactCrimeJusticeProjectionOwnerBindings(
        DailyV1SelectedRuntimeOwnerGraph graph);
    bool TryCreateSelectedDemographyContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDemographyExecutionContext context);
    bool TryCreateSelectedDemographyHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDemographyHostPorts hostPorts);
    void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result);
    /// <summary>
    /// Legacy context port for direct/unadmitted execution only. An admitted
    /// Core session derives its selected day-start roster and owner boundaries
    /// from the exact Core roster and actor-turn boundary already acquired for
    /// that day.
    /// </summary>
    bool TryCreateSelectedDayStartContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDayStartExecutionContext context);
    /// <summary>Legacy whole-stage path used only when selected owner coverage is mixed.</summary>
    void BeginSimulationDay();
    void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph);
    void LogSelectedDailyEconomy(
        DailyV1CityRuntimeOwners owner,
        SimulationLogCategory category,
        string message);
    void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph);
    bool TryCreateSelectedActorTurnContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnExecutionContext context);
    bool TryCreateSelectedActorTurnHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnHostPorts hostPorts);
    bool TryCreateSelectedTravelDayContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedTravelDayExecutionContext context);
    /// <summary>
    /// Supplies only projection and P12 ports for an admitted Core session.
    /// Core binds those ports to its own selected travel context and graph.
    /// </summary>
    bool TryCreateSelectedTravelDayHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedTravelDayHostPorts hostPorts);
}
/// <summary>
/// Narrow Unity-owned effects and adapters for one admitted selected runtime.
/// Core, rather than the host, builds the selected execution boundary and
/// derives actor, demography, day-start, and travel contexts from the exact
/// execution state and owner graph. The host contributes optional projection,
/// demographic sample providers, logging, and compatibility facts only; Core
/// composes the selected Person-death P12 boundary for its admitted graph.
/// </summary>
internal interface IDailyV1SelectedDailyExecutionHostPorts
{
    void BeginLoggerDay(long absoluteDay);
    bool HasExactCrimeJusticeProjectionOwnerBindings(
        DailyV1SelectedRuntimeOwnerGraph graph);
    bool TryCreateSelectedDemographyHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDemographyHostPorts hostPorts);
    void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result);
    /// <summary>
    /// Retains the established whole-stage compatibility path for a mixed
    /// roster that contains runtime members outside the admitted Core graph.
    /// </summary>
    void BeginSimulationDay();
    /// <summary>Validates only the Unity CityRuntime projections for Core's admitted owners.</summary>
    void EnsureSelectedEconomyProjectionCanRun(DailyV1SelectedRuntimeOwnerGraph graph);
    void LogSelectedDailyEconomy(
        DailyV1CityRuntimeOwners owner,
        SimulationLogCategory category,
        string message);
    /// <summary>Validates Unity NPC/system projections after Core validates its owner graph.</summary>
    void EnsureSelectedLocalKnowledgeProjectionCanRun(
        DailyV1SelectedRuntimeOwnerGraph graph,
        bool merchantTradeEnabled);
    bool TryCreateSelectedActorTurnHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnHostPorts hostPorts);
    bool TryCreateSelectedTravelDayHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedTravelDayHostPorts hostPorts);
}

/// <summary>
/// Core constructs one selected execution adapter for the exact admitted
/// session. The wrapper owns session/graph binding and all selected contexts;
/// its Unity ports cannot select a different roster, owner store, clock, or
/// execution graph.
/// </summary>
internal sealed class DailyV1CoreSelectedDailyExecutionBoundary
    : IDailyV1SelectedDailyExecutionBoundary
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph ownerGraph;
    private readonly IDailyV1SelectedDailyExecutionHostPorts hostPorts;

    private DailyV1CoreSelectedDailyExecutionBoundary(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IDailyV1SelectedDailyExecutionHostPorts hostPorts)
    {
        this.executionState = executionState;
        this.ownerGraph = ownerGraph;
        this.hostPorts = hostPorts;
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IDailyV1SelectedDailyExecutionHostPorts hostPorts,
        out IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        boundary = null;
        if (executionState == null || ownerGraph == null || hostPorts == null
            || !executionState.HasAdmittedSelectedDailyExecution
            || !executionState.SelectedRuntimeExecutionRootsBound
            || !executionState.HasRuntimeProtocolOperationInventory
            || executionState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
            || ownerGraph.Definitions?.EffectiveConfiguration == null
            || executionState.SimulationTime == null)
            return false;

        boundary = new DailyV1CoreSelectedDailyExecutionBoundary(
            executionState, ownerGraph, hostPorts);
        return true;
    }

    private bool IsBoundTo(DailyV1SelectedRuntimeOwnerGraph graph) =>
        graph != null
        && executionState.HasAdmittedSelectedDailyExecution
        && executionState.SelectedRuntimeExecutionRootsBound
        && executionState.HasRuntimeProtocolOperationInventory
        && executionState.AdmissionContext?.Profile
            == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
        && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
        && ReferenceEquals(graph, ownerGraph)
        && graph.Definitions?.EffectiveConfiguration != null
        && executionState.SimulationTime != null;

    internal bool IsBoundTo(
        DailyV1ExecutionBoundaryState candidateState,
        DailyV1SelectedRuntimeOwnerGraph candidateGraph) =>
        ReferenceEquals(executionState, candidateState)
        && ReferenceEquals(ownerGraph, candidateGraph)
        && IsBoundTo(candidateGraph);

    private void EnsureBoundTo(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!IsBoundTo(graph))
            throw new InvalidOperationException(
                "The Core selected execution adapter no longer matches its admitted session and owner graph.");
    }

    public void BeginLoggerDay(long absoluteDay)
    {
        EnsureBoundTo(ownerGraph);
        hostPorts.BeginLoggerDay(absoluteDay);
    }

    public bool HasExactCrimeJusticeProjectionOwnerBindings(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!IsBoundTo(graph)
            || !hostPorts.HasExactCrimeJusticeProjectionOwnerBindings(graph))
            return false;

        if (hostPorts is IDailyV1SelectedCrimeJusticeDailyMutationHostPort bindingPort)
        {
            if (!bindingPort.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding))
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                return false;
            }
            if (binding == null || !binding.AdmissionEnabled)
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                return false;
            }

            bool hostProvidesCrimeJusticeOwnerIdentity =
                binding.CrimeOwnerIdentity != null
                || binding.CrimeReceiptCensusOwnerIdentity != null
                || binding.CrimeJusticeRecordsOwner != null
                || binding.JusticeOwnerIdentity != null
                || binding.JusticeReceiptCensusOwnerIdentity != null
                || binding.JusticeRecordsOwner != null;
            bool exactBinding = hostProvidesCrimeJusticeOwnerIdentity
                ? executionState.TryBindSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, binding)
                : executionState.TryBindSelectedCrimeJusticeDailyMutationBindingFromCoreCensus(
                    graph);
            if (!exactBinding)
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                return false;
            }
        }
        return true;
    }

    public bool TryCreateSelectedDemographyContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDemographyExecutionContext context)
    {
        context = null;
        return TryCreateSelectedDemographyHostPorts(
                graph, out IDailyV1SelectedDemographyHostPorts ports)
            && DailyV1CoreSelectedDemographyExecutionContext.TryCreate(
                executionState, ownerGraph, ports, out context);
    }

    public bool TryCreateSelectedDemographyHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDemographyHostPorts selectedHostPorts)
    {
        selectedHostPorts = null;
        if (!IsBoundTo(graph)
            || !hostPorts.TryCreateSelectedDemographyHostPorts(
                ownerGraph, out IDailyV1SelectedDemographyHostPorts sourceHostPorts)
            || sourceHostPorts == null)
            return false;

        // A detached Core host may provide the already Core-owned death
        // boundary for this exact execution state. Do not wrap that boundary
        // as if it were a Unity projection: doing so would create a second
        // callback path around the same authoritative owner operation.
        if (sourceHostPorts is DailyV1CoreSelectedDemographyHostPorts coreHostPorts
            && coreHostPorts.Boundary is DailyV1CoreSelectedPersonDeathBoundary coreBoundary
            && coreBoundary.IsBoundTo(executionState, ownerGraph))
        {
            selectedHostPorts = coreHostPorts;
            return true;
        }

        IDailyV1SelectedDemographyBoundary projection = sourceHostPorts.Boundary;
        selectedHostPorts = new DailyV1CoreSelectedDemographyHostPorts(
            sourceHostPorts.MortalitySamples,
            sourceHostPorts.AggregateProvider,
            new DailyV1CoreSelectedPersonDeathBoundary(
                executionState, ownerGraph, projection));
        return true;
    }

    public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result)
    {
        EnsureBoundTo(ownerGraph);
        hostPorts.PublishSelectedDemographyResult(result);
    }

    public bool TryCreateSelectedDayStartContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDayStartExecutionContext context)
    {
        context = null;
        // An admitted day-start roster is derived by Core from its exact actor
        // roster. Only the mixed-roster fallback calls BeginSimulationDay.
        return false;
    }

    public void BeginSimulationDay()
    {
        EnsureBoundTo(ownerGraph);
        hostPorts.BeginSimulationDay();
    }

    public void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        EnsureBoundTo(graph);
        if (executionState.CalendarIdentity == null)
        {
            for (int index = 0; index < ownerGraph.Cities.Count; index++)
                if (ownerGraph.Cities[index]?.HasLocalDailyMaterialFlow == true)
                    throw new InvalidOperationException(
                        "The selected Core economy stage requires its admitted calendar for local material flow.");
        }

        for (int index = 0; index < ownerGraph.Cities.Count; index++)
        {
            DailyV1CityRuntimeOwners city = ownerGraph.Cities[index];
            if (city == null || !ownerGraph.ContainsExactCityOwner(city))
                throw new InvalidOperationException(
                    "The selected Core economy stage could not admit its exact City owner graph.");
        }

        hostPorts.EnsureSelectedEconomyProjectionCanRun(ownerGraph);
    }

    public void LogSelectedDailyEconomy(
        DailyV1CityRuntimeOwners owner,
        SimulationLogCategory category,
        string message)
    {
        EnsureBoundTo(ownerGraph);
        bool exactCityOwner = false;
        for (int index = 0; index < ownerGraph.Cities.Count; index++)
            if (ReferenceEquals(ownerGraph.Cities[index], owner))
            {
                exactCityOwner = true;
                break;
            }
        if (!exactCityOwner)
            throw new InvalidOperationException(
                "The Core daily economy logger received a City outside the admitted owner graph.");
        hostPorts.LogSelectedDailyEconomy(owner, category, message);
    }

    public void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        EnsureBoundTo(graph);
        if (ownerGraph.Cities == null || ownerGraph.Npcs == null
            || !ownerGraph.MatchesExactOwnerInstances(ownerGraph.Cities, ownerGraph.Npcs))
            throw new InvalidOperationException(
                "The selected Core local-knowledge stage requires its exact City/NPC owner graph.");

        bool merchantTradeEnabled = ownerGraph.Definitions.EffectiveConfiguration.MerchantTrade?.Enabled == true;
        hostPorts.EnsureSelectedLocalKnowledgeProjectionCanRun(
            ownerGraph, merchantTradeEnabled);
    }

    public bool TryCreateSelectedActorTurnContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnExecutionContext context)
    {
        context = null;
        return IsBoundTo(graph)
            && DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
                executionState, ownerGraph, this, out context);
    }

    public bool TryCreateSelectedActorTurnHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnHostPorts selectedHostPorts)
    {
        selectedHostPorts = null;
        return IsBoundTo(graph)
            && hostPorts.TryCreateSelectedActorTurnHostPorts(ownerGraph, out selectedHostPorts)
            && selectedHostPorts != null;
    }

    public bool TryCreateSelectedTravelDayContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedTravelDayExecutionContext context)
    {
        context = null;
        return TryCreateSelectedTravelDayHostPorts(
                graph, out IDailyV1SelectedTravelDayHostPorts ports)
            && DailyV1CoreSelectedTravelDayExecutionContext.TryCreate(
                executionState, ownerGraph, ports, out context);
    }

    public bool TryCreateSelectedTravelDayHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedTravelDayHostPorts selectedHostPorts)
    {
        selectedHostPorts = null;
        if (!IsBoundTo(graph)
            || !hostPorts.TryCreateSelectedTravelDayHostPorts(
                ownerGraph, out selectedHostPorts)
            || selectedHostPorts?.Boundary == null
            || selectedHostPorts.P12Protocol == null)
        {
            selectedHostPorts = null;
            return false;
        }
        return true;
    }
}

/// <summary>
/// Compatibility ports for the established start-of-day Crime/Justice and
/// Merchant-plan operations. Core owns invoking these operations; Unity
/// supplies only their existing admission and projection boundaries. The
/// admitted state-backed path takes its roster from Core state; a mixed roster
/// uses the legacy whole-stage path.
/// </summary>
internal interface IDailyV1SelectedDayStartExecutionContext
{
    IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
    /// <summary>
    /// Compatibility signal for callers outside an admitted execution state.
    /// An admitted runtime derives unselected membership from its Core-bound
    /// ordered identity roster instead.
    /// </summary>
    bool HasUnselectedLiveRuntimeMember { get; }
    bool CrimeSystemPresent { get; }
    bool JusticeSystemPresent { get; }
    IDailyV1SelectedCrimeJusticeDailyBoundary CrimeJusticeBoundary { get; }
    IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
}

internal interface IDailyV1SelectedDemographyExecutionContext
{
    SimulationCalendar Calendar { get; }
    IPersonNaturalMortalitySampleProvider MortalitySamples { get; }
    IAggregateDemographyProvider AggregateProvider { get; }
    IDailyV1SelectedDemographyBoundary Boundary { get; }
}

/// <summary>
/// Host inputs required by the selected demographic stage. The admitted Core
/// session supplies calendar and graph identity; the host supplies only the
/// existing sample providers and writer boundary.
/// </summary>
internal interface IDailyV1SelectedDemographyHostPorts
{
    IPersonNaturalMortalitySampleProvider MortalitySamples { get; }
    IAggregateDemographyProvider AggregateProvider { get; }
    IDailyV1SelectedDemographyBoundary Boundary { get; }
}

/// <summary>
/// Core-composed demographic ports. Providers remain host inputs, while the
/// selected death boundary is created by Core for the admitted state and graph.
/// </summary>
internal sealed class DailyV1CoreSelectedDemographyHostPorts
    : IDailyV1SelectedDemographyHostPorts
{
    internal DailyV1CoreSelectedDemographyHostPorts(
        IPersonNaturalMortalitySampleProvider mortalitySamples,
        IAggregateDemographyProvider aggregateProvider,
        IDailyV1SelectedDemographyBoundary boundary)
    {
        MortalitySamples = mortalitySamples;
        AggregateProvider = aggregateProvider;
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
    }

    public IPersonNaturalMortalitySampleProvider MortalitySamples { get; }
    public IAggregateDemographyProvider AggregateProvider { get; }
    public IDailyV1SelectedDemographyBoundary Boundary { get; }
}

/// <summary>
/// Core-owned context for the admitted demographic stage. It binds host
/// sample providers to the exact Core calendar and owner graph.
/// </summary>
internal sealed class DailyV1CoreSelectedDemographyExecutionContext
    : IDailyV1SelectedDemographyExecutionContext
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph ownerGraph;
    private readonly IDailyV1SelectedDemographyHostPorts hostPorts;

    private DailyV1CoreSelectedDemographyExecutionContext(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IDailyV1SelectedDemographyHostPorts hostPorts)
    {
        this.executionState = executionState;
        this.ownerGraph = ownerGraph;
        this.hostPorts = hostPorts;
    }

    public SimulationCalendar Calendar
    {
        get
        {
            EnsureStillBoundToSession();
            return executionState.CalendarIdentity;
        }
    }

    public IPersonNaturalMortalitySampleProvider MortalitySamples
    {
        get
        {
            EnsureStillBoundToSession();
            return hostPorts.MortalitySamples;
        }
    }

    public IAggregateDemographyProvider AggregateProvider
    {
        get
        {
            EnsureStillBoundToSession();
            return hostPorts.AggregateProvider;
        }
    }

    public IDailyV1SelectedDemographyBoundary Boundary
    {
        get
        {
            EnsureStillBoundToSession();
            return hostPorts.Boundary;
        }
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        IDailyV1SelectedDemographyHostPorts hostPorts,
        out IDailyV1SelectedDemographyExecutionContext context)
    {
        context = null;
        if (executionState == null || ownerGraph == null || hostPorts == null
            || !executionState.HasAdmittedSelectedDailyExecution
            || executionState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !executionState.SelectedRuntimeExecutionRootsBound
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
            || executionState.CalendarIdentity == null
            || ownerGraph.PersonStore == null
            || hostPorts.Boundary == null)
            return false;

        context = new DailyV1CoreSelectedDemographyExecutionContext(
            executionState, ownerGraph, hostPorts);
        return true;
    }

    private void EnsureStillBoundToSession()
    {
        if (!executionState.HasAdmittedSelectedDailyExecution
            || !executionState.SelectedRuntimeExecutionRootsBound
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
            || executionState.CalendarIdentity == null
            || ownerGraph.PersonStore == null
            || hostPorts.Boundary == null)
            throw new InvalidOperationException(
                "The selected demography context is no longer bound to its admitted Core session.");
    }
}

/// <summary>
/// Exact owner inputs and host ports for Core's existing ordered actor-turn
/// stage. The host contributes runtime identity projections and P8/P12/action
/// adapters; Core builds the roster and invokes the operation stage.
/// </summary>
internal interface IDailyV1SelectedActorTurnExecutionContext
{
    IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
    PersonStore PersonStore { get; }
    ActorChoiceStore ActorChoiceStore { get; }
    DailyV1ActorTurnCoreRoots CoreRoots { get; }
    IDailyV1ActorTurnExecutionBoundary Boundary { get; }
    IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
}

/// <summary>
/// Exact decision and random roots already bound to the admitted Core runtime.
/// The Unity action adapter may project those decisions, but it cannot choose
/// or replace the allocator, sequence, decision store, or random source used
/// by an admitted actor turn.
/// </summary>
internal sealed class DailyV1ActorTurnCoreRoots
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph ownerGraph;

    internal IAuthoritativeRandomSource RandomSource { get; }
    internal RuntimeIdAllocator IdAllocator { get; }
    internal SimulationRecordSequence RecordSequence { get; }
    internal NpcDecisionStore DecisionStore { get; }
    internal long AbsoluteDay => executionState.SimulationTime?.AbsoluteDay ?? -1L;

    internal DailyV1ActorTurnCoreRoots(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph)
    {
        this.executionState = executionState ?? throw new ArgumentNullException(nameof(executionState));
        this.ownerGraph = ownerGraph ?? throw new ArgumentNullException(nameof(ownerGraph));
        RandomSource = executionState.SelectedRandomSource
            ?? throw new ArgumentException("The Core actor-turn random source is unavailable.", nameof(executionState));
        IdAllocator = executionState.SelectedRuntimeIdAllocator
            ?? throw new ArgumentException("The Core actor-turn ID allocator is unavailable.", nameof(executionState));
        RecordSequence = executionState.SelectedRecordSequence
            ?? throw new ArgumentException("The Core actor-turn record sequence is unavailable.", nameof(executionState));
        DecisionStore = executionState.SelectedDecisionStore
            ?? throw new ArgumentException("The Core actor-turn decision store is unavailable.", nameof(executionState));
    }

    internal bool IsBoundTo(
        DailyV1ExecutionBoundaryState candidateState,
        DailyV1SelectedRuntimeOwnerGraph candidateGraph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore)
    {
        return ReferenceEquals(executionState, candidateState)
            && ReferenceEquals(ownerGraph, candidateGraph)
            && candidateState != null
            && candidateState.SelectedRuntimeExecutionRootsBound
            && ReferenceEquals(candidateState.SelectedRuntimeOwnerGraph, ownerGraph)
            && ReferenceEquals(candidateState.SelectedRandomSource, RandomSource)
            && ReferenceEquals(candidateState.SelectedRuntimeIdAllocator, IdAllocator)
            && ReferenceEquals(candidateState.SelectedRecordSequence, RecordSequence)
            && ReferenceEquals(candidateState.SelectedDecisionStore, DecisionStore)
            && ReferenceEquals(ownerGraph.PersonStore, personStore)
            && ReferenceEquals(candidateState.SelectedActorChoiceStore, actorChoiceStore)
            && ReferenceEquals(actorChoiceStore?.PersonStore, personStore);
    }

    internal bool IsBoundTo(
        DailyV1SelectedRuntimeOwnerGraph candidateGraph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore) => IsBoundTo(
            executionState, candidateGraph, personStore, actorChoiceStore);

    internal bool IsBoundToGraph(DailyV1SelectedRuntimeOwnerGraph candidateGraph) =>
        ReferenceEquals(ownerGraph, candidateGraph)
        && executionState.SelectedRuntimeExecutionRootsBound
        && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
        && ReferenceEquals(executionState.SelectedRandomSource, RandomSource)
        && ReferenceEquals(executionState.SelectedRuntimeIdAllocator, IdAllocator)
        && ReferenceEquals(executionState.SelectedRecordSequence, RecordSequence)
        && ReferenceEquals(executionState.SelectedDecisionStore, DecisionStore);
}

/// <summary>
/// Creates the admitted actor-turn context from Core's exact bound roots.
/// The host supplies only the selected actor-turn boundary; it cannot choose the
/// actor roster, PersonStore, or ActorChoiceStore used by the shared stage.
/// </summary>
internal static class DailyV1SelectedActorTurnExecutionContextFactory
{
    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedDailyExecutionBoundary hostBoundary,
        out IDailyV1SelectedActorTurnExecutionContext context)
    {
        context = null;
        if (executionState == null || graph == null || hostBoundary == null
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            || !executionState.SelectedRuntimeExecutionRootsBound
            || executionState.SelectedRuntimeActorRoster == null
            || graph.PersonStore == null
            || executionState.SelectedActorChoiceStore == null
            || !ReferenceEquals(executionState.SelectedActorChoiceStore.PersonStore, graph.PersonStore))
            return false;

        DailyV1ActorTurnCoreRoots coreRoots = executionState.SelectedRuntimeActorTurnCoreRoots;
        if (coreRoots == null
            || !coreRoots.IsBoundTo(
                executionState, graph, graph.PersonStore, executionState.SelectedActorChoiceStore))
            return false;

        if (!hostBoundary.TryCreateSelectedActorTurnHostPorts(
                graph, out IDailyV1SelectedActorTurnHostPorts actorHostPorts)
            || actorHostPorts == null)
            return TryCreateCoreOnly(executionState, graph, out context);

        if (!DailyV1CoreSelectedActorTurnExecutionBoundary.TryCreate(
                executionState, graph, graph.PersonStore,
                executionState.SelectedActorChoiceStore, coreRoots,
                actorHostPorts,
                out DailyV1CoreSelectedActorTurnExecutionBoundary actorBoundary)
            || !TryCreateCoreMerchantBoundary(
                graph, coreRoots, actorBoundary,
                out IDailyV1SelectedMerchantTradeStateBoundary merchantTradeBoundary))
            return false;
        actorBoundary.BindMerchantBoundary(merchantTradeBoundary);

        context = new CoreSelectedActorTurnExecutionContext(
            executionState.SelectedRuntimeActorRoster,
            graph.PersonStore,
            executionState.SelectedActorChoiceStore,
            coreRoots,
            actorBoundary,
            merchantTradeBoundary);
        if (context.MerchantTradeBoundary == null
            && graph.Definitions.EffectiveConfiguration?.MerchantTrade?.Enabled == true)
        {
            context = null;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Creates the bounded actor-turn context that needs no Unity host ports.
    /// This path is intentionally narrower than the admitted Unity profile:
    /// it requires a sealed full-owner census and the active DailyAdvance scope,
    /// executes the selected Merchant plan pass through Core's sealed P12 rows,
    /// excludes actor-choice inputs, and permits the existing
    /// targetless autonomous actions, the exact Core Guard Arrest, FleeCity, and Steal transitions,
    /// and the admitted scheduled EscapePrison ForceOutcome. Unsupported profiles are rejected before actor selection can
    /// consume deterministic RNG or mutate owners.
    /// </summary>
    internal static bool TryCreateCoreOnly(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnExecutionContext context)
    {
        context = null;
        if (executionState == null || graph == null
            || !executionState.TryBindSelectedCrimeJusticeDailyMutationBindingFromCoreCensus(graph)
            || !CanExecuteCoreOnlyActorTurn(
                executionState, graph, requireActiveDailyAdvanceScope: true, out _,
                allowRetainedPriorDayBuyGoodsActionSlot: true,
                allowRetainedPriorDayTradeTravelActionSlot: true)
            || graph.PersonStore == null
            || executionState.SelectedActorChoiceStore == null
            || !ReferenceEquals(executionState.SelectedActorChoiceStore.PersonStore, graph.PersonStore))
            return false;
        if (!executionState.TryBindSelectedCoreLocalKnowledgeMutationBoundaries(graph))
            return false;

        DailyV1ActorTurnCoreRoots coreRoots = executionState.SelectedRuntimeActorTurnCoreRoots;
        if (coreRoots == null
            || !coreRoots.IsBoundTo(
                executionState, graph, graph.PersonStore, executionState.SelectedActorChoiceStore)
            || !executionState.TryBindSelectedCoreDecisionMutationBoundaries(
                executionState.SelectedRuntimeIdAllocator,
                executionState.SelectedRecordSequence)
            || !DailyV1CoreSelectedActorTurnExecutionBoundary.TryCreateCoreOnly(
                executionState, graph, graph.PersonStore,
                executionState.SelectedActorChoiceStore, coreRoots,
                out DailyV1CoreSelectedActorTurnExecutionBoundary actorBoundary))
            return false;

        if (!DailyV1ActorTurnExecutionStage.TryBuildRoster(
                graph, executionState.SelectedRuntimeActorRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterEntry> roster))
            return false;

        if (!TryCreateCoreMerchantBoundary(
                graph, coreRoots, actorBoundary,
                out IDailyV1SelectedMerchantTradeStateBoundary merchantTradeBoundary))
            return false;
        actorBoundary.BindMerchantBoundary(merchantTradeBoundary);

        context = new CoreSelectedActorTurnExecutionContext(
            executionState.SelectedRuntimeActorRoster,
            graph.PersonStore,
            executionState.SelectedActorChoiceStore,
            coreRoots,
            actorBoundary,
            merchantTradeBoundary);
        return true;
    }

    /// <summary>
    /// Pre-publication admission for the bounded detached Core-only actor
    /// envelope. It binds the exact Crime/Justice mutation identity from the
    /// sealed Core census, then performs the same owner/action checks as the
    /// daily-stage fallback without requiring the advance scope that only
    /// exists after the application has published the session.
    /// </summary>
    internal static bool TryValidateCoreOnlyStart(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out string diagnostic) =>
        TryValidateCoreOnlyStart(
            executionState, graph,
            allowRetainedPriorDayBuyGoodsActionSlot: false,
            out diagnostic);

    internal static bool TryValidateCoreOnlyStart(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        bool allowRetainedPriorDayBuyGoodsActionSlot,
        out string diagnostic) =>
        TryValidateCoreOnlyStart(
            executionState, graph, allowRetainedPriorDayBuyGoodsActionSlot,
            allowRetainedPriorDayTradeTravelActionSlot: false, out diagnostic);

    internal static bool TryValidateCoreOnlyStart(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        bool allowRetainedPriorDayBuyGoodsActionSlot,
        bool allowRetainedPriorDayTradeTravelActionSlot,
        out string diagnostic)
    {
        diagnostic = null;
        if (executionState == null || graph == null)
        {
            diagnostic = "The prepared selected profile is outside the detached Core-only actor-turn envelope.";
            return false;
        }
        if (!executionState.TryBindSelectedCrimeJusticeDailyMutationBindingFromCoreCensus(graph))
        {
            diagnostic = "The prepared selected profile is outside the detached Core-only actor-turn envelope: "
                + "the exact Crime/Justice receipt binding could not be established from the sealed census.";
            return false;
        }
        if (!CanExecuteCoreOnlyActorTurn(
                executionState, graph, requireActiveDailyAdvanceScope: false,
                out string actorTurnDiagnostic,
                allowRetainedPriorDayBuyGoodsActionSlot,
                allowRetainedPriorDayTradeTravelActionSlot))
        {
            diagnostic = "The prepared selected profile is outside the detached Core-only actor-turn envelope: "
                + actorTurnDiagnostic;
            return false;
        }
        if (!executionState.TryBindSelectedCoreLocalKnowledgeMutationBoundaries(graph))
        {
            diagnostic = "The selected Core local-knowledge owner boundaries could not bind to the exact sealed census.";
            return false;
        }
        return true;
    }

    private static bool CanExecuteCoreOnlyActorTurn(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        bool requireActiveDailyAdvanceScope,
        out string diagnostic,
        bool allowRetainedPriorDayBuyGoodsActionSlot = false,
        bool allowRetainedPriorDayTradeTravelActionSlot = false)
    {
        diagnostic = null;
        if (executionState == null || graph == null
            || !executionState.HasAdmittedSelectedDailyExecution
            || !executionState.SelectedRuntimeExecutionRootsBound
            || !executionState.HasRuntimeProtocolOperationInventory
            || executionState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph))
        {
            diagnostic = "the selected Core runtime, execution roots, protocol inventory, or owner graph is not admitted";
            return false;
        }
        if (!executionState.HasSealedSelectedOwnerSectionCensusRegistry
            || executionState.IsSelectedRuntimeCensusProtocolFaulted
            || (requireActiveDailyAdvanceScope
                && !executionState.IsSelectedDailyAdvanceCoreTurnActive)
            || !executionState.TryValidateCurrentSelectedOwnerSectionCensusBaselines())
        {
            diagnostic = "the complete selected-owner census is not sealed, current, and inside the required daily scope";
            return false;
        }
        if (executionState.SelectedRuntimeActorRoster == null
            || executionState.SelectedActorChoiceStore == null
            || graph.PersonStore == null
            || !ReferenceEquals(executionState.SelectedActorChoiceStore.PersonStore, graph.PersonStore)
            || graph.ScheduledDirectives == null)
        {
            diagnostic = "the actor roster, Person store, or directive store is unsupported";
            return false;
        }
        if (graph.Definitions?.EffectiveConfiguration == null
            || !DailyV1SelectedNpcActionDecisionStage.TryResolveConfiguredActions(
                graph.Definitions, out IReadOnlyList<DailyV1ActionDefinition> actions)
            || actions == null || actions.Count == 0)
        {
            diagnostic = "the admitted effective definitions contain no resolvable configured actions";
            return false;
        }
        if (!DailyV1ActorTurnExecutionStage.TryBuildRoster(
                graph, executionState.SelectedRuntimeActorRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterEntry> roster))
        {
            diagnostic = "the exact ordered runtime roster cannot be resolved to Core NPC owners";
            return false;
        }

        if (!TryGetCoreOnlyForcedEscapeActorIds(
                graph, out HashSet<string> forcedEscapeActorIds))
        {
            diagnostic = "the scheduled directive set contains an unsupported or unresolved force-outcome";
            return false;
        }
        if (!CanExecuteCoreOnlyCrimeJustice(executionState, graph, forcedEscapeActorIds))
        {
            diagnostic = "the exact Core Crime/Justice owner binding or its selected facts are unsupported";
            return false;
        }

        foreach (DailyV1ActorTurnRosterProjection projection
            in executionState.SelectedRuntimeActorRoster)
        {
            // Null roster positions remain valid ordering holes. A populated
            // Unity-only member is not admitted to this host-neutral slice.
            if (projection == null) continue;
            if (string.IsNullOrWhiteSpace(projection.RuntimeId)
                || !graph.TryGetNpcOwner(projection.RuntimeId,
                    out DailyV1NpcRuntimeOwners exactOwner)
                || exactOwner == null)
            {
                diagnostic = "a populated runtime roster position does not bind to its exact Core NPC owner";
                return false;
            }
        }

        List<DailyV1NpcRuntimeOwners> rosterOwners = new List<DailyV1NpcRuntimeOwners>();
        foreach (DailyV1ActorTurnRosterEntry entry in roster)
            if (entry?.ActorOwner != null)
                rosterOwners.Add(entry.ActorOwner);
        if (!graph.MatchesExactOwnerInstances(graph.Cities, rosterOwners))
        {
            diagnostic = "the Core owner graph does not match the exact city and roster owner instances";
            return false;
        }

        foreach (DailyV1ActionDefinition action in actions)
        {
            int actionType = action?.Values.actionType?.Value ?? int.MinValue;
            bool admittedAutonomousAction = actionType == DailyV1NpcDefinitionRules.NormalActionTypeValue
                || actionType == DailyV1NpcDefinitionRules.HideActionTypeValue
                || actionType == DailyV1NpcDefinitionRules.ArrestActionTypeValue
                || actionType == DailyV1NpcDefinitionRules.FleeCityActionTypeValue
                || actionType == DailyV1NpcDefinitionRules.StealActionTypeValue
                || actionType == DailyV1NpcDefinitionRules.TravelActionTypeValue
                || actionType == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue;
            bool admittedCoreActorChoiceAction = actionType
                == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
                && DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                    graph.Definitions, action);
            bool admittedCoreAutonomousMarketPurchase = actionType
                == DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
                && DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                    graph.Definitions, action)
                && DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                    graph.Definitions, action);
            bool actionStatusEffectsSupported = action != null
                && DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                    graph.Definitions, action, out _, out _,
                    out IReadOnlyList<DailyV1StatusDefinition> actionStatusToRemove,
                    out IReadOnlyList<DailyV1StatusDefinition> actionStatusToAdd)
                && actionStatusToRemove.Count == 0
                && actionStatusToAdd.Count == 0;
            if (action == null
                || !graph.Definitions.Actions.TryGetValue(
                    action.DefinitionId, out DailyV1ActionDefinition admittedAction)
                || !ReferenceEquals(admittedAction, action)
                || (!admittedAutonomousAction && !admittedCoreActorChoiceAction && !admittedCoreAutonomousMarketPurchase)
                || (admittedAutonomousAction
                    && !DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                        graph.Definitions, action)) || !actionStatusEffectsSupported)
            {
                diagnostic = "a configured action is outside the autonomous or bounded Core actor-choice execution set: " + (action?.DefinitionId ?? "<null>") + " / type=" + actionType + " / autonomous=" + admittedAutonomousAction + " / choice=" + admittedCoreActorChoiceAction + " / purchase=" + admittedCoreAutonomousMarketPurchase + " / actionStatusEffects=" + actionStatusEffectsSupported;
                return false;
            }
        }

        foreach (DailyV1NpcRuntimeOwners actor in graph.Npcs)
        {
            if (actor?.ActionSlot == null)
            {
                diagnostic = "a Core NPC owner has no authoritative action slot";
                return false;
            }
            DailyV1ActionDefinition installed = actor.ActionSlot.CurrentAction;
            if (installed == null)
            {
                if (actor.ActionSlot.CurrentPayload != null)
                {
                    diagnostic = "an empty action slot retains a stale payload";
                    return false;
                }
                continue;
            }
            if (DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalAction(
                    graph, actor, installed)
                || IsProjectionIndependentCoreHideAction(graph, actor, installed)
                || IsProjectionIndependentCoreGuardArrestAction(graph, actor, installed)
                || IsProjectionIndependentCoreFleeCityAction(graph, actor, installed)
                || IsProjectionIndependentCoreStealAction(graph, actor, installed)
                || DailyV1CoreSelectedActorTurnExecutionBoundary
                    .IsExactCoreOnlySoloTravelActionSlot(
                        graph, actor, installed, actor.ActionSlot.CurrentPayload,
                        requireInstalledSlot: true)
                || IsProjectionIndependentCoreLocalSellGoodsActionSlot(
                    executionState, graph, actor, installed)
                || IsProjectionIndependentCoreLocalBuyGoodsActionSlot(
                    executionState, graph, actor, installed)
                // The saved current-action slot is prior intent, not a command to execute again.
                // Daily actor processing selects or clears the current action before dispatch;
                // exact target identity is still required, and execution still checks locality.
                || (allowRetainedPriorDayBuyGoodsActionSlot
                    && !string.Equals(
                        actor.ActionSlot.CurrentPayload?.TargetCityRuntimeId,
                        actor.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal)
                    && IsProjectionIndependentCoreLocalBuyGoodsActionSlot(
                        executionState, graph, actor, installed,
                        requireCurrentCityMatch: false))
                || (allowRetainedPriorDayTradeTravelActionSlot
                    && IsProjectionIndependentRetainedCoreMerchantTravelActionSlot(
                        graph, actor, installed)))
                continue;
            diagnostic = "an installed actor action is not supported by the host-neutral execution boundary: "
                + actor.RuntimeId + " / " + installed.DefinitionId;
            return false;
        }

        diagnostic = null;
        return true;
    }

    private static bool IsProjectionIndependentRetainedCoreMerchantTravelActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action)
    {
        DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
        DailyV1NpcTravelOwner travel = actor?.Travel;
        DailyV1NpcTravelPlanOwner travelPlan = actor?.TravelPlan;
        DailyV1NpcMerchantTradePlanOwner merchant = actor?.MerchantTradePlan;
        if (graph == null || actor == null || action == null || payload == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || !ReferenceEquals(actor.ActionSlot?.CurrentAction, action)
            || !ReferenceEquals(payload.Action, action)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.TravelActionTypeValue
            || !graph.Definitions.Actions.TryGetValue(
                action.DefinitionId, out DailyV1ActionDefinition exactAction)
            || !ReferenceEquals(exactAction, action)
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || !DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, action, actor.Status?.Statuses)
            || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || travel == null || travelPlan == null
            || !travelPlan.Matches(null, null, NpcTravelReason.None, 0f, 0f, null)
            || merchant?.IsActive != true
            || !ReferenceEquals(merchant.Definitions, graph.Definitions)
            || merchant.ItemDefinition == null
            || !graph.Definitions.Items.TryGetValue(
                merchant.ItemDefinition.DefinitionId,
                out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(admittedItem, merchant.ItemDefinition)
            || payload.TravelReason != NpcTravelReason.Trade
            || payload.TargetItem != null
            || !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            || payload.Amount != 0
            || float.IsNaN(payload.ExpectedUnitPrice)
            || float.IsInfinity(payload.ExpectedUnitPrice)
            || payload.ExpectedUnitPrice < 0f
            || !payload.SuccessChanceMultiplier.Equals(1f)
            || payload.ExpectedNetValue != 0f
            || payload.CommercialDecisionEvidence != null
            || payload.CommercialScoutingEvidence != null
            || string.IsNullOrWhiteSpace(payload.TargetCityRuntimeId)
            || !string.Equals(payload.TargetCityRuntimeId,
                merchant.TargetCityRuntimeId, StringComparison.Ordinal)
            || !string.Equals(payload.OriginDecisionId,
                merchant.OriginDecisionId, StringComparison.Ordinal)
            || !graph.TryGetCityLocationRuntimeId(
                payload.TargetCityRuntimeId, out string destinationLocationRuntimeId)
            || graph.TravelParties == null
            || graph.IsManagedByTravelParty(actor, graph.TravelParties))
            return false;

        if (travel.IsTraveling)
            return string.Equals(travel.DestinationCityRuntimeId,
                    payload.TargetCityRuntimeId, StringComparison.Ordinal)
                && string.Equals(travel.DestinationLocationRuntimeId,
                    destinationLocationRuntimeId, StringComparison.Ordinal)
                && travel.TravelDaysRemaining > 0
                && travel.TravelDaysTotal >= travel.TravelDaysRemaining
                && string.Equals(travel.TravelOriginDecisionId,
                    payload.OriginDecisionId, StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(travel.ActiveTravelPartyId);

        return string.Equals(travel.CurrentCityRuntimeId,
                payload.TargetCityRuntimeId, StringComparison.Ordinal)
            && string.Equals(travel.CurrentLocationRuntimeId,
                destinationLocationRuntimeId, StringComparison.Ordinal);
    }

    private static bool IsProjectionIndependentCoreLocalSellGoodsActionSlot(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action)
    {
        if (executionState == null || actor == null || action == null
            || !DailyV1CoreSelectedActorTurnExecutionBoundary.IsExactCoreLocalSellGoodsActionSlot(
                graph, actor, action))
            return false;

        DailyV1NpcActionPayloadOwner payload = actor.ActionSlot.CurrentPayload;
        if (string.IsNullOrWhiteSpace(payload?.OriginDecisionId)
            || executionState.SelectedActorChoiceStore == null)
            return false;

        bool decisionRecordExists = false;
        foreach (NpcDecisionRecord decision in executionState.SelectedDecisionStore?.Decisions
            ?? Array.Empty<NpcDecisionRecord>())
        {
            if (decision == null
                || !string.Equals(decision.DecisionId, payload.OriginDecisionId, StringComparison.Ordinal))
                continue;
            decisionRecordExists = true;
            if (!string.Equals(decision.ActorRuntimeId, actor.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(decision.ActionDefinitionId, action.DefinitionId, StringComparison.Ordinal))
                continue;

            // Autonomous merchant SellGoods is an admitted Daily-v1 action. Its
            // durable decision record is sufficient to validate the installed
            // slot; it does not imply or synthesize a P11 actor choice.
            if (decision.Origin == NpcDecisionOrigin.Autonomous)
                return true;
            if (decision.Origin != NpcDecisionOrigin.ActorChoice)
                continue;

            PersonId rosterPersonId = null;
            foreach (DailyV1ActorTurnRosterProjection projection
                in executionState.SelectedRuntimeActorRoster ?? Array.Empty<DailyV1ActorTurnRosterProjection>())
            {
                if (projection == null
                    || !string.Equals(projection.RuntimeId, actor.RuntimeId, StringComparison.Ordinal))
                    continue;
                if (rosterPersonId != null || projection.PersonId == null)
                {
                    rosterPersonId = null;
                    break;
                }
                rosterPersonId = projection.PersonId;
            }
            if (rosterPersonId == null) continue;

            foreach (ActorChoiceInput input in executionState.SelectedActorChoiceStore.Inputs)
            {
                if (input == null || input.Status != ActorChoiceInputStatus.AttemptReturned
                || input.PersonId == null || !input.PersonId.Equals(rosterPersonId)
                || !string.Equals(input.ActionDefinitionId, action.DefinitionId, StringComparison.Ordinal)
                || input.Origin != WorldCommandOrigin.LocalPlayer
                || input.Authority != WorldCommandAuthorityMode.Request
                || input.TemporalCapture != null
                || input.Dispositions == null || input.Dispositions.Count != 2)
                    continue;

                ActorChoiceDisposition dispatch = input.Dispositions[0];
                ActorChoiceDisposition returned = input.Dispositions[1];
                if (dispatch == null || returned == null
                    || dispatch.Kind != ActorChoiceDispositionKind.DispatchStarted
                    || returned.Kind != ActorChoiceDispositionKind.AttemptReturned
                    || !string.Equals(dispatch.DecisionRecordId,
                        payload.OriginDecisionId, StringComparison.Ordinal))
                    continue;

                return true;
            }
        }

        if (decisionRecordExists) return false;

        // P12 retains the exact current-action payload and its origin identity,
        // but this selected profile intentionally omits the full decision
        // store. The durable choice store still distinguishes a prior player
        // dispatch from an autonomous SellGoods slot; never infer autonomy
        // when that identity is present.
        foreach (ActorChoiceInput input in executionState.SelectedActorChoiceStore.Inputs)
        {
            if (input?.Dispositions == null) continue;
            foreach (ActorChoiceDisposition disposition in input.Dispositions)
                if (disposition != null
                    && string.Equals(disposition.DecisionRecordId,
                        payload.OriginDecisionId, StringComparison.Ordinal))
                    return false;
        }
        return true;
    }

    private static bool IsProjectionIndependentCoreLocalBuyGoodsActionSlot(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        bool requireCurrentCityMatch = true)
    {
        if (executionState?.SelectedDecisionStore == null || actor == null || action == null
            || !DailyV1CoreSelectedActorTurnExecutionBoundary.IsExactCoreLocalBuyGoodsActionSlot(
                graph, actor, action, requireCurrentCityMatch))
            return false;

        string decisionId = actor.ActionSlot.CurrentPayload?.OriginDecisionId;
        if (string.IsNullOrWhiteSpace(decisionId)) return false;
        foreach (NpcDecisionRecord decision in executionState.SelectedDecisionStore.Decisions)
            if (decision != null
                && string.Equals(decision.DecisionId, decisionId, StringComparison.Ordinal))
                return string.Equals(decision.ActorRuntimeId, actor.RuntimeId, StringComparison.Ordinal)
                    && string.Equals(decision.ActionDefinitionId, action.DefinitionId, StringComparison.Ordinal)
                    && decision.Origin == NpcDecisionOrigin.Autonomous;

        // P12 persists the exact current-action payload and its opaque
        // OriginDecisionId, but this accepted profile intentionally does not
        // persist the full NpcDecisionStore. A detached restore therefore may
        // retain a valid autonomous BuyGoods slot whose decision record is no
        // longer available. The persisted ActorChoiceStore is authoritative
        // for the relevant negative case: never reinterpret a retained
        // actor-choice dispatch as autonomous just because its decision
        // record was omitted.
        ActorChoiceStore actorChoices = executionState.SelectedActorChoiceStore;
        if (actorChoices == null) return false;
        foreach (ActorChoiceInput input in actorChoices.Inputs)
        {
            if (input?.Dispositions == null) continue;
            foreach (ActorChoiceDisposition disposition in input.Dispositions)
                if (disposition != null
                    && string.Equals(disposition.DecisionRecordId, decisionId,
                        StringComparison.Ordinal))
                    return false;
        }

        return true;
    }

    private static bool IsProjectionIndependentCoreHideAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action)
    {
        DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
        return DailyV1CoreOnlyCrimeActionBoundary.IsExactTargetlessHideSlot(
            graph, actor, action, payload)
            && DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                graph.Definitions, action,
                out _, out _,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd)
            && targetStatusToRemove.Count == 0
            && targetStatusToAdd.Count == 0;
    }

    private static bool IsProjectionIndependentCoreGuardArrestAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action)
    {
        DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
        return DailyV1CoreOnlyCrimeActionBoundary.IsExactGuardArrestActionSlot(
                graph, actor, action, payload)
            && !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            && graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                out DailyV1NpcRuntimeOwners target)
            && target != null
            && !ReferenceEquals(target, actor);
    }

    private static bool IsProjectionIndependentCoreStealAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action)
    {
        DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
        return DailyV1CoreOnlyCrimeActionBoundary.IsExactStealActionSlot(
                graph, actor, action, payload)
            && !string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
            && graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                out DailyV1NpcRuntimeOwners target)
            && target != null
            && !ReferenceEquals(target, actor);
    }

    private static bool IsProjectionIndependentCoreFleeCityAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action)
    {
        DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
        return DailyV1CoreOnlyCrimeActionBoundary.IsExactFleeCityActionSlot(
                graph, actor, action, payload)
            && DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                graph.Definitions, action,
                out _, out _,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd)
            && targetStatusToRemove.Count == 0
            && targetStatusToAdd.Count == 0;
    }

    private static bool CanExecuteCoreOnlyCrimeJustice(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        HashSet<string> forcedEscapeActorIds)
    {
        DailyV1JusticeRecordsOwner justice = graph.JusticeRecordsOwner;
        if (justice == null || forcedEscapeActorIds == null
            || !executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding)
            || binding == null || !binding.AdmissionEnabled
            || binding.CrimeOwnerIdentity == null || binding.JusticeOwnerIdentity == null
            || !ReferenceEquals(binding.CrimeOwnerIdentity, binding.CrimeReceiptCensusOwnerIdentity)
            || !ReferenceEquals(binding.JusticeOwnerIdentity, binding.JusticeReceiptCensusOwnerIdentity)
            || !binding.HasExactCoreOwnerRelationships(graph)
            || !TryResolveConfiguredStatus(graph.Definitions,
                graph.Definitions.Configuration?.hiddenStatus, out _)
            || !TryResolveConfiguredStatus(graph.Definitions,
                graph.Definitions.Configuration?.arrestedStatus,
                out DailyV1StatusDefinition arrestedStatus)
            || !TryResolveConfiguredStatus(graph.Definitions,
                graph.Definitions.Configuration?.freeStatus, out _)
            || !TryResolveConfiguredStatus(graph.Definitions,
                graph.Definitions.Configuration?.wantedStatus, out _))
            return false;

        HashSet<string> activeSentenceTargets = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1JusticeSentenceOwner sentence in justice.Sentences)
        {
            if (sentence == null || !forcedEscapeActorIds.Contains(sentence.TargetRuntimeId)
                || !graph.TryGetNpcOwner(sentence.TargetRuntimeId,
                    out DailyV1NpcRuntimeOwners sentenceActor)
                || sentenceActor == null
                || sentence.Warrant == null
                || !justice.ContainsWantedRecord(sentence.Warrant)
                || !string.Equals(sentence.Warrant.TargetRuntimeId,
                    sentence.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(sentence.Warrant.CityRuntimeId,
                    sentence.CityRuntimeId, StringComparison.Ordinal)
                || !graph.Cities.Any(city => city != null
                    && graph.ContainsExactCityOwner(city)
                    && string.Equals(city.RuntimeId, sentence.CityRuntimeId,
                        StringComparison.Ordinal)))
                return false;

            if (sentence.IsActive
                && (!activeSentenceTargets.Add(sentence.TargetRuntimeId)
                    || sentence.Warrant.IsResolved))
                return false;
        }

        foreach (DailyV1NpcRuntimeOwners actor in graph.Npcs)
        {
            if (actor?.Status == null || actor.HiddenState == null
                || (arrestedStatus != null && actor.Status.Contains(arrestedStatus)
                    && !forcedEscapeActorIds.Contains(actor.RuntimeId)))
                return false;
        }
        return true;
    }

    private static bool TryGetCoreOnlyForcedEscapeActorIds(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out HashSet<string> actorRuntimeIds)
    {
        actorRuntimeIds = null;
        if (graph?.ScheduledDirectives == null || graph.Definitions?.Actions == null)
            return false;

        HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (ScheduledDirective directive in graph.ScheduledDirectives.Directives)
        {
            if (directive == null) return false;
            if (!directive.IsPending) continue;
            if (directive.Mode != ScheduledDirectiveMode.ForceOutcome
                || directive.Operation != ScheduledDirectiveOperation.EscapePrison
                || string.IsNullOrWhiteSpace(directive.ActorRuntimeId)
                || !graph.TryGetNpcOwner(directive.ActorRuntimeId,
                    out DailyV1NpcRuntimeOwners actor)
                || actor == null
                || !graph.Definitions.Actions.TryGetValue(
                    directive.ActionDefinitionId, out DailyV1ActionDefinition action)
                || action == null
                || !string.Equals(action.DefinitionId, directive.ActionDefinitionId,
                    StringComparison.Ordinal)
                || action.Values.actionType?.Value
                    != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
                || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
                || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                    graph.Definitions, action, out _, out _, out _, out _))
                return false;
            selected.Add(directive.ActorRuntimeId);
        }

        actorRuntimeIds = selected;
        return true;
    }

    private static bool TryResolveConfiguredStatus(
        DailyV1EffectiveDefinitions definitions,
        string statusId,
        out DailyV1StatusDefinition status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(statusId)) return true;
        return definitions?.Statuses != null
            && definitions.Statuses.TryGetValue(statusId, out status)
            && status != null
            && string.Equals(status.DefinitionId, statusId, StringComparison.Ordinal);
    }

    private static bool TryCreateCoreMerchantBoundary(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1ActorTurnCoreRoots coreRoots,
        IDailyV1ActorTurnExecutionBoundary actorBoundary,
        out IDailyV1SelectedMerchantTradeStateBoundary merchantBoundary)
    {
        merchantBoundary = null;
        if (graph?.Definitions?.EffectiveConfiguration?.MerchantTrade?.Enabled != true)
            return true;
        if (!(actorBoundary is IDailyV1SelectedMerchantTradeStateP12HostPorts hostPorts)
            || !DailyV1CoreSelectedMerchantTradeStateBoundary.TryCreate(
                graph, coreRoots, hostPorts,
                out DailyV1CoreSelectedMerchantTradeStateBoundary coreBoundary))
            return false;
        merchantBoundary = coreBoundary;
        return true;
    }

    private sealed class CoreSelectedActorTurnExecutionContext
        : IDailyV1SelectedActorTurnExecutionContext
    {
        internal CoreSelectedActorTurnExecutionContext(
            IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster,
            PersonStore personStore,
            ActorChoiceStore actorChoiceStore,
            DailyV1ActorTurnCoreRoots coreRoots,
            IDailyV1ActorTurnExecutionBoundary boundary,
            IDailyV1SelectedMerchantTradeStateBoundary merchantTradeBoundary)
        {
            RuntimeRoster = runtimeRoster ?? throw new ArgumentNullException(nameof(runtimeRoster));
            PersonStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
            ActorChoiceStore = actorChoiceStore ?? throw new ArgumentNullException(nameof(actorChoiceStore));
            CoreRoots = coreRoots ?? throw new ArgumentNullException(nameof(coreRoots));
            Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            MerchantTradeBoundary = merchantTradeBoundary;
        }

        public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
        public PersonStore PersonStore { get; }
        public ActorChoiceStore ActorChoiceStore { get; }
        public DailyV1ActorTurnCoreRoots CoreRoots { get; }
        public IDailyV1ActorTurnExecutionBoundary Boundary { get; }
        public IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
    }
}

/// <summary>
/// Host P12/projection ports for the existing Core-selected travel operation.
/// Core takes the party store and managed membership from its admitted graph,
/// invokes the travel algorithm, and owns party-before-solo ordering.
/// </summary>
internal interface IDailyV1SelectedTravelDayExecutionContext
{
    IDailyV1SelectedTravelDayBoundary Boundary { get; }
    IDailyV1SelectedP12TravelPartyAdvanceProtocol P12Protocol { get; }
    bool AdvanceSoloTravel { get; }
    /// <summary>
    /// Legacy compatibility wrapper used only by non-admitted selected-stage
    /// callers. An admitted session owns this scope through its execution
    /// state and never delegates operation ownership to the host.
    /// </summary>
    bool ExecuteWithinPartyOwnerOperation(Func<bool> operation);
    void PublishCompletedTravelResults(
        IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults);
    void ObserveCompletedTravelResults();
}

/// <summary>
/// Owns the order of the selected Daily-v1 runtime phases. Exact-empty
/// place-content and autonomous-expedition capabilities are not invoked for
/// this admitted profile; Core-owned graph and operation stages remain in Core.
/// </summary>
internal static class DailyV1SelectedDailyExecutionStage
{
    private sealed class CoreSelectedDayStartExecutionContext
        : IDailyV1SelectedDayStartExecutionContext
    {
        internal CoreSelectedDayStartExecutionContext(
            IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster,
            IDailyV1SelectedCrimeJusticeDailyBoundary crimeJusticeBoundary,
            IDailyV1SelectedMerchantTradeStateBoundary merchantTradeBoundary)
        {
            RuntimeRoster = runtimeRoster
                ?? throw new ArgumentNullException(nameof(runtimeRoster));
            CrimeJusticeBoundary = crimeJusticeBoundary
                ?? throw new ArgumentNullException(nameof(crimeJusticeBoundary));
            // The Merchant adapter is optional only when the admitted
            // configuration disables MerchantTrade. PrepareCore and
            // AdvanceCore enforce that relationship before a daily write.
            MerchantTradeBoundary = merchantTradeBoundary;
        }

        public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
        public bool HasUnselectedLiveRuntimeMember => false;
        public bool CrimeSystemPresent => true;
        public bool JusticeSystemPresent => true;
        public IDailyV1SelectedCrimeJusticeDailyBoundary CrimeJusticeBoundary { get; }
        public IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
    }

    internal sealed class PreparedDay
    {
        internal readonly DailyV1ExecutionBoundaryState executionState;
        internal readonly DailyV1SelectedRuntimeOwnerGraph graph;
        internal readonly ScheduledDirectiveStore scheduledDirectives;
        internal readonly IDailyV1SelectedDailyExecutionBoundary boundary;
        internal readonly long absoluteDay;
        internal readonly long clockDayAtPreparation;
        internal readonly bool clockMustAdvance;
        internal readonly EffectiveSimulationConfiguration selectedConfiguration;
        internal readonly IDailyV1SelectedActorTurnExecutionContext actorTurnContext;
        internal readonly IReadOnlyList<DailyV1ActorTurnRosterEntry> actorRoster;
        internal readonly bool coreSelectedCompatibility;
        internal readonly IDailyV1SelectedDayStartExecutionContext dayStartContext;
        internal readonly bool hasSelectedDayStartContext;
        internal readonly bool dayStartContextPreflighted;
        internal readonly bool admittedSelectedBoundariesPreflighted;
        internal readonly IDailyV1SelectedTravelDayExecutionContext travelContext;
        internal readonly bool travelContextPreflighted;
        internal readonly IDailyV1SelectedDemographyExecutionContext demographyContext;
        internal readonly bool demographyContextPreflighted;
        internal readonly bool selectedEconomyPreflighted;
        internal readonly bool selectedLocalKnowledgePreflighted;
        internal bool consumed;

        internal PreparedDay(
            DailyV1ExecutionBoundaryState executionState,
            long absoluteDay,
            long clockDayAtPreparation,
            bool clockMustAdvance,
            DailyV1SelectedRuntimeOwnerGraph graph,
            ScheduledDirectiveStore scheduledDirectives,
            IDailyV1SelectedDailyExecutionBoundary boundary,
            EffectiveSimulationConfiguration selectedConfiguration,
            IDailyV1SelectedActorTurnExecutionContext actorTurnContext,
            IReadOnlyList<DailyV1ActorTurnRosterEntry> actorRoster,
            bool coreSelectedCompatibility,
            IDailyV1SelectedDayStartExecutionContext dayStartContext,
            bool hasSelectedDayStartContext,
            bool dayStartContextPreflighted,
            bool admittedSelectedBoundariesPreflighted,
            IDailyV1SelectedTravelDayExecutionContext travelContext,
            bool travelContextPreflighted,
            IDailyV1SelectedDemographyExecutionContext demographyContext,
            bool demographyContextPreflighted,
            bool selectedEconomyPreflighted,
            bool selectedLocalKnowledgePreflighted)
        {
            this.executionState = executionState;
            this.absoluteDay = absoluteDay;
            this.clockDayAtPreparation = clockDayAtPreparation;
            this.clockMustAdvance = clockMustAdvance;
            this.graph = graph;
            this.scheduledDirectives = scheduledDirectives;
            this.boundary = boundary;
            this.selectedConfiguration = selectedConfiguration;
            this.actorTurnContext = actorTurnContext;
            this.actorRoster = actorRoster;
            this.coreSelectedCompatibility = coreSelectedCompatibility;
            this.dayStartContext = dayStartContext;
            this.hasSelectedDayStartContext = hasSelectedDayStartContext;
            this.dayStartContextPreflighted = dayStartContextPreflighted;
            this.admittedSelectedBoundariesPreflighted = admittedSelectedBoundariesPreflighted;
            this.travelContext = travelContext;
            this.travelContextPreflighted = travelContextPreflighted;
            this.demographyContext = demographyContext;
            this.demographyContextPreflighted = demographyContextPreflighted;
            this.selectedEconomyPreflighted = selectedEconomyPreflighted;
            this.selectedLocalKnowledgePreflighted = selectedLocalKnowledgePreflighted;
        }
    }

    internal static void Advance(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        if (executionState == null) throw new ArgumentNullException(nameof(executionState));
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (!executionState.SelectedRuntimeExecutionRootsBound
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph))
            throw new InvalidOperationException(
                "The selected Daily-v1 stage requires the exact runtime execution roots bound to its Core state.");
        SimulationTime simulationTime = executionState.SimulationTime;
        if (simulationTime == null)
            throw new InvalidOperationException(
                "The selected Daily-v1 stage requires the clock bound to its Core execution state.");

        Advance(PrepareCore(executionState, simulationTime.AbsoluteDay,
            simulationTime.AbsoluteDay, clockMustAdvance: false,
            graph, graph.ScheduledDirectives, boundary));
    }

    internal static void Advance(
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph,
        ScheduledDirectiveStore scheduledDirectives,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        Advance(PrepareCore(null, absoluteDay, absoluteDay, clockMustAdvance: false,
            graph, scheduledDirectives, boundary));
    }

    internal static PreparedDay PrepareForNextDay(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        if (executionState == null) throw new ArgumentNullException(nameof(executionState));
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        SimulationTime clock = executionState.SimulationTime;
        if (clock == null)
            throw new InvalidOperationException(
                "The selected Daily-v1 stage requires the clock bound to its Core execution state.");
        if (clock.AbsoluteDay == long.MaxValue)
            throw new InvalidOperationException(
                "The selected Daily-v1 stage cannot prepare a day beyond the clock limit.");

        return PrepareCore(executionState, clock.AbsoluteDay + 1L,
            clock.AbsoluteDay, clockMustAdvance: true,
            graph, graph.ScheduledDirectives, boundary);
    }

    internal static void Advance(PreparedDay preparedDay)
    {
        if (preparedDay == null) throw new ArgumentNullException(nameof(preparedDay));
        if (preparedDay.consumed)
            throw new InvalidOperationException("A prepared Daily-v1 day can be executed only once.");

        long expectedClockDay = preparedDay.clockMustAdvance
            ? preparedDay.clockDayAtPreparation + 1L
            : preparedDay.clockDayAtPreparation;
        if (preparedDay.clockMustAdvance
            && (preparedDay.clockDayAtPreparation == long.MaxValue
                || preparedDay.absoluteDay != expectedClockDay))
            throw new InvalidOperationException("The prepared Daily-v1 day has an invalid clock boundary.");

        DailyV1ExecutionBoundaryState executionState = preparedDay.executionState;
        if (executionState != null
            && (executionState.SimulationTime == null
                || executionState.SimulationTime.AbsoluteDay != expectedClockDay
                || !executionState.SelectedRuntimeExecutionRootsBound
                || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, preparedDay.graph)
                || !ReferenceEquals(preparedDay.graph.ScheduledDirectives, preparedDay.scheduledDirectives)
                || !ReferenceEquals(preparedDay.graph.Definitions?.EffectiveConfiguration,
                    preparedDay.selectedConfiguration)))
            throw new InvalidOperationException(
                "The prepared Daily-v1 day no longer matches its exact clock, definitions, or owner graph.");

        if (executionState != null)
        {
            string rootFailure = null;
            if (preparedDay.actorTurnContext == null
                || !HasExactSelectedExecutionRoots(
                    executionState, preparedDay.graph, preparedDay.actorTurnContext,
                    preparedDay.actorRoster, out rootFailure))
                throw new InvalidOperationException(
                    "The prepared Daily-v1 actor-turn roots changed before execution: "
                    + (rootFailure ?? "actor-turn context is unavailable") + ".");
        }

        preparedDay.consumed = true;
        AdvanceCore(preparedDay);
    }

    private static PreparedDay PrepareCore(
        DailyV1ExecutionBoundaryState executionState,
        long absoluteDay,
        long clockDayAtPreparation,
        bool clockMustAdvance,
        DailyV1SelectedRuntimeOwnerGraph graph,
        ScheduledDirectiveStore scheduledDirectives,
        IDailyV1SelectedDailyExecutionBoundary boundary)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (boundary == null) throw new ArgumentNullException(nameof(boundary));
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));
        if (executionState != null
            && (executionState.SimulationTime == null
                || executionState.SimulationTime.AbsoluteDay != clockDayAtPreparation
                || (clockMustAdvance
                    ? clockDayAtPreparation == long.MaxValue
                        || absoluteDay != clockDayAtPreparation + 1L
                    : absoluteDay != clockDayAtPreparation)))
            throw new InvalidOperationException(
                "The selected Daily-v1 preparation does not match the exact bound clock boundary.");
        if (!ReferenceEquals(graph.ScheduledDirectives, scheduledDirectives))
            throw new InvalidOperationException(
                "The selected Daily-v1 stage requires the exact directive store bound to its Core owner graph.");

        EffectiveSimulationConfiguration selectedConfiguration =
            graph.Definitions?.EffectiveConfiguration;
        if (selectedConfiguration == null)
            throw new InvalidOperationException(
                "The selected Daily-v1 graph has no admitted effective configuration.");

        IDailyV1SelectedActorTurnExecutionContext actorTurnContext = null;
        IReadOnlyList<DailyV1ActorTurnRosterEntry> actorRoster = null;
        if (executionState != null)
        {
            string executionRootFailure = null;
            if (!DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
                    executionState, graph, boundary, out actorTurnContext))
                executionRootFailure = "Core could not bind its exact actor-turn roots to the host action boundary";
            else if (actorTurnContext == null)
                executionRootFailure = "host returned a null selected actor-turn context";
            else if (actorTurnContext.RuntimeRoster == null)
                executionRootFailure = "selected actor-turn runtime roster is null";
            else if (actorTurnContext.PersonStore == null)
                executionRootFailure = "selected actor-turn PersonStore is null";
            else if (!ReferenceEquals(graph.PersonStore, actorTurnContext.PersonStore))
                executionRootFailure = "selected actor-turn PersonStore differs from the owner graph";
            else if (actorTurnContext.ActorChoiceStore == null)
                executionRootFailure = "selected actor-turn ActorChoiceStore is null";
            else if (actorTurnContext.Boundary == null)
                executionRootFailure = "selected actor-turn action boundary is null";
            else if (!DailyV1ActorTurnExecutionStage.TryBuildRoster(
                         graph, actorTurnContext.RuntimeRoster, out actorRoster))
                executionRootFailure = "selected actor-turn roster does not match exact Core owners";
            else
                HasExactSelectedExecutionRoots(
                    executionState, graph, actorTurnContext, actorRoster,
                    out executionRootFailure);

            if (executionRootFailure != null)
                throw new InvalidOperationException(
                    "The selected Daily-v1 host could not prove the exact actor, choice, random, allocator, sequence, and decision roots before daily mutations: "
                    + executionRootFailure + ".");
        }

        // A fully selected admitted day must prove the required day-start
        // operation and projection ports before any daily callback or writer
        // can mutate state. A mixed roster is handled from Core's exact roster
        // below and retains the established Unity whole-stage compatibility.
        bool coreSelectedCompatibility = executionState != null
            && HasUnselectedRuntimeMemberFromCoreRoster(executionState, actorRoster);
        IDailyV1SelectedDayStartExecutionContext dayStartContext = null;
        bool hasSelectedDayStartContext = false;
        bool admittedSelectedBoundariesPreflighted =
            executionState?.HasAdmittedSelectedDailyExecution == true
            && !coreSelectedCompatibility;
        bool dayStartContextPreflighted = false;
        if (admittedSelectedBoundariesPreflighted)
        {
            EffectiveMerchantTradeConfiguration merchantTradeConfiguration =
                graph.Definitions.EffectiveConfiguration?.MerchantTrade;
            bool hasCrimeJusticeBoundary = actorTurnContext.Boundary
                is IDailyV1SelectedCrimeJusticeDailyBoundary;
            bool hasExactProjectionBindings = hasCrimeJusticeBoundary
                && boundary.HasExactCrimeJusticeProjectionOwnerBindings(graph);
            IDailyV1SelectedCrimeJusticeDailyBoundary crimeJusticeBoundary =
                actorTurnContext.Boundary as IDailyV1SelectedCrimeJusticeDailyBoundary;
            if (!hasCrimeJusticeBoundary || !hasExactProjectionBindings
                || !ReferenceEquals(graph.JusticeRecordsOwner,
                    crimeJusticeBoundary?.JusticeRecordsOwner)
                || merchantTradeConfiguration == null
                || (merchantTradeConfiguration.Enabled
                    && actorTurnContext.MerchantTradeBoundary == null))
                throw new InvalidOperationException(
                    "The admitted Daily-v1 actor-turn boundary could not provide the exact Core day-start owner, Unity projection bindings, and operation contracts.");

            // Reuse the one exact adapter object already bound to this Core
            // actor roster. Unity no longer supplies a parallel selected
            // roster, system-presence booleans, or a second daily boundary.
            dayStartContext = new CoreSelectedDayStartExecutionContext(
                executionState.SelectedRuntimeActorRoster,
                crimeJusticeBoundary,
                actorTurnContext.MerchantTradeBoundary);
            hasSelectedDayStartContext = true;
            dayStartContextPreflighted = true;
        }

        // The travel projection and P12 protocol are also required by an
        // admitted selected day. Acquire their exact owner bindings before
        // daily callbacks or writers; actual travel advancement remains at its
        // established position after actor turns.
        IDailyV1SelectedTravelDayExecutionContext travelContext = null;
        bool travelContextPreflighted = admittedSelectedBoundariesPreflighted;
        if (travelContextPreflighted
            && (!TryCreateSelectedTravelDayContext(
                    executionState, graph, boundary, out travelContext)
                || travelContext == null
                || travelContext.Boundary == null))
            throw new InvalidOperationException(
                "The selected Daily-v1 host could not provide its exact travel projection and P12 context.");

        IDailyV1SelectedDemographyExecutionContext demographyContext = null;
        bool demographyContextPreflighted =
            executionState?.HasAdmittedSelectedDailyExecution == true;
        if (demographyContextPreflighted
            && (selectedConfiguration.NaturalMortality.Enabled
                || selectedConfiguration.AggregateDemography.Enabled))
        {
            bool hasDemographyContext;
            if (executionState?.HasAdmittedSelectedDailyExecution == true)
            {
                hasDemographyContext = boundary.TryCreateSelectedDemographyHostPorts(
                        graph, out IDailyV1SelectedDemographyHostPorts hostPorts)
                    && DailyV1CoreSelectedDemographyExecutionContext.TryCreate(
                        executionState, graph, hostPorts, out demographyContext);
            }
            else
            {
                hasDemographyContext = boundary.TryCreateSelectedDemographyContext(
                    graph, out demographyContext);
            }
            if (!hasDemographyContext
                || demographyContext == null
                || graph.PersonStore == null
                || executionState.CalendarIdentity == null
                || demographyContext.Boundary == null)
                throw new InvalidOperationException(
                    "The selected Daily-v1 host could not provide the exact demographic owners and writer boundary.");
        }

        bool selectedEconomyPreflighted = false;
        bool selectedLocalKnowledgePreflighted = false;
        if (executionState?.HasAdmittedSelectedDailyExecution == true)
        {
            if (scheduledDirectives != null && graph.RuntimeIdentityIndex == null)
                throw new InvalidOperationException(
                    "Selected Daily-v1 directive preparation requires the Core runtime identity index.");
            if (graph.Definitions.EffectiveConfiguration?.MerchantTrade == null)
                throw new InvalidOperationException(
                    "The selected Daily-v1 actor-turn stage has no admitted Merchant configuration.");
            if (selectedConfiguration.Economy?.Enabled == true)
            {
                boundary.EnsureSelectedEconomyCanRun(graph);
                selectedEconomyPreflighted = true;
            }
            boundary.EnsureSelectedLocalKnowledgeCanRun(graph);
            selectedLocalKnowledgePreflighted = true;
        }

        return new PreparedDay(
            executionState,
            absoluteDay,
            clockDayAtPreparation,
            clockMustAdvance,
            graph,
            scheduledDirectives,
            boundary,
            selectedConfiguration,
            actorTurnContext,
            actorRoster,
            coreSelectedCompatibility,
            dayStartContext,
            hasSelectedDayStartContext,
            dayStartContextPreflighted,
            admittedSelectedBoundariesPreflighted,
            travelContext,
            travelContextPreflighted,
            demographyContext,
            demographyContextPreflighted,
            selectedEconomyPreflighted,
            selectedLocalKnowledgePreflighted);
    }

    private static void AdvanceCore(PreparedDay preparedDay)
    {
        DailyV1ExecutionBoundaryState executionState = preparedDay.executionState;
        long absoluteDay = preparedDay.absoluteDay;
        DailyV1SelectedRuntimeOwnerGraph graph = preparedDay.graph;
        ScheduledDirectiveStore scheduledDirectives = preparedDay.scheduledDirectives;
        IDailyV1SelectedDailyExecutionBoundary boundary = preparedDay.boundary;
        EffectiveSimulationConfiguration selectedConfiguration = preparedDay.selectedConfiguration;
        IDailyV1SelectedActorTurnExecutionContext actorTurnContext = preparedDay.actorTurnContext;
        IReadOnlyList<DailyV1ActorTurnRosterEntry> actorRoster = preparedDay.actorRoster;
        bool coreSelectedCompatibility = preparedDay.coreSelectedCompatibility;
        IDailyV1SelectedDayStartExecutionContext dayStartContext = preparedDay.dayStartContext;
        bool hasSelectedDayStartContext = preparedDay.hasSelectedDayStartContext;
        bool dayStartContextPreflighted = preparedDay.dayStartContextPreflighted;
        bool admittedSelectedBoundariesPreflighted = preparedDay.admittedSelectedBoundariesPreflighted;
        IDailyV1SelectedTravelDayExecutionContext travelContext = preparedDay.travelContext;
        bool travelContextPreflighted = preparedDay.travelContextPreflighted;

        boundary.BeginLoggerDay(absoluteDay);

        // The accepted Daily-v1 proving profile disables both demographic
        // writers. In that profile the shared Core stage must not require a
        // Unity person-death writer or represented-resident projection simply
        // to perform an empty phase. Enabled demographic policies continue
        // through the exact existing owner/admission boundary below.
        DailyV1SelectedDemographyResult demographyResult;
        if (selectedConfiguration.NaturalMortality.Enabled
            || selectedConfiguration.AggregateDemography.Enabled)
        {
            IDailyV1SelectedDemographyExecutionContext demographyContext =
                preparedDay.demographyContext;
            bool hasDemographyContext = preparedDay.demographyContextPreflighted
                ? demographyContext != null
                : boundary.TryCreateSelectedDemographyContext(graph, out demographyContext);
            SimulationCalendar demographyCalendar = executionState == null
                ? demographyContext?.Calendar
                : executionState.CalendarIdentity;
            if (!hasDemographyContext
                || demographyContext == null
                || graph.PersonStore == null
                || demographyCalendar == null
                || demographyContext.Boundary == null)
                throw new InvalidOperationException(
                    "The selected Daily-v1 host could not provide the exact demographic owners and writer boundary.");

            demographyResult = DailyV1SelectedDemographyStage.Advance(
                graph,
                graph.PersonStore,
                absoluteDay,
                demographyCalendar,
                selectedConfiguration.NaturalMortality,
                selectedConfiguration.AggregateDemography,
                demographyContext.MortalitySamples,
                demographyContext.AggregateProvider,
                demographyContext.Boundary);
        }
        else
        {
            demographyResult = new DailyV1SelectedDemographyResult(absoluteDay);
        }
        boundary.PublishSelectedDemographyResult(demographyResult);
        if (!dayStartContextPreflighted && !coreSelectedCompatibility)
            hasSelectedDayStartContext = boundary.TryCreateSelectedDayStartContext(
                graph, out dayStartContext);
        if (hasSelectedDayStartContext)
        {
            if (dayStartContext == null)
                throw new InvalidOperationException(
                    "The selected Daily-v1 host returned a null day-start context.");

            bool hasUnselectedRuntimeMember = executionState == null
                ? dayStartContext.HasUnselectedLiveRuntimeMember
                : HasUnselectedRuntimeMemberFromCoreRoster(executionState, actorRoster);
            if (hasUnselectedRuntimeMember)
            {
                // For an admitted runtime, Core selects the established
                // whole-stage compatibility path from its exact ordered
                // identity roster and owner graph. A Unity adapter's parallel
                // runtime-membership flag cannot select execution authority.
                boundary.BeginSimulationDay();
            }
            else
            {
                // An admitted selected session executes its Crime/Justice P12
                // reservations through the Core actor-turn boundary. The
                // day-start host context still supplies the exact roster and
                // projection authority, but it cannot own the reservation or
                // epoch lifecycle. Non-admitted compatibility execution keeps
                // the established host boundary.
                IDailyV1SelectedCrimeJusticeDailyBoundary crimeJusticeBoundary =
                    executionState?.HasAdmittedSelectedDailyExecution == true
                        ? actorTurnContext?.Boundary as IDailyV1SelectedCrimeJusticeDailyBoundary
                        : dayStartContext.CrimeJusticeBoundary;
                IReadOnlyList<DailyV1ActorTurnRosterProjection> dayStartRuntimeRoster =
                    executionState?.SelectedRuntimeActorRoster ?? dayStartContext.RuntimeRoster;
                if (dayStartRuntimeRoster == null
                    || crimeJusticeBoundary == null
                    || (selectedConfiguration.MerchantTrade?.Enabled == true
                        && dayStartContext.MerchantTradeBoundary == null)
                    || !ReferenceEquals(graph.JusticeRecordsOwner,
                        crimeJusticeBoundary.JusticeRecordsOwner)
                    || graph.Definitions.EffectiveConfiguration?.MerchantTrade == null
                    || !DailyV1ActorTurnExecutionStage.TryBuildRoster(
                        graph, dayStartRuntimeRoster,
                        out IReadOnlyList<DailyV1ActorTurnRosterEntry> dayStartRoster))
                    throw new InvalidOperationException(
                        "The selected Daily-v1 host could not provide its exact day-start owner roster and boundaries.");

                List<DailyV1NpcRuntimeOwners> exactNpcOwners = new List<DailyV1NpcRuntimeOwners>();
                foreach (DailyV1ActorTurnRosterEntry entry in dayStartRoster)
                    if (entry?.ActorOwner != null)
                        exactNpcOwners.Add(entry.ActorOwner);
                if (!graph.MatchesExactOwnerInstances(graph.Cities, exactNpcOwners))
                    throw new InvalidOperationException(
                        "The selected Daily-v1 day-start roster does not match the exact Core NPC owners.");

                // The admitted selected profile requires both existing
                // Crime/Justice infrastructures. Crime.Enabled controls action
                // provider admission; it does not suppress hidden timers or
                // Justice day-start work. Do not let parallel Unity presence
                // flags suppress those Core-owned phases. If the required
                // Justice owner is absent, TryRun rejects before any mutation.
                bool admittedSelectedExecution =
                    executionState?.HasAdmittedSelectedDailyExecution == true;
                bool selectedJusticeInfrastructurePresent = admittedSelectedExecution
                    || dayStartContext.JusticeSystemPresent;
                bool selectedCrimeInfrastructurePresent = admittedSelectedExecution
                    || dayStartContext.CrimeSystemPresent;
                if (!DailyV1SelectedCrimeJusticeDailyStage.TryRun(
                        graph,
                        dayStartRoster,
                        selectedCrimeInfrastructurePresent,
                        selectedJusticeInfrastructurePresent,
                        crimeJusticeBoundary))
                    throw new InvalidOperationException(
                        "The selected Core Crime/Justice day-start stage rejected its exact owner graph.");

                IDailyV1SelectedMerchantTradeStateAdmissionBoundary merchantAdmissionBoundary =
                    admittedSelectedExecution
                        ? actorTurnContext?.Boundary as IDailyV1SelectedMerchantTradeStateAdmissionBoundary
                        : null;
                if (admittedSelectedExecution
                    && graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled
                    && merchantAdmissionBoundary == null)
                    throw new InvalidOperationException(
                        "The admitted Core runtime omitted its MerchantDailyTrade P12 operation boundary.");
                if (graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled
                    && !DailyV1SelectedMerchantTradeStateOperation.TryAdvancePendingTravelDays(
                        graph, exactNpcOwners, dayStartContext.MerchantTradeBoundary,
                        merchantAdmissionBoundary))
                    throw new InvalidOperationException(
                        "The selected Core Merchant plan-urgency stage rejected its exact owner graph.");
            }
        }
        else
        {
            // A Core-bound unselected runtime member still selects the
            // established mixed-roster compatibility path. Otherwise, an
            // admitted execution state must not silently delegate a missing
            // selected boundary back to Unity: Core has already bound its
            // exact owner graph and selected runtime roots.
            if (executionState != null && !coreSelectedCompatibility)
                throw new InvalidOperationException(
                    "The admitted Daily-v1 host could not provide its selected day-start boundary.");

            boundary.BeginSimulationDay();
        }
        if (scheduledDirectives != null)
        {
            if (graph.RuntimeIdentityIndex == null)
                throw new InvalidOperationException(
                    "Selected Daily-v1 directive preparation requires the Core runtime identity index.");
            scheduledDirectives.PrepareSelectedDailyV1Day(absoluteDay, graph.RuntimeIdentityIndex);
        }

        if (graph.Definitions.EffectiveConfiguration?.Economy?.Enabled == true)
        {
            if (!preparedDay.selectedEconomyPreflighted)
                boundary.EnsureSelectedEconomyCanRun(graph);
            bool hasAdmittedFullP12Census = executionState != null
                && executionState.HasSealedSelectedOwnerSectionCensusRegistry;
            string economyP12Diagnostic = !hasAdmittedFullP12Census
                ? "Legacy non-admitted economy path"
                : null;
            bool economyCompleted = !hasAdmittedFullP12Census
                ? graph.TrySimulateDailyEconomyDay(
                    graph.Cities,
                    absoluteDay,
                    GetCalendarSemanticVersion(graph.Definitions.Calendar),
                    boundary.LogSelectedDailyEconomy)
                : TrySimulateAdmittedDailyEconomyDay(
                    executionState,
                    graph,
                    absoluteDay,
                    GetCalendarSemanticVersion(graph.Definitions.Calendar),
                    boundary.LogSelectedDailyEconomy,
                    out economyP12Diagnostic);
            if (!economyCompleted)
                throw new InvalidOperationException(
                    "The selected Core economy stage could not complete against its exact runtime owner graph. "
                    + economyP12Diagnostic);
        }

        if (!preparedDay.selectedLocalKnowledgePreflighted)
            boundary.EnsureSelectedLocalKnowledgeCanRun(graph);
        if (!DailyV1SelectedLocalKnowledgeRefreshStage.TryRun(graph, absoluteDay))
            throw new InvalidOperationException(
                "The selected Core local-knowledge stage could not complete against its exact runtime owner graph.");

        if (executionState == null
            && (!boundary.TryCreateSelectedActorTurnContext(
                    graph, out actorTurnContext)
                || actorTurnContext == null
                || actorTurnContext.RuntimeRoster == null
                || actorTurnContext.PersonStore == null
                || !ReferenceEquals(graph.PersonStore, actorTurnContext.PersonStore)
                || actorTurnContext.ActorChoiceStore == null
                || actorTurnContext.Boundary == null
                || !DailyV1ActorTurnExecutionStage.TryBuildRoster(
                    graph, actorTurnContext.RuntimeRoster, out actorRoster)))
            throw new InvalidOperationException(
                "The selected Core actor-turn stage could not build the exact admitted graph and runtime roster.");
        if (graph.Definitions.EffectiveConfiguration?.MerchantTrade == null)
            throw new InvalidOperationException(
                "The selected Core actor-turn stage has no admitted Merchant configuration.");

        if (!DailyV1ActorTurnExecutionStage.TryRunOrdered(
                graph,
                actorRoster,
                actorTurnContext.PersonStore,
                actorTurnContext.ActorChoiceStore,
                scheduledDirectives,
                absoluteDay,
                graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled,
                requireCoreOwnedSelectedOperations: executionState != null,
                actorTurnContext.CoreRoots,
                actorTurnContext.Boundary,
                actorTurnContext.MerchantTradeBoundary))
            throw new InvalidOperationException(
                "The selected Core actor-turn stage could not run with the exact admitted graph and runtime roster.");
        if (!travelContextPreflighted
            && !TryCreateSelectedTravelDayContext(
                executionState, graph, boundary, out travelContext))
            throw new InvalidOperationException(
                "The selected Daily-v1 host could not provide its exact travel projection and P12 context.");
        if (travelContext == null || travelContext.Boundary == null)
            throw new InvalidOperationException(
                "The selected Daily-v1 host could not provide its exact travel projection and P12 context.");
        bool travelAdvanced = TryAdvanceTravelDay(
            executionState, graph, travelContext,
            out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
            out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults);
        if (!travelAdvanced)
            throw new InvalidOperationException(
                "The selected Core TravelParty/solo-travel stages could not advance the exact owner graph.");
        travelContext.PublishCompletedTravelResults(partyArrivals, soloResults);
        if (!DailyV1SelectedMerchantTradeStateOperation.ObserveCompletedTravelResults(
                graph, partyArrivals, soloResults, absoluteDay))
            throw new InvalidOperationException(
                "The selected Core post-travel market observation could not apply to its exact owner graph.");
        travelContext.ObserveCompletedTravelResults();
    }

    private static bool TrySimulateAdmittedDailyEconomyDay(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        long absoluteDay,
        string calendarVersion,
        Action<DailyV1CityRuntimeOwners, SimulationLogCategory, string> logOutput,
        out string diagnostic)
    {
        diagnostic = null;
        if (executionState == null || graph == null
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            || !TryGetDailyEconomyP12MutationUpperBound(graph, out int mutationUpperBound)
            || !executionState.MutationEpochState.CanAdvanceBy(mutationUpperBound))
        {
            diagnostic = "P12DailyEconomyPreflightCapacityOrGraph";
            return false;
        }

        DailyV1CoreSelectedOwnerSectionMutationProtocol protocol =
            new DailyV1CoreSelectedOwnerSectionMutationProtocol(executionState, graph);
        if (!TryReadAdmittedDailyEconomyP12Witnesses(
                executionState,
                requireCurrentBaselines: true,
                out IReadOnlyList<OwnerSectionCensusSnapshot> before,
                out _,
                out long epochBefore,
                out string baselineReadDiagnostic))
        {
            diagnostic = "P12DailyEconomyBaselineRead:" + baselineReadDiagnostic;
            protocol.FaultClosed();
            return false;
        }
        if (before == null || before.Count == 0
            || epochBefore != executionState.MutationEpochState.CurrentEpoch)
        {
            diagnostic = "P12DailyEconomyBaselineVectorOrEpoch";
            protocol.FaultClosed();
            return false;
        }

        bool completed = graph.TrySimulateDailyEconomyDay(
            graph.Cities, absoluteDay, calendarVersion, logOutput);
        if (!TryReadAdmittedDailyEconomyP12Witnesses(
                executionState,
                requireCurrentBaselines: false,
                out IReadOnlyList<OwnerSectionCensusSnapshot> after,
                out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> afterRegistrations,
                out long postReadEpoch,
                out string postReadDiagnostic))
        {
            diagnostic = "P12DailyEconomyPostWitnessRead:" + postReadDiagnostic
                + ":" + executionState.LastSelectedOwnerSectionCensusRefreshRejection;
            protocol.FaultClosed();
            return false;
        }
        long epochAfter = executionState.MutationEpochState.CurrentEpoch;
        if (postReadEpoch != epochAfter || postReadEpoch < epochBefore)
        {
            diagnostic = "P12DailyEconomyPostWitnessEpochDrift:before=" + epochBefore
                + ";witness=" + postReadEpoch + ";current=" + epochAfter;
            protocol.FaultClosed();
            return false;
        }

        if (!TryGetChangedDailyEconomyP12Sections(
                before, after,
                out IReadOnlyList<string> changedSectionIds,
                out int committedMutationCount))
        {
            diagnostic = "P12DailyEconomyChangedOwnerCutInvalid";
            protocol.FaultClosed();
            return false;
        }

        if (!completed)
        {
            if (changedSectionIds.Count != 0
                || epochAfter != epochBefore)
            {
                diagnostic = "P12DailyEconomyRejectedAfterMutation";
                protocol.FaultClosed();
            }
            return false;
        }

        if (changedSectionIds.Count == 0)
        {
            bool unchangedEpoch = epochAfter == epochBefore;
            if (!unchangedEpoch) diagnostic = "P12DailyEconomyEpochChangedWithoutOwnerRows";
            return unchangedEpoch;
        }

        // Unity compatibility owners may already publish each successful
        // Market commit through their bound P12 callback. That callback shares
        // this Core epoch and atomically refreshes the sealed Core census.
        // Accept that path only when the complete post-operation baseline now
        // exactly matches every live witness; never publish a second epoch.
        long publishedEpochDelta = epochAfter - epochBefore;
        if (publishedEpochDelta > 0L)
        {
            if (!HasExactHostPublishedDailyEconomyMutationEpochDelta(
                    publishedEpochDelta, committedMutationCount, mutationUpperBound)
                || !HasExactAdmittedDailyEconomyBaselines(afterRegistrations, after))
            {
                diagnostic = "P12DailyEconomyHostPublishedBaselineOrEpochMismatch:epochDelta="
                    + publishedEpochDelta.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ";ownerMutations="
                    + committedMutationCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                protocol.FaultClosed();
                return false;
            }
            return true;
        }

        if (committedMutationCount <= 0
            || committedMutationCount > mutationUpperBound
            || !protocol.TryRefreshCommittedMutationBaselines(changedSectionIds)
            || !executionState.MutationEpochState.TryAdvanceCommittedMutations(
                committedMutationCount)
            || executionState.MutationEpochState.CurrentEpoch
                != epochBefore + committedMutationCount)
        {
            diagnostic = "P12DailyEconomyPublish:" + executionState.LastSelectedOwnerSectionCensusRefreshRejection;
            protocol.FaultClosed();
            return false;
        }

        diagnostic = "P12DailyEconomyCommittedRows=" + changedSectionIds.Count
            + ";ownerMutations=" + committedMutationCount;
        return true;
    }

    internal static bool HasExactHostPublishedDailyEconomyMutationEpochDelta(
        long publishedEpochDelta,
        int committedMutationCount,
        int mutationUpperBound) =>
        publishedEpochDelta > 0L
        && committedMutationCount > 0
        && committedMutationCount <= mutationUpperBound
        && publishedEpochDelta == committedMutationCount;

    private static bool TryReadAdmittedDailyEconomyP12Witnesses(
        DailyV1ExecutionBoundaryState executionState,
        bool requireCurrentBaselines,
        out IReadOnlyList<OwnerSectionCensusSnapshot> snapshots,
        out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        out long mutationEpoch,
        out string diagnostic)
    {
        snapshots = null;
        registrations = null;
        mutationEpoch = 0L;
        diagnostic = null;
        if (executionState == null)
        {
            diagnostic = "ExecutionStateMissing";
            return false;
        }
        if (!executionState.TryGetSelectedOwnerSectionCensusRegistryForDailyAdvance(
                out registrations,
                out _, out mutationEpoch, out DailyCaptureEligibilityFailure registryFailure,
                out string registryDiagnostic))
        {
            diagnostic = registryFailure + ":" + registryDiagnostic;
            return false;
        }
        if (registrations == null || registrations.Count == 0)
        {
            diagnostic = "OwnerCoverageIncomplete";
            return false;
        }

        List<OwnerSectionCensusSnapshot> captured =
            new List<OwnerSectionCensusSnapshot>(registrations.Count);
        HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (DailyV1OwnerSectionCensusProviderRegistration registration in registrations)
            {
                if (registration == null || registration.Provider == null
                    || !registration.HasBaseline
                    || string.IsNullOrWhiteSpace(registration.SectionId)
                    || registration.SchemaVersion <= 0
                    || registration.BaselineOwnerInstanceIdentity == null
                    || !sectionIds.Add(registration.SectionId))
                    return false;

                OwnerSectionCensusWitness witness = registration.Provider.GetCurrentCensus();
                if (witness == null
                    || !string.Equals(witness.SectionId, registration.SectionId,
                        StringComparison.Ordinal)
                    || witness.SchemaVersion != registration.SchemaVersion
                    || witness.OwnerInstanceIdentity == null
                    || !ReferenceEquals(witness.OwnerInstanceIdentity,
                        registration.BaselineOwnerInstanceIdentity)
                    || witness.Cardinality < 0 || witness.Revision < 0L)
                {
                    diagnostic = "OwnerWitnessInvalid:" + registration.SectionId;
                    return false;
                }
                if (requireCurrentBaselines
                    && (witness.Cardinality != registration.BaselineCardinality
                        || witness.Revision != registration.BaselineRevision))
                {
                    diagnostic = "BaselineDrift:" + registration.SectionId;
                    return false;
                }

                captured.Add(new OwnerSectionCensusSnapshot(
                    witness.SectionId,
                    witness.SchemaVersion,
                    registration.Role,
                    witness.OwnerInstanceIdentity,
                    witness.Cardinality,
                    witness.Revision));
            }
        }
        catch
        {
            diagnostic = "ProviderReadException";
            return false;
        }

        captured.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
        snapshots = captured.AsReadOnly();
        return true;
    }

    private static bool HasExactAdmittedDailyEconomyBaselines(
        IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registrations,
        IReadOnlyList<OwnerSectionCensusSnapshot> witnesses)
    {
        if (registrations == null || witnesses == null || registrations.Count == 0
            || registrations.Count != witnesses.Count)
            return false;

        Dictionary<string, OwnerSectionCensusSnapshot> witnessesById;
        try
        {
            witnessesById = witnesses.ToDictionary(row => row.SectionId, StringComparer.Ordinal);
        }
        catch
        {
            return false;
        }

        HashSet<string> registrationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in registrations)
        {
            if (registration == null || registration.Provider == null || !registration.HasBaseline
                || string.IsNullOrWhiteSpace(registration.SectionId)
                || !registrationIds.Add(registration.SectionId)
                || !witnessesById.TryGetValue(registration.SectionId,
                    out OwnerSectionCensusSnapshot witness)
                || witness == null
                || witness.SchemaVersion != registration.SchemaVersion
                || witness.Role != registration.Role
                || !ReferenceEquals(witness.OwnerInstanceIdentity,
                    registration.BaselineOwnerInstanceIdentity)
                || witness.Cardinality != registration.BaselineCardinality
                || witness.Revision != registration.BaselineRevision)
                return false;
        }
        return registrationIds.Count == witnessesById.Count;
    }

    private static bool TryGetChangedDailyEconomyP12Sections(
        IReadOnlyList<OwnerSectionCensusSnapshot> before,
        IReadOnlyList<OwnerSectionCensusSnapshot> after,
        out IReadOnlyList<string> changedSectionIds,
        out int committedMutationCount)
    {
        changedSectionIds = null;
        committedMutationCount = 0;
        if (before == null || after == null || before.Count == 0
            || before.Count != after.Count)
            return false;

        Dictionary<string, OwnerSectionCensusSnapshot> priorById;
        try
        {
            priorById = before.ToDictionary(row => row.SectionId, StringComparer.Ordinal);
        }
        catch { return false; }

        List<string> changed = new List<string>();
        List<KeyValuePair<object, long>> ownerRevisionDeltas =
            new List<KeyValuePair<object, long>>();
        foreach (OwnerSectionCensusSnapshot current in after)
        {
            if (current == null || string.IsNullOrWhiteSpace(current.SectionId)
                || !priorById.TryGetValue(current.SectionId,
                    out OwnerSectionCensusSnapshot prior)
                || prior == null || prior.Role != current.Role
                || prior.SchemaVersion != current.SchemaVersion
                || !ReferenceEquals(prior.OwnerInstanceIdentity,
                    current.OwnerInstanceIdentity))
                return false;

            bool cardinalityChanged = current.Cardinality != prior.Cardinality;
            bool revisionChanged = current.Revision != prior.Revision;
            if (!cardinalityChanged && !revisionChanged) continue;
            if (!revisionChanged || current.Revision <= prior.Revision) return false;

            long revisionDelta = current.Revision - prior.Revision;
            int ownerIndex = -1;
            for (int index = 0; index < ownerRevisionDeltas.Count; index++)
                if (ReferenceEquals(ownerRevisionDeltas[index].Key,
                        current.OwnerInstanceIdentity))
                {
                    ownerIndex = index;
                    break;
                }

            if (ownerIndex < 0)
                ownerRevisionDeltas.Add(new KeyValuePair<object, long>(
                    current.OwnerInstanceIdentity, revisionDelta));
            else if (ownerRevisionDeltas[ownerIndex].Value != revisionDelta)
                return false;

            changed.Add(current.SectionId);
        }

        long total = 0L;
        foreach (KeyValuePair<object, long> ownerDelta in ownerRevisionDeltas)
        {
            if (ownerDelta.Value <= 0L || total > int.MaxValue - ownerDelta.Value)
                return false;
            total += ownerDelta.Value;
        }

        changed.Sort(StringComparer.Ordinal);
        changedSectionIds = changed.AsReadOnly();
        committedMutationCount = (int)total;
        return true;
    }

    private static bool TryGetDailyEconomyP12MutationUpperBound(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out int mutationUpperBound)
    {
        mutationUpperBound = 0;
        if (graph?.Cities == null || graph.Definitions?.Items == null) return false;

        long upperBound = 0L;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city == null || city.Definition?.Values == null
                || city.Market == null || !graph.ContainsExactCityOwner(city))
                return false;

            var values = city.Definition.Values;
            int productionCount = values.productionConfigs?.Count ?? 0;
            int marketCount = values.marketItems?.Count ?? 0;
            int localFlowCount = city.HasLocalDailyMaterialFlow ? 1 : 0;
            upperBound += productionCount + (long)marketCount * 2L
                + localFlowCount + 4L;
            if (upperBound > int.MaxValue) return false;
        }

        mutationUpperBound = (int)upperBound;
        return mutationUpperBound > 0;
    }

    private static bool HasUnselectedRuntimeMemberFromCoreRoster(
        DailyV1ExecutionBoundaryState executionState,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> coreActorRoster)
    {
        IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster =
            executionState?.SelectedRuntimeActorRoster;
        if (runtimeRoster == null || coreActorRoster == null
            || runtimeRoster.Count != coreActorRoster.Count)
            throw new InvalidOperationException(
                "Core cannot select Daily-v1 compatibility execution without its exact ordered runtime roster.");

        for (int ordinal = 0; ordinal < runtimeRoster.Count; ordinal++)
        {
            DailyV1ActorTurnRosterProjection projection = runtimeRoster[ordinal];
            DailyV1ActorTurnRosterEntry entry = coreActorRoster[ordinal];
            if (entry == null || entry.RosterOrdinal != ordinal)
                throw new InvalidOperationException(
                    "Core cannot select Daily-v1 compatibility execution from a malformed owner roster.");

            if (projection != null && entry.ActorOwner == null)
                return true;
        }

        return false;
    }

    private static bool HasExactSelectedExecutionRoots(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedActorTurnExecutionContext actorTurnContext,
        IReadOnlyList<DailyV1ActorTurnRosterEntry> actorRoster,
        out string failure)
    {
        failure = null;
        if (actorTurnContext.CoreRoots == null
            || !actorTurnContext.CoreRoots.IsBoundTo(
                executionState, graph, actorTurnContext.PersonStore,
                actorTurnContext.ActorChoiceStore))
        {
            failure = "Core actor-turn decision roots differ from the exact execution-state roots";
            return false;
        }
        if (!(actorTurnContext.Boundary is IDailyV1SelectedActorActionExecutionBoundary actionBoundary))
        {
            failure = "actor action boundary does not implement the selected Core contract";
            return false;
        }
        if (!(actorTurnContext.Boundary is IDailyV1SelectedActorChoiceExecutionContext choiceContext)
            || !(actorTurnContext.Boundary is IDailyV1CoreActorChoiceExecutionBoundary))
        {
            failure = "actor action boundary does not provide the Core-owned selected actor-choice contract";
            return false;
        }
        bool merchantTradeEnabled = graph.Definitions.EffectiveConfiguration?.MerchantTrade?.Enabled == true;
        if (merchantTradeEnabled
            && (!(actorTurnContext.Boundary is IDailyV1SelectedMerchantTradeStateP12HostPorts)
                || !(actorTurnContext.Boundary is IDailyV1SelectedMerchantTradeStateAdmissionBoundary)))
        {
            failure = "actor action boundary does not provide the Core Merchant P12 effect ports and operation admission contract";
            return false;
        }
        if (!executionState.HasExactSelectedRuntimeActorTurnRoots(
                graph, actorTurnContext.PersonStore, actorTurnContext.ActorChoiceStore,
                actorTurnContext.CoreRoots.RandomSource))
        {
            failure = "execution-state graph, PersonStore, ActorChoiceStore, or Core random-source identity differs";
            return false;
        }

        foreach (DailyV1ActorTurnRosterEntry entry in actorRoster)
        {
            DailyV1NpcRuntimeOwners actor = entry?.ActorOwner;
            if (actor == null) continue;
            if (!actionBoundary.UsesSelectedDailyV1ActionExecution(actor))
            {
                failure = "selected actor " + actor.RuntimeId + " would fall back to host-owned action execution";
                return false;
            }

            if (!choiceContext.TryGetActorChoiceExecutionContext(
                    actor,
                    out IDailyV1ActorChoiceExecutionBoundary actorChoiceBoundary)
                || !(actorChoiceBoundary is IDailyV1CoreActorChoiceExecutionBoundary coreActorChoiceBoundary))
            {
                failure = "actor-choice boundary is unavailable or is not Core-owned for actor "
                    + actor.RuntimeId;
                return false;
            }
            if (!coreActorChoiceBoundary.UsesSelectedDailyV1ActionExecution(actor))
            {
                failure = "actor-choice boundary would fall back to host-owned action execution for actor "
                    + actor.RuntimeId;
                return false;
            }
            if (!ReferenceEquals(coreActorChoiceBoundary, actionBoundary))
            {
                failure = "actor-choice projection/admission adapter differs from the exact selected actor-turn adapter for actor "
                    + actor.RuntimeId;
                return false;
            }
        }

        return true;
    }

    private static bool TryCreateSelectedTravelDayContext(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedDailyExecutionBoundary boundary,
        out IDailyV1SelectedTravelDayExecutionContext context)
    {
        context = null;
        if (executionState == null || !executionState.HasAdmittedSelectedDailyExecution)
            return boundary.TryCreateSelectedTravelDayContext(graph, out context);

        if (!boundary.TryCreateSelectedTravelDayHostPorts(
                graph, out IDailyV1SelectedTravelDayHostPorts hostPorts))
            return false;

        return DailyV1CoreSelectedTravelDayExecutionContext.TryCreate(
            executionState, graph, hostPorts, out context);
    }

    internal static bool TryAdvanceTravelDay(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedTravelDayExecutionContext context,
        out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
        out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
    {
        partyArrivals = Array.Empty<DailyV1SelectedTravelPartyArrivalResult>();
        soloResults = Array.Empty<DailyV1SelectedSoloTravelDayResult>();
        if (graph == null || context == null) return false;
        if (context.Boundary == null)
            return false;

        return DailyV1SelectedTravelDayStage.TryAdvance(
            graph,
            context.Boundary,
            executionState,
            context.P12Protocol,
            executionState == null ? context.ExecuteWithinPartyOwnerOperation : null,
            ShouldAdvanceSelectedSoloTravel(executionState, context.AdvanceSoloTravel),
            out partyArrivals,
            out soloResults);
    }

    /// <summary>
    /// A sealed Core session owns selected travel-stage availability. The host
    /// value is retained only for non-admitted compatibility execution; an
    /// admitted session without its required sealed inventory is invalid.
    /// </summary>
    internal static bool ShouldAdvanceSelectedSoloTravel(
        DailyV1ExecutionBoundaryState executionState,
        bool hostReportedTravelAvailability)
    {
        if (executionState == null)
            return hostReportedTravelAvailability;

        if (!executionState.HasRuntimeProtocolOperationInventory)
        {
            if (executionState.HasAdmittedSelectedDailyExecution)
                throw new InvalidOperationException(
                    "The admitted selected Core session has no sealed runtime operation inventory.");
            return hostReportedTravelAvailability;
        }

        return executionState.HasRuntimeProtocolOperationContract(
            DailyV1RuntimeProtocolOperationContracts.SoloTravelStart);
    }
    private static string GetCalendarSemanticVersion(DailyV1CalendarDefinition calendar)
    {
        if (calendar == null) throw new InvalidOperationException(
            "The selected Core economy stage requires an admitted calendar definition.");

        CalendarDefinition definition = calendar.monthLengths != null && calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(calendar.monthLengths), calendar.daysPerWeek)
            : new CalendarDefinition(calendar.monthsPerYear, calendar.weeksPerMonth, calendar.daysPerWeek);
        return new SimulationCalendar(definition).SemanticVersion;
    }
}
