using System;
using NUnit.Framework;

[TestFixture]
public sealed class ScheduledDirectiveStoreTests
{
    private const int EscapePrisonActionType = 8;

    [Test]
    public void AddAndTerminalTransitionAdvanceTheCoreOwnerRevisionOnce()
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime(3L));
        ScheduledDirective directive = CreateDirective("directive/core", 4L, "action/escape");

        Assert.That(store.Add(directive), Is.True);
        Assert.That(store.Revision, Is.EqualTo(1L));
        Assert.That(store.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(store));
        Assert.That(store.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(store.GetCurrentCensus().Revision, Is.EqualTo(1L));

        Assert.That(directive.MarkSucceeded(4L), Is.True);
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
        Assert.That(directive.ActionDefinitionId, Is.EqualTo("action/escape"));
        Assert.That(store.Revision, Is.EqualTo(2L));
        Assert.That(directive.MarkFailed(4L, "duplicate"), Is.False);
        Assert.That(store.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void InitialInvalidScheduleIsOneCommittedSkippedOwnerRow()
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime(5L));
        ScheduledDirective directive = CreateDirective("directive/invalid", 0L, "action/escape");

        Assert.That(store.Add(directive), Is.True);
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(directive.ProcessedDay, Is.EqualTo(5L));
        Assert.That(directive.ResultReason, Is.EqualTo("AbsoluteDay must be at least 1."));
        Assert.That(store.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void P12FDetachedCaptureAndStagePreserveIdentityTerminalStateAndExactRevision()
    {
        ScheduledDirectiveStore source = new ScheduledDirectiveStore(new SimulationTime(2L));
        ScheduledDirective directive = CreateDirective("directive/staged", 3L, "action/escape");
        Assert.That(source.Add(directive), Is.True);
        Assert.That(directive.MarkFailed(3L, "preserved failure"), Is.True);

        Assert.That(source.TryCaptureP12FState(out var detachedRows, out long capturedRevision), Is.True);
        Assert.That(capturedRevision, Is.EqualTo(2L));
        Assert.That(detachedRows, Has.Count.EqualTo(1));
        Assert.That(detachedRows[0], Is.Not.SameAs(directive));
        Assert.That(detachedRows[0].ActionDefinitionId, Is.EqualTo("action/escape"));
        Assert.That(detachedRows[0].State, Is.EqualTo(ScheduledDirectiveState.Failed));
        Assert.That(detachedRows[0].ProcessedDay, Is.EqualTo(3L));
        Assert.That(detachedRows[0].ResultReason, Is.EqualTo("preserved failure"));

        Assert.That(ScheduledDirectiveStore.TryCreateP12FStaged(
            new SimulationTime(3L), detachedRows, capturedRevision, out ScheduledDirectiveStore staged), Is.True);
        Assert.That(staged, Is.Not.SameAs(source));
        Assert.That(staged.Revision, Is.EqualTo(2L));
        Assert.That(staged.Directives, Has.Count.EqualTo(1));
        Assert.That(staged.Directives[0].DirectiveId, Is.EqualTo("directive/staged"));
        Assert.That(staged.Directives[0].State, Is.EqualTo(ScheduledDirectiveState.Failed));
        Assert.That(staged.Directives[0].ResultReason, Is.EqualTo("preserved failure"));
        Assert.That(staged.Directives[0].MarkSucceeded(4L), Is.False);
        Assert.That(staged.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void DirectiveRejectsActionTypeOutsideItsBoundEscapeOperation()
    {
        Assert.Throws<ArgumentException>(() => new ScheduledDirective(
            "directive/wrong-action", 1L, ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison, "npc/actor", "action/travel", 5));
        Assert.Throws<ArgumentException>(() => CreateDirective("directive/missing-action", 1L, " "));
    }

    [Test]
    public void PrepareAndConsumeUsesCoreTransientIndexWithoutOwnerRevision()
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirective directive = CreateDirective("directive/transient", 1L, "action/escape");
        Assert.That(store.Add(directive), Is.True);
        long committedRevision = store.Revision;

        store.PrepareDay(1L, actorRuntimeId => actorRuntimeId == "npc/actor");
        Assert.That(store.TryTakeDirective("npc/actor", out ScheduledDirective taken), Is.True);
        Assert.That(taken, Is.SameAs(directive));
        Assert.That(store.TryTakeDirective("npc/actor", out _), Is.False);
        Assert.That(store.Revision, Is.EqualTo(committedRevision));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Pending));
    }

    [Test]
    public void SelectedDailyPreparationUsesCoreNpcIdentityKind()
    {
        DailyV1RuntimeIdentityIndex npcIndex = new DailyV1RuntimeIdentityIndex();
        Assert.That(npcIndex.TryStageRegister("npc/actor", DailyV1RuntimeIdentityKind.Npc), Is.True);
        Assert.That(npcIndex.TryAdvanceRevision(), Is.True);
        ScheduledDirectiveStore resolvedStore = new ScheduledDirectiveStore(new SimulationTime());
        Assert.That(resolvedStore.Add(CreateDirective("directive/npc", 1L, "action/escape")), Is.True);

        resolvedStore.PrepareSelectedDailyV1Day(1L, npcIndex);
        Assert.That(resolvedStore.TryTakeDirective("npc/actor", out _), Is.True);

        DailyV1RuntimeIdentityIndex cityIndex = new DailyV1RuntimeIdentityIndex();
        Assert.That(cityIndex.TryStageRegister("npc/actor", DailyV1RuntimeIdentityKind.City), Is.True);
        Assert.That(cityIndex.TryAdvanceRevision(), Is.True);
        ScheduledDirectiveStore rejectedStore = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirective rejected = CreateDirective("directive/wrong-kind", 1L, "action/escape");
        Assert.That(rejectedStore.Add(rejected), Is.True);

        rejectedStore.PrepareSelectedDailyV1Day(1L, cityIndex);
        Assert.That(rejected.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(rejected.ResultReason, Is.EqualTo(
            "Actor RuntimeId 'npc/actor' could not be resolved."));
    }

    [Test]
    public void PrepareMarksConflictingAndUnresolvedRowsThroughOwnerTransitions()
    {
        ScheduledDirectiveStore conflictStore = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirective first = CreateDirective("directive/conflict-1", 1L, "action/escape");
        ScheduledDirective second = new ScheduledDirective("directive/conflict-2", 1L,
            ScheduledDirectiveMode.ForceOutcome, ScheduledDirectiveOperation.EscapePrison,
            "npc/actor", "action/escape", EscapePrisonActionType);
        Assert.That(conflictStore.Add(first), Is.True);
        Assert.That(conflictStore.Add(second), Is.True);
        conflictStore.PrepareDay(1L, _ => true);
        Assert.That(first.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(second.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(first.ResultReason, Does.Contain("Multiple significant directives"));
        Assert.That(second.ResultReason, Is.EqualTo(first.ResultReason));
        Assert.That(conflictStore.Revision, Is.EqualTo(4L));

        ScheduledDirectiveStore unresolvedStore = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirective unresolved = CreateDirective("directive/unresolved", 1L, "action/escape");
        Assert.That(unresolvedStore.Add(unresolved), Is.True);
        unresolvedStore.PrepareDay(1L, _ => false);
        Assert.That(unresolved.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        Assert.That(unresolved.ResultReason, Is.EqualTo("Actor RuntimeId 'npc/actor' could not be resolved."));
        Assert.That(unresolvedStore.Revision, Is.EqualTo(2L));
    }

    private static ScheduledDirective CreateDirective(string id, long absoluteDay, string actionDefinitionId) =>
        new ScheduledDirective(id, absoluteDay, ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison, "npc/actor", actionDefinitionId,
            EscapePrisonActionType);
}
