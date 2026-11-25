using System;
using System.Collections.Generic;

/// <summary>Stable selected Daily-v1 directive semantics.</summary>
public enum ScheduledDirectiveMode
{
    RequestAction,
    ForceOutcome
}

public enum ScheduledDirectiveOperation
{
    EscapePrison
}

public enum ScheduledDirectiveState
{
    Pending,
    Succeeded,
    Failed,
    Skipped
}

/// <summary>
/// Core-owned scheduled command state. Action identity is retained as a stable
/// definition ID; Unity authoring objects are resolved only by the host adapter.
/// </summary>
[Serializable]
public sealed class ScheduledDirective : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly string directiveId;
    private readonly long absoluteDay;
    private readonly ScheduledDirectiveMode mode;
    private readonly ScheduledDirectiveOperation operation;
    private readonly string actorRuntimeId;
    private readonly string actionDefinitionId;
    private ScheduledDirectiveState state;
    private long processedDay = -1L;
    private string resultReason;
    [NonSerialized] private ScheduledDirectiveStore ownerStore;

    public string DirectiveId => directiveId;
    public long AbsoluteDay => absoluteDay;
    public ScheduledDirectiveMode Mode => mode;
    public ScheduledDirectiveOperation Operation => operation;
    public string ActorRuntimeId => actorRuntimeId;
    public string ActionDefinitionId => actionDefinitionId;
    public ScheduledDirectiveState State => state;
    public long ProcessedDay => processedDay;
    public string ResultReason => resultReason;
    public bool IsPending => state == ScheduledDirectiveState.Pending;

    public ScheduledDirective(
        string directiveId,
        long absoluteDay,
        ScheduledDirectiveMode mode,
        ScheduledDirectiveOperation operation,
        string actorRuntimeId,
        string actionDefinitionId,
        int actionTypeValue)
    {
        if (string.IsNullOrWhiteSpace(directiveId))
            throw new ArgumentException("ScheduledDirective requires a non-empty DirectiveId.", nameof(directiveId));
        if (absoluteDay < 0L)
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), "ScheduledDirective AbsoluteDay cannot be negative.");
        if (mode != ScheduledDirectiveMode.RequestAction && mode != ScheduledDirectiveMode.ForceOutcome)
            throw new ArgumentOutOfRangeException(nameof(mode), "ScheduledDirective mode is not supported.");
        if (operation != ScheduledDirectiveOperation.EscapePrison)
            throw new ArgumentOutOfRangeException(nameof(operation), "ScheduledDirective operation is not supported.");
        if (string.IsNullOrWhiteSpace(actorRuntimeId))
            throw new ArgumentException("ScheduledDirective requires a non-empty ActorRuntimeId.", nameof(actorRuntimeId));
        if (string.IsNullOrWhiteSpace(actionDefinitionId)
            || actionTypeValue != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)
            throw new ArgumentException("EscapePrison directive requires a matching action DefinitionId and type.", nameof(actionDefinitionId));

        this.directiveId = directiveId;
        this.absoluteDay = absoluteDay;
        this.mode = mode;
        this.operation = operation;
        this.actorRuntimeId = actorRuntimeId;
        this.actionDefinitionId = actionDefinitionId;
        state = ScheduledDirectiveState.Pending;
    }

    public bool MarkSucceeded(long currentDay) => MarkProcessed(ScheduledDirectiveState.Succeeded, currentDay, string.Empty);
    public bool MarkFailed(long currentDay, string reason) => MarkProcessed(ScheduledDirectiveState.Failed, currentDay, reason);
    public bool MarkSkipped(long currentDay, string reason) => MarkProcessed(ScheduledDirectiveState.Skipped, currentDay, reason);

    private bool MarkProcessed(ScheduledDirectiveState finalState, long currentDay, string reason)
    {
        if (ownerStore != null) return ownerStore.TryMarkProcessed(this, finalState, currentDay, reason);
        return ApplyProcessedState(finalState, currentDay, reason);
    }

    internal bool ApplyProcessedState(ScheduledDirectiveState finalState, long currentDay, string reason)
    {
        if (!mutationGuardBinding.CanMutate || state != ScheduledDirectiveState.Pending
            || currentDay < 0L || finalState == ScheduledDirectiveState.Pending
            || !Enum.IsDefined(typeof(ScheduledDirectiveState), finalState)) return false;
        state = finalState;
        processedDay = currentDay;
        resultReason = reason ?? string.Empty;
        return true;
    }

    internal bool RestoreP12FState(ScheduledDirectiveState restoredState, long restoredDay, string restoredReason)
    {
        if (!Enum.IsDefined(typeof(ScheduledDirectiveState), restoredState)) return false;
        if (restoredState == ScheduledDirectiveState.Pending)
        {
            if (restoredDay != -1L || restoredReason != null) return false;
        }
        else if (restoredDay < 0L || restoredReason == null) return false;
        state = restoredState;
        processedDay = restoredDay;
        resultReason = restoredReason;
        return true;
    }

    internal bool CanBindOwner(ScheduledDirectiveStore owner) => owner != null && (ownerStore == null || ReferenceEquals(ownerStore, owner));
    internal bool TryBindOwner(ScheduledDirectiveStore owner)
    {
        if (!CanBindOwner(owner)) return false;
        ownerStore = owner;
        return true;
    }
    internal bool IsOwnedBy(ScheduledDirectiveStore owner) => owner != null && ReferenceEquals(ownerStore, owner);
    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

