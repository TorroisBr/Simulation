using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedLocalKnowledgeOwnerBindingTests
{
    [Test]
    public void FailedCreationReleasesEarlierOwnerBindingsBeforeCleanRetry()
    {
        DailyV1EffectiveDefinitions definitions = CreateSingleItemDefinitions();
        DailyV1NpcDefinition[] admittedNpcs = definitions.Npcs.Values.Take(2).ToArray();
        Assert.That(admittedNpcs, Has.Length.EqualTo(2));
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/session-first", definitions, admittedNpcs[0], 0f,
            out DailyV1NpcRuntimeOwners first), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/session-second", definitions, admittedNpcs[1], 0f,
            out DailyV1NpcRuntimeOwners second), Is.True);

        // Construct a malformed graph where the later NPC aliases the first
        // NPC's commercial owner. Session creation binds the first NPC before
        // detecting that duplicate and must roll that binding back.
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateBoundNpcOwner(
            second.RuntimeId, definitions, second.Definition, second.Status.Statuses,
            second.Inventory, second.MoneyAccount, first.CommercialKnowledge,
            second.MerchantTradePlan, second.SpatialKnowledge,
            out DailyV1NpcRuntimeOwners aliasedSecond), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            Array.Empty<DailyV1CityRuntimeOwners>(), new[] { first, aliasedSecond },
            out DailyV1SelectedRuntimeOwnerGraph malformedGraph), Is.True);

        DailyV1MutationEpochState failedCreateEpoch = new DailyV1MutationEpochState();
        bool failedCreateOperation = false;
        Func<bool> failedCreateAdmission = () => failedCreateOperation && failedCreateEpoch.CanAdvance;
        Action failedCreateCommit = () =>
        {
            if (!failedCreateOperation || !failedCreateEpoch.TryAdvanceCommittedMutation())
                throw new InvalidOperationException("The enclosing runtime rejected an owner commit.");
        };
        Assert.That(DailyV1SelectedLocalKnowledgeOwnerBinding.TryCreate(
            malformedGraph, () => failedCreateOperation, failedCreateAdmission,
            failedCreateCommit, out _), Is.False);

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            Array.Empty<DailyV1CityRuntimeOwners>(), new[] { first },
            out DailyV1SelectedRuntimeOwnerGraph cleanGraph), Is.True);
        Assert.That(DailyV1SelectedLocalKnowledgeOwnerBinding.TryCreate(
            cleanGraph, () => failedCreateOperation, failedCreateAdmission,
            failedCreateCommit,
            out DailyV1SelectedLocalKnowledgeOwnerBinding cleanBinding), Is.True,
            "The earlier Spatial and Commercial owners must no longer retain the failed candidate's delegates.");
        Assert.That(cleanBinding.TryRelease(), Is.True);
    }

    [Test]
    public void CoreOwnerBindingRunsLocalKnowledgeStageInsideCallerOwnedOperation()
    {
        DailyV1EffectiveDefinitions definitions = CreateSingleItemDefinitions();
        DailyV1CityDefinition cityDefinition = definitions.Cities.Values.Single();
        DailyV1NpcDefinition merchantDefinition = definitions.Npcs.Values.First(candidate =>
            DailyV1NpcDefinitionRules.HasJobType(candidate, definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue));

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            "city/core-session", "location/core-session", definitions,
            cityDefinition, null, out DailyV1CityRuntimeOwners city), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/core-session-merchant", definitions, merchantDefinition, 100f,
            out DailyV1NpcRuntimeOwners merchant), Is.True);
        Assert.That(merchant.Travel.TryCommitState(city.LocationRuntimeId, city.RuntimeId,
            null, null, 0, 0, null, false, null, null, false), Is.True);
        Assert.That(city.NpcPresence.TryAdd(merchant.RuntimeId), Is.True);

        // Day-zero genesis already knows this location. The day-one stage must
        // not create a spatial revision while it records only current facts.
        merchant.SpatialKnowledge.DiscoverLocation(city.LocationRuntimeId);
        long spatialRevisionBefore = merchant.SpatialKnowledge.Revision;
        long commercialRevisionBefore = merchant.CommercialKnowledge.Revision;
        long marketRevisionBefore = city.Market.Revision;
        DailyV1MarketItemState marketItem = city.Market.Items.Single();

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            new[] { city }, new[] { merchant }, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1MutationEpochState mutationEpoch = new DailyV1MutationEpochState();
        bool operationOpen = false;
        Func<bool> isCurrentOperation = () => operationOpen;
        Func<bool> mutationAdmission = () => operationOpen && mutationEpoch.CanAdvance;
        Action mutationCommitted = () =>
        {
            if (!operationOpen || !mutationEpoch.TryAdvanceCommittedMutation())
                throw new InvalidOperationException("The enclosing runtime rejected an owner commit.");
        };
        Assert.That(DailyV1SelectedLocalKnowledgeOwnerBinding.TryCreate(
            graph, isCurrentOperation, mutationAdmission, mutationCommitted,
            out DailyV1SelectedLocalKnowledgeOwnerBinding binding), Is.True);
        Assert.That(binding.Graph, Is.SameAs(graph));
        Assert.That(binding.Graph.Definitions, Is.SameAs(definitions));
        Assert.That(binding.Graph.Cities.Single(), Is.SameAs(city));
        Assert.That(binding.Graph.Npcs.Single(), Is.SameAs(merchant));
        Assert.That(merchant.CommercialKnowledge.Definitions, Is.SameAs(definitions));
        Assert.That(marketItem.Item, Is.SameAs(definitions.Items[marketItem.Item.DefinitionId]));

        operationOpen = true;
        Assert.That(binding.TryRefreshLocalKnowledge(1L), Is.True);
        operationOpen = false;

        Assert.That(merchant.SpatialKnowledge.KnowsLocation(city.LocationRuntimeId), Is.True);
        Assert.That(merchant.SpatialKnowledge.Revision, Is.EqualTo(spatialRevisionBefore));
        Assert.That(merchant.CommercialKnowledge.Observations, Has.Count.EqualTo(1));
        DailyV1CommercialMarketObservation marketObservation =
            merchant.CommercialKnowledge.Observations.Single();
        Assert.That(marketObservation.LocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(marketObservation.ItemDefinition, Is.SameAs(marketItem.Item));
        Assert.That(marketObservation.ObservedDay, Is.EqualTo(1L));
        Assert.That(marketObservation.ReceivedDay, Is.EqualTo(1L));
        Assert.That(marketObservation.Source,
            Is.EqualTo(DailyV1CommercialKnowledgeSource.DirectObservation));
        Assert.That(merchant.CommercialKnowledge.LiquidityObservations, Has.Count.EqualTo(1));
        DailyV1CommercialLiquidityObservation liquidity =
            merchant.CommercialKnowledge.LiquidityObservations.Single();
        Assert.That(liquidity.LocationRuntimeId, Is.EqualTo(city.LocationRuntimeId));
        Assert.That(liquidity.ObservedDay, Is.EqualTo(1L));
        Assert.That(liquidity.ReceivedDay, Is.EqualTo(1L));
        Assert.That(liquidity.Source,
            Is.EqualTo(DailyV1CommercialKnowledgeSource.DirectObservation));
        Assert.That(merchant.CommercialKnowledge.Revision,
            Is.EqualTo(commercialRevisionBefore + 2L));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore));
        Assert.That(mutationEpoch.CurrentEpoch, Is.EqualTo(2L));
        Assert.That(merchant.CommercialKnowledge.TryReadCensus(
            out int marketCount, out int liquidityCount, out int receiptCount, out _), Is.True);
        Assert.That(marketCount, Is.EqualTo(1));
        Assert.That(liquidityCount, Is.EqualTo(1));
        Assert.That(receiptCount, Is.Zero,
            "One present Merchant produces no peer-sharing receipt.");

        operationOpen = true;
        Assert.That(binding.TryRefreshLocalKnowledge(1L), Is.True);
        operationOpen = false;
        Assert.That(mutationEpoch.CurrentEpoch, Is.EqualTo(2L),
            "Replaying the same local stage/day creates no new owner commit or epoch.");
        Assert.That(merchant.CommercialKnowledge.Revision,
            Is.EqualTo(commercialRevisionBefore + 2L));
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevisionBefore));
        Assert.That(binding.TryRelease(), Is.True);
    }

    private static DailyV1EffectiveDefinitions CreateSingleItemDefinitions()
    {
        DailyV1EffectiveDefinitions source =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1CityDefinition originalCity = source.Cities.Values.First();
        DailyV1CityData value = originalCity.Values;
        DailyV1CityData singleItemValue = new DailyV1CityData(
            value.id, value.cityName, value.settlementSemanticId,
            value.materialFlowLocationId, value.marketStoreSemanticId,
            value.materialFlowProfile, value.initialPopulation,
            value.marketLiquidity, value.populationConsumption,
            value.marketItems.Take(1).ToArray(), value.productionConfigs,
            value.connections);
        DailyV1CityDefinition singleItemCity = new DailyV1CityDefinition(
            "CityData", originalCity.DefinitionId, singleItemValue);

        return new DailyV1EffectiveDefinitions(
            source.Configuration, source.EffectiveConfiguration, source.Calendar,
            null, new[] { singleItemCity }, source.Items.Values, source.Npcs.Values,
            source.Jobs.Values, source.Actions.Values, source.Statuses.Values,
            null, source.StageOrder, source.ProfileContractIdentity,
            source.GeographyProfileContractIdentity, source.GeographyStageIdentity,
            source.HexCoordinateConvention, source.HexCoordinateCanonicalOrder,
            null, source.Traits.Values, source.CapabilityAttributes.Values,
            source.ExplorableSites.Values);
    }
}
