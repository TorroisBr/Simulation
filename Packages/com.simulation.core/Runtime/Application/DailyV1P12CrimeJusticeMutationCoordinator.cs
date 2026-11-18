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

    internal static DailyV1P12CrimeJusticeOwnerBinding ForNpcTravelPlanOwner(
        DailyV1NpcRuntimeOwners owner)
    {
        if (owner?.TravelPlan == null) throw new ArgumentNullException(nameof(owner));
        return new DailyV1P12CrimeJusticeOwnerBinding(
            owner.TravelPlan,
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, owner.RuntimeId),
            () => owner.TravelPlan.Revision);
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
    bool TryRefreshCommittedMutationBaselines(IReadOnlyCollection<string> sectionIds);
    bool TryRefreshCommittedMutationBaselinesForReservation(
        object reservationToken,
        IReadOnlyCollection<string> sectionIds);
    bool TryReserveMutationEpoch(out object reservation);
    bool TryNotifyReservedCommittedMutations(object reservation, IReadOnlyCollection<string> sectionIds);
    void ReleaseMutationEpoch(object reservation);
    bool TryValidateMutationEpochCapacity();
    bool NotifyCommittedMutation(string sectionId);
}

/// <summary>
/// P12 Crime/Justice census adapter backed by the sealed Core owner registry.
/// It is used by detached Core hosts that have no Unity census mirror. Core
/// keeps the same operation coordinator, section identities, and mutation
/// epoch; unsupported or stale witnesses fail closed.
/// </summary>
internal sealed class DailyV1CoreSelectedOwnerSectionMutationProtocol
    : IDailyV1P12CrimeJusticeMutationProtocol,
      IDailyV1P12CrimeJusticeReservedBaselineProtocol,
      IDailyV1P12CrimeJusticeMutationDiagnosticProtocol
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;

    internal DailyV1CoreSelectedOwnerSectionMutationProtocol(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        this.executionState = executionState ?? throw new ArgumentNullException(nameof(executionState));
        this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    internal bool IsBoundTo(
        DailyV1ExecutionBoundaryState candidateState,
        DailyV1SelectedRuntimeOwnerGraph candidateGraph) =>
        ReferenceEquals(executionState, candidateState)
        && ReferenceEquals(graph, candidateGraph)
        && executionState.HasAdmittedSelectedDailyExecution
        && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph);

    public void FaultClosed()
    {
        executionState.MarkSelectedRuntimeCensusProtocolFaulted();
        executionState.InvalidateCurrentCaptureToken();
    }

    public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds) =>
        TryValidateUnchangedSections(sectionIds, out _);

    public bool TryValidateUnchangedSections(
        IReadOnlyList<string> sectionIds,
        out string failure)
    {
        failure = null;
        if (!IsBoundTo(executionState, graph))
        {
            failure = "CoreProtocolUnbound";
            return false;
        }
        return executionState.TryValidateCoreSelectedOwnerSectionCensusRows(
            graph, sectionIds, null, out failure);
    }
    public bool TryValidateUnchangedSectionsDuringDailyReservation(
        object reservationToken,
        IReadOnlyList<string> sectionIds,
        out string failure)
    {
        failure = null;
        DailyV1MutationEpochReservation reservation =
            executionState.MutationEpochState.ActiveReservation;
        if (!IsBoundTo(executionState, graph)
            || reservation == null
            || !ReferenceEquals(reservation.Token, reservationToken)
            || !executionState.MutationEpochState.Owns(reservation)
            || !executionState.TryValidateCoreSelectedOwnerSectionCensusRows(
                graph, sectionIds, reservation))
        {
            failure = "CoreDailyReservationOrOwnerBaseline";
            return false;
        }
        return true;
    }

    public bool TryRefreshCommittedMutationBaselines(IReadOnlyCollection<string> sectionIds) =>
        IsBoundTo(executionState, graph)
        && executionState.TryRefreshCoreSelectedOwnerSectionCensusRows(graph, sectionIds, null);

    public bool TryRefreshCommittedMutationBaselinesForReservation(
        object reservationToken,
        IReadOnlyCollection<string> sectionIds)
    {
        DailyV1MutationEpochReservation reservation = executionState.MutationEpochState.ActiveReservation;
        return IsBoundTo(executionState, graph)
            && reservation != null
            && ReferenceEquals(reservation.Token, reservationToken)
            && executionState.MutationEpochState.Owns(reservation)
            && executionState.TryRefreshCoreSelectedOwnerSectionCensusRows(graph, sectionIds, reservation);
    }

    public bool TryReserveMutationEpoch(out object reservation)
    {
        reservation = null;
        if (!IsBoundTo(executionState, graph)
            || !executionState.MutationEpochState.TryReserve(
                out DailyV1MutationEpochReservation coreReservation))
            return false;
        reservation = coreReservation;
        return true;
    }

    public bool TryNotifyReservedCommittedMutations(
        object reservation,
        IReadOnlyCollection<string> sectionIds)
    {
        if (!(reservation is DailyV1MutationEpochReservation coreReservation)
            || !executionState.MutationEpochState.Owns(coreReservation)
            || !IsBoundTo(executionState, graph)
            || !executionState.TryRefreshCoreSelectedOwnerSectionCensusRows(
                graph, sectionIds, coreReservation))
            return false;
        return executionState.MutationEpochState.TryComplete(coreReservation, true);
    }

    public void ReleaseMutationEpoch(object reservation)
    {
        if (reservation is DailyV1MutationEpochReservation coreReservation
            && executionState.MutationEpochState.Owns(coreReservation))
            executionState.MutationEpochState.TryRelease(coreReservation);
    }

    public bool TryValidateMutationEpochCapacity() =>
        IsBoundTo(executionState, graph)
        && !executionState.MutationEpochState.HasActiveReservation
        && executionState.MutationEpochState.CanAdvance;

    public bool NotifyCommittedMutation(string sectionId)
    {
        if (string.IsNullOrWhiteSpace(sectionId)
            || !IsBoundTo(executionState, graph)
            || !executionState.TryRefreshCoreSelectedOwnerSectionCensusRows(
                graph, new[] { sectionId }, null))
            return false;
        return executionState.MutationEpochState.TryAdvanceCommittedMutation();
    }
}

