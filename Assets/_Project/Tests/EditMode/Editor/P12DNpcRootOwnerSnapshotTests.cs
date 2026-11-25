using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class P12DNpcRootOwnerSnapshotTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void DetachedP12HydrationBindsTheStagedSpatialCoreOwnerIntoSelectedComposition()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition selectedDefinition = definitions.Npcs.Values.First();
        List<DailyV1StatusDefinition> selectedStatuses = selectedDefinition.Values.statusPadrao
            .Select(statusId => definitions.Statuses[statusId]).ToList();
        NpcData npcData = SimulationTestFactory.CreateNpc(selectedDefinition.DefinitionId);
        npcData.statusPadrao.Clear();
        List<NpcStatusData> statusAssets = new List<NpcStatusData>();
        foreach (DailyV1StatusDefinition selectedStatus in selectedStatuses)
        {
            NpcStatusData statusAsset = SimulationTestFactory.CreateStatus(selectedStatus.DefinitionId);
            npcData.statusPadrao.Add(statusAsset);
            statusAssets.Add(statusAsset);
        }

        Assert.That(SpatialKnowledgeRuntime.TryCreateFromOwnerSnapshot(
            "npc-detached-spatial", new[] { "location/second", "location/first" },
            new[] { "route/second", "route/first" }, 4L,
            out SpatialKnowledgeRuntime stagedSpatialOwner), Is.True);
        DailyV1InventoryOwner selectedInventoryOwner = new DailyV1InventoryOwner(definitions);
        DailyV1CommercialKnowledgeOwner selectedCommercialOwner =
            new DailyV1CommercialKnowledgeOwner(definitions);
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
            definitions, out DailyV1NpcMerchantTradePlanOwner selectedTradePlanOwner), Is.True);

        P12DNpcStagedOwnerState stagedState = new P12DNpcStagedOwnerState
        {
            NpcDefinitionId = selectedDefinition.DefinitionId,
            SelectedDefinitions = definitions,
            SelectedNpcDefinition = selectedDefinition,
            SelectedCurrentStatusDefinitions = selectedStatuses,
            NpcData = npcData,
            LifeState = NpcLifeState.Alive,
            InjurySeverity = NpcInjurySeverity.None,
            CurrentStatus = statusAssets,
            Inventory = new InventoryRuntime(selectedInventoryOwner),
            MoneyAccount = new MoneyAccountRuntime(),
            MerchantTradePlan = new MerchantTradePlanRuntime(),
            SelectedMerchantTradePlanOwner = selectedTradePlanOwner,
            TravelPlan = new NpcTravelPlanRuntime(),
            CommercialKnowledge = new CommercialKnowledgeRuntime(selectedCommercialOwner, null),
            SpatialKnowledge = stagedSpatialOwner,
            ExplorableSiteKnowledge = new ExplorableSiteKnowledgeRuntime("npc-detached-spatial"),
            LocalTopologyKnowledge = new LocalTopologyKnowledgeRuntime("npc-detached-spatial"),
            AdventureSiteIntelKnowledge = new AdventureSiteIntelKnowledgeRuntime("npc-detached-spatial")
        };
        NpcRuntime restored = new NpcRuntime("npc-detached-spatial", npcData);

        Assert.That(restored.TryInstallP12OwnerSnapshot(stagedState), Is.True);
        Assert.That(restored.SelectedRuntimeOwners, Is.Not.Null);
        Assert.That(restored.SelectedRuntimeOwners.SpatialKnowledge, Is.SameAs(stagedSpatialOwner));
        Assert.That(restored.SpatialKnowledge, Is.SameAs(stagedSpatialOwner));
        Assert.That(restored.ExistingSpatialKnowledge, Is.SameAs(stagedSpatialOwner));
        Assert.That(restored.SpatialKnowledge.KnownLocationRuntimeIds,
            Is.EqualTo(new[] { "location/second", "location/first" }));
        Assert.That(restored.SpatialKnowledge.KnownRouteRuntimeIds,
            Is.EqualTo(new[] { "route/second", "route/first" }));
        Assert.That(restored.SpatialKnowledge.Revision, Is.EqualTo(4L));

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SpatialKnowledgeCensusProvider.CreateProviders(new[] { restored });
        Assert.That(providers[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(stagedSpatialOwner));
        Assert.That(providers[1].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(stagedSpatialOwner));
        Assert.That(providers[0].GetCurrentCensus().Revision, Is.EqualTo(4L));
        Assert.That(providers[1].GetCurrentCensus().Revision, Is.EqualTo(4L));
    }

    [Test]
    public void SelectedImageStageHydratesCoreNpcOwnerBeforeBindingUnityProjection()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npcNode = definitions.Npcs.Values.First();
        DailyV1ActionDefinition actionNode = definitions.Actions.Values.First();
        DailyV1ItemDefinition itemNode = definitions.Items.Values.First();
        string runtimeId = "p12d-core-first-npc";

        NpcData npcAsset = SimulationTestFactory.CreateNpc(npcNode.DefinitionId);
        NpcActionData actionAsset = SimulationTestFactory.CreateAction(
            actionNode.DefinitionId, NpcActionType.Normal);
        ItemData itemAsset = SimulationTestFactory.CreateItem(itemNode.DefinitionId, 1f);
        List<string> statusIds = (npcNode.Values.statusPadrao ?? Array.Empty<string>()).ToList();
        List<NpcStatusData> statusAssets = statusIds
            .Select(SimulationTestFactory.CreateStatus).ToList();

        P12DNpcDRow d = new P12DNpcDRow(runtimeId, npcNode.DefinitionId,
            null, null, NpcLifeState.Alive, NpcInjurySeverity.None, statusIds,
            0, actionNode.DefinitionId, null, null, null, null,
            new[] { new P12DNpcInventoryRowValue(itemNode.DefinitionId, 3, 2.5f) },
            4L, 17.5f, 6L, 2L, 0L, 3L, 5L);
        P12DNpcFRow f = new P12DNpcFRow(runtimeId,
            new P12DNpcActionValue(actionNode.DefinitionId, null, null, itemNode.DefinitionId,
                2, 1.25f, 0.75f, NpcTravelReason.None, 0f,
                "p12d-core-first-decision", "p12d-core-first-occurrence", null, null),
            0, 0, null, false, null, null, 7L,
            new P12DNpcMerchantPlanValue(itemNode.DefinitionId, null, null, 0, 0, 0f, 0, 0, null, 0L),
            new P12DNpcTravelPlanValue(null, null, NpcTravelReason.None, 0f, 0f, null, 8L),
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(),
            Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(),
            Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(),
            Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);
        P12AOwnerSection[] receiptSections =
        {
            new P12AOwnerSection(
                P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionIdFor(runtimeId),
                P12DNpcReceiptOwnerCensusProvider.SchemaVersion, 0,
                OwnerSectionRole.Required, null, 0L, Array.Empty<P12AOwnerRow>()),
            new P12AOwnerSection(
                P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionIdFor(runtimeId),
                P12DNpcReceiptOwnerCensusProvider.SchemaVersion, 0,
                OwnerSectionRole.Required, null, 0L, Array.Empty<P12AOwnerRow>())
        };

        Assert.That(P12DNpcRootOwnerSnapshot.TryStageForImage(
            new[] { d }, new[] { f }, Array.Empty<P12DCityMembershipLinker>(),
            new PersonStore(), new[] { npcAsset }, new[] { actionAsset }, statusAssets,
            new[] { itemAsset }, Array.Empty<SpatialLocationRuntime>(),
            Array.Empty<string>(), Array.Empty<string>(), receiptSections,
            out IReadOnlyList<NpcRuntime> staged, out P12DNpcRootOwnerSnapshotFailure failure,
            definitions), Is.True, failure.ToString());

        NpcRuntime restored = staged.Single();
        DailyV1NpcRuntimeOwners owners = restored.SelectedRuntimeOwners;
        Assert.That(owners, Is.Not.Null);
        Assert.That(restored.NpcData, Is.SameAs(npcAsset));
        Assert.That(owners.Definitions, Is.SameAs(definitions));
        Assert.That(owners.Definition, Is.SameAs(npcNode));
        Assert.That(owners.Inventory.Revision, Is.EqualTo(4L));
        Assert.That(restored.Inventory.SelectedDailyV1Owner, Is.SameAs(owners.Inventory));
        Assert.That(restored.MoneyAccount, Is.SameAs(owners.MoneyAccount));
        Assert.That(restored.CommercialKnowledge.SelectedOwner, Is.SameAs(owners.CommercialKnowledge));
        Assert.That(restored.MerchantTradePlan.SelectedOwner, Is.SameAs(owners.MerchantTradePlan));
        Assert.That(restored.MerchantTradePlan.Item, Is.SameAs(itemAsset));
        Assert.That(restored.SpatialKnowledge, Is.SameAs(owners.SpatialKnowledge));
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.OwnerRuntimeId, Is.EqualTo(runtimeId));
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.PlaceObservationCount, Is.Zero);
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.ConnectionObservationCount, Is.Zero);
        Assert.That(owners.LocalTopologyKnowledgeEmptyOwner.Revision, Is.Zero);
        Assert.That(restored.LocalTopologyKnowledge.OwnerRuntimeId, Is.EqualTo(runtimeId));
        Assert.That(restored.LocalTopologyKnowledge.PlaceObservations, Is.Empty);
        Assert.That(restored.LocalTopologyKnowledge.ConnectionObservations, Is.Empty);
        Assert.That(owners.ActionSlot.Revision, Is.EqualTo(5L));
        Assert.That(restored.CurrentActionRuntime.SelectedOwner,
            Is.SameAs(owners.ActionSlot.CurrentPayload));
        Assert.That(owners.ActionSlot.CurrentPayload.Amount, Is.EqualTo(2));
        Assert.That(owners.ActionSlot.CurrentPayload.TargetItem, Is.SameAs(itemNode));
        Assert.That(owners.Travel.Revision, Is.EqualTo(7L));
        Assert.That(owners.TravelPlan.Revision, Is.EqualTo(8L));

        UnityEngine.ScriptableObject[] selectedAssets = new UnityEngine.ScriptableObject[] { npcAsset, actionAsset, itemAsset }
            .Concat(statusAssets).ToArray();
        Assert.That(DailyV1DefinitionAdapter.SelectedBindings.TryCreate(
            selectedAssets, definitions,
            out DailyV1DefinitionAdapter.SelectedBindings fullBindings, out string diagnostic),
            Is.True, diagnostic);
        Assert.That(restored.TryBindSelectedDailyV1Definition(
            npcNode, definitions, fullBindings), Is.True);
        Assert.That(restored.NpcData, Is.SameAs(npcAsset));
        Assert.That(restored.MerchantTradePlan.SelectedOwner, Is.SameAs(owners.MerchantTradePlan));
    }

    [TestCase("")]
    [TestCase("npc-spatial-owner-other")]
    public void CaptureRejectsBlankOrMismatchedSerializedSpatialOwnerIdentity(string serializedOwnerRuntimeId)
    {
        NpcRuntime npc = new NpcRuntime("npc-spatial-owner-capture",
            SimulationTestFactory.CreateNpc("p12d-spatial-owner-capture-definition"));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        SetSerializedSpatialOwnerIdentityAndDeserialize(npc, serializedOwnerRuntimeId);

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(d, Is.Null);
        Assert.That(f, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }

    [Test]
    public void DailyV1Package_StagesPersonFirstAndPreservesOneOrderedCityNpcGraph()
    {
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12d-owner-package-city", "p12d-owner-package-location");
        CityRuntime destination = SimulationTestFactory.CreateCity(
            "p12d-owner-package-destination", "p12d-owner-package-destination-location");
        NpcRuntime npc = new NpcRuntime(
            "p12d-owner-package-npc",
            SimulationTestFactory.CreateNpc("p12d-owner-package-npc-definition"), city, 0f);
        DailyFixture fixture = CreateDailyFixture(city, npc, destination);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        RuntimeIdentityRegistry stagedIdentities = new RuntimeIdentityRegistry();

        long sourceMembershipRevision = city.ImportantNpcRevision;
        long sourcePersonRevision = fixture.Runtime.PersonStore.Revision;
        long sourceGenealogyRevision = fixture.Runtime.GenealogyStoreForWorldBoundary.Revision;
        long sourceSpatialRevision = fixture.SpatialNetwork.Revision;
        Assert.That(TryStageDailyPackage(
            fixture, token, stagedIdentities,
            out P12DDailyV1OwnerPackage package,
            out P12DDailyV1OwnerPackageFailure failure), Is.True, failure.ToString());

        Assert.That(package, Is.Not.Null);
        Assert.That(package.WorldId, Is.SameAs(token.WorldId));
        Assert.That(package.Cities, Has.Count.EqualTo(fixture.Cities.Length));
        Assert.That(package.Npcs, Has.Count.EqualTo(fixture.Npcs.Length));
        Assert.That(package.NpcFRows, Has.Count.EqualTo(fixture.Npcs.Length));
        Assert.That(package.NpcFRows.Single(value => value.RuntimeId == npc.RuntimeId), Is.Not.SameAs(npc));
        Assert.That(package.Cities[0], Is.Not.SameAs(fixture.Cities[0]));
        Assert.That(package.Npcs.Single(value => value.RuntimeId == npc.RuntimeId), Is.Not.SameAs(npc));
        Assert.That(package.Cities[0].ImportantNpcRevision, Is.EqualTo(sourceMembershipRevision));
        Assert.That(package.Cities[0].ImportantNpcs.Select(value => value.RuntimeId),
            Is.EqualTo(city.ImportantNpcs.Select(value => value.RuntimeId)));
        Assert.That(package.Cities[0].ImportantNpcs.Single(), Is.SameAs(
            package.Npcs.Single(value => value.RuntimeId == npc.RuntimeId)));
        Assert.That(package.Persons.Revision, Is.EqualTo(sourcePersonRevision));
        Assert.That(package.Genealogy.Revision, Is.EqualTo(sourceGenealogyRevision));
        Assert.That(package.SpatialNetwork.Revision, Is.EqualTo(sourceSpatialRevision));
        Assert.That(package.EmptyExplorableSites.Count, Is.Zero);
        Assert.That(package.EmptyExplorableSites.Revision, Is.Zero);
        Assert.That(package.RuntimeIdentities.CensusRevision, Is.EqualTo(
            fixture.Identities.CensusRevision));
        Assert.That(city.ImportantNpcRevision, Is.EqualTo(sourceMembershipRevision));
        Assert.That(fixture.Runtime.PersonStore.Revision, Is.EqualTo(sourcePersonRevision));
        Assert.That(fixture.Runtime.GenealogyStoreForWorldBoundary.Revision,
            Is.EqualTo(sourceGenealogyRevision));
        Assert.That(fixture.SpatialNetwork.Revision, Is.EqualTo(sourceSpatialRevision));
    }

    [Test]
    public void DailyV1Package_PreflightsCityLocationReciprocityBeforeAnyMembershipFill()
    {
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12d-owner-package-invalid-city", "p12d-owner-package-invalid-location");
        CityRuntime destination = SimulationTestFactory.CreateCity(
            "p12d-owner-package-invalid-destination", "p12d-owner-package-invalid-destination-location");
        NpcRuntime npc = new NpcRuntime(
            "p12d-owner-package-invalid-npc",
            SimulationTestFactory.CreateNpc("p12d-owner-package-invalid-npc-definition"), city, 0f);
        DailyFixture fixture = CreateDailyFixture(city, npc, destination);
        long sourceRevision = city.ImportantNpcRevision;
        int sourceMemberCount = city.ImportantNpcs.Count;
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        object stamp = new object();
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            fixture.Runtime, token, stamp, token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f, out _), Is.True);

        List<P12DCityRootOwnerSnapshot.StagingCaptureEnvelope> cityCaptures =
            new List<P12DCityRootOwnerSnapshot.StagingCaptureEnvelope>();
        Dictionary<string, string> cityLocationIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (CityRuntime sourceCity in fixture.Cities)
        {
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(
                sourceCity, token, stamp, token.OwnerSections,
                out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture, out _), Is.True);
            cityCaptures.Add(cityCapture);
            cityLocationIds.Add(cityCapture.Snapshot.CityRuntimeId,
                cityCapture.Snapshot.LegacyLocationRuntimeId);
        }

        Dictionary<string, SpatialLocationRuntime> locationsById = fixture.SpatialNetwork.Locations
            .ToDictionary(value => value.RuntimeId, StringComparer.Ordinal);
        Assert.That(P12DDailyV1OwnerPackage.TryPreflightRelations(
            fixture.Runtime.PersonStore.CaptureOwnerSnapshot(),
            new GenealogyOwnerSnapshot(GenealogyOwnerBridge.CaptureOwnerSnapshot(
                fixture.Runtime.GenealogyStoreForWorldBoundary)),
            cityCaptures, d.Rows, locationsById, cityLocationIds), Is.True,
            "The captured source graph must be internally consistent before mutation of the detached projection.");

        // Simulate malformed staged input without corrupting live owners after the completed boundary.
        // The destination location exists, but it does not belong to the NPC's captured current City.
        P12DNpcDProjection detachedWrongLocation = ReplaceDRow(d, npc.RuntimeId, source => CopyDRow(
            source, source.PersonIdValue, source.ResidenceSettlementRuntimeId,
            source.CurrentCityRuntimeId, destination.Location.RuntimeId,
            source.DestinationCityRuntimeId, source.DestinationLocationRuntimeId,
            source.CurrentActionDefinitionId));

        Assert.That(P12DDailyV1OwnerPackage.TryPreflightRelations(
            fixture.Runtime.PersonStore.CaptureOwnerSnapshot(),
            new GenealogyOwnerSnapshot(GenealogyOwnerBridge.CaptureOwnerSnapshot(
                fixture.Runtime.GenealogyStoreForWorldBoundary)),
            cityCaptures, detachedWrongLocation.Rows, locationsById, cityLocationIds), Is.False,
            "A destination Location owned by another City must be rejected before membership fill.");
        Assert.That(city.ImportantNpcRevision, Is.EqualTo(sourceRevision));
        Assert.That(city.ImportantNpcs, Has.Count.EqualTo(sourceMemberCount));
        Assert.That(city.ImportantNpcs.Single(), Is.SameAs(npc));
        Assert.That(npc.CurrentCity, Is.SameAs(city));
        Assert.That(npc.CurrentLocation, Is.SameAs(city.Location));
    }

    [Test]
    public void DailyV1Package_StagesDormantMaterializedPeopleAndGenealogyTogether()
    {
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12d-owner-package-people-city", "p12d-owner-package-people-location");
        CityRuntime destination = SimulationTestFactory.CreateCity(
            "p12d-owner-package-people-destination", "p12d-owner-package-people-destination-location");
        NpcRuntime npc = new NpcRuntime(
            "p12d-owner-package-people-npc",
            SimulationTestFactory.CreateNpc("p12d-owner-package-people-npc-definition"), city, 0f);

        PersonRuntime dormantParent = new PersonRuntime(
            new PersonId("p12d-owner-package-dormant-parent"), 0L);
        PersonRuntime materializedChild = new PersonRuntime(
            new PersonId("p12d-owner-package-materialized-child"), 10L);
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(dormantParent, city.RuntimeId), Is.True);
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(materializedChild, city.RuntimeId), Is.True);

        PersonStore sourcePersons = new PersonStore();
        Assert.That(sourcePersons.TryRegister(dormantParent, out _), Is.True);
        Assert.That(sourcePersons.TryRegister(materializedChild, out _), Is.True);
        Assert.That(sourcePersons.TryBindMaterializedNpc(
            materializedChild.PersonId, npc.RuntimeId, out _), Is.True);
        Assert.That(npc.TryAssignPersonId(materializedChild.PersonId), Is.True);
        Assert.That(npc.TryBindPersonRuntime(materializedChild), Is.True);

        GenealogyStore sourceGenealogy = new GenealogyStore();
        long sourcePersonRevision = sourcePersons.Revision;
        long sourceGenealogyRevision = sourceGenealogy.Revision;
        long sourceCityRevision = city.ImportantNpcRevision;
        DailyFixture fixture = CreateDailyFixture(
            city, npc, destination, personStore: sourcePersons, genealogyStore: sourceGenealogy);
        Assert.That(fixture.Runtime.TryAddParentage(
            dormantParent.PersonId, materializedChild.PersonId,
            out PersonGenealogyFailure genealogyFailure), Is.True, genealogyFailure.ToString());
        long runtimeGenealogyRevision = fixture.Runtime.GenealogyStoreForWorldBoundary.Revision;
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);

        Assert.That(TryStageDailyPackage(
            fixture, token, new RuntimeIdentityRegistry(),
            out P12DDailyV1OwnerPackage package,
            out P12DDailyV1OwnerPackageFailure failure), Is.True, failure.ToString());

        Assert.That(package, Is.Not.Null);
        Assert.That(package.Persons.Persons.Count, Is.EqualTo(2));
        Assert.That(package.Persons.Revision, Is.EqualTo(sourcePersonRevision));
        Assert.That(package.Persons.TryGet(dormantParent.PersonId, out PersonRuntime stagedDormant), Is.True);
        Assert.That(stagedDormant, Is.Not.SameAs(dormantParent));
        Assert.That(stagedDormant.IsMaterialized, Is.False);
        Assert.That(stagedDormant.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(package.Persons.TryGet(materializedChild.PersonId, out PersonRuntime stagedChild), Is.True);
        Assert.That(stagedChild, Is.Not.SameAs(materializedChild));
        Assert.That(stagedChild.IsMaterialized, Is.True);
        Assert.That(stagedChild.MaterializedNpcRuntimeId, Is.EqualTo(npc.RuntimeId));
        Assert.That(stagedChild.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(package.Persons.TryGetByMaterializedNpcRuntimeId(
            npc.RuntimeId, out PersonRuntime indexedChild), Is.True);
        Assert.That(indexedChild, Is.SameAs(stagedChild));

        NpcRuntime stagedNpc = package.Npcs.Single(value => value.RuntimeId == npc.RuntimeId);
        Assert.That(stagedNpc.PersonId, Is.EqualTo(materializedChild.PersonId));
        Assert.That(stagedNpc.BoundPersonRuntime, Is.SameAs(stagedChild));
        Assert.That(stagedNpc.BoundPersonRuntime, Is.Not.SameAs(materializedChild));
        Assert.That(package.Genealogy.Revision, Is.EqualTo(runtimeGenealogyRevision));
        Assert.That(package.Genealogy.Records, Has.Count.EqualTo(1));
        Assert.That(package.Genealogy.Records.Single().ParentId, Is.EqualTo(dormantParent.PersonId));
        Assert.That(package.Genealogy.Records.Single().ChildId, Is.EqualTo(materializedChild.PersonId));

        Assert.That(sourcePersons.Revision, Is.EqualTo(sourcePersonRevision));
        Assert.That(sourceGenealogy.Revision, Is.EqualTo(sourceGenealogyRevision));
        Assert.That(fixture.Runtime.GenealogyStoreForWorldBoundary.Revision,
            Is.EqualTo(runtimeGenealogyRevision));
        Assert.That(fixture.Runtime.GenealogyRecords, Has.Count.EqualTo(1));
        Assert.That(city.ImportantNpcRevision, Is.EqualTo(sourceCityRevision));
        Assert.That(city.ImportantNpcs.Single(), Is.SameAs(npc));
        Assert.That(npc.BoundPersonRuntime, Is.SameAs(materializedChild));
    }

    [Test]
    public void DailyV1Package_LateDanglingPersonResidenceReturnsNoPackageAndPreservesSources()
    {
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12d-owner-package-late-failure-city", "p12d-owner-package-late-failure-location");
        CityRuntime destination = SimulationTestFactory.CreateCity(
            "p12d-owner-package-late-failure-destination",
            "p12d-owner-package-late-failure-destination-location");
        NpcRuntime npc = new NpcRuntime(
            "p12d-owner-package-late-failure-npc",
            SimulationTestFactory.CreateNpc("p12d-owner-package-late-failure-npc-definition"), city, 0f);
        PersonRuntime danglingResident = new PersonRuntime(
            new PersonId("p12d-owner-package-dangling-resident"), 0L);
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            danglingResident,
            "p12d-owner-package-absent-city"), Is.True);
        PersonStore sourcePersons = new PersonStore();
        Assert.That(sourcePersons.TryRegister(danglingResident, out _), Is.True);
        GenealogyStore sourceGenealogy = new GenealogyStore();

        DailyFixture fixture = CreateDailyFixture(
            city, npc, destination, personStore: sourcePersons, genealogyStore: sourceGenealogy);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        long personRevision = sourcePersons.Revision;
        long genealogyRevision = sourceGenealogy.Revision;
        long cityRevision = city.ImportantNpcRevision;
        long identityRevision = fixture.Identities.CensusRevision;
        long spatialRevision = fixture.SpatialNetwork.Revision;
        GenealogyStore runtimeGenealogy = fixture.Runtime.GenealogyStoreForWorldBoundary;
        long runtimeGenealogyRevision = runtimeGenealogy.Revision;
        SpatialLocationRuntime sourceLocation = npc.CurrentLocation;
        CityRuntime sourceCity = npc.CurrentCity;
        IReadOnlyList<NpcRuntime> sourceMembers = city.ImportantNpcs.ToArray();
        RuntimeIdentityRegistry stagedIdentities = new RuntimeIdentityRegistry();

        Assert.That(TryStageDailyPackage(
            fixture, token, stagedIdentities,
            out P12DDailyV1OwnerPackage package,
            out P12DDailyV1OwnerPackageFailure failure), Is.False);

        Assert.That(package, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DDailyV1OwnerPackageFailure.InvalidRelation));
        Assert.That(sourcePersons.Revision, Is.EqualTo(personRevision));
        Assert.That(sourcePersons.Persons, Has.Count.EqualTo(1));
        Assert.That(sourcePersons.TryGet(danglingResident.PersonId, out PersonRuntime sourceResident), Is.True);
        Assert.That(sourceResident, Is.SameAs(danglingResident));
        Assert.That(sourceResident.ResidenceSettlementRuntimeId, Is.EqualTo("p12d-owner-package-absent-city"));
        Assert.That(sourceGenealogy.Revision, Is.EqualTo(genealogyRevision));
        Assert.That(sourceGenealogy.Records, Is.Empty);
        Assert.That(city.ImportantNpcRevision, Is.EqualTo(cityRevision));
        Assert.That(city.ImportantNpcs, Is.EqualTo(sourceMembers));
        Assert.That(city.ImportantNpcs.Single(), Is.SameAs(npc));
        Assert.That(npc.CurrentCity, Is.SameAs(sourceCity));
        Assert.That(npc.CurrentLocation, Is.SameAs(sourceLocation));
        Assert.That(fixture.Identities.CensusRevision, Is.EqualTo(identityRevision));
        Assert.That(fixture.Identities.TryGetNpc(npc.RuntimeId, out NpcRuntime registeredNpc), Is.True);
        Assert.That(registeredNpc, Is.SameAs(npc));
        Assert.That(fixture.SpatialNetwork.Revision, Is.EqualTo(spatialRevision));
        Assert.That(fixture.Runtime.PersonStore, Is.SameAs(sourcePersons));
        Assert.That(fixture.Runtime.GenealogyStoreForWorldBoundary, Is.SameAs(runtimeGenealogy));
        Assert.That(runtimeGenealogy.Revision, Is.EqualTo(runtimeGenealogyRevision));
    }

    [Test]
    public void CaptureAndStage_PreserveNpcDAndFValuesWithoutReplayingWrites()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12d-npc-snapshot-item", 12f);
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12d-npc-snapshot-action", NpcActionType.Normal);
        NpcData npcDefinition = SimulationTestFactory.CreateNpc("p12d-npc-snapshot-definition");
        CityRuntime city = SimulationTestFactory.CreateCity("p12d-npc-snapshot-city", "p12d-npc-snapshot-location");
        CityRuntime destinationCity = SimulationTestFactory.CreateCity(
            "p12d-npc-snapshot-destination-city", "p12d-npc-snapshot-destination-location");
        NpcRuntime npc = new NpcRuntime("p12d-npc-snapshot-runtime", npcDefinition, city, 25f);
        Assert.That(npc.Inventory.TryAddItem(item, 3, 4.5f), Is.True);
        Assert.That(npc.MoneyAccount.TryCredit(2.5f), Is.True);
        npc.HideForDays(2);
        Assert.That(npc.HiddenDaysRemaining, Is.EqualTo(3),
            "Two hidden days include the current day in the existing runtime countdown.");
        Assert.That(npc.SetResidenceSettlementRuntimeId(city.RuntimeId), Is.True);
        NpcActionRuntime actionRuntime = new NpcActionRuntime(action, destinationCity, item, 2, 3.25f);
        actionRuntime.SetSuccessChanceMultiplier(0.4f);
        actionRuntime.SetOriginDecisionId("p12d-action-decision");
        actionRuntime.SetStableOccurrenceKey("p12d-action-occurrence");
        npc.SetCurrentActionRuntime(actionRuntime);
        npc.SetTravelPlan(destinationCity, NpcTravelReason.Trade, 2.75f, 1.25f, "p12d-travel-plan-decision");
        npc.SetMerchantTradePlan(item, city, destinationCity, 5, 4.25f, "p12d-merchant-plan-decision");
        Assert.That(npc.SpatialKnowledge.DiscoverLocation(destinationCity.Location.RuntimeId), Is.True);
        Assert.That(npc.SpatialKnowledge.DiscoverRoute("p12d-npc-snapshot-route-a"), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            "p12d-known-site", destinationCity.Location.RuntimeId, 0L, 0L,
            ExplorableSiteKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(npc.AdventureSiteIntelKnowledge.RecordObservation(new AdventureNotableItemObservation(
            "p12d-known-site", "p12d-known-place", "p12d-notable-item", item.DefinitionId,
            0L, 0L, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(npc.CommercialKnowledge.RecordObservation(new CommercialMarketObservation(
            destinationCity.Location.RuntimeId, item, 12.5f, 7, 0L, 0L,
            CommercialKnowledgeSource.DirectObservation, null)), Is.True);

        DailyFixture fixture = CreateDailyFixture(city, npc, destinationCity);
        SimulationRuntime runtime = fixture.Runtime;
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(runtime);
        OwnerSectionCensusSnapshot siteSection = token.OwnerSections.Single(section =>
            section.SectionId == ExplorableSiteCensusProvider.SectionId);
        Assert.That(siteSection.Role, Is.EqualTo(OwnerSectionRole.ExplicitlyEmpty));
        Assert.That(ReferenceEquals(siteSection.OwnerInstanceIdentity, fixture.SiteStore), Is.True);
        Assert.That(siteSection.Cardinality, Is.Zero);
        Assert.That(siteSection.Revision, Is.Zero);
        object stamp = new object();
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            runtime, token, stamp, token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure captureFailure), Is.True, captureFailure.ToString());

        Assert.That(d.Rows, Has.Count.EqualTo(10));
        Assert.That(f.Rows, Has.Count.EqualTo(10));
        Assert.That(d.Evidence.HasSameCaptureIdentity(f.Evidence), Is.True);
        P12DNpcDRow capturedD = d.Rows.Single(row => row.RuntimeId == npc.RuntimeId);
        P12DNpcFRow capturedF = f.Rows.Single(row => row.RuntimeId == npc.RuntimeId);
        NpcRuntime emptyNpc = fixture.Npcs[1];
        P12DNpcDRow emptyD = d.Rows.Single(row => row.RuntimeId == emptyNpc.RuntimeId);
        P12DNpcFRow emptyF = f.Rows.Single(row => row.RuntimeId == emptyNpc.RuntimeId);
        Assert.That(emptyD.HiddenDaysRemaining, Is.Zero);
        Assert.That(emptyD.CurrentCityRuntimeId, Is.Null);
        Assert.That(emptyD.CurrentLocationRuntimeId, Is.Null);
        Assert.That(emptyD.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(emptyD.InventoryRows, Is.Empty);
        Assert.That(emptyD.InventoryRevision, Is.Zero);
        Assert.That(emptyD.MoneyBalance, Is.Zero);
        Assert.That(emptyD.MoneyRevision, Is.Zero);
        Assert.That(emptyF.Action, Is.Null);
        Assert.That(emptyF.KnownLocationRuntimeIds, Is.Empty);
        Assert.That(emptyF.KnownRouteRuntimeIds, Is.Empty);
        Assert.That(emptyF.SiteObservations, Is.Empty);
        Assert.That(emptyF.NotableItemObservations, Is.Empty);
        Assert.That(emptyF.CommercialMarkets, Is.Empty);
        Assert.That(capturedD.InventoryRows, Has.Count.EqualTo(1));
        Assert.That(capturedD.InventoryRows[0].Amount, Is.EqualTo(3));
        Assert.That(capturedD.InventoryRows[0].AverageUnitCost, Is.EqualTo(4.5f));
        var receiptSections = token.OwnerSections.Where(section =>
            section.SectionId.StartsWith(P12DNpcReceiptOwnerCensusProvider.LocalObservationSectionPrefix, StringComparison.Ordinal)
            || section.SectionId.StartsWith(P12DNpcReceiptOwnerCensusProvider.MerchantTradeStateSectionPrefix, StringComparison.Ordinal)).ToArray();
        Assert.That(receiptSections, Has.Length.EqualTo(fixture.Npcs.Length * 2));
        Assert.That(receiptSections.All(section => section.Cardinality == 0 && section.Revision == 0L), Is.True);
        Assert.That(capturedD.InventoryRevision, Is.EqualTo(npc.Inventory.Revision));
        Assert.That(capturedD.MoneyBalance, Is.EqualTo(27.5f));
        Assert.That(capturedD.MoneyRevision, Is.EqualTo(1L));
        Assert.That(capturedD.CurrentCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(capturedD.CurrentLocationRuntimeId, Is.EqualTo(city.Location.RuntimeId));
        Assert.That(capturedD.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(capturedD.HiddenDaysRemaining, Is.EqualTo(2),
            "A complete Daily-v1 boundary advances the existing hidden-day countdown once.");
        Assert.That(capturedF.Action.ActionDefinitionId, Is.EqualTo(action.DefinitionId));
        Assert.That(capturedF.Action.Amount, Is.EqualTo(2));
        Assert.That(capturedF.Action.TargetCityRuntimeId, Is.EqualTo(destinationCity.RuntimeId));
        Assert.That(capturedF.Action.TargetItemDefinitionId, Is.EqualTo(item.DefinitionId));
        Assert.That(capturedF.Action.ExpectedUnitPrice, Is.EqualTo(3.25f));
        Assert.That(capturedF.Action.SuccessChanceMultiplier, Is.EqualTo(0.4f));
        Assert.That(capturedF.Action.OriginDecisionId, Is.EqualTo("p12d-action-decision"));
        Assert.That(capturedF.Action.StableOccurrenceKey, Is.EqualTo("p12d-action-occurrence"));
        Assert.That(capturedF.TravelPlan.Revision, Is.EqualTo(npc.TravelPlan.Revision));
        Assert.That(capturedF.MerchantPlan.Revision, Is.EqualTo(npc.MerchantTradePlan.Revision));
        Assert.That(capturedF.KnownLocationRuntimeIds, Is.EqualTo(new[] { destinationCity.Location.RuntimeId, city.Location.RuntimeId }));
        Assert.That(capturedF.KnownRouteRuntimeIds, Is.EqualTo(new[] { "p12d-npc-snapshot-route-a" }));
        Assert.That(capturedF.SiteObservations, Has.Count.EqualTo(1));
        Assert.That(capturedF.NotableItemObservations, Has.Count.EqualTo(1));
        Assert.That(capturedF.CommercialMarkets, Has.Count.EqualTo(1));

        List<P12DCityMembershipLinker> cityLinkers = new List<P12DCityMembershipLinker>();
        List<CityRuntime> stagedCities = new List<CityRuntime>();
        foreach (CityRuntime sourceCity in fixture.Cities)
        {
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(
                sourceCity, token, stamp, token.OwnerSections,
                out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture,
                out P12DCityRootOwnerSnapshotFailure cityCaptureFailure), Is.True, cityCaptureFailure.ToString());
            Assert.That(cityCapture.TryStage(
                fixture.Cities.Select(value => value.CityData).ToArray(), Array.Empty<ItemData>(),
                fixture.Cities.Select(value => value.Location).ToArray(),
                out CityRuntime stagedCity,
                out P12DCityMembershipLinker cityLinker,
                out P12DCityRootOwnerSnapshotFailure cityStageFailure), Is.True, cityStageFailure.ToString());
            cityLinkers.Add(cityLinker);
            stagedCities.Add(stagedCity);
        }
        CityRuntime stagedTargetCity = stagedCities[Array.IndexOf(fixture.Cities, city)];

        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(
            runtime, token, d, f, cityLinkers, new PersonStore(),
            fixture.Npcs.Select(value => value.NpcData).ToArray(), new[] { action }, Array.Empty<NpcStatusData>(), new[] { item },
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> staged,
            out P12DNpcRootOwnerSnapshotFailure stageFailure), Is.True, stageFailure.ToString());

        Assert.That(staged, Has.Count.EqualTo(10));
        NpcRuntime restored = staged.Single(value => value.RuntimeId == npc.RuntimeId);
        Assert.That(restored, Is.Not.SameAs(npc));
        Assert.That(restored.CurrentCity, Is.SameAs(stagedTargetCity));
        Assert.That(restored.CurrentLocation, Is.SameAs(stagedTargetCity.Location));
        Assert.That(stagedTargetCity.ImportantNpcs, Is.EqualTo(new[] { restored }));
        Assert.That(stagedTargetCity.ImportantNpcRevision, Is.EqualTo(city.ImportantNpcRevision));
        Assert.That(restored.CurrentActionRuntime.Action, Is.SameAs(action));
        Assert.That(restored.CurrentActionRuntime.Amount, Is.EqualTo(2));
        Assert.That(restored.CurrentActionRuntime.TargetCity, Is.SameAs(stagedCities[1]));
        Assert.That(restored.CurrentActionRuntime.TargetItem, Is.SameAs(item));
        Assert.That(restored.CurrentActionRuntime.ExpectedUnitPrice, Is.EqualTo(3.25f));
        Assert.That(restored.CurrentActionRuntime.SuccessChanceMultiplier, Is.EqualTo(0.4f));
        Assert.That(restored.CurrentActionRuntime.OriginDecisionId, Is.EqualTo("p12d-action-decision"));
        Assert.That(restored.CurrentActionRuntime.StableOccurrenceKey, Is.EqualTo("p12d-action-occurrence"));
        Assert.That(restored.Inventory.Items[0].Item, Is.SameAs(item));
        Assert.That(restored.Inventory.Items[0].Amount, Is.EqualTo(3));
        Assert.That(restored.Inventory.Items[0].AverageUnitCost, Is.EqualTo(4.5f));
        Assert.That(restored.Inventory.Revision, Is.EqualTo(npc.Inventory.Revision));
        Assert.That(restored.MoneyAccount.Balance, Is.EqualTo(27.5f));
        Assert.That(restored.MoneyAccount.Revision, Is.EqualTo(1L));
        Assert.That(restored.HiddenDaysRemaining, Is.EqualTo(2));
        Assert.That(restored.AdvanceHiddenDay(), Is.False);
        Assert.That(restored.HiddenDaysRemaining, Is.EqualTo(1),
            "staging must not consume the hidden-day clock; the next explicit day input advances it once");
        Assert.That(restored.TravelPlan.TargetCity, Is.SameAs(stagedCities[1]));
        Assert.That(restored.TravelPlan.Utility, Is.EqualTo(2.75f));
        Assert.That(restored.TravelPlan.ExpectedCost, Is.EqualTo(1.25f));
        Assert.That(restored.TravelPlan.OriginDecisionId, Is.EqualTo("p12d-travel-plan-decision"));
        Assert.That(restored.TravelPlan.Revision, Is.EqualTo(npc.TravelPlan.Revision));
        Assert.That(restored.MerchantTradePlan.Item, Is.SameAs(item));
        Assert.That(restored.MerchantTradePlan.OriginCity, Is.SameAs(stagedCities[0]));
        Assert.That(restored.MerchantTradePlan.TargetCity, Is.SameAs(stagedCities[1]));
        Assert.That(restored.MerchantTradePlan.PlannedAmount, Is.EqualTo(5));
        Assert.That(restored.MerchantTradePlan.PurchasePricePerItem, Is.EqualTo(4.25f));
        Assert.That(restored.MerchantTradePlan.OriginDecisionId, Is.EqualTo("p12d-merchant-plan-decision"));
        Assert.That(restored.MerchantTradePlan.Revision, Is.EqualTo(npc.MerchantTradePlan.Revision));
        Assert.That(restored.SpatialKnowledge.KnownLocationRuntimeIds,
            Is.EqualTo(new[] { destinationCity.Location.RuntimeId, city.Location.RuntimeId }));
        Assert.That(restored.SpatialKnowledge.KnownRouteRuntimeIds,
            Is.EqualTo(new[] { "p12d-npc-snapshot-route-a" }));
        Assert.That(restored.SpatialKnowledge.Revision, Is.EqualTo(npc.SpatialKnowledge.Revision));
        Assert.That(restored.ExplorableSiteKnowledge.Observations[0].SiteRuntimeId, Is.EqualTo("p12d-known-site"));
        Assert.That(restored.ExplorableSiteKnowledge.Revision, Is.EqualTo(npc.ExplorableSiteKnowledge.Revision));
        Assert.That(restored.AdventureSiteIntelKnowledge.NotableItemObservations[0].NotableItemRuntimeId,
            Is.EqualTo("p12d-notable-item"));
        Assert.That(restored.AdventureSiteIntelKnowledge.Revision, Is.EqualTo(npc.AdventureSiteIntelKnowledge.Revision));
        Assert.That(restored.CommercialKnowledge.TryGetObservation(
            destinationCity.Location.RuntimeId, item.DefinitionId,
            out CommercialMarketObservation restoredObservation), Is.True);
        Assert.That(restoredObservation.ObservedPrice, Is.EqualTo(12.5f));
        Assert.That(restored.CommercialKnowledge.Revision, Is.EqualTo(npc.CommercialKnowledge.Revision));
        Assert.That(restored.CurrentActionRevision, Is.EqualTo(npc.CurrentActionRevision));
    }

    [Test]
    public void CaptureAndStage_PreserveActiveTravelAndPartyProgressWithoutAdvancingIt()
    {
        CityRuntime origin = SimulationTestFactory.CreateCity(
            "p12d-npc-travel-origin", "p12d-npc-travel-origin-location");
        CityRuntime destination = SimulationTestFactory.CreateCity(
            "p12d-npc-travel-destination", "p12d-npc-travel-destination-location");
        NpcRuntime npc = new NpcRuntime(
            "p12d-npc-travel-runtime", SimulationTestFactory.CreateNpc("p12d-npc-travel-definition"), origin, 0f);
        npc.SetTravelPlan(destination, NpcTravelReason.Trade, 8.5f, 2.25f, "p12d-travel-plan");
        Assert.That(npc.StartTravel(destination.Location, destination, 4, "p12d-travel-start", "p12d-npc-snapshot-route-a"), Is.True);
        Assert.That(npc.SetActiveTravelPartyId("p12d-travel-party"), Is.True);

        DailyFixture fixture = CreateDailyFixture(origin, npc, destination);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        object stamp = new object();
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, stamp, token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure captureFailure), Is.True, captureFailure.ToString());
        P12DNpcDRow dRow = d.Rows.Single(row => row.RuntimeId == npc.RuntimeId);
        P12DNpcFRow fRow = f.Rows.Single(row => row.RuntimeId == npc.RuntimeId);
        Assert.That(dRow.CurrentCityRuntimeId, Is.Null);
        Assert.That(dRow.CurrentLocationRuntimeId, Is.Null);
        Assert.That(dRow.DestinationCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(dRow.DestinationLocationRuntimeId, Is.EqualTo(destination.Location.RuntimeId));
        Assert.That(fRow.TravelDaysRemaining, Is.EqualTo(4));
        Assert.That(fRow.TravelDaysTotal, Is.EqualTo(4));
        Assert.That(fRow.TravelRouteRuntimeId, Is.EqualTo("p12d-npc-snapshot-route-a"));
        Assert.That(fRow.TravelStartedToday, Is.True);
        Assert.That(fRow.TravelOriginDecisionId, Is.EqualTo("p12d-travel-start"));
        Assert.That(fRow.ActiveTravelPartyId, Is.EqualTo("p12d-travel-party"));
        Assert.That(fRow.TravelStateRevision, Is.EqualTo(npc.TravelStateRevision));

        List<P12DCityMembershipLinker> linkers = new List<P12DCityMembershipLinker>();
        List<CityRuntime> stagedCities = new List<CityRuntime>();
        foreach (CityRuntime sourceCity in fixture.Cities)
        {
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(sourceCity, token, stamp,
                token.OwnerSections, out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope capture,
                out P12DCityRootOwnerSnapshotFailure cityCaptureFailure), Is.True, cityCaptureFailure.ToString());
            Assert.That(capture.TryStage(fixture.Cities.Select(value => value.CityData).ToArray(),
                Array.Empty<ItemData>(), fixture.Cities.Select(value => value.Location).ToArray(),
                out CityRuntime stagedCity, out P12DCityMembershipLinker linker,
                out P12DCityRootOwnerSnapshotFailure cityStageFailure), Is.True, cityStageFailure.ToString());
            stagedCities.Add(stagedCity);
            linkers.Add(linker);
        }
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, d, f, linkers,
            new PersonStore(), fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds,
            new[] { "p12d-travel-party" }, out IReadOnlyList<NpcRuntime> staged,
            out P12DNpcRootOwnerSnapshotFailure stageFailure), Is.True, stageFailure.ToString());

        NpcRuntime restored = staged.Single(value => value.RuntimeId == npc.RuntimeId);
        Assert.That(restored.IsTraveling, Is.True);
        Assert.That(restored.CurrentCity, Is.Null);
        Assert.That(restored.CurrentLocation, Is.Null);
        Assert.That(restored.DestinationCity, Is.SameAs(stagedCities[1]));
        Assert.That(restored.DestinationLocation, Is.SameAs(stagedCities[1].Location));
        Assert.That(restored.TravelDaysRemaining, Is.EqualTo(4));
        Assert.That(restored.TravelDaysTotal, Is.EqualTo(4));
        Assert.That(restored.TravelRouteRuntimeId, Is.EqualTo("p12d-npc-snapshot-route-a"));
        Assert.That(restored.TravelStartedToday, Is.True);
        Assert.That(restored.TravelOriginDecisionId, Is.EqualTo("p12d-travel-start"));
        Assert.That(restored.ActiveTravelPartyId, Is.EqualTo("p12d-travel-party"));
        Assert.That(restored.TravelStateRevision, Is.EqualTo(npc.TravelStateRevision));
        Assert.That(restored.TravelPlan.Utility, Is.EqualTo(8.5f));
        Assert.That(restored.TravelPlan.ExpectedCost, Is.EqualTo(2.25f));
        Assert.That(restored.TravelPlan.OriginDecisionId, Is.EqualTo("p12d-travel-plan"));
        Assert.That(restored.TravelPlan.Revision, Is.EqualTo(npc.TravelPlan.Revision));
    }
    [Test]
    public void CaptureAndStage_PreservePersonBindingAndPersonOwnedResidence()
    {
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12d-npc-person-city", "p12d-npc-person-location");
        CityRuntime destination = SimulationTestFactory.CreateCity(
            "p12d-npc-person-destination", "p12d-npc-person-destination-location");
        NpcRuntime npc = new NpcRuntime("p12d-npc-person-runtime",
            SimulationTestFactory.CreateNpc("p12d-npc-person-definition"), city, 0f);
        PersonId personId = new PersonId("p12d-person-id");
        PersonRuntime person = new PersonRuntime(personId, 17L);
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(person, city.RuntimeId), Is.True);
        PersonStore sourcePeople = new PersonStore();
        Assert.That(sourcePeople.TryRegister(person, out _), Is.True);
        Assert.That(sourcePeople.TryBindMaterializedNpc(personId, npc.RuntimeId, out _), Is.True);
        Assert.That(npc.TryAssignPersonId(personId), Is.True);
        Assert.That(npc.TryBindPersonRuntime(person), Is.True);

        DailyFixture fixture = CreateDailyFixture(city, npc, destination, sourcePeople);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        object stamp = new object();
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, stamp, token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure captureFailure), Is.True, captureFailure.ToString());
        P12DNpcDRow row = d.Rows.Single(value => value.RuntimeId == npc.RuntimeId);
        Assert.That(row.PersonIdValue, Is.EqualTo(personId.Value));
        Assert.That(row.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(row.ResidenceRevision, Is.EqualTo(npc.ResidenceRevision));

        List<P12DCityMembershipLinker> linkers = new List<P12DCityMembershipLinker>();
        List<CityRuntime> stagedCities = new List<CityRuntime>();
        foreach (CityRuntime sourceCity in fixture.Cities)
        {
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(sourceCity, token, stamp,
                token.OwnerSections, out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture,
                out P12DCityRootOwnerSnapshotFailure cityCaptureFailure), Is.True, cityCaptureFailure.ToString());
            Assert.That(cityCapture.TryStage(fixture.Cities.Select(value => value.CityData).ToArray(),
                Array.Empty<ItemData>(), fixture.Cities.Select(value => value.Location).ToArray(),
                out CityRuntime stagedCity, out P12DCityMembershipLinker linker,
                out P12DCityRootOwnerSnapshotFailure cityStageFailure), Is.True, cityStageFailure.ToString());
            stagedCities.Add(stagedCity);
            linkers.Add(linker);
        }

        PersonStore stagedPeople = new PersonStore();
        PersonRuntime stagedPerson = PersonRuntimeOwnerBridge.CreateForOwnerSnapshot(
            personId,
            17L,
            null,
            city.RuntimeId,
            null,
            PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person));
        Assert.That(stagedPeople.TryRegister(stagedPerson, out _), Is.True);
        Assert.That(stagedPeople.TryBindMaterializedNpc(personId, npc.RuntimeId, out _), Is.True);
        long[] membershipRevisions = stagedCities.Select(value => value.ImportantNpcRevision).ToArray();
        P12DNpcDProjection wrongResidence = ReplaceDRow(d, npc.RuntimeId, source => CopyDRow(
            source, source.PersonIdValue, destination.RuntimeId, source.CurrentCityRuntimeId,
            source.CurrentLocationRuntimeId, source.DestinationCityRuntimeId,
            source.DestinationLocationRuntimeId, source.CurrentActionDefinitionId));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, wrongResidence, f, linkers,
            stagedPeople, fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> wrongResidenceStage,
            out P12DNpcRootOwnerSnapshotFailure wrongResidenceFailure), Is.False);
        Assert.That(wrongResidenceStage, Is.Null);
        Assert.That(wrongResidenceFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidPersonBinding));
        Assert.That(stagedCities.Select(value => value.ImportantNpcs.Count), Is.All.EqualTo(0));
        Assert.That(stagedCities.Select(value => value.ImportantNpcRevision), Is.EqualTo(membershipRevisions));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, d, f, linkers,
            stagedPeople, fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> staged, out P12DNpcRootOwnerSnapshotFailure stageFailure), Is.True,
            stageFailure.ToString());

        NpcRuntime restored = staged.Single(value => value.RuntimeId == npc.RuntimeId);
        Assert.That(restored.PersonId, Is.EqualTo(personId));
        Assert.That(restored.BoundPersonRuntime, Is.SameAs(stagedPerson));
        Assert.That(restored.BoundPersonRuntime, Is.Not.SameAs(person));
        Assert.That(restored.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(stagedPeople.TryGetByMaterializedNpcRuntimeId(npc.RuntimeId, out PersonRuntime indexed), Is.True);
        Assert.That(indexed, Is.SameAs(stagedPerson));
        Assert.That(indexed.BirthAbsoluteDay, Is.EqualTo(17L));
        Assert.That(
            PersonRuntimeOwnerBridge.GetLifeResidenceRevision(indexed),
            Is.EqualTo(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person)));
        Assert.That(restored.CurrentCity, Is.SameAs(stagedCities[0]));
        Assert.That(stagedCities[0].ImportantNpcs, Is.EqualTo(new[] { restored }));
    }
    [Test]
    public void Stage_RejectsDAndFProjectionsCapturedWithDifferentStampsBeforeReturningRoster()
    {
        NpcRuntime npc = new NpcRuntime(
            "p12d-npc-snapshot-mismatch-runtime",
            SimulationTestFactory.CreateNpc("p12d-npc-snapshot-mismatch-definition"));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        SimulationRuntime runtime = fixture.Runtime;
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(runtime);
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection firstD, out _, out _), Is.True);
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            runtime, token, new object(), token.OwnerSections,
            out _, out P12DNpcFProjection secondF, out _), Is.True);

        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(
            runtime, token, firstD, secondF, Array.Empty<P12DCityMembershipLinker>(), new PersonStore(),
            fixture.Npcs.Select(value => value.NpcData).ToArray(), Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(),
            Array.Empty<ItemData>(), fixture.Cities.Select(value => value.Location).ToArray(), Array.Empty<string>(),
            Array.Empty<string>(), out IReadOnlyList<NpcRuntime> staged,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }

    [Test]
    public void Stage_RejectsDifferentTokenAndOwnerVectorIdentity()
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-identity-source",
            SimulationTestFactory.CreateNpc("p12d-npc-identity-source-definition"));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f, out _), Is.True);

        NpcRuntime otherNpc = new NpcRuntime("p12d-npc-identity-other",
            SimulationTestFactory.CreateNpc("p12d-npc-identity-other-definition"));
        DailyFixture otherFixture = CreateDailyFixture(null, otherNpc);
        DailyCaptureEligibilityToken otherToken = CompleteDailyBoundary(otherFixture.Runtime);
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, otherToken, d, f,
            Array.Empty<P12DCityMembershipLinker>(), new PersonStore(),
            fixture.Npcs.Select(value => value.NpcData).ToArray(), Array.Empty<NpcActionData>(),
            Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(), fixture.Cities.Select(value => value.Location).ToArray(),
            Array.Empty<string>(), Array.Empty<string>(), out IReadOnlyList<NpcRuntime> wrongTokenStage,
            out P12DNpcRootOwnerSnapshotFailure wrongTokenFailure), Is.False);
        Assert.That(wrongTokenStage, Is.Null);
        Assert.That(wrongTokenFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));

        P12DNpcFProjection copiedVectorF = new P12DNpcFProjection(
            f.Evidence, f.Token, f.CaptureStamp, f.OwnerSectionVector.ToArray(), f.Rows);
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, d, copiedVectorF,
            Array.Empty<P12DCityMembershipLinker>(), new PersonStore(),
            fixture.Npcs.Select(value => value.NpcData).ToArray(), Array.Empty<NpcActionData>(),
            Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(), fixture.Cities.Select(value => value.Location).ToArray(),
            Array.Empty<string>(), Array.Empty<string>(), out IReadOnlyList<NpcRuntime> wrongVectorStage,
            out P12DNpcRootOwnerSnapshotFailure wrongVectorFailure), Is.False);
        Assert.That(wrongVectorStage, Is.Null);
        Assert.That(wrongVectorFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }

    [TestCase("inventory")]
    [TestCase("money")]
    public void Capture_RejectsAliasedOwnerIdentity(string ownerKind)
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-alias-target-" + ownerKind,
            SimulationTestFactory.CreateNpc("p12d-npc-alias-target-definition-" + ownerKind));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        FieldInfo ownerField = typeof(NpcRuntime).GetField(
            ownerKind == "inventory" ? "inventory" : "moneyAccount", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(ownerField, Is.Not.Null);
        ownerField.SetValue(fixture.Npcs[1], ownerField.GetValue(npc));

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(d, Is.Null);
        Assert.That(f, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }
    [TestCase("inventory")]
    [TestCase("money")]
    public void Capture_RejectsOwnerReplacementAfterCompletedBoundary(string ownerKind)
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-replace-target-" + ownerKind,
            SimulationTestFactory.CreateNpc("p12d-npc-replace-target-definition-" + ownerKind));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        FieldInfo ownerField = typeof(NpcRuntime).GetField(
            ownerKind == "inventory" ? "inventory" : "moneyAccount", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(ownerField, Is.Not.Null);
        ownerField.SetValue(npc, ownerKind == "inventory" ? (object)new InventoryRuntime() : new MoneyAccountRuntime(0f));

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(d, Is.Null);
        Assert.That(f, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }

    [TestCase("local")]
    [TestCase("merchant")]
    public void Capture_RejectsPostTokenP18ReceiptWrite(string ownerKind)
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-receipt-target-" + ownerKind,
            SimulationTestFactory.CreateNpc("p12d-npc-receipt-target-definition-" + ownerKind));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        PopulateReceiptOwner(npc, ownerKind);

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(d, Is.Null);
        Assert.That(f, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }

    [TestCase("local")]
    [TestCase("merchant")]
    public void Capture_RejectsReceiptOwnerPopulatedBeforeBoundary(string ownerKind)
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-initial-receipt-" + ownerKind,
            SimulationTestFactory.CreateNpc("p12d-npc-initial-receipt-definition-" + ownerKind));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        PopulateReceiptOwner(npc, ownerKind);

        bool marked = fixture.Runtime.TryMarkWorldPublishedForFactualRead();
        if (marked) fixture.Runtime.TryAdvanceDay(out _);
        Assert.That(fixture.Runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token, out _), Is.False);
        Assert.That(token, Is.Null);
    }

    [TestCase("local", true)]
    [TestCase("local", false)]
    [TestCase("merchant", true)]
    [TestCase("merchant", false)]
    public void Capture_RejectsMissingOrReplacedReceiptOwner(string ownerKind, bool replace)
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-receipt-identity-" + ownerKind + replace,
            SimulationTestFactory.CreateNpc("p12d-npc-receipt-identity-definition-" + ownerKind + replace));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        string fieldName = ownerKind == "local"
            ? "localKnowledgeObservationRuntime"
            : "merchantTradeStateRuntime";
        FieldInfo ownerField = typeof(NpcRuntime).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(ownerField, Is.Not.Null);
        object replacement = null;
        if (replace)
            replacement = ownerKind == "local"
                ? (object)new NpcLocalKnowledgeObservationRuntime()
                : new NpcMerchantTradeStateRuntime();
        ownerField.SetValue(npc, replacement);

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(d, Is.Null);
        Assert.That(f, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }

    [Test]
    public void DailyV1Boundary_RejectsPopulatedLegacyExplorableSiteStore()
    {
        ExplorableSiteStore sites = new ExplorableSiteStore();
        SpatialLocationRuntime siteLocation = new SpatialLocationRuntime("p12d-populated-site-location");
        Assert.That(sites.Add(new ExplorableSiteRuntime("p12d-populated-site",
            SimulationTestFactory.CreateExplorableSiteDefinition("p12d-populated-site-definition"), siteLocation)), Is.True);
        NpcRuntime npc = new NpcRuntime("p12d-npc-site-exclusion",
            SimulationTestFactory.CreateNpc("p12d-npc-site-exclusion-definition"));
        Assert.Throws<InvalidOperationException>(() => CreateDailyFixture(null, npc, siteStore: sites));

    }
    [Test]
    public void Capture_RejectsDailyV1LocalTopologyKnowledge()
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-local-topology-excluded",
            SimulationTestFactory.CreateNpc("p12d-npc-local-topology-excluded-definition"));
        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(new LocalPlaceKnowledgeObservation(
            npc.RuntimeId, "p12d-excluded-local-place", null, "p12d-place-type", "Fixture", true,
            0L, 0L, LocalTopologyKnowledgeSource.DirectObservation)), Is.True);
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(d, Is.Null);
        Assert.That(f, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidProfileExclusion));
    }

    [Test]
    public void Capture_RejectsDailyV1LocalTopologyPlaceAndConnectionKnowledge()
    {
        NpcRuntime npc = new NpcRuntime("p12d-npc-local-connection-excluded",
            SimulationTestFactory.CreateNpc("p12d-npc-local-connection-excluded-definition"));
        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(new LocalPlaceKnowledgeObservation(
            npc.RuntimeId, "p12d-excluded-connection-origin", null, "p12d-place-type", "Origin", true,
            0L, 0L, LocalTopologyKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.RecordPlaceObservation(new LocalPlaceKnowledgeObservation(
            npc.RuntimeId, "p12d-excluded-connection-destination", null, "p12d-place-type", "Destination", true,
            0L, 0L, LocalTopologyKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.RecordConnectionObservation(new LocalConnectionKnowledgeObservation(
            npc.RuntimeId, "p12d-excluded-local-connection", "p12d-excluded-connection-origin",
            "p12d-excluded-connection-destination", 1f, "p12d-connection-type", 0L, 0L,
            LocalTopologyKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(npc.LocalTopologyKnowledge.PlaceObservations, Has.Count.EqualTo(2));
        Assert.That(npc.LocalTopologyKnowledge.ConnectionObservations, Has.Count.EqualTo(1));
        DailyFixture fixture = CreateDailyFixture(null, npc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.False);
        Assert.That(d, Is.Null);
        Assert.That(f, Is.Null);
        Assert.That(failure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidProfileExclusion));
    }
    [Test]
    public void Stage_RejectsDanglingPersonAndMismatchedDestinationWithoutPublication()
    {
        CityRuntime origin = SimulationTestFactory.CreateCity(
            "p12d-npc-invalid-origin", "p12d-npc-invalid-origin-location");
        CityRuntime destination = SimulationTestFactory.CreateCity(
            "p12d-npc-invalid-destination", "p12d-npc-invalid-destination-location");
        NpcRuntime npc = new NpcRuntime("p12d-npc-invalid-reference-runtime",
            SimulationTestFactory.CreateNpc("p12d-npc-invalid-reference-definition"), origin, 0f);
        Assert.That(npc.StartTravel(destination.Location, destination, 2, "p12d-invalid-travel",
            "p12d-npc-snapshot-route-a"), Is.True);
        DailyFixture fixture = CreateDailyFixture(origin, npc, destination);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        object stamp = new object();
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, stamp, token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f, out _), Is.True);

        List<P12DCityMembershipLinker> linkers = new List<P12DCityMembershipLinker>();
        List<CityRuntime> stagedCities = new List<CityRuntime>();
        foreach (CityRuntime sourceCity in fixture.Cities)
        {
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(sourceCity, token, stamp,
                token.OwnerSections, out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture,
                out P12DCityRootOwnerSnapshotFailure cityCaptureFailure), Is.True, cityCaptureFailure.ToString());
            Assert.That(cityCapture.TryStage(fixture.Cities.Select(value => value.CityData).ToArray(),
                Array.Empty<ItemData>(), fixture.Cities.Select(value => value.Location).ToArray(),
                out CityRuntime stagedCity, out P12DCityMembershipLinker linker,
                out P12DCityRootOwnerSnapshotFailure cityStageFailure), Is.True, cityStageFailure.ToString());
            stagedCities.Add(stagedCity);
            linkers.Add(linker);
        }
        long[] membershipRevisions = stagedCities.Select(value => value.ImportantNpcRevision).ToArray();
        P12DNpcDRow targetRow = d.Rows.Single(value => value.RuntimeId == npc.RuntimeId);

        P12DNpcDProjection mismatchedDestination = ReplaceDRow(d, npc.RuntimeId, source => CopyDRow(
            source, source.PersonIdValue, source.ResidenceSettlementRuntimeId,
            source.CurrentCityRuntimeId, source.CurrentLocationRuntimeId,
            source.DestinationCityRuntimeId, origin.Location.RuntimeId, source.CurrentActionDefinitionId));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, mismatchedDestination, f,
            linkers, new PersonStore(), fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> mismatchStage, out P12DNpcRootOwnerSnapshotFailure mismatchFailure), Is.False);
        Assert.That(mismatchStage, Is.Null);
        Assert.That(mismatchFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidReference));

        P12DNpcDProjection danglingDestination = ReplaceDRow(d, npc.RuntimeId, source => CopyDRow(
            source, source.PersonIdValue, source.ResidenceSettlementRuntimeId,
            source.CurrentCityRuntimeId, source.CurrentLocationRuntimeId,
            source.DestinationCityRuntimeId, "p12d-dangling-destination-location", source.CurrentActionDefinitionId));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, danglingDestination, f,
            linkers, new PersonStore(), fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> danglingStage, out P12DNpcRootOwnerSnapshotFailure danglingFailure), Is.False);
        Assert.That(danglingStage, Is.Null);
        Assert.That(danglingFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidReference));

        P12DNpcDProjection danglingPerson = ReplaceDRow(d, npc.RuntimeId, source => CopyDRow(
            source, "p12d-unregistered-person", source.ResidenceSettlementRuntimeId,
            source.CurrentCityRuntimeId, source.CurrentLocationRuntimeId,
            source.DestinationCityRuntimeId, source.DestinationLocationRuntimeId, source.CurrentActionDefinitionId));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, danglingPerson, f,
            linkers, new PersonStore(), fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> danglingPersonStage,
            out P12DNpcRootOwnerSnapshotFailure danglingPersonFailure), Is.False);
        Assert.That(danglingPersonStage, Is.Null);
        Assert.That(danglingPersonFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidPersonBinding));

        Assert.That(targetRow.DestinationCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(stagedCities.Select(value => value.ImportantNpcs.Count), Is.All.EqualTo(0));
        Assert.That(stagedCities.Select(value => value.ImportantNpcRevision), Is.EqualTo(membershipRevisions));
    }
    [Test]
    public void Stage_RejectsCurrentCityLocationMismatchAndNonreciprocalMembershipWithoutPublication()
    {
        CityRuntime city = SimulationTestFactory.CreateCity(
            "p12d-npc-current-city", "p12d-npc-current-city-location");
        CityRuntime otherCity = SimulationTestFactory.CreateCity(
            "p12d-npc-other-city", "p12d-npc-other-city-location");
        NpcRuntime npc = new NpcRuntime("p12d-npc-current-city-runtime",
            SimulationTestFactory.CreateNpc("p12d-npc-current-city-definition"), city, 0f);
        DailyFixture fixture = CreateDailyFixture(city, npc, otherCity);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        object stamp = new object();
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(fixture.Runtime, token, stamp, token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f, out _), Is.True);

        List<P12DCityMembershipLinker> linkers = new List<P12DCityMembershipLinker>();
        List<CityRuntime> stagedCities = new List<CityRuntime>();
        foreach (CityRuntime sourceCity in fixture.Cities)
        {
            Assert.That(P12DCityRootOwnerSnapshot.TryCaptureForStaging(sourceCity, token, stamp,
                token.OwnerSections, out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture,
                out P12DCityRootOwnerSnapshotFailure cityCaptureFailure), Is.True, cityCaptureFailure.ToString());
            Assert.That(cityCapture.TryStage(fixture.Cities.Select(value => value.CityData).ToArray(),
                Array.Empty<ItemData>(), fixture.Cities.Select(value => value.Location).ToArray(),
                out CityRuntime stagedCity, out P12DCityMembershipLinker linker,
                out P12DCityRootOwnerSnapshotFailure cityStageFailure), Is.True, cityStageFailure.ToString());
            stagedCities.Add(stagedCity);
            linkers.Add(linker);
        }
        long[] revisions = stagedCities.Select(value => value.ImportantNpcRevision).ToArray();
        P12DNpcDRow source = d.Rows.Single(value => value.RuntimeId == npc.RuntimeId);
        P12DNpcDProjection wrongLocation = ReplaceDRow(d, npc.RuntimeId, row => CopyDRow(
            row, row.PersonIdValue, row.ResidenceSettlementRuntimeId, row.CurrentCityRuntimeId,
            otherCity.Location.RuntimeId, row.DestinationCityRuntimeId, row.DestinationLocationRuntimeId,
            row.CurrentActionDefinitionId));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, wrongLocation, f, linkers,
            new PersonStore(), fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> wrongLocationStage,
            out P12DNpcRootOwnerSnapshotFailure wrongLocationFailure), Is.False);
        Assert.That(wrongLocationStage, Is.Null);
        Assert.That(wrongLocationFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidReference));

        P12DNpcDProjection missingReciprocalCity = ReplaceDRow(d, npc.RuntimeId, row => CopyDRow(
            row, row.PersonIdValue, row.ResidenceSettlementRuntimeId, null, null,
            row.DestinationCityRuntimeId, row.DestinationLocationRuntimeId, row.CurrentActionDefinitionId));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(fixture.Runtime, token, missingReciprocalCity, f, linkers,
            new PersonStore(), fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(), Array.Empty<ItemData>(),
            stagedCities.Select(value => value.Location).ToArray(), fixture.RouteIds, Array.Empty<string>(),
            out IReadOnlyList<NpcRuntime> nonreciprocalStage,
            out P12DNpcRootOwnerSnapshotFailure nonreciprocalFailure), Is.False);
        Assert.That(nonreciprocalStage, Is.Null);
        Assert.That(nonreciprocalFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.InvalidCityMembership));
        Assert.That(source.CurrentCityRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(stagedCities.Select(value => value.ImportantNpcs.Count), Is.All.EqualTo(0));
        Assert.That(stagedCities.Select(value => value.ImportantNpcRevision), Is.EqualTo(revisions));
    }

    [Test]
    public void SelectedOwnerCollection_AllowsNonemptyNpcRosterWithoutCityOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npcDefinition = definitions.Npcs.Values.First();
        NpcRuntime npc = NpcRuntime.CreateSelectedDailyV1(
            "p12d-selected-npc-without-city", npcDefinition, definitions, (CityRuntime)null, 0f);
        MethodInfo collect = typeof(P12DDailyV1OwnerPackage).GetMethod(
            "TryCollectSelectedOwners", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(collect, Is.Not.Null);

        object[] arguments =
        {
            definitions,
            System.Array.Empty<CityRuntime>(),
            new[] { npc },
            null,
            null
        };
        Assert.That((bool)collect.Invoke(null, arguments), Is.True);
        IReadOnlyList<DailyV1CityRuntimeOwners> cityOwners =
            (IReadOnlyList<DailyV1CityRuntimeOwners>)arguments[3];
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcOwners =
            (IReadOnlyList<DailyV1NpcRuntimeOwners>)arguments[4];
        Assert.That(cityOwners, Is.Empty);
        Assert.That(npcOwners.Count, Is.EqualTo(1));
        Assert.That(npcOwners[0], Is.SameAs(npc.SelectedRuntimeOwners));

        object[] emptyArguments =
        {
            definitions,
            System.Array.Empty<CityRuntime>(),
            System.Array.Empty<NpcRuntime>(),
            null,
            null
        };
        Assert.That((bool)collect.Invoke(null, emptyArguments), Is.False,
            "A completely empty selected owner vector cannot establish its definition identity.");
    }

    [Test]
    public void SelectedDailyV1CaptureUsesSharedCoreLocalTopologyEmptyOwnerForAliasedSections()
    {
        SimulationConfigData config = SimulationTestFactory.CreateSimulationConfig();
        NpcData npcAsset = SimulationTestFactory.CreateNpc("p12d-selected-empty-topology-definition");
        config.Npcs.Add(new NpcSimulationConfig { npc = npcAsset, initialMoney = 0f });
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(config.Calendar, out _);
        DailyV1EffectiveDefinitions definitions = SimulationGenesisPipeline.CreateSelectedDefinitions(
            config, effective, calendar, out DailyV1DefinitionAdapter.SelectedBindings bindings);
        DailyV1NpcDefinition definition = definitions.Npcs[npcAsset.DefinitionId];
        NpcRuntime selectedNpc = NpcRuntime.CreateSelectedDailyV1(
            "p12d-selected-empty-topology", npcAsset, definition, definitions,
            bindings, (CityRuntime)null, 0f);
        DailyFixture fixture = CreateDailyFixture(null, selectedNpc);
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            fixture.Runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.True, failure.ToString());
        Assert.That(d.Rows, Has.Count.EqualTo(fixture.Npcs.Length));
        Assert.That(f.Rows, Has.Count.EqualTo(fixture.Npcs.Length));

        DailyV1NpcLocalTopologyKnowledgeEmptyOwner emptyOwner =
            selectedNpc.SelectedRuntimeOwners.LocalTopologyKnowledgeEmptyOwner;
        OwnerSectionCensusSnapshot places = token.OwnerSections.Single(row =>
            row.SectionId == NpcKnowledgeCensusProvider.SectionIdFor(1, selectedNpc.RuntimeId));
        OwnerSectionCensusSnapshot connections = token.OwnerSections.Single(row =>
            row.SectionId == NpcKnowledgeCensusProvider.SectionIdFor(2, selectedNpc.RuntimeId));
        Assert.That(places.OwnerInstanceIdentity, Is.SameAs(emptyOwner));
        Assert.That(connections.OwnerInstanceIdentity, Is.SameAs(emptyOwner));
        Assert.That(places.Cardinality, Is.Zero);
        Assert.That(connections.Cardinality, Is.Zero);
        Assert.That(places.Revision, Is.Zero);
        Assert.That(connections.Revision, Is.Zero);
    }

    [Test]
    public void Capture_ProducesDetachedValuesWhenSourceOwnersChangeAfterCapture()
    {
        ItemData item = SimulationTestFactory.CreateItem("p12d-npc-snapshot-detached-item", 7f);
        NpcRuntime npc = new NpcRuntime(
            "p12d-npc-snapshot-detached-runtime",
            SimulationTestFactory.CreateNpc("p12d-npc-snapshot-detached-definition"), null, 8f);
        Assert.That(npc.Inventory.TryAddItem(item, 4, 3f), Is.True);
        DailyFixture fixture = CreateDailyFixture(null, npc);
        SimulationRuntime runtime = fixture.Runtime;
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(runtime);

        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection d, out P12DNpcFProjection f,
            out P12DNpcRootOwnerSnapshotFailure failure), Is.True, failure.ToString());
        Assert.That(P12DNpcRootOwnerSnapshot.TryCapture(
            runtime, token, new object(), token.OwnerSections,
            out P12DNpcDProjection repeatedD, out P12DNpcFProjection repeatedF,
            out P12DNpcRootOwnerSnapshotFailure repeatedFailure), Is.True, repeatedFailure.ToString());
        P12DNpcDRow captured = d.Rows.Single(row => row.RuntimeId == npc.RuntimeId);
        P12DNpcDRow repeated = repeatedD.Rows.Single(row => row.RuntimeId == npc.RuntimeId);
        Assert.That(repeated.RuntimeId, Is.EqualTo(captured.RuntimeId));
        Assert.That(repeated.InventoryRows.Select(value => new { value.ItemDefinitionId, value.Amount, value.AverageUnitCost }),
            Is.EqualTo(captured.InventoryRows.Select(value => new { value.ItemDefinitionId, value.Amount, value.AverageUnitCost })));
        Assert.That(repeated.MoneyBalance, Is.EqualTo(captured.MoneyBalance));
        Assert.That(repeated.MoneyRevision, Is.EqualTo(captured.MoneyRevision));
        Assert.That(repeatedF.Rows.Select(value => value.RuntimeId), Is.EqualTo(f.Rows.Select(value => value.RuntimeId)));
        Assert.That(repeatedD.Evidence.HasSameCaptureIdentity(d.Evidence), Is.False,
            "Each capture call has its own transient stamp even when the exact owner vector is unchanged.");

        Assert.That(npc.Inventory.TryAddItem(item, 2, 3f), Is.True);
        Assert.That(npc.MoneyAccount.TryCredit(5f), Is.True);
        Assert.That(captured.InventoryRows[0].Amount, Is.EqualTo(4));
        Assert.That(repeated.InventoryRows[0].Amount, Is.EqualTo(4));
        Assert.That(captured.MoneyBalance, Is.EqualTo(8f));
        Assert.That(repeated.MoneyBalance, Is.EqualTo(8f));
        Assert.That(P12DNpcRootOwnerSnapshot.TryStage(runtime, token, d, f,
            Array.Empty<P12DCityMembershipLinker>(), new PersonStore(),
            fixture.Npcs.Select(value => value.NpcData).ToArray(), Array.Empty<NpcActionData>(),
            Array.Empty<NpcStatusData>(), new[] { item }, fixture.Cities.Select(value => value.Location).ToArray(),
            fixture.RouteIds, Array.Empty<string>(), out IReadOnlyList<NpcRuntime> staged,
            out P12DNpcRootOwnerSnapshotFailure staleFailure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(staleFailure, Is.EqualTo(P12DNpcRootOwnerSnapshotFailure.StaleCapture));
    }
    private sealed class DailyFixture
    {
        internal SimulationRuntime Runtime;
        internal CityRuntime[] Cities;
        internal NpcRuntime[] Npcs;
        internal string[] RouteIds;
        internal ExplorableSiteStore SiteStore;
        internal RuntimeIdentityRegistry Identities;
        internal SpatialNetworkRuntime SpatialNetwork;
    }

    private static P12DNpcDProjection ReplaceDRow(
        P12DNpcDProjection projection, string runtimeId, Func<P12DNpcDRow, P12DNpcDRow> replacement)
    {
        bool replaced = false;
        List<P12DNpcDRow> rows = new List<P12DNpcDRow>(projection.Rows.Count);
        foreach (P12DNpcDRow row in projection.Rows)
        {
            if (row.RuntimeId == runtimeId)
            {
                rows.Add(replacement(row));
                replaced = true;
            }
            else rows.Add(row);
        }
        if (!replaced) throw new InvalidOperationException("The requested NPC row is not present.");
        return new P12DNpcDProjection(projection.Evidence, projection.Token,
            projection.CaptureStamp, projection.OwnerSectionVector, rows);
    }

    private static P12DNpcDRow CopyDRow(
        P12DNpcDRow source, string personIdValue, string residenceSettlementRuntimeId,
        string currentCityRuntimeId, string currentLocationRuntimeId,
        string destinationCityRuntimeId, string destinationLocationRuntimeId,
        string currentActionDefinitionId)
    {
        return new P12DNpcDRow(source.RuntimeId, source.NpcDefinitionId, personIdValue,
            residenceSettlementRuntimeId, source.LifeState, source.InjurySeverity,
            source.CurrentStatusDefinitionIds, source.HiddenDaysRemaining, currentActionDefinitionId,
            currentCityRuntimeId, currentLocationRuntimeId, destinationCityRuntimeId,
            destinationLocationRuntimeId, source.InventoryRows, source.InventoryRevision,
            source.MoneyBalance, source.MoneyRevision, source.LifeStateRevision,
            source.ResidenceRevision, source.CrimeJusticeRevision, source.CurrentActionRevision);
    }
    private static void PopulateReceiptOwner(NpcRuntime npc, string ownerKind)
    {
        if (ownerKind == "local")
        {
            NpcLocalKnowledgeObservationRuntime owner = npc.ExistingLocalKnowledgeObservationRuntime;
            FieldInfo receipts = typeof(NpcLocalKnowledgeObservationRuntime).GetField(
                "receipts", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo revision = typeof(NpcLocalKnowledgeObservationRuntime).GetField(
                "revision", BindingFlags.Instance | BindingFlags.NonPublic);
            receipts.SetValue(owner, new List<NpcLocalKnowledgeObservationReceipt>
            {
                new NpcLocalKnowledgeObservationReceipt("op", "descriptor", "snapshot",
                    npc.RuntimeId, string.Empty, 1L, 0, 0, false, null, 0L, 0L, 0L, 0L)
            });
            revision.SetValue(owner, 1L);
            return;
        }

        NpcMerchantTradeStateRuntime merchantOwner = npc.ExistingMerchantTradeStateRuntime;
        FieldInfo merchantReceipts = typeof(NpcMerchantTradeStateRuntime).GetField(
            "receipts", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo merchantRevision = typeof(NpcMerchantTradeStateRuntime).GetField(
            "revision", BindingFlags.Instance | BindingFlags.NonPublic);
        merchantReceipts.SetValue(merchantOwner, new List<NpcMerchantTradeStateReceipt>
        {
            new NpcMerchantTradeStateReceipt("op", "descriptor", npc.RuntimeId, string.Empty, 0L, 1L)
        });
        merchantRevision.SetValue(merchantOwner, 1L);
    }
    private static DailyFixture CreateDailyFixture(
        CityRuntime targetCity,
        NpcRuntime targetNpc,
        CityRuntime destinationCity = null,
        PersonStore personStore = null,
        ExplorableSiteStore siteStore = null,
        GenealogyStore genealogyStore = null)
    {
        CityRuntime[] cities =
        {
            targetCity ?? SimulationTestFactory.CreateCity("p12d-npc-snapshot-city-a", "p12d-npc-snapshot-location-a"),
            destinationCity ?? SimulationTestFactory.CreateCity("p12d-npc-snapshot-city-b", "p12d-npc-snapshot-location-b")
        };
        RuntimeIdentityRegistry identities = new RuntimeIdentityRegistry();
        foreach (CityRuntime city in cities)
            Assert.That(identities.RegisterCity(city), Is.True);

        NpcRuntime[] npcs = new NpcRuntime[10];
        npcs[0] = targetNpc;
        Assert.That(identities.RegisterNpc(targetNpc), Is.True);
        for (int i = 1; i < npcs.Length; i++)
        {
            npcs[i] = new NpcRuntime("p12d-npc-snapshot-filler-" + i,
                SimulationTestFactory.CreateNpc("p12d-npc-snapshot-filler-definition-" + i));
            Assert.That(identities.RegisterNpc(npcs[i]), Is.True);
        }

        SpatialLocationRuntime[] networkLocations = cities.Select(city => city.Location).ToArray();
        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identities);
        foreach (SpatialLocationRuntime location in networkLocations)
            Assert.That(network.RegisterLocation(location), Is.True);
        string[] routeIds = { "p12d-npc-snapshot-route-a", "p12d-npc-snapshot-route-b" };
        Assert.That(network.RegisterRoute(new SpatialRouteRuntime(
            routeIds[0], networkLocations[0], networkLocations[1], 1)), Is.True);
        Assert.That(network.RegisterRoute(new SpatialRouteRuntime(
            routeIds[1], networkLocations[1], networkLocations[0], 1)), Is.True);

        if (siteStore == null) siteStore = new ExplorableSiteStore();
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        EconomyTransactionService transactionService = new EconomyTransactionService();
        TravelSystem travelSystem = new TravelSystem(
            network,
            location => cities.FirstOrDefault(city => ReferenceEquals(city.Location, location)),
            1f,
            records.EventRecorder,
            null,
            transactionService);
        ScheduledDirectiveStore directiveStore = new ScheduledDirectiveStore(records.Time);
        ScheduledDirectiveSystem directiveSystem = new ScheduledDirectiveSystem(directiveStore, identities);
        JusticeSystem justiceSystem = new JusticeSystem(null, null, null, null,
            records.EventRecorder, null);
        CrimeSystem crimeSystem = new CrimeSystem(justiceSystem, travelSystem, null,
            transactionService: transactionService,
            configuration: new EffectiveCrimeConfiguration(false, false),
            simulationTime: records.Time);
        TravelPartyStore travelPartyStore = new TravelPartyStore();
        TravelPartySystem travelPartySystem = new TravelPartySystem(
            travelPartyStore,
            records.Allocator,
            identities,
            travelSystem,
            records.Time,
            records.Sequence,
            records.EventRecorder,
            transactionService: transactionService);
        ExpeditionStore expeditionStore = new ExpeditionStore();
        ExplorableSiteKnowledgeSystem siteKnowledgeSystem = new ExplorableSiteKnowledgeSystem();
        ExpeditionSystem expeditionSystem = new ExpeditionSystem(
            expeditionStore,
            records.Allocator,
            identities,
            siteStore,
            travelPartySystem,
            travelPartyStore,
            siteKnowledgeSystem,
            records.Time,
            records.EventRecorder);

        SimulationRuntime runtime = new SimulationRuntime(
            records.Time, cities, npcs,
            economyEnabled: false,
            scheduledDirectiveSystem: directiveSystem,
            justiceSystem: justiceSystem,
            crimeSystem: crimeSystem,
            travelSystem: travelSystem,
            travelPartySystem: travelPartySystem,
            decisionRecorder: records.DecisionRecorder,
            explorableSiteStore: siteStore,
            explorableSiteKnowledgeSystem: siteKnowledgeSystem,
            expeditionSystem: expeditionSystem,
            spatialAuthorityStore: CreateSpatialAuthority(),
            personStore: personStore,
            genealogyStore: genealogyStore,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            economyTransactionService: transactionService,
            requireP12ReceiptCensusOwners: true,
            runtimeIdentityRegistry: identities,
            spatialNetworkRuntime: network,
            requireP12RuntimeIdentitySpatialCensusOwners: true,
            worldId: new WorldId(Guid.NewGuid()));
        return new DailyFixture
        {
            Runtime = runtime,
            Cities = cities,
            Npcs = npcs,
            RouteIds = routeIds,
            SiteStore = siteStore,
            Identities = identities,
            SpatialNetwork = network
        };
    }

    private static bool TryStageDailyPackage(
        DailyFixture fixture,
        DailyCaptureEligibilityToken token,
        RuntimeIdentityRegistry stagedIdentities,
        out P12DDailyV1OwnerPackage package,
        out P12DDailyV1OwnerPackageFailure failure)
    {
        if (!DailyCaptureStagingAttempt.TryBegin(
                fixture.Runtime, token, token.OwnerSections,
                out DailyCaptureStagingAttempt stagingAttempt))
        {
            package = null;
            failure = P12DDailyV1OwnerPackageFailure.InvalidCaptureContext;
            return false;
        }

        return P12DDailyV1OwnerPackage.TryCaptureAndStage(
            fixture.Runtime,
            token,
            stagingAttempt,
            token.OwnerSections,
            fixture.Identities,
            fixture.SpatialNetwork,
            fixture.SiteStore,
            stagedIdentities,
            token.WorldId,
            fixture.Cities.Select(value => value.CityData).ToArray(),
            Array.Empty<ItemData>(),
            fixture.Npcs.Select(value => value.NpcData).ToArray(),
            Array.Empty<NpcActionData>(),
            Array.Empty<NpcStatusData>(),
            Array.Empty<DailyV1ExplorableSiteDefinition>(),
            Array.Empty<string>(),
            out package,
            out failure);
    }

    private static SpatialAuthorityStore CreateSpatialAuthority()
    {
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("p12d-npc-snapshot-scale", "fixture", "v1", 1m, "step"),
            new[]
            {
                new HexRecord(new HexId("p12d-npc-snapshot-hex"), new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain.p12d-npc-snapshot"), "v1"))
            },
            new[] { new LocationRecord(new LocationId("p12d-npc-snapshot-location"), new HexId("p12d-npc-snapshot-hex")) });
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        if (!authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure))
            throw new InvalidOperationException("Could not compose P12-D NPC snapshot geography: " + failure);
        return authority;
    }

    private static DailyCaptureEligibilityToken CompleteDailyBoundary(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        return token;
    }

    private static void SetSerializedSpatialOwnerIdentityAndDeserialize(
        NpcRuntime npc, string serializedOwnerRuntimeId)
    {
        object adapter = typeof(NpcRuntime).GetField("spatialKnowledge",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(npc);
        Assert.That(adapter, Is.Not.Null);
        adapter.GetType().GetField("ownerRuntimeId", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(adapter, serializedOwnerRuntimeId);
        adapter.GetType().GetMethod("OnAfterDeserialize").Invoke(adapter, null);
    }
}
