using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

public sealed class WorldStateDiagnosticsTests
{
    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (UnityEngine.Object createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void EmptyContextProducesValidSnapshot()
    {
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext());

        Assert.IsNotNull(snapshot);
        Assert.AreEqual(0L, snapshot.AbsoluteDay);
        Assert.IsEmpty(snapshot.Npcs);
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("METADATA|AbsoluteDay|0"));
    }

    [Test]
    public void SnapshotContainsAbsoluteDay()
    {
        WorldStateSnapshot snapshot = BuildSnapshot(new SimulationTime(25L));

        Assert.AreEqual(25L, snapshot.AbsoluteDay);
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("METADATA|AbsoluteDay|25"));
    }

    [Test]
    public void SnapshotBuildDoesNotAdvanceTime()
    {
        SimulationTime time = new SimulationTime(12L);

        WorldStateSnapshotBuilder.BuildSnapshot(Context(time));

        Assert.AreEqual(12L, time.AbsoluteDay);
    }

    [Test]
    public void SnapshotBuildDoesNotMutateNpc()
    {
        SpatialLocationRuntime location = new SpatialLocationRuntime("location-1");
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        Assert.IsTrue(npc.SetCurrentPresence(location));
        InventoryRuntime inventory = npc.Inventory;
        int statusCount = npc.CurrentStatus.Count;
        string currentLocationId = npc.CurrentLocation.RuntimeId;

        WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(1L), new[] { npc }));

        Assert.AreSame(inventory, npc.Inventory);
        Assert.AreEqual(statusCount, npc.CurrentStatus.Count);
        Assert.AreEqual(currentLocationId, npc.CurrentLocation.RuntimeId);
        Assert.IsNull(npc.CurrentActionRuntime);
    }

    [Test]
    public void SnapshotBuildDoesNotMutateInventory()
    {
        ItemData item = CreateItem("iron", "Iron", 2.5f);
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        npc.Inventory.AddItem(item, 8, 1.25f);
        int amount = npc.Inventory.GetAmount(item);
        float averageUnitCost = npc.Inventory.GetAverageUnitCost(item);

        WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(1L), new[] { npc }));

        Assert.AreEqual(amount, npc.Inventory.GetAmount(item));
        Assert.AreEqual(averageUnitCost, npc.Inventory.GetAverageUnitCost(item));
        Assert.AreEqual(1, npc.Inventory.Items.Count);
    }

    [Test]
    public void SnapshotBuildDoesNotAllocateRuntimeIds()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.AreEqual("npc-000001", allocator.AllocateNpcId());

        WorldStateSnapshotBuilder.BuildSnapshot(Context());

        Assert.AreEqual("npc-000002", allocator.AllocateNpcId());
    }

    [Test]
    public void RepeatedSnapshotOfSameWorldIsCanonicalEqual()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateSnapshotContext context = Context(new SimulationTime(2L), new[] { npc });

        string first = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(context));
        string second = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(context));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void RepeatedSnapshotHasSameDigest()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateSnapshotContext context = Context(new SimulationTime(2L), new[] { npc });

        string first = WorldStateSnapshotDigest.Compute(WorldStateSnapshotBuilder.BuildSnapshot(context));
        string second = WorldStateSnapshotDigest.Compute(WorldStateSnapshotBuilder.BuildSnapshot(context));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void NpcOrderDoesNotAffectCanonicalSnapshot()
    {
        NpcRuntime firstNpc = CreateNpc("npc-a", "definition-a", "A");
        NpcRuntime secondNpc = CreateNpc("npc-b", "definition-b", "B");
        WorldStateSnapshot first = WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(3L), new[] { secondNpc, firstNpc }));
        WorldStateSnapshot second = WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(3L), new[] { firstNpc, secondNpc }));

        Assert.AreEqual(WorldStateCanonicalWriter.Write(first), WorldStateCanonicalWriter.Write(second));
    }

    [Test]
    public void CityOrderDoesNotAffectCanonicalSnapshot()
    {
        CityRuntime firstCity = CreateCity("city-a", "city-definition-a", "location-a");
        CityRuntime secondCity = CreateCity("city-b", "city-definition-b", "location-b");
        WorldStateSnapshot first = WorldStateSnapshotBuilder.BuildSnapshot(Context(
            new SimulationTime(3L),
            cities: new[] { secondCity, firstCity }));
        WorldStateSnapshot second = WorldStateSnapshotBuilder.BuildSnapshot(Context(
            new SimulationTime(3L),
            cities: new[] { firstCity, secondCity }));

        Assert.AreEqual(WorldStateCanonicalWriter.Write(first), WorldStateCanonicalWriter.Write(second));
    }

    [Test]
    public void RouteRegistrationOrderDoesNotAffectCanonicalSnapshot()
    {
        SpatialNetworkRuntime firstNetwork = CreateNetwork(false);
        SpatialNetworkRuntime secondNetwork = CreateNetwork(true);

        WorldStateSnapshot first = WorldStateSnapshotBuilder.BuildSnapshot(Context(spatialNetwork: firstNetwork));
        WorldStateSnapshot second = WorldStateSnapshotBuilder.BuildSnapshot(Context(spatialNetwork: secondNetwork));

        Assert.AreEqual(WorldStateCanonicalWriter.Write(first), WorldStateCanonicalWriter.Write(second));
    }

    [Test]
    public void InventoryInsertionOrderDoesNotAffectCanonicalSnapshot()
    {
        ItemData firstIron = CreateItem("iron", "Iron", 2f);
        ItemData secondIron = CreateItem("iron", "Iron", 2f);
        NpcRuntime firstNpc = CreateNpc("npc-1", "npc-definition", "Display Name");
        NpcRuntime secondNpc = CreateNpc("npc-1", "npc-definition", "Display Name");
        firstNpc.Inventory.AddItem(firstIron, 2, 1f);
        firstNpc.Inventory.AddItem(secondIron, 6, 3f);
        secondNpc.Inventory.AddItem(secondIron, 6, 3f);
        secondNpc.Inventory.AddItem(firstIron, 2, 1f);

        string first = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(1L), new[] { firstNpc })));
        string second = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(1L), new[] { secondNpc })));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void NpcSnapshotUsesRuntimeIdNotDisplayName()
    {
        NpcRuntime npc = CreateNpc("npc-runtime-id", "npc-definition-id", "Visible Display Name");

        WorldStateSnapshot snapshot = BuildSnapshot(new SimulationTime(1L), new[] { npc });
        string canonical = WorldStateCanonicalWriter.Write(snapshot);

        Assert.AreEqual("npc-runtime-id", snapshot.Npcs[0].RuntimeId);
        Assert.AreEqual("npc-definition-id", snapshot.Npcs[0].DefinitionId);
        Assert.That(canonical, Does.Not.Contain("Visible Display Name"));
    }

    [Test]
    public void CommonStackUsesDefinitionIdWithoutRuntimeId()
    {
        ItemData item = CreateItem("iron-definition", "Iron", 2f);
        CityRuntime city = CreateCity("city-1", "city-definition", "location-1");
        PlaceContentStore store = new PlaceContentStore();
        Assert.IsTrue(store.TryAddStack(city, item, 12, PlaceContentPersistencePolicy.Durable, out _, averageUnitCost: 1.5f));

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(Context(
            new SimulationTime(1L),
            cities: new[] { city },
            placeContentStore: store));
        string canonical = WorldStateCanonicalWriter.Write(snapshot);

        Assert.That(canonical, Does.Contain("PLACE_STACK"));
        Assert.That(canonical, Does.Contain("iron-definition"));
        Assert.That(canonical, Does.Not.Contain("stack-runtime"));
    }

    [Test]
    public void NotableItemSnapshotPreservesRuntimeIdentity()
    {
        NotableFixture fixture = CreateNotableFixture();
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(Context(
            new SimulationTime(1L),
            cities: new[] { fixture.City },
            placeContentStore: fixture.Store));

        Assert.AreEqual("notable-item-1", snapshot.NotableItems[0].RuntimeId);
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("notable-item-1"));
    }

    [Test]
    public void NotableNpcCustodyIsCaptured()
    {
        NotableFixture fixture = CreateNotableFixture();
        Assert.IsTrue(fixture.Registry.RegisterNpc(fixture.Npc));
        Assert.IsTrue(fixture.Store.TryTransferNotableFromPlaceToNpc(
            fixture.Owner,
            "notable-item-1",
            fixture.Npc,
            out _,
            out string diagnostic), diagnostic);

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(Context(
            new SimulationTime(1L),
            npcs: new[] { fixture.Npc },
            cities: new[] { fixture.City },
            placeContentStore: fixture.Store));

        Assert.AreEqual("npc-1", snapshot.NotableItems[0].CustodianNpcRuntimeId);
        Assert.AreEqual(NotableItemCustodyKind.Npc, snapshot.NotableItems[0].CustodyKind);
    }

    [Test]
    public void NotablePlaceCustodyIsCaptured()
    {
        NotableFixture fixture = CreateNotableFixture();
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(Context(
            new SimulationTime(1L),
            cities: new[] { fixture.City },
            placeContentStore: fixture.Store));

        Assert.AreEqual(NotableItemCustodyKind.Place, snapshot.NotableItems[0].CustodyKind);
        Assert.AreEqual(fixture.City.RuntimeId, snapshot.NotableItems[0].OwnerRuntimeId);
        Assert.AreEqual(fixture.City.Location.RuntimeId, snapshot.NotableItems[0].OwnerMacroLocationRuntimeId);
    }

    [Test]
    public void ExpeditionSnapshotPreservesExpeditionIdAsActivityIdentity()
    {
        ExpeditionFixture fixture = CreateExpeditionFixture();
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(Context(
            new SimulationTime(1L),
            npcs: fixture.Npcs,
            expeditionStore: fixture.Store));

        Assert.AreEqual("expedition-activity-1", snapshot.Expeditions[0].ExpeditionId);
        Assert.AreEqual("expedition-activity-1", snapshot.Npcs[0].ActiveExpeditionId);
    }

    [Test]
    public void ExpeditionParticipantOrderIsDeterministic()
    {
        ExpeditionRuntime first = CreateExpedition("expedition-1", new[] { "npc-b", "npc-a" }, new[] { "npc-b" }, new[] { "npc-a" });
        ExpeditionRuntime second = CreateExpedition("expedition-1", new[] { "npc-a", "npc-b" }, new[] { "npc-b" }, new[] { "npc-a" });
        ExpeditionStore firstStore = new ExpeditionStore();
        ExpeditionStore secondStore = new ExpeditionStore();
        Assert.IsTrue(firstStore.Add(first));
        Assert.IsTrue(secondStore.Add(second));

        string firstCanonical = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(Context(expeditionStore: firstStore)));
        string secondCanonical = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(Context(expeditionStore: secondStore)));

        Assert.AreEqual(firstCanonical, secondCanonical);
    }

    [Test]
    public void LocalTopologyPlaceOrderIsDeterministic()
    {
        TopologyFixture first = CreateTopologyFixture(false, false);
        TopologyFixture second = CreateTopologyFixture(true, false);
        string firstCanonical = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(Context(localTopologyStore: first.Store)));
        string secondCanonical = WorldStateCanonicalWriter.Write(WorldStateSnapshotBuilder.BuildSnapshot(Context(localTopologyStore: second.Store)));

        Assert.AreEqual(firstCanonical, secondCanonical);
    }

    [Test]
    public void LocalTopologyConnectionDirectionIsPreserved()
    {
        TopologyFixture fixture = CreateTopologyFixture(false, false);
        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(Context(localTopologyStore: fixture.Store));
        WorldStateLocalConnectionSnapshot connection = snapshot.LocalTopologies[0].Connections[0];

        Assert.AreEqual("local-place-root", connection.OriginRuntimeId);
        Assert.AreEqual("local-place-a", connection.DestinationRuntimeId);
    }

    [Test]
    public void HierarchyAndTraversalRemainDistinct()
    {
        TopologyFixture fixture = CreateTopologyFixture(false, false);
        WorldStateLocalTopologySnapshot topology = WorldStateSnapshotBuilder.BuildSnapshot(Context(localTopologyStore: fixture.Store)).LocalTopologies[0];
        WorldStateLocalPlaceSnapshot child = FindLocalPlace(topology, "local-place-a");
        WorldStateLocalConnectionSnapshot connection = topology.Connections[0];

        Assert.AreEqual("local-place-root", child.ParentRuntimeId);
        Assert.AreEqual("local-place-root", connection.OriginRuntimeId);
        Assert.AreEqual("local-place-a", connection.DestinationRuntimeId);
    }

    [Test]
    public void SnapshotDoesNotContainDecisionHistory()
    {
        string canonical = WorldStateCanonicalWriter.Write(BuildSnapshot(new SimulationTime(1L)));

        Assert.That(canonical, Does.Not.Contain("DECISION_STORE"));
        Assert.That(canonical, Does.Not.Contain("DECISION_HISTORY"));
    }

    [Test]
    public void SnapshotDoesNotContainDomainEventHistory()
    {
        string canonical = WorldStateCanonicalWriter.Write(BuildSnapshot(new SimulationTime(1L)));

        Assert.That(canonical, Does.Not.Contain("DOMAIN_EVENT"));
        Assert.That(canonical, Does.Not.Contain("EVENT_STORE"));
    }

    [Test]
    public void SnapshotDoesNotContainNpcKnowledge()
    {
        string canonical = WorldStateCanonicalWriter.Write(BuildSnapshot(new SimulationTime(1L)));

        Assert.That(canonical, Does.Not.Contain("KNOWLEDGE"));
        Assert.That(canonical, Does.Not.Contain("SpatialKnowledge"));
    }

    [Test]
    public void IdenticalSnapshotsProduceEmptyDiff()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateSnapshotContext context = Context(new SimulationTime(4L), new[] { npc });
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(context);
        WorldStateSnapshot after = WorldStateSnapshotBuilder.BuildSnapshot(context);

        Assert.IsTrue(WorldStateDiff.Compare(before, after).IsEmpty);
    }

    [Test]
    public void NpcLocationChangeProducesSingleSemanticDifference()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        SpatialLocationRuntime firstLocation = new SpatialLocationRuntime("location-1");
        SpatialLocationRuntime secondLocation = new SpatialLocationRuntime("location-2");
        Assert.IsTrue(npc.SetCurrentPresence(firstLocation));
        WorldStateSnapshot before = BuildSnapshot(new SimulationTime(1L), new[] { npc });
        Assert.IsTrue(npc.SetCurrentPresence(secondLocation));
        WorldStateDiff diff = WorldStateDiff.Compare(before, BuildSnapshot(new SimulationTime(1L), new[] { npc }));

        Assert.AreEqual(1, diff.Differences.Count);
        Assert.AreEqual("CurrentLocationRuntimeId", diff.Differences[0].Field);
        Assert.AreEqual("location-1", diff.Differences[0].BeforeValue);
        Assert.AreEqual("location-2", diff.Differences[0].AfterValue);
    }

    [Test]
    public void NpcInjuryChangeIsDetected()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateSnapshot before = BuildSnapshot(new SimulationTime(1L), new[] { npc });
        Assert.IsTrue(npc.TryApplyInjury(NpcInjurySeverity.SeriouslyInjured));
        WorldStateDiff diff = WorldStateDiff.Compare(before, BuildSnapshot(new SimulationTime(1L), new[] { npc }));

        Assert.AreEqual(1, diff.Differences.Count);
        Assert.AreEqual("InjurySeverity", diff.Differences[0].Field);
    }

    [Test]
    public void InventoryAmountChangeIsDetected()
    {
        ItemData item = CreateItem("iron", "Iron", 2f);
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        npc.Inventory.AddItem(item, 20, 1f);
        WorldStateSnapshot before = BuildSnapshot(new SimulationTime(1L), new[] { npc });
        Assert.IsTrue(npc.Inventory.RemoveItem(item, 4));
        WorldStateDiff diff = WorldStateDiff.Compare(before, BuildSnapshot(new SimulationTime(1L), new[] { npc }));

        Assert.AreEqual(1, diff.Differences.Count);
        Assert.AreEqual("Amount", diff.Differences[0].Field);
        Assert.AreEqual("20", diff.Differences[0].BeforeValue);
        Assert.AreEqual("16", diff.Differences[0].AfterValue);
    }

    [Test]
    public void AddedNpcIsReportedAsAdded()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateDiff diff = WorldStateDiff.Compare(BuildSnapshot(new SimulationTime(1L)), BuildSnapshot(new SimulationTime(1L), new[] { npc }));

        Assert.AreEqual(WorldStateDifferenceChangeKind.Added, diff.Differences[0].ChangeKind);
        Assert.AreEqual("npc-1", diff.Differences[0].Identity);
    }

    [Test]
    public void RemovedNpcIsReportedAsRemoved()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateDiff diff = WorldStateDiff.Compare(BuildSnapshot(new SimulationTime(1L), new[] { npc }), BuildSnapshot(new SimulationTime(1L)));

        Assert.AreEqual(WorldStateDifferenceChangeKind.Removed, diff.Differences[0].ChangeKind);
        Assert.AreEqual("npc-1", diff.Differences[0].Identity);
    }

    [Test]
    public void NotableCustodyTransferIsDetected()
    {
        NotableFixture fixture = CreateNotableFixture();
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(Context(placeContentStore: fixture.Store, cities: new[] { fixture.City }));
        Assert.IsTrue(fixture.Registry.RegisterNpc(fixture.Npc));
        Assert.IsTrue(fixture.Store.TryTransferNotableFromPlaceToNpc(
            fixture.Owner, "notable-item-1", fixture.Npc, out _, out string diagnostic), diagnostic);
        WorldStateDiff diff = WorldStateDiff.Compare(before, WorldStateSnapshotBuilder.BuildSnapshot(Context(
            placeContentStore: fixture.Store, cities: new[] { fixture.City }, npcs: new[] { fixture.Npc })));

        Assert.IsTrue(ContainsDifference(diff, "NotableItem", "Custody"));
    }

    [Test]
    public void PlaceStackConsumptionIsDetected()
    {
        CityRuntime city = CreateCity("city-1", "city-definition", "location-1");
        ItemData item = CreateItem("iron", "Iron", 2f);
        PlaceContentStore store = new PlaceContentStore();
        Assert.IsTrue(store.TryAddStack(city, item, 20, PlaceContentPersistencePolicy.Durable, out _));
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(Context(placeContentStore: store, cities: new[] { city }));
        Assert.IsTrue(store.GetOrCreate(city).GetStack(item).TryRemove(4));
        WorldStateDiff diff = WorldStateDiff.Compare(before, WorldStateSnapshotBuilder.BuildSnapshot(Context(placeContentStore: store, cities: new[] { city })));

        Assert.IsTrue(ContainsDifference(diff, "PlaceStack", "Amount"));
    }

    [Test]
    public void ExpeditionStateChangeIsDetected()
    {
        ExpeditionFixture fixture = CreateExpeditionFixture();
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(Context(expeditionStore: fixture.Store));
        Assert.IsTrue(fixture.Expedition.TryBeginExploration());
        WorldStateDiff diff = WorldStateDiff.Compare(before, WorldStateSnapshotBuilder.BuildSnapshot(Context(expeditionStore: fixture.Store)));

        Assert.IsTrue(ContainsDifference(diff, "Expedition", "State"));
    }

    [Test]
    public void ExpeditionCurrentLocalPlaceChangeIsDetected()
    {
        ExpeditionFixture fixture = CreateExpeditionFixture();
        Assert.IsTrue(fixture.Expedition.TryBeginExploration());
        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(Context(expeditionStore: fixture.Store));
        Assert.IsTrue(fixture.Expedition.TrySetCurrentLocalPlace("local-place-1"));
        WorldStateDiff diff = WorldStateDiff.Compare(before, WorldStateSnapshotBuilder.BuildSnapshot(Context(expeditionStore: fixture.Store)));

        Assert.IsTrue(ContainsDifference(diff, "Expedition", "CurrentLocalPlaceRuntimeId"));
    }

    [Test]
    public void AddedLocalPlaceIsDetected()
    {
        WorldStateLocalTopologySnapshot beforeTopology = CreateManualTopologySnapshot(false, false);
        WorldStateLocalTopologySnapshot afterTopology = CreateManualTopologySnapshot(true, false);
        WorldStateDiff diff = WorldStateDiff.Compare(
            new WorldStateSnapshot(1L, localTopologies: new[] { beforeTopology }),
            new WorldStateSnapshot(1L, localTopologies: new[] { afterTopology }));

        Assert.IsTrue(ContainsDifference(diff, "LocalPlace", "Entity"));
    }

    [Test]
    public void AddedDirectedConnectionIsDetected()
    {
        WorldStateLocalTopologySnapshot beforeTopology = CreateManualTopologySnapshot(true, false);
        WorldStateLocalTopologySnapshot afterTopology = CreateManualTopologySnapshot(true, true);
        WorldStateDiff diff = WorldStateDiff.Compare(
            new WorldStateSnapshot(1L, localTopologies: new[] { beforeTopology }),
            new WorldStateSnapshot(1L, localTopologies: new[] { afterTopology }));

        Assert.IsTrue(ContainsDifference(diff, "LocalConnection", "Entity"));
    }

    [Test]
    public void DiffOrderingIsDeterministic()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateSnapshot before = BuildSnapshot(new SimulationTime(1L), new[] { npc });
        Assert.IsTrue(npc.TryApplyInjury(NpcInjurySeverity.SeriouslyInjured));
        SpatialLocationRuntime location = new SpatialLocationRuntime("location-1");
        SpatialLocationRuntime otherLocation = new SpatialLocationRuntime("location-2");
        SpatialRouteRuntime route = new SpatialRouteRuntime("route-1", location, otherLocation, 2);
        WorldStateSnapshot after = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            new SimulationTime(1L), new[] { npc }, spatialNetwork: CreateNetworkWithRoute(route)));
        WorldStateDiff diff = WorldStateDiff.Compare(before, after);

        Assert.GreaterOrEqual(diff.Differences.Count, 2);
        Assert.AreEqual("NPC", diff.Differences[0].Section);
        Assert.AreEqual("Route", diff.Differences[diff.Differences.Count - 1].Section);
    }

    [Test]
    public void DiffDoesNotDependOnInputCollectionOrder()
    {
        NpcRuntime firstNpc = CreateNpc("npc-a", "definition-a", "A");
        NpcRuntime secondNpc = CreateNpc("npc-b", "definition-b", "B");
        WorldStateSnapshot first = WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(1L), new[] { firstNpc, secondNpc }));
        WorldStateSnapshot second = WorldStateSnapshotBuilder.BuildSnapshot(Context(new SimulationTime(1L), new[] { secondNpc, firstNpc }));

        Assert.IsTrue(WorldStateDiff.Compare(first, second).IsEmpty);
    }

    [Test]
    public void CanonicalWriterProducesStableOutput()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        WorldStateSnapshot snapshot = BuildSnapshot(new SimulationTime(7L), new[] { npc });

        Assert.AreEqual(WorldStateCanonicalWriter.Write(snapshot), WorldStateCanonicalWriter.Write(snapshot));
    }

    [Test]
    public void CanonicalWriterDoesNotUseObjectGetHashCode()
    {
        NpcRuntime firstNpc = CreateNpc("npc-1", "npc-definition", "Display Name");
        NpcRuntime secondNpc = CreateNpc("npc-1", "npc-definition", "Display Name");

        string first = WorldStateCanonicalWriter.Write(BuildSnapshot(new SimulationTime(1L), new[] { firstNpc }));
        string second = WorldStateCanonicalWriter.Write(BuildSnapshot(new SimulationTime(1L), new[] { secondNpc }));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void CanonicalWriterUsesInvariantCultureForNumbers()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
            npc.AddMoney(12.5f);
            string canonical = WorldStateCanonicalWriter.Write(BuildSnapshot(new SimulationTime(1L), new[] { npc }));

            Assert.That(canonical, Does.Contain("12.5"));
            Assert.That(canonical, Does.Not.Contain("12,5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Test]
    public void CanonicalWriterEscapesDelimiterSensitiveValuesIfNecessary()
    {
        NpcData definition = CreateNpcData("definition|with-pipe", "display\nname");
        NpcRuntime npc = new NpcRuntime("npc|with-pipe", definition);
        string canonical = WorldStateCanonicalWriter.Write(BuildSnapshot(new SimulationTime(1L), new[] { npc }));

        Assert.That(canonical, Does.Contain("npc\\|with-pipe"));
        Assert.That(canonical, Does.Contain("definition\\|with-pipe"));
        Assert.That(canonical, Does.Not.Contain("display"));
    }

    [Test]
    public void DigestChangesWhenSemanticStateChanges()
    {
        NpcRuntime npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        string before = WorldStateSnapshotDigest.Compute(BuildSnapshot(new SimulationTime(1L), new[] { npc }));
        Assert.IsTrue(npc.TryApplyInjury(NpcInjurySeverity.SeriouslyInjured));
        string after = WorldStateSnapshotDigest.Compute(BuildSnapshot(new SimulationTime(1L), new[] { npc }));

        Assert.AreNotEqual(before, after);
    }

    [Test]
    public void DigestDoesNotChangeWhenInputOrderChanges()
    {
        NpcRuntime firstNpc = CreateNpc("npc-a", "definition-a", "A");
        NpcRuntime secondNpc = CreateNpc("npc-b", "definition-b", "B");
        string first = WorldStateSnapshotDigest.Compute(BuildSnapshot(new SimulationTime(1L), new[] { firstNpc, secondNpc }));
        string second = WorldStateSnapshotDigest.Compute(BuildSnapshot(new SimulationTime(1L), new[] { secondNpc, firstNpc }));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void CreateDeterministicSnapshotsAcrossEquivalentRuns()
    {
        SimulationTime firstTime = new SimulationTime(0L);
        SimulationTime secondTime = new SimulationTime(0L);
        NpcRuntime firstNpc = CreateNpc("npc-1", "npc-definition", "Display Name");
        NpcRuntime secondNpc = CreateNpc("npc-1", "npc-definition", "Display Name");
        for (int day = 0; day < 10; day++)
        {
            firstTime.AdvanceDay();
            secondTime.AdvanceDay();
        }

        WorldStateSnapshot first = BuildSnapshot(firstTime, new[] { firstNpc });
        WorldStateSnapshot second = BuildSnapshot(secondTime, new[] { secondNpc });

        Assert.AreEqual(WorldStateCanonicalWriter.Write(first), WorldStateCanonicalWriter.Write(second));
        Assert.AreEqual(WorldStateSnapshotDigest.Compute(first), WorldStateSnapshotDigest.Compute(second));
    }

    private WorldStateSnapshot BuildSnapshot(SimulationTime time, IEnumerable<NpcRuntime> npcs = null)
    {
        return WorldStateSnapshotBuilder.BuildSnapshot(Context(time, npcs));
    }

    private WorldStateSnapshotContext Context(
        SimulationTime simulationTime = null,
        IEnumerable<NpcRuntime> npcs = null,
        IEnumerable<CityRuntime> cities = null,
        SpatialNetworkRuntime spatialNetwork = null,
        ExplorableSiteStore explorableSiteStore = null,
        ExpeditionStore expeditionStore = null,
        PlaceContentStore placeContentStore = null,
        LocalTopologyStore localTopologyStore = null)
    {
        return new WorldStateSnapshotContext(
            simulationTime,
            npcs,
            cities,
            spatialNetwork,
            explorableSiteStore,
            expeditionStore,
            placeContentStore,
            localTopologyStore);
    }

    private NpcRuntime CreateNpc(string runtimeId, string definitionId, string displayName)
    {
        return new NpcRuntime(runtimeId, CreateNpcData(definitionId, displayName));
    }

    private NpcData CreateNpcData(string definitionId, string displayName)
    {
        NpcData data = Track(ScriptableObject.CreateInstance<NpcData>());
        data.id = definitionId;
        data.name = displayName;
        return data;
    }

    private ItemData CreateItem(string definitionId, string displayName, float basePrice)
    {
        ItemData item = Track(ScriptableObject.CreateInstance<ItemData>());
        item.id = definitionId;
        item.itemName = displayName;
        item.basePrice = basePrice;
        return item;
    }

    private CityRuntime CreateCity(string runtimeId, string definitionId, string locationRuntimeId)
    {
        CityData data = Track(ScriptableObject.CreateInstance<CityData>());
        data.id = definitionId;
        data.cityName = "Display City";
        data.initialPopulation = 100;
        return new CityRuntime(runtimeId, data, new SpatialLocationRuntime(locationRuntimeId));
    }

    private SpatialNetworkRuntime CreateNetwork(bool reverse)
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(registry);
        SpatialLocationRuntime first = new SpatialLocationRuntime("location-a");
        SpatialLocationRuntime second = new SpatialLocationRuntime("location-b");
        SpatialLocationRuntime third = new SpatialLocationRuntime("location-c");
        Assert.IsTrue(network.RegisterLocation(first));
        Assert.IsTrue(network.RegisterLocation(second));
        Assert.IsTrue(network.RegisterLocation(third));
        SpatialRouteRuntime firstRoute = new SpatialRouteRuntime("route-a", first, second, 2);
        SpatialRouteRuntime secondRoute = new SpatialRouteRuntime("route-b", second, third, 4);
        if (reverse == false)
        {
            Assert.IsTrue(network.RegisterRoute(firstRoute));
            Assert.IsTrue(network.RegisterRoute(secondRoute));
        }
        else
        {
            Assert.IsTrue(network.RegisterRoute(secondRoute));
            Assert.IsTrue(network.RegisterRoute(firstRoute));
        }

        return network;
    }

    private SpatialNetworkRuntime CreateNetworkWithRoute(SpatialRouteRuntime route)
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(registry);
        Assert.IsTrue(network.RegisterLocation(route.Origin));
        Assert.IsTrue(network.RegisterLocation(route.Destination));
        Assert.IsTrue(network.RegisterRoute(route));
        return network;
    }

    private NotableFixture CreateNotableFixture()
    {
        NotableFixture fixture = new NotableFixture();
        fixture.Registry = new RuntimeIdentityRegistry();
        fixture.City = CreateCity("city-1", "city-definition", "location-1");
        fixture.Npc = CreateNpc("npc-1", "npc-definition", "Display Name");
        Assert.IsTrue(fixture.Registry.RegisterCity(fixture.City));
        fixture.Store = new PlaceContentStore(new RuntimeIdAllocator(), fixture.Registry);
        fixture.Owner = PlaceContentOwnerReference.ForCity(fixture.City);
        ItemData item = CreateItem("notable-definition", "Notable", 10f);
        NotableItemRuntime notable = new NotableItemRuntime("notable-item-1", item);
        Assert.IsTrue(fixture.Store.TryAddNotable(fixture.Owner, notable, out string diagnostic), diagnostic);
        return fixture;
    }

    private ExpeditionFixture CreateExpeditionFixture()
    {
        ExpeditionFixture fixture = new ExpeditionFixture();
        fixture.Npcs = new[] { CreateNpc("npc-1", "npc-definition", "Display Name") };
        fixture.Expedition = CreateExpedition("expedition-activity-1", new[] { "npc-1" }, new[] { "npc-1" }, Array.Empty<string>());
        fixture.Store = new ExpeditionStore();
        Assert.IsTrue(fixture.Store.Add(fixture.Expedition));
        return fixture;
    }

    private ExpeditionRuntime CreateExpedition(
        string expeditionId,
        IEnumerable<string> members,
        IEnumerable<string> performers,
        IEnumerable<string> support)
    {
        return new ExpeditionRuntime(
            expeditionId,
            "site-1",
            "location-1",
            "location-2",
            "route-1",
            null,
            "decision-1",
            members,
            performers,
            support,
            ExpeditionState.AtSite,
            ExpeditionObjectiveRuntime.Explore());
    }

    private TopologyFixture CreateTopologyFixture(bool reversePlaces, bool reverseConnections)
    {
        TopologyFixture fixture = new TopologyFixture();
        fixture.Registry = new RuntimeIdentityRegistry();
        fixture.City = CreateCity("city-topology", "city-definition", "location-topology");
        Assert.IsTrue(fixture.Registry.RegisterLocation(fixture.City.Location));
        Assert.IsTrue(fixture.Registry.RegisterCity(fixture.City));
        LocalTopologyOwnerReference owner = new LocalTopologyOwnerReference(
            fixture.City.RuntimeId,
            LocalTopologyOwnerKind.City,
            fixture.City.Location.RuntimeId);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(owner, fixture.Registry);
        LocalPlaceRuntime root = new LocalPlaceRuntime("local-place-root");
        LocalPlaceRuntime placeA = new LocalPlaceRuntime("local-place-a");
        LocalPlaceRuntime placeB = new LocalPlaceRuntime("local-place-b");
        Assert.IsTrue(topology.AddPlace(root, isEntryPoint: true));
        if (reversePlaces == false)
        {
            Assert.IsTrue(topology.AddPlace(placeA, root));
            Assert.IsTrue(topology.AddPlace(placeB, root));
        }
        else
        {
            Assert.IsTrue(topology.AddPlace(placeB, root));
            Assert.IsTrue(topology.AddPlace(placeA, root));
        }

        LocalTopologyConnectionRuntime connectionA = new LocalTopologyConnectionRuntime("local-connection-a", root, placeA, 1f);
        LocalTopologyConnectionRuntime connectionB = new LocalTopologyConnectionRuntime("local-connection-b", placeA, placeB, 2f);
        if (reverseConnections == false)
        {
            Assert.IsTrue(topology.AddConnection(connectionA));
            Assert.IsTrue(topology.AddConnection(connectionB));
        }
        else
        {
            Assert.IsTrue(topology.AddConnection(connectionB));
            Assert.IsTrue(topology.AddConnection(connectionA));
        }

        fixture.Store = new LocalTopologyStore(fixture.Registry);
        Assert.IsTrue(fixture.Store.Add(topology));
        return fixture;
    }

    private WorldStateLocalTopologySnapshot CreateManualTopologySnapshot(bool includePlace, bool includeConnection)
    {
        List<WorldStateLocalPlaceSnapshot> places = new List<WorldStateLocalPlaceSnapshot>
        {
            new WorldStateLocalPlaceSnapshot("local-place-root", null, null, true)
        };
        if (includePlace)
        {
            places.Add(new WorldStateLocalPlaceSnapshot("local-place-a", null, "local-place-root", false));
        }

        List<WorldStateLocalConnectionSnapshot> connections = new List<WorldStateLocalConnectionSnapshot>();
        if (includeConnection)
        {
            connections.Add(new WorldStateLocalConnectionSnapshot(
                "local-connection-a", "local-place-root", "local-place-a", 1f, null));
        }

        return new WorldStateLocalTopologySnapshot(
            LocalTopologyOwnerKind.City,
            "city-1",
            "location-1",
            LocalTopologyPublicationState.Published,
            places,
            connections);
    }

    private WorldStateLocalPlaceSnapshot FindLocalPlace(WorldStateLocalTopologySnapshot topology, string runtimeId)
    {
        foreach (WorldStateLocalPlaceSnapshot place in topology.Places)
        {
            if (place.RuntimeId == runtimeId)
            {
                return place;
            }
        }

        Assert.Fail("Local place not found: " + runtimeId);
        return null;
    }

    private static bool ContainsDifference(WorldStateDiff diff, string section, string field)
    {
        foreach (WorldStateDifference difference in diff.Differences)
        {
            if (difference.Section == section && difference.Field == field)
            {
                return true;
            }
        }

        return false;
    }

    private T Track<T>(T value) where T : UnityEngine.Object
    {
        createdObjects.Add(value);
        return value;
    }

    private sealed class NotableFixture
    {
        public RuntimeIdentityRegistry Registry;
        public CityRuntime City;
        public NpcRuntime Npc;
        public PlaceContentStore Store;
        public PlaceContentOwnerReference Owner;
    }

    private sealed class ExpeditionFixture
    {
        public NpcRuntime[] Npcs;
        public ExpeditionRuntime Expedition;
        public ExpeditionStore Store;
    }

    private sealed class TopologyFixture
    {
        public RuntimeIdentityRegistry Registry;
        public CityRuntime City;
        public LocalTopologyStore Store;
    }
}
