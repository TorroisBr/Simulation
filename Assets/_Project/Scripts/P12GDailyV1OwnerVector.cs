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
        out string diagnostic)
    {
        failure = P12GDailyV1OwnerVectorFailure.None;
        diagnostic = null;
        if (imageCensus == null)
            return Fail(P12GDailyV1OwnerVectorFailure.MissingSourceVector,
                "The detached image census is required.", out failure, out diagnostic);
        if (targetOwnerSections == null)
            return Fail(P12GDailyV1OwnerVectorFailure.MissingTargetVector,
                "The private target owner vector is required.", out failure, out diagnostic);

        if (!P12ADailyV1Census.TryCreate(targetOwnerSections,
                out P12ADailyV1Census targetCensus, out P12AResult censusResult)
            || targetCensus == null)
            return Fail(P12GDailyV1OwnerVectorFailure.ContractMismatch,
                "The target candidate did not produce a valid Daily-v1 census: "
                    + (censusResult?.SectionCode ?? censusResult?.Code.ToString() ?? "OwnerGraphRejected") + ".",
                out failure, out diagnostic);

        if (!imageCensus.LogicallyEquals(targetCensus))
            return Fail(P12GDailyV1OwnerVectorFailure.OwnerAliasMismatch,
                "The target candidate does not preserve the detached image's exact section, role, cardinality, revision, and logical alias facts.",
                out failure, out diagnostic);

        foreach (OwnerSectionCensusSnapshot target in targetOwnerSections)
        {
            if (target?.OwnerInstanceIdentity == null
                || ((target.Role == OwnerSectionRole.ExplicitlyEmpty || target.Role == OwnerSectionRole.Excluded)
                    && target.Cardinality != 0))
                return Fail(P12GDailyV1OwnerVectorFailure.InvalidEmptyDisposition,
                    "The private target has a missing owner identity or populated empty/excluded section.",
                    out failure, out diagnostic);
        }

        return true;
    }

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
    private static bool Fail(
        P12GDailyV1OwnerVectorFailure reason,
        string message,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic)
    {
        failure = reason;
        diagnostic = message;
        return false;
    }
}
