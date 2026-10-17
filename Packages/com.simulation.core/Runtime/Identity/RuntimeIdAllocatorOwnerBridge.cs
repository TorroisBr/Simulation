using System;

/// <summary>
/// Narrow host-facing access to the selected runtime identity root. Mutable
/// allocator state and its P12 admission controls remain owned by the allocator.
/// </summary>
public static class RuntimeIdAllocatorOwnerBridge
{
    public static bool TryCreateStagedFromSnapshot(
        RuntimeIdAllocatorSnapshot snapshot,
        out RuntimeIdAllocator allocator,
        out string diagnostic)
    {
        return RuntimeIdAllocator.TryCreateStagedFromSnapshot(snapshot, out allocator, out diagnostic);
    }

    public static void BindP12EventIdMutationBoundary(
        RuntimeIdAllocator allocator,
        Func<bool> admission,
        Action committed)
    {
        if (allocator == null) throw new ArgumentNullException(nameof(allocator));
        allocator.BindP12EventIdMutationBoundary(admission, committed);
    }

    public static void BindP12DecisionIdMutationBoundary(
        RuntimeIdAllocator allocator,
        Func<bool> admission,
        Action committed)
    {
        if (allocator == null) throw new ArgumentNullException(nameof(allocator));
        allocator.BindP12DecisionIdMutationBoundary(admission, committed);
    }

    public static void BindP12TravelPartyIdMutationBoundary(
        RuntimeIdAllocator allocator,
        Func<bool> admission,
        Action committed)
    {
        if (allocator == null) throw new ArgumentNullException(nameof(allocator));
        allocator.BindP12TravelPartyIdMutationBoundary(admission, committed);
    }

    public static void CaptureCensusIdentity(
        RuntimeIdAllocator allocator,
        RuntimeIdAllocatorCensusCounter counter,
        out object ownerInstanceIdentity,
        out long revision)
    {
        if (allocator == null) throw new ArgumentNullException(nameof(allocator));
        ownerInstanceIdentity = allocator.CensusOwnerIdentity;
        revision = allocator.GetCensusRevision(counter);
    }

    public static bool TryMatchCurrentCensus(
        RuntimeIdAllocator allocator,
        RuntimeIdAllocatorCensusCounter counter,
        object ownerInstanceIdentity,
        long witnessRevision,
        out long currentRevision)
    {
        currentRevision = allocator == null ? -1L : allocator.GetCensusRevision(counter);
        return allocator != null
            && ownerInstanceIdentity != null
            && ReferenceEquals(ownerInstanceIdentity, allocator.CensusOwnerIdentity)
            && witnessRevision == currentRevision;
    }
}
