using System;
using System.Collections.Generic;

/// <summary>
/// Applies the selected TravelParty's shared account charge through the exact
/// MoneyAccount owners in the admitted Core runtime graph. The Unity host keeps
/// only the legacy fallback for rosters that are not wholly Core-selected.
/// </summary>
internal static class DailyV1SelectedTravelPartyChargeExecution
{
    internal static EconomyTransactionResult TryCharge(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        float amountPerMember)
    {
        EconomyTransactionType transactionType = EconomyTransactionType.TravelCharge;
        MoneyEffect moneyEffect = MoneyEffect.ExplicitSink;
        List<string> participantIds = new List<string>();
        HashSet<DailyV1NpcRuntimeOwners> uniqueOwners = new HashSet<DailyV1NpcRuntimeOwners>();
        HashSet<string> uniqueRuntimeIds = new HashSet<string>(StringComparer.Ordinal);

        if (graph == null || members == null || members.Count == 0
            || !IsValidNonNegativeFiniteAmount(amountPerMember))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType, moneyEffect, EconomyTransactionFailureReason.InvalidInput,
                participantRuntimeIds: participantIds);
        }

        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            if (member == null || !graph.ContainsExactNpcOwner(member)
                || member.MoneyAccount == null
                || !uniqueOwners.Add(member)
                || string.IsNullOrWhiteSpace(member.RuntimeId)
                || !uniqueRuntimeIds.Add(member.RuntimeId))
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType, moneyEffect, EconomyTransactionFailureReason.InvalidInput,
                    participantRuntimeIds: participantIds);
            }

            participantIds.Add(member.RuntimeId);
            if (!member.MoneyAccount.CanDebit(amountPerMember))
            {
                return EconomyTransactionResult.CreateFailure(
                    transactionType, moneyEffect, EconomyTransactionFailureReason.InsufficientFunds,
                    actorRuntimeId: member.RuntimeId,
                    participantRuntimeIds: participantIds);
            }
        }

        float totalAmount = amountPerMember * members.Count;
        if (!IsValidNonNegativeFiniteAmount(totalAmount))
        {
            return EconomyTransactionResult.CreateFailure(
                transactionType, moneyEffect, EconomyTransactionFailureReason.InvalidInput,
                participantRuntimeIds: participantIds);
        }

        int chargedCount = 0;
        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            if (!member.MoneyAccount.TryDebit(amountPerMember))
            {
                for (int index = 0; index < chargedCount; index++)
                    members[index].MoneyAccount.TryCredit(amountPerMember);

                return EconomyTransactionResult.CreateFailure(
                    transactionType, moneyEffect, EconomyTransactionFailureReason.TransactionCommitFailed,
                    participantRuntimeIds: participantIds);
            }

            chargedCount++;
        }

        return EconomyTransactionResult.CreateSuccess(
            transactionType, moneyEffect, totalAmount,
            totalPrice: totalAmount,
            participantRuntimeIds: participantIds);
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount) =>
        amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);
}
