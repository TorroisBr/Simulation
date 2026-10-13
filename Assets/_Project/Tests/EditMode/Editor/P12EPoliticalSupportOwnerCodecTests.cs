using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

public sealed class P12EPoliticalSupportOwnerCodecTests
{
    [Test]
    public void PoliticalSupportCodecPreservesTypedEndpointsTimelineAndStableOrder()
    {
        P12EPoliticalSupportOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EPoliticalSupportOwnerCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True,
            encodeError?.Code.ToString());
        Assert.That(encoded.Id, Is.EqualTo(PoliticalSupportStoreCensusProvider.RelationsSectionId));
        Assert.That(encoded.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(encoded.Revision, Is.EqualTo(7L));
        Assert.That(encoded.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "relation/relation.person-claim-old",
            "relation/relation.person-claim-current",
            "relation/relation.person-candidate",
            "relation/relation.faction-claim"
        }));

        Assert.That(P12EPoliticalSupportOwnerCodec.TryDecode(encoded, 2L,
            out P12EPoliticalSupportOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.CapturedAbsoluteDay, Is.EqualTo(2L));
        Assert.That(decoded.Relations.RecordCount, Is.EqualTo(4));
        Assert.That(decoded.Relations.Revision, Is.EqualTo(7L));

        P12EPoliticalSupportSnapshotRow ended = decoded.Relations.Records[0];
        Assert.That(ended.SourceKind, Is.EqualTo((int)PoliticalSupportSourceKind.Person));
        Assert.That(ended.SourceId, Is.EqualTo("person.a"));
        Assert.That(ended.TargetKind, Is.EqualTo((int)PoliticalSupportTargetKind.PoliticalClaim));
        Assert.That(ended.TargetId, Is.EqualTo("claim.a"));
        Assert.That(ended.Disposition, Is.EqualTo((int)PoliticalSupportDisposition.Support));
        Assert.That(ended.StartedAbsoluteDay, Is.Zero);
        Assert.That(ended.EndedAbsoluteDay, Is.EqualTo(1L));
        Assert.That(decoded.Relations.Records[1].EndedAbsoluteDay, Is.Null);
        Assert.That(decoded.Relations.Records[1].Disposition, Is.EqualTo((int)PoliticalSupportDisposition.Oppose));
        Assert.That(decoded.Relations.Records[2].TargetKind,
            Is.EqualTo((int)PoliticalSupportTargetKind.SuccessionCandidate));
        Assert.That(decoded.Relations.Records[2].TargetId, Is.EqualTo("person.c"));
        Assert.That(decoded.Relations.Records[3].SourceKind, Is.EqualTo((int)PoliticalSupportSourceKind.Faction));
        Assert.That(decoded.Relations.Records[3].SourceId, Is.EqualTo("faction.a"));

        Assert.That(P12EPoliticalSupportOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows.Select(row => Convert.ToBase64String(row.Payload)),
            Is.EqualTo(encoded.Rows.Select(row => Convert.ToBase64String(row.Payload))));
    }

    [Test]
    public void PoliticalSupportCodecPreservesExactEmptyOwner()
    {
        P12EPoliticalSupportOwnerSnapshot empty = new P12EPoliticalSupportOwnerSnapshot(0L,
            new P12EPoliticalSupportSnapshotSection(PoliticalSupportStoreCensusProvider.RelationsSectionId,
                PoliticalSupportStoreCensusProvider.SchemaVersion, 0, 0L,
                Array.Empty<P12EPoliticalSupportSnapshotRow>()));
        Assert.That(P12EPoliticalSupportOwnerCodec.TryEncode(empty,
            out P12AOwnerSection encoded, out _), Is.True);
        Assert.That(encoded.Rows, Is.Empty);
        Assert.That(P12EPoliticalSupportOwnerCodec.TryDecode(encoded, 0L,
            out P12EPoliticalSupportOwnerSnapshot decoded, out _), Is.True);
        Assert.That(decoded.Relations.RecordCount, Is.Zero);
        Assert.That(decoded.Relations.Revision, Is.Zero);
    }

    [Test]
    public void PoliticalSupportCodecRejectsImpossibleRevisionCardinalityPairs()
    {
        P12EPoliticalSupportOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EPoliticalSupportOwnerCodec.TryEncode(source,
            out P12AOwnerSection populated, out _), Is.True);
        Assert.That(P12EPoliticalSupportOwnerCodec.TryDecode(
            CopySection(populated, populated.Rows.Count - 1, populated.Rows), 2L, out _, out _), Is.False);

        P12EPoliticalSupportOwnerSnapshot empty = new P12EPoliticalSupportOwnerSnapshot(0L,
            new P12EPoliticalSupportSnapshotSection(PoliticalSupportStoreCensusProvider.RelationsSectionId,
                PoliticalSupportStoreCensusProvider.SchemaVersion, 0, 0L,
                Array.Empty<P12EPoliticalSupportSnapshotRow>()));
        Assert.That(P12EPoliticalSupportOwnerCodec.TryEncode(empty,
            out P12AOwnerSection emptySection, out _), Is.True);
        Assert.That(P12EPoliticalSupportOwnerCodec.TryDecode(
            CopySection(emptySection, 1L, emptySection.Rows), 0L, out _, out _), Is.False);
    }

    [Test]
    public void PoliticalSupportCodecRejectsUnknownEnumMismatchedKeyAndOversizedCount()
    {
        P12EPoliticalSupportOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EPoliticalSupportOwnerCodec.TryEncode(source,
            out P12AOwnerSection valid, out _), Is.True);

        byte[] invalidEnumPayload = valid.Rows[0].Payload;
        byte[] contract = Encoding.UTF8.GetBytes("p12a.political-support/v1");
        byte[] relationId = Encoding.UTF8.GetBytes("relation.person-claim-old");
        int enumOffset = sizeof(int) + contract.Length + sizeof(int) + relationId.Length;
        invalidEnumPayload[enumOffset] = 0xff;
        invalidEnumPayload[enumOffset + 1] = 0xff;
        List<P12AOwnerRow> invalidEnumRows = new List<P12AOwnerRow>(valid.Rows)
        { [0] = new P12AOwnerRow(valid.Rows[0].OwnerKey, 0L, invalidEnumPayload) };
        Assert.That(P12EPoliticalSupportOwnerCodec.TryDecode(CopySection(valid, valid.Revision, invalidEnumRows),
            2L, out _, out _), Is.False);

        List<P12AOwnerRow> invalidKeyRows = new List<P12AOwnerRow>(valid.Rows)
        { [0] = new P12AOwnerRow("relation/other", 0L, valid.Rows[0].Payload) };
        Assert.That(P12EPoliticalSupportOwnerCodec.TryDecode(CopySection(valid, valid.Revision, invalidKeyRows),
            2L, out _, out _), Is.False);

        int oversizedCount = checked(P12AContinuationCodec.MaxRows + 1);
        P12EPoliticalSupportOwnerSnapshot oversized = new P12EPoliticalSupportOwnerSnapshot(0L,
            new P12EPoliticalSupportSnapshotSection(PoliticalSupportStoreCensusProvider.RelationsSectionId,
                PoliticalSupportStoreCensusProvider.SchemaVersion, oversizedCount, oversizedCount,
                Array.Empty<P12EPoliticalSupportSnapshotRow>()));
        Assert.That(P12EPoliticalSupportOwnerCodec.TryEncode(oversized,
            out P12AOwnerSection oversizedSection, out P12AResult limitResult), Is.False);
        Assert.That(oversizedSection, Is.Null);
        Assert.That(limitResult.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    private static P12AOwnerSection CopySection(P12AOwnerSection source, long revision,
        IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            source.AliasGroup, revision, rows);

    private static P12EPoliticalSupportOwnerSnapshot CreateSnapshot()
    {
        P12EPoliticalSupportSnapshotRow[] rows =
        {
            new P12EPoliticalSupportSnapshotRow("relation.person-claim-old",
                (int)PoliticalSupportSourceKind.Person, "person.a",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "claim.a",
                (int)PoliticalSupportDisposition.Support, 0L, 1L),
            new P12EPoliticalSupportSnapshotRow("relation.person-claim-current",
                (int)PoliticalSupportSourceKind.Person, "person.a",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "claim.a",
                (int)PoliticalSupportDisposition.Oppose, 1L, null),
            new P12EPoliticalSupportSnapshotRow("relation.person-candidate",
                (int)PoliticalSupportSourceKind.Person, "person.a",
                (int)PoliticalSupportTargetKind.SuccessionCandidate, "person.c",
                (int)PoliticalSupportDisposition.Support, 1L, null),
            new P12EPoliticalSupportSnapshotRow("relation.faction-claim",
                (int)PoliticalSupportSourceKind.Faction, "faction.a",
                (int)PoliticalSupportTargetKind.PoliticalClaim, "claim.a",
                (int)PoliticalSupportDisposition.Oppose, 2L, 2L)
        };
        return new P12EPoliticalSupportOwnerSnapshot(2L,
            new P12EPoliticalSupportSnapshotSection(PoliticalSupportStoreCensusProvider.RelationsSectionId,
                PoliticalSupportStoreCensusProvider.SchemaVersion, rows.Length, 7L, rows));
    }
}
