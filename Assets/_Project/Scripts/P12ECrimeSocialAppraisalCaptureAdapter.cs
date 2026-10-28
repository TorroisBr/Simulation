using System.Collections.Generic;

/// <summary>Unity boundary for the Core-owned CrimeSocialAppraisal snapshot.</summary>
internal static class P12ECrimeSocialAppraisalCaptureAdapter
{
    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        out P12ECrimeSocialAppraisalSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12ECrimeSocialAppraisalSnapshotFailure.Create(
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires an admitted completed Daily-v1 runtime.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out _)
            || token == null)
            return false;

        return TryCapture(runtime, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector,
        out P12ECrimeSocialAppraisalOwnerSnapshot snapshot,
        out P12ECrimeSocialAppraisalSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        snapshot = null;
        failure = P12ECrimeSocialAppraisalSnapshotFailure.Create(
            P12ECrimeSocialAppraisalSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and owner-section vector.");
        if (runtime == null)
            return false;

        IP12ECrimeSocialAppraisalCaptureSource source =
            new RuntimeCaptureSource(runtime, stagingAttempt);
        return P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(
            source, token, ownerSectionVector, runtime.CrimeSocialAppraisal,
            out snapshot, out failure);
    }

    private sealed class RuntimeCaptureSource : IP12ECrimeSocialAppraisalCaptureSource
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