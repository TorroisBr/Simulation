using System;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1JusticeRecordsOwnerTests
{
    [Test]
    public void OrderedRowsAndSentenceWarrantIdentityRemainInOneCoreOwner()
    {
        DailyV1JusticeRecordsOwner owner = new DailyV1JusticeRecordsOwner();
        DailyV1JusticeWantedRecordOwner first = new DailyV1JusticeWantedRecordOwner(
            "npc/guard-target", "city/one", 12f, 3);
        DailyV1JusticeWantedRecordOwner second = new DailyV1JusticeWantedRecordOwner(
            "npc/other-target", "city/two", 5f, 1);
        DailyV1JusticeSentenceOwner sentence = new DailyV1JusticeSentenceOwner(
            "npc/guard-target", "city/one", first, 3, failedEscapeAttempts: 1,
            wasArrestedToday: false);

        Assert.That(owner.TryAddWantedRecord(first), Is.True);
        Assert.That(owner.TryAddWantedRecord(second), Is.True);
        Assert.That(owner.TryAddSentence(sentence), Is.True);
        Assert.That(owner.TryAddSentence(new DailyV1JusticeSentenceOwner(
            "npc/guard-target", "city/one", first, 2)), Is.True);

        Assert.That(owner.WantedRecords, Is.EqualTo(new[] { first, second }));
        Assert.That(owner.Sentences[0], Is.SameAs(sentence));
        Assert.That(owner.Sentences[0].Warrant, Is.SameAs(owner.WantedRecords[0]));
        Assert.That(owner.Sentences[1].Warrant, Is.SameAs(first));
    }

    [Test]
    public void JusticeRowsPreserveMutationValuesWhenUnbound()
    {
        DailyV1JusticeWantedRecordOwner warrant = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/one", 4f, 2);
        DailyV1JusticeSentenceOwner sentence = new DailyV1JusticeSentenceOwner(
            "npc/target", "city/one", warrant, 2);

        Assert.That(warrant.TryAddPenalty(3f, 2), Is.True);
        Assert.That(warrant.Bounty, Is.EqualTo(7f));
        Assert.That(warrant.SentenceDays, Is.EqualTo(4));
        Assert.That(sentence.TryRegisterFailedEscape(1), Is.True);
        Assert.That(sentence.FailedEscapeAttempts, Is.EqualTo(1));
        Assert.That(sentence.RemainingDays, Is.EqualTo(3));
        Assert.That(sentence.TryClearArrestedToday(), Is.True);
        Assert.That(sentence.WasArrestedToday, Is.False);
        Assert.That(sentence.TryAdvanceDay(), Is.True);
        Assert.That(sentence.RemainingDays, Is.EqualTo(2));
    }

    [Test]
    public void P12MutationAdmissionAndOwnerRevisionStayCoupled()
    {
        DailyV1JusticeRecordsOwner owner = new DailyV1JusticeRecordsOwner();
        DailyV1JusticeWantedRecordOwner warrant = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/one", 4f, 2);
        Assert.That(owner.TryAddWantedRecord(warrant), Is.True);
        bool admitted = true;
        int commits = 0;
        Assert.That(owner.TryBindP12MutationBoundary(() => admitted, () => commits++), Is.True);

        Assert.That(warrant.TryAddPenalty(3f, 1), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(commits, Is.EqualTo(1));

        admitted = false;
        Assert.That(warrant.TryResolve(), Is.False);
        Assert.That(warrant.IsResolved, Is.False);
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(commits, Is.EqualTo(1));
    }

    [Test]
    public void ActiveJusticeQueriesUseStableIdentityAndRetainOwnerOrder()
    {
        DailyV1JusticeRecordsOwner owner = new DailyV1JusticeRecordsOwner();
        DailyV1JusticeWantedRecordOwner resolved = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/one", 4f, 2);
        DailyV1JusticeWantedRecordOwner active = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/one", 8f, 4);
        DailyV1JusticeWantedRecordOwner otherCity = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/two", 9f, 5);
        Assert.That(owner.TryAddWantedRecord(resolved), Is.True);
        Assert.That(owner.TryAddWantedRecord(active), Is.True);
        Assert.That(owner.TryAddWantedRecord(otherCity), Is.True);
        Assert.That(resolved.TryResolve(), Is.True);

        DailyV1JusticeSentenceOwner expired = new DailyV1JusticeSentenceOwner(
            "npc/target", "city/one", active, 0);
        DailyV1JusticeSentenceOwner current = new DailyV1JusticeSentenceOwner(
            "npc/target", "city/one", active, 3);
        Assert.That(owner.TryAddSentence(expired), Is.True);
        Assert.That(owner.TryAddSentence(current), Is.True);

        Assert.That(owner.FindActiveWantedRecord("npc/target", "city/one"), Is.SameAs(active));
        Assert.That(owner.FindActiveWantedRecord("npc/target", "city/two"), Is.SameAs(otherCity));
        Assert.That(owner.FindActiveWantedRecord("npc/missing", "city/one"), Is.Null);
        Assert.That(owner.FindActiveWantedRecords("npc/target", "city/one"), Is.EqualTo(new[] { active }));
        Assert.That(owner.FindActiveWantedRecords("npc/target"),
            Is.EqualTo(new[] { active, otherCity }));
        Assert.That(owner.FindActiveSentence("npc/target"), Is.SameAs(current));
        Assert.That(owner.FindActiveSentences("npc/target"), Is.EqualTo(new[] { current }));
    }

    [Test]
    public void SentenceRejectsWarrantWithDifferentTargetOrCityIdentity()
    {
        DailyV1JusticeWantedRecordOwner warrant = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/one", 4f, 2);
        DailyV1JusticeRecordsOwner owner = new DailyV1JusticeRecordsOwner();
        Assert.That(owner.TryAddWantedRecord(warrant), Is.True);

        Assert.That(owner.TryAddSentence(new DailyV1JusticeSentenceOwner(
            "npc/other", "city/one", warrant, 2)), Is.False);
        Assert.That(owner.TryAddSentence(new DailyV1JusticeSentenceOwner(
            "npc/target", "city/two", warrant, 2)), Is.False);
        Assert.That(owner.Sentences, Is.Empty);
    }

    [Test]
    public void UnboundCompatibilityRowsCanRepresentMissingReferencesButCannotEnterOwner()
    {
        DailyV1JusticeRecordsOwner owner = new DailyV1JusticeRecordsOwner();
        DailyV1JusticeWantedRecordOwner missingWantedIdentity =
            new DailyV1JusticeWantedRecordOwner(null, null, 1f, 1);
        DailyV1JusticeSentenceOwner missingSentenceIdentity =
            new DailyV1JusticeSentenceOwner(null, null, null, 1);

        Assert.That(owner.TryAddWantedRecord(missingWantedIdentity), Is.False);
        Assert.That(owner.TryAddSentence(missingSentenceIdentity), Is.False);
        Assert.That(owner.WantedRecords, Is.Empty);
        Assert.That(owner.Sentences, Is.Empty);
    }

    [Test]
    public void FailedBoundaryBindingLeavesOwnerUnboundAndRowsUnchanged()
    {
        DailyV1JusticeRecordsOwner owner = new DailyV1JusticeRecordsOwner();
        DailyV1JusticeWantedRecordOwner row = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/one", 4f, 2);
        Assert.That(owner.TryAddWantedRecord(row), Is.True);
        Assert.That(row.TryBindP12MutationBoundary(() => true, () => { }), Is.True);

        Assert.That(owner.TryBindP12MutationBoundary(() => true, () => { }), Is.False);
        Assert.That(owner.HasP12MutationBoundary, Is.False);
        Assert.That(owner.Revision, Is.EqualTo(0));
    }

    [Test]
    public void BoundOwnerRejectsMutationsAfterRuntimeFault()
    {
        DailyV1JusticeRecordsOwner owner = new DailyV1JusticeRecordsOwner();
        DailyV1JusticeWantedRecordOwner warrant = new DailyV1JusticeWantedRecordOwner(
            "npc/target", "city/one", 4f, 2);
        Assert.That(owner.TryAddWantedRecord(warrant), Is.True);
        AuthoritativeMutationGuard guard = new AuthoritativeMutationGuard();
        Assert.That(owner.TryBindMutationGuard(guard), Is.True);
        guard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        Assert.Throws<InvalidOperationException>(() => warrant.TryAddPenalty(1f, 0));
        Assert.That(warrant.Bounty, Is.EqualTo(4f));
    }
}
