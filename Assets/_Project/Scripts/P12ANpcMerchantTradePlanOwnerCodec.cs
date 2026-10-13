using System;
using System.IO;
using System.Text;

/// <summary>Explicit Daily-v1 codec for one NPC merchant-trade-plan census section.</summary>
internal static class P12ANpcMerchantTradePlanOwnerCodec
{
    private const string PayloadContract = "p12a.npc-merchant-trade-plan/v1";

    internal static bool TryEncode(P12DNpcFRow source,
        out P12AOwnerSection section, out P12AResult result)
    {
        if (source == null || source.MerchantPlan == null)
        {
            section = null;
            result = new P12AResult(P12AResultCode.InvalidOwnerValue,
                NpcPlanCensusProvider.MerchantTradePlanSectionPrefix, source?.RuntimeId);
            return false;
        }
        P12DNpcMerchantPlanValue plan = source.MerchantPlan;
        return TryEncode(source.RuntimeId, plan.ItemDefinitionId, plan.OriginCityRuntimeId,
            plan.TargetCityRuntimeId, plan.PlannedAmount, plan.RawRemainingAmount,
            plan.PurchasePricePerItem, plan.WaitDaysAtDestination, plan.PendingTravelDays,
            plan.OriginDecisionId, plan.Revision, out section, out result);
    }

    internal static bool TryEncode(string runtimeId, string itemDefinitionId,
        string originCityRuntimeId, string targetCityRuntimeId, int plannedAmount,
        int rawRemainingAmount, float purchasePricePerItem, int waitDaysAtDestination,
        int pendingTravelDays, string originDecisionId, long revision,
        out P12AOwnerSection section, out P12AResult result)
    {
        section = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            NpcPlanCensusProvider.MerchantTradePlanSectionPrefix, runtimeId);
        if (!TryValidate(runtimeId, itemDefinitionId, originCityRuntimeId, targetCityRuntimeId,
            plannedAmount, rawRemainingAmount, purchasePricePerItem, waitDaysAtDestination,
            pendingTravelDays, revision, out string sectionId)) return false;

        try
        {
            P12AWire.GetEncodedStringByteCount(sectionId);
            P12AWire.GetEncodedStringByteCount(runtimeId);
            long payloadLength = EncodedStringFieldLength(PayloadContract)
                + EncodedStringFieldLength(runtimeId)
                + NullableStringFieldLength(itemDefinitionId)
                + NullableStringFieldLength(originCityRuntimeId)
                + NullableStringFieldLength(targetCityRuntimeId)
                + sizeof(int) + sizeof(int) + sizeof(float) + sizeof(int) + sizeof(int)
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
                P12AWire.WriteString(writer, itemDefinitionId);
                P12AWire.WriteString(writer, originCityRuntimeId);
                P12AWire.WriteString(writer, targetCityRuntimeId);
                writer.Write(plannedAmount);
                writer.Write(rawRemainingAmount);
                writer.Write(purchasePricePerItem);
                writer.Write(waitDaysAtDestination);
                writer.Write(pendingTravelDays);
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
                NpcPlanCensusProvider.MerchantTradePlanSectionPrefix + runtimeId, runtimeId);
            return false;
        }
    }

    internal static bool TryDecode(P12AOwnerSection section,
        out P12DNpcMerchantPlanValue value, out P12AResult result)
    {
        value = null;
        result = new P12AResult(P12AResultCode.InvalidOwnerValue,
            section?.Id ?? NpcPlanCensusProvider.MerchantTradePlanSectionPrefix);
        if (section == null || string.IsNullOrWhiteSpace(section.Id)
            || !section.Id.StartsWith(NpcPlanCensusProvider.MerchantTradePlanSectionPrefix, StringComparison.Ordinal)
            || section.Major != NpcPlanCensusProvider.SchemaVersion || section.Minor != 0
            || section.Role != OwnerSectionRole.Required || section.AliasGroup != null
            || section.Revision < 0L || section.Rows.Count != 1) return false;
        string runtimeId = section.Id.Substring(NpcPlanCensusProvider.MerchantTradePlanSectionPrefix.Length);
        if (!IsRuntimeId(runtimeId)
            || section.Id != NpcPlanCensusProvider.SectionIdFor(NpcPlanCensusProvider.MerchantTradePlanKind, runtimeId))
            return false;
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
                    || !P12AWire.ReadString(reader, out string itemId)
                    || !P12AWire.ReadString(reader, out string originCityId)
                    || !P12AWire.ReadString(reader, out string targetCityId)) return false;
                int planned = reader.ReadInt32();
                int remaining = reader.ReadInt32();
                float price = reader.ReadSingle();
                int waitDays = reader.ReadInt32();
                int pendingTravelDays = reader.ReadInt32();
                if (!P12AWire.ReadString(reader, out string decisionId)) return false;
                long revision = reader.ReadInt64();
                if (stream.Position != stream.Length || revision != row.Revision
                    || revision != section.Revision
                    || !TryValidate(runtimeId, itemId, originCityId, targetCityId,
                        planned, remaining, price, waitDays, pendingTravelDays, revision, out _)) return false;
                value = new P12DNpcMerchantPlanValue(itemId, originCityId, targetCityId,
                    planned, remaining, price, waitDays, pendingTravelDays, decisionId, revision);
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

    private static bool TryValidate(string runtimeId, string itemId, string originCityId,
        string targetCityId, int plannedAmount, int rawRemainingAmount, float price,
        int waitDays, int pendingTravelDays, long revision, out string sectionId)
    {
        sectionId = null;
        if (!IsRuntimeId(runtimeId) || revision < 0L || plannedAmount < 0 || rawRemainingAmount < 0
            || waitDays < 0 || pendingTravelDays < 0 || float.IsNaN(price)
            || float.IsInfinity(price) || price < 0f
            || (itemId == null && (plannedAmount != 0 || rawRemainingAmount != 0
                || originCityId != null || targetCityId != null))
            || (itemId != null && string.IsNullOrWhiteSpace(itemId))
            || (originCityId != null && string.IsNullOrWhiteSpace(originCityId))
            || (targetCityId != null && string.IsNullOrWhiteSpace(targetCityId))) return false;
        sectionId = NpcPlanCensusProvider.SectionIdFor(
            NpcPlanCensusProvider.MerchantTradePlanKind, runtimeId);
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
