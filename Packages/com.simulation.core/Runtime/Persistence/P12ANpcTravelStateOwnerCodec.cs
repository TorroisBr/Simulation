using System;
using System.IO;
using System.Text;

/// <summary>Detached values owned by one NPC travel-state section.</summary>
internal sealed class P12ANpcTravelStateOwnerValue
{
    internal P12ANpcTravelStateOwnerValue(string runtimeId,
        int travelDaysRemaining, int travelDaysTotal, string travelRouteRuntimeId,
        bool travelStartedToday, string travelOriginDecisionId, string activeTravelPartyId,
        long revision, string currentCityRuntimeId = null, string currentLocationRuntimeId = null,
        string destinationCityRuntimeId = null, string destinationLocationRuntimeId = null,
        long residenceRevision = 0L)
    {
        RuntimeId = runtimeId;
        TravelDaysRemaining = travelDaysRemaining;
        TravelDaysTotal = travelDaysTotal;
        TravelRouteRuntimeId = travelRouteRuntimeId;
        TravelStartedToday = travelStartedToday;
        TravelOriginDecisionId = travelOriginDecisionId;
        ActiveTravelPartyId = activeTravelPartyId;
        Revision = revision;
        CurrentCityRuntimeId = currentCityRuntimeId;
        CurrentLocationRuntimeId = currentLocationRuntimeId;
        DestinationCityRuntimeId = destinationCityRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
        ResidenceRevision = residenceRevision;
    }

    internal string RuntimeId { get; }
    internal int TravelDaysRemaining { get; }
    internal int TravelDaysTotal { get; }
    internal string TravelRouteRuntimeId { get; }
    internal bool TravelStartedToday { get; }
    internal string TravelOriginDecisionId { get; }
    internal string ActiveTravelPartyId { get; }
    internal long Revision { get; }
    internal string CurrentCityRuntimeId { get; }
    internal string CurrentLocationRuntimeId { get; }
    internal string DestinationCityRuntimeId { get; }
    internal string DestinationLocationRuntimeId { get; }
    internal long ResidenceRevision { get; }
}

/// <summary>Explicit Daily-v1 codec for the per-NPC travel-state owner section.</summary>
internal static class P12ANpcTravelStateOwnerCodec
{
    private const string PayloadContract = "p12a.npc-travel-state/v2";

