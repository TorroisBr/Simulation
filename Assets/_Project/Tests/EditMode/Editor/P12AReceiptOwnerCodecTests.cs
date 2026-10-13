using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12AReceiptOwnerCodecTests
{
    [Test]
    public void PopulatedReceiptOwners_RoundTripDeterministically()
    {
        P12ACrimeReceiptOwnerSnapshot crime = new P12ACrimeReceiptOwnerSnapshot(4L,
            Series("crime-op-", "crime-fingerprint-", 4));
        P12AJusticeReceiptOwnerSnapshot justice = new P12AJusticeReceiptOwnerSnapshot(
            3L, Series("justice-begin-", "begin-fingerprint-", 3),
            2L, Series("justice-advance-", "advance-fingerprint-", 2),
            1L, Series("justice-sync-", "sync-fingerprint-", 1));

        Assert.That(P12AReceiptOwnerCodec.TryEncode(crime, justice,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult encodeResult), Is.True,
            encodeResult?.SectionCode);
        Assert.That(sections.Select(section => section.Id), Is.EqualTo(new[]
        {
            P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId,
            P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId
        }));
        Assert.That(P12AReceiptOwnerCodec.TryDecode(sections,
            out P12ACrimeReceiptOwnerSnapshot restoredCrime,
            out P12AJusticeReceiptOwnerSnapshot restoredJustice,
            out P12AResult decodeResult), Is.True, decodeResult?.SectionCode);

        Assert.That(restoredCrime.StepRevision, Is.EqualTo(4L));
        Assert.That(restoredCrime.CensusRevision, Is.EqualTo(8L));
        Assert.That(restoredCrime.Receipts[0].Identity, Is.EqualTo("crime-op-0"));
        Assert.That(restoredCrime.Receipts[0].DescriptorFingerprint, Is.EqualTo("crime-fingerprint-0"));
        Assert.That(restoredJustice.CensusRevision, Is.EqualTo(12L));
        Assert.That(restoredJustice.BeginDayReceipts[0].Identity, Is.EqualTo("justice-begin-0"));
        Assert.That(restoredJustice.AdvanceSentencesReceipts[0].RevisionBefore, Is.EqualTo(0L));
        Assert.That(restoredJustice.SyncWantedStatusesReceipts[0].RevisionAfter, Is.EqualTo(1L));

        Assert.That(P12AReceiptOwnerCodec.TryEncode(restoredCrime, restoredJustice,
            out IReadOnlyList<P12AOwnerSection> reencoded, out _), Is.True);
        for (int section = 0; section < sections.Count; section++)
        {
            Assert.That(reencoded[section].Revision, Is.EqualTo(sections[section].Revision));
            Assert.That(reencoded[section].Rows[0].OwnerKey, Is.EqualTo(sections[section].Rows[0].OwnerKey));
            Assert.That(reencoded[section].Rows[0].Payload, Is.EqualTo(sections[section].Rows[0].Payload));
        }
    }

    [Test]
    public void EmptyOwners_AreRepresentedAsRequiredSingleOwnerRows()
    {
        var crime = new P12ACrimeReceiptOwnerSnapshot(0L, new P12AReceiptValue[0]);
        var justice = new P12AJusticeReceiptOwnerSnapshot(0L, new P12AReceiptValue[0],
            0L, new P12AReceiptValue[0], 0L, new P12AReceiptValue[0]);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(crime, justice,
            out IReadOnlyList<P12AOwnerSection> sections, out _), Is.True);
        Assert.That(sections.All(section => section.Role == OwnerSectionRole.Required), Is.True);
        Assert.That(sections.All(section => section.Rows.Count == 1), Is.True);
        Assert.That(P12AReceiptOwnerCodec.TryDecode(sections, out _, out _, out _), Is.True);
    }

    [Test]
    public void Encode_RejectsUnorderedDuplicateOrOverflowingReceiptHistory()
    {
        var unordered = new P12ACrimeReceiptOwnerSnapshot(4L, new[]
        {
            Receipt("z", "fingerprint-z", 0L), Receipt("a", "fingerprint-a", 1L)
        });
        var duplicate = new P12ACrimeReceiptOwnerSnapshot(4L, new[]
        {
            Receipt("same", "fingerprint-1", 0L), Receipt("same", "fingerprint-2", 1L)
        });
        var duplicateTransition = new P12ACrimeReceiptOwnerSnapshot(2L, new[]
        {
            Receipt("a", "fingerprint-1", 0L), Receipt("b", "fingerprint-2", 0L)
        });
        var incompleteHistory = new P12ACrimeReceiptOwnerSnapshot(4L, new[]
        {
            Receipt("a", "fingerprint-1", 0L), Receipt("b", "fingerprint-2", 2L)
        });
        var overflow = new P12ACrimeReceiptOwnerSnapshot(0L, new[]
        {
            Receipt("overflow", "fingerprint", long.MaxValue)
        });
        var emptyJustice = new P12AJusticeReceiptOwnerSnapshot(0L, new P12AReceiptValue[0],
            0L, new P12AReceiptValue[0], 0L, new P12AReceiptValue[0]);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(unordered, emptyJustice, out _, out _), Is.False);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(duplicate, emptyJustice, out _, out _), Is.False);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(duplicateTransition, emptyJustice, out _, out _), Is.False);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(incompleteHistory, emptyJustice, out _, out _), Is.False);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(overflow, emptyJustice, out _, out _), Is.False);
    }

    [Test]
    public void Encode_ConvertsOversizedReceiptFieldsIntoStructuredFailure()
    {
        var crime = new P12ACrimeReceiptOwnerSnapshot(1L, new[]
        {
            Receipt(new string('x', P12AContinuationCodec.MaxStringBytes + 1), "fingerprint", 0L)
        });
        var justice = new P12AJusticeReceiptOwnerSnapshot(0L, new P12AReceiptValue[0],
            0L, new P12AReceiptValue[0], 0L, new P12AReceiptValue[0]);

        Assert.That(P12AReceiptOwnerCodec.TryEncode(crime, justice,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult result), Is.False);
        Assert.That(sections, Is.Null);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void Decode_RejectsDuplicateAndMissingSections()
    {
        var crime = new P12ACrimeReceiptOwnerSnapshot(0L, new P12AReceiptValue[0]);
        var justice = new P12AJusticeReceiptOwnerSnapshot(0L, new P12AReceiptValue[0],
            0L, new P12AReceiptValue[0], 0L, new P12AReceiptValue[0]);
        Assert.That(P12AReceiptOwnerCodec.TryEncode(crime, justice,
            out IReadOnlyList<P12AOwnerSection> sections, out _), Is.True);
        Assert.That(P12AReceiptOwnerCodec.TryDecode(new[] { sections[0], sections[0] },
            out _, out _, out _), Is.False);
        Assert.That(P12AReceiptOwnerCodec.TryDecode(new[] { sections[0] }, out _, out _, out _), Is.False);
    }

    private static P12AReceiptValue Receipt(string identity, string fingerprint, long revisionBefore) =>
        new P12AReceiptValue(identity, fingerprint, revisionBefore, revisionBefore + 1L);

    private static IReadOnlyList<P12AReceiptValue> Series(string identityPrefix,
        string fingerprintPrefix, int count)
    {
        List<P12AReceiptValue> values = new List<P12AReceiptValue>(count);
        for (int revision = 0; revision < count; revision++)
            values.Add(Receipt(identityPrefix + revision, fingerprintPrefix + revision, revision));
        return values;
    }
}
