using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SimulationRuntimeAdmissionTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
        {
            if (simulationObject != null)
                UnityEngine.Object.DestroyImmediate(simulationObject);
        }
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DailyProfileScopesAdvancesAndRoutesOwnedClockThroughRuntime()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime("npc-admission", SimulationTestFactory.CreateNpc("admission"));
        NpcActionData action = SimulationTestFactory.CreateAction("admission-travel", NpcActionType.Travel);
        action.baseUtility = 1f;
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 1f });

        SimulationRuntime runtime = null;
        int operationObservations = 0;
        bool reentrantClockWasRejected = false;
        SimulationTimeAdvanceFailure reentrantClockFailure = SimulationTimeAdvanceFailure.None;
        AdmissionProbeActionProvider provider = new AdmissionProbeActionProvider(action, () =>
        {
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
            Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));
            reentrantClockWasRejected = !records.Time.TryAdvanceDay(out reentrantClockFailure);
            operationObservations++;
        });
        runtime = new SimulationRuntime(
            records.Time,
            null,
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(new System.Collections.Generic.List<INpcActionProvider> { provider }),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure before), Is.True, before.ToString());
        Assert.That(records.Time.TryAdvanceDay(out SimulationTimeAdvanceFailure directClockFailure), Is.True,
            directClockFailure.ToString());
        Assert.That(runtime.CurrentDay, Is.EqualTo(1L), "the runtime-owned clock dispatches a complete day advance");
        Assert.That(operationObservations, Is.EqualTo(1));
        Assert.That(reentrantClockWasRejected, Is.True);
        Assert.That(reentrantClockFailure, Is.EqualTo(SimulationTimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterDirectClock), Is.True,
            afterDirectClock.ToString());

        Assert.That(runtime.TryAdvanceDays(0, out int zeroDaysAdvanced, out SimulationRuntimeAdvanceFailure zeroFailure), Is.True);
        Assert.That(zeroDaysAdvanced, Is.Zero);
        Assert.That(zeroFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(operationObservations, Is.EqualTo(1), "the zero-day no-op does not enter an operation scope");
        Assert.That(runtime.TryAdvanceDays(-1, out _, out SimulationRuntimeAdvanceFailure invalidFailure), Is.False);
        Assert.That(invalidFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.InvalidDayCount));

        Assert.That(runtime.TryAdvanceDays(2, out int daysAdvanced, out SimulationRuntimeAdvanceFailure batchFailure), Is.True,
            batchFailure.ToString());
        Assert.That(daysAdvanced, Is.EqualTo(2));
        Assert.That(runtime.CurrentDay, Is.EqualTo(3L));
        Assert.That(operationObservations, Is.EqualTo(3), "one outer scope covers each full multi-day batch");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterBatch), Is.True,
            afterBatch.ToString());
    }

    [Test]
    public void CompletedDailyTokenRequiresSuccessfulPositiveAdvanceAndNoOpsPreserveIt()
    {
        SimulationTime time = new SimulationTime();
        SimulationRuntime runtime = CreatePublishedDailyCaptureRuntime(time);

        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out _, out DailyCaptureEligibilityFailure beforeAdvance), Is.False);
        Assert.That(beforeAdvance, Is.EqualTo(DailyCaptureEligibilityFailure.NoCompletedBoundary));
        Assert.That(runtime.TryAdvanceDays(0, out int zeroDays, out _), Is.True);
        Assert.That(zeroDays, Is.Zero);
        Assert.That(runtime.TryAdvanceDays(-1, out _, out SimulationRuntimeAdvanceFailure invalid), Is.False);
        Assert.That(invalid, Is.EqualTo(SimulationRuntimeAdvanceFailure.InvalidDayCount));
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out _, out DailyCaptureEligibilityFailure stillNoBoundary), Is.False);
        Assert.That(stillNoBoundary, Is.EqualTo(DailyCaptureEligibilityFailure.NoCompletedBoundary));

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvance), Is.True,
            firstAdvance.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken firstToken,
            out DailyCaptureEligibilityFailure firstReadFailure), Is.True, firstReadFailure.ToString());
        Assert.That(firstToken.AbsoluteDay, Is.EqualTo(1L));
        Assert.That(firstToken.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(firstToken.OwnerSections, Is.Not.Empty);
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(firstToken, out _), Is.True);

        Assert.That(runtime.TryAdvanceDays(0, out _, out _), Is.True);
        Assert.That(runtime.TryAdvanceDays(-2, out _, out _), Is.False);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure readOnlyAssessment), Is.True,
            readOnlyAssessment.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken afterNoOps,
            out DailyCaptureEligibilityFailure afterNoOpsFailure), Is.True, afterNoOpsFailure.ToString());
        Assert.That(afterNoOps, Is.SameAs(firstToken));

        Assert.That(time.TryAdvanceDay(out SimulationTimeAdvanceFailure directClockFailure), Is.True,
            directClockFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken directClockToken,
            out DailyCaptureEligibilityFailure directClockReadFailure), Is.True,
            directClockReadFailure.ToString());
        Assert.That(directClockToken, Is.Not.SameAs(firstToken));
        Assert.That(directClockToken.AbsoluteDay, Is.EqualTo(2L));
        Assert.That(directClockToken.CompletedCoreSequence, Is.EqualTo(2L));
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(firstToken, out DailyCaptureEligibilityFailure stale), Is.False);
        Assert.That(stale, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));

        Assert.That(runtime.TryAdvanceDays(3, out int daysAdvanced, out SimulationRuntimeAdvanceFailure batchFailure),
            Is.True, batchFailure.ToString());
        Assert.That(daysAdvanced, Is.EqualTo(3));
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken batchToken,
            out DailyCaptureEligibilityFailure batchReadFailure), Is.True, batchReadFailure.ToString());
        Assert.That(batchToken.AbsoluteDay, Is.EqualTo(5L));
        Assert.That(batchToken.CompletedCoreSequence, Is.EqualTo(5L));
    }

    [Test]
    public void UnadmittedRestoredOwnerVectorCaptureIsReadOnlyAndQuiescent()
    {
        SimulationRuntime runtime = CreateUnpublishedDailyCaptureRuntime(new SimulationTime(8L));

        Assert.That(runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> first,
            out long firstEpoch,
            out ContinuationCensusFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(first, Is.Not.Null.And.Not.Empty);
        Assert.That(firstEpoch, Is.Zero);
        AssertRestoredAdmissionWasNotPublished(runtime);

        Assert.That(runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> second,
            out long secondEpoch,
            out ContinuationCensusFailure secondFailure), Is.True, secondFailure.ToString());
        Assert.That(second, Is.Not.SameAs(first), "each capture returns a copied read-only vector");
        Assert.That(second.Count, Is.EqualTo(first.Count));
        Assert.That(secondEpoch, Is.EqualTo(firstEpoch));
        for (int i = 0; i < first.Count; i++)
        {
            Assert.That(second[i].SectionId, Is.EqualTo(first[i].SectionId));
            Assert.That(second[i].OwnerInstanceIdentity, Is.SameAs(first[i].OwnerInstanceIdentity));
            Assert.That(second[i].Cardinality, Is.EqualTo(first[i].Cardinality));
            Assert.That(second[i].Revision, Is.EqualTo(first[i].Revision));
        }
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out _, out _), Is.False,
            "reading the private target census cannot issue a completed-boundary token");
        Assert.That(ReadPrivateField<bool>(runtime, "factualReadWorldPublished"), Is.False);
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.Zero);
    }

    [Test]
    public void UnadmittedRestoredOwnerVectorCaptureRejectsActiveAdvanceAndWrongThread()
    {
        NpcActionData action = SimulationTestFactory.CreateAction("restore-vector-probe", NpcActionType.Travel);
        action.baseUtility = 1f;
        NpcRuntime npc = new NpcRuntime("restore-vector-probe", SimulationTestFactory.CreateNpc("restore-vector-probe"));
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 1f });

        SimulationRuntime runtime = null;
        bool capturedDuringAdvance = true;
        ContinuationCensusFailure advanceCaptureFailure = ContinuationCensusFailure.None;
        AdmissionProbeActionProvider provider = new AdmissionProbeActionProvider(action, () =>
        {
            capturedDuringAdvance = runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                out _, out _, out advanceCaptureFailure);
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider> { provider }),
            new[] { action });

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(capturedDuringAdvance, Is.False);
        Assert.That(advanceCaptureFailure, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));

        SimulationRuntime wrongThreadRuntime = CreateUnpublishedDailyCaptureRuntime(new SimulationTime(3L));
        bool capturedFromWorker = true;
        ContinuationCensusFailure workerFailure = ContinuationCensusFailure.None;
        Thread worker = new Thread(() =>
        {
            capturedFromWorker = wrongThreadRuntime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                out _, out _, out workerFailure);
        });
        worker.Start();
        worker.Join();
        Assert.That(capturedFromWorker, Is.False);
        Assert.That(workerFailure, Is.EqualTo(ContinuationCensusFailure.WrongOwnerThread));
        AssertRestoredAdmissionWasNotPublished(wrongThreadRuntime);
    }

    [Test]
    public void UnadmittedRestoredOwnerVectorCaptureRejectsPublishedCandidate()
    {
        SimulationRuntime runtime = CreatePublishedDailyCaptureRuntime(new SimulationTime(3L));

        Assert.That(runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> sections,
            out long mutationEpoch,
            out ContinuationCensusFailure failure), Is.False);
        Assert.That(sections, Is.Null);
        Assert.That(mutationEpoch, Is.Zero);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(ReadPrivateField<bool>(runtime, "factualReadWorldPublished"), Is.True);
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.Zero);
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(runtime, "currentDailyCaptureToken"), Is.Null);
    }

    [Test]
    public void RestoredDailyBoundaryAdmissionIssuesFreshTokenAndResumesSequence()
    {
        WorldId worldId = new WorldId(Guid.NewGuid());
        SimulationRuntime source = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(), worldId: worldId);
        Assert.That(source.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance), Is.True,
            sourceAdvance.ToString());
        Assert.That(source.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken sourceToken, out DailyCaptureEligibilityFailure sourceFailure), Is.True,
            sourceFailure.ToString());

        SimulationRuntime restored = CreateUnpublishedDailyCaptureRuntime(
            new SimulationTime(sourceToken.AbsoluteDay), worldId);
        Assert.That(restored.TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetSections,
            out long validatedTargetEpoch,
            out ContinuationCensusFailure targetCaptureFailure), Is.True,
            targetCaptureFailure.ToString());
        Assert.That(restored.TryAdmitRestoredDailyBoundary(
            worldId,
            sourceToken.AbsoluteDay,
            sourceToken.CompletedCoreSequence,
            validatedTargetSections,
            validatedTargetEpoch,
            out DailyCaptureEligibilityFailure admissionFailure), Is.True,
            admissionFailure.ToString());

        Assert.That(restored.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken restoredToken,
            out DailyCaptureEligibilityFailure restoredFailure), Is.True, restoredFailure.ToString());
        Assert.That(restoredToken, Is.Not.SameAs(sourceToken));
        Assert.That(restoredToken.RuntimeInstanceIdentity, Is.Not.SameAs(sourceToken.RuntimeInstanceIdentity));
        Assert.That(restoredToken.AdmissionContext, Is.SameAs(ReadPrivateField<SimulationRuntimeAdmissionContext>(
            restored, "runtimeAdmissionContext")));
        Assert.That(restoredToken.WorldId, Is.SameAs(worldId));
        Assert.That(restoredToken.AbsoluteDay, Is.EqualTo(sourceToken.AbsoluteDay));
        Assert.That(restoredToken.CompletedCoreSequence, Is.EqualTo(sourceToken.CompletedCoreSequence));
        Assert.That(restoredToken.OwnerSections, Is.SameAs(validatedTargetSections),
            "The admission token must retain the target vector that passed P12-G graph validation.");
        Assert.That(restoredToken.BoundaryProvenance,
            Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
        Assert.That(restoredToken.OwnerSections, Is.Not.Empty);
        Assert.That(restored.TryValidateCompletedDailyCaptureToken(restoredToken, out _), Is.True);
        Assert.That(source.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
            "admitting the staged runtime must not stale or transfer the source runtime's token");
        Assert.That(source.TryValidateCompletedDailyCaptureToken(restoredToken, out DailyCaptureEligibilityFailure crossRuntime), Is.False);
        Assert.That(crossRuntime, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));

        Assert.That(restored.TryAdvanceDay(out SimulationRuntimeAdvanceFailure resumedAdvance), Is.True,
            resumedAdvance.ToString());
        Assert.That(restored.CurrentDay, Is.EqualTo(sourceToken.AbsoluteDay + 1L));
        Assert.That(ReadPrivateField<long>(restored, "completedDailyCoreSequence"),
            Is.EqualTo(sourceToken.CompletedCoreSequence + 1L));
        Assert.That(restored.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken nextToken,
            out DailyCaptureEligibilityFailure nextFailure), Is.True, nextFailure.ToString());
        Assert.That(nextToken.BoundaryProvenance,
            Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance));
        Assert.That(nextToken.CompletedCoreSequence, Is.EqualTo(sourceToken.CompletedCoreSequence + 1L));
    }

    [Test]
    public void RestoredDailyBoundaryAdmissionRejectsWrongIdentityDayAndSequenceWithoutPartialPublication()
    {
        WorldId worldId = new WorldId(Guid.NewGuid());
        WorldId otherWorldId = new WorldId(Guid.NewGuid());
        SimulationRuntime wrongIdentityRuntime = CreateUnpublishedDailyCaptureRuntime(
            new SimulationTime(4L), worldId);
        Assert.That(wrongIdentityRuntime.TryAdmitRestoredDailyBoundary(
            otherWorldId, 4L, 7L, out DailyCaptureEligibilityFailure wrongIdentityFailure), Is.False);
        Assert.That(wrongIdentityFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
        AssertRestoredAdmissionWasNotPublished(wrongIdentityRuntime);

        SimulationRuntime wrongDayRuntime = CreateUnpublishedDailyCaptureRuntime(
            new SimulationTime(4L), worldId);
        Assert.That(wrongDayRuntime.TryAdmitRestoredDailyBoundary(
            worldId, 5L, 7L, out DailyCaptureEligibilityFailure wrongDayFailure), Is.False);
        Assert.That(wrongDayFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
        AssertRestoredAdmissionWasNotPublished(wrongDayRuntime);

        SimulationRuntime zeroSequenceRuntime = CreateUnpublishedDailyCaptureRuntime(
            new SimulationTime(4L), worldId);
        Assert.That(zeroSequenceRuntime.TryAdmitRestoredDailyBoundary(
            worldId, 4L, 0L, out DailyCaptureEligibilityFailure zeroSequenceFailure), Is.False);
        Assert.That(zeroSequenceFailure, Is.EqualTo(DailyCaptureEligibilityFailure.NoCompletedBoundary));
        AssertRestoredAdmissionWasNotPublished(zeroSequenceRuntime);

        SimulationRuntime negativeDayRuntime = CreateUnpublishedDailyCaptureRuntime(
            new SimulationTime(4L), worldId);
        Assert.That(negativeDayRuntime.TryAdmitRestoredDailyBoundary(
            worldId, -1L, 7L, out DailyCaptureEligibilityFailure negativeDayFailure), Is.False);
        Assert.That(negativeDayFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
        AssertRestoredAdmissionWasNotPublished(negativeDayRuntime);
    }

    [Test]
    public void RestoredDailyBoundaryAdmissionRejectsAlreadyPublishedOrAdvancedCandidateWithoutReplacement()
    {
        WorldId worldId = new WorldId(Guid.NewGuid());
        SimulationRuntime publishedRuntime = CreateUnpublishedDailyCaptureRuntime(new SimulationTime(2L), worldId);
        Assert.That(publishedRuntime.TryMarkWorldPublishedForFactualRead(), Is.True);
        Assert.That(publishedRuntime.TryAdmitRestoredDailyBoundary(
            worldId, 2L, 11L, out DailyCaptureEligibilityFailure publishedFailure), Is.False);
        Assert.That(publishedFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
        Assert.That(ReadPrivateField<bool>(publishedRuntime, "factualReadWorldPublished"), Is.True);
        Assert.That(ReadPrivateField<long>(publishedRuntime, "completedDailyCoreSequence"), Is.Zero);
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(publishedRuntime, "currentDailyCaptureToken"), Is.Null);

        SimulationRuntime advancedRuntime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(2L), worldId: worldId);
        Assert.That(advancedRuntime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(advancedRuntime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken existingToken, out _), Is.True);
        Assert.That(advancedRuntime.TryAdmitRestoredDailyBoundary(
            worldId, 2L, 11L, out DailyCaptureEligibilityFailure advancedFailure), Is.False);
        Assert.That(advancedFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
        Assert.That(ReadPrivateField<long>(advancedRuntime, "completedDailyCoreSequence"), Is.EqualTo(1L));
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(advancedRuntime, "currentDailyCaptureToken"),
            Is.SameAs(existingToken));
        Assert.That(advancedRuntime.TryValidateCompletedDailyCaptureToken(existingToken, out _), Is.True);
    }

    [Test]
    public void RestoredDailyBoundaryAdmissionRejectsActiveOperationAndWrongThreadWithoutMutation()
    {
        WorldId worldId = new WorldId(Guid.NewGuid());
        SimulationRuntime activeOperationRuntime = CreateUnpublishedDailyCaptureRuntime(new SimulationTime(3L), worldId);
        Assert.That(activeOperationRuntime.TryBeginBootstrapPublicationScope(out SimulationOperationScope activeScope), Is.True);
        using (activeScope)
        {
            Assert.That(activeOperationRuntime.TryAdmitRestoredDailyBoundary(
                worldId, 3L, 9L, out DailyCaptureEligibilityFailure operationFailure), Is.False);
            Assert.That(operationFailure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
            AssertRestoredAdmissionWasNotPublished(activeOperationRuntime);
        }

        SimulationRuntime wrongThreadRuntime = CreateUnpublishedDailyCaptureRuntime(new SimulationTime(3L), worldId);
        bool admittedFromWorker = true;
        DailyCaptureEligibilityFailure workerFailure = DailyCaptureEligibilityFailure.None;
        Thread worker = new Thread(() => admittedFromWorker = wrongThreadRuntime.TryAdmitRestoredDailyBoundary(
            worldId, 3L, 9L, out workerFailure));
        worker.Start();
        worker.Join();

        Assert.That(admittedFromWorker, Is.False);
        Assert.That(workerFailure, Is.EqualTo(DailyCaptureEligibilityFailure.WrongOwnerThread));
        AssertRestoredAdmissionWasNotPublished(wrongThreadRuntime);
    }

    [Test]
    public void RestoredDailyBoundaryAdmissionRejectsNonDailyProfile()
    {
        WorldId worldId = new WorldId(Guid.NewGuid());
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(3L), null, null, worldId: worldId);

        Assert.That(runtime.TryAdmitRestoredDailyBoundary(
            worldId, 3L, 9L, out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.UnsupportedProfile));
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.Zero);
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(runtime, "currentDailyCaptureToken"), Is.Null);
    }

    [Test]
    public void CompletedDailyTokenIsUnavailableDuringCallbacksAndBatchPublishesOnlyOnceAtReturn()
    {
        NpcActionData action = SimulationTestFactory.CreateAction("daily-token-probe", NpcActionType.Travel);
        action.baseUtility = 1f;
        NpcRuntime npc = new NpcRuntime("npc-daily-token-probe", SimulationTestFactory.CreateNpc("daily-token-probe"));
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 1f });

        SimulationRuntime runtime = null;
        int callbackCount = 0;
        AdmissionProbeActionProvider provider = new AdmissionProbeActionProvider(action, () =>
        {
            Assert.That(runtime.TryGetCompletedDailyCaptureToken(
                out _, out DailyCaptureEligibilityFailure failure), Is.False);
            Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
            callbackCount++;
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider> { provider }),
            new[] { action });

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstFailure), Is.True,
            firstFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out _, out _), Is.True);
        Assert.That(runtime.TryAdvanceDays(2, out int daysAdvanced, out SimulationRuntimeAdvanceFailure batchFailure),
            Is.True, batchFailure.ToString());
        Assert.That(daysAdvanced, Is.EqualTo(2));
        Assert.That(callbackCount, Is.EqualTo(3));
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure readFailure), Is.True, readFailure.ToString());
        Assert.That(token.AbsoluteDay, Is.EqualTo(3L));
        Assert.That(token.CompletedCoreSequence, Is.EqualTo(3L));
    }

    [Test]
    public void PartialBatchExceptionKeepsOnlyNormallyCompletedCoreSequenceAndIssuesNoToken()
    {
        NpcActionData action = SimulationTestFactory.CreateAction("daily-token-throw", NpcActionType.Travel);
        action.baseUtility = 1f;
        NpcRuntime npc = new NpcRuntime("npc-daily-token-throw", SimulationTestFactory.CreateNpc("daily-token-throw"));
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 1f });

        SimulationRuntime runtime = null;
        int callbackCount = 0;
        AdmissionProbeActionProvider provider = new AdmissionProbeActionProvider(action, () =>
        {
            callbackCount++;
            if (callbackCount == 2) throw new InvalidOperationException("injected second-day callback failure");
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider> { provider }),
            new[] { action });

        Assert.Throws<InvalidOperationException>(() => runtime.TryAdvanceDays(
            2, out _, out SimulationRuntimeAdvanceFailure _));

        Assert.That(runtime.CurrentDay, Is.EqualTo(2L), "the failed second core may already have advanced time");
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.EqualTo(1L));
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(runtime, "currentDailyCaptureToken"), Is.Null);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out _, out DailyCaptureEligibilityFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.RuntimeFaulted));
    }

    [Test]
    public void SupportedSameDayRosterMutationInvalidatesTokenAndTokensCannotCrossRuntimes()
    {
        NpcRuntime initialNpc = new NpcRuntime("npc-before-token-mutation", SimulationTestFactory.CreateNpc("before-token"));
        SimulationRuntime runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(), new[] { initialNpc });
        Assert.That(runtime.TryAdvanceDay(out _), Is.True);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken original,
            out DailyCaptureEligibilityFailure originalFailure), Is.True, originalFailure.ToString());

        NpcRuntime addedNpc = new NpcRuntime("npc-after-token-mutation", SimulationTestFactory.CreateNpc("after-token"));
        Assert.That(runtime.TryRegisterNpc(addedNpc, out WorldNpcRegistryFailure registerFailure), Is.True,
            registerFailure.ToString());
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(original, out DailyCaptureEligibilityFailure changed), Is.False);
        Assert.That(changed, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));

        SimulationRuntime secondRuntime = CreatePublishedDailyCaptureRuntime(new SimulationTime());
        Assert.That(secondRuntime.TryAdvanceDay(out _), Is.True);
        Assert.That(secondRuntime.TryValidateCompletedDailyCaptureToken(original, out DailyCaptureEligibilityFailure crossRuntime), Is.False);
        Assert.That(crossRuntime, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
    }

    [Test]
    public void WrongThreadTokenValidationFaultsAdmissionAndSequenceOverflowRejectsBeforeClockWrite()
    {
        SimulationRuntime wrongThreadRuntime = CreatePublishedDailyCaptureRuntime(new SimulationTime());
        Assert.That(wrongThreadRuntime.TryAdvanceDay(out _), Is.True);
        Assert.That(wrongThreadRuntime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token, out _), Is.True);
        bool validFromWorker = true;
        DailyCaptureEligibilityFailure workerFailure = DailyCaptureEligibilityFailure.None;
        Thread worker = new Thread(() => validFromWorker = wrongThreadRuntime.TryValidateCompletedDailyCaptureToken(
            token, out workerFailure));
        worker.Start();
        worker.Join();
        Assert.That(validFromWorker, Is.False);
        Assert.That(workerFailure, Is.EqualTo(DailyCaptureEligibilityFailure.WrongOwnerThread));
        Assert.That(wrongThreadRuntime.TryGetCompletedDailyCaptureToken(out _, out DailyCaptureEligibilityFailure faulted), Is.False);
        Assert.That(faulted, Is.EqualTo(DailyCaptureEligibilityFailure.RuntimeFaulted));

        SimulationRuntime overflowRuntime = CreatePublishedDailyCaptureRuntime(new SimulationTime());
        WritePrivateField(overflowRuntime, "completedDailyCoreSequence", long.MaxValue - 2L);
        Assert.That(overflowRuntime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure finalAvailableAdvance), Is.True,
            finalAvailableAdvance.ToString());
        Assert.That(overflowRuntime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken priorToken, out DailyCaptureEligibilityFailure priorTokenFailure),
            Is.True, priorTokenFailure.ToString());
        Assert.That(priorToken.CompletedCoreSequence, Is.EqualTo(long.MaxValue - 1L));
        Assert.That(overflowRuntime.TryAdvanceDays(2, out _, out SimulationRuntimeAdvanceFailure overflow), Is.False);
        Assert.That(overflow, Is.EqualTo(SimulationRuntimeAdvanceFailure.CompletedBoundarySequenceOverflow));
        Assert.That(overflowRuntime.CurrentDay, Is.EqualTo(1L));
        Assert.That(ReadPrivateField<long>(overflowRuntime, "completedDailyCoreSequence"), Is.EqualTo(long.MaxValue - 1L));
        Assert.That(overflowRuntime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken afterOverflow, out DailyCaptureEligibilityFailure afterOverflowFailure),
            Is.True, afterOverflowFailure.ToString());
        Assert.That(afterOverflow, Is.SameAs(priorToken), "a clean sequence-capacity preflight preserves the prior valid boundary");
    }

    [Test]
    public void DailyCaptureTokenSurvivesCleanAbsoluteDayOverflowPreflight()
    {
        SimulationRuntime runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(long.MaxValue - 1L));

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstFailure), Is.True,
            firstFailure.ToString());
        Assert.That(runtime.CurrentDay, Is.EqualTo(long.MaxValue));
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken priorToken,
            out DailyCaptureEligibilityFailure priorFailure), Is.True, priorFailure.ToString());
        Assert.That(priorToken.AbsoluteDay, Is.EqualTo(long.MaxValue));
        Assert.That(priorToken.CompletedCoreSequence, Is.EqualTo(1L));

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure overflowFailure), Is.False);
        Assert.That(overflowFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow));
        Assert.That(runtime.CurrentDay, Is.EqualTo(long.MaxValue));
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.EqualTo(1L));
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(
            priorToken, out DailyCaptureEligibilityFailure validateFailure), Is.True,
            validateFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken retainedToken,
            out DailyCaptureEligibilityFailure retainedFailure), Is.True, retainedFailure.ToString());
        Assert.That(retainedToken, Is.SameAs(priorToken));
    }

    [Test]
    public void DailyCapturePartialBatchAtAbsoluteDayOverflowPublishesNoToken()
    {
        SimulationRuntime runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(long.MaxValue - 2L));

        Assert.That(runtime.TryAdvanceDays(
            3, out int daysAdvanced, out SimulationRuntimeAdvanceFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow));
        Assert.That(daysAdvanced, Is.EqualTo(2));
        Assert.That(runtime.CurrentDay, Is.EqualTo(long.MaxValue));
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.EqualTo(2L));
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(runtime, "currentDailyCaptureToken"), Is.Null);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out _, out DailyCaptureEligibilityFailure tokenFailure), Is.False);
        Assert.That(tokenFailure, Is.EqualTo(DailyCaptureEligibilityFailure.NoCompletedBoundary));
    }
    [Test]
    public void CompletedDailyTokenRejectsFinalCensusMismatchAfterCompletedBatchCores()
    {
        NpcActionData action = SimulationTestFactory.CreateAction("daily-token-final-census", NpcActionType.Travel);
        action.baseUtility = 1f;
        NpcRuntime npc = new NpcRuntime("npc-daily-token-final-census", SimulationTestFactory.CreateNpc("final-census"));
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 1f });

        SimulationRuntime runtime = null;
        ContinuationCensusProtocol protocol = null;
        int callbackCount = 0;
        bool removedSection = false;
        AdmissionProbeActionProvider provider = new AdmissionProbeActionProvider(action, () =>
        {
            callbackCount++;
            if (callbackCount != 2) return;

            Dictionary<string, OwnerSectionContract> expectedSections =
                ReadPrivateField<Dictionary<string, OwnerSectionContract>>(protocol, "expectedSections");
            string sectionId = null;
            foreach (string candidate in expectedSections.Keys)
            {
                sectionId = candidate;
                break;
            }

            Assert.That(sectionId, Is.Not.Null.And.Not.Empty);
            Assert.That(expectedSections.Remove(sectionId), Is.True);
            removedSection = true;
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider> { provider }),
            new[] { action });
        protocol = ReadPrivateField<ContinuationCensusProtocol>(runtime, "npcRosterCensusProtocol");

        bool advanced = runtime.TryAdvanceDays(
            2,
            out int daysAdvanced,
            out SimulationRuntimeAdvanceFailure failure);

        Assert.That(removedSection, Is.True);
        Assert.That(callbackCount, Is.EqualTo(2));
        Assert.That(advanced, Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(daysAdvanced, Is.EqualTo(2), "both daily cores completed before final census publication was rejected");
        Assert.That(runtime.CurrentDay, Is.EqualTo(2L));
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.EqualTo(2L));
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(runtime, "currentDailyCaptureToken"), Is.Null);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out _, out DailyCaptureEligibilityFailure tokenFailure), Is.False);
        Assert.That(tokenFailure, Is.EqualTo(DailyCaptureEligibilityFailure.RuntimeFaulted));
    }

    [Test]
    public void CompletedDailyTokenIsNotPublishedAfterOperationScopeDisposalFault()
    {
        NpcActionData action = SimulationTestFactory.CreateAction("daily-token-scope-disposal", NpcActionType.Travel);
        action.baseUtility = 1f;
        NpcRuntime npc = new NpcRuntime("npc-daily-token-scope-disposal", SimulationTestFactory.CreateNpc("scope-disposal"));
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 1f });

        SimulationRuntime runtime = null;
        ContinuationCensusProtocol protocol = null;
        int callbackCount = 0;
        AdmissionProbeActionProvider provider = new AdmissionProbeActionProvider(action, () =>
        {
            callbackCount++;
            WritePrivateField(protocol, "activeOperationCount", 0);
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider> { provider }),
            new[] { action });
        protocol = ReadPrivateField<ContinuationCensusProtocol>(runtime, "npcRosterCensusProtocol");

        bool advanced = runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure);

        Assert.That(callbackCount, Is.EqualTo(1));
        Assert.That(advanced, Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(1L), "the domain core completed before scope disposal detected the accounting fault");
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.EqualTo(1L));
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(runtime, "currentDailyCaptureToken"), Is.Null);
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out _, out DailyCaptureEligibilityFailure tokenFailure), Is.False);
        Assert.That(tokenFailure, Is.EqualTo(DailyCaptureEligibilityFailure.RuntimeFaulted));
    }

    [Test]
    public void CompletedDailyTokenBindsOwnerRevisionAndMutationEpochWitnesses()
    {
        SimulationRuntime ownerRuntime = CreatePublishedDailyCaptureRuntime(new SimulationTime());
        Assert.That(ownerRuntime.TryAdvanceDay(out _), Is.True);
        Assert.That(ownerRuntime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken ownerToken, out _), Is.True);

        List<OwnerSectionCensusSnapshot> ownerSections = new List<OwnerSectionCensusSnapshot>(ownerToken.OwnerSections);
        Assert.That(ownerSections, Is.Not.Empty);
        OwnerSectionCensusSnapshot first = ownerSections[0];
        ownerSections[0] = new OwnerSectionCensusSnapshot(
            first.SectionId,
            first.SchemaVersion,
            first.Role,
            first.OwnerInstanceIdentity,
            first.Cardinality,
            first.Revision + 1L);
        WritePrivateField(ownerToken, "<OwnerSections>k__BackingField", Array.AsReadOnly(ownerSections.ToArray()));

        Assert.That(ownerRuntime.TryValidateCompletedDailyCaptureToken(
            ownerToken, out DailyCaptureEligibilityFailure ownerFailure), Is.False);
        Assert.That(ownerFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));

        SimulationRuntime epochRuntime = CreatePublishedDailyCaptureRuntime(new SimulationTime());
        Assert.That(epochRuntime.TryAdvanceDay(out _), Is.True);
        Assert.That(epochRuntime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken epochToken, out _), Is.True);
        WritePrivateField(epochToken, "<MutationEpoch>k__BackingField", epochToken.MutationEpoch + 1L);

        Assert.That(epochRuntime.TryValidateCompletedDailyCaptureToken(
            epochToken, out DailyCaptureEligibilityFailure epochFailure), Is.False);
        Assert.That(epochFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
    }

    [Test]
    public void CompletedDailyTokenIsUnavailableInsideEveryRegisteredOperationScope()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("daily-token-operation-scope-probes");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        SimulationRuntime runtime = simulation.Runtime;
        Assert.That(runtime, Is.Not.Null);
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token, out DailyCaptureEligibilityFailure tokenFailure), Is.True,
            tokenFailure.ToString());

        ContinuationCensusProtocol protocol = ReadPrivateField<ContinuationCensusProtocol>(runtime, "npcRosterCensusProtocol");
        string[] operationIds =
        {
            "runtime.npc-membership",
            "runtime.bootstrap-publication",
            "runtime.advance-day",
            "runtime.travel.start",
            "runtime.travel-party.start",
            "runtime.travel-party.advance",
            "runtime.economy.npc-trade",
            "runtime.economy.money-transfer",
            "runtime.economy.market-purchase",
            "runtime.economy.market-sale",
            "runtime.merchant.advance-npc-trade-state",
            "p12.institution-office.owner-commit",
            "p12.faction.owner-commit",
            "p12.political-claim.owner-commit",
            "p12.political-support.owner-commit",
            "p12.property.owner-commit",
            "p12.estate.owner-commit",
            "runtime.population.immigration",
            "runtime.population.emigration",
            "runtime.population.resident-death",
            "runtime.population.residence-migration",
            "runtime.person.death",
            "runtime.person.residence-bind",
            "runtime.person.parentage"
        };
        HashSet<string> registeredOperations = ReadPrivateField<HashSet<string>>(protocol, "expectedOperations");
        CollectionAssert.AreEquivalent(operationIds, registeredOperations,
            "the probes must cover the full currently registered Daily-v1 operation inventory");

        MethodInfo beginRuntimeOperation = typeof(SimulationRuntime).GetMethod(
            "TryEnterRuntimeAdmissionOperation",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo beginMembershipOperation = typeof(SimulationRuntime).GetMethod(
            "BeginNpcMembershipCensusScope",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(beginRuntimeOperation, Is.Not.Null);
        Assert.That(beginMembershipOperation, Is.Not.Null);

        foreach (string operationId in operationIds)
        {
            IDisposable scope;
            if (string.Equals(operationId, "runtime.npc-membership", StringComparison.Ordinal))
            {
                scope = (IDisposable)beginMembershipOperation.Invoke(runtime, null);
                Assert.That(scope, Is.Not.Null, operationId);
            }
            else
            {
                object[] arguments = { operationId, null };
                bool entered = (bool)beginRuntimeOperation.Invoke(runtime, arguments);
                Assert.That(entered, Is.True, operationId);
                scope = arguments[1] as IDisposable;
                Assert.That(scope, Is.Not.Null, operationId);
            }

            using (scope)
            {
                Assert.That(runtime.TryGetCompletedDailyCaptureToken(
                    out _, out DailyCaptureEligibilityFailure inScopeFailure), Is.False, operationId);
                Assert.That(inScopeFailure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress), operationId);
            }

            Assert.That(runtime.TryValidateCompletedDailyCaptureToken(
                token, out DailyCaptureEligibilityFailure afterScopeFailure), Is.True,
                operationId + ": " + afterScopeFailure);
        }
    }
    [Test]
    public void DailyProfileAdmissionAcceptsExactInitialSpatialIdentityAndLegacyNetworkCardinalities()
    {
        SimulationRuntime runtime = CreateSpatialAdmissionRuntime();

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True,
            failure.ToString());
    }

    [Test]
    public void DailyProfileAdmissionRejectsZeroForRequiredP8AGeography()
    {
        AssertDailyProfileAdmissionRejected(() => CreateSpatialAdmissionRuntime(
            spatialAuthorityStore: new SpatialAuthorityStore()));
    }

    [Test]
    public void DailyProfileAdmissionRejectsExtraRequiredP8AHex()
    {
        AssertDailyProfileAdmissionRejected(() => CreateSpatialAdmissionRuntime(
            spatialAuthorityStore: CreateAdjacentSpatialAuthority()));
    }

    [TestCase(0)]
    [TestCase(9)]
    public void DailyProfileAdmissionRejectsWrongRequiredRuntimeIdentityNpcCardinality(int npcCount)
    {
        AssertDailyProfileAdmissionRejected(() => CreateSpatialAdmissionRuntime(npcCount: npcCount));
    }

    [Test]
    public void DailyProfileAdmissionRejectsZeroForRequiredLegacySpatialNetworkSection()
    {
        AssertDailyProfileAdmissionRejected(() => CreateSpatialAdmissionRuntime(populateLegacyNetwork: false));
    }

    [Test]
    public void DailyProfileAdmissionRejectsWrongRequiredLegacySpatialNetworkRouteCardinality()
    {
        AssertDailyProfileAdmissionRejected(() => CreateSpatialAdmissionRuntime(legacyNetworkRouteCount: 1));
    }

    [Test]
    public void FixedOwnerAdmissionRejectsPopulatedExplicitlyEmptyP8BAndP8CSections()
    {
        SpatialAuthorityStore passageAuthority = CreateAdjacentSpatialAuthority();
        HexBoundaryKey boundary = new HexBoundaryKey(new HexId("admission-hex-a"), new HexId("admission-hex-b"));
        Assert.That(passageAuthority.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(
                TraversalOptionRef.ForConnection(new ConnectionId("admission-connection")),
                boundary,
                "admission-road",
                "v1"),
            PassageCondition.Available,
            out SpatialAuthorityFailure passageFailure), Is.True, passageFailure?.ToString());
        Assert.That(passageAuthority.TryRegisterCrossing(
            new CrossingRecord(
                new CrossingId("admission-crossing"),
                boundary,
                new HexId("admission-hex-a"),
                "admission-bridge",
                "v1",
                null),
            out SpatialAuthorityFailure crossingFailure), Is.True, crossingFailure?.ToString());

        SpatialAuthorityStore singleLocationAuthority = CreateSingleLocationSpatialAuthority();
        LegacySpatialAnchorBindingStore populatedAnchors =
            new LegacySpatialAnchorBindingStore(singleLocationAuthority);
        Assert.That(populatedAnchors.TryBindCity(
            "admission-city",
            new LocationId("admission-location"),
            out SpatialAnchorBindingFailure anchorFailure), Is.True, anchorFailure?.ToString());

        PersonStore people = new PersonStore();
        PersonId personId = new PersonId("admission-person");
        Assert.That(people.TryRegister(new PersonRuntime(personId), out PersonStoreFailure personFailure), Is.True,
            personFailure.ToString());
        PersonSpatialPositionStore populatedPositions = new PersonSpatialPositionStore(
            people,
            singleLocationAuthority,
            new SpatialPassageTraversalOptionResolver(singleLocationAuthority.PassageAuthority));
        Assert.That(populatedPositions.TrySetAt(
            personId,
            StablePositionReference.ForLocation(new LocationId("admission-location")),
            out PersonSpatialPositionFailure positionFailure), Is.True, positionFailure.ToString());

        AssertFixedOwnerAdmissionRejectsNonzero(
            new SpatialPassageStateCensusProvider(passageAuthority),
            passageAuthority.PassageAuthority);
        AssertFixedOwnerAdmissionRejectsNonzero(
            new SpatialCrossingCensusProvider(passageAuthority),
            passageAuthority);
        AssertFixedOwnerAdmissionRejectsNonzero(
            new LegacySpatialAnchorBindingCensusProvider(populatedAnchors),
            populatedAnchors);
        AssertFixedOwnerAdmissionRejectsNonzero(
            new PersonSpatialPositionCensusProvider(populatedPositions),
            populatedPositions);
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void FullDailyProfileReceiptInventoryFailsClosedWhenEitherOwnerIsMissing(
        bool includeDecisionRecorder,
        bool includeEconomyTransactionService)
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();

        Assert.Throws<System.InvalidOperationException>(() => new SimulationRuntime(
            records.Time,
            null,
            null,
            decisionRecorder: includeDecisionRecorder ? records.DecisionRecorder : null,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            economyTransactionService: includeEconomyTransactionService
                ? new EconomyTransactionService()
                : null,
            requireP12ReceiptCensusOwners: true));
    }

    [TestCase("decision")]
    [TestCase("economy")]
    public void FullDailyProfileReceiptInventoryFailsClosedWhenReceiptOwnerStartsPopulated(string receiptOwner)
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        EconomyTransactionService economyTransactionService = new EconomyTransactionService();
        object owner = receiptOwner == "decision"
            ? (object)records.DecisionRecorder
            : economyTransactionService;
        string stateField = receiptOwner == "decision" ? "occurrenceReceipts" : "keyedSaleReceipts";
        object collection = owner.GetType()
            .GetField(stateField, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(owner);
        if (collection is System.Collections.IDictionary dictionary)
            dictionary.Add("p12-exact-zero-receipt-test", null);
        else
            ((System.Collections.IList)collection).Add(null);

        Assert.Throws<System.InvalidOperationException>(() => new SimulationRuntime(
            records.Time,
            null,
            null,
            decisionRecorder: records.DecisionRecorder,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            economyTransactionService: economyTransactionService,
            requireP12ReceiptCensusOwners: true));
    }

    [TestCase("decision", "revision")]
    [TestCase("economy", "revision")]
    [TestCase("decision", "cardinality")]
    [TestCase("economy", "cardinality")]
    public void ExactZeroReceiptEvidenceDriftFailsRuntimeCensusAssessment(
        string receiptOwner,
        string driftKind)
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        EconomyTransactionService economyTransactionService = new EconomyTransactionService();
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            null,
            null,
            decisionRecorder: records.DecisionRecorder,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            economyTransactionService: economyTransactionService,
            requireP12ReceiptCensusOwners: true);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialFailure), Is.True,
            initialFailure.ToString());

        object owner = receiptOwner == "decision"
            ? (object)records.DecisionRecorder
            : economyTransactionService;
        string stateField = receiptOwner == "decision"
            ? "occurrenceReceipts"
            : "keyedSaleReceipts";
        string revisionField = receiptOwner == "decision"
            ? "occurrenceReceiptsRevision"
            : "keyedSaleReceiptsRevision";
        if (driftKind == "revision")
        {
            FieldInfo revision = owner.GetType().GetField(
                revisionField,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(revision, Is.Not.Null);
            revision.SetValue(owner, (long)revision.GetValue(owner) + 1L);
        }
        else
        {
            FieldInfo state = owner.GetType().GetField(
                stateField,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(state, Is.Not.Null);
            object collection = state.GetValue(owner);
            if (collection is System.Collections.IDictionary dictionary)
                dictionary.Add("p12-exact-zero-receipt-test", null);
            else
                ((System.Collections.IList)collection).Add(null);
        }

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure driftFailure), Is.False);
        Assert.That(driftFailure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [TestCase("wrong-section")]
    [TestCase("wrong-schema")]
    [TestCase("unstable-owner")]
    [TestCase("populated")]
    [TestCase("missing-witness")]
    public void ExactZeroReceiptRegistrationRejectsMalformedProviders(string defect)
    {
        string sectionId = NpcDecisionRecorder.OccurrenceReceiptSectionId;
        int schemaVersion = NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion;
        object firstOwner = new object();
        OwnerSectionCensusWitness first = CreateReceiptWitness(sectionId, schemaVersion, firstOwner, 0, 0L);
        OwnerSectionCensusWitness second = first;
        if (defect == "wrong-section")
            first = second = CreateReceiptWitness(sectionId + "/unexpected", schemaVersion, firstOwner, 0, 0L);
        else if (defect == "wrong-schema")
            first = second = CreateReceiptWitness(sectionId, schemaVersion + 1, firstOwner, 0, 0L);
        else if (defect == "unstable-owner")
            second = CreateReceiptWitness(sectionId, schemaVersion, new object(), 0, 0L);
        else if (defect == "populated")
            first = second = CreateReceiptWitness(sectionId, schemaVersion, firstOwner, 1, 0L);
        else if (defect == "missing-witness")
            first = second = null;

        ReceiptCensusProviderStub provider = new ReceiptCensusProviderStub(first, second);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        MethodInfo register = typeof(SimulationRuntime).GetMethod(
            "TryRegisterP12ExplicitlyEmptyReceiptOwner",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(register, Is.Not.Null);

        object registered = register.Invoke(null, new object[] { protocol, provider, sectionId, schemaVersion });

        Assert.That(registered, Is.EqualTo(false));
    }

    private static OwnerSectionCensusWitness CreateReceiptWitness(
        string sectionId,
        int schemaVersion,
        object ownerIdentity,
        int cardinality,
        long revision)
    {
        ConstructorInfo constructor = typeof(OwnerSectionCensusWitness).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(int), typeof(object), typeof(int), typeof(long) },
            null);
        Assert.That(constructor, Is.Not.Null);
        return (OwnerSectionCensusWitness)constructor.Invoke(new object[]
        {
            sectionId,
            schemaVersion,
            ownerIdentity,
            cardinality,
            revision
        });
    }

    private sealed class ReceiptCensusProviderStub : IOwnerSectionCensusProvider
    {
        private readonly OwnerSectionCensusWitness first;
        private readonly OwnerSectionCensusWitness second;
        private int reads;

        public ReceiptCensusProviderStub(
            OwnerSectionCensusWitness first,
            OwnerSectionCensusWitness second)
        {
            this.first = first;
            this.second = second;
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            return reads++ == 0 ? first : second;
        }
    }

    [Test]
    public void DailyKnowledgeWritersRefreshOwnerBaselinesBeforeMerchantRosterCall()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-merchant-daily-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-merchant-daily-city",
            "p12-merchant-daily-location",
            SimulationTestFactory.CreateMarketItem(item, 8, 10));
        NpcRuntime merchant = new NpcRuntime(
            "p12-merchant-daily-actor",
            SimulationTestFactory.CreateNpc("p12-merchant-daily-actor", NpcJobType.Merchant),
            city,
            100f);
        SimulationTime time = new SimulationTime();
        EffectiveSimulationConfiguration defaults = SimulationConfigurationDefaults.Create();
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            defaults.Population,
            new EffectiveEconomyConfiguration(false),
            defaults.Travel,
            defaults.Crime,
            defaults.GuardCrime,
            defaults.NaturalMortality,
            defaults.AggregateDemography,
            new EffectiveMerchantTradeConfiguration(enabled: true),
            defaults.CommercialKnowledge);
        MerchantSystem merchantSystem = new MerchantSystem(
            new EffectiveMerchantTradeConfiguration(enabled: true),
            defaults.CommercialKnowledge,
            null,
            time,
            null);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { merchant },
            merchantSystem: merchantSystem,
            configuration: configuration,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        const int dayCount = 100;
        Assert.That(runtime.TryAdvanceDays(dayCount, out int advanced, out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(advanced, Is.EqualTo(dayCount));
        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.Location.RuntimeId), Is.True);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            city.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation observation), Is.True);
        Assert.That(observation.ObservedDay, Is.EqualTo(dayCount));
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            city.Location.RuntimeId, out CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.EqualTo(dayCount));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 3 + ((dayCount - 1) * 2)),
            "the first daily refresh commits SpatialKnowledge once and CommercialKnowledge twice; each later refresh updates the two shared-revision Commercial sections before the same-day Merchant roster call");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void DirectPlanOwnerCommitsOutsideMerchantOperationRefreshTheirBaselinesImmediately()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-direct-plan-owner-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-direct-plan-owner-city",
            "p12-direct-plan-owner-location");
        NpcRuntime merchant = new NpcRuntime(
            "p12-direct-plan-owner-actor",
            SimulationTestFactory.CreateNpc("p12-direct-plan-owner-actor", NpcJobType.Merchant),
            city,
            100f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { merchant },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());

        merchant.MerchantTradePlan.Set(item, city, city, 2, 4f);

        Assert.That(merchant.MerchantTradePlan.Revision, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long afterMerchantPlan, out ContinuationCensusFailure merchantPlanFailure),
            Is.True, merchantPlanFailure.ToString());
        Assert.That(afterMerchantPlan, Is.EqualTo(before + 1),
            "a committed direct merchant-plan write outside the nested operation immediately advances its owner section");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterMerchantPlanAssessment),
            Is.True, afterMerchantPlanAssessment.ToString());

        merchant.TravelPlan.Set(city, NpcTravelReason.Trade, 1f, 2f, "direct-plan-owner-decision");

        Assert.That(merchant.TravelPlan.Revision, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long afterTravelPlan, out ContinuationCensusFailure travelPlanFailure),
            Is.True, travelPlanFailure.ToString());
        Assert.That(afterTravelPlan, Is.EqualTo(afterMerchantPlan + 1),
            "a committed direct travel-plan write outside the nested operation immediately advances its owner section");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void MerchantOperationBatchesChangedOwnerSectionsOnceAndHoldsScopeThroughCommit()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ItemData item = SimulationTestFactory.CreateItem("p12-merchant-operation-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-merchant-operation-city",
            "p12-merchant-operation-location",
            SimulationTestFactory.CreateMarketItem(item, 8, 10));
        CityRuntime staleTarget = SimulationTestFactory.CreateCity(
            "p12-merchant-operation-stale-target",
            "p12-merchant-operation-stale-target-location");
        NpcRuntime merchant = new NpcRuntime(
            "p12-merchant-operation-actor",
            SimulationTestFactory.CreateNpc(
                "p12-merchant-operation-actor", NpcJobType.Merchant, MerchantBehavior.Local),
            city,
            100f);
        merchant.MerchantTradePlan.Set(item, city, city, 2, 4f);
        merchant.TravelPlan.Set(staleTarget, NpcTravelReason.Trade, 20f, 1f, "existing-trade-intent");
        SimulationTime time = records.Time;
        EffectiveSimulationConfiguration defaults = SimulationConfigurationDefaults.Create();
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            defaults.Population,
            new EffectiveEconomyConfiguration(false),
            defaults.Travel,
            defaults.Crime,
            defaults.GuardCrime,
            defaults.NaturalMortality,
            defaults.AggregateDemography,
            new EffectiveMerchantTradeConfiguration(enabled: true),
            defaults.CommercialKnowledge);
        MerchantSystem merchantSystem = new MerchantSystem(
            new EffectiveMerchantTradeConfiguration(enabled: true),
            defaults.CommercialKnowledge,
            null,
            time,
            records.DecisionRecorder);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            new[] { city },
            new[] { merchant },
            decisionRecorder: records.DecisionRecorder,
            merchantSystem: merchantSystem,
            configuration: configuration,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        MethodInfo beginOperation = typeof(SimulationRuntime).GetMethod(
            "TryBeginP12MerchantDailyNpcTradeOperation",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(beginOperation, Is.Not.Null);
        object[] arguments = { merchant, null };
        Assert.That((bool)beginOperation.Invoke(runtime, arguments), Is.True);
        System.IDisposable operationScope = (System.IDisposable)arguments[1];
        Assert.That(operationScope, Is.Not.Null);
        using (operationScope)
        {
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure activeFailure), Is.False);
            Assert.That(activeFailure, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));
            merchantSystem.AdvanceNpcTradeState(merchant);
            Assert.That(records.DecisionRecorder.Record(
                merchant.RuntimeId,
                NpcDecisionType.TradeRedirect,
                NpcDecisionOrigin.Autonomous,
                null,
                null,
                staleTarget.Location.RuntimeId,
                null), Is.Not.Null,
                "the Decision ID and record sequence are allocated while the existing Merchant batch is active");
        }

        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.Location.RuntimeId), Is.True);
        Assert.That(merchant.MerchantTradePlan.HasData, Is.False,
            "the local merchant normalizes its existing plan inside the nested operation");
        Assert.That(merchant.TravelPlan.IsActive, Is.False,
            "clearing the local merchant plan clears its embedded Trade travel intent in the same batch");
        Assert.That(merchant.MerchantTradePlan.Revision, Is.EqualTo(2));
        Assert.That(merchant.TravelPlan.Revision, Is.EqualTo(2));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            city.Location.RuntimeId, item.DefinitionId, out CommercialMarketObservation observation), Is.True);
        Assert.That(observation.ObservedDay, Is.Zero);
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            city.Location.RuntimeId, out CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.Zero);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1),
            "owner, Decision-counter, and record-sequence changes publish as one post-commit epoch");
        Assert.That(RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(records.Allocator)
            .GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(new SimulationRecordSequenceCensusProvider(records.Sequence)
            .GetCurrentCensus().Revision, Is.EqualTo(1L));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12NpcTrade_NotifiesEachCommittedAccountAndInventoryOwner()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-npc-trade");
        NpcRuntime buyer = new NpcRuntime("buyer-trade", SimulationTestFactory.CreateNpc("buyer-trade"), null, 100f);
        NpcRuntime seller = new NpcRuntime("seller-trade", SimulationTestFactory.CreateNpc("seller-trade"), null, 20f);
        seller.Inventory.AddItem(item, 3, 4f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { buyer, seller },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        EconomyTransactionResult result = service.TryExecuteNpcTrade(buyer, seller, item, 2, 10f);

        Assert.That(result.Success, Is.True);
        Assert.That(buyer.Money, Is.EqualTo(80f));
        Assert.That(seller.Money, Is.EqualTo(40f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 4), "each of the two account and two Inventory installs advances the epoch");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12MoneyTransfer_ScopesExactAccountCommitsAndKeepsZeroValueAsNoOp()
    {
        NpcRuntime source = new NpcRuntime("p12-transfer-source", null, null, 100f);
        NpcRuntime destination = new NpcRuntime("p12-transfer-destination", null, null, 25f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { source, destination },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        ContinuationCensusFailure[] observedOperationFailures = new ContinuationCensusFailure[2];
        int observedCommits = 0;
        Action observeCommit = () =>
        {
            if (observedCommits < observedOperationFailures.Length)
                runtime.TryAssessNpcRosterCensus(out observedOperationFailures[observedCommits]);
            observedCommits++;
        };
        WrapP12AccountCommit(source.MoneyAccount, observeCommit);
        WrapP12AccountCommit(destination.MoneyAccount, observeCommit);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        EconomyTransactionResult result = service.TryTransferMoney(source, destination, 40f);

        Assert.That(result.Success, Is.True);
        Assert.That(source.Money, Is.EqualTo(60f));
        Assert.That(destination.Money, Is.EqualTo(65f));
        Assert.That(source.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(destination.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(observedCommits, Is.EqualTo(2));
        Assert.That(observedOperationFailures, Is.All.EqualTo(ContinuationCensusFailure.OperationInProgress));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterTransfer, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(afterTransfer, Is.EqualTo(before + 2));
        AssertMoneyAccountWitness(runtime, source, source.MoneyAccount, 1);
        AssertMoneyAccountWitness(runtime, destination, destination.MoneyAccount, 1);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());

        long sourceRevision = source.MoneyAccount.Revision;
        long destinationRevision = destination.MoneyAccount.Revision;
        Assert.That(service.TryTransferMoney(source, destination, 0f).Success, Is.True);
        Assert.That(source.MoneyAccount.Revision, Is.EqualTo(sourceRevision));
        Assert.That(destination.MoneyAccount.Revision, Is.EqualTo(destinationRevision));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterZero, out ContinuationCensusFailure zeroFailure),
            Is.True, zeroFailure.ToString());
        Assert.That(afterZero, Is.EqualTo(afterTransfer));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterZeroAssessment), Is.True,
            afterZeroAssessment.ToString());

        EconomyTransactionResult invalid = service.TryTransferMoney(source, destination, float.NaN);
        Assert.That(invalid.Success, Is.False);
        Assert.That(invalid.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.InvalidInput));
        Assert.That(source.Money, Is.EqualTo(60f));
        Assert.That(destination.Money, Is.EqualTo(65f));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterInvalid, out ContinuationCensusFailure invalidFailure),
            Is.True, invalidFailure.ToString());
        Assert.That(afterInvalid, Is.EqualTo(afterTransfer));

        EconomyTransactionResult raw = service.TryTransferMoney(
            source.MoneyAccount,
            destination.MoneyAccount,
            1f);
        Assert.That(raw.Success, Is.False);
        Assert.That(raw.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(source.Money, Is.EqualTo(60f));
        Assert.That(destination.Money, Is.EqualTo(65f));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterRaw, out ContinuationCensusFailure rawFailure),
            Is.True, rawFailure.ToString());
        Assert.That(afterRaw, Is.EqualTo(afterTransfer));
    }

    [Test]
    public void P12MoneyTransfer_CompensationCommitsSourceRevisionWithinOneOperation()
    {
        NpcRuntime source = new NpcRuntime("p12-transfer-comp-source", null, null, 100f);
        NpcRuntime destination = new NpcRuntime("p12-transfer-comp-destination", null, null, 25f);
        SetAccountRevision(destination.MoneyAccount, long.MaxValue);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { source, destination },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        ContinuationCensusFailure[] observedFailures = new ContinuationCensusFailure[2];
        int observedCommits = 0;
        WrapP12AccountCommit(source.MoneyAccount, () =>
        {
            if (observedCommits < observedFailures.Length)
                runtime.TryAssessNpcRosterCensus(out observedFailures[observedCommits]);
            observedCommits++;
        });
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());

        EconomyTransactionResult result = service.TryTransferMoney(source, destination, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(source.Money, Is.EqualTo(100f), "the existing source-credit compensation remains in force");
        Assert.That(source.MoneyAccount.Revision, Is.EqualTo(2), "debit and successful refund are distinct committed revisions");
        Assert.That(destination.Money, Is.EqualTo(25f));
        Assert.That(destination.MoneyAccount.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(observedCommits, Is.EqualTo(2));
        Assert.That(observedFailures, Is.All.EqualTo(ContinuationCensusFailure.OperationInProgress));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 2));
        AssertMoneyAccountWitness(runtime, source, source.MoneyAccount, 2);
        AssertMoneyAccountWitness(runtime, destination, destination.MoneyAccount, long.MaxValue);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12MoneyTransfer_RejectsStaleOrReplacedOwnersAndWrongThreadBeforeWrites()
    {
        NpcRuntime source = new NpcRuntime("p12-transfer-stale-source", null, null, 100f);
        NpcRuntime destination = new NpcRuntime("p12-transfer-stale-destination", null, null, 25f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { source, destination },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        NpcRuntime staleSource = new NpcRuntime(source.RuntimeId, null, null, 100f);
        EconomyTransactionResult stale = service.TryTransferMoney(staleSource, destination, 10f);
        Assert.That(stale.Success, Is.False);
        Assert.That(stale.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(source.Money, Is.EqualTo(100f));
        Assert.That(staleSource.Money, Is.EqualTo(100f));
        Assert.That(destination.Money, Is.EqualTo(25f));
        Assert.That(staleSource.MoneyAccount.Revision, Is.Zero);

        NpcRuntime secondSource = new NpcRuntime("p12-transfer-replaced-source", null, null, 80f);
        NpcRuntime secondDestination = new NpcRuntime("p12-transfer-replaced-destination", null, null, 20f);
        SimulationRuntime secondRuntime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { secondSource, secondDestination },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService secondService = new EconomyTransactionService();
        BindP12CensusRuntime(secondService, secondRuntime);
        MoneyAccountRuntime replacementAccount = new MoneyAccountRuntime(80f);
        typeof(NpcRuntime)
            .GetField("moneyAccount", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(secondSource, replacementAccount);
        EconomyTransactionResult replaced = secondService.TryTransferMoney(secondSource, secondDestination, 10f);
        Assert.That(replaced.Success, Is.False);
        Assert.That(replaced.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(secondSource.MoneyAccount, Is.SameAs(replacementAccount));
        Assert.That(replacementAccount.Balance, Is.EqualTo(80f));
        Assert.That(replacementAccount.Revision, Is.Zero);
        Assert.That(secondDestination.Money, Is.EqualTo(20f));

        NpcRuntime thirdSource = new NpcRuntime("p12-transfer-thread-source", null, null, 80f);
        NpcRuntime thirdDestination = new NpcRuntime("p12-transfer-thread-destination", null, null, 20f);
        SimulationRuntime thirdRuntime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { thirdSource, thirdDestination },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService thirdService = new EconomyTransactionService();
        BindP12CensusRuntime(thirdService, thirdRuntime);

        EconomyTransactionResult rejected = null;
        Thread wrongThread = new Thread(() =>
        {
            rejected = thirdService.TryTransferMoney(thirdSource, thirdDestination, 10f);
        });
        wrongThread.Start();
        wrongThread.Join();

        Assert.That(rejected.Success, Is.False);
        Assert.That(rejected.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(thirdSource.Money, Is.EqualTo(80f));
        Assert.That(thirdDestination.Money, Is.EqualTo(20f));
        Assert.That(thirdSource.MoneyAccount.Revision, Is.Zero);
        Assert.That(thirdDestination.MoneyAccount.Revision, Is.Zero);
    }

    [Test]
    public void P12MarketWrappers_UseSharedServiceAndNotifyEveryCommittedOwner()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-market-roundtrip", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-market-roundtrip-city",
            "p12-market-roundtrip-location",
            SimulationTestFactory.CreateMarketItem(item, 10, 10));
        NpcRuntime npc = new NpcRuntime(
            "p12-market-roundtrip-npc",
            SimulationTestFactory.CreateNpc("p12-market-roundtrip-npc"),
            city,
            100f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        Assert.That(city.Market.BuyItem(npc, item, 2, out int bought, out _, out _), Is.True);
        Assert.That(bought, Is.EqualTo(2));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterPurchase, out ContinuationCensusFailure purchaseFailure),
            Is.True, purchaseFailure.ToString());
        Assert.That(afterPurchase, Is.EqualTo(before + 3),
            "the shared bound wrapper reports the NPC account, Market, and Inventory commits");

        Assert.That(city.Market.SellItem(npc, item, 1, out int sold, out _, out _, out _), Is.True);
        Assert.That(sold, Is.EqualTo(1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterSale, out ContinuationCensusFailure saleFailure),
            Is.True, saleFailure.ToString());
        Assert.That(afterSale, Is.EqualTo(afterPurchase + 3));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12MarketSale_ReportsAccountCommitAndSuccessfulCompensationWhenInventoryCannotCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-market-sale-compensation", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-market-sale-compensation-city",
            "p12-market-sale-compensation-location",
            SimulationTestFactory.CreateMarketItem(item, 0, 10));
        NpcRuntime npc = new NpcRuntime(
            "p12-market-sale-compensation-npc",
            SimulationTestFactory.CreateNpc("p12-market-sale-compensation-npc"),
            city,
            20f);
        npc.Inventory.AddItem(item, 2, 4f);
        typeof(InventoryRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(npc.Inventory, long.MaxValue);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        EconomyTransactionResult result = service.TryExecuteMarketSale(npc, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(npc.Money, Is.EqualTo(20f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(city.Market.GetAmount(item), Is.Zero);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 2), "the successful credit and account reversal both advance the epoch");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12MarketSale_ExhaustedMarketRevisionRejectsBeforeAnyOwnerCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-market-revision-limit", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-market-revision-limit-city",
            "p12-market-revision-limit-location",
            SimulationTestFactory.CreateMarketItem(item, 0, 10));
        typeof(MarketRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(city.Market, long.MaxValue);
        NpcRuntime npc = new NpcRuntime(
            "p12-market-revision-limit-npc",
            SimulationTestFactory.CreateNpc("p12-market-revision-limit-npc"),
            city,
            20f);
        npc.Inventory.AddItem(item, 2, 4f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment), Is.True,
            initialAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        EconomyTransactionResult result = service.TryExecuteMarketSale(npc, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(npc.Money, Is.EqualTo(20f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(city.Market.GetAmount(item), Is.Zero);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12MarketPurchase_ExhaustedMarketRevisionRejectsBeforeAnyOwnerCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-market-purchase-revision-limit", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-market-purchase-revision-limit-city",
            "p12-market-purchase-revision-limit-location",
            SimulationTestFactory.CreateMarketItem(item, 5, 5));
        typeof(MarketRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(city.Market, long.MaxValue);
        NpcRuntime npc = new NpcRuntime(
            "p12-market-purchase-revision-limit-npc",
            SimulationTestFactory.CreateNpc("p12-market-purchase-revision-limit-npc"),
            city,
            50f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialAssessment), Is.True,
            initialAssessment.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        EconomyTransactionResult result = service.TryExecuteMarketPurchase(npc, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(npc.Money, Is.EqualTo(50f));
        Assert.That(npc.Inventory.GetAmount(item), Is.Zero);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12MarketPurchase_StaleMarketRevisionRejectsBeforeAnyOwnerCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-market-stale-revision", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-market-stale-revision-city",
            "p12-market-stale-revision-location",
            SimulationTestFactory.CreateMarketItem(item, 5, 5));
        NpcRuntime npc = new NpcRuntime(
            "p12-market-stale-revision-npc",
            SimulationTestFactory.CreateNpc("p12-market-stale-revision-npc"),
            city,
            50f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        typeof(MarketRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(city.Market, city.Market.Revision + 1);

        EconomyTransactionResult result = service.TryExecuteMarketPurchase(npc, city.Market, item, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(npc.Money, Is.EqualTo(50f));
        Assert.That(npc.Inventory.GetAmount(item), Is.Zero);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.False);
        Assert.That(assessment, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void P12MarketPurchase_WrongThreadRejectsBeforeAnyOwnerCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-market-wrong-thread", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-market-wrong-thread-city",
            "p12-market-wrong-thread-location",
            SimulationTestFactory.CreateMarketItem(item, 5, 5));
        NpcRuntime npc = new NpcRuntime(
            "p12-market-wrong-thread-npc",
            SimulationTestFactory.CreateNpc("p12-market-wrong-thread-npc"),
            city,
            50f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        EconomyTransactionResult result = null;
        Thread offThread = new Thread(() => result = service.TryExecuteMarketPurchase(npc, city.Market, item, 1));
        offThread.Start();
        offThread.Join();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(npc.Money, Is.EqualTo(50f));
        Assert.That(npc.Inventory.GetAmount(item), Is.Zero);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.False);
        Assert.That(assessment, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void P12MarketDirectMutation_UpdatesLiveMarketWitnessAndSkipsNoOps()
    {
        ItemData initialItem = SimulationTestFactory.CreateItem("p12-market-direct-initial", 5f);
        ItemData addedItem = SimulationTestFactory.CreateItem("p12-market-direct-added", 7f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12-market-direct-city",
            "p12-market-direct-location",
            SimulationTestFactory.CreateMarketItem(initialItem, 4, 4));
        NpcRuntime npc = new NpcRuntime(
            "p12-market-direct-npc",
            SimulationTestFactory.CreateNpc("p12-market-direct-npc"),
            city,
            10f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        long initialMarketRevision = city.Market.Revision;
        Assert.That(city.Market.AddStock(addedItem, 3), Is.EqualTo(3));
        Assert.That(city.Market.AddStock(initialItem, 0), Is.Zero);
        Assert.That(city.Market.RemoveStockUpTo(initialItem, 0), Is.Zero);
        city.Market.UpdatePrices();

        initialItem.basePrice = 6f;
        city.Market.UpdatePrices();
        Assert.That(city.Market.GetPrice(initialItem), Is.EqualTo(6f));
        city.Market.UpdatePrices();

        Assert.That(city.Market.Items.Count, Is.EqualTo(2));
        Assert.That(city.Market.Revision, Is.EqualTo(initialMarketRevision + 2),
            "the row addition and changed-price refresh each commit one Market revision");
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 2), "the row addition and changed-price refresh are reported; no-ops are not");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12DailyMarketProduction_ReportsMarketCommitInsideAdvance()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-daily-market-production", 5f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "p12-daily-market-production-city-data",
            SimulationTestFactory.CreateMarketItem(item, 10, 10));
        cityData.productionConfigs.Add(new CityProductionConfig { item = item, amountPerDay = 2 });
        CityRuntime city = new CityRuntime(
            "p12-daily-market-production-city",
            cityData,
            new SpatialLocationRuntime("p12-daily-market-production-location"));
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new NpcRuntime[0],
            economyEnabled: true,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        long initialMarketRevision = city.Market.Revision;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());

        Assert.That(city.Market.Revision, Is.EqualTo(initialMarketRevision + 1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(12));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12DailyFreeConsumption_ReportsMarketCommitInsideAdvance()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-daily-free-consumption", 5f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "p12-daily-free-consumption-city",
            SimulationTestFactory.CreateMarketItem(item, 4, 4));
        cityData.marketItems[0].consumptionPer1000Population = 1f;
        CityRuntime city = new CityRuntime(
            "p12-daily-free-consumption-city",
            cityData,
            new SpatialLocationRuntime("p12-daily-free-consumption-location"));
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new NpcRuntime[0],
            economyEnabled: true,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        long initialMarketRevision = city.Market.Revision;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());

        Assert.That(city.Market.GetAmount(item), Is.EqualTo(3));
        Assert.That(city.Market.Revision, Is.EqualTo(initialMarketRevision + 1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12DailyChangedPriceRefresh_ReportsMarketCommitInsideAdvance()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-daily-price-refresh", 5f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "p12-daily-price-refresh-city",
            SimulationTestFactory.CreateMarketItem(item, 10, 10));
        CityRuntime city = new CityRuntime(
            "p12-daily-price-refresh-city",
            cityData,
            new SpatialLocationRuntime("p12-daily-price-refresh-location"));
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new NpcRuntime[0],
            economyEnabled: true,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        long initialMarketRevision = city.Market.Revision;
        Assert.That(city.Market.GetPrice(item), Is.EqualTo(5f));
        item.basePrice = 6f;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());

        Assert.That(city.Market.GetPrice(item), Is.EqualTo(6f));
        Assert.That(city.Market.Revision, Is.EqualTo(initialMarketRevision + 1));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12NpcTrade_ReportsBuyerDebitAndSuccessfulCompensationWhenSellerCreditCannotCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-npc-trade-compensation");
        NpcRuntime buyer = new NpcRuntime("buyer-compensation", SimulationTestFactory.CreateNpc("buyer-compensation"), null, 100f);
        NpcRuntime seller = new NpcRuntime("seller-compensation", SimulationTestFactory.CreateNpc("seller-compensation"), null, 20f);
        seller.Inventory.AddItem(item, 3, 4f);
        typeof(MoneyAccountRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(seller.MoneyAccount, long.MaxValue);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { buyer, seller },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        EconomyTransactionResult result = service.TryExecuteNpcTrade(buyer, seller, item, 2, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(100f));
        Assert.That(seller.Money, Is.EqualTo(20f));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(3));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 2), "the debit and its committed compensation are both reported");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12NpcTrade_ReportsBothSuccessfulAccountCompensationsWhenSellerInventoryCannotCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-npc-trade-inventory-compensation");
        NpcRuntime buyer = new NpcRuntime("buyer-inventory-compensation", SimulationTestFactory.CreateNpc("buyer-inventory-compensation"), null, 100f);
        NpcRuntime seller = new NpcRuntime("seller-inventory-compensation", SimulationTestFactory.CreateNpc("seller-inventory-compensation"), null, 20f);
        seller.Inventory.AddItem(item, 3, 4f);
        typeof(InventoryRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(seller.Inventory, long.MaxValue);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { buyer, seller },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out ContinuationCensusFailure beforeFailure),
            Is.True, beforeFailure.ToString());
        EconomyTransactionResult result = service.TryExecuteNpcTrade(buyer, seller, item, 2, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(100f));
        Assert.That(seller.Money, Is.EqualTo(20f));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(3));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure afterFailure),
            Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 4), "debit, credit, and both committed account compensations are reported");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.True,
            assessment.ToString());
    }

    [Test]
    public void P12NpcTrade_PreexistingUnnotifiedOwnerDriftRejectsBeforeAnyTradeCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-npc-trade-drift");
        NpcRuntime buyer = new NpcRuntime("buyer-drift", SimulationTestFactory.CreateNpc("buyer-drift"), null, 100f);
        NpcRuntime seller = new NpcRuntime("seller-drift", SimulationTestFactory.CreateNpc("seller-drift"), null, 20f);
        seller.Inventory.AddItem(item, 3, 4f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { buyer, seller },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long before, out _), Is.True);
        UnbindCurrentP12OwnerBoundary(buyer.MoneyAccount);
        buyer.MoneyAccount.TryCredit(5f);

        EconomyTransactionResult result = service.TryExecuteNpcTrade(buyer, seller, item, 2, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(105f), "the preexisting external credit remains, but the trade does not begin");
        Assert.That(seller.Money, Is.EqualTo(20f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(3));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long after, out ContinuationCensusFailure epochFailure),
            Is.False);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(after, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.False);
        Assert.That(assessment, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(before, Is.Zero);
    }

    [Test]
    public void P12NpcTrade_UnregisteredParticipantIsRejectedBeforeAnyOwnerCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-npc-trade-unregistered");
        NpcRuntime buyer = new NpcRuntime("buyer-unregistered", SimulationTestFactory.CreateNpc("buyer-unregistered"), null, 100f);
        NpcRuntime seller = new NpcRuntime("seller-unregistered", SimulationTestFactory.CreateNpc("seller-unregistered"), null, 20f);
        seller.Inventory.AddItem(item, 3, 4f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { seller },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);

        EconomyTransactionResult result = service.TryExecuteNpcTrade(buyer, seller, item, 2, 10f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(100f));
        Assert.That(seller.Money, Is.EqualTo(20f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(3));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.False);
        Assert.That(assessment, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void P12NpcTrade_WrongThreadAdmissionRejectsBeforeAnyOwnerCommit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-npc-trade-wrong-thread");
        NpcRuntime buyer = new NpcRuntime("buyer-wrong-thread", SimulationTestFactory.CreateNpc("buyer-wrong-thread"), null, 100f);
        NpcRuntime seller = new NpcRuntime("seller-wrong-thread", SimulationTestFactory.CreateNpc("seller-wrong-thread"), null, 20f);
        seller.Inventory.AddItem(item, 3, 4f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { buyer, seller },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        EconomyTransactionResult result = null;
        Thread offThread = new Thread(() => result = service.TryExecuteNpcTrade(buyer, seller, item, 2, 10f));
        offThread.Start();
        offThread.Join();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(100f));
        Assert.That(seller.Money, Is.EqualTo(20f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(3));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.False);
        Assert.That(assessment, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void P12NpcTrade_SaturationAfterFirstCommitPreservesCommittedLeafAndFaultsProtocol()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12-npc-trade-post-commit-fault");
        NpcRuntime buyer = new NpcRuntime("buyer-post-commit-fault", SimulationTestFactory.CreateNpc("buyer-post-commit-fault"), null, 100f);
        NpcRuntime seller = new NpcRuntime("seller-post-commit-fault", SimulationTestFactory.CreateNpc("seller-post-commit-fault"), null, 20f);
        seller.Inventory.AddItem(item, 3, 4f);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { buyer, seller },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        EconomyTransactionService service = new EconomyTransactionService();
        BindP12CensusRuntime(service, runtime);
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        typeof(ContinuationCensusProtocol).GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(protocol, long.MaxValue);

        EconomyTransactionResult result = service.TryExecuteNpcTrade(buyer, seller, item, 2, 10f);

        Assert.That(result.Success, Is.False,
            "The first leaf commit is retained; later writes are rejected after the protocol faults, and this contract adds no trade rollback.");
        Assert.That(result.FailureReason, Is.EqualTo(EconomyTransactionFailureReason.TransactionCommitFailed));
        Assert.That(buyer.Money, Is.EqualTo(80f));
        Assert.That(buyer.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(seller.Money, Is.EqualTo(20f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.Zero);
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(3));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment), Is.False);
        Assert.That(assessment, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileRejectsWrongThreadWithoutAdvancingAndFaultsPartialProtocol()
    {
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

        bool advanced = true;
        SimulationRuntimeAdvanceFailure advanceFailure = SimulationRuntimeAdvanceFailure.None;
        Thread offThread = new Thread(() => advanced = runtime.TryAdvanceDay(out advanceFailure));
        offThread.Start();
        offThread.Join();

        Assert.That(advanced, Is.False);
        Assert.That(advanceFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void RuntimeOwnedClockApisRejectWrongThreadWithoutAdvancing()
    {
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

        Exception advanceException = null;
        bool tryAdvanceResult = true;
        SimulationTimeAdvanceFailure tryAdvanceFailure = SimulationTimeAdvanceFailure.None;
        Thread offThread = new Thread(() =>
        {
            try
            {
                runtime.SimulationTime.AdvanceDay();
            }
            catch (Exception exception)
            {
                advanceException = exception;
            }

            tryAdvanceResult = runtime.SimulationTime.TryAdvanceDay(out tryAdvanceFailure);
        });
        offThread.Start();
        offThread.Join();

        Assert.That(advanceException, Is.TypeOf<InvalidOperationException>());
        Assert.That(tryAdvanceResult, Is.False);
        Assert.That(tryAdvanceFailure, Is.EqualTo(SimulationTimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static void AssertMoneyAccountWitness(
        SimulationRuntime runtime,
        NpcRuntime npc,
        MoneyAccountRuntime account,
        long expectedRevision)
    {
        string expectedSectionId = NpcMoneyAccountCensusProvider.SectionPrefix + npc.RuntimeId;
        foreach (IOwnerSectionCensusProvider provider in runtime.MoneyAccountCensusProviders)
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (!string.Equals(witness.SectionId, expectedSectionId, System.StringComparison.Ordinal)) continue;

            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(account));
            Assert.That(witness.SchemaVersion, Is.EqualTo(NpcMoneyAccountCensusProvider.SchemaVersion));
            Assert.That(witness.Cardinality, Is.EqualTo(1));
            Assert.That(witness.Revision, Is.EqualTo(expectedRevision));
            return;
        }

        Assert.Fail("The expected per-NPC MoneyAccount witness was not published: " + expectedSectionId);
    }

    private static void WrapP12AccountCommit(MoneyAccountRuntime account, Action observeCommit)
    {
        FieldInfo callbackField = typeof(MoneyAccountRuntime).GetField(
            "p12MutationCommitted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Action original = (Action)callbackField.GetValue(account);
        callbackField.SetValue(account, (Action)(() =>
        {
            observeCommit();
            original();
        }));
    }

    private static void SetAccountRevision(MoneyAccountRuntime account, long revision)
    {
        typeof(MoneyAccountRuntime)
            .GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(account, revision);
    }

    private static void BindP12CensusRuntime(EconomyTransactionService service, SimulationRuntime runtime)
    {
        typeof(SimulationRuntime)
            .GetMethod("BindP12EconomyTransactionService", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(runtime, new object[] { service });
    }

    private static void UnbindCurrentP12OwnerBoundary(object owner)
    {
        Func<bool> admission = (Func<bool>)owner.GetType().GetField(
            "p12MutationAdmission", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        Action committed = (Action)owner.GetType().GetField(
            "p12MutationCommitted", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        MethodInfo method = owner.GetType().GetMethod(
            "UnbindP12MutationBoundary", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        Assert.That(method.Invoke(owner, new object[] { admission, committed }), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DailyProfileFaultsAdmissionAfterInterruptedAdvance(bool useBatch)
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime("npc-admission-failure", SimulationTestFactory.CreateNpc("admission-failure"));
        NpcActionData action = SimulationTestFactory.CreateAction("admission-failure-travel", NpcActionType.Travel);
        action.baseUtility = 1f;
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 1f });
        AdmissionProbeActionProvider provider = new AdmissionProbeActionProvider(
            action,
            () => throw new InvalidOperationException("injected incomplete daily execution"));
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            null,
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(new System.Collections.Generic.List<INpcActionProvider> { provider }),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

        if (useBatch)
        {
            Assert.Throws<InvalidOperationException>(() => runtime.TryAdvanceDays(
                2,
                out _,
                out _));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => runtime.TryAdvanceDay(out _));
        }

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retryFailure), Is.False);
        Assert.That(retryFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
    }

    [Test]
    public void DailyProfileRejectsMismatchedConstructionThreadAndP18Composition()
    {
        SimulationRuntimeAdmissionContext mainThreadContext =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        Exception constructionFailure = null;
        Thread offThread = new Thread(() =>
        {
            try
            {
                new SimulationRuntime(
                    new SimulationTime(), null, null, economyEnabled: false,
                    runtimeAdmissionContext: mainThreadContext);
            }
            catch (Exception exception)
            {
                constructionFailure = exception;
            }
        });
        offThread.Start();
        offThread.Join();
        Assert.That(constructionFailure, Is.TypeOf<ArgumentException>());

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false,
            p18dIntradayProfile: new P18DIntradayProfile("world", "profile", "config", "content"),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1()));
    }

    [Test]
    public void UnityBootstrapDailyRejectsFiniteSourceBeforeIdentityOrOwnerConstruction()
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.FiniteReserveDaily, 7, out _);
        GameObject bootstrapObject = new GameObject("P14-B rejected finite Daily profile");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(bootstrap, config);
            int worldIdentityAllocations = 0;
            WritePrivateField(bootstrap, "worldIdentityAllocator", new Func<WorldId>(() =>
            {
                worldIdentityAllocations++;
                return null;
            }));
            List<string> completedStages = new List<string>();

            TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
                InvokeInitializeSimulation(bootstrap, completedStages.Add));

            Assert.That(thrown.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(thrown.InnerException.Message, Does.Contain("P14-B"));
            Assert.That(completedStages, Is.Empty, "resolve-profile fails before its stage completion callback");
            Assert.That(worldIdentityAllocations, Is.Zero);
            Assert.That(ReadPrivateField<RuntimeIdAllocator>(bootstrap, "runtimeIdAllocator"), Is.Null);
            Assert.That(ReadPrivateField<SimulationRuntime>(bootstrap, "simulationRuntime"), Is.Null);
            Assert.That(ReadPrivateField<SimulationBootstrapComposition>(bootstrap, "draftComposition"), Is.Null);
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.Null);
            Assert.That(ReadPrivateField<List<CityRuntime>>(bootstrap, "cityRuntimeList"), Is.Empty);
            Assert.That(ReadPrivateField<bool>(bootstrap, "bootstrapFailed"), Is.True);
            Assert.That(bootstrap.Bootstrap, Is.Null);
            Assert.That(bootstrap.Runtime, Is.Null);
            Assert.That(bootstrap.CurrentDay, Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void UnityBootstrapDailyRejectsExogenousMaterialFlowBeforeIdentityOrOwnerConstruction()
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.ExogenousDaily, 0, out _);
        GameObject bootstrapObject = new GameObject("P14-A rejected by P12 Daily profile");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(bootstrap, config);
            int worldIdentityAllocations = 0;
            WritePrivateField(bootstrap, "worldIdentityAllocator", new Func<WorldId>(() =>
            {
                worldIdentityAllocations++;
                return null;
            }));
            List<string> completedStages = new List<string>();

            TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
                InvokeInitializeSimulation(bootstrap, completedStages.Add));

            Assert.That(thrown.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(thrown.InnerException.Message, Does.Contain("P14-A"));
            Assert.That(completedStages, Is.Empty, "resolve-profile fails before its stage completion callback");
            Assert.That(worldIdentityAllocations, Is.Zero);
            Assert.That(ReadPrivateField<RuntimeIdAllocator>(bootstrap, "runtimeIdAllocator"), Is.Null);
            Assert.That(ReadPrivateField<SimulationRuntime>(bootstrap, "simulationRuntime"), Is.Null);
            Assert.That(ReadPrivateField<SimulationBootstrapComposition>(bootstrap, "draftComposition"), Is.Null);
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.Null);
            Assert.That(ReadPrivateField<List<CityRuntime>>(bootstrap, "cityRuntimeList"), Is.Empty);
            Assert.That(ReadPrivateField<bool>(bootstrap, "bootstrapFailed"), Is.True);
            Assert.That(bootstrap.Bootstrap, Is.Null);
            Assert.That(bootstrap.Runtime, Is.Null);
            Assert.That(bootstrap.CurrentDay, Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void P14AExogenousMaterialFlowRunsInItsSingleCityProvingProfile()
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.ExogenousDaily, 0, out ItemData item);
        GameObject bootstrapObject = new GameObject("P14-A admitted exogenous Daily profile");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureUnscopedBootstrap(bootstrap, config);

            InvokeInitializeSimulation(bootstrap, null);

            Assert.That(bootstrap.Bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Runtime.Cities.Count, Is.EqualTo(1));
            CityRuntime city = bootstrap.Runtime.Cities[0];
            Assert.That(bootstrap.Runtime.LegacySpatialAnchorBindingStore.TryGet(
                new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, city.RuntimeId), out LocationId boundLocation), Is.True);
            Assert.That(boundLocation.Value, Is.EqualTo(config.authoredLocationId));
            Assert.That(city.FiniteProductionSources, Is.Null);
            Assert.That(city.Market.GetAmount(item), Is.EqualTo(10));

            Assert.That(bootstrap.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
                advanceFailure.ToString());

            Assert.That(city.FiniteProductionSources, Is.Null);
            Assert.That(city.LastMaterialFlow.AppliedSourceQuantity, Is.EqualTo(5));
            Assert.That(city.LastMaterialFlow.ActualFreeConsumption, Is.EqualTo(1));
            Assert.That(city.LastMaterialFlow.ClosingStock, Is.EqualTo(14));
            Assert.That(bootstrap.CurrentDay, Is.EqualTo(1));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void FiniteSourceProfileRunsOutsideP12Daily()
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.FiniteReserveDaily, 7, out ItemData item);
        GameObject bootstrapObject = new GameObject("P14-B finite non-P12 profile");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureUnscopedBootstrap(bootstrap, config);

            InvokeInitializeSimulation(bootstrap, null);

            Assert.That(bootstrap.Bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Runtime.Cities.Count, Is.EqualTo(1));
            CityRuntime city = bootstrap.Runtime.Cities[0];
            Assert.That(bootstrap.Runtime.LegacySpatialAnchorBindingStore.TryGet(
                new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, city.RuntimeId), out LocationId boundLocation), Is.True);
            Assert.That(boundLocation.Value, Is.EqualTo(config.authoredLocationId));
            Assert.That(city.FiniteProductionSources, Is.Not.Null);
            Assert.That(bootstrap.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
                advanceFailure.ToString());
            Assert.That(city.LastMaterialFlow.AppliedSourceQuantity, Is.EqualTo(5));
            Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(2));
            Assert.That(city.LastMaterialFlow.ClosingStock, Is.EqualTo(14));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void FiniteSourceProfileWithP10AIsDeferredBeforeIdentityOrOwnerConstruction()
    {
        AssertP14FiniteProfileWithP10Rejected(generated: false);
    }

    [Test]
    public void FiniteSourceProfileWithP10BIsDeferredBeforeIdentityOrOwnerConstruction()
    {
        AssertP14FiniteProfileWithP10Rejected(generated: true);
    }

    [Test]
    public void FiniteSourceProfileRejectsAdditionalCityBeforeConstruction()
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.FiniteReserveDaily, 7, out _);
        GameObject bootstrapObject = new GameObject("P14-B multi-City finite profile");
        try
        {
            config.Cities.Add(config.Cities[0]);
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureUnscopedBootstrap(bootstrap, config);
            int worldIdentityAllocations = 0;
            WritePrivateField(bootstrap, "worldIdentityAllocator", new Func<WorldId>(() =>
            {
                worldIdentityAllocations++;
                return null;
            }));
            List<string> completedStages = new List<string>();

            TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
                InvokeInitializeSimulation(bootstrap, completedStages.Add));

            Assert.That(thrown.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(thrown.InnerException.Message, Does.Contain("FiniteReserveProfileRequiresExactlyOneAuthoredCity"));
            Assert.That(completedStages, Is.Empty);
            Assert.That(worldIdentityAllocations, Is.Zero);
            Assert.That(ReadPrivateField<List<CityRuntime>>(bootstrap, "cityRuntimeList"), Is.Empty);
            Assert.That(ReadPrivateField<RuntimeIdAllocator>(bootstrap, "runtimeIdAllocator"), Is.Null);
            Assert.That(ReadPrivateField<SimulationRuntime>(bootstrap, "simulationRuntime"), Is.Null);
            Assert.That(bootstrap.Bootstrap, Is.Null);
            Assert.That(bootstrap.CurrentDay, Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
    [Test]
    public void BootstrapScopesValidationThroughPublicationAndRevokesFailedPublication()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);

        GameObject successfulObject = new GameObject("P12 bootstrap admission success");
        GameObject failedObject = new GameObject("P12 bootstrap admission failure");
        try
        {
            TesteSimulacao successfulBootstrap = successfulObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(successfulBootstrap, config);
            bool validationTailWasBusy = false;
            InvokeInitializeSimulation(successfulBootstrap, stageId =>
            {
                if (stageId == "p9.genesis.validate-profile/v1")
                {
                    SimulationRuntime runtime = ReadPrivateField<SimulationRuntime>(successfulBootstrap, "simulationRuntime");
                    validationTailWasBusy = !runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure)
                        && failure == ContinuationCensusFailure.OperationInProgress;
                }
            });

            Assert.That(validationTailWasBusy, Is.True);
            Assert.That(successfulBootstrap.Bootstrap, Is.Not.Null);
            Assert.That(successfulBootstrap.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure idle), Is.True,
                idle.ToString());

            TesteSimulacao failedBootstrap = failedObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(failedBootstrap, config);
            TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
                InvokeInitializeSimulation(failedBootstrap, stageId =>
                {
                    if (stageId == "p9.genesis.publish/v1")
                        throw new InvalidOperationException("injected post-publication callback failure");
                }));
            Assert.That(thrown.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(failedBootstrap.Bootstrap, Is.Null);
            SimulationRuntime failedRuntime = ReadPrivateField<SimulationRuntime>(failedBootstrap, "simulationRuntime");
            Assert.That(failedRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure faulted), Is.False);
            Assert.That(faulted, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));

            failedBootstrap.Start();
            Assert.That(failedBootstrap.Bootstrap, Is.Null, "a failed Start attempt is permanently latched");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(successfulObject);
            UnityEngine.Object.DestroyImmediate(failedObject);
        }
    }

    [Test]
    public void StartCapturesAndPassesTheSelectedProfileOwnerThread()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);

        GameObject bootstrapObject = new GameObject("P12 Start owner binding");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            WritePrivateField(bootstrap, "simulationConfig", config);
            WritePrivateField(bootstrap, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);

            bootstrap.Start();

            Assert.That(bootstrap.Bootstrap, Is.Not.Null);
            Assert.That(ReadPrivateField<Thread>(bootstrap, "bootstrapStartThread"), Is.SameAs(Thread.CurrentThread));
            Assert.That(ReadPrivateField<int>(bootstrap, "bootstrapStartThreadId"), Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
            Assert.That(bootstrap.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True,
                failure.ToString());

            SimulationActiveSession activeSession = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(activeSession, Is.Not.Null);
            Assert.That(bootstrap.Bootstrap, Is.SameAs(activeSession.Composition));
            Assert.That(bootstrap.Runtime, Is.SameAs(activeSession.Runtime));
            Assert.That(bootstrap.ChronicleFormatter, Is.SameAs(activeSession.Composition.ChronicleFormatter));
            Assert.That(bootstrap.FullLog, Is.EqualTo(activeSession.Logger.FullLog));
            Assert.That(ReadPrivateField<SimulationRuntime>(bootstrap, "simulationRuntime"), Is.Null,
                "Published runtime access must come from the one active-session snapshot.");
            Assert.That(ReadPrivateField<SimulationLogger>(bootstrap, "logger"), Is.Null);
            Assert.That(ReadPrivateField<JusticeSystem>(bootstrap, "justiceSystem"), Is.Null);
            Assert.That(ReadPrivateField<RuntimeIdentityRegistry>(bootstrap, "runtimeIdentityRegistry"), Is.Null);
            Assert.That(ReadPrivateField<SpatialNetworkRuntime>(bootstrap, "spatialNetwork"), Is.Null);
            Assert.That(ReadPrivateField<List<CityRuntime>>(bootstrap, "cityRuntimeList"), Is.Null);
            Assert.That(ReadPrivateField<List<NpcRuntime>>(bootstrap, "npcRuntimeList"), Is.Null);
            Assert.That(ReadPrivateField<Dictionary<SpatialLocationRuntime, CityRuntime>>(bootstrap, "cityRuntimeByLocation"), Is.Null);

            Assert.That(bootstrap.TryPublishRestoredSession(activeSession, activeSession), Is.False,
                "A normal genesis/completed-boundary runtime is not a restored continuation candidate.");
            Assert.That(bootstrap.Bootstrap, Is.SameAs(activeSession.Composition),
                "Rejected admission must keep the original active-session reference authoritative.");

            bootstrap.Runtime.AdvanceDay();
            Assert.That(bootstrap.TryPublishRestoredSession(activeSession, activeSession), Is.False,
                "A normal completed advance cannot be relabeled as restored-continuation admission.");
            Assert.That(bootstrap.Runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken completedToken,
                out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
            Assert.That(completedToken.BoundaryProvenance, Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance),
                "A rejected exchange must leave the source completed-boundary behavior intact.");
            Assert.That(bootstrap.Bootstrap, Is.SameAs(activeSession.Composition));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    [Test]
    public void RestoredSessionExchangeWaitsForIdleWindowAndSwitchesActiveAliases()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);

        GameObject activeOwnerObject = new GameObject("P12 active restored-session exchange owner");
        GameObject stagedOwnerObject = new GameObject("P12 staged restored-session candidate");
        try
        {
            TesteSimulacao activeOwner = activeOwnerObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(activeOwner, config);
            InvokeInitializeSimulation(activeOwner, null);
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(activeOwner, "activeSession");
            Assert.That(sourceSession, Is.Not.Null);

            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(sourceSession.Runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken sourceToken,
                out DailyCaptureEligibilityFailure sourceFailure), Is.True, sourceFailure.ToString());

            TesteSimulacao stagedOwner = stagedOwnerObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(stagedOwner, config);
            WritePrivateField(stagedOwner, "worldIdentityAllocator", new Func<WorldId>(() => sourceSession.Composition.WorldId));
            InvokeInitializeSimulation(stagedOwner, stageId =>
            {
                if (stageId == "p9.genesis.resolve-profile/v1")
                    ReadPrivateField<SimulationTime>(stagedOwner, "simulationTime").AdvanceDay();
            });

            SimulationActiveSession candidateSession = ReadPrivateField<SimulationActiveSession>(stagedOwner, "activeSession");
            Assert.That(candidateSession, Is.Not.Null);
            Assert.That(candidateSession.Composition.WorldId, Is.SameAs(sourceToken.WorldId));
            Assert.That(candidateSession.Runtime.CurrentDay, Is.EqualTo(sourceToken.AbsoluteDay));

            // The bootstrap-built graph is now detached as a private restore candidate. The
            // runtime is still fresh: give it the preserved boundary identity and let the real
            // restored-admission protocol issue its candidate-bound token.
            WritePrivateField(candidateSession.Runtime, "factualReadWorldPublished", false);
            WritePrivateField<SimulationActiveSession>(stagedOwner, "activeSession", null);
            Assert.That(stagedOwner.Bootstrap, Is.Null);
            Assert.That(candidateSession.Runtime.TryAdmitRestoredDailyBoundary(
                sourceToken.WorldId,
                sourceToken.AbsoluteDay,
                sourceToken.CompletedCoreSequence,
                out DailyCaptureEligibilityFailure candidateAdmissionFailure), Is.True,
                candidateAdmissionFailure.ToString());
            Assert.That(candidateSession.Runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken candidateToken,
                out DailyCaptureEligibilityFailure candidateTokenFailure), Is.True,
                candidateTokenFailure.ToString());
            Assert.That(candidateToken.BoundaryProvenance,
                Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
            Assert.That(candidateToken, Is.Not.SameAs(sourceToken));

            MethodInfo beginOperation = typeof(TesteSimulacao).GetMethod(
                "TryBeginActiveSessionOperation",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(SimulationActiveSession).MakeByRefType() },
                null);
            MethodInfo endOperation = typeof(TesteSimulacao).GetMethod(
                "EndActiveSessionOperation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(beginOperation, Is.Not.Null);
            Assert.That(endOperation, Is.Not.Null);
            object[] operationArguments = { null };
            Assert.That((bool)beginOperation.Invoke(activeOwner, operationArguments), Is.True);
            Assert.That(operationArguments[0], Is.SameAs(sourceSession));
            try
            {
                Assert.That(activeOwner.TryPublishRestoredSession(sourceSession, candidateSession), Is.False,
                    "A restored session must not exchange while an owner operation is in flight.");
                Assert.That(activeOwner.Bootstrap, Is.SameAs(sourceSession.Composition));
                Assert.That(sourceSession.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
                    "Rejected exchange must leave the source boundary and active reference unchanged.");
            }
            finally
            {
                endOperation.Invoke(activeOwner, null);
            }

            Assert.That(activeOwner.TryPublishRestoredSession(sourceSession, candidateSession), Is.True,
                "An admitted restored candidate may publish at an idle owner-thread boundary.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(activeOwner, "activeSession"), Is.SameAs(candidateSession));
            Assert.That(activeOwner.Bootstrap, Is.SameAs(candidateSession.Composition));
            Assert.That(activeOwner.Runtime, Is.SameAs(candidateSession.Runtime));
            Assert.That(activeOwner.SpatialNetwork, Is.SameAs(candidateSession.SpatialNetwork));
            Assert.That(activeOwner.NpcChronicles, Is.SameAs(candidateSession.Composition.NpcChronicles));
            Assert.That(activeOwner.FullLog, Is.EqualTo(candidateSession.Logger.FullLog));
            Assert.That(activeOwner.CurrentDay, Is.EqualTo(sourceToken.AbsoluteDay));

            SpatialLocationRuntime candidateLocation = null;
            foreach (SpatialLocationRuntime location in candidateSession.SpatialNetwork.Locations)
            {
                candidateLocation = location;
                break;
            }
            Assert.That(candidateLocation, Is.Not.Null);
            Assert.That(activeOwner.TryGetSpatialLocation(candidateLocation.RuntimeId, out SpatialLocationRuntime resolved), Is.True);
            Assert.That(resolved, Is.SameAs(candidateLocation),
                "Post-exchange lookups must resolve through the candidate session's spatial owners.");
            Assert.That(sourceSession.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
                "Publishing the candidate must not mutate the detached source runtime.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(stagedOwnerObject);
            UnityEngine.Object.DestroyImmediate(activeOwnerObject);
        }
    }

    [Test]
    public void DailyV1RestoreStagesFreshGraphPublishesOnceAndContinuesDeterministically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);

        GameObject uninterruptedObject = new GameObject("P12-G uninterrupted continuation");
        GameObject restoredObject = new GameObject("P12-G restored continuation");
        try
        {
            TesteSimulacao uninterrupted = uninterruptedObject.AddComponent<TesteSimulacao>();
            TesteSimulacao restored = restoredObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(uninterrupted, config);
            ConfigureSelectedBootstrap(restored, config);
            InvokeInitializeSimulation(uninterrupted, null);
            InvokeInitializeSimulation(restored, null);

            SimulationActiveSession uninterruptedSession = ReadPrivateField<SimulationActiveSession>(
                uninterrupted, "activeSession");
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(
                restored, "activeSession");
            Assert.That(uninterruptedSession, Is.Not.Null);
            Assert.That(sourceSession, Is.Not.Null);

            CaptureTerminalP11History(uninterruptedSession.Runtime, "p12g-terminal-input");
            CaptureTerminalP11History(sourceSession.Runtime, "p12g-terminal-input");

            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure uninterruptedFirst),
                Is.True, uninterruptedFirst.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFirst),
                Is.True, sourceFirst.ToString());
            Assert.That(sourceSession.Runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken sourceToken,
                out DailyCaptureEligibilityFailure sourceTokenFailure), Is.True, sourceTokenFailure.ToString());

            Assert.That(sourceSession.Runtime.NpcRuntimes.Any(npc => npc?.CurrentAction != null), Is.True,
                "The authored continuation must exercise deterministic autonomous action selection; its authoritative current-action result is compared below.");

            string sourceFactsBeforeRestore = CaptureSelectedDailyFacts(sourceSession.Runtime);
            string sourceRootsBeforeRestore = CaptureContinuationRootFacts(sourceSession);
            string sourceGraphBeforeRestore = CaptureCompleteDailyV1OwnerProjection(
                sourceSession, sourceToken);
            string uninterruptedFactsAtBoundary = CaptureSelectedDailyFacts(uninterruptedSession.Runtime);
            Assert.That(sourceFactsBeforeRestore, Is.EqualTo(uninterruptedFactsAtBoundary));
            Assert.That(sourceGraphBeforeRestore,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    uninterruptedSession,
                    GetCompletedDailyToken(uninterruptedSession.Runtime))),
                "The C-F detached snapshots must agree across independently bootstrapped equivalent worlds.");
            Assert.That(CaptureContinuationRootFacts(sourceSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));

            Assert.That(restored.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure,
                    out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);

            SimulationActiveSession targetSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
            Assert.That(targetSession, Is.Not.Null.And.Not.SameAs(sourceSession),
                "The operation must publish one fresh active-session reference.");
            Assert.That(targetSession.Runtime, Is.Not.SameAs(sourceSession.Runtime));
            Assert.That(targetSession.Composition, Is.Not.SameAs(sourceSession.Composition));
            Assert.That(targetSession.IdentityRegistry, Is.Not.SameAs(sourceSession.IdentityRegistry));
            Assert.That(targetSession.SpatialNetwork, Is.Not.SameAs(sourceSession.SpatialNetwork));
            Assert.That(targetSession.Runtime.CurrentDay, Is.EqualTo(sourceToken.AbsoluteDay),
                "Staging must not execute another gameplay day.");
            Assert.That(targetSession.Runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken restoredToken,
                out DailyCaptureEligibilityFailure restoredTokenFailure), Is.True,
                restoredTokenFailure.ToString());
            Assert.That(restoredToken, Is.Not.SameAs(sourceToken));
            Assert.That(restoredToken.BoundaryProvenance,
                Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
            Assert.That(restoredToken.OwnerSections, Is.Not.SameAs(sourceToken.OwnerSections));
            Assert.That(CaptureSelectedDailyFacts(targetSession.Runtime), Is.EqualTo(sourceFactsBeforeRestore));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    targetSession, restoredToken), Is.EqualTo(sourceGraphBeforeRestore),
                "Restore must preserve every included typed C-F owner snapshot, not only selected facts and census metadata.");
            Assert.That(CaptureContinuationRootFacts(targetSession), Is.EqualTo(sourceRootsBeforeRestore),
                "Restore must preserve allocator, shared sequence, world lineage, and random root without replaying genesis or allocating replacement identities.");
            Assert.That(targetSession.Runtime.ActorChoiceStore.TemporalInputCount, Is.Zero);
            OwnerSectionCensusSnapshot restoredP11Section = restoredToken.OwnerSections.Single(
                section => section.SectionId == ActorChoiceP11CensusProvider.SectionId);
            OwnerSectionCensusWitness restoredTemporalWitness =
                targetSession.Composition.ActorChoiceTemporalCensusProvider.GetCurrentCensus();
            Assert.That(restoredP11Section.Revision, Is.GreaterThan(0L));
            Assert.That(restoredTemporalWitness.Revision, Is.EqualTo(restoredP11Section.Revision));
            Assert.That(restoredTemporalWitness.Cardinality, Is.Zero);
            Assert.That(restoredTemporalWitness.OwnerInstanceIdentity, Is.SameAs(restoredP11Section.OwnerInstanceIdentity));
            Assert.That(sourceSession.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
                "The detached source remains valid and unchanged after the candidate exchange.");
            Assert.That(CaptureSelectedDailyFacts(sourceSession.Runtime), Is.EqualTo(sourceFactsBeforeRestore));
            AssertOwnerVectorFactsMatch(sourceToken.OwnerSections, restoredToken.OwnerSections,
                requireFreshOwners: true);

            for (int boundary = 0; boundary < 2; boundary++)
            {
                Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFailure),
                    Is.True, controlFailure.ToString());
                Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredFailure),
                    Is.True, restoredFailure.ToString());
                Assert.That(restored.Runtime.TryGetCompletedDailyCaptureToken(
                    out DailyCaptureEligibilityToken nextRestoredToken,
                    out DailyCaptureEligibilityFailure nextRestoredFailure), Is.True,
                    nextRestoredFailure.ToString());
                Assert.That(uninterrupted.Runtime.TryGetCompletedDailyCaptureToken(
                    out DailyCaptureEligibilityToken nextControlToken,
                    out DailyCaptureEligibilityFailure nextControlFailure), Is.True,
                    nextControlFailure.ToString());
                Assert.That(nextRestoredToken.AbsoluteDay, Is.EqualTo(nextControlToken.AbsoluteDay));
                Assert.That(nextRestoredToken.CompletedCoreSequence,
                    Is.EqualTo(nextControlToken.CompletedCoreSequence));
                Assert.That(nextRestoredToken.BoundaryProvenance,
                    Is.EqualTo(DailyCaptureBoundaryProvenance.CompletedAdvance));
                AssertOwnerVectorFactsMatch(nextControlToken.OwnerSections, nextRestoredToken.OwnerSections,
                    requireFreshOwners: true);
                Assert.That(CaptureSelectedDailyFacts(restored.Runtime),
                    Is.EqualTo(CaptureSelectedDailyFacts(uninterrupted.Runtime)),
                    "Authoritative Daily-v1 facts must remain equal after each identical continuation advance.");
                Assert.That(CaptureCompleteDailyV1OwnerProjection(
                        ReadPrivateField<SimulationActiveSession>(restored, "activeSession"), nextRestoredToken),
                    Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(uninterruptedSession, nextControlToken)),
                    "Every included typed C-F owner snapshot must remain equal after each identical continuation advance.");
                Assert.That(CaptureContinuationRootFacts(
                        ReadPrivateField<SimulationActiveSession>(restored, "activeSession"), includeWorldIdentity: false),
                    Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)),
                    "Continuation must preserve identical allocator, shared-sequence, world-lineage, and random-root facts.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(uninterruptedObject);
            UnityEngine.Object.DestroyImmediate(restoredObject);
        }
    }

    [Test]
    public void DailyV1RestorePreservesDynamicNpcRosterTransitionAndContinuesDeterministically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject controlObject = new GameObject("P12-G dynamic roster uninterrupted control");
        GameObject sourceObject = new GameObject("P12-G dynamic roster restore source");
        try
        {
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(control, config);
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(control, null);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(controlSession, Is.Not.Null);
            Assert.That(sourceSession, Is.Not.Null);
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirst),
                Is.True, controlFirst.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFirst),
                Is.True, sourceFirst.ToString());

            NpcData lateNpcDefinition = SimulationTestFactory.CreateNpc("p12g-dynamic-roster-late-npc");
            string sourceLateNpcId = sourceSession.Composition.RuntimeIdAllocator.AllocateNpcId();
            string controlLateNpcId = controlSession.Composition.RuntimeIdAllocator.AllocateNpcId();
            Assert.That(sourceLateNpcId, Is.EqualTo(controlLateNpcId));
            Assert.That(sourceSession.Runtime.TryRegisterNpc(
                    new NpcRuntime(sourceLateNpcId, lateNpcDefinition),
                    out WorldNpcRegistryFailure sourceRegisterFailure), Is.True, sourceRegisterFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterNpc(
                    new NpcRuntime(controlLateNpcId, lateNpcDefinition),
                    out WorldNpcRegistryFailure controlRegisterFailure), Is.True, controlRegisterFailure.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceSecond),
                Is.True, sourceSecond.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlSecond),
                Is.True, controlSecond.ToString());

            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(sourceSession.Runtime);
            DailyCaptureEligibilityToken controlToken = GetCompletedDailyToken(controlSession.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlToken)),
                "Equivalent supported roster transitions must produce the same completed-boundary owner graph.");

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure, out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(restoredSession.AdmissionContext.IsRestoredContinuation, Is.True);
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(restoredToken.AbsoluteDay, Is.EqualTo(sourceToken.AbsoluteDay));
            Assert.That(restoredToken.CompletedCoreSequence, Is.EqualTo(sourceToken.CompletedCoreSequence));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken), Is.EqualTo(sourceGraph),
                "Restore must retain the post-transition dynamic roster and its typed owner vector.");

            Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredNext),
                Is.True, restoredNext.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNext),
                Is.True, controlNext.ToString());
            DailyCaptureEligibilityToken nextRestoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            DailyCaptureEligibilityToken nextControlToken = GetCompletedDailyToken(controlSession.Runtime);
            Assert.That(nextRestoredToken.AbsoluteDay, Is.EqualTo(nextControlToken.AbsoluteDay));
            Assert.That(nextRestoredToken.CompletedCoreSequence, Is.EqualTo(nextControlToken.CompletedCoreSequence));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, nextRestoredToken),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, nextControlToken)),
                "Identical continuation after restore must preserve deterministic owner-state parity after dynamic roster membership.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controlObject);
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsBeforeBoundaryWithoutChangingActiveSession()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G restore before completed boundary");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(bootstrap, config);
            InvokeInitializeSimulation(bootstrap, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            string before = CaptureSelectedDailyFacts(original.Runtime);

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic), Is.False);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.InvalidSourceSession), diagnostic);
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(before));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString(),
                "A rejected restore must leave the original graph able to continue normally.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    [Test]
    public void DailyV1RestoreDoesNotReplayFutureScheduledDirectiveAndItsFirstDueAdvanceMatches()
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(sourceConfig, Is.Not.Null);
        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        NpcActionData escapeAction = SimulationTestFactory.CreateAction(
            "p12g-no-replay-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        config.Actions.Add(escapeAction);
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig
        {
            absoluteDay = 5L,
            mode = ScheduledDirectiveMode.RequestAction,
            operation = ScheduledDirectiveOperation.EscapePrison,
            actor = config.Npcs[0].npc,
            action = escapeAction
        });

        GameObject uninterruptedObject = new GameObject("P12-G directive uninterrupted");
        GameObject restoredObject = new GameObject("P12-G directive restored");
        try
        {
            TesteSimulacao uninterrupted = uninterruptedObject.AddComponent<TesteSimulacao>();
            TesteSimulacao restored = restoredObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(uninterrupted, config);
            ConfigureSelectedBootstrap(restored, config);
            InvokeInitializeSimulation(uninterrupted, null);
            InvokeInitializeSimulation(restored, null);

            SimulationActiveSession uninterruptedSession = ReadPrivateField<SimulationActiveSession>(
                uninterrupted, "activeSession");
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
            Assert.That(uninterruptedSession, Is.Not.Null);
            Assert.That(sourceSession, Is.Not.Null);
            Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirst),
                Is.True, controlFirst.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFirst),
                Is.True, sourceFirst.ToString());

            ScheduledDirective sourceDirective = sourceSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(sourceDirective.AbsoluteDay, Is.EqualTo(5L));
            Assert.That(sourceDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending));
            long sourceDirectiveRevision = sourceSession.Composition.ScheduledDirectives.Revision;
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(sourceSession.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));

            Assert.That(restored.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure,
                    out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);
            SimulationActiveSession targetSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
            DailyCaptureEligibilityToken targetToken = GetCompletedDailyToken(targetSession.Runtime);
            ScheduledDirective restoredDirective = targetSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(restoredDirective.DirectiveId, Is.EqualTo(sourceDirective.DirectiveId));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending),
                "Staging a future directive must not dispatch, consume, retry, or mark it processed.");
            Assert.That(targetSession.Composition.ScheduledDirectives.Revision, Is.EqualTo(sourceDirectiveRevision));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(targetSession, targetToken), Is.EqualTo(sourceGraph));

            for (int day = 2; day <= 5; day++)
            {
                Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFailure),
                    Is.True, controlFailure.ToString());
                Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredFailure),
                    Is.True, restoredFailure.ToString());
                DailyCaptureEligibilityToken controlToken = GetCompletedDailyToken(uninterrupted.Runtime);
                DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
                Assert.That(CaptureCompleteDailyV1OwnerProjection(uninterruptedSession, controlToken),
                    Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                        ReadPrivateField<SimulationActiveSession>(restored, "activeSession"), restoredToken)),
                    "The pending directive must be applied once on its due day with identical authoritative results.");
            }

            ScheduledDirective controlDirective = uninterruptedSession.Composition.ScheduledDirectives.Directives.Single();
            restoredDirective = targetSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(controlDirective.State, Is.Not.EqualTo(ScheduledDirectiveState.Pending));
            Assert.That(restoredDirective.State, Is.EqualTo(controlDirective.State));
            Assert.That(restoredDirective.ProcessedDay, Is.EqualTo(controlDirective.ProcessedDay));
            Assert.That(targetSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(uninterruptedSession.Composition.ScheduledDirectives.Revision));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(uninterruptedObject);
            UnityEngine.Object.DestroyImmediate(restoredObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void DailyV1RestorePreservesActiveTravelPartyWithoutReplayingEffects()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject uninterruptedObject = new GameObject("P12-G active party uninterrupted");
        GameObject restoredObject = new GameObject("P12-G active party restored");
        try
        {
            TesteSimulacao uninterrupted = uninterruptedObject.AddComponent<TesteSimulacao>();
            TesteSimulacao restored = restoredObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(uninterrupted, config);
            ConfigureSelectedBootstrap(restored, config);
            InvokeInitializeSimulation(uninterrupted, null);
            InvokeInitializeSimulation(restored, null);

            SimulationActiveSession uninterruptedSession = ReadPrivateField<SimulationActiveSession>(
                uninterrupted, "activeSession");
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(
                restored, "activeSession");
            Assert.That(uninterruptedSession, Is.Not.Null);
            Assert.That(sourceSession, Is.Not.Null);

            ActionExecutionContext controlTravel = CreateDailyTravelPartyContext(uninterruptedSession);
            ActionExecutionContext sourceTravel = CreateDailyTravelPartyContext(sourceSession);
            Assert.That(uninterrupted.Runtime.TryStartTravelParty(controlTravel), Is.True,
                "The authored Daily-v1 cities support a normal active TravelParty commitment.");
            Assert.That(sourceSession.Runtime.TryStartTravelParty(sourceTravel), Is.True);
            Assert.That(uninterruptedSession.Composition.TravelParties.ActiveParties, Has.Count.EqualTo(1));
            Assert.That(sourceSession.Composition.TravelParties.ActiveParties, Has.Count.EqualTo(1));

            Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirst),
                Is.True, controlFirst.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFirst),
                Is.True, sourceFirst.ToString());
            Assert.That(sourceSession.Runtime.TryGetCompletedDailyCaptureToken(
                    out DailyCaptureEligibilityToken sourceToken,
                    out DailyCaptureEligibilityFailure sourceTokenFailure), Is.True,
                sourceTokenFailure.ToString());
            string beforeRestoreFacts = CaptureSelectedDailyFacts(sourceSession.Runtime);
            string beforeRestoreRoots = CaptureContinuationRootFacts(sourceSession);
            string beforeRestoreGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            Assert.That(beforeRestoreGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));

            TravelPartyRuntime sourceParty = sourceSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(sourceParty.IsActive, Is.True);
            Assert.That(sourceParty.TravelerRuntimeIds, Is.Not.Empty);
            float[] memberBalances = sourceParty.MemberRuntimeIds
                .Select(runtimeId => sourceSession.IdentityRegistry.TryGetNpc(runtimeId, out NpcRuntime npc)
                    ? npc.MoneyAccount.Balance : float.NaN)
                .ToArray();
            int[] memberProgress = sourceParty.MemberRuntimeIds
                .Select(runtimeId => sourceSession.IdentityRegistry.TryGetNpc(runtimeId, out NpcRuntime npc)
                    ? npc.TravelDaysRemaining : -1)
                .ToArray();

            Assert.That(restored.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure,
                    out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
            Assert.That(restoredSession, Is.Not.SameAs(sourceSession));
            Assert.That(CaptureSelectedDailyFacts(restoredSession.Runtime), Is.EqualTo(beforeRestoreFacts));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(sourceSession, includeWorldIdentity: false)));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken),
                Is.EqualTo(beforeRestoreGraph),
                "Restore must retain active commitment values/revisions without charging, moving, or reapplying its effects.");

            TravelPartyRuntime restoredParty = restoredSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(restoredParty.TravelPartyId, Is.EqualTo(sourceParty.TravelPartyId));
            Assert.That(restoredParty.TravelerRuntimeIds, Is.EqualTo(sourceParty.TravelerRuntimeIds));
            Assert.That(restoredParty.EscortRuntimeIds, Is.EqualTo(sourceParty.EscortRuntimeIds));
            Assert.That(restoredParty.OriginDecisionId, Is.EqualTo(sourceParty.OriginDecisionId));
            Assert.That(restoredParty.MemberRuntimeIds.Select(runtimeId =>
                    restoredSession.IdentityRegistry.TryGetNpc(runtimeId, out NpcRuntime npc)
                        ? npc.MoneyAccount.Balance : float.NaN), Is.EqualTo(memberBalances));
            Assert.That(restoredParty.MemberRuntimeIds.Select(runtimeId =>
                    restoredSession.IdentityRegistry.TryGetNpc(runtimeId, out NpcRuntime npc)
                        ? npc.TravelDaysRemaining : -1), Is.EqualTo(memberProgress));

            for (int boundary = 0; boundary < 4; boundary++)
            {
                Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFailure),
                    Is.True, controlFailure.ToString());
                Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredFailure),
                    Is.True, restoredFailure.ToString());
                DailyCaptureEligibilityToken controlToken = GetCompletedDailyToken(uninterruptedSession.Runtime);
                restoredSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
                restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
                Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken),
                    Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(uninterruptedSession, controlToken)),
                    "The active commitment must advance and resolve identically on every later boundary.");
            }

            Assert.That(uninterruptedSession.Composition.TravelParties.ActiveParties, Is.Empty,
                "The supported active commitment should reach the same terminal arrival on the control.");
            Assert.That(restoredSession.Composition.TravelParties.ActiveParties, Is.Empty);
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(uninterruptedObject);
            UnityEngine.Object.DestroyImmediate(restoredObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsCorruptedTravelPartyCrossOwnerBindingAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G party-link source");
        GameObject controlObject = new GameObject("P12-G party-link control");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            ConfigureSelectedBootstrap(control, config);
            InvokeInitializeSimulation(source, null);
            InvokeInitializeSimulation(control, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(controlSession, Is.Not.Null);
            Assert.That(original.Runtime.TryStartTravelParty(CreateDailyTravelPartyContext(original)), Is.True);
            Assert.That(controlSession.Runtime.TryStartTravelParty(CreateDailyTravelPartyContext(controlSession)), Is.True);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string originalGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            Assert.That(source.TryRestoreDailyContinuationForTest(candidate =>
            {
                TravelPartyRuntime party = candidate.Composition.TravelParties.ActiveParties.Single();
                Assert.That(candidate.IdentityRegistry.TryGetNpc(party.MemberRuntimeIds[0], out NpcRuntime member), Is.True);
                WritePrivateField(member, "activeTravelPartyId", "p12g/missing-party");
            }, out P12GDailyV1RestoreFailure restoreFailure, out string diagnostic), Is.False);
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.BindingValidationFailed));
            Assert.That(diagnostic, Does.Contain("ActiveTravelPartyId"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(originalGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedAdvance),
                Is.True, retainedAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure expectedAdvance),
                Is.True, expectedAdvance.ToString());
            DailyCaptureEligibilityToken retainedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken expectedToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedGraph = CaptureCompleteDailyV1OwnerProjection(original, retainedToken);
            Assert.That(continuedGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, expectedToken)),
                "Rejecting a broken private reciprocal link must preserve the old session's later continuation.");

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure,
                    out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(continuedGraph));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1ExpeditionZeroWitnessRequiresExactRequiredOwnerAndMatchingRevision()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G Expedition census witness");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(bootstrap, config);
            InvokeInitializeSimulation(bootstrap, null);
            SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(session, Is.Not.Null);
            ExpeditionStore store = session.Composition.Expeditions;

            OwnerSectionCensusWitness witness = new ExpeditionCensusProvider(store).GetCurrentCensus();
            OwnerSectionCensusSnapshot requiredRow = new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
            Assert.That(P12GDailyV1RestoreCoordinator.TryValidateDailyV1ExpeditionExactZero(
                session.Composition, new[] { requiredRow }, out string diagnostic), Is.True, diagnostic);

            OwnerSectionCensusSnapshot wrongRole = new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
            Assert.That(P12GDailyV1RestoreCoordinator.TryValidateDailyV1ExpeditionExactZero(
                session.Composition, new[] { wrongRole }, out diagnostic), Is.False);

            OwnerSectionCensusSnapshot wrongOwner = new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                new object(), witness.Cardinality, witness.Revision);
            Assert.That(P12GDailyV1RestoreCoordinator.TryValidateDailyV1ExpeditionExactZero(
                session.Composition, new[] { wrongOwner }, out diagnostic), Is.False);

            Assert.That(store.Add(CreateUnsupportedDailyV1Expedition()), Is.True);
            witness = new ExpeditionCensusProvider(store).GetCurrentCensus();
            OwnerSectionCensusSnapshot populatedRow = new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
            Assert.That(P12GDailyV1RestoreCoordinator.TryValidateDailyV1ExpeditionExactZero(
                session.Composition, new[] { populatedRow }, out diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain("cardinality zero"));
            Assert.That(store.Remove("expedition-000001"), Is.True);

            witness = new ExpeditionCensusProvider(store).GetCurrentCensus();
            Assert.That(witness.Cardinality, Is.Zero);
            Assert.That(witness.Revision, Is.EqualTo(2L));
            OwnerSectionCensusSnapshot emptyAfterRemoval = new OwnerSectionCensusSnapshot(
                witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
            Assert.That(P12GDailyV1RestoreCoordinator.TryValidateDailyV1ExpeditionExactZero(
                session.Composition, new[] { emptyAfterRemoval }, out diagnostic), Is.True, diagnostic);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    [Test]
    public void DailyV1PopulatedExpeditionCannotProduceCompletedBoundaryForRestore()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G populated source Expedition rejection");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(original.Composition.Expeditions.Add(CreateUnsupportedDailyV1Expedition()), Is.True);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.False);
            Assert.That(advanceFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
            Assert.That(original.Runtime.TryGetCompletedDailyCaptureToken(
                    out _, out DailyCaptureEligibilityFailure tokenFailure), Is.False);
            Assert.That(tokenFailure, Is.EqualTo(DailyCaptureEligibilityFailure.RuntimeFaulted));
            OwnerSectionCensusWitness expeditionWitness =
                new ExpeditionCensusProvider(original.Composition.Expeditions).GetCurrentCensus();
            Assert.That(expeditionWitness.Cardinality, Is.EqualTo(1));
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsTargetExpeditionAddedAfterOwnerCensusAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G target Expedition post-census rejection");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);

            SimulationActiveSession privateCandidate = null;
            bool targetMutationApplied = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage != P12GDailyV1RestoreStage.TargetOwnerVectorCaptured) return;
                        Assert.That(privateCandidate, Is.Not.Null);
                        Assert.That(privateCandidate.Composition.Expeditions.Add(CreateUnsupportedDailyV1Expedition()),
                            Is.True);
                        targetMutationApplied = true;
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(targetMutationApplied, Is.True);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("exact-empty Expedition owner"));
            Assert.That(restored, Is.Null);
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(sourceGraph));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void DailyV1RestoreChecksOpaqueActorAndPartyDecisionReferencesAgainstAllocatorHighWater()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G opaque decision references");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(original, Is.Not.Null);
            CaptureTerminalP11HistoryWithDecisionReference(
                original.Runtime, "p12g-opaque-decision", "opaque-decision-reference");
            Assert.That(original.Runtime.TryStartTravelParty(CreateDailyTravelPartyContext(original)), Is.True);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            DailyCaptureEligibilityToken token = GetCompletedDailyToken(original.Runtime);
            string graph = CaptureCompleteDailyV1OwnerProjection(original, token);

            Assert.That(source.TryRestoreDailyContinuationForTest(candidate =>
            {
                List<ActorChoiceInput> storedInputs = ReadPrivateField<List<ActorChoiceInput>>(
                    candidate.Runtime.ActorChoiceStore, "inputs");
                ActorChoiceDisposition disposition = storedInputs
                    .Single(input => input.WorldCommandId == "p12g-opaque-decision")
                    .Dispositions.Single(value => !string.IsNullOrWhiteSpace(value.DecisionRecordId));
                WritePrivateField(disposition, "<DecisionRecordId>k__BackingField", "decision-999999");
            }, out P12GDailyV1RestoreFailure actorFailure, out string actorDiagnostic), Is.False);
            Assert.That(actorFailure, Is.EqualTo(P12GDailyV1RestoreFailure.BindingValidationFailed));
            Assert.That(actorDiagnostic, Does.Contain("retained opaque decision reference"));

            Assert.That(source.TryRestoreDailyContinuationForTest(candidate =>
            {
                TravelPartyRuntime party = candidate.Composition.TravelParties.ActiveParties.Single();
                WritePrivateField(party, "originDecisionId", "decision-999999");
            }, out P12GDailyV1RestoreFailure partyFailure, out string partyDiagnostic), Is.False);
            Assert.That(partyFailure, Is.EqualTo(P12GDailyV1RestoreFailure.BindingValidationFailed));
            Assert.That(partyDiagnostic, Does.Contain("retained opaque decision reference"));

            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, token), Is.EqualTo(graph));
            ActorChoiceInput retainedInput = original.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == "p12g-opaque-decision");
            Assert.That(retainedInput.Dispositions.Single(value => value.DecisionRecordId != null).DecisionRecordId,
                Is.EqualTo("opaque-decision-reference"));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            ActorChoiceInput restoredInput = restored.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == "p12g-opaque-decision");
            Assert.That(restoredInput.Dispositions.Single(value => value.DecisionRecordId != null).DecisionRecordId,
                Is.EqualTo("opaque-decision-reference"), "Opaque references must be retained without a decision-record lookup.");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(graph));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPrivateTargetExpeditionBeforePublicationAndKeepsOldSessionRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G Expedition target source");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            DailyCaptureEligibilityToken token = GetCompletedDailyToken(original.Runtime);
            string graph = CaptureCompleteDailyV1OwnerProjection(original, token);

            Assert.That(source.TryRestoreDailyContinuationForTest(candidate =>
            {
                Assert.That(candidate.Composition.Expeditions.Add(CreateUnsupportedDailyV1Expedition()), Is.True);
            }, out P12GDailyV1RestoreFailure failure, out string diagnostic), Is.False);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("OwnerCoverageIncomplete"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, token), Is.EqualTo(graph));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(graph), "A valid retry must reconstruct the unchanged old completed boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [TestCase("allocator-high-water")]
    [TestCase("record-sequence")]
    [TestCase("owner-revision")]
    [TestCase("p8-location-cardinality")]
    [TestCase("p8-authoritative-location-cardinality")]
    [TestCase("wrong-family-identity-key")]
    public void DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically(string corruptionKind)
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G corrupted graph source " + corruptionKind);
        GameObject controlObject = new GameObject("P12-G corrupted graph control " + corruptionKind);
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            ConfigureSelectedBootstrap(control, config);
            InvokeInitializeSimulation(source, null);
            InvokeInitializeSimulation(control, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(controlSession, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);

            bool corrupted = false;
            bool restoreSucceeded = source.TryRestoreDailyContinuationForTest(candidate =>
            {
                Assert.That(candidate, Is.Not.Null);
                switch (corruptionKind)
                {
                    case "allocator-high-water":
                        WritePrivateField(candidate.Composition.RuntimeIdAllocator, "nextNpcSequence", 1L);
                        break;
                    case "record-sequence":
                        WritePrivateField(candidate.Composition.RecordSequence, "nextSequence", 1L);
                        break;
                    case "owner-revision":
                        NpcRuntime candidateNpc = candidate.Runtime.NpcRuntimes.First();
                        long accountRevision = ReadPrivateField<long>(candidateNpc.MoneyAccount, "revision");
                        WritePrivateField(candidateNpc.MoneyAccount, "revision", accountRevision + 1L);
                        break;
                    case "p8-location-cardinality":
                        Dictionary<string, SpatialLocationRuntime> locations = ReadPrivateField<Dictionary<string, SpatialLocationRuntime>>(
                            candidate.IdentityRegistry, "locationsByRuntimeId");
                        locations.Clear();
                        break;
                    case "p8-authoritative-location-cardinality":
                        HexRecord anchorHex = candidate.Composition.SpatialAuthority.Hexes.First();
                        Assert.That(candidate.Composition.SpatialAuthority.TryRegisterLocation(
                            new LocationRecord(new LocationId("p12g-extra-location"), anchorHex.Id),
                            out SpatialAuthorityFailure locationFailure), Is.True, locationFailure?.ToString());
                        break;
                    case "wrong-family-identity-key":
                        Dictionary<string, CityRuntime> cities = ReadPrivateField<Dictionary<string, CityRuntime>>(
                            candidate.IdentityRegistry, "citiesByRuntimeId");
                        CityRuntime city = cities.Values.First();
                        NpcRuntime npc = candidate.Runtime.NpcRuntimes.First();
                        Assert.That(city.RuntimeId, Is.Not.EqualTo(npc.RuntimeId));
                        Assert.That(cities.Remove(city.RuntimeId), Is.True);
                        cities.Add(npc.RuntimeId, city);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(corruptionKind), corruptionKind, "Unknown P12-G corruption case.");
                }
                corrupted = true;
            }, out P12GDailyV1RestoreFailure restoreFailure, out string diagnostic);

            Assert.That(corrupted, Is.True);
            Assert.That(restoreSucceeded, Is.False, diagnostic);
            Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
            Assert.That(restoreFailure, Is.EqualTo(corruptionKind == "allocator-high-water"
                || corruptionKind == "wrong-family-identity-key"
                ? P12GDailyV1RestoreFailure.BindingValidationFailed
                : P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedAdvance),
                Is.True, retainedAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure expectedAdvance),
                Is.True, expectedAdvance.ToString());
            DailyCaptureEligibilityToken retainedToken = GetCompletedDailyToken(original.Runtime);
            string continuedGraph = CaptureCompleteDailyV1OwnerProjection(original, retainedToken);
            Assert.That(continuedGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "A rejected staged graph must not change subsequent normal continuation.");

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    retried, GetCompletedDailyToken(retried.Runtime)), Is.EqualTo(continuedGraph),
                "A later valid restore must reconstruct the same graph after every rejection case.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsTransferredSourceTokenBeforeReturningAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G admitted-target token drift source");
        GameObject controlObject = new GameObject("P12-G admitted-target token drift control");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            ConfigureSelectedBootstrap(control, config);
            InvokeInitializeSimulation(source, null);
            InvokeInitializeSimulation(control, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(controlSession, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            SimulationActiveSession privateCandidate = null;
            bool tokenCorruptedAfterAdmission = false;

            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage != P12GDailyV1RestoreStage.BoundaryAdmitted) return;
                        Assert.That(privateCandidate, Is.Not.Null);
                        Assert.That(privateCandidate.Runtime.HasValidRestoredDailyBoundaryAdmission(), Is.True);
                        WritePrivateField(
                            privateCandidate.Runtime, "currentDailyCaptureToken", sourceToken);
                        tokenCorruptedAfterAdmission = true;
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(tokenCorruptedAfterAdmission, Is.True);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetAdmissionFailed));
            Assert.That(diagnostic, Does.Contain("admitted target or source boundary changed"));
            Assert.That(restored, Is.Null);
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedAdvance),
                Is.True, retainedAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure expectedAdvance),
                Is.True, expectedAdvance.ToString());
            string continuedGraph = CaptureCompleteDailyV1OwnerProjection(original, GetCompletedDailyToken(original.Runtime));
            Assert.That(continuedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(continuedGraph));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestorePublicationGateRejectionPreservesSourceAndLaterContinuation()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G publication gate rejection source");
        GameObject controlObject = new GameObject("P12-G publication gate rejection control");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            ConfigureSelectedBootstrap(control, config);
            InvokeInitializeSimulation(source, null);
            InvokeInitializeSimulation(control, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(controlSession, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                original, out SimulationActiveSession candidate, out P12GDailyV1RestoreFailure stageFailure,
                out string stageDiagnostic), Is.True, stageFailure + ": " + stageDiagnostic);
            Assert.That(candidate, Is.Not.Null);

            MethodInfo beginOperation = typeof(TesteSimulacao).GetMethod(
                "TryBeginActiveSessionOperation", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(SimulationActiveSession).MakeByRefType() }, null);
            MethodInfo endOperation = typeof(TesteSimulacao).GetMethod(
                "EndActiveSessionOperation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(beginOperation, Is.Not.Null);
            Assert.That(endOperation, Is.Not.Null);
            object[] operationArguments = { null };
            Assert.That((bool)beginOperation.Invoke(source, operationArguments), Is.True);
            Assert.That(operationArguments[0], Is.SameAs(original));
            try
            {
                Assert.That(source.TryPublishRestoredSession(original, candidate), Is.False,
                    "A candidate must not publish while an active-session operation is held.");
            }
            finally
            {
                endOperation.Invoke(source, null);
            }

            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedAdvance),
                Is.True, retainedAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure expectedAdvance),
                Is.True, expectedAdvance.ToString());
            string continuedGraph = CaptureCompleteDailyV1OwnerProjection(original, GetCompletedDailyToken(original.Runtime));
            Assert.That(continuedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(continuedGraph));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }
    [TestCase((int)P12GDailyV1RestoreStage.SourceCaptured)]
    [TestCase((int)P12GDailyV1RestoreStage.RootsStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.DStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.FStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.OwnersStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.CandidateComposed)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetOwnerVectorCaptured)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetChecksCompleted)]
    [TestCase((int)P12GDailyV1RestoreStage.CandidateGuardBound)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured)]
    [TestCase((int)P12GDailyV1RestoreStage.BoundaryAdmitted)]
    [TestCase((int)P12GDailyV1RestoreStage.BeforePublication)]
    public void DailyV1RestoreInjectedPrivateFailureKeepsOldSessionHealthyAndAllowsLaterRestore(
        int injectedStageValue)
    {
        P12GDailyV1RestoreStage injectedStage = (P12GDailyV1RestoreStage)injectedStageValue;
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G restore private-stage atomicity");
        GameObject controlObject = new GameObject("P12-G restore private-stage control");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(bootstrap, config);
            ConfigureSelectedBootstrap(control, config);
            InvokeInitializeSimulation(bootstrap, null);
            InvokeInitializeSimulation(control, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(controlSession, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            Assert.That(original.Runtime.TryGetCompletedDailyCaptureToken(
                    out DailyCaptureEligibilityToken originalToken,
                    out DailyCaptureEligibilityFailure tokenFailure), Is.True,
                tokenFailure.ToString());
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);
            bool injected = false;

            Assert.That(bootstrap.TryRestoreDailyContinuation(stage =>
            {
                if (stage != injectedStage) return;
                injected = true;
                throw new InvalidOperationException("P12-G test failure at " + stage + ".");
            }, out P12GDailyV1RestoreFailure restoreFailure, out string diagnostic), Is.False);
            Assert.That(injected, Is.True, "The selected private stage must be reached before fault injection.");
            Assert.That(restoreFailure, Is.Not.EqualTo(P12GDailyV1RestoreFailure.None));
            Assert.That(diagnostic, Does.Contain("P12-G test failure at " + injectedStage));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken),
                Is.EqualTo(beforeOwnerGraph),
                "A discarded candidate must leave every included C-F owner value and revision unchanged.");

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString(),
                "A rejected restore must leave the still-active original graph able to continue normally.");
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryGetCompletedDailyCaptureToken(
                    out DailyCaptureEligibilityToken continuedToken,
                    out DailyCaptureEligibilityFailure continuedFailure), Is.True,
                continuedFailure.ToString());
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, the original and uninterrupted runtimes must continue identically with the same input.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure,
                    out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(restored.Runtime.TryGetCompletedDailyCaptureToken(
                    out DailyCaptureEligibilityToken restoredToken,
                    out DailyCaptureEligibilityFailure restoredTokenFailure), Is.True,
                restoredTokenFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken),
                Is.EqualTo(continuedOwnerGraph),
                "A successful retry after each injected failure must reconstruct the original graph after its normal continuation.");
            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString(),
                "A later valid restore and normal advance must succeed after a discarded candidate.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPrivateTargetMutationAfterGraphChecksAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G target epoch admission race");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);

            SimulationActiveSession privateCandidate = null;
            bool mutationInjected = false;
            bool targetRosterMutationApplied = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage != P12GDailyV1RestoreStage.TargetChecksCompleted) return;
                        mutationInjected = true;
                        Assert.That(privateCandidate, Is.Not.Null,
                            "The private candidate must be available before its final admission check.");
                        Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.False,
                            "Whole-graph checks must finish before binding the candidate mutation guard.");
                        NpcRuntime lateNpc = new NpcRuntime(
                            "p12g-private-target-late-npc",
                            SimulationTestFactory.CreateNpc("p12g-private-target-late-npc"));
                        Assert.That(privateCandidate.Runtime.TryRegisterNpc(
                                lateNpc, out WorldNpcRegistryFailure registerFailure),
                            Is.True, registerFailure.ToString());
                        targetRosterMutationApplied = true;
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(mutationInjected, Is.True);
            Assert.That(targetRosterMutationApplied, Is.True);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("owner vector changed during mutation-guard binding"));
            Assert.That(restored, Is.Null, "A target changed after graph checks must never be returned for publication.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(sourceGraph), "A rejected private-target mutation must not affect a later valid reconstruction.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsMutationGuardBindFailureBeforeBoundaryAdmissionAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G mutation-guard bind failure");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(original, Is.Not.Null);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);

            SimulationActiveSession privateCandidate = null;
            bool conflictInjected = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage != P12GDailyV1RestoreStage.TargetChecksCompleted) return;
                        Assert.That(privateCandidate, Is.Not.Null);
                        Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.False);
                        AuthoritativeMutationGuard foreignGuard = new AuthoritativeMutationGuard();
                        Assert.That(privateCandidate.Runtime.Cities[0].TryBindRuntimeMutationGuard(foreignGuard),
                            Is.True, "The private candidate CityRuntime should be unbound before guard installation.");
                        conflictInjected = true;
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(conflictInjected, Is.True);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.BindingValidationFailed));
            Assert.That(diagnostic, Does.Contain("already owned by another SimulationRuntime"));
            Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.False,
                "A failed guard preflight must not report a complete candidate binding.");
            Assert.That(restored, Is.Null);
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            Assert.That(retried.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(sourceGraph));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    private static void AssertP14FiniteProfileWithP10Rejected(bool generated)
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.FiniteReserveDaily, 7, out _);
        ExplorableSiteData ruin = SimulationTestFactory.CreateExplorableSite(
            generated ? "p14-p10b-deferred-ruin" : "p14-p10a-deferred-ruin", ExplorableSiteKind.Ruin);
        config.authoredP10RuinSite = ruin;
        if (generated)
        {
            config.genesisProfileContractIdentity = SimulationGenesisPipeline.P10BGeneratedRuinProfileContractIdentity;
            config.p10bStableSiteKey = "p14/test/ruin/one";
        }
        else
        {
            config.genesisProfileContractIdentity = P10RuinLocalTopologyGenesis.ContractIdentity;
        }

        GameObject bootstrapObject = new GameObject(generated
            ? "P14 with P10-B profile deferred"
            : "P14 with P10-A profile deferred");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureUnscopedBootstrap(bootstrap, config);
            int worldIdentityAllocations = 0;
            WritePrivateField(bootstrap, "worldIdentityAllocator", new Func<WorldId>(() =>
            {
                worldIdentityAllocations++;
                return null;
            }));
            List<string> completedStages = new List<string>();

            TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
                InvokeInitializeSimulation(bootstrap, completedStages.Add));

            Assert.That(thrown.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(thrown.InnerException.Message, Does.Contain("P14/P10 combined bootstrap profile is deferred"));
            Assert.That(completedStages, Is.Empty);
            Assert.That(worldIdentityAllocations, Is.Zero);
            Assert.That(ReadPrivateField<RuntimeIdAllocator>(bootstrap, "runtimeIdAllocator"), Is.Null);
            Assert.That(ReadPrivateField<SimulationRuntime>(bootstrap, "simulationRuntime"), Is.Null);
            Assert.That(ReadPrivateField<List<CityRuntime>>(bootstrap, "cityRuntimeList"), Is.Empty);
            Assert.That(bootstrap.Bootstrap, Is.Null);
            Assert.That(bootstrap.Runtime, Is.Null);
            Assert.That(bootstrap.CurrentDay, Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(ruin);
        }
    }
    private static SimulationConfigData CreateP14AdmissionConfig(
        LocalMaterialFlowProfile profile,
        int initialReserve,
        out ItemData item)
    {
        SimulationConfigData template = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(template, Is.Not.Null);
        SimulationConfigData config = UnityEngine.Object.Instantiate(template);
        config.simulationName = "P14-B " + profile;
        config.enabledModules = new List<SimulationModule> { SimulationModule.Economy };
        config.cities = new List<CityData>();
        config.npcs = new List<NpcSimulationConfig>();
        config.explorableSites = new List<ExplorableSiteConfig>();
        config.initialWarrants = new List<InitialWantedRecordConfig>();
        config.scheduledDirectives = new List<ScheduledDirectiveConfig>();
        config.authoredP10RuinSite = null;
        config.genesisProfileContractIdentity = string.Empty;
        config.p10bStableSiteKey = string.Empty;

        item = SimulationTestFactory.CreateItem("p14-admission-" + profile);
        CityData city = SimulationTestFactory.CreateCityData(
            "city.p14.admission." + profile,
            new MarketItemConfig
            {
                item = item,
                initialAmount = 10,
                desiredAmount = 20,
                consumptionPer1000Population = 1f
            });
        city.settlementSemanticId = "settlement.p14.admission";
        city.materialFlowLocationId = config.authoredLocationId;
        city.marketStoreSemanticId = "store.p14.admission";
        city.materialFlowProfile = profile;
        city.initialPopulation = 1000;
        city.populationConsumption = new PopulationConsumptionConfig
        {
            paymentMode = ConsumptionPaymentMode.Free
        };
        city.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = 5,
            initialReserve = initialReserve,
            productionSourceId = "source.p14.admission",
            contentRevision = "p14-content-v1"
        });
        config.cities.Add(city);
        return config;
    }

    private static SimulationRuntime CreateSpatialAdmissionRuntime(
        int npcCount = 10,
        bool populateLegacyNetwork = true,
        int legacyNetworkRouteCount = 2,
        SpatialAuthorityStore spatialAuthorityStore = null)
    {
        CityRuntime[] cities =
        {
            SimulationTestFactory.CreateCity("admission-city-a", "admission-city-location-a"),
            SimulationTestFactory.CreateCity("admission-city-b", "admission-city-location-b")
        };
        RuntimeIdentityRegistry identities = new RuntimeIdentityRegistry();
        foreach (CityRuntime city in cities)
            Assert.That(identities.RegisterCity(city), Is.True);

        NpcRuntime[] npcs = new NpcRuntime[npcCount];
        for (int i = 0; i < npcs.Length; i++)
        {
            string id = "admission-npc-" + i;
            npcs[i] = new NpcRuntime(id, SimulationTestFactory.CreateNpc(id), cities[i % cities.Length], 1f);
            Assert.That(identities.RegisterNpc(npcs[i]), Is.True);
        }

        SpatialLocationRuntime[] locations =
        {
            new SpatialLocationRuntime("admission-network-location-a"),
            new SpatialLocationRuntime("admission-network-location-b")
        };
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identities);
        for (int i = 0; i < locations.Length; i++)
        {
            bool registered = populateLegacyNetwork
                ? network.RegisterLocation(locations[i])
                : identities.RegisterLocation(locations[i]);
            Assert.That(registered, Is.True);
        }

        SpatialRouteRuntime[] routes =
        {
            new SpatialRouteRuntime("admission-network-route-a", locations[0], locations[1], 1),
            new SpatialRouteRuntime("admission-network-route-b", locations[1], locations[0], 1)
        };
        for (int i = 0; i < routes.Length; i++)
        {
            bool registered = populateLegacyNetwork && i < legacyNetworkRouteCount
                ? network.RegisterRoute(routes[i])
                : identities.RegisterRoute(routes[i]);
            Assert.That(registered, Is.True);
        }

        return new SimulationRuntime(
            new SimulationTime(),
            cities,
            npcs,
            economyEnabled: false,
            explorableSiteStore: new ExplorableSiteStore(),
            spatialAuthorityStore: spatialAuthorityStore ?? CreateSingleLocationSpatialAuthority(),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            runtimeIdentityRegistry: identities,
            spatialNetworkRuntime: network,
            requireP12RuntimeIdentitySpatialCensusOwners: true);
    }

    private static SpatialAuthorityStore CreateSingleLocationSpatialAuthority()
    {
        const string hexId = "admission-hex";
        const string locationId = "admission-location";
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("admission-scale", "fixture", "v1", 1m, "step"),
            new[]
            {
                new HexRecord(
                    new HexId(hexId),
                    new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain.admission"), "v1"))
            },
            new[] { new LocationRecord(new LocationId(locationId), new HexId(hexId)) });
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        if (!authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure))
            throw new InvalidOperationException("Could not create the single-location P8-A admission fixture: " + failure);
        return authority;
    }

    private static SpatialAuthorityStore CreateAdjacentSpatialAuthority()
    {
        HexRecord[] hexes =
        {
            CreateAdmissionHex("admission-hex-a", 0, 0),
            CreateAdmissionHex("admission-hex-b", 1, 0)
        };
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("admission-passage-scale", "fixture", "v1", 1m, "step"),
            hexes,
            new[] { new LocationRecord(new LocationId("admission-passage-location"), new HexId("admission-hex-a")) });
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        if (!authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure))
            throw new InvalidOperationException("Could not create the adjacent P8-B admission fixture: " + failure);
        return authority;
    }

    private static HexRecord CreateAdmissionHex(string id, int q, int r)
    {
        return new HexRecord(
            new HexId(id),
            new HexCoordinate(q, r),
            new TerrainReference(new TerrainDefinitionId("terrain.admission"), "v1"));
    }

    private static void AssertFixedOwnerAdmissionRejectsNonzero(
        IOwnerSectionCensusProvider provider,
        object expectedOwner)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.Cardinality, Is.GreaterThan(0));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(expectedOwner));
        MethodInfo registration = typeof(SimulationRuntime).GetMethod(
            "TryRegisterP12FixedOwnerSection",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(registration, Is.Not.Null);
        bool accepted = (bool)registration.Invoke(null, new object[]
        {
            new ContinuationCensusProtocol(),
            provider,
            witness.SectionId,
            witness.SchemaVersion,
            OwnerSectionRole.ExplicitlyEmpty,
            expectedOwner,
            false,
            null
        });
        Assert.That(accepted, Is.False, witness.SectionId);
    }

    private static void AssertDailyProfileAdmissionRejected(Action compose)
    {
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
        {
            compose();
        });
        Assert.That(failure.Message, Does.Contain("P12 runtime-admission adapter could not bind"));
    }

    private static void AssertOwnerVectorFactsMatch(
        IReadOnlyList<OwnerSectionCensusSnapshot> expected,
        IReadOnlyList<OwnerSectionCensusSnapshot> actual,
        bool requireFreshOwners)
    {
        Assert.That(expected, Is.Not.Null);
        Assert.That(actual, Is.Not.Null);
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        Dictionary<string, OwnerSectionCensusSnapshot> actualById = actual.ToDictionary(
            section => section.SectionId, StringComparer.Ordinal);
        List<OwnerSectionCensusSnapshot> orderedExpected = new List<OwnerSectionCensusSnapshot>();
        List<OwnerSectionCensusSnapshot> orderedActual = new List<OwnerSectionCensusSnapshot>();
        foreach (OwnerSectionCensusSnapshot expectedSection in expected)
        {
            Assert.That(actualById.TryGetValue(expectedSection.SectionId, out OwnerSectionCensusSnapshot actualSection),
                Is.True, expectedSection.SectionId);
            Assert.That(actualSection.SchemaVersion, Is.EqualTo(expectedSection.SchemaVersion), expectedSection.SectionId);
            Assert.That(actualSection.Role, Is.EqualTo(expectedSection.Role), expectedSection.SectionId);
            Assert.That(actualSection.Cardinality, Is.EqualTo(expectedSection.Cardinality), expectedSection.SectionId);
            Assert.That(actualSection.Revision, Is.EqualTo(expectedSection.Revision), expectedSection.SectionId);
            if (requireFreshOwners)
                Assert.That(actualSection.OwnerInstanceIdentity, Is.Not.SameAs(expectedSection.OwnerInstanceIdentity),
                    expectedSection.SectionId);
            orderedExpected.Add(expectedSection);
            orderedActual.Add(actualSection);
        }

        for (int left = 0; left < orderedExpected.Count; left++)
        {
            for (int right = left + 1; right < orderedExpected.Count; right++)
            {
                Assert.That(
                    ReferenceEquals(orderedExpected[left].OwnerInstanceIdentity, orderedExpected[right].OwnerInstanceIdentity),
                    Is.EqualTo(ReferenceEquals(orderedActual[left].OwnerInstanceIdentity, orderedActual[right].OwnerInstanceIdentity)),
                    orderedExpected[left].SectionId + " / " + orderedExpected[right].SectionId);
            }
        }
    }

    private static void CaptureTerminalP11HistoryWithDecisionReference(
        SimulationRuntime runtime,
        string commandId,
        string decisionReference)
    {
        Assert.That(runtime, Is.Not.Null);
        PersonId personId = new PersonId("p12g-terminal-actor-" + commandId);
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(personId), out PersonStoreFailure personFailure),
            Is.True, personFailure.ToString());
        Assert.That(runtime.TryCaptureActorChoiceInput(
            commandId, personId, "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, out ActorChoiceStoreFailureCode captureFailure),
            Is.True, captureFailure.ToString());
        ActorChoiceInput input = runtime.ActorChoiceStore.Inputs.Single(value => value.WorldCommandId == commandId);
        Assert.That(runtime.ActorChoiceStore.TryMarkDispatchStarted(
            input.InputId, runtime.CurrentDay, 0, decisionReference,
            out ActorChoiceStoreFailureCode dispatchFailure), Is.True, dispatchFailure.ToString());
        Assert.That(runtime.ActorChoiceStore.TryRecordAttemptReturned(
            input.InputId, runtime.CurrentDay, 0, null,
            out ActorChoiceStoreFailureCode attemptFailure), Is.True, attemptFailure.ToString());
    }

    private static void CaptureTerminalP11History(SimulationRuntime runtime, string commandId)
    {
        Assert.That(runtime, Is.Not.Null);
        PersonId personId = new PersonId("p12g-terminal-actor-" + commandId);
        Assert.That(runtime.TryRegisterPerson(
                new PersonRuntime(personId), out PersonStoreFailure personFailure), Is.True,
            personFailure.ToString());
        Assert.That(runtime.TryCaptureActorChoiceInput(
                commandId,
                personId,
                "sell-goods",
                WorldCommandOrigin.System,
                WorldCommandAuthorityMode.Request,
                out ActorChoiceStoreFailureCode captureFailure), Is.True,
            captureFailure.ToString());

        ActorChoiceInput pending = runtime.ActorChoiceStore.Inputs.Single(
            input => input.WorldCommandId == commandId);
        Assert.That(runtime.ActorChoiceStore.TryReject(
                pending.InputId,
                runtime.CurrentDay,
                0,
                ActorChoiceFailure.ActionUnavailable,
                out ActorChoiceStoreFailureCode rejectFailure), Is.True,
            rejectFailure.ToString());
    }

    private static string CaptureSelectedDailyFacts(SimulationRuntime runtime)
    {
        Assert.That(runtime, Is.Not.Null);
        StringBuilder facts = new StringBuilder();
        facts.Append("day=").Append(runtime.CurrentDay.ToString(CultureInfo.InvariantCulture));

        facts.Append("|actor-choice-revision=")
            .Append(runtime.ActorChoiceStore.CensusRevision.ToString(CultureInfo.InvariantCulture))
            .Append(",temporal=")
            .Append(runtime.ActorChoiceStore.TemporalInputCount.ToString(CultureInfo.InvariantCulture));
        foreach (ActorChoiceInput input in runtime.ActorChoiceStore.Inputs.OrderBy(value => value.InputSequence))
        {
            facts.Append("|actor-choice=").Append(input.InputId?.Value)
                .Append(',').Append(input.WorldCommandId)
                .Append(',').Append(input.InputSequence.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(input.PersonId?.Value)
                .Append(',').Append(input.ActionDefinitionId)
                .Append(',').Append(input.Origin)
                .Append(',').Append(input.Authority)
                .Append(',').Append(input.CapturedAbsoluteDay.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(input.Status);
            foreach (ActorChoiceDisposition disposition in input.Dispositions)
            {
                facts.Append(",disposition=").Append(disposition.TransitionOrdinal.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(disposition.Kind)
                    .Append(':').Append(disposition.AbsoluteDay.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(disposition.ActorTurnRosterOrdinal.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(disposition.DeferralReason)
                    .Append(':').Append(disposition.Failure)
                    .Append(':').Append(disposition.DecisionRecordId)
                    .Append(':').Append(disposition.AttemptOutcome)
                    .Append(':').Append(disposition.ReturnedResultStatus);
            }
        }

        foreach (CityRuntime city in runtime.Cities.OrderBy(value => value.RuntimeId, StringComparer.Ordinal))
        {
            facts.Append("|city=").Append(city.RuntimeId)
                .Append(',').Append(city.DefinitionId)
                .Append(',').Append(city.Location?.RuntimeId)
                .Append(',').Append(city.CurrentPopulation.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(city.ImportantNpcRevision.ToString(CultureInfo.InvariantCulture))
                .Append(",marketRev=").Append(city.Market.Revision.ToString(CultureInfo.InvariantCulture));
            foreach (NpcRuntime importantNpc in city.ImportantNpcs.OrderBy(value => value.RuntimeId, StringComparer.Ordinal))
                facts.Append(",important=").Append(importantNpc.RuntimeId);
            foreach (MarketItemRuntime row in city.Market.Items.OrderBy(
                         value => value.Item?.DefinitionId, StringComparer.Ordinal))
                facts.Append(",stock=").Append(row.Item?.DefinitionId)
                    .Append(':').Append(row.Amount.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(row.DesiredAmount.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(row.CurrentPrice.ToString("R", CultureInfo.InvariantCulture));
        }

        foreach (NpcRuntime npc in runtime.NpcRuntimes.OrderBy(value => value.RuntimeId, StringComparer.Ordinal))
        {
            facts.Append("|npc=").Append(npc.RuntimeId)
                .Append(',').Append(npc.DefinitionId)
                .Append(',').Append(npc.PersonId?.Value)
                .Append(',').Append(npc.ResidenceSettlementRuntimeId)
                .Append(',').Append(npc.CurrentCity?.RuntimeId)
                .Append(',').Append(npc.CurrentLocation?.RuntimeId)
                .Append(',').Append(npc.DestinationCity?.RuntimeId)
                .Append(',').Append(npc.DestinationLocation?.RuntimeId)
                .Append(',').Append(npc.TravelRouteRuntimeId)
                .Append(',').Append(npc.TravelDaysRemaining.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(npc.TravelDaysTotal.ToString(CultureInfo.InvariantCulture))
                .Append(',').Append(npc.TravelOriginDecisionId)
                .Append(',').Append(npc.ActiveTravelPartyId)
                .Append(',').Append(npc.CurrentAction?.DefinitionId)
                .Append(',').Append(npc.LifeState)
                .Append(',').Append(npc.InjurySeverity)
                .Append(',').Append(npc.HiddenDaysRemaining.ToString(CultureInfo.InvariantCulture))
                .Append(",money=").Append(npc.MoneyAccount.Balance.ToString("R", CultureInfo.InvariantCulture))
                .Append(',').Append(npc.MoneyAccount.Revision.ToString(CultureInfo.InvariantCulture))
                .Append(",inventoryRev=").Append(npc.Inventory.Revision.ToString(CultureInfo.InvariantCulture));
            foreach (InventoryItemRuntime row in npc.Inventory.Items.OrderBy(
                         value => value.Item?.DefinitionId, StringComparer.Ordinal))
                facts.Append(",item=").Append(row.Item?.DefinitionId)
                    .Append(':').Append(row.Amount.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(row.AverageUnitCost.ToString("R", CultureInfo.InvariantCulture));
        }

        return facts.ToString();
    }

    private static ExpeditionRuntime CreateUnsupportedDailyV1Expedition()
    {
        const string memberId = "npc-unsupported-expedition-member";
        return new ExpeditionRuntime(
            "expedition-000001",
            "site-unsupported-by-daily-v1",
            "location-unsupported-origin",
            "location-unsupported-target",
            "route-unsupported-by-daily-v1",
            new[] { memberId },
            new[] { memberId },
            Array.Empty<string>(),
            "decision-000001",
            ExpeditionObjectiveRuntime.Explore());
    }

    private static ActionExecutionContext CreateDailyTravelPartyContext(SimulationActiveSession session)
    {
        Assert.That(session?.Runtime, Is.Not.Null);
        Assert.That(session.SpatialNetwork, Is.Not.Null);
        foreach (SpatialRouteRuntime route in session.SpatialNetwork.Routes
                     .OrderBy(value => value.RuntimeId, StringComparer.Ordinal))
        {
            float memberCost = session.Runtime.Configuration.Travel.TravelCostPerDay * route.TravelDays;
            NpcRuntime[] members = session.Runtime.NpcRuntimes
                .Where(npc => npc != null
                    && npc.IsAlive
                    && npc.CurrentLocation == route.Origin
                    && !npc.IsTraveling
                    && string.IsNullOrWhiteSpace(npc.ActiveTravelPartyId)
                    && npc.MoneyAccount != null
                    && npc.MoneyAccount.Balance >= memberCost)
                .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal)
                .Take(2)
                .ToArray();
            if (members.Length != 2) continue;

            return new ActionExecutionContext(
                "p12g-active-travel-party",
                members.Select(member => new ActionExecutionParticipant(
                    member.RuntimeId, ActionExecutionParticipantRole.Performer)),
                route.Destination.RuntimeId,
                route.RuntimeId);
        }

        Assert.Fail("The authored Daily-v1 profile must expose a direct route with two eligible NPCs.");
        return null;
    }

    private static string CaptureContinuationRootFacts(
        SimulationActiveSession session,
        bool includeWorldIdentity = true)
    {
        Assert.That(session, Is.Not.Null);
        Assert.That(session.Composition, Is.Not.Null);
        Assert.That(session.RandomSource, Is.TypeOf<DeterministicRandomSource>());
        RuntimeIdAllocatorSnapshot allocator = session.Composition.RuntimeIdAllocator.CaptureSnapshot();
        SimulationRecordSequenceSnapshot sequence = session.Composition.RecordSequence.CaptureSnapshot();
        DeterministicRandomRootSnapshot random = ((DeterministicRandomSource)session.RandomSource).CaptureSnapshot();
        StringBuilder facts = new StringBuilder();
        if (includeWorldIdentity)
            facts.Append("world=").Append(session.Composition.WorldId.Value).Append('|');
        facts.Append("profile=").Append(session.Composition.ProfileContractIdentity)
            .Append('|').Append(session.Composition.ProfileFingerprint)
            .Append("|sequence=").Append(sequence.SchemaId).Append(':')
            .Append(sequence.SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append(':')
            .Append(sequence.NextSequence.ToString(CultureInfo.InvariantCulture))
            .Append("|random=").Append(random.SchemaId).Append(':')
            .Append(random.SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append(':')
            .Append(random.ProviderId).Append(':')
            .Append(random.ProviderVersion.ToString(CultureInfo.InvariantCulture)).Append(':')
            .Append(random.AlgorithmId).Append(':')
            .Append(random.AlgorithmVersion.ToString(CultureInfo.InvariantCulture)).Append(':')
            .Append(random.Seed.ToString(CultureInfo.InvariantCulture));
        foreach (RuntimeIdAllocatorCounterSnapshot counter in allocator.Counters.OrderBy(
                     value => value.FamilyId, StringComparer.Ordinal))
        {
            facts.Append("|allocator=").Append(counter.FamilyId).Append(':')
                .Append(counter.NextSequence.ToString(CultureInfo.InvariantCulture));
        }
        return facts.ToString();
    }

    private static DailyCaptureEligibilityToken GetCompletedDailyToken(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        return token;
    }

    /// <summary>
    /// Captures every typed C-F owner snapshot used by the accepted Daily-v1
    /// package graph. The structural formatter below is test diagnostics only;
    /// it is not an envelope encoding or production persistence contract.
    /// </summary>
    private static string CaptureCompleteDailyV1OwnerProjection(
        SimulationActiveSession session,
        DailyCaptureEligibilityToken token)
    {
        Assert.That(session, Is.Not.Null);
        Assert.That(token, Is.Not.Null);
        SimulationRuntime runtime = session.Runtime;
        SimulationBootstrapComposition composition = session.Composition;
        IReadOnlyList<OwnerSectionCensusSnapshot> vector = token.OwnerSections;
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True);
        SortedDictionary<string, object> snapshots = new SortedDictionary<string, object>(StringComparer.Ordinal);

        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            composition.Manifest,
            out P12CP9GenesisManifestSnapshot manifestSnapshot,
            out string manifestDiagnostic), Is.True, manifestDiagnostic);
        snapshots.Add("C/P9Manifest", manifestSnapshot);
        Assert.That(P12CSpatialAuthoritySnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            composition.Manifest.SelectedP9ContractIdentity,
            composition.Manifest.SelectedP9SchemaVersion,
            composition.SpatialAuthority,
            out P12CSpatialAuthoritySnapshot spatialAuthoritySnapshot,
            out P12CSpatialAuthoritySnapshotFailure spatialFailure), Is.True,
            spatialFailure?.Message);
        snapshots.Add("C/P8SpatialAuthority", spatialAuthoritySnapshot);
        snapshots.Add("C/RuntimeIdentityValues", session.IdentityRegistry.CaptureRegisteredRuntimeIdentityValues());
        snapshots.Add("C/RuntimeIdAllocator", composition.RuntimeIdAllocator.CaptureSnapshot());
        snapshots.Add("C/RecordSequence", composition.RecordSequence.CaptureSnapshot());
        Assert.That(session.RandomSource, Is.TypeOf<DeterministicRandomSource>());
        snapshots.Add("C/RandomRoot", ((DeterministicRandomSource)session.RandomSource).CaptureSnapshot());

        object sharedCaptureStamp = new object();
        for (int index = 0; index < runtime.Cities.Count; index++)
        {
            CityRuntime city = runtime.Cities[index];
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(
                city, token, sharedCaptureStamp, vector,
                out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture,
                out P12DCityRootOwnerSnapshotFailure cityFailure), Is.True,
                cityFailure.ToString());
            snapshots.Add("D/City/" + city.RuntimeId, cityCapture.Snapshot);
        }
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            runtime, token, sharedCaptureStamp, vector,
            out P12DNpcDProjection npcDProjection,
            out P12DNpcFProjection npcFProjection,
            out P12DNpcRootOwnerSnapshotFailure npcFailure), Is.True,
            npcFailure.ToString());
        snapshots.Add("D/NpcRoots", npcDProjection.Rows);
        snapshots.Add("D/NpcFProjection", npcFProjection.Rows);
        snapshots.Add("D/Persons", runtime.PersonStore.CaptureOwnerSnapshot());
        snapshots.Add("D/Genealogy", runtime.GenealogyStoreForWorldBoundary.CaptureOwnerSnapshot());
        snapshots.Add("D/SpatialNetwork", session.SpatialNetwork.CaptureOwnerSnapshot());
        Assert.That(composition.ExplorableSites.TryCaptureOwnerSnapshot(
            out ExplorableSiteOwnerSnapshot siteSnapshot, out var siteFailure), Is.True, siteFailure?.Message);
        snapshots.Add("D/EmptyExplorableSites", siteSnapshot);

        Assert.That(P12EInstitutionOfficeOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EInstitutionOfficeOwnerSnapshot institutionOfficeSnapshot,
            out P12EInstitutionOfficeSnapshotFailure institutionFailure), Is.True,
            institutionFailure?.Message);
        snapshots.Add("E/InstitutionOffice", institutionOfficeSnapshot);
        Assert.That(PropertyEstateOwnerSnapshot.TryCapture(
            runtime.PropertyOwnershipStore, runtime.EstateStore, token, vector,
            out PropertyEstateOwnerSnapshot propertyEstateSnapshot,
            out PropertyEstateOwnerSnapshotFailure propertyEstateFailure), Is.True,
            propertyEstateFailure?.Message);
        snapshots.Add("E/PropertyEstate", propertyEstateSnapshot);
        Assert.That(P12EFactionOwnerSnapshot.TryCapture(
            runtime, GetRequiredOwner<FactionStore>(vector, FactionStoreCensusProvider.FactionsSectionId),
            token, vector,
            out P12EFactionOwnerSnapshot factionSnapshot,
            out P12EFactionSnapshotFailure factionFailure), Is.True,
            factionFailure?.Message);
        snapshots.Add("E/Factions", factionSnapshot);
        Assert.That(P12EPoliticalClaimOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EPoliticalClaimOwnerSnapshot claimSnapshot,
            out P12EPoliticalClaimSnapshotFailure claimFailure), Is.True,
            claimFailure?.Message);
        snapshots.Add("E/PoliticalClaims", claimSnapshot);
        Assert.That(P12EPoliticalSupportOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EPoliticalSupportOwnerSnapshot supportSnapshot,
            out P12EPoliticalSupportSnapshotFailure supportFailure), Is.True,
            supportFailure?.Message);
        snapshots.Add("E/PoliticalSupport", supportSnapshot);
        Assert.That(P12EPoliticalDecisionOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EPoliticalDecisionOwnerSnapshot decisionSnapshot,
            out P12EPoliticalDecisionSnapshotFailure decisionFailure), Is.True,
            decisionFailure?.Message);
        snapshots.Add("E/PoliticalDecisions", decisionSnapshot);
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            runtime.ArmedForceStore,
            runtime.ContingentManpowerStateStore,
            runtime.ArmedForceSpatialStateStore,
            token, vector,
            out P12EMilitaryOwnerSnapshot militarySnapshot,
            out P12EMilitaryOwnerSnapshotFailure militaryFailure), Is.True,
            militaryFailure?.Message);
        snapshots.Add("E/Military", militarySnapshot);
        Assert.That(PersistentConflictOwnerSnapshot.TryCapture(
            runtime.ConflictStore, token, vector,
            out PersistentConflictOwnerSnapshot conflictSnapshot,
            out PersistentConflictOwnerSnapshotFailure conflictFailure), Is.True,
            conflictFailure?.Message);
        snapshots.Add("E/Conflicts", conflictSnapshot);
        Assert.That(PersistentWarOwnerSnapshot.TryCapture(
            runtime.WarStore, token, vector,
            out PersistentWarOwnerSnapshot warSnapshot,
            out PersistentWarOwnerSnapshotFailure warFailure), Is.True,
            warFailure?.Message);
        snapshots.Add("E/Wars", warSnapshot);
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(
            runtime.BattleStore, token, vector,
            out PersistentBattleOwnerSnapshot battleSnapshot,
            out PersistentBattleOwnerSnapshotFailure battleFailure), Is.True,
            battleFailure?.Message);
        snapshots.Add("E/Battles", battleSnapshot);
        Assert.That(P12EJusticeRecordsOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12EJusticeRecordsOwnerSnapshot justiceSnapshot,
            out P12EJusticeSnapshotFailure justiceFailure), Is.True,
            justiceFailure?.Message);
        snapshots.Add("E/Justice", justiceSnapshot);
        Assert.That(P12ECrimeSocialAppraisalOwnerSnapshot.TryCapture(
            runtime, token, vector,
            out P12ECrimeSocialAppraisalOwnerSnapshot crimeSocialSnapshot,
            out P12ECrimeSocialAppraisalSnapshotFailure crimeSocialFailure), Is.True,
            crimeSocialFailure?.Message);
        snapshots.Add("E/CrimeSocial", crimeSocialSnapshot);

        Assert.That(P12FPoliticalKnowledgeOwnerSnapshot.TryCapture(
            runtime.PoliticalKnowledgeStoreForWorldBoundary, token, vector,
            out P12FPoliticalKnowledgeOwnerSnapshot knowledgeSnapshot,
            out string knowledgeFailure), Is.True, knowledgeFailure);
        snapshots.Add("F/PoliticalKnowledge", knowledgeSnapshot.CopyDetachedRuntimes());
        Assert.That(P12FScheduledDirectiveOwnerSnapshot.TryCapture(
            composition.ScheduledDirectives, token, vector,
            out P12FScheduledDirectiveOwnerSnapshot directiveSnapshot,
            out string directiveFailure), Is.True, directiveFailure);
        snapshots.Add("F/ScheduledDirectives", directiveSnapshot);
        Assert.That(P12FActorChoiceSnapshot.TryCapture(
            runtime.ActorChoiceStore, token, vector,
            out P12FActorChoiceSnapshot actorChoiceSnapshot,
            out P12FActorChoiceSnapshotFailure actorChoiceFailure), Is.True, actorChoiceFailure.ToString());
        snapshots.Add("F/ActorChoice", actorChoiceSnapshot);
        Assert.That(P12FTravelPartyOwnerSnapshot.TryCapture(
            composition.TravelParties, token, vector,
            out P12FTravelPartyOwnerSnapshot travelPartySnapshot,
            out P12FCommitmentSnapshotFailure travelPartyFailure), Is.True, travelPartyFailure.ToString());
        snapshots.Add("F/TravelParties", travelPartySnapshot);
        Assert.That(P12FExpeditionOwnerSnapshot.TryCapture(
            composition.Expeditions, token, vector,
            out P12FExpeditionOwnerSnapshot expeditionSnapshot,
            out P12FCommitmentSnapshotFailure expeditionFailure), Is.True, expeditionFailure.ToString());
        snapshots.Add("F/Expeditions", expeditionSnapshot);

        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(token, out _), Is.True,
            "Capturing the diagnostic projection must leave the exact completed boundary current.");
        StringBuilder result = new StringBuilder();
        foreach (KeyValuePair<string, object> entry in snapshots)
        {
            AppendSnapshotToken(result, entry.Key);
            AppendCanonicalSnapshotValue(result, entry.Value, 0);
        }
        return result.ToString();
    }

    private static T GetRequiredOwner<T>(
        IReadOnlyList<OwnerSectionCensusSnapshot> vector,
        string sectionId) where T : class
    {
        OwnerSectionCensusSnapshot section = vector.SingleOrDefault(value =>
            string.Equals(value.SectionId, sectionId, StringComparison.Ordinal));
        Assert.That(section, Is.Not.Null, sectionId);
        Assert.That(section.Role, Is.EqualTo(OwnerSectionRole.Required), sectionId);
        T owner = section.OwnerInstanceIdentity as T;
        Assert.That(owner, Is.Not.Null, sectionId);
        return owner;
    }

    private static void AppendCanonicalSnapshotValue(StringBuilder output, object value, int depth)
    {
        if (depth > 64) throw new InvalidOperationException(
            "Typed snapshot projection exceeded its acyclic depth bound at "
            + (value?.GetType().FullName ?? "null") + ".");
        if (value == null)
        {
            AppendSnapshotToken(output, "null");
            return;
        }

        Type type = value.GetType();
        AppendSnapshotToken(output, type.FullName ?? type.Name);
        if (type == typeof(object))
        {
            // Some detached snapshots retain an opaque source-owner marker;
            // owner identity and alias shape are compared separately from the
            // value projection in AssertOwnerVectorFactsMatch.
            AppendSnapshotToken(output, "opaque-owner-marker");
            return;
        }
        if (value is string text)
        {
            AppendSnapshotToken(output, text);
            return;
        }
        if (value is Type reflectedType)
        {
            AppendSnapshotToken(output, reflectedType.AssemblyQualifiedName ?? reflectedType.FullName ?? reflectedType.Name);
            return;
        }
        if (type.IsEnum)
        {
            AppendSnapshotToken(output, Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
            return;
        }
        if (value is bool boolean)
        {
            AppendSnapshotToken(output, boolean ? "1" : "0");
            return;
        }
        if (value is float single)
        {
            AppendSnapshotToken(output, BitConverter.ToInt32(BitConverter.GetBytes(single), 0).ToString("X8", CultureInfo.InvariantCulture));
            return;
        }
        if (value is double doubleValue)
        {
            AppendSnapshotToken(output, BitConverter.DoubleToInt64Bits(doubleValue).ToString("X16", CultureInfo.InvariantCulture));
            return;
        }
        if (value is decimal decimalValue)
        {
            foreach (int part in decimal.GetBits(decimalValue))
                AppendSnapshotToken(output, part.ToString("X8", CultureInfo.InvariantCulture));
            return;
        }
        if (value is DateTime dateTime)
        {
            AppendSnapshotToken(output, dateTime.ToBinary().ToString(CultureInfo.InvariantCulture));
            return;
        }
        if (value is TimeSpan timeSpan)
        {
            AppendSnapshotToken(output, timeSpan.Ticks.ToString(CultureInfo.InvariantCulture));
            return;
        }
        if (value is Guid guid)
        {
            AppendSnapshotToken(output, guid.ToString("N"));
            return;
        }
        if (type.IsPrimitive || value is IFormattable)
        {
            AppendSnapshotToken(output, Convert.ToString(value, CultureInfo.InvariantCulture));
            return;
        }
        if (value is IDictionary dictionary)
        {
            List<string> entries = new List<string>();
            foreach (DictionaryEntry entry in dictionary)
            {
                StringBuilder item = new StringBuilder();
                AppendCanonicalSnapshotValue(item, entry.Key, depth + 1);
                AppendCanonicalSnapshotValue(item, entry.Value, depth + 1);
                entries.Add(item.ToString());
            }
            entries.Sort(StringComparer.Ordinal);
            foreach (string entry in entries) AppendSnapshotToken(output, entry);
            return;
        }
        if (value is IEnumerable enumerable)
        {
            List<string> items = new List<string>();
            foreach (object item in enumerable)
            {
                StringBuilder itemOutput = new StringBuilder();
                AppendCanonicalSnapshotValue(itemOutput, item, depth + 1);
                items.Add(itemOutput.ToString());
            }
            if (type.GetInterfaces().Any(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(ISet<>)))
                items.Sort(StringComparer.Ordinal);
            foreach (string item in items) AppendSnapshotToken(output, item);
            return;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo[] properties = type.GetProperties(flags)
            .Where(property => property.GetIndexParameters().Length == 0
                && property.GetGetMethod(true) != null
                && !IsSnapshotContextMember(property.Name)
                && !string.Equals(property.Name, "WorldId", StringComparison.Ordinal))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();
        if (properties.Length > 0)
        {
            foreach (PropertyInfo property in properties)
            {
                AppendSnapshotToken(output, property.Name);
                AppendCanonicalSnapshotValue(output, property.GetValue(value, null), depth + 1);
            }
            return;
        }

        FieldInfo[] fields = type.GetFields(flags)
            .Where(field => !field.IsStatic && !field.IsNotSerialized
                && !IsSnapshotContextMember(field.Name)
                && !string.Equals(field.Name, "WorldId", StringComparison.Ordinal))
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
        if (fields.Length == 0)
            throw new InvalidOperationException("No stable value members found for snapshot type " + type.FullName + ".");
        foreach (FieldInfo field in fields)
        {
            AppendSnapshotToken(output, field.Name);
            AppendCanonicalSnapshotValue(output, field.GetValue(value), depth + 1);
        }
    }

    private static bool IsSnapshotContextMember(string name) =>
        string.Equals(name, "token", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "vector", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "ownerSections", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "stagingAttempt", StringComparison.OrdinalIgnoreCase);

    private static void AppendSnapshotToken(StringBuilder output, string value)
    {
        value = value ?? string.Empty;
        output.Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':').Append(value).Append(';');
    }

    private static void ConfigureUnscopedBootstrap(TesteSimulacao bootstrap, SimulationConfigData config)
    {
        WritePrivateField(bootstrap, "simulationConfig", config);
        WritePrivateField(bootstrap, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.None);
        WritePrivateField<SimulationRuntimeAdmissionContext>(bootstrap, "runtimeAdmissionContext", null);
    }

    private static void ConfigureSelectedBootstrap(TesteSimulacao bootstrap, SimulationConfigData config)
    {
        Thread ownerThread = Thread.CurrentThread;
        WritePrivateField(bootstrap, "simulationConfig", config);
        WritePrivateField(bootstrap, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        WritePrivateField(bootstrap, "bootstrapStartThread", ownerThread);
        WritePrivateField(bootstrap, "bootstrapStartThreadId", ownerThread.ManagedThreadId);
        WritePrivateField(bootstrap, "runtimeAdmissionContext",
            new SimulationRuntimeAdmissionContext(
                SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                ownerThread,
                ownerThread.ManagedThreadId));
        WritePrivateField(bootstrap, "bootstrapStartAttempted", true);
    }

    private static void InvokeInitializeSimulation(TesteSimulacao bootstrap, Action<string> stageCompleted)
    {
        MethodInfo method = typeof(TesteSimulacao).GetMethod(
            "InitializeSimulation",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(bootstrap, new object[] { stageCompleted });
    }

    private static SimulationRuntime CreatePublishedDailyCaptureRuntime(
        SimulationTime time,
        NpcRuntime[] npcRuntimes = null,
        NpcDecisionSystem npcDecisionSystem = null,
        IReadOnlyList<NpcActionData> configuredActions = null,
        WorldId worldId = null)
    {
        SimulationRuntime runtime = CreateUnpublishedDailyCaptureRuntime(time, worldId, npcRuntimes,
            npcDecisionSystem, configuredActions);
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True,
            "The test runtime must complete the selected-profile publication boundary before issuing a daily token.");
        return runtime;
    }

    private static SimulationRuntime CreateUnpublishedDailyCaptureRuntime(
        SimulationTime time,
        WorldId worldId = null,
        NpcRuntime[] npcRuntimes = null,
        NpcDecisionSystem npcDecisionSystem = null,
        IReadOnlyList<NpcActionData> configuredActions = null)
    {
        return new SimulationRuntime(
            time ?? new SimulationTime(),
            null,
            npcRuntimes,
            economyEnabled: false,
            configuredActions: configuredActions,
            npcDecisionSystem: npcDecisionSystem,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: worldId ?? new WorldId(Guid.NewGuid()));
    }

    private static void AssertRestoredAdmissionWasNotPublished(SimulationRuntime runtime)
    {
        Assert.That(ReadPrivateField<bool>(runtime, "factualReadWorldPublished"), Is.False);
        Assert.That(ReadPrivateField<long>(runtime, "completedDailyCoreSequence"), Is.Zero);
        Assert.That(ReadPrivateField<DailyCaptureEligibilityToken>(runtime, "currentDailyCaptureToken"), Is.Null);
    }

    private static T ReadPrivateField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    private static void WritePrivateField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private sealed class AdmissionProbeActionProvider : INpcActionProvider
    {
        private readonly NpcActionData action;
        private readonly Action observeAdmission;

        public AdmissionProbeActionProvider(NpcActionData action, Action observeAdmission)
        {
            this.action = action;
            this.observeAdmission = observeAdmission;
        }

        public bool HandlesAction(NpcActionData candidate) => ReferenceEquals(candidate, action);

        public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData candidate, ref float utility)
        {
            observeAdmission?.Invoke();
            return new NpcActionRuntime(candidate);
        }

        public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
        {
            return NpcActionResult.Succeeded();
        }
    }
}
