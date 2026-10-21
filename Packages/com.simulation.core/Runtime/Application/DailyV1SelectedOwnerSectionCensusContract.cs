using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Canonical owner-section inventory for the selected Daily-v1 continuation
/// profile. P12 image validation and the live runtime census consume this
/// same immutable manifest; it describes coverage and never manufactures a
/// witness for an owner.
/// </summary>
internal static class DailyV1SelectedOwnerSectionCensusContract
{
    internal static IReadOnlyList<string> NpcPrefixes { get; } = ReadOnly(new[]
    {
        "p12f.explorable-site-knowledge/", "p12f.local-topology-knowledge.places/",
        "p12f.local-topology-knowledge.connections/", "p12f.adventure-intel.opposition/",
        "p12f.adventure-intel.notable-items/", "p12f.adventure-intel.common-resources/",
        "p12f.adventure-intel.access/", "p12f.commercial-knowledge.markets/",
        "p12f.commercial-knowledge.liquidity/", "p12f.commercial-knowledge.share-receipts/",
        "p12f.spatial-knowledge.locations/", "p12f.spatial-knowledge.routes/",
        "p12f.npc-travel-state/", "p12f.inventory/", "p12e.npc-money-account/",
        "p12b.merchant-trade-plan/", "p12b.npc-travel-plan/",
        "p12d.npc-local-observation-receipts/", "p12d.npc-merchant-trade-state-receipts/",
        "p12b.npc-status-crime-state/", "p12b.npc-current-action/", "p12b.npc-life-state/"
    });

    internal static IReadOnlyList<string> CityPrefixes { get; } = ReadOnly(new[]
    {
        "p12b.city.important-npcs/", "p12e.city-market-stock-rows/",
        "p12d.city-population.aggregate/", "p12d.city-population.operation-receipts/"
    });

    internal static IReadOnlyList<string> FixedEmpty { get; } = ReadOnly(new[]
    {
        "p12c.runtime-identities.explorable-sites", "p12c.runtime-identities.local-places",
        "p12c.runtime-identities.local-connections", "p12c.runtime-identities.notable-items",
        "p12d.explorable-sites", "p8b.passage-option-barrier-state", "p8b.crossings",
        "p8c.city-site-location-bindings", "p8c.person-positions", "p8d.spatial-route-observations",
        "p8d.person-route-plan-history", "p12f.npc-decision-occurrence-receipts",
        "p12e.economy-keyed-sale-receipts"
    });

    internal static IReadOnlyList<string> FixedRequired { get; } = ReadOnly(new[]
    {
        "p12d.person.membership", "p12d.person.materialization-binding",
        "p12c.runtime-identities.npcs", "p12c.runtime-identities.cities", "p12c.runtime-identities.locations",
        "p12c.runtime-identities.routes", "p12d.legacy-spatial-network.locations", "p12d.legacy-spatial-network.routes",
        "p8a.hexes", "p8a.locations", "p8a.scale-context", "p12c.runtime-id-allocator.events",
        "p12c.runtime-id-allocator.decisions", "p12c.runtime-id-allocator.travel-parties",
        "p12c.simulation-record-sequence", "p12d.genealogy.parentage", "p12f.political-knowledge.holders",
        "p12f.political-decisions.records", "p12.crime-social-appraisal.outcomes",
        "p12.crime-social-appraisal.knowledge", "p12.crime-social-appraisal.reactions", "p12b.justice-records",
        "p12b.crime-p18-receipts", "p12b.justice-p18-receipts", "p12e.armed-force.forces",
        "p12e.armed-force.contingents", "p12e.armed-force.relevant-person-references",
        "p12e.contingent-manpower.states", "p12e.armed-force-spatial.positions", "p12e.conflicts",
        "p12e.wars", "p12e.battles", "p12e.institution.records", "p12e.office.records",
        "p12e.office.incumbencies", "p12e.office.tenures", "p12e.faction.records",
        "p12e.faction.affiliations", "p12e.political-claim.records", "p12e.political-claim.recognitions",
        "p12e.political-support.relations", "p12e.property.ownership", "p12e.property.transfer-history",
        "p12e.estate.records", "p12f.actor-choice-inputs", "p12f.scheduled-directives",
        "p12f.travel-parties", "p12f.expeditions"
    });

