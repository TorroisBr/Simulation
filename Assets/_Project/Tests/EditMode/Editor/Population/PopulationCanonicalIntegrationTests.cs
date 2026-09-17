using System.Reflection;
using NUnit.Framework;

public sealed class PopulationCanonicalIntegrationTests
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
    public void CityPopulationUsesSettlementPopulationRuntime()
    {
        CityRuntime city = CreateCity("city-population-runtime", 1200);

        Assert.That(city.Population, Is.Not.Null);
        Assert.That(city.Population.SettlementRuntimeId, Is.EqualTo(city.RuntimeId));
    }

    [Test]
    public void CityCurrentPopulationMatchesPopulationRuntime()
    {
        CityRuntime city = CreateCity("city-population-match", 1200);

        Assert.That(city.CurrentPopulation, Is.EqualTo(city.Population.CurrentPopulation));
    }

    [Test]
    public void InitialPopulationComesFromCityData()
    {
        CityRuntime city = CreateCity("city-population-initial", 734);

        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(734));
        Assert.That(city.CurrentPopulation, Is.EqualTo(734));
    }

    [Test]
    public void CityHasSingleMutablePopulationTruth()
    {
        FieldInfo legacyField = typeof(CityRuntime).GetField(
            "currentPopulation",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(legacyField, Is.Null);
        Assert.That(typeof(CityRuntime).GetProperty("Population"), Is.Not.Null);
    }

    [Test]
    public void PopulationChangeUpdatesCityCurrentPopulation()
    {
        CityRuntime city = CreateCity("city-population-change", 1000);
        Assert.That(SettlementPopulationSystem.TryPropose(
            city.Population,
            new PopulationChangeSet(100, 0, 0, 0),
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure proposalFailure), Is.True);
        Assert.That(proposalFailure, Is.EqualTo(PopulationTransitionFailure.None));

        Assert.That(SettlementPopulationSystem.TryApply(
            city.Population,
            transition,
            out PopulationTransitionFailure applyFailure), Is.True);
        Assert.That(applyFailure, Is.EqualTo(PopulationTransitionFailure.None));
        Assert.That(city.CurrentPopulation, Is.EqualTo(1100));
        Assert.That(city.CurrentPopulation, Is.EqualTo(city.Population.CurrentPopulation));
    }

    [Test]
    public void ConsumptionUsesCanonicalPopulation()
    {
        ItemData item = SimulationTestFactory.CreateItem("population-canonical-consumption", 1f);
        CityData cityData = SimulationTestFactory.CreateCityData(
            "population-canonical-consumption-city",
            SimulationTestFactory.CreateMarketItem(item, 10, 10));
        cityData.initialPopulation = 1000;
        cityData.marketItems[0].consumptionPer1000Population = 2f;
        CityRuntime city = new CityRuntime(
            "city-population-canonical-consumption",
            cityData,
            new SpatialLocationRuntime("location-population-canonical-consumption"));

        Assert.That(SettlementPopulationSystem.TryPropose(
            city.Population,
            new PopulationChangeSet(1000, 0, 0, 0),
            out SettlementPopulationTransition transition,
            out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryApply(city.Population, transition, out _), Is.True);

        CityConsumptionResult result = city.SimulateConsumptionDay()[0];

        Assert.That(result.RequestedQuantity, Is.EqualTo(4));
        Assert.That(result.ConsumedQuantity, Is.EqualTo(4));
    }

    [Test]
    public void ImmigrationTerminologyIsExplicit()
    {
        Assert.That(typeof(PopulationChangeSet).GetProperty("Immigrations"), Is.Not.Null);
        Assert.That(typeof(SettlementPopulationTransition).GetProperty("Immigrations"), Is.Not.Null);
        Assert.That(typeof(PopulationChangeSet).GetProperty("Arrivals"), Is.Null);
        Assert.That(typeof(SettlementPopulationTransition).GetProperty("Arrivals"), Is.Null);
    }

    [Test]
    public void EmigrationTerminologyIsExplicit()
    {
        Assert.That(typeof(PopulationChangeSet).GetProperty("Emigrations"), Is.Not.Null);
        Assert.That(typeof(SettlementPopulationTransition).GetProperty("Emigrations"), Is.Not.Null);
        Assert.That(typeof(PopulationChangeSet).GetProperty("Departures"), Is.Null);
        Assert.That(typeof(SettlementPopulationTransition).GetProperty("Departures"), Is.Null);
    }

    [Test]
    public void TravelArrivalTerminologyDoesNotAffectPopulationChangeSet()
    {
        PopulationChangeSet travelArrival = new PopulationChangeSet(0, 0, 0, 0);

        Assert.That(travelArrival.NetChange, Is.EqualTo(0L));
        Assert.That(travelArrival.Immigrations, Is.EqualTo(0));
        Assert.That(travelArrival.Emigrations, Is.EqualTo(0));
    }

    private static CityRuntime CreateCity(string runtimeId, int initialPopulation)
    {
        CityData cityData = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        cityData.initialPopulation = initialPopulation;
        return new CityRuntime(
            runtimeId,
            cityData,
            new SpatialLocationRuntime("location-" + runtimeId));
    }
}
