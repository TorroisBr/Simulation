using System.Collections.Generic;
using NUnit.Framework;

public sealed class ActorChoiceRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void EligibleLocalSellChoiceRunsOnceThroughCurrentCityTruth()
    {
        Fixture fixture = CreateFixture(inventoryAmount: 10);
        SetActorAtCurrentCityLocation(fixture);
        ActorChoiceInput input = Capture(fixture, "choice-success");

        fixture.Runtime.AdvanceDay();

        ActorChoiceInput completed = GetInput(fixture.Runtime, input.InputId);
        Assert.That(completed.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(completed.Dispositions, Has.Count.EqualTo(2));
        Assert.That(completed.Dispositions[0].Kind, Is.EqualTo(ActorChoiceDispositionKind.DispatchStarted));
        Assert.That(completed.Dispositions[0].DecisionRecordId, Is.Not.Null.And.Not.Empty);
        Assert.That(completed.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(fixture.Records.Decisions.Decisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.ActorChoice));
        Assert.That(fixture.Records.Decisions.Decisions[0].ActionDefinitionId, Is.EqualTo(fixture.SellAction.DefinitionId));
        Assert.That(fixture.Actor.CurrentActionRuntime.Action, Is.SameAs(fixture.SellAction));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(5));
        Assert.That(fixture.Runtime.ActorChoiceStore.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void UnavailableSupportedChoiceDoesNotFallBackToAutonomousAction()
    {
        Fixture fixture = CreateFixture(inventoryAmount: 0);
        ActorChoiceInput input = Capture(fixture, "choice-unavailable");

        fixture.Runtime.AdvanceDay();

        ActorChoiceInput rejected = GetInput(fixture.Runtime, input.InputId);
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.Dispositions, Has.Count.EqualTo(1));
        Assert.That(rejected.Dispositions[0].Failure, Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
        Assert.That(fixture.Records.Decisions.Decisions, Is.Empty);
        Assert.That(fixture.Actor.CurrentActionRuntime, Is.Null);
        Assert.That(fixture.Actor.CurrentStatus, Has.No.Member(fixture.FallbackStatus));
    }

    [Test]
    public void MatchingAnchorHexDoesNotSubstituteForAtCityLocation()
    {
        Fixture fixture = CreateFixture(inventoryAmount: 10);
        AddCityAnchor(fixture, "hex-choice-city");
        Assert.That(fixture.Runtime.PersonSpatialPositionStore.TrySetAt(
            fixture.Actor.PersonId,
            StablePositionReference.ForHex(new HexId("hex-choice-city")),
            out PersonSpatialPositionFailure positionFailure), Is.True, positionFailure?.ToString());
        ActorChoiceInput input = Capture(fixture, "choice-hex-anchor");

        fixture.Runtime.AdvanceDay();

        ActorChoiceInput rejected = GetInput(fixture.Runtime, input.InputId);
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.Dispositions[0].Failure, Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
        Assert.That(fixture.Records.Decisions.Decisions, Is.Empty);
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(10));
    }

    [Test]
    public void InTransitChoiceDefersAtActorTurnBoundaryWithoutAutonomousFallback()
    {
        Fixture fixture = CreateFixture(inventoryAmount: 10);
        BeginActorTransit(fixture);
        ActorChoiceInput input = Capture(fixture, "choice-in-transit");

        fixture.Runtime.AdvanceDay();

        ActorChoiceInput deferred = GetInput(fixture.Runtime, input.InputId);
        Assert.That(deferred.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        Assert.That(deferred.Dispositions, Has.Count.EqualTo(1));
        Assert.That(deferred.Dispositions[0].Kind, Is.EqualTo(ActorChoiceDispositionKind.Deferred));
        Assert.That(deferred.Dispositions[0].DeferralReason, Is.EqualTo(ActorChoiceDeferralReason.Traveling));
        Assert.That(fixture.Records.Decisions.Decisions, Is.Empty);
        Assert.That(fixture.Actor.CurrentActionRuntime, Is.Null);
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.EqualTo(10));
    }

    [Test]
    public void MultipleInputsForOnePersonConsumeOnlyTheEarliestPerDailyTurn()
    {
        Fixture fixture = CreateFixture(inventoryAmount: 10);
        ActorChoiceInput first = Capture(fixture, "choice-first");
        ActorChoiceInput second = Capture(fixture, "choice-second");

        fixture.Runtime.AdvanceDay();

        Assert.That(GetInput(fixture.Runtime, first.InputId).Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        ActorChoiceInput waiting = GetInput(fixture.Runtime, second.InputId);
        Assert.That(waiting.Status, Is.EqualTo(ActorChoiceInputStatus.Pending));
        Assert.That(waiting.Dispositions, Is.Empty);
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(fixture.Records.Decisions.Decisions[0].AbsoluteDay, Is.EqualTo(1L));

        fixture.Runtime.AdvanceDay();

        Assert.That(GetInput(fixture.Runtime, second.InputId).Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(fixture.Records.Decisions.Decisions, Has.Count.EqualTo(2));
        Assert.That(fixture.Records.Decisions.Decisions[1].AbsoluteDay, Is.EqualTo(2L));
        Assert.That(fixture.Actor.Inventory.GetAmount(fixture.Item), Is.Zero);
        Assert.That(fixture.Runtime.ActorChoiceStore.ValidateInvariants().IsValid, Is.True);
    }

    private static Fixture CreateFixture(int inventoryAmount)
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ItemData item = SimulationTestFactory.CreateItem("actor-choice-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            "city-actor-choice",
            "location-actor-choice",
            1000f,
            SimulationTestFactory.CreateMarketItem(item, 100, 100));
        NpcActionData sellAction = SimulationTestFactory.CreateAction(
            "sell-goods-actor-choice",
            NpcActionType.SellGoods,
            NpcActionCategory.Commerce);
        NpcActionData fallbackAction = SimulationTestFactory.CreateAction("actor-choice-fallback", NpcActionType.Normal);
        NpcStatusData fallbackStatus = SimulationTestFactory.CreateStatus("actor-choice-fallback-ran");
        fallbackAction.statusToAdd.Add(fallbackStatus);
        MerchantSystem merchantSystem = SimulationTestFactory.CreateMerchantSystem(
            null,
            records.Time,
            records.DecisionRecorder,
            maxTradeAmount: 5);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(new List<INpcActionProvider> { merchantSystem });
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(false),
                merchantTrade: new MerchantTradeConfigurationOverrides(enabled: true)));
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { city },
            null,
            configuredActions: new[] { sellAction, fallbackAction },
            npcDecisionSystem: decisionSystem,
            decisionRecorder: records.DecisionRecorder,
            merchantSystem: merchantSystem,
            configuration: configuration);

        PersonId personId = new PersonId("person-actor-choice");
        Assert.That(runtime.TryRegisterPerson(new PersonRuntime(personId), out PersonStoreFailure registrationFailure),
            Is.True, registrationFailure.ToString());
        Assert.That(runtime.TryMaterializePerson(
            personId,
            SimulationTestFactory.CreateNpc("actor-choice-merchant", NpcJobType.Merchant, MerchantBehavior.Local),
            "npc-actor-choice",
            city,
            0f,
            out NpcRuntime actor,
            out PersonMaterializationFailure materializationFailure), Is.True, materializationFailure.ToString());
        if (inventoryAmount > 0)
        {
            actor.Inventory.AddItem(item, inventoryAmount, 1f);
        }

        return new Fixture(runtime, records, city, actor, item, sellAction, fallbackStatus);
    }

    private static void SetActorAtCurrentCityLocation(Fixture fixture)
    {
        AddCityAnchor(fixture, "hex-choice-city");
        Assert.That(fixture.Runtime.PersonSpatialPositionStore.TrySetAt(
            fixture.Actor.PersonId,
            StablePositionReference.ForLocation(new LocationId(fixture.City.Location.RuntimeId)),
            out PersonSpatialPositionFailure failure), Is.True, failure?.ToString());
    }

    private static void AddCityAnchor(Fixture fixture, string anchorHexId)
    {
        LocationId locationId = new LocationId(fixture.City.Location.RuntimeId);
        Assert.That(fixture.Runtime.SpatialAuthorityStore.TryRegisterHex(
            new HexRecord(new HexId(anchorHexId)), out SpatialAuthorityFailure hexFailure), Is.True, hexFailure?.ToString());
        Assert.That(fixture.Runtime.SpatialAuthorityStore.TryRegisterLocation(
            new LocationRecord(locationId, new HexId(anchorHexId)), out SpatialAuthorityFailure locationFailure), Is.True, locationFailure?.ToString());
        Assert.That(fixture.Runtime.LegacySpatialAnchorBindingStore.TryBindCity(
            fixture.City.RuntimeId, locationId, out SpatialAnchorBindingFailure bindingFailure), Is.True, bindingFailure?.ToString());
    }

    private static void BeginActorTransit(Fixture fixture)
    {
        HexId from = new HexId("hex-choice-from");
        HexId to = new HexId("hex-choice-to");
        HexBoundaryKey boundary = new HexBoundaryKey(from, to);
        TraversalOptionRef option = TraversalOptionRef.ForConnection(new ConnectionId("connection-actor-choice"));
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("choice-scale", "fixture", "v1", 1m, "unit"),
            new[]
            {
                CreateGeographicHex(from.Value, 0, 0),
                CreateGeographicHex(to.Value, 1, 0)
            },
            new[] { new LocationRecord(new LocationId(fixture.City.Location.RuntimeId), from) });
        Assert.That(fixture.Runtime.SpatialAuthorityStore.TryComposeGeography(
            geography, out SpatialAuthorityFailure geographyFailure), Is.True, geographyFailure?.ToString());
        Assert.That(fixture.Runtime.SpatialAuthorityStore.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(option, boundary, "choice-road", "v1"),
            PassageCondition.Available,
            out SpatialAuthorityFailure passageFailure), Is.True, passageFailure?.ToString());
        Assert.That(fixture.Runtime.LegacySpatialAnchorBindingStore.TryBindCity(
            fixture.City.RuntimeId,
            new LocationId(fixture.City.Location.RuntimeId),
            out SpatialAnchorBindingFailure bindingFailure), Is.True, bindingFailure?.ToString());
        Assert.That(fixture.Runtime.PersonSpatialPositionStore.TrySetAt(
            fixture.Actor.PersonId,
            StablePositionReference.ForHex(from),
            out PersonSpatialPositionFailure atFailure), Is.True, atFailure?.ToString());
        Assert.That(fixture.Runtime.PersonSpatialPositionStore.TryBeginTransit(
            fixture.Actor.PersonId,
            option,
            boundary,
            from,
            to,
            out PersonSpatialPositionFailure transitFailure), Is.True, transitFailure?.ToString());
    }

    private static HexRecord CreateGeographicHex(string id, int q, int r)
    {
        return new HexRecord(
            new HexId(id),
            new HexCoordinate(q, r),
            new TerrainReference(new TerrainDefinitionId("terrain.actor-choice"), "v1"));
    }

    private static ActorChoiceInput Capture(Fixture fixture, string commandId)
    {
        Assert.That(fixture.Runtime.ActorChoiceStore.TryCapture(
            commandId,
            fixture.Actor.PersonId,
            fixture.SellAction.DefinitionId,
            WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request,
            fixture.Runtime.CurrentDay,
            out ActorChoiceInput input,
            out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
        return input;
    }

    private static ActorChoiceInput GetInput(SimulationRuntime runtime, ActorChoiceInputId inputId)
    {
        Assert.That(runtime.ActorChoiceStore.TryGet(inputId, out ActorChoiceInput input), Is.True);
        return input;
    }

    private sealed class Fixture
    {
        public SimulationRuntime Runtime { get; }
        public RecordFixture Records { get; }
        public CityRuntime City { get; }
        public NpcRuntime Actor { get; }
        public ItemData Item { get; }
        public NpcActionData SellAction { get; }
        public NpcStatusData FallbackStatus { get; }

        public Fixture(
            SimulationRuntime runtime,
            RecordFixture records,
            CityRuntime city,
            NpcRuntime actor,
            ItemData item,
            NpcActionData sellAction,
            NpcStatusData fallbackStatus)
        {
            Runtime = runtime;
            Records = records;
            City = city;
            Actor = actor;
            Item = item;
            SellAction = sellAction;
            FallbackStatus = fallbackStatus;
        }
    }
}
