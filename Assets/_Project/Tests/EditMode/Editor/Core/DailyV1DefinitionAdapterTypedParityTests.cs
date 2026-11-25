using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DailyV1DefinitionAdapterTypedParityTests
{
    private const string ProfilePath = "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset";

    [Test]
    public void UnityAdapterMatchesEveryFixedTypedDefinitionValue()
    {
        SimulationConfigData authoring = LoadOfficialProfile();
        DailyV1EffectiveDefinitions expected = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions actual = CreateUnityDefinitions(authoring);

        AssertDefinitionsEqual(expected, actual);
    }

    [Test]
    public void AdaptedTypedDefinitionsRemainCopiedWhenSourceAssetsAndListsChange()
    {
        SimulationConfigData authoring = LoadOfficialProfile();
        DailyV1EffectiveDefinitions expected = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions actual = CreateUnityDefinitions(authoring);

        CityData city = authoring.Cities[0];
        Assert.That(city, Is.Not.Null);
        Assert.That(city.productionConfigs, Is.Not.Null.And.Not.Empty);
        string originalCityName = city.cityName;
        int originalPopulation = city.initialPopulation;
        int originalProductionAmount = city.productionConfigs[0].amountPerDay;
        CityData originalFirstCityReference = authoring.cities[0];
        try
        {
            city.cityName = "mutation-probe";
            city.initialPopulation = unchecked(originalPopulation + 17);
            city.productionConfigs[0].amountPerDay = unchecked(originalProductionAmount + 19);
            authoring.cities[0] = authoring.cities[1];

            AssertDefinitionsEqual(expected, actual);
        }
        finally
        {
            authoring.cities[0] = originalFirstCityReference;
            city.cityName = originalCityName;
            city.initialPopulation = originalPopulation;
            city.productionConfigs[0].amountPerDay = originalProductionAmount;
        }
    }

    [Test]
    public void AdapterBindsAdmittedTraitAttributeAndSiteAssetsToCopiedTypedNodes()
    {
        SimulationConfigData authoring = LoadOfficialProfile();
        NpcData npc = authoring.Npcs[0].npc;
        ItemData item = authoring.Cities[0].marketItems[0].item;
        TraitData trait = ScriptableObject.CreateInstance<TraitData>();
        CapabilityAttributeData attribute = ScriptableObject.CreateInstance<CapabilityAttributeData>();
        ExplorableSiteData site = ScriptableObject.CreateInstance<ExplorableSiteData>();
        trait.id = "trait/adapter-binding";
        trait.traitName = " ";
        attribute.id = "attribute/adapter-binding";
        attribute.displayName = null;
        site.id = "site/adapter-binding";
        site.siteName = null;
        site.kind = ExplorableSiteKind.Ruin;

        List<TraitData> originalTraits = npc.traits;
        List<CapabilityAttributeValue> originalCapabilityValues = npc.capabilityValues;
        List<CapabilityAttributeModifier> originalItemModifiers = item.capabilityModifiers;
        List<ExplorableSiteConfig> originalSites = authoring.explorableSites;
        try
        {
            npc.traits = new List<TraitData> { trait };
            npc.capabilityValues = new List<CapabilityAttributeValue>
            {
                new CapabilityAttributeValue(attribute, 0.25f)
            };
            item.capabilityModifiers = new List<CapabilityAttributeModifier>
            {
                new CapabilityAttributeModifier(attribute, 0.5f)
            };
            authoring.explorableSites = new List<ExplorableSiteConfig>
            {
                new ExplorableSiteConfig { site = site, anchorCity = authoring.Cities[0], travelDaysFromAnchor = 2 }
            };

            DailyV1EffectiveDefinitions actual = CreateUnityDefinitions(authoring);

            Assert.That(actual.Traits.Count, Is.EqualTo(1));
            Assert.That(actual.Traits[trait.id].TypeToken, Is.EqualTo("TraitData"));
            Assert.That(actual.Traits[trait.id].DisplayName, Is.EqualTo(trait.id));
            Assert.That(actual.CapabilityAttributes.Count, Is.EqualTo(1));
            Assert.That(actual.CapabilityAttributes[attribute.id].TypeToken, Is.EqualTo("CapabilityAttributeData"));
            Assert.That(actual.CapabilityAttributes[attribute.id].DisplayName, Is.EqualTo(attribute.id));
            Assert.That(actual.ExplorableSites.Count, Is.EqualTo(1));
            DailyV1ExplorableSiteDefinition siteNode = actual.ExplorableSites[site.id];
            Assert.That(siteNode.TypeToken, Is.EqualTo("ExplorableSiteData"));
            Assert.That(siteNode.DisplayName, Is.Null);
            Assert.That(siteNode.Kind.Value, Is.EqualTo((int)ExplorableSiteKind.Ruin));
            Assert.That(siteNode.Kind.Spelling, Is.EqualTo(nameof(ExplorableSiteKind.Ruin)));
            Assert.That(actual.Npcs[npc.DefinitionId].Values.traits, Does.Contain(trait.id));
            Assert.That(actual.Npcs[npc.DefinitionId].Values.capabilityValues[0].attribute, Is.EqualTo(attribute.id));
            Assert.That(actual.Items[item.DefinitionId].Values.capabilityModifiers[0].attribute, Is.EqualTo(attribute.id));
            Assert.That(actual.Configuration.explorableSites[0].site, Is.EqualTo(site.id));
        }
        finally
        {
            npc.traits = originalTraits;
            npc.capabilityValues = originalCapabilityValues;
            item.capabilityModifiers = originalItemModifiers;
            authoring.explorableSites = originalSites;
            UnityEngine.Object.DestroyImmediate(trait);
            UnityEngine.Object.DestroyImmediate(attribute);
            UnityEngine.Object.DestroyImmediate(site);
        }
    }

    private static SimulationConfigData LoadOfficialProfile()
    {
        SimulationConfigData authoring = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(ProfilePath);
        Assert.That(authoring, Is.Not.Null, "The approved Daily-v1 profile asset must be present.");
        Assert.That(authoring.simulationName, Is.EqualTo("Daily v1"));
        return authoring;
    }

    private static DailyV1EffectiveDefinitions CreateUnityDefinitions(SimulationConfigData authoring)
    {
        SimulationConfigurationResolutionResult resolution = SimulationConfigurationResolver.Resolve(
            null, null, authoring.CreateConfigurationOverrides());
        Assert.That(resolution.IsValid, Is.True, string.Join("; ", resolution.Validation.Errors));
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.calendar, out string diagnostic);
        Assert.That(string.IsNullOrEmpty(diagnostic), Is.True, diagnostic);
        IReadOnlyList<string> stageOrder = SimulationGenesisPipeline.ResolveStageOrder(includeAuthoredGeography: true);

        Assert.That(DailyV1DefinitionAdapter.TryCreate(authoring, resolution.Configuration, calendar,
            stageOrder, "axial-hex-v1", "q-then-r", out DailyV1EffectiveDefinitions definitions,
            out string adapterDiagnostic), Is.True, adapterDiagnostic);
        return definitions;
    }

    private static void AssertDefinitionsEqual(DailyV1EffectiveDefinitions expected,
        DailyV1EffectiveDefinitions actual)
    {
        AssertEquivalent(expected.Configuration, actual.Configuration, "Configuration");
        AssertEquivalent(expected.EffectiveConfiguration, actual.EffectiveConfiguration, "EffectiveConfiguration");
        AssertEquivalent(expected.Calendar, actual.Calendar, "Calendar");
        AssertEquivalent(expected.Cities, actual.Cities, "Cities");
        AssertEquivalent(expected.Items, actual.Items, "Items");
        AssertEquivalent(expected.Npcs, actual.Npcs, "Npcs");
        AssertEquivalent(expected.Jobs, actual.Jobs, "Jobs");
        AssertEquivalent(expected.Actions, actual.Actions, "Actions");
        AssertEquivalent(expected.Statuses, actual.Statuses, "Statuses");
        AssertEquivalent(expected.Traits, actual.Traits, "Traits");
        AssertEquivalent(expected.CapabilityAttributes, actual.CapabilityAttributes, "CapabilityAttributes");
        AssertEquivalent(expected.ExplorableSites, actual.ExplorableSites, "ExplorableSites");
        AssertEquivalent(expected.StageOrder, actual.StageOrder, "StageOrder");
        AssertEquivalent(expected.ProfileContractIdentity, actual.ProfileContractIdentity, "ProfileContractIdentity");
        AssertEquivalent(expected.GeographyProfileContractIdentity, actual.GeographyProfileContractIdentity,
            "GeographyProfileContractIdentity");
        AssertEquivalent(expected.GeographyStageIdentity, actual.GeographyStageIdentity, "GeographyStageIdentity");
        AssertEquivalent(expected.HexCoordinateConvention, actual.HexCoordinateConvention, "HexCoordinateConvention");
        AssertEquivalent(expected.HexCoordinateCanonicalOrder, actual.HexCoordinateCanonicalOrder,
            "HexCoordinateCanonicalOrder");
    }

    private static void AssertEquivalent(object expected, object actual, string path)
    {
        if (ReferenceEquals(expected, actual)) return;
        Assert.That(actual, Is.Not.Null, path + " unexpectedly became null.");
        Assert.That(expected, Is.Not.Null, path + " unexpectedly became non-null.");
        Type type = expected.GetType();
        Assert.That(actual.GetType(), Is.EqualTo(type), path + " runtime type differs.");

        if (type == typeof(float))
        {
            Assert.That(BitConverter.ToInt32(BitConverter.GetBytes((float)actual), 0),
                Is.EqualTo(BitConverter.ToInt32(BitConverter.GetBytes((float)expected), 0)), path + " float bits differ.");
            return;
        }
        if (type == typeof(double))
        {
            Assert.That(BitConverter.ToInt64(BitConverter.GetBytes((double)actual), 0),
                Is.EqualTo(BitConverter.ToInt64(BitConverter.GetBytes((double)expected), 0)), path + " double bits differ.");
            return;
        }
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            Assert.That(actual, Is.EqualTo(expected), path + " differs.");
            return;
        }

        if (expected is IDictionary expectedDictionary)
        {
            IDictionary actualDictionary = actual as IDictionary;
            Assert.That(actualDictionary, Is.Not.Null, path + " dictionary shape differs.");
            Assert.That(actualDictionary.Count, Is.EqualTo(expectedDictionary.Count), path + " key count differs.");
            foreach (object key in expectedDictionary.Keys)
            {
                Assert.That(actualDictionary.Contains(key), Is.True, path + " is missing key " + key + ".");
                AssertEquivalent(expectedDictionary[key], actualDictionary[key], path + "[" + key + "]");
            }
            return;
        }

        if (expected is IEnumerable expectedSequence)
        {
            IEnumerable actualSequence = actual as IEnumerable;
            Assert.That(actualSequence, Is.Not.Null, path + " sequence shape differs.");
            IEnumerator left = expectedSequence.GetEnumerator();
            IEnumerator right = actualSequence.GetEnumerator();
            int index = 0;
            while (true)
            {
                bool hasLeft = left.MoveNext();
                bool hasRight = right.MoveNext();
                Assert.That(hasRight, Is.EqualTo(hasLeft), path + " sequence length differs at " + index + ".");
                if (!hasLeft) break;
                AssertEquivalent(left.Current, right.Current, path + "[" + index + "]");
                index++;
            }
            return;
        }

        PropertyInfo[] expectedProperties = ReadableProperties(type);
        PropertyInfo[] actualProperties = ReadableProperties(actual.GetType());
        CollectionAssert.AreEqual(expectedProperties.Select(property => property.Name).ToArray(),
            actualProperties.Select(property => property.Name).ToArray(), path + " typed property coverage differs.");
        foreach (PropertyInfo property in expectedProperties)
            AssertEquivalent(property.GetValue(expected, null), property.GetValue(actual, null), path + "." + property.Name);
    }

    private static PropertyInfo[] ReadableProperties(Type type) => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
        .OrderBy(property => property.Name, StringComparer.Ordinal)
        .ToArray();
}
