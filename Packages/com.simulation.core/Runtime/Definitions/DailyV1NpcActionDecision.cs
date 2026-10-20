using System;
using System.Collections.Generic;

/// <summary>A selected, graph-owned action and its current decision weight.</summary>
internal sealed class DailyV1NpcActionCandidate
{
    internal DailyV1ActionDefinition Action { get; }
    internal float Utility { get; set; }
    internal DailyV1NpcActionPayloadOwner PreparedPayload { get; set; }
    internal float ExpectedTravelCost { get; set; }

    internal DailyV1NpcActionCandidate(DailyV1ActionDefinition action, float utility)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Utility = utility;
    }
}

/// <summary>Core action eligibility, utility and deterministic selection for selected Daily-v1 definitions.</summary>
internal static class DailyV1NpcActionDecision
{
    /// <summary>Prepares an existing selected TravelPlan as the current action
    /// from exact graph owners, the shared P8 party store, and Core route/cost
    /// values. Returns false when no active plan exists so another provider can
    /// prepare a new Merchant reposition plan.</summary>
    internal static bool TryPrepareSelectedExistingTravelCandidate(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate,
        TravelPartyStore partyStore)
    {
        if (graph == null || actor == null || candidate?.Action == null
            || candidate.Action.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.TravelActionTypeValue)
            return false;

        candidate.PreparedPayload = null;
        candidate.ExpectedTravelCost = 0f;
        DailyV1NpcTravelPlanOwner plan = actor.TravelPlan;
        if (plan == null || string.IsNullOrWhiteSpace(plan.TargetLocationRuntimeId))
            return false;

        if (!graph.ContainsExactNpcOwner(actor)
            || graph.IsManagedByTravelParty(actor, partyStore)
            || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || actor.Travel == null || actor.Travel.IsTraveling
            || !graph.TryGetCityLocationRuntimeId(plan.TargetCityRuntimeId,
                out string targetLocationRuntimeId)
            || !string.Equals(targetLocationRuntimeId, plan.TargetLocationRuntimeId,
                StringComparison.Ordinal)
            || !graph.TryPlanSelectedSoloTravel(actor, actor.Travel.CurrentLocationRuntimeId,
                targetLocationRuntimeId, plan.TargetCityRuntimeId,
                out SpatialRouteRuntime route, out float travelCost)
            || actor.MoneyAccount == null || actor.MoneyAccount.Balance < travelCost
            || !TryGetExactCity(graph, actor.Travel.CurrentCityRuntimeId,
                out DailyV1CityRuntimeOwners currentCity)
            || !TryGetExactCity(graph, plan.TargetCityRuntimeId,
                out DailyV1CityRuntimeOwners destinationCity)
            || !DailyV1SelectedTravelTransitionPlan.TryStartTravel(
                actor, currentCity, actor.Travel.CurrentLocationRuntimeId,
                targetLocationRuntimeId, destinationCity, route.TravelDays,
                plan.OriginDecisionId, route.RuntimeId, out _))
        {
            candidate.Utility = 0f;
            return true;
        }

        bool matchingMerchantPlan = plan.Reason == NpcTravelReason.Trade
            && actor.MerchantTradePlan?.IsActive == true
            && string.Equals(actor.MerchantTradePlan.TargetCityRuntimeId,
                plan.TargetCityRuntimeId, StringComparison.Ordinal);
        if (!DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
                graph.Definitions, actor.Definition, candidate.Action,
                plan.Utility, plan.Reason == NpcTravelReason.Trade,
                matchingMerchantPlan, matchingMerchantPlan
                    ? actor.MerchantTradePlan.PendingTravelDays : 0,
                candidate.Utility, out float utility)
            || !DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, candidate.Action, null,
                plan.TargetCityRuntimeId, null, 0, travelCost, 1f,
                plan.Reason, 0f, plan.OriginDecisionId, null, null, null,
                out DailyV1NpcActionPayloadOwner payload))
        {
            candidate.Utility = 0f;
            return true;
        }

        candidate.Utility = utility;
        candidate.ExpectedTravelCost = travelCost;
        candidate.PreparedPayload = payload;
        return true;
    }

    /// <summary>Prepares an admitted autonomous SellGoods candidate from the
    /// exact graph owners, including active-plan local-buyer and market fallback
    /// paths. Returns false only when the caller must retain another provider
    /// path; otherwise utility and payload are authoritative, including
    /// no-opportunity rejection.</summary>
    internal static bool TryPrepareSelectedSellGoodsCandidate(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate,
        long currentAbsoluteDay)
    {
        if (graph == null || actor == null || candidate?.Action == null
            || candidate.Action.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.SellGoodsActionTypeValue)
            return false;

        candidate.PreparedPayload = null;
        DailyV1MerchantLocalSalePlan plan;
        if (actor.MerchantTradePlan?.IsActive == true)
        {
            if (!string.Equals(actor.MerchantTradePlan.TargetCityRuntimeId,
                    actor.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal)
                || !DailyV1MerchantActionPlanning.TryPlanSelectedPlannedLocalBuyer(
                    graph, actor, candidate.Action, currentAbsoluteDay,
                    candidate.Utility, out plan))
            {
                candidate.Utility = 0f;
                return true;
            }

            if (plan == null
                && !DailyV1MerchantActionPlanning.TryPlanSelectedPlannedMarketSale(
                    graph, actor, candidate.Action, currentAbsoluteDay,
                    candidate.Utility, out plan))
            {
                candidate.Utility = 0f;
                return true;
            }
        }
        else if (!DailyV1MerchantActionPlanning.TryPlanSelectedLocalSale(
                     graph, actor, candidate.Action, currentAbsoluteDay,
                     candidate.Utility, out plan))
        {
            candidate.Utility = 0f;
            return true;
        }

        if (plan == null || plan.SelectedPayload == null)
        {
            candidate.Utility = 0f;
            return true;
        }

        candidate.Utility = plan.Utility;
        candidate.PreparedPayload = plan.SelectedPayload;
        return true;
    }

    /// <summary>Prepares the selected autonomous BuyGoods candidate from exact
    /// graph owners. The Unity provider remains responsible only for projecting
    /// the resolved City and Item compatibility objects.</summary>
    internal static bool TryPrepareSelectedBuyGoodsCandidate(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate,
        long currentAbsoluteDay)
    {
        if (graph == null || actor == null || candidate?.Action == null
            || candidate.Action.Values.actionType?.Value
                != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue)
            return false;

        candidate.PreparedPayload = null;
        if (!DailyV1NpcDefinitionRules.HasJobType(
                actor.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            || actor.MerchantTradePlan?.IsActive == true
            || DailyV1NpcDefinitionRules.HasMerchantBehavior(
                actor.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue)
            || !DailyV1MerchantActionPlanning.TryPlanSelectedTradeOpportunity(
                graph, actor, candidate.Action, actor.Travel?.CurrentCityRuntimeId,
                currentAbsoluteDay, 0f, 0,
                out DailyV1MerchantTradePlan plan)
            || plan == null || plan.SelectedPayload == null)
        {
            candidate.Utility = 0f;
            return true;
        }

        candidate.Utility = UnityMathfCompatibility.Max(
            candidate.Utility, 30f + plan.Score);
        candidate.PreparedPayload = plan.SelectedPayload;
        return true;
    }

    /// <summary>Rolls the action currently committed in the exact selected
    /// owner slot. The projected Unity action/payload must not become a second
    /// source of decision identity or success multiplier.</summary>
    internal static bool TryRollCurrentActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        out bool succeeded)
    {
        succeeded = false;
        if (graph == null || actor == null || !graph.ContainsExactNpcOwner(actor)
            || actor.ActionSlot?.CurrentAction == null
            || !IsAdmittedAction(graph.Definitions, actor.ActionSlot.CurrentAction))
            return false;

        DailyV1NpcActionPayloadOwner payload = actor.ActionSlot.CurrentPayload;
        if (payload != null && !ReferenceEquals(payload.Action, actor.ActionSlot.CurrentAction))
            return false;

        return TryRollActionSuccess(
            graph.Definitions,
            actor.ActionSlot.CurrentAction,
            actor.RuntimeId,
            payload?.OriginDecisionId ?? actor.ActionSlot.CurrentAction.DefinitionId,
            payload?.SuccessChanceMultiplier ?? 1f,
            currentAbsoluteDay,
            randomSource,
            out succeeded);
    }

    /// <summary>Applies the admitted Daily-v1 action's existing success-roll contract.</summary>
    internal static bool TryRollActionSuccess(
        DailyV1EffectiveDefinitions definitions,
        DailyV1ActionDefinition action,
        string actorRuntimeId,
        string decisionId,
        float contextualMultiplier,
        long currentAbsoluteDay,
        IAuthoritativeRandomSource randomSource,
        out bool succeeded)
    {
        succeeded = false;
        if (!IsAdmittedAction(definitions, action))
            return false;

        if (!action.Values.canFail)
        {
            succeeded = true;
            return true;
        }

        if (randomSource == null)
            return false;

        float effectiveChance = UnityMathfCompatibility.Clamp01(
            action.Values.baseSuccessChance * UnityMathfCompatibility.Max(0f, contextualMultiplier));
        string decisionKey = decisionId ?? "unbound";
        string actionKey = action.DefinitionId;
        float randomValue = randomSource.NextUnit(
            "action-success|" + (actorRuntimeId ?? "unknown-actor") + "|" + decisionKey + "|"
            + actionKey + "|" + currentAbsoluteDay);
        succeeded = randomValue <= effectiveChance;
        return true;
    }

    internal static bool TryCreateCandidates(
        DailyV1NpcDefinition npc,
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1ActionDefinition> actions,
        IReadOnlyList<DailyV1StatusDefinition> activeStatuses,
        out List<DailyV1NpcActionCandidate> candidates)
    {
        candidates = null;
        if (!IsAdmittedNpc(definitions, npc) || actions == null)
            return false;

        List<DailyV1NpcActionCandidate> resolved = new List<DailyV1NpcActionCandidate>();
        foreach (DailyV1ActionDefinition action in actions)
        {
            if (!IsAdmittedAction(definitions, action)
                || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(definitions, action, activeStatuses))
                continue;

            float utility = DailyV1NpcDefinitionRules.GetActionBaseUtility(definitions, action);
            if (DailyV1NpcDefinitionRules.TryGetWorkActionUtility(npc, definitions, action, out float workUtility))
                utility = workUtility;
            if (DailyV1NpcDefinitionRules.TryGetDefaultActionUtility(npc, definitions, action, out float defaultUtility))
                utility = defaultUtility;
            utility = DailyV1NpcDefinitionRules.ApplyStatusModifiers(definitions, action, activeStatuses, utility);
            resolved.Add(new DailyV1NpcActionCandidate(action, utility));
        }

        resolved.Sort(CompareCandidates);
        candidates = resolved;
        return true;
    }

    internal static DailyV1ActionDefinition ChooseWeightedAction(
        IReadOnlyList<DailyV1NpcActionCandidate> candidates,
        IAuthoritativeRandomSource randomSource,
        string randomStreamKey)
    {
        return ChooseWeightedCandidate(candidates, randomSource, randomStreamKey)?.Action;
    }

    internal static DailyV1NpcActionCandidate ChooseWeightedCandidate(
        IReadOnlyList<DailyV1NpcActionCandidate> candidates,
        IAuthoritativeRandomSource randomSource,
        string randomStreamKey)
    {
        if (candidates == null || randomSource == null || string.IsNullOrWhiteSpace(randomStreamKey))
            return null;

        List<DailyV1NpcActionCandidate> ordered = new List<DailyV1NpcActionCandidate>(candidates);
        ordered.Sort(CompareCandidates);

        float totalWeight = 0f;
        foreach (DailyV1NpcActionCandidate candidate in ordered)
            if (candidate != null && candidate.Utility > 0f)
                totalWeight += candidate.Utility;

        if (totalWeight <= 0f)
            return null;

        float randomValue = randomSource.NextUnit(randomStreamKey) * totalWeight;
        float currentWeight = 0f;
        foreach (DailyV1NpcActionCandidate candidate in ordered)
        {
            if (candidate == null || candidate.Utility <= 0f)
                continue;

            currentWeight += candidate.Utility;
            if (randomValue <= currentWeight)
                return candidate;
        }

        return null;
    }

    private static int CompareCandidates(DailyV1NpcActionCandidate left, DailyV1NpcActionCandidate right)
    {
        DailyV1ActionDefinition leftAction = left?.Action;
        DailyV1ActionDefinition rightAction = right?.Action;
        int idComparison = string.CompareOrdinal(
            leftAction?.DefinitionId ?? string.Empty,
            rightAction?.DefinitionId ?? string.Empty);
        if (idComparison != 0)
            return idComparison;

        int nameComparison = string.CompareOrdinal(
            leftAction?.Values.actionName ?? string.Empty,
            rightAction?.Values.actionName ?? string.Empty);
        if (nameComparison != 0)
            return nameComparison;

        return (leftAction?.Values.actionType?.Value ?? int.MinValue)
            .CompareTo(rightAction?.Values.actionType?.Value ?? int.MinValue);
    }

    private static bool IsAdmittedNpc(DailyV1EffectiveDefinitions definitions, DailyV1NpcDefinition npc) =>
        definitions != null && npc != null
        && definitions.Npcs.TryGetValue(npc.DefinitionId, out DailyV1NpcDefinition admitted)
        && ReferenceEquals(admitted, npc);

    private static bool IsAdmittedAction(DailyV1EffectiveDefinitions definitions, DailyV1ActionDefinition action) =>
        definitions != null && action != null
        && definitions.Actions.TryGetValue(action.DefinitionId, out DailyV1ActionDefinition admitted)
        && ReferenceEquals(admitted, action);

    private static bool TryGetExactCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (graph == null || string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                city = candidate;
                return graph.ContainsExactCityOwner(city);
            }
        return false;
    }
}

