using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

internal enum P12CSpatialAuthoritySnapshotFailureCode
{
    None = 0,
    UnsupportedSnapshotSchema = 1,
    UnsupportedAdmissionProfile = 2,
    UnsupportedGenesisProfile = 3,
    InvalidOwner = 4,
    ExcludedFactsPresent = 5,
    InvalidSnapshot = 6,
    StageCompositionFailed = 7
}

internal sealed class P12CSpatialAuthoritySnapshotFailure
{
    private static readonly P12CSpatialAuthoritySnapshotFailure none =
        new P12CSpatialAuthoritySnapshotFailure(P12CSpatialAuthoritySnapshotFailureCode.None, string.Empty);

    private P12CSpatialAuthoritySnapshotFailure(P12CSpatialAuthoritySnapshotFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static P12CSpatialAuthoritySnapshotFailure None => none;
    internal P12CSpatialAuthoritySnapshotFailureCode Code { get; }
    internal string Message { get; }
    internal bool IsFailure => Code != P12CSpatialAuthoritySnapshotFailureCode.None;

    internal static P12CSpatialAuthoritySnapshotFailure Create(
        P12CSpatialAuthoritySnapshotFailureCode code,
        string message)
    {
        return code == P12CSpatialAuthoritySnapshotFailureCode.None
            ? None
            : new P12CSpatialAuthoritySnapshotFailure(code, message);
    }

    public override string ToString() => Code
        + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

internal sealed class P12CSpatialHexSnapshot
{
    internal P12CSpatialHexSnapshot(string id, int q, int r, string terrainDefinitionId, string authoredRevisionToken)
    {
        Id = id;
        Q = q;
        R = r;
        TerrainDefinitionId = terrainDefinitionId;
        AuthoredRevisionToken = authoredRevisionToken;
    }

    internal string Id { get; }
    internal int Q { get; }
    internal int R { get; }
    internal string TerrainDefinitionId { get; }
    internal string AuthoredRevisionToken { get; }
}

internal sealed class P12CSpatialLocationSnapshot
{
    internal P12CSpatialLocationSnapshot(string id, string anchorHexId)
    {
        Id = id;
        AnchorHexId = anchorHexId;
    }

    internal string Id { get; }
    internal string AnchorHexId { get; }
}

internal sealed class P12CSpatialScaleSnapshot
{
    internal P12CSpatialScaleSnapshot(
        string resolvedConventionId,
        string sourceIdentity,
        string sourceVersion,
        decimal distancePerNeighborStep,
        string unit)
    {
        ResolvedConventionId = resolvedConventionId;
        SourceIdentity = sourceIdentity;
        SourceVersion = sourceVersion;
        DistancePerNeighborStep = distancePerNeighborStep;
        Unit = unit;
    }

    internal string ResolvedConventionId { get; }
    internal string SourceIdentity { get; }
    internal string SourceVersion { get; }
    internal decimal DistancePerNeighborStep { get; }
    internal string Unit { get; }
}

/// <summary>
/// Detached P12-C snapshot for the selected P8-A geography owner. This is an
/// owner slice only; admission and completed-boundary evidence remain owned by
/// the P12-B coordinator.
/// </summary>
internal sealed class P12CSpatialAuthoritySnapshot
{
    internal const string CurrentSchemaId = "p12c-spatial-authority-geography";
    internal const int CurrentSchemaVersion = 1;

    private readonly ReadOnlyCollection<P12CSpatialHexSnapshot> hexes;
    private readonly ReadOnlyCollection<P12CSpatialLocationSnapshot> locations;

    internal P12CSpatialAuthoritySnapshot(
        string schemaId,
        int schemaVersion,
        SimulationRuntimeAdmissionProfile admissionProfile,
        string p9ProfileContractIdentity,
        int p9ProfileSchemaVersion,
        long ownerRevision,
        string coordinateConventionVersion,
        string coordinateCanonicalOrder,
        IEnumerable<P12CSpatialHexSnapshot> hexes,
        IEnumerable<P12CSpatialLocationSnapshot> locations,
        P12CSpatialScaleSnapshot scaleContext)
    {
        SchemaId = schemaId;
        SchemaVersion = schemaVersion;
        AdmissionProfile = admissionProfile;
        P9ProfileContractIdentity = p9ProfileContractIdentity;
        P9ProfileSchemaVersion = p9ProfileSchemaVersion;
        OwnerRevision = ownerRevision;
        CoordinateConventionVersion = coordinateConventionVersion;
        CoordinateCanonicalOrder = coordinateCanonicalOrder;
        this.hexes = Copy(hexes);
        this.locations = Copy(locations);
        ScaleContext = scaleContext;
    }

    internal string SchemaId { get; }
    internal int SchemaVersion { get; }
    internal SimulationRuntimeAdmissionProfile AdmissionProfile { get; }
    internal string P9ProfileContractIdentity { get; }
    internal int P9ProfileSchemaVersion { get; }
    internal long OwnerRevision { get; }
    internal string CoordinateConventionVersion { get; }
    internal string CoordinateCanonicalOrder { get; }
    internal IReadOnlyList<P12CSpatialHexSnapshot> Hexes => hexes;
    internal IReadOnlyList<P12CSpatialLocationSnapshot> Locations => locations;
    internal P12CSpatialScaleSnapshot ScaleContext { get; }

    internal static bool TryCapture(
        SimulationRuntimeAdmissionProfile admissionProfile,
        string p9ProfileContractIdentity,
        int p9ProfileSchemaVersion,
        SpatialAuthorityStore source,
        out P12CSpatialAuthoritySnapshot snapshot,
        out P12CSpatialAuthoritySnapshotFailure failure)
    {
        snapshot = null;
        failure = P12CSpatialAuthoritySnapshotFailure.None;

        if (admissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            return Fail(
                P12CSpatialAuthoritySnapshotFailureCode.UnsupportedAdmissionProfile,
                "P12-C P8-A geography capture requires the UnityBootstrapDailyV1 admission profile.",
                out failure);
        }

        if (!string.Equals(p9ProfileContractIdentity, SimulationGenesisPipeline.GeographyProfileContractIdentity, StringComparison.Ordinal)
            || p9ProfileSchemaVersion != 2)
        {
            return Fail(
                P12CSpatialAuthoritySnapshotFailureCode.UnsupportedGenesisProfile,
                "P12-C P8-A geography capture requires the P9-B authored-geography contract at schema 2.",
                out failure);
        }

        if (source == null)
        {
            return Fail(P12CSpatialAuthoritySnapshotFailureCode.InvalidOwner, "SpatialAuthorityStore is missing.", out failure);
        }

        if (HasExcludedFacts(source))
        {
            return Fail(
                P12CSpatialAuthoritySnapshotFailureCode.ExcludedFactsPresent,
                "The selected P8-A snapshot cannot omit crossings, passage/barrier facts, or local-topology bindings.",
                out failure);
        }

        SpatialAuthorityInvariantReport report = source.ValidateInvariants();
        if (report == null || !report.IsValid)
        {
            return Fail(
                P12CSpatialAuthoritySnapshotFailureCode.InvalidOwner,
                "SpatialAuthorityStore invariants are invalid: " + (report == null ? "missing report" : string.Join("; ", report.Violations)),
                out failure);
        }

        if (source.Revision != 1L || source.HexCount != 1 || source.LocationCount != 1 || !source.HasGeography
            || source.Hexes == null || source.Hexes.Count != 1
            || source.Locations == null || source.Locations.Count != 1
            || source.Crossings == null || source.Crossings.Count != 0
            || source.LocalTopologyBindings == null || source.LocalTopologyBindings.Count != 0)
        {
            return Fail(
                P12CSpatialAuthoritySnapshotFailureCode.InvalidOwner,
                "The selected profile requires one P8-A Hex, one anchored Location, scale context, and owner revision 1.",
                out failure);
        }

        HexRecord hex = source.Hexes[0];
        LocationRecord location = source.Locations[0];
        SpatialWorldScaleContext scale = source.ScaleContext;
        if (!IsValidHex(hex) || !IsValidLocation(location) || scale == null
            || !string.Equals(source.CoordinateConventionVersion, HexCoordinate.ConventionVersion, StringComparison.Ordinal)
            || !string.Equals(source.CoordinateCanonicalOrder, HexCoordinate.CanonicalOrder, StringComparison.Ordinal)
            || !string.Equals(location.AnchorHexId.Value, hex.Id.Value, StringComparison.Ordinal)
            || !IsValidScale(scale))
        {
            return Fail(
                P12CSpatialAuthoritySnapshotFailureCode.InvalidOwner,
                "The selected P8-A geography owner contains malformed identity, anchor, convention, terrain, or scale facts.",
                out failure);
        }

        snapshot = new P12CSpatialAuthoritySnapshot(
            CurrentSchemaId,
            CurrentSchemaVersion,
            admissionProfile,
            p9ProfileContractIdentity,
            p9ProfileSchemaVersion,
            source.Revision,
            source.CoordinateConventionVersion,
            source.CoordinateCanonicalOrder,
            new[]
            {
                new P12CSpatialHexSnapshot(
                    hex.Id.Value,
                    hex.Coordinate.Value.Q,
                    hex.Coordinate.Value.R,
                    hex.TerrainDefinitionId.Value,
                    hex.AuthoredRevisionToken)
            },
            new[] { new P12CSpatialLocationSnapshot(location.Id.Value, location.AnchorHexId.Value) },
            new P12CSpatialScaleSnapshot(
                scale.ResolvedConventionId,
                scale.SourceIdentity,
                scale.SourceVersion,
                scale.DistancePerNeighborStep,
                scale.Unit));
        return true;
    }

    internal static bool TryCreateStagedFromSnapshot(
        P12CSpatialAuthoritySnapshot snapshot,
        out SpatialAuthorityStore staged,
        out P12CSpatialAuthoritySnapshotFailure failure)
    {
        staged = null;
        failure = P12CSpatialAuthoritySnapshotFailure.None;
        if (snapshot == null)
        {
            return Fail(P12CSpatialAuthoritySnapshotFailureCode.InvalidSnapshot, "P8-A geography snapshot is null.", out failure);
        }

        if (!string.Equals(snapshot.SchemaId, CurrentSchemaId, StringComparison.Ordinal)
            || snapshot.SchemaVersion != CurrentSchemaVersion)
        {
            return Fail(P12CSpatialAuthoritySnapshotFailureCode.UnsupportedSnapshotSchema, "P8-A geography snapshot schema is unsupported.", out failure);
        }

        if (snapshot.AdmissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            return Fail(P12CSpatialAuthoritySnapshotFailureCode.UnsupportedAdmissionProfile, "P8-A geography snapshot is bound to an unsupported P12 admission profile.", out failure);
        }

        if (!string.Equals(snapshot.P9ProfileContractIdentity, SimulationGenesisPipeline.GeographyProfileContractIdentity, StringComparison.Ordinal)
            || snapshot.P9ProfileSchemaVersion != 2)
        {
            return Fail(P12CSpatialAuthoritySnapshotFailureCode.UnsupportedGenesisProfile, "P8-A geography snapshot is bound to an unsupported P9 genesis profile.", out failure);
        }

        if (snapshot.OwnerRevision != 1L
            || snapshot.Hexes == null || snapshot.Hexes.Count != 1
            || snapshot.Locations == null || snapshot.Locations.Count != 1
            || snapshot.ScaleContext == null
            || !string.Equals(snapshot.CoordinateConventionVersion, HexCoordinate.ConventionVersion, StringComparison.Ordinal)
            || !string.Equals(snapshot.CoordinateCanonicalOrder, HexCoordinate.CanonicalOrder, StringComparison.Ordinal))
        {
            return Fail(P12CSpatialAuthoritySnapshotFailureCode.InvalidSnapshot, "P8-A geography snapshot has invalid cardinality, owner revision, scale, or coordinate convention.", out failure);
        }

        P12CSpatialHexSnapshot hexFact = snapshot.Hexes[0];
        P12CSpatialLocationSnapshot locationFact = snapshot.Locations[0];
        P12CSpatialScaleSnapshot scaleFact = snapshot.ScaleContext;
        if (!IsValidHex(hexFact) || !IsValidLocation(locationFact) || !IsValidScale(scaleFact)
            || !string.Equals(locationFact.AnchorHexId, hexFact.Id, StringComparison.Ordinal))
        {
            return Fail(P12CSpatialAuthoritySnapshotFailureCode.InvalidSnapshot, "P8-A geography snapshot contains malformed IDs, terrain, anchor, or scale facts.", out failure);
        }

        try
        {
            var scale = new SpatialWorldScaleContext(
                scaleFact.ResolvedConventionId,
                scaleFact.SourceIdentity,
                scaleFact.SourceVersion,
                scaleFact.DistancePerNeighborStep,
                scaleFact.Unit);
            var geography = new SpatialGeographyDefinition(
                scale,
                new[]
                {
                    new HexRecord(
                        new HexId(hexFact.Id),
                        new HexCoordinate(hexFact.Q, hexFact.R),
                        new TerrainReference(new TerrainDefinitionId(hexFact.TerrainDefinitionId), hexFact.AuthoredRevisionToken))
                },
                new[] { new LocationRecord(new LocationId(locationFact.Id), new HexId(locationFact.AnchorHexId)) });
            var candidate = new SpatialAuthorityStore();
            if (!candidate.TryComposeGeography(geography, out SpatialAuthorityFailure compositionFailure))
            {
                return Fail(
                    P12CSpatialAuthoritySnapshotFailureCode.StageCompositionFailed,
                    "Private P8-A geography composition failed: " + compositionFailure,
                    out failure);
            }

            SpatialAuthorityInvariantReport report = candidate.ValidateInvariants();
            if (report == null || !report.IsValid || candidate.Revision != snapshot.OwnerRevision
                || candidate.HexCount != 1 || candidate.LocationCount != 1
                || candidate.Crossings.Count != 0 || candidate.LocalTopologyBindingCount != 0
                || candidate.PassageAuthority.Options.Count != 0 || candidate.PassageAuthority.Barriers.Count != 0
                || candidate.PassageAuthority.OptionStates.Count != 0 || candidate.PassageAuthority.BarrierStates.Count != 0
                || !HasSameFacts(candidate, snapshot))
            {
                return Fail(
                    P12CSpatialAuthoritySnapshotFailureCode.StageCompositionFailed,
                    "Private P8-A geography composition did not reproduce the exact snapshot facts and owner revision.",
                    out failure);
            }

            staged = candidate;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException)
        {
            return Fail(
                P12CSpatialAuthoritySnapshotFailureCode.StageCompositionFailed,
                "Private P8-A geography reconstruction rejected malformed values: " + exception.Message,
                out failure);
        }
    }

    private static bool HasExcludedFacts(SpatialAuthorityStore source)
    {
        return source.Crossings == null || source.Crossings.Count != 0
            || source.LocalTopologyBindings == null || source.LocalTopologyBindings.Count != 0
            || source.PassageAuthority == null
            || source.PassageAuthority.Options == null || source.PassageAuthority.Options.Count != 0
            || source.PassageAuthority.Barriers == null || source.PassageAuthority.Barriers.Count != 0
            || source.PassageAuthority.OptionStates == null || source.PassageAuthority.OptionStates.Count != 0
            || source.PassageAuthority.BarrierStates == null || source.PassageAuthority.BarrierStates.Count != 0;
    }

    private static bool HasSameFacts(SpatialAuthorityStore candidate, P12CSpatialAuthoritySnapshot snapshot)
    {
        if (!candidate.HasGeography
            || !string.Equals(candidate.CoordinateConventionVersion, snapshot.CoordinateConventionVersion, StringComparison.Ordinal)
            || !string.Equals(candidate.CoordinateCanonicalOrder, snapshot.CoordinateCanonicalOrder, StringComparison.Ordinal)
            || candidate.Hexes.Count != snapshot.Hexes.Count
            || candidate.Locations.Count != snapshot.Locations.Count)
        {
            return false;
        }

        HexRecord hex = candidate.Hexes[0];
        P12CSpatialHexSnapshot hexFact = snapshot.Hexes[0];
        LocationRecord location = candidate.Locations[0];
        P12CSpatialLocationSnapshot locationFact = snapshot.Locations[0];
        SpatialWorldScaleContext scale = candidate.ScaleContext;
        P12CSpatialScaleSnapshot scaleFact = snapshot.ScaleContext;
        return string.Equals(hex.Id.Value, hexFact.Id, StringComparison.Ordinal)
            && hex.Coordinate.HasValue
            && hex.Coordinate.Value.Q == hexFact.Q
            && hex.Coordinate.Value.R == hexFact.R
            && string.Equals(hex.TerrainDefinitionId.Value, hexFact.TerrainDefinitionId, StringComparison.Ordinal)
            && string.Equals(hex.AuthoredRevisionToken, hexFact.AuthoredRevisionToken, StringComparison.Ordinal)
            && string.Equals(location.Id.Value, locationFact.Id, StringComparison.Ordinal)
            && string.Equals(location.AnchorHexId.Value, locationFact.AnchorHexId, StringComparison.Ordinal)
            && string.Equals(scale.ResolvedConventionId, scaleFact.ResolvedConventionId, StringComparison.Ordinal)
            && string.Equals(scale.SourceIdentity, scaleFact.SourceIdentity, StringComparison.Ordinal)
            && string.Equals(scale.SourceVersion, scaleFact.SourceVersion, StringComparison.Ordinal)
            && scale.DistancePerNeighborStep == scaleFact.DistancePerNeighborStep
            && string.Equals(scale.Unit, scaleFact.Unit, StringComparison.Ordinal);
    }

    private static bool IsValidHex(HexRecord value)
    {
        return value != null && value.Id != null && !string.IsNullOrWhiteSpace(value.Id.Value)
            && value.Coordinate.HasValue && value.TerrainDefinitionId != null
            && !string.IsNullOrWhiteSpace(value.TerrainDefinitionId.Value)
            && !string.IsNullOrWhiteSpace(value.AuthoredRevisionToken);
    }

    private static bool IsValidHex(P12CSpatialHexSnapshot value)
    {
        return value != null && !string.IsNullOrWhiteSpace(value.Id)
            && !string.IsNullOrWhiteSpace(value.TerrainDefinitionId)
            && !string.IsNullOrWhiteSpace(value.AuthoredRevisionToken);
    }

    private static bool IsValidLocation(LocationRecord value)
    {
        return value != null && value.Id != null && !string.IsNullOrWhiteSpace(value.Id.Value)
            && value.AnchorHexId != null && !string.IsNullOrWhiteSpace(value.AnchorHexId.Value);
    }

    private static bool IsValidLocation(P12CSpatialLocationSnapshot value)
    {
        return value != null && !string.IsNullOrWhiteSpace(value.Id)
            && !string.IsNullOrWhiteSpace(value.AnchorHexId);
    }

    private static bool IsValidScale(SpatialWorldScaleContext value)
    {
        return value != null && !string.IsNullOrWhiteSpace(value.ResolvedConventionId)
            && !string.IsNullOrWhiteSpace(value.SourceIdentity)
            && !string.IsNullOrWhiteSpace(value.SourceVersion)
            && value.DistancePerNeighborStep > 0m
            && !string.IsNullOrWhiteSpace(value.Unit);
    }

    private static bool IsValidScale(P12CSpatialScaleSnapshot value)
    {
        return value != null && !string.IsNullOrWhiteSpace(value.ResolvedConventionId)
            && !string.IsNullOrWhiteSpace(value.SourceIdentity)
            && !string.IsNullOrWhiteSpace(value.SourceVersion)
            && value.DistancePerNeighborStep > 0m
            && !string.IsNullOrWhiteSpace(value.Unit);
    }

    private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> values)
    {
        return values == null
            ? null
            : new ReadOnlyCollection<T>(new List<T>(values));
    }

    private static bool Fail(
        P12CSpatialAuthoritySnapshotFailureCode code,
        string message,
        out P12CSpatialAuthoritySnapshotFailure failure)
    {
        failure = P12CSpatialAuthoritySnapshotFailure.Create(code, message);
        return false;
    }
}
