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
    DailyAdvance = 2
}

/// <summary>
/// Owns the per-runtime single-writer advance lease and completed Daily-v1
/// core sequence. This is execution state, not a thread lock or persisted data.
/// </summary>
internal sealed class DailyV1ExecutionBoundaryState
{
    private readonly object runtimeInstanceIdentity = new object();
    private readonly object operationSyncRoot = new object();
    private readonly List<DailyV1OperationScopeKind> activeOperationScopes =
        new List<DailyV1OperationScopeKind>();
    private bool runtimeIdentityBound;
    private bool runtimeConfigurationBound;
    private DailyV1AdvanceLease advanceLease;
    private long completedCoreSequence;
    private DailyCaptureEligibilityToken currentCaptureToken;
    private bool selectedRuntimeOwnerGraphBound;
    private DailyV1SelectedRuntimeOwnerGraph selectedRuntimeOwnerGraph;

    internal SimulationTime SimulationTime { get; private set; }
    internal SimulationRuntimeAdmissionContext AdmissionContext { get; private set; }
    internal SimulationRuntimeCompositionProfile CompositionProfile { get; private set; }
    internal WorldId WorldId { get; private set; }
    internal EffectiveSimulationConfiguration ConfigurationIdentity { get; private set; }
    internal SimulationCalendar CalendarIdentity { get; private set; }

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
            if (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread())
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

    internal bool TryAssessCaptureScope(DailyV1CaptureScopeAllowance allowance)
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread())
                return false;
            if (allowance == DailyV1CaptureScopeAllowance.None)
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
            if (AdmissionContext == null || !AdmissionContext.IsOwnedByCurrentThread())
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
    internal DailyV1SelectedRuntimeOwnerGraph SelectedRuntimeOwnerGraph => selectedRuntimeOwnerGraph;

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
