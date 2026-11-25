using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 wire codec for the typed PoliticalSupport relation owner.</summary>
internal static class P12EPoliticalSupportOwnerCodec
{
    private const string PayloadContract = "p12a.political-support/v1";
    private const string OwnerPrefix = "relation/";

    internal static bool TryEncode(P12EPoliticalSupportOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalSupportStoreCensusProvider.RelationsSectionId);
        if (snapshot == null || snapshot.Relations == null) return false;
        if (snapshot.Relations.RecordCount > P12AContinuationCodec.MaxRows)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                PoliticalSupportStoreCensusProvider.RelationsSectionId);
            return false;
        }
        if (!snapshot.TryValidate(out _)) return false;

        try
        {
            P12EPoliticalSupportSnapshotSection relations = snapshot.Relations;
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(relations.RecordCount);
            foreach (P12EPoliticalSupportSnapshotRow relation in relations.Records)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    P12AWire.WriteString(writer, relation.RelationId);
                    writer.Write((ushort)relation.SourceKind);
                    P12AWire.WriteString(writer, relation.SourceId);
                    writer.Write((ushort)relation.TargetKind);
                    P12AWire.WriteString(writer, relation.TargetId);
                    writer.Write((ushort)relation.Disposition);
                    writer.Write(relation.StartedAbsoluteDay);
                    WriteNullableInt64(writer, relation.EndedAbsoluteDay);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            PoliticalSupportStoreCensusProvider.RelationsSectionId,
                            OwnerPrefix + relation.RelationId);
                        return false;
                    }
                    rows.Add(new P12AOwnerRow(OwnerPrefix + relation.RelationId, 0L, stream.ToArray()));
                }
            }

            section = new P12AOwnerSection(PoliticalSupportStoreCensusProvider.RelationsSectionId,
                PoliticalSupportStoreCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                null, relations.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalSupportStoreCensusProvider.RelationsSectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section, long capturedAbsoluteDay,
        out P12EPoliticalSupportOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            PoliticalSupportStoreCensusProvider.RelationsSectionId);
        if (section == null || capturedAbsoluteDay < 0L
            || section.Id != PoliticalSupportStoreCensusProvider.RelationsSectionId
            || section.Major != PoliticalSupportStoreCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0L || section.Rows.Count > P12AContinuationCodec.MaxRows)
            return false;

        try
        {
            List<P12EPoliticalSupportSnapshotRow> relations = new List<P12EPoliticalSupportSnapshotRow>(section.Rows.Count);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != 0L || string.IsNullOrEmpty(row.OwnerKey)
                    || !row.OwnerKey.StartsWith(OwnerPrefix, StringComparison.Ordinal)
                    || row.OwnerKey.Length == OwnerPrefix.Length || row.PayloadLength < 0
                    || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
                using (MemoryStream stream = new MemoryStream(row.Payload, false))
                using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
                {
                    if (!ReadRequiredString(reader, out string contract) || contract != PayloadContract
                        || !ReadRequiredString(reader, out string relationId)) return false;
                    PoliticalSupportSourceKind sourceKind = (PoliticalSupportSourceKind)reader.ReadUInt16();
                    if (!Enum.IsDefined(typeof(PoliticalSupportSourceKind), sourceKind)
                        || !ReadRequiredString(reader, out string sourceId)) return false;
                    PoliticalSupportTargetKind targetKind = (PoliticalSupportTargetKind)reader.ReadUInt16();
                    if (!Enum.IsDefined(typeof(PoliticalSupportTargetKind), targetKind)
                        || !ReadRequiredString(reader, out string targetId)) return false;
                    PoliticalSupportDisposition disposition = (PoliticalSupportDisposition)reader.ReadUInt16();
                    if (!Enum.IsDefined(typeof(PoliticalSupportDisposition), disposition)) return false;
                    long startedDay = reader.ReadInt64();
                    if (!TryReadNullableInt64(reader, out long? endedDay)
                        || stream.Position != stream.Length
                        || !string.Equals(row.OwnerKey, OwnerPrefix + relationId, StringComparison.Ordinal))
                        return false;
                    relations.Add(new P12EPoliticalSupportSnapshotRow(relationId,
                        (int)sourceKind, sourceId, (int)targetKind, targetId,
                        (int)disposition, startedDay, endedDay));
                }
            }

            P12EPoliticalSupportSnapshotSection values = new P12EPoliticalSupportSnapshotSection(
                PoliticalSupportStoreCensusProvider.RelationsSectionId,
                PoliticalSupportStoreCensusProvider.SchemaVersion,
                relations.Count,
                section.Revision,
                relations);
            P12EPoliticalSupportOwnerSnapshot candidate = new P12EPoliticalSupportOwnerSnapshot(
                capturedAbsoluteDay, values);
            if (!candidate.TryValidate(out _)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                PoliticalSupportStoreCensusProvider.RelationsSectionId);
            return false;
        }
    }

    private static void WriteNullableInt64(BinaryWriter writer, long? value)
    {
        writer.Write(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue) writer.Write(value.Value);
    }

    private static bool TryReadNullableInt64(BinaryReader reader, out long? value)
    {
        value = null;
        byte present = reader.ReadByte();
        if (present == 0) return true;
        if (present != 1) return false;
        value = reader.ReadInt64();
        return true;
    }

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && value != null;

    private static bool IsWireException(Exception exception)
        => exception is P12ALimitException || exception is EndOfStreamException
            || exception is IOException || exception is ArgumentException
            || exception is DecoderFallbackException || exception is OverflowException;
}
