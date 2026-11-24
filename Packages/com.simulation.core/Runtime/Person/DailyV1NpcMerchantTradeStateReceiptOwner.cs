using System;
using System.Collections.Generic;

/// <summary>Engine-free selected Merchant plan state carried across the P18-D install boundary.</summary>
internal sealed class DailyV1MerchantTradePlanReceiptState
{
    internal DailyV1ItemDefinition Item { get; }
    internal string OriginCityRuntimeId { get; }
    internal string TargetCityRuntimeId { get; }
    internal int PlannedAmount { get; }
    internal int RawRemainingAmount { get; }
    internal float PurchasePricePerItem { get; }
    internal int WaitDaysAtDestination { get; }
    internal int PendingTravelDays { get; }
    internal string OriginDecisionId { get; }

    internal DailyV1MerchantTradePlanReceiptState(DailyV1ItemDefinition item,
        string originCityRuntimeId, string targetCityRuntimeId, int plannedAmount,
        int rawRemainingAmount, float purchasePricePerItem, int waitDaysAtDestination,
        int pendingTravelDays, string originDecisionId)
    {
        Item = item;
        OriginCityRuntimeId = originCityRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        PlannedAmount = plannedAmount;
        RawRemainingAmount = rawRemainingAmount;
        PurchasePricePerItem = purchasePricePerItem;
        WaitDaysAtDestination = waitDaysAtDestination;
        PendingTravelDays = pendingTravelDays;
        OriginDecisionId = originDecisionId;
    }

    internal bool Matches(DailyV1NpcMerchantTradePlanOwner owner) => owner != null
        && owner.MatchesState(Item, OriginCityRuntimeId, TargetCityRuntimeId,
            PlannedAmount, RawRemainingAmount, PurchasePricePerItem,
            WaitDaysAtDestination, PendingTravelDays, OriginDecisionId);

    internal bool CanInstall(DailyV1NpcMerchantTradePlanOwner owner) => owner != null
        && owner.CanInstallState(Item, OriginCityRuntimeId, TargetCityRuntimeId,
            PlannedAmount, RawRemainingAmount, PurchasePricePerItem,
            WaitDaysAtDestination, PendingTravelDays, OriginDecisionId);
}

/// <summary>Engine-free selected TravelPlan state carried across the P18-D install boundary.</summary>
internal sealed class DailyV1NpcTravelPlanReceiptState
{
    internal string TargetLocationRuntimeId { get; }
    internal string TargetCityRuntimeId { get; }
    internal NpcTravelReason Reason { get; }
    internal float Utility { get; }
    internal float ExpectedCost { get; }
    internal string OriginDecisionId { get; }

    internal DailyV1NpcTravelPlanReceiptState(string targetLocationRuntimeId,
        string targetCityRuntimeId, NpcTravelReason reason, float utility,
        float expectedCost, string originDecisionId)
    {
        TargetLocationRuntimeId = targetLocationRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        Reason = reason;
        Utility = utility;
        ExpectedCost = expectedCost;
        OriginDecisionId = originDecisionId;
    }

    internal bool Matches(DailyV1NpcTravelPlanOwner owner) => owner != null
        && owner.Matches(TargetLocationRuntimeId, TargetCityRuntimeId, Reason,
            Utility, ExpectedCost, OriginDecisionId);

    internal bool CanCommit(DailyV1NpcTravelPlanOwner owner) => owner != null
        && owner.CanCommit(TargetLocationRuntimeId, TargetCityRuntimeId, Reason,
            Utility, ExpectedCost, OriginDecisionId);
}

/// <summary>
/// Core authority for one selected NPC's P18-D merchant trade-state receipts.
/// The P12 Daily-v1 profile admits this owner only in its exact-empty state;
/// P18 receipt production remains outside that profile.
/// </summary>
internal sealed class DailyV1NpcMerchantTradeStateReceiptOwner
{
    private readonly DailyV1NpcRuntimeOwners actor;
    private List<DailyV1NpcMerchantTradeStateReceipt> receipts =
        new List<DailyV1NpcMerchantTradeStateReceipt>();
    private long revision;

    internal long Revision => revision;
    internal string RuntimeId => actor.RuntimeId;

    internal DailyV1NpcMerchantTradeStateReceiptOwner(DailyV1NpcRuntimeOwners actor)
    {
        this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
    }

