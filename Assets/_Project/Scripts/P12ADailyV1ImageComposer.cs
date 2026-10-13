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
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
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
            result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }
        if (!census.MatchesImage(candidate) || !census.Manifest.Matches(candidate.Sections, out result))
        {
            image = null;
            manifest = null;
            if (result == null) result = new P12AResult(P12AResultCode.InvalidOwnerValue);
            return false;
        }

        image = candidate;
        manifest = census.Manifest;
        result = null;
        return true;
    }
}
