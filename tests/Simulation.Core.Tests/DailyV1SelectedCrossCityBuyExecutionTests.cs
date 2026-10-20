using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedCrossCityBuyExecutionTests
{
    [Test]
    public void CompositeUsesPreparedPurchaseValuesAndPreservesDecisionIdentity()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners origin,
            out DailyV1CityRuntimeOwners destination, out DailyV1ItemDefinition item,
            out DailyV1NpcActionPayloadOwner payload);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, payload.Action, payload, null), Is.True);
        float balance = actor.MoneyAccount.Balance;
        int stock = origin.Market.GetAmount(item);
        int inventory = actor.Inventory.GetAmount(item);
        PurchaseBoundary boundary = new PurchaseBoundary();
        int merchantAdmission = 0, merchantNotifications = 0;
        int travelAdmission = 0, travelNotifications = 0;

        DailyV1SelectedCrossCityBuyResult result = Execute(graph, actor, payload, boundary,
            () => { merchantAdmission++; return true; }, () => merchantNotifications++,
            () => { travelAdmission++; return true; }, () => travelNotifications++);

        Assert.That(result.Succeeded, Is.True,
            $"{result.Failure}, merchantTarget={actor.MerchantTradePlan.TargetCityRuntimeId}, travelTarget={actor.TravelPlan.TargetCityRuntimeId}");
        Assert.That(result.Quantity, Is.GreaterThan(0));
        Assert.That(result.UnitPrice, Is.EqualTo(origin.Market.GetPrice(item)).Within(0.001f));
        Assert.That(actor.MoneyAccount.Balance,
            Is.EqualTo(balance - result.Quantity * result.UnitPrice).Within(0.001f));
        Assert.That(origin.Market.GetAmount(item), Is.EqualTo(stock - result.Quantity));
        Assert.That(actor.Inventory.GetAmount(item), Is.EqualTo(inventory + result.Quantity));
        Assert.That(actor.MerchantTradePlan.ItemDefinition, Is.SameAs(item));
        Assert.That(actor.MerchantTradePlan.OriginCityRuntimeId, Is.EqualTo(origin.RuntimeId));
        Assert.That(actor.MerchantTradePlan.TargetCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(actor.MerchantTradePlan.PlannedAmount, Is.EqualTo(result.Quantity));
        Assert.That(actor.MerchantTradePlan.RawRemainingAmount, Is.EqualTo(result.Quantity));
        Assert.That(actor.MerchantTradePlan.PurchasePricePerItem, Is.EqualTo(result.UnitPrice));
        Assert.That(actor.MerchantTradePlan.OriginDecisionId, Is.EqualTo(payload.OriginDecisionId));
        Assert.That(actor.TravelPlan.TargetCityRuntimeId, Is.EqualTo(destination.RuntimeId));
        Assert.That(actor.TravelPlan.OriginDecisionId, Is.EqualTo(payload.OriginDecisionId));
        Assert.That(actor.TravelPlan.ExpectedCost, Is.EqualTo(result.TravelCost));
        Assert.That(boundary.BeginCalls, Is.EqualTo(1));
        Assert.That(boundary.CompleteCalls, Is.EqualTo(1));
        Assert.That(boundary.DisposeCalls, Is.EqualTo(1));
        Assert.That(merchantAdmission, Is.EqualTo(1));
        Assert.That(merchantNotifications, Is.EqualTo(1));
        Assert.That(travelAdmission, Is.EqualTo(1));
        Assert.That(travelNotifications, Is.EqualTo(1));
    }

    [Test]
    public void SelectedCrossCityBuyDispatcherRollsBeforePurchaseAndOwnerPlanWrites()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners origin,
            out _, out DailyV1ItemDefinition item, out DailyV1NpcActionPayloadOwner payload,
            1f);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, payload.Action, payload, null), Is.True);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);
        PurchaseBoundary boundary = new PurchaseBoundary(order);
        float balance = actor.MoneyAccount.Balance;
        int stock = origin.Market.GetAmount(item);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedCrossCityBuyActionSlot(
            graph, actor, payload.Action, random, 31L, boundary,
            () => { order.Add("merchant-admit"); return true; },
            () => order.Add("merchant-commit"),
            () => { order.Add("travel-admit"); return true; },
            () => order.Add("travel-commit"), out var result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.True);
        Assert.That(result.Purchase.Succeeded, Is.True);
        Assert.That(order, Is.EqualTo(new[]
        {
            "roll", "merchant-admit", "travel-admit", "purchase-begin",
            "purchase-complete", "purchase-dispose", "merchant-commit", "travel-commit"
        }));
        Assert.That(actor.MoneyAccount.Balance, Is.LessThan(balance));
        Assert.That(origin.Market.GetAmount(item), Is.LessThan(stock));
    }

    [Test]
    public void SelectedCrossCityBuyFailedRollStopsBeforePurchaseAdmissionOrWrites()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners origin,
            out _, out DailyV1ItemDefinition item, out DailyV1NpcActionPayloadOwner payload,
            0.5f);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, payload.Action, payload, null), Is.True);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.99f);
        PurchaseBoundary boundary = new PurchaseBoundary(order);
        float balance = actor.MoneyAccount.Balance;
        long accountRevision = actor.MoneyAccount.Revision;
        long inventoryRevision = actor.Inventory.Revision;
        long marketRevision = origin.Market.Revision;
        long merchantPlanRevision = actor.MerchantTradePlan.Revision;
        long travelPlanRevision = actor.TravelPlan.Revision;
        int stock = origin.Market.GetAmount(item);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedCrossCityBuyActionSlot(
            graph, actor, payload.Action, random, 32L, boundary,
            () => { order.Add("merchant-admit"); return true; },
            () => order.Add("merchant-commit"),
            () => { order.Add("travel-admit"); return true; },
            () => order.Add("travel-commit"), out var result), Is.True);

        Assert.That(result.ActionRollSucceeded, Is.False);
        Assert.That(result.Purchase, Is.Null);
        Assert.That(order, Is.EqualTo(new[] { "roll" }));
        Assert.That(boundary.BeginCalls, Is.Zero);
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balance));
        Assert.That(actor.MoneyAccount.Revision, Is.EqualTo(accountRevision));
        Assert.That(actor.Inventory.Revision, Is.EqualTo(inventoryRevision));
        Assert.That(origin.Market.Revision, Is.EqualTo(marketRevision));
        Assert.That(origin.Market.GetAmount(item), Is.EqualTo(stock));
        Assert.That(actor.MerchantTradePlan.Revision, Is.EqualTo(merchantPlanRevision));
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(travelPlanRevision));
    }

    [Test]
    public void SelectedCrossCityBuyDispatcherRejectsMismatchedActionBeforeRandomness()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out _, out _, out _,
            out DailyV1NpcActionPayloadOwner payload, 1f);
        Assert.That(graph.TryCommitCurrentActionSlot(actor, payload.Action, payload, null), Is.True);
        DailyV1ActionDefinition wrongAction = graph.Definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue);
        List<string> order = new List<string>();
        FixedActionRandom random = new FixedActionRandom(order, 0.25f);

        Assert.That(DailyV1SelectedActionExecutionStage.TryExecuteSelectedCrossCityBuyActionSlot(
            graph, actor, wrongAction, random, 33L, new PurchaseBoundary(order),
            () => true, () => { }, () => true, () => { }, out _), Is.False);
        Assert.That(random.Calls, Is.Zero);
        Assert.That(order, Is.Empty);
    }

    [TestCase(false, true, "MerchantPlanAdmission")]
    [TestCase(true, false, "TravelPlanAdmission")]
    public void PlanAdmissionRejectionPrecedesEveryPurchaseWrite(
        bool allowMerchant, bool allowTravel, string expectedFailure)
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners origin,
            out _, out DailyV1ItemDefinition item,
            out DailyV1NpcActionPayloadOwner payload);
        float balance = actor.MoneyAccount.Balance;
        long accountRevision = actor.MoneyAccount.Revision;
        long inventoryRevision = actor.Inventory.Revision;
        long marketRevision = origin.Market.Revision;
        int stock = origin.Market.GetAmount(item);
        PurchaseBoundary boundary = new PurchaseBoundary();
        int notifications = 0;

        DailyV1SelectedCrossCityBuyResult result = Execute(graph, actor, payload, boundary,
            () => allowMerchant, () => notifications++,
            () => allowTravel, () => notifications++);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure.ToString(), Is.EqualTo(expectedFailure));
        Assert.That(boundary.BeginCalls, Is.Zero);
        Assert.That(boundary.CompleteCalls, Is.Zero);
        Assert.That(boundary.DisposeCalls, Is.Zero);
        Assert.That(notifications, Is.Zero);
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balance));
        Assert.That(actor.MoneyAccount.Revision, Is.EqualTo(accountRevision));
        Assert.That(actor.Inventory.Revision, Is.EqualTo(inventoryRevision));
        Assert.That(origin.Market.Revision, Is.EqualTo(marketRevision));
        Assert.That(origin.Market.GetAmount(item), Is.EqualTo(stock));
        Assert.That(actor.MerchantTradePlan.Revision, Is.Zero);
        Assert.That(actor.TravelPlan.Revision, Is.Zero);
    }

    [TestCase(true, "MerchantPlanPreflight")]
    [TestCase(false, "TravelPlanPreflight")]
    public void RevisionSaturationPreflightLeavesPurchaseOwnersUntouched(
        bool saturateMerchantPlan, string expectedFailure)
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners origin,
            out _, out DailyV1ItemDefinition item,
            out DailyV1NpcActionPayloadOwner payload);
        if (saturateMerchantPlan)
        {
            Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
                graph.Definitions, null, null, null, 0, 0, 0f, 0, 0, null,
                long.MaxValue, out DailyV1NpcMerchantTradePlanOwner saturated), Is.True);
            ReplaceOwner(actor, "<MerchantTradePlan>k__BackingField", saturated);
        }
        else
        {
            DailyV1NpcTravelPlanOwner saturated = new DailyV1NpcTravelPlanOwner();
            Assert.That(saturated.TryInstallExact(null, null, NpcTravelReason.None,
                0f, 0f, null, long.MaxValue), Is.True);
            ReplaceOwner(actor, "<TravelPlan>k__BackingField", saturated);
        }

        float balance = actor.MoneyAccount.Balance;
        long accountRevision = actor.MoneyAccount.Revision;
        long inventoryRevision = actor.Inventory.Revision;
        long marketRevision = origin.Market.Revision;
        int stock = origin.Market.GetAmount(item);
        PurchaseBoundary boundary = new PurchaseBoundary();
        int admissions = 0;

        DailyV1SelectedCrossCityBuyResult result = Execute(graph, actor, payload, boundary,
            () => { admissions++; return true; }, () => { },
            () => { admissions++; return true; }, () => { });

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure.ToString(), Is.EqualTo(expectedFailure));
        Assert.That(boundary.BeginCalls, Is.Zero);
        Assert.That(admissions, Is.EqualTo(saturateMerchantPlan ? 0 : 1),
            "The merchant-plan admission is preflighted before travel-plan saturation is discovered.");
        Assert.That(actor.MoneyAccount.Balance, Is.EqualTo(balance));
        Assert.That(actor.MoneyAccount.Revision, Is.EqualTo(accountRevision));
        Assert.That(actor.Inventory.Revision, Is.EqualTo(inventoryRevision));
        Assert.That(origin.Market.Revision, Is.EqualTo(marketRevision));
        Assert.That(origin.Market.GetAmount(item), Is.EqualTo(stock));
    }

    [Test]
    public void UnexpectedPostPurchaseTravelOwnerConflictIsReportedAsPartialCommit()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out DailyV1NpcRuntimeOwners actor, out DailyV1CityRuntimeOwners origin,
            out _, out DailyV1ItemDefinition item,
            out DailyV1NpcActionPayloadOwner payload);
        float balance = actor.MoneyAccount.Balance;
        int stock = origin.Market.GetAmount(item);
        PurchaseBoundary boundary = new PurchaseBoundary
        {
            AfterComplete = () => Assert.That(actor.TravelPlan.TryInstallExact(
                null, null, NpcTravelReason.None, 0f, 0f, null, long.MaxValue), Is.True)
        };

        DailyV1SelectedCrossCityBuyResult result = Execute(graph, actor, payload,
            boundary, () => true, () => { }, () => true, () => { });

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.PartialCommit, Is.True);
        Assert.That(result.Failure, Is.EqualTo(DailyV1SelectedCrossCityBuyFailure.PostPurchaseOwnerCommit));
        Assert.That(result.PurchaseCommit.Succeeded, Is.True);
        Assert.That(actor.MoneyAccount.Balance, Is.LessThan(balance));
        Assert.That(origin.Market.GetAmount(item), Is.LessThan(stock));
        Assert.That(actor.MerchantTradePlan.IsActive, Is.True);
        Assert.That(actor.TravelPlan.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CoreRejectsCrossCityBuyForNonMerchantBeforeAdmissionOrWrites()
    {
        CreateFixture(out DailyV1SelectedRuntimeOwnerGraph graph,
            out _, out DailyV1CityRuntimeOwners origin,
            out DailyV1CityRuntimeOwners destination, out DailyV1ItemDefinition item,
            out _);
        DailyV1NpcRuntimeOwners nonMerchant = graph.Npcs.First(candidate =>
            !DailyV1NpcDefinitionRules.HasJobType(candidate.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue));
        DailyV1ActionDefinition buy = graph.Definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(graph.Definitions, buy,
            null, destination.RuntimeId, item, 1, 1f, 1f, NpcTravelReason.None,
            0f, "decision/not-merchant", null, null, null, out var payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(nonMerchant, buy, payload, null), Is.True);
        float balance = nonMerchant.MoneyAccount.Balance;
        int stock = origin.Market.GetAmount(item);
        PurchaseBoundary boundary = new PurchaseBoundary();
        int admissions = 0;

        DailyV1SelectedCrossCityBuyResult result = Execute(graph, nonMerchant,
            payload, boundary, () => { admissions++; return true; }, () => { },
            () => { admissions++; return true; }, () => { });

        Assert.That(result.Failure, Is.EqualTo(DailyV1SelectedCrossCityBuyFailure.InvalidInput));
        Assert.That(boundary.BeginCalls, Is.Zero);
        Assert.That(admissions, Is.Zero);
        Assert.That(nonMerchant.MoneyAccount.Balance, Is.EqualTo(balance));
        Assert.That(origin.Market.GetAmount(item), Is.EqualTo(stock));
    }

    private static DailyV1SelectedCrossCityBuyResult Execute(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        DailyV1NpcActionPayloadOwner payload,
        PurchaseBoundary boundary,
        Func<bool> merchantAdmission,
        Action merchantCommitted,
        Func<bool> travelAdmission,
        Action travelCommitted)
    {
        if (actor.ActionSlot.CurrentPayload == null)
            Assert.That(graph.TryCommitCurrentActionSlot(actor, payload.Action, payload, null), Is.True);
        return DailyV1SelectedCrossCityBuyExecution.TryExecute(
            graph, actor, payload, boundary, merchantAdmission, merchantCommitted,
            travelAdmission, travelCommitted);
    }

    private static void CreateFixture(
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1NpcRuntimeOwners actor,
        out DailyV1CityRuntimeOwners origin,
        out DailyV1CityRuntimeOwners destination,
        out DailyV1ItemDefinition item,
        out DailyV1NpcActionPayloadOwner payload,
        float? buySuccessChance = null)
    {
        DailyV1EffectiveDefinitions source = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions definitions = source;
        if (buySuccessChance.HasValue)
        {
            IEnumerable<DailyV1NpcActionData> actions = source.Actions.Values.Select(action =>
            {
                DailyV1NpcActionData values = action.Values;
                if (values.actionType?.Value != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue)
                    return values;
                return new DailyV1NpcActionData(values.id, values.statusNecessariosParaFazerAcao,
                    values.actionName, values.normalActionLogText, values.actionCategory, values.actionType,
                    values.baseUtility, values.statusModifiers, true, buySuccessChance.Value,
                    values.statusToAdd, values.statusToRemove, values.targetStatusToAdd,
                    values.targetStatusToRemove, values.crimeSettings);
            });
            definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
                source.Configuration, source.EffectiveConfiguration, source.Calendar,
                source.Cities.Values.Select(value => value.Values),
                source.Items.Values.Select(value => value.Values),
                source.Npcs.Values.Select(value => value.Values),
                source.Jobs.Values.Select(value => value.Values), actions,
                source.Statuses.Values.Select(value => value.Values), source.StageOrder,
                source.ProfileContractIdentity, source.GeographyProfileContractIdentity,
                source.GeographyStageIdentity, source.HexCoordinateConvention,
                source.HexCoordinateCanonicalOrder);
        }
        SpatialNetworkOwner spatial = new SpatialNetworkOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions, spatial);
        RuntimeIdAllocator ids = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(ids, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(ids,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        actor = builder.Npcs.First(owner => DailyV1NpcDefinitionRules.HasJobType(
            owner.Definition, definitions, DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && !DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, definitions,
                DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue));
        string originRuntimeId = actor.Travel.CurrentCityRuntimeId;
        DailyV1CityRuntimeOwners originOwner = builder.Cities.Single(owner => string.Equals(owner.RuntimeId,
            originRuntimeId, StringComparison.Ordinal));
        DailyV1CityRuntimeOwners destinationOwner = builder.Cities.First(owner => !ReferenceEquals(owner, originOwner));
        origin = originOwner;
        destination = destinationOwner;
        Assert.That(spatial.TryGetLocation(originOwner.LocationRuntimeId,
            out SpatialLocationRuntime originLocation), Is.True);
        Assert.That(spatial.TryGetLocation(destinationOwner.LocationRuntimeId,
            out SpatialLocationRuntime destinationLocation), Is.True);
        Assert.That(spatial.TryRegisterRoute(new SpatialRouteRuntime(
            "route/cross-city-buy", originLocation, destinationLocation, 2), () => true), Is.True);
        Assert.That(builder.TryBuild(out graph), Is.True);

        item = definitions.Items.Values.First(definition => originOwner.Market.GetAmount(definition) > 0);
        DailyV1ActionDefinition buy = definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue);
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(definitions, buy,
            null, destination.RuntimeId, item, 4, 1f, 1f, NpcTravelReason.None,
            0f, "decision/cross-city-buy", null, null, null, out payload), Is.True);
    }

    private static void ReplaceOwner(DailyV1NpcRuntimeOwners actor, string fieldName, object owner)
    {
        FieldInfo field = typeof(DailyV1NpcRuntimeOwners).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(actor, owner);
    }

    private sealed class PurchaseBoundary : IDailyV1SelectedOpenMarketPurchaseBoundary
    {
        private readonly IList<string> events;
        internal int BeginCalls { get; private set; }
        internal int CompleteCalls { get; private set; }
        internal int DisposeCalls { get; private set; }
        internal Action AfterComplete { get; set; }
        internal PurchaseBoundary(IList<string> events = null) => this.events = events;
        public bool CanUseAccountBacked => true;
        public bool TryBegin() { BeginCalls++; events?.Add("purchase-begin"); return true; }
        public void Complete(DailyV1SelectedOpenMarketPurchaseCommitResult result)
        { CompleteCalls++; events?.Add("purchase-complete"); AfterComplete?.Invoke(); }
        public void Dispose() { DisposeCalls++; events?.Add("purchase-dispose"); }
    }

    private sealed class FixedActionRandom : IAuthoritativeRandomSource
    {
        private readonly IList<string> events;
        private readonly float value;
        internal int Calls { get; private set; }
        internal FixedActionRandom(IList<string> events, float value)
        { this.events = events; this.value = value; }
        public float NextUnit(string streamKey, long drawIndex = 0L)
        { Calls++; events.Add("roll"); return value; }
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }
}
