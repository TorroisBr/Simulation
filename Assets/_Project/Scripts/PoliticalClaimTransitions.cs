using System;
using System.Collections.Generic;

public sealed class PoliticalClaimRecognitionTransition : IEquatable<PoliticalClaimRecognitionTransition>
{
    internal PoliticalClaimRecognitionRecord ExpectedRecognition { get; }
    internal long ExpectedStoreRevision { get; }
    public PoliticalClaimId ClaimId { get; }
    public long ExpectedWorldDay { get; }
    public InstitutionId RecognizingInstitutionId { get; }
    public PoliticalClaimRecognitionState RecognitionState { get; }
    public long RecognitionAbsoluteDay { get; }
    public string Reason { get; }

    internal PoliticalClaimRecognitionTransition(
        PoliticalClaimId claimId,
        PoliticalClaimRecognitionRecord expectedRecognition,
        long expectedStoreRevision,
        long expectedWorldDay,
        InstitutionId recognizingInstitutionId,
        PoliticalClaimRecognitionState recognitionState,
        long recognitionAbsoluteDay,
        string reason)
    {
        ExpectedRecognition = expectedRecognition;
        ExpectedStoreRevision = expectedStoreRevision;
        ExpectedWorldDay = expectedWorldDay;
        ClaimId = claimId;
        RecognizingInstitutionId = recognizingInstitutionId;
        RecognitionState = recognitionState;
        RecognitionAbsoluteDay = recognitionAbsoluteDay;
        Reason = reason ?? string.Empty;
    }

    public bool Equals(PoliticalClaimRecognitionTransition other)
    {
        return other != null
            && ClaimId == other.ClaimId
            && RecognizingInstitutionId == other.RecognizingInstitutionId
            && RecognitionState == other.RecognitionState
            && RecognitionAbsoluteDay == other.RecognitionAbsoluteDay
            && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
            && ExpectedStoreRevision == other.ExpectedStoreRevision
            && ExpectedWorldDay == other.ExpectedWorldDay;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalClaimRecognitionTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ClaimId?.GetHashCode() ?? 0;
            hash = (hash * 397) ^ (RecognizingInstitutionId?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (int)RecognitionState;
            hash = (hash * 397) ^ RecognitionAbsoluteDay.GetHashCode();
            return (hash * 397) ^ ExpectedWorldDay.GetHashCode();
        }
    }
}

public sealed class PoliticalClaimResolutionTransition : IEquatable<PoliticalClaimResolutionTransition>
{
    internal PoliticalClaimRecord ExpectedClaim { get; }
    internal long ExpectedStoreRevision { get; }
    public PoliticalClaimId ClaimId { get; }
    public long ExpectedWorldDay { get; }
    public PoliticalClaimStatus Status { get; }
    public long ResolutionAbsoluteDay { get; }

    internal PoliticalClaimResolutionTransition(
        PoliticalClaimRecord expectedClaim,
        long expectedStoreRevision,
        long expectedWorldDay,
        PoliticalClaimStatus status,
        long resolutionAbsoluteDay)
    {
        ExpectedClaim = expectedClaim;
        ExpectedStoreRevision = expectedStoreRevision;
        ExpectedWorldDay = expectedWorldDay;
        ClaimId = expectedClaim?.ClaimId;
        Status = status;
        ResolutionAbsoluteDay = resolutionAbsoluteDay;
    }

    public bool Equals(PoliticalClaimResolutionTransition other)
    {
        return other != null
            && ClaimId == other.ClaimId
            && Status == other.Status
            && ResolutionAbsoluteDay == other.ResolutionAbsoluteDay
            && ExpectedStoreRevision == other.ExpectedStoreRevision
            && ExpectedWorldDay == other.ExpectedWorldDay;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PoliticalClaimResolutionTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (((ClaimId?.GetHashCode() ?? 0) * 397) ^ (int)Status) * 397
                ^ ResolutionAbsoluteDay.GetHashCode();
            return (hash * 397) ^ ExpectedWorldDay.GetHashCode();
        }
    }
}

