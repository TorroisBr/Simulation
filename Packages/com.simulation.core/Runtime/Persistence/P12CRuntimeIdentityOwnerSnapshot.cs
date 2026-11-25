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

}
