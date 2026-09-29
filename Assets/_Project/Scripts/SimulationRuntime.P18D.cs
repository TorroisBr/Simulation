using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Explicit identity contract for the selected P18-D intraday runtime profile.</summary>
public sealed class P18DIntradayProfile
{
    public string WorldId { get; }
    public string ProfileId { get; }
    public string ConfigurationIdentity { get; }
    public string ContentIdentity { get; }

    public P18DIntradayProfile(string worldId, string profileId,
        string configurationIdentity, string contentIdentity)
    {
        if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World identity is required.", nameof(worldId));
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile identity is required.", nameof(profileId));
        if (string.IsNullOrWhiteSpace(configurationIdentity)) throw new ArgumentException("Configuration identity is required.", nameof(configurationIdentity));
        if (string.IsNullOrWhiteSpace(contentIdentity)) throw new ArgumentException("Content identity is required.", nameof(contentIdentity));
        WorldId = worldId;
        ProfileId = profileId;
        ConfigurationIdentity = configurationIdentity;
        ContentIdentity = contentIdentity;
    }
}

public sealed partial class SimulationRuntime
{
    private P18DIntradayProfile p18dIntradayProfile;
    private SimulationTimeline p18dTimeline;
    private ActivityLifecycleStore p18dActivityLifecycleStore;
    private ActorDecisionRequestState p18dActorDecisionRequestState;
    private ActorChoiceTemporalDecisionBridge p18dActorChoiceDecisionBridge;
    private long p18dNextTimelineInputSequence = 1L;
    private long p18dLastActivityTransitionSequence;
    private TimelineFailure p18dLastTimelineFailure;
    private string p18dLastBridgeFailure;
    private bool p18dPostAdvanceHandoffPending;
    private readonly Dictionary<string, P18DActorChoiceExecution> p18dActorChoiceExecutions =
        new Dictionary<string, P18DActorChoiceExecution>(StringComparer.Ordinal);

    public bool UsesP18DIntradayProfile => p18dTimeline != null;
    public SimulationTimeline P18DTimeline => p18dTimeline;
    public ActivityLifecycleStore P18DActivityLifecycleStore => p18dActivityLifecycleStore;
    public IReadOnlyList<ActorDecisionRequestReceipt> P18DActorDecisionReceipts =>
        p18dActorDecisionRequestState != null
            ? p18dActorDecisionRequestState.Snapshot()
            : Array.Empty<ActorDecisionRequestReceipt>();
    public TimelineFailure LastP18DTimelineFailure => p18dLastTimelineFailure;
    public string LastP18DBridgeFailure => p18dLastBridgeFailure;

    private void InitializeP18DIntradayProfile(P18DIntradayProfile profile)
    {
        if (profile == null) return;

        p18dIntradayProfile = profile;
        ValidateP18DIntradayComposition();

        LogicalTick initialInstant = new LogicalTick(checked(
            simulationTime.AbsoluteDay * LogicalTick.TicksPerDay));
        p18dActivityLifecycleStore = new ActivityLifecycleStore(profile.WorldId);
        ActorChoiceTemporalInputOwner inputOwner = new ActorChoiceTemporalInputOwner(
            actorChoiceStore, profile.ProfileId);
        List<IP18DDailyBoundaryStepProvider> providers = BuildP18DDailyBoundaryProviders();
        if (providers.Count == 0)
            throw new ArgumentException("The selected intraday profile has no supported daily boundary owners.", nameof(profile));

        P18DDailyBoundaryOwner boundaryOwner = new P18DDailyBoundaryOwner(
            profile.WorldId,
            profile.ProfileId,
            profile.ConfigurationIdentity,
            profile.ContentIdentity,
            providers.AsReadOnly());
        p18dTimeline = new SimulationTimeline(calendar, initialInstant,
            p18dActivityLifecycleStore, inputOwner, boundaryOwner,
            profile.WorldId, profile.ProfileId);
        p18dActivityLifecycleStore.BindTimeline(p18dTimeline);
        if (!simulationTime.TryBindAbsoluteDayProjection(
                () => p18dTimeline.CurrentInstant.AbsoluteDay))
            throw new ArgumentException("SimulationTime cannot be projected from this intraday timeline.", nameof(profile));

        p18dActorDecisionRequestState = new ActorDecisionRequestState();
        p18dActorChoiceDecisionBridge = new ActorChoiceTemporalDecisionBridge(
            actorChoiceStore, p18dActorDecisionRequestState, profile.ProfileId);
        p18dLastTimelineFailure = TimelineFailure.None;
    }

