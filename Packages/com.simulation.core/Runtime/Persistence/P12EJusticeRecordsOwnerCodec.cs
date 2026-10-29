using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 wire codec for ordered Justice wanted and sentence records.</summary>
internal static class P12EJusticeRecordsOwnerCodec
{
    private const string PayloadContract = "p12a.justice-records/v1";
    private const string WantedOwnerPrefix = "wanted/";
    private const string SentenceOwnerPrefix = "sentence/";
    private const byte WantedRowKind = 1;
    private const byte SentenceRowKind = 2;

    internal static bool TryEncode(P12EJusticeRecordsOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
        if (snapshot?.Records == null) return false;

        int count;
        try { count = checked((snapshot.Records.WantedRows?.Count ?? 0)
            + (snapshot.Records.SentenceRows?.Count ?? 0)); }
        catch (OverflowException)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
            return false;
        }
        if (count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
            return false;
        }
        if (!snapshot.TryValidate(out _)) return false;

        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(count);
            long encodedLength = sizeof(int);
            foreach (P12EJusticeWantedSnapshotRow wanted in snapshot.Records.WantedRows)
            {
                P12AOwnerRow row = EncodeWanted(wanted);
                if (!AccumulateEncodedLength(row, ref encodedLength))
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded,
                        DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, row.OwnerKey);
                    return false;
                }
                rows.Add(row);
            }
            foreach (P12EJusticeSentenceSnapshotRow sentence in snapshot.Records.SentenceRows)
            {
                P12AOwnerRow row = EncodeSentence(sentence);
                if (!AccumulateEncodedLength(row, ref encodedLength))
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded,
                        DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId, row.OwnerKey);
                    return false;
                }
                rows.Add(row);
            }

            string sectionId = DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId;
            section = new P12AOwnerSection(sectionId,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId), snapshot.Records.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section, long capturedAbsoluteDay,
        out P12EJusticeRecordsOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
        if (section?.Rows.Count > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
            return false;
        }
        string sectionId = DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId;
        if (capturedAbsoluteDay < 0L || section == null || section.Revision < 0L
            || !string.Equals(section.Id, sectionId, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required
            || !string.Equals(section.AliasGroup,
                P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId), StringComparison.Ordinal))
        {
            result = new P12AResult(P12AResultCode.UnsupportedSchema, sectionId);
            return false;
        }

        try
        {
            List<P12EJusticeWantedSnapshotRow> wantedRows = new List<P12EJusticeWantedSnapshotRow>();
            List<P12EJusticeSentenceSnapshotRow> sentenceRows = new List<P12EJusticeSentenceSnapshotRow>();
            long encodedLength = sizeof(int);
            bool encounteredSentence = false;
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != 0L || row.PayloadLength < 0
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
                    return false;
                byte[] payload = row.Payload;
                if (payload == null || payload.Length != row.PayloadLength
                    || !AccumulateEncodedLength(row, ref encodedLength))
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, row.OwnerKey);
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(row.OwnerKey)
                    && row.OwnerKey.StartsWith(WantedOwnerPrefix, StringComparison.Ordinal))
                {
                    if (encounteredSentence
                        || !TryDecodeWanted(row, wantedRows.Count, out P12EJusticeWantedSnapshotRow wanted))
                        return false;
                    wantedRows.Add(wanted);
                }
                else if (!string.IsNullOrWhiteSpace(row.OwnerKey)
                    && row.OwnerKey.StartsWith(SentenceOwnerPrefix, StringComparison.Ordinal))
                {
                    encounteredSentence = true;
                    if (!TryDecodeSentence(row, sentenceRows.Count, out P12EJusticeSentenceSnapshotRow sentence))
                        return false;
                    sentenceRows.Add(sentence);
                }
                else return false;
            }

            int recordCount = checked(wantedRows.Count + sentenceRows.Count);
            P12EJusticeRecordsOwnerSnapshot candidate = new P12EJusticeRecordsOwnerSnapshot(
                capturedAbsoluteDay,
                new P12EJusticeRecordsSnapshotSection(
                    P12EJusticeRecordsSnapshotSection.CurrentSectionId,
                    P12EJusticeRecordsOwnerSnapshot.CurrentSchemaVersion,
                    recordCount, section.Revision, wantedRows, sentenceRows));
            if (!candidate.TryValidate(out _)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            snapshot = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, sectionId);
            return false;
        }
    }

    private static P12AOwnerRow EncodeWanted(P12EJusticeWantedSnapshotRow row)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            writer.Write(WantedRowKind);
            writer.Write(row.Ordinal);
            P12AWire.WriteString(writer, row.TargetRuntimeId);
            P12AWire.WriteString(writer, row.TargetPersonIdValue);
            P12AWire.WriteString(writer, row.CityRuntimeId);
            writer.Write(row.Bounty);
            writer.Write(row.SentenceDays);
            writer.Write(row.IsResolved ? (byte)1 : (byte)0);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(WantedOwnerKey(row.Ordinal), 0L, stream.ToArray());
        }
    }

    private static P12AOwnerRow EncodeSentence(P12EJusticeSentenceSnapshotRow row)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            writer.Write(SentenceRowKind);
            writer.Write(row.Ordinal);
            P12AWire.WriteString(writer, row.TargetRuntimeId);
            P12AWire.WriteString(writer, row.TargetPersonIdValue);
            P12AWire.WriteString(writer, row.CityRuntimeId);
            writer.Write(row.WarrantOrdinal);
            writer.Write(row.RemainingDays);
            writer.Write(row.FailedEscapeAttempts);
            writer.Write(row.WasArrestedToday ? (byte)1 : (byte)0);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return new P12AOwnerRow(SentenceOwnerKey(row.Ordinal), 0L, stream.ToArray());
        }
    }

    private static bool TryDecodeWanted(P12AOwnerRow row, int expectedOrdinal,
        out P12EJusticeWantedSnapshotRow decoded)
    {
        decoded = null;
        if (!TryOpenPayload(row, out string ownerKey, out BinaryReader reader, out MemoryStream stream)) return false;
        using (reader)
        {
            if (!ReadContract(reader) || reader.ReadByte() != WantedRowKind) return false;
            int ordinal = reader.ReadInt32();
            if (ordinal != expectedOrdinal || !string.Equals(ownerKey, WantedOwnerKey(ordinal), StringComparison.Ordinal)
                || !ReadRequiredString(reader, out string runtimeId)
                || !P12AWire.ReadString(reader, out string personId)
                || !ReadRequiredString(reader, out string cityId)) return false;
            float bounty = reader.ReadSingle();
            int sentenceDays = reader.ReadInt32();
            if (!TryReadBoolean(reader, out bool resolved) || stream.Position != stream.Length) return false;
            decoded = new P12EJusticeWantedSnapshotRow(ordinal, runtimeId, personId, cityId,
                bounty, sentenceDays, resolved);
            return true;
        }
    }

    private static bool TryDecodeSentence(P12AOwnerRow row, int expectedOrdinal,
        out P12EJusticeSentenceSnapshotRow decoded)
    {
        decoded = null;
        if (!TryOpenPayload(row, out string ownerKey, out BinaryReader reader, out MemoryStream stream)) return false;
        using (reader)
        {
            if (!ReadContract(reader) || reader.ReadByte() != SentenceRowKind) return false;
            int ordinal = reader.ReadInt32();
            if (ordinal != expectedOrdinal || !string.Equals(ownerKey, SentenceOwnerKey(ordinal), StringComparison.Ordinal)
                || !ReadRequiredString(reader, out string runtimeId)
                || !P12AWire.ReadString(reader, out string personId)
                || !ReadRequiredString(reader, out string cityId)) return false;
            int warrantOrdinal = reader.ReadInt32();
            int remainingDays = reader.ReadInt32();
            int failedEscapeAttempts = reader.ReadInt32();
            if (!TryReadBoolean(reader, out bool arrestedToday) || stream.Position != stream.Length) return false;
            decoded = new P12EJusticeSentenceSnapshotRow(ordinal, runtimeId, personId, cityId,
                warrantOrdinal, remainingDays, failedEscapeAttempts, arrestedToday);
            return true;
        }
    }

    private static bool TryOpenPayload(P12AOwnerRow row, out string ownerKey,
        out BinaryReader reader, out MemoryStream stream)
    {
        ownerKey = null;
        reader = null;
        stream = null;
        if (row == null || row.Revision != 0L || string.IsNullOrWhiteSpace(row.OwnerKey)
            || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
        P12AWire.GetEncodedStringByteCount(row.OwnerKey);
        byte[] payload = row.Payload;
        if (payload == null || payload.Length != row.PayloadLength) return false;
        stream = new MemoryStream(payload, false);
        reader = new BinaryReader(stream, P12AWire.Utf8, true);
        ownerKey = row.OwnerKey;
        return true;
    }

    private static bool AccumulateEncodedLength(P12AOwnerRow row, ref long encodedLength)
    {
        int keyLength = P12AWire.GetEncodedStringByteCount(row.OwnerKey);
        encodedLength = checked(encodedLength + sizeof(int) + keyLength + sizeof(long)
            + sizeof(int) + row.PayloadLength);
        return encodedLength <= P12AContinuationCodec.MaxSectionBytes;
    }

    private static string WantedOwnerKey(int ordinal)
        => WantedOwnerPrefix + ordinal.ToString("D10", System.Globalization.CultureInfo.InvariantCulture);

    private static string SentenceOwnerKey(int ordinal)
        => SentenceOwnerPrefix + ordinal.ToString("D10", System.Globalization.CultureInfo.InvariantCulture);

    private static bool ReadContract(BinaryReader reader)
        => ReadRequiredString(reader, out string contract)
            && string.Equals(contract, PayloadContract, StringComparison.Ordinal);

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && value != null;

    private static bool TryReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte encoded = reader.ReadByte();
        if (encoded > 1) return false;
        value = encoded == 1;
        return true;
    }

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException;
}
