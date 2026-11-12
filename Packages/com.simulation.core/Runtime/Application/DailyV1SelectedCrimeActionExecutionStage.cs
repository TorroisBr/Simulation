using System;

/// <summary>
/// Core-owned selected Hide and EscapePrison transitions. Unity supplies only
/// the existing P12 operation scope, identity-checked projections and output
/// publication; the selected Justice and NPC owners remain authoritative.
/// </summary>
internal static class DailyV1SelectedCrimeActionExecutionStage
{
    internal interface ISelectedCrimeActionProjectionBoundary
        : DailyV1SelectedActionExecutionStage.ISelectedNpcOwnerMutationBoundary
    {
        bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence);
        void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence);
        void PublishHidden(DailyV1NpcRuntimeOwners actor);
        void PublishFailedEscape(DailyV1NpcRuntimeOwners actor, int sentencePenalty);
        void PublishEscaped(
            DailyV1NpcRuntimeOwners actor,
            DailyV1CityRuntimeOwners eventCity,
            bool hadActiveSentence,
            float bountyAfterPenalty,
            string originDecisionId);
    }

    internal interface ISelectedCrimeActionBoundary
        : ISelectedCrimeActionProjectionBoundary
    {
        DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }
        bool ExecuteWithinOperation(Func<bool> operation);
    }

    internal static class DailyV1CoreSelectedFleeCityActionBoundary
    {
        internal static bool TryCreate(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition expectedAction,
            DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionProjectionBoundary projection,
            IDailyV1SelectedCrimeJusticeDailyMutationHostPort hostBindingPort,
            out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
        {
            boundary = null;
            if (executionState == null || graph == null || actor == null
                || expectedAction == null || projection == null || hostBindingPort == null
                || !executionState.HasAdmittedSelectedDailyExecution
                || executionState.AdmissionContext == null
                || !executionState.AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(actor)
                || !ReferenceEquals(actor.Definitions, graph.Definitions)
                || !ReferenceEquals(actor.ActionSlot?.CurrentAction, expectedAction)
                || !ReferenceEquals(actor.ActionSlot?.CurrentPayload?.Action, expectedAction)
                || expectedAction.Values.actionType?.Value
                    != DailyV1NpcDefinitionRules.FleeCityActionTypeValue
                || graph.JusticeRecordsOwner == null)
                return false;

            DailyV1NpcActionPayloadOwner expectedPayload = actor.ActionSlot.CurrentPayload;
            if (expectedPayload == null
                || string.IsNullOrWhiteSpace(expectedPayload.TargetCityRuntimeId)
                || expectedPayload.TargetNpcRuntimeId != null
                || expectedPayload.TargetItem != null
                || expectedPayload.Amount != 0)
                return false;

            if (!hostBindingPort.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostCandidate)
                || hostCandidate == null || !hostCandidate.AdmissionEnabled
                || hostCandidate.JusticeOwnerIdentity == null
                || !ReferenceEquals(hostCandidate.JusticeRecordsOwner, graph.JusticeRecordsOwner)
                || !executionState.TryBindSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, hostCandidate)
                || !executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding)
                || !hostBinding.HasSameSessionBinding(hostCandidate)
                || !executionState.TryBindSelectedP12CrimeJusticeMutationProtocol(
                    hostBinding.Protocol, graph,
                    out DailyV1P12CrimeJusticeMutationCoordinator coordinator)
                || coordinator == null)
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                return false;
            }

            boundary = new CoreFleeCityBoundary(
                executionState, graph, actor, expectedAction, expectedPayload,
                hostBinding, coordinator, projection, hostBindingPort);
            return true;
        }

        private sealed class CoreFleeCityBoundary
            : DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary
        {
            private readonly DailyV1ExecutionBoundaryState executionState;
            private readonly DailyV1SelectedRuntimeOwnerGraph graph;
            private readonly DailyV1NpcRuntimeOwners actor;
            private readonly DailyV1ActionDefinition expectedAction;
            private readonly DailyV1NpcActionPayloadOwner expectedPayload;
            private readonly DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding;
            private readonly DailyV1P12CrimeJusticeMutationCoordinator coordinator;
            private readonly DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionProjectionBoundary projection;
            private readonly IDailyV1SelectedCrimeJusticeDailyMutationHostPort hostBindingPort;

            internal CoreFleeCityBoundary(
                DailyV1ExecutionBoundaryState executionState,
                DailyV1SelectedRuntimeOwnerGraph graph,
                DailyV1NpcRuntimeOwners actor,
                DailyV1ActionDefinition expectedAction,
                DailyV1NpcActionPayloadOwner expectedPayload,
                DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding,
                DailyV1P12CrimeJusticeMutationCoordinator coordinator,
                DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionProjectionBoundary projection,
                IDailyV1SelectedCrimeJusticeDailyMutationHostPort hostBindingPort)
            {
                this.executionState = executionState;
                this.graph = graph;
                this.actor = actor;
                this.expectedAction = expectedAction;
                this.expectedPayload = expectedPayload;
                this.hostBinding = hostBinding;
                this.coordinator = coordinator;
                this.projection = projection;
                this.hostBindingPort = hostBindingPort;
            }

            public bool ExecuteWithinOperation(Func<bool> operation)
            {
                if (operation == null) throw new ArgumentNullException(nameof(operation));
                if (!IsCurrentSession())
                {
                    coordinator.FaultSelected(executionState);
                    throw new InvalidOperationException(
                        "The selected FleeCity action lost its exact admitted Core session.");
                }

                if (!coordinator.TryBeginSelectedCrimeActionMutation(
                        graph, actor, hostBinding.JusticeOwnerIdentity,
                        includeCrimeReceiptSection: hostBinding.CrimeOwnerIdentity != null,
                        out IDisposable scope)
                    || scope == null)
                    throw new InvalidOperationException(
                        "P12 Core could not admit the selected FleeCity actor, Justice, and receipt sections.");

                using (scope) return operation();
            }

            public bool CanPrepareTravelPlanProjection(
                DailyV1NpcRuntimeOwners candidateActor,
                DailyV1CityRuntimeOwners destination,
                float travelCost,
                string originDecisionId) =>
                IsExactActor(candidateActor)
                && graph.ContainsExactCityOwner(destination)
                && ReferenceEquals(destination.Definitions, graph.Definitions)
                && IsCurrentSession()
                && projection.CanPrepareTravelPlanProjection(
                    candidateActor, destination, travelCost, originDecisionId);

            public bool CanAdmitTravelPlanMutation() =>
                IsCurrentSession() && projection.CanAdmitTravelPlanMutation();

            public void PublishCommittedTravelPlan(
                DailyV1NpcRuntimeOwners candidateActor,
                DailyV1CityRuntimeOwners destination,
                float travelCost,
                string originDecisionId)
            {
                if (!IsExactActor(candidateActor)
                    || !graph.ContainsExactCityOwner(destination)
                    || !ReferenceEquals(destination.Definitions, graph.Definitions)
                    || !IsCurrentSession())
                    throw new InvalidOperationException(
                        "The selected FleeCity TravelPlan projection left its exact Core session.");
                projection.PublishCommittedTravelPlan(
                    candidateActor, destination, travelCost, originDecisionId);
            }

            private bool IsExactActor(DailyV1NpcRuntimeOwners candidate) =>
                ReferenceEquals(candidate, actor)
                && graph.ContainsExactNpcOwner(candidate)
                && ReferenceEquals(candidate.Definitions, graph.Definitions);

            private bool IsCurrentSession() =>
                executionState.HasAdmittedSelectedDailyExecution
                && executionState.AdmissionContext != null
                && executionState.AdmissionContext.IsOwnedByCurrentThread()
                && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
                && graph.ContainsExactNpcOwner(actor)
                && ReferenceEquals(actor.Definitions, graph.Definitions)
                && ReferenceEquals(actor.ActionSlot?.CurrentAction, expectedAction)
                && ReferenceEquals(actor.ActionSlot?.CurrentPayload, expectedPayload)
                && ReferenceEquals(expectedPayload?.Action, expectedAction)
                && hostBindingPort.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding currentCandidate)
                && currentCandidate != null
                && executionState.TryBindSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, currentCandidate)
                && executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding currentBinding)
                && ReferenceEquals(currentBinding, hostBinding)
                && currentBinding.HasSameSessionBinding(currentCandidate)
                && executionState.TryBindSelectedP12CrimeJusticeMutationProtocol(
                    hostBinding.Protocol, graph,
                    out DailyV1P12CrimeJusticeMutationCoordinator currentCoordinator)
                && ReferenceEquals(currentCoordinator, coordinator);
        }
    }

    /// <summary>
    /// Binds selected Hide/Escape to the exact admitted Core session. Core
    /// derives and opens the P12 actor/Justice/receipt scope; Unity contributes
    /// only exact projection checks and publication callbacks.
    /// </summary>
    internal static class DailyV1CoreSelectedCrimeActionBoundary
    {
        internal static bool TryCreate(
            DailyV1ExecutionBoundaryState executionState,
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition expectedAction,
            ISelectedCrimeActionProjectionBoundary projection,
            IDailyV1SelectedCrimeJusticeDailyMutationHostPort hostBindingPort,
            out ISelectedCrimeActionBoundary boundary)
        {
            boundary = null;
            if (executionState == null || graph == null || actor == null
                || expectedAction == null || projection == null || hostBindingPort == null
                || !executionState.HasAdmittedSelectedDailyExecution
                || executionState.AdmissionContext == null
                || !executionState.AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(actor)
                || !ReferenceEquals(actor.Definitions, graph.Definitions)
                || !ReferenceEquals(actor.ActionSlot?.CurrentAction, expectedAction)
                || !ReferenceEquals(actor.ActionSlot?.CurrentPayload?.Action, expectedAction)
                || (expectedAction.Values.actionType?.Value
                    != DailyV1NpcDefinitionRules.HideActionTypeValue
                    && expectedAction.Values.actionType?.Value
                        != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)
                || graph.JusticeRecordsOwner == null)
                return false;

            if (!hostBindingPort.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostCandidate)
                || hostCandidate == null || !hostCandidate.AdmissionEnabled
                || !executionState.TryBindSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, hostCandidate)
                || !executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding)
                || !hostBinding.HasSameSessionBinding(hostCandidate)
                || hostBinding.JusticeOwnerIdentity == null
                || !executionState.TryBindSelectedP12CrimeJusticeMutationProtocol(
                    hostBinding.Protocol, graph,
                    out DailyV1P12CrimeJusticeMutationCoordinator coordinator)
                || coordinator == null)
            {
                executionState.MarkSelectedRuntimeCensusProtocolFaulted();
                return false;
            }

            boundary = new CoreBoundary(
                executionState, graph, actor, expectedAction,
                hostBinding, coordinator, projection, hostBindingPort);
            return true;
        }

        private sealed class CoreBoundary : ISelectedCrimeActionBoundary
        {
            private readonly DailyV1ExecutionBoundaryState executionState;
            private readonly DailyV1SelectedRuntimeOwnerGraph graph;
            private readonly DailyV1NpcRuntimeOwners actor;
            private readonly DailyV1ActionDefinition expectedAction;
            private readonly DailyV1NpcActionPayloadOwner expectedPayload;
            private readonly DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding;
            private readonly DailyV1P12CrimeJusticeMutationCoordinator coordinator;
            private readonly ISelectedCrimeActionProjectionBoundary projection;
            private readonly IDailyV1SelectedCrimeJusticeDailyMutationHostPort hostBindingPort;

            internal CoreBoundary(
                DailyV1ExecutionBoundaryState executionState,
                DailyV1SelectedRuntimeOwnerGraph graph,
                DailyV1NpcRuntimeOwners actor,
                DailyV1ActionDefinition expectedAction,
                DailyV1SelectedCrimeJusticeDailyMutationHostBinding hostBinding,
                DailyV1P12CrimeJusticeMutationCoordinator coordinator,
                ISelectedCrimeActionProjectionBoundary projection,
                IDailyV1SelectedCrimeJusticeDailyMutationHostPort hostBindingPort)
            {
                this.executionState = executionState;
                this.graph = graph;
                this.actor = actor;
                this.expectedAction = expectedAction;
                expectedPayload = actor.ActionSlot.CurrentPayload;
                this.hostBinding = hostBinding;
                this.coordinator = coordinator;
                this.projection = projection;
                this.hostBindingPort = hostBindingPort;
            }

            public DailyV1JusticeRecordsOwner JusticeRecordsOwner => graph.JusticeRecordsOwner;

            public bool ExecuteWithinOperation(Func<bool> operation)
            {
                if (operation == null) throw new ArgumentNullException(nameof(operation));
                if (!IsCurrentSession())
                {
                    coordinator.FaultSelected(executionState);
                    throw new InvalidOperationException(
                        "The selected Crime/Justice action lost its exact admitted Core session.");
                }

                if (!coordinator.TryBeginSelectedCrimeActionMutation(
                        graph, actor, hostBinding.JusticeOwnerIdentity,
                        includeCrimeReceiptSection: hostBinding.CrimeOwnerIdentity != null,
                        out IDisposable scope)
                    || scope == null)
                    throw new InvalidOperationException(
                        "P12 Core could not admit the selected Crime/Justice action owners and receipt sections.");

                using (scope)
                    return operation();
            }

            public bool CanProjectStatusChanges(
                DailyV1NpcRuntimeOwners owner,
                System.Collections.Generic.IReadOnlyList<DailyV1StatusDefinition> statusToAdd) =>
                IsExactActor(owner) && IsCurrentSession()
                && projection.CanProjectStatusChanges(owner, statusToAdd);

            public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner)
            {
                if (!IsExactActor(owner) || !IsCurrentSession())
                    throw new InvalidOperationException(
                        "The selected Crime owner is outside its exact admitted Core operation.");
                projection.EnsureMutationAllowed(owner);
            }

            public void NotifyStatusMutationCommitted(
                DailyV1NpcRuntimeOwners owner,
                DailyV1StatusDefinition status,
                bool added)
            {
                if (!IsExactActor(owner) || !IsCurrentSession())
                    throw new InvalidOperationException(
                        "The selected Crime status owner changed outside its exact admitted Core operation.");
                projection.NotifyStatusMutationCommitted(owner, status, added);
            }

            public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner)
            {
                if (!IsExactActor(owner) || !IsCurrentSession())
                    throw new InvalidOperationException(
                        "The selected Crime hidden-state owner changed outside its exact admitted Core operation.");
                projection.NotifyHiddenMutationCommitted(owner);
            }

            public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
                IsCurrentSession() && projection.CanRemoveSentenceProjection(sentence);

            public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence)
            {
                if (!IsCurrentSession())
                    throw new InvalidOperationException(
                        "The selected Crime sentence projection changed outside its exact Core operation.");
                projection.CommitSentenceProjectionRemoval(sentence);
            }

            public void PublishHidden(DailyV1NpcRuntimeOwners owner)
            {
                EnsureCurrentActor(owner);
                projection.PublishHidden(owner);
            }

            public void PublishFailedEscape(DailyV1NpcRuntimeOwners owner, int sentencePenalty)
            {
                EnsureCurrentActor(owner);
                projection.PublishFailedEscape(owner, sentencePenalty);
            }

            public void PublishEscaped(
                DailyV1NpcRuntimeOwners owner,
                DailyV1CityRuntimeOwners eventCity,
                bool hadActiveSentence,
                float bountyAfterPenalty,
                string originDecisionId)
            {
                EnsureCurrentActor(owner);
                projection.PublishEscaped(
                    owner, eventCity, hadActiveSentence, bountyAfterPenalty, originDecisionId);
            }

            private bool IsExactActor(DailyV1NpcRuntimeOwners candidate) =>
                ReferenceEquals(candidate, actor)
                && graph.ContainsExactNpcOwner(candidate)
                && ReferenceEquals(candidate.Definitions, graph.Definitions);

            private void EnsureCurrentActor(DailyV1NpcRuntimeOwners candidate)
            {
                if (!IsExactActor(candidate) || !IsCurrentSession())
                    throw new InvalidOperationException(
                        "The selected Crime action lost its exact owner before publication.");
            }

            private bool IsCurrentSession() =>
                executionState.HasAdmittedSelectedDailyExecution
                && executionState.AdmissionContext != null
                && executionState.AdmissionContext.IsOwnedByCurrentThread()
                && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
                && graph.ContainsExactNpcOwner(actor)
                && ReferenceEquals(actor.Definitions, graph.Definitions)
                && ReferenceEquals(actor.ActionSlot?.CurrentAction, expectedAction)
                && ReferenceEquals(actor.ActionSlot?.CurrentPayload, expectedPayload)
                && ReferenceEquals(expectedPayload?.Action, expectedAction)
                && hostBindingPort.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding currentHostCandidate)
                && currentHostCandidate != null
                && executionState.TryBindSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, currentHostCandidate)
                && executionState.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                    graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding currentBinding)
                && ReferenceEquals(currentBinding, hostBinding)
                && currentBinding.HasSameSessionBinding(currentHostCandidate)
                && executionState.TryBindSelectedP12CrimeJusticeMutationProtocol(
                    hostBinding.Protocol, graph,
                    out DailyV1P12CrimeJusticeMutationCoordinator currentCoordinator)
                && ReferenceEquals(currentCoordinator, coordinator);
        }
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
        if (!DailyV1SelectedActionExecutionStage.TryHideSelectedNpcForDays(
                actor, hiddenDays, boundary)) return false;

        if (!TryResolveOptionalStatus(graph.Definitions,
                graph.Definitions.Configuration?.hiddenStatus,
                out DailyV1StatusDefinition hiddenStatus)
            || (hiddenStatus != null && !DailyV1SelectedActionExecutionStage
                .TryApplySelectedStatusChanges(actor,
                    Array.Empty<DailyV1StatusDefinition>(), new[] { hiddenStatus }, boundary)))
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
            if (!boundary.CanRemoveSentenceProjection(sentence)) return false;
            if (!justice.TryRemoveSentence(sentence)) return false;
            boundary.CommitSentenceProjectionRemoval(sentence);
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

        if (!DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, new[] { arrestedStatus }, Array.Empty<DailyV1StatusDefinition>(), boundary)) return false;
        if (freeStatus != null && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, Array.Empty<DailyV1StatusDefinition>(), new[] { freeStatus }, boundary)) return false;
        if (clearHidden)
        {
            if (!DailyV1SelectedActionExecutionStage.TryClearSelectedNpcHiddenState(actor, boundary)) return false;
            if (hiddenStatus != null && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                    actor, new[] { hiddenStatus }, Array.Empty<DailyV1StatusDefinition>(), boundary))
                return false;
        }

        bool hasActiveWarrant = graph.JusticeRecordsOwner.FindActiveWantedRecords(actor.RuntimeId).Count > 0;
        if (wantedStatus != null
            && !DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor,
                hasActiveWarrant ? Array.Empty<DailyV1StatusDefinition>() : new[] { wantedStatus },
                hasActiveWarrant ? new[] { wantedStatus } : Array.Empty<DailyV1StatusDefinition>(),
                boundary))
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
