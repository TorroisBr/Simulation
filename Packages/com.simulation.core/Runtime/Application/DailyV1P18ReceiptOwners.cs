using System;
using System.Collections.Generic;

internal sealed class DailyV1CrimeP18ReceiptMutation
{
    internal P12AReceiptValue Receipt { get; }
    internal bool IsReplay { get; }
    internal P12ACrimeReceiptOwnerSnapshot Expected { get; }
    internal P12ACrimeReceiptOwnerSnapshot Next { get; }

    internal DailyV1CrimeP18ReceiptMutation(P12AReceiptValue receipt, bool isReplay,
        P12ACrimeReceiptOwnerSnapshot expected, P12ACrimeReceiptOwnerSnapshot next)
    {
        Receipt = receipt;
        IsReplay = isReplay;
        Expected = expected;
        Next = next;
    }
}

/// <summary>
/// Core-owned receipt ledger for the selected Crime P18 hidden-status step.
/// Unity may supply the legacy runtime mutation, but receipt identity,
/// idempotency, revision and P12 snapshot state live here.
/// </summary>
internal sealed class DailyV1CrimeP18ReceiptOwner
{
    private P12ACrimeReceiptOwnerSnapshot state =
        new P12ACrimeReceiptOwnerSnapshot(0L, Array.Empty<P12AReceiptValue>());
    private bool restored;

    internal long StepRevision => state.StepRevision;
    internal long CensusRevision => state.CensusRevision;
    internal int CensusCardinality => state != null && state.IsValid
        ? 1
        : -1;
    internal bool IsExactEmpty => state != null && state.IsValid
        && state.StepRevision == 0L && state.Receipts.Count == 0;
    internal bool WasRestored => restored;

    internal bool TryResolve(string identity, out P12AReceiptValue receipt)
    {
        receipt = null;
        if (state == null || !state.IsValid || string.IsNullOrWhiteSpace(identity)) return false;
        foreach (P12AReceiptValue candidate in state.Receipts)
        {
            if (!string.Equals(candidate.Identity, identity, StringComparison.Ordinal)) continue;
            receipt = candidate;
            return true;
        }
        return false;
    }

    internal bool TryPrepare(string identity, string descriptorFingerprint,
        out DailyV1CrimeP18ReceiptMutation mutation)
    {
        mutation = null;
        if (state == null || !state.IsValid || string.IsNullOrWhiteSpace(identity)
            || string.IsNullOrWhiteSpace(descriptorFingerprint)) return false;

        if (TryResolve(identity, out P12AReceiptValue existing))
        {
            if (!string.Equals(existing.DescriptorFingerprint, descriptorFingerprint, StringComparison.Ordinal))
                return false;
            mutation = new DailyV1CrimeP18ReceiptMutation(existing, true, state, state);
            return true;
        }

        if (state.StepRevision == long.MaxValue) return false;
        P12AReceiptValue nextReceipt = new P12AReceiptValue(identity, descriptorFingerprint,
            state.StepRevision, state.StepRevision + 1L);
        List<P12AReceiptValue> receipts = new List<P12AReceiptValue>(state.Receipts) { nextReceipt };
        receipts.Sort((left, right) => StringComparer.Ordinal.Compare(left.Identity, right.Identity));
        P12ACrimeReceiptOwnerSnapshot next =
            new P12ACrimeReceiptOwnerSnapshot(state.StepRevision + 1L, receipts);
        if (!next.IsValid) return false;
        mutation = new DailyV1CrimeP18ReceiptMutation(nextReceipt, false, state, next);
        return true;
    }

    internal bool CanCommit(DailyV1CrimeP18ReceiptMutation mutation)
    {
        if (mutation == null || mutation.Receipt == null || !ReferenceEquals(mutation.Expected, state)
            || state == null || !state.IsValid) return false;
        if (mutation.IsReplay)
            return ReferenceEquals(mutation.Next, state)
                && TryResolve(mutation.Receipt.Identity, out P12AReceiptValue existing)
                && SameReceipt(existing, mutation.Receipt);
        return mutation.Next != null && mutation.Next.IsValid
            && mutation.Next.StepRevision == state.StepRevision + 1L
            && HasSingleAppend(state.Receipts, mutation.Next.Receipts, mutation.Receipt);
    }

