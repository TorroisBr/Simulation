using System.Collections.Generic;

/// <summary>Unity boundary for Core-owned PoliticalClaim capture.</summary>
internal static class P12EPoliticalClaimCaptureAdapter
{
    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EPoliticalClaimOwnerSnapshot snapshot,
        out P12EPoliticalClaimSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalClaimSnapshotFailure.Create(
            P12EPoliticalClaimSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the live admitted Daily-v1 runtime and its section vector.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken token,
                out DailyCaptureEligibilityFailure tokenFailure)
            || tokenFailure != DailyCaptureEligibilityFailure.None
            || token == null)
            return false;
        return TryCapture(runtime, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        out P12EPoliticalClaimOwnerSnapshot snapshot,
        out P12EPoliticalClaimSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        IP12EPoliticalClaimCaptureSource source = runtime == null
            ? null
            : new RuntimeCaptureSource(runtime, stagingAttempt);
        return P12EPoliticalClaimOwnerSnapshot.TryCapture(
            source,
            token,
            vector,
            runtime?.PersonStore,
            runtime?.PropertyOwnershipStore,
            runtime?.InstitutionStoreForWorldBoundary,
            runtime?.OfficeStoreForWorldBoundary,
            out snapshot,
            out failure);
    }

    private sealed class RuntimeCaptureSource : IP12EPoliticalClaimCaptureSource
    {
        private readonly SimulationRuntime runtime;
        private readonly DailyCaptureStagingAttempt stagingAttempt;

        internal RuntimeCaptureSource(
            SimulationRuntime runtime,
            DailyCaptureStagingAttempt stagingAttempt)
        {
            this.runtime = runtime;
            this.stagingAttempt = stagingAttempt;
        }

        public bool IsCaptureCurrent(
            DailyCaptureEligibilityToken token,
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector) =>
            DailyCaptureStagingAttempt.IsCaptureCurrent(
                runtime, token, ownerSectionVector, stagingAttempt);
    }
}