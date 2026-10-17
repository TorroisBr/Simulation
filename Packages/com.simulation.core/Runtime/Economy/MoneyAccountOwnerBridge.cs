using System;

/// <summary>
/// Narrow Unity-assembly access to prepared installation and P12 boundary
/// operations owned by the shared MoneyAccountRuntime implementation.
/// </summary>
public static class MoneyAccountOwnerBridge
{
    public static bool CanInstall(MoneyAccountRuntime owner, long expectedRevision, float replacement)
    {
        return owner != null && owner.CanInstall(expectedRevision, replacement);
    }

    public static bool CanInstall(
        MoneyAccountRuntime owner,
        long expectedRevision,
        long revisionIncrements,
        float replacement)
    {
        return owner != null && owner.CanInstall(expectedRevision, revisionIncrements, replacement);
    }

    public static void InstallPrepared(MoneyAccountRuntime owner, long expectedRevision, float replacement)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        owner.InstallPrepared(expectedRevision, replacement);
    }

    public static void InstallPrepared(
        MoneyAccountRuntime owner,
        long expectedRevision,
        long revisionIncrements,
        float replacement)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        owner.InstallPrepared(expectedRevision, revisionIncrements, replacement);
    }

    public static bool TryCreateFromOwnerSnapshot(
        float exactBalance,
        long exactRevision,
        out MoneyAccountRuntime staged)
    {
        return MoneyAccountRuntime.TryCreateFromOwnerSnapshot(exactBalance, exactRevision, out staged);
    }

    public static void BindP12MutationBoundary(
        MoneyAccountRuntime owner,
        Func<bool> admission,
        Action committed)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        owner.BindP12MutationBoundary(admission, committed);
    }

    public static bool UnbindP12MutationBoundary(
        MoneyAccountRuntime owner,
        Func<bool> admission,
        Action committed)
    {
        return owner != null && owner.UnbindP12MutationBoundary(admission, committed);
    }

    public static bool HasP12MutationBoundary(MoneyAccountRuntime owner)
    {
        return owner != null && owner.HasP12MutationBoundary;
    }
}
