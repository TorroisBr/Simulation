using System;
using System.Collections.Generic;

internal enum DailyV1RuntimeIdentityKind : byte
{
    Npc = 0,
    City = 1,
    Location = 2,
    Route = 3,
    ExplorableSite = 4,
    LocalPlace = 5,
    LocalConnection = 6,
    NotableItem = 7
}

/// <summary>
/// Core-owned authority for global runtime-ID uniqueness, family membership,
/// and the shared selected-profile identity census revision. Unity keeps
/// typed object projections for presentation lookups, but cannot author IDs.
/// </summary>
internal sealed class DailyV1RuntimeIdentityIndex
{
    private readonly Dictionary<string, DailyV1RuntimeIdentityKind> kindsByRuntimeId =
        new Dictionary<string, DailyV1RuntimeIdentityKind>(StringComparer.Ordinal);
    private readonly List<HashSet<string>> runtimeIdsByKind = CreateFamilies();
    private long censusRevision;

    internal object CensusOwnerIdentity => this;
    internal long CensusRevision => censusRevision;

    /// <summary>
    /// Recreates the selected Daily-v1 identity index from its admitted
    /// detached census value while preserving the exact local revision.
    /// Registration values are staged without publishing mutations; the
    /// persisted revision may exceed active cardinality after prior removals.
    /// </summary>
    internal static bool TryCreateFromOwnerSnapshot(
        P12CRuntimeIdentityOwnerSnapshot snapshot,
        out DailyV1RuntimeIdentityIndex staged)
    {
        staged = null;
        if (snapshot == null
            || snapshot.SchemaVersion != P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0
            || snapshot.IdentityValues == null)
            return false;

        DailyV1RuntimeIdentityIndex candidate = new DailyV1RuntimeIdentityIndex();
        Dictionary<string, DailyV1RuntimeIdentityKind> kinds =
            new Dictionary<string, DailyV1RuntimeIdentityKind>(StringComparer.Ordinal)
            {
                ["npc"] = DailyV1RuntimeIdentityKind.Npc,
                ["city"] = DailyV1RuntimeIdentityKind.City,
                ["location"] = DailyV1RuntimeIdentityKind.Location,
                ["route"] = DailyV1RuntimeIdentityKind.Route
            };
        Dictionary<string, string> priorIdsByFamily =
            new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> value in snapshot.IdentityValues)
        {
            if (!kinds.TryGetValue(value.Key, out DailyV1RuntimeIdentityKind kind)
                || string.IsNullOrWhiteSpace(value.Value)
                || (priorIdsByFamily.TryGetValue(value.Key, out string priorId)
                    && StringComparer.Ordinal.Compare(priorId, value.Value) >= 0)
                || !candidate.TryStageRegister(value.Value, kind))
                return false;
            priorIdsByFamily[value.Key] = value.Value;
        }

        if (snapshot.Revision < candidate.kindsByRuntimeId.Count)
            return false;

