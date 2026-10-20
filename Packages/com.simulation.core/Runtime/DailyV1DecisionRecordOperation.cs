using System;
using System.Collections.Generic;

/// <summary>Allocates and appends one decision through the shared Core owners.</summary>
internal static class DailyV1DecisionRecordOperation
{
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