/// <summary>
/// Core-owned P12 boundary for the exact CrimeSocialAppraisal owners already
/// admitted by the selected runtime census. It preserves the bounded
/// TheftAcceptance write budget and its single nested knowledge operation.
/// </summary>
internal sealed class DailyV1CoreCrimeSocialAppraisalMutationBoundary
    : IP12CrimeSocialAppraisalOwnerMutationBoundary,
      IP12CrimeSocialAppraisalOperationBoundary
{
    private enum TheftStage : byte { None = 0, BeforeKnowledge = 1, Knowledge = 2, AfterKnowledge = 3 }

    private sealed class Owner
    {
        internal object Value;
        internal string Section;
        internal Func<long> Revision;
    }

    private sealed class Context
    {
        internal P12CrimeSocialAppraisalOperation Operation;
        internal Thread Thread;
        internal int ThreadId;
        internal TheftStage Stage;
        internal bool NestedKnowledgeUsed;
        internal DailyV1MutationEpochReservation Reservation;
        internal readonly Dictionary<object, int> Remaining =
            new Dictionary<object, int>(ReferenceIdentityComparer.Instance);
        internal readonly Dictionary<object, long> Revisions =
            new Dictionary<object, long>(ReferenceIdentityComparer.Instance);
        internal readonly HashSet<string> Changed = new HashSet<string>(StringComparer.Ordinal);
        internal bool IsCurrentThread => ReferenceEquals(Thread, System.Threading.Thread.CurrentThread)
            && ThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceIdentityComparer Instance = new ReferenceIdentityComparer();
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }

    private readonly DailyV1ExecutionBoundaryState state;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly CrimeSocialAppraisalWorldState world;
    private readonly DailyV1CoreSelectedOwnerSectionMutationProtocol protocol;
    private readonly Owner[] owners;
    private Context active;

    private DailyV1CoreCrimeSocialAppraisalMutationBoundary(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph,
        CrimeSocialAppraisalWorldState world,
        DailyV1CoreSelectedOwnerSectionMutationProtocol protocol)
    {
        this.state = state;
        this.graph = graph;
        this.world = world;
        this.protocol = protocol;
        owners = new[]
        {
            new Owner { Value = world.TheftOutcomes, Section = P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId,
                Revision = () => world.TheftOutcomes.P12CensusRevision },
            new Owner { Value = world.CrimeKnowledge, Section = P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId,
                Revision = () => world.CrimeKnowledge.P12CensusRevision },
            new Owner { Value = world.SocialReactions, Section = P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId,
                Revision = () => world.SocialReactions.P12CensusRevision }
        };
    }

    internal static bool TryCreateAndBind(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1CoreCrimeSocialAppraisalMutationBoundary boundary)
    {
        boundary = null;
        CrimeSocialAppraisalWorldState world = graph?.P12EOwners?.CrimeSocialAppraisal;
        if (state == null || graph == null || world == null
            || !state.HasAdmittedSelectedDailyExecution
            || state.AdmissionContext?.IsOwnedByCurrentThread() != true
            || !ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
            || !ReferenceEquals(world.PersonStore, graph.PersonStore)
            || !ReferenceEquals(world.InstitutionStore, graph.P12EOwners.Institutions)
            || !ReferenceEquals(world.SimulationTime, state.SimulationTime)
            || !state.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                graph, out IDailyV1P12CrimeJusticeMutationProtocol p12Protocol, out _)
            || !(p12Protocol is DailyV1CoreSelectedOwnerSectionMutationProtocol coreProtocol))
            return false;

        DailyV1CoreCrimeSocialAppraisalMutationBoundary candidate =
            new DailyV1CoreCrimeSocialAppraisalMutationBoundary(state, graph, world, coreProtocol);
        foreach (Owner owner in candidate.owners)
            if (!state.TryGetSelectedOwnerSectionCensusBaselineOwnerIdentity(
                    owner.Section, graph, out object censusOwner)
                || !ReferenceEquals(owner.Value, censusOwner))
                return false;

        // Do not replace a different host's callbacks on these same owners.
        if (!world.TheftOutcomes.TryBindP12MutationBoundary(candidate)
            || !world.CrimeKnowledge.TryBindP12MutationBoundary(candidate)
            || !world.SocialReactions.TryBindP12MutationBoundary(candidate)
            || !world.Integration.TryBindP12OperationBoundary(candidate))
            return false;
        boundary = candidate;
        return true;
    }

    internal bool IsBoundTo(DailyV1ExecutionBoundaryState candidateState,
        DailyV1SelectedRuntimeOwnerGraph candidateGraph) =>
        ReferenceEquals(state, candidateState) && ReferenceEquals(graph, candidateGraph)
        && ReferenceEquals(graph.P12EOwners?.CrimeSocialAppraisal, world)
        && state.HasAdmittedSelectedDailyExecution
        && ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
        && protocol.IsBoundTo(state, graph);

    public bool TryBeginOperation(P12CrimeSocialAppraisalOperation operation)
    {
        if (!IsCurrent() || !Enum.IsDefined(typeof(P12CrimeSocialAppraisalOperation), operation))
            return false;
        if (active != null)
        {
            if (operation == P12CrimeSocialAppraisalOperation.KnowledgeAndAppraisal
                && active.Operation == P12CrimeSocialAppraisalOperation.TheftAcceptance
                && active.Stage == TheftStage.BeforeKnowledge && !active.NestedKnowledgeUsed
                && active.IsCurrentThread)
            {
                active.NestedKnowledgeUsed = true;
                active.Stage = TheftStage.Knowledge;
                return true;
            }
            Fault();
            return false;
        }

        int[] budgets = operation == P12CrimeSocialAppraisalOperation.TheftAcceptance
            ? new[] { 2, 2, 4 } : new[] { 0, 2, 4 };
        if (!TryOpen(operation, budgets, out Context context)) return false;
        if (operation == P12CrimeSocialAppraisalOperation.TheftAcceptance)
            context.Stage = TheftStage.BeforeKnowledge;
        active = context;
        return true;
    }

    public bool EndOperation(P12CrimeSocialAppraisalOperation operation)
    {
        Context current = active;
        if (current == null || !current.IsCurrentThread) { Fault(); return false; }
        if (current.Operation == P12CrimeSocialAppraisalOperation.TheftAcceptance
            && operation == P12CrimeSocialAppraisalOperation.KnowledgeAndAppraisal
            && current.Stage == TheftStage.Knowledge)
        {
            current.Stage = TheftStage.AfterKnowledge;
            return true;
        }
        if (current.Operation != operation) { Fault(); return false; }
        return Close(current);
    }

    public bool CanCommit(object owner)
    {
        Owner entry = Find(owner);
        Context current = active;
        if (entry == null || current == null || !current.IsCurrentThread
            || !Allowed(current, entry)
            || !current.Remaining.TryGetValue(owner, out int remaining) || remaining <= 0)
        { Fault(); return false; }
        long revision;
        try { revision = entry.Revision(); }
        catch { Fault(); return false; }
        if (revision == long.MaxValue || revision != current.Revisions[owner])
        { Fault(); return false; }
        current.Remaining[owner] = remaining - 1;
        return true;
    }

    public void Committed(object owner)
    {
        Owner entry = Find(owner);
        Context current = active;
        if (entry == null || current == null || !current.IsCurrentThread
            || !current.Revisions.TryGetValue(owner, out long prior)
            || prior == long.MaxValue || entry.Revision() != prior + 1L)
        { Fault(); return; }
        current.Revisions[owner] = prior + 1L;
        current.Changed.Add(entry.Section);
    }

    public void Fault()
    {
        try { protocol?.FaultClosed(); } catch { }
        state?.MarkSelectedRuntimeCensusProtocolFaulted();
        state?.InvalidateCurrentCaptureToken();
    }

    private bool TryOpen(P12CrimeSocialAppraisalOperation operation, int[] budgets, out Context context)
    {
        context = null;
        if (!IsCurrent() || active != null || budgets == null || budgets.Length != owners.Length)
        { Fault(); return false; }
        List<string> sections = new List<string>(owners.Length);
        for (int i = 0; i < owners.Length; i++)
        {
            if (budgets[i] < 0) return false;
            if (budgets[i] == 0) continue;
            long revision;
            try { revision = owners[i].Revision(); } catch { return false; }
            if (revision < 0L || revision > long.MaxValue - budgets[i]) return false;
            sections.Add(owners[i].Section);
        }
        if (sections.Count == 0 || !protocol.TryValidateUnchangedSections(sections)
            || !state.MutationEpochState.TryReserve(out DailyV1MutationEpochReservation reservation))
            return false;

        context = new Context
        {
            Operation = operation,
            Thread = System.Threading.Thread.CurrentThread,
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
            Reservation = reservation
        };
        for (int i = 0; i < owners.Length; i++)
        {
            if (budgets[i] == 0) continue;
            context.Remaining.Add(owners[i].Value, budgets[i]);
            context.Revisions.Add(owners[i].Value, owners[i].Revision());
        }
        return true;
    }

    private bool Allowed(Context context, Owner entry)
    {
        if (context.Operation == P12CrimeSocialAppraisalOperation.KnowledgeAndAppraisal)
            return !ReferenceEquals(entry.Value, world.TheftOutcomes);
        if (context.Operation != P12CrimeSocialAppraisalOperation.TheftAcceptance) return false;
        if (context.Stage == TheftStage.BeforeKnowledge || context.Stage == TheftStage.AfterKnowledge)
            return ReferenceEquals(entry.Value, world.TheftOutcomes);
        return context.Stage == TheftStage.Knowledge
            && !ReferenceEquals(entry.Value, world.TheftOutcomes);
    }

    private bool Close(Context context)
    {
        if (!ReferenceEquals(active, context) || !context.IsCurrentThread
            || context.Reservation == null || !state.MutationEpochState.Owns(context.Reservation))
        { Fault(); return false; }
        active = null;
        bool completed;
        if (context.Changed.Count == 0)
            completed = state.MutationEpochState.TryRelease(context.Reservation);
        else
        {
            List<string> changed = new List<string>(context.Changed);
            changed.Sort(StringComparer.Ordinal);
            completed = protocol.TryRefreshCommittedMutationBaselinesForReservation(
                    context.Reservation.Token, changed)
                && state.MutationEpochState.TryComplete(context.Reservation, true);
        }
        if (!completed) Fault();
        return completed;
    }

    private Owner Find(object owner)
    {
        foreach (Owner candidate in owners)
            if (ReferenceEquals(candidate.Value, owner)) return candidate;
        return null;
    }

    private bool IsCurrent() => state != null && graph != null && world != null
        && state.HasAdmittedSelectedDailyExecution
        && state.AdmissionContext?.IsOwnedByCurrentThread() == true
        && ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
        && ReferenceEquals(graph.P12EOwners?.CrimeSocialAppraisal, world)
        && protocol.IsBoundTo(state, graph);
}

