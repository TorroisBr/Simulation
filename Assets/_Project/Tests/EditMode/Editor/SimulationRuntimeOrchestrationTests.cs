using System.Collections.Generic;
using NUnit.Framework;

public sealed class SimulationRuntimeOrchestrationTests
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
    public void AdvanceDay_ExecutesOneAutonomousDecisionThroughRuntime()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime("npc-autonomous", SimulationTestFactory.CreateNpc("autonomous"));
        NpcStatusData completedStatus = SimulationTestFactory.CreateStatus("completed");
        NpcActionData action = SimulationTestFactory.CreateAction("autonomous-action", NpcActionType.Normal);
        action.statusToAdd.Add(completedStatus);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(new List<INpcActionProvider>());
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            null,
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: decisionSystem,
            decisionRecorder: records.DecisionRecorder);

        runtime.AdvanceDay();

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        NpcDecisionRecord decision = records.Decisions.Decisions[0];
        Assert.That(decision.ActorRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(decision.DecisionType, Is.EqualTo(NpcDecisionType.Action));
        Assert.That(decision.Origin, Is.EqualTo(NpcDecisionOrigin.Autonomous));
        Assert.That(decision.ActionDefinitionId, Is.EqualTo(action.DefinitionId));
        Assert.That(npc.CurrentActionRuntime, Is.Not.Null);
        Assert.That(npc.CurrentActionRuntime.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        Assert.That(npc.CurrentStatus, Has.Member(completedStatus));
    }

    [Test]
    public void AdvanceDay_ProcessesScheduledDirectiveBeforeAutonomousDecision()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        CityRuntime city = SimulationTestFactory.CreateCity("city-directive", "location-directive");
        NpcRuntime guard = new NpcRuntime(
            "npc-directive-guard",
            SimulationTestFactory.CreateNpc("directive-guard"),
            city,
            0f);
        NpcRuntime actor = new NpcRuntime(
            "npc-directive-actor",
            SimulationTestFactory.CreateNpc("directive-actor"),
            city,
            0f);
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("free");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("wanted");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("arrested");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("hidden");
        JusticeSystem justice = new JusticeSystem(
            freeStatus,
            wantedStatus,
            arrestedStatus,
            hiddenStatus,
            records.EventRecorder,
            null);
        justice.CreateOrIncreaseWarrant(actor, city, 50f, 3);
        Assert.That(justice.Arrest(guard, actor, city), Is.True);

        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(actor), Is.True);
        NpcActionData escape = SimulationTestFactory.CreateAction(
            "escape-directive",
            NpcActionType.EscapePrison,
            NpcActionCategory.Justice);
        ScheduledDirective directive = new ScheduledDirective(
            "directive-runtime",
            1L,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            actor.RuntimeId,
            escape);
        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(records.Time);
        Assert.That(directiveStore.Add(directive), Is.True);
        ScheduledDirectiveSystem directiveSystem = new ScheduledDirectiveSystem(directiveStore, registry);
        CrimeSystem crime = new CrimeSystem(justice, null, hiddenStatus);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(new List<INpcActionProvider> { crime });
        NpcActionData autonomousFallback = SimulationTestFactory.CreateAction("autonomous-fallback", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { city },
            new[] { actor },
            configuredActions: new[] { autonomousFallback },
            scheduledDirectiveSystem: directiveSystem,
            justiceSystem: justice,
            crimeSystem: crime,
            npcDecisionSystem: decisionSystem,
            decisionRecorder: records.DecisionRecorder,
            configuration: SimulationConfigurationResolver.ResolveOrThrow(
                contentOverrides: new SimulationConfigurationOverrides(
                    economy: new EconomyConfigurationOverrides(false),
                    crime: new CrimeConfigurationOverrides(true, false))));

        runtime.AdvanceDay();

        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        Assert.That(records.Decisions.Decisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.ScheduledDirective));
        Assert.That(records.Decisions.Decisions[0].DecisionType, Is.EqualTo(NpcDecisionType.Escape));
        Assert.That(records.Decisions.Decisions[0].ActionDefinitionId, Is.EqualTo(escape.DefinitionId));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
        Assert.That(actor.CurrentActionRuntime, Is.Not.Null);
        Assert.That(actor.CurrentActionRuntime.Action, Is.SameAs(escape));
        Assert.That(justice.IsArrested(actor), Is.False);
    }

    [Test]
    public void CrimeEnabledWithoutAutonomyStillSupportsExplicitActionButNotAutonomousSelection()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        CityRuntime city = SimulationTestFactory.CreateCity("city-crime-policy", "location-crime-policy");
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("free-crime-policy");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("wanted-crime-policy");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("arrested-crime-policy");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("hidden-crime-policy");
        JusticeSystem justice = new JusticeSystem(
            freeStatus,
            wantedStatus,
            arrestedStatus,
            hiddenStatus,
            records.EventRecorder,
            null);
        NpcRuntime thief = new NpcRuntime(
            "npc-crime-thief",
            SimulationTestFactory.CreateNpc("crime-thief"),
            city,
            0f);
        NpcRuntime target = new NpcRuntime(
            "npc-crime-target",
            SimulationTestFactory.CreateNpc("crime-target"),
            city,
            10f);
        NpcActionData steal = SimulationTestFactory.CreateAction(
            "explicit-steal",
            NpcActionType.Steal,
            NpcActionCategory.Crime);
        steal.crimeSettings = new CrimeActionSettings
        {
            amount = 5,
            bounty = 10f,
            sentenceDays = 2
        };
        thief.NpcData.acoesPadrao.Add(new NPCDefaultAction
        {
            action = steal,
            baseUtility = 10f
        });
        EffectiveCrimeConfiguration crimeConfiguration = new EffectiveCrimeConfiguration(true, false);
        CrimeSystem crime = new CrimeSystem(
            justice,
            null,
            hiddenStatus,
            configuration: crimeConfiguration,
            randomSource: new DeterministicRandomSource(3),
            simulationTime: records.Time);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(new List<INpcActionProvider> { crime });

        NpcActionRuntime explicitAction = decisionSystem.CreateRequestedAction(thief, steal);
        NpcActionRuntime autonomousAction = decisionSystem.ChooseAction(
            thief,
            new List<NpcActionData> { steal },
            records.Time.AbsoluteDay);

        Assert.That(crime.AllowAutonomousAction(steal), Is.False);
        Assert.That(explicitAction, Is.Not.Null);
        Assert.That(autonomousAction, Is.Null);
        Assert.That(crime.TryExecuteAction(thief, explicitAction).Success, Is.True);
        Assert.That(target.Money, Is.EqualTo(5f));
    }

    [Test]
    public void DisabledCrimeDoesNotInitiateNormalOrAutonomousAction()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        CityRuntime city = SimulationTestFactory.CreateCity("city-crime-disabled", "location-crime-disabled");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("hidden-crime-disabled");
        CrimeSystem crime = new CrimeSystem(
            null,
            null,
            hiddenStatus,
            configuration: new EffectiveCrimeConfiguration(false, false),
            simulationTime: records.Time);
        NpcRuntime npc = new NpcRuntime(
            "npc-crime-disabled",
            SimulationTestFactory.CreateNpc("crime-disabled"),
            city,
            10f);
        NpcActionData hide = SimulationTestFactory.CreateAction("hide-disabled", NpcActionType.Hide, NpcActionCategory.Crime);
        NpcDecisionSystem decisions = new NpcDecisionSystem(new List<INpcActionProvider> { crime });
        float utility = 10f;

        Assert.That(crime.CreateAction(npc, hide, ref utility), Is.Null);
        Assert.That(decisions.CreateRequestedAction(npc, hide), Is.Null);
        Assert.That(decisions.ChooseAction(npc, new List<NpcActionData> { hide }), Is.Null);
    }

    [Test]
    public void DisabledCrimeStillAdvancesExistingHiddenStateTimer()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        CityRuntime city = SimulationTestFactory.CreateCity("city-hidden-timer", "location-hidden-timer");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("hidden-timer");
        NpcRuntime npc = new NpcRuntime(
            "npc-hidden-timer",
            SimulationTestFactory.CreateNpc("hidden-timer"),
            city,
            0f);
        npc.HideForDays(1);
        npc.AddStatus(hiddenStatus);
        CrimeSystem crime = new CrimeSystem(
            null,
            null,
            hiddenStatus,
            configuration: new EffectiveCrimeConfiguration(false, false),
            simulationTime: records.Time);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { city },
            new[] { npc },
            crimeSystem: crime,
            configuration: SimulationConfigurationResolver.ResolveOrThrow(
                contentOverrides: new SimulationConfigurationOverrides(
                    economy: new EconomyConfigurationOverrides(false),
                    crime: new CrimeConfigurationOverrides(false, false))));

        runtime.AdvanceDays(2);

        Assert.That(npc.IsHidden, Is.False);
        Assert.That(npc.CurrentStatus.Contains(hiddenStatus), Is.False);
    }

    [Test]
    public void SentenceProgressesWhenGuardCrimeIsDisabled()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        CityRuntime city = SimulationTestFactory.CreateCity("city-sentence", "location-sentence");
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("free-sentence");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("wanted-sentence");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("arrested-sentence");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("hidden-sentence");
        JusticeSystem justice = new JusticeSystem(
            freeStatus,
            wantedStatus,
            arrestedStatus,
            hiddenStatus,
            records.EventRecorder,
            null);
        NpcRuntime guard = new NpcRuntime(
            "npc-sentence-guard",
            SimulationTestFactory.CreateNpc("sentence-guard", NpcJobType.Guard),
            city,
            0f);
        NpcRuntime prisoner = new NpcRuntime(
            "npc-sentence-prisoner",
            SimulationTestFactory.CreateNpc("sentence-prisoner"),
            city,
            0f);
        justice.CreateOrIncreaseWarrant(prisoner, city, 10f, 3);
        Assert.That(justice.Arrest(guard, prisoner, city), Is.True);

        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(false),
                guardCrime: new GuardCrimeConfigurationOverrides(false)));
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { city },
            new[] { prisoner },
            justiceSystem: justice,
            configuration: configuration);

        runtime.AdvanceDay();

        Assert.That(runtime.Configuration.GuardCrime.Enabled, Is.False);
        Assert.That(justice.GetRemainingSentenceDays(prisoner), Is.EqualTo(2));
        Assert.That(justice.IsArrested(prisoner), Is.True);
    }

    [Test]
    public void AdvanceDay_TravelingNpcSkipsAutonomousDecisionUntilTravelProgresses()
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 2);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime(
            "npc-traveling",
            SimulationTestFactory.CreateNpc("traveling"),
            world.A,
            100f);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        Assert.That(travel.TryStartTravel(
            npc,
            new NpcActionRuntime(travelAction, world.B, null, NpcTravelReason.Trade, 0f, 0f)), Is.True);
        npc.ClearTravelStartedToday();

        NpcActionData autonomousAction = SimulationTestFactory.CreateAction("autonomous-action", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B },
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { autonomousAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            decisionRecorder: records.DecisionRecorder,
            travelSystem: travel);

        runtime.AdvanceDay();

        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(0));
        Assert.That(npc.IsTraveling, Is.True);
        Assert.That(npc.CurrentCity, Is.Null);
        Assert.That(npc.DestinationCity, Is.SameAs(world.B));
        Assert.That(npc.TravelDaysRemaining, Is.EqualTo(1));
    }

    [Test]
    public void AdvanceDay_ArrivalHappensAfterDecisionAndNextDayCanDecideAtDestination()
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 1);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime(
            "npc-arrival",
            SimulationTestFactory.CreateNpc("arrival"),
            world.A,
            100f);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        Assert.That(travel.TryStartTravel(
            npc,
            new NpcActionRuntime(travelAction, world.B, null, NpcTravelReason.Trade, 0f, 0f)), Is.True);
        npc.ClearTravelStartedToday();

        NpcActionData autonomousAction = SimulationTestFactory.CreateAction("destination-action", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B },
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { autonomousAction },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            decisionRecorder: records.DecisionRecorder,
            travelSystem: travel);

        runtime.AdvanceDay();

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(0));
        Assert.That(npc.IsTraveling, Is.False);
        Assert.That(npc.CurrentCity, Is.SameAs(world.B));

        runtime.AdvanceDay();

        Assert.That(records.Decisions.Decisions.Count, Is.EqualTo(1));
        Assert.That(records.Decisions.Decisions[0].AbsoluteDay, Is.EqualTo(2L));
        Assert.That(records.Decisions.Decisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.Autonomous));
        Assert.That(npc.CurrentActionRuntime.Action, Is.SameAs(autonomousAction));
    }

    [Test]
    public void AdvanceDay_ArrivalRefreshesMerchantKnowledgeAfterTravelProgress()
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 1);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ItemData item = SimulationTestFactory.CreateItem("item-arrival");
        world.B.Market.AddStock(item, 10, 10);
        NpcRuntime merchant = new NpcRuntime(
            "npc-arriving-merchant",
            SimulationTestFactory.CreateNpc("arriving-merchant", NpcJobType.Merchant, MerchantBehavior.Traveling),
            world.A,
            100f);
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        MerchantSystem merchantSystem = SimulationTestFactory.CreateMerchantSystem(
            travel,
            records.Time,
            records.DecisionRecorder);
        merchantSystem.ObserveCurrentMarket(merchant);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.B.Location.RuntimeId,
            item.DefinitionId,
            out _), Is.False);

        NpcActionData travelAction = SimulationTestFactory.CreateAction("travel", NpcActionType.Travel, NpcActionCategory.Travel);
        Assert.That(travel.TryStartTravel(
            merchant,
            new NpcActionRuntime(travelAction, world.B, null, NpcTravelReason.Trade, 0f, 0f)), Is.True);
        merchant.ClearTravelStartedToday();

        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B },
            new[] { merchant },
            travelSystem: travel,
            merchantSystem: merchantSystem,
            configuration: SimulationConfigurationResolver.ResolveOrThrow(
                contentOverrides: new SimulationConfigurationOverrides(
                    economy: new EconomyConfigurationOverrides(false),
                    merchantTrade: new MerchantTradeConfigurationOverrides(enabled: true))));

        runtime.AdvanceDay();

        Assert.That(merchant.CurrentCity, Is.SameAs(world.B));
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            world.B.Location.RuntimeId,
            item.DefinitionId,
            out CommercialMarketObservation observation), Is.True);
        Assert.That(observation.ObservedDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(observation.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            world.B.Location.RuntimeId,
            out CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(liquidity.Source, Is.EqualTo(CommercialKnowledgeSource.DirectObservation));
    }

    [Test]
    public void AdvanceDay_GroupTravelProgressesOnceAndKeepsMembersSynchronized()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(3);
        Assert.That(fixture.System.TryStartTravelParty(CreateGroupTravelContext(fixture), out TravelPartyRuntime party), Is.True);

        foreach (NpcRuntime member in fixture.Members)
        {
            member.ClearTravelStartedToday();
        }

        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.World.A, fixture.World.B, fixture.World.C },
            fixture.Members,
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.System);

        runtime.AdvanceDay();

        Assert.That(party.IsActive, Is.True);
        Assert.That(fixture.Parties.ActiveParties.Count, Is.EqualTo(1));
        foreach (NpcRuntime member in fixture.Members)
        {
            Assert.That(member.IsTraveling, Is.True);
            Assert.That(member.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId));
            Assert.That(member.TravelDaysRemaining, Is.EqualTo(2));
        }

        SimulationInvariantValidator.ValidateTravelParties(
            fixture.Parties,
            fixture.World.IdentityRegistry,
            fixture.Members);
    }

    private static ActionExecutionContext CreateGroupTravelContext(TravelPartyFixture fixture)
    {
        return new ActionExecutionContext(
            "group-travel-runtime",
            new[]
            {
                new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Marta.RuntimeId, ActionExecutionParticipantRole.Support)
            },
            fixture.World.B.Location.RuntimeId,
            fixture.World.RouteAB.RuntimeId);
    }
}
