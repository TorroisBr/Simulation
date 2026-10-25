using System;
using System.IO;
using System.Text;

/// <summary>
/// Detached exact values for one Required Person life/residence witness.
/// These values also appear inside the fixed PersonStore owner snapshot; the
/// eventual image composer must compare both representations field-for-field
/// and reject disagreement rather than selecting one representation.
/// </summary>
internal sealed class P12APersonLifeResidenceOwnerValue
{
    internal P12APersonLifeResidenceOwnerValue(string personIdValue, long? birthAbsoluteDay,
        long? deathAbsoluteDay, string residenceSettlementRuntimeId, long revision)
    {
        PersonIdValue = personIdValue;
        BirthAbsoluteDay = birthAbsoluteDay;
        DeathAbsoluteDay = deathAbsoluteDay;
        ResidenceSettlementRuntimeId = residenceSettlementRuntimeId;
        Revision = revision;
    }

    internal string PersonIdValue { get; }
    internal long? BirthAbsoluteDay { get; }
    internal long? DeathAbsoluteDay { get; }
    internal string ResidenceSettlementRuntimeId { get; }
    internal long Revision { get; }
}

/// <summary>
/// Explicit Daily-v1 codec for the dynamic singleton section of one Person's
/// life/residence state. It captures no live PersonRuntime reference.
/// </summary>
internal static class P12APersonLifeResidenceOwnerCodec
{
    private const string PayloadContract = "p12a.person-life-residence/v1";

    internal static bool TryEncode(PersonStoreOwnerSnapshotRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix);
            return false;
        }
        return TryEncode(new P12APersonLifeResidenceOwnerValue(
            source.PersonIdValue, source.BirthAbsoluteDay, source.DeathAbsoluteDay,
            source.ResidenceSettlementRuntimeId, source.LifeResidenceRevision), out section, out result);
    }

    internal static bool TryEncode(P12APersonLifeResidenceOwnerValue value,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix);
        if (!TryValidate(value, out string sectionId)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(value.PersonIdValue);
            P12AWire.GetEncodedStringByteCount(
                DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId));
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, value.PersonIdValue);
                WriteNullableInt64(writer, value.BirthAbsoluteDay);
                WriteNullableInt64(writer, value.DeathAbsoluteDay);
                P12AWire.WriteString(writer, value.ResidenceSettlementRuntimeId);
                writer.Write(value.Revision);
                writer.Flush();
                if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.PersonIdValue);
                    return false;
                }

                section = new P12AOwnerSection(sectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
                    OwnerSectionRole.Required,
                    DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId), value.Revision,
                    new[] { new P12AOwnerRow(value.PersonIdValue, value.Revision, stream.ToArray()) });
            }
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix, value.PersonIdValue);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12APersonLifeResidenceOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? DailyV1SelectedOwnerSectionCensusContract.PersonLifeResidencePrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id) || section.Rows.Count != 1
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required
            || !string.Equals(section.AliasGroup,
                DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id), StringComparison.Ordinal)
            || section.Revision < 0L)
            return false;

        P12AOwnerRow row = section.Rows[0];
        if (row == null || row.Revision != section.Revision
            || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
            || string.IsNullOrWhiteSpace(row.OwnerKey))
            return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(row.OwnerKey);
            byte[] payload = row.Payload;
            if (payload == null || payload.Length != row.PayloadLength) return false;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                    || !ReadRequiredString(reader, out string personId)
                    || !ReadNullableInt64(reader, out long? birth)
                    || !ReadNullableInt64(reader, out long? death)
                    || !P12AWire.ReadString(reader, out string residence)
                    || !ReadNonNegativeInt64(reader, out long revision)
                    || stream.Position != stream.Length)
                    return false;

                P12APersonLifeResidenceOwnerValue candidate = new P12APersonLifeResidenceOwnerValue(
                    personId, birth, death, residence, revision);
                if (!string.Equals(row.OwnerKey, personId, StringComparison.Ordinal)
                    || !TryValidate(candidate, out string expectedSectionId)
                    || !string.Equals(section.Id, expectedSectionId, StringComparison.Ordinal)
                    || section.Revision != revision)
                    return false;

                value = candidate;
                result = null;
                return true;
            }
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                section.Id, row.OwnerKey);
            return false;
        }
    }

    private static bool TryValidate(P12APersonLifeResidenceOwnerValue value, out string sectionId)
    {
        sectionId = null;
        if (value == null || string.IsNullOrWhiteSpace(value.PersonIdValue) || value.Revision < 0L
            || (value.BirthAbsoluteDay.HasValue && value.BirthAbsoluteDay.Value < 0L)
            || (value.DeathAbsoluteDay.HasValue && value.DeathAbsoluteDay.Value < 0L)
            || (value.BirthAbsoluteDay.HasValue && value.DeathAbsoluteDay.HasValue
                && value.DeathAbsoluteDay.Value < value.BirthAbsoluteDay.Value)
            || (value.ResidenceSettlementRuntimeId != null
                && string.IsNullOrWhiteSpace(value.ResidenceSettlementRuntimeId))
            || !PersonId.TryCreate(value.PersonIdValue, out PersonId personId))
            return false;

        try
        {
            sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(personId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void WriteNullableInt64(BinaryWriter writer, long? value)
    {
        writer.Write(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool ReadNullableInt64(BinaryReader reader, out long? value)
    {
        value = null;
        byte present = reader.ReadByte();
        if (present == 0) return true;
        if (present != 1) return false;
        value = reader.ReadInt64();
        return true;
    }

    private static bool ReadNonNegativeInt64(BinaryReader reader, out long value)
    {
        value = reader.ReadInt64();
        return value >= 0L;
    }

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;
}
