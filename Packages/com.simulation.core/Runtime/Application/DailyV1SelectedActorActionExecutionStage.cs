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
    /// <summary>
    /// Resolves an action's authoritative result from the dispatch produced by
    /// Core. A host may publish the matching Unity projection and presentation
    /// message, but it cannot change whether the admitted owner operation
    /// succeeded.
    /// </summary>
    internal static bool TryResolveDispatchOutcome(
        DailyV1SelectedActionDispatchResult dispatch,
        out bool actionSucceeded,
        out NpcActionResultType resultType)
    {
        actionSucceeded = false;
        resultType = default;
        if (dispatch == null
            || !Enum.IsDefined(typeof(DailyV1SelectedActionDispatchKind), dispatch.Kind)
            || (!dispatch.ActionRollSucceeded && dispatch.Succeeded))
            return false;

        bool detailedSuccess;
        switch (dispatch.Kind)
        {
            case DailyV1SelectedActionDispatchKind.Normal:
                detailedSuccess = true;
                break;
            case DailyV1SelectedActionDispatchKind.PlannedNpcSale:
                detailedSuccess = dispatch.PlannedNpcSale?.Succeeded == true
                    && dispatch.PlannedNpcSale.Sale?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.PlannedMarketSale:
                detailedSuccess = dispatch.PlannedMarketSale?.Succeeded == true
                    && dispatch.PlannedMarketSale.Sale?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.OpenMarket:
                detailedSuccess = dispatch.OpenMarket?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.CrossCityBuy:
                detailedSuccess = dispatch.CrossCityBuy?.Purchase?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.SoloTravel:
                detailedSuccess = dispatch.SoloTravel?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.FleeCity:
                detailedSuccess = dispatch.FleeCity?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.GuardArrest:
                detailedSuccess = dispatch.GuardArrest?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.Steal:
                detailedSuccess = dispatch.Steal?.Succeeded == true;
                break;
            case DailyV1SelectedActionDispatchKind.Crime:
                detailedSuccess = dispatch.Crime?.Succeeded == true;
                break;
            default:
                return false;
        }

        if (dispatch.Succeeded && !detailedSuccess)
            return false;

        actionSucceeded = dispatch.ActionRollSucceeded && dispatch.Succeeded;
        resultType = actionSucceeded
            ? NpcActionResultType.Success
            : NpcActionResultType.Failure;
        return true;
    }

    /// <summary>
    /// Identifies a targetless Normal action whose exact Core slot and typed
    /// action-status plan are sufficient for dispatch. Actor status effects
    /// still commit through the Core status owner and the existing host P12
    /// operation boundary; no Unity NpcActionRuntime is needed.
    /// </summary>
    internal static bool IsProjectionIndependentNormalAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action)
    {
        if (!TryGetProjectionIndependentNormalAction(
                graph, actor, action, out DailyV1NpcActionPayloadOwner payload,
                out _, out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd))
            return false;

        return payload.TargetNpcRuntimeId == null
            && targetStatusToRemove.Count == 0
            && targetStatusToAdd.Count == 0;
    }

    /// <summary>
    /// Identifies a Normal action whose target is the exact admitted Core NPC
    /// owner named by its slot payload. This permits the existing Core target
    /// status plan to execute without requiring a Unity action projection.
    /// </summary>
    internal static bool IsProjectionIndependentNormalActionForExactTarget(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        DailyV1NpcRuntimeOwners target)
    {
        if (target == null
            || !TryGetProjectionIndependentNormalAction(
                graph, actor, action, out DailyV1NpcActionPayloadOwner payload,
                out _, out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd))
            return false;

        return graph.ContainsExactNpcOwner(target)
            && ReferenceEquals(target.Definitions, graph.Definitions)
            && string.Equals(payload.TargetNpcRuntimeId, target.RuntimeId, StringComparison.Ordinal)
            && (targetStatusToRemove.Count > 0 || targetStatusToAdd.Count > 0);
    }

    private static bool TryGetProjectionIndependentNormalAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1ActionDefinition action,
        out DailyV1NpcActionPayloadOwner payload,
        out IReadOnlyList<DailyV1StatusDefinition> actorStatusToRemove,
        out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
        out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd)
    {
        payload = null;
        actorStatusToRemove = null;
        targetStatusToRemove = null;
        targetStatusToAdd = null;
        if (graph == null || actor == null || action == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || actor.ActionSlot == null
            || !ReferenceEquals(actor.ActionSlot.CurrentAction, action)
            || !graph.Definitions.Actions.TryGetValue(
                action.DefinitionId, out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(action, admittedAction)
            || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.NormalActionTypeValue
            || !DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                graph.Definitions, action,
                out _,
                out actorStatusToRemove,
                out targetStatusToRemove,
                out targetStatusToAdd))
            return false;

        payload = actor.ActionSlot.CurrentPayload;
        return payload != null
            && ReferenceEquals(payload.Action, action)
            && payload.TargetCityRuntimeId == null
            && payload.TargetItem == null
            && payload.Amount == 0
            && payload.ExpectedUnitPrice == 0f
            && payload.TravelReason == NpcTravelReason.None
            && payload.ExpectedNetValue == 0f
            && payload.CommercialDecisionEvidence == null
            && payload.CommercialScoutingEvidence == null;
    }

    private static bool TryGetActionProjectionBoundary(
        DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedActorActionExecutionBoundary boundary,
        out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
    {
        projectionBoundary = null;
        if (boundary == null) return false;
        if (!boundary.RequiresActionProjection) return true;
        return boundary.TryCreateActionProjectionBoundary(actor, out projectionBoundary)
            && projectionBoundary != null;
    }

    internal static bool TryExecuteAutonomousAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        PersonStore personStore,
        IAuthoritativeRandomSource randomSource,
        long absoluteDay,
        IDailyV1SelectedActorActionExecutionBoundary boundary,
        DailyV1ActorTurnCoreRoots coreRoots = null)
    {
        if (coreRoots != null)
        {
            if (!coreRoots.IsBoundToGraph(graph))
                throw new InvalidOperationException(
                    "The selected action received Core decision roots from another owner graph.");
            randomSource = coreRoots.RandomSource;
        }
        if (graph == null || actor == null || personStore == null
            || randomSource == null || boundary == null
            || absoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || actor.Status == null || actor.ActionSlot == null)
            return false;

        if (!TryGetActionProjectionBoundary(actor, boundary,
                out IDailyV1SelectedActionProjectionBoundary projectionBoundary))
            return true; // A missing required host projection preserves the existing no-action turn.

        if (!DailyV1SelectedNpcActionDecisionStage.TryResolveConfiguredActions(
                graph.Definitions, out IReadOnlyList<DailyV1ActionDefinition> availableActions)
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
                absoluteDay, randomSource,
                out DailyV1NpcActionCandidate selected, personStore, projectionBoundary))
            return false;

        // Preserve the legacy no-selection clear and the exact decision-record,
        // origin-id, P12 admission, Core slot and Unity projection ordering.
        if (selected == null)
        {
            if (!TryClearCurrentAction(graph, actor, boundary)) return false;
        }
        else if (!TryCommitSelectedActionSlot(
                     graph, actor, selected, projectionBoundary, NpcDecisionOrigin.Autonomous,
                     absoluteDay, boundary, coreRoots))
            throw new InvalidOperationException(
                "The exact selected Daily-v1 action could not be installed through its admitted Core owner boundary.");

        DailyV1ActionDefinition action = actor.ActionSlot.CurrentAction;
        if (action == null) return true;

        if (action.Values.actionType?.Value == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue
            && !graph.IsActorAtCurrentCityLocation(actor))
        {
            if (!TryClearCurrentAction(graph, actor, boundary)) return false;
            return true;
        }

        return TryExecuteInstalledAction(
            graph, actor, randomSource, absoluteDay, boundary,
            out _, out _, out _);
    }

    private static bool TryCommitSelectedActionSlot(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionCandidate selected,
        IDailyV1SelectedActionProjectionBoundary projectionBoundary,
        NpcDecisionOrigin origin,
        long absoluteDay,
        IDailyV1SelectedActorActionExecutionBoundary boundary,
        DailyV1ActorTurnCoreRoots coreRoots)
    {
        DailyV1NpcActionPayloadOwner payload = selected?.PreparedPayload;
        bool requiresActionProjection = boundary?.RequiresActionProjection ?? true;
        if (graph == null || actor == null || selected?.Action == null
            || boundary == null
            || (requiresActionProjection && projectionBoundary == null)
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions))
            return false;
        if (coreRoots != null && !coreRoots.IsBoundToGraph(graph))
            throw new InvalidOperationException(
                "The selected action slot cannot use decision roots from another owner graph.");

        if (payload == null
            && !DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, selected.Action, null, null, null, 0,
                0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
                out payload))
            return false;
        selected.PreparedPayload = payload;

        Func<DailyV1NpcActionPayloadOwner, bool> prepareProjection = requiresActionProjection
            ? candidate => ReferenceEquals(candidate, payload)
                && boundary.TryPrepareSelectedActionProjection(
                    actor, selected, projectionBoundary, candidate)
            : null;
        Func<bool> slotAdmission = () => (!requiresActionProjection
                || boundary.CanCommitSelectedActionSlot(actor, payload))
            && CanCommitSelectedActionSlotP12Mutation(graph, actor, boundary);
        RuntimeIdAllocator idAllocator;
        SimulationRecordSequence recordSequence;
        NpcDecisionStore decisionStore;
        bool hasDecisionOwners;
        if (coreRoots != null)
        {
            idAllocator = coreRoots.IdAllocator;
            recordSequence = coreRoots.RecordSequence;
            decisionStore = coreRoots.DecisionStore;
            hasDecisionOwners = true;
        }
        else if (boundary is IDailyV1ActorChoiceExecutionBoundary actorChoiceBoundary)
        {
            hasDecisionOwners = actorChoiceBoundary.TryGetDecisionRecordOwners(
                actor, out idAllocator, out recordSequence, out decisionStore);
        }
        else
        {
            idAllocator = null;
            recordSequence = null;
            decisionStore = null;
            hasDecisionOwners = false;
        }

        bool committed;
        bool existingTravelDecision = selected.Action.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.TravelActionTypeValue
            && !string.IsNullOrWhiteSpace(payload.OriginDecisionId);

        if (!hasDecisionOwners || existingTravelDecision)
        {
            committed = DailyV1DecisionRecordOperation.TryCommitSelectedActionSlotWithExistingIdentity(
                graph, actor, payload, prepareProjection, slotAdmission);
        }
        else
        {
            try
            {
                committed = DailyV1DecisionRecordOperation.TryRecordAndCommitSelectedActionSlot(
                    idAllocator, recordSequence, decisionStore, absoluteDay,
                    graph, actor, payload,
                    DailyV1DecisionRecordOperation.GetDecisionType(selected.Action, payload),
                    origin,
                    prepareProjection, slotAdmission, out _);
            }
            catch (ArgumentException exception)
            {
                boundary.LogSelectedActionDecisionFailure(actor,
                    "Cannot create NPC decision record: " + exception.Message);
                committed = DailyV1DecisionRecordOperation.TryCommitSelectedActionSlotWithExistingIdentity(
                    graph, actor, payload, prepareProjection, slotAdmission);
            }
            catch (InvalidOperationException exception)
            {
                boundary.LogSelectedActionDecisionFailure(actor,
                    "Cannot allocate NPC decision record identity: " + exception.Message);
                committed = DailyV1DecisionRecordOperation.TryCommitSelectedActionSlotWithExistingIdentity(
                    graph, actor, payload, prepareProjection, slotAdmission);
            }
        }

        if (!committed)
        {
            if (requiresActionProjection)
                boundary.CancelPreparedSelectedActionProjection(actor, payload);
            return false;
        }

        if (requiresActionProjection
            && !boundary.TryPublishSelectedActionProjection(actor, payload))
        {
            boundary.CancelPreparedSelectedActionProjection(actor, payload);
            throw new InvalidOperationException(
                "The selected action owner slot committed but its exact host projection could not be published.");
        }
        NotifySelectedActionSlotP12MutationCommitted(graph, actor, boundary);
        return true;
    }

    /// <summary>
    /// Clears the exact selected Core action slot before its Unity projection.
    /// The host admits and publishes the compatibility object; it does not own
    /// the action-slot mutation or revision.
    /// </summary>
    internal static bool TryClearCurrentAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedActorActionExecutionBoundary boundary)
    {
        DailyV1NpcActionSlotOwner slot = actor?.ActionSlot;
        if (graph == null || actor == null || boundary == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || slot == null)
            return false;

        if (slot.CurrentAction == null) return slot.CurrentPayload == null;
        bool requiresActionProjection = boundary.RequiresActionProjection;
        if (slot.CurrentPayload == null
            || (requiresActionProjection
                && !boundary.TryPrepareCurrentActionClearProjection(actor)))
            return false;

        if (!graph.TryCommitCurrentActionSlot(actor, null, null,
                () => (!requiresActionProjection
                        || boundary.CanCommitCurrentActionClear(actor))
                    && CanCommitSelectedActionSlotP12Mutation(graph, actor, boundary)))
        {
            if (requiresActionProjection)
                boundary.CancelPreparedCurrentActionClearProjection(actor);
            return false;
        }

        if (requiresActionProjection
            && !boundary.TryPublishCurrentActionClearProjection(actor))
            throw new InvalidOperationException(
                "A committed selected Core action clear could not publish its host projection.");
        NotifySelectedActionSlotP12MutationCommitted(graph, actor, boundary);
        return true;
    }

    private static bool CanCommitSelectedActionSlotP12Mutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedActorActionExecutionBoundary boundary)
    {
        return !(boundary is IDailyV1SelectedActionSlotP12MutationBoundary p12Boundary)
            || !p12Boundary.OwnsSelectedActionSlotP12Mutation
            || p12Boundary.CanCommitSelectedActionSlotP12Mutation(graph, actor);
    }

    private static void NotifySelectedActionSlotP12MutationCommitted(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedActorActionExecutionBoundary boundary)
    {
        if (!(boundary is IDailyV1SelectedActionSlotP12MutationBoundary p12Boundary)
            || !p12Boundary.OwnsSelectedActionSlotP12Mutation)
            return;

        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
            + actor.RuntimeId;
        long revision = actor.ActionSlot?.Revision ?? -1L;
        if (!p12Boundary.NotifySelectedActionSlotP12MutationCommitted(
                graph, actor, sectionId, revision))
            throw new InvalidOperationException(
                "A committed selected Core action-slot mutation could not publish its exact P12 owner-section invalidation.");
    }

    internal static void ProcessScheduledRequestAction(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        ScheduledDirective directive,
        IAuthoritativeRandomSource randomSource,
        long absoluteDay,
        IDailyV1SelectedActorActionExecutionBoundary boundary,
        DailyV1ActorTurnCoreRoots coreRoots = null)
    {
        const string incompatibleReason =
            "Actor is not in a compatible state for the requested action.";
        if (coreRoots != null)
        {
            if (!coreRoots.IsBoundToGraph(graph))
                throw new InvalidOperationException(
                    "The scheduled selected action received Core decision roots from another owner graph.");
            randomSource = coreRoots.RandomSource;
        }
        if (graph == null || actor == null || directive == null
            || randomSource == null || boundary == null || absoluteDay < 0L
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !string.Equals(directive.ActorRuntimeId, actor.RuntimeId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The scheduled Daily-v1 action did not target its exact admitted actor owner.");

        if (!TryClearCurrentAction(graph, actor, boundary))
            throw new InvalidOperationException(
                "The scheduled Daily-v1 action could not clear its exact Core action slot.");
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
        if (!TryGetActionProjectionBoundary(actor, boundary,
                out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
            || !DailyV1CrimeActionPlanning.TryPrepareSelectedEscapePrison(
                graph, actor, requested)
            || requested.PreparedPayload == null)
        {
            directive.MarkSkipped(absoluteDay, incompatibleReason);
            return;
        }

        if (!TryCommitSelectedActionSlot(
                graph, actor, requested, projectionBoundary,
                NpcDecisionOrigin.ScheduledDirective, absoluteDay, boundary, coreRoots))
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

        if (!TryClearCurrentAction(graph, actor, boundary))
            throw new InvalidOperationException(
                "The scheduled Daily-v1 outcome could not clear its exact Core action slot.");
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

        bool requiresActionProjection = boundary.RequiresActionProjection;
        if (!DailyV1NpcActionPayloadOwner.TryCreate(
                graph.Definitions, action, null, null, null, 0,
                0f, 1f, NpcTravelReason.None, 0f, null, null, null, null,
                out DailyV1NpcActionPayloadOwner forcedPayload)
            || (requiresActionProjection
                && !boundary.TryPrepareForcedOutcomeActionProjection(
                    actor, action, forcedPayload)))
        {
            MarkForcedEscapeSkipped(boundary, directive, absoluteDay,
                "Admitted Escape status effects are unavailable.");
            return;
        }

        if (!graph.TryCommitCurrentActionSlot(actor, action, forcedPayload,
                () => (!requiresActionProjection
                        || boundary.CanCommitSelectedActionSlot(actor, forcedPayload))
                    && CanCommitSelectedActionSlotP12Mutation(graph, actor, boundary)))
        {
            if (requiresActionProjection)
                boundary.CancelPreparedSelectedActionProjection(actor, forcedPayload);
            MarkForcedEscapeSkipped(boundary, directive, absoluteDay,
                "Admitted Escape status effects are unavailable.");
            return;
        }
        if (requiresActionProjection
            && !boundary.TryPublishSelectedActionProjection(actor, forcedPayload))
            throw new InvalidOperationException(
                "A committed forced Escape action could not publish its host projection.");
        NotifySelectedActionSlotP12MutationCommitted(graph, actor, boundary);

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

        if (!DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                graph.Definitions, action)
            || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                graph.Definitions, action, out _, out _, out _, out _))
        {
            resultType = NpcActionResultType.Failure;
            return true;
        }

        // Keep the established host logging callback order. For a targetless
        // Normal action the Unity adapter's implementation is a no-op when
        // its optional action projection is absent.
        boundary.LogChosenActionTarget(actor);

        DailyV1NpcActionPayloadOwner payload = actor.ActionSlot.CurrentPayload;
        bool coreOwnedNormalAction = IsProjectionIndependentNormalAction(
            graph, actor, action);
        if (!coreOwnedNormalAction
            && !string.IsNullOrWhiteSpace(payload?.TargetNpcRuntimeId)
            && graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                out DailyV1NpcRuntimeOwners selectedTarget))
            coreOwnedNormalAction = IsProjectionIndependentNormalActionForExactTarget(
                graph, actor, action, selectedTarget);

        IDailyV1SelectedActionDispatchBoundary dispatchBoundary = null;
        if (!coreOwnedNormalAction)
        {
            if (!boundary.TryGetDispatchBoundary(actor, out dispatchBoundary)
                || dispatchBoundary == null)
                throw new InvalidOperationException(
                    "The exact selected Daily-v1 action could not execute through its Core owner operation.");
        }

        if (!DailyV1SelectedActionDispatchStage.TryDispatch(
                graph, actor, randomSource, absoluteDay, dispatchBoundary,
                out DailyV1SelectedActionDispatchResult dispatch)
            || dispatch == null)
            throw new InvalidOperationException(
                "The exact selected Daily-v1 action could not execute through its Core owner operation.");

        if (!TryResolveDispatchOutcome(
                dispatch, out actionSucceeded, out NpcActionResultType coreResultType))
            throw new InvalidOperationException(
                "Core produced an invalid selected-action dispatch outcome.");
        if (!boundary.TryCompleteDispatch(actor, dispatch, out resultMessage))
            throw new InvalidOperationException(
                "The selected Daily-v1 action projection could not be completed by its host adapter.");
        resultType = coreResultType;

        if (!actionSucceeded) return true;

        DailyV1NpcRuntimeOwners target = null;
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
