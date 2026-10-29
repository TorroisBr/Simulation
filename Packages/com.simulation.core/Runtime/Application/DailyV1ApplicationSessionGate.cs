using System;
using System.Threading;

/// <summary>
/// Mutable reporting progress kept separate from the immutable active-session
/// reference set. Hosts may read a snapshot and update it only while operating
/// on the owning session thread.
/// </summary>
internal sealed class DailyV1SessionReportState
{
    internal long LastEconomySnapshotDay { get; private set; }

    internal DailyV1SessionReportState(long lastEconomySnapshotDay)
    {
        LastEconomySnapshotDay = lastEconomySnapshotDay;
    }

    internal void MarkEconomySnapshotDay(long day)
    {
        LastEconomySnapshotDay = day;
    }
}

/// <summary>
/// Retains the exact P12-C manifest and spatial authority associated with one
/// active selected runtime. Restored sessions retain these reconstructed roots
/// without claiming to retain an initial genesis composition.
/// </summary>
internal sealed class DailyV1P12CRuntimeRoots
{
    private DailyV1P12CRuntimeRoots(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        P12CP9GenesisManifestCoreSnapshot manifest,
        SpatialAuthorityStore spatialAuthority)
    {
        ExecutionBoundaryState = executionBoundaryState;
        Manifest = manifest;
        SpatialAuthority = spatialAuthority;
    }

    internal DailyV1ExecutionBoundaryState ExecutionBoundaryState { get; }
    internal P12CP9GenesisManifestCoreSnapshot Manifest { get; }
    internal SpatialAuthorityStore SpatialAuthority { get; }

    internal bool IsFor(DailyV1ActiveRuntimeSession session) =>
        session != null
        && ReferenceEquals(ExecutionBoundaryState, session.ExecutionBoundaryState)
        && Manifest != null
        && Manifest.TryValidate(out _)
        && Manifest.P12AdmissionProfile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
        && string.Equals(Manifest.SelectedP9ProfileFingerprint,
            session.ProfileFingerprint, StringComparison.Ordinal)
        && SpatialAuthority != null
        && ExecutionBoundaryState.HasAdmittedSelectedDailyExecution
        && ReferenceEquals(ExecutionBoundaryState.SelectedRuntimeOwnerGraph, session.OwnerGraph)
        && ExecutionBoundaryState.HasExactSelectedRuntimeExecutionRoots(
            session.OwnerGraph,
            session.OwnerGraph?.PersonStore,
            ExecutionBoundaryState.SelectedActorChoiceStore,
            ExecutionBoundaryState.SelectedRandomSource,
            ExecutionBoundaryState.SelectedRuntimeIdAllocator,
            ExecutionBoundaryState.SelectedRecordSequence,
            ExecutionBoundaryState.SelectedDecisionStore);

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions definitions,
        P12CP9GenesisManifestCoreSnapshot manifest,
        SpatialAuthorityStore spatialAuthority,
        out DailyV1P12CRuntimeRoots roots,
        out string diagnostic)
    {
        roots = null;
        diagnostic = null;
        if (executionBoundaryState == null || definitions == null
            || manifest == null || spatialAuthority == null)
        {
            diagnostic = "The exact selected P12-C execution state, definitions, manifest, and spatial authority are required.";
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph graph = executionBoundaryState.SelectedRuntimeOwnerGraph;
        string profileFingerprint = DailyV1DefinitionIdentity.CreateP9Fingerprint(definitions, out _);
        if (!executionBoundaryState.HasAdmittedSelectedDailyExecution
            || !ReferenceEquals(graph?.Definitions, definitions)
            || !executionBoundaryState.HasExactSelectedRuntimeExecutionRoots(
                graph,
                graph?.PersonStore,
                executionBoundaryState.SelectedActorChoiceStore,
                executionBoundaryState.SelectedRandomSource,
                executionBoundaryState.SelectedRuntimeIdAllocator,
                executionBoundaryState.SelectedRecordSequence,
                executionBoundaryState.SelectedDecisionStore)
            || !manifest.TryValidate(out diagnostic)
            || manifest.P12AdmissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !string.Equals(manifest.SelectedP9ProfileFingerprint, profileFingerprint,
                StringComparison.Ordinal))
        {
            diagnostic = diagnostic ?? "The P12-C manifest and root identities do not match the exact admitted Daily-v1 execution state.";
            return false;
        }

        if (!P12CSpatialAuthoritySnapshot.TryCapture(
                executionBoundaryState.AdmissionContext.Profile,
                manifest.SelectedP9ContractIdentity,
                manifest.SelectedP9SchemaVersion,
                spatialAuthority,
                out _,
                out P12CSpatialAuthoritySnapshotFailure spatialFailure))
        {
            diagnostic = "The retained P12-C spatial authority is invalid for its P9 manifest: "
                + (spatialFailure?.Message ?? "Unknown spatial-authority validation failure.");
            return false;
        }

        roots = new DailyV1P12CRuntimeRoots(executionBoundaryState, manifest, spatialAuthority);
        diagnostic = null;
        return true;
    }
}

