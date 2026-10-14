using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>One selected serialized asset record using the source CLR tokens and field order.</summary>
internal sealed class DailyV1ContentRecord
{
    public string TypeToken { get; }
    public IReadOnlyList<DailyV1DefinitionField> Fields { get; }

    public DailyV1ContentRecord(string typeToken, IEnumerable<DailyV1DefinitionField> fields)
    {
        TypeToken = string.IsNullOrWhiteSpace(typeToken) ? throw new ArgumentException("Content type token is required.", nameof(typeToken)) : typeToken;
        Fields = fields == null ? throw new ArgumentNullException(nameof(fields))
            : new System.Collections.ObjectModel.ReadOnlyCollection<DailyV1DefinitionField>(new List<DailyV1DefinitionField>(fields));
    }
}

/// <summary>
/// The sole P12 selected-catalog encoder. It consumes captured compatibility tokens and never
/// asks the executing CLR to synthesize legacy Unity type identities.
/// </summary>
internal static class DailyV1ContentProjection
{
    public const string CatalogIdentity = "p12a.official-content-catalog/v1";
    public const int MaxCatalogAssets = 65536;
    public const int MaxCatalogBytes = 64 * 1024 * 1024;
    public const int MaxValueDepth = 64;

    internal static bool TryEncode(IReadOnlyDictionary<string, DailyV1ContentRecord> records,
        out byte[] bytes, out string diagnostic)
    {
        bytes = null;
        if (records == null || records.Count == 0 || records.Count > MaxCatalogAssets)
        { diagnostic = "The Daily-v1 content catalog has an invalid record count."; return false; }
        try
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
            {
                ContinuationWire.WriteString(writer, CatalogIdentity);
                writer.Write(records.Count);
                foreach (KeyValuePair<string, DailyV1ContentRecord> record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Key) || record.Value == null)
                        throw new InvalidOperationException("The Daily-v1 catalog contains an invalid record.");
                    ContinuationWire.WriteString(writer, record.Key);
                    byte[] payload = SerializeObject(record.Value);
                    writer.Write(payload.Length);
                    writer.Write(payload);
                    if (stream.Length > MaxCatalogBytes)
                        throw new InvalidOperationException("The Daily-v1 content catalog exceeds its byte bound.");
                }
                writer.Flush();
                bytes = stream.ToArray();
            }
            diagnostic = null;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is EncoderFallbackException || exception is IOException)
        {
            bytes = null;
            diagnostic = exception.Message;
            return false;
        }
    }

    internal static string FingerprintRecords(IReadOnlyDictionary<string, DailyV1ContentRecord> records)
    {
        if (!TryEncode(records, out byte[] bytes, out string diagnostic))
            throw new InvalidOperationException(diagnostic ?? "The Daily-v1 content projection is invalid.");
        using (SHA256 sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes));
    }

    internal static byte[] SerializeObject(DailyV1ContentRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            ContinuationWire.WriteString(writer, record.TypeToken);
            writer.Write(record.Fields.Count);
            foreach (DailyV1DefinitionField field in record.Fields)
            {
                ContinuationWire.WriteString(writer, field.DeclaringTypeToken);
                ContinuationWire.WriteString(writer, field.Name);
                WriteValue(writer, field.Value, 0);
            }
            writer.Flush();
            if (stream.Length > MaxCatalogBytes)
                throw new InvalidOperationException("A Daily-v1 content definition exceeds its byte bound.");
            return stream.ToArray();
        }
    }

    private static void WriteValue(BinaryWriter writer, DailyV1DefinitionValue value, int depth)
    {
        if (depth > MaxValueDepth) throw new InvalidOperationException("Content value exceeds its depth bound.");
        if (value == null || value.Kind == DailyV1DefinitionValue.ValueKind.Null)
        { writer.Write((byte)0); return; }
        ContinuationWire.WriteString(writer, value.TypeToken);
        switch (value.Kind)
        {
            case DailyV1DefinitionValue.ValueKind.Reference:
                writer.Write((byte)1);
                ContinuationWire.WriteString(writer, value.ReferenceKey);
                return;
            case DailyV1DefinitionValue.ValueKind.Scalar:
                WriteScalar(writer, value.Scalar);
                return;
            case DailyV1DefinitionValue.ValueKind.Enum:
                writer.Write((byte)3);
                ContinuationWire.WriteString(writer, value.EnumSpelling);
                return;
            case DailyV1DefinitionValue.ValueKind.List:
                writer.Write((byte)20);
                writer.Write(value.Items.Count);
                foreach (DailyV1DefinitionValue item in value.Items) WriteValue(writer, item, depth + 1);
                return;
            case DailyV1DefinitionValue.ValueKind.Object:
                writer.Write((byte)21);
                writer.Write(value.Fields.Count);
                foreach (DailyV1DefinitionField field in value.Fields)
                {
                    ContinuationWire.WriteString(writer, field.DeclaringTypeToken);
                    ContinuationWire.WriteString(writer, field.Name);
                    WriteValue(writer, field.Value, depth + 1);
                }
                return;
            default:
                throw new InvalidOperationException("Unsupported Daily-v1 content value kind.");
        }
    }

    private static void WriteScalar(BinaryWriter writer, object value)
    {
        if (value is string text) { writer.Write((byte)2); ContinuationWire.WriteString(writer, text); return; }
        if (value is bool boolean) { writer.Write((byte)4); writer.Write(boolean); return; }
        if (value is byte byteValue) { writer.Write((byte)5); writer.Write(byteValue); return; }
        if (value is sbyte sbyteValue) { writer.Write((byte)6); writer.Write(sbyteValue); return; }
        if (value is short shortValue) { writer.Write((byte)7); writer.Write(shortValue); return; }
        if (value is ushort ushortValue) { writer.Write((byte)8); writer.Write(ushortValue); return; }
        if (value is int intValue) { writer.Write((byte)9); writer.Write(intValue); return; }
        if (value is uint uintValue) { writer.Write((byte)10); writer.Write(uintValue); return; }
        if (value is long longValue) { writer.Write((byte)11); writer.Write(longValue); return; }
        if (value is ulong ulongValue) { writer.Write((byte)12); writer.Write(ulongValue); return; }
        if (value is char charValue) { writer.Write((byte)13); writer.Write((ushort)charValue); return; }
        if (value is float singleValue) { writer.Write((byte)14); writer.Write(BitConverter.ToInt32(BitConverter.GetBytes(singleValue), 0)); return; }
        if (value is double doubleValue) { writer.Write((byte)15); writer.Write(BitConverter.ToInt64(BitConverter.GetBytes(doubleValue), 0)); return; }
        if (value is decimal decimalValue)
        { writer.Write((byte)16); foreach (int part in decimal.GetBits(decimalValue)) writer.Write(part); return; }
        if (value is Guid guid) { writer.Write((byte)17); ContinuationWire.WriteString(writer, guid.ToString("N")); return; }
        if (value is DateTime dateTime) { writer.Write((byte)18); writer.Write(dateTime.ToBinary()); return; }
        if (value is TimeSpan timeSpan) { writer.Write((byte)19); writer.Write(timeSpan.Ticks); return; }
        throw new InvalidOperationException("The Daily-v1 content catalog contains an unsupported scalar.");
    }

    private static string Hex(byte[] bytes)
    {
        char[] chars = new char[bytes.Length * 2];
        const string digits = "0123456789abcdef";
        for (int i = 0; i < bytes.Length; i++)
        { chars[i * 2] = digits[bytes[i] >> 4]; chars[i * 2 + 1] = digits[bytes[i] & 15]; }
        return new string(chars);
    }
}
