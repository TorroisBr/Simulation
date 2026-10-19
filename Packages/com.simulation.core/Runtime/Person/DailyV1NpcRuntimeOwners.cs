using System;
using System.Collections.Generic;

/// <summary>The existing selected NPC travel-plan reason values.
/// Numeric order is retained for Unity serialization and P12 persistence.</summary>
public enum NpcTravelReason
{
    None,
    Trade,
    Flee,
    TradeReposition,
    CommercialScout
}

/// <summary>
/// Core-owned composition of the selected Daily-v1 NPC owners used by the
/// Unity runtime adapter. The owner instances are shared with that adapter;
/// this object does not copy or mirror owner state.
/// </summary>
internal sealed class DailyV1NpcRuntimeOwners
{
    internal string RuntimeId { get; }
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal DailyV1NpcDefinition Definition { get; }
    internal DailyV1InventoryOwner Inventory { get; }
    internal DailyV1NpcStatusOwner Status { get; }
    internal DailyV1NpcMerchantTradePlanOwner MerchantTradePlan { get; }
    internal MoneyAccountRuntime MoneyAccount { get; }
    internal DailyV1CommercialKnowledgeOwner CommercialKnowledge { get; }
    internal SpatialKnowledgeRuntime SpatialKnowledge { get; }
    internal DailyV1NpcLifecycleOwner Lifecycle { get; }
    internal DailyV1NpcActionSlotOwner ActionSlot { get; }
    internal DailyV1NpcTravelOwner Travel { get; }
    internal DailyV1NpcTravelPlanOwner TravelPlan { get; }
    internal DailyV1NpcHiddenStateOwner HiddenState { get; }
    internal long CrimeJusticeRevision { get; private set; }

    private DailyV1NpcRuntimeOwners(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        DailyV1InventoryOwner inventory,
        DailyV1NpcMerchantTradePlanOwner merchantTradePlan,
        DailyV1NpcStatusOwner status,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        SpatialKnowledgeRuntime spatialKnowledge)
    {
        RuntimeId = runtimeId;
        Definitions = definitions;
        Definition = definition;
        Inventory = inventory;
        MerchantTradePlan = merchantTradePlan;
        Status = status;
        MoneyAccount = moneyAccount;
        CommercialKnowledge = commercialKnowledge;
        SpatialKnowledge = spatialKnowledge;
        Lifecycle = new DailyV1NpcLifecycleOwner();
        ActionSlot = new DailyV1NpcActionSlotOwner(definitions);
        Travel = new DailyV1NpcTravelOwner(runtimeId);
        TravelPlan = new DailyV1NpcTravelPlanOwner();
        HiddenState = new DailyV1NpcHiddenStateOwner();
    }

    internal static bool TryCreateInitial(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        out DailyV1NpcRuntimeOwners owners)
    {
        return TryCreateInitial(runtimeId, definitions, definition, initialMoney,
            string.IsNullOrWhiteSpace(runtimeId) ? null : new SpatialKnowledgeRuntime(runtimeId), out owners);
    }

