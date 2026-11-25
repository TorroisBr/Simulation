using System;

/// <summary>Passive census evidence for one sequence cursor, without exposing its mutable owner.</summary>
public sealed class SimulationRecordSequenceCensusProvider : IOwnerSectionCensusProvider
{
    public const string SectionId = SimulationRecordSequenceOwnerBridge.SectionId;
    public const int SchemaVersion = SimulationRecordSequenceOwnerBridge.SchemaVersion;

    private readonly SimulationRecordSequence sequence;

    public SimulationRecordSequenceCensusProvider(SimulationRecordSequence sequence)
    {
        this.sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
    }

    public OwnerSectionCensusWitness GetCurrentCensus()
    {
        SimulationRecordSequenceOwnerBridge.CaptureCensusIdentity(
            sequence,
            out object ownerInstanceIdentity,
            out long revision);
        return new OwnerSectionCensusWitness(
            SectionId,
            SchemaVersion,
            ownerInstanceIdentity,
            1,
            revision);
    }
}