    internal static IReadOnlyList<IReadOnlyList<string>> FixedAliasGroups { get; } = ReadOnlyGroups(new[]
    {
        new[] { "p12b.justice-p18-receipts", "p12b.justice-records" },
        new[] { "p12c.runtime-id-allocator.decisions", "p12c.runtime-id-allocator.events", "p12c.runtime-id-allocator.travel-parties" },
        new[] { "p12c.runtime-identities.cities", "p12c.runtime-identities.explorable-sites", "p12c.runtime-identities.local-connections", "p12c.runtime-identities.local-places", "p12c.runtime-identities.locations", "p12c.runtime-identities.notable-items", "p12c.runtime-identities.npcs", "p12c.runtime-identities.routes" },
        new[] { "p12d.legacy-spatial-network.locations", "p12d.legacy-spatial-network.routes" },
        new[] { "p12d.person.materialization-binding", "p12d.person.membership" },
        new[] { "p12e.armed-force.contingents", "p12e.armed-force.forces", "p12e.armed-force.relevant-person-references" },
        new[] { "p12e.faction.affiliations", "p12e.faction.records" },
        new[] { "p12e.office.incumbencies", "p12e.office.records", "p12e.office.tenures" },
        new[] { "p12e.political-claim.recognitions", "p12e.political-claim.records" },
        new[] { "p12e.property.ownership", "p12e.property.transfer-history" },
        new[] { "p8a.hexes", "p8a.locations", "p8a.scale-context", "p8b.crossings" }
    });

    internal static IReadOnlyList<IReadOnlyList<string>> NpcAliasFamilies { get; } = ReadOnlyGroups(new[]
    {
        new[] { "p12b.npc-current-action/", "p12b.npc-life-state/", "p12b.npc-residence/", "p12b.npc-status-crime-state/", "p12f.npc-travel-state/" },
        new[] { "p12f.adventure-intel.access/", "p12f.adventure-intel.common-resources/", "p12f.adventure-intel.notable-items/", "p12f.adventure-intel.opposition/" },
        new[] { "p12f.commercial-knowledge.liquidity/", "p12f.commercial-knowledge.markets/", "p12f.commercial-knowledge.share-receipts/" },
        new[] { "p12f.local-topology-knowledge.connections/", "p12f.local-topology-knowledge.places/" },
        new[] { "p12f.spatial-knowledge.locations/", "p12f.spatial-knowledge.routes/" }
    });

    internal static IReadOnlyList<string> CityPopulationAliasFamily { get; } = ReadOnly(new[]
    {
        "p12d.city-population.aggregate/", "p12d.city-population.operation-receipts/"
    });

