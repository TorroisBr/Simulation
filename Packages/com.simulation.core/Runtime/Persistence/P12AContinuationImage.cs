using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

internal sealed class P12AResult
{
    internal P12AResult(P12AResultCode code, string sectionCode = null, string stableOwnerKey = null)
    { Code = code; SectionCode = sectionCode; StableOwnerKey = stableOwnerKey; }
    internal P12AResultCode Code { get; }
    internal string SectionCode { get; }
    internal string StableOwnerKey { get; }
}



internal sealed class P12AContinuationHeader
{
    internal const string Profile = "UnityBootstrap-Daily-v1";
    internal const int ProfileVersion = 1;
    internal P12AContinuationHeader(string worldId, long day, long sequence, long lastEconomySnapshotDay,
        string p9ProfileStage, string p9ManifestFingerprint, string p8Profile, string p8GeographyFingerprint,
        long politicalWorldRevision,
        P12ACompatibilityDescriptor compatibility)
        : this(worldId, day, sequence, lastEconomySnapshotDay, p9ProfileStage,
            p9ManifestFingerprint, p8Profile, p8GeographyFingerprint,
            politicalWorldRevision, compatibility, null)
    {
    }

    internal static P12AContinuationHeader ForDotNetV2(string worldId, long day, long sequence,
        long lastEconomySnapshotDay,
        string p9ProfileStage, string p9ManifestFingerprint, string p8Profile, string p8GeographyFingerprint,
        long politicalWorldRevision,
        P12ACompatibilityDescriptorV2 compatibility)
    {
        return new P12AContinuationHeader(worldId, day, sequence, lastEconomySnapshotDay, p9ProfileStage,
            p9ManifestFingerprint, p8Profile, p8GeographyFingerprint,
            politicalWorldRevision, null, compatibility);
    }

    private P12AContinuationHeader(string worldId, long day, long sequence, long lastEconomySnapshotDay,
        string p9ProfileStage, string p9ManifestFingerprint, string p8Profile, string p8GeographyFingerprint,
        long politicalWorldRevision, P12ACompatibilityDescriptor compatibility,
        P12ACompatibilityDescriptorV2 compatibilityV2)
    {
        WorldId = worldId; Day = day; Sequence = sequence; LastEconomySnapshotDay = lastEconomySnapshotDay;
        PoliticalWorldRevision = politicalWorldRevision;
        P9ProfileStage = p9ProfileStage; P9ManifestFingerprint = p9ManifestFingerprint;
        P8Profile = p8Profile;
        P8GeographyFingerprint = p8GeographyFingerprint;
        Compatibility = compatibility;
        CompatibilityV2 = compatibilityV2;
    }
    internal string WorldId { get; }
    internal long Day { get; }
    internal long Sequence { get; }
    internal long LastEconomySnapshotDay { get; }
    /// <summary>Exact continuation precondition revision; unlike capture epochs, this affects later domain decisions.</summary>
    internal long PoliticalWorldRevision { get; }
    internal string P9ProfileStage { get; }
    internal string P9ManifestFingerprint { get; }
    internal string P8Profile { get; }
    internal string P8GeographyFingerprint { get; }
    internal P12ACompatibilityDescriptor Compatibility { get; }
    internal P12ACompatibilityDescriptorV2 CompatibilityV2 { get; }
    internal byte[] CompatibilityBytes => Compatibility != null
        ? Compatibility.CanonicalBytes
        : CompatibilityV2?.CanonicalBytes;
    internal bool IsValid => global::WorldId.TryParse(WorldId, out global::WorldId parsedWorldId)
        && string.Equals(parsedWorldId.Value, WorldId, StringComparison.Ordinal)
        && Day >= 0 && Sequence > 0
        && LastEconomySnapshotDay >= -1 && PoliticalWorldRevision >= 0
        && !string.IsNullOrWhiteSpace(P9ProfileStage)
        && !string.IsNullOrWhiteSpace(P9ManifestFingerprint)
        && !string.IsNullOrWhiteSpace(P8Profile)
        && !string.IsNullOrWhiteSpace(P8GeographyFingerprint)
        && ((Compatibility != null && CompatibilityV2 == null
                && Compatibility.P9Fingerprint == P9ManifestFingerprint
                && Compatibility.P8Fingerprint == P8GeographyFingerprint
                && Compatibility.ProfileId == Profile && Compatibility.ProfileVersion == ProfileVersion)
            || (Compatibility == null && CompatibilityV2 != null
                && CompatibilityV2.P9Fingerprint == P9ManifestFingerprint
                && CompatibilityV2.P8Fingerprint == P8GeographyFingerprint
                && CompatibilityV2.ProfileId == Profile && CompatibilityV2.ProfileVersion == ProfileVersion));
}

