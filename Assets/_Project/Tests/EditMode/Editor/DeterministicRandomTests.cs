using System.Collections.Generic;
using System.Reflection;
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

    [Test]
    public void StreamCounterWrapsAndRejectsTheFollowingNegativeDrawIndex()
    {
        DeterministicRandomState state = new DeterministicRandomState(1, "rollover", long.MaxValue);
        DeterministicRandomSource source = new DeterministicRandomSource(1);
        ConstructorInfo streamConstructor = typeof(DeterministicRandomStream).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(IAuthoritativeRandomSource), typeof(DeterministicRandomState) },
            null);

        Assert.That(streamConstructor, Is.Not.Null);
        DeterministicRandomStream stream = (DeterministicRandomStream)streamConstructor.Invoke(
            new object[] { source, state });
        float maximumDraw = stream.NextUnit();
        Assert.That(maximumDraw, Is.EqualTo(source.NextUnit(state.StreamKey, long.MaxValue)));
        Assert.That(state.DrawIndex, Is.EqualTo(long.MinValue));
        Assert.That(() => stream.NextUnit(), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(state.DrawIndex, Is.EqualTo(long.MinValue + 1L));
    }
}
