using System.Collections.Generic;

internal interface IP12EFactionCaptureSource
{
    bool IsCaptureCurrent(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSectionVector);
}