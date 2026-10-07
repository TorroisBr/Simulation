using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class P14CMixedSourceProductionTests
{
    [SetUp] public void SetUp() => SimulationTestFactory.CleanupDefinitions();
    [TearDown] public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void MixedProfileRequiresOneCityAndTwoDistinctExplicitSourcePolicies()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-admission-item");
        CityData city = CreateCity("p14c-admission-city", item, 10, 0f, reverseSourceRows: false);

        Assert.That(FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
            new List<CityData> { city }, out string validRejection), Is.True, validRejection);

        city.productionConfigs.Add(new CityProductionConfig
        {
            item = item, amountPerDay = 1, productionSourceId = "source.extra",
            contentRevision = "v1", sourceKind = CityProductionSourceKind.ExogenousDaily
        });
        Assert.That(FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
            new List<CityData> { city }, out string extraRejection), Is.False);
        Assert.That(extraRejection, Is.EqualTo("MixedSourcesProfileRequiresExactlyTwoSources"));

        city = CreateCity("p14c-admission-city-duplicate", item, 10, 0f, reverseSourceRows: false);
        city.productionConfigs[1].productionSourceId = city.productionConfigs[0].productionSourceId;
        Assert.That(FiniteSourceProfileAdmission.TryValidateAuthoredCityCardinality(
            new List<CityData> { city }, out string duplicateRejection), Is.False);
        Assert.That(duplicateRejection, Is.EqualTo("MixedSourcesSourceIdentityOrQuantityInvalid"));
    }

    [Test]
    public void DirectFlowOrdersContributorsAndBalancesOneStockResult()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-direct-item");
        CityData data = CreateCity("p14c-direct-city", item, 5, 4f, reverseSourceRows: true);
        CityRuntime city = new CityRuntime("runtime.p14c.direct", data,
            new SpatialLocationRuntime("legacy.p14c.direct"));

        SimulateDirect(city, 3L);

        LocalDailyMaterialFlowResult flow = city.LastMaterialFlow;
        Assert.That(flow, Is.Not.Null);
        Assert.That(flow.OpeningStock, Is.EqualTo(5));
        Assert.That(flow.ConfiguredSourceQuantity, Is.EqualTo(5L));
        Assert.That(flow.AppliedSourceQuantity, Is.EqualTo(5));
        Assert.That(flow.ActualFreeConsumption, Is.EqualTo(4));
        Assert.That(flow.ClosingStock, Is.EqualTo(6));
        Assert.That(flow.OpeningStock + flow.AppliedSourceQuantity - flow.ActualFreeConsumption,
            Is.EqualTo(flow.ClosingStock));
        Assert.That(flow.SourceOutcomes.Count, Is.EqualTo(2));
        Assert.That(flow.SourceOutcomes[0].ProductionSourceId, Is.EqualTo("a-exogenous"));
        Assert.That(flow.SourceOutcomes[0].AppliedQuantity, Is.EqualTo(2));
        Assert.That(flow.SourceOutcomes[1].ProductionSourceId, Is.EqualTo("b-finite"));
        Assert.That(flow.SourceOutcomes[1].AppliedQuantity, Is.EqualTo(3));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(4));
        Assert.That(city.FiniteProductionSources.Source.Revision, Is.EqualTo(1));
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(6));
    }

    [Test]
    public void SourceOrderFingerprintAndDailyResultsIgnoreAuthoredRowOrder()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-order-item");
        CityRuntime forward = new CityRuntime("runtime.p14c.order",
            CreateCity("p14c-order-city", item, 5, 1f, reverseSourceRows: false),
            new SpatialLocationRuntime("legacy.p14c.order.forward"));
        CityRuntime reverse = new CityRuntime("runtime.p14c.order",
            CreateCity("p14c-order-city", item, 5, 1f, reverseSourceRows: true),
            new SpatialLocationRuntime("legacy.p14c.order.reverse"));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world.p14c", "profile.p14c", 4L);
        Assert.That(forward.TryCreateDailyEconomyStep(operation, 0, CityDailyEconomyStepKind.Production,
            out BoundaryContinuationStep forwardStep, out _), Is.True);
        Assert.That(reverse.TryCreateDailyEconomyStep(operation, 0, CityDailyEconomyStepKind.Production,
            out BoundaryContinuationStep reverseStep, out _), Is.True);
        Assert.That(forwardStep.OwnerRevision, Is.EqualTo(reverseStep.OwnerRevision));

        SimulateDirect(forward, 4L);
        SimulateDirect(reverse, 4L);

        Assert.That(forward.LastMaterialFlow.SourceOutcomes.Count, Is.EqualTo(reverse.LastMaterialFlow.SourceOutcomes.Count));
        for (int i = 0; i < forward.LastMaterialFlow.SourceOutcomes.Count; i++)
        {
            Assert.That(forward.LastMaterialFlow.SourceOutcomes[i].ProductionSourceId,
                Is.EqualTo(reverse.LastMaterialFlow.SourceOutcomes[i].ProductionSourceId));
            Assert.That(forward.LastMaterialFlow.SourceOutcomes[i].AppliedQuantity,
                Is.EqualTo(reverse.LastMaterialFlow.SourceOutcomes[i].AppliedQuantity));
        }
        Assert.That(forward.Market.GetAmount(item), Is.EqualTo(reverse.Market.GetAmount(item)));
        Assert.That(forward.FiniteProductionSources.Source.RemainingReserve,
            Is.EqualTo(reverse.FiniteProductionSources.Source.RemainingReserve));
    }

    [Test]
    public void CapacityRejectsWholeLaterContributorWithoutFiniteDebit()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-overflow-item");
        CityData data = CreateCity("p14c-overflow-city", item, int.MaxValue - 3, 0f, reverseSourceRows: true);
        CityRuntime city = new CityRuntime("runtime.p14c.overflow", data,
            new SpatialLocationRuntime("legacy.p14c.overflow"));

        SimulateDirect(city, 8L);

        LocalDailyMaterialFlowResult flow = city.LastMaterialFlow;
        Assert.That(flow.SourceOutcomes[0].ProductionSourceId, Is.EqualTo("a-exogenous"));
        Assert.That(flow.SourceOutcomes[0].AppliedQuantity, Is.EqualTo(2));
        Assert.That(flow.SourceOutcomes[1].ProductionSourceId, Is.EqualTo("b-finite"));
        Assert.That(flow.SourceOutcomes[1].PlannedQuantity, Is.EqualTo(3));
        Assert.That(flow.SourceOutcomes[1].AppliedQuantity, Is.Zero);
        Assert.That(flow.SourceOutcomes[1].RejectionReason, Is.EqualTo("AggregateStockOverflow"));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(7));
        Assert.That(city.FiniteProductionSources.Source.Revision, Is.Zero);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(int.MaxValue - 1));
    }

    [Test]
    public void ExhaustedFiniteContributorIsExactZeroWithoutFiniteOwnerMutation()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-exhausted-item");
        CityData data = CreateCity("p14c-exhausted-city", item, 5, 0f, reverseSourceRows: false);
        data.productionConfigs.Find(row => row.sourceKind == CityProductionSourceKind.FiniteReserveDaily).initialReserve = 0;
        CityRuntime city = new CityRuntime("runtime.p14c.exhausted", data,
            new SpatialLocationRuntime("legacy.p14c.exhausted"));

        SimulateDirect(city, 8L);

        CityDailyMaterialFlowSourceOutcome finite = city.LastMaterialFlow.SourceOutcomes[1];
        Assert.That(finite.ProductionSourceId, Is.EqualTo("b-finite"));
        Assert.That(finite.ConfiguredQuantity, Is.EqualTo(3));
        Assert.That(finite.PlannedQuantity, Is.Zero);
        Assert.That(finite.AppliedQuantity, Is.Zero);
        Assert.That(finite.RejectionReason, Is.EqualTo("ReserveExhausted"));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.Zero);
        Assert.That(city.FiniteProductionSources.Source.Revision, Is.Zero);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(7));
    }

    [Test]
    public void P18ReceiptsCarryOccurrenceDaySourceOutcomesAndReplayWithoutReapplication()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-boundary-item");
        CityRuntime city = new CityRuntime("runtime.p14c.boundary",
            CreateCity("p14c-boundary-city", item, 5, 4f, reverseSourceRows: false),
            new SpatialLocationRuntime("legacy.p14c.boundary"));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world.p14c.boundary", "profile.p14c", 12L);
        Assert.That(city.TryCreateDailyEconomyStep(operation, 0, CityDailyEconomyStepKind.Production,
            out BoundaryContinuationStep productionStep, out _), Is.True);
        Assert.That(city.TryCreateDailyEconomyStep(operation, 1, CityDailyEconomyStepKind.Consumption,
            out BoundaryContinuationStep consumptionStep, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation,
            "daily-economy", "1", "p14c-config", new[] { productionStep, consumptionStep });

        IBoundaryContinuationStepCommit production = Prepare(city, manifest, productionStep);
        Assert.That(production.TryCommit(out TimelineFailure productionFailure), Is.True, productionFailure.ToString());
        Assert.That(city.TryResolveDailyEconomyReceipt(manifest, productionStep,
            out CityDailyEconomyReceipt productionReceipt, out _), Is.True);
        Assert.That(productionReceipt.BoundaryOccurrenceId, Is.EqualTo(manifest.BoundaryOccurrenceId));
        Assert.That(productionReceipt.AbsoluteDay, Is.EqualTo(12L));
        Assert.That(productionReceipt.OpeningStock, Is.EqualTo(5));
        Assert.That(productionReceipt.PostProductionStock, Is.EqualTo(10));
        Assert.That(productionReceipt.PostProductionMarketRevision, Is.EqualTo(2L));
        Assert.That(productionReceipt.SourceOutcomes.Count, Is.EqualTo(2));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(4));

        IBoundaryContinuationStepCommit consumption = Prepare(city, manifest, consumptionStep);
        Assert.That(consumption.TryCommit(out TimelineFailure consumptionFailure), Is.True, consumptionFailure.ToString());
        Assert.That(city.TryResolveDailyEconomyReceipt(manifest, consumptionStep,
            out CityDailyEconomyReceipt consumptionReceipt, out _), Is.True);
        Assert.That(consumptionReceipt.BoundaryOccurrenceId, Is.EqualTo(productionReceipt.BoundaryOccurrenceId));
        Assert.That(consumptionReceipt.AbsoluteDay, Is.EqualTo(productionReceipt.AbsoluteDay));
        Assert.That(consumptionReceipt.SourceOutcomes.Count, Is.EqualTo(2));
        Assert.That(consumptionReceipt.ActualFreeConsumption, Is.EqualTo(4));
        Assert.That(consumptionReceipt.ClosingStock, Is.EqualTo(6));
        Assert.That(consumptionReceipt.OpeningStock + consumptionReceipt.SourceOutcomes[0].AppliedQuantity
            + consumptionReceipt.SourceOutcomes[1].AppliedQuantity - consumptionReceipt.ActualFreeConsumption,
            Is.EqualTo(consumptionReceipt.ClosingStock));

        CityRuntime direct = new CityRuntime("runtime.p14c.boundary.direct",
            CreateCity("p14c-boundary-city", item, 5, 4f, reverseSourceRows: true),
            new SpatialLocationRuntime("legacy.p14c.boundary.direct"));
        SimulateDirect(direct, 12L);
        Assert.That(direct.Market.GetAmount(item), Is.EqualTo(city.Market.GetAmount(item)));
        Assert.That(direct.FiniteProductionSources.Source.RemainingReserve,
            Is.EqualTo(city.FiniteProductionSources.Source.RemainingReserve));
        Assert.That(direct.LastMaterialFlow.ClosingStock, Is.EqualTo(consumptionReceipt.ClosingStock));
        Assert.That(direct.LastMaterialFlow.SourceOutcomes.Count, Is.EqualTo(consumptionReceipt.SourceOutcomes.Count));
        for (int i = 0; i < direct.LastMaterialFlow.SourceOutcomes.Count; i++)
        {
            Assert.That(direct.LastMaterialFlow.SourceOutcomes[i].ProductionSourceId,
                Is.EqualTo(consumptionReceipt.SourceOutcomes[i].ProductionSourceId));
            Assert.That(direct.LastMaterialFlow.SourceOutcomes[i].AppliedQuantity,
                Is.EqualTo(consumptionReceipt.SourceOutcomes[i].AppliedQuantity));
        }

        long marketRevision = city.Market.Revision;
        int reserve = city.FiniteProductionSources.Source.RemainingReserve;
        IBoundaryContinuationStepCommit replay = Prepare(city, manifest, productionStep);
        Assert.That(replay.TryCommit(out _), Is.True);
        Assert.That(city.Market.Revision, Is.EqualTo(marketRevision));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(reserve));
    }

    [Test]
    public void StaleSourceConfigurationOrRejectedMarketAdmissionInstallsNothing()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-stale-item");
        CityData data = CreateCity("p14c-stale-city", item, 5, 0f, reverseSourceRows: false);
        CityRuntime city = new CityRuntime("runtime.p14c.stale", data,
            new SpatialLocationRuntime("legacy.p14c.stale"));
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world.p14c.stale", "profile.p14c", 9L);
        Assert.That(city.TryCreateDailyEconomyStep(operation, 0, CityDailyEconomyStepKind.Production,
            out BoundaryContinuationStep step, out _), Is.True);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(operation,
            "daily-economy", "1", "p14c-config", new[] { step });
        IBoundaryContinuationStepCommit prepared = Prepare(city, manifest, step);
        data.productionConfigs[0].amountPerDay++;
        Assert.That(prepared.TryCommit(out _), Is.False);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(5));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(7));
        Assert.That(city.FiniteProductionSources.Source.Revision, Is.Zero);

        CityRuntime rejected = new CityRuntime("runtime.p14c.rejected",
            CreateCity("p14c-rejected-city", item, 10, 0f, reverseSourceRows: false),
            new SpatialLocationRuntime("legacy.p14c.rejected"));
        rejected.Market.BindP12MutationBoundary(() => false, () => Assert.Fail("Rejected P14-C source mutation notified."));
        TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() => SimulateDirect(rejected, 10L));
        Assert.That(thrown.InnerException, Is.TypeOf<LocalDailyMaterialFlowRejectedException>());
        Assert.That(rejected.Market.GetAmount(item), Is.EqualTo(10));
        Assert.That(rejected.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(7));
        Assert.That(rejected.FiniteProductionSources.Source.Revision, Is.Zero);
        Assert.That(rejected.LastMaterialFlow, Is.Null);
    }

    [Test]
    public void PreparedP18ProductionRejectsStaleMarketAndFiniteOwnerRevisions()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-stale-owner-item");
        CityRuntime staleMarket = new CityRuntime("runtime.p14c.stale-market",
            CreateCity("p14c-stale-market-city", item, 10, 0f, reverseSourceRows: false),
            new SpatialLocationRuntime("legacy.p14c.stale-market"));
        BoundaryContinuationManifest marketManifest = CreateManifest(staleMarket, 11L,
            CityDailyEconomyStepKind.Production);
        BoundaryContinuationStep marketStep = marketManifest.Steps[0];
        IBoundaryContinuationStepCommit marketPrepared = Prepare(staleMarket, marketManifest, marketStep);
        Assert.That(staleMarket.Market.AddStock(item, 1), Is.EqualTo(1));
        Assert.That(marketPrepared.TryCommit(out _), Is.False);
        Assert.That(staleMarket.Market.GetAmount(item), Is.EqualTo(11));
        Assert.That(staleMarket.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(7));
        Assert.That(staleMarket.FiniteProductionSources.Source.Revision, Is.Zero);
        Assert.That(staleMarket.TryResolveDailyEconomyReceipt(marketManifest, marketStep, out _, out _), Is.False);

        CityRuntime staleSource = new CityRuntime("runtime.p14c.stale-source",
            CreateCity("p14c-stale-source-city", item, 10, 0f, reverseSourceRows: false),
            new SpatialLocationRuntime("legacy.p14c.stale-source"));
        BoundaryContinuationManifest sourceManifest = CreateManifest(staleSource, 11L,
            CityDailyEconomyStepKind.Production);
        BoundaryContinuationStep sourceStep = sourceManifest.Steps[0];
        IBoundaryContinuationStepCommit sourcePrepared = Prepare(staleSource, sourceManifest, sourceStep);
        SetPrivateLong(staleSource.FiniteProductionSources.Source, "revision", 1L);
        Assert.That(sourcePrepared.TryCommit(out _), Is.False);
        Assert.That(staleSource.Market.GetAmount(item), Is.EqualTo(10));
        Assert.That(staleSource.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(7));
        Assert.That(staleSource.TryResolveDailyEconomyReceipt(sourceManifest, sourceStep, out _, out _), Is.False);
    }

    private static CityData CreateCity(string id, ItemData item, int openingStock,
        float consumptionPer1000, bool reverseSourceRows)
    {
        CityData city = SimulationTestFactory.CreateCityData(id,
            SimulationTestFactory.CreateMarketItem(item, openingStock, 100));
        city.materialFlowProfile = LocalMaterialFlowProfile.MixedSourcesDaily;
        city.settlementSemanticId = "settlement." + id;
        city.materialFlowLocationId = "location." + id;
        city.marketStoreSemanticId = "store." + id;
        city.initialPopulation = 1000;
        city.populationConsumption = new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.Free };
        city.marketItems[0].consumptionPer1000Population = consumptionPer1000;
        CityProductionConfig exogenous = new CityProductionConfig
        {
            item = item, amountPerDay = 2, initialReserve = 0, productionSourceId = "a-exogenous",
            contentRevision = "content-exo-v1", sourceKind = CityProductionSourceKind.ExogenousDaily
        };
        CityProductionConfig finite = new CityProductionConfig
        {
            item = item, amountPerDay = 3, initialReserve = 7, productionSourceId = "b-finite",
            contentRevision = "content-finite-v1", sourceKind = CityProductionSourceKind.FiniteReserveDaily
        };
        if (reverseSourceRows)
        { city.productionConfigs.Add(finite); city.productionConfigs.Add(exogenous); }
        else
        { city.productionConfigs.Add(exogenous); city.productionConfigs.Add(finite); }
        return city;
    }

    private static void SimulateDirect(CityRuntime city, long day) =>
        typeof(CityRuntime).GetMethod("SimulateLocalDailyMaterialFlow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(city, new object[] { day, "calendar-v1" });

    private static IBoundaryContinuationStepCommit Prepare(CityRuntime city,
        BoundaryContinuationManifest manifest, BoundaryContinuationStep step)
    {
        Assert.That(city.TryPrepareDailyEconomyStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure), Is.True, failure.ToString());
        return prepared;
    }

    private static BoundaryContinuationManifest CreateManifest(CityRuntime city, long day,
        CityDailyEconomyStepKind kind)
    {
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world.p14c.stale", "profile.p14c", day);
        Assert.That(city.TryCreateDailyEconomyStep(operation, 0, kind,
            out BoundaryContinuationStep step, out _), Is.True);
        return new BoundaryContinuationManifest(operation, "daily-economy", "1", "p14c-config", new[] { step });
    }

    private static void SetPrivateLong(object target, string fieldName, long value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