    internal static bool IsCompleteSnapshot(IReadOnlyList<OwnerSectionCensusSnapshot> snapshots)
    {
        if (snapshots == null) return false;
        Dictionary<string, OwnerSectionCensusSnapshot> byId =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        List<HashSet<string>> npcFamilies = NewFamilies(NpcPrefixes.Count);
        List<HashSet<string>> cityFamilies = NewFamilies(CityPrefixes.Count);
        HashSet<string> npcIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> nonPersonNpcIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (OwnerSectionCensusSnapshot snapshot in snapshots)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.SectionId)
                || snapshot.SchemaVersion <= 0 || snapshot.Cardinality < 0 || snapshot.Revision < 0L
                || snapshot.OwnerInstanceIdentity == null || !byId.TryAdd(snapshot.SectionId, snapshot))
                return false;

            string id = snapshot.SectionId;
            if (ContainsOrdinal(FixedRequired, id))
            {
                if (snapshot.Role != OwnerSectionRole.Required) return false;
                continue;
            }
            if (ContainsOrdinal(FixedEmpty, id))
            {
                if (snapshot.Role != OwnerSectionRole.ExplicitlyEmpty || snapshot.Cardinality != 0)
                    return false;
                continue;
            }

            int family = FindFamily(id, NpcPrefixes);
            if (family >= 0)
            {
                if (snapshot.Role != OwnerSectionRole.Required
                    || !TryDynamicKey(id, NpcPrefixes[family], out string key)
                    || !npcFamilies[family].Add(key)) return false;
                npcIds.Add(key);
                continue;
            }
            family = FindFamily(id, CityPrefixes);
            if (family >= 0)
            {
                if (snapshot.Role != OwnerSectionRole.Required
                    || !TryCityKey(id, CityPrefixes[family], out string key)
                    || !cityFamilies[family].Add(key)) return false;
                cityIds.Add(key);
                continue;
            }
            if (TryDynamicKey(id, "p12b.npc-residence/", out string npcKey))
            {
                if (snapshot.Role != OwnerSectionRole.Required || !nonPersonNpcIds.Add(npcKey)) return false;
                continue;
            }
            if (TryEncodedKey(id, "p12b.person-life-residence/", out string personKey))
            {
                if (snapshot.Role != OwnerSectionRole.Required || !personIds.Add(personKey)) return false;
                continue;
            }
            return false;
        }

        foreach (string sectionId in FixedRequired)
            if (!byId.ContainsKey(sectionId)) return false;
        foreach (string sectionId in FixedEmpty)
            if (!byId.ContainsKey(sectionId)) return false;
        if (npcFamilies.Count == 0 || cityFamilies.Count == 0
            || npcFamilies[0].Count == 0 || cityFamilies[0].Count == 0) return false;
        foreach (HashSet<string> family in npcFamilies)
            if (!family.SetEquals(npcFamilies[0])) return false;
        foreach (HashSet<string> family in cityFamilies)
            if (!family.SetEquals(cityFamilies[0])) return false;
        foreach (string id in nonPersonNpcIds)
            if (!npcIds.Contains(id)) return false;

        long expected = 61L + (long)NpcPrefixes.Count * npcIds.Count
            + nonPersonNpcIds.Count + personIds.Count + (long)CityPrefixes.Count * cityIds.Count;
        if (expected != snapshots.Count) return false;

        return HasExactAliasIdentity(byId);
    }

    private static List<HashSet<string>> NewFamilies(int count)
    {
        List<HashSet<string>> result = new List<HashSet<string>>(count);
        for (int i = 0; i < count; i++) result.Add(new HashSet<string>(StringComparer.Ordinal));
        return result;
    }

    private static bool HasExactAliasIdentity(Dictionary<string, OwnerSectionCensusSnapshot> byId)
    {
        Dictionary<string, object> identitiesByAlias = new Dictionary<string, object>(StringComparer.Ordinal);
        Dictionary<object, string> aliasByIdentity = new Dictionary<object, string>(ReferenceComparer.Instance);
        foreach (KeyValuePair<string, OwnerSectionCensusSnapshot> pair in byId)
        {
            string alias = ExpectedAliasGroupForSection(pair.Key);
            object identity = pair.Value.OwnerInstanceIdentity;
            if (alias == null)
            {
                if (aliasByIdentity.ContainsKey(identity)) return false;
                aliasByIdentity.Add(identity, null);
                continue;
            }

            if (identitiesByAlias.TryGetValue(alias, out object existing)
                && !ReferenceEquals(existing, identity)) return false;
            identitiesByAlias[alias] = identity;
            if (aliasByIdentity.TryGetValue(identity, out string existingAlias)
                && !string.Equals(existingAlias, alias, StringComparison.Ordinal)) return false;
            aliasByIdentity[identity] = alias;
        }

        foreach (KeyValuePair<string, string> alias in EnumerateExpectedAliasSections(byId))
            if (!identitiesByAlias.ContainsKey(alias.Value)) return false;
        return true;
    }

    private static IEnumerable<KeyValuePair<string, string>> EnumerateExpectedAliasSections(
        Dictionary<string, OwnerSectionCensusSnapshot> byId)
    {
        foreach (string id in byId.Keys)
        {
            string alias = ExpectedAliasGroupForSection(id);
            if (alias != null) yield return new KeyValuePair<string, string>(id, alias);
        }
    }

    internal static string ExpectedAliasGroupForSection(string id)
    {
        foreach (IReadOnlyList<string> group in FixedAliasGroups)
            if (ContainsOrdinal(group, id)) return "alias/" + group[0];
        foreach (IReadOnlyList<string> group in NpcAliasFamilies)
            foreach (string prefix in group)
                if (TryDynamicKey(id, prefix, out string key)) return "alias/" + group[0] + key;
        foreach (string prefix in CityPopulationAliasFamily)
            if (TryEncodedKey(id, prefix, out string key))
                return "alias/" + CityPopulationAliasFamily[0]
                    + key.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + key;
        return null;
    }

    private static bool ContainsOrdinal(IReadOnlyList<string> values, string value)
    {
        for (int i = 0; i < values.Count; i++)
            if (string.Equals(values[i], value, StringComparison.Ordinal)) return true;
        return false;
    }

    private static int FindFamily(string id, IReadOnlyList<string> prefixes)
    {
        for (int i = 0; i < prefixes.Count; i++)
            if (id.StartsWith(prefixes[i], StringComparison.Ordinal)) return i;
        return -1;
    }

    private static bool TryDynamicKey(string id, string prefix, out string key)
    {
        key = null;
        if (id == null || !id.StartsWith(prefix, StringComparison.Ordinal)) return false;
        key = id.Substring(prefix.Length);
        return key.Length > 0 && !key.Contains("/") && !string.IsNullOrWhiteSpace(key);
    }

    private static bool TryEncodedKey(string id, string prefix, out string key)
    {
        key = null;
        if (id == null || !id.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string encoded = id.Substring(prefix.Length);
        int separator = encoded.IndexOf(':');
        if (separator <= 0 || !int.TryParse(encoded.Substring(0, separator),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out int length)) return false;
        key = encoded.Substring(separator + 1);
        return length == key.Length && key.Length > 0 && !key.Contains("/");
    }

    private static bool TryCityKey(string id, string prefix, out string key) =>
        string.Equals(prefix, "p12b.city.important-npcs/", StringComparison.Ordinal)
            ? TryDynamicKey(id, prefix, out key) : TryEncodedKey(id, prefix, out key);

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new ReferenceComparer();
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }

    private static IReadOnlyList<string> ReadOnly(string[] values) =>
        new ReadOnlyCollection<string>((string[])values.Clone());

    private static IReadOnlyList<IReadOnlyList<string>> ReadOnlyGroups(string[][] groups)
    {
        IReadOnlyList<string>[] result = new IReadOnlyList<string>[groups.Length];
        for (int index = 0; index < groups.Length; index++) result[index] = ReadOnly(groups[index]);
        return new ReadOnlyCollection<IReadOnlyList<string>>(result);
    }
}
