using System;
using System.Collections.Generic;

/// <summary>
/// Internal immutable P12-D adapter value retained beside the Core wire codec.
/// It carries no world identity or capture-token authority.
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