    internal static bool TryCreateInitial(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        SpatialKnowledgeRuntime spatialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId) || !IsAdmitted(definitions, definition)
            || spatialKnowledge == null
            || !string.Equals(spatialKnowledge.OwnerRuntimeId, runtimeId, StringComparison.Ordinal)) return false;

        List<DailyV1StatusDefinition> statuses = new List<DailyV1StatusDefinition>();
        foreach (string statusId in definition.Values.statusPadrao ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(statusId)
                || !definitions.Statuses.TryGetValue(statusId, out DailyV1StatusDefinition status))
                return false;
            statuses.Add(status);
        }

        DailyV1InventoryOwner inventory = new DailyV1InventoryOwner(definitions);
        if (!DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
                definitions, out DailyV1NpcMerchantTradePlanOwner merchantTradePlan))
            return false;
        MoneyAccountRuntime moneyAccount = new MoneyAccountRuntime(initialMoney);
        return TryCreateBound(runtimeId, definitions, definition, statuses, inventory, moneyAccount,
            new DailyV1CommercialKnowledgeOwner(definitions), merchantTradePlan,
            spatialKnowledge, out owners);
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        DailyV1NpcMerchantTradePlanOwner merchantTradePlan,
        out DailyV1NpcRuntimeOwners owners)
    {
        return TryCreateBound(runtimeId, definitions, definition, statuses, inventory,
            moneyAccount, commercialKnowledge, merchantTradePlan,
            string.IsNullOrWhiteSpace(runtimeId) ? null : new SpatialKnowledgeRuntime(runtimeId), out owners);
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        DailyV1NpcMerchantTradePlanOwner merchantTradePlan,
        SpatialKnowledgeRuntime spatialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || !IsAdmitted(definitions, definition)
            || statuses == null
            || moneyAccount == null
            || commercialKnowledge == null
            || !ReferenceEquals(commercialKnowledge.Definitions, definitions)
            || merchantTradePlan == null
            || !ReferenceEquals(merchantTradePlan.Definitions, definitions)
            || spatialKnowledge == null
            || !string.Equals(spatialKnowledge.OwnerRuntimeId, runtimeId, StringComparison.Ordinal)
            || (inventory != null && !ReferenceEquals(inventory.Definitions, definitions))
            || !DailyV1NpcStatusOwner.TryCreate(definitions, statuses, out DailyV1NpcStatusOwner statusOwner))
            return false;

        owners = new DailyV1NpcRuntimeOwners(
            runtimeId, definitions, definition, inventory, merchantTradePlan,
            statusOwner, moneyAccount, commercialKnowledge, spatialKnowledge);
        return true;
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (definitions == null
            || !DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
                definitions, out DailyV1NpcMerchantTradePlanOwner merchantTradePlan))
            return false;
        return TryCreateBound(runtimeId, definitions, definition, statuses, inventory,
            moneyAccount, commercialKnowledge, merchantTradePlan, out owners);
    }

    internal static bool TryCreateBound(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (definitions == null
            || !DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
                definitions, out DailyV1NpcMerchantTradePlanOwner merchantTradePlan))
            return false;
        return TryCreateBound(runtimeId, definitions, definition, statuses, inventory,
            moneyAccount, new DailyV1CommercialKnowledgeOwner(definitions),
            merchantTradePlan, out owners);
    }

    private static bool IsAdmitted(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition) =>
        definitions != null && definition != null
        && definitions.Npcs.TryGetValue(definition.DefinitionId, out DailyV1NpcDefinition admitted)
        && ReferenceEquals(definition, admitted);

    internal bool TryCopyStatusDefinitionIds(List<string> destination) =>
        Status != null && Status.TryCopyDefinitionIds(destination);

    internal bool CanAdvanceCrimeJusticeRevision(int increments) =>
        increments >= 0 && CrimeJusticeRevision <= long.MaxValue - increments;

    internal bool TryInstallCrimeJusticeRevision(long revision)
    {
        if (revision < 0L) return false;
        CrimeJusticeRevision = revision;
        return true;
    }

    internal void NotifyCrimeJusticeMutationCommitted() => CrimeJusticeRevision++;
}

/// <summary>
/// Holds the selected NPC's existing hidden-day counter. Mutation admission
/// and P12 invalidation remain with the existing NpcRuntime writer boundary.
/// </summary>
internal sealed class DailyV1NpcHiddenStateOwner
{
    internal int RemainingDays { get; private set; }

    internal bool TryInstallExact(int remainingDays)
    {
        if (remainingDays < 0) return false;
        RemainingDays = remainingDays;
        return true;
    }

    internal bool TrySetRemainingDays(int remainingDays) => TryInstallExact(remainingDays);
}

/// <summary>
/// Holds the exact admitted action-definition identity occupying one selected
/// NPC's current-action slot. Unity's NpcActionRuntime remains the existing
/// execution and persistence projection of this node.
/// </summary>
internal sealed class DailyV1NpcActionSlotOwner
{
    private readonly DailyV1EffectiveDefinitions definitions;

    internal DailyV1ActionDefinition CurrentAction { get; private set; }
    internal DailyV1NpcActionPayloadOwner CurrentPayload { get; private set; }
    internal long Revision { get; private set; }

