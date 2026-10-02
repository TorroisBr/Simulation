using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;

public sealed class FactionFactualReaderTests
{
    [Test]
    public void ReaderCopiesFactionAndActiveAffiliationFactsInOrdinalOrder()
    {
        PersonStore people = new PersonStore();
        FactionStore factions = new FactionStore(people);
        PersonRuntime personLower = RegisterPerson(people, "ipek");
        PersonRuntime personUpper = RegisterPerson(people, "Ipek");
        PersonRuntime personDotless = RegisterPerson(people, "ıpek");
        PersonRuntime formerPerson = RegisterPerson(people, "former");
        FactionRecord zeta = RegisterFaction(factions, "zeta", "Zeta", 2L);
        FactionRecord dotless = RegisterFaction(factions, "ıstanbul", "  ", 0L);
        FactionRecord lower = RegisterFaction(factions, "istanbul", "Istanbul", 1L, FactionMembershipPolicy.LeaveNoRejoin, false);
        FactionRecord upper = RegisterFaction(factions, "Istanbul", "Upper", 0L, FactionMembershipPolicy.CannotLeave, true);

        RegisterAffiliation(factions, upper.Id, personLower.PersonId, 4L, "aff-upper-lower");
        RegisterAffiliation(factions, upper.Id, personUpper.PersonId, 2L, "aff-upper-upper");
        RegisterAffiliation(factions, dotless.Id, personDotless.PersonId, 3L, "aff-dotless");
        RegisterAffiliation(factions, lower.Id, personLower.PersonId, 5L, "aff-lower");
        RegisterAffiliation(factions, zeta.Id, formerPerson.PersonId, 2L, "aff-ended", 3L);

        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            FactualReadCoordinator coordinator = CreateCoordinator(factions, people, 42L);
            Assert.That(coordinator.TryCaptureCoherent(
                out FactualReadCapture capture,
                FactionFactualReader.FactionTruthCapabilityId), Is.True);

            Assert.That(capture.LogicalBoundary, Is.EqualTo(42L));
            Assert.That(capture.FactionStoreRevision, Is.EqualTo(factions.Revision));
            Assert.That(capture.PersonStoreRevision, Is.EqualTo(people.Revision));
            Assert.That(capture.SourceVersions, Has.Count.EqualTo(1));
            Assert.That(capture.SourceVersions[0].CapabilityId, Is.EqualTo("simulation.faction-truth/v1"));
            Assert.That(capture.SourceVersions[0].Version, Is.EqualTo(1));
            Assert.That(capture.TryGet<FactionTruthFacts>(
                FactionFactualReader.FactionTruthCapabilityId,
                out FactReadResult<FactionTruthFacts> result), Is.True);
            Assert.That(result.Status, Is.EqualTo(FactReadStatus.Present));

            FactionTruthFacts truth = result.Value;
            Assert.That(GetFactionIds(truth), Is.EqualTo(new[] { "Istanbul", "istanbul", "zeta", "ıstanbul" }));
            Assert.That(truth.Factions[0].DisplayName, Is.EqualTo("Upper"));
            Assert.That(truth.Factions[0].CreatedAbsoluteDay, Is.EqualTo(0L));
            Assert.That(truth.Factions[0].MembershipPolicy, Is.EqualTo(FactionMembershipPolicy.CannotLeave));
            Assert.That(truth.Factions[0].ExpulsionAllowed, Is.True);
            Assert.That(truth.Factions[1].DisplayName, Is.EqualTo("Istanbul"));
            Assert.That(truth.Factions[1].MembershipPolicy, Is.EqualTo(FactionMembershipPolicy.LeaveNoRejoin));
            Assert.That(truth.Factions[1].ExpulsionAllowed, Is.False);
            Assert.That(truth.Factions[3].DisplayName, Is.Null);
            Assert.That(truth.GetFaction(new FactionId("Istanbul")).Status, Is.EqualTo(FactReadStatus.Present));
            Assert.That(truth.GetFaction(new FactionId("missing")).Status, Is.EqualTo(FactReadStatus.Absent));

            Assert.That(GetAffiliationIds(truth), Is.EqualTo(new[]
            {
                "Istanbul|Ipek|2|aff-upper-upper",
                "Istanbul|ipek|4|aff-upper-lower",
                "istanbul|ipek|5|aff-lower",
                "ıstanbul|ıpek|3|aff-dotless"
            }));
            Assert.That(Array.Exists(GetAffiliationIds(truth), value => value.Contains("aff-ended")), Is.False);
            Assert.That(truth.ActiveAffiliations[0].PersonId.Value, Is.EqualTo("Ipek"));
            Assert.That(truth.ActiveAffiliations[0].PersonId, Is.Not.SameAs(personUpper.PersonId));

            IList<FactionFact> factionView = truth.Factions as IList<FactionFact>;
            IList<ActiveFactionAffiliationFact> affiliationView = truth.ActiveAffiliations as IList<ActiveFactionAffiliationFact>;
            Assert.That(factionView, Is.Not.Null);
            Assert.That(factionView.IsReadOnly, Is.True);
            Assert.That(affiliationView, Is.Not.Null);
            Assert.That(affiliationView.IsReadOnly, Is.True);

            RegisterFaction(factions, "after-capture", "After", 8L);
            Assert.That(truth.Factions, Has.Count.EqualTo(4));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Test]
    public void EmptyFactionOwnerProducesPresentEmptyCollections()
    {
        PersonStore people = new PersonStore();
        FactionStore factions = new FactionStore(people);
        FactualReadCoordinator coordinator = CreateCoordinator(factions, people, 0L);

        Assert.That(coordinator.TryCaptureCoherent(
            out FactualReadCapture capture,
            FactionFactualReader.FactionTruthCapabilityId), Is.True);
        Assert.That(capture.TryGet<FactionTruthFacts>(
            FactionFactualReader.FactionTruthCapabilityId,
            out FactReadResult<FactionTruthFacts> result), Is.True);
        Assert.That(result.Status, Is.EqualTo(FactReadStatus.Present));
        Assert.That(result.Value.Factions, Is.Empty);
        Assert.That(result.Value.ActiveAffiliations, Is.Empty);
    }

