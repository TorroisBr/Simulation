using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EJusticeRecordsOwnerCodecTests
{
    [Test]
    public void JusticeCodecPreservesOrderedRowsNullablePersonBindingsAndExactRevision()
    {
        P12EJusticeRecordsOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EJusticeRecordsOwnerCodec.TryEncode(source,
            out P12AOwnerSection section, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());

        Assert.That(section.Id, Is.EqualTo(P12CrimeJusticeCensusProvider.JusticeRecordsSectionId));
        Assert.That(section.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(section.Id)));
        Assert.That(section.Revision, Is.EqualTo(7L));
        Assert.That(section.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "wanted/0000000000", "wanted/0000000001", "sentence/0000000000"
        }));

        Assert.That(P12EJusticeRecordsOwnerCodec.TryDecode(section, 10L,
            out P12EJusticeRecordsOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.CapturedAbsoluteDay, Is.EqualTo(10L));
        Assert.That(decoded.Records.RecordCount, Is.EqualTo(3));
        Assert.That(decoded.Records.Revision, Is.EqualTo(7L));
        Assert.That(decoded.Records.WantedRows.Select(row => row.TargetRuntimeId),
            Is.EqualTo(new[] { "npc-z", "npc-a" }));
        Assert.That(decoded.Records.WantedRows[0].TargetPersonIdValue, Is.Null);
        Assert.That(decoded.Records.WantedRows[1].TargetPersonIdValue, Is.EqualTo("person-a"));
        Assert.That(decoded.Records.WantedRows[0].Bounty, Is.EqualTo(25.5f));
        Assert.That(decoded.Records.WantedRows[0].SentenceDays, Is.EqualTo(4));
        Assert.That(decoded.Records.WantedRows[1].IsResolved, Is.True);
        Assert.That(decoded.Records.SentenceRows.Single().WarrantOrdinal, Is.Zero);
        Assert.That(decoded.Records.SentenceRows.Single().RemainingDays, Is.EqualTo(2));
        Assert.That(decoded.Records.SentenceRows.Single().FailedEscapeAttempts, Is.EqualTo(1));
        Assert.That(decoded.Records.SentenceRows.Single().WasArrestedToday, Is.True);

        Assert.That(P12EJusticeRecordsOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        AssertRowsEqual(section, encodedAgain);
    }

    [Test]
    public void JusticeCodecPreservesRequiredEmptySectionAndNonzeroPostRemovalRevision()
    {
        P12EJusticeRecordsOwnerSnapshot empty = new P12EJusticeRecordsOwnerSnapshot(0L,
            new P12EJusticeRecordsSnapshotSection(P12EJusticeRecordsSnapshotSection.CurrentSectionId,
                P12EJusticeRecordsOwnerSnapshot.CurrentSchemaVersion, 0, 3L,
                Array.Empty<P12EJusticeWantedSnapshotRow>(), Array.Empty<P12EJusticeSentenceSnapshotRow>()));
        Assert.That(P12EJusticeRecordsOwnerCodec.TryEncode(empty,
            out P12AOwnerSection section, out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(section.Rows, Is.Empty);
        Assert.That(section.Revision, Is.EqualTo(3L));
        Assert.That(P12EJusticeRecordsOwnerCodec.TryDecode(section, 0L,
            out P12EJusticeRecordsOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.Records.Revision, Is.EqualTo(3L));
        Assert.That(decoded.Records.RecordCount, Is.Zero);
    }

    [Test]
    public void JusticeCodecRejectsWrongMetadataMalformedBooleanTrailingBytesAndOrdering()
    {
        Assert.That(P12EJusticeRecordsOwnerCodec.TryEncode(CreateSnapshot(),
            out P12AOwnerSection section, out _), Is.True);
        Assert.That(P12EJusticeRecordsOwnerCodec.TryDecode(
            CopySection(section, section.Rows, aliasGroup: null), 10L, out _, out _), Is.False);

        byte[] badBoolean = section.Rows[0].Payload;
        badBoolean[badBoolean.Length - 1] = 2;
        List<P12AOwnerRow> badBooleanRows = new List<P12AOwnerRow>(section.Rows)
        { [0] = new P12AOwnerRow(section.Rows[0].OwnerKey, section.Rows[0].Revision, badBoolean) };
        Assert.That(P12EJusticeRecordsOwnerCodec.TryDecode(
            CopySection(section, badBooleanRows), 10L, out _, out _), Is.False);

        byte[] trailing = new byte[section.Rows[0].PayloadLength + 1];
        Array.Copy(section.Rows[0].Payload, trailing, section.Rows[0].PayloadLength);
        List<P12AOwnerRow> trailingRows = new List<P12AOwnerRow>(section.Rows)
        { [0] = new P12AOwnerRow(section.Rows[0].OwnerKey, section.Rows[0].Revision, trailing) };
        Assert.That(P12EJusticeRecordsOwnerCodec.TryDecode(
            CopySection(section, trailingRows), 10L, out _, out _), Is.False);

        List<P12AOwnerRow> reordered = new List<P12AOwnerRow>(section.Rows);
        P12AOwnerRow first = reordered[0];
        reordered[0] = reordered[1];
        reordered[1] = first;
        Assert.That(P12EJusticeRecordsOwnerCodec.TryDecode(
            CopySection(section, reordered), 10L, out _, out _), Is.False);
    }

    private static P12EJusticeRecordsOwnerSnapshot CreateSnapshot()
    {
        P12EJusticeWantedSnapshotRow[] wanted =
        {
            new P12EJusticeWantedSnapshotRow(0, "npc-z", null, "city-z", 25.5f, 4, false),
            new P12EJusticeWantedSnapshotRow(1, "npc-a", "person-a", "city-a", 0f, 1, true)
        };
        P12EJusticeSentenceSnapshotRow[] sentences =
        {
            new P12EJusticeSentenceSnapshotRow(0, "npc-z", null, "city-z", 0, 2, 1, true)
        };
        return new P12EJusticeRecordsOwnerSnapshot(10L,
            new P12EJusticeRecordsSnapshotSection(P12EJusticeRecordsSnapshotSection.CurrentSectionId,
                P12EJusticeRecordsOwnerSnapshot.CurrentSchemaVersion, 3, 7L, wanted, sentences));
    }

    private static P12AOwnerSection CopySection(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            source.AliasGroup, source.Revision, rows);

    private static P12AOwnerSection CopySection(P12AOwnerSection source,
        IEnumerable<P12AOwnerRow> rows, string aliasGroup)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            aliasGroup, source.Revision, rows);

    private static void AssertRowsEqual(P12AOwnerSection expected, P12AOwnerSection actual)
    {
        Assert.That(actual.Id, Is.EqualTo(expected.Id));
        Assert.That(actual.AliasGroup, Is.EqualTo(expected.AliasGroup));
        Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
        Assert.That(actual.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(expected.Rows.Select(row => row.OwnerKey)));
        Assert.That(actual.Rows.Select(row => Convert.ToBase64String(row.Payload)),
            Is.EqualTo(expected.Rows.Select(row => Convert.ToBase64String(row.Payload))));
    }
}