        candidate.censusRevision = snapshot.Revision;
        staged = candidate;
        return true;
    }
    internal bool CanRegister(string runtimeId)
    {
        return !string.IsNullOrWhiteSpace(runtimeId)
            && !kindsByRuntimeId.ContainsKey(runtimeId)
            && censusRevision < long.MaxValue;
    }

    internal bool TryStageRegister(string runtimeId, DailyV1RuntimeIdentityKind kind)
    {
        if (!CanRegister(runtimeId) || !IsValidKind(kind)) return false;
        kindsByRuntimeId.Add(runtimeId, kind);
        runtimeIdsByKind[(int)kind].Add(runtimeId);
        return true;
    }

    internal bool TryAdvanceRevision()
    {
        if (censusRevision == long.MaxValue) return false;
        censusRevision++;
        return true;
    }

    internal bool TryRollbackRevision()
    {
        if (censusRevision <= 0L) return false;
        censusRevision--;
        return true;
    }

    internal bool TryRemoveStaged(string runtimeId, DailyV1RuntimeIdentityKind kind)
    {
        if (!kindsByRuntimeId.TryGetValue(runtimeId ?? string.Empty, out DailyV1RuntimeIdentityKind actual)
            || actual != kind) return false;
        kindsByRuntimeId.Remove(runtimeId);
        return runtimeIdsByKind[(int)kind].Remove(runtimeId);
    }

    internal bool Contains(string runtimeId) =>
        !string.IsNullOrWhiteSpace(runtimeId) && kindsByRuntimeId.ContainsKey(runtimeId);

    internal bool TryGetKind(string runtimeId, out DailyV1RuntimeIdentityKind kind) =>
        kindsByRuntimeId.TryGetValue(runtimeId ?? string.Empty, out kind);

    internal bool HasIdentity(string runtimeId, DailyV1RuntimeIdentityKind kind) =>
        !string.IsNullOrWhiteSpace(runtimeId)
        && kindsByRuntimeId.TryGetValue(runtimeId, out DailyV1RuntimeIdentityKind actual)
        && actual == kind;

    internal int GetCardinality(DailyV1RuntimeIdentityKind kind)
    {
        if (!IsValidKind(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        return runtimeIdsByKind[(int)kind].Count;
    }

    internal OwnerSectionCensusWitness GetOwnerSectionCensusWitness(
        DailyV1RuntimeIdentityKind kind)
    {
        string sectionId;
        switch (kind)
        {
            case DailyV1RuntimeIdentityKind.Npc: sectionId = P12CRuntimeIdentitySectionContract.NpcsSectionId; break;
            case DailyV1RuntimeIdentityKind.City: sectionId = P12CRuntimeIdentitySectionContract.CitiesSectionId; break;
            case DailyV1RuntimeIdentityKind.Location: sectionId = P12CRuntimeIdentitySectionContract.LocationsSectionId; break;
            case DailyV1RuntimeIdentityKind.Route: sectionId = P12CRuntimeIdentitySectionContract.RoutesSectionId; break;
            case DailyV1RuntimeIdentityKind.ExplorableSite: sectionId = P12CRuntimeIdentitySectionContract.ExplorableSitesSectionId; break;
            case DailyV1RuntimeIdentityKind.LocalPlace: sectionId = P12CRuntimeIdentitySectionContract.LocalPlacesSectionId; break;
            case DailyV1RuntimeIdentityKind.LocalConnection: sectionId = P12CRuntimeIdentitySectionContract.LocalConnectionsSectionId; break;
            case DailyV1RuntimeIdentityKind.NotableItem: sectionId = P12CRuntimeIdentitySectionContract.NotableItemsSectionId; break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new OwnerSectionCensusWitness(sectionId, P12CRuntimeIdentitySectionContract.SchemaVersion, this,
            GetCardinality(kind), CensusRevision);
    }

    internal IReadOnlyList<KeyValuePair<string, string>> CaptureRegisteredValues()
    {
        List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>(kindsByRuntimeId.Count);
        foreach (KeyValuePair<string, DailyV1RuntimeIdentityKind> item in kindsByRuntimeId)
            values.Add(new KeyValuePair<string, string>(FamilyId(item.Value), item.Key));
        values.Sort((left, right) =>
        {
            int familyOrder = string.Compare(left.Key, right.Key, StringComparison.Ordinal);
            return familyOrder != 0 ? familyOrder : string.Compare(left.Value, right.Value, StringComparison.Ordinal);
        });
        return values.AsReadOnly();
    }

    internal static string FamilyId(DailyV1RuntimeIdentityKind kind)
    {
        switch (kind)
        {
            case DailyV1RuntimeIdentityKind.Npc: return "npc";
            case DailyV1RuntimeIdentityKind.City: return "city";
            case DailyV1RuntimeIdentityKind.Location: return "location";
            case DailyV1RuntimeIdentityKind.Route: return "route";
            case DailyV1RuntimeIdentityKind.ExplorableSite: return "site";
            case DailyV1RuntimeIdentityKind.LocalPlace: return "local-place";
            case DailyV1RuntimeIdentityKind.LocalConnection: return "local-connection";
            case DailyV1RuntimeIdentityKind.NotableItem: return "notable-item";
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static bool IsValidKind(DailyV1RuntimeIdentityKind kind) =>
        (byte)kind <= (byte)DailyV1RuntimeIdentityKind.NotableItem;

    private static List<HashSet<string>> CreateFamilies()
    {
        List<HashSet<string>> result = new List<HashSet<string>>(8);
        for (int i = 0; i < 8; i++) result.Add(new HashSet<string>(StringComparer.Ordinal));
        return result;
    }
}
