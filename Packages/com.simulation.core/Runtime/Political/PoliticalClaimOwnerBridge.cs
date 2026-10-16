using System.Collections.Generic;

/// <summary>
/// Narrow Unity Runtime and P12 access to the authoritative political-claim
/// owner and transition system. This is not a general Store mutation API.
/// </summary>
public static class PoliticalClaimOwnerBridge
{
    public static bool CanBindMutationStatus(
        PoliticalClaimStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.CanBindMutationStatus(status);
    }

    public static bool TryBindMutationStatus(
        PoliticalClaimStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.TryBindMutationStatus(status);
    }

    public static bool TryRegisterRecognitionForRuntimeClone(
        PoliticalClaimStore replayCandidate,
        PoliticalClaimRecognitionRecord record,
        out PoliticalClaimFailure failure)
    {
        return replayCandidate.TryRegisterRecognition(record, out failure);
    }

    public static PoliticalClaimStore CloneForRestoredContinuation(PoliticalClaimStore source)
    {
        return source.Clone();
    }

    public static bool TryCreateStagedOwner(
        IReadOnlyList<PoliticalClaimRecord> claims,
        IReadOnlyList<PoliticalClaimRecognitionRecord> recognitions,
        long revision,
        out PoliticalClaimStore stagedOwner)
    {
        return PoliticalClaimStore.TryCreateFromP12EOwnerSnapshot(
            claims,
            recognitions,
            revision,
            out stagedOwner);
    }

    public static bool TryProposeRecognition(
        PoliticalClaimStore store,
        PoliticalClaimId claimId,
        InstitutionId recognizingInstitutionId,
        PoliticalClaimRecognitionState recognitionState,
        long recognitionAbsoluteDay,
        long expectedWorldDay,
        string reason,
        out PoliticalClaimRecognitionTransition transition,
        out PoliticalClaimFailure failure)
    {
        return PoliticalClaimSystem.TryProposeRecognition(
            store,
            claimId,
            recognizingInstitutionId,
            recognitionState,
            recognitionAbsoluteDay,
            expectedWorldDay,
            reason,
            out transition,
            out failure);
    }

    public static bool TryApplyRecognition(
        PoliticalClaimStore store,
        PoliticalClaimRecognitionTransition transition,
        out PoliticalClaimFailure failure)
    {
        return PoliticalClaimSystem.TryApplyRecognition(store, transition, out failure);
    }

    public static bool TryProposeResolution(
        PoliticalClaimStore store,
        PoliticalClaimId claimId,
        PoliticalClaimStatus status,
        long resolutionAbsoluteDay,
        long expectedWorldDay,
        out PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        return PoliticalClaimSystem.TryProposeResolution(
            store,
            claimId,
            status,
            resolutionAbsoluteDay,
            expectedWorldDay,
            out transition,
            out failure);
    }

    public static bool TryApplyResolution(
        PoliticalClaimStore store,
        PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        return PoliticalClaimSystem.TryApplyResolution(store, transition, out failure);
    }
}