/// <summary>
/// Core-owned binding for one published selected Daily-v1 runtime session.
/// It retains the exact execution boundary and owner graph already created by
/// genesis or staged restoration; it does not rebuild or mirror their state.
/// Unity host sessions may keep presentation/projection references separately,
/// but their operation gate must resolve through this exact Core boundary.
/// </summary>
internal sealed class DailyV1ActiveRuntimeSession
{
    private IDailyV1RuntimeAdvanceEvidenceBoundary runtimeAdvanceBoundary;

    private DailyV1ActiveRuntimeSession(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions definitions,
        string profileFingerprint,
        long lastEconomySnapshotDay,
        DailyV1GenesisComposition genesisComposition,
        DailyV1P12CRuntimeRoots p12CRoots,
        DailyV1DomainEventAuthority eventAuthority)
    {
        ExecutionBoundaryState = executionBoundaryState;
        Definitions = definitions;
        ProfileFingerprint = profileFingerprint;
        ReportState = new DailyV1SessionReportState(lastEconomySnapshotDay);
        GenesisComposition = genesisComposition;
        P12CRoots = p12CRoots;
        EventAuthority = eventAuthority ?? throw new ArgumentNullException(nameof(eventAuthority));
    }

    internal DailyV1ExecutionBoundaryState ExecutionBoundaryState { get; }
    internal DailyV1SessionReportState ReportState { get; }
    internal DailyV1GenesisComposition GenesisComposition { get; }
    internal DailyV1P12CRuntimeRoots P12CRoots { get; }
    internal DailyV1DomainEventAuthority EventAuthority { get; }
    internal DailyV1SelectedRuntimeOwnerGraph OwnerGraph =>
        ExecutionBoundaryState.SelectedRuntimeOwnerGraph;
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal SimulationRuntimeAdmissionContext AdmissionContext => ExecutionBoundaryState.AdmissionContext;
    internal WorldId WorldId => ExecutionBoundaryState.WorldId;
    internal SimulationTime SimulationTime => ExecutionBoundaryState.SimulationTime;
    internal EffectiveSimulationConfiguration EffectiveConfiguration => ExecutionBoundaryState.ConfigurationIdentity;
    internal SimulationCalendar Calendar => ExecutionBoundaryState.CalendarIdentity;
    internal string ProfileFingerprint { get; }
    internal IDailyV1RuntimeAdvanceEvidenceBoundary RuntimeAdvanceBoundary => runtimeAdvanceBoundary;

