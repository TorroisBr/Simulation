using System;
using System.Collections.Generic;

internal enum DailyV1OperationScopeKind : byte
{
    Application = 0,
    RuntimeProtocol = 1,
    ContinuationSave = 2,
    DailyAdvance = 3
}

internal enum DailyV1CaptureScopeAllowance : byte
{
    None = 0,
    ContinuationSave = 1,
    DailyAdvance = 2,
    SessionReplacementValidation = 3
}

/// <summary>
/// Owns the per-runtime single-writer advance lease and completed Daily-v1
/// core sequence. This is execution state, not a thread lock or persisted data.
/// </summary>
internal sealed partial class DailyV1ExecutionBoundaryState
{
    private readonly object runtimeInstanceIdentity = new object();
    private readonly object operationSyncRoot = new object();
    private readonly List<DailyV1OperationScopeKind> activeOperationScopes =
        new List<DailyV1OperationScopeKind>();
    private readonly DailyV1MutationEpochState mutationEpochState =
        new DailyV1MutationEpochState();
    private readonly AuthoritativeMutationGuard mutationGuard =
        new AuthoritativeMutationGuard();
    private bool runtimeIdentityBound;
    private bool runtimeConfigurationBound;
    private bool exclusiveSessionReplacementInProgress;
    private DailyV1AdvanceLease advanceLease;
    private long advanceLeaseInitialCompletedSequence;
    private long completedCoreSequence;
    private DailyCaptureEligibilityToken currentCaptureToken;
    private bool selectedRuntimeOwnerGraphBound;
    private DailyV1SelectedRuntimeOwnerGraph selectedRuntimeOwnerGraph;
    private IReadOnlyList<DailyV1ActorTurnRosterProjection> selectedRuntimeActorRoster;
    private bool selectedRuntimeExecutionRootsBound;
    private RuntimeIdAllocator selectedRuntimeIdAllocator;
    private IAuthoritativeRandomSource selectedRandomSource;
    private SimulationRecordSequence selectedRecordSequence;
    private NpcDecisionStore selectedDecisionStore;
    private ActorChoiceStore selectedActorChoiceStore;

    internal SimulationTime SimulationTime { get; private set; }
    internal SimulationRuntimeAdmissionContext AdmissionContext { get; private set; }
    internal SimulationRuntimeCompositionProfile CompositionProfile { get; private set; }
    internal WorldId WorldId { get; private set; }
    internal EffectiveSimulationConfiguration ConfigurationIdentity { get; private set; }
    internal SimulationCalendar CalendarIdentity { get; private set; }
    internal AuthoritativeMutationGuard MutationGuard => mutationGuard;
    internal bool CanMutate => mutationGuard.CanMutate;

    internal DailyV1ExecutionBoundaryState() { }

    /// <summary>
    /// Constructs the one Core execution state used by a selected runtime.
    /// Clock, admission, composition and world identities live beside that
    /// runtime's lease, completed-boundary sequence, graph and capture token;
    /// a Unity adapter must not keep parallel copies as runtime authority.
    /// </summary>
    internal DailyV1ExecutionBoundaryState(
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId)
    {
        if (simulationTime == null) throw new ArgumentNullException(nameof(simulationTime));
        if (!Enum.IsDefined(typeof(SimulationRuntimeCompositionProfile), compositionProfile))
            throw new ArgumentOutOfRangeException(nameof(compositionProfile));
        SimulationTime = simulationTime;
        AdmissionContext = admissionContext;
        CompositionProfile = compositionProfile;
        WorldId = worldId;
        runtimeIdentityBound = true;
    }

    internal bool TryBindRuntimeConfiguration(
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity)
    {
        if (!runtimeIdentityBound || configurationIdentity == null || calendarIdentity == null)
            return false;
        if (runtimeConfigurationBound)
            return ReferenceEquals(ConfigurationIdentity, configurationIdentity)
                && ReferenceEquals(CalendarIdentity, calendarIdentity);
        ConfigurationIdentity = configurationIdentity;
        CalendarIdentity = calendarIdentity;
        runtimeConfigurationBound = true;
        return true;
    }

