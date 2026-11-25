using System;
using System.Collections.Generic;

public sealed class NpcDecisionRecorder : IOwnerSectionCensusProvider
{
    public const string OccurrenceReceiptSectionId = DailyV1DecisionOccurrenceReceiptAuthority.SectionId;
    public const int OccurrenceReceiptSectionSchemaVersion = DailyV1DecisionOccurrenceReceiptAuthority.SectionSchemaVersion;

    private readonly DailyV1DecisionOccurrenceReceiptAuthority receiptAuthority;
    private readonly SimulationLogger logger;

    private RuntimeIdAllocator idAllocator => receiptAuthority.RuntimeIdAllocator;
    private SimulationTime simulationTime => receiptAuthority.SimulationTime;
    private SimulationRecordSequence recordSequence => receiptAuthority.RecordSequence;
    private NpcDecisionStore decisionStore => receiptAuthority.DecisionStore;

    internal RuntimeIdAllocator DecisionIdAllocator => idAllocator;
    internal SimulationRecordSequence RecordSequence => recordSequence;
    internal NpcDecisionStore DecisionStore => decisionStore;
    internal bool TryRecordAndInstallSelectedAction(
        NpcRuntime actor,
        NpcActionRuntime actionRuntime,
        NpcDecisionOrigin origin,
        out NpcDecisionRecord decision,
        out bool handled)
    {
        decision = null;
        handled = false;
        if (actor?.SelectedRuntimeOwners == null || actionRuntime == null
            || actionRuntime.Action == null
            || !actor.TryPrepareSelectedActionPayload(
                actionRuntime, out DailyV1NpcActionPayloadOwner payload))
            return false;

        handled = true;
        bool committed;
        try
        {
            Func<DailyV1NpcActionPayloadOwner, bool> prepareProjection = candidate =>
                actionRuntime.TryUseSelectedPayload(
                    candidate, actor.SelectedDailyV1DefinitionGraph);
            Func<bool> slotAdmission = () => actor.CanCommitP12LifecycleMutation(
                false, false, true, actionRuntime);
            if (actionRuntime.Action.actionType == NpcActionType.Travel
                && !string.IsNullOrWhiteSpace(actionRuntime.OriginDecisionId))
            {
                committed = DailyV1DecisionRecordOperation.TryCommitSelectedActionSlotWithExistingIdentity(
                    actor.SelectedRuntimeOwnerGraph, actor.SelectedRuntimeOwners, payload,
                    prepareProjection, slotAdmission);
            }
            else
            {
                committed = DailyV1DecisionRecordOperation.TryRecordAndCommitSelectedActionSlot(
                    idAllocator, recordSequence, decisionStore, simulationTime.AbsoluteDay,
                    actor.SelectedRuntimeOwnerGraph, actor.SelectedRuntimeOwners, payload,
                    GetDecisionType(actionRuntime), origin,
                    prepareProjection, slotAdmission, out decision);
            }
        }
        catch (ArgumentException exception)
        {
            logger.LogError("Cannot create NPC decision record: " + exception.Message);
            committed = actionRuntime.TryUseSelectedPayload(
                    payload, actor.SelectedDailyV1DefinitionGraph)
                && actor.SelectedRuntimeOwnerGraph.TryCommitCurrentActionSlot(
                    actor.SelectedRuntimeOwners, payload.Action, payload,
                    () => actor.CanCommitP12LifecycleMutation(false, false, true, actionRuntime));
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError("Cannot allocate NPC decision record identity: " + exception.Message);
            committed = actionRuntime.TryUseSelectedPayload(
                    payload, actor.SelectedDailyV1DefinitionGraph)
                && actor.SelectedRuntimeOwnerGraph.TryCommitCurrentActionSlot(
                    actor.SelectedRuntimeOwners, payload.Action, payload,
                    () => actor.CanCommitP12LifecycleMutation(false, false, true, actionRuntime));
        }
        if (!committed) return true;

        if (!actor.TryInstallPrecommittedCurrentActionRuntime(actionRuntime))
            throw new InvalidOperationException(
                "A committed selected action slot could not publish its Unity action projection.");
        return true;
    }

    public NpcDecisionRecorder(
        RuntimeIdAllocator idAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        SimulationLogger logger = null)
    {
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
        receiptAuthority = DailyV1DecisionOccurrenceReceiptAuthority.Create(
            idAllocator, simulationTime, recordSequence, decisionStore, this.logger);
    }

