using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Detached schema-v1 ExplorableSite owner facts. This records site identity,
/// definition identity, and the legacy spatial-location reference only; the
/// owner currently has no mutable site-progress value to export.
/// </summary>
internal sealed class ExplorableSiteOwnerSnapshot
{
    internal const int CurrentSchemaVersion = 1;

    internal int SchemaVersion { get; }
    internal long Revision { get; }
    internal IReadOnlyList<ExplorableSiteOwnerSnapshotRecord> Sites { get; }

    internal ExplorableSiteOwnerSnapshot(
        int schemaVersion,
        long revision,
        IEnumerable<ExplorableSiteOwnerSnapshotRecord> sites)
    {
        SchemaVersion = schemaVersion;
        Revision = revision;
        if (sites == null)
        {
            Sites = null;
            return;
        }

        List<ExplorableSiteOwnerSnapshotRecord> copiedSites = new List<ExplorableSiteOwnerSnapshotRecord>();
        foreach (ExplorableSiteOwnerSnapshotRecord site in sites)
        {
            copiedSites.Add(site == null
                ? null
                : new ExplorableSiteOwnerSnapshotRecord(
                    site.RuntimeId,
                    site.SiteInstanceId,
                    site.DefinitionId,
                    site.LocationRuntimeId));
        }

        Sites = new ReadOnlyCollection<ExplorableSiteOwnerSnapshotRecord>(copiedSites);
    }
}

/// <summary>One exact site identity and its authored/legacy references.</summary>
internal sealed class ExplorableSiteOwnerSnapshotRecord
{
    internal string RuntimeId { get; }
    internal string SiteInstanceId { get; }
    internal string DefinitionId { get; }
    internal string LocationRuntimeId { get; }

    internal ExplorableSiteOwnerSnapshotRecord(
        string runtimeId,
        string siteInstanceId,
        string definitionId,
        string locationRuntimeId)
    {
        RuntimeId = runtimeId;
        SiteInstanceId = siteInstanceId;
        DefinitionId = definitionId;
        LocationRuntimeId = locationRuntimeId;
    }
}

internal enum ExplorableSiteSnapshotFailureCode
{
    None = 0,
    InvalidSnapshot,
    UnsupportedSnapshotSchema,
    InvalidSiteIdentity,
    DuplicateSiteIdentity,
    DuplicateDefinitionIdentity,
    IncompatibleDefinition,
    InvalidLegacyLocation,
    DuplicateLegacyLocationIdentity,
    MissingLegacyLocation,
    RuntimeIdentityCollision
}

internal sealed class ExplorableSiteSnapshotFailure
{
    internal static readonly ExplorableSiteSnapshotFailure None =
        new ExplorableSiteSnapshotFailure(ExplorableSiteSnapshotFailureCode.None, string.Empty);

    internal ExplorableSiteSnapshotFailureCode Code { get; }
    internal string Message { get; }

    private ExplorableSiteSnapshotFailure(ExplorableSiteSnapshotFailureCode code, string message)
    {
        Code = code;
        Message = message ?? string.Empty;
    }

    internal static ExplorableSiteSnapshotFailure Create(
        ExplorableSiteSnapshotFailureCode code,
        string message)
    {
        return code == ExplorableSiteSnapshotFailureCode.None
            ? None
            : new ExplorableSiteSnapshotFailure(code, message);
    }
}