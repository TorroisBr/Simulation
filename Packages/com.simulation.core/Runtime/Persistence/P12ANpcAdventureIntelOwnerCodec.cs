using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Detached captured values for one NPC's four shared-revision adventure-intel sections.</summary>
internal sealed class P12ANpcAdventureIntelOwnerValue
{
    internal P12ANpcAdventureIntelOwnerValue(string runtimeId,
        IEnumerable<AdventureOppositionObservation> opposition,
        IEnumerable<AdventureNotableItemObservation> notableItems,
        IEnumerable<AdventureCommonResourceObservation> resources,
        IEnumerable<AdventureAccessObservation> access, long revision)
    {
        RuntimeId = runtimeId;
        Opposition = Copy(opposition);
        NotableItems = Copy(notableItems);
        Resources = Copy(resources);
        Access = Copy(access);
        Revision = revision;
    }

    internal string RuntimeId { get; }
    internal IReadOnlyList<AdventureOppositionObservation> Opposition { get; }
    internal IReadOnlyList<AdventureNotableItemObservation> NotableItems { get; }
    internal IReadOnlyList<AdventureCommonResourceObservation> Resources { get; }
    internal IReadOnlyList<AdventureAccessObservation> Access { get; }
    internal long Revision { get; }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
        => Array.AsReadOnly((source ?? throw new ArgumentNullException(nameof(source))).ToArray());
}

/// <summary>Exact P12-A codecs for the four per-NPC AdventureSiteIntel owner sections.</summary>
internal static class P12ANpcAdventureIntelOwnerCodec
{
    private const string PayloadContract = "p12a.npc-adventure-intel/v1";
    private const int OppositionKind = 3;
    private const int NotableItemKind = 4;
    private const int CommonResourceKind = 5;
    private const int AccessKind = 6;