    internal DailyV1NpcActionSlotOwner(DailyV1EffectiveDefinitions definitions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    internal bool TrySetCurrentAction(DailyV1ActionDefinition action)
    {
        DailyV1NpcActionPayloadOwner payload = action != null
            && ReferenceEquals(CurrentAction, action)
            ? CurrentPayload
            : null;
        return TrySetCurrentAction(action, payload);
    }

    internal bool TrySetCurrentAction(
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload)
    {
        if (!CanSetCurrentAction(action, payload)) return false;

        CurrentAction = action;
        CurrentPayload = payload;
        return true;
    }

    internal bool CanSetCurrentAction(
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload)
    {
        if (action != null
            && (!definitions.Actions.TryGetValue(action.DefinitionId, out DailyV1ActionDefinition admitted)
                || !ReferenceEquals(action, admitted)))
            return false;
        if (action == null && payload != null
            || (payload != null && !ReferenceEquals(payload.Action, action)))
            return false;
        return true;
    }

    internal bool CanAdvanceRevision(int increments) =>
        increments >= 0 && Revision <= long.MaxValue - increments;

    internal bool TryInstallRevision(long revision)
    {
        if (revision < 0L) return false;
        Revision = revision;
        return true;
    }

    internal void NotifyCurrentActionMutationCommitted()
    {
        if (Revision < long.MaxValue) Revision++;
    }
}

/// <summary>
/// Owns the selected NPC's mutable current-action payload. Unity retains only
/// compatibility object references; admitted definition identities, stable
/// target IDs, scalar values, and commercial evidence are held here.
/// </summary>
internal sealed class DailyV1NpcActionPayloadOwner
{
    internal DailyV1ActionDefinition Action { get; }
    internal string TargetNpcRuntimeId { get; }
    internal string TargetCityRuntimeId { get; }
    internal DailyV1ItemDefinition TargetItem { get; }
    internal int Amount { get; }
    internal float ExpectedUnitPrice { get; }
    internal float SuccessChanceMultiplier { get; private set; }
    internal NpcTravelReason TravelReason { get; }
    internal float ExpectedNetValue { get; }
    internal string OriginDecisionId { get; private set; }
    internal string StableOccurrenceKey { get; private set; }
    internal CommercialDecisionEvidence CommercialDecisionEvidence { get; private set; }
    internal CommercialScoutingEvidence CommercialScoutingEvidence { get; private set; }

    private DailyV1NpcActionPayloadOwner(
        DailyV1ActionDefinition action,
        string targetNpcRuntimeId,
        string targetCityRuntimeId,
        DailyV1ItemDefinition targetItem,
        int amount,
        float expectedUnitPrice,
        float successChanceMultiplier,
        NpcTravelReason travelReason,
        float expectedNetValue,
        string originDecisionId,
        string stableOccurrenceKey,
        CommercialDecisionEvidence commercialDecisionEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence)
    {
        Action = action;
        TargetNpcRuntimeId = targetNpcRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        TargetItem = targetItem;
        Amount = amount;
        ExpectedUnitPrice = expectedUnitPrice;
        SuccessChanceMultiplier = successChanceMultiplier;
        TravelReason = travelReason;
        ExpectedNetValue = expectedNetValue;
        OriginDecisionId = originDecisionId;
        StableOccurrenceKey = stableOccurrenceKey;
        CommercialDecisionEvidence = commercialDecisionEvidence;
        CommercialScoutingEvidence = commercialScoutingEvidence;
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action,
        string targetNpcRuntimeId,
        string targetCityRuntimeId,
        DailyV1ItemDefinition targetItem,
        int amount,
        float expectedUnitPrice,
        float successChanceMultiplier,
        NpcTravelReason travelReason,
        float expectedNetValue,
        string originDecisionId,
        string stableOccurrenceKey,
        CommercialDecisionEvidence commercialDecisionEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence,
        out DailyV1NpcActionPayloadOwner owner)
    {
        owner = null;
        if (definitions == null || action == null
            || !definitions.Actions.TryGetValue(action.DefinitionId, out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, action)
            || (targetItem != null
                && (!definitions.Items.TryGetValue(targetItem.DefinitionId, out DailyV1ItemDefinition admittedItem)
                    || !ReferenceEquals(admittedItem, targetItem))))
            return false;

        owner = new DailyV1NpcActionPayloadOwner(action, targetNpcRuntimeId,
            targetCityRuntimeId, targetItem, amount, expectedUnitPrice,
            successChanceMultiplier, travelReason, expectedNetValue,
            originDecisionId, stableOccurrenceKey, commercialDecisionEvidence,
            commercialScoutingEvidence);
        return true;
    }

