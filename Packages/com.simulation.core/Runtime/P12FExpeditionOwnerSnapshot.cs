using System;
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

    internal static bool TryCapture(ExpeditionStore source, DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        out P12FExpeditionOwnerSnapshot snapshot, out P12FCommitmentSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12FCommitmentSnapshotFailure.InvalidCaptureContext;
        if (source == null || !P12FCommitmentSnapshotValidation.HasExactContext(token, exactOwnerSectionVector))
            return false;
        using (source.EnterReadWindow())
        {
            if (!P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    ExpeditionCensusProvider.SectionId, ExpeditionCensusProvider.SchemaVersion,
                    source, out int expectedCount, out long expectedRevision))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidOwnerSection;
                return false;
            }
            if (!source.ValidateCensus(out int count, out long revision)
                || count != expectedCount || revision != expectedRevision
                || !source.TryCaptureOwnerSnapshot(out snapshot) || snapshot == null
                || snapshot.Expeditions.Count != expectedCount || snapshot.Revision != expectedRevision)
            {
                snapshot = null;
                failure = P12FCommitmentSnapshotFailure.InvalidOwner;
                return false;
            }
            if (!source.ValidateCensus(out int afterCount, out long afterRevision)
                || afterCount != expectedCount || afterRevision != expectedRevision
                || !P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    ExpeditionCensusProvider.SectionId, ExpeditionCensusProvider.SchemaVersion,
                    source, out int finalCount, out long finalRevision)
                || finalCount != afterCount || finalRevision != afterRevision)
            {
                snapshot = null;
                failure = P12FCommitmentSnapshotFailure.StaleCapture;
                return false;
            }
            failure = P12FCommitmentSnapshotFailure.None;
            return true;
        }
    }

    internal static bool IsValidRecord(P12FExpeditionOwnerSnapshotRecord value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ExpeditionId)
            || string.IsNullOrWhiteSpace(value.TargetSiteRuntimeId)
            || string.IsNullOrWhiteSpace(value.OriginLocationRuntimeId)
            || string.IsNullOrWhiteSpace(value.TargetLocationRuntimeId)
            || string.IsNullOrWhiteSpace(value.OutboundRouteRuntimeId)
            || !IsOptionalId(value.TravelPartyId) || !IsOptionalId(value.OriginDecisionId)
            || !IsOptionalId(value.CurrentLocalPlaceRuntimeId)
            || !IsOptionalId(value.TargetItemDefinitionId) || !IsOptionalId(value.TargetNotableItemRuntimeId)
            || !IsOptionalId(value.TargetOppositionRuntimeId)
            || !Enum.IsDefined(typeof(ExpeditionState), value.State)
            || !Enum.IsDefined(typeof(ExpeditionObjectiveType), value.ObjectiveType)
            || value.MemberRuntimeIds == null || value.PerformerRuntimeIds == null || value.SupportRuntimeIds == null
            || value.VisitedLocalPlaceRuntimeIds == null || value.ObservedLocalConnectionRuntimeIds == null
            || value.RequiredProgress <= 0 || value.Progress < 0 || value.Progress > value.RequiredProgress
            || (value.ObjectiveCompleted != (value.Progress == value.RequiredProgress))) return false;
        if ((value.State == ExpeditionState.TravelingToSite || value.State == ExpeditionState.Returning)
            && string.IsNullOrWhiteSpace(value.TravelPartyId)) return false;
        foreach (string id in value.MemberRuntimeIds)
            if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (string id in value.PerformerRuntimeIds)
            if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (string id in value.SupportRuntimeIds)
            if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (string id in value.VisitedLocalPlaceRuntimeIds)
            if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (string id in value.ObservedLocalConnectionRuntimeIds)
            if (string.IsNullOrWhiteSpace(id)) return false;
        HashSet<string> members = new HashSet<string>(value.MemberRuntimeIds, StringComparer.Ordinal);
        if (members.Count != value.MemberRuntimeIds.Count || members.Count == 0) return false;
        HashSet<string> roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in value.PerformerRuntimeIds) if (!members.Contains(id) || !roles.Add(id)) return false;
        foreach (string id in value.SupportRuntimeIds) if (!members.Contains(id) || !roles.Add(id)) return false;
        return roles.Count == members.Count;
    }

    private static bool IsOptionalId(string value) => value == null || !string.IsNullOrWhiteSpace(value);
}
