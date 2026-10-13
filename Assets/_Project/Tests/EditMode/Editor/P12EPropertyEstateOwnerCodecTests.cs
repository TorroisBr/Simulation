using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EPropertyEstateOwnerCodecTests
{
    [Test]
    public void PropertyEstateCodecRoundTripsCanonicalRowsAndDuplicateTransferOccurrences()
    {
        PropertyEstateOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EPropertyEstateOwnerCodec.TryEncode(source, 10L,
            out P12AOwnerSection ownership, out P12AOwnerSection history, out P12AOwnerSection estates,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());

        Assert.That(ownership.Id, Is.EqualTo(PropertyOwnershipCensusProvider.OwnershipSectionId));
        Assert.That(history.Id, Is.EqualTo(PropertyOwnershipCensusProvider.TransferHistorySectionId));
        Assert.That(estates.Id, Is.EqualTo(EstateCensusProvider.SectionId));
        Assert.That(ownership.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(ownership.Id)));
        Assert.That(history.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(history.Id)));
        Assert.That(estates.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(estates.Id)));
        Assert.That(ownership.Revision, Is.EqualTo(5L));
        Assert.That(history.Revision, Is.EqualTo(5L));
        Assert.That(estates.Revision, Is.EqualTo(1L));
        Assert.That(ownership.Rows.Select(row => row.OwnerKey).ToArray(),
            Is.EqualTo(new[] { "property/property-a", "property/property-z" }));
        Assert.That(history.Rows.Select(row => row.OwnerKey).ToArray(),
            Is.EqualTo(new[]
            {
                "transfer/10:property-a/4/10:person-old/10:person-new/0000000000",
                "transfer/10:property-a/4/10:person-old/10:person-new/0000000001",
                "transfer/10:property-z/2/8:person-z/8:person-z/0000000000"
            }));
        Assert.That(estates.Rows.Select(row => row.OwnerKey).ToArray(), Is.EqualTo(new[] { "estate/estate-a" }));

        Assert.That(P12EPropertyEstateOwnerCodec.TryDecode(ownership, history, estates, 10L,
            out PropertyEstateOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.OwnershipRows.Select(row => row.PropertyIdValue).ToArray(),
            Is.EqualTo(new[] { "property-a", "property-z" }));
        Assert.That(decoded.TransferHistoryRows.Select(row => row.TransferAbsoluteDay).ToArray(), Is.EqualTo(new[] { 4L, 4L, 2L }));
        Assert.That(decoded.TransferHistoryRows[1].PropertyIdValue, Is.EqualTo("property-a"));
        Assert.That(decoded.TransferHistoryRows[2].PropertyIdValue, Is.EqualTo("property-z"));
        Assert.That(decoded.TransferHistoryRows[1].PreviousOwnerPersonIdValue, Is.EqualTo("person-old"));
        Assert.That(decoded.TransferHistoryRows[1].NewOwnerPersonIdValue, Is.EqualTo("person-new"));

        Assert.That(P12EPropertyEstateOwnerCodec.TryEncode(decoded, 10L,
            out P12AOwnerSection ownershipAgain, out P12AOwnerSection historyAgain,
            out P12AOwnerSection estatesAgain, out _), Is.True);
        AssertRowsEqual(ownership, ownershipAgain);
        AssertRowsEqual(history, historyAgain);
        AssertRowsEqual(estates, estatesAgain);

        PersonStore stagedPersons = CreateStagingPersons();
        Assert.That(decoded.TryStage(stagedPersons, 10L,
            out PropertyOwnershipStore stagedProperty, out EstateStore stagedEstate, out _), Is.True);
        Assert.That(stagedProperty.Count, Is.EqualTo(2));
        Assert.That(stagedProperty.TransferHistory.Count, Is.EqualTo(3));
        Assert.That(stagedProperty.Revision, Is.EqualTo(5L));
        Assert.That(stagedEstate.Count, Is.EqualTo(1));
        Assert.That(stagedEstate.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void PropertyEstateCodecPreservesThreeExactEmptyRequiredSections()
    {
        PropertyEstateOwnerSnapshot empty = new PropertyEstateOwnerSnapshot(
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 0, 0L, Array.Empty<PropertyEstateOwnershipRow>(),
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 0, 0L, Array.Empty<PropertyEstateTransferHistoryRow>(),
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 0, 0L, Array.Empty<PropertyEstateEstateRow>());
        Assert.That(P12EPropertyEstateOwnerCodec.TryEncode(empty, 0L,
            out P12AOwnerSection ownership, out P12AOwnerSection history,
            out P12AOwnerSection estates, out _), Is.True);
        Assert.That(ownership.Rows, Is.Empty);
        Assert.That(history.Rows, Is.Empty);
        Assert.That(estates.Rows, Is.Empty);
        Assert.That(P12EPropertyEstateOwnerCodec.TryDecode(ownership, history, estates, 0L,
            out PropertyEstateOwnerSnapshot decoded, out _), Is.True);
        Assert.That(decoded.PropertyRevision, Is.Zero);
        Assert.That(decoded.EstateRevision, Is.Zero);
    }

    [Test]
    public void PropertyEstateCodecRejectsWrongRevisionIdentityOwnerKeyAndTrailingPayload()
    {
        Assert.That(P12EPropertyEstateOwnerCodec.TryEncode(CreateSnapshot(), 10L,
            out P12AOwnerSection ownership, out P12AOwnerSection history,
            out P12AOwnerSection estates, out _), Is.True);

        Assert.That(P12EPropertyEstateOwnerCodec.TryDecode(
            CopySection(ownership, ownership.Revision - 1L, ownership.Rows), history, estates, 10L, out _, out _), Is.False);
        Assert.That(P12EPropertyEstateOwnerCodec.TryDecode(ownership, history,
            CopySection(estates, estates.Revision + 1L, estates.Rows), 10L, out _, out _), Is.False);

        List<P12AOwnerRow> wrongOwnershipKey = new List<P12AOwnerRow>(ownership.Rows)
        { [0] = new P12AOwnerRow("property/other", ownership.Rows[0].Revision, ownership.Rows[0].Payload) };
        Assert.That(P12EPropertyEstateOwnerCodec.TryDecode(
            CopySection(ownership, ownership.Revision, wrongOwnershipKey), history, estates, 10L, out _, out _), Is.False);

        List<P12AOwnerRow> trailingEstatePayload = new List<P12AOwnerRow>(estates.Rows);
        byte[] payload = estates.Rows[0].Payload;
        byte[] extendedPayload = new byte[payload.Length + 1];
        Array.Copy(payload, extendedPayload, payload.Length);
        extendedPayload[extendedPayload.Length - 1] = 0x7f;
        trailingEstatePayload[0] = new P12AOwnerRow(estates.Rows[0].OwnerKey,
            estates.Rows[0].Revision, extendedPayload);
        Assert.That(P12EPropertyEstateOwnerCodec.TryDecode(ownership, history,
            CopySection(estates, estates.Revision, trailingEstatePayload), 10L, out _, out _), Is.False);

        PropertyEstateOwnerSnapshot duplicateProperty = new PropertyEstateOwnerSnapshot(
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 2, 4L,
            new[]
            {
                new PropertyEstateOwnershipRow("property-a", "person-new"),
                new PropertyEstateOwnershipRow("property-a", "person-new")
            },
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 2, 4L,
            new[]
            {
                new PropertyEstateTransferHistoryRow("property-a", "person-old", "person-new", 2L),
                new PropertyEstateTransferHistoryRow("property-a", "person-new", "person-new", 4L)
            },
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 0, 0L,
            Array.Empty<PropertyEstateEstateRow>());
        Assert.That(P12EPropertyEstateOwnerCodec.TryEncode(duplicateProperty, 10L,
            out _, out _, out _, out _), Is.False);
    }

    [Test]
    public void PropertyEstateCodecAndStagerRejectOutOfBoundaryDaysAndMissingReferences()
    {
        PropertyEstateOwnerSnapshot source = CreateSnapshot();
        PropertyEstateOwnerSnapshot lateTransfer = new PropertyEstateOwnerSnapshot(
            source.OwnershipSchemaVersion, source.OwnershipRecordCount, source.PropertyRevision, source.OwnershipRows,
            source.TransferHistorySchemaVersion, source.TransferHistoryRecordCount, source.TransferHistoryRevision,
            new[]
            {
                new PropertyEstateTransferHistoryRow("property-a", "person-old", "person-new", 2L),
                new PropertyEstateTransferHistoryRow("property-a", "person-old", "person-new", 4L),
                new PropertyEstateTransferHistoryRow("property-z", "person-z", "person-z", 11L)
            },
            source.EstateSchemaVersion, source.EstateRecordCount, source.EstateRevision, source.EstateRows);
        Assert.That(P12EPropertyEstateOwnerCodec.TryEncode(lateTransfer, 10L,
            out _, out _, out _, out _), Is.False);

        Assert.That(P12EPropertyEstateOwnerCodec.TryEncode(source, 10L,
            out P12AOwnerSection ownership, out P12AOwnerSection history,
            out P12AOwnerSection estates, out _), Is.True);
        Assert.That(P12EPropertyEstateOwnerCodec.TryDecode(ownership, history, estates, 10L,
            out PropertyEstateOwnerSnapshot canonical, out _), Is.True);
        Assert.That(canonical.TryStage(new PersonStore(), 10L,
            out _, out _, out PropertyEstateOwnerSnapshotFailure missingReference), Is.False);
        Assert.That(missingReference.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidReference));

        PropertyEstateOwnerSnapshot earlyEstate = new PropertyEstateOwnerSnapshot(
            canonical.OwnershipSchemaVersion, canonical.OwnershipRecordCount, canonical.PropertyRevision, canonical.OwnershipRows,
            canonical.TransferHistorySchemaVersion, canonical.TransferHistoryRecordCount, canonical.TransferHistoryRevision,
            canonical.TransferHistoryRows,
            source.EstateSchemaVersion, source.EstateRecordCount, source.EstateRevision,
            new[] { new PropertyEstateEstateRow("estate-a", "person-deceased", 4L) });
        Assert.That(earlyEstate.TryStage(CreateStagingPersons(), 10L,
            out _, out _, out PropertyEstateOwnerSnapshotFailure earlyFailure), Is.False);
        Assert.That(earlyFailure.Code, Is.EqualTo(PropertyEstateOwnerSnapshotFailureCode.InvalidReference));
    }

    private static PropertyEstateOwnerSnapshot CreateSnapshot()
        => new PropertyEstateOwnerSnapshot(
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 2, 5L,
            new[]
            {
                new PropertyEstateOwnershipRow("property-z", "person-z"),
                new PropertyEstateOwnershipRow("property-a", "person-new")
            },
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 3, 5L,
            new[]
            {
                new PropertyEstateTransferHistoryRow("property-a", "person-old", "person-new", 4L),
                new PropertyEstateTransferHistoryRow("property-z", "person-z", "person-z", 2L),
                new PropertyEstateTransferHistoryRow("property-a", "person-old", "person-new", 4L)
            },
            PropertyEstateOwnerSnapshot.CurrentSchemaVersion, 1, 1L,
            new[] { new PropertyEstateEstateRow("estate-a", "person-deceased", 6L) });

    private static PersonStore CreateStagingPersons()
    {
        PersonStore persons = new PersonStore();
        foreach (string id in new[] { "person-new", "person-old", "person-z" })
            Assert.That(persons.TryRegister(new PersonRuntime(new PersonId(id)), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(new PersonId("person-deceased"), 0L, 5L), out _), Is.True);
        return persons;
    }

    private static P12AOwnerSection CopySection(P12AOwnerSection source, long revision,
        IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor,
            source.Role, source.AliasGroup, revision, rows);

    private static void AssertRowsEqual(P12AOwnerSection left, P12AOwnerSection right)
    {
        Assert.That(right.Id, Is.EqualTo(left.Id));
        Assert.That(right.Revision, Is.EqualTo(left.Revision));
        Assert.That(right.Rows.Select(row => row.OwnerKey).ToArray(), Is.EqualTo(left.Rows.Select(row => row.OwnerKey).ToArray()));
        Assert.That(right.Rows.Select(row => Convert.ToBase64String(row.Payload)).ToArray(),
            Is.EqualTo(left.Rows.Select(row => Convert.ToBase64String(row.Payload)).ToArray()));
    }
}
