using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PoliticalSupportFoundationTests
{
    [Test]
    public void SourcesAndTargetsUseTypedStableIdentity()
    {
        PersonId personId = new PersonId("person.supporter");
        FactionId factionId = new FactionId("faction.council");
        PoliticalClaimId claimId = new PoliticalClaimId("claim.office");

        PoliticalSupportSource personSource = PoliticalSupportSource.ForPerson(personId);
        PoliticalSupportSource equivalentPersonSource = PoliticalSupportSource.ForPerson(new PersonId("person.supporter"));
        PoliticalSupportSource factionSource = PoliticalSupportSource.ForFaction(factionId);
        PoliticalSupportTarget claimTarget = PoliticalSupportTarget.ForClaim(claimId);
        PoliticalSupportTarget candidateTarget = PoliticalSupportTarget.ForSuccessionCandidate(new PersonId("person.candidate"));

        Assert.That(personSource.Kind, Is.EqualTo(PoliticalSupportSourceKind.Person));
        Assert.That(personSource.PersonId, Is.EqualTo(personId));
        Assert.That(personSource.FactionId, Is.Null);
        Assert.That(personSource, Is.EqualTo(equivalentPersonSource));
        Assert.That(factionSource.Kind, Is.EqualTo(PoliticalSupportSourceKind.Faction));
        Assert.That(factionSource.FactionId, Is.EqualTo(factionId));
        Assert.That(claimTarget.Kind, Is.EqualTo(PoliticalSupportTargetKind.PoliticalClaim));
        Assert.That(claimTarget.PoliticalClaimId, Is.EqualTo(claimId));
        Assert.That(candidateTarget.Kind, Is.EqualTo(PoliticalSupportTargetKind.SuccessionCandidate));
        Assert.That(candidateTarget.SuccessionCandidatePersonId, Is.EqualTo(new PersonId("person.candidate")));
        Assert.That(claimTarget, Is.Not.EqualTo(candidateTarget));
        Assert.That(typeof(PoliticalSupportSource).GetProperty("NpcRuntimeId"), Is.Null);
        Assert.That(typeof(PoliticalSupportTarget).GetProperty("NpcRuntimeId"), Is.Null);
    }

    [Test]
    public void SupportAndOppositionAreExplicitAndOnlyOneCanBeActiveForAPair()
    {
        PoliticalSupportStore store = CreateStore();
        PoliticalSupportSource source = PoliticalSupportSource.ForFaction(new FactionId("faction.council"));
        PoliticalSupportTarget target = PoliticalSupportTarget.ForPoliticalClaim(new PoliticalClaimId("claim.office"));

        Assert.That(store.TryRegister(
            new PoliticalSupportRelationRecord(
                new PoliticalSupportRelationId("relation.support"),
                source,
                target,
                PoliticalSupportDisposition.Support,
                0L),
            out PoliticalSupportFailure supportFailure), Is.True, supportFailure.ToString());

        Assert.That(store.TryRegister(
            new PoliticalSupportRelationRecord(
                new PoliticalSupportRelationId("relation.oppose-overlap"),
                source,
                target,
                PoliticalSupportDisposition.Oppose,
                0L),
            out PoliticalSupportFailure overlapFailure), Is.False);
        Assert.That(overlapFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.ActiveRelationAlreadyExists));

        Assert.That(store.TryProposeEnd(
            new PoliticalSupportRelationId("relation.support"),
            1L,
            out PoliticalSupportEndTransition end,
            out PoliticalSupportFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(store.TryApplyEnd(end, 1L, out PoliticalSupportFailure endFailure), Is.True, endFailure.ToString());

        Assert.That(store.TryRegister(
            new PoliticalSupportRelationRecord(
                new PoliticalSupportRelationId("relation.oppose"),
                source,
                target,
                PoliticalSupportDisposition.Oppose,
                2L),
            out PoliticalSupportFailure oppositionFailure), Is.True, oppositionFailure.ToString());

        Assert.That(store.GetForPair(source, target), Has.Count.EqualTo(2));
        Assert.That(store.GetForPair(source, target)[0].Disposition, Is.EqualTo(PoliticalSupportDisposition.Support));
        Assert.That(store.GetForPair(source, target)[1].Disposition, Is.EqualTo(PoliticalSupportDisposition.Oppose));
    }

    [Test]
    public void EndingAndReAddingRetainsImmutableRelationHistory()
    {
        PoliticalSupportStore store = CreateStore();
        PoliticalSupportSource source = PoliticalSupportSource.ForPerson(new PersonId("person.supporter"));
        PoliticalSupportTarget target = PoliticalSupportTarget.ForSuccessionCandidate(new PersonId("person.candidate"));
        PoliticalSupportRelationRecord original = new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("relation.first"),
            source,
            target,
            PoliticalSupportDisposition.Support,
            3L);

        Assert.That(store.TryRegister(original, out PoliticalSupportFailure registrationFailure), Is.True, registrationFailure.ToString());
        Assert.That(store.TryProposeEnd(source, target, 5L, out PoliticalSupportEndTransition end, out PoliticalSupportFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(store.TryApplyEnd(end, 5L, out PoliticalSupportFailure endFailure), Is.True, endFailure.ToString());

        Assert.That(original.IsActive, Is.True);
        Assert.That(store.TryRegister(
            new PoliticalSupportRelationRecord(
                new PoliticalSupportRelationId("relation.second"),
                source,
                target,
                PoliticalSupportDisposition.Support,
                6L),
            out PoliticalSupportFailure reAddFailure), Is.True, reAddFailure.ToString());

        IReadOnlyList<PoliticalSupportRelationRecord> history = store.GetForPair(source, target);
        Assert.That(history, Has.Count.EqualTo(2));
        Assert.That(history[0].RelationId.Value, Is.EqualTo("relation.first"));
        Assert.That(history[0].EndedAbsoluteDay, Is.EqualTo(5L));
        Assert.That(history[1].RelationId.Value, Is.EqualTo("relation.second"));
        Assert.That(history[1].IsActive, Is.True);
        Assert.That(store.TryGetActive(source, target, out PoliticalSupportRelationRecord active), Is.True);
        Assert.That(active.RelationId.Value, Is.EqualTo("relation.second"));
    }

    [Test]
    public void StaleEndTransitionsAreRejectedAndQueriesAreDeterministicallySorted()
    {
        PoliticalSupportStore store = CreateStore();
        PoliticalSupportSource firstSource = PoliticalSupportSource.ForPerson(new PersonId("person.a"));
        PoliticalSupportSource secondSource = PoliticalSupportSource.ForPerson(new PersonId("person.b"));
        PoliticalSupportTarget claimTarget = PoliticalSupportTarget.ForClaim(new PoliticalClaimId("claim.z"));
        PoliticalSupportTarget candidateTarget = PoliticalSupportTarget.ForSuccessionCandidate(new PersonId("person.candidate"));

        Register(store, "relation.z", secondSource, candidateTarget, PoliticalSupportDisposition.Support, 0L);
        Register(store, "relation.b", firstSource, claimTarget, PoliticalSupportDisposition.Support, 0L);
        Register(store, "relation.a", firstSource, candidateTarget, PoliticalSupportDisposition.Oppose, 0L);

        Assert.That(store.Records[0].RelationId.Value, Is.EqualTo("relation.b"));
        Assert.That(store.Records[1].RelationId.Value, Is.EqualTo("relation.a"));
        Assert.That(store.Records[2].RelationId.Value, Is.EqualTo("relation.z"));
        Assert.That(store.GetForSource(firstSource)[0].Target.Kind, Is.EqualTo(PoliticalSupportTargetKind.PoliticalClaim));
        Assert.That(store.GetForSource(firstSource)[1].Target.Kind, Is.EqualTo(PoliticalSupportTargetKind.SuccessionCandidate));
        Assert.That(store.GetForTarget(candidateTarget)[0].Source.Value, Is.EqualTo("person.a"));
        Assert.That(store.GetForTarget(candidateTarget)[1].Source.Value, Is.EqualTo("person.b"));

        Assert.That(store.TryProposeEnd(
            new PoliticalSupportRelationId("relation.a"),
            0L,
            out PoliticalSupportEndTransition staleByRevision,
            out PoliticalSupportFailure revisionProposalFailure), Is.True, revisionProposalFailure.ToString());
        Register(
            store,
            "relation.c",
            PoliticalSupportSource.ForFaction(new FactionId("faction.new")),
            PoliticalSupportTarget.ForClaim(new PoliticalClaimId("claim.new")),
            PoliticalSupportDisposition.Support,
            0L);
        Assert.That(store.TryApplyEnd(staleByRevision, 0L, out PoliticalSupportFailure revisionFailure), Is.False);
        Assert.That(revisionFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.StaleRelation));

        Assert.That(store.TryProposeEnd(
            new PoliticalSupportRelationId("relation.a"),
            0L,
            out PoliticalSupportEndTransition staleByDay,
            out _), Is.True);
        Assert.That(store.TryApplyEnd(staleByDay, 1L, out PoliticalSupportFailure dayFailure), Is.False);
        Assert.That(dayFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.StaleWorldDay));
    }

    [Test]
    public void DuplicateInvalidAndCrossStoreOperationsFailWithoutAliasing()
    {
        PoliticalSupportStore first = CreateStore();
        PoliticalSupportStore second = CreateStore();
        PoliticalSupportSource source = PoliticalSupportSource.ForPerson(new PersonId("person.supporter"));
        PoliticalSupportTarget target = PoliticalSupportTarget.ForClaim(new PoliticalClaimId("claim.office"));
        PoliticalSupportRelationRecord relation = new PoliticalSupportRelationRecord(
            new PoliticalSupportRelationId("relation.one"),
            source,
            target,
            PoliticalSupportDisposition.Support,
            0L);

        Assert.That(first.TryRegister(null, out PoliticalSupportFailure invalidFailure), Is.False);
        Assert.That(invalidFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.InvalidRelation));
        Assert.That(first.TryRegister(relation, out _), Is.True);
        Assert.That(first.TryRegister(relation, out PoliticalSupportFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.DuplicateRelationId));

        Assert.That(first.TryProposeEnd(
            relation.RelationId,
            0L,
            out PoliticalSupportEndTransition transition,
            out _), Is.True);
        Assert.That(second.TryApplyEnd(transition, 0L, out PoliticalSupportFailure crossStoreFailure), Is.False);
        Assert.That(crossStoreFailure.Code, Is.EqualTo(PoliticalSupportFailureCode.WrongSupportStore));
        Assert.That(second.Count, Is.EqualTo(0));

        IReadOnlyList<PoliticalSupportRelationRecord> snapshot = first.Records;
        Assert.That(((IList<PoliticalSupportRelationRecord>)snapshot).IsReadOnly, Is.True);
        Assert.That(first.Count, Is.EqualTo(1));
        Assert.That(first.GetForSource(PoliticalSupportSource.ForPerson(new PersonId("person.supporter"))), Has.Count.EqualTo(1));
    }

    private static void Register(
        PoliticalSupportStore store,
        string relationId,
        PoliticalSupportSource source,
        PoliticalSupportTarget target,
        PoliticalSupportDisposition disposition,
        long startedAbsoluteDay)
    {
        Assert.That(store.TryRegister(
            new PoliticalSupportRelationRecord(new PoliticalSupportRelationId(relationId), source, target, disposition, startedAbsoluteDay),
            out PoliticalSupportFailure failure), Is.True, failure.ToString());
    }

    private static PoliticalSupportStore CreateStore()
    {
        PersonStore personStore = new PersonStore();
        string[] personIds =
        {
            "person.supporter", "person.candidate", "person.a", "person.b"
        };
        foreach (string personId in personIds)
        {
            Assert.That(
                personStore.TryRegister(new PersonRuntime(new PersonId(personId)), out PersonStoreFailure personFailure),
                Is.True,
                personFailure.ToString());
        }

        FactionStore factionStore = new FactionStore(personStore);
        foreach (string factionId in new[] { "faction.council", "faction.new" })
        {
            Assert.That(
                factionStore.TryRegister(
                    new FactionRecord(new FactionId(factionId), factionId, 0L),
                    out FactionFoundationFailure factionFailure),
                Is.True,
                factionFailure.ToString());
        }

        PoliticalClaimStore claimStore = new PoliticalClaimStore();
        foreach (string claimId in new[] { "claim.office", "claim.z", "claim.new" })
        {
            Assert.That(
                claimStore.TryRegister(
                    new PoliticalClaimRecord(
                        new PoliticalClaimId(claimId),
                        new PersonId("person.supporter"),
                        PoliticalClaimType.StatusRecognition,
                        PoliticalClaimTarget.ForPerson(new PersonId("person.candidate")),
                        PoliticalClaimBasis.ExplicitDecision,
                        string.Empty,
                        0L,
                        null),
                    out PoliticalClaimFailure claimFailure),
                Is.True,
                claimFailure.ToString());
        }

        return new PoliticalSupportStore(personStore, factionStore, claimStore);
    }
}
