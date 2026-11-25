using System.Collections.Generic;

internal interface IP12EPoliticalDecisionCaptureSource
{
    bool IsCaptureCurrent(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector);
}
