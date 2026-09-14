using System;
using NUnit.Framework;

public sealed class MoneyAccountTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void MoneyAccount_ZeroInitialBalanceIsValid()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(0f);

        Assert.That(account.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void MoneyAccount_PositiveInitialBalanceIsPreserved()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(125.5f);

        Assert.That(account.Balance, Is.EqualTo(125.5f));
    }

    [Test]
    public void MoneyAccount_NegativeInitialBalanceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MoneyAccountRuntime(-1f));
    }

    [Test]
    public void MoneyAccount_NaNInitialBalanceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MoneyAccountRuntime(float.NaN));
    }

    [Test]
    public void MoneyAccount_PositiveInfinityInitialBalanceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MoneyAccountRuntime(float.PositiveInfinity));
    }

    [Test]
    public void MoneyAccount_NegativeInfinityInitialBalanceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MoneyAccountRuntime(float.NegativeInfinity));
    }

    [Test]
    public void MoneyAccount_CreditPositiveAmountIncreasesBalance()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryCredit(15f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(40f));
    }

    [Test]
    public void MoneyAccount_CreditZeroDoesNotChangeBalance()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.CanCredit(0f), Is.True);
        Assert.That(account.TryCredit(0f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_CreditNegativeIsRejectedWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryCredit(-1f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_CreditNaNIsRejectedWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryCredit(float.NaN), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_CreditInfinityIsRejectedWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryCredit(float.PositiveInfinity), Is.False);
        Assert.That(account.TryCredit(float.NegativeInfinity), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_CreditOverflowIsRejectedWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(float.MaxValue);

        Assert.That(account.TryCredit(float.MaxValue), Is.False);
        Assert.That(account.Balance, Is.EqualTo(float.MaxValue));
    }

    [Test]
    public void MoneyAccount_PositiveCreditThatCannotChangeFloatIsRejected()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(float.MaxValue);
        float before = account.Balance;

        Assert.That(account.CanCredit(10f), Is.False);
        Assert.That(account.TryCredit(10f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(before));
    }

    [Test]
    public void MoneyAccount_DebitAvailableBalanceDecreasesBalance()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryDebit(15f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(10f));
    }

    [Test]
    public void MoneyAccount_DebitExactBalanceReachesZero()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryDebit(25f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(0f));
    }

    [Test]
    public void MoneyAccount_PositiveDebitThatCannotChangeFloatIsRejected()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(float.MaxValue);
        float before = account.Balance;

        Assert.That(account.CanDebit(10f), Is.False);
        Assert.That(account.TryDebit(10f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(before));
    }

    [Test]
    public void MoneyAccount_DebitMoreThanBalanceFailsWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryDebit(26f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_DebitNegativeIsRejectedWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryDebit(-1f), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_DebitNaNIsRejectedWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryDebit(float.NaN), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_DebitInfinityIsRejectedWithoutMutation()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.TryDebit(float.PositiveInfinity), Is.False);
        Assert.That(account.TryDebit(float.NegativeInfinity), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_DebitZeroDoesNotChangeBalance()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.CanDebit(0f), Is.True);
        Assert.That(account.TryDebit(0f), Is.True);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void MoneyAccount_CanDebitIsAReadOnlyQuery()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(25f);

        Assert.That(account.CanDebit(25f), Is.True);
        Assert.That(account.CanDebit(26f), Is.False);
        Assert.That(account.CanDebit(-1f), Is.False);
        Assert.That(account.CanDebit(float.NaN), Is.False);
        Assert.That(account.CanDebit(float.PositiveInfinity), Is.False);
        Assert.That(account.Balance, Is.EqualTo(25f));
    }

    [Test]
    public void NpcRuntime_MoneyProjectsMoneyAccountBalance()
    {
        NpcRuntime npc = CreateNpc(100f);

        Assert.That(npc.Money, Is.EqualTo(100f));
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(100f));

        npc.AddMoney(25f);

        Assert.That(npc.Money, Is.EqualTo(125f));
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(125f));

        Assert.That(npc.TrySpendMoney(40f), Is.True);
        Assert.That(npc.Money, Is.EqualTo(85f));
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(85f));
    }

    [Test]
    public void NpcRuntime_AddMoneyAndTrySpendMoneyDelegateToAccount()
    {
        NpcRuntime npc = CreateNpc(100f);

        npc.AddMoney(50f);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(150f));
        Assert.That(npc.TrySpendMoney(30f), Is.True);
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(120f));
        Assert.That(npc.Money, Is.EqualTo(npc.MoneyAccount.Balance));
    }

    [Test]
    public void NpcRuntime_CompatibilityMethodsRejectInvalidAmountsWithoutMutation()
    {
        NpcRuntime npc = CreateNpc(100f);

        npc.AddMoney(-1f);
        npc.AddMoney(float.NaN);
        npc.AddMoney(float.PositiveInfinity);

        Assert.That(npc.TrySpendMoney(-1f), Is.False);
        Assert.That(npc.TrySpendMoney(float.NaN), Is.False);
        Assert.That(npc.TrySpendMoney(float.PositiveInfinity), Is.False);
        Assert.That(npc.Money, Is.EqualTo(100f));
        Assert.That(npc.MoneyAccount.Balance, Is.EqualTo(100f));
    }

    [Test]
    public void NpcRuntime_InvalidInitialMoneyIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateNpc(-1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateNpc(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateNpc(float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateNpc(float.NegativeInfinity));
    }

    [Test]
    public void MoneyAccount_BalancesRemainValidAcrossValidAndInvalidOperations()
    {
        MoneyAccountRuntime account = new MoneyAccountRuntime(100f);

        for (int i = 0; i < 100; i++)
        {
            Assert.That(account.TryCredit(7.5f), Is.True);
            Assert.That(account.TryDebit(3.25f), Is.True);
            Assert.That(account.TryDebit(1000000f), Is.False);
            Assert.That(account.TryCredit(float.NaN), Is.False);
            Assert.That(account.TryDebit(float.PositiveInfinity), Is.False);
            Assert.That(account.Balance, Is.GreaterThanOrEqualTo(0f));
            Assert.That(float.IsNaN(account.Balance), Is.False);
            Assert.That(float.IsInfinity(account.Balance), Is.False);
        }
    }

    private static NpcRuntime CreateNpc(float initialMoney)
    {
        return new NpcRuntime(
            "npc-money-account",
            SimulationTestFactory.CreateNpc("npc-money-account"),
            null,
            initialMoney);
    }
}
