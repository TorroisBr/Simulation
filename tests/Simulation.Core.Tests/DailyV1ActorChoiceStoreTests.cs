using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1ActorChoiceStoreTests
{
    [Test]
    public void CapturesStableInputAndReturnsFifoForTheSamePerson()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        PersonId actor = new PersonId("person-merchant");

        Assert.That(store.TryCapture("command-1", actor, "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput first, out ActorChoiceStoreFailureCode firstFailure),
            Is.True, firstFailure.ToString());
        Assert.That(store.TryCapture("command-2", new PersonId("person-other"), "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out _, out ActorChoiceStoreFailureCode otherFailure), Is.True, otherFailure.ToString());
        Assert.That(store.TryCapture("command-3", actor, "action.sell-goods",
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 4L,
            out ActorChoiceInput second, out ActorChoiceStoreFailureCode secondFailure),
            Is.True, secondFailure.ToString());

        Assert.That(first.InputId.Value, Is.EqualTo("actor-choice-000001"));
        Assert.That(first.InputSequence, Is.EqualTo(1L));
        Assert.That(first.Origin, Is.EqualTo(WorldCommandOrigin.LocalPlayer));
        Assert.That(first.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
        Assert.That(store.P11InputCount, Is.EqualTo(3));
        Assert.That(store.CensusRevision, Is.EqualTo(3L));
        Assert.That(store.TryGetNextPendingForActor(actor, out ActorChoiceInput pending), Is.True);
        Assert.That(pending.InputId, Is.EqualTo(first.InputId));

        Assert.That(store.TryMarkDispatchStarted(first.InputId, 5L, 0, "decision-1", out _), Is.True);
        Assert.That(store.TryGetNextPendingForActor(actor, out pending), Is.True);
        Assert.That(pending.InputId, Is.EqualTo(second.InputId));
    }

    [Test]
    public void RecordsReturnedStatusesWithoutDependingOnUnityActionResult()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput succeeded = Capture(store, "command-success", "person-success");
        ActorChoiceInput noResult = Capture(store, "command-null", "person-null");
        ActorChoiceInput threw = Capture(store, "command-throw", "person-throw");

        Assert.That(store.TryMarkDispatchStarted(succeeded.InputId, 1L, 0, "decision-success", out _), Is.True);
        Assert.That(store.TryRecordAttemptReturned(succeeded.InputId, 1L, 0,
            NpcActionResultType.Success, out ActorChoiceStoreFailureCode successFailure),
            Is.True, successFailure.ToString());
        Assert.That(store.TryMarkDispatchStarted(noResult.InputId, 1L, 1, "decision-null", out _), Is.True);
        Assert.That(store.TryRecordAttemptReturned(noResult.InputId, 1L, 1,
            null, out ActorChoiceStoreFailureCode nullFailure), Is.True, nullFailure.ToString());
        Assert.That(store.TryMarkDispatchStarted(threw.InputId, 1L, 2, "decision-throw", out _), Is.True);
        Assert.That(store.TryRecordAttemptThrew(threw.InputId, 1L, 2,
            out ActorChoiceStoreFailureCode throwFailure), Is.True, throwFailure.ToString());

        Assert.That(store.TryGet(succeeded.InputId, out ActorChoiceInput succeededValue), Is.True);
        Assert.That(succeededValue.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
        Assert.That(succeededValue.Dispositions[1].ReturnedResultStatus,
            Is.EqualTo(NpcActionResultType.Success));
        Assert.That(store.TryGet(noResult.InputId, out ActorChoiceInput noResultValue), Is.True);
        Assert.That(noResultValue.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.ReturnedNoResult));
        Assert.That(noResultValue.Dispositions[1].ReturnedResultStatus, Is.Null);
        Assert.That(store.TryGet(threw.InputId, out ActorChoiceInput threwValue), Is.True);
        Assert.That(threwValue.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.Threw));
        Assert.That(threwValue.Dispositions[1].ReturnedResultStatus, Is.Null);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void P12MutationBoundaryReceivesOneAdmissionAndCommitPerOwnerWrite()
    {
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        int admissions = 0;
        int commits = 0;
        store.BindP12MutationBoundary(() => { admissions++; return true; }, () => commits++);

        ActorChoiceInput input = Capture(store, "command-p12", "person-p12");
        Assert.That(store.TryDefer(input.InputId, 1L, 0,
            ActorChoiceDeferralReason.ScheduledDirective,
            out ActorChoiceStoreFailureCode deferFailure), Is.True, deferFailure.ToString());

        Assert.That(admissions, Is.EqualTo(2));
        Assert.That(commits, Is.EqualTo(2));
        Assert.That(store.CensusRevision, Is.EqualTo(2L));
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void SelectedCoreTurnPlansInstallsAndClosesTheActorChoiceAgainstExactOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            owner.RuntimeId == actor.Travel.CurrentCityRuntimeId);
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        Assert.That(actor.Inventory.TryAddItem(iron, 5, 10f), Is.True);
        float referencePrice = actor.Inventory.GetAverageUnitCost(iron);
        Assert.That(actor.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, iron, referencePrice + 3f, 12, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        PersonId personId = new PersonId("person-jorge");
        ActorChoiceInput input = Capture(store, "command-selected-sell", personId.Value,
            "VENDER_MERCADORIA");
        NpcDecisionStore decisions = new NpcDecisionStore();
        SelectedActorChoiceTestBoundary boundary = new SelectedActorChoiceTestBoundary(
            graph, allocator, new SimulationRecordSequence(), decisions);

        Assert.That(DailyV1ActorChoiceExecutionStage.TryProcessPending(
            graph, actor, personId, store, 20L, 3, true, true, true, boundary), Is.True);

        Assert.That(boundary.ClearCount, Is.EqualTo(1));
        Assert.That(boundary.AdmissionCount, Is.EqualTo(1));
        Assert.That(boundary.RefreshCount, Is.EqualTo(1));
        Assert.That(boundary.InstallCount, Is.EqualTo(1));
        Assert.That(boundary.ExecuteCount, Is.EqualTo(1));
        Assert.That(boundary.InstalledPayload, Is.Not.Null);
        Assert.That(ReferenceEquals(actor.ActionSlot.CurrentPayload, boundary.InstalledPayload), Is.True);
        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput completed), Is.True);
        Assert.That(completed.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(completed.Dispositions.Select(disposition => disposition.Kind),
            Is.EqualTo(new[]
            {
                ActorChoiceDispositionKind.DispatchStarted,
                ActorChoiceDispositionKind.AttemptReturned
            }));
        Assert.That(decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(completed.Dispositions[0].DecisionRecordId,
            Is.EqualTo(decisions.Decisions.Single().DecisionId));
        Assert.That(decisions.Decisions.Single().Origin, Is.EqualTo(NpcDecisionOrigin.ActorChoice));
        Assert.That(boundary.InstalledPayload.OriginDecisionId,
            Is.EqualTo(decisions.Decisions.Single().DecisionId));
        Assert.That(completed.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void SelectedCoreTurnKeepsTheAttemptRecordWhenP12DeclinesTheSlotCommit()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        DailyV1CityRuntimeOwners city = graph.Cities.Single(owner =>
            owner.RuntimeId == actor.Travel.CurrentCityRuntimeId);
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        Assert.That(actor.Inventory.TryAddItem(iron, 5, 10f), Is.True);
        float referencePrice = actor.Inventory.GetAverageUnitCost(iron);
        Assert.That(actor.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
            city.LocationRuntimeId, iron, referencePrice + 3f, 12, 20L, 20L,
            DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);

        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        PersonId personId = new PersonId("person-jorge");
        ActorChoiceInput input = Capture(store, "command-p12-declines-sell", personId.Value,
            "VENDER_MERCADORIA");
        NpcDecisionStore decisions = new NpcDecisionStore();
        SelectedActorChoiceTestBoundary boundary = new SelectedActorChoiceTestBoundary(
            graph, allocator, new SimulationRecordSequence(), decisions)
        {
            AllowSlotCommit = false
        };

        Assert.That(DailyV1ActorChoiceExecutionStage.TryProcessPending(
            graph, actor, personId, store, 22L, 5, true, true, true, boundary), Is.True);

        Assert.That(boundary.AdmissionCount, Is.EqualTo(1));
        Assert.That(boundary.RefreshCount, Is.EqualTo(1));
        Assert.That(boundary.InstallCount, Is.Zero);
        Assert.That(boundary.ExecuteCount, Is.Zero);
        Assert.That(actor.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(decisions.Decisions, Has.Count.EqualTo(1));
        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput completed), Is.True);
        Assert.That(completed.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(completed.Dispositions[0].DecisionRecordId,
            Is.EqualTo(decisions.Decisions.Single().DecisionId));
        Assert.That(completed.Dispositions[1].AttemptOutcome,
            Is.EqualTo(ActorChoiceAttemptOutcome.ReturnedNoResult));
    }

    [Test]
    public void SelectedCoreTurnClearsTheSlotBeforeRejectingUnavailableCurrentTruth()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        PersonId personId = new PersonId("person-jorge");
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput input = Capture(store, "command-reject-sell", personId.Value,
            "VENDER_MERCADORIA");
        SelectedActorChoiceTestBoundary boundary = new SelectedActorChoiceTestBoundary(
            graph, allocator, new SimulationRecordSequence(), new NpcDecisionStore());

        Assert.That(DailyV1ActorChoiceExecutionStage.TryProcessPending(
            graph, actor, personId, store, 21L, 4, false, true, true, boundary), Is.True);

        Assert.That(boundary.ClearCount, Is.EqualTo(1));
        Assert.That(boundary.InstallCount, Is.Zero);
        Assert.That(boundary.ExecuteCount, Is.Zero);
        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput rejected), Is.True);
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.Dispositions.Single().Failure,
            Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
    }

    [Test]
    public void SelectedCoreTurnRejectsDisabledMerchantProfileWithoutAskingHostToClassifyAction()
    {
        DailyV1EffectiveDefinitions definitions = CreateDefinitionsWithMerchantTradeDisabled();
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions,
            out DailyV1NpcRuntimeOwners actor, out RuntimeIdAllocator allocator);
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput input = Capture(store, "command-disabled-sell", "person-jorge",
            "VENDER_MERCADORIA");
        SelectedActorChoiceTestBoundary boundary = new SelectedActorChoiceTestBoundary(
            graph, allocator, new SimulationRecordSequence(), new NpcDecisionStore());

        Assert.That(DailyV1ActorChoiceExecutionStage.TryProcessPending(
            graph, actor, input.PersonId, store, 23L, 6,
            actorAtCurrentCityLocation: true,
            merchantTradeEnabled: true,
            decisionSystemAvailable: true,
            boundary: boundary), Is.True);

        Assert.That(boundary.ClearCount, Is.EqualTo(1));
        Assert.That(boundary.InstallCount, Is.Zero);
        Assert.That(boundary.ExecuteCount, Is.Zero);
        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput rejected), Is.True);
        Assert.That(rejected.Dispositions.Single().Failure,
            Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
    }

    [Test]
    public void SelectedCoreTurnRejectsUnresolvedActionIdentityWithoutHostClassification()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions,
            out DailyV1NpcRuntimeOwners actor, out RuntimeIdAllocator allocator);
        ActorChoiceStore store = new ActorChoiceStore(new PersonStore());
        ActorChoiceInput input = Capture(store, "command-unresolved-action", "person-jorge",
            "action/not-in-admitted-graph");
        SelectedActorChoiceTestBoundary boundary = new SelectedActorChoiceTestBoundary(
            graph, allocator, new SimulationRecordSequence(), new NpcDecisionStore());

        Assert.That(DailyV1ActorChoiceExecutionStage.TryProcessPending(
            graph, actor, input.PersonId, store, 24L, 7,
            actorAtCurrentCityLocation: true,
            merchantTradeEnabled: true,
            decisionSystemAvailable: true,
            boundary: boundary), Is.True);

        Assert.That(boundary.ClearCount, Is.EqualTo(1));
        Assert.That(boundary.InstallCount, Is.Zero);
        Assert.That(boundary.ExecuteCount, Is.Zero);
        Assert.That(store.TryGet(input.InputId, out ActorChoiceInput rejected), Is.True);
        Assert.That(rejected.Dispositions.Single().Failure,
            Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions,
        out DailyV1NpcRuntimeOwners actor,
        out RuntimeIdAllocator allocator)
    {
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        actor = graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE");
        return graph;
    }

    private static DailyV1EffectiveDefinitions CreateDefinitionsWithMerchantTradeDisabled()
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        EffectiveSimulationConfiguration effective = baseline.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration trade = effective.MerchantTrade;
        EffectiveSimulationConfiguration disabled = new EffectiveSimulationConfiguration(
            effective.Population,
            effective.Economy,
            effective.Travel,
            effective.Crime,
            effective.GuardCrime,
            effective.NaturalMortality,
            effective.AggregateDemography,
            new EffectiveMerchantTradeConfiguration(
                false,
                trade.AllowAutonomousTradeRepositioning,
                trade.MaxTradeAmount,
                trade.LocalWholesalePriceMultiplier,
                trade.LocalReserveRatio,
                trade.MaxUnprofitablePlanWaitDays),
            effective.CommercialKnowledge);

        return DailyV1EffectiveDefinitions.CreateSelectedSchema(
            baseline.Configuration,
            disabled,
            baseline.Calendar,
            baseline.Cities.Values.Select(value => value.Values),
            baseline.Items.Values.Select(value => value.Values),
            baseline.Npcs.Values.Select(value => value.Values),
            baseline.Jobs.Values.Select(value => value.Values),
            baseline.Actions.Values.Select(value => value.Values),
            baseline.Statuses.Values.Select(value => value.Values),
            baseline.StageOrder,
            baseline.ProfileContractIdentity,
            baseline.GeographyProfileContractIdentity,
            baseline.GeographyStageIdentity,
            baseline.HexCoordinateConvention,
            baseline.HexCoordinateCanonicalOrder,
            baseline.Traits.Values.Select(value =>
                new KeyValuePair<string, string>(value.DefinitionId, value.DisplayName)),
            baseline.CapabilityAttributes.Values.Select(value =>
                new KeyValuePair<string, string>(value.DefinitionId, value.DisplayName)),
            baseline.ExplorableSites.Values.Select(value => value.Values));
    }

    private static ActorChoiceInput Capture(
        ActorChoiceStore store,
        string commandId,
        string personId,
        string actionDefinitionId = "action.sell-goods")
    {
        Assert.That(store.TryCapture(commandId, new PersonId(personId), actionDefinitionId,
            WorldCommandOrigin.LocalPlayer, WorldCommandAuthorityMode.Request, 0L,
            out ActorChoiceInput input, out ActorChoiceStoreFailureCode failure),
            Is.True, failure.ToString());
        return input;
    }

    private sealed class SelectedActorChoiceTestBoundary : IDailyV1ActorChoiceExecutionBoundary
    {
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly RuntimeIdAllocator idAllocator;
        private readonly SimulationRecordSequence recordSequence;
        private readonly NpcDecisionStore decisionStore;
        private DailyV1NpcActionPayloadOwner preparedPayload;

        internal SelectedActorChoiceTestBoundary(
            DailyV1SelectedRuntimeOwnerGraph graph,
            RuntimeIdAllocator idAllocator,
            SimulationRecordSequence recordSequence,
            NpcDecisionStore decisionStore)
        {
            this.graph = graph;
            this.idAllocator = idAllocator;
            this.recordSequence = recordSequence;
            this.decisionStore = decisionStore;
        }

        internal int ClearCount { get; private set; }
        internal int AdmissionCount { get; private set; }
        internal int RefreshCount { get; private set; }
        internal int InstallCount { get; private set; }
        internal int ExecuteCount { get; private set; }
        internal DailyV1NpcActionPayloadOwner InstalledPayload { get; private set; }
        internal bool AllowSlotCommit { get; set; } = true;

        public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor) => ClearCount++;

        public bool TryGetDecisionRecordOwners(
            DailyV1NpcRuntimeOwners actor,
            out RuntimeIdAllocator selectedIdAllocator,
            out SimulationRecordSequence selectedRecordSequence,
            out NpcDecisionStore selectedDecisionStore)
        {
            selectedIdAllocator = idAllocator;
            selectedRecordSequence = recordSequence;
            selectedDecisionStore = decisionStore;
            return true;
        }

        public bool TryPrepareRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            preparedPayload = payload;
            return true;
        }

        public bool TryRefreshRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            RefreshCount++;
            return ReferenceEquals(preparedPayload, payload);
        }

        public bool CanCommitRequestedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload) =>
            CanCommitSlot(actor, payload);

        private bool CanCommitSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            AdmissionCount++;
            return AllowSlotCommit && ReferenceEquals(preparedPayload, payload);
        }

        public bool TryPublishRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (!ReferenceEquals(preparedPayload, payload)) return false;
            InstallCount++;
            InstalledPayload = payload;
            preparedPayload = null;
            return true;
        }

        public void CancelPreparedRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            if (ReferenceEquals(preparedPayload, payload)) preparedPayload = null;
        }

        public void LogRequestedActionDecisionFailure(string message) { }

        public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor)
        {
            ExecuteCount++;
            return actor.ActionSlot.CurrentPayload == null
                ? (NpcActionResultType?)null
                : NpcActionResultType.Success;
        }
    }
}
