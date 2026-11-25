using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 codec for one NPC ExplorableSiteKnowledge owner.</summary>
internal static class P12ANpcExplorableSiteKnowledgeOwnerCodec
{
    private const string SectionPrefix = "p12f.explorable-site-knowledge/";
    private const string PayloadContract = "p12a.npc-explorable-site-knowledge/v1";
    private const int SectionKind = 0;

    internal static bool TryEncode(P12DNpcFRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, SectionPrefix, source?.RuntimeId);
        if (source == null || source.SiteObservations == null
            || source.SiteObservations.Count > P12AContinuationCodec.MaxRows
            || source.SiteObservations.Count > source.SiteKnowledgeRevision
            || !IsRuntimeId(source.RuntimeId) || source.SiteKnowledgeRevision < 0L)
        {
            if (source?.SiteObservations?.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, SectionPrefix, source.RuntimeId);
            return false;
        }

        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(SectionKind, source.RuntimeId);
        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(source.SiteObservations.Count);
            HashSet<string> seenSiteIds = new HashSet<string>(StringComparer.Ordinal);
            long encodedRowsLength = sizeof(int);
            foreach (ExplorableSiteKnowledgeObservation observation in source.SiteObservations)
            {
                if (observation == null || !TryValidate(observation)
                    || !seenSiteIds.Add(observation.SiteRuntimeId)) return false;
                string ownerKey = observation.SiteRuntimeId;
                P12AWire.GetEncodedStringByteCount(ownerKey);
                byte[] payload = EncodeObservation(source.RuntimeId, observation);
                rows.Add(new P12AOwnerRow(ownerKey, source.SiteKnowledgeRevision, payload));
                encodedRowsLength = checked(encodedRowsLength + EncodedRowLength(ownerKey, payload.Length));
                if (encodedRowsLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, source.RuntimeId);
                    return false;
                }
            }
            string aliasGroup = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId);
            section = new P12AOwnerSection(sectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
                OwnerSectionRole.Required, aliasGroup, source.SiteKnowledgeRevision, rows);
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                sectionId, source.RuntimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section, out string runtimeId,
        out IReadOnlyList<ExplorableSiteKnowledgeObservation> observations,
        out long revision, out P12AResult result)
    {
        runtimeId = null;
        observations = null;
        revision = 0L;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, section?.Id ?? SectionPrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(SectionPrefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows.Count > P12AContinuationCodec.MaxRows
            || section.Rows.Count > section.Revision)
        {
            if (section?.Rows.Count > P12AContinuationCodec.MaxRows)
                result = new P12AResult(P12AResultCode.LimitExceeded, section.Id);
            return false;
        }
        runtimeId = section.Id.Substring(SectionPrefix.Length);
        if (!IsRuntimeId(runtimeId)
            || section.Id != DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(SectionKind, runtimeId)
            || section.AliasGroup != DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id))
        {
            runtimeId = null;
            return false;
        }
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            if (section.AliasGroup != null) P12AWire.GetEncodedStringByteCount(section.AliasGroup);
            List<ExplorableSiteKnowledgeObservation> values =
                new List<ExplorableSiteKnowledgeObservation>(section.Rows.Count);
            HashSet<string> siteIds = new HashSet<string>(StringComparer.Ordinal);
            long encodedRowsLength = sizeof(int);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (row == null || row.Revision != section.Revision
                    || !IsDomainId(row.OwnerKey) || !siteIds.Add(row.OwnerKey)
                    || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes)
                    return false;
                P12AWire.GetEncodedStringByteCount(row.OwnerKey);
                byte[] payload = row.Payload;
                if (payload == null || payload.Length != row.PayloadLength) return false;
                encodedRowsLength = checked(encodedRowsLength + EncodedRowLength(row.OwnerKey, payload.Length));
                if (encodedRowsLength > P12AContinuationCodec.MaxSectionBytes)
                {
                    result = new P12AResult(P12AResultCode.LimitExceeded, section.Id, runtimeId);
                    return false;
                }
                if (!TryDecodeObservation(payload, runtimeId, row.OwnerKey,
                    out ExplorableSiteKnowledgeObservation observation)) return false;
                values.Add(observation);
            }
            observations = values.AsReadOnly();
            revision = section.Revision;
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            runtimeId = null;
            observations = null;
            revision = 0L;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                section.Id, runtimeId);
            return false;
        }
    }

    private static byte[] EncodeObservation(string runtimeId,
        ExplorableSiteKnowledgeObservation observation)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, PayloadContract);
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, observation.SiteRuntimeId);
            P12AWire.WriteString(writer, observation.LocationRuntimeId);
            writer.Write(observation.ObservedDay);
            writer.Write(observation.ReceivedDay);
            writer.Write((ushort)observation.Source);
            writer.Flush();
            if (stream.Length > P12AContinuationCodec.MaxSectionBytes) throw new P12ALimitException();
            return stream.ToArray();
        }
    }

    private static bool TryDecodeObservation(byte[] payload, string runtimeId, string ownerKey,
        out ExplorableSiteKnowledgeObservation observation)
    {
        observation = null;
        using (MemoryStream stream = new MemoryStream(payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                || !P12AWire.ReadString(reader, out string payloadRuntimeId) || payloadRuntimeId != runtimeId
                || !P12AWire.ReadString(reader, out string siteId) || siteId != ownerKey
                || !P12AWire.ReadString(reader, out string locationId)) return false;
            long observedDay = reader.ReadInt64();
            long receivedDay = reader.ReadInt64();
            ushort encodedSource = reader.ReadUInt16();
            if (stream.Position != stream.Length
                || !Enum.IsDefined(typeof(ExplorableSiteKnowledgeSource), (int)encodedSource)) return false;
            ExplorableSiteKnowledgeObservation candidate = new ExplorableSiteKnowledgeObservation(
                siteId, locationId, observedDay, receivedDay, (ExplorableSiteKnowledgeSource)encodedSource);
            observation = candidate;
            return true;
        }
    }

    private static bool TryValidate(ExplorableSiteKnowledgeObservation value)
        => IsDomainId(value.SiteRuntimeId) && IsDomainId(value.LocationRuntimeId)
            && value.ObservedDay >= 0L && value.ReceivedDay >= value.ObservedDay
            && Enum.IsDefined(typeof(ExplorableSiteKnowledgeSource), value.Source);

    private static long EncodedRowLength(string ownerKey, int payloadLength)
        => sizeof(int) + (long)P12AWire.GetEncodedStringByteCount(ownerKey)
            + sizeof(long) + sizeof(int) + payloadLength;

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value) && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static bool IsDomainId(string value) => !string.IsNullOrWhiteSpace(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;
}