    internal static bool TryEncode(P12DNpcFRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix);
            return false;
        }
        return TryEncode(new P12ANpcTravelStateOwnerValue(source.RuntimeId,
            source.TravelDaysRemaining, source.TravelDaysTotal, source.TravelRouteRuntimeId,
            source.TravelStartedToday, source.TravelOriginDecisionId, source.ActiveTravelPartyId,
            source.TravelStateRevision), out section, out result);
    }

    internal static bool TryEncode(P12DNpcDRow dSource, P12DNpcFRow fSource,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (dSource == null || fSource == null
            || !string.Equals(dSource.RuntimeId, fSource.RuntimeId, StringComparison.Ordinal))
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix, dSource?.RuntimeId);
            return false;
        }
        if ((fSource.TravelDaysRemaining > 0
                && (string.IsNullOrWhiteSpace(dSource.DestinationCityRuntimeId)
                    || string.IsNullOrWhiteSpace(dSource.DestinationLocationRuntimeId)))
            || ((dSource.DestinationCityRuntimeId == null)
                != (dSource.DestinationLocationRuntimeId == null)))
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, dSource.RuntimeId), dSource.RuntimeId);
            return false;
        }
        return TryEncode(new P12ANpcTravelStateOwnerValue(dSource.RuntimeId,
            fSource.TravelDaysRemaining, fSource.TravelDaysTotal, fSource.TravelRouteRuntimeId,
            fSource.TravelStartedToday, fSource.TravelOriginDecisionId, fSource.ActiveTravelPartyId,
            fSource.TravelStateRevision, dSource.CurrentCityRuntimeId,
            dSource.CurrentLocationRuntimeId, dSource.DestinationCityRuntimeId,
            dSource.DestinationLocationRuntimeId, dSource.ResidenceRevision), out section, out result);
    }

    internal static bool TryEncode(P12ANpcTravelStateOwnerValue value,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix, value?.RuntimeId);
        if (!TryValidate(value, out string sectionId, out string aliasGroup)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(aliasGroup);
            long payloadLength = EncodedStringFieldLength(PayloadContract)
                + EncodedStringFieldLength(value.RuntimeId)
                + sizeof(int) + sizeof(int) + NullableStringFieldLength(value.TravelRouteRuntimeId)
                + sizeof(byte) + NullableStringFieldLength(value.TravelOriginDecisionId)
                + NullableStringFieldLength(value.ActiveTravelPartyId) + sizeof(long)
                + NullableStringFieldLength(value.CurrentCityRuntimeId)
                + NullableStringFieldLength(value.CurrentLocationRuntimeId)
                + NullableStringFieldLength(value.DestinationCityRuntimeId)
                + NullableStringFieldLength(value.DestinationLocationRuntimeId) + sizeof(long);
            if (payloadLength > P12AContinuationCodec.MaxSectionBytes)
            {
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, value.RuntimeId);
                return false;
            }

            using (MemoryStream stream = new MemoryStream((int)payloadLength))
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, value.RuntimeId);
                writer.Write(value.TravelDaysRemaining);
                writer.Write(value.TravelDaysTotal);
                P12AWire.WriteString(writer, value.TravelRouteRuntimeId);
                writer.Write(value.TravelStartedToday);
                P12AWire.WriteString(writer, value.TravelOriginDecisionId);
                P12AWire.WriteString(writer, value.ActiveTravelPartyId);
                writer.Write(value.Revision);
                P12AWire.WriteString(writer, value.CurrentCityRuntimeId);
                P12AWire.WriteString(writer, value.CurrentLocationRuntimeId);
                P12AWire.WriteString(writer, value.DestinationCityRuntimeId);
                P12AWire.WriteString(writer, value.DestinationLocationRuntimeId);
                writer.Write(value.ResidenceRevision);
                writer.Flush();
                if (stream.Length != payloadLength)
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, value.RuntimeId);
                    return false;
                }
                section = new P12AOwnerSection(sectionId, DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion, 0,
                    OwnerSectionRole.Required, aliasGroup, value.Revision,
                    new[] { new P12AOwnerRow(value.RuntimeId, value.Revision, stream.ToArray()) });
            }
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                sectionId, value.RuntimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12ANpcTravelStateOwnerValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix, StringComparison.Ordinal)
            || section.Major != DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.Revision < 0L
            || section.Rows.Count != 1) return false;

        string runtimeId = section.Id.Substring(DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix.Length);
        if (!IsRuntimeId(runtimeId)
            || section.Id != DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, runtimeId)) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(section.AliasGroup);
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue, section.Id, runtimeId);
            return false;
        }
        if (!string.Equals(section.AliasGroup,
            DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(section.Id), StringComparison.Ordinal)) return false;

        P12AOwnerRow row = section.Rows[0];
        if (row == null || row.OwnerKey != runtimeId || row.Revision != section.Revision
            || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(row.OwnerKey);
            byte[] payload = row.Payload;
            if (payload == null || payload.Length != row.PayloadLength) return false;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                    || !ReadRequiredString(reader, out string payloadRuntimeId)
                    || payloadRuntimeId != runtimeId) return false;
                int remaining = reader.ReadInt32();
                int total = reader.ReadInt32();
                if (!P12AWire.ReadString(reader, out string routeId)
                    || !TryReadBoolean(reader, out bool startedToday)
                    || !P12AWire.ReadString(reader, out string originDecisionId)
                    || !P12AWire.ReadString(reader, out string activeTravelPartyId)) return false;
                long revision = reader.ReadInt64();
                if (!P12AWire.ReadString(reader, out string currentCityId)
                    || !P12AWire.ReadString(reader, out string currentLocationId)
                    || !P12AWire.ReadString(reader, out string destinationCityId)
                    || !P12AWire.ReadString(reader, out string destinationLocationId)) return false;
                long residenceRevision = reader.ReadInt64();
                P12ANpcTravelStateOwnerValue candidate = new P12ANpcTravelStateOwnerValue(
                    runtimeId, remaining, total, routeId, startedToday, originDecisionId,
                    activeTravelPartyId, revision, currentCityId, currentLocationId,
                    destinationCityId, destinationLocationId, residenceRevision);
                if (stream.Position != stream.Length || revision != row.Revision
                    || revision != section.Revision || !TryValidate(candidate, out _, out _)) return false;
                value = candidate;
                result = null;
                return true;
            }
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                section.Id, row.OwnerKey);
            return false;
        }
    }

    private static bool TryValidate(P12ANpcTravelStateOwnerValue value,
        out string sectionId, out string aliasGroup)
    {
        sectionId = null;
        aliasGroup = null;
        if (value == null || !IsRuntimeId(value.RuntimeId)
            || value.TravelDaysRemaining < 0 || value.TravelDaysTotal < 0
            || (value.TravelDaysTotal > 0 && value.TravelDaysRemaining > value.TravelDaysTotal)
            || value.Revision < 0L || value.ResidenceRevision < 0L
            || !IsOptionalRuntimeId(value.CurrentCityRuntimeId)
            || !IsOptionalRuntimeId(value.CurrentLocationRuntimeId)
            || !IsOptionalRuntimeId(value.DestinationCityRuntimeId)
            || !IsOptionalRuntimeId(value.DestinationLocationRuntimeId)
            || (value.CurrentCityRuntimeId == null) != (value.CurrentLocationRuntimeId == null)
            || (value.DestinationCityRuntimeId == null) != (value.DestinationLocationRuntimeId == null)) return false;
        sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, value.RuntimeId);
        aliasGroup = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId);
        return !string.IsNullOrWhiteSpace(aliasGroup);
    }

    private static bool TryReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte encoded = reader.ReadByte();
        if (encoded > 1) return false;
        value = encoded == 1;
        return true;
    }

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

    private static bool IsOptionalRuntimeId(string value)
        => value == null || IsRuntimeId(value);

    private static bool ReadRequiredString(BinaryReader reader, out string value)
        => P12AWire.ReadString(reader, out value) && !string.IsNullOrWhiteSpace(value);

    private static long EncodedStringFieldLength(string value)
        => checked(sizeof(int) + (long)P12AWire.GetEncodedStringByteCount(value));

    private static long NullableStringFieldLength(string value)
        => value == null ? sizeof(int) : EncodedStringFieldLength(value);

    private static bool IsWireException(Exception exception)
        => exception is IOException || exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is P12ALimitException
            || exception is DecoderFallbackException;
}
