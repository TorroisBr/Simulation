using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcCommercialKnowledgeOwnerCodecTests
{
    [Test]
    public void RoundTripsCapturedMarketLiquidityAndReceiptValues()
    {
        P12DNpcCommercialShareReceiptValue receipt = CreateReceipt("edge-1", "npc-sender", "npc-owner", 1);
        P12DNpcFRow source = CreateRow("npc-owner", 3L,
            new[] { Market("loc/a", "item/a", 12.25f, 7, 2, 4,
                CommercialKnowledgeSource.SharedByNpc, "npc-sender") },
            new[] { Liquidity("loc-b", MarketLiquidityMode.AccountBacked, 77.125f, 3, 5,
                CommercialKnowledgeSource.DirectObservation, null) }, new[] { receipt });

        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(source,
            out P12AOwnerSection markets, out P12AOwnerSection liquidity,
            out P12AOwnerSection receipts, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(new[] { markets, liquidity, receipts }.All(section =>
            section.Role == OwnerSectionRole.Required && section.Revision == 3L), Is.True);
        Assert.That(markets.AliasGroup, Is.EqualTo(liquidity.AliasGroup));
        Assert.That(liquidity.AliasGroup, Is.EqualTo(receipts.AliasGroup));
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(markets, liquidity, receipts,
            out P12ANpcCommercialKnowledgeOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.Markets[0].LocationRuntimeId, Is.EqualTo("loc/a"));
        Assert.That(decoded.Markets[0].ItemDefinitionId, Is.EqualTo("item/a"));
        Assert.That(BitConverter.SingleToInt32Bits(decoded.Markets[0].ObservedPrice),
            Is.EqualTo(BitConverter.SingleToInt32Bits(12.25f)));
        Assert.That(decoded.Markets[0].ObservedStock, Is.EqualTo(7));
        Assert.That(decoded.Markets[0].Source, Is.EqualTo(CommercialKnowledgeSource.SharedByNpc));
        Assert.That(decoded.Markets[0].SourceRuntimeId, Is.EqualTo("npc-sender"));
        Assert.That(decoded.Liquidity[0].Mode, Is.EqualTo(MarketLiquidityMode.AccountBacked));
        Assert.That(BitConverter.SingleToInt32Bits(decoded.Liquidity[0].PurchasingPower),
            Is.EqualTo(BitConverter.SingleToInt32Bits(77.125f)));
        Assert.That(decoded.Receipts[0].OperationIdentity, Is.EqualTo(receipt.OperationIdentity));
        Assert.That(decoded.Receipts[0].Fingerprint, Is.EqualTo(receipt.Fingerprint));
        Assert.That(decoded.Receipts[0].BoundaryOccurrenceId, Is.EqualTo(receipt.BoundaryOccurrenceId));
        Assert.That(decoded.Receipts[0].SenderPersonId, Is.EqualTo(receipt.SenderPersonId));
        Assert.That(decoded.Receipts[0].ReceiverPersonId, Is.EqualTo(receipt.ReceiverPersonId));
        Assert.That(decoded.Receipts[0].SourceSnapshotFingerprint, Is.EqualTo(receipt.SourceSnapshotFingerprint));
        Assert.That(decoded.Receipts[0].AppliedObservationCount, Is.EqualTo(1));
    }

    [Test]
    public void EmitsThreeRequiredSectionsForExactEmptyOwner()
    {
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(CreateRow("npc-empty", 0L),
            out P12AOwnerSection markets, out P12AOwnerSection liquidity,
            out P12AOwnerSection receipts, out _), Is.True);
        Assert.That(new[] { markets, liquidity, receipts }.All(section =>
            section.Role == OwnerSectionRole.Required && section.Revision == 0L && section.Rows.Count == 0), Is.True);
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(markets, liquidity, receipts,
            out P12ANpcCommercialKnowledgeOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.Markets, Is.Empty);
        Assert.That(decoded.Liquidity, Is.Empty);
        Assert.That(decoded.Receipts, Is.Empty);
    }

    [Test]
    public void MultipleMarketRowsCanResultFromOneSuccessfulShareRevision()
    {
        P12DNpcCommercialShareReceiptValue receipt = CreateReceipt("edge-many", "npc-sender", "npc-owner", 2);
        P12DNpcFRow source = CreateRow("npc-owner", 1L,
            new[] { Market("loc-a", "item-a", 1f, 1, 0, 0, CommercialKnowledgeSource.DirectObservation, null),
                Market("loc-b", "item-b", 2f, 2, 0, 0, CommercialKnowledgeSource.DirectObservation, null) },
            null, new[] { receipt });
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(source,
            out P12AOwnerSection markets, out P12AOwnerSection liquidity,
            out P12AOwnerSection receipts, out P12AResult error), Is.True, error?.Code.ToString());
        Assert.That(markets.Rows.Count, Is.EqualTo(2));
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(markets, liquidity, receipts,
            out P12ANpcCommercialKnowledgeOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.Markets.Count, Is.EqualTo(2));
    }

    [Test]
    public void RejectsDuplicateOwnerKeysUnreachableReceiptCountAndMismatchedSiblingSections()
    {
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(CreateRow("npc-a", 2L,
            new[] { Market("loc-a", "item-a", 1f, 1, 0, 0, CommercialKnowledgeSource.DirectObservation, null),
                Market("loc-a", "item-a", 2f, 2, 0, 0, CommercialKnowledgeSource.DirectObservation, null) }),
            out _, out _, out _, out _), Is.False);
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(CreateRow("npc-a", 0L,
            null, null, new[] { CreateReceipt("edge-valid", "sender", "receiver", 0) }),
            out _, out _, out _, out _), Is.False);
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(CreateRow("npc-a", 1L),
            out P12AOwnerSection markets, out P12AOwnerSection liquidity,
            out P12AOwnerSection receipts, out _), Is.True);
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(markets,
            Copy(liquidity, revision: 2L), receipts, out _, out P12AResult error), Is.False);
        Assert.That(error.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    [Test]
    public void DecoderRejectsTrailingReceiptPayload()
    {
        P12DNpcCommercialShareReceiptValue receipt = CreateReceipt("edge-1", "npc-sender", "npc-owner", 1);
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(CreateRow("npc-owner", 1L,
            null, null, new[] { receipt }), out P12AOwnerSection markets,
            out P12AOwnerSection liquidity, out P12AOwnerSection receipts, out _), Is.True);
        byte[] payload = receipts.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray();
        P12AOwnerSection trailing = Copy(receipts, rows: new[]
        {
            new P12AOwnerRow(receipts.Rows[0].OwnerKey, receipts.Revision, payload)
        });
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(markets, liquidity, trailing,
            out _, out _), Is.False);
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void RejectsMarketPricesThatRestoreWouldNormalize(float price)
    {
        P12DNpcFRow source = CreateRow("npc-a", 1L,
            new[] { Market("loc-a", "item-a", price, 1, 0, 0,
                CommercialKnowledgeSource.DirectObservation, null) });
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(source,
            out _, out _, out _, out _), Is.False);

        string id = NpcKnowledgeCensusProvider.SectionIdFor(7, "npc-a");
        string key = SpatialStableKey.Encode("loc-a", "item-a");
        string alias = P12ADailyV1Census.ExpectedAliasGroupForSection(id);
        P12AOwnerSection invalidMarket = new P12AOwnerSection(id, 1, 0,
            OwnerSectionRole.Required, alias, 1L,
            new[] { new P12AOwnerRow(key, 1L, EncodeMarketPayload("npc-a", price, 1)) });
        P12AOwnerSection liquidity = EmptySection(8, "npc-a", 1L);
        P12AOwnerSection receipts = EmptySection(9, "npc-a", 1L);
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(invalidMarket,
            liquidity, receipts, out _, out _), Is.False);
    }

    [Test]
    public void RejectsNegativeMarketStockOnEncodeAndDecode()
    {
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryEncode(CreateRow("npc-a", 1L,
            new[] { Market("loc-a", "item-a", 1f, -1, 0, 0,
                CommercialKnowledgeSource.DirectObservation, null) }), out _, out _, out _, out _), Is.False);
        string id = NpcKnowledgeCensusProvider.SectionIdFor(7, "npc-a");
        string alias = P12ADailyV1Census.ExpectedAliasGroupForSection(id);
        P12AOwnerSection invalidMarket = new P12AOwnerSection(id, 1, 0,
            OwnerSectionRole.Required, alias, 1L,
            new[] { new P12AOwnerRow(SpatialStableKey.Encode("loc-a", "item-a"), 1L,
                EncodeMarketPayload("npc-a", 1f, -1)) });
        Assert.That(P12ANpcCommercialKnowledgeOwnerCodec.TryDecode(invalidMarket,
            EmptySection(8, "npc-a", 1L), EmptySection(9, "npc-a", 1L), out _, out _), Is.False);
    }

    private static P12DNpcFRow CreateRow(string runtimeId, long revision,
        P12DNpcCommercialMarketValue[] markets = null,
        P12DNpcCommercialLiquidityValue[] liquidity = null,
        P12DNpcCommercialShareReceiptValue[] receipts = null)
        => new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, null, 0L,
            null, null, Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            markets ?? Array.Empty<P12DNpcCommercialMarketValue>(),
            liquidity ?? Array.Empty<P12DNpcCommercialLiquidityValue>(),
            receipts ?? Array.Empty<P12DNpcCommercialShareReceiptValue>(), revision);

    private static P12DNpcCommercialMarketValue Market(string location, string item, float price,
        int stock, long observed, long received, CommercialKnowledgeSource source, string sourceNpc)
        => new P12DNpcCommercialMarketValue(location, item, price, stock, observed, received, source, sourceNpc);

    private static P12DNpcCommercialLiquidityValue Liquidity(string location, MarketLiquidityMode mode,
        float power, long observed, long received, CommercialKnowledgeSource source, string sourceNpc)
        => new P12DNpcCommercialLiquidityValue(location, mode, power, observed, received, source, sourceNpc);

    private static P12DNpcCommercialShareReceiptValue CreateReceipt(string boundary, string sender,
        string receiver, int applied)
    {
        string operation = StableKeyEncoding.Encode("commercial-sharing-edge/v1", boundary, sender, receiver);
        Assert.That(P12DNpcCommercialShareReceiptValue.TryCreateFromOwnerSnapshot(operation, "batch-fingerprint",
            boundary, sender, "sender-person", receiver, "receiver-person", "source-fingerprint",
            applied, out P12DNpcCommercialShareReceiptValue receipt), Is.True);
        return receipt;
    }

    private static P12AOwnerSection Copy(P12AOwnerSection source, long? revision = null,
        IEnumerable<P12AOwnerRow> rows = null)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role, source.AliasGroup,
            revision ?? source.Revision, rows ?? source.Rows);

    private static P12AOwnerSection EmptySection(int kind, string runtimeId, long revision)
    {
        string id = NpcKnowledgeCensusProvider.SectionIdFor(kind, runtimeId);
        return new P12AOwnerSection(id, 1, 0, OwnerSectionRole.Required,
            P12ADailyV1Census.ExpectedAliasGroupForSection(id), revision, Array.Empty<P12AOwnerRow>());
    }

    private static byte[] EncodeMarketPayload(string runtimeId, float price, int stock)
    {
        using (MemoryStream stream = new MemoryStream())
        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-commercial-knowledge/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, "market");
            writer.Write(1L);
            P12AWire.WriteString(writer, "loc-a");
            P12AWire.WriteString(writer, "item-a");
            writer.Write(price); writer.Write(stock);
            writer.Write(0L); writer.Write(0L);
            writer.Write((int)CommercialKnowledgeSource.DirectObservation);
            P12AWire.WriteString(writer, null);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