    internal NpcDecisionRecorder(
        DailyV1DecisionOccurrenceReceiptAuthority receiptAuthority,
        SimulationLogger logger)
    {
        this.receiptAuthority = receiptAuthority
            ?? throw new ArgumentNullException(nameof(receiptAuthority));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Reports the exact live cardinality and revision of the recorder-owned
    /// idempotency receipt section. This is census evidence only, not an export.
    /// </summary>
    public OwnerSectionCensusWitness GetOccurrenceReceiptCensus()
    {
        return receiptAuthority.GetCensus();
    }
    OwnerSectionCensusWitness IOwnerSectionCensusProvider.GetCurrentCensus()
    {
        return GetOccurrenceReceiptCensus();
    }

    public NpcDecisionRecord RecordChosenAction(NpcRuntime actor, NpcActionRuntime actionRuntime, NpcDecisionOrigin origin)
    {
        if (actor == null || actionRuntime == null || actionRuntime.Action == null)
        {
            return null;
        }

        if (actionRuntime.Action.actionType == NpcActionType.Travel
            && string.IsNullOrWhiteSpace(actionRuntime.OriginDecisionId) == false)
        {
            return null;
        }

        string actionDefinitionId = actionRuntime.Action.DefinitionId;

        if (string.IsNullOrWhiteSpace(actionDefinitionId) == true)
        {
            logger.LogError("Cannot record chosen NPC action: ActionDefinitionId is empty.");
            return null;
        }

        NpcDecisionRecord decision;
        if (actor.SelectedRuntimeOwners != null)
        {
            if (!actor.TryPrepareSelectedActionPayload(actionRuntime,
                    out DailyV1NpcActionPayloadOwner selectedPayload)) return null;
            try
            {
                decision = DailyV1DecisionRecordOperation.RecordSelectedAction(
                    idAllocator, recordSequence, decisionStore, simulationTime.AbsoluteDay,
                    actor.SelectedRuntimeOwnerGraph, actor.SelectedRuntimeOwners,
                    selectedPayload, GetDecisionType(actionRuntime), origin);
            }
            catch (ArgumentException exception)
            {
                logger.LogError("Cannot create NPC decision record: " + exception.Message);
                return null;
            }
            catch (InvalidOperationException exception)
            {
                logger.LogError("Cannot allocate NPC decision record identity: " + exception.Message);
                return null;
            }
        }
        else
        {
            decision = Record(
                actor.RuntimeId,
                GetDecisionType(actionRuntime),
                origin,
                actionDefinitionId,
                actionRuntime.TargetNpc?.RuntimeId,
                actionRuntime.TargetCity?.Location?.RuntimeId,
                actionRuntime.CommercialDecisionEvidence,
                actionRuntime.CommercialScoutingEvidence);
        }

        if (decision != null)
        {
            actionRuntime.SetOriginDecisionId(decision.DecisionId);
        }

        return decision;
    }

    public NpcDecisionRecord Record(
        string actorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        string targetRuntimeId,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence = null)
    {
        return RecordWithParticipants(
            actorRuntimeId,
            decisionType,
            origin,
            actionDefinitionId,
            null,
            string.IsNullOrWhiteSpace(targetRuntimeId) == true ? null : new[] { targetRuntimeId },
            targetLocationRuntimeId,
            commercialEvidence,
            commercialScoutingEvidence);
    }

    public NpcDecisionRecord RecordWithParticipants(
        string primaryActorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        IEnumerable<NpcDecisionParticipant> decisionParticipants,
        IEnumerable<string> targetRuntimeIds,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence = null)
    {
        try
        {
            return DailyV1DecisionRecordOperation.Record(
                idAllocator, recordSequence, decisionStore, simulationTime.AbsoluteDay,
                primaryActorRuntimeId, decisionType, origin, actionDefinitionId,
                decisionParticipants, targetRuntimeIds, targetLocationRuntimeId,
                commercialEvidence, commercialScoutingEvidence);
        }
        catch (ArgumentException exception)
        {
            logger.LogError("Cannot create NPC decision record: " + exception.Message);
            return null;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError("Cannot allocate NPC decision record identity: " + exception.Message);
            return null;
        }
    }

    /// <summary>
    /// Records an owner operation once. The receipt is retained by the recorder,
    /// the authority that allocates decision identity/sequence and appends the
    /// corresponding decision-store record.
    /// </summary>
    internal bool TryRecordOccurrenceOnce(
        string operationIdentity,
        string operationFingerprint,
        string primaryActorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence,
        out NpcDecisionRecord decision) =>
        receiptAuthority.TryRecordOccurrenceOnce(
            operationIdentity, operationFingerprint, primaryActorRuntimeId,
            decisionType, origin, actionDefinitionId, targetLocationRuntimeId,
            commercialEvidence, out decision);
    internal static NpcDecisionType GetDecisionType(NpcActionRuntime actionRuntime)
    {
        if (actionRuntime?.SelectedOwner is DailyV1NpcActionPayloadOwner selectedPayload)
            return DailyV1DecisionRecordOperation.GetDecisionType(
                selectedPayload.Action, selectedPayload);

        switch (actionRuntime.Action.actionType)
        {
            case NpcActionType.BuyGoods:
                return NpcDecisionType.TradePurchase;
            case NpcActionType.SellGoods:
                return NpcDecisionType.TradeSale;
            case NpcActionType.Travel:
                if (actionRuntime.TravelReason == NpcTravelReason.TradeReposition)
                {
                    return NpcDecisionType.TradeReposition;
                }

                return actionRuntime.TravelReason == NpcTravelReason.CommercialScout
                    ? NpcDecisionType.CommercialScout
                    : NpcDecisionType.Travel;
            case NpcActionType.FleeCity:
                return NpcDecisionType.Travel;
            case NpcActionType.Arrest:
                return NpcDecisionType.Arrest;
            case NpcActionType.EscapePrison:
                return NpcDecisionType.Escape;
            default:
                return NpcDecisionType.Action;
        }
    }
}