    internal void SetSuccessChanceMultiplier(float value) => SuccessChanceMultiplier = value;
    internal void SetOriginDecisionId(string value) => OriginDecisionId = value;
    internal void SetStableOccurrenceKey(string value) => StableOccurrenceKey = value;
    internal void SetCommercialDecisionEvidence(CommercialDecisionEvidence value) => CommercialDecisionEvidence = value;
    internal void SetCommercialScoutingEvidence(CommercialScoutingEvidence value) => CommercialScoutingEvidence = value;
}

/// <summary>
/// Owns the selected NPC's factual life and injury values and the existing
/// P12 life-state revision. Unity fields remain compatibility projections.
/// </summary>
internal sealed class DailyV1NpcLifecycleOwner
{
    internal NpcLifeState LifeState { get; private set; } = NpcLifeState.Alive;
    internal NpcInjurySeverity InjurySeverity { get; private set; } = NpcInjurySeverity.None;
    internal long LifeStateRevision { get; private set; }

    internal bool TryInstallState(NpcLifeState lifeState, NpcInjurySeverity injurySeverity, long revision)
    {
        if (!NpcInjuryRules.IsValid(lifeState)
            || !NpcInjuryRules.IsValid(injurySeverity)
            || revision < 0L)
            return false;

        LifeState = lifeState;
        InjurySeverity = injurySeverity;
        LifeStateRevision = revision;
        return true;
    }

    internal bool TrySetInjurySeverity(NpcInjurySeverity severity)
    {
        if (!NpcInjuryRules.IsValid(severity)) return false;
        if (severity > InjurySeverity) InjurySeverity = severity;
        return true;
    }

    internal bool TrySetLifeState(NpcLifeState lifeState)
    {
        if (!NpcInjuryRules.IsValid(lifeState)) return false;
        LifeState = lifeState;
        return true;
    }

    internal bool CanAdvanceRevision(int increments) =>
        increments >= 0 && LifeStateRevision <= long.MaxValue - increments;

    internal void NotifyLifeStateMutationCommitted()
    {
        if (LifeStateRevision < long.MaxValue) LifeStateRevision++;
    }
}

/// <summary>
/// Owns the selected NPC's travel/presence values and travel-state revision.
/// Unity City and Location references remain the adapter projection used by
/// existing operations; this Core record stores their stable runtime IDs.
/// </summary>
internal sealed class DailyV1NpcTravelOwner
{
    internal string NpcRuntimeId { get; }
    internal string CurrentLocationRuntimeId { get; private set; }
    internal string CurrentCityRuntimeId { get; private set; }
    internal string DestinationLocationRuntimeId { get; private set; }
    internal string DestinationCityRuntimeId { get; private set; }
    internal int TravelDaysRemaining { get; private set; }
    internal int TravelDaysTotal { get; private set; }
    internal string TravelRouteRuntimeId { get; private set; }
    internal bool TravelStartedToday { get; private set; }
    internal string TravelOriginDecisionId { get; private set; }
    internal string ActiveTravelPartyId { get; private set; }
    internal long Revision { get; private set; }
    internal bool IsTraveling => DestinationLocationRuntimeId != null && TravelDaysRemaining > 0;

    internal DailyV1NpcTravelOwner(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId))
            throw new ArgumentException("An NPC travel owner requires its stable RuntimeId.", nameof(npcRuntimeId));
        NpcRuntimeId = npcRuntimeId;
    }

    internal bool CanAdvanceRevision(int increments) =>
        increments >= 0 && Revision <= long.MaxValue - increments;

    internal bool TryInstallState(
        string currentLocationRuntimeId,
        string currentCityRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId,
        int travelDaysRemaining,
        int travelDaysTotal,
        string travelRouteRuntimeId,
        bool travelStartedToday,
        string travelOriginDecisionId,
        string activeTravelPartyId,
        long revision)
    {
        if (revision < 0L) return false;
        CurrentLocationRuntimeId = currentLocationRuntimeId;
        CurrentCityRuntimeId = currentCityRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        DestinationCityRuntimeId = destinationCityRuntimeId;
        TravelDaysRemaining = travelDaysRemaining;
        TravelDaysTotal = travelDaysTotal;
        TravelRouteRuntimeId = travelRouteRuntimeId;
        TravelStartedToday = travelStartedToday;
        TravelOriginDecisionId = travelOriginDecisionId;
        ActiveTravelPartyId = activeTravelPartyId;
        Revision = revision;
        return true;
    }

    internal bool TryCommitState(
        string currentLocationRuntimeId,
        string currentCityRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId,
        int travelDaysRemaining,
        int travelDaysTotal,
        string travelRouteRuntimeId,
        bool travelStartedToday,
        string travelOriginDecisionId,
        string activeTravelPartyId,
        bool travelStateChanged)
    {
        long nextRevision = Revision;
        if (travelStateChanged)
        {
            if (!CanAdvanceRevision(1)) return false;
            nextRevision++;
        }
        return TryInstallState(
            currentLocationRuntimeId,
            currentCityRuntimeId,
            destinationLocationRuntimeId,
            destinationCityRuntimeId,
            travelDaysRemaining,
            travelDaysTotal,
            travelRouteRuntimeId,
            travelStartedToday,
            travelOriginDecisionId,
            activeTravelPartyId,
            nextRevision);
    }
}