    private List<IP18DDailyBoundaryStepProvider> BuildP18DDailyBoundaryProviders()
    {
        List<IP18DDailyBoundaryStepProvider> providers = new List<IP18DDailyBoundaryStepProvider>();
        if (placeContentStore != null) providers.Add(new PlaceContentDailyBoundaryStepProvider(placeContentStore));
        if (logger != null) providers.Add(new LoggerDailyBoundaryStepProvider(logger));
        providers.Add(new DailyDemographyBoundaryStepProvider(
            this, naturalMortalitySamples, aggregateDemographyProvider));
        if (justiceSystem != null) providers.Add(new JusticeBeginDayDailyBoundaryStepProvider(justiceSystem));
        if (crimeSystem != null)
            providers.Add(new CrimeHiddenStatusesDailyBoundaryStepProvider(crimeSystem, npcRuntimes));
        if (justiceSystem != null)
            providers.Add(new JusticeResolutionDailyBoundaryStepProvider(justiceSystem, npcRuntimes));
        if (configuration.MerchantTrade.Enabled && merchantSystem != null)
            providers.Add(new MerchantPlanUrgencyDailyBoundaryStepProvider(merchantSystem, npcRuntimeSnapshot));
        if (configuration.Economy.Enabled)
            providers.Add(new CityEconomyDailyBoundaryStepProvider(() => cities));
        providers.Add(new NpcLocalKnowledgeDailyBoundaryStepProvider(
            () => npcRuntimeSnapshot, configuration.MerchantTrade.Enabled));
        if (commercialKnowledgeSharingSystem != null)
            providers.Add(new CommercialKnowledgeSharingDailyBoundaryStepProvider(
                () => npcRuntimeSnapshot,
                runtimeId => npcRegistryById.TryGetValue(runtimeId, out NpcRuntime actor) ? actor : null,
                commercialKnowledgeSharingSystem));
        providers.Add(new MerchantTradeStateDailyBoundaryStepProvider(
            merchantSystem, () => npcRuntimeSnapshot));
        return providers;
    }

    private void ValidateP18DIntradayComposition()
    {
        if (merchantSystem == null || !configuration.MerchantTrade.Enabled
            || npcDecisionSystem == null || decisionRecorder == null)
            throw new ArgumentException(
                "P18-D Intraday SellGoods requires the existing MerchantSystem, enabled merchant configuration, NpcDecisionSystem, and decision recorder.");
        if (travelSystem != null || travelPartySystem != null)
            throw new ArgumentException("P18-D intraday composition requires travel owners to be absent until their temporal consumer is integrated.");
        if (adventureExpeditionAutonomySystem != null)
            throw new ArgumentException("P18-D intraday composition excludes autonomous expedition execution.");
        if (scheduledDirectiveSystem != null && scheduledDirectiveSystem.HasPendingDirectives)
            throw new ArgumentException("P18-D intraday composition cannot advance while a scheduled directive is pending.");
        if (expeditionSystem != null && expeditionSystem.Store.ActiveExpeditions.Count > 0)
            throw new ArgumentException("P18-D intraday composition cannot advance with active expeditions or reserved participants.");
        if (personSpatialPositionStore.Positions.Any(position => position != null && position.IsInTransit))
            throw new ArgumentException("P18-D intraday composition cannot advance with a person in transit.");
        foreach (NpcRuntime actor in npcRuntimes)
        {
            if (actor == null) continue;
            if (actor.IsTraveling || expeditionSystem != null
                && expeditionSystem.IsNpcOnActiveExpedition(actor.RuntimeId))
                throw new ArgumentException("P18-D intraday composition cannot advance with an actor traveling or committed to an expedition.");
        }
        foreach (ActorChoiceInput input in actorChoiceStore.Inputs)
        {
            bool active = input.Status == ActorChoiceInputStatus.Pending
                || input.Status == ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt;
            if (active && (p18dTimeline == null || input.TemporalCapture == null
                || !string.Equals(input.TemporalCapture.ProfileId,
                    p18dIntradayProfile.ProfileId, StringComparison.Ordinal)))
                throw new ArgumentException("P18-D does not migrate legacy or foreign-profile pending ActorChoice inputs.");
        }
    }