/// <summary>
/// Executes the existing shared transfer through the selected runtime's exact
/// two MoneyAccount census rows. Every actual debit/credit refreshes its own
/// baseline and advances the shared epoch, matching the established owner
/// callback contract.
/// </summary>
internal sealed class DailyV1CoreNpcMoneyTransferMutationBoundary
{
    private sealed class AccountBinding
    {
        internal DailyV1NpcRuntimeOwners Npc;
        internal MoneyAccountRuntime Account;
        internal string SectionId;
        internal long ExpectedRevision;
        internal int RemainingWrites;
        internal Func<bool> Admission;
        internal Action Committed;
    }

    private readonly DailyV1ExecutionBoundaryState state;
    private readonly DailyV1SelectedRuntimeOwnerGraph graph;
    private readonly DailyV1CoreSelectedOwnerSectionMutationProtocol protocol;
    private readonly AccountBinding source;
    private readonly AccountBinding destination;
    private bool active;

    private DailyV1CoreNpcMoneyTransferMutationBoundary(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1CoreSelectedOwnerSectionMutationProtocol protocol,
        DailyV1NpcRuntimeOwners source,
        DailyV1NpcRuntimeOwners destination)
    {
        this.state = state;
        this.graph = graph;
        this.protocol = protocol;
        this.source = CreateBinding(source, 2);
        this.destination = CreateBinding(destination, 1);
        this.source.Admission = () => CanCommit(this.source);
        this.source.Committed = () => NotifyCommitted(this.source);
        this.destination.Admission = () => CanCommit(this.destination);
        this.destination.Committed = () => NotifyCommitted(this.destination);
    }

