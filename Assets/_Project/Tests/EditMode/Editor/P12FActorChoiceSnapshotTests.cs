using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class P12FActorChoiceSnapshotTests
{
    [Test]
    public void EmptyOwnerCapturesAndStagesWithExactRevisionAndSequence()
    {
        ActorChoiceStore source = new ActorChoiceStore(new PersonStore());
        DailyCaptureEligibilityToken token = CreateToken(source, out IReadOnlyList<OwnerSectionCensusSnapshot> vector);

        Assert.That(P12FActorChoiceSnapshot.TryCapture(source, token, vector,
            out P12FActorChoiceSnapshot snapshot, out P12FActorChoiceSnapshotFailure failure), Is.True, failure.ToString());
        PersonStore stagedPeople = new PersonStore();
        Assert.That(P12FActorChoiceSnapshot.TryStage(snapshot, stagedPeople,
            out ActorChoiceStore staged, out failure), Is.True, failure.ToString());
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.NextInputSequence, Is.EqualTo(1L));
        Assert.That(staged.CensusRevision, Is.Zero);
        Assert.That(staged.PersonStore, Is.SameAs(stagedPeople));
    }

    [Test]
    public void PopulatedTerminalHistoryIsDetachedAndDuplicateCommandRemainsIdempotentlyRejected()
    {
        ActorChoiceStore source = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput returned = Capture(source, "command-1", "person-1");
        Assert.That(source.TryMarkDispatchStarted(returned.InputId, 1L, 0, "decision-1", out _), Is.True);
        Assert.That(source.TryRecordAttemptReturned(returned.InputId, 1L, 0, NpcActionResult.Succeeded(), out _), Is.True);
        ActorChoiceInput rejected = Capture(source, "command-2", "person-1");
        Assert.That(source.TryReject(rejected.InputId, 2L, 0, ActorChoiceFailure.ActionUnavailable, out _), Is.True);
        ActorChoiceInput threw = Capture(source, "command-3", "person-1");
        Assert.That(source.TryMarkDispatchStarted(threw.InputId, 3L, 0, null, out _), Is.True);
        Assert.That(source.TryRecordAttemptThrew(threw.InputId, 3L, 0, out _), Is.True);
        DailyCaptureEligibilityToken token = CreateToken(source, out IReadOnlyList<OwnerSectionCensusSnapshot> vector);

        Assert.That(P12FActorChoiceSnapshot.TryCapture(source, token, vector,
            out P12FActorChoiceSnapshot snapshot, out P12FActorChoiceSnapshotFailure failure), Is.True, failure.ToString());
        PersonStore stagedPeople = PersonWithId("person-1");
        Assert.That(P12FActorChoiceSnapshot.TryStage(snapshot, stagedPeople,
            out ActorChoiceStore staged, out failure), Is.True, failure.ToString());
        Assert.That(staged.Inputs.Count, Is.EqualTo(3));
        Assert.That(staged.Inputs[0].Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(staged.Inputs[0].Dispositions.Count, Is.EqualTo(2));
        Assert.That(staged.Inputs[1].Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(staged.Inputs[2].Status, Is.EqualTo(ActorChoiceInputStatus.AttemptThrew));
        Assert.That(staged.Inputs[2].Dispositions[0].DecisionRecordId, Is.Null);
        Assert.That(staged.NextInputSequence, Is.EqualTo(source.NextInputSequence));
        Assert.That(staged.CensusRevision, Is.EqualTo(source.CensusRevision));
        Assert.That(staged.PersonStore, Is.SameAs(stagedPeople));
        Assert.That(ReferenceEquals(staged.Inputs[0], source.Inputs[0]), Is.False);
        Assert.That(staged.TryCapture("command-1", new PersonId("person-1"), "sell-goods",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, 3L,
            out _, out ActorChoiceStoreFailureCode captureFailure), Is.False);
        Assert.That(captureFailure, Is.EqualTo(ActorChoiceStoreFailureCode.DuplicateWorldCommandId));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RejectsPendingAndConsumedAwaitingTerminalHistory(bool dispatched)
    {
        ActorChoiceStore source = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput input = Capture(source, "command", "person");
        if (dispatched)
            Assert.That(source.TryMarkDispatchStarted(input.InputId, 0L, 0, "decision", out _), Is.True);
        DailyCaptureEligibilityToken token = CreateToken(source, out IReadOnlyList<OwnerSectionCensusSnapshot> vector);

        Assert.That(P12FActorChoiceSnapshot.TryCapture(source, token, vector,
            out P12FActorChoiceSnapshot snapshot, out P12FActorChoiceSnapshotFailure failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FActorChoiceSnapshotFailure.UnsupportedHistory));
    }

    [Test]
    public void RejectsDeferredAndP18TemporalHistory()
    {
        ActorChoiceStore deferred = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput deferredInput = Capture(deferred, "deferred", "person");
        Assert.That(deferred.TryDefer(deferredInput.InputId, 0L, 0, ActorChoiceDeferralReason.Traveling, out _), Is.True);
        DailyCaptureEligibilityToken deferredToken = CreateToken(deferred, out IReadOnlyList<OwnerSectionCensusSnapshot> deferredVector);
        Assert.That(P12FActorChoiceSnapshot.TryCapture(deferred, deferredToken, deferredVector, out _, out _), Is.False);

        ActorChoiceStore temporal = new ActorChoiceStore(new PersonStore());
        Assert.That(temporal.TryCaptureTemporal("temporal-command", new PersonId("person"), "sell-goods",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, "profile",
            new TimelineInputReference(1, "timeline-1", "actor-choice", "payload", new LogicalTick(5L)),
            out _, out _), Is.True);
        DailyCaptureEligibilityToken temporalToken = CreateToken(temporal, out IReadOnlyList<OwnerSectionCensusSnapshot> temporalVector);
        Assert.That(P12FActorChoiceSnapshot.TryCapture(temporal, temporalToken, temporalVector,
            out P12FActorChoiceSnapshot temporalSnapshot, out P12FActorChoiceSnapshotFailure temporalFailure), Is.False);
        Assert.That(temporalSnapshot, Is.Null);
        Assert.That(temporalFailure, Is.EqualTo(P12FActorChoiceSnapshotFailure.UnsupportedHistory));
    }

    [Test]
    public void RequiresExactTokenVectorAndMatchingOwnerCardinalityRevisionAndIdentity()
    {
        ActorChoiceStore source = new ActorChoiceStore(new PersonStore());
        DailyCaptureEligibilityToken token = CreateToken(source, out IReadOnlyList<OwnerSectionCensusSnapshot> vector);
        List<OwnerSectionCensusSnapshot> differentVector = new List<OwnerSectionCensusSnapshot>(vector);
        Assert.That(P12FActorChoiceSnapshot.TryCapture(source, token, differentVector, out _, out _), Is.False);

        OwnerSectionCensusSnapshot original = vector[0];
        List<OwnerSectionCensusSnapshot> wrongRevision = new List<OwnerSectionCensusSnapshot>
        {
            new OwnerSectionCensusSnapshot(original.SectionId, original.SchemaVersion, original.Role,
                original.OwnerInstanceIdentity, original.Cardinality, original.Revision + 1L)
        };
        DailyCaptureEligibilityToken wrongToken = CreateToken(source, wrongRevision);
        Assert.That(P12FActorChoiceSnapshot.TryCapture(source, wrongToken, wrongRevision, out _, out _), Is.False);

        List<OwnerSectionCensusSnapshot> wrongIdentity = new List<OwnerSectionCensusSnapshot>
        {
            new OwnerSectionCensusSnapshot(original.SectionId, original.SchemaVersion, original.Role,
                new object(), original.Cardinality, original.Revision)
        };
        DailyCaptureEligibilityToken identityToken = CreateToken(source, wrongIdentity);
        Assert.That(P12FActorChoiceSnapshot.TryCapture(source, identityToken, wrongIdentity, out _, out _), Is.False);
    }

    [Test]
    public void StagingRequiresPersonBindingInTheExactTargetStore()
    {
        ActorChoiceStore source = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput input = Capture(source, "command", "person");
        Assert.That(source.TryReject(input.InputId, 0L, 0,
            ActorChoiceFailure.ActionUnavailable, out ActorChoiceStoreFailureCode rejectionFailure),
            Is.True, rejectionFailure.ToString());
        DailyCaptureEligibilityToken token = CreateToken(source, out IReadOnlyList<OwnerSectionCensusSnapshot> vector);
        Assert.That(P12FActorChoiceSnapshot.TryCapture(source, token, vector,
            out P12FActorChoiceSnapshot snapshot, out P12FActorChoiceSnapshotFailure failure), Is.True, failure.ToString());
        Assert.That(P12FActorChoiceSnapshot.TryStage(snapshot, new PersonStore(), out ActorChoiceStore staged, out failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FActorChoiceSnapshotFailure.PersonBindingMismatch));
    }

    private static ActorChoiceInput Capture(ActorChoiceStore store, string command, string person)
    {
        Assert.That(store.TryCapture(command, new PersonId(person), "sell-goods",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, 0L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
        return input;
    }

    private static PersonStore PersonWithId(string id)
    {
        PersonStore store = new PersonStore();
        Assert.That(store.TryRegister(new PersonRuntime(new PersonId(id)), out PersonStoreFailure failure), Is.True, failure.ToString());
        return store;
    }

    private static DailyCaptureEligibilityToken CreateToken(
        ActorChoiceStore source,
        out IReadOnlyList<OwnerSectionCensusSnapshot> vector)
    {
        OwnerSectionCensusWitness witness = new ActorChoiceP11CensusProvider(source).GetCurrentCensus();
        List<OwnerSectionCensusSnapshot> sections = new List<OwnerSectionCensusSnapshot>
        {
            new OwnerSectionCensusSnapshot(witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision)
        };
        vector = sections;
        return CreateToken(source, vector);
    }

    private static DailyCaptureEligibilityToken CreateToken(
        ActorChoiceStore source,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections)
    {
        return new DailyCaptureEligibilityToken(new object(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 0L, 1L, 0L, sections);
    }
}
