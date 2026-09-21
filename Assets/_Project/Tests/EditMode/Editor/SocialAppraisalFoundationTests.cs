using NUnit.Framework;

public sealed class SocialAppraisalFoundationTests
{
    [Test]
    public void ReactionIdAndEvaluatorRemainStableAndTargetIsSeparateFromAttribution()
    {
        PersonId evaluator = new PersonId("person.maria");
        SocialSourceReference source = new SocialSourceReference("crime.theft", "theft-1");
        SocialReactionTarget target = SocialReactionTarget.ForTheftOutcome("theft-1");
        SocialPerceivedAttribution attribution = SocialPerceivedAttribution.Unknown();
        SocialCognitiveBasis basis = new SocialCognitiveBasis(
            SocialCognitiveBasisKind.DirectExperience,
            "loss");

        SocialReactionId firstId = SocialReactionId.Create(
            evaluator, source, target, attribution, basis,
            SocialReactionValence.Negative,
            SocialReactionSalience.High,
            100L);
        SocialReactionId secondId = SocialReactionId.Create(
            evaluator, source, target, attribution, basis,
            SocialReactionValence.Negative,
            SocialReactionSalience.High,
            100L);

        SocialReaction reaction = new SocialReaction(
            firstId,
            evaluator,
            source,
            target,
            attribution,
            SocialReactionValence.Negative,
            SocialReactionSalience.High,
            basis,
            100L);

        Assert.That(firstId, Is.EqualTo(secondId));
        Assert.That(reaction.EvaluatorPersonId, Is.EqualTo(evaluator));
        Assert.That(reaction.Target, Is.Not.SameAs(reaction.PerceivedAttribution));
        Assert.That(reaction.Target.Kind, Is.EqualTo(SocialReactionTargetKind.TheftOutcome));
    }

    [Test]
    public void UnknownAttributionIsDistinctFromNotApplicableAndBelievedPersonDoesNotRewriteSource()
    {
        SocialPerceivedAttribution unknown = SocialPerceivedAttribution.Unknown();
        SocialPerceivedAttribution notApplicable = SocialPerceivedAttribution.NotApplicable();
        PersonId joao = new PersonId("person.joao");
        SocialPerceivedAttribution believed = SocialPerceivedAttribution.BelievedPerson(joao);

        Assert.That(unknown.Kind, Is.Not.EqualTo(notApplicable.Kind));
        Assert.That(unknown.Equals(notApplicable), Is.False);
        Assert.That(believed.PersonId, Is.EqualTo(joao));
        Assert.That(believed.Kind, Is.EqualTo(SocialPerceivedAttributionKind.BelievedPerson));
    }