    internal static bool TryEncode(P12DNpcFRow source,
        out P12AOwnerSection opposition, out P12AOwnerSection notableItems,
        out P12AOwnerSection resources, out P12AOwnerSection access, out P12AResult result)
    {
        opposition = notableItems = resources = access = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(OppositionKind, source?.RuntimeId), source?.RuntimeId);
        if (source == null || source.OppositionObservations == null || source.NotableItemObservations == null
            || source.CommonResourceObservations == null || source.AccessObservations == null) return false;
        int exceededKind = FirstExceededKind(source.OppositionObservations.Count,
            source.NotableItemObservations.Count, source.CommonResourceObservations.Count,
            source.AccessObservations.Count);
        if (exceededKind >= 0)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(exceededKind, source.RuntimeId), source.RuntimeId);
            return false;
        }
        return TryEncode(new P12ANpcAdventureIntelOwnerValue(source.RuntimeId,
            source.OppositionObservations, source.NotableItemObservations,
            source.CommonResourceObservations, source.AccessObservations,
            source.AdventureKnowledgeRevision), out opposition, out notableItems,
            out resources, out access, out result);
    }

    internal static bool TryEncode(P12ANpcAdventureIntelOwnerValue value,
        out P12AOwnerSection opposition, out P12AOwnerSection notableItems,
        out P12AOwnerSection resources, out P12AOwnerSection access, out P12AResult result)
    {
        opposition = notableItems = resources = access = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(OppositionKind, value?.RuntimeId), value?.RuntimeId);
        int exceededKind = value == null ? -1 : FirstExceededKind(value.Opposition?.Count ?? 0,
            value.NotableItems?.Count ?? 0, value.Resources?.Count ?? 0, value.Access?.Count ?? 0);
        if (exceededKind >= 0)
        {
            result = new P12AResult(P12AResultCode.LimitExceeded,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(exceededKind, value.RuntimeId), value.RuntimeId);
            return false;
        }
        if (!Validate(value)) return false;
        try
        {
            string[] ids = SectionIds(value.RuntimeId);
            string[] aliases = ids.Select(DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection).ToArray();
            if (aliases.Any(string.IsNullOrWhiteSpace) || aliases.Any(alias => alias != aliases[0])) return false;
            foreach (string id in ids) P12AWire.GetEncodedStringByteCount(id);
            P12AWire.GetEncodedStringByteCount(aliases[0]);

            if (!TryEncodeRows(value.Opposition, value.RuntimeId, value.Revision, ids[0],
                    observation => StableKeyEncoding.Encode(observation.SiteRuntimeId,
                        observation.LocalPlaceRuntimeId, observation.OppositionRuntimeId),
                    observation => EncodeOpposition(value.RuntimeId, value.Revision, observation),
                    out List<P12AOwnerRow> oppositionRows, out result)
                || !TryEncodeRows(value.NotableItems, value.RuntimeId, value.Revision, ids[1],
                    observation => StableKeyEncoding.Encode(observation.SiteRuntimeId,
                        observation.LocalPlaceRuntimeId, observation.NotableItemRuntimeId),
                    observation => EncodeNotableItem(value.RuntimeId, value.Revision, observation),
                    out List<P12AOwnerRow> notableRows, out result)
                || !TryEncodeRows(value.Resources, value.RuntimeId, value.Revision, ids[2],
                    observation => StableKeyEncoding.Encode(observation.SiteRuntimeId,
                        observation.LocalPlaceRuntimeId, observation.ItemDefinitionId),
                    observation => EncodeResource(value.RuntimeId, value.Revision, observation),
                    out List<P12AOwnerRow> resourceRows, out result)
                || !TryEncodeRows(value.Access, value.RuntimeId, value.Revision, ids[3],
                    observation => StableKeyEncoding.Encode(observation.SiteRuntimeId,
                        observation.LocalPlaceRuntimeId),
                    observation => EncodeAccess(value.RuntimeId, value.Revision, observation),
                    out List<P12AOwnerRow> accessRows, out result)) return false;

            opposition = Section(ids[0], aliases[0], value.Revision, oppositionRows);
            notableItems = Section(ids[1], aliases[1], value.Revision, notableRows);
            resources = Section(ids[2], aliases[2], value.Revision, resourceRows);
            access = Section(ids[3], aliases[3], value.Revision, accessRows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            opposition = notableItems = resources = access = null;
            result = new P12AResult(IsLimitException(exception)
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(OppositionKind, value.RuntimeId), value.RuntimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection opposition, P12AOwnerSection notableItems,
        P12AOwnerSection resources, P12AOwnerSection access,
        out P12ANpcAdventureIntelOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            opposition?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(OppositionKind, string.Empty));
        if (!TryReadSection(opposition, OppositionKind, out string runtimeId, out long revision,
                out IReadOnlyList<P12AOwnerRow> oppositionRows, out result)
            || !TryReadSection(notableItems, NotableItemKind, out string notableRuntimeId, out long notableRevision,
                out IReadOnlyList<P12AOwnerRow> notableRows, out result)
            || !TryReadSection(resources, CommonResourceKind, out string resourceRuntimeId, out long resourceRevision,
                out IReadOnlyList<P12AOwnerRow> resourceRows, out result)
            || !TryReadSection(access, AccessKind, out string accessRuntimeId, out long accessRevision,
                out IReadOnlyList<P12AOwnerRow> accessRows, out result)) return false;
        if (runtimeId != notableRuntimeId || runtimeId != resourceRuntimeId || runtimeId != accessRuntimeId
            || revision != notableRevision || revision != resourceRevision || revision != accessRevision)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, opposition.Id, runtimeId);
            return false;
        }
        long totalCount = (long)oppositionRows.Count + notableRows.Count + resourceRows.Count + accessRows.Count;
        if (totalCount > revision)
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, opposition.Id, runtimeId);
            return false;
        }

        if (!TryDecodeRows(oppositionRows, runtimeId, revision, "opposition", OppositionIdentity,
                DecodeOpposition, out List<AdventureOppositionObservation> oppositionValues)
            || !TryDecodeRows(notableRows, runtimeId, revision, "notable-item", NotableIdentity,
                DecodeNotableItem, out List<AdventureNotableItemObservation> notableValues)
            || !TryDecodeRows(resourceRows, runtimeId, revision, "common-resource", ResourceIdentity,
                DecodeResource, out List<AdventureCommonResourceObservation> resourceValues)
            || !TryDecodeRows(accessRows, runtimeId, revision, "access", AccessIdentity,
                DecodeAccess, out List<AdventureAccessObservation> accessValues))
        {
            result = new P12AResult(P12AResultCode.InvalidOwnerValue, opposition.Id, runtimeId);
            return false;
        }
        value = new P12ANpcAdventureIntelOwnerValue(runtimeId,
            oppositionValues, notableValues, resourceValues, accessValues, revision);
        result = null;
        return true;
    }

    private static bool Validate(P12ANpcAdventureIntelOwnerValue value)
        => value != null && IsRuntimeId(value.RuntimeId) && value.Revision >= 0L
            && value.Opposition != null && value.NotableItems != null && value.Resources != null && value.Access != null
            && value.Opposition.Count <= P12AContinuationCodec.MaxRows
            && value.NotableItems.Count <= P12AContinuationCodec.MaxRows
            && value.Resources.Count <= P12AContinuationCodec.MaxRows
            && value.Access.Count <= P12AContinuationCodec.MaxRows
            && (long)value.Opposition.Count + value.NotableItems.Count + value.Resources.Count + value.Access.Count <= value.Revision;

    internal static int FirstExceededKind(int opposition, int notableItems, int resources, int access)
    {
        if (opposition > P12AContinuationCodec.MaxRows) return OppositionKind;
        if (notableItems > P12AContinuationCodec.MaxRows) return NotableItemKind;
        if (resources > P12AContinuationCodec.MaxRows) return CommonResourceKind;
        if (access > P12AContinuationCodec.MaxRows) return AccessKind;
        return -1;
    }

    private static string[] SectionIds(string runtimeId)
        => new[] { OppositionKind, NotableItemKind, CommonResourceKind, AccessKind }
            .Select(kind => DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(kind, runtimeId)).ToArray();

    private static P12AOwnerSection Section(string id, string alias, long revision, List<P12AOwnerRow> rows)
        => new P12AOwnerSection(id, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
            OwnerSectionRole.Required, alias, revision, rows);

    private static bool TryEncodeRows<T>(IReadOnlyList<T> values, string runtimeId, long revision,
        string sectionId, Func<T, string> identity, Func<T, byte[]> encode,
        out List<P12AOwnerRow> rows, out P12AResult result) where T : class
    {
        rows = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, runtimeId);
        if (values == null || values.Count > P12AContinuationCodec.MaxRows)
        {
            if (values?.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, runtimeId);
            return false;
        }
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<P12AOwnerRow> encodedRows = new List<P12AOwnerRow>(values.Count);
        long encodedLength = sizeof(int);
        foreach (T value in values)
        {
            if (value == null) return false;
            string key = identity(value);
            if (string.IsNullOrWhiteSpace(key) || !seen.Add(key)) return false;
            byte[] payload = encode(value);
            P12AWire.GetEncodedStringByteCount(key);
            encodedLength = checked(encodedLength + EncodedRowLength(key, payload.Length));
            if (encodedLength > P12AContinuationCodec.MaxSectionBytes)
            {
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, runtimeId);
                return false;
            }
            encodedRows.Add(new P12AOwnerRow(key, revision, payload));
        }
        rows = encodedRows;
        result = null;
        return true;
    }

    private static bool TryReadSection(P12AOwnerSection section, int kind, out string runtimeId,
        out long revision, out IReadOnlyList<P12AOwnerRow> rows, out P12AResult result)
    {
        string prefix = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(kind, string.Empty);
        runtimeId = null; revision = 0L; rows = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id ?? prefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id) || !section.Id.StartsWith(prefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows == null || section.Rows.Count > P12AContinuationCodec.MaxRows
            || section.Rows.Count > section.Revision)
        {
            if (section?.Rows?.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, section.Id);
            return false;
        }
        string candidate = section.Id.Substring(prefix.Length);
        if (!IsRuntimeId(candidate) || section.Id != prefix + candidate
            || section.AliasGroup != DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(section.AliasGroup);
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            long encodedLength = sizeof(int);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != section.Revision || string.IsNullOrWhiteSpace(row.OwnerKey)
                    || !keys.Add(row.OwnerKey) || row.Payload == null || row.Payload.Length != row.PayloadLength
                    || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
                P12AWire.GetEncodedStringByteCount(row.OwnerKey);
                encodedLength = checked(encodedLength + EncodedRowLength(row.OwnerKey, row.PayloadLength));
                if (encodedLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, section.Id, candidate);
                    return false;
                }
            }
            runtimeId = candidate; revision = section.Revision; rows = section.Rows;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(IsLimitException(exception)
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, section.Id, candidate);
            return false;
        }
    }

    private static bool TryDecodeRows<T>(IReadOnlyList<P12AOwnerRow> rows, string runtimeId,
        long revision, string kind, Func<T, string> identity, Func<byte[], T> decode,
        out List<T> values) where T : class
    {
        values = new List<T>(rows.Count);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12AOwnerRow row in rows)
        {
            T value;
            try { value = DecodePayload(row.Payload, runtimeId, revision, kind, decode); }
            catch (Exception exception) when (IsWireException(exception) || exception is ArgumentOutOfRangeException)
            { values = null; return false; }
            if (value == null || identity(value) != row.OwnerKey || !seen.Add(row.OwnerKey))
            { values = null; return false; }
            values.Add(value);
        }
        return true;
    }

    private static T DecodePayload<T>(byte[] payload, string runtimeId, long revision,
        string kind, Func<byte[], T> decode) where T : class
    {
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                || !P12AWire.ReadString(reader, out string owner) || owner != runtimeId
                || !P12AWire.ReadString(reader, out string rowKind) || rowKind != kind
                || reader.ReadInt64() != revision) return null;
            long remaining = stream.Length - stream.Position;
            if (remaining < 0L || remaining > int.MaxValue) return null;
            byte[] data = reader.ReadBytes((int)remaining);
            if (stream.Position != stream.Length || data.Length != remaining) return null;
            return decode(data);
        }
    }

    private static byte[] EncodeOpposition(string runtimeId, long revision, AdventureOppositionObservation value)
        => Encode(runtimeId, revision, "opposition", writer =>
        {
            WriteBase(writer, value);
            P12AWire.WriteString(writer, value.OppositionRuntimeId);
            writer.Write((int)value.ObservedState);
        });

    private static byte[] EncodeNotableItem(string runtimeId, long revision, AdventureNotableItemObservation value)
        => Encode(runtimeId, revision, "notable-item", writer =>
        {
            WriteBase(writer, value);
            P12AWire.WriteString(writer, value.NotableItemRuntimeId);
            P12AWire.WriteString(writer, value.ItemDefinitionId);
        });

    private static byte[] EncodeResource(string runtimeId, long revision, AdventureCommonResourceObservation value)
        => Encode(runtimeId, revision, "common-resource", writer =>
        {
            WriteBase(writer, value);
            P12AWire.WriteString(writer, value.ItemDefinitionId);
            writer.Write(value.ObservedAmount);
            P12AWire.WriteString(writer, value.ResourceCategory);
        });

    private static byte[] EncodeAccess(string runtimeId, long revision, AdventureAccessObservation value)
        => Encode(runtimeId, revision, "access", writer =>
        {
            WriteBase(writer, value);
            writer.Write((int)value.ObservedAccessState);
        });

    private static byte[] Encode(string runtimeId, long revision, string kind, Action<BinaryWriter> write)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, kind);
            writer.Write(revision);
            write(writer);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return stream.ToArray();
        }
    }

    private static void WriteBase(BinaryWriter writer, AdventureSiteIntelObservation value)
    {
        P12AWire.WriteString(writer, value.SiteRuntimeId);
        P12AWire.WriteString(writer, value.LocalPlaceRuntimeId);
        writer.Write(value.ObservedDay); writer.Write(value.ReceivedDay);
        writer.Write((int)value.Source);
        P12AWire.WriteString(writer, value.SourceRuntimeId);
    }

    private static AdventureOppositionObservation DecodeOpposition(byte[] data)
        => DecodeObservation(data, (reader, site, place, observed, received, source, sourceNpc) =>
        {
            if (!P12AWire.ReadString(reader, out string oppositionId)) return null;
            AdventureOppositionObservedState state = (AdventureOppositionObservedState)reader.ReadInt32();
            if (!Enum.IsDefined(typeof(AdventureOppositionObservedState), state)) return null;
            return new AdventureOppositionObservation(site, place, oppositionId, state,
                observed, received, source, sourceNpc);
        });

    private static AdventureNotableItemObservation DecodeNotableItem(byte[] data)
        => DecodeObservation(data, (reader, site, place, observed, received, source, sourceNpc) =>
        {
            if (!P12AWire.ReadString(reader, out string notableId)
                || !P12AWire.ReadString(reader, out string itemId)) return null;
            return new AdventureNotableItemObservation(site, place, notableId, itemId,
                observed, received, source, sourceNpc);
        });

    private static AdventureCommonResourceObservation DecodeResource(byte[] data)
        => DecodeObservation(data, (reader, site, place, observed, received, source, sourceNpc) =>
        {
            if (!P12AWire.ReadString(reader, out string itemId)) return null;
            int amount = reader.ReadInt32();
            if (!P12AWire.ReadString(reader, out string category)) return null;
            if (!IsNormalizedNullable(category)) return null;
            return new AdventureCommonResourceObservation(site, place, itemId, amount, category,
                observed, received, source, sourceNpc);
        });

    private static AdventureAccessObservation DecodeAccess(byte[] data)
        => DecodeObservation(data, (reader, site, place, observed, received, source, sourceNpc) =>
        {
            PlaceAccessState state = (PlaceAccessState)reader.ReadInt32();
            if (!Enum.IsDefined(typeof(PlaceAccessState), state)) return null;
            return new AdventureAccessObservation(site, place, state, observed, received, source, sourceNpc);
        });

    private static T DecodeObservation<T>(byte[] data,
        Func<BinaryReader, string, string, long, long, AdventureIntelSource, string, T> finish) where T : class
    {
        using (MemoryStream stream = new MemoryStream(data, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string site)
                || !P12AWire.ReadString(reader, out string place)) return null;
            long observed = reader.ReadInt64();
            long received = reader.ReadInt64();
            AdventureIntelSource source = (AdventureIntelSource)reader.ReadInt32();
            if (!P12AWire.ReadString(reader, out string sourceRuntimeId)
                || !IsNormalizedNullable(place) || !IsNormalizedNullable(sourceRuntimeId)
                || !Enum.IsDefined(typeof(AdventureIntelSource), source)) return null;
            T value = finish(reader, site, place, observed, received, source, sourceRuntimeId);
            return value != null && stream.Position == stream.Length ? value : null;
        }
    }

    private static string OppositionIdentity(AdventureOppositionObservation value)
        => StableKeyEncoding.Encode(value.SiteRuntimeId, value.LocalPlaceRuntimeId, value.OppositionRuntimeId);
    private static string NotableIdentity(AdventureNotableItemObservation value)
        => StableKeyEncoding.Encode(value.SiteRuntimeId, value.LocalPlaceRuntimeId, value.NotableItemRuntimeId);
    private static string ResourceIdentity(AdventureCommonResourceObservation value)
        => StableKeyEncoding.Encode(value.SiteRuntimeId, value.LocalPlaceRuntimeId, value.ItemDefinitionId);
    private static string AccessIdentity(AdventureAccessObservation value)
        => StableKeyEncoding.Encode(value.SiteRuntimeId, value.LocalPlaceRuntimeId);

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value) && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;
    private static bool IsNormalizedNullable(string value) => value == null || !string.IsNullOrWhiteSpace(value);
    private static long EncodedRowLength(string key, int payloadLength)
        => sizeof(int) + (long)P12AWire.GetEncodedStringByteCount(key) + sizeof(long) + sizeof(int) + payloadLength;
    private static bool IsLimitException(Exception exception)
        => exception is P12ALimitException || exception is OverflowException;
    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException || exception is DecoderFallbackException;
}