/// <summary>
/// Core owner for the selected P12F directive section. Add/terminal transitions
/// advance its revision once and invoke the same P12 commit boundary as before.
/// </summary>
public sealed class ScheduledDirectiveStore : IAuthoritativeMutationGuardBindable
{
    internal const string SectionId = "p12f.scheduled-directives";
    internal const int SchemaVersion = 1;

    private readonly object ownerMonitor = new object();
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly List<ScheduledDirective> directives = new List<ScheduledDirective>();
    private readonly IReadOnlyList<ScheduledDirective> readOnlyDirectives;
    private readonly Dictionary<string, ScheduledDirective> directivesById = new Dictionary<string, ScheduledDirective>(StringComparer.Ordinal);
    private readonly Dictionary<string, ScheduledDirective> directivesByActorForCurrentDay =
        new Dictionary<string, ScheduledDirective>(StringComparer.Ordinal);
    private readonly SimulationTime simulationTime;
    private SimulationLogger logger;
    private long revision;
    private long lastPreparedAbsoluteDay = -1L;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;

    public IReadOnlyList<ScheduledDirective> Directives => readOnlyDirectives;
    public long Revision { get { lock (ownerMonitor) return revision; } }

    public ScheduledDirectiveStore(SimulationTime simulationTime, SimulationLogger logger = null)
    {
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.logger = logger;
        readOnlyDirectives = directives.AsReadOnly();
    }

    internal void BindDiagnosticLogger(SimulationLogger diagnosticLogger)
    {
        if (diagnosticLogger == null) return;
        lock (ownerMonitor)
            if (logger == null) logger = diagnosticLogger;
    }

    public bool Add(ScheduledDirective directive)
    {
        lock (ownerMonitor)
        {
            if (!mutationGuardBinding.CanMutate || directive == null
                || (mutationGuardBinding.BoundGuard != null && !directive.CanBindMutationGuard(mutationGuardBinding.BoundGuard))
                || !directive.CanBindOwner(this))
            {
                if (mutationGuardBinding.CanMutate)
                    logger?.LogError(directive == null
                        ? "Cannot add scheduled directive: directive is null."
                        : "Cannot add scheduled directive: directive is owned by another runtime.");
                return false;
            }
            if (directivesById.ContainsKey(directive.DirectiveId))
            {
                logger?.LogError($"Cannot add duplicate DirectiveId '{directive.DirectiveId}'.");
                return false;
            }
            if (revision == long.MaxValue) return false;

            RequireP12MutationAdmission();
            if (mutationGuardBinding.BoundGuard != null && !directive.TryBindMutationGuard(mutationGuardBinding.BoundGuard)) return false;
            if (!directive.TryBindOwner(this)) return false;

            directivesById.Add(directive.DirectiveId, directive);
            directives.Add(directive);
            long currentDay = simulationTime.AbsoluteDay;
            if (directive.AbsoluteDay < 1L)
                SkipInvalidSchedule(directive, currentDay, "AbsoluteDay must be at least 1.");
            else if (directive.AbsoluteDay < currentDay)
                SkipInvalidSchedule(directive, currentDay, $"AbsoluteDay {directive.AbsoluteDay} is earlier than current day {currentDay}.");

            revision++;
            p12MutationCommitted?.Invoke();
            return true;
        }
    }

