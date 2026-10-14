using System;
using System.IO;
using System.Text;

public enum P12AResultCode
{
    NoSave, Saved, Loaded, Encoded, Decoded, WrongThread, Busy, CaptureRejected, UnsupportedProfile,
    IncompatibleBuildOrRuntime, IncompatibleContentOrConfiguration, UnsupportedSchema,
    CorruptOrTruncated, DigestMismatch, LimitExceeded, MissingOrDuplicateSection,
    InvalidOwnerValue, OwnerGraphRejected, AdmissionRejected, StorageFailure
}

public sealed class P12ALimitException : Exception { }

public static class ContinuationWire
{
    public const int MaxFrameBytes = 128 * 1024 * 1024;
    public const int MaxHeaderBytes = 64 * 1024;
    public const int MaxSections = 4096;
    public const int MaxSectionBytes = 16 * 1024 * 1024;
    public const int MaxStringBytes = 1024 * 1024;
    public const int MaxRows = 1_000_000;
    public static Encoding CreateStrictUtf8Encoding() => new UTF8Encoding(false, true);
    public static int GetEncodedStringByteCount(string value)
    {
        if (value == null) return 0;
        int count = StrictUtf8.GetByteCount(value);
        if (count > MaxStringBytes) throw new P12ALimitException();
        return count;
    }
    public static long GetEncodedStringFieldLength(string value) => checked(4L + GetEncodedStringByteCount(value));
    public static void WriteString(BinaryWriter writer, string value)
    {
        if (value == null) { writer.Write(-1); return; }
        int count = GetEncodedStringByteCount(value);
        writer.Write(count);
        byte[] bytes = StrictUtf8.GetBytes(value);
        if (bytes.Length != count) throw new InvalidOperationException("UTF-8 byte count changed during encoding.");
        writer.Write(bytes);
    }
    public static bool ReadString(BinaryReader reader, out string value)
    {
        value = null;
        int length;
        try { length = reader.ReadInt32(); } catch (EndOfStreamException) { return false; }
        if (length == -1) return true;
        if (length < -1) return false;
        if (length > MaxStringBytes) throw new P12ALimitException();
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length) return false;
        try { value = StrictUtf8.GetString(bytes); } catch (DecoderFallbackException) { return false; }
        return true;
    }
    public static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length) return false;
        int difference = 0;
        for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
        return difference == 0;
    }
    private static UTF8Encoding StrictUtf8 => new UTF8Encoding(false, true);
}

internal static class P12AWire
{
    internal static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
    internal static int GetEncodedStringByteCount(string value) => ContinuationWire.GetEncodedStringByteCount(value);
    internal static long GetEncodedStringFieldLength(string value) => ContinuationWire.GetEncodedStringFieldLength(value);
    internal static void WriteString(BinaryWriter writer, string value) => ContinuationWire.WriteString(writer, value);
    internal static bool ReadString(BinaryReader reader, out string value) => ContinuationWire.ReadString(reader, out value);
}