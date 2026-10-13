using System;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 codec for one NPC travel-plan census section.</summary>
internal static class P12ANpcTravelPlanOwnerCodec
{
    private const string PayloadContract = "p12a.npc-travel-plan/v1";

    internal static bool TryEncode(P12DNpcFRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null || source.TravelPlan == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                NpcPlanCensusProvider.TravelPlanSectionPrefix, source?.RuntimeId);
            return false;
        }
        P12DNpcTravelPlanValue plan = source.TravelPlan;
        return TryEncode(source.RuntimeId, plan.TargetLocationRuntimeId,
            plan.TargetCityRuntimeId, plan.Reason, plan.Utility, plan.ExpectedCost,
            plan.OriginDecisionId, plan.Revision, out section, out result);
    }

    internal static bool TryEncode(string runtimeId, string targetLocationRuntimeId,
        string targetCityRuntimeId, NpcTravelReason reason, float utility,
        float expectedCost, string originDecisionId, long revision,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            NpcPlanCensusProvider.TravelPlanSectionPrefix, runtimeId);
        if (!TryValidate(runtimeId, targetLocationRuntimeId, targetCityRuntimeId,
            reason, utility, expectedCost, revision, out string sectionId)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(runtimeId);
            long payloadLength = EncodedStringFieldLength(PayloadContract)
                + EncodedStringFieldLength(runtimeId)
                + NullableStringFieldLength(targetLocationRuntimeId)
                + NullableStringFieldLength(targetCityRuntimeId)
                + sizeof(ushort) + sizeof(float) + sizeof(float)
                + NullableStringFieldLength(originDecisionId) + sizeof(long);
            if (payloadLength > P12AContinuationCodec.MaxSectionBytes)
            {
                result = new P12AResult(P12AResultCode.LimitExceeded, sectionId, runtimeId);
                return false;
            }
            using (MemoryStream stream = new MemoryStream((int)payloadLength))
            using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
            {
                P12AWire.WriteString(writer, PayloadContract);
                P12AWire.WriteString(writer, runtimeId);
                P12AWire.WriteString(writer, targetLocationRuntimeId);
                P12AWire.WriteString(writer, targetCityRuntimeId);
                writer.Write((ushort)reason);
                writer.Write(utility);
                writer.Write(expectedCost);
                P12AWire.WriteString(writer, originDecisionId);
                writer.Write(revision);
                writer.Flush();
                if (stream.Length != payloadLength)
                {
                    result = new P12AResult(P12AResultCode.InvalidOwnerValue, sectionId, runtimeId);
                    return false;
                }
                section = new P12AOwnerSection(sectionId, NpcPlanCensusProvider.SchemaVersion, 0,
                    OwnerSectionRole.Required, null, revision,
                    new[] { new P12AOwnerRow(runtimeId, revision, stream.ToArray()) });
            }
            result = null;
            return true;
        }
        catch (Exception exception) when (IsWireException(exception))
        {
            section = null;
            result = new P12AResult(exception is P12ALimitException || exception is OverflowException
                ? P12AResultCode.LimitExceeded : P12AResultCode.InvalidOwnerValue,
                sectionId, runtimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12DNpcTravelPlanValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? NpcPlanCensusProvider.TravelPlanSectionPrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(NpcPlanCensusProvider.TravelPlanSectionPrefix, StringComparison.Ordinal)
            || section.Major != NpcPlanCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0L || section.Rows.Count != 1) return false;
        string runtimeId = section.Id.Substring(NpcPlanCensusProvider.TravelPlanSectionPrefix.Length);
        if (!IsRuntimeId(runtimeId)
            || section.Id != NpcPlanCensusProvider.SectionIdFor(
                NpcPlanCensusProvider.TravelPlanKind, runtimeId)) return false;
        P12AOwnerRow row = section.Rows[0];
        if (row == null || row.OwnerKey != runtimeId || row.Revision != section.Revision
            || row.PayloadLength < 0 || row.PayloadLength > P12AContinuationCodec.MaxSectionBytes) return false;
        try
        {
            P12AWire.GetEncodedStringByteCount(section.Id);
            P12AWire.GetEncodedStringByteCount(row.OwnerKey);
            byte[] payload = row.Payload;
            if (payload == null || payload.Length != row.PayloadLength) return false;
            using (MemoryStream stream = new MemoryStream(payload, false))
            using (BinaryReader reader = new BinaryReader(stream, P12AWire.Utf8, true))
            {
                if (!P12AWire.ReadString(reader, out string contract) || contract != PayloadContract
                    || !ReadRequiredString(reader, out string payloadRuntimeId) || payloadRuntimeId != runtimeId
                    || !P12AWire.ReadString(reader, out string targetLocationId)
                    || !P12AWire.ReadString(reader, out string targetCityId)) return false;
                ushort encodedReason = reader.ReadUInt16();
                if (!Enum.IsDefined(typeof(NpcTravelReason), (int)encodedReason)) return false;
                float utility = reader.ReadSingle();
                float expectedCost = reader.ReadSingle();
                if (!P12AWire.ReadString(reader, out string originDecisionId)) return false;
                long revision = reader.ReadInt64();
                NpcTravelReason reason = (NpcTravelReason)encodedReason;
                if (stream.Position != stream.Length || revision != row.Revision
                    || revision != section.Revision
                    || !TryValidate(runtimeId, targetLocationId, targetCityId,
                        reason, utility, expectedCost, revision, out _)) return false;
                value = new P12DNpcTravelPlanValue(targetLocationId, targetCityId,
                    reason, utility, expectedCost, originDecisionId, revision);
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

    private static bool TryValidate(string runtimeId, string targetLocationId,
        string targetCityId, NpcTravelReason reason, float utility, float expectedCost,
        long revision, out string sectionId)
    {
        sectionId = null;
        if (!IsRuntimeId(runtimeId) || revision < 0L
            || !Enum.IsDefined(typeof(NpcTravelReason), reason)
            || float.IsNaN(utility) || float.IsInfinity(utility) || utility < 0f
            || float.IsNaN(expectedCost) || float.IsInfinity(expectedCost) || expectedCost < 0f
            || (targetLocationId == null && (targetCityId != null || reason != NpcTravelReason.None))
            || (targetLocationId != null && string.IsNullOrWhiteSpace(targetLocationId))
            || (targetCityId != null && string.IsNullOrWhiteSpace(targetCityId))) return false;
        sectionId = NpcPlanCensusProvider.SectionIdFor(
            NpcPlanCensusProvider.TravelPlanKind, runtimeId);
        return true;
    }

    private static bool IsRuntimeId(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.IndexOf('/') < 0 && value.IndexOf('\0') < 0;

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
