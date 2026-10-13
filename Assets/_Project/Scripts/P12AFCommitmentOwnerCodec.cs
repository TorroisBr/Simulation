using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>Explicit wire codecs for the detached Daily-v1 TravelParty and Expedition owners.</summary>
internal static class P12AFCommitmentOwnerCodec
{
    private const string TravelPartyPayloadContract = "p12a.travel-party/v1";
    private const string ExpeditionPayloadContract = "p12a.expedition/v1";
    private const string TravelPartyOwnerPrefix = "travel-party/";
    private const string ExpeditionOwnerPrefix = "expedition/";

    internal static bool TryEncodeTravelParties(P12FTravelPartyOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, TravelPartyCensusProvider.SectionId);
        if (snapshot == null || snapshot.SchemaVersion != P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0 || snapshot.Parties == null
            || snapshot.Parties.Count > P12AContinuationCodec.MaxRows
            || snapshot.Revision < snapshot.Parties.Count) return false;
        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Parties.Count);
            HashSet<string> partyIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> allMembers = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12FTravelPartyOwnerSnapshotRecord party in snapshot.Parties)
            {
                if (!P12FTravelPartyOwnerSnapshot.IsValidRecord(party)
                    || !MembersMatchRoles(party)
                    || !partyIds.Add(party.TravelPartyId)) return false;
                foreach (string member in party.MemberRuntimeIds)
                    if (!allMembers.Add(member)) return false;
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, TravelPartyPayloadContract);
                    WriteTravelParty(writer, party);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            TravelPartyCensusProvider.SectionId, TravelPartyOwnerPrefix + party.TravelPartyId);
                        return false;
                    }
                    // This owner exposes a section-wide revision, not a per-party local revision.
                    rows.Add(new P12AOwnerRow(TravelPartyOwnerPrefix + party.TravelPartyId, 0L, stream.ToArray()));
                }
            }
            section = new P12AOwnerSection(TravelPartyCensusProvider.SectionId,
                TravelPartyCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required, null,
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
                TravelPartyCensusProvider.SectionId);
            return false;
        }
    }

    internal static bool TryDecodeTravelParties(P12AOwnerSection section,
        out P12FTravelPartyOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, TravelPartyCensusProvider.SectionId);
        if (!IsSection(section, TravelPartyCensusProvider.SectionId,
                TravelPartyCensusProvider.SchemaVersion)) return false;
        try
        {
            List<P12FTravelPartyOwnerSnapshotRecord> parties = new List<P12FTravelPartyOwnerSnapshotRecord>(section.Rows.Count);
            HashSet<string> partyIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> allMembers = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (!TryReadPayload(row, TravelPartyPayloadContract, out P12FTravelPartyOwnerSnapshotRecord party)
                    || row.OwnerKey != TravelPartyOwnerPrefix + party.TravelPartyId
                    || !P12FTravelPartyOwnerSnapshot.IsValidRecord(party) || !MembersMatchRoles(party)
                    || !partyIds.Add(party.TravelPartyId))
                { result = new P12AResult(P12AResultCode.InvalidOwnerValue, section.Id, row?.OwnerKey); return false; }
                foreach (string member in party.MemberRuntimeIds)
                    if (!allMembers.Add(member))
                    { result = new P12AResult(P12AResultCode.InvalidOwnerValue, section.Id, row.OwnerKey); return false; }
                parties.Add(party);
            }
            if (section.Revision < parties.Count) return false;
            snapshot = new P12FTravelPartyOwnerSnapshot(
                P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, section.Revision, parties);
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException)
        { snapshot = null; return false; }
    }

    internal static bool TryEncodeExpeditions(P12FExpeditionOwnerSnapshot snapshot,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, ExpeditionCensusProvider.SectionId);
        if (snapshot == null || snapshot.SchemaVersion != P12FExpeditionOwnerSnapshot.CurrentSchemaVersion
            || snapshot.Revision < 0 || snapshot.Expeditions == null
            || snapshot.Expeditions.Count > P12AContinuationCodec.MaxRows
            || snapshot.Revision < snapshot.Expeditions.Count) return false;
        try
        {
            List<P12AOwnerRow> rows = new List<P12AOwnerRow>(snapshot.Expeditions.Count);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12FExpeditionOwnerSnapshotRecord expedition in snapshot.Expeditions)
            {
                if (!P12FExpeditionOwnerSnapshot.IsValid(expedition) || !ids.Add(expedition.ExpeditionId)) return false;
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
                {
                    P12AWire.WriteString(writer, ExpeditionPayloadContract);
                    WriteExpedition(writer, expedition);
                    writer.Flush();
                    if (stream.Length > P12AContinuationCodec.MaxSectionBytes)
                    {
                        result = new P12AResult(P12AResultCode.LimitExceeded,
                            ExpeditionCensusProvider.SectionId, ExpeditionOwnerPrefix + expedition.ExpeditionId);
                        return false;
                    }
                    rows.Add(new P12AOwnerRow(ExpeditionOwnerPrefix + expedition.ExpeditionId, 0L, stream.ToArray()));
                }
            }
            section = new P12AOwnerSection(ExpeditionCensusProvider.SectionId,
                ExpeditionCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required, null,
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
                ExpeditionCensusProvider.SectionId);
            return false;
        }
    }

    internal static bool TryDecodeExpeditions(P12AOwnerSection section,
        out P12FExpeditionOwnerSnapshot snapshot, out P12AResult result)
    {
        snapshot = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue, ExpeditionCensusProvider.SectionId);
        if (!IsSection(section, ExpeditionCensusProvider.SectionId, ExpeditionCensusProvider.SchemaVersion)) return false;
        try
        {
            List<P12FExpeditionOwnerSnapshotRecord> expeditions =
                new List<P12FExpeditionOwnerSnapshotRecord>(section.Rows.Count);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (P12AOwnerRow row in section.Rows)
            {
                if (!TryReadPayload(row, ExpeditionPayloadContract, out P12FExpeditionOwnerSnapshotRecord expedition)
                    || row.OwnerKey != ExpeditionOwnerPrefix + expedition.ExpeditionId
                    || !P12FExpeditionOwnerSnapshot.IsValid(expedition) || !ids.Add(expedition.ExpeditionId))
                { result = new P12AResult(P12AResultCode.InvalidOwnerValue, section.Id, row?.OwnerKey); return false; }
                expeditions.Add(expedition);
            }
            if (section.Revision < expeditions.Count) return false;
            snapshot = new P12FExpeditionOwnerSnapshot(
                P12FExpeditionOwnerSnapshot.CurrentSchemaVersion, section.Revision, expeditions);
            result = null;
            return true;
        }
        catch (Exception exception) when (exception is EndOfStreamException || exception is IOException
            || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException)
        { snapshot = null; return false; }
    }

    private static bool IsSection(P12AOwnerSection section, string id, int schemaVersion) => section != null
        && section.Id == id && section.Major == schemaVersion && section.Minor == 0
        && section.Role == OwnerSectionRole.Required && section.AliasGroup == null
        && section.Revision >= 0 && section.Rows.Count <= P12AContinuationCodec.MaxRows
        && section.Rows.All(row => row != null && row.Revision == 0L && row.Payload != null);

    private static bool TryReadPayload<T>(P12AOwnerRow row, string contract, out T value) where T : class
    {
        value = null;
        if (row == null || row.Revision != 0L || row.Payload == null
            || row.Payload.Length > P12AContinuationCodec.MaxSectionBytes) return false;
        using (MemoryStream stream = new MemoryStream(row.Payload, false))
        using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
        {
            if (!P12AWire.ReadString(reader, out string foundContract) || foundContract != contract) return false;
            if (typeof(T) == typeof(P12FTravelPartyOwnerSnapshotRecord))
            {
                if (!ReadTravelParty(reader, out P12FTravelPartyOwnerSnapshotRecord party)
                    || stream.Position != stream.Length) return false;
                value = party as T;
                return value != null;
            }
            if (typeof(T) == typeof(P12FExpeditionOwnerSnapshotRecord))
            {
                if (!ReadExpedition(reader, out P12FExpeditionOwnerSnapshotRecord expedition)
                    || stream.Position != stream.Length) return false;
                value = expedition as T;
                return value != null;
            }
            return false;
        }
    }

    private static bool MembersMatchRoles(P12FTravelPartyOwnerSnapshotRecord party)
    {
        if (party?.TravelerRuntimeIds == null || party.EscortRuntimeIds == null || party.MemberRuntimeIds == null) return false;
        if (party.TravelerRuntimeIds.Count + party.EscortRuntimeIds.Count != party.MemberRuntimeIds.Count) return false;
        int index = 0;
        foreach (string traveler in party.TravelerRuntimeIds)
            if (!string.Equals(party.MemberRuntimeIds[index++], traveler, StringComparison.Ordinal)) return false;
        foreach (string escort in party.EscortRuntimeIds)
            if (!string.Equals(party.MemberRuntimeIds[index++], escort, StringComparison.Ordinal)) return false;
        return true;
    }

    private static void WriteTravelParty(BinaryWriter writer, P12FTravelPartyOwnerSnapshotRecord party)
    {
        P12AWire.WriteString(writer, party.TravelPartyId);
        P12AWire.WriteString(writer, party.OriginLocationRuntimeId);
        P12AWire.WriteString(writer, party.DestinationLocationRuntimeId);
        P12AWire.WriteString(writer, party.RouteRuntimeId);
        WriteStrings(writer, party.TravelerRuntimeIds);
        WriteStrings(writer, party.EscortRuntimeIds);
        WriteStrings(writer, party.MemberRuntimeIds);
        writer.Write(party.MemberCosts.Count);
        foreach (TravelPartyMemberCost cost in party.MemberCosts)
        { P12AWire.WriteString(writer, cost.RuntimeId); writer.Write(cost.Amount); }
        writer.Write(party.TravelDaysTotal);
        P12AWire.WriteString(writer, party.OriginDecisionId);
    }

    private static bool ReadTravelParty(BinaryReader reader, out P12FTravelPartyOwnerSnapshotRecord party)
    {
        party = null;
        if (!P12AWire.ReadString(reader, out string id) || id == null
            || !P12AWire.ReadString(reader, out string origin) || origin == null
            || !P12AWire.ReadString(reader, out string destination) || destination == null
            || !P12AWire.ReadString(reader, out string route) || route == null
            || !ReadStrings(reader, out List<string> travelers)
            || !ReadStrings(reader, out List<string> escorts)
            || !ReadStrings(reader, out List<string> members)) return false;
        int costCount = reader.ReadInt32();
        if (costCount < 0 || costCount > P12AContinuationCodec.MaxRows) return false;
        List<TravelPartyMemberCost> costs = new List<TravelPartyMemberCost>(costCount);
        for (int i = 0; i < costCount; i++)
        {
            if (!P12AWire.ReadString(reader, out string member) || member == null) return false;
            costs.Add(new TravelPartyMemberCost(member, reader.ReadSingle()));
        }
        int days = reader.ReadInt32();
        if (!P12AWire.ReadString(reader, out string decision)) return false;
        party = new P12FTravelPartyOwnerSnapshotRecord(id, origin, destination, route,
            travelers, escorts, members, costs, days, decision);
        return true;
    }

    private static void WriteExpedition(BinaryWriter writer, P12FExpeditionOwnerSnapshotRecord value)
    {
        P12AWire.WriteString(writer, value.ExpeditionId);
        P12AWire.WriteString(writer, value.TargetSiteRuntimeId);
        P12AWire.WriteString(writer, value.OriginLocationRuntimeId);
        P12AWire.WriteString(writer, value.TargetLocationRuntimeId);
        P12AWire.WriteString(writer, value.OutboundRouteRuntimeId);
        P12AWire.WriteString(writer, value.TravelPartyId);
        P12AWire.WriteString(writer, value.OriginDecisionId);
        WriteStrings(writer, value.MemberRuntimeIds);
        WriteStrings(writer, value.PerformerRuntimeIds);
        WriteStrings(writer, value.SupportRuntimeIds);
        writer.Write((ushort)value.State);
        P12AWire.WriteString(writer, value.CurrentLocalPlaceRuntimeId);
        WriteStrings(writer, value.VisitedLocalPlaceRuntimeIds);
        WriteStrings(writer, value.ObservedLocalConnectionRuntimeIds);
        writer.Write((ushort)value.ObjectiveType);
        P12AWire.WriteString(writer, value.TargetItemDefinitionId);
        P12AWire.WriteString(writer, value.TargetNotableItemRuntimeId);
        P12AWire.WriteString(writer, value.TargetOppositionRuntimeId);
        writer.Write(value.RequiredProgress);
        writer.Write(value.Progress);
        writer.Write((byte)(value.ObjectiveCompleted ? 1 : 0));
        writer.Write((byte)(value.AllowContinueAfterCompletion ? 1 : 0));
    }

    private static bool ReadExpedition(BinaryReader reader, out P12FExpeditionOwnerSnapshotRecord value)
    {
        value = null;
        if (!P12AWire.ReadString(reader, out string id) || id == null
            || !P12AWire.ReadString(reader, out string site) || site == null
            || !P12AWire.ReadString(reader, out string origin) || origin == null
            || !P12AWire.ReadString(reader, out string target) || target == null
            || !P12AWire.ReadString(reader, out string route) || route == null
            || !P12AWire.ReadString(reader, out string party)
            || !P12AWire.ReadString(reader, out string decision)
            || !ReadStrings(reader, out List<string> members)
            || !ReadStrings(reader, out List<string> performers)
            || !ReadStrings(reader, out List<string> supports)
            || !ReadEnum(reader, out ExpeditionState state)
            || !P12AWire.ReadString(reader, out string currentPlace)
            || !ReadStrings(reader, out List<string> visited)
            || !ReadStrings(reader, out List<string> connections)
            || !ReadEnum(reader, out ExpeditionObjectiveType objective)
            || !P12AWire.ReadString(reader, out string targetItem)
            || !P12AWire.ReadString(reader, out string notableItem)
            || !P12AWire.ReadString(reader, out string opposition)) return false;
        int required = reader.ReadInt32();
        int progress = reader.ReadInt32();
        if (!ReadBoolean(reader, out bool completed) || !ReadBoolean(reader, out bool continueAfterCompletion)) return false;
        value = new P12FExpeditionOwnerSnapshotRecord(id, site, origin, target, route, party, decision,
            members, performers, supports, state, currentPlace, visited, connections, objective,
            targetItem, notableItem, opposition, required, progress, completed, continueAfterCompletion);
        return true;
    }

    private static void WriteStrings(BinaryWriter writer, IReadOnlyList<string> values)
    {
        if (values == null || values.Count > P12AContinuationCodec.MaxRows) throw new ArgumentException("Invalid collection.");
        writer.Write(values.Count);
        foreach (string value in values) P12AWire.WriteString(writer, value);
    }

    private static bool ReadStrings(BinaryReader reader, out List<string> values)
    {
        values = null;
        int count = reader.ReadInt32();
        if (count < 0 || count > P12AContinuationCodec.MaxRows) return false;
        List<string> candidate = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            if (!P12AWire.ReadString(reader, out string value) || value == null) return false;
            candidate.Add(value);
        }
        values = candidate;
        return true;
    }

    private static bool ReadBoolean(BinaryReader reader, out bool value)
    {
        value = false;
        byte raw = reader.ReadByte();
        if (raw > 1) return false;
        value = raw == 1;
        return true;
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
