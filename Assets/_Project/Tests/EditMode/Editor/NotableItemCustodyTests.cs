using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class NotableItemCustodyTests
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
    public void NotableItemGetsIndependentRuntimeIdSequence()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        PlaceContentStore store = new PlaceContentStore(allocator, registry);

        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-000001"));
        Assert.That(store.CreateNotableItem(SimulationTestFactory.CreateItem("first-relic")).RuntimeId,
            Is.EqualTo("notable-item-000001"));
        Assert.That(store.CreateNotableItem(SimulationTestFactory.CreateItem("second-relic")).RuntimeId,
            Is.EqualTo("notable-item-000002"));
    }

    [Test]
    public void NotableItemRuntimeIdParticipatesInCrossTypeUniqueness()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        NpcRuntime npc = new NpcRuntime("shared-runtime-id", SimulationTestFactory.CreateNpc("shared-npc"));
        NotableItemRuntime notable = new NotableItemRuntime(
            "shared-runtime-id", SimulationTestFactory.CreateItem("shared-relic"));

        Assert.That(registry.RegisterNpc(npc), Is.True);
        LogAssert.Expect(LogType.Error,
            "Duplicate RuntimeId 'shared-runtime-id' while registering NotableItem; it is already registered as NPC.");
        Assert.That(registry.RegisterNotableItem(notable), Is.False);
        Assert.That(registry.TryGetNpc(npc.RuntimeId, out NpcRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(npc));
    }

    [Test]
    public void RegisteredNotableItemCanBeResolvedGlobally()
    {
        CreateStore(out RuntimeIdentityRegistry registry, out PlaceContentStore store);
        NotableItemRuntime notable = store.CreateNotableItem(SimulationTestFactory.CreateItem("global-relic"));

        Assert.That(store.TryGetNotableItem(notable.RuntimeId, out NotableItemRuntime stored), Is.True);
        Assert.That(registry.TryGetNotableItem(notable.RuntimeId, out NotableItemRuntime global), Is.True);
        Assert.That(stored, Is.SameAs(notable));
        Assert.That(global, Is.SameAs(notable));
    }

    [Test]
    public void NotableItemAtPlaceHasPlaceCustody()
    {
        CreateStore(out RuntimeIdentityRegistry registry, out PlaceContentStore store);
        CityRuntime city = RegisterCity(registry, "place-custody");
        NotableItemRuntime notable = store.CreateNotableItem(SimulationTestFactory.CreateItem("place-relic"));

        Assert.That(store.TryAddNotable(city, notable, out string diagnostic), Is.True, diagnostic);
        Assert.That(notable.Custody.CustodyKind, Is.EqualTo(NotableItemCustodyKind.Place));
        Assert.That(notable.Custody.PlaceOwner.StableKey,
            Is.EqualTo(PlaceContentOwnerReference.ForCity(city).StableKey));
    }

    [Test]
    public void TakingNotableItemTransfersCustodyToNpc()
    {
        TransferFixture fixture = new TransferFixture("take");

        Assert.That(fixture.Store.TryTransferNotableFromPlaceToNpc(
            fixture.CityOwner, fixture.Notable.RuntimeId, fixture.FirstNpc,
            out NotableItemRuntime taken, out string diagnostic), Is.True, diagnostic);

        Assert.That(taken, Is.SameAs(fixture.Notable));
        Assert.That(fixture.Store.GetNotableItemsAtPlace(fixture.CityOwner), Is.Empty);
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.FirstNpc), Contains.Item(fixture.Notable));
        Assert.That(fixture.Store.TryGetNotableItem(fixture.Notable.RuntimeId, out _), Is.True);
    }

    [Test]
    public void NpcCanQueryHeldNotableItems()
    {
        TransferFixture fixture = new TransferFixture("query");
        Assert.That(fixture.TakeToFirstNpc(out string diagnostic), Is.True, diagnostic);

        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.FirstNpc), Is.EqualTo(new[] { fixture.Notable }));
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.SecondNpc), Is.Empty);
    }

    [Test]
    public void NotableItemCannotBeHeldByTwoNpcs()
    {
        TransferFixture fixture = new TransferFixture("two-npcs");
        Assert.That(fixture.TakeToFirstNpc(out string firstDiagnostic), Is.True, firstDiagnostic);

        Assert.That(fixture.Store.TryTransferNotableFromPlaceToNpc(
            fixture.CityOwner, fixture.Notable.RuntimeId, fixture.SecondNpc,
            out _, out _), Is.False);
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.FirstNpc), Contains.Item(fixture.Notable));
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.SecondNpc), Is.Empty);
    }

    [Test]
    public void NotableItemCannotExistAtPlaceAndNpcSimultaneously()
    {
        TransferFixture fixture = new TransferFixture("exclusive");
        CityRuntime otherCity = RegisterCity(fixture.Registry, "exclusive-other");
        Assert.That(fixture.TakeToFirstNpc(out string diagnostic), Is.True, diagnostic);

        Assert.That(fixture.Store.TryAddNotable(otherCity, fixture.Notable, out _), Is.False);
        Assert.That(fixture.Store.GetNotableItemsAtPlace(fixture.CityOwner), Is.Empty);
        Assert.That(fixture.Store.GetNotableItemsAtPlace(PlaceContentOwnerReference.ForCity(otherCity)), Is.Empty);
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.FirstNpc), Contains.Item(fixture.Notable));
    }

    [Test]
    public void NpcToPlaceTransferIsAtomic()
    {
        TransferFixture fixture = new TransferFixture("return");
        CityRuntime destination = RegisterCity(fixture.Registry, "return-destination");
        PlaceContentOwnerReference destinationOwner = PlaceContentOwnerReference.ForCity(destination);
        Assert.That(fixture.TakeToFirstNpc(out string takeDiagnostic), Is.True, takeDiagnostic);

        Assert.That(fixture.Store.TryTransferNotableFromNpcToPlace(
            fixture.FirstNpc, fixture.Notable.RuntimeId, destinationOwner,
            out NotableItemRuntime transferred, out string diagnostic), Is.True, diagnostic);

        Assert.That(transferred, Is.SameAs(fixture.Notable));
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.FirstNpc), Is.Empty);
        Assert.That(fixture.Store.GetNotableItemsAtPlace(destinationOwner), Contains.Item(fixture.Notable));
        Assert.That(fixture.Notable.Custody.PlaceOwner.StableKey, Is.EqualTo(destinationOwner.StableKey));
    }

    [Test]
    public void FailedTransferPreservesOriginalCustody()
    {
        TransferFixture fixture = new TransferFixture("failed");
        CityRuntime destination = RegisterCity(fixture.Registry, "failed-destination");
        Assert.That(fixture.TakeToFirstNpc(out string takeDiagnostic), Is.True, takeDiagnostic);

        Assert.That(fixture.Store.TryTransferNotableFromNpcToPlace(
            fixture.SecondNpc, fixture.Notable.RuntimeId, PlaceContentOwnerReference.ForCity(destination),
            out _, out _), Is.False);

        Assert.That(fixture.Notable.IsHeldByNpc, Is.True);
        Assert.That(fixture.Notable.CustodianNpcRuntimeId, Is.EqualTo(fixture.FirstNpc.RuntimeId));
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.FirstNpc), Contains.Item(fixture.Notable));
    }

    [Test]
    public void TakingNotableDoesNotRemoveGlobalIdentity()
    {
        TransferFixture fixture = new TransferFixture("global-after-take");
        Assert.That(fixture.TakeToFirstNpc(out string diagnostic), Is.True, diagnostic);

        Assert.That(fixture.Store.TryGetNotableItem(
            fixture.Notable.RuntimeId, out NotableItemRuntime stored), Is.True);
        Assert.That(fixture.Registry.TryGetNotableItem(
            fixture.Notable.RuntimeId, out NotableItemRuntime registered), Is.True);
        Assert.That(stored, Is.SameAs(fixture.Notable));
        Assert.That(registered, Is.SameAs(fixture.Notable));
    }

    [Test]
    public void NotableItemPersistsAcrossAdvanceDays()
    {
        TransferFixture fixture = new TransferFixture("days");
        Assert.That(fixture.TakeToFirstNpc(out string diagnostic), Is.True, diagnostic);

        fixture.Store.AdvanceDays(5000);

        Assert.That(fixture.Store.TryGetNotableItem(fixture.Notable.RuntimeId, out _), Is.True);
        Assert.That(fixture.Store.GetNotableItemsHeldByNpc(fixture.FirstNpc), Contains.Item(fixture.Notable));
    }

    [Test]
    public void LocalPlaceOwnerWithoutTopologyOrExplicitMacroLocationIsRejected()
    {
        LocalPlaceRuntime orphan = new LocalPlaceRuntime("orphan-place", "Orphan");

        Assert.That(
            () => PlaceContentOwnerReference.ForLocalPlace(orphan),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void LocalPlaceOwnerUsesOwningTopologyMacroLocationWhenAvailable()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        CityRuntime city = RegisterCity(registry, "topology-custody");
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
        LocalPlaceRuntime localPlace = new LocalPlaceRuntime("topology-custody-room", "Room");
        Assert.That(topology.AddPlace(localPlace, null, true), Is.True);

        PlaceContentOwnerReference owner = PlaceContentOwnerReference.ForLocalPlace(localPlace);

        Assert.That(owner.MacroLocationRuntimeId, Is.EqualTo(city.Location.RuntimeId));
        Assert.That(owner.TopologyOwnerRuntimeId, Is.EqualTo(city.RuntimeId));
    }

    [Test]
    public void CommonStackItemsRemainAggregateWithoutRuntimeIds()
    {
        CreateStore(out RuntimeIdentityRegistry registry, out PlaceContentStore store);
        CityRuntime city = RegisterCity(registry, "aggregate-stack");
        ItemData iron = SimulationTestFactory.CreateItem("aggregate-iron");

        Assert.That(store.TryAddStack(
            city, iron, 100, PlaceContentPersistencePolicy.Durable, out PlaceContentStackRuntime stack), Is.True);
        Assert.That(stack.Amount, Is.EqualTo(100));
        Assert.That(typeof(PlaceContentStackRuntime).GetProperty("RuntimeId"), Is.Null);
        Assert.That(store.NotableItems, Is.Empty);
    }

    private static void CreateStore(
        out RuntimeIdentityRegistry registry,
        out PlaceContentStore store)
    {
        registry = new RuntimeIdentityRegistry();
        store = new PlaceContentStore(new RuntimeIdAllocator(), registry);
    }

    private static CityRuntime RegisterCity(RuntimeIdentityRegistry registry, string id)
    {
        CityRuntime city = SimulationTestFactory.CreateCity(id, id + "-location");
        Assert.That(registry.RegisterCity(city), Is.True);
        Assert.That(registry.RegisterLocation(city.Location), Is.True);
        return city;
    }

    private sealed class TransferFixture
    {
        public RuntimeIdentityRegistry Registry { get; }
        public PlaceContentStore Store { get; }
        public PlaceContentOwnerReference CityOwner { get; }
        public NotableItemRuntime Notable { get; }
        public NpcRuntime FirstNpc { get; }
        public NpcRuntime SecondNpc { get; }

        public TransferFixture(string id)
        {
            Registry = new RuntimeIdentityRegistry();
            Store = new PlaceContentStore(new RuntimeIdAllocator(), Registry);
            CityRuntime city = RegisterCity(Registry, id + "-city");
            CityOwner = PlaceContentOwnerReference.ForCity(city);
            FirstNpc = new NpcRuntime(id + "-first", SimulationTestFactory.CreateNpc(id + "-first"));
            SecondNpc = new NpcRuntime(id + "-second", SimulationTestFactory.CreateNpc(id + "-second"));
            Assert.That(Registry.RegisterNpc(FirstNpc), Is.True);
            Assert.That(Registry.RegisterNpc(SecondNpc), Is.True);
            Notable = Store.CreateNotableItem(SimulationTestFactory.CreateItem(id + "-relic"));
            Assert.That(Store.TryAddNotable(CityOwner, Notable, out string diagnostic), Is.True, diagnostic);
        }

        public bool TakeToFirstNpc(out string diagnostic)
        {
            return Store.TryTransferNotableFromPlaceToNpc(
                CityOwner,
                Notable.RuntimeId,
                FirstNpc,
                out _,
                out diagnostic);
        }
    }
}
