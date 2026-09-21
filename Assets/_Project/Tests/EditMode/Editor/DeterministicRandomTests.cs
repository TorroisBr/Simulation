using System.Collections.Generic;
using NUnit.Framework;

public sealed class DeterministicRandomTests
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
    public void SameSeedContextAndDrawProduceTheSameValue()
    {
        DeterministicRandomSource first = new DeterministicRandomSource(12345);
        DeterministicRandomSource second = new DeterministicRandomSource(12345);

        Assert.That(second.NextUnit("operation|npc-a|day-4", 7L), Is.EqualTo(
            first.NextUnit("operation|npc-a|day-4", 7L)));
    }

    [Test]
    public void UnrelatedStreamConsumptionDoesNotShiftOperationStream()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(77);
        float expected = source.NextUnit("operation-a", 3L);

        source.NextUnit("diagnostic", 0L);
        source.NextUnit("diagnostic", 1L);
        source.NextUnit("other-operation", 0L);

        Assert.That(source.NextUnit("operation-a", 3L), Is.EqualTo(expected));
    }

    [Test]
    public void StreamStateMakesDrawPositionExplicitAndIndependent()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(9);
        DeterministicRandomStream first = source.CreateStream("operation");
        DeterministicRandomStream second = source.CreateStream("operation");

        float firstValue = first.NextUnit();
        float secondValue = second.NextUnit();

        Assert.That(secondValue, Is.EqualTo(firstValue));
        Assert.That(first.State.DrawIndex, Is.EqualTo(1L));
        Assert.That(second.State.DrawIndex, Is.EqualTo(1L));
    }

    [Test]
    public void NpcDecisionSelectionDoesNotDependOnActionInsertionOrder()
    {
        NpcRuntime npc = new NpcRuntime(
            "npc-order",
            SimulationTestFactory.CreateNpc("npc-order"));
        NpcActionData firstAction = SimulationTestFactory.CreateAction("action-a", NpcActionType.Normal);
        NpcActionData secondAction = SimulationTestFactory.CreateAction("action-b", NpcActionType.Normal);
        NpcDecisionSystem firstSystem = new NpcDecisionSystem(
            new List<INpcActionProvider>(),
            new DeterministicRandomSource(100));
        NpcDecisionSystem secondSystem = new NpcDecisionSystem(
            new List<INpcActionProvider>(),
            new DeterministicRandomSource(100));

        NpcActionRuntime first = firstSystem.ChooseAction(
            npc,
            new List<NpcActionData> { firstAction, secondAction },
            12L);
        NpcActionRuntime second = secondSystem.ChooseAction(
            npc,
            new List<NpcActionData> { secondAction, firstAction },
            12L);

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(second.Action.DefinitionId, Is.EqualTo(first.Action.DefinitionId));
    }
}
