using System;
using System.Collections.Generic;

/// <summary>Allocates and appends one decision through the shared Core owners.</summary>
internal static class DailyV1DecisionRecordOperation
{
    internal static NpcDecisionType GetDecisionType(
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload)
    {
        switch (action?.Values.actionType?.Value ?? int.MinValue)
        {
            case DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue:
                return NpcDecisionType.TradePurchase;
            case DailyV1NpcDefinitionRules.SellGoodsActionTypeValue:
                return NpcDecisionType.TradeSale;
            case DailyV1NpcDefinitionRules.TravelActionTypeValue:
                if (payload?.TravelReason == NpcTravelReason.TradeReposition)
                    return NpcDecisionType.TradeReposition;
                return payload?.TravelReason == NpcTravelReason.CommercialScout
                    ? NpcDecisionType.CommercialScout
                    : NpcDecisionType.Travel;
            case DailyV1NpcDefinitionRules.FleeCityActionTypeValue:
                return NpcDecisionType.Travel;
            case DailyV1NpcDefinitionRules.ArrestActionTypeValue:
                return NpcDecisionType.Arrest;
            case DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue:
                return NpcDecisionType.Escape;
            default:
                return NpcDecisionType.Action;
        }
    }

    internal static NpcDecisionRecord RecordSelectedAction(
        RuntimeIdAllocator idAllocator,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner action,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin)
    {
        if (graph == null || actor == null || action == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !graph.Definitions.Actions.TryGetValue(action.Action?.DefinitionId ?? string.Empty,
                out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, action.Action)
            || (action.TargetNpcRuntimeId != null
                && !graph.ContainsNpcRuntimeId(action.TargetNpcRuntimeId))) return null;

        string targetLocationRuntimeId = null;
        if (action.TargetCityRuntimeId != null
            && !graph.TryGetCityLocationRuntimeId(action.TargetCityRuntimeId,
                out targetLocationRuntimeId)) return null;
        IReadOnlyList<string> targetIds = action.TargetNpcRuntimeId == null
            ? null
            : new[] { action.TargetNpcRuntimeId };
        return Record(idAllocator, recordSequence, decisionStore, absoluteDay,
            actor.RuntimeId, decisionType, origin, action.Action.DefinitionId,
            null, targetIds, targetLocationRuntimeId,
            action.CommercialDecisionEvidence, action.CommercialScoutingEvidence);
    }

    /// <summary>Records the selected action before installing that same exact
    /// payload into the graph-owned slot. A failed record append retains the
    /// prior behavior of still attempting the action-slot install.</summary>
    internal static bool TryRecordAndCommitSelectedActionSlot(
        RuntimeIdAllocator idAllocator,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner action,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        Func<DailyV1NpcActionPayloadOwner, bool> prepareProjection,
        Func<bool> slotAdmission,
        out NpcDecisionRecord decision)
    {
        decision = null;
        if (graph == null || actor == null || action == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !graph.Definitions.Actions.TryGetValue(action.Action?.DefinitionId ?? string.Empty,
                out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, action.Action))
            return false;

        decision = RecordSelectedAction(idAllocator, recordSequence, decisionStore,
            absoluteDay, graph, actor, action, decisionType, origin);
        if (decision != null) action.SetOriginDecisionId(decision.DecisionId);
        if (prepareProjection != null && !prepareProjection(action)) return false;
        return graph.TryCommitCurrentActionSlot(actor, action.Action, action, slotAdmission);
    }

    /// <summary>Commits an already-identified selected action without allocating
    /// a second decision identity. Used for existing Travel-plan decisions whose
    /// OriginDecisionId is authoritative and whose legacy path deliberately
    /// skips a new decision record.</summary>
    internal static bool TryCommitSelectedActionSlotWithExistingIdentity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner action,
        Func<DailyV1NpcActionPayloadOwner, bool> prepareProjection,
        Func<bool> slotAdmission)
    {
        if (graph == null || actor == null || action == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(graph.Definitions, actor.Definitions)
            || !graph.Definitions.Actions.TryGetValue(action.Action?.DefinitionId ?? string.Empty,
                out DailyV1ActionDefinition admittedAction)
            || !ReferenceEquals(admittedAction, action.Action))
            return false;

        if (prepareProjection != null && !prepareProjection(action)) return false;
        return graph.TryCommitCurrentActionSlot(actor, action.Action, action, slotAdmission);
    }

    internal static NpcDecisionRecord Record(
        RuntimeIdAllocator idAllocator,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        long absoluteDay,
        string primaryActorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        IEnumerable<NpcDecisionParticipant> decisionParticipants,
        IEnumerable<string> targetRuntimeIds,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence,
        CommercialScoutingEvidence commercialScoutingEvidence)
    {
        if (idAllocator == null) throw new ArgumentNullException(nameof(idAllocator));
        if (recordSequence == null) throw new ArgumentNullException(nameof(recordSequence));
        if (decisionStore == null) throw new ArgumentNullException(nameof(decisionStore));

        // Preserve the existing order: decision identity, sequence, record construction, store append.
        string decisionId = idAllocator.AllocateDecisionId();
        long sequence = recordSequence.Allocate();
        NpcDecisionRecord decision = new NpcDecisionRecord(
            decisionId, absoluteDay, sequence, primaryActorRuntimeId, decisionType,
            origin, actionDefinitionId, decisionParticipants, targetRuntimeIds,
            targetLocationRuntimeId, commercialEvidence, commercialScoutingEvidence);
        return decisionStore.Record(decision) ? decision : null;
    }
}
