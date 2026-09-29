using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P18DConsumerIntegrationTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void IntradaySellChoiceExecutesAtCapturedTickAndTimelineOwnsClock()
    {
        Fixture fixture = CreateFixture();
        SetActorAtCity(fixture);
        fixture.Runtime.AdvanceDay();
        LogicalTick capturedTick = new LogicalTick(LogicalTick.TicksPerDay + 17L);

        Assert.That(fixture.Runtime.TryCaptureIntradayActorChoice(
            "intraday-sell-command", fixture.Actor.PersonId, fixture.SellAction.DefinitionId,
            capturedTick, out TimelineFailure captureFailure), Is.True, captureFailure.ToString());
        Assert.That(fixture.Runtime.TryAdvanceIntradayTo(capturedTick, out TimelineFailure advanceFailure),
            Is.True, advanceFailure.ToString());

        ActorChoiceInput input = fixture.Runtime.ActorChoiceStore.Inputs.Single();
        Assert.That(input.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned),
            string.Join(";", input.TemporalDispositions.Select(disposition => disposition.Kind + ":" + disposition.Failure)));
        Assert.That(input.TemporalCapture.TargetInstant, Is.EqualTo(capturedTick));
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(fixture.Records.Decisions.Decisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.ActorChoice));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(5));
        Assert.That(fixture.Runtime.P18DTimeline.CurrentInstant, Is.EqualTo(capturedTick));
        Assert.That(fixture.Runtime.CurrentDay, Is.EqualTo(1L));

        Assert.That(fixture.Runtime.SimulationTime.TryAdvanceDay(out SimulationTimeAdvanceFailure clockFailure), Is.False);
        Assert.That(clockFailure, Is.EqualTo(SimulationTimeAdvanceFailure.TimelineProjectionOwnsClock));
        fixture.Runtime.AdvanceDay();
        Assert.That(fixture.Runtime.CurrentDay, Is.EqualTo(2L));
        Assert.That(fixture.Runtime.SimulationTime.AbsoluteDay, Is.EqualTo(2L));
        Assert.That(fixture.Runtime.P18DTimeline.CurrentInstant.Value % LogicalTick.TicksPerDay, Is.Zero);
    }

    [Test]
    public void TrustedWorldCommandCapturesAtNextUnsealedTickAndExecutesIntraday()
    {
        Fixture fixture = CreateFixture();
        SetActorAtCity(fixture);
        fixture.Runtime.AdvanceDay();
        WorldCommandService service = new WorldCommandService(simulationTime: fixture.Runtime.SimulationTime);
        Assert.That(TrustedLocalUiWorldCommandHandlerRegistration.RegisterActorActionChoiceHandler(
            service, fixture.Runtime), Is.True);
        WorldCommand command = new WorldCommand(
            WorldCommandKind.ActorActionChoice,
            WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request,
            new ActorActionChoiceWorldCommandPayload(fixture.Actor.PersonId,
                fixture.SellAction.DefinitionId));

        WorldCommandResult queued = service.Execute(command);

        Assert.That(queued.Success, Is.True, queued.Diagnostic);
        LogicalTick target = new LogicalTick(LogicalTick.TicksPerDay + 1L);
        TimelineInputReference queuedInput = fixture.Runtime.P18DTimeline.PreviewInputs(target).Single();
        Assert.That(queuedInput.CommandKind, Is.EqualTo(ActorChoiceTemporalInputOwner.CommandKind));
        Assert.That(queuedInput.TargetInstant, Is.EqualTo(target));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(10));

        fixture.Runtime.AdvanceDay();

        Assert.That(fixture.Runtime.ActorChoiceStore.Inputs.Single().Status,
            Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(5));
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(1));
    }

    [Test]
    public void ThrownSuccessRollIsTerminalizedOnceBeforeItEscapes()
    {
        Fixture fixture = CreateFixture(throwOnActionSuccess: true);
        SetActorAtCity(fixture);
        fixture.Runtime.AdvanceDay();
        LogicalTick capturedTick = new LogicalTick(LogicalTick.TicksPerDay + 19L);
        Assert.That(fixture.Runtime.TryCaptureIntradayActorChoice(
            "intraday-sell-throws", fixture.Actor.PersonId, fixture.SellAction.DefinitionId,
            capturedTick, out TimelineFailure captureFailure), Is.True, captureFailure.ToString());

        Assert.Throws<System.InvalidOperationException>(() =>
        {
            fixture.Runtime.TryAdvanceIntradayTo(capturedTick, out _);
        });

        ActorChoiceInput thrown = fixture.Runtime.ActorChoiceStore.Inputs.Single();
        Assert.That(thrown.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptThrew));
        Assert.That(thrown.TemporalDispositions.Select(disposition => disposition.Kind),
            Does.Contain(ActorChoiceTemporalDispositionKind.AttemptThrew));
        Assert.That(fixture.Runtime.P18DActorDecisionReceipts.Last().Kind,
            Is.EqualTo(ActorDecisionRequestReceiptKind.TerminalReconciled));
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(10));

        LogicalTick recoveredTarget = new LogicalTick(capturedTick.Value + 1L);
        Assert.That(fixture.Runtime.TryAdvanceIntradayTo(recoveredTarget, out TimelineFailure retryFailure),
            Is.True, retryFailure.ToString());
        Assert.That(fixture.Runtime.P18DTimeline.CurrentInstant, Is.EqualTo(recoveredTarget));
        Assert.That(fixture.Runtime.ActorChoiceStore.Inputs.Single().Status,
            Is.EqualTo(ActorChoiceInputStatus.AttemptThrew));
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(1));
    }

    [Test]
    public void DailyBoundaryRunsBeforeActorChoiceHandoffAtTheSameInstant()
    {
        Fixture fixture = CreateFixture();
        SetActorAtCity(fixture);
        LogicalTick boundary = new LogicalTick(LogicalTick.TicksPerDay);
        Assert.That(fixture.Runtime.TryCaptureIntradayActorChoice(
            "intraday-sell-at-boundary", fixture.Actor.PersonId, fixture.SellAction.DefinitionId,
            boundary, out TimelineFailure captureFailure), Is.True, captureFailure.ToString());

        Assert.That(fixture.Runtime.TryAdvanceIntradayTo(boundary, out TimelineFailure advanceFailure),
            Is.True, advanceFailure.ToString());

        ActorChoiceInput input = fixture.Runtime.ActorChoiceStore.Inputs.Single();
        Assert.That(input.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(fixture.Runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(5));
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(1));
    }

    [Test]
    public void CurrentLocationTruthRejectsActorOutsideItsCurrentCity()
    {
        Fixture fixture = CreateFixture();
        SetActorAtOtherLocation(fixture);
        fixture.Runtime.AdvanceDay();
        LogicalTick capturedTick = new LogicalTick(LogicalTick.TicksPerDay + 23L);
        Assert.That(fixture.Runtime.TryCaptureIntradayActorChoice(
            "intraday-sell-after-move", fixture.Actor.PersonId, fixture.SellAction.DefinitionId,
            capturedTick, out TimelineFailure captureFailure), Is.True, captureFailure.ToString());

        Assert.That(fixture.Runtime.TryAdvanceIntradayTo(capturedTick, out TimelineFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        ActorChoiceInput rejected = fixture.Runtime.ActorChoiceStore.Inputs.Single();
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.TemporalDispositions.Single().Failure,
            Is.EqualTo(ActorChoiceFailure.ActorUnavailable));
        Assert.That(fixture.Records.Decisions.Decisions, Is.Empty);
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(10));
    }

    [Test]
    public void ActiveMerchantPlanRejectsFreshActorChoiceWithoutSaleEffects()
    {
        Fixture fixture = CreateFixture();
        SetActorAtCity(fixture);
        fixture.Runtime.AdvanceDay();
        fixture.Actor.SetMerchantTradePlan(fixture.Item, fixture.City, fixture.City, 5, 1f);
        LogicalTick capturedTick = new LogicalTick(LogicalTick.TicksPerDay + 29L);
        Assert.That(fixture.Runtime.TryCaptureIntradayActorChoice(
            "intraday-sell-with-plan", fixture.Actor.PersonId, fixture.SellAction.DefinitionId,
            capturedTick, out TimelineFailure captureFailure), Is.True, captureFailure.ToString());

        Assert.That(fixture.Runtime.TryAdvanceIntradayTo(capturedTick, out TimelineFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        ActorChoiceInput rejected = fixture.Runtime.ActorChoiceStore.Inputs.Single();
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.TemporalDispositions.Single().Failure,
            Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
        Assert.That(fixture.Records.Decisions.Decisions, Is.Empty);
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(10));
    }

    [Test]
    public void CommittedSaleReplayAfterPlanActivationReturnsReceiptWithoutRepeatingEffects()
    {
        Fixture fixture = CreateFixture();
        SetActorAtCity(fixture);
        fixture.Runtime.AdvanceDay();
        NpcActionRuntime action = fixture.Decisions.CreateRequestedAction(
            fixture.Actor, fixture.SellAction);
        Assert.That(action, Is.Not.Null);
        Assert.That(action.TargetCity, Is.SameAs(fixture.City));
        Assert.That(action.TargetItem, Is.SameAs(fixture.Item));

        string proposalId = "p18d-replay-proposal";
        string fingerprint = "p18d-replay-fingerprint";
        string inputId = "p18d-replay-input";
        string requestId = "p18d-replay-request";
        LogicalTick instant = new LogicalTick(fixture.Runtime.P18DTimeline.CurrentInstant.Value + 7L);
        Assert.That(fixture.Merchant.TryExecuteKeyedLocalMarketSale(
            fixture.Actor, fixture.Actor.PersonId, action, proposalId, fingerprint,
            inputId, requestId, "UnityBootstrap-Daily-v1", instant,
            out KeyedSaleReceipt committed), Is.True);
        Assert.That(committed.Outcome, Is.EqualTo(KeyedSaleOutcome.Committed));
        int inventoryAfterFirst = fixture.Actor.Inventory.GetAmount(fixture.Item);
        int marketAfterFirst = fixture.City.Market.GetAmount(fixture.Item);
        float balanceAfterFirst = fixture.Actor.Money;

        fixture.Actor.SetMerchantTradePlan(fixture.Item, fixture.City, fixture.City, 5, 1f);
        Assert.That(fixture.Merchant.TryExecuteKeyedLocalMarketSale(
            fixture.Actor, fixture.Actor.PersonId, action, proposalId, fingerprint,
            inputId, requestId, "UnityBootstrap-Daily-v1", instant,
            out KeyedSaleReceipt replay), Is.True);

        Assert.That(replay, Is.SameAs(committed));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(inventoryAfterFirst));
        Assert.That(fixture.City.Market.GetAmount(fixture.Item), Is.EqualTo(marketAfterFirst));
        Assert.That(fixture.Actor.Money, Is.EqualTo(balanceAfterFirst));
    }

    [Test]
    public void IntradayCompositionRejectsP14LocalMaterialFlowBeforeAnyBoundaryMutation()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ItemData item = SimulationTestFactory.CreateItem("p18d-p14-excluded-item", 10f);
        string locationId = "p18d-p14-excluded-location";
        CityData data = SimulationTestFactory.CreateCityData("p18d-p14-excluded-city",
            SimulationTestFactory.CreateMarketItem(item, 6, 20));
        data.settlementSemanticId = "settlement.p18d-excluded";
        data.marketStoreSemanticId = "store.p18d-excluded";
        data.materialFlowLocationId = locationId;
        data.initialPopulation = 1000;
        data.marketItems[0].consumptionPer1000Population = 2f;
        data.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = 3,
            productionSourceId = "source.p18d-excluded",
            contentRevision = "content-v1"
        });
        CityRuntime city = new CityRuntime("runtime.p18d-p14-excluded", data,
            new SpatialLocationRuntime("legacy.p18d-p14-excluded"));
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        Assert.That(spatial.TryRegisterHex(new HexRecord(new HexId("hex.p18d-p14-excluded")), out _), Is.True);
        Assert.That(spatial.TryRegisterLocation(new LocationRecord(new LocationId(locationId),
            new HexId("hex.p18d-p14-excluded")), out _), Is.True);
        LegacySpatialAnchorBindingStore anchors = new LegacySpatialAnchorBindingStore(spatial);
        Assert.That(anchors.TryBindCity(city.RuntimeId, new LocationId(locationId), out _), Is.True);

        int openingStock = city.Market.GetAmount(item);
        int openingPopulation = city.Population.CurrentPopulation;
        MerchantSystem merchant = SimulationTestFactory.CreateMerchantSystem(
            null, records.Time, records.DecisionRecorder, maxTradeAmount: 5);
        NpcDecisionSystem decisions = new NpcDecisionSystem(new List<INpcActionProvider> { merchant });
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(false),
                merchantTrade: new MerchantTradeConfigurationOverrides(enabled: true)));

        Assert.Throws<System.ArgumentException>(() => new SimulationRuntime(
            records.Time, new[] { city }, null,
            npcDecisionSystem: decisions,
            decisionRecorder: records.DecisionRecorder,
            merchantSystem: merchant,
            configuration: configuration,
            spatialAuthorityStore: spatial,
            legacySpatialAnchorBindingStore: anchors,
            p18dIntradayProfile: new P18DIntradayProfile(
                "p18d-p14-world", "p18d-p14-profile", "config-v1", "content-v1")));

        Assert.That(records.Time.AbsoluteDay, Is.Zero);
        Assert.That(city.LastMaterialFlow, Is.Null);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(openingStock));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(openingPopulation));
    }

    [Test]
    public void SharedActivityCompletionTriggersDeferredChoicesForEachPersonId()
    {
        Fixture fixture = CreateFixture(includeSecondActor: true);
        SetActorsAtCity(fixture);
        fixture.Runtime.AdvanceDay();
        LogicalTick start = new LogicalTick(LogicalTick.TicksPerDay + 10L);
        LogicalTick dueChoice = new LogicalTick(LogicalTick.TicksPerDay + 20L);
        LogicalTick completion = new LogicalTick(LogicalTick.TicksPerDay + 30L);
        Assert.That(fixture.Runtime.P18DActivityLifecycleStore.TryPropose(
            new ActivityDefinition("shared-work", "v1"), "shared-work-instance",
            out ActivityInstanceSnapshot proposed, out ActivityFailure proposeFailure), Is.True, proposeFailure.ToString());
        Assert.That(fixture.Runtime.P18DActivityLifecycleStore.TrySchedule(
            fixture.Runtime.P18DTimeline, proposed.Id, fixture.Runtime.P18DTimeline.CurrentInstant,
            start, new LogicalTick(20L), new[] { fixture.Actor.PersonId.Value, fixture.SecondActor.PersonId.Value },
            out ActivityFailure scheduleFailure), Is.True, scheduleFailure.ToString());

        Assert.That(fixture.Runtime.TryCaptureIntradayActorChoice(
            "intraday-shared-choice-a", fixture.Actor.PersonId, fixture.SellAction.DefinitionId,
            dueChoice, out TimelineFailure firstCaptureFailure), Is.True, firstCaptureFailure.ToString());
        Assert.That(fixture.Runtime.TryCaptureIntradayActorChoice(
            "intraday-shared-choice-b", fixture.SecondActor.PersonId, fixture.SellAction.DefinitionId,
            dueChoice, out TimelineFailure secondCaptureFailure), Is.True, secondCaptureFailure.ToString());
        Assert.That(fixture.Runtime.TryAdvanceIntradayTo(dueChoice, out TimelineFailure deferFailure),
            Is.True, deferFailure.ToString());

        IReadOnlyList<ActorChoiceInput> deferred = fixture.Runtime.ActorChoiceStore.Inputs;
        Assert.That(deferred, Has.Count.EqualTo(2));
        Assert.That(deferred.All(input => input.Status == ActorChoiceInputStatus.Pending), Is.True);
        Assert.That(fixture.Runtime.P18DActorDecisionReceipts.Count(receipt =>
            receipt.Kind == ActorDecisionRequestReceiptKind.Deferred), Is.EqualTo(2));
        Assert.That(fixture.Runtime.TryAdvanceIntradayTo(completion, out TimelineFailure completionFailure),
            Is.True, completionFailure.ToString());

        IReadOnlyList<ActorChoiceInput> completed = fixture.Runtime.ActorChoiceStore.Inputs;
        Assert.That(completed.All(input => input.Status == ActorChoiceInputStatus.AttemptReturned), Is.True);
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(2));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(5));
        Assert.That(fixture.SecondActor.Inventory.GetAmount(fixture.Item), Is.EqualTo(5));
        ActorDecisionRequestReceipt[] activityTriggers = fixture.Runtime.P18DActorDecisionReceipts
            .Where(receipt => receipt.Kind == ActorDecisionRequestReceiptKind.TriggerObserved).ToArray();
        Assert.That(activityTriggers, Has.Length.EqualTo(2));
        Assert.That(activityTriggers.Select(receipt => receipt.Actor.Value).Distinct().Count(), Is.EqualTo(2));
        Assert.That(activityTriggers.Select(receipt => receipt.RequestId).Distinct().Count(), Is.EqualTo(2));
        Assert.That(fixture.Runtime.P18DActivityLifecycleStore.SnapshotTransitionReceipts().Last().ParticipantIds,
            Is.EquivalentTo(new[] { fixture.Actor.PersonId.Value, fixture.SecondActor.PersonId.Value }));
    }

    private static Fixture CreateFixture(bool throwOnActionSuccess = false, bool includeSecondActor = false)
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ItemData item = SimulationTestFactory.CreateItem("p18d-intraday-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "p18d-intraday-city", "p18d-intraday-location", 1000f,
            SimulationTestFactory.CreateMarketItem(item, 100, 100));
        NpcActionData sellAction = SimulationTestFactory.CreateAction(
            "p18d-intraday-sell", NpcActionType.SellGoods, NpcActionCategory.Commerce);
        if (throwOnActionSuccess)
        {
            sellAction.canFail = true;
            sellAction.baseSuccessChance = 0.5f;
        }
        MerchantSystem merchantSystem = SimulationTestFactory.CreateMerchantSystem(
            null, records.Time, records.DecisionRecorder, maxTradeAmount: 5);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(
            new List<INpcActionProvider> { merchantSystem });
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(false),
                merchantTrade: new MerchantTradeConfigurationOverrides(enabled: true)));
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time, new[] { city }, null,
            configuredActions: new[] { sellAction },
            npcDecisionSystem: decisionSystem,
            decisionRecorder: records.DecisionRecorder,
            merchantSystem: merchantSystem,
            configuration: configuration,
            randomSource: throwOnActionSuccess ? new ThrowingActionSuccessRandomSource() : null,
            p18dIntradayProfile: new P18DIntradayProfile(
                "p18d-test-world", "UnityBootstrap-Daily-v1", "test-config-v1", "test-content-v1"));

        PersonId personId = new PersonId("p18d-intraday-person");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(personId), out PersonStoreFailure registrationFailure),
            Is.True, registrationFailure.ToString());
        Assert.That(runtime.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("p18d-intraday-merchant", NpcJobType.Merchant, MerchantBehavior.Local),
            "p18d-intraday-npc", city, 0f,
            out NpcRuntime actor, out PersonMaterializationFailure materializationFailure),
            Is.True, materializationFailure.ToString());
        actor.Inventory.AddItem(item, 10, 1f);
        NpcRuntime secondActor = null;
        if (includeSecondActor)
        {
            PersonId secondPersonId = new PersonId("p18d-intraday-person-b");
            Assert.That(runtime.TryRegisterPerson(new PersonRuntime(secondPersonId), out registrationFailure),
                Is.True, registrationFailure.ToString());
            Assert.That(runtime.TryMaterializePerson(
                secondPersonId,
                SimulationTestFactory.CreateNpc("p18d-intraday-merchant-b", NpcJobType.Merchant, MerchantBehavior.Local),
                "p18d-intraday-npc-b", city, 0f,
                out secondActor, out materializationFailure), Is.True, materializationFailure.ToString());
            secondActor.Inventory.AddItem(item, 10, 1f);
        }
        return new Fixture(runtime, records, city, actor, secondActor, item, sellAction,
            merchantSystem, decisionSystem);
    }

    private static void SetActorAtCity(Fixture fixture)
    {
        AddCityAnchor(fixture);
        SetActorPositionAtCity(fixture, fixture.Actor);
    }

    private static void SetActorsAtCity(Fixture fixture)
    {
        AddCityAnchor(fixture);
        SetActorPositionAtCity(fixture, fixture.Actor);
        SetActorPositionAtCity(fixture, fixture.SecondActor);
    }

    private static void SetActorAtOtherLocation(Fixture fixture)
    {
        AddCityAnchor(fixture);
        LocationId otherLocation = new LocationId("p18d-other-location");
        Assert.That(fixture.Runtime.SpatialAuthorityStore.TryRegisterLocation(
            new LocationRecord(otherLocation, new HexId("p18d-test-hex")),
            out SpatialAuthorityFailure locationFailure), Is.True, locationFailure?.ToString());
        Assert.That(fixture.Runtime.PersonSpatialPositionStore.TrySetAt(
            fixture.Actor.PersonId, StablePositionReference.ForLocation(otherLocation),
            out PersonSpatialPositionFailure positionFailure), Is.True, positionFailure?.ToString());
    }

    private static void AddCityAnchor(Fixture fixture)
    {
        LocationId locationId = new LocationId(fixture.City.Location.RuntimeId);
        Assert.That(fixture.Runtime.SpatialAuthorityStore.TryRegisterHex(
            new HexRecord(new HexId("p18d-test-hex")), out SpatialAuthorityFailure hexFailure),
            Is.True, hexFailure?.ToString());
        Assert.That(fixture.Runtime.SpatialAuthorityStore.TryRegisterLocation(
            new LocationRecord(locationId, new HexId("p18d-test-hex")), out SpatialAuthorityFailure locationFailure),
            Is.True, locationFailure?.ToString());
        Assert.That(fixture.Runtime.LegacySpatialAnchorBindingStore.TryBindCity(
            fixture.City.RuntimeId, locationId, out SpatialAnchorBindingFailure bindingFailure),
            Is.True, bindingFailure?.ToString());
    }

    private static void SetActorPositionAtCity(Fixture fixture, NpcRuntime actor)
    {
        LocationId locationId = new LocationId(fixture.City.Location.RuntimeId);
        Assert.That(fixture.Runtime.PersonSpatialPositionStore.TrySetAt(
            actor.PersonId, StablePositionReference.ForLocation(locationId),
            out PersonSpatialPositionFailure positionFailure), Is.True, positionFailure?.ToString());
    }

    private sealed class Fixture
    {
        public SimulationRuntime Runtime { get; }
        public RecordFixture Records { get; }
        public CityRuntime City { get; }
        public NpcRuntime Actor { get; }
        public NpcRuntime SecondActor { get; }
        public ItemData Item { get; }
        public NpcActionData SellAction { get; }
        public MerchantSystem Merchant { get; }
        public NpcDecisionSystem Decisions { get; }

        public Fixture(SimulationRuntime runtime, RecordFixture records, CityRuntime city,
            NpcRuntime actor, NpcRuntime secondActor, ItemData item, NpcActionData sellAction,
            MerchantSystem merchant, NpcDecisionSystem decisions)
        {
            Runtime = runtime;
            Records = records;
            City = city;
            Actor = actor;
            SecondActor = secondActor;
            Item = item;
            SellAction = sellAction;
            Merchant = merchant;
            Decisions = decisions;
        }
    }

    private sealed class ThrowingActionSuccessRandomSource : IAuthoritativeRandomSource
    {
        private readonly DeterministicRandomSource fallback = new DeterministicRandomSource();

        public float NextUnit(string streamKey, long drawIndex = 0L)
        {
            if (streamKey != null && streamKey.StartsWith("action-success|", System.StringComparison.Ordinal))
                throw new System.InvalidOperationException("Injected action-success draw failure.");
            return fallback.NextUnit(streamKey, drawIndex);
        }

        public DeterministicRandomStream CreateStream(string streamKey) => fallback.CreateStream(streamKey);
    }
}
