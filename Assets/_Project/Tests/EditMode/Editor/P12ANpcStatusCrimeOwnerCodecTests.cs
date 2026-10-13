using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcStatusCrimeOwnerCodecTests
{
    [Test]
    public void StatusCrimeCodecPreservesOrderedValuesHiddenDaysAndLocalRevision()
    {
        string[] statuses = { "injured", "masked", "injured" };
        P12ANpcStatusCrimeOwnerValue source =
            new P12ANpcStatusCrimeOwnerValue("npc-7", statuses, 5, 19L);
        statuses[0] = "mutated-after-capture";

        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(section.Id,
            Is.EqualTo(P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor(source.RuntimeId)));
        Assert.That(section.Major, Is.EqualTo(P12CrimeJusticeCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Rows[0].OwnerKey, Is.EqualTo(source.RuntimeId));
        Assert.That(section.Rows[0].Revision, Is.EqualTo(19L));
        Assert.That(section.Revision, Is.EqualTo(19L));

        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(section,
            out P12ANpcStatusCrimeOwnerValue decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.RuntimeId, Is.EqualTo(source.RuntimeId));
        Assert.That(decoded.HiddenDaysRemaining, Is.EqualTo(5));
        Assert.That(decoded.CurrentStatusDefinitionIds,
            Is.EqualTo(new[] { "injured", "masked", "injured" }));
        Assert.That(decoded.Revision, Is.EqualTo(19L));

        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows.Single().Payload, Is.EqualTo(section.Rows.Single().Payload));
    }

    [Test]
    public void EmptyStatusListAndZeroRevisionRemainRequiredOwnerValues()
    {
        P12ANpcStatusCrimeOwnerValue source =
            new P12ANpcStatusCrimeOwnerValue("npc-empty", Array.Empty<string>(), 0, 0L);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out _), Is.True);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Revision, Is.Zero);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(section,
            out P12ANpcStatusCrimeOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.CurrentStatusDefinitionIds, Is.Empty);
        Assert.That(decoded.HiddenDaysRemaining, Is.Zero);
        Assert.That(decoded.Revision, Is.Zero);
    }

    [Test]
    public void DRowAdapterUsesOnlyCapturedStatusValues()
    {
        P12DNpcDRow source = CreateDRow("npc-captured", new[] { "wanted", "hidden" }, 8, 12L);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError),
            Is.True, encodeError?.Code.ToString());
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(section,
            out P12ANpcStatusCrimeOwnerValue decoded, out _), Is.True);
        Assert.That(decoded.CurrentStatusDefinitionIds, Is.EqualTo(source.CurrentStatusDefinitionIds));
        Assert.That(decoded.HiddenDaysRemaining, Is.EqualTo(source.HiddenDaysRemaining));
        Assert.That(decoded.Revision, Is.EqualTo(source.CrimeJusticeRevision));
    }

    [Test]
    public void RejectsInvalidOwnerValuesAndOversizedStrings()
    {
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode((P12ANpcStatusCrimeOwnerValue)null,
            out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(
            new P12ANpcStatusCrimeOwnerValue(" ", Array.Empty<string>(), 0, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(
            new P12ANpcStatusCrimeOwnerValue("npc/invalid", Array.Empty<string>(), 0, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(
            new P12ANpcStatusCrimeOwnerValue("npc-a", new[] { " " }, 0, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(
            new P12ANpcStatusCrimeOwnerValue("npc-a", Array.Empty<string>(), -1, 0L), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(
            new P12ANpcStatusCrimeOwnerValue("npc-a", Array.Empty<string>(), 0, -1L), out _, out _), Is.False);

        string oversized = new string('s', P12AContinuationCodec.MaxStringBytes + 1);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(
            new P12ANpcStatusCrimeOwnerValue("npc-a", new[] { oversized }, 0, 0L),
            out _, out P12AResult limitError), Is.False);
        Assert.That(limitError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void RejectsPrefixedEnvelopeStringsThatExceedTheWireLimit()
    {
        string shortSectionId = P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor("x");
        string shortAlias = P12ADailyV1Census.ExpectedAliasGroupForSection(shortSectionId);
        int longestPrefixLength = Math.Max(
            P12CrimeJusticeCensusProvider.NpcStatusSectionPrefix.Length,
            shortAlias.Length - 1);
        string runtimeId = new string('x', P12AContinuationCodec.MaxStringBytes - longestPrefixLength + 1);
        Assert.That(runtimeId.Length, Is.LessThanOrEqualTo(P12AContinuationCodec.MaxStringBytes));

        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(
            new P12ANpcStatusCrimeOwnerValue(runtimeId, Array.Empty<string>(), 0, 0L),
            out _, out P12AResult error), Is.False);
        Assert.That(error.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void DRowEncodeReturnsLimitResultWhenCapturedStatusCountExceedsBound()
    {
        List<string> statuses = new List<string>(P12AContinuationCodec.MaxRows + 1);
        for (int index = 0; index <= P12AContinuationCodec.MaxRows; index++) statuses.Add("status");
        P12DNpcDRow source = CreateDRow("npc-many-statuses", statuses, 0, 0L);

        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult error), Is.False);
        Assert.That(section, Is.Null);
        Assert.That(error.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void DecodeRejectsOverlongDerivedAliasWithoutThrowing()
    {
        string shortSectionId = P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor("x");
        string shortAlias = P12ADailyV1Census.ExpectedAliasGroupForSection(shortSectionId);
        int aliasPrefixLength = shortAlias.Length - 1;
        string runtimeId = new string('x', P12AContinuationCodec.MaxStringBytes - aliasPrefixLength + 1);
        string sectionId = P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor(runtimeId);
        string alias = P12ADailyV1Census.ExpectedAliasGroupForSection(sectionId);
        byte[] payload = Payload(runtimeId, 0, Array.Empty<string>(), 0L);
        P12AOwnerSection section = new P12AOwnerSection(sectionId,
            P12CrimeJusticeCensusProvider.SchemaVersion, 0, OwnerSectionRole.Required,
            alias, 0L, new[] { new P12AOwnerRow(runtimeId, 0L, payload) });

        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(section, out _, out P12AResult error), Is.False);
        Assert.That(error.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void RejectsWrongMetadataIdentityRevisionAndMalformedPayloads()
    {
        P12AOwnerSection section = Encode(new P12ANpcStatusCrimeOwnerValue(
            "npc-a", new[] { "status-a" }, 1, 4L));
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section, major: 2), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            aliasGroup: "wrong"), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            role: OwnerSectionRole.ExplicitlyEmpty), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            revision: 5L), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            rows: Array.Empty<P12AOwnerRow>()), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            rows: new[] { section.Rows.Single(), section.Rows.Single() }), out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-other", 4L, section.Rows.Single().Payload) }),
            out _, out _), Is.False);

        P12AOwnerSection nullId = new P12AOwnerSection(null, section.Major, section.Minor,
            section.Role, section.AliasGroup, section.Revision, section.Rows);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(nullId, out _, out _), Is.False);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            sectionId: P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor("npc-other")),
            out _, out _), Is.False);

        byte[] trailing = section.Rows.Single().Payload.Concat(new byte[] { 0 }).ToArray();
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, trailing) }), out _, out _), Is.False);
        byte[] impossibleCount = MalformedCountPayload();
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, impossibleCount) }), out _, out _), Is.False);
        byte[] negativeHidden = Payload("npc-a", -1, Array.Empty<string>(), 4L);
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryDecode(Copy(section,
            rows: new[] { new P12AOwnerRow("npc-a", 4L, negativeHidden) }), out _, out _), Is.False);
    }

    private static P12AOwnerSection Encode(P12ANpcStatusCrimeOwnerValue value)
    {
        Assert.That(P12ANpcStatusCrimeOwnerCodec.TryEncode(value,
            out P12AOwnerSection section, out P12AResult error), Is.True, error?.Code.ToString());
        return section;
    }

    private static P12DNpcDRow CreateDRow(string runtimeId, IEnumerable<string> statuses,
        int hiddenDaysRemaining, long crimeJusticeRevision)
        => new P12DNpcDRow(runtimeId, "villager", null, null,
            NpcLifeState.Alive, NpcInjurySeverity.None, statuses, hiddenDaysRemaining,
            null, null, null, null, null, Array.Empty<P12DNpcInventoryRowValue>(),
            0L, 0f, 0L, 0L, 0L, crimeJusticeRevision, 0L);

    private static P12AOwnerSection Copy(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows = null, string aliasGroup = null,
        OwnerSectionRole? role = null, int? major = null, long? revision = null,
        string sectionId = null)
        => new P12AOwnerSection(sectionId ?? source.Id, major ?? source.Major, source.Minor,
            role ?? source.Role, aliasGroup ?? source.AliasGroup, revision ?? source.Revision,
            rows ?? source.Rows);

    private static byte[] MalformedCountPayload()
        => Payload("npc-a", 0, null, 4L, overrideCount: int.MaxValue);

    private static byte[] Payload(string runtimeId, int hiddenDaysRemaining,
        IReadOnlyList<string> statuses, long revision, int? overrideCount = null)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, P12AWire.Utf8, true))
        {
            P12AWire.WriteString(writer, "p12a.npc-status-crime-state/v1");
            P12AWire.WriteString(writer, runtimeId);
            writer.Write(hiddenDaysRemaining);
            writer.Write(overrideCount ?? statuses.Count);
            if (statuses != null)
                foreach (string status in statuses) P12AWire.WriteString(writer, status);
            writer.Write(revision);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
