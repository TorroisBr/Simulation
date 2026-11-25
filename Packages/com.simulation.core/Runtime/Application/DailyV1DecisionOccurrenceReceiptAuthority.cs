using System;
using System.Collections.Generic;

/// <summary>
/// Core-owned idempotency receipts for decision-producing selected runtime
/// operations. The Unity recorder is an adapter over this exact census owner.
/// </summary>
internal sealed class DailyV1DecisionOccurrenceReceiptAuthority
{
    internal const string SectionId = "p12f.npc-decision-occurrence-receipts";
    internal const int SectionSchemaVersion = 1;

    private readonly RuntimeIdAllocator runtimeIdAllocator;
    private readonly SimulationTime simulationTime;
    private readonly SimulationRecordSequence recordSequence;
    private readonly NpcDecisionStore decisionStore;
    private readonly SimulationLogger logger;
    private readonly object ownerIdentity = new object();
    private readonly Dictionary<string, NpcDecisionOccurrenceReceipt> receipts =
        new Dictionary<string, NpcDecisionOccurrenceReceipt>(StringComparer.Ordinal);
    private long revision;

    private DailyV1DecisionOccurrenceReceiptAuthority(
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        SimulationLogger logger)
    {
        this.runtimeIdAllocator = runtimeIdAllocator
            ?? throw new ArgumentNullException(nameof(runtimeIdAllocator));
        this.simulationTime = simulationTime
            ?? throw new ArgumentNullException(nameof(simulationTime));
        this.recordSequence = recordSequence
            ?? throw new ArgumentNullException(nameof(recordSequence));
        this.decisionStore = decisionStore
            ?? throw new ArgumentNullException(nameof(decisionStore));
        this.logger = logger;
    }

    internal RuntimeIdAllocator RuntimeIdAllocator => runtimeIdAllocator;
    internal SimulationTime SimulationTime => simulationTime;
    internal SimulationRecordSequence RecordSequence => recordSequence;
    internal NpcDecisionStore DecisionStore => decisionStore;

    internal static DailyV1DecisionOccurrenceReceiptAuthority Create(
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        SimulationLogger logger = null) =>
        new DailyV1DecisionOccurrenceReceiptAuthority(
            runtimeIdAllocator, simulationTime, recordSequence, decisionStore, logger);

    internal bool IsFor(
        RuntimeIdAllocator candidateRuntimeIdAllocator,
        SimulationTime candidateSimulationTime,
        SimulationRecordSequence candidateRecordSequence,
        NpcDecisionStore candidateDecisionStore) =>
        ReferenceEquals(runtimeIdAllocator, candidateRuntimeIdAllocator)
        && ReferenceEquals(simulationTime, candidateSimulationTime)
        && ReferenceEquals(recordSequence, candidateRecordSequence)
        && ReferenceEquals(decisionStore, candidateDecisionStore);

    internal OwnerSectionCensusWitness GetCensus() =>
        new OwnerSectionCensusWitness(
            SectionId, SectionSchemaVersion, ownerIdentity, receipts.Count, revision);

    internal bool TryRecordOccurrenceOnce(
        string operationIdentity,
        string operationFingerprint,
        string primaryActorRuntimeId,
        NpcDecisionType decisionType,
        NpcDecisionOrigin origin,
        string actionDefinitionId,
        string targetLocationRuntimeId,
        CommercialDecisionEvidence commercialEvidence,
        out NpcDecisionRecord decision)
    {
        decision = null;
        if (string.IsNullOrWhiteSpace(operationIdentity)
            || string.IsNullOrWhiteSpace(operationFingerprint)
            || string.IsNullOrWhiteSpace(primaryActorRuntimeId))
            return false;

        if (receipts.TryGetValue(operationIdentity,
                out NpcDecisionOccurrenceReceipt existing))
        {
            if (!string.Equals(existing.OperationFingerprint,
                    operationFingerprint, StringComparison.Ordinal))
                return false;
            decision = existing.Decision;
            return true;
        }

        if (revision == long.MaxValue) return false;
        long nextRevision = revision + 1L;

        try
        {
            NpcDecisionRecord candidate = new NpcDecisionRecord(
                runtimeIdAllocator.AllocateDecisionId(),
                simulationTime.AbsoluteDay,
                recordSequence.Allocate(),
                primaryActorRuntimeId,
                decisionType,
                origin,
                actionDefinitionId,
                null,
                null,
                targetLocationRuntimeId,
                commercialEvidence,
                null);
            if (!decisionStore.Record(candidate)) return false;

            NpcDecisionOccurrenceReceipt receipt = new NpcDecisionOccurrenceReceipt(
                operationIdentity, operationFingerprint, candidate);
            receipts.Add(operationIdentity, receipt);
            revision = nextRevision;
            decision = candidate;
            return true;
        }
        catch (ArgumentException exception)
        {
            logger?.LogError("Cannot create NPC decision record: " + exception.Message);
            return false;
        }
        catch (InvalidOperationException exception)
        {
            logger?.LogError("Cannot allocate NPC decision record identity: " + exception.Message);
            return false;
        }
    }
}

internal sealed class NpcDecisionOccurrenceReceipt
{
    internal string OperationIdentity { get; }
    internal string OperationFingerprint { get; }
    internal NpcDecisionRecord Decision { get; }

    internal NpcDecisionOccurrenceReceipt(
        string operationIdentity,
        string operationFingerprint,
        NpcDecisionRecord decision)
    {
        OperationIdentity = operationIdentity
            ?? throw new ArgumentNullException(nameof(operationIdentity));
        OperationFingerprint = operationFingerprint
            ?? throw new ArgumentNullException(nameof(operationFingerprint));
        Decision = decision ?? throw new ArgumentNullException(nameof(decision));
    }
}