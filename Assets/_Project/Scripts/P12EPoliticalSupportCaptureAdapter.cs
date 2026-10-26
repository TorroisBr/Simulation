using System.Collections.Generic;

/// <summary>Unity boundary for the Core-owned PoliticalSupport snapshot.</summary>
internal static class P12EPoliticalSupportCaptureAdapter
{
    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EPoliticalSupportOwnerSnapshot snapshot,
        out P12EPoliticalSupportSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalSupportSnapshotFailure.Create(
            P12EPoliticalSupportSnapshotFailureCode.InvalidCaptureContext,
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
        out P12EPoliticalSupportOwnerSnapshot snapshot,
        out P12EPoliticalSupportSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        IP12EPoliticalSupportCaptureSource source = runtime == null
            ? null
            : new RuntimeCaptureSource(runtime, stagingAttempt);
        return P12EPoliticalSupportOwnerSnapshot.TryCapture(
            source, token, vector, out snapshot, out failure);
    }

    private sealed class RuntimeCaptureSource : IP12EPoliticalSupportCaptureSource
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
