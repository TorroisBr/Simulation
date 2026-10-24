using System;
using System.Collections.Generic;

/// <summary>
/// Opaque identity for one reconstruction staging attempt at an exact
/// completed Daily-v1 boundary. C, D, and E retain the same instance so roots
/// from separate attempts cannot be combined accidentally.
/// </summary>
internal sealed class DailyCaptureStagingAttempt
{
    private readonly SimulationRuntime sourceRuntime;
    private readonly DailyCaptureEligibilityToken token;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections;
    private readonly DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation;

    private DailyCaptureStagingAttempt(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation)
    {
        this.sourceRuntime = sourceRuntime;
        this.token = token;
        this.ownerSections = ownerSections;
        this.replacementOperation = replacementOperation;
    }

    internal static bool TryBegin(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureStagingAttempt attempt,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation = null)
    {
        attempt = null;
        if (sourceRuntime == null || token == null || ownerSections == null
            || !ReferenceEquals(token.OwnerSections, ownerSections)
            || !ReferenceEquals(token.WorldId, sourceRuntime.WorldId)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || !sourceRuntime.TryValidateCompletedDailyCaptureTokenForP12Capture(
                token, replacementOperation, out _))
            return false;

        attempt = new DailyCaptureStagingAttempt(
            sourceRuntime, token, ownerSections, replacementOperation);
        return true;
    }

    internal bool IsCurrentFor(
        SimulationRuntime candidateRuntime,
        DailyCaptureEligibilityToken candidateToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> candidateOwnerSections)
    {
        return ReferenceEquals(sourceRuntime, candidateRuntime)
            && ReferenceEquals(token, candidateToken)
            && ReferenceEquals(ownerSections, candidateOwnerSections)
            && candidateRuntime != null
            && candidateRuntime.TryValidateCompletedDailyCaptureTokenForP12Capture(
                candidateToken, replacementOperation, out _);
    }

    internal bool IsCurrent => IsCurrentFor(sourceRuntime, token, ownerSections);

    internal static bool IsCaptureCurrent(
        SimulationRuntime candidateRuntime,
        DailyCaptureEligibilityToken candidateToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> candidateOwnerSections,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        if (stagingAttempt != null)
            return stagingAttempt.IsCurrentFor(candidateRuntime, candidateToken, candidateOwnerSections);
        return candidateRuntime != null
            && candidateRuntime.TryValidateCompletedDailyCaptureTokenForP12Capture(
                candidateToken, out _);
    }
}

/// <summary>
/// One privately staged set of the already-supported P12-C continuation roots.
/// It is not published to an active SimulationRuntime.
/// </summary>
internal sealed class P12CStagedContinuationRoot
{
    private readonly WorldId legacyWorldIdentity;
    private readonly RuntimeIdAllocator legacyRuntimeIdAllocator;
    private readonly SimulationRecordSequence legacyRecordSequence;
    private readonly SpatialAuthorityStore legacySpatialAuthority;
    private readonly DeterministicRandomSource legacyDeterministicRandom;

    internal P12CStagedContinuationRoot(
        DailyCaptureStagingAttempt stagingAttempt,
        P12CStagedCoreContinuationRoot coreRoot,
        SimulationGenesisManifest genesisManifest)
    {
        StagingAttempt = stagingAttempt;
        CoreRoot = coreRoot ?? throw new ArgumentNullException(nameof(coreRoot));
        GenesisManifest = genesisManifest ?? throw new ArgumentNullException(nameof(genesisManifest));
    }

    // Retains the partial fixture shell used by the P12-E package tests. A root
    // returned by P12CContinuationRootStager always uses the Core-owned overload.
    internal P12CStagedContinuationRoot(
        DailyCaptureStagingAttempt stagingAttempt,
        WorldId worldIdentity,
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationRecordSequence recordSequence,
        SpatialAuthorityStore spatialAuthority,
        SimulationGenesisManifest genesisManifest,
        DeterministicRandomSource deterministicRandom)
    {
        StagingAttempt = stagingAttempt;
        legacyWorldIdentity = worldIdentity;
        legacyRuntimeIdAllocator = runtimeIdAllocator;
        legacyRecordSequence = recordSequence;
        legacySpatialAuthority = spatialAuthority;
        GenesisManifest = genesisManifest;
        legacyDeterministicRandom = deterministicRandom;
    }

    internal DailyCaptureStagingAttempt StagingAttempt { get; }
    /// <summary>True only for roots staged from a persisted typed image, without a live capture attempt.</summary>
    internal bool IsDetached => CoreRoot == null ? StagingAttempt == null : CoreRoot.IsDetached;
    internal P12CStagedCoreContinuationRoot CoreRoot { get; }
    internal WorldId WorldIdentity => CoreRoot == null ? legacyWorldIdentity : CoreRoot.WorldIdentity;
    internal RuntimeIdAllocator RuntimeIdAllocator => CoreRoot == null ? legacyRuntimeIdAllocator : CoreRoot.RuntimeIdAllocator;
    internal SimulationRecordSequence RecordSequence => CoreRoot == null ? legacyRecordSequence : CoreRoot.RecordSequence;
    internal SpatialAuthorityStore SpatialAuthority => CoreRoot == null ? legacySpatialAuthority : CoreRoot.SpatialAuthority;
    internal SimulationGenesisManifest GenesisManifest { get; }
    internal DeterministicRandomSource DeterministicRandom => CoreRoot == null ? legacyDeterministicRandom : CoreRoot.DeterministicRandom;
}

