using NUnit.Framework;

public sealed class ScheduledDirectiveTests
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
    public void ScheduledDirectiveStore_PreparesAndConsumesRequestActionOnce()
    {
        SimulationTime time = new SimulationTime();
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        NpcRuntime actor = new NpcRuntime("npc-actor", SimulationTestFactory.CreateNpc("actor"));
        registry.RegisterNpc(actor);
        NpcActionData escape = SimulationTestFactory.CreateAction("escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        ScheduledDirective directive = new ScheduledDirective(
            "directive-1", 1L, ScheduledDirectiveMode.RequestAction, ScheduledDirectiveOperation.EscapePrison, actor.RuntimeId, escape);
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(time);
        ScheduledDirectiveSystem system = new ScheduledDirectiveSystem(store, registry);

        Assert.That(store.Add(directive), Is.True);
        system.PrepareDay(1L);
        Assert.That(system.TryTakeDirective(actor, out ScheduledDirective taken), Is.True);
        Assert.That(taken, Is.SameAs(directive));
        Assert.That(system.TryTakeDirective(actor, out _), Is.False);
    }

    [Test]
    public void ScheduledDirectiveStore_RejectsDuplicateDirectiveId()
    {
        SimulationTime time = new SimulationTime();
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(time);
        NpcActionData escape = SimulationTestFactory.CreateAction("escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        ScheduledDirective first = new ScheduledDirective(
            "directive-1", 1L, ScheduledDirectiveMode.ForceOutcome, ScheduledDirectiveOperation.EscapePrison, "npc-actor", escape);
        ScheduledDirective duplicate = new ScheduledDirective(
            "directive-1", 2L, ScheduledDirectiveMode.ForceOutcome, ScheduledDirectiveOperation.EscapePrison, "npc-actor", escape);

        Assert.That(store.Add(first), Is.True);
        Assert.That(store.Add(duplicate), Is.False);
    }

    [Test]
    public void ScheduledDirectiveDecision_UsesScheduledDirectiveOrigin()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime actor = new NpcRuntime("npc-actor", SimulationTestFactory.CreateNpc("actor"));
        NpcActionData escape = SimulationTestFactory.CreateAction("escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);

        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordChosenAction(
            actor,
            new NpcActionRuntime(escape),
            NpcDecisionOrigin.ScheduledDirective);

        Assert.That(decision.Origin, Is.EqualTo(NpcDecisionOrigin.ScheduledDirective));
        Assert.That(decision.DecisionType, Is.EqualTo(NpcDecisionType.Escape));
    }

    [Test]
    public void ForceOutcomeDirective_IsRepresentedWithoutAutonomousOrigin()
    {
        NpcActionData escape = SimulationTestFactory.CreateAction("escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        ScheduledDirective directive = new ScheduledDirective(
            "directive-force", 1L, ScheduledDirectiveMode.ForceOutcome, ScheduledDirectiveOperation.EscapePrison, "npc-actor", escape);

        Assert.That(directive.Mode, Is.EqualTo(ScheduledDirectiveMode.ForceOutcome));
        Assert.That(directive.IsPending, Is.True);
    }
}
