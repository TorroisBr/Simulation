using System;
using System.Collections.Generic;

/// <summary>
/// Constructs the Core-owned legacy spatial authority from a detached P12-D
/// snapshot. Host identity indexes are populated by the host after this owner
/// has been staged successfully.
/// </summary>
internal static class P12DSpatialNetworkOwnerHydrator
{
    private static readonly Func<bool> IdentityInstallAccepted = () => true;

    internal static bool TryCreateOwner(
        SpatialNetworkOwnerSnapshot snapshot,
        out SpatialNetworkOwner stagedOwner,
        out SpatialNetworkSnapshotFailure failure)
    {
        stagedOwner = null;
        if (snapshot == null)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "A spatial owner snapshot is required.");
            return false;
        }

        if (snapshot.SchemaVersion != SpatialNetworkOwnerSnapshot.CurrentSchemaVersion)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.UnsupportedSnapshotSchema,
                "The spatial owner snapshot schema is not supported.");
            return false;
        }

        if (snapshot.LocationRuntimeIds == null || snapshot.Routes == null || snapshot.Revision < 0)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "The spatial owner snapshot is missing required collections or has a negative revision.");
            return false;
        }

        long minimumRevision = (long)snapshot.LocationRuntimeIds.Count + snapshot.Routes.Count;
        if (snapshot.Revision < minimumRevision)
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "The spatial owner revision cannot account for its registered locations and routes.");
            return false;
        }

        HashSet<string> allRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, SpatialLocationRuntime> stagedLocations =
            new Dictionary<string, SpatialLocationRuntime>(StringComparer.Ordinal);

        for (int index = 0; index < snapshot.LocationRuntimeIds.Count; index++)
        {
            string locationId = snapshot.LocationRuntimeIds[index];
            if (string.IsNullOrWhiteSpace(locationId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.InvalidLocationId,
                    "The spatial owner snapshot contains an empty location identity.");
                return false;
            }

            if (!allRuntimeIds.Add(locationId) || !locationIds.Add(locationId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.DuplicateRuntimeId,
                    "The spatial owner snapshot contains a duplicate runtime identity.");
                return false;
            }
        }

        for (int index = 0; index < snapshot.Routes.Count; index++)
        {
            SpatialRouteOwnerSnapshotRecord route = snapshot.Routes[index];
            if (route == null
                || string.IsNullOrWhiteSpace(route.RuntimeId)
                || string.IsNullOrWhiteSpace(route.OriginLocationRuntimeId)
                || string.IsNullOrWhiteSpace(route.DestinationLocationRuntimeId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.InvalidRoute,
                    "The spatial owner snapshot contains a null route or empty route identity/reference.");
                return false;
            }

            if (!allRuntimeIds.Add(route.RuntimeId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.DuplicateRuntimeId,
                    "The spatial owner snapshot contains a duplicate runtime identity.");
                return false;
            }

            if (!locationIds.Contains(route.OriginLocationRuntimeId)
                || !locationIds.Contains(route.DestinationLocationRuntimeId))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.MissingRouteEndpoint,
                    "Every legacy route endpoint must refer to a registered snapshot location.");
                return false;
            }

            if (string.Equals(route.OriginLocationRuntimeId,
                route.DestinationLocationRuntimeId, StringComparison.Ordinal))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.SelfRoute,
                    "A legacy route cannot connect a location to itself.");
                return false;
            }

            if (route.TravelDays < 1)
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.InvalidTravelDays,
                    "A legacy route must preserve a positive normalized travel duration.");
                return false;
            }
        }

        SpatialNetworkOwner staged = new SpatialNetworkOwner();
        foreach (string locationId in snapshot.LocationRuntimeIds)
        {
            SpatialLocationRuntime location = new SpatialLocationRuntime(locationId);
            if (!staged.TryInstallRestoredLocation(location, IdentityInstallAccepted))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The Core spatial owner rejected a validated legacy location identity.");
                return false;
            }
            stagedLocations.Add(locationId, location);
        }

        foreach (SpatialRouteOwnerSnapshotRecord routeRecord in snapshot.Routes)
        {
            SpatialRouteRuntime route = new SpatialRouteRuntime(
                routeRecord.RuntimeId,
                stagedLocations[routeRecord.OriginLocationRuntimeId],
                stagedLocations[routeRecord.DestinationLocationRuntimeId],
                routeRecord.TravelDays);
            if (!staged.TryInstallRestoredRoute(route, IdentityInstallAccepted))
            {
                failure = SpatialNetworkSnapshotFailure.Create(
                    SpatialNetworkSnapshotFailureCode.RuntimeIdentityCollision,
                    "The Core spatial owner rejected a validated legacy route identity.");
                return false;
            }
        }

        if (!staged.TryInstallRestoredRevision(snapshot.Revision))
        {
            failure = SpatialNetworkSnapshotFailure.Create(
                SpatialNetworkSnapshotFailureCode.InvalidSnapshot,
                "The spatial owner snapshot revision is below its staged registration count.");
            return false;
        }

        stagedOwner = staged;
        failure = SpatialNetworkSnapshotFailure.None;
        return true;
    }
}
