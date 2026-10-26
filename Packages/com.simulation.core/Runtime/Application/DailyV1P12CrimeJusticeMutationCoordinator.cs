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

    internal static DailyV1P12CrimeJusticeOwnerBinding ForJusticeRecordsOwner(
        object censusOwnerIdentity,
        DailyV1JusticeRecordsOwner owner)
    {
        if (censusOwnerIdentity == null) throw new ArgumentNullException(nameof(censusOwnerIdentity));
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        return new DailyV1P12CrimeJusticeOwnerBinding(
            censusOwnerIdentity,
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            () => owner.Revision);
    }
}

/// <summary>
/// Narrow adapter over the selected P12 census protocol. It deliberately
/// exposes section baselines and epoch reservation, not owner discovery or
/// mutation policy.
/// </summary>
internal interface IDailyV1P12CrimeJusticeMutationProtocol
{
    void FaultClosed();
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
        internal DailyV1ExecutionBoundaryState ExecutionState;

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

    /// <summary>
    /// Core preflight for the selected daily mutation boundary. It verifies
    /// the exact admitted execution state and owner thread before a Unity
    /// compatibility adapter inspects system projections.
    /// </summary>
    internal bool CanBeginSelectedDailyMutation(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (executionState != null
            && graph != null
            && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            && executionState.HasAdmittedSelectedDailyExecution
            && executionState.AdmissionContext != null
            && executionState.AdmissionContext.IsOwnedByCurrentThread()
            && activeContext == null)
            return true;

        FaultSelected(executionState);
        return false;
    }

    internal void ExecuteDailyMutation(
        IReadOnlyList<DailyV1P12CrimeJusticeOwnerBinding> owners,
        IReadOnlyList<string> sectionIds,
        Action mutation) => ExecuteDailyMutation(owners, sectionIds, mutation, null);

    private void ExecuteDailyMutation(
        IReadOnlyList<DailyV1P12CrimeJusticeOwnerBinding> owners,
        IReadOnlyList<string> sectionIds,
        Action mutation,
        DailyV1ExecutionBoundaryState executionState)
    {
        if (mutation == null) throw new ArgumentNullException(nameof(mutation));
        if (executionState == null && !isAdmissionEnabled())
        {
            mutation();
            return;
        }
        if (sectionIds != null && sectionIds.Count == 0)
        {
            mutation();
            return;
        }

        if (!TryBuildContext(ScopeMode.DailyReserved, owners, sectionIds, executionState,
                out Context context)
            || !IsOwnerThreadCurrent(context)
            || activeContext != null
            || sectionIds.Count == 0)
        {
            Reject("P12 Crime/Justice daily mutation scope is unavailable.", executionState);
            return;
        }

        if (!protocol.TryValidateUnchangedSections(sectionIds)
            || !protocol.TryReserveMutationEpoch(out object reservation)
            || reservation == null)
        {
            Reject("P12 Crime/Justice daily mutation could not reserve its owner baseline and epoch.", executionState);
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

    /// <summary>
    /// Admits one selected Daily-v1 Crime/Justice phase from the exact Core
    /// runtime graph. Core derives every NPC status owner, Justice revision
    /// source, and section identity. The host supplies opaque Crime and Justice
    /// owner/census identity tokens; Core verifies both receipt bindings and
    /// each phase remains a separate reserved P12 scope.
    /// </summary>
    internal void ExecuteSelectedDailyMutation(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1SelectedCrimeJusticeDailyPhase phase,
        object crimeOwnerIdentity,
        object crimeReceiptCensusOwnerIdentity,
        object justiceOwnerIdentity,
        object justiceReceiptCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        bool includeJusticeReceiptSection,
        Action mutation)
    {
        if (mutation == null) throw new ArgumentNullException(nameof(mutation));
        if (!Enum.IsDefined(typeof(DailyV1SelectedCrimeJusticeDailyPhase), phase)
            || executionState == null
            || graph == null
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            || !executionState.HasAdmittedSelectedDailyExecution
            || executionState.AdmissionContext == null
            || !executionState.AdmissionContext.IsOwnedByCurrentThread()
            || graph.Npcs == null
            || !graph.MatchesExactOwnerInstances(graph.Cities, graph.Npcs)
            || includeCrimeReceiptSection != (crimeOwnerIdentity != null)
            || !ReferenceEquals(crimeOwnerIdentity, crimeReceiptCensusOwnerIdentity)
            || includeJusticeReceiptSection != (justiceOwnerIdentity != null)
            || !ReferenceEquals(justiceOwnerIdentity, justiceReceiptCensusOwnerIdentity)
            || (justiceOwnerIdentity != null && graph.JusticeRecordsOwner == null))
        {
            Reject("P12 selected Crime/Justice daily mutation does not match its exact Core owner graph or phase contract, or lacks the exact Crime receipt census identity or exact Justice census identity.", executionState);
            return;
        }

        List<DailyV1P12CrimeJusticeOwnerBinding> npcBindings =
            new List<DailyV1P12CrimeJusticeOwnerBinding>(graph.Npcs.Count);
        HashSet<string> npcSectionSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null
                || !graph.ContainsExactNpcOwner(npc)
                || !ReferenceEquals(npc.Definitions, graph.Definitions))
            {
                Reject("P12 selected Crime/Justice daily mutation encountered a non-exact Core NPC owner.", executionState);
                return;
            }

            DailyV1P12CrimeJusticeOwnerBinding binding =
                DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(npc);
            if (!npcSectionSet.Add(binding.SectionId))
            {
                Reject("P12 selected Crime/Justice daily mutation contains duplicate NPC status sections.", executionState);
                return;
            }
            npcBindings.Add(binding);
        }

        npcBindings.Sort((left, right) => StringComparer.Ordinal.Compare(
            ((DailyV1NpcRuntimeOwners)left.OwnerIdentity).RuntimeId,
            ((DailyV1NpcRuntimeOwners)right.OwnerIdentity).RuntimeId));

        List<DailyV1P12CrimeJusticeOwnerBinding> owners =
            new List<DailyV1P12CrimeJusticeOwnerBinding>(graph.Npcs.Count + 1);
        List<string> sections = new List<string>(graph.Npcs.Count + 3);
        HashSet<string> sectionSet = new HashSet<string>(StringComparer.Ordinal);

        DailyV1P12CrimeJusticeOwnerBinding justiceRecordsBinding =
            justiceReceiptCensusOwnerIdentity == null
                ? null
                : DailyV1P12CrimeJusticeOwnerBinding.ForJusticeRecordsOwner(
                    justiceReceiptCensusOwnerIdentity, graph.JusticeRecordsOwner);
        if (justiceRecordsBinding != null)
        {
            owners.Add(justiceRecordsBinding);
            if (!sectionSet.Add(justiceRecordsBinding.SectionId))
            {
                Reject("P12 selected Crime/Justice daily mutation contains a duplicate Justice section.", executionState);
                return;
            }
            sections.Add(justiceRecordsBinding.SectionId);
        }

        if (includeJusticeReceiptSection
            && sectionSet.Add(DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId))
            sections.Add(DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId);
        if (includeCrimeReceiptSection
            && sectionSet.Add(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId))
            sections.Add(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId);

        foreach (DailyV1P12CrimeJusticeOwnerBinding binding in npcBindings)
        {
            owners.Add(binding);
            if (!sectionSet.Add(binding.SectionId))
            {
                Reject("P12 selected Crime/Justice daily mutation contains a duplicate owner section.", executionState);
                return;
            }
            sections.Add(binding.SectionId);
        }

        if (sections.Count == 0)
        {
            Reject("P12 selected Crime/Justice daily mutation has no admitted owner sections.", executionState);
            return;
        }

        ExecuteDailyMutation(owners, sections, mutation, executionState);
    }

