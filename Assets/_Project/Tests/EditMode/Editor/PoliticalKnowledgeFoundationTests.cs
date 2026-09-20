using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;

public sealed class PoliticalKnowledgeFoundationTests
{
    [Test]
    public void KnowledgeSupportsPersonAndInstitutionHoldersWithStableIdentity()
    {
        PersonId personId = new PersonId("person.holder");
        InstitutionId institutionId = new InstitutionId("institution.archive");
        PoliticalKnowledgeRuntime personKnowledge = new PoliticalKnowledgeRuntime(personId);
        PoliticalKnowledgeRuntime institutionKnowledge = new PoliticalKnowledgeRuntime(institutionId);

        Assert.That(personKnowledge.HolderKind, Is.EqualTo(PoliticalKnowledgeHolderKind.Person));
        Assert.That(personKnowledge.HolderPersonId, Is.EqualTo(personId));
        Assert.That(personKnowledge.HolderInstitutionId, Is.Null);
        Assert.That(personKnowledge.Holder.StableId, Is.EqualTo("person.holder"));
        Assert.That(institutionKnowledge.HolderKind, Is.EqualTo(PoliticalKnowledgeHolderKind.Institution));
        Assert.That(institutionKnowledge.HolderInstitutionId, Is.EqualTo(institutionId));
        Assert.That(institutionKnowledge.HolderPersonId, Is.Null);
        Assert.That(institutionKnowledge.Holder.StableId, Is.EqualTo("institution.archive"));
        Assert.That(typeof(PoliticalKnowledgeRuntime).GetProperty("NpcRuntimeId"), Is.Null);
    }

    [Test]
    public void ObservationsUseStableTypedPoliticalReferences()
    {
        PoliticalClaimId claimId = new PoliticalClaimId("claim.office");
        FactionId factionId = new FactionId("faction.council");
        PersonId personId = new PersonId("person.candidate");
        InstitutionId institutionId = new InstitutionId("institution.court");
        OfficeId officeId = new OfficeId("office.seat");
        PoliticalKnowledgeProvenance provenance = DirectProvenance();
        PoliticalKnowledgeRuntime knowledge = new PoliticalKnowledgeRuntime(personId);

        Assert.That(knowledge.RecordClaimObservation(new PoliticalClaimKnowledgeObservation(
            claimId,
            true,
            PoliticalClaimStatus.Active,
            PoliticalClaimRecognitionState.Unrecognized,
            0L,
            0L,
            provenance)), Is.True);
        Assert.That(knowledge.RecordFactionObservation(new FactionKnowledgeObservation(
            factionId, true, 0L, 0L, provenance)), Is.True);
        Assert.That(knowledge.RecordFactionAffiliationObservation(new FactionAffiliationKnowledgeObservation(
            factionId, personId, true, 0L, 0L, provenance)), Is.True);
        Assert.That(knowledge.RecordOfficeVacancyObservation(new OfficeVacancyKnowledgeObservation(
            officeId, institutionId, true, 0L, 0L, provenance)), Is.True);
        Assert.That(knowledge.RecordPersonDeathObservation(new PersonDeathKnowledgeObservation(
            personId, true, 0L, 0L, 0L, provenance)), Is.True);

        Assert.That(knowledge.ClaimObservations[0].ClaimId, Is.EqualTo(claimId));
        Assert.That(knowledge.FactionObservations[0].FactionId, Is.EqualTo(factionId));
        Assert.That(knowledge.FactionAffiliationObservations[0].FactionId, Is.EqualTo(factionId));
        Assert.That(knowledge.FactionAffiliationObservations[0].PersonId, Is.EqualTo(personId));
        Assert.That(knowledge.OfficeVacancyObservations[0].OfficeId, Is.EqualTo(officeId));
        Assert.That(knowledge.OfficeVacancyObservations[0].InstitutionId, Is.EqualTo(institutionId));
        Assert.That(knowledge.PersonDeathObservations[0].PersonId, Is.EqualTo(personId));
    }

    [Test]
    public void KnowledgeSnapshotsCanBeStaleOrFalseWithoutMutatingWorldTruth()
    {
        PersonStore personStore = new PersonStore();
        PersonId personId = new PersonId("person.alive");
        PersonRuntime person = new PersonRuntime(personId, 0L);
        Assert.That(personStore.TryRegister(person, out PersonStoreFailure registrationFailure), Is.True, registrationFailure.ToString());

        PoliticalKnowledgeRuntime knowledge = new PoliticalKnowledgeRuntime(personId);
        Assert.That(knowledge.RecordPersonDeathObservation(new PersonDeathKnowledgeObservation(
            personId,
            true,
            3L,
            3L,
            20L,
            DirectProvenance())), Is.True);
        Assert.That(knowledge.RecordPersonDeathObservation(new PersonDeathKnowledgeObservation(
            personId,
            false,
            null,
            2L,
            20L,
            DirectProvenance())), Is.False);

        Assert.That(person.IsDeadAt(20L), Is.False);
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(knowledge.TryGetLatestPersonDeathObservation(personId, out PersonDeathKnowledgeObservation snapshot), Is.True);
        Assert.That(snapshot.IsDead, Is.True);
        Assert.That(snapshot.DeathAbsoluteDay, Is.EqualTo(3L));
    }