/// <summary>
/// Host projection and provider boundary for one selected Daily-v1 actor turn.
/// Core retains the candidate order, supported owner planning and deterministic
/// weighted choice; a host may resolve its existing provider-backed actions
/// without supplying a second decision engine.
/// </summary>
internal interface IDailyV1SelectedNpcActionDecisionBoundary
{
    bool HasTravelActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action);
    TravelPartyStore GetTravelPartyStore(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action);
    bool TryProjectTravelAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate);
    bool HasMerchantActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action);
    bool HasEnabledCrimeActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action);
    bool TryProjectStealAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate);
    bool TryProjectFleeCityAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate);
    bool TryProjectBuyAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate);
    bool TryProjectSellAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate);
    bool TryPrepareAutonomousProviderAction(
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate candidate,
        out float utility);
}

/// <summary>Core-owned selection half of one admitted Daily-v1 actor turn.</summary>
internal static class DailyV1SelectedNpcActionDecisionStage
{
    /// <summary>
    /// Retains selected-definition action choice for admitted Unity runtimes
    /// that predate selected-owner graph composition. Owner-backed planning is
    /// intentionally unavailable on this path, matching the host's existing
    /// provider fallback until the shared runtime graph is bound.
    /// </summary>
    internal static bool TryChooseWithoutRuntimeOwners(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition actor,
        DailyV1NpcRuntimeOwners runtimeOwner,
        string actorRuntimeId,
        IReadOnlyList<DailyV1ActionDefinition> availableActions,
        IReadOnlyList<DailyV1StatusDefinition> activeStatuses,
        long currentAbsoluteDay,
        IAuthoritativeRandomSource randomSource,
        IDailyV1SelectedNpcActionDecisionBoundary boundary,
        out DailyV1NpcActionCandidate selected)
    {
        selected = null;
        if (definitions == null || actor == null || string.IsNullOrWhiteSpace(actorRuntimeId)
            || availableActions == null || activeStatuses == null || randomSource == null
            || boundary == null
            || (runtimeOwner != null
                && (!ReferenceEquals(runtimeOwner.Definitions, definitions)
                    || !ReferenceEquals(runtimeOwner.Definition, actor)
                    || !string.Equals(runtimeOwner.RuntimeId, actorRuntimeId, StringComparison.Ordinal)))
            || !DailyV1NpcActionDecision.TryCreateCandidates(
                actor, definitions, availableActions, activeStatuses,
                out List<DailyV1NpcActionCandidate> candidates))
            return false;

        foreach (DailyV1NpcActionCandidate candidate in candidates)
        {
            if (candidate.Action.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.NormalActionTypeValue)
                continue;

            if (!boundary.TryPrepareAutonomousProviderAction(runtimeOwner, candidate, out float utility))
                candidate.Utility = 0f;
            else
                candidate.Utility = UnityMathfCompatibility.Max(0f, utility);
        }

        selected = DailyV1NpcActionDecision.ChooseWeightedCandidate(
            candidates, randomSource,
            "npc-decision|" + actorRuntimeId + "|" + currentAbsoluteDay);
        return true;
    }

