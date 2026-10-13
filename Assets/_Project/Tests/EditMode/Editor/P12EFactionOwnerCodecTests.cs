using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EFactionOwnerCodecTests
{
    [Test]
    public void FactionCodecRoundTripsPoliciesHistoryAndActiveMemberships()
    {
        P12EFactionOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EFactionOwnerCodec.TryEncode(source, 10L,
            out P12AOwnerSection factions, out P12AOwnerSection affiliations,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(factions.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(factions.Id)));
        Assert.That(affiliations.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(affiliations.Id)));
        Assert.That(factions.Rows.Select(row => row.OwnerKey).ToArray(),
            Is.EqualTo(new[] { "faction/faction-a", "faction/faction-z" }));
        Assert.That(affiliations.Rows.Select(row => row.OwnerKey).ToArray(),
            Is.EqualTo(new[] { "affiliation/aff-1", "affiliation/aff-2", "affiliation/aff-3" }));

        Assert.That(P12EFactionOwnerCodec.TryDecode(factions, affiliations, 10L,
            out P12EFactionOwnerSnapshot decoded, out P12AResult decodeError),
            Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.Factions.Records.Select(row => row.Id).ToArray(),
            Is.EqualTo(new[] { "faction-a", "faction-z" }));
        Assert.That(decoded.Factions.Records[0].DisplayName, Is.EqualTo("Assembly"));
        Assert.That(decoded.Factions.Records[0].MembershipPolicy,
            Is.EqualTo((int)FactionMembershipPolicy.LeaveAndRejoin));
        Assert.That(decoded.Factions.Records[1].ExpulsionAllowed, Is.False);
        Assert.That(decoded.Factions.Records[1].DisplayName, Is.Empty);
        Assert.That(decoded.Affiliations.Records[0].EndedAbsoluteDay, Is.EqualTo(2L));
        Assert.That(decoded.Affiliations.Records[0].EndReason,
            Is.EqualTo((int)FactionAffiliationEndReason.VoluntaryLeave));
        Assert.That(decoded.Affiliations.Records[0].IsActive, Is.False);
        Assert.That(decoded.Affiliations.Records[1].EndedAbsoluteDay, Is.Null);
        Assert.That(decoded.Affiliations.Records[1].EndReason, Is.Null);
        Assert.That(decoded.Affiliations.Records[1].IsActive, Is.True);

        Assert.That(P12EFactionOwnerCodec.TryEncode(decoded, 10L,
            out P12AOwnerSection factionsAgain, out P12AOwnerSection affiliationsAgain, out _), Is.True);
        AssertRowsEqual(factions, factionsAgain);
        AssertRowsEqual(affiliations, affiliationsAgain);

        PersonStore stagedPersons = new PersonStore();
        Assert.That(stagedPersons.TryRegister(new PersonRuntime(new PersonId("person-x")), out _), Is.True);
        Assert.That(stagedPersons.TryRegister(new PersonRuntime(new PersonId("person-y")), out _), Is.True);
        Assert.That(decoded.TryCreateStagedOwner(stagedPersons, 10L,
            out FactionStore staged, out P12EFactionSnapshotFailure stageFailure), Is.True, stageFailure?.Code.ToString());
        Assert.That(staged.Count, Is.EqualTo(2));
        Assert.That(staged.AffiliationCount, Is.EqualTo(3));
        Assert.That(staged.Revision, Is.EqualTo(5L));
    }

    [Test]
    public void FactionCodecPreservesTwoExactEmptyRequiredSections()
    {
        P12EFactionOwnerSnapshot empty = new P12EFactionOwnerSnapshot(
            Section(FactionStoreCensusProvider.FactionsSectionId, 0L,
                Array.Empty<P12EFactionSnapshotRecord>(), row => row?.Copy()),
            Section(FactionStoreCensusProvider.AffiliationsSectionId, 0L,
                Array.Empty<P12EFactionAffiliationSnapshotRecord>(), row => row?.Copy()));
        Assert.That(P12EFactionOwnerCodec.TryEncode(empty, 0L,
            out P12AOwnerSection factions, out P12AOwnerSection affiliations, out _), Is.True);
        Assert.That(factions.Rows, Is.Empty);
        Assert.That(affiliations.Rows, Is.Empty);
        Assert.That(P12EFactionOwnerCodec.TryDecode(factions, affiliations, 0L,
            out P12EFactionOwnerSnapshot decoded, out _), Is.True);
        Assert.That(decoded.Factions.Revision, Is.Zero);
        Assert.That(decoded.Affiliations.Revision, Is.Zero);
    }

    [Test]
    public void FactionCodecRejectsUnreachableRevisionBadKeyInvalidBooleanAndTemporalValues()
    {
        Assert.That(P12EFactionOwnerCodec.TryEncode(CreateSnapshot(), 10L,
            out P12AOwnerSection factions, out P12AOwnerSection affiliations, out _), Is.True);

        Assert.That(P12EFactionOwnerCodec.TryDecode(
            CopySection(factions, 0L, factions.Rows), CopySection(affiliations, 0L, affiliations.Rows),
            10L, out _, out _), Is.False);

        List<P12AOwnerRow> badFactionKey = new List<P12AOwnerRow>(factions.Rows)
        { [0] = new P12AOwnerRow("faction/not-the-row", 0L, factions.Rows[0].Payload) };
        Assert.That(P12EFactionOwnerCodec.TryDecode(
            CopySection(factions, factions.Revision, badFactionKey), affiliations, 10L, out _, out _), Is.False);

        List<P12AOwnerRow> badBooleanRows = new List<P12AOwnerRow>(factions.Rows);
        byte[] badBoolean = factions.Rows[0].Payload;
        badBoolean[badBoolean.Length - 1] = 2;
        badBooleanRows[0] = new P12AOwnerRow(factions.Rows[0].OwnerKey, 0L, badBoolean);
        Assert.That(P12EFactionOwnerCodec.TryDecode(
            CopySection(factions, factions.Revision, badBooleanRows), affiliations, 10L, out _, out _), Is.False);

        Assert.That(P12EFactionOwnerCodec.TryEncode(CreateSnapshot(), 2L,
            out _, out _, out _), Is.False);
    }

    [Test]
    public void FactionStagingRejectsNullNameWithoutNormalizingIt()
    {
        P12EFactionOwnerSnapshot malformed = new P12EFactionOwnerSnapshot(
            Section(FactionStoreCensusProvider.FactionsSectionId, 1L,
                new[] { new P12EFactionSnapshotRecord("faction-a", null, 0L,
                    (int)FactionMembershipPolicy.LeaveAndRejoin, true) }, row => row?.Copy()),
            Section(FactionStoreCensusProvider.AffiliationsSectionId, 1L,
                Array.Empty<P12EFactionAffiliationSnapshotRecord>(), row => row?.Copy()));
        Assert.That(P12EFactionOwnerCodec.TryEncode(malformed, 0L, out _, out _, out _), Is.False);
        Assert.That(malformed.TryCreateStagedOwner(new PersonStore(), 0L, out _, out _), Is.False);
    }

    private static P12EFactionOwnerSnapshot CreateSnapshot()
        => new P12EFactionOwnerSnapshot(
            Section(FactionStoreCensusProvider.FactionsSectionId, 5L,
                new[]
                {
                    new P12EFactionSnapshotRecord("faction-z", string.Empty, 0L,
                        (int)FactionMembershipPolicy.LeaveNoRejoin, false),
                    new P12EFactionSnapshotRecord("faction-a", "Assembly", 0L,
                        (int)FactionMembershipPolicy.LeaveAndRejoin, true)
                }, row => row?.Copy()),
            Section(FactionStoreCensusProvider.AffiliationsSectionId, 5L,
                new[]
                {
                    new P12EFactionAffiliationSnapshotRecord("aff-3", "faction-z", "person-y",
                        2L, null, null, true),
                    new P12EFactionAffiliationSnapshotRecord("aff-2", "faction-a", "person-x",
                        3L, null, null, true),
                    new P12EFactionAffiliationSnapshotRecord("aff-1", "faction-a", "person-x",
                        1L, 2L, (int)FactionAffiliationEndReason.VoluntaryLeave, false)
                }, row => row?.Copy()));

    private static P12EFactionSnapshotSection<T> Section<T>(string id, long revision,
        IEnumerable<T> rows, Func<T, T> copy)
    {
        List<T> values = rows.ToList();
        return new P12EFactionSnapshotSection<T>(id, P12EFactionOwnerSnapshot.CurrentSchemaVersion,
            values.Count, revision, values, copy);
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
