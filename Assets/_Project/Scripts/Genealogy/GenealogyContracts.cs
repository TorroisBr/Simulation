using System;
using System.Collections.Generic;

/// <summary>
/// Internal immutable P12-D adapter value. Core owns the public domain edge
/// and failure contracts; this wire-facing wrapper remains in Unity.
/// </summary>
internal sealed class GenealogyOwnerSnapshot
{
    internal const int CurrentSchemaVersion = GenealogyOwnerExport.CurrentSchemaVersion;

    private readonly GenealogyOwnerExport export;

    internal GenealogyOwnerExport Export => export;
    internal int SchemaVersion => export.SchemaVersion;
    internal long Revision => export.Revision;
    internal IReadOnlyList<ParentageRecord> Records => export.Records;

    internal GenealogyOwnerSnapshot(GenealogyOwnerExport export)
    {
        this.export = export ?? throw new ArgumentNullException(nameof(export));
    }

    internal GenealogyOwnerSnapshot(
        int schemaVersion,
        long revision,
        IEnumerable<ParentageRecord> records)
    {
        export = new GenealogyOwnerExport(schemaVersion, revision, records);
    }
}
