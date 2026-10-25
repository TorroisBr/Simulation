using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal static class P12FExpeditionOwnerSnapshotStager
{
    internal static bool TryStage(P12FExpeditionOwnerSnapshot snapshot,
        IReadOnlyList<NpcRuntime> stagedNpcs, IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        RuntimeIdentityRegistry stagedIdentities, TravelPartyStore stagedTravelParties,
        out ExpeditionStore stagedStore, out P12FCommitmentSnapshotFailure failure)
    {
        stagedStore = null; failure = P12FCommitmentSnapshotFailure.InvalidSnapshot;
        if (snapshot == null || snapshot.SchemaVersion != P12FExpeditionOwnerSnapshot.CurrentSchemaVersion || snapshot.Revision < 0
            || snapshot.Expeditions == null || stagedIdentities == null || stagedTravelParties == null
            || !P12FUnityCommitmentSnapshotValidation.TryBuildNpcRows(stagedNpcs, detachedNpcRows,
                out Dictionary<string, NpcRuntime> npcs, out Dictionary<string, P12DNpcFRow> rows))
        { failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding; return false; }
        HashSet<string> expeditionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12FExpeditionOwnerSnapshotRecord record in snapshot.Expeditions)
        {
            if (!P12FExpeditionOwnerSnapshot.IsValidRecord(record) || !expeditionIds.Add(record.ExpeditionId)
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

    private static bool SameMembers(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        HashSet<string> rightIds = new HashSet<string>(right, StringComparer.Ordinal);
        foreach (string value in left) if (!rightIds.Contains(value)) return false;
        return true;
    }
}