    [Test]
    public void LatestSelectionIsObservedDayThenSourceThenReceivedDayAndDeterministicSnapshot()
    {
        PoliticalClaimId claimId = new PoliticalClaimId("claim.latest");
        PoliticalKnowledgeRuntime knowledge = new PoliticalKnowledgeRuntime(new PersonId("person.reader"));

        Assert.That(knowledge.RecordClaimObservation(new PoliticalClaimKnowledgeObservation(
            claimId,
            true,
            PoliticalClaimStatus.Active,
            PoliticalClaimRecognitionState.Unrecognized,
            10L,
            10L,
            InitialProvenance())), Is.True);
        Assert.That(knowledge.RecordClaimObservation(new PoliticalClaimKnowledgeObservation(
            claimId,
            false,
            PoliticalClaimStatus.Rejected,
            PoliticalClaimRecognitionState.Rejected,
            9L,
            30L,
            DirectProvenance())), Is.False);
        Assert.That(knowledge.RecordClaimObservation(new PoliticalClaimKnowledgeObservation(
            claimId,
            false,
            PoliticalClaimStatus.Rejected,
            PoliticalClaimRecognitionState.Rejected,
            10L,
            11L,
            SharedByPersonProvenance("person.source"))), Is.True);
        Assert.That(knowledge.RecordClaimObservation(new PoliticalClaimKnowledgeObservation(
            claimId,
            false,
            PoliticalClaimStatus.Rejected,
            PoliticalClaimRecognitionState.Rejected,
            10L,
            11L,
            DirectProvenance())), Is.True);

        Assert.That(knowledge.TryGetLatestClaimObservation(claimId, out PoliticalClaimKnowledgeObservation current), Is.True);
        Assert.That(current.Exists, Is.False);
        Assert.That(current.Source, Is.EqualTo(PoliticalKnowledgeSource.DirectObservation));

        PoliticalKnowledgeRuntime first = new PoliticalKnowledgeRuntime(new PersonId("person.first"));
        PoliticalKnowledgeRuntime second = new PoliticalKnowledgeRuntime(new PersonId("person.second"));
        PoliticalClaimKnowledgeObservation falseSnapshot = new PoliticalClaimKnowledgeObservation(
            claimId,
            false,
            PoliticalClaimStatus.Active,
            PoliticalClaimRecognitionState.Unrecognized,
            5L,
            5L,
            DirectProvenance());
        PoliticalClaimKnowledgeObservation trueSnapshot = new PoliticalClaimKnowledgeObservation(
            claimId,
            true,
            PoliticalClaimStatus.Active,
            PoliticalClaimRecognitionState.Unrecognized,
            5L,
            5L,
            DirectProvenance());
        Assert.That(first.RecordClaimObservation(falseSnapshot), Is.True);
        Assert.That(first.RecordClaimObservation(trueSnapshot), Is.True);
        Assert.That(second.RecordClaimObservation(trueSnapshot), Is.True);
        Assert.That(second.RecordClaimObservation(falseSnapshot), Is.False);
        Assert.That(first.TryGetLatestClaimObservation(claimId, out PoliticalClaimKnowledgeObservation firstCurrent), Is.True);
        Assert.That(second.TryGetLatestClaimObservation(claimId, out PoliticalClaimKnowledgeObservation secondCurrent), Is.True);
        Assert.That(firstCurrent.Exists, Is.EqualTo(secondCurrent.Exists));
    }

