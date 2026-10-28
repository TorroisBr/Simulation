using System;

/// <summary>
/// Unity-owned P12-C staging projection. Image encoding and decoding are
/// authoritative in Simulation.Core. During application restoration, Core
/// owns detached root staging and this adapter constructs only the Unity
/// manifest projection required by the legacy runtime view.
/// </summary>
internal static class P12ACausalRootImageAdapter
{
    internal static bool TryStage(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        P12AContinuationImage image,
        out P12CStagedContinuationRoot staged,
        out P12AResult result,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        staged = null;
        if (application == null)
            return TryStage(image, out staged, out result, stageObserver);

        Action<string> coreStageObserver = stageName =>
        {
            if (!P12CContinuationRootStager.TryMapRestoreStageName(
                    stageName, out P12GDailyV1RestoreStage stage))
                throw new InvalidOperationException(
                    "Core P12-C staging emitted an unknown restore stage: " + stageName);
            stageObserver?.Invoke(stage);
        };

        if (!application.TryStageRestoredContinuationRoots(
                image, expectedSession, replacementOperation,
                out P12CStagedCoreContinuationRoot coreRoots,
                out result,
                coreStageObserver)
            || coreRoots == null)
            return false;

        return TryProject(coreRoots, out staged, out result);
    }

    internal static bool TryProject(
        P12CStagedCoreContinuationRoot coreRoots,
        out P12CStagedContinuationRoot staged,
        out P12AResult result)
    {
        staged = null;
        result = new P12AResult(P12AResultCode.OwnerGraphRejected,
            "p12c.genesis-manifest-projection");
        if (coreRoots == null || !coreRoots.IsDetached) return false;

        P12CP9GenesisManifestSnapshot manifestSnapshot =
            P12CP9GenesisManifestSnapshot.FromCore(coreRoots.GenesisManifest);
        if (manifestSnapshot == null
            || !manifestSnapshot.TryStageManifest(
                out SimulationGenesisManifest manifest, out _)
            || manifest == null)
            return false;

        staged = new P12CStagedContinuationRoot(null, coreRoots, manifest);
        result = null;
        return true;
    }

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
