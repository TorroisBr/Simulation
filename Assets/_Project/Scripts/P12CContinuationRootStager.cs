using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Opaque identity for one reconstruction staging attempt at an exact
/// completed Daily-v1 boundary. C, D, and E retain the same instance so roots
/// from separate attempts cannot be combined accidentally.
/// </summary>
internal sealed class DailyCaptureStagingAttempt
{
    private readonly SimulationRuntime sourceRuntime;
    private readonly DailyCaptureEligibilityToken token;
    private readonly IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections;

    private DailyCaptureStagingAttempt(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections)
    {
        this.sourceRuntime = sourceRuntime;
        this.token = token;
        this.ownerSections = ownerSections;
    }

    internal static bool TryBegin(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureStagingAttempt attempt)
    {
        attempt = null;
        if (sourceRuntime == null || token == null || ownerSections == null
            || !ReferenceEquals(token.OwnerSections, ownerSections)
            || !ReferenceEquals(token.WorldId, sourceRuntime.WorldId)
            || token.AdmissionContext == null
            || token.AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || token.CompletedCoreSequence <= 0L
            || !sourceRuntime.TryValidateCompletedDailyCaptureToken(token, out _))
            return false;

        attempt = new DailyCaptureStagingAttempt(sourceRuntime, token, ownerSections);
        return true;
    }

    internal bool IsCurrentFor(
        SimulationRuntime candidateRuntime,
        DailyCaptureEligibilityToken candidateToken,
        IReadOnlyList<OwnerSectionCensusSnapshot> candidateOwnerSections)
    {
        return ReferenceEquals(sourceRuntime, candidateRuntime)
            && ReferenceEquals(token, candidateToken)
            && ReferenceEquals(ownerSections, candidateOwnerSections)
            && candidateRuntime != null
            && candidateRuntime.TryValidateCompletedDailyCaptureToken(candidateToken, out _);
    }

    internal bool IsCurrent => IsCurrentFor(sourceRuntime, token, ownerSections);
}

/// <summary>
/// Detached value for the identity of the same causal world continuation.
/// </summary>
internal sealed class P12CWorldIdentitySnapshot
{
    internal const string CurrentSnapshotContract = "p12c.world-identity-snapshot/v1";
    internal const int CurrentSnapshotSchemaVersion = 1;

    internal P12CWorldIdentitySnapshot(string snapshotContract, int schemaVersion, string worldIdValue)
    {
        SnapshotContract = snapshotContract;
        SchemaVersion = schemaVersion;
        WorldIdValue = worldIdValue;
    }

    internal string SnapshotContract { get; }
    internal int SchemaVersion { get; }
    internal string WorldIdValue { get; }

    internal static bool TryCapture(WorldId source, out P12CWorldIdentitySnapshot snapshot, out string diagnostic)
    {
        snapshot = null;
        diagnostic = null;
        if (source == null || !WorldId.TryParse(source.Value, out WorldId validated)
            || !string.Equals(validated.Value, source.Value, StringComparison.Ordinal))
        {
            diagnostic = "P12-C requires the canonical WorldId already published by the validated composition.";
            return false;
        }

        snapshot = new P12CWorldIdentitySnapshot(CurrentSnapshotContract, CurrentSnapshotSchemaVersion, source.Value);
        return true;
    }

    internal bool TryStage(out WorldId staged, out string diagnostic)
    {
        staged = null;
        diagnostic = null;
        if (!string.Equals(SnapshotContract, CurrentSnapshotContract, StringComparison.Ordinal)
            || SchemaVersion != CurrentSnapshotSchemaVersion)
        {
            diagnostic = "P12-C WorldId snapshot schema is unsupported.";
            return false;
        }

        if (!WorldId.TryParse(WorldIdValue, out staged)
            || !string.Equals(staged.Value, WorldIdValue, StringComparison.Ordinal))
        {
            staged = null;
            diagnostic = "P12-C WorldId snapshot value is not canonical.";
            return false;
        }

        return true;
    }
}

/// <summary>
/// One privately staged set of the already-supported P12-C continuation roots.
/// It is not published to an active SimulationRuntime.
/// </summary>
internal sealed class P12CStagedContinuationRoot
{
    internal P12CStagedContinuationRoot(
        DailyCaptureStagingAttempt stagingAttempt,
        WorldId worldIdentity,
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationRecordSequence recordSequence,
        SpatialAuthorityStore spatialAuthority,
        SimulationGenesisManifest genesisManifest,
        DeterministicRandomSource deterministicRandom)
    {
        StagingAttempt = stagingAttempt;
        WorldIdentity = worldIdentity;
        RuntimeIdAllocator = runtimeIdAllocator;
        RecordSequence = recordSequence;
        SpatialAuthority = spatialAuthority;
        GenesisManifest = genesisManifest;
        DeterministicRandom = deterministicRandom;
    }

