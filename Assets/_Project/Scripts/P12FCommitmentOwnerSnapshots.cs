using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12FCommitmentSnapshotFailure
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerSection,
    InvalidOwner,
    InvalidOwnerValue,
    InvalidNpcBinding,
    InvalidIdentityBinding,
    InvalidSnapshot,
    StageFailed,
    StaleCapture
}

/// <summary>Detached active TravelParty owner value; contains no source owner or NPC references.</summary>
internal sealed class P12FTravelPartyOwnerSnapshotRecord
{
    internal string TravelPartyId { get; }
    internal string OriginLocationRuntimeId { get; }
    internal string DestinationLocationRuntimeId { get; }
    internal string RouteRuntimeId { get; }
    internal IReadOnlyList<string> TravelerRuntimeIds { get; }
    internal IReadOnlyList<string> EscortRuntimeIds { get; }
    internal IReadOnlyList<string> MemberRuntimeIds { get; }
    internal IReadOnlyList<TravelPartyMemberCost> MemberCosts { get; }
    internal int TravelDaysTotal { get; }
    internal string OriginDecisionId { get; }

    internal P12FTravelPartyOwnerSnapshotRecord(TravelPartyRuntime party)
    {
        TravelPartyId = party.TravelPartyId;
        OriginLocationRuntimeId = party.OriginLocationRuntimeId;
        DestinationLocationRuntimeId = party.DestinationLocationRuntimeId;
        RouteRuntimeId = party.RouteRuntimeId;
        TravelerRuntimeIds = Copy(party.TravelerRuntimeIds);
        EscortRuntimeIds = Copy(party.EscortRuntimeIds);
        MemberRuntimeIds = Copy(party.MemberRuntimeIds);
        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>();
        foreach (TravelPartyMemberCost cost in party.MemberCosts)
            costs.Add(new TravelPartyMemberCost(cost.RuntimeId, cost.Amount));
        MemberCosts = new ReadOnlyCollection<TravelPartyMemberCost>(costs);
        TravelDaysTotal = party.TravelDaysTotal;
        OriginDecisionId = party.OriginDecisionId;
    }

    private static IReadOnlyList<string> Copy(IEnumerable<string> source) =>
        new ReadOnlyCollection<string>(new List<string>(source));
}

