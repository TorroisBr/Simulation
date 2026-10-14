using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

internal static class P12AContinuationCodec
{
    internal const int MaxFrameBytes = ContinuationWire.MaxFrameBytes;
    internal const int MaxHeaderBytes = ContinuationWire.MaxHeaderBytes;
    internal const int MaxSections = ContinuationWire.MaxSections;
    internal const int MaxSectionBytes = ContinuationWire.MaxSectionBytes;
    internal const int MaxStringBytes = ContinuationWire.MaxStringBytes;
    internal const int MaxRows = ContinuationWire.MaxRows;
    private static readonly byte[] Magic = { 0x53, 0x49, 0x4D, 0x44, 0x4C, 0x59, 0x31, 0x00 };

    internal static bool TryEncode(P12AContinuationImage image, P12ADailyV1SectionManifest manifest,
        out byte[] bytes, out P12AResult result)
    {
        bytes = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue);
        if (image == null || manifest == null || !image.Header.IsValid
            || !image.CausalRoots.IsValidFor(image.Header)) return false;
        int wireSectionCount = image.Sections.Count + 1;
        if (wireSectionCount > MaxSections) { result = new P12AResult(P12AResultCode.LimitExceeded); return false; }
        if (!manifest.Matches(image.Sections, out result)) return false;
        if (!P12ACausalRootOwnerCodec.TryEncodeContinuationRoots(image.CausalRoots, image.Header,
            out P12AOwnerSection causalRootsSection, out result)) return false;
        try
        {
            byte[] header = HeaderBytes(image.Header);
            if (header.Length > MaxHeaderBytes) { result = new P12AResult(P12AResultCode.LimitExceeded); return false; }
            List<P12AOwnerSection> wireSections = new List<P12AOwnerSection>(wireSectionCount)
            { causalRootsSection };
            wireSections.AddRange(image.Sections);
            wireSections.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic); writer.Write((ushort)1); writer.Write((ushort)0);
                writer.Write((uint)header.Length); writer.Write((uint)wireSections.Count); writer.Write(header);
                foreach (P12AOwnerSection section in wireSections)
                {
                    if (!EncodeRows(section, out byte[] payload, out P12AResultCode rowFailure))
                    { result = new P12AResult(rowFailure, section.Id); return false; }
                    WriteSection(writer, section, payload); writer.Flush();
                    if (stream.Length + 32 > MaxFrameBytes)
                    { result = new P12AResult(P12AResultCode.LimitExceeded); return false; }
                }
                byte[] prefix = stream.ToArray(); byte[] digest;
                using (SHA256 sha = SHA256.Create()) digest = sha.ComputeHash(prefix);
                stream.Position = stream.Length; writer.Write(digest); writer.Flush();
                bytes = stream.ToArray();
            }
            result = new P12AResult(P12AResultCode.Encoded); return true;
        }
        catch (P12ALimitException) { result = new P12AResult(P12AResultCode.LimitExceeded); return false; }
        catch (EncoderFallbackException) { result = new P12AResult(P12AResultCode.InvalidOwnerValue); return false; }
        catch (OverflowException) { result = new P12AResult(P12AResultCode.LimitExceeded); return false; }
        catch (ArgumentException) { result = new P12AResult(P12AResultCode.InvalidOwnerValue); return false; }
    }

    internal static bool TryDecode(byte[] bytes, P12ADailyV1SectionManifest manifest,
        P12ACompatibilityDescriptor expected, out P12AContinuationImage image, out P12AResult result)
    {
        image = null; result = new P12AResult(P12AResultCode.CorruptOrTruncated);
        if (bytes == null || expected == null) return false;
        if (bytes.Length < 56 || bytes.Length > MaxFrameBytes)
        { result = new P12AResult(bytes.Length > MaxFrameBytes ? P12AResultCode.LimitExceeded : P12AResultCode.CorruptOrTruncated); return false; }
        int contentLength = bytes.Length - 32;
        if (!HasDigest(bytes, contentLength)) { result = new P12AResult(P12AResultCode.DigestMismatch); return false; }
        try
        {
            using (MemoryStream stream = new MemoryStream(bytes, 0, contentLength, false, true))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                if (!BytesEqual(reader.ReadBytes(Magic.Length), Magic)) return false;
                ushort major = reader.ReadUInt16(), minor = reader.ReadUInt16();
                if (major != 1 || minor != 0) { result = new P12AResult(P12AResultCode.UnsupportedSchema); return false; }
                uint headerLength = reader.ReadUInt32(), sectionCount = reader.ReadUInt32();
                if (headerLength > MaxHeaderBytes || sectionCount > MaxSections)
                { result = new P12AResult(P12AResultCode.LimitExceeded); return false; }
                if (stream.Length - stream.Position < headerLength) return false;
                byte[] headerBytes = reader.ReadBytes((int)headerLength);
                if (headerBytes.Length != headerLength
                    || !ReadHeader(headerBytes, expected, out P12AContinuationHeader header, out result)) return false;
                if (sectionCount == 0) return false;
                List<P12AOwnerSection> sections = new List<P12AOwnerSection>((int)sectionCount - 1);
                P12ACausalRootPayload causalRoots = null;
                string prior = null;
                for (int i = 0; i < (int)sectionCount; i++)
                {
                    if (!ReadSection(reader, stream, out P12AOwnerSection section, out result)) return false;
                    if (prior != null && StringComparer.Ordinal.Compare(prior, section.Id) >= 0)
                    { result = new P12AResult(P12AResultCode.MissingOrDuplicateSection, section.Id); return false; }
                    prior = section.Id;
                    if (section.Id == P12ACausalRootPayload.SectionId)
                    {
                        if (causalRoots != null || !P12ACausalRootOwnerCodec.TryDecodeContinuationRoots(
                            section, header, out causalRoots, out result)) return false;
                    }
                    else sections.Add(section);
                }
                if (causalRoots == null) { result = new P12AResult(P12AResultCode.MissingOrDuplicateSection,
                    P12ACausalRootPayload.SectionId); return false; }
                image = new P12AContinuationImage(header, causalRoots, sections);
                if (stream.Position != contentLength) { image = null; return false; }
                if (manifest != null)
                {
                    if (!manifest.Matches(sections, out result)) { image = null; return false; }
                }
                else if (!P12ADailyV1Census.TryCreateForImage(image, out _, out result))
                { image = null; return false; }
                result = new P12AResult(P12AResultCode.Decoded); return true;
            }
        }
        catch (P12ALimitException) { result = new P12AResult(P12AResultCode.LimitExceeded); return false; }
        catch (EndOfStreamException) { return false; }
        catch (DecoderFallbackException) { result = new P12AResult(P12AResultCode.InvalidOwnerValue); return false; }
        catch (IOException) { return false; }
        catch (ArgumentException) { result = new P12AResult(P12AResultCode.InvalidOwnerValue); return false; }
    }

    private static byte[] HeaderBytes(P12AContinuationHeader header)
    {
        byte[] descriptor = header.Compatibility.CanonicalBytes;
        long encodedLength = 40L + descriptor.Length
            + P12AWire.GetEncodedStringFieldLength(P12AContinuationHeader.Profile)
            + P12AWire.GetEncodedStringFieldLength(header.WorldId)
            + P12AWire.GetEncodedStringFieldLength(header.P9ProfileStage)
            + P12AWire.GetEncodedStringFieldLength(header.P9ManifestFingerprint)
            + P12AWire.GetEncodedStringFieldLength(header.P8Profile)
            + P12AWire.GetEncodedStringFieldLength(header.P8GeographyFingerprint);
        if (encodedLength > MaxHeaderBytes) throw new P12ALimitException();
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            P12AWire.WriteString(writer, P12AContinuationHeader.Profile);
            writer.Write((ushort)P12AContinuationHeader.ProfileVersion); writer.Write((ushort)0);
            P12AWire.WriteString(writer, header.WorldId); writer.Write(header.Day);
            writer.Write(header.Sequence); writer.Write(header.LastEconomySnapshotDay);
            writer.Write(header.PoliticalWorldRevision);
            P12AWire.WriteString(writer, header.P9ProfileStage);
            P12AWire.WriteString(writer, header.P9ManifestFingerprint);
            P12AWire.WriteString(writer, header.P8Profile);
            P12AWire.WriteString(writer, header.P8GeographyFingerprint);
            writer.Write(descriptor.Length); writer.Write(descriptor); writer.Flush();
            return stream.ToArray();
        }
    }

    private static bool ReadHeader(byte[] bytes, P12ACompatibilityDescriptor expected,
        out P12AContinuationHeader header, out P12AResult result)
    {
        header = null; result = new P12AResult(P12AResultCode.CorruptOrTruncated);
        using (MemoryStream stream = new MemoryStream(bytes, false))
        using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            if (!RequiredString(reader, out string profile)) return false;
            if (profile != P12AContinuationHeader.Profile)
            { result = new P12AResult(P12AResultCode.UnsupportedProfile); return false; }
            if (reader.ReadUInt16() != P12AContinuationHeader.ProfileVersion || reader.ReadUInt16() != 0)
            { result = new P12AResult(P12AResultCode.UnsupportedSchema); return false; }
            if (!RequiredString(reader, out string world)) return false;
            long day = reader.ReadInt64(), sequence = reader.ReadInt64(), reportDay = reader.ReadInt64();
            long politicalWorldRevision = reader.ReadInt64();
            if (!RequiredString(reader, out string p9) || !RequiredString(reader, out string p9hash)) return false;
            if (!RequiredString(reader, out string p8) || !RequiredString(reader, out string p8hash)) return false;
            int length = reader.ReadInt32();
            if (length < 0 || length > MaxHeaderBytes || stream.Length - stream.Position < length) return false;
            byte[] descriptor = reader.ReadBytes((int)length);
            if (descriptor.Length != length || stream.Position != stream.Length) return false;
            if (descriptor.Length >= 4 && descriptor[0] == 0xFE && descriptor[1] == 0xFF && descriptor[2] == 0xFF && descriptor[3] == 0xFF) { result = new P12AResult(P12ACompatibilityAdmission.ClassifyUnityV2(descriptor)); return false; }
            if (!P12ACompatibilityDescriptor.TryReadCanonical(descriptor, out P12ACompatibilityDescriptor actual))
            { result = new P12AResult(P12AResultCode.CorruptOrTruncated); return false; }
            if (!expected.Matches(descriptor))
            { result = new P12AResult(expected.MismatchCode(actual)); return false; }
            header = new P12AContinuationHeader(world, day, sequence, reportDay,
                p9, p9hash, p8, p8hash, politicalWorldRevision, expected);
            if (!header.IsValid) { header = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue); return false; }
            return true;
        }
    }

    private static bool EncodeRows(P12AOwnerSection section, out byte[] payload, out P12AResultCode failure)
    {
        payload = null; failure = P12AResultCode.InvalidOwnerValue;
        if (section.Rows.Count > MaxRows) { failure = P12AResultCode.LimitExceeded; return false; }
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        long encodedLength = sizeof(int);
        foreach (P12AOwnerRow row in section.Rows)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.OwnerKey) || row.Revision < 0
                || row.PayloadLength < 0 || !keys.Add(row.OwnerKey)) return false;
            int keyLength = P12AWire.GetEncodedStringByteCount(row.OwnerKey);
            if (row.PayloadLength > MaxSectionBytes)
            { failure = P12AResultCode.LimitExceeded; return false; }
            encodedLength = checked(encodedLength + sizeof(int) + keyLength + sizeof(long)
                + sizeof(int) + row.PayloadLength);
            if (encodedLength > MaxSectionBytes)
            { failure = P12AResultCode.LimitExceeded; return false; }
        }
        using (MemoryStream stream = new MemoryStream((int)encodedLength))
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(section.Rows.Count);
            foreach (P12AOwnerRow row in section.Rows)
            {
                int valueLength = row.PayloadLength;
                P12AWire.WriteString(writer, row.OwnerKey); writer.Write(row.Revision);
                writer.Write(valueLength); row.WritePayload(writer);
            }
            writer.Flush(); payload = stream.ToArray();
            if (payload.Length != encodedLength || payload.Length > MaxSectionBytes)
            { payload = null; failure = P12AResultCode.LimitExceeded; return false; }
            failure = P12AResultCode.Encoded; return true;
        }
    }

    private static void WriteSection(BinaryWriter writer, P12AOwnerSection section, byte[] payload)
    {
        P12AWire.WriteString(writer, section.Id);
        writer.Write((ushort)section.Major); writer.Write((ushort)section.Minor);
        writer.Write((ushort)section.Role); P12AWire.WriteString(writer, section.AliasGroup);
        writer.Write(section.Rows.Count); writer.Write(section.Revision);
        writer.Write(payload.Length); writer.Write(payload);
    }

    private static bool ReadSection(BinaryReader reader, Stream frame,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null; result = new P12AResult(P12AResultCode.CorruptOrTruncated);
        if (!RequiredString(reader, out string id)) return false;
        ushort major = reader.ReadUInt16(), minor = reader.ReadUInt16(); ushort role = reader.ReadUInt16();
        if (role > (ushort)OwnerSectionRole.Excluded || !P12AWire.ReadString(reader, out string alias)) return false;
        int count = reader.ReadInt32(); long revision = reader.ReadInt64(); int length = reader.ReadInt32();
        if (count < 0 || count > MaxRows || length < 0 || length > MaxSectionBytes)
        { result = new P12AResult(P12AResultCode.LimitExceeded, id); return false; }
        if (revision < 0 || frame.Length - frame.Position < length) return false;
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length || !ReadRows(bytes, count, out List<P12AOwnerRow> rows))
        { result = new P12AResult(P12AResultCode.InvalidOwnerValue, id); return false; }
        section = new P12AOwnerSection(id, major, minor, (OwnerSectionRole)role, alias, revision, rows);
        return true;
    }

    private static bool ReadRows(byte[] bytes, int expected, out List<P12AOwnerRow> rows)
    {
        rows = null;
        using (MemoryStream stream = new MemoryStream(bytes, false))
        using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            int count = reader.ReadInt32(); if (count < 0 || count > MaxRows || count != expected) return false;
            List<P12AOwnerRow> values = new List<P12AOwnerRow>(count);
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                if (!RequiredString(reader, out string key) || !keys.Add(key)) return false;
                long revision = reader.ReadInt64(); int length = reader.ReadInt32();
                if (revision < 0 || length < 0 || length > MaxSectionBytes || stream.Length - stream.Position < length) return false;
                byte[] value = reader.ReadBytes(length);
                if (value.Length != length) return false;
                values.Add(new P12AOwnerRow(key, revision, value));
            }
            if (stream.Position != stream.Length) return false;
            rows = values; return true;
        }
    }

    private static bool RequiredString(BinaryReader reader, out string value)
    { return P12AWire.ReadString(reader, out value) && value != null; }

    private static bool HasDigest(byte[] bytes, int contentLength)
    {
        byte[] supplied = new byte[32]; Buffer.BlockCopy(bytes, contentLength, supplied, 0, 32);
        byte[] prefix = new byte[contentLength]; Buffer.BlockCopy(bytes, 0, prefix, 0, contentLength);
        byte[] actual; using (SHA256 sha = SHA256.Create()) actual = sha.ComputeHash(prefix);
        return BytesEqual(actual, supplied);
    }

    internal static bool BytesEqual(byte[] left, byte[] right) => ContinuationWire.BytesEqual(left, right);
}
