using System.Collections.Generic;

internal interface IP12EPoliticalClaimCaptureSource
{
    bool IsCaptureCurrent(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector);
}