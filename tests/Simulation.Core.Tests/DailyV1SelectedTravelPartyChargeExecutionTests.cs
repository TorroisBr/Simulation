using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedTravelPartyChargeExecutionTests
{
    [Test]
    public void SelectedGroupChargeUsesExactCoreAccountsInMemberOrder()
    {
        CreateMembers(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
        float firstBefore = first.MoneyAccount.Balance;
        float secondBefore = second.MoneyAccount.Balance;
        long firstRevision = first.MoneyAccount.Revision;
        long secondRevision = second.MoneyAccount.Revision;

        EconomyTransactionResult result = DailyV1SelectedTravelPartyChargeExecution.TryCharge(
            graph, new[] { first, second }, 7f);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TransactionType, Is.EqualTo(EconomyTransactionType.TravelCharge));
        Assert.That(result.MoneyEffect, Is.EqualTo(MoneyEffect.ExplicitSink));
        Assert.That(result.TotalPrice, Is.EqualTo(14f));
        Assert.That(result.ParticipantRuntimeIds, Is.EqualTo(new[] { first.RuntimeId, second.RuntimeId }));
        Assert.That(first.MoneyAccount.Balance, Is.EqualTo(firstBefore - 7f));
        Assert.That(second.MoneyAccount.Balance, Is.EqualTo(secondBefore - 7f));
        Assert.That(first.MoneyAccount.Revision, Is.EqualTo(firstRevision + 1));
        Assert.That(second.MoneyAccount.Revision, Is.EqualTo(secondRevision + 1));
    }

    [Test]
    public void InsufficientLaterMemberIsRejectedBeforeAnyAccountWrite()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        DailyV1NpcRuntimeOwners first = graph.Npcs[0];
        DailyV1NpcRuntimeOwners second = graph.Npcs[1];
        if (second.MoneyAccount.Balance > 0f)
            Assert.That(second.MoneyAccount.TryDebit(second.MoneyAccount.Balance), Is.True);
        Assert.That(first.MoneyAccount.TryCredit(8f), Is.True);
        float firstBefore = first.MoneyAccount.Balance;
        long firstRevision = first.MoneyAccount.Revision;
        long secondRevision = second.MoneyAccount.Revision;

        EconomyTransactionResult result = DailyV1SelectedTravelPartyChargeExecution.TryCharge(
            graph, new[] { first, second }, 5f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InsufficientFunds));
        Assert.That(result.ActorRuntimeId, Is.EqualTo(second.RuntimeId));
        Assert.That(result.ParticipantRuntimeIds, Is.EqualTo(new[] { first.RuntimeId, second.RuntimeId }));
        Assert.That(first.MoneyAccount.Balance, Is.EqualTo(firstBefore));
        Assert.That(first.MoneyAccount.Revision, Is.EqualTo(firstRevision));
        Assert.That(second.MoneyAccount.Revision, Is.EqualTo(secondRevision));
    }

    [Test]
    public void LaterP12DebitRejectionCompensatesEarlierSelectedAccount()
    {
        CreateMembers(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
        float firstBefore = first.MoneyAccount.Balance;
        float secondBefore = second.MoneyAccount.Balance;
        long firstRevision = first.MoneyAccount.Revision;
        long secondRevision = second.MoneyAccount.Revision;
        second.MoneyAccount.BindP12MutationBoundary(() => false, () => { });

        EconomyTransactionResult result = DailyV1SelectedTravelPartyChargeExecution.TryCharge(
            graph, new[] { first, second }, 7f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason,
            Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(first.MoneyAccount.Balance, Is.EqualTo(firstBefore));
        Assert.That(second.MoneyAccount.Balance, Is.EqualTo(secondBefore));
        Assert.That(first.MoneyAccount.Revision, Is.EqualTo(firstRevision + 2),
            "The committed debit and compensating credit both remain observable P12 writes.");
        Assert.That(second.MoneyAccount.Revision, Is.EqualTo(secondRevision));
    }

    [Test]
    public void ForeignOrRepeatedCoreOwnersAreRejectedBeforeMutation()
    {
        CreateMembers(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
        DailyV1SelectedRuntimeOwnerGraph foreignGraph = CreateGraph();
        DailyV1NpcRuntimeOwners foreign = foreignGraph.Npcs[0];
        float firstBefore = first.MoneyAccount.Balance;

        EconomyTransactionResult duplicate = DailyV1SelectedTravelPartyChargeExecution.TryCharge(
            graph, new[] { first, first }, 1f);
        EconomyTransactionResult foreignResult = DailyV1SelectedTravelPartyChargeExecution.TryCharge(
            graph, new[] { first, foreign }, 1f);

        Assert.That(duplicate.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InvalidInput));
        Assert.That(foreignResult.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InvalidInput));
        Assert.That(first.MoneyAccount.Balance, Is.EqualTo(firstBefore));
        Assert.That(second.MoneyAccount.Balance, Is.GreaterThanOrEqualTo(0f));
    }

    private static void CreateMembers(
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners first,
        out DailyV1NpcRuntimeOwners second)
    {
        graph = CreateGraph();
        first = graph.Npcs[0];
        second = graph.Npcs[1];
        Assert.That(first.MoneyAccount.TryCredit(20f), Is.True);
        Assert.That(second.MoneyAccount.TryCredit(20f), Is.True);
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);
        return composition.OwnerGraph;
    }
}