/// <summary>
/// Owns the selected NPC's pending travel-plan values. Unity City and Location
/// references are compatibility projections; the plan authority stores their
/// stable runtime identities and the exact existing revision.
/// </summary>
internal sealed class DailyV1NpcTravelPlanOwner
{
    internal string TargetLocationRuntimeId { get; private set; }
    internal string TargetCityRuntimeId { get; private set; }
    internal NpcTravelReason Reason { get; private set; }
    internal float Utility { get; private set; }
    internal float ExpectedCost { get; private set; }
    internal string OriginDecisionId { get; private set; }
    internal long Revision { get; private set; }

    internal bool Matches(
        string targetLocationRuntimeId,
        string targetCityRuntimeId,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId) =>
        string.Equals(TargetLocationRuntimeId, targetLocationRuntimeId, StringComparison.Ordinal)
        && string.Equals(TargetCityRuntimeId, targetCityRuntimeId, StringComparison.Ordinal)
        && Reason == reason
        && Utility.Equals(utility)
        && ExpectedCost.Equals(expectedCost)
        && string.Equals(OriginDecisionId, originDecisionId, StringComparison.Ordinal);

    internal bool CanCommit(
        string targetLocationRuntimeId,
        string targetCityRuntimeId,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId) =>
        IsValid(targetLocationRuntimeId, targetCityRuntimeId, reason, utility, expectedCost)
        && (Matches(targetLocationRuntimeId, targetCityRuntimeId, reason, utility, expectedCost, originDecisionId)
            || Revision < long.MaxValue);

    internal bool TryCommit(
        string targetLocationRuntimeId,
        string targetCityRuntimeId,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId)
    {
        if (!CanCommit(targetLocationRuntimeId, targetCityRuntimeId, reason,
                utility, expectedCost, originDecisionId)) return false;
        if (Matches(targetLocationRuntimeId, targetCityRuntimeId, reason,
                utility, expectedCost, originDecisionId)) return true;
        if (!TryInstallExact(targetLocationRuntimeId, targetCityRuntimeId, reason,
                utility, expectedCost, originDecisionId, Revision + 1L)) return false;
        return true;
    }

    internal bool TryInstallExact(
        string targetLocationRuntimeId,
        string targetCityRuntimeId,
        NpcTravelReason reason,
        float utility,
        float expectedCost,
        string originDecisionId,
        long revision)
    {
        if (revision < 0L || !IsValid(targetLocationRuntimeId, targetCityRuntimeId,
                reason, utility, expectedCost)) return false;
        TargetLocationRuntimeId = targetLocationRuntimeId;
        TargetCityRuntimeId = targetCityRuntimeId;
        Reason = reason;
        Utility = utility;
        ExpectedCost = expectedCost;
        OriginDecisionId = originDecisionId;
        Revision = revision;
        return true;
    }

    private static bool IsValid(
        string targetLocationRuntimeId,
        string targetCityRuntimeId,
        NpcTravelReason reason,
        float utility,
        float expectedCost)
    {
        if (!Enum.IsDefined(typeof(NpcTravelReason), reason)
            || float.IsNaN(utility) || float.IsInfinity(utility) || utility < 0f
            || float.IsNaN(expectedCost) || float.IsInfinity(expectedCost) || expectedCost < 0f
            || (targetLocationRuntimeId != null && string.IsNullOrWhiteSpace(targetLocationRuntimeId))
            || (targetCityRuntimeId != null && string.IsNullOrWhiteSpace(targetCityRuntimeId))) return false;
        return targetLocationRuntimeId != null
            || (targetCityRuntimeId == null && reason == NpcTravelReason.None);
    }
}
