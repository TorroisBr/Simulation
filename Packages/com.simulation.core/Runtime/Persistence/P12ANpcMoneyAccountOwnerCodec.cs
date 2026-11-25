using System;
using System.IO;
using System.Text;

/// <summary>Detached values owned by one NPC MoneyAccount section.</summary>
internal sealed class P12ANpcMoneyAccountOwnerValue
{
    internal P12ANpcMoneyAccountOwnerValue(string runtimeId, float balance, long revision)
    {
        RuntimeId = runtimeId;
        Balance = balance;
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal float Balance { get; }
    internal long Revision { get; }
}

/// <summary>Explicit Daily-v1 codec for the per-NPC MoneyAccount owner section.</summary>
internal static class P12ANpcMoneyAccountOwnerCodec
{
    private const string PayloadContract = "p12a.npc-money-account/v1";

    internal static bool TryEncode(P12DNpcDRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
            return false;
        }
        return TryEncode(new P12ANpcMoneyAccountOwnerValue(source.RuntimeId,
            source.MoneyBalance, source.MoneyRevision), out section, out result);
    }

    internal static bool TryEncode(P12ANpcMoneyAccountOwnerValue value,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix, value?.RuntimeId);
        if (!TryValidate(value, out string sectionId)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(value.RuntimeId);
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, value.RuntimeId);
                writer.Write(value.Balance);
                writer.Write(value.Revision);
                writer.Flush();
                if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                    return false;
                }
                section = new P12AOwnerSection(sectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
                    OwnerSectionRole.Required, null, value.Revision,
                    new[] { new P12AOwnerRow(value.RuntimeId, value.Revision, stream.ToArray()) });
            }
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                sectionId, value.RuntimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12ANpcMoneyAccountOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0L || section.Rows.Count != 1) return false;

        string runtimeId = section.Id.Substring(DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix.Length);
        if (!IsRuntimeId(runtimeId)
            || section.Id != DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + runtimeId) return false;
        P12AOwnerRow row = section.Rows[0];
        if (row == null || row.OwnerKey != runtimeId || row.Revision != section.Revision
            || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;

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
                    || !ReadRequiredString(reader, out string payloadRuntimeId)
                    || payloadRuntimeId != runtimeId) return false;
                float balance = reader.ReadSingle();
                long revision = reader.ReadInt64();
                P12ANpcMoneyAccountOwnerValue candidate =
                    new P12ANpcMoneyAccountOwnerValue(runtimeId, balance, revision);
                if (stream.Position != stream.Length || revision != row.Revision
                    || revision != section.Revision || !TryValidate(candidate, out _)) return false;
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

    private static bool TryValidate(P12ANpcMoneyAccountOwnerValue value, out string sectionId)
    {
        sectionId = null;
        if (value == null || !IsRuntimeId(value.RuntimeId) || value.Revision < 0L
            || float.IsNaN(value.Balance) || float.IsInfinity(value.Balance) || value.Balance < 0f)
            return false;
        sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + value.RuntimeId;
        return true;
    }

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;
}
