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
