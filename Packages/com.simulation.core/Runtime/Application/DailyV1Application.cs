using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

/// <summary>
/// Core-owned lifetime and operation gate for one selected Daily-v1
/// application. Host adapters may retain projections, but this instance owns
/// the exact active Core session and its serialized publication boundary.
/// </summary>
internal sealed class DailyV1Application
{
    private readonly DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession> activeSessionGate =
        new DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>(
            session => session.ExecutionBoundaryState);
    private SimulationRuntimeAdmissionContext bootstrapAdmissionContext;
    private bool initialGenesisPreparationAttempted;
    private DailyV1GenesisComposition preparedInitialGenesis;
    private DailyV1ActiveRuntimeSession preparedInitialSession;

    // Construction remains internal until the complete TryStart path can
    // create and return the application only after genesis or restore commits.
    internal DailyV1Application() { }

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
        TryPrepareInitialGenesis(
            definitions, allocateWorldId: null, stageCompleted,
            out composition, out candidate, out diagnostic);

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
            if (initialGenesisPreparationAttempted
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

        if (!DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                admissionContext,
                allocateWorldId,
                stageCompleted,
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
        if (executionBoundaryState == null || expectedDefinitions == null)
        {
            diagnostic = "The restored Core session requires its exact execution boundary and effective definitions.";
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph ownerGraph = executionBoundaryState.SelectedRuntimeOwnerGraph;
        if (!executionBoundaryState.HasAdmittedSelectedDailyExecution
            || ownerGraph == null
            || !ReferenceEquals(ownerGraph.Definitions, expectedDefinitions))
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
                    out candidate, out diagnostic)
                : DailyV1ActiveRuntimeSession.TryCreateRestored(
                    executionBoundaryState, expectedDefinitions, lastEconomySnapshotDay,
                    p9Manifest, spatialAuthority, out candidate, out diagnostic);
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
        candidate = null;
        calendar = null;
        diagnostic = null;
        if (definitions == null
            || !ReferenceEquals(definitions.EffectiveConfiguration, effectiveConfiguration))
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
                replacementOperation,
                out DailyV1ActiveRuntimeSession candidateSession,
                out diagnostic))
            return false;

        candidate = candidateSession;
        calendar = candidateCalendar;
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

    internal bool TryBeginExclusiveOperation(
        DailyV1ActiveRuntimeSession expectedSession,
        out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation operation) =>
        activeSessionGate.TryBeginExclusiveOperation(expectedSession, out operation);

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
                    && hostPorts.TryBindApplicationAdvanceAuthority(this, candidateSession);
            }
            if (!IsBootstrapOrRestoredDescendant(candidateSession.AdmissionContext)
                || activeSessionGate.ActiveOperationCount != 0
                || (current != null && !current.IsProfileCompatibleWith(candidateSession)))
                return false;
            return candidateSession.TryBindSelectedRuntimeAdvanceHostPorts(hostPorts)
                && hostPorts.TryBindApplicationAdvanceAuthority(this, candidateSession);
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
        out string diagnostic)
    {
        diagnostic = null;
        if (candidate == null)
        {
            diagnostic = "The Core application cannot commit a null restored session.";
            return false;
        }

        lock (activeSessionGate.SyncRoot)
        {
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

    private bool IsBootstrapOrRestoredDescendant(
        SimulationRuntimeAdmissionContext candidate)
    {
        if (ReferenceEquals(candidate, bootstrapAdmissionContext)) return true;
        return candidate != null
            && candidate.IsRestoredContinuationOf(bootstrapAdmissionContext);
    }
}
