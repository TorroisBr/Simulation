using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Detached active Expedition owner value, including all current objective and exploration progress.</summary>
internal sealed class P12FExpeditionOwnerSnapshotRecord
{
    internal string ExpeditionId { get; }
    internal string TargetSiteRuntimeId { get; }
    internal string OriginLocationRuntimeId { get; }
    internal string TargetLocationRuntimeId { get; }
    internal string OutboundRouteRuntimeId { get; }
    internal string TravelPartyId { get; }
    internal string OriginDecisionId { get; }
    internal IReadOnlyList<string> MemberRuntimeIds { get; }
    internal IReadOnlyList<string> PerformerRuntimeIds { get; }
    internal IReadOnlyList<string> SupportRuntimeIds { get; }
    internal ExpeditionState State { get; }
    internal string CurrentLocalPlaceRuntimeId { get; }
    internal IReadOnlyList<string> VisitedLocalPlaceRuntimeIds { get; }
    internal IReadOnlyList<string> ObservedLocalConnectionRuntimeIds { get; }
    internal ExpeditionObjectiveType ObjectiveType { get; }
    internal string TargetItemDefinitionId { get; }
    internal string TargetNotableItemRuntimeId { get; }
    internal string TargetOppositionRuntimeId { get; }
    internal int RequiredProgress { get; }
    internal int Progress { get; }
    internal bool ObjectiveCompleted { get; }
    internal bool AllowContinueAfterCompletion { get; }

    internal P12FExpeditionOwnerSnapshotRecord(string expeditionId, string targetSiteRuntimeId,
        string originLocationRuntimeId, string targetLocationRuntimeId, string outboundRouteRuntimeId,
        string travelPartyId, string originDecisionId, IEnumerable<string> members,
        IEnumerable<string> performers, IEnumerable<string> supports, ExpeditionState state,
        string currentLocalPlaceRuntimeId, IEnumerable<string> visited, IEnumerable<string> observed,
        ExpeditionObjectiveType objectiveType, string targetItemDefinitionId,
        string targetNotableItemRuntimeId, string targetOppositionRuntimeId,
        int requiredProgress, int progress, bool objectiveCompleted, bool allowContinueAfterCompletion)
    {
        ExpeditionId = expeditionId;
        TargetSiteRuntimeId = targetSiteRuntimeId;
        OriginLocationRuntimeId = originLocationRuntimeId;
        TargetLocationRuntimeId = targetLocationRuntimeId;
        OutboundRouteRuntimeId = outboundRouteRuntimeId;
        TravelPartyId = travelPartyId;
        OriginDecisionId = originDecisionId;
        MemberRuntimeIds = Copy(members); PerformerRuntimeIds = Copy(performers); SupportRuntimeIds = Copy(supports);
        State = state; CurrentLocalPlaceRuntimeId = currentLocalPlaceRuntimeId;
        VisitedLocalPlaceRuntimeIds = Copy(visited); ObservedLocalConnectionRuntimeIds = Copy(observed);
        ObjectiveType = objectiveType; TargetItemDefinitionId = targetItemDefinitionId;
        TargetNotableItemRuntimeId = targetNotableItemRuntimeId; TargetOppositionRuntimeId = targetOppositionRuntimeId;
        RequiredProgress = requiredProgress; Progress = progress; ObjectiveCompleted = objectiveCompleted;
        AllowContinueAfterCompletion = allowContinueAfterCompletion;
    }

    private static IReadOnlyList<string> Copy(IEnumerable<string> values) =>
        new ReadOnlyCollection<string>(new List<string>(values));
}

internal sealed class P12FExpeditionOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;
    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12FExpeditionOwnerSnapshotRecord> Expeditions { get; }

    internal P12FExpeditionOwnerSnapshot(int schemaVersion, long revision,
        IEnumerable<P12FExpeditionOwnerSnapshotRecord> expeditions)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        Expeditions = new ReadOnlyCollection<P12FExpeditionOwnerSnapshotRecord>(
            new List<P12FExpeditionOwnerSnapshotRecord>(expeditions));
    }
}