internal sealed class P12FTravelPartyOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;
    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12FTravelPartyOwnerSnapshotRecord> Parties { get; }

    internal P12FTravelPartyOwnerSnapshot(int schemaVersion, long revision,
        IEnumerable<P12FTravelPartyOwnerSnapshotRecord> parties)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        Parties = new ReadOnlyCollection<P12FTravelPartyOwnerSnapshotRecord>(new List<P12FTravelPartyOwnerSnapshotRecord>(parties));
    }

    internal static bool TryCapture(TravelPartyStore source, DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        out P12FTravelPartyOwnerSnapshot snapshot, out P12FCommitmentSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12FCommitmentSnapshotFailure.InvalidCaptureContext;
        if (source == null || !P12FCommitmentSnapshotValidation.HasExactContext(token, exactOwnerSectionVector)) return false;
        using (source.EnterReadWindow())
        {
            if (!P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    TravelPartyCensusProvider.SectionId, TravelPartyCensusProvider.SchemaVersion,
                    source, out int expectedCount, out long expectedRevision))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidOwnerSection;
                return false;
            }
            if (!source.ValidateCensus(out int count, out long revision)
                || count != expectedCount || revision != expectedRevision)
            {
                failure = P12FCommitmentSnapshotFailure.InvalidOwner;
                return false;
            }
            List<P12FTravelPartyOwnerSnapshotRecord> records = new List<P12FTravelPartyOwnerSnapshotRecord>(count);
            foreach (TravelPartyRuntime party in source.ActiveParties)
            {
                if (!IsValid(party))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidOwnerValue;
                    return false;
                }
                records.Add(new P12FTravelPartyOwnerSnapshotRecord(party));
            }
            if (!source.ValidateCensus(out int afterCount, out long afterRevision)
                || afterCount != expectedCount || afterRevision != expectedRevision
                || !P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    TravelPartyCensusProvider.SectionId, TravelPartyCensusProvider.SchemaVersion,
                    source, out int finalCount, out long finalRevision)
                || finalCount != afterCount || finalRevision != afterRevision)
            {
                failure = P12FCommitmentSnapshotFailure.StaleCapture;
                return false;
            }
            snapshot = new P12FTravelPartyOwnerSnapshot(CurrentSchemaVersion, revision, records);
            failure = P12FCommitmentSnapshotFailure.None;
            return true;
        }
    }

    internal static bool TryStage(P12FTravelPartyOwnerSnapshot snapshot,
        IReadOnlyList<NpcRuntime> stagedNpcs, IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        RuntimeIdentityRegistry stagedIdentities, out TravelPartyStore stagedStore,
        out P12FCommitmentSnapshotFailure failure)
    {
        stagedStore = null;
        failure = P12FCommitmentSnapshotFailure.InvalidSnapshot;
        if (snapshot == null || snapshot.SchemaVersion != CurrentSchemaVersion || snapshot.Revision < 0
            || snapshot.Parties == null || stagedNpcs == null || detachedNpcRows == null || stagedIdentities == null)
            return false;
        if (!P12FCommitmentSnapshotValidation.TryBuildNpcRows(stagedNpcs, detachedNpcRows,
                out Dictionary<string, NpcRuntime> npcs, out Dictionary<string, P12DNpcFRow> rows))
        {
            failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
            return false;
        }
        HashSet<string> partyIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12FTravelPartyOwnerSnapshotRecord record in snapshot.Parties)
        {
            if (!IsValidRecord(record) || !partyIds.Add(record.TravelPartyId)
                || !stagedIdentities.TryGetLocation(record.OriginLocationRuntimeId, out _)
                || !stagedIdentities.TryGetLocation(record.DestinationLocationRuntimeId, out _)
                || !stagedIdentities.TryGetRoute(record.RouteRuntimeId, out SpatialRouteRuntime route)
                || !string.Equals(route.Origin.RuntimeId, record.OriginLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(route.Destination.RuntimeId, record.DestinationLocationRuntimeId, StringComparison.Ordinal))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding;
                return false;
            }
            foreach (string memberId in record.MemberRuntimeIds)
            {
                if (!memberIds.Add(memberId) || !npcs.ContainsKey(memberId)
                    || !rows.TryGetValue(memberId, out P12DNpcFRow row)
                    || !string.Equals(row.ActiveTravelPartyId, record.TravelPartyId, StringComparison.Ordinal)
                    || !string.Equals(npcs[memberId].ActiveTravelPartyId, record.TravelPartyId, StringComparison.Ordinal))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                    return false;
                }
            }
        }
        foreach (P12DNpcFRow row in detachedNpcRows)
        {
            if (string.IsNullOrWhiteSpace(row.ActiveTravelPartyId)) continue;
            if (!partyIds.Contains(row.ActiveTravelPartyId)
                || !ContainsMember(snapshot, row.ActiveTravelPartyId, row.RuntimeId))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                return false;
            }
        }
        if (!TravelPartyStore.TryCreateFromOwnerSnapshot(snapshot, out stagedStore) || stagedStore == null)
        {
            failure = P12FCommitmentSnapshotFailure.StageFailed;
            return false;
        }
        failure = P12FCommitmentSnapshotFailure.None;
        return true;
    }

    private static bool ContainsMember(P12FTravelPartyOwnerSnapshot snapshot, string partyId, string npcId)
    {
        foreach (P12FTravelPartyOwnerSnapshotRecord party in snapshot.Parties)
            if (string.Equals(party.TravelPartyId, partyId, StringComparison.Ordinal))
                foreach (string memberId in party.MemberRuntimeIds)
                    if (string.Equals(memberId, npcId, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool IsValid(TravelPartyRuntime party)
    {
        if (party == null || !party.IsActive || string.IsNullOrWhiteSpace(party.TravelPartyId)
            || string.IsNullOrWhiteSpace(party.OriginLocationRuntimeId)
            || string.IsNullOrWhiteSpace(party.DestinationLocationRuntimeId)
            || string.IsNullOrWhiteSpace(party.RouteRuntimeId) || party.TravelDaysTotal <= 0
            || party.MemberRuntimeIds == null || party.MemberRuntimeIds.Count == 0
            || party.MemberCosts == null || party.MemberCosts.Count != party.MemberRuntimeIds.Count) return false;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in party.MemberRuntimeIds) if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) return false;
        HashSet<string> costIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (TravelPartyMemberCost cost in party.MemberCosts)
            if (cost == null || !ids.Contains(cost.RuntimeId) || !costIds.Add(cost.RuntimeId)
                || cost.Amount < 0f || float.IsNaN(cost.Amount) || float.IsInfinity(cost.Amount)) return false;
        return costIds.Count == ids.Count;
    }

    private static bool IsValidRecord(P12FTravelPartyOwnerSnapshotRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.TravelPartyId)
            || string.IsNullOrWhiteSpace(record.OriginLocationRuntimeId)
            || string.IsNullOrWhiteSpace(record.DestinationLocationRuntimeId)
            || string.IsNullOrWhiteSpace(record.RouteRuntimeId) || record.TravelDaysTotal <= 0
            || record.MemberRuntimeIds == null || record.MemberCosts == null
            || record.MemberRuntimeIds.Count == 0 || record.MemberCosts.Count != record.MemberRuntimeIds.Count) return false;
        HashSet<string> members = new HashSet<string>(record.MemberRuntimeIds, StringComparer.Ordinal);
        HashSet<string> costs = new HashSet<string>(StringComparer.Ordinal);
        if (members.Count != record.MemberRuntimeIds.Count) return false;
        foreach (TravelPartyMemberCost cost in record.MemberCosts)
            if (cost == null || !members.Contains(cost.RuntimeId) || !costs.Add(cost.RuntimeId)
                || cost.Amount < 0f || float.IsNaN(cost.Amount) || float.IsInfinity(cost.Amount)) return false;
        return costs.Count == members.Count;
    }
}

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
    internal P12FExpeditionOwnerSnapshot(int schemaVersion, long revision, IEnumerable<P12FExpeditionOwnerSnapshotRecord> expeditions)
    {
        SchemaVersion = schemaVersion; Revision = revision;
        Expeditions = new ReadOnlyCollection<P12FExpeditionOwnerSnapshotRecord>(new List<P12FExpeditionOwnerSnapshotRecord>(expeditions));
    }

    internal static bool TryCapture(ExpeditionStore source, DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        out P12FExpeditionOwnerSnapshot snapshot, out P12FCommitmentSnapshotFailure failure)
    {
        snapshot = null;
        failure = P12FCommitmentSnapshotFailure.InvalidCaptureContext;
        if (source == null || !P12FCommitmentSnapshotValidation.HasExactContext(token, exactOwnerSectionVector)) return false;
        using (source.EnterReadWindow())
        {
            if (!P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    ExpeditionCensusProvider.SectionId, ExpeditionCensusProvider.SchemaVersion,
                    source, out int expectedCount, out long expectedRevision))
            { failure = P12FCommitmentSnapshotFailure.InvalidOwnerSection; return false; }
            if (!source.ValidateCensus(out int count, out long revision)
                || count != expectedCount || revision != expectedRevision
                || !source.TryCaptureOwnerSnapshot(out snapshot) || snapshot == null
                || snapshot.Expeditions.Count != expectedCount || snapshot.Revision != expectedRevision)
            { snapshot = null; failure = P12FCommitmentSnapshotFailure.InvalidOwner; return false; }
            if (!source.ValidateCensus(out int afterCount, out long afterRevision)
                || afterCount != expectedCount || afterRevision != expectedRevision
                || !P12FCommitmentSnapshotValidation.MatchesSection(token, exactOwnerSectionVector,
                    ExpeditionCensusProvider.SectionId, ExpeditionCensusProvider.SchemaVersion,
                    source, out int finalCount, out long finalRevision)
                || finalCount != afterCount || finalRevision != afterRevision)
            { snapshot = null; failure = P12FCommitmentSnapshotFailure.StaleCapture; return false; }
            failure = P12FCommitmentSnapshotFailure.None;
            return true;
        }
    }

    internal static bool TryStage(P12FExpeditionOwnerSnapshot snapshot,
        IReadOnlyList<NpcRuntime> stagedNpcs, IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        RuntimeIdentityRegistry stagedIdentities, TravelPartyStore stagedTravelParties,
        out ExpeditionStore stagedStore, out P12FCommitmentSnapshotFailure failure)
    {
        stagedStore = null; failure = P12FCommitmentSnapshotFailure.InvalidSnapshot;
        if (snapshot == null || snapshot.SchemaVersion != CurrentSchemaVersion || snapshot.Revision < 0
            || snapshot.Expeditions == null || stagedIdentities == null || stagedTravelParties == null
            || !P12FCommitmentSnapshotValidation.TryBuildNpcRows(stagedNpcs, detachedNpcRows,
                out Dictionary<string, NpcRuntime> npcs, out Dictionary<string, P12DNpcFRow> rows))
        { failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding; return false; }
        HashSet<string> expeditionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12FExpeditionOwnerSnapshotRecord record in snapshot.Expeditions)
        {
            if (!IsValid(record) || !expeditionIds.Add(record.ExpeditionId)
                || !stagedIdentities.TryGetExplorableSite(record.TargetSiteRuntimeId, out ExplorableSiteRuntime site)
                || !stagedIdentities.TryGetLocation(record.OriginLocationRuntimeId, out _)
                || !stagedIdentities.TryGetLocation(record.TargetLocationRuntimeId, out _)
                || !stagedIdentities.TryGetRoute(record.OutboundRouteRuntimeId, out SpatialRouteRuntime route)
                || !string.Equals(site.Location.RuntimeId, record.TargetLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(route.Origin.RuntimeId, record.OriginLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(route.Destination.RuntimeId, record.TargetLocationRuntimeId, StringComparison.Ordinal))
            { failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding; return false; }
            foreach (string member in record.MemberRuntimeIds)
                if (!npcs.ContainsKey(member) || !rows.ContainsKey(member))
                { failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding; return false; }
            foreach (string reference in record.VisitedLocalPlaceRuntimeIds)
                if (!stagedIdentities.TryGetLocalPlace(reference, out _))
                { failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding; return false; }
            foreach (string reference in record.ObservedLocalConnectionRuntimeIds)
                if (!stagedIdentities.TryGetLocalConnection(reference, out _))
                { failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding; return false; }
            if (record.CurrentLocalPlaceRuntimeId != null
                && !stagedIdentities.TryGetLocalPlace(record.CurrentLocalPlaceRuntimeId, out _))
            { failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding; return false; }
            if (!string.IsNullOrWhiteSpace(record.TravelPartyId)
                && (record.State == ExpeditionState.TravelingToSite || record.State == ExpeditionState.Returning))
            {
                TravelPartyRuntime party = stagedTravelParties.GetById(record.TravelPartyId);
                if (party == null || !SameMembers(record.MemberRuntimeIds, party.MemberRuntimeIds))
                { failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding; return false; }
                foreach (string member in record.MemberRuntimeIds)
                    if (!rows.TryGetValue(member, out P12DNpcFRow row)
                        || !string.Equals(row.ActiveTravelPartyId, record.TravelPartyId, StringComparison.Ordinal)
                        || !string.Equals(npcs[member].ActiveTravelPartyId, record.TravelPartyId, StringComparison.Ordinal))
                    { failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding; return false; }
            }
        }
        if (!ExpeditionStore.TryCreateFromOwnerSnapshot(snapshot, out stagedStore) || stagedStore == null)
        { failure = P12FCommitmentSnapshotFailure.StageFailed; return false; }
        failure = P12FCommitmentSnapshotFailure.None; return true;
    }

    private static bool IsValid(P12FExpeditionOwnerSnapshotRecord value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ExpeditionId)
            || string.IsNullOrWhiteSpace(value.TargetSiteRuntimeId)
            || string.IsNullOrWhiteSpace(value.OriginLocationRuntimeId)
            || string.IsNullOrWhiteSpace(value.TargetLocationRuntimeId)
            || string.IsNullOrWhiteSpace(value.OutboundRouteRuntimeId)
            || !Enum.IsDefined(typeof(ExpeditionState), value.State)
            || !Enum.IsDefined(typeof(ExpeditionObjectiveType), value.ObjectiveType)
            || value.MemberRuntimeIds == null || value.PerformerRuntimeIds == null || value.SupportRuntimeIds == null
            || value.VisitedLocalPlaceRuntimeIds == null || value.ObservedLocalConnectionRuntimeIds == null
            || value.RequiredProgress <= 0 || value.Progress < 0 || value.Progress > value.RequiredProgress
            || (value.ObjectiveCompleted != (value.Progress == value.RequiredProgress))) return false;
        HashSet<string> members = new HashSet<string>(value.MemberRuntimeIds, StringComparer.Ordinal);
        if (members.Count != value.MemberRuntimeIds.Count || members.Count == 0) return false;
        HashSet<string> roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in value.PerformerRuntimeIds) if (!members.Contains(id) || !roles.Add(id)) return false;
        foreach (string id in value.SupportRuntimeIds) if (!members.Contains(id) || !roles.Add(id)) return false;
        return roles.Count == members.Count;
    }

    private static bool SameMembers(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        HashSet<string> rightIds = new HashSet<string>(right, StringComparer.Ordinal);
        foreach (string value in left) if (!rightIds.Contains(value)) return false;
        return true;
    }
}