    internal bool TryReadP12ReceiptCensus(out int cardinality, out long ownerRevision)
    {
        cardinality = 0;
        ownerRevision = 0L;
        if (receipts == null || revision < 0L) return false;
        cardinality = receipts.Count;
        ownerRevision = revision;
        return true;
    }

    internal bool IsExactEmpty => receipts != null && receipts.Count == 0 && revision == 0L;

    internal bool TryResolve(string operationIdentity, string descriptorFingerprint,
        out DailyV1NpcMerchantTradeStateReceipt receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(operationIdentity)
            || string.IsNullOrWhiteSpace(descriptorFingerprint)
            || receipts == null) return false;
        foreach (DailyV1NpcMerchantTradeStateReceipt candidate in receipts)
        {
            if (candidate == null || candidate.OperationIdentity != operationIdentity) continue;
            if (candidate.DescriptorFingerprint != descriptorFingerprint
                || candidate.ActorRuntimeId != RuntimeId) return false;
            receipt = candidate;
            return true;
        }
        return false;
    }

    internal bool TryPrepareInstall(DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners candidateActor, string personId,
        string operationIdentity, string descriptorFingerprint,
        DailyV1MerchantTradePlanReceiptState expectedPlan,
        DailyV1NpcTravelPlanReceiptState expectedTravelPlan,
        DailyV1MerchantTradePlanReceiptState nextPlan,
        DailyV1NpcTravelPlanReceiptState nextTravelPlan,
        Func<bool> canAdmit, Action<bool, bool> publishCommitted,
        out DailyV1NpcMerchantTradeStateReceiptCommit prepared)
    {
        prepared = null;
        if (!IsExactBinding(graph, candidateActor, personId)
            || string.IsNullOrWhiteSpace(operationIdentity)
            || string.IsNullOrWhiteSpace(descriptorFingerprint)
            || receipts == null) return false;

        foreach (DailyV1NpcMerchantTradeStateReceipt existing in receipts)
        {
            if (existing == null || existing.OperationIdentity != operationIdentity) continue;
            if (existing.DescriptorFingerprint != descriptorFingerprint
                || existing.ActorRuntimeId != RuntimeId
                || existing.PersonId != (personId ?? string.Empty)) return false;
            prepared = new DailyV1NpcMerchantTradeStateReceiptCommit(this,
                graph, existing, expectedPlan, expectedTravelPlan, nextPlan,
                nextTravelPlan, revision, actor.MerchantTradePlan.Revision,
                actor.TravelPlan.Revision, true, null, canAdmit, publishCommitted);
            return true;
        }

        if (revision == long.MaxValue || expectedPlan == null || expectedTravelPlan == null
            || nextPlan == null || nextTravelPlan == null
            || !expectedPlan.Matches(actor.MerchantTradePlan)
            || !expectedTravelPlan.Matches(actor.TravelPlan)
            || !IsPlanCitiesBound(graph, nextPlan)
            || !IsTravelPlanBound(graph, nextTravelPlan)
            || !nextPlan.CanInstall(actor.MerchantTradePlan)
            || !nextTravelPlan.CanCommit(actor.TravelPlan)) return false;

        DailyV1NpcMerchantTradeStateReceipt nextReceipt =
            new DailyV1NpcMerchantTradeStateReceipt(operationIdentity,
                descriptorFingerprint, RuntimeId, personId ?? string.Empty,
                revision, revision + 1L);
        List<DailyV1NpcMerchantTradeStateReceipt> nextReceipts =
            new List<DailyV1NpcMerchantTradeStateReceipt>(receipts) { nextReceipt };
        prepared = new DailyV1NpcMerchantTradeStateReceiptCommit(this, graph,
            nextReceipt, expectedPlan, expectedTravelPlan, nextPlan,
            nextTravelPlan, revision, actor.MerchantTradePlan.Revision,
            actor.TravelPlan.Revision, false, nextReceipts, canAdmit,
            publishCommitted);
        return true;
    }

    internal bool TryCommit(DailyV1NpcMerchantTradeStateReceiptCommit commit,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (commit == null || !ReferenceEquals(commit.Owner, this)) return false;

        if (receipts != null)
        {
            foreach (DailyV1NpcMerchantTradeStateReceipt existing in receipts)
            {
                if (existing == null || existing.OperationIdentity != commit.Receipt.OperationIdentity) continue;
                if (existing.DescriptorFingerprint != commit.Receipt.DescriptorFingerprint
                    || existing.ActorRuntimeId != commit.Receipt.ActorRuntimeId
                    || existing.PersonId != commit.Receipt.PersonId) return false;
                commit.MarkCompleted();
                failure = TimelineFailure.None;
                return true;
            }
        }

        if (commit.IsReplay || receipts == null || revision != commit.ExpectedRevision
            || revision == long.MaxValue
            || !IsExactBinding(commit.Graph, actor, commit.Receipt.PersonId)
            || actor.MerchantTradePlan.Revision != commit.ExpectedPlanRevision
            || actor.TravelPlan.Revision != commit.ExpectedTravelPlanRevision
            || commit.ExpectedPlan == null || commit.ExpectedTravelPlan == null
            || commit.NextPlan == null || commit.NextTravelPlan == null
            || !commit.ExpectedPlan.Matches(actor.MerchantTradePlan)
            || !commit.ExpectedTravelPlan.Matches(actor.TravelPlan)
            || !IsPlanCitiesBound(commit.Graph, commit.NextPlan)
            || !IsTravelPlanBound(commit.Graph, commit.NextTravelPlan)
            || !commit.NextPlan.CanInstall(actor.MerchantTradePlan)
            || !commit.NextTravelPlan.CanCommit(actor.TravelPlan)
            || commit.NextReceipts == null
            || commit.NextReceipts.Count != receipts.Count + 1
            || !ReferenceEquals(commit.NextReceipts[commit.NextReceipts.Count - 1], commit.Receipt))
            return false;

        if (commit.CanAdmit != null)
        {
            try
            {
                if (!commit.CanAdmit()) return false;
            }
            catch
            {
                return false;
            }
        }

        bool planChanged = !commit.NextPlan.Matches(actor.MerchantTradePlan);
        bool travelPlanChanged = !commit.NextTravelPlan.Matches(actor.TravelPlan);
        long nextPlanRevision = commit.ExpectedPlanRevision + (planChanged ? 1L : 0L);
        long nextTravelPlanRevision = commit.ExpectedTravelPlanRevision + (travelPlanChanged ? 1L : 0L);
        if (!actor.MerchantTradePlan.TryInstallPreparedState(commit.NextPlan, nextPlanRevision))
            return false;
        if (!actor.TravelPlan.TryInstallExact(commit.NextTravelPlan.TargetLocationRuntimeId,
                commit.NextTravelPlan.TargetCityRuntimeId, commit.NextTravelPlan.Reason,
                commit.NextTravelPlan.Utility, commit.NextTravelPlan.ExpectedCost,
                commit.NextTravelPlan.OriginDecisionId, nextTravelPlanRevision))
        {
            if (planChanged)
                actor.MerchantTradePlan.TryInstallPreparedState(commit.ExpectedPlan,
                    commit.ExpectedPlanRevision);
            return false;
        }

        receipts = commit.NextReceipts;
        revision = commit.Receipt.OwnerRevisionAfter;
        commit.MarkCompleted();
        failure = TimelineFailure.None;
        commit.PublishCommitted?.Invoke(planChanged, travelPlanChanged);
        return true;
    }

    private bool IsExactBinding(DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners candidateActor, string personId)
    {
        if (graph == null || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(candidateActor, actor)
            || !string.Equals(candidateActor.RuntimeId, RuntimeId, StringComparison.Ordinal)) return false;
        if (graph.PersonStore != null
            && graph.PersonStore.TryGetByMaterializedNpcRuntimeId(RuntimeId, out PersonRuntime person))
            return person?.PersonId != null
                && string.Equals(person.PersonId.Value, personId ?? string.Empty, StringComparison.Ordinal);
        return string.IsNullOrEmpty(personId);
    }

    private static bool IsPlanCitiesBound(DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1MerchantTradePlanReceiptState state)
    {
        if (state == null) return false;
        return (state.OriginCityRuntimeId == null
                || graph.TryGetCityLocationRuntimeId(state.OriginCityRuntimeId, out _))
            && (state.TargetCityRuntimeId == null
                || graph.TryGetCityLocationRuntimeId(state.TargetCityRuntimeId, out _));
    }

    private static bool IsTravelPlanBound(DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcTravelPlanReceiptState state)
    {
        if (state == null) return false;
        if (state.TargetLocationRuntimeId != null
            && (graph.SpatialNetwork == null
                || !graph.SpatialNetwork.TryGetLocation(state.TargetLocationRuntimeId, out _))) return false;
        if (state.TargetCityRuntimeId != null)
            return graph.TryGetCityLocationRuntimeId(state.TargetCityRuntimeId,
                    out string locationRuntimeId)
                && string.Equals(locationRuntimeId, state.TargetLocationRuntimeId,
                    StringComparison.Ordinal);
        if (state.TargetLocationRuntimeId != null
            && !graph.TryGetUniqueCityOwnerForLocation(state.TargetLocationRuntimeId, out _)) return false;
        return true;
    }
}