    public bool TryCaptureIntradayActorChoice(string worldCommandId, PersonId actor,
        string actionDefinitionId, LogicalTick targetInstant, out TimelineFailure failure,
        WorldCommandOrigin origin = WorldCommandOrigin.LocalPlayer,
        WorldCommandAuthorityMode authority = WorldCommandAuthorityMode.Request)
    {
        failure = TimelineFailure.None;
        if (p18dTimeline == null || p18dIntradayProfile == null)
        {
            failure = TimelineFailure.UnsupportedProfile;
            return false;
        }
        if (advanceLeaseHeld || p18dTimeline.IsAdvanceInProgress)
        {
            failure = TimelineFailure.ReentrantAdvance;
            return false;
        }
        if (p18dNextTimelineInputSequence == long.MaxValue)
        {
            failure = TimelineFailure.Overflow;
            return false;
        }

        ActorChoiceTemporalCommand command;
        try
        {
            command = new ActorChoiceTemporalCommand(worldCommandId, actor,
                actionDefinitionId, origin, authority);
        }
        catch (ArgumentException)
        {
            failure = TimelineFailure.InvalidInput;
            return false;
        }

        TimelineInputReference accepted;
        try
        {
            string inputId = SpatialStableKey.Encode(
                "p18d-actor-choice-input", p18dIntradayProfile.ProfileId, worldCommandId);
            accepted = new TimelineInputReference(p18dNextTimelineInputSequence, inputId,
                ActorChoiceTemporalInputOwner.CommandKind, command.Encode(), targetInstant);
        }
        catch (ArgumentException)
        {
            failure = TimelineFailure.InvalidInput;
            return false;
        }

        if (!p18dTimeline.TryAcceptInput(accepted, out failure)) return false;
        p18dNextTimelineInputSequence++;
        return true;
    }

    public bool TryAdvanceIntradayTo(LogicalTick target, out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        if (p18dTimeline == null)
        {
            failure = TimelineFailure.UnsupportedProfile;
            return false;
        }
        if (!TryAcquireAdvanceLease(out AdvanceLease lease))
        {
            failure = TimelineFailure.ReentrantAdvance;
            return false;
        }

        using (lease)
        {
            bool advanced = TryAdvanceP18DIntradayToCore(target, out SimulationRuntimeAdvanceFailure advanceFailure);
            failure = p18dLastTimelineFailure;
            return advanced && advanceFailure == SimulationRuntimeAdvanceFailure.None;
        }
    }

    private bool TryAdvanceP18DIntradayToCore(LogicalTick target,
        out SimulationRuntimeAdvanceFailure failure)
    {
        failure = SimulationRuntimeAdvanceFailure.None;
        p18dLastTimelineFailure = TimelineFailure.None;
        if (p18dTimeline == null || p18dActorChoiceDecisionBridge == null)
        {
            p18dLastTimelineFailure = TimelineFailure.UnsupportedProfile;
            failure = SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
            return false;
        }
        if (!mutationGuard.CanMutate)
        {
            p18dLastTimelineFailure = TimelineFailure.RuntimeFaulted;
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }
        try
        {
            ValidateP18DIntradayComposition();
        }
        catch (ArgumentException)
        {
            p18dLastTimelineFailure = TimelineFailure.CompositionUnsupported;
            failure = SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
            return false;
        }
        if (p18dPostAdvanceHandoffPending)
        {
            if (!TryCompleteP18DPostAdvanceBoundary(out p18dLastTimelineFailure))
            {
                failure = MapP18DAdvanceFailure(p18dLastTimelineFailure);
                return false;
            }
            p18dPostAdvanceHandoffPending = false;
        }
        if (target.Value < p18dTimeline.CurrentInstant.Value)
        {
            p18dLastTimelineFailure = TimelineFailure.TargetBeforeNow;
            failure = SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
            return false;
        }

        if (!p18dTimeline.TrySealInputsThrough(target, out p18dLastTimelineFailure))
        {
            failure = MapP18DAdvanceFailure(p18dLastTimelineFailure);
            return false;
        }

        while (true)
        {
            if (!p18dTimeline.TryGetNextCausalInstant(target,
                    out LogicalTick next, out p18dLastTimelineFailure))
            {
                failure = MapP18DAdvanceFailure(p18dLastTimelineFailure);
                return false;
            }
            if (!p18dTimeline.TryAdvanceTo(next, out p18dLastTimelineFailure))
            {
                failure = MapP18DAdvanceFailure(p18dLastTimelineFailure);
                return false;
            }

            if (p18dTimeline.SuccessfulAdvanceAwaitingHandoff
                && !p18dTimeline.TryCompleteSuccessfulAdvanceHandoffs(out p18dLastTimelineFailure))
            {
                failure = MapP18DAdvanceFailure(p18dLastTimelineFailure);
                return false;
            }

            p18dPostAdvanceHandoffPending = true;
            if (!TryCompleteP18DPostAdvanceBoundary(out p18dLastTimelineFailure))
            {
                failure = MapP18DAdvanceFailure(p18dLastTimelineFailure);
                return false;
            }
            p18dPostAdvanceHandoffPending = false;
            if (p18dTimeline.CurrentInstant == target) break;
        }

        return true;
    }

