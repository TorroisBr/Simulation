using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SimulationRecordSequenceP12InvalidationTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void SelectedDailyProfileTracksEachAllocationPathOnceAndIgnoresReceiptReplay()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment), Is.True,
            initialAssessment.ToString());
        AssertEpoch(runtime, 0L);

        Assert.That(records.Sequence.Allocate(), Is.EqualTo(1L));
        AssertEpoch(runtime, 1L);

        NpcDecisionRecord decision = records.DecisionRecorder.Record(
            "actor", NpcDecisionType.Action, NpcDecisionOrigin.Autonomous,
            "action", null, null, null);
        Assert.That(decision, Is.Not.Null);
        Assert.That(decision.RecordSequence, Is.EqualTo(2L));
        AssertEpoch(runtime, 2L);

        Assert.That(records.EventRecorder.Record((eventId, day, sequence) =>
            new NpcArrivedEvent(eventId, day, sequence, "actor", "location")), Is.True);
        Assert.That(records.Events.Events.Count, Is.EqualTo(1));
        Assert.That(records.Events.Events[0].RecordSequence, Is.EqualTo(3L));
        AssertEpoch(runtime, 3L);

        NpcDecisionRecord occurrence = RecordOccurrence(records.DecisionRecorder, "occurrence-1");
        Assert.That(occurrence, Is.Not.Null);
        Assert.That(occurrence.RecordSequence, Is.EqualTo(4L));
        AssertEpoch(runtime, 4L);

        NpcDecisionRecord replay = RecordOccurrence(records.DecisionRecorder, "occurrence-1");
        Assert.That(replay, Is.SameAs(occurrence));
        AssertEpoch(runtime, 4L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalAssessment), Is.True,
            finalAssessment.ToString());
    }

    [Test]
    public void AllocationInsideNestedRegisteredOperationsAdvancesRevisionAndEpochOnce()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence);
        SimulationRecordSequenceCensusProvider provider = new SimulationRecordSequenceCensusProvider(records.Sequence);
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);

        Assert.That(protocol.TryEnterOperation(
            "runtime.bootstrap-publication", out SimulationOperationScope outer, out ContinuationCensusFailure outerFailure),
            Is.True, outerFailure.ToString());
        using (outer)
        {
            Assert.That(protocol.TryEnterOperation(
                "runtime.bootstrap-publication", out SimulationOperationScope inner, out ContinuationCensusFailure innerFailure),
                Is.True, innerFailure.ToString());
            using (inner)
            {
                Assert.That(protocol.TryReadActiveOperationCount(out int active, out ContinuationCensusFailure activeFailure),
                    Is.True, activeFailure.ToString());
                Assert.That(active, Is.EqualTo(2));

                Assert.That(records.Sequence.Allocate(), Is.EqualTo(1L));
                Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1L),
                    "one committed allocation emits one owner-section revision even inside nested operations");
            }
        }

        AssertEpoch(runtime, 1L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalAssessment), Is.True,
            finalAssessment.ToString());
    }

    [Test]
    public void WrongThreadAndStaleBaselineRejectBeforeConsumingSequence()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence);
        SimulationRecordSequenceCensusProvider provider = new SimulationRecordSequenceCensusProvider(records.Sequence);

        Exception wrongThreadFailure = null;
        Thread worker = new Thread(() =>
        {
            try { records.Sequence.Allocate(); }
            catch (Exception exception) { wrongThreadFailure = exception; }
        });
        worker.Start();
        worker.Join();

        Assert.That(wrongThreadFailure, Is.TypeOf<InvalidOperationException>());
        Assert.That(provider.GetCurrentCensus().Revision, Is.Zero);
        AssertProtocolFaulted(runtime);

        RecordFixture staleRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime staleRuntime = CreateRuntime(staleRecords, staleRecords.Sequence);
        SimulationRecordSequenceCensusProvider staleProvider =
            new SimulationRecordSequenceCensusProvider(staleRecords.Sequence);
        typeof(SimulationRecordSequence)
            .GetField("nextSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(staleRecords.Sequence, 2L);

        Assert.That(staleProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.Throws<InvalidOperationException>(() => staleRecords.Sequence.Allocate());
        Assert.That(staleProvider.GetCurrentCensus().Revision, Is.EqualTo(1L),
            "the rejected allocation does not add another sequence value");
        AssertProtocolFaulted(staleRuntime);
    }

    [Test]
    public void ExhaustionDoesNotAdvanceSequenceOrEpochWhenItIsTheRegisteredBaseline()
    {
        SimulationRecordSequence sequence = new SimulationRecordSequence();
        typeof(SimulationRecordSequence)
            .GetField("nextSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(sequence, long.MaxValue);
        SimulationRecordSequenceCensusProvider provider = new SimulationRecordSequenceCensusProvider(sequence);
        SimulationRuntime runtime = CreateRuntime(null, sequence);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
        Assert.Throws<InvalidOperationException>(() => sequence.Allocate());
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
        AssertEpoch(runtime, 0L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterExhaustion), Is.True,
            afterExhaustion.ToString());
    }

    [Test]
    public void SelectedDailyProfileTracksExactEventCounterOwnerAndSuccessfulAllocation()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence, records.Allocator);
        IOwnerSectionCensusProvider provider = RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(records.Allocator);
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();

        Assert.That(initial.SectionId, Is.EqualTo(RuntimeIdAllocatorCensusProvider.EventsSectionId));
        Assert.That(initial.SchemaVersion, Is.EqualTo(RuntimeIdAllocatorCensusProvider.SchemaVersion));
        Assert.That(initial.OwnerInstanceIdentity, Is.SameAs(records.Allocator.CensusOwnerIdentity));
        Assert.That(initial.Cardinality, Is.EqualTo(1));
        Assert.That(initial.Revision, Is.Zero);
        Assert.That(runtime.HasSameRuntimeIdAllocatorEventCounterOwner(provider), Is.True);
        Assert.That(runtime.HasSameRuntimeIdAllocatorEventCounterOwner(
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(new RuntimeIdAllocator())), Is.False,
            "a second allocator with the same counter value is not the selected owner");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment), Is.True,
            initialAssessment.ToString());

        Assert.That(records.Allocator.AllocateEventId(), Is.EqualTo("event-000001"));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        AssertEpoch(runtime, 1L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalAssessment), Is.True,
            finalAssessment.ToString());
    }

    [Test]
    public void SelectedDailyProfileTracksExactDecisionCounterOwnerAndSuccessfulAllocation()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence, records.Allocator);
        IOwnerSectionCensusProvider provider =
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(records.Allocator);
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();

        Assert.That(initial.SectionId, Is.EqualTo(RuntimeIdAllocatorCensusProvider.DecisionsSectionId));
        Assert.That(initial.SchemaVersion, Is.EqualTo(RuntimeIdAllocatorCensusProvider.SchemaVersion));
        Assert.That(initial.OwnerInstanceIdentity, Is.SameAs(records.Allocator.CensusOwnerIdentity));
        Assert.That(initial.Cardinality, Is.EqualTo(1));
        Assert.That(initial.Revision, Is.Zero);
        Assert.That(runtime.HasSameRuntimeIdAllocatorDecisionCounterOwner(provider), Is.True);
        Assert.That(runtime.HasSameRuntimeIdAllocatorDecisionCounterOwner(
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(new RuntimeIdAllocator())), Is.False,
            "a second allocator with the same counter value is not the selected owner");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment), Is.True,
            initialAssessment.ToString());

        Assert.That(records.Allocator.AllocateDecisionId(), Is.EqualTo("decision-000001"));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(records.Allocator)
            .GetCurrentCensus().Revision, Is.Zero,
            "the Decision allocation does not change another allocator counter");
        AssertEpoch(runtime, 1L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalAssessment), Is.True,
            finalAssessment.ToString());
    }

    [Test]
    public void RejectedOrExhaustedDecisionAllocationDoesNotAdvanceCounterOrEpoch()
    {
        RecordFixture staleRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime staleRuntime = CreateRuntime(
            staleRecords, staleRecords.Sequence, staleRecords.Allocator);
        IOwnerSectionCensusProvider staleProvider =
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(staleRecords.Allocator);
        typeof(RuntimeIdAllocator)
            .GetField("nextDecisionSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(staleRecords.Allocator, 2L);

        Assert.That(staleProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.Throws<InvalidOperationException>(() => staleRecords.Allocator.AllocateDecisionId());
        Assert.That(staleProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        ContinuationCensusProtocol staleProtocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(staleRuntime);
        Assert.That(typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(staleProtocol), Is.Zero);
        AssertProtocolFaulted(staleRuntime);

        RecordFixture wrongThreadRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime wrongThreadRuntime = CreateRuntime(
            wrongThreadRecords, wrongThreadRecords.Sequence, wrongThreadRecords.Allocator);
        IOwnerSectionCensusProvider wrongThreadProvider =
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(wrongThreadRecords.Allocator);
        Exception wrongThreadFailure = null;
        Thread worker = new Thread(() =>
        {
            try { wrongThreadRecords.Allocator.AllocateDecisionId(); }
            catch (Exception exception) { wrongThreadFailure = exception; }
        });
        worker.Start();
        worker.Join();

        Assert.That(wrongThreadFailure, Is.TypeOf<InvalidOperationException>());
        Assert.That(wrongThreadProvider.GetCurrentCensus().Revision, Is.Zero);
        Assert.That(typeof(RuntimeIdAllocator)
            .GetField("nextDecisionSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(wrongThreadRecords.Allocator), Is.EqualTo(1L));
        AssertProtocolFaulted(wrongThreadRuntime);

        SimulationRecordSequence unusedSequence = new SimulationRecordSequence();
        RuntimeIdAllocator exhaustedAllocator = new RuntimeIdAllocator();
        typeof(RuntimeIdAllocator)
            .GetField("nextDecisionSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(exhaustedAllocator, long.MaxValue);
        SimulationRuntime exhaustedRuntime = CreateRuntime(null, unusedSequence, exhaustedAllocator);
        IOwnerSectionCensusProvider exhaustedProvider =
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(exhaustedAllocator);

        Assert.That(exhaustedProvider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
        InvalidOperationException exhaustion = Assert.Throws<InvalidOperationException>(
            () => exhaustedAllocator.AllocateDecisionId());
        Assert.That(exhaustion.Message, Is.EqualTo("RuntimeId sequence exhausted for type 'decision'."));
        Assert.That(exhaustedProvider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
        AssertEpoch(exhaustedRuntime, 0L);
        Assert.That(exhaustedRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void DecisionCounterCapacityPreflightRejectsAtMaximumAndAllowsFinalEpochStep()
    {
        RecordFixture exhaustedEpochRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime exhaustedEpochRuntime = CreateRuntime(
            exhaustedEpochRecords, exhaustedEpochRecords.Sequence, exhaustedEpochRecords.Allocator);
        IOwnerSectionCensusProvider exhaustedEpochProvider =
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(exhaustedEpochRecords.Allocator);
        ContinuationCensusProtocol exhaustedEpochProtocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(exhaustedEpochRuntime);
        typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(exhaustedEpochProtocol, long.MaxValue);

        Assert.Throws<InvalidOperationException>(() => exhaustedEpochRecords.Allocator.AllocateDecisionId());
        Assert.That(exhaustedEpochProvider.GetCurrentCensus().Revision, Is.Zero);
        Assert.That(typeof(RuntimeIdAllocator)
            .GetField("nextDecisionSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(exhaustedEpochRecords.Allocator), Is.EqualTo(1L));
        AssertProtocolFaulted(exhaustedEpochRuntime);

        RecordFixture finalEpochRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime finalEpochRuntime = CreateRuntime(
            finalEpochRecords, finalEpochRecords.Sequence, finalEpochRecords.Allocator);
        IOwnerSectionCensusProvider finalEpochProvider =
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(finalEpochRecords.Allocator);
        ContinuationCensusProtocol finalEpochProtocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(finalEpochRuntime);
        typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(finalEpochProtocol, long.MaxValue - 1L);

        Assert.That(finalEpochRecords.Allocator.AllocateDecisionId(), Is.EqualTo("decision-000001"));
        Assert.That(finalEpochProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(finalEpochRuntime.TryReadNpcRosterCensusMutationEpoch(
            out long finalEpoch, out ContinuationCensusFailure readFailure), Is.True, readFailure.ToString());
        Assert.That(finalEpoch, Is.EqualTo(long.MaxValue));

        Assert.Throws<InvalidOperationException>(() => finalEpochRecords.Allocator.AllocateDecisionId());
        Assert.That(finalEpochProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(typeof(RuntimeIdAllocator)
            .GetField("nextDecisionSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(finalEpochRecords.Allocator), Is.EqualTo(2L));
        AssertProtocolFaulted(finalEpochRuntime);
    }

    [Test]
    public void DecisionAllocationRemainsInvalidatedWhenLaterRecordSequenceAllocationFails()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        typeof(SimulationRecordSequence)
            .GetField("nextSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(records.Sequence, long.MaxValue);
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence, records.Allocator);
        IOwnerSectionCensusProvider decisionProvider =
            RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(records.Allocator);
        SimulationRecordSequenceCensusProvider sequenceProvider =
            new SimulationRecordSequenceCensusProvider(records.Sequence);
        LogAssert.Expect(LogType.Error,
            "Cannot allocate NPC decision record identity: Simulation record sequence is exhausted.");

        Assert.That(records.DecisionRecorder.Record(
            "actor", NpcDecisionType.Action, NpcDecisionOrigin.Autonomous,
            "action", null, null, null), Is.Null);

        Assert.That(decisionProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(sequenceProvider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
        Assert.That(records.Decisions.Decisions, Is.Empty);
        AssertEpoch(runtime, 1L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void RejectedOrExhaustedEventAllocationDoesNotAdvanceCounterOrEpoch()
    {
        RecordFixture staleRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime staleRuntime = CreateRuntime(staleRecords, staleRecords.Sequence, staleRecords.Allocator);
        IOwnerSectionCensusProvider staleProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(staleRecords.Allocator);
        typeof(RuntimeIdAllocator)
            .GetField("nextEventSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(staleRecords.Allocator, 2L);

        Assert.That(staleProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.Throws<InvalidOperationException>(() => staleRecords.Allocator.AllocateEventId());
        Assert.That(staleProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        ContinuationCensusProtocol staleProtocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(staleRuntime);
        Assert.That(typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(staleProtocol), Is.Zero);
        AssertProtocolFaulted(staleRuntime);

        RecordFixture wrongThreadRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime wrongThreadRuntime = CreateRuntime(
            wrongThreadRecords, wrongThreadRecords.Sequence, wrongThreadRecords.Allocator);
        IOwnerSectionCensusProvider wrongThreadProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(wrongThreadRecords.Allocator);
        Exception wrongThreadFailure = null;
        Thread worker = new Thread(() =>
        {
            try { wrongThreadRecords.Allocator.AllocateEventId(); }
            catch (Exception exception) { wrongThreadFailure = exception; }
        });
        worker.Start();
        worker.Join();

        Assert.That(wrongThreadFailure, Is.TypeOf<InvalidOperationException>());
        Assert.That(wrongThreadProvider.GetCurrentCensus().Revision, Is.Zero);
        Assert.That(typeof(RuntimeIdAllocator)
            .GetField("nextEventSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(wrongThreadRecords.Allocator), Is.EqualTo(1L));
        AssertProtocolFaulted(wrongThreadRuntime);

        SimulationRecordSequence unusedSequence = new SimulationRecordSequence();
        RuntimeIdAllocator exhaustedAllocator = new RuntimeIdAllocator();
        typeof(RuntimeIdAllocator)
            .GetField("nextEventSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(exhaustedAllocator, long.MaxValue);
        SimulationRuntime exhaustedRuntime = CreateRuntime(null, unusedSequence, exhaustedAllocator);
        IOwnerSectionCensusProvider exhaustedProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(exhaustedAllocator);

        Assert.That(exhaustedProvider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
        InvalidOperationException exhaustion = Assert.Throws<InvalidOperationException>(
            () => exhaustedAllocator.AllocateEventId());
        Assert.That(exhaustion.Message, Is.EqualTo("RuntimeId sequence exhausted for type 'event'."));
        Assert.That(exhaustedProvider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1L));
        AssertEpoch(exhaustedRuntime, 0L);
        Assert.That(exhaustedRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void EventCounterCapacityPreflightRejectsAtMaximumAndAllowsFinalEpochStep()
    {
        RecordFixture exhaustedEpochRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime exhaustedEpochRuntime = CreateRuntime(
            exhaustedEpochRecords, exhaustedEpochRecords.Sequence, exhaustedEpochRecords.Allocator);
        IOwnerSectionCensusProvider exhaustedEpochProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(exhaustedEpochRecords.Allocator);
        ContinuationCensusProtocol exhaustedEpochProtocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(exhaustedEpochRuntime);
        typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(exhaustedEpochProtocol, long.MaxValue);

        Assert.Throws<InvalidOperationException>(() => exhaustedEpochRecords.Allocator.AllocateEventId());
        Assert.That(exhaustedEpochProvider.GetCurrentCensus().Revision, Is.Zero);
        Assert.That(typeof(RuntimeIdAllocator)
            .GetField("nextEventSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(exhaustedEpochRecords.Allocator), Is.EqualTo(1L));
        AssertProtocolFaulted(exhaustedEpochRuntime);

        RecordFixture finalEpochRecords = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime finalEpochRuntime = CreateRuntime(finalEpochRecords, finalEpochRecords.Sequence, finalEpochRecords.Allocator);
        IOwnerSectionCensusProvider finalEpochProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(finalEpochRecords.Allocator);
        ContinuationCensusProtocol finalEpochProtocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(finalEpochRuntime);
        typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(finalEpochProtocol, long.MaxValue - 1L);

        Assert.That(finalEpochRecords.Allocator.AllocateEventId(), Is.EqualTo("event-000001"));
        Assert.That(finalEpochProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(finalEpochRuntime.TryReadNpcRosterCensusMutationEpoch(
            out long finalEpoch, out ContinuationCensusFailure readFailure), Is.True, readFailure.ToString());
        Assert.That(finalEpoch, Is.EqualTo(long.MaxValue));

        Assert.Throws<InvalidOperationException>(() => finalEpochRecords.Allocator.AllocateEventId());
        Assert.That(finalEpochProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(typeof(RuntimeIdAllocator)
            .GetField("nextEventSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(finalEpochRecords.Allocator), Is.EqualTo(2L));
        AssertProtocolFaulted(finalEpochRuntime);
    }

    [Test]
    public void EventCounterInvalidationPrecedesLaterEventFactoryFailure()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence, records.Allocator);
        IOwnerSectionCensusProvider eventProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(records.Allocator);
        SimulationRecordSequenceCensusProvider sequenceProvider =
            new SimulationRecordSequenceCensusProvider(records.Sequence);
        LogAssert.Expect(LogType.Error, "Cannot allocate domain EventId: injected event factory failure.");

        Assert.That(records.EventRecorder.Record((eventId, day, sequence) =>
        {
            throw new InvalidOperationException("injected event factory failure.");
        }), Is.False);

        Assert.That(eventProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(sequenceProvider.GetCurrentCensus().Revision, Is.EqualTo(1L),
            "both ID and record-sequence allocations precede invocation of the failing factory");
        Assert.That(records.Events.Events, Is.Empty);
        AssertEpoch(runtime, 2L);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void NonP12RuntimeIdAllocatorRetainsExistingEventIdOutput()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(allocator.AllocateEventId(), Is.EqualTo("event-000001"));
        Assert.That(allocator.AllocateEventId(), Is.EqualTo("event-000002"));
        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-000001"));
        Assert.That(allocator.AllocateDecisionId(), Is.EqualTo("decision-000001"));
    }

    [Test]
    public void NotificationFailureRetainsAllocatedSequenceAndRejectsTheFollowingEventWrite()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRuntime runtime = CreateRuntime(records, records.Sequence);
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(protocol, long.MaxValue);
        LogAssert.Expect(LogType.Error,
            "Cannot allocate domain EventId: The committed P12 record-sequence allocation could not advance the mutation epoch.");

        Assert.That(records.EventRecorder.Record((eventId, day, sequence) =>
            new NpcArrivedEvent(eventId, day, sequence, "actor", "location")), Is.False);
        Assert.That(new SimulationRecordSequenceCensusProvider(records.Sequence)
            .GetCurrentCensus().Revision, Is.EqualTo(1L),
            "the sequence increment committed before its notification failed and is not rolled back");
        Assert.That(records.Events.Events, Is.Empty,
            "the recorder stops before appending an event after the post-commit notification fails");
        AssertProtocolFaulted(runtime);
    }

    [Test]
    public void SelectedRuntimeRejectsASequenceDifferentFromItsDecisionRecorder()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SimulationRecordSequence differentSequence = new SimulationRecordSequence();

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            records.Time,
            null,
            null,
            decisionRecorder: records.DecisionRecorder,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: differentSequence));
    }

    private static SimulationRuntime CreateRuntime(
        RecordFixture records,
        SimulationRecordSequence sequence,
        RuntimeIdAllocator runtimeIdAllocator = null)
    {
        return new SimulationRuntime(
            records != null ? records.Time : new SimulationTime(),
            null,
            null,
            decisionRecorder: records != null ? records.DecisionRecorder : null,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: sequence,
            runtimeIdAllocator: runtimeIdAllocator);
    }

    private static NpcDecisionRecord RecordOccurrence(NpcDecisionRecorder recorder, string operationIdentity)
    {
        MethodInfo method = typeof(NpcDecisionRecorder).GetMethod(
            "TryRecordOccurrenceOnce", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments =
        {
            operationIdentity,
            "fingerprint-" + operationIdentity,
            "actor",
            NpcDecisionType.Action,
            NpcDecisionOrigin.Autonomous,
            "action",
            null,
            null,
            null
        };
        Assert.That((bool)method.Invoke(recorder, arguments), Is.True);
        return (NpcDecisionRecord)arguments[8];
    }

    private static void AssertEpoch(SimulationRuntime runtime, long expected)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long actual, out ContinuationCensusFailure failure), Is.True, failure.ToString());
        Assert.That(actual, Is.EqualTo(expected));
    }

    private static void AssertProtocolFaulted(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out _, out ContinuationCensusFailure epochFailure), Is.False);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }
}
