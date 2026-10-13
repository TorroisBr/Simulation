using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

public sealed class P12EPoliticalClaimOwnerCodecTests
{
    [Test]
    public void PoliticalClaimCodecRejectsPrefixedClaimAndRecognitionKeysBeyondWireStringLimit()
    {
        string oversizedClaimId = new string('c', P12AContinuationCodec.MaxStringBytes);
        P12EPoliticalClaimOwnerSnapshot claimKey = new P12EPoliticalClaimOwnerSnapshot(
            Section(PoliticalClaimStoreCensusProvider.ClaimsSectionId, 1L,
                new[] { new P12EPoliticalClaimRow(oversizedClaimId, "person", (int)PoliticalClaimType.OfficeEntitlement,
                    (int)PoliticalClaimTargetKind.Office, "office", (int)PoliticalClaimBasis.OfficeIncumbency,
                    string.Empty, 0L, (int)PoliticalClaimStatus.Active, null, Array.Empty<string>()) }, row => row?.Copy()),
            Section(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 1L,
                Array.Empty<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy()));
        Assert.That(P12EPoliticalClaimOwnerCodec.TryEncode(claimKey, 0L,
            out _, out _, out P12AResult claimError), Is.False);
        Assert.That(claimError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));

        const int halfIdentifierLength = (P12AContinuationCodec.MaxStringBytes - 14) / 2;
        string claimId = new string('c', halfIdentifierLength);
        string institutionId = new string('i', halfIdentifierLength);
        string recognitionId = PoliticalClaimRecognitionRecord.BuildRecognitionId(
            new PoliticalClaimId(claimId), new InstitutionId(institutionId));
        Assert.That(P12AWire.GetEncodedStringByteCount(recognitionId),
            Is.EqualTo(P12AContinuationCodec.MaxStringBytes));
        P12EPoliticalClaimOwnerSnapshot recognitionKey = new P12EPoliticalClaimOwnerSnapshot(
            Section(PoliticalClaimStoreCensusProvider.ClaimsSectionId, 2L,
                new[] { new P12EPoliticalClaimRow(claimId, "person", (int)PoliticalClaimType.OfficeEntitlement,
                    (int)PoliticalClaimTargetKind.Office, "office", (int)PoliticalClaimBasis.OfficeIncumbency,
                    string.Empty, 0L, (int)PoliticalClaimStatus.Active, null, Array.Empty<string>()) }, row => row?.Copy()),
            Section(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 2L,
                new[] { new P12EPoliticalClaimRecognitionRow(claimId, institutionId, recognitionId,
                    (int)PoliticalClaimRecognitionState.Recognized, 0L, string.Empty,
                    new[] { new P12EPoliticalClaimRecognitionHistoryRow(
                        (int)PoliticalClaimRecognitionState.Recognized, 0L, string.Empty) }) }, row => row?.Copy()));
        Assert.That(P12EPoliticalClaimOwnerCodec.TryEncode(recognitionKey, 0L,
            out _, out _, out P12AResult recognitionError), Is.False);
        Assert.That(recognitionError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void PoliticalClaimCodecPreservesTypedValuesRecognitionHistoryAndStableOrder()
    {
        P12EPoliticalClaimOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EPoliticalClaimOwnerCodec.TryEncode(source, 3L,
            out P12AOwnerSection claims, out P12AOwnerSection recognitions,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(claims.Id, Is.EqualTo(PoliticalClaimStoreCensusProvider.ClaimsSectionId));
        Assert.That(recognitions.Id, Is.EqualTo(PoliticalClaimStoreCensusProvider.RecognitionsSectionId));
        Assert.That(claims.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(claims.Id)));
        Assert.That(recognitions.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(recognitions.Id)));
        Assert.That(claims.Revision, Is.EqualTo(7L));
        Assert.That(recognitions.Revision, Is.EqualTo(7L));
        Assert.That(claims.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { "claim/claim-a", "claim/claim-b" }));
        Assert.That(recognitions.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "recognition/7:claim-a13:institution-a",
            "recognition/7:claim-b13:institution-a"
        }));

        Assert.That(P12EPoliticalClaimOwnerCodec.TryDecode(claims, recognitions, 3L,
            out P12EPoliticalClaimOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.Claims.Records[0].ClaimId, Is.EqualTo("claim-a"));
        Assert.That(decoded.Claims.Records[0].TargetKind, Is.EqualTo((int)PoliticalClaimTargetKind.Office));
        Assert.That(decoded.Claims.Records[0].TargetId, Is.EqualTo("office-a"));
        Assert.That(decoded.Claims.Records[0].ResolutionAbsoluteDay, Is.Null);
        Assert.That(decoded.Claims.Records[0].EvidenceReferences, Is.Empty);
        Assert.That(decoded.Claims.Records[1].TargetKind, Is.EqualTo((int)PoliticalClaimTargetKind.Property));
        Assert.That(decoded.Claims.Records[1].ResolutionAbsoluteDay, Is.EqualTo(2L));
        Assert.That(decoded.Claims.Records[1].EvidenceReferences, Is.EqualTo(new[] { "evidence-a", "evidence-z" }));
        Assert.That(decoded.Recognitions.Records[0].State, Is.EqualTo((int)PoliticalClaimRecognitionState.Recognized));
        Assert.That(decoded.Recognitions.Records[0].History.Select(row => row.State), Is.EqualTo(new[]
        {
            (int)PoliticalClaimRecognitionState.Unrecognized,
            (int)PoliticalClaimRecognitionState.Recognized
        }));
        Assert.That(decoded.Recognitions.Records[0].History[0].Reason, Is.EqualTo("initial"));
        Assert.That(decoded.Recognitions.Records[1].History.Single().Reason, Is.EqualTo("review"));

        Assert.That(P12EPoliticalClaimOwnerCodec.TryEncode(decoded, 3L,
            out P12AOwnerSection claimsAgain, out P12AOwnerSection recognitionsAgain, out _), Is.True);
        Assert.That(claimsAgain.Rows.Select(row => Convert.ToBase64String(row.Payload)),
            Is.EqualTo(claims.Rows.Select(row => Convert.ToBase64String(row.Payload))));
        Assert.That(recognitionsAgain.Rows.Select(row => Convert.ToBase64String(row.Payload)),
            Is.EqualTo(recognitions.Rows.Select(row => Convert.ToBase64String(row.Payload))));
    }

    [Test]
    public void PoliticalClaimCodecPreservesExactEmptyOwnerPair()
    {
        P12EPoliticalClaimOwnerSnapshot empty = new P12EPoliticalClaimOwnerSnapshot(
            Section(PoliticalClaimStoreCensusProvider.ClaimsSectionId, 0L,
                Array.Empty<P12EPoliticalClaimRow>(), row => row?.Copy()),
            Section(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 0L,
                Array.Empty<P12EPoliticalClaimRecognitionRow>(), row => row?.Copy()));
        Assert.That(P12EPoliticalClaimOwnerCodec.TryEncode(empty, 0L,
            out P12AOwnerSection claims, out P12AOwnerSection recognitions, out _), Is.True);
        Assert.That(claims.Rows, Is.Empty);
        Assert.That(recognitions.Rows, Is.Empty);
        Assert.That(P12EPoliticalClaimOwnerCodec.TryDecode(claims, recognitions, 0L,
            out P12EPoliticalClaimOwnerSnapshot decoded, out _), Is.True);
        Assert.That(decoded.Claims.RecordCount, Is.Zero);
        Assert.That(decoded.Recognitions.RecordCount, Is.Zero);
        Assert.That(decoded.Claims.Revision, Is.Zero);
        Assert.That(P12EPoliticalClaimOwnerCodec.TryDecode(
            CopySection(claims, 1L, claims.Rows), CopySection(recognitions, 1L, recognitions.Rows),
            0L, out _, out _), Is.False);
    }

    [Test]
    public void PoliticalClaimCodecRejectsUnknownEnumWrongOwnerKeyAndImpossibleRevision()
    {
        Assert.That(P12EPoliticalClaimOwnerCodec.TryEncode(CreateSnapshot(), 4L,
            out P12AOwnerSection claims, out P12AOwnerSection recognitions, out _), Is.True);
        byte[] unknownEnumPayload = claims.Rows[0].Payload;
        byte[] contract = Encoding.UTF8.GetBytes("p12a.political-claims/v1");
        byte[] claimId = Encoding.UTF8.GetBytes("claim-a");
        byte[] claimantId = Encoding.UTF8.GetBytes("person-a");
        int claimTypeOffset = sizeof(int) + contract.Length + sizeof(int) + claimId.Length
            + sizeof(int) + claimantId.Length;
        unknownEnumPayload[claimTypeOffset] = 0xff;
        unknownEnumPayload[claimTypeOffset + 1] = 0xff;
        List<P12AOwnerRow> invalidRows = new List<P12AOwnerRow>(claims.Rows)
        { [0] = new P12AOwnerRow(claims.Rows[0].OwnerKey, 0L, unknownEnumPayload) };
        Assert.That(P12EPoliticalClaimOwnerCodec.TryDecode(
            CopySection(claims, claims.Revision, invalidRows), recognitions, 3L, out _, out _), Is.False);

        invalidRows = new List<P12AOwnerRow>(claims.Rows)
        { [0] = new P12AOwnerRow("claim/other", 0L, claims.Rows[0].Payload) };
        Assert.That(P12EPoliticalClaimOwnerCodec.TryDecode(
            CopySection(claims, claims.Revision, invalidRows), recognitions, 3L, out _, out _), Is.False);

        Assert.That(P12EPoliticalClaimOwnerCodec.TryDecode(
            CopySection(claims, 3L, claims.Rows), recognitions, 4L, out _, out _), Is.False);

        Assert.That(P12EPoliticalClaimOwnerCodec.TryDecode(
            CopySection(claims, 3L, claims.Rows), CopySection(recognitions, 3L, recognitions.Rows),
            4L, out _, out _), Is.False);

        P12EPoliticalClaimOwnerSnapshot source = CreateSnapshot();
        P12EPoliticalClaimRecognitionRow sourceRecognition = source.Recognitions.Records[0];
        P12EPoliticalClaimRecognitionRow invalidHistory = new P12EPoliticalClaimRecognitionRow(
            sourceRecognition.ClaimId, sourceRecognition.InstitutionId, sourceRecognition.RecognitionId,
            sourceRecognition.State, sourceRecognition.RecognitionAbsoluteDay, sourceRecognition.Reason,
            new P12EPoliticalClaimRecognitionHistoryRow[] { null, sourceRecognition.History[1].Copy() });
        P12EPoliticalClaimOwnerSnapshot invalidSnapshot = new P12EPoliticalClaimOwnerSnapshot(
            source.Claims,
            Section(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 7L,
                new[] { invalidHistory, source.Recognitions.Records[1].Copy() }, row => row?.Copy()));
        Assert.That(P12EPoliticalClaimOwnerCodec.TryEncode(invalidSnapshot, 3L,
            out _, out _, out _), Is.False);
    }

    private static P12AOwnerSection CopySection(P12AOwnerSection source, long revision,
        IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            source.AliasGroup, revision, rows);

    private static P12EPoliticalClaimSnapshotSection<T> Section<T>(string id, long revision,
        IEnumerable<T> rows, Func<T, T> copy)
    {
        List<T> values = rows.ToList();
        return new P12EPoliticalClaimSnapshotSection<T>(id,
            P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion, values.Count, revision, values, copy);
    }

    private static P12EPoliticalClaimOwnerSnapshot CreateSnapshot()
    {
        P12EPoliticalClaimRow[] claims =
        {
            new P12EPoliticalClaimRow("claim-a", "person-a", (int)PoliticalClaimType.OfficeEntitlement,
                (int)PoliticalClaimTargetKind.Office, "office-a", (int)PoliticalClaimBasis.OfficeIncumbency,
                "incumbent", 0L, (int)PoliticalClaimStatus.Active, null, Array.Empty<string>()),
            new P12EPoliticalClaimRow("claim-b", "person-b", (int)PoliticalClaimType.PropertyEntitlement,
                (int)PoliticalClaimTargetKind.Property, "property-b", (int)PoliticalClaimBasis.PropertyOwnership,
                "owner", 1L, (int)PoliticalClaimStatus.Withdrawn, 2L, new[] { "evidence-a", "evidence-z" })
        };
        string recognitionA = PoliticalClaimRecognitionRecord.BuildRecognitionId(
            new PoliticalClaimId("claim-a"), new InstitutionId("institution-a"));
        string recognitionB = PoliticalClaimRecognitionRecord.BuildRecognitionId(
            new PoliticalClaimId("claim-b"), new InstitutionId("institution-a"));
        P12EPoliticalClaimRecognitionRow[] recognitions =
        {
            new P12EPoliticalClaimRecognitionRow("claim-a", "institution-a", recognitionA,
                (int)PoliticalClaimRecognitionState.Recognized, 1L, "recognized",
                new[]
                {
                    new P12EPoliticalClaimRecognitionHistoryRow(
                        (int)PoliticalClaimRecognitionState.Unrecognized, 0L, "initial"),
                    new P12EPoliticalClaimRecognitionHistoryRow(
                        (int)PoliticalClaimRecognitionState.Recognized, 1L, "recognized")
                }),
            new P12EPoliticalClaimRecognitionRow("claim-b", "institution-a", recognitionB,
                (int)PoliticalClaimRecognitionState.Contested, 2L, "review",
                new[]
                {
                    new P12EPoliticalClaimRecognitionHistoryRow(
                        (int)PoliticalClaimRecognitionState.Contested, 2L, "review")
                })
        };
        return new P12EPoliticalClaimOwnerSnapshot(
            Section(PoliticalClaimStoreCensusProvider.ClaimsSectionId, 7L, claims, row => row?.Copy()),
            Section(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 7L, recognitions, row => row?.Copy()));
    }
}