    internal static EconomyTransactionResult Execute(
        DailyV1ExecutionBoundaryState state,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners source,
        DailyV1NpcRuntimeOwners destination,
        Func<EconomyTransactionResult> operation)
    {
        if (operation == null || !IsExactNpc(graph, source) || !IsExactNpc(graph, destination)
            || ReferenceEquals(source, destination) || source.MoneyAccount == null
            || destination.MoneyAccount == null
            || ReferenceEquals(source.MoneyAccount, destination.MoneyAccount))
            return Failure(source, destination, EconomyTransactionFailureReason.InvalidInput);

        if (state == null || !state.HasAdmittedSelectedDailyExecution
            || state.AdmissionContext?.IsOwnedByCurrentThread() != true
            || !ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
            || !state.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                graph, out IDailyV1P12CrimeJusticeMutationProtocol p12Protocol, out _)
            || !(p12Protocol is DailyV1CoreSelectedOwnerSectionMutationProtocol protocol))
            return Failure(source, destination, EconomyTransactionFailureReason.TransactionCommitFailed);

        string sourceSection = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix
            + source.RuntimeId;
        string destinationSection = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix
            + destination.RuntimeId;
        if (!state.TryGetSelectedOwnerSectionCensusBaselineOwnerIdentity(
                sourceSection, graph, out object sourceIdentity)
            || !ReferenceEquals(sourceIdentity, source.MoneyAccount)
            || !state.TryGetSelectedOwnerSectionCensusBaselineOwnerIdentity(
                destinationSection, graph, out object destinationIdentity)
            || !ReferenceEquals(destinationIdentity, destination.MoneyAccount)
            || MoneyAccountOwnerBridge.HasP12MutationBoundary(source.MoneyAccount)
            || MoneyAccountOwnerBridge.HasP12MutationBoundary(destination.MoneyAccount)
            || state.MutationEpochState.CurrentEpoch > long.MaxValue - 2L
            || !protocol.TryValidateUnchangedSections(new[] { sourceSection, destinationSection })
            || !state.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.NpcMoneyTransfer))
            return Failure(source, destination, EconomyTransactionFailureReason.TransactionCommitFailed);

        DailyV1CoreNpcMoneyTransferMutationBoundary boundary =
            new DailyV1CoreNpcMoneyTransferMutationBoundary(state, graph, protocol, source, destination);
        bool sourceBound = false;
        bool destinationBound = false;
        try
        {
            MoneyAccountOwnerBridge.BindP12MutationBoundary(
                source.MoneyAccount, boundary.source.Admission, boundary.source.Committed);
            sourceBound = true;
            MoneyAccountOwnerBridge.BindP12MutationBoundary(
                destination.MoneyAccount, boundary.destination.Admission, boundary.destination.Committed);
            destinationBound = true;
            boundary.active = true;
            return operation();
        }
        catch
        {
            boundary.Fault();
            throw;
        }
        finally
        {
            boundary.active = false;
            bool unbound = true;
            if (destinationBound)
                unbound &= MoneyAccountOwnerBridge.UnbindP12MutationBoundary(
                    destination.MoneyAccount, boundary.destination.Admission,
                    boundary.destination.Committed);
            if (sourceBound)
                unbound &= MoneyAccountOwnerBridge.UnbindP12MutationBoundary(
                    source.MoneyAccount, boundary.source.Admission,
                    boundary.source.Committed);
            bool ended = state.TryEndRuntimeProtocolOperation();
            if (!unbound || !ended) boundary.Fault();
        }
    }

    private AccountBinding CreateBinding(DailyV1NpcRuntimeOwners npc, int writes) => new AccountBinding
    {
        Npc = npc,
        Account = npc.MoneyAccount,
        SectionId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + npc.RuntimeId,
        ExpectedRevision = npc.MoneyAccount.Revision,
        RemainingWrites = writes
    };

    private bool CanCommit(AccountBinding binding)
    {
        if (!active || !IsCurrent() || binding == null || binding.RemainingWrites <= 0
            || !IsExactNpc(graph, binding.Npc)
            || !ReferenceEquals(binding.Npc.MoneyAccount, binding.Account)
            || binding.Account.Revision != binding.ExpectedRevision
            || binding.Account.Revision == long.MaxValue
            || !protocol.TryValidateUnchangedSections(new[] { binding.SectionId }))
        {
            Fault();
            return false;
        }
        binding.RemainingWrites--;
        return true;
    }

    private void NotifyCommitted(AccountBinding binding)
    {
        if (!active || !IsCurrent() || binding == null
            || binding.Account.Revision != binding.ExpectedRevision + 1L
            || !protocol.NotifyCommittedMutation(binding.SectionId))
        {
            Fault();
            return;
        }
        binding.ExpectedRevision++;
    }

    private bool IsCurrent() => state != null && graph != null
        && state.HasAdmittedSelectedDailyExecution
        && state.AdmissionContext?.IsOwnedByCurrentThread() == true
        && ReferenceEquals(state.SelectedRuntimeOwnerGraph, graph)
        && protocol.IsBoundTo(state, graph);

    private void Fault()
    {
        try { protocol?.FaultClosed(); } catch { }
        state?.MarkSelectedRuntimeCensusProtocolFaulted();
        state?.InvalidateCurrentCaptureToken();
    }

    private static bool IsExactNpc(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners owner) => graph != null && owner != null
        && graph.ContainsExactNpcOwner(owner)
        && ReferenceEquals(owner.Definitions, graph.Definitions);

    private static EconomyTransactionResult Failure(
        DailyV1NpcRuntimeOwners source,
        DailyV1NpcRuntimeOwners destination,
        EconomyTransactionFailureReason reason) =>
        EconomyTransactionResult.CreateFailure(EconomyTransactionType.MoneyTransfer,
            MoneyEffect.Transfer, reason, source?.RuntimeId, destination?.RuntimeId);
}