    [Test]
    public void FreshnessUsesObservedDayAndRejectsInvalidCurrentDay()
    {
        PoliticalKnowledgeRuntime knowledge = new PoliticalKnowledgeRuntime(new InstitutionId("institution.reader"));
        FactionKnowledgeObservation observation = new FactionKnowledgeObservation(
            new FactionId("faction.known"),
            true,
            10L,
            12L,
            DirectProvenance());
        knowledge.RecordFactionObservation(observation);
        PoliticalKnowledgePolicy policy = new PoliticalKnowledgePolicy(2, 6);

        Assert.That(policy.GetAgeDays(observation, 11L), Is.EqualTo(1L));
        Assert.That(policy.IsFresh(observation, 12L), Is.True);
        Assert.That(policy.GetFreshness(observation, 14L), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(policy.GetFreshness(observation, 16L), Is.EqualTo(0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => observation.GetAgeDays(-1L));
    }

    [Test]
    public void InvalidIdentityAndDayInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PoliticalKnowledgeHolder.ForPerson(null));
        Assert.Throws<ArgumentNullException>(() => new PoliticalKnowledgeRuntime((PoliticalKnowledgeHolder)null));
        Assert.Throws<ArgumentNullException>(() => new FactionKnowledgeObservation(
            null,
            true,
            0L,
            0L,
            DirectProvenance()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FactionKnowledgeObservation(
            new FactionId("faction.invalid"),
            true,
            -1L,
            0L,
            DirectProvenance()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FactionKnowledgeObservation(
            new FactionId("faction.invalid"),
            true,
            2L,
            1L,
            DirectProvenance()));
        Assert.Throws<ArgumentException>(() => new PersonDeathKnowledgeObservation(
            new PersonId("person.invalid"),
            false,
            1L,
            1L,
            1L,
            DirectProvenance()));
        Assert.Throws<ArgumentException>(() => new PoliticalKnowledgeProvenance(
            PoliticalKnowledgeSource.SharedByPerson));
        Assert.Throws<ArgumentException>(() => new PoliticalKnowledgeProvenance(
            PoliticalKnowledgeSource.SharedByPerson,
            sourcePersonId: new PersonId("person.source"),
            sourceInstitutionId: new InstitutionId("institution.source")));
    }

    [Test]
    public void CloneAndReadOnlySnapshotsDoNotShareMutableCollectionState()
    {
        PoliticalKnowledgeRuntime original = new PoliticalKnowledgeRuntime(new PersonId("person.original"));
        FactionId factionId = new FactionId("faction.clone");
        Assert.That(original.RecordFactionObservation(new FactionKnowledgeObservation(
            factionId,
            true,
            0L,
            0L,
            DirectProvenance())), Is.True);

        PoliticalKnowledgeRuntime clone = original.Clone();
        Assert.That(clone.RecordFactionObservation(new FactionKnowledgeObservation(
            factionId,
            false,
            1L,
            1L,
            DirectProvenance())), Is.True);

        Assert.That(original.TryGetLatestFactionObservation(factionId, out FactionKnowledgeObservation originalObservation), Is.True);
        Assert.That(clone.TryGetLatestFactionObservation(factionId, out FactionKnowledgeObservation cloneObservation), Is.True);
        Assert.That(originalObservation.Exists, Is.True);
        Assert.That(cloneObservation.Exists, Is.False);
        Assert.That(original.FactionObservations, Is.TypeOf<ReadOnlyCollection<FactionKnowledgeObservation>>());
        Assert.Throws<NotSupportedException>(() => ((IList<FactionKnowledgeObservation>)original.FactionObservations).Add(cloneObservation));
    }

    [Test]
    public void NonMaterializedPersonsCanHoldAndReceivePoliticalKnowledge()
    {
        PersonId holderId = new PersonId("person.dormant");
        PersonId subjectId = new PersonId("person.subject");
        PersonStore persons = new PersonStore();
        PersonRuntime holder = new PersonRuntime(holderId, 0L);
        Assert.That(persons.TryRegister(holder, out PersonStoreFailure failure), Is.True, failure.ToString());
        Assert.That(holder.IsMaterialized, Is.False);

        PoliticalKnowledgeRuntime knowledge = new PoliticalKnowledgeRuntime(holderId);
        Assert.That(knowledge.RecordPersonDeathObservation(new PersonDeathKnowledgeObservation(
            subjectId,
            true,
            4L,
            4L,
            5L,
            DirectProvenance())), Is.True);

        Assert.That(knowledge.TryGetCurrentPersonDeathObservation(subjectId, out PersonDeathKnowledgeObservation observation), Is.True);
        Assert.That(observation.PersonId, Is.EqualTo(subjectId));
        Assert.That(observation.IsDead, Is.True);
        Assert.That(holder.IsMaterialized, Is.False);
    }

    private static PoliticalKnowledgeProvenance DirectProvenance()
    {
        return new PoliticalKnowledgeProvenance(
            PoliticalKnowledgeSource.DirectObservation,
            "field-observation");
    }

    private static PoliticalKnowledgeProvenance InitialProvenance()
    {
        return new PoliticalKnowledgeProvenance(
            PoliticalKnowledgeSource.InitialScenarioKnowledge,
            "scenario");
    }

    private static PoliticalKnowledgeProvenance SharedByPersonProvenance(string personId)
    {
        return new PoliticalKnowledgeProvenance(
            PoliticalKnowledgeSource.SharedByPerson,
            "transmission",
            new PersonId(personId));
    }
}
