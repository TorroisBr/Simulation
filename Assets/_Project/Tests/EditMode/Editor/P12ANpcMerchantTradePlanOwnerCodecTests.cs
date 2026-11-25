using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcMerchantTradePlanOwnerCodecTests
{
    [Test]
    public void CodecPreservesCapturedFieldsAndLocalRevision()
    {
        P12DNpcMerchantPlanValue plan = new P12DNpcMerchantPlanValue(
            "item-iron", "city-origin", "city-target", 8, 3, 2.375f, 4, 7,
            "decision-merchant", 19L);
        P12DNpcFRow source = CreateFRow("npc-7", plan, null);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id, Is.EqualTo(NpcPlanCensusProvider.SectionIdFor(
            NpcPlanCensusProvider.MerchantTradePlanKind, source.RuntimeId)));
        Assert.That(section.Major, Is.EqualTo(NpcPlanCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup, Is.Null);
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Rows[0].OwnerKey, Is.EqualTo(source.RuntimeId));
        Assert.That(section.Rows[0].Revision, Is.EqualTo(plan.Revision));
        Assert.That(section.Revision, Is.EqualTo(plan.Revision));

        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(section,
            out P12DNpcMerchantPlanValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.ItemDefinitionId, Is.EqualTo(plan.ItemDefinitionId));
        Assert.That(decoded.OriginCityRuntimeId, Is.EqualTo(plan.OriginCityRuntimeId));
        Assert.That(decoded.TargetCityRuntimeId, Is.EqualTo(plan.TargetCityRuntimeId));
        Assert.That(decoded.PlannedAmount, Is.EqualTo(plan.PlannedAmount));
        Assert.That(decoded.RawRemainingAmount, Is.EqualTo(plan.RawRemainingAmount));
        Assert.That(FloatBits(decoded.PurchasePricePerItem), Is.EqualTo(FloatBits(plan.PurchasePricePerItem)));
        Assert.That(decoded.WaitDaysAtDestination, Is.EqualTo(plan.WaitDaysAtDestination));
        Assert.That(decoded.PendingTravelDays, Is.EqualTo(plan.PendingTravelDays));
        Assert.That(decoded.OriginDecisionId, Is.EqualTo(plan.OriginDecisionId));
        Assert.That(decoded.Revision, Is.EqualTo(plan.Revision));
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryEncode("npc-7",
            decoded.ItemDefinitionId, decoded.OriginCityRuntimeId, decoded.TargetCityRuntimeId,
            decoded.PlannedAmount, decoded.RawRemainingAmount, decoded.PurchasePricePerItem,
            decoded.WaitDaysAtDestination, decoded.PendingTravelDays, decoded.OriginDecisionId,
            decoded.Revision, out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows[0].Payload, Is.EqualTo(section.Rows[0].Payload));
    }

    [Test]
    public void EmptyRequiredPlanRetainsNullAndEmptyOptionalValues()
    {
        P12DNpcMerchantPlanValue plan = new P12DNpcMerchantPlanValue(
            null, null, null, 0, 0, 0f, 0, 0, string.Empty, 0L);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryEncode(CreateFRow("npc-empty", plan, null),
            out P12AOwnerSection section, out _), Is.True);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(section,
            out P12DNpcMerchantPlanValue decoded, out _), Is.True);
        Assert.That(decoded.ItemDefinitionId, Is.Null);
        Assert.That(decoded.OriginCityRuntimeId, Is.Null);
        Assert.That(decoded.TargetCityRuntimeId, Is.Null);
        Assert.That(decoded.OriginDecisionId, Is.Empty);
    }

    [Test]
    public void RejectsInvalidValuesAndWireBounds()
    {
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryEncode((P12DNpcFRow)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryEncode(CreateFRow("npc-no-plan", null, null),
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", null, null, null, 1, 0, 0f, 0, 0, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "item-a", null, null, 1, 0, 0f, 0, 0, null, 0L,
            out P12AOwnerSection itemOnly, out _), Is.True);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(itemOnly,
            out P12DNpcMerchantPlanValue itemOnlyDecoded, out _), Is.True);
        Assert.That(itemOnlyDecoded.ItemDefinitionId, Is.EqualTo("item-a"));
        Assert.That(EncodeRaw("npc-a", null, null, null, 0, 1, 0f, 0, 0, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "item-a", null, null, -1, 0, 0f, 0, 0, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "item-a", null, null, 1, 0, float.NaN, 0, 0, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "item-a", null, null, 1, 0, float.PositiveInfinity, 0, 0, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "item-a", null, null, 1, 0, -1f, 0, 0, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "item-a", null, null, 1, 0, 0f, -1, 0, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "item-a", null, null, 1, 0, 0f, 0, -1, null, 0L,
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc/a", null, null, null, 0, 0, 0f, 0, 0, null, 0L,
            out _, out _), Is.False);

        string tooLong = new string('m', P12AContinuationCodec.MaxStringBytes + 1);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryEncode("npc-a", tooLong,
            null, null, 1, 0, 0f, 0, 0, null, 0L,
            out _, out P12AResult limitError), Is.False);
        Assert.That(limitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void RejectsMalformedSectionIdentityRevisionAndPayload()
    {
        P12AOwnerSection section = Encode("npc-a", "item-a", "city-a", "city-b",
            4, 2, 1.5f, 1, 3, "decision-a", 8L);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(null, out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section, major: 2), out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section, minor: 1), out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section,
            aliasGroup: "unexpected"), out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section, revision: 9L), out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section,
            sectionId: NpcPlanCensusProvider.SectionIdFor(NpcPlanCensusProvider.MerchantTradePlanKind, "npc-other")),
            out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section,
            rows: Array.Empty<P12AOwnerRow>()), out _, out _), Is.False);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows[0], section.Rows[0] }), out _, out _), Is.False);
        byte[] trailing = section.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 8L, trailing) }), out _, out _), Is.False);
        byte[] mismatchedRevision = Payload("npc-a", "item-a", "city-a", "city-b",
            4, 2, 1.5f, 1, 3, "decision-a", 9L);
        Assert.That(P12ANpcMerchantTradePlanOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 8L, mismatchedRevision) }), out _, out _), Is.False);
    }

    private static bool EncodeRaw(string runtimeId, string itemId, string originCityId,
        string targetCityId, int planned, int remaining, float price, int wait, int pending,
        string decisionId, long revision, out P12AOwnerSection section, out P12AResult error)
        => P12ANpcMerchantTradePlanOwnerCodec.TryEncode(runtimeId, itemId, originCityId,
            targetCityId, planned, remaining, price, wait, pending, decisionId,
            revision, out section, out error);

    private static P12DNpcFRow CreateFRow(string runtimeId,
        P12DNpcMerchantPlanValue merchantPlan, P12DNpcTravelPlanValue travelPlan)
        => new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, null, 0L,
            merchantPlan, travelPlan, Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);

    private static P12AOwnerSection Encode(string runtimeId, string itemId, string originCityId,
        string targetCityId, int planned, int remaining, float price, int wait, int pending,
        string decisionId, long revision)
    {
        Assert.That(EncodeRaw(runtimeId, itemId, originCityId, targetCityId, planned,
            remaining, price, wait, pending, decisionId, revision,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        return section;
    }

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, int? minor = null,
        long? revision = null, string sectionId = null)
        => new P12AOwnerSection(sectionId ?? source.Id, major ?? source.Major, minor ?? source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);

    private static byte[] Payload(string runtimeId, string itemId, string originCityId,
        string targetCityId, int planned, int remaining, float price, int wait, int pending,
        string decisionId, long revision)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-merchant-trade-plan/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, itemId);
            P12AWire.WriteString(writer, originCityId);
            P12AWire.WriteString(writer, targetCityId);
            writer.Write(planned);
            writer.Write(remaining);
            writer.Write(price);
            writer.Write(wait);
            writer.Write(pending);
            P12AWire.WriteString(writer, decisionId);
            writer.Write(revision);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static int FloatBits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
}