    internal bool TryBeginImmediateMutation(
        IReadOnlyList<DailyV1P12CrimeJusticeOwnerBinding> owners,
        IReadOnlyList<string> sectionIds,
        out IDisposable scope)
    {
        scope = null;
        if (!isAdmissionEnabled()) return true;
        if (!TryBuildContext(ScopeMode.ActionImmediate, owners, sectionIds, null,
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
        if (!TryBuildContext(ScopeMode.ActionImmediate, owners, sectionIds, null,
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
        Context context = activeContext;
        if (!IsAdmissionEnabled(context)) return true;
        if (!TryGetBinding(context, ownerIdentity, sectionId,
                out DailyV1P12CrimeJusticeOwnerBinding binding)
            || !IsOwnerThreadCurrent(context))
        {
            Fault(context);
            return false;
        }

        long revision;
        try { revision = binding.ReadRevision(); }
        catch
        {
            Fault(context);
            return false;
        }
        if (revision == long.MaxValue)
        {
            Fault(context);
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
            Fault(context);
            return false;
        }
        return true;
    }

    internal void NotifyCommitted(object ownerIdentity, string sectionId)
    {
        Context context = activeContext;
        if (!IsAdmissionEnabled(context)) return;
        if (!TryGetBinding(context, ownerIdentity, sectionId,
                out _)
            || !IsOwnerThreadCurrent(context))
        {
            Fault(context);
            return;
        }
        if (context.Mode == ScopeMode.DailyReserved)
        {
            context.ChangedSections.Add(sectionId);
            return;
        }
        if (context.Mode != ScopeMode.ActionImmediate
            || !protocol.NotifyCommittedMutation(sectionId))
            Fault(context);
    }

    private bool EndDailyScope(Context context)
    {
        bool valid = context != null
            && context.Mode == ScopeMode.DailyReserved
            && IsOwnerThreadCurrent(context)
            && ReferenceEquals(activeContext, context)
            && context.EpochReservation != null;
        if (!valid)
        {
            Fault(context);
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
        if (!notified) Fault(context);
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
        DailyV1ExecutionBoundaryState executionState,
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
            AllowedSections = allowedSections,
            ExecutionState = executionState
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

    private bool IsAdmissionEnabled(Context context) =>
        context?.ExecutionState != null || isAdmissionEnabled();

    private bool IsOwnerThreadCurrent(Context context) => context?.ExecutionState != null
        ? context.ExecutionState.AdmissionContext != null
            && context.ExecutionState.AdmissionContext.IsOwnedByCurrentThread()
        : isOwnerThreadCurrent();

    private void Fault(Context context)
    {
        if (context?.ExecutionState != null)
        {
            FaultSelected(context.ExecutionState);
            return;
        }
        faultAdmission();
    }

    private void FaultSelected(DailyV1ExecutionBoundaryState executionState)
    {
        try { protocol.FaultClosed(); }
        catch { }
    }

    private void Reject(string message, DailyV1ExecutionBoundaryState executionState = null)
    {
        if (executionState != null)
        {
            FaultSelected(executionState);
        }
        else
        {
            faultAdmission();
        }
        throw new InvalidOperationException(message);
    }

    private void TryRelease(object reservation)
    {
        try { protocol.ReleaseMutationEpoch(reservation); }
        catch { }
    }
}
