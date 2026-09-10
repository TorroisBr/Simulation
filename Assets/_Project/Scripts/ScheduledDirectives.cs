using System;
using System.Collections.Generic;

[Serializable]
public sealed class ScheduledDirective
{
    private readonly string directiveId;
    private readonly long absoluteDay;
    private readonly ScheduledDirectiveMode mode;
    private readonly ScheduledDirectiveOperation operation;
    private readonly string actorRuntimeId;
    private readonly NpcActionData action;
    private ScheduledDirectiveState state;
    private long processedDay = -1L;
    private string resultReason;

    public string DirectiveId => directiveId;
    public long AbsoluteDay => absoluteDay;
    public ScheduledDirectiveMode Mode => mode;
    public ScheduledDirectiveOperation Operation => operation;
    public string ActorRuntimeId => actorRuntimeId;
    public NpcActionData Action => action;
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
        NpcActionData action)
    {
        if (string.IsNullOrWhiteSpace(directiveId) == true)
        {
            throw new ArgumentException("ScheduledDirective requires a non-empty DirectiveId.", nameof(directiveId));
        }

        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), "ScheduledDirective AbsoluteDay cannot be negative.");
        }

        if (mode != ScheduledDirectiveMode.RequestAction && mode != ScheduledDirectiveMode.ForceOutcome)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), "ScheduledDirective mode is not supported.");
        }

        if (operation != ScheduledDirectiveOperation.EscapePrison)
        {
            throw new ArgumentOutOfRangeException(nameof(operation), "ScheduledDirective operation is not supported.");
        }

        if (string.IsNullOrWhiteSpace(actorRuntimeId) == true)
        {
            throw new ArgumentException("ScheduledDirective requires a non-empty ActorRuntimeId.", nameof(actorRuntimeId));
        }

        if (action == null || action.actionType != NpcActionType.EscapePrison)
        {
            throw new ArgumentException("EscapePrison directive requires a matching NpcActionData.", nameof(action));
        }

        this.directiveId = directiveId;
        this.absoluteDay = absoluteDay;
        this.mode = mode;
        this.operation = operation;
        this.actorRuntimeId = actorRuntimeId;
        this.action = action;
        state = ScheduledDirectiveState.Pending;
    }

    public bool MarkSucceeded(long currentDay)
    {
        return MarkProcessed(ScheduledDirectiveState.Succeeded, currentDay, string.Empty);
    }

    public bool MarkFailed(long currentDay, string reason)
    {
        return MarkProcessed(ScheduledDirectiveState.Failed, currentDay, reason);
    }

    public bool MarkSkipped(long currentDay, string reason)
    {
        return MarkProcessed(ScheduledDirectiveState.Skipped, currentDay, reason);
    }

    private bool MarkProcessed(ScheduledDirectiveState finalState, long currentDay, string reason)
    {
        if (state != ScheduledDirectiveState.Pending || currentDay < 0L || finalState == ScheduledDirectiveState.Pending)
        {
            return false;
        }

        state = finalState;
        processedDay = currentDay;
        resultReason = reason ?? string.Empty;
        return true;
    }
}

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

public sealed class ScheduledDirectiveStore
{
    private readonly List<ScheduledDirective> directives = new List<ScheduledDirective>();
    private readonly IReadOnlyList<ScheduledDirective> readOnlyDirectives;
    private readonly Dictionary<string, ScheduledDirective> directivesById = new Dictionary<string, ScheduledDirective>(StringComparer.Ordinal);
    private readonly SimulationTime simulationTime;
    private readonly SimulationLogger logger;

    public IReadOnlyList<ScheduledDirective> Directives => readOnlyDirectives;

    public ScheduledDirectiveStore(SimulationTime simulationTime, SimulationLogger logger = null)
    {
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.logger = logger ?? new SimulationLogger(null);
        readOnlyDirectives = directives.AsReadOnly();
    }

