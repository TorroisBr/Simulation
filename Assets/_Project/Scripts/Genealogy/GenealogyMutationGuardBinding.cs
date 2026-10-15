/// <summary>
/// Unity composition adapter that binds the final Core genealogy owner to the
/// exact guard instance owned by its SimulationRuntime.
/// </summary>
internal sealed class GenealogyMutationGuardBinding : IAuthoritativeMutationGuardBindable
{
    private readonly GenealogyStore owner;

    internal GenealogyMutationGuardBinding(GenealogyStore owner)
    {
        this.owner = owner ?? throw new System.ArgumentNullException(nameof(owner));
    }

    public bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return GenealogyOwnerBridge.CanBindMutationStatus(owner, guard);
    }

    public bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return GenealogyOwnerBridge.TryBindMutationStatus(owner, guard);
    }
}
