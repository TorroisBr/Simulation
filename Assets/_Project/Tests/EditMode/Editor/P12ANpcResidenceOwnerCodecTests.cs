using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcResidenceOwnerCodecTests
{
    [Test]
    public void UnboundNpcResidenceCodecPreservesNullAndPresentValuesAndRevision()
    {
        foreach (string residence in new[] { null, "city-settlement-4" })
        {
            P12ANpcResidenceOwnerValue source = new P12ANpcResidenceOwnerValue("npc-4", residence, 0L);
            Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(source,
                out P12AOwnerSection section, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
            Assert.That(section.Id, Is.EqualTo(NpcLifecycleCensusProvider.SectionIdFor(source.RuntimeId, true)));
            Assert.That(section.Major, Is.EqualTo(NpcLifecycleCensusProvider.SchemaVersion));
            Assert.That(section.Minor, Is.Zero);
            Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
            Assert.That(section.AliasGroup,
                Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
            Assert.That(section.Rows.Count, Is.EqualTo(1));
            Assert.That(section.Rows[0].OwnerKey, Is.EqualTo(source.RuntimeId));
            Assert.That(section.Revision, Is.Zero);

            Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(section,
                out P12ANpcResidenceOwnerValue decoded, out P12AResult decodeError),
                Is.True, decodeError?.Code.ToString());
            Assert.That(decoded.RuntimeId, Is.EqualTo(source.RuntimeId));
            Assert.That(decoded.ResidenceSettlementRuntimeId, Is.EqualTo(residence));
            Assert.That(decoded.Revision, Is.Zero);
            Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(decoded,
                out P12AOwnerSection encodedAgain, out _), Is.True);
            Assert.That(encodedAgain.Rows.Single().Payload, Is.EqualTo(section.Rows.Single().Payload));
        }
    }

    [Test]
    public void PersonBoundNpcDoesNotEmitNpcResidenceSection()
    {
        P12DNpcDRow bound = CreateDRow("npc-bound", "person-9", "city-9", 5L);
        Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(bound,
            out P12AOwnerSection section, out P12AResult result), Is.True);
        Assert.That(section, Is.Null);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void UnboundNpcDRowMapsCapturedResidenceWithoutRereadingRuntime()
    {
        P12DNpcDRow source = CreateDRow("npc-unbound", null, null, 12L);
        Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(section,
            out P12ANpcResidenceOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.RuntimeId, Is.EqualTo(source.RuntimeId));
        Assert.That(decoded.ResidenceSettlementRuntimeId, Is.EqualTo(source.ResidenceSettlementRuntimeId));
        Assert.That(decoded.Revision, Is.EqualTo(source.ResidenceRevision));
    }

    [Test]
    public void ResidenceCodecRejectsInvalidMetadataIdentityRevisionAndPayload()
    {
        Assert.That(P12ANpcResidenceOwnerCodec.TryEncode((P12ANpcResidenceOwnerValue)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(new P12ANpcResidenceOwnerValue(
            "npc-a", " ", 0L), out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(new P12ANpcResidenceOwnerValue(
            "npc/ambiguous", "city-a", 0L), out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(new P12ANpcResidenceOwnerValue(
            "npc-a", "city-a", -1L), out _, out _), Is.False);

        P12AOwnerSection section = Encode(new P12ANpcResidenceOwnerValue("npc-a", null, 4L));
        P12AOwnerSection nullIdSection = new P12AOwnerSection(null, section.Major,
            section.Minor, section.Role, section.AliasGroup, section.Revision, section.Rows);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(nullIdSection, out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section, major: 2), out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section, aliasGroup: "wrong"), out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section, role: OwnerSectionRole.ExplicitlyEmpty),
            out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section,
            rows: Array.Empty<P12AOwnerRow>()), out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows.Single(), section.Rows.Single() }), out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-other", 4L, section.Rows.Single().Payload) }),
            out _, out _), Is.False);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section, revision: 5L), out _, out _), Is.False);

        byte[] emptyStringPayload = Payload("npc-a", "", 4L);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, emptyStringPayload) }), out _, out _), Is.False);
        byte[] staleRevisionPayload = Payload("npc-a", null, 3L);
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, staleRevisionPayload) }), out _, out _), Is.False);
        byte[] trailing = section.Rows.Single().Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcResidenceOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, trailing) }), out _, out _), Is.False);
    }

    private static P12AOwnerSection Encode(P12ANpcResidenceOwnerValue value)
    {
        Assert.That(P12ANpcResidenceOwnerCodec.TryEncode(value,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        return section;
    }

    private static P12DNpcDRow CreateDRow(string runtimeId, string personId,
        string residenceId, long revision)
        => new P12DNpcDRow(runtimeId, "villager", personId, residenceId,
            NpcLifeState.Alive, NpcInjurySeverity.None, Array.Empty<string>(), 0,
            null, null, null, null, null, Array.Empty<P12DNpcInventoryRowValue>(),
            0L, 0f, 0L, 0L, revision, 0L, 0L);

    private static byte[] Payload(string runtimeId, string residenceId, long revision)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-residence/v1");
            P12AWire.WriteString(writer, runtimeId);
            P12AWire.WriteString(writer, residenceId);
            writer.Write(revision);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, long? revision = null)
        => new P12AOwnerSection(source.Id, major ?? source.Major, source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);
}
