using System;

public sealed class SimulationRecordSequenceSnapshot
{
    public const string CurrentSchemaId = "simulation-record-sequence";
    public const int CurrentSchemaVersion = 1;

    public string SchemaId { get; }
    public int SchemaVersion { get; }
    public long NextSequence { get; }

    public SimulationRecordSequenceSnapshot(string schemaId, int schemaVersion, long nextSequence)
    {
        SchemaId = schemaId;
        SchemaVersion = schemaVersion;
        NextSequence = nextSequence;
    }
}

/// <summary>The single owner for deterministic simulation record sequence allocation.</summary>
public sealed class SimulationRecordSequence
{
    private long nextSequence = 1L;
    private readonly object censusOwnerIdentity = new object();
    private Func<bool> p12MutationAdmission;
    private Action p12MutationCommitted;

    internal object CensusOwnerIdentity => censusOwnerIdentity;
    internal long CensusRevision => nextSequence - 1L;

    public SimulationRecordSequence()
    {
    }

    public SimulationRecordSequenceSnapshot CaptureSnapshot()
    {
        return new SimulationRecordSequenceSnapshot(
            SimulationRecordSequenceSnapshot.CurrentSchemaId,
            SimulationRecordSequenceSnapshot.CurrentSchemaVersion,
            nextSequence);
    }

    internal static bool TryCreateStagedFromSnapshot(
        SimulationRecordSequenceSnapshot snapshot,
        out SimulationRecordSequence sequence,
        out string diagnostic)
    {
        sequence = null;
        diagnostic = null;

        if (snapshot == null)
        {
            diagnostic = "Simulation record sequence snapshot is null.";
            return false;
        }

        if (string.Equals(snapshot.SchemaId, SimulationRecordSequenceSnapshot.CurrentSchemaId, StringComparison.Ordinal) == false
            || snapshot.SchemaVersion != SimulationRecordSequenceSnapshot.CurrentSchemaVersion)
        {
            diagnostic = "Simulation record sequence snapshot schema is unsupported.";
            return false;
        }

        if (snapshot.NextSequence < 1L || snapshot.NextSequence == long.MaxValue)
        {
            diagnostic = "Simulation record sequence snapshot contains an invalid or exhausted next sequence.";
            return false;
        }

        sequence = new SimulationRecordSequence(snapshot.NextSequence);
        return true;
    }

    private SimulationRecordSequence(long restoredNextSequence)
    {
        nextSequence = restoredNextSequence;
    }

    public long Allocate()
    {
        if (nextSequence == long.MaxValue)
        {
            throw new InvalidOperationException("Simulation record sequence is exhausted.");
        }

        if (p12MutationAdmission != null && !CanCommitP12Mutation())
        {
            throw new InvalidOperationException("The P12 record-sequence mutation was rejected before allocation.");
        }

        long allocated = nextSequence;
        nextSequence++;
        NotifyP12MutationCommitted();
        return allocated;
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("SimulationRecordSequence is already bound to a P12 mutation boundary.");
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    private bool CanCommitP12Mutation()
    {
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        p12MutationCommitted?.Invoke();
    }
}
