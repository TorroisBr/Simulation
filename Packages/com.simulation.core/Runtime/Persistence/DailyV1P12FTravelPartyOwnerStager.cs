using System;
using System.Collections.Generic;

internal static class DailyV1P12FTravelPartyOwnerStager
{
    /// <summary>
    /// Validates detached TravelParty values against selected Core NPC owners,
    /// the complete P12-D F-row vector, and Core spatial topology. The F-row
    /// vector may include Unity-compatibility NPCs without selected Core owners.
    /// </summary>
    internal static bool TryStage(
        P12FTravelPartyOwnerSnapshot snapshot,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedNpcOwners,
        IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        SpatialNetworkOwner spatialNetwork,
        out TravelPartyStore stagedStore,
        out P12FCommitmentSnapshotFailure failure)
    {
        stagedStore = null;
        failure = P12FCommitmentSnapshotFailure.InvalidSnapshot;
        if (snapshot == null
            || snapshot.SchemaVersion != P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0L
            || snapshot.Parties == null
            || stagedNpcOwners == null
            || detachedNpcRows == null
            || spatialNetwork == null)
            return false;

        Dictionary<string, DailyV1NpcRuntimeOwners> ownersById =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
        Dictionary<string, P12DNpcFRow> rowsById =
            new Dictionary<string, P12DNpcFRow>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners owner in stagedNpcOwners)
        {
            if (owner == null
                || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Travel == null
                || !string.Equals(owner.Travel.NpcRuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                || !ownersById.TryAdd(owner.RuntimeId, owner))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                return false;
            }
        }
        foreach (P12DNpcFRow row in detachedNpcRows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.RuntimeId)
                || !rowsById.TryAdd(row.RuntimeId, row))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                return false;
            }
        }

        foreach (KeyValuePair<string, DailyV1NpcRuntimeOwners> pair in ownersById)
        {
            if (!rowsById.TryGetValue(pair.Key, out P12DNpcFRow row)
                || !string.Equals(pair.Value.Travel.ActiveTravelPartyId,
                    row.ActiveTravelPartyId, StringComparison.Ordinal))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                return false;
            }
        }

        HashSet<string> partyIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12FTravelPartyOwnerSnapshotRecord record in snapshot.Parties)
        {
            if (!P12FTravelPartyOwnerSnapshot.IsValidRecord(record)
                || !partyIds.Add(record.TravelPartyId)
                || !spatialNetwork.TryGetLocation(record.OriginLocationRuntimeId, out _)
                || !spatialNetwork.TryGetLocation(record.DestinationLocationRuntimeId, out _)
                || !spatialNetwork.TryGetRoute(record.RouteRuntimeId, out SpatialRouteRuntime route)
                || !string.Equals(route.Origin.RuntimeId,
                    record.OriginLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(route.Destination.RuntimeId,
                    record.DestinationLocationRuntimeId, StringComparison.Ordinal))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding;
                return false;
            }

            foreach (string memberId in record.MemberRuntimeIds)
            {
                if (!memberIds.Add(memberId)
                    || !rowsById.TryGetValue(memberId, out P12DNpcFRow row)
                    || !string.Equals(row.ActiveTravelPartyId,
                        record.TravelPartyId, StringComparison.Ordinal)
                    || (ownersById.TryGetValue(memberId, out DailyV1NpcRuntimeOwners owner)
                        && !string.Equals(owner.Travel.ActiveTravelPartyId,
                            record.TravelPartyId, StringComparison.Ordinal)))
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

    private static bool ContainsMember(P12FTravelPartyOwnerSnapshot snapshot,
        string partyId, string npcId)
    {
        foreach (P12FTravelPartyOwnerSnapshotRecord party in snapshot.Parties)
        {
            if (!string.Equals(party.TravelPartyId, partyId, StringComparison.Ordinal)) continue;
            foreach (string memberId in party.MemberRuntimeIds)
                if (string.Equals(memberId, npcId, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