    private static SimulationRuntimeAdvanceFailure MapP18DAdvanceFailure(TimelineFailure failure)
    {
        if (failure == TimelineFailure.RuntimeFaulted) return SimulationRuntimeAdvanceFailure.RuntimeFaulted;
        if (failure == TimelineFailure.Overflow) return SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow;
        return SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
    }

    private bool TryCompleteP18DPostAdvanceBoundary(out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        if (!TryReconcileP18DTerminalExecutions(out failure)) return false;
        IReadOnlyList<ActorChoiceTemporalTrigger> triggers = SnapshotP18DActivityTriggers(
            p18dTimeline.CurrentInstant, out long latestSequence);
        if (!p18dActorChoiceDecisionBridge.AfterSuccessfulAdvance(
                p18dTimeline.CurrentInstant, triggers, out IReadOnlyList<ActorChoiceTemporalDecisionRequest> admitted,
                out string bridgeFailure))
        {
            p18dLastBridgeFailure = bridgeFailure;
            failure = TimelineFailure.DispatchFailed;
            return false;
        }
        p18dLastBridgeFailure = null;
        p18dLastActivityTransitionSequence = latestSequence;

        foreach (ActorChoiceTemporalDecisionRequest request in admitted)
            if (!TryExecuteP18DActorChoice(request, out failure)) return false;
        return TryReconcileP18DTerminalExecutions(out failure);
    }

    private IReadOnlyList<ActorChoiceTemporalTrigger> SnapshotP18DActivityTriggers(
        LogicalTick instant, out long latestSequence)
    {
        List<ActorChoiceTemporalTrigger> triggers = new List<ActorChoiceTemporalTrigger>();
        latestSequence = p18dLastActivityTransitionSequence;
        foreach (ActivityTransitionReceipt receipt in p18dActivityLifecycleStore
            .SnapshotTransitionReceipts(p18dLastActivityTransitionSequence))
        {
            latestSequence = Math.Max(latestSequence, receipt.Sequence);
            if (receipt.Instant.Value > instant.Value) continue;
            foreach (string participantId in receipt.ParticipantIds)
            {
                foreach (ActorChoiceInput pending in actorChoiceStore.PendingInputs)
                {
                    if (pending.PersonId == null
                        || !string.Equals(pending.PersonId.Value, participantId, StringComparison.Ordinal)
                        || pending.TemporalCapture == null
                        || !string.Equals(pending.TemporalCapture.ProfileId,
                            p18dIntradayProfile.ProfileId, StringComparison.Ordinal)) continue;
                    triggers.Add(new ActorChoiceTemporalTrigger(pending.PersonId,
                        receipt.Id, receipt.Id, receipt.ActivityRevision,
                        receipt.Sequence, instant));
                    break;
                }
            }
        }
        return triggers.AsReadOnly();
    }

    private bool TryExecuteP18DActorChoice(ActorChoiceTemporalDecisionRequest admitted,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        ActorChoiceInput input = admitted?.Input;
        ActorDecisionRequest request = admitted?.Request;
        if (input == null || request == null || input.PersonId == null
            || !actorChoiceStore.TryGet(input.InputId, out ActorChoiceInput currentInput))
        {
            failure = TimelineFailure.DispatchFailed;
            return false;
        }

        string requestId = request.Id;
        string inputId = input.InputId.Value;
        if (p18dActorChoiceExecutions.TryGetValue(requestId,
                out P18DActorChoiceExecution existing))
        {
            if (existing.InputId != inputId || !existing.ActorPersonId.Equals(input.PersonId)
                || (admitted.RetryableProposalId != null
                    && existing.ProposalId != admitted.RetryableProposalId))
            {
                failure = TimelineFailure.DispatchFailed;
                return false;
            }
            return TryResumeP18DActorChoice(existing, out failure);
        }

        if (currentInput.Status != ActorChoiceInputStatus.Pending)
        {
            failure = TimelineFailure.DispatchFailed;
            return false;
        }

        string proposalId = SpatialStableKey.Encode(
            "p18d-sell-goods-proposal", p18dIntradayProfile.ProfileId,
            requestId, inputId);
        P18DActorChoiceExecution execution = new P18DActorChoiceExecution(
            requestId, inputId, input.PersonId, proposalId, request.Instant);
        p18dActorChoiceExecutions.Add(requestId, execution);

        if (!personStore.TryGet(input.PersonId, out PersonRuntime person)
            || person.IsDeadAt(CurrentDay) || !person.IsMaterialized
            || string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId)
            || !npcRegistryById.TryGetValue(person.MaterializedNpcRuntimeId,
                out NpcRuntime materializedActor))
        {
            return CompleteP18DActorChoiceRejection(execution,
                ActorChoiceFailure.ActorUnavailable, out failure);
        }
        execution.Actor = materializedActor;
        if (execution.Actor == null
            || !IsCurrentMaterializedPersonActor(execution.Actor)
            || !execution.Actor.IsAlive
            || !IsActorAtCurrentCityLocation(execution.Actor))
        {
            return CompleteP18DActorChoiceRejection(execution,
                ActorChoiceFailure.ActorUnavailable, out failure);
        }

