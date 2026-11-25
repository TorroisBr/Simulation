using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Explicit value codec for the selected Daily-v1 Genealogy owner.</summary>
internal static class P12AGenealogyOwnerCodec
{
    private const string PayloadContract = "p12a.genealogy-parentage/v1";

    internal static bool TryEncode(GenealogyOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            GenealogyCensusProvider.SectionId);
        if (!TryValidate(snapshot, out List<ParentageRecord> ordered, out result)) return false;

        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(ordered.Count);
            foreach (ParentageRecord record in ordered)
            {
                string ownerKey = OwnerKey(record.ParentId.Value, record.ChildId.Value);
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, PayloadContract);
                    P12AWire.WriteString(writer, record.ParentId.Value);
                    P12AWire.WriteString(writer, record.ChildId.Value);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            GenealogyCensusProvider.SectionId, ownerKey);
                        return false;
                    }
                    rows.Add(new P12AOwnerRow(ownerKey, snapshot.Revision, stream.ToArray()));
                }
            }
            rows.Sort((left, right) => StringComparer.Ordinal.Compare(left.OwnerKey, right.OwnerKey));

            section = new P12AOwnerSection(GenealogyCensusProvider.SectionId,
                GenealogyCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                P12ADailyV1Census.ExpectedAliasGroupForSection(GenealogyCensusProvider.SectionId),
                snapshot.Revision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                GenealogyCensusProvider.SectionId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out GenealogyOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? GenealogyCensusProvider.SectionId);
        if (section == null || section.Id != GenealogyCensusProvider.SectionId
            || section.Major != GenealogyCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required
            || section.AliasGroup != P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)
            || section.Revision < 0 || section.Rows.Count > P12AContinuationCodec.MaxRows)
            return false;

        try
        {
            List<ParentageRecord> records = new List<ParentageRecord>(section.Rows.Count);
            string priorKey = null;
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != section.Revision
                    || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes
                    || string.IsNullOrWhiteSpace(row.OwnerKey)
                    || (priorKey != null && StringComparer.Ordinal.Compare(priorKey, row.OwnerKey) >= 0)
                    || !TryReadRecord(row, out ParentageRecord record)
                    || row.OwnerKey != OwnerKey(record.ParentId.Value, record.ChildId.Value))
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                        section.Id, row?.OwnerKey);
                    return false;
                }
                priorKey = row.OwnerKey;
                records.Add(record);
            }

            GenealogyOwnerSnapshot candidate = new GenealogyOwnerSnapshot(
                GenealogyOwnerSnapshot.CurrentSchemaVersion, section.Revision, records);
            if (!TryValidate(candidate, out _, out result)) return false;
            snapshot = candidate;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, section.Id);
            return false;
        }
    }

    private static bool TryValidate(GenealogyOwnerSnapshot snapshot,
        out List<ParentageRecord> ordered, out P12AResult result)
    {
        ordered = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            GenealogyCensusProvider.SectionId);
        if (snapshot == null || snapshot.SchemaVersion != GenealogyOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0 || snapshot.Records == null
            || snapshot.Records.Count > P12AContinuationCodec.MaxRows
            || snapshot.Revision < snapshot.Records.Count) return false;

        List<ParentageRecord> values = new List<ParentageRecord>(snapshot.Records.Count);
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, List<string>> childrenByParent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (ParentageRecord record in snapshot.Records)
        {
            if (record?.ParentId == null || record.ChildId == null
                || string.IsNullOrWhiteSpace(record.ParentId.Value)
                || string.IsNullOrWhiteSpace(record.ChildId.Value)
                || record.ParentId == record.ChildId)
                return false;
            try
            {
                P12AWire.GetEncodedStringByteCount(record.ParentId.Value);
                P12AWire.GetEncodedStringByteCount(record.ChildId.Value);
                string key = OwnerKey(record.ParentId.Value, record.ChildId.Value);
                P12AWire.GetEncodedStringByteCount(key);
                if (!keys.Add(key)) return false;
            }
            catch (Exception exception) when (IsWireException(exception))
            {
                result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                    ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                    GenealogyCensusProvider.SectionId);
                return false;
            }
            if (!childrenByParent.TryGetValue(record.ParentId.Value, out List<string> children))
            {
                children = new List<string>();
                childrenByParent.Add(record.ParentId.Value, children);
            }
            children.Add(record.ChildId.Value);
            values.Add(new ParentageRecord(new PersonId(record.ParentId.Value),
                new PersonId(record.ChildId.Value)));
        }

        if (HasCycle(childrenByParent)) return false;
        values.Sort(CompareRecords);
        ordered = values;
        result = null;
        return true;
    }

    private static bool TryReadRecord(P12AOwnerRow row, out ParentageRecord record)
    {
        record = null;
        byte[] payload = row.Payload;
        if (payload == null || payload.Length != row.PayloadLength) return false;
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                || !P12AWire.ReadString(reader, out string parent) || string.IsNullOrWhiteSpace(parent)
                || !P12AWire.ReadString(reader, out string child) || string.IsNullOrWhiteSpace(child)
                || stream.Position != stream.Length || parent == child) return false;
            record = new ParentageRecord(new PersonId(parent), new PersonId(child));
            return true;
        }
    }

    private static bool HasCycle(Dictionary<string, List<string>> childrenByParent)
    {
        HashSet<string> people = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, int> incomingEdges = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<string>> item in childrenByParent)
        {
            people.Add(item.Key);
            if (!incomingEdges.ContainsKey(item.Key)) incomingEdges.Add(item.Key, 0);
            foreach (string child in item.Value)
            {
                people.Add(child);
                incomingEdges.TryGetValue(child, out int incoming);
                incomingEdges[child] = incoming + 1;
            }
        }

        Queue<string> ready = new Queue<string>();
        foreach (string person in people)
            if (incomingEdges.TryGetValue(person, out int incoming) && incoming == 0)
                ready.Enqueue(person);
        int visited = 0;
        while (ready.Count > 0)
        {
            string parent = ready.Dequeue();
            visited++;
            if (!childrenByParent.TryGetValue(parent, out List<string> children)) continue;
            foreach (string child in children)
            {
                int remaining = incomingEdges[child] - 1;
                incomingEdges[child] = remaining;
                if (remaining == 0) ready.Enqueue(child);
            }
        }
        return visited != people.Count;
    }

    private static string OwnerKey(string parentId, string childId)
        => parentId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + parentId
            + childId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + childId;

    private static int CompareRecords(ParentageRecord left, ParentageRecord right)
    {
        int parent = StringComparer.Ordinal.Compare(left.ParentId.Value, right.ParentId.Value);
        return parent != 0 ? parent : StringComparer.Ordinal.Compare(left.ChildId.Value, right.ChildId.Value);
    }

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is EncoderFallbackException || exception is DecoderFallbackException;
}
