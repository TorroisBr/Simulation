using System;
using System.Reflection;
using NUnit.Framework;

public sealed class RuntimeAuthoritativeMutationGuardTests
{
    [Test]
    public void StandaloneSimulationTimeRemainsMutable()
    {
        SimulationTime time = new SimulationTime(4L);

        time.AdvanceDay();

        Assert.That(time.AbsoluteDay, Is.EqualTo(5L));
        Assert.That(time.TryAdvanceDay(out SimulationTimeAdvanceFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationTimeAdvanceFailure.None));
        Assert.That(time.AbsoluteDay, Is.EqualTo(6L));
    }

    [Test]
    public void RuntimeStartsHealthyAndBindsItsSimulationTime()
    {
        SimulationTime time = new SimulationTime();
        SimulationRuntime runtime = new SimulationRuntime(time, null, null);

        Assert.That(runtime.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Healthy));
        Assert.That(runtime.IsMutationFaulted, Is.False);
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(time.AbsoluteDay, Is.EqualTo(1L));
    }

    [Test]
    public void FaultIsStickyAndBlocksRuntimeAndDirectClockAdvancement()
    {
        SimulationTime time = new SimulationTime(7L);
        SimulationRuntime runtime = new SimulationRuntime(time, null, null);

        MarkFaulted(runtime, AuthoritativeMutationFaultReason.RollbackRestoreFailed);
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.IntegrityRestoreFailed);

        Assert.That(runtime.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Faulted));
        Assert.That(runtime.MutationFaultReason, Is.EqualTo(AuthoritativeMutationFaultReason.RollbackRestoreFailed));
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(time.TryAdvanceDay(out SimulationTimeAdvanceFailure timeFailure), Is.False);
        Assert.That(timeFailure, Is.EqualTo(SimulationTimeAdvanceFailure.RuntimeFaulted));
        Assert.Throws<InvalidOperationException>(() => runtime.AdvanceDay());
        Assert.Throws<InvalidOperationException>(() => time.AdvanceDay());
        Assert.That(runtime.CurrentDay, Is.EqualTo(7L));
    }

    [Test]
    public void FaultedAdvanceDaysRejectsBeforeAdvancingAnyDay()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(12L), null, null);
        MarkFaulted(runtime, AuthoritativeMutationFaultReason.IntegrityRestoreFailed);

        Assert.That(
            runtime.TryAdvanceDays(5, out int daysAdvanced, out SimulationRuntimeAdvanceFailure failure),
            Is.False);

        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(12L));
        Assert.Throws<InvalidOperationException>(() => runtime.AdvanceDays(5));
        Assert.That(runtime.CurrentDay, Is.EqualTo(12L));
    }

    [Test]
    public void FaultingOneRuntimeDoesNotAffectAnotherRuntime()
    {
        SimulationRuntime worldA = new SimulationRuntime(new SimulationTime(2L), null, null);
        SimulationRuntime worldB = new SimulationRuntime(new SimulationTime(9L), null, null);
        MarkFaulted(worldA, AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        Assert.That(worldA.IsMutationFaulted, Is.True);
        Assert.That(worldB.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Healthy));
        Assert.That(worldA.TryAdvanceDay(out _), Is.False);
        Assert.That(worldB.TryAdvanceDay(out _), Is.True);
        Assert.That(worldA.CurrentDay, Is.EqualTo(2L));
        Assert.That(worldB.CurrentDay, Is.EqualTo(10L));
    }

    [Test]
    public void RuntimeRejectsReusingBoundSimulationTime()
    {
        SimulationTime sharedTime = new SimulationTime();
        _ = new SimulationRuntime(sharedTime, null, null);

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(sharedTime, null, null));
    }

    [Test]
    public void OrdinaryInvalidDayCountDoesNotFaultRuntime()
    {
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);

        Assert.That(
            runtime.TryAdvanceDays(-1, out int daysAdvanced, out SimulationRuntimeAdvanceFailure failure),
            Is.False);

        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.InvalidDayCount));
        Assert.That(runtime.MutationHealth, Is.EqualTo(AuthoritativeMutationHealth.Healthy));
        Assert.That(runtime.TryAdvanceDay(out _), Is.True);
    }

    private static void MarkFaulted(
        SimulationRuntime runtime,
        AuthoritativeMutationFaultReason reason)
    {
        MethodInfo markFaulted = typeof(SimulationRuntime).GetMethod(
            "MarkAuthoritativeMutationFaulted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(markFaulted, Is.Not.Null);
        markFaulted.Invoke(runtime, new object[] { reason });
    }
}
