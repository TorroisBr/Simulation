using System;

/// <summary>Core-owned event, history, and decision-receipt authorities for one selected runtime.</summary>
internal sealed class DailyV1DomainEventAuthority
{
    private DailyV1DomainEventAuthority(
        HistoryStore history,
        DomainEventStore eventStore,
        DomainEventRecorder recorder,
        DailyV1DecisionOccurrenceReceiptAuthority decisionOccurrenceReceipts)
    {
        History = history ?? throw new ArgumentNullException(nameof(history));
        EventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        Recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        DecisionOccurrenceReceipts = decisionOccurrenceReceipts
            ?? throw new ArgumentNullException(nameof(decisionOccurrenceReceipts));
    }

    internal HistoryStore History { get; }
    internal DomainEventStore EventStore { get; }
    internal DomainEventRecorder Recorder { get; }
    internal DailyV1DecisionOccurrenceReceiptAuthority DecisionOccurrenceReceipts { get; }

    internal static DailyV1DomainEventAuthority Create(
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        SimulationLogger diagnosticLogger = null)
    {
        if (runtimeIdAllocator == null)
            throw new ArgumentNullException(nameof(runtimeIdAllocator));
        if (simulationTime == null)
            throw new ArgumentNullException(nameof(simulationTime));
        if (recordSequence == null)
            throw new ArgumentNullException(nameof(recordSequence));
        if (decisionStore == null)
            throw new ArgumentNullException(nameof(decisionStore));

        HistoryStore history = new HistoryStore();
        DomainEventStore eventStore = new DomainEventStore(
            history, new HistoryPolicy(), diagnosticLogger);
        DomainEventRecorder recorder = new DomainEventRecorder(
            runtimeIdAllocator, simulationTime, recordSequence, eventStore, diagnosticLogger);
        DailyV1DecisionOccurrenceReceiptAuthority decisionOccurrenceReceipts =
            DailyV1DecisionOccurrenceReceiptAuthority.Create(
                runtimeIdAllocator, simulationTime, recordSequence, decisionStore,
                diagnosticLogger);
        DailyV1DomainEventAuthority authority = new DailyV1DomainEventAuthority(
            history, eventStore, recorder, decisionOccurrenceReceipts);
        if (!authority.IsFor(runtimeIdAllocator, simulationTime, recordSequence, decisionStore))
            throw new InvalidOperationException(
                "Core record authority did not retain its exact runtime identity roots.");
        return authority;
    }

    internal bool IsFor(
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore) =>
        EventStore.UsesHistoryStore(History)
        && Recorder.UsesEventStore(EventStore)
        && Recorder.UsesIdentityOwners(runtimeIdAllocator, simulationTime, recordSequence)
        && DecisionOccurrenceReceipts.IsFor(
            runtimeIdAllocator, simulationTime, recordSequence, decisionStore);

    internal bool TryBindSelectedTravelEventRecorders(
        DailyV1ExecutionBoundaryState executionBoundaryState)
    {
        if (executionBoundaryState == null
            || !IsFor(executionBoundaryState.SelectedRuntimeIdAllocator,
                executionBoundaryState.SimulationTime,
                executionBoundaryState.SelectedRecordSequence,
                executionBoundaryState.SelectedDecisionStore))
            return false;

        return executionBoundaryState.TryBindSelectedTravelEventRecorders(
            Recorder, Recorder);
    }
}