using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1CommercialKnowledgeSharingStageTests
{
    [Test]
    public void SharesNewestSnapshotValueThroughExactOwnersAndRetainsPerWriteRevision()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(2,
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second);
        DailyV1ItemDefinition older = graph.Definitions.Items["FERRO"];
        DailyV1ItemDefinition newer = graph.Definitions.Items["TRIGO"];
        DailyV1CityRuntimeOwners city = graph.Cities.Single();
        Assert.That(first.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, older, 8f, 2, 5L, 5L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(first.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, newer, 12f, 4, 8L, 8L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        int committed = 0;
        second.CommercialKnowledge.BindMutationBoundary(() => true, () => committed++);

        Assert.That(DailyV1NpcDefinitionRules.HasJobType(first.Definition, graph.Definitions,
            DailyV1NpcDefinitionRules.MerchantJobTypeValue), Is.True);
        Assert.That(first.Travel.CurrentCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(second.Travel.CurrentCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(DailyV1CommercialKnowledgeSharingStage.TryShareAmongPresentMerchants(
            graph, 10L, new EffectiveCommercialKnowledgeConfiguration(), 1), Is.True);

        Assert.That(second.CommercialKnowledge.TryGetObservation(city.LocationRuntimeId,
            newer.DefinitionId, out DailyV1CommercialMarketObservation received), Is.True,
            "Merchant stages: " + string.Join(",", graph.Npcs.Select(npc =>
                npc.RuntimeId + "/" + npc.Lifecycle.LifeState + "/" + npc.Travel.IsTraveling
                + "/" + npc.Travel.CurrentCityRuntimeId + "/" + npc.CommercialKnowledge.Observations.Count)));
        Assert.That(received.ItemDefinition, Is.SameAs(newer));
        Assert.That(received.ObservedDay, Is.EqualTo(8L));
        Assert.That(received.ReceivedDay, Is.EqualTo(10L));
        Assert.That(received.Source, Is.EqualTo(DailyV1CommercialKnowledgeSource.SharedByNpc));
        Assert.That(received.SourceRuntimeId, Is.EqualTo(first.RuntimeId));
        Assert.That(second.CommercialKnowledge.Revision, Is.EqualTo(1L));
        Assert.That(committed, Is.EqualTo(1));
    }

    [Test]
    public void SameDaySharedObservationCannotCascadeAcrossSubsequentSharingStage()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(3,
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second,
            out DailyV1NpcRuntimeOwners third);
        DailyV1CityRuntimeOwners city = graph.Cities.Single();
        DailyV1ItemDefinition item = graph.Definitions.Items["FERRO"];
        Assert.That(first.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, item, 51f, 10, 0L, 0L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        Assert.That(DailyV1CommercialKnowledgeSharingStage.TryShareAmongPresentMerchants(
            graph, 0L, new EffectiveCommercialKnowledgeConfiguration(), 2), Is.True);
        Assert.That(second.CommercialKnowledge.TryGetObservation(city.LocationRuntimeId,
            item.DefinitionId, out DailyV1CommercialMarketObservation received), Is.True);
        Assert.That(received.SourceRuntimeId, Is.EqualTo(first.RuntimeId));

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            graph.Cities, new[] { second, third }, out DailyV1SelectedRuntimeOwnerGraph nextPair), Is.True);
        Assert.That(DailyV1CommercialKnowledgeSharingStage.TryShareAmongPresentMerchants(
            nextPair, 0L, new EffectiveCommercialKnowledgeConfiguration(), 2), Is.True);

        Assert.That(third.CommercialKnowledge.TryGetObservation(city.LocationRuntimeId,
            item.DefinitionId, out _), Is.False);
        Assert.That(second.CommercialKnowledge.Revision, Is.EqualTo(1L));
        Assert.That(third.CommercialKnowledge.Revision, Is.Zero);
    }

    [Test]
    public void OddGroupRotationUsesAbsoluteDayAndStableRuntimeOrdering()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(3,
            out DailyV1NpcRuntimeOwners first, out DailyV1NpcRuntimeOwners second,
            out DailyV1NpcRuntimeOwners third);
        DailyV1CityRuntimeOwners city = graph.Cities.Single();
        DailyV1ItemDefinition item = graph.Definitions.Items["FERRO"];
        Assert.That(first.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, item, 25f, 3, 0L, 0L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        Assert.That(DailyV1CommercialKnowledgeSharingStage.TryShareAmongPresentMerchants(
            graph, 1L, new EffectiveCommercialKnowledgeConfiguration(), 2), Is.True);
        Assert.That(second.CommercialKnowledge.Observations, Is.Empty,
            "Day one pairs the second and third sorted merchants, leaving the first unpaired.");
        Assert.That(third.CommercialKnowledge.Observations, Is.Empty);

        Assert.That(DailyV1CommercialKnowledgeSharingStage.TryShareAmongPresentMerchants(
            graph, 2L, new EffectiveCommercialKnowledgeConfiguration(), 2), Is.True);
        Assert.That(third.CommercialKnowledge.TryGetObservation(city.LocationRuntimeId,
            item.DefinitionId, out DailyV1CommercialMarketObservation received), Is.True);
        Assert.That(received.SourceRuntimeId, Is.EqualTo(first.RuntimeId));
        Assert.That(received.ReceivedDay, Is.EqualTo(2L));
    }

    [Test]
    public void LocalKnowledgeRefreshDiscoversLocationAndCapturesCurrentMerchantMarket()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(2,
            out DailyV1NpcRuntimeOwners merchant, out _);
        DailyV1CityRuntimeOwners city = graph.Cities.Single();

        Assert.That(DailyV1SelectedLocalKnowledgeRefreshStage.TryRun(
            graph, 7L), Is.True);

        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.LocationRuntimeId), Is.True);
        Assert.That(merchant.CommercialKnowledge.TryGetLiquidityObservation(
            city.LocationRuntimeId, out DailyV1CommercialLiquidityObservation liquidity), Is.True);
        Assert.That(liquidity.ObservedDay, Is.EqualTo(7L));
        Assert.That(liquidity.ReceivedDay, Is.EqualTo(7L));
        Assert.That(liquidity.Source, Is.EqualTo(DailyV1CommercialKnowledgeSource.DirectObservation));
    }

    [Test]
    public void LocalKnowledgeRefreshUsesAdmittedMerchantPolicyWhenCapturingMarket()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(2,
            out DailyV1NpcRuntimeOwners merchant, out _, merchantTradeEnabled: false);
        DailyV1CityRuntimeOwners city = graph.Cities.Single();

        Assert.That(DailyV1SelectedLocalKnowledgeRefreshStage.TryRun(graph, 7L), Is.True);

        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.LocationRuntimeId), Is.True);
        Assert.That(merchant.CommercialKnowledge.Observations, Is.Empty,
            "Disabled admitted MerchantTrade configuration suppresses observation and sharing.");
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        int merchantCount,
        out DailyV1NpcRuntimeOwners first,
        out DailyV1NpcRuntimeOwners second,
        bool merchantTradeEnabled = true) =>
        CreateGraph(merchantCount, out first, out second, out _, merchantTradeEnabled);

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        int merchantCount,
        out DailyV1NpcRuntimeOwners first,
        out DailyV1NpcRuntimeOwners second,
        out DailyV1NpcRuntimeOwners third,
        bool merchantTradeEnabled = true)
    {
        Assert.That(merchantCount, Is.InRange(2, 3));
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        if (!merchantTradeEnabled)
            definitions = CopyWithMerchantTradeEnabled(definitions, enabled: false);
        DailyV1CityDefinition cityDefinition = definitions.Cities.Values.First();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city/sharing-test", "location/sharing-test", definitions, cityDefinition,
            null, out DailyV1CityRuntimeOwners city), Is.True);
        DailyV1NpcDefinition merchantDefinition = definitions.Npcs.Values.FirstOrDefault(npc =>
            DailyV1NpcDefinitionRules.HasJobType(npc, definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue));
        Assert.That(merchantDefinition, Is.Not.Null);

        List<DailyV1NpcRuntimeOwners> merchants = new List<DailyV1NpcRuntimeOwners>();
        string[] ids = { "merchant/a", "merchant/b", "merchant/c" };
        for (int index = 0; index < merchantCount; index++)
        {
            Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
                ids[index], definitions, merchantDefinition, 100f,
                out DailyV1NpcRuntimeOwners merchant), Is.True);
            Assert.That(merchant.Travel.TryCommitState(city.LocationRuntimeId, city.RuntimeId,
                null, null, 0, 0, null, false, null, null, false), Is.True);
            Assert.That(city.NpcPresence.TryAdd(merchant.RuntimeId), Is.True);
            merchants.Add(merchant);
        }

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            new[] { city }, merchants, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        first = merchants[0];
        second = merchants[1];
        third = merchantCount > 2 ? merchants[2] : null;
        return graph;
    }

    private static DailyV1EffectiveDefinitions CopyWithMerchantTradeEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            current.Economy,
            current.Travel,
            current.Crime,
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            new EffectiveMerchantTradeConfiguration(enabled, false, 5, 0.7f, 0.25f, 3),
            current.CommercialKnowledge);
        return new DailyV1EffectiveDefinitions(
            definitions.Configuration,
            configuration,
            definitions.Calendar,
            null,
            definitions.Cities.Values,
            definitions.Items.Values,
            definitions.Npcs.Values,
            definitions.Jobs.Values,
            definitions.Actions.Values,
            definitions.Statuses.Values,
            null,
            definitions.StageOrder,
            definitions.ProfileContractIdentity,
            definitions.GeographyProfileContractIdentity,
            definitions.GeographyStageIdentity,
            definitions.HexCoordinateConvention,
            definitions.HexCoordinateCanonicalOrder,
            null,
            definitions.Traits.Values,
            definitions.CapabilityAttributes.Values,
            definitions.ExplorableSites.Values);
    }
}