    internal bool TryMarkProcessed(ScheduledDirective directive, ScheduledDirectiveState finalState, long currentDay, string reason)
    {
        lock (ownerMonitor)
        {
            if (!mutationGuardBinding.CanMutate || directive == null || !directive.IsOwnedBy(this)
                || directive.State != ScheduledDirectiveState.Pending || currentDay < 0L
                || finalState == ScheduledDirectiveState.Pending || !Enum.IsDefined(typeof(ScheduledDirectiveState), finalState)
                || revision == long.MaxValue) return false;
            RequireP12MutationAdmission();
            if (!directive.ApplyProcessedState(finalState, currentDay, reason)) return false;
            revision++;
            p12MutationCommitted?.Invoke();
            return true;
        }
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        lock (ownerMonitor)
        {
            if (p12MutationAdmission != null || p12MutationCommitted != null)
                throw new InvalidOperationException("The ScheduledDirective store already has a P12 mutation boundary.");
            p12MutationAdmission = admission;
            p12MutationCommitted = committed;
        }
    }

    private void RequireP12MutationAdmission()
    {
        if (p12MutationAdmission != null && !p12MutationAdmission())
            throw new InvalidOperationException("The ScheduledDirective mutation was rejected by the selected P12 census protocol.");
    }

    internal OwnerSectionCensusWitness GetCurrentCensus()
    {
        lock (ownerMonitor) return new OwnerSectionCensusWitness(SectionId, SchemaVersion, this, directives.Count, revision);
    }

    internal bool TryCaptureP12FState(out IReadOnlyList<ScheduledDirective> rows, out long capturedRevision)
    {
        lock (ownerMonitor)
        {
            List<ScheduledDirective> copies = new List<ScheduledDirective>(directives.Count);
            foreach (ScheduledDirective source in directives)
            {
                if (source == null) { rows = null; capturedRevision = revision; return false; }
                ScheduledDirective copy;
                try { copy = new ScheduledDirective(source.DirectiveId, source.AbsoluteDay, source.Mode,
                    source.Operation, source.ActorRuntimeId, source.ActionDefinitionId,
                    DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue); }
                catch (ArgumentException) { rows = null; capturedRevision = revision; return false; }
                if (!copy.RestoreP12FState(source.State, source.ProcessedDay, source.ResultReason))
                { rows = null; capturedRevision = revision; return false; }
                copies.Add(copy);
            }
            rows = copies.AsReadOnly();
            capturedRevision = revision;
            return rows.Count == directives.Count && capturedRevision >= 0L;
        }
    }

