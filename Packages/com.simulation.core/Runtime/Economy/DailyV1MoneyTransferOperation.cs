using System;

/// <summary>
/// Shared authoritative account-transfer behavior. Admission and P12 owner
/// operation scopes remain with the caller; this operation changes only the
/// exact shared MoneyAccountRuntime owners it receives.
/// </summary>
internal static class DailyV1MoneyTransferOperation
{
    internal static EconomyTransactionResult TryExecute(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount,
        string sourceRuntimeId,
        string destinationRuntimeId)
    {
        EconomyTransactionResult preflightFailure = Validate(
            source, destination, amount, sourceRuntimeId, destinationRuntimeId);
        if (preflightFailure != null) return preflightFailure;

        if (!TryCommit(source, destination, amount))
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.MoneyTransfer,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.TransactionCommitFailed,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        return EconomyTransactionResult.CreateSuccess(
            EconomyTransactionType.MoneyTransfer,
            MoneyEffect.Transfer,
            amount,
            totalPrice: amount,
            sourceRuntimeId: sourceRuntimeId,
            destinationRuntimeId: destinationRuntimeId);
    }

    internal static EconomyTransactionResult Validate(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount,
        string sourceRuntimeId,
        string destinationRuntimeId)
    {
        if (source == null || destination == null || !IsValidNonNegativeFiniteAmount(amount))
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.MoneyTransfer,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.InvalidInput,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        if (ReferenceEquals(source, destination))
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.MoneyTransfer,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.SameAccount,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        if (!source.CanDebit(amount))
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.MoneyTransfer,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.InsufficientFunds,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        if (!destination.CanCredit(amount))
        {
            return EconomyTransactionResult.CreateFailure(
                EconomyTransactionType.MoneyTransfer,
                MoneyEffect.Transfer,
                EconomyTransactionFailureReason.CreditRejected,
                sourceRuntimeId,
                destinationRuntimeId);
        }

        return null;
    }

    internal static bool TryCommit(
        MoneyAccountRuntime source,
        MoneyAccountRuntime destination,
        float amount)
    {
        if (source == null || destination == null || ReferenceEquals(source, destination)
            || !IsValidNonNegativeFiniteAmount(amount)
            || !source.CanDebit(amount) || !destination.CanCredit(amount))
            return false;

        if (!source.TryDebit(amount)) return false;
        if (destination.TryCredit(amount)) return true;

        source.TryCredit(amount);
        return false;
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount) =>
        amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);
}