[Serializable]
internal sealed class DailyV1NpcMerchantTradeStateReceipt
{
    internal string OperationIdentity { get; }
    internal string DescriptorFingerprint { get; }
    internal string ActorRuntimeId { get; }
    internal string PersonId { get; }
    internal long OwnerRevisionBefore { get; }
    internal long OwnerRevisionAfter { get; }

    internal DailyV1NpcMerchantTradeStateReceipt(string operationIdentity,
        string descriptorFingerprint, string actorRuntimeId, string personId,
        long ownerRevisionBefore, long ownerRevisionAfter)
    {
        OperationIdentity = operationIdentity;
        DescriptorFingerprint = descriptorFingerprint;
        ActorRuntimeId = actorRuntimeId;
        PersonId = personId;
        OwnerRevisionBefore = ownerRevisionBefore;
        OwnerRevisionAfter = ownerRevisionAfter;
    }
}

internal sealed class DailyV1NpcMerchantTradeStateReceiptCommit : IBoundaryContinuationStepCommit
{
    private static readonly IReadOnlyList<DueWorkReference> NoTimelineFacts =
        Array.Empty<DueWorkReference>();
    private static readonly IReadOnlyList<string> NoSignals = Array.Empty<string>();
    private bool completed;

    internal DailyV1NpcMerchantTradeStateReceiptOwner Owner { get; }
    internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
    internal DailyV1NpcMerchantTradeStateReceipt Receipt { get; }
    internal DailyV1MerchantTradePlanReceiptState ExpectedPlan { get; }
    internal DailyV1NpcTravelPlanReceiptState ExpectedTravelPlan { get; }
    internal DailyV1MerchantTradePlanReceiptState NextPlan { get; }
    internal DailyV1NpcTravelPlanReceiptState NextTravelPlan { get; }
    internal long ExpectedRevision { get; }
    internal long ExpectedPlanRevision { get; }
    internal long ExpectedTravelPlanRevision { get; }
    internal bool IsReplay { get; }
    internal List<DailyV1NpcMerchantTradeStateReceipt> NextReceipts { get; }
    internal Func<bool> CanAdmit { get; }
    internal Action<bool, bool> PublishCommitted { get; }

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => NoTimelineFacts;
    public IReadOnlyList<string> RetainedSourceSignals => NoSignals;

    internal DailyV1NpcMerchantTradeStateReceiptCommit(
        DailyV1NpcMerchantTradeStateReceiptOwner owner,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcMerchantTradeStateReceipt receipt,
        DailyV1MerchantTradePlanReceiptState expectedPlan,
        DailyV1NpcTravelPlanReceiptState expectedTravelPlan,
        DailyV1MerchantTradePlanReceiptState nextPlan,
        DailyV1NpcTravelPlanReceiptState nextTravelPlan,
        long expectedRevision, long expectedPlanRevision,
        long expectedTravelPlanRevision, bool replay,
        List<DailyV1NpcMerchantTradeStateReceipt> nextReceipts,
        Func<bool> canAdmit, Action<bool, bool> publishCommitted)
    {
        Owner = owner;
        Graph = graph;
        Receipt = receipt;
        ExpectedPlan = expectedPlan;
        ExpectedTravelPlan = expectedTravelPlan;
        NextPlan = nextPlan;
        NextTravelPlan = nextTravelPlan;
        ExpectedRevision = expectedRevision;
        ExpectedPlanRevision = expectedPlanRevision;
        ExpectedTravelPlanRevision = expectedTravelPlanRevision;
        IsReplay = replay;
        NextReceipts = nextReceipts;
        CanAdmit = canAdmit;
        PublishCommitted = publishCommitted;
    }

    internal void MarkCompleted() => completed = true;

    public bool TryCommit(out TimelineFailure failure)
    {
        if (completed)
        {
            failure = TimelineFailure.None;
            return true;
        }
        return Owner.TryCommit(this, out failure);
    }
}
