using System;
using System.Collections.Generic;

internal enum P12GDailyV1OwnerVectorFailure
{
    None = 0,
    MissingSourceVector,
    MissingTargetVector,
    SectionCountMismatch,
    DuplicateSectionIdentity,
    MissingSection,
    ContractMismatch,
    CardinalityMismatch,
    RevisionMismatch,
    ReusedSourceOwnerIdentity,
    InvalidEmptyDisposition,
    OwnerAliasMismatch,
    OwnerVectorChanged,
    MutationEpochChanged
}

/// <summary>
/// Core-owned comparison of source, staged, and recaptured Daily-v1 owner
/// vectors. Process-local owner references are compared for alias structure,
/// never copied from the source graph into the candidate.
/// </summary>
internal static class DailyV1RestoredOwnerVectorContract
{
    internal static bool TryValidateRestoredCandidate(
        IReadOnlyList<OwnerSectionCensusSnapshot> sourceOwnerSections,
        IReadOnlyList<OwnerSectionCensusSnapshot> targetOwnerSections,
        out P12GDailyV1OwnerVectorFailure failure,
        out string diagnostic)
    {
        failure = P12GDailyV1OwnerVectorFailure.None;
        diagnostic = null;
        if (sourceOwnerSections == null)
            return Fail(P12GDailyV1OwnerVectorFailure.MissingSourceVector,
                "The source completed-boundary owner vector is required.", out failure, out diagnostic);
        if (targetOwnerSections == null)
            return Fail(P12GDailyV1OwnerVectorFailure.MissingTargetVector,
                "The private target owner vector is required.", out failure, out diagnostic);
        if (sourceOwnerSections.Count != targetOwnerSections.Count)
            return Fail(P12GDailyV1OwnerVectorFailure.SectionCountMismatch,
                "The target candidate does not contain the source boundary's exact owner-section count.",
                out failure, out diagnostic);

        Dictionary<string, OwnerSectionCensusSnapshot> targets =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot target in targetOwnerSections)
        {
            if (target == null || string.IsNullOrWhiteSpace(target.SectionId)
                || !targets.TryAdd(target.SectionId, target))
                return Fail(P12GDailyV1OwnerVectorFailure.DuplicateSectionIdentity,
                    "The target candidate contains a missing or duplicate owner-section identity.",
                    out failure, out diagnostic);
        }

        HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> orderedSource =
            new List<OwnerSectionCensusSnapshot>(sourceOwnerSections.Count);
        List<OwnerSectionCensusSnapshot> orderedTarget =
            new List<OwnerSectionCensusSnapshot>(sourceOwnerSections.Count);
        foreach (OwnerSectionCensusSnapshot source in sourceOwnerSections)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.SectionId)
                || !sourceIds.Add(source.SectionId))
                return Fail(P12GDailyV1OwnerVectorFailure.DuplicateSectionIdentity,
                    "The source boundary contains a missing or duplicate owner-section identity.",
                    out failure, out diagnostic);
            if (!targets.TryGetValue(source.SectionId, out OwnerSectionCensusSnapshot target))
                return Fail(P12GDailyV1OwnerVectorFailure.MissingSection,
                    "The target candidate is missing owner section '" + source.SectionId + "'.",
                    out failure, out diagnostic);
            if (source.SchemaVersion != target.SchemaVersion || source.Role != target.Role)
                return Fail(P12GDailyV1OwnerVectorFailure.ContractMismatch,
                    "The target contract for owner section '" + source.SectionId + "' does not match the source profile.",
                    out failure, out diagnostic);
            if (source.Cardinality != target.Cardinality)
                return Fail(P12GDailyV1OwnerVectorFailure.CardinalityMismatch,
                    "The target cardinality for owner section '" + source.SectionId + "' does not preserve the source value.",
                    out failure, out diagnostic);
            if (source.Revision != target.Revision)
                return Fail(P12GDailyV1OwnerVectorFailure.RevisionMismatch,
                    "The target revision for owner section '" + source.SectionId + "' does not preserve the source value.",
                    out failure, out diagnostic);
            if (ReferenceEquals(source.OwnerInstanceIdentity, target.OwnerInstanceIdentity))
                return Fail(P12GDailyV1OwnerVectorFailure.ReusedSourceOwnerIdentity,
                    "The target owner for section '" + source.SectionId + "' reuses the source owner identity.",
                    out failure, out diagnostic);
            if ((target.Role == OwnerSectionRole.ExplicitlyEmpty
                    || target.Role == OwnerSectionRole.Excluded)
                && target.Cardinality != 0)
                return Fail(P12GDailyV1OwnerVectorFailure.InvalidEmptyDisposition,
                    "Owner section '" + target.SectionId + "' has populated state under an empty/excluded role.",
                    out failure, out diagnostic);

            orderedSource.Add(source);
            orderedTarget.Add(target);
        }

        for (int sourceIndex = 0; sourceIndex < orderedSource.Count; sourceIndex++)
        {
            for (int targetIndex = 0; targetIndex < orderedTarget.Count; targetIndex++)
            {
                if (ReferenceEquals(orderedSource[sourceIndex].OwnerInstanceIdentity,
                    orderedTarget[targetIndex].OwnerInstanceIdentity))
                    return Fail(P12GDailyV1OwnerVectorFailure.ReusedSourceOwnerIdentity,
                        "The target owner for section '" + orderedTarget[targetIndex].SectionId
                            + "' reuses a source owner identity from section '"
                            + orderedSource[sourceIndex].SectionId + "'.",
                        out failure, out diagnostic);
            }
        }

        for (int left = 0; left < orderedSource.Count; left++)
        {
            for (int right = left + 1; right < orderedSource.Count; right++)
            {
                bool sourceAliases = ReferenceEquals(
                    orderedSource[left].OwnerInstanceIdentity,
                    orderedSource[right].OwnerInstanceIdentity);
                bool targetAliases = ReferenceEquals(
                    orderedTarget[left].OwnerInstanceIdentity,
                    orderedTarget[right].OwnerInstanceIdentity);
                if (sourceAliases != targetAliases)
                    return Fail(P12GDailyV1OwnerVectorFailure.OwnerAliasMismatch,
                        "The target owner graph changes the source profile's owner alias relationships.",
                        out failure, out diagnostic);
            }
        }

        return true;
    }

    /// <summary>
    /// Compares a detached image's logical census with a newly staged target.
    /// Image alias labels are compared as logical facts; process-local owner
    /// references are validated only on the target graph.
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
        out string diagnostic)
    {
        failure = P12GDailyV1OwnerVectorFailure.None;
        diagnostic = null;
        if (validatedTargetSections == null || currentTargetSections == null
            || validatedTargetSections.Count != currentTargetSections.Count)
            return Fail(P12GDailyV1OwnerVectorFailure.OwnerVectorChanged,
                "The validated target owner vector is missing or has changed cardinality.",
                out failure, out diagnostic);
        if (validatedMutationEpoch < 0L || currentMutationEpoch != validatedMutationEpoch)
            return Fail(P12GDailyV1OwnerVectorFailure.MutationEpochChanged,
                "The target mutation epoch changed after whole-graph validation.",
                out failure, out diagnostic);

        Dictionary<string, OwnerSectionCensusSnapshot> currentById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot current in currentTargetSections)
        {
            if (current == null || string.IsNullOrWhiteSpace(current.SectionId)
                || !currentById.TryAdd(current.SectionId, current))
                return Fail(P12GDailyV1OwnerVectorFailure.OwnerVectorChanged,
                    "The current target owner vector has a missing or duplicate section identity.",
                    out failure, out diagnostic);
        }

        HashSet<string> validatedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot validated in validatedTargetSections)
        {
            if (validated == null || string.IsNullOrWhiteSpace(validated.SectionId)
                || !validatedIds.Add(validated.SectionId)
                || !currentById.TryGetValue(validated.SectionId, out OwnerSectionCensusSnapshot current)
                || validated.SchemaVersion != current.SchemaVersion
                || validated.Role != current.Role
                || validated.Cardinality != current.Cardinality
                || validated.Revision != current.Revision
                || !ReferenceEquals(validated.OwnerInstanceIdentity, current.OwnerInstanceIdentity))
                return Fail(P12GDailyV1OwnerVectorFailure.OwnerVectorChanged,
                    "The current target owner vector differs from the vector that passed whole-graph validation.",
                    out failure, out diagnostic);
        }

        return true;
    }

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
