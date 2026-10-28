using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12DCityCoreOwnerHydratorTests
{
    [Test]
    public void P12DRowsHydrateCityOwnersFromCanonicalDefinitionsWithExactOrderAndRevisions()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1ItemDefinition[] items = definitions.Items.Values.Take(2).ToArray();
        Assert.That(items, Has.Length.EqualTo(2), "The fixture must include two admitted items to prove row order.");
        PopulationOperationReceiptSnapshot receipt = new PopulationOperationReceiptSnapshot(
            "population-op/1",
            "population-fingerprint/1",
            PopulationOwnerBridge.CreateDecodedTransition(
                "city/p12d-hydrator", 2L, 1190,
                new PopulationChangeSet(10, 0, 0, 0), 10L, 1200));
        DailyV1P12DCityCoreOwnerHydrationInput input = CreateInput(
            city.DefinitionId,
            new[]
            {
                new P12DCityMarketItemSnapshot(items[1].DefinitionId, 17, 60, 4.25f),
                new P12DCityMarketItemSnapshot(items[0].DefinitionId, 8, 40, 2.5f)
            },
            new[] { receipt });

        Assert.That(DailyV1P12DCityCoreOwnerHydrator.TryCreate(input, definitions,
            out DailyV1CityRuntimeOwners owners,
            out DailyV1P12DCityCoreOwnerHydrationFailure failure), Is.True, failure.ToString());

        Assert.That(owners.Definitions, Is.SameAs(definitions));
        Assert.That(owners.Definition, Is.SameAs(city));
        Assert.That(owners.RuntimeId, Is.EqualTo("city/p12d-hydrator"));
        Assert.That(owners.LocationRuntimeId, Is.EqualTo("location/p12d-hydrator"));
        Assert.That(owners.Population.CurrentPopulation, Is.EqualTo(1200));
        Assert.That(owners.Population.Revision, Is.EqualTo(3L));
        Assert.That(owners.Population.TryCaptureOperationReceiptSnapshot(
            out var restoredReceipts, out long restoredReceiptRevision), Is.True);
        Assert.That(restoredReceipts, Has.Count.EqualTo(1));
        Assert.That(restoredReceipts[0].Identity, Is.EqualTo(receipt.Identity));
        Assert.That(restoredReceipts[0].Fingerprint, Is.EqualTo(receipt.Fingerprint));
        Assert.That(restoredReceipts[0].Transition, Is.EqualTo(receipt.Transition));
        Assert.That(restoredReceiptRevision, Is.EqualTo(7L));
        Assert.That(owners.Market.Revision, Is.EqualTo(11L));
        Assert.That(owners.Market.Items.Select(row => row.Item), Is.EqualTo(items.Reverse()));
        Assert.That(owners.Market.Items.Select(row => row.Amount), Is.EqualTo(new[] { 17, 8 }));
        Assert.That(owners.Market.Items.Select(row => row.DesiredAmount), Is.EqualTo(new[] { 60, 40 }));
        Assert.That(owners.Market.Items.Select(row => row.CurrentPrice), Is.EqualTo(new[] { 4.25f, 2.5f }));
        Assert.That(owners.Counterparty.CounterpartyRuntimeId, Is.EqualTo("city/p12d-hydrator"));
        Assert.That(owners.Counterparty.LiquidityMode, Is.EqualTo(MarketLiquidityMode.Open));
        Assert.That(owners.Counterparty.MoneyAccount, Is.Null);
        Assert.That(owners.PopulationEconomy.PopulationEconomicRuntimeId,
            Is.EqualTo("population-city/p12d-hydrator"));
        Assert.That(owners.NpcPresence.OrderedNpcRuntimeIds,
            Is.EqualTo(new[] { "npc/second", "npc/first" }));
        Assert.That(owners.NpcPresence.Revision, Is.EqualTo(9L));
        Assert.That(failure, Is.EqualTo(DailyV1P12DCityCoreOwnerHydrationFailure.None));
    }

    [Test]
    public void P12DCityCoreHydratorRejectsUnresolvedCityWithoutReturningOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1P12DCityCoreOwnerHydrationInput input = CreateInput(
            "missing-city-definition", Array.Empty<P12DCityMarketItemSnapshot>(),
            Array.Empty<PopulationOperationReceiptSnapshot>());

        Assert.That(DailyV1P12DCityCoreOwnerHydrator.TryCreate(input, definitions,
            out DailyV1CityRuntimeOwners owners,
            out DailyV1P12DCityCoreOwnerHydrationFailure failure), Is.False);

        Assert.That(owners, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DCityCoreOwnerHydrationFailure.MissingCityDefinition));
    }

    [Test]
    public void P12DCityCoreHydratorRejectsUnresolvedMarketItemWithoutReturningOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1P12DCityCoreOwnerHydrationInput input = CreateInput(
            city.DefinitionId,
            new[] { new P12DCityMarketItemSnapshot("missing-item", 1, 1, 1f) },
            Array.Empty<PopulationOperationReceiptSnapshot>());

        Assert.That(DailyV1P12DCityCoreOwnerHydrator.TryCreate(input, definitions,
            out DailyV1CityRuntimeOwners owners,
            out DailyV1P12DCityCoreOwnerHydrationFailure failure), Is.False);

        Assert.That(owners, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DCityCoreOwnerHydrationFailure.MissingItemDefinition));
    }

    [TestCase(-1, 1, 1f)]
    [TestCase(1, 0, 1f)]
    [TestCase(1, 1, 0f)]
    [TestCase(1, 1, float.NaN)]
    [TestCase(1, 1, float.PositiveInfinity)]
    public void P12DCityCoreHydratorRejectsInvalidMarketRowsWithoutReturningOwners(
        int amount, int desiredAmount, float currentPrice)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1ItemDefinition item = definitions.Items.Values.First();
        DailyV1P12DCityCoreOwnerHydrationInput input = CreateInput(
            city.DefinitionId,
            new[] { new P12DCityMarketItemSnapshot(item.DefinitionId, amount, desiredAmount, currentPrice) },
            Array.Empty<PopulationOperationReceiptSnapshot>());

        Assert.That(DailyV1P12DCityCoreOwnerHydrator.TryCreate(input, definitions,
            out DailyV1CityRuntimeOwners owners,
            out DailyV1P12DCityCoreOwnerHydrationFailure failure), Is.False);

        Assert.That(owners, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DCityCoreOwnerHydrationFailure.InvalidMarketItem));
    }

    [Test]
    public void P12DCityCoreHydratorRejectsMismatchedLocationBindingAndDuplicateMembership()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1P12DCityCoreOwnerHydrationInput mismatchedLocation = CreateInput(
            city.DefinitionId, Array.Empty<P12DCityMarketItemSnapshot>(),
            Array.Empty<PopulationOperationReceiptSnapshot>(), resolvedLocationRuntimeId: "location/other");

        Assert.That(DailyV1P12DCityCoreOwnerHydrator.TryCreate(mismatchedLocation, definitions,
            out DailyV1CityRuntimeOwners rejectedLocation,
            out DailyV1P12DCityCoreOwnerHydrationFailure locationFailure), Is.False);
        Assert.That(rejectedLocation, Is.Null);
        Assert.That(locationFailure, Is.EqualTo(DailyV1P12DCityCoreOwnerHydrationFailure.InvalidInput));

        DailyV1P12DCityCoreOwnerHydrationInput duplicateMembership = CreateInput(
            city.DefinitionId, Array.Empty<P12DCityMarketItemSnapshot>(),
            Array.Empty<PopulationOperationReceiptSnapshot>(),
            importantNpcRuntimeIds: new[] { "npc/one", "npc/one" });
        Assert.That(DailyV1P12DCityCoreOwnerHydrator.TryCreate(duplicateMembership, definitions,
            out DailyV1CityRuntimeOwners rejectedMembership,
            out DailyV1P12DCityCoreOwnerHydrationFailure membershipFailure), Is.False);
        Assert.That(rejectedMembership, Is.Null);
        Assert.That(membershipFailure, Is.EqualTo(DailyV1P12DCityCoreOwnerHydrationFailure.InvalidOwnerValue));
    }

    [Test]
    public void P12DCityCoreHydratorRejectsUnsupportedSchemaWithoutReturningOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition city = definitions.Cities.Values.First();
        DailyV1P12DCityCoreOwnerHydrationInput input = CreateInput(
            city.DefinitionId, Array.Empty<P12DCityMarketItemSnapshot>(),
            Array.Empty<PopulationOperationReceiptSnapshot>(), schemaVersion: 2);

        Assert.That(DailyV1P12DCityCoreOwnerHydrator.TryCreate(input, definitions,
            out DailyV1CityRuntimeOwners owners,
            out DailyV1P12DCityCoreOwnerHydrationFailure failure), Is.False);
        Assert.That(owners, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DCityCoreOwnerHydrationFailure.InvalidInput));
    }

    private static DailyV1P12DCityCoreOwnerHydrationInput CreateInput(
        string cityDefinitionId,
        P12DCityMarketItemSnapshot[] rows,
        PopulationOperationReceiptSnapshot[] receipts,
        string resolvedLocationRuntimeId = "location/p12d-hydrator",
        string[] importantNpcRuntimeIds = null,
        int schemaVersion = 1) =>
        new DailyV1P12DCityCoreOwnerHydrationInput(
            "city/p12d-hydrator", cityDefinitionId, "location/p12d-hydrator",
            resolvedLocationRuntimeId, 9L,
            importantNpcRuntimeIds ?? new[] { "npc/second", "npc/first" },
            "city/p12d-hydrator", MarketLiquidityMode.Open, false, 11L, rows,
            "population-city/p12d-hydrator", ConsumptionPaymentMode.Free, false,
            "city/p12d-hydrator", 1200, 3L, 7L, receipts, schemaVersion);
}
