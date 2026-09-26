using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SimulationBootstrapCompositionTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
        {
            if (simulationObject != null)
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void DisabledCrimeBootstrapAdvancesExistingHiddenTimerWithoutRegisteringCrimeProvider()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("disabled-crime-city");
        NpcData npcData = SimulationTestFactory.CreateNpc("disabled-crime-npc");
        NpcActionData hide = SimulationTestFactory.CreateAction(
            "disabled-crime-hide",
            NpcActionType.Hide,
            NpcActionCategory.Crime);
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("disabled-crime-free");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("disabled-crime-wanted");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("disabled-crime-arrested");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("disabled-crime-hidden");

        npcData.acoesPadrao.Add(new NPCDefaultAction
        {
            action = hide,
            baseUtility = 100f
        });
        config.Cities.Add(city);
        config.Npcs.Add(new NpcSimulationConfig
        {
            npc = npcData,
            startingCity = city
        });
        config.Actions.Add(hide);
        config.freeStatus = freeStatus;
        config.wantedStatus = wantedStatus;
        config.arrestedStatus = arrestedStatus;
        config.hiddenStatus = hiddenStatus;

        GameObject simulationObject = new GameObject("disabled-crime-bootstrap-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        FieldInfo configField = typeof(TesteSimulacao).GetField(
            "simulationConfig",
            BindingFlags.Instance | BindingFlags.NonPublic);
        configField.SetValue(simulation, config);

        simulation.Start();

        Assert.That(simulation.Runtime.Configuration.Crime.Enabled, Is.False);
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime npc), Is.True);

        FieldInfo crimeField = typeof(TesteSimulacao).GetField(
            "crimeSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo decisionField = typeof(TesteSimulacao).GetField(
            "npcDecisionSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        CrimeSystem composedCrime = crimeField.GetValue(simulation) as CrimeSystem;
        NpcDecisionSystem decisions = decisionField.GetValue(simulation) as NpcDecisionSystem;

        Assert.That(composedCrime, Is.Not.Null);
        Assert.That(decisions, Is.Not.Null);
        Assert.That(decisions.HasProvider<CrimeSystem>(), Is.False);

        npc.HideForDays(1);
        npc.AddStatus(hiddenStatus);
        simulation.Runtime.AdvanceDays(2);

        Assert.That(npc.IsHidden, Is.False);
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(0));
        Assert.That(npc.CurrentStatus.Contains(hiddenStatus), Is.False);
        Assert.That(simulation.Decisions.Decisions, Is.Empty);
    }

    [Test]
    public void AuthoredGenesisFingerprintIsStableAndPreservesCausalListOrder()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("genesis-city");
        CityData unorderedCity = SimulationTestFactory.CreateCityData("genesis-unordered-city");
        NpcData npc = SimulationTestFactory.CreateNpc("genesis-npc");
        NpcActionData action = SimulationTestFactory.CreateAction("genesis-action", NpcActionType.Hide, NpcActionCategory.Crime);
        ItemData fingerprintItem = SimulationTestFactory.CreateItem("genesis-fingerprint-item", 10f);
        city.marketItems.Add(new MarketItemConfig { item = fingerprintItem, initialAmount = 4, desiredAmount = 7 });
        config.Cities.Add(city);
        config.Cities.Add(unorderedCity);
        config.Npcs.Add(new NpcSimulationConfig { npc = npc, startingCity = city });
        config.Actions.Add(action);
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig { actor = npc, action = action, absoluteDay = 1 });
        config.ScheduledDirectives.Add(new ScheduledDirectiveConfig { actor = npc, action = action, absoluteDay = 2 });

        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(config.Calendar, out _);
        string first = SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _);
        string same = SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _);
        Assert.That(first, Is.EqualTo(same));
        config.Cities.Reverse();
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _), Is.EqualTo(first));
        Assert.That(SimulationGenesisPipeline.ResolveStageOrder(), Is.EqualTo(new[]
        {
            "p9.genesis.resolve-profile/v1", "p9.genesis.authored-world/v1",
            "p9.genesis.authored-actors/v1", "p9.genesis.validate-profile/v1", "p9.genesis.publish/v1"
        }));

        config.ScheduledDirectives.Reverse();
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, effective, calendar, out _), Is.Not.EqualTo(first));
        config.ScheduledDirectives.Reverse();
        config.travelCostPerDay += 1f;
        EffectiveSimulationConfiguration changedEffective = SimulationConfigurationResolver.ResolveOrThrow(contentOverrides: config.CreateConfigurationOverrides());
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, changedEffective, calendar, out _), Is.Not.EqualTo(first));
        config.travelCostPerDay -= 1f;
        fingerprintItem.basePrice += 1f;
        EffectiveSimulationConfiguration restoredEffective = SimulationConfigurationResolver.ResolveOrThrow(contentOverrides: config.CreateConfigurationOverrides());
        Assert.That(SimulationGenesisPipeline.CreateFingerprint(config, restoredEffective, calendar, out _), Is.Not.EqualTo(first));
    }

    [Test]
    public void RouteIdentityUsesTupleComponentsEvenWhenDefinitionIdsContainSeparator()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData firstOrigin = SimulationTestFactory.CreateCityData("route\u001fleft");
        CityData firstDestination = SimulationTestFactory.CreateCityData("right");
        CityData secondOrigin = SimulationTestFactory.CreateCityData("route");
        CityData secondDestination = SimulationTestFactory.CreateCityData("left\u001fright");
        firstOrigin.connections.Add(new CityConnection { destination = firstDestination, travelDays = 3 });
        secondOrigin.connections.Add(new CityConnection { destination = secondDestination, travelDays = 3 });
        config.Cities.Add(firstOrigin);
        config.Cities.Add(firstDestination);
        config.Cities.Add(secondOrigin);
        config.Cities.Add(secondDestination);

        Assert.DoesNotThrow(() => SimulationGenesisPipeline.ValidateProfile(config));
        secondOrigin.connections.Add(new CityConnection { destination = secondDestination, travelDays = 3 });
        Assert.Throws<System.InvalidOperationException>(() => SimulationGenesisPipeline.ValidateProfile(config));
    }

    [Test]
    public void InvalidAuthoredCalendarFailsBeforeHostCompositionIsPublished()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.calendar = new CalendarDefinition(0, 1, 1);
        GameObject simulationObject = new GameObject("invalid-calendar-bootstrap-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());
        Assert.That(simulation.Bootstrap, Is.Null);
    }

    [Test]
    public void InvalidAuthoredProfileFailsBeforeHostCompositionIsPublished()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        config.Cities.Add(SimulationTestFactory.CreateCityData("genesis-duplicate"));
        config.Cities.Add(SimulationTestFactory.CreateCityData("genesis-duplicate"));
        GameObject simulationObject = new GameObject("invalid-genesis-bootstrap-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        Assert.Throws<System.InvalidOperationException>(() => simulation.Start());
        Assert.That(simulation.Bootstrap, Is.Null);
        Assert.That(simulation.Runtime, Is.Null);
        Assert.That(simulation.History, Is.Null);
    }

    [Test]
    public void AuthoredGenesisPreservesPopulationDistinctRouteDurationsAndWarrantAccumulation()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData origin = SimulationTestFactory.CreateCityData("a-genesis-origin");
        CityData destination = SimulationTestFactory.CreateCityData("b-genesis-destination");
        ItemData authoredItem = SimulationTestFactory.CreateItem("genesis-production-item", 12f);
        origin.initialPopulation = 4321;
        origin.marketLiquidity = new MarketLiquidityConfig { liquidityMode = MarketLiquidityMode.AccountBacked, initialPurchasingPower = 91f };
        origin.populationConsumption = new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.AccountBacked, initialPurchasingPower = 37f };
        origin.productionConfigs.Add(new CityProductionConfig { item = authoredItem, amountPerDay = 3 });
        origin.connections.Add(new CityConnection { destination = destination, travelDays = 2 });
        origin.connections.Add(new CityConnection { destination = destination, travelDays = 5 });
        NpcData actor = SimulationTestFactory.CreateNpc("genesis-warrant-target");
        NpcStatusData authoredStatus = SimulationTestFactory.CreateStatus("genesis-warrant-default-status");
        config.wantedStatus = SimulationTestFactory.CreateStatus("genesis-wanted-status");
        actor.statusPadrao.Add(authoredStatus);
        config.Cities.Add(origin);
        config.Cities.Add(destination);
        config.Npcs.Add(new NpcSimulationConfig { npc = actor, startingCity = origin });
        config.InitialWarrants.Add(new InitialWantedRecordConfig { target = actor, city = origin, bounty = 11f, sentenceDays = 2 });
        config.InitialWarrants.Add(new InitialWantedRecordConfig { target = actor, city = origin, bounty = 7f, sentenceDays = 3 });
        GameObject simulationObject = new GameObject("complete-genesis-profile-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);

        simulation.Start();

        Assert.That(simulation.TryGetCityRuntime("city-000001", out CityRuntime originRuntime), Is.True);
        Assert.That(originRuntime.CurrentPopulation, Is.EqualTo(4321));
        Assert.That(originRuntime.MarketCounterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.AccountBacked));
        Assert.That(originRuntime.MarketCounterparty.MoneyAccount.Balance, Is.EqualTo(91f));
        Assert.That(originRuntime.PopulationEconomy.PaymentMode, Is.EqualTo(ConsumptionPaymentMode.AccountBacked));
        Assert.That(originRuntime.PopulationEconomy.MoneyAccount.Balance, Is.EqualTo(37f));
        Assert.That(originRuntime.CityData.productionConfigs.Single().amountPerDay, Is.EqualTo(3));
        Assert.That(simulation.SpatialNetwork.Routes, Has.Count.EqualTo(2));
        Assert.That(simulation.SpatialNetwork.Routes, Has.Some.Property("TravelDays").EqualTo(2));
        Assert.That(simulation.SpatialNetwork.Routes, Has.Some.Property("TravelDays").EqualTo(5));
        Assert.That(simulation.TryGetNpcRuntime("npc-000001", out NpcRuntime actorRuntime), Is.True);
        Assert.That(actorRuntime.CurrentStatus, Is.EqualTo(new[] { authoredStatus, config.wantedStatus }));
        JusticeSystem justice = typeof(TesteSimulacao).GetField("justiceSystem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation) as JusticeSystem;
        WantedRecordRuntime warrant = justice.GetActiveWarrants(actorRuntime).Single();
        Assert.That(warrant.Bounty, Is.EqualTo(18f));
        Assert.That(warrant.SentenceDays, Is.EqualTo(5));
        Assert.That(simulation.History.HistoricalEvents, Is.Empty);
        Assert.That(simulation.Bootstrap.Manifest.EffectiveConfiguration.Travel.TravelCostPerDay, Is.EqualTo(config.travelCostPerDay));
        Assert.That(simulation.Bootstrap.Manifest.CanonicalProvenanceRecords, Has.Some.Contains("effective.travel"));
        Assert.That(simulation.Bootstrap.Manifest.AuthoredDefinitionIds, Does.Contain("city/a-genesis-origin"));
    }

    [Test]
    public void RuntimeCannotAdvanceAfterValidationFailureBeforePublication()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        CityData city = SimulationTestFactory.CreateCityData("failure-atomic-city");
        NpcData npc = SimulationTestFactory.CreateNpc("failure-atomic-npc");
        config.Cities.Add(city);
        config.Npcs.Add(new NpcSimulationConfig { npc = npc, startingCity = city });
        GameObject simulationObject = new GameObject("post-validation-failure-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(simulation, config);
        System.Action<string> injectFailure = stageId =>
        {
            if (stageId == "p9.genesis.validate-profile/v1") throw new System.InvalidOperationException("injected post-validation failure");
        };
        MethodInfo initialize = typeof(TesteSimulacao).GetMethod("InitializeSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => initialize.Invoke(simulation, new object[] { injectFailure }));
        Assert.That(exception.InnerException, Is.TypeOf<System.InvalidOperationException>());
        Assert.That(simulation.Bootstrap, Is.Null);
        MethodInfo simulate = typeof(TesteSimulacao).GetMethod("Simulate", BindingFlags.Instance | BindingFlags.NonPublic);
        simulate.Invoke(simulation, new object[] { 2 });
        SimulationTime candidateTime = typeof(TesteSimulacao).GetField("simulationTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation) as SimulationTime;
        HistoryStore candidateHistory = typeof(TesteSimulacao).GetField("historyStore", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation) as HistoryStore;
        Assert.That(candidateTime.AbsoluteDay, Is.Zero);
        Assert.That(candidateHistory.HistoricalEvents, Is.Empty);
        Assert.That(simulation.CurrentDay, Is.Zero);
        Assert.That(simulation.History, Is.Null);
    }
}
