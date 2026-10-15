/// <summary>
/// Read-only view of the mutation health for a composed authoritative runtime.
/// The runtime that owns the guard controls how health changes.
/// </summary>
public interface IAuthoritativeMutationStatus
{
    bool CanMutate { get; }
}
