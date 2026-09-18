using System;
using NUnit.Framework;

public sealed class WorldStateDiagnosticsV2Tests
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
    public void CaptureIncludesCalendarPopulationPresenceAndNpcActivity()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-v2", "location-v2");
        NpcRuntime npc = new NpcRuntime("npc-v2", SimulationTestFactory.CreateNpc("npc-definition-v2"), city, 125f);
        Assert.IsTrue(SetResidence(city, npc));
        NpcStatusData status = SimulationTestFactory.CreateStatus("wanted");
        npc.AddStatus(status);
        ItemData item = SimulationTestFactory.CreateItem("iron", 4f);
        npc.Inventory.AddItem(item, 3, 2f);
        NpcActionData action = SimulationTestFactory.CreateAction("trade-action", NpcActionType.SellGoods, NpcActionCategory.Commerce);
        npc.SetCurrentActionRuntime(new NpcActionRuntime(action, city, item, 2, 4.5f));
        npc.SetMerchantTradePlan(item, city, city, 3, 2f, "decision-v2");

        WorldStateSnapshot snapshot = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            new SimulationTime(30L),
            new[] { npc },
            new[] { city },
            calendarDefinition: new CalendarDefinition(12, 4, 7)));

        Assert.AreEqual(1, snapshot.SettlementCount);
        Assert.AreEqual(1, snapshot.KnownNpcCount);
        Assert.IsNotNull(snapshot.Metadata.CalendarDate);
        Assert.AreEqual(30L, snapshot.Metadata.CalendarDate.AbsoluteDay);
        Assert.AreEqual(city.CityName, snapshot.Cities[0].CityName);
        Assert.AreEqual(city.Population.Revision, snapshot.Cities[0].PopulationRevision);
        Assert.AreEqual(1, snapshot.Cities[0].NamedResidentCount);
        Assert.AreEqual(1, snapshot.Cities[0].NamedPresentCount);
        Assert.AreEqual(npc.NpcName, snapshot.Npcs[0].Name);
        Assert.That(snapshot.Npcs[0].StatusNames, Is.EquivalentTo(new[] { "wanted" }));
        Assert.AreEqual("trade-action", snapshot.Npcs[0].CurrentAction.DefinitionId);
        Assert.AreEqual("iron", snapshot.Npcs[0].CurrentAction.TargetItemDefinitionId);
        Assert.AreEqual("decision-v2", snapshot.Npcs[0].MerchantTradePlan.OriginDecisionId);
        Assert.AreEqual(3, snapshot.Npcs[0].Inventory[0].Amount);
    }

    [Test]
    public void CaptureIsIndependentFromLaterRuntimeChanges()
    {
        ItemData item = SimulationTestFactory.CreateItem("iron", 4f);
        NpcRuntime npc = new NpcRuntime("npc-copy", SimulationTestFactory.CreateNpc("npc-copy-definition"));
        npc.Inventory.AddItem(item, 5, 2f);
        WorldStateSnapshot before = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            new SimulationTime(1L),
            new[] { npc }));

        npc.Inventory.RemoveItem(item, 2);
        npc.AddStatus(SimulationTestFactory.CreateStatus("after-capture"));

        Assert.AreEqual(5, before.Npcs[0].Inventory[0].Amount);
        Assert.IsEmpty(before.Npcs[0].StatusNames);
    }

    [Test]
    public void SnapshotConstructionSortsValueObjectCollections()
    {
        WorldStateNpcSnapshot npcB = new WorldStateNpcSnapshot(
            "npc-b", "def-b", "B", null, NpcLifeState.Alive, NpcInjurySeverity.None,
            null, null, null, null, false, null, 0, null, 0f, null,
            new[]
            {
                new WorldStateInventoryStackSnapshot("z-item", 1, 1f),
                new WorldStateInventoryStackSnapshot("a-item", 1, 1f)
            },
            new[] { "z-status", "a-status" });
        WorldStateNpcSnapshot npcA = new WorldStateNpcSnapshot(
            "npc-a", "def-a", "A", null, NpcLifeState.Alive, NpcInjurySeverity.None,
            null, null, null, null, false, null, 0, null, 0f, null, null);

        WorldStateSnapshot snapshot = new WorldStateSnapshot(0L, new[] { npcB, npcA });

        Assert.AreEqual("npc-a", snapshot.Npcs[0].RuntimeId);
        Assert.AreEqual("a-item", snapshot.Npcs[1].Inventory[0].ItemDefinitionId);
        Assert.AreEqual("a-status", snapshot.Npcs[1].StatusNames[0]);
    }

    [Test]
    public void DiffReportsActivityStatusTradePlanAndPopulationRevisionChanges()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-diff-v2", "location-diff-v2");
        NpcRuntime npc = new NpcRuntime("npc-diff-v2", SimulationTestFactory.CreateNpc("npc-diff-definition"), city, 10f);
        ItemData item = SimulationTestFactory.CreateItem("grain", 3f);
        WorldStateSnapshot before = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            new SimulationTime(2L), new[] { npc }, new[] { city }));

        npc.AddStatus(SimulationTestFactory.CreateStatus("busy"));
        npc.SetCurrentAction(SimulationTestFactory.CreateAction("walk", NpcActionType.Normal));
        npc.SetMerchantTradePlan(item, city, city, 4, 2f);
        Assert.IsTrue(SetResidence(city, npc));
        Assert.IsTrue(SetPopulation(city, new PopulationChangeSet(1, 0, 0, 0)));

        WorldStateDiff diff = WorldStateDiagnostics.Compare(
            before,
            WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
                new SimulationTime(2L), new[] { npc }, new[] { city })));

        Assert.IsTrue(Contains(diff, "NPC", "StatusNames"));
        Assert.IsTrue(Contains(diff, "NpcAction", "DefinitionId"));
        Assert.IsTrue(Contains(diff, "MerchantTradePlan", "ItemDefinitionId"));
        Assert.IsTrue(Contains(diff, "City", "PopulationRevision"));
        Assert.IsTrue(Contains(diff, "City", "NamedResidentCount"));
    }

    [Test]
    public void ValidatorReportsReadOnlySnapshotInconsistencies()
    {
        WorldStateCitySnapshot city = new WorldStateCitySnapshot(
            "city-valid", "city-definition", "location", -1, null, MarketLiquidityMode.Open, float.NaN, null, null,
            cityName: "City", populationRevision: -1L, namedResidentCount: 2, namedPresentCount: 0);
        WorldStateNpcSnapshot npc = new WorldStateNpcSnapshot(
            "npc-invalid", "npc-definition", null, "missing-city", NpcLifeState.Alive, NpcInjurySeverity.None,
            null, null, "destination", null, true, null, -1, null, float.PositiveInfinity, null,
            new[] { new WorldStateInventoryStackSnapshot("item", -2, float.NaN) });
        WorldStateInvariantReport report = WorldStateDiagnostics.Validate(new WorldStateSnapshot(
            0L, new[] { npc }, new[] { city }));

        Assert.IsTrue(report.HasErrors);
        Assert.IsTrue(ContainsIssue(report, "NegativePopulation"));
        Assert.IsTrue(ContainsIssue(report, "NamedResidentsExceedPopulation"));
        Assert.IsTrue(ContainsIssue(report, "ResidenceSettlementMissing"));
        Assert.IsTrue(ContainsIssue(report, "NegativeTravelDays"));
        Assert.IsTrue(ContainsIssue(report, "NonFiniteMoney"));
        Assert.IsTrue(ContainsIssue(report, "NegativeInventoryAmount"));
        Assert.IsTrue(ContainsIssue(report, "NonFiniteMarketBalance"));
    }

    [Test]
    public void ConsistentRuntimeSnapshotProducesNoInvariantErrors()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("city-consistent", "location-consistent");
        NpcRuntime npc = new NpcRuntime("npc-consistent", SimulationTestFactory.CreateNpc("npc-consistent-definition"), city, 5f);
        Assert.IsTrue(SetResidence(city, npc));

        WorldStateInvariantReport report = WorldStateDiagnostics.Validate(
            WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
                new SimulationTime(3L), new[] { npc }, new[] { city })));

        Assert.IsFalse(report.HasErrors);
    }

    [Test]
    public void DiffReportsTravelLifecycleMoneyAndLifeStateChanges()
    {
        CityRuntime origin = SimulationTestFactory.CreateCity("city-travel-origin", "location-travel-origin");
        CityRuntime destination = SimulationTestFactory.CreateCity("city-travel-destination", "location-travel-destination");
        NpcRuntime npc = new NpcRuntime("npc-travel-v2", SimulationTestFactory.CreateNpc("npc-travel-definition"), origin, 10f);
        WorldStateSnapshot before = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            new SimulationTime(1L), new[] { npc }, new[] { origin, destination }));

        Assert.IsTrue(npc.StartTravel(destination, 2));
        npc.AddMoney(5f);
        WorldStateSnapshot traveling = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            new SimulationTime(1L), new[] { npc }, new[] { origin, destination }));
        Assert.IsTrue(Contains(WorldStateDiagnostics.Compare(before, traveling), "NPC", "IsTraveling"));
        Assert.IsTrue(Contains(WorldStateDiagnostics.Compare(before, traveling), "NPC", "MoneyBalance"));

        Assert.IsFalse(npc.AdvanceTravelDay(out _));
        Assert.IsTrue(npc.AdvanceTravelDay(out _));
        Assert.IsTrue(npc.TryApplyDeath());
        WorldStateDiff ended = WorldStateDiagnostics.Compare(
            traveling,
            WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
                new SimulationTime(1L), new[] { npc }, new[] { origin, destination })));

        Assert.IsTrue(Contains(ended, "NPC", "IsTraveling"));
        Assert.IsTrue(Contains(ended, "NPC", "LifeState"));
    }

    [Test]
    public void FormatterAndExportAreDeterministicAndHumanReadable()
    {
        NpcRuntime npc = new NpcRuntime("npc-format", SimulationTestFactory.CreateNpc("npc-format-definition"));
        WorldStateSnapshot snapshot = WorldStateDiagnostics.Capture(new WorldStateSnapshotContext(
            new SimulationTime(7L), new[] { npc }));
        WorldStateDiff diff = WorldStateDiagnostics.Compare(snapshot, snapshot);

        string formatted = WorldStateDiagnostics.Format(snapshot);
        Assert.That(formatted, Does.Contain("WORLD DAY 7"));
        Assert.That(formatted, Does.Contain("NPC npc-format"));
        Assert.That(WorldStateDiagnostics.Format(diff), Does.Contain("No changes."));
        Assert.AreEqual(WorldStateDiagnostics.Export(snapshot), WorldStateDiagnostics.Export(snapshot));
    }

    private static bool SetResidence(CityRuntime city, NpcRuntime npc)
    {
        return SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc }),
            out PopulationMembershipFailure failure)
            && failure == PopulationMembershipFailure.None;
    }

    private static bool SetPopulation(CityRuntime city, PopulationChangeSet changes)
    {
        if (SettlementPopulationSystem.TryPropose(city.Population, changes, out SettlementPopulationTransition transition, out PopulationTransitionFailure failure) == false
            || failure != PopulationTransitionFailure.None)
        {
            return false;
        }

        return SettlementPopulationSystem.TryApply(city.Population, transition, out failure)
            && failure == PopulationTransitionFailure.None;
    }

    private static bool Contains(WorldStateDiff diff, string section, string field)
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

    private static bool ContainsIssue(WorldStateInvariantReport report, string code)
    {
        foreach (WorldStateInvariantIssue issue in report.Issues)
        {
            if (issue.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