internal static class P12FCommitmentSnapshotValidation
{
    internal static bool HasExactContext(DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector) => token != null && vector != null
        && ReferenceEquals(token.OwnerSections, vector)
        && token.AdmissionContext != null
        && token.AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
        && token.CompletedCoreSequence > 0 && token.AbsoluteDay >= 0 && token.MutationEpoch >= 0;

    internal static bool MatchesSection(DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> vector, string sectionId, int schemaVersion,
        object owner, out int cardinality, out long revision)
    {
        cardinality = 0; revision = 0;
        if (!HasExactContext(token, vector) || string.IsNullOrWhiteSpace(sectionId) || owner == null) return false;
        OwnerSectionCensusSnapshot match = null;
        foreach (OwnerSectionCensusSnapshot section in vector)
            if (section != null && string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
            {
                if (match != null) return false;
                match = section;
            }
        if (match == null || match.SchemaVersion != schemaVersion || match.Role != OwnerSectionRole.Required
            || !ReferenceEquals(match.OwnerInstanceIdentity, owner) || match.Cardinality < 0 || match.Revision < 0) return false;
        cardinality = match.Cardinality; revision = match.Revision; return true;
    }

    internal static bool TryBuildNpcRows(IReadOnlyList<NpcRuntime> npcs,
        IReadOnlyList<P12DNpcFRow> rows, out Dictionary<string, NpcRuntime> npcsById,
        out Dictionary<string, P12DNpcFRow> rowsById)
    {
        npcsById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        rowsById = new Dictionary<string, P12DNpcFRow>(StringComparer.Ordinal);
        if (npcs == null || rows == null || npcs.Count != rows.Count) return false;
        foreach (NpcRuntime npc in npcs)
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) || !npcsById.TryAdd(npc.RuntimeId, npc)) return false;
        foreach (P12DNpcFRow row in rows)
            if (row == null || string.IsNullOrWhiteSpace(row.RuntimeId) || !rowsById.TryAdd(row.RuntimeId, row)) return false;
        if (npcsById.Count != rowsById.Count) return false;
        foreach (KeyValuePair<string, NpcRuntime> pair in npcsById)
            if (!rowsById.ContainsKey(pair.Key)) return false;
        return true;
    }
}
