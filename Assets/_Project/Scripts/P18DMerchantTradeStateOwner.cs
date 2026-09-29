using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actor-owned receipt authority for the merchant plan and trade travel-intent
/// portion of one P18-D merchant trade-state occurrence.
/// </summary>
[Serializable]
public sealed class NpcMerchantTradeStateRuntime
{
    [SerializeField] private long revision;
    [SerializeField] private List<NpcMerchantTradeStateReceipt> receipts =
        new List<NpcMerchantTradeStateReceipt>();

    public long Revision => revision;
    private List<NpcMerchantTradeStateReceipt> ReceiptList =>
        receipts ?? (receipts = new List<NpcMerchantTradeStateReceipt>());

    internal bool TryResolve(string operationIdentity, string descriptorFingerprint,
        out NpcMerchantTradeStateReceipt receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(operationIdentity)
            || string.IsNullOrWhiteSpace(descriptorFingerprint)) return false;
        foreach (NpcMerchantTradeStateReceipt candidate in ReceiptList)
        {
            if (candidate == null || candidate.OperationIdentity != operationIdentity) continue;
            if (candidate.DescriptorFingerprint != descriptorFingerprint) return false;
            receipt = candidate;
            return true;
        }
        return false;
    }

    internal bool TryPrepareInstall(NpcRuntime actor, string operationIdentity,
        string descriptorFingerprint, MerchantTradePlanState expectedPlan,
        NpcTravelPlanState expectedTravelPlan, MerchantTradePlanState nextPlan,
        NpcTravelPlanState nextTravelPlan,
        out IBoundaryContinuationStepCommit prepared)
    {
        prepared = null;
        if (actor == null || string.IsNullOrWhiteSpace(actor.RuntimeId)
            || string.IsNullOrWhiteSpace(operationIdentity)
            || string.IsNullOrWhiteSpace(descriptorFingerprint)) return false;

        foreach (NpcMerchantTradeStateReceipt receipt in ReceiptList)
        {
            if (receipt == null || receipt.OperationIdentity != operationIdentity) continue;
            if (receipt.DescriptorFingerprint != descriptorFingerprint) return false;
            prepared = new NpcMerchantTradeStateCommit(this, actor, receipt,
                expectedPlan, expectedTravelPlan, nextPlan, nextTravelPlan,
                revision, true);
            return true;
        }

        if (revision == long.MaxValue || expectedPlan == null || expectedTravelPlan == null
            || nextPlan == null || nextTravelPlan == null) return false;

        NpcMerchantTradeStateReceipt nextReceipt = new NpcMerchantTradeStateReceipt(
            operationIdentity, descriptorFingerprint, actor.RuntimeId,
            actor.PersonId != null ? actor.PersonId.Value : string.Empty,
            revision, revision + 1L);
        List<NpcMerchantTradeStateReceipt> nextReceipts =
            new List<NpcMerchantTradeStateReceipt>(ReceiptList) { nextReceipt };
        prepared = new NpcMerchantTradeStateCommit(this, actor, nextReceipt,
            expectedPlan, expectedTravelPlan, nextPlan, nextTravelPlan,
            revision, false, nextReceipts);
        return true;
    }

    internal bool TryCommit(NpcMerchantTradeStateCommit commit,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (commit == null || commit.Owner != this) return false;
        foreach (NpcMerchantTradeStateReceipt existing in ReceiptList)
        {
            if (existing == null || existing.OperationIdentity != commit.Receipt.OperationIdentity) continue;
            if (existing.DescriptorFingerprint != commit.Receipt.DescriptorFingerprint) return false;
            failure = TimelineFailure.None;
            return true;
        }
        if (commit.IsReplay || commit.Actor == null || revision != commit.ExpectedRevision
            || revision == long.MaxValue
            || commit.Actor.RuntimeId != commit.Receipt.ActorRuntimeId
            || (commit.Actor.PersonId != null ? commit.Actor.PersonId.Value : string.Empty)
                != commit.Receipt.PersonId
            || !commit.Actor.MerchantTradePlan.MatchesOwnerState(commit.ExpectedPlan)
            || !commit.Actor.TravelPlan.MatchesOwnerState(commit.ExpectedTravelPlan)
            || commit.NextReceipts == null
            || commit.NextReceipts.Count != ReceiptList.Count + 1
            || !ReferenceEquals(commit.NextReceipts[commit.NextReceipts.Count - 1], commit.Receipt))
            return false;

        // All preflight and allocating work completed during preparation. These
        // actor-owned installs and receipt publication run under the runtime lease.
        commit.Actor.MerchantTradePlan.InstallOwnerState(commit.NextPlan);
        commit.Actor.TravelPlan.InstallOwnerState(commit.NextTravelPlan);
        receipts = commit.NextReceipts;
        revision = commit.Receipt.OwnerRevisionAfter;
        failure = TimelineFailure.None;
        return true;
    }
}

