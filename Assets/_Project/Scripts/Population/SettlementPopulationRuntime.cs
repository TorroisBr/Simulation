using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
    private Func<SettlementPopulationRuntime, bool, bool, bool> p12PopulationMutationAdmission;
    private Action<SettlementPopulationRuntime, bool, bool> p12PopulationMutationCommitted;
    private Dictionary<string, PopulationOperationReceipt> operationReceipts =
        new Dictionary<string, PopulationOperationReceipt>(StringComparer.Ordinal);

    public string SettlementRuntimeId => settlementRuntimeId;
    public int CurrentPopulation => currentPopulation;
    public long Revision => revision;
    internal bool CanMutate => mutationGuardBinding.CanMutate;
    internal AuthoritativeMutationGuard BoundMutationGuard => mutationGuardBinding.BoundGuard;
    internal bool IsP12PopulationBound => p12PopulationMutationAdmission != null;
    internal bool CanAdvanceP12AggregateRevision =>
        p12PopulationMutationAdmission == null || revision < long.MaxValue;

    internal void BindP12PopulationMutationBoundary(
        Func<SettlementPopulationRuntime, bool, bool, bool> admission,
        Action<SettlementPopulationRuntime, bool, bool> committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12PopulationMutationAdmission != null || p12PopulationMutationCommitted != null)
            throw new InvalidOperationException("SettlementPopulationRuntime is already bound to a P12 population boundary.");
        p12PopulationMutationAdmission = admission;
        p12PopulationMutationCommitted = committed;
    }

    private bool CanCommitP12PopulationMutation(bool aggregateChanged, bool receiptChanged)
    {
        if (p12PopulationMutationAdmission == null) return true;
        if (aggregateChanged && revision == long.MaxValue) return false;
        try { return p12PopulationMutationAdmission(this, aggregateChanged, receiptChanged); }
        catch { return false; }
    }

    private void NotifyP12PopulationMutationCommitted(bool aggregateChanged, bool receiptChanged)
    {
        if (p12PopulationMutationCommitted == null) return;
        try { p12PopulationMutationCommitted(this, aggregateChanged, receiptChanged); }
        catch { }
    }

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

    internal bool TryCaptureOperationReceiptSnapshot(
        out IReadOnlyList<PopulationOperationReceiptSnapshot> receipts,
        out long receiptRevision)
    {
        receipts = null;
        lock (operationReceiptGate)
        {
            receiptRevision = operationReceiptRevision;
            if (operationReceipts == null || operationReceiptRevision < 0L)
            {
                return false;
            }

            List<string> identities = new List<string>(operationReceipts.Keys);
            identities.Sort(StringComparer.Ordinal);
            List<PopulationOperationReceiptSnapshot> copiedReceipts =
                new List<PopulationOperationReceiptSnapshot>(identities.Count);
            foreach (string identity in identities)
            {
                if (string.IsNullOrWhiteSpace(identity)
                    || !operationReceipts.TryGetValue(identity, out PopulationOperationReceipt receipt)
                    || receipt == null
                    || string.IsNullOrWhiteSpace(receipt.Fingerprint)
                    || receipt.Transition == null)
                {
                    return false;
                }

                copiedReceipts.Add(new PopulationOperationReceiptSnapshot(
                    identity,
                    receipt.Fingerprint,
                    receipt.Transition));
            }

            receipts = new ReadOnlyCollection<PopulationOperationReceiptSnapshot>(copiedReceipts);
            return true;
        }
    }

    internal static bool TryCreateFromOwnerSnapshot(
        string snapshotSettlementRuntimeId,
        int snapshotPopulation,
        long snapshotRevision,
        IReadOnlyList<PopulationOperationReceiptSnapshot> snapshotReceipts,
        long snapshotReceiptRevision,
        out SettlementPopulationRuntime population)
    {
        population = null;
        if (string.IsNullOrWhiteSpace(snapshotSettlementRuntimeId)
            || snapshotPopulation < 0
            || snapshotRevision < 0L
            || snapshotReceipts == null
            || snapshotReceiptRevision < 0L
            || snapshotReceiptRevision < snapshotReceipts.Count)
        {
            return false;
        }

        SettlementPopulationRuntime staged = new SettlementPopulationRuntime(
            snapshotSettlementRuntimeId,
            snapshotPopulation)
        {
            revision = snapshotRevision,
            operationReceiptRevision = snapshotReceiptRevision,
            operationReceipts = new Dictionary<string, PopulationOperationReceipt>(StringComparer.Ordinal)
        };

        foreach (PopulationOperationReceiptSnapshot snapshotReceipt in snapshotReceipts)
        {
            if (snapshotReceipt == null
                || string.IsNullOrWhiteSpace(snapshotReceipt.Identity)
                || string.IsNullOrWhiteSpace(snapshotReceipt.Fingerprint)
                || snapshotReceipt.Transition == null
                || !IsValidSnapshotTransition(
                    snapshotSettlementRuntimeId,
                    snapshotRevision,
                    snapshotReceipt.Transition)
                || staged.operationReceipts.ContainsKey(snapshotReceipt.Identity))
            {
                return false;
            }

            staged.operationReceipts.Add(
                snapshotReceipt.Identity,
                new PopulationOperationReceipt(
                    snapshotReceipt.Fingerprint,
                    CopyTransitionValue(snapshotReceipt.Transition)));
        }

        population = staged;
        return true;
    }

    internal static SettlementPopulationTransition CopyTransitionValue(
        SettlementPopulationTransition transition)
    {
        if (transition == null) return null;
        return new SettlementPopulationTransition(
            transition.SettlementRuntimeId,
            transition.ExpectedRevision,
            transition.PopulationBefore,
            new PopulationChangeSet(
                transition.Births,
                transition.Deaths,
                transition.Immigrations,
                transition.Emigrations),
            transition.NetChange,
            transition.PopulationAfter);
    }

    private static bool IsValidSnapshotTransition(
        string settlementRuntimeId,
        long ownerRevision,
        SettlementPopulationTransition transition)
    {
        if (transition == null
            || !string.Equals(transition.SettlementRuntimeId, settlementRuntimeId, StringComparison.Ordinal)
            || transition.ExpectedRevision < 0L
            || transition.ExpectedRevision >= ownerRevision
            || transition.PopulationBefore < 0
            || transition.PopulationAfter < 0
            || transition.Births < 0
            || transition.Deaths < 0
            || transition.Immigrations < 0
            || transition.Emigrations < 0)
        {
            return false;
        }

        long expectedNetChange = (long)transition.Births
            + transition.Immigrations
            - transition.Deaths
            - transition.Emigrations;
        return transition.NetChange == expectedNetChange
            && (long)transition.PopulationBefore + expectedNetChange == transition.PopulationAfter;
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

        if (!CanCommitP12PopulationMutation(true, true))
        {
            failure = PopulationTransitionFailure.RuntimeFaulted;
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
            NotifyP12PopulationMutationCommitted(true, true);
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

        if (!CanCommitP12PopulationMutation(true, false))
        {
            failure = PopulationTransitionFailure.RuntimeFaulted;
            return false;
        }

        currentPopulation = transition.PopulationAfter;
        revision++;
        NotifyP12PopulationMutationCommitted(true, false);
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

        bool hasP12Boundary = origin.p12PopulationMutationAdmission != null
            || destination.p12PopulationMutationAdmission != null;
        if (hasP12Boundary
            && (origin.p12PopulationMutationAdmission == null
                || destination.p12PopulationMutationAdmission == null
                || !origin.CanCommitP12PopulationMutation(true, false)
                || !destination.CanCommitP12PopulationMutation(true, false)))
        {
            failure = PopulationTransitionFailure.RuntimeFaulted;
            return false;
        }

        // This boundary computes the only valid migration delta and commits both sides
        // only after every state, limit, and revision check has passed.
        origin.currentPopulation = originPopulationBefore - 1;
        origin.revision++;
        destination.currentPopulation = destinationPopulationBefore + 1;
        destination.revision++;
        origin.NotifyP12PopulationMutationCommitted(true, false);
        destination.NotifyP12PopulationMutationCommitted(true, false);
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

/// <summary>Detached immutable value for one retained population operation receipt.</summary>
internal sealed class PopulationOperationReceiptSnapshot
{
    internal string Identity { get; }
    internal string Fingerprint { get; }
    internal SettlementPopulationTransition Transition { get; }

    internal PopulationOperationReceiptSnapshot(
        string identity,
        string fingerprint,
        SettlementPopulationTransition transition)
    {
        Identity = identity;
        Fingerprint = fingerprint;
        Transition = SettlementPopulationRuntime.CopyTransitionValue(transition);
    }
}

internal enum PopulationOperationReceiptResolution
{
    Missing = 0,
    Matching = 1,
    Conflicting = 2
}
