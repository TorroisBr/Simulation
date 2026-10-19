using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedRuntimeOwnerGraphTests
{
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

        Assert.That(DailyV1SelectedTravelTransitionPlan.TryStartTravel(
            npc, origin, origin.LocationRuntimeId, destination.LocationRuntimeId, destination,
            requestedTravelDays: 2, originDecisionId: "decision-core-host", routeId: "route-core-host",
            out DailyV1SelectedTravelTransitionPlan plan), Is.True);

        int admissionCalls = 0;
        Assert.That(plan.TryCommit(() =>
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