/// <summary>
/// Typed P12-C continuation roots carried alongside, but outside, the census
/// provider rows. The P9 manifest owns seed provenance; the random snapshot
/// pins the concrete provider/algorithm contract and must agree with that seed.
/// </summary>
internal sealed class P12ACausalRootPayload
{
    internal const string SectionId = "p12c.continuation-roots";
    internal const string OwnerKey = "p12c-continuation-roots";
    internal const int SchemaVersion = 1;

    internal P12ACausalRootPayload(P12CP9GenesisManifestCoreSnapshot p9Manifest,
        DeterministicRandomRootSnapshot randomRoot)
    { P9Manifest = p9Manifest; RandomRoot = randomRoot; }

    internal P12CP9GenesisManifestCoreSnapshot P9Manifest { get; }
    internal DeterministicRandomRootSnapshot RandomRoot { get; }

    internal bool IsValidFor(P12AContinuationHeader header) => header != null
        && P9Manifest != null && P9Manifest.TryValidate(out _)
        && RandomRoot != null && RandomRoot.Seed == P9Manifest.Seed
        && header.P9ProfileStage == P9Manifest.SelectedP9ContractIdentity
        && header.P9ManifestFingerprint == P9Manifest.Fingerprint;
}

internal sealed class P12AOwnerRow
{
    private readonly byte[] payload;
    internal P12AOwnerRow(string ownerKey, long revision, byte[] payload)
    { OwnerKey = ownerKey; Revision = revision; this.payload = payload == null ? null : (byte[])payload.Clone(); }
    internal string OwnerKey { get; }
    internal long Revision { get; }
    internal byte[] Payload => payload == null ? null : (byte[])payload.Clone();
    internal int PayloadLength => payload == null ? -1 : payload.Length;
    internal void WritePayload(BinaryWriter writer)
    {
        if (payload == null) throw new InvalidOperationException("Owner payload is missing.");
        writer.Write(payload);
    }
}

internal sealed class P12AOwnerSection
{
    private readonly ReadOnlyCollection<P12AOwnerRow> rows;
    internal P12AOwnerSection(string id, int major, int minor, OwnerSectionRole role,
        string aliasGroup, long revision, IEnumerable<P12AOwnerRow> rows)
    {
        Id = id; Major = major; Minor = minor; Role = role; AliasGroup = aliasGroup; Revision = revision;
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        List<P12AOwnerRow> copy = new List<P12AOwnerRow>();
        foreach (P12AOwnerRow row in rows)
        {
            if (row == null) throw new ArgumentException("Null owner row.");
            copy.Add(new P12AOwnerRow(row.OwnerKey, row.Revision, row.Payload));
        }
        this.rows = new ReadOnlyCollection<P12AOwnerRow>(copy);
    }
    internal string Id { get; }
    internal int Major { get; }
    internal int Minor { get; }
    internal OwnerSectionRole Role { get; }
    internal string AliasGroup { get; }
    internal long Revision { get; }
    internal IReadOnlyList<P12AOwnerRow> Rows => rows;
}

