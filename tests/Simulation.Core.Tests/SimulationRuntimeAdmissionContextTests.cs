using System;
using System.Threading;
using NUnit.Framework;

public sealed class SimulationRuntimeAdmissionContextTests
{
    [Test]
    public void Capture_daily_v1_retains_exact_current_thread_identity()
    {
        SimulationRuntimeAdmissionContext context =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();

        Assert.That(context.Profile, Is.EqualTo(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1));
        Assert.That(context.ExpectedOwnerThread, Is.SameAs(Thread.CurrentThread));
        Assert.That(context.ExpectedOwnerThreadId, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
        Assert.That(context.IsOwnedByCurrentThread(), Is.True);
        Assert.That(context.IsRestoredContinuation, Is.False);
    }

    [Test]
    public void Restored_context_is_a_distinct_context_bound_to_the_original_thread()
    {
        SimulationRuntimeAdmissionContext original =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();

        SimulationRuntimeAdmissionContext restored = original.CreateRestoredContinuationContext();

        Assert.That(restored, Is.Not.SameAs(original));
        Assert.That(restored.Profile, Is.EqualTo(original.Profile));
        Assert.That(restored.ExpectedOwnerThread, Is.SameAs(original.ExpectedOwnerThread));
        Assert.That(restored.ExpectedOwnerThreadId, Is.EqualTo(original.ExpectedOwnerThreadId));
        Assert.That(restored.IsOwnedByCurrentThread(), Is.True);
        Assert.That(restored.IsRestoredContinuation, Is.True);
    }

    [Test]
    public void Restored_context_cannot_be_derived_twice()
    {
        SimulationRuntimeAdmissionContext restored =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1()
                .CreateRestoredContinuationContext();

        Assert.Throws<InvalidOperationException>(() => restored.CreateRestoredContinuationContext());
    }

    [Test]
    public void Context_rejects_non_daily_profile_and_mismatched_owner_thread_identity()
    {
        Thread current = Thread.CurrentThread;

        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationRuntimeAdmissionContext(
            SimulationRuntimeAdmissionProfile.None, current, current.ManagedThreadId));
        Assert.Throws<ArgumentException>(() => new SimulationRuntimeAdmissionContext(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1, current, current.ManagedThreadId + 1));
    }
}
