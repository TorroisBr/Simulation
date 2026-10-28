using System.Collections.Generic;

/// <summary>
/// Unity runtime boundary for the Core-owned Institution/Office snapshot.
/// It supplies only the admitted live owner references and the existing P12
/// capture-staging freshness check; snapshot validation and staging are Core.
/// </summary>
internal static class P12EInstitutionOfficeCaptureAdapter
{
    internal static bool TryCapture(
        SimulationRuntime runtime,
        out P12EInstitutionOfficeOwnerSnapshot snapshot,
        out P12EInstitutionOfficeSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12EInstitutionOfficeSnapshotFailure.Create(
            P12EInstitutionOfficeSnapshotFailureCode.InvalidCaptureContext,
            "Institution/Office capture requires a live admitted Daily-v1 runtime.");
        if (runtime == null
            || !runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken token,
                out DailyCaptureEligibilityFailure tokenFailure)
            || tokenFailure != DailyCaptureEligibilityFailure.None
            || token == null)
            return false;

        return TryCapture(runtime, token, token.OwnerSections,
            out snapshot, out failure);
    }

    internal static bool TryCapture(
        SimulationRuntime runtime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sharedOwnerSectionVector,
        out P12EInstitutionOfficeOwnerSnapshot snapshot,
        out P12EInstitutionOfficeSnapshotFailure failure,
        DailyCaptureStagingAttempt stagingAttempt = null)
    {
        snapshot = null;
        failure = P12EInstitutionOfficeSnapshotFailure.Create(
            P12EInstitutionOfficeSnapshotFailureCode.InvalidCaptureContext,
            "Capture requires the exact live P12-B token and its section vector.");
        if (runtime == null || token == null || sharedOwnerSectionVector == null
            || !ReferenceEquals(token.OwnerSections, sharedOwnerSectionVector))
            return false;

        return P12EInstitutionOfficeOwnerSnapshot.TryCapture(
            new RuntimeCaptureSource(runtime, stagingAttempt),
            token, sharedOwnerSectionVector, out snapshot, out failure);
    }

    private sealed class RuntimeCaptureSource : IP12EInstitutionOfficeCaptureSource
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

        public InstitutionStore InstitutionOwner => runtime.InstitutionStoreForWorldBoundary;
        public OfficeStore OfficeOwner => runtime.OfficeStoreForWorldBoundary;
        public PersonStore PersonOwner => runtime.PersonStore;

        public bool IsCaptureCurrent(
            DailyCaptureEligibilityToken token,
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector) =>
            DailyCaptureStagingAttempt.IsCaptureCurrent(
                runtime, token, ownerSectionVector, stagingAttempt);
    }
}