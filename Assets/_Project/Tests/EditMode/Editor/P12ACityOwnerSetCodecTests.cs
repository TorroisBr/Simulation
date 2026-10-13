using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12ACityOwnerSetCodecTests
{
    [Test]
    public void ComposesEveryCityOwnerAgainstItsExactDynamicCensus()
    {
        P12DCityRootOwnerSnapshot[] cities = { City("city-a"), City("city-b") };
        IReadOnlyList<OwnerSectionCensusSnapshot> census = Census(cities);

        Assert.That(P12ACityOwnerSetCodec.TryEncode(cities, census,
            out IReadOnlyList<P12AOwnerSection> sections, out P12AResult error),
            Is.True, error?.SectionCode + ":" + error?.Code);
        Assert.That(sections, Has.Count.EqualTo(8));
        Assert.That(sections.Select(section => section.Id), Is.Ordered);
        foreach (P12DCityRootOwnerSnapshot city in cities)
        {
            Assert.That(sections.Count(section =>
                section.Id == CityNpcPresenceCensusProvider.SectionIdFor(city.CityRuntimeId)), Is.EqualTo(1));
        }
        Assert.That(P12ACityOwnerSetCodec.TryDecode(sections,
            out IReadOnlyList<P12DCityRootOwnerSnapshot> decoded, out P12AResult decodeError),
            Is.True, decodeError?.SectionCode + ":" + decodeError?.Code);
        Assert.That(decoded.Select(city => city.CityRuntimeId), Is.EqualTo(new[] { "city-a", "city-b" }));
        Assert.That(P12ACityOwnerSetCodec.TryEncode(decoded, census,
            out IReadOnlyList<P12AOwnerSection> encodedAgain, out P12AResult reencodeError),
            Is.True, reencodeError?.Code.ToString());
        Assert.That(encodedAgain.Select(section => section.Id), Is.EqualTo(sections.Select(section => section.Id)));
        for (int i = 0; i < sections.Count; i++)
        {
            Assert.That(encodedAgain[i].Rows, Has.Count.EqualTo(sections[i].Rows.Count));
            for (int rowIndex = 0; rowIndex < sections[i].Rows.Count; rowIndex++)
                Assert.That(encodedAgain[i].Rows[rowIndex].Payload,
                    Is.EqualTo(sections[i].Rows[rowIndex].Payload));
        }
    }

    [Test]
    public void RejectsMissingDuplicateAndStaleCityOwnership()
    {
        P12DCityRootOwnerSnapshot city = City("city-a");
        IReadOnlyList<OwnerSectionCensusSnapshot> census = Census(new[] { city });

        Assert.That(P12ACityOwnerSetCodec.TryEncode(new[] { city, city }, census,
            out _, out _), Is.False);
        Assert.That(P12ACityOwnerSetCodec.TryEncode(new[] { city }, census.Take(census.Count - 1).ToArray(),
            out _, out _), Is.False);

        List<OwnerSectionCensusSnapshot> stale = census.ToList();
        int marketIndex = stale.FindIndex(value => value.SectionId.StartsWith(
            CityMarketCensusProvider.SectionIdPrefix, StringComparison.Ordinal));
        stale[marketIndex] = new OwnerSectionCensusSnapshot(stale[marketIndex].SectionId,
            stale[marketIndex].SchemaVersion, stale[marketIndex].Role,
            stale[marketIndex].OwnerInstanceIdentity, stale[marketIndex].Cardinality + 1,
            stale[marketIndex].Revision);
        Assert.That(P12ACityOwnerSetCodec.TryEncode(new[] { city }, stale,
            out _, out _), Is.False);

        Assert.That(P12ACityOwnerSetCodec.TryDecode(Array.Empty<P12AOwnerSection>(),
            out _, out _), Is.False);
        Assert.That(P12ACityOwnerCodec.TryEncode(city,
            out IReadOnlyList<P12AOwnerSection> encoded, out _), Is.True);
        Assert.That(P12ACityOwnerSetCodec.TryDecode(encoded.Concat(new[] { encoded[0] }).ToArray(),
            out _, out _), Is.False);
    }

    private static P12DCityRootOwnerSnapshot City(string cityId)
        => new P12DCityRootOwnerSnapshot(cityId, "definition-" + cityId,
            "location-" + cityId, 0L, Array.Empty<string>(), cityId,
            MarketLiquidityMode.Open, 0L, Array.Empty<P12DCityMarketItemSnapshot>(),
            "population-" + cityId, ConsumptionPaymentMode.Free, cityId, 10,
            0L, 0L, Array.Empty<PopulationOperationReceiptSnapshot>());

    private static IReadOnlyList<OwnerSectionCensusSnapshot> Census(
        IReadOnlyList<P12DCityRootOwnerSnapshot> cities)
    {
        List<OwnerSectionCensusSnapshot> result = new List<OwnerSectionCensusSnapshot>();
        foreach (string id in P12ADailyV1Census.FixedRequiredSectionIds)
            Add(result, id, OwnerSectionRole.Required, 1, 0L);
        foreach (string id in P12ADailyV1Census.FixedEmptySectionIds)
            Add(result, id, OwnerSectionRole.ExplicitlyEmpty, 0, 0L);

        const string npcId = "npc-a";
        foreach (string prefix in P12ADailyV1Census.NpcSectionPrefixes)
            Add(result, prefix + npcId, OwnerSectionRole.Required, 1, 0L);
        Add(result, NpcLifecycleCensusProvider.ResidenceSectionPrefix + npcId,
            OwnerSectionRole.Required, 1, 0L);

        foreach (P12DCityRootOwnerSnapshot city in cities)
        {
            Assert.That(P12ACityOwnerCodec.TryEncode(city,
                out IReadOnlyList<P12AOwnerSection> encoded, out P12AResult error),
                Is.True, error?.Code.ToString());
            foreach (P12AOwnerSection section in encoded)
                Add(result, section.Id, section.Role, section.Rows.Count, section.Revision);
        }
        return result.AsReadOnly();
    }

    private static void Add(List<OwnerSectionCensusSnapshot> destination, string id,
        OwnerSectionRole role, int cardinality, long revision)
    {
        string expectedAlias = P12ADailyV1Census.ExpectedAliasGroupForSection(id);
        object ownerIdentity = expectedAlias == null
            ? new object()
            : destination.Select(snapshot => snapshot)
                .FirstOrDefault(snapshot => P12ADailyV1Census.ExpectedAliasGroupForSection(snapshot.SectionId)
                    == expectedAlias)?.OwnerInstanceIdentity ?? new object();
        destination.Add(new OwnerSectionCensusSnapshot(id, 1, role, ownerIdentity,
            cardinality, revision));
    }
}
