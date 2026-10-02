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

    private static SimulationRuntime CreateRuntime(RecordFixture records, SimulationRecordSequence sequence)
    {
        return new SimulationRuntime(
            records != null ? records.Time : new SimulationTime(),
            null,
            null,
            decisionRecorder: records != null ? records.DecisionRecorder : null,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: sequence);
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
