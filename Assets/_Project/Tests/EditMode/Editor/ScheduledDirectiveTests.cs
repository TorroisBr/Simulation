using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
        LogAssert.Expect(LogType.Error, "Cannot add duplicate DirectiveId 'directive-1'.");
        Assert.That(store.Add(duplicate), Is.False);
    }

    [Test]
    public void ScheduledDirectiveDecision_UsesScheduledDirectiveOrigin()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture(1L);
        CityRuntime city = SimulationTestFactory.CreateCity("city-request", "location-request");
        NpcRuntime guard = new NpcRuntime("npc-guard", SimulationTestFactory.CreateNpc("guard"), city, 0f);
        NpcRuntime actor = new NpcRuntime("npc-actor", SimulationTestFactory.CreateNpc("actor"), city, 0f);
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(actor), Is.True);
        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus("free");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus("wanted");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus("arrested");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus("hidden");
        JusticeSystem justice = new JusticeSystem(
            freeStatus,
            wantedStatus,
            arrestedStatus,
            hiddenStatus,
            fixture.EventRecorder,
            null);
        justice.CreateOrIncreaseWarrant(actor, city, 50f, 3);
        Assert.That(justice.Arrest(guard, actor, city), Is.True);
        justice.BeginDay();

        NpcActionData escape = SimulationTestFactory.CreateAction("escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        ScheduledDirective directive = new ScheduledDirective(
            "directive-request", 1L, ScheduledDirectiveMode.RequestAction, ScheduledDirectiveOperation.EscapePrison, actor.RuntimeId, escape);
        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(fixture.Time);
        ScheduledDirectiveSystem directiveSystem = new ScheduledDirectiveSystem(directiveStore, registry);
        Assert.That(directiveStore.Add(directive), Is.True);
        directiveSystem.PrepareDay(1L);
        Assert.That(directiveSystem.TryTakeDirective(actor, out ScheduledDirective taken), Is.True);

        CrimeSystem crime = new CrimeSystem(justice, null, hiddenStatus);
        NpcDecisionSystem npcDecisionSystem = new NpcDecisionSystem(new List<INpcActionProvider> { crime });
        NpcActionRuntime requestedAction = npcDecisionSystem.CreateRequestedAction(actor, taken.Action);
        Assert.That(requestedAction, Is.Not.Null);

        NpcDecisionRecord decision = fixture.DecisionRecorder.RecordChosenAction(
            actor,
            requestedAction,
            NpcDecisionOrigin.ScheduledDirective);
        actor.SetCurrentActionRuntime(requestedAction);
        Assert.That(crime.TryExecuteAction(actor, requestedAction).Success, Is.True);
        Assert.That(directive.MarkSucceeded(fixture.Time.AbsoluteDay), Is.True);

        Assert.That(decision.Origin, Is.EqualTo(NpcDecisionOrigin.ScheduledDirective));
        Assert.That(decision.DecisionType, Is.EqualTo(NpcDecisionType.Escape));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
        Assert.That(fixture.Events.Events, Has.Count.EqualTo(2));
        NpcEscapedEvent escaped = fixture.Events.Events[1] as NpcEscapedEvent;
        Assert.That(escaped, Is.Not.Null);
        Assert.That(escaped.OriginDecisionId, Is.EqualTo(decision.DecisionId));
        SimulationInvariantValidator.ValidateDecisions(fixture.Decisions.Decisions);
        SimulationInvariantValidator.ValidateDomainEvents(fixture.Events.Events, null, fixture.Decisions);
    }

    [Test]
    public void ForceOutcomeDirective_UsesCanonicalEscapeTransitionWithoutAutonomousDecision()
    {
        RecordFixture fixture = SimulationTestFactory.CreateRecordFixture(1L);
        CityRuntime city = SimulationTestFactory.CreateCity("city-prison", "location-prison");
        NpcRuntime guard = new NpcRuntime(
            "npc-guard",
            SimulationTestFactory.CreateNpc("guard"),
            city,
            0f);
        NpcRuntime actor = new NpcRuntime(
            "npc-actor",
            SimulationTestFactory.CreateNpc("actor"),
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
            fixture.EventRecorder,
            null);

        justice.CreateOrIncreaseWarrant(actor, city, 50f, 3);
        Assert.That(justice.Arrest(guard, actor, city), Is.True);
        Assert.That(justice.IsArrested(actor), Is.True);

        NpcActionData escape = SimulationTestFactory.CreateAction("escape", NpcActionType.EscapePrison, NpcActionCategory.Justice);
        ScheduledDirective directive = new ScheduledDirective(
            "directive-force", 1L, ScheduledDirectiveMode.ForceOutcome, ScheduledDirectiveOperation.EscapePrison, "npc-actor", escape);

        Assert.That(directive.Mode, Is.EqualTo(ScheduledDirectiveMode.ForceOutcome));
        Assert.That(directive.IsPending, Is.True);
        actor.SetCurrentActionRuntime(new NpcActionRuntime(directive.Action));

        Assert.That(justice.ApplyEscapeSuccess(actor, escape.crimeSettings.escapeBountyPenalty), Is.True);
        Assert.That(directive.MarkSucceeded(fixture.Time.AbsoluteDay), Is.True);
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Succeeded));
        Assert.That(justice.IsArrested(actor), Is.False);
        Assert.That(actor.CurrentStatus, Does.Contain(freeStatus));
        Assert.That(actor.CurrentStatus, Does.Not.Contain(arrestedStatus));
        Assert.That(fixture.Decisions.Decisions, Is.Empty);

        Assert.That(fixture.Events.Events, Has.Count.EqualTo(2));
        NpcEscapedEvent escaped = fixture.Events.Events[1] as NpcEscapedEvent;
        Assert.That(escaped, Is.Not.Null);
        Assert.That(escaped.ActorRuntimeId, Is.EqualTo(actor.RuntimeId));
        Assert.That(escaped.AbsoluteDay, Is.EqualTo(fixture.Time.AbsoluteDay));
        Assert.That(escaped.OriginDecisionId, Is.Null);
        Assert.That(escaped.RecordSequence, Is.GreaterThan(fixture.Events.Events[0].RecordSequence));

        IReadOnlyList<NpcChronicleEntry> chronicle = fixture.Chronicle.GetChronicle(actor.RuntimeId);
        NpcChronicleEntry escapedEntry = null;
        foreach (NpcChronicleEntry entry in chronicle)
        {
            if (entry.DomainEvent == escaped)
            {
                escapedEntry = entry;
                break;
            }
        }

        Assert.That(escapedEntry, Is.Not.Null);
        Assert.That(escapedEntry.EntryType, Is.EqualTo(NpcChronicleEntryType.DomainEvent));
        SimulationInvariantValidator.ValidateDomainEvents(fixture.Events.Events);
        SimulationInvariantValidator.ValidateChronicle(chronicle);
    }
}
