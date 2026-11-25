using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcTravelPlanOwnerCodecTests
{
    [Test]
    public void CodecPreservesCapturedFieldsAndLocalRevision()
    {
        P12DNpcTravelPlanValue plan = new P12DNpcTravelPlanValue(
            "location-target", "city-target", NpcTravelReason.TradeReposition,
            2.375f, 0.125f, "decision-travel", 24L);
        P12DNpcFRow source = CreateFRow("npc-8", plan);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id, Is.EqualTo(NpcPlanCensusProvider.SectionIdFor(
            NpcPlanCensusProvider.TravelPlanKind, source.RuntimeId)));
        Assert.That(section.Major, Is.EqualTo(NpcPlanCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup, Is.Null);
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Rows[0].OwnerKey, Is.EqualTo(source.RuntimeId));
        Assert.That(section.Rows[0].Revision, Is.EqualTo(plan.Revision));
        Assert.That(section.Revision, Is.EqualTo(plan.Revision));

        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(section,
            out P12DNpcTravelPlanValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.TargetLocationRuntimeId, Is.EqualTo(plan.TargetLocationRuntimeId));
        Assert.That(decoded.TargetCityRuntimeId, Is.EqualTo(plan.TargetCityRuntimeId));
        Assert.That(decoded.Reason, Is.EqualTo(plan.Reason));
        Assert.That(FloatBits(decoded.Utility), Is.EqualTo(FloatBits(plan.Utility)));
        Assert.That(FloatBits(decoded.ExpectedCost), Is.EqualTo(FloatBits(plan.ExpectedCost)));
        Assert.That(decoded.OriginDecisionId, Is.EqualTo(plan.OriginDecisionId));
        Assert.That(decoded.Revision, Is.EqualTo(plan.Revision));
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryEncode("npc-8",
            decoded.TargetLocationRuntimeId, decoded.TargetCityRuntimeId, decoded.Reason,
            decoded.Utility, decoded.ExpectedCost, decoded.OriginDecisionId, decoded.Revision,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows[0].Payload, Is.EqualTo(section.Rows[0].Payload));
    }

    [Test]
    public void EmptyRequiredPlanPreservesNullAndPresentEmptyDecision()
    {
        P12DNpcTravelPlanValue plan = new P12DNpcTravelPlanValue(
            null, null, NpcTravelReason.None, 0f, 0f, string.Empty, 0L);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryEncode(CreateFRow("npc-empty", plan),
            out P12AOwnerSection section, out _), Is.True);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(section,
            out P12DNpcTravelPlanValue decoded, out _), Is.True);
        Assert.That(decoded.TargetLocationRuntimeId, Is.Null);
        Assert.That(decoded.TargetCityRuntimeId, Is.Null);
        Assert.That(decoded.OriginDecisionId, Is.Empty);
        Assert.That(decoded.Reason, Is.EqualTo(NpcTravelReason.None));
    }

    [Test]
    public void RejectsInvalidValuesAndWireBounds()
    {
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryEncode((P12DNpcFRow)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryEncode(CreateFRow("npc-no-plan", null),
            out _, out _), Is.False);
        Assert.That(EncodeRaw("npc/a", null, null, NpcTravelReason.None,
            0f, 0f, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", null, "city-a", NpcTravelReason.None,
            0f, 0f, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", null, null, NpcTravelReason.Trade,
            0f, 0f, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "", null, NpcTravelReason.Trade,
            0f, 0f, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "location-a", "", NpcTravelReason.Trade,
            0f, 0f, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "location-a", null, (NpcTravelReason)999,
            0f, 0f, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "location-a", null, NpcTravelReason.Trade,
            -1f, 0f, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "location-a", null, NpcTravelReason.Trade,
            0f, float.NaN, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "location-a", null, NpcTravelReason.Trade,
            0f, float.PositiveInfinity, null, 0L, out _, out _), Is.False);
        Assert.That(EncodeRaw("npc-a", "location-a", null, NpcTravelReason.Trade,
            0f, 0f, null, -1L, out _, out _), Is.False);

        string tooLong = new string('t', P12AContinuationCodec.MaxStringBytes + 1);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryEncode("npc-a", tooLong, null,
            NpcTravelReason.Trade, 0f, 0f, null, 0L,
            out _, out P12AResult limitError), Is.False);
        Assert.That(limitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void RejectsMalformedSectionAndUnknownEnumEncoding()
    {
        P12AOwnerSection section = Encode("npc-a", "location-a", "city-a",
            NpcTravelReason.Trade, 1f, 2f, null, 4L);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(null, out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section, major: 2), out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section, minor: 1), out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section,
            aliasGroup: "unexpected"), out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section, revision: 5L), out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section,
            sectionId: NpcPlanCensusProvider.SectionIdFor(NpcPlanCensusProvider.TravelPlanKind, "npc-other")),
            out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section,
            rows: Array.Empty<P12AOwnerRow>()), out _, out _), Is.False);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows[0], section.Rows[0] }), out _, out _), Is.False);
        byte[] badEnum = Payload("npc-a", "location-a", "city-a", ushort.MaxValue,
            1f, 2f, null, 4L);
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, badEnum) }), out _, out _), Is.False);
        byte[] trailing = section.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcTravelPlanOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, trailing) }), out _, out _), Is.False);
    }

    private static bool EncodeRaw(string runtimeId, string locationId, string cityId,
        NpcTravelReason reason, float utility, float cost, string decisionId, long revision,
        out P12AOwnerSection section, out P12AResult error)
        => P12ANpcTravelPlanOwnerCodec.TryEncode(runtimeId, locationId, cityId, reason,
            utility, cost, decisionId, revision, out section, out error);

    private static P12DNpcFRow CreateFRow(string runtimeId, P12DNpcTravelPlanValue plan)
        => new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, null, 0L,
            null, plan, Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);

    private static P12AOwnerSection Encode(string runtimeId, string locationId, string cityId,
        NpcTravelReason reason, float utility, float cost, string decisionId, long revision)
    {
        Assert.That(EncodeRaw(runtimeId, locationId, cityId, reason, utility, cost,
            decisionId, revision, out P12AOwnerSection section, out P12AResult error),
            Is.True, error?.Code.ToString());
        return section;
    }

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, int? minor = null,
        long? revision = null, string sectionId = null)
        => new P12AOwnerSection(sectionId ?? source.Id, major ?? source.Major, minor ?? source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);

    private static byte[] Payload(string runtimeId, string locationId, string cityId,
        ushort reason, float utility, float cost, string decisionId, long revision)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-travel-plan/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, locationId);
            P12AWire.WriteString(writer, cityId);
            writer.Write(reason);
            writer.Write(utility);
            writer.Write(cost);
            P12AWire.WriteString(writer, decisionId);
            writer.Write(revision);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static int FloatBits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
}
