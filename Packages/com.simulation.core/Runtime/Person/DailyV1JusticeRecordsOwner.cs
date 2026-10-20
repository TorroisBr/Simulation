using System;
using System.Collections.Generic;

/// <summary>
/// Shared value owner for one Daily-v1 wanted-record row. Unity object
/// references are intentionally kept by its host projection; this owner holds
/// only stable runtime identities and mutable domain values.
/// </summary>
internal sealed class DailyV1JusticeWantedRecordOwner : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private Func<bool> mutationAdmission;
    private Action mutationCommitted;
    private float bounty;
    private int sentenceDays;
    private bool resolved;

    internal string TargetRuntimeId { get; }
    internal string CityRuntimeId { get; }
    internal float Bounty => bounty;
    internal int SentenceDays => sentenceDays;
    internal bool IsResolved => resolved;
    internal bool HasP12MutationBoundary => mutationAdmission != null || mutationCommitted != null;

    internal DailyV1JusticeWantedRecordOwner(
        string targetRuntimeId,
        string cityRuntimeId,
        float bounty,
        int sentenceDays,
        bool resolved = false)
    {
        TargetRuntimeId = targetRuntimeId;
        CityRuntimeId = cityRuntimeId;
        this.bounty = UnityMathfCompatibility.Max(0f, bounty);
        this.sentenceDays = UnityMathfCompatibility.Max(1, sentenceDays);
        this.resolved = resolved;
    }

    internal bool TryAddPenalty(float additionalBounty, int additionalSentenceDays)
    {
        ThrowIfFaulted();
        float nextBounty = bounty + UnityMathfCompatibility.Max(0f, additionalBounty);
        int nextSentenceDays = UnityMathfCompatibility.Max(1,
            sentenceDays + UnityMathfCompatibility.Max(0, additionalSentenceDays));
        if (nextBounty == bounty && nextSentenceDays == sentenceDays) return true;
        if (!CanCommitMutation()) return false;
        bounty = nextBounty;
        sentenceDays = nextSentenceDays;
        NotifyMutationCommitted();
        return true;
    }

    internal bool TryResolve()
    {
        ThrowIfFaulted();
        if (resolved) return true;
        if (!CanCommitMutation()) return false;
        resolved = true;
        NotifyMutationCommitted();
        return true;
    }

    internal bool TryBindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!CanBindP12MutationBoundary(admission, committed)) return false;
        mutationAdmission = admission;
        mutationCommitted = committed;
        return true;
    }

    internal bool CanBindP12MutationBoundary(Func<bool> admission, Action committed) =>
        admission != null && committed != null && mutationAdmission == null && mutationCommitted == null;

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);

    private bool CanCommitMutation()
    {
        if (mutationAdmission == null && mutationCommitted == null) return true;
        if (mutationAdmission == null || mutationCommitted == null) return false;
        try { return mutationAdmission(); }
        catch { return false; }
    }

    private void NotifyMutationCommitted()
    {
        try { mutationCommitted?.Invoke(); }
        catch { }
    }

    private void ThrowIfFaulted()
    {
        if (!mutationGuardBinding.CanMutate)
            throw new InvalidOperationException("Runtime mutation is faulted.");
    }
}