    public bool Add(ScheduledDirective directive)
    {
        if (directive == null)
        {
            logger.LogError("Cannot add scheduled directive: directive is null.");
            return false;
        }

        if (directivesById.ContainsKey(directive.DirectiveId) == true)
        {
            logger.LogError($"Cannot add duplicate DirectiveId '{directive.DirectiveId}'.");
            return false;
        }

        directivesById.Add(directive.DirectiveId, directive);
        directives.Add(directive);
        long currentDay = simulationTime.AbsoluteDay;

        if (directive.AbsoluteDay < 1L)
        {
            SkipInvalidSchedule(directive, currentDay, "AbsoluteDay must be at least 1.");
        }
        else if (directive.AbsoluteDay < currentDay)
        {
            SkipInvalidSchedule(directive, currentDay, $"AbsoluteDay {directive.AbsoluteDay} is earlier than current day {currentDay}.");
        }

        return true;
    }

    public List<ScheduledDirective> GetPendingForDay(long absoluteDay)
    {
        List<ScheduledDirective> pending = new List<ScheduledDirective>();

        foreach (ScheduledDirective directive in directives)
        {
            if (directive != null && directive.IsPending == true && directive.AbsoluteDay == absoluteDay)
            {
                pending.Add(directive);
            }
        }

        return pending;
    }

    private void SkipInvalidSchedule(ScheduledDirective directive, long currentDay, string reason)
    {
        directive.MarkSkipped(currentDay, reason);
        logger.LogWarning($"Scheduled directive '{directive.DirectiveId}' was skipped: {reason}");
    }
}

public sealed class ScheduledDirectiveSystem
{
    private readonly ScheduledDirectiveStore directiveStore;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly SimulationLogger logger;
    private readonly Dictionary<string, ScheduledDirective> directivesByActorForCurrentDay = new Dictionary<string, ScheduledDirective>(StringComparer.Ordinal);
    private long lastPreparedAbsoluteDay = -1L;

    public ScheduledDirectiveSystem(
        ScheduledDirectiveStore directiveStore,
        RuntimeIdentityRegistry identityRegistry,
        SimulationLogger logger = null)
    {
        this.directiveStore = directiveStore ?? throw new ArgumentNullException(nameof(directiveStore));
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.logger = logger ?? new SimulationLogger(null);
    }

    public void PrepareDay(long absoluteDay)
    {
        if (absoluteDay < 1L || lastPreparedAbsoluteDay == absoluteDay)
        {
            return;
        }

        lastPreparedAbsoluteDay = absoluteDay;
        directivesByActorForCurrentDay.Clear();
        List<ScheduledDirective> dueDirectives = directiveStore.GetPendingForDay(absoluteDay);
        Dictionary<string, int> directiveCountsByActor = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (ScheduledDirective directive in dueDirectives)
        {
            directiveCountsByActor.TryGetValue(directive.ActorRuntimeId, out int count);
            directiveCountsByActor[directive.ActorRuntimeId] = count + 1;
        }

        foreach (ScheduledDirective directive in dueDirectives)
        {
            if (directiveCountsByActor[directive.ActorRuntimeId] > 1)
            {
                string reason = $"Multiple significant directives target actor '{directive.ActorRuntimeId}' on day {absoluteDay}.";
                directive.MarkSkipped(absoluteDay, reason);
                logger.LogError($"Scheduled directive conflict: '{directive.DirectiveId}' was skipped. {reason}");
                continue;
            }

            if (identityRegistry.TryGetNpc(directive.ActorRuntimeId, out NpcRuntime actorRuntime) == false || actorRuntime == null)
            {
                string reason = $"Actor RuntimeId '{directive.ActorRuntimeId}' could not be resolved.";
                directive.MarkSkipped(absoluteDay, reason);
                logger.LogWarning($"Scheduled directive '{directive.DirectiveId}' was skipped: {reason}");
                continue;
            }

            directivesByActorForCurrentDay.Add(directive.ActorRuntimeId, directive);
        }
    }

    public bool TryTakeDirective(NpcRuntime actorRuntime, out ScheduledDirective directive)
    {
        directive = null;

        if (actorRuntime == null || string.IsNullOrWhiteSpace(actorRuntime.RuntimeId) == true)
        {
            return false;
        }

        if (directivesByActorForCurrentDay.TryGetValue(actorRuntime.RuntimeId, out directive) == false)
        {
            return false;
        }

        directivesByActorForCurrentDay.Remove(actorRuntime.RuntimeId);
        return directive != null && directive.IsPending == true;
    }
}
