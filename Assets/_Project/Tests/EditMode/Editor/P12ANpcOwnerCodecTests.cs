using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12ANpcOwnerCodecTests
{
    [Test]
    public void ComposesAllRequiredNpcSectionsAndConditionalUnboundResidence()
    {
        const string runtimeId = "npc-a";
        P12DNpcDRow source = DRow(runtimeId, null);
        P12DNpcFRow fRow = FRow(runtimeId);
        IReadOnlyList<OwnerSectionCensusSnapshot> census = Census(runtimeId);
        Assert.That(P12ANpcOwnerCodec.TryEncode(source, fRow,
            census, out IReadOnlyList<P12AOwnerSection> sections, out P12AResult error),
            Is.True, error?.SectionCode + ":" + error?.Code);
        Assert.That(sections, Has.Count.EqualTo(23));
        Assert.That(sections.Select(section => section.Id), Is.Ordered);
        foreach (string prefix in P12ADailyV1Census.NpcSectionPrefixes)
            Assert.That(sections.Count(section => section.Id.StartsWith(prefix, StringComparison.Ordinal)),
                Is.EqualTo(1), prefix);
        Assert.That(sections.Count(section => section.Id.StartsWith("p12b.npc-residence/", StringComparison.Ordinal)),
            Is.EqualTo(1));
    }

    [Test]
    public void OmitsNpcResidenceWhenPersonOwnsThatFact()
    {
        const string runtimeId = "npc-bound";
        Assert.That(P12ANpcOwnerCodec.TryEncode(DRow(runtimeId, "person-1"), FRow(runtimeId),
            Census(runtimeId), out IReadOnlyList<P12AOwnerSection> sections, out P12AResult error),
            Is.True, error?.SectionCode + ":" + error?.Code);
        Assert.That(sections, Has.Count.EqualTo(22));
        Assert.That(sections.Any(section => section.Id.StartsWith("p12b.npc-residence/",
            StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void RejectsMissingOrDuplicatedRequiredWitnesses()
    {
        const string runtimeId = "npc-a";
        List<OwnerSectionCensusSnapshot> census = Census(runtimeId).ToList();
        Assert.That(P12ANpcOwnerCodec.TryEncode(DRow(runtimeId, null), FRow(runtimeId),
            census.Take(3).ToArray(), out _, out _), Is.False);
        census.Add(census[0]);
        Assert.That(P12ANpcOwnerCodec.TryEncode(DRow(runtimeId, null), FRow(runtimeId),
            census, out _, out _), Is.False);
    }

    private static IReadOnlyList<OwnerSectionCensusSnapshot> Census(string runtimeId)
    {
        object local = new object(), trade = new object(), topology = new object();
        return new[]
        {
            new OwnerSectionCensusSnapshot(P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(runtimeId),
                P12DNpcReceiptOwnerCensusProvider.SchemaVersion, OwnerSectionRole.Required, local, 0, 0L),
            new OwnerSectionCensusSnapshot(P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(runtimeId),
                P12DNpcReceiptOwnerCensusProvider.SchemaVersion, OwnerSectionRole.Required, trade, 0, 0L),
            new OwnerSectionCensusSnapshot(NpcKnowledgeCensusProvider.SectionIdFor(1, runtimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required, topology, 0, 0L),
            new OwnerSectionCensusSnapshot(NpcKnowledgeCensusProvider.SectionIdFor(2, runtimeId),
                NpcKnowledgeCensusProvider.SchemaVersion, OwnerSectionRole.Required, topology, 0, 0L)
        };
    }

    private static P12DNpcDRow DRow(string runtimeId, string personId)
        => new P12DNpcDRow(runtimeId, "npc-definition", personId, null,
            NpcLifeState.Alive, NpcInjurySeverity.None, Array.Empty<string>(), 0,
            null, null, null, null, null, Array.Empty<P12DNpcInventoryRowValue>(),
            0L, 0f, 0L, 0L, 0L, 0L, 0L);

    private static P12DNpcFRow FRow(string runtimeId)
        => new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, null, 0L,
            new P12DNpcMerchantPlanValue(null, null, null, 0, 0, 0f, 0, 0, null, 0L),
            new P12DNpcTravelPlanValue(null, null, NpcTravelReason.None, 0f, 0f, null, 0L),
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);
}
