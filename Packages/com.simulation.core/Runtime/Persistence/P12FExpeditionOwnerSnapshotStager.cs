using System;
using System.Collections.Generic;

internal static class P12FExpeditionOwnerSnapshotStager
{
    internal static bool TryStage(
        P12FExpeditionOwnerSnapshot snapshot,
        IReadOnlyList<string> stagedNpcRuntimeIds,
        IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedSelectedNpcOwners,
        DailyV1RuntimeIdentityIndex stagedIdentities,
        SpatialNetworkOwner stagedSpatialNetwork,
        ExplorableSiteStore stagedExplorableSites,
        TravelPartyStore stagedTravelParties,
        out ExpeditionStore stagedStore,
        out P12FCommitmentSnapshotFailure failure)
    {
        stagedStore = null;
        failure = P12FCommitmentSnapshotFailure.InvalidSnapshot;
        if (snapshot == null
            || snapshot.SchemaVersion != P12FExpeditionOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0L
            || snapshot.Expeditions == null
            || stagedIdentities == null
            || stagedSpatialNetwork == null
            || stagedExplorableSites == null
            || stagedTravelParties == null
            || !TryBuildNpcRows(stagedNpcRuntimeIds, detachedNpcRows, stagedSelectedNpcOwners,
                out HashSet<string> stagedNpcIds, out Dictionary<string, P12DNpcFRow> rowsById))
        {
            failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
            return false;
        }

        HashSet<string> expeditionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12FExpeditionOwnerSnapshotRecord record in snapshot.Expeditions)
        {
            if (!P12FExpeditionOwnerSnapshot.IsValidRecord(record)
                || !expeditionIds.Add(record.ExpeditionId)
                || !stagedIdentities.HasIdentity(record.TargetSiteRuntimeId, DailyV1RuntimeIdentityKind.ExplorableSite)
                || !stagedExplorableSites.TryGetByRuntimeId(record.TargetSiteRuntimeId, out ExplorableSiteRuntime site)
                || !stagedIdentities.HasIdentity(record.OriginLocationRuntimeId, DailyV1RuntimeIdentityKind.Location)
                || !stagedSpatialNetwork.TryGetLocation(record.OriginLocationRuntimeId, out _)
                || !stagedIdentities.HasIdentity(record.TargetLocationRuntimeId, DailyV1RuntimeIdentityKind.Location)
                || !stagedSpatialNetwork.TryGetLocation(record.TargetLocationRuntimeId, out _)
                || !stagedIdentities.HasIdentity(record.OutboundRouteRuntimeId, DailyV1RuntimeIdentityKind.Route)
                || !stagedSpatialNetwork.TryGetRoute(record.OutboundRouteRuntimeId, out SpatialRouteRuntime route)
                || !string.Equals(site.Location.RuntimeId, record.TargetLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(route.Origin.RuntimeId, record.OriginLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(route.Destination.RuntimeId, record.TargetLocationRuntimeId, StringComparison.Ordinal))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding;
                return false;
            }

            foreach (string member in record.MemberRuntimeIds)
            {
                if (!stagedNpcIds.Contains(member) || !rowsById.ContainsKey(member))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                    return false;
                }
            }

            foreach (string reference in record.VisitedLocalPlaceRuntimeIds)
            {
                if (!stagedIdentities.HasIdentity(reference, DailyV1RuntimeIdentityKind.LocalPlace))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding;
                    return false;
                }
            }

            foreach (string reference in record.ObservedLocalConnectionRuntimeIds)
            {
                if (!stagedIdentities.HasIdentity(reference, DailyV1RuntimeIdentityKind.LocalConnection))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding;
                    return false;
                }
            }

            if (record.CurrentLocalPlaceRuntimeId != null
                && !stagedIdentities.HasIdentity(record.CurrentLocalPlaceRuntimeId, DailyV1RuntimeIdentityKind.LocalPlace))
            {
                failure = P12FCommitmentSnapshotFailure.InvalidIdentityBinding;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(record.TravelPartyId)
                && (record.State == ExpeditionState.TravelingToSite || record.State == ExpeditionState.Returning))
            {
                TravelPartyRuntime party = stagedTravelParties.GetById(record.TravelPartyId);
                if (party == null || !SameMembers(record.MemberRuntimeIds, party.MemberRuntimeIds))
                {
                    failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                    return false;
                }

                foreach (string member in record.MemberRuntimeIds)
                {
                    if (!rowsById.TryGetValue(member, out P12DNpcFRow row)
                        || !string.Equals(row.ActiveTravelPartyId, record.TravelPartyId, StringComparison.Ordinal))
                    {
                        failure = P12FCommitmentSnapshotFailure.InvalidNpcBinding;
                        return false;
                    }
                }
            }
        }

        if (!ExpeditionStore.TryCreateFromOwnerSnapshot(snapshot, out stagedStore) || stagedStore == null)
        {
            failure = P12FCommitmentSnapshotFailure.StageFailed;
            return false;
        }

        failure = P12FCommitmentSnapshotFailure.None;
        return true;
    }

    private static bool TryBuildNpcRows(
        IReadOnlyList<string> stagedNpcRuntimeIds,
        IReadOnlyList<P12DNpcFRow> detachedNpcRows,
        IReadOnlyList<DailyV1NpcRuntimeOwners> stagedSelectedNpcOwners,
        out HashSet<string> stagedNpcIds,
        out Dictionary<string, P12DNpcFRow> rowsById)
    {
        stagedNpcIds = new HashSet<string>(StringComparer.Ordinal);
        rowsById = new Dictionary<string, P12DNpcFRow>(StringComparer.Ordinal);
        if (stagedNpcRuntimeIds == null || detachedNpcRows == null || stagedSelectedNpcOwners == null
            || stagedNpcRuntimeIds.Count != detachedNpcRows.Count)
            return false;

        foreach (string runtimeId in stagedNpcRuntimeIds)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || !stagedNpcIds.Add(runtimeId))
                return false;
        }

        foreach (P12DNpcFRow row in detachedNpcRows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.RuntimeId)
                || !rowsById.TryAdd(row.RuntimeId, row))
                return false;
        }

        if (stagedNpcIds.Count != rowsById.Count)
            return false;
        foreach (string runtimeId in stagedNpcIds)
        {
            if (!rowsById.ContainsKey(runtimeId))
                return false;
        }

        Dictionary<string, DailyV1NpcRuntimeOwners> ownersById =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners owner in stagedSelectedNpcOwners)
        {
            if (owner == null
                || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Travel == null
                || !stagedNpcIds.Contains(owner.RuntimeId)
                || !string.Equals(owner.Travel.NpcRuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                || !ownersById.TryAdd(owner.RuntimeId, owner)
                || !rowsById.TryGetValue(owner.RuntimeId, out P12DNpcFRow row)
                || !string.Equals(owner.Travel.ActiveTravelPartyId, row.ActiveTravelPartyId, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool SameMembers(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left == null || right == null || left.Count != right.Count)
            return false;

        HashSet<string> rightIds = new HashSet<string>(right, StringComparer.Ordinal);
        foreach (string value in left)
        {
            if (!rightIds.Contains(value))
                return false;
        }

        return true;
    }
}
