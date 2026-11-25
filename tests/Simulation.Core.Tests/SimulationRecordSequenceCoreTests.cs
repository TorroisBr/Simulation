using NUnit.Framework;

[TestFixture]
public sealed class SimulationRecordSequenceCoreTests
{
    [Test]
    public void AllocationAndCensusRemainOwnedByTheSameSequence()
    {
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        SimulationRecordSequenceOwnerBridge.CaptureCensusIdentity(
            sequence,
            out object initialOwnerIdentity,
            out long initialOwnerRevision);
        SimulationRecordSequenceSnapshot initial = sequence.CaptureSnapshot();
        Assert.That(
            SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
                sequence,
                initialOwnerIdentity,
                initialOwnerRevision,
                out long initialRevision),
            Is.True);
        Assert.That(initialRevision, Is.EqualTo(0L));

        Assert.That(initial.SchemaId, Is.EqualTo(SimulationRecordSequenceSnapshot.CurrentSchemaId));
        Assert.That(initial.SchemaVersion, Is.EqualTo(SimulationRecordSequenceSnapshot.CurrentSchemaVersion));
        Assert.That(initial.NextSequence, Is.EqualTo(1L));

        Assert.That(sequence.Allocate(), Is.EqualTo(1L));

        SimulationRecordSequenceSnapshot advanced = sequence.CaptureSnapshot();
        SimulationRecordSequenceOwnerBridge.CaptureCensusIdentity(
            sequence,
            out object ownerIdentityAfterAllocation,
            out long ownerRevisionAfterAllocation);
        Assert.That(ownerIdentityAfterAllocation, Is.SameAs(initialOwnerIdentity));
        Assert.That(
            SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
                sequence,
                initialOwnerIdentity,
                initialOwnerRevision,
                out long currentRevision),
            Is.False);
        Assert.That(currentRevision, Is.EqualTo(1L));
        Assert.That(
            SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
                sequence,
                ownerIdentityAfterAllocation,
                ownerRevisionAfterAllocation,
                out currentRevision),
            Is.True);
        Assert.That(ownerRevisionAfterAllocation, Is.EqualTo(1L));
        Assert.That(advanced.NextSequence, Is.EqualTo(2L));
    }

    [Test]
    public void NarrowBridgePreservesMutationAdmissionAndStagedRestore()
    {
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        bool admitted = true;
        int committed = 0;
        SimulationRecordSequenceOwnerBridge.BindP12MutationBoundary(
            sequence,
            () => admitted,
            () => committed++);

        Assert.That(sequence.Allocate(), Is.EqualTo(1L));
        Assert.That(committed, Is.EqualTo(1));
        admitted = false;
        Assert.Throws<System.InvalidOperationException>(() => sequence.Allocate());
        Assert.That(sequence.CaptureSnapshot().NextSequence, Is.EqualTo(2L));
        Assert.That(committed, Is.EqualTo(1));

        Assert.That(
            SimulationRecordSequenceOwnerBridge.TryCreateStagedFromSnapshot(
                sequence.CaptureSnapshot(),
                out SimulationRecordSequence staged,
                out string diagnostic),
            Is.True,
            diagnostic);
        Assert.That(staged.CaptureSnapshot().NextSequence, Is.EqualTo(2L));
    }
}