/// <summary>Shared value owner for one Daily-v1 prison-sentence row.</summary>
internal sealed class DailyV1JusticeSentenceOwner : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private Func<bool> mutationAdmission;
    private Action mutationCommitted;
    private int remainingDays;
    private int failedEscapeAttempts;
    private bool wasArrestedToday;

    internal string TargetRuntimeId { get; }
    internal string CityRuntimeId { get; }
    internal DailyV1JusticeWantedRecordOwner Warrant { get; }
    internal int RemainingDays => remainingDays;
    internal int FailedEscapeAttempts => failedEscapeAttempts;
    internal bool WasArrestedToday => wasArrestedToday;
    internal bool IsActive => remainingDays > 0;
    internal bool HasP12MutationBoundary => mutationAdmission != null || mutationCommitted != null;

    internal DailyV1JusticeSentenceOwner(
        string targetRuntimeId,
        string cityRuntimeId,
        DailyV1JusticeWantedRecordOwner warrant,
        int remainingDays,
        int failedEscapeAttempts = 0,
        bool wasArrestedToday = true)
    {
        if (remainingDays < 0) throw new ArgumentOutOfRangeException(nameof(remainingDays));
        if (failedEscapeAttempts < 0) throw new ArgumentOutOfRangeException(nameof(failedEscapeAttempts));
        TargetRuntimeId = targetRuntimeId;
        CityRuntimeId = cityRuntimeId;
        Warrant = warrant;
        this.remainingDays = remainingDays;
        this.failedEscapeAttempts = failedEscapeAttempts;
        this.wasArrestedToday = wasArrestedToday;
    }

    internal bool TryAdvanceDay()
    {
        ThrowIfFaulted();
        int next = UnityMathfCompatibility.Max(0, remainingDays - 1);
        if (next == remainingDays) return true;
        if (!CanCommitMutation()) return false;
        remainingDays = next;
        NotifyMutationCommitted();
        return true;
    }

    internal bool TryRegisterFailedEscape(int additionalSentenceDays)
    {
        ThrowIfFaulted();
        if (!CanCommitMutation()) return false;
        failedEscapeAttempts++;
        remainingDays += UnityMathfCompatibility.Max(0, additionalSentenceDays);
        NotifyMutationCommitted();
        return true;
    }

    internal bool TryClearArrestedToday()
    {
        ThrowIfFaulted();
        if (!wasArrestedToday) return true;
        if (!CanCommitMutation()) return false;
        wasArrestedToday = false;
        NotifyMutationCommitted();
        return true;
    }

    internal bool TryBindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!CanBindP12MutationBoundary(admission, committed)) return false;
        mutationAdmission = admission;
        mutationCommitted = committed;
        return true;
    }

    internal bool CanBindP12MutationBoundary(Func<bool> admission, Action committed) =>
        admission != null && committed != null && mutationAdmission == null && mutationCommitted == null;

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);

    private bool CanCommitMutation()
    {
        if (mutationAdmission == null && mutationCommitted == null) return true;
        if (mutationAdmission == null || mutationCommitted == null) return false;
        try { return mutationAdmission(); }
        catch { return false; }
    }

    private void NotifyMutationCommitted()
    {
        try { mutationCommitted?.Invoke(); }
        catch { }
    }

    private void ThrowIfFaulted()
    {
        if (!mutationGuardBinding.CanMutate)
            throw new InvalidOperationException("Runtime mutation is faulted.");
    }
}

