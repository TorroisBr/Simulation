using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class ActorChoiceCensusTests
{
    [Test]
    public void P11AndTemporalSectionsShareOpaqueIdentityAndSeparateLiveCounts()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceP11CensusProvider p11Provider = new ActorChoiceP11CensusProvider(store);
        ActorChoiceTemporalCensusProvider temporalProvider = new ActorChoiceTemporalCensusProvider(store);

        OwnerSectionCensusWitness initialP11 = p11Provider.GetCurrentCensus();
        OwnerSectionCensusWitness initialTemporal = temporalProvider.GetCurrentCensus();
        Assert.That(initialP11.Cardinality, Is.Zero);
        Assert.That(initialTemporal.Cardinality, Is.Zero);
        Assert.That(initialP11.Revision, Is.Zero);
        Assert.That(initialTemporal.Revision, Is.Zero);
        Assert.That(initialP11.OwnerInstanceIdentity, Is.Not.SameAs(store));
        Assert.That(initialTemporal.OwnerInstanceIdentity, Is.SameAs(initialP11.OwnerInstanceIdentity));

        CaptureP11(store, "p11-one", "actor-p11");
        CaptureTemporal(store, "temporal-one", TemporalReference(1));

        OwnerSectionCensusWitness p11 = p11Provider.GetCurrentCensus();
        OwnerSectionCensusWitness temporal = temporalProvider.GetCurrentCensus();
        Assert.That(p11.SectionId, Is.EqualTo(ActorChoiceP11CensusProvider.SectionId));
        Assert.That(p11.SchemaVersion, Is.EqualTo(ActorChoiceP11CensusProvider.SchemaVersion));
        Assert.That(p11.Cardinality, Is.EqualTo(1));
        Assert.That(temporal.SectionId, Is.EqualTo(ActorChoiceTemporalCensusProvider.SectionId));
        Assert.That(temporal.SchemaVersion, Is.EqualTo(ActorChoiceTemporalCensusProvider.SchemaVersion));
        Assert.That(temporal.Cardinality, Is.EqualTo(1));
        Assert.That(p11.OwnerInstanceIdentity, Is.SameAs(initialP11.OwnerInstanceIdentity));
        Assert.That(temporal.OwnerInstanceIdentity, Is.SameAs(initialP11.OwnerInstanceIdentity));
        Assert.That(p11.Revision, Is.EqualTo(2L));
        Assert.That(temporal.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void EverySuccessfulP11CaptureAndDispositionAdvancesOneSharedRevision()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceP11CensusProvider provider = new ActorChoiceP11CensusProvider(store);
        ActorChoiceInput deferred = CaptureP11(store, "p11-defer", "actor-defer");
        AssertP11(provider, 1, 1L);

        Assert.That(store.TryDefer(deferred.InputId, 1L, 0, ActorChoiceDeferralReason.Traveling, out _), Is.True);
        AssertP11(provider, 1, 2L);
        Assert.That(store.TryReject(deferred.InputId, 2L, 0, ActorChoiceFailure.ActionUnavailable, out _), Is.True);
        AssertP11(provider, 1, 3L);

        ActorChoiceInput returned = CaptureP11(store, "p11-return", "actor-return");
        AssertP11(provider, 2, 4L);
        Assert.That(store.TryMarkDispatchStarted(returned.InputId, 1L, 1, "decision-return", out _), Is.True);
        AssertP11(provider, 2, 5L);
        Assert.That(store.TryRecordAttemptReturned(returned.InputId, 1L, 1, NpcActionResult.Succeeded(), out _), Is.True);
        AssertP11(provider, 2, 6L);

        ActorChoiceInput threw = CaptureP11(store, "p11-throw", "actor-throw");
        AssertP11(provider, 3, 7L);
        Assert.That(store.TryMarkDispatchStarted(threw.InputId, 1L, 2, "decision-throw", out _), Is.True);
        AssertP11(provider, 3, 8L);
        Assert.That(store.TryRecordAttemptThrew(threw.InputId, 1L, 2, out _), Is.True);
        AssertP11(provider, 3, 9L);

        Assert.That(store.TryDefer(deferred.InputId, 3L, 0, ActorChoiceDeferralReason.Traveling, out _), Is.False);
        AssertP11(provider, 3, 9L);
    }

    [Test]
    public void TemporalCaptureAndAllDispositionKindsAdvanceOnceWhileReplaysDoNot()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceP11CensusProvider p11Provider = new ActorChoiceP11CensusProvider(store);
        ActorChoiceTemporalCensusProvider temporalProvider = new ActorChoiceTemporalCensusProvider(store);

        TimelineInputReference firstAccepted = TemporalReference(1);
        ActorChoiceInput returned = CaptureTemporal(store, "temporal-return", firstAccepted);
        AssertSections(p11Provider, temporalProvider, 0, 1, 1L);
        Assert.That(store.TryCaptureTemporal("temporal-return", returned.PersonId, returned.ActionDefinitionId,
            returned.Origin, returned.Authority, "profile", firstAccepted, out _, out _), Is.True);
        AssertSections(p11Provider, temporalProvider, 0, 1, 1L);

        ActorChoiceTemporalBoundaryReference firstBoundary = Boundary("receipt-return", 10L);
        Assert.That(store.TryRecordTemporalDispatchStarted(returned.InputId, firstBoundary,
            "temporal-dispatch-return", "decision-return", out _), Is.True);
        AssertSections(p11Provider, temporalProvider, 0, 1, 2L);
        Assert.That(store.TryRecordTemporalDispatchStarted(returned.InputId, firstBoundary,
            "temporal-dispatch-return", "decision-return", out _), Is.True);
        AssertSections(p11Provider, temporalProvider, 0, 1, 2L);
        Assert.That(store.TryRecordTemporalAttemptReturned(returned.InputId, firstBoundary,
            "temporal-attempt-return", NpcActionResult.Succeeded(), out _), Is.True);
        AssertSections(p11Provider, temporalProvider, 0, 1, 3L);

        ActorChoiceInput rejected = CaptureTemporal(store, "temporal-reject", TemporalReference(2));
        AssertSections(p11Provider, temporalProvider, 0, 2, 4L);
        Assert.That(store.TryRecordTemporalRejected(rejected.InputId, Boundary("receipt-reject", 20L),
            "temporal-reject-op", ActorChoiceFailure.ActionUnavailable, out _), Is.True);
        AssertSections(p11Provider, temporalProvider, 0, 2, 5L);

        ActorChoiceInput threw = CaptureTemporal(store, "temporal-throw", TemporalReference(3));
        AssertSections(p11Provider, temporalProvider, 0, 3, 6L);
        ActorChoiceTemporalBoundaryReference throwBoundary = Boundary("receipt-throw", 30L);
        Assert.That(store.TryRecordTemporalDispatchStarted(threw.InputId, throwBoundary,
            "temporal-dispatch-throw", "decision-throw", out _), Is.True);
        AssertSections(p11Provider, temporalProvider, 0, 3, 7L);
        Assert.That(store.TryRecordTemporalAttemptThrew(threw.InputId, throwBoundary,
            "temporal-attempt-throw", out _), Is.True);
        AssertSections(p11Provider, temporalProvider, 0, 3, 8L);

        Assert.That(store.TryRecordTemporalRejected(rejected.InputId, Boundary("receipt-late", 21L),
            "temporal-reject-late", ActorChoiceFailure.ActionUnavailable, out _), Is.False);
        AssertSections(p11Provider, temporalProvider, 0, 3, 8L);
    }

    [Test]
    public void ClonePreservesSectionsAndRevisionButCreatesNewOwnerIdentity()
    {
        ActorChoiceStore source = new ActorChoiceStore(new PersonStore());
        CaptureP11(source, "clone-p11", "actor-clone");
        ActorChoiceInput temporal = CaptureTemporal(source, "clone-temporal", TemporalReference(10));
        Assert.That(source.TryRecordTemporalRejected(temporal.InputId, Boundary("clone-receipt", 100L),
            "clone-reject", ActorChoiceFailure.ActionUnavailable, out _), Is.True);

        ActorChoiceP11CensusProvider sourceP11 = new ActorChoiceP11CensusProvider(source);
        ActorChoiceTemporalCensusProvider sourceTemporal = new ActorChoiceTemporalCensusProvider(source);
        ActorChoiceStore clone = CloneStore(source, new PersonStore());
        ActorChoiceP11CensusProvider cloneP11 = new ActorChoiceP11CensusProvider(clone);
        ActorChoiceTemporalCensusProvider cloneTemporal = new ActorChoiceTemporalCensusProvider(clone);
        AssertSections(sourceP11, sourceTemporal, 1, 1, 3L);
        AssertSections(cloneP11, cloneTemporal, 1, 1, 3L);
        Assert.That(cloneP11.GetCurrentCensus().OwnerInstanceIdentity,
            Is.Not.SameAs(sourceP11.GetCurrentCensus().OwnerInstanceIdentity));

        CaptureP11(clone, "clone-after", "actor-after");
        AssertSections(cloneP11, cloneTemporal, 2, 1, 4L);
        AssertSections(sourceP11, sourceTemporal, 1, 1, 3L);
    }

    [Test]
    public void RevisionExhaustionRejectsCaptureAndDispositionBeforeMutation()
    {
        ActorChoiceStore captureStore = new ActorChoiceStore(new PersonStore());
        ActorChoiceP11CensusProvider captureProvider = new ActorChoiceP11CensusProvider(captureStore);
        SetRevision(captureStore, long.MaxValue);
        Assert.That(captureStore.TryCapture("exhausted", new PersonId("actor"), "sell-goods",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, 0L, out _, out ActorChoiceStoreFailureCode captureFailure), Is.False);
        Assert.That(captureFailure, Is.EqualTo(ActorChoiceStoreFailureCode.RevisionExhausted));
        AssertP11(captureProvider, 0, long.MaxValue);

        ActorChoiceStore temporalCaptureStore = new ActorChoiceStore(new PersonStore());
        ActorChoiceTemporalCensusProvider temporalCaptureProvider = new ActorChoiceTemporalCensusProvider(temporalCaptureStore);
        SetRevision(temporalCaptureStore, long.MaxValue);
        Assert.That(temporalCaptureStore.TryCaptureTemporal("temporal-capture-exhausted", new PersonId("actor-temporal"),
            "sell-goods/v1", WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, "profile",
            TemporalReference(21), out _, out ActorChoiceStoreFailureCode temporalCaptureFailure), Is.False);
        Assert.That(temporalCaptureFailure, Is.EqualTo(ActorChoiceStoreFailureCode.RevisionExhausted));
        Assert.That(temporalCaptureProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(temporalCaptureProvider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));

        ActorChoiceStore p11Store = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput p11Input = CaptureP11(p11Store, "p11-exhausted", "actor-p11");
        ActorChoiceP11CensusProvider p11Provider = new ActorChoiceP11CensusProvider(p11Store);
        SetRevision(p11Store, long.MaxValue);
        Assert.That(p11Store.TryDefer(p11Input.InputId, 1L, 0, ActorChoiceDeferralReason.Traveling,
            out ActorChoiceStoreFailureCode p11Failure), Is.False);
        Assert.That(p11Failure, Is.EqualTo(ActorChoiceStoreFailureCode.RevisionExhausted));
        AssertP11(p11Provider, 1, long.MaxValue);
        Assert.That(p11Store.TryGet(p11Input.InputId, out ActorChoiceInput p11Unchanged), Is.True);
        Assert.That(p11Unchanged.Dispositions, Is.Empty);

        ActorChoiceStore temporalStore = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput temporalInput = CaptureTemporal(temporalStore, "temporal-exhausted", TemporalReference(20));
        ActorChoiceP11CensusProvider temporalP11 = new ActorChoiceP11CensusProvider(temporalStore);
        ActorChoiceTemporalCensusProvider temporalProvider = new ActorChoiceTemporalCensusProvider(temporalStore);
        SetRevision(temporalStore, long.MaxValue);
        Assert.That(temporalStore.TryRecordTemporalRejected(temporalInput.InputId, Boundary("receipt-exhausted", 200L),
            "temporal-exhausted-op", ActorChoiceFailure.ActionUnavailable, out ActorChoiceStoreFailureCode temporalFailure), Is.False);
        Assert.That(temporalFailure, Is.EqualTo(ActorChoiceStoreFailureCode.RevisionExhausted));
        AssertSections(temporalP11, temporalProvider, 0, 1, long.MaxValue);
        Assert.That(temporalStore.TryGet(temporalInput.InputId, out ActorChoiceInput temporalUnchanged), Is.True);
        Assert.That(temporalUnchanged.TemporalDispositions, Is.Empty);
    }

    [Test]
    public void DailyProfileTracksExactActorChoiceP11OwnerAndEveryStandaloneCommit()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime();
        ActorChoiceStore store = runtime.ActorChoiceStore;
        ActorChoiceP11CensusProvider provider = new ActorChoiceP11CensusProvider(store);
        ActorChoiceTemporalCensusProvider temporalProvider = new ActorChoiceTemporalCensusProvider(store);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialFailure),
            Is.True, initialFailure.ToString());
        Assert.That(runtime.HasSameActorChoiceP11Owner(provider), Is.True);
        Assert.That(runtime.HasSameActorChoiceP11Owner(temporalProvider), Is.False,
            "the P18 temporal section is not the selected daily P11 section");
        Assert.That(runtime.HasSameActorChoiceP11Owner(
            new ActorChoiceP11CensusProvider(CloneStore(store, new PersonStore()))), Is.False,
            "a similarly populated clone is not the runtime-owned store");
        AssertRuntimeP11(runtime, provider, 0, 0L, 0L);

        ActorChoiceInput deferred = CaptureP11(store, "p12-choice-defer", "p12-choice-defer-actor");
        AssertRuntimeP11(runtime, provider, 1, 1L, 1L);
        Assert.That(store.TryCapture("p12-choice-defer", new PersonId("duplicate"), "sell-goods",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, 0L, out _,
            out ActorChoiceStoreFailureCode duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(ActorChoiceStoreFailureCode.DuplicateWorldCommandId));
        AssertRuntimeP11(runtime, provider, 1, 1L, 1L);

        Assert.That(store.TryDefer(deferred.InputId, 1L, 0, ActorChoiceDeferralReason.Traveling, out _), Is.True);
        AssertRuntimeP11(runtime, provider, 1, 2L, 2L);
        Assert.That(store.TryReject(deferred.InputId, 2L, 0, ActorChoiceFailure.ActionUnavailable, out _), Is.True);
        AssertRuntimeP11(runtime, provider, 1, 3L, 3L);
        Assert.That(store.TryDefer(deferred.InputId, 3L, 0, ActorChoiceDeferralReason.Traveling, out _), Is.False,
            "a replayed terminal transition is a no-op");
        AssertRuntimeP11(runtime, provider, 1, 3L, 3L);

        ActorChoiceInput returned = CaptureP11(store, "p12-choice-return", "p12-choice-return-actor");
        AssertRuntimeP11(runtime, provider, 2, 4L, 4L);
        Assert.That(store.TryMarkDispatchStarted(returned.InputId, 1L, 1, "p12-choice-return-decision", out _), Is.True);
        AssertRuntimeP11(runtime, provider, 2, 5L, 5L);
        Assert.That(store.TryRecordAttemptReturned(returned.InputId, 1L, 1, NpcActionResult.Succeeded(), out _), Is.True);
        AssertRuntimeP11(runtime, provider, 2, 6L, 6L);

        ActorChoiceInput threw = CaptureP11(store, "p12-choice-throw", "p12-choice-throw-actor");
        AssertRuntimeP11(runtime, provider, 3, 7L, 7L);
        Assert.That(store.TryMarkDispatchStarted(threw.InputId, 1L, 2, "p12-choice-throw-decision", out _), Is.True);
        AssertRuntimeP11(runtime, provider, 3, 8L, 8L);
        Assert.That(store.TryRecordAttemptThrew(threw.InputId, 1L, 2, out _), Is.True);
        AssertRuntimeP11(runtime, provider, 3, 9L, 9L);

        OwnerSectionCensusWitness p11 = provider.GetCurrentCensus();
        OwnerSectionCensusWitness temporal = temporalProvider.GetCurrentCensus();
        Assert.That(p11.SectionId, Is.EqualTo(ActorChoiceP11CensusProvider.SectionId));
        Assert.That(p11.SchemaVersion, Is.EqualTo(ActorChoiceP11CensusProvider.SchemaVersion));
        Assert.That(p11.Cardinality, Is.EqualTo(3));
        Assert.That(temporal.SectionId, Is.EqualTo(ActorChoiceTemporalCensusProvider.SectionId));
        Assert.That(temporal.Cardinality, Is.Zero);
        Assert.That(temporal.Revision, Is.EqualTo(9L));
        Assert.That(temporal.OwnerInstanceIdentity, Is.SameAs(p11.OwnerInstanceIdentity));
    }

    [Test]
    public void DailyProfileRejectsActorChoiceMutationWhenOwnerThreadIsWrong()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime();
        ActorChoiceStore store = runtime.ActorChoiceStore;
        bool result = true;
        ActorChoiceStoreFailureCode failure = ActorChoiceStoreFailureCode.None;
        Thread wrongThread = new Thread(() => result = store.TryCapture(
            "p12-choice-wrong-thread",
            new PersonId("p12-choice-wrong-thread-actor"),
            "sell-goods",
            WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request,
            0L,
            out _,
            out failure));

        wrongThread.Start();
        wrongThread.Join();

        Assert.That(result, Is.False);
        Assert.That(failure, Is.EqualTo(ActorChoiceStoreFailureCode.RuntimeFaulted));
        Assert.That(store.Count, Is.Zero);
        Assert.That(new ActorChoiceP11CensusProvider(store).GetCurrentCensus().Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.False);
        Assert.That(censusFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailyProfileRejectsActorChoiceCommitAtEpochCapacityBeforeStoreMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime();
        SetProtocolMutationEpoch(runtime, long.MaxValue);

        Assert.That(runtime.ActorChoiceStore.TryCapture("p12-choice-epoch-full",
            new PersonId("p12-choice-epoch-full-actor"), "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, 0L, out _, out ActorChoiceStoreFailureCode failure), Is.False);
        Assert.That(failure, Is.EqualTo(ActorChoiceStoreFailureCode.RuntimeFaulted));
        Assert.That(runtime.ActorChoiceStore.Count, Is.Zero);
        Assert.That(new ActorChoiceP11CensusProvider(runtime.ActorChoiceStore).GetCurrentCensus().Revision, Is.Zero);
    }

    [Test]
    public void DailyProfileRejectsActorChoiceCommitAgainstStaleP11BaselineBeforeMutation()
    {
        SimulationRuntime runtime = CreateDailyProfileRuntime();
        ActorChoiceStore store = runtime.ActorChoiceStore;
        FieldInfo admissionField = typeof(ActorChoiceStore).GetField(
            "p12MutationAdmission", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo committedField = typeof(ActorChoiceStore).GetField(
            "p12MutationCommitted", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(admissionField, Is.Not.Null);
        Assert.That(committedField, Is.Not.Null);
        Func<bool> admission = (Func<bool>)admissionField.GetValue(store);
        Action committed = (Action)committedField.GetValue(store);

        admissionField.SetValue(store, null);
        committedField.SetValue(store, null);
        Assert.That(store.TryCapture("p12-choice-untracked",
            new PersonId("p12-choice-untracked-actor"), "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, 0L, out _, out _), Is.True);
        admissionField.SetValue(store, admission);
        committedField.SetValue(store, committed);

        Assert.That(store.TryCapture("p12-choice-after-stale-baseline",
            new PersonId("p12-choice-after-stale-baseline-actor"), "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, 0L, out _, out ActorChoiceStoreFailureCode failure), Is.False);
        Assert.That(failure, Is.EqualTo(ActorChoiceStoreFailureCode.RuntimeFaulted));
        Assert.That(store.Count, Is.EqualTo(1));
        Assert.That(new ActorChoiceP11CensusProvider(store).GetCurrentCensus().Revision, Is.EqualTo(1L));
    }

    private static void AssertP11(ActorChoiceP11CensusProvider provider, int count, long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.Cardinality, Is.EqualTo(count));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private static void AssertSections(ActorChoiceP11CensusProvider p11Provider,
        ActorChoiceTemporalCensusProvider temporalProvider, int p11Count, int temporalCount, long revision)
    {
        OwnerSectionCensusWitness p11 = p11Provider.GetCurrentCensus();
        OwnerSectionCensusWitness temporal = temporalProvider.GetCurrentCensus();
        Assert.That(p11.Cardinality, Is.EqualTo(p11Count));
        Assert.That(temporal.Cardinality, Is.EqualTo(temporalCount));
        Assert.That(p11.Revision, Is.EqualTo(revision));
        Assert.That(temporal.Revision, Is.EqualTo(revision));
        Assert.That(temporal.OwnerInstanceIdentity, Is.SameAs(p11.OwnerInstanceIdentity));
    }

    private static ActorChoiceInput CaptureP11(ActorChoiceStore store, string commandId, string personId)
    {
        Assert.That(store.TryCapture(commandId, new PersonId(personId), "sell-goods", WorldCommandOrigin.System,
            WorldCommandAuthorityMode.Request, 0L, out ActorChoiceInput input, out ActorChoiceStoreFailureCode failure),
            Is.True, failure.ToString());
        return input;
    }

    private static ActorChoiceInput CaptureTemporal(ActorChoiceStore store, string commandId, TimelineInputReference accepted)
    {
        Assert.That(store.TryCaptureTemporal(commandId, new PersonId(commandId), "sell-goods/v1",
            WorldCommandOrigin.System, WorldCommandAuthorityMode.Request, "profile", accepted,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
        return input;
    }

    private static TimelineInputReference TemporalReference(int sequence)
    {
        return new TimelineInputReference(sequence, "timeline-input-" + sequence, "actor-choice", "payload-" + sequence,
            new LogicalTick(sequence * 10L));
    }

    private static ActorChoiceTemporalBoundaryReference Boundary(string receiptId, long tick)
    {
        return new ActorChoiceTemporalBoundaryReference("profile", new LogicalTick(tick), receiptId, 1L);
    }

    private static ActorChoiceStore CloneStore(ActorChoiceStore source, PersonStore targetPersonStore)
    {
        MethodInfo clone = typeof(ActorChoiceStore).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(clone, Is.Not.Null);
        return (ActorChoiceStore)clone.Invoke(source, new object[] { targetPersonStore, null });
    }

    private static void SetRevision(ActorChoiceStore store, long revision)
    {
        FieldInfo field = typeof(ActorChoiceStore).GetField("censusRevision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(store, revision);
    }

    private static SimulationRuntime CreateDailyProfileRuntime()
    {
        return new SimulationRuntime(
            new SimulationTime(),
            null,
            null,
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static void AssertRuntimeP11(
        SimulationRuntime runtime,
        ActorChoiceP11CensusProvider provider,
        int count,
        long revision,
        long epoch)
    {
        AssertP11(provider, count, revision);
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long actualEpoch, out ContinuationCensusFailure failure), Is.True, failure.ToString());
        Assert.That(actualEpoch, Is.EqualTo(epoch));
        Assert.That(runtime.TryAssessNpcRosterCensus(out failure), Is.True, failure.ToString());
    }

    private static void SetProtocolMutationEpoch(SimulationRuntime runtime, long epoch)
    {
        FieldInfo protocolField = typeof(SimulationRuntime).GetField(
            "npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(protocolField, Is.Not.Null);
        object protocol = protocolField.GetValue(runtime);
        FieldInfo epochField = typeof(ContinuationCensusProtocol).GetField(
            "mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(epochField, Is.Not.Null);
        epochField.SetValue(protocol, epoch);
    }
}
