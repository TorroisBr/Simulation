using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ConflictFoundationTests
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
    public void ConflictRequiresAtLeastTwoSides()
    {
        Conflict conflict = new Conflict("conflict-one");
        conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);

        Assert.That(conflict.TryValidate(out string diagnostic), Is.False);
        Assert.That(diagnostic, Is.EqualTo("Conflict requires at least two sides."));
    }

    [Test]
    public void NpcCannotBelongToMultipleSides()
    {
        NpcRuntime npc = CreateNpc("shared");
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddNpc(npc);
        conflict.Sides[1].AddNpc(npc);

        Assert.That(conflict.TryValidate(out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("cannot belong to multiple conflict sides"));
    }

    [Test]
    public void IndividualAndAggregateParticipantsCanCoexist()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddNpc(CreateNpc("lich"));
        conflict.Sides[0].AddNpc(CreateNpc("lieutenant"));
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("undead-army", 100f, 500));
        conflict.Sides[1].AddNpc(CreateNpc("guard-one"));
        conflict.Sides[1].AddNpc(CreateNpc("guard-two"));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("guard-force", 90f, 100));

        ConflictResolutionResult result = Resolve(conflict, new FixedCapabilityModel(10f), new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(conflict.TryValidate(out string diagnostic), Is.True, diagnostic);
        Assert.That(result.SideResults[0].ParticipantContributions, Has.Count.EqualTo(3));
        Assert.That(result.SideResults[0].ParticipantContributions[0].Kind, Is.EqualTo(ConflictParticipantKind.Npc));
        Assert.That(result.SideResults[0].ParticipantContributions[2].Kind, Is.EqualTo(ConflictParticipantKind.Aggregate));
        Assert.That(result.SideResults[1].ParticipantContributions, Has.Count.EqualTo(3));
    }

    [Test]
    public void ResolverUsesInjectedCapabilityModel()
    {
        Conflict conflict = CreateConflictWithSides();
        NpcRuntime npc = CreateNpc("model-consumer");
        conflict.Sides[0].AddNpc(npc);
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("opposition", 10f));
        FixedCapabilityModel model = new FixedCapabilityModel(100f);

        ConflictResolutionResult result = Resolve(conflict, model, new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(model.LastParticipant, Is.SameAs(npc));
        Assert.That(result.SideResults[0].RawCapability, Is.EqualTo(100f).Within(0.001f));
    }

    [Test]
    public void StrongerSideWinsWithNeutralDeterministicRandom()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("strong", 100f));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("weak", 10f));

        ConflictResolutionResult result = Resolve(conflict, new FixedCapabilityModel(0f), new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(result.Outcome, Is.EqualTo(ConflictOutcomeType.Victory));
        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.SideResults[0].RandomFactor, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void CloseSidesCanProduceUpsetWithDeterministicRandom()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("slightly-stronger", 100f));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("slightly-weaker", 99f));

        ConflictResolutionResult result = Resolve(conflict, new FixedCapabilityModel(0f), new SequenceConflictRandomSource(0f, 1f));

        Assert.That(result.WinningSideId, Is.EqualTo("b"));
        Assert.That(result.SideResults[0].RandomFactor, Is.EqualTo(0.8f).Within(0.001f));
        Assert.That(result.SideResults[1].RandomFactor, Is.EqualTo(1.2f).Within(0.001f));
    }

    [Test]
    public void HugeCapabilityDifferenceCannotBeFlippedByNormalBoundedRandom()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("overwhelming", 500f));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("tiny", 10f));

        ConflictResolutionResult result = Resolve(conflict, new FixedCapabilityModel(0f), new SequenceConflictRandomSource(0f, 1f));

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
    }

    [Test]
    public void DrawIsPossible()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("equal-a", 100f));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("equal-b", 100f));

        ConflictResolutionResult result = Resolve(conflict, new FixedCapabilityModel(0f), new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(result.Outcome, Is.EqualTo(ConflictOutcomeType.Draw));
        Assert.That(result.WinningSideId, Is.Null);
        Assert.That(result.SideResults[0].Disposition, Is.EqualTo(ConflictSideDisposition.Stalemate));
    }

    [Test]
    public void ContextModifierCanChangeOutcome()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("a-force", 10f));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("b-force", 12f));
        conflict.AddModifier(new ConflictModifier("fortification", "a", 5f));

        ConflictResolutionResult result = Resolve(conflict, new FixedCapabilityModel(0f), new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.SideResults[0].AppliedModifiers, Has.Count.EqualTo(1));
        Assert.That(result.SideResults[0].ModifierAdjustedCapability, Is.EqualTo(15f).Within(0.001f));
    }

    [Test]
    public void ResultContainsStructuredCapabilityBreakdown()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        NpcData npcData = SimulationTestFactory.CreateNpc("breakdown");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 20f));
        NpcRuntime npc = new NpcRuntime("npc-breakdown", npcData);
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddNpc(npc);
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("opposition", 1f));
        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });

        ConflictResolutionResult result = Resolve(conflict, model, new SequenceConflictRandomSource(0.5f, 0.5f));
        ConflictParticipantCapabilityResult contribution = result.SideResults[0].ParticipantContributions[0];

        Assert.That(contribution.CapabilityBreakdown, Has.Count.EqualTo(2));
        Assert.That(contribution.CapabilityBreakdown[0].Source, Is.EqualTo(CapabilityContributionSource.BaseAttribute));
        Assert.That(contribution.CapabilityBreakdown[0].AttributeDefinitionId, Is.EqualTo("physical"));
        Assert.That(contribution.EffectiveCapability, Is.EqualTo(20f).Within(0.001f));
    }

    [Test]
    public void ForcedWinnerIsHonoredWithoutChangingRawScores()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("weak", 1f));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("strong", 100f));

        ConflictResolutionResult result = Resolve(
            conflict,
            new FixedCapabilityModel(0f),
            new SequenceConflictRandomSource(0.5f, 0.5f),
            new ConflictResolutionConstraints { ForcedWinningSideId = "a" });

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(result.OutcomeWasExternallyConstrained, Is.True);
        Assert.That(result.SideResults[0].RawCapability, Is.EqualTo(1f).Within(0.001f));
        Assert.That(result.SideResults[1].RawCapability, Is.EqualTo(100f).Within(0.001f));
    }

    [Test]
    public void ForcedResultIsMarkedAsExternallyConstrained()
    {
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddAggregate(new AggregateParticipantSnapshot("a-force", 10f));
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("b-force", 10f));

        ConflictResolutionResult forced = Resolve(
            conflict,
            new FixedCapabilityModel(0f),
            new SequenceConflictRandomSource(0.5f, 0.5f),
            new ConflictResolutionConstraints { ForcedWinningSideId = "b" });
        ConflictResolutionResult simulated = Resolve(
            conflict,
            new FixedCapabilityModel(0f),
            new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(forced.OutcomeWasExternallyConstrained, Is.True);
        Assert.That(forced.WasSimulated, Is.False);
        Assert.That(simulated.WasSimulated, Is.True);
    }

    [Test]
    public void AggregateParticipantDoesNotEnterRuntimeIdentityRegistry()
    {
        AggregateParticipantSnapshot aggregate = new AggregateParticipantSnapshot("army-snapshot", 50f);
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();

        LogAssert.Expect(LogType.Warning, "NPC runtime resolution failed: RuntimeId 'army-snapshot' is not registered.");
        Assert.That(registry.TryGetNpc(aggregate.SourceId, out _), Is.False);
    }

    [Test]
    public void ConflictIdSequenceIsIndependent()
    {
        ConflictIdAllocator first = new ConflictIdAllocator();
        RuntimeIdAllocator runtime = new RuntimeIdAllocator();
        ConflictIdAllocator second = new ConflictIdAllocator();

        Assert.That(first.AllocateConflictId(), Is.EqualTo("conflict-000001"));
        Assert.That(runtime.AllocateNpcId(), Is.EqualTo("npc-000001"));
        Assert.That(first.AllocateConflictId(), Is.EqualTo("conflict-000002"));
        Assert.That(second.AllocateConflictId(), Is.EqualTo("conflict-000001"));
    }

    [Test]
    public void ResolvingConflictWithoutApplyingConsequencesDoesNotMutateWorld()
    {
        CapabilityAttributeData physical = SimulationTestFactory.CreateCapabilityAttribute("physical");
        CapabilityAttributeData mental = SimulationTestFactory.CreateCapabilityAttribute("mental");
        ItemData item = SimulationTestFactory.CreateItem("item");
        item.capabilityModifiers.Add(new CapabilityAttributeModifier(physical, 5f));
        NpcData npcData = SimulationTestFactory.CreateNpc("unchanged");
        npcData.capabilityValues.Add(new CapabilityAttributeValue(physical, 10f));
        NpcRuntime npc = new NpcRuntime("npc-unchanged", npcData, null, 75f);
        npc.Inventory.AddItem(item, 2, 3f);
        Conflict conflict = CreateConflictWithSides();
        conflict.Sides[0].AddNpc(npc);
        conflict.Sides[1].AddAggregate(new AggregateParticipantSnapshot("other", 1f));
        GenericCapabilityModel model = new GenericCapabilityModel(new GenericCapabilityModelConfiguration
        {
            physicalAttribute = physical,
            mentalAttribute = mental
        });

        Resolve(conflict, model, new SequenceConflictRandomSource(0.5f, 0.5f));

        Assert.That(npc.Money, Is.EqualTo(75f).Within(0.001f));
        Assert.That(npc.Inventory.GetAmount(item), Is.EqualTo(2));
        Assert.That(npc.Inventory.GetAverageUnitCost(item), Is.EqualTo(3f).Within(0.001f));
        Assert.That(npc.CurrentAction, Is.Null);
    }

    [Test]
    public void ThreeSidesCanBeResolved()
    {
        Conflict conflict = new Conflict("conflict-three");
        ConflictSide sideA = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        ConflictSide sideB = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        ConflictSide sideC = conflict.AddSide("c", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        sideA.AddAggregate(new AggregateParticipantSnapshot("a-force", 100f));
        sideB.AddAggregate(new AggregateParticipantSnapshot("b-force", 20f));
        sideC.AddAggregate(new AggregateParticipantSnapshot("c-force", 10f));

        ConflictResolutionResult result = Resolve(conflict, new FixedCapabilityModel(0f), new SequenceConflictRandomSource(0.5f, 0.5f, 0.5f));

        Assert.That(result.SideResults, Has.Count.EqualTo(3));
        Assert.That(result.WinningSideId, Is.EqualTo("a"));
    }

    private static Conflict CreateConflictWithSides()
    {
        Conflict conflict = new Conflict("conflict-test");
        conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Meaningful);
        return conflict;
    }

    private static NpcRuntime CreateNpc(string id)
    {
        return new NpcRuntime("npc-" + id, SimulationTestFactory.CreateNpc(id));
    }

    private static ConflictResolutionResult Resolve(
        Conflict conflict,
        ICapabilityModel capabilityModel,
        IConflictRandomSource randomSource,
        ConflictResolutionConstraints constraints = null)
    {
        return new ConflictResolver(capabilityModel, randomSource).Resolve(conflict, constraints);
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        private readonly float value;

        public NpcRuntime LastParticipant { get; private set; }

        public FixedCapabilityModel(float value)
        {
            this.value = value;
        }

        public CapabilityEvaluationResult Evaluate(
            NpcRuntime participant,
            CapabilityEvaluationContext context = null)
        {
            LastParticipant = participant;
            return new CapabilityEvaluationResult(value, value, null);
        }
    }
}