internal sealed class P12AContinuationImage
{
    private readonly ReadOnlyCollection<P12AOwnerSection> sections;
    internal P12AContinuationImage(P12AContinuationHeader header, P12ACausalRootPayload causalRoots,
        IEnumerable<P12AOwnerSection> sections)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        CausalRoots = causalRoots ?? throw new ArgumentNullException(nameof(causalRoots));
        if (sections == null) throw new ArgumentNullException(nameof(sections));
        List<P12AOwnerSection> copy = new List<P12AOwnerSection>();
        foreach (P12AOwnerSection section in sections)
        {
            if (section == null) throw new ArgumentException("Null section.");
            copy.Add(new P12AOwnerSection(section.Id, section.Major, section.Minor, section.Role,
                section.AliasGroup, section.Revision, section.Rows));
        }
        this.sections = new ReadOnlyCollection<P12AOwnerSection>(copy);
    }
    internal P12AContinuationHeader Header { get; }
    internal P12ACausalRootPayload CausalRoots { get; }
    internal IReadOnlyList<P12AOwnerSection> Sections => sections;
}

internal sealed class P12ASectionContract
{
    internal P12ASectionContract(string id, int major, int minor, OwnerSectionRole role, string aliasGroup)
    { Id = id; Major = major; Minor = minor; Role = role; AliasGroup = aliasGroup; }
    internal string Id { get; }
    internal int Major { get; }
    internal int Minor { get; }
    internal OwnerSectionRole Role { get; }
    internal string AliasGroup { get; }
}

internal sealed class P12ACensusSectionValue
{
    internal P12ACensusSectionValue(string id, int schemaVersion, OwnerSectionRole role,
        int cardinality, long revision, string aliasGroup)
    { Id = id; SchemaVersion = schemaVersion; Role = role; Cardinality = cardinality; Revision = revision; AliasGroup = aliasGroup; }
    internal string Id { get; }
    internal int SchemaVersion { get; }
    internal OwnerSectionRole Role { get; }
    internal int Cardinality { get; }
    internal long Revision { get; }
    internal string AliasGroup { get; }
}

/// <summary>
/// Detached logical census captured from the promoted P12 owner protocol. The
/// census object identities are used only while deriving alias equivalence;
/// only deterministic section keys and alias labels escape this boundary.
/// </summary>
internal sealed class P12ADailyV1Census
{
    private readonly ReadOnlyCollection<P12ACensusSectionValue> sections;
    private readonly P12ADailyV1SectionManifest manifest;
    private P12ADailyV1Census(IReadOnlyList<P12ACensusSectionValue> values,
        P12ADailyV1SectionManifest sectionManifest)
    {
        sections = new ReadOnlyCollection<P12ACensusSectionValue>(new List<P12ACensusSectionValue>(values));
        manifest = sectionManifest;
    }
    internal IReadOnlyList<P12ACensusSectionValue> Sections => sections;
    internal P12ADailyV1SectionManifest Manifest => manifest;
    internal static IReadOnlyList<string> FixedRequiredSectionIds => DailyV1SelectedOwnerSectionCensusContract.FixedRequired;
    internal static IReadOnlyList<string> FixedEmptySectionIds => DailyV1SelectedOwnerSectionCensusContract.FixedEmpty;
    internal static IReadOnlyList<string> NpcSectionPrefixes => DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes;
    internal static IReadOnlyList<string> CitySectionPrefixes => DailyV1SelectedOwnerSectionCensusContract.CityPrefixes;

