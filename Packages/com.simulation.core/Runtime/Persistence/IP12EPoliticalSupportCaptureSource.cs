using System.Collections.Generic;

internal interface IP12EPoliticalSupportCaptureSource
{
    bool IsCaptureCurrent(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector);
}