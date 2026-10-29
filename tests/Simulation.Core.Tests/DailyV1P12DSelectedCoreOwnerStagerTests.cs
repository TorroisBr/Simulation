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
    public void StagesPairedPersistedRowsThroughCoreWithExactDefinitionsAndRevisions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1P12DCityCoreOwnerHydrationInput city = CreateCityInput(definitions, "npc/one");
        DailyV1P12DNpcCoreOwnerHydrationInput seed = CreateNpcInput(
            definitions, "npc/one", city.CityRuntimeId, city.ResolvedLocationRuntimeId,
            inventoryRevision: 3L, moneyRevision: 4L, lifeStateRevision: 5L,
            residenceRevision: 6L, crimeJusticeRevision: 7L, actionRevision: 8L);
        P12DNpcFRow fRow = CreateNpcFRow(
            "npc/one", travelRevision: 13L, merchantPlanRevision: 14L,
            travelPlanRevision: 15L, siteRevision: 16L, spatialRevision: 17L,
            commercialRevision: 18L, adventureRevision: 19L);

        Assert.That(DailyV1P12DSelectedCoreOwnerStager.TryCreateFromRows(
            definitions, new[] { city }, new[] { seed.DRow }, new[] { fRow },
            CreateSpatial(city.ResolvedLocationRuntimeId), Array.Empty<string>(),
            out DailyV1P12DSelectedCoreOwnerStage staged,
            out DailyV1P12DSelectedCoreOwnerStageFailure failure), Is.True, failure.ToString());

        DailyV1CityRuntimeOwners cityOwner = staged.Cities.Single();
        DailyV1NpcRuntimeOwners npcOwner = staged.Npcs.Single();
        Assert.That(cityOwner.Definitions, Is.SameAs(definitions));
        Assert.That(cityOwner.Definition,
            Is.SameAs(definitions.Cities[cityOwner.Definition.DefinitionId]));
        Assert.That(npcOwner.Definitions, Is.SameAs(definitions));
        Assert.That(npcOwner.Definition,
            Is.SameAs(definitions.Npcs[seed.DRow.NpcDefinitionId]));
        Assert.That(npcOwner.RuntimeId, Is.EqualTo(seed.DRow.RuntimeId));
        Assert.That(npcOwner.Inventory.Revision, Is.EqualTo(seed.DRow.InventoryRevision));
        Assert.That(npcOwner.MoneyAccount.Revision, Is.EqualTo(seed.DRow.MoneyRevision));
        Assert.That(npcOwner.Lifecycle.LifeStateRevision, Is.EqualTo(seed.DRow.LifeStateRevision));
        Assert.That(npcOwner.Residence.Revision, Is.EqualTo(seed.DRow.ResidenceRevision));
        Assert.That(npcOwner.CrimeJusticeRevision, Is.EqualTo(seed.DRow.CrimeJusticeRevision));
        Assert.That(npcOwner.ActionSlot.Revision, Is.EqualTo(seed.DRow.CurrentActionRevision));
        Assert.That(npcOwner.Travel.Revision, Is.EqualTo(fRow.TravelStateRevision));
        Assert.That(npcOwner.MerchantTradePlan.Revision, Is.EqualTo(fRow.MerchantPlan.Revision));
        Assert.That(npcOwner.TravelPlan.Revision, Is.EqualTo(fRow.TravelPlan.Revision));
        Assert.That(npcOwner.ExplorableSiteKnowledge.Revision, Is.EqualTo(fRow.SiteKnowledgeRevision));
        Assert.That(npcOwner.SpatialKnowledge.Revision, Is.EqualTo(fRow.SpatialKnowledgeRevision));
        Assert.That(npcOwner.CommercialKnowledge.Revision, Is.EqualTo(fRow.CommercialKnowledgeRevision));
        Assert.That(npcOwner.AdventureSiteIntelKnowledge.Revision,
            Is.EqualTo(fRow.AdventureKnowledgeRevision));
        Assert.That(cityOwner.NpcPresence.OrderedNpcRuntimeIds, Is.EqualTo(new[] { npcOwner.RuntimeId }));
        Assert.That(npcOwner.Travel.CurrentCityRuntimeId, Is.EqualTo(cityOwner.RuntimeId));
        Assert.That(npcOwner.Travel.CurrentLocationRuntimeId, Is.EqualTo(cityOwner.LocationRuntimeId));
    }

    [Test]
    public void StagesImageSpatialAndSelectedOwnersInDecodedOrderWithExactRevisions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1P12DCityCoreOwnerHydrationInput city = CreateCityInput(
            definitions, "npc/two", "npc/one");
        DailyV1P12DNpcCoreOwnerHydrationInput npcTwo = CreateNpcInput(
            definitions, "npc/two", city.CityRuntimeId, city.ResolvedLocationRuntimeId,
            inventoryRevision: 3L, moneyRevision: 4L, lifeStateRevision: 5L);
        DailyV1P12DNpcCoreOwnerHydrationInput npcOne = CreateNpcInput(
            definitions, "npc/one", city.CityRuntimeId, city.ResolvedLocationRuntimeId,
            inventoryRevision: 7L, moneyRevision: 8L, lifeStateRevision: 9L);
        P12DNpcFRow npcTwoF = CreateNpcFRow("npc/two", travelRevision: 13L);
        P12DNpcFRow npcOneF = CreateNpcFRow("npc/one", travelRevision: 17L);
        SpatialNetworkOwnerSnapshot spatial = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion,
            7L,
            new[] { "location/other", city.ResolvedLocationRuntimeId },
            new[]
            {
                new SpatialRouteOwnerSnapshotRecord(
                    "route/staged", "location/other", city.ResolvedLocationRuntimeId, 3)
            });
        P12CRuntimeIdentityOwnerSnapshot identities = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion,
            9L,
            new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("npc", "npc/one"),
                new System.Collections.Generic.KeyValuePair<string, string>("npc", "npc/two"),
                new System.Collections.Generic.KeyValuePair<string, string>("city", city.CityRuntimeId),
                new System.Collections.Generic.KeyValuePair<string, string>("location", "location/other"),
                new System.Collections.Generic.KeyValuePair<string, string>("location", city.ResolvedLocationRuntimeId),
                new System.Collections.Generic.KeyValuePair<string, string>("route", "route/staged")
            });
        P12FTravelPartyOwnerSnapshot parties = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, 0L,
            Array.Empty<P12FTravelPartyOwnerSnapshotRecord>());

        Assert.That(DailyV1P12DSelectedCoreOwnerStager.TryCreateFromImageValues(
            definitions, spatial, identities, new[] { city },
            new[] { npcTwo.DRow, npcOne.DRow }, new[] { npcTwoF, npcOneF }, parties,
            out DailyV1P12DCoreRestoreOwnerStage staged,
            out DailyV1P12DSelectedCoreOwnerStageFailure failure), Is.True, failure.ToString());

        Assert.That(staged.SpatialNetwork.Revision, Is.EqualTo(spatial.Revision));
        Assert.That(staged.SpatialNetwork.RouteCount, Is.EqualTo(1));
        Assert.That(staged.SpatialNetwork.Routes[0].RuntimeId, Is.EqualTo("route/staged"));
        Assert.That(staged.RuntimeIdentityIndex.CensusRevision, Is.EqualTo(identities.Revision));
        Assert.That(staged.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Npc), Is.EqualTo(2));
        Assert.That(staged.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.City), Is.EqualTo(1));
        Assert.That(staged.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Location), Is.EqualTo(2));
        Assert.That(staged.RuntimeIdentityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Route), Is.EqualTo(1));
        Assert.That(staged.Cities.Select(owner => owner.RuntimeId), Is.EqualTo(new[] { city.CityRuntimeId }));
        Assert.That(staged.Npcs.Select(owner => owner.RuntimeId), Is.EqualTo(new[] { "npc/two", "npc/one" }));
        Assert.That(staged.Cities[0].NpcPresence.OrderedNpcRuntimeIds,
            Is.EqualTo(new[] { "npc/two", "npc/one" }));
        Assert.That(staged.Npcs[0].Inventory.Revision, Is.EqualTo(3L));
        Assert.That(staged.Npcs[1].Inventory.Revision, Is.EqualTo(7L));
        Assert.That(staged.Npcs[0].Travel.Revision, Is.EqualTo(13L));
        Assert.That(staged.Npcs[1].Travel.Revision, Is.EqualTo(17L));
        Assert.That(staged.Cities.All(owner => ReferenceEquals(owner.Definitions, definitions)), Is.True);
        Assert.That(staged.Npcs.All(owner => ReferenceEquals(owner.Definitions, definitions)), Is.True);
    }

    [Test]
    public void RejectsImageIdentityIndexCollisionWithoutReturningPartialOwnerGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1P12DCityCoreOwnerHydrationInput city = CreateCityInput(definitions, "npc/one");
        DailyV1P12DNpcCoreOwnerHydrationInput npc = CreateNpcInput(
            definitions, "npc/one", city.CityRuntimeId, city.ResolvedLocationRuntimeId);
        SpatialNetworkOwnerSnapshot spatial = new SpatialNetworkOwnerSnapshot(
            SpatialNetworkOwnerSnapshot.CurrentSchemaVersion, 1L,
            new[] { city.ResolvedLocationRuntimeId }, Array.Empty<SpatialRouteOwnerSnapshotRecord>());
        P12CRuntimeIdentityOwnerSnapshot identities = new P12CRuntimeIdentityOwnerSnapshot(
            P12CRuntimeIdentityOwnerSnapshot.CurrentSchemaVersion, 4L,
            new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("npc", "npc/one"),
                new System.Collections.Generic.KeyValuePair<string, string>("city", city.CityRuntimeId),
                new System.Collections.Generic.KeyValuePair<string, string>("location", city.ResolvedLocationRuntimeId),
                new System.Collections.Generic.KeyValuePair<string, string>("route", "npc/one")
            });
        P12FTravelPartyOwnerSnapshot parties = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, 0L,
            Array.Empty<P12FTravelPartyOwnerSnapshotRecord>());

        Assert.That(DailyV1P12DSelectedCoreOwnerStager.TryCreateFromImageValues(
            definitions, spatial, identities, new[] { city }, new[] { npc.DRow },
            new[] { CreateNpcFRow("npc/one") }, parties,
            out DailyV1P12DCoreRestoreOwnerStage staged,
            out DailyV1P12DSelectedCoreOwnerStageFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DSelectedCoreOwnerStageFailure.InvalidInput));
    }
    [Test]
    public void RejectsMismatchedPersistedNpcRowsWithoutReturningPartialOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1P12DCityCoreOwnerHydrationInput city = CreateCityInput(definitions, "npc/one");
        DailyV1P12DNpcCoreOwnerHydrationInput seed = CreateNpcInput(
            definitions, "npc/one", city.CityRuntimeId, city.ResolvedLocationRuntimeId);

        Assert.That(DailyV1P12DSelectedCoreOwnerStager.TryCreateFromRows(
            definitions, new[] { city }, new[] { seed.DRow }, new[] { CreateNpcFRow("npc/two") },
            CreateSpatial(city.ResolvedLocationRuntimeId), Array.Empty<string>(),
            out DailyV1P12DSelectedCoreOwnerStage staged,
            out DailyV1P12DSelectedCoreOwnerStageFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DSelectedCoreOwnerStageFailure.InvalidNpcIdentity));
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
        string locationId,
        long inventoryRevision = 0L,
        long moneyRevision = 0L,
        long lifeStateRevision = 0L,
        long residenceRevision = 0L,
        long crimeJusticeRevision = 0L,
        long actionRevision = 0L)
    {
        DailyV1NpcDefinition definition = definitions.Npcs.Values.First();
        P12DNpcDRow row = new P12DNpcDRow(runtimeId, definition.DefinitionId,
            null, null, NpcLifeState.Alive, NpcInjurySeverity.None,
            Array.Empty<string>(), 0, null, cityId, locationId, null, null,
            Array.Empty<P12DNpcInventoryRowValue>(), inventoryRevision, 0f,
            moneyRevision, lifeStateRevision, residenceRevision,
            crimeJusticeRevision, actionRevision);
        return new DailyV1P12DNpcCoreOwnerHydrationInput(
            row, null, 0, 0, null, false, null, null, 0L,
            new P12DNpcMerchantPlanValue(null, null, null, 0, 0, 0f, 0, 0, null, 0L),
            new P12DNpcTravelPlanValue(null, null, NpcTravelReason.None, 0f, 0f, null, 0L),
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(),
            Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<DailyV1CommercialKnowledgeShareReceipt>(), 0L,
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<AdventureOppositionObservation>(),
            Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(),
            Array.Empty<AdventureAccessObservation>(), 0L);
    }

    private static P12DNpcFRow CreateNpcFRow(
        string runtimeId,
        long travelRevision = 0L,
        long merchantPlanRevision = 0L,
        long travelPlanRevision = 0L,
        long siteRevision = 0L,
        long spatialRevision = 0L,
        long commercialRevision = 0L,
        long adventureRevision = 0L) =>
        new P12DNpcFRow(
            runtimeId, null, 0, 0, null, false, null, null, travelRevision,
            new P12DNpcMerchantPlanValue(null, null, null, 0, 0, 0f, 0, 0, null, merchantPlanRevision),
            new P12DNpcTravelPlanValue(null, null, NpcTravelReason.None, 0f, 0f, null, travelPlanRevision),
            Array.Empty<ExplorableSiteKnowledgeObservation>(), siteRevision,
            Array.Empty<string>(), Array.Empty<string>(), spatialRevision,
            Array.Empty<AdventureOppositionObservation>(),
            Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(),
            Array.Empty<AdventureAccessObservation>(), adventureRevision,
            Array.Empty<P12DNpcCommercialMarketValue>(),
            Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), commercialRevision);
}
