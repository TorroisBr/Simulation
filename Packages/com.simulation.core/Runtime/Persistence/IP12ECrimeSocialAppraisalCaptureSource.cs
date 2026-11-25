using System.Collections.Generic;

internal interface IP12ECrimeSocialAppraisalCaptureSource
{
    bool IsCaptureCurrent(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector);
}