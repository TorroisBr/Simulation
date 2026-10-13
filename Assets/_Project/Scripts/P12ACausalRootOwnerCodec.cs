using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit codecs for the small C-owner snapshots with scalar contracts.</summary>
internal static class P12ACausalRootOwnerCodec
{
    private const string AllocatorOwnerKey = "runtime-id-allocator";
    private const string SequenceOwnerKey = "simulation-record-sequence";
    private const string ContinuationRootsOwnerKey = P12ACausalRootPayload.OwnerKey;
    private static readonly string[] AllocatorFamilies =
    {
        "npc", "city", "location", "route", "event", "directive", "decision", "travel-party",
        "organization", "site", "expedition", "local-place", "local-connection", "notable-item"
    };

    internal static bool TryEncodeRandomRoot(DeterministicRandomRootSnapshot snapshot,
        out byte[] bytes, out P12AResult result)
    {
        bytes = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p12c.random-root");
        if (!IsSupportedRandomRoot(snapshot)) return false;
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, snapshot.SchemaId); writer.Write(snapshot.SchemaVersion);
            P12AWire.WriteString(writer, snapshot.ProviderId); writer.Write(snapshot.ProviderVersion);
            P12AWire.WriteString(writer, snapshot.AlgorithmId); writer.Write(snapshot.AlgorithmVersion);
            writer.Write(snapshot.Seed); writer.Flush(); bytes = stream.ToArray(); result = null; return true;
        }
    }

    internal static bool TryDecodeRandomRoot(byte[] bytes,
        out DeterministicRandomRootSnapshot snapshot, out P12AResult result)
    {
        snapshot = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, "p12c.random-root");
        if (bytes == null || bytes.Length > P12AContinuationCodec.MaxSectionBytes) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!P12AWire.ReadString(reader, out string schema) || schema == null) return false;
                int schemaVersion = reader.ReadInt32();
                if (!P12AWire.ReadString(reader, out string provider) || provider == null) return false;
                int providerVersion = reader.ReadInt32();
                if (!P12AWire.ReadString(reader, out string algorithm) || algorithm == null) return false;
                int algorithmVersion = reader.ReadInt32(); int seed = reader.ReadInt32();
                DeterministicRandomRootSnapshot candidate = new DeterministicRandomRootSnapshot(
                    schema, schemaVersion, provider, providerVersion, algorithm, algorithmVersion, seed);
                if (stream.Position != stream.Length || !IsSupportedRandomRoot(candidate)) return false;
                snapshot = candidate; result = null; return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException)
        { snapshot = null; return false; }
    }

    private static bool IsSupportedRandomRoot(DeterministicRandomRootSnapshot snapshot) => snapshot != null
        && snapshot.SchemaId == "deterministic-random-root" && snapshot.SchemaVersion == 1
        && snapshot.ProviderId == "simulation/deterministic-random-source" && snapshot.ProviderVersion == 1
        && snapshot.AlgorithmId == "fnv1a64-keyed-utf16" && snapshot.AlgorithmVersion == 1;

    internal static bool TryEncodeContinuationRoots(P12ACausalRootPayload roots,
        P12AContinuationHeader header, out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            P12ACausalRootPayload.SectionId, ContinuationRootsOwnerKey);
        if (roots == null || !roots.IsValidFor(header)
            || !P12ACP9ManifestCodec.TryEncode(roots.P9Manifest, out byte[] p9Bytes, out _)
            || !TryEncodeRandomRoot(roots.RandomRoot, out byte[] randomBytes, out _)) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                writer.Write(p9Bytes.Length); writer.Write(p9Bytes);
                writer.Write(randomBytes.Length); writer.Write(randomBytes);
                writer.Flush();
                if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                { result = new P12AResult(P12AResultCode.LimitExceeded, P12ACausalRootPayload.SectionId); return false; }
                P12AOwnerRow row = new P12AOwnerRow(ContinuationRootsOwnerKey, 0, stream.ToArray());
                section = new P12AOwnerSection(P12ACausalRootPayload.SectionId,
                    P12ACausalRootPayload.SchemaVersion, 0, OwnerSectionRole.Required, null, 0, new[] { row });
            }
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is ArgumentException
            || exception is OverflowException)
        { section = null; return false; }
    }

    internal static bool TryDecodeContinuationRoots(P12AOwnerSection section,
        P12AContinuationHeader header, out P12ACausalRootPayload roots, out P12AResult result)
    {
        roots = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            P12ACausalRootPayload.SectionId, ContinuationRootsOwnerKey);
        if (section == null || section.Id != P12ACausalRootPayload.SectionId
            || section.Major != P12ACausalRootPayload.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision != 0 || section.Rows.Count != 1
            || section.Rows[0].OwnerKey != ContinuationRootsOwnerKey
            || section.Rows[0].Revision != 0 || section.Rows[0].Payload == null) return false;
        byte[] payload = section.Rows[0].Payload;
        try
        {
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!ReadNestedValue(reader, stream, out byte[] p9Bytes)
                    || !ReadNestedValue(reader, stream, out byte[] randomBytes)
                    || stream.Position != stream.Length
                    || !P12ACP9ManifestCodec.TryDecode(p9Bytes,
                        out P12CP9GenesisManifestSnapshot p9Manifest, out _)
                    || !TryDecodeRandomRoot(randomBytes,
                        out DeterministicRandomRootSnapshot randomRoot, out _)) return false;
                P12ACausalRootPayload candidate = new P12ACausalRootPayload(p9Manifest, randomRoot);
                if (!candidate.IsValidFor(header)) return false;
                roots = candidate; result = null; return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException
            || exception is OverflowException)
        { roots = null; return false; }
    }

    private static bool ReadNestedValue(BinaryReader reader, Stream stream, out byte[] value)
    {
        value = null;
        int length = reader.ReadInt32();
        if (length < 0 || length > P12AContinuationCodec.MaxSectionBytes
            || stream.Length - stream.Position < length) return false;
        value = reader.ReadBytes(length);
        return value.Length == length;
    }

    internal static bool TryEncodeRuntimeIdAllocator(RuntimeIdAllocatorSnapshot snapshot, string sectionId,
        long sectionRevision, string aliasGroup, out P12AOwnerSection section, out P12AResult result)
    {
        section = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, AllocatorOwnerKey);
        if (!TryValidateAllocator(snapshot, out Dictionary<string, long> counters)) return false;
        string trackedFamily = TrackedFamily(sectionId);
        if (trackedFamily == null || counters[trackedFamily] - 1L != sectionRevision) return false;
        try
        {
            byte[] payload = EncodeAllocator(snapshot);
            P12AOwnerRow row = new P12AOwnerRow(AllocatorOwnerKey, sectionRevision, payload);
            section = new P12AOwnerSection(sectionId, RuntimeIdAllocatorCensusProvider.SchemaVersion, 0,
                OwnerSectionRole.Required, aliasGroup, sectionRevision, new[] { row });
            result = null; return true;
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException || exception is OverflowException)
        { section = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, AllocatorOwnerKey); return false; }
    }

    internal static bool TryDecodeRuntimeIdAllocator(P12AOwnerSection section,
        out RuntimeIdAllocatorSnapshot snapshot, out P12AResult result)
    {
        snapshot = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id, AllocatorOwnerKey);
        if (section == null || !IsAllocatorSection(section.Id) || section.Major != RuntimeIdAllocatorCensusProvider.SchemaVersion
            || section.Minor != 0 || section.Role != OwnerSectionRole.Required || section.Rows.Count != 1
            || section.Rows[0].OwnerKey != AllocatorOwnerKey || section.Revision < 0
            || section.Rows[0].Revision != section.Revision) return false;
        byte[] payload = section.Rows[0].Payload;
        if (payload == null) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!P12AWire.ReadString(reader, out string schemaId) || schemaId != RuntimeIdAllocatorSnapshot.CurrentSchemaId
                    || reader.ReadInt32() != RuntimeIdAllocatorSnapshot.CurrentSchemaVersion) return false;
                int count = reader.ReadInt32();
                if (count != AllocatorFamilies.Length) return false;
                List<RuntimeIdAllocatorCounterSnapshot> counters = new List<RuntimeIdAllocatorCounterSnapshot>(count);
                string trackedFamily = TrackedFamily(section.Id);
                long trackedNext = -1L;
                for (int i = 0; i < count; i++)
                {
                    if (!P12AWire.ReadString(reader, out string family) || family != AllocatorFamilies[i]) return false;
                    long next = reader.ReadInt64();
                    if (next < 1L || next == long.MaxValue) return false;
                    counters.Add(new RuntimeIdAllocatorCounterSnapshot(family, next));
                    if (family == trackedFamily) trackedNext = next;
                }
                if (stream.Position != stream.Length || trackedNext < 1L || trackedNext - 1L != section.Revision) return false;
                snapshot = new RuntimeIdAllocatorSnapshot(RuntimeIdAllocatorSnapshot.CurrentSchemaId,
                    RuntimeIdAllocatorSnapshot.CurrentSchemaVersion, counters);
                result = null; return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is P12ALimitException
            || exception is IOException || exception is ArgumentException || exception is DecoderFallbackException)
        { snapshot = null; return false; }
    }

    internal static bool TryEncodeRecordSequence(SimulationRecordSequenceSnapshot snapshot,
        long sectionRevision, string aliasGroup, out P12AOwnerSection section, out P12AResult result)
    {
        section = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            SimulationRecordSequenceCensusProvider.SectionId, SequenceOwnerKey);
        if (snapshot == null || snapshot.SchemaId != SimulationRecordSequenceSnapshot.CurrentSchemaId
            || snapshot.SchemaVersion != SimulationRecordSequenceSnapshot.CurrentSchemaVersion
            || snapshot.NextSequence < 1L || snapshot.NextSequence == long.MaxValue
            || snapshot.NextSequence - 1L != sectionRevision) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, snapshot.SchemaId); writer.Write(snapshot.SchemaVersion);
                writer.Write(snapshot.NextSequence); writer.Flush();
                P12AOwnerRow row = new P12AOwnerRow(SequenceOwnerKey, sectionRevision, stream.ToArray());
                section = new P12AOwnerSection(SimulationRecordSequenceCensusProvider.SectionId,
                    SimulationRecordSequenceCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
                    aliasGroup, sectionRevision, new[] { row });
            }
            result = null; return true;
        }
        catch (Exception exception) when (exception is P12ALimitException || exception is IOException
            || exception is ArgumentException)
        { section = null; return false; }
    }

    internal static bool TryDecodeRecordSequence(P12AOwnerSection section,
        out SimulationRecordSequenceSnapshot snapshot, out P12AResult result)
    {
        snapshot = null; result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id, SequenceOwnerKey);
        if (section == null || section.Id != SimulationRecordSequenceCensusProvider.SectionId
            || section.Major != SimulationRecordSequenceCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Rows.Count != 1
            || section.Rows[0].OwnerKey != SequenceOwnerKey || section.Revision < 0
            || section.Rows[0].Revision != section.Revision) return false;
        byte[] payload = section.Rows[0].Payload;
        if (payload == null) return false;
        try
        {
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!P12AWire.ReadString(reader, out string schema) || schema != SimulationRecordSequenceSnapshot.CurrentSchemaId)
                    return false;
                int version = reader.ReadInt32(); long next = reader.ReadInt64();
                if (version != SimulationRecordSequenceSnapshot.CurrentSchemaVersion
                    || next < 1L || next == long.MaxValue || next - 1L != section.Revision
                    || stream.Position != stream.Length) return false;
                snapshot = new SimulationRecordSequenceSnapshot(schema, version, next);
                result = null; return true;
            }
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is P12ALimitException
            || exception is IOException || exception is ArgumentException || exception is DecoderFallbackException)
        { snapshot = null; return false; }
    }

    private static byte[] EncodeAllocator(RuntimeIdAllocatorSnapshot snapshot)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, snapshot.SchemaId); writer.Write(snapshot.SchemaVersion);
            writer.Write(AllocatorFamilies.Length);
            foreach (RuntimeIdAllocatorCounterSnapshot counter in snapshot.Counters)
            { P12AWire.WriteString(writer, counter.FamilyId); writer.Write(counter.NextSequence); }
            writer.Flush(); return stream.ToArray();
        }
    }

    private static bool TryValidateAllocator(RuntimeIdAllocatorSnapshot snapshot, out Dictionary<string, long> counters)
    {
        counters = null;
        if (snapshot == null || snapshot.SchemaId != RuntimeIdAllocatorSnapshot.CurrentSchemaId
            || snapshot.SchemaVersion != RuntimeIdAllocatorSnapshot.CurrentSchemaVersion
            || snapshot.Counters == null || snapshot.Counters.Count != AllocatorFamilies.Length) return false;
        Dictionary<string, long> values = new Dictionary<string, long>(StringComparer.Ordinal);
        for (int i = 0; i < AllocatorFamilies.Length; i++)
        {
            RuntimeIdAllocatorCounterSnapshot counter = snapshot.Counters[i];
            if (counter == null || counter.FamilyId != AllocatorFamilies[i]
                || counter.NextSequence < 1L || counter.NextSequence == long.MaxValue
                || !values.TryAdd(counter.FamilyId, counter.NextSequence)) return false;
        }
        counters = values; return true;
    }

    private static string TrackedFamily(string sectionId)
    {
        if (sectionId == RuntimeIdAllocatorCensusProvider.EventsSectionId) return "event";
        if (sectionId == RuntimeIdAllocatorCensusProvider.DecisionsSectionId) return "decision";
        if (sectionId == RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId) return "travel-party";
        return null;
    }
    private static bool IsAllocatorSection(string sectionId) => TrackedFamily(sectionId) != null;
    internal static string AllocatorOwnerKeyForSection(string sectionId)
        => IsAllocatorSection(sectionId) ? AllocatorOwnerKey : null;
}
