using System;
using NUnit.Framework;

[TestFixture]
public sealed class MoneyAccountRuntimeCoreTests
{
    [Test]
    public void CreditAndDebitUseTheSharedOwnerAndAdvanceItsRevisionOnce()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryCredit(15f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(40f));
        Assert.That(account.Revision, Is.EqualTo(1L));
        Assert.That(account.TryDebit(10f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(30f));
        Assert.That(account.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void ZeroAndRejectedOperationsDoNotChangeStateOrRevision()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryCredit(0f), Is.True);
        Assert.That(account.TryDebit(0f), Is.True);
        Assert.That(account.TryCredit(float.NaN), Is.False);
        Assert.That(account.TryDebit(float.PositiveInfinity), Is.False);
        Assert.That(account.TryDebit(26f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
        Assert.That(account.Revision, Is.Zero);
    }

    [Test]
    public void P12AdmissionRejectsBeforeMutationAndCommitCallbackRunsAfterSuccess()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(20f);
        bool admitted = false;
        int commits = 0;
        MoneyAccountOwnerBridge.BindP12MutationBoundary(account, () => admitted, () => commits++);

        Assert.That(account.TryDebit(5f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(20f));
        Assert.That(account.Revision, Is.Zero);
        Assert.That(commits, Is.Zero);

        admitted = true;
        Assert.That(account.TryDebit(5f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(15f));
        Assert.That(account.Revision, Is.EqualTo(1L));
        Assert.That(commits, Is.EqualTo(1));
        Assert.That(MoneyAccountOwnerBridge.HasP12MutationBoundary(account), Is.True);
        Assert.That(MoneyAccountOwnerBridge.UnbindP12MutationBoundary(account, null, null), Is.False);
    }

    [Test]
    public void PreparedInstallAndSnapshotKeepExactRevisionSemantics()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(10f);
        Assert.That(MoneyAccountOwnerBridge.CanInstall(account, 0L, 2L, 25f), Is.True);
        MoneyAccountOwnerBridge.InstallPrepared(account, 0L, 2L, 25f);
        Assert.That(account.Balance, Is.EqualTo(25f));
        Assert.That(account.Revision, Is.EqualTo(2L));
        Assert.That(MoneyAccountOwnerBridge.CanInstall(account, 0L, 1L, 30f), Is.False);

        Assert.That(MoneyAccountOwnerBridge.TryCreateFromOwnerSnapshot(25f, 7L, out MoneyAccountRuntime restored), Is.True);
        Assert.That(restored, Is.Not.SameAs(account));
        Assert.That(restored.Balance, Is.EqualTo(25f));
        Assert.That(restored.Revision, Is.EqualTo(7L));
        Assert.That(MoneyAccountOwnerBridge.HasP12MutationBoundary(restored), Is.False);
    }

    [Test]
    public void ThrowingCommitNotificationDoesNotUndoCommittedBalance()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(10f);
        MoneyAccountOwnerBridge.BindP12MutationBoundary(account, () => true, () => throw new InvalidOperationException());

        Assert.That(account.TryCredit(5f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(15f));
        Assert.That(account.Revision, Is.EqualTo(1L));
    }
}