        if (!p18dActivityLifecycleStore.IsAvailable(input.PersonId.Value, request.Instant))
        {
            string boundaryId = SpatialStableKey.Encode("p18d-decision-boundary",
                p18dIntradayProfile.ProfileId,
                request.Instant.Value.ToString(CultureInfo.InvariantCulture));
            if (!p18dActorDecisionRequestState.TryDefer(
                    SpatialStableKey.Encode("p18d-temporary-availability", requestId,
                        request.Instant.Value.ToString(CultureInfo.InvariantCulture)),
                    requestId, inputId, input.PersonId, request.Instant, boundaryId,
                    request.BoundaryRevision, request.BoundarySequence,
                    ActorDecisionDeferralReason.TemporarilyUnavailable, out _))
            {
                failure = TimelineFailure.DispatchFailed;
                return false;
            }
            execution.Stage = P18DActorChoiceStage.Deferred;
            return true;
        }

        if (execution.Actor.CurrentCity == null
            || !IsActorAtCurrentCityLocation(execution.Actor)
            || !configuration.MerchantTrade.Enabled
            || execution.Actor.MerchantTradePlan.IsActive
            || npcDecisionSystem == null)
        {
            return CompleteP18DActorChoiceRejection(execution,
                ActorChoiceFailure.ActionUnavailable, out failure);
        }

        NpcActionData definition = null;
        int matchingDefinitions = 0;
        if (configuredActions != null)
        {
            foreach (NpcActionData action in configuredActions)
            {
                if (action != null && string.Equals(action.DefinitionId,
                        input.ActionDefinitionId, StringComparison.Ordinal))
                {
                    definition = action;
                    matchingDefinitions++;
                }
            }
        }
        if (matchingDefinitions != 1 || definition == null
            || definition.actionType != NpcActionType.SellGoods
            || !IsActionEnabledForRuntime(definition))
        {
            return CompleteP18DActorChoiceRejection(execution,
                ActorChoiceFailure.ActionUnavailable, out failure);
        }

        NpcActionRuntime actionRuntime = npcDecisionSystem.CreateRequestedAction(
            execution.Actor, definition);
        if (actionRuntime == null || !ReferenceEquals(actionRuntime.Action, definition)
            || actionRuntime.Action.actionType != NpcActionType.SellGoods
            || actionRuntime.TargetCity != execution.Actor.CurrentCity
            || actionRuntime.TargetNpc != null || actionRuntime.TargetItem == null
            || actionRuntime.Amount <= 0)
        {
            return CompleteP18DActorChoiceRejection(execution,
                ActorChoiceFailure.ActionUnavailable, out failure);
        }

