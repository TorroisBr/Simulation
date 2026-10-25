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
        if (!P12DCityOwnerSetImageDecoder.TryDecode(sections,
                out IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> decoded, out result))
            return false;

        List<P12DCityRootOwnerSnapshot> adapted =
            new List<P12DCityRootOwnerSnapshot>(decoded.Count);
        foreach (DailyV1P12DCityCoreOwnerHydrationInput value in decoded)
            adapted.Add(P12ACityOwnerCodec.FromCoreValue(value));

        snapshots = new ReadOnlyCollection<P12DCityRootOwnerSnapshot>(adapted);
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
