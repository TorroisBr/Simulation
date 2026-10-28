using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcExplorableSiteKnowledgeOwnerCodecTests
{
    [Test]
    public void CodecPreservesCapturedObservationOrderValuesAndRevision()
    {
        ExplorableSiteKnowledgeObservation first = new ExplorableSiteKnowledgeObservation(
            "site/one", "location/one", 3L, 5L, ExplorableSiteKnowledgeSource.InitialScenarioKnowledge);
        ExplorableSiteKnowledgeObservation second = new ExplorableSiteKnowledgeObservation(
            "site-two", "location-two", 8L, 9L, ExplorableSiteKnowledgeSource.DirectObservation);
        P12DNpcFRow source = CreateFRow("npc-3", new[] { first, second }, 12L);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id, Is.EqualTo(NpcKnowledgeCensusProvider.SectionIdFor(0, source.RuntimeId)));
        Assert.That(section.Major, Is.EqualTo(NpcKnowledgeCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup, Is.Null);
        Assert.That(section.Revision, Is.EqualTo(12L));
        Assert.That(section.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { "site/one", "site-two" }));
        Assert.That(section.Rows.All(row => row.Revision == 12L), Is.True);

        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(section,
            out string runtimeId, out IReadOnlyList<ExplorableSiteKnowledgeObservation> decoded,
            out long revision, out P12AResult decodeError), Is.True, decodeError?.Code.ToString());
        Assert.That(runtimeId, Is.EqualTo("npc-3"));
        Assert.That(revision, Is.EqualTo(12L));
        Assert.That(decoded.Select(value => value.SiteRuntimeId), Is.EqualTo(new[] { "site/one", "site-two" }));
        Assert.That(decoded[0].LocationRuntimeId, Is.EqualTo("location/one"));
        Assert.That(decoded[0].ObservedDay, Is.EqualTo(3L));
        Assert.That(decoded[0].ReceivedDay, Is.EqualTo(5L));
        Assert.That(decoded[0].Source, Is.EqualTo(ExplorableSiteKnowledgeSource.InitialScenarioKnowledge));
        Assert.That(decoded[1].ObservedDay, Is.EqualTo(8L));
        Assert.That(decoded[1].ReceivedDay, Is.EqualTo(9L));
        Assert.That(decoded[1].Source, Is.EqualTo(ExplorableSiteKnowledgeSource.DirectObservation));
    }

    [Test]
    public void EmptyObservationsRemainRequiredAndStringBoundaryIsExact()
    {
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(
            CreateFRow("npc-empty", Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L),
            out P12AOwnerSection empty, out _), Is.True);
        Assert.That(empty.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(empty.Rows, Is.Empty);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(empty,
            out _, out IReadOnlyList<ExplorableSiteKnowledgeObservation> none, out long revision, out _), Is.True);
        Assert.That(none, Is.Empty);
        Assert.That(revision, Is.Zero);

        string maxSiteId = new string('s', P12AContinuationCodec.MaxStringBytes);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-boundary",
            new[] { new ExplorableSiteKnowledgeObservation(maxSiteId, "loc", 0L, 0L,
                ExplorableSiteKnowledgeSource.DirectObservation) }, 1L),
            out P12AOwnerSection boundary, out _), Is.True);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(boundary,
            out _, out IReadOnlyList<ExplorableSiteKnowledgeObservation> decoded, out _, out _), Is.True);
        Assert.That(decoded[0].SiteRuntimeId, Is.EqualTo(maxSiteId));
    }

    [Test]
    public void RejectsDuplicateInvalidAndMalformedObservationData()
    {
        ExplorableSiteKnowledgeObservation duplicateA = new ExplorableSiteKnowledgeObservation(
            "site-a", "location-a", 1L, 1L, ExplorableSiteKnowledgeSource.DirectObservation);
        ExplorableSiteKnowledgeObservation duplicateB = new ExplorableSiteKnowledgeObservation(
            "site-a", "location-b", 2L, 2L, ExplorableSiteKnowledgeSource.DirectObservation);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(
            CreateFRow("npc-a", new[] { duplicateA, duplicateB }, 2L), out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(
            CreateFRow("npc/a", Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L), out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(
            CreateFRow("npc-a", Array.Empty<ExplorableSiteKnowledgeObservation>(), -1L), out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-unreachable",
            new[] { new ExplorableSiteKnowledgeObservation("site-a", "location-a", 0L, 0L,
                ExplorableSiteKnowledgeSource.DirectObservation) }, 0L), out _, out _), Is.False);
        string tooLong = new string('x', P12AContinuationCodec.MaxStringBytes + 1);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-a",
            new[] { new ExplorableSiteKnowledgeObservation(tooLong, "location-a", 0L, 0L,
                ExplorableSiteKnowledgeSource.DirectObservation) }, 1L),
            out _, out P12AResult sizeError), Is.False);
        Assert.That(sizeError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));

        P12AOwnerSection valid = Encode("npc-a", new[] { new ExplorableSiteKnowledgeObservation(
            "site-a", "location-a", 1L, 2L, ExplorableSiteKnowledgeSource.DirectObservation) }, 4L);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(null,
            out _, out _, out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(Copy(valid, major: 2),
            out _, out _, out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(Copy(valid, minor: 1),
            out _, out _, out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(Copy(valid,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _, out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(Copy(valid,
            aliasGroup: "wrong"), out _, out _, out _, out _), Is.False);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(Copy(valid, revision: 5L),
            out _, out _, out _, out _), Is.False);
        byte[] badEnum = Payload("npc-a", "site-a", "location-a", 1L, 2L, ushort.MaxValue);
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(Copy(valid,
            rows: new[] { new P12AOwnerRow("site-a", 4L, badEnum) }), out _, out _, out _, out _), Is.False);
        byte[] trailing = valid.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryDecode(Copy(valid,
            rows: new[] { new P12AOwnerRow("site-a", 4L, trailing) }), out _, out _, out _, out _), Is.False);
    }

    private static P12DNpcFRow CreateFRow(string runtimeId,
        IEnumerable<ExplorableSiteKnowledgeObservation> observations, long revision)
        => new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, null, 0L,
            null, null, observations, revision, Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);

    private static P12AOwnerSection Encode(string runtimeId,
        IReadOnlyList<ExplorableSiteKnowledgeObservation> observations, long revision)
    {
        Assert.That(P12ANpcExplorableSiteKnowledgeOwnerCodec.TryEncode(CreateFRow(runtimeId, observations, revision),
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

    private static byte[] Payload(string runtimeId, string siteId, string locationId,
        long observedDay, long receivedDay, ushort source)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-explorable-site-knowledge/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, siteId);
            P12AWire.WriteString(writer, locationId);
            writer.Write(observedDay);
            writer.Write(receivedDay);
            writer.Write(source);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
