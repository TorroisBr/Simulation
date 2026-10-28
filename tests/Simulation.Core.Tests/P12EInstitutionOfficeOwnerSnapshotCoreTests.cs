using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12EInstitutionOfficeOwnerSnapshotCoreTests
{
    [Test]
    public void CodecRoundTripAndStaging_PreserveInstitutionOfficeIdentityRevisionAndBinding()
    {
        P12EInstitutionOfficeOwnerSnapshot source = CreateSnapshot(
            new[] { new P12EInstitutionSnapshotRecord("institution:alpha", "Alpha") },
            new[] { new P12EOfficeSnapshotRecord("office:mayor", "institution:alpha", "Mayor") },
            Array.Empty<P12EOfficeIncumbencySnapshotRecord>(),
            Array.Empty<P12EOfficeTenureSnapshotRecord>(),
            institutionRevision: 1,
            officeRevision: 1);

        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(source,
            out P12AOwnerSection institutionsSection,
            out P12AOwnerSection officesSection,
            out P12AOwnerSection incumbenciesSection,
            out P12AOwnerSection tenuresSection,
            out P12AResult encodeFailure), Is.True, encodeFailure?.Code.ToString());

        Assert.That(P12EInstitutionOfficeOwnerCodec.TryDecode(
            institutionsSection, officesSection, incumbenciesSection, tenuresSection,
            out P12EInstitutionOfficeOwnerSnapshot decoded,
            out P12AResult decodeFailure), Is.True, decodeFailure?.Code.ToString());

        Assert.That(decoded.TryCreateStagedOwners(new PersonStore(),
            out InstitutionStore stagedInstitutions,
            out OfficeStore stagedOffices,
            out P12EInstitutionOfficeSnapshotFailure stageFailure),
            Is.True, stageFailure?.Message);
        Assert.That(stagedInstitutions.Revision, Is.EqualTo(1));
        Assert.That(stagedOffices.Revision, Is.EqualTo(1));
        Assert.That(InstitutionOfficeOwnerBridge.BelongsTo(stagedOffices, stagedInstitutions), Is.True);
        Assert.That(stagedInstitutions.TryGet(new InstitutionId("institution:alpha"),
            out InstitutionRecord institution), Is.True);
        Assert.That(institution.DisplayName, Is.EqualTo("Alpha"));
        Assert.That(stagedOffices.TryGet(new OfficeId("office:mayor"),
            out OfficeRecord office), Is.True);
        Assert.That(office.InstitutionId.Value, Is.EqualTo("institution:alpha"));
        Assert.That(office.DisplayName, Is.EqualTo("Mayor"));
    }

    [Test]
    public void LocalValidation_RejectsOfficeWhoseInstitutionIsMissing()
    {
        P12EInstitutionOfficeOwnerSnapshot snapshot = CreateSnapshot(
            new[] { new P12EInstitutionSnapshotRecord("institution:alpha", "Alpha") },
            new[] { new P12EOfficeSnapshotRecord("office:mayor", "institution:missing", "Mayor") },
            Array.Empty<P12EOfficeIncumbencySnapshotRecord>(),
            Array.Empty<P12EOfficeTenureSnapshotRecord>(),
            institutionRevision: 1,
            officeRevision: 1);

        Assert.That(snapshot.TryValidateLocal(out P12EInstitutionOfficeSnapshotFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(P12EInstitutionOfficeSnapshotFailureCode.InvalidRelation));
    }

    [Test]
    public void SnapshotCodec_PreservesEmptyIncumbencyAndTenureSectionsAtSharedOfficeRevision()
    {
        P12EInstitutionOfficeOwnerSnapshot snapshot = CreateSnapshot(
            Array.Empty<P12EInstitutionSnapshotRecord>(),
            Array.Empty<P12EOfficeSnapshotRecord>(),
            Array.Empty<P12EOfficeIncumbencySnapshotRecord>(),
            Array.Empty<P12EOfficeTenureSnapshotRecord>(),
            institutionRevision: 0,
            officeRevision: 3);

        Assert.That(P12EInstitutionOfficeOwnerCodec.TryEncode(snapshot,
            out P12AOwnerSection institutionsSection,
            out P12AOwnerSection officesSection,
            out P12AOwnerSection incumbenciesSection,
            out P12AOwnerSection tenuresSection,
            out P12AResult failure), Is.True, failure?.Code.ToString());
        Assert.That(institutionsSection.Revision, Is.EqualTo(0));
        Assert.That(officesSection.Revision, Is.EqualTo(3));
        Assert.That(incumbenciesSection.Revision, Is.EqualTo(3));
        Assert.That(tenuresSection.Revision, Is.EqualTo(3));
        Assert.That(incumbenciesSection.Rows, Is.Empty);
        Assert.That(tenuresSection.Rows, Is.Empty);
    }

    private static P12EInstitutionOfficeOwnerSnapshot CreateSnapshot(
        IReadOnlyList<P12EInstitutionSnapshotRecord> institutions,
        IReadOnlyList<P12EOfficeSnapshotRecord> offices,
        IReadOnlyList<P12EOfficeIncumbencySnapshotRecord> incumbencies,
        IReadOnlyList<P12EOfficeTenureSnapshotRecord> tenures,
        long institutionRevision,
        long officeRevision) =>
        new P12EInstitutionOfficeOwnerSnapshot(
            Section(InstitutionOfficeCensusProvider.InstitutionsSectionId, institutions.Count,
                institutionRevision, institutions, row => row.Copy()),
            Section(InstitutionOfficeCensusProvider.OfficesSectionId, offices.Count,
                officeRevision, offices, row => row.Copy()),
            Section(InstitutionOfficeCensusProvider.IncumbenciesSectionId, incumbencies.Count,
                officeRevision, incumbencies, row => row.Copy()),
            Section(InstitutionOfficeCensusProvider.TenuresSectionId, tenures.Count,
                officeRevision, tenures, row => row.Copy()));

    private static P12EInstitutionOfficeSnapshotSection<T> Section<T>(
        string id, int count, long revision, IReadOnlyList<T> records, Func<T, T> copy) =>
        new P12EInstitutionOfficeSnapshotSection<T>(
            id, P12EInstitutionOfficeOwnerSnapshot.CurrentSchemaVersion,
            count, revision, records, copy);
}