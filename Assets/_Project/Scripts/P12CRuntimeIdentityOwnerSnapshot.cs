using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Detached runtime-identity indexes and their shared local revision.</summary>
internal sealed class P12CRuntimeIdentityOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal IReadOnlyList<KeyValuePair<string, string>> IdentityValues { get; }

    internal P12CRuntimeIdentityOwnerSnapshot(int schemaVersion, long revision,
        IEnumerable<KeyValuePair<string, string>> identityValues)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        IdentityValues = identityValues == null ? null
            : new ReadOnlyCollection<KeyValuePair<string, string>>(
                new List<KeyValuePair<string, string>>(identityValues));
    }

    /// <summary>
    /// Captures only the registry's value projection. The caller still owns
    /// completed-boundary authority and validates the token after capture.
    /// </summary>
    internal static bool TryCapture(RuntimeIdentityRegistry source,
        out P12CRuntimeIdentityOwnerSnapshot snapshot)
    {
        snapshot = null;
        if (source == null) return false;
        long before = source.CensusRevision;
        IReadOnlyList<KeyValuePair<string, string>> values = source.CaptureRegisteredRuntimeIdentityValues();
        long after = source.CensusRevision;
        if (before < 0 || before != after || values == null) return false;
        snapshot = new P12CRuntimeIdentityOwnerSnapshot(CurrentSchemaVersion, after, values);
        return true;
    }
}
