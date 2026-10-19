using System;
using UnityEngine;

[Serializable]
public class NpcActionRuntime
{
    [SerializeField] private NpcActionData action;
    [NonSerialized] private NpcRuntime targetNpc;
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private ItemData targetItem;
    [SerializeField] private int amount;
    [SerializeField] private float expectedUnitPrice;
    [SerializeField] private float successChanceMultiplier = 1f;
    [SerializeField] private NpcTravelReason travelReason;
    [SerializeField] private float expectedNetValue;
    [SerializeField] private string originDecisionId;
    [SerializeField] private string stableOccurrenceKey;
    [NonSerialized] private CommercialDecisionEvidence commercialDecisionEvidence;
    [NonSerialized] private CommercialScoutingEvidence commercialScoutingEvidence;
    [NonSerialized] private NpcRuntime p12Owner;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;
    [NonSerialized] private DailyV1NpcActionPayloadOwner selectedOwner;
    [NonSerialized] private DailyV1EffectiveDefinitions selectedDefinitions;
    [NonSerialized] private DailyV1DefinitionAdapter.SelectedBindings selectedBindings;

    public NpcActionData Action => selectedOwner == null || selectedBindings == null
        ? action
        : selectedBindings.TryGetAsset(selectedOwner.Action, out NpcActionData selectedAction)
            ? selectedAction : null;
    public NpcRuntime TargetNpc => targetNpc;
    public CityRuntime TargetCity => targetCity;
    public ItemData TargetItem => selectedOwner == null || selectedBindings == null
        ? targetItem
        : selectedOwner.TargetItem == null ? null
        : selectedBindings.TryGetAsset(selectedOwner.TargetItem, out ItemData selectedItem)
            ? selectedItem : null;
    public int Amount => selectedOwner == null ? amount : selectedOwner.Amount;
    public float ExpectedUnitPrice => selectedOwner == null ? expectedUnitPrice : selectedOwner.ExpectedUnitPrice;
    public float ExpectedTravelCost => selectedOwner == null ? expectedUnitPrice : selectedOwner.ExpectedUnitPrice;
    public float SuccessChanceMultiplier => selectedOwner == null ? successChanceMultiplier : selectedOwner.SuccessChanceMultiplier;
    public NpcTravelReason TravelReason => selectedOwner == null ? travelReason : selectedOwner.TravelReason;
    public float ExpectedNetValue => selectedOwner == null ? expectedNetValue : selectedOwner.ExpectedNetValue;
    public string OriginDecisionId => selectedOwner == null ? originDecisionId : selectedOwner.OriginDecisionId;
    public string StableOccurrenceKey => selectedOwner == null ? stableOccurrenceKey : selectedOwner.StableOccurrenceKey;
    public CommercialDecisionEvidence CommercialDecisionEvidence => selectedOwner == null
        ? commercialDecisionEvidence : selectedOwner.CommercialDecisionEvidence;
    public CommercialScoutingEvidence CommercialScoutingEvidence => selectedOwner == null
        ? commercialScoutingEvidence : selectedOwner.CommercialScoutingEvidence;

    internal DailyV1NpcActionPayloadOwner SelectedOwner => selectedOwner;

