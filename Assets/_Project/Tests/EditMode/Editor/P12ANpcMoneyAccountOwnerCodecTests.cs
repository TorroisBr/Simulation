using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public sealed class P12ANpcMoneyAccountOwnerCodecTests
{
    [Test]
    public void MoneyAccountCodecPreservesBalanceIdentityAndLocalRevision()
    {
        P12ANpcMoneyAccountOwnerValue source =
            new P12ANpcMoneyAccountOwnerValue("npc-7", 123.25f, 19L);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id, Is.EqualTo(NpcMoneyAccountCensusProvider.SectionPrefix + source.RuntimeId));
        Assert.That(section.Major, Is.EqualTo(NpcMoneyAccountCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup, Is.Null);
        Assert.That(section.Revision, Is.EqualTo(19L));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Rows[0].OwnerKey, Is.EqualTo(source.RuntimeId));
        Assert.That(section.Rows[0].Revision, Is.EqualTo(19L));

        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(section,
            out P12ANpcMoneyAccountOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.RuntimeId, Is.EqualTo(source.RuntimeId));
        Assert.That(BitConverter.GetBytes(decoded.Balance), Is.EqualTo(BitConverter.GetBytes(source.Balance)));
        Assert.That(decoded.Revision, Is.EqualTo(source.Revision));
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows[0].Payload, Is.EqualTo(section.Rows[0].Payload));
    }

    [Test]
    public void DRowAdapterUsesOnlyCapturedBalanceAndRevision()
    {
        P12DNpcDRow source = new P12DNpcDRow("npc-captured", "villager", null, null,
            NpcLifeState.Alive, NpcInjurySeverity.None, Array.Empty<string>(), 0,
            null, null, null, null, null, Array.Empty<P12DNpcInventoryRowValue>(), 0L,
            8.5f, 12L, 0L, 0L, 0L, 0L);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(section,
            out P12ANpcMoneyAccountOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.Balance, Is.EqualTo(source.MoneyBalance));
        Assert.That(decoded.Revision, Is.EqualTo(source.MoneyRevision));
    }

    [Test]
    public void ZeroBalanceAndZeroRevisionRemainRequiredOwnerValues()
    {
        P12ANpcMoneyAccountOwnerValue source = new P12ANpcMoneyAccountOwnerValue("npc-empty", 0f, 0L);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(source, out P12AOwnerSection section, out _), Is.True);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Revision, Is.Zero);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(section,
            out P12ANpcMoneyAccountOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.Balance, Is.Zero);
        Assert.That(decoded.Revision, Is.Zero);
    }

    [Test]
    public void RejectsInvalidValuesOversizedIdentityAndMalformedSections()
    {
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode((P12ANpcMoneyAccountOwnerValue)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(
            new P12ANpcMoneyAccountOwnerValue("npc/invalid", 0f, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(
            new P12ANpcMoneyAccountOwnerValue("npc-a", -1f, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(
            new P12ANpcMoneyAccountOwnerValue("npc-a", float.NaN, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(
            new P12ANpcMoneyAccountOwnerValue("npc-a", float.PositiveInfinity, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(
            new P12ANpcMoneyAccountOwnerValue("npc-a", 0f, -1L), out _, out _), Is.False);

        string oversized = new string('n', P12AContinuationCodec.MaxStringBytes);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(
            new P12ANpcMoneyAccountOwnerValue(oversized, 0f, 0L),
            out _, out P12AResult limitError), Is.False);
        Assert.That(limitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));

        P12AOwnerSection section = Encode(new P12ANpcMoneyAccountOwnerValue("npc-a", 5f, 4L));
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section, major: 2), out _, out _), Is.False);
        P12AOwnerSection wrongMinor = new P12AOwnerSection(section.Id, section.Major, 1,
            section.Role, section.AliasGroup, section.Revision, section.Rows);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(wrongMinor, out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section,
            aliasGroup: "unexpected"), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section,
            revision: 5L), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section,
            rows: Array.Empty<P12AOwnerRow>()), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows[0], section.Rows[0] }), out _, out _), Is.False);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section,
            sectionId: NpcMoneyAccountCensusProvider.SectionPrefix + "npc-other"), out _, out _), Is.False);
        byte[] trailing = new byte[section.Rows[0].PayloadLength + 1];
        Buffer.BlockCopy(section.Rows[0].Payload, 0, trailing, 0, section.Rows[0].PayloadLength);
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, trailing) }), out _, out _), Is.False);
    }

    private static P12AOwnerSection Encode(P12ANpcMoneyAccountOwnerValue value)
    {
        Assert.That(P12ANpcMoneyAccountOwnerCodec.TryEncode(value,
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
}