/// <summary>
/// Composes the accepted Daily-v1 P12-C owner snapshots into private staged
/// roots, rejecting any cross-owner disagreement before returning a bundle.
/// </summary>
internal static class P12CContinuationRootStager
{
    internal static bool TryStage(
        DailyCaptureStagingAttempt stagingAttempt,
        P12CWorldIdentitySnapshot worldIdentitySnapshot,
        RuntimeIdAllocatorSnapshot allocatorSnapshot,
        SimulationRecordSequenceSnapshot recordSequenceSnapshot,
        P12CSpatialAuthoritySnapshot spatialSnapshot,
        P12CP9GenesisManifestSnapshot genesisManifestSnapshot,
        DeterministicRandomRootSnapshot randomRootSnapshot,
        out P12CStagedContinuationRoot staged,
        out string diagnostic)
    {
        return TryStageForRestore(
            stagingAttempt,
            worldIdentitySnapshot,
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            genesisManifestSnapshot,
            randomRootSnapshot,
            out staged,
            out diagnostic,
            null);
    }

    internal static bool TryStageForRestore(
        DailyCaptureStagingAttempt stagingAttempt,
        P12CWorldIdentitySnapshot worldIdentitySnapshot,
        RuntimeIdAllocatorSnapshot allocatorSnapshot,
        SimulationRecordSequenceSnapshot recordSequenceSnapshot,
        P12CSpatialAuthoritySnapshot spatialSnapshot,
        P12CP9GenesisManifestSnapshot genesisManifestSnapshot,
        DeterministicRandomRootSnapshot randomRootSnapshot,
        out P12CStagedContinuationRoot staged,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver)
    {
        return TryStageCore(
            stagingAttempt,
            worldIdentitySnapshot,
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            genesisManifestSnapshot,
            randomRootSnapshot,
            true,
            out staged,
            out diagnostic,
            stageObserver);
    }

    /// <summary>
    /// Reconstructs C roots from detached typed image values. This entry has no
    /// source runtime, token, owner vector, or capture-attempt dependency.
    /// </summary>
    internal static bool TryStageForImage(
        P12CWorldIdentitySnapshot worldIdentitySnapshot,
        RuntimeIdAllocatorSnapshot allocatorSnapshot,
        SimulationRecordSequenceSnapshot recordSequenceSnapshot,
        P12CSpatialAuthoritySnapshot spatialSnapshot,
        P12CP9GenesisManifestSnapshot genesisManifestSnapshot,
        DeterministicRandomRootSnapshot randomRootSnapshot,
        out P12CStagedContinuationRoot staged,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        return TryStageCore(
            null,
            worldIdentitySnapshot,
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            genesisManifestSnapshot,
            randomRootSnapshot,
            false,
            out staged,
            out diagnostic,
            stageObserver);
    }

    private static bool TryStageCore(
        DailyCaptureStagingAttempt stagingAttempt,
        P12CWorldIdentitySnapshot worldIdentitySnapshot,
        RuntimeIdAllocatorSnapshot allocatorSnapshot,
        SimulationRecordSequenceSnapshot recordSequenceSnapshot,
        P12CSpatialAuthoritySnapshot spatialSnapshot,
        P12CP9GenesisManifestSnapshot genesisManifestSnapshot,
        DeterministicRandomRootSnapshot randomRootSnapshot,
        bool requireCurrentCaptureAttempt,
        out P12CStagedContinuationRoot staged,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver)
    {
        staged = null;
        diagnostic = null;

        try
        {
            if (requireCurrentCaptureAttempt
                ? stagingAttempt == null || !stagingAttempt.IsCurrent
                : stagingAttempt != null)
            {
                diagnostic = "P12-C staging attempt is stale or incomplete.";
                return false;
            }

            SimulationGenesisManifest stagedManifest = null;
            Action<string> coreStageObserver = stageName =>
            {
                if (string.Equals(stageName, "CGenesisManifestStaged", StringComparison.Ordinal))
                {
                    if (genesisManifestSnapshot == null)
                    {
                        throw new InvalidOperationException("P12-C P9-B genesis manifest snapshot is required.");
                    }

                    if (!P12CP9GenesisManifestSnapshot.FromCore(genesisManifestSnapshot.CoreValue)
                        .TryStageManifest(out stagedManifest, out string manifestDiagnostic))
                        throw new InvalidOperationException(manifestDiagnostic
                            ?? "P12-C Unity manifest projection could not be staged.");
                }

                if (!Enum.TryParse(stageName, ignoreCase: false, out P12GDailyV1RestoreStage stage)
                    || !string.Equals(stage.ToString(), stageName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "P12-C Core staging emitted an unknown restore stage: " + stageName);
                }

                stageObserver?.Invoke(stage);
            };

            if (!P12CSelectedCoreRootStager.TryStage(
                worldIdentitySnapshot,
                allocatorSnapshot,
                recordSequenceSnapshot,
                spatialSnapshot,
                genesisManifestSnapshot == null ? null : genesisManifestSnapshot.CoreValue,
                randomRootSnapshot,
                isDetached: stagingAttempt == null,
                stageObserver: coreStageObserver,
                out P12CStagedCoreContinuationRoot coreRoot,
                out diagnostic))
            {
                return false;
            }

            if (stagedManifest == null)
            {
                diagnostic = "P12-C Core staging returned without a Unity P9-B manifest projection.";
                return false;
            }

            P12CStagedContinuationRoot candidate = new P12CStagedContinuationRoot(
                stagingAttempt,
                coreRoot,
                stagedManifest);
            if (requireCurrentCaptureAttempt && !stagingAttempt.IsCurrent)
            {
                diagnostic = "P12-C staging attempt became stale before the continuation roots were returned.";
                return false;
            }
            staged = candidate;
            return true;
        }
        catch (Exception exception)
        {
            staged = null;
            diagnostic = "P12-C private root staging failed: " + exception.GetType().Name + ": " + exception.Message;
            return false;
        }
    }

}
