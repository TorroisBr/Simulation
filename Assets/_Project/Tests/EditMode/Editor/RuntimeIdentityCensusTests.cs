using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class RuntimeIdentityCensusTests
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
    public void TypedRegistrationsUpdateOnlyTheirOwnIndexAndSharedRevision()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        IReadOnlyList<IOwnerSectionCensusProvider> providers = RuntimeIdentityRegistryCensusProvider.CreateProviders(registry);
        object ownerIdentity = registry;
        AssertCensus(Capture(providers), new int[8], 0L, ownerIdentity);

        Func<bool>[] registrations =
        {
            () => registry.RegisterNpc(new NpcRuntime("npc-one", null)),
            () => registry.RegisterCity(SimulationTestFactory.CreateCity("city-one", "city-location-one")),
            () => registry.RegisterLocation(new SpatialLocationRuntime("location-one")),
            () => registry.RegisterRoute(new SpatialRouteRuntime(
                "route-one", new SpatialLocationRuntime("route-origin"), new SpatialLocationRuntime("route-destination"), 2)),
            () => registry.RegisterExplorableSite(new ExplorableSiteRuntime(
                "site-one", SimulationTestFactory.CreateExplorableSite("site-definition-one"), new SpatialLocationRuntime("site-location-one"))),
            () => registry.RegisterLocalPlace(new LocalPlaceRuntime("local-place-one")),
            () => registry.RegisterLocalConnection(new LocalTopologyConnectionRuntime(
                "local-connection-one", new LocalPlaceRuntime("connection-origin"), new LocalPlaceRuntime("connection-destination"), 1f)),
            () => registry.RegisterNotableItem(new NotableItemRuntime(
                "notable-item-one", SimulationTestFactory.CreateItem("notable-item-definition-one")))
        };

        int[] expectedCardinalities = new int[8];
        for (int registeredIndex = 0; registeredIndex < registrations.Length; registeredIndex++)
        {
            OwnerSectionCensusWitness[] before = Capture(providers);
            Assert.That(registrations[registeredIndex](), Is.True);
            expectedCardinalities[registeredIndex]++;
            OwnerSectionCensusWitness[] after = Capture(providers);

            for (int index = 0; index < after.Length; index++)
            {
                Assert.That(after[index].SectionId, Is.EqualTo(ExpectedSectionIds[index]));
                Assert.That(after[index].SchemaVersion, Is.EqualTo(RuntimeIdentityRegistryCensusProvider.SchemaVersion));
                Assert.That(after[index].OwnerInstanceIdentity, Is.SameAs(ownerIdentity));
                Assert.That(after[index].Cardinality, Is.EqualTo(expectedCardinalities[index]));
                Assert.That(after[index].Revision, Is.EqualTo(before[index].Revision + 1L));
            }
        }
    }

    [Test]
    public void RejectedRegistrationsPreserveAllCountsAndRevision()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        IReadOnlyList<IOwnerSectionCensusProvider> providers = RuntimeIdentityRegistryCensusProvider.CreateProviders(registry);
        Assert.That(registry.RegisterNpc(new NpcRuntime("same-id", null)), Is.True);

        Func<bool>[] nullRegistrations =
        {
            () => registry.RegisterNpc(null),
            () => registry.RegisterCity(null),
            () => registry.RegisterLocation(null),
            () => registry.RegisterRoute(null),
            () => registry.RegisterExplorableSite(null),
            () => registry.RegisterLocalPlace(null),
            () => registry.RegisterLocalConnection(null),
            () => registry.RegisterNotableItem(null)
        };
        string[] nullDiagnostics =
        {
            "Cannot register NPC runtime identity: runtime instance is null.",
            "Cannot register City runtime identity: runtime instance is null.",
            "Cannot register Location runtime identity: runtime instance is null.",
            "Cannot register Route runtime identity: runtime instance is null.",
            "Cannot register ExplorableSite runtime identity: runtime instance is null.",
            "Cannot register LocalPlace runtime identity: runtime instance is null.",
            "Cannot register LocalConnection runtime identity: runtime instance is null.",
            "Cannot register NotableItem runtime identity: runtime instance is null."
        };
        for (int i = 0; i < nullRegistrations.Length; i++)
        {
            LogAssert.Expect(UnityEngine.LogType.Error, nullDiagnostics[i]);
            Assert.That(nullRegistrations[i](), Is.False);
        }

        LogAssert.Expect(
            UnityEngine.LogType.Error,
            "Duplicate RuntimeId 'same-id' while registering NPC; it is already registered as NPC.");
        Assert.That(registry.RegisterNpc(new NpcRuntime("same-id", null)), Is.False);
        LogAssert.Expect(
            UnityEngine.LogType.Error,
            "Duplicate RuntimeId 'same-id' while registering City; it is already registered as NPC.");
        Assert.That(registry.RegisterCity(SimulationTestFactory.CreateCity("same-id", "city-location")), Is.False);

        AssertCensus(Capture(providers), new[] { 1, 0, 0, 0, 0, 0, 0, 0 }, 1L, registry);
    }

    [Test]
    public void LocalTopologyBatchAdvancesOnceAndEmptyBatchDoesNotAdvance()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        CityRuntime owner = SimulationTestFactory.CreateCity("topology-owner", "topology-owner-location");
        Assert.That(registry.RegisterCity(owner), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(owner), registry);
        LocalPlaceRuntime entrance = new LocalPlaceRuntime("topology-entrance");
        LocalPlaceRuntime chamber = new LocalPlaceRuntime("topology-chamber");
        Assert.That(topology.AddPlace(entrance, isEntryPoint: true), Is.True);
        Assert.That(topology.AddPlace(chamber), Is.True);
        Assert.That(topology.AddConnection(new LocalTopologyConnectionRuntime(
            "topology-connection", entrance, chamber, 1f)), Is.True);

        LocalTopologyStore store = new LocalTopologyStore(registry);
        Assert.That(store.Add(topology), Is.True);
        AssertCensus(
            Capture(RuntimeIdentityRegistryCensusProvider.CreateProviders(registry)),
            new[] { 0, 1, 0, 0, 0, 2, 1, 0 },
            2L,
            registry);

        CityRuntime emptyOwner = SimulationTestFactory.CreateCity("empty-topology-owner", "empty-topology-location");
        Assert.That(registry.RegisterCity(emptyOwner), Is.True);
        LocalTopologyRuntime emptyTopology = new LocalTopologyRuntime(
            LocalTopologyOwnerReference.ForCity(emptyOwner), registry);
        Assert.That(store.Add(emptyTopology), Is.True, "An empty valid topology batch is a no-op for identity membership.");
        AssertCensus(
            Capture(RuntimeIdentityRegistryCensusProvider.CreateProviders(registry)),
            new[] { 0, 2, 0, 0, 0, 2, 1, 0 },
            3L,
            registry);
    }

    [Test]
    public void RejectedLocalTopologyBatchLeavesCountsAndRevisionUnchanged()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        CityRuntime owner = SimulationTestFactory.CreateCity("failed-batch-owner", "failed-batch-location");
        Assert.That(registry.RegisterCity(owner), Is.True);
        Assert.That(registry.RegisterLocation(new SpatialLocationRuntime("occupied-id")), Is.True);

        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(owner), registry);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("occupied-id")), Is.True);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        Assert.That(store.Add(topology), Is.False);

        AssertCensus(
            Capture(RuntimeIdentityRegistryCensusProvider.CreateProviders(registry)),
            new[] { 0, 1, 1, 0, 0, 0, 0, 0 },
            2L,
            registry);
    }

    [Test]
    public void RevisionExhaustionRejectsSingleAndBatchWritesBeforeMutation()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        CityRuntime owner = SimulationTestFactory.CreateCity("exhaustion-owner", "exhaustion-location");
        Assert.That(registry.RegisterCity(owner), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(owner), registry);
        Assert.That(topology.AddPlace(new LocalPlaceRuntime("exhaustion-place")), Is.True);

        typeof(RuntimeIdentityRegistry)
            .GetField("censusRevision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(registry, long.MaxValue);
        LogAssert.Expect(
            UnityEngine.LogType.Error,
            "Cannot register NPC runtime identity: census revision is exhausted.");
        Assert.That(registry.RegisterNpc(new NpcRuntime("exhaustion-npc", null)), Is.False);
        LocalTopologyStore store = new LocalTopologyStore(registry);
        LogAssert.Expect(
            UnityEngine.LogType.Error,
            "Cannot register local topology batch runtime identity: census revision is exhausted.");
        Assert.That(store.TryAddTopology(topology, out string diagnostic), Is.False);
        Assert.That(diagnostic, Is.EqualTo("Runtime identity census revision is exhausted."));

        AssertCensus(
            Capture(RuntimeIdentityRegistryCensusProvider.CreateProviders(registry)),
            new[] { 0, 1, 0, 0, 0, 0, 0, 0 },
            long.MaxValue,
            registry);
    }

    private static readonly string[] ExpectedSectionIds =
    {
        RuntimeIdentityRegistryCensusProvider.NpcsSectionId,
        RuntimeIdentityRegistryCensusProvider.CitiesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocationsSectionId,
        RuntimeIdentityRegistryCensusProvider.RoutesSectionId,
        RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocalPlacesSectionId,
        RuntimeIdentityRegistryCensusProvider.LocalConnectionsSectionId,
        RuntimeIdentityRegistryCensusProvider.NotableItemsSectionId
    };

    private static OwnerSectionCensusWitness[] Capture(IReadOnlyList<IOwnerSectionCensusProvider> providers)
    {
        OwnerSectionCensusWitness[] witnesses = new OwnerSectionCensusWitness[providers.Count];
        for (int i = 0; i < providers.Count; i++)
        {
            witnesses[i] = providers[i].GetCurrentCensus();
        }

        return witnesses;
    }

    private static void AssertCensus(
        OwnerSectionCensusWitness[] witnesses,
        int[] expectedCardinalities,
        long expectedRevision,
        object expectedOwnerIdentity)
    {
        Assert.That(witnesses.Length, Is.EqualTo(ExpectedSectionIds.Length));
        for (int i = 0; i < witnesses.Length; i++)
        {
            Assert.That(witnesses[i].SectionId, Is.EqualTo(ExpectedSectionIds[i]));
            Assert.That(witnesses[i].SchemaVersion, Is.EqualTo(RuntimeIdentityRegistryCensusProvider.SchemaVersion));
            Assert.That(witnesses[i].OwnerInstanceIdentity, Is.SameAs(expectedOwnerIdentity));
            Assert.That(witnesses[i].Cardinality, Is.EqualTo(expectedCardinalities[i]));
            Assert.That(witnesses[i].Revision, Is.EqualTo(expectedRevision));
        }
    }
}
