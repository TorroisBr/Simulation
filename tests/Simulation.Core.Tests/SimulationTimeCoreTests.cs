using System;
using NUnit.Framework;

[TestFixture]
public sealed class SimulationTimeCoreTests
{
    [Test]
    public void StandaloneClockAdvancesOneAbsoluteDay()
    {
        SimulationTime clock = new SimulationTime(17L);

        Assert.That(clock.TryAdvanceDay(out SimulationTimeAdvanceFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationTimeAdvanceFailure.None));
        Assert.That(clock.AbsoluteDay, Is.EqualTo(18L));
    }

    [Test]
    public void MaximumAbsoluteDayRejectsWithoutMutation()
    {
        SimulationTime clock = new SimulationTime(long.MaxValue);

        Assert.That(clock.TryAdvanceDay(out SimulationTimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationTimeAdvanceFailure.AbsoluteDayOverflow));
        Assert.That(clock.AbsoluteDay, Is.EqualTo(long.MaxValue));
        Assert.Throws<InvalidOperationException>(() => clock.AdvanceDay());
    }

    [Test]
    public void BoundFaultedRuntimeStatusRejectsWithoutMutation()
    {
        SimulationTime clock = new SimulationTime(4L);
        MutableMutationStatus status = new MutableMutationStatus();

        Assert.That(SimulationTimeOwnerBridge.CanBindMutationStatus(clock, status), Is.True);
        Assert.That(SimulationTimeOwnerBridge.TryBindMutationStatus(clock, status), Is.True);
        status.CanMutate = false;

        Assert.That(clock.TryAdvanceDay(out SimulationTimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationTimeAdvanceFailure.RuntimeFaulted));
        Assert.That(clock.AbsoluteDay, Is.EqualTo(4L));
    }

    [Test]
    public void PublicAdvanceUsesRuntimeDispatcherAndRuntimeClockPath()
    {
        SimulationTime clock = new SimulationTime(9L);
        int dispatchCount = 0;
        Assert.That(SimulationTimeOwnerBridge.TryBindRuntimeAdvanceDispatcher(clock,
            (out SimulationTimeAdvanceFailure failure) =>
            {
                dispatchCount++;
                return SimulationTimeOwnerBridge.TryAdvanceDayFromRuntime(clock, out failure);
            }), Is.True);

        clock.AdvanceDay();

        Assert.That(dispatchCount, Is.EqualTo(1));
        Assert.That(clock.AbsoluteDay, Is.EqualTo(10L));
        Assert.That(SimulationTimeOwnerBridge.TryBindRuntimeAdvanceDispatcher(clock,
            (out SimulationTimeAdvanceFailure failure) => { failure = SimulationTimeAdvanceFailure.None; return true; }), Is.False);
    }

    [Test]
    public void TimelineProjectionOwnsTheClockAndRejectsIndependentAdvance()
    {
        SimulationTime clock = new SimulationTime(3L);
        long projectedDay = 3L;
        Assert.That(SimulationTimeOwnerBridge.TryBindAbsoluteDayProjection(clock, () => projectedDay), Is.True);

        projectedDay = 8L;
        Assert.That(clock.AbsoluteDay, Is.EqualTo(8L));
        Assert.That(clock.TryAdvanceDay(out SimulationTimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationTimeAdvanceFailure.TimelineProjectionOwnsClock));
        Assert.That(clock.AbsoluteDay, Is.EqualTo(8L));
    }

    [Test]
    public void ProjectionRequiresTheExistingBoundaryAndCannotBeRebound()
    {
        SimulationTime clock = new SimulationTime(3L);
        Assert.That(SimulationTimeOwnerBridge.TryBindAbsoluteDayProjection(clock, () => 4L), Is.False);
        Assert.That(clock.AbsoluteDay, Is.EqualTo(3L));
        Assert.That(SimulationTimeOwnerBridge.TryBindAbsoluteDayProjection(clock, () => 3L), Is.True);
        Assert.That(SimulationTimeOwnerBridge.TryBindAbsoluteDayProjection(clock, () => 3L), Is.False);
    }

    private sealed class MutableMutationStatus : IAuthoritativeMutationStatus
    {
        public bool CanMutate { get; set; } = true;
    }
}
