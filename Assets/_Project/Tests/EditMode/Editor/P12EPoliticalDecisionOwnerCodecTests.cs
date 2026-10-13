using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

public sealed class P12EPoliticalDecisionOwnerCodecTests
{
    [Test]
    public void PoliticalDecisionCodecPreservesTypedHistoryAndProducesStableBytes()
    {
        P12EPoliticalDecisionOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EPoliticalDecisionOwnerCodec.TryEncode(source,
            out P12AOwnerSection encoded, out P12AResult encodeError), Is.True,
            encodeError?.Code.ToString());
        Assert.That(encoded.Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(encoded.Revision, Is.EqualTo(4L));
        Assert.That(encoded.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "decision/decision.a", "decision/decision.b", "decision/decision.c", "decision/decision.d"
        }));

        Assert.That(P12EPoliticalDecisionOwnerCodec.TryDecode(encoded, 2L,
            out P12EPoliticalDecisionOwnerSnapshot decoded, out P12AResult decodeError), Is.True,
            decodeError?.Code.ToString());
        Assert.That(decoded.CapturedAbsoluteDay, Is.EqualTo(2L));
        Assert.That(decoded.Decisions.RecordCount, Is.EqualTo(4));
        Assert.That(decoded.Decisions.Revision, Is.EqualTo(4L));

        P12EPoliticalDecisionSnapshotRow selected = decoded.Decisions.Records[0];
        Assert.That(selected.DeciderKind, Is.EqualTo((int)PoliticalKnowledgeHolderKind.Person));
        Assert.That(selected.DeciderId, Is.EqualTo("person.decider"));
        Assert.That(selected.CandidatePersonIds, Is.EqualTo(new[] { "candidate.a", "candidate.z" }));
        Assert.That(selected.OutcomeKind, Is.EqualTo((int)PoliticalDecisionOutcomeKind.CandidateSelected));
        Assert.That(selected.SelectedCandidatePersonId, Is.EqualTo("candidate.a"));
        Assert.That(selected.EvidenceReferences, Is.EqualTo(new[] { "e.a", "e.z" }));
        Assert.That(selected.KnowledgeReferences, Is.EqualTo(new[] { "k.a", "k.z" }));
        Assert.That(selected.ExpectedWorldRevision, Is.EqualTo(2L));
        Assert.That(selected.ExpectedKnowledgeRevision, Is.EqualTo(4L));
        Assert.That(selected.OfficeId, Is.EqualTo("office.a"));
        Assert.That(selected.RecognizingInstitutionId, Is.Null);

        P12EPoliticalDecisionSnapshotRow claim = decoded.Decisions.Records[1];
        Assert.That(claim.DeciderKind, Is.EqualTo((int)PoliticalKnowledgeHolderKind.Faction));
        Assert.That(claim.OutcomeKind, Is.EqualTo((int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed));
        Assert.That(claim.ReferencedClaimId, Is.EqualTo("claim.a"));
        Assert.That(claim.RecognizingInstitutionId, Is.EqualTo("institution.a"));
        Assert.That(claim.OfficeId, Is.Null);

        P12EPoliticalDecisionSnapshotRow noSelection = decoded.Decisions.Records[2];
        Assert.That(noSelection.CandidatePersonIds, Is.EqualTo(new[] { "candidate.a" }));
        Assert.That(noSelection.EvidenceReferences, Is.Empty);
        Assert.That(noSelection.KnowledgeReferences, Is.Empty);
        Assert.That(noSelection.OutcomeKind, Is.EqualTo((int)PoliticalDecisionOutcomeKind.NoSelection));

        P12EPoliticalDecisionSnapshotRow rejected = decoded.Decisions.Records[3];
        Assert.That(rejected.CandidatePersonIds, Is.Empty);
        Assert.That(rejected.OutcomeKind, Is.EqualTo((int)PoliticalDecisionOutcomeKind.Rejected));
        Assert.That(rejected.SelectedCandidatePersonId, Is.Null);
        Assert.That(rejected.ReferencedClaimId, Is.Null);

        Assert.That(P12EPoliticalDecisionOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection encodedAgain, out _), Is.True);
        Assert.That(encodedAgain.Rows.Select(row => Convert.ToBase64String(row.Payload)),
            Is.EqualTo(encoded.Rows.Select(row => Convert.ToBase64String(row.Payload))));
    }

    [Test]
    public void PoliticalDecisionCodecPreservesExactEmptyOwner()
    {
        P12EPoliticalDecisionOwnerSnapshot empty = new P12EPoliticalDecisionOwnerSnapshot(0L,
            new P12EPoliticalDecisionSnapshotSection(PoliticalDecisionStoreCensusProvider.SectionId,
                PoliticalDecisionStoreCensusProvider.SchemaVersion, 0, 0L,
                Array.Empty<P12EPoliticalDecisionSnapshotRow>()));
        Assert.That(P12EPoliticalDecisionOwnerCodec.TryEncode(empty,
            out P12AOwnerSection encoded, out _), Is.True);
        Assert.That(encoded.Rows, Is.Empty);
        Assert.That(P12EPoliticalDecisionOwnerCodec.TryDecode(encoded, 0L,
            out P12EPoliticalDecisionOwnerSnapshot decoded, out _), Is.True);
        Assert.That(decoded.Decisions.RecordCount, Is.Zero);
        Assert.That(decoded.Decisions.Revision, Is.Zero);
    }

    [Test]
    public void PoliticalDecisionCodecRejectsRowCountAboveWireLimitBeforeAllocation()
    {
        int oversizedCount = checked(P12AContinuationCodec.MaxRows + 1);
        P12EPoliticalDecisionOwnerSnapshot oversized = new P12EPoliticalDecisionOwnerSnapshot(0L,
            new P12EPoliticalDecisionSnapshotSection(PoliticalDecisionStoreCensusProvider.SectionId,
                PoliticalDecisionStoreCensusProvider.SchemaVersion, oversizedCount, oversizedCount,
                Array.Empty<P12EPoliticalDecisionSnapshotRow>()));

        Assert.That(P12EPoliticalDecisionOwnerCodec.TryEncode(oversized,
            out P12AOwnerSection section, out P12AResult result), Is.False);
        Assert.That(section, Is.Null);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void PoliticalDecisionCodecRejectsUnknownEnumMismatchedOwnerKeyAndRevision()
    {
        P12EPoliticalDecisionOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EPoliticalDecisionOwnerCodec.TryEncode(source,
            out P12AOwnerSection valid, out _), Is.True);

        byte[] invalidEnumPayload = valid.Rows[0].Payload;
        byte[] contract = Encoding.UTF8.GetBytes("p12a.political-decisions/v1");
        byte[] decisionId = Encoding.UTF8.GetBytes("decision.a");
        int enumOffset = sizeof(int) + contract.Length + sizeof(int) + decisionId.Length;
        invalidEnumPayload[enumOffset] = 0xff;
        invalidEnumPayload[enumOffset + 1] = 0xff;
        List<P12AOwnerRow> invalidEnumRows = new List<P12AOwnerRow>(valid.Rows);
        invalidEnumRows[0] = new P12AOwnerRow(valid.Rows[0].OwnerKey, 0L, invalidEnumPayload);
        P12AOwnerSection badEnum = CopySection(valid, valid.Revision, invalidEnumRows);
        Assert.That(P12EPoliticalDecisionOwnerCodec.TryDecode(badEnum, 2L, out _, out _), Is.False);

        List<P12AOwnerRow> invalidKeyRows = new List<P12AOwnerRow>(valid.Rows);
        invalidKeyRows[0] = new P12AOwnerRow("decision/other", 0L, valid.Rows[0].Payload);
        P12AOwnerSection badOwnerKey = CopySection(valid, valid.Revision, invalidKeyRows);
        Assert.That(P12EPoliticalDecisionOwnerCodec.TryDecode(badOwnerKey, 2L, out _, out _), Is.False);

        P12AOwnerSection badRevision = CopySection(valid, valid.Revision + 1L, valid.Rows);
        Assert.That(P12EPoliticalDecisionOwnerCodec.TryDecode(badRevision, 2L, out _, out _), Is.False);
    }

    private static P12AOwnerSection CopySection(P12AOwnerSection source, long revision,
        IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            source.AliasGroup, revision, rows);

    private static P12EPoliticalDecisionOwnerSnapshot CreateSnapshot()
    {
        P12EPoliticalDecisionSnapshotRow[] rows =
        {
            new P12EPoliticalDecisionSnapshotRow("decision.a",
                (int)PoliticalKnowledgeHolderKind.Person, "person.decider",
                (int)PoliticalDecisionKind.SuccessionSelection,
                new[] { "candidate.a", "candidate.z" },
                (int)PoliticalDecisionOutcomeKind.CandidateSelected, "candidate.a", null,
                0L, 1L, new[] { "e.a", "e.z" }, new[] { "k.a", "k.z" },
                2L, 4L, "office.a", null),
            new P12EPoliticalDecisionSnapshotRow("decision.b",
                (int)PoliticalKnowledgeHolderKind.Faction, "faction.decider",
                (int)PoliticalDecisionKind.ClaimRecognitionProposal,
                Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.ClaimRecognitionProposed, null, "claim.a",
                1L, 1L, new[] { "claim.evidence" }, new[] { "claim.knowledge" },
                3L, 5L, null, "institution.a"),
            new P12EPoliticalDecisionSnapshotRow("decision.c",
                (int)PoliticalKnowledgeHolderKind.Institution, "institution.decider",
                (int)PoliticalDecisionKind.OfficeSelection,
                new[] { "candidate.a" },
                (int)PoliticalDecisionOutcomeKind.NoSelection, null, null,
                1L, 2L, Array.Empty<string>(), Array.Empty<string>(),
                4L, 6L, "office.b", null),
            new P12EPoliticalDecisionSnapshotRow("decision.d",
                (int)PoliticalKnowledgeHolderKind.Person, "person.decider",
                (int)PoliticalDecisionKind.Other,
                Array.Empty<string>(),
                (int)PoliticalDecisionOutcomeKind.Rejected, null, null,
                2L, 2L, Array.Empty<string>(), Array.Empty<string>(),
                5L, 7L, null, null)
        };
        return new P12EPoliticalDecisionOwnerSnapshot(2L,
            new P12EPoliticalDecisionSnapshotSection(PoliticalDecisionStoreCensusProvider.SectionId,
                PoliticalDecisionStoreCensusProvider.SchemaVersion, rows.Length, rows.Length, rows));
    }
}