        execution.Action = actionRuntime;
        NpcDecisionRecord decision = decisionRecorder.RecordChosenAction(
            execution.Actor, actionRuntime, NpcDecisionOrigin.ActorChoice);
        if (decision == null || string.IsNullOrWhiteSpace(decision.DecisionId))
        {
            failure = TimelineFailure.DispatchFailed;
            return false;
        }
        execution.DecisionRecordId = decision.DecisionId;
        execution.Actor.SetCurrentActionRuntime(actionRuntime);
        execution.Fingerprint = SpatialStableKey.Encode(
            "local-market-sell/v1", inputId, requestId,
            input.PersonId.Value, p18dIntradayProfile.ProfileId,
            request.Instant.Value.ToString(CultureInfo.InvariantCulture),
            actionRuntime.Action.DefinitionId,
            actionRuntime.TargetCity.RuntimeId,
            actionRuntime.TargetItem.DefinitionId,
            actionRuntime.Amount.ToString(CultureInfo.InvariantCulture));
        execution.Stage = P18DActorChoiceStage.Prepared;
        return TryResumeP18DActorChoice(execution, out failure);
    }

    private bool TryResumeP18DActorChoice(P18DActorChoiceExecution execution,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        if (execution.Stage == P18DActorChoiceStage.Terminal
            || execution.Stage == P18DActorChoiceStage.Deferred
            || execution.Stage == P18DActorChoiceStage.Unresolved)
            return true;
        if (execution.Stage == P18DActorChoiceStage.ActionFailed)
            return CompleteP18DActorChoiceReturned(execution, out failure);

        if (execution.Stage == P18DActorChoiceStage.ProvenNoInstall)
        {
            if (!IsCurrentMaterializedPersonActor(execution.Actor)
                || !execution.Actor.IsAlive || !IsActorAtCurrentCityLocation(execution.Actor)
                || execution.Actor.MerchantTradePlan.IsActive
                || !p18dActivityLifecycleStore.IsAvailable(
                    execution.ActorPersonId.Value, p18dTimeline.CurrentInstant))
            {
                return CompleteP18DActorChoiceRejection(execution,
                    ActorChoiceFailure.ActionUnavailable, out failure);
            }
        }

        if (!execution.CorrelationPrepared)
        {
            if (!p18dActorDecisionRequestState.TryRecordRetryableProposal(
                    SpatialStableKey.Encode("p18d-proposal-prepared", execution.RequestId),
                    execution.RequestId, execution.InputId, execution.ActorPersonId,
                    execution.RequestInstant, execution.ProposalId, "prepared", out _))
            {
                failure = TimelineFailure.DispatchFailed;
                return false;
            }
            execution.CorrelationPrepared = true;
        }

        ActorChoiceTemporalBoundaryReference boundary = new ActorChoiceTemporalBoundaryReference(
            p18dIntradayProfile.ProfileId, p18dTimeline.CurrentInstant,
            execution.RequestId, 0L);
        if (!execution.DispatchStarted)
        {
            if (!actorChoiceStore.TryRecordTemporalDispatchStarted(
                    new ActorChoiceInputId(execution.InputId), boundary,
                    SpatialStableKey.Encode("p18d-dispatch-started", execution.RequestId),
                    execution.DecisionRecordId, out _))
            {
                failure = TimelineFailure.DispatchFailed;
                return false;
            }
            execution.DispatchStarted = true;
        }

        if (execution.Stage != P18DActorChoiceStage.ProvenNoInstall)
        {
            if (!execution.ActionSuccessResolved)
            {
                try
                {
                    LogChosenTargetAction(execution.Actor, execution.Action);
                    execution.ActionSuccess = RollActionSuccess(
                        execution.Actor, execution.Action.Action, execution.Action);
                    execution.ActionSuccessResolved = true;
                }
                catch (Exception exception)
                {
                    if (!CompleteP18DActorChoiceThrew(execution, out failure))
                        throw new InvalidOperationException(
                            "Actor choice threw and its terminal attempt could not be recorded: " + failure + ".",
                            exception);
                    throw;
                }
                if (!execution.ActionSuccess)
                {
                    execution.Result = NpcActionResult.Failed();
                    execution.Stage = P18DActorChoiceStage.ActionFailed;
                    return CompleteP18DActorChoiceReturned(execution, out failure);
                }
            }
        }

        KeyedSaleReceipt receipt;
        try
        {
            bool executed = merchantSystem.TryExecuteKeyedLocalMarketSale(
                execution.Actor, execution.ActorPersonId, execution.Action,
                execution.ProposalId, execution.Fingerprint, execution.InputId,
                execution.RequestId, p18dIntradayProfile.ProfileId,
                execution.RequestInstant, out receipt);
            if (!executed || receipt == null)
            {
                execution.Result = NpcActionResult.Failed();
                execution.Stage = P18DActorChoiceStage.ActionFailed;
                return CompleteP18DActorChoiceReturned(execution, out failure);
            }
        }
        catch (Exception)
        {
            // Resolve an uncertain owner response by replaying the identical key.
            try
            {
                if (!merchantSystem.TryExecuteKeyedLocalMarketSale(
                        execution.Actor, execution.ActorPersonId, execution.Action,
                        execution.ProposalId, execution.Fingerprint, execution.InputId,
                        execution.RequestId, p18dIntradayProfile.ProfileId,
                        execution.RequestInstant, out receipt)
                    || receipt == null)
                {
                    execution.Stage = P18DActorChoiceStage.Unresolved;
                    return RecordP18DRetryableOutcome(execution, "unresolved", out failure);
                }
            }
            catch (Exception)
            {
                execution.Stage = P18DActorChoiceStage.Unresolved;
                return RecordP18DRetryableOutcome(execution, "unresolved", out failure);
            }
        }

        execution.Receipt = receipt;
        switch (receipt.Outcome)
        {
            case KeyedSaleOutcome.Committed:
                execution.Stage = P18DActorChoiceStage.SaleCommitted;
                execution.Result = NpcActionResult.Succeeded();
                return CompleteP18DActorChoiceReturned(execution, out failure);
            case KeyedSaleOutcome.TerminalRejection:
                execution.Stage = P18DActorChoiceStage.SaleRejected;
                execution.Result = NpcActionResult.Failed();
                return CompleteP18DActorChoiceReturned(execution, out failure);
            case KeyedSaleOutcome.ProvenNoInstall:
                execution.Stage = P18DActorChoiceStage.ProvenNoInstall;
                return RecordP18DRetryableOutcome(execution, "proven-no-install", out failure);
            case KeyedSaleOutcome.Unresolved:
                execution.Stage = P18DActorChoiceStage.Unresolved;
                return RecordP18DRetryableOutcome(execution, "unresolved", out failure);
            default:
                failure = TimelineFailure.DispatchFailed;
                return false;
        }
    }

    private bool RecordP18DRetryableOutcome(P18DActorChoiceExecution execution,
        string outcome, out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        string operationId = SpatialStableKey.Encode("p18d-proposal-retryable",
            execution.RequestId, outcome,
            execution.RetryableReceiptCount.ToString(CultureInfo.InvariantCulture));
        if (!p18dActorDecisionRequestState.TryRecordRetryableProposal(
                operationId, execution.RequestId, execution.InputId,
                execution.ActorPersonId, execution.RequestInstant,
                execution.ProposalId, outcome, out _))
        {
            failure = TimelineFailure.DispatchFailed;
            return false;
        }
        execution.RetryableReceiptCount++;
        return true;
    }

    private bool CompleteP18DActorChoiceRejection(P18DActorChoiceExecution execution,
        ActorChoiceFailure reason, out TimelineFailure failure)
    {
        execution.TerminalKind = P18DActorChoiceTerminalKind.Rejected;
        execution.TerminalFailure = reason;
        execution.TerminalProposalId = execution.ProposalId;
        execution.TerminalOutcome = "rejected:" + reason;
        return TryFinalizeP18DActorChoice(execution, out failure);
    }

    private bool CompleteP18DActorChoiceReturned(P18DActorChoiceExecution execution,
        out TimelineFailure failure)
    {
        if (execution.Result != null && execution.Result.Success)
        {
            if (execution.Receipt != null && !execution.TradeLogApplied)
            {
                execution.TradeLogApplied = true;
                merchantSystem.LogP18DLocalMarketSale(
                    execution.Actor, execution.Action, execution.Receipt);
            }
            if (!execution.SuccessEffectsApplied)
            {
                ApplySuccessStatusChanges(execution.Actor,
                    execution.Action, execution.Action.Action);
                execution.SuccessEffectsApplied = true;
            }
        }
        if (execution.Result != null && !string.IsNullOrEmpty(execution.Result.Message))
            logger?.Log(SimulationLogCategory.NpcAction, execution.Result.Message);

        execution.TerminalKind = P18DActorChoiceTerminalKind.Returned;
        execution.TerminalProposalId = execution.ProposalId;
        execution.TerminalOutcome = execution.Result != null && execution.Result.Success
            ? "returned:success" : "returned:failure";
        return TryFinalizeP18DActorChoice(execution, out failure);
    }

    private bool CompleteP18DActorChoiceThrew(P18DActorChoiceExecution execution,
        out TimelineFailure failure)
    {
        execution.TerminalKind = P18DActorChoiceTerminalKind.Threw;
        execution.TerminalProposalId = execution.ProposalId;
        execution.TerminalOutcome = "threw";
        return TryFinalizeP18DActorChoice(execution, out failure);
    }

    private bool TryFinalizeP18DActorChoice(P18DActorChoiceExecution execution,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        ActorChoiceTemporalBoundaryReference boundary = new ActorChoiceTemporalBoundaryReference(
            p18dIntradayProfile.ProfileId, p18dTimeline.CurrentInstant,
            execution.RequestId, 0L);
        string operationId = SpatialStableKey.Encode("p18d-terminal-p11",
            execution.RequestId, execution.TerminalKind.ToString());
        bool recorded = execution.TerminalKind == P18DActorChoiceTerminalKind.Rejected
            ? actorChoiceStore.TryRecordTemporalRejected(
                new ActorChoiceInputId(execution.InputId), boundary, operationId,
                execution.TerminalFailure, out _)
            : execution.TerminalKind == P18DActorChoiceTerminalKind.Returned
                ? actorChoiceStore.TryRecordTemporalAttemptReturned(
                    new ActorChoiceInputId(execution.InputId), boundary, operationId,
                    execution.Result, out _)
                : actorChoiceStore.TryRecordTemporalAttemptThrew(
                    new ActorChoiceInputId(execution.InputId), boundary, operationId, out _);
        if (!recorded)
        {
            failure = TimelineFailure.DispatchFailed;
            return false;
        }
        execution.P11TerminalRecorded = true;
        return TryReconcileP18DTerminalExecution(execution, out failure);
    }

    private bool TryReconcileP18DTerminalExecutions(out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        foreach (P18DActorChoiceExecution execution in p18dActorChoiceExecutions.Values)
        {
            if (!execution.P11TerminalRecorded || execution.CRequestTerminalRecorded) continue;
            if (!TryReconcileP18DTerminalExecution(execution, out failure)) return false;
        }
        return true;
    }

    private bool TryReconcileP18DTerminalExecution(P18DActorChoiceExecution execution,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.None;
        if (execution.CRequestTerminalRecorded) return true;
        if (!p18dActorDecisionRequestState.TryReconcileTerminal(
                SpatialStableKey.Encode("p18d-terminal-c", execution.RequestId,
                    execution.TerminalOutcome),
                execution.RequestId, execution.InputId,
                execution.ActorPersonId, execution.RequestInstant,
                execution.TerminalProposalId, execution.TerminalOutcome, out _))
        {
            failure = TimelineFailure.DispatchFailed;
            return false;
        }
        execution.CRequestTerminalRecorded = true;
        execution.Stage = P18DActorChoiceStage.Terminal;
        return true;
    }
}

