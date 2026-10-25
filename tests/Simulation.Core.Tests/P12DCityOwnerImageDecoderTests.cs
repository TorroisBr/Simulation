using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12DCityOwnerImageDecoderTests
{
    [Test]
    public void CitySectionRoundTripPreservesIdentityRevisionValuesAndWireAliases()
    {
        DailyV1P12DCityCoreOwnerHydrationInput source = City("city-roundtrip");
        Assert.That(P12DCityOwnerImageSectionCodec.TryEncode(source,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult encodeError),
            Is.True, encodeError?.SectionCode + ":" + encodeError?.Code);

        Assert.That(sections.Select(section => section.Id), Is.EqualTo(new[]
        {
            "p12b.city.important-npcs/city-roundtrip",
            "p12d.city-population.aggregate/14:city-roundtrip",
            "p12d.city-population.operation-receipts/14:city-roundtrip",
            "p12e.city-market-stock-rows/14:city-roundtrip"
        }));
        P12AOwnerSection population = sections.Single(section =>
            section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix,
                StringComparison.Ordinal));
        P12AOwnerSection receipts = sections.Single(section =>
            section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix,
                StringComparison.Ordinal));
        Assert.That(population.Major, Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion));
        Assert.That(receipts.Major, Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion));
        Assert.That(population.AliasGroup, Is.EqualTo(receipts.AliasGroup));

        Assert.That(P12DCityOwnerImageSectionCodec.TryDecode(
            sections[0], sections[3], sections[1], sections[2],
            out DailyV1P12DCityCoreOwnerHydrationInput decoded, out P12AResult decodeError),
            Is.True, decodeError?.SectionCode + ":" + decodeError?.Code);
        Assert.That(decoded.CityRuntimeId, Is.EqualTo(source.CityRuntimeId));
        Assert.That(decoded.CityDefinitionId, Is.EqualTo(source.CityDefinitionId));
        Assert.That(decoded.LegacyLocationRuntimeId, Is.EqualTo(source.LegacyLocationRuntimeId));
        Assert.That(decoded.ResolvedLocationRuntimeId, Is.EqualTo(source.LegacyLocationRuntimeId));
        Assert.That(decoded.ImportantNpcRuntimeIds, Is.EqualTo(source.ImportantNpcRuntimeIds));
        Assert.That(decoded.ImportantNpcRevision, Is.EqualTo(source.ImportantNpcRevision));
        P12DCityMarketItemSnapshot decodedItem = decoded.MarketItems.Single();
        Assert.That(decodedItem.ItemDefinitionId, Is.EqualTo("item-iron"));
        Assert.That(decodedItem.Amount, Is.EqualTo(4));
        Assert.That(decodedItem.DesiredAmount, Is.EqualTo(7));
        Assert.That(decodedItem.CurrentPrice, Is.EqualTo(2.5f));
        Assert.That(decoded.MarketRevision, Is.EqualTo(source.MarketRevision));
        Assert.That(decoded.CurrentPopulation, Is.EqualTo(source.CurrentPopulation));
        Assert.That(decoded.PopulationRevision, Is.EqualTo(source.PopulationRevision));
        Assert.That(decoded.PopulationReceiptRevision, Is.EqualTo(source.PopulationReceiptRevision));
        PopulationOperationReceiptSnapshot decodedReceipt = decoded.PopulationReceipts.Single();
        PopulationOperationReceiptSnapshot sourceReceipt = source.PopulationReceipts.Single();
        Assert.That(decodedReceipt.Identity, Is.EqualTo(sourceReceipt.Identity));
        Assert.That(decodedReceipt.Fingerprint, Is.EqualTo(sourceReceipt.Fingerprint));
        Assert.That(decodedReceipt.Transition, Is.EqualTo(sourceReceipt.Transition));
    }

    [Test]
    public void CitySetDecoderSortsCitiesAndPublishesNoPartialSetAfterMissingSection()
    {
        List<P12AOwnerSection> sections = new List<P12AOwnerSection>();
        foreach (DailyV1P12DCityCoreOwnerHydrationInput city in new[] { City("city-z"), City("city-a") })
        {
            Assert.That(P12DCityOwnerImageSectionCodec.TryEncode(city,
                out IReadOnlyList<P12AOwnerSection> citySections, out P12AResult error),
                Is.True, error?.SectionCode + ":" + error?.Code);
            sections.AddRange(citySections);
        }

        Assert.That(P12DCityOwnerSetImageDecoder.TryDecode(sections,
            out IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> decoded, out P12AResult result),
            Is.True, result?.SectionCode + ":" + result?.Code);
        Assert.That(decoded.Select(city => city.CityRuntimeId), Is.EqualTo(new[] { "city-a", "city-z" }));

        sections.RemoveAt(sections.Count - 1);
        Assert.That(P12DCityOwnerSetImageDecoder.TryDecode(sections,
            out IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> failed, out _), Is.False);
        Assert.That(failed, Is.Null);
    }

    [Test]
    public void CitySectionDecoderRejectsAliasIdentityDriftWithoutReturningHydrationInput()
    {
        DailyV1P12DCityCoreOwnerHydrationInput source = City("city-alias");
        Assert.That(P12DCityOwnerImageSectionCodec.TryEncode(source,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult error),
            Is.True, error?.SectionCode + ":" + error?.Code);

        List<P12AOwnerSection> drifted = sections.ToList();
        int index = drifted.FindIndex(section =>
            section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix,
                StringComparison.Ordinal));
        P12AOwnerSection original = drifted[index];
        drifted[index] = new P12AOwnerSection(original.Id, original.Major, original.Minor,
            original.Role, "alias/unexpected", original.Revision, original.Rows);

        P12AOwnerSection presence = drifted.Single(section =>
            section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                StringComparison.Ordinal));
        P12AOwnerSection market = drifted.Single(section =>
            section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix,
                StringComparison.Ordinal));
        P12AOwnerSection population = drifted.Single(section =>
            section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix,
                StringComparison.Ordinal));
        P12AOwnerSection receipts = drifted.Single(section =>
            section.Id.StartsWith(DailyV1SelectedOwnerSectionCensusContract.CityPopulationOperationReceiptsSectionPrefix,
                StringComparison.Ordinal));

        Assert.That(P12DCityOwnerImageSectionCodec.TryDecode(presence, market, population, receipts,
            out DailyV1P12DCityCoreOwnerHydrationInput failed, out _), Is.False);
        Assert.That(failed, Is.Null);
    }

    private static DailyV1P12DCityCoreOwnerHydrationInput City(string cityId)
        => new DailyV1P12DCityCoreOwnerHydrationInput(
            cityId,
            "city-definition-" + cityId,
            "location-" + cityId,
            "location-" + cityId,
            3L,
            new[] { "npc-z", "npc-a" },
            cityId,
            MarketLiquidityMode.Open,
            false,
            8L,
            new[] { new P12DCityMarketItemSnapshot("item-iron", 4, 7, 2.5f) },
            "population-" + cityId,
            ConsumptionPaymentMode.Free,
            false,
            cityId,
            120,
            9L,
            1L,
            new[]
            {
                new PopulationOperationReceiptSnapshot(
                    "population-op-" + cityId,
                    "population-fingerprint-" + cityId,
                    PopulationOwnerBridge.CreateDecodedTransition(
                        cityId, 2L, 119, new PopulationChangeSet(1, 0, 0, 0), 1L, 120))
            });
}