internal static class PoliticalClaimSystem
{
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
        transition = null;
        if (store == null || claimId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidTransition,
                "A claim store and claim id are required.");
            return false;
        }

        if (Enum.IsDefined(typeof(PoliticalClaimRecognitionState), recognitionState) == false
            || recognitionState == PoliticalClaimRecognitionState.Unrecognized)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidRecognitionState,
                "An explicit recognition transition must set a recognized, contested, or rejected state.");
            return false;
        }

        if (recognizingInstitutionId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RecognitionRequiresInstitution,
                "Institutional claim recognition requires a recognizing institution.");
            return false;
        }

        if (recognitionAbsoluteDay < 0L)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidRecognitionAbsoluteDay,
                "RecognitionAbsoluteDay cannot be negative.");
            return false;
        }

        if (expectedWorldDay < 0L)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidRecognitionAbsoluteDay,
                "ExpectedWorldDay cannot be negative.");
            return false;
        }

        if (store.TryGet(claimId, out PoliticalClaimRecord claim) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaimId,
                "The political claim is not registered.");
            return false;
        }

        if (recognitionAbsoluteDay < claim.CreatedAbsoluteDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidRecognitionAbsoluteDay,
                "RecognitionAbsoluteDay cannot precede claim creation.");
            return false;
        }

        store.TryGetRecognition(claimId, recognizingInstitutionId, out PoliticalClaimRecognitionRecord existing);
        transition = new PoliticalClaimRecognitionTransition(
            claimId,
            existing,
            store.Revision,
            expectedWorldDay,
            recognizingInstitutionId,
            recognitionState,
            recognitionAbsoluteDay,
            reason);
        failure = PoliticalClaimFailure.None;
        return true;
    }

    public static bool TryApplyRecognition(
        PoliticalClaimStore store,
        PoliticalClaimRecognitionTransition transition,
        out PoliticalClaimFailure failure)
    {
        if (store == null || transition == null || transition.ClaimId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidTransition,
                "A valid political claim recognition transition is required.");
            return false;
        }

        IEnumerable<PoliticalClaimRecognitionHistoryEntry> history = transition.ExpectedRecognition == null
            ? null
            : transition.ExpectedRecognition.History;
        PoliticalClaimRecognitionRecord next = new PoliticalClaimRecognitionRecord(
            transition.ClaimId,
            transition.RecognizingInstitutionId,
            transition.RecognitionState,
            transition.RecognitionAbsoluteDay,
            transition.Reason,
            history);
        return store.TryApplyRecognition(transition, next, out failure);
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
        transition = null;
        if (store == null || claimId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidTransition,
                "A claim store and claim id are required.");
            return false;
        }

        if (status == PoliticalClaimStatus.Active
            || Enum.IsDefined(typeof(PoliticalClaimStatus), status) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaimStatus,
                "A resolution transition must choose a terminal claim status.");
            return false;
        }

        if (resolutionAbsoluteDay < 0L)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay,
                "ResolutionAbsoluteDay cannot be negative.");
            return false;
        }

        if (expectedWorldDay < 0L)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay,
                "ExpectedWorldDay cannot be negative.");
            return false;
        }

        if (store.TryGet(claimId, out PoliticalClaimRecord claim) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaimId,
                "The political claim is not registered.");
            return false;
        }

        if (claim.Status != PoliticalClaimStatus.Active)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ResolutionRequiresActiveClaim,
                "Only an active political claim can be resolved.");
            return false;
        }

        if (resolutionAbsoluteDay < claim.CreatedAbsoluteDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay,
                "ResolutionAbsoluteDay cannot precede claim creation.");
            return false;
        }

        transition = new PoliticalClaimResolutionTransition(
            claim,
            store.Revision,
            expectedWorldDay,
            status,
            resolutionAbsoluteDay);
        failure = PoliticalClaimFailure.None;
        return true;
    }

    public static bool TryApplyResolution(
        PoliticalClaimStore store,
        PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        if (store == null || transition == null || transition.ClaimId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidTransition,
                "A valid political claim resolution transition is required.");
            return false;
        }

        return store.TryApplyResolution(
            transition,
            transition.ExpectedClaim.WithStatus(transition.Status, transition.ResolutionAbsoluteDay),
            out failure);
    }
}