    internal static bool TryChoose(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IReadOnlyList<DailyV1ActionDefinition> availableActions,
        IReadOnlyList<DailyV1StatusDefinition> activeStatuses,
        long currentAbsoluteDay,
        IAuthoritativeRandomSource randomSource,
        IDailyV1SelectedNpcActionDecisionBoundary boundary,
        out DailyV1NpcActionCandidate selected,
        PersonStore personStore = null)
    {
        selected = null;
        if (graph == null || actor == null || availableActions == null
            || randomSource == null || boundary == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || activeStatuses == null
            || !DailyV1NpcActionDecision.TryCreateCandidates(
                actor.Definition, graph.Definitions, availableActions,
                activeStatuses, out List<DailyV1NpcActionCandidate> candidates))
            return false;

        foreach (DailyV1NpcActionCandidate candidate in candidates)
        {
            int? actionType = candidate.Action.Values.actionType?.Value;
            if (actionType == DailyV1NpcDefinitionRules.NormalActionTypeValue)
                continue;

            if (actionType == DailyV1NpcDefinitionRules.TravelActionTypeValue
                && boundary.HasTravelActionProvider(actor, candidate.Action)
                && DailyV1NpcActionDecision.TryPrepareSelectedExistingTravelCandidate(
                    graph, actor, candidate,
                    boundary.GetTravelPartyStore(actor, candidate.Action)))
            {
                if (candidate.PreparedPayload == null
                    || !boundary.TryProjectTravelAction(actor, candidate))
                    candidate.Utility = 0f;
                continue;
            }

            if (actionType == DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
                && boundary.HasMerchantActionProvider(actor, candidate.Action)
                && DailyV1NpcActionDecision.TryPrepareSelectedBuyGoodsCandidate(
                    graph, actor, candidate, currentAbsoluteDay))
            {
                if (candidate.PreparedPayload == null
                    || !boundary.TryProjectBuyAction(actor, candidate))
                    candidate.Utility = 0f;
                continue;
            }

            if (actionType == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
                && boundary.HasMerchantActionProvider(actor, candidate.Action)
                && DailyV1NpcActionDecision.TryPrepareSelectedSellGoodsCandidate(
                    graph, actor, candidate, currentAbsoluteDay))
            {
                if (candidate.PreparedPayload == null
                    || !boundary.TryProjectSellAction(actor, candidate))
                    candidate.Utility = 0f;
                continue;
            }

            if (actionType == DailyV1NpcDefinitionRules.HideActionTypeValue
                && boundary.HasEnabledCrimeActionProvider(actor, candidate.Action))
            {
                if (!DailyV1CrimeActionPlanning.TryPrepareSelectedHide(graph, actor, candidate))
                    candidate.Utility = 0f;
                continue;
            }

            if (actionType == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
                && boundary.HasEnabledCrimeActionProvider(actor, candidate.Action))
            {
                if (!DailyV1CrimeActionPlanning.TryPrepareSelectedEscapePrison(graph, actor, candidate))
                    candidate.Utility = 0f;
                continue;
            }

            if (actionType == DailyV1NpcDefinitionRules.StealActionTypeValue
                && boundary.HasEnabledCrimeActionProvider(actor, candidate.Action))
            {
                if (!DailyV1CrimeActionPlanning.TryPrepareSelectedSteal(
                        graph, actor, candidate, personStore, currentAbsoluteDay, randomSource)
                    || candidate.PreparedPayload == null
                    || !boundary.TryProjectStealAction(actor, candidate))
                    candidate.Utility = 0f;
                continue;
            }

            if (actionType == DailyV1NpcDefinitionRules.FleeCityActionTypeValue
                && boundary.HasEnabledCrimeActionProvider(actor, candidate.Action))
            {
                if (!DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
                        graph, actor, candidate)
                    || candidate.PreparedPayload == null
                    || !boundary.TryProjectFleeCityAction(actor, candidate))
                    candidate.Utility = 0f;
                continue;
            }

            if (!boundary.TryPrepareAutonomousProviderAction(actor, candidate, out float utility))
                candidate.Utility = 0f;
            else
                candidate.Utility = UnityMathfCompatibility.Max(0f, utility);
        }

        selected = DailyV1NpcActionDecision.ChooseWeightedCandidate(
            candidates, randomSource,
            "npc-decision|" + actor.RuntimeId + "|" + currentAbsoluteDay);
        return true;
    }
}
