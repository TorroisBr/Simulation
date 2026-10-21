using System;

/// <summary>Unity identity-resolution adapter for the Core-owned directive lifecycle.</summary>
public sealed class ScheduledDirectiveSystem : IAuthoritativeMutationGuardBindable
{
    private readonly ScheduledDirectiveStore directiveStore;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly SimulationLogger logger;

    internal ScheduledDirectiveStore Store => directiveStore;
    internal bool HasPendingDirectives => directiveStore.HasPendingDirectives;

    public ScheduledDirectiveSystem(
        ScheduledDirectiveStore directiveStore,
        RuntimeIdentityRegistry identityRegistry,
        SimulationLogger logger = null)
    {
        this.directiveStore = directiveStore ?? throw new ArgumentNullException(nameof(directiveStore));
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
        this.directiveStore.BindDiagnosticLogger(this.logger);
    }

    public void PrepareDay(long absoluteDay) => directiveStore.PrepareDay(
        absoluteDay,
        runtimeId => identityRegistry.TryGetNpc(runtimeId, out NpcRuntime actor) && actor != null,
        logger);

    public bool TryTakeDirective(NpcRuntime actorRuntime, out ScheduledDirective directive)
    {
        directive = null;
        return actorRuntime != null
            && directiveStore.TryTakeDirective(actorRuntime.RuntimeId, out directive);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) =>
        directiveStore.CanBindMutationGuard(guard);

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) =>
        directiveStore.TryBindMutationGuard(guard);

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) =>
        CanBindMutationGuard(guard);

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) =>
        TryBindMutationGuard(guard);
}
