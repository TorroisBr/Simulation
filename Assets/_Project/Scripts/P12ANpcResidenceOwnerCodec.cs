using System;
using System.IO;

/// <summary>Detached values owned by one unbound NPC's residence section.</summary>
internal sealed class P12ANpcResidenceOwnerValue
{
    internal P12ANpcResidenceOwnerValue(string runtimeId, string residenceSettlementRuntimeId, long revision)
    {
        RuntimeId = runtimeId;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal string ResidenceSettlementRuntimeId { get; }
    internal long Revision { get; }
}

/// <summary>Explicit Daily-v1 codec for conditional per-NPC residence ownership.</summary>
internal static class P12ANpcResidenceOwnerCodec
{
    private const string PayloadContract = "p12a.npc-residence/v1";

    /// <summary>
    /// Encodes the NPC-owned residence only when the captured NPC is not bound to a Person.
    /// A successful call with a null section means the Person owner supplies this fact.
    /// </summary>
    internal static bool TryEncode(P12DNpcDRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            NpcLifecycleCensusProvider.ResidenceSectionPrefix);
        if (source == null || string.IsNullOrWhiteSpace(source.RuntimeId)
            || source.RuntimeId.Contains("/") || source.ResidenceRevision < 0L
            || (source.PersonIdValue != null && string.IsNullOrWhiteSpace(source.PersonIdValue))) return false;
        if (source.PersonIdValue != null)
        {
            result = null;
            return true;
        }
        return TryEncode(new P12ANpcResidenceOwnerValue(source.RuntimeId,
            source.ResidenceSettlementRuntimeId, source.ResidenceRevision), out section, out result);
    }

    internal static bool TryEncode(P12ANpcResidenceOwnerValue value,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            NpcLifecycleCensusProvider.ResidenceSectionPrefix);
        if (!TryValidate(value, out string sectionId)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(value.RuntimeId);
            P12AWire.GetEncodedStringByteCount(value.ResidenceSettlementRuntimeId);
            P12AWire.GetEncodedStringByteCount(
                P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId));
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, value.RuntimeId);
                P12AWire.WriteString(writer, value.ResidenceSettlementRuntimeId);
                writer.Write(value.Revision);
                writer.Flush();
                if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                    return false;
                }
                section = new P12AOwnerSection(sectionId, NpcLifecycleCensusProvider.SchemaVersion, 0,
                    OwnerSectionRole.Required,
                    P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId), value.Revision,
                    new[] { new P12AOwnerRow(value.RuntimeId, value.Revision, stream.ToArray()) });
            }
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, sectionId, value.RuntimeId);
            section = null;
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12ANpcResidenceOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? NpcLifecycleCensusProvider.ResidenceSectionPrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id) || section.Rows.Count != 1
            || !section.Id.StartsWith(NpcLifecycleCensusProvider.ResidenceSectionPrefix, StringComparison.Ordinal)
            || section.Major != NpcLifecycleCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required
            || !string.Equals(section.AliasGroup,
                P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id), StringComparison.Ordinal)
            || section.Revision < 0L) return false;

        P12AOwnerRow row = section.Rows[0];
        if (row == null || row.Revision != section.Revision || row.PayloadLength < 0
            || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
            || string.IsNullOrWhiteSpace(row.OwnerKey)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(row.OwnerKey);
            if (section.Id != NpcLifecycleCensusProvider.SectionIdFor(row.OwnerKey, residence: true)) return false;
            byte[] payload = row.Payload;
            if (payload == null || payload.Length != row.PayloadLength) return false;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!ReadContract(reader) || !ReadRequiredString(reader, out string runtimeId)
                    || !P12AWire.ReadString(reader, out string residenceId)) return false;
                long revision = reader.ReadInt64();
                P12ANpcResidenceOwnerValue candidate = new P12ANpcResidenceOwnerValue(
                    runtimeId, residenceId, revision);
                if (stream.Position != stream.Length || row.OwnerKey != runtimeId
                    || section.Revision != revision || !TryValidate(candidate, out _)) return false;
                value = candidate;
                result = null;
                return true;
            }
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, section.Id, row.OwnerKey);
            return false;
        }
    }

    private static bool TryValidate(P12ANpcResidenceOwnerValue value, out string sectionId)
    {
        sectionId = null;
        if (value == null || string.IsNullOrWhiteSpace(value.RuntimeId)
            || value.RuntimeId.Contains("/") || value.Revision < 0L
            || (value.ResidenceSettlementRuntimeId != null
                && string.IsNullOrWhiteSpace(value.ResidenceSettlementRuntimeId))) return false;
        try { sectionId = NpcLifecycleCensusProvider.SectionIdFor(value.RuntimeId, residence: true); }
        catch (ArgumentException) { return false; }
        return true;
    }

    private static bool ReadContract(BinaryReader reader)
        => P12AWire.ReadString(reader, out string value)
            && string.Equals(value, PayloadContract, StringComparison.Ordinal);

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is System.Text.DecoderFallbackException;
}