/// <summary>Optional failure detail for the selected host's P12 baseline adapter.</summary>
internal interface IDailyV1P12CrimeJusticeMutationDiagnosticProtocol
{
    bool TryValidateUnchangedSections(
        IReadOnlyList<string> sectionIds,
        out string failure);
}

/// <summary>
/// Optional Core-only baseline read for an unchanged owner row while the exact
/// daily epoch reservation is held. The reservation token proves that the
/// caller is still inside the operation which validated the complete section
/// set before acquiring its commit window.
/// </summary>
internal interface IDailyV1P12CrimeJusticeReservedBaselineProtocol
{
    bool TryValidateUnchangedSectionsDuringDailyReservation(
        object reservationToken,
        IReadOnlyList<string> sectionIds,
        out string failure);
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
        internal DailyV1MutationEpochReservation CoreEpochReservation;
        internal DailyV1ExecutionBoundaryState ExecutionState;
        internal object CoreOperationScopeToken;

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
    private readonly DailyV1MutationEpochState mutationEpochState;
    private readonly DailyV1ExecutionBoundaryState executionState;
    private Context activeContext;
    private string lastCanCommitRejection;

    internal string LastCanCommitRejection => lastCanCommitRejection;

    internal DailyV1P12CrimeJusticeMutationCoordinator(
        Func<bool> isAdmissionEnabled,
        Func<bool> isOwnerThreadCurrent,
        Action faultAdmission,
        IDailyV1P12CrimeJusticeMutationProtocol protocol,
        DailyV1MutationEpochState mutationEpochState = null,
        DailyV1ExecutionBoundaryState executionState = null)
    {
        this.isAdmissionEnabled = isAdmissionEnabled ?? throw new ArgumentNullException(nameof(isAdmissionEnabled));
        this.isOwnerThreadCurrent = isOwnerThreadCurrent ?? throw new ArgumentNullException(nameof(isOwnerThreadCurrent));
        this.faultAdmission = faultAdmission ?? throw new ArgumentNullException(nameof(faultAdmission));
        this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        this.mutationEpochState = mutationEpochState;
        this.executionState = executionState;
    }