    private bool MatchesBoundRuntimeIdentity(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay)
    {
        return !runtimeIdentityBound
            || (runtimeConfigurationBound
                && ReferenceEquals(AdmissionContext, admissionContext)
                && ReferenceEquals(ConfigurationIdentity, configurationIdentity)
                && ReferenceEquals(CalendarIdentity, calendarIdentity)
                && CompositionProfile == compositionProfile
                && ReferenceEquals(WorldId, worldId)
                && absoluteDay == SimulationTime.AbsoluteDay);
    }

    internal bool AdvanceLeaseHeld => advanceLease != null;

    /// <summary>
    /// The single operation count shared by application admission and the P12
    /// census protocol for this runtime. It is deliberately stored beside the
    /// runtime lease and capture token, rather than mirrored by a host gate.
    /// </summary>
    internal int ActiveOperationCount
    {
        get { lock (operationSyncRoot) return activeOperationCount; }
    }

    internal int ActiveRuntimeOperationCount
    {
        get
        {
            lock (operationSyncRoot)
            {
                int count = 0;
                foreach (DailyV1OperationScopeKind scope in activeOperationScopes)
                    if (scope == DailyV1OperationScopeKind.RuntimeProtocol) count++;
                return count;
            }
        }
    }

    internal bool TryBeginOperation(DailyV1OperationScopeKind kind = DailyV1OperationScopeKind.Application)
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            if (kind == DailyV1OperationScopeKind.RuntimeProtocol
                || !Enum.IsDefined(typeof(DailyV1OperationScopeKind), kind))
                return false;
            return TryAddOperationScope(kind);
        }
    }

    internal bool TryEndOperation(DailyV1OperationScopeKind kind = DailyV1OperationScopeKind.Application)
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread())
                return false;
            if (kind == DailyV1OperationScopeKind.RuntimeProtocol
                || !Enum.IsDefined(typeof(DailyV1OperationScopeKind), kind))
                return false;
            return TryRemoveOperationScope(kind);
        }
    }

    /// <summary>
    /// Prevents both application and direct runtime/P12 operations from
    /// entering the source session while a detached replacement is staged.
    /// This flag shares the same lock and operation registry as daily advance
    /// and P12 census scopes, so same-thread callbacks cannot bypass the app gate.
    /// </summary>
    internal bool TryBeginExclusiveSessionReplacement()
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || activeOperationScopes.Count != 0
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            exclusiveSessionReplacementInProgress = true;
            return true;
        }
    }

    internal bool TryEndExclusiveSessionReplacement()
    {
        lock (operationSyncRoot)
        {
            if (!exclusiveSessionReplacementInProgress
                || activeOperationScopes.Count != 0
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            exclusiveSessionReplacementInProgress = false;
            return true;
        }
    }

    internal bool TryAssessCaptureScope(DailyV1CaptureScopeAllowance allowance)
    {
        lock (operationSyncRoot)
        {
            if ((exclusiveSessionReplacementInProgress
                    && allowance != DailyV1CaptureScopeAllowance.SessionReplacementValidation)
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            if (allowance == DailyV1CaptureScopeAllowance.None
                || allowance == DailyV1CaptureScopeAllowance.SessionReplacementValidation)
                return activeOperationScopes.Count == 0;
            if (activeOperationScopes.Count != 1)
                return false;

            DailyV1OperationScopeKind expected = allowance == DailyV1CaptureScopeAllowance.ContinuationSave
                ? DailyV1OperationScopeKind.ContinuationSave
                : allowance == DailyV1CaptureScopeAllowance.DailyAdvance
                    ? DailyV1OperationScopeKind.DailyAdvance
                    : (DailyV1OperationScopeKind)byte.MaxValue;
            return activeOperationScopes[0] == expected;
        }
    }

    internal bool TryBeginRuntimeProtocolOperation()
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || AdmissionContext == null || !AdmissionContext.IsOwnedByCurrentThread())
                return false;
            return TryAddOperationScope(DailyV1OperationScopeKind.RuntimeProtocol);
        }
    }

    internal bool TryEndRuntimeProtocolOperation()
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext == null || !AdmissionContext.IsOwnedByCurrentThread())
                return false;
            return TryRemoveOperationScope(DailyV1OperationScopeKind.RuntimeProtocol);
        }
    }

    private int activeOperationCount => activeOperationScopes.Count;

    private bool TryAddOperationScope(DailyV1OperationScopeKind kind)
    {
        if (activeOperationScopes.Count == int.MaxValue) return false;
        activeOperationScopes.Add(kind);
        return true;
    }

    private bool TryRemoveOperationScope(DailyV1OperationScopeKind kind)
    {
        for (int index = activeOperationScopes.Count - 1; index >= 0; index--)
        {
            if (activeOperationScopes[index] != kind) continue;
            activeOperationScopes.RemoveAt(index);
            return true;
        }
        return false;
    }

    internal long CompletedCoreSequence => completedCoreSequence;
    internal object RuntimeInstanceIdentity => runtimeInstanceIdentity;
    internal DailyCaptureEligibilityToken CurrentCaptureToken => currentCaptureToken;
    /// <summary>
    /// The mutation epoch paired with this runtime's operation registry,
    /// completed-boundary sequence, and capture token. Continuation census
    /// protocols attached to this runtime must share this exact state.
    /// </summary>
    internal DailyV1MutationEpochState MutationEpochState => mutationEpochState;
    internal DailyV1SelectedRuntimeOwnerGraph SelectedRuntimeOwnerGraph => selectedRuntimeOwnerGraph;
    /// <summary>
    /// Core-owned ordered runtime identity slots used by selected Daily-v1
    /// turns. Null holes and unselected host identities remain in this exact
    /// sequence because actor-choice dispositions persist their ordinals.
    /// </summary>
    internal IReadOnlyList<DailyV1ActorTurnRosterProjection> SelectedRuntimeActorRoster =>
        selectedRuntimeActorRoster;
    internal bool SelectedRuntimeExecutionRootsBound => selectedRuntimeExecutionRootsBound;
    /// <summary>
    /// Whether this admitted state has the complete Core owner/configuration
    /// roots needed by the selected Daily-v1 execution path. Path selection is
    /// derived from Core-owned identities; a Unity adapter cannot opt the
    /// authoritative runtime in or out by reporting its own demographic view.
    /// </summary>
    internal bool HasAdmittedSelectedDailyExecution
    {
        get
        {
            DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
            return runtimeIdentityBound
                && runtimeConfigurationBound
                && AdmissionContext != null
                && AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                && selectedRuntimeOwnerGraphBound
                && selectedRuntimeExecutionRootsBound
                && graph != null
                && graph.Definitions != null
                && ReferenceEquals(graph.Definitions.EffectiveConfiguration, ConfigurationIdentity)
                && graph.PersonStore != null
                && graph.ScheduledDirectives != null
                && graph.SpatialNetwork != null
                && graph.JusticeRecordsOwner != null
                && graph.TravelParties != null
                && graph.RuntimeIdentityIndex != null
                && selectedRuntimeIdAllocator != null
                && selectedRandomSource != null
                && selectedRecordSequence != null
                && selectedDecisionStore != null
                && selectedActorChoiceStore != null
                && ReferenceEquals(selectedActorChoiceStore.PersonStore, graph.PersonStore)
                && HasSelectedCityAndNpcOwners(graph);
        }
    }
    internal RuntimeIdAllocator SelectedRuntimeIdAllocator => selectedRuntimeIdAllocator;
    internal IAuthoritativeRandomSource SelectedRandomSource => selectedRandomSource;
    internal SimulationRecordSequence SelectedRecordSequence => selectedRecordSequence;
    internal NpcDecisionStore SelectedDecisionStore => selectedDecisionStore;
    internal ActorChoiceStore SelectedActorChoiceStore => selectedActorChoiceStore;

    internal bool TryBindSelectedRuntimeOwnerGraph(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (selectedRuntimeOwnerGraphBound)
            return ReferenceEquals(selectedRuntimeOwnerGraph, graph);

        selectedRuntimeOwnerGraph = graph;
        selectedRuntimeOwnerGraphBound = true;
        return true;
    }

    internal bool TryReplaceSelectedRuntimeOwnerGraph(
        DailyV1SelectedRuntimeOwnerGraph expected,
        DailyV1SelectedRuntimeOwnerGraph replacement)
    {
        if (!selectedRuntimeOwnerGraphBound
            || expected == null
            || replacement == null
            || !ReferenceEquals(selectedRuntimeOwnerGraph, expected)
            || !ReferenceEquals(expected.Definitions, replacement.Definitions))
            return false;

        selectedRuntimeOwnerGraph = replacement;
        return true;
    }

    /// <summary>
    /// Binds the runtime's exact actor identity/ordinal projection after
    /// constructor membership has been composed. Domain owner state remains
    /// in the selected graph; this ordered identity roster is the single Core
    /// source for actor-turn ordinals and preserves unselected runtime slots.
    /// </summary>
    internal bool TryBindSelectedRuntimeActorRoster(
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster)
    {
        if (!selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || selectedRuntimeActorRoster != null
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                selectedRuntimeOwnerGraph, runtimeRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> candidate))
            return false;

        selectedRuntimeActorRoster = candidate;
        return true;
    }

    internal bool TryCreateSelectedRuntimeActorRosterWithAppendedMember(
        DailyV1ActorTurnRosterProjection member,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        replacementRoster = null;
        if (selectedRuntimeActorRoster == null || member == null
            || replacementGraph == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count + 1);
        candidate.AddRange(selectedRuntimeActorRoster);
        candidate.Add(member);
        return TryCreateValidatedSelectedRuntimeActorRoster(
            replacementGraph, candidate, out replacementRoster);
    }

    internal bool TryCreateSelectedRuntimeActorRosterWithoutMember(
        string runtimeId,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        replacementRoster = null;
        if (selectedRuntimeActorRoster == null
            || string.IsNullOrWhiteSpace(runtimeId)
            || replacementGraph == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count);
        bool removed = false;
        foreach (DailyV1ActorTurnRosterProjection member in selectedRuntimeActorRoster)
        {
            if (member != null && string.Equals(member.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                if (removed) return false;
                removed = true;
                continue;
            }
            candidate.Add(member);
        }

        return removed && TryCreateValidatedSelectedRuntimeActorRoster(
            replacementGraph, candidate, out replacementRoster);
    }

    /// <summary>
    /// Atomically publishes a new selected graph together with its exact
    /// ordered actor identity roster. Membership callers prepare and validate
    /// both snapshots before invoking this single validation-and-publication point.
    /// </summary>
    internal bool TryReplaceSelectedRuntimeMembership(
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        if (!selectedRuntimeOwnerGraphBound
            || expectedGraph == null
            || replacementGraph == null
            || !ReferenceEquals(selectedRuntimeOwnerGraph, expectedGraph)
            || !ReferenceEquals(selectedRuntimeActorRoster, expectedRoster)
            || !ReferenceEquals(expectedGraph.Definitions, replacementGraph.Definitions)
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                replacementGraph, replacementRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> validatedRoster))
            return false;

        selectedRuntimeOwnerGraph = replacementGraph;
        selectedRuntimeActorRoster = validatedRoster;
        return true;
    }

    private static bool TryCreateValidatedSelectedRuntimeActorRoster(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> roster)
    {
        roster = null;
        if (graph == null || runtimeRoster == null) return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1ActorTurnRosterProjection member in runtimeRoster)
        {
            if (member != null
                && (string.IsNullOrWhiteSpace(member.RuntimeId)
                    || !runtimeIds.Add(member.RuntimeId)))
                return false;
            candidate.Add(member);
        }

        if (!DailyV1ActorTurnExecutionStage.TryBuildRoster(
                graph, candidate, out IReadOnlyList<DailyV1ActorTurnRosterEntry> entries))
            return false;
        List<DailyV1NpcRuntimeOwners> selectedOwners =
            new List<DailyV1NpcRuntimeOwners>(graph.Npcs.Count);
        foreach (DailyV1ActorTurnRosterEntry entry in entries)
            if (entry?.ActorOwner != null)
                selectedOwners.Add(entry.ActorOwner);
        if (!graph.MatchesExactOwnerInstances(graph.Cities, selectedOwners))
            return false;

        roster = candidate.AsReadOnly();
        return true;
    }

    /// <summary>
    /// Binds the selected runtime's mutable decision roots beside its clock,
    /// admission, graph and capture state. Unity may project these same
    /// instances, but it cannot substitute a different RNG, choice store,
    /// allocator, sequence or decision store at a Core execution boundary.
    /// </summary>
    internal bool TryBindSelectedRuntimeExecutionRoots(
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore)
    {
        if (!runtimeIdentityBound || !runtimeConfigurationBound
            || !selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || runtimeIdAllocator == null || randomSource == null
            || recordSequence == null || decisionStore == null || actorChoiceStore == null
            || selectedRuntimeOwnerGraph.PersonStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, selectedRuntimeOwnerGraph.PersonStore))
            return false;

        if (selectedRuntimeExecutionRootsBound)
            return ReferenceEquals(selectedRuntimeIdAllocator, runtimeIdAllocator)
                && ReferenceEquals(selectedRandomSource, randomSource)
                && ReferenceEquals(selectedRecordSequence, recordSequence)
                && ReferenceEquals(selectedDecisionStore, decisionStore)
                && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore);

        selectedRuntimeIdAllocator = runtimeIdAllocator;
        selectedRandomSource = randomSource;
        selectedRecordSequence = recordSequence;
        selectedDecisionStore = decisionStore;
        selectedActorChoiceStore = actorChoiceStore;
        selectedRuntimeExecutionRootsBound = true;
        return true;
    }

    private static bool HasSelectedCityAndNpcOwners(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph.Cities == null || graph.Npcs == null)
            return false;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city == null || city.Population == null
                || !ReferenceEquals(city.Definitions, graph.Definitions))
                return false;
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
            if (npc == null || npc.Lifecycle == null || npc.Travel == null
                || npc.ActionSlot == null || !ReferenceEquals(npc.Definitions, graph.Definitions))
                return false;
        return true;
    }

    internal bool HasExactSelectedRuntimeExecutionRoots(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        IAuthoritativeRandomSource randomSource,
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore)
    {
        return selectedRuntimeExecutionRootsBound
            && ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            && ReferenceEquals(graph?.PersonStore, personStore)
            && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore)
            && ReferenceEquals(actorChoiceStore?.PersonStore, personStore)
            && ReferenceEquals(selectedRandomSource, randomSource)
            && ReferenceEquals(selectedRuntimeIdAllocator, runtimeIdAllocator)
            && ReferenceEquals(selectedRecordSequence, recordSequence)
            && ReferenceEquals(selectedDecisionStore, decisionStore);
    }

    internal bool HasExactSelectedRuntimeActorTurnRoots(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        IAuthoritativeRandomSource randomSource)
    {
        return selectedRuntimeExecutionRootsBound
            && ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            && ReferenceEquals(graph?.PersonStore, personStore)
            && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore)
            && ReferenceEquals(actorChoiceStore?.PersonStore, personStore)
            && ReferenceEquals(selectedRandomSource, randomSource);
    }

    internal bool HasExactSelectedRuntimeOwnerGraph(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners) =>
        selectedRuntimeOwnerGraph != null
        && ReferenceEquals(selectedRuntimeOwnerGraph.Definitions, definitions)
        && selectedRuntimeOwnerGraph.MatchesExactOwnerInstances(cityOwners, npcOwners);

    internal void InvalidateCurrentCaptureToken()
    {
        currentCaptureToken = null;
    }

    internal bool TryCompareCurrentCaptureToken(
        DailyCaptureEligibilityToken token,
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
        long currentMutationEpoch)
    {
        return MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            && token != null
            && ReferenceEquals(token.RuntimeInstanceIdentity, runtimeInstanceIdentity)
            && ReferenceEquals(token.AdmissionContext, admissionContext)
            && ReferenceEquals(token.ConfigurationIdentity, configurationIdentity)
            && ReferenceEquals(token.CalendarIdentity, calendarIdentity)
            && token.CompositionProfile == compositionProfile
            && ReferenceEquals(token.WorldId, worldId)
            && token.AbsoluteDay == absoluteDay
            && token.CompletedCoreSequence == completedCoreSequence
            && token.CompletedCoreSequence > 0L
            && (token.BoundaryProvenance == DailyCaptureBoundaryProvenance.CompletedAdvance
                || token.BoundaryProvenance == DailyCaptureBoundaryProvenance.RestoredContinuation)
            && token.MutationEpoch == currentMutationEpoch
            && AreOwnerSectionsEqual(token.OwnerSections, currentOwnerSections);
    }

    internal static bool AreOwnerSectionsEqual(
        IReadOnlyList<OwnerSectionCensusSnapshot> left,
        IReadOnlyList<OwnerSectionCensusSnapshot> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
        {
            OwnerSectionCensusSnapshot first = left[index];
            OwnerSectionCensusSnapshot second = right[index];
            if (first == null
                || second == null
                || !string.Equals(first.SectionId, second.SectionId, StringComparison.Ordinal)
                || first.SchemaVersion != second.SchemaVersion
                || first.Role != second.Role
                || !ReferenceEquals(first.OwnerInstanceIdentity, second.OwnerInstanceIdentity)
                || first.Cardinality != second.Cardinality
                || first.Revision != second.Revision)
                return false;
        }

        return true;
    }

    internal bool CanRecordCompletedCoreCount(long requestedCoreCount)
    {
        return requestedCoreCount > 0L
            && completedCoreSequence <= long.MaxValue - requestedCoreCount;
    }

    internal bool TryRecordCompletedCore()
    {
        if (completedCoreSequence == long.MaxValue)
            return false;

        completedCoreSequence++;
        return true;
    }

    internal bool TryAcquireAdvanceLease(out DailyV1AdvanceLease lease)
    {
        lease = null;
        if (advanceLease != null)
            return false;

        DailyV1AdvanceLease candidate = new DailyV1AdvanceLease(this);
        advanceLease = candidate;
        advanceLeaseInitialCompletedSequence = completedCoreSequence;
        lease = candidate;
        return true;
    }

    internal bool TryBeginAdvance(bool invalidateCaptureToken, out DailyV1AdvanceLease lease)
    {
        if (!TryAcquireAdvanceLease(out lease)) return false;
        if (invalidateCaptureToken) InvalidateCurrentCaptureToken();
        return true;
    }
    internal void ReleaseAdvanceLease(DailyV1AdvanceLease lease)
    {
        if (ReferenceEquals(advanceLease, lease))
            advanceLease = null;
    }

    internal bool TryInstallRestoredCompletedCoreSequence(long restoredSequence)
    {
        if (restoredSequence <= 0L
            || completedCoreSequence != 0L
            || AdvanceLeaseHeld)
            return false;

        completedCoreSequence = restoredSequence;
        return true;
    }

    internal bool TryCreateCompletedAdvanceToken(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            || completedCoreSequence <= 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        token = new DailyCaptureEligibilityToken(
            runtimeInstanceIdentity,
            admissionContext,
            configurationIdentity,
            calendarIdentity,
            compositionProfile,
            worldId,
            absoluteDay,
            completedCoreSequence,
            DailyCaptureBoundaryProvenance.CompletedAdvance,
            mutationEpoch,
            ownerSections);
        currentCaptureToken = token;
        return true;
    }

    /// <summary>
    /// Issues a completed-advance token only for a positive boundary recorded
    /// during this exact held runtime lease. The P12 census remains the source
    /// of owner snapshots and mutation epoch; this state owns the publication
    /// decision and binds the token to its own admitted runtime identities.
    /// </summary>
    internal bool TryIssueCompletedAdvanceCaptureToken(
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!runtimeIdentityBound
            || !runtimeConfigurationBound
            || AdmissionContext == null
            || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdvanceLeaseHeld
            || currentCaptureToken != null
            || completedCoreSequence <= advanceLeaseInitialCompletedSequence
            || SimulationTime == null
            || mutationEpoch < 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        return TryCreateCompletedAdvanceToken(
            AdmissionContext,
            ConfigurationIdentity,
            CalendarIdentity,
            CompositionProfile,
            WorldId,
            SimulationTime.AbsoluteDay,
            mutationEpoch,
            ownerSections,
            out token);
    }

    internal bool TryCreateRestoredContinuationToken(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long restoredCoreSequence,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            || restoredCoreSequence <= 0L || completedCoreSequence != 0L || AdvanceLeaseHeld
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        DailyCaptureEligibilityToken candidate = new DailyCaptureEligibilityToken(
            runtimeInstanceIdentity,
            admissionContext,
            configurationIdentity,
            calendarIdentity,
            compositionProfile,
            worldId,
            absoluteDay,
            restoredCoreSequence,
            DailyCaptureBoundaryProvenance.RestoredContinuation,
            mutationEpoch,
            ownerSections);
        if (!TryInstallRestoredCompletedCoreSequence(restoredCoreSequence))
            return false;

        currentCaptureToken = candidate;
        token = candidate;
        return true;
    }
}

/// <summary>
/// Idempotent handle for one runtime advance window. Disposing an older handle
/// cannot release a later lease from the same boundary state.
/// </summary>
internal sealed class DailyV1AdvanceLease : IDisposable
{
    private DailyV1ExecutionBoundaryState owner;

    internal DailyV1AdvanceLease(DailyV1ExecutionBoundaryState owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Dispose()
    {
        DailyV1ExecutionBoundaryState current = owner;
        owner = null;
        current?.ReleaseAdvanceLease(this);
    }
}
