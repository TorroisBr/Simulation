using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class ActorChoiceDiagnosticsTests
{
    [Test]
    public void SnapshotInvariantsRejectContradictoryReturnedOutcome()
    {
        (ActorChoiceAttemptOutcome outcome, NpcActionResultType? result)[] mismatches =
        {
            (ActorChoiceAttemptOutcome.Succeeded, NpcActionResultType.Failure),
            (ActorChoiceAttemptOutcome.Failed, NpcActionResultType.Success),
            (ActorChoiceAttemptOutcome.ReturnedNoResult, NpcActionResultType.Success)
        };
        foreach ((ActorChoiceAttemptOutcome outcome, NpcActionResultType? result) in mismatches)
        {
            ActorChoiceDisposition dispatch = CreateDisposition(1, ActorChoiceDispositionKind.DispatchStarted, 0, 0);
            ActorChoiceDisposition returned = CreateDisposition(2, ActorChoiceDispositionKind.AttemptReturned, 0, 0,
                attemptOutcome: outcome, returnedResultStatus: result);
            WorldStateInvariantReport report = WorldStateInvariantValidator.Validate(
                CreateSnapshot(ActorChoiceInputStatus.AttemptReturned, dispatch, returned));
            Assert.That(report.Errors, Has.Some.Property("Code").EqualTo("ActorChoiceLifecycleTransitionInvalid"), outcome.ToString());
        }
    }

    [Test]
    public void SnapshotInvariantsRejectStrayFieldsOnDeferredAndTerminalDispositions()
    {
        ActorChoiceDisposition strayDeferred = CreateDisposition(1, ActorChoiceDispositionKind.Deferred, 0, 0,
            deferralReason: ActorChoiceDeferralReason.Traveling,
            decisionRecordId: "stray-decision",
            attemptOutcome: ActorChoiceAttemptOutcome.Failed);
        WorldStateInvariantReport deferredReport = WorldStateInvariantValidator.Validate(
            CreateSnapshot(ActorChoiceInputStatus.Pending, strayDeferred));
        Assert.That(deferredReport.Errors, Has.Some.Property("Code").EqualTo("ActorChoiceLifecycleTransitionInvalid"));

        ActorChoiceDisposition dispatch = CreateDisposition(1, ActorChoiceDispositionKind.DispatchStarted, 0, 0);
        ActorChoiceDisposition strayTerminal = CreateDisposition(2, ActorChoiceDispositionKind.AttemptThrew, 0, 0,
            attemptOutcome: ActorChoiceAttemptOutcome.Threw,
            failure: ActorChoiceFailure.ActionUnavailable);
        WorldStateInvariantReport terminalReport = WorldStateInvariantValidator.Validate(
            CreateSnapshot(ActorChoiceInputStatus.AttemptThrew, dispatch, strayTerminal));
        Assert.That(terminalReport.Errors, Has.Some.Property("Code").EqualTo("ActorChoiceLifecycleTransitionInvalid"));
    }

    [Test]
    public void SnapshotCanonicalExportAndDiffExposeInputAndLifecycle()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        Assert.That(store.TryCapture("command-1", new PersonId("actor-1"), "sell-goods",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, 2L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
        Assert.That(store.TryDefer(input.InputId, 3L, 4, ActorChoiceDeferralReason.Traveling, out failure), Is.True);
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(3L), actorChoiceStore: store));
        Assert.That(store.TryReject(input.InputId, 4L, 1, ActorChoiceFailure.ActorUnavailable, out failure), Is.True);
        WorldStateSnapshot after = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(4L), actorChoiceStore: store));

        string canonical = WorldStateCanonicalWriter.Write(after);
        Assert.That(canonical, Does.Contain("ACTOR_CHOICE"));
        Assert.That(canonical, Does.Contain("command-1"));
        Assert.That(canonical, Does.Contain("sell-goods"));
        Assert.That(canonical, Does.Contain("Traveling"));
        Assert.That(canonical, Does.Contain("ActorUnavailable"));
        Assert.That(WorldStateSnapshotFormatter.Format(after), Does.Contain("roster=4"));
        Assert.That(WorldStateDiff.Compare(before, after).Differences, Has.Some.Property("Section").EqualTo("ActorChoice"));
        Assert.That(WorldStateInvariantValidator.Validate(after).Errors,
            Has.Some.Property("Code").EqualTo("ActorChoicePersonMissing"));
    }

    private static WorldStateSnapshot CreateSnapshot(
        ActorChoiceInputStatus status,
        params ActorChoiceDisposition[] dispositions)
    {
        Type inputType = typeof(ActorChoiceInput);
        ConstructorInfo constructor = inputType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[]
            {
                typeof(ActorChoiceInputId), typeof(string), typeof(long), typeof(PersonId), typeof(string),
                typeof(WorldCommandOrigin), typeof(WorldCommandAuthorityMode), typeof(long),
                typeof(ActorChoiceInputStatus), typeof(IEnumerable<ActorChoiceDisposition>)
            }, null);
        ActorChoiceInput input = (ActorChoiceInput)constructor.Invoke(new object[]
        {
            new ActorChoiceInputId("choice-test"), "command-test", 1L, new PersonId("person-test"),
            "sell-goods", WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, 0L,
            status, dispositions
        });
        return new WorldStateSnapshot(absoluteDay: 1L,
            actorChoices: new[] { new WorldStateActorChoiceSnapshot(input) });
    }

    private static ActorChoiceDisposition CreateDisposition(
        long ordinal,
        ActorChoiceDispositionKind kind,
        long day,
        int rosterOrdinal,
        ActorChoiceDeferralReason? deferralReason = null,
        ActorChoiceFailure? failure = null,
        ActorChoiceAttemptOutcome? attemptOutcome = null,
        NpcActionResultType? returnedResultStatus = null,
        string decisionRecordId = null)
    {
        ConstructorInfo constructor = typeof(ActorChoiceDisposition).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[]
            {
                typeof(long), typeof(ActorChoiceDispositionKind), typeof(long), typeof(int),
                typeof(ActorChoiceDeferralReason?), typeof(ActorChoiceFailure?), typeof(string),
                typeof(ActorChoiceAttemptOutcome?), typeof(NpcActionResultType?)
            }, null);
        return (ActorChoiceDisposition)constructor.Invoke(new object[]
        {
            ordinal, kind, day, rosterOrdinal, deferralReason, failure, decisionRecordId, attemptOutcome, returnedResultStatus
        });
    }
}