    internal DailyCaptureStagingAttempt StagingAttempt { get; }
    internal WorldId WorldIdentity { get; }
    internal RuntimeIdAllocator RuntimeIdAllocator { get; }
    internal SimulationRecordSequence RecordSequence { get; }
    internal SpatialAuthorityStore SpatialAuthority { get; }
    internal SimulationGenesisManifest GenesisManifest { get; }
    internal DeterministicRandomSource DeterministicRandom { get; }
}

/// <summary>
/// Composes the accepted Daily-v1 P12-C owner snapshots into private staged
/// roots, rejecting any cross-owner disagreement before returning a bundle.
/// </summary>
internal static class P12CContinuationRootStager
{
    private const string SpatialAuthorityOutputOwner = "SpatialAuthorityStore";

    internal static bool TryStage(
        DailyCaptureStagingAttempt stagingAttempt,
        P12CWorldIdentitySnapshot worldIdentitySnapshot,
        RuntimeIdAllocatorSnapshot allocatorSnapshot,
        SimulationRecordSequenceSnapshot recordSequenceSnapshot,
        P12CSpatialAuthoritySnapshot spatialSnapshot,
        P12CP9GenesisManifestSnapshot genesisManifestSnapshot,
        DeterministicRandomRootSnapshot randomRootSnapshot,
        out P12CStagedContinuationRoot staged,
        out string diagnostic)
    {
        return TryStageForRestore(
            stagingAttempt,
            worldIdentitySnapshot,
            allocatorSnapshot,
            recordSequenceSnapshot,
            spatialSnapshot,
            genesisManifestSnapshot,
            randomRootSnapshot,
            out staged,
            out diagnostic,
            null);
    }

    internal static bool TryStageForRestore(
        DailyCaptureStagingAttempt stagingAttempt,
        P12CWorldIdentitySnapshot worldIdentitySnapshot,
        RuntimeIdAllocatorSnapshot allocatorSnapshot,
        SimulationRecordSequenceSnapshot recordSequenceSnapshot,
        P12CSpatialAuthoritySnapshot spatialSnapshot,
        P12CP9GenesisManifestSnapshot genesisManifestSnapshot,
        DeterministicRandomRootSnapshot randomRootSnapshot,
        out P12CStagedContinuationRoot staged,
        out string diagnostic,
        Action<P12GDailyV1RestoreStage> stageObserver)
    {
        staged = null;
        diagnostic = null;

        try
        {
            if (stagingAttempt == null || !stagingAttempt.IsCurrent)
            {
                diagnostic = "P12-C staging attempt is stale or incomplete.";
                return false;
            }

            if (worldIdentitySnapshot == null)
            {
                diagnostic = "P12-C WorldId snapshot is required.";
                return false;
            }

            if (!worldIdentitySnapshot.TryStage(out WorldId stagedWorldIdentity, out diagnostic))
                return false;
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CWorldIdentityStaged);

            if (genesisManifestSnapshot == null)
            {
                diagnostic = "P12-C P9-B genesis manifest snapshot is required.";
                return false;
            }

            if (!genesisManifestSnapshot.TryStageManifest(
                out SimulationGenesisManifest stagedManifest,
                out diagnostic))
            {
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CGenesisManifestStaged);

            if (spatialSnapshot == null)
            {
                diagnostic = "P12-C P8-A geography snapshot is required.";
                return false;
            }

            if (spatialSnapshot.AdmissionProfile != genesisManifestSnapshot.P12AdmissionProfile
                || spatialSnapshot.AdmissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            {
                diagnostic = "P12-C P8-A and P9-B snapshots must use the same Daily-v1 admission profile.";
                return false;
            }

            if (!string.Equals(
                    spatialSnapshot.P9ProfileContractIdentity,
                    genesisManifestSnapshot.SelectedP9ContractIdentity,
                    StringComparison.Ordinal)
                || spatialSnapshot.P9ProfileSchemaVersion != genesisManifestSnapshot.SelectedP9SchemaVersion)
            {
                diagnostic = "P12-C P8-A geography identity/schema does not match the selected P9-B manifest.";
                return false;
            }

            if (!ContainsExactlyOne(
                genesisManifestSnapshot.OutputOwners,
                SpatialAuthorityOutputOwner))
            {
                diagnostic = "P12-C P9-B manifest must retain SpatialAuthorityStore exactly once in OutputOwners.";
                return false;
            }

            if (!P12CSpatialAuthoritySnapshot.TryCreateStagedFromSnapshot(
                spatialSnapshot,
                out SpatialAuthorityStore stagedSpatialAuthority,
                out P12CSpatialAuthoritySnapshotFailure spatialFailure))
            {
                diagnostic = spatialFailure == null
                    ? "P12-C P8-A geography snapshot was rejected."
                    : spatialFailure.Message;
                return false;
            }

            if (!P9GeographyMatchesSpatialSnapshot(genesisManifestSnapshot, spatialSnapshot, out diagnostic))
            {
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CSpatialAuthorityStaged);

            if (!DeterministicRandomSource.TryCreateStagedFromSnapshot(
                randomRootSnapshot,
                out DeterministicRandomSource stagedRandom,
                out diagnostic))
            {
                return false;
            }

            if (stagedRandom.Seed != genesisManifestSnapshot.Seed)
            {
                diagnostic = "P12-C deterministic-random seed does not match the retained P9-B effective seed.";
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CDeterministicRandomStaged);

            if (!RuntimeIdAllocator.TryCreateStagedFromSnapshot(
                allocatorSnapshot,
                out RuntimeIdAllocator stagedAllocator,
                out diagnostic))
            {
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CRuntimeIdAllocatorStaged);

            if (!SimulationRecordSequence.TryCreateStagedFromSnapshot(
                recordSequenceSnapshot,
                out SimulationRecordSequence stagedRecordSequence,
                out diagnostic))
            {
                return false;
            }
            stageObserver?.Invoke(P12GDailyV1RestoreStage.CRecordSequenceStaged);

            P12CStagedContinuationRoot candidate = new P12CStagedContinuationRoot(
                stagingAttempt,
                stagedWorldIdentity,
                stagedAllocator,
                stagedRecordSequence,
                stagedSpatialAuthority,
                stagedManifest,
                stagedRandom);
            if (!stagingAttempt.IsCurrent)
            {
                diagnostic = "P12-C staging attempt became stale before the continuation roots were returned.";
                return false;
            }
            staged = candidate;
            return true;
        }
        catch (Exception exception)
        {
            staged = null;
            diagnostic = "P12-C private root staging failed: " + exception.GetType().Name + ": " + exception.Message;
            return false;
        }
    }

