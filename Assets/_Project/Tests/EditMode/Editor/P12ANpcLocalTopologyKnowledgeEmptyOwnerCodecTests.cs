using System;
using NUnit.Framework;

public sealed class P12ANpcLocalTopologyKnowledgeEmptyOwnerCodecTests
{
    [Test]
    public void EncodesOnlyCapturedExactEmptyOwnerPair()
    {
        object owner = new object();
        OwnerSectionCensusSnapshot placeWitness = Snapshot("p12f.local-topology-knowledge.places/npc-a", owner, 0, 0L);
        OwnerSectionCensusSnapshot connectionWitness = Snapshot("p12f.local-topology-knowledge.connections/npc-a", owner, 0, 0L);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a",
            placeWitness, connectionWitness, out P12AOwnerSection places,
            out P12AOwnerSection connections, out P12AResult error),
            Is.True, error?.Code.ToString());
        Assert.That(places.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(connections.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(places.Major, Is.EqualTo(NpcKnowledgeCensusProvider.SchemaVersion));
        Assert.That(connections.Major, Is.EqualTo(NpcKnowledgeCensusProvider.SchemaVersion));
        Assert.That(places.Minor, Is.Zero);
        Assert.That(connections.Minor, Is.Zero);
        Assert.That(places.Revision, Is.Zero);
        Assert.That(connections.Revision, Is.Zero);
        Assert.That(places.Rows, Is.Empty);
        Assert.That(connections.Rows, Is.Empty);
        Assert.That(places.AliasGroup, Is.EqualTo(connections.AliasGroup));
        Assert.That(places.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(places.Id)));
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryDecode(places, connections,
            out string runtimeId, out _), Is.True);
        Assert.That(runtimeId, Is.EqualTo("npc-a"));
    }

    [Test]
    public void RejectsPopulatedOrMismatchedCaptureWitnesses()
    {
        object owner = new object();
        OwnerSectionCensusSnapshot place = Snapshot("p12f.local-topology-knowledge.places/npc-a", owner, 0, 0L);
        OwnerSectionCensusSnapshot connection = Snapshot("p12f.local-topology-knowledge.connections/npc-a", owner, 0, 0L);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a", null, connection,
            out _, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc/a", place, connection,
            out _, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a",
            Snapshot("p12f.local-topology-knowledge.places/npc-other", owner, 0, 0L), connection,
            out _, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a", place,
            Snapshot("p12f.local-topology-knowledge.connections/npc-a", new object(), 0, 0L),
            out _, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a",
            Snapshot("p12f.local-topology-knowledge.places/npc-a", owner, 1, 0L), connection,
            out _, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a", place,
            Snapshot("p12f.local-topology-knowledge.connections/npc-a", owner, 0, 1L),
            out _, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a",
            new OwnerSectionCensusSnapshot("p12f.local-topology-knowledge.places/npc-a", 2,
                OwnerSectionRole.Required, owner, 0, 0L), connection,
            out _, out _, out _), Is.False);
    }

    [Test]
    public void DecoderRejectsPopulatedAndIncorrectlyTypedSections()
    {
        object owner = new object();
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode("npc-a",
            Snapshot("p12f.local-topology-knowledge.places/npc-a", owner, 0, 0L),
            Snapshot("p12f.local-topology-knowledge.connections/npc-a", owner, 0, 0L),
            out P12AOwnerSection places, out P12AOwnerSection connections, out _), Is.True);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryDecode(
            Copy(places, revision: 1L), connections, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryDecode(
            Copy(places, role: OwnerSectionRole.ExplicitlyEmpty), connections, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryDecode(
            Copy(places, aliasGroup: "wrong"), connections, out _, out _), Is.False);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryDecode(
            Copy(places, rows: new[] { new P12AOwnerRow("place-a", 0L, Array.Empty<byte>()) }),
            connections, out _, out _), Is.False);
    }

    [Test]
    public void InvalidUtf16IsNotReportedAsAFormatLimit()
    {
        string runtimeId = "npc-\ud800";
        object owner = new object();
        string placeId = NpcKnowledgeCensusProvider.SectionIdFor(1, runtimeId);
        string connectionId = NpcKnowledgeCensusProvider.SectionIdFor(2, runtimeId);
        Assert.That(P12ANpcLocalTopologyKnowledgeEmptyOwnerCodec.TryEncode(runtimeId,
            Snapshot(placeId, owner, 0, 0L), Snapshot(connectionId, owner, 0, 0L),
            out _, out _, out P12AResult result), Is.False);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    private static OwnerSectionCensusSnapshot Snapshot(string id, object owner,
        int cardinality, long revision)
        => new OwnerSectionCensusSnapshot(id, NpcKnowledgeCensusProvider.SchemaVersion,
            OwnerSectionRole.Required, owner, cardinality, revision);

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        System.Collections.Generic.IEnumerable<P12AOwnerRow> rows = null,
        string aliasGroup = null, OwnerSectionRole? role = null, long? revision = null)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);
}