/// <summary>
/// Single Core-owned ordered store and revision for selected Daily-v1 Justice
/// facts. Host adapters may project these values, but may not mirror them.
/// </summary>
internal sealed class DailyV1JusticeRecordsOwner : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly List<DailyV1JusticeWantedRecordOwner> wantedRecords = new List<DailyV1JusticeWantedRecordOwner>();
    private readonly List<DailyV1JusticeSentenceOwner> sentences = new List<DailyV1JusticeSentenceOwner>();
    private Func<bool> p12MutationAdmission;
    private Action p12MutationCommitted;
    private long revision;

    internal IReadOnlyList<DailyV1JusticeWantedRecordOwner> WantedRecords => wantedRecords;
    internal IReadOnlyList<DailyV1JusticeSentenceOwner> Sentences => sentences;
    internal long Revision => revision;
    internal bool HasP12MutationBoundary => p12MutationAdmission != null || p12MutationCommitted != null;

    internal DailyV1JusticeWantedRecordOwner FindActiveWantedRecord(string targetRuntimeId, string cityRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(targetRuntimeId) || string.IsNullOrWhiteSpace(cityRuntimeId)) return null;
        foreach (DailyV1JusticeWantedRecordOwner row in wantedRecords)
            if (row != null && !row.IsResolved
                && string.Equals(row.TargetRuntimeId, targetRuntimeId, StringComparison.Ordinal)
                && string.Equals(row.CityRuntimeId, cityRuntimeId, StringComparison.Ordinal))
                return row;
        return null;
    }

    internal IReadOnlyList<DailyV1JusticeWantedRecordOwner> FindActiveWantedRecords(
        string targetRuntimeId, string cityRuntimeId)
    {
        List<DailyV1JusticeWantedRecordOwner> result = new List<DailyV1JusticeWantedRecordOwner>();
        if (!string.IsNullOrWhiteSpace(targetRuntimeId) && !string.IsNullOrWhiteSpace(cityRuntimeId))
            foreach (DailyV1JusticeWantedRecordOwner row in wantedRecords)
                if (row != null && !row.IsResolved
                    && string.Equals(row.TargetRuntimeId, targetRuntimeId, StringComparison.Ordinal)
                    && string.Equals(row.CityRuntimeId, cityRuntimeId, StringComparison.Ordinal))
                    result.Add(row);
        return result.AsReadOnly();
    }

    internal IReadOnlyList<DailyV1JusticeWantedRecordOwner> FindActiveWantedRecords(string targetRuntimeId)
    {
        List<DailyV1JusticeWantedRecordOwner> result = new List<DailyV1JusticeWantedRecordOwner>();
        if (!string.IsNullOrWhiteSpace(targetRuntimeId))
            foreach (DailyV1JusticeWantedRecordOwner row in wantedRecords)
                if (row != null && !row.IsResolved
                    && string.Equals(row.TargetRuntimeId, targetRuntimeId, StringComparison.Ordinal))
                    result.Add(row);
        return result.AsReadOnly();
    }

    internal DailyV1JusticeSentenceOwner FindActiveSentence(string targetRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(targetRuntimeId)) return null;
        foreach (DailyV1JusticeSentenceOwner row in sentences)
            if (row != null && row.IsActive
                && string.Equals(row.TargetRuntimeId, targetRuntimeId, StringComparison.Ordinal))
                return row;
        return null;
    }

    internal IReadOnlyList<DailyV1JusticeSentenceOwner> FindActiveSentences(string targetRuntimeId)
    {
        List<DailyV1JusticeSentenceOwner> result = new List<DailyV1JusticeSentenceOwner>();
        if (!string.IsNullOrWhiteSpace(targetRuntimeId))
            foreach (DailyV1JusticeSentenceOwner row in sentences)
                if (row != null && row.IsActive
                    && string.Equals(row.TargetRuntimeId, targetRuntimeId, StringComparison.Ordinal))
                    result.Add(row);
        return result.AsReadOnly();
    }

    internal bool TryAddWantedRecord(DailyV1JusticeWantedRecordOwner row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
            || string.IsNullOrWhiteSpace(row.CityRuntimeId) || wantedRecords.Contains(row)
            || row.HasP12MutationBoundary
            || !BindWantedRowGuard(row)
            || !BindWantedRowBoundary(row)) return false;
        wantedRecords.Add(row);
        return true;
    }

    internal bool TryAddSentence(DailyV1JusticeSentenceOwner row)
    {
        if (row == null || sentences.Contains(row)
            || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
            || string.IsNullOrWhiteSpace(row.CityRuntimeId)
            || row.Warrant == null
            || !string.Equals(row.TargetRuntimeId, row.Warrant.TargetRuntimeId, StringComparison.Ordinal)
            || !string.Equals(row.CityRuntimeId, row.Warrant.CityRuntimeId, StringComparison.Ordinal)
            || !wantedRecords.Contains(row.Warrant) || row.HasP12MutationBoundary
            || !BindSentenceRowGuard(row)
            || !BindSentenceRowBoundary(row)) return false;
        sentences.Add(row);
        return true;
    }

    internal void AddWantedRecordForStaging(DailyV1JusticeWantedRecordOwner row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
            || string.IsNullOrWhiteSpace(row.CityRuntimeId)
            || wantedRecords.Contains(row) || row.HasP12MutationBoundary
            || !BindWantedRowGuard(row))
            throw new InvalidOperationException("A staged Justice wanted row is invalid.");
        if (!BindWantedRowBoundary(row))
            throw new InvalidOperationException("A staged Justice wanted row could not bind its mutation owner.");
        wantedRecords.Add(row);
    }

    internal void AddSentenceForStaging(DailyV1JusticeSentenceOwner row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.TargetRuntimeId)
            || string.IsNullOrWhiteSpace(row.CityRuntimeId)
            || row.Warrant == null
            || !string.Equals(row.TargetRuntimeId, row.Warrant.TargetRuntimeId, StringComparison.Ordinal)
            || !string.Equals(row.CityRuntimeId, row.Warrant.CityRuntimeId, StringComparison.Ordinal)
            || sentences.Contains(row) || row.HasP12MutationBoundary || !wantedRecords.Contains(row.Warrant)
            || !BindSentenceRowGuard(row))
            throw new InvalidOperationException("A staged Justice sentence row is invalid.");
        if (!BindSentenceRowBoundary(row))
            throw new InvalidOperationException("A staged Justice sentence row could not bind its mutation owner.");
        sentences.Add(row);
    }

    internal bool TryRemoveSentenceAt(int index)
    {
        if (index < 0 || index >= sentences.Count) return false;
        sentences.RemoveAt(index);
        return true;
    }

    internal int IndexOfSentence(DailyV1JusticeSentenceOwner row) => sentences.IndexOf(row);
    internal bool ContainsWantedRecord(DailyV1JusticeWantedRecordOwner row) => wantedRecords.Contains(row);
    internal bool ContainsSentence(DailyV1JusticeSentenceOwner row) => sentences.Contains(row);

    internal bool TryInstallExactRevision(long value)
    {
        if (value < 0L || revision != 0L || HasP12MutationBoundary) return false;
        revision = value;
        return true;
    }

    internal bool TryBindWantedRecordP12MutationBoundary(DailyV1JusticeWantedRecordOwner row)
    {
        if (row == null) return false;
        return BindWantedRowBoundary(row);
    }

    internal bool TryBindSentenceP12MutationBoundary(DailyV1JusticeSentenceOwner row)
    {
        if (row == null) return false;
        return BindSentenceRowBoundary(row);
    }

    internal bool TryBindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null || committed == null
            || p12MutationAdmission != null || p12MutationCommitted != null) return false;
        foreach (DailyV1JusticeWantedRecordOwner row in wantedRecords)
            if (!row.CanBindP12MutationBoundary(
                () => wantedRecords.Contains(row) && CanCommitMutation(),
                () => NotifyMutationCommitted())) return false;
        foreach (DailyV1JusticeSentenceOwner row in sentences)
            if (!row.CanBindP12MutationBoundary(
                () => sentences.Contains(row) && CanCommitMutation(),
                () => NotifyMutationCommitted())) return false;
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
        foreach (DailyV1JusticeWantedRecordOwner row in wantedRecords)
            if (!BindWantedRowBoundary(row))
                throw new InvalidOperationException("Justice wanted-row mutation binding changed after preflight.");
        foreach (DailyV1JusticeSentenceOwner row in sentences)
            if (!BindSentenceRowBoundary(row))
                throw new InvalidOperationException("Justice sentence-row mutation binding changed after preflight.");
        return true;
    }

    internal bool CanCommitMutation()
    {
        if (!mutationGuardBinding.CanMutate) return false;
        if (p12MutationAdmission == null && p12MutationCommitted == null) return true;
        if (p12MutationAdmission == null || p12MutationCommitted == null || revision == long.MaxValue) return false;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    internal bool NotifyMutationCommitted()
    {
        if (!mutationGuardBinding.CanMutate) return false;
        if (p12MutationAdmission == null) return true;
        if (p12MutationCommitted == null || revision == long.MaxValue) return false;
        revision++;
        try { p12MutationCommitted?.Invoke(); }
        catch { }
        return true;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (!mutationGuardBinding.CanBindTo(guard)) return false;
        foreach (DailyV1JusticeWantedRecordOwner row in wantedRecords)
            if (guard == null || !row.CanBindMutationGuard(guard)) return false;
        foreach (DailyV1JusticeSentenceOwner row in sentences)
            if (guard == null || !row.CanBindMutationGuard(guard)) return false;
        return true;
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (!CanBindMutationGuard(guard) || !mutationGuardBinding.TryBindTo(guard)) return false;
        foreach (DailyV1JusticeWantedRecordOwner row in wantedRecords)
            if (!row.TryBindMutationGuard(guard)) return false;
        foreach (DailyV1JusticeSentenceOwner row in sentences)
            if (!row.TryBindMutationGuard(guard)) return false;
        return true;
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);

    private bool BindWantedRowGuard(DailyV1JusticeWantedRecordOwner row) =>
        mutationGuardBinding.BoundGuard == null
        || row.TryBindMutationGuard(mutationGuardBinding.BoundGuard);

    private bool BindSentenceRowGuard(DailyV1JusticeSentenceOwner row) =>
        mutationGuardBinding.BoundGuard == null
        || row.TryBindMutationGuard(mutationGuardBinding.BoundGuard);

    private bool BindWantedRowBoundary(DailyV1JusticeWantedRecordOwner row)
    {
        if (p12MutationAdmission == null && p12MutationCommitted == null) return true;
        return p12MutationAdmission != null && p12MutationCommitted != null
            && row.TryBindP12MutationBoundary(
                () => wantedRecords.Contains(row) && CanCommitMutation(),
                () => NotifyMutationCommitted());
    }

    private bool BindSentenceRowBoundary(DailyV1JusticeSentenceOwner row)
    {
        if (p12MutationAdmission == null && p12MutationCommitted == null) return true;
        return p12MutationAdmission != null && p12MutationCommitted != null
            && row.TryBindP12MutationBoundary(
                () => sentences.Contains(row) && CanCommitMutation(),
                () => NotifyMutationCommitted());
    }
}
