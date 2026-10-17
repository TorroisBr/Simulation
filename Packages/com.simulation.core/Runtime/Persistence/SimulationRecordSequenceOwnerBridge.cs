using System;

/// <summary>
/// Narrow host-facing operations for the Core-owned simulation record sequence.
/// The mutable sequence keeps its staging and mutation-boundary details internal.
/// </summary>
public static class SimulationRecordSequenceOwnerBridge
{
    public const string SectionId = "p12c.simulation-record-sequence";
    public const int SchemaVersion = 1;

    /// <summary>
    /// Binds the existing P12 admission and committed-write callbacks supplied
    /// by the sole SimulationRuntime owner.
    /// </summary>
    public static void BindP12MutationBoundary(
        SimulationRecordSequence sequence,
        Func<bool> admission,
        Action committed)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        sequence.BindP12MutationBoundary(admission, committed);
    }

    /// <summary>Creates an unpublished sequence from its validated P12 snapshot.</summary>
    public static bool TryCreateStagedFromSnapshot(
        SimulationRecordSequenceSnapshot snapshot,
        out SimulationRecordSequence sequence,
        out string diagnostic)
    {
        return SimulationRecordSequence.TryCreateStagedFromSnapshot(
            snapshot,
            out sequence,
            out diagnostic);
    }

    /// <summary>Checks that a census witness still describes this exact live sequence.</summary>
    public static bool TryMatchCurrentCensus(
        SimulationRecordSequence sequence,
        object ownerInstanceIdentity,
        long witnessRevision,
        out long currentRevision)
    {
        currentRevision = sequence == null ? -1L : sequence.CensusRevision;
        return sequence != null
            && ownerInstanceIdentity != null
            && ReferenceEquals(ownerInstanceIdentity, sequence.CensusOwnerIdentity)
            && witnessRevision == currentRevision;
    }

    /// <summary>Reads the opaque owner token and local revision for the Unity census adapter.</summary>
    public static void CaptureCensusIdentity(
        SimulationRecordSequence sequence,
        out object ownerInstanceIdentity,
        out long revision)
    {
        if (sequence == null) throw new ArgumentNullException(nameof(sequence));
        ownerInstanceIdentity = sequence.CensusOwnerIdentity;
        revision = sequence.CensusRevision;
    }
}
