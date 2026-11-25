using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Builds the immutable selected-profile image from typed owner sections and
/// the exact live census vector captured at the same completed boundary.
/// </summary>
internal static class P12ADailyV1ImageComposer
{
    internal static bool TryCompose(P12AContinuationHeader header,
        P12ACausalRootPayload causalRoots,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSections,
        IReadOnlyList<P12AOwnerSection> encodedRequiredOwnerSections,
        out P12AContinuationImage image,
        out P12ADailyV1SectionManifest manifest,
        out P12AResult result)
    {
        image = null;
        manifest = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p12a.image-input");
        if (header == null || !header.IsValid || causalRoots == null
            || !causalRoots.IsValidFor(header) || exactOwnerSections == null
            || encodedRequiredOwnerSections == null)
            return false;
        if (!P12ADailyV1Census.TryCreate(exactOwnerSections,
                out P12ADailyV1Census census, out result)
            || !P12AExactEmptyOwnerCodec.TryEncode(exactOwnerSections,
                out IReadOnlyList<P12AOwnerSection> exactEmptySections, out result))
            return false;

        HashSet<string> emptyIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in exactEmptySections) emptyIds.Add(section.Id);
        List<P12AOwnerSection> sections = new List<P12AOwnerSection>(
            encodedRequiredOwnerSections.Count + exactEmptySections.Count);
        HashSet<string> suppliedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in encodedRequiredOwnerSections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.Id)
                || emptyIds.Contains(section.Id) || !suppliedIds.Add(section.Id))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, section?.Id);
                return false;
            }
            sections.Add(section);
        }
        sections.AddRange(exactEmptySections);
        sections.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));

        P12AContinuationImage candidate;
        try { candidate = new P12AContinuationImage(header, causalRoots, sections); }
        catch (ArgumentException)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p12a.image-constructor");
            return false;
        }
        if (!census.MatchesImage(candidate))
        {
            image = null;
            manifest = null;
            string mismatch = FindFirstImageCensusMismatch(census, candidate);
            int mismatchSeparator = mismatch?.IndexOf('#') ?? -1;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                "p12a.census-image-mismatch"
                    + (mismatchSeparator >= 0 ? "." + mismatch.Substring(mismatchSeparator + 1) : string.Empty),
                mismatchSeparator >= 0 ? mismatch.Substring(0, mismatchSeparator) : mismatch);
            return false;
        }
        if (!census.Manifest.Matches(candidate.Sections, out result))
        {
            image = null;
            manifest = null;
            if (result == null)
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    "p12a.census-manifest-mismatch");
            return false;
        }

        image = candidate;
        manifest = census.Manifest;
        result = null;
        return true;
    }

    private static string FindFirstImageCensusMismatch(
        P12ADailyV1Census census,
        P12AContinuationImage image)
    {
        if (census == null || image == null) return null;
        IReadOnlyList<P12ACensusSectionValue> expected = census.Sections;
        IReadOnlyList<P12AOwnerSection> actual = image.Sections;
        int commonCount = Math.Min(expected.Count, actual.Count);
        for (int index = 0; index < commonCount; index++)
        {
            P12ACensusSectionValue expectedSection = expected[index];
            P12AOwnerSection actualSection = actual[index];
            if (actualSection == null) return expectedSection?.Id + "#missing";
            if (expectedSection.Id != actualSection.Id)
                return expectedSection.Id + "=>" + actualSection.Id + "#identity";
            if (expectedSection.SchemaVersion != actualSection.Major || actualSection.Minor != 0)
                return actualSection.Id + "#schema";
            if (expectedSection.Role != actualSection.Role) return actualSection.Id + "#role";
            if (expectedSection.Cardinality != actualSection.Rows.Count) return actualSection.Id + "#cardinality";
            if (expectedSection.Revision != actualSection.Revision) return actualSection.Id + "#revision";
            if (expectedSection.AliasGroup != actualSection.AliasGroup) return actualSection.Id + "#alias";
        }
        return expected.Count == actual.Count ? null
            : commonCount < expected.Count ? expected[commonCount].Id + "#missing" : actual[commonCount]?.Id + "#unexpected";
    }
}
