using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Encodes the selected profile's fixed exact-empty owner sections.</summary>
internal static class P12AExactEmptyOwnerCodec
{
    internal static bool TryEncode(IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSections,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (exactOwnerSections == null) return false;

        HashSet<string> expectedIds = new HashSet<string>(
            P12ADailyV1Census.FixedEmptySectionIds, StringComparer.Ordinal);
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        List<P12AOwnerSection> encoded = new List<P12AOwnerSection>(expectedIds.Count);
        foreach (OwnerSectionCensusSnapshot snapshot in exactOwnerSections)
        {
            if (snapshot == null)
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue);
                return false;
            }
            if (!expectedIds.Contains(snapshot.SectionId)) continue;
            if (!seenIds.Add(snapshot.SectionId))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, snapshot.SectionId);
                return false;
            }
            if (snapshot.OwnerInstanceIdentity == null
                || snapshot.Role != OwnerSectionRole.ExplicitlyEmpty
                || snapshot.Cardinality != 0 || snapshot.Revision < 0
                || snapshot.SchemaVersion <= 0)
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue, snapshot.SectionId);
                return false;
            }
            encoded.Add(new P12AOwnerSection(snapshot.SectionId, snapshot.SchemaVersion, 0,
                OwnerSectionRole.ExplicitlyEmpty,
                P12ADailyV1Census.ExpectedAliasGroupForSection(snapshot.SectionId),
                snapshot.Revision, Array.Empty<P12AOwnerRow>()));
        }

        if (!seenIds.SetEquals(expectedIds))
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
