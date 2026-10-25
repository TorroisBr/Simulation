using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Detached values owned by one NPC inventory census section.</summary>
internal sealed class P12ANpcInventoryOwnerValue
{
    private readonly IReadOnlyList<P12DNpcInventoryRowValue> rows;

    internal P12ANpcInventoryOwnerValue(string runtimeId,
        IEnumerable<P12DNpcInventoryRowValue> inventoryRows, long revision)
    {
        RuntimeId = runtimeId;
        if (inventoryRows == null) throw new ArgumentNullException(nameof(inventoryRows));
        List<P12DNpcInventoryRowValue> copy = new List<P12DNpcInventoryRowValue>();
        foreach (P12DNpcInventoryRowValue row in inventoryRows)
        {
            if (copy.Count == P12AContinuationCodec.MaxRows)
                throw new ArgumentException("Inventory owner row count exceeds the format bound.", nameof(inventoryRows));
            if (row == null)
            {
                copy.Add(null);
                continue;
            }
            copy.Add(new P12DNpcInventoryRowValue(row.ItemDefinitionId, row.Amount, row.AverageUnitCost));
        }
        rows = copy.AsReadOnly();
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal IReadOnlyList<P12DNpcInventoryRowValue> Rows => rows;
    internal long Revision { get; }
}

/// <summary>Explicit Daily-v1 codec for the per-NPC InventoryRuntime owner section.</summary>
internal static class P12ANpcInventoryOwnerCodec
{
    private const string PayloadContract = "p12a.npc-inventory/v1";
    private const string ItemOwnerPrefix = "item/";

