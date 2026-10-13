using System;
using NUnit.Framework;

public sealed class P12ANpcReceiptEmptyOwnerCodecTests
{
    [Test]
    public void EmitsOnlyExactEmptyRequiredSectionsFromPairedCapturedOwners()
    {
        object localOwner = new object();
        object tradeOwner = new object();
        OwnerSectionCensusSnapshot local = Snapshot(
            P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor("npc-a"), localOwner, 0, 0L);
        OwnerSectionCensusSnapshot trade = Snapshot(
            P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor("npc-a"), tradeOwner, 0, 0L);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryEncode("npc-a", local, trade,
            out P12AOwnerSection localSection, out P12AOwnerSection tradeSection,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(localSection.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(tradeSection.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(localSection.AliasGroup, Is.Null);
        Assert.That(tradeSection.AliasGroup, Is.Null);
        Assert.That(localSection.Rows, Is.Empty);
        Assert.That(tradeSection.Rows, Is.Empty);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryDecode(localSection, tradeSection,
            out string runtimeId, out _), Is.True);
        Assert.That(runtimeId, Is.EqualTo("npc-a"));
    }

    [Test]
    public void RejectsPopulatedAliasedOrMismatchedOwners()
    {
        object localOwner = new object();
        object tradeOwner = new object();
        OwnerSectionCensusSnapshot local = Snapshot(
            P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor("npc-a"), localOwner, 0, 0L);
        OwnerSectionCensusSnapshot trade = Snapshot(
            P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor("npc-a"), tradeOwner, 0, 0L);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryEncode("npc/a", local, trade,
            out _, out _, out _), Is.False);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryEncode("npc-a", local,
            Snapshot(trade.SectionId, tradeOwner, 1, 0L), out _, out _, out _), Is.False);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryEncode("npc-a", local,
            Snapshot(trade.SectionId, localOwner, 0, 0L), out _, out _, out _), Is.False);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryEncode("npc-a", local,
            Snapshot(trade.SectionId, tradeOwner, 0, 1L), out _, out _, out _), Is.False);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryEncode("npc-a", local, trade,
            out P12AOwnerSection localSection, out P12AOwnerSection tradeSection, out _), Is.True);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryDecode(Copy(localSection, alias: "unexpected"),
            tradeSection, out _, out _), Is.False);
        P12AOwnerSection nullId = new P12AOwnerSection(null, localSection.Major, localSection.Minor,
            localSection.Role, localSection.AliasGroup, localSection.Revision, localSection.Rows);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryDecode(nullId,
            tradeSection, out _, out _), Is.False);
        Assert.That(P12ANpcReceiptEmptyOwnerCodec.TryDecode(Copy(localSection,
                rows: new[] { new P12AOwnerRow("receipt", 0L, Array.Empty<byte>()) }),
            tradeSection, out _, out _), Is.False);
    }

    private static OwnerSectionCensusSnapshot Snapshot(string id, object owner, int cardinality, long revision)
        => new OwnerSectionCensusSnapshot(id, P12DNpcReceiptOwnerCensusProvider.SchemaVersion,
            OwnerSectionRole.Required, owner, cardinality, revision);

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        string alias = null, System.Collections.Generic.IEnumerable<P12AOwnerRow> rows = null)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            alias, source.Revision, rows ?? source.Rows);
}
