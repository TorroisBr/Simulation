using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcSpatialKnowledgeOwnerCodecTests
{
    [Test]
    public void PairedSectionsPreserveCapturedOrderedIdsAndSharedRevision()
    {
        P12DNpcFRow source = CreateFRow("npc-4",
            new[] { "location-z", "location-a" }, new[] { "route-2", "route-1" }, 11L);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(source,
            out P12AOwnerSection locations, out P12AOwnerSection routes,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(locations.Id, Is.EqualTo(SpatialKnowledgeCensusProvider.LocationsSectionPrefix + "npc-4"));
        Assert.That(routes.Id, Is.EqualTo(SpatialKnowledgeCensusProvider.RoutesSectionPrefix + "npc-4"));
        string expectedAlias = P12ADailyV1Census.ExpectedAliasGroupForSection(locations.Id);
        Assert.That(locations.AliasGroup, Is.EqualTo(expectedAlias));
        Assert.That(routes.AliasGroup, Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(routes.Id)));
        Assert.That(locations.AliasGroup, Is.EqualTo(routes.AliasGroup));
        Assert.That(locations.Revision, Is.EqualTo(11L));
        Assert.That(routes.Revision, Is.EqualTo(11L));
        Assert.That(locations.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { "location-z", "location-a" }));
        Assert.That(routes.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { "route-2", "route-1" }));
        Assert.That(locations.Rows.All(row => row.Revision == 11L), Is.True);
        Assert.That(routes.Rows.All(row => row.Revision == 11L), Is.True);

        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(locations, routes,
            out P12ANpcSpatialKnowledgeOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.RuntimeId, Is.EqualTo("npc-4"));
        Assert.That(decoded.Revision, Is.EqualTo(11L));
        Assert.That(decoded.LocationIds, Is.EqualTo(new[] { "location-z", "location-a" }));
        Assert.That(decoded.RouteIds, Is.EqualTo(new[] { "route-2", "route-1" }));
    }

    [Test]
    public void EmptyKnowledgeUsesTwoRequiredAliasedSections()
    {
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-empty",
            Array.Empty<string>(), Array.Empty<string>(), 0L),
            out P12AOwnerSection locations, out P12AOwnerSection routes, out _), Is.True);
        Assert.That(locations.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(routes.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(locations.Rows, Is.Empty);
        Assert.That(routes.Rows, Is.Empty);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(locations, routes,
            out P12ANpcSpatialKnowledgeOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.LocationIds, Is.Empty);
        Assert.That(decoded.RouteIds, Is.Empty);
        Assert.That(decoded.Revision, Is.Zero);
    }

    [Test]
    public void RejectsDuplicateInvalidAndOversizedIds()
    {
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-a",
            new[] { "location-a", "location-a" }, Array.Empty<string>(), 2L), out _, out _, out _), Is.False);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc/a",
            Array.Empty<string>(), Array.Empty<string>(), 0L), out _, out _, out _), Is.False);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-a",
            new[] { " " }, Array.Empty<string>(), 1L), out _, out _, out _), Is.False);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-a",
            Array.Empty<string>(), Array.Empty<string>(), -1L), out _, out _, out _), Is.False);
        string maxId = new string('i', P12AContinuationCodec.MaxStringBytes);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-boundary",
            new[] { maxId }, new[] { maxId }, 2L), out P12AOwnerSection locations,
            out P12AOwnerSection routes, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(locations, routes,
            out P12ANpcSpatialKnowledgeOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.LocationIds[0], Is.EqualTo(maxId));
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-unreachable",
            new[] { "location-a" }, new[] { "route-a" }, 1L), out _, out _, out _), Is.False);

        string tooLong = new string('x', P12AContinuationCodec.MaxStringBytes + 1);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-a",
            new[] { tooLong }, Array.Empty<string>(), 1L), out _, out _, out P12AResult sizeError), Is.False);
        Assert.That(sizeError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void RejectsSectionAliasVersionRevisionPairAndPayloadMismatch()
    {
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-a",
            new[] { "location-a" }, new[] { "route-a" }, 4L),
            out P12AOwnerSection locations, out P12AOwnerSection routes, out _), Is.True);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(Copy(locations, major: 2), routes,
            out _, out _), Is.False);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(Copy(locations, minor: 1), routes,
            out _, out _), Is.False);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(Copy(locations, aliasGroup: "wrong"), routes,
            out _, out _), Is.False);
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(locations, Copy(routes, revision: 5L),
            out _, out P12AResult mismatchedRevisionError), Is.False);
        Assert.That(mismatchedRevisionError.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(locations,
            Copy(routes, sectionId: SpatialKnowledgeCensusProvider.RoutesSectionPrefix + "npc-other"),
            out _, out P12AResult mismatchedOwnerError), Is.False);
        Assert.That(mismatchedOwnerError.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(Copy(locations,
            rows: Array.Empty<P12AOwnerRow>()), routes,
            out P12ANpcSpatialKnowledgeOwnerValue oneEmpty, out _), Is.True);
        Assert.That(oneEmpty.LocationIds, Is.Empty);
        Assert.That(oneEmpty.RouteIds, Is.EqualTo(new[] { "route-a" }));
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(Copy(locations,
            rows: new[] { locations.Rows[0], locations.Rows[0] }), routes, out _, out _), Is.False);
        byte[] payload = locations.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(Copy(locations,
            rows: new[] { new P12AOwnerRow("location-a", 4L, payload) }), routes, out _, out _), Is.False);
    }

    [Test]
    public void DecodeRejectsCombinedCardinalityBeyondSharedRevision()
    {
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryEncode(CreateFRow("npc-pair",
            new[] { "location-a" }, new[] { "route-a" }, 2L),
            out P12AOwnerSection locations, out P12AOwnerSection routes, out _), Is.True);
        P12AOwnerSection oneLocationAtRevisionOne = Copy(locations,
            revision: 1L,
            rows: new[] { new P12AOwnerRow("location-a", 1L,
                EncodeRow("npc-pair", "location-a", 1L, "location")) });
        P12AOwnerSection oneRouteAtRevisionOne = Copy(routes,
            revision: 1L,
            rows: new[] { new P12AOwnerRow("route-a", 1L,
                EncodeRow("npc-pair", "route-a", 1L, "route")) });

        // Each section individually fits revision 1. Together they require two
        // successful DiscoverId mutations and therefore revision at least 2.
        Assert.That(P12ANpcSpatialKnowledgeOwnerCodec.TryDecode(oneLocationAtRevisionOne,
            oneRouteAtRevisionOne, out _, out P12AResult decodeError), Is.False);
        Assert.That(decodeError.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    private static P12DNpcFRow CreateFRow(string runtimeId,
        IEnumerable<string> locations, IEnumerable<string> routes, long revision)
        => new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, null, 0L,
            null, null, Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            locations, routes, revision,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<CommercialKnowledgeShareReceipt>(), 0L);

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, int? minor = null,
        long? revision = null, string sectionId = null)
        => new P12AOwnerSection(sectionId ?? source.Id, major ?? source.Major, minor ?? source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);

    private static byte[] EncodeRow(string runtimeId, string id, long revision, string keyKind)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-spatial-knowledge/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, keyKind);
            P12AWire.WriteString(writer, id);
            writer.Write(revision);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
