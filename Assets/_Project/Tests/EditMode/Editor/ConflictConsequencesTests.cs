using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class ConflictConsequencesTests
{
    private static readonly Dictionary<string, float> capabilityOverrides = new Dictionary<string, float>(StringComparer.Ordinal);

    [SetUp]
    public void SetUp()
    {
        capabilityOverrides.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void LosingConflictDoesNotAutomaticallyKillNpc()
    {
        NpcRuntime winner = CreateNpc("winner");
        NpcRuntime loser = CreateNpc("loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Meaningful, 100f, 10f);
        ConflictResolutionService service = CreateService(new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(service.TryResolveAndApply(conflict, null, out ConflictResolutionResult result, out string reason), Is.True, reason);

        SimulationInvariantValidator.ValidateConflictResult(conflict, result, requireConsequences: true);
        SimulationInvariantValidator.ValidateNpcLifeState(winner);
        SimulationInvariantValidator.ValidateNpcLifeState(loser);
        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(loser.IsAlive, Is.True);
        Assert.That(loser.InjurySeverity, Is.Not.EqualTo(NpcInjurySeverity.None));
    }

    [Test]
    public void CloseConflictCanInjureWinnerAndLoser()
    {
        NpcRuntime winner = CreateNpc("close-winner");
        NpcRuntime loser = CreateNpc("close-loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Meaningful, 101f, 100f);
        ConflictResolver resolver = new ConflictResolver(
            new FixedCapabilityModel(capabilityOverrides),
            new SequenceConflictRandomSource(0.5f, 0.5f),
            new ConflictResolverSettings { DrawMarginFraction = 0.001f });
        ConflictResolutionService service = new ConflictResolutionService(resolver);

        Assert.That(service.TryResolveAndApply(conflict, null, out _, out string reason), Is.True, reason);

        Assert.That(winner.InjurySeverity, Is.EqualTo(NpcInjurySeverity.Injured));
        Assert.That(loser.InjurySeverity, Is.EqualTo(NpcInjurySeverity.Injured));
        Assert.That(winner.IsDead, Is.False);
        Assert.That(loser.IsDead, Is.False);
    }

    [Test]
    public void ExistentialCloseVictoryCanKillWinningSideNpc()
    {
        NpcRuntime winner = CreateNpc("fatal-winner");
        NpcRuntime loser = CreateNpc("fatal-winner-opponent");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Existential, 102f, 100f);
        ConflictResolutionService service = CreateService(new SequenceConflictRandomSource(0.5f, 0.5f, 0f, 1f));

        Assert.That(service.TryResolveAndApply(conflict, null, out ConflictResolutionResult result, out string reason), Is.True, reason);

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(winner.IsDead, Is.True);
        Assert.That(loser.IsAlive, Is.True);
    }

    [Test]
    public void WinningSideDeathDoesNotChangeWinningSide()
    {
        NpcRuntime winner = CreateNpc("dead-victor");
        NpcRuntime loser = CreateNpc("dead-victor-opponent");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Existential, 102f, 100f);

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f, 0f, 1f)).Compute(conflict);

        Assert.That(result.NpcConsequences.FindByRuntimeId(winner.RuntimeId).IsDead, Is.True);
        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.Outcome, Is.EqualTo(ConflictOutcomeType.Victory));
    }

    [Test]
    public void CloseExistentialConflictCanProduceFatalCasualtiesOnBothSides()
    {
        NpcRuntime winner = CreateNpc("both-fatal-winner");
        NpcRuntime loser = CreateNpc("both-fatal-loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Existential, 102f, 100f);

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f, 0f, 0f)).Compute(conflict);

        Assert.That(result.NpcConsequences.FindByRuntimeId(winner.RuntimeId).IsDead, Is.True);
        Assert.That(result.NpcConsequences.FindByRuntimeId(loser.RuntimeId).IsDead, Is.True);
    }

    [Test]
    public void LowStakesWinnerDoesNotDieUnderEquivalentDeterministicInput()
    {
        NpcRuntime winner = CreateNpc("low-stakes-winner");
        NpcRuntime loser = CreateNpc("low-stakes-loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Low, 102f, 100f);

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f, 0f, 0f)).Compute(conflict);

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.NpcConsequences.FindByRuntimeId(winner.RuntimeId).IsDead, Is.False);
    }

    [Test]
    public void DecisiveVictoryProtectsWinnerFromDefaultFatality()
    {
        NpcRuntime winner = CreateNpc("decisive-winner");
        NpcRuntime loser = CreateNpc("decisive-loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Existential, 100f, 10f);

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f, 0f)).Compute(conflict);

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.NpcConsequences.FindByRuntimeId(winner.RuntimeId).IsDead, Is.False);
    }

    [Test]
    public void ForceAliveStillPreventsWinnerDeath()
    {
        NpcRuntime winner = CreateNpc("force-alive-winner");
        NpcRuntime loser = CreateNpc("force-alive-loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Existential, 102f, 100f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(winner.RuntimeId)
        {
            ForceAlive = true
        });

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f, 0f, 0f)).Compute(conflict, constraints);

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.NpcConsequences.FindByRuntimeId(winner.RuntimeId).IsDead, Is.False);
    }

    [Test]
    public void ForcedDeathStillOverridesNormalWinnerProtection()
    {
        NpcRuntime winner = CreateNpc("forced-dead-decisive-winner");
        NpcRuntime loser = CreateNpc("forced-dead-decisive-loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Existential, 100f, 10f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(winner.RuntimeId)
        {
            ForceDeath = true
        });

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f, 0f)).Compute(conflict, constraints);

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.NpcConsequences.FindByRuntimeId(winner.RuntimeId).IsDead, Is.True);
    }

    [Test]
    public void LowStakesProducesLessSevereConsequencesThanExistentialUnderSameDeterministicInputs()
    {
        NpcRuntime lowA = CreateNpc("low-a");
        NpcRuntime lowB = CreateNpc("low-b");
        NpcRuntime existentialA = CreateNpc("existential-a");
        NpcRuntime existentialB = CreateNpc("existential-b");
        Conflict lowConflict = CreateNpcConflict(lowA, lowB, ConflictStakes.Low, 100f, 100f);
        Conflict existentialConflict = CreateNpcConflict(existentialA, existentialB, ConflictStakes.Existential, 100f, 100f);

        ConflictResolutionResult low = CreateService(new SequenceConflictRandomSource(0.5f, 0.5f)).Compute(lowConflict);
        ConflictResolutionResult existential = CreateService(new SequenceConflictRandomSource(0.5f, 0.5f)).Compute(existentialConflict);

        int lowSeverity = (int)low.NpcConsequences[0].InjurySeverity + (int)low.NpcConsequences[1].InjurySeverity;
        int existentialSeverity = (int)existential.NpcConsequences[0].InjurySeverity + (int)existential.NpcConsequences[1].InjurySeverity;
        Assert.That(existentialSeverity, Is.GreaterThan(lowSeverity));
    }

    [Test]
    public void InjuryReducesEffectiveCapabilityWithoutChangingBaseCapability()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        NpcData npcData = SimulationTestFactory.CreateNpc("injured-capability");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 40f));
        NpcRuntime npc = new NpcRuntime("npc-injured-capability", npcData);
        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });

        CapabilityEvaluationResult healthy = model.Evaluate(npc);
        Assert.That(npc.TryApplyInjury(NpcInjurySeverity.SeriouslyInjured), Is.True);
        CapabilityEvaluationResult injured = model.Evaluate(npc);

        Assert.That(healthy.BaseCapability, Is.EqualTo(40f).Within(0.001f));
        Assert.That(injured.BaseCapability, Is.EqualTo(40f).Within(0.001f));
        Assert.That(injured.EffectiveCapability, Is.LessThan(healthy.EffectiveCapability));
    }

    [Test]
    public void IncapacitatedNpcIsAliveButHasSeverelyReducedCapability()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        NpcData npcData = SimulationTestFactory.CreateNpc("incapacitated");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 100f));
        NpcRuntime npc = new NpcRuntime("npc-incapacitated", npcData);
        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });

        Assert.That(npc.TryApplyInjury(NpcInjurySeverity.Incapacitated), Is.True);
        CapabilityEvaluationResult result = model.Evaluate(npc);

        Assert.That(npc.IsAlive, Is.True);
        Assert.That(result.EffectiveCapability, Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void FatalConsequenceMarksNpcDead()
    {
        NpcRuntime npc = CreateNpc("fatal");

        Assert.That(npc.TryApplyDeath(), Is.True);
        Assert.That(npc.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.IsAlive, Is.False);
    }

    [Test]
    public void DeathDoesNotDeleteInventoryOrMoney()
    {
        ItemData item = SimulationTestFactory.CreateItem("kept-after-death");
        NpcRuntime npc = new NpcRuntime("npc-kept-after-death", SimulationTestFactory.CreateNpc("kept-after-death"), null, 125f);
        npc.Inventory.AddItem(item, 4, 7f);

        Assert.That(npc.TryApplyDeath(), Is.True);
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(4));
        Assert.That(npc.Inventory.GetAverageUnitCost(item), Is.EqualTo(7f).Within(0.001f));
        Assert.That(npc.Money, Is.EqualTo(125f).Within(0.001f));
    }

    [Test]
    public void DeadNpcCannotStartIndividualTravel()
    {
        ThreeCityFixture world = new ThreeCityFixture();
        NpcRuntime npc = new NpcRuntime("npc-dead-travel", SimulationTestFactory.CreateNpc("dead-travel"), world.A, 100f);
        TravelSystem travel = world.CreateTravelSystem(new SimulationTime());
        npc.TryApplyDeath();

        Assert.That(travel.CanStartTravel(npc, world.B, out _, out _), Is.False);
        Assert.That(travel.TryStartTravel(npc, world.B.Location, world.B), Is.False);
    }

    [Test]
    public void DeadNpcCannotJoinTravelParty()
    {
        TravelPartyFixture fixture = new TravelPartyFixture();
        fixture.Bruno.TryApplyDeath();
        ActionExecutionContext context = new ActionExecutionContext(
            "dead-group-travel",
            new[]
            {
                new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer)
            },
            fixture.World.B.Location.RuntimeId,
            fixture.World.RouteAB.RuntimeId);

        Assert.That(fixture.System.TryStartTravelParty(context, out _), Is.False);
        Assert.That(fixture.Bruno.ActiveTravelPartyId, Is.Null);
    }

    [Test]
    public void DeadNpcCannotStartExpedition()
    {
        ExpeditionGuardFixture fixture = new ExpeditionGuardFixture();
        fixture.Performer.TryApplyDeath();

        Assert.That(fixture.ExpeditionSystem.TryStartExpedition(fixture.Site, fixture.Context, out _), Is.False);
    }

    [Test]
    public void DeadNpcCannotJoinNewConflict()
    {
        NpcRuntime dead = CreateNpc("dead-conflict");
        dead.TryApplyDeath();
        Conflict conflict = new Conflict("conflict-dead-participant");
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        sideA.AddNpc(dead);
        sideB.AddAggregate(new AggregateParticipantSnapshot("other", 10f));

        Assert.That(conflict.TryValidate(out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("cannot join a new conflict"));
    }

    [Test]
    public void DeadNpcDoesNotTakeAutonomousActionOnLaterDay()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("dead-action-city", "dead-action-location");
        NpcData npcData = SimulationTestFactory.CreateNpc("dead-action");
        NpcActionData action = SimulationTestFactory.CreateAction("dead-action", NpcActionType.Normal);
        npcData.acoesPadrao.Add(new NPCDefaultAction { action = action, baseUtility = 100f });
        NpcRuntime npc = new NpcRuntime("npc-dead-action", npcData, city, 100f);
        npc.TryApplyDeath();
        SimulationRuntime simulation = new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(null));

        simulation.AdvanceDay();

        Assert.That(npc.CurrentAction, Is.Null);
        Assert.That(npc.CurrentActionRuntime, Is.Null);
    }

    [Test]
    public void DeadNpcDoesNotTradeOrShareKnowledgeAsLivingNpc()
    {
        ItemData item = SimulationTestFactory.CreateItem("dead-merchant-item");
        CityRuntime city = SimulationTestFactory.CreateCity("dead-merchant-city", "dead-merchant-location");
        NpcRuntime deadMerchant = new NpcRuntime(
            "npc-dead-merchant",
            SimulationTestFactory.CreateNpc("dead-merchant", NpcJobType.Merchant, MerchantBehavior.Local),
            city,
            100f);
        NpcRuntime livingMerchant = new NpcRuntime(
            "npc-living-merchant",
            SimulationTestFactory.CreateNpc("living-merchant", NpcJobType.Merchant, MerchantBehavior.Local),
            city,
            100f);
        deadMerchant.Inventory.AddItem(item, 1, 1f);
        deadMerchant.CommercialKnowledge.RecordObservation(new CommercialMarketObservation(
            city.Location.RuntimeId, item, 12f, 20, 0, 0, CommercialKnowledgeSource.DirectObservation, null));
        deadMerchant.TryApplyDeath();

        CommercialKnowledgeSharingSystem sharing = new CommercialKnowledgeSharingSystem(
            new SimulationTime(),
            new CommercialKnowledgeSettings());

        Assert.That(new EconomyTransactionService().TryExecuteNpcTrade(livingMerchant, deadMerchant, item, 1, 1f).Success, Is.False);
        sharing.ShareAmongPresentMerchants(new[] { deadMerchant, livingMerchant });
        Assert.That(livingMerchant.CommercialKnowledge.TryGetObservation(city.Location.RuntimeId, item.DefinitionId, out _), Is.False);
    }

    [Test]
    public void AggregateLossIsReturnedButDoesNotMutateNonexistentExternalDomain()
    {
        Conflict conflict = new Conflict("conflict-aggregate-loss");
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        sideA.AddAggregate(new AggregateParticipantSnapshot("aggregate-a", 10f));
        sideB.AddAggregate(new AggregateParticipantSnapshot("aggregate-b", 100f));

        ConflictResolutionResult result = CreateService(new SequenceConflictRandomSource(0.5f, 0.5f)).Compute(conflict);

        Assert.That(result.AggregateConsequences, Has.Count.EqualTo(2));
        Assert.That(result.AggregateConsequences[0].LossFraction, Is.GreaterThanOrEqualTo(0f));
        Assert.That(result.AggregateConsequences[0].RemainingCapability, Is.LessThanOrEqualTo(result.AggregateConsequences[0].OriginalCapability));
        Assert.That(conflict.Sides[0].Participants[0].Aggregate.BaseCapability, Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void ParticipantConstraintDoesNotMarkOutcomeAsExternallyConstrained()
    {
        NpcRuntime winner = CreateNpc("participant-provenance-winner");
        NpcRuntime constrained = CreateNpc("participant-provenance-constrained");
        Conflict conflict = CreateNpcConflict(winner, constrained, ConflictStakes.Low, 100f, 90f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(constrained.RuntimeId)
        {
            ForceDeath = true
        });

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f)).Compute(conflict, constraints);

        Assert.That(result.OutcomeSource, Is.EqualTo(ConflictOutcomeSource.Simulated));
        Assert.That(result.OutcomeWasExternallyConstrained, Is.False);
        Assert.That(result.ConsequencesWereExternallyConstrained, Is.True);
        Assert.That(constraints.HasExternalConstraints, Is.True);
    }

    [Test]
    public void ForcedWinnerMarksOnlyOutcomeAsExternallyConstrained()
    {
        NpcRuntime forcedWinner = CreateNpc("outcome-only-winner");
        NpcRuntime strongerOpponent = CreateNpc("outcome-only-opponent");
        Conflict conflict = CreateNpcConflict(forcedWinner, strongerOpponent, ConflictStakes.Meaningful, 1f, 100f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints
        {
            ForcedWinningSideId = "a"
        };

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f)).Compute(conflict, constraints);

        Assert.That(result.OutcomeWasExternallyConstrained, Is.True);
        Assert.That(result.ConsequencesWereExternallyConstrained, Is.False);
    }

    [Test]
    public void ForcedWinnerAndParticipantConstraintTrackBothSources()
    {
        NpcRuntime forcedWinner = CreateNpc("both-sources-winner");
        NpcRuntime forcedDead = CreateNpc("both-sources-dead");
        Conflict conflict = CreateNpcConflict(forcedWinner, forcedDead, ConflictStakes.Meaningful, 1f, 100f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints
        {
            ForcedWinningSideId = "a"
        };
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(forcedDead.RuntimeId)
        {
            ForceDeath = true
        });

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f)).Compute(conflict, constraints);

        Assert.That(result.OutcomeWasExternallyConstrained, Is.True);
        Assert.That(result.ConsequencesWereExternallyConstrained, Is.True);
    }

    [Test]
    public void FullySimulatedConflictHasNoExternalConstraintFlags()
    {
        NpcRuntime winner = CreateNpc("fully-simulated-winner");
        NpcRuntime loser = CreateNpc("fully-simulated-loser");
        Conflict conflict = CreateNpcConflict(winner, loser, ConflictStakes.Meaningful, 100f, 90f);

        ConflictResolutionResult result = CreateService(
            new SequenceConflictRandomSource(0.5f, 0.5f)).Compute(conflict);

        Assert.That(result.OutcomeWasExternallyConstrained, Is.False);
        Assert.That(result.ConsequencesWereExternallyConstrained, Is.False);
        Assert.That(result.WasSimulated, Is.True);
    }

    [Test]
    public void ParticipantConstraintDoesNotChangeRawOrFinalScores()
    {
        NpcRuntime first = CreateNpc("score-provenance-first");
        NpcRuntime second = CreateNpc("score-provenance-second");
        Conflict conflict = CreateNpcConflict(first, second, ConflictStakes.Meaningful, 100f, 90f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(second.RuntimeId)
        {
            ForcedInjurySeverity = NpcInjurySeverity.SeriouslyInjured
        });

        ConflictResolutionResult simulated = CreateService(
            new SequenceConflictRandomSource(0.2f, 0.8f)).Compute(conflict);
        ConflictResolutionResult constrained = CreateService(
            new SequenceConflictRandomSource(0.2f, 0.8f)).Compute(conflict, constraints);

        Assert.That(constrained.WinningSideId, Is.EqualTo(simulated.WinningSideId));
        for (int i = 0; i < simulated.SideResults.Count; i++)
        {
            Assert.That(constrained.SideResults[i].RawCapability, Is.EqualTo(simulated.SideResults[i].RawCapability));
            Assert.That(constrained.SideResults[i].ModifierAdjustedCapability, Is.EqualTo(simulated.SideResults[i].ModifierAdjustedCapability));
            Assert.That(constrained.SideResults[i].RandomFactor, Is.EqualTo(simulated.SideResults[i].RandomFactor));
            Assert.That(constrained.SideResults[i].FinalScore, Is.EqualTo(simulated.SideResults[i].FinalScore));
        }
    }

    [Test]
    public void ForcedParticipantDeathIsHonored()
    {
        NpcRuntime first = CreateNpc("forced-death-first");
        NpcRuntime second = CreateNpc("forced-death-second");
        Conflict conflict = CreateNpcConflict(first, second, ConflictStakes.Low, 100f, 100f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        ConflictParticipantResolutionConstraint participantConstraint = new ConflictParticipantResolutionConstraint(second.RuntimeId)
        {
            ForceDeath = true,
            ForcedInjurySeverity = NpcInjurySeverity.SeriouslyInjured
        };
        constraints.AddParticipantConstraint(participantConstraint);

        Assert.That(CreateService(new SequenceConflictRandomSource(0.5f, 0.5f)).TryResolveAndApply(
            conflict,
            constraints,
            out ConflictResolutionResult result,
            out string reason), Is.True, reason);

        Assert.That(second.IsDead, Is.True);
        Assert.That(result.NpcConsequences.FindByRuntimeId(second.RuntimeId).IsDead, Is.True);
    }

    [Test]
    public void ForcedWinnerAndForcedDeathCanCoexistWhileOtherConsequencesRemainSimulated()
    {
        NpcRuntime forcedWinner = CreateNpc("forced-winner");
        NpcRuntime forcedDead = CreateNpc("forced-dead");
        Conflict conflict = CreateNpcConflict(forcedWinner, forcedDead, ConflictStakes.Meaningful, 1f, 100f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints
        {
            ForcedWinningSideId = "a"
        };
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(forcedDead.RuntimeId)
        {
            ForceDeath = true
        });

        Assert.That(CreateService(new SequenceConflictRandomSource(0.5f, 0.5f)).TryResolveAndApply(
            conflict,
            constraints,
            out ConflictResolutionResult result,
            out string reason), Is.True, reason);

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.OutcomeWasExternallyConstrained, Is.True);
        Assert.That(result.ConsequencesWereExternallyConstrained, Is.True);
        Assert.That(forcedDead.IsDead, Is.True);
        Assert.That(forcedWinner.IsAlive, Is.True);
        Assert.That(forcedWinner.InjurySeverity, Is.Not.EqualTo(NpcInjurySeverity.None));
    }

    [Test]
    public void ConflictingConstraintsAreRejectedBeforeWorldMutation()
    {
        NpcRuntime first = CreateNpc("constraint-first");
        NpcRuntime second = CreateNpc("constraint-second");
        Conflict conflict = CreateNpcConflict(first, second, ConflictStakes.Existential, 100f, 100f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(first.RuntimeId)
        {
            ForceDeath = true,
            ForceAlive = true
        });

        Assert.That(CreateService(new SequenceConflictRandomSource(0.5f, 0.5f)).TryResolveAndApply(
            conflict,
            constraints,
            out _,
            out string reason), Is.False);
        Assert.That(reason, Does.Contain("both death and life"));
        Assert.That(first.IsAlive, Is.True);
        Assert.That(second.IsAlive, Is.True);
        Assert.That(first.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
        Assert.That(second.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
    }

    [Test]
    public void FailedConflictApplicationIsAtomic()
    {
        NpcRuntime first = CreateNpc("atomic-first");
        NpcRuntime second = CreateNpc("atomic-second");
        Conflict conflict = CreateNpcConflict(first, second, ConflictStakes.Meaningful, 100f, 90f);
        ConflictResolutionService service = CreateService(new SequenceConflictRandomSource(0.5f, 0.5f));
        ConflictResolutionResult result = service.Compute(conflict);
        second.TryApplyDeath();

        Assert.That(service.TryApply(conflict, result, out string reason), Is.False);
        Assert.That(reason, Does.Contain("cannot join a new conflict"));
        Assert.That(first.IsAlive, Is.True);
        Assert.That(first.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
    }

    [Test]
    public void ConflictResolvedEventIsRecordedOnlyAfterSuccessfulMutation()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime first = CreateNpc("event-first");
        NpcRuntime second = CreateNpc("event-second");
        Conflict conflict = CreateNpcConflict(first, second, ConflictStakes.Meaningful, 100f, 90f);
        ConflictResolutionService service = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(capabilityOverrides), new SequenceConflictRandomSource(0.5f, 0.5f)),
            null,
            records.EventRecorder);
        ConflictResolutionResult result = service.Compute(conflict);

        Assert.That(records.Events.Events, Has.Count.EqualTo(0));
        Assert.That(service.TryApply(conflict, result, out string reason), Is.True, reason);
        Assert.That(records.Events.Events, Has.Count.EqualTo(1));
        Assert.That(records.Events.Events[0], Is.TypeOf<ConflictResolvedEvent>());
        Assert.That(((ConflictResolvedEvent)records.Events.Events[0]).ConsequencesWereExternallyConstrained, Is.False);
        Assert.That(first.InjurySeverity, Is.EqualTo(result.NpcConsequences.FindByRuntimeId(first.RuntimeId).InjurySeverity));
    }

    [Test]
    public void ConflictResolvedEventPreservesDistinctConstraintProvenance()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime winner = CreateNpc("event-provenance-winner");
        NpcRuntime forcedDead = CreateNpc("event-provenance-dead");
        Conflict conflict = CreateNpcConflict(winner, forcedDead, ConflictStakes.Low, 100f, 90f);
        ConflictResolutionConstraints constraints = new ConflictResolutionConstraints();
        constraints.AddParticipantConstraint(new ConflictParticipantResolutionConstraint(forcedDead.RuntimeId)
        {
            ForceDeath = true
        });
        ConflictResolutionService service = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(capabilityOverrides), new SequenceConflictRandomSource(0.5f, 0.5f)),
            null,
            records.EventRecorder);

        Assert.That(service.TryResolveAndApply(conflict, constraints, out _, out string reason), Is.True, reason);
        ConflictResolvedEvent domainEvent = records.Events.Events[0] as ConflictResolvedEvent;

        Assert.That(domainEvent, Is.Not.Null);
        Assert.That(domainEvent.OutcomeSource, Is.EqualTo(ConflictOutcomeSource.Simulated));
        Assert.That(domainEvent.ConsequencesWereExternallyConstrained, Is.True);
        Assert.That(domainEvent.Resolution.OutcomeWasExternallyConstrained, Is.False);
    }

    [Test]
    public void ConflictResolvedEventUsesNpcRuntimeIdsForDomainParticipants()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = CreateNpc("event-npc");
        Conflict conflict = new Conflict("conflict-event-ids");
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        sideA.AddNpc(npc);
        sideB.AddAggregate(new AggregateParticipantSnapshot("army-snapshot", 100f));
        ConflictResolutionService service = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(capabilityOverrides), new SequenceConflictRandomSource(0.5f, 0.5f)),
            null,
            records.EventRecorder);

        Assert.That(service.TryResolveAndApply(conflict, null, out _, out string reason), Is.True, reason);
        ConflictResolvedEvent domainEvent = records.Events.Events[0] as ConflictResolvedEvent;

        Assert.That(domainEvent.GetParticipants(), Has.Count.EqualTo(1));
        Assert.That(domainEvent.GetParticipants()[0].RuntimeId, Is.EqualTo(npc.RuntimeId));
    }

    [Test]
    public void RecordingConflictResultDoesNotConsumeRandomOrMutateWorld()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        SequenceConflictRandomSource random = new SequenceConflictRandomSource(0.5f, 0.5f);
        Conflict conflict = new Conflict("conflict-recording-purity");
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        sideA.AddAggregate(new AggregateParticipantSnapshot("a", 10f));
        sideB.AddAggregate(new AggregateParticipantSnapshot("b", 8f));
        ConflictResolutionService service = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(capabilityOverrides), random),
            null,
            records.EventRecorder);
        ConflictResolutionResult result = service.Compute(conflict);
        int consumedBeforeApply = random.ConsumedCount;

        Assert.That(service.TryApply(conflict, result, out string reason), Is.True, reason);

        Assert.That(random.ConsumedCount, Is.EqualTo(consumedBeforeApply));
        Assert.That(result.NpcConsequences, Is.Empty);
    }

    [Test]
    public void ConflictWithNoOriginDecisionDoesNotInventDecision()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        Conflict conflict = new Conflict("conflict-no-decision");
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        sideA.AddAggregate(new AggregateParticipantSnapshot("a", 10f));
        sideB.AddAggregate(new AggregateParticipantSnapshot("b", 8f));
        ConflictResolutionService service = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(capabilityOverrides), new SequenceConflictRandomSource(0.5f, 0.5f)),
            null,
            records.EventRecorder);

        Assert.That(service.TryResolveAndApply(conflict, null, out _, out string reason), Is.True, reason);
        Assert.That(records.Events.Events[0].OriginDecisionId, Is.Null);
        Assert.That(records.Decisions.Decisions, Is.Empty);
    }

    [Test]
    public void ConflictWithOriginDecisionPreservesOriginDecisionId()
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        Conflict conflict = new Conflict("conflict-origin-decision", originDecisionId: "decision-origin");
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        sideA.AddAggregate(new AggregateParticipantSnapshot("a", 10f));
        sideB.AddAggregate(new AggregateParticipantSnapshot("b", 8f));
        ConflictResolutionService service = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(capabilityOverrides), new SequenceConflictRandomSource(0.5f, 0.5f)),
            null,
            records.EventRecorder);

        Assert.That(service.TryResolveAndApply(conflict, null, out _, out string reason), Is.True, reason);
        Assert.That(records.Events.Events[0].OriginDecisionId, Is.EqualTo("decision-origin"));
        Assert.That(records.Decisions.Decisions, Is.Empty);
    }

    private static ConflictResolutionService CreateService(IConflictRandomSource randomSource)
    {
        return new ConflictResolutionService(new ConflictResolver(new FixedCapabilityModel(capabilityOverrides), randomSource));
    }

    private static Conflict CreateNpcConflict(
        NpcRuntime first,
        NpcRuntime second,
        ConflictStakes stakes,
        float firstCapability,
        float secondCapability)
    {
        Conflict conflict = new Conflict("conflict-" + first.RuntimeId);
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, stakes);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, stakes);
        sideA.AddNpc(first);
        sideB.AddNpc(second);
        capabilityOverrides[first.RuntimeId] = firstCapability;
        capabilityOverrides[second.RuntimeId] = secondCapability;
        return conflict;
    }

    private static NpcRuntime CreateNpc(string id)
    {
        return new NpcRuntime("npc-" + id, SimulationTestFactory.CreateNpc(id));
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        private readonly Dictionary<string, float> values = new Dictionary<string, float>(StringComparer.Ordinal);

        public FixedCapabilityModel(IReadOnlyDictionary<string, float> overrides = null)
        {
            if (overrides == null)
            {
                return;
            }

            foreach (KeyValuePair<string, float> pair in overrides)
            {
                values[pair.Key] = pair.Value;
            }
        }

        public CapabilityEvaluationResult Evaluate(
            NpcRuntime participant,
            CapabilityEvaluationContext context = null)
        {
            float value = values.TryGetValue(participant.RuntimeId, out float configured) ? configured : 100f;
            return new CapabilityEvaluationResult(value, value * participant.GetCapabilityMultiplier(), null);
        }
    }

    private sealed class ExpeditionGuardFixture
    {
        public readonly NpcRuntime Performer;
        public readonly ExplorableSiteRuntime Site;
        public readonly ExpeditionSystem ExpeditionSystem;
        public readonly ActionExecutionContext Context;

        public ExpeditionGuardFixture()
        {
            CityRuntime city = SimulationTestFactory.CreateCity("expedition-guard-city", "expedition-guard-location");
            SpatialLocationRuntime siteLocation = new SpatialLocationRuntime("expedition-guard-site-location");
            SpatialRouteRuntime route = new SpatialRouteRuntime(
                "expedition-guard-route",
                city.Location,
                siteLocation,
                1);
            RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
            SpatialNetworkRuntime network = new SpatialNetworkRuntime(registry);
            network.RegisterLocation(city.Location);
            network.RegisterLocation(siteLocation);
            network.RegisterRoute(route);
            Site = new ExplorableSiteRuntime(
                "expedition-guard-site",
                SimulationTestFactory.CreateExplorableSite("expedition-guard-site-definition"),
                siteLocation);
            ExplorableSiteStore sites = new ExplorableSiteStore();
            sites.Add(Site);
            RecordFixture records = SimulationTestFactory.CreateRecordFixture();
            TravelSystem travel = new TravelSystem(
                network,
                location => location == city.Location ? city : null,
                0f,
                records.EventRecorder,
                null);
            TravelPartyStore parties = new TravelPartyStore();
            TravelPartySystem partySystem = new TravelPartySystem(
                parties,
                records.Allocator,
                registry,
                travel,
                records.Time,
                records.Sequence,
                records.EventRecorder);
            ExplorableSiteKnowledgeSystem knowledge = new ExplorableSiteKnowledgeSystem();
            ExpeditionSystem = new ExpeditionSystem(
                new ExpeditionStore(),
                records.Allocator,
                registry,
                sites,
                partySystem,
                parties,
                knowledge,
                records.Time,
                records.EventRecorder);
            Performer = new NpcRuntime(
                "npc-expedition-guard",
                SimulationTestFactory.CreateNpc("expedition-guard-performer"),
                city,
                100f);
            registry.RegisterNpc(Performer);
            Performer.SpatialKnowledge.DiscoverLocation(city.Location.RuntimeId);
            Performer.SpatialKnowledge.DiscoverLocation(siteLocation.RuntimeId);
            Performer.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
            knowledge.RecordInitialScenarioKnowledge(Performer, Site);
            Context = new ActionExecutionContext(
                "expedition-guard-action",
                new[] { new ActionExecutionParticipant(Performer.RuntimeId, ActionExecutionParticipantRole.Performer) },
                siteLocation.RuntimeId,
                route.RuntimeId);
        }
    }
}

internal static class ConflictConsequenceTestExtensions
{
    public static ConflictNpcConsequence FindByRuntimeId(
        this IReadOnlyList<ConflictNpcConsequence> consequences,
        string runtimeId)
    {
        foreach (ConflictNpcConsequence consequence in consequences)
        {
            if (consequence != null && consequence.RuntimeId == runtimeId)
            {
                return consequence;
            }
        }

        return null;
    }
}
