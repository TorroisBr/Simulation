using System;
using NUnit.Framework;

public sealed class FactionFoundationTests
{
    [Test]
    public void FactionAffiliationsUseFactionIdAndPersonIdAndRemainSeparateFromNpcRuntime()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        FactionId factionId = RegisterFaction(world, "faction.council");

        Assert.That(world.TryProposeFactionAffiliation(
            factionId,
            person.PersonId,
            out FactionAffiliationAddTransition proposal,
            out FactionFoundationFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(world.TryApplyFactionAffiliation(proposal, out FactionFoundationFailure applyFailure), Is.True, applyFailure.ToString());

        FactionAffiliationRecord affiliation = world.FactionAffiliationRecords[0];
        Assert.That(affiliation.FactionId, Is.EqualTo(factionId));
        Assert.That(affiliation.PersonId, Is.EqualTo(person.PersonId));
        Assert.That(typeof(FactionAffiliationRecord).GetProperty("NpcRuntimeId"), Is.Null);
        Assert.That(world.FactionRecords[0].Id, Is.EqualTo(factionId));
    }

    [Test]
    public void LeaveAndRejoinCreatesDistinctHistoricalTenures()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        FactionId factionId = RegisterFaction(world, "faction.rejoin");

        Assert.That(world.TryProposeFactionAffiliation(factionId, person.PersonId, out FactionAffiliationAddTransition firstAdd, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(firstAdd, out _), Is.True);
        Assert.That(world.TryProposeFactionAffiliationEnd(factionId, person.PersonId, out FactionAffiliationEndTransition end, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliationEnd(end, out _), Is.True);
        world.AdvanceDay();
        Assert.That(world.TryProposeFactionAffiliation(factionId, person.PersonId, out FactionAffiliationAddTransition secondAdd, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(secondAdd, out _), Is.True);

        Assert.That(world.FactionAffiliationRecords, Has.Count.EqualTo(2));
        Assert.That(world.FactionAffiliationRecords[0].AffiliationId, Is.Not.EqualTo(world.FactionAffiliationRecords[1].AffiliationId));
        Assert.That(world.FactionAffiliationRecords[0].IsActive, Is.False);
        Assert.That(world.FactionAffiliationRecords[0].EndReason, Is.EqualTo(FactionAffiliationEndReason.VoluntaryLeave));
        Assert.That(world.FactionAffiliationRecords[1].IsActive, Is.True);
    }

    [Test]
    public void SameDayLeaveAndRejoinStillGetsDistinctTenureIds()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        FactionId factionId = RegisterFaction(world, "faction.same-day-rejoin");

        Assert.That(world.TryProposeFactionAffiliation(factionId, person.PersonId, out FactionAffiliationAddTransition firstAdd, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(firstAdd, out _), Is.True);
        Assert.That(world.TryProposeFactionAffiliationEnd(factionId, person.PersonId, out FactionAffiliationEndTransition end, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliationEnd(end, out _), Is.True);
        Assert.That(world.TryProposeFactionAffiliation(factionId, person.PersonId, out FactionAffiliationAddTransition secondAdd, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(secondAdd, out _), Is.True);

        Assert.That(world.FactionAffiliationRecords, Has.Count.EqualTo(2));
        Assert.That(world.FactionAffiliationRecords[0].AffiliationId, Is.Not.EqualTo(world.FactionAffiliationRecords[1].AffiliationId));
    }

    [Test]
    public void LeaveNoRejoinPolicyRejectsSecondTenureAndCannotLeaveRejectsVoluntaryEnd()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        FactionId noRejoin = new FactionId("faction.no-rejoin");
        FactionId cannotLeave = new FactionId("faction.cannot-leave");
        Assert.That(world.TryRegisterFaction(new FactionRecord(
            noRejoin, "No Rejoin", 0L, FactionMembershipPolicy.LeaveNoRejoin), out _), Is.True);
        Assert.That(world.TryRegisterFaction(new FactionRecord(
            cannotLeave, "Cannot Leave", 0L, FactionMembershipPolicy.CannotLeave), out _), Is.True);

        Assert.That(world.TryProposeFactionAffiliation(noRejoin, person.PersonId, out FactionAffiliationAddTransition add, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(add, out _), Is.True);
        world.AdvanceDay();
        Assert.That(world.TryProposeFactionAffiliationEnd(noRejoin, person.PersonId, out FactionAffiliationEndTransition end, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliationEnd(end, out _), Is.True);
        world.AdvanceDay();
        Assert.That(world.TryProposeFactionAffiliation(noRejoin, person.PersonId, out _, out FactionFoundationFailure noRejoinFailure), Is.False);
        Assert.That(noRejoinFailure.Code, Is.EqualTo(FactionFoundationFailureCode.DuplicateAffiliation));

        Assert.That(world.TryProposeFactionAffiliation(cannotLeave, person.PersonId, out FactionAffiliationAddTransition cannotLeaveAdd, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(cannotLeaveAdd, out _), Is.True);
        Assert.That(world.TryProposeFactionAffiliationEnd(cannotLeave, person.PersonId, out _, out FactionFoundationFailure leaveFailure), Is.False);
        Assert.That(leaveFailure.Code, Is.EqualTo(FactionFoundationFailureCode.VoluntaryLeaveNotAllowed));
    }

    [Test]
    public void ExpulsionIsAnExplicitPolicyControlledEndReason()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        FactionId allowed = new FactionId("faction.expulsion-allowed");
        FactionId disallowed = new FactionId("faction.expulsion-disallowed");
        Assert.That(world.TryRegisterFaction(new FactionRecord(allowed, "Allowed", 0L, FactionMembershipPolicy.CannotLeave, true), out _), Is.True);
        Assert.That(world.TryRegisterFaction(new FactionRecord(disallowed, "Disallowed", 0L, FactionMembershipPolicy.CannotLeave, false), out _), Is.True);

        Assert.That(world.TryProposeFactionAffiliation(allowed, person.PersonId, out FactionAffiliationAddTransition allowedAdd, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(allowedAdd, out _), Is.True);
        Assert.That(world.TryProposeFactionAffiliationExpulsion(allowed, person.PersonId, out FactionAffiliationEndTransition expulsion, out _), Is.True);
        Assert.That(expulsion.IsExpulsion, Is.True);
        Assert.That(expulsion.EndReason, Is.EqualTo(FactionAffiliationEndReason.Expulsion));
        Assert.That(world.TryApplyFactionAffiliationEnd(expulsion, out _), Is.True);
        Assert.That(world.FactionAffiliationRecords[0].EndReason, Is.EqualTo(FactionAffiliationEndReason.Expulsion));

        Assert.That(world.TryProposeFactionAffiliation(disallowed, person.PersonId, out FactionAffiliationAddTransition disallowedAdd, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(disallowedAdd, out _), Is.True);
        Assert.That(world.TryProposeFactionAffiliationExpulsion(disallowed, person.PersonId, out _, out FactionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(FactionFoundationFailureCode.ExpulsionNotAllowed));
    }

    [Test]
    public void ActiveFactionAffiliationUniquenessIsEnforcedByStableTenureStore()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        FactionStore store = new FactionStore(persons);
        FactionId factionId = new FactionId("faction.unique");
        Assert.That(store.TryRegister(new FactionRecord(factionId, "Unique", 0L), out _), Is.True);
        Assert.That(store.TryRegisterAffiliation(
            new FactionAffiliationRecord(factionId, person.PersonId, 0L), out _), Is.True);
        Assert.That(store.TryRegisterAffiliation(
            new FactionAffiliationRecord(factionId, person.PersonId, 1L), out FactionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(FactionFoundationFailureCode.DuplicateAffiliation));
    }

    [Test]
    public void WorldRejectsAffiliationForMissingFactionOrPerson()
    {
        PersonStore persons = new PersonStore();
        RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);

        Assert.That(world.TryProposeFactionAffiliation(
            new FactionId("missing.faction"),
            new PersonId("person.member"),
            out _,
            out FactionFoundationFailure missingFactionFailure), Is.False);
        Assert.That(missingFactionFailure.Code, Is.EqualTo(FactionFoundationFailureCode.FactionNotRegistered));

        FactionId factionId = RegisterFaction(world, "faction.council");
        Assert.That(world.TryProposeFactionAffiliation(
            factionId,
            new PersonId("missing.person"),
            out _,
            out FactionFoundationFailure missingPersonFailure), Is.False);
        Assert.That(missingPersonFailure.Code, Is.EqualTo(FactionFoundationFailureCode.PersonNotRegistered));
    }

    [Test]
    public void WorldClonesFactionStoreAndDoesNotExposeMutableSourceAuthority()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        FactionStore source = new FactionStore(persons);
        FactionId factionId = new FactionId("faction.council");
        Assert.That(source.TryRegister(new FactionRecord(factionId, "Council", 0L), out _), Is.True);
        Assert.That(source.TryRegisterAffiliation(
            new FactionAffiliationRecord(factionId, person.PersonId, 0L),
            out _), Is.True);

        SimulationRuntime world = CreateWorld(persons, source);
        Assert.That(world.FactionRecords, Has.Count.EqualTo(1));
        Assert.That(world.FactionAffiliationRecords, Has.Count.EqualTo(1));

        Assert.That(source.TryRegister(new FactionRecord(new FactionId("faction.second"), "Second", 0L), out _), Is.True);
        Assert.That(source.Factions, Has.Count.EqualTo(2));
        Assert.That(world.FactionRecords, Has.Count.EqualTo(1));
    }

    [Test]
    public void CrossWorldAffiliationTransitionsAreRejected()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime firstWorld = CreateWorld(persons);
        PersonStore secondPersons = new PersonStore();
        Assert.That(secondPersons.TryRegister(new PersonRuntime(person.PersonId), out _), Is.True);
        SimulationRuntime secondWorld = CreateWorld(secondPersons);
        FactionId factionId = RegisterFaction(firstWorld, "faction.council");
        RegisterFaction(secondWorld, "faction.council");

        Assert.That(firstWorld.TryProposeFactionAffiliation(
            factionId,
            person.PersonId,
            out FactionAffiliationAddTransition firstAdd,
            out _), Is.True);
        Assert.That(secondWorld.TryApplyFactionAffiliation(firstAdd, out FactionFoundationFailure crossWorldAddFailure), Is.False);
        Assert.That(crossWorldAddFailure.Code, Is.EqualTo(FactionFoundationFailureCode.WrongFactionStore));
        Assert.That(firstWorld.TryApplyFactionAffiliation(firstAdd, out _), Is.True);

        Assert.That(secondWorld.TryProposeFactionAffiliation(
            factionId,
            person.PersonId,
            out FactionAffiliationAddTransition secondAdd,
            out _), Is.True);
        Assert.That(secondWorld.TryApplyFactionAffiliation(secondAdd, out _), Is.True);

        Assert.That(firstWorld.TryProposeFactionAffiliationEnd(
            factionId,
            person.PersonId,
            out FactionAffiliationEndTransition firstEnd,
            out _), Is.True);
        Assert.That(secondWorld.TryApplyFactionAffiliationEnd(firstEnd, out FactionFoundationFailure crossWorldEndFailure), Is.False);
        Assert.That(crossWorldEndFailure.Code, Is.EqualTo(FactionFoundationFailureCode.WrongFactionStore));
    }

    [Test]
    public void FactionStoreRejectsMalformedDirectAffiliationEndpoints()
    {
        PersonStore persons = new PersonStore();
        FactionStore store = new FactionStore(persons);
        Assert.That(store.TryRegisterAffiliation(
            new FactionAffiliationRecord(new FactionId("missing.faction"), new PersonId("missing.person"), 0L),
            out FactionFoundationFailure missingFactionFailure), Is.False);
        Assert.That(missingFactionFailure.Code, Is.EqualTo(FactionFoundationFailureCode.FactionNotRegistered));

        FactionId factionId = new FactionId("faction.council");
        Assert.That(store.TryRegister(new FactionRecord(factionId, "Council", 0L), out _), Is.True);
        Assert.That(store.TryRegisterAffiliation(
            new FactionAffiliationRecord(factionId, new PersonId("missing.person"), 0L),
            out FactionFoundationFailure missingPersonFailure), Is.False);
        Assert.That(missingPersonFailure.Code, Is.EqualTo(FactionFoundationFailureCode.PersonNotRegistered));
    }

    [Test]
    public void AffiliationTransitionsRejectStaleStoreAndWorldDayState()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        FactionId factionId = RegisterFaction(world, "faction.council");

        Assert.That(world.TryProposeFactionAffiliation(
            factionId,
            person.PersonId,
            out FactionAffiliationAddTransition staleProposal,
            out _), Is.True);
        RegisterFaction(world, "faction.second");
        Assert.That(world.TryApplyFactionAffiliation(staleProposal, out FactionFoundationFailure staleFailure), Is.False);
        Assert.That(staleFailure.Code, Is.EqualTo(FactionFoundationFailureCode.StaleAffiliation));

        Assert.That(world.TryProposeFactionAffiliation(
            factionId,
            person.PersonId,
            out FactionAffiliationAddTransition addProposal,
            out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(addProposal, out _), Is.True);
        Assert.That(world.TryProposeFactionAffiliationEnd(
            factionId,
            person.PersonId,
            out FactionAffiliationEndTransition endProposal,
            out _), Is.True);

        world.SimulationTime.AdvanceDay();
        Assert.That(world.TryApplyFactionAffiliationEnd(endProposal, out FactionFoundationFailure staleDayFailure), Is.False);
        Assert.That(staleDayFailure.Code, Is.EqualTo(FactionFoundationFailureCode.StaleAffiliation));

        Assert.That(world.TryProposeFactionAffiliationEnd(
            factionId,
            person.PersonId,
            out endProposal,
            out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliationEnd(endProposal, out _), Is.True);
        Assert.That(world.FactionAffiliationRecords[0].EndedAbsoluteDay, Is.EqualTo(1L));
    }

    [Test]
    public void WorldRejectsFutureFactionCreationAndFutureAffiliationClone()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        Assert.That(world.TryRegisterFaction(
            new FactionRecord(new FactionId("faction.future"), "Future", 1L),
            out FactionFoundationFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(FactionFoundationFailureCode.InvalidCreationAbsoluteDay));

        FactionStore source = new FactionStore(persons);
        FactionId factionId = new FactionId("faction.council");
        Assert.That(source.TryRegister(new FactionRecord(factionId, "Council", 0L), out _), Is.True);
        Assert.That(source.TryRegisterAffiliation(
            new FactionAffiliationRecord(factionId, person.PersonId, 1L),
            out _), Is.True);

        Assert.Throws<ArgumentException>(() => CreateWorld(persons, source));
    }

    [Test]
    public void FactionsAndAffiliationsParticipateInDeterministicDiagnostics()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime person = RegisterPerson(persons, "person.member");
        SimulationRuntime world = CreateWorld(persons);
        FactionId factionId = RegisterFaction(world, "faction.council");
        Assert.That(world.TryProposeFactionAffiliation(factionId, person.PersonId, out FactionAffiliationAddTransition add, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliation(add, out _), Is.True);

        WorldStateSnapshot before = Capture(world);
        Assert.That(WorldStateDiagnostics.Export(before), Does.Contain("FACTION"));
        Assert.That(WorldStateInvariantValidator.Validate(before).IsValid, Is.True);

        Assert.That(world.TryProposeFactionAffiliationEnd(factionId, person.PersonId, out FactionAffiliationEndTransition end, out _), Is.True);
        Assert.That(world.TryApplyFactionAffiliationEnd(end, out _), Is.True);
        WorldStateSnapshot after = Capture(world);
        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, after);
        string affiliationIdentity = world.FactionAffiliationRecords[0].AffiliationId.Value;

        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(difference =>
            difference.Section == "FactionAffiliation"
            && difference.Identity == affiliationIdentity
            && difference.Field == "EndedAbsoluteDay"));
        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(difference =>
            difference.Section == "FactionAffiliation"
            && difference.Identity == affiliationIdentity
            && difference.Field == "EndReason"));
        Assert.That(WorldStateInvariantValidator.Validate(after).IsValid, Is.True);
    }

    [Test]
    public void FactionDiagnosticOutputIsIndependentOfRegistrationOrder()
    {
        PersonStore firstPersons = new PersonStore();
        PersonRuntime firstPerson = RegisterPerson(firstPersons, "person.member");
        SimulationRuntime firstWorld = CreateWorld(firstPersons);
        FactionId firstA = RegisterFaction(firstWorld, "faction.a");
        FactionId firstB = RegisterFaction(firstWorld, "faction.b");
        Assert.That(firstWorld.TryProposeFactionAffiliation(firstB, firstPerson.PersonId, out FactionAffiliationAddTransition firstAddB, out _), Is.True);
        Assert.That(firstWorld.TryApplyFactionAffiliation(firstAddB, out _), Is.True);
        Assert.That(firstWorld.TryProposeFactionAffiliation(firstA, firstPerson.PersonId, out FactionAffiliationAddTransition firstAddA, out _), Is.True);
        Assert.That(firstWorld.TryApplyFactionAffiliation(firstAddA, out _), Is.True);

        PersonStore secondPersons = new PersonStore();
        PersonRuntime secondPerson = RegisterPerson(secondPersons, "person.member");
        SimulationRuntime secondWorld = CreateWorld(secondPersons);
        FactionId secondB = RegisterFaction(secondWorld, "faction.b");
        FactionId secondA = RegisterFaction(secondWorld, "faction.a");
        Assert.That(secondWorld.TryProposeFactionAffiliation(secondA, secondPerson.PersonId, out FactionAffiliationAddTransition secondAddA, out _), Is.True);
        Assert.That(secondWorld.TryApplyFactionAffiliation(secondAddA, out _), Is.True);
        Assert.That(secondWorld.TryProposeFactionAffiliation(secondB, secondPerson.PersonId, out FactionAffiliationAddTransition secondAddB, out _), Is.True);
        Assert.That(secondWorld.TryApplyFactionAffiliation(secondAddB, out _), Is.True);

        Assert.That(WorldStateDiagnostics.Export(Capture(firstWorld)), Is.EqualTo(WorldStateDiagnostics.Export(Capture(secondWorld))));
    }

    private static SimulationRuntime CreateWorld(PersonStore persons, FactionStore factions = null)
    {
        return new SimulationRuntime(
            new SimulationTime(),
            Array.Empty<CityRuntime>(),
            Array.Empty<NpcRuntime>(),
            personStore: persons,
            factionStore: factions);
    }

    private static PersonRuntime RegisterPerson(PersonStore store, string id)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id), 0L);
        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static FactionId RegisterFaction(SimulationRuntime world, string id)
    {
        FactionId factionId = new FactionId(id);
        Assert.That(world.TryRegisterFaction(new FactionRecord(factionId, id, world.CurrentDay), out FactionFoundationFailure failure), Is.True, failure.ToString());
        return factionId;
    }

    private static WorldStateSnapshot Capture(SimulationRuntime world)
    {
        return WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            calendar: world.Calendar,
            personStore: world.PersonStore,
            factions: world.FactionRecords,
            factionAffiliations: world.FactionAffiliationRecords));
    }
}
