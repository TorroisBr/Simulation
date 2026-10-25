using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12DSelectedCoreOwnerStagerTests
{
    [Test]
    public void StagesExactSelectedCityAndNpcOwnersBeforeHostProjection()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1P12DCityCoreOwnerHydrationInput city = CreateCityInput(definitions, "npc/one");
        DailyV1P12DNpcCoreOwnerHydrationInput npc = CreateNpcInput(
            definitions, "npc/one", city.CityRuntimeId, city.ResolvedLocationRuntimeId);

        Assert.That(DailyV1P12DSelectedCoreOwnerStager.TryCreate(
            definitions, new[] { city }, new[] { npc }, CreateSpatial(city.ResolvedLocationRuntimeId),
            Array.Empty<string>(), out DailyV1P12DSelectedCoreOwnerStage staged,
            out DailyV1P12DSelectedCoreOwnerStageFailure failure), Is.True, failure.ToString());

        Assert.That(staged.Cities, Has.Count.EqualTo(1));
        Assert.That(staged.Npcs, Has.Count.EqualTo(1));
        Assert.That(staged.Cities[0].Definitions, Is.SameAs(definitions));
        Assert.That(staged.Npcs[0].Definitions, Is.SameAs(definitions));
        Assert.That(staged.Cities[0].RuntimeId, Is.EqualTo(city.CityRuntimeId));
        Assert.That(staged.Npcs[0].RuntimeId, Is.EqualTo("npc/one"));
        Assert.That(staged.Cities[0].NpcPresence.OrderedNpcRuntimeIds, Is.EqualTo(new[] { "npc/one" }));
        Assert.That(staged.Npcs[0].Travel.CurrentCityRuntimeId, Is.EqualTo(city.CityRuntimeId));
        Assert.That(staged.Npcs[0].Travel.CurrentLocationRuntimeId, Is.EqualTo(city.ResolvedLocationRuntimeId));
        Assert.That(staged.Cities[0].Market.Items.All(row => definitions.Items.Values.Contains(row.Item)), Is.True);
    }

    [Test]
    public void RejectsNonreciprocalMembershipWithoutReturningPartialOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1P12DCityCoreOwnerHydrationInput city = CreateCityInput(definitions, "npc/one");
        DailyV1P12DNpcCoreOwnerHydrationInput npc = CreateNpcInput(definitions, "npc/one", null, null);

        Assert.That(DailyV1P12DSelectedCoreOwnerStager.TryCreate(
            definitions, new[] { city }, new[] { npc }, CreateSpatial(city.ResolvedLocationRuntimeId),
            Array.Empty<string>(), out DailyV1P12DSelectedCoreOwnerStage staged,
            out DailyV1P12DSelectedCoreOwnerStageFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DSelectedCoreOwnerStageFailure.InvalidCityMembership));
    }

    private static SpatialNetworkOwner CreateSpatial(string locationId)
    {
        SpatialNetworkOwner spatial = new SpatialNetworkOwner();
        SpatialLocationRuntime location = new SpatialLocationRuntime(locationId);
        Assert.That(spatial.TryInstallRestoredLocation(location, () => true), Is.True);
        return spatial;
    }

    private static DailyV1P12DCityCoreOwnerHydrationInput CreateCityInput(
        DailyV1EffectiveDefinitions definitions,
        params string[] members)
    {
        DailyV1CityDefinition definition = definitions.Cities.Values.First();
        const string cityId = "city/staged";
        const string locationId = "location/staged";
        return new DailyV1P12DCityCoreOwnerHydrationInput(
            cityId, definition.DefinitionId, locationId, locationId, 2L, members,
            cityId, MarketLiquidityMode.Open, false, 3L, Array.Empty<P12DCityMarketItemSnapshot>(),
            "population-city/staged", ConsumptionPaymentMode.Free, false,
            cityId, 1200, 4L, 0L, Array.Empty<PopulationOperationReceiptSnapshot>());
    }

    private static DailyV1P12DNpcCoreOwnerHydrationInput CreateNpcInput(
        DailyV1EffectiveDefinitions definitions,
        string runtimeId,
        string cityId,
        string locationId)
    {
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        P12DNpcDRow row = new P12DNpcDRow(runtimeId, definition.DefinitionId,
            null, null, NpcLifeState.Alive, NpcInjurySeverity.None,
            Array.Empty<string>(), 0, null, cityId, locationId, null, null,
            Array.Empty<P12DNpcInventoryRowValue>(), 0L, 0f, 0L, 0L, 0L, 0L, 0L);
        return new DailyV1P12DNpcCoreOwnerHydrationInput(
            row, null, 0, 0, null, false, null, null, 0L,
            new P12DNpcMerchantPlanValue(null, null, null, 0, 0, 0f, 0, 0, null, 0L),
            new P12DNpcTravelPlanValue(null, null, NpcTravelReason.None, 0f, 0f, null, 0L),
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(),
            Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<DailyV1CommercialKnowledgeShareReceipt>(), 0L);
    }
}
