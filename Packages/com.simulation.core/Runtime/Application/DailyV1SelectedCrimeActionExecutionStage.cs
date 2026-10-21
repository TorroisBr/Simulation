using System;

/// <summary>
/// Core-owned selected Hide and EscapePrison transitions. Unity supplies only
/// the existing P12 operation scope, identity-checked projections and output
/// publication; the selected Justice and NPC owners remain authoritative.
/// </summary>
internal static class DailyV1SelectedCrimeActionExecutionStage
{
    internal interface ISelectedCrimeActionBoundary
    {
        DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }
        bool ExecuteWithinOperation(Func<bool> operation);
        bool ApplyHideDuration(DailyV1NpcRuntimeOwners actor, int followingFullDays);
        bool ClearHidden(DailyV1NpcRuntimeOwners actor);
        bool ApplyStatusChange(DailyV1NpcRuntimeOwners actor, DailyV1StatusDefinition status, bool add);
        bool TryRemoveSentence(DailyV1JusticeSentenceOwner sentence);
        void PublishHidden(DailyV1NpcRuntimeOwners actor);
        void PublishFailedEscape(DailyV1NpcRuntimeOwners actor, int sentencePenalty);
        void PublishEscaped(
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners eventCity,
            bool hadActiveSentence,
            float bountyAfterPenalty,
            string originDecisionId);
    }

    internal sealed class Result
    {
        internal bool ActionRollSucceeded { get; }
        internal bool Succeeded { get; }

        internal Result(bool actionRollSucceeded, bool succeeded)
        {
            ActionRollSucceeded = actionRollSucceeded;
            Succeeded = succeeded;
        }
    }

    internal static bool TryExecuteSelectedActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        IAuthoritativeRandomSource randomSource,
        long currentAbsoluteDay,
        ISelectedCrimeActionBoundary boundary,
        out Result result)
    {
        result = null;
        if (graph == null || actor == null || action == null || boundary == null
            || currentAbsoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !ReferenceEquals(action, actor.ActionSlot?.CurrentAction)
            || !ReferenceEquals(action, actor.ActionSlot?.CurrentPayload?.Action)
            || !ReferenceEquals(graph.JusticeRecordsOwner, boundary.JusticeRecordsOwner)
            || !IsSelectedCrimeAction(action.Values.actionType?.Value)
            || !HasEmptyTargets(actor.ActionSlot.CurrentPayload))
            return false;

        if (!DailyV1SelectedActionExecutionStage.TryRollCurrentActionSlot(
                graph, actor, randomSource, currentAbsoluteDay, out bool actionRollSucceeded))
            return false;

        if (!actionRollSucceeded)
        {
            // Escape failure still records its sentence penalty under the same
            // immediate action scope. Hide has no failure mutation.
            boundary.ExecuteWithinOperation(() => action.Values.actionType.Value
                == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
                    ? TryRegisterFailedEscape(graph, actor, action, boundary)
                    : false);
            result = new Result(false, false);
            return true;
        }

        bool succeeded = boundary.ExecuteWithinOperation(() =>
            action.Values.actionType.Value == DailyV1NpcDefinitionRules.HideActionTypeValue
                ? TryApplyHide(graph, actor, action, boundary)
                : TryApplyEscape(graph, actor, action, boundary, allowSameDayArrest: false));
        result = new Result(true, succeeded);
        return true;
    }

    /// <summary>
    /// Applies an already admitted forced Escape outcome. Unlike the ordinary
    /// action path this operation performs no roll and retains the legacy
    /// ForceOutcome rule that permits an escape on the day of arrest.
    /// </summary>
    internal static bool TryExecuteForcedEscapeOutcome(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        ISelectedCrimeActionBoundary crimeBoundary,
        DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary statusBoundary,
        out bool escaped)
    {
        escaped = false;
        if (graph == null || actor == null || action == null
            || crimeBoundary == null || statusBoundary == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !ReferenceEquals(action, actor.ActionSlot?.CurrentAction)
            || !ReferenceEquals(action, actor.ActionSlot?.CurrentPayload?.Action)
            || !ReferenceEquals(graph.JusticeRecordsOwner, crimeBoundary.JusticeRecordsOwner)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
            || !HasEmptyTargets(actor.ActionSlot.CurrentPayload)
            || !TryResolveRequiredStatus(graph.Definitions,
                graph.Definitions.Configuration?.arrestedStatus,
                out DailyV1StatusDefinition arrestedStatus)
            || actor.Status == null || !actor.Status.Contains(arrestedStatus)
            || !graph.TryCreateActionStatusPlan(actor, action, false, null, out _))
            return false;

        bool operationSucceeded = crimeBoundary.ExecuteWithinOperation(() =>
        {
            if (!TryApplyEscape(graph, actor, action, crimeBoundary,
                    allowSameDayArrest: true))
                return false;

            if (!DailyV1SelectedActionExecutionStage.TryApplyActionStatusPlanAfterDispatch(
                    true, graph, actor, action, false, null, statusBoundary,
                    out DailyV1SelectedActionStatusPlanResult statusResult)
                || statusResult == DailyV1SelectedActionStatusPlanResult.ActorRejected
                || statusResult == DailyV1SelectedActionStatusPlanResult.TargetRejected)
                throw new InvalidOperationException(
                    "The forced selected Daily-v1 Escape status plan could not commit inside its admitted Crime/Justice operation.");
            return true;
        });

        escaped = operationSucceeded;
        return true;
    }

    private static bool TryApplyHide(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        ISelectedCrimeActionBoundary boundary)
    {
        if (!TryGetCanActCity(graph, actor, out DailyV1CityRuntimeOwners city)
            || actor.Status == null
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, action, actor.Status.Statuses)
            || graph.JusticeRecordsOwner.FindActiveWantedRecord(actor.RuntimeId, city.RuntimeId) == null)
            return false;

        DailyV1CrimeActionSettings settings = DailyV1NpcDefinitionRules.GetCrimeActionSettings(
            graph.Definitions, action);
        int hiddenDays = UnityMathfCompatibility.Max(1, settings?.hiddenDays ?? 1);
        if (!boundary.ApplyHideDuration(actor, hiddenDays)) return false;

        if (!TryResolveOptionalStatus(graph.Definitions,
                graph.Definitions.Configuration?.hiddenStatus,
                out DailyV1StatusDefinition hiddenStatus)
            || (hiddenStatus != null && !boundary.ApplyStatusChange(actor, hiddenStatus, add: true)))
            return false;

        boundary.PublishHidden(actor);
        return true;
    }

    private static bool TryRegisterFailedEscape(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        ISelectedCrimeActionBoundary boundary)
    {
        DailyV1JusticeSentenceOwner sentence = graph.JusticeRecordsOwner.FindActiveSentence(actor.RuntimeId);
        if (sentence == null || sentence.Warrant == null || sentence.Warrant.IsResolved)
            return false;

        int penalty = UnityMathfCompatibility.Max(0,
            DailyV1NpcDefinitionRules.GetCrimeActionSettings(graph.Definitions, action)
                ?.failedEscapeSentencePenalty ?? 0);
        if (!sentence.TryRegisterFailedEscape(penalty)) return false;
        boundary.PublishFailedEscape(actor, penalty);
        return true;
    }

    private static bool TryApplyEscape(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        ISelectedCrimeActionBoundary boundary,
        bool allowSameDayArrest)
    {
        if (!TryResolveRequiredStatus(graph.Definitions,
                graph.Definitions.Configuration?.arrestedStatus,
                out DailyV1StatusDefinition arrestedStatus)
            || actor.Status == null
            || !actor.Status.Contains(arrestedStatus))
            return false;

        DailyV1JusticeRecordsOwner justice = graph.JusticeRecordsOwner;
        DailyV1JusticeSentenceOwner sentence = justice.FindActiveSentence(actor.RuntimeId);
        if (!allowSameDayArrest && sentence?.WasArrestedToday == true) return false;

        DailyV1JusticeWantedRecordOwner warrant = sentence?.Warrant;
        bool hasActiveSentence = sentence != null && warrant != null && !warrant.IsResolved;
        DailyV1CityRuntimeOwners eventCity = null;
        string eventCityRuntimeId = hasActiveSentence
            ? sentence.CityRuntimeId
            : actor.Travel?.CurrentCityRuntimeId;
        if (hasActiveSentence)
        {
            if (!TryFindExactCity(graph, eventCityRuntimeId, out eventCity)) return false;
        }
        else if (!string.IsNullOrWhiteSpace(eventCityRuntimeId)
            && !TryFindExactCity(graph, eventCityRuntimeId, out eventCity))
            return false;

        float bountyAfterPenalty = warrant?.Bounty ?? 0f;
        if (hasActiveSentence)
        {
            float penalty = DailyV1NpcDefinitionRules.GetCrimeActionSettings(
                graph.Definitions, action)?.escapeBountyPenalty ?? 0f;
            if (penalty > 0f && !warrant.TryAddPenalty(penalty, 0))
                return false;
            bountyAfterPenalty = warrant.Bounty;
            if (!boundary.TryRemoveSentence(sentence)) return false;
        }

        if (!ApplyReleaseStatuses(graph, actor, arrestedStatus, boundary,
                clearHidden: !hasActiveSentence))
            return false;

        boundary.PublishEscaped(actor, eventCity, hasActiveSentence,
            bountyAfterPenalty, actor.ActionSlot.CurrentPayload?.OriginDecisionId);
        return true;
    }

    private static bool ApplyReleaseStatuses(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1StatusDefinition arrestedStatus,
        ISelectedCrimeActionBoundary boundary,
        bool clearHidden)
    {
        if (!TryResolveOptionalStatus(graph.Definitions,
                graph.Definitions.Configuration?.freeStatus,
                out DailyV1StatusDefinition freeStatus)
            || !TryResolveOptionalStatus(graph.Definitions,
                graph.Definitions.Configuration?.hiddenStatus,
                out DailyV1StatusDefinition hiddenStatus)
            || !TryResolveOptionalStatus(graph.Definitions,
                graph.Definitions.Configuration?.wantedStatus,
                out DailyV1StatusDefinition wantedStatus))
            return false;

        if (!boundary.ApplyStatusChange(actor, arrestedStatus, add: false)) return false;
        if (freeStatus != null && !boundary.ApplyStatusChange(actor, freeStatus, add: true)) return false;
        if (clearHidden)
        {
            if (!boundary.ClearHidden(actor)) return false;
            if (hiddenStatus != null && !boundary.ApplyStatusChange(actor, hiddenStatus, add: false))
                return false;
        }

        bool hasActiveWarrant = graph.JusticeRecordsOwner.FindActiveWantedRecords(actor.RuntimeId).Count > 0;
        if (wantedStatus != null
            && !boundary.ApplyStatusChange(actor, wantedStatus, add: hasActiveWarrant))
            return false;
        return true;
    }

    private static bool TryGetCanActCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (graph == null || actor == null || actor.Lifecycle?.LifeState != NpcLifeState.Alive
            || actor.Travel == null || actor.Travel.IsTraveling
            || !TryFindExactCity(graph, actor.Travel.CurrentCityRuntimeId, out city))
            return false;

        string arrestedStatusId = graph.Definitions.Configuration?.arrestedStatus;
        if (string.IsNullOrWhiteSpace(arrestedStatusId)) return true;
        return graph.Definitions.Statuses.TryGetValue(arrestedStatusId,
                   out DailyV1StatusDefinition arrestedStatus)
            && actor.Status?.Contains(arrestedStatus) != true;
    }

    private static bool TryResolveOptionalStatus(
        DailyV1EffectiveDefinitions definitions,
        string statusId,
        out DailyV1StatusDefinition status)
    {
        status = null;
        return string.IsNullOrWhiteSpace(statusId)
            || definitions.Statuses.TryGetValue(statusId, out status);
    }

    private static bool TryResolveRequiredStatus(
        DailyV1EffectiveDefinitions definitions,
        string statusId,
        out DailyV1StatusDefinition status)
    {
        status = null;
        return !string.IsNullOrWhiteSpace(statusId)
            && definitions.Statuses.TryGetValue(statusId, out status);
    }

    private static bool TryFindExactCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string runtimeId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (graph == null || string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            if (candidate != null
                && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                if (city != null || !graph.ContainsExactCityOwner(candidate)) return false;
                city = candidate;
            }
        return city != null;
    }

    private static bool HasEmptyTargets(DailyV1NpcActionPayloadOwner payload) =>
        payload != null
        && payload.TargetNpcRuntimeId == null
        && payload.TargetCityRuntimeId == null
        && payload.TargetItem == null
        && payload.Amount == 0;

    private static bool IsSelectedCrimeAction(int? actionType) =>
        actionType == DailyV1NpcDefinitionRules.HideActionTypeValue
        || actionType == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue;
}
