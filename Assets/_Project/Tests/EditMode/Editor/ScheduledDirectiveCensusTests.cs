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

    private static ScheduledDirective CreateDirective(string id, long day, string actorId = "npc-actor")
    {
        NpcActionData action = SimulationTestFactory.CreateAction(
            "escape-" + id,
            NpcActionType.EscapePrison,
            NpcActionCategory.Justice);
        return new ScheduledDirective(
            id,
            day,
            ScheduledDirectiveMode.RequestAction,
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
}
