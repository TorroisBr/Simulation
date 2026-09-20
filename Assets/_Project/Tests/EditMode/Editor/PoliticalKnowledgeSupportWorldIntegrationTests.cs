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
            politicalClaims: world.PoliticalClaimRecords,
            factions: world.FactionRecords,
            factionAffiliations: world.FactionAffiliationRecords,
            politicalSupports: world.PoliticalSupportRecords));
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
