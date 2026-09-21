using System;
using NUnit.Framework;

public sealed class PoliticalKnowledgeSupportWorldIntegrationTests
{
    [Test]
    public void SupportIsWorldOwnedAndUsesAuthoritativeCurrentDayTransitions()
    {
        Fixture fixture = CreateFixture();
        PoliticalClaimId claimId = RegisterClaim(fixture.World, fixture.Supporter, fixture.Candidate);
        PoliticalSupportRelationRecord relation = new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.claim"),
            PoliticalSupportSource.ForFaction(fixture.Faction),
            PoliticalSupportTarget.ForPoliticalClaim(claimId),
            PoliticalSupportDisposition.Support,
            fixture.World.CurrentDay);

        Assert.That(fixture.World.TryProposePoliticalSupportAdd(
            relation,
            out PoliticalSupportAddTransition add,
            out PoliticalSupportFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(fixture.World.TryApplyPoliticalSupportAdd(add, out PoliticalSupportFailure addFailure), Is.True, addFailure.ToString());
        Assert.That(fixture.World.PoliticalSupportRecords, Has.Count.EqualTo(1));

        fixture.World.AdvanceDay();
        Assert.That(fixture.World.TryProposePoliticalSupportEnd(
            relation.RelationId,
            out PoliticalSupportEndTransition end,
            out PoliticalSupportFailure endProposalFailure), Is.True, endProposalFailure.ToString());
        Assert.That(fixture.World.TryApplyPoliticalSupportEnd(end, out PoliticalSupportFailure endFailure), Is.True, endFailure.ToString());
        Assert.That(fixture.World.PoliticalSupportRecords[0].EndedAbsoluteDay, Is.EqualTo(fixture.World.CurrentDay));

        PoliticalSupportRelationRecord future = new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.future"),
            PoliticalSupportSource.ForPerson(fixture.Supporter),
            PoliticalSupportTarget.ForSuccessionCandidate(fixture.Candidate),
            PoliticalSupportDisposition.Oppose,
            fixture.World.CurrentDay + 1L);
        Assert.That(fixture.World.TryRegisterPoliticalSupport(future, out PoliticalSupportFailure futureFailure), Is.False);
        Assert.That(futureFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.StaleWorldDay));
    }

    [Test]
    public void PoliticalKnowledgeIsHolderScopedAndCannotReceiveFutureObservations()
    {
        Fixture fixture = CreateFixture();
        Assert.That(fixture.World.TryRegisterPoliticalClaim(
            new PoliticalClaimRecord(
                new PoliticalClaimId("claim.known"),
                fixture.Supporter,
                PoliticalClaimType.StatusRecognition,
                PoliticalClaimTarget.ForPerson(fixture.Candidate),
                PoliticalClaimBasis.ExplicitDecision,
                "known",
                fixture.World.CurrentDay,
                null),
            out PoliticalClaimFailure knownClaimFailure), Is.True, knownClaimFailure.ToString());
        Assert.That(fixture.World.TryRegisterPoliticalKnowledgeHolder(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out PoliticalKnowledgeFailure personHolderFailure), Is.True, personHolderFailure.ToString());
        Assert.That(fixture.World.TryRegisterPoliticalKnowledgeHolder(
            PoliticalKnowledgeHolder.ForInstitution(fixture.Institution),
            out PoliticalKnowledgeFailure institutionHolderFailure), Is.True, institutionHolderFailure.ToString());

        PoliticalClaimKnowledgeObservation current = new PoliticalClaimKnowledgeObservation(
            new PoliticalClaimId("claim.known"),
            true,
            fixture.Supporter,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(fixture.Candidate),
            PoliticalClaimBasis.ExplicitDecision,
            0L,
            PoliticalClaimStatus.Active,
            null,
            PoliticalClaimRecognitionState.Unrecognized,
            null,
            null,
            fixture.World.CurrentDay,
            fixture.World.CurrentDay,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "field"));
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            current,
            out PoliticalKnowledgeFailure recordFailure), Is.True, recordFailure.ToString());
        Assert.That(fixture.World.TryGetPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out PoliticalKnowledgeRuntime personKnowledge), Is.True);
        Assert.That(personKnowledge.ClaimObservations, Has.Count.EqualTo(1));
        Assert.That(personKnowledge.ClaimObservations[0].Target.TargetId, Is.EqualTo(fixture.Candidate.Value));
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            new PersonDeathKnowledgeObservation(
                fixture.Candidate,
                false,
                null,
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                new PoliticalKnowledgeProvenance(
                    PoliticalKnowledgeSource.SharedByPerson,
                    "orphan-source",
                    new PersonId("person.unknown"))),
            out PoliticalKnowledgeFailure orphanProvenanceFailure), Is.False);
        Assert.That(orphanProvenanceFailure.Code, Is.EqualTo(PoliticalKnowledgeFailureCode.InvalidObservation));
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            new FactionAffiliationKnowledgeObservation(
                fixture.Faction,
                fixture.Supporter,
                true,
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "affiliation")),
            out PoliticalKnowledgeFailure affiliationFailure), Is.True, affiliationFailure.ToString());

        WorldStateSnapshot knowledgeSnapshot = Capture(fixture.World);
        Assert.That(knowledgeSnapshot.PoliticalKnowledgeHolderCount, Is.EqualTo(2));
        Assert.That(knowledgeSnapshot.PoliticalKnowledgeRevision, Is.EqualTo(fixture.World.PoliticalKnowledgeRevision));
        Assert.That(WorldStateCanonicalWriter.Write(knowledgeSnapshot), Does.Contain("POLITICAL_KNOWLEDGE"));
        Assert.That(WorldStateCanonicalWriter.Write(knowledgeSnapshot), Does.Contain("POLITICAL_KNOWLEDGE_OBSERVATION"));
        Assert.That(WorldStateInvariantValidator.Validate(knowledgeSnapshot).IsValid, Is.True, WorldStateInvariantValidator.Validate(knowledgeSnapshot).ToString());

        PoliticalClaimKnowledgeObservation future = new PoliticalClaimKnowledgeObservation(
            new PoliticalClaimId("claim.future"),
            true,
            fixture.Supporter,
            PoliticalClaimType.StatusRecognition,
            PoliticalClaimTarget.ForPerson(fixture.Candidate),
            PoliticalClaimBasis.ExplicitDecision,
            0L,
            PoliticalClaimStatus.Active,
            null,
            PoliticalClaimRecognitionState.Unrecognized,
            null,
            null,
            fixture.World.CurrentDay + 1L,
            fixture.World.CurrentDay + 1L,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "future"));
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            future,
            out PoliticalKnowledgeFailure futureFailure), Is.False);
        Assert.That(futureFailure.Code, Is.EqualTo(PoliticalKnowledgeFailureCode.FutureObservation));
        Assert.That(fixture.World.PoliticalKnowledgeHolderCount, Is.EqualTo(2));
        Assert.That(typeof(PoliticalKnowledgeRuntime).GetProperty("NpcRuntimeId"), Is.Null);
    }

    [Test]
    public void FactionPoliticalKnowledgeHolderIsIndependentFromMemberKnowledge()
    {
        Fixture fixture = CreateFixture();
        PoliticalKnowledgeHolder factionHolder = PoliticalKnowledgeHolder.ForFaction(fixture.Faction);
        Assert.That(fixture.World.TryRegisterPoliticalKnowledgeHolder(
            factionHolder,
            out PoliticalKnowledgeFailure holderFailure), Is.True, holderFailure.ToString());

        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            factionHolder,
            new FactionKnowledgeObservation(
                fixture.Faction,
                true,
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "faction-record")),
            out PoliticalKnowledgeFailure observationFailure), Is.True, observationFailure.ToString());

        Assert.That(fixture.World.TryRegisterPoliticalKnowledgeHolder(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out _), Is.True);
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            new FactionAffiliationKnowledgeObservation(
                fixture.Faction,
                fixture.Supporter,
                true,
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "member-record")),
            out _), Is.True);

        Assert.That(fixture.World.TryGetPoliticalKnowledge(factionHolder, out PoliticalKnowledgeRuntime factionKnowledge), Is.True);
        Assert.That(factionKnowledge.FactionObservations, Has.Count.EqualTo(1));
        Assert.That(factionKnowledge.FactionAffiliationObservations, Is.Empty);
        Assert.That(fixture.World.TryGetPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out PoliticalKnowledgeRuntime personKnowledge), Is.True);
        Assert.That(personKnowledge.FactionObservations, Is.Empty);
        Assert.That(personKnowledge.FactionAffiliationObservations, Has.Count.EqualTo(1));
    }

    [Test]
    public void ConsumerKnowledgeSnapshotsCannotMutateWorldKnowledge()
    {
        Fixture fixture = CreateFixture();
        Assert.That(fixture.World.TryRegisterPoliticalKnowledgeHolder(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out _), Is.True);
        Assert.That(fixture.World.TryGetPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out PoliticalKnowledgeRuntime snapshot), Is.True);
        Assert.That(snapshot.RecordFactionObservation(new FactionKnowledgeObservation(
            fixture.Faction,
            true,
            fixture.World.CurrentDay + 1L,
            fixture.World.CurrentDay + 1L,
            new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "consumer"))), Is.True);
        Assert.That(fixture.World.TryGetPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out PoliticalKnowledgeRuntime unchanged), Is.True);
        Assert.That(unchanged.FactionObservations, Is.Empty);
    }

    [Test]
    public void PoliticalSupportAppearsInCanonicalDiagnosticsAndDiffs()
    {
        Fixture fixture = CreateFixture();
        PoliticalClaimId claimId = RegisterClaim(fixture.World, fixture.Supporter, fixture.Candidate);
        PoliticalSupportRelationRecord relation = new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("support.diagnostic"),
            PoliticalSupportSource.ForFaction(fixture.Faction),
            PoliticalSupportTarget.ForPoliticalClaim(claimId),
            PoliticalSupportDisposition.Oppose,
            fixture.World.CurrentDay);
        Assert.That(fixture.World.TryRegisterPoliticalSupport(relation, out PoliticalSupportFailure failure), Is.True, failure.ToString());

        WorldStateSnapshot before = Capture(fixture.World);
        Assert.That(before.PoliticalSupportCount, Is.EqualTo(1));
        Assert.That(WorldStateCanonicalWriter.Write(before), Does.Contain("POLITICAL_SUPPORT"));
        Assert.That(WorldStateInvariantValidator.Validate(before).IsValid, Is.True);

        Assert.That(fixture.World.TryProposePoliticalSupportEnd(
            relation.RelationId,
            out PoliticalSupportEndTransition end,
            out _), Is.True);
        Assert.That(fixture.World.TryApplyPoliticalSupportEnd(end, out _), Is.True);
        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, Capture(fixture.World));
        Assert.That(diff.IsEmpty, Is.False);
    }

    [Test]
    public void PoliticalSupportDiagnosticsKeepDistinctDelimiterPairsAndRejectMalformedEndpointsSafely()
    {
        Fixture fixture = CreateFixture();
        PersonId sourceWithColon = new PersonId("support:source");
        PersonId sourcePlain = new PersonId("support");
        PersonId candidateWithPrefix = new PersonId("0:candidate");
        Assert.That(fixture.World.TryRegisterPerson(new PersonRuntime(sourceWithColon, 0L), out _), Is.True);
        Assert.That(fixture.World.TryRegisterPerson(new PersonRuntime(sourcePlain, 0L), out _), Is.True);
        Assert.That(fixture.World.TryRegisterPerson(new PersonRuntime(candidateWithPrefix, 0L), out _), Is.True);

        PoliticalClaimId claimId = new PoliticalClaimId("0:candidate");
        Assert.That(fixture.World.TryRegisterPoliticalClaim(
            new PoliticalClaimRecord(
                claimId,
                fixture.Supporter,
                PoliticalClaimType.StatusRecognition,
                PoliticalClaimTarget.ForPerson(fixture.Candidate),
                PoliticalClaimBasis.ExplicitDecision,
                "delimiter",
                fixture.World.CurrentDay,
                null),
            out _), Is.True);

        Assert.That(fixture.World.TryRegisterPoliticalSupport(
            new PoliticalSupportRelationRecord(
                new PoliticalSupportRelationId("support.delimiter.claim"),
                PoliticalSupportSource.ForPerson(sourceWithColon),
                PoliticalSupportTarget.ForPoliticalClaim(claimId),
                PoliticalSupportDisposition.Support,
                fixture.World.CurrentDay),
            out _), Is.True);
        Assert.That(fixture.World.TryRegisterPoliticalSupport(
            new PoliticalSupportRelationRecord(
                new PoliticalSupportRelationId("support.delimiter.candidate"),
                PoliticalSupportSource.ForPerson(sourcePlain),
                PoliticalSupportTarget.ForSuccessionCandidate(candidateWithPrefix),
                PoliticalSupportDisposition.Support,
                fixture.World.CurrentDay),
            out _), Is.True);

        WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(Capture(fixture.World));
        Assert.That(report.IsValid, Is.True, report.ToString());

        WorldStateSnapshot malformed = new WorldStateSnapshot(
            fixture.World.CurrentDay,
            politicalSupports: new[] {
                new WorldStatePoliticalSupportSnapshot(
                    "support.malformed",
                    PoliticalSupportSourceKind.Person,
                    null,
                    PoliticalSupportTargetKind.SuccessionCandidate,
                    null,
                    PoliticalSupportDisposition.Support,
                    fixture.World.CurrentDay,
                    null)
            });
        Assert.That(() => WorldStateInvariantValidator.Validate(malformed), Throws.Nothing);
        Assert.That(WorldStateInvariantValidator.Validate(malformed).IsValid, Is.False);
    }

    [Test]
    public void PoliticalKnowledgeDiagnosticsRejectForgedClaimStateAndOrphanProvenance()
    {
        Fixture fixture = CreateFixture();
        PoliticalClaimId claimId = RegisterClaim(fixture.World, fixture.Supporter, fixture.Candidate);
        Assert.That(fixture.World.TryRegisterPoliticalKnowledgeHolder(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            out _), Is.True);
        Assert.That(fixture.World.TryRecordPoliticalKnowledge(
            PoliticalKnowledgeHolder.ForPerson(fixture.Supporter),
            new PoliticalClaimKnowledgeObservation(
                claimId,
                true,
                fixture.Supporter,
                PoliticalClaimType.StatusRecognition,
                PoliticalClaimTarget.ForPerson(fixture.Candidate),
                PoliticalClaimBasis.ExplicitDecision,
                fixture.World.CurrentDay,
                PoliticalClaimStatus.Active,
                null,
                PoliticalClaimRecognitionState.Unrecognized,
                null,
                null,
                fixture.World.CurrentDay,
                fixture.World.CurrentDay,
                new PoliticalKnowledgeProvenance(PoliticalKnowledgeSource.DirectObservation, "claim")),
            out PoliticalKnowledgeFailure recordFailure), Is.True, recordFailure.ToString());

        WorldStateSnapshot valid = Capture(fixture.World);
        WorldStatePoliticalKnowledgeSnapshot holder = valid.PoliticalKnowledge[0];
        WorldStatePoliticalKnowledgeObservationSnapshot claimObservation = holder.Observations[0];
        string[] staleFields = claimObservation.StateKey.Split(new[] { '\u001F' });
        staleFields[7] = "1";
        staleFields[8] = fixture.World.CurrentDay.ToString();
        WorldStateSnapshot staleKnowledge = new WorldStateSnapshot(
            valid.AbsoluteDay,
            persons: valid.Persons,
            politicalClaims: valid.PoliticalClaims,
            institutionIds: valid.InstitutionIds,
            politicalKnowledge: new[] {
                new WorldStatePoliticalKnowledgeSnapshot(
                    holder.HolderStableId,
                    holder.HolderKind,
                    holder.HolderPersonId,
                    holder.HolderInstitutionId,
                    new[] {
                        new WorldStatePoliticalKnowledgeObservationSnapshot(
                            claimObservation.IdentityKey,
                            claimObservation.FactKind,
                            claimObservation.ObservedAbsoluteDay,
                            claimObservation.ReceivedAbsoluteDay,
                            claimObservation.Source,
                            claimObservation.SourceReference,
                            claimObservation.SourcePersonId,
                            claimObservation.SourceInstitutionId,
                            string.Join("\u001F", staleFields))
                    })
            },
            politicalKnowledgeRevision: valid.PoliticalKnowledgeRevision,
            hasPoliticalKnowledgeState: true);
        WorldStateInvariantReport staleReport = WorldStateInvariantValidator.Validate(staleKnowledge);
        Assert.That(staleReport.IsValid, Is.True);

        string[] forgedFields = claimObservation.StateKey.Split(new[] { '\u001F' });
        forgedFields[5] = "999";
        WorldStatePoliticalKnowledgeObservationSnapshot forgedClaim = new WorldStatePoliticalKnowledgeObservationSnapshot(
            claimObservation.IdentityKey,
            claimObservation.FactKind,
            claimObservation.ObservedAbsoluteDay,
            claimObservation.ReceivedAbsoluteDay,
            claimObservation.Source,
            claimObservation.SourceReference,
            claimObservation.SourcePersonId,
            claimObservation.SourceInstitutionId,
            string.Join("\u001F", forgedFields));
        WorldStateSnapshot forged = new WorldStateSnapshot(
            valid.AbsoluteDay,
            persons: valid.Persons,
            politicalClaims: valid.PoliticalClaims,
            institutionIds: valid.InstitutionIds,
            politicalKnowledge: new[] {
                new WorldStatePoliticalKnowledgeSnapshot(
                    holder.HolderStableId,
                    holder.HolderKind,
                    holder.HolderPersonId,
                    holder.HolderInstitutionId,
                    new[] { forgedClaim })
            },
            politicalKnowledgeRevision: valid.PoliticalKnowledgeRevision,
            hasPoliticalKnowledgeState: true);
        WorldStateInvariantReport forgedReport = WorldStateInvariantValidator.Validate(forged);
        Assert.That(HasIssueCode(forgedReport, "PoliticalKnowledgeClaimStateInvalid"), Is.True, forgedReport.ToString());

        WorldStatePoliticalKnowledgeObservationSnapshot orphan = new WorldStatePoliticalKnowledgeObservationSnapshot(
            claimObservation.IdentityKey,
            claimObservation.FactKind,
            claimObservation.ObservedAbsoluteDay,
            claimObservation.ReceivedAbsoluteDay,
            PoliticalKnowledgeSource.SharedByPerson,
            "orphan",
            "person.unknown",
            null,
            claimObservation.StateKey);
        WorldStateSnapshot orphanSnapshot = new WorldStateSnapshot(
            valid.AbsoluteDay,
            persons: valid.Persons,
            politicalClaims: valid.PoliticalClaims,
            institutionIds: valid.InstitutionIds,
            politicalKnowledge: new[] {
                new WorldStatePoliticalKnowledgeSnapshot(
                    holder.HolderStableId,
                    holder.HolderKind,
                    holder.HolderPersonId,
                    holder.HolderInstitutionId,
                    new[] { orphan })
            },
            politicalKnowledgeRevision: valid.PoliticalKnowledgeRevision,
            hasPoliticalKnowledgeState: true);
        WorldStateInvariantReport orphanReport = WorldStateInvariantValidator.Validate(orphanSnapshot);
        Assert.That(HasIssueCode(orphanReport, "PoliticalKnowledgeProvenancePersonMissing"), Is.True, orphanReport.ToString());
    }

    private static PoliticalClaimId RegisterClaim(
        SimulationRuntime world,
        PersonId claimant,
        PersonId target)
    {
        PoliticalClaimId claimId = new PoliticalClaimId("claim.integration");
        Assert.That(world.TryRegisterPoliticalClaim(
            new PoliticalClaimRecord(
                claimId,
                claimant,
                PoliticalClaimType.StatusRecognition,
                PoliticalClaimTarget.ForPerson(target),
                PoliticalClaimBasis.ExplicitDecision,
                "integration",
                world.CurrentDay,
                null),
            out PoliticalClaimFailure failure), Is.True, failure.ToString());
        return claimId;
    }

    private static WorldStateSnapshot Capture(SimulationRuntime world)
    {
        return WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            simulationTime: world.SimulationTime,
            calendar: world.Calendar,
            personStore: world.PersonStore,
            institutionIds: new[] { "institution.court" },
            politicalClaims: world.PoliticalClaimRecords,
            politicalClaimRecognitions: world.PoliticalClaimRecognitionRecords,
            factions: world.FactionRecords,
            factionAffiliations: world.FactionAffiliationRecords,
            politicalSupports: world.PoliticalSupportRecords,
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

    private static Fixture CreateFixture()
    {
        PersonStore persons = new PersonStore();
        PersonId supporter = new PersonId("person.supporter");
        PersonId candidate = new PersonId("person.candidate");
        Assert.That(persons.TryRegister(new PersonRuntime(supporter, 0L), out PersonStoreFailure supporterFailure), Is.True, supporterFailure.ToString());
        Assert.That(persons.TryRegister(new PersonRuntime(candidate, 0L), out PersonStoreFailure candidateFailure), Is.True, candidateFailure.ToString());

        InstitutionStore institutions = new InstitutionStore();
        InstitutionId institution = new InstitutionId("institution.court");
        Assert.That(institutions.TryRegister(new InstitutionRecord(institution, "Court"), out InstitutionFoundationFailure institutionFailure), Is.True, institutionFailure.ToString());

        FactionStore factions = new FactionStore(persons);
        FactionId faction = new FactionId("faction.council");
        Assert.That(factions.TryRegister(new FactionRecord(faction, "Council", 0L), out FactionFoundationFailure factionFailure), Is.True, factionFailure.ToString());

        return new Fixture(
            new SimulationRuntime(
                new SimulationTime(0L),
                Array.Empty<CityRuntime>(),
                Array.Empty<NpcRuntime>(),
                economyEnabled: false,
                personStore: persons,
                institutionStore: institutions,
                factionStore: factions),
            supporter,
            candidate,
            faction,
            institution);
    }

    private sealed class Fixture
    {
        public SimulationRuntime World { get; }
        public PersonId Supporter { get; }
        public PersonId Candidate { get; }
        public FactionId Faction { get; }
        public InstitutionId Institution { get; }

        public Fixture(SimulationRuntime world, PersonId supporter, PersonId candidate, FactionId faction, InstitutionId institution)
        {
            World = world;
            Supporter = supporter;
            Candidate = candidate;
            Faction = faction;
            Institution = institution;
        }
    }
}
