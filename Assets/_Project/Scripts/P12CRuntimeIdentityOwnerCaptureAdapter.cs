using System.Collections.Generic;

/// <summary>
/// Projects the Unity-hosted identity registry into the detached Core snapshot.
/// Capture-boundary authority remains with the caller.
/// </summary>
internal static class P12CRuntimeIdentityOwnerCaptureAdapter
{
    internal static bool TryCapture(RuntimeIdentityRegistry source,
        out P12CRuntimeIdentityOwnerSnapshot snapshot)
    {
        snapshot = null;
        if (source == null) return false;
        long before = source.CensusRevision;
        IReadOnlyList<KeyValuePair<string, string>> values = source.CaptureRegisteredRuntimeIdentityValues();
        long after = source.CensusRevision;
        if (before < 0 || before != after || values == null) return false;
        snapshot = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion, after, values);
        return true;
    }
}
