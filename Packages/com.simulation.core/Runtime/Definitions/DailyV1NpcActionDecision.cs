using System;
using System.Collections.Generic;

/// <summary>A selected, graph-owned action and its current decision weight.</summary>
internal sealed class DailyV1NpcActionCandidate
{
    internal DailyV1ActionDefinition Action { get; }
    internal float Utility { get; set; }
    internal DailyV1NpcActionPayloadOwner PreparedPayload { get; set; }

    internal DailyV1NpcActionCandidate(DailyV1ActionDefinition action, float utility)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Utility = utility;
    }
}

/// <summary>Core action eligibility, utility and deterministic selection for selected Daily-v1 definitions.</summary>
internal static class DailyV1NpcActionDecision
{
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
}
