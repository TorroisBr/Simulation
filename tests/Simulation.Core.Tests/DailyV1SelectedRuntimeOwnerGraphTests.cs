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

    [Test]
    public void SelectedSoloTravelDayUsesExactSortedOwnersAndPreservesAdmissionAndPartyRules()
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

        List<DailyV1NpcRuntimeOwners> selected = graph.Npcs
            .OrderBy(owner => owner.RuntimeId, StringComparer.Ordinal)
            .Take(4)
            .ToList();
        DailyV1NpcRuntimeOwners startedToday = selected[0];
        DailyV1NpcRuntimeOwners rejectedAdvance = selected[1];
        DailyV1NpcRuntimeOwners arriving = selected[2];
        DailyV1NpcRuntimeOwners partyMember = selected[3];
        foreach (DailyV1NpcRuntimeOwners npc in selected)
        {
            DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
                string.Equals(city.RuntimeId, npc.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
            DailyV1CityRuntimeOwners destination = graph.Cities.Single(city => !ReferenceEquals(city, origin));
            Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
                npc, origin, origin.LocationRuntimeId,
                destination.LocationRuntimeId, destination,
                requestedTravelDays: 3,
                originDecisionId: "decision-" + npc.RuntimeId,
                routeId: "route-" + npc.RuntimeId,
                out DailyV1SelectedTravelTransitionPlan startPlan), Is.True);
            Assert.That(graph.TryCommitTravelTransition(npc, startPlan, () => true), Is.True);
        }

        DailyV1CityRuntimeOwners arrivingDestination = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, arriving.Travel.DestinationCityRuntimeId, StringComparison.Ordinal));
        Assert.That(startedToday.Travel.TravelStartedToday, Is.True);
        Assert.That(startedToday.Travel.TryInstallState(
            null, null, startedToday.Travel.DestinationLocationRuntimeId,
            startedToday.Travel.DestinationCityRuntimeId, 3, 3,
            startedToday.Travel.TravelRouteRuntimeId, true,
            startedToday.Travel.TravelOriginDecisionId, null, startedToday.Travel.Revision), Is.True);
        Assert.That(rejectedAdvance.Travel.TryInstallState(
            null, null, rejectedAdvance.Travel.DestinationLocationRuntimeId,
            rejectedAdvance.Travel.DestinationCityRuntimeId, 2, 3,
            rejectedAdvance.Travel.TravelRouteRuntimeId, false,
            rejectedAdvance.Travel.TravelOriginDecisionId, null, rejectedAdvance.Travel.Revision), Is.True);
        Assert.That(arriving.Travel.TryInstallState(
            null, null, arriving.Travel.DestinationLocationRuntimeId,
            arriving.Travel.DestinationCityRuntimeId, 1, 3,
            arriving.Travel.TravelRouteRuntimeId, false,
            arriving.Travel.TravelOriginDecisionId, null, arriving.Travel.Revision), Is.True);
        Assert.That(partyMember.Travel.TryInstallState(
            null, null, partyMember.Travel.DestinationLocationRuntimeId,
            partyMember.Travel.DestinationCityRuntimeId, 2, 3,
            partyMember.Travel.TravelRouteRuntimeId, false,
            partyMember.Travel.TravelOriginDecisionId, "party-1", partyMember.Travel.Revision), Is.True);

        long[] revisionsBeforeRejectedRoster = graph.Npcs.Select(npc => npc.Travel.Revision).ToArray();
        int preflightCommitCalls = 0;
        Assert.That(graph.TryAdvanceSoloTravelDay(
            graph.Npcs.Reverse(),
            _ => false,
            (_, _, _) => { preflightCommitCalls++; return true; },
            (_, _, _) => Assert.Fail("An inexact roster must not emit travel results.")), Is.False);
        Assert.That(preflightCommitCalls, Is.Zero);
        Assert.That(graph.Npcs.Select(npc => npc.Travel.Revision), Is.EqualTo(revisionsBeforeRejectedRoster));

        List<string> reported = new List<string>();
        Assert.That(graph.TryAdvanceSoloTravelDay(
            graph.Npcs,
            npc => npc.Travel.ActiveTravelPartyId != null,
            (npc, plan, _) => graph.TryCommitTravelTransition(
                npc, plan, () => !ReferenceEquals(npc, rejectedAdvance)),
            (npc, didArrive, originDecisionId) => reported.Add(
                npc.RuntimeId + ":" + didArrive + ":" + originDecisionId)), Is.True);

        Assert.That(startedToday.Travel.TravelDaysRemaining, Is.EqualTo(3),
            "Travel started today is cleared without consuming a travel day.");
        Assert.That(startedToday.Travel.TravelStartedToday, Is.False);
        Assert.That(startedToday.Travel.Revision, Is.EqualTo(2L));
        Assert.That(rejectedAdvance.Travel.TravelDaysRemaining, Is.EqualTo(2),
            "Rejected host admission leaves authoritative travel state unchanged.");
        Assert.That(rejectedAdvance.Travel.Revision, Is.EqualTo(1L));
        Assert.That(arriving.Travel.CurrentCityRuntimeId, Is.EqualTo(arrivingDestination.RuntimeId));
        Assert.That(arriving.Travel.CurrentLocationRuntimeId, Is.EqualTo(arrivingDestination.LocationRuntimeId));
        Assert.That(arriving.Travel.DestinationCityRuntimeId, Is.Null);
        Assert.That(arrivingDestination.NpcPresence.Contains(arriving.RuntimeId), Is.True);
        Assert.That(arriving.Travel.Revision, Is.EqualTo(2L));
        Assert.That(partyMember.Travel.TravelDaysRemaining, Is.EqualTo(2),
            "TravelParty membership keeps the owner out of solo advancement.");
        Assert.That(partyMember.Travel.Revision, Is.EqualTo(1L));

        Assert.That(reported, Is.EqualTo(new[]
        {
            rejectedAdvance.RuntimeId + ":False:" + rejectedAdvance.Travel.TravelOriginDecisionId,
            arriving.RuntimeId + ":True:" + "decision-" + arriving.RuntimeId
        }.OrderBy(value => value.Split(':')[0], StringComparer.Ordinal)));
    }
}
