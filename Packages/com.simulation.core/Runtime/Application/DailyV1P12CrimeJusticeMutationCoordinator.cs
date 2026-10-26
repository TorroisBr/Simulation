using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

/// <summary>
/// Host-neutral view of one exact P12 owner identity participating in a
/// Crime/Justice mutation scope. The host supplies identity and revision reads;
/// the Core coordinator owns admission, scope lifetime, and epoch publication.
/// </summary>
internal sealed class DailyV1P12CrimeJusticeOwnerBinding
{
    internal object OwnerIdentity { get; }
    internal string SectionId { get; }
    internal Func<long> ReadRevision { get; }

    internal DailyV1P12CrimeJusticeOwnerBinding(
        object ownerIdentity,
        string sectionId,
        Func<long> readRevision)
    {
        OwnerIdentity = ownerIdentity ?? throw new ArgumentNullException(nameof(ownerIdentity));
        SectionId = string.IsNullOrWhiteSpace(sectionId)
            ? throw new ArgumentException("A P12 owner section identity is required.", nameof(sectionId))
            : sectionId;
        ReadRevision = readRevision ?? throw new ArgumentNullException(nameof(readRevision));
    }

    internal static DailyV1P12CrimeJusticeOwnerBinding ForNpcStatusOwner(
        DailyV1NpcRuntimeOwners owner)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        return new DailyV1P12CrimeJusticeOwnerBinding(
            owner,
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(owner.RuntimeId),
            () => owner.CrimeJusticeRevision);
    }
}

/// <summary>
/// Narrow adapter over the selected P12 census protocol. It deliberately
/// exposes section baselines and epoch reservation, not owner discovery or
/// mutation policy.
/// </summary>
internal interface IDailyV1P12CrimeJusticeMutationProtocol
{
    bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds);
    bool TryReserveMutationEpoch(out object reservation);
    bool TryNotifyReservedCommittedMutations(object reservation, IReadOnlyCollection<string> sectionIds);
    void ReleaseMutationEpoch(object reservation);
    bool TryValidateMutationEpochCapacity();
    bool NotifyCommittedMutation(string sectionId);
}

/// <summary>
/// Core-owned admission and commit policy for the selected P12 Crime/Justice
/// operations. The host retains exact census identity discovery, while this
/// coordinator owns owner-thread checks, allowed owner/section membership,
/// revision capacity, daily reservation completion, and immediate commits.
/// </summary>
internal sealed class DailyV1P12CrimeJusticeMutationCoordinator
{
    private enum ScopeMode : byte
    {
        DailyReserved = 0,
        ActionImmediate = 1
    }

