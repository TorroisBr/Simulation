using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcTravelStateOwnerCodecTests
{
    [Test]
    public void TravelStateCodecPreservesCapturedFieldsAndLocalRevision()
    {
        P12ANpcTravelStateOwnerValue source = new P12ANpcTravelStateOwnerValue(
            "npc-7", 2, 5, "route-a", true, null, "party-a", 19L);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id, Is.EqualTo(NpcTravelStateCensusProvider.SectionIdFor(source.RuntimeId)));
        Assert.That(section.Major, Is.EqualTo(NpcTravelStateCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Rows[0].OwnerKey, Is.EqualTo(source.RuntimeId));
        Assert.That(section.Rows[0].Revision, Is.EqualTo(19L));
        Assert.That(section.Revision, Is.EqualTo(19L));

        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(section,
            out P12ANpcTravelStateOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.RuntimeId, Is.EqualTo(source.RuntimeId));
        Assert.That(decoded.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(decoded.TravelDaysTotal, Is.EqualTo(5));
        Assert.That(decoded.TravelRouteRuntimeId, Is.EqualTo("route-a"));
        Assert.That(decoded.TravelStartedToday, Is.True);
        Assert.That(decoded.TravelOriginDecisionId, Is.Null);
        Assert.That(decoded.ActiveTravelPartyId, Is.EqualTo("party-a"));
        Assert.That(decoded.Revision, Is.EqualTo(19L));
        Assert.That(decoded.CurrentCityRuntimeId, Is.Null);
        Assert.That(decoded.CurrentLocationRuntimeId, Is.Null);
        Assert.That(decoded.DestinationCityRuntimeId, Is.Null);
        Assert.That(decoded.DestinationLocationRuntimeId, Is.Null);
        Assert.That(decoded.ResidenceRevision, Is.Zero);

        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows[0].Payload, Is.EqualTo(section.Rows[0].Payload));
    }

    [Test]
    public void NullAndEmptyOptionalReferencesRemainDistinct()
    {
        P12ANpcTravelStateOwnerValue source = new P12ANpcTravelStateOwnerValue(
            "npc-empty", 0, 0, null, false, string.Empty, null, 0L);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(source, out P12AOwnerSection section, out _), Is.True);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(section,
            out P12ANpcTravelStateOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.TravelRouteRuntimeId, Is.Null);
        Assert.That(decoded.TravelOriginDecisionId, Is.Empty);
        Assert.That(decoded.ActiveTravelPartyId, Is.Null);
        Assert.That(decoded.Revision, Is.Zero);
    }

    [Test]
    public void FRowAdapterUsesOnlyCapturedTravelValues()
    {
        P12DNpcFRow source = CreateFRow("npc-captured", 1, 3, "route-captured",
            false, "decision-captured", "party-captured", 7L);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(section,
            out P12ANpcTravelStateOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.TravelRouteRuntimeId, Is.EqualTo(source.TravelRouteRuntimeId));
        Assert.That(decoded.TravelDaysRemaining, Is.EqualTo(source.TravelDaysRemaining));
        Assert.That(decoded.TravelDaysTotal, Is.EqualTo(source.TravelDaysTotal));
        Assert.That(decoded.TravelStartedToday, Is.EqualTo(source.TravelStartedToday));
        Assert.That(decoded.TravelOriginDecisionId, Is.EqualTo(source.TravelOriginDecisionId));
        Assert.That(decoded.ActiveTravelPartyId, Is.EqualTo(source.ActiveTravelPartyId));
        Assert.That(decoded.Revision, Is.EqualTo(source.TravelStateRevision));
    }

    [Test]
    public void DAndFAdapterPreservesTheCompleteNpcPositionBinding()
    {
        P12DNpcDRow d = new P12DNpcDRow("npc-moving", "villager", "person-1", "city-home",
            NpcLifeState.Alive, NpcInjurySeverity.None, Array.Empty<string>(), 0, null,
            null, null, "city-target", "location-target", Array.Empty<P12DNpcInventoryRowValue>(),
            0L, 0f, 0L, 0L, 7L, 0L, 0L);
        P12DNpcFRow f = CreateFRow("npc-moving", 2, 4, null, false, null, null, 9L);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(d, f,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(section,
            out P12ANpcTravelStateOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.CurrentCityRuntimeId, Is.Null);
        Assert.That(decoded.CurrentLocationRuntimeId, Is.Null);
        Assert.That(decoded.DestinationCityRuntimeId, Is.EqualTo("city-target"));
        Assert.That(decoded.DestinationLocationRuntimeId, Is.EqualTo("location-target"));
        Assert.That(decoded.ResidenceRevision, Is.EqualTo(7L));
    }

    [Test]
    public void RejectsInvalidValuesOversizedStringsAndMalformedSections()
    {
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode((P12ANpcTravelStateOwnerValue)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(
            new P12ANpcTravelStateOwnerValue("npc/invalid", 0, 0, null, false, null, null, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(
            new P12ANpcTravelStateOwnerValue("npc-a", -1, 0, null, false, null, null, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(
            new P12ANpcTravelStateOwnerValue("npc-a", 4, 3, null, false, null, null, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(
            new P12ANpcTravelStateOwnerValue("npc-a", 0, 0, null, false, null, null, -1L), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(
            new P12ANpcTravelStateOwnerValue("npc-a", 0, 0, null, false, null, null, 0L,
                "city", null), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(
            new P12ANpcTravelStateOwnerValue("npc-a", 0, 0, null, false, null, null, 0L,
                null, null, null, null, -1L), out _, out _), Is.False);

        string oversized = new string('r', P12AContinuationCodec.MaxStringBytes + 1);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(
            new P12ANpcTravelStateOwnerValue("npc-a", 0, 0, oversized, false, null, null, 0L),
            out _, out P12AResult limitError), Is.False);
        Assert.That(limitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));

        P12AOwnerSection section = Encode(new P12ANpcTravelStateOwnerValue(
            "npc-a", 1, 2, null, false, null, null, 4L));
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section, major: 2), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section, aliasGroup: "wrong"), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section, revision: 5L), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section,
            rows: Array.Empty<P12AOwnerRow>()), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows[0], section.Rows[0] }), out _, out _), Is.False);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section,
            sectionId: NpcTravelStateCensusProvider.SectionIdFor("npc-other")), out _, out _), Is.False);

        byte[] badBoolean = Payload("npc-a", 1, 2, null, 2, null, null, 4L);
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, badBoolean) }), out _, out _), Is.False);
        byte[] trailing = section.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcTravelStateOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, trailing) }), out _, out _), Is.False);
    }

    private static P12DNpcFRow CreateFRow(string runtimeId, int remaining, int total,
        string routeId, bool startedToday, string originDecisionId, string activePartyId, long revision)
        => new P12DNpcFRow(runtimeId, null, remaining, total, routeId, startedToday,
            originDecisionId, activePartyId, revision, null, null,
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);

    private static P12AOwnerSection Encode(P12ANpcTravelStateOwnerValue value)
    {
        Assert.That(P12ANpcTravelStateOwnerCodec.TryEncode(value,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        return section;
    }

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, long? revision = null,
        string sectionId = null)
        => new P12AOwnerSection(sectionId ?? source.Id, major ?? source.Major, source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);

    private static byte[] Payload(string runtimeId, int remaining, int total, string route,
        byte started, string originDecision, string partyId, long revision)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-travel-state/v2");
            P12AWire.WriteString(writer, runtimeId);
            writer.Write(remaining);
            writer.Write(total);
            P12AWire.WriteString(writer, route);
            writer.Write(started);
            P12AWire.WriteString(writer, originDecision);
            P12AWire.WriteString(writer, partyId);
            writer.Write(revision);
            P12AWire.WriteString(writer, null);
            P12AWire.WriteString(writer, null);
            P12AWire.WriteString(writer, null);
            P12AWire.WriteString(writer, null);
            writer.Write(0L);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
