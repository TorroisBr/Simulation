using System;

/// <summary>Passive census evidence for one sequence cursor, without exposing its mutable owner.</summary>
public sealed class SimulationRecordSequenceCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = "p12c.simulation-record-sequence";
    public const int SchemaVersion = 1;

    private readonly SimulationRecordSequence sequence;

    public SimulationRecordSequenceCensusProvider(SimulationRecordSequence sequence)
    {
        this.sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            sequence.CensusOwnerIdentity,
            1,
            sequence.CensusRevision);
    }
}
