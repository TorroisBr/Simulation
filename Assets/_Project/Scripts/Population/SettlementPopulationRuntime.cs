using System;
using System.Collections.Generic;

/// <summary>
/// Mutable aggregate population state owned by a future settlement runtime.
/// This ledger intentionally has no independent runtime identity and no named-NPC awareness.
/// </summary>
public sealed class SettlementPopulationRuntime : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly object operationReceiptGate = new object();
    private readonly string settlementRuntimeId;
    private int currentPopulation;
    private long revision;
    private long operationReceiptRevision;
    private Dictionary<string, PopulationOperationReceipt> operationReceipts =
        new Dictionary<string, PopulationOperationReceipt>(StringComparer.Ordinal);

    public string SettlementRuntimeId => settlementRuntimeId;
    public int CurrentPopulation => currentPopulation;
    public long Revision => revision;
    internal bool CanMutate => mutationGuardBinding.CanMutate;
    internal AuthoritativeMutationGuard BoundMutationGuard => mutationGuardBinding.BoundGuard;

    internal void RestoreSnapshot(int population, long expectedRevision)
    {
        if (population < 0 || expectedRevision < 0L)
        {
            return;
        }

        lock (operationReceiptGate)
        {
            List<string> discardedReceiptIds = new List<string>();
            foreach (KeyValuePair<string, PopulationOperationReceipt> pair in operationReceipts)
            {
                if (pair.Value.Transition.ExpectedRevision >= expectedRevision)
                {
                    discardedReceiptIds.Add(pair.Key);
                }
            }

            if (discardedReceiptIds.Count > 0 && operationReceiptRevision < long.MaxValue)
            {
                operationReceiptRevision++;
            }

            foreach (string operationIdentity in discardedReceiptIds)
            {
                operationReceipts.Remove(operationIdentity);
            }

            currentPopulation = population;
            revision = expectedRevision;
        }
    }

    internal void GetOperationReceiptCensus(out int cardinality, out long receiptRevision)
    {
        lock (operationReceiptGate)
        {
            cardinality = operationReceipts.Count;
            receiptRevision = operationReceiptRevision;
        }
    }

    public SettlementPopulationRuntime(string settlementRuntimeId, int currentPopulation)
    {
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            throw new ArgumentException(
                "SettlementPopulationRuntime requires a non-empty settlement RuntimeId.",
                nameof(settlementRuntimeId));
        }

        if (currentPopulation < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentPopulation),
                "Settlement population cannot be negative.");
        }

        this.settlementRuntimeId = settlementRuntimeId;
        this.currentPopulation = currentPopulation;
        revision = 0L;
    }

    internal bool TryApplyTransition(
        SettlementPopulationTransition transition,
        out PopulationTransitionFailure failure)
    {
        return TryApplyTransitionCore(transition, out failure);
    }

    internal bool TryApplyTransitionWithReceipt(
        string operationIdentity,
        string operationFingerprint,
        SettlementPopulationTransition transition,
        out bool newlyApplied,
        out PopulationTransitionFailure failure)
    {
        newlyApplied = false;
        failure = PopulationTransitionFailure.None;
        if (string.IsNullOrWhiteSpace(operationIdentity)
            || string.IsNullOrWhiteSpace(operationFingerprint)
            || transition == null)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        lock (operationReceiptGate)
        {
            PopulationOperationReceiptResolution resolution = ResolveOperationReceiptCore(
                operationIdentity, operationFingerprint, transition);
            if (resolution == PopulationOperationReceiptResolution.Matching)
            {
                return true;
            }

            if (resolution == PopulationOperationReceiptResolution.Conflicting)
            {
                failure = PopulationTransitionFailure.OperationIdentityConflict;
                return false;
            }

            if (!CanApplyTransition(transition, out failure))
            {
                return false;
            }

            if (operationReceiptRevision == long.MaxValue)
            {
                failure = PopulationTransitionFailure.RevisionOverflow;
                return false;
            }

            operationReceipts.Add(operationIdentity, new PopulationOperationReceipt(
                operationFingerprint, transition));
            currentPopulation = transition.PopulationAfter;
            revision++;
            operationReceiptRevision++;
            newlyApplied = true;
            return true;
        }
    }

    internal PopulationOperationReceiptResolution ResolveOperationReceipt(
        string operationIdentity,
        string operationFingerprint,
        SettlementPopulationTransition transition = null)
    {
        lock (operationReceiptGate)
        {
            return ResolveOperationReceiptCore(operationIdentity, operationFingerprint, transition);
        }
    }

    private PopulationOperationReceiptResolution ResolveOperationReceiptCore(
        string operationIdentity,
        string operationFingerprint,
        SettlementPopulationTransition transition)
    {
        if (string.IsNullOrWhiteSpace(operationIdentity)
            || !operationReceipts.TryGetValue(operationIdentity, out PopulationOperationReceipt receipt))
        {
            return PopulationOperationReceiptResolution.Missing;
        }

        return string.Equals(receipt.Fingerprint, operationFingerprint, StringComparison.Ordinal)
            && (transition == null || receipt.Transition.Equals(transition))
            ? PopulationOperationReceiptResolution.Matching
            : PopulationOperationReceiptResolution.Conflicting;
    }

    private bool TryApplyTransitionCore(
        SettlementPopulationTransition transition,
        out PopulationTransitionFailure failure)
    {
        if (!CanApplyTransition(transition, out failure))
        {
            return false;
        }

        currentPopulation = transition.PopulationAfter;
        revision++;
        return true;
    }

    private bool CanApplyTransition(
        SettlementPopulationTransition transition,
        out PopulationTransitionFailure failure)
    {
        failure = PopulationTransitionFailure.None;
        if (!mutationGuardBinding.CanMutate)
        {
            failure = PopulationTransitionFailure.RuntimeFaulted;
            return false;
        }

        if (transition == null)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        if (!string.Equals(SettlementRuntimeId, transition.SettlementRuntimeId, StringComparison.Ordinal))
        {
            failure = PopulationTransitionFailure.InvalidSettlement;
            return false;
        }

        if (transition.PopulationBefore < 0 || transition.PopulationAfter < 0)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        long expectedNetChange = (long)transition.Births
            + transition.Immigrations
            - transition.Deaths
            - transition.Emigrations;
        if (transition.Births < 0 || transition.Deaths < 0
            || transition.Immigrations < 0 || transition.Emigrations < 0
            || transition.NetChange != expectedNetChange
            || (long)transition.PopulationBefore + transition.NetChange != transition.PopulationAfter)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        if (revision != transition.ExpectedRevision || currentPopulation != transition.PopulationBefore)
        {
            failure = PopulationTransitionFailure.StaleState;
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PopulationTransitionFailure.RevisionOverflow;
            return false;
        }

        return true;
    }

    internal static bool TryApplyPairedMigration(
        SettlementPopulationRuntime origin,
        SettlementPopulationRuntime destination,
        long originExpectedRevision,
        long destinationExpectedRevision,
        int originPopulationBefore,
        int destinationPopulationBefore,
        out PopulationTransitionFailure failure)
    {
        failure = PopulationTransitionFailure.None;

        if (!originCanApply(origin, destination, out failure))
        {
            return false;
        }

        if (origin == null || destination == null
            || string.IsNullOrWhiteSpace(origin.SettlementRuntimeId) == true
            || string.IsNullOrWhiteSpace(destination.SettlementRuntimeId) == true
            || ReferenceEquals(origin, destination)
            || string.Equals(origin.SettlementRuntimeId, destination.SettlementRuntimeId, StringComparison.Ordinal))
        {
            failure = PopulationTransitionFailure.InvalidSettlement;
            return false;
        }

        if (originExpectedRevision < 0L
            || destinationExpectedRevision < 0L
            || originPopulationBefore < 0
            || destinationPopulationBefore < 0)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        if (origin.Revision != originExpectedRevision
            || origin.CurrentPopulation != originPopulationBefore)
        {
            failure = PopulationTransitionFailure.StaleState;
            return false;
        }

        if (destination.Revision != destinationExpectedRevision
            || destination.CurrentPopulation != destinationPopulationBefore)
        {
            failure = PopulationTransitionFailure.StaleState;
            return false;
        }

        if (originPopulationBefore == 0)
        {
            failure = PopulationTransitionFailure.WouldUnderflow;
            return false;
        }

        if (destinationPopulationBefore == int.MaxValue)
        {
            failure = PopulationTransitionFailure.WouldOverflow;
            return false;
        }

        if (origin.Revision == long.MaxValue || destination.Revision == long.MaxValue)
        {
            failure = PopulationTransitionFailure.RevisionOverflow;
            return false;
        }

        // This boundary computes the only valid migration delta and commits both sides
        // only after every state, limit, and revision check has passed.
        origin.currentPopulation = originPopulationBefore - 1;
        origin.revision++;
        destination.currentPopulation = destinationPopulationBefore + 1;
        destination.revision++;
        return true;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    private static bool originCanApply(
        SettlementPopulationRuntime origin,
        SettlementPopulationRuntime destination,
        out PopulationTransitionFailure failure)
    {
        if (origin == null || destination == null)
        {
            failure = PopulationTransitionFailure.None;
            return true;
        }

        if (!origin.mutationGuardBinding.CanMutate || !destination.mutationGuardBinding.CanMutate)
        {
            failure = PopulationTransitionFailure.RuntimeFaulted;
            return false;
        }

        if (!ReferenceEquals(
                origin.mutationGuardBinding.BoundGuard,
                destination.mutationGuardBinding.BoundGuard))
        {
            failure = PopulationTransitionFailure.RuntimeOwnershipMismatch;
            return false;
        }

        failure = PopulationTransitionFailure.None;
        return true;
    }

    private sealed class PopulationOperationReceipt
    {
        public readonly string Fingerprint;
        public readonly SettlementPopulationTransition Transition;

        public PopulationOperationReceipt(string fingerprint, SettlementPopulationTransition transition)
        {
            Fingerprint = fingerprint;
            Transition = transition;
        }
    }
}

internal enum PopulationOperationReceiptResolution
{
    Missing = 0,
    Matching = 1,
    Conflicting = 2
}
