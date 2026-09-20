using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;

public sealed class PoliticalSuccessionIntegrationTests
{
    [Test]
    public void RuntimeComposesDecisionHistoryReadOnlyAndAppliesOnlyThroughOfficeSuccession()
    {
        Fixture fixture = CreateFixture();
        Assert.That(fixture.World.PoliticalDecisionRecords, Is.TypeOf<ReadOnlyCollection<PoliticalDecisionRecord>>());
        Assert.That(fixture.World.PoliticalDecisionRecords, Has.Count.EqualTo(1));
        Assert.That(fixture.Decision.EvidenceReferences, Has.Member("support:institutional-majority"));
        Assert.That(fixture.Decision.EvidenceReferences, Has.Member("legitimacy:threshold"));
        Assert.That(fixture.Decision.KnowledgeReferences, Has.Member("knowledge:office-vacancy"));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<PoliticalDecisionRecord>)fixture.World.PoliticalDecisionRecords).Clear());
        Assert.That(fixture.World.NpcRuntimes, Is.Empty);

        Assert.That(fixture.World.IsOfficeVacant(fixture.OfficeId), Is.True);
        Assert.That(fixture.World.TryProposePoliticalOfficeSuccession(
                fixture.Decision.DecisionId,
                fixture.OfficeId,
                fixture.World.CurrentDay,
                out PoliticalOfficeSuccessionTransition transition,
                out PoliticalSuccessionFailure proposalFailure),
            Is.True,
            proposalFailure.ToString());
        Assert.That(fixture.World.IsOfficeVacant(fixture.OfficeId), Is.True);

        Assert.That(fixture.World.TryApplyPoliticalOfficeSuccession(
                transition,
                out PoliticalSuccessionFailure applyFailure),
            Is.True,
            applyFailure.ToString());
        Assert.That(fixture.World.TryGetCurrentOfficeIncumbent(
            fixture.OfficeId,
            out PersonId incumbent), Is.True);
        Assert.That(incumbent, Is.EqualTo(fixture.SelectedCandidateId));
        Assert.That(fixture.World.PoliticalDecisionRecords, Has.Count.EqualTo(1));
        Assert.That(fixture.World.OfficeTenureHistory, Has.Count.EqualTo(2));
    }

    [Test]
    public void DecisionRegistrationRequiresCurrentKnowledgeHolderAndRejectsFutureDay()
    {
        Fixture fixture = CreateFixture(includeDecision: false);
        PoliticalDecisionRecord unregisteredDecider = CreateDecision(
            "decision.unregistered",
            fixture.World.CurrentDay,
            fixture.World.CurrentDay,
            fixture.CandidateIds,
            fixture.SelectedCandidateId,
            PoliticalKnowledgeHolder.ForPerson(fixture.SelectedCandidateId));
        Assert.That(fixture.World.TryRegisterPoliticalDecision(
                unregisteredDecider,
                out PoliticalDecisionFailure unregisteredFailure), Is.False);
        Assert.That(unregisteredFailure.Code, Is.EqualTo(PoliticalDecisionFailureCode.InvalidDecision));

        PoliticalDecisionRecord future = CreateDecision(
            "decision.future",
            fixture.World.CurrentDay,
            fixture.World.CurrentDay + 1L,
            fixture.CandidateIds,
            fixture.SelectedCandidateId,
            fixture.Decider);
        Assert.That(fixture.World.TryRegisterPoliticalDecision(
                future,
                out PoliticalDecisionFailure futureFailure), Is.False);
        Assert.That(futureFailure.Code, Is.EqualTo(PoliticalDecisionFailureCode.InvalidDecision));
    }

    [Test]
    public void PoliticalDecisionCloneDoesNotShareMutableHistoryAndAdvanceDayDoesNotExecutePolitics()
    {
        Fixture fixture = CreateFixture();
        int initialCount = fixture.World.PoliticalDecisionRecords.Count;
        Assert.That(fixture.Decisions.TryRegister(
            CreateDecision(
                "decision.after-clone",
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                fixture.CandidateIds,
                fixture.SelectedCandidateId,
                fixture.Decider),
            out _), Is.True);
        Assert.That(fixture.Decisions.Count, Is.EqualTo(initialCount + 1));
        Assert.That(fixture.World.PoliticalDecisionRecords, Has.Count.EqualTo(initialCount));

        fixture.World.AdvanceDay();
        Assert.That(fixture.World.PoliticalDecisionRecords, Has.Count.EqualTo(initialCount));
        Assert.That(fixture.World.IsOfficeVacant(fixture.OfficeId), Is.True);
    }

    [Test]
    public void PoliticalSelectionUsesCandidateFingerprintAndRevalidatesDeadCandidate()
    {
        Fixture fixture = CreateFixture();
        PoliticalDecisionRecord incompleteSet = CreateDecision(
            "decision.incomplete-set",
            fixture.World.CurrentDay,
            fixture.World.CurrentDay,
            new[] { fixture.SelectedCandidateId },
            fixture.SelectedCandidateId,
            fixture.Decider,
            fixture.World.PoliticalWorldRevision,
            fixture.World.PoliticalKnowledgeRevision);
        Assert.That(fixture.World.TryRegisterPoliticalDecision(
            incompleteSet,
            out PoliticalDecisionFailure registrationFailure), Is.True, registrationFailure.ToString());
        Assert.That(fixture.World.TryProposePoliticalOfficeSuccession(
                incompleteSet.DecisionId,
                fixture.OfficeId,
                fixture.World.CurrentDay,
                out _,
                out PoliticalSuccessionFailure fingerprintFailure), Is.False);
        Assert.That(fingerprintFailure.Code, Is.EqualTo(PoliticalSuccessionFailureCode.CandidateFingerprintMismatch));

        Assert.That(fixture.World.TryProposePoliticalOfficeSuccession(
                fixture.Decision.DecisionId,
                fixture.OfficeId,
                fixture.World.CurrentDay,
                out PoliticalOfficeSuccessionTransition transition,
                out PoliticalSuccessionFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(fixture.World.TryApplyPersonDeath(
            fixture.SelectedCandidateId,
            out _,
            out PersonDeathLifecycleFailure deathFailure), Is.True, deathFailure.ToString());
        Assert.That(fixture.World.TryApplyPoliticalOfficeSuccession(
                transition,
                out PoliticalSuccessionFailure applyFailure), Is.False);
        Assert.That(applyFailure.Code, Is.EqualTo(PoliticalSuccessionFailureCode.StaleDecision));
        Assert.That(fixture.World.IsOfficeVacant(fixture.OfficeId), Is.True);
    }

    [Test]
    public void PoliticalDecisionHistoryParticipatesInCanonicalDiagnosticsAndDiffs()
    {
        Fixture fixture = CreateFixture();
        WorldStateSnapshot before = Capture(fixture.World);
        Assert.That(before.PoliticalDecisionCount, Is.EqualTo(1));
        Assert.That(WorldStateCanonicalWriter.Write(before), Does.Contain("POLITICAL_DECISION"));
        Assert.That(WorldStateInvariantValidator.Validate(before).IsValid, Is.True);

        WorldStateSnapshot after = new WorldStateSnapshot(
            fixture.World.CurrentDay,
            politicalDecisions: new WorldStatePoliticalDecisionSnapshot[0]);
        Assert.That(WorldStateDiagnostics.Compare(before, after).IsEmpty, Is.False);
    }

    [Test]
    public void StaleDecisionAndWrongDecisionInputsCannotExecuteOfficeSuccession()
    {
        Fixture fixture = CreateFixture();
        Assert.That(fixture.World.TryProposePoliticalOfficeSuccession(
                fixture.Decision.DecisionId,
                fixture.OfficeId,
                fixture.World.CurrentDay,
                out PoliticalOfficeSuccessionTransition transition,
                out PoliticalSuccessionFailure proposalFailure), Is.True, proposalFailure.ToString());
        fixture.World.AdvanceDay();
        Assert.That(fixture.World.TryApplyPoliticalOfficeSuccession(
                transition,
                out PoliticalSuccessionFailure staleFailure), Is.False);
        Assert.That(staleFailure.Code, Is.EqualTo(PoliticalSuccessionFailureCode.StaleWorldDay));
        Assert.That(fixture.World.IsOfficeVacant(fixture.OfficeId), Is.True);

        Assert.That(fixture.World.TryProposePoliticalOfficeSuccession(
                new PoliticalDecisionId("decision.missing"),
                fixture.OfficeId,
                fixture.World.CurrentDay,
                out _,
                out PoliticalSuccessionFailure missingFailure), Is.False);
        Assert.That(missingFailure.Code, Is.EqualTo(PoliticalSuccessionFailureCode.DecisionNotFound));
    }

    [Test]
    public void AuthoritativeKnowledgeRevisionAndOfficeBindingRejectStaleOrWrongTargets()
    {
        Fixture fixture = CreateFixture();
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            fixture.Decider,
            new FactionKnowledgeObservation(
                new FactionId("knowledge.faction"),
                true,
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                new PoliticalKnowledgeProvenance(
                    PoliticalKnowledgeSource.DirectObservation,
                    "same-day-change")),
            out PoliticalKnowledgeFailure knowledgeFailure), Is.True, knowledgeFailure.ToString());
        Assert.That(fixture.World.TryProposePoliticalOfficeSuccession(
                fixture.Decision.DecisionId,
                fixture.OfficeId,
                fixture.World.CurrentDay,
                out _,
                out PoliticalSuccessionFailure staleFailure), Is.False);
        Assert.That(staleFailure.Code, Is.EqualTo(PoliticalSuccessionFailureCode.StaleDecision));

        Fixture wrongOfficeFixture = CreateFixture(includeDecision: false);
        OfficeId otherOfficeId = new OfficeId("political-office-other");
        Assert.That(wrongOfficeFixture.World.TryRegisterOffice(
            new OfficeRecord(otherOfficeId, new InstitutionId("political-institution")),
            out InstitutionFoundationFailure officeFailure), Is.True, officeFailure.ToString());
        PoliticalDecisionRecord wrongOfficeDecision = CreateDecision(
            "decision.other-office",
            wrongOfficeFixture.World.CurrentDay,
            wrongOfficeFixture.World.CurrentDay,
            wrongOfficeFixture.CandidateIds,
            wrongOfficeFixture.SelectedCandidateId,
            wrongOfficeFixture.Decider,
            wrongOfficeFixture.World.PoliticalWorldRevision,
            wrongOfficeFixture.World.PoliticalKnowledgeRevision,
            otherOfficeId);
        Assert.That(wrongOfficeFixture.World.TryRegisterPoliticalDecision(
            wrongOfficeDecision,
            out PoliticalDecisionFailure decisionFailure), Is.True, decisionFailure.ToString());
        Assert.That(wrongOfficeFixture.World.TryProposePoliticalOfficeSuccession(
                wrongOfficeDecision.DecisionId,
                wrongOfficeFixture.OfficeId,
                wrongOfficeFixture.World.CurrentDay,
                out _,
                out PoliticalSuccessionFailure officeMismatchFailure), Is.False);
        Assert.That(officeMismatchFailure.Code, Is.EqualTo(PoliticalSuccessionFailureCode.DecisionOfficeMismatch));
    }

    private static Fixture CreateFixture(bool includeDecision = true)
    {
        const long currentDay = 10_000L;
        PersonStore people = new PersonStore();
        PersonRuntime formerIncumbent = Register(people, "political-former", 0L, 1L);
        PersonRuntime firstCandidate = Register(people, "political-candidate-a", 0L);
        PersonRuntime selectedCandidate = Register(people, "political-candidate-b", 0L);
        PersonRuntime thirdCandidate = Register(people, "political-candidate-c", 0L);

        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(formerIncumbent.PersonId, firstCandidate.PersonId, out _), Is.True);
        Assert.That(genealogy.TryAddParentage(formerIncumbent.PersonId, selectedCandidate.PersonId, out _), Is.True);
        Assert.That(genealogy.TryAddParentage(formerIncumbent.PersonId, thirdCandidate.PersonId, out _), Is.True);

        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institutionId = new InstitutionId("political-institution");
        Assert.That(institutions.TryRegister(new InstitutionRecord(institutionId), out _), Is.True);
        OfficeStore offices = new OfficeStore(institutions);
        OfficeId officeId = new OfficeId("political-office");
        Assert.That(offices.TryRegister(new OfficeRecord(officeId, institutionId), out _), Is.True);
        Assert.That(offices.TryAssignIncumbent(
            officeId,
            formerIncumbent.PersonId,
            0L,
            out _), Is.True);

        PoliticalKnowledgeHolder decider = PoliticalKnowledgeHolder.ForInstitution(institutionId);
        PoliticalKnowledgeStore knowledge = new PoliticalKnowledgeStore(people, institutions);
        Assert.That(knowledge.TryRegisterHolder(decider, currentDay, out _), Is.True);

        PersonId[] candidates = {
            firstCandidate.PersonId,
            selectedCandidate.PersonId,
            thirdCandidate.PersonId
        };
        PoliticalDecisionRecord decision = CreateDecision(
            "decision.succession",
            currentDay,
            currentDay,
            candidates,
            selectedCandidate.PersonId,
            decider,
            0L,
            1L);
        PoliticalDecisionStore decisions = new PoliticalDecisionStore();

        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(currentDay),
            Array.Empty<CityRuntime>(),
            null,
            personStore: people,
            genealogyStore: genealogy,
            institutionStore: institutions,
            officeStore: offices,
            politicalKnowledgeStore: knowledge,
            politicalDecisionStore: decisions);

        Assert.That(world.TryProposeInstitutionalVacancyRecognition(
            officeId,
            InstitutionalVacancyRecognitionReason.FactualDeath,
            out InstitutionalVacancyRecognitionTransition vacancyTransition,
            out InstitutionalVacancyRecognitionFailure vacancyFailure), Is.True, vacancyFailure.ToString());
        Assert.That(world.TryApplyInstitutionalVacancyRecognition(
            vacancyTransition,
            out InstitutionalVacancyRecognitionFailure vacancyApplyFailure), Is.True, vacancyApplyFailure.ToString());

        decision = CreateDecision(
            "decision.succession",
            currentDay,
            currentDay,
            candidates,
            selectedCandidate.PersonId,
            decider,
            world.PoliticalWorldRevision,
            world.PoliticalKnowledgeRevision);
        if (includeDecision)
        {
            Assert.That(world.TryRegisterPoliticalDecision(
                decision,
                out PoliticalDecisionFailure worldDecisionFailure), Is.True, worldDecisionFailure.ToString());
            Assert.That(decisions.TryRegister(decision, out _), Is.True);
        }

        return new Fixture(
            world,
            decisions,
            decision,
            decider,
            officeId,
            new[] { firstCandidate.PersonId, selectedCandidate.PersonId, thirdCandidate.PersonId },
            selectedCandidate.PersonId);
    }

    private static PoliticalDecisionRecord CreateDecision(
        string id,
        long observedDay,
        long decisionDay,
        IEnumerable<PersonId> candidates,
        PersonId selectedCandidate,
        PoliticalKnowledgeHolder decider,
        long expectedWorldRevision = 0L,
        long expectedKnowledgeRevision = 1L,
        OfficeId officeId = null)
    {
        return new PoliticalDecisionRecord(
            new PoliticalDecisionId(id),
            decider,
            PoliticalDecisionKind.SuccessionSelection,
            candidates,
            PoliticalDecisionOutcome.Candidate(selectedCandidate),
            observedDay,
            decisionDay,
            new[] { "support:institutional-majority", "legitimacy:threshold" },
            new[] { "knowledge:office-vacancy", "knowledge:candidate-set" },
            expectedWorldRevision,
            expectedKnowledgeRevision,
            officeId ?? new OfficeId("political-office"));
    }

    private static WorldStateSnapshot Capture(SimulationRuntime world)
    {
        return WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            calendar: world.Calendar,
            personStore: world.PersonStore,
            institutionIds: new[] { "political-institution" },
            officeIds: new[] { "political-office" },
            politicalDecisions: world.PoliticalDecisionRecords));
    }

    private static PersonRuntime Register(
        PersonStore people,
        string id,
        long birthDay,
        long? deathDay = null)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id), birthDay, deathDay);
        Assert.That(people.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private sealed class Fixture
    {
        public Fixture(
            SimulationRuntime world,
            PoliticalDecisionStore decisions,
            PoliticalDecisionRecord decision,
            PoliticalKnowledgeHolder decider,
            OfficeId officeId,
            PersonId[] candidateIds,
            PersonId selectedCandidateId)
        {
            World = world;
            Decisions = decisions;
            Decision = decision;
            Decider = decider;
            OfficeId = officeId;
            CandidateIds = candidateIds;
            SelectedCandidateId = selectedCandidateId;
        }

        public SimulationRuntime World { get; }
        public PoliticalDecisionStore Decisions { get; }
        public PoliticalDecisionRecord Decision { get; }
        public PoliticalKnowledgeHolder Decider { get; }
        public OfficeId OfficeId { get; }
        public PersonId[] CandidateIds { get; }
        public PersonId SelectedCandidateId { get; }
    }
}
