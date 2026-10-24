using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12DSelectedOwnerGraphStagerTests
{
    [Test]
    public void RestoredGraphUsesExactOwnersAndDeterministicIdentityOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph source), Is.True);
        PersonStore persons = new PersonStore();
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(8L));
        ExpeditionStore expeditions = new ExpeditionStore();

        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            source.Cities.Reverse().ToArray(),
            source.Npcs.Reverse().ToArray(),
            source.SpatialNetwork,
            source.JusticeRecordsOwner,
            new TravelPartyStore(),
            source.RuntimeIdentityIndex,
            persons,
            directives,
            expeditions,
            out DailyV1SelectedRuntimeOwnerGraph restored,
            out DailyV1P12DSelectedOwnerGraphFailure failure), Is.True, failure.ToString());

        Assert.That(restored.Definitions, Is.SameAs(definitions));
        Assert.That(restored.SpatialNetwork, Is.SameAs(source.SpatialNetwork));
        Assert.That(restored.RuntimeIdentityIndex, Is.SameAs(source.RuntimeIdentityIndex));
        Assert.That(restored.PersonStore, Is.SameAs(persons));
        Assert.That(restored.ScheduledDirectives, Is.SameAs(directives));
        Assert.That(restored.Expeditions, Is.SameAs(expeditions));
        Assert.That(restored.Cities.Select(owner => owner.RuntimeId).ToArray(),
            Is.EqualTo(source.Cities.Select(owner => owner.RuntimeId)
                .OrderBy(runtimeId => runtimeId, System.StringComparer.Ordinal).ToArray()));
        Assert.That(restored.Npcs.Select(owner => owner.RuntimeId).ToArray(),
            Is.EqualTo(source.Npcs.Select(owner => owner.RuntimeId)
                .OrderBy(runtimeId => runtimeId, System.StringComparer.Ordinal).ToArray()));
        foreach (DailyV1CityRuntimeOwners owner in source.Cities)
            Assert.That(restored.ContainsExactCityOwner(owner), Is.True);
        foreach (DailyV1NpcRuntimeOwners owner in source.Npcs)
            Assert.That(restored.ContainsExactNpcOwner(owner), Is.True);
    }

    [Test]
    public void RejectsDuplicateOrForeignOwnerWithoutReturningPartialGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph source), Is.True);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(8L));
        ExpeditionStore expeditions = new ExpeditionStore();
        TravelPartyStore parties = new TravelPartyStore();

        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            source.Cities.Concat(new[] { source.Cities[0] }).ToArray(),
            source.Npcs,
            source.SpatialNetwork,
            source.JusticeRecordsOwner,
            parties,
            source.RuntimeIdentityIndex,
            new PersonStore(),
            directives,
            expeditions,
            out DailyV1SelectedRuntimeOwnerGraph duplicateGraph,
            out DailyV1P12DSelectedOwnerGraphFailure duplicateFailure), Is.False);
        Assert.That(duplicateGraph, Is.Null);
        Assert.That(duplicateFailure, Is.EqualTo(DailyV1P12DSelectedOwnerGraphFailure.InvalidCityOwner));

        DailyV1EffectiveDefinitions foreignDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "foreign-npc", foreignDefinitions, foreignDefinitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners foreignNpc), Is.True);
        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            source.Cities,
            source.Npcs.Concat(new[] { foreignNpc }).ToArray(),
            source.SpatialNetwork,
            source.JusticeRecordsOwner,
            parties,
            source.RuntimeIdentityIndex,
            new PersonStore(),
            directives,
            expeditions,
            out DailyV1SelectedRuntimeOwnerGraph foreignGraph,
            out DailyV1P12DSelectedOwnerGraphFailure foreignFailure), Is.False);
        Assert.That(foreignGraph, Is.Null);
        Assert.That(foreignFailure, Is.EqualTo(DailyV1P12DSelectedOwnerGraphFailure.InvalidNpcOwner));
    }
}
