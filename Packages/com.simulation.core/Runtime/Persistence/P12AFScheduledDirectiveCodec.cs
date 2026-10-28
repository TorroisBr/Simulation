using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit value codec and target-only adapter for Daily-v1 directives.</summary>
internal static class P12AFScheduledDirectiveCodec
{
    private const string PayloadContract = "p12a.scheduled-directive/v1";
    private const string OwnerPrefix = "directive/";

    internal static bool TryEncode(P12FScheduledDirectiveOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, ScheduledDirectiveCensusProvider.SectionId);
        if (snapshot == null || snapshot.Rows == null || snapshot.Revision < 0
            || snapshot.Rows.Count > P12AContinuationCodec.MaxRows
            || snapshot.Revision < snapshot.Rows.Count
            || (snapshot.Rows.Count == 0 && snapshot.Revision != 0L)) return false;
        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Rows.Count);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12FScheduledDirectiveOwnerSnapshot.Row row in snapshot.Rows)
            {
                if (!IsValid(row) || !ids.Add(row.Id)) return false;
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    WriteRow(writer, row);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            ScheduledDirectiveCensusProvider.SectionId, OwnerPrefix + row.Id);
                        return false;
                    }
                    rows.Add(new P12AOwnerRow(OwnerPrefix + row.Id, 0L, stream.ToArray()));
                }
            }
            section = new P12AOwnerSection(ScheduledDirectiveCensusProvider.SectionId,
                ScheduledDirectiveCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required, null,
                snapshot.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException || exception is OverflowException || exception is EncoderFallbackException)
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                ScheduledDirectiveCensusProvider.SectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12FScheduledDirectiveOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, ScheduledDirectiveCensusProvider.SectionId);
        if (section == null || section.Id != ScheduledDirectiveCensusProvider.SectionId
            || section.Major != ScheduledDirectiveCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0 || section.Rows.Count > P12AContinuationCodec.MaxRows
            || (section.Rows.Count == 0 && section.Revision != 0L))
            return false;
        try
        {
            List<P12FScheduledDirectiveOwnerSnapshot.Row> rows =
                new List<P12FScheduledDirectiveOwnerSnapshot.Row>(section.Rows.Count);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != 0L || row.Payload == null
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                    || !TryReadRow(row.Payload, out P12FScheduledDirectiveOwnerSnapshot.Row value)
                    || row.OwnerKey != OwnerPrefix + value.Id || !IsValid(value) || !ids.Add(value.Id))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        ScheduledDirectiveCensusProvider.SectionId, row?.OwnerKey);
                    return false;
                }
                rows.Add(value);
            }
            if (section.Revision < rows.Count) return false;
            snapshot = new P12FScheduledDirectiveOwnerSnapshot(rows, section.Revision);
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException)
        { snapshot = null; return false; }
    }

    internal static bool TryStageForImage(P12FScheduledDirectiveOwnerSnapshot snapshot,
        SimulationTime stagedTime, DailyV1EffectiveDefinitions selectedDefinitions,
        out ScheduledDirectiveStore staged, out string failure)
    {
        staged = null;
        failure = null;
        return snapshot != null && snapshot.TryStageForImage(stagedTime,
            selectedDefinitions, out staged, out failure);
    }

    private static bool IsValid(P12FScheduledDirectiveOwnerSnapshot.Row row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.Id) || row.AbsoluteDay < 0L
            || !Enum.IsDefined(typeof(ScheduledDirectiveMode), row.Mode)
            || !Enum.IsDefined(typeof(ScheduledDirectiveOperation), row.Operation)
            || string.IsNullOrWhiteSpace(row.ActorRuntimeId) || string.IsNullOrWhiteSpace(row.ActionDefinitionId)
            || !Enum.IsDefined(typeof(ScheduledDirectiveState), row.State)) return false;
        if (row.State == ScheduledDirectiveState.Pending)
            return row.ProcessedDay == -1L && row.ResultReason == null;
        return row.ProcessedDay >= 0L && row.ResultReason != null;
    }

    private static void WriteRow(BinaryWriter writer, P12FScheduledDirectiveOwnerSnapshot.Row row)
    {
        P12AWire.WriteString(writer, row.Id);
        writer.Write(row.AbsoluteDay);
        writer.Write((ushort)row.Mode);
        writer.Write((ushort)row.Operation);
        P12AWire.WriteString(writer, row.ActorRuntimeId);
        P12AWire.WriteString(writer, row.ActionDefinitionId);
        writer.Write((ushort)row.State);
        writer.Write(row.ProcessedDay);
        P12AWire.WriteString(writer, row.ResultReason);
    }

    private static bool TryReadRow(byte[] payload, out P12FScheduledDirectiveOwnerSnapshot.Row row)
    {
        row = null;
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                || !P12AWire.ReadString(reader, out string id) || id == null) return false;
            long absoluteDay = reader.ReadInt64();
            if (!ReadEnum(reader, out ScheduledDirectiveMode mode)
                || !ReadEnum(reader, out ScheduledDirectiveOperation operation)
                || !P12AWire.ReadString(reader, out string actor) || actor == null
                || !P12AWire.ReadString(reader, out string action) || action == null
                || !ReadEnum(reader, out ScheduledDirectiveState state)) return false;
            long processedDay = reader.ReadInt64();
            if (!P12AWire.ReadString(reader, out string reason) || stream.Position != stream.Length) return false;
            row = new P12FScheduledDirectiveOwnerSnapshot.Row(id, absoluteDay, mode,
                operation, actor, action, state, processedDay, reason);
            return true;
        }
    }

    private static bool ReadEnum<T>(BinaryReader reader, out T value) where T : struct, Enum
    {
        value = default;
        ushort raw = reader.ReadUInt16();
        T decoded = (T)Enum.ToObject(typeof(T), raw);
        if (!Enum.IsDefined(typeof(T), decoded)) return false;
        value = decoded;
        return true;
    }
}