[Serializable]
public sealed class NpcMerchantTradeStateReceipt
{
    [SerializeField] private string operationIdentity;
    [SerializeField] private string descriptorFingerprint;
    [SerializeField] private string actorRuntimeId;
    [SerializeField] private string personId;
    [SerializeField] private long ownerRevisionBefore;
    [SerializeField] private long ownerRevisionAfter;

    public string OperationIdentity => operationIdentity;
    public string DescriptorFingerprint => descriptorFingerprint;
    public string ActorRuntimeId => actorRuntimeId;
    public string PersonId => personId;
    public long OwnerRevisionBefore => ownerRevisionBefore;
    public long OwnerRevisionAfter => ownerRevisionAfter;

    internal NpcMerchantTradeStateReceipt(string operationIdentity,
        string descriptorFingerprint, string actorRuntimeId, string personId,
        long ownerRevisionBefore, long ownerRevisionAfter)
    {
        this.operationIdentity = operationIdentity;
        this.descriptorFingerprint = descriptorFingerprint;
        this.actorRuntimeId = actorRuntimeId;
        this.personId = personId;
        this.ownerRevisionBefore = ownerRevisionBefore;
        this.ownerRevisionAfter = ownerRevisionAfter;
    }
}

internal sealed class NpcMerchantTradeStateCommit : IBoundaryContinuationStepCommit
{
    private static readonly IReadOnlyList<DueWorkReference> NoTimelineFacts =
        Array.Empty<DueWorkReference>();
    private static readonly IReadOnlyList<string> NoSignals = Array.Empty<string>();
    private bool completed;

    internal NpcMerchantTradeStateRuntime Owner { get; }
    internal NpcRuntime Actor { get; }
    internal NpcMerchantTradeStateReceipt Receipt { get; }
    internal MerchantTradePlanState ExpectedPlan { get; }
    internal NpcTravelPlanState ExpectedTravelPlan { get; }
    internal MerchantTradePlanState NextPlan { get; }
    internal NpcTravelPlanState NextTravelPlan { get; }
    internal long ExpectedRevision { get; }
    internal bool IsReplay { get; }
    internal List<NpcMerchantTradeStateReceipt> NextReceipts { get; }

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => NoTimelineFacts;
    public IReadOnlyList<string> RetainedSourceSignals => NoSignals;

    internal NpcMerchantTradeStateCommit(NpcMerchantTradeStateRuntime owner,
        NpcRuntime actor, NpcMerchantTradeStateReceipt receipt,
        MerchantTradePlanState expectedPlan, NpcTravelPlanState expectedTravelPlan,
        MerchantTradePlanState nextPlan, NpcTravelPlanState nextTravelPlan,
        long expectedRevision, bool replay,
        List<NpcMerchantTradeStateReceipt> nextReceipts = null)
    {
        Owner = owner;
        Actor = actor;
        Receipt = receipt;
        ExpectedPlan = expectedPlan;
        ExpectedTravelPlan = expectedTravelPlan;
        NextPlan = nextPlan;
        NextTravelPlan = nextTravelPlan;
        ExpectedRevision = expectedRevision;
        IsReplay = replay;
        NextReceipts = nextReceipts;
    }

    public bool TryCommit(out TimelineFailure failure)
    {
        if (completed)
        {
            failure = TimelineFailure.None;
            return true;
        }
        completed = Owner.TryCommit(this, out failure);
        return completed;
    }
}
