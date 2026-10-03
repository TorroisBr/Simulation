using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ScheduledDirectiveCensusTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ProviderReportsExactOwnerAndSuccessfulAddsAndTerminalTransitions()
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirectiveCensusProvider provider = new ScheduledDirectiveCensusProvider(store);
        OwnerSectionCensusWitness initial = provider.GetCurrentCensus();

        Assert.That(initial.SectionId, Is.EqualTo("p12f.scheduled-directives"));
        Assert.That(initial.SchemaVersion, Is.EqualTo(1));
        Assert.That(initial.OwnerInstanceIdentity, Is.SameAs(store));
        Assert.That(initial.Cardinality, Is.Zero);
        Assert.That(initial.Revision, Is.Zero);
        Assert.That(
            typeof(ScheduledDirective).GetField("ownerStore", BindingFlags.Instance | BindingFlags.NonPublic).IsNotSerialized,
            Is.True,
            "The owner callback is runtime-only and must not become serialized delegate state.");

        ScheduledDirective succeeded = CreateDirective("succeeded", 1L);
        Assert.That(store.Add(succeeded), Is.True);
        AssertCensus(provider, store, 1, 1L);
        Assert.That(succeeded.MarkSucceeded(2L), Is.True);
        AssertCensus(provider, store, 1, 2L);
        Assert.That(succeeded.ProcessedDay, Is.EqualTo(2L));
        Assert.That(succeeded.MarkSucceeded(3L), Is.False);
        AssertCensus(provider, store, 1, 2L);

        ScheduledDirective failed = CreateDirective("failed", 1L);
        ScheduledDirective skipped = CreateDirective("skipped", 1L);
        Assert.That(store.Add(failed), Is.True);
        Assert.That(store.Add(skipped), Is.True);
        Assert.That(failed.MarkFailed(4L, "failure"), Is.True);
        Assert.That(skipped.MarkSkipped(5L, "skip"), Is.True);
        AssertCensus(provider, store, 3, 6L);
    }

    [Test]
    public void AddWithInitialSkipIsOneCommitAndPrepareSkipIsOneOwnerTransition()
    {
        SimulationTime time = new SimulationTime();
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(time);
        ScheduledDirectiveCensusProvider provider = new ScheduledDirectiveCensusProvider(store);

        ScheduledDirective zeroDay = CreateDirective("zero", 0L);
        LogAssert.Expect(LogType.Warning, "Scheduled directive 'zero' was skipped: AbsoluteDay must be at least 1.");
        Assert.That(store.Add(zeroDay), Is.True);
        Assert.That(zeroDay.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        AssertCensus(provider, store, 1, 1L);

        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        ScheduledDirective unresolved = CreateDirective("unresolved", 1L);
        Assert.That(store.Add(unresolved), Is.True);
        AssertCensus(provider, store, 2, 2L);
        LogAssert.Expect(LogType.Warning, "Scheduled directive 'unresolved' was skipped: Actor RuntimeId 'npc-actor' could not be resolved.");
        new ScheduledDirectiveSystem(store, registry).PrepareDay(1L);
        Assert.That(unresolved.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        AssertCensus(provider, store, 2, 3L);
        new ScheduledDirectiveSystem(store, registry).PrepareDay(1L);
        AssertCensus(provider, store, 2, 3L);
    }

    [Test]
    public void TakingTransientPreparedLookupDoesNotChangeOwnerCensus()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        NpcRuntime actor = new NpcRuntime("npc-actor", SimulationTestFactory.CreateNpc("actor"));
        Assert.That(registry.RegisterNpc(actor), Is.True);
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirectiveCensusProvider provider = new ScheduledDirectiveCensusProvider(store);
        Assert.That(store.Add(CreateDirective("pending", 1L, actor.RuntimeId)), Is.True);
        ScheduledDirectiveSystem system = new ScheduledDirectiveSystem(store, registry);

        system.PrepareDay(1L);
        AssertCensus(provider, store, 1, 1L);
        Assert.That(system.TryTakeDirective(actor, out ScheduledDirective taken), Is.True);
        Assert.That(taken.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(system.TryTakeDirective(actor, out _), Is.False);
        AssertCensus(provider, store, 1, 1L);
    }

    [Test]
    public void SaturatedRevisionRejectsAddAndTerminalTransitionWithoutEffects()
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirectiveCensusProvider provider = new ScheduledDirectiveCensusProvider(store);
        SetRevision(store, long.MaxValue);
        ScheduledDirective rejected = CreateDirective("rejected", 1L);
        Assert.That(store.Add(rejected), Is.False);
        Assert.That(store.Directives, Is.Empty);
        Assert.That(rejected.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        AssertCensus(provider, store, 0, long.MaxValue);

        SetRevision(store, 0L);
        ScheduledDirective accepted = CreateDirective("accepted", 1L);
        Assert.That(store.Add(accepted), Is.True);
        SetRevision(store, long.MaxValue);
        Assert.That(accepted.MarkSkipped(1L, "saturated"), Is.False);
        Assert.That(accepted.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(accepted.ProcessedDay, Is.EqualTo(-1L));
        AssertCensus(provider, store, 1, long.MaxValue);
    }

    [Test]
    public void DuplicateAddAndInvalidTransitionDoNotAdvanceRevision()
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirectiveCensusProvider provider = new ScheduledDirectiveCensusProvider(store);
        Assert.That(store.Add(CreateDirective("duplicate", 1L)), Is.True);
        long revision = store.Revision;
        LogAssert.Expect(LogType.Error, "Cannot add duplicate DirectiveId 'duplicate'.");
        Assert.That(store.Add(CreateDirective("duplicate", 1L)), Is.False);
        Assert.That(store.Directives[0].MarkFailed(-1L, "invalid"), Is.False);
        AssertCensus(provider, store, 1, revision);

        ScheduledDirectiveStore otherStore = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirectiveCensusProvider otherProvider = new ScheduledDirectiveCensusProvider(otherStore);
        LogAssert.Expect(LogType.Error, "Cannot add scheduled directive: directive is owned by another runtime.");
        Assert.That(otherStore.Add(store.Directives[0]), Is.False);
        AssertCensus(otherProvider, otherStore, 0, 0L);
    }

    [Test]
    public void SelectedProfileTracksGenesisBaselineAndSuccessfulScheduledDirectiveCommits()
    {
        ScheduledDirective genesis = CreateDirective("p12-genesis", 1L);
        P12DirectiveFixture fixture = CreateP12DirectiveRuntime(
            initializeStore: store => Assert.That(store.Add(genesis), Is.True));
        ScheduledDirectiveCensusProvider provider =
            ReadPrivateField<ScheduledDirectiveCensusProvider>(fixture.Runtime, "scheduledDirectiveCensusProvider");

        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure initialFailure),
            Is.True, initialFailure.ToString());
        AssertCensus(provider, fixture.Store, 1, 1L);
        AssertMutationEpoch(fixture.Runtime, 0L,
            "authored genesis is represented by the initial baseline without a synthetic write notification");

        MethodInfo sameOwnerMethod = typeof(SimulationRuntime).GetMethod(
            "HasSameScheduledDirectiveOwner",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(sameOwnerMethod, Is.Not.Null);
        Assert.That(sameOwnerMethod.Invoke(fixture.Runtime, new object[] { provider }), Is.EqualTo(true));
        Assert.That(sameOwnerMethod.Invoke(
            fixture.Runtime,
            new object[] { new ScheduledDirectiveCensusProvider(new ScheduledDirectiveStore(fixture.Time)) }),
            Is.EqualTo(false),
            "bootstrap publication must reject a different ScheduledDirectiveStore");

        ScheduledDirective added = CreateDirective("p12-added", 1L);
        Assert.That(fixture.Store.Add(added), Is.True);
        AssertCensus(provider, fixture.Store, 2, 2L);
        AssertMutationEpoch(fixture.Runtime, 1L, "a post-baseline Add reports one committed owner revision");

        Assert.That(added.MarkFailed(0L, "failed in the existing action flow"), Is.True);
        AssertCensus(provider, fixture.Store, 2, 3L);
        AssertMutationEpoch(fixture.Runtime, 2L, "a terminal transition reports one committed owner revision");

        ScheduledDirective immediateSkip = CreateDirective("p12-immediate-skip", 0L);
        LogAssert.Expect(
            LogType.Warning,
            "Scheduled directive 'p12-immediate-skip' was skipped: AbsoluteDay must be at least 1.");
        Assert.That(fixture.Store.Add(immediateSkip), Is.True);
        Assert.That(immediateSkip.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        AssertCensus(provider, fixture.Store, 3, 4L);
        AssertMutationEpoch(fixture.Runtime, 3L,
            "Add with an immediate terminal status remains one owner commit and one epoch notification");

        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure finalFailure),
            Is.True, finalFailure.ToString());
    }

    [Test]
    public void SelectedProfileStandaloneMayOmitOptionalScheduledDirectiveOwnerButCannotPublishAnotherStore()
    {
        SimulationTime time = new SimulationTime();
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            null,
            null,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
        Assert.That(
            ReadPrivateField<ScheduledDirectiveCensusProvider>(runtime, "scheduledDirectiveCensusProvider"),
            Is.Null,
            "a standalone runtime that omits the optional system makes no ScheduledDirective section claim");

        MethodInfo sameOwnerMethod = typeof(SimulationRuntime).GetMethod(
            "HasSameScheduledDirectiveOwner",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(sameOwnerMethod, Is.Not.Null);
        ScheduledDirectiveCensusProvider publishedProvider =
            new ScheduledDirectiveCensusProvider(new ScheduledDirectiveStore(time));
        Assert.That(sameOwnerMethod.Invoke(runtime, new object[] { publishedProvider }), Is.EqualTo(false),
            "the selected bootstrap ownership predicate rejects publication without the runtime's exact owner");
    }

    [Test]
    public void SelectedProfileSuccessfulSucceededAndSkippedTransitionsRefreshOwnerAndEpoch()
    {
        P12DirectiveFixture fixture = CreateP12DirectiveRuntime();
        ScheduledDirectiveCensusProvider provider =
            ReadPrivateField<ScheduledDirectiveCensusProvider>(fixture.Runtime, "scheduledDirectiveCensusProvider");

        ScheduledDirective succeeded = CreateDirective("p12-succeeded", 1L);
        Assert.That(fixture.Store.Add(succeeded), Is.True);
        AssertCensus(provider, fixture.Store, 1, 1L);
        AssertMutationEpoch(fixture.Runtime, 1L, "the successful Add is one committed owner mutation");
        Assert.That(succeeded.MarkSucceeded(1L), Is.True);
        AssertCensus(provider, fixture.Store, 1, 2L);
        AssertMutationEpoch(fixture.Runtime, 2L, "MarkSucceeded advances one owner revision and epoch");

        ScheduledDirective skipped = CreateDirective("p12-skipped", 1L);
        Assert.That(fixture.Store.Add(skipped), Is.True);
        AssertCensus(provider, fixture.Store, 2, 3L);
        AssertMutationEpoch(fixture.Runtime, 3L, "the second successful Add advances once");
        Assert.That(skipped.MarkSkipped(1L, "selected P12 skip"), Is.True);
        AssertCensus(provider, fixture.Store, 2, 4L);
        AssertMutationEpoch(fixture.Runtime, 4L, "MarkSkipped advances one owner revision and epoch");
    }

    [Test]
    public void SelectedProfilePrepareDayDuplicateAndUnresolvedSkipsRefreshOwnerAndEpoch()
    {
        NpcRuntime actor = new NpcRuntime(
            "p12-prepared-actor",
            SimulationTestFactory.CreateNpc("p12-prepared-actor"));
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(actor), Is.True);
        P12DirectiveFixture fixture = CreateP12DirectiveRuntime(
            actors: new[] { actor },
            identityRegistry: registry,
            initializeStore: store =>
            {
                Assert.That(store.Add(CreateDirective("p12-duplicate-a", 1L, actor.RuntimeId)), Is.True);
                Assert.That(store.Add(CreateDirective("p12-duplicate-b", 1L, actor.RuntimeId)), Is.True);
                Assert.That(store.Add(CreateDirective("p12-unresolved", 1L, "missing-p12-actor")), Is.True);
            });
        ScheduledDirectiveCensusProvider provider =
            ReadPrivateField<ScheduledDirectiveCensusProvider>(fixture.Runtime, "scheduledDirectiveCensusProvider");

        AssertCensus(provider, fixture.Store, 3, 3L);
        AssertMutationEpoch(fixture.Runtime, 0L, "pre-runtime authored directives are part of the initial baseline");
        LogAssert.Expect(
            LogType.Error,
            "Scheduled directive conflict: 'p12-duplicate-a' was skipped. Multiple significant directives target actor 'p12-prepared-actor' on day 1.");
        LogAssert.Expect(
            LogType.Error,
            "Scheduled directive conflict: 'p12-duplicate-b' was skipped. Multiple significant directives target actor 'p12-prepared-actor' on day 1.");
        LogAssert.Expect(
            LogType.Warning,
            "Scheduled directive 'p12-unresolved' was skipped: Actor RuntimeId 'missing-p12-actor' could not be resolved.");

        fixture.System.PrepareDay(1L);

        foreach (ScheduledDirective directive in fixture.Store.Directives)
            Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Skipped));
        AssertCensus(provider, fixture.Store, 3, 6L);
        AssertMutationEpoch(fixture.Runtime, 3L,
            "each successful conflict or unresolved-actor terminal transition refreshes the shared epoch");
        fixture.System.PrepareDay(1L);
        AssertCensus(provider, fixture.Store, 3, 6L);
        AssertMutationEpoch(fixture.Runtime, 3L, "repeated preparation with no transition is non-mutating");
    }

    [Test]
    public void SelectedProfileTryTakeRemainsTransientForTheRegisteredOwner()
    {
        NpcRuntime actor = new NpcRuntime(
            "p12-transient-actor",
            SimulationTestFactory.CreateNpc("p12-transient-actor"));
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(actor), Is.True);
        P12DirectiveFixture fixture = CreateP12DirectiveRuntime(
            actors: new[] { actor },
            identityRegistry: registry,
            initializeStore: store => Assert.That(
                store.Add(CreateDirective("p12-transient", 1L, actor.RuntimeId)), Is.True));
        ScheduledDirectiveCensusProvider provider =
            ReadPrivateField<ScheduledDirectiveCensusProvider>(fixture.Runtime, "scheduledDirectiveCensusProvider");

        fixture.System.PrepareDay(1L);
        OwnerSectionCensusWitness beforeTake = provider.GetCurrentCensus();
        long epochBeforeTake = ReadMutationEpoch(fixture.Runtime);
        Assert.That(fixture.System.TryTakeDirective(actor, out ScheduledDirective taken), Is.True);
        Assert.That(taken.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(fixture.System.TryTakeDirective(actor, out _), Is.False);
        AssertCensus(provider, fixture.Store, beforeTake.Cardinality, beforeTake.Revision);
        Assert.That(ReadMutationEpoch(fixture.Runtime), Is.EqualTo(epochBeforeTake));
    }

    [Test]
    public void SelectedProfileAdmissionDenialLeavesIncomingDirectiveUnboundAndStoreUnchanged()
    {
        P12DirectiveFixture staleFixture = CreateP12DirectiveRuntime();
        ScheduledDirective staleRejected = CreateDirective("p12-stale-rejected", 1L);
        SetRevision(staleFixture.Store, 1L);

        Assert.Throws<InvalidOperationException>(() => staleFixture.Store.Add(staleRejected));
        Assert.That(staleFixture.Store.Directives, Is.Empty);
        Assert.That(staleFixture.Store.Revision, Is.EqualTo(1L), "the injected pre-existing drift is not overwritten");
        AssertDirectiveRemainsUnbound(staleRejected);
        Assert.That(ReadMutationEpoch(staleFixture.Runtime), Is.Zero);

        P12DirectiveFixture saturatedFixture = CreateP12DirectiveRuntime();
        ScheduledDirective saturatedRejected = CreateDirective("p12-epoch-rejected", 1L);
        SetMutationEpoch(saturatedFixture.Runtime, long.MaxValue);

        Assert.Throws<InvalidOperationException>(() => saturatedFixture.Store.Add(saturatedRejected));
        Assert.That(saturatedFixture.Store.Directives, Is.Empty);
        Assert.That(saturatedFixture.Store.Revision, Is.Zero);
        AssertDirectiveRemainsUnbound(saturatedRejected);
        Assert.That(ReadMutationEpoch(saturatedFixture.Runtime), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void SelectedProfilePrepareDayConflictsAndUnresolvedActorsPropagateAdmissionFailure()
    {
        P12DirectiveFixture conflictFixture = CreateP12DirectiveRuntime(
            initializeStore: store =>
            {
                Assert.That(store.Add(CreateDirective("p12-conflict-a", 1L, "missing-actor")), Is.True);
                Assert.That(store.Add(CreateDirective("p12-conflict-b", 1L, "missing-actor")), Is.True);
            });
        ScheduledDirective conflictA = conflictFixture.Store.Directives[0];
        ScheduledDirective conflictB = conflictFixture.Store.Directives[1];
        SetMutationEpoch(conflictFixture.Runtime, long.MaxValue);

        Assert.Throws<InvalidOperationException>(() => conflictFixture.Runtime.TryAdvanceDay(out _));
        Assert.That(conflictA.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(conflictB.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(conflictFixture.Store.Revision, Is.EqualTo(2L));
        Assert.That(conflictFixture.Store.Directives, Has.Count.EqualTo(2));
        Assert.That(ReadMutationEpoch(conflictFixture.Runtime), Is.EqualTo(long.MaxValue));

        P12DirectiveFixture unresolvedFixture = CreateP12DirectiveRuntime(
            initializeStore: store => Assert.That(
                store.Add(CreateDirective("p12-unresolved", 1L, "missing-actor")), Is.True));
        ScheduledDirective unresolved = unresolvedFixture.Store.Directives[0];
        SetMutationEpoch(unresolvedFixture.Runtime, long.MaxValue);

        Assert.Throws<InvalidOperationException>(() => unresolvedFixture.Runtime.TryAdvanceDay(out _));
        Assert.That(unresolved.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(unresolvedFixture.Store.Revision, Is.EqualTo(1L));
        Assert.That(unresolvedFixture.Store.Directives, Has.Count.EqualTo(1));
        Assert.That(ReadMutationEpoch(unresolvedFixture.Runtime), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void SelectedProfileActorTurnTerminalDenialThrowsAfterTransientDirectiveTake()
    {
        NpcRuntime actor = new NpcRuntime("p12-scheduled-actor", SimulationTestFactory.CreateNpc("p12-scheduled-actor"));
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(actor), Is.True);
        P12DirectiveFixture fixture = CreateP12DirectiveRuntime(
            actors: new[] { actor },
            identityRegistry: registry,
            initializeStore: store => Assert.That(
                store.Add(CreateDirective(
                    "p12-actor-turn",
                    1L,
                    actor.RuntimeId,
                    ScheduledDirectiveMode.ForceOutcome)), Is.True));
        ScheduledDirective directive = fixture.Store.Directives[0];
        SetMutationEpoch(fixture.Runtime, long.MaxValue);

        Assert.Throws<InvalidOperationException>(() => fixture.Runtime.TryAdvanceDay(out _));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(fixture.Store.Revision, Is.EqualTo(1L));
        Assert.That(fixture.Store.Directives, Has.Count.EqualTo(1));
        Assert.That(ReadMutationEpoch(fixture.Runtime), Is.EqualTo(long.MaxValue));
        Assert.That(fixture.System.TryTakeDirective(actor, out _), Is.False,
            "the actor turn consumed the transient lookup before its terminal store commit was denied");
    }

    [Test]
    public void SelectedProfilePostCommitNotificationFailureFaultsAfterKeepingCommittedRow()
    {
        P12DirectiveFixture fixture = CreateP12DirectiveRuntime();
        object protocol = ReadPrivateField<object>(fixture.Runtime, "npcRosterCensusProtocol");
        FieldInfo callbackField = typeof(ScheduledDirectiveStore).GetField(
            "p12MutationCommitted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(callbackField, Is.Not.Null);
        Action original = (Action)callbackField.GetValue(fixture.Store);
        Assert.That(original, Is.Not.Null);
        callbackField.SetValue(fixture.Store, new Action(() =>
        {
            SetPrivateField(protocol, "mutationEpoch", long.MaxValue);
            original();
        }));

        ScheduledDirective directive = CreateDirective("p12-post-commit-failure", 1L);
        Assert.Throws<InvalidOperationException>(() => fixture.Store.Add(directive));
        Assert.That(fixture.Store.Directives, Has.Count.EqualTo(1));
        Assert.That(fixture.Store.Directives[0], Is.SameAs(directive));
        Assert.That(fixture.Store.Revision, Is.EqualTo(1L));
        Assert.That(fixture.Store.Directives, Has.Count.EqualTo(1));
        Assert.That(ReadMutationEpoch(fixture.Runtime), Is.EqualTo(long.MaxValue));
        Assert.That(directive.State, Is.EqualTo(ScheduledDirectiveState.Pending));
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out _), Is.False);
    }

    [Test]
    public void ScheduledDirectiveWriteInsideExistingMerchantBatchNotifiesAtOuterBoundary()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("p12-directive-batch-city", "p12-directive-batch-location");
        NpcRuntime merchant = new NpcRuntime(
            "p12-directive-batch-merchant",
            SimulationTestFactory.CreateNpc("p12-directive-batch-merchant", NpcJobType.Merchant),
            city,
            100f);
        P12DirectiveFixture fixture = CreateP12DirectiveRuntime(
            actors: new[] { merchant },
            cities: new[] { city });
        long epochBefore = ReadMutationEpoch(fixture.Runtime);
        MethodInfo beginMerchantOperation = typeof(SimulationRuntime).GetMethod(
            "TryBeginP12MerchantDailyNpcTradeOperation",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(beginMerchantOperation, Is.Not.Null);
        object[] arguments = { merchant, null };
        Assert.That(beginMerchantOperation.Invoke(fixture.Runtime, arguments), Is.EqualTo(true));
        IDisposable operation = (IDisposable)arguments[1];
        Assert.That(operation, Is.Not.Null);

        using (operation)
        {
            Assert.That(fixture.Store.Add(CreateDirective("p12-batched-add", 1L)), Is.True);
            Assert.That(ReadMutationEpoch(fixture.Runtime), Is.EqualTo(epochBefore),
                "the inner owner commit queues its changed section on the active operation");
        }

        Assert.That(ReadMutationEpoch(fixture.Runtime), Is.EqualTo(epochBefore + 1L),
            "the outer Merchant boundary reports the changed ScheduledDirective section once");
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
    }

    [Test]
    public void ConcurrentCensusSamplesRemainCoherentAcrossAdds()
    {
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(new SimulationTime());
        ScheduledDirectiveCensusProvider provider = new ScheduledDirectiveCensusProvider(store);
        ScheduledDirective[] prepared = new ScheduledDirective[64];
        for (int i = 0; i < prepared.Length; i++)
        {
            prepared[i] = CreateDirective("concurrent-" + i, 1L);
        }

        using ManualResetEventSlim stopSampling = new ManualResetEventSlim(false);
        using ManualResetEventSlim readerStarted = new ManualResetEventSlim(false);
        bool coherent = true;
        Task reader = Task.Run(() =>
        {
            readerStarted.Set();
            while (!stopSampling.IsSet)
            {
                OwnerSectionCensusWitness sample = provider.GetCurrentCensus();
                if (sample.Cardinality != sample.Revision) coherent = false;
            }
        });
        readerStarted.Wait();
        Task writer = Task.Run(() =>
        {
            foreach (ScheduledDirective directive in prepared)
            {
                if (!store.Add(directive)) coherent = false;
                Thread.Yield();
            }
        });
        Task.WaitAll(writer);
        stopSampling.Set();
        Task.WaitAll(reader);

        Assert.That(coherent, Is.True);
        AssertCensus(provider, store, prepared.Length, prepared.Length);
    }

    private static ScheduledDirective CreateDirective(
        string id,
        long day,
        string actorId = "npc-actor",
        ScheduledDirectiveMode mode = ScheduledDirectiveMode.RequestAction)
    {
        NpcActionData action = SimulationTestFactory.CreateAction(
            "escape-" + id,
            NpcActionType.EscapePrison,
            NpcActionCategory.Justice);
        return new ScheduledDirective(
            id,
            day,
            mode,
            ScheduledDirectiveOperation.EscapePrison,
            actorId,
            action);
    }

    private static void AssertCensus(
        ScheduledDirectiveCensusProvider provider,
        ScheduledDirectiveStore owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private static void SetRevision(ScheduledDirectiveStore store, long revision)
    {
        typeof(ScheduledDirectiveStore)
            .GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(store, revision);
    }

    private static P12DirectiveFixture CreateP12DirectiveRuntime(
        NpcRuntime[] actors = null,
        CityRuntime[] cities = null,
        RuntimeIdentityRegistry identityRegistry = null,
        Action<ScheduledDirectiveStore> initializeStore = null)
    {
        SimulationTime time = new SimulationTime();
        ScheduledDirectiveStore store = new ScheduledDirectiveStore(time);
        initializeStore?.Invoke(store);
        RuntimeIdentityRegistry registry = identityRegistry ?? new RuntimeIdentityRegistry();
        if (identityRegistry == null && actors != null)
        {
            foreach (NpcRuntime actor in actors)
                Assert.That(registry.RegisterNpc(actor), Is.True);
        }
        ScheduledDirectiveSystem system = new ScheduledDirectiveSystem(store, registry);
        SimulationRuntime runtime = new SimulationRuntime(
            time,
            cities,
            actors,
            scheduledDirectiveSystem: system,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        return new P12DirectiveFixture(time, store, system, runtime);
    }

    private static long ReadMutationEpoch(SimulationRuntime runtime)
    {
        object protocol = ReadPrivateField<object>(runtime, "npcRosterCensusProtocol");
        return ReadPrivateField<long>(protocol, "mutationEpoch");
    }

    private static void AssertMutationEpoch(SimulationRuntime runtime, long expected, string message)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long actual,
            out ContinuationCensusFailure failure), Is.True, failure.ToString());
        Assert.That(actual, Is.EqualTo(expected), message);
    }

    private static void SetMutationEpoch(SimulationRuntime runtime, long epoch)
    {
        object protocol = ReadPrivateField<object>(runtime, "npcRosterCensusProtocol");
        SetPrivateField(protocol, "mutationEpoch", epoch);
    }

    private static void AssertDirectiveRemainsUnbound(ScheduledDirective directive)
    {
        Assert.That(
            typeof(ScheduledDirective).GetField("ownerStore", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(directive),
            Is.Null);
        Assert.That(directive.CanBindMutationGuard(new AuthoritativeMutationGuard()), Is.True,
            "preflight rejection must precede the incoming directive's mutation-guard binding");
    }

    private static T ReadPrivateField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(target, value);
    }

    private sealed class P12DirectiveFixture
    {
        public SimulationTime Time { get; }
        public ScheduledDirectiveStore Store { get; }
        public ScheduledDirectiveSystem System { get; }
        public SimulationRuntime Runtime { get; }

        public P12DirectiveFixture(
            SimulationTime time,
            ScheduledDirectiveStore store,
            ScheduledDirectiveSystem system,
            SimulationRuntime runtime)
        {
            Time = time;
            Store = store;
            System = system;
            Runtime = runtime;
        }
    }
}
