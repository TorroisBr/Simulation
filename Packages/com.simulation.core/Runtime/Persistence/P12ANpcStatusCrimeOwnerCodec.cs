using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Detached values owned by one NPC status/crime section.</summary>
internal sealed class P12ANpcStatusCrimeOwnerValue
{
    private readonly ReadOnlyCollection<string> statusDefinitionIds;

    internal P12ANpcStatusCrimeOwnerValue(string runtimeId,
        IEnumerable<string> currentStatusDefinitionIds, int hiddenDaysRemaining, long revision)
    {
        RuntimeId = runtimeId;
        if (currentStatusDefinitionIds == null)
            throw new ArgumentNullException(nameof(currentStatusDefinitionIds));
        List<string> copy = new List<string>();
        foreach (string definitionId in currentStatusDefinitionIds)
        {
            if (copy.Count == P12AContinuationCodec.MaxRows)
                throw new ArgumentException("Status owner row count exceeds the format bound.", nameof(currentStatusDefinitionIds));
            copy.Add(definitionId);
        }
        statusDefinitionIds = new ReadOnlyCollection<string>(copy);
        HiddenDaysRemaining = hiddenDaysRemaining;
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal IReadOnlyList<string> CurrentStatusDefinitionIds => statusDefinitionIds;
    internal int HiddenDaysRemaining { get; }
    internal long Revision { get; }
}

/// <summary>Explicit Daily-v1 codec for the per-NPC status/crime-state section.</summary>
internal static class P12ANpcStatusCrimeOwnerCodec
{
    private const string PayloadContract = "p12a.npc-status-crime-state/v1";
    private const string SectionPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimePrefix;

    internal static bool TryEncode(P12DNpcDRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, SectionPrefix);
            return false;
        }
        if (source.CurrentStatusDefinitionIds.Count > P12AContinuationCodec.MaxRows)
        {
            section = null;
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimePrefix, source.RuntimeId);
            return false;
        }

        return TryEncode(new P12ANpcStatusCrimeOwnerValue(source.RuntimeId,
            source.CurrentStatusDefinitionIds, source.HiddenDaysRemaining,
            source.CrimeJusticeRevision), out section, out result);
    }

    internal static bool TryEncode(P12ANpcStatusCrimeOwnerValue value,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, SectionPrefix);
        if (!TryValidate(value, out string sectionId)) return false;

        try
        {
            string aliasGroup = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId);
            if (aliasGroup == null) return false;
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(aliasGroup);
            P12AWire.GetEncodedStringByteCount(value.RuntimeId);
            long payloadLength = EncodedStringFieldLength(PayloadContract);
            payloadLength = checked(payloadLength + EncodedStringFieldLength(value.RuntimeId)
                + sizeof(int) + sizeof(int) + sizeof(long));
            foreach (string definitionId in value.CurrentStatusDefinitionIds)
                payloadLength = checked(payloadLength + EncodedStringFieldLength(definitionId));
            if (payloadLength > P12AContinuationCodec.MaxSectionBytes)
            {
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                return false;
            }

            using (MemoryStream stream = new MemoryStream((int)payloadLength))
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, value.RuntimeId);
                writer.Write(value.HiddenDaysRemaining);
                writer.Write(value.CurrentStatusDefinitionIds.Count);
                foreach (string definitionId in value.CurrentStatusDefinitionIds)
                {
                    P12AWire.GetEncodedStringByteCount(definitionId);
                    P12AWire.WriteString(writer, definitionId);
                }
                writer.Write(value.Revision);
                writer.Flush();

                if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                    return false;
                }

                section = new P12AOwnerSection(sectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0, OwnerSectionRole.Required,
                    aliasGroup, value.Revision,
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
        out P12ANpcStatusCrimeOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? SectionPrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(SectionPrefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows.Count != 1)
            return false;

        string sectionRuntimeId = section.Id.Substring(SectionPrefix.Length);
        if (!IsRuntimeId(sectionRuntimeId)
            || section.Id != DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(19, sectionRuntimeId))
            return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(section.AliasGroup);
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, section.Id);
            return false;
        }
        if (!string.Equals(section.AliasGroup,
            DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id), StringComparison.Ordinal))
            return false;

        P12AOwnerRow row = section.Rows[0];
        if (row == null || row.OwnerKey != sectionRuntimeId || row.Revision != section.Revision
            || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
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
                if (!P12AWire.ReadString(reader, out string contract)
                    || contract != PayloadContract
                    || !ReadRequiredString(reader, out string runtimeId)
                    || runtimeId != sectionRuntimeId)
                    return false;

                int hiddenDaysRemaining = reader.ReadInt32();
                int statusCount = reader.ReadInt32();
                long remaining = stream.Length - stream.Position;
                if (hiddenDaysRemaining < 0 || statusCount < 0
                    || statusCount > P12AContinuationCodec.MaxRows
                    || remaining < sizeof(long)
                    || statusCount > (remaining - sizeof(long)) / sizeof(int))
                    return false;

                List<string> statusIds = new List<string>(statusCount);
                for (int i = 0; i < statusCount; i++)
                {
                    if (!ReadRequiredString(reader, out string statusId)) return false;
                    statusIds.Add(statusId);
                }

                long revision = reader.ReadInt64();
                if (revision < 0L || revision != section.Revision
                    || stream.Position != stream.Length)
                    return false;

                value = new P12ANpcStatusCrimeOwnerValue(runtimeId,
                    statusIds, hiddenDaysRemaining, revision);
                result = null;
                return true;
            }
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            value = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                section.Id, row.OwnerKey);
            return false;
        }
    }

    private static bool TryValidate(P12ANpcStatusCrimeOwnerValue value, out string sectionId)
    {
        sectionId = null;
        if (value == null || !IsRuntimeId(value.RuntimeId)
            || value.CurrentStatusDefinitionIds == null
            || value.CurrentStatusDefinitionIds.Count > P12AContinuationCodec.MaxRows
            || value.HiddenDaysRemaining < 0 || value.Revision < 0L)
            return false;

        foreach (string definitionId in value.CurrentStatusDefinitionIds)
            if (string.IsNullOrWhiteSpace(definitionId)) return false;

        try
        {
            sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(19, value.RuntimeId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static long EncodedStringFieldLength(string value)
        => checked(sizeof(int) + (long)P12AWire.GetEncodedStringByteCount(value));

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;
}
