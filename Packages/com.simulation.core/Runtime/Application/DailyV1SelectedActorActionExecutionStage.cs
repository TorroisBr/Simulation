using System;
using System.Collections.Generic;

/// <summary>
/// Core-owned autonomous action lifecycle for a selected Daily-v1 actor.
/// Selection, exact-slot installation, dispatch and post-dispatch status
/// planning are kept in one ordered operation; the host provides only the
/// existing projection, P12 admission/effect and output ports.
/// </summary>
internal static class DailyV1SelectedActorActionExecutionStage
{
    internal static bool TryExecuteAutonomousAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        PersonStore personStore,
        IAuthoritativeRandomSource randomSource,
        long absoluteDay,
        IDailyV1SelectedActorActionExecutionBoundary boundary)
    {
        if (graph == null || actor == null || personStore == null
            || randomSource == null || boundary == null
            || absoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || actor.Status == null || actor.ActionSlot == null)
            return false;

        if (!boundary.TryCreateDecisionBoundary(actor,
                out IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary)
            || decisionBoundary == null)
            return true; // No configured decision service preserves the old no-op turn.

        if (!boundary.TryGetConfiguredActionIds(actor, out IReadOnlyList<string> actionIds)
            || !DailyV1SelectedNpcActionDecisionStage.TryResolveConfiguredActions(
                graph.Definitions, actionIds, out IReadOnlyList<DailyV1ActionDefinition> availableActions)
            || availableActions == null)
            return false;

        foreach (DailyV1ActionDefinition configuredAction in availableActions)
        {
            if (configuredAction == null
                || !graph.Definitions.Actions.TryGetValue(
                    configuredAction.DefinitionId, out DailyV1ActionDefinition admittedAction)
                || !ReferenceEquals(configuredAction, admittedAction))
                return false;
        }

        if (!DailyV1SelectedNpcActionDecisionStage.TryChoose(
                graph, actor, availableActions, actor.Status.Statuses,
                absoluteDay, randomSource, decisionBoundary,
                out DailyV1NpcActionCandidate selected, personStore))
            return false;

        // Preserve the legacy no-selection clear and the exact decision-record,
        // origin-id, P12 admission, Core slot and Unity projection ordering.
        if (selected == null)
            boundary.ClearCurrentAction(actor);
        else if (!boundary.TryRecordAndInstallSelectedAction(
                     actor, selected, decisionBoundary, NpcDecisionOrigin.Autonomous))
            throw new InvalidOperationException(
                "The exact selected Daily-v1 action could not be installed through its admitted Core owner boundary.");

        DailyV1ActionDefinition action = actor.ActionSlot.CurrentAction;
        if (action == null) return true;

        if (action.Values.actionType?.Value == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            && !boundary.IsActorAtCurrentCityLocation(actor))
        {
            boundary.ClearCurrentAction(actor);
            return true;
        }

        return TryExecuteInstalledAction(
            graph, actor, randomSource, absoluteDay, boundary,
            out _, out _, out _);
    }

    internal static void ProcessScheduledRequestAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        ScheduledDirective directive,
        IAuthoritativeRandomSource randomSource,
        long absoluteDay,
        IDailyV1SelectedActorActionExecutionBoundary boundary)
    {
        const string incompatibleReason =
            "Actor is not in a compatible state for the requested action.";
        if (graph == null || actor == null || directive == null
            || randomSource == null || boundary == null || absoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !string.Equals(directive.ActorRuntimeId, actor.RuntimeId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The scheduled Daily-v1 action did not target its exact admitted actor owner.");

        boundary.ClearCurrentAction(actor);
        if (directive.Mode != ScheduledDirectiveMode.RequestAction
            || directive.Operation != ScheduledDirectiveOperation.EscapePrison
            || !graph.Definitions.Actions.TryGetValue(
                directive.ActionDefinitionId, out DailyV1ActionDefinition action)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
            || !DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.HasRequiredActionStatuses(
                graph.Definitions, action, actor.Status?.Statuses))
        {
            directive.MarkSkipped(absoluteDay, incompatibleReason);
            return;
        }

        DailyV1NpcActionCandidate requested = new DailyV1NpcActionCandidate(action, 0f);
        if (!boundary.TryCreateDecisionBoundary(actor,
                out IDailyV1SelectedNpcActionDecisionBoundary decisionBoundary)
            || decisionBoundary == null
            || !boundary.HasRequestedActionProvider(actor, action)
            || !DailyV1CrimeActionPlanning.TryPrepareSelectedEscapePrison(
                graph, actor, requested)
            || requested.PreparedPayload == null)
        {
            directive.MarkSkipped(absoluteDay, incompatibleReason);
            return;
        }

        if (!boundary.TryRecordAndInstallSelectedAction(
                actor, requested, decisionBoundary, NpcDecisionOrigin.ScheduledDirective))
        {
            directive.MarkSkipped(absoluteDay, incompatibleReason);
            return;
        }

        if (!TryExecuteInstalledAction(
                graph, actor, randomSource, absoluteDay, boundary,
                out bool actionSucceeded,
                out NpcActionResultType? resultType,
                out string resultMessage))
            throw new InvalidOperationException(
                "The scheduled Daily-v1 action could not complete through its exact Core owner graph.");

        if (actionSucceeded && resultType == NpcActionResultType.Success)
        {
            directive.MarkSucceeded(absoluteDay);
            return;
        }

        string failureReason = string.IsNullOrEmpty(resultMessage)
            ? "Requested action was attempted and failed."
            : resultMessage;
        directive.MarkFailed(absoluteDay, failureReason);
    }

    internal static void ProcessScheduledForceOutcome(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        ScheduledDirective directive,
        long absoluteDay,
        IDailyV1SelectedActorActionExecutionBoundary boundary)
    {
        if (graph == null || actor == null || directive == null
            || boundary == null || absoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !string.Equals(directive.ActorRuntimeId, actor.RuntimeId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The scheduled Daily-v1 forced outcome did not target its exact admitted actor owner.");

        boundary.ClearCurrentAction(actor);
        if (directive.Mode != ScheduledDirectiveMode.ForceOutcome
            || directive.Operation != ScheduledDirectiveOperation.EscapePrison
            || graph.JusticeRecordsOwner == null)
        {
            MarkForcedEscapeSkipped(boundary, directive, absoluteDay,
                "Escape domain operation is unavailable.");
            return;
        }

        if (!graph.Definitions.Actions.TryGetValue(
                directive.ActionDefinitionId, out DailyV1ActionDefinition action)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
            || !DailyV1NpcDefinitionRules.IsAdmittedAction(graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                graph.Definitions, action, out _, out _, out _, out _))
        {
            MarkForcedEscapeSkipped(boundary, directive, absoluteDay,
                "Admitted Escape status effects are unavailable.");
            return;
        }

        string arrestedStatusId = graph.Definitions.Configuration?.arrestedStatus;
        if (string.IsNullOrWhiteSpace(arrestedStatusId)
            || !graph.Definitions.Statuses.TryGetValue(
                arrestedStatusId, out DailyV1StatusDefinition arrestedStatus)
            || actor.Status == null || !actor.Status.Contains(arrestedStatus))
        {
            MarkForcedEscapeSkipped(boundary, directive, absoluteDay,
                "Actor is not arrested; escape outcome is incompatible with current state.");
            return;
        }

        if (!graph.TryCreateActionStatusPlan(actor, action, false, null, out _))
        {
            MarkForcedEscapeSkipped(boundary, directive, absoluteDay,
                "Admitted Escape status effects are unavailable.");
            return;
        }

        if (!boundary.TryInstallForcedOutcomeAction(actor, action))
        {
            MarkForcedEscapeSkipped(boundary, directive, absoluteDay,
                "Admitted Escape status effects are unavailable.");
            return;
        }

        if (!boundary.TryGetForcedEscapeBoundary(actor, action,
                out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary crimeBoundary)
            || crimeBoundary == null
            || !boundary.TryGetActionStatusBoundary(actor, null, reuseExistingOperation: true,
                out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary statusBoundary)
            || statusBoundary == null
            || !DailyV1SelectedCrimeActionExecutionStage.TryExecuteForcedEscapeOutcome(
                graph, actor, action, crimeBoundary, statusBoundary, out bool escaped))
            throw new InvalidOperationException(
                "The selected Daily-v1 forced Escape could not execute through its exact admitted Crime/Justice owner boundary.");

        if (!escaped)
        {
            MarkForcedEscapeFailed(directive, absoluteDay,
                "Canonical escape transition rejected the forced outcome.");
            return;
        }

        if (!directive.MarkSucceeded(absoluteDay))
            throw new InvalidOperationException(
                "The scheduled Daily-v1 forced Escape changed owner state but could not commit its terminal directive result.");
    }

    private static void MarkForcedEscapeSkipped(
        IDailyV1SelectedActorActionExecutionBoundary boundary,
        ScheduledDirective directive,
        long absoluteDay,
        string reason)
    {
        if (!directive.MarkSkipped(absoluteDay, reason))
            throw new InvalidOperationException(
                "The selected Daily-v1 forced Escape could not commit its skipped directive result.");
        boundary.LogSkippedDirective(directive, reason);
    }

    private static void MarkForcedEscapeFailed(
        ScheduledDirective directive,
        long absoluteDay,
        string reason)
    {
        if (!directive.MarkFailed(absoluteDay, reason))
            throw new InvalidOperationException(
                "The selected Daily-v1 forced Escape could not commit its failed directive result.");
    }

    /// <summary>
    /// Executes an already-installed exact selected action slot. This is shared
    /// by autonomous turns, actor-choice dispatch and scheduled action requests;
    /// it never selects, replaces, or falls back to a Unity provider.
    /// </summary>
    internal static bool TryExecuteInstalledAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IAuthoritativeRandomSource randomSource,
        long absoluteDay,
        IDailyV1SelectedActorActionExecutionBoundary boundary,
        out bool actionSucceeded,
        out NpcActionResultType? resultType,
        out string resultMessage)
    {
        actionSucceeded = false;
        resultType = null;
        resultMessage = null;
        if (graph == null || actor == null || randomSource == null
            || boundary == null || absoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || actor.ActionSlot == null)
            return false;

        DailyV1ActionDefinition action = actor.ActionSlot.CurrentAction;
        if (action == null) return true;

        boundary.LogChosenActionTarget(actor);
        if (!DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                graph.Definitions, action, out _, out _, out _, out _))
        {
            resultType = NpcActionResultType.Failure;
            return true;
        }

        if (!boundary.HasExactCurrentActionProjection(actor)
            || !boundary.TryGetDispatchBoundary(actor,
                out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
            || dispatchBoundary == null
            || !DailyV1SelectedActionDispatchStage.TryDispatch(
                graph, actor, randomSource, absoluteDay, dispatchBoundary,
                out DailyV1SelectedActionDispatchResult dispatch)
            || dispatch == null)
            throw new InvalidOperationException(
                "The exact selected Daily-v1 action could not execute through its Core owner operation.");

        if (!boundary.TryCompleteDispatch(
                actor, dispatch, out actionSucceeded, out resultType,
                out resultMessage))
            throw new InvalidOperationException(
                "The selected Daily-v1 action result could not be published by its host adapter.");

        if (!actionSucceeded) return true;

        DailyV1NpcRuntimeOwners target = null;
        DailyV1NpcActionPayloadOwner payload = actor.ActionSlot.CurrentPayload;
        bool targetExpected = !string.IsNullOrWhiteSpace(payload?.TargetNpcRuntimeId);
        if (targetExpected
            && (!graph.TryGetNpcOwner(payload.TargetNpcRuntimeId, out target)
                || !graph.ContainsExactNpcOwner(target)))
            throw new InvalidOperationException(
                "A successful selected action referenced a target outside the exact runtime owner graph.");

        if (!boundary.TryGetActionStatusBoundary(actor, target,
                reuseExistingOperation: false,
                out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary statusBoundary)
            || !DailyV1SelectedActionExecutionStage.TryApplyActionStatusPlanAfterDispatch(
                true, graph, actor, action, targetExpected, target, statusBoundary,
                out DailyV1SelectedActionStatusPlanResult statusResult)
            || statusResult == DailyV1SelectedActionStatusPlanResult.ActorRejected
            || statusResult == DailyV1SelectedActionStatusPlanResult.TargetRejected)
            throw new InvalidOperationException(
                "The selected Daily-v1 action status plan could not commit through its existing owner boundary.");

        return true;
    }
}