    private static bool P9GeographyMatchesSpatialSnapshot(
        P12CP9GenesisManifestSnapshot manifest,
        P12CSpatialAuthoritySnapshot spatial,
        out string diagnostic)
    {
        diagnostic = null;
        if (manifest == null || spatial == null
            || spatial.Hexes == null || spatial.Hexes.Count != 1
            || spatial.Locations == null || spatial.Locations.Count != 1
            || spatial.ScaleContext == null)
        {
            diagnostic = "P12-C cannot reconcile incomplete P8-A and P9-B geography values.";
            return false;
        }

        P12CSpatialHexSnapshot hex = spatial.Hexes[0];
        P12CSpatialLocationSnapshot location = spatial.Locations[0];
        P12CSpatialScaleSnapshot scale = spatial.ScaleContext;
        if (hex == null || location == null)
        {
            diagnostic = "P12-C P8-A geography snapshot contains a missing authored fact.";
            return false;
        }

        string expectedHex = EncodeTaggedRecord(
            "authored-hex",
            hex.Id,
            hex.Q,
            hex.R,
            spatial.CoordinateConventionVersion,
            spatial.CoordinateCanonicalOrder,
            hex.TerrainDefinitionId,
            hex.AuthoredRevisionToken);
        string expectedLocation = EncodeTaggedRecord(
            "authored-location",
            location.Id,
            location.AnchorHexId);
        string expectedScale = EncodeTaggedRecord(
            "authored-scale",
            scale.ResolvedConventionId,
            scale.SourceIdentity,
            scale.SourceVersion,
            scale.DistancePerNeighborStep,
            scale.Unit);

        if (!HasExactlyOneTaggedRecord(manifest.CanonicalProvenanceRecords, "authored-hex", expectedHex)
            || !HasExactlyOneTaggedRecord(manifest.CanonicalProvenanceRecords, "authored-location", expectedLocation)
            || !HasExactlyOneTaggedRecord(manifest.CanonicalProvenanceRecords, "authored-scale", expectedScale))
        {
            diagnostic = "P12-C P8-A Hex, Location, or scale values do not exactly match the unique P9-B authored geography provenance records.";
            return false;
        }

        return true;
    }

    private static bool HasExactlyOneTaggedRecord(
        System.Collections.Generic.IReadOnlyList<string> records,
        string tag,
        string expectedRecord)
    {
        if (records == null) return false;
        int count = 0;
        bool exactMatch = false;
        for (int i = 0; i < records.Count; i++)
        {
            string record = records[i];
            if (record == null || !record.StartsWith(tag, StringComparison.Ordinal)) continue;
            count++;
            if (string.Equals(record, expectedRecord, StringComparison.Ordinal)) exactMatch = true;
        }

        return count == 1 && exactMatch;
    }

    private static bool ContainsExactlyOne(
        System.Collections.Generic.IReadOnlyList<string> values,
        string expected)
    {
        if (values == null) return false;
        int count = 0;
        for (int i = 0; i < values.Count; i++)
            if (string.Equals(values[i], expected, StringComparison.Ordinal)) count++;
        return count == 1;
    }

    private static string EncodeTaggedRecord(string tag, params object[] values)
    {
        StringBuilder record = new StringBuilder(tag);
        for (int i = 0; i < values.Length; i++)
        {
            string value = Convert.ToString(values[i], CultureInfo.InvariantCulture) ?? string.Empty;
            record.Append('|')
                .Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(value);
        }

        return record.ToString();
    }
}
