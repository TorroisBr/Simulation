using NUnit.Framework;

public sealed class DailyV1MoneyTransferOperationTests
{
    [Test]
    public void SuccessfulTransferMutatesTheExactAccountsOnceAndReturnsExistingReceiptShape()
    {
        MoneyAccountRuntime source = new MoneyAccountRuntime(100f);
        MoneyAccountRuntime destination = new MoneyAccountRuntime(20f);

        EconomyTransactionResult result = DailyV1MoneyTransferOperation.TryExecute(
            source, destination, 30f, "npc:source", "npc:destination");

        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionType, Is.EqualTo(EconomyTransactionType.MoneyTransfer));
        Assert.That(result.MoneyEffect, Is.EqualTo(MoneyEffect.Transfer));
        Assert.That(result.Amount, Is.EqualTo(30f));
        Assert.That(result.TotalPrice, Is.EqualTo(30f));
        Assert.That(result.SourceRuntimeId, Is.EqualTo("npc:source"));
        Assert.That(result.DestinationRuntimeId, Is.EqualTo("npc:destination"));
        Assert.That(source.Balance, Is.EqualTo(70f));
        Assert.That(destination.Balance, Is.EqualTo(50f));
        Assert.That(source.Revision, Is.EqualTo(1L));
        Assert.That(destination.Revision, Is.EqualTo(1L));
    }

    [TestCase(-1f, EconomyTransactionFailureReason.InvalidInput)]
    [TestCase(float.NaN, EconomyTransactionFailureReason.InvalidInput)]
    [TestCase(float.PositiveInfinity, EconomyTransactionFailureReason.InvalidInput)]
    public void InvalidAmountIsRejectedWithoutMutation(float amount, EconomyTransactionFailureReason expected)
    {
        MoneyAccountRuntime source = new MoneyAccountRuntime(100f);
        MoneyAccountRuntime destination = new MoneyAccountRuntime(20f);

        EconomyTransactionResult result = DailyV1MoneyTransferOperation.TryExecute(
            source, destination, amount, "npc:source", "npc:destination");

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(expected));
        Assert.That(source.Balance, Is.EqualTo(100f));
        Assert.That(destination.Balance, Is.EqualTo(20f));
        Assert.That(source.Revision, Is.Zero);
        Assert.That(destination.Revision, Is.Zero);
    }

    [Test]
    public void FailedDestinationAdmissionCompensatesTheSourceBalance()
    {
        MoneyAccountRuntime source = new MoneyAccountRuntime(100f);
        MoneyAccountRuntime destination = new MoneyAccountRuntime(20f);
        source.BindP12MutationBoundary(() => true, () => { });
        destination.BindP12MutationBoundary(() => false, () => { });

        EconomyTransactionResult result = DailyV1MoneyTransferOperation.TryExecute(
            source, destination, 30f, "npc:source", "npc:destination");

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(source.Balance, Is.EqualTo(100f));
        Assert.That(destination.Balance, Is.EqualTo(20f));
        Assert.That(source.Revision, Is.EqualTo(2L));
        Assert.That(destination.Revision, Is.Zero);
    }

    [Test]
    public void SameAccountAndInsufficientFundsAreRejectedBeforeEitherWrite()
    {
        MoneyAccountRuntime source = new MoneyAccountRuntime(10f);
        EconomyTransactionResult sameAccount = DailyV1MoneyTransferOperation.TryExecute(
            source, source, 1f, "same", "same");
        EconomyTransactionResult insufficient = DailyV1MoneyTransferOperation.TryExecute(
            source, new MoneyAccountRuntime(0f), 11f, "source", "destination");

        Assert.That(sameAccount.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.SameAccount));
        Assert.That(insufficient.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientFunds));
        Assert.That(source.Balance, Is.EqualTo(10f));
        Assert.That(source.Revision, Is.Zero);
    }
}
