using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1MixedSourceProfileAdmissionTests
{
    [Test]
    public void AcceptsOneOrderedExogenousAndFiniteSourceForTheSameItem()
    {
        Assert.That(DailyV1MixedSourceProfileAdmission.TryValidate(CreateValidCity(), out string rejection), Is.True, rejection);
        Assert.That(rejection, Is.Empty);
    }

    [TestCase("missing-profile", "MissingMixedSourcesCity")]
    [TestCase("missing-identity", "MixedSourcesCityIdentityIncomplete")]
    [TestCase("missing-source", "MixedSourcesProfileRequiresExactlyTwoSources")]
    [TestCase("duplicate-source-id", "MixedSourcesSourceIdentityOrQuantityInvalid")]
    [TestCase("different-items", "MixedSourcesItemMismatch")]
    [TestCase("unspecified-kind", "MixedSourcesExplicitKindRequired")]
    [TestCase("paid-consumption", "MixedSourcesRequiresMatchingSingleMarketItemAndFreeConsumption")]
    public void RejectsInvalidTypedSourceContractWithExistingReason(string variant, string expectedReason)
    {
        DailyV1CityData city = CreateValidCity();
        if (variant == "missing-profile")
            city = new DailyV1CityData(city.id, city.cityName, city.settlementSemanticId,
                city.materialFlowLocationId, city.marketStoreSemanticId,
                new DailyV1EnumValue((int)LocalMaterialFlowProfile.ExogenousDaily, "ExogenousDaily"),
                city.initialPopulation, city.marketLiquidity, city.populationConsumption,
                city.marketItems, city.productionConfigs, city.connections);
        else if (variant == "missing-identity")
            city = new DailyV1CityData(city.id, city.cityName, null,
                city.materialFlowLocationId, city.marketStoreSemanticId, city.materialFlowProfile,
                city.initialPopulation, city.marketLiquidity, city.populationConsumption,
                city.marketItems, city.productionConfigs, city.connections);
        else if (variant == "missing-source")
            city = new DailyV1CityData(city.id, city.cityName, city.settlementSemanticId,
                city.materialFlowLocationId, city.marketStoreSemanticId, city.materialFlowProfile,
                city.initialPopulation, city.marketLiquidity, city.populationConsumption,
                city.marketItems, new[] { city.productionConfigs[0] }, city.connections);
        else if (variant == "duplicate-source-id")
            city = CreateCityWithSources(
                Source("a-exogenous", "item/a", CityProductionSourceKind.ExogenousDaily, 0),
                Source("a-exogenous", "item/a", CityProductionSourceKind.FiniteReserveDaily, 7));
        else if (variant == "different-items")
            city = CreateCityWithSources(
                Source("a-exogenous", "item/a", CityProductionSourceKind.ExogenousDaily, 0),
                Source("b-finite", "item/b", CityProductionSourceKind.FiniteReserveDaily, 7));
        else if (variant == "unspecified-kind")
            city = CreateCityWithSources(
                Source("a-exogenous", "item/a", CityProductionSourceKind.Unspecified, 0),
                Source("b-finite", "item/a", CityProductionSourceKind.FiniteReserveDaily, 7));
        else if (variant == "paid-consumption")
            city = new DailyV1CityData(city.id, city.cityName, city.settlementSemanticId,
                city.materialFlowLocationId, city.marketStoreSemanticId, city.materialFlowProfile,
                city.initialPopulation, city.marketLiquidity,
                new DailyV1PopulationConsumptionConfig(
                    new DailyV1EnumValue((int)ConsumptionPaymentMode.AccountBacked, "AccountBacked"), 0f),
                city.marketItems, city.productionConfigs, city.connections);

        Assert.That(DailyV1MixedSourceProfileAdmission.TryValidate(city, out string rejection), Is.False);
        Assert.That(rejection, Is.EqualTo(expectedReason));
    }

    private static DailyV1CityData CreateValidCity() => CreateCityWithSources(
        Source("a-exogenous", "item/a", CityProductionSourceKind.ExogenousDaily, 0),
        Source("b-finite", "item/a", CityProductionSourceKind.FiniteReserveDaily, 7));

    private static DailyV1CityData CreateCityWithSources(
        DailyV1CityProductionConfig exogenous, DailyV1CityProductionConfig finite)
    {
        return new DailyV1CityData("city/a", "City A", "settlement/a", "location/a", "market/a",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.MixedSourcesDaily, "MixedSourcesDaily"),
            1000, null,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig("item/a", 5, 100, 4f) },
            new[] { exogenous, finite }, new List<DailyV1CityConnection>());
    }

    private static DailyV1CityProductionConfig Source(string sourceId, string itemId,
        CityProductionSourceKind kind, int reserve) =>
        new DailyV1CityProductionConfig(itemId, 2, reserve, sourceId, "content-v1",
            new DailyV1EnumValue((int)kind, kind.ToString()));
}
