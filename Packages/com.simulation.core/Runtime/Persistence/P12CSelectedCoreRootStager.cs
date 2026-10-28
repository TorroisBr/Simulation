using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>Core-owned private reconstruction of the selected P12-C value roots.</summary>
internal sealed class P12CStagedCoreContinuationRoot
{
    private DailyV1P12RestoreStageTransaction restoreStageTransaction;

    internal P12CStagedCoreContinuationRoot(
        bool isDetached,
        WorldId worldIdentity,
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationRecordSequence recordSequence,
        SpatialAuthorityStore spatialAuthority,
        P12CP9GenesisManifestCoreSnapshot genesisManifest,
        DeterministicRandomSource deterministicRandom)
    {
        IsDetached = isDetached;
        WorldIdentity = worldIdentity ?? throw new ArgumentNullException(nameof(worldIdentity));
        RuntimeIdAllocator = runtimeIdAllocator ?? throw new ArgumentNullException(nameof(runtimeIdAllocator));
        RecordSequence = recordSequence ?? throw new ArgumentNullException(nameof(recordSequence));
        SpatialAuthority = spatialAuthority ?? throw new ArgumentNullException(nameof(spatialAuthority));
        GenesisManifest = genesisManifest ?? throw new ArgumentNullException(nameof(genesisManifest));
        DeterministicRandom = deterministicRandom ?? throw new ArgumentNullException(nameof(deterministicRandom));
    }

    internal bool IsDetached { get; }
    internal WorldId WorldIdentity { get; }
    internal RuntimeIdAllocator RuntimeIdAllocator { get; }
    internal SimulationRecordSequence RecordSequence { get; }
    internal SpatialAuthorityStore SpatialAuthority { get; }
    internal P12CP9GenesisManifestCoreSnapshot GenesisManifest { get; }
    internal DeterministicRandomSource DeterministicRandom { get; }
    internal DailyV1P12RestoreStageTransaction RestoreStageTransaction => restoreStageTransaction;

    internal bool TryBindRestoreStageTransaction(DailyV1P12RestoreStageTransaction transaction)
    {
        if (transaction == null || restoreStageTransaction != null)
            return false;
        restoreStageTransaction = transaction;
        return true;
    }
}

/// <summary>
/// Reconstructs the Core-owned P12-C roots from detached, validated P12 value snapshots.
/// It has no Unity manifest, live capture, or presentation dependency.
/// </summary>
internal static class P12CSelectedCoreRootStager
{
    private const string SpatialAuthorityOutputOwner = "SpatialAuthorityStore";

    internal static bool TryStage(
        P12CWorldIdentitySnapshot worldIdentitySnapshot,
        RuntimeIdAllocatorSnapshot allocatorSnapshot,
        SimulationRecordSequenceSnapshot recordSequenceSnapshot,
        P12CSpatialAuthoritySnapshot spatialSnapshot,
        P12CP9GenesisManifestCoreSnapshot genesisManifestSnapshot,
        DeterministicRandomRootSnapshot randomRootSnapshot,
        bool isDetached,
        Action<string> stageObserver,
        out P12CStagedCoreContinuationRoot staged,
        out string diagnostic)
    {
        staged = null;
        diagnostic = null;
        try
        {
            if (worldIdentitySnapshot == null)
            {
                diagnostic = "P12-C WorldId snapshot is required.";
                return false;
            }

            if (!worldIdentitySnapshot.TryStage(out WorldId stagedWorldIdentity, out diagnostic))
                return false;
            stageObserver?.Invoke("CWorldIdentityStaged");

            if (genesisManifestSnapshot == null || !genesisManifestSnapshot.TryValidate(out diagnostic))
            {
                diagnostic = diagnostic ?? "P12-C P9-B genesis manifest snapshot is required.";
                return false;
            }
            stageObserver?.Invoke("CGenesisManifestStaged");

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

            if (!string.Equals(spatialSnapshot.P9ProfileContractIdentity,
                    genesisManifestSnapshot.SelectedP9ContractIdentity, StringComparison.Ordinal)
                || spatialSnapshot.P9ProfileSchemaVersion != genesisManifestSnapshot.SelectedP9SchemaVersion)
            {
                diagnostic = "P12-C P8-A geography identity/schema does not match the selected P9-B manifest.";
                return false;
            }

            if (!ContainsExactlyOne(genesisManifestSnapshot.OutputOwners, SpatialAuthorityOutputOwner))
            {
                diagnostic = "P12-C P9-B manifest must retain SpatialAuthorityStore exactly once in OutputOwners.";
                return false;
            }

            if (!P12CSpatialAuthoritySnapshot.TryCreateStagedFromSnapshot(
                    spatialSnapshot, out SpatialAuthorityStore stagedSpatialAuthority,
                    out P12CSpatialAuthoritySnapshotFailure spatialFailure))
            {
                diagnostic = spatialFailure == null
                    ? "P12-C P8-A geography snapshot was rejected."
                    : spatialFailure.Message;
                return false;
            }

            if (!P9GeographyMatchesSpatialSnapshot(genesisManifestSnapshot, spatialSnapshot, out diagnostic))
                return false;
            stageObserver?.Invoke("CSpatialAuthorityStaged");

            if (!DeterministicRandomSource.TryCreateStagedFromSnapshot(
                    randomRootSnapshot, out DeterministicRandomSource stagedRandom, out diagnostic))
                return false;

            if (stagedRandom.Seed != genesisManifestSnapshot.Seed)
            {
                diagnostic = "P12-C deterministic-random seed does not match the retained P9-B effective seed.";
                return false;
            }
            stageObserver?.Invoke("CDeterministicRandomStaged");

            if (!RuntimeIdAllocatorOwnerBridge.TryCreateStagedFromSnapshot(
                    allocatorSnapshot, out RuntimeIdAllocator stagedAllocator, out diagnostic))
                return false;
            stageObserver?.Invoke("CRuntimeIdAllocatorStaged");

            if (!SimulationRecordSequenceOwnerBridge.TryCreateStagedFromSnapshot(
                    recordSequenceSnapshot, out SimulationRecordSequence stagedRecordSequence, out diagnostic))
                return false;
            stageObserver?.Invoke("CRecordSequenceStaged");

            staged = new P12CStagedCoreContinuationRoot(
                isDetached,
                stagedWorldIdentity,
                stagedAllocator,
                stagedRecordSequence,
                stagedSpatialAuthority,
                genesisManifestSnapshot,
                stagedRandom);
            return true;
        }
        catch (Exception exception)
        {
            staged = null;
            diagnostic = "P12-C Core root staging failed: " + exception.GetType().Name + ": " + exception.Message;
            return false;
        }
    }

    private static bool P9GeographyMatchesSpatialSnapshot(
        P12CP9GenesisManifestCoreSnapshot manifest,
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

        string expectedHex = EncodeTaggedRecord("authored-hex", hex.Id, hex.Q, hex.R,
            spatial.CoordinateConventionVersion, spatial.CoordinateCanonicalOrder,
            hex.TerrainDefinitionId, hex.AuthoredRevisionToken);
        string expectedLocation = EncodeTaggedRecord("authored-location", location.Id, location.AnchorHexId);
        string expectedScale = EncodeTaggedRecord("authored-scale", scale.ResolvedConventionId,
            scale.SourceIdentity, scale.SourceVersion, scale.DistancePerNeighborStep, scale.Unit);

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
        IReadOnlyList<string> records, string tag, string expectedRecord)
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

    private static bool ContainsExactlyOne(IReadOnlyList<string> values, string expected)
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
            record.Append('|').Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
        }
        return record.ToString();
    }
}
