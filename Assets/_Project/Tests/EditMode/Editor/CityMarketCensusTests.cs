using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CityMarketCensusTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
        {
            if (simulationObject != null)
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void SelectedProfilePublishesExactPerCityMarketStockWitnesses()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.useAuthoredGeographyProfile, Is.True);

        GameObject simulationObject = new GameObject("selected-profile-market-census-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        simulation.Start();

        CityRuntime[] cities = simulation.Bootstrap.Runtime.Cities
            .OrderBy(city => city.RuntimeId, System.StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            simulation.Bootstrap.CityMarketCensusProviders;
        Assert.That(cities, Has.Length.EqualTo(2));
        Assert.That(providers, Has.Count.EqualTo(cities.Length));

        for (int i = 0; i < cities.Length; i++)
        {
            CityRuntime city = cities[i];
            OwnerSectionCensusWitness witness = providers[i].GetCurrentCensus();
            string expectedSectionId = CityMarketCensusProvider.SectionIdPrefix
                + city.RuntimeId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ":" + city.RuntimeId;

            Assert.That(witness.SectionId, Is.EqualTo(expectedSectionId));
            Assert.That(witness.SchemaVersion, Is.EqualTo(CityMarketCensusProvider.SchemaVersion));
            Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(city.Market));
            Assert.That(witness.Cardinality, Is.EqualTo(5));
            Assert.That(witness.Revision, Is.Zero);

            OwnerSectionCensusWitness repeated = providers[i].GetCurrentCensus();
            Assert.That(repeated.OwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity));
            Assert.That(repeated.Cardinality, Is.EqualTo(witness.Cardinality));
            Assert.That(repeated.Revision, Is.EqualTo(witness.Revision));
        }

        MarketRuntime market = cities[0].Market;
        ItemData existingItem = market.Items[0].Item;
        long initialRevision = market.Revision;
        Assert.That(market.AddStock(existingItem, 1), Is.EqualTo(1));
        OwnerSectionCensusWitness sameRowUpdate = providers[0].GetCurrentCensus();
        Assert.That(sameRowUpdate.OwnerInstanceIdentity, Is.SameAs(market));
        Assert.That(sameRowUpdate.Cardinality, Is.EqualTo(5));
        Assert.That(sameRowUpdate.Revision, Is.EqualTo(initialRevision + 1));

        ItemData addedItem = SimulationTestFactory.CreateItem("market-census-new-row");
        Assert.That(market.AddStock(addedItem, 2), Is.EqualTo(2));
        OwnerSectionCensusWitness newRow = providers[0].GetCurrentCensus();
        Assert.That(newRow.OwnerInstanceIdentity, Is.SameAs(market));
        Assert.That(newRow.Cardinality, Is.EqualTo(6));
        Assert.That(newRow.Revision, Is.EqualTo(initialRevision + 2));
    }

    [Test]
    public void MarketPriceRefresh_DoesNotMutateWhenRevisionIsExhausted()
    {
        ItemData item = SimulationTestFactory.CreateItem("market-price-revision-exhausted", 10f);
        CityRuntime city = SimulationTestFactory.CreateCity(
            "market-price-revision-exhausted-city",
            "market-price-revision-exhausted-location",
            SimulationTestFactory.CreateMarketItem(item, 10, 10));
        MarketRuntime market = city.Market;
        float existingPrice = market.GetPrice(item);
        typeof(MarketRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(market, long.MaxValue);
        item.basePrice = 20f;

        market.UpdatePrices();

        Assert.That(market.GetPrice(item), Is.EqualTo(existingPrice),
            "a derived price change cannot commit without a new local revision");
        Assert.That(market.Revision, Is.EqualTo(long.MaxValue));

        item.basePrice = 10f;
        market.UpdatePrices();
        Assert.That(market.GetPrice(item), Is.EqualTo(existingPrice),
            "a price refresh that would not change stored values remains a no-op");
        Assert.That(market.Revision, Is.EqualTo(long.MaxValue));
    }
}