internal enum P18DActorChoiceStage
{
    Prepared = 1,
    Deferred = 2,
    ProvenNoInstall = 3,
    Unresolved = 4,
    ActionFailed = 5,
    SaleCommitted = 6,
    SaleRejected = 7,
    Terminal = 8
}

internal enum P18DActorChoiceTerminalKind
{
    None = 0,
    Rejected = 1,
    Returned = 2,
    Threw = 3
}

internal sealed class P18DActorChoiceExecution
{
    public string RequestId { get; }
    public string InputId { get; }
    public PersonId ActorPersonId { get; }
    public string ProposalId { get; }
    public LogicalTick RequestInstant { get; }
    public string Fingerprint { get; set; }
    public NpcRuntime Actor { get; set; }
    public NpcActionRuntime Action { get; set; }
    public string DecisionRecordId { get; set; }
    public P18DActorChoiceStage Stage { get; set; }
    public bool CorrelationPrepared { get; set; }
    public bool DispatchStarted { get; set; }
    public bool ActionSuccessResolved { get; set; }
    public bool ActionSuccess { get; set; }
    public bool SuccessEffectsApplied { get; set; }
    public int RetryableReceiptCount { get; set; }
    public KeyedSaleReceipt Receipt { get; set; }
    public NpcActionResult Result { get; set; }
    public P18DActorChoiceTerminalKind TerminalKind { get; set; }
    public ActorChoiceFailure TerminalFailure { get; set; }
    public string TerminalProposalId { get; set; }
    public string TerminalOutcome { get; set; }
    public bool P11TerminalRecorded { get; set; }
    public bool CRequestTerminalRecorded { get; set; }
    public bool TradeLogApplied { get; set; }

