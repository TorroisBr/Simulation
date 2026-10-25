using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Decodes the complete admitted NPC image set in deterministic identity order
/// before a caller can publish any staged runtime projection.
/// </summary>
internal static class P12ADailyV1NpcOwnerBatchDecoder
{
    internal static bool TryDecode(
        P12AContinuationImage image,
        out IReadOnlyList<P12DNpcDRow> dRows,
        out IReadOnlyList<P12DNpcFRow> fRows,
        out P12AResult result)
    {
        dRows = null;
        fRows = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (image == null || image.Sections == null
            || !TryGetUnique(image, P12CRuntimeIdentitySectionContract.NpcsSectionId,
                out P12AOwnerSection npcsSection, out result)
            || !TryGetUnique(image, P12CRuntimeIdentitySectionContract.CitiesSectionId,
                out P12AOwnerSection citiesSection, out result)
            || !TryGetUnique(image, P12CRuntimeIdentitySectionContract.LocationsSectionId,
                out P12AOwnerSection locationsSection, out result)
            || !TryGetUnique(image, P12CRuntimeIdentitySectionContract.RoutesSectionId,
                out P12AOwnerSection routesSection, out result)
            || !P12ARuntimeIdentityOwnerCodec.TryDecode(
                new[] { npcsSection, citiesSection, locationsSection, routesSection },
                out P12CRuntimeIdentityOwnerSnapshot identities, out result)
            || !TryGetUnique(image, DailyV1SelectedOwnerSectionCensusContract.PersonMembershipSectionId,
                out P12AOwnerSection membership, out result)
            || !TryGetUnique(image, DailyV1SelectedOwnerSectionCensusContract.PersonMaterializationBindingSectionId,
                out P12AOwnerSection bindings, out result)
            || !P12APersonStoreOwnerCodec.TryDecode(membership, bindings,
                out PersonStoreOwnerSnapshot persons, out result))
            return false;

        return TryDecode(image, identities, persons, out dRows, out fRows, out result);
    }

    internal static bool TryDecode(
        P12AContinuationImage image,
        P12CRuntimeIdentityOwnerSnapshot identities,
        PersonStoreOwnerSnapshot persons,
        out IReadOnlyList<P12DNpcDRow> dRows,
        out IReadOnlyList<P12DNpcFRow> fRows,
        out P12AResult result)
    {
        dRows = null;
        fRows = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (image == null || image.Sections == null || identities?.IdentityValues == null || persons == null)
            return false;

        List<string> runtimeIds = new List<string>();
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> identity in identities.IdentityValues)
        {
            if (!string.Equals(identity.Key, "npc", StringComparison.Ordinal)) continue;
            if (string.IsNullOrWhiteSpace(identity.Value) || !uniqueIds.Add(identity.Value))
            {
                result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                    P12CRuntimeIdentitySectionContract.NpcsSectionId, identity.Value);
                return false;
            }
            runtimeIds.Add(identity.Value);
        }
        if (runtimeIds.Count == 0)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                P12CRuntimeIdentitySectionContract.NpcsSectionId);
            return false;
        }
        runtimeIds.Sort(StringComparer.Ordinal);

        List<P12DNpcDRow> stagedD = new List<P12DNpcDRow>(runtimeIds.Count);
        List<P12DNpcFRow> stagedF = new List<P12DNpcFRow>(runtimeIds.Count);
        foreach (string runtimeId in runtimeIds)
        {
            if (!P12ANpcOwnerDecoder.TryDecode(image, runtimeId, persons,
                    out P12DNpcDRow dRow, out P12DNpcFRow fRow, out result))
            {
                dRows = null;
                fRows = null;
                return false;
            }
            stagedD.Add(dRow);
            stagedF.Add(fRow);
        }

        dRows = new ReadOnlyCollection<P12DNpcDRow>(stagedD);
        fRows = new ReadOnlyCollection<P12DNpcFRow>(stagedF);
        result = null;
        return true;
    }

    private static bool TryGetUnique(P12AContinuationImage image, string id,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, id);
        int count = 0;
        foreach (P12AOwnerSection candidate in image.Sections)
        {
            if (candidate == null || !string.Equals(candidate.Id, id, StringComparison.Ordinal)) continue;
            section = candidate;
            count++;
        }
        if (count != 1)
        {
            section = null;
            return false;
        }
        result = null;
        return true;
    }
}
