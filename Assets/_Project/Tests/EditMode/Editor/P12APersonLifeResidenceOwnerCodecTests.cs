using System;
using System.IO;
using NUnit.Framework;

public sealed class P12APersonLifeResidenceOwnerCodecTests
{
    [Test]
    public void DynamicPersonSectionUsesCanonicalEncodedIdAndRoundTripsExactNullableFacts()
    {
        string personId = "A\U0001F600/B";
        P12APersonLifeResidenceOwnerValue source = new P12APersonLifeResidenceOwnerValue(
            personId, 0L, null, " settlement/東京 ", 7L);

        Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeFailure),
            Is.True, encodeFailure?.Code.ToString());
        string expectedId = "p12b.person-life-residence/"
            + personId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + personId;
        Assert.That(section.Id, Is.EqualTo(expectedId));
        Assert.That(section.Major, Is.EqualTo(PersonLifeResidenceCensusProvider.SchemaVersion));
        Assert.That(section.Minor, Is.Zero);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
        Assert.That(section.Rows, Has.Count.EqualTo(1));
        Assert.That(section.Rows[0].OwnerKey, Is.EqualTo(personId));
        Assert.That(section.Rows[0].Revision, Is.EqualTo(7L));
        Assert.That(section.Revision, Is.EqualTo(7L));

        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(section,
            out P12APersonLifeResidenceOwnerValue restored, out P12AResult decodeFailure),
            Is.True, decodeFailure?.Code.ToString());
        AssertValueEqual(source, restored);
        Assert.That(restored.BirthAbsoluteDay, Is.EqualTo(0L));
        Assert.That(restored.DeathAbsoluteDay, Is.Null);
    }

    [Test]
    public void PersonSectionPreservesExactZeroRevisionAndNullLifeResidenceValues()
    {
        P12APersonLifeResidenceOwnerValue source = new P12APersonLifeResidenceOwnerValue(
            "person-uninitialized", null, null, null, 0L);
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out _), Is.True);
        Assert.That(section.Revision, Is.Zero);
        Assert.That(section.Rows[0].Revision, Is.Zero);
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(section,
            out P12APersonLifeResidenceOwnerValue restored, out _), Is.True);
        AssertValueEqual(source, restored);
    }

    [Test]
    public void RejectsWrongIdentityRevisionAliasCardinalityAndTrailingPayload()
    {
        P12APersonLifeResidenceOwnerValue source = new P12APersonLifeResidenceOwnerValue(
            "person-one", 2L, 4L, "settlement-one", 3L);
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out _), Is.True);

        P12AOwnerSection nullId = new P12AOwnerSection(null, section.Major, section.Minor,
            section.Role, section.AliasGroup, section.Revision, section.Rows);
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(nullId, out _, out _), Is.False);

        P12AOwnerSection wrongKey = Rebuild(section,
            new[] { new P12AOwnerRow("person-other", 3L, section.Rows[0].Payload) });
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(wrongKey,
            out P12APersonLifeResidenceOwnerValue restored, out _), Is.False);
        Assert.That(restored, Is.Null);

        P12AOwnerSection wrongAlias = new P12AOwnerSection(section.Id, section.Major, section.Minor,
            section.Role, "wrong-alias", section.Revision, section.Rows);
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(wrongAlias,
            out restored, out _), Is.False);

        P12AOwnerSection wrongRevision = Rebuild(section,
            new[] { new P12AOwnerRow(section.Rows[0].OwnerKey, 2L, section.Rows[0].Payload) });
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(wrongRevision,
            out restored, out _), Is.False);

        P12AOwnerSection noRow = Rebuild(section, Array.Empty<P12AOwnerRow>());
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(noRow,
            out restored, out _), Is.False);

        byte[] trailing = new byte[section.Rows[0].Payload.Length + 1];
        Buffer.BlockCopy(section.Rows[0].Payload, 0, trailing, 0, section.Rows[0].Payload.Length);
        trailing[trailing.Length - 1] = 1;
        P12AOwnerSection trailingPayload = Rebuild(section,
            new[] { new P12AOwnerRow(section.Rows[0].OwnerKey, section.Rows[0].Revision, trailing) });
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryDecode(trailingPayload,
            out restored, out _), Is.False);
    }

    [Test]
    public void RejectsInvalidLifeResidenceValuesAndOversizedSectionIdentity()
    {
        Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(
            new P12APersonLifeResidenceOwnerValue("person-one", 8L, 7L, null, 0L),
            out P12AOwnerSection section, out _), Is.False);
        Assert.That(section, Is.Null);

        Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(
            new P12APersonLifeResidenceOwnerValue("person-one", null, null, " ", 0L),
            out section, out _), Is.False);
        Assert.That(section, Is.Null);

        Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(
            new P12APersonLifeResidenceOwnerValue("person-one", null, null, null, -1L),
            out section, out _), Is.False);

        Assert.That(P12APersonLifeResidenceOwnerCodec.TryEncode(
            new P12APersonLifeResidenceOwnerValue(
                new string('p', P12AContinuationCodec.MaxStringBytes), null, null, null, 0L),
            out section, out P12AResult limitFailure), Is.False);
        Assert.That(limitFailure.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    private static void AssertValueEqual(P12APersonLifeResidenceOwnerValue expected,
        P12APersonLifeResidenceOwnerValue actual)
    {
        Assert.That(actual.PersonIdValue, Is.EqualTo(expected.PersonIdValue));
        Assert.That(actual.BirthAbsoluteDay, Is.EqualTo(expected.BirthAbsoluteDay));
        Assert.That(actual.DeathAbsoluteDay, Is.EqualTo(expected.DeathAbsoluteDay));
        Assert.That(actual.ResidenceSettlementRuntimeId, Is.EqualTo(expected.ResidenceSettlementRuntimeId));
        Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
    }

    private static P12AOwnerSection Rebuild(P12AOwnerSection source,
        System.Collections.Generic.IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            source.AliasGroup, source.Revision, rows);
}
