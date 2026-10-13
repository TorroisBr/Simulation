using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>Composes the complete selected Daily-v1 City owner family set.</summary>
internal static class P12ACityOwnerSetCodec
{
    private const int SectionsPerCity = 4;

    internal static bool TryDecode(IReadOnlyList<P12AOwnerSection> sections,
        out IReadOnlyList<P12DCityRootOwnerSnapshot> snapshots, out P12AResult result)
    {
        snapshots = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (sections == null || sections.Count == 0 || sections.Count % SectionsPerCity != 0
            || sections.Count / SectionsPerCity > P12AContinuationCodec.MaxSections / SectionsPerCity)
            return false;

        Dictionary<string, P12AOwnerSection> byId = new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in sections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.Id) || !byId.TryAdd(section.Id, section))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, section?.Id);
                return false;
            }
        }

        List<string> cityIds = new List<string>();
        HashSet<string> uniqueCityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in sections)
        {
            if (!section.Id.StartsWith(CityNpcPresenceCensusProvider.SectionIdPrefix, StringComparison.Ordinal))
                continue;
            string cityId = section.Id.Substring(CityNpcPresenceCensusProvider.SectionIdPrefix.Length);
            if (string.IsNullOrWhiteSpace(cityId) || cityId.Contains("/") || cityId.IndexOf('\0') >= 0
                || !uniqueCityIds.Add(cityId))
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue, section.Id);
                return false;
            }
            cityIds.Add(cityId);
        }
        if (cityIds.Count == 0 || cityIds.Count * SectionsPerCity != sections.Count)
        {
            result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
            return false;
        }
        cityIds.Sort(StringComparer.Ordinal);

        List<P12DCityRootOwnerSnapshot> decoded = new List<P12DCityRootOwnerSnapshot>(cityIds.Count);
        HashSet<string> consumed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string cityId in cityIds)
        {
            string cityKey;
            try { cityKey = P12ACityOwnerCodec.EncodeCityKeyForSection(cityId); }
            catch (ArgumentException)
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    CityNpcPresenceCensusProvider.SectionIdFor(cityId), cityId);
                return false;
            }
            string presenceId = CityNpcPresenceCensusProvider.SectionIdFor(cityId);
            string marketId = CityMarketCensusProvider.SectionIdPrefix + cityKey;
            string populationId = SettlementPopulationCensusProvider.AggregateSectionPrefix + cityKey;
            string receiptsId = SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + cityKey;
            if (!byId.TryGetValue(presenceId, out P12AOwnerSection presence)
                || !byId.TryGetValue(marketId, out P12AOwnerSection market)
                || !byId.TryGetValue(populationId, out P12AOwnerSection population)
                || !byId.TryGetValue(receiptsId, out P12AOwnerSection receipts)
                || !consumed.Add(presenceId) || !consumed.Add(marketId)
                || !consumed.Add(populationId) || !consumed.Add(receiptsId))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, presenceId, cityId);
                return false;
            }
            if (!P12ACityOwnerCodec.TryDecode(presence, market, population, receipts,
                    out P12DCityRootOwnerSnapshot snapshot, out result))
            {
                result = result ?? new P12AResult(P12AResultCode.InvalidOwnerValue, presenceId, cityId);
                return false;
            }
            decoded.Add(snapshot);
        }
        if (consumed.Count != sections.Count)
        {
            result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
            return false;
        }
        snapshots = new ReadOnlyCollection<P12DCityRootOwnerSnapshot>(decoded);
        result = null;
        return true;
    }

    internal static bool TryEncode(IReadOnlyList<P12DCityRootOwnerSnapshot> snapshots,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSections,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (snapshots == null || exactOwnerSections == null
            || snapshots.Count == 0
            || snapshots.Count > P12AContinuationCodec.MaxSections / SectionsPerCity
            || !P12ADailyV1Census.TryCreate(exactOwnerSections,
                out P12ADailyV1Census census, out result))
            return false;

        Dictionary<string, P12ACensusSectionValue> expectedById = census.Sections
            .Where(value => P12ADailyV1Census.CitySectionPrefixes.Any(prefix =>
                value.Id.StartsWith(prefix, StringComparison.Ordinal)))
            .ToDictionary(value => value.Id, StringComparer.Ordinal);
        if (expectedById.Count != snapshots.Count * SectionsPerCity)
        {
            result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
            return false;
        }

        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
        List<P12AOwnerSection> encoded = new List<P12AOwnerSection>(expectedById.Count);
        foreach (P12DCityRootOwnerSnapshot snapshot in snapshots)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.CityRuntimeId)
                || !cityIds.Add(snapshot.CityRuntimeId))
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    snapshot == null || string.IsNullOrWhiteSpace(snapshot.CityRuntimeId)
                        ? CityNpcPresenceCensusProvider.SectionIdPrefix
                        : CityNpcPresenceCensusProvider.SectionIdFor(snapshot.CityRuntimeId));
                return false;
            }
            if (!P12ACityOwnerCodec.TryEncode(snapshot,
                    out IReadOnlyList<P12AOwnerSection> citySections, out result))
                return false;
            if (citySections == null || citySections.Count != SectionsPerCity)
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    CityNpcPresenceCensusProvider.SectionIdFor(snapshot.CityRuntimeId),
                    snapshot.CityRuntimeId);
                return false;
            }

            foreach (P12AOwnerSection section in citySections)
            {
                if (section == null || !sectionIds.Add(section.Id)
                    || !expectedById.TryGetValue(section.Id, out P12ACensusSectionValue expected))
                {
                    result = new P12AResult(P12AResultCode.MissingOrDuplicateSection,
                        section?.Id, snapshot.CityRuntimeId);
                    return false;
                }
                if (section.Major != expected.SchemaVersion || section.Minor != 0
                    || section.Role != expected.Role || section.Revision != expected.Revision
                    || section.Rows.Count != expected.Cardinality
                    || !string.Equals(section.AliasGroup, expected.AliasGroup, StringComparison.Ordinal))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        section.Id, snapshot.CityRuntimeId);
                    return false;
                }
                encoded.Add(section);
            }
        }

        if (sectionIds.Count != expectedById.Count || !sectionIds.SetEquals(expectedById.Keys))
        {
            result = new P12AResult(P12AResultCode.MissingOrDuplicateSection);
            return false;
        }

        encoded.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
        sections = new ReadOnlyCollection<P12AOwnerSection>(encoded);
        result = null;
        return true;
    }
}