    /// <summary>
    /// Binds the session once to the host boundary constructed for this exact
    /// Core execution state. The application establishes this association
    /// before publication so an advance cannot substitute another host
    /// mutation or capture authority.
    /// </summary>
    internal bool TryBindSelectedRuntimeAdvanceHostPorts(
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts)
    {
        if (hostPorts == null
            || !ReferenceEquals(hostPorts.ExecutionState, ExecutionBoundaryState)
            || !ExecutionBoundaryState.HasAdmittedSelectedDailyExecution
            || ExecutionBoundaryState.SelectedRuntimeOwnerGraph == null
            || ExecutionBoundaryState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            return false;

        if (runtimeAdvanceBoundary == null)
        {
            runtimeAdvanceBoundary = new DailyV1CoreSelectedRuntimeAdvanceBoundary(
                ExecutionBoundaryState, hostPorts);
            return true;
        }

        return runtimeAdvanceBoundary is DailyV1CoreSelectedRuntimeAdvanceBoundary coreBoundary
            && coreBoundary.IsBoundTo(ExecutionBoundaryState, hostPorts);
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        out DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        session = null;
        diagnostic = null;
        if (executionBoundaryState == null)
        {
            diagnostic = "The active Core session requires its exact execution boundary.";
            return false;
        }

        DailyV1DomainEventAuthority eventAuthority;
        try
        {
            eventAuthority = DailyV1DomainEventAuthority.Create(
                executionBoundaryState.SelectedRuntimeIdAllocator,
                executionBoundaryState.SimulationTime,
                executionBoundaryState.SelectedRecordSequence);
        }
        catch (ArgumentException exception)
        {
            diagnostic = "The Core session could not create its exact event/history authority: "
                + exception.Message;
            return false;
        }

        return TryCreate(executionBoundaryState, expectedDefinitions,
            lastEconomySnapshotDay, eventAuthority, out session, out diagnostic);
    }

    internal static bool TryCreate(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        DailyV1DomainEventAuthority eventAuthority,
        out DailyV1ActiveRuntimeSession session,
        out string diagnostic) =>
        TryCreateCore(executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
            null, null, eventAuthority, out session, out diagnostic);

    internal static bool TryCreate(
        DailyV1GenesisComposition genesisComposition,
        long lastEconomySnapshotDay,
        out DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        if (genesisComposition == null)
        {
            session = null;
            diagnostic = "The active Core session requires its exact genesis composition.";
            return false;
        }

        if (!DailyV1P12CRuntimeRoots.TryCreate(
                genesisComposition.ExecutionBoundaryState,
                genesisComposition.Definitions,
                genesisComposition.P9ManifestSnapshot,
                genesisComposition.SpatialAuthority,
                out DailyV1P12CRuntimeRoots p12CRoots,
                out diagnostic))
        {
            session = null;
            return false;
        }

        return TryCreateCore(genesisComposition.ExecutionBoundaryState,
            genesisComposition.Definitions, lastEconomySnapshotDay,
            genesisComposition, p12CRoots, genesisComposition.EventAuthority,
            out session, out diagnostic);
    }

    internal static bool TryCreateRestored(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        P12CP9GenesisManifestCoreSnapshot p9Manifest,
        SpatialAuthorityStore spatialAuthority,
        DailyV1DomainEventAuthority eventAuthority,
        out DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        session = null;
        if (!DailyV1P12CRuntimeRoots.TryCreate(
                executionBoundaryState, expectedDefinitions, p9Manifest, spatialAuthority,
                out DailyV1P12CRuntimeRoots p12CRoots, out diagnostic))
            return false;

        return TryCreateCore(executionBoundaryState, expectedDefinitions,
            lastEconomySnapshotDay, null, p12CRoots, eventAuthority,
            out session, out diagnostic);
    }

    private static bool TryCreateCore(
        DailyV1ExecutionBoundaryState executionBoundaryState,
        DailyV1EffectiveDefinitions expectedDefinitions,
        long lastEconomySnapshotDay,
        DailyV1GenesisComposition genesisComposition,
        DailyV1P12CRuntimeRoots p12CRoots,
        DailyV1DomainEventAuthority eventAuthority,
        out DailyV1ActiveRuntimeSession session,
        out string diagnostic)
    {
        session = null;
        diagnostic = null;
        if (executionBoundaryState == null || expectedDefinitions == null)
        {
            diagnostic = "The active Core session requires its exact execution boundary and effective definitions.";
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph ownerGraph = executionBoundaryState.SelectedRuntimeOwnerGraph;
        SimulationRuntimeAdmissionContext admissionContext = executionBoundaryState.AdmissionContext;
        if (!executionBoundaryState.HasAdmittedSelectedDailyExecution
            || ownerGraph == null
            || !ReferenceEquals(ownerGraph.Definitions, expectedDefinitions)
            || admissionContext == null
            || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || executionBoundaryState.WorldId == null
            || executionBoundaryState.SimulationTime == null
            || executionBoundaryState.ConfigurationIdentity == null
            || executionBoundaryState.CalendarIdentity == null
            || eventAuthority == null
            || !eventAuthority.IsFor(
                executionBoundaryState.SelectedRuntimeIdAllocator,
                executionBoundaryState.SimulationTime,
                executionBoundaryState.SelectedRecordSequence))
        {
            diagnostic = "The Core execution boundary is not the exact admitted selected Daily-v1 runtime for these definitions.";
            return false;
        }

        string profileFingerprint = DailyV1DefinitionIdentity.CreateP9Fingerprint(
            expectedDefinitions, out _);
        if (string.IsNullOrWhiteSpace(profileFingerprint))
        {
            diagnostic = "The Core active session has no selected profile fingerprint.";
            return false;
        }

        if (genesisComposition != null
            && (!ReferenceEquals(genesisComposition.ExecutionBoundaryState, executionBoundaryState)
                || !ReferenceEquals(genesisComposition.Definitions, expectedDefinitions)
                || !ReferenceEquals(genesisComposition.OwnerGraph, ownerGraph)))
        {
            diagnostic = "The retained genesis roots must be the exact admitted runtime boundary and owner graph.";
            return false;
        }

        DailyV1ActiveRuntimeSession candidateSession = new DailyV1ActiveRuntimeSession(
            executionBoundaryState, expectedDefinitions, profileFingerprint,
            lastEconomySnapshotDay, genesisComposition, p12CRoots, eventAuthority);
        if (p12CRoots != null && !p12CRoots.IsFor(candidateSession))
        {
            diagnostic = "The active Core session does not retain its exact P12-C roots.";
            return false;
        }
        if (!eventAuthority.TryBindSelectedTravelEventRecorders(executionBoundaryState))
        {
            diagnostic = "The Core active session could not bind its exact travel event recorder.";
            return false;
        }

        session = candidateSession;
        return true;
    }

    internal bool IsProfileCompatibleWith(DailyV1ActiveRuntimeSession candidate)
    {
        return candidate != null
            && AdmissionContext != null
            && candidate.AdmissionContext != null
            && AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && candidate.AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            && string.Equals(ProfileFingerprint, candidate.ProfileFingerprint, StringComparison.Ordinal);
    }
}

/// <summary>
/// Owns the active-session reference and operation serialization shared by the
/// Unity and detached hosts. The session type stays internal to the owning
/// application assembly; this gate owns its publication lifecycle only.
/// </summary>
internal sealed class DailyV1ApplicationSessionGate<TSession>
    where TSession : class
{
    private readonly object syncRoot = new object();
    private readonly Func<TSession, DailyV1ExecutionBoundaryState> executionState;
    private TSession current;
    private object exclusiveOperationToken;
    private DailyV1ExecutionBoundaryState activeExclusiveState;
    private object runtimeAdvanceToken;

    internal DailyV1ApplicationSessionGate(
        Func<TSession, DailyV1ExecutionBoundaryState> executionState)
    {
        this.executionState = executionState
            ?? throw new ArgumentNullException(nameof(executionState));
    }

    internal object SyncRoot => syncRoot;

    internal TSession Current => Volatile.Read(ref current);

    internal int ActiveOperationCount
    {
        get { lock (syncRoot) return GetExecutionState(current)?.ActiveOperationCount ?? 0; }
    }

    internal bool TryBeginOperation(TSession expectedSession, out TSession session) =>
        TryBeginOperation(expectedSession, DailyV1OperationScopeKind.Application, out session);

    internal bool TryBeginOperation(
        TSession expectedSession,
        DailyV1OperationScopeKind scopeKind,
        out TSession session)
    {
        lock (syncRoot)
        {
            session = current;
            if (exclusiveOperationToken != null
                || runtimeAdvanceToken != null
                || session == null
                || (expectedSession != null && !ReferenceEquals(session, expectedSession)))
            {
                session = null;
                return false;
            }

            DailyV1ExecutionBoundaryState state = GetExecutionState(session);
            if (state == null || !state.TryBeginOperation(scopeKind))
            {
                session = null;
                return false;
            }
            return true;
        }
    }

    internal void EndOperation() => EndOperation(DailyV1OperationScopeKind.Application);

    internal void EndOperation(DailyV1OperationScopeKind scopeKind)
    {
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState state = GetExecutionState(current);
            if (state == null || !state.TryEndOperation(scopeKind))
                throw new InvalidOperationException("Active-session operation scope underflow.");
        }
    }

    internal bool TryPublishInitial(TSession candidateSession)
    {
        if (candidateSession == null) return false;
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState state = GetExecutionState(candidateSession);
            if (exclusiveOperationToken != null
                || runtimeAdvanceToken != null
                || state == null || state.ActiveOperationCount != 0 || current != null) return false;
            return ReferenceEquals(
                Interlocked.CompareExchange(ref current, candidateSession, null), null);
        }
    }