    internal static bool TryEncode(P12DNpcDRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
            return false;
        }
        if (source.InventoryRows.Count > P12AContinuationCodec.MaxRows)
        {
            section = null;
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix, source.RuntimeId);
            return false;
        }
        return TryEncode(new P12ANpcInventoryOwnerValue(source.RuntimeId,
            source.InventoryRows, source.InventoryRevision), out section, out result);
    }

    internal static bool TryEncode(P12ANpcInventoryOwnerValue value,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix, value?.RuntimeId);
        if (!TryValidate(value, out string sectionId, out string aliasGroup)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            if (aliasGroup != null) P12AWire.GetEncodedStringByteCount(aliasGroup);
            long encodedRowsLength = sizeof(int);
            long[] rowPayloadLengths = new long[value.Rows.Count];
            string[] rowOwnerKeys = new string[value.Rows.Count];
            for (int i = 0; i < value.Rows.Count; i++)
            {
                P12DNpcInventoryRowValue item = value.Rows[i];
                string ownerKey = ItemOwnerPrefix + item.ItemDefinitionId;
                rowOwnerKeys[i] = ownerKey;
                long payloadLength = EncodedStringFieldLength(PayloadContract)
                    + EncodedStringFieldLength(value.RuntimeId)
                    + EncodedStringFieldLength(item.ItemDefinitionId)
                    + sizeof(int) + sizeof(float);
                long ownerKeyLength = EncodedStringFieldLength(ownerKey);
                P12AWire.GetEncodedStringByteCount(ownerKey);
                rowPayloadLengths[i] = payloadLength;
                if (payloadLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                    return false;
                }
                encodedRowsLength = checked(encodedRowsLength + ownerKeyLength + sizeof(long)
                    + sizeof(int) + payloadLength);
                if (encodedRowsLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                    return false;
                }
            }

            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(value.Rows.Count);
            for (int i = 0; i < value.Rows.Count; i++)
            {
                P12DNpcInventoryRowValue item = value.Rows[i];
                using (MemoryStream stream = new MemoryStream((int)rowPayloadLengths[i]))
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    P12AWire.WriteString(writer, value.RuntimeId);
                    P12AWire.WriteString(writer, item.ItemDefinitionId);
                    writer.Write(item.Amount);
                    writer.Write(item.AverageUnitCost);
                    writer.Flush();
                    if (stream.Length != rowPayloadLengths[i])
                    {
                        result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, value.RuntimeId);
                        return false;
                    }
                    rows.Add(new P12AOwnerRow(rowOwnerKeys[i], value.Revision, stream.ToArray()));
                }
            }

            section = new P12AOwnerSection(sectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
                OwnerSectionRole.Required, aliasGroup, value.Revision, rows);
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
        out P12ANpcInventoryOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows.Count > P12AContinuationCodec.MaxRows)
        {
            if (section?.Rows.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, section.Id);
            return false;
        }

        string runtimeId = section.Id.Substring(DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix.Length);
        if (!IsRuntimeId(runtimeId)
            || section.Id != DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + runtimeId
            || !string.Equals(section.AliasGroup,
                DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id), StringComparison.Ordinal))
            return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            if (section.AliasGroup != null) P12AWire.GetEncodedStringByteCount(section.AliasGroup);
            HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
            List<P12DNpcInventoryRowValue> rows = new List<P12DNpcInventoryRowValue>(section.Rows.Count);
            long encodedRowsLength = sizeof(int);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != section.Revision
                    || !TryItemId(row.OwnerKey, out string ownerItemId)
                    || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
                    return false;
                P12AWire.GetEncodedStringByteCount(row.OwnerKey);
                byte[] payload = row.Payload;
                if (payload == null || payload.Length != row.PayloadLength) return false;
                encodedRowsLength = checked(encodedRowsLength + EncodedStringFieldLength(row.OwnerKey)
                    + sizeof(long) + sizeof(int) + payload.Length);
                if (encodedRowsLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, section.Id, runtimeId);
                    return false;
                }

                if (!ReadItem(payload, runtimeId, ownerItemId,
                    out P12DNpcInventoryRowValue decodedItem)
                    || !itemIds.Add(decodedItem.ItemDefinitionId)) return false;
                rows.Add(decodedItem);
            }

            value = new P12ANpcInventoryOwnerValue(runtimeId, rows, section.Revision);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            value = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                section.Id, runtimeId);
            return false;
        }
    }

    private static bool ReadItem(byte[] payload, string runtimeId, string ownerItemId,
        out P12DNpcInventoryRowValue item)
    {
        item = null;
        if (payload == null || payload.Length > P12AContinuationCodec.MaxSectionBytes) return false;
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract)
                || contract != PayloadContract
                || !ReadRequiredString(reader, out string payloadRuntimeId)
                || payloadRuntimeId != runtimeId
                || !ReadRequiredString(reader, out string itemId)
                || itemId != ownerItemId) return false;
            int amount = reader.ReadInt32();
            float averageUnitCost = reader.ReadSingle();
            if (amount < 0 || float.IsNaN(averageUnitCost) || float.IsInfinity(averageUnitCost)
                || averageUnitCost < 0f || stream.Position != stream.Length) return false;
            item = new P12DNpcInventoryRowValue(itemId, amount, averageUnitCost);
            return true;
        }
    }

    private static bool TryValidate(P12ANpcInventoryOwnerValue value,
        out string sectionId, out string aliasGroup)
    {
        sectionId = null;
        aliasGroup = null;
        if (value == null || !IsRuntimeId(value.RuntimeId) || value.Rows == null
            || value.Rows.Count > P12AContinuationCodec.MaxRows || value.Revision < 0L)
            return false;

        HashSet<string> itemIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12DNpcInventoryRowValue item in value.Rows)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemDefinitionId)
                || item.Amount < 0 || float.IsNaN(item.AverageUnitCost)
                || float.IsInfinity(item.AverageUnitCost) || item.AverageUnitCost < 0f
                || !itemIds.Add(item.ItemDefinitionId)) return false;
        }

        sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + value.RuntimeId;
        aliasGroup = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId);
        return true;
    }

    private static bool TryItemId(string ownerKey, out string itemId)
    {
        itemId = null;
        if (string.IsNullOrWhiteSpace(ownerKey)
            || !ownerKey.StartsWith(ItemOwnerPrefix, StringComparison.Ordinal)) return false;
        itemId = ownerKey.Substring(ItemOwnerPrefix.Length);
        return !string.IsNullOrWhiteSpace(itemId);
    }

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static long EncodedStringFieldLength(string value)
        => checked(sizeof(int) + (long)P12AWire.GetEncodedStringByteCount(value));

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;
}
