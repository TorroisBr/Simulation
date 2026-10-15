using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1TypedDefinitionGraphTests
{
    [Test]
    public void TypedGraphCopiesSelectedValuesAndP9BuilderUsesThem()
    {
        string[] stageOrder = { "p9.genesis.resolve-profile/v1", "p9.genesis.authored-world/v1" };
        string[] configuredCities = { "CITY_A" };
        DailyV1SimulationConfigData configuration = CreateConfiguration(1f, configuredCities);
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            configuration, SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { new DailyV1CityData("CITY_A", "City A", null, null, null, null, 2, null, null, null, null, null) },
            Array.Empty<DailyV1ItemData>(), Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(), stageOrder,
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");

        configuredCities[0] = "MUTATED";
        stageOrder[0] = "MUTATED";
        string fingerprint = DailyV1DefinitionIdentity.CreateP9Fingerprint(definitions, out var records);

        Assert.That(definitions.Configuration.cities[0], Is.EqualTo("CITY_A"));
        Assert.That(definitions.StageOrder[0], Is.EqualTo("p9.genesis.resolve-profile/v1"));
        Assert.That(definitions.Cities.ContainsKey("CITY_A"), Is.True);
        Assert.That(records, Does.Contain("city|6:CITY_A|1:2|0:|0:|0:|0:"));
        Assert.That(fingerprint, Is.EqualTo(DailyV1DefinitionIdentity.CreateP9Fingerprint(records)));

        DailyV1EffectiveDefinitions changed = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            CreateConfiguration(2f, new[] { "CITY_A" }), SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null),
            new[] { new DailyV1CityData("CITY_A", "City A", null, null, null, null, 2, null, null, null, null, null) },
            Array.Empty<DailyV1ItemData>(), Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(),
            new[] { "p9.genesis.resolve-profile/v1", "p9.genesis.authored-world/v1" },
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");
        Assert.That(DailyV1DefinitionIdentity.CreateP9Fingerprint(changed, out _), Is.Not.EqualTo(fingerprint));
    }

    [Test]
    public void P9BuilderPreservesExplorableSiteIdentityAndKind()
    {
        DailyV1ExplorableSiteConfig[] sites =
        {
            new DailyV1ExplorableSiteConfig("SITE_A", "CITY_A", 2, new DailyV1EnumValue(1, "Ruin"))
        };
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            CreateConfiguration(1f, new[] { "CITY_A" }, sites), SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null), Array.Empty<DailyV1CityData>(),
            Array.Empty<DailyV1ItemData>(), Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(), Array.Empty<string>(),
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");

        DailyV1DefinitionIdentity.CreateP9Fingerprint(definitions, out var records);

        Assert.That(records, Does.Contain("site|6:SITE_A|6:CITY_A|1:2|4:Ruin"));
    }

    [Test]
    public void DefinitionMapsAreReadOnlyAndDuplicateIdentityIsRejected()
    {
        DailyV1CityData city = new DailyV1CityData("CITY_A", "City A", null, null, null, null,
            2, null, null, null, null, null);
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            CreateConfiguration(1f, new[] { "CITY_A" }), SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null), new[] { city }, Array.Empty<DailyV1ItemData>(),
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(), Array.Empty<DailyV1NpcActionData>(),
            Array.Empty<DailyV1NpcStatusData>(), Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s");

        Assert.That(definitions.Cities["CITY_A"].Values, Is.SameAs(city));
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, DailyV1CityDefinition>)definitions.Cities).Clear());
        Assert.Throws<ArgumentException>(() => DailyV1EffectiveDefinitions.CreateSelectedSchema(
            CreateConfiguration(1f, new[] { "CITY_A" }), SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null), new[] { city, city }, Array.Empty<DailyV1ItemData>(),
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(), Array.Empty<DailyV1NpcActionData>(),
            Array.Empty<DailyV1NpcStatusData>(), Array.Empty<string>(), "test.daily-v1", "test.geography-v1",
            "test.geography-stage/v1", "hex-v1", "q-r-s"));
    }

    private static DailyV1SimulationConfigData CreateConfiguration(float travelCost, string[] cities,
        DailyV1ExplorableSiteConfig[] explorableSites = null)
    {
        return new DailyV1SimulationConfigData(
            "Typed", new DailyV1CalendarDefinition(1, 1, 1, null), new[] { new DailyV1EnumValue(0, "Economy") },
            cities, explorableSites, null, null, null, null, null, null,
            false, null, 0, 0,
            null, null, null, null, null, null, null, null, null,
            "test.daily-v1", null,
            travelCost, false, 5, 0.7f, 0.25f, 3, false, null,
            false, 0, false, 0, null,
            false, 0d, false, 0d, 0d,
            null, null, null, null);
    }
}
