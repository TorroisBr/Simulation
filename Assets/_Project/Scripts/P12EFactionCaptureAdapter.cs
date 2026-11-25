using System.Collections.Generic;

/// <summary>Unity boundary for Core-owned Faction capture and staging.</summary>
internal static class P12EFactionCaptureAdapter
{
    internal static bool TryCapture(
        SimulationRuntime runtime,
        FactionStore owner,
        out P12EFactionOwnerSnapshot snapshot,
        out P12EFactionSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EFactionSnapshotFailure.Create(
            P12EFactionSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the live admitted Daily-v1 runtime and its section vector.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken token, out DailyCaptureEligibilityFailure tokenFailure)
            || tokenFailure != DailyCaptureEligibilityFailure.None || token == null)
            return false;
        return TryCapture(runtime, owner, token, token.OwnerSections, out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        FactionStore owner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        out P12EFactionOwnerSnapshot snapshot,
        out P12EFactionSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        IP12EFactionCaptureSource source = runtime == null
            ? null
            : new RuntimeCaptureSource(runtime, stagingAttempt);
        return P12EFactionOwnerSnapshot.TryCapture(source, owner, token, vector, out snapshot, out failure);
    }

    private sealed class RuntimeCaptureSource : IP12EFactionCaptureSource
    {
        private readonly SimulationRuntime runtime;
        private readonly DailyCaptureStagingAttempt stagingAttempt;

        internal RuntimeCaptureSource(SimulationRuntime runtime, DailyCaptureStagingAttempt stagingAttempt)
        {
            this.runtime = runtime;
            this.stagingAttempt = stagingAttempt;
        }

        public bool IsCaptureCurrent(
            DailyCaptureEligibilityToken token,
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector) =>
            DailyCaptureStagingAttempt.IsCaptureCurrent(runtime, token, ownerSectionVector, stagingAttempt);
    }
}