    internal static bool TryCreate(IReadOnlyList<OwnerSectionCensusSnapshot> snapshots,
        out P12ADailyV1Census census, out P12AResult result)
    {
        census = null; result = new P12AResult(P12AResultCode.OwnerGraphRejected);
        if (snapshots == null) return false;
        Dictionary<string, OwnerSectionCensusSnapshot> byId = new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> ordered = new List<OwnerSectionCensusSnapshot>(snapshots.Count);
        foreach (OwnerSectionCensusSnapshot snapshot in snapshots)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.SectionId)
                || snapshot.SchemaVersion <= 0 || snapshot.Cardinality < 0 || snapshot.Revision < 0
                || snapshot.OwnerInstanceIdentity == null || !byId.TryAdd(snapshot.SectionId, snapshot))
                return false;
            ordered.Add(snapshot);
        }
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));

        HashSet<string> fixedRequired = new HashSet<string>(DailyV1SelectedOwnerSectionCensusContract.FixedRequired, StringComparer.Ordinal);
        HashSet<string> fixedEmpty = new HashSet<string>(DailyV1SelectedOwnerSectionCensusContract.FixedEmpty, StringComparer.Ordinal);
        HashSet<string> seenFixedRequired = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> seenFixedEmpty = new HashSet<string>(StringComparer.Ordinal);
        List<HashSet<string>> npcFamilies = new List<HashSet<string>>();
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes) npcFamilies.Add(new HashSet<string>(StringComparer.Ordinal));
        List<HashSet<string>> cityFamilies = new List<HashSet<string>>();
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes) cityFamilies.Add(new HashSet<string>(StringComparer.Ordinal));
        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> nonPersonNpcIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (OwnerSectionCensusSnapshot snapshot in ordered)
        {
            string id = snapshot.SectionId;
            if (fixedRequired.Contains(id))
            {
                if (snapshot.Role != OwnerSectionRole.Required || !seenFixedRequired.Add(id)) return false;
                continue;
            }
            if (fixedEmpty.Contains(id))
            {
                if (snapshot.Role != OwnerSectionRole.ExplicitlyEmpty || snapshot.Cardinality != 0
                    || !seenFixedEmpty.Add(id)) return false;
                continue;
            }
            int familyIndex = FindFamily(id, DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes);
            if (familyIndex >= 0)
            {
                if (snapshot.Role != OwnerSectionRole.Required
                    || !TryDynamicKey(id, DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes[familyIndex], out string key)
                    || !npcFamilies[familyIndex].Add(key)) return false;
                npcIds.Add(key); continue;
            }
            familyIndex = FindFamily(id, DailyV1SelectedOwnerSectionCensusContract.CityPrefixes);
            if (familyIndex >= 0)
            {
                if (snapshot.Role != OwnerSectionRole.Required
                    || !TryCityKey(id, DailyV1SelectedOwnerSectionCensusContract.CityPrefixes[familyIndex], out string key)
                    || !cityFamilies[familyIndex].Add(key)) return false;
                cityIds.Add(key); continue;
            }
            if (TryDynamicKey(id, "p12b.npc-residence/", out string npcResidence))
            {
                if (snapshot.Role != OwnerSectionRole.Required || !nonPersonNpcIds.Add(npcResidence)) return false;
                continue;
            }
            if (TryPersonKey(id, out string personId))
            {
                if (snapshot.Role != OwnerSectionRole.Required || !personIds.Add(personId)) return false;
                continue;
            }
            return false;
        }

        Dictionary<object, List<string>> identityGroups = new Dictionary<object, List<string>>(ReferenceComparer.Instance);
        foreach (OwnerSectionCensusSnapshot snapshot in ordered)
        {
            if (!identityGroups.TryGetValue(snapshot.OwnerInstanceIdentity, out List<string> aliases))
            { aliases = new List<string>(); identityGroups.Add(snapshot.OwnerInstanceIdentity, aliases); }
            aliases.Add(snapshot.SectionId);
        }
        Dictionary<string, string> aliasBySection = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (List<string> aliases in identityGroups.Values)
        {
            if (aliases.Count < 2) continue;
            aliases.Sort(StringComparer.Ordinal);
            string logicalAlias = "alias/" + aliases[0];
            foreach (string id in aliases) aliasBySection.Add(id, logicalAlias);
        }

        List<P12ACensusSectionValue> values = new List<P12ACensusSectionValue>(ordered.Count);
        foreach (OwnerSectionCensusSnapshot snapshot in ordered)
        {
            aliasBySection.TryGetValue(snapshot.SectionId, out string alias);
            values.Add(new P12ACensusSectionValue(snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role,
                snapshot.Cardinality, snapshot.Revision, alias));
        }
        return TryCreateFromValues(values, out census, out result);
    }

    internal bool LogicallyEquals(P12ADailyV1Census other)
    {
        if (other == null || sections.Count != other.sections.Count) return false;
        for (int i = 0; i < sections.Count; i++)
        {
            P12ACensusSectionValue left = sections[i], right = other.sections[i];
            if (left.Id != right.Id || left.SchemaVersion != right.SchemaVersion || left.Role != right.Role
                || left.Cardinality != right.Cardinality || left.Revision != right.Revision
                || left.AliasGroup != right.AliasGroup) return false;
        }
        return true;
    }

    internal bool MatchesImage(P12AContinuationImage image)
    {
        if (image == null || image.Sections.Count != sections.Count) return false;
        for (int i = 0; i < sections.Count; i++)
        {
            P12ACensusSectionValue censusSection = sections[i];
            P12AOwnerSection imageSection = image.Sections[i];
            if (censusSection.Id != imageSection.Id || censusSection.SchemaVersion != imageSection.Major
                || imageSection.Minor != 0 || censusSection.Role != imageSection.Role
                || censusSection.Cardinality != imageSection.Rows.Count
                || censusSection.Revision != imageSection.Revision
                || censusSection.AliasGroup != imageSection.AliasGroup) return false;
        }
        return true;
    }

    internal static bool TryCreateForImage(P12AContinuationImage image,
        out P12ADailyV1Census census, out P12AResult result)
    {
        census = null; result = new P12AResult(P12AResultCode.OwnerGraphRejected);
        if (image == null || image.Header == null || !image.Header.IsValid
            || image.CausalRoots == null || !image.CausalRoots.IsValidFor(image.Header)) return false;
        List<P12ACensusSectionValue> values = new List<P12ACensusSectionValue>(image.Sections.Count);
        foreach (P12AOwnerSection section in image.Sections)
        {
            if (section == null) return false;
            values.Add(new P12ACensusSectionValue(section.Id, section.Major, section.Role,
                section.Rows.Count, section.Revision, section.AliasGroup));
        }
        if (!TryCreateFromValues(values, out census, out result)) return false;
        if (!census.MatchesImage(image))
        { census = null; result = new P12AResult(P12AResultCode.OwnerGraphRejected); return false; }
        return true;
    }

    private static bool TryCreateFromValues(IReadOnlyList<P12ACensusSectionValue> input,
        out P12ADailyV1Census census, out P12AResult result)
    {
        census = null; result = new P12AResult(P12AResultCode.OwnerGraphRejected);
        if (input == null) return false;
        List<P12ACensusSectionValue> ordered = new List<P12ACensusSectionValue>(input);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left == null ? null : left.Id,
            right == null ? null : right.Id));
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> fixedRequired = new HashSet<string>(DailyV1SelectedOwnerSectionCensusContract.FixedRequired, StringComparer.Ordinal);
        HashSet<string> fixedEmpty = new HashSet<string>(DailyV1SelectedOwnerSectionCensusContract.FixedEmpty, StringComparer.Ordinal);
        HashSet<string> seenFixedRequired = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> seenFixedEmpty = new HashSet<string>(StringComparer.Ordinal);
        List<HashSet<string>> npcFamilies = new List<HashSet<string>>();
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes) npcFamilies.Add(new HashSet<string>(StringComparer.Ordinal));
        List<HashSet<string>> cityFamilies = new List<HashSet<string>>();
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes) cityFamilies.Add(new HashSet<string>(StringComparer.Ordinal));
        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> nonPersonNpcIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (P12ACensusSectionValue value in ordered)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Id) || value.SchemaVersion <= 0
                || value.Cardinality < 0 || value.Revision < 0 || !seenIds.Add(value.Id)
                || value.AliasGroup != ExpectedAliasGroupForSection(value.Id)) return false;
            string id = value.Id;
            if (fixedRequired.Contains(id))
            {
                if (value.Role != OwnerSectionRole.Required || !seenFixedRequired.Add(id)) return false;
                continue;
            }
            if (fixedEmpty.Contains(id))
            {
                if (value.Role != OwnerSectionRole.ExplicitlyEmpty || value.Cardinality != 0
                    || !seenFixedEmpty.Add(id)) return false;
                continue;
            }
            int familyIndex = FindFamily(id, DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes);
            if (familyIndex >= 0)
            {
                if (value.Role != OwnerSectionRole.Required
                    || !TryDynamicKey(id, DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes[familyIndex], out string key)
                    || !npcFamilies[familyIndex].Add(key)) return false;
                npcIds.Add(key); continue;
            }
            familyIndex = FindFamily(id, DailyV1SelectedOwnerSectionCensusContract.CityPrefixes);
            if (familyIndex >= 0)
            {
                if (value.Role != OwnerSectionRole.Required
                    || !TryCityKey(id, DailyV1SelectedOwnerSectionCensusContract.CityPrefixes[familyIndex], out string key)
                    || !cityFamilies[familyIndex].Add(key)) return false;
                cityIds.Add(key); continue;
            }
            if (TryDynamicKey(id, "p12b.npc-residence/", out string npcResidence))
            {
                if (value.Role != OwnerSectionRole.Required || !nonPersonNpcIds.Add(npcResidence)) return false;
                continue;
            }
            if (TryPersonKey(id, out string personId))
            {
                if (value.Role != OwnerSectionRole.Required || !personIds.Add(personId)) return false;
                continue;
            }
            return false;
        }

        if (seenFixedRequired.Count != DailyV1SelectedOwnerSectionCensusContract.FixedRequired.Count
            || seenFixedEmpty.Count != DailyV1SelectedOwnerSectionCensusContract.FixedEmpty.Count
            || npcFamilies.Count == 0 || npcFamilies[0].Count == 0 || cityFamilies[0].Count == 0) return false;
        foreach (HashSet<string> family in npcFamilies) if (!SameSet(family, npcFamilies[0])) return false;
        foreach (HashSet<string> family in cityFamilies) if (!SameSet(family, cityFamilies[0])) return false;
        foreach (string id in nonPersonNpcIds) if (!npcIds.Contains(id)) return false;
        long expectedCount = 61L + (long)DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes.Count * npcIds.Count
            + nonPersonNpcIds.Count + personIds.Count
            + (long)DailyV1SelectedOwnerSectionCensusContract.CityPrefixes.Count * cityIds.Count;
        if (expectedCount != ordered.Count) return false;

        List<P12ASectionContract> contracts = new List<P12ASectionContract>(ordered.Count);
        foreach (P12ACensusSectionValue value in ordered)
            contracts.Add(new P12ASectionContract(value.Id, value.SchemaVersion, 0, value.Role, value.AliasGroup));
        try { census = new P12ADailyV1Census(ordered, new P12ADailyV1SectionManifest(contracts)); }
        catch (ArgumentException) { return false; }
        result = null;
        return true;
    }

    internal static string ExpectedAliasGroupForSection(string id) =>
        DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(id);

    private static bool ContainsOrdinal(IReadOnlyList<string> values, string value)
    { for (int i = 0; i < values.Count; i++) if (string.Equals(values[i], value, StringComparison.Ordinal)) return true; return false; }
    private static int FindFamily(string id, IReadOnlyList<string> prefixes)
    { for (int i = 0; i < prefixes.Count; i++) if (id.StartsWith(prefixes[i], StringComparison.Ordinal)) return i; return -1; }
    private static bool TryDynamicKey(string id, string prefix, out string key)
    {
        key = null; if (!id.StartsWith(prefix, StringComparison.Ordinal)) return false;
        key = id.Substring(prefix.Length); return key.Length > 0 && !key.Contains("/") && !string.IsNullOrWhiteSpace(key);
    }
    private static bool TryEncodedCityKey(string id, string prefix, out string key)
    {
        key = null; if (!id.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string encoded = id.Substring(prefix.Length); int separator = encoded.IndexOf(':');
        if (separator <= 0 || !int.TryParse(encoded.Substring(0, separator), System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out int length)) return false;
        key = encoded.Substring(separator + 1); return length == key.Length && key.Length > 0 && !key.Contains("/");
    }
    private static bool TryCityKey(string id, string prefix, out string key)
    {
        if (prefix == "p12b.city.important-npcs/") return TryDynamicKey(id, prefix, out key);
        return TryEncodedCityKey(id, prefix, out key);
    }
    private static bool TryPersonKey(string id, out string key) => TryEncodedCityKey(id, "p12b.person-life-residence/", out key);
    private static bool SameSet(HashSet<string> left, HashSet<string> right) => left.SetEquals(right);

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new ReferenceComparer();
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}

