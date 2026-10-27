using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public void SelectedMerchantProjectionMismatchFaultsBeforeProviderExecution()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        NpcActionData sellAction = config.Actions.Single(value =>
            value != null && value.actionType == NpcActionType.SellGoods);

        GameObject hostObject = new GameObject("P12 selected merchant projection mismatch");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        ConfigureSelectedBootstrap(host, config);
        InvokeInitializeSimulation(host, null);

        SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
        Assert.That(session, Is.Not.Null);
        SimulationTime simulationTime = ReadPrivateField<SimulationTime>(session.Runtime, "simulationTime");
        MerchantSystem merchant = new MerchantSystem(
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(), null, simulationTime, null);
        FieldInfo merchantField = typeof(SimulationRuntime).GetField(
            "merchantSystem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(merchantField, Is.Not.Null);
        merchantField.SetValue(session.Runtime, merchant);
        NpcRuntime seller = null;
        NpcActionRuntime selectedAction = null;
        foreach (NpcRuntime candidate in session.Runtime.NpcRuntimes)
        {
            if (candidate == null || !candidate.HasJobType(NpcJobType.Merchant)
                || candidate.CurrentCity == null) continue;
            ItemData item = candidate.CurrentCity.Market?.Items
                .Select(row => row?.Item).FirstOrDefault(value => value != null);
            if (item == null
                || !candidate.TryGetSelectedDailyV1ActionNode(
                    sellAction, out DailyV1ActionDefinition actionNode)
                || !candidate.TryGetSelectedDailyV1Item(
                    item, out DailyV1ItemDefinition itemNode)
                || !DailyV1NpcActionPayloadOwner.TryCreate(
                    candidate.SelectedDailyV1DefinitionGraph,
                    actionNode,
                    null,
                    candidate.CurrentCity.RuntimeId,
                    itemNode,
                    1,
                    1f,
                    1f,
                    NpcTravelReason.None,
                    0f,
                    "projection-mismatch-test",
                    null,
                    null,
                    null,
                    out DailyV1NpcActionPayloadOwner payload)
                || !NpcActionRuntime.TryCreateFromOwnerSnapshot(
                    sellAction,
                    null,
                    candidate.CurrentCity,
                    item,
                    1,
                    1f,
                    1f,
                    NpcTravelReason.None,
                    0f,
                    "projection-mismatch-test",
                    null,
                    null,
                    null,
                    out NpcActionRuntime proposal)
                || !proposal.TryUseSelectedPayload(
                    payload, candidate.SelectedDailyV1DefinitionGraph)) continue;
            seller = candidate;
            selectedAction = proposal;
            break;
        }

        Assert.That(seller, Is.Not.Null,
            "The accepted Daily-v1 fixture must expose a supported Merchant SellGoods proposal.");
        Assert.That(selectedAction, Is.Not.Null);
        Assert.That(selectedAction.SelectedOwner, Is.Not.Null,
            "The supported proposal must carry its exact Core action owner before dispatch.");
        Assert.That(session.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure before),
            Is.True, before.ToString());

        float balanceBefore = seller.Money;
        long accountRevisionBefore = seller.MoneyAccount.Revision;
        long inventoryRevisionBefore = seller.Inventory.Revision;
        long marketRevisionBefore = seller.CurrentCity.Market.SelectedDailyV1Owner.Revision;
        int inventoryAmountBefore = seller.Inventory.GetAmount(selectedAction.TargetItem);
        int marketAmountBefore = seller.CurrentCity.Market.GetAmount(selectedAction.TargetItem);

        FieldInfo selectedOwnerField = typeof(NpcActionRuntime).GetField(
            "selectedOwner", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(selectedOwnerField, Is.Not.Null);
        selectedOwnerField.SetValue(selectedAction, null);

        MethodInfo execute = typeof(SimulationRuntime).GetMethod("TryExecuteAction",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(execute, Is.Not.Null);
        TargetInvocationException failure = Assert.Throws<TargetInvocationException>(() => execute.Invoke(
            session.Runtime, new object[] { seller, selectedAction, sellAction }));
        Assert.That(failure.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(failure.InnerException.Message,
            Does.Contain("exact Core action projection"));

        Assert.That(seller.Money, Is.EqualTo(balanceBefore));
        Assert.That(seller.MoneyAccount.Revision, Is.EqualTo(accountRevisionBefore));
        Assert.That(seller.Inventory.Revision, Is.EqualTo(inventoryRevisionBefore));
        Assert.That(seller.Inventory.GetAmount(selectedAction.TargetItem), Is.EqualTo(inventoryAmountBefore));
        Assert.That(seller.CurrentCity.Market.SelectedDailyV1Owner.Revision, Is.EqualTo(marketRevisionBefore));
        Assert.That(seller.CurrentCity.Market.GetAmount(selectedAction.TargetItem), Is.EqualTo(marketAmountBefore));
        Assert.That(session.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure after), Is.False);
        Assert.That(after, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void ProjectionIndependentNormalActionDoesNotLogStaleUnityTarget()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);

        GameObject hostObject = new GameObject("P12 projection-independent Normal action logging");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        ConfigureSelectedBootstrap(host, config);
        InvokeInitializeSimulation(host, null);

        SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
        Assert.That(session, Is.Not.Null);
        SimulationRuntime runtime = session.Runtime;
        NpcRuntime actor = null;
        NpcRuntime target = null;
        DailyV1ActionDefinition normalAction = null;
        DailyV1NpcActionPayloadOwner payload = null;

        foreach (NpcRuntime candidate in runtime.NpcRuntimes)
        {
            if (candidate?.SelectedRuntimeOwners == null
                || candidate.SelectedRuntimeOwnerGraph == null)
                continue;

            foreach (NpcActionData actionAsset in config.Actions.Where(value =>
                         value != null && value.actionType == NpcActionType.Normal))
            {
                if (!candidate.TryGetSelectedDailyV1ActionNode(
                        actionAsset, out DailyV1ActionDefinition actionNode)
                    || !DailyV1NpcActionPayloadOwner.TryCreate(
                        candidate.SelectedDailyV1DefinitionGraph,
                        actionNode,
                        null,
                        null,
                        null,
                        0,
                        0f,
                        1f,
                        NpcTravelReason.None,
                        0f,
                        "stale-projection-normal-action",
                        null,
                        null,
                        null,
                        out DailyV1NpcActionPayloadOwner candidatePayload))
                    continue;

                actor = candidate;
                normalAction = actionNode;
                payload = candidatePayload;
                break;
            }

            if (actor != null) break;
        }

        Assert.That(actor, Is.Not.Null,
            "The admitted Daily-v1 fixture must provide a selected actor for the Normal action regression.");
        Assert.That(normalAction, Is.Not.Null);
        Assert.That(payload, Is.Not.Null);
        target = runtime.NpcRuntimes.FirstOrDefault(candidate =>
            candidate != null && !ReferenceEquals(candidate, actor));
        Assert.That(target, Is.Not.Null,
            "The stale Unity projection must reference a distinct target runtime.");

        Assert.That(actor.SelectedRuntimeOwnerGraph.TryCommitCurrentActionSlot(
            actor.SelectedRuntimeOwners, normalAction, payload, () => true), Is.True);
        Assert.That(actor.SelectedRuntimeOwners.ActionSlot.CurrentAction,
            Is.SameAs(normalAction));
        Assert.That(DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalAction(
            actor.SelectedRuntimeOwnerGraph, actor.SelectedRuntimeOwners, normalAction), Is.True);

        NpcActionData staleActionAsset = config.Actions.First(value =>
            value != null && value.actionType != NpcActionType.Normal);
        NpcActionRuntime staleProjection = new NpcActionRuntime(staleActionAsset, target);
        WritePrivateField(actor, "currentActionRuntime", staleProjection);
        WritePrivateField(actor, "currentAction", staleActionAsset);

        List<string> output = new List<string>();
        SimulationLogger logger = new SimulationLogger(
            new SimulationLogSettings(), Path.GetTempPath(), (_, message) => output.Add(message));
        WritePrivateField(runtime, "logger", logger);

        MethodInfo execute = typeof(SimulationRuntime).GetMethod(
            "TryExecuteSelectedCurrentAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(execute, Is.Not.Null);
        NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[] { actor });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True,
            "The action must execute from the exact Core slot despite the stale Unity projection.");
        Assert.That(actor.SelectedRuntimeOwners.ActionSlot.CurrentAction,
            Is.SameAs(normalAction));
        Assert.That(output.Any(message => message.Contains("escolheu")
            && message.Contains(target.NpcName)), Is.False,
            "A stale Unity action projection must not contribute target text to the Core action log.");
    }

    [Test]
    public void ProjectionIndependentNormalActionCommitsActorStatusFromCoreSlot()
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(sourceConfig, Is.Not.Null);

        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        NpcActionData sourceNormal = sourceConfig.Actions.First(value =>
            value != null && value.actionType == NpcActionType.Normal);
        NpcActionData normalWithActorEffect = UnityEngine.Object.Instantiate(sourceNormal);
        normalWithActorEffect.id = "core-normal-actor-status-projection-independent";
        normalWithActorEffect.actionName = "Core status action";
        normalWithActorEffect.normalActionLogText = "completed a Core status action.";
        normalWithActorEffect.statusToAdd.Clear();
        normalWithActorEffect.statusToRemove.Clear();
        normalWithActorEffect.targetStatusToAdd.Clear();
        normalWithActorEffect.targetStatusToRemove.Clear();
        NpcStatusData effectStatus = SimulationTestFactory.CreateStatus(
            "core-normal-actor-status-projection-independent-effect");
        normalWithActorEffect.statusToAdd.Add(effectStatus);
        config.actions = new List<NpcActionData>(sourceConfig.Actions)
        { normalWithActorEffect };
        config.statuses = new List<NpcStatusData>(sourceConfig.Statuses)
        { effectStatus };

        GameObject hostObject = new GameObject("Core-slot Normal status action without Unity projection");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        try
        {
            ConfigureSelectedBootstrap(host, config);
            InvokeInitializeSimulation(host, null);

            SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
            Assert.That(session, Is.Not.Null);
            SimulationRuntime runtime = session.Runtime;
            NpcRuntime actor = runtime.NpcRuntimes.FirstOrDefault(value =>
                value?.SelectedRuntimeOwners?.Status != null
                && value.TryGetSelectedDailyV1ActionNode(
                    normalWithActorEffect, out DailyV1ActionDefinition actionNode)
                && actionNode != null);
            Assert.That(actor, Is.Not.Null,
                "The selected graph must bind the added Normal action and its actor status effect.");
            Assert.That(actor.TryGetSelectedDailyV1ActionNode(
                normalWithActorEffect, out DailyV1ActionDefinition selectedAction), Is.True);
            Assert.That(actor.SelectedDailyV1DefinitionGraph.Statuses.TryGetValue(
                effectStatus.DefinitionId, out DailyV1StatusDefinition selectedEffect), Is.True);
            Assert.That(actor.SelectedDailyV1StatusOwner.Statuses.Any(status =>
                ReferenceEquals(status, selectedEffect)), Is.False);
            Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
                actor.SelectedDailyV1DefinitionGraph,
                selectedAction,
                null, null, null, 0, 0f, 1f, NpcTravelReason.None, 0f,
                "normal-actor-status-without-projection",
                null, null, null,
                out DailyV1NpcActionPayloadOwner payload), Is.True);
            Assert.That(actor.SelectedRuntimeOwnerGraph.TryCommitCurrentActionSlot(
                actor.SelectedRuntimeOwners, selectedAction, payload, () => true), Is.True);
            Assert.That(DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalAction(
                actor.SelectedRuntimeOwnerGraph, actor.SelectedRuntimeOwners, selectedAction), Is.True);

            long mutationEpochBefore = runtime.ExecutionBoundaryState
                .MutationEpochState.CurrentEpoch;
            WritePrivateField<NpcActionRuntime>(actor, "currentActionRuntime", null);
            WritePrivateField<NpcActionData>(actor, "currentAction", null);
            Assert.That(actor.CurrentActionRuntime, Is.Null,
                "This path must execute without a Unity NpcActionRuntime projection.");

            MethodInfo execute = typeof(SimulationRuntime).GetMethod(
                "TryExecuteSelectedCurrentAction", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(execute, Is.Not.Null);
            NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[] { actor });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(actor.SelectedDailyV1StatusOwner.Statuses.Any(status =>
                ReferenceEquals(status, selectedEffect)), Is.True,
                "The effect must be committed to the exact Core status owner from the selected Core action slot.");
            Assert.That(actor.SelectedDailyV1DefinitionGraph.Statuses.TryGetValue(
                effectStatus.DefinitionId, out DailyV1StatusDefinition admittedEffect), Is.True);
            Assert.That(actor.SelectedDailyV1StatusOwner.Statuses.Single(value =>
                value.DefinitionId == effectStatus.DefinitionId), Is.SameAs(admittedEffect));
            Assert.That(runtime.ExecutionBoundaryState.MutationEpochState.CurrentEpoch,
                Is.EqualTo(mutationEpochBefore + 1L),
                "The successful Core status-owner write must invalidate the runtime capture epoch.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(normalWithActorEffect);
            UnityEngine.Object.DestroyImmediate(effectStatus);
        }
    }

    [Test]
    public void ProjectionIndependentTargetedNormalActionCommitsExactTargetAndRejectsAliasedProjectionTarget()
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(sourceConfig, Is.Not.Null);

        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        NpcActionData sourceNormal = sourceConfig.Actions.First(value =>
            value != null && value.actionType == NpcActionType.Normal);
        NpcActionData normalWithTargetEffect = UnityEngine.Object.Instantiate(sourceNormal);
        normalWithTargetEffect.id = "core-normal-target-status-projection-independent";
        normalWithTargetEffect.actionName = "Core targeted status action";
        normalWithTargetEffect.normalActionLogText = "completed a Core targeted status action.";
        normalWithTargetEffect.statusToAdd.Clear();
        normalWithTargetEffect.statusToRemove.Clear();
        normalWithTargetEffect.targetStatusToAdd.Clear();
        normalWithTargetEffect.targetStatusToRemove.Clear();
        NpcStatusData effectStatus = SimulationTestFactory.CreateStatus(
            "core-normal-target-status-projection-independent-effect");
        normalWithTargetEffect.targetStatusToAdd.Add(effectStatus);
        config.actions = new List<NpcActionData>(sourceConfig.Actions)
        { normalWithTargetEffect };
        config.statuses = new List<NpcStatusData>(sourceConfig.Statuses)
        { effectStatus };

        GameObject hostObject = new GameObject("Core-slot targeted Normal status action without Unity projection");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        try
        {
            ConfigureSelectedBootstrap(host, config);
            InvokeInitializeSimulation(host, null);

            SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
            Assert.That(session, Is.Not.Null);
            SimulationRuntime runtime = session.Runtime;
            NpcRuntime[] selectedActors = runtime.NpcRuntimes.Where(value =>
                value?.SelectedRuntimeOwners?.Status != null
                && value.SelectedRuntimeOwnerGraph != null).ToArray();
            Assert.That(selectedActors.Length, Is.GreaterThanOrEqualTo(2),
                "The Daily-v1 proving profile must provide an exact actor and target owner.");
            NpcRuntime actor = selectedActors[0];
            NpcRuntime target = selectedActors.First(value => !ReferenceEquals(value, actor));
            Assert.That(actor.TryGetSelectedDailyV1ActionNode(
                normalWithTargetEffect, out DailyV1ActionDefinition selectedAction), Is.True);
            Assert.That(actor.SelectedDailyV1DefinitionGraph.Statuses.TryGetValue(
                effectStatus.DefinitionId, out DailyV1StatusDefinition selectedEffect), Is.True);
            Assert.That(target.SelectedDailyV1StatusOwner.Statuses.Any(status =>
                ReferenceEquals(status, selectedEffect)), Is.False);

            Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
                actor.SelectedDailyV1DefinitionGraph,
                selectedAction,
                target.RuntimeId, null, null, 0, 0f, 1f, NpcTravelReason.None, 0f,
                "normal-target-status-without-projection",
                null, null, null,
                out DailyV1NpcActionPayloadOwner payload), Is.True);
            Assert.That(actor.SelectedRuntimeOwnerGraph.TryCommitCurrentActionSlot(
                actor.SelectedRuntimeOwners, selectedAction, payload, () => true), Is.True);
            Assert.That(DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalActionForExactTarget(
                actor.SelectedRuntimeOwnerGraph, actor.SelectedRuntimeOwners, selectedAction,
                target.SelectedRuntimeOwners), Is.True);

            long mutationEpochBefore = runtime.ExecutionBoundaryState
                .MutationEpochState.CurrentEpoch;

            // A projection can still match the payload's serialized RuntimeId
            // while pointing at a different runtime object. The log and any
            // compatibility projection must use the graph's exact runtime.
            NpcRuntime staleTargetAlias = new NpcRuntime(
                target.RuntimeId, SimulationTestFactory.CreateNpc(
                    "core-normal-target-status-stale-projection-alias"));
            NpcActionRuntime staleProjection = new NpcActionRuntime(
                normalWithTargetEffect, target);
            Assert.That(staleProjection.TryUseSelectedPayload(
                payload, actor.SelectedDailyV1DefinitionGraph), Is.True);
            WritePrivateField(staleProjection, "targetNpc", staleTargetAlias);
            WritePrivateField(actor, "currentActionRuntime", staleProjection);
            WritePrivateField(actor, "currentAction", normalWithTargetEffect);

            List<string> output = new List<string>();
            SimulationLogger selectedLogger = new SimulationLogger(
                new SimulationLogSettings(), Path.GetTempPath(),
                (_, message) => output.Add(message));
            WritePrivateField(runtime, "logger", selectedLogger);

            MethodInfo execute = typeof(SimulationRuntime).GetMethod(
                "TryExecuteSelectedCurrentAction", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(execute, Is.Not.Null);
            NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[] { actor });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(target.SelectedDailyV1StatusOwner.Statuses.Any(status =>
                ReferenceEquals(status, selectedEffect)), Is.True,
                "The target effect must be committed to the exact Core owner named by the slot payload.");
            Assert.That(actor.SelectedDailyV1StatusOwner.Statuses.Any(status =>
                ReferenceEquals(status, selectedEffect)), Is.False,
                "The target effect must not be redirected to the actor owner.");
            Assert.That(target.SelectedDailyV1StatusOwner.Statuses.Single(value =>
                value.DefinitionId == effectStatus.DefinitionId), Is.SameAs(selectedEffect));
            Assert.That(runtime.ExecutionBoundaryState.MutationEpochState.CurrentEpoch,
                Is.EqualTo(mutationEpochBefore + 1L),
                "The successful Core target-status write must invalidate the runtime capture epoch once.");
            Assert.That(output.Any(message => message.Contains("escolheu")
                && message.Contains(staleTargetAlias.NpcName)), Is.False,
                "The target log must not name a runtime object that only aliases the Core target RuntimeId.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(normalWithTargetEffect);
            UnityEngine.Object.DestroyImmediate(effectStatus);
        }
    }

    [Test]
    public void PartialPlannedNpcTradeDispatchFaultsP12Admission()
    {
        EconomyTransactionService transactionService = new EconomyTransactionService();
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            economyTransactionService: transactionService,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            worldId: new WorldId(Guid.NewGuid()));
        transactionService.BindP12CensusRuntime(runtime);
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);

        DailyV1SelectedNpcTradeCommitResult partialTrade =
            new DailyV1SelectedNpcTradeCommitResult(false,
                DailyV1SelectedNpcTradeFailure.PartialCommit, partialCommit: true,
                buyerAccountMutationCount: 1, sellerAccountMutationCount: 1,
                sellerInventoryMutationCount: 1);
        DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult sale =
            new DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult(
                false, false, 0, 0f, false, false, partialTrade, null);
        DailyV1SelectedActionExecutionStage.SelectedPlannedNpcSaleActionResult plannedSale =
            new DailyV1SelectedActionExecutionStage.SelectedPlannedNpcSaleActionResult(
                false, true, sale);
        DailyV1SelectedActionDispatchResult dispatch = new DailyV1SelectedActionDispatchResult(
            DailyV1SelectedActionDispatchKind.PlannedNpcSale,
            actionRollSucceeded: true,
            succeeded: false,
            plannedNpcSale: plannedSale);

        MethodInfo complete = typeof(SimulationRuntime).GetMethod(
            "CompleteSelectedActionDispatch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(complete, Is.Not.Null);
        NpcActionResult result = (NpcActionResult)complete.Invoke(runtime,
            new object[] { null, null, null, dispatch });

        Assert.That(result.Success, Is.False);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [TestCase(NpcActionType.Hide, false)]
    [TestCase(NpcActionType.EscapePrison, true)]
    public void SelectedCrimeDispatchCompletesAndPreservesEscapeSummaryLog(
        NpcActionType actionType,
        bool expectsEscapeSummary)
    {
        List<string> output = new List<string>();
        SimulationLogger selectedLogger = new SimulationLogger(
            new SimulationLogSettings(),
            Path.GetTempPath(),
            (_, message) => output.Add(message));
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            logger: selectedLogger);
        NpcRuntime npc = new NpcRuntime(
            "selected-crime-dispatch",
            SimulationTestFactory.CreateNpc("selected-crime-dispatch"));
        NpcActionData action = SimulationTestFactory.CreateAction(
            "selected-crime-dispatch-action", actionType, NpcActionCategory.Crime);
        DailyV1SelectedActionDispatchResult dispatch = new DailyV1SelectedActionDispatchResult(
            DailyV1SelectedActionDispatchKind.Crime,
            actionRollSucceeded: true,
            succeeded: true,
            crime: new DailyV1SelectedCrimeActionExecutionStage.Result(
                actionRollSucceeded: true, succeeded: true));

        MethodInfo complete = typeof(SimulationRuntime).GetMethod(
            "CompleteSelectedActionDispatch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(complete, Is.Not.Null);
        NpcActionResult result = (NpcActionResult)complete.Invoke(runtime,
            new object[] { npc, null, action, dispatch });

        Assert.That(result.Success, Is.True);
        if (expectsEscapeSummary)
        {
            Assert.That(output, Is.EqualTo(new[]
            {
                "selected-crime-dispatch display esta livre e nao possui mandado ativo."
            }));
        }
        else
        {
            Assert.That(output, Is.Empty,
                "Hide publishes its own transition output and must not receive the Escape summary.");
        }
    }

    [Test]
    public void DailyProfileScopesAdvancesAndRoutesOwnedClockThroughRuntime()
    {
        SimulationTime time = new SimulationTime();
        CityRuntime city = SimulationTestFactory.CreateCity("daily-profile-clock-city", "daily-profile-clock-location");
        NpcRuntime npc = new NpcRuntime("npc-admission", SimulationTestFactory.CreateNpc("admission"), city, 0f);
        NpcActionData[] actions = CreatePositiveNormalProbeActions("admission-probe", npc);
        SimulationRuntime runtime = null;
        RecordFixture records = null;
        int operationObservations = 0;
        bool reentrantClockWasRejected = false;
        SimulationTimeAdvanceFailure reentrantClockFailure = SimulationTimeAdvanceFailure.None;
        AdmissionProbeRandomSource randomSource = new AdmissionProbeRandomSource(npc.RuntimeId, () =>
        {
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
            Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));
            reentrantClockWasRejected = !records.Time.TryAdvanceDay(out reentrantClockFailure);
            operationObservations++;
        });
        DailyV1SelectedProfileFixture profile = DailyV1SelectedProfileFixture.Create(
            time,
            "daily-profile-clock-owner-profile",
            cities: new[] { city },
            npcs: new[] { npc },
            configuredActions: actions,
            decisionSystemFactory: (selectedRecords, selectedTravel, merchant) =>
                new NpcDecisionSystem(new List<INpcActionProvider>(), randomSource));
        records = profile.Records;
        runtime = profile.Runtime;
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True,
            "The selected-profile clock test must publish its complete world before advancing it.");

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure before), Is.True,
            "The published selected-profile fixture must begin with a valid census: " + before);
        Assert.That(records.Time.TryAdvanceDay(out SimulationTimeAdvanceFailure directClockFailure), Is.True,
            "The runtime-owned clock must dispatch the selected daily core: " + directClockFailure);
        Assert.That(runtime.CurrentDay, Is.EqualTo(1L), "the runtime-owned clock dispatches a complete day advance");
        Assert.That(operationObservations, Is.EqualTo(1));
        Assert.That(reentrantClockWasRejected, Is.True);
        Assert.That(reentrantClockFailure, Is.EqualTo(SimulationTimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterDirectClock), Is.True,
            "The runtime must remain assessable after direct clock dispatch: " + afterDirectClock);

        Assert.That(runtime.TryAdvanceDays(0, out int zeroDaysAdvanced, out SimulationRuntimeAdvanceFailure zeroFailure), Is.True);
        Assert.That(zeroDaysAdvanced, Is.Zero);
        Assert.That(zeroFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(operationObservations, Is.EqualTo(1), "the zero-day no-op does not enter an operation scope");
        Assert.That(runtime.TryAdvanceDays(-1, out _, out SimulationRuntimeAdvanceFailure invalidFailure), Is.False);
        Assert.That(invalidFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.InvalidDayCount));

        Assert.That(runtime.TryAdvanceDays(2, out int daysAdvanced, out SimulationRuntimeAdvanceFailure batchFailure), Is.True,
            "The runtime must retain clock ownership for a batch after direct clock dispatch: " + batchFailure);
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
        NpcRuntime npc = new NpcRuntime("restore-vector-probe", SimulationTestFactory.CreateNpc("restore-vector-probe"));
        NpcActionData[] actions = CreatePositiveNormalProbeActions("restore-vector-probe-action", npc);

        SimulationRuntime runtime = null;
        bool capturedDuringAdvance = true;
        ContinuationCensusFailure advanceCaptureFailure = ContinuationCensusFailure.None;
        AdmissionProbeRandomSource randomSource = new AdmissionProbeRandomSource(npc.RuntimeId, () =>
        {
            capturedDuringAdvance = runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                out _, out _, out advanceCaptureFailure);
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider>(), randomSource),
            actions);

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
        NpcRuntime npc = new NpcRuntime("npc-daily-token-probe", SimulationTestFactory.CreateNpc("daily-token-probe"));
        NpcActionData[] actions = CreatePositiveNormalProbeActions("daily-token-probe-action", npc);

        SimulationRuntime runtime = null;
        int callbackCount = 0;
        AdmissionProbeRandomSource randomSource = new AdmissionProbeRandomSource(npc.RuntimeId, () =>
        {
            Assert.That(runtime.TryGetCompletedDailyCaptureToken(
                out _, out DailyCaptureEligibilityFailure failure), Is.False);
            Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress));
            callbackCount++;
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider>(), randomSource),
            actions);

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
        NpcRuntime npc = new NpcRuntime("npc-daily-token-throw", SimulationTestFactory.CreateNpc("daily-token-throw"));
        NpcActionData[] actions = CreatePositiveNormalProbeActions("daily-token-throw-action", npc);

        SimulationRuntime runtime = null;
        int callbackCount = 0;
        AdmissionProbeRandomSource randomSource = new AdmissionProbeRandomSource(npc.RuntimeId, () =>
        {
            callbackCount++;
            if (callbackCount == 2) throw new InvalidOperationException("injected second-day callback failure");
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider>(), randomSource),
            actions);

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
        object overflowBoundary = ReadPrivateField<object>(overflowRuntime, "dailyExecutionBoundary");
        WritePrivateField(overflowBoundary, "completedCoreSequence", long.MaxValue - 2L);
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
        NpcRuntime npc = new NpcRuntime("npc-daily-token-final-census", SimulationTestFactory.CreateNpc("final-census"));
        NpcActionData[] actions = CreatePositiveNormalProbeActions("daily-token-final-census-action", npc);

        SimulationRuntime runtime = null;
        ContinuationCensusProtocol protocol = null;
        int callbackCount = 0;
        bool removedSection = false;
        AdmissionProbeRandomSource randomSource = new AdmissionProbeRandomSource(npc.RuntimeId, () =>
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
            new NpcDecisionSystem(new List<INpcActionProvider>(), randomSource),
            actions);
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
        NpcRuntime npc = new NpcRuntime("npc-daily-token-scope-disposal", SimulationTestFactory.CreateNpc("scope-disposal"));
        NpcActionData[] actions = CreatePositiveNormalProbeActions("daily-token-scope-disposal-action", npc);

        SimulationRuntime runtime = null;
        ContinuationCensusProtocol protocol = null;
        DailyV1ExecutionBoundaryState executionBoundaryState = null;
        int callbackCount = 0;
        bool endedSharedProtocolOperation = false;
        AdmissionProbeRandomSource randomSource = new AdmissionProbeRandomSource(npc.RuntimeId, () =>
        {
            callbackCount++;
            MethodInfo endProtocolOperation = typeof(DailyV1ExecutionBoundaryState).GetMethod(
                "TryEndRuntimeProtocolOperation", BindingFlags.Instance | BindingFlags.NonPublic);
            if (endProtocolOperation != null && executionBoundaryState != null)
                endedSharedProtocolOperation = (bool)endProtocolOperation.Invoke(executionBoundaryState, null);
        });
        runtime = CreatePublishedDailyCaptureRuntime(
            new SimulationTime(),
            new[] { npc },
            new NpcDecisionSystem(new List<INpcActionProvider>(), randomSource),
            actions);
        protocol = ReadPrivateField<ContinuationCensusProtocol>(runtime, "npcRosterCensusProtocol");
        executionBoundaryState = ReadPrivateField<DailyV1ExecutionBoundaryState>(protocol, "sharedExecutionState");

        bool advanced = runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure);

        Assert.That(callbackCount, Is.EqualTo(1));
        Assert.That(endedSharedProtocolOperation, Is.True,
            "The test must remove the shared protocol scope before its matching scope object is disposed.");
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
        IReadOnlyCollection<string> registeredOperations = protocol.RegisteredOperationContractIds;
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
    public void SelectedSoloTravelProjectsEachArrivalBeforeP12CensusInvalidation()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("daily-selected-travel-p12-projection-order");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        SimulationRuntime runtime = simulation.Runtime;
        Assert.That(runtime, Is.Not.Null);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialCensus), Is.True,
            initialCensus.ToString());

        IGrouping<CityRuntime, NpcRuntime> sourceGroup = runtime.NpcRuntimes
            .Where(npc => npc != null && npc.IsAlive && !npc.IsTraveling && npc.CurrentCity != null)
            .GroupBy(npc => npc.CurrentCity)
            .Where(group => group.Count() >= 2)
            .OrderBy(group => group.Key.RuntimeId, StringComparer.Ordinal)
            .FirstOrDefault();
        Assert.That(sourceGroup, Is.Not.Null,
            "The arrival regression needs two selected NPC owners sharing one origin City.");
        NpcRuntime[] travelers = sourceGroup.OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal)
            .Take(2).ToArray();
        Assert.That(travelers, Has.Length.EqualTo(2),
            "The accepted Daily-v1 profile supplies two independent selected NPC owners for the arrival witness.");

        CityRuntime commonDestination = runtime.Cities.FirstOrDefault(city =>
            city != null && city.Location != null
            && !ReferenceEquals(city, sourceGroup.Key));
        Assert.That(commonDestination, Is.Not.Null,
            "The arrival regression uses one destination City shared by both selected travel owners.");
        Dictionary<NpcRuntime, CityRuntime> destinations = new Dictionary<NpcRuntime, CityRuntime>();
        foreach (NpcRuntime npc in travelers)
        {
            CityRuntime destination = commonDestination;
            Assert.That(npc.StartTravel(destination, 1, "p12-selected-arrival-" + npc.RuntimeId), Is.True,
                "Starting travel must use the selected Core owner and admitted P12 boundary.");
            destinations.Add(npc, destination);
        }
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure departureCensus), Is.True,
            departureCensus.ToString());

        TravelSystem travelSystem = ReadPrivateField<TravelSystem>(runtime, "travelSystem");
        MethodInfo advanceSelectedSoloTravels = typeof(TravelSystem).GetMethod(
            "TryAdvanceSelectedSoloTravels", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(travelSystem, Is.Not.Null);
        Assert.That(advanceSelectedSoloTravels, Is.Not.Null);

        object[] arguments = { runtime.SelectedRuntimeOwnerGraph, runtime.NpcRuntimes.ToList(), null };
        Assert.That((bool)advanceSelectedSoloTravels.Invoke(travelSystem, arguments), Is.True,
            "The first boundary clears the established TravelStartedToday marker.");
        Assert.That(arguments[2] as IReadOnlyList<NpcRuntime>, Is.Empty);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure clearedCensus), Is.True,
            clearedCensus.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochBeforeArrivals, out ContinuationCensusFailure epochBeforeFailure), Is.True,
            epochBeforeFailure.ToString());

        Dictionary<NpcRuntime, OwnerSectionCensusWitness> spatialWitnessesBefore =
            new Dictionary<NpcRuntime, OwnerSectionCensusWitness>();
        Dictionary<NpcRuntime, bool> destinationKnownBefore = new Dictionary<NpcRuntime, bool>();
        foreach (NpcRuntime npc in travelers)
        {
            IOwnerSectionCensusProvider locationsProvider = runtime.SpatialKnowledgeCensusProviders.Single(provider =>
                string.Equals(provider.GetCurrentCensus().SectionId,
                    SpatialKnowledgeCensusProvider.LocationsSectionPrefix + npc.RuntimeId,
                    StringComparison.Ordinal));
            OwnerSectionCensusWitness locations = locationsProvider.GetCurrentCensus();
            Assert.That(locations.OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
            spatialWitnessesBefore.Add(npc, locations);
            destinationKnownBefore.Add(npc,
                npc.SpatialKnowledge.KnowsLocation(commonDestination.Location.RuntimeId));
        }

        RuntimeIdAllocator runtimeIdAllocator = ReadPrivateField<RuntimeIdAllocator>(runtime, "runtimeIdAllocator");
        SimulationRecordSequence recordSequence = ReadPrivateField<SimulationRecordSequence>(runtime, "simulationRecordSequence");
        Assert.That(travelSystem, Is.Not.Null);
        Assert.That(runtimeIdAllocator, Is.Not.Null);
        Assert.That(recordSequence, Is.Not.Null);
        DomainEventRecorder eventRecorder = ReadPrivateField<DomainEventRecorder>(travelSystem, "domainEventRecorder");
        Assert.That(eventRecorder, Is.Not.Null);
        DomainEventStore domainEventStore = ReadPrivateField<DomainEventStore>(eventRecorder, "eventStore");
        Assert.That(domainEventStore, Is.Not.Null);
        IOwnerSectionCensusProvider eventCounterProvider =
            RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(runtimeIdAllocator);
        SimulationRecordSequenceCensusProvider sequenceProvider =
            new SimulationRecordSequenceCensusProvider(recordSequence);
        long eventCounterBefore = eventCounterProvider.GetCurrentCensus().Revision;
        long recordSequenceBefore = sequenceProvider.GetCurrentCensus().Revision;
        int eventCountBefore = domainEventStore.Events.Count;
        Dictionary<CityRuntime, long> cityPresenceRevisions = runtime.Cities.ToDictionary(
            city => city, city => city.SelectedRuntimeOwners.NpcPresence.Revision);

        arguments = new object[] { runtime.SelectedRuntimeOwnerGraph, runtime.NpcRuntimes.ToList(), null };
        Assert.That((bool)advanceSelectedSoloTravels.Invoke(travelSystem, arguments), Is.True,
            "Each arrival must project reciprocal Core/Unity presence before its P12 census notification and before the next NPC commits.");
        IReadOnlyList<NpcRuntime> arrivals = arguments[2] as IReadOnlyList<NpcRuntime>;
        Assert.That(arrivals, Has.Count.EqualTo(2));
        foreach (NpcRuntime npc in travelers)
        {
            Assert.That(npc.CurrentCity, Is.SameAs(destinations[npc]));
            Assert.That(destinations[npc].ContainsImportantNpc(npc), Is.True);
            Assert.That(npc.HasConsistentSelectedTravelState, Is.True);
        }
        foreach (CityRuntime city in runtime.Cities)
        {
            long expectedArrivals = ReferenceEquals(city, commonDestination) ? travelers.Length : 0L;
            Assert.That(city.SelectedRuntimeOwners.NpcPresence.Revision,
                Is.EqualTo(cityPresenceRevisions[city] + expectedArrivals),
                "Only the shared destination presence owner advances, exactly once for each committed arrival.");
        }

        long discoveryMutations = 0L;
        foreach (NpcRuntime npc in travelers)
        {
            string locationsSectionId = SpatialKnowledgeCensusProvider.LocationsSectionPrefix + npc.RuntimeId;
            string routesSectionId = SpatialKnowledgeCensusProvider.RoutesSectionPrefix + npc.RuntimeId;
            IOwnerSectionCensusProvider locationsProvider = runtime.SpatialKnowledgeCensusProviders.Single(provider =>
                string.Equals(provider.GetCurrentCensus().SectionId, locationsSectionId, StringComparison.Ordinal));
            IOwnerSectionCensusProvider routesProvider = runtime.SpatialKnowledgeCensusProviders.Single(provider =>
                string.Equals(provider.GetCurrentCensus().SectionId, routesSectionId, StringComparison.Ordinal));
            OwnerSectionCensusWitness locationsAfter = locationsProvider.GetCurrentCensus();
            OwnerSectionCensusWitness routesAfter = routesProvider.GetCurrentCensus();
            long expectedDiscoveryRevision = spatialWitnessesBefore[npc].Revision
                + (destinationKnownBefore[npc] ? 0L : 1L);
            Assert.That(locationsAfter.OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
            Assert.That(routesAfter.OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
            Assert.That(locationsAfter.Revision, Is.EqualTo(expectedDiscoveryRevision),
                "The selected arrival discovers only a previously unknown destination on its exact Core owner.");
            Assert.That(routesAfter.Revision, Is.EqualTo(expectedDiscoveryRevision),
                "Location and route sections share the exact SpatialKnowledge owner revision.");
            Assert.That(locationsAfter.Cardinality,
                Is.EqualTo(spatialWitnessesBefore[npc].Cardinality + (destinationKnownBefore[npc] ? 0 : 1)));
            discoveryMutations += destinationKnownBefore[npc] ? 0L : 1L;
        }

        long eventCounterAfter = eventCounterProvider.GetCurrentCensus().Revision;
        long recordSequenceAfter = sequenceProvider.GetCurrentCensus().Revision;
        Assert.That(eventCounterAfter - eventCounterBefore, Is.EqualTo(arrivals.Count),
            "Each arrival event uses the registered Event-ID counter owner once.");
        Assert.That(recordSequenceAfter - recordSequenceBefore, Is.EqualTo(arrivals.Count),
            "Each arrival event uses the registered record-sequence owner once.");
        Assert.That(domainEventStore.Events.Count - eventCountBefore, Is.EqualTo(arrivals.Count));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochAfterArrivals, out ContinuationCensusFailure epochAfterFailure), Is.True,
            epochAfterFailure.ToString());
        Assert.That(epochAfterArrivals, Is.EqualTo(epochBeforeArrivals
            + arrivals.Count // one travel-state + City-presence commit vector per NPC
            + discoveryMutations
            + (eventCounterAfter - eventCounterBefore)
            + (recordSequenceAfter - recordSequenceBefore)),
            "The shared P12 epoch advances once for each committed owner vector, including arrival knowledge and event identities.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure arrivalCensus), Is.True,
            arrivalCensus.ToString(),
            "A fresh census assessment proves every exact per-NPC spatial section baseline accepted the committed owner revision.");
    }

    [Test]
    public void SelectedSoloTravelUsesCoreDestinationPresenceInsteadOfStaleUnityCache()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("daily-selected-travel-destination-preflight");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        SimulationRuntime runtime = simulation.Runtime;
        NpcRuntime traveler = runtime.NpcRuntimes
            .Where(npc => npc != null && npc.IsAlive && !npc.IsTraveling && npc.CurrentCity != null)
            .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal)
            .FirstOrDefault();
        Assert.That(traveler, Is.Not.Null);
        CityRuntime destination = runtime.Cities.First(city =>
            city != null && city.Location != null && !ReferenceEquals(city, traveler.CurrentCity));
        Assert.That(traveler.StartTravel(destination, 2, "p12-destination-preflight"), Is.True);

        TravelSystem travelSystem = ReadPrivateField<TravelSystem>(runtime, "travelSystem");
        MethodInfo advanceSelectedSoloTravels = typeof(TravelSystem).GetMethod(
            "TryAdvanceSelectedSoloTravels", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] arguments = { runtime.SelectedRuntimeOwnerGraph, runtime.NpcRuntimes.ToList(), null };
        Assert.That((bool)advanceSelectedSoloTravels.Invoke(travelSystem, arguments), Is.True,
            "The first boundary clears TravelStartedToday before testing the next travel day.");
        Assert.That(traveler.TravelDaysRemaining, Is.EqualTo(2));

        FieldInfo membershipField = typeof(CityRuntime).GetField("importantNpcs",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(membershipField, Is.Not.Null);
        List<NpcRuntime> destinationProjection = (List<NpcRuntime>)membershipField.GetValue(destination);
        Assert.That(destinationProjection, Is.Not.Null);
        Assert.That(destinationProjection.Contains(traveler), Is.False);
        destinationProjection.Add(traveler);
        Assert.That(ReadPrivateField<object>(destination, "selectedRuntimeOwners"), Is.Not.Null);
        Assert.That(destination.ImportantNpcs.Contains(traveler), Is.False,
            "Selected City presence is projected from its Core owner, not the legacy Unity membership cache.");
        Assert.That(destination.SelectedRuntimeOwners.NpcPresence.Contains(traveler.RuntimeId), Is.False);
        Assert.That(traveler.SelectedRuntimeOwners.Travel.DestinationCityRuntimeId,
            Is.EqualTo(destination.RuntimeId));

        long travelRevision = traveler.SelectedRuntimeOwners.Travel.Revision;
        long destinationPresenceRevision = destination.SelectedRuntimeOwners.NpcPresence.Revision;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long mutationEpoch, out ContinuationCensusFailure epochFailure), Is.True,
            epochFailure.ToString());
        arguments = new object[] { runtime.SelectedRuntimeOwnerGraph, runtime.NpcRuntimes.ToList(), null };
        Assert.That((bool)advanceSelectedSoloTravels.Invoke(travelSystem, arguments), Is.True,
            "The stale Unity cache is not an authoritative travel precondition.");
        Assert.That(arguments[2] as IReadOnlyList<NpcRuntime>, Is.Empty,
            "A non-arrival must not report an arrival even when the ignored cache contains the traveler.");
        Assert.That(traveler.TravelDaysRemaining, Is.EqualTo(1),
            "The Core travel owner advances when the only inconsistency is in the discarded legacy City cache.");
        Assert.That(traveler.SelectedRuntimeOwners.Travel.Revision, Is.EqualTo(travelRevision + 1L));
        Assert.That(destination.SelectedRuntimeOwners.NpcPresence.Revision,
            Is.EqualTo(destinationPresenceRevision));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long mutationEpochAfter, out ContinuationCensusFailure epochFailureAfter), Is.True,
            epochFailureAfter.ToString());
        Assert.That(mutationEpochAfter, Is.EqualTo(mutationEpoch + 1L),
            "The accepted travel-state owner mutation invalidates the P12 census epoch once.");
        Assert.That(traveler.CurrentCity, Is.Null);
        Assert.That(traveler.DestinationCity, Is.SameAs(destination));
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
        DailyV1SelectedProfileFixture profile = DailyV1SelectedProfileFixture.Create(
            time,
            "p12-merchant-daily-profile",
            cities: new[] { city },
            npcs: new[] { merchant },
            configuration: configuration,
            merchantSystem: merchantSystem);
        SimulationRuntime runtime = profile.Runtime;
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
        DailyV1SelectedProfileFixture profile = DailyV1SelectedProfileFixture.Create(
            "p12-daily-market-production-profile",
            cities: new[] { city },
            economyEnabled: true);
        SimulationRuntime runtime = profile.Runtime;
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
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
        DailyV1SelectedProfileFixture profile = DailyV1SelectedProfileFixture.Create(
            "p12-daily-free-consumption-profile",
            cities: new[] { city },
            economyEnabled: true);
        SimulationRuntime runtime = profile.Runtime;
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
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
        DailyV1SelectedProfileFixture profile = DailyV1SelectedProfileFixture.Create(
            "p12-daily-price-refresh-profile",
            cities: new[] { city },
            economyEnabled: true);
        SimulationRuntime runtime = profile.Runtime;
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
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
        object mutationEpochState = typeof(ContinuationCensusProtocol)
            .GetField("mutationEpochState", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(protocol);
        mutationEpochState.GetType().GetField("epoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(mutationEpochState, long.MaxValue);

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
        Assert.That(runtime.ExecutionBoundaryState.ActiveOperationCount, Is.Zero,
            "The outer DailyAdvance scope closes when an admitted day-stage operation throws.");
        Assert.That(runtime.ExecutionBoundaryState.ActiveRuntimeOperationCount, Is.Zero,
            "The inner P12 runtime.advance-day scope also closes on the exception path.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retryFailure), Is.False);
        Assert.That(retryFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(runtime.ExecutionBoundaryState.ActiveOperationCount, Is.Zero);
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
    public void UnityBootstrapDailyRejectsMixedP14CSourcesBeforeIdentityOrOwnerConstruction()
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.MixedSourcesDaily, 7, out _);
        GameObject bootstrapObject = new GameObject("P14-C rejected by P12 Daily profile");
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

            TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
                InvokeInitializeSimulation(bootstrap, null));

            Assert.That(thrown.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(thrown.InnerException.Message, Does.Contain("P14-C"));
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
    public void P14CMixedSourcesRunInTheirStandaloneSingleCityProvingProfile()
    {
        SimulationConfigData config = CreateP14AdmissionConfig(
            LocalMaterialFlowProfile.MixedSourcesDaily, 7, out ItemData item);
        GameObject bootstrapObject = new GameObject("P14-C mixed-source standalone profile");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureUnscopedBootstrap(bootstrap, config);

            InvokeInitializeSimulation(bootstrap, null);

            Assert.That(bootstrap.Bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Runtime.Cities.Count, Is.EqualTo(1));
            CityRuntime city = bootstrap.Runtime.Cities[0];
            Assert.That(city.Market.GetAmount(item), Is.EqualTo(10));
            Assert.That(bootstrap.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
                advanceFailure.ToString());
            Assert.That(city.FiniteProductionSources, Is.Not.Null);
            Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(4));
            Assert.That(city.LastMaterialFlow.SourceOutcomes.Count, Is.EqualTo(2));
            Assert.That(city.LastMaterialFlow.SourceOutcomes[0].ProductionSourceId,
                Is.EqualTo("source.p14.admission.exogenous"));
            Assert.That(city.LastMaterialFlow.SourceOutcomes[0].AppliedQuantity, Is.EqualTo(5));
            Assert.That(city.LastMaterialFlow.SourceOutcomes[1].ProductionSourceId,
                Is.EqualTo("source.p14.admission.finite"));
            Assert.That(city.LastMaterialFlow.SourceOutcomes[1].AppliedQuantity, Is.EqualTo(3));
            Assert.That(city.LastMaterialFlow.OpeningStock, Is.EqualTo(10));
            Assert.That(city.LastMaterialFlow.ActualFreeConsumption, Is.EqualTo(1));
            Assert.That(city.LastMaterialFlow.ClosingStock, Is.EqualTo(17));
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
            SimulationRuntimeAdmissionContext admissionContext =
                ReadPrivateField<SimulationRuntimeAdmissionContext>(bootstrap, "runtimeAdmissionContext");
            Assert.That(admissionContext, Is.Not.Null);
            Assert.That(admissionContext.ExpectedOwnerThread, Is.SameAs(Thread.CurrentThread));
            Assert.That(admissionContext.ExpectedOwnerThreadId, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
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

            SimulationRuntimeAdmissionContext bootstrapAdmissionContext =
                ReadPrivateField<SimulationRuntimeAdmissionContext>(activeOwner, "runtimeAdmissionContext");
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    sourceSession,
                    bootstrapAdmissionContext,
                    out SimulationActiveSession candidateSession,
                    out P12GDailyV1RestoreFailure candidateRestoreFailure,
                    out string candidateRestoreDiagnostic), Is.True,
                candidateRestoreFailure + ": " + candidateRestoreDiagnostic);
            Assert.That(candidateSession, Is.Not.Null);
            Assert.That(candidateSession.Composition.WorldId.Value, Is.EqualTo(sourceToken.WorldId.Value));
            Assert.That(candidateSession.Runtime.CurrentDay, Is.EqualTo(sourceToken.AbsoluteDay));
            Assert.That(candidateSession.AdmissionContext.IsRestoredContinuation, Is.True);
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
                Assert.That(ReadPrivateField<SimulationActiveSession>(activeOwner, "activeSession"),
                    Is.SameAs(sourceSession),
                    "Rejected exchange must leave the active-session reference unchanged.");
            }
            finally
            {
                endOperation.Invoke(activeOwner, new[] { Enum.ToObject(
                    endOperation.GetParameters()[0].ParameterType, 0) });
            }

            Assert.That(sourceSession.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
                "Rejected exchange must preserve the source completed boundary after the operation closes.");

            Assert.That(activeOwner.TryPublishRestoredSession(sourceSession, candidateSession), Is.False,
                "A Core restored replacement cannot publish without its exclusive owner-thread reservation.");

            object application = ReadPrivateField<object>(activeOwner, "dailyV1Application");
            PropertyInfo coreSessionProperty = typeof(SimulationActiveSession).GetProperty(
                "CoreRuntimeSession", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(coreSessionProperty, Is.Not.Null);
            object sourceCoreSession = coreSessionProperty.GetValue(sourceSession);
            MethodInfo beginExclusive = application.GetType().GetMethod(
                "TryBeginExclusiveOperation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(beginExclusive, Is.Not.Null);
            object[] exclusiveArguments = { sourceCoreSession, null };
            Assert.That((bool)beginExclusive.Invoke(application, exclusiveArguments), Is.True,
                "The exact active Core session must be exclusively reserved before replacement.");
            object exclusiveOperation = exclusiveArguments[1];
            Assert.That(exclusiveOperation, Is.Not.Null);
            MethodInfo commitRestored = typeof(TesteSimulacao).GetMethod(
                "TryPublishRestoredSession",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(SimulationActiveSession), typeof(SimulationActiveSession), exclusiveOperation.GetType() },
                null);
            Assert.That(commitRestored, Is.Not.Null);
            using ((IDisposable)exclusiveOperation)
            {
                Assert.That((bool)commitRestored.Invoke(
                    activeOwner, new[] { sourceSession, candidateSession, exclusiveOperation }), Is.True,
                    "An admitted restored candidate may publish under its exact idle owner-thread reservation.");
            }
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
            UnityEngine.Object.DestroyImmediate(activeOwnerObject);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DailyV1RestoreRetainsTerminalActorChoiceAndRejectsDuplicateTrustedCommandBeforeContinuing(
        bool successfulSellGoodsAttempt)
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        string actionDefinitionId = successfulSellGoodsAttempt
            ? config.Actions.Single(action => action != null && action.actionType == NpcActionType.SellGoods).DefinitionId
            : "sell-goods";

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

            CaptureTerminalP11History(
                uninterruptedSession.Runtime, "p12g-terminal-input", actionDefinitionId,
                successfulSellGoodsAttempt);
            CaptureTerminalP11History(
                sourceSession.Runtime, "p12g-terminal-input", actionDefinitionId,
                successfulSellGoodsAttempt);

            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure uninterruptedFirst),
                Is.True, uninterruptedFirst.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFirst),
                Is.True, sourceFirst.ToString());
            Assert.That(sourceSession.Runtime.TryGetCompletedDailyCaptureToken(
                out DailyCaptureEligibilityToken sourceToken,
                out DailyCaptureEligibilityFailure sourceTokenFailure), Is.True, sourceTokenFailure.ToString());

            Assert.That(sourceSession.Composition.Decisions, Is.Not.Null,
                "The omitted NPC decision read model remains composed at the completed boundary.");
            Assert.That(sourceSession.Composition.DomainEventStore, Is.Not.Null,
                "The omitted domain-event read model remains composed at the completed boundary.");
            int sourceOmittedDecisionCount = sourceSession.Composition.Decisions.Decisions.Count;
            Assert.That(sourceOmittedDecisionCount, Is.GreaterThan(0),
                "The source boundary deliberately contains populated NpcDecisionStore rows; their omission is not evidence of emptiness.");

            string sourceRootsBeforeReadModelSeed = CaptureContinuationRootFacts(sourceSession);
            if (sourceSession.Composition.DomainEventStore.Events.Count == 0)
            {
                NpcRuntime readModelActor = sourceSession.Runtime.NpcRuntimes.FirstOrDefault(
                    npc => npc?.CurrentLocation != null);
                Assert.That(readModelActor, Is.Not.Null,
                    "The selected profile supplies an existing actor/location pair for the omitted event row.");
                NpcDecisionRecord decisionReference = sourceSession.Composition.Decisions.Decisions[0];
                // This profile need not emit a travel/arrest/escape event on day one. Seed one
                // concrete read-model row directly so the test covers the permitted populated case.
                // Do not use DomainEventRecorder here: it allocates the authoritative record sequence.
                Assert.That(sourceSession.Composition.DomainEventStore.Record(new NpcArrivedEvent(
                    "p12g-omitted-read-model-arrival-" + sourceToken.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
                    sourceToken.AbsoluteDay,
                    Math.Max(1L, sourceToken.AbsoluteDay),
                    readModelActor.RuntimeId,
                    readModelActor.CurrentLocation.RuntimeId,
                    decisionReference.DecisionId)), Is.True);
            }
            int sourceOmittedEventCount = sourceSession.Composition.DomainEventStore.Events.Count;
            Assert.That(sourceOmittedEventCount, Is.GreaterThan(0),
                "The source boundary deliberately contains populated DomainEventStore rows; their omission is not evidence of emptiness.");
            Assert.That(sourceSession.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
                "Populating a known omitted read model must not stale the completed authoritative boundary.");
            Assert.That(CaptureContinuationRootFacts(sourceSession), Is.EqualTo(sourceRootsBeforeReadModelSeed),
                "The read-model-only row must leave allocator, shared sequence, lineage, and random roots unchanged.");

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

            P12GDailyV1RestoreFailure restoreFailure;
            string restoreDiagnostic;
            SimulationGenesisPipeline.ExecutionProbeForTest genesisExecutionProbe =
                SimulationGenesisPipeline.BeginExecutionProbeForTest();
            SimulationRuntime.DailyGameplayExecutionProbeForTest gameplayExecutionProbe =
                SimulationRuntime.BeginDailyGameplayExecutionProbeForTest();
            try
            {
                Assert.That(restored.TryRestoreDailyContinuation(
                        out restoreFailure,
                        out restoreDiagnostic), Is.True,
                    restoreFailure + ": " + restoreDiagnostic);
            }
            finally
            {
                gameplayExecutionProbe.Dispose();
                genesisExecutionProbe.Dispose();
            }
            Assert.That(genesisExecutionProbe.ExecuteStagesInvocationCount, Is.Zero,
                "Daily-v1 restoration stages the captured P9 manifest and must not enter the P9 genesis pipeline.");
            Assert.That(gameplayExecutionProbe.AdvanceDayAfterClockAdvanceInvocationCount, Is.Zero,
                "Daily-v1 restoration must not enter the normal daily gameplay callback pipeline.");

            SimulationActiveSession targetSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
            Assert.That(targetSession, Is.Not.Null.And.Not.SameAs(sourceSession),
                "The operation must publish one fresh active-session reference.");
            Assert.That(targetSession.Runtime, Is.Not.SameAs(sourceSession.Runtime));
            Assert.That(targetSession.Composition, Is.Not.SameAs(sourceSession.Composition));
            Assert.That(targetSession.IdentityRegistry, Is.Not.SameAs(sourceSession.IdentityRegistry));
            Assert.That(targetSession.SpatialNetwork, Is.Not.SameAs(sourceSession.SpatialNetwork));
            Assert.That(targetSession.Composition.Decisions, Is.Not.SameAs(sourceSession.Composition.Decisions));
            Assert.That(targetSession.Composition.Decisions.Decisions, Is.Empty,
                "The populated source NpcDecisionStore is an omitted noncausal read model and is not hydrated into the candidate.");
            Assert.That(targetSession.Composition.DomainEventStore, Is.Not.SameAs(sourceSession.Composition.DomainEventStore));
            Assert.That(targetSession.Composition.DomainEventStore.Events, Is.Empty,
                "The populated source DomainEventStore is an omitted noncausal read model and is not hydrated into the candidate.");
            Assert.That(sourceSession.Composition.Decisions.Decisions.Count, Is.EqualTo(sourceOmittedDecisionCount));
            Assert.That(sourceSession.Composition.DomainEventStore.Events.Count, Is.EqualTo(sourceOmittedEventCount));
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
            Dictionary<string, object> expectedTargetOwners =
                SimulationBootstrapCompositionTests.BuildSelectedDailyV1RegisteredOwnerIdentityMap(
                    targetSession.Runtime, targetSession.Composition);
            Dictionary<string, OwnerSectionCensusSnapshot> actualTargetRows = restoredToken.OwnerSections
                .ToDictionary(section => section.SectionId, StringComparer.Ordinal);
            Assert.That(expectedTargetOwners.Count, Is.EqualTo(sourceToken.OwnerSections.Count),
                "The target identity map must follow the exact completed-boundary vector, including supported dynamic rows.");
            Assert.That(actualTargetRows.Count, Is.EqualTo(expectedTargetOwners.Count),
                "The restored target vector must contain each expected section exactly once.");
            Assert.That(actualTargetRows.Keys.OrderBy(sectionId => sectionId, StringComparer.Ordinal),
                Is.EqualTo(expectedTargetOwners.Keys.OrderBy(sectionId => sectionId, StringComparer.Ordinal)));
            foreach (KeyValuePair<string, object> expectedTargetOwner in expectedTargetOwners)
            {
                Assert.That(actualTargetRows[expectedTargetOwner.Key].OwnerInstanceIdentity,
                    Is.SameAs(expectedTargetOwner.Value),
                    "The restored row must identify the exact C/D/E/F-produced target authority: "
                    + expectedTargetOwner.Key + ".");
            }

            ActorChoiceInput expectedTerminalChoice = sourceSession.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == "p12g-terminal-input");
            ActorChoiceInput restoredTerminalChoice = targetSession.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == expectedTerminalChoice.WorldCommandId);
            if (successfulSellGoodsAttempt)
            {
                Assert.That(expectedTerminalChoice.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
                Assert.That(expectedTerminalChoice.Dispositions, Has.Count.EqualTo(2));
                Assert.That(expectedTerminalChoice.Dispositions[0].Kind,
                    Is.EqualTo(ActorChoiceDispositionKind.DispatchStarted));
                Assert.That(expectedTerminalChoice.Dispositions[1].Kind,
                    Is.EqualTo(ActorChoiceDispositionKind.AttemptReturned));
                Assert.That(expectedTerminalChoice.Dispositions[1].AttemptOutcome,
                    Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));

                NpcRuntime sourceMerchant = sourceSession.Runtime.NpcRuntimes.Single(
                    npc => npc != null && npc.PersonId == expectedTerminalChoice.PersonId);
                Assert.That(sourceMerchant.NpcData?.DefinitionId, Is.EqualTo("PEDRO"),
                    "The success path must use the authored Daily-v1 merchant rather than introducing a new NPC.");
                string selectedDecisionId = expectedTerminalChoice.Dispositions[0].DecisionRecordId;
                Assert.That(selectedDecisionId, Is.Not.Null.And.Not.Empty,
                    "A successful actor choice must retain its Core decision identity at dispatch.");
                Assert.That(sourceMerchant.CurrentActionRuntime, Is.Not.Null,
                    "The selected Unity action projection remains installed after its successful attempt.");
                Assert.That(sourceMerchant.CurrentActionRuntime.OriginDecisionId,
                    Is.EqualTo(selectedDecisionId),
                    "The Unity projection must be refreshed with the exact Core-allocated decision identity before slot admission.");
                Assert.That(sourceMerchant.CurrentActionRuntime.MatchesSelectedOwner(), Is.True,
                    "The installed Unity action projection must still match the exact selected Core payload.");
                NpcDecisionRecord selectedDecision = sourceSession.Composition.Decisions.Decisions.Single(
                    decision => decision != null && decision.DecisionId == selectedDecisionId);
                Assert.That(selectedDecision.Origin, Is.EqualTo(NpcDecisionOrigin.ActorChoice));
                Assert.That(selectedDecision.ActionDefinitionId,
                    Is.EqualTo(expectedTerminalChoice.ActionDefinitionId));
            }
            else
            {
                Assert.That(expectedTerminalChoice.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
                Assert.That(expectedTerminalChoice.Dispositions, Has.Count.EqualTo(1));
                Assert.That(expectedTerminalChoice.Dispositions[0].Failure,
                    Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
            }
            Assert.That(restoredTerminalChoice.InputId, Is.EqualTo(expectedTerminalChoice.InputId));
            Assert.That(restoredTerminalChoice.Status, Is.EqualTo(expectedTerminalChoice.Status));

            long targetChoiceRevisionBeforeDuplicate = targetSession.Runtime.ActorChoiceStore.CensusRevision;
            long targetChoiceSequenceBeforeDuplicate = targetSession.Runtime.ActorChoiceStore.NextInputSequence;
            int targetChoiceCountBeforeDuplicate = targetSession.Runtime.ActorChoiceStore.Count;
            string targetFactsBeforeDuplicate = CaptureSelectedDailyFacts(targetSession.Runtime);
            string targetGraphBeforeDuplicate = CaptureCompleteDailyV1OwnerProjection(targetSession, restoredToken);
            string targetRootsBeforeDuplicate = CaptureContinuationRootFacts(targetSession);
            ActorActionChoiceWorldCommandHandler duplicateHandler =
                new ActorActionChoiceWorldCommandHandler(targetSession.Runtime);
            WorldCommandHandlerResult duplicateResult = duplicateHandler.Execute(
                new WorldCommand(
                    WorldCommandKind.ActorActionChoice,
                    WorldCommandOrigin.LocalPlayer,
                    WorldCommandAuthorityMode.Request,
                    new ActorActionChoiceWorldCommandPayload(
                        expectedTerminalChoice.PersonId,
                        expectedTerminalChoice.ActionDefinitionId)),
                new WorldCommandExecutionContext(expectedTerminalChoice.WorldCommandId));
            Assert.That(duplicateResult.Success, Is.False);
            Assert.That(duplicateResult.Diagnostic,
                Does.Contain(ActorChoiceStoreFailureCode.DuplicateWorldCommandId.ToString()));
            Assert.That(targetSession.Runtime.ActorChoiceStore.Count, Is.EqualTo(targetChoiceCountBeforeDuplicate));
            Assert.That(targetSession.Runtime.ActorChoiceStore.CensusRevision,
                Is.EqualTo(targetChoiceRevisionBeforeDuplicate));
            Assert.That(targetSession.Runtime.ActorChoiceStore.NextInputSequence,
                Is.EqualTo(targetChoiceSequenceBeforeDuplicate));
            Assert.That(CaptureSelectedDailyFacts(targetSession.Runtime), Is.EqualTo(targetFactsBeforeDuplicate),
                "Retrying a terminal command must not create another ActorChoice input or alter terminal history.");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(targetSession, restoredToken),
                Is.EqualTo(targetGraphBeforeDuplicate),
                "A duplicate request must not repeat domain effects or change any included owner value.");
            Assert.That(CaptureContinuationRootFacts(targetSession), Is.EqualTo(targetRootsBeforeDuplicate),
                "A duplicate request must not allocate another identity or shared record sequence.");
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

    [TestCase(ActorChoiceInputStatus.Pending)]
    [TestCase(ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt)]
    public void DailyV1RestoreRejectsInFlightActorChoiceSnapshotInsideFHydratorAtomically(
        ActorChoiceInputStatus rejectedStatus)
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        string actionDefinitionId = config.Actions.Single(
            action => action != null && action.actionType == NpcActionType.SellGoods).DefinitionId;
        string commandId = "p12g-inflight-actor-choice-" + rejectedStatus;
        GameObject sourceObject = new GameObject("P12-G in-flight ActorChoice failure source " + rejectedStatus);
        GameObject controlObject = new GameObject("P12-G in-flight ActorChoice failure control " + rejectedStatus);
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
            CaptureTerminalP11History(original.Runtime, commandId, actionDefinitionId, bindDailyMerchant: true);
            CaptureTerminalP11History(controlSession.Runtime, commandId, actionDefinitionId, bindDailyMerchant: true);
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());

            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken controlToken = GetCompletedDailyToken(controlSession.Runtime);
            ActorChoiceInput terminalInput = original.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == commandId);
            Assert.That(terminalInput.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
            Assert.That(terminalInput.Dispositions, Has.Count.EqualTo(2));
            Assert.That(terminalInput.Dispositions[0].Kind, Is.EqualTo(ActorChoiceDispositionKind.DispatchStarted));
            Assert.That(terminalInput.Dispositions[1].Kind, Is.EqualTo(ActorChoiceDispositionKind.AttemptReturned));
            Assert.That(terminalInput.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlToken)),
                "The source and uninterrupted control must begin from equivalent successful SellGoods boundaries.");

            string sourceFactsBeforeRestore = CaptureSelectedDailyFacts(original.Runtime);
            string sourceRootsBeforeRestore = CaptureContinuationRootFacts(original);
            string sourceGraphBeforeRestore = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            long sourceDayBeforeRestore = sourceToken.AbsoluteDay;
            long sourceSequenceBeforeRestore = sourceToken.CompletedCoreSequence;
            bool fOwnerSnapshotsCaptured = false;
            bool actorChoicesStaged = false;
            bool fStaged = false;
            bool beforePublication = false;
            bool overrideApplied = false;
            ActorChoiceInput transformedInFlightInput = null;
            P12GDailyV1RestoreFailure restoreFailure;
            string restoreDiagnostic;

            SimulationRuntime.DailyGameplayExecutionProbeForTest gameplayExecutionProbe =
                SimulationRuntime.BeginDailyGameplayExecutionProbeForTest();
            try
            {
                Assert.That(source.TryRestoreDailyContinuationWithActorChoiceSnapshotForTest(
                    snapshot =>
                    {
                        Assert.That(fOwnerSnapshotsCaptured, Is.True,
                            "The supported source snapshot must be captured and validated before the detached override.");
                        Assert.That(snapshot, Is.Not.Null);
                        Assert.That(snapshot.SchemaVersion, Is.EqualTo(P12FActorChoiceSnapshot.CurrentSchemaVersion));
                        Assert.That(snapshot.CensusRevision, Is.EqualTo(original.Runtime.ActorChoiceStore.CensusRevision));
                        Assert.That(snapshot.NextInputSequence, Is.EqualTo(original.Runtime.ActorChoiceStore.NextInputSequence));
                        ActorChoiceInput captured = snapshot.Inputs.Single(input => input.WorldCommandId == commandId);
                        Assert.That(captured.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
                        Assert.That(captured.Dispositions, Has.Count.EqualTo(2));

                        IReadOnlyList<ActorChoiceDisposition> inFlightDispositions;
                        if (rejectedStatus == ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt)
                        {
                            Assert.That(captured.Dispositions[0].Kind,
                                Is.EqualTo(ActorChoiceDispositionKind.DispatchStarted));
                            inFlightDispositions = new[] { captured.Dispositions[0] };
                        }
                        else
                        {
                            Assert.That(rejectedStatus, Is.EqualTo(ActorChoiceInputStatus.Pending));
                            inFlightDispositions = Array.Empty<ActorChoiceDisposition>();
                        }

                        transformedInFlightInput = new ActorChoiceInput(
                            captured.InputId,
                            captured.WorldCommandId,
                            captured.InputSequence,
                            captured.PersonId,
                            captured.ActionDefinitionId,
                            captured.Origin,
                            captured.Authority,
                            captured.CapturedAbsoluteDay,
                            rejectedStatus,
                            inFlightDispositions,
                            null,
                            Array.Empty<ActorChoiceTemporalDisposition>());
                        Assert.That(transformedInFlightInput.InputId, Is.EqualTo(captured.InputId));
                        Assert.That(transformedInFlightInput.WorldCommandId, Is.EqualTo(captured.WorldCommandId));
                        Assert.That(transformedInFlightInput.InputSequence, Is.EqualTo(captured.InputSequence));
                        Assert.That(transformedInFlightInput.PersonId, Is.EqualTo(captured.PersonId));
                        Assert.That(transformedInFlightInput.Status, Is.EqualTo(rejectedStatus));
                        Assert.That(transformedInFlightInput.Dispositions, Has.Count.EqualTo(inFlightDispositions.Count));
                        overrideApplied = true;

                        return snapshot.WithInputsForTest(snapshot.Inputs.Select(input =>
                            input.WorldCommandId == commandId ? transformedInFlightInput : input));
                    },
                    stage =>
                    {
                        if (stage == P12GDailyV1RestoreStage.FOwnerSnapshotsCaptured)
                            fOwnerSnapshotsCaptured = true;
                        else if (stage == P12GDailyV1RestoreStage.FActorChoicesStaged)
                            actorChoicesStaged = true;
                        else if (stage == P12GDailyV1RestoreStage.FStaged)
                            fStaged = true;
                        else if (stage == P12GDailyV1RestoreStage.BeforePublication)
                            beforePublication = true;
                    },
                    out restoreFailure,
                    out restoreDiagnostic), Is.False);
            }
            finally
            {
                gameplayExecutionProbe.Dispose();
            }
            Assert.That(gameplayExecutionProbe.AdvanceDayAfterClockAdvanceInvocationCount, Is.Zero,
                "A rejected restore must not enter the normal daily gameplay callback pipeline.");

            Assert.That(overrideApplied, Is.True);
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(restoreDiagnostic, Does.Contain("P12-F could not stage the retained owner capture"));
            Assert.That(actorChoicesStaged, Is.False,
                rejectedStatus + " history must fail inside TryCreateP12FStaged before the ActorChoice stage event.");
            Assert.That(fStaged, Is.False);
            Assert.That(beforePublication, Is.False);
            Assert.That(transformedInFlightInput, Is.Not.Null);
            Assert.That(transformedInFlightInput.Status, Is.EqualTo(rejectedStatus));
            if (rejectedStatus == ActorChoiceInputStatus.ConsumedAwaitingTerminalAttempt)
            {
                Assert.That(transformedInFlightInput.Dispositions, Has.Count.EqualTo(1));
                Assert.That(transformedInFlightInput.Dispositions[0].Kind,
                    Is.EqualTo(ActorChoiceDispositionKind.DispatchStarted));
            }
            else
            {
                Assert.That(transformedInFlightInput.Dispositions, Is.Empty);
            }
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(source.Bootstrap, Is.SameAs(original.Composition));
            Assert.That(source.Runtime, Is.SameAs(original.Runtime));
            Assert.That(original.Runtime.CurrentDay, Is.EqualTo(sourceDayBeforeRestore));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
                "Rejecting the detached " + rejectedStatus + " snapshot must preserve the exact source boundary token.");
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(sourceFactsBeforeRestore));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(sourceRootsBeforeRestore));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraphBeforeRestore));
            Assert.That(sourceToken.CompletedCoreSequence, Is.EqualTo(sourceSequenceBeforeRestore));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAfterReject),
                Is.True, sourceAfterReject.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAfterReject),
                Is.True, controlAfterReject.ToString());
            DailyCaptureEligibilityToken sourceRetryToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken controlRetryToken = GetCompletedDailyToken(controlSession.Runtime);
            string retrySourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceRetryToken);
            Assert.That(retrySourceGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlRetryToken)),
                "After rejected staging, the original runtime must continue with identical authoritative results.");
            Assert.That(CaptureContinuationRootFacts(original, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(controlSession, includeWorldIdentity: false)));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(restoredToken.AbsoluteDay, Is.EqualTo(sourceRetryToken.AbsoluteDay));
            Assert.That(restoredToken.CompletedCoreSequence, Is.EqualTo(sourceRetryToken.CompletedCoreSequence));
            Assert.That(restoredToken.BoundaryProvenance,
                Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(retrySourceGraph));
            ActorChoiceInput restoredInput = restored.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == commandId);
            Assert.That(restoredInput.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
            Assert.That(restoredInput.Dispositions, Has.Count.EqualTo(2));
            Assert.That(restoredInput.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredNext),
                Is.True, restoredNext.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNext),
                Is.True, controlNext.ToString());
            DailyCaptureEligibilityToken restoredNextToken = GetCompletedDailyToken(restored.Runtime);
            DailyCaptureEligibilityToken controlNextToken = GetCompletedDailyToken(controlSession.Runtime);
            Assert.That(restoredNextToken.AbsoluteDay, Is.EqualTo(controlNextToken.AbsoluteDay));
            Assert.That(restoredNextToken.CompletedCoreSequence, Is.EqualTo(controlNextToken.CompletedCoreSequence));
            Assert.That(CaptureSelectedDailyFacts(restored.Runtime),
                Is.EqualTo(CaptureSelectedDailyFacts(controlSession.Runtime)));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredNextToken),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlNextToken)),
                "A valid retry and subsequent boundary must preserve exact deterministic continuation parity.");
            Assert.That(CaptureContinuationRootFacts(restored, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(controlSession, includeWorldIdentity: false)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPendingActorChoiceAfterCompletedBoundaryMutationAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        string actionDefinitionId = config.Actions.Single(
            action => action != null && action.actionType == NpcActionType.SellGoods).DefinitionId;
        const string commandId = "p12g-post-boundary-pending-choice";
        PersonId personId = new PersonId("p12g-post-boundary-pending-person");
        GameObject sourceObject = new GameObject("P12-G post-boundary ActorChoice restore source");
        GameObject controlObject = new GameObject("P12-G post-boundary ActorChoice control");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            ConfigureSelectedBootstrap(control, config);
            InvokeInitializeSimulation(source, null);
            InvokeInitializeSimulation(control, null);

            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(
                source, "activeSession");
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(
                control, "activeSession");
            Assert.That(sourceSession, Is.Not.Null);
            Assert.That(controlSession, Is.Not.Null);

            Assert.That(sourceSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(personId), out PersonStoreFailure sourcePersonFailure),
                Is.True, sourcePersonFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(personId), out PersonStoreFailure controlPersonFailure),
                Is.True, controlPersonFailure.ToString());
            NpcRuntime sourceMerchant = sourceSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData?.DefinitionId == "PEDRO");
            NpcRuntime controlMerchant = controlSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData?.DefinitionId == "PEDRO");
            Assert.That(sourceSession.Runtime.TryBindExistingNpcToPerson(
                    personId, sourceMerchant.RuntimeId, out PersonMaterializationFailure sourceBindingFailure),
                Is.True, sourceBindingFailure.ToString());
            Assert.That(controlSession.Runtime.TryBindExistingNpcToPerson(
                    personId, controlMerchant.RuntimeId, out PersonMaterializationFailure controlBindingFailure),
                Is.True, controlBindingFailure.ToString());

            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFirstAdvance),
                Is.True, sourceFirstAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstAdvance),
                Is.True, controlFirstAdvance.ToString());
            DailyCaptureEligibilityToken sourceCompletedBoundary = GetCompletedDailyToken(sourceSession.Runtime);
            DailyCaptureEligibilityToken controlCompletedBoundary = GetCompletedDailyToken(controlSession.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceCompletedBoundary),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlCompletedBoundary)),
                "The source and uninterrupted control must begin from the same supported completed boundary.");

            long sourceRevisionBeforeRequest = sourceSession.Runtime.ActorChoiceStore.CensusRevision;
            long sourceSequenceBeforeRequest = sourceSession.Runtime.ActorChoiceStore.NextInputSequence;
            int sourceCountBeforeRequest = sourceSession.Runtime.ActorChoiceStore.Count;
            Assert.That(sourceCountBeforeRequest, Is.Zero);
            OwnerSectionCensusSnapshot p11AtCompletedBoundary = sourceCompletedBoundary.OwnerSections.Single(
                section => section.SectionId == ActorChoiceP11CensusProvider.SectionId);
            Assert.That(p11AtCompletedBoundary.Cardinality, Is.Zero);
            Assert.That(p11AtCompletedBoundary.Revision, Is.EqualTo(sourceRevisionBeforeRequest));
            Assert.That(p11AtCompletedBoundary.OwnerInstanceIdentity,
                Is.SameAs(sourceSession.Runtime.ActorChoiceStore.CensusOwnerIdentity));
            WorldCommand request = new WorldCommand(
                WorldCommandKind.ActorActionChoice,
                WorldCommandOrigin.LocalPlayer,
                WorldCommandAuthorityMode.Request,
                new ActorActionChoiceWorldCommandPayload(personId, actionDefinitionId));
            WorldCommandHandlerResult sourceQueued = new ActorActionChoiceWorldCommandHandler(sourceSession.Runtime).Execute(
                request, new WorldCommandExecutionContext(commandId));
            WorldCommandHandlerResult controlQueued = new ActorActionChoiceWorldCommandHandler(controlSession.Runtime).Execute(
                request, new WorldCommandExecutionContext(commandId));
            Assert.That(sourceQueued.Success, Is.True, sourceQueued.Diagnostic);
            Assert.That(controlQueued.Success, Is.True, controlQueued.Diagnostic);

            ActorChoiceInput sourcePending = sourceSession.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == commandId);
            ActorChoiceInput controlPending = controlSession.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == commandId);
            Assert.That(sourcePending.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
            Assert.That(sourcePending.Dispositions, Is.Empty);
            Assert.That(sourcePending.InputId, Is.EqualTo(controlPending.InputId));
            Assert.That(sourcePending.InputSequence, Is.EqualTo(sourceSequenceBeforeRequest));
            Assert.That(sourcePending.PersonId, Is.EqualTo(personId));
            Assert.That(sourcePending.ActionDefinitionId, Is.EqualTo(actionDefinitionId));
            Assert.That(sourcePending.Origin, Is.EqualTo(WorldCommandOrigin.LocalPlayer));
            Assert.That(sourcePending.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
            Assert.That(sourceSession.Runtime.ActorChoiceStore.Count, Is.EqualTo(sourceCountBeforeRequest + 1));
            Assert.That(sourceSession.Runtime.ActorChoiceStore.CensusRevision,
                Is.EqualTo(sourceRevisionBeforeRequest + 1L));
            Assert.That(sourceSession.Runtime.ActorChoiceStore.NextInputSequence,
                Is.EqualTo(sourceSequenceBeforeRequest + 1L));
            OwnerSectionCensusWitness p11WitnessAfterRequest =
                sourceSession.Composition.ActorChoiceInputCensusProvider.GetCurrentCensus();
            Assert.That(p11WitnessAfterRequest.SectionId, Is.EqualTo(ActorChoiceP11CensusProvider.SectionId));
            Assert.That(p11WitnessAfterRequest.SchemaVersion, Is.EqualTo(ActorChoiceP11CensusProvider.SchemaVersion));
            Assert.That(p11WitnessAfterRequest.Cardinality, Is.EqualTo(1));
            Assert.That(p11WitnessAfterRequest.Revision,
                Is.EqualTo(sourceSession.Runtime.ActorChoiceStore.CensusRevision));
            Assert.That(p11WitnessAfterRequest.OwnerInstanceIdentity,
                Is.SameAs(sourceSession.Runtime.ActorChoiceStore.CensusOwnerIdentity));
            Assert.That(sourceSession.Runtime.TryValidateCompletedDailyCaptureToken(
                    sourceCompletedBoundary, out DailyCaptureEligibilityFailure staleBoundaryFailure),
                Is.False, "The normal P11 owner mutation after T must invalidate the prior completed boundary.");
            Assert.That(staleBoundaryFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));
            Assert.That(sourceSession.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True,
                "A stale completed token must not be mistaken for a faulted runtime.");

            long sourceDayBeforeRestore = sourceSession.Runtime.CurrentDay;
            string sourceFactsBeforeRestore = CaptureSelectedDailyFacts(sourceSession.Runtime);
            string sourceRootsBeforeRestore = CaptureContinuationRootFacts(sourceSession);
            long sourceRevisionBeforeRestore = sourceSession.Runtime.ActorChoiceStore.CensusRevision;
            long sourceSequenceBeforeRestore = sourceSession.Runtime.ActorChoiceStore.NextInputSequence;
            int sourceCountBeforeRestore = sourceSession.Runtime.ActorChoiceStore.Count;
            OwnerSectionCensusWitness p11WitnessBeforeRestore =
                sourceSession.Composition.ActorChoiceInputCensusProvider.GetCurrentCensus();
            List<P12GDailyV1RestoreStage> restoreStages = new List<P12GDailyV1RestoreStage>();
            P12GDailyV1RestoreFailure restoreFailure;
            string restoreDiagnostic;
            SimulationRuntime.DailyGameplayExecutionProbeForTest gameplayExecutionProbe =
                SimulationRuntime.BeginDailyGameplayExecutionProbeForTest();
            try
            {
                Assert.That(source.TryRestoreDailyContinuation(
                        stage => restoreStages.Add(stage), out restoreFailure, out restoreDiagnostic),
                    Is.False);
            }
            finally
            {
                gameplayExecutionProbe.Dispose();
            }

            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.InvalidSourceSession));
            Assert.That(restoreDiagnostic, Does.Contain("healthy completed-boundary session"));
            Assert.That(restoreStages, Is.Empty,
                "The stale source boundary must be rejected before source capture or any private restore stage.");
            Assert.That(gameplayExecutionProbe.AdvanceDayAfterClockAdvanceInvocationCount, Is.Zero,
                "Rejected source admission must not enter the normal daily gameplay callback pipeline.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(sourceSession));
            Assert.That(source.Bootstrap, Is.SameAs(sourceSession.Composition));
            Assert.That(source.Runtime, Is.SameAs(sourceSession.Runtime));
            Assert.That(sourceSession.Runtime.CurrentDay, Is.EqualTo(sourceDayBeforeRestore));
            Assert.That(sourceSession.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(sourceSession.Runtime), Is.EqualTo(sourceFactsBeforeRestore));
            Assert.That(CaptureContinuationRootFacts(sourceSession), Is.EqualTo(sourceRootsBeforeRestore));
            Assert.That(sourceSession.Runtime.ActorChoiceStore.Count, Is.EqualTo(sourceCountBeforeRestore));
            Assert.That(sourceSession.Runtime.ActorChoiceStore.CensusRevision,
                Is.EqualTo(sourceRevisionBeforeRestore));
            Assert.That(sourceSession.Runtime.ActorChoiceStore.NextInputSequence,
                Is.EqualTo(sourceSequenceBeforeRestore));
            ActorChoiceInput pendingAfterRestore = sourceSession.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == commandId);
            Assert.That(pendingAfterRestore.InputId, Is.EqualTo(sourcePending.InputId));
            Assert.That(pendingAfterRestore.InputSequence, Is.EqualTo(sourcePending.InputSequence));
            Assert.That(pendingAfterRestore.PersonId, Is.EqualTo(sourcePending.PersonId));
            Assert.That(pendingAfterRestore.ActionDefinitionId, Is.EqualTo(sourcePending.ActionDefinitionId));
            Assert.That(pendingAfterRestore.CapturedAbsoluteDay, Is.EqualTo(sourcePending.CapturedAbsoluteDay));
            Assert.That(pendingAfterRestore.Origin, Is.EqualTo(sourcePending.Origin));
            Assert.That(pendingAfterRestore.Authority, Is.EqualTo(sourcePending.Authority));
            Assert.That(pendingAfterRestore.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
            Assert.That(pendingAfterRestore.Dispositions, Is.Empty);
            OwnerSectionCensusWitness p11WitnessAfterRestore =
                sourceSession.Composition.ActorChoiceInputCensusProvider.GetCurrentCensus();
            Assert.That(p11WitnessAfterRestore.SectionId, Is.EqualTo(p11WitnessBeforeRestore.SectionId));
            Assert.That(p11WitnessAfterRestore.SchemaVersion, Is.EqualTo(p11WitnessBeforeRestore.SchemaVersion));
            Assert.That(p11WitnessAfterRestore.Cardinality, Is.EqualTo(p11WitnessBeforeRestore.Cardinality));
            Assert.That(p11WitnessAfterRestore.Revision, Is.EqualTo(p11WitnessBeforeRestore.Revision));
            Assert.That(p11WitnessAfterRestore.OwnerInstanceIdentity,
                Is.SameAs(p11WitnessBeforeRestore.OwnerInstanceIdentity));

            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAfterReject),
                Is.True, sourceAfterReject.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAfterReject),
                Is.True, controlAfterReject.ToString());
            ActorChoiceInput sourceTerminal = sourceSession.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == commandId);
            ActorChoiceInput controlTerminal = controlSession.Runtime.ActorChoiceStore.Inputs
                .Single(input => input.WorldCommandId == commandId);
            Assert.That(sourceTerminal.Status, Is.Not.EqualTo(ActorChoiceInputStatus.Pending));
            Assert.That(sourceTerminal.Status, Is.EqualTo(controlTerminal.Status));
            Assert.That(sourceTerminal.Dispositions.Select(disposition => disposition.Kind),
                Is.EqualTo(controlTerminal.Dispositions.Select(disposition => disposition.Kind)));
            DailyCaptureEligibilityToken sourceRetryBoundary = GetCompletedDailyToken(sourceSession.Runtime);
            DailyCaptureEligibilityToken controlRetryBoundary = GetCompletedDailyToken(controlSession.Runtime);
            string sourceRetryGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceRetryBoundary);
            Assert.That(sourceRetryGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlRetryBoundary)),
                "After the rejected attempt, both normal runtimes must consume the same queued input and graph.");

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(restoredSession, Is.Not.SameAs(sourceSession));
            DailyCaptureEligibilityToken restoredBoundary = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredBoundary),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlRetryBoundary)));

            Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredNext),
                Is.True, restoredNext.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNext),
                Is.True, controlNext.ToString());
            DailyCaptureEligibilityToken restoredNextBoundary = GetCompletedDailyToken(restoredSession.Runtime);
            DailyCaptureEligibilityToken controlNextBoundary = GetCompletedDailyToken(controlSession.Runtime);
            Assert.That(restoredNextBoundary.AbsoluteDay, Is.EqualTo(controlNextBoundary.AbsoluteDay));
            Assert.That(restoredNextBoundary.CompletedCoreSequence, Is.EqualTo(controlNextBoundary.CompletedCoreSequence));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredNextBoundary),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlNextBoundary)),
                "A valid restore after normal input completion must continue with exact owner parity.");
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(controlSession, includeWorldIdentity: false)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
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

            string sourceLateNpcId = sourceSession.Composition.RuntimeIdAllocator.AllocateNpcId();
            string controlLateNpcId = controlSession.Composition.RuntimeIdAllocator.AllocateNpcId();
            Assert.That(sourceLateNpcId, Is.EqualTo(controlLateNpcId));
            NpcRuntime sourceTemplate = sourceSession.Runtime.NpcRuntimes[0];
            NpcRuntime controlTemplate = controlSession.Runtime.NpcRuntimes[0];
            NpcRuntime sourceLateNpc = CreateSelectedDailyV1RosterMember(sourceTemplate, sourceLateNpcId);
            NpcRuntime controlLateNpc = CreateSelectedDailyV1RosterMember(controlTemplate, controlLateNpcId);
            Assert.That(sourceSession.Runtime.TryRegisterNpc(
                    sourceLateNpc,
                    out WorldNpcRegistryFailure sourceRegisterFailure), Is.True, sourceRegisterFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterNpc(
                    controlLateNpc,
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

            List<P12GDailyV1RestoreStage> restoreStages = new List<P12GDailyV1RestoreStage>();
            Assert.That(source.TryRestoreDailyContinuation(
                    restoreStages.Add,
                    out P12GDailyV1RestoreFailure restoreFailure,
                    out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic
                + " Stages: " + string.Join(",", restoreStages));
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

            Assert.That(restoredSession.Runtime.TryUnregisterNpc(
                    sourceLateNpcId,
                    out WorldNpcRegistryFailure sourceUnregisterFailure), Is.True,
                sourceUnregisterFailure.ToString());
            Assert.That(controlSession.Runtime.TryUnregisterNpc(
                    controlLateNpcId,
                    out WorldNpcRegistryFailure controlUnregisterFailure), Is.True,
                controlUnregisterFailure.ToString());
            Assert.That(restoredSession.Runtime.HasExactSelectedRuntimeOwnerGraph(
                    restoredSession.Runtime.SelectedRuntimeOwnerGraph.Definitions), Is.True,
                "The selected owner graph must track the exact roster after an admitted NPC is removed.");
            Assert.That(controlSession.Runtime.HasExactSelectedRuntimeOwnerGraph(
                    controlSession.Runtime.SelectedRuntimeOwnerGraph.Definitions), Is.True);

            Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredAfterRemoval),
                Is.True, restoredAfterRemoval.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAfterRemoval),
                Is.True, controlAfterRemoval.ToString());
            DailyCaptureEligibilityToken restoredAfterRemovalToken = GetCompletedDailyToken(restoredSession.Runtime);
            DailyCaptureEligibilityToken controlAfterRemovalToken = GetCompletedDailyToken(controlSession.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredAfterRemovalToken),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlAfterRemovalToken)),
                "Removing a dynamic selected owner must preserve deterministic continuation and census parity.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controlObject);
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void SelectedCrimeJusticeDailyStagePublishesP12WritesAndUsesWholeRosterCompatibilityFallback()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.hiddenStatus, Is.Not.Null);
        GameObject hostObject = new GameObject("P12 selected Crime Justice daily stage mixed roster");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        ConfigureSelectedBootstrap(host, config);
        InvokeInitializeSimulation(host, null);

        SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
        Assert.That(session, Is.Not.Null);
        SimulationRuntime runtime = session.Runtime;
        DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
        NpcRuntime selected = runtime.NpcRuntimes.First(npc => npc != null && npc.UsesSelectedDailyV1Definitions);
        Assert.That(runtime.HasExactSelectedRuntimeOwnerGraph(graph.Definitions), Is.True);
        Assert.That(runtime.CrimeSystemForWorldBoundary, Is.Not.Null);
        Assert.That(runtime.JusticeSystemForWorldBoundary, Is.Not.Null);

        MethodInfo beginScope = typeof(SimulationRuntime).GetMethod(
            "TryBeginP12CrimeJusticeActionScope", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(beginScope, Is.Not.Null);
        object[] scopeArguments = { new[] { selected }, false, null };
        Assert.That(beginScope.Invoke(runtime, scopeArguments), Is.EqualTo(true));
        IDisposable selectedMutationScope = scopeArguments[2] as IDisposable;
        Assert.That(selectedMutationScope, Is.Not.Null);
        using (selectedMutationScope)
        {
            selected.AddStatus(config.hiddenStatus);
            selected.HideForDays(2);
        }
        Assert.That(selected.HiddenDaysRemaining, Is.EqualTo(3));

        NpcRuntime unselected = new NpcRuntime(
            "p12-selected-crime-justice-unselected-fallback",
            SimulationTestFactory.CreateNpc("p12-selected-crime-justice-unselected-fallback"));
        unselected.AddStatus(config.hiddenStatus);
        unselected.HideForDays(2);
        Assert.That(unselected.UsesSelectedDailyV1Definitions, Is.False);
        Assert.That(runtime.TryRegisterNpc(
                unselected, out WorldNpcRegistryFailure registerFailure), Is.True,
            registerFailure.ToString());
        Assert.That(runtime.HasExactSelectedRuntimeOwnerGraph(graph.Definitions), Is.True,
            "The compatibility NPC must remain outside the selected owner graph.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialCensusFailure), Is.True,
            initialCensusFailure.ToString());

        long selectedRevisionBefore = selected.P12CrimeJusticeRevision;
        long unselectedRevisionBefore = unselected.P12CrimeJusticeRevision;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochBefore, out ContinuationCensusFailure epochBeforeFailure), Is.True,
            epochBeforeFailure.ToString());
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());

        Assert.That(selected.HiddenDaysRemaining, Is.EqualTo(2));
        Assert.That(unselected.HiddenDaysRemaining, Is.EqualTo(2),
            "The established Unity Crime stage still traverses every NPC when the roster is mixed.");
        Assert.That(selected.P12CrimeJusticeRevision, Is.EqualTo(selectedRevisionBefore + 1),
            "The selected Core owner publishes the committed hidden-day decrement exactly once.");
        Assert.That(unselected.P12CrimeJusticeRevision, Is.EqualTo(unselectedRevisionBefore + 1),
            "The legacy compatibility owner publishes the committed hidden-day decrement exactly once.");
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochAfter, out ContinuationCensusFailure epochAfterFailure), Is.True,
            epochAfterFailure.ToString());
        Assert.That(epochAfter, Is.GreaterThan(epochBefore));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalCensusFailure), Is.True,
            finalCensusFailure.ToString());
    }

    [Test]
    public void SelectedCrimeJusticeDailyStageUsesSeparateAdmittedP12WriterScopes()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.hiddenStatus, Is.Not.Null);
        GameObject hostObject = new GameObject("P12 selected Crime Justice daily stage owner scopes");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        ConfigureSelectedBootstrap(host, config);
        InvokeInitializeSimulation(host, null);

        SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
        Assert.That(session, Is.Not.Null);
        SimulationRuntime runtime = session.Runtime;
        DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
        Assert.That(graph, Is.Not.Null);
        Assert.That(runtime.HasExactSelectedRuntimeOwnerGraph(graph.Definitions), Is.True);

        JusticeSystem justice = runtime.JusticeSystemForWorldBoundary;
        Assert.That(justice, Is.Not.Null);
        Assert.That(ReferenceEquals(graph.JusticeRecordsOwner, justice.CoreOwner), Is.True);
        NpcRuntime guard = runtime.NpcRuntimes.First(npc =>
            npc != null && npc.UsesSelectedDailyV1Definitions
            && npc.NpcData?.job?.jobType == NpcJobType.Guard);
        NpcRuntime target = runtime.NpcRuntimes.First(npc =>
            npc != null && npc != guard && npc.UsesSelectedDailyV1Definitions
            && ReferenceEquals(npc.CurrentCity, guard.CurrentCity));
        NpcRuntime hiddenActor = runtime.NpcRuntimes.First(npc =>
            npc != null && npc != guard && npc != target && npc.UsesSelectedDailyV1Definitions);

        MethodInfo beginActionScope = typeof(SimulationRuntime).GetMethod(
            "TryBeginP12CrimeJusticeActionScope", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(beginActionScope, Is.Not.Null);
        object[] scopeArguments = { new[] { target, guard, hiddenActor }, true, null };
        Assert.That(beginActionScope.Invoke(runtime, scopeArguments), Is.EqualTo(true));
        IDisposable setupScope = scopeArguments[2] as IDisposable;
        Assert.That(setupScope, Is.Not.Null);
        using (setupScope)
        {
            Assert.That(justice.CreateOrIncreaseWarrant(target, target.CurrentCity, 12f, 4), Is.Not.Null);
            Assert.That(justice.Arrest(guard, target, target.CurrentCity), Is.True);
            hiddenActor.AddStatus(config.hiddenStatus);
            hiddenActor.HideForDays(2);
        }

        PrisonSentenceRuntime sentence = ReadPrivateField<IList<PrisonSentenceRuntime>>(
            justice, "prisonSentences").Single();
        Assert.That(justice.IsArrested(target), Is.True);
        Assert.That(sentence.WasArrestedToday, Is.True);
        Assert.That(hiddenActor.HiddenDaysRemaining, Is.GreaterThan(0));
        long justiceRevisionBefore = justice.P12CrimeJusticeRevision;
        long targetRevisionBefore = target.P12CrimeJusticeRevision;
        long hiddenRevisionBefore = hiddenActor.P12CrimeJusticeRevision;
        int sentenceDaysBefore = sentence.RemainingDays;
        int hiddenDaysBefore = hiddenActor.HiddenDaysRemaining;
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochBefore, out ContinuationCensusFailure epochBeforeFailure), Is.True,
            epochBeforeFailure.ToString());
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusBeforeFailure), Is.True,
            censusBeforeFailure.ToString());

        string dailyAdvanceOperationId =
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance;
        Assert.That(dailyAdvanceOperationId, Is.EqualTo("runtime.advance-day"));
        MethodInfo enterDailyAdvanceOperation = typeof(SimulationRuntime).GetMethod(
            "TryEnterRuntimeAdmissionOperation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(enterDailyAdvanceOperation, Is.Not.Null);
        object[] dailyOperationArguments = { dailyAdvanceOperationId, null };
        Assert.That(enterDailyAdvanceOperation.Invoke(runtime, dailyOperationArguments), Is.EqualTo(true));
        IDisposable dailyAdvanceScope = dailyOperationArguments[1] as IDisposable;
        Assert.That(dailyAdvanceScope, Is.Not.Null);
        using (dailyAdvanceScope)
        {
            MethodInfo runSelectedDailyStage = typeof(SimulationRuntime).GetMethod(
                "TryAdvanceSelectedCrimeJusticeDailyStage", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(runSelectedDailyStage, Is.Not.Null);
            Assert.That(runSelectedDailyStage.Invoke(runtime, new object[] { graph }), Is.EqualTo(true));
        }

        Assert.That(sentence.WasArrestedToday, Is.False,
            "The first admitted Justice daily scope clears the same-day arrest marker.");
        Assert.That(sentence.RemainingDays, Is.EqualTo(sentenceDaysBefore - 1),
            "The later admitted Justice daily scope advances the sentence once.");
        Assert.That(justice.P12CrimeJusticeRevision, Is.EqualTo(justiceRevisionBefore + 2),
            "The arrest-marker clear and sentence decrement commit as separate Justice owner writes.");
        Assert.That(hiddenActor.HiddenDaysRemaining, Is.EqualTo(hiddenDaysBefore - 1));
        Assert.That(hiddenActor.P12CrimeJusticeRevision, Is.EqualTo(hiddenRevisionBefore + 1),
            "The intervening Crime scope commits the hidden timer exactly once.");
        Assert.That(target.P12CrimeJusticeRevision, Is.EqualTo(targetRevisionBefore),
            "No extra selected target status mutation is published during the no-op wanted projection sync.");
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochAfter, out ContinuationCensusFailure epochAfterFailure), Is.True,
            epochAfterFailure.ToString());
        Assert.That(epochAfter, Is.EqualTo(epochBefore + 3),
            "Only the two changing Justice scopes and the changing Crime scope consume a mutation epoch.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusAfterFailure), Is.True,
            censusAfterFailure.ToString());
    }

    [Test]
    public void SelectedCrimeJusticeDailyMutationRejectsMissingExactCrimeReceiptIdentity()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject hostObject = new GameObject("P12 selected Crime receipt identity mismatch");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        ConfigureSelectedBootstrap(host, config);
        InvokeInitializeSimulation(host, null);

        SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
        Assert.That(session, Is.Not.Null);
        SimulationRuntime runtime = session.Runtime;
        DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
        Assert.That(graph, Is.Not.Null);
        Assert.That(ReadPrivateField<CrimeSystem>(runtime, "crimeSystem"), Is.Not.Null);

        FieldInfo boundCrimeField = typeof(SimulationRuntime).GetField(
            "p12CrimeJusticeBoundCrimeSystem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(boundCrimeField, Is.Not.Null);
        boundCrimeField.SetValue(runtime, null);

        MethodInfo runMutation = typeof(SimulationRuntime).GetMethod(
            "RunP12CrimeJusticeDailyMutation",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(DailyV1SelectedRuntimeOwnerGraph),
                typeof(DailyV1SelectedCrimeJusticeDailyPhase),
                typeof(Action)
            },
            modifiers: null);
        Assert.That(runMutation, Is.Not.Null);
        int mutationCalls = 0;
        TargetInvocationException failure = Assert.Throws<TargetInvocationException>(() =>
            runMutation.Invoke(runtime, new object[]
            {
                graph,
                DailyV1SelectedCrimeJusticeDailyPhase.AdvanceHiddenStatuses,
                (Action)(() => mutationCalls++)
            }));

        Assert.That(failure.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(failure.InnerException.Message,
            Does.Contain("exact Crime receipt census identity"));
        Assert.That(mutationCalls, Is.Zero,
            "A missing exact Crime receipt census binding is rejected before the admitted mutation runs.");
    }

    [Test]
    public void SelectedCrimeJusticeDailyStageContinuesAcrossPublicDailyAdvances()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.hiddenStatus, Is.Not.Null);
        GameObject hostObject = new GameObject("P12 selected Crime Justice multi-day public advance");
        simulationObjects.Add(hostObject);
        TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
        ConfigureSelectedBootstrap(host, config);
        InvokeInitializeSimulation(host, null);

        SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
        Assert.That(session, Is.Not.Null);
        SimulationRuntime runtime = session.Runtime;
        DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
        Assert.That(graph, Is.Not.Null);
        Assert.That(runtime.HasExactSelectedRuntimeOwnerGraph(graph.Definitions), Is.True);
        Assert.That(runtime.NpcRuntimes.All(npc => npc == null || npc.UsesSelectedDailyV1Definitions), Is.True,
            "The Core stage is admitted only for the complete selected Daily-v1 owner roster.");

        JusticeSystem justice = runtime.JusticeSystemForWorldBoundary;
        Assert.That(justice, Is.Not.Null);
        Assert.That(ReferenceEquals(graph.JusticeRecordsOwner, justice.CoreOwner), Is.True);
        NpcRuntime guard = runtime.NpcRuntimes.First(npc =>
            npc != null && npc.UsesSelectedDailyV1Definitions
            && npc.NpcData?.job?.jobType == NpcJobType.Guard);
        NpcRuntime target = runtime.NpcRuntimes.First(npc =>
            npc != null && npc != guard && npc.UsesSelectedDailyV1Definitions
            && ReferenceEquals(npc.CurrentCity, guard.CurrentCity));
        NpcRuntime hiddenActor = runtime.NpcRuntimes.First(npc =>
            npc != null && npc != guard && npc != target && npc.UsesSelectedDailyV1Definitions);

        MethodInfo beginActionScope = typeof(SimulationRuntime).GetMethod(
            "TryBeginP12CrimeJusticeActionScope", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(beginActionScope, Is.Not.Null);
        object[] scopeArguments = { new[] { target, guard, hiddenActor }, true, null };
        Assert.That(beginActionScope.Invoke(runtime, scopeArguments), Is.EqualTo(true));
        IDisposable setupScope = scopeArguments[2] as IDisposable;
        Assert.That(setupScope, Is.Not.Null);
        using (setupScope)
        {
            Assert.That(justice.CreateOrIncreaseWarrant(target, target.CurrentCity, 12f, 4), Is.Not.Null);
            Assert.That(justice.Arrest(guard, target, target.CurrentCity), Is.True);
            hiddenActor.AddStatus(config.hiddenStatus);
            hiddenActor.HideForDays(3);
        }

        PrisonSentenceRuntime sentence = ReadPrivateField<IList<PrisonSentenceRuntime>>(
            justice, "prisonSentences").Single();
        int remainingSentenceDays = sentence.RemainingDays;
        int remainingHiddenDays = hiddenActor.HiddenDaysRemaining;
        long justiceRevision = justice.P12CrimeJusticeRevision;
        long hiddenRevision = hiddenActor.P12CrimeJusticeRevision;
        Assert.That(sentence.WasArrestedToday, Is.True);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochBefore, out ContinuationCensusFailure epochFailure), Is.True,
            epochFailure.ToString());
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.True,
            censusFailure.ToString());
        long startingDay = runtime.CurrentDay;

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure), Is.True,
            firstAdvanceFailure.ToString());
        Assert.That(runtime.CurrentDay, Is.EqualTo(startingDay + 1));
        Assert.That(sentence.WasArrestedToday, Is.False,
            "The first public admitted day executes the selected Justice clear-arrested marker phase.");
        Assert.That(sentence.RemainingDays, Is.EqualTo(remainingSentenceDays - 1),
            "The first public admitted day decrements the selected sentence once.");
        Assert.That(hiddenActor.HiddenDaysRemaining, Is.EqualTo(remainingHiddenDays - 1),
            "The first public admitted day decrements the selected Crime hidden timer once.");
        Assert.That(justice.P12CrimeJusticeRevision, Is.GreaterThanOrEqualTo(justiceRevision + 2),
            "The arrest-marker clear and sentence advancement commit through admitted Justice writes.");
        Assert.That(hiddenActor.P12CrimeJusticeRevision, Is.GreaterThanOrEqualTo(hiddenRevision + 1),
            "The hidden-state decrement commits through its selected NPC owner.");
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochAfterFirst, out ContinuationCensusFailure firstEpochFailure), Is.True,
            firstEpochFailure.ToString());
        Assert.That(epochAfterFirst, Is.GreaterThan(epochBefore));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure firstCensusFailure), Is.True,
            firstCensusFailure.ToString());

        long justiceRevisionAfterFirst = justice.P12CrimeJusticeRevision;
        long hiddenRevisionAfterFirst = hiddenActor.P12CrimeJusticeRevision;
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure secondAdvanceFailure), Is.True,
            secondAdvanceFailure.ToString());
        Assert.That(runtime.CurrentDay, Is.EqualTo(startingDay + 2));
        Assert.That(sentence.RemainingDays, Is.EqualTo(remainingSentenceDays - 2),
            "The second public admitted day continues the same selected sentence exactly once.");
        Assert.That(justice.IsArrested(target), Is.True,
            "The selected actor remains arrested while its active sentence has days remaining.");
        Assert.That(hiddenActor.HiddenDaysRemaining, Is.EqualTo(remainingHiddenDays - 2),
            "The second public admitted day continues the selected Crime hidden timer exactly once.");
        Assert.That(justice.P12CrimeJusticeRevision, Is.GreaterThan(justiceRevisionAfterFirst));
        Assert.That(hiddenActor.P12CrimeJusticeRevision, Is.GreaterThan(hiddenRevisionAfterFirst));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epochAfterSecond, out ContinuationCensusFailure secondEpochFailure), Is.True,
            secondEpochFailure.ToString());
        Assert.That(epochAfterSecond, Is.GreaterThan(epochAfterFirst));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure secondCensusFailure), Is.True,
            secondCensusFailure.ToString());
    }

    [Test]
    public void DailyV1RestoreHydratesNonemptyPersonAndGenealogyGraphAndContinuesDeterministically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G Person and Genealogy restore source");
        GameObject controlObject = new GameObject("P12-G Person and Genealogy restore control");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            ConfigureSelectedBootstrap(control, config);
            InvokeInitializeSimulation(source, null);
            InvokeInitializeSimulation(control, null);
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            Assert.That(sourceSession, Is.Not.Null);
            Assert.That(controlSession, Is.Not.Null);

            PersonId parentId = new PersonId("p12g-evolved-genealogy-parent");
            PersonId childId = new PersonId("p12g-evolved-genealogy-child");
            Assert.That(sourceSession.Runtime.TryRegisterPerson(
                new PersonRuntime(parentId, 0L), out PersonStoreFailure sourceParentFailure),
                Is.True, sourceParentFailure.ToString());
            Assert.That(sourceSession.Runtime.TryRegisterPerson(
                new PersonRuntime(childId, 0L), out PersonStoreFailure sourceChildFailure),
                Is.True, sourceChildFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(
                new PersonRuntime(parentId, 0L), out PersonStoreFailure controlParentFailure),
                Is.True, controlParentFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(
                new PersonRuntime(childId, 0L), out PersonStoreFailure controlChildFailure),
                Is.True, controlChildFailure.ToString());

            NpcRuntime sourceNpc = sourceSession.Runtime.NpcRuntimes
                .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal).First();
            NpcRuntime controlNpc = controlSession.Runtime.NpcRuntimes
                .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal).First();
            Assert.That(sourceNpc.RuntimeId, Is.EqualTo(controlNpc.RuntimeId));
            Assert.That(sourceNpc.PersonId, Is.Null);
            Assert.That(controlNpc.PersonId, Is.Null);
            Assert.That(sourceSession.Runtime.TryBindExistingNpcToPerson(
                childId, sourceNpc.RuntimeId, out PersonMaterializationFailure sourceBindFailure),
                Is.True, sourceBindFailure.ToString());
            Assert.That(controlSession.Runtime.TryBindExistingNpcToPerson(
                childId, controlNpc.RuntimeId, out PersonMaterializationFailure controlBindFailure),
                Is.True, controlBindFailure.ToString());
            Assert.That(sourceSession.Runtime.TryAddParentage(
                parentId, childId, out PersonGenealogyFailure sourceGenealogyFailure),
                Is.True, sourceGenealogyFailure.ToString());
            Assert.That(controlSession.Runtime.TryAddParentage(
                parentId, childId, out PersonGenealogyFailure controlGenealogyFailure),
                Is.True, controlGenealogyFailure.ToString());
            Assert.That(sourceSession.Runtime.PersonStore.Persons.Count, Is.EqualTo(2));
            Assert.That(sourceSession.Runtime.ContainsParentage(parentId, childId), Is.True);
            Assert.That(sourceSession.Runtime.NpcRuntimes.Count(npc => npc.PersonId != null), Is.EqualTo(1));
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());

            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(sourceSession.Runtime);
            DailyCaptureEligibilityToken controlToken = GetCompletedDailyToken(controlSession.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, controlToken)),
                "Equivalent Person bindings and genealogy edges must produce the same included owner graph.");
            OwnerSectionCensusSnapshot[] sourcePersonSections = sourceToken.OwnerSections
                .Where(section => section.SectionId.StartsWith(
                    PersonLifeResidenceCensusProvider.SectionPrefix, StringComparison.Ordinal))
                .OrderBy(section => section.SectionId, StringComparer.Ordinal).ToArray();
            Assert.That(sourcePersonSections.Length, Is.EqualTo(2));
            Assert.That(sourcePersonSections.All(section => section.Cardinality == 1), Is.True);

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure, out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(restoredSession.Runtime.PersonStore.Persons.Count, Is.EqualTo(2));
            Assert.That(restoredSession.Runtime.PersonStore.TryGet(parentId, out PersonRuntime restoredParent), Is.True);
            Assert.That(restoredSession.Runtime.PersonStore.TryGet(childId, out PersonRuntime restoredChild), Is.True);
            Assert.That(restoredParent, Is.Not.SameAs(sourceSession.Runtime.PersonStore.Persons.Single(
                person => person.PersonId == parentId)));
            Assert.That(restoredChild, Is.Not.SameAs(sourceSession.Runtime.PersonStore.Persons.Single(
                person => person.PersonId == childId)));
            Assert.That(restoredChild.MaterializedNpcRuntimeId, Is.EqualTo(sourceNpc.RuntimeId));
            Assert.That(restoredSession.Runtime.NpcRuntimes.Count(npc => npc.PersonId != null), Is.EqualTo(1));
            Assert.That(restoredSession.Runtime.NpcRuntimes.Single(npc => npc.PersonId == childId).RuntimeId,
                Is.EqualTo(sourceNpc.RuntimeId));
            Assert.That(restoredSession.Runtime.ContainsParentage(parentId, childId), Is.True);
            Assert.That(restoredSession.Runtime.GetGenealogyParents(childId).Single(), Is.EqualTo(parentId));
            Assert.That(restoredSession.Runtime.GetGenealogyChildren(parentId).Single(), Is.EqualTo(childId));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken), Is.EqualTo(sourceGraph),
                "The restored D owner package must preserve exact Person identities, bindings and genealogy relations.");
            OwnerSectionCensusSnapshot[] restoredPersonSections = restoredToken.OwnerSections
                .Where(section => section.SectionId.StartsWith(
                    PersonLifeResidenceCensusProvider.SectionPrefix, StringComparison.Ordinal))
                .OrderBy(section => section.SectionId, StringComparer.Ordinal).ToArray();
            Assert.That(restoredPersonSections.Length, Is.EqualTo(2));
            Assert.That(restoredPersonSections.All(section => section.Cardinality == 1), Is.True);
            Assert.That(restoredPersonSections.Select(section => section.SectionId),
                Is.EqualTo(sourcePersonSections.Select(section => section.SectionId)));
            Assert.That(restoredPersonSections.Single(section => section.SectionId == PersonLifeResidenceCensusProvider.SectionIdFor(parentId)).OwnerInstanceIdentity,
                Is.SameAs(restoredParent));
            Assert.That(restoredPersonSections.Single(section => section.SectionId == PersonLifeResidenceCensusProvider.SectionIdFor(childId)).OwnerInstanceIdentity,
                Is.SameAs(restoredChild));

            for (int boundary = 0; boundary < 2; boundary++)
            {
                Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredAdvance),
                    Is.True, restoredAdvance.ToString());
                Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNext),
                    Is.True, controlNext.ToString());
                DailyCaptureEligibilityToken nextRestoredToken = GetCompletedDailyToken(restoredSession.Runtime);
                DailyCaptureEligibilityToken nextControlToken = GetCompletedDailyToken(controlSession.Runtime);
                Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, nextRestoredToken),
                    Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, nextControlToken)),
                    "The evolved Person/Genealogy graph must preserve exact owner parity on every subsequent boundary.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controlObject);
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsBrokenPersonNpcBindingAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G Person/NPC binding source");
        GameObject controlObject = new GameObject("P12-G Person/NPC binding control");
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

            PersonId personId = new PersonId("p12g-broken-binding-person");
            Assert.That(original.Runtime.TryRegisterPerson(new PersonRuntime(personId, 0L), out PersonStoreFailure sourcePersonFailure),
                Is.True, sourcePersonFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(new PersonRuntime(personId, 0L), out PersonStoreFailure controlPersonFailure),
                Is.True, controlPersonFailure.ToString());
            NpcRuntime sourceNpc = original.Runtime.NpcRuntimes
                .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal).First();
            NpcRuntime controlNpc = controlSession.Runtime.NpcRuntimes
                .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal).First();
            Assert.That(sourceNpc.RuntimeId, Is.EqualTo(controlNpc.RuntimeId));
            Assert.That(original.Runtime.TryBindExistingNpcToPerson(
                personId, sourceNpc.RuntimeId, out PersonMaterializationFailure sourceBindFailure),
                Is.True, sourceBindFailure.ToString());
            Assert.That(controlSession.Runtime.TryBindExistingNpcToPerson(
                personId, controlNpc.RuntimeId, out PersonMaterializationFailure controlBindFailure),
                Is.True, controlBindFailure.ToString());
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());

            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string originalGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);
            Assert.That(source.TryRestoreDailyContinuationForTest(candidate =>
            {
                Assert.That(candidate.IdentityRegistry.TryGetNpc(sourceNpc.RuntimeId, out NpcRuntime candidateNpc), Is.True);
                WritePrivateField<PersonRuntime>(candidateNpc, "personRuntime", null);
            }, out P12GDailyV1RestoreFailure restoreFailure, out string diagnostic), Is.False);
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.BindingValidationFailed), diagnostic);
            Assert.That(diagnostic, Does.Contain("Person/NPC"));
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
                "Rejecting a broken private Person/NPC link must preserve later source continuation parity.");

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
    public void DailyV1RestoreRejectsMalformedOrInconsistentP9FingerprintBeforeRootStagingAtomicallyAndAllowsRetry()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G P9 lineage rejection source");
        GameObject controlObject = new GameObject("P12-G P9 lineage rejection control");
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
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string originalGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);
            SimulationGenesisManifest manifest = original.Composition.Manifest;
            string originalSelectedP9Fingerprint = manifest.SelectedP9ProfileFingerprint;
            Assert.That(originalSelectedP9Fingerprint, Is.EqualTo(manifest.Fingerprint));

            string validButInconsistentFingerprint = string.Equals(
                originalSelectedP9Fingerprint.Substring(0, 1), "a", StringComparison.Ordinal)
                ? "b" + originalSelectedP9Fingerprint.Substring(1)
                : "a" + originalSelectedP9Fingerprint.Substring(1);
            Assert.That(validButInconsistentFingerprint, Has.Length.EqualTo(64));
            Assert.That(validButInconsistentFingerprint, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(validButInconsistentFingerprint, Is.Not.EqualTo(originalSelectedP9Fingerprint));

            string[] corruptedFingerprints =
            {
                "invalid-selected-p9-fingerprint",
                validButInconsistentFingerprint
            };
            foreach (string corruptedFingerprint in corruptedFingerprints)
            {
                // Reject both malformed format and a well-formed but inconsistent
                // selected-P9 fingerprint before staging candidate roots.
                WritePrivateField(
                    manifest,
                    "<SelectedP9ProfileFingerprint>k__BackingField",
                    corruptedFingerprint);
                bool sourceCaptured = false;
                bool rootsStaged = false;
                try
                {
                    Assert.That(source.TryRestoreDailyContinuation(stage =>
                    {
                        sourceCaptured |= stage == P12GDailyV1RestoreStage.SourceCaptured;
                        rootsStaged |= stage == P12GDailyV1RestoreStage.RootsStaged;
                    }, out P12GDailyV1RestoreFailure restoreFailure, out string diagnostic), Is.False);
                    Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.SourceCaptureFailed), diagnostic);
                    Assert.That(diagnostic, Does.Contain("P12-C P9 manifest capture failed"));
                    if (string.Equals(corruptedFingerprint, validButInconsistentFingerprint, StringComparison.Ordinal))
                    {
                        Assert.That(diagnostic,
                            Does.Contain("P9-B full-profile and selected-P9 fingerprints must be equal."));
                    }
                    Assert.That(sourceCaptured, Is.True,
                        "The selected-profile source census is completed before retained P9 lineage is validated.");
                    Assert.That(rootsStaged, Is.False,
                        "Invalid P9 lineage must reject before P12-C candidate roots are staged.");
                    Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
                    Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
                    Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
                }
                finally
                {
                    WritePrivateField(
                        manifest,
                        "<SelectedP9ProfileFingerprint>k__BackingField",
                        originalSelectedP9Fingerprint);
                }

                Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(originalGraph),
                    "Rejected P9 lineage must leave the active source owner graph unchanged.");
            }

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedAdvance),
                Is.True, retainedAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure expectedAdvance),
                Is.True, expectedAdvance.ToString());
            DailyCaptureEligibilityToken retainedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken expectedToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedGraph = CaptureCompleteDailyV1OwnerProjection(original, retainedToken);
            Assert.That(continuedGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, expectedToken)),
                "Rejecting invalid P9 lineage must preserve later deterministic continuation parity.");

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure,
                    out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(continuedGraph),
                "Restoring the original P9 lineage must allow a valid retry from the retained boundary.");

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredAdvance),
                Is.True, restoredAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextControlAdvance),
                Is.True, nextControlAdvance.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "A valid retry must continue with exact owner parity at the next boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(controlObject);
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void RestoredDailyOwnerBaselineAcceptsPopulatedPoliticalKnowledgeAndDecisionOwners()
    {
        PersonStore people = new PersonStore();
        PersonId parentId = new PersonId("p12g-restored-political-parent");
        PersonId childId = new PersonId("p12g-restored-political-child");
        Assert.That(people.TryRegister(new PersonRuntime(parentId, 0L), out PersonStoreFailure parentFailure),
            Is.True, parentFailure.ToString());
        Assert.That(people.TryRegister(new PersonRuntime(childId, 0L), out PersonStoreFailure childFailure),
            Is.True, childFailure.ToString());

        GenealogyStore genealogy = new GenealogyStore();
        Assert.That(genealogy.TryAddParentage(parentId, childId, out GenealogyFailure genealogyFailure),
            Is.True, genealogyFailure.ToString());
        InstitutionStore institutions = new InstitutionStore();
        OfficeStore offices = new OfficeStore(institutions);
        PropertyOwnershipStore property = new PropertyOwnershipStore(people);
        PoliticalClaimStore claims = new PoliticalClaimStore();
        FactionStore factions = new FactionStore(people);
        PoliticalKnowledgeStore knowledge = new PoliticalKnowledgeStore(
            people, institutions, claims, factions, offices, property);
        PoliticalKnowledgeHolder holder = PoliticalKnowledgeHolder.ForPerson(parentId);
        Assert.That(knowledge.TryRegisterHolder(holder, 0L, out PoliticalKnowledgeFailure holderFailure),
            Is.True, holderFailure.ToString());
        Assert.That(knowledge.TryRecordObservation(
            holder,
            new PersonDeathKnowledgeObservation(
                childId,
                false,
                null,
                0L,
                0L,
                new PoliticalKnowledgeProvenance(
                    PoliticalKnowledgeSource.DirectObservation,
                    "p12g-restored-political-child")),
            0L,
            out PoliticalKnowledgeFailure observationFailure),
            Is.True, observationFailure.ToString());

        PoliticalDecisionStore decisions = new PoliticalDecisionStore();
        PoliticalDecisionId decisionId = new PoliticalDecisionId("p12g-restored-political-decision");
        PoliticalDecisionRecord decision = new PoliticalDecisionRecord(
            decisionId,
            holder,
            PoliticalDecisionKind.Other,
            Array.Empty<PersonId>(),
            PoliticalDecisionOutcome.None(),
            0L,
            0L,
            Array.Empty<string>(),
            Array.Empty<string>(),
            0L,
            knowledge.Revision);
        Assert.That(decisions.TryRegister(decision, out PoliticalDecisionFailure decisionFailure),
            Is.True, decisionFailure.ToString());

        SimulationRuntimeAdmissionContext bootstrapContext =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            cities: null,
            npcRuntimes: null,
            economyEnabled: false,
            runtimeAdmissionContext: bootstrapContext.CreateRestoredContinuationContext(),
            personStore: people,
            genealogyStore: genealogy,
            institutionStore: institutions,
            officeStore: offices,
            propertyOwnershipStore: property,
            politicalClaimStore: claims,
            factionStore: factions,
            politicalKnowledgeStore: knowledge,
            politicalDecisionStore: decisions,
            politicalWorldRevision: 0L,
            worldId: new WorldId(Guid.Parse("7dd77620-2057-4588-87b4-c7b039585299")));

        Assert.That(runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
            out IReadOnlyList<OwnerSectionCensusSnapshot> sections,
            out _,
            out ContinuationCensusFailure censusFailure),
            Is.True, censusFailure.ToString());
        OwnerSectionCensusSnapshot knowledgeSection = sections.Single(section =>
            section.SectionId == PoliticalKnowledgeStoreCensusProvider.SectionId);
        OwnerSectionCensusSnapshot decisionSection = sections.Single(section =>
            section.SectionId == PoliticalDecisionStoreCensusProvider.SectionId);
        Assert.That(knowledgeSection.Cardinality, Is.EqualTo(1));
        Assert.That(knowledgeSection.Revision, Is.EqualTo(knowledge.Revision));
        Assert.That(knowledgeSection.OwnerInstanceIdentity,
            Is.SameAs(runtime.PoliticalKnowledgeStoreForWorldBoundary));
        Assert.That(decisionSection.Cardinality, Is.EqualTo(1));
        Assert.That(decisionSection.Revision, Is.EqualTo(decisions.Revision));
        Assert.That(decisionSection.OwnerInstanceIdentity, Is.TypeOf<PoliticalDecisionStore>());
        Assert.That(((PoliticalDecisionStore)decisionSection.OwnerInstanceIdentity).Count, Is.EqualTo(1));
        Assert.That(runtime.PoliticalKnowledgeHolderCount, Is.EqualTo(1));
        Assert.That(runtime.PoliticalKnowledgeRevision, Is.EqualTo(knowledge.Revision));
        Assert.That(runtime.TryGetPoliticalKnowledge(holder, out PoliticalKnowledgeRuntime restoredKnowledge), Is.True);
        Assert.That(restoredKnowledge.PersonDeathObservations, Has.Count.EqualTo(1));
        Assert.That(restoredKnowledge.PersonDeathObservations[0].PersonId, Is.EqualTo(childId));
        Assert.That(runtime.TryGetPoliticalDecision(decisionId, out PoliticalDecisionRecord restoredDecision), Is.True);
        Assert.That(restoredDecision.Decider, Is.EqualTo(holder));
        Assert.That(restoredDecision.ExpectedKnowledgeRevision, Is.EqualTo(knowledge.Revision));
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
    public void DailyV1RestoreRejectsSameIdNpcProjectionOutsideTheAdmittedRuntimeGraph()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject hostObject = new GameObject("P12-G reject counterfeit runtime identity projection");
        try
        {
            TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(host, config);
            InvokeInitializeSimulation(host, null);
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
            Assert.That(sourceSession, Is.Not.Null);
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());

            SimulationActiveSession privateCandidate = null;
            string counterfeitRuntimeId = null;
            Assert.That(host.TryRestoreDailyContinuationForTest(candidate =>
                {
                    privateCandidate = candidate;
                    NpcRuntime activeNpc = candidate.Runtime.NpcRuntimes.FirstOrDefault();
                    Assert.That(activeNpc, Is.Not.Null);
                    Dictionary<string, NpcRuntime> npcProjection =
                        ReadPrivateField<Dictionary<string, NpcRuntime>>(
                            candidate.IdentityRegistry, "npcsByRuntimeId");
                    Assert.That(npcProjection, Is.Not.Null);
                    Assert.That(npcProjection.TryGetValue(activeNpc.RuntimeId, out NpcRuntime registeredNpc), Is.True);
                    Assert.That(registeredNpc, Is.SameAs(activeNpc));

                    // Keep identity kind and RuntimeId unchanged while replacing
                    // the private candidate's typed projection value. Admission
                    // must reject it before publishing this detached graph.
                    counterfeitRuntimeId = activeNpc.RuntimeId;
                    npcProjection[activeNpc.RuntimeId] = new NpcRuntime(
                        activeNpc.RuntimeId, activeNpc.NpcData, activeNpc.CurrentCity, 0f);
                },
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic),
                Is.False);
            Assert.That(privateCandidate, Is.Not.Null);
            Assert.That(counterfeitRuntimeId, Is.Not.Null.And.Not.Empty);
            Assert.That(privateCandidate.Runtime.HasSameP12RuntimeIdentitySpatialOwners(
                    privateCandidate.IdentityRegistry,
                    privateCandidate.SpatialNetwork,
                    privateCandidate.ExplorableSites),
                Is.False);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.BindingValidationFailed), diagnostic);
            Assert.That(ReadPrivateField<SimulationActiveSession>(host, "activeSession"), Is.SameAs(sourceSession));
            Assert.That(sourceSession.Runtime.CurrentDay, Is.EqualTo(1L));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hostObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsSourceIdentityProjectionOutsideTheAdmittedRuntimeGraph()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject hostObject = new GameObject("P12-G reject counterfeit source identity projection");
        try
        {
            TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(host, config);
            InvokeInitializeSimulation(host, null);
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
            Assert.That(sourceSession, Is.Not.Null);
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            long sourceDay = sourceSession.Runtime.CurrentDay;
            NpcRuntime activeNpc = sourceSession.Runtime.NpcRuntimes.FirstOrDefault();
            Assert.That(activeNpc, Is.Not.Null);
            Dictionary<string, NpcRuntime> npcProjection = ReadPrivateField<Dictionary<string, NpcRuntime>>(
                sourceSession.IdentityRegistry, "npcsByRuntimeId");
            Assert.That(npcProjection, Is.Not.Null);
            npcProjection[activeNpc.RuntimeId] = new NpcRuntime(
                activeNpc.RuntimeId, activeNpc.NpcData, activeNpc.CurrentCity, 0f);

            Assert.That(sourceSession.Runtime.HasSameP12RuntimeIdentitySpatialOwners(
                    sourceSession.IdentityRegistry,
                    sourceSession.SpatialNetwork,
                    sourceSession.ExplorableSites),
                Is.False);
            Assert.That(host.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic),
                Is.False);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.InvalidSourceSession), diagnostic);
            Assert.That(ReadPrivateField<SimulationActiveSession>(host, "activeSession"), Is.SameAs(sourceSession));
            Assert.That(sourceSession.Runtime.CurrentDay, Is.EqualTo(sourceDay));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hostObject);
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
    public void DailyV1RestorePreservesSuccessfulScheduledEscapePrisonEffectAcrossBoundaries()
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        NpcActionData arrest = AssetDatabase.LoadAssetAtPath<NpcActionData>(
            "Assets/_Project/Data/Actions/Action-Prender.asset");
        Assert.That(sourceConfig, Is.Not.Null);
        Assert.That(arrest, Is.Not.Null);
        Assert.That(arrest.actionType, Is.EqualTo(NpcActionType.Arrest));

        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        NpcSimulationConfig actorConfig = config.Npcs.Single(entry =>
            entry?.npc != null && string.Equals(entry.npc.id, "JOBSON", StringComparison.Ordinal));
        NpcData actorDefinition = actorConfig.npc;
        const int initialSentenceDays = 5;
        const float initialWarrantBounty = 50f;
        NpcActionData escape = SimulationTestFactory.CreateAction(
            "p12g-successful-directive-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        escape.canFail = false;
        escape.crimeSettings.escapeBountyPenalty = 7f;
        // The restore/replay regression uses an admitted initial warrant and
        // Core Guard, with autonomous Crime disabled. Scheduled Escape remains
        // runtime-enabled while no independent autonomous Escape can occur on
        // the boundary after the directive succeeds.
        config.crimeAutonomousEnabled = false;
        config.Actions.Add(escape);
        config.InitialWarrants.Clear();
        config.InitialWarrants.Add(new InitialWantedRecordConfig
        {
            target = actorDefinition,
            city = actorConfig.startingCity,
            bounty = initialWarrantBounty,
            sentenceDays = initialSentenceDays
        });
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig
        {
            absoluteDay = 3L,
            mode = ScheduledDirectiveMode.RequestAction,
            operation = ScheduledDirectiveOperation.EscapePrison,
            actor = actorDefinition,
            action = escape
        });

        bool originalArrestCanFail = arrest.canFail;
        // Selected runtimes execute the immutable definition graph captured at
        // bootstrap. Configure successful outcomes before either graph is built.
        GameObject uninterruptedObject = new GameObject("P12-G successful directive uninterrupted");
        GameObject restoredObject = new GameObject("P12-G successful directive restored");
        try
        {
            arrest.canFail = false;
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

            NpcRuntime controlActor = uninterruptedSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData != null && string.Equals(npc.NpcData.id, actorDefinition.id, StringComparison.Ordinal));
            NpcRuntime sourceActor = sourceSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData != null && string.Equals(npc.NpcData.id, actorDefinition.id, StringComparison.Ordinal));
            Assert.That(controlActor.RuntimeId, Is.EqualTo(sourceActor.RuntimeId));
            Assert.That(controlActor.CurrentCity, Is.Not.Null);
            Assert.That(sourceActor.CurrentCity.RuntimeId, Is.EqualTo(controlActor.CurrentCity.RuntimeId));

            NpcRuntime controlGuard = uninterruptedSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData?.job != null && npc.NpcData.job.jobType == NpcJobType.Guard
                    && ReferenceEquals(npc.CurrentCity, controlActor.CurrentCity));
            NpcRuntime sourceGuard = sourceSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData?.job != null && npc.NpcData.job.jobType == NpcJobType.Guard
                    && ReferenceEquals(npc.CurrentCity, sourceActor.CurrentCity));
            Assert.That(controlGuard.RuntimeId, Is.EqualTo(sourceGuard.RuntimeId));

            foreach (SimulationActiveSession session in new[] { uninterruptedSession, sourceSession })
            {
                foreach (NpcRuntime participant in session.Runtime.Cities
                             .Single(city => city.RuntimeId == controlActor.CurrentCity.RuntimeId).ImportantNpcs)
                {
                    Assert.That(participant?.NpcData, Is.Not.Null);
                    PersonId personId = new PersonId("p12g-directive-person-" + participant.NpcData.id);
                    Assert.That(session.Runtime.TryRegisterPerson(
                            new PersonRuntime(personId, session.Runtime.CurrentDay),
                            out PersonStoreFailure registrationFailure), Is.True, registrationFailure.ToString());
                    Assert.That(session.Runtime.TryBindExistingNpcToPerson(
                            personId, participant.RuntimeId,
                            out PersonMaterializationFailure bindingFailure), Is.True, bindingFailure.ToString());
                }
                Assert.That(session.Runtime.TryAssessNpcRosterCensus(
                        out ContinuationCensusFailure censusFailure), Is.True, censusFailure.ToString());
            }

            void AssertBuiltInDecisionConfiguration(SimulationRuntime runtime)
            {
                EffectiveSimulationConfiguration effective = ReadPrivateField<EffectiveSimulationConfiguration>(
                    runtime, "configuration");
                NpcDecisionSystem decisions = ReadPrivateField<NpcDecisionSystem>(runtime, "npcDecisionSystem");
                Assert.That(effective, Is.Not.Null);
                Assert.That(effective.Crime.Enabled, Is.True);
                Assert.That(effective.Crime.AutonomousEnabled, Is.False);
                Assert.That(effective.GuardCrime.Enabled, Is.True);
                Assert.That(decisions, Is.Not.Null);
                Assert.That(decisions.HasProvider<CrimeSystem>(), Is.True);
                Assert.That(decisions.HasProvider<GuardSystem>(), Is.True);
                Assert.That(decisions.GetProviderForAction(escape), Is.TypeOf<CrimeSystem>());
                Assert.That(decisions.GetProviderForAction(arrest), Is.TypeOf<GuardSystem>());
            }

            long ReadMutationEpoch(SimulationRuntime runtime)
            {
                Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
                        out long epoch, out ContinuationCensusFailure failure),
                    Is.True, failure.ToString());
                return epoch;
            }

            JusticeSystem controlJustice = ReadPrivateField<JusticeSystem>(uninterruptedSession.Runtime, "justiceSystem");
            JusticeSystem sourceJustice = ReadPrivateField<JusticeSystem>(sourceSession.Runtime, "justiceSystem");
            Assert.That(controlJustice, Is.Not.Null);
            Assert.That(sourceJustice, Is.Not.Null);

            Assert.That(controlJustice.HasActiveWarrantInCity(controlActor, controlActor.CurrentCity), Is.True);
            Assert.That(sourceJustice.HasActiveWarrantInCity(sourceActor, sourceActor.CurrentCity), Is.True);
            Assert.That(controlJustice.GetBounty(controlActor, controlActor.CurrentCity),
                Is.EqualTo(initialWarrantBounty));
            Assert.That(sourceJustice.GetBounty(sourceActor, sourceActor.CurrentCity),
                Is.EqualTo(initialWarrantBounty));

            // The effective built-in provider composition is a compatibility
            // input. Autonomous Crime is disabled, while the same admitted
            // Crime and Guard providers remain composed on every host.
            AssertBuiltInDecisionConfiguration(uninterruptedSession.Runtime);
            AssertBuiltInDecisionConfiguration(sourceSession.Runtime);
            Assert.That(controlJustice.HasActiveWarrantInCity(controlActor, controlActor.CurrentCity), Is.True);
            Assert.That(sourceJustice.HasActiveWarrantInCity(sourceActor, sourceActor.CurrentCity), Is.True);
            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlArrestFailure),
                Is.True, controlArrestFailure.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceArrestFailure),
                Is.True, sourceArrestFailure.ToString());
            Assert.That(controlJustice.IsArrested(controlActor), Is.True,
                "The selected Daily-v1 Guard must arrest the initial-warrant target through the normal Arrest action.");
            Assert.That(sourceJustice.IsArrested(sourceActor), Is.True);
            Assert.That(controlJustice.WasArrestedToday(controlActor), Is.True);
            Assert.That(sourceJustice.WasArrestedToday(sourceActor), Is.True);
            Assert.That(controlJustice.GetRemainingSentenceDays(controlActor),
                Is.EqualTo(initialSentenceDays));
            Assert.That(sourceJustice.GetRemainingSentenceDays(sourceActor),
                Is.EqualTo(initialSentenceDays));

            ScheduledDirectiveStore sourceDirectiveStore = sourceSession.Composition.ScheduledDirectives;
            ScheduledDirective sourceDirective = sourceDirectiveStore.Directives.Single();
            Assert.That(sourceDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending));
            Assert.That(sourceDirective.AbsoluteDay, Is.EqualTo(3L));
            long sourceDirectiveRevision = sourceDirectiveStore.Revision;
            long controlEpochAtSourceBoundary = ReadMutationEpoch(uninterruptedSession.Runtime);
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(sourceSession.Runtime);
            string sourceOwnerGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            string sourceRoots = CaptureContinuationRootFacts(sourceSession, includeWorldIdentity: false);
            Assert.That(sourceOwnerGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));
            Assert.That(sourceRoots, Is.EqualTo(CaptureContinuationRootFacts(
                uninterruptedSession, includeWorldIdentity: false)));

            Assert.That(restored.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure,
                    out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
            Assert.That(restoredSession, Is.Not.SameAs(sourceSession));
            AssertBuiltInDecisionConfiguration(restoredSession.Runtime);
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken), Is.EqualTo(sourceOwnerGraph));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false), Is.EqualTo(sourceRoots));
            Assert.That(restoredSession.Runtime.CurrentDay, Is.EqualTo(sourceSession.Runtime.CurrentDay));

            NpcRuntime restoredActor = restoredSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData != null && string.Equals(npc.NpcData.id, actorDefinition.id, StringComparison.Ordinal));
            JusticeSystem restoredJustice = ReadPrivateField<JusticeSystem>(restoredSession.Runtime, "justiceSystem");
            Assert.That(restoredJustice.IsArrested(restoredActor), Is.True);
            Assert.That(restoredJustice.WasArrestedToday(restoredActor),
                Is.EqualTo(controlJustice.WasArrestedToday(controlActor)),
                "Restore must retain the persisted day-1 Guard arrest marker at the captured boundary.");
            Assert.That(restoredJustice.WasArrestedToday(restoredActor), Is.True);

            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlResetFailure),
                Is.True, controlResetFailure.ToString());
            Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredResetFailure),
                Is.True, restoredResetFailure.ToString());
            Assert.That(controlJustice.WasArrestedToday(controlActor), Is.False,
                "Start-of-day reset clears the prior-day Guard arrest marker before the next sentence decrement.");
            Assert.That(restoredJustice.WasArrestedToday(restoredActor), Is.False,
                "The restored runtime must apply the same next-day reset after hydrating the true marker.");
            Assert.That(restoredJustice.GetRemainingSentenceDays(restoredActor),
                Is.EqualTo(controlJustice.GetRemainingSentenceDays(controlActor)));
            Assert.That(restoredJustice.GetBounty(restoredActor, restoredActor.CurrentCity),
                Is.EqualTo(controlJustice.GetBounty(controlActor, controlActor.CurrentCity)));
            Assert.That(controlJustice.GetRemainingSentenceDays(controlActor), Is.GreaterThan(2),
                "The day-3 RequestAction must remain eligible after that day's sentence decrement.");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    restoredSession, GetCompletedDailyToken(restoredSession.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));

            ScheduledDirective restoredDirective = restoredSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(restoredDirective.DirectiveId, Is.EqualTo(sourceDirective.DirectiveId));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending));
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision, Is.EqualTo(sourceDirectiveRevision));
            // Shared mutation epochs are target-runtime bookkeeping rather
            // than persisted owner state. Both boundary reads must be valid;
            // continuation parity is checked by the next boundary's delta.
            long sourceEpochAtRestoreBoundary = ReadMutationEpoch(sourceSession.Runtime);
            long restoredEpochAtRestoreBoundary = ReadMutationEpoch(restoredSession.Runtime);
            Assert.That(sourceEpochAtRestoreBoundary, Is.EqualTo(controlEpochAtSourceBoundary),
                "The uninterrupted and source runtimes have applied the same day-1 mutations before restoration.");
            Assert.That(sourceEpochAtRestoreBoundary, Is.GreaterThanOrEqualTo(0L));
            Assert.That(restoredEpochAtRestoreBoundary, Is.GreaterThanOrEqualTo(0L));

            float bountyBeforeEscape = controlJustice.GetBounty(controlActor, controlActor.CurrentCity);
            long directiveRevisionBeforeEscape = sourceDirectiveStore.Revision;
            long controlEpochBeforeEscape = ReadMutationEpoch(uninterruptedSession.Runtime);
            long restoredEpochBeforeEscape = ReadMutationEpoch(restoredSession.Runtime);
            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlEscapeFailure),
                Is.True, controlEscapeFailure.ToString());
            Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredEscapeFailure),
                Is.True, restoredEscapeFailure.ToString());
            long controlEpochAfterEscape = ReadMutationEpoch(uninterruptedSession.Runtime);
            long restoredEpochAfterEscape = ReadMutationEpoch(restoredSession.Runtime);
            Assert.That(controlEpochAfterEscape, Is.GreaterThan(controlEpochBeforeEscape));
            Assert.That(restoredEpochAfterEscape, Is.GreaterThan(restoredEpochBeforeEscape));
            Assert.That(restoredEpochAfterEscape - restoredEpochBeforeEscape,
                Is.EqualTo(controlEpochAfterEscape - controlEpochBeforeEscape),
                "The restored runtime applies the same committed owner invalidations through the Escape/Guard boundary.");

            ScheduledDirective controlDirective = uninterruptedSession.Composition.ScheduledDirectives.Directives.Single();
            restoredDirective = restoredSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(controlDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(controlDirective.ProcessedDay, Is.EqualTo(3L));
            Assert.That(restoredDirective.ProcessedDay, Is.EqualTo(controlDirective.ProcessedDay));
            Assert.That(uninterruptedSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(directiveRevisionBeforeEscape + 1L));
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(uninterruptedSession.Composition.ScheduledDirectives.Revision));
            Assert.That(controlJustice.IsArrested(controlActor), Is.True,
                "The ordered Guard turn may re-arrest the actor after the scheduled Escape succeeds in the same daily boundary.");
            Assert.That(restoredJustice.IsArrested(restoredActor), Is.True,
                "Restoration must preserve the same-day ordered Guard response after the successful scheduled Escape.");
            Assert.That(controlJustice.GetRemainingSentenceDays(controlActor),
                Is.EqualTo(initialSentenceDays),
                "The later ordered Guard arrest creates a fresh sentence from the committed warrant policy.");
            Assert.That(restoredJustice.GetRemainingSentenceDays(restoredActor),
                Is.EqualTo(controlJustice.GetRemainingSentenceDays(controlActor)));
            Assert.That(controlJustice.WasArrestedToday(controlActor), Is.True);
            Assert.That(restoredJustice.WasArrestedToday(restoredActor), Is.True);
            Assert.That(controlJustice.GetBounty(controlActor, controlActor.CurrentCity),
                Is.EqualTo(bountyBeforeEscape + escape.crimeSettings.escapeBountyPenalty));
            float bountyAfterEscape = controlJustice.GetBounty(controlActor, controlActor.CurrentCity);
            Assert.That(restoredJustice.GetBounty(restoredActor, restoredActor.CurrentCity),
                Is.EqualTo(bountyAfterEscape));

            restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            DailyCaptureEligibilityToken controlToken = GetCompletedDailyToken(uninterruptedSession.Runtime);
            string postEscapeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(uninterruptedSession, controlToken);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken), Is.EqualTo(postEscapeOwnerGraph));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));

            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredNextFailure),
                Is.True, restoredNextFailure.ToString());
            Assert.That(controlJustice.GetBounty(controlActor, controlActor.CurrentCity),
                Is.EqualTo(bountyAfterEscape),
                "A terminal successful directive must not apply its Escape effect on a later boundary.");
            Assert.That(restoredJustice.GetBounty(restoredActor, restoredActor.CurrentCity),
                Is.EqualTo(bountyAfterEscape),
                "The restored terminal directive must not replay its Escape effect on a later boundary.");
            Assert.That(controlDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(controlDirective.ProcessedDay, Is.EqualTo(3L));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(restoredDirective.ProcessedDay, Is.EqualTo(3L));
            Assert.That(uninterruptedSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(directiveRevisionBeforeEscape + 1L));
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(uninterruptedSession.Composition.ScheduledDirectives.Revision));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    restoredSession, GetCompletedDailyToken(restoredSession.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));
        }
        finally
        {
            arrest.canFail = originalArrestCanFail;
            UnityEngine.Object.DestroyImmediate(uninterruptedObject);
            UnityEngine.Object.DestroyImmediate(restoredObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void DailyV1RestorePreservesNonemptyCrimeSocialOwnersAcrossContinuation()
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        NpcActionData steal = AssetDatabase.LoadAssetAtPath<NpcActionData>(
            "Assets/_Project/Data/Actions/Action-Roubar.asset");
        Assert.That(sourceConfig, Is.Not.Null);
        Assert.That(steal, Is.Not.Null);
        Assert.That(steal.actionType, Is.EqualTo(NpcActionType.Steal));

        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        bool originalStealCanFail = steal.canFail;
        GameObject uninterruptedObject = new GameObject("P12 CrimeSocial uninterrupted control");
        GameObject restoredObject = new GameObject("P12 CrimeSocial restored continuation");
        try
        {
            // The selected graph captures authored action policy at bootstrap.
            // Make the proving Steal outcome deterministic before either host
            // composes its Daily-v1 definition graph.
            steal.canFail = false;
            TesteSimulacao uninterrupted = uninterruptedObject.AddComponent<TesteSimulacao>();
            TesteSimulacao restored = restoredObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(uninterrupted, config);
            ConfigureSelectedBootstrap(restored, config);
            InvokeInitializeSimulation(uninterrupted, null);
            InvokeInitializeSimulation(restored, null);

            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(
                uninterrupted, "activeSession");
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(
                restored, "activeSession");
            Assert.That(controlSession, Is.Not.Null);
            Assert.That(sourceSession, Is.Not.Null);

            void SelectOnlySteal(SimulationRuntime runtime)
            {
                List<NpcActionData> configuredActions = ReadPrivateField<List<NpcActionData>>(
                    runtime, "configuredActions");
                Assert.That(configuredActions, Is.Not.Null);
                configuredActions.Clear();
                configuredActions.Add(steal);
            }

            NpcRuntime AdmitStealParticipants(SimulationRuntime runtime)
            {
                NpcRuntime thief = runtime.NpcRuntimes.FirstOrDefault(candidate =>
                    candidate?.NpcData?.acoesPadrao != null
                    && candidate.NpcData.acoesPadrao.Any(preference =>
                        preference != null && ReferenceEquals(preference.action, steal)));
                Assert.That(thief, Is.Not.Null,
                    "The selected profile must retain the authored NPC with the default Steal action.");
                CityRuntime city = thief.CurrentCity;
                Assert.That(city, Is.Not.Null);
                Assert.That(city.ImportantNpcs.Count, Is.GreaterThan(1),
                    "The admitted local city roster must provide an eligible theft target.");

                foreach (NpcRuntime participant in city.ImportantNpcs)
                {
                    PersonId personId = new PersonId("p12-crime-social-restore-" + participant.RuntimeId);
                    Assert.That(runtime.TryRegisterPerson(new PersonRuntime(personId, runtime.CurrentDay),
                        out PersonStoreFailure registrationFailure), Is.True, registrationFailure.ToString());
                    Assert.That(runtime.TryBindExistingNpcToPerson(personId, participant.RuntimeId,
                        out PersonMaterializationFailure bindingFailure), Is.True, bindingFailure.ToString());
                }
                Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure),
                    Is.True, censusFailure.ToString());
                SelectOnlySteal(runtime);
                return thief;
            }

            void AssertNonemptyCrimeSocial(SimulationRuntime runtime)
            {
                CrimeSocialAppraisalWorldState crimeSocial = runtime.CrimeSocialAppraisal;
                Assert.That(crimeSocial, Is.Not.Null);
                Assert.That(crimeSocial.TheftOutcomes.Count, Is.GreaterThan(0));
                Assert.That(crimeSocial.CrimeKnowledge.Count, Is.GreaterThan(0));
                Assert.That(crimeSocial.SocialReactions.Count, Is.GreaterThan(0));
            }

            NpcRuntime controlThief = AdmitStealParticipants(controlSession.Runtime);
            NpcRuntime sourceThief = AdmitStealParticipants(sourceSession.Runtime);
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFailure),
                Is.True, controlFailure.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFailure),
                Is.True, sourceFailure.ToString());
            AssertNonemptyCrimeSocial(controlSession.Runtime);
            AssertNonemptyCrimeSocial(sourceSession.Runtime);
            Assert.That(controlSession.Runtime.JusticeSystemForWorldBoundary.HasActiveWarrantInCity(
                    controlThief, controlThief.CurrentCity),
                Is.True,
                "The selected Daily-v1 actor must create a warrant through the successful Steal action.");
            Assert.That(sourceSession.Runtime.JusticeSystemForWorldBoundary.HasActiveWarrantInCity(
                    sourceThief, sourceThief.CurrentCity),
                Is.True,
                "The source runtime must retain the same Steal-to-Justice effect before capture.");

            string controlProjection = CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime));
            string sourceProjection = CaptureCompleteDailyV1OwnerProjection(
                sourceSession, GetCompletedDailyToken(sourceSession.Runtime));
            Assert.That(sourceProjection, Is.EqualTo(controlProjection));

            Assert.That(restored.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure,
                    out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(
                restored, "activeSession");
            Assert.That(restoredSession, Is.Not.Null);
            Assert.That(restoredSession, Is.Not.SameAs(sourceSession));
            AssertNonemptyCrimeSocial(restoredSession.Runtime);
            NpcRuntime restoredThief = restoredSession.Runtime.NpcRuntimes.Single(candidate =>
                candidate?.RuntimeId == sourceThief.RuntimeId);
            Assert.That(restoredSession.Runtime.JusticeSystemForWorldBoundary.HasActiveWarrantInCity(
                    restoredThief, restoredThief.CurrentCity),
                Is.True,
                "Detached restore must preserve the warrant produced by the admitted Steal action.");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    restoredSession, GetCompletedDailyToken(restoredSession.Runtime)),
                Is.EqualTo(controlProjection));

            // The uninterrupted and restored hosts use the same test action
            // input for the next boundary, then compare all selected owners.
            SelectOnlySteal(controlSession.Runtime);
            SelectOnlySteal(restoredSession.Runtime);
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextControlFailure),
                Is.True, nextControlFailure.ToString());
            Assert.That(restoredSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextRestoredFailure),
                Is.True, nextRestoredFailure.ToString());
            AssertNonemptyCrimeSocial(controlSession.Runtime);
            AssertNonemptyCrimeSocial(restoredSession.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    restoredSession, GetCompletedDailyToken(restoredSession.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))));
        }
        finally
        {
            steal.canFail = originalStealCanFail;
            UnityEngine.Object.DestroyImmediate(uninterruptedObject);
            UnityEngine.Object.DestroyImmediate(restoredObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [TestCase(ScheduledDirectiveMode.ForceOutcome)]
    [TestCase(ScheduledDirectiveMode.RequestAction)]
    public void SelectedDailyV1EscapeCompletesStatusAndDirectiveWithoutCrimeProvider(
        ScheduledDirectiveMode mode)
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(sourceConfig, Is.Not.Null);
        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        NpcSimulationConfig actorConfig = config.Npcs.Single(entry =>
            entry?.npc != null && string.Equals(entry.npc.id, "JOBSON", StringComparison.Ordinal));
        NpcData actorDefinition = actorConfig.npc;
        NpcActionData escape = SimulationTestFactory.CreateAction(
            "p12-selected-forced-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        escape.canFail = false;
        escape.crimeSettings.escapeBountyPenalty = 3f;
        escape.statusToRemove.Add(config.arrestedStatus);
        escape.statusToAdd.Add(config.freeStatus);
        config.Actions.Add(escape);
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig
        {
            absoluteDay = 1L,
            mode = mode,
            operation = ScheduledDirectiveOperation.EscapePrison,
            actor = actorDefinition,
            action = escape
        });

        GameObject hostObject = new GameObject("P12 selected forced Escape scope reuse");
        simulationObjects.Add(hostObject);
        try
        {
            TesteSimulacao host = hostObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(host, config);
            InvokeInitializeSimulation(host, null);
            SimulationActiveSession session = ReadPrivateField<SimulationActiveSession>(host, "activeSession");
            Assert.That(session, Is.Not.Null);
            SimulationRuntime runtime = session.Runtime;
            WritePrivateField(runtime, "npcDecisionSystem",
                new NpcDecisionSystem(new List<INpcActionProvider>()));
            NpcRuntime actor = runtime.NpcRuntimes.Single(npc =>
                npc?.NpcData != null && string.Equals(npc.NpcData.id, actorDefinition.id, StringComparison.Ordinal));
            Assert.That(actor.CurrentCity, Is.Not.Null);
            NpcRuntime guard = runtime.NpcRuntimes.Single(npc =>
                npc?.NpcData?.job != null
                && npc.NpcData.job.jobType == NpcJobType.Guard
                && ReferenceEquals(npc.CurrentCity, actor.CurrentCity));

            foreach (NpcRuntime participant in actor.CurrentCity.ImportantNpcs)
            {
                Assert.That(participant?.NpcData, Is.Not.Null);
                PersonId personId = new PersonId("p12-selected-forced-escape-person-" + participant.NpcData.id);
                Assert.That(runtime.TryRegisterPerson(
                        new PersonRuntime(personId, runtime.CurrentDay), out PersonStoreFailure registrationFailure),
                    Is.True, registrationFailure.ToString());
                Assert.That(runtime.TryBindExistingNpcToPerson(
                        personId, participant.RuntimeId, out PersonMaterializationFailure bindingFailure),
                    Is.True, bindingFailure.ToString());
            }
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialCensusFailure),
                Is.True, initialCensusFailure.ToString());

            JusticeSystem justice = ReadPrivateField<JusticeSystem>(runtime, "justiceSystem");
            Assert.That(justice, Is.Not.Null);
            MethodInfo beginCrimeJusticeScope = typeof(SimulationRuntime).GetMethod(
                "TryBeginP12CrimeJusticeActionScope", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(beginCrimeJusticeScope, Is.Not.Null);
            object[] scopeArguments = { new[] { actor, guard }, true, null };
            Assert.That(beginCrimeJusticeScope.Invoke(runtime, scopeArguments), Is.EqualTo(true));
            IDisposable arrestScope = scopeArguments[2] as IDisposable;
            Assert.That(arrestScope, Is.Not.Null);
            using (arrestScope)
            {
                Assert.That(justice.CreateOrIncreaseWarrant(actor, actor.CurrentCity, 12f, 4), Is.Not.Null);
                Assert.That(justice.Arrest(guard, actor, actor.CurrentCity), Is.True);
            }

            // Keep Escape as the scheduled action under test. A later ordered
            // Guard turn is still allowed to respond to its successful release.
            List<NpcActionData> configuredActions = ReadPrivateField<List<NpcActionData>>(
                runtime, "configuredActions");
            Assert.That(configuredActions, Is.Not.Null);
            configuredActions.Clear();
            configuredActions.Add(escape);

            Assert.That(justice.IsArrested(actor), Is.True);
            Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
                    out long beforeEscapeEpoch, out ContinuationCensusFailure beforeEscapeFailure),
                Is.True, beforeEscapeFailure.ToString());
            Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());

            ScheduledDirective directive = session.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(directive.ProcessedDay, Is.EqualTo(1L));
            if (mode == ScheduledDirectiveMode.RequestAction)
            {
                Assert.That(justice.IsArrested(actor), Is.True,
                    "The ordered Guard turn may arrest the actor after a successful scheduled Escape in the same daily boundary.");
                Assert.That(justice.WasArrestedToday(actor), Is.True);
                Assert.That(actor.CurrentStatus.Any(status => ReferenceEquals(status, config.arrestedStatus)), Is.True);
                Assert.That(actor.CurrentStatus.Any(status => ReferenceEquals(status, config.freeStatus)), Is.False);
                Assert.That(justice.GetBounty(actor, actor.CurrentCity), Is.EqualTo(15f),
                    "The successful Escape penalty remains committed even when a later Guard turn re-arrests the actor.");
            }
            else
            {
                Assert.That(justice.IsArrested(actor), Is.False);
                Assert.That(actor.CurrentStatus.Any(status => ReferenceEquals(status, config.freeStatus)), Is.True);
                Assert.That(actor.CurrentStatus.Any(status => ReferenceEquals(status, config.arrestedStatus)), Is.False);
            }
            Assert.That(actor.P12CrimeJusticeRevision, Is.GreaterThan(0L));
            Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
                    out long afterEscapeEpoch, out ContinuationCensusFailure afterEscapeFailure),
                Is.True, afterEscapeFailure.ToString());
            Assert.That(afterEscapeEpoch, Is.GreaterThan(beforeEscapeEpoch));
            Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalCensusFailure),
                Is.True, finalCensusFailure.ToString());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void DailyV1RestorePreservesSuccessfulScheduledDirectiveEffectWithActiveTravelPartyAcrossBoundaries()
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        NpcActionData arrest = AssetDatabase.LoadAssetAtPath<NpcActionData>(
            "Assets/_Project/Data/Actions/Action-Prender.asset");
        Assert.That(sourceConfig, Is.Not.Null);
        Assert.That(arrest, Is.Not.Null);

        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        NpcSimulationConfig actorConfig = config.Npcs.Single(entry =>
            entry?.npc != null && string.Equals(entry.npc.id, "JOBSON", StringComparison.Ordinal));
        NpcData actorDefinition = actorConfig.npc;
        const int initialSentenceDays = 5;
        const float initialWarrantBounty = 50f;
        NpcActionData escape = SimulationTestFactory.CreateAction(
            "p12g-successful-party-directive-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        escape.canFail = false;
        escape.crimeSettings.escapeBountyPenalty = 7f;
        // The restore/replay regression uses an admitted initial warrant and
        // Core Guard, with autonomous Crime disabled. Scheduled Escape remains
        // runtime-enabled while no independent autonomous Escape can occur on
        // the boundary after the directive succeeds.
        config.crimeAutonomousEnabled = false;
        config.Actions.Add(escape);
        config.InitialWarrants.Clear();
        config.InitialWarrants.Add(new InitialWantedRecordConfig
        {
            target = actorDefinition,
            city = actorConfig.startingCity,
            bounty = initialWarrantBounty,
            sentenceDays = initialSentenceDays
        });
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig
        {
            absoluteDay = 3L,
            mode = ScheduledDirectiveMode.RequestAction,
            operation = ScheduledDirectiveOperation.EscapePrison,
            actor = actorDefinition,
            action = escape
        });

        bool originalArrestCanFail = arrest.canFail;
        // Selected runtimes execute the immutable definition graph captured at
        // bootstrap. Configure successful outcomes before either graph is built.
        GameObject uninterruptedObject = new GameObject("P12-G successful directive-party uninterrupted");
        GameObject restoredObject = new GameObject("P12-G successful directive-party restored");
        try
        {
            arrest.canFail = false;
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

            NpcRuntime controlActor = uninterruptedSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData != null && string.Equals(npc.NpcData.id, actorDefinition.id, StringComparison.Ordinal));
            NpcRuntime sourceActor = sourceSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData != null && string.Equals(npc.NpcData.id, actorDefinition.id, StringComparison.Ordinal));
            Assert.That(controlActor.RuntimeId, Is.EqualTo(sourceActor.RuntimeId));
            Assert.That(controlActor.CurrentCity, Is.Not.Null);
            Assert.That(sourceActor.CurrentCity.RuntimeId, Is.EqualTo(controlActor.CurrentCity.RuntimeId));

            NpcRuntime controlGuard = uninterruptedSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData?.job != null && npc.NpcData.job.jobType == NpcJobType.Guard
                    && ReferenceEquals(npc.CurrentCity, controlActor.CurrentCity));
            NpcRuntime sourceGuard = sourceSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData?.job != null && npc.NpcData.job.jobType == NpcJobType.Guard
                    && ReferenceEquals(npc.CurrentCity, sourceActor.CurrentCity));
            Assert.That(sourceGuard.RuntimeId, Is.EqualTo(controlGuard.RuntimeId));

            foreach (SimulationActiveSession session in new[] { uninterruptedSession, sourceSession })
            {
                foreach (NpcRuntime participant in session.Runtime.Cities
                             .Single(city => city.RuntimeId == controlActor.CurrentCity.RuntimeId).ImportantNpcs)
                {
                    Assert.That(participant?.NpcData, Is.Not.Null);
                    PersonId personId = new PersonId("p12g-party-directive-person-" + participant.NpcData.id);
                    Assert.That(session.Runtime.TryRegisterPerson(
                            new PersonRuntime(personId, session.Runtime.CurrentDay),
                            out PersonStoreFailure registrationFailure), Is.True, registrationFailure.ToString());
                    Assert.That(session.Runtime.TryBindExistingNpcToPerson(
                            personId, participant.RuntimeId,
                            out PersonMaterializationFailure bindingFailure), Is.True, bindingFailure.ToString());
                }
                Assert.That(session.Runtime.TryAssessNpcRosterCensus(
                        out ContinuationCensusFailure censusFailure), Is.True, censusFailure.ToString());
            }

            void AssertBuiltInDecisionConfiguration(SimulationRuntime runtime)
            {
                EffectiveSimulationConfiguration effective = ReadPrivateField<EffectiveSimulationConfiguration>(
                    runtime, "configuration");
                NpcDecisionSystem decisions = ReadPrivateField<NpcDecisionSystem>(runtime, "npcDecisionSystem");
                Assert.That(effective, Is.Not.Null);
                Assert.That(effective.Crime.Enabled, Is.True);
                Assert.That(effective.Crime.AutonomousEnabled, Is.False);
                Assert.That(effective.GuardCrime.Enabled, Is.True);
                Assert.That(decisions, Is.Not.Null);
                Assert.That(decisions.HasProvider<CrimeSystem>(), Is.True);
                Assert.That(decisions.HasProvider<GuardSystem>(), Is.True);
                Assert.That(decisions.GetProviderForAction(escape), Is.TypeOf<CrimeSystem>());
                Assert.That(decisions.GetProviderForAction(arrest), Is.TypeOf<GuardSystem>());
            }

            long ReadMutationEpoch(SimulationRuntime runtime)
            {
                Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
                        out long epoch, out ContinuationCensusFailure failure),
                    Is.True, failure.ToString());
                return epoch;
            }

            JusticeSystem controlJustice = ReadPrivateField<JusticeSystem>(uninterruptedSession.Runtime, "justiceSystem");
            JusticeSystem sourceJustice = ReadPrivateField<JusticeSystem>(sourceSession.Runtime, "justiceSystem");
            Assert.That(controlJustice, Is.Not.Null);
            Assert.That(sourceJustice, Is.Not.Null);

            Assert.That(uninterrupted.Runtime.TryStartTravelParty(
                    CreateDailyTravelPartyContext(uninterruptedSession, controlActor.RuntimeId, controlGuard.RuntimeId)), Is.True,
                "The arrested actor and its local Guard stay available while two other NPCs travel.");
            Assert.That(sourceSession.Runtime.TryStartTravelParty(
                    CreateDailyTravelPartyContext(sourceSession, sourceActor.RuntimeId, sourceGuard.RuntimeId)), Is.True);
            TravelPartyRuntime controlParty = uninterruptedSession.Composition.TravelParties.ActiveParties.Single();
            TravelPartyRuntime sourceParty = sourceSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(controlParty.MemberRuntimeIds, Does.Not.Contain(controlActor.RuntimeId));
            Assert.That(controlParty.MemberRuntimeIds, Does.Not.Contain(controlGuard.RuntimeId));
            Assert.That(sourceParty.MemberRuntimeIds, Does.Not.Contain(sourceActor.RuntimeId));
            Assert.That(sourceParty.MemberRuntimeIds, Does.Not.Contain(sourceGuard.RuntimeId));
            Assert.That(sourceParty.TravelPartyId, Is.EqualTo(controlParty.TravelPartyId));

            // Source, control, and restored hosts use the same effective
            // built-in provider set; P12 compatibility binds that set rather
            // than serializing the provider objects themselves.
            AssertBuiltInDecisionConfiguration(uninterruptedSession.Runtime);
            AssertBuiltInDecisionConfiguration(sourceSession.Runtime);
            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlArrestFailure),
                Is.True, controlArrestFailure.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceArrestFailure),
                Is.True, sourceArrestFailure.ToString());
            Assert.That(controlJustice.IsArrested(controlActor), Is.True,
                "The Core Guard must execute the admitted Arrest action against the initialized warrant.");
            Assert.That(sourceJustice.IsArrested(sourceActor), Is.True);
            Assert.That(controlJustice.WasArrestedToday(controlActor), Is.True);
            Assert.That(sourceJustice.WasArrestedToday(sourceActor), Is.True);
            Assert.That(controlJustice.GetRemainingSentenceDays(controlActor),
                Is.EqualTo(initialSentenceDays));
            Assert.That(sourceJustice.GetRemainingSentenceDays(sourceActor),
                Is.EqualTo(initialSentenceDays));

            Assert.That(uninterruptedSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlResetFailure),
                Is.True, controlResetFailure.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceResetFailure),
                Is.True, sourceResetFailure.ToString());
            Assert.That(controlJustice.WasArrestedToday(controlActor), Is.False,
                "Start-of-day reset clears the prior-day Guard arrest marker before the next sentence decrement.");
            Assert.That(sourceJustice.WasArrestedToday(sourceActor), Is.False);
            Assert.That(controlJustice.HasActiveWarrantInCity(controlActor, controlActor.CurrentCity), Is.True);
            Assert.That(sourceJustice.HasActiveWarrantInCity(sourceActor, sourceActor.CurrentCity), Is.True);
            Assert.That(controlJustice.GetBounty(controlActor, controlActor.CurrentCity),
                Is.EqualTo(initialWarrantBounty));
            Assert.That(sourceJustice.GetBounty(sourceActor, sourceActor.CurrentCity),
                Is.EqualTo(initialWarrantBounty));

            Assert.That(controlJustice.IsArrested(controlActor), Is.True);
            Assert.That(sourceJustice.IsArrested(sourceActor), Is.True);
            Assert.That(sourceJustice.GetRemainingSentenceDays(sourceActor), Is.GreaterThan(2),
                "The day-3 RequestAction must remain eligible after that day's sentence decrement.");

            // Restore at the completed day-2 boundary, with both the sentence and active commitment present.
            Assert.That(controlParty.IsActive, Is.True);
            Assert.That(sourceParty.IsActive, Is.True);
            ScheduledDirective sourceDirective = sourceSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(sourceDirective.AbsoluteDay, Is.EqualTo(3L));
            Assert.That(sourceDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending));
            long sourceDirectiveRevision = sourceSession.Composition.ScheduledDirectives.Revision;
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(sourceSession.Runtime);
            string sourceOwnerGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            string sourceRoots = CaptureContinuationRootFacts(sourceSession, includeWorldIdentity: false);
            Assert.That(sourceOwnerGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));
            Assert.That(sourceRoots, Is.EqualTo(CaptureContinuationRootFacts(
                uninterruptedSession, includeWorldIdentity: false)));

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
            AssertBuiltInDecisionConfiguration(restoredSession.Runtime);
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken), Is.EqualTo(sourceOwnerGraph));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false), Is.EqualTo(sourceRoots));

            NpcRuntime restoredActor = restoredSession.Runtime.NpcRuntimes.Single(
                npc => npc?.NpcData != null && string.Equals(npc.NpcData.id, actorDefinition.id, StringComparison.Ordinal));
            JusticeSystem restoredJustice = ReadPrivateField<JusticeSystem>(restoredSession.Runtime, "justiceSystem");
            Assert.That(restoredJustice.IsArrested(restoredActor), Is.True);
            Assert.That(restoredJustice.GetRemainingSentenceDays(restoredActor),
                Is.EqualTo(controlJustice.GetRemainingSentenceDays(controlActor)));
            ScheduledDirective restoredDirective = restoredSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(restoredDirective.DirectiveId, Is.EqualTo(sourceDirective.DirectiveId));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending));
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision, Is.EqualTo(sourceDirectiveRevision));
            TravelPartyRuntime restoredParty = restoredSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(restoredParty.IsActive, Is.True);
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

            float bountyBeforeEscape = controlJustice.GetBounty(controlActor, controlActor.CurrentCity);
            Assert.That(controlParty.IsActive, Is.True);
            Assert.That(restoredParty.IsActive, Is.True);
            // The target rebuilds a fresh shared epoch on restore; only the
            // subsequent committed mutation delta must match its source run.
            long controlEpochBeforeEscape = ReadMutationEpoch(uninterruptedSession.Runtime);
            long sourceEpochAtRestoreBoundary = ReadMutationEpoch(sourceSession.Runtime);
            long restoredEpochBeforeEscape = ReadMutationEpoch(restoredSession.Runtime);
            Assert.That(sourceEpochAtRestoreBoundary, Is.EqualTo(controlEpochBeforeEscape),
                "The uninterrupted and source runtimes have applied the same pre-restore mutations.");
            Assert.That(sourceEpochAtRestoreBoundary, Is.GreaterThanOrEqualTo(0L));
            Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlEscapeFailure),
                Is.True, controlEscapeFailure.ToString());
            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredEscapeFailure),
                Is.True, restoredEscapeFailure.ToString());
            long controlEpochAfterEscape = ReadMutationEpoch(uninterruptedSession.Runtime);
            long restoredEpochAfterEscape = ReadMutationEpoch(restoredSession.Runtime);
            Assert.That(controlEpochAfterEscape, Is.GreaterThan(controlEpochBeforeEscape));
            Assert.That(restoredEpochAfterEscape, Is.GreaterThan(restoredEpochBeforeEscape));
            Assert.That(restoredEpochAfterEscape - restoredEpochBeforeEscape,
                Is.EqualTo(controlEpochAfterEscape - controlEpochBeforeEscape),
                "The restored runtime applies the same committed owner invalidations through the Escape/Guard boundary.");

            ScheduledDirective controlDirective = uninterruptedSession.Composition.ScheduledDirectives.Directives.Single();
            restoredDirective = restoredSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(controlDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(controlDirective.ProcessedDay, Is.EqualTo(3L));
            Assert.That(restoredDirective.ProcessedDay, Is.EqualTo(controlDirective.ProcessedDay));
            Assert.That(uninterruptedSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(sourceDirectiveRevision + 1L));
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(uninterruptedSession.Composition.ScheduledDirectives.Revision));
            Assert.That(controlJustice.IsArrested(controlActor), Is.True,
                "The ordered Guard turn may re-arrest the actor after the scheduled Escape succeeds in the same daily boundary.");
            Assert.That(restoredJustice.IsArrested(restoredActor), Is.True,
                "Restoration must preserve the same-day ordered Guard response after the successful scheduled Escape.");
            Assert.That(controlJustice.GetBounty(controlActor, controlActor.CurrentCity),
                Is.EqualTo(bountyBeforeEscape + escape.crimeSettings.escapeBountyPenalty));
            float bountyAfterEscape = controlJustice.GetBounty(controlActor, controlActor.CurrentCity);
            Assert.That(restoredJustice.GetBounty(restoredActor, restoredActor.CurrentCity),
                Is.EqualTo(bountyAfterEscape));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    restoredSession, GetCompletedDailyToken(restoredSession.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));

            Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure restoredNextFailure),
                Is.True, restoredNextFailure.ToString());
            Assert.That(controlJustice.GetBounty(controlActor, controlActor.CurrentCity),
                Is.EqualTo(bountyAfterEscape),
                "A terminal successful directive must not apply its Escape effect on a later boundary.");
            Assert.That(restoredJustice.GetBounty(restoredActor, restoredActor.CurrentCity),
                Is.EqualTo(bountyAfterEscape),
                "The restored terminal directive must not replay its Escape effect on a later boundary.");
            Assert.That(controlDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(controlDirective.ProcessedDay, Is.EqualTo(3L));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
            Assert.That(restoredDirective.ProcessedDay, Is.EqualTo(3L));
            Assert.That(uninterruptedSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(sourceDirectiveRevision + 1L));
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(uninterruptedSession.Composition.ScheduledDirectives.Revision));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    restoredSession, GetCompletedDailyToken(restoredSession.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));
        }
        finally
        {
            arrest.canFail = originalArrestCanFail;
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
    public void DailyV1RestorePreservesScheduledDirectiveAndActiveTravelPartyTogether()
    {
        SimulationConfigData sourceConfig = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(sourceConfig, Is.Not.Null);
        SimulationConfigData config = UnityEngine.Object.Instantiate(sourceConfig);
        NpcActionData escapeAction = SimulationTestFactory.CreateAction(
            "p12g-joint-continuation-escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        config.Actions.Add(escapeAction);
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig
        {
            absoluteDay = 5L,
            mode = ScheduledDirectiveMode.RequestAction,
            operation = ScheduledDirectiveOperation.EscapePrison,
            actor = config.Npcs[0].npc,
            action = escapeAction
        });

        GameObject uninterruptedObject = new GameObject("P12-G joint directive-party uninterrupted");
        GameObject restoredObject = new GameObject("P12-G joint directive-party restored");
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

            Assert.That(uninterrupted.Runtime.TryStartTravelParty(
                CreateDailyTravelPartyContext(uninterruptedSession)), Is.True);
            Assert.That(sourceSession.Runtime.TryStartTravelParty(
                CreateDailyTravelPartyContext(sourceSession)), Is.True);
            Assert.That(uninterruptedSession.Composition.TravelParties.ActiveParties, Has.Count.EqualTo(1));
            Assert.That(sourceSession.Composition.TravelParties.ActiveParties, Has.Count.EqualTo(1));

            Assert.That(uninterrupted.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirst),
                Is.True, controlFirst.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceFirst),
                Is.True, sourceFirst.ToString());

            ScheduledDirective sourceDirective = sourceSession.Composition.ScheduledDirectives.Directives.Single();
            TravelPartyRuntime sourceParty = sourceSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(sourceDirective.AbsoluteDay, Is.EqualTo(5L));
            Assert.That(sourceDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending));
            Assert.That(sourceParty.IsActive, Is.True);
            long sourceDirectiveRevision = sourceSession.Composition.ScheduledDirectives.Revision;
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(sourceSession.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                uninterruptedSession, GetCompletedDailyToken(uninterruptedSession.Runtime))));

            string sourceRoots = CaptureContinuationRootFacts(sourceSession, includeWorldIdentity: false);
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
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(
                restored, "activeSession");
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(restoredSession, Is.Not.SameAs(sourceSession));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken), Is.EqualTo(sourceGraph));
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false), Is.EqualTo(sourceRoots));

            ScheduledDirective restoredDirective = restoredSession.Composition.ScheduledDirectives.Directives.Single();
            TravelPartyRuntime restoredParty = restoredSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(restoredDirective.DirectiveId, Is.EqualTo(sourceDirective.DirectiveId));
            Assert.That(restoredDirective.State, Is.EqualTo(ScheduledDirectiveState.Pending),
                "Restoring the future directive must not dispatch or consume it.");
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision, Is.EqualTo(sourceDirectiveRevision));
            Assert.That(restoredParty.TravelPartyId, Is.EqualTo(sourceParty.TravelPartyId));
            Assert.That(restoredParty.TravelerRuntimeIds, Is.EqualTo(sourceParty.TravelerRuntimeIds));
            Assert.That(restoredParty.EscortRuntimeIds, Is.EqualTo(sourceParty.EscortRuntimeIds));
            Assert.That(restoredParty.OriginDecisionId, Is.EqualTo(sourceParty.OriginDecisionId));
            Assert.That(restoredParty.IsActive, Is.True,
                "Restoring the active commitment must not complete or replay its travel effects.");
            Assert.That(restoredParty.MemberRuntimeIds.Select(runtimeId =>
                    restoredSession.IdentityRegistry.TryGetNpc(runtimeId, out NpcRuntime npc)
                        ? npc.MoneyAccount.Balance : float.NaN), Is.EqualTo(memberBalances));
            Assert.That(restoredParty.MemberRuntimeIds.Select(runtimeId =>
                    restoredSession.IdentityRegistry.TryGetNpc(runtimeId, out NpcRuntime npc)
                        ? npc.TravelDaysRemaining : -1), Is.EqualTo(memberProgress));

            for (int day = 2; day <= 5; day++)
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
                    "Directive dispatch and active-party progress must match at every completed boundary.");
            }

            ScheduledDirective controlDirective = uninterruptedSession.Composition.ScheduledDirectives.Directives.Single();
            restoredDirective = restoredSession.Composition.ScheduledDirectives.Directives.Single();
            Assert.That(controlDirective.State, Is.Not.EqualTo(ScheduledDirectiveState.Pending));
            Assert.That(restoredDirective.State, Is.EqualTo(controlDirective.State));
            Assert.That(restoredDirective.ProcessedDay, Is.EqualTo(5L));
            Assert.That(restoredDirective.ProcessedDay, Is.EqualTo(controlDirective.ProcessedDay));
            Assert.That(restoredSession.Composition.ScheduledDirectives.Revision,
                Is.EqualTo(uninterruptedSession.Composition.ScheduledDirectives.Revision));
            Assert.That(uninterruptedSession.Composition.TravelParties.ActiveParties, Is.Empty,
                "The active TravelParty must arrive on the same boundary as the uninterrupted control.");
            Assert.That(restoredSession.Composition.TravelParties.ActiveParties, Is.Empty);
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(uninterruptedObject);
            UnityEngine.Object.DestroyImmediate(restoredObject);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
    [Test]
    public void DailyV1RestoreRetainsTerminalSuccessfulActorChoiceWithActiveTravelParty()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        string actionDefinitionId = config.Actions.Single(
            action => action != null && action.actionType == NpcActionType.SellGoods).DefinitionId;
        const string commandId = "p12g-successful-actor-choice-active-party";
        PersonId personId = new PersonId("p12g-terminal-actor-" + commandId);

        GameObject uninterruptedObject = new GameObject("P12-G ActorChoice-party uninterrupted");
        GameObject restoredObject = new GameObject("P12-G ActorChoice-party restored");
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

            CaptureTerminalP11History(uninterruptedSession.Runtime, commandId, actionDefinitionId,
                bindDailyMerchant: true);
            CaptureTerminalP11History(sourceSession.Runtime, commandId, actionDefinitionId,
                bindDailyMerchant: true);
            NpcRuntime uninterruptedMerchant = uninterruptedSession.Runtime.NpcRuntimes.Single(
                npc => npc != null && npc.PersonId == personId);
            NpcRuntime sourceMerchant = sourceSession.Runtime.NpcRuntimes.Single(
                npc => npc != null && npc.PersonId == personId);

            Assert.That(uninterrupted.Runtime.TryStartTravelParty(
                    CreateDailyTravelPartyContext(uninterruptedSession, uninterruptedMerchant.RuntimeId)),
                Is.True, "A distinct pair of authored NPCs should form a normal active party.");
            Assert.That(sourceSession.Runtime.TryStartTravelParty(
                    CreateDailyTravelPartyContext(sourceSession, sourceMerchant.RuntimeId)), Is.True);

            Assert.That(uninterrupted.Runtime.TryAdvanceDay(
                    out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            Assert.That(sourceSession.Runtime.TryAdvanceDay(
                    out SimulationRuntimeAdvanceFailure sourceFirstFailure),
                Is.True, sourceFirstFailure.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(sourceSession.Runtime);
            DailyCaptureEligibilityToken controlToken = GetCompletedDailyToken(uninterruptedSession.Runtime);

            ActorChoiceInput sourceChoice = sourceSession.Runtime.ActorChoiceStore.Inputs.Single(
                input => input.WorldCommandId == commandId);
            ActorChoiceInput controlChoice = uninterruptedSession.Runtime.ActorChoiceStore.Inputs.Single(
                input => input.WorldCommandId == commandId);
            Assert.That(sourceChoice.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
            Assert.That(sourceChoice.Dispositions, Has.Count.EqualTo(2));
            Assert.That(sourceChoice.Dispositions[1].AttemptOutcome,
                Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
            Assert.That(controlChoice.Status, Is.EqualTo(sourceChoice.Status));
            Assert.That(sourceChoice.PersonId, Is.EqualTo(personId));

            TravelPartyRuntime sourceParty = sourceSession.Composition.TravelParties.ActiveParties.Single();
            TravelPartyRuntime controlParty = uninterruptedSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(sourceParty.IsActive, Is.True);
            Assert.That(sourceParty.MemberRuntimeIds, Does.Not.Contain(sourceMerchant.RuntimeId),
                "The proving party is independent from the merchant performing SellGoods.");
            Assert.That(sourceParty.TravelPartyId, Is.EqualTo(controlParty.TravelPartyId));
            Assert.That(sourceParty.MemberRuntimeIds, Is.EqualTo(controlParty.MemberRuntimeIds));

            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(sourceSession, sourceToken);
            string sourceRoots = CaptureContinuationRootFacts(sourceSession, includeWorldIdentity: false);
            Assert.That(sourceGraph, Is.EqualTo(
                CaptureCompleteDailyV1OwnerProjection(uninterruptedSession, controlToken)));
            Assert.That(sourceRoots, Is.EqualTo(
                CaptureContinuationRootFacts(uninterruptedSession, includeWorldIdentity: false)));

            Assert.That(restored.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure restoreFailure,
                    out string restoreDiagnostic), Is.True,
                restoreFailure + ": " + restoreDiagnostic);
            SimulationActiveSession restoredSession = ReadPrivateField<SimulationActiveSession>(
                restored, "activeSession");
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restoredSession.Runtime);
            Assert.That(restoredSession, Is.Not.SameAs(sourceSession));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken),
                Is.EqualTo(sourceGraph),
                "Restoring both owners must preserve the successful terminal choice and active-party graph without replay.");
            Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                Is.EqualTo(sourceRoots));
            AssertOwnerVectorFactsMatch(sourceToken.OwnerSections, restoredToken.OwnerSections,
                requireFreshOwners: true);

            ActorChoiceInput restoredChoice = restoredSession.Runtime.ActorChoiceStore.Inputs.Single(
                input => input.WorldCommandId == commandId);
            Assert.That(restoredChoice.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
            Assert.That(restoredChoice.Dispositions[1].AttemptOutcome,
                Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
            TravelPartyRuntime restoredParty = restoredSession.Composition.TravelParties.ActiveParties.Single();
            Assert.That(restoredParty.TravelPartyId, Is.EqualTo(sourceParty.TravelPartyId));
            Assert.That(restoredParty.MemberRuntimeIds, Is.EqualTo(sourceParty.MemberRuntimeIds));
            Assert.That(restoredParty.TravelerRuntimeIds, Is.EqualTo(sourceParty.TravelerRuntimeIds));
            Assert.That(restoredParty.OriginDecisionId, Is.EqualTo(sourceParty.OriginDecisionId));

            long choiceRevisionBeforeDuplicate = restoredSession.Runtime.ActorChoiceStore.CensusRevision;
            long choiceSequenceBeforeDuplicate = restoredSession.Runtime.ActorChoiceStore.NextInputSequence;
            int choiceCountBeforeDuplicate = restoredSession.Runtime.ActorChoiceStore.Count;
            string graphBeforeDuplicate = CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken);
            string rootsBeforeDuplicate = CaptureContinuationRootFacts(restoredSession);
            WorldCommandHandlerResult duplicateResult = new ActorActionChoiceWorldCommandHandler(
                    restoredSession.Runtime)
                .Execute(
                    new WorldCommand(
                        WorldCommandKind.ActorActionChoice,
                        WorldCommandOrigin.LocalPlayer,
                        WorldCommandAuthorityMode.Request,
                        new ActorActionChoiceWorldCommandPayload(personId, actionDefinitionId)),
                    new WorldCommandExecutionContext(commandId));
            Assert.That(duplicateResult.Success, Is.False);
            Assert.That(duplicateResult.Diagnostic,
                Does.Contain(ActorChoiceStoreFailureCode.DuplicateWorldCommandId.ToString()));
            Assert.That(restoredSession.Runtime.ActorChoiceStore.CensusRevision,
                Is.EqualTo(choiceRevisionBeforeDuplicate));
            Assert.That(restoredSession.Runtime.ActorChoiceStore.NextInputSequence,
                Is.EqualTo(choiceSequenceBeforeDuplicate));
            Assert.That(restoredSession.Runtime.ActorChoiceStore.Count, Is.EqualTo(choiceCountBeforeDuplicate));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, restoredToken),
                Is.EqualTo(graphBeforeDuplicate),
                "A duplicate terminal command must not repeat SellGoods or alter the active party.");
            Assert.That(CaptureContinuationRootFacts(restoredSession), Is.EqualTo(rootsBeforeDuplicate));

            for (int boundary = 0; boundary < 4; boundary++)
            {
                Assert.That(uninterrupted.Runtime.TryAdvanceDay(
                        out SimulationRuntimeAdvanceFailure controlFailure),
                    Is.True, controlFailure.ToString());
                Assert.That(restored.Runtime.TryAdvanceDay(
                        out SimulationRuntimeAdvanceFailure restoredFailure),
                    Is.True, restoredFailure.ToString());
                DailyCaptureEligibilityToken nextControlToken = GetCompletedDailyToken(
                    uninterruptedSession.Runtime);
                restoredSession = ReadPrivateField<SimulationActiveSession>(restored, "activeSession");
                DailyCaptureEligibilityToken nextRestoredToken = GetCompletedDailyToken(restoredSession.Runtime);
                Assert.That(nextRestoredToken.AbsoluteDay, Is.EqualTo(nextControlToken.AbsoluteDay));
                if (boundary < 2)
                {
                    Assert.That(uninterruptedSession.Composition.TravelParties.ActiveParties,
                        Has.Count.EqualTo(1),
                        "The authored route must remain active across multiple post-restore boundaries.");
                    Assert.That(restoredSession.Composition.TravelParties.ActiveParties,
                        Has.Count.EqualTo(1));
                }
                Assert.That(CaptureCompleteDailyV1OwnerProjection(restoredSession, nextRestoredToken),
                    Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                        uninterruptedSession, nextControlToken)),
                    "All included owners must remain in parity through active-party travel and arrival.");
                Assert.That(CaptureContinuationRootFacts(restoredSession, includeWorldIdentity: false),
                    Is.EqualTo(CaptureContinuationRootFacts(
                        uninterruptedSession, includeWorldIdentity: false)));
            }

            Assert.That(uninterruptedSession.Composition.TravelParties.ActiveParties, Is.Empty);
            Assert.That(restoredSession.Composition.TravelParties.ActiveParties, Is.Empty);
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
                object selectedOwners = ReadPrivateField<object>(member, "selectedRuntimeOwners");
                object selectedTravelOwner = ReadPrivateField<object>(selectedOwners, "Travel");
                WritePrivateField(selectedTravelOwner, "<ActiveTravelPartyId>k__BackingField", "p12g/missing-party");
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
    public void DailyV1RestoreRejectsPrivateTargetRosterMutationAfterOwnerVectorCaptureAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G target roster mutation after initial census");
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
                        if (stage != P12GDailyV1RestoreStage.TargetOwnerVectorCaptured) return;
                        mutationInjected = true;
                        Assert.That(privateCandidate, Is.Not.Null,
                            "The private candidate must exist before the initial target-vector checks.");
                        Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.False,
                            "The candidate remains private and unbound while target validation runs.");
                        string lateNpcId = privateCandidate.Composition.RuntimeIdAllocator.AllocateNpcId();
                        NpcRuntime lateNpc = new NpcRuntime(
                            lateNpcId,
                            SimulationTestFactory.CreateNpc("p12g-private-target-precheck-late-npc"));
                        Assert.That(privateCandidate.Runtime.TryRegisterNpc(
                                lateNpc, out WorldNpcRegistryFailure registerFailure),
                            Is.True, registerFailure.ToString());
                        targetRosterMutationApplied = true;
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(mutationInjected, Is.True);
            Assert.That(targetRosterMutationApplied, Is.True);
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed), diagnostic);
            Assert.That(diagnostic, Does.Contain("owner vector changed during mutation-guard binding"));
            Assert.That(restored, Is.Null,
                "A target roster change after initial census must not be admitted or published.");
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
                Is.EqualTo(sourceGraph),
                "A rejected target-census mutation must not affect a later valid restore.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPostGraphPersonPositionMutationAfterGuardBindingAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G post-graph Person position target mutation");
        GameObject controlObject = new GameObject("P12-G post-graph Person position control");
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

            PersonId personId = new PersonId("p12g-post-graph-person-position");
            Assert.That(original.Runtime.TryRegisterPerson(new PersonRuntime(personId, 0L),
                    out PersonStoreFailure sourcePersonFailure),
                Is.True, sourcePersonFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(new PersonRuntime(personId, 0L),
                    out PersonStoreFailure controlPersonFailure),
                Is.True, controlPersonFailure.ToString());
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime)),
                Is.EqualTo(sourceGraph));

            SimulationActiveSession privateCandidate = null;
            bool mutationInjected = false;
            bool targetPositionApplied = false;
            long targetPositionRevisionBefore = -1L;
            long targetPositionRevisionAfter = -1L;
            long targetMutationEpochBefore = -1L;
            long targetMutationEpochAfter = -1L;
            bool targetVectorRecaptured = false;
            bool targetBoundaryAdmitted = false;
            bool publicationReached = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage == P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured)
                            targetVectorRecaptured = true;
                        if (stage == P12GDailyV1RestoreStage.BoundaryAdmitted)
                            targetBoundaryAdmitted = true;
                        if (stage == P12GDailyV1RestoreStage.BeforePublication)
                            publicationReached = true;
                        if (stage != P12GDailyV1RestoreStage.CandidateGuardBound) return;
                        mutationInjected = true;
                        Assert.That(privateCandidate, Is.Not.Null);
                        Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.True,
                            "This mutation must occur after the candidate has completed cross-owner graph checks and guard binding.");
                        PersonSpatialPositionStore positions = privateCandidate.Runtime.PersonSpatialPositionStore;
                        Assert.That(positions, Is.Not.Null);
                        Assert.That(positions.Count, Is.Zero,
                            "Daily-v1 starts with an exact-empty P8-C Person position owner.");
                        PersonSpatialPositionCensusProvider provider =
                            new PersonSpatialPositionCensusProvider(positions);
                        OwnerSectionCensusWitness beforeWitness = provider.GetCurrentCensus();
                        Assert.That(beforeWitness.SectionId, Is.EqualTo(PersonSpatialPositionCensusProvider.SectionId));
                        Assert.That(beforeWitness.Cardinality, Is.Zero);
                        targetPositionRevisionBefore = positions.Revision;
                        Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochBefore, out ContinuationCensusFailure epochBeforeFailure),
                            Is.True, epochBeforeFailure.ToString());
                        LocationId locationId = privateCandidate.Runtime.SpatialAuthorityStore.Locations.Single().Id;
                        targetPositionApplied = positions.TrySetAt(
                            personId,
                            StablePositionReference.ForLocation(locationId),
                            out PersonSpatialPositionFailure positionFailure);
                        Assert.That(targetPositionApplied, Is.True, positionFailure.ToString());
                        targetPositionRevisionAfter = positions.Revision;
                        OwnerSectionCensusWitness afterWitness = provider.GetCurrentCensus();
                        Assert.That(afterWitness.SectionId, Is.EqualTo(beforeWitness.SectionId));
                        Assert.That(afterWitness.SchemaVersion, Is.EqualTo(beforeWitness.SchemaVersion));
                        Assert.That(afterWitness.OwnerInstanceIdentity, Is.SameAs(beforeWitness.OwnerInstanceIdentity));
                        Assert.That(afterWitness.Cardinality, Is.EqualTo(beforeWitness.Cardinality + 1));
                        Assert.That(afterWitness.Revision, Is.EqualTo(beforeWitness.Revision + 1L));
                        Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochAfter, out ContinuationCensusFailure epochAfterFailure),
                            Is.True, epochAfterFailure.ToString());
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(mutationInjected, Is.True);
            Assert.That(targetPositionApplied, Is.True);
            Assert.That(targetPositionRevisionAfter, Is.EqualTo(targetPositionRevisionBefore + 1L));
            Assert.That(targetMutationEpochAfter, Is.EqualTo(targetMutationEpochBefore),
                "This P8-C local owner write changes its revision without changing the observed shared census epoch.");
            Assert.That(privateCandidate.Runtime.PersonSpatialPositionStore.Count, Is.EqualTo(1));
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed), diagnostic);
            Assert.That(diagnostic, Does.Contain("OwnerCoverageIncomplete"));
            Assert.That(targetVectorRecaptured, Is.False,
                "The final census must reject the populated exact-empty owner before vector recapture completes.");
            Assert.That(targetBoundaryAdmitted, Is.False);
            Assert.That(publicationReached, Is.False);
            Assert.That(restored, Is.Null,
                "A valid P8-C writer after guard binding must be rejected by final target-owner recapture before publication.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out sourceAdvance), Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out controlAdvance), Is.True, controlAdvance.ToString());
            string continuedGraph = CaptureCompleteDailyV1OwnerProjection(
                original, GetCompletedDailyToken(original.Runtime));
            Assert.That(continuedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(continuedGraph),
                "The rejected private target mutation must not affect a later valid restore.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
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
    [TestCase("p8c-city-location-binding-owner")]
    [TestCase("p8c-city-location-binding-populated-target")]
    [TestCase("p8c-person-position-owner")]
    [TestCase("p8c-person-position-populated-target")]
    [TestCase("p8d-route-observation-populated-target")]
    [TestCase("p8d-route-observation-owner")]
    [TestCase("p8d-person-route-plan-owner")]
    [TestCase("p12f-expedition-owner")]
    [TestCase("p12f-decision-occurrence-receipt-owner")]
    [TestCase("p12e-economy-keyed-sale-receipt-owner")]
    [TestCase("p12f-actor-choice-temporal-owner")]
    [TestCase("p18-crime-receipt-owner")]
    [TestCase("p18-justice-receipt-owner")]
    [TestCase("p18-npc-local-observation-receipt-owner")]
    [TestCase("p18-npc-merchant-trade-state-receipt-owner")]
    [TestCase("genealogy-dangling-person-endpoint")]
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
            LegacySpatialAnchorBindingStore p8CReplacementStore = null;
            bool p8CStoreReplaced = false;
            LegacySpatialAnchorBindingStore p8CPopulatedTargetStore = null;
            string p8CPopulatedTargetCityId = null;
            LocationId p8CPopulatedTargetLocationId = null;
            bool p8CPopulatedTargetBindingApplied = false;
            string p8CPopulatedTargetBindingFailure = null;
            OwnerSectionCensusWitness p8CPopulatedTargetWitness = null;
            PersonSpatialPositionStore p8CPositionReplacementStore = null;
            bool p8CPositionStoreReplaced = false;
            PersonId p8CPopulatedPositionPersonId = null;
            PersonSpatialPositionStore p8CPopulatedPositionStore = null;
            LocationId p8CPopulatedPositionLocationId = null;
            bool p8CPopulatedPositionApplied = false;
            string p8CPopulatedPositionFailure = null;
            OwnerSectionCensusWitness p8CPopulatedPositionWitness = null;
            PersonId p8DPopulatedObservationPersonId = null;
            SpatialRouteKnowledgeStore p8DPopulatedObservationStore = null;
            LocationId p8DPopulatedObservationLocationId = null;
            bool p8DPopulatedObservationApplied = false;
            string p8DPopulatedObservationFailure = null;
            OwnerSectionCensusWitness p8DPopulatedObservationWitness = null;
            SpatialRouteKnowledgeStore p8DRouteKnowledgeReplacementStore = null;
            bool p8DRouteKnowledgeStoreReplaced = false;
            PersonRoutePlanStore p8DRoutePlanReplacementStore = null;
            bool p8DRoutePlanStoreReplaced = false;
            ExpeditionStore originalExpeditionStore = null;
            ExpeditionStore expeditionReplacementStore = null;
            bool expeditionCompositionOwnerReplaced = false;
            bool expeditionVectorStillIdentifiesRuntimeOwner = false;
            NpcDecisionRecorder originalDecisionRecorderOwner = null;
            NpcDecisionRecorder decisionRecorderReplacementOwner = null;
            bool decisionRecorderCompositionOwnerReplaced = false;
            bool decisionRecorderVectorStillIdentifiesRuntimeOwner = false;
            EconomyTransactionService originalEconomyReceiptOwner = null;
            EconomyTransactionService economyReceiptReplacementOwner = null;
            bool economyReceiptCompositionOwnerReplaced = false;
            bool economyReceiptVectorStillIdentifiesRuntimeOwner = false;
            IOwnerSectionCensusProvider sourceActorChoiceTemporalProvider = null;
            bool actorChoiceTemporalCompositionProviderReplaced = false;
            bool actorChoiceTemporalVectorStillIdentifiesRuntimeOwner = false;
            long actorChoiceTemporalCapturedRevision = -1L;
            CrimeSystem targetCrimeReceiptOwner = null;
            CrimeSystem sourceCrimeReceiptOwner = null;
            bool crimeReceiptRuntimeOwnerReplaced = false;
            bool crimeReceiptVectorStillIdentifiesTarget = false;
            JusticeSystem targetJusticeReceiptOwner = null;
            JusticeSystem sourceJusticeReceiptOwner = null;
            bool justiceReceiptRuntimeOwnerReplaced = false;
            bool justiceReceiptVectorStillIdentifiesTarget = false;
            IOwnerSectionCensusProvider[] sourceNpcReceiptProviders = null;
            IOwnerSectionCensusProvider[] replacementNpcReceiptProviders = null;
            bool npcReceiptCompositionProvidersReplaced = false;
            bool npcReceiptVectorStillIdentifiesTarget = false;
            string replacedNpcReceiptSectionPrefix = null;
            PersonId genealogyExistingEndpoint = null;
            PersonId genealogyMissingEndpoint = null;
            GenealogyStore corruptedGenealogyStore = null;
            bool genealogyEdgeAdded = false;
            if (corruptionKind == "p8c-person-position-populated-target")
            {
                p8CPopulatedPositionPersonId = new PersonId("p12g-p8c-person-position");
                Assert.That(original.Runtime.TryRegisterPerson(
                    new PersonRuntime(p8CPopulatedPositionPersonId, 0L),
                    out PersonStoreFailure sourcePersonFailure), Is.True, sourcePersonFailure.ToString());
                Assert.That(controlSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(p8CPopulatedPositionPersonId, 0L),
                    out PersonStoreFailure controlPersonFailure), Is.True, controlPersonFailure.ToString());
            }
            if (corruptionKind == "p8d-route-observation-populated-target")
            {
                p8DPopulatedObservationPersonId = new PersonId("p12g-p8d-route-observation");
                Assert.That(original.Runtime.TryRegisterPerson(
                    new PersonRuntime(p8DPopulatedObservationPersonId, 0L),
                    out PersonStoreFailure sourcePersonFailure), Is.True, sourcePersonFailure.ToString());
                Assert.That(controlSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(p8DPopulatedObservationPersonId, 0L),
                    out PersonStoreFailure controlPersonFailure), Is.True, controlPersonFailure.ToString());
            }
            if (corruptionKind == "genealogy-dangling-person-endpoint")
            {
                genealogyExistingEndpoint = new PersonId("p12g-existing-genealogy-endpoint");
                Assert.That(original.Runtime.TryRegisterPerson(
                    new PersonRuntime(genealogyExistingEndpoint, 0L),
                    out PersonStoreFailure sourcePersonFailure), Is.True, sourcePersonFailure.ToString());
                Assert.That(controlSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(genealogyExistingEndpoint, 0L),
                    out PersonStoreFailure controlPersonFailure), Is.True, controlPersonFailure.ToString());
            }
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
                    case "p8c-city-location-binding-owner":
                        p8CReplacementStore = new LegacySpatialAnchorBindingStore(
                            candidate.Runtime.SpatialAuthorityStore);
                        FieldInfo p8CBindingField = candidate.Runtime.GetType().GetField(
                            "legacySpatialAnchorBindingStore",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        if (p8CBindingField != null)
                        {
                            p8CBindingField.SetValue(candidate.Runtime, p8CReplacementStore);
                            p8CStoreReplaced = ReferenceEquals(
                                candidate.Runtime.LegacySpatialAnchorBindingStore,
                                p8CReplacementStore);
                        }
                        break;
                    case "p8c-city-location-binding-populated-target":
                    {
                        p8CPopulatedTargetStore = candidate.Runtime.LegacySpatialAnchorBindingStore;
                        Assert.That(p8CPopulatedTargetStore, Is.Not.Null);
                        Assert.That(p8CPopulatedTargetStore.Count, Is.Zero);
                        CityRuntime stagedCity = candidate.Runtime.Cities
                            .OrderBy(city => city.RuntimeId, StringComparer.Ordinal)
                            .First();
                        p8CPopulatedTargetCityId = stagedCity.RuntimeId;
                        p8CPopulatedTargetLocationId = candidate.Runtime.SpatialAuthorityStore.Locations.Single().Id;
                        p8CPopulatedTargetBindingApplied = p8CPopulatedTargetStore.TryBindCity(
                            p8CPopulatedTargetCityId,
                            p8CPopulatedTargetLocationId,
                            out SpatialAnchorBindingFailure p8CBindingFailure);
                        p8CPopulatedTargetBindingFailure = p8CBindingFailure.ToString();
                        if (p8CPopulatedTargetBindingApplied)
                        {
                            p8CPopulatedTargetWitness = new LegacySpatialAnchorBindingCensusProvider(
                                p8CPopulatedTargetStore).GetCurrentCensus();
                        }
                        break;
                    }
                    case "p8c-person-position-owner":
                        p8CPositionReplacementStore = new PersonSpatialPositionStore(
                            candidate.Runtime.PersonStore,
                            candidate.Runtime.SpatialAuthorityStore,
                            new SpatialPassageTraversalOptionResolver(
                                candidate.Runtime.SpatialAuthorityStore.PassageAuthority));
                        FieldInfo p8CPositionField = candidate.Runtime.GetType().GetField(
                            "personSpatialPositionStore",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        if (p8CPositionField != null)
                        {
                            p8CPositionField.SetValue(candidate.Runtime, p8CPositionReplacementStore);
                            p8CPositionStoreReplaced = ReferenceEquals(
                                candidate.Runtime.PersonSpatialPositionStore,
                                p8CPositionReplacementStore);
                        }
                        break;
                    case "p8c-person-position-populated-target":
                    {
                        p8CPopulatedPositionStore = candidate.Runtime.PersonSpatialPositionStore;
                        Assert.That(p8CPopulatedPositionStore, Is.Not.Null);
                        Assert.That(p8CPopulatedPositionStore.Count, Is.Zero);
                        p8CPopulatedPositionLocationId = candidate.Runtime.SpatialAuthorityStore.Locations.Single().Id;
                        p8CPopulatedPositionApplied = p8CPopulatedPositionStore.TrySetAt(
                            p8CPopulatedPositionPersonId,
                            StablePositionReference.ForLocation(p8CPopulatedPositionLocationId),
                            out PersonSpatialPositionFailure p8CPositionFailure);
                        p8CPopulatedPositionFailure = p8CPositionFailure.ToString();
                        if (p8CPopulatedPositionApplied)
                        {
                            p8CPopulatedPositionWitness = new PersonSpatialPositionCensusProvider(
                                p8CPopulatedPositionStore).GetCurrentCensus();
                        }
                        break;
                    }
                    case "p8d-route-observation-populated-target":
                    {
                        p8DPopulatedObservationStore = candidate.Runtime.SpatialRouteKnowledgeStore;
                        Assert.That(p8DPopulatedObservationStore, Is.Not.Null);
                        Assert.That(p8DPopulatedObservationStore.ObservationCount, Is.Zero);
                        p8DPopulatedObservationLocationId =
                            candidate.Runtime.SpatialAuthorityStore.Locations.Single().Id;
                        SpatialObservation observation = new SpatialObservation(
                            SpatialSubject.ForLocation(p8DPopulatedObservationLocationId),
                            SpatialObservationValue.ForEntityBelief(SpatialEntityBelief.KnownPresent),
                            new SpatialObservationProvenance(
                                SpatialObservationSourceKind.DirectObservation,
                                "p12g-p8d-target-observation",
                                "p12g-p8d-target-observation-origin"),
                            sourceToken.AbsoluteDay,
                            sourceToken.AbsoluteDay,
                            1000,
                            "p12g-p8d-location-presence-v1");
                        p8DPopulatedObservationApplied =
                            p8DPopulatedObservationStore.TryRecordObservation(
                                p8DPopulatedObservationPersonId,
                                observation,
                                sourceToken.AbsoluteDay,
                                out SpatialKnowledgeFailure p8DObservationFailure);
                        p8DPopulatedObservationFailure = p8DObservationFailure.ToString();
                        if (p8DPopulatedObservationApplied)
                        {
                            p8DPopulatedObservationWitness =
                                new SpatialRouteObservationCensusProvider(
                                    p8DPopulatedObservationStore).GetCurrentCensus();
                        }
                        break;
                    }
                    case "p8d-route-observation-owner":
                        p8DRouteKnowledgeReplacementStore = new SpatialRouteKnowledgeStore(
                            candidate.Runtime.PersonStore);
                        FieldInfo p8DRouteKnowledgeField = candidate.Runtime.GetType().GetField(
                            "spatialRouteKnowledgeStore",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        if (p8DRouteKnowledgeField != null)
                        {
                            p8DRouteKnowledgeField.SetValue(candidate.Runtime, p8DRouteKnowledgeReplacementStore);
                            p8DRouteKnowledgeStoreReplaced = ReferenceEquals(
                                candidate.Runtime.SpatialRouteKnowledgeStore,
                                p8DRouteKnowledgeReplacementStore);
                        }
                        break;
                    case "p8d-person-route-plan-owner":
                        p8DRoutePlanReplacementStore = new PersonRoutePlanStore(
                            candidate.Runtime.PersonStore,
                            candidate.Runtime.SpatialRouteKnowledgeStore);
                        FieldInfo p8DRoutePlanField = candidate.Runtime.GetType().GetField(
                            "personRoutePlanStore",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        if (p8DRoutePlanField != null)
                        {
                            p8DRoutePlanField.SetValue(candidate.Runtime, p8DRoutePlanReplacementStore);
                            p8DRoutePlanStoreReplaced = ReferenceEquals(
                                candidate.Runtime.PersonRoutePlanStore,
                                p8DRoutePlanReplacementStore);
                        }
                        break;
                    case "p12f-expedition-owner":
                    {
                        originalExpeditionStore = candidate.Composition.Expeditions;
                        expeditionReplacementStore = new ExpeditionStore();
                        WritePrivateField(
                            candidate.Composition,
                            "<Expeditions>k__BackingField",
                            expeditionReplacementStore);
                        expeditionCompositionOwnerReplaced = ReferenceEquals(
                            candidate.Composition.Expeditions,
                            expeditionReplacementStore);
                        Assert.That(originalExpeditionStore, Is.Not.Null);
                        Assert.That(expeditionCompositionOwnerReplaced, Is.True);
                        Assert.That(expeditionReplacementStore.ActiveExpeditions, Is.Empty);
                        Assert.That(expeditionReplacementStore.Revision, Is.Zero);
                        Assert.That(candidate.Runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                            out IReadOnlyList<OwnerSectionCensusSnapshot> preValidationTargetRows,
                            out _,
                            out ContinuationCensusFailure targetCensusFailure), Is.True,
                            targetCensusFailure.ToString());
                        OwnerSectionCensusSnapshot expeditionRow = preValidationTargetRows.Single(
                            row => string.Equals(
                                row.SectionId,
                                ExpeditionCensusProvider.SectionId,
                                StringComparison.Ordinal));
                        expeditionVectorStillIdentifiesRuntimeOwner = ReferenceEquals(
                            expeditionRow.OwnerInstanceIdentity,
                            originalExpeditionStore)
                            && !ReferenceEquals(
                                expeditionRow.OwnerInstanceIdentity,
                                expeditionReplacementStore);
                        break;
                    }
                    case "p12f-decision-occurrence-receipt-owner":
                    {
                        OwnerSectionCensusWitness sourceReceipt =
                            original.Composition.GetNpcDecisionOccurrenceReceiptCensus();
                        Assert.That(sourceReceipt.Cardinality, Is.Zero);
                        Assert.That(sourceReceipt.Revision, Is.Zero);
                        originalDecisionRecorderOwner = ReadPrivateField<NpcDecisionRecorder>(
                            original.Composition, "decisionRecorder");

                        OwnerSectionCensusWitness candidateReceipt =
                            candidate.Composition.GetNpcDecisionOccurrenceReceiptCensus();
                        Assert.That(candidateReceipt.Cardinality, Is.Zero);
                        Assert.That(candidateReceipt.Revision, Is.Zero);
                        decisionRecorderReplacementOwner = ReadPrivateField<NpcDecisionRecorder>(
                            candidate.Composition, "decisionRecorder");
                        Assert.That(decisionRecorderReplacementOwner,
                            Is.Not.SameAs(originalDecisionRecorderOwner));
                        WritePrivateField(
                            candidate.Composition,
                            "decisionRecorder",
                            originalDecisionRecorderOwner);
                        decisionRecorderCompositionOwnerReplaced = ReferenceEquals(
                            candidate.Composition.GetNpcDecisionOccurrenceReceiptCensus().OwnerInstanceIdentity,
                            sourceReceipt.OwnerInstanceIdentity);

                        Assert.That(candidate.Runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                            out IReadOnlyList<OwnerSectionCensusSnapshot> decisionTargetRows,
                            out _,
                            out ContinuationCensusFailure decisionTargetFailure), Is.True,
                            decisionTargetFailure.ToString());
                        OwnerSectionCensusSnapshot decisionTargetRow = decisionTargetRows.Single(
                            row => string.Equals(
                                row.SectionId,
                                NpcDecisionRecorder.OccurrenceReceiptSectionId,
                                StringComparison.Ordinal));
                        decisionRecorderVectorStillIdentifiesRuntimeOwner = ReferenceEquals(
                            decisionTargetRow.OwnerInstanceIdentity,
                            candidateReceipt.OwnerInstanceIdentity)
                            && !ReferenceEquals(
                                decisionTargetRow.OwnerInstanceIdentity,
                                sourceReceipt.OwnerInstanceIdentity)
                            && decisionTargetRow.Cardinality == 0
                            && decisionTargetRow.Revision == 0L;
                        break;
                    }
                    case "p12e-economy-keyed-sale-receipt-owner":
                    {
                        OwnerSectionCensusWitness sourceReceipt =
                            original.Composition.GetEconomyKeyedSaleReceiptCensus();
                        Assert.That(sourceReceipt.Cardinality, Is.Zero);
                        Assert.That(sourceReceipt.Revision, Is.Zero);
                        originalEconomyReceiptOwner = ReadPrivateField<EconomyTransactionService>(
                            original.Composition, "economyTransactionService");

                        OwnerSectionCensusWitness candidateReceipt =
                            candidate.Composition.GetEconomyKeyedSaleReceiptCensus();
                        Assert.That(candidateReceipt.Cardinality, Is.Zero);
                        Assert.That(candidateReceipt.Revision, Is.Zero);
                        economyReceiptReplacementOwner = ReadPrivateField<EconomyTransactionService>(
                            candidate.Composition, "economyTransactionService");
                        Assert.That(economyReceiptReplacementOwner,
                            Is.Not.SameAs(originalEconomyReceiptOwner));
                        WritePrivateField(
                            candidate.Composition,
                            "economyTransactionService",
                            originalEconomyReceiptOwner);
                        economyReceiptCompositionOwnerReplaced = ReferenceEquals(
                            candidate.Composition.GetEconomyKeyedSaleReceiptCensus().OwnerInstanceIdentity,
                            sourceReceipt.OwnerInstanceIdentity);

                        Assert.That(candidate.Runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                            out IReadOnlyList<OwnerSectionCensusSnapshot> economyTargetRows,
                            out _,
                            out ContinuationCensusFailure economyTargetFailure), Is.True,
                            economyTargetFailure.ToString());
                        OwnerSectionCensusSnapshot economyTargetRow = economyTargetRows.Single(
                            row => string.Equals(
                                row.SectionId,
                                EconomyTransactionService.KeyedSaleReceiptSectionId,
                                StringComparison.Ordinal));
                        economyReceiptVectorStillIdentifiesRuntimeOwner = ReferenceEquals(
                            economyTargetRow.OwnerInstanceIdentity,
                            candidateReceipt.OwnerInstanceIdentity)
                            && !ReferenceEquals(
                                economyTargetRow.OwnerInstanceIdentity,
                                sourceReceipt.OwnerInstanceIdentity)
                            && economyTargetRow.Cardinality == 0
                            && economyTargetRow.Revision == 0L;
                        break;
                    }
                    case "p12f-actor-choice-temporal-owner":
                    {
                        sourceActorChoiceTemporalProvider =
                            original.Composition.ActorChoiceTemporalCensusProvider;
                        OwnerSectionCensusWitness sourceTemporal =
                            sourceActorChoiceTemporalProvider.GetCurrentCensus();
                        Assert.That(sourceTemporal.Cardinality, Is.Zero);

                        OwnerSectionCensusWitness candidateTemporal =
                            candidate.Composition.ActorChoiceTemporalCensusProvider.GetCurrentCensus();
                        Assert.That(candidateTemporal.Cardinality, Is.Zero);
                        Assert.That(candidateTemporal.Revision, Is.EqualTo(sourceTemporal.Revision));
                        Assert.That(candidateTemporal.OwnerInstanceIdentity,
                            Is.Not.SameAs(sourceTemporal.OwnerInstanceIdentity));

                        OwnerSectionCensusWitness candidateP11Owner =
                            candidate.Composition.ActorChoiceInputCensusProvider.GetCurrentCensus();
                        Assert.That(candidateP11Owner.Cardinality, Is.Zero);
                        Assert.That(candidateP11Owner.OwnerInstanceIdentity,
                            Is.SameAs(candidateTemporal.OwnerInstanceIdentity));
                        Assert.That(candidateP11Owner.Revision, Is.EqualTo(candidateTemporal.Revision));

                        WritePrivateField(
                            candidate.Composition,
                            "<ActorChoiceTemporalCensusProvider>k__BackingField",
                            sourceActorChoiceTemporalProvider);
                        actorChoiceTemporalCompositionProviderReplaced = ReferenceEquals(
                            candidate.Composition.ActorChoiceTemporalCensusProvider,
                            sourceActorChoiceTemporalProvider);

                        Assert.That(candidate.Runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                            out IReadOnlyList<OwnerSectionCensusSnapshot> actorChoiceTargetRows,
                            out _,
                            out ContinuationCensusFailure actorChoiceTargetFailure), Is.True,
                            actorChoiceTargetFailure.ToString());
                        OwnerSectionCensusSnapshot p11TargetRow = actorChoiceTargetRows.Single(
                            row => string.Equals(
                                row.SectionId,
                                ActorChoiceP11CensusProvider.SectionId,
                                StringComparison.Ordinal));
                        actorChoiceTemporalVectorStillIdentifiesRuntimeOwner =
                            p11TargetRow.Role == OwnerSectionRole.Required
                            && p11TargetRow.Cardinality == 0
                            && p11TargetRow.Revision == candidateTemporal.Revision
                            && ReferenceEquals(
                                p11TargetRow.OwnerInstanceIdentity,
                                candidateTemporal.OwnerInstanceIdentity)
                            && !ReferenceEquals(
                                p11TargetRow.OwnerInstanceIdentity,
                                sourceTemporal.OwnerInstanceIdentity);
                        actorChoiceTemporalCapturedRevision = candidateTemporal.Revision;
                        break;
                    }
                    case "p18-crime-receipt-owner":
                    {
                        sourceCrimeReceiptOwner = original.Runtime.CrimeSystemForWorldBoundary;
                        targetCrimeReceiptOwner = candidate.Runtime.CrimeSystemForWorldBoundary;
                        Assert.That(sourceCrimeReceiptOwner, Is.Not.Null);
                        Assert.That(targetCrimeReceiptOwner, Is.Not.Null.And.Not.SameAs(sourceCrimeReceiptOwner));
                        OwnerSectionCensusWitness sourceReceipt =
                            new P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionProvider(
                                sourceCrimeReceiptOwner).GetCurrentCensus();
                        OwnerSectionCensusWitness targetReceipt =
                            new P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionProvider(
                                targetCrimeReceiptOwner).GetCurrentCensus();
                        Assert.That(sourceReceipt.Cardinality, Is.EqualTo(1));
                        Assert.That(sourceReceipt.Revision, Is.Zero);
                        Assert.That(targetReceipt.Cardinality, Is.EqualTo(1));
                        Assert.That(targetReceipt.Revision, Is.Zero);
                        Assert.That(sourceReceipt.OwnerInstanceIdentity, Is.Not.SameAs(targetReceipt.OwnerInstanceIdentity));

                        WritePrivateField(candidate.Runtime, "crimeSystem", sourceCrimeReceiptOwner);
                        crimeReceiptRuntimeOwnerReplaced = ReferenceEquals(
                            candidate.Runtime.CrimeSystemForWorldBoundary, sourceCrimeReceiptOwner);
                        Assert.That(candidate.Runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                            out IReadOnlyList<OwnerSectionCensusSnapshot> crimeTargetRows,
                            out _, out ContinuationCensusFailure crimeTargetFailure), Is.True,
                            crimeTargetFailure.ToString());
                        OwnerSectionCensusSnapshot crimeTargetRow = crimeTargetRows.Single(row =>
                            string.Equals(row.SectionId,
                                P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId,
                                StringComparison.Ordinal));
                        crimeReceiptVectorStillIdentifiesTarget =
                            ReferenceEquals(crimeTargetRow.OwnerInstanceIdentity, targetCrimeReceiptOwner)
                            && crimeTargetRow.Cardinality == 1
                            && crimeTargetRow.Revision == 0L;
                        break;
                    }
                    case "p18-justice-receipt-owner":
                    {
                        sourceJusticeReceiptOwner = original.Runtime.JusticeSystemForWorldBoundary;
                        targetJusticeReceiptOwner = candidate.Runtime.JusticeSystemForWorldBoundary;
                        Assert.That(sourceJusticeReceiptOwner, Is.Not.Null);
                        Assert.That(targetJusticeReceiptOwner, Is.Not.Null.And.Not.SameAs(sourceJusticeReceiptOwner));
                        OwnerSectionCensusWitness sourceReceipt =
                            new P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionProvider(
                                sourceJusticeReceiptOwner).GetCurrentCensus();
                        OwnerSectionCensusWitness targetReceipt =
                            new P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionProvider(
                                targetJusticeReceiptOwner).GetCurrentCensus();
                        Assert.That(sourceReceipt.Cardinality, Is.EqualTo(1));
                        Assert.That(sourceReceipt.Revision, Is.Zero);
                        Assert.That(targetReceipt.Cardinality, Is.EqualTo(1));
                        Assert.That(targetReceipt.Revision, Is.Zero);
                        Assert.That(sourceReceipt.OwnerInstanceIdentity, Is.Not.SameAs(targetReceipt.OwnerInstanceIdentity));

                        WritePrivateField(candidate.Runtime, "justiceSystem", sourceJusticeReceiptOwner);
                        justiceReceiptRuntimeOwnerReplaced = ReferenceEquals(
                            candidate.Runtime.JusticeSystemForWorldBoundary, sourceJusticeReceiptOwner);
                        Assert.That(candidate.Runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                            out IReadOnlyList<OwnerSectionCensusSnapshot> justiceTargetRows,
                            out _, out ContinuationCensusFailure justiceTargetFailure), Is.True,
                            justiceTargetFailure.ToString());
                        OwnerSectionCensusSnapshot justiceTargetRow = justiceTargetRows.Single(row =>
                            string.Equals(row.SectionId,
                                P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId,
                                StringComparison.Ordinal));
                        justiceReceiptVectorStillIdentifiesTarget =
                            ReferenceEquals(justiceTargetRow.OwnerInstanceIdentity, targetJusticeReceiptOwner)
                            && justiceTargetRow.Cardinality == 1
                            && justiceTargetRow.Revision == 0L;
                        break;
                    }
                    case "p18-npc-local-observation-receipt-owner":
                    case "p18-npc-merchant-trade-state-receipt-owner":
                    {
                        bool replaceLocalObservation = string.Equals(
                            corruptionKind,
                            "p18-npc-local-observation-receipt-owner",
                            StringComparison.Ordinal);
                        replacedNpcReceiptSectionPrefix = replaceLocalObservation
                            ? P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionPrefix
                            : P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionPrefix;
                        sourceNpcReceiptProviders = original.Composition.NpcReceiptOwnerCensusProviders.ToArray();
                        IOwnerSectionCensusProvider[] candidateNpcReceiptProviders =
                            candidate.Composition.NpcReceiptOwnerCensusProviders.ToArray();
                        Assert.That(sourceNpcReceiptProviders.Length,
                            Is.EqualTo(original.Runtime.NpcRuntimes.Count * 2));
                        Assert.That(candidateNpcReceiptProviders.Length,
                            Is.EqualTo(candidate.Runtime.NpcRuntimes.Count * 2));
                        Dictionary<string, IOwnerSectionCensusProvider> sourceProvidersBySection =
                            sourceNpcReceiptProviders.ToDictionary(
                                provider => provider.GetCurrentCensus().SectionId,
                                StringComparer.Ordinal);
                        replacementNpcReceiptProviders = new IOwnerSectionCensusProvider[candidateNpcReceiptProviders.Length];
                        bool replacedAtLeastOne = false;
                        for (int i = 0; i < candidateNpcReceiptProviders.Length; i++)
                        {
                            IOwnerSectionCensusProvider targetProvider = candidateNpcReceiptProviders[i];
                            OwnerSectionCensusWitness targetWitness = targetProvider.GetCurrentCensus();
                            Assert.That(targetWitness.Cardinality, Is.Zero);
                            Assert.That(targetWitness.Revision, Is.Zero);
                            if (targetWitness.SectionId.StartsWith(
                                    replacedNpcReceiptSectionPrefix, StringComparison.Ordinal))
                            {
                                IOwnerSectionCensusProvider sourceProvider =
                                    sourceProvidersBySection[targetWitness.SectionId];
                                OwnerSectionCensusWitness sourceWitness = sourceProvider.GetCurrentCensus();
                                Assert.That(sourceWitness.Cardinality, Is.Zero);
                                Assert.That(sourceWitness.Revision, Is.Zero);
                                Assert.That(sourceWitness.OwnerInstanceIdentity,
                                    Is.Not.SameAs(targetWitness.OwnerInstanceIdentity));
                                replacementNpcReceiptProviders[i] = sourceProvider;
                                replacedAtLeastOne = true;
                            }
                            else
                            {
                                replacementNpcReceiptProviders[i] = targetProvider;
                            }
                        }
                        Assert.That(replacedAtLeastOne, Is.True,
                            "The selected receipt family must have one exact-zero row per NPC.");
                        WritePrivateField(candidate.Composition,
                            "<NpcReceiptOwnerCensusProviders>k__BackingField",
                            Array.AsReadOnly(replacementNpcReceiptProviders));
                        npcReceiptCompositionProvidersReplaced =
                            ReferenceEquals(candidate.Composition.NpcReceiptOwnerCensusProviders,
                                replacementNpcReceiptProviders)
                            || candidate.Composition.NpcReceiptOwnerCensusProviders
                                .SequenceEqual(replacementNpcReceiptProviders);

                        Assert.That(candidate.Runtime.TryCaptureUnadmittedRestoredDailyOwnerVector(
                            out IReadOnlyList<OwnerSectionCensusSnapshot> npcReceiptTargetRows,
                            out _, out ContinuationCensusFailure npcReceiptTargetFailure), Is.True,
                            npcReceiptTargetFailure.ToString());
                        npcReceiptVectorStillIdentifiesTarget = true;
                        int selectedRows = 0;
                        foreach (OwnerSectionCensusSnapshot row in npcReceiptTargetRows.Where(row =>
                                     row.SectionId.StartsWith(replacedNpcReceiptSectionPrefix,
                                         StringComparison.Ordinal)))
                        {
                            IOwnerSectionCensusProvider sourceProvider =
                                sourceProvidersBySection[row.SectionId];
                            OwnerSectionCensusWitness sourceWitness = sourceProvider.GetCurrentCensus();
                            Assert.That(row.Cardinality, Is.Zero);
                            Assert.That(row.Revision, Is.Zero);
                            Assert.That(row.OwnerInstanceIdentity,
                                Is.Not.SameAs(sourceWitness.OwnerInstanceIdentity));
                            Assert.That(replacementNpcReceiptProviders.Single(provider =>
                                provider.GetCurrentCensus().SectionId == row.SectionId),
                                Is.SameAs(sourceProvider));
                            selectedRows++;
                        }
                        Assert.That(selectedRows, Is.EqualTo(candidate.Runtime.NpcRuntimes.Count));
                        break;
                    }
                    case "genealogy-dangling-person-endpoint":
                    {
                        corruptedGenealogyStore = candidate.Runtime.GenealogyStoreForWorldBoundary;
                        genealogyMissingEndpoint = new PersonId("p12g-absent-genealogy-endpoint");
                        Assert.That(candidate.Runtime.PersonStore.TryGet(
                            genealogyExistingEndpoint, out _), Is.True);
                        Assert.That(candidate.Runtime.PersonStore.TryGet(
                            genealogyMissingEndpoint, out _), Is.False);
                        Assert.That(corruptedGenealogyStore.TryAddParentage(
                            genealogyExistingEndpoint,
                            genealogyMissingEndpoint,
                            out GenealogyFailure genealogyFailure), Is.True, genealogyFailure.ToString());
                        genealogyEdgeAdded = corruptedGenealogyStore.ContainsParentage(
                            genealogyExistingEndpoint, genealogyMissingEndpoint);
                        break;
                    }
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

            Assert.That(corrupted, Is.True,
                "The private candidate corruption callback must execute. Restore returned: "
                + restoreFailure + ": " + diagnostic);
            if (corruptionKind == "p8c-city-location-binding-owner")
            {
                Assert.That(p8CStoreReplaced, Is.True);
                Assert.That(p8CReplacementStore, Is.Not.Null);
                Assert.That(p8CReplacementStore.Count, Is.Zero);
            }
            if (corruptionKind == "p8c-city-location-binding-populated-target")
            {
                Assert.That(p8CPopulatedTargetBindingApplied, Is.True);
                Assert.That(p8CPopulatedTargetStore, Is.Not.Null);
                Assert.That(p8CPopulatedTargetWitness, Is.Not.Null, p8CPopulatedTargetBindingFailure);
                Assert.That(p8CPopulatedTargetWitness.SectionId,
                    Is.EqualTo(LegacySpatialAnchorBindingCensusProvider.SectionId));
                Assert.That(p8CPopulatedTargetWitness.OwnerInstanceIdentity,
                    Is.SameAs(p8CPopulatedTargetStore));
                Assert.That(p8CPopulatedTargetWitness.Cardinality, Is.EqualTo(1));
                Assert.That(p8CPopulatedTargetWitness.Revision, Is.EqualTo(1L));
                Assert.That(p8CPopulatedTargetStore.Count, Is.EqualTo(1));
                Assert.That(p8CPopulatedTargetStore.Revision, Is.EqualTo(1L));
                Assert.That(p8CPopulatedTargetStore.TryGet(
                        new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, p8CPopulatedTargetCityId),
                        out LocationId retainedP8CLocationId),
                    Is.True);
                Assert.That(retainedP8CLocationId, Is.EqualTo(p8CPopulatedTargetLocationId));
            }
            if (corruptionKind == "p8c-person-position-owner")
            {
                Assert.That(p8CPositionStoreReplaced, Is.True);
                Assert.That(p8CPositionReplacementStore, Is.Not.Null);
                Assert.That(p8CPositionReplacementStore.Count, Is.Zero);
            }
            if (corruptionKind == "p8c-person-position-populated-target")
            {
                Assert.That(p8CPopulatedPositionApplied, Is.True, p8CPopulatedPositionFailure);
                Assert.That(p8CPopulatedPositionStore, Is.Not.Null);
                Assert.That(p8CPopulatedPositionWitness, Is.Not.Null, p8CPopulatedPositionFailure);
                Assert.That(p8CPopulatedPositionWitness.SectionId,
                    Is.EqualTo(PersonSpatialPositionCensusProvider.SectionId));
                Assert.That(p8CPopulatedPositionWitness.OwnerInstanceIdentity,
                    Is.SameAs(p8CPopulatedPositionStore));
                Assert.That(p8CPopulatedPositionWitness.Cardinality, Is.EqualTo(1));
                Assert.That(p8CPopulatedPositionWitness.Revision, Is.EqualTo(1L));
                Assert.That(p8CPopulatedPositionStore.Count, Is.EqualTo(1));
                Assert.That(p8CPopulatedPositionStore.Revision, Is.EqualTo(1L));
                Assert.That(p8CPopulatedPositionStore.TryGetPosition(
                    p8CPopulatedPositionPersonId, out PersonSpatialPosition retainedPosition), Is.True);
                Assert.That(retainedPosition.Position, Is.EqualTo(
                    StablePositionReference.ForLocation(p8CPopulatedPositionLocationId)));
            }
            if (corruptionKind == "p8d-route-observation-populated-target")
            {
                Assert.That(p8DPopulatedObservationApplied, Is.True, p8DPopulatedObservationFailure);
                Assert.That(p8DPopulatedObservationStore, Is.Not.Null);
                Assert.That(p8DPopulatedObservationWitness, Is.Not.Null, p8DPopulatedObservationFailure);
                Assert.That(p8DPopulatedObservationWitness.SectionId,
                    Is.EqualTo(SpatialRouteObservationCensusProvider.SectionId));
                Assert.That(p8DPopulatedObservationWitness.OwnerInstanceIdentity,
                    Is.SameAs(p8DPopulatedObservationStore));
                Assert.That(p8DPopulatedObservationWitness.Cardinality, Is.EqualTo(1));
                Assert.That(p8DPopulatedObservationWitness.Revision, Is.EqualTo(1L));
                Assert.That(p8DPopulatedObservationStore.ObservationCount, Is.EqualTo(1));
                Assert.That(p8DPopulatedObservationStore.Revision, Is.EqualTo(1L));
                Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
                StringAssert.Contains("OwnerCoverageIncomplete", diagnostic);
            }
            if (corruptionKind == "p8d-route-observation-owner")
            {
                Assert.That(p8DRouteKnowledgeStoreReplaced, Is.True);
                Assert.That(p8DRouteKnowledgeReplacementStore, Is.Not.Null);
                Assert.That(p8DRouteKnowledgeReplacementStore.ObservationCount, Is.Zero);
                Assert.That(p8DRouteKnowledgeReplacementStore.Revision, Is.Zero);
            }
            if (corruptionKind == "p8d-person-route-plan-owner")
            {
                Assert.That(p8DRoutePlanStoreReplaced, Is.True);
                Assert.That(p8DRoutePlanReplacementStore, Is.Not.Null);
                Assert.That(p8DRoutePlanReplacementStore.PlanCount, Is.Zero);
                Assert.That(p8DRoutePlanReplacementStore.Revision, Is.Zero);
            }
            if (corruptionKind == "p12f-expedition-owner")
            {
                Assert.That(expeditionCompositionOwnerReplaced, Is.True);
                Assert.That(expeditionVectorStillIdentifiesRuntimeOwner, Is.True,
                    "The runtime's target census must remain bound to its installed ExpeditionStore.");
                Assert.That(expeditionReplacementStore, Is.Not.Null);
                Assert.That(expeditionReplacementStore.ActiveExpeditions, Is.Empty);
                Assert.That(expeditionReplacementStore.Revision, Is.Zero);
            }
            if (corruptionKind == "p12f-decision-occurrence-receipt-owner")
            {
                Assert.That(decisionRecorderCompositionOwnerReplaced, Is.True);
                Assert.That(decisionRecorderVectorStillIdentifiesRuntimeOwner, Is.True,
                    "The candidate runtime target row must remain bound to its installed decision recorder.");
                Assert.That(originalDecisionRecorderOwner.GetOccurrenceReceiptCensus().Cardinality, Is.Zero);
                Assert.That(originalDecisionRecorderOwner.GetOccurrenceReceiptCensus().Revision, Is.Zero);
                Assert.That(decisionRecorderReplacementOwner.GetOccurrenceReceiptCensus().Cardinality, Is.Zero);
                Assert.That(decisionRecorderReplacementOwner.GetOccurrenceReceiptCensus().Revision, Is.Zero);
            }
            if (corruptionKind == "p12e-economy-keyed-sale-receipt-owner")
            {
                Assert.That(economyReceiptCompositionOwnerReplaced, Is.True);
                Assert.That(economyReceiptVectorStillIdentifiesRuntimeOwner, Is.True,
                    "The candidate runtime target row must remain bound to its installed economy receipt owner.");
                Assert.That(originalEconomyReceiptOwner.GetKeyedSaleReceiptCensus().Cardinality, Is.Zero);
                Assert.That(originalEconomyReceiptOwner.GetKeyedSaleReceiptCensus().Revision, Is.Zero);
                Assert.That(economyReceiptReplacementOwner.GetKeyedSaleReceiptCensus().Cardinality, Is.Zero);
                Assert.That(economyReceiptReplacementOwner.GetKeyedSaleReceiptCensus().Revision, Is.Zero);
            }
            if (corruptionKind == "p12f-actor-choice-temporal-owner")
            {
                Assert.That(sourceActorChoiceTemporalProvider, Is.Not.Null);
                Assert.That(actorChoiceTemporalCompositionProviderReplaced, Is.True);
                Assert.That(actorChoiceTemporalVectorStillIdentifiesRuntimeOwner, Is.True,
                    "The target P11 row must remain bound to the target ActorChoice store.");
                OwnerSectionCensusWitness sourceTemporal =
                    sourceActorChoiceTemporalProvider.GetCurrentCensus();
                Assert.That(sourceTemporal.Cardinality, Is.Zero);
                Assert.That(sourceTemporal.Revision, Is.EqualTo(actorChoiceTemporalCapturedRevision));
                StringAssert.Contains(
                    "The non-serialized ActorChoice temporal input count must be zero on the required P11 owner at its captured census revision.",
                    diagnostic);
            }
            if (corruptionKind == "p18-crime-receipt-owner")
            {
                Assert.That(crimeReceiptRuntimeOwnerReplaced, Is.True);
                Assert.That(crimeReceiptVectorStillIdentifiesTarget, Is.True,
                    "The candidate target vector must retain the exact staged CrimeSystem owner.");
            }
            if (corruptionKind == "p18-justice-receipt-owner")
            {
                Assert.That(justiceReceiptRuntimeOwnerReplaced, Is.True);
                Assert.That(justiceReceiptVectorStillIdentifiesTarget, Is.True,
                    "The candidate target vector must retain the exact staged JusticeSystem owner.");
            }
            if (corruptionKind == "p18-npc-local-observation-receipt-owner"
                || corruptionKind == "p18-npc-merchant-trade-state-receipt-owner")
            {
                Assert.That(sourceNpcReceiptProviders, Is.Not.Null);
                Assert.That(replacementNpcReceiptProviders, Is.Not.Null);
                Assert.That(npcReceiptCompositionProvidersReplaced, Is.True);
                Assert.That(npcReceiptVectorStillIdentifiesTarget, Is.True,
                    "The runtime target vector must remain bound to its own per-NPC receipt owners.");
                Assert.That(replacedNpcReceiptSectionPrefix, Is.Not.Null);
            }
            if (corruptionKind == "genealogy-dangling-person-endpoint")
            {
                Assert.That(genealogyExistingEndpoint, Is.Not.Null);
                Assert.That(genealogyMissingEndpoint, Is.Not.Null);
                Assert.That(corruptedGenealogyStore, Is.Not.Null);
                Assert.That(genealogyEdgeAdded, Is.True);
                Assert.That(corruptedGenealogyStore.Count, Is.EqualTo(1));
                Assert.That(corruptedGenealogyStore.ContainsParentage(
                    genealogyExistingEndpoint, genealogyMissingEndpoint), Is.True);
            }
            Assert.That(restoreSucceeded, Is.False, diagnostic);
            Assert.That(diagnostic, Is.Not.Null.And.Not.Empty);
            Assert.That(restoreFailure, Is.EqualTo(corruptionKind == "allocator-high-water"
                || corruptionKind == "genealogy-dangling-person-endpoint"
                || corruptionKind == "wrong-family-identity-key"
                ? P12GDailyV1RestoreFailure.BindingValidationFailed
                : P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            if (corruptionKind == "genealogy-dangling-person-endpoint")
            {
                Assert.That(diagnostic, Is.EqualTo(
                    "A Genealogy edge endpoint does not resolve to a Person in the private target."));
            }
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
                        object executionBoundary = ReadPrivateField<object>(
                            privateCandidate.Runtime, "dailyExecutionBoundary");
                        WritePrivateField(
                            executionBoundary, "currentCaptureToken", sourceToken);
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
                endOperation.Invoke(source, new[] { Enum.ToObject(
                    endOperation.GetParameters()[0].ParameterType, 0) });
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
    [TestCase((int)P12GDailyV1RestoreStage.CWorldIdentityStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.CGenesisManifestStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.CSpatialAuthorityStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.CDeterministicRandomStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.CRuntimeIdAllocatorStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.CRecordSequenceStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.DSnapshotsCaptured)]
    [TestCase((int)P12GDailyV1RestoreStage.DSpatialNetworkStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.DPersonsStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.DGenealogyStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.DCitiesStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.DRelationsValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.DNpcsStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.DFinalGraphValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)]
    [TestCase((int)P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EFactionOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EPoliticalDecisionsOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EMilitaryOwnersStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EConflictOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EWarOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EBattleOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EJusticeOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.ECrimeSocialOwnerStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.EUnresolvedBindingsValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.FOwnerSnapshotsCaptured)]
    [TestCase((int)P12GDailyV1RestoreStage.FPoliticalKnowledgeStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.FScheduledDirectivesStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.FActorChoicesStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.FTravelPartiesStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.FExpeditionsStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetPersonNpcGenealogyBindingsValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetOwnerVectorValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetSentinelsValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetPoliticalKnowledgeBindingsValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetSpatialInvariantsValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetNpcCensusValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetTravelPartyBindingsValidated)]
    [TestCase((int)P12GDailyV1RestoreStage.DGenealogyHydratorEntry)]
    [TestCase((int)P12GDailyV1RestoreStage.DGenealogyEdgeStaged)]
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
            if (injectedStage == P12GDailyV1RestoreStage.DGenealogyEdgeStaged)
            {
                PersonId parentId = new PersonId("p12g-genealogy-partial-stage-parent");
                PersonId firstChildId = new PersonId("p12g-genealogy-partial-stage-first-child");
                PersonId secondChildId = new PersonId("p12g-genealogy-partial-stage-second-child");
                Assert.That(original.Runtime.TryRegisterPerson(
                    new PersonRuntime(parentId, 0L), out PersonStoreFailure sourceParentFailure),
                    Is.True, sourceParentFailure.ToString());
                Assert.That(original.Runtime.TryRegisterPerson(
                    new PersonRuntime(firstChildId, 0L), out PersonStoreFailure sourceFirstChildFailure),
                    Is.True, sourceFirstChildFailure.ToString());
                Assert.That(original.Runtime.TryRegisterPerson(
                    new PersonRuntime(secondChildId, 0L), out PersonStoreFailure sourceSecondChildFailure),
                    Is.True, sourceSecondChildFailure.ToString());
                Assert.That(controlSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(parentId, 0L), out PersonStoreFailure controlParentFailure),
                    Is.True, controlParentFailure.ToString());
                Assert.That(controlSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(firstChildId, 0L), out PersonStoreFailure controlFirstChildFailure),
                    Is.True, controlFirstChildFailure.ToString());
                Assert.That(controlSession.Runtime.TryRegisterPerson(
                    new PersonRuntime(secondChildId, 0L), out PersonStoreFailure controlSecondChildFailure),
                    Is.True, controlSecondChildFailure.ToString());
                Assert.That(original.Runtime.TryAddParentage(
                    parentId, firstChildId, out PersonGenealogyFailure sourceFirstEdgeFailure),
                    Is.True, sourceFirstEdgeFailure.ToString());
                Assert.That(original.Runtime.TryAddParentage(
                    parentId, secondChildId, out PersonGenealogyFailure sourceSecondEdgeFailure),
                    Is.True, sourceSecondEdgeFailure.ToString());
                Assert.That(controlSession.Runtime.TryAddParentage(
                    parentId, firstChildId, out PersonGenealogyFailure controlFirstEdgeFailure),
                    Is.True, controlFirstEdgeFailure.ToString());
                Assert.That(controlSession.Runtime.TryAddParentage(
                    parentId, secondChildId, out PersonGenealogyFailure controlSecondEdgeFailure),
                    Is.True, controlSecondEdgeFailure.ToString());
                Assert.That(original.Runtime.GenealogyStoreForWorldBoundary.Count, Is.EqualTo(2));
                Assert.That(controlSession.Runtime.GenealogyStoreForWorldBoundary.Count, Is.EqualTo(2));
            }
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
            Assert.That(diagnostic, Is.Not.Empty,
                "A rejected private-stage attempt must retain a useful failure diagnostic.");
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
    public void DailyV1RestoredRuntimePreservesGenealogyRevisionGapsWhileOrdinaryConstructionNormalizes()
    {
        PersonId ordinaryParent = new PersonId("genealogy-ordinary-clone-parent");
        PersonId ordinaryChild = new PersonId("genealogy-ordinary-clone-child");
        GenealogyStore ordinarySource = new GenealogyStore();
        Assert.That(ordinarySource.TryAddParentage(ordinaryParent, ordinaryChild, out _), Is.True);
        Assert.That(ordinarySource.TryRemoveParentage(ordinaryParent, ordinaryChild, out _), Is.True);
        Assert.That(ordinarySource.Count, Is.Zero);
        Assert.That(ordinarySource.Revision, Is.EqualTo(2L));

        SimulationRuntime ordinaryRuntime = new SimulationRuntime(
            new SimulationTime(), null, null, economyEnabled: false, genealogyStore: ordinarySource);
        Assert.That(ordinaryRuntime.GenealogyStoreForWorldBoundary.Count, Is.Zero);
        Assert.That(ordinaryRuntime.GenealogyStoreForWorldBoundary.Revision, Is.Zero,
            "Ordinary constructor cloning keeps its existing edge-replay normalization.");

        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G restored genealogy revision preservation");
        try
        {
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(bootstrap, config);
            InvokeInitializeSimulation(bootstrap, null);
            SimulationActiveSession original = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(original, Is.Not.Null);

            PersonId parent = new PersonId("p12g-restored-revision-parent");
            PersonId child = new PersonId("p12g-restored-revision-child");
            Assert.That(original.Runtime.TryRegisterPerson(
                new PersonRuntime(parent, 0L), out PersonStoreFailure parentFailure),
                Is.True, parentFailure.ToString());
            Assert.That(original.Runtime.TryRegisterPerson(
                new PersonRuntime(child, 0L), out PersonStoreFailure childFailure),
                Is.True, childFailure.ToString());
            Assert.That(original.Runtime.TryAddParentage(
                parent, child, out PersonGenealogyFailure addFailure), Is.True, addFailure.ToString());
            Assert.That(original.Runtime.TryRemoveParentage(
                parent, child, out PersonGenealogyFailure removeFailure), Is.True, removeFailure.ToString());
            Assert.That(original.Runtime.GenealogyStoreForWorldBoundary.Count, Is.Zero);
            Assert.That(original.Runtime.GenealogyStoreForWorldBoundary.Revision, Is.EqualTo(2L));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            Assert.That(bootstrap.TryRestoreDailyContinuation(
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.True, restoreFailure + ": " + diagnostic);

            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(restored.Runtime.GenealogyStoreForWorldBoundary.Count, Is.Zero);
            Assert.That(restored.Runtime.GenealogyStoreForWorldBoundary.Revision, Is.EqualTo(2L));
            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsDuplicateGenealogySnapshotAfterOnePrivateEdgeAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G duplicate genealogy snapshot atomicity");
        GameObject controlObject = new GameObject("P12-G duplicate genealogy snapshot control");
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

            PersonId parentId = new PersonId("p12g-genealogy-duplicate-parent");
            PersonId firstChildId = new PersonId("p12g-genealogy-duplicate-first-child");
            PersonId secondChildId = new PersonId("p12g-genealogy-duplicate-second-child");
            foreach (SimulationRuntime runtime in new[] { original.Runtime, controlSession.Runtime })
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(parentId, 0L), out PersonStoreFailure parentFailure),
                    Is.True, parentFailure.ToString());
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(firstChildId, 0L), out PersonStoreFailure firstChildFailure),
                    Is.True, firstChildFailure.ToString());
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(secondChildId, 0L), out PersonStoreFailure secondChildFailure),
                    Is.True, secondChildFailure.ToString());
                Assert.That(runtime.TryAddParentage(
                    parentId, firstChildId, out PersonGenealogyFailure firstEdgeFailure),
                    Is.True, firstEdgeFailure.ToString());
                Assert.That(runtime.TryAddParentage(
                    parentId, secondChildId, out PersonGenealogyFailure secondEdgeFailure),
                    Is.True, secondEdgeFailure.ToString());
            }

            Assert.That(original.Runtime.GenealogyStoreForWorldBoundary.Count, Is.EqualTo(2));
            Assert.That(controlSession.Runtime.GenealogyStoreForWorldBoundary.Count, Is.EqualTo(2));
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
            bool transformApplied = false;
            int stagedGenealogyEdges = 0;

            Assert.That(bootstrap.TryRestoreDailyContinuationWithGenealogySnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.SchemaVersion, Is.EqualTo(GenealogyOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Revision, Is.EqualTo(2L));
                    Assert.That(snapshot.Records, Has.Count.EqualTo(2));
                    transformApplied = true;
                    return new GenealogyOwnerSnapshot(
                        snapshot.SchemaVersion,
                        snapshot.Revision,
                        new[] { snapshot.Records[0], snapshot.Records[0] });
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.DGenealogyEdgeStaged)
                        stagedGenealogyEdges++;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(transformApplied, Is.True,
                "The test-only snapshot replacement must run at the private snapshot-to-hydrator handoff.");
            Assert.That(stagedGenealogyEdges, Is.EqualTo(1),
                "The factory must stage the first duplicate row, then reject the second before its stage observer.");
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-D could not stage the factual roots"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure,
                    out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(continuedOwnerGraph));
            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPropertyEstateDanglingOwnerAfterEarlierEOwnersStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G Property/Estate false-return atomicity");
        GameObject controlObject = new GameObject("P12-G Property/Estate false-return control");
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

            PersonId ownerId = new PersonId("p12g-property-estate-failure-owner");
            PropertyId propertyId = new PropertyId("p12g-property-estate-failure-property");
            Assert.That(original.Runtime.TryRegisterPerson(
                new PersonRuntime(ownerId, 0L), out PersonStoreFailure sourcePersonFailure),
                Is.True, sourcePersonFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(
                new PersonRuntime(ownerId, 0L), out PersonStoreFailure controlPersonFailure),
                Is.True, controlPersonFailure.ToString());
            Assert.That(original.Runtime.TryRegisterPropertyOwnership(
                new PropertyOwnershipRecord(propertyId, ownerId), out PropertyFoundationFailure sourcePropertyFailure),
                Is.True, sourcePropertyFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPropertyOwnership(
                new PropertyOwnershipRecord(propertyId, ownerId), out PropertyFoundationFailure controlPropertyFailure),
                Is.True, controlPropertyFailure.ToString());
            Assert.That(original.Runtime.PropertyOwnershipStore.Count, Is.EqualTo(1));
            Assert.That(controlSession.Runtime.PropertyOwnershipStore.Count, Is.EqualTo(1));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            PersonId missingStagedPersonId = new PersonId("p12g-property-estate-missing-staged-person");
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithPropertyEstateSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True,
                        "The detached Property/Estate snapshot is replaced only after all E snapshots were captured.");
                    Assert.That(institutionOfficeStaged, Is.True,
                        "Institution and Office must already be privately staged at this failure boundary.");
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.OwnershipSchemaVersion,
                        Is.EqualTo(PropertyEstateOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.OwnershipRecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.PropertyRevision, Is.EqualTo(1L));
                    Assert.That(snapshot.TransferHistoryRecordCount, Is.EqualTo(0));
                    Assert.That(snapshot.EstateRecordCount, Is.EqualTo(0));
                    Assert.That(snapshot.OwnershipRows, Has.Count.EqualTo(1));
                    PropertyEstateOwnershipRow current = snapshot.OwnershipRows[0];
                    Assert.That(current.PropertyIdValue, Is.EqualTo(propertyId.Value));
                    Assert.That(current.OwnerPersonIdValue, Is.EqualTo(ownerId.Value));
                    replacementApplied = true;
                    return new PropertyEstateOwnerSnapshot(
                        snapshot.OwnershipSchemaVersion,
                        snapshot.OwnershipRecordCount,
                        snapshot.PropertyRevision,
                        new[] { new PropertyEstateOwnershipRow(current.PropertyIdValue, missingStagedPersonId.Value) },
                        snapshot.TransferHistorySchemaVersion,
                        snapshot.TransferHistoryRecordCount,
                        snapshot.TransferHistoryRevision,
                        snapshot.TransferHistoryRows,
                        snapshot.EstateSchemaVersion,
                        snapshot.EstateRecordCount,
                        snapshot.EstateRevision,
                        snapshot.EstateRows);
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                    {
                        Assert.That(snapshotsCaptured, Is.True);
                        institutionOfficeStaged = true;
                    }
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True,
                "The test-only detached replacement must run at the Property/Estate stage boundary.");
            Assert.That(propertyEstateStaged, Is.False,
                "The dangling Person reference must reject before a Property/Estate owner pair is staged.");
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPoliticalSupportDanglingClaimAfterPoliticalClaimsStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G PoliticalSupport false-return atomicity");
        GameObject controlObject = new GameObject("P12-G PoliticalSupport false-return control");
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

            PersonId claimant = new PersonId("p12g-support-failure-claimant");
            PersonId target = new PersonId("p12g-support-failure-target");
            PoliticalClaimId claimId = new PoliticalClaimId("p12g-support-failure-claim");
            PoliticalSupportRelationId supportId = new PoliticalSupportRelationId("p12g-support-failure-relation");
            Action<SimulationRuntime> registerSupportFixture = runtime =>
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(claimant, runtime.CurrentDay), out PersonStoreFailure personFailure),
                    Is.True, personFailure.ToString());
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(target, runtime.CurrentDay), out PersonStoreFailure targetFailure),
                    Is.True, targetFailure.ToString());
                PoliticalClaimRecord claim = new PoliticalClaimRecord(
                    claimId, claimant, PoliticalClaimType.StatusRecognition,
                    PoliticalClaimTarget.ForPerson(target), PoliticalClaimBasis.Other,
                    "P12-G integrated PoliticalSupport relation fixture", runtime.CurrentDay, Array.Empty<string>());
                Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure),
                    Is.True, claimFailure.ToString());
                PoliticalSupportRelationRecord support = new PoliticalSupportRelationRecord(
                    supportId, PoliticalSupportSource.ForPerson(claimant),
                    PoliticalSupportTarget.ForPoliticalClaim(claimId),
                    PoliticalSupportDisposition.Support, runtime.CurrentDay);
                Assert.That(runtime.TryRegisterPoliticalSupport(support, out PoliticalSupportFailure supportFailure),
                    Is.True, supportFailure.ToString());
            };
            registerSupportFixture(original.Runtime);
            registerSupportFixture(controlSession.Runtime);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            PoliticalClaimId missingStagedClaimId = new PoliticalClaimId("p12g-support-failure-missing-claim");
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool factionStaged = false;
            bool claimsStaged = false;
            bool supportStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithPoliticalSupportSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True,
                        "The detached PoliticalSupport snapshot is replaced only after all E snapshots were captured.");
                    Assert.That(institutionOfficeStaged, Is.True);
                    Assert.That(propertyEstateStaged, Is.True);
                    Assert.That(factionStaged, Is.True);
                    Assert.That(claimsStaged, Is.True,
                        "The exact staged PoliticalClaim owner must exist before PoliticalSupport validation.");
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(originalToken.AbsoluteDay));
                    Assert.That(snapshot.Relations, Is.Not.Null);
                    Assert.That(snapshot.Relations.SectionId,
                        Is.EqualTo(PoliticalSupportStoreCensusProvider.RelationsSectionId));
                    Assert.That(snapshot.Relations.SchemaVersion,
                        Is.EqualTo(P12EPoliticalSupportOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Relations.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Relations.Revision, Is.GreaterThan(0L));
                    Assert.That(snapshot.Relations.Records, Has.Count.EqualTo(1));
                    P12EPoliticalSupportSnapshotRow current = snapshot.Relations.Records[0];
                    Assert.That(current.RelationId, Is.EqualTo(supportId.Value));
                    Assert.That(current.SourceKind, Is.EqualTo((int)PoliticalSupportSourceKind.Person));
                    Assert.That(current.SourceId, Is.EqualTo(claimant.Value));
                    Assert.That(current.TargetKind, Is.EqualTo((int)PoliticalSupportTargetKind.PoliticalClaim));
                    Assert.That(current.TargetId, Is.EqualTo(claimId.Value));
                    replacementApplied = true;
                    return new P12EPoliticalSupportOwnerSnapshot(
                        snapshot.CapturedAbsoluteDay,
                        new P12EPoliticalSupportSnapshotSection(
                            snapshot.Relations.SectionId,
                            snapshot.Relations.SchemaVersion,
                            snapshot.Relations.RecordCount,
                            snapshot.Relations.Revision,
                            new[]
                            {
                                new P12EPoliticalSupportSnapshotRow(
                                    current.RelationId,
                                    current.SourceKind,
                                    current.SourceId,
                                    current.TargetKind,
                                    missingStagedClaimId.Value,
                                    current.Disposition,
                                    current.StartedAbsoluteDay,
                                    current.EndedAbsoluteDay)
                            }));
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                    {
                        Assert.That(snapshotsCaptured, Is.True);
                        institutionOfficeStaged = true;
                    }
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EFactionOwnerStaged)
                        factionStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged)
                    {
                        Assert.That(factionStaged, Is.True);
                        claimsStaged = true;
                    }
                    if (stage == P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged)
                        supportStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True);
            Assert.That(supportStaged, Is.False,
                "The dangling Claim reference must reject before a PoliticalSupport owner is staged.");
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPoliticalSupportDanglingFactionAfterEarlierOwnersStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G PoliticalSupport missing-Faction source atomicity");
        GameObject controlObject = new GameObject("P12-G PoliticalSupport missing-Faction source control");
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

            PersonId claimant = new PersonId("p12g-support-faction-claimant");
            PersonId target = new PersonId("p12g-support-faction-target");
            FactionId factionId = new FactionId("p12g-support-faction-source");
            PoliticalClaimId claimId = new PoliticalClaimId("p12g-support-faction-claim");
            PoliticalSupportRelationId supportId = new PoliticalSupportRelationId("p12g-support-faction-relation");
            Action<SimulationRuntime> registerSupportFixture = runtime =>
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(claimant, runtime.CurrentDay), out PersonStoreFailure personFailure),
                    Is.True, personFailure.ToString());
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(target, runtime.CurrentDay), out PersonStoreFailure targetFailure),
                    Is.True, targetFailure.ToString());
                Assert.That(runtime.TryRegisterFaction(
                    new FactionRecord(factionId, "Political support fixture", runtime.CurrentDay),
                    out FactionFoundationFailure factionFailure), Is.True, factionFailure.ToString());
                PoliticalClaimRecord claim = new PoliticalClaimRecord(
                    claimId, claimant, PoliticalClaimType.StatusRecognition,
                    PoliticalClaimTarget.ForPerson(target), PoliticalClaimBasis.Other,
                    "P12-G integrated PoliticalSupport relation fixture", runtime.CurrentDay, Array.Empty<string>());
                Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure),
                    Is.True, claimFailure.ToString());
                PoliticalSupportRelationRecord support = new PoliticalSupportRelationRecord(
                    supportId, PoliticalSupportSource.ForFaction(factionId),
                    PoliticalSupportTarget.ForPoliticalClaim(claimId),
                    PoliticalSupportDisposition.Support, runtime.CurrentDay);
                Assert.That(runtime.TryRegisterPoliticalSupport(support, out PoliticalSupportFailure supportFailure),
                    Is.True, supportFailure.ToString());
            };
            registerSupportFixture(original.Runtime);
            registerSupportFixture(controlSession.Runtime);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            FactionId missingStagedFactionId = new FactionId("p12g-support-faction-missing-source");
            FactionStore sourceFactions = ReadPrivateField<FactionStore>(original.Runtime, "factionStore");
            Assert.That(sourceFactions.TryGet(factionId, out _), Is.True);
            Assert.That(sourceFactions.TryGet(missingStagedFactionId, out _), Is.False);
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool factionStaged = false;
            bool claimsStaged = false;
            bool supportStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithPoliticalSupportSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True,
                        "The detached PoliticalSupport snapshot is replaced only after all E snapshots were captured.");
                    Assert.That(institutionOfficeStaged, Is.True);
                    Assert.That(propertyEstateStaged, Is.True);
                    Assert.That(factionStaged, Is.True);
                    Assert.That(claimsStaged, Is.True,
                        "The exact staged PoliticalClaim owner must exist before PoliticalSupport validation.");
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.CapturedAbsoluteDay, Is.EqualTo(originalToken.AbsoluteDay));
                    Assert.That(snapshot.Relations, Is.Not.Null);
                    Assert.That(snapshot.Relations.SectionId,
                        Is.EqualTo(PoliticalSupportStoreCensusProvider.RelationsSectionId));
                    Assert.That(snapshot.Relations.SchemaVersion,
                        Is.EqualTo(P12EPoliticalSupportOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Relations.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Relations.Revision, Is.GreaterThan(0L));
                    Assert.That(snapshot.Relations.Records, Has.Count.EqualTo(1));
                    P12EPoliticalSupportSnapshotRow current = snapshot.Relations.Records[0];
                    Assert.That(current.RelationId, Is.EqualTo(supportId.Value));
                    Assert.That(current.SourceKind, Is.EqualTo((int)PoliticalSupportSourceKind.Faction));
                    Assert.That(current.SourceId, Is.EqualTo(factionId.Value));
                    Assert.That(current.TargetKind, Is.EqualTo((int)PoliticalSupportTargetKind.PoliticalClaim));
                    Assert.That(current.TargetId, Is.EqualTo(claimId.Value));
                    replacementApplied = true;
                    return new P12EPoliticalSupportOwnerSnapshot(
                        snapshot.CapturedAbsoluteDay,
                        new P12EPoliticalSupportSnapshotSection(
                            snapshot.Relations.SectionId,
                            snapshot.Relations.SchemaVersion,
                            snapshot.Relations.RecordCount,
                            snapshot.Relations.Revision,
                            new[]
                            {
                                new P12EPoliticalSupportSnapshotRow(
                                    current.RelationId,
                                    current.SourceKind,
                                    missingStagedFactionId.Value,
                                    current.TargetKind,
                                    current.TargetId,
                                    current.Disposition,
                                    current.StartedAbsoluteDay,
                                    current.EndedAbsoluteDay)
                            }));
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                    {
                        Assert.That(snapshotsCaptured, Is.True);
                        institutionOfficeStaged = true;
                    }
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EFactionOwnerStaged)
                        factionStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged)
                    {
                        Assert.That(factionStaged, Is.True);
                        claimsStaged = true;
                    }
                    if (stage == P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged)
                        supportStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True);
            Assert.That(supportStaged, Is.False,
                "The missing Faction source must reject before a PoliticalSupport owner is staged.");
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            FactionStore restoredFactions = ReadPrivateField<FactionStore>(restored.Runtime, "factionStore");
            Assert.That(restoredFactions.Count, Is.EqualTo(1));
            Assert.That(restoredFactions.TryGet(factionId, out FactionRecord restoredFaction), Is.True);
            Assert.That(restoredFaction.Id, Is.EqualTo(factionId));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }
    [Test]
    public void DailyV1RestoreRejectsFactionAffiliationMissingPersonAfterEarlierEOwnersStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G Faction affiliation missing-Person atomicity");
        GameObject controlObject = new GameObject("P12-G Faction affiliation missing-Person control");
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

            PersonId member = new PersonId("p12g-faction-affiliation-member");
            FactionId factionId = new FactionId("p12g-faction-affiliation-faction");
            Action<SimulationRuntime> registerAffiliationFixture = runtime =>
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(member, runtime.CurrentDay), out PersonStoreFailure personFailure),
                    Is.True, personFailure.ToString());
                Assert.That(runtime.TryRegisterFaction(
                    new FactionRecord(factionId, "P12-G affiliation fixture", runtime.CurrentDay),
                    out FactionFoundationFailure factionFailure), Is.True, factionFailure.ToString());
                Assert.That(runtime.TryProposeFactionAffiliation(
                    factionId, member,
                    out FactionAffiliationAddTransition proposal,
                    out FactionFoundationFailure proposalFailure), Is.True, proposalFailure.ToString());
                Assert.That(runtime.TryApplyFactionAffiliation(
                    proposal, out FactionFoundationFailure applyFailure), Is.True, applyFailure.ToString());
            };
            registerAffiliationFixture(original.Runtime);
            registerAffiliationFixture(controlSession.Runtime);

            Assert.That(original.Runtime.FactionRecords, Has.Count.EqualTo(1));
            Assert.That(original.Runtime.FactionAffiliationRecords, Has.Count.EqualTo(1));
            FactionAffiliationRecord sourceAffiliation = original.Runtime.FactionAffiliationRecords[0];
            Assert.That(sourceAffiliation.FactionId, Is.EqualTo(factionId));
            Assert.That(sourceAffiliation.PersonId, Is.EqualTo(member));
            Assert.That(sourceAffiliation.IsActive, Is.True);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            PersonId missingStagedPersonId = new PersonId("p12g-faction-affiliation-missing-person");
            PersonStore sourcePersons = ReadPrivateField<PersonStore>(original.Runtime, "personStore");
            Assert.That(sourcePersons.TryGet(member, out _), Is.True);
            Assert.That(sourcePersons.TryGet(missingStagedPersonId, out _), Is.False);
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool factionStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithFactionSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True,
                        "The detached Faction snapshot is replaced only after all E snapshots were captured.");
                    Assert.That(institutionOfficeStaged, Is.True);
                    Assert.That(propertyEstateStaged, Is.True);
                    Assert.That(factionStaged, Is.False);
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.Factions, Is.Not.Null);
                    Assert.That(snapshot.Factions.SectionId,
                        Is.EqualTo(FactionStoreCensusProvider.FactionsSectionId));
                    Assert.That(snapshot.Factions.SchemaVersion,
                        Is.EqualTo(P12EFactionOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Factions.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Factions.Records, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Factions.Revision, Is.GreaterThan(0L));
                    Assert.That(snapshot.Factions.Records[0].Id, Is.EqualTo(factionId.Value));
                    Assert.That(snapshot.Affiliations, Is.Not.Null);
                    Assert.That(snapshot.Affiliations.SectionId,
                        Is.EqualTo(FactionStoreCensusProvider.AffiliationsSectionId));
                    Assert.That(snapshot.Affiliations.SchemaVersion,
                        Is.EqualTo(P12EFactionOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Affiliations.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Affiliations.Records, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Affiliations.Revision, Is.EqualTo(snapshot.Factions.Revision));
                    P12EFactionAffiliationSnapshotRecord current = snapshot.Affiliations.Records[0];
                    Assert.That(current.AffiliationId, Is.EqualTo(sourceAffiliation.AffiliationId.Value));
                    Assert.That(current.FactionId, Is.EqualTo(factionId.Value));
                    Assert.That(current.PersonId, Is.EqualTo(member.Value));
                    Assert.That(current.JoinedAbsoluteDay, Is.EqualTo(sourceAffiliation.JoinedAbsoluteDay));
                    Assert.That(current.EndedAbsoluteDay, Is.EqualTo(sourceAffiliation.EndedAbsoluteDay));
                    Assert.That(current.EndReason, Is.EqualTo(sourceAffiliation.EndReason.HasValue
                        ? (int?)sourceAffiliation.EndReason.Value
                        : null));
                    Assert.That(current.IsActive, Is.EqualTo(sourceAffiliation.IsActive));
                    replacementApplied = true;
                    return new P12EFactionOwnerSnapshot(
                        snapshot.Factions,
                        new P12EFactionSnapshotSection<P12EFactionAffiliationSnapshotRecord>(
                            snapshot.Affiliations.SectionId,
                            snapshot.Affiliations.SchemaVersion,
                            snapshot.Affiliations.RecordCount,
                            snapshot.Affiliations.Revision,
                            new[]
                            {
                                new P12EFactionAffiliationSnapshotRecord(
                                    current.AffiliationId,
                                    current.FactionId,
                                    missingStagedPersonId.Value,
                                    current.JoinedAbsoluteDay,
                                    current.EndedAbsoluteDay,
                                    current.EndReason,
                                    current.IsActive)
                            },
                            row => row?.Copy()));
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                    {
                        Assert.That(snapshotsCaptured, Is.True);
                        institutionOfficeStaged = true;
                    }
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EFactionOwnerStaged)
                        factionStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True);
            Assert.That(factionStaged, Is.False,
                "The missing Person endpoint must reject before a Faction owner is staged.");
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            FactionStore restoredFactions = ReadPrivateField<FactionStore>(restored.Runtime, "factionStore");
            Assert.That(restoredFactions.Count, Is.EqualTo(1));
            Assert.That(restoredFactions.AffiliationCount, Is.EqualTo(1));
            Assert.That(restoredFactions.TryGet(factionId, out FactionRecord restoredFaction), Is.True);
            Assert.That(restoredFaction.Id, Is.EqualTo(factionId));
            Assert.That(restoredFactions.TryGetAffiliation(
                sourceAffiliation.AffiliationId, out FactionAffiliationRecord restoredAffiliation), Is.True);
            Assert.That(restoredAffiliation.PersonId, Is.EqualTo(member));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }
    [Test]
    public void DailyV1RestoreRejectsPoliticalClaimDanglingOfficeAfterEarlierEOwnersStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G PoliticalClaim Office-target false-return atomicity");
        GameObject controlObject = new GameObject("P12-G PoliticalClaim Office-target false-return control");
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

            PersonId claimant = new PersonId("p12g-claim-failure-claimant");
            InstitutionId institutionId = new InstitutionId("p12g-claim-failure-institution");
            OfficeId officeId = new OfficeId("p12g-claim-failure-office");
            PoliticalClaimId claimId = new PoliticalClaimId("p12g-claim-failure-claim");
            Action<SimulationRuntime> registerClaimFixture = runtime =>
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(claimant, runtime.CurrentDay), out PersonStoreFailure personFailure),
                    Is.True, personFailure.ToString());
                Assert.That(runtime.TryRegisterInstitution(
                    new InstitutionRecord(institutionId), out InstitutionFoundationFailure institutionFailure),
                    Is.True, institutionFailure.ToString());
                Assert.That(runtime.TryRegisterOffice(
                    new OfficeRecord(officeId, institutionId), out InstitutionFoundationFailure officeFailure),
                    Is.True, officeFailure.ToString());
                PoliticalClaimRecord claim = new PoliticalClaimRecord(
                    claimId, claimant, PoliticalClaimType.OfficeEntitlement,
                    PoliticalClaimTarget.ForOffice(officeId), PoliticalClaimBasis.Other,
                    "P12-G integrated PoliticalClaim Office-target fixture", runtime.CurrentDay, Array.Empty<string>());
                Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure),
                    Is.True, claimFailure.ToString());
            };
            registerClaimFixture(original.Runtime);
            registerClaimFixture(controlSession.Runtime);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            OfficeId missingStagedOfficeId = new OfficeId("p12g-claim-failure-missing-office");
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool factionStaged = false;
            bool claimStaged = false;
            bool supportStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithPoliticalClaimSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True,
                        "The detached PoliticalClaim snapshot is replaced only after all E snapshots were captured.");
                    Assert.That(institutionOfficeStaged, Is.True,
                        "The exact staged Institution/Office roots must exist before claim validation.");
                    Assert.That(propertyEstateStaged, Is.True);
                    Assert.That(factionStaged, Is.True);
                    Assert.That(claimStaged, Is.False,
                        "The corrupted PoliticalClaim snapshot must be rejected before its owner stage event.");
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.Claims, Is.Not.Null);
                    Assert.That(snapshot.Claims.SectionId, Is.EqualTo(PoliticalClaimStoreCensusProvider.ClaimsSectionId));
                    Assert.That(snapshot.Claims.SchemaVersion, Is.EqualTo(P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Claims.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Claims.Revision, Is.GreaterThan(0L));
                    Assert.That(snapshot.Claims.Records, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Recognitions, Is.Not.Null);
                    Assert.That(snapshot.Recognitions.SectionId,
                        Is.EqualTo(PoliticalClaimStoreCensusProvider.RecognitionsSectionId));
                    Assert.That(snapshot.Recognitions.SchemaVersion,
                        Is.EqualTo(P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Recognitions.RecordCount, Is.EqualTo(0));
                    Assert.That(snapshot.Recognitions.Revision, Is.EqualTo(snapshot.Claims.Revision));
                    Assert.That(snapshot.Recognitions.Records, Is.Empty);
                    P12EPoliticalClaimRow current = snapshot.Claims.Records[0];
                    Assert.That(current.ClaimId, Is.EqualTo(claimId.Value));
                    Assert.That(current.CreatedAbsoluteDay, Is.LessThanOrEqualTo(originalToken.AbsoluteDay));
                    Assert.That(current.ClaimantPersonId, Is.EqualTo(claimant.Value));
                    Assert.That(current.ClaimType, Is.EqualTo((int)PoliticalClaimType.OfficeEntitlement));
                    Assert.That(current.TargetKind, Is.EqualTo((int)PoliticalClaimTargetKind.Office));
                    Assert.That(current.TargetId, Is.EqualTo(officeId.Value));
                    replacementApplied = true;
                    P12EPoliticalClaimRow corrupted = new P12EPoliticalClaimRow(
                        current.ClaimId,
                        current.ClaimantPersonId,
                        current.ClaimType,
                        current.TargetKind,
                        missingStagedOfficeId.Value,
                        current.Basis,
                        current.BasisDescription,
                        current.CreatedAbsoluteDay,
                        current.Status,
                        current.ResolutionAbsoluteDay,
                        current.EvidenceReferences);
                    return new P12EPoliticalClaimOwnerSnapshot(
                        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                            snapshot.Claims.SectionId,
                            snapshot.Claims.SchemaVersion,
                            snapshot.Claims.RecordCount,
                            snapshot.Claims.Revision,
                            new[] { corrupted },
                            row => row?.Copy()),
                        snapshot.Recognitions);
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                    {
                        Assert.That(snapshotsCaptured, Is.True);
                        institutionOfficeStaged = true;
                    }
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EFactionOwnerStaged)
                        factionStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged)
                        claimStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged)
                        supportStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True);
            Assert.That(claimStaged, Is.False,
                "The dangling Office reference must reject before the PoliticalClaim owner is staged.");
            Assert.That(supportStaged, Is.False);
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPoliticalClaimRecognitionForMissingInstitutionAfterEarlierEOwnersStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G PoliticalClaim Recognition Institution rejection atomicity");
        GameObject controlObject = new GameObject("P12-G PoliticalClaim Recognition Institution control");
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

            PersonId claimant = new PersonId("p12g-recognition-failure-claimant");
            InstitutionId institutionId = new InstitutionId("p12g-recognition-failure-institution");
            PoliticalClaimId claimId = new PoliticalClaimId("p12g-recognition-failure-claim");
            Action<SimulationRuntime> registerFixture = runtime =>
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(claimant, runtime.CurrentDay), out PersonStoreFailure personFailure),
                    Is.True, personFailure.ToString());
                Assert.That(runtime.TryRegisterInstitution(
                    new InstitutionRecord(institutionId), out InstitutionFoundationFailure institutionFailure),
                    Is.True, institutionFailure.ToString());
                PoliticalClaimRecord claim = new PoliticalClaimRecord(
                    claimId, claimant, PoliticalClaimType.StatusRecognition,
                    PoliticalClaimTarget.ForPerson(claimant), PoliticalClaimBasis.Other,
                    "P12-G integrated PoliticalClaim Recognition fixture", runtime.CurrentDay, Array.Empty<string>());
                Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure),
                    Is.True, claimFailure.ToString());
                Assert.That(runtime.TryProposePoliticalClaimRecognition(
                        claimId, institutionId, PoliticalClaimRecognitionState.Recognized,
                        "P12-G retained recognition history", out PoliticalClaimRecognitionTransition transition,
                        out PoliticalClaimFailure proposeFailure),
                    Is.True, proposeFailure.ToString());
                Assert.That(runtime.TryApplyPoliticalClaimRecognition(transition, out PoliticalClaimFailure applyFailure),
                    Is.True, applyFailure.ToString());
            };
            registerFixture(original.Runtime);
            registerFixture(controlSession.Runtime);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);
            PoliticalClaimRecognitionRecord originalRecognition = original.Runtime.PoliticalClaimRecognitionRecords.Single();
            Assert.That(originalRecognition.RecognitionId,
                Is.EqualTo(PoliticalClaimRecognitionRecord.BuildRecognitionId(claimId, institutionId)));
            Assert.That(originalRecognition.History, Is.Not.Empty);
            PoliticalClaimRecognitionHistoryEntry[] originalHistory = originalRecognition.History.ToArray();

            InstitutionId missingStagedInstitutionId = new InstitutionId("p12g-recognition-failure-missing-institution");
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool factionStaged = false;
            bool claimStaged = false;
            bool supportStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithPoliticalClaimSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True,
                        "The detached PoliticalClaim snapshot is replaced only after all E snapshots were captured.");
                    Assert.That(institutionOfficeStaged, Is.True);
                    Assert.That(propertyEstateStaged, Is.True);
                    Assert.That(factionStaged, Is.True);
                    Assert.That(claimStaged, Is.False,
                        "Recognition validation must reject before the PoliticalClaim owner stage event.");
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.Claims.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Claims.Revision, Is.GreaterThan(0L));
                    Assert.That(snapshot.Recognitions.SectionId,
                        Is.EqualTo(PoliticalClaimStoreCensusProvider.RecognitionsSectionId));
                    Assert.That(snapshot.Recognitions.SchemaVersion,
                        Is.EqualTo(P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Recognitions.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Recognitions.Revision, Is.EqualTo(snapshot.Claims.Revision));
                    Assert.That(snapshot.Recognitions.Records, Has.Count.EqualTo(1));
                    P12EPoliticalClaimRecognitionRow current = snapshot.Recognitions.Records[0];
                    Assert.That(current.ClaimId, Is.EqualTo(claimId.Value));
                    Assert.That(current.InstitutionId, Is.EqualTo(institutionId.Value));
                    Assert.That(current.RecognitionId,
                        Is.EqualTo(PoliticalClaimRecognitionRecord.BuildRecognitionId(claimId, institutionId)));
                    Assert.That(current.History, Has.Count.EqualTo(originalHistory.Length));
                    Assert.That(current.State, Is.EqualTo((int)originalRecognition.State));
                    Assert.That(current.RecognitionAbsoluteDay, Is.EqualTo(originalRecognition.RecognitionAbsoluteDay));
                    Assert.That(current.Reason, Is.EqualTo(originalRecognition.Reason));

                    P12EPoliticalClaimRecognitionRow corrupted = new P12EPoliticalClaimRecognitionRow(
                        current.ClaimId,
                        missingStagedInstitutionId.Value,
                        PoliticalClaimRecognitionRecord.BuildRecognitionId(claimId, missingStagedInstitutionId),
                        current.State,
                        current.RecognitionAbsoluteDay,
                        current.Reason,
                        current.History);
                    replacementApplied = true;
                    return new P12EPoliticalClaimOwnerSnapshot(
                        snapshot.Claims,
                        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRecognitionRow>(
                            snapshot.Recognitions.SectionId,
                            snapshot.Recognitions.SchemaVersion,
                            snapshot.Recognitions.RecordCount,
                            snapshot.Recognitions.Revision,
                            new[] { corrupted },
                            row => row?.Copy()));
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                        institutionOfficeStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EFactionOwnerStaged)
                        factionStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged)
                        claimStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged)
                        supportStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True);
            Assert.That(claimStaged, Is.False);
            Assert.That(supportStaged, Is.False);
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));
            PoliticalClaimRecognitionRecord retainedRecognition = original.Runtime.PoliticalClaimRecognitionRecords.Single();
            Assert.That(retainedRecognition, Is.EqualTo(originalRecognition));
            Assert.That(retainedRecognition.History, Is.EqualTo(originalHistory));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(restored.Runtime.PoliticalClaimRecognitionRecords.Single(), Is.EqualTo(originalRecognition));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPoliticalClaimDanglingPropertyAfterEarlierEOwnersStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G PoliticalClaim Property-target false-return atomicity");
        GameObject controlObject = new GameObject("P12-G PoliticalClaim Property-target control");
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

            PersonId claimant = new PersonId("p12g-claim-property-failure-claimant");
            PropertyId propertyId = new PropertyId("p12g-claim-property-failure-property");
            PoliticalClaimId claimId = new PoliticalClaimId("p12g-claim-property-failure-claim");
            Action<SimulationRuntime> registerFixture = runtime =>
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(claimant, runtime.CurrentDay), out PersonStoreFailure personFailure),
                    Is.True, personFailure.ToString());
                Assert.That(runtime.TryRegisterPropertyOwnership(
                    new PropertyOwnershipRecord(propertyId, claimant), out PropertyFoundationFailure propertyFailure),
                    Is.True, propertyFailure.ToString());
                PoliticalClaimRecord claim = new PoliticalClaimRecord(
                    claimId, claimant, PoliticalClaimType.PropertyEntitlement,
                    PoliticalClaimTarget.ForProperty(propertyId), PoliticalClaimBasis.PropertyOwnership,
                    "P12-G integrated PoliticalClaim Property-target fixture", runtime.CurrentDay, Array.Empty<string>());
                Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure),
                    Is.True, claimFailure.ToString());
            };
            registerFixture(original.Runtime);
            registerFixture(controlSession.Runtime);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            PropertyId missingStagedPropertyId = new PropertyId("p12g-claim-property-failure-missing-property");
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool factionStaged = false;
            bool claimStaged = false;
            bool supportStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithPoliticalClaimSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True);
                    Assert.That(institutionOfficeStaged, Is.True);
                    Assert.That(propertyEstateStaged, Is.True,
                        "The exact staged Property root must exist before claim target validation.");
                    Assert.That(factionStaged, Is.True);
                    Assert.That(claimStaged, Is.False,
                        "The corrupted PoliticalClaim snapshot must be rejected before its owner stage event.");
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.Claims.SectionId, Is.EqualTo(PoliticalClaimStoreCensusProvider.ClaimsSectionId));
                    Assert.That(snapshot.Claims.SchemaVersion, Is.EqualTo(P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Claims.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Claims.Revision, Is.GreaterThan(0L));
                    Assert.That(snapshot.Claims.Records, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Recognitions.SectionId,
                        Is.EqualTo(PoliticalClaimStoreCensusProvider.RecognitionsSectionId));
                    Assert.That(snapshot.Recognitions.SchemaVersion,
                        Is.EqualTo(P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Recognitions.RecordCount, Is.EqualTo(0));
                    Assert.That(snapshot.Recognitions.Revision, Is.EqualTo(snapshot.Claims.Revision));
                    Assert.That(snapshot.Recognitions.Records, Is.Empty);
                    P12EPoliticalClaimRow current = snapshot.Claims.Records[0];
                    Assert.That(current.ClaimId, Is.EqualTo(claimId.Value));
                    Assert.That(current.ClaimantPersonId, Is.EqualTo(claimant.Value));
                    Assert.That(current.ClaimType, Is.EqualTo((int)PoliticalClaimType.PropertyEntitlement));
                    Assert.That(current.TargetKind, Is.EqualTo((int)PoliticalClaimTargetKind.Property));
                    Assert.That(current.TargetId, Is.EqualTo(propertyId.Value));
                    replacementApplied = true;
                    P12EPoliticalClaimRow corrupted = new P12EPoliticalClaimRow(
                        current.ClaimId,
                        current.ClaimantPersonId,
                        current.ClaimType,
                        current.TargetKind,
                        missingStagedPropertyId.Value,
                        current.Basis,
                        current.BasisDescription,
                        current.CreatedAbsoluteDay,
                        current.Status,
                        current.ResolutionAbsoluteDay,
                        current.EvidenceReferences);
                    return new P12EPoliticalClaimOwnerSnapshot(
                        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                            snapshot.Claims.SectionId,
                            snapshot.Claims.SchemaVersion,
                            snapshot.Claims.RecordCount,
                            snapshot.Claims.Revision,
                            new[] { corrupted },
                            row => row?.Copy()),
                        snapshot.Recognitions);
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                        institutionOfficeStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EFactionOwnerStaged)
                        factionStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged)
                        claimStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged)
                        supportStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True);
            Assert.That(claimStaged, Is.False);
            Assert.That(supportStaged, Is.False);
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsPoliticalClaimDanglingPersonAfterEarlierEOwnersStageAtomically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject bootstrapObject = new GameObject("P12-G PoliticalClaim Person-target false-return atomicity");
        GameObject controlObject = new GameObject("P12-G PoliticalClaim Person-target control");
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

            PersonId claimant = new PersonId("p12g-claim-person-failure-claimant");
            PersonId targetPersonId = new PersonId("p12g-claim-person-failure-target");
            PoliticalClaimId claimId = new PoliticalClaimId("p12g-claim-person-failure-claim");
            Action<SimulationRuntime> registerFixture = runtime =>
            {
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(claimant, runtime.CurrentDay), out PersonStoreFailure claimantFailure),
                    Is.True, claimantFailure.ToString());
                Assert.That(runtime.TryRegisterPerson(
                    new PersonRuntime(targetPersonId, runtime.CurrentDay), out PersonStoreFailure targetFailure),
                    Is.True, targetFailure.ToString());
                PoliticalClaimRecord claim = new PoliticalClaimRecord(
                    claimId, claimant, PoliticalClaimType.StatusRecognition,
                    PoliticalClaimTarget.ForPerson(targetPersonId), PoliticalClaimBasis.Other,
                    "P12-G integrated PoliticalClaim Person-target fixture", runtime.CurrentDay, Array.Empty<string>());
                Assert.That(runtime.TryRegisterPoliticalClaim(claim, out PoliticalClaimFailure claimFailure),
                    Is.True, claimFailure.ToString());
            };
            registerFixture(original.Runtime);
            registerFixture(controlSession.Runtime);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure firstAdvanceFailure),
                Is.True, firstAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlFirstFailure),
                Is.True, controlFirstFailure.ToString());
            DailyCaptureEligibilityToken originalToken = GetCompletedDailyToken(original.Runtime);
            string beforeFacts = CaptureSelectedDailyFacts(original.Runtime);
            string beforeRoots = CaptureContinuationRootFacts(original);
            string beforeOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, originalToken);

            PersonId missingStagedPersonId = new PersonId("p12g-claim-person-failure-missing-target");
            bool snapshotsCaptured = false;
            bool institutionOfficeStaged = false;
            bool propertyEstateStaged = false;
            bool factionStaged = false;
            bool claimStaged = false;
            bool supportStaged = false;
            bool replacementApplied = false;
            Assert.That(bootstrap.TryRestoreDailyContinuationWithPoliticalClaimSnapshotForTest(
                snapshot =>
                {
                    Assert.That(snapshotsCaptured, Is.True);
                    Assert.That(institutionOfficeStaged, Is.True);
                    Assert.That(propertyEstateStaged, Is.True);
                    Assert.That(factionStaged, Is.True,
                        "The exact staged Person roots and earlier E owners must exist before claim target validation.");
                    Assert.That(claimStaged, Is.False,
                        "The corrupted PoliticalClaim snapshot must be rejected before its owner stage event.");
                    Assert.That(snapshot, Is.Not.Null);
                    Assert.That(snapshot.Claims.SectionId, Is.EqualTo(PoliticalClaimStoreCensusProvider.ClaimsSectionId));
                    Assert.That(snapshot.Claims.SchemaVersion, Is.EqualTo(P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Claims.RecordCount, Is.EqualTo(1));
                    Assert.That(snapshot.Claims.Revision, Is.GreaterThan(0L));
                    Assert.That(snapshot.Claims.Records, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Recognitions.SectionId,
                        Is.EqualTo(PoliticalClaimStoreCensusProvider.RecognitionsSectionId));
                    Assert.That(snapshot.Recognitions.SchemaVersion,
                        Is.EqualTo(P12EPoliticalClaimOwnerSnapshot.CurrentSchemaVersion));
                    Assert.That(snapshot.Recognitions.RecordCount, Is.EqualTo(0));
                    Assert.That(snapshot.Recognitions.Revision, Is.EqualTo(snapshot.Claims.Revision));
                    Assert.That(snapshot.Recognitions.Records, Is.Empty);
                    P12EPoliticalClaimRow current = snapshot.Claims.Records[0];
                    Assert.That(current.ClaimId, Is.EqualTo(claimId.Value));
                    Assert.That(current.ClaimantPersonId, Is.EqualTo(claimant.Value));
                    Assert.That(current.ClaimType, Is.EqualTo((int)PoliticalClaimType.StatusRecognition));
                    Assert.That(current.TargetKind, Is.EqualTo((int)PoliticalClaimTargetKind.Person));
                    Assert.That(current.TargetId, Is.EqualTo(targetPersonId.Value));
                    replacementApplied = true;
                    P12EPoliticalClaimRow corrupted = new P12EPoliticalClaimRow(
                        current.ClaimId,
                        current.ClaimantPersonId,
                        current.ClaimType,
                        current.TargetKind,
                        missingStagedPersonId.Value,
                        current.Basis,
                        current.BasisDescription,
                        current.CreatedAbsoluteDay,
                        current.Status,
                        current.ResolutionAbsoluteDay,
                        current.EvidenceReferences);
                    return new P12EPoliticalClaimOwnerSnapshot(
                        new P12EPoliticalClaimSnapshotSection<P12EPoliticalClaimRow>(
                            snapshot.Claims.SectionId,
                            snapshot.Claims.SchemaVersion,
                            snapshot.Claims.RecordCount,
                            snapshot.Claims.Revision,
                            new[] { corrupted },
                            row => row?.Copy()),
                        snapshot.Recognitions);
                },
                stage =>
                {
                    if (stage == P12GDailyV1RestoreStage.EOwnerSnapshotsCaptured)
                        snapshotsCaptured = true;
                    if (stage == P12GDailyV1RestoreStage.EInstitutionOfficeOwnersStaged)
                        institutionOfficeStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPropertyEstateOwnersStaged)
                        propertyEstateStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EFactionOwnerStaged)
                        factionStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalClaimsOwnerStaged)
                        claimStaged = true;
                    if (stage == P12GDailyV1RestoreStage.EPoliticalSupportOwnerStaged)
                        supportStaged = true;
                },
                out P12GDailyV1RestoreFailure restoreFailure,
                out string diagnostic), Is.False);

            Assert.That(replacementApplied, Is.True);
            Assert.That(claimStaged, Is.False);
            Assert.That(supportStaged, Is.False);
            Assert.That(restoreFailure, Is.EqualTo(P12GDailyV1RestoreFailure.OwnerStageFailed));
            Assert.That(diagnostic, Does.Contain("P12-E could not stage the configured domain owners"));
            Assert.That(ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(originalToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureSelectedDailyFacts(original.Runtime), Is.EqualTo(beforeFacts));
            Assert.That(CaptureContinuationRootFacts(original), Is.EqualTo(beforeRoots));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, originalToken), Is.EqualTo(beforeOwnerGraph));

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retainedFailure),
                Is.True, retainedFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlRetainedFailure),
                Is.True, controlRetainedFailure.ToString());
            DailyCaptureEligibilityToken continuedToken = GetCompletedDailyToken(original.Runtime);
            DailyCaptureEligibilityToken continuedControlToken = GetCompletedDailyToken(controlSession.Runtime);
            string continuedOwnerGraph = CaptureCompleteDailyV1OwnerProjection(original, continuedToken);
            Assert.That(continuedOwnerGraph,
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession, continuedControlToken)),
                "After rejection, source and uninterrupted runtimes must continue identically.");

            Assert.That(bootstrap.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(bootstrap, "activeSession");
            Assert.That(restored, Is.Not.SameAs(original));
            DailyCaptureEligibilityToken restoredToken = GetCompletedDailyToken(restored.Runtime);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, restoredToken), Is.EqualTo(continuedOwnerGraph));
            Assert.That(restored.Runtime.PoliticalClaimRecords, Has.Count.EqualTo(1));
            PoliticalClaimRecord restoredClaim = null;
            foreach (PoliticalClaimRecord record in restored.Runtime.PoliticalClaimRecords)
            {
                if (string.Equals(record.ClaimId.Value, claimId.Value, StringComparison.Ordinal))
                    restoredClaim = record;
            }
            Assert.That(restoredClaim, Is.Not.Null);
            Assert.That(restoredClaim.Target.Kind, Is.EqualTo(PoliticalClaimTargetKind.Person));
            Assert.That(restoredClaim.Target.TargetId, Is.EqualTo(targetPersonId.Value));

            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextAdvanceFailure),
                Is.True, nextAdvanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextFailure),
                Is.True, controlNextFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored, GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The restored runtime and uninterrupted control must remain in parity after another boundary.");
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
    public void DailyV1RestoreRejectsP8DObservationAddedAfterTargetChecksAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G P8-D post-check target recapture source");
        GameObject controlObject = new GameObject("P12-G P8-D post-check target recapture control");
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

            PersonId stagedPersonId = new PersonId("p12g-p8d-post-check-observation-person");
            Assert.That(original.Runtime.TryRegisterPerson(
                new PersonRuntime(stagedPersonId, 0L), out PersonStoreFailure sourcePersonFailure),
                Is.True, sourcePersonFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(
                new PersonRuntime(stagedPersonId, 0L), out PersonStoreFailure controlPersonFailure),
                Is.True, controlPersonFailure.ToString());
            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());

            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));
            OwnerSectionCensusWitness sourceP8DRow = new SpatialRouteObservationCensusProvider(
                original.Runtime.SpatialRouteKnowledgeStore).GetCurrentCensus();
            Assert.That(sourceP8DRow.SectionId, Is.EqualTo(SpatialRouteObservationCensusProvider.SectionId));
            Assert.That(sourceP8DRow.SchemaVersion, Is.EqualTo(SpatialRouteObservationCensusProvider.SchemaVersion));
            Assert.That(sourceP8DRow.Cardinality, Is.Zero);
            Assert.That(sourceP8DRow.Revision, Is.Zero);

            SimulationActiveSession privateCandidate = null;
            SpatialRouteKnowledgeStore targetStore = null;
            LocationId targetLocationId = null;
            OwnerSectionCensusWitness targetRowBeforeMutation = null;
            OwnerSectionCensusWitness targetRowAfterMutation = null;
            long targetMutationEpochBefore = -1L;
            long targetMutationEpochAfter = -1L;
            bool mutationInjected = false;
            bool candidateGuardBound = false;
            bool targetChecksCompleted = false;
            bool targetVectorRecaptured = false;
            bool boundaryAdmitted = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage == P12GDailyV1RestoreStage.TargetChecksCompleted)
                        {
                            targetChecksCompleted = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.CandidateGuardBound)
                        {
                            Assert.That(privateCandidate, Is.Not.Null);
                            Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.True,
                                "The supported store mutation is injected after the candidate guard binds and before recapture.");
                            Assert.That(privateCandidate.Runtime.PersonStore.TryGet(stagedPersonId, out _), Is.True,
                                "The Person must be part of the exact staged target graph.");
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochBefore,
                                out ContinuationCensusFailure censusBeforeFailure), Is.True,
                                censusBeforeFailure.ToString());

                            targetStore = privateCandidate.Runtime.SpatialRouteKnowledgeStore;
                            targetRowBeforeMutation = new SpatialRouteObservationCensusProvider(targetStore)
                                .GetCurrentCensus();
                            Assert.That(targetRowBeforeMutation.SectionId,
                                Is.EqualTo(SpatialRouteObservationCensusProvider.SectionId));
                            Assert.That(targetRowBeforeMutation.SchemaVersion,
                                Is.EqualTo(SpatialRouteObservationCensusProvider.SchemaVersion));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity,
                                Is.Not.SameAs(sourceP8DRow.OwnerInstanceIdentity));
                            Assert.That(targetRowBeforeMutation.Cardinality, Is.Zero);
                            Assert.That(targetRowBeforeMutation.Revision, Is.Zero);
                            targetLocationId = privateCandidate.Runtime.SpatialAuthorityStore.Locations.Single().Id;
                            SpatialObservation observation = new SpatialObservation(
                                SpatialSubject.ForLocation(targetLocationId),
                                SpatialObservationValue.ForEntityBelief(SpatialEntityBelief.KnownPresent),
                                new SpatialObservationProvenance(
                                    SpatialObservationSourceKind.DirectObservation,
                                    "p12g-p8d-post-check-observation",
                                    "p12g-p8d-post-check-observation-origin"),
                                sourceToken.AbsoluteDay,
                                sourceToken.AbsoluteDay,
                                1000,
                                "p12g-p8d-location-presence-v1");
                            Assert.That(targetStore.TryRecordObservation(
                                stagedPersonId,
                                observation,
                                sourceToken.AbsoluteDay,
                                out SpatialKnowledgeFailure observationFailure), Is.True,
                                observationFailure.ToString());
                            targetRowAfterMutation = new SpatialRouteObservationCensusProvider(targetStore)
                                .GetCurrentCensus();
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochAfter,
                                out ContinuationCensusFailure censusAfterFailure), Is.True,
                                censusAfterFailure.ToString());
                            mutationInjected = true;
                            candidateGuardBound = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured)
                        {
                            targetVectorRecaptured = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.BoundaryAdmitted)
                        {
                            boundaryAdmitted = true;
                        }
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(mutationInjected, Is.True);
            Assert.That(targetChecksCompleted, Is.True);
            Assert.That(candidateGuardBound, Is.True);
            Assert.That(targetVectorRecaptured, Is.False,
                "The post-binding owner recapture must reject the row change before it becomes the admitted vector.");
            Assert.That(boundaryAdmitted, Is.False);
            Assert.That(targetRowBeforeMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation.SectionId, Is.EqualTo(targetRowBeforeMutation.SectionId));
            Assert.That(targetRowAfterMutation.SchemaVersion, Is.EqualTo(targetRowBeforeMutation.SchemaVersion));
            Assert.That(targetRowAfterMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
            Assert.That(targetRowAfterMutation.Cardinality, Is.EqualTo(1));
            Assert.That(targetRowAfterMutation.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.ObservationCount, Is.EqualTo(1));
            Assert.That(targetStore.Revision, Is.EqualTo(1L));
            Assert.That(targetMutationEpochBefore, Is.GreaterThanOrEqualTo(0L));
            Assert.That(targetMutationEpochAfter, Is.EqualTo(targetMutationEpochBefore),
                "This direct P8-D owner write changes its local revision without claiming shared-epoch coverage.");
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("could not recapture its owner vector after mutation-guard binding"));
            Assert.That(diagnostic, Does.Contain("OwnerCoverageIncomplete"));
            Assert.That(restored, Is.Null,
                "A target changed after its checked exact-zero vector must not be returned for publication.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            string retriedGraph = CaptureCompleteDailyV1OwnerProjection(
                retried, GetCompletedDailyToken(retried.Runtime));
            Assert.That(retriedGraph, Is.EqualTo(sourceGraph));
            Assert.That(retriedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            Assert.That(retried.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retryAdvance),
                Is.True, retryAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextAdvance),
                Is.True, controlNextAdvance.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The rejected target observation must not affect source retry or later deterministic continuation parity.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsP8CCityBindingAddedAfterTargetChecksAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G P8-C City binding post-check target recapture source");
        GameObject controlObject = new GameObject("P12-G P8-C City binding post-check target recapture control");
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

            CityRuntime stagedCity = original.Runtime.Cities
                .OrderBy(city => city.RuntimeId, StringComparer.Ordinal)
                .First();
            string stagedCityId = stagedCity.RuntimeId;
            LegacySpatialAnchorBindingStore sourceStore = original.Runtime.LegacySpatialAnchorBindingStore;
            Assert.That(sourceStore, Is.Not.Null);
            OwnerSectionCensusWitness sourceP8CRow = new LegacySpatialAnchorBindingCensusProvider(sourceStore)
                .GetCurrentCensus();
            Assert.That(sourceP8CRow.SectionId, Is.EqualTo(LegacySpatialAnchorBindingCensusProvider.SectionId));
            Assert.That(sourceP8CRow.SchemaVersion, Is.EqualTo(LegacySpatialAnchorBindingCensusProvider.SchemaVersion));
            Assert.That(sourceP8CRow.OwnerInstanceIdentity, Is.SameAs(sourceStore));
            Assert.That(sourceP8CRow.Cardinality, Is.Zero);
            Assert.That(sourceP8CRow.Revision, Is.Zero);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            SimulationActiveSession privateCandidate = null;
            LegacySpatialAnchorBindingStore targetStore = null;
            LocationId targetLocationId = null;
            OwnerSectionCensusWitness targetRowBeforeMutation = null;
            OwnerSectionCensusWitness targetRowAfterMutation = null;
            long targetMutationEpochBefore = -1L;
            long targetMutationEpochAfter = -1L;
            bool mutationInjected = false;
            bool candidateGuardBound = false;
            bool targetChecksCompleted = false;
            bool targetVectorRecaptured = false;
            bool boundaryAdmitted = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage == P12GDailyV1RestoreStage.TargetChecksCompleted)
                        {
                            targetChecksCompleted = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.CandidateGuardBound)
                        {
                            Assert.That(privateCandidate, Is.Not.Null);
                            Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.True,
                                "The supported store mutation is injected after the candidate guard binds and before recapture.");
                            CityRuntime candidateCity = privateCandidate.Runtime.Cities.Single(
                                city => string.Equals(city.RuntimeId, stagedCityId, StringComparison.Ordinal));
                            Assert.That(candidateCity, Is.Not.Null);
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochBefore,
                                out ContinuationCensusFailure censusBeforeFailure), Is.True,
                                censusBeforeFailure.ToString());

                            targetStore = privateCandidate.Runtime.LegacySpatialAnchorBindingStore;
                            Assert.That(targetStore, Is.Not.Null);
                            targetRowBeforeMutation = new LegacySpatialAnchorBindingCensusProvider(targetStore)
                                .GetCurrentCensus();
                            Assert.That(targetRowBeforeMutation.SectionId,
                                Is.EqualTo(LegacySpatialAnchorBindingCensusProvider.SectionId));
                            Assert.That(targetRowBeforeMutation.SchemaVersion,
                                Is.EqualTo(LegacySpatialAnchorBindingCensusProvider.SchemaVersion));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity,
                                Is.Not.SameAs(sourceP8CRow.OwnerInstanceIdentity));
                            Assert.That(targetRowBeforeMutation.Cardinality, Is.Zero);
                            Assert.That(targetRowBeforeMutation.Revision, Is.Zero);
                            targetLocationId = privateCandidate.Runtime.SpatialAuthorityStore.Locations.Single().Id;
                            Assert.That(targetStore.TryBindCity(
                                stagedCityId,
                                targetLocationId,
                                out SpatialAnchorBindingFailure bindingFailure), Is.True,
                                bindingFailure.ToString());
                            targetRowAfterMutation = new LegacySpatialAnchorBindingCensusProvider(targetStore)
                                .GetCurrentCensus();
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochAfter,
                                out ContinuationCensusFailure censusAfterFailure), Is.True,
                                censusAfterFailure.ToString());
                            mutationInjected = true;
                            candidateGuardBound = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured)
                        {
                            targetVectorRecaptured = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.BoundaryAdmitted)
                        {
                            boundaryAdmitted = true;
                        }
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(mutationInjected, Is.True);
            Assert.That(targetChecksCompleted, Is.True);
            Assert.That(candidateGuardBound, Is.True);
            Assert.That(targetVectorRecaptured, Is.False,
                "The post-binding owner recapture must reject the row change before it becomes the admitted vector.");
            Assert.That(boundaryAdmitted, Is.False);
            Assert.That(targetRowBeforeMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation.SectionId, Is.EqualTo(targetRowBeforeMutation.SectionId));
            Assert.That(targetRowAfterMutation.SchemaVersion, Is.EqualTo(targetRowBeforeMutation.SchemaVersion));
            Assert.That(targetRowAfterMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
            Assert.That(targetRowAfterMutation.Cardinality, Is.EqualTo(1));
            Assert.That(targetRowAfterMutation.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.Count, Is.EqualTo(1));
            Assert.That(targetStore.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.TryGet(
                    new SpatialAnchorOwnerId(SpatialAnchorOwnerKind.City, stagedCityId),
                    out LocationId retainedLocationId), Is.True);
            Assert.That(retainedLocationId, Is.EqualTo(targetLocationId));
            Assert.That(targetMutationEpochBefore, Is.GreaterThanOrEqualTo(0L));
            Assert.That(targetMutationEpochAfter, Is.EqualTo(targetMutationEpochBefore),
                "This direct P8-C owner write changes its local revision without claiming shared-epoch coverage.");
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("could not recapture its owner vector after mutation-guard binding"));
            Assert.That(diagnostic, Does.Contain("OwnerCoverageIncomplete"));
            Assert.That(restored, Is.Null,
                "A target changed after its checked exact-zero vector must not be returned for publication.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));
            Assert.That(sourceStore.Count, Is.Zero);
            Assert.That(sourceStore.Revision, Is.Zero);

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            string retriedGraph = CaptureCompleteDailyV1OwnerProjection(
                retried, GetCompletedDailyToken(retried.Runtime));
            Assert.That(retriedGraph, Is.EqualTo(sourceGraph));
            Assert.That(retriedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));
            Assert.That(retried.Runtime.LegacySpatialAnchorBindingStore.Count, Is.Zero);
            Assert.That(retried.Runtime.LegacySpatialAnchorBindingStore.Revision, Is.Zero);

            Assert.That(retried.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retryAdvance),
                Is.True, retryAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextAdvance),
                Is.True, controlNextAdvance.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The rejected target binding must not affect source retry or later deterministic continuation parity.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsGenealogyParentageAddedAfterTargetChecksAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G P12-D Genealogy post-check target recapture source");
        GameObject controlObject = new GameObject("P12-G P12-D Genealogy post-check target recapture control");
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

            PersonId parentId = new PersonId("p12g-genealogy-post-check-parent");
            PersonId childId = new PersonId("p12g-genealogy-post-check-child");
            Assert.That(original.Runtime.TryRegisterPerson(
                new PersonRuntime(parentId, 0L), out PersonStoreFailure sourceParentFailure),
                Is.True, sourceParentFailure.ToString());
            Assert.That(original.Runtime.TryRegisterPerson(
                new PersonRuntime(childId, 0L), out PersonStoreFailure sourceChildFailure),
                Is.True, sourceChildFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(
                new PersonRuntime(parentId, 0L), out PersonStoreFailure controlParentFailure),
                Is.True, controlParentFailure.ToString());
            Assert.That(controlSession.Runtime.TryRegisterPerson(
                new PersonRuntime(childId, 0L), out PersonStoreFailure controlChildFailure),
                Is.True, controlChildFailure.ToString());

            NpcRuntime sourceNpc = original.Runtime.NpcRuntimes
                .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal).First();
            NpcRuntime controlNpc = controlSession.Runtime.NpcRuntimes
                .OrderBy(npc => npc.RuntimeId, StringComparer.Ordinal).First();
            Assert.That(sourceNpc.RuntimeId, Is.EqualTo(controlNpc.RuntimeId));
            Assert.That(sourceNpc.PersonId, Is.Null);
            Assert.That(controlNpc.PersonId, Is.Null);
            Assert.That(original.Runtime.TryBindExistingNpcToPerson(
                childId, sourceNpc.RuntimeId, out PersonMaterializationFailure sourceBindFailure),
                Is.True, sourceBindFailure.ToString());
            Assert.That(controlSession.Runtime.TryBindExistingNpcToPerson(
                childId, controlNpc.RuntimeId, out PersonMaterializationFailure controlBindFailure),
                Is.True, controlBindFailure.ToString());
            Assert.That(original.Runtime.ContainsParentage(parentId, childId), Is.False);
            Assert.That(controlSession.Runtime.ContainsParentage(parentId, childId), Is.False);

            GenealogyStore sourceStore = original.Runtime.GenealogyStoreForWorldBoundary;
            OwnerSectionCensusWitness sourceRow = new GenealogyCensusProvider(sourceStore).GetCurrentCensus();
            Assert.That(sourceRow.SectionId, Is.EqualTo(GenealogyCensusProvider.SectionId));
            Assert.That(sourceRow.SchemaVersion, Is.EqualTo(GenealogyCensusProvider.SchemaVersion));
            Assert.That(sourceRow.OwnerInstanceIdentity, Is.SameAs(sourceStore));
            Assert.That(sourceRow.Cardinality, Is.Zero);
            Assert.That(sourceRow.Revision, Is.Zero);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));
            OwnerSectionCensusSnapshot sourceTokenRow = sourceToken.OwnerSections.Single(section =>
                section.SectionId == GenealogyCensusProvider.SectionId);
            Assert.That(sourceTokenRow.Cardinality, Is.Zero);
            Assert.That(sourceTokenRow.Revision, Is.Zero);

            SimulationActiveSession privateCandidate = null;
            GenealogyStore targetStore = null;
            OwnerSectionCensusWitness targetRowBeforeMutation = null;
            OwnerSectionCensusWitness targetRowAfterMutation = null;
            long targetMutationEpochBefore = -1L;
            long targetMutationEpochAfter = -1L;
            bool targetChecksCompleted = false;
            bool candidateGuardBound = false;
            bool mutationInjected = false;
            bool targetVectorRecaptured = false;
            bool boundaryAdmitted = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage == P12GDailyV1RestoreStage.TargetChecksCompleted)
                        {
                            targetChecksCompleted = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.CandidateGuardBound)
                        {
                            Assert.That(privateCandidate, Is.Not.Null);
                            Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.True,
                                "The supported runtime operation is injected after graph checks and guard binding.");
                            targetStore = privateCandidate.Runtime.GenealogyStoreForWorldBoundary;
                            targetRowBeforeMutation = new GenealogyCensusProvider(targetStore).GetCurrentCensus();
                            Assert.That(targetRowBeforeMutation.SectionId,
                                Is.EqualTo(GenealogyCensusProvider.SectionId));
                            Assert.That(targetRowBeforeMutation.SchemaVersion,
                                Is.EqualTo(GenealogyCensusProvider.SchemaVersion));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity,
                                Is.Not.SameAs(sourceRow.OwnerInstanceIdentity));
                            Assert.That(targetRowBeforeMutation.Cardinality, Is.Zero);
                            Assert.That(targetRowBeforeMutation.Revision, Is.Zero);
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochBefore,
                                out ContinuationCensusFailure censusBeforeFailure), Is.True,
                                censusBeforeFailure.ToString());

                            Assert.That(privateCandidate.Runtime.TryAddParentage(
                                parentId, childId, out PersonGenealogyFailure genealogyFailure), Is.True,
                                genealogyFailure.ToString());
                            Assert.That(privateCandidate.Runtime.ContainsParentage(parentId, childId), Is.True);
                            targetRowAfterMutation = new GenealogyCensusProvider(targetStore).GetCurrentCensus();
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochAfter,
                                out ContinuationCensusFailure censusAfterFailure), Is.True,
                                censusAfterFailure.ToString());
                            mutationInjected = true;
                            candidateGuardBound = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured)
                        {
                            targetVectorRecaptured = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.BoundaryAdmitted)
                        {
                            boundaryAdmitted = true;
                        }
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(targetChecksCompleted, Is.True);
            Assert.That(candidateGuardBound, Is.True);
            Assert.That(mutationInjected, Is.True);
            Assert.That(targetVectorRecaptured, Is.False,
                "A changed required Genealogy row must reject before final owner-vector recapture.");
            Assert.That(boundaryAdmitted, Is.False);
            Assert.That(targetRowBeforeMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation.SectionId, Is.EqualTo(targetRowBeforeMutation.SectionId));
            Assert.That(targetRowAfterMutation.SchemaVersion, Is.EqualTo(targetRowBeforeMutation.SchemaVersion));
            Assert.That(targetRowAfterMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
            Assert.That(targetRowAfterMutation.Cardinality, Is.EqualTo(1));
            Assert.That(targetRowAfterMutation.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.Count, Is.EqualTo(1));
            Assert.That(targetStore.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.ContainsParentage(parentId, childId), Is.True);
            Assert.That(targetMutationEpochBefore, Is.GreaterThanOrEqualTo(0L));
            Assert.That(targetMutationEpochAfter, Is.EqualTo(targetMutationEpochBefore + 1L),
                "The committed P12-B parentage operation must invalidate the shared epoch exactly once.");
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("target mutation epoch changed after whole-graph validation"));
            Assert.That(restored, Is.Null,
                "A private target whose required Genealogy row changed must not reach publication.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));
            Assert.That(sourceStore.Count, Is.Zero);
            Assert.That(sourceStore.Revision, Is.Zero);
            Assert.That(original.Runtime.ContainsParentage(parentId, childId), Is.False);

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            Assert.That(retried.Runtime.GenealogyStoreForWorldBoundary.Count, Is.Zero);
            Assert.That(retried.Runtime.GenealogyStoreForWorldBoundary.Revision, Is.Zero);
            Assert.That(retried.Runtime.ContainsParentage(parentId, childId), Is.False);
            string retriedGraph = CaptureCompleteDailyV1OwnerProjection(
                retried, GetCompletedDailyToken(retried.Runtime));
            Assert.That(retriedGraph, Is.EqualTo(sourceGraph));
            Assert.That(retriedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            Assert.That(retried.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retryAdvance),
                Is.True, retryAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextAdvance),
                Is.True, controlNextAdvance.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The rejected private Genealogy mutation must not change retry or later continuation parity.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsFactionAddedAfterTargetChecksAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G P12-E Faction post-check target recapture source");
        GameObject controlObject = new GameObject("P12-G P12-E Faction post-check target recapture control");
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

            FactionStore sourceStore = ReadPrivateField<FactionStore>(original.Runtime, "factionStore");
            IReadOnlyList<IOwnerSectionCensusProvider> sourceProviders =
                FactionStoreCensusProvider.CreateProviders(sourceStore);
            OwnerSectionCensusWitness sourceFactionRow = sourceProviders[0].GetCurrentCensus();
            OwnerSectionCensusWitness sourceAffiliationRow = sourceProviders[1].GetCurrentCensus();
            Assert.That(sourceFactionRow.SectionId, Is.EqualTo(FactionStoreCensusProvider.FactionsSectionId));
            Assert.That(sourceAffiliationRow.SectionId, Is.EqualTo(FactionStoreCensusProvider.AffiliationsSectionId));
            Assert.That(sourceFactionRow.SchemaVersion, Is.EqualTo(FactionStoreCensusProvider.SchemaVersion));
            Assert.That(sourceAffiliationRow.SchemaVersion, Is.EqualTo(FactionStoreCensusProvider.SchemaVersion));
            Assert.That(sourceFactionRow.OwnerInstanceIdentity, Is.SameAs(sourceStore));
            Assert.That(sourceAffiliationRow.OwnerInstanceIdentity, Is.SameAs(sourceStore));
            Assert.That(sourceFactionRow.Cardinality, Is.Zero);
            Assert.That(sourceAffiliationRow.Cardinality, Is.Zero);
            Assert.That(sourceFactionRow.Revision, Is.Zero);
            Assert.That(sourceAffiliationRow.Revision, Is.Zero);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));
            OwnerSectionCensusSnapshot sourceTokenFaction = sourceToken.OwnerSections.Single(section =>
                section.SectionId == FactionStoreCensusProvider.FactionsSectionId);
            OwnerSectionCensusSnapshot sourceTokenAffiliation = sourceToken.OwnerSections.Single(section =>
                section.SectionId == FactionStoreCensusProvider.AffiliationsSectionId);
            Assert.That(sourceTokenFaction.Cardinality, Is.Zero);
            Assert.That(sourceTokenAffiliation.Cardinality, Is.Zero);
            Assert.That(sourceTokenFaction.Revision, Is.Zero);
            Assert.That(sourceTokenAffiliation.Revision, Is.Zero);

            SimulationActiveSession privateCandidate = null;
            FactionStore targetStore = null;
            OwnerSectionCensusWitness targetFactionBefore = null;
            OwnerSectionCensusWitness targetAffiliationBefore = null;
            OwnerSectionCensusWitness targetFactionAfter = null;
            OwnerSectionCensusWitness targetAffiliationAfter = null;
            long targetMutationEpochBefore = -1L;
            long targetMutationEpochAfter = -1L;
            bool targetChecksCompleted = false;
            bool candidateGuardBound = false;
            bool mutationInjected = false;
            bool targetVectorRecaptured = false;
            bool boundaryAdmitted = false;
            FactionId injectedFactionId = new FactionId("p12g-faction-post-guard-recapture");

            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage == P12GDailyV1RestoreStage.TargetChecksCompleted)
                        {
                            targetChecksCompleted = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.CandidateGuardBound)
                        {
                            Assert.That(privateCandidate, Is.Not.Null);
                            Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.True,
                                "The supported runtime operation is injected after graph checks and guard binding.");
                            targetStore = ReadPrivateField<FactionStore>(
                                privateCandidate.Runtime, "factionStore");
                            Assert.That(targetStore, Is.Not.Null);
                            IReadOnlyList<IOwnerSectionCensusProvider> targetProviders =
                                FactionStoreCensusProvider.CreateProviders(targetStore);
                            targetFactionBefore = targetProviders[0].GetCurrentCensus();
                            targetAffiliationBefore = targetProviders[1].GetCurrentCensus();
                            Assert.That(targetFactionBefore.SectionId,
                                Is.EqualTo(FactionStoreCensusProvider.FactionsSectionId));
                            Assert.That(targetAffiliationBefore.SectionId,
                                Is.EqualTo(FactionStoreCensusProvider.AffiliationsSectionId));
                            Assert.That(targetFactionBefore.SchemaVersion,
                                Is.EqualTo(FactionStoreCensusProvider.SchemaVersion));
                            Assert.That(targetAffiliationBefore.SchemaVersion,
                                Is.EqualTo(FactionStoreCensusProvider.SchemaVersion));
                            Assert.That(targetFactionBefore.OwnerInstanceIdentity, Is.SameAs(targetStore));
                            Assert.That(targetAffiliationBefore.OwnerInstanceIdentity, Is.SameAs(targetStore));
                            Assert.That(targetFactionBefore.OwnerInstanceIdentity,
                                Is.Not.SameAs(sourceFactionRow.OwnerInstanceIdentity));
                            Assert.That(targetAffiliationBefore.OwnerInstanceIdentity,
                                Is.Not.SameAs(sourceAffiliationRow.OwnerInstanceIdentity));
                            Assert.That(targetFactionBefore.Cardinality, Is.Zero);
                            Assert.That(targetAffiliationBefore.Cardinality, Is.Zero);
                            Assert.That(targetFactionBefore.Revision, Is.Zero);
                            Assert.That(targetAffiliationBefore.Revision, Is.Zero);
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochBefore,
                                out ContinuationCensusFailure censusBeforeFailure), Is.True,
                                censusBeforeFailure.ToString());

                            Assert.That(privateCandidate.Runtime.TryRegisterFaction(
                                new FactionRecord(
                                    injectedFactionId,
                                    "P12-G post-guard Faction",
                                    privateCandidate.Runtime.CurrentDay),
                                out FactionFoundationFailure factionFailure), Is.True,
                                factionFailure.ToString());
                            Assert.That(targetStore.Factions.Single().Id, Is.EqualTo(injectedFactionId));
                            targetFactionAfter = targetProviders[0].GetCurrentCensus();
                            targetAffiliationAfter = targetProviders[1].GetCurrentCensus();
                            Assert.That(privateCandidate.Runtime.TryReadNpcRosterCensusMutationEpoch(
                                out targetMutationEpochAfter,
                                out ContinuationCensusFailure censusAfterFailure), Is.True,
                                censusAfterFailure.ToString());
                            mutationInjected = true;
                            candidateGuardBound = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured)
                        {
                            targetVectorRecaptured = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.BoundaryAdmitted)
                        {
                            boundaryAdmitted = true;
                        }
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(targetChecksCompleted, Is.True);
            Assert.That(candidateGuardBound, Is.True);
            Assert.That(mutationInjected, Is.True);
            Assert.That(targetVectorRecaptured, Is.False,
                "A changed required Faction owner must reject before final owner-vector recapture.");
            Assert.That(boundaryAdmitted, Is.False);
            Assert.That(targetFactionBefore, Is.Not.Null);
            Assert.That(targetAffiliationBefore, Is.Not.Null);
            Assert.That(targetFactionAfter, Is.Not.Null);
            Assert.That(targetAffiliationAfter, Is.Not.Null);
            Assert.That(targetFactionAfter.SectionId, Is.EqualTo(targetFactionBefore.SectionId));
            Assert.That(targetAffiliationAfter.SectionId, Is.EqualTo(targetAffiliationBefore.SectionId));
            Assert.That(targetFactionAfter.SchemaVersion, Is.EqualTo(targetFactionBefore.SchemaVersion));
            Assert.That(targetAffiliationAfter.SchemaVersion, Is.EqualTo(targetAffiliationBefore.SchemaVersion));
            Assert.That(targetFactionAfter.OwnerInstanceIdentity, Is.SameAs(targetStore));
            Assert.That(targetAffiliationAfter.OwnerInstanceIdentity, Is.SameAs(targetStore));
            Assert.That(targetFactionAfter.Cardinality, Is.EqualTo(1));
            Assert.That(targetAffiliationAfter.Cardinality, Is.Zero);
            Assert.That(targetFactionAfter.Revision, Is.EqualTo(1L));
            Assert.That(targetAffiliationAfter.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.Count, Is.EqualTo(1));
            Assert.That(targetStore.AffiliationCount, Is.Zero);
            Assert.That(targetStore.Revision, Is.EqualTo(1L));
            Assert.That(targetMutationEpochBefore, Is.GreaterThanOrEqualTo(0L));
            Assert.That(targetMutationEpochAfter, Is.EqualTo(targetMutationEpochBefore + 1L),
                "The committed Faction owner operation must invalidate the shared epoch exactly once.");
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("target mutation epoch changed after whole-graph validation"));
            Assert.That(restored, Is.Null,
                "A private target whose required Faction owner changed must not reach publication.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));
            Assert.That(sourceStore.Count, Is.Zero);
            Assert.That(sourceStore.AffiliationCount, Is.Zero);
            Assert.That(sourceStore.Revision, Is.Zero);
            Assert.That(original.Runtime.FactionRecords.Any(record => record.Id == injectedFactionId), Is.False);

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            FactionStore retriedStore = ReadPrivateField<FactionStore>(retried.Runtime, "factionStore");
            Assert.That(retriedStore.Count, Is.Zero);
            Assert.That(retriedStore.AffiliationCount, Is.Zero);
            Assert.That(retriedStore.Revision, Is.Zero);
            Assert.That(retried.Runtime.FactionRecords.Any(record => record.Id == injectedFactionId), Is.False);
            string retriedGraph = CaptureCompleteDailyV1OwnerProjection(
                retried, GetCompletedDailyToken(retried.Runtime));
            Assert.That(retriedGraph, Is.EqualTo(sourceGraph));
            Assert.That(retriedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            Assert.That(retried.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retryAdvance),
                Is.True, retryAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextAdvance),
                Is.True, controlNextAdvance.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The rejected private Faction mutation must not change retry or later continuation parity.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
        }
    }

    [Test]
    public void DailyV1RestoreRejectsExplorableSiteAddedAfterTargetChecksAndKeepsSourceRetryable()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G P12-D site post-check target recapture source");
        GameObject controlObject = new GameObject("P12-G P12-D site post-check target recapture control");
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

            ExplorableSiteStore sourceStore = original.ExplorableSites;
            OwnerSectionCensusWitness sourceSiteRow = new ExplorableSiteCensusProvider(sourceStore)
                .GetCurrentCensus();
            Assert.That(sourceSiteRow.SectionId, Is.EqualTo(ExplorableSiteCensusProvider.SectionId));
            Assert.That(sourceSiteRow.SchemaVersion, Is.EqualTo(ExplorableSiteCensusProvider.SchemaVersion));
            Assert.That(sourceSiteRow.OwnerInstanceIdentity, Is.SameAs(sourceStore));
            Assert.That(sourceSiteRow.Cardinality, Is.Zero);
            Assert.That(sourceSiteRow.Revision, Is.Zero);

            Assert.That(original.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
                Is.True, sourceAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
                Is.True, controlAdvance.ToString());
            DailyCaptureEligibilityToken sourceToken = GetCompletedDailyToken(original.Runtime);
            string sourceGraph = CaptureCompleteDailyV1OwnerProjection(original, sourceToken);
            Assert.That(sourceGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));

            SimulationActiveSession privateCandidate = null;
            ExplorableSiteStore targetStore = null;
            ExplorableSiteRuntime injectedSite = null;
            OwnerSectionCensusWitness targetRowBeforeMutation = null;
            OwnerSectionCensusWitness targetRowAfterMutation = null;
            bool targetChecksCompleted = false;
            bool mutationInjected = false;
            bool targetVectorRecaptured = false;
            bool boundaryAdmitted = false;
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSession(
                    original,
                    out SimulationActiveSession restored,
                    out P12GDailyV1RestoreFailure failure,
                    out string diagnostic,
                    stage =>
                    {
                        if (stage == P12GDailyV1RestoreStage.TargetChecksCompleted)
                        {
                            targetChecksCompleted = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.CandidateGuardBound)
                        {
                            Assert.That(privateCandidate, Is.Not.Null);
                            Assert.That(privateCandidate.Runtime.AreCoreMutationGuardAuthoritiesBound, Is.True,
                                "The site mutation is injected after restored-graph checks and candidate guard binding.");
                            targetStore = privateCandidate.ExplorableSites;
                            targetRowBeforeMutation = new ExplorableSiteCensusProvider(targetStore)
                                .GetCurrentCensus();
                            Assert.That(targetRowBeforeMutation.SectionId,
                                Is.EqualTo(ExplorableSiteCensusProvider.SectionId));
                            Assert.That(targetRowBeforeMutation.SchemaVersion,
                                Is.EqualTo(ExplorableSiteCensusProvider.SchemaVersion));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
                            Assert.That(targetRowBeforeMutation.OwnerInstanceIdentity,
                                Is.Not.SameAs(sourceSiteRow.OwnerInstanceIdentity));
                            Assert.That(targetRowBeforeMutation.Cardinality, Is.Zero);
                            Assert.That(targetRowBeforeMutation.Revision, Is.Zero);

                            SpatialLocationRuntime targetLocation = privateCandidate.SpatialNetwork.Locations
                                .OrderBy(location => location.RuntimeId, StringComparer.Ordinal)
                                .FirstOrDefault();
                            Assert.That(targetLocation, Is.Not.Null,
                                "The synthetic site must bind to an already-staged legacy location.");
                            DailyV1ExplorableSiteDefinition definition = SimulationTestFactory.CreateExplorableSiteDefinition(
                                "p12g-private-target-late-site-definition",
                                ExplorableSiteKind.Generic);
                            injectedSite = new ExplorableSiteRuntime(
                                "p12g-private-target-late-site",
                                definition,
                                targetLocation);
                            Assert.That(targetStore.Add(injectedSite), Is.True,
                                "The supported ExplorableSiteStore writer must change the private target after guard binding.");
                            targetRowAfterMutation = new ExplorableSiteCensusProvider(targetStore)
                                .GetCurrentCensus();
                            mutationInjected = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.TargetOwnerVectorRecaptured)
                        {
                            targetVectorRecaptured = true;
                        }
                        else if (stage == P12GDailyV1RestoreStage.BoundaryAdmitted)
                        {
                            boundaryAdmitted = true;
                        }
                    },
                    candidate => privateCandidate = candidate), Is.False);

            Assert.That(targetChecksCompleted, Is.True);
            Assert.That(mutationInjected, Is.True);
            Assert.That(targetVectorRecaptured, Is.False,
                "A populated exact-empty target row must reject before final owner-vector recapture.");
            Assert.That(boundaryAdmitted, Is.False);
            Assert.That(targetRowBeforeMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation, Is.Not.Null);
            Assert.That(targetRowAfterMutation.SectionId, Is.EqualTo(targetRowBeforeMutation.SectionId));
            Assert.That(targetRowAfterMutation.SchemaVersion, Is.EqualTo(targetRowBeforeMutation.SchemaVersion));
            Assert.That(targetRowAfterMutation.OwnerInstanceIdentity, Is.SameAs(targetStore));
            Assert.That(targetRowAfterMutation.Cardinality, Is.EqualTo(1));
            Assert.That(targetRowAfterMutation.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.Count, Is.EqualTo(1));
            Assert.That(targetStore.Revision, Is.EqualTo(1L));
            Assert.That(targetStore.GetByRuntimeId("p12g-private-target-late-site"), Is.SameAs(injectedSite));
            Assert.That(failure, Is.EqualTo(P12GDailyV1RestoreFailure.TargetOwnerVectorFailed));
            Assert.That(diagnostic, Does.Contain("could not recapture its owner vector after mutation-guard binding"));
            Assert.That(diagnostic, Does.Contain("OwnerCoverageIncomplete"));
            Assert.That(restored, Is.Null,
                "A private target with a populated profile-excluded site owner must not reach publication.");
            Assert.That(ReadPrivateField<SimulationActiveSession>(source, "activeSession"), Is.SameAs(original));
            Assert.That(original.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(original.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);
            Assert.That(CaptureCompleteDailyV1OwnerProjection(original, sourceToken), Is.EqualTo(sourceGraph));
            Assert.That(sourceStore.Count, Is.Zero);
            Assert.That(sourceStore.Revision, Is.Zero);

            Assert.That(source.TryRestoreDailyContinuation(
                    out P12GDailyV1RestoreFailure retryFailure, out string retryDiagnostic), Is.True,
                retryFailure + ": " + retryDiagnostic);
            SimulationActiveSession retried = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(retried, Is.Not.SameAs(original));
            string retriedGraph = CaptureCompleteDailyV1OwnerProjection(
                retried, GetCompletedDailyToken(retried.Runtime));
            Assert.That(retriedGraph, Is.EqualTo(sourceGraph));
            Assert.That(retriedGraph, Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                controlSession, GetCompletedDailyToken(controlSession.Runtime))));
            Assert.That(retried.ExplorableSites.Count, Is.Zero);
            Assert.That(retried.ExplorableSites.Revision, Is.Zero);

            Assert.That(retried.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure retryAdvance),
                Is.True, retryAdvance.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlNextAdvance),
                Is.True, controlNextAdvance.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(
                    retried, GetCompletedDailyToken(retried.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(
                    controlSession, GetCompletedDailyToken(controlSession.Runtime))),
                "The rejected private site must not change source retry or later deterministic continuation.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
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
        if (profile == LocalMaterialFlowProfile.MixedSourcesDaily)
        {
            city.productionConfigs.Add(new CityProductionConfig
            {
                item = item, amountPerDay = 5, initialReserve = 0,
                productionSourceId = "source.p14.admission.exogenous",
                contentRevision = "p14-content-v1", sourceKind = CityProductionSourceKind.ExogenousDaily
            });
            city.productionConfigs.Add(new CityProductionConfig
            {
                item = item, amountPerDay = 3, initialReserve = initialReserve,
                productionSourceId = "source.p14.admission.finite",
                contentRevision = "p14-content-v1", sourceKind = CityProductionSourceKind.FiniteReserveDaily
            });
        }
        else
        {
            city.productionConfigs.Add(new CityProductionConfig
            {
                item = item,
                amountPerDay = 5,
                initialReserve = initialReserve,
                productionSourceId = "source.p14.admission",
                contentRevision = "p14-content-v1"
            });
        }
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

    private static void CaptureTerminalP11History(
        SimulationRuntime runtime,
        string commandId,
        string actionDefinitionId,
        bool bindDailyMerchant)
    {
        Assert.That(runtime, Is.Not.Null);
        PersonId personId = new PersonId("p12g-terminal-actor-" + commandId);
        Assert.That(runtime.TryRegisterPerson(
                new PersonRuntime(personId), out PersonStoreFailure personFailure), Is.True,
            personFailure.ToString());

        if (bindDailyMerchant)
        {
            NpcRuntime dailyMerchant = runtime.NpcRuntimes.Single(
                npc => npc?.NpcData?.DefinitionId == "PEDRO");
            Assert.That(runtime.TryBindExistingNpcToPerson(
                    personId,
                    dailyMerchant.RuntimeId,
                    out PersonMaterializationFailure bindingFailure), Is.True,
                bindingFailure.ToString());
        }

        ActorActionChoiceWorldCommandHandler handler = new ActorActionChoiceWorldCommandHandler(runtime);
        WorldCommandHandlerResult queued = handler.Execute(
            new WorldCommand(
                WorldCommandKind.ActorActionChoice,
                WorldCommandOrigin.LocalPlayer,
                WorldCommandAuthorityMode.Request,
                new ActorActionChoiceWorldCommandPayload(personId, actionDefinitionId)),
            new WorldCommandExecutionContext(commandId));
        Assert.That(queued.Success, Is.True, queued.Diagnostic);

        if (!bindDailyMerchant)
        {
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

    private static ActionExecutionContext CreateDailyTravelPartyContext(
        SimulationActiveSession session,
        params string[] excludedRuntimeIds)
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
                    && (excludedRuntimeIds == null
                        || !excludedRuntimeIds.Contains(npc.RuntimeId, StringComparer.Ordinal))
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
        snapshots.Add("D/Genealogy", new GenealogyOwnerSnapshot(
            GenealogyOwnerBridge.CaptureOwnerSnapshot(runtime.GenealogyStoreForWorldBoundary)));
        snapshots.Add("D/SpatialNetwork", session.SpatialNetwork.CaptureOwnerSnapshot());
        Assert.That(composition.ExplorableSites.TryCaptureOwnerSnapshot(
            out ExplorableSiteOwnerSnapshot siteSnapshot, out var siteFailure), Is.True, siteFailure?.Message);
        snapshots.Add("D/EmptyExplorableSites", siteSnapshot);

        Assert.That(P12EInstitutionOfficeCaptureAdapter.TryCapture(
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
        Assert.That(P12EFactionCaptureAdapter.TryCapture(
            runtime, GetRequiredOwner<FactionStore>(vector, FactionStoreCensusProvider.FactionsSectionId),
            token, vector,
            out P12EFactionOwnerSnapshot factionSnapshot,
            out P12EFactionSnapshotFailure factionFailure), Is.True,
            factionFailure?.Message);
        snapshots.Add("E/Factions", factionSnapshot);
        Assert.That(P12EPoliticalClaimCaptureAdapter.TryCapture(
            runtime, token, vector,
            out P12EPoliticalClaimOwnerSnapshot claimSnapshot,
            out P12EPoliticalClaimSnapshotFailure claimFailure), Is.True,
            claimFailure?.Message);
        snapshots.Add("E/PoliticalClaims", claimSnapshot);
        Assert.That(P12EPoliticalSupportCaptureAdapter.TryCapture(
            runtime, token, vector,
            out P12EPoliticalSupportOwnerSnapshot supportSnapshot,
            out P12EPoliticalSupportSnapshotFailure supportFailure), Is.True,
            supportFailure?.Message);
        snapshots.Add("E/PoliticalSupport", supportSnapshot);
        Assert.That(P12EPoliticalDecisionCaptureAdapter.TryCapture(
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
        Assert.That(P12EJusticeRecordsSnapshotAdapter.TryCapture(
            runtime, token, vector,
            out P12EJusticeRecordsOwnerSnapshot justiceSnapshot,
            out P12EJusticeSnapshotFailure justiceFailure), Is.True,
            justiceFailure?.Message);
        snapshots.Add("E/Justice", justiceSnapshot);
        Assert.That(P12ECrimeSocialAppraisalCaptureAdapter.TryCapture(
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

    [TestCase((int)P12GDailyV1RestoreStage.OwnersStaged)]
    [TestCase((int)P12GDailyV1RestoreStage.TargetChecksCompleted)]
    [TestCase((int)P12GDailyV1RestoreStage.BoundaryAdmitted)]
    public void CoreContinuationSaveRejectsOperationClosedByRestoreStageCallback(
        int invalidateAtValue)
    {
        P12GDailyV1RestoreStage invalidateAt = (P12GDailyV1RestoreStage)invalidateAtValue;
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-G Core save operation callback invalidation");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession sourceSession =
                ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(sourceSession?.CoreRuntimeSession, Is.Not.Null);
            foreach (CityRuntime city in sourceSession.Runtime.Cities)
            {
                Assert.That(city.SelectedRuntimeOwners, Is.Not.Null,
                    "The selected Daily-v1 save source must be bound to Core City owners.");

                OwnerSectionCensusWitness presenceWitness = null;
                foreach (IOwnerSectionCensusProvider provider in
                    sourceSession.Composition.CityNpcPresenceCensusProviders)
                {
                    OwnerSectionCensusWitness candidate = provider.GetCurrentCensus();
                    if (string.Equals(candidate.SectionId,
                        CityNpcPresenceCensusProvider.SectionIdFor(city.RuntimeId),
                        StringComparison.Ordinal))
                    {
                        presenceWitness = candidate;
                        break;
                    }
                }
                Assert.That(presenceWitness, Is.Not.Null);
                Assert.That(presenceWitness.OwnerInstanceIdentity,
                    Is.SameAs(city.SelectedRuntimeOwners.NpcPresence));
                Assert.That(presenceWitness.Cardinality,
                    Is.EqualTo(city.SelectedRuntimeOwners.NpcPresence.OrderedNpcRuntimeIds.Count));
                Assert.That(presenceWitness.Revision,
                    Is.EqualTo(city.SelectedRuntimeOwners.NpcPresence.Revision));

                OwnerSectionCensusWitness marketWitness = null;
                foreach (IOwnerSectionCensusProvider provider in
                    sourceSession.Composition.CityMarketCensusProviders)
                {
                    OwnerSectionCensusWitness candidate = provider.GetCurrentCensus();
                    if (string.Equals(candidate.SectionId,
                        CityMarketCensusProvider.SectionIdFor(city.RuntimeId),
                        StringComparison.Ordinal))
                    {
                        marketWitness = candidate;
                        break;
                    }
                }
                Assert.That(marketWitness, Is.Not.Null);
                Assert.That(marketWitness.OwnerInstanceIdentity,
                    Is.SameAs(city.SelectedRuntimeOwners.Market));
                Assert.That(marketWitness.Cardinality,
                    Is.EqualTo(city.SelectedRuntimeOwners.Market.Items.Count));
                Assert.That(marketWitness.Revision,
                    Is.EqualTo(city.SelectedRuntimeOwners.Market.Revision));
            }
            Assert.That(sourceSession.Composition.SpatialAuthority,
                Is.SameAs(sourceSession.CoreRuntimeSession.GenesisComposition.SpatialAuthority),
                "The admitted Unity runtime must retain Core's exact P12-C SpatialAuthorityStore owner.");
            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());

            DailyV1Application application =
                ReadPrivateField<DailyV1Application>(source, "dailyV1Application");
            DailyV1ActiveRuntimeSession coreSession = sourceSession.CoreRuntimeSession;
            Assert.That(application, Is.Not.Null);
            Assert.That(application.TryBeginContinuationSaveOperation(
                coreSession,
                out DailyV1P12GContinuationSaveOperation operation,
                out DailyCaptureEligibilityFailure beginFailure), Is.True, beginFailure.ToString());

            bool callbackInvoked = false;
            P12AContinuationImage image;
            SimulationActiveSession restored;
            P12GDailyV1RestoreFailure failure;
            string diagnostic;
            using (operation)
            {
                Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureImage(
                    sourceSession,
                    sourceSession.AdmissionContext,
                    operation,
                    out image,
                    out restored,
                    out failure,
                    out diagnostic,
                    stage =>
                    {
                        if (stage != invalidateAt) return;
                        callbackInvoked = true;
                        operation.Dispose();
                    }), Is.False,
                    "A closed Core continuation-save operation must not fall back to Unity token validation.");
            }

            Assert.That(callbackInvoked, Is.True,
                invalidateAt + ": " + failure + ": " + diagnostic);
            Assert.That(image, Is.Null);
            Assert.That(restored, Is.Null);
            Assert.That(failure, Is.Not.EqualTo(P12GDailyV1RestoreFailure.None), diagnostic);
            Assert.That(application.ActiveSession, Is.SameAs(coreSession));
            Assert.That(application.ActiveOperationCount, Is.Zero);
            Assert.That(sourceSession.Runtime.IsHealthyDailyOwnerThreadBoundary(), Is.True);

            Assert.That(application.TryBeginContinuationSaveOperation(
                coreSession,
                out DailyV1P12GContinuationSaveOperation retry,
                out DailyCaptureEligibilityFailure retryFailure), Is.True, retryFailure.ToString());
            retry.Dispose();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }

    [Test]
    public void PersistedDailyV1ImageRestoresWithoutUsingSourceSessionAndContinuesIdentically()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject sourceObject = new GameObject("P12-A persisted source");
        GameObject controlObject = new GameObject("P12-A uninterrupted control");
        GameObject targetObject = new GameObject("P12-A detached target");
        GameObject idleObject = new GameObject("P12-A idle replacement target");
        string storageRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "P12ADetachedRestore", Guid.NewGuid().ToString("N"));
        Guid sharedWorldGuid = Guid.Parse("a4a3c5e4-5e64-4f2f-a758-398989a8d374");
        try
        {
            TesteSimulacao source = sourceObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(source, config);
            WritePrivateField(source, "worldIdentityAllocator", (Func<WorldId>)(() => new WorldId(sharedWorldGuid)));
            InvokeInitializeSimulation(source, null);
            SimulationActiveSession sourceSession = ReadPrivateField<SimulationActiveSession>(source, "activeSession");
            Assert.That(sourceSession, Is.Not.Null);
            Assert.That(sourceSession.CoreRuntimeSession?.GenesisComposition, Is.Not.Null,
                "A freshly bootstrapped source session must retain its exact Core genesis composition.");
            string expectedP9ManifestFingerprint = sourceSession.CoreRuntimeSession.P12CRoots.Manifest.Fingerprint;

            TesteSimulacao control = controlObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(control, config);
            WritePrivateField(control, "worldIdentityAllocator", (Func<WorldId>)(() => new WorldId(sharedWorldGuid)));
            InvokeInitializeSimulation(control, null);
            SimulationActiveSession controlSession = ReadPrivateField<SimulationActiveSession>(control, "activeSession");
            Assert.That(controlSession, Is.Not.Null);

            Assert.That(sourceSession.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
                Is.True, advanceFailure.ToString());
            Assert.That(controlSession.Runtime.TryAdvanceDay(out advanceFailure), Is.True, advanceFailure.ToString());
            Assert.That(CaptureCompleteDailyV1OwnerProjection(sourceSession,
                    GetCompletedDailyToken(sourceSession.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession,
                    GetCompletedDailyToken(controlSession.Runtime))));
            Assert.That(sourceSession.CoreRuntimeSession.GenesisComposition, Is.Not.Null,
                "The original Core session must retain its genesis roots after the completed advance.");
            Assert.That(source.TrySaveDailyV1ContinuationToPathForTest(storageRoot, out string saveCode), Is.True, saveCode);
            Assert.That(saveCode, Is.EqualTo(P12AResultCode.Saved.ToString()));

            UnityEngine.Object.DestroyImmediate(sourceObject);
            sourceObject = null;
            source = null;
            sourceSession = null;

            TesteSimulacao target = targetObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(target, config);
            int genesisIdentityAllocations = 0;
            WritePrivateField(target, "worldIdentityAllocator", (Func<WorldId>)(() =>
            {
                genesisIdentityAllocations++;
                return new WorldId(Guid.NewGuid());
            }));
            Assert.That(target.TryLoadSavedDailyV1ContinuationFromPathForTest(storageRoot), Is.True);
            Assert.That(genesisIdentityAllocations, Is.Zero,
                "Loading a persisted boundary must publish before and bypass genesis identity allocation.");

            SimulationActiveSession restored = ReadPrivateField<SimulationActiveSession>(target, "activeSession");
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.CoreRuntimeSession?.GenesisComposition, Is.Null,
                "A detached continuation must not be mislabeled as an initial genesis composition.");
            Assert.That(restored.CoreRuntimeSession?.P12CRoots, Is.Not.Null,
                "A detached continuation must retain its exact reconstructed P12-C roots for a later save.");
            Assert.That(restored.CoreRuntimeSession.P12CRoots.Manifest.Fingerprint,
                Is.EqualTo(expectedP9ManifestFingerprint));
            Assert.That(restored.Runtime, Is.Not.SameAs(controlSession.Runtime));
            Assert.That(restored.IdentityRegistry, Is.Not.SameAs(controlSession.IdentityRegistry));
            Assert.That(restored.Runtime.CurrentDay, Is.EqualTo(controlSession.Runtime.CurrentDay));
            Assert.That(restored.Runtime.PoliticalWorldRevision,
                Is.EqualTo(controlSession.Runtime.PoliticalWorldRevision));
            Assert.That(restored.ReportState.LastEconomySnapshotDay,
                Is.EqualTo(controlSession.ReportState.LastEconomySnapshotDay));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(restored,
                    GetCompletedDailyToken(restored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession,
                    GetCompletedDailyToken(controlSession.Runtime))));

            for (int boundary = 0; boundary < 2; boundary++)
            {
                Assert.That(controlSession.Runtime.TryAdvanceDay(out advanceFailure), Is.True, advanceFailure.ToString());
                Assert.That(restored.Runtime.TryAdvanceDay(out advanceFailure), Is.True, advanceFailure.ToString());
                Assert.That(restored.Runtime.CurrentDay, Is.EqualTo(controlSession.Runtime.CurrentDay));
                Assert.That(restored.Runtime.PoliticalWorldRevision,
                    Is.EqualTo(controlSession.Runtime.PoliticalWorldRevision));
                Assert.That(CaptureCompleteDailyV1OwnerProjection(restored,
                        GetCompletedDailyToken(restored.Runtime)),
                    Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(controlSession,
                        GetCompletedDailyToken(controlSession.Runtime))));
            }

            Assert.That(target.TrySaveDailyV1ContinuationToPathForTest(storageRoot, out saveCode), Is.True, saveCode);
            Assert.That(saveCode, Is.EqualTo(P12AResultCode.Saved.ToString()),
                "A restored session must be eligible for a later persisted boundary capture.");

            TesteSimulacao idleTarget = idleObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(idleTarget, config);
            InvokeInitializeSimulation(idleTarget, null);
            SimulationActiveSession preLoadIdleSession = ReadPrivateField<SimulationActiveSession>(idleTarget, "activeSession");
            Assert.That(preLoadIdleSession, Is.Not.Null);
            Assert.That(preLoadIdleSession.Runtime.TryAdvanceDay(out advanceFailure), Is.True,
                advanceFailure.ToString());
            Assert.That(idleTarget.TryLoadDailyV1ContinuationFromPathForTest(storageRoot, out string loadCode), Is.True,
                loadCode);
            Assert.That(loadCode, Is.EqualTo(P12AResultCode.Loaded.ToString()));
            SimulationActiveSession idleRestored = ReadPrivateField<SimulationActiveSession>(idleTarget, "activeSession");
            Assert.That(idleRestored, Is.Not.SameAs(preLoadIdleSession));
            Assert.That(idleRestored.Runtime.CurrentDay, Is.EqualTo(restored.Runtime.CurrentDay));
            Assert.That(CaptureCompleteDailyV1OwnerProjection(idleRestored,
                    GetCompletedDailyToken(idleRestored.Runtime)),
                Is.EqualTo(CaptureCompleteDailyV1OwnerProjection(restored,
                    GetCompletedDailyToken(restored.Runtime))));
        }
        finally
        {
            if (System.IO.Directory.Exists(storageRoot)) System.IO.Directory.Delete(storageRoot, true);
            if (sourceObject != null) UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(controlObject);
            UnityEngine.Object.DestroyImmediate(targetObject);
            UnityEngine.Object.DestroyImmediate(idleObject);
        }
    }
    [Test]
    public void PresentInvalidDailyV1SaveFailsClosedBeforeGenesis()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject targetObject = new GameObject("P12-A corrupt slot must not trigger genesis");
        string storageRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "P12ACorruptBootstrap", System.Guid.NewGuid().ToString("N"));
        try
        {
            TesteSimulacao target = targetObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(target, config);
            int genesisIdentityAllocations = 0;
            WritePrivateField(target, "worldIdentityAllocator", (Func<WorldId>)(() =>
            {
                genesisIdentityAllocations++;
                return new WorldId(System.Guid.NewGuid());
            }));

            string slot = P12AContinuationStore.SlotPath(storageRoot);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(slot));
            System.IO.File.WriteAllBytes(slot, new byte[] { 1, 2, 3, 4 });
            CountingReadFileOperations loadFileOperations = new CountingReadFileOperations();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "P12-A continuation load failed; genesis was not started. CorruptOrTruncated");
            Assert.That(target.TryLoadSavedDailyV1ContinuationFromPathForTest(storageRoot, loadFileOperations), Is.True,
                "A present slot is handled even when rejected, so Start cannot fall through to genesis.");
            Assert.That(loadFileOperations.OpenReadCount, Is.EqualTo(2),
                "The same injected file-operation port must handle slot inspection and continuation loading.");
            Assert.That(ReadPrivateField<bool>(target, "bootstrapFailed"), Is.True);
            Assert.That(ReadPrivateField<SimulationActiveSession>(target, "activeSession"), Is.Null);
            Assert.That(genesisIdentityAllocations, Is.Zero);
        }
        finally
        {
            if (System.IO.Directory.Exists(storageRoot)) System.IO.Directory.Delete(storageRoot, true);
            UnityEngine.Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void UninspectableDailyV1SaveFailsClosedBeforeGenesis()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        GameObject targetObject = new GameObject("P12-A uninspectable slot must not trigger genesis");
        string storageRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "P12AUninspectableBootstrap", System.Guid.NewGuid().ToString("N"));
        try
        {
            TesteSimulacao target = targetObject.AddComponent<TesteSimulacao>();
            ConfigureSelectedBootstrap(target, config);
            int genesisIdentityAllocations = 0;
            WritePrivateField(target, "worldIdentityAllocator", (Func<WorldId>)(() =>
            {
                genesisIdentityAllocations++;
                return new WorldId(System.Guid.NewGuid());
            }));

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "P12-A continuation slot inspection failed; genesis was not started.");
            Assert.That(target.TryLoadSavedDailyV1ContinuationFromPathForTest(
                storageRoot, new SlotInspectionFailureFileOperations()), Is.True);
            Assert.That(ReadPrivateField<bool>(target, "bootstrapFailed"), Is.True);
            Assert.That(ReadPrivateField<SimulationActiveSession>(target, "activeSession"), Is.Null);
            Assert.That(genesisIdentityAllocations, Is.Zero);
        }
        finally
        {
            if (System.IO.Directory.Exists(storageRoot)) System.IO.Directory.Delete(storageRoot, true);
            UnityEngine.Object.DestroyImmediate(targetObject);
        }
    }

    private static void ConfigureUnscopedBootstrap(TesteSimulacao bootstrap, SimulationConfigData config)
    {
        WritePrivateField(bootstrap, "simulationConfig", config);
        WritePrivateField(bootstrap, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.None);
        WritePrivateField<SimulationRuntimeAdmissionContext>(bootstrap, "runtimeAdmissionContext", null);
    }

    private static void ConfigureSelectedBootstrap(TesteSimulacao bootstrap, SimulationConfigData config)
    {
        WritePrivateField(bootstrap, "simulationConfig", config);
        WritePrivateField(bootstrap, "runtimeAdmissionProfile", SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        DailyV1Application application = ReadPrivateField<DailyV1Application>(bootstrap, "dailyV1Application");
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext admissionContext), Is.True);
        WritePrivateField(bootstrap, "runtimeAdmissionContext", admissionContext);
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

    private static NpcActionData[] CreatePositiveNormalProbeActions(string idPrefix, NpcRuntime npc)
    {
        if (string.IsNullOrWhiteSpace(idPrefix)) throw new ArgumentException("A probe action prefix is required.", nameof(idPrefix));
        if (npc == null) throw new ArgumentNullException(nameof(npc));

        NpcActionData first = SimulationTestFactory.CreateAction(idPrefix + "-a", NpcActionType.Normal);
        NpcActionData second = SimulationTestFactory.CreateAction(idPrefix + "-b", NpcActionType.Normal);
        first.baseUtility = 1f;
        second.baseUtility = 1f;
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = first, baseUtility = 1f });
        npc.NpcData.acoesPadrao.Add(new NPCDefaultAction { action = second, baseUtility = 1f });
        return new[] { first, second };
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
        // These capture/admission tests exercise the accepted Daily-v1
        // continuation profile. Compose its real fixed owners and complete
        // dynamic owner families instead of creating a partial runtime that
        // must fail closed under the current P12 inventory contract.
        List<CityRuntime> cities = new List<CityRuntime>();
        if (npcRuntimes != null)
        {
            foreach (NpcRuntime npc in npcRuntimes)
            {
                CityRuntime currentCity = npc?.CurrentCity;
                if (currentCity != null && !cities.Any(city => ReferenceEquals(city, currentCity)))
                    cities.Add(currentCity);
            }
        }
        for (int index = 0; cities.Count < 2; index++)
        {
            CityRuntime city = SimulationTestFactory.CreateCity(
                "daily-capture-city-" + index,
                "daily-capture-location-" + index);
            if (cities.Any(existing => string.Equals(existing.RuntimeId, city.RuntimeId, StringComparison.Ordinal)))
                continue;
            cities.Add(city);
        }

        List<NpcRuntime> npcs = npcRuntimes != null
            ? new List<NpcRuntime>(npcRuntimes)
            : new List<NpcRuntime>();
        for (int index = 0; npcs.Count < 10; index++)
        {
            string runtimeId = "daily-capture-npc-" + index;
            if (npcs.Any(npc => npc != null && string.Equals(npc.RuntimeId, runtimeId, StringComparison.Ordinal)))
                continue;
            npcs.Add(new NpcRuntime(
                runtimeId,
                SimulationTestFactory.CreateNpc("definition-" + runtimeId),
                cities[index % cities.Count],
                0f));
        }

        RuntimeIdentityRegistry identities = new RuntimeIdentityRegistry();
        foreach (CityRuntime city in cities)
            Assert.That(identities.RegisterCity(city), Is.True, city.RuntimeId);
        foreach (NpcRuntime npc in npcs)
            Assert.That(identities.RegisterNpc(npc), Is.True, npc?.RuntimeId);

        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identities);
        foreach (CityRuntime city in cities)
            Assert.That(network.RegisterLocation(city.Location), Is.True, city.Location.RuntimeId);
        for (int index = 0; index < cities.Count; index++)
        {
            CityRuntime origin = cities[index];
            CityRuntime destination = cities[(index + 1) % cities.Count];
            Assert.That(network.RegisterRoute(new SpatialRouteRuntime(
                "daily-capture-route-" + index,
                origin.Location,
                destination.Location,
                1)), Is.True);
        }

        RecordFixture records = SimulationTestFactory.CreateRecordFixtureWithTime(
            time ?? new SimulationTime());
        EconomyTransactionService transactionService = new EconomyTransactionService();
        TravelSystem travelSystem = new TravelSystem(
            network,
            location => cities.FirstOrDefault(city => ReferenceEquals(city.Location, location)),
            1f,
            records.EventRecorder,
            null,
            transactionService);
        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(records.Time);
        ScheduledDirectiveSystem directiveSystem = new ScheduledDirectiveSystem(directiveStore, identities);
        JusticeSystem justiceSystem = new JusticeSystem(null, null, null, null,
            records.EventRecorder, null);
        CrimeSystem crimeSystem = new CrimeSystem(
            justiceSystem,
            travelSystem,
            null,
            transactionService: transactionService,
            configuration: new EffectiveCrimeConfiguration(false, false),
            simulationTime: records.Time);
        TravelPartyStore travelPartyStore = new TravelPartyStore();
        TravelPartySystem travelPartySystem = new TravelPartySystem(
            travelPartyStore,
            records.Allocator,
            identities,
            travelSystem,
            records.Time,
            records.Sequence,
            records.EventRecorder,
            transactionService: transactionService);
        ExplorableSiteStore siteStore = new ExplorableSiteStore();
        ExplorableSiteKnowledgeSystem siteKnowledgeSystem = new ExplorableSiteKnowledgeSystem();
        ExpeditionSystem expeditionSystem = new ExpeditionSystem(
            new ExpeditionStore(),
            records.Allocator,
            identities,
            siteStore,
            travelPartySystem,
            travelPartyStore,
            siteKnowledgeSystem,
            records.Time,
            records.EventRecorder);

        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            cities,
            npcs,
            economyEnabled: false,
            configuredActions: configuredActions,
            scheduledDirectiveSystem: directiveSystem,
            npcDecisionSystem: npcDecisionSystem,
            justiceSystem: justiceSystem,
            crimeSystem: crimeSystem,
            travelSystem: travelSystem,
            travelPartySystem: travelPartySystem,
            decisionRecorder: records.DecisionRecorder,
            explorableSiteStore: siteStore,
            explorableSiteKnowledgeSystem: siteKnowledgeSystem,
            expeditionSystem: expeditionSystem,
            spatialAuthorityStore: CreateSingleLocationSpatialAuthority(),
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            economyTransactionService: transactionService,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            runtimeIdentityRegistry: identities,
            spatialNetworkRuntime: network,
            requireP12RuntimeIdentitySpatialCensusOwners: true,
            requireP12ReceiptCensusOwners: true,
            worldId: worldId ?? new WorldId(Guid.NewGuid()));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialFailure), Is.True,
            "The complete selected-profile test fixture must satisfy owner census before publication: " + initialFailure);
        return runtime;
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
        if (field != null)
            return (T)field.GetValue(target);

        // Core extraction moves some runtime values behind read-only properties.
        // Keep these white-box assertions bound to the same semantic member
        // without requiring the production runtime to retain duplicate fields.
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, "Expected private field or property '" + name + "'.");
        MethodInfo getter = property.GetGetMethod(nonPublic: true);
        Assert.That(getter, Is.Not.Null, "Expected a readable private property '" + name + "'.");
        return (T)property.GetValue(target);
    }

    private static NpcRuntime CreateSelectedDailyV1RosterMember(NpcRuntime admittedTemplate, string runtimeId)
    {
        Assert.That(admittedTemplate, Is.Not.Null);
        object definition = ReadPrivateField<object>(admittedTemplate, "selectedDailyV1Definition");
        object definitions = ReadPrivateField<object>(admittedTemplate, "selectedDefinitionGraph");
        object bindings = ReadPrivateField<object>(admittedTemplate, "selectedDefinitionBindings");
        MethodInfo factory = typeof(NpcRuntime).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method =>
            {
                if (method.Name != "CreateSelectedDailyV1") return false;
                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 6
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(DailyV1NpcDefinition)
                    && parameters[2].ParameterType == typeof(DailyV1EffectiveDefinitions)
                    && parameters[3].ParameterType == typeof(DailyV1DefinitionAdapter.SelectedBindings)
                    && parameters[4].ParameterType == typeof(CityRuntime)
                    && parameters[5].ParameterType == typeof(float);
            });
        return (NpcRuntime)factory.Invoke(null, new object[]
        {
            runtimeId, definition, definitions, bindings, admittedTemplate.CurrentCity, 0f
        });
    }

    private static void WritePrivateField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private sealed class SlotInspectionFailureFileOperations : IAtomicFileSlotOperations
    {
        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => throw new NotSupportedException();
        public Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public Stream OpenRead(string path) => throw new UnauthorizedAccessException("Injected slot inspection failure.");
        public void FlushToDisk(Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
    }

    private sealed class CountingReadFileOperations : IAtomicFileSlotOperations
    {
        private readonly IAtomicFileSlotOperations inner = SystemAtomicFileSlotOperations.Instance;
        internal int OpenReadCount { get; private set; }

        public void CreateDirectory(string path) => inner.CreateDirectory(path);
        public bool Exists(string path) => inner.Exists(path);
        public Stream CreateNewTemporary(string path) => inner.CreateNewTemporary(path);
        public Stream OpenRead(string path)
        {
            OpenReadCount++;
            return inner.OpenRead(path);
        }
        public void FlushToDisk(Stream stream) => inner.FlushToDisk(stream);
        public void Replace(string source, string destination) => inner.Replace(source, destination);
        public void Move(string source, string destination) => inner.Move(source, destination);
        public void Delete(string path) => inner.Delete(path);
    }

    private sealed class AdmissionProbeRandomSource : IAuthoritativeRandomSource
    {
        private readonly IAuthoritativeRandomSource inner = new DeterministicRandomSource();
        private readonly string decisionStreamPrefix;
        private readonly Action observeDecision;

        public AdmissionProbeRandomSource(string actorRuntimeId, Action observeDecision)
        {
            if (string.IsNullOrWhiteSpace(actorRuntimeId))
                throw new ArgumentException("An observed actor runtime identity is required.", nameof(actorRuntimeId));
            decisionStreamPrefix = "npc-decision|" + actorRuntimeId + "|";
            this.observeDecision = observeDecision;
        }

        public float NextUnit(string streamKey, long drawIndex = 0L)
        {
            float value = inner.NextUnit(streamKey, drawIndex);
            if (observeDecision != null
                && streamKey != null
                && streamKey.StartsWith(decisionStreamPrefix, StringComparison.Ordinal))
                observeDecision();
            return value;
        }

        public DeterministicRandomStream CreateStream(string streamKey)
        {
            return inner.CreateStream(streamKey);
        }
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
