using System;

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