    internal void InstallPrepared(DailyV1CrimeP18ReceiptMutation mutation)
    {
        state = mutation.Next;
    }

    internal bool TryCaptureP12AReceiptOwnerSnapshot(out P12ACrimeReceiptOwnerSnapshot snapshot)
    {
        snapshot = null;
        if (state == null || !state.IsValid || state.CensusRevision != CensusRevision) return false;
        snapshot = state;
        return true;
    }

    internal bool TryInstallP12AReceiptOwnerSnapshot(P12ACrimeReceiptOwnerSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.IsValid || restored || !IsExactEmpty) return false;
        state = snapshot;
        restored = true;
        return state.CensusRevision == snapshot.CensusRevision;
    }

    private static bool HasSingleAppend(IReadOnlyList<P12AReceiptValue> before,
        IReadOnlyList<P12AReceiptValue> after, P12AReceiptValue appended)
    {
        if (before == null || after == null || appended == null || after.Count != before.Count + 1) return false;
        int matched = 0;
        foreach (P12AReceiptValue value in before)
        {
            bool found = false;
            foreach (P12AReceiptValue candidate in after)
            {
                if (!string.Equals(value.Identity, candidate.Identity, StringComparison.Ordinal)) continue;
                if (!SameReceipt(value, candidate)) return false;
                found = true;
                matched++;
                break;
            }
            if (!found) return false;
        }
        if (matched != before.Count) return false;
        foreach (P12AReceiptValue candidate in after)
            if (string.Equals(candidate.Identity, appended.Identity, StringComparison.Ordinal))
                return SameReceipt(candidate, appended)
                    && !Contains(before, appended.Identity);
        return false;
    }

    private static bool Contains(IReadOnlyList<P12AReceiptValue> values, string identity)
    {
        foreach (P12AReceiptValue value in values)
            if (string.Equals(value.Identity, identity, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static bool SameReceipt(P12AReceiptValue left, P12AReceiptValue right) =>
        left != null && right != null
        && string.Equals(left.Identity, right.Identity, StringComparison.Ordinal)
        && string.Equals(left.DescriptorFingerprint, right.DescriptorFingerprint, StringComparison.Ordinal)
        && left.RevisionBefore == right.RevisionBefore
        && left.RevisionAfter == right.RevisionAfter;
}

internal enum DailyV1JusticeP18ReceiptKind
{
    BeginDay,
    AdvanceSentences,
    SyncWantedStatuses
}

internal sealed class DailyV1JusticeP18ReceiptMutation
{
    internal DailyV1JusticeP18ReceiptKind Kind { get; }
    internal P12AReceiptValue Receipt { get; }
    internal bool IsReplay { get; }
    internal P12AJusticeReceiptOwnerSnapshot Expected { get; }
    internal P12AJusticeReceiptOwnerSnapshot Next { get; }

    internal DailyV1JusticeP18ReceiptMutation(DailyV1JusticeP18ReceiptKind kind,
        P12AReceiptValue receipt, bool isReplay,
        P12AJusticeReceiptOwnerSnapshot expected, P12AJusticeReceiptOwnerSnapshot next)
    {
        Kind = kind;
        Receipt = receipt;
        IsReplay = isReplay;
        Expected = expected;
        Next = next;
    }
}

/// <summary>
/// Core-owned receipt ledger for the three selected Justice P18 boundary
/// steps. The existing P12 codec already serializes these snapshots.
/// </summary>
internal sealed class DailyV1JusticeP18ReceiptOwner
{
    private P12AJusticeReceiptOwnerSnapshot state = Empty();
    private bool restored;

    internal long BeginDayRevision => state.BeginDayRevision;
    internal long AdvanceSentencesRevision => state.AdvanceSentencesRevision;
    internal long SyncWantedStatusesRevision => state.SyncWantedStatusesRevision;
    internal long CensusRevision => state.CensusRevision;
    internal int CensusCardinality => state != null && state.IsValid
        ? 1
        : -1;
    internal bool IsExactEmpty => state != null && state.IsValid
        && state.BeginDayRevision == 0L && state.BeginDayReceipts.Count == 0
        && state.AdvanceSentencesRevision == 0L && state.AdvanceSentencesReceipts.Count == 0
        && state.SyncWantedStatusesRevision == 0L && state.SyncWantedStatusesReceipts.Count == 0;
    internal bool WasRestored => restored;

    internal bool TryResolve(DailyV1JusticeP18ReceiptKind kind, string identity,
        out P12AReceiptValue receipt)
    {
        receipt = null;
        if (state == null || !state.IsValid
            || !Enum.IsDefined(typeof(DailyV1JusticeP18ReceiptKind), kind)
            || string.IsNullOrWhiteSpace(identity)) return false;
        foreach (P12AReceiptValue candidate in ReceiptsFor(kind, state))
        {
            if (!string.Equals(candidate.Identity, identity, StringComparison.Ordinal)) continue;
            receipt = candidate;
            return true;
        }
        return false;
    }

    internal bool TryPrepare(DailyV1JusticeP18ReceiptKind kind, string identity,
        string descriptorFingerprint, out DailyV1JusticeP18ReceiptMutation mutation)
    {
        mutation = null;
        if (state == null || !state.IsValid || !Enum.IsDefined(typeof(DailyV1JusticeP18ReceiptKind), kind)
            || string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(descriptorFingerprint))
            return false;

        IReadOnlyList<P12AReceiptValue> current = ReceiptsFor(kind, state);
        foreach (P12AReceiptValue existing in current)
        {
            if (!string.Equals(existing.Identity, identity, StringComparison.Ordinal)) continue;
            if (!string.Equals(existing.DescriptorFingerprint, descriptorFingerprint, StringComparison.Ordinal))
                return false;
            mutation = new DailyV1JusticeP18ReceiptMutation(kind, existing, true, state, state);
            return true;
        }

        long revision = RevisionFor(kind, state);
        if (revision == long.MaxValue) return false;
        P12AReceiptValue nextReceipt = new P12AReceiptValue(identity, descriptorFingerprint,
            revision, revision + 1L);
        List<P12AReceiptValue> nextReceipts = new List<P12AReceiptValue>(current) { nextReceipt };
        nextReceipts.Sort((left, right) => StringComparer.Ordinal.Compare(left.Identity, right.Identity));
        P12AJusticeReceiptOwnerSnapshot next = CreateNext(kind, state, revision + 1L, nextReceipts);
        if (!next.IsValid) return false;
        mutation = new DailyV1JusticeP18ReceiptMutation(kind, nextReceipt, false, state, next);
        return true;
    }

    internal bool CanCommit(DailyV1JusticeP18ReceiptMutation mutation)
    {
        if (mutation == null || mutation.Receipt == null || !ReferenceEquals(mutation.Expected, state)
            || state == null || !state.IsValid) return false;
        IReadOnlyList<P12AReceiptValue> current = ReceiptsFor(mutation.Kind, state);
        if (mutation.IsReplay)
            return ReferenceEquals(mutation.Next, state)
                && ContainsSame(current, mutation.Receipt);
        if (mutation.Next == null || !mutation.Next.IsValid
            || RevisionFor(mutation.Kind, mutation.Next) != RevisionFor(mutation.Kind, state) + 1L
            || !HasSingleAppend(current, ReceiptsFor(mutation.Kind, mutation.Next), mutation.Receipt))
            return false;
        foreach (DailyV1JusticeP18ReceiptKind other in Enum.GetValues(typeof(DailyV1JusticeP18ReceiptKind)))
            if (other != mutation.Kind
                && (RevisionFor(other, state) != RevisionFor(other, mutation.Next)
                    || !SameReceipts(ReceiptsFor(other, state), ReceiptsFor(other, mutation.Next))))
                return false;
        return true;
    }

    internal void InstallPrepared(DailyV1JusticeP18ReceiptMutation mutation)
    {
        state = mutation.Next;
    }

    internal bool TryCaptureP12AReceiptOwnerSnapshot(out P12AJusticeReceiptOwnerSnapshot snapshot)
    {
        snapshot = null;
        if (state == null || !state.IsValid) return false;
        snapshot = state;
        return true;
    }

    internal bool TryInstallP12AReceiptOwnerSnapshot(P12AJusticeReceiptOwnerSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.IsValid || restored || !IsExactEmpty) return false;
        state = snapshot;
        restored = true;
        return state.CensusRevision == snapshot.CensusRevision;
    }

    private static P12AJusticeReceiptOwnerSnapshot Empty() =>
        new P12AJusticeReceiptOwnerSnapshot(0L, Array.Empty<P12AReceiptValue>(),
            0L, Array.Empty<P12AReceiptValue>(), 0L, Array.Empty<P12AReceiptValue>());

    private static long RevisionFor(DailyV1JusticeP18ReceiptKind kind, P12AJusticeReceiptOwnerSnapshot snapshot)
    {
        switch (kind)
        {
            case DailyV1JusticeP18ReceiptKind.BeginDay: return snapshot.BeginDayRevision;
            case DailyV1JusticeP18ReceiptKind.AdvanceSentences: return snapshot.AdvanceSentencesRevision;
            case DailyV1JusticeP18ReceiptKind.SyncWantedStatuses: return snapshot.SyncWantedStatusesRevision;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static IReadOnlyList<P12AReceiptValue> ReceiptsFor(
        DailyV1JusticeP18ReceiptKind kind, P12AJusticeReceiptOwnerSnapshot snapshot)
    {
        switch (kind)
        {
            case DailyV1JusticeP18ReceiptKind.BeginDay: return snapshot.BeginDayReceipts;
            case DailyV1JusticeP18ReceiptKind.AdvanceSentences: return snapshot.AdvanceSentencesReceipts;
            case DailyV1JusticeP18ReceiptKind.SyncWantedStatuses: return snapshot.SyncWantedStatusesReceipts;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static P12AJusticeReceiptOwnerSnapshot CreateNext(DailyV1JusticeP18ReceiptKind kind,
        P12AJusticeReceiptOwnerSnapshot current, long revision, IReadOnlyList<P12AReceiptValue> receipts)
    {
        switch (kind)
        {
            case DailyV1JusticeP18ReceiptKind.BeginDay:
                return new P12AJusticeReceiptOwnerSnapshot(revision, receipts,
                    current.AdvanceSentencesRevision, current.AdvanceSentencesReceipts,
                    current.SyncWantedStatusesRevision, current.SyncWantedStatusesReceipts);
            case DailyV1JusticeP18ReceiptKind.AdvanceSentences:
                return new P12AJusticeReceiptOwnerSnapshot(current.BeginDayRevision, current.BeginDayReceipts,
                    revision, receipts, current.SyncWantedStatusesRevision, current.SyncWantedStatusesReceipts);
            case DailyV1JusticeP18ReceiptKind.SyncWantedStatuses:
                return new P12AJusticeReceiptOwnerSnapshot(current.BeginDayRevision, current.BeginDayReceipts,
                    current.AdvanceSentencesRevision, current.AdvanceSentencesReceipts, revision, receipts);
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static bool ContainsSame(IReadOnlyList<P12AReceiptValue> values, P12AReceiptValue expected)
    {
        foreach (P12AReceiptValue value in values)
            if (string.Equals(value.Identity, expected.Identity, StringComparison.Ordinal))
                return DailyV1CrimeP18ReceiptOwner.SameReceipt(value, expected);
        return false;
    }

    private static bool HasSingleAppend(IReadOnlyList<P12AReceiptValue> before,
        IReadOnlyList<P12AReceiptValue> after, P12AReceiptValue appended)
    {
        if (after == null || before == null || after.Count != before.Count + 1
            || !ContainsSame(after, appended)) return false;
        foreach (P12AReceiptValue value in before)
            if (!ContainsSame(after, value)) return false;
        return true;
    }

    private static bool SameReceipts(IReadOnlyList<P12AReceiptValue> left,
        IReadOnlyList<P12AReceiptValue> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int i = 0; i < left.Count; i++)
            if (!DailyV1CrimeP18ReceiptOwner.SameReceipt(left[i], right[i])) return false;
        return true;
    }
}