    internal static bool TryCreateP12FStaged(SimulationTime stagedTime, IReadOnlyList<ScheduledDirective> detachedRows,
        long exactRevision, out ScheduledDirectiveStore staged)
    {
        staged = null;
        if (stagedTime == null || detachedRows == null || exactRevision < detachedRows.Count || exactRevision < 0L) return false;
        ScheduledDirectiveStore candidate = new ScheduledDirectiveStore(stagedTime);
        foreach (ScheduledDirective source in detachedRows)
        {
            if (source == null || candidate.directivesById.ContainsKey(source.DirectiveId)) return false;
            ScheduledDirective copy;
            try { copy = new ScheduledDirective(source.DirectiveId, source.AbsoluteDay, source.Mode,
                source.Operation, source.ActorRuntimeId, source.ActionDefinitionId,
                DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue); }
            catch (ArgumentException) { return false; }
            if (!copy.RestoreP12FState(source.State, source.ProcessedDay, source.ResultReason) || !copy.TryBindOwner(candidate)) return false;
            candidate.directivesById.Add(copy.DirectiveId, copy);
            candidate.directives.Add(copy);
        }
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    public List<ScheduledDirective> GetPendingForDay(long absoluteDay)
    {
        List<ScheduledDirective> pending = new List<ScheduledDirective>();
        lock (ownerMonitor)
            foreach (ScheduledDirective directive in directives)
                if (directive != null && directive.IsPending && directive.AbsoluteDay == absoluteDay) pending.Add(directive);
        return pending;
    }

    /// <summary>
    /// Prepares the transient per-actor lookup for one day. Identity existence
    /// is supplied by the composed host; no Unity runtime object is retained.
    /// </summary>
    internal void PrepareDay(long absoluteDay, Func<string, bool> actorIdentityExists,
        SimulationLogger diagnosticLogger = null)
    {
        if (!mutationGuardBinding.CanMutate)
            throw new InvalidOperationException("Runtime mutation is faulted.");
        if (absoluteDay < 1L || lastPreparedAbsoluteDay == absoluteDay) return;

        lastPreparedAbsoluteDay = absoluteDay;
        lock (ownerMonitor) directivesByActorForCurrentDay.Clear();
        List<ScheduledDirective> dueDirectives = GetPendingForDay(absoluteDay);
        Dictionary<string, int> directiveCountsByActor = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ScheduledDirective directive in dueDirectives)
        {
            directiveCountsByActor.TryGetValue(directive.ActorRuntimeId, out int count);
            directiveCountsByActor[directive.ActorRuntimeId] = count + 1;
        }

        SimulationLogger diagnostics = diagnosticLogger ?? logger;
        foreach (ScheduledDirective directive in dueDirectives)
        {
            if (directiveCountsByActor[directive.ActorRuntimeId] > 1)
            {
                string reason = $"Multiple significant directives target actor '{directive.ActorRuntimeId}' on day {absoluteDay}.";
                directive.MarkSkipped(absoluteDay, reason);
                diagnostics?.LogError($"Scheduled directive conflict: '{directive.DirectiveId}' was skipped. {reason}");
                continue;
            }
            if (actorIdentityExists == null || !actorIdentityExists(directive.ActorRuntimeId))
            {
                string reason = $"Actor RuntimeId '{directive.ActorRuntimeId}' could not be resolved.";
                directive.MarkSkipped(absoluteDay, reason);
                diagnostics?.LogWarning($"Scheduled directive '{directive.DirectiveId}' was skipped: {reason}");
                continue;
            }
            lock (ownerMonitor) directivesByActorForCurrentDay.Add(directive.ActorRuntimeId, directive);
        }
    }

    /// <summary>
    /// Prepares selected Daily-v1 directives against the Core-owned identity
    /// authority. Unity's typed object projection is not needed to decide
    /// whether a scheduled actor RuntimeId exists as an NPC.
    /// </summary>
    internal void PrepareSelectedDailyV1Day(
        long absoluteDay,
        DailyV1RuntimeIdentityIndex identityIndex)
    {
        if (identityIndex == null)
            throw new ArgumentNullException(nameof(identityIndex));

        PrepareDay(absoluteDay,
            runtimeId => identityIndex.HasIdentity(runtimeId, DailyV1RuntimeIdentityKind.Npc));
    }

    /// <summary>Consumes only the transient prepared lookup; it does not revise owner state.</summary>
    internal bool TryTakeDirective(string actorRuntimeId, out ScheduledDirective directive)
    {
        directive = null;
        if (!mutationGuardBinding.CanMutate || string.IsNullOrWhiteSpace(actorRuntimeId)) return false;
        lock (ownerMonitor)
        {
            if (!directivesByActorForCurrentDay.TryGetValue(actorRuntimeId, out directive)) return false;
            directivesByActorForCurrentDay.Remove(actorRuntimeId);
            return directive != null && directive.IsPending;
        }
    }

    internal bool HasPendingDirectives
    {
        get
        {
            lock (ownerMonitor)
                foreach (ScheduledDirective directive in directives)
                    if (directive != null && directive.IsPending) return true;
            return false;
        }
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        lock (ownerMonitor)
        {
            if (!mutationGuardBinding.CanBindTo(guard)) return false;
            foreach (ScheduledDirective directive in directives)
                if (directive != null && !directive.CanBindMutationGuard(guard)) return false;
            return true;
        }
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        lock (ownerMonitor)
        {
            if (!CanBindMutationGuard(guard) || !mutationGuardBinding.TryBindTo(guard)) return false;
            foreach (ScheduledDirective directive in directives)
                if (directive != null && !directive.TryBindMutationGuard(guard)) return false;
            return true;
        }
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);

    private void SkipInvalidSchedule(ScheduledDirective directive, long currentDay, string reason)
    {
        directive.ApplyProcessedState(ScheduledDirectiveState.Skipped, currentDay, reason);
        logger?.LogWarning($"Scheduled directive '{directive.DirectiveId}' was skipped: {reason}");
    }
}
