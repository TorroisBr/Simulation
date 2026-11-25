using System;
using System.Collections.Generic;


/// <summary>
/// Cross-checks one staged Daily-v1 candidate's quiescent owner vector against
/// the source boundary. Source owner identities are never copied into the
/// candidate; section contract, cardinality, and revision must be preserved.
/// </summary>
internal static class P12GDailyV1OwnerVector
{
    internal static bool TryValidateRestoredCandidate(
        IReadOnlyList<OwnerSectionCensusSnapshot> sourceOwnerSections,
        IReadOnlyList<OwnerSectionCensusSnapshot> targetOwnerSections,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic) =>
        DailyV1RestoredOwnerVectorContract.TryValidateRestoredCandidate(
            sourceOwnerSections, targetOwnerSections, out failure, out diagnostic);
    /// <summary>
    /// Compares a detached image's persisted logical census with a freshly
    /// staged target. The image contains logical alias labels rather than
    /// process-local owner references, so this path deliberately does not
    /// synthesize a source owner vector.
    /// </summary>
    internal static bool TryValidateRestoredImageCandidate(
        P12ADailyV1Census imageCensus,
        IReadOnlyList<OwnerSectionCensusSnapshot> targetOwnerSections,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic) =>
        DailyV1RestoredOwnerVectorContract.TryValidateRestoredImageCandidate(
            imageCensus, targetOwnerSections, out failure, out diagnostic);

    internal static bool TryMatchCurrentTargetSnapshot(
        IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetSections,
        long validatedMutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> currentTargetSections,
        long currentMutationEpoch,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic) =>
        DailyV1RestoredOwnerVectorContract.TryMatchCurrentTargetSnapshot(
            validatedTargetSections, validatedMutationEpoch,
            currentTargetSections, currentMutationEpoch, out failure, out diagnostic);
}
