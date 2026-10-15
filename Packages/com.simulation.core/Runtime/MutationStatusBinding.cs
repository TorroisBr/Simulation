/// <summary>
/// Narrow one-way binding to the health status of one composed runtime.
/// Unbound owners remain usable standalone; a bound owner cannot change worlds.
/// </summary>
internal sealed class MutationStatusBinding
{
    private IAuthoritativeMutationStatus boundStatus;

    internal IAuthoritativeMutationStatus BoundStatus => boundStatus;

    internal bool CanMutate => boundStatus == null || boundStatus.CanMutate;

    internal bool CanBindTo(IAuthoritativeMutationStatus status)
    {
        return status != null
            && (boundStatus == null || ReferenceEquals(boundStatus, status));
    }

    internal bool TryBindTo(IAuthoritativeMutationStatus status)
    {
        if (!CanBindTo(status))
        {
            return false;
        }

        if (boundStatus == null)
        {
            boundStatus = status;
        }

        return true;
    }
}
