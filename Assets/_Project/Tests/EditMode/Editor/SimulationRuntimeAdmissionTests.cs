using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SimulationRuntimeAdmissionTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

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
            Assert.That(ReadPrivateField<SimulationBootstrapComposition>(bootstrap, "publishedComposition"), Is.Null);
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
            Assert.That(ReadPrivateField<SimulationBootstrapComposition>(bootstrap, "publishedComposition"), Is.Null);
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
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
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
        IReadOnlyList<NpcActionData> configuredActions = null)
    {
        SimulationRuntime runtime = new SimulationRuntime(
            time ?? new SimulationTime(),
            null,
            npcRuntimes,
            economyEnabled: false,
            configuredActions: configuredActions,
            npcDecisionSystem: npcDecisionSystem,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True,
            "The test runtime must complete the selected-profile publication boundary before issuing a daily token.");
        return runtime;
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