    [Test]
    public void MissingPersonEndpointFailsClosedWithDiagnosticAndNoPartialFacts()
    {
        PersonStore people = new PersonStore();
        FactionStore factions = new FactionStore(people);
        FactionRecord faction = RegisterFaction(factions, "faction", "Faction", 0L);
        PersonRuntime orphan = RegisterPerson(people, "orphan");
        RegisterAffiliation(factions, faction.Id, orphan.PersonId, 0L, "aff-orphan");
        Assert.That(people.TryRollbackRegistration(orphan), Is.True);
        FactualReadCoordinator coordinator = CreateCoordinator(factions, people, 0L);

        Assert.That(coordinator.TryCaptureCoherent(
            out FactualReadCapture capture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(capture.TryGet<FactionTruthFacts>(
            FactionFactualReader.FactionTruthCapabilityId,
            out FactReadResult<FactionTruthFacts> result), Is.True);
        Assert.That(result.Status, Is.EqualTo(FactReadStatus.Unavailable));
        Assert.That(capture.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(capture.Diagnostics[0].CapabilityId, Is.EqualTo(FactionFactualReader.FactionTruthCapabilityId));
        Assert.That(capture.Diagnostics[0].Code, Is.EqualTo("faction.active-affiliation.person-endpoint-missing"));
        Assert.That(capture.Diagnostics[0].Message, Does.Not.Contain("orphan"));
    }

    [Test]
    public void DuplicateActivePairAndMissingFactionEndpointFailClosed()
    {
        PersonStore people = new PersonStore();
        FactionStore factions = new FactionStore(people);
        FactionRecord faction = RegisterFaction(factions, "faction", "Faction", 0L);
        PersonRuntime person = RegisterPerson(people, "person");
        RegisterAffiliation(factions, faction.Id, person.PersonId, 0L, "aff-first");
        AddCorruptAffiliation(factions, new FactionAffiliationRecord(
            faction.Id, person.PersonId, 1L, affiliationId: new FactionAffiliationId("aff-second")));

        FactualReadCoordinator duplicateCoordinator = CreateCoordinator(factions, people, 1L);
        Assert.That(duplicateCoordinator.TryCaptureCoherent(
            out FactualReadCapture duplicateCapture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(duplicateCapture.Diagnostics[0].Code, Is.EqualTo("faction.active-affiliation.duplicate-pair"));

        PersonStore otherPeople = new PersonStore();
        FactionStore orphanFactionStore = new FactionStore(otherPeople);
        PersonRuntime validPerson = RegisterPerson(otherPeople, "person");
        AddCorruptAffiliation(orphanFactionStore, new FactionAffiliationRecord(
            new FactionId("missing-faction"), validPerson.PersonId, 0L,
            affiliationId: new FactionAffiliationId("aff-orphan-faction")));
        FactualReadCoordinator orphanCoordinator = CreateCoordinator(orphanFactionStore, otherPeople, 0L);
        Assert.That(orphanCoordinator.TryCaptureCoherent(
            out FactualReadCapture orphanCapture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(orphanCapture.Diagnostics[0].Code, Is.EqualTo("faction.affiliation.faction-endpoint-missing"));
    }

    [Test]
    public void AffiliationBeforeFactionCreationIsUnavailable()
    {
        PersonStore people = new PersonStore();
        FactionStore factions = new FactionStore(people);
        FactionRecord faction = RegisterFaction(factions, "faction", "Faction", 10L);
        PersonRuntime person = RegisterPerson(people, "person");
        RegisterAffiliation(factions, faction.Id, person.PersonId, 5L, "aff-before-created");
        FactualReadCoordinator coordinator = CreateCoordinator(factions, people, 10L);

        Assert.That(coordinator.TryCaptureCoherent(
            out FactualReadCapture capture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(capture.Diagnostics[0].Code, Is.EqualTo("faction.affiliation.joined-before-faction-created"));
    }

    [Test]
    public void EndedAffiliationsStillRequireFactionEndpointAndValidJoinChronology()
    {
        PersonStore orphanPeople = new PersonStore();
        FactionStore orphanFactionStore = new FactionStore(orphanPeople);
        PersonRuntime orphanPerson = RegisterPerson(orphanPeople, "orphan-ended");
        AddCorruptAffiliation(orphanFactionStore, new FactionAffiliationRecord(
            new FactionId("missing-faction"), orphanPerson.PersonId, 0L, 1L,
            new FactionAffiliationId("ended-orphan"), FactionAffiliationEndReason.VoluntaryLeave));
        FactualReadCoordinator orphanCoordinator = CreateCoordinator(orphanFactionStore, orphanPeople, 1L);
        Assert.That(orphanCoordinator.TryCaptureCoherent(
            out FactualReadCapture orphanCapture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(orphanCapture.Diagnostics[0].Code, Is.EqualTo("faction.affiliation.faction-endpoint-missing"));

        PersonStore chronologyPeople = new PersonStore();
        FactionStore chronologyStore = new FactionStore(chronologyPeople);
        FactionRecord lateFaction = RegisterFaction(chronologyStore, "late-faction", "Late", 10L);
        PersonRuntime chronologyPerson = RegisterPerson(chronologyPeople, "ended-before-faction");
        RegisterAffiliation(chronologyStore, lateFaction.Id, chronologyPerson.PersonId, 5L, "ended-before-created", 6L);
        FactualReadCoordinator chronologyCoordinator = CreateCoordinator(chronologyStore, chronologyPeople, 10L);
        Assert.That(chronologyCoordinator.TryCaptureCoherent(
            out FactualReadCapture chronologyCapture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(chronologyCapture.Diagnostics[0].Code, Is.EqualTo("faction.affiliation.joined-before-faction-created"));
    }

    [Test]
    public void FutureFactionCreationAndAffiliationDatesAreUnavailable()
    {
        PersonStore futureFactionPeople = new PersonStore();
        FactionStore futureFactionStore = new FactionStore(futureFactionPeople);
        RegisterFaction(futureFactionStore, "future-faction", "Future", 1L);
        FactualReadCoordinator futureFactionCoordinator = CreateCoordinator(futureFactionStore, futureFactionPeople, 0L);
        Assert.That(futureFactionCoordinator.TryCaptureCoherent(
            out FactualReadCapture futureFactionCapture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(futureFactionCapture.Diagnostics[0].Code, Is.EqualTo("faction.record.created-after-boundary"));

        PersonStore futureJoinPeople = new PersonStore();
        FactionStore futureJoinStore = new FactionStore(futureJoinPeople);
        FactionRecord joinedFaction = RegisterFaction(futureJoinStore, "faction", "Faction", 0L);
        PersonRuntime futureJoinPerson = RegisterPerson(futureJoinPeople, "future-join");
        RegisterAffiliation(futureJoinStore, joinedFaction.Id, futureJoinPerson.PersonId, 1L, "aff-future-join");
        FactualReadCoordinator futureJoinCoordinator = CreateCoordinator(futureJoinStore, futureJoinPeople, 0L);
        Assert.That(futureJoinCoordinator.TryCaptureCoherent(
            out FactualReadCapture futureJoinCapture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(futureJoinCapture.Diagnostics[0].Code, Is.EqualTo("faction.affiliation.joined-after-boundary"));

        PersonStore futureEndPeople = new PersonStore();
        FactionStore futureEndStore = new FactionStore(futureEndPeople);
        FactionRecord endedFaction = RegisterFaction(futureEndStore, "faction", "Faction", 0L);
        PersonRuntime futureEndPerson = RegisterPerson(futureEndPeople, "future-end");
        RegisterAffiliation(futureEndStore, endedFaction.Id, futureEndPerson.PersonId, 0L, "aff-future-end", 1L);
        FactualReadCoordinator futureEndCoordinator = CreateCoordinator(futureEndStore, futureEndPeople, 0L);
        Assert.That(futureEndCoordinator.TryCaptureCoherent(
            out FactualReadCapture futureEndCapture,
            FactionFactualReader.FactionTruthCapabilityId), Is.False);
        Assert.That(futureEndCapture.Diagnostics[0].Code, Is.EqualTo("faction.affiliation.ended-after-boundary"));
    }

    private static FactualReadCoordinator CreateCoordinator(FactionStore factions, PersonStore people, long logicalBoundary)
    {
        RuntimeState runtime = new RuntimeState(logicalBoundary);
        FactualReadAdmission admission = new FactualReadAdmission(runtime);
        Assert.That(admission.TryBindStores(factions, people), Is.True);
        return new FactualReadCoordinator(
            admission,
            new IFactualReader[] { new FactionFactualReader(factions, people) });
    }

    private static PersonRuntime RegisterPerson(PersonStore people, string id)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id));
        Assert.That(people.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static FactionRecord RegisterFaction(
        FactionStore store,
        string id,
        string displayName,
        long createdAbsoluteDay,
        FactionMembershipPolicy membershipPolicy = FactionMembershipPolicy.LeaveAndRejoin,
        bool expulsionAllowed = true)
    {
        FactionRecord faction = new FactionRecord(
            new FactionId(id), displayName, createdAbsoluteDay, membershipPolicy, expulsionAllowed);
        Assert.That(store.TryRegister(faction, out FactionFoundationFailure failure), Is.True, failure.ToString());
        return faction;
    }

    private static FactionAffiliationRecord RegisterAffiliation(
        FactionStore factions,
        FactionId factionId,
        PersonId personId,
        long joinedAbsoluteDay,
        string affiliationId,
        long? endedAbsoluteDay = null)
    {
        FactionAffiliationRecord affiliation = new FactionAffiliationRecord(
            factionId,
            personId,
            joinedAbsoluteDay,
            endedAbsoluteDay,
            new FactionAffiliationId(affiliationId),
            endedAbsoluteDay.HasValue ? FactionAffiliationEndReason.VoluntaryLeave : (FactionAffiliationEndReason?)null);
        Assert.That(factions.TryRegisterAffiliation(affiliation, out FactionFoundationFailure failure), Is.True, failure.ToString());
        return affiliation;
    }

    private static void AddCorruptAffiliation(FactionStore store, FactionAffiliationRecord affiliation)
    {
        FieldInfo field = typeof(FactionStore).GetField("affiliationsById", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        Dictionary<string, FactionAffiliationRecord> records =
            (Dictionary<string, FactionAffiliationRecord>)field.GetValue(store);
        records.Add(affiliation.AffiliationId.Value, affiliation);
    }

    private static string[] GetFactionIds(FactionTruthFacts truth)
    {
        string[] ids = new string[truth.Factions.Count];
        for (int i = 0; i < ids.Length; i++) ids[i] = truth.Factions[i].FactionId.Value;
        return ids;
    }

    private static string[] GetAffiliationIds(FactionTruthFacts truth)
    {
        string[] ids = new string[truth.ActiveAffiliations.Count];
        for (int i = 0; i < ids.Length; i++)
        {
            ActiveFactionAffiliationFact fact = truth.ActiveAffiliations[i];
            ids[i] = fact.FactionId.Value + "|" + fact.PersonId.Value + "|"
                + fact.JoinedAbsoluteDay + "|" + fact.FactionAffiliationId.Value;
        }
        return ids;
    }

    private sealed class RuntimeState : IFactualReadRuntimeState
    {
        private long logicalBoundary;

        internal RuntimeState(long logicalBoundary)
        {
            this.logicalBoundary = logicalBoundary;
            AdmissionContext = SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        }

        public SimulationRuntimeAdmissionContext AdmissionContext { get; }
        public bool IsWorldPublished => true;
        public bool IsHealthy => true;
        public bool IsBootstrapOrAdvanceActive => false;

        public bool TryReadCompletedLogicalBoundary(out long boundary)
        {
            boundary = logicalBoundary;
            return true;
        }
    }
}
