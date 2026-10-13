using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcCurrentActionOwnerCodecTests
{
    [Test]
    public void RoundTripsOptionalActionAndAllNestedCommercialEvidence()
    {
        P12DNpcActionValue action = CreateAction();
        P12DNpcDRow d = DRow("npc-a", action.ActionDefinitionId, 7L);
        P12DNpcFRow f = FRow("npc-a", action);
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryEncode(d, f,
            out P12AOwnerSection section, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
        Assert.That(section.Revision, Is.EqualTo(7L));
        Assert.That(section.Rows, Has.Count.EqualTo(1));

        Assert.That(P12ANpcCurrentActionOwnerCodec.TryDecode(section, out string runtimeId,
            out string definitionId, out P12DNpcActionValue decoded, out long revision,
            out P12AResult decodeError), Is.True, decodeError?.Code.ToString());
        Assert.That(runtimeId, Is.EqualTo("npc-a"));
        Assert.That(definitionId, Is.EqualTo(action.ActionDefinitionId));
        Assert.That(revision, Is.EqualTo(7L));
        Assert.That(decoded.TargetNpcRuntimeId, Is.EqualTo(action.TargetNpcRuntimeId));
        Assert.That(decoded.TargetCityRuntimeId, Is.EqualTo(action.TargetCityRuntimeId));
        Assert.That(decoded.TargetItemDefinitionId, Is.EqualTo(action.TargetItemDefinitionId));
        Assert.That(decoded.Amount, Is.EqualTo(action.Amount));
        Assert.That(decoded.ExpectedUnitPrice, Is.EqualTo(action.ExpectedUnitPrice));
        Assert.That(decoded.SuccessChanceMultiplier, Is.EqualTo(action.SuccessChanceMultiplier));
        Assert.That(decoded.TravelReason, Is.EqualTo(action.TravelReason));
        Assert.That(decoded.ExpectedNetValue, Is.EqualTo(action.ExpectedNetValue));
        Assert.That(decoded.OriginDecisionId, Is.EqualTo(action.OriginDecisionId));
        Assert.That(decoded.StableOccurrenceKey, Is.EqualTo(action.StableOccurrenceKey));
        Assert.That(decoded.CommercialDecisionEvidence.ExpectedGrossProfit,
            Is.EqualTo(action.CommercialDecisionEvidence.ExpectedGrossProfit));
        Assert.That(decoded.CommercialDecisionEvidence.OriginObservation.SourceRuntimeId, Is.EqualTo("npc-source"));
        Assert.That(decoded.CommercialDecisionEvidence.DestinationLiquidityObservation.ObservedPurchasingPower,
            Is.EqualTo(25f));
        Assert.That(decoded.CommercialScoutingEvidence.KnownRouteRuntimeId, Is.EqualTo("route-a"));
    }

    [Test]
    public void EncodesExactEmptySlotWhilePreservingItsRevision()
    {
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryEncode(DRow("npc-empty", null, 3L),
            FRow("npc-empty", null), out P12AOwnerSection section, out P12AResult error),
            Is.True, error?.Code.ToString());
        Assert.That(section.Rows, Is.Empty);
        Assert.That(section.Revision, Is.EqualTo(3L));
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryDecode(section, out string runtimeId,
            out string definitionId, out P12DNpcActionValue action, out long revision, out _), Is.True);
        Assert.That(runtimeId, Is.EqualTo("npc-empty"));
        Assert.That(definitionId, Is.Null);
        Assert.That(action, Is.Null);
        Assert.That(revision, Is.EqualTo(3L));
    }

    [Test]
    public void RejectsDAndFBindingMismatchAndMalformedSections()
    {
        P12DNpcActionValue action = CreateAction();
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryEncode(DRow("npc-a", "different-action", 1L),
            FRow("npc-a", action), out _, out _), Is.False);
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryEncode(DRow("npc-a", action.ActionDefinitionId, 1L),
            FRow("npc-b", action), out _, out _), Is.False);

        Assert.That(P12ANpcCurrentActionOwnerCodec.TryEncode(DRow("npc-a", action.ActionDefinitionId, 1L),
            FRow("npc-a", action), out P12AOwnerSection section, out _), Is.True);
        P12AOwnerSection wrongAlias = Copy(section, alias: "wrong");
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryDecode(wrongAlias, out _, out _, out _, out _, out _), Is.False);
        P12AOwnerSection trailing = Copy(section, rows: new[] { new P12AOwnerRow("npc-a", 1L,
            section.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray()) });
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryDecode(trailing, out _, out _, out _, out _, out _), Is.False);
        P12AOwnerSection multiple = Copy(section, rows: section.Rows.Concat(section.Rows));
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryDecode(multiple, out _, out _, out _, out _, out _), Is.False);
    }

    [Test]
    public void RejectsNonCanonicalBooleanBytes()
    {
        using (MemoryStream stream = new MemoryStream(new byte[] { 0, 1 }))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            Assert.That(P12ANpcCurrentActionOwnerCodec.TryReadBoolean(reader, out bool zero), Is.True);
            Assert.That(zero, Is.False);
            Assert.That(P12ANpcCurrentActionOwnerCodec.TryReadBoolean(reader, out bool one), Is.True);
            Assert.That(one, Is.True);
        }
        using (MemoryStream stream = new MemoryStream(new byte[] { 2 }))
        using (BinaryReader reader = new BinaryReader(stream))
            Assert.That(P12ANpcCurrentActionOwnerCodec.TryReadBoolean(reader, out _), Is.False);

        P12DNpcActionValue bare = new P12DNpcActionValue("action-bare", null, null, null,
            0, 0f, 1f, NpcTravelReason.None, 0f, null, null, null, null);
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryEncode(DRow("npc-bare", bare.ActionDefinitionId, 1L),
            FRow("npc-bare", bare), out P12AOwnerSection section, out _), Is.True);
        byte[] payload = section.Rows[0].Payload;
        payload[payload.Length - 1] = 2; // optional scouting-presence tag
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-bare", 1L, payload) }),
            out _, out _, out _, out _, out _), Is.False);
        payload = section.Rows[0].Payload;
        payload[payload.Length - 2] = 2; // optional decision-presence tag
        Assert.That(P12ANpcCurrentActionOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-bare", 1L, payload) }),
            out _, out _, out _, out _, out _), Is.False);
    }

    private static P12DNpcActionValue CreateAction()
    {
        CommercialObservationEvidence origin = new CommercialObservationEvidence(
            "origin-location", 2.5f, 4, 1L, 2L, 0.75f,
            CommercialKnowledgeSource.SharedByNpc, "npc-source");
        CommercialObservationEvidence destination = new CommercialObservationEvidence(
            "destination-location", 7.5f, 8, 2L, 2L, 1f,
            CommercialKnowledgeSource.DirectObservation, null);
        CommercialLiquidityEvidence liquidity = CommercialLiquidityEvidence.Capture(
            new CommercialLiquidityObservation("destination-location", MarketLiquidityMode.AccountBacked,
                25f, 2L, 3L, CommercialKnowledgeSource.InitialScenarioKnowledge), 0.6f);
        CommercialDecisionEvidence decision = new CommercialDecisionEvidence(
            "item-definition", "current-location", "origin-location", "destination-location", null,
            origin, destination, 3, 2.5f, 7.5f, 1.25f, 15f, 13.75f, 0.9f, liquidity);
        CommercialScoutingEvidence scouting = new CommercialScoutingEvidence(
            "destination-location", "route-a", 2, 1, true, 1L, 3.5f, 2, 0.8f);
        return new P12DNpcActionValue("action-a", "npc-target", "city-target", "item-definition",
            3, 7.5f, 0.4f, NpcTravelReason.CommercialScout, -2.25f,
            "decision-a", "occurrence-a", decision, scouting);
    }

    private static P12DNpcDRow DRow(string runtimeId, string actionDefinitionId, long revision)
        => new P12DNpcDRow(runtimeId, "npc-definition", null, null,
            NpcLifeState.Alive, NpcInjurySeverity.None, Array.Empty<string>(), 0,
            actionDefinitionId, null, null, null, null, Array.Empty<P12DNpcInventoryRowValue>(),
            0L, 0f, 0L, 0L, 0L, 0L, revision);

    private static P12DNpcFRow FRow(string runtimeId, P12DNpcActionValue action)
        => new P12DNpcFRow(runtimeId, action, 0, 0, null, false, null, null, 0L,
            null, null, Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<CommercialKnowledgeShareReceipt>(), 0L);

    private static P12AOwnerSection Copy(P12AOwnerSection source, string alias = null,
        System.Collections.Generic.IEnumerable<P12AOwnerRow> rows = null)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            alias ?? source.AliasGroup, source.Revision, rows ?? source.Rows);
}
