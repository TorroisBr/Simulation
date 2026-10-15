using System;

/// <summary>
/// Narrow assembly boundary for the Unity-owned Runtime and P12 adapters.
/// These methods expose immutable transfer values and named lifecycle ports,
/// not a second owner or a general revision setter.
/// </summary>
public static class GenealogyOwnerBridge
{
    public static bool CanBindMutationStatus(
        GenealogyStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.CanBindMutationStatus(status);
    }

    public static bool TryBindMutationStatus(
        GenealogyStore owner,
        IAuthoritativeMutationStatus status)
    {
        return owner != null && owner.TryBindMutationStatus(status);
    }

    /// <summary>
    /// Captures the owner's immutable sorted edge values and exact local
    /// revision. The caller remains responsible for P12 capture authority.
    /// </summary>
    public static GenealogyOwnerExport CaptureOwnerSnapshot(GenealogyStore owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        return owner.CaptureOwnerExport();
    }

    /// <summary>
    /// Creates an unpublished, unbound owner from an immutable export. This
    /// does not validate cross-owner membership or publish a runtime.
    /// </summary>
    public static bool TryCreateStagedOwner(
        GenealogyOwnerExport export,
        out GenealogyStore stagedStore,
        out GenealogyFailure failure,
        Action afterRecordStagedForTest = null)
    {
        return GenealogyStore.TryCreateFromOwnerExport(
            export,
            out stagedStore,
            out failure,
            afterRecordStagedForTest);
    }

    /// <summary>
    /// Performs the tightly scoped PersonBirthLifecycle rollback operation.
    /// At saturated revision only this rollback may remove the just-created
    /// edge without advancing the revision.
    /// </summary>
    public static bool TryRollbackParentageForBirth(
        GenealogyStore owner,
        PersonId parent,
        PersonId child,
        out GenealogyFailure failure)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        return owner.TryRollbackParentageForBirth(parent, child, out failure);
    }
}
