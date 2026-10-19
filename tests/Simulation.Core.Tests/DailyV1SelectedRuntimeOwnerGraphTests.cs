using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedRuntimeOwnerGraphTests
{
    [Test]
    public void SelectedDailyEconomyPreflightsExactCityOwnersAndPreservesGlobalPassOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        long[] originalRevisions = graph.Cities.Select(city => city.Market.Revision).ToArray();
        int[][] originalStock = graph.Cities.Select(city => city.Market.Items.Select(row => row.Amount).ToArray()).ToArray();
        List<string> events = new List<string>();

        Assert.That(graph.TrySimulateDailyEconomyDay(
            graph.Cities.Reverse(), 1, "calendar-v1",
            (owner, category, _) => events.Add(owner.Definition.DefinitionId + ":" + category),
            (_, _) => Assert.Fail("A rejected owner sequence must not project material flow."),
            (_, _, _) => throw new AssertionException("The admitted fixture uses Free population consumption.")), Is.False);
        Assert.That(events, Is.Empty, "A mismatched City order is rejected before any writes or callbacks.");
        Assert.That(graph.Cities.Select(city => city.Market.Revision), Is.EqualTo(originalRevisions));
        Assert.That(graph.Cities.Select(city => city.Market.Items.Select(row => row.Amount).ToArray()),
            Is.EqualTo(originalStock));

        Assert.That(graph.TrySimulateDailyEconomyDay(
            graph.Cities, 1, "calendar-v1",
            (owner, category, _) => events.Add(owner.Definition.DefinitionId + ":" + category),
            (_, _) => { },
            (_, _, _) => throw new AssertionException("The admitted fixture uses Free population consumption.")), Is.True);

        string[] productionEvents = events.Where(value => value.EndsWith(":" + SimulationLogCategory.EconomyProduction,
            StringComparison.Ordinal)).ToArray();
        string[] consumptionEvents = events.Where(value => value.EndsWith(":" + SimulationLogCategory.EconomyConsumption,
            StringComparison.Ordinal)).ToArray();
        string[] marketEvents = events.Where(value => value.EndsWith(":" + SimulationLogCategory.Market,
            StringComparison.Ordinal)).ToArray();
        Assert.That(productionEvents, Is.Not.Empty);
        Assert.That(marketEvents, Has.Length.EqualTo(graph.Cities.Count));
        Assert.That(marketEvents.Select(value => value.Split(':')[0]),
            Is.EqualTo(graph.Cities.Select(city => city.Definition.DefinitionId)));

        int firstConsumption = events.FindIndex(value => value.EndsWith(":" + SimulationLogCategory.EconomyConsumption,
            StringComparison.Ordinal));
        int firstMarket = events.FindIndex(value => value.EndsWith(":" + SimulationLogCategory.Market,
            StringComparison.Ordinal));
        int lastProduction = events.FindLastIndex(value => value.EndsWith(":" + SimulationLogCategory.EconomyProduction,
            StringComparison.Ordinal));
        Assert.That(lastProduction, Is.LessThan(firstMarket), "All production remains ahead of the final City pass.");
        if (consumptionEvents.Length > 0)
            Assert.That(firstConsumption, Is.GreaterThan(lastProduction),
                "All City production completes before any City consumes.");
    }

    [Test]
    public void AdmittedDefinitionsConstructAndOperatePairedCityNpcOwnersInCore()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(
            allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Definitions, Is.SameAs(definitions));
        Assert.That(graph.Cities.Count, Is.EqualTo(2));
        Assert.That(graph.Npcs.Count, Is.EqualTo(10));

        DailyV1NpcRuntimeOwners npc = graph.Npcs.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "JORGE", StringComparison.Ordinal));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "CAMPO_VERDE", StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(owner =>
            string.Equals(owner.Definition.DefinitionId, "SERRA_DE_FERRO", StringComparison.Ordinal));

        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.EqualTo(origin.LocationRuntimeId));
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True);
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(npc.Definitions, Is.SameAs(graph.Definitions));
        Assert.That(origin.Definitions, Is.SameAs(graph.Definitions));
        Assert.That(destination.Definitions, Is.SameAs(graph.Definitions));

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc-outside-graph", definitions, npc.Definition, 0f,
            out DailyV1NpcRuntimeOwners outsideNpc), Is.True);
        Assert.That(outsideNpc.Travel.TryInstallState(
            origin.LocationRuntimeId, origin.RuntimeId,
            null, null, 0, 0, null, false, null, null, 0L), Is.True);
        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            outsideNpc, origin, origin.LocationRuntimeId,
            destination.LocationRuntimeId, destination,
            requestedTravelDays: 2, originDecisionId: "unregistered", routeId: "route-core-host",
            out DailyV1SelectedTravelTransitionPlan outsidePlan), Is.True);
        int rejectedAdmissionCalls = 0;
        Assert.That(graph.TryCommitTravelTransition(outsideNpc, outsidePlan, () =>
        {
            rejectedAdmissionCalls++;
            return true;
        }), Is.False);
        Assert.That(rejectedAdmissionCalls, Is.Zero,
            "A prepared plan for an NPC outside this runtime is rejected before host admission.");
        Assert.That(outsideNpc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(outsideNpc.Travel.DestinationCityRuntimeId, Is.Null);
        Assert.That(outsideNpc.Travel.Revision, Is.Zero);

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, origin.LocationRuntimeId, destination.LocationRuntimeId, destination,
            requestedTravelDays: 2, originDecisionId: "decision-core-host", routeId: "route-core-host",
            out DailyV1SelectedTravelTransitionPlan plan), Is.True);

        int admissionCalls = 0;
        Assert.That(graph.TryCommitTravelTransition(npc, plan, () =>
        {
            admissionCalls++;
            Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.True,
                "Admission runs before the paired City and NPC owner write.");
            Assert.That(npc.Travel.CurrentCityRuntimeId, Is.EqualTo(origin.RuntimeId));
            return true;
        }), Is.True);

        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(origin.NpcPresence.Contains(npc.RuntimeId), Is.False);
        Assert.That(destination.NpcPresence.Contains(npc.RuntimeId), Is.False,
            "Travel start removes current presence without prematurely adding destination presence.");
        Assert.That(npc.Travel.CurrentCityRuntimeId, Is.Null);
        Assert.That(npc.Travel.CurrentLocationRuntimeId, Is.Null);
        Assert.That(npc.Travel.DestinationCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(npc.Travel.DestinationLocationRuntimeId, Is.EqualTo(destination.LocationRuntimeId));
        Assert.That(npc.Travel.TravelDaysRemaining, Is.EqualTo(2));
        Assert.That(npc.Travel.Revision, Is.EqualTo(1L));
    }
}
