using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EInstitutionOfficeOwnerCodecTests
{
    [Test]
    public void InstitutionOfficeCodecRejectsPrefixedOwnerKeysBeyondWireStringLimit()
    {
        string oversizedKeyValue = new string('i', P12AContinuationCodec.MaxStringBytes);
        P12EInstitutionOfficeOwnerSnapshot institutionKey = new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, 1L,
                new[] { new P12EInstitutionSnapshotRecord(oversizedKeyValue, string.Empty) }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, 0L,
                Array.Empty<P12EOfficeSnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 0L,
                Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, 0L,
                Array.Empty<P12EOfficeTenureSnapshotRecord>(), row => row?.Copy()));
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(institutionKey,
            out _, out _, out _, out _, out P12AResult institutionError), Is.False);
        Assert.That(institutionError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));

        P12EInstitutionOfficeOwnerSnapshot officeKey = new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, 1L,
                new[] { new P12EInstitutionSnapshotRecord("institution", string.Empty) }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, 1L,
                new[] { new P12EOfficeSnapshotRecord(oversizedKeyValue, "institution", string.Empty) }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 1L,
                Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, 1L,
                Array.Empty<P12EOfficeTenureSnapshotRecord>(), row => row?.Copy()));
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(officeKey,
            out _, out _, out _, out _, out P12AResult officeError), Is.False);
        Assert.That(officeError.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void InstitutionOfficeCodecPreservesNullableValuesDuplicateTenuresAndMutationOrder()
    {
        P12EInstitutionOfficeOwnerSnapshot source = CreateSnapshot();
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(source,
            out P12AOwnerSection institutions, out P12AOwnerSection offices,
            out P12AOwnerSection incumbencies, out P12AOwnerSection tenures,
            out P12AResult encodeError), Is.True, encodeError?.Code.ToString());
        Assert.That(institutions.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(institutions.Id)));
        Assert.That(offices.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(offices.Id)));
        Assert.That(incumbencies.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(incumbencies.Id)));
        Assert.That(tenures.AliasGroup,
            Is.EqualTo(P12ADailyV1Census.ExpectedAliasGroupForSection(tenures.Id)));
        Assert.That(institutions.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "institution/institution-a", "institution/institution-b" }));
        Assert.That(offices.Rows.Select(row => row.OwnerKey),
            Is.EqualTo(new[] { "office/office-a", "office/office-b" }));
        Assert.That(incumbencies.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[] { "incumbency/office-a" }));
        Assert.That(tenures.Rows.Select(row => row.OwnerKey), Is.EqualTo(new[]
        {
            "tenure/0000000000", "tenure/0000000001", "tenure/0000000002"
        }));
        Assert.That(institutions.Revision, Is.EqualTo(2L));
        Assert.That(offices.Revision, Is.EqualTo(8L));
        Assert.That(incumbencies.Revision, Is.EqualTo(8L));
        Assert.That(tenures.Revision, Is.EqualTo(8L));

        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(institutions, offices,
            incumbencies, tenures, out P12EInstitutionOfficeOwnerSnapshot decoded,
            out P12AResult decodeError), Is.True, decodeError?.Code.ToString());
        Assert.That(decoded.Institutions.Records.Select(row => row.InstitutionIdValue),
            Is.EqualTo(new[] { "institution-a", "institution-b" }));
        Assert.That(decoded.Institutions.Records[0].DisplayName, Is.EqualTo("Council"));
        Assert.That(decoded.Offices.Records.Select(row => row.OfficeIdValue),
            Is.EqualTo(new[] { "office-a", "office-b" }));
        Assert.That(decoded.Incumbencies.Records.Single().StartAbsoluteDay, Is.EqualTo(4L));
        Assert.That(decoded.Tenures.Records[0].EndReason,
            Is.EqualTo((int)InstitutionalVacancyRecognitionReason.Resignation));
        Assert.That(decoded.Tenures.Records[1].StartAbsoluteDay, Is.EqualTo(4L));
        Assert.That(decoded.Tenures.Records[1].EndAbsoluteDay, Is.Null);
        Assert.That(decoded.Tenures.Records[1].EndReason, Is.Null);
        Assert.That(decoded.Tenures.Records[1].IsClosed, Is.False);
        Assert.That(decoded.Tenures.Records[0].OfficeIdValue, Is.EqualTo("office-b"));
        Assert.That(decoded.Tenures.Records[1].OfficeIdValue, Is.EqualTo("office-a"));
        Assert.That(decoded.Tenures.Records[2].OfficeIdValue, Is.EqualTo("office-b"));
        Assert.That(decoded.Tenures.Records[0].EndAbsoluteDay, Is.EqualTo(2L));
        Assert.That(decoded.Tenures.Records[2].EndAbsoluteDay, Is.EqualTo(2L));

        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(decoded,
            out P12AOwnerSection institutionsAgain, out P12AOwnerSection officesAgain,
            out P12AOwnerSection incumbenciesAgain, out P12AOwnerSection tenuresAgain, out _), Is.True);
        AssertRowsEqual(institutions, institutionsAgain);
        AssertRowsEqual(offices, officesAgain);
        AssertRowsEqual(incumbencies, incumbenciesAgain);
        AssertRowsEqual(tenures, tenuresAgain);
    }

    [Test]
    public void InstitutionOfficeCodecPreservesFourExactEmptyRequiredSections()
    {
        P12EInstitutionOfficeOwnerSnapshot empty = new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, 0L,
                Array.Empty<P12EInstitutionSnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, 0L,
                Array.Empty<P12EOfficeSnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 0L,
                Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, 0L,
                Array.Empty<P12EOfficeTenureSnapshotRecord>(), row => row?.Copy()));
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(empty,
            out P12AOwnerSection institutions, out P12AOwnerSection offices,
            out P12AOwnerSection incumbencies, out P12AOwnerSection tenures, out _), Is.True);
        Assert.That(institutions.Rows, Is.Empty);
        Assert.That(offices.Rows, Is.Empty);
        Assert.That(incumbencies.Rows, Is.Empty);
        Assert.That(tenures.Rows, Is.Empty);
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(institutions, offices,
            incumbencies, tenures, out P12EInstitutionOfficeOwnerSnapshot decoded, out _), Is.True);
        Assert.That(decoded.Institutions.Revision, Is.Zero);
        Assert.That(decoded.Offices.Revision, Is.Zero);
        Assert.That(decoded.Incumbencies.Revision, Is.Zero);
        Assert.That(decoded.Tenures.Revision, Is.Zero);
    }

    [Test]
    public void InstitutionOfficeCodecRejectsWrongSectionRevisionOwnerKeyAndNonCanonicalBoolean()
    {
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(CreateSnapshot(),
            out P12AOwnerSection institutions, out P12AOwnerSection offices,
            out P12AOwnerSection incumbencies, out P12AOwnerSection tenures, out _), Is.True);
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(institutions, offices,
            CopySection(incumbencies, incumbencies.Revision + 1L, incumbencies.Rows), tenures, out _, out _), Is.False);

        List<P12AOwnerRow> invalidOfficeRows = new List<P12AOwnerRow>(offices.Rows)
        { [0] = new P12AOwnerRow("office/other", 0L, offices.Rows[0].Payload) };
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(institutions,
            CopySection(offices, offices.Revision, invalidOfficeRows), incumbencies, tenures,
            out _, out _), Is.False);

        byte[] invalidBoolean = tenures.Rows[0].Payload;
        invalidBoolean[invalidBoolean.Length - 1] = 2;
        List<P12AOwnerRow> invalidTenureRows = new List<P12AOwnerRow>(tenures.Rows)
        { [0] = new P12AOwnerRow(tenures.Rows[0].OwnerKey, 0L, invalidBoolean) };
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(institutions, offices, incumbencies,
            CopySection(tenures, tenures.Revision, invalidTenureRows), out _, out _), Is.False);
    }

    [Test]
    public void InstitutionOfficeCodecRejectsOversizedSectionBeforeRowAllocation()
    {
        int oversized = checked(P12AContinuationCodec.MaxRows + 1);
        P12EInstitutionOfficeOwnerSnapshot invalid = new P12EInstitutionOfficeOwnerSnapshot(
            new P12EInstitutionOfficeSnapshotSection<P12EInstitutionSnapshotRecord>(
                InstitutionOfficeCensusProvider.InstitutionsSectionId,
                P12EInstitutionOfficeOwnerSnapshot.CurrentSchemaVersion, oversized, oversized,
                Array.Empty<P12EInstitutionSnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, 0L,
                Array.Empty<P12EOfficeSnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 0L,
                Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, 0L,
                Array.Empty<P12EOfficeTenureSnapshotRecord>(), row => row?.Copy()));
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(invalid,
            out P12AOwnerSection section, out _, out _, out _, out P12AResult result), Is.False);
        Assert.That(section, Is.Null);
        Assert.That(result.Code, Is.EqualTo(P12AResultCode.LimitExceeded));
    }

    [Test]
    public void InstitutionOfficeCodecRejectsLossyNamesAndUnreachableOwnerRevisions()
    {
        P12EInstitutionOfficeOwnerSnapshot nullInstitutionName = new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, 1L,
                new[] { new P12EInstitutionSnapshotRecord("institution-a", null) }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, 0L,
                Array.Empty<P12EOfficeSnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 0L,
                Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, 0L,
                Array.Empty<P12EOfficeTenureSnapshotRecord>(), row => row?.Copy()));

        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(nullInstitutionName,
            out _, out _, out _, out _, out _), Is.False);
        Assert.That(nullInstitutionName.TryCreateStagedOwners(new PersonStore(),
            out _, out _, out _), Is.False);

        P12EInstitutionOfficeOwnerSnapshot valid = CreateSnapshot();
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(valid,
            out P12AOwnerSection institutions, out P12AOwnerSection offices,
            out P12AOwnerSection incumbencies, out P12AOwnerSection tenures, out _), Is.True);

        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(
            CopySection(institutions, 0L, institutions.Rows), offices, incumbencies, tenures,
            out _, out _), Is.False, "A retained institution row cannot be reachable at revision zero.");
        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(institutions,
            CopySection(offices, 0L, offices.Rows),
            CopySection(incumbencies, 0L, incumbencies.Rows),
            CopySection(tenures, 0L, tenures.Rows), out _, out _), Is.False,
            "Retained office, incumbency, and tenure rows must be reachable from their shared revision.");

        P12EInstitutionOfficeOwnerSnapshot unreachableOffice = new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, 1L,
                new[] { new P12EInstitutionSnapshotRecord("institution-a", "Council") }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, 0L,
                new[] { new P12EOfficeSnapshotRecord("office-a", "institution-a", "Chancellor") }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 0L,
                Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, 0L,
                Array.Empty<P12EOfficeTenureSnapshotRecord>(), row => row?.Copy()));
        Assert.That(unreachableOffice.TryCreateStagedOwners(new PersonStore(),
            out _, out _, out _), Is.False);
    }

    private static void AssertRowsEqual(P12AOwnerSection left, P12AOwnerSection right)
    {
        Assert.That(right.Id, Is.EqualTo(left.Id));
        Assert.That(right.Revision, Is.EqualTo(left.Revision));
        Assert.That(right.Rows.Select(row => row.OwnerKey), Is.EqualTo(left.Rows.Select(row => row.OwnerKey)));
        Assert.That(right.Rows.Select(row => Convert.ToBase64String(row.Payload)),
            Is.EqualTo(left.Rows.Select(row => Convert.ToBase64String(row.Payload))));
    }

    private static P12AOwnerSection CopySection(P12AOwnerSection source, long revision,
        IEnumerable<P12AOwnerRow> rows)
        => new P12AOwnerSection(source.Id, source.Major, source.Minor, source.Role,
            source.AliasGroup, revision, rows);

    private static P12EInstitutionOfficeSnapshotSection<T> Section<T>(string id, long revision,
        IEnumerable<T> rows, Func<T, T> copy)
    {
        List<T> values = rows.ToList();
        return new P12EInstitutionOfficeSnapshotSection<T>(id,
            P12EInstitutionOfficeOwnerSnapshot.CurrentSchemaVersion, values.Count, revision, values, copy);
    }

    private static P12EInstitutionOfficeOwnerSnapshot CreateSnapshot()
    {
        return new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, 2L,
                new[]
                {
                    new P12EInstitutionSnapshotRecord("institution-b", "Assembly"),
                    new P12EInstitutionSnapshotRecord("institution-a", "Council")
                }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, 8L,
                new[]
                {
                    new P12EOfficeSnapshotRecord("office-b", "institution-b", "Speaker"),
                    new P12EOfficeSnapshotRecord("office-a", "institution-a", "Chancellor")
                }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 8L,
                new[] { new P12EOfficeIncumbencySnapshotRecord("office-a", "person-a", 4L) }, row => row?.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, 8L,
                new[]
                {
                    new P12EOfficeTenureSnapshotRecord("office-b", "person-b", 1L, 2L,
                        (int)InstitutionalVacancyRecognitionReason.Resignation, true),
                    new P12EOfficeTenureSnapshotRecord("office-a", "person-a", 4L, null, null, false),
                    new P12EOfficeTenureSnapshotRecord("office-b", "person-b", 1L, 2L,
                        (int)InstitutionalVacancyRecognitionReason.Resignation, true)
                }, row => row?.Copy()));
    }
}