    internal bool IsBoundToProtocol(IDailyV1P12CrimeJusticeMutationProtocol candidate) =>
        ReferenceEquals(protocol, candidate);

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
            || sectionIds.Count == 0
            || (executionState != null
                && !ReferenceEquals(executionState.MutationEpochState, mutationEpochState)))
        {
            Reject("P12 Crime/Justice daily mutation scope is unavailable.", executionState);
            return;
        }

        if (!protocol.TryValidateUnchangedSections(sectionIds))
        {
            Reject("P12 Crime/Justice daily mutation could not validate its owner baseline.", executionState);
            return;
        }

        if (mutationEpochState != null)
        {
            if (!mutationEpochState.TryReserve(out DailyV1MutationEpochReservation coreReservation))
            {
                Reject("P12 Crime/Justice daily mutation could not reserve the Core mutation epoch.", executionState);
                return;
            }
            context.CoreEpochReservation = coreReservation;
        }
        else if (!protocol.TryReserveMutationEpoch(out object reservation) || reservation == null)
        {
            Reject("P12 Crime/Justice daily mutation could not reserve its owner baseline and epoch.", executionState);
            return;
        }
        else
        {
            context.EpochReservation = reservation;
        }
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

        if (executionState != null
            && !executionState.TryBeginSelectedP12CrimeJusticeMutationOperationScope(
                this, out context.CoreOperationScopeToken))
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

    /// <summary>
    /// Builds the selected Guard-Arrest P12 scope from the exact admitted Core
    /// graph. The host supplies only the opaque census identity used by the
    /// existing Justice provider and whether the selected Crime receipt owner
    /// is composed. Core derives NPC/Justice owner bindings and every section
    /// ID; it does not accept a caller-assembled participant or section list.
    /// </summary>
    internal bool TryBeginSelectedGuardArrestMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners guard,
        bool targetPresent,
        DailyV1NpcRuntimeOwners target,
        object justiceCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        out IDisposable scope)
    {
        scope = null;
        if (!isAdmissionEnabled()) return true;

        if (graph == null || graph.Definitions == null || guard == null
            || justiceCensusOwnerIdentity == null
            || !graph.ContainsExactNpcOwner(guard)
            || !ReferenceEquals(guard.Definitions, graph.Definitions)
            || (targetPresent != (target != null))
            || (targetPresent && (!graph.ContainsExactNpcOwner(target)
                || !ReferenceEquals(target.Definitions, graph.Definitions)))
            || graph.JusticeRecordsOwner == null)
        {
            faultAdmission();
            return false;
        }

        List<DailyV1P12CrimeJusticeOwnerBinding> owners =
            new List<DailyV1P12CrimeJusticeOwnerBinding>(targetPresent ? 3 : 2);
        List<string> sections = new List<string>(targetPresent ? 5 : 4);
        HashSet<DailyV1NpcRuntimeOwners> uniqueNpcs =
            new HashSet<DailyV1NpcRuntimeOwners>(ReferenceIdentityComparer.Instance);

        List<DailyV1NpcRuntimeOwners> participants = !targetPresent
            ? new List<DailyV1NpcRuntimeOwners> { guard }
            : new List<DailyV1NpcRuntimeOwners> { guard, target };
        foreach (DailyV1NpcRuntimeOwners npc in participants)
        {
            if (!uniqueNpcs.Add(npc)) continue;
            DailyV1P12CrimeJusticeOwnerBinding binding =
                DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(npc);
            owners.Add(binding);
            sections.Add(binding.SectionId);
        }

        DailyV1P12CrimeJusticeOwnerBinding justiceBinding =
            DailyV1P12CrimeJusticeOwnerBinding.ForJusticeRecordsOwner(
                justiceCensusOwnerIdentity, graph.JusticeRecordsOwner);
        owners.Add(justiceBinding);
        sections.Add(justiceBinding.SectionId);
        sections.Add(DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId);
        if (includeCrimeReceiptSection)
            sections.Add(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId);

        return TryBeginImmediateMutation(owners, sections, out scope);
    }

    /// <summary>
    /// Builds the existing selected Steal Crime/Justice action scope from its
    /// exact Core participants. Core derives participant, Justice, and receipt
    /// section identities; money-transfer and Crime/Social scopes remain their
    /// existing separate operations.
    /// </summary>
    internal bool TryBeginSelectedStealMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners perpetrator,
        DailyV1NpcRuntimeOwners victim,
        object justiceCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        out IDisposable scope)
    {
        scope = null;
        if (!isAdmissionEnabled()) return true;

        if (graph == null || graph.Definitions == null || perpetrator == null
            || victim == null || justiceCensusOwnerIdentity == null
            || !graph.ContainsExactNpcOwner(perpetrator)
            || !ReferenceEquals(perpetrator.Definitions, graph.Definitions)
            || !graph.ContainsExactNpcOwner(victim)
            || !ReferenceEquals(victim.Definitions, graph.Definitions)
            || graph.JusticeRecordsOwner == null)
        {
            faultAdmission();
            return false;
        }

        List<DailyV1P12CrimeJusticeOwnerBinding> owners =
            new List<DailyV1P12CrimeJusticeOwnerBinding>(3);
        List<string> sections = new List<string>(5);
        HashSet<DailyV1NpcRuntimeOwners> uniqueNpcs =
            new HashSet<DailyV1NpcRuntimeOwners>(ReferenceIdentityComparer.Instance);

        foreach (DailyV1NpcRuntimeOwners npc in new[] { perpetrator, victim })
        {
            if (!uniqueNpcs.Add(npc)) continue;
            DailyV1P12CrimeJusticeOwnerBinding binding =
                DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(npc);
            owners.Add(binding);
            sections.Add(binding.SectionId);
        }

        DailyV1P12CrimeJusticeOwnerBinding justiceBinding =
            DailyV1P12CrimeJusticeOwnerBinding.ForJusticeRecordsOwner(
                justiceCensusOwnerIdentity, graph.JusticeRecordsOwner);
        owners.Add(justiceBinding);
        sections.Add(justiceBinding.SectionId);
        sections.Add(DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId);
        if (includeCrimeReceiptSection)
            sections.Add(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId);

        return TryBeginImmediateMutation(owners, sections, out scope);
    }

    /// <summary>
    /// Builds an ordinary selected Crime action's Justice/receipt operation
    /// from the exact Core actor and the already-pinned host Justice census
    /// identity. Core derives every owner binding and section; the host does
    /// not assemble a mutable section list for this selected operation.
    /// </summary>
    internal bool TryBeginSelectedCrimeActionMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        object justiceCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        out IDisposable scope)
    {
        scope = null;
        if (!isAdmissionEnabled()) return true;
        if (!TryBuildSelectedCrimeActionMutation(
                graph, actor, justiceCensusOwnerIdentity, includeCrimeReceiptSection,
                out List<DailyV1P12CrimeJusticeOwnerBinding> owners,
                out List<string> sections))
            return false;
        return TryBeginImmediateMutation(owners, sections, out scope);
    }

    /// <summary>
    /// Builds the exact selected FleeCity operation. The pending travel-plan
    /// owner joins the actor, Justice and receipt sections because its revision
    /// is the factual leaf committed by this action.
    /// </summary>
    internal bool TryBeginSelectedFleeCityMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        object justiceCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        out IDisposable scope)
    {
        scope = null;
        if (!isAdmissionEnabled()) return true;
        if (graph == null || graph.Definitions == null || actor == null
            || justiceCensusOwnerIdentity == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || actor.TravelPlan == null
            || graph.JusticeRecordsOwner == null)
        {
            faultAdmission();
            return false;
        }

        DailyV1P12CrimeJusticeOwnerBinding actorBinding =
            DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(actor);
        DailyV1P12CrimeJusticeOwnerBinding travelPlanBinding =
            DailyV1P12CrimeJusticeOwnerBinding.ForNpcTravelPlanOwner(actor);
        DailyV1P12CrimeJusticeOwnerBinding justiceBinding =
            DailyV1P12CrimeJusticeOwnerBinding.ForJusticeRecordsOwner(
                justiceCensusOwnerIdentity, graph.JusticeRecordsOwner);
        List<DailyV1P12CrimeJusticeOwnerBinding> owners = new List<DailyV1P12CrimeJusticeOwnerBinding>(3)
        {
            actorBinding,
            travelPlanBinding,
            justiceBinding
        };
        List<string> sections = new List<string>(includeCrimeReceiptSection ? 5 : 4)
        {
            actorBinding.SectionId,
            travelPlanBinding.SectionId,
            justiceBinding.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId
        };
        if (includeCrimeReceiptSection)
            sections.Add(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId);
        return TryBeginImmediateMutation(owners, sections, out scope);
    }

    /// <summary>
    /// Verifies that a forced Escape's nested status write reuses the exact
    /// already-open Crime/Justice operation. It does not create a second scope
    /// and admits no additional owner or section.
    /// </summary>
    internal bool CanReuseSelectedCrimeActionMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        object justiceCensusOwnerIdentity,
        bool includeCrimeReceiptSection)
    {
        if (!isAdmissionEnabled()) return true;
        return TryBuildSelectedCrimeActionMutation(
                graph, actor, justiceCensusOwnerIdentity, includeCrimeReceiptSection,
                out List<DailyV1P12CrimeJusticeOwnerBinding> owners,
                out List<string> sections)
            && CanReuseImmediateMutation(owners, sections);
    }

    private bool TryBuildSelectedCrimeActionMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        object justiceCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        out List<DailyV1P12CrimeJusticeOwnerBinding> owners,
        out List<string> sections)
    {
        owners = null;
        sections = null;
        if (graph == null || graph.Definitions == null || actor == null
            || justiceCensusOwnerIdentity == null
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || graph.JusticeRecordsOwner == null)
        {
            faultAdmission();
            return false;
        }

        DailyV1P12CrimeJusticeOwnerBinding actorBinding =
            DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(actor);
        DailyV1P12CrimeJusticeOwnerBinding justiceBinding =
            DailyV1P12CrimeJusticeOwnerBinding.ForJusticeRecordsOwner(
                justiceCensusOwnerIdentity, graph.JusticeRecordsOwner);
        owners = new List<DailyV1P12CrimeJusticeOwnerBinding>(2)
        {
            actorBinding,
            justiceBinding
        };
        sections = new List<string>(includeCrimeReceiptSection ? 4 : 3)
        {
            actorBinding.SectionId,
            justiceBinding.SectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId
        };
        if (includeCrimeReceiptSection)
            sections.Add(DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId);
        return true;
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
        lastCanCommitRejection = null;
        Context context = activeContext;
        if (!IsAdmissionEnabled(context)) return true;
        if (!TryGetBinding(context, ownerIdentity, sectionId,
                out DailyV1P12CrimeJusticeOwnerBinding binding)
            || !IsOwnerThreadCurrent(context))
        {
            lastCanCommitRejection = "OwnerBindingOrThread";
            Fault(context);
            return false;
        }

        long revision;
        try { revision = binding.ReadRevision(); }
        catch
        {
            lastCanCommitRejection = "RevisionRead";
            Fault(context);
            return false;
        }
        if (revision == long.MaxValue)
        {
            lastCanCommitRejection = "RevisionCapacity";
            Fault(context);
            return false;
        }

        if (context.Mode == ScopeMode.DailyReserved)
        {
            bool ownsReservation = (context.CoreEpochReservation != null
                    && mutationEpochState != null
                    && mutationEpochState.Owns(context.CoreEpochReservation))
                || context.EpochReservation != null;
            if (!ownsReservation)
            {
                lastCanCommitRejection = "DailyEpochReservationLost";
                return false;
            }
            if (context.ChangedSections.Contains(sectionId)) return true;

            bool baselineUnchanged;
            string dailyFailure;
            if (context.CoreEpochReservation != null
                && protocol is IDailyV1P12CrimeJusticeReservedBaselineProtocol reservedProtocol)
            {
                baselineUnchanged = reservedProtocol.TryValidateUnchangedSectionsDuringDailyReservation(
                    context.CoreEpochReservation.Token, new[] { sectionId }, out dailyFailure);
            }
            else
            {
                baselineUnchanged = TryValidateUnchangedSections(
                    new[] { sectionId }, out dailyFailure);
            }
            if (!baselineUnchanged)
            {
                lastCanCommitRejection = "DailyOwnerSectionBaseline:" + dailyFailure;
                return false;
            }
            return true;
        }
        if (context.Mode != ScopeMode.ActionImmediate)
        {
            lastCanCommitRejection = "ScopeMode";
            Fault(context);
            return false;
        }
        if (!TryValidateUnchangedSections(new[] { sectionId }, out string baselineFailure))
        {
            lastCanCommitRejection = "OwnerSectionBaseline:" + baselineFailure;
            Fault(context);
            return false;
        }
        if (mutationEpochState != null)
        {
            if (mutationEpochState.HasActiveReservation)
            {
                lastCanCommitRejection = "MutationEpochReserved";
                Fault(context);
                return false;
            }
            if (!mutationEpochState.CanAdvance)
            {
                lastCanCommitRejection = "MutationEpochCapacity";
                Fault(context);
                return false;
            }
        }
        else if (!protocol.TryValidateMutationEpochCapacity())
        {
            lastCanCommitRejection = "ProtocolEpochCapacity";
            Fault(context);
            return false;
        }
        return true;
    }

    private bool TryValidateUnchangedSections(
        IReadOnlyList<string> sectionIds,
        out string failure)
    {
        if (protocol is IDailyV1P12CrimeJusticeMutationDiagnosticProtocol diagnosticProtocol)
            return diagnosticProtocol.TryValidateUnchangedSections(sectionIds, out failure);

        bool valid = protocol.TryValidateUnchangedSections(sectionIds);
        failure = valid ? null : "UnspecifiedProtocolFailure";
        return valid;
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
        bool notified;
        if (context.Mode != ScopeMode.ActionImmediate)
        {
            Fault(context);
            return;
        }

        try
        {
            if (mutationEpochState != null)
            {
                notified = protocol.TryRefreshCommittedMutationBaselines(new[] { sectionId })
                    && mutationEpochState.TryAdvanceCommittedMutation();
            }
            else
            {
                notified = protocol.NotifyCommittedMutation(sectionId);
            }
        }
        catch { notified = false; }
        if (!notified) Fault(context);
    }

    private bool EndDailyScope(Context context)
    {
        bool coreReservation = context?.CoreEpochReservation != null;
        bool valid = context != null
            && context.Mode == ScopeMode.DailyReserved
            && IsOwnerThreadCurrent(context)
            && ReferenceEquals(activeContext, context)
            && (coreReservation
                ? mutationEpochState != null && mutationEpochState.Owns(context.CoreEpochReservation)
                : context.EpochReservation != null);
        if (!valid)
        {
            Fault(context);
            if (context != null && context.CoreEpochReservation != null)
            {
                if (!TryReleaseCore(mutationEpochState, context.CoreEpochReservation))
                    Fault(context);
            }
            else if (context != null && context.EpochReservation != null)
            {
                TryRelease(context.EpochReservation);
            }
            if (ReferenceEquals(activeContext, context)) activeContext = null;
            return false;
        }

        bool notified;
        if (context.CoreEpochReservation != null)
        {
            if (context.ChangedSections.Count == 0)
            {
                notified = TryReleaseCore(mutationEpochState, context.CoreEpochReservation);
            }
            else
            {
                bool refreshed = false;
                try
                {
                    refreshed = protocol.TryRefreshCommittedMutationBaselinesForReservation(
                        context.CoreEpochReservation.Token,
                        ToOrderedSectionIds(context.ChangedSections));
                }
                catch { }

                if (!refreshed)
                {
                    bool released = TryReleaseCore(mutationEpochState, context.CoreEpochReservation);
                    notified = false;
                    if (!released) Fault(context);
                }
                else
                {
                    notified = mutationEpochState.TryComplete(context.CoreEpochReservation, true);
                }
            }
        }
        else if (context.ChangedSections.Count == 0)
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
        if (!notified)
        {
            if (context.CoreEpochReservation != null
                && mutationEpochState != null
                && mutationEpochState.Owns(context.CoreEpochReservation))
                TryReleaseCore(mutationEpochState, context.CoreEpochReservation);
            Fault(context);
        }
        activeContext = null;
        return notified;
    }

    private static string[] ToOrderedSectionIds(HashSet<string> sectionIds)
    {
        if (sectionIds == null || sectionIds.Count == 0) return Array.Empty<string>();
        string[] ordered = new string[sectionIds.Count];
        sectionIds.CopyTo(ordered);
        Array.Sort(ordered, StringComparer.Ordinal);
        return ordered;
    }

    private static bool TryReleaseCore(
        DailyV1MutationEpochState state,
        DailyV1MutationEpochReservation reservation)
    {
        try { return state != null && state.TryRelease(reservation); }
        catch { return false; }
    }

    private void EndImmediateScope(Context context)
    {
        if (context == null) return;
        if (context.Mode != ScopeMode.ActionImmediate
            || !context.IsOwnedByCurrentThread()
            || !ReferenceEquals(activeContext, context))
        {
            faultAdmission();
        }
        object coreScopeToken = context.CoreOperationScopeToken;
        context.CoreOperationScopeToken = null;
        if (coreScopeToken != null
            && (executionState == null
                || !executionState.TryEndSelectedP12CrimeJusticeMutationOperationScope(
                    this, coreScopeToken)))
        {
            faultAdmission();
        }
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

    internal void FaultSelected(DailyV1ExecutionBoundaryState executionState)
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
