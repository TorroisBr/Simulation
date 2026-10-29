using System;

/// <summary>One Core-owned event/history read model and its exact allocator-bound writer.</summary>
internal sealed class DailyV1DomainEventAuthority
{
    private DailyV1DomainEventAuthority(
        HistoryStore history,
        DomainEventStore eventStore,
        DomainEventRecorder recorder)
    {
        History = history ?? throw new ArgumentNullException(nameof(history));
        EventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        Recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
    }

    internal HistoryStore History { get; }
    internal DomainEventStore EventStore { get; }
    internal DomainEventRecorder Recorder { get; }

    internal static DailyV1DomainEventAuthority Create(
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        SimulationLogger diagnosticLogger = null)
    {
        if (runtimeIdAllocator == null)
            throw new ArgumentNullException(nameof(runtimeIdAllocator));
        if (simulationTime == null)
            throw new ArgumentNullException(nameof(simulationTime));
        if (recordSequence == null)
            throw new ArgumentNullException(nameof(recordSequence));

        HistoryStore history = new HistoryStore();
        DomainEventStore eventStore = new DomainEventStore(
            history, new HistoryPolicy(), diagnosticLogger);
        DomainEventRecorder recorder = new DomainEventRecorder(
            runtimeIdAllocator, simulationTime, recordSequence, eventStore, diagnosticLogger);
        DailyV1DomainEventAuthority authority = new DailyV1DomainEventAuthority(
            history, eventStore, recorder);
        if (!authority.IsFor(runtimeIdAllocator, simulationTime, recordSequence))
            throw new InvalidOperationException(
                "Core event/history authority did not retain its exact runtime identity roots.");
        return authority;
    }

    internal bool IsFor(
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence) =>
        EventStore.UsesHistoryStore(History)
        && Recorder.UsesEventStore(EventStore)
        && Recorder.UsesIdentityOwners(runtimeIdAllocator, simulationTime, recordSequence);

    internal bool TryBindSelectedTravelEventRecorders(
        DailyV1ExecutionBoundaryState executionBoundaryState)
    {
        if (executionBoundaryState == null
            || !IsFor(executionBoundaryState.SelectedRuntimeIdAllocator,
                executionBoundaryState.SimulationTime,
                executionBoundaryState.SelectedRecordSequence))
            return false;

        return executionBoundaryState.TryBindSelectedTravelEventRecorders(
            Recorder, Recorder);
    }
}