    [Test]
    public void SupersessionPreservesHistoryAndRebuildsCurrentView()
    {
        PersonStore persons = new PersonStore();
        PersonId maria = new PersonId("person.maria");
        persons.TryRegister(new PersonRuntime(maria), out _);
        SocialReactionStore store = new SocialReactionStore(persons);
        SocialSourceReference source = new SocialSourceReference("crime.theft", "theft-1");
        SocialReactionTarget target = SocialReactionTarget.ForTheftOutcome("theft-1");
        SocialCognitiveBasis basis = new SocialCognitiveBasis(
            SocialCognitiveBasisKind.DirectExperience,
            "victim-loss");

        Assert.That(store.TryRecordAppraisal(
            maria, source, target, SocialPerceivedAttribution.Unknown(), basis,
            SocialAppraisalResult.Reaction(SocialReactionValence.Negative, SocialReactionSalience.High),
            100L, null, out SocialReaction first, out SocialReactionStoreFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(store.TryRecordAppraisal(
            maria, source, target, SocialPerceivedAttribution.BelievedPerson(new PersonId("person.joao")), basis,
            SocialAppraisalResult.Reaction(SocialReactionValence.Negative, SocialReactionSalience.High),
            103L, first.ReactionId, out SocialReaction second, out SocialReactionStoreFailure secondFailure), Is.True, secondFailure.ToString());

        Assert.That(store.HistoricalReactions, Has.Count.EqualTo(2));
        Assert.That(store.GetCurrentReactions(), Has.Count.EqualTo(1));
        Assert.That(store.GetCurrentReactions()[0].ReactionId, Is.EqualTo(second.ReactionId));
        Assert.That(store.HistoricalReactions[0].PerceivedAttribution.Kind, Is.EqualTo(SocialPerceivedAttributionKind.Unknown));
        Assert.That(store.HistoricalReactions[1].SupersedesReactionId, Is.EqualTo(first.ReactionId));
    }

    [Test]
    public void NoReactionPreviewDoesNotMutateStoreAndNeutralHasNoPersistedRecord()
    {
        PersonStore persons = new PersonStore();
        PersonId maria = new PersonId("person.maria");
        persons.TryRegister(new PersonRuntime(maria), out _);
        SocialReactionStore store = new SocialReactionStore(persons);
        SocialSourceReference source = new SocialSourceReference("crime.theft", "theft-1");
        SocialReactionTarget target = SocialReactionTarget.ForTheftOutcome("theft-1");
        SocialCognitiveBasis basis = new SocialCognitiveBasis(
            SocialCognitiveBasisKind.DirectObservation,
            "nothing-significant");

        Assert.That(store.TryRecordAppraisal(
            maria, source, target, SocialPerceivedAttribution.NotApplicable(), basis,
            SocialAppraisalResult.NoReaction(),
            100L, null, out SocialReaction reaction, out SocialReactionStoreFailure failure), Is.True, failure.ToString());
        Assert.That(reaction, Is.Null);
        Assert.That(store.Count, Is.EqualTo(0));
        Assert.That(store.HistoricalReactions, Is.Empty);
        Assert.That(store.GetCurrentReactions(), Is.Empty);
    }

    [Test]
    public void StoreSnapshotIsDeterministicAndRejectsCrossThreadSupersession()
    {
        PersonStore persons = new PersonStore();
        PersonId maria = new PersonId("person.maria");
        persons.TryRegister(new PersonRuntime(maria), out _);
        SocialReactionStore store = new SocialReactionStore(persons);
        SocialCognitiveBasis basis = new SocialCognitiveBasis(SocialCognitiveBasisKind.KnownFact, "fact");
        SocialReaction sourceB = CreateReaction(maria, "theft-b", "theft-b", basis, 100L, null);
        SocialReaction sourceA = CreateReaction(maria, "theft-a", "theft-a", basis, 100L, null);
        Assert.That(store.TryRecord(sourceB, out _), Is.True);
        Assert.That(store.TryRecord(sourceA, out _), Is.True);

        Assert.That(store.HistoricalReactions[0].Source.StableId, Is.EqualTo("theft-a"));
        Assert.That(store.HistoricalReactions[1].Source.StableId, Is.EqualTo("theft-b"));

        SocialReaction crossThread = CreateReaction(
            maria,
            "theft-c",
            "theft-c",
            basis,
            101L,
            sourceA.ReactionId);
        Assert.That(store.TryRecord(crossThread, out SocialReactionStoreFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SocialReactionStoreFailureCode.SupersessionThreadMismatch));
    }

    [Test]
    public void SupersessionCannotBranchFromOneHistoricalReaction()
    {
        PersonStore persons = new PersonStore();
        PersonId maria = new PersonId("person.maria");
        persons.TryRegister(new PersonRuntime(maria), out _);
        SocialReactionStore store = new SocialReactionStore(persons);
        SocialCognitiveBasis basis = new SocialCognitiveBasis(SocialCognitiveBasisKind.KnownFact, "fact");
        SocialReaction first = CreateReaction(maria, "theft-branch", "theft-branch", basis, 100L, null);
        Assert.That(store.TryRecord(first, out _), Is.True);

        SocialReaction second = CreateReaction(
            maria, "theft-branch", "theft-branch", basis, 101L, first.ReactionId);
        Assert.That(store.TryRecord(second, out _), Is.True);

        SocialReaction branch = new SocialReaction(
            SocialReactionId.Create(
                maria,
                second.Source,
                second.Target,
                SocialPerceivedAttribution.BelievedPerson(new PersonId("person.pedro")),
                basis,
                SocialReactionValence.Negative,
                SocialReactionSalience.High,
                102L,
                new SocialReactionId(first.ReactionId.Value)),
            maria,
            second.Source,
            second.Target,
            SocialPerceivedAttribution.BelievedPerson(new PersonId("person.pedro")),
            SocialReactionValence.Negative,
            SocialReactionSalience.High,
            basis,
            102L,
            new SocialReactionId(first.ReactionId.Value));

        Assert.That(store.TryRecord(branch, out SocialReactionStoreFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SocialReactionStoreFailureCode.SupersessionAlreadyUsed));
        Assert.That(store.GetCurrentReactions(), Has.Count.EqualTo(1));
        Assert.That(store.GetCurrentReactions()[0].ReactionId, Is.EqualTo(second.ReactionId));
    }

    private static SocialReaction CreateReaction(
        PersonId evaluator,
        string sourceId,
        string targetId,
        SocialCognitiveBasis basis,
        long day,
        SocialReactionId supersedes)
    {
        SocialSourceReference source = new SocialSourceReference("crime.theft", sourceId);
        SocialReactionTarget target = SocialReactionTarget.ForTheftOutcome(targetId);
        SocialPerceivedAttribution attribution = SocialPerceivedAttribution.Unknown();
        SocialReactionId id = SocialReactionId.Create(
            evaluator, source, target, attribution, basis,
            SocialReactionValence.Negative, SocialReactionSalience.Medium,
            day, supersedes);
        return new SocialReaction(
            id, evaluator, source, target, attribution,
            SocialReactionValence.Negative, SocialReactionSalience.Medium,
            basis, day, supersedes);
    }
}