internal sealed class P12ADailyV1SectionManifest
{
    private readonly ReadOnlyCollection<P12ASectionContract> contracts;
    internal P12ADailyV1SectionManifest(IEnumerable<P12ASectionContract> contracts)
    {
        if (contracts == null) throw new ArgumentNullException(nameof(contracts));
        List<P12ASectionContract> sorted = new List<P12ASectionContract>(contracts);
        sorted.Sort((a, b) => StringComparer.Ordinal.Compare(a == null ? null : a.Id, b == null ? null : b.Id));
        for (int i = 0; i < sorted.Count; i++)
        {
            P12ASectionContract c = sorted[i];
            if (c == null || string.IsNullOrWhiteSpace(c.Id) || c.Major <= 0 || c.Major > ushort.MaxValue
                || c.Minor < 0 || c.Minor > ushort.MaxValue
                || (c.Role != OwnerSectionRole.Required && c.Role != OwnerSectionRole.ExplicitlyEmpty)
                || (i > 0 && sorted[i - 1].Id == c.Id)) throw new ArgumentException("Invalid manifest.");
        }
        this.contracts = new ReadOnlyCollection<P12ASectionContract>(sorted);
    }
    internal IReadOnlyList<P12ASectionContract> Contracts => contracts;
    internal bool Matches(IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        result = null;
        if (sections == null || sections.Count != contracts.Count)
        { result = new P12AResult(P12AResultCode.MissingOrDuplicateSection); return false; }
        string prior = null;
        for (int i = 0; i < contracts.Count; i++)
        {
            P12ASectionContract c = contracts[i]; P12AOwnerSection s = sections[i];
            if (s == null || string.IsNullOrWhiteSpace(s.Id) || (prior != null && StringComparer.Ordinal.Compare(prior, s.Id) >= 0))
            { result = new P12AResult(P12AResultCode.MissingOrDuplicateSection); return false; }
            prior = s.Id;
            if (s.Id != c.Id) { result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, c.Id); return false; }
            if (s.Major != c.Major || s.Minor != c.Minor)
            { result = new P12AResult(P12AResultCode.UnsupportedSchema, s.Id); return false; }
            if (s.Role != c.Role || s.AliasGroup != c.AliasGroup
                || s.Revision < 0
                || (s.Role == OwnerSectionRole.ExplicitlyEmpty && s.Rows.Count != 0))
            { result = new P12AResult(P12AResultCode.InvalidOwnerValue, s.Id); return false; }
        }
        return true;
    }
}
internal static class P12AWire
{
    internal static readonly System.Text.UTF8Encoding Utf8 = ContinuationWire.CreateStrictUtf8Encoding() as System.Text.UTF8Encoding;
    internal static int GetEncodedStringByteCount(string value) => ContinuationWire.GetEncodedStringByteCount(value);
    internal static long GetEncodedStringFieldLength(string value) => ContinuationWire.GetEncodedStringFieldLength(value);
    internal static void WriteString(BinaryWriter writer, string value) => ContinuationWire.WriteString(writer, value);
    internal static bool ReadString(BinaryReader reader, out string value) => ContinuationWire.ReadString(reader, out value);
}
