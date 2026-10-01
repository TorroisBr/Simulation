using System;
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
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

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
    public void P12NpcTrade_PostCommitCensusFailurePreservesCommittedDomainResult()
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

        Assert.That(result.Success, Is.True);
        Assert.That(buyer.Money, Is.EqualTo(80f));
        Assert.That(seller.Money, Is.EqualTo(40f));
        Assert.That(buyer.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(seller.Inventory.GetAmount(item), Is.EqualTo(1));
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

    private static void BindP12CensusRuntime(EconomyTransactionService service, SimulationRuntime runtime)
    {
        typeof(EconomyTransactionService)
            .GetMethod("BindP12CensusRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(service, new object[] { runtime });
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
    public void BootstrapScopesValidationThroughPublicationAndRevokesFailedPublication()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
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
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
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
