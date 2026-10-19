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
    public void SelectedDefinitionMixedSourceFlowUsesAdmittedCityValuesWithoutRetainedAuthoringCity()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-selected-item");
        CityData authoredCity = CreateCity(
            "p14c-selected-city", item, 5, 4f, reverseSourceRows: true);
        SimulationConfigData authoring = UnityEngine.ScriptableObject.CreateInstance<SimulationConfigData>();
        authoring.simulationName = "P14-C selected typed graph fixture";
        authoring.cities = new List<CityData> { authoredCity };

        try
        {
            SimulationConfigurationResolutionResult resolution = SimulationConfigurationResolver.Resolve(
                null, null, authoring.CreateConfigurationOverrides());
            Assert.That(resolution.IsValid, Is.True,
                string.Join("; ", resolution.Validation.Errors));
            CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(
                authoring.calendar, out string calendarDiagnostic);
            Assert.That(string.IsNullOrEmpty(calendarDiagnostic), Is.True, calendarDiagnostic);
            Assert.That(DailyV1DefinitionAdapter.TryCreate(authoring, resolution.Configuration,
                calendar, SimulationGenesisPipeline.ResolveStageOrder(includeAuthoredGeography: true),
                "axial-hex-v1", "q-then-r", out DailyV1EffectiveDefinitions definitions,
                out DailyV1DefinitionAdapter.SelectedBindings bindings, out string diagnostic),
                Is.True, diagnostic);
            Assert.That(definitions.Cities.TryGetValue(authoredCity.DefinitionId,
                out DailyV1CityDefinition cityDefinition), Is.True);
            Assert.That(bindings.TryGet(authoredCity, out DailyV1CityDefinition boundDefinition), Is.True);
            Assert.That(boundDefinition, Is.SameAs(cityDefinition));

            // After admission, the runtime must use the captured graph values;
            // the authoring object remains only an exact identity bridge.
            authoredCity.materialFlowProfile = LocalMaterialFlowProfile.ExogenousDaily;
            authoredCity.settlementSemanticId = "changed-after-admission";
            authoredCity.productionConfigs[0].amountPerDay = 999;
            authoredCity.productionConfigs[1].initialReserve = 999;
            authoredCity.marketItems[0].initialAmount = 999;
            authoredCity.marketItems[0].item = null;

            CityRuntime city = CityRuntime.CreateSelectedDailyV1(
                "runtime.p14c.selected", cityDefinition, definitions, bindings,
                new SpatialLocationRuntime("location.p14c.selected"));
            Assert.That(city.CityData, Is.SameAs(authoredCity),
                "P12 retains the exact authoring object only as its identity bridge.");
            DailyV1MarketOwner selectedMarket = city.Market.SelectedDailyV1Owner;
            Assert.That(selectedMarket, Is.Not.Null,
                "The selected City Market owner must be shared Core state, not an ItemData-backed duplicate.");
            Assert.That(selectedMarket.Definitions, Is.SameAs(definitions));
            Assert.That(selectedMarket.Items, Has.Count.EqualTo(1));
            Assert.That(selectedMarket.Items[0].Item, Is.SameAs(definitions.Items[item.DefinitionId]));
            FiniteProductionSourceState finiteProjection = city.FiniteProductionSources.Source;
            Assert.That(city.FiniteProductionSources.Source, Is.SameAs(finiteProjection),
                "The Unity compatibility view retains its stable object identity while Core owns the source state.");
            Assert.That(city.SelectedRuntimeOwners.FiniteSource.Item,
                Is.SameAs(definitions.Items[item.DefinitionId]));
            PreparedMarketState typedPrepared = city.Market.CreatePreparedSnapshot();
            Assert.That(typedPrepared.SelectedState, Is.Not.Null,
                "Selected prepared Market work must stay in the typed Core owner state rather than projecting through ItemData rows.");
            Assert.That(typedPrepared.Items, Is.Null);
            Assert.That(typedPrepared.SelectedState.ReadOnlyRows[0].Item,
                Is.SameAs(definitions.Items[item.DefinitionId]));

            ValidateSelectedCityAnchor(city, "location.p14c-selected-city");

            SimulateDirect(city, 3L);

            LocalDailyMaterialFlowResult flow = city.LastMaterialFlow;
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.OpeningStock, Is.EqualTo(5));
            Assert.That(flow.AppliedSourceQuantity, Is.EqualTo(5));
            Assert.That(flow.ActualFreeConsumption, Is.EqualTo(4));
            Assert.That(flow.ClosingStock, Is.EqualTo(6));
            Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(4));
            Assert.That(city.FiniteProductionSources.Source.Revision, Is.EqualTo(1));
            Assert.That(city.FiniteProductionSources.Source, Is.SameAs(finiteProjection));
            Assert.That(finiteProjection.RemainingReserve, Is.EqualTo(4));
            Assert.That(finiteProjection.Revision, Is.EqualTo(1));
            Assert.That(city.Market.GetAmount(item), Is.EqualTo(6));
            Assert.That(selectedMarket.Items[0].Item, Is.SameAs(definitions.Items[item.DefinitionId]));
            Assert.That(selectedMarket.Items[0].Amount, Is.EqualTo(6));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(authoring);
        }
    }

    [Test]
    public void SelectedFiniteReserveFlowUsesTheCoreCityOwnersAfterAuthoringItemsAreDetached()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14b-selected-finite-item", 4f);
        CityData authoredCity = CreateFiniteCity("p14b-selected-finite-city", item, 5, 4f);
        using (SelectedCityFixture fixture = CreateSelectedCityFixture(
            "p14b-selected-finite-city", authoredCity))
        {
            CityRuntime city = fixture.City;
            DailyV1CityRuntimeOwners exactOwners = city.SelectedRuntimeOwners;
            DailyV1FiniteProductionSourceOwner finiteSource = exactOwners.FiniteSource;
            DailyV1ItemDefinition admittedItem = fixture.Definitions.Items[item.DefinitionId];

            authoredCity.materialFlowProfile = LocalMaterialFlowProfile.ExogenousDaily;
            authoredCity.productionConfigs[0].item = null;
            authoredCity.productionConfigs[0].amountPerDay = 999;
            authoredCity.productionConfigs[0].initialReserve = 999;
            authoredCity.marketItems[0].item = null;
            item.itemName = "Changed after admission";

            SimulateDirect(city, 1L);

            LocalDailyMaterialFlowResult flow = city.LastMaterialFlow;
            Assert.That(city.SelectedRuntimeOwners, Is.SameAs(exactOwners));
            Assert.That(finiteSource.Item, Is.SameAs(admittedItem));
            Assert.That(flow.ItemDefinitionId, Is.EqualTo(admittedItem.DefinitionId));
            Assert.That(flow.OpeningStock, Is.EqualTo(5));
            Assert.That(flow.ConfiguredSourceQuantity, Is.EqualTo(3));
            Assert.That(flow.AppliedSourceQuantity, Is.EqualTo(3));
            Assert.That(flow.SourceRejectionReason, Is.Empty);
            Assert.That(flow.RequestedFreeConsumption, Is.EqualTo(4));
            Assert.That(flow.ActualFreeConsumption, Is.EqualTo(4));
            Assert.That(flow.ClosingStock, Is.EqualTo(4));
            Assert.That(finiteSource.RemainingReserve, Is.EqualTo(4));
            Assert.That(finiteSource.Revision, Is.EqualTo(1));
            Assert.That(finiteSource.LastProcessedDay, Is.EqualTo(1));
            Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(admittedItem), Is.EqualTo(4));
        }
    }

    [Test]
    public void SelectedMixedProductionReceiptUsesAdmittedGraphAfterAuthoringItemsAreDetached()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-selected-receipt-item");
        using (SelectedCityFixture fixture = CreateSelectedMixedCity(
            "p14c-selected-receipt-city", item, 5, 0f))
        {
            CityRuntime city = fixture.City;
            CityData authored = fixture.Authoring.cities[0];
            authored.materialFlowProfile = LocalMaterialFlowProfile.ExogenousDaily;
            foreach (CityProductionConfig source in authored.productionConfigs)
            {
                source.item = null;
                source.productionSourceId = "mutated-after-admission";
                source.amountPerDay = 999;
            }
            authored.marketItems[0].item = null;

            ValidateSelectedCityAnchor(city, "location.p14c-selected-receipt-city");
            BoundaryContinuationManifest manifest = CreateSelectedManifest(
                city, CityDailyEconomyStepKind.Production);
            Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0],
                out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure), Is.True,
                prepareFailure.ToString());
            Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True,
                commitFailure.ToString());
            Assert.That(city.TryResolveDailyEconomyReceipt(manifest, manifest.Steps[0],
                out CityDailyEconomyReceipt receipt, out TimelineFailure receiptFailure), Is.True,
                receiptFailure.ToString());

            Assert.That(receipt.ProductionResults, Has.Count.EqualTo(2));
            Assert.That(receipt.ProductionResults, Has.All.Property(nameof(CityProductionResult.ItemDefinitionId))
                .EqualTo(item.DefinitionId));
            Assert.That(receipt.SourceOutcomes, Has.Count.EqualTo(2));
            Assert.That(city.Market.SelectedDailyV1Owner.Items[0].Item,
                Is.SameAs(fixture.Definitions.Items[item.DefinitionId]));
        }
    }

    [TestCase(CityDailyEconomyStepKind.PriceRefresh)]
    [TestCase(CityDailyEconomyStepKind.Consumption)]
    public void SelectedNoOpDailyStepRecordsReceiptWithoutInstallingOrInvalidatingMarket(
        CityDailyEconomyStepKind kind)
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-selected-noop-price-item");
        using (SelectedCityFixture fixture = CreateSelectedCity(
            "p14-selected-noop-price-city", item, 5, 3, 0f))
        {
            CityRuntime city = fixture.City;
            DailyV1MarketOwner owner = city.Market.SelectedDailyV1Owner;
            DailyV1MarketItemState originalRow = owner.Items[0];
            int admissions = 0;
            int notifications = 0;
            city.Market.BindP12MutationBoundary(() => { admissions++; return true; }, () => notifications++);
            BoundaryContinuationManifest manifest = CreateSelectedManifest(city, kind);

            Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0],
                out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure), Is.True,
                prepareFailure.ToString());
            Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True,
                commitFailure.ToString());

            Assert.That(city.TryResolveDailyEconomyReceipt(manifest, manifest.Steps[0], out _, out _), Is.True);
            Assert.That(city.Market.Revision, Is.Zero);
            Assert.That(owner.Items[0], Is.SameAs(originalRow),
                "A no-op prepared replacement must not replace the authoritative row instance.");
            Assert.That(admissions, Is.Zero);
            Assert.That(notifications, Is.Zero);
        }
    }

    [Test]
    public void SelectedPreparedMarketWriteRejectsAdmissionAtPrepareAndCommitWithoutInstalling()
    {
        ItemData prepareDeniedItem = SimulationTestFactory.CreateItem("p14-selected-deny-prepare-item");
        using (SelectedCityFixture fixture = CreateSelectedCity(
            "p14-selected-deny-prepare-city", prepareDeniedItem, 4, 5, 0f))
        {
            CityRuntime city = fixture.City;
            int admissions = 0;
            int notifications = 0;
            city.Market.BindP12MutationBoundary(() => { admissions++; return false; }, () => notifications++);
            BoundaryContinuationManifest manifest = CreateSelectedManifest(
                city, CityDailyEconomyStepKind.Production);

            Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0],
                out IBoundaryContinuationStepCommit prepared, out _), Is.False);
            Assert.That(prepared, Is.Null);
            Assert.That(admissions, Is.EqualTo(1));
            Assert.That(notifications, Is.Zero);
            Assert.That(city.Market.Revision, Is.Zero);
            Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(
                fixture.Definitions.Items[prepareDeniedItem.DefinitionId]), Is.EqualTo(4));
            Assert.That(city.TryResolveDailyEconomyReceipt(manifest, manifest.Steps[0], out _, out _), Is.False);
        }

        ItemData commitDeniedItem = SimulationTestFactory.CreateItem("p14-selected-deny-commit-item");
        using (SelectedCityFixture fixture = CreateSelectedCity(
            "p14-selected-deny-commit-city", commitDeniedItem, 4, 5, 0f))
        {
            CityRuntime city = fixture.City;
            bool admitted = true;
            int admissions = 0;
            int notifications = 0;
            city.Market.BindP12MutationBoundary(() => { admissions++; return admitted; }, () => notifications++);
            BoundaryContinuationManifest manifest = CreateSelectedManifest(
                city, CityDailyEconomyStepKind.Production);

            Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0],
                out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure), Is.True,
                prepareFailure.ToString());
            admitted = false;
            Assert.That(prepared.TryCommit(out _), Is.False);

            Assert.That(admissions, Is.EqualTo(2));
            Assert.That(notifications, Is.Zero);
            Assert.That(city.Market.Revision, Is.Zero);
            Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(
                fixture.Definitions.Items[commitDeniedItem.DefinitionId]), Is.EqualTo(4));
            Assert.That(city.TryResolveDailyEconomyReceipt(manifest, manifest.Steps[0], out _, out _), Is.False);
        }
    }

    [Test]
    public void SelectedPreparedMarketWriteNotifiesExactlyOnceAfterSuccessfulInstall()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-selected-success-item");
        using (SelectedCityFixture fixture = CreateSelectedCity(
            "p14-selected-success-city", item, 4, 5, 0f))
        {
            CityRuntime city = fixture.City;
            List<string> observations = new List<string>();
            city.Market.BindP12MutationBoundary(
                () => { observations.Add("admit"); return true; },
                () => observations.Add("commit:" + city.Market.Revision + ":"
                    + city.Market.SelectedDailyV1Owner.GetAmount(fixture.Definitions.Items[item.DefinitionId])));
            BoundaryContinuationManifest manifest = CreateSelectedManifest(
                city, CityDailyEconomyStepKind.Production);

            Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0],
                out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure), Is.True,
                prepareFailure.ToString());
            Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True,
                commitFailure.ToString());

            Assert.That(observations, Is.EqualTo(new[] { "admit", "admit", "commit:1:9" }));
            Assert.That(city.Market.Revision, Is.EqualTo(1));
            Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(
                fixture.Definitions.Items[item.DefinitionId]), Is.EqualTo(9));
            Assert.That(city.TryResolveDailyEconomyReceipt(manifest, manifest.Steps[0], out _, out _), Is.True);
        }
    }

    [Test]
    public void DirectSelectedNonMixedFlowUsesOwnerAdmissionAndEpochNotificationForEachWrite()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-selected-direct-boundary-item");
        using (SelectedCityFixture fixture = CreateSelectedCity(
            "p14-selected-direct-boundary-city", item, 0, 5, 3f))
        {
            CityRuntime city = fixture.City;
            DailyV1ItemDefinition itemNode = fixture.Definitions.Items[item.DefinitionId];
            List<string> observations = new List<string>();
            city.Market.BindP12MutationBoundary(
                () => { observations.Add("admit:" + city.Market.Revision); return true; },
                () => observations.Add("commit:" + city.Market.Revision + ":"
                    + city.Market.SelectedDailyV1Owner.GetAmount(itemNode)));

            SimulateDirect(city, 3L);

            Assert.That(city.LastMaterialFlow.OpeningStock, Is.Zero);
            Assert.That(city.LastMaterialFlow.AppliedSourceQuantity, Is.EqualTo(5));
            Assert.That(city.LastMaterialFlow.ActualFreeConsumption, Is.EqualTo(3));
            Assert.That(city.LastMaterialFlow.ClosingStock, Is.EqualTo(2));
            Assert.That(city.Market.Revision, Is.EqualTo(2));
            Assert.That(observations, Is.EqualTo(new[]
            {
                "admit:0", "commit:1:5", "admit:1", "commit:2:2"
            }));
        }
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
    public void SelectedMixedOverflowCommitsExogenousContributionWhenFiniteOwnerCannotAdvance()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14-selected-mixed-overflow-max-revision-item");
        using (SelectedCityFixture fixture = CreateSelectedMixedCity(
            "p14-selected-mixed-overflow-max-revision-city", item, int.MaxValue - 3, 0f))
        {
            CityRuntime city = fixture.City;
            DailyV1ItemDefinition itemNode = fixture.Definitions.Items[item.DefinitionId];
            FiniteProductionSourceState finite = city.FiniteProductionSources.Source;
            SetPrivateLong(finite, "revision", long.MaxValue);

            List<string> notifications = new List<string>();
            city.Market.BindP12MutationBoundary(() => true, () => notifications.Add(
                city.Market.Revision + ":" + city.Market.SelectedDailyV1Owner.GetAmount(itemNode)));
            BoundaryContinuationManifest manifest = CreateSelectedManifest(
                city, CityDailyEconomyStepKind.Production);

            Assert.That(city.TryPrepareDailyEconomyStep(manifest, manifest.Steps[0],
                out IBoundaryContinuationStepCommit prepared, out TimelineFailure prepareFailure), Is.True,
                prepareFailure.ToString());
            Assert.That(prepared.TryCommit(out TimelineFailure commitFailure), Is.True,
                commitFailure.ToString());

            Assert.That(city.TryResolveDailyEconomyReceipt(manifest, manifest.Steps[0],
                out CityDailyEconomyReceipt receipt, out _), Is.True);
            Assert.That(receipt.SourceOutcomes.Count, Is.EqualTo(2));
            Assert.That(receipt.SourceOutcomes[0].ProductionSourceId, Is.EqualTo("a-exogenous"));
            Assert.That(receipt.SourceOutcomes[0].AppliedQuantity, Is.EqualTo(2));
            Assert.That(receipt.SourceOutcomes[1].ProductionSourceId, Is.EqualTo("b-finite"));
            Assert.That(receipt.SourceOutcomes[1].PlannedQuantity, Is.EqualTo(3));
            Assert.That(receipt.SourceOutcomes[1].AppliedQuantity, Is.Zero);
            Assert.That(receipt.SourceOutcomes[1].RejectionReason, Is.EqualTo("AggregateStockOverflow"));

            Assert.That(city.Market.SelectedDailyV1Owner.GetAmount(itemNode), Is.EqualTo(int.MaxValue - 1));
            Assert.That(city.Market.Revision, Is.EqualTo(1));
            Assert.That(finite.RemainingReserve, Is.EqualTo(7));
            Assert.That(finite.Revision, Is.EqualTo(long.MaxValue));
            Assert.That(notifications, Is.EqualTo(new[] { "1:" + (int.MaxValue - 1) }),
                "The accepted exogenous contribution installs atomically and emits one post-install Market callback.");
        }
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

    [Test]
    public void PreparedP18ProductionRejectsStaleFiniteOwnerWhenFiniteSourceOverflowed()
    {
        ItemData item = SimulationTestFactory.CreateItem("p14c-overflow-stale-source-item");
        CityRuntime city = new CityRuntime("runtime.p14c.overflow-stale-source",
            CreateCity("p14c-overflow-stale-source-city", item, int.MaxValue - 2, 0f, reverseSourceRows: false),
            new SpatialLocationRuntime("legacy.p14c.overflow-stale-source"));
        BoundaryContinuationManifest manifest = CreateManifest(city, 12L,
            CityDailyEconomyStepKind.Production);
        BoundaryContinuationStep step = manifest.Steps[0];

        IBoundaryContinuationStepCommit prepared = Prepare(city, manifest, step);
        SetPrivateLong(city.FiniteProductionSources.Source, "revision", 1L);

        Assert.That(prepared.TryCommit(out _), Is.False);
        Assert.That(city.Market.GetAmount(item), Is.EqualTo(int.MaxValue - 2));
        Assert.That(city.FiniteProductionSources.Source.RemainingReserve, Is.EqualTo(7));
        Assert.That(city.TryResolveDailyEconomyReceipt(manifest, step, out _, out _), Is.False);
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

    private static CityData CreateFiniteCity(string id, ItemData item, int openingStock,
        float consumptionPer1000)
    {
        CityData city = SimulationTestFactory.CreateCityData(id,
            SimulationTestFactory.CreateMarketItem(item, openingStock, 100));
        city.materialFlowProfile = LocalMaterialFlowProfile.FiniteReserveDaily;
        city.settlementSemanticId = "settlement." + id;
        city.materialFlowLocationId = "location." + id;
        city.marketStoreSemanticId = "store." + id;
        city.initialPopulation = 1000;
        city.populationConsumption = new PopulationConsumptionConfig { paymentMode = ConsumptionPaymentMode.Free };
        city.marketItems[0].consumptionPer1000Population = consumptionPer1000;
        city.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = 3,
            initialReserve = 7,
            productionSourceId = "source." + id,
            contentRevision = "content-finite-v1",
            sourceKind = CityProductionSourceKind.FiniteReserveDaily
        });
        return city;
    }

    private static SelectedCityFixture CreateSelectedCity(string id, ItemData item,
        int openingStock, int productionPerDay, float consumptionPer1000)
    {
        CityData authoredCity = SimulationTestFactory.CreateCityData(id,
            SimulationTestFactory.CreateMarketItem(item, openingStock, 100));
        authoredCity.materialFlowProfile = LocalMaterialFlowProfile.ExogenousDaily;
        authoredCity.initialPopulation = 1000;
        authoredCity.populationConsumption = new PopulationConsumptionConfig
        { paymentMode = ConsumptionPaymentMode.Free };
        authoredCity.marketItems[0].consumptionPer1000Population = consumptionPer1000;
        authoredCity.productionConfigs.Add(new CityProductionConfig
        {
            item = item,
            amountPerDay = productionPerDay,
            initialReserve = 0,
            productionSourceId = "source." + id,
            contentRevision = "content-v1",
            sourceKind = CityProductionSourceKind.ExogenousDaily
        });

        return CreateSelectedCityFixture(id, authoredCity);
    }

    private static SelectedCityFixture CreateSelectedMixedCity(string id, ItemData item,
        int openingStock, float consumptionPer1000)
    {
        return CreateSelectedCityFixture(id,
            CreateCity(id, item, openingStock, consumptionPer1000, reverseSourceRows: true));
    }

    private static SelectedCityFixture CreateSelectedCityFixture(string id, CityData authoredCity)
    {
        SimulationConfigData authoring = UnityEngine.ScriptableObject.CreateInstance<SimulationConfigData>();
        authoring.simulationName = "Selected Daily-v1 P12 mutation fixture";
        authoring.cities = new List<CityData> { authoredCity };
        try
        {
            SimulationConfigurationResolutionResult resolution = SimulationConfigurationResolver.Resolve(
                null, null, authoring.CreateConfigurationOverrides());
            if (!resolution.IsValid)
                throw new InvalidOperationException(string.Join("; ", resolution.Validation.Errors));
            CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(
                authoring.calendar, out string calendarDiagnostic);
            if (!string.IsNullOrEmpty(calendarDiagnostic))
                throw new InvalidOperationException(calendarDiagnostic);
            if (!DailyV1DefinitionAdapter.TryCreate(authoring, resolution.Configuration, calendar,
                    SimulationGenesisPipeline.ResolveStageOrder(includeAuthoredGeography: true),
                    "axial-hex-v1", "q-then-r", out DailyV1EffectiveDefinitions definitions,
                    out DailyV1DefinitionAdapter.SelectedBindings bindings, out string diagnostic))
                throw new InvalidOperationException(diagnostic);
            if (!bindings.TryGet(authoredCity, out DailyV1CityDefinition cityDefinition))
                throw new InvalidOperationException("Selected City binding was not published.");
            CityRuntime city = CityRuntime.CreateSelectedDailyV1("runtime." + id,
                cityDefinition, definitions, bindings, new SpatialLocationRuntime("location." + id));
            return new SelectedCityFixture(authoring, definitions, city);
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(authoring);
            throw;
        }
    }

    private static BoundaryContinuationManifest CreateSelectedManifest(CityRuntime city,
        CityDailyEconomyStepKind kind)
    {
        DailyBoundaryOperation operation = new DailyBoundaryOperation("world.selected", "profile.selected", 1L);
        if (!city.TryCreateDailyEconomyStep(operation, 0, kind,
                out BoundaryContinuationStep step, out TimelineFailure failure))
            throw new InvalidOperationException(failure.ToString());
        return new BoundaryContinuationManifest(operation, "daily-economy", "1", "selected-config", new[] { step });
    }

    private static void ValidateSelectedCityAnchor(CityRuntime city, string locationId)
    {
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId hexId = new HexId("hex." + city.RuntimeId);
        Assert.That(spatial.TryRegisterHex(new HexRecord(hexId), out SpatialAuthorityFailure hexFailure), Is.True,
            hexFailure.ToString());
        Assert.That(spatial.TryRegisterLocation(new LocationRecord(new LocationId(locationId), hexId),
            out SpatialAuthorityFailure locationFailure), Is.True, locationFailure.ToString());
        LegacySpatialAnchorBindingStore bindings = new LegacySpatialAnchorBindingStore(spatial);
        Assert.That(bindings.TryBindCity(city.RuntimeId, new LocationId(locationId),
            out SpatialAnchorBindingFailure bindingFailure), Is.True, bindingFailure.ToString());
        Assert.DoesNotThrow(() => city.ValidateLocalDailyMaterialFlowAnchor(bindings, spatial));
    }

    private sealed class SelectedCityFixture : IDisposable
    {
        internal readonly SimulationConfigData Authoring;
        internal readonly DailyV1EffectiveDefinitions Definitions;
        internal readonly CityRuntime City;

        internal SelectedCityFixture(SimulationConfigData authoring,
            DailyV1EffectiveDefinitions definitions, CityRuntime city)
        { Authoring = authoring; Definitions = definitions; City = city; }

        public void Dispose() => UnityEngine.Object.DestroyImmediate(Authoring);
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
