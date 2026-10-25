using System;

/// <summary>
/// Unity-owned P12-C staging projection. Image encoding and decoding are
/// authoritative in Simulation.Core; only construction of the Unity manifest
/// projection remains here.
/// </summary>
internal static class P12ACausalRootImageAdapter
{
    internal static bool TryStage(P12AContinuationImage image,
        out P12CStagedContinuationRoot staged, out P12AResult result,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        staged = null;
        if (!P12ACausalRootImageCodec.TryDecode(image,
                out P12ACausalRootImageSnapshots snapshots, out result))
            return false;
        if (!P12CContinuationRootStager.TryStageForImage(snapshots.WorldIdentity,
                snapshots.Allocator, snapshots.RecordSequence, snapshots.SpatialAuthority,
                P12CP9GenesisManifestSnapshot.FromCore(snapshots.P9Manifest),
                snapshots.RandomRoot, out staged, out _, stageObserver))
        {
            staged = null;
            result = new P12AResult(P12AResultCode.OwnerGraphRejected,
                "p12c.continuation-roots");
            return false;
        }
        result = null;
        return true;
    }
}