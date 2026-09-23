using System;

public enum AuthoritativeMutationHealth
{
    Healthy,
    Faulted
}

public enum AuthoritativeMutationFaultReason
{
    None,
    RollbackRestoreFailed,
    IntegrityRestoreFailed
}

/// <summary>
/// The integrity latch shared by one composed SimulationRuntime and its bound
/// authorities. It is deliberately not a gameplay-facing mutation API.
/// </summary>
internal sealed class AuthoritativeMutationGuard
{
    public AuthoritativeMutationHealth Health { get; private set; }
        = AuthoritativeMutationHealth.Healthy;

    public AuthoritativeMutationFaultReason FaultReason { get; private set; }
        = AuthoritativeMutationFaultReason.None;

    public bool CanMutate => Health == AuthoritativeMutationHealth.Healthy;

    internal void MarkFaulted(AuthoritativeMutationFaultReason reason)
    {
        if (reason == AuthoritativeMutationFaultReason.None
            || Enum.IsDefined(typeof(AuthoritativeMutationFaultReason), reason) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        if (Health == AuthoritativeMutationHealth.Faulted)
        {
            return;
        }

        FaultReason = reason;
        Health = AuthoritativeMutationHealth.Faulted;
    }
}

/// <summary>
/// Narrow one-way binding shared by runtime-owned authorities. An unbound
/// authority remains usable standalone; a bound authority cannot silently
/// change worlds.
/// </summary>
internal sealed class MutationGuardBinding
{
    private AuthoritativeMutationGuard boundGuard;

    internal AuthoritativeMutationGuard BoundGuard => boundGuard;

    internal bool CanMutate => boundGuard == null || boundGuard.CanMutate;

    internal bool CanBindTo(AuthoritativeMutationGuard guard)
    {
        return guard != null
            && (boundGuard == null || ReferenceEquals(boundGuard, guard));
    }

    internal bool TryBindTo(AuthoritativeMutationGuard guard)
    {
        if (CanBindTo(guard) == false)
        {
            return false;
        }

        if (boundGuard == null)
        {
            boundGuard = guard;
        }

        return true;
    }
}

internal interface IAuthoritativeMutationGuardBindable
{
    bool CanBindMutationGuard(AuthoritativeMutationGuard guard);
    bool TryBindMutationGuard(AuthoritativeMutationGuard guard);
}
