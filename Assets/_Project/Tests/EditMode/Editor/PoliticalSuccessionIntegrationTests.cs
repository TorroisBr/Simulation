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

        PoliticalDecisionRecord unknownCandidate = CreateDecision(
            "decision.unknown-candidate",
            fixture.World.CurrentDay,
            fixture.World.CurrentDay,
            new[] { new PersonId("person.unknown") },
            new PersonId("person.unknown"),
            fixture.Decider,
            fixture.World.PoliticalWorldRevision,
            fixture.World.PoliticalKnowledgeRevision);
        Assert.That(fixture.World.TryRegisterPoliticalDecision(
                unknownCandidate,
                out PoliticalDecisionFailure unknownCandidateFailure), Is.False);
        Assert.That(unknownCandidateFailure.Code, Is.EqualTo(PoliticalDecisionFailureCode.InvalidDecision));
    }

    [Test]
    public void DecisionRegistrationRejectsARecordBoundToAnotherWorld()
    {
        Fixture source = CreateFixture(includeDecision: false);
        Assert.That(source.World.TryRegisterPoliticalDecision(
            source.Decision,
            out PoliticalDecisionFailure sourceFailure), Is.True, sourceFailure.ToString());

        Fixture other = CreateFixture(includeDecision: false);
        Assert.That(other.World.TryRegisterPoliticalDecision(
            source.Decision,
            out PoliticalDecisionFailure otherFailure), Is.False);
        Assert.That(otherFailure.Code, Is.EqualTo(PoliticalDecisionFailureCode.WorldMismatch));
    }

    [Test]
    public void FailedDuplicateRegistrationDoesNotBindTheCallerDecisionRecord()
    {
        Fixture first = CreateFixture();
        PoliticalDecisionRecord duplicate = CreateDecision(
            "decision.succession",
            first.World.CurrentDay,
            first.World.CurrentDay,
            first.CandidateIds,
            first.SelectedCandidateId,
            first.Decider,
            first.World.PoliticalWorldRevision,
            first.World.PoliticalKnowledgeRevision);

        Assert.That(first.World.TryRegisterPoliticalDecision(
            duplicate,
            out PoliticalDecisionFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(PoliticalDecisionFailureCode.DuplicateDecisionId));

        Fixture second = CreateFixture(includeDecision: false);
        Assert.That(second.World.TryRegisterPoliticalDecision(
            duplicate,
            out PoliticalDecisionFailure secondFailure), Is.True, secondFailure.ToString());
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
    public void ComposingPoliticalWorldPreservesCapturedRevisionAndAcceptsFreshDecision()
    {
        Fixture fixture = CreateFixture();
        long capturedRevision = fixture.World.PoliticalWorldRevision;

        SimulationRuntime composed = new SimulationRuntime(
            new SimulationTime(fixture.World.CurrentDay),
            Array.Empty<CityRuntime>(),
            null,
            personStore: fixture.People,
            genealogyStore: fixture.Genealogy,
            institutionStore: fixture.Institutions,
            officeStore: fixture.Offices,
            politicalKnowledgeStore: fixture.Knowledge,
            politicalDecisionStore: fixture.Decisions,
            politicalWorldRevision: capturedRevision);

        Assert.That(composed.PoliticalWorldRevision, Is.EqualTo(capturedRevision));

        PoliticalDecisionRecord freshDecision = CreateDecision(
            "decision.after-composition",
            composed.CurrentDay,
            composed.CurrentDay,
            fixture.CandidateIds,
            fixture.SelectedCandidateId,
            fixture.Decider,
            capturedRevision,
            composed.PoliticalKnowledgeRevision);
        Assert.That(composed.TryRegisterPoliticalDecision(
                freshDecision,
                out PoliticalDecisionFailure failure), Is.True, failure.ToString());
    }

    [Test]
    public void ImportedPoliticalDecisionHistoryRequiresExplicitRevisionAndWorldPersonStore()
    {
        Fixture fixture = CreateFixture();

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(fixture.World.CurrentDay),
            Array.Empty<CityRuntime>(),
            null,
            personStore: fixture.People,
            genealogyStore: fixture.Genealogy,
            institutionStore: fixture.Institutions,
            officeStore: fixture.Offices,
            politicalKnowledgeStore: fixture.Knowledge,
            politicalDecisionStore: fixture.Decisions));

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(fixture.World.CurrentDay),
            Array.Empty<CityRuntime>(),
            null,
            personStore: new PersonStore(),
            politicalWorldRevision: fixture.World.PoliticalWorldRevision,
            politicalDecisionStore: fixture.Decisions));

        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationRuntime(
            new SimulationTime(fixture.World.CurrentDay),
            Array.Empty<CityRuntime>(),
            null,
            politicalWorldRevision: -1L));

        PoliticalDecisionStore futureHistory = new PoliticalDecisionStore();
        PoliticalDecisionRecord futureRevision = CreateDecision(
            "decision.future-revision-history",
            fixture.World.CurrentDay,
            fixture.World.CurrentDay,
            fixture.CandidateIds,
            fixture.SelectedCandidateId,
            fixture.Decider,
            fixture.World.PoliticalWorldRevision + 1L,
            fixture.World.PoliticalKnowledgeRevision);
        Assert.That(futureHistory.TryRegister(futureRevision, out _), Is.True);
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(fixture.World.CurrentDay),
            Array.Empty<CityRuntime>(),
            null,
            personStore: fixture.People,
            genealogyStore: fixture.Genealogy,
            institutionStore: fixture.Institutions,
            officeStore: fixture.Offices,
            politicalKnowledgeStore: fixture.Knowledge,
            politicalDecisionStore: futureHistory,
            politicalWorldRevision: fixture.World.PoliticalWorldRevision));
    }

    [Test]
    public void FailedWorldCompositionDoesNotBindCallerDecisionStore()
    {
        PoliticalDecisionStore source = new PoliticalDecisionStore();

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(0L),
            Array.Empty<CityRuntime>(),
            new NpcRuntime[] { null },
            politicalDecisionStore: source));

        Assert.DoesNotThrow(() => new SimulationRuntime(
            new SimulationTime(0L),
            Array.Empty<CityRuntime>(),
            null,
            personStore: new PersonStore(),
            politicalDecisionStore: source));
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
        Assert.That(WorldStateCanonicalWriter.Write(before), Does.Contain("support:institutional-majority"));
        Assert.That(WorldStateInvariantValidator.Validate(before).IsValid, Is.True);

        WorldStatePoliticalDecisionSnapshot original = before.PoliticalDecisions[0];
        WorldStateSnapshot evidenceChanged = new WorldStateSnapshot(
            fixture.World.CurrentDay,
            politicalDecisions: new[] {
                new WorldStatePoliticalDecisionSnapshot(
                    original.DecisionId,
                    original.DeciderStableId,
                    original.DecisionKind,
                    original.OfficeId,
                    original.RecognizingInstitutionId,
                    original.CandidatePersonIds,
                    original.CandidateFingerprint,
                    original.OutcomeKind,
                    original.SelectedCandidatePersonId,
                    original.ReferencedClaimId,
                    new[] { "evidence.changed" },
                    original.KnowledgeReferences,
                    original.ObservedAbsoluteDay,
                    original.DecisionAbsoluteDay,
                    original.ExpectedWorldRevision,
                    original.ExpectedKnowledgeRevision)
            });
        Assert.That(WorldStateDiagnostics.Compare(before, evidenceChanged).IsEmpty, Is.False);

        WorldStateSnapshot after = new WorldStateSnapshot(
            fixture.World.CurrentDay,
            politicalDecisions: new WorldStatePoliticalDecisionSnapshot[0]);
        Assert.That(WorldStateDiagnostics.Compare(before, after).IsEmpty, Is.False);
    }

    [Test]
    public void PoliticalKnowledgeDiagnosticsRejectOfficeInstitutionMismatch()
    {
        Fixture fixture = CreateFixture();
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            fixture.Decider,
            new OfficeVacancyKnowledgeObservation(
                fixture.OfficeId,
                new InstitutionId("political-institution"),
                true,
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                new PoliticalKnowledgeProvenance(
                    PoliticalKnowledgeSource.DirectObservation,
                    "office")),
            out PoliticalKnowledgeFailure recordFailure), Is.True, recordFailure.ToString());

        WorldStateSnapshot valid = Capture(fixture.World);
        WorldStatePoliticalKnowledgeSnapshot holder = valid.PoliticalKnowledge[0];
        WorldStatePoliticalKnowledgeObservationSnapshot observation = holder.Observations[0];
        WorldStatePoliticalKnowledgeObservationSnapshot forged = new WorldStatePoliticalKnowledgeObservationSnapshot(
            observation.IdentityKey,
            observation.FactKind,
            observation.ObservedAbsoluteDay,
            observation.ReceivedAbsoluteDay,
            observation.Source,
            observation.SourceReference,
            observation.SourcePersonId,
            observation.SourceInstitutionId,
            "5:other\u001F1\u001F1");
        WorldStateSnapshot malformed = new WorldStateSnapshot(
            valid.AbsoluteDay,
            persons: valid.Persons,
            institutionIds: valid.InstitutionIds,
            officeIds: valid.OfficeIds,
            officeInstitutionIds: valid.OfficeInstitutionIds,
            politicalKnowledge: new[] {
                new WorldStatePoliticalKnowledgeSnapshot(
                    holder.HolderStableId,
                    holder.HolderKind,
                    holder.HolderPersonId,
                    holder.HolderInstitutionId,
                    new[] { forged })
            },
            politicalKnowledgeRevision: valid.PoliticalKnowledgeRevision,
            hasPoliticalKnowledgeState: true);
        WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(malformed);
        Assert.That(HasIssueCode(report, "PoliticalKnowledgeOfficeInstitutionMismatch"), Is.True, report.ToString());
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

        Fixture otherWorld = CreateFixture();
        Assert.That(otherWorld.World.TryApplyPoliticalOfficeSuccession(
                transition,
                out PoliticalSuccessionFailure crossWorldFailure), Is.False);
        Assert.That(crossWorldFailure.Code, Is.EqualTo(PoliticalSuccessionFailureCode.InvalidTransition));

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
            new PersonDeathKnowledgeObservation(
                fixture.SelectedCandidateId,
                false,
                null,
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
        PoliticalKnowledgeStore knowledge = new PoliticalKnowledgeStore(
            people,
            institutions,
            new PoliticalClaimStore(),
            new FactionStore(people),
            new OfficeStore(institutions),
            new PropertyOwnershipStore(people));
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
            selectedCandidate.PersonId,
            people,
            genealogy,
            institutions,
            offices,
            knowledge);
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
            officeInstitutionIds: new Dictionary<string, string> {
                ["political-office"] = "political-institution"
            },
            politicalDecisions: world.PoliticalDecisionRecords,
            politicalKnowledgeRuntimes: world.PoliticalKnowledgeRuntimes,
            politicalKnowledgeRevision: world.PoliticalKnowledgeRevision));
    }

    private static bool HasIssueCode(WorldStateInvariantReport report, string code)
    {
        foreach (WorldStateInvariantIssue issue in report.Issues)
        {
            if (issue != null && issue.Code == code)
            {
                return true;
            }
        }

        return false;
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
            PersonId selectedCandidateId,
            PersonStore people,
            GenealogyStore genealogy,
            InstitutionStore institutions,
            OfficeStore offices,
            PoliticalKnowledgeStore knowledge)
        {
            World = world;
            Decisions = decisions;
            Decision = decision;
            Decider = decider;
            OfficeId = officeId;
            CandidateIds = candidateIds;
            SelectedCandidateId = selectedCandidateId;
            People = people;
            Genealogy = genealogy;
            Institutions = institutions;
            Offices = offices;
            Knowledge = knowledge;
        }

        public SimulationRuntime World { get; }
        public PoliticalDecisionStore Decisions { get; }
        public PoliticalDecisionRecord Decision { get; }
        public PoliticalKnowledgeHolder Decider { get; }
        public OfficeId OfficeId { get; }
        public PersonId[] CandidateIds { get; }
        public PersonId SelectedCandidateId { get; }
        public PersonStore People { get; }
        public GenealogyStore Genealogy { get; }
        public InstitutionStore Institutions { get; }
        public OfficeStore Offices { get; }
        public PoliticalKnowledgeStore Knowledge { get; }
    }
}