    /// <summary>
    /// Reserves the current idle session while a detached candidate is loaded,
    /// validated, and prepared for publication. Unlike holding SyncRoot over
    /// candidate construction, the reservation rejects same-thread callback
    /// re-entry as well as callers on other threads.
    /// </summary>
    internal bool TryBeginExclusiveOperation(
        TSession expectedSession,
        out ExclusiveOperation operation)
    {
        operation = null;
        if (expectedSession == null) return false;
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState state = GetExecutionState(current);
            if (exclusiveOperationToken != null
                || runtimeAdvanceToken != null
                || !ReferenceEquals(current, expectedSession)
                || state == null
                || state.ActiveOperationCount != 0
                || !state.TryBeginExclusiveSessionReplacement())
                return false;

            object token = new object();
            exclusiveOperationToken = token;
            activeExclusiveState = state;
            operation = new ExclusiveOperation(this, token, expectedSession);
            return true;
        }
    }

    /// <summary>Publishes only through the exclusive reservation that staged this replacement.</summary>
    internal bool TryPublishReplacement(
        ExclusiveOperation operation,
        TSession candidateSession)
    {
        if (operation == null || candidateSession == null) return false;
        lock (syncRoot)
        {
            if (!operation.IsOwnedBy(this, exclusiveOperationToken)
                || runtimeAdvanceToken != null
                || !operation.IsOwnedByCurrentThread()
                || !ReferenceEquals(current, operation.ExpectedSession))
                return false;

            DailyV1ExecutionBoundaryState currentState = GetExecutionState(current);
            DailyV1ExecutionBoundaryState candidateState = GetExecutionState(candidateSession);
            if (currentState == null || candidateState == null
                || currentState.ActiveOperationCount != 0
                || candidateState.ActiveOperationCount != 0)
                return false;

            Interlocked.Exchange(ref current, candidateSession);
            return true;
        }
    }

    internal bool TryPublishReplacement(TSession expectedSession, TSession candidateSession)
    {
        if (expectedSession == null || candidateSession == null) return false;
        lock (syncRoot)
        {
            if (exclusiveOperationToken != null || runtimeAdvanceToken != null) return false;
            DailyV1ExecutionBoundaryState currentState = GetExecutionState(current);
            DailyV1ExecutionBoundaryState candidateState = GetExecutionState(candidateSession);
            if (currentState == null || candidateState == null
                || currentState.ActiveOperationCount != 0
                || candidateState.ActiveOperationCount != 0
                || !ReferenceEquals(current, expectedSession))
                return false;
            Interlocked.Exchange(ref current, candidateSession);
            return true;
        }
    }

    /// <summary>
    /// Reserves the exact active session for one Core-owned daily advance
    /// without adding another P12 operation scope. The runtime advance stage
    /// owns the single DailyAdvance scope used by capture eligibility.
    /// </summary>
    internal bool TryBeginRuntimeAdvance(
        TSession expectedSession,
        out RuntimeAdvanceOperation operation)
    {
        operation = null;
        if (expectedSession == null) return false;
        lock (syncRoot)
        {
            DailyV1ExecutionBoundaryState state = GetExecutionState(current);
            if (exclusiveOperationToken != null
                || runtimeAdvanceToken != null
                || !ReferenceEquals(current, expectedSession)
                || state == null
                || state.ActiveOperationCount != 0
                || state.AdvanceLeaseHeld
                || (state.AdmissionContext != null
                    && !state.AdmissionContext.IsOwnedByCurrentThread()))
                return false;

            object token = new object();
            runtimeAdvanceToken = token;
            operation = new RuntimeAdvanceOperation(this, token, expectedSession);
            return true;
        }
    }

    private void EndRuntimeAdvance(object token, TSession expectedSession)
    {
        lock (syncRoot)
        {
            if (token == null
                || !ReferenceEquals(runtimeAdvanceToken, token)
                || !ReferenceEquals(current, expectedSession))
                throw new InvalidOperationException("The active-session runtime advance reservation is no longer current.");
            runtimeAdvanceToken = null;
        }
    }

    private DailyV1ExecutionBoundaryState GetExecutionState(TSession session) =>
        session == null ? null : executionState(session);

    private void EndExclusiveOperation(object token)
    {
        lock (syncRoot)
        {
            if (token == null
                || !ReferenceEquals(exclusiveOperationToken, token)
                || activeExclusiveState == null
                || !activeExclusiveState.TryEndExclusiveSessionReplacement())
                throw new InvalidOperationException("The active-session exclusive operation is no longer current.");
            exclusiveOperationToken = null;
            activeExclusiveState = null;
        }
    }

    private bool AuthorizesSessionReplacementCapture(
        ExclusiveOperation operation,
        object token,
        TSession expectedSession,
        DailyV1ExecutionBoundaryState expectedState)
    {
        if (operation == null || expectedSession == null || expectedState == null
            || !operation.IsOwnedBy(this, token)
            || !ReferenceEquals(operation.ExpectedSession, expectedSession)
            || !operation.IsOwnedByCurrentThread())
            return false;

        lock (syncRoot)
        {
            return ReferenceEquals(exclusiveOperationToken, token)
                && ReferenceEquals(current, expectedSession)
                && ReferenceEquals(activeExclusiveState, expectedState)
                && ReferenceEquals(GetExecutionState(expectedSession), expectedState);
        }
    }

    internal sealed class ExclusiveOperation : IDisposable
    {
        private DailyV1ApplicationSessionGate<TSession> gate;
        private readonly object token;
        private readonly Thread ownerThread;
        private readonly int ownerThreadId;

        internal TSession ExpectedSession { get; }

        internal ExclusiveOperation(
            DailyV1ApplicationSessionGate<TSession> gate,
            object token,
            TSession expectedSession)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.token = token ?? throw new ArgumentNullException(nameof(token));
            ExpectedSession = expectedSession ?? throw new ArgumentNullException(nameof(expectedSession));
            ownerThread = Thread.CurrentThread;
            ownerThreadId = ownerThread.ManagedThreadId;
        }

        internal bool IsOwnedBy(
            DailyV1ApplicationSessionGate<TSession> expectedGate,
            object expectedToken) =>
            ReferenceEquals(gate, expectedGate)
            && ReferenceEquals(token, expectedToken);

        internal bool IsOwnedByCurrentThread() =>
            ReferenceEquals(ownerThread, Thread.CurrentThread)
            && ownerThreadId == Thread.CurrentThread.ManagedThreadId;

        /// <summary>
        /// Grants read-only completed-boundary validation to P12-G staging
        /// only while this exact replacement reservation is active on its
        /// owner thread. It does not grant ordinary P12 capture eligibility.
        /// </summary>
        internal bool AuthorizesSessionReplacementCapture(DailyV1ExecutionBoundaryState expectedState)
        {
            DailyV1ApplicationSessionGate<TSession> owner = gate;
            return owner != null
                && IsOwnedByCurrentThread()
                && owner.AuthorizesSessionReplacementCapture(
                    this, token, ExpectedSession, expectedState);
        }

        public void Dispose()
        {
            DailyV1ApplicationSessionGate<TSession> owner = gate;
            if (owner == null) return;
            if (!IsOwnedByCurrentThread())
                throw new InvalidOperationException("The active-session exclusive operation must close on its owner thread.");
            owner.EndExclusiveOperation(token);
            gate = null;
        }
    }

    internal sealed class RuntimeAdvanceOperation : IDisposable
    {
        private DailyV1ApplicationSessionGate<TSession> gate;
        private readonly object token;
        private readonly Thread ownerThread;
        private readonly int ownerThreadId;
        private readonly TSession expectedSession;

        internal RuntimeAdvanceOperation(
            DailyV1ApplicationSessionGate<TSession> gate,
            object token,
            TSession expectedSession)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.token = token ?? throw new ArgumentNullException(nameof(token));
            this.expectedSession = expectedSession
                ?? throw new ArgumentNullException(nameof(expectedSession));
            ownerThread = Thread.CurrentThread;
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        internal TSession ExpectedSession => expectedSession;

        internal bool IsOwnedByCurrentThread() =>
            ReferenceEquals(ownerThread, Thread.CurrentThread)
            && ownerThreadId == Thread.CurrentThread.ManagedThreadId;

        public void Dispose()
        {
            DailyV1ApplicationSessionGate<TSession> owner = gate;
            if (owner == null) return;
            if (!IsOwnedByCurrentThread())
                throw new InvalidOperationException("The active-session runtime advance must close on its owner thread.");
            owner.EndRuntimeAdvance(token, expectedSession);
            gate = null;
        }
    }
}