    public P18DActorChoiceExecution(string requestId, string inputId,
        PersonId actorPersonId, string proposalId, LogicalTick requestInstant)
    {
        RequestId = requestId;
        InputId = inputId;
        ActorPersonId = actorPersonId;
        ProposalId = proposalId;
        RequestInstant = requestInstant;
    }
}

public partial class MerchantSystem
{
    internal bool IsP18DTradeStateEligible(NpcRuntime actor) => actor != null
        && IsMerchant(actor) && actor.IsAlive && actor.CurrentCity != null
        && !actor.IsTraveling;

    internal void LogP18DLocalMarketSale(NpcRuntime seller,
        NpcActionRuntime action, KeyedSaleReceipt receipt)
    {
        if (seller == null || action?.TargetItem == null || receipt?.Snapshot == null
            || receipt.Outcome != KeyedSaleOutcome.Committed) return;
        int quantity = receipt.Snapshot.EffectiveQuantity;
        CityRuntime city = action.TargetCity;
        if (quantity <= 0 || city == null) return;
        logger?.Log(SimulationLogCategory.Trade,
            $"{seller.NpcName} vendeu {quantity} {action.TargetItem.itemName} ao mercado de {city.CityName} por {receipt.Snapshot.UnitPrice:0.##} cada");
    }
}
