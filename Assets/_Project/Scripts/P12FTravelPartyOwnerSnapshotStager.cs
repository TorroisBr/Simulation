using System;
using System.Collections.Generic;

/// <summary>
/// Unity-host restore adapter for validating the Core TravelParty snapshot
/// against staged Unity NPC projections and the staged spatial identity graph.
/// </summary>
internal static class P12FTravelPartyOwnerSnapshotStager
{
    internal static bool TryStage(P12FTravelPartyOwnerSnapshot snapshot,
        IReadOnlyList<NpcRuntime> stagedNpcs, IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        RuntimeIdentityRegistry stagedIdentities, out TravelPartyStore stagedStore,
        out P12FCommitmentSnapshotFailure failure)
    {
        stagedStore = null;
        failure = P12FCommitmentSnapshotFailure.InvalidSnapshot;
        if (snapshot == null || snapshot.SchemaVersion != P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0 || snapshot.Parties == null
            || stagedNpcs == null || detachedNpcRows == null || stagedIdentities == null)
            return false;
        if (!P12FUnityCommitmentSnapshotValidation.TryBuildNpcRows(stagedNpcs, detachedNpcRows,
                out Dictionary<string, NpcRuntime> npcs, out Dictionary<string, P12DNpcFRow> rows))
        {
            failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
            return false;
        }

        HashSet<string> partyIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12FTravelPartyOwnerSnapshotRecord record in snapshot.Parties)
        {
            if (!P12FTravelPartyOwnerSnapshot.IsValidRecord(record) || !partyIds.Add(record.TravelPartyId)
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

        return P12FTravelPartyOwnerSnapshot.TryCreateStoreFromOwnerSnapshot(
            snapshot, out stagedStore, out failure);
    }

    private static bool ContainsMember(P12FTravelPartyOwnerSnapshot snapshot, string partyId, string npcId)
    {
        foreach (P12FTravelPartyOwnerSnapshotRecord party in snapshot.Parties)
            if (string.Equals(party.TravelPartyId, partyId, StringComparison.Ordinal))
                foreach (string memberId in party.MemberRuntimeIds)
                    if (string.Equals(memberId, npcId, StringComparison.Ordinal)) return true;
        return false;
    }
}

internal static class P12FUnityCommitmentSnapshotValidation
{
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
