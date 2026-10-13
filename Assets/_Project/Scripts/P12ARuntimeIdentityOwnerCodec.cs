using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Explicit Daily-v1 codec for the admitted RuntimeIdentityRegistry indexes.</summary>
internal static class P12ARuntimeIdentityOwnerCodec
{
    private const string PayloadContract = "p12a.runtime-identities/v1";

    private static readonly IdentityFamily[] AdmittedFamilies =
    {
        new IdentityFamily("npc", RuntimeIdentityRegistryCensusProvider.NpcsSectionId),
        new IdentityFamily("city", RuntimeIdentityRegistryCensusProvider.CitiesSectionId),
        new IdentityFamily("location", RuntimeIdentityRegistryCensusProvider.LocationsSectionId),
        new IdentityFamily("route", RuntimeIdentityRegistryCensusProvider.RoutesSectionId)
    };

    internal static bool TryEncode(P12CRuntimeIdentityOwnerSnapshot snapshot,
        out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result)
    {
        sections = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (!TryValidate(snapshot, out Dictionary<string, List<string>> idsByFamily, out result)) return false;

        try
        {
            List<P12AOwnerSection> encoded = new List<P12AOwnerSection>(AdmittedFamilies.Length);
            foreach (IdentityFamily family in AdmittedFamilies)
            {
                List<string> ids = idsByFamily[family.FamilyId];
                List<P12AOwnerRow> rows = new List<P12AOwnerRow>(ids.Count);
                foreach (string id in ids)
                {
                    using (MemoryStream stream = new MemoryStream())
                    using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                    {
                        P12AWire.WriteString(writer, PayloadContract);
                        P12AWire.WriteString(writer, family.FamilyId);
                        P12AWire.WriteString(writer, id);
                        writer.Flush();
                        if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                        {
                            result = new P12AResult(P12AResultCode.LimitExceeded, family.SectionId, id);
                            return false;
                        }
                        rows.Add(new P12AOwnerRow(id, snapshot.Revision, stream.ToArray()));
                    }
                }
                encoded.Add(new P12AOwnerSection(family.SectionId,
                    RuntimeIdentityRegistryCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                    P12ADailyV1Census.ExpectedAliasGroupForSection(family.SectionId),
                    snapshot.Revision, rows));
            }
            sections = encoded.AsReadOnly();
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            sections = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue);
            return false;
        }
    }

    internal static bool TryDecode(IReadOnlyList<P12AOwnerSection> sections,
        out P12CRuntimeIdentityOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (sections == null || sections.Count != AdmittedFamilies.Length) return false;
        Dictionary<string, P12AOwnerSection> sectionsById = new Dictionary<string, P12AOwnerSection>(StringComparer.Ordinal);
        foreach (P12AOwnerSection section in sections)
            if (section == null || !sectionsById.TryAdd(section.Id, section))
            { result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, section?.Id); return false; }

        long? revision = null;
        List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (IdentityFamily family in AdmittedFamilies)
        {
            if (!sectionsById.TryGetValue(family.SectionId, out P12AOwnerSection section)
                || !IsSection(section, family))
            {
                result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, family.SectionId);
                return false;
            }
            if (revision.HasValue && revision.Value != section.Revision)
            { result = new P12AResult(P12AResultCode.InvalidOwnerValue, family.SectionId); return false; }
            revision = section.Revision;

            string prior = null;
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != section.Revision || row.PayloadLength < 0
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                    || !TryReadIdentity(row, family, out string runtimeId)
                    || row.OwnerKey != runtimeId
                    || (prior != null && StringComparer.Ordinal.Compare(prior, runtimeId) >= 0)
                    || !runtimeIds.Add(runtimeId))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        family.SectionId, row?.OwnerKey);
                    return false;
                }
                prior = runtimeId;
                values.Add(new KeyValuePair<string, string>(family.FamilyId, runtimeId));
            }
        }

        P12CRuntimeIdentityOwnerSnapshot candidate = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion, revision ?? 0L, values);
        if (!TryValidate(candidate, out _, out result)) return false;
        snapshot = candidate;
        result = null;
        return true;
    }

    private static bool TryValidate(P12CRuntimeIdentityOwnerSnapshot snapshot,
        out Dictionary<string, List<string>> idsByFamily, out P12AResult result)
    {
        idsByFamily = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (snapshot == null || snapshot.SchemaVersion != P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0 || snapshot.IdentityValues == null
            || snapshot.IdentityValues.Count > P12AContinuationCodec.MaxRows) return false;

        Dictionary<string, List<string>> values = AdmittedFamilies.ToDictionary(
            family => family.FamilyId, _ => new List<string>(), StringComparer.Ordinal);
        HashSet<string> familyIds = new HashSet<string>(values.Keys, StringComparer.Ordinal);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> identity in snapshot.IdentityValues)
        {
            if (identity.Key == "site" || identity.Key == "local-place"
                || identity.Key == "local-connection" || identity.Key == "notable-item")
            {
                result = new P12AResult(P12AResultCode.UnsupportedProfile,
                    EmptySectionForFamily(identity.Key), identity.Value);
                return false;
            }
            if (!familyIds.Contains(identity.Key) || string.IsNullOrWhiteSpace(identity.Value)
                || !runtimeIds.Add(identity.Value)) return false;
            try
            {
                P12AWire.GetEncodedStringByteCount(identity.Key);
                P12AWire.GetEncodedStringByteCount(identity.Value);
            }
            catch (Exception exception) when (IsWireException(exception))
            {
                result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                    ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue);
                return false;
            }
            values[identity.Key].Add(identity.Value);
        }

        long total = 0;
        foreach (List<string> familyValues in values.Values)
        {
            familyValues.Sort(StringComparer.Ordinal);
            total += familyValues.Count;
        }
        if (snapshot.Revision < total) return false;
        idsByFamily = values;
        result = null;
        return true;
    }

    private static bool IsSection(P12AOwnerSection section, IdentityFamily family)
        => section != null && section.Id == family.SectionId
            && section.Major == RuntimeIdentityRegistryCensusProvider.SchemaVersion && section.Minor == 0
            && section.Role == OwnerSectionRole.Required
            && section.AliasGroup == P12ADailyV1Census.ExpectedAliasGroupForSection(family.SectionId)
            && section.Revision >= 0 && section.Rows.Count <= P12AContinuationCodec.MaxRows;

    private static bool TryReadIdentity(P12AOwnerRow row, IdentityFamily family, out string runtimeId)
    {
        runtimeId = null;
        byte[] payload = row.Payload;
        if (payload == null || payload.Length != row.PayloadLength) return false;
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!ReadRequired(reader, out string contract) || contract != PayloadContract
                || !ReadRequired(reader, out string familyId) || familyId != family.FamilyId
                || !ReadRequired(reader, out runtimeId) || stream.Position != stream.Length) return false;
            return true;
        }
    }

    private static bool ReadRequired(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static string EmptySectionForFamily(string familyId)
        => familyId == "site" ? RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId
            : familyId == "local-place" ? RuntimeIdentityRegistryCensusProvider.LocalPlacesSectionId
            : familyId == "local-connection" ? RuntimeIdentityRegistryCensusProvider.LocalConnectionsSectionId
            : RuntimeIdentityRegistryCensusProvider.NotableItemsSectionId;

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is EncoderFallbackException || exception is DecoderFallbackException;

    private sealed class IdentityFamily
    {
        internal IdentityFamily(string familyId, string sectionId)
        { FamilyId = familyId; SectionId = sectionId; }
        internal string FamilyId { get; }
        internal string SectionId { get; }
    }
}