    /// <summary>Uses the exact Core-prepared payload as this Unity projection.</summary>
    internal bool TryUseSelectedPayload(
        DailyV1NpcActionPayloadOwner owner,
        DailyV1EffectiveDefinitions definitions)
    {
        if (owner == null || definitions == null || action == null
            || !definitions.Actions.TryGetValue(owner.Action.DefinitionId, out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, owner.Action)
            || !string.Equals(action.DefinitionId, owner.Action.DefinitionId, StringComparison.Ordinal)
            || !string.Equals(owner.TargetCityRuntimeId, targetCity?.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(owner.TargetNpcRuntimeId, targetNpc?.RuntimeId, StringComparison.Ordinal))
            return false;

        if (owner.TargetItem != null
            && (!definitions.Items.TryGetValue(owner.TargetItem.DefinitionId, out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(admittedItem, owner.TargetItem)))
            return false;
        if (owner.TargetItem != null
            && (targetItem == null
                || !string.Equals(targetItem.DefinitionId, owner.TargetItem.DefinitionId, StringComparison.Ordinal)))
            return false;
        if (owner.TargetItem == null && targetItem != null) return false;

        amount = owner.Amount;
        expectedUnitPrice = owner.ExpectedUnitPrice;
        successChanceMultiplier = owner.SuccessChanceMultiplier;
        travelReason = owner.TravelReason;
        expectedNetValue = owner.ExpectedNetValue;
        originDecisionId = owner.OriginDecisionId;
        stableOccurrenceKey = owner.StableOccurrenceKey;
        commercialDecisionEvidence = owner.CommercialDecisionEvidence;
        commercialScoutingEvidence = owner.CommercialScoutingEvidence;
        selectedOwner = owner;
        selectedDefinitions = definitions;
        selectedBindings = null;
        return true;
    }

    internal bool TryCreateSelectedOwner(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition selectedAction,
        out DailyV1NpcActionPayloadOwner owner)
    {
        owner = null;
        if (selectedOwner != null)
        {
            if (!ReferenceEquals(selectedDefinitions, definitions)
                || !ReferenceEquals(selectedOwner.Action, selectedAction)
                || !Matches(selectedOwner, definitions, null))
                return false;
            owner = selectedOwner;
            return true;
        }
        DailyV1ItemDefinition selectedItem = null;
        if (targetItem != null
            && (definitions == null
                || !definitions.Items.TryGetValue(targetItem.DefinitionId, out selectedItem)
                || selectedItem == null
                || !string.Equals(selectedItem.DefinitionId, targetItem.DefinitionId, StringComparison.Ordinal)))
            return false;

        return DailyV1NpcActionPayloadOwner.TryCreate(definitions, selectedAction,
            targetNpc?.RuntimeId, targetCity?.RuntimeId, selectedItem, amount,
            expectedUnitPrice, successChanceMultiplier, travelReason, expectedNetValue,
            originDecisionId, stableOccurrenceKey, commercialDecisionEvidence,
            commercialScoutingEvidence, out owner);
    }

    internal bool TryBindSelectedOwner(
        DailyV1NpcActionPayloadOwner owner,
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (owner == null || definitions == null
            || !Matches(owner, definitions, bindings)) return false;
        if (selectedOwner != null)
        {
            if (!ReferenceEquals(selectedOwner, owner)) return false;
            selectedDefinitions = definitions;
            selectedBindings = bindings;
            return true;
        }
        selectedOwner = owner;
        selectedDefinitions = definitions;
        selectedBindings = bindings;
        return true;
    }

    internal bool MatchesSelectedOwner() => selectedOwner != null
        && Matches(selectedOwner, selectedDefinitions, selectedBindings);

    internal void UnbindSelectedOwner()
    {
        selectedOwner = null;
        selectedDefinitions = null;
        selectedBindings = null;
    }

    private bool Matches(
        DailyV1NpcActionPayloadOwner owner,
        DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (owner == null
            || !string.Equals(owner.TargetNpcRuntimeId, targetNpc?.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(owner.TargetCityRuntimeId, targetCity?.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(owner.TargetItem?.DefinitionId, targetItem?.DefinitionId, StringComparison.Ordinal)
            || owner.Amount != amount
            || !owner.ExpectedUnitPrice.Equals(expectedUnitPrice)
            || !owner.SuccessChanceMultiplier.Equals(successChanceMultiplier)
            || owner.TravelReason != travelReason
            || !owner.ExpectedNetValue.Equals(expectedNetValue)
            || !string.Equals(owner.OriginDecisionId, originDecisionId, StringComparison.Ordinal)
            || !string.Equals(owner.StableOccurrenceKey, stableOccurrenceKey, StringComparison.Ordinal)
            || !ReferenceEquals(owner.CommercialDecisionEvidence, commercialDecisionEvidence)
            || !ReferenceEquals(owner.CommercialScoutingEvidence, commercialScoutingEvidence)) return false;

        if (definitions != null
            && (!definitions.Actions.TryGetValue(owner.Action.DefinitionId, out DailyV1ActionDefinition admittedAction)
                || !ReferenceEquals(admittedAction, owner.Action)
                || (owner.TargetItem != null
                    && (!definitions.Items.TryGetValue(owner.TargetItem.DefinitionId, out DailyV1ItemDefinition admittedItem)
                        || !ReferenceEquals(admittedItem, owner.TargetItem))))) return false;

        if (action == null || !string.Equals(action.DefinitionId, owner.Action.DefinitionId, StringComparison.Ordinal))
            return false;
        if (bindings == null) return true;
        return bindings.TryGet(action, out DailyV1ActionDefinition mappedAction)
            && ReferenceEquals(mappedAction, owner.Action)
            && bindings.TryGetAsset(owner.Action, out NpcActionData mappedActionAsset)
            && ReferenceEquals(mappedActionAsset, action)
            && (owner.TargetItem == null
                ? targetItem == null
                : bindings.TryGet(targetItem, out DailyV1ItemDefinition mappedItemNode)
                    && ReferenceEquals(mappedItemNode, owner.TargetItem)
                    && bindings.TryGetAsset(owner.TargetItem, out ItemData mappedItem)
                    && ReferenceEquals(mappedItem, targetItem));
    }

    public NpcActionRuntime(NpcActionData action)
    {
        this.action = action;
    }

    public NpcActionRuntime(NpcActionData action, NpcRuntime targetNpc)
    {
        this.action = action;
        this.targetNpc = targetNpc;
    }

    public NpcActionRuntime(NpcActionData action, NpcRuntime targetNpc, int amount)
    {
        this.action = action;
        this.targetNpc = targetNpc;
        this.amount = UnityMathfCompatibility.Max(0, amount);
    }

    public NpcActionRuntime(NpcActionData action, NpcRuntime targetNpc, ItemData targetItem, int amount, float expectedUnitPrice)
    {
        this.action = action;
        this.targetNpc = targetNpc;
        this.targetItem = targetItem;
        this.amount = UnityMathfCompatibility.Max(0, amount);
        this.expectedUnitPrice = UnityMathfCompatibility.Max(0f, expectedUnitPrice);
    }

    /// <summary>Creates an unpublished action payload without replaying its setters.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        NpcActionData action,
        NpcRuntime targetNpc,
        CityRuntime targetCity,
        ItemData targetItem,
        int amount,
        float expectedUnitPrice,
        float successChanceMultiplier,
        NpcTravelReason travelReason,
        float expectedNetValue,
        string originDecisionId,
        string stableOccurrenceKey,
        CommercialDecisionEvidence commercialDecisionEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence,
        out NpcActionRuntime staged)
    {
        staged = null;
        if (action == null || amount < 0
            || !IsFiniteNonNegative(expectedUnitPrice)
            || !IsFiniteNonNegative(successChanceMultiplier)
            || !Enum.IsDefined(typeof(NpcTravelReason), travelReason)
            || float.IsNaN(expectedNetValue) || float.IsInfinity(expectedNetValue))
            return false;
        NpcActionRuntime candidate = new NpcActionRuntime(action);
        candidate.targetNpc = targetNpc;
        candidate.targetCity = targetCity;
        candidate.targetItem = targetItem;
        candidate.amount = amount;
        candidate.expectedUnitPrice = expectedUnitPrice;
        candidate.successChanceMultiplier = successChanceMultiplier;
        candidate.travelReason = travelReason;
        candidate.expectedNetValue = expectedNetValue;
        candidate.originDecisionId = originDecisionId;
        candidate.stableOccurrenceKey = stableOccurrenceKey;
        candidate.commercialDecisionEvidence = commercialDecisionEvidence;
        candidate.commercialScoutingEvidence = commercialScoutingEvidence;
        staged = candidate;
        return true;
    }

    private static bool IsFiniteNonNegative(float value) =>
        value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    public void SetSuccessChanceMultiplier(float multiplier)
    {
        float normalized = UnityMathfCompatibility.Max(0f, multiplier);
        float current = selectedOwner == null ? successChanceMultiplier : selectedOwner.SuccessChanceMultiplier;
        if (current.Equals(normalized)) return;
        if (!CanCommitP12Mutation()) return;
        selectedOwner?.SetSuccessChanceMultiplier(normalized);
        successChanceMultiplier = normalized;
        NotifyP12MutationCommitted();
    }

    public void SetOriginDecisionId(string decisionId)
    {
        string normalized = string.IsNullOrWhiteSpace(decisionId) ? null : decisionId;
        string current = selectedOwner == null ? originDecisionId : selectedOwner.OriginDecisionId;
        if (string.Equals(current, normalized, StringComparison.Ordinal)) return;
        if (!CanCommitP12Mutation()) return;
        selectedOwner?.SetOriginDecisionId(normalized);
        originDecisionId = normalized;
        NotifyP12MutationCommitted();
    }

    /// <summary>
    /// Supplies semantic occurrence identity for domain outcomes. This is
    /// intentionally separate from the decision history id.
    /// </summary>
    public void SetStableOccurrenceKey(string occurrenceKey)
    {
        string normalized = string.IsNullOrWhiteSpace(occurrenceKey) == true
            ? null
            : occurrenceKey;
        string current = selectedOwner == null ? stableOccurrenceKey : selectedOwner.StableOccurrenceKey;
        if (string.Equals(current, normalized, StringComparison.Ordinal)) return;
        if (!CanCommitP12Mutation()) return;
        selectedOwner?.SetStableOccurrenceKey(normalized);
        stableOccurrenceKey = normalized;
        NotifyP12MutationCommitted();
    }

    public void SetCommercialDecisionEvidence(CommercialDecisionEvidence evidence)
    {
        CommercialDecisionEvidence current = selectedOwner == null
            ? commercialDecisionEvidence : selectedOwner.CommercialDecisionEvidence;
        if (ReferenceEquals(current, evidence)) return;
        if (!CanCommitP12Mutation()) return;
        selectedOwner?.SetCommercialDecisionEvidence(evidence);
        commercialDecisionEvidence = evidence;
        NotifyP12MutationCommitted();
    }

    public void SetCommercialScoutingEvidence(CommercialScoutingEvidence evidence)
    {
        CommercialScoutingEvidence current = selectedOwner == null
            ? commercialScoutingEvidence : selectedOwner.CommercialScoutingEvidence;
        if (ReferenceEquals(current, evidence)) return;
        if (!CanCommitP12Mutation()) return;
        selectedOwner?.SetCommercialScoutingEvidence(evidence);
        commercialScoutingEvidence = evidence;
        NotifyP12MutationCommitted();
    }

    internal bool IsP12Bound => p12Owner != null;

    internal bool CanBindToP12Owner(NpcRuntime owner) => owner != null
        && (p12Owner == null || ReferenceEquals(p12Owner, owner));

    internal bool TryBindP12Owner(NpcRuntime owner)
    {
        if (owner == null) return false;
        if (ReferenceEquals(p12Owner, owner)) return true;
        if (p12Owner != null) return false;
        p12Owner = owner;
        p12MutationAdmission = () => owner.CanCommitInstalledP12ActionMutation(this);
        p12MutationCommitted = () => owner.NotifyInstalledP12ActionMutationCommitted(this);
        return true;
    }

    internal bool UnbindP12Owner(NpcRuntime owner)
    {
        if (p12Owner == null) return true;
        if (!ReferenceEquals(p12Owner, owner)) return false;
        p12Owner = null;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }

    public NpcActionRuntime(NpcActionData action, CityRuntime targetCity, ItemData targetItem, int amount, float expectedUnitPrice)
    {
        this.action = action;
        this.targetCity = targetCity;
        this.targetItem = targetItem;
        this.amount = amount;
        this.expectedUnitPrice = expectedUnitPrice;
    }

    public NpcActionRuntime(NpcActionData action, CityRuntime targetCity, ItemData targetItem, NpcTravelReason travelReason, float expectedCost, float expectedNetValue)
    {
        this.action = action;
        this.targetCity = targetCity;
        this.targetItem = targetItem;
        this.travelReason = travelReason;
        expectedUnitPrice = UnityMathfCompatibility.Max(0f, expectedCost);
        this.expectedNetValue = expectedNetValue;
    }
}

[Serializable]
public class NpcTravelPlanRuntime
{
    [NonSerialized] private SpatialLocationRuntime targetLocation;
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private NpcTravelReason reason;
    [SerializeField] private float utility;
    [SerializeField] private float expectedCost;
    [SerializeField] private string originDecisionId;
    [SerializeField] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;
    [NonSerialized] private DailyV1NpcTravelPlanOwner selectedOwner;

    public SpatialLocationRuntime TargetLocation => targetLocation;
    public CityRuntime TargetCity => targetCity;
    public NpcTravelReason Reason => reason;
    public float Utility => utility;
    public float ExpectedCost => expectedCost;
    public string OriginDecisionId => originDecisionId;
    public long Revision => selectedOwner == null ? revision : selectedOwner.Revision;
    public bool IsActive => targetLocation != null && reason != NpcTravelReason.None;

    internal DailyV1NpcTravelPlanOwner SelectedOwner => selectedOwner;

    internal bool TryBindSelectedOwner(DailyV1NpcTravelPlanOwner owner)
    {
        if (owner == null) return false;
        if (selectedOwner != null) return ReferenceEquals(selectedOwner, owner) && MatchesSelectedOwner();
        if (!owner.Matches(targetLocation?.RuntimeId, targetCity?.RuntimeId, reason,
                utility, expectedCost, originDecisionId)
            || owner.Revision != revision) return false;
        selectedOwner = owner;
        return true;
    }

    internal bool MatchesSelectedOwner() => selectedOwner != null
        && (targetCity == null || ReferenceEquals(targetCity.Location, targetLocation))
        && selectedOwner.Matches(targetLocation?.RuntimeId, targetCity?.RuntimeId,
            reason, utility, expectedCost, originDecisionId)
        && selectedOwner.Revision == revision;

    internal NpcTravelPlanState CaptureOwnerState() => new NpcTravelPlanState(
        targetLocation, targetCity, reason, utility, expectedCost, originDecisionId);

    internal bool MatchesOwnerState(NpcTravelPlanState state) => state != null
        && (selectedOwner == null || MatchesSelectedOwner())
        && ReferenceEquals(targetLocation, state.TargetLocation)
        && ReferenceEquals(targetCity, state.TargetCity)
        && reason == state.Reason
        && utility.Equals(state.Utility)
        && expectedCost.Equals(state.ExpectedCost)
        && string.Equals(originDecisionId, state.OriginDecisionId, StringComparison.Ordinal);

    internal bool CanInstallOwnerState(NpcTravelPlanState state)
    {
        if (state == null) return false;
        if (selectedOwner != null && !MatchesSelectedOwner()) return false;
        if (MatchesOwnerState(state)) return true;
        long currentRevision = Revision;
        if (currentRevision == long.MaxValue || !CanCommitP12Mutation()) return false;
        return selectedOwner == null
            || selectedOwner.CanCommit(state.TargetLocation?.RuntimeId,
                state.TargetCity?.RuntimeId, state.Reason, state.Utility,
                state.ExpectedCost, state.OriginDecisionId);
    }

    internal bool InstallOwnerState(NpcTravelPlanState state)
    {
        if (state == null) return false;
        if (MatchesOwnerState(state)) return true;
        if (selectedOwner != null && !MatchesSelectedOwner()) return false;
        if (Revision == long.MaxValue || !CanCommitP12Mutation()) return false;
        if (selectedOwner != null
            && !selectedOwner.TryCommit(state.TargetLocation?.RuntimeId,
                state.TargetCity?.RuntimeId, state.Reason, state.Utility,
                state.ExpectedCost, state.OriginDecisionId)) return false;
        targetLocation = state.TargetLocation;
        targetCity = state.TargetCity;
        reason = state.Reason;
        utility = state.Utility;
        expectedCost = state.ExpectedCost;
        originDecisionId = state.OriginDecisionId;
        if (selectedOwner == null) revision++;
        else revision = selectedOwner.Revision;
        NotifyP12MutationCommitted();
        return true;
    }

    /// <summary>Creates an unpublished exact plan without replaying a plan mutation.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        SpatialLocationRuntime targetLocation,
        CityRuntime targetCity,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId,
        long exactRevision,
        out NpcTravelPlanRuntime staged)
    {
        staged = null;
        if (exactRevision < 0L || !Enum.IsDefined(typeof(NpcTravelReason), reason)
            || float.IsNaN(utility) || float.IsInfinity(utility) || utility < 0f
            || float.IsNaN(expectedCost) || float.IsInfinity(expectedCost) || expectedCost < 0f
            || (targetCity != null && !ReferenceEquals(targetCity.Location, targetLocation))
            || (targetLocation == null && (targetCity != null || reason != NpcTravelReason.None)))
            return false;
        NpcTravelPlanRuntime candidate = new NpcTravelPlanRuntime();
        candidate.targetLocation = targetLocation;
        candidate.targetCity = targetCity;
        candidate.reason = reason;
        candidate.utility = utility;
        candidate.expectedCost = expectedCost;
        candidate.originDecisionId = originDecisionId;
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    public void Set(CityRuntime targetCity, NpcTravelReason reason, float utility, float expectedCost, string originDecisionId = null)
    {
        Set(targetCity?.Location, targetCity, reason, utility, expectedCost, originDecisionId);
    }

    public void Set(
        SpatialLocationRuntime targetLocation,
        CityRuntime targetCityProjection,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId = null)
    {
        if (targetLocation == null
            || (targetCityProjection != null && targetCityProjection.Location != targetLocation))
        {
            Clear();
            return;
        }

        NpcTravelPlanState next = new NpcTravelPlanState(
            targetLocation,
            targetCityProjection,
            reason,
            UnityMathfCompatibility.Max(0f, utility),
            UnityMathfCompatibility.Max(0f, expectedCost),
            string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId);
        InstallOwnerState(next);
    }

    public void Clear()
    {
        InstallOwnerState(new NpcTravelPlanState(
            null, null, NpcTravelReason.None, 0f, 0f, null));
    }

    internal bool TryInstallSelectedTradePlan(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        string targetCityRuntimeId,
        float utility,
        string originDecisionId)
    {
        if (selectedOwner == null || npc == null
            || !ReferenceEquals(npc.TravelPlan, selectedOwner) || graph == null) return false;
        long priorRevision = selectedOwner.Revision;
        if (!graph.TryCommitMerchantTradeTravelPlan(npc, targetCityRuntimeId,
                utility, originDecisionId, CanCommitP12Mutation, out _)) return false;
        if (selectedOwner.Revision != priorRevision) NotifyP12MutationCommitted();
        return true;
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("NpcTravelPlanRuntime is already bound to a P12 mutation boundary.");
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }
}

internal sealed class NpcTravelPlanState
{
    internal SpatialLocationRuntime TargetLocation { get; }
    internal CityRuntime TargetCity { get; }
    internal NpcTravelReason Reason { get; }
    internal float Utility { get; }
    internal float ExpectedCost { get; }
    internal string OriginDecisionId { get; }

    internal NpcTravelPlanState(SpatialLocationRuntime targetLocation, CityRuntime targetCity,
        NpcTravelReason reason, float utility, float expectedCost, string originDecisionId)
    {
        TargetLocation = targetLocation;
        TargetCity = targetCity;
        Reason = reason;
        Utility = utility;
        ExpectedCost = expectedCost;
        OriginDecisionId = originDecisionId;
    }
}

public enum NpcActionResultType
{
    Success,
    Failure
}

public class NpcActionResult
{
    public NpcActionResultType ResultType { get; }
    public string Message { get; }
    public bool Success => ResultType == NpcActionResultType.Success;

    private NpcActionResult(NpcActionResultType resultType, string message)
    {
        ResultType = resultType;
        Message = message;
    }

    public static NpcActionResult Succeeded(string message = null)
    {
        return new NpcActionResult(NpcActionResultType.Success, message);
    }

    public static NpcActionResult Failed(string message = null)
    {
        return new NpcActionResult(NpcActionResultType.Failure, message);
    }
}

[Serializable]
public class MerchantTradePlanRuntime
{
    [SerializeField] private ItemData item;
    [NonSerialized] private CityRuntime originCity;
    [NonSerialized] private CityRuntime targetCity;
    [SerializeField] private int plannedAmount;
    [SerializeField] private int remainingAmount;
    [SerializeField] private float purchasePricePerItem;
    [SerializeField] private int waitDaysAtDestination;
    [SerializeField] private int pendingTravelDays;
    [SerializeField] private string originDecisionId;
    [SerializeField] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;
    [NonSerialized] private DailyV1NpcMerchantTradePlanOwner selectedOwner;
    [NonSerialized] private DailyV1DefinitionAdapter.SelectedBindings selectedBindings;
    [NonSerialized] private Func<string, CityRuntime> selectedCityResolver;

    public ItemData Item => selectedOwner == null ? item : ResolveSelectedItem();
    public CityRuntime OriginCity => selectedOwner == null ? originCity : ResolveSelectedCity(selectedOwner.OriginCityRuntimeId);
    public CityRuntime TargetCity => selectedOwner == null ? targetCity : ResolveSelectedCity(selectedOwner.TargetCityRuntimeId);
    public int PlannedAmount => selectedOwner == null ? plannedAmount : selectedOwner.PlannedAmount;
    public int RemainingAmount => selectedOwner == null
        ? (remainingAmount > 0 ? remainingAmount : plannedAmount)
        : selectedOwner.RemainingAmount;
    public float PurchasePricePerItem => selectedOwner == null ? purchasePricePerItem : selectedOwner.PurchasePricePerItem;
    public int WaitDaysAtDestination => selectedOwner == null ? waitDaysAtDestination : selectedOwner.WaitDaysAtDestination;
    public int PendingTravelDays => selectedOwner == null ? pendingTravelDays : selectedOwner.PendingTravelDays;
    public string OriginDecisionId => selectedOwner == null ? originDecisionId : selectedOwner.OriginDecisionId;
    public long Revision => selectedOwner == null ? revision : selectedOwner.Revision;
    public bool HasData => selectedOwner == null
        ? item != null || originCity != null || targetCity != null || plannedAmount > 0 || remainingAmount > 0
        : selectedOwner.HasData;
    public bool IsActive => selectedOwner == null
        ? item != null && targetCity != null && RemainingAmount > 0
        : selectedOwner.IsActive && TargetCity != null;

    internal DailyV1NpcMerchantTradePlanOwner SelectedOwner => selectedOwner;

    internal bool TryBindSelectedOwner(
        DailyV1NpcMerchantTradePlanOwner owner,
        DailyV1DefinitionAdapter.SelectedBindings bindings,
        Func<string, CityRuntime> cityResolver)
    {
        if (owner == null || bindings == null || cityResolver == null
            || !ReferenceEquals(owner.Definitions, bindings.Definitions)) return false;
        if (selectedOwner != null)
        {
            if (!ReferenceEquals(selectedOwner, owner)
                || !ReferenceEquals(selectedBindings, bindings)) return false;
            Func<string, CityRuntime> previousResolver = selectedCityResolver;
            selectedCityResolver = cityResolver;
            if (CanProjectSelectedOwner()) return true;
            selectedCityResolver = previousResolver;
            return false;
        }

        DailyV1ItemDefinition stagedItem = null;
        if (item != null
            && (!bindings.TryGet(item, out stagedItem)
                || !ReferenceEquals(stagedItem, owner.ItemDefinition))) return false;
        string stagedOriginId = originCity?.RuntimeId;
        string stagedTargetId = targetCity?.RuntimeId;
        if ((originCity != null && (string.IsNullOrWhiteSpace(stagedOriginId)
                || !ReferenceEquals(cityResolver(stagedOriginId), originCity)))
            || (targetCity != null && (string.IsNullOrWhiteSpace(stagedTargetId)
                || !ReferenceEquals(cityResolver(stagedTargetId), targetCity)))
            || !ReferenceEquals(stagedItem, owner.ItemDefinition)
            || !string.Equals(stagedOriginId, owner.OriginCityRuntimeId, StringComparison.Ordinal)
            || !string.Equals(stagedTargetId, owner.TargetCityRuntimeId, StringComparison.Ordinal)
            || plannedAmount != owner.PlannedAmount || remainingAmount != owner.RawRemainingAmount
            || !purchasePricePerItem.Equals(owner.PurchasePricePerItem)
            || waitDaysAtDestination != owner.WaitDaysAtDestination
            || pendingTravelDays != owner.PendingTravelDays
            || !string.Equals(originDecisionId, owner.OriginDecisionId, StringComparison.Ordinal)
            || revision != owner.Revision) return false;

        selectedOwner = owner;
        selectedBindings = bindings;
        selectedCityResolver = cityResolver;
        item = null;
        originCity = null;
        targetCity = null;
        plannedAmount = 0;
        remainingAmount = 0;
        purchasePricePerItem = 0f;
        waitDaysAtDestination = 0;
        pendingTravelDays = 0;
        originDecisionId = null;
        revision = 0L;
        if (CanProjectSelectedOwner()) return true;
        selectedOwner = null;
        selectedBindings = null;
        selectedCityResolver = null;
        return false;
    }

    internal MerchantTradePlanState CaptureOwnerState() => selectedOwner == null
        ? new MerchantTradePlanState(item, originCity, targetCity, plannedAmount, remainingAmount,
            purchasePricePerItem, waitDaysAtDestination, pendingTravelDays, originDecisionId)
        : CaptureSelectedOwnerState();

    internal bool MatchesOwnerState(MerchantTradePlanState state) => selectedOwner == null
        ? state != null
        && ReferenceEquals(item, state.Item)
        && ReferenceEquals(originCity, state.OriginCity)
        && ReferenceEquals(targetCity, state.TargetCity)
        && plannedAmount == state.PlannedAmount
        && remainingAmount == state.RawRemainingAmount
        && purchasePricePerItem.Equals(state.PurchasePricePerItem)
        && waitDaysAtDestination == state.WaitDaysAtDestination
        && pendingTravelDays == state.PendingTravelDays
        && string.Equals(originDecisionId, state.OriginDecisionId, StringComparison.Ordinal)
        : TryConvertSelectedState(state, out DailyV1ItemDefinition selectedItem,
            out string originId, out string targetId)
            && selectedOwner.MatchesState(selectedItem, originId, targetId,
                state.PlannedAmount, state.RawRemainingAmount, state.PurchasePricePerItem,
                state.WaitDaysAtDestination, state.PendingTravelDays, state.OriginDecisionId);

    internal bool CanInstallOwnerState(MerchantTradePlanState state) => state != null
        && (MatchesOwnerState(state)
            || (Revision < long.MaxValue && CanCommitP12Mutation()
                && (selectedOwner == null || CanConvertAndInstallSelectedState(state))));

    internal bool InstallOwnerState(MerchantTradePlanState state)
    {
        if (state == null) return false;
        if (MatchesOwnerState(state)) return true;
        if (Revision == long.MaxValue || !CanCommitP12Mutation()) return false;
        if (selectedOwner != null)
        {
            if (!TryConvertSelectedState(state, out DailyV1ItemDefinition selectedItem,
                    out string originId, out string targetId)
                || !selectedOwner.TryInstallState(selectedItem, originId, targetId,
                    state.PlannedAmount, state.RawRemainingAmount, state.PurchasePricePerItem,
                    state.WaitDaysAtDestination, state.PendingTravelDays, state.OriginDecisionId))
                return false;
            NotifyP12MutationCommitted();
            return true;
        }
        item = state.Item;
        originCity = state.OriginCity;
        targetCity = state.TargetCity;
        plannedAmount = state.PlannedAmount;
        remainingAmount = state.RawRemainingAmount;
        purchasePricePerItem = state.PurchasePricePerItem;
        waitDaysAtDestination = state.WaitDaysAtDestination;
        pendingTravelDays = state.PendingTravelDays;
        originDecisionId = state.OriginDecisionId;
        revision++;
        NotifyP12MutationCommitted();
        return true;
    }

    internal bool TryInstallSelectedPlan(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1ItemDefinition item,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        float purchasePricePerItem,
        string originDecisionId)
    {
        if (selectedOwner == null || npc == null
            || !ReferenceEquals(npc.MerchantTradePlan, selectedOwner)) return false;
        plannedAmount = UnityMathfCompatibility.Max(0, plannedAmount);
        purchasePricePerItem = UnityMathfCompatibility.Max(0f, purchasePricePerItem);
        originDecisionId = string.IsNullOrWhiteSpace(originDecisionId) ? null : originDecisionId;
        if (MatchesSelectedPlan(item, originCityRuntimeId, targetCityRuntimeId,
                plannedAmount, purchasePricePerItem, originDecisionId)) return true;
        if (!CanCommitP12Mutation()) return false;
        if (graph == null || !graph.TryInstallMerchantTradePlan(npc, item,
                originCityRuntimeId, targetCityRuntimeId, plannedAmount,
                purchasePricePerItem, originDecisionId)) return false;
        NotifyP12MutationCommitted();
        return true;
    }

    private bool MatchesSelectedPlan(
        DailyV1ItemDefinition item,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        float purchasePricePerItem,
        string originDecisionId) => selectedOwner != null
        && selectedOwner.MatchesState(item, originCityRuntimeId, targetCityRuntimeId,
            plannedAmount, 0, purchasePricePerItem, 0, 0, originDecisionId);

    private bool CanConvertAndInstallSelectedState(MerchantTradePlanState state) =>
        TryConvertSelectedState(state, out DailyV1ItemDefinition selectedItem,
            out string originId, out string targetId)
        && selectedOwner.CanInstallState(selectedItem, originId, targetId,
            state.PlannedAmount, state.RawRemainingAmount, state.PurchasePricePerItem,
            state.WaitDaysAtDestination, state.PendingTravelDays, state.OriginDecisionId);

    private bool TryConvertSelectedState(MerchantTradePlanState state,
        out DailyV1ItemDefinition selectedItem, out string originId, out string targetId)
    {
        selectedItem = null;
        originId = null;
        targetId = null;
        if (selectedOwner == null || state == null || selectedBindings == null
            || selectedCityResolver == null) return false;
        if (state.Item != null
            && (!selectedBindings.TryGet(state.Item, out selectedItem)
                || selectedItem == null
                || !selectedOwner.Definitions.Items.TryGetValue(selectedItem.DefinitionId,
                    out DailyV1ItemDefinition admittedItem)
                || !ReferenceEquals(admittedItem, selectedItem))) return false;
        if (state.OriginCity != null)
        {
            originId = state.OriginCity.RuntimeId;
            if (string.IsNullOrWhiteSpace(originId)
                || !ReferenceEquals(selectedCityResolver(originId), state.OriginCity)) return false;
        }
        if (state.TargetCity != null)
        {
            targetId = state.TargetCity.RuntimeId;
            if (string.IsNullOrWhiteSpace(targetId)
                || !ReferenceEquals(selectedCityResolver(targetId), state.TargetCity)) return false;
        }
        return true;
    }

    private ItemData ResolveSelectedItem()
    {
        if (selectedOwner?.ItemDefinition == null || selectedBindings == null
            || !selectedBindings.TryGetAsset(selectedOwner.ItemDefinition, out ItemData selectedItem))
            return null;
        return selectedItem;
    }

    private CityRuntime ResolveSelectedCity(string runtimeId)
    {
        if (runtimeId == null) return null;
        CityRuntime city = selectedCityResolver?.Invoke(runtimeId);
        return city != null && string.Equals(city.RuntimeId, runtimeId, StringComparison.Ordinal)
            ? city : null;
    }

    private bool CanProjectSelectedOwner()
    {
        if (selectedOwner == null || selectedBindings == null || selectedCityResolver == null)
            return false;
        if (selectedOwner.ItemDefinition != null && ResolveSelectedItem() == null) return false;
        return (selectedOwner.OriginCityRuntimeId == null || ResolveSelectedCity(selectedOwner.OriginCityRuntimeId) != null)
            && (selectedOwner.TargetCityRuntimeId == null || ResolveSelectedCity(selectedOwner.TargetCityRuntimeId) != null);
    }

    private MerchantTradePlanState CaptureSelectedOwnerState()
    {
        if (!CanProjectSelectedOwner())
            throw new InvalidOperationException("The selected merchant-plan owner has no exact Unity projection.");
        return new MerchantTradePlanState(ResolveSelectedItem(),
            ResolveSelectedCity(selectedOwner.OriginCityRuntimeId),
            ResolveSelectedCity(selectedOwner.TargetCityRuntimeId),
            selectedOwner.PlannedAmount, selectedOwner.RawRemainingAmount,
            selectedOwner.PurchasePricePerItem, selectedOwner.WaitDaysAtDestination,
            selectedOwner.PendingTravelDays, selectedOwner.OriginDecisionId);
    }

    /// <summary>Creates an unpublished exact plan without replaying trade-plan mutations.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        ItemData item,
        CityRuntime originCity,
        CityRuntime targetCity,
        int plannedAmount,
        int rawRemainingAmount,
        float purchasePricePerItem,
        int waitDaysAtDestination,
        int pendingTravelDays,
        string originDecisionId,
        long exactRevision,
        out MerchantTradePlanRuntime staged)
    {
        staged = null;
        if (exactRevision < 0L || plannedAmount < 0 || rawRemainingAmount < 0
            || waitDaysAtDestination < 0 || pendingTravelDays < 0
            || float.IsNaN(purchasePricePerItem) || float.IsInfinity(purchasePricePerItem)
            || purchasePricePerItem < 0f
            || (item == null && (plannedAmount != 0 || rawRemainingAmount != 0
                || originCity != null || targetCity != null))
            || (item != null && string.IsNullOrWhiteSpace(item.DefinitionId))
            || (targetCity != null && targetCity.Location == null))
            return false;
        MerchantTradePlanRuntime candidate = new MerchantTradePlanRuntime();
        candidate.item = item;
        candidate.originCity = originCity;
        candidate.targetCity = targetCity;
        candidate.plannedAmount = plannedAmount;
        candidate.remainingAmount = rawRemainingAmount;
        candidate.purchasePricePerItem = purchasePricePerItem;
        candidate.waitDaysAtDestination = waitDaysAtDestination;
        candidate.pendingTravelDays = pendingTravelDays;
        candidate.originDecisionId = originDecisionId;
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    public void Set(ItemData item, CityRuntime originCity, CityRuntime targetCity, int plannedAmount, float purchasePricePerItem, string originDecisionId = null)
    {
        int normalizedAmount = UnityMathfCompatibility.Max(0, plannedAmount);
        InstallOwnerState(new MerchantTradePlanState(
            item,
            originCity,
            targetCity,
            normalizedAmount,
            normalizedAmount,
            UnityMathfCompatibility.Max(0f, purchasePricePerItem),
            0,
            0,
            string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId));
    }

    public void RedirectTo(CityRuntime targetCity, string originDecisionId = null)
    {
        int nextWaitDays = TargetCity != targetCity ? 0 : WaitDaysAtDestination;
        string nextDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == false
            ? originDecisionId
            : OriginDecisionId;
        InstallOwnerState(new MerchantTradePlanState(
            Item, OriginCity, targetCity, PlannedAmount, RemainingAmount,
            PurchasePricePerItem, nextWaitDays, PendingTravelDays, nextDecisionId));
    }

    public void IncrementWaitDayAtDestination()
    {
        InstallOwnerState(new MerchantTradePlanState(
            Item, OriginCity, TargetCity, PlannedAmount, RemainingAmount,
            PurchasePricePerItem, WaitDaysAtDestination + 1, PendingTravelDays,
            OriginDecisionId));
    }

    public void ResetWaitDaysAtDestination()
    {
        InstallOwnerState(new MerchantTradePlanState(
            Item, OriginCity, TargetCity, PlannedAmount, RemainingAmount,
            PurchasePricePerItem, 0, PendingTravelDays, OriginDecisionId));
    }

    public void IncrementPendingTravelDay()
    {
        InstallOwnerState(new MerchantTradePlanState(
            Item, OriginCity, TargetCity, PlannedAmount, RemainingAmount,
            PurchasePricePerItem, WaitDaysAtDestination, PendingTravelDays + 1,
            OriginDecisionId));
    }

    public bool RegisterSale(int amountSold)
    {
        if (amountSold <= 0)
        {
            return false;
        }

        int nextRemainingAmount = UnityMathfCompatibility.Max(0, RemainingAmount - amountSold);
        bool completed = nextRemainingAmount <= 0;
        MerchantTradePlanState next = completed
            ? new MerchantTradePlanState(null, null, null, 0, 0, 0f, 0, 0, null)
            : new MerchantTradePlanState(Item, OriginCity, TargetCity,
                PlannedAmount, nextRemainingAmount, PurchasePricePerItem, 0,
                PendingTravelDays, OriginDecisionId);
        return InstallOwnerState(next) && completed;
    }

    public void Clear()
    {
        InstallOwnerState(new MerchantTradePlanState(null, null, null,
            0, 0, 0f, 0, 0, null));
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("MerchantTradePlanRuntime is already bound to a P12 mutation boundary.");
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }
}

internal sealed class MerchantTradePlanState
{
    internal ItemData Item { get; }
    internal CityRuntime OriginCity { get; }
    internal CityRuntime TargetCity { get; }
    internal int PlannedAmount { get; }
    internal int RawRemainingAmount { get; }
    internal int RemainingAmount => RawRemainingAmount > 0 ? RawRemainingAmount : PlannedAmount;
    internal float PurchasePricePerItem { get; }
    internal int WaitDaysAtDestination { get; }
    internal int PendingTravelDays { get; }
    internal string OriginDecisionId { get; }
    internal bool HasData => Item != null || OriginCity != null || TargetCity != null
        || PlannedAmount > 0 || RawRemainingAmount > 0;
    internal bool IsActive => Item != null && TargetCity != null && RemainingAmount > 0;

    internal MerchantTradePlanState(ItemData item, CityRuntime originCity, CityRuntime targetCity,
        int plannedAmount, int rawRemainingAmount, float purchasePricePerItem,
        int waitDaysAtDestination, int pendingTravelDays, string originDecisionId)
    {
        Item = item;
        OriginCity = originCity;
        TargetCity = targetCity;
        PlannedAmount = plannedAmount;
        RawRemainingAmount = rawRemainingAmount;
        PurchasePricePerItem = purchasePricePerItem;
        WaitDaysAtDestination = waitDaysAtDestination;
        PendingTravelDays = pendingTravelDays;
        OriginDecisionId = originDecisionId;
    }
}