    private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceIdentityComparer Instance = new ReferenceIdentityComparer();
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
    }

    private sealed class Context
    {
        internal ScopeMode Mode;
        internal Thread OwnerThread;
        internal int OwnerManagedThreadId;
        internal Dictionary<object, DailyV1P12CrimeJusticeOwnerBinding> Owners;
        internal HashSet<string> AllowedSections;
        internal HashSet<string> ChangedSections = new HashSet<string>(StringComparer.Ordinal);
        internal object EpochReservation;

        internal bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class ImmediateScope : IDisposable
    {
        private DailyV1P12CrimeJusticeMutationCoordinator coordinator;
        private readonly Context context;

        internal ImmediateScope(DailyV1P12CrimeJusticeMutationCoordinator coordinator, Context context)
        {
            this.coordinator = coordinator;
            this.context = context;
        }

        public void Dispose()
        {
            DailyV1P12CrimeJusticeMutationCoordinator owner = coordinator;
            coordinator = null;
            owner?.EndImmediateScope(context);
        }
    }

    private readonly Func<bool> isAdmissionEnabled;
    private readonly Func<bool> isOwnerThreadCurrent;
    private readonly Action faultAdmission;
    private readonly IDailyV1P12CrimeJusticeMutationProtocol protocol;
    private Context activeContext;

    internal DailyV1P12CrimeJusticeMutationCoordinator(
        Func<bool> isAdmissionEnabled,
        Func<bool> isOwnerThreadCurrent,
        Action faultAdmission,
        IDailyV1P12CrimeJusticeMutationProtocol protocol)
    {
        this.isAdmissionEnabled = isAdmissionEnabled ?? throw new ArgumentNullException(nameof(isAdmissionEnabled));
        this.isOwnerThreadCurrent = isOwnerThreadCurrent ?? throw new ArgumentNullException(nameof(isOwnerThreadCurrent));
        this.faultAdmission = faultAdmission ?? throw new ArgumentNullException(nameof(faultAdmission));
        this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
    }

    internal bool CanBeginDailyMutation() => !isAdmissionEnabled()
        || (isOwnerThreadCurrent() && activeContext == null);

    internal void ExecuteDailyMutation(
        IReadOnlyList<DailyV1P12CrimeJusticeOwnerBinding> owners,
        IReadOnlyList<string> sectionIds,
        Action mutation)
    {
        if (mutation == null) throw new ArgumentNullException(nameof(mutation));
        if (!isAdmissionEnabled())
        {
            mutation();
            return;
        }
        if (sectionIds != null && sectionIds.Count == 0)
        {
            mutation();
            return;
        }

        if (!TryBuildContext(ScopeMode.DailyReserved, owners, sectionIds,
                out Context context)
            || !isOwnerThreadCurrent()
            || activeContext != null
            || sectionIds.Count == 0)
        {
            Reject("P12 Crime/Justice daily mutation scope is unavailable.");
            return;
        }

        if (!protocol.TryValidateUnchangedSections(sectionIds)
            || !protocol.TryReserveMutationEpoch(out object reservation)
            || reservation == null)
        {
            Reject("P12 Crime/Justice daily mutation could not reserve its owner baseline and epoch.");
            return;
        }

        context.EpochReservation = reservation;
        activeContext = context;
        Exception originalException = null;
        try
        {
            mutation();
        }
        catch (Exception exception)
        {
            originalException = exception;
        }

        bool completed = EndDailyScope(context);
        if (originalException != null)
            ExceptionDispatchInfo.Capture(originalException).Throw();
        if (!completed)
            throw new InvalidOperationException("P12 Crime/Justice daily mutation notification failed.");
    }

    internal bool TryBeginImmediateMutation(
        IReadOnlyList<DailyV1P12CrimeJusticeOwnerBinding> owners,
        IReadOnlyList<string> sectionIds,
        out IDisposable scope)
    {
        scope = null;
        if (!isAdmissionEnabled()) return true;
        if (!TryBuildContext(ScopeMode.ActionImmediate, owners, sectionIds,
                out Context context)
            || !isOwnerThreadCurrent()
            || activeContext != null
            || sectionIds.Count == 0
            || !protocol.TryValidateUnchangedSections(sectionIds))
        {
            faultAdmission();
            return false;
        }

        activeContext = context;
        scope = new ImmediateScope(this, context);
        return true;
    }

    /// <summary>
    /// Builds the admitted P12 status-owner scope from the exact selected Core
    /// owner graph. Unity may resolve a compatibility projection to its owner,
    /// but section identity and owner binding are derived here from Core truth.
    /// Duplicate and null participant entries retain the legacy action-scope
    /// behavior; any non-null owner outside this graph fails closed.
    /// </summary>
    internal bool TryBeginSelectedNpcStatusMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> participants,
        out IDisposable scope)
    {
        scope = null;
        if (!isAdmissionEnabled()) return true;
        if (graph == null || participants == null)
        {
            faultAdmission();
            return false;
        }

        List<DailyV1P12CrimeJusticeOwnerBinding> owners =
            new List<DailyV1P12CrimeJusticeOwnerBinding>(participants.Count);
        List<string> sections = new List<string>(participants.Count);
        HashSet<DailyV1NpcRuntimeOwners> uniqueOwners =
            new HashSet<DailyV1NpcRuntimeOwners>(ReferenceIdentityComparer.Instance);
        foreach (DailyV1NpcRuntimeOwners owner in participants)
        {
            if (owner == null || !uniqueOwners.Add(owner)) continue;
            if (!graph.ContainsExactNpcOwner(owner)
                || !ReferenceEquals(owner.Definitions, graph.Definitions))
            {
                faultAdmission();
                return false;
            }

            DailyV1P12CrimeJusticeOwnerBinding binding =
                DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(owner);
            owners.Add(binding);
            sections.Add(binding.SectionId);
        }

        if (sections.Count == 0)
        {
            faultAdmission();
            return false;
        }

        return TryBeginImmediateMutation(owners, sections, out scope);
    }

    internal bool CanReuseImmediateMutation(
        IReadOnlyList<DailyV1P12CrimeJusticeOwnerBinding> owners,
        IReadOnlyList<string> sectionIds)
    {
        if (!isAdmissionEnabled()) return true;
        if (!TryBuildContext(ScopeMode.ActionImmediate, owners, sectionIds,
                out Context expected)
            || !isOwnerThreadCurrent()
            || activeContext == null
            || activeContext.Mode != ScopeMode.ActionImmediate
            || !activeContext.IsOwnedByCurrentThread()
            || !SameOwners(activeContext.Owners, expected.Owners)
            || !activeContext.AllowedSections.SetEquals(expected.AllowedSections))
        {
            faultAdmission();
            return false;
        }
        return true;
    }

    internal bool CanCommit(object ownerIdentity, string sectionId)
    {
        if (!isAdmissionEnabled()) return true;
        Context context = activeContext;
        if (!TryGetBinding(context, ownerIdentity, sectionId,
                out DailyV1P12CrimeJusticeOwnerBinding binding)
            || !context.IsOwnedByCurrentThread())
        {
            faultAdmission();
            return false;
        }

        long revision;
        try { revision = binding.ReadRevision(); }
        catch
        {
            faultAdmission();
            return false;
        }
        if (revision == long.MaxValue)
        {
            faultAdmission();
            return false;
        }

        if (context.Mode == ScopeMode.DailyReserved)
        {
            return context.EpochReservation != null
                && (context.ChangedSections.Contains(sectionId)
                    || protocol.TryValidateUnchangedSections(new[] { sectionId }));
        }
        if (context.Mode != ScopeMode.ActionImmediate
            || !protocol.TryValidateUnchangedSections(new[] { sectionId })
            || !protocol.TryValidateMutationEpochCapacity())
        {
            faultAdmission();
            return false;
        }
        return true;
    }

    internal void NotifyCommitted(object ownerIdentity, string sectionId)
    {
        if (!isAdmissionEnabled()) return;
        Context context = activeContext;
        if (!TryGetBinding(context, ownerIdentity, sectionId,
                out _)
            || !context.IsOwnedByCurrentThread())
        {
            faultAdmission();
            return;
        }
        if (context.Mode == ScopeMode.DailyReserved)
        {
            context.ChangedSections.Add(sectionId);
            return;
        }
        if (context.Mode != ScopeMode.ActionImmediate
            || !protocol.NotifyCommittedMutation(sectionId))
            faultAdmission();
    }

    private bool EndDailyScope(Context context)
    {
        bool valid = context != null
            && context.Mode == ScopeMode.DailyReserved
            && context.IsOwnedByCurrentThread()
            && ReferenceEquals(activeContext, context)
            && context.EpochReservation != null;
        if (!valid)
        {
            faultAdmission();
            if (context != null && context.EpochReservation != null)
                TryRelease(context.EpochReservation);
            if (ReferenceEquals(activeContext, context)) activeContext = null;
            return false;
        }

        bool notified;
        if (context.ChangedSections.Count == 0)
        {
            try
            {
                protocol.ReleaseMutationEpoch(context.EpochReservation);
                notified = true;
            }
            catch { notified = false; }
        }
        else
        {
            notified = protocol.TryNotifyReservedCommittedMutations(
                context.EpochReservation, context.ChangedSections);
        }
        if (!notified) faultAdmission();
        activeContext = null;
        return notified;
    }

    private void EndImmediateScope(Context context)
    {
        if (context == null) return;
        if (context.Mode != ScopeMode.ActionImmediate
            || !context.IsOwnedByCurrentThread()
            || !ReferenceEquals(activeContext, context))
            faultAdmission();
        if (ReferenceEquals(activeContext, context)) activeContext = null;
    }

    private bool TryBuildContext(
        ScopeMode mode,
        IReadOnlyList<DailyV1P12CrimeJusticeOwnerBinding> owners,
        IReadOnlyList<string> sectionIds,
        out Context context)
    {
        context = null;
        if (owners == null || sectionIds == null) return false;
        Dictionary<object, DailyV1P12CrimeJusticeOwnerBinding> ownerMap =
            new Dictionary<object, DailyV1P12CrimeJusticeOwnerBinding>(ReferenceIdentityComparer.Instance);
        HashSet<string> allowedSections = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in sectionIds)
            if (string.IsNullOrWhiteSpace(sectionId) || !allowedSections.Add(sectionId))
                return false;
        foreach (DailyV1P12CrimeJusticeOwnerBinding binding in owners)
        {
            if (binding == null
                || !allowedSections.Contains(binding.SectionId)
                || !ownerMap.TryAdd(binding.OwnerIdentity, binding))
                return false;
        }

        context = new Context
        {
            Mode = mode,
            OwnerThread = Thread.CurrentThread,
            OwnerManagedThreadId = Thread.CurrentThread.ManagedThreadId,
            Owners = ownerMap,
            AllowedSections = allowedSections
        };
        return true;
    }

    private static bool TryGetBinding(
        Context context,
        object ownerIdentity,
        string sectionId,
        out DailyV1P12CrimeJusticeOwnerBinding binding)
    {
        binding = null;
        return context != null
            && ownerIdentity != null
            && !string.IsNullOrWhiteSpace(sectionId)
            && context.IsOwnedByCurrentThread()
            && context.AllowedSections.Contains(sectionId)
            && context.Owners.TryGetValue(ownerIdentity, out binding)
            && string.Equals(binding.SectionId, sectionId, StringComparison.Ordinal);
    }

    private static bool SameOwners(
        Dictionary<object, DailyV1P12CrimeJusticeOwnerBinding> left,
        Dictionary<object, DailyV1P12CrimeJusticeOwnerBinding> right)
    {
        if (left.Count != right.Count) return false;
        foreach (KeyValuePair<object, DailyV1P12CrimeJusticeOwnerBinding> entry in left)
        {
            if (!right.TryGetValue(entry.Key, out DailyV1P12CrimeJusticeOwnerBinding match)
                || !string.Equals(entry.Value.SectionId, match.SectionId, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private void Reject(string message)
    {
        faultAdmission();
        throw new InvalidOperationException(message);
    }

    private void TryRelease(object reservation)
    {
        try { protocol.ReleaseMutationEpoch(reservation); }
        catch { }
    }
}
