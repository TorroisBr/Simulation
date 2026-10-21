using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1TypedDefinitionGraphTests
{
    [Test]
    public void SelectedConfiguredActionsComeFromCanonicalGraphInConfigurationOrder()
    {
        DailyV1NpcActionData firstValues = CreateAction("action/first", 0, 1f);
        DailyV1NpcActionData disabledValues = CreateAction("action/disabled-crime", 5, 2f);
        DailyV1NpcActionData secondValues = CreateAction("action/second", 0, 3f);
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            CreateConfiguration(1f, Array.Empty<string>(), actions: new[]
            {
                firstValues.id, " ", disabledValues.id, secondValues.id, firstValues.id
            }),
            SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null),
            Array.Empty<DailyV1CityData>(), Array.Empty<DailyV1ItemData>(),
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            new[] { firstValues, disabledValues, secondValues },
            Array.Empty<DailyV1NpcStatusData>(), Array.Empty<string>(),
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");

        Assert.That(DailyV1SelectedNpcActionDecisionStage.TryResolveConfiguredActions(
            definitions, definitions.Configuration.actions,
            out IReadOnlyList<DailyV1ActionDefinition> configured), Is.True);
        Assert.That(configured.Select(action => action.DefinitionId),
            Is.EqualTo(new[] { firstValues.id, disabledValues.id, secondValues.id, firstValues.id }));
        Assert.That(configured[0], Is.SameAs(definitions.Actions[firstValues.id]));
        Assert.That(configured[3], Is.SameAs(configured[0]),
            "Duplicate configured entries retain their original weighted-choice cardinality.");
    }

    [Test]
    public void SelectedConfiguredActionsRejectAnUnresolvedIdentity()
    {
        DailyV1NpcActionData admitted = CreateAction("action/admitted", 0, 1f);
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            CreateConfiguration(1f, Array.Empty<string>(), actions: new[] { admitted.id, "action/missing" }),
            SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null),
            Array.Empty<DailyV1CityData>(), Array.Empty<DailyV1ItemData>(),
            Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            new[] { admitted }, Array.Empty<DailyV1NpcStatusData>(), Array.Empty<string>(),
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");

        Assert.That(DailyV1SelectedNpcActionDecisionStage.TryResolveConfiguredActions(
            definitions, definitions.Configuration.actions,
            out IReadOnlyList<DailyV1ActionDefinition> configured), Is.False);
        Assert.That(configured, Is.Null);
    }

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

    [Test]
    public void OptionalDefinitionMapsFreezeCanonicalNodesAndPreserveThemWithContentProjection()
    {
        KeyValuePair<string, string>[] traits =
        {
            new KeyValuePair<string, string>("trait/friendly", "Friendly")
        };
        KeyValuePair<string, string>[] attributes =
        {
            new KeyValuePair<string, string>("attribute/strength", "Strength")
        };
        DailyV1ExplorableSiteData[] sites =
        {
            new DailyV1ExplorableSiteData("site/ruin", null, new DailyV1EnumValue(1, "Ruin"))
        };
        DailyV1EffectiveDefinitions definitions = CreateDefinitions(traits, attributes, sites);
        DailyV1TraitDefinition trait = definitions.Traits["trait/friendly"];
        DailyV1CapabilityAttributeDefinition attribute = definitions.CapabilityAttributes["attribute/strength"];
        DailyV1ExplorableSiteDefinition site = definitions.ExplorableSites["site/ruin"];

        Assert.That(trait.TypeToken, Is.EqualTo("TraitData"));
        Assert.That(trait.DisplayName, Is.EqualTo("Friendly"));
        Assert.That(attribute.TypeToken, Is.EqualTo("CapabilityAttributeData"));
        Assert.That(attribute.DisplayName, Is.EqualTo("Strength"));
        Assert.That(site.TypeToken, Is.EqualTo("ExplorableSiteData"));
        Assert.That(site.Values, Is.SameAs(sites[0]));
        Assert.That(site.DisplayName, Is.Null);
        Assert.That(site.Kind.Value, Is.EqualTo(1));
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, DailyV1TraitDefinition>)definitions.Traits).Clear());

        DailyV1CapturedSelectedContentFixture.Create(out DailyV1SelectedContentAsset profile,
            out DailyV1SelectedContentAsset[] assets);
        Assert.That(DailyV1SelectedContentProjectionFactory.TryCreate(assets, profile,
            out DailyV1SelectedContentProjection projection, out string diagnostic), Is.True, diagnostic);
        DailyV1EffectiveDefinitions projected = definitions.WithContentProjection(projection);
        Assert.That(projected.Traits["trait/friendly"], Is.SameAs(trait));
        Assert.That(projected.CapabilityAttributes["attribute/strength"], Is.SameAs(attribute));
        Assert.That(projected.ExplorableSites["site/ruin"], Is.SameAs(site));
    }

    [Test]
    public void OptionalDefinitionFamiliesRejectDuplicateKeysAndKeepTheirOwnIdentitySpaces()
    {
        string sharedId = "same/id";
        DailyV1EffectiveDefinitions definitions = CreateDefinitions(
            new[] { new KeyValuePair<string, string>(sharedId, "Trait") },
            new[] { new KeyValuePair<string, string>(sharedId, "Attribute") }, null);
        Assert.That(definitions.Traits.ContainsKey(sharedId), Is.True);
        Assert.That(definitions.CapabilityAttributes.ContainsKey(sharedId), Is.True);

        Assert.Throws<ArgumentException>(() => CreateDefinitions(new[]
        {
            new KeyValuePair<string, string>(sharedId, "Trait A"),
            new KeyValuePair<string, string>(sharedId, "Trait B")
        }, null, null));
        Assert.Throws<ArgumentException>(() => CreateDefinitions(null, new[]
        {
            new KeyValuePair<string, string>(sharedId, "Attribute A"),
            new KeyValuePair<string, string>(sharedId, "Attribute B")
        }, null));
        Assert.Throws<ArgumentException>(() => CreateDefinitions(null, null, new[]
        {
            new DailyV1ExplorableSiteData(sharedId, "Site A", new DailyV1EnumValue(1, "Ruin")),
            new DailyV1ExplorableSiteData(sharedId, "Site B", new DailyV1EnumValue(1, "Ruin"))
        }));
    }

    [Test]
    public void TraitAndCapabilityAttributeDisplayNamesUseExistingIdFallback()
    {
        DailyV1EffectiveDefinitions definitions = CreateDefinitions(
            new[] { new KeyValuePair<string, string>("trait/no-name", " ") },
            new[] { new KeyValuePair<string, string>("attribute/no-name", null) }, null);
        Assert.That(definitions.Traits["trait/no-name"].DisplayName, Is.EqualTo("trait/no-name"));
        Assert.That(definitions.CapabilityAttributes["attribute/no-name"].DisplayName, Is.EqualTo("attribute/no-name"));
    }

    [Test]
    public void OptionalLocalDescriptorsPreserveRawValuesWithoutOwningValidation()
    {
        DailyV1LocalPlaceTypeDefinition place = new DailyV1LocalPlaceTypeDefinition(null, " ");
        DailyV1LocalConnectionTypeDefinition connection = new DailyV1LocalConnectionTypeDefinition("", null);
        Assert.That(place.DefinitionId, Is.Null);
        Assert.That(place.DisplayName, Is.EqualTo(" "));
        Assert.That(connection.DefinitionId, Is.Empty);
        Assert.That(connection.DisplayName, Is.Null);
    }

    internal static DailyV1SimulationConfigData CreateConfiguration(float travelCost, string[] cities,
        DailyV1ExplorableSiteConfig[] explorableSites = null, string[] actions = null)
    {
        return new DailyV1SimulationConfigData(
            "Typed", new DailyV1CalendarDefinition(1, 1, 1, null), new[] { new DailyV1EnumValue(0, "Economy") },
            cities, explorableSites, null, actions, null, null, null, null,
            false, null, 0, 0,
            null, null, null, null, null, null, null, null, null,
            "test.daily-v1", null,
            travelCost, false, 5, 0.7f, 0.25f, 3, false, null,
            false, 0, false, 0, null,
            false, 0d, false, 0d, 0d,
            null, null, null, null);
    }

    private static DailyV1NpcActionData CreateAction(string id, int actionType, float baseUtility) =>
        new DailyV1NpcActionData(id, null, id, id,
            new DailyV1EnumValue(0, "Action"), new DailyV1EnumValue(actionType, "Type"),
            baseUtility, null, false, 0f, null, null, null, null, null);

    private static DailyV1EffectiveDefinitions CreateDefinitions(
        IEnumerable<KeyValuePair<string, string>> traits,
        IEnumerable<KeyValuePair<string, string>> attributes,
        IEnumerable<DailyV1ExplorableSiteData> sites)
    {
        return DailyV1EffectiveDefinitions.CreateSelectedSchema(
            CreateConfiguration(1f, Array.Empty<string>()), SimulationConfigurationDefaults.Create(),
            new DailyV1CalendarDefinition(1, 1, 1, null), Array.Empty<DailyV1CityData>(),
            Array.Empty<DailyV1ItemData>(), Array.Empty<DailyV1NpcData>(), Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(), Array.Empty<DailyV1NpcStatusData>(), Array.Empty<string>(),
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s",
            traits, attributes, sites);
    }
}
