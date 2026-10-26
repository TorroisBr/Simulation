using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12ECoreOwnerRestoreStagerTests
{
    [Test]
    public void StagesCoreOwnerRootsInDependencyOrderAndPreservesExactReferences()
    {
        PersonStore persons = new PersonStore();
        List<P12ECoreOwnerRestoreStep> order = new List<P12ECoreOwnerRestoreStep>();
        Assert.That(DailyV1P12ECoreOwnerRestoreStager.TryStage(
            EmptyInstitutionOffice(), EmptyPropertyEstate(), EmptyFaction(), EmptyPoliticalClaims(),
            EmptyPoliticalSupport(), persons, 4L, order.Add,
            out P12ECoreOwnerRestoreStage staged,
            out P12ECoreOwnerRestoreFailure failure), Is.True, failure.ToString());

        Assert.That(staged.Persons, Is.SameAs(persons));
        Assert.That(staged.Offices.InstitutionStoreForWorldBoundary, Is.SameAs(staged.Institutions));
        Assert.That(staged.PropertyOwnership.PersonStoreForWorldBoundary, Is.SameAs(persons));
        Assert.That(staged.Institutions.Revision, Is.Zero);
        Assert.That(staged.Offices.Revision, Is.Zero);
        Assert.That(staged.PropertyOwnership.Revision, Is.Zero);
        Assert.That(staged.Factions.Revision, Is.Zero);
        Assert.That(staged.PoliticalClaims.Revision, Is.Zero);
        Assert.That(staged.PoliticalSupport.Count, Is.Zero);
        Assert.That(staged.PoliticalSupport.Revision, Is.Zero);
        Assert.That(order, Is.EqualTo(new[]
        {
            P12ECoreOwnerRestoreStep.InstitutionOffice,
            P12ECoreOwnerRestoreStep.PropertyEstate,
            P12ECoreOwnerRestoreStep.Faction,
            P12ECoreOwnerRestoreStep.PoliticalClaim,
            P12ECoreOwnerRestoreStep.PoliticalSupport
        }));
    }

    [Test]
    public void InvalidClaimSnapshotRejectsWholePrivateCoreStage()
    {
        PersonStore persons = new PersonStore();
        P12EPoliticalClaimOwnerSnapshot invalidClaims = new P12EPoliticalClaimOwnerSnapshot(
            null,
            ClaimSection(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 0, 0,
                Array.Empty<P12EPoliticalClaimRecognitionRow>(), row => row.Copy()));

        Assert.That(DailyV1P12ECoreOwnerRestoreStager.TryStage(
            EmptyInstitutionOffice(), EmptyPropertyEstate(), EmptyFaction(), invalidClaims,
            EmptyPoliticalSupport(), persons, 4L, null,
            out P12ECoreOwnerRestoreStage staged,
            out P12ECoreOwnerRestoreFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12ECoreOwnerRestoreFailure.StageFailed));
    }

    [Test]
    public void InvalidPoliticalSupportSnapshotRejectsWholePrivateCoreStage()
    {
        PersonStore persons = new PersonStore();
        P12EPoliticalSupportOwnerSnapshot invalidSupport =
            new P12EPoliticalSupportOwnerSnapshot(4L, null);

        Assert.That(DailyV1P12ECoreOwnerRestoreStager.TryStage(
            EmptyInstitutionOffice(), EmptyPropertyEstate(), EmptyFaction(), EmptyPoliticalClaims(),
            invalidSupport, persons, 4L, null,
            out P12ECoreOwnerRestoreStage staged,
            out P12ECoreOwnerRestoreFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12ECoreOwnerRestoreFailure.StageFailed));
    }

    private static P12EInstitutionOfficeOwnerSnapshot EmptyInstitutionOffice() =>
        new P12EInstitutionOfficeOwnerSnapshot(
            InstitutionSection(InstitutionOfficeCensusProvider.InstitutionsSectionId, 0, 0,
                Array.Empty<P12EInstitutionSnapshotRecord>(), row => row.Copy()),
            InstitutionSection(InstitutionOfficeCensusProvider.OfficesSectionId, 0, 0,
                Array.Empty<P12EOfficeSnapshotRecord>(), row => row.Copy()),
            InstitutionSection(InstitutionOfficeCensusProvider.IncumbenciesSectionId, 0, 0,
                Array.Empty<P12EOfficeIncumbencySnapshotRecord>(), row => row.Copy()),
            InstitutionSection(InstitutionOfficeCensusProvider.TenuresSectionId, 0, 0,
                Array.Empty<P12EOfficeTenureSnapshotRecord>(), row => row.Copy()));

    private static PropertyEstateOwnerSnapshot EmptyPropertyEstate() =>
        new PropertyEstateOwnerSnapshot(
            1, 0, 0L, Array.Empty<PropertyEstateOwnershipRow>(),
            1, 0, 0L, Array.Empty<PropertyEstateTransferHistoryRow>(),
            1, 0, 0L, Array.Empty<PropertyEstateEstateRow>());

    private static P12EFactionOwnerSnapshot EmptyFaction() =>
        new P12EFactionOwnerSnapshot(
            FactionSection(FactionStoreCensusProvider.FactionsSectionId, 0, 0,
                Array.Empty<P12EFactionSnapshotRecord>(), row => row.Copy()),
            FactionSection(FactionStoreCensusProvider.AffiliationsSectionId, 0, 0,
                Array.Empty<P12EFactionAffiliationSnapshotRecord>(), row => row.Copy()));

    private static P12EPoliticalSupportOwnerSnapshot EmptyPoliticalSupport() =>
        new P12EPoliticalSupportOwnerSnapshot(4L,
            new P12EPoliticalSupportSnapshotSection(
                PoliticalSupportStoreCensusProvider.RelationsSectionId,
                P12EPoliticalSupportOwnerSnapshot.CurrentSchemaVersion,
                0, 0L, Array.Empty<P12EPoliticalSupportSnapshotRow>()));

    private static P12EPoliticalClaimOwnerSnapshot EmptyPoliticalClaims() =>
        new P12EPoliticalClaimOwnerSnapshot(
            ClaimSection(PoliticalClaimStoreCensusProvider.ClaimsSectionId, 0, 0,
                Array.Empty<P12EPoliticalClaimRow>(), row => row.Copy()),
            ClaimSection(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 0, 0,
                Array.Empty<P12EPoliticalClaimRecognitionRow>(), row => row.Copy()));

    private static P12EInstitutionOfficeSnapshotSection<T> InstitutionSection<T>(
        string id, int count, long revision, IReadOnlyList<T> rows, Func<T, T> copy) =>
        new P12EInstitutionOfficeSnapshotSection<T>(
            id, P12EInstitutionOfficeOwnerSnapshot.CurrentSchemaVersion,
            count, revision, rows, copy);

    private static P12EFactionSnapshotSection<T> FactionSection<T>(
        string id, int count, long revision, IReadOnlyList<T> rows, Func<T, T> copy) =>
        new P12EFactionSnapshotSection<T>(
            id, P12EFactionOwnerSnapshot.CurrentSchemaVersion,
            count, revision, rows, copy);

    private static P12EPoliticalClaimSnapshotSection<T> ClaimSection<T>(
        string id, int count, long revision, IReadOnlyList<T> rows, Func<T, T> copy) =>
        new P12EPoliticalClaimSnapshotSection<T>(
            id, P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion,
            count, revision, rows, copy);
}