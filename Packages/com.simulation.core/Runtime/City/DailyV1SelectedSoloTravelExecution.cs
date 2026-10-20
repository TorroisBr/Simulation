using System;

internal enum DailyV1SelectedSoloTravelFailure
{
    None,
    InvalidInput,
    InsufficientFunds,
    ChargeRejected,
    TransitionRejected,
    RefundRejected
}

internal sealed class DailyV1SelectedSoloTravelResult
{
    internal bool Succeeded { get; }
    internal DailyV1SelectedSoloTravelFailure Failure { get; }
    internal bool RefundSucceeded { get; }

    internal DailyV1SelectedSoloTravelResult(
        bool succeeded, DailyV1SelectedSoloTravelFailure failure, bool refundSucceeded = false)
    {
        Succeeded = succeeded;
        Failure = failure;
        RefundSucceeded = refundSucceeded;
    }
}

/// <summary>Runs the selected solo travel charge-before-transition transaction.
/// Account and transition owners retain their existing P12 bindings; callbacks
/// only enter the already-prepared Core transition at its admission boundary.</summary>
internal static class DailyV1SelectedSoloTravelExecution
{
    internal static DailyV1SelectedSoloTravelResult TryExecute(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan preparedTransition,
        float travelCost,
        Func<bool> commitTransition)
    {
        if (npc?.MoneyAccount == null || preparedTransition == null || commitTransition == null
            || !ReferenceEquals(preparedTransition.Npc, npc)
            || float.IsNaN(travelCost) || float.IsInfinity(travelCost) || travelCost < 0f)
            return new DailyV1SelectedSoloTravelResult(false, DailyV1SelectedSoloTravelFailure.InvalidInput);

        if (!npc.MoneyAccount.CanDebit(travelCost))
            return new DailyV1SelectedSoloTravelResult(false, DailyV1SelectedSoloTravelFailure.InsufficientFunds);
        if (!npc.MoneyAccount.TryDebit(travelCost))
            return new DailyV1SelectedSoloTravelResult(false, DailyV1SelectedSoloTravelFailure.ChargeRejected);

        if (commitTransition())
            return new DailyV1SelectedSoloTravelResult(true, DailyV1SelectedSoloTravelFailure.None);

        bool refunded = npc.MoneyAccount.TryCredit(travelCost);
        return new DailyV1SelectedSoloTravelResult(false,
            refunded ? DailyV1SelectedSoloTravelFailure.TransitionRejected : DailyV1SelectedSoloTravelFailure.RefundRejected,
            refunded);
    }
}
