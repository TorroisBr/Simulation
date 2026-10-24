using System;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Assembly-CSharp")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
[assembly: InternalsVisibleTo("Simulation.Core.Tests")]

/// <summary>The selected Daily-v1 runtime admission profile.</summary>
public enum SimulationRuntimeAdmissionProfile
{
    None = 0,
    UnityBootstrapDailyV1 = 1
}

/// <summary>Explicit profile and owner-thread identity for the bounded P12 daily adapter.</summary>
public sealed class SimulationRuntimeAdmissionContext
{
    private readonly object bootstrapLineageIdentity;

    public SimulationRuntimeAdmissionProfile Profile { get; }
    internal bool IsRestoredContinuation { get; }
    internal Thread ExpectedOwnerThread { get; }
    internal int ExpectedOwnerThreadId { get; }

    public SimulationRuntimeAdmissionContext(
        SimulationRuntimeAdmissionProfile profile,
        Thread expectedOwnerThread,
        int expectedOwnerThreadId)
        : this(profile, expectedOwnerThread, expectedOwnerThreadId, false, null)
    {
    }

    private SimulationRuntimeAdmissionContext(
        SimulationRuntimeAdmissionProfile profile,
        Thread expectedOwnerThread,
        int expectedOwnerThreadId,
        bool isRestoredContinuation,
        object bootstrapLineageIdentity)
    {
        if (profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            throw new ArgumentOutOfRangeException(nameof(profile));
        if (expectedOwnerThread == null)
            throw new ArgumentNullException(nameof(expectedOwnerThread));
        if (expectedOwnerThreadId <= 0
            || expectedOwnerThread.ManagedThreadId != expectedOwnerThreadId)
            throw new ArgumentException(
                "The expected managed thread id must match the captured Unity Start thread.",
                nameof(expectedOwnerThreadId));

        Profile = profile;
        IsRestoredContinuation = isRestoredContinuation;
        ExpectedOwnerThread = expectedOwnerThread;
        ExpectedOwnerThreadId = expectedOwnerThreadId;
        this.bootstrapLineageIdentity = bootstrapLineageIdentity ?? new object();
    }

    internal SimulationRuntimeAdmissionContext CreateRestoredContinuationContext()
    {
        if (Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1 || IsRestoredContinuation)
            throw new InvalidOperationException("A restored continuation context must derive from the selected Daily-v1 bootstrap context.");

        return new SimulationRuntimeAdmissionContext(
            Profile,
            ExpectedOwnerThread,
            ExpectedOwnerThreadId,
            true,
            bootstrapLineageIdentity);
    }

    internal bool IsRestoredContinuationOf(
        SimulationRuntimeAdmissionContext bootstrapContext)
    {
        return IsRestoredContinuation
            && bootstrapContext != null
            && !bootstrapContext.IsRestoredContinuation
            && Profile == bootstrapContext.Profile
            && ReferenceEquals(bootstrapLineageIdentity,
                bootstrapContext.bootstrapLineageIdentity);
    }

    public static SimulationRuntimeAdmissionContext CaptureUnityBootstrapDailyV1()
    {
        Thread ownerThread = Thread.CurrentThread;
        return new SimulationRuntimeAdmissionContext(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            ownerThread,
            ownerThread.ManagedThreadId);
    }

    internal bool IsOwnedByCurrentThread()
    {
        return ReferenceEquals(ExpectedOwnerThread, Thread.CurrentThread)
            && ExpectedOwnerThreadId == Thread.CurrentThread.ManagedThreadId;
    }
}
