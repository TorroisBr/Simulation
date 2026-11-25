using System.Collections.Generic;

/// <summary>Unity boundary for the Core-owned PoliticalDecision snapshot.</summary>
internal static class P12EPoliticalDecisionCaptureAdapter
{
    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EPoliticalDecisionOwnerSnapshot snapshot,
        out P12EPoliticalDecisionSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EPoliticalDecisionSnapshotFailure.Create(
            P12EPoliticalDecisionSnapshotFailureCode.InvalidCaptureContext,
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
        out P12EPoliticalDecisionOwnerSnapshot snapshot,
        out P12EPoliticalDecisionSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        IP12EPoliticalDecisionCaptureSource source = runtime == null
            ? null
            : new RuntimeCaptureSource(runtime, stagingAttempt);
        return P12EPoliticalDecisionOwnerSnapshot.TryCapture(
            source, token, vector, out snapshot, out failure);
    }

    private sealed class RuntimeCaptureSource : IP12EPoliticalDecisionCaptureSource
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
