using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcAdventureIntelOwnerCodecTests
{
    [Test]
    public void RoundTripsFourOrderedSectionsAndAllCapturedValues()
    {
        P12DNpcFRow source = CreateRow("npc-a", 4L,
            new[] { new AdventureOppositionObservation("site/a", null, "opposition-1",
                AdventureOppositionObservedState.Resolved, 2, 4, AdventureIntelSource.DirectObservation) },
            new[] { new AdventureNotableItemObservation("site-a", "place-a", "item-runtime",
                "item-definition", 3, 5, AdventureIntelSource.SharedByNpc, "npc-source") },
            new[] { new AdventureCommonResourceObservation("site-b", null, "ore", 6, null,
                1, 6, AdventureIntelSource.InitialScenarioKnowledge) },
            new[] { new AdventureAccessObservation("site-c", "place-c", PlaceAccessState.Contested,
                4, 7, AdventureIntelSource.DirectObservation) });

        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryEncode(source,
            out P12AOwnerSection opposition, out P12AOwnerSection notable,
            out P12AOwnerSection resources, out P12AOwnerSection access,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(new[] { opposition, notable, resources, access }.All(section =>
            section.Role == OwnerSectionRole.Required && section.Revision == 4L), Is.True);
        Assert.That(new[] { opposition, notable, resources, access }
            .Select(section => section.AliasGroup).Distinct().Count(), Is.EqualTo(1));
        Assert.That(opposition.Rows[0].OwnerKey,
            Is.EqualTo(SpatialStableKey.Encode("site/a", null, "opposition-1")));

        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryDecode(opposition, notable, resources, access,
            out P12ANpcAdventureIntelOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.RuntimeId, Is.EqualTo("npc-a"));
        Assert.That(decoded.Revision, Is.EqualTo(4L));
        Assert.That(decoded.Opposition[0].ObservedState, Is.EqualTo(AdventureOppositionObservedState.Resolved));
        Assert.That(decoded.Opposition[0].ObservedDay, Is.EqualTo(2L));
        Assert.That(decoded.Opposition[0].ReceivedDay, Is.EqualTo(4L));
        Assert.That(decoded.Opposition[0].LocalPlaceRuntimeId, Is.Null);
        Assert.That(decoded.NotableItems[0].SourceRuntimeId, Is.EqualTo("npc-source"));
        Assert.That(decoded.NotableItems[0].ItemDefinitionId, Is.EqualTo("item-definition"));
        Assert.That(decoded.Resources[0].ObservedAmount, Is.EqualTo(6));
        Assert.That(decoded.Resources[0].ResourceCategory, Is.Null);
        Assert.That(decoded.Access[0].ObservedAccessState, Is.EqualTo(PlaceAccessState.Contested));
    }

    [Test]
    public void EmitsFourRequiredSectionsForExactEmptyOwner()
    {
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryEncode(CreateRow("npc-empty", 0L),
            out P12AOwnerSection opposition, out P12AOwnerSection notable,
            out P12AOwnerSection resources, out P12AOwnerSection access, out _), Is.True);
        Assert.That(new[] { opposition, notable, resources, access }.All(section =>
            section.Role == OwnerSectionRole.Required && section.Revision == 0L && section.Rows.Count == 0), Is.True);
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryDecode(opposition, notable, resources, access,
            out P12ANpcAdventureIntelOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.Opposition, Is.Empty);
        Assert.That(decoded.NotableItems, Is.Empty);
        Assert.That(decoded.Resources, Is.Empty);
        Assert.That(decoded.Access, Is.Empty);
    }

    [Test]
    public void RejectsDuplicateIdentityAndUnreachableSharedRevision()
    {
        AdventureAccessObservation duplicateA = new AdventureAccessObservation("site", null,
            PlaceAccessState.Accessible, 0, 0, AdventureIntelSource.DirectObservation);
        AdventureAccessObservation duplicateB = new AdventureAccessObservation("site", " ",
            PlaceAccessState.Inaccessible, 0, 1, AdventureIntelSource.DirectObservation);
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryEncode(CreateRow("npc-a", 2L,
            access: new[] { duplicateA, duplicateB }), out _, out _, out _, out _, out _), Is.False);

        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryEncode(CreateRow("npc-a", 1L,
            opposition: new[] { new AdventureOppositionObservation("site", null, "opposition",
                AdventureOppositionObservedState.Active, 0, 0, AdventureIntelSource.DirectObservation) },
            access: new[] { new AdventureAccessObservation("site-2", null, PlaceAccessState.Accessible,
                0, 0, AdventureIntelSource.DirectObservation) }), out _, out _, out _, out _, out _), Is.False);
    }

    [Test]
    public void RowLimitReportsTheFirstExceededOwnerFamily()
    {
        int over = P12AContinuationCodec.MaxRows + 1;
        Assert.That(P12ANpcAdventureIntelOwnerCodec.FirstExceededKind(over, 0, 0, 0), Is.EqualTo(3));
        Assert.That(P12ANpcAdventureIntelOwnerCodec.FirstExceededKind(0, over, 0, 0), Is.EqualTo(4));
        Assert.That(P12ANpcAdventureIntelOwnerCodec.FirstExceededKind(0, 0, over, 0), Is.EqualTo(5));
        Assert.That(P12ANpcAdventureIntelOwnerCodec.FirstExceededKind(0, 0, 0, over), Is.EqualTo(6));
        Assert.That(P12ANpcAdventureIntelOwnerCodec.FirstExceededKind(0, 0, 0, 0), Is.EqualTo(-1));
    }

    [Test]
    public void DecodeRejectsCombinedRowsBeyondSharedRevisionAndSiblingMismatch()
    {
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryEncode(CreateRow("npc-pair", 2L,
            opposition: new[] { new AdventureOppositionObservation("site-a", null, "opposition-a",
                AdventureOppositionObservedState.Active, 0, 0, AdventureIntelSource.DirectObservation) },
            access: new[] { new AdventureAccessObservation("site-b", null, PlaceAccessState.Accessible,
                0, 0, AdventureIntelSource.DirectObservation) }),
            out P12AOwnerSection opposition, out P12AOwnerSection notable,
            out P12AOwnerSection resources, out P12AOwnerSection access, out _), Is.True);

        P12AOwnerSection invalidOpposition = Copy(opposition, 1L,
            new[] { new P12AOwnerRow(opposition.Rows[0].OwnerKey, 1L,
                EncodeOpposition("npc-pair", 1L, "site-a", "opposition-a")) });
        P12AOwnerSection invalidNotable = Copy(notable, 1L, Array.Empty<P12AOwnerRow>());
        P12AOwnerSection invalidResources = Copy(resources, 1L, Array.Empty<P12AOwnerRow>());
        P12AOwnerSection invalidAccess = Copy(access, 1L,
            new[] { new P12AOwnerRow(access.Rows[0].OwnerKey, 1L,
                EncodeAccess("npc-pair", 1L, "site-b")) });
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryDecode(invalidOpposition, invalidNotable,
            invalidResources, invalidAccess, out _, out P12AResult cardinalityError), Is.False);
        Assert.That(cardinalityError.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryDecode(opposition,
            Copy(notable, 3L, Array.Empty<P12AOwnerRow>()), resources, access, out _,
            out P12AResult siblingError), Is.False);
        Assert.That(siblingError.Code, Is.EqualTo(P12AResultCode.InvalidOwnerValue));
    }

    [Test]
    public void RejectsWrongAliasesTrailingPayloadAndInvalidEnums()
    {
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryEncode(CreateRow("npc-a", 1L,
            access: new[] { new AdventureAccessObservation("site", null, PlaceAccessState.Accessible,
                0, 0, AdventureIntelSource.DirectObservation) }),
            out P12AOwnerSection opposition, out P12AOwnerSection notable,
            out P12AOwnerSection resources, out P12AOwnerSection access, out _), Is.True);
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryDecode(opposition, notable, resources,
            Copy(access, access.Revision, access.Rows, "wrong"), out _, out _), Is.False);
        byte[] trailing = access.Rows[0].Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryDecode(opposition, notable, resources,
            Copy(access, access.Revision,
                new[] { new P12AOwnerRow(access.Rows[0].OwnerKey, access.Revision, trailing) }),
            out _, out _), Is.False);
        Assert.That(P12ANpcAdventureIntelOwnerCodec.TryDecode(opposition, notable, resources,
            Copy(access, access.Revision, new[] { new P12AOwnerRow(access.Rows[0].OwnerKey,
                access.Revision, EncodeAccess("npc-a", access.Revision, "site", 900)) }),
            out _, out _), Is.False);
    }

    private static P12DNpcFRow CreateRow(string runtimeId, long revision,
        AdventureOppositionObservation[] opposition = null,
        AdventureNotableItemObservation[] notable = null,
        AdventureCommonResourceObservation[] resources = null,
        AdventureAccessObservation[] access = null)
        => new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, null, 0L,
            null, null, Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            opposition ?? Array.Empty<AdventureOppositionObservation>(),
            notable ?? Array.Empty<AdventureNotableItemObservation>(),
            resources ?? Array.Empty<AdventureCommonResourceObservation>(),
            access ?? Array.Empty<AdventureAccessObservation>(), revision,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<CommercialKnowledgeShareReceipt>(), 0L);

    private static P12AOwnerSection Copy(P12AOwnerSection source, long revision,
        System.Collections.Generic.IEnumerable<P12AOwnerRow> rows, string alias = null)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            alias ?? source.AliasGroup, revision, rows);

    private static byte[] EncodeHeader(string runtimeId, long revision, string kind, Action<BinaryWriter> write)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-adventure-intel/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, kind);
            writer.Write(revision);
            write(writer);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static byte[] EncodeBase(string runtimeId, long revision, string kind, string site,
        string place, Action<BinaryWriter> write)
        => EncodeHeader(runtimeId, revision, kind, writer =>
        {
            P12AWire.WriteString(writer, site);
            P12AWire.WriteString(writer, place);
            writer.Write(0L); writer.Write(0L);
            writer.Write((int)AdventureIntelSource.DirectObservation);
            P12AWire.WriteString(writer, null);
            write(writer);
        });

    private static byte[] EncodeOpposition(string runtimeId, long revision, string site, string id)
        => EncodeBase(runtimeId, revision, "opposition", site, null, writer =>
        {
            P12AWire.WriteString(writer, id);
            writer.Write((int)AdventureOppositionObservedState.Active);
        });

    private static byte[] EncodeAccess(string runtimeId, long revision, string site, int state = (int)PlaceAccessState.Accessible)
        => EncodeBase(runtimeId, revision, "access", site, null, writer => writer.Write(state));
}
