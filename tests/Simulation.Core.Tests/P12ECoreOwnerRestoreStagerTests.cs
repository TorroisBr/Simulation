using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12ECoreOwnerRestoreStagerTests
{
    [Test]
    public void StagesCoreOwnerRootsInDependencyOrderAndPreservesExactReferences()
    {
        PersonStore persons = new PersonStore();
        RootFixture roots = CreateRoots("p12e-core-city", "p12e-core-person-backed", "p12e-core-legacy");
        List<P12ECoreOwnerRestoreStep> order = new List<P12ECoreOwnerRestoreStep>();
        Assert.That(DailyV1P12ECoreOwnerRestoreStager.TryStage(
            EmptyInstitutionOffice(), EmptyPropertyEstate(), EmptyFaction(), EmptyPoliticalClaims(),
            EmptyPoliticalSupport(), EmptyPoliticalDecisions(), EmptyJustice(4L), persons,
            roots.Cities, roots.Npcs, 4L, order.Add,
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
        Assert.That(staged.PoliticalDecisions.Count, Is.Zero);
        Assert.That(staged.PoliticalDecisions.Revision, Is.Zero);
        Assert.That(staged.JusticeRecords, Is.Not.Null);
        Assert.That(staged.JusticeRecords.WantedRecords, Is.Empty);
        Assert.That(staged.JusticeRecords.Sentences, Is.Empty);
        Assert.That(staged.JusticeRecords.Revision, Is.Zero);
        Assert.That(order, Is.EqualTo(new[]
        {
            P12ECoreOwnerRestoreStep.InstitutionOffice,
            P12ECoreOwnerRestoreStep.PropertyEstate,
            P12ECoreOwnerRestoreStep.Faction,
            P12ECoreOwnerRestoreStep.PoliticalClaim,
            P12ECoreOwnerRestoreStep.PoliticalSupport,
            P12ECoreOwnerRestoreStep.PoliticalDecision,
            P12ECoreOwnerRestoreStep.JusticeRecords
        }));
    }

    [Test]
    public void JusticeSnapshotStagesOneExactCoreOwnerWithOrderedRowsAndReciprocalPersonBindings()
    {
        RootFixture roots = CreateRoots("p12e-core-justice-city",
            "p12e-core-justice-person", "p12e-core-justice-legacy");
        PersonStore persons = new PersonStore();
        PersonId personId = new PersonId("p12e-core-justice-person-id");
        PersonRuntime person = new PersonRuntime(personId, 0L);
        Assert.That(persons.TryRegister(person, out _), Is.True);
        Assert.That(persons.TryBindMaterializedNpc(personId,
            "p12e-core-justice-person", out _), Is.True);

        P12EJusticeRecordsOwnerSnapshot snapshot = JusticeSnapshot(9L,
            new[]
            {
                new P12EJusticeWantedSnapshotRow(0, "p12e-core-justice-person",
                    personId.Value, "p12e-core-justice-city", 12.5f, 7, true),
                new P12EJusticeWantedSnapshotRow(1, "p12e-core-justice-legacy",
                    null, "p12e-core-justice-city", 4f, 3, false)
            },
            new[]
            {
                new P12EJusticeSentenceSnapshotRow(0, "p12e-core-justice-person",
                    personId.Value, "p12e-core-justice-city", 0, 0, 2, false),
                new P12EJusticeSentenceSnapshotRow(1, "p12e-core-justice-legacy",
                    null, "p12e-core-justice-city", 1, 5, 0, true)
            }, 9L);

        Assert.That(snapshot.TryStageCoreOwner(persons, roots.Cities, roots.Npcs,
            out DailyV1JusticeRecordsOwner staged, out P12EJusticeSnapshotFailure failure),
            Is.True, failure?.Message);
        Assert.That(staged.Revision, Is.EqualTo(9L));
        Assert.That(staged.WantedRecords, Has.Count.EqualTo(2));
        Assert.That(staged.Sentences, Has.Count.EqualTo(2));
        Assert.That(staged.WantedRecords[0].TargetRuntimeId, Is.EqualTo("p12e-core-justice-person"));
        Assert.That(staged.WantedRecords[0].Bounty, Is.EqualTo(12.5f));
        Assert.That(staged.WantedRecords[0].SentenceDays, Is.EqualTo(7));
        Assert.That(staged.WantedRecords[0].IsResolved, Is.True);
        Assert.That(staged.WantedRecords[1].TargetRuntimeId, Is.EqualTo("p12e-core-justice-legacy"));
        Assert.That(staged.Sentences[0].Warrant, Is.SameAs(staged.WantedRecords[0]));
        Assert.That(staged.Sentences[1].Warrant, Is.SameAs(staged.WantedRecords[1]));
        Assert.That(staged.Sentences[0].RemainingDays, Is.Zero);
        Assert.That(staged.Sentences[0].FailedEscapeAttempts, Is.EqualTo(2));
        Assert.That(staged.Sentences[0].WasArrestedToday, Is.False);
        Assert.That(staged.Sentences[1].WasArrestedToday, Is.True);
        Assert.That(person.MaterializedNpcRuntimeId, Is.EqualTo("p12e-core-justice-person"));
        Assert.That(persons.TryGetByMaterializedNpcRuntimeId("p12e-core-justice-person",
            out PersonRuntime indexed), Is.True);
        Assert.That(indexed, Is.SameAs(person));
        Assert.That(staged.HasP12MutationBoundary, Is.False);
    }

    [Test]
    public void JusticeSnapshotRejectsMissingAmbiguousRootsAndOneSidedBindingsAtomically()
    {
        RootFixture roots = CreateRoots("p12e-core-reject-city",
            "p12e-core-reject-person", "p12e-core-reject-legacy");
        PersonStore persons = new PersonStore();
        PersonId personId = new PersonId("p12e-core-reject-person-id");
        PersonRuntime person = new PersonRuntime(personId, 0L);
        Assert.That(persons.TryRegister(person, out _), Is.True);
        Assert.That(persons.TryBindMaterializedNpc(personId,
            "p12e-core-reject-person", out _), Is.True);
        P12EJusticeRecordsOwnerSnapshot personBacked = JusticeSnapshot(3L,
            new[] { new P12EJusticeWantedSnapshotRow(0, "p12e-core-reject-person",
                personId.Value, "p12e-core-reject-city", 1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>());

        AssertJusticeStageRejected(personBacked, persons, roots.Cities,
            Array.Empty<DailyV1NpcRuntimeOwners>());
        AssertJusticeStageRejected(personBacked, persons,
            new[] { roots.Cities[0], roots.Cities[0] }, roots.Npcs);
        AssertJusticeStageRejected(personBacked, new PersonStore(), roots.Cities, roots.Npcs);

        P12EJusticeRecordsOwnerSnapshot legacyWithBinding = JusticeSnapshot(0L,
            new[] { new P12EJusticeWantedSnapshotRow(0, "p12e-core-reject-person",
                null, "p12e-core-reject-city", 1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>());
        AssertJusticeStageRejected(legacyWithBinding, persons, roots.Cities, roots.Npcs);

        PersonStore npcOnly = new PersonStore();
        Assert.That(npcOnly.TryRegister(new PersonRuntime(personId, 0L), out _), Is.True);
        AssertJusticeStageRejected(personBacked, npcOnly, roots.Cities, roots.Npcs);
    }

    [Test]
    public void JusticeSnapshotRejectsWrongTargetCityAndPersonRuntimeBinding()
    {
        RootFixture roots = CreateRoots("p12e-core-link-city",
            "p12e-core-link-person", "p12e-core-link-legacy");
        PersonId firstId = new PersonId("p12e-core-link-person-id");
        PersonStore wrongTargetStore = new PersonStore();
        PersonRuntime wrongTargetPerson = new PersonRuntime(firstId, 0L);
        Assert.That(wrongTargetStore.TryRegister(wrongTargetPerson, out _), Is.True);
        Assert.That(wrongTargetStore.TryBindMaterializedNpc(firstId,
            "p12e-core-link-legacy", out _), Is.True);
        P12EJusticeRecordsOwnerSnapshot wrongTarget = JusticeSnapshot(0L,
            new[] { new P12EJusticeWantedSnapshotRow(0, "p12e-core-link-person",
                firstId.Value, "p12e-core-link-city", 1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>());
        AssertJusticeStageRejected(wrongTarget, wrongTargetStore, roots.Cities, roots.Npcs);

        P12EJusticeRecordsOwnerSnapshot wrongCity = JusticeSnapshot(0L,
            new[] { new P12EJusticeWantedSnapshotRow(0, "p12e-core-link-person",
                null, "missing-city", 1f, 1, false) },
            Array.Empty<P12EJusticeSentenceSnapshotRow>());
        AssertJusticeStageRejected(wrongCity, new PersonStore(), roots.Cities, roots.Npcs);
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
            EmptyPoliticalSupport(), EmptyPoliticalDecisions(), EmptyJustice(4L), persons,
            Array.Empty<DailyV1CityRuntimeOwners>(), Array.Empty<DailyV1NpcRuntimeOwners>(), 4L, null,
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
            invalidSupport, EmptyPoliticalDecisions(), EmptyJustice(4L), persons,
            Array.Empty<DailyV1CityRuntimeOwners>(), Array.Empty<DailyV1NpcRuntimeOwners>(), 4L, null,
            out P12ECoreOwnerRestoreStage staged,
            out P12ECoreOwnerRestoreFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12ECoreOwnerRestoreFailure.StageFailed));
    }

    [Test]
    public void InvalidPoliticalDecisionSnapshotRejectsWholePrivateCoreStage()
    {
        PersonStore persons = new PersonStore();
        P12EPoliticalDecisionOwnerSnapshot invalidDecisions =
            new P12EPoliticalDecisionOwnerSnapshot(4L, null);

        Assert.That(DailyV1P12ECoreOwnerRestoreStager.TryStage(
            EmptyInstitutionOffice(), EmptyPropertyEstate(), EmptyFaction(), EmptyPoliticalClaims(),
            EmptyPoliticalSupport(), invalidDecisions, EmptyJustice(4L), persons,
            Array.Empty<DailyV1CityRuntimeOwners>(), Array.Empty<DailyV1NpcRuntimeOwners>(), 4L, null,
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

    private static P12EPoliticalDecisionOwnerSnapshot EmptyPoliticalDecisions() =>
        new P12EPoliticalDecisionOwnerSnapshot(4L,
            new P12EPoliticalDecisionSnapshotSection(
                PoliticalDecisionStoreCensusProvider.SectionId,
                P12EPoliticalDecisionOwnerSnapshot.CurrentSchemaVersion,
                0, 0L, Array.Empty<P12EPoliticalDecisionSnapshotRow>()));

    private static P12EPoliticalClaimOwnerSnapshot EmptyPoliticalClaims() =>
        new P12EPoliticalClaimOwnerSnapshot(
            ClaimSection(PoliticalClaimStoreCensusProvider.ClaimsSectionId, 0, 0,
                Array.Empty<P12EPoliticalClaimRow>(), row => row.Copy()),
            ClaimSection(PoliticalClaimStoreCensusProvider.RecognitionsSectionId, 0, 0,
                Array.Empty<P12EPoliticalClaimRecognitionRow>(), row => row.Copy()));

    private static P12EJusticeRecordsOwnerSnapshot EmptyJustice(long day) =>
        JusticeSnapshot(day, Array.Empty<P12EJusticeWantedSnapshotRow>(),
            Array.Empty<P12EJusticeSentenceSnapshotRow>());

    private static P12EJusticeRecordsOwnerSnapshot JusticeSnapshot(
        long day,
        IReadOnlyList<P12EJusticeWantedSnapshotRow> wanted,
        IReadOnlyList<P12EJusticeSentenceSnapshotRow> sentences,
        long revision = 0L) =>
        new P12EJusticeRecordsOwnerSnapshot(day,
            new P12EJusticeRecordsSnapshotSection(
                P12EJusticeRecordsSnapshotSection.CurrentSectionId,
                P12EJusticeRecordsSnapshotSection.CurrentSchemaVersion,
                checked(wanted.Count + sentences.Count), revision, wanted, sentences));

    private static RootFixture CreateRoots(string cityId, params string[] npcIds)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition cityDefinition = definitions.Cities.Values.First();
        DailyV1NpcDefinition npcDefinition = definitions.Npcs.Values.First();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            cityId, "location/" + cityId, definitions, cityDefinition, null,
            out DailyV1CityRuntimeOwners city), Is.True);
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>(npcIds.Length);
        foreach (string npcId in npcIds)
        {
            Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
                npcId, definitions, npcDefinition, 0f,
                out DailyV1NpcRuntimeOwners npc), Is.True);
            npcs.Add(npc);
        }
        return new RootFixture(new[] { city }, npcs);
    }

    private static void AssertJusticeStageRejected(
        P12EJusticeRecordsOwnerSnapshot snapshot,
        PersonStore persons,
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcs)
    {
        int personCount = persons.Persons.Count;
        long personRevision = persons.Revision;
        Assert.That(snapshot.TryStageCoreOwner(persons, cities, npcs,
            out DailyV1JusticeRecordsOwner staged, out P12EJusticeSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(P12EJusticeSnapshotFailureCode.InvalidReference));
        Assert.That(persons.Persons.Count, Is.EqualTo(personCount));
        Assert.That(persons.Revision, Is.EqualTo(personRevision));
    }

    private sealed class RootFixture
    {
        internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities { get; }
        internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs { get; }

        internal RootFixture(
            IReadOnlyList<DailyV1CityRuntimeOwners> cities,
            IReadOnlyList<DailyV1NpcRuntimeOwners> npcs)
        {
            Cities = cities;
            Npcs = npcs;
        }
    }

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
