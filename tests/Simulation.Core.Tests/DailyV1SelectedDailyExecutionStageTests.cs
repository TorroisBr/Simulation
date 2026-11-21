using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1SelectedDailyExecutionStageTests
{
    [TestCase(1L, 1, 4, true)]
    [TestCase(2L, 1, 4, false)]
    [TestCase(1L, 2, 4, false)]
    [TestCase(3L, 3, 2, false)]
    [TestCase(0L, 0, 4, false)]
    [TestCase(-1L, 1, 4, false)]
    public void HostPublishedDailyEconomyEpochMatchesExactOwnerMutationCount(
        long publishedEpochDelta,
        int committedMutationCount,
        int mutationUpperBound,
        bool expected)
    {
        Assert.That(
            DailyV1SelectedDailyExecutionStage.HasExactHostPublishedDailyEconomyMutationEpochDelta(
                publishedEpochDelta, committedMutationCount, mutationUpperBound),
            Is.EqualTo(expected));
    }

    private sealed class CoreMarketOwnerCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1CityRuntimeOwners city;
        private readonly string sectionId;

        internal CoreMarketOwnerCensusProvider(DailyV1CityRuntimeOwners city)
        {
            this.city = city ?? throw new ArgumentNullException(nameof(city));
            string cityKey = P12DCityOwnerImageSectionCodec
                .EncodeCityKeyForSection(city.RuntimeId);
            sectionId = DailyV1SelectedOwnerSectionCensusContract
                .CityMarketStockRowsSectionPrefix + cityKey;
        }

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.Market,
                city.Market.Items.Count,
                city.Market.Revision);
    }

    private sealed class CoreCityPresenceCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1CityRuntimeOwners city;
        private readonly string sectionId;

        internal CoreCityPresenceCensusProvider(DailyV1CityRuntimeOwners city)
        {
            this.city = city ?? throw new ArgumentNullException(nameof(city));
            if (city.NpcPresence == null)
                throw new ArgumentException("The selected City has no Core presence owner.", nameof(city));
            sectionId = DailyV1SelectedOwnerSectionCensusContract
                .CityImportantNpcSectionIdFor(city.RuntimeId);
        }

        public OwnerSectionCensusWitness GetCurrentCensus() =>
            new OwnerSectionCensusWitness(
                sectionId,
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                city.NpcPresence,
                city.NpcPresence.OrderedNpcRuntimeIds.Count,
                city.NpcPresence.Revision);
    }

    private sealed class MutableSelectedOwnerCensusProvider : IOwnerSectionCensusProvider
    {
        internal MutableSelectedOwnerCensusProvider(OwnerSectionCensusSnapshot snapshot) =>
            Set(snapshot);

        internal OwnerSectionCensusWitness Witness { get; private set; }
        internal int? CurrentCardinalityOverride { get; set; }
        internal long? CurrentRevisionOverride { get; set; }
        private DailyV1NpcRuntimeOwners liveStatusOwner;
        private OwnerSectionCensusSnapshot liveStatusBaseline;

        public OwnerSectionCensusWitness GetCurrentCensus() => liveStatusOwner == null
            ? Witness
            : new OwnerSectionCensusWitness(
                liveStatusBaseline.SectionId, liveStatusBaseline.SchemaVersion,
                liveStatusBaseline.OwnerInstanceIdentity,
                CurrentCardinalityOverride ?? liveStatusBaseline.Cardinality,
                CurrentRevisionOverride ?? liveStatusOwner.CrimeJusticeRevision);

        internal void BindNpcStatusOwner(
            OwnerSectionCensusSnapshot baseline,
            DailyV1NpcRuntimeOwners owner)
        {
            if (baseline == null || owner == null)
                throw new ArgumentNullException(baseline == null ? nameof(baseline) : nameof(owner));
            liveStatusBaseline = baseline;
            liveStatusOwner = owner;
        }

        internal void Set(OwnerSectionCensusSnapshot snapshot)
        {
            Witness = new OwnerSectionCensusWitness(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.OwnerInstanceIdentity,
                snapshot.Cardinality, snapshot.Revision);
        }
    }

    private sealed class CoreTheftOutcomesCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly TheftOutcomeStore owner;
        private readonly string sectionId;
        private readonly int schemaVersion;
        private readonly object ownerIdentity;

        internal CoreTheftOutcomesCensusProvider(
            TheftOutcomeStore owner,
            OwnerSectionCensusSnapshot baseline)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            if (!string.Equals(baseline.SectionId,
                    P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId,
                    StringComparison.Ordinal))
                throw new ArgumentException("The theft-outcome census baseline has the wrong section.", nameof(baseline));
            sectionId = baseline.SectionId;
            schemaVersion = baseline.SchemaVersion;
            ownerIdentity = baseline.OwnerInstanceIdentity;
        }

        public OwnerSectionCensusWitness GetCurrentCensus() => new OwnerSectionCensusWitness(
            sectionId, schemaVersion, ownerIdentity, owner.Count, owner.P12CensusRevision);
    }

    private sealed class CoreP12OwnerCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly OwnerSectionCensusSnapshot baseline;
        private readonly object ownerIdentity;
        private readonly System.Func<int> cardinality;
        private readonly System.Func<long> revision;

        internal CoreP12OwnerCensusProvider(
            OwnerSectionCensusSnapshot baseline,
            object ownerIdentity,
            System.Func<int> cardinality,
            System.Func<long> revision)
        {
            this.baseline = baseline ?? throw new System.ArgumentNullException(nameof(baseline));
            if (ownerIdentity == null || !ReferenceEquals(ownerIdentity, baseline.OwnerInstanceIdentity))
                throw new System.ArgumentException("The live P12 provider must preserve its exact baseline owner identity.", nameof(ownerIdentity));
            this.ownerIdentity = ownerIdentity;
            this.cardinality = cardinality ?? throw new System.ArgumentNullException(nameof(cardinality));
            this.revision = revision ?? throw new System.ArgumentNullException(nameof(revision));
        }

        public OwnerSectionCensusWitness GetCurrentCensus() => new OwnerSectionCensusWitness(
            baseline.SectionId, baseline.SchemaVersion, ownerIdentity, cardinality(), revision());
    }
    private sealed class CoreJusticeRecordsCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1JusticeRecordsOwner owner;
        private readonly object censusOwnerIdentity;
        private readonly int schemaVersion;

        internal CoreJusticeRecordsCensusProvider(
            DailyV1JusticeRecordsOwner owner,
            OwnerSectionCensusSnapshot baseline)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            if (!string.Equals(baseline.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
                    StringComparison.Ordinal))
                throw new ArgumentException("The Justice census baseline has the wrong section.", nameof(baseline));
            censusOwnerIdentity = baseline.OwnerInstanceIdentity;
            schemaVersion = baseline.SchemaVersion;
        }

        public OwnerSectionCensusWitness GetCurrentCensus() => new OwnerSectionCensusWitness(
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            schemaVersion,
            censusOwnerIdentity,
            owner.WantedRecords.Count + owner.Sentences.Count,
            owner.Revision);
    }

    [Test]
    public void AdvanceRunsSelectedDailyPhasesInTheEstablishedOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary();
        ScheduledDirectiveStore directives = CreateDirectiveForFirstNpc(graph, 23L);
        long[] marketRevisionsBefore = graph.Cities.Select(city => city.Market.Revision).ToArray();
        long[] spatialKnowledgeRevisionsBefore = graph.Npcs
            .Select(npc => npc.SpatialKnowledge.Revision).ToArray();

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, directives, boundary);

        List<string> expectedCalls = new List<string>
        {
            "logger:23", "begin-day",
            "economy-preflight", "knowledge-preflight", "actor-context"
        };
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc.Lifecycle.LifeState == NpcLifeState.Alive)
            {
                expectedCalls.Add("actor-merchant:" + npc.RuntimeId);
                if (ReferenceEquals(npc, graph.Npcs[0])) expectedCalls.Add("directive");
            }
        }
        expectedCalls.AddRange(new[] { "travel-context", "travel-results", "travel-observation" });
        Assert.That(boundary.Calls, Is.EqualTo(expectedCalls));
        Assert.That(boundary.Calls, Does.Not.Contain("place-content"));
        Assert.That(boundary.Calls, Does.Not.Contain("expeditions"));
        Assert.That(boundary.Calls, Does.Not.Contain("autonomous-expeditions"),
            "The accepted profile has no place-content, explorable-site, or autonomy producer to invoke.");
        Assert.That(directives.TryTakeDirective(graph.Npcs[0].RuntimeId,
            out _), Is.False, "The Core actor-turn stage consumes its prepared selected directive.");
        Assert.That(graph.Cities.Select(city => city.Market.Revision).ToArray(),
            Is.Not.EqualTo(marketRevisionsBefore),
            "The Core phase owns and commits the selected daily Economy writes.");
        Assert.That(graph.Npcs.Select(npc => npc.SpatialKnowledge.Revision).ToArray(),
            Is.Not.EqualTo(spatialKnowledgeRevisionsBefore),
            "The Core phase owns and commits the selected local Knowledge writes.");
    }

    [Test]
    public void AdvanceRunsSelectedCrimeJusticeScopesAndMerchantUrgencyInCoreDayStartPosition()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            new DailyV1JusticeRecordsOwner());
        Assert.That(graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled, Is.True,
            "The positive plan-urgency case is admitted by the immutable selected configuration.");
        DailyV1NpcRuntimeOwners localMerchant = graph.Npcs.First(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, localMerchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners target = graph.Cities.First(city => !ReferenceEquals(city, origin));
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(graph.TryInstallMerchantTradePlan(localMerchant, item,
            origin.RuntimeId, target.RuntimeId, 2, 1f, "decision/day-start-plan"), Is.True);
        long planRevision = localMerchant.MerchantTradePlan.Revision;

        FakeBoundary boundary = new FakeBoundary
        {
            UseSelectedDayStartContext = true,
            CrimeSystemPresent = true,
            JusticeSystemPresent = true
        };

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Not.Contain("begin-day"),
            "The admitted selected graph runs the Core day-start stages directly.");
        Assert.That(boundary.DayStartPhases, Is.EqualTo(new[]
        {
            DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceHiddenStatuses,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
            DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses
        }), "The four existing P12 daily scopes retain their order.");
        Assert.That(boundary.Calls.IndexOf("merchant-plan"), Is.GreaterThan(
            boundary.Calls.IndexOf("crime:" + DailyV1SelectedCrimeJusticeDailyPhase.SyncWantedStatuses)));
        Assert.That(boundary.Calls.IndexOf("merchant-plan"), Is.LessThan(
            boundary.Calls.IndexOf("economy-preflight")));
        Assert.That(boundary.Calls.IndexOf("merchant-plan"), Is.LessThan(
            boundary.Calls.IndexOf("actor-context")));
        Assert.That(localMerchant.MerchantTradePlan.HasData, Is.True,
            "The day-start urgency phase preserves an admitted plan; actor-turn Merchant eligibility handles local-plan cleanup later.");
        Assert.That(localMerchant.MerchantTradePlan.PendingTravelDays, Is.EqualTo(1),
            "The day-start urgency phase advances the exact active plan's pending-travel count.");
        Assert.That(localMerchant.MerchantTradePlan.Revision, Is.EqualTo(planRevision + 1L));
    }

    [Test]
    public void CoreActorTurnPreservesDirectCrimeJusticeMutationWhenHostAdmissionIsDisabled()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        FakeBoundary hostBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator,
            composition.RandomSource,
            composition.RecordSequence,
            composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            CrimeJusticeAdmissionEnabled = false
        };

        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, composition.OwnerGraph, hostBoundary,
            out IDailyV1SelectedActorTurnExecutionContext context), Is.True,
            "The fixture supplies the admitted Core actor-turn roots and the existing compatibility host port.");
        Assert.That(context.Boundary, Is.InstanceOf<DailyV1CoreSelectedActorTurnExecutionBoundary>());

        int mutationCount = 0;
        ((IDailyV1SelectedCrimeJusticeDailyBoundary)context.Boundary).ExecuteWithinDailyMutation(
            DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
            () => mutationCount++);

        Assert.That(mutationCount, Is.EqualTo(1),
            "The compatibility path executes the existing mutation directly when the host has not admitted the P12 operation.");
        Assert.That(hostBoundary.LegacyCrimeJusticeDailyMutations, Is.Zero,
            "Core must not delegate this selected operation through the host's legacy mutation wrapper.");
        Assert.That(state.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
            composition.OwnerGraph, out _), Is.False,
            "A non-admitted host binding is not pinned as the Core session's admitted P12 identity.");
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
    }
    [Test]
    public void CoreActorTurnUsesSealedCoreActionCensusWithoutHostMutationProtocol()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition, 0L, out DailyV1ActiveRuntimeSession session,
            out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        OwnerSectionCensusSnapshot[] completeVector =
            DailyV1ApplicationSessionGateTests.CreateCoreBoundCompleteOwnerSectionVector(session);
        List<DailyV1OwnerSectionContractIdentity> contracts =
            new List<DailyV1OwnerSectionContractIdentity>(completeVector.Length);
        List<DailyV1OwnerSectionCensusProviderRegistration> registrations =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(completeVector.Length);
        Dictionary<string, MutableSelectedOwnerCensusProvider> providers =
            new Dictionary<string, MutableSelectedOwnerCensusProvider>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot snapshot in completeVector)
        {
            MutableSelectedOwnerCensusProvider provider =
                new MutableSelectedOwnerCensusProvider(snapshot);
            if (string.Equals(snapshot.SectionId,
                    DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(actor.RuntimeId),
                    StringComparison.Ordinal))
                provider.BindNpcStatusOwner(snapshot, actor);
            providers.Add(snapshot.SectionId, provider);
            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));
            registrations.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role, provider,
                snapshot.OwnerInstanceIdentity, snapshot.Cardinality, snapshot.Revision,
                hasBaseline: true));
        }

        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
            contracts, registrations), Is.True);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        Assert.That(state.TrySealSelectedOwnerSectionCensusRegistry(registrations), Is.True);
        Assert.That(state.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);

        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            OmitActionSlotP12MutationProtocol = true
        };
        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, graph, dailyBoundary,
            out IDailyV1SelectedActorTurnExecutionContext actorContext), Is.True,
            "The sealed Core census supplies action-slot P12 authority without a host protocol.");
        DailyV1CoreSelectedActorTurnExecutionBoundary coreBoundary =
            actorContext.Boundary as DailyV1CoreSelectedActorTurnExecutionBoundary;
        Assert.That(coreBoundary, Is.Not.Null);
        Assert.That(coreBoundary.OwnsSelectedActionSlotP12Mutation, Is.True);
        Assert.That(dailyBoundary.LastActorTurnHostPorts.ActionSlotP12MutationProtocol, Is.Null);

        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
            + actor.RuntimeId;
        MutableSelectedOwnerCensusProvider actionProvider = providers[sectionId];
        OwnerSectionCensusWitness before = actionProvider.Witness;
        DailyV1ActionDefinition action = graph.Definitions.Actions.Values.FirstOrDefault(candidate =>
            candidate.Values.actionType?.Value != DailyV1NpcDefinitionRules.NormalActionTypeValue);
        Assert.That(action, Is.Not.Null,
            "The selected graph needs a non-Normal slot for the Core fallback rejection proof.");
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            graph.Definitions, action, null, null, null, 0, 0f, 1f,
            NpcTravelReason.None, 0f, "decision/non-normal-core-test", null,
            null, null, out DailyV1NpcActionPayloadOwner actionPayload), Is.True);
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        try
        {
            Assert.That(coreBoundary.CanCommitSelectedActionSlotP12Mutation(graph, actor), Is.True);
            Assert.That(graph.TryCommitCurrentActionSlot(
                actor, action, actionPayload,
                () => coreBoundary.CanCommitSelectedActionSlotP12Mutation(graph, actor)), Is.True);
            actionProvider.Set(new OwnerSectionCensusSnapshot(
                sectionId,
                before.SchemaVersion,
                OwnerSectionRole.Required,
                before.OwnerInstanceIdentity,
                actor.ActionSlot.CurrentAction == null ? 0 : 1,
                actor.ActionSlot.Revision));
            Assert.That(coreBoundary.NotifySelectedActionSlotP12MutationCommitted(
                graph, actor, sectionId, actor.ActionSlot.Revision), Is.True);
        }
        finally
        {
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        }

        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(1L));
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long epoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        OwnerSectionCensusSnapshot refreshed = captured.Single(row => row.SectionId == sectionId);
        Assert.That(refreshed.Revision, Is.EqualTo(before.Revision + 1L));
        Assert.That(refreshed.Cardinality, Is.EqualTo(1));
        Assert.That(epoch, Is.EqualTo(1L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);

        dailyBoundary.RequiresActionProjection = false;
        DailyV1SelectedActionDispatchResult forgedNormalDispatch =
            new DailyV1SelectedActionDispatchResult(
                DailyV1SelectedActionDispatchKind.Normal, true, true);
        Assert.That(coreBoundary.TryCompleteDispatch(actor, forgedNormalDispatch,
            out _), Is.False,
            "A Normal result cannot complete a non-Normal exact action slot without the Unity projection.");
        Assert.That(coreBoundary.TryGetActionStatusBoundary(actor, null,
            reuseExistingOperation: true,
            out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary nestedStatusBoundary), Is.False,
            "A forced outcome that reuses an enclosing Crime/Justice operation must not fall back to a standalone Core status operation.");
        Assert.That(nestedStatusBoundary, Is.Null);

        DailyV1ActionDefinition normalAction = graph.Definitions.Actions["DESCANSAR"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            graph.Definitions, normalAction, null, null, null, 0, 0f, 1f,
            NpcTravelReason.None, 0f, "decision/normal-core-test", null,
            null, null, out DailyV1NpcActionPayloadOwner normalPayload), Is.True);
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        try
        {
            OwnerSectionCensusWitness actionWitness = actionProvider.GetCurrentCensus();
            Assert.That(coreBoundary.CanCommitSelectedActionSlotP12Mutation(graph, actor), Is.True);
            Assert.That(graph.TryCommitCurrentActionSlot(
                actor, normalAction, normalPayload,
                () => coreBoundary.CanCommitSelectedActionSlotP12Mutation(graph, actor)), Is.True);
            actionProvider.Set(new OwnerSectionCensusSnapshot(
                sectionId, actionWitness.SchemaVersion, OwnerSectionRole.Required,
                actionWitness.OwnerInstanceIdentity, actionWitness.Cardinality,
                actor.ActionSlot.Revision));
            Assert.That(coreBoundary.NotifySelectedActionSlotP12MutationCommitted(
                graph, actor, sectionId, actor.ActionSlot.Revision), Is.True);
        }
        finally
        {
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        }
        Assert.That(DailyV1SelectedActorActionExecutionStage.TryExecuteInstalledAction(
            graph, actor, composition.RandomSource, 0L, coreBoundary,
            out bool coreActionSucceeded, out NpcActionResultType? coreActionResult,
            out string coreActionMessage), Is.True);
        Assert.That(coreActionSucceeded, Is.True);
        Assert.That(coreActionResult, Is.EqualTo(NpcActionResultType.Success));
        Assert.That(coreActionMessage, Is.Null);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(2L),
            "Installing the projection-independent action advances the epoch once; its effect-free turn adds no mutation.");

        DailyV1StatusDefinition statusToAdd = graph.Definitions.Statuses.Values.FirstOrDefault(
            status => !actor.Status.Contains(status));
        Assert.That(statusToAdd, Is.Not.Null,
            "The selected actor needs an admitted status transition for the Core-owned mutation proof.");
        long statusRevisionBefore = actor.CrimeJusticeRevision;
        string statusSectionId = DailyV1SelectedOwnerSectionCensusContract
            .NpcStatusCrimeSectionIdFor(actor.RuntimeId);
        OwnerSectionCensusWitness statusWitnessBefore = providers[statusSectionId].GetCurrentCensus();
        Assert.That(ReferenceEquals(statusWitnessBefore.OwnerInstanceIdentity, actor), Is.True);
        MutableSelectedOwnerCensusProvider statusProvider = providers[statusSectionId];
        foreach (int invalidCardinality in new[] { 0, 2 })
        {
            statusProvider.CurrentCardinalityOverride = invalidCardinality;
            statusProvider.CurrentRevisionOverride = statusRevisionBefore + 1L;
            Assert.That(state.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
            try
            {
                Assert.That(state.TryRefreshCoreSelectedOwnerSectionCensusRows(
                    graph, new[] { statusSectionId }, null), Is.False,
                    "A changed NPC status census row must retain exactly one owner.");
                Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(2L));
            }
            finally
            {
                Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            }
        }
        statusProvider.CurrentCardinalityOverride = null;
        statusProvider.CurrentRevisionOverride = null;
        Assert.That(coreBoundary.TryGetActionStatusBoundary(actor, null,
            reuseExistingOperation: false,
            out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary statusBoundary), Is.True,
            "A detached Core host with no Unity mutation adapter uses the sealed census boundary.");
        DailyV1SelectedActionStatusPlanResult statusResult = statusBoundary.ExecuteWithinOperation(
            actor, null, () => DailyV1SelectedActionExecutionStage.TryApplySelectedStatusChanges(
                actor, Array.Empty<DailyV1StatusDefinition>(), new[] { statusToAdd }, statusBoundary)
                    ? DailyV1SelectedActionStatusPlanResult.Applied
                    : DailyV1SelectedActionStatusPlanResult.ActorRejected);
        Assert.That(statusResult, Is.EqualTo(DailyV1SelectedActionStatusPlanResult.Applied));
        Assert.That(actor.Status.Contains(statusToAdd), Is.True);
        Assert.That(actor.CrimeJusticeRevision, Is.EqualTo(statusRevisionBefore + 1L));
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(3L),
            "The Core-owned status write advances the same epoch once after the two prior action-slot writes.");
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out captured, out epoch, out failure), Is.True, failure.ToString());
        OwnerSectionCensusSnapshot refreshedStatus = captured.Single(
            row => row.SectionId == statusSectionId);
        Assert.That(ReferenceEquals(refreshedStatus.OwnerInstanceIdentity,
            statusWitnessBefore.OwnerInstanceIdentity), Is.True);
        Assert.That(refreshedStatus.Cardinality, Is.EqualTo(statusWitnessBefore.Cardinality));
        Assert.That(refreshedStatus.Revision, Is.EqualTo(statusWitnessBefore.Revision + 1L));
        Assert.That(epoch, Is.EqualTo(3L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    [Test]
    public void CoreDecisionAndSequenceAllocationsRefreshTheirExactP12RowsAndEpoch()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition, 0L, out DailyV1ActiveRuntimeSession session,
            out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        OwnerSectionCensusSnapshot[] completeVector =
            DailyV1ApplicationSessionGateTests.CreateCoreBoundCompleteOwnerSectionVector(session);
        List<DailyV1OwnerSectionContractIdentity> contracts =
            new List<DailyV1OwnerSectionContractIdentity>(completeVector.Length);
        List<DailyV1OwnerSectionCensusProviderRegistration> registrations =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(completeVector.Length);
        Dictionary<string, OwnerSectionCensusSnapshot> before =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);

        foreach (OwnerSectionCensusSnapshot snapshot in completeVector)
        {
            IOwnerSectionCensusProvider provider =
                string.Equals(snapshot.SectionId,
                    RuntimeIdAllocatorCensusProvider.DecisionsSectionId, StringComparison.Ordinal)
                    ? RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(
                        composition.RuntimeIdAllocator)
                    : string.Equals(snapshot.SectionId,
                        SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal)
                        ? new SimulationRecordSequenceCensusProvider(composition.RecordSequence)
                        : new MutableSelectedOwnerCensusProvider(snapshot);
            before.Add(snapshot.SectionId, snapshot);
            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));
            registrations.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role, provider,
                snapshot.OwnerInstanceIdentity, snapshot.Cardinality, snapshot.Revision,
                hasBaseline: true));
        }

        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
            contracts, registrations), Is.True);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        Assert.That(state.TrySealSelectedOwnerSectionCensusRegistry(registrations), Is.True);
        Assert.That(state.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);
        Assert.That(state.TryBindSelectedCoreDecisionMutationBoundaries(
            composition.RuntimeIdAllocator, composition.RecordSequence), Is.False,
            "Selected counter owners cannot be bound while the runtime is quiescent outside the admitted turn setup.");
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False,
            "A rejected pre-bind attempt does not fault or mutate the admitted session.");

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        Assert.That(state.TryBeginAdvance(invalidateCaptureToken: false,
            out DailyV1AdvanceLease lease), Is.True);
        Assert.That(state.TryBeginSelectedRuntimeDailyAdvanceOperation(), Is.True);
        try
        {
            Assert.That(state.IsSelectedDailyAdvanceCoreTurnActive, Is.True);
            Assert.That(state.TryBindSelectedCoreDecisionMutationBoundaries(
                composition.RuntimeIdAllocator, composition.RecordSequence), Is.True);

            string decisionId = composition.RuntimeIdAllocator.AllocateDecisionId();
            Assert.That(decisionId, Does.StartWith("decision-"));
            Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(1L),
                "A successful decision allocation refreshes its census row and publishes one epoch.");

            long sequence = composition.RecordSequence.Allocate();
            Assert.That(sequence, Is.GreaterThan(0L));
            Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(2L),
                "The record sequence allocation is a distinct committed owner write and advances once.");
            Assert.That(state.TryValidateCurrentSelectedOwnerSectionCensusBaselines(), Is.True);
        }
        finally
        {
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            lease.Dispose();
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long epoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
        Assert.That(epoch, Is.EqualTo(2L));
        OwnerSectionCensusSnapshot decisionAfter = captured.Single(row =>
            row.SectionId == RuntimeIdAllocatorCensusProvider.DecisionsSectionId);
        OwnerSectionCensusSnapshot sequenceAfter = captured.Single(row =>
            row.SectionId == SimulationRecordSequenceCensusProvider.SectionId);
        Assert.That(ReferenceEquals(decisionAfter.OwnerInstanceIdentity,
            before[decisionAfter.SectionId].OwnerInstanceIdentity), Is.True);
        Assert.That(decisionAfter.Cardinality, Is.EqualTo(before[decisionAfter.SectionId].Cardinality));
        Assert.That(decisionAfter.Revision, Is.EqualTo(before[decisionAfter.SectionId].Revision + 1L));
        Assert.That(ReferenceEquals(sequenceAfter.OwnerInstanceIdentity,
            before[sequenceAfter.SectionId].OwnerInstanceIdentity), Is.True);
        Assert.That(sequenceAfter.Cardinality, Is.EqualTo(before[sequenceAfter.SectionId].Cardinality));
        Assert.That(sequenceAfter.Revision, Is.EqualTo(before[sequenceAfter.SectionId].Revision + 1L));

        long decisionRevision = decisionAfter.Revision;
        Assert.Throws<InvalidOperationException>(() =>
            composition.RuntimeIdAllocator.AllocateDecisionId(),
            "A bound selected allocator rejects writes outside the Core-owned DailyAdvance scope.");
        Assert.That(composition.RuntimeIdAllocator.GetCensusRevision(
            RuntimeIdAllocatorCensusCounter.Decisions), Is.EqualTo(decisionRevision),
            "Rejected out-of-scope allocation does not change the owner revision.");
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.True,
            "An attempted write outside the admitted mutation scope faults the selected census closed.");
    }

    [TestCase(false, false, false, false, false, false, false, false, false, false, false, false, false)]
    [TestCase(true, false, false, false, false, false, false, false, false, false, false, false, false)]
    [TestCase(false, true, false, false, false, false, false, false, false, false, false, false, false)]
    [TestCase(false, false, true, false, false, false, false, false, false, false, false, false, false)]
    [TestCase(false, false, false, true, false, false, false, false, false, false, false, false, false)]
    [TestCase(false, false, false, false, true, false, false, false, false, false, false, false, false)]
    [TestCase(false, false, false, false, false, true, false, false, false, false, false, false, false)]
    [TestCase(false, false, false, false, false, false, true, false, false, false, false, false, false)]
    [TestCase(false, false, false, false, false, false, true, true, false, false, false, false, false)]
    [TestCase(false, false, false, false, false, false, true, false, true, false, false, false, false)]
    [TestCase(false, false, false, false, false, false, false, false, false, true, false, false, false)]
    [TestCase(false, false, false, false, false, false, false, false, false, false, true, false, false)]
    [TestCase(false, false, false, false, false, false, false, false, false, false, true, true, false)]
    [TestCase(false, false, false, false, false, false, false, false, false, false, false, false, true)]
    public void CoreOnlyActorTurnExecutesAdmittedActionWithCompleteP12Census(
        bool prebindMismatchedCrimeReceiptIdentity,
        bool executeCoreOnlyHideAction,
        bool expireCoreOnlyHiddenState,
        bool executeCoreOnlyForcedEscape,
        bool executeCoreOnlyGuardArrest,
        bool executeCoreOnlyFleeCity,
        bool enableCoreOnlyMerchantTrade,
        bool executeCoreOnlyMerchantScopedKnowledgeMutation,
        bool rejectCoreOnlyCrossThreadKnowledgeMutation,
        bool executeCoreOnlySteal,
        bool executeCoreOnlyLocalSellGoodsChoice,
        bool executeCoreOnlyLocalSellGoodsMissingMarketRow,
        bool executeCoreOnlySoloTravel)
    {
        DailyV1EffectiveDefinitions configuredDefinitions = CopyWithMerchantTradeEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            enableCoreOnlyMerchantTrade || executeCoreOnlyLocalSellGoodsChoice);
        if (executeCoreOnlyFleeCity || executeCoreOnlySteal)
            configuredDefinitions = CopyWithCrimeAutonomousEnabled(configuredDefinitions, true);
        DailyV1EffectiveDefinitions definitions = CopyWithConfiguredActions(
            configuredDefinitions,
            executeCoreOnlySoloTravel ? new[]
                {
                    configuredDefinitions.Actions.Values.Single(action =>
                        action.Values.actionType?.Value == DailyV1NpcDefinitionRules.TravelActionTypeValue)
                        .DefinitionId
                }
                : executeCoreOnlyFleeCity ? new[] { "FUGIR_DA_CIDADE" }
                : executeCoreOnlyHideAction ? new[] { "ESCONDER_SE" }
                : executeCoreOnlyGuardArrest ? new[] { "PRENDER" }
                : executeCoreOnlySteal ? new[]
                {
                    configuredDefinitions.Actions.Values.Single(action =>
                        action.Values.actionType?.Value == DailyV1NpcDefinitionRules.StealActionTypeValue)
                        .DefinitionId
                }
                : executeCoreOnlyLocalSellGoodsChoice ? new[]
                {
                    configuredDefinitions.Actions.Values.Single(action =>
                        action.Values.actionType?.Value == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue)
                        .DefinitionId
                }
                : new[] { "DESCANSAR" });
        if (executeCoreOnlyHideAction)
            definitions = CopyWithGuaranteedSuccessfulAction(
                definitions, DailyV1NpcDefinitionRules.HideActionTypeValue);
        if (executeCoreOnlyGuardArrest)
            definitions = CopyWithGuaranteedSuccessfulAction(
                definitions, DailyV1NpcDefinitionRules.ArrestActionTypeValue);
        if (executeCoreOnlyFleeCity)
            definitions = CopyWithGuaranteedSuccessfulAction(
                definitions, DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        if (executeCoreOnlySteal)
            definitions = CopyWithGuaranteedSuccessfulAction(
                definitions, DailyV1NpcDefinitionRules.StealActionTypeValue);
        if (executeCoreOnlyLocalSellGoodsChoice)
            definitions = CopyWithGuaranteedSuccessfulAction(
                definitions, DailyV1NpcDefinitionRules.SellGoodsActionTypeValue);
        if (executeCoreOnlySoloTravel)
            definitions = CopyWithGuaranteedSuccessfulAction(
                definitions, DailyV1NpcDefinitionRules.TravelActionTypeValue);
        if (executeCoreOnlyLocalSellGoodsMissingMarketRow)
        {
            string startingCityDefinitionId = definitions.Configuration.npcs.Single(row =>
                string.Equals(row.npc, "JORGE", StringComparison.Ordinal)).startingCity;
            definitions = CopyWithCityMarketItemRemoved(
                definitions, startingCityDefinitionId, "FERRO");
        }
        DailyV1GenesisComposition composition = CreateSelectedGenesis(
            definitions, hasSoloTravelOperation: executeCoreOnlySoloTravel);
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1StatusDefinition hiddenStatus = definitions.Statuses[
            definitions.Configuration.hiddenStatus];
        DailyV1ActionDefinition stealAction = executeCoreOnlySteal
            ? definitions.Actions.Values.Single(action => action.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.StealActionTypeValue)
            : null;
        definitions.Statuses.TryGetValue(
            definitions.Configuration?.arrestedStatus ?? string.Empty,
            out DailyV1StatusDefinition stealArrestedStatus);
        DailyV1NpcRuntimeOwners guardArrestActor = executeCoreOnlyGuardArrest
            ? graph.Npcs.First(owner => DailyV1NpcDefinitionRules.HasJobType(
                owner.Definition, definitions, DailyV1NpcDefinitionRules.GuardJobTypeValue))
            : null;
        DailyV1NpcRuntimeOwners guardArrestTarget = executeCoreOnlyGuardArrest
            ? graph.Npcs.First(owner => !ReferenceEquals(owner, guardArrestActor)
                && string.Equals(owner.Travel.CurrentCityRuntimeId,
                    guardArrestActor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal))
            : null;
        DailyV1NpcRuntimeOwners staleHiddenActor = executeCoreOnlyGuardArrest
            ? graph.Npcs.First(owner => !ReferenceEquals(owner, guardArrestActor)
                && !ReferenceEquals(owner, guardArrestTarget))
            : graph.Npcs[0];
        DailyV1NpcRuntimeOwners hider = executeCoreOnlyHideAction ? graph.Npcs[1] : null;
        DailyV1NpcRuntimeOwners fleeCityActor = executeCoreOnlyFleeCity ? graph.Npcs[1] : null;
        DailyV1ActionDefinition soloTravelAction = executeCoreOnlySoloTravel
            ? definitions.Actions.Values.Single(action => action.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.TravelActionTypeValue)
            : null;
        DailyV1NpcRuntimeOwners soloTravelActor = executeCoreOnlySoloTravel
            ? graph.Npcs.FirstOrDefault(owner => owner != null
                && owner.Lifecycle?.LifeState == NpcLifeState.Alive
                && owner.Travel != null && !owner.Travel.IsTraveling
                && owner.HiddenState?.RemainingDays <= 0
                && owner.MoneyAccount != null
                && DailyV1NpcDefinitionRules.HasConfiguredDefaultAction(
                    owner.Definition, definitions, soloTravelAction)
                && graph.Cities.Any(city => city != null
                    && string.Equals(city.RuntimeId, owner.Travel.CurrentCityRuntimeId,
                        StringComparison.Ordinal)
                    && city.NpcPresence.Contains(owner.RuntimeId)))
            : null;
        DailyV1CityRuntimeOwners soloTravelDestination = null;
        DailyV1CityRuntimeOwners soloTravelOrigin = null;
        float soloTravelCost = 0f;
        float soloTravelOpeningBalance = 0f;
        long soloTravelOpeningRevision = -1L;
        string soloTravelRouteRuntimeId = null;
        string soloTravelOriginDecisionId = null;
        long soloTravelEventDay = -1L;
        int soloTravelStartedEventsBefore = 0;
        int soloTravelActorDecisionsBefore = 0;
        bool soloTravelRouteKnownBefore = false;
        DailyV1NpcRuntimeOwners stealActor = executeCoreOnlySteal
            ? graph.Npcs.FirstOrDefault(owner => owner != null
                && owner.Travel != null && !owner.Travel.IsTraveling
                && owner.Lifecycle?.LifeState == NpcLifeState.Alive
                && owner.HiddenState?.RemainingDays <= 0
                && owner.MoneyAccount != null
                && DailyV1NpcDefinitionRules.HasConfiguredDefaultAction(
                    owner.Definition, definitions, stealAction)
                && graph.Cities.Any(city => city != null
                    && string.Equals(city.RuntimeId, owner.Travel.CurrentCityRuntimeId,
                        StringComparison.Ordinal)
                    && city.NpcPresence.Contains(owner.RuntimeId))
                && (stealArrestedStatus == null
                    || owner.Status?.Contains(stealArrestedStatus) != true))
            : null;
        DailyV1NpcRuntimeOwners stealVictim = executeCoreOnlySteal
            ? graph.Npcs.FirstOrDefault(owner => owner != null
                && !ReferenceEquals(owner, stealActor)
                && string.Equals(owner.Travel?.CurrentCityRuntimeId,
                    stealActor?.Travel?.CurrentCityRuntimeId, StringComparison.Ordinal)
                && !owner.Travel.IsTraveling
                && owner.Lifecycle?.LifeState == NpcLifeState.Alive
                && owner.HiddenState?.RemainingDays <= 0
                && owner.MoneyAccount?.Balance >= 17f
                && graph.Cities.Any(city => city != null
                    && string.Equals(city.RuntimeId, owner.Travel.CurrentCityRuntimeId,
                        StringComparison.Ordinal)
                    && city.NpcPresence.Contains(owner.RuntimeId))
                && (stealArrestedStatus == null
                    || owner.Status?.Contains(stealArrestedStatus) != true))
            : null;
        DailyV1NpcRuntimeOwners localSellChoiceActor = executeCoreOnlyLocalSellGoodsChoice
            ? graph.Npcs.Single(owner => owner.Definition.DefinitionId == "JORGE")
            : null;
        DailyV1ActionDefinition localSellChoiceAction = executeCoreOnlyLocalSellGoodsChoice
            ? definitions.Actions.Values.Single(action => action.Values.actionType?.Value
                == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue)
            : null;
        PersonId localSellChoicePersonId = executeCoreOnlyLocalSellGoodsChoice
            ? new PersonId("core-only-local-sell-choice-person")
            : null;
        DailyV1CityRuntimeOwners localSellChoiceCity = executeCoreOnlyLocalSellGoodsChoice
            ? graph.Cities.Single(city => city.RuntimeId == localSellChoiceActor.Travel.CurrentCityRuntimeId)
            : null;
        DailyV1ItemDefinition localSellChoiceItem = executeCoreOnlyLocalSellGoodsChoice
            ? definitions.Items["FERRO"]
            : null;
        int localSellInventoryBefore = 0;
        int localSellMarketBefore = 0;
        int localSellMarketRowCountBefore = 0;
        float localSellBalanceBefore = 0f;
        if (executeCoreOnlyLocalSellGoodsChoice)
        {
            Assert.That(localSellChoiceItem, Is.Not.Null,
                "The actor choice resolves an exact admitted item for the selected sale.");
            if (executeCoreOnlyLocalSellGoodsMissingMarketRow)
                Assert.That(localSellChoiceCity.Market.GetItem(localSellChoiceItem), Is.Null,
                    "The first-row regression begins without a row for the selected admitted Market item.");
            Assert.That(localSellChoiceActor.Inventory.TryAddItem(
                localSellChoiceItem, 5, 10f), Is.True,
                "The selected host-neutral SellGoods fixture starts with an exact Core inventory owner and stock.");
            float referencePrice = localSellChoiceActor.Inventory.GetAverageUnitCost(localSellChoiceItem);
            Assert.That(localSellChoiceActor.CommercialKnowledge.RecordObservation(
                new DailyV1CommercialMarketObservation(
                    localSellChoiceCity.LocationRuntimeId, localSellChoiceItem,
                    referencePrice + 3f, 12,
                    composition.SimulationTime.AbsoluteDay, composition.SimulationTime.AbsoluteDay,
                    DailyV1CommercialKnowledgeSource.DirectObservation)), Is.True);
            localSellInventoryBefore = localSellChoiceActor.Inventory.GetAmount(localSellChoiceItem);
            localSellMarketBefore = localSellChoiceCity.Market.GetAmount(localSellChoiceItem);
            localSellMarketRowCountBefore = localSellChoiceCity.Market.Items.Count;
            localSellBalanceBefore = localSellChoiceActor.MoneyAccount.Balance;
        }
        PersonId stealActorPersonId = null;
        PersonId stealVictimPersonId = null;
        CrimeSocialAppraisalWorldState stealSocial = null;
        int stealOutcomesBefore = 0;
        long stealOutcomeRevisionBefore = 0L;
        float stealActorBalanceBefore = 0f;
        float stealVictimBalanceBefore = 0f;
        Dictionary<string, float> stealBalancesBefore = null;
        DailyV1CityRuntimeOwners fleeCityDestination = null;
        DailyV1ActionDefinition fleeCityAction = null;
        float fleeCityExpectedTravelCost = 0f;
        long fleeCityTravelPlanRevisionBefore = -1L;
        DailyV1JusticeWantedRecordOwner guardArrestWarrant = null;
        DailyV1NpcRuntimeOwners forcedEscapeActor = null;
        DailyV1JusticeSentenceOwner forcedEscapeSentence = null;
        ScheduledDirective forcedEscapeDirective = null;
        long justiceRevisionBeforeTurn = -1L;
        if (hider != null)
        {
            DailyV1CityRuntimeOwners hiderCity = graph.Cities.Single(city =>
                string.Equals(city.RuntimeId, hider.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
            Assert.That(graph.JusticeRecordsOwner.TryAddWantedRecord(
                new DailyV1JusticeWantedRecordOwner(hider.RuntimeId, hiderCity.RuntimeId, 12f, 4)), Is.True,
                "The Core Hide proving actor receives one existing canonical Justice warrant before admission.");
            justiceRevisionBeforeTurn = graph.JusticeRecordsOwner.Revision;
        }
        if (executeCoreOnlyForcedEscape)
        {
            Assert.That(composition.SimulationTime.TryAdvanceDay(
                out SimulationTimeAdvanceFailure dayAdvanceFailure), Is.True,
                dayAdvanceFailure.ToString());
            forcedEscapeActor = graph.Npcs.First(owner => !ReferenceEquals(owner, staleHiddenActor));
            DailyV1StatusDefinition arrestedStatus = definitions.Statuses[
                definitions.Configuration.arrestedStatus];
            Assert.That(forcedEscapeActor.Status.TryAdd(arrestedStatus), Is.True,
                "The scheduled Escape proving actor begins with its exact admitted arrested-status owner fact.");
            forcedEscapeActor.NotifyCrimeJusticeMutationCommitted();
            DailyV1CityRuntimeOwners escapeCity = graph.Cities.Single(city =>
                string.Equals(city.RuntimeId, forcedEscapeActor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal));
            DailyV1JusticeWantedRecordOwner escapeWarrant = new DailyV1JusticeWantedRecordOwner(
                forcedEscapeActor.RuntimeId, escapeCity.RuntimeId, 12f, 4);
            Assert.That(graph.JusticeRecordsOwner.TryAddWantedRecord(escapeWarrant), Is.True);
            forcedEscapeSentence = new DailyV1JusticeSentenceOwner(
                forcedEscapeActor.RuntimeId, escapeCity.RuntimeId, escapeWarrant,
                remainingDays: 4, failedEscapeAttempts: 0, wasArrestedToday: true);
            Assert.That(graph.JusticeRecordsOwner.TryAddSentence(forcedEscapeSentence), Is.True,
                "The selected Justice owner contains the exact active sentence/warrant relationship before admission.");
            DailyV1ActionDefinition escapeAction = definitions.Actions.Values.Single(action =>
                action.Values.actionType?.Value
                    == DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
            forcedEscapeDirective = new ScheduledDirective(
                "core-only-forced-escape", composition.SimulationTime.AbsoluteDay,
                ScheduledDirectiveMode.ForceOutcome, ScheduledDirectiveOperation.EscapePrison,
                forcedEscapeActor.RuntimeId, escapeAction.DefinitionId,
                DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue);
            Assert.That(graph.ScheduledDirectives.Add(forcedEscapeDirective), Is.True,
                "The existing P12F scheduled-directive owner admits the bounded ForceOutcome input before the turn.");
            justiceRevisionBeforeTurn = graph.JusticeRecordsOwner.Revision;
        }
        if (executeCoreOnlyGuardArrest)
        {
            DailyV1CityRuntimeOwners guardCity = graph.Cities.Single(city =>
                string.Equals(city.RuntimeId, guardArrestActor.Travel.CurrentCityRuntimeId,
                    StringComparison.Ordinal));
            Assert.That(guardArrestTarget.Travel.IsTraveling, Is.False);
            Assert.That(guardArrestTarget.Status.Contains(definitions.Statuses[
                definitions.Configuration.arrestedStatus]), Is.False);
            Assert.That(graph.JusticeRecordsOwner.FindActiveWantedRecord(
                guardArrestTarget.RuntimeId, guardCity.RuntimeId), Is.Null);
            guardArrestWarrant = new DailyV1JusticeWantedRecordOwner(
                guardArrestTarget.RuntimeId, guardCity.RuntimeId, 100f, 4);
            Assert.That(graph.JusticeRecordsOwner.TryAddWantedRecord(guardArrestWarrant), Is.True,
                "The proving target receives one exact active Justice warrant before selected-profile admission.");
            justiceRevisionBeforeTurn = graph.JusticeRecordsOwner.Revision;
        }
        if (executeCoreOnlyFleeCity)
        {
            fleeCityDestination = graph.Cities.FirstOrDefault(candidate =>
                candidate != null
                && !string.Equals(candidate.RuntimeId, fleeCityActor.Travel?.CurrentCityRuntimeId,
                    StringComparison.Ordinal)
                && !string.Equals(candidate.LocationRuntimeId,
                    fleeCityActor.Travel?.CurrentLocationRuntimeId, StringComparison.Ordinal)
                && graph.TryPlanSelectedSoloTravel(fleeCityActor,
                    fleeCityActor.Travel.CurrentLocationRuntimeId,
                    candidate.LocationRuntimeId,
                    candidate.RuntimeId,
                    out SpatialRouteRuntime route,
                    out float cost)
                && route != null
                && fleeCityActor.MoneyAccount?.CanDebit(cost) == true
                && fleeCityActor.TravelPlan?.CanCommit(
                    candidate.LocationRuntimeId, candidate.RuntimeId,
                    NpcTravelReason.Flee, 80f, cost, "decision/flee-city") == true);
            Assert.That(fleeCityDestination, Is.Not.Null,
                "The admitted DailyAdvance fixture has a valid reachable FleeCity destination.");
            Assert.That(graph.TryPlanSelectedSoloTravel(fleeCityActor,
                fleeCityActor.Travel.CurrentLocationRuntimeId,
                fleeCityDestination.LocationRuntimeId,
                fleeCityDestination.RuntimeId,
                out SpatialRouteRuntime fleeCityRoute,
                out fleeCityExpectedTravelCost), Is.True);
            Assert.That(fleeCityRoute, Is.Not.Null);
            fleeCityAction = definitions.Actions.Values.Single(action =>
                action.Values.actionType?.Value == DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
            Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
                definitions, fleeCityAction, null, fleeCityDestination.RuntimeId, null,
                0, 0f, 1f, NpcTravelReason.None, 0f,
                "decision/flee-city", null, null, null,
                out DailyV1NpcActionPayloadOwner fleeCityPayload), Is.True);
            Assert.That(graph.TryCommitCurrentActionSlot(
                fleeCityActor, fleeCityAction, fleeCityPayload, null), Is.True,
                "The exact admitted Core FleeCity action slot is prepared before the P12 census baseline.");
            fleeCityTravelPlanRevisionBefore = fleeCityActor.TravelPlan.Revision;
        }
        if (executeCoreOnlySoloTravel)
        {
            Assert.That(soloTravelActor, Is.Not.Null);
            Assert.That(DailyV1NpcDefinitionRules.HasConfiguredDefaultAction(
                soloTravelActor.Definition, definitions, soloTravelAction), Is.True,
                "The daily actor stage selects Travel only for an NPC whose admitted default-action policy includes it.");
            Assert.That(DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                definitions, soloTravelAction), Is.True,
                "The exact authored Travel action is admitted for host-neutral autonomous selection.");
            DailyV1CityRuntimeOwners initialSoloTravelOrigin = graph.Cities.Single(city =>
                string.Equals(city.RuntimeId,
                    soloTravelActor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
            DailyV1CityRuntimeOwners undiscoveredRouteOrigin = null;
            foreach (DailyV1CityRuntimeOwners candidateOrigin in graph.Cities)
            {
                if (candidateOrigin == null || ReferenceEquals(candidateOrigin, initialSoloTravelOrigin)
                    || !graph.SpatialNetwork.TryGetLocation(candidateOrigin.LocationRuntimeId,
                        out SpatialLocationRuntime candidateOriginLocation))
                    continue;
                bool hasUndiscoveredDirectRoute = graph.SpatialNetwork
                    .GetOutgoingRoutes(candidateOriginLocation)
                    .Any(route => route != null
                        && !soloTravelActor.SpatialKnowledge.KnowsRoute(route.RuntimeId)
                        && graph.Cities.Any(destination => destination != null
                            && !ReferenceEquals(destination, candidateOrigin)
                            && string.Equals(destination.LocationRuntimeId,
                                route.Destination?.RuntimeId, StringComparison.Ordinal)));
                if (!hasUndiscoveredDirectRoute) continue;
                undiscoveredRouteOrigin = candidateOrigin;
                break;
            }
            Assert.That(undiscoveredRouteOrigin, Is.Not.Null,
                "The authored Core graph has an ordinary route from another canonical City that this actor has not discovered.");
            Assert.That(initialSoloTravelOrigin.NpcPresence.Contains(soloTravelActor.RuntimeId), Is.True);
            Assert.That(undiscoveredRouteOrigin.NpcPresence.Contains(soloTravelActor.RuntimeId), Is.False);
            Assert.That(soloTravelActor.Travel.CanAdvanceRevision(1), Is.True);
            Assert.That(soloTravelActor.Travel.TryInstallState(
                undiscoveredRouteOrigin.LocationRuntimeId,
                undiscoveredRouteOrigin.RuntimeId,
                null, null, 0, 0, null, false, null, null,
                soloTravelActor.Travel.Revision + 1L), Is.True,
                "The pre-admission fixture moves the exact Travel owner to a different canonical City.");
            Assert.That(initialSoloTravelOrigin.NpcPresence.TryRemove(soloTravelActor.RuntimeId), Is.True);
            Assert.That(undiscoveredRouteOrigin.NpcPresence.TryAdd(soloTravelActor.RuntimeId), Is.True);
            soloTravelOrigin = undiscoveredRouteOrigin;
            Assert.That(graph.IsActorAtCurrentCityLocation(soloTravelActor), Is.True,
                "Travel state and the exact Core City-presence owner agree before the P12 baseline is captured.");
            soloTravelDestination = graph.Cities.FirstOrDefault(candidate =>
                candidate != null
                && !ReferenceEquals(candidate, soloTravelOrigin)
                && graph.TryPlanSelectedSoloTravel(soloTravelActor,
                    soloTravelActor.Travel.CurrentLocationRuntimeId,
                    candidate.LocationRuntimeId, candidate.RuntimeId,
                    out SpatialRouteRuntime route, out float cost)
                && route != null
                && soloTravelActor.SpatialKnowledge != null
                && !soloTravelActor.SpatialKnowledge.KnownRouteRuntimeIds
                    .Contains(route.RuntimeId, StringComparer.Ordinal)
                && soloTravelActor.MoneyAccount.CanDebit(cost));
            Assert.That(soloTravelDestination, Is.Not.Null,
                "The selected Core owner graph has a direct, affordable ordinary Travel destination.");
            Assert.That(graph.TryPlanSelectedSoloTravel(soloTravelActor,
                soloTravelActor.Travel.CurrentLocationRuntimeId,
                soloTravelDestination.LocationRuntimeId, soloTravelDestination.RuntimeId,
                out SpatialRouteRuntime soloTravelRoute, out soloTravelCost), Is.True);
            Assert.That(soloTravelRoute, Is.Not.Null);
            soloTravelRouteRuntimeId = soloTravelRoute.RuntimeId;
            soloTravelOriginDecisionId = "decision/core-only-solo-travel";
            soloTravelEventDay = composition.SimulationTime.AbsoluteDay;
            soloTravelRouteKnownBefore = soloTravelActor.SpatialKnowledge.KnownRouteRuntimeIds
                .Contains(soloTravelRouteRuntimeId, StringComparer.Ordinal);
            Assert.That(soloTravelRouteKnownBefore, Is.False,
                "The selected Core fixture requires a genuinely new route-knowledge write.");
            Assert.That(soloTravelActor.TravelPlan.TryCommit(
                soloTravelDestination.LocationRuntimeId, soloTravelDestination.RuntimeId,
                NpcTravelReason.None, 50f, soloTravelCost,
                soloTravelOriginDecisionId), Is.True,
                "The exact Core TravelPlan owner prepares an ordinary route for autonomous DailyAdvance selection.");
            soloTravelOpeningBalance = soloTravelActor.MoneyAccount.Balance;
            soloTravelOpeningRevision = soloTravelActor.Travel.Revision;
            soloTravelStartedEventsBefore = composition.EventAuthority.EventStore.Events
                .OfType<NpcTravelStartedEvent>()
                .Count(domainEvent => string.Equals(
                    domainEvent.ActorRuntimeId, soloTravelActor.RuntimeId, StringComparison.Ordinal));
            soloTravelActorDecisionsBefore = composition.DecisionStore.Decisions.Count(decision =>
                string.Equals(decision.ActorRuntimeId, soloTravelActor.RuntimeId, StringComparison.Ordinal));
        }
        if (executeCoreOnlySteal || executeCoreOnlyLocalSellGoodsChoice)
        {
            if (executeCoreOnlySteal)
            {
                Assert.That(stealActor, Is.Not.Null,
                    "The authored selected profile provides an alive, present actor whose admitted NPC definition enables autonomous Steal.");
                Assert.That(stealVictim, Is.Not.Null,
                    "The authored selected profile provides a co-located living victim with sufficient funds.");
                Assert.That(DailyV1NpcDefinitionRules.HasConfiguredDefaultAction(
                    stealActor.Definition, definitions, stealAction), Is.True,
                    "The daily turn must select Steal through the actor's admitted default-action policy.");
                Assert.That(DailyV1SelectedNpcActionDecisionStage.IsEnabledForAutonomousSelection(
                    definitions, stealAction), Is.True);
                Assert.That(DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                    definitions, stealAction,
                    out _, out _,
                    out IReadOnlyList<DailyV1StatusDefinition> stealTargetStatusToRemove,
                    out IReadOnlyList<DailyV1StatusDefinition> stealTargetStatusToAdd), Is.True);
                Assert.That(stealTargetStatusToRemove, Is.Empty);
                Assert.That(stealTargetStatusToAdd, Is.Empty);
                stealActorPersonId = new PersonId("core-only-daily-steal-actor");
                stealVictimPersonId = new PersonId("core-only-daily-steal-victim");
            }
            else
            {
                Assert.That(localSellChoiceActor, Is.Not.Null);
                Assert.That(localSellChoiceAction, Is.Not.Null);
                Assert.That(DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                    definitions, localSellChoiceAction), Is.True,
                    "The accepted actor choice uses a Local SellGoods action enabled by the admitted Core definitions.");
            }
            DailyV1ExecutionBoundaryState genesisState = composition.ExecutionBoundaryState;
            foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
            {
                PersonId personId = ReferenceEquals(owner, stealActor)
                    ? stealActorPersonId
                    : ReferenceEquals(owner, stealVictim)
                        ? stealVictimPersonId
                        : ReferenceEquals(owner, localSellChoiceActor)
                            ? localSellChoicePersonId
                        : new PersonId("core-only-daily-steal-person-" + owner.RuntimeId.Replace("/", "-"));
                Assert.That(graph.PersonStore.TryRegister(
                    new PersonRuntime(personId), out _), Is.True);
                Assert.That(genesisState.TryCreateSelectedRuntimeActorRosterWithPersonBinding(
                    owner.RuntimeId, personId,
                    out IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
                    out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster), Is.True,
                    "A materialized Person identity is prepared on the exact selected actor roster without changing ordinal order.");
                Assert.That(genesisState.TryCommitSelectedRuntimeActorRoster(
                    expectedRoster, replacementRoster), Is.True);
                Assert.That(graph.PersonStore.TryBindMaterializedNpc(
                    personId, owner.RuntimeId, out _), Is.True);
            }
            if (executeCoreOnlySteal)
            {
                Assert.That(graph.P12EOwners?.CrimeSocialAppraisal, Is.Not.Null);
                stealSocial = graph.P12EOwners.CrimeSocialAppraisal;
                stealOutcomesBefore = stealSocial.TheftOutcomes.Count;
                stealOutcomeRevisionBefore = stealSocial.TheftOutcomes.P12CensusRevision;
                stealActorBalanceBefore = stealActor.MoneyAccount.Balance;
                stealVictimBalanceBefore = stealVictim.MoneyAccount.Balance;
                stealBalancesBefore = graph.Npcs.ToDictionary(
                    owner => owner.RuntimeId, owner => owner.MoneyAccount.Balance,
                    StringComparer.Ordinal);
            }
        }
        Assert.That(staleHiddenActor.HiddenState.RemainingDays, Is.Zero);
        if (expireCoreOnlyHiddenState)
            Assert.That(staleHiddenActor.HiddenState.TrySetRemainingDays(1), Is.True,
                "The exact NPC begins with one admitted hidden day remaining.");
        Assert.That(staleHiddenActor.Status.TryAdd(hiddenStatus), Is.True,
            "The proving fixture starts with an expired hidden-status owner fact to exercise Core reconciliation.");
        staleHiddenActor.NotifyCrimeJusticeMutationCommitted();
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition, 0L, out DailyV1ActiveRuntimeSession session,
            out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        OwnerSectionCensusSnapshot[] initialVector =
            DailyV1ApplicationSessionGateTests.CreateCoreBoundCompleteOwnerSectionVector(session);
        if (executeCoreOnlySteal)
        {
            Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsStructurallyCompleteSnapshot(
                initialVector), Is.True,
                "All required owner-family rows remain complete after exact Person-to-NPC binding.");
        }
        Dictionary<string, OwnerSectionCensusSnapshot> before = initialVector.ToDictionary(
            row => row.SectionId, StringComparer.Ordinal);
        if (executeCoreOnlyLocalSellGoodsChoice)
        {
            string accountSection = DailyV1SelectedOwnerSectionCensusContract
                .NpcMoneyAccountPrefix + localSellChoiceActor.RuntimeId;
            string inventorySection = DailyV1SelectedOwnerSectionCensusContract
                .NpcInventoryPrefix + localSellChoiceActor.RuntimeId;
            string marketSection = DailyV1SelectedOwnerSectionCensusContract
                .CityMarketStockRowsSectionPrefix
                + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(
                    localSellChoiceCity.RuntimeId);
            Assert.That(before[accountSection].OwnerInstanceIdentity,
                Is.SameAs(localSellChoiceActor.MoneyAccount));
            Assert.That(before[inventorySection].OwnerInstanceIdentity,
                Is.SameAs(localSellChoiceActor.Inventory));
            Assert.That(before[marketSection].OwnerInstanceIdentity,
                Is.SameAs(localSellChoiceCity.Market));
        }
        Dictionary<string, IOwnerSectionCensusProvider> liveRuntimeProviders =
            DailyV1NpcOwnerCensusProviders.CreateInventoryProviders(graph.Npcs)
                .Concat(DailyV1NpcOwnerCensusProviders.CreateRuntimeAuthorityProviders(graph))
                .Concat(DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph))
                .Concat(DailyV1NpcOwnerCensusProviders.CreateCommercialKnowledgeProviders(graph.Npcs))
                .Concat(DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs))
                .Concat(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs))
                .ToDictionary(provider => provider.GetCurrentCensus().SectionId,
                    StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            IOwnerSectionCensusProvider presenceProvider =
                new CoreCityPresenceCensusProvider(city);
            liveRuntimeProviders.Add(
                presenceProvider.GetCurrentCensus().SectionId, presenceProvider);
            IOwnerSectionCensusProvider provider = new CoreMarketOwnerCensusProvider(city);
            liveRuntimeProviders.Add(provider.GetCurrentCensus().SectionId, provider);
        }
        liveRuntimeProviders[ScheduledDirectiveStore.SectionId] =
            new ScheduledDirectiveCensusProvider(graph.ScheduledDirectives);
        liveRuntimeProviders[ActorChoiceP11CensusProvider.SectionId] =
            new ActorChoiceP11CensusProvider(state.SelectedActorChoiceStore);
        if (executeCoreOnlySteal)
        {
            OwnerSectionCensusSnapshot theftOutcomeBaseline = initialVector.Single(row =>
                row.SectionId == P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId);
            liveRuntimeProviders[P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId] =
                new CoreTheftOutcomesCensusProvider(stealSocial.TheftOutcomes, theftOutcomeBaseline);
            CrimeSocialAppraisalWorldState crimeSocialWorld = stealSocial;
            OwnerSectionCensusSnapshot crimeKnowledgeBaseline = initialVector.Single(row =>
                row.SectionId == P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId);
            liveRuntimeProviders[P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId] =
                new CoreP12OwnerCensusProvider(crimeKnowledgeBaseline,
                    crimeSocialWorld.CrimeKnowledge,
                    () => crimeSocialWorld.CrimeKnowledge.Count,
                    () => crimeSocialWorld.CrimeKnowledge.P12CensusRevision);
            OwnerSectionCensusSnapshot socialReactionsBaseline = initialVector.Single(row =>
                row.SectionId == P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId);
            liveRuntimeProviders[P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId] =
                new CoreP12OwnerCensusProvider(socialReactionsBaseline,
                    crimeSocialWorld.SocialReactions,
                    () => crimeSocialWorld.SocialReactions.Count,
                    () => crimeSocialWorld.SocialReactions.P12CensusRevision);
        }
        OwnerSectionCensusSnapshot justiceRecordsBaseline = initialVector.Single(row =>
            row.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
        liveRuntimeProviders[DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId] =
            new CoreJusticeRecordsCensusProvider(graph.JusticeRecordsOwner, justiceRecordsBaseline);
        List<DailyV1OwnerSectionContractIdentity> contracts =
            new List<DailyV1OwnerSectionContractIdentity>(initialVector.Length);
        List<DailyV1OwnerSectionCensusProviderRegistration> registrations =
            new List<DailyV1OwnerSectionCensusProviderRegistration>(initialVector.Length);

        foreach (OwnerSectionCensusSnapshot snapshot in initialVector)
        {
            IOwnerSectionCensusProvider provider;
            if (string.Equals(snapshot.SectionId,
                    RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal))
                provider = RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(
                    composition.RuntimeIdAllocator);
            else if (string.Equals(snapshot.SectionId,
                    RuntimeIdAllocatorCensusProvider.DecisionsSectionId, StringComparison.Ordinal))
                provider = RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(
                    composition.RuntimeIdAllocator);
            else if (string.Equals(snapshot.SectionId,
                         SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal))
                provider = new SimulationRecordSequenceCensusProvider(composition.RecordSequence);
            else if (!liveRuntimeProviders.TryGetValue(snapshot.SectionId, out provider))
                provider = new MutableSelectedOwnerCensusProvider(snapshot);

            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));
            registrations.Add(new DailyV1OwnerSectionCensusProviderRegistration(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role, provider,
                snapshot.OwnerInstanceIdentity, snapshot.Cardinality, snapshot.Revision,
                hasBaseline: true));
        }

        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
            contracts, registrations), Is.True);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        Assert.That(state.TrySealSelectedOwnerSectionCensusRegistry(registrations), Is.True);
        Assert.That(state.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);

        ActorChoiceInput localSellChoiceInput = null;
        long localSellChoiceRevisionBefore = state.SelectedActorChoiceStore.CensusRevision;
        if (executeCoreOnlyLocalSellGoodsChoice)
        {
            long choiceCaptureEpochBefore = state.MutationEpochState.CurrentEpoch;
            Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.Application), Is.True,
                "The trusted local command enters through the selected Core application operation boundary.");
            try
            {
                Assert.That(state.SelectedActorChoiceStore.TryCapture(
                    "core-only-local-sell-choice",
                    localSellChoicePersonId,
                    localSellChoiceAction.DefinitionId,
                    WorldCommandOrigin.LocalPlayer,
                    WorldCommandAuthorityMode.Request,
                    state.SimulationTime.AbsoluteDay,
                    out localSellChoiceInput,
                    out ActorChoiceStoreFailureCode choiceFailure), Is.True, choiceFailure.ToString());
            }
            finally
            {
                Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.Application), Is.True);
            }
            Assert.That(state.SelectedActorChoiceStore.CensusRevision,
                Is.EqualTo(localSellChoiceRevisionBefore + 1L));
            Assert.That(state.MutationEpochState.CurrentEpoch,
                Is.EqualTo(choiceCaptureEpochBefore + 1L),
                "P11 command capture updates the exact owner census and publishes one shared epoch.");
        }

        object crimeReceiptIdentity = initialVector.Single(row =>
            row.SectionId == DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId)
            .OwnerInstanceIdentity;
        object justiceReceiptIdentity = initialVector.Single(row =>
            row.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId)
            .OwnerInstanceIdentity;

        if (prebindMismatchedCrimeReceiptIdentity)
        {
            Assert.That(state.TryGetCoreSelectedP12CrimeJusticeMutationBoundary(
                graph,
                out IDailyV1P12CrimeJusticeMutationProtocol mismatchedProtocol,
                out _), Is.True);
            object wrongCrimeReceiptIdentity = new object();
            Assert.That(state.TryBindSelectedCrimeJusticeDailyMutationHostBinding(
                graph,
                new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                    mismatchedProtocol,
                    wrongCrimeReceiptIdentity,
                    wrongCrimeReceiptIdentity,
                    graph.JusticeRecordsOwner,
                    justiceReceiptIdentity,
                    justiceReceiptIdentity,
                    graph.JusticeRecordsOwner,
                    admissionEnabled: true)), Is.True,
                "The setup models a structurally valid host binding whose Crime token differs from the sealed census.");
            Assert.That(state.TryBindSelectedCrimeJusticeDailyMutationBindingFromCoreCensus(graph), Is.False,
                "Core must reject an existing host token that does not match its committed baseline identity.");
            Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.True,
                "The identity mismatch faults the selected census protocol closed.");
            Assert.That(graph.Npcs.All(owner => owner.ActionSlot.CurrentAction == null), Is.True,
                "A mismatched receipt token is rejected before actor selection or mutation.");
            Assert.That(state.SimulationTime.AbsoluteDay, Is.Zero,
                "A mismatched receipt token is rejected before the day advances.");
            return;
        }

        Assert.That(state.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
            graph, out _), Is.False,
            "The Core-only session begins without a Unity-selected Crime/Justice receipt binding.");

        if (rejectCoreOnlyCrossThreadKnowledgeMutation)
        {
            Assert.That(state.TryCreateRestoredContinuationToken(
                state.AdmissionContext, state.ConfigurationIdentity, state.CalendarIdentity,
                state.CompositionProfile, state.WorldId, state.SimulationTime.AbsoluteDay,
                1L, state.MutationEpochState.CurrentEpoch, initialVector,
                out DailyCaptureEligibilityToken priorCaptureToken), Is.True,
                "The regression starts with an actual exact-census continuation token.");
            Assert.That(state.CurrentCaptureToken, Is.SameAs(priorCaptureToken));
        }

        if (executeCoreOnlySteal || executeCoreOnlyLocalSellGoodsChoice)
        {
            Assert.That(state.TryValidateCurrentSelectedOwnerSectionCensusBaselines(), Is.True,
                "The complete Core P12 census remains current after binding the selected actor operation and Person links.");
            Assert.That(state.TryBindSelectedCrimeJusticeDailyMutationBindingFromCoreCensus(graph), Is.True,
                "The exact Crime/Justice P12 host binding is established from the sealed census.");
            Assert.That(state.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding coreCrimeJusticeBinding), Is.True);
            Assert.That(coreCrimeJusticeBinding.AdmissionEnabled, Is.True);
            Assert.That(coreCrimeJusticeBinding.HasExactCoreOwnerRelationships(graph), Is.True);
            Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryValidateCoreOnlyStart(
                state, graph, out string coreOnlyStartDiagnostic), Is.True, coreOnlyStartDiagnostic,
                "The admitted Core actor boundary permits the pending Local SellGoods choice for its roster turn.");
        }
        long mutationEpochBeforeTurn = state.MutationEpochState.CurrentEpoch;
        bool coreOnlyFleeCityActionSucceeded = false;
        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        Assert.That(state.TryBeginAdvance(invalidateCaptureToken: false,
            out DailyV1AdvanceLease lease), Is.True);
        Assert.That(state.TryBeginSelectedRuntimeDailyAdvanceOperation(), Is.True);
        if (executeCoreOnlySteal || executeCoreOnlyLocalSellGoodsChoice)
        {
            Assert.That(state.IsSelectedDailyAdvanceCoreTurnActive, Is.True);
            Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreateCoreOnly(
                state, graph, out IDailyV1SelectedActorTurnExecutionContext admittedCoreContext), Is.True,
                "The active DailyAdvance must admit the exact Core actor context, including the queued P11 choice.");
            Assert.That(admittedCoreContext, Is.Not.Null);
        }
        if (executeCoreOnlyMerchantScopedKnowledgeMutation)
        {
            Assert.That(enableCoreOnlyMerchantTrade, Is.True,
                "The nested Merchant commit regression uses the admitted Merchant profile.");
            Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreateCoreOnly(
                state, graph,
                out IDailyV1SelectedActorTurnExecutionContext merchantContext), Is.True,
                "The exact Core-only execution context binds the selected Merchant operation.");
            DailyV1CoreSelectedActorTurnExecutionBoundary merchantBoundary =
                merchantContext.Boundary as DailyV1CoreSelectedActorTurnExecutionBoundary;
            Assert.That(merchantBoundary, Is.Not.Null);
            DailyV1NpcRuntimeOwners merchantActor = graph.Npcs.First(owner =>
                DailyV1NpcDefinitionRules.HasJobType(owner.Definition, definitions,
                    DailyV1NpcDefinitionRules.MerchantJobTypeValue));
            DailyV1CityRuntimeOwners observationCity = null;
            DailyV1ItemDefinition observationItem = null;
            foreach (DailyV1CityRuntimeOwners candidateCity in graph.Cities)
            {
                foreach (DailyV1ItemDefinition candidateItem in definitions.Items.Values)
                {
                    if (merchantActor.CommercialKnowledge.TryGetObservation(
                        candidateCity.LocationRuntimeId, candidateItem.DefinitionId, out _))
                        continue;
                    observationCity = candidateCity;
                    observationItem = candidateItem;
                    break;
                }
                if (observationCity != null) break;
            }
            bool addsMarketObservation = observationCity != null;
            string mutationLocationRuntimeId = observationCity?.LocationRuntimeId;
            DailyV1ItemDefinition mutationItem = observationItem;
            if (!addsMarketObservation)
            {
                DailyV1CommercialMarketObservation replaceableObservation =
                    merchantActor.CommercialKnowledge.Observations.FirstOrDefault(existing =>
                        existing.Source != DailyV1CommercialKnowledgeSource.DirectObservation
                        || existing.ObservedDay < state.SimulationTime.AbsoluteDay);
                Assert.That(replaceableObservation, Is.Not.Null,
                    "The selected Core owner exposes a valid initial or stale fact that can be directly re-observed.");
                mutationLocationRuntimeId = replaceableObservation.LocationRuntimeId;
                mutationItem = replaceableObservation.ItemDefinition;
            }
            Assert.That(mutationItem, Is.Not.Null);

            string marketSectionId =
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix
                + merchantActor.RuntimeId;
            string liquiditySectionId =
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix
                + merchantActor.RuntimeId;
            string receiptsSectionId =
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix
                + merchantActor.RuntimeId;
            string[] merchantSectionIds =
            {
                marketSectionId, liquiditySectionId, receiptsSectionId
            };
            OwnerSectionCensusWitness marketBefore =
                liveRuntimeProviders[marketSectionId].GetCurrentCensus();
            OwnerSectionCensusWitness liquidityBefore =
                liveRuntimeProviders[liquiditySectionId].GetCurrentCensus();
            OwnerSectionCensusWitness receiptsBefore =
                liveRuntimeProviders[receiptsSectionId].GetCurrentCensus();
            long merchantRevisionBefore = merchantActor.CommercialKnowledge.Revision;
            long merchantEpochBefore = state.MutationEpochState.CurrentEpoch;
            DailyV1CommercialMarketObservation nestedObservation =
                new DailyV1CommercialMarketObservation(
                    mutationLocationRuntimeId, mutationItem, 3f, 2,
                    state.SimulationTime.AbsoluteDay, state.SimulationTime.AbsoluteDay,
                    DailyV1CommercialKnowledgeSource.DirectObservation);

            Assert.That(merchantBoundary.ExecuteWithinSelectedMerchantDailyOperation(
                merchantActor,
                () => merchantActor.CommercialKnowledge.RecordObservation(nestedObservation)), Is.True,
                "A real Core CommercialKnowledge owner commit executes inside the P12 Merchant operation.");
            Assert.That(merchantActor.CommercialKnowledge.Revision,
                Is.EqualTo(merchantRevisionBefore + 1L));
            Assert.That(state.MutationEpochState.CurrentEpoch,
                Is.EqualTo(merchantEpochBefore + 1L),
                "The nested Merchant transaction publishes exactly one shared epoch at operation close. Rejection="
                    + state.LastSelectedMerchantDailyTradeRejection + "; census="
                    + state.LastSelectedOwnerSectionCensusRefreshRejection);
            Assert.That(state.TryValidateCoreSelectedOwnerSectionCensusRows(
                graph, merchantSectionIds, null), Is.True,
                "The Merchant commit refreshes all exact CommercialKnowledge census siblings.");

            OwnerSectionCensusWitness marketAfter =
                liveRuntimeProviders[marketSectionId].GetCurrentCensus();
            OwnerSectionCensusWitness liquidityAfter =
                liveRuntimeProviders[liquiditySectionId].GetCurrentCensus();
            OwnerSectionCensusWitness receiptsAfter =
                liveRuntimeProviders[receiptsSectionId].GetCurrentCensus();
            Assert.That(marketAfter.OwnerInstanceIdentity,
                Is.SameAs(merchantActor.CommercialKnowledge));
            Assert.That(liquidityAfter.OwnerInstanceIdentity,
                Is.SameAs(merchantActor.CommercialKnowledge));
            Assert.That(receiptsAfter.OwnerInstanceIdentity,
                Is.SameAs(merchantActor.CommercialKnowledge));
            Assert.That(marketAfter.Cardinality, Is.EqualTo(marketBefore.Cardinality + (addsMarketObservation ? 1 : 0)));
            Assert.That(liquidityAfter.Cardinality, Is.EqualTo(liquidityBefore.Cardinality));
            Assert.That(receiptsAfter.Cardinality, Is.EqualTo(receiptsBefore.Cardinality));
            Assert.That(marketAfter.Revision, Is.EqualTo(merchantRevisionBefore + 1L));
            Assert.That(liquidityAfter.Revision, Is.EqualTo(marketAfter.Revision));
            Assert.That(receiptsAfter.Revision, Is.EqualTo(marketAfter.Revision));
        }
        if (executeCoreOnlyFleeCity)
        {
            Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreateCoreOnly(
                    state, graph,
                    out IDailyV1SelectedActorTurnExecutionContext actorTurnContext), Is.True,
                "The normal Core-only actor-turn factory admits the exact FleeCity slot under DailyAdvance.");
            Assert.That(actorTurnContext.Boundary,
                Is.InstanceOf<DailyV1CoreSelectedActorTurnExecutionBoundary>());
            Assert.That(actorTurnContext.Boundary,
                Is.InstanceOf<IDailyV1CoreActorChoiceExecutionBoundary>());
            Assert.That(DailyV1SelectedActorActionExecutionStage.TryExecuteInstalledAction(
                    graph, fleeCityActor, state.SelectedRandomSource,
                    state.SimulationTime.AbsoluteDay,
                    (IDailyV1SelectedActorActionExecutionBoundary)actorTurnContext.Boundary,
                    out coreOnlyFleeCityActionSucceeded, out _, out string fleeResultMessage), Is.True,
                fleeResultMessage);
            Assert.That(coreOnlyFleeCityActionSucceeded, Is.True,
                "The admitted Core actor-turn boundary executes its exact FleeCity owner operation.");
        }
        if (executeCoreOnlySoloTravel)
        {
            Assert.That(soloTravelActor.TravelPlan.Matches(
                soloTravelDestination.LocationRuntimeId, soloTravelDestination.RuntimeId,
                NpcTravelReason.None, 50f, soloTravelCost,
                soloTravelOriginDecisionId), Is.True,
                "The exact admitted TravelPlan is ready for the normal daily actor stage.");
        }
        FakeBoundary dailyBoundary = new FakeBoundary
        {
            RejectActorTurnHostPorts = true,
            ExpectedCoreTravelDayActor = executeCoreOnlySoloTravel ? soloTravelActor : null,
            ExecutionState = state
        };
        try
        {
            if (rejectCoreOnlyCrossThreadKnowledgeMutation)
            {
                Assert.That(enableCoreOnlyMerchantTrade, Is.True,
                    "The cross-thread refusal regression uses the admitted Merchant owner boundary.");
                Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreateCoreOnly(
                    state, graph,
                    out IDailyV1SelectedActorTurnExecutionContext merchantContext), Is.True,
                    "Core binds the exact CommercialKnowledge owner before the attempted write.");
                DailyV1NpcRuntimeOwners merchantActor = graph.Npcs.First(owner =>
                    DailyV1NpcDefinitionRules.HasJobType(owner.Definition, definitions,
                        DailyV1NpcDefinitionRules.MerchantJobTypeValue));
                DailyV1CityRuntimeOwners observationCity = null;
                DailyV1ItemDefinition observationItem = null;
                foreach (DailyV1CityRuntimeOwners candidateCity in graph.Cities)
                {
                    foreach (DailyV1ItemDefinition candidateItem in definitions.Items.Values)
                    {
                        if (merchantActor.CommercialKnowledge.TryGetObservation(
                            candidateCity.LocationRuntimeId, candidateItem.DefinitionId, out _))
                            continue;
                        observationCity = candidateCity;
                        observationItem = candidateItem;
                        break;
                    }
                    if (observationCity != null) break;
                }
                bool addsMarketObservation = observationCity != null;
                string mutationLocationRuntimeId = observationCity?.LocationRuntimeId;
                DailyV1ItemDefinition mutationItem = observationItem;
                if (!addsMarketObservation)
                {
                    DailyV1CommercialMarketObservation replaceableObservation =
                        merchantActor.CommercialKnowledge.Observations.FirstOrDefault(existing =>
                            existing.Source != DailyV1CommercialKnowledgeSource.DirectObservation
                            || existing.ObservedDay < state.SimulationTime.AbsoluteDay);
                    Assert.That(replaceableObservation, Is.Not.Null,
                        "The attempted operation can upgrade a valid initial or stale owner fact.");
                    mutationLocationRuntimeId = replaceableObservation.LocationRuntimeId;
                    mutationItem = replaceableObservation.ItemDefinition;
                }
                Assert.That(mutationItem, Is.Not.Null);
                DailyV1CommercialMarketObservation rejectedObservation =
                    new DailyV1CommercialMarketObservation(
                        mutationLocationRuntimeId, mutationItem, 4f, 1,
                        state.SimulationTime.AbsoluteDay, state.SimulationTime.AbsoluteDay,
                        DailyV1CommercialKnowledgeSource.DirectObservation);
                Assert.That(merchantActor.CommercialKnowledge.CanImproveWith(rejectedObservation), Is.True,
                    "The candidate is a valid changed fact before thread admission is evaluated.");
                IReadOnlyList<DailyV1CommercialMarketObservation> ownerFactsBefore =
                    merchantActor.CommercialKnowledge.Observations.ToArray();
                long ownerRevisionBefore = merchantActor.CommercialKnowledge.Revision;
                long mutationEpochBeforeRejectedWrite = state.MutationEpochState.CurrentEpoch;
                DailyCaptureEligibilityToken captureTokenBeforeRejectedWrite = state.CurrentCaptureToken;
                Assert.That(captureTokenBeforeRejectedWrite, Is.Not.Null);
                Assert.That(captureTokenBeforeRejectedWrite.BoundaryProvenance,
                    Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));

                bool committedOnWrongThread = System.Threading.Tasks.Task.Run(
                    () => merchantActor.CommercialKnowledge.RecordObservation(rejectedObservation))
                    .GetAwaiter().GetResult();

                Assert.That(committedOnWrongThread, Is.False,
                    "The valid write is refused specifically while the admitted operation is owned by another thread.");
                Assert.That(merchantActor.CommercialKnowledge.Revision,
                    Is.EqualTo(ownerRevisionBefore));
                Assert.That(merchantActor.CommercialKnowledge.Observations.Count,
                    Is.EqualTo(ownerFactsBefore.Count));
                for (int index = 0; index < ownerFactsBefore.Count; index++)
                    Assert.That(merchantActor.CommercialKnowledge.Observations[index],
                        Is.SameAs(ownerFactsBefore[index]),
                        "Refusal occurs before any authoritative owner fact changes.");
                Assert.That(merchantActor.CommercialKnowledge.TryGetObservation(
                    mutationLocationRuntimeId, mutationItem.DefinitionId, out _),
                    Is.EqualTo(!addsMarketObservation));
                Assert.That(state.MutationEpochState.CurrentEpoch,
                    Is.EqualTo(mutationEpochBeforeRejectedWrite));
                Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.True);
                Assert.That(state.CurrentCaptureToken, Is.Null,
                    "The old continuation token is revoked at the rejected owner boundary.");
            }
            else
            {
                DailyV1SelectedDailyExecutionStage.Advance(state, graph, dailyBoundary);
            }
        }
        finally
        {
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
            lease.Dispose();
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }

        if (rejectCoreOnlyCrossThreadKnowledgeMutation)
        {
            Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
                state, DailyV1CaptureScopeAllowance.None,
                out _, out _, out DailyCaptureEligibilityFailure rejectedCaptureFailure), Is.False);
            Assert.That(rejectedCaptureFailure,
                Is.EqualTo(DailyCaptureEligibilityFailure.OperationInProgress),
                "The faulted census registry refuses capture rather than accepting stale owner state.");
            return;
        }

        Assert.That(dailyBoundary.Calls, Does.Contain("actor-host-ports"),
            "The production Daily-v1 preparation must attempt the normal host boundary.");
        Assert.That(dailyBoundary.Calls, Does.Contain("travel-results"),
            "The production daily stage must continue through its travel and completion phases.");
        Assert.That(dailyBoundary.Calls, Does.Contain("travel-observation"));

        Assert.That(state.TryGetSelectedCrimeJusticeDailyMutationHostBinding(
            graph, out DailyV1SelectedCrimeJusticeDailyMutationHostBinding crimeJusticeBinding), Is.True,
            "The Core-only actor-turn path derives the exact P12 binding from its sealed census.");
        Assert.That(crimeJusticeBinding.CrimeOwnerIdentity, Is.SameAs(crimeReceiptIdentity));
        Assert.That(crimeJusticeBinding.CrimeReceiptCensusOwnerIdentity, Is.SameAs(crimeReceiptIdentity));
        Assert.That(crimeJusticeBinding.JusticeOwnerIdentity, Is.SameAs(justiceReceiptIdentity));
        Assert.That(crimeJusticeBinding.JusticeReceiptCensusOwnerIdentity, Is.SameAs(justiceReceiptIdentity));
        Assert.That(crimeJusticeBinding.JusticeRecordsOwner, Is.SameAs(graph.JusticeRecordsOwner));
        Assert.That(crimeJusticeBinding.Protocol,
            Is.TypeOf<DailyV1CoreSelectedOwnerSectionMutationProtocol>(),
            "The standalone Core branch binds the Core protocol, not a host-created substitute.");

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> after,
            out long epoch,
            out DailyCaptureEligibilityFailure failure), Is.True,
            failure + ":" + state.LastSelectedOwnerSectionCensusRefreshRejection + ":"
                + state.LastSelectedP12SoloTravelStartRejection);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(after), Is.True);
        Assert.That(after.Count, Is.EqualTo(initialVector.Length));
        if (executeCoreOnlySoloTravel)
        {
            Assert.That(soloTravelActor.Travel.Revision,
                Is.GreaterThan(soloTravelOpeningRevision));
            Assert.That(soloTravelActor.MoneyAccount.Balance,
                Is.EqualTo(soloTravelOpeningBalance - soloTravelCost));
            Assert.That(soloTravelOrigin.NpcPresence.Contains(soloTravelActor.RuntimeId), Is.False,
                "Core's selected daily stage removes the actor from the exact origin City owner.");

            string travelSectionId = DailyV1SelectedOwnerSectionCensusContract
                .NpcSectionIdFor(12, soloTravelActor.RuntimeId);
            OwnerSectionCensusSnapshot travelAfter = after.Single(row =>
                row.SectionId == travelSectionId);
            Assert.That(travelAfter.OwnerInstanceIdentity, Is.SameAs(soloTravelActor));
            Assert.That(travelAfter.Revision, Is.EqualTo(soloTravelActor.Travel.Revision));

            string originPresenceSectionId = DailyV1SelectedOwnerSectionCensusContract
                .CityImportantNpcSectionIdFor(soloTravelOrigin.RuntimeId);
            OwnerSectionCensusSnapshot originPresenceBefore = before[originPresenceSectionId];
            OwnerSectionCensusSnapshot originPresenceAfter = after.Single(row =>
                row.SectionId == originPresenceSectionId);
            Assert.That(originPresenceAfter.OwnerInstanceIdentity,
                Is.SameAs(soloTravelOrigin.NpcPresence));
            Assert.That(originPresenceAfter.Cardinality,
                Is.EqualTo(originPresenceBefore.Cardinality - 1));
            Assert.That(originPresenceAfter.Revision,
                Is.EqualTo(soloTravelOrigin.NpcPresence.Revision));

            string locationsSectionId = DailyV1SelectedOwnerSectionCensusContract
                .NpcSpatialKnowledgeLocationsPrefix + soloTravelActor.RuntimeId;
            string routesSectionId = DailyV1SelectedOwnerSectionCensusContract
                .NpcSpatialKnowledgeRoutesPrefix + soloTravelActor.RuntimeId;
            OwnerSectionCensusSnapshot locationsAfter = after.Single(row =>
                row.SectionId == locationsSectionId);
            OwnerSectionCensusSnapshot routesAfter = after.Single(row =>
                row.SectionId == routesSectionId);
            Assert.That(locationsAfter.OwnerInstanceIdentity,
                Is.SameAs(soloTravelActor.SpatialKnowledge));
            Assert.That(routesAfter.OwnerInstanceIdentity,
                Is.SameAs(soloTravelActor.SpatialKnowledge));
            Assert.That(locationsAfter.Revision, Is.EqualTo(soloTravelActor.SpatialKnowledge.Revision));
            Assert.That(routesAfter.Revision, Is.EqualTo(locationsAfter.Revision),
                "The location and route knowledge census rows retain their common owner revision.");
            Assert.That(locationsAfter.Cardinality,
                Is.EqualTo(soloTravelActor.SpatialKnowledge.KnownLocationRuntimeIds.Count));
            Assert.That(routesAfter.Cardinality,
                Is.EqualTo(soloTravelActor.SpatialKnowledge.KnownRouteRuntimeIds.Count));
            Assert.That(soloTravelActor.SpatialKnowledge.KnowsRoute(soloTravelRouteRuntimeId), Is.True,
                "Core's ordinary Travel stage commits discovery of its exact route.");
            Assert.That(routesAfter.Cardinality,
                Is.EqualTo(before[routesSectionId].Cardinality + 1),
                "The admitted SoloTravel operation records exactly one newly discovered route row.");
            Assert.That(routesAfter.Revision,
                Is.EqualTo(before[routesSectionId].Revision + 1L),
                "The exact SpatialKnowledge owner revision advances for that new route fact.");

            NpcTravelStartedEvent[] actorTravelStartedEvents = composition.EventAuthority.EventStore.Events
                .OfType<NpcTravelStartedEvent>()
                .Where(domainEvent => string.Equals(
                    domainEvent.ActorRuntimeId, soloTravelActor.RuntimeId, StringComparison.Ordinal))
                .ToArray();
            Assert.That(actorTravelStartedEvents, Has.Length.EqualTo(soloTravelStartedEventsBefore + 1),
                "The Core-only action publishes exactly one authoritative TravelStarted event for its actor.");
            NpcTravelStartedEvent travelStarted = actorTravelStartedEvents.Last();
            Assert.That(travelStarted.OriginLocationRuntimeId,
                Is.EqualTo(soloTravelOrigin.LocationRuntimeId));
            Assert.That(travelStarted.DestinationLocationRuntimeId,
                Is.EqualTo(soloTravelDestination.LocationRuntimeId));
            Assert.That(travelStarted.RouteRuntimeId, Is.EqualTo(soloTravelRouteRuntimeId));
            Assert.That(travelStarted.OriginDecisionId, Is.EqualTo(soloTravelOriginDecisionId));
            Assert.That(travelStarted.AbsoluteDay, Is.EqualTo(soloTravelEventDay));

            OwnerSectionCensusSnapshot eventCounterBefore = before[
                RuntimeIdAllocatorCensusProvider.EventsSectionId];
            OwnerSectionCensusSnapshot eventCounterAfter = after.Single(row =>
                row.SectionId == RuntimeIdAllocatorCensusProvider.EventsSectionId);
            Assert.That(eventCounterAfter.OwnerInstanceIdentity,
                Is.SameAs(eventCounterBefore.OwnerInstanceIdentity));
            Assert.That(eventCounterAfter.Revision,
                Is.GreaterThan(eventCounterBefore.Revision));
            OwnerSectionCensusSnapshot soloTravelSequenceBefore = before[
                SimulationRecordSequenceCensusProvider.SectionId];
            OwnerSectionCensusSnapshot soloTravelSequenceAfter = after.Single(row =>
                row.SectionId == SimulationRecordSequenceCensusProvider.SectionId);
            Assert.That(soloTravelSequenceAfter.OwnerInstanceIdentity,
                Is.SameAs(soloTravelSequenceBefore.OwnerInstanceIdentity));
            Assert.That(soloTravelSequenceAfter.Revision,
                Is.GreaterThan(soloTravelSequenceBefore.Revision));
            Assert.That(travelStarted.RecordSequence,
                Is.GreaterThan(soloTravelSequenceBefore.Revision));
            Assert.That(travelStarted.RecordSequence,
                Is.LessThanOrEqualTo(soloTravelSequenceAfter.Revision));
            Assert.That(state.TryValidateCurrentSelectedOwnerSectionCensusBaselines(), Is.True,
                "The complete selected 89-family census remains current after the normal Core daily stage.");
            Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
            Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(epoch));
            Assert.That(epoch, Is.GreaterThan(mutationEpochBeforeTurn));
            Assert.That(composition.DecisionStore.Decisions.Count(decision =>
                string.Equals(decision.ActorRuntimeId, soloTravelActor.RuntimeId, StringComparison.Ordinal)),
                Is.EqualTo(soloTravelActorDecisionsBefore),
                "An already-installed Travel slot is executed without allocating a second autonomous decision.");
        }
        if (executeCoreOnlyLocalSellGoodsChoice)
        {
            Assert.That(state.SelectedActorChoiceStore.TryGet(
                localSellChoiceInput.InputId, out ActorChoiceInput completedChoice), Is.True);
            Assert.That(completedChoice.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
            Assert.That(completedChoice.Dispositions.Select(disposition => disposition.Kind),
                Is.EqualTo(new[]
                {
                    ActorChoiceDispositionKind.DispatchStarted,
                    ActorChoiceDispositionKind.AttemptReturned
                }));
            Assert.That(completedChoice.Dispositions[0].DecisionRecordId, Is.Not.Null);
            NpcDecisionRecord choiceDecision = composition.DecisionStore.Decisions.Single(decision =>
                decision.ActorRuntimeId == localSellChoiceActor.RuntimeId
                && decision.Origin == NpcDecisionOrigin.ActorChoice);
            Assert.That(choiceDecision.ActionDefinitionId,
                Is.EqualTo(localSellChoiceAction.DefinitionId));
            Assert.That(localSellChoiceActor.Inventory.GetAmount(localSellChoiceItem),
                Is.LessThan(localSellInventoryBefore),
                "Core dispatch executes the requested local sale against the exact Inventory owner.");
            Assert.That(localSellChoiceCity.Market.GetAmount(localSellChoiceItem),
                Is.GreaterThan(localSellMarketBefore),
                "The sale commits through the exact City Market owner.");
            Assert.That(localSellChoiceActor.MoneyAccount.Balance,
                Is.GreaterThan(localSellBalanceBefore),
                "The sale commits its proceeds through the exact actor MoneyAccount owner.");
            string accountSection = DailyV1SelectedOwnerSectionCensusContract
                .NpcMoneyAccountPrefix + localSellChoiceActor.RuntimeId;
            string inventorySection = DailyV1SelectedOwnerSectionCensusContract
                .NpcInventoryPrefix + localSellChoiceActor.RuntimeId;
            string marketSection = DailyV1SelectedOwnerSectionCensusContract
                .CityMarketStockRowsSectionPrefix
                + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(
                    localSellChoiceCity.RuntimeId);
            OwnerSectionCensusSnapshot accountAfter = after.Single(row =>
                row.SectionId == accountSection);
            OwnerSectionCensusSnapshot inventoryAfter = after.Single(row =>
                row.SectionId == inventorySection);
            OwnerSectionCensusSnapshot marketAfter = after.Single(row =>
                row.SectionId == marketSection);
            Assert.That(accountAfter.OwnerInstanceIdentity,
                Is.SameAs(localSellChoiceActor.MoneyAccount));
            Assert.That(inventoryAfter.OwnerInstanceIdentity,
                Is.SameAs(localSellChoiceActor.Inventory));
            Assert.That(marketAfter.OwnerInstanceIdentity,
                Is.SameAs(localSellChoiceCity.Market));
            Assert.That(accountAfter.Revision,
                Is.EqualTo(before[accountSection].Revision + 1L));
            Assert.That(inventoryAfter.Revision,
                Is.EqualTo(before[inventorySection].Revision + 1L));
            Assert.That(marketAfter.Revision,
                Is.GreaterThan(before[marketSection].Revision),
                "Daily consumption/price updates and the actor sale share this completed daily boundary; the MarketSale boundary separately enforces its exact one-write revision delta.");
            Assert.That(marketAfter.Cardinality,
                Is.EqualTo(before[marketSection].Cardinality
                    + (executeCoreOnlyLocalSellGoodsMissingMarketRow ? 1 : 0)),
                "A committed sale may add exactly its selected City Market stock row when that admitted item was previously absent.");
            Assert.That(localSellChoiceCity.Market.Items.Count,
                Is.EqualTo(localSellMarketRowCountBefore
                    + (executeCoreOnlyLocalSellGoodsMissingMarketRow ? 1 : 0)));
            Assert.That(accountAfter.Revision,
                Is.EqualTo(localSellChoiceActor.MoneyAccount.Revision));
            Assert.That(inventoryAfter.Revision,
                Is.EqualTo(localSellChoiceActor.Inventory.Revision));
            Assert.That(marketAfter.Revision,
                Is.EqualTo(localSellChoiceCity.Market.Revision));
            Assert.That(state.MutationEpochState.CurrentEpoch,
                Is.GreaterThanOrEqualTo(mutationEpochBeforeTurn + 3L),
                "The MoneyAccount, Inventory and Market sale commits each publish through the shared P12 epoch.");
            Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
            Assert.That(state.SelectedActorChoiceStore.PendingInputs, Is.Empty,
                "Only terminal actor-choice history remains at the completed P12 boundary.");
            OwnerSectionCensusSnapshot p11After = after.Single(row =>
                row.SectionId == ActorChoiceP11CensusProvider.SectionId);
            OwnerSectionCensusSnapshot p11Before = before[
                ActorChoiceP11CensusProvider.SectionId];
            Assert.That(p11After.OwnerInstanceIdentity,
                Is.SameAs(state.SelectedActorChoiceStore.CensusOwnerIdentity),
                "The P11 row is bound to ActorChoiceStore's exact census identity token.");
            Assert.That(p11After.Cardinality,
                Is.EqualTo(p11Before.Cardinality + 1));
            Assert.That(p11After.Revision,
                Is.GreaterThan(localSellChoiceRevisionBefore + 1L),
                "Core P12 census includes capture and terminal dispatch history on the same ActorChoice owner.");
            Assert.That(state.CurrentCaptureToken, Is.Null,
                "The test invokes the selected daily stage inside manually owned scopes, not the outer runtime-advance publisher; committed choice/sale mutations must revoke the old token until the real completed boundary publishes a new one.");
        }
        if (enableCoreOnlyMerchantTrade)
        {
            string[] commercialPrefixes =
            {
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix,
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix,
                DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix
            };
            OwnerSectionCensusSnapshot[] commercialRows = after.Where(row =>
                commercialPrefixes.Any(prefix => row.SectionId.StartsWith(
                    prefix, StringComparison.Ordinal))).ToArray();
            Assert.That(commercialRows, Is.Not.Empty,
                "The full selected census retains every CommercialKnowledge owner family.");
            Assert.That(commercialRows.Any(row =>
                row.Revision > before[row.SectionId].Revision), Is.True,
                "The Core-only host executes real local market observation writes.");
            foreach (OwnerSectionCensusSnapshot row in commercialRows)
            {
                OwnerSectionCensusSnapshot baseline = before[row.SectionId];
                Assert.That(row.OwnerInstanceIdentity, Is.SameAs(baseline.OwnerInstanceIdentity),
                    "Host-neutral execution keeps the exact P12 owner identity.");
                string prefix = commercialPrefixes.Single(candidate =>
                    row.SectionId.StartsWith(candidate, StringComparison.Ordinal));
                string runtimeId = row.SectionId.Substring(prefix.Length);
                DailyV1NpcRuntimeOwners exactOwner = graph.Npcs.Single(candidate =>
                    string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal));
                Assert.That(row.OwnerInstanceIdentity, Is.SameAs(exactOwner.CommercialKnowledge),
                    "The live Core census is bound to the exact CommercialKnowledge owner in the executing graph.");
                Assert.That(exactOwner.CommercialKnowledge.TryReadCensus(
                    out int markets, out int liquidity, out int receipts, out long ownerRevision), Is.True);
                int expectedCardinality = prefix ==
                    DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix
                    ? markets
                    : prefix == DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix
                        ? liquidity : receipts;
                Assert.That(row.Cardinality, Is.EqualTo(expectedCardinality),
                    "The full P12 vector reflects the live dynamic cardinality of its exact Core owner.");
                Assert.That(row.Revision, Is.EqualTo(ownerRevision),
                    "All CommercialKnowledge census siblings retain the shared owner revision.");
                if (prefix == DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix)
                    Assert.That(row.Cardinality, Is.EqualTo(baseline.Cardinality),
                        "Observation-only daily execution does not create share receipts.");
            }
            Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
            Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(epoch));
            Assert.That(epoch, Is.GreaterThan(mutationEpochBeforeTurn),
                "Committed Core local-knowledge writes publish through the shared mutation epoch.");
        }
        Assert.That(after.Single(row => row.SectionId ==
                DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId)
                .OwnerInstanceIdentity, Is.SameAs(crimeReceiptIdentity));
        Assert.That(after.Single(row => row.SectionId ==
                DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId)
                .OwnerInstanceIdentity, Is.SameAs(justiceReceiptIdentity));
        OwnerSectionCensusSnapshot decisionBefore = before[
            RuntimeIdAllocatorCensusProvider.DecisionsSectionId];
        OwnerSectionCensusSnapshot sequenceBefore = before[
            SimulationRecordSequenceCensusProvider.SectionId];
        OwnerSectionCensusSnapshot decisionAfter = after.Single(row =>
            row.SectionId == RuntimeIdAllocatorCensusProvider.DecisionsSectionId);
        OwnerSectionCensusSnapshot sequenceAfter = after.Single(row =>
            row.SectionId == SimulationRecordSequenceCensusProvider.SectionId);
        string staleHiddenSectionId =
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(
                staleHiddenActor.RuntimeId);
        OwnerSectionCensusSnapshot staleHiddenBefore = before[staleHiddenSectionId];
        OwnerSectionCensusSnapshot staleHiddenAfter = after.Single(row =>
            row.SectionId == staleHiddenSectionId);
        if (executeCoreOnlyFleeCity || executeCoreOnlySoloTravel)
            Assert.That(decisionAfter.Revision, Is.EqualTo(decisionBefore.Revision),
                "The preinstalled FleeCity or ordinary Travel action does not allocate an autonomous decision.");
        else if (executeCoreOnlySteal)
        {
            Assert.That(decisionAfter.Revision, Is.GreaterThan(decisionBefore.Revision),
                "Core autonomous selection records its chosen Steal action in the exact decision owner.");
            NpcDecisionRecord[] stealDecisions = composition.DecisionStore.Decisions
                .Where(decision => string.Equals(decision.ActorRuntimeId,
                    stealActor.RuntimeId, StringComparison.Ordinal)).ToArray();
            Assert.That(stealDecisions, Has.Length.EqualTo(1));
            Assert.That(stealDecisions[0].ActionDefinitionId, Is.EqualTo(stealAction.DefinitionId));
            Assert.That(stealDecisions[0].Origin, Is.EqualTo(NpcDecisionOrigin.Autonomous));
            Assert.That(stealDecisions[0].AbsoluteDay,
                Is.EqualTo(composition.SimulationTime.AbsoluteDay));
        }
        else
            Assert.That(decisionAfter.Revision, Is.GreaterThan(decisionBefore.Revision),
                $"Decision revision must advance for this autonomous-action scenario; before={decisionBefore.Revision}, after={decisionAfter.Revision}, forcedEscape={executeCoreOnlyForcedEscape}.");
        if (executeCoreOnlyFleeCity)
            Assert.That(sequenceAfter.Revision, Is.EqualTo(sequenceBefore.Revision),
                "The preinstalled FleeCity transition emits no decision or record-sequence entry.");
        else if (executeCoreOnlySoloTravel)
            Assert.That(sequenceAfter.Revision, Is.GreaterThan(sequenceBefore.Revision),
                "Core publishes the ordinary TravelStarted event through the selected RecordSequence owner.");
        else if (executeCoreOnlySteal)
            Assert.That(sequenceAfter.Revision, Is.GreaterThan(sequenceBefore.Revision),
                "Core autonomous Steal selection allocates and commits its decision sequence.");
        else
            Assert.That(sequenceAfter.Revision, Is.GreaterThan(sequenceBefore.Revision),
                $"Record sequence must advance for this autonomous-action scenario; before={sequenceBefore.Revision}, after={sequenceAfter.Revision}, forcedEscape={executeCoreOnlyForcedEscape}.");
        Assert.That(staleHiddenActor.Status.Contains(hiddenStatus), Is.False,
            "The Core-owned daily stage reconciles the expired hidden status against its exact NPC owner.");
        Assert.That(staleHiddenActor.HiddenState.RemainingDays, Is.Zero,
            "The Core-owned daily stage advances the exact hidden-day owner.");
        if (executeCoreOnlyFleeCity)
        {
            Assert.That(coreOnlyFleeCityActionSucceeded, Is.True,
                "The Core-only actor-turn factory admitted and executed the exact installed FleeCity slot.");
            Assert.That(fleeCityActor.TravelPlan.Revision,
                Is.EqualTo(fleeCityTravelPlanRevisionBefore + 1L));
            Assert.That(fleeCityActor.TravelPlan.Matches(
                    fleeCityDestination.LocationRuntimeId,
                    fleeCityDestination.RuntimeId,
                    NpcTravelReason.Flee,
                    80f,
                    fleeCityExpectedTravelCost,
                    "decision/flee-city"), Is.True);
            string fleeTravelPlanSectionId =
                DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
                    fleeCityActor.RuntimeId);
            OwnerSectionCensusSnapshot fleePlanBefore = before[fleeTravelPlanSectionId];
            OwnerSectionCensusSnapshot fleePlanAfter = after.Single(row =>
                row.SectionId == fleeTravelPlanSectionId);
            Assert.That(fleePlanAfter.OwnerInstanceIdentity,
                Is.SameAs(fleePlanBefore.OwnerInstanceIdentity));
            Assert.That(fleePlanAfter.Cardinality, Is.EqualTo(fleePlanBefore.Cardinality));
            Assert.That(fleePlanAfter.Revision, Is.EqualTo(fleePlanBefore.Revision + 1L),
                "P12 refreshes the exact TravelPlan census row for Core FleeCity execution.");
            Assert.That(epoch, Is.GreaterThan(mutationEpochBeforeTurn),
                "Core FleeCity publishes its mutation within the active DailyAdvance epoch.");
            Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(epoch));
        }
        Assert.That(staleHiddenAfter.Revision,
            Is.EqualTo(staleHiddenBefore.Revision + (expireCoreOnlyHiddenState ? 2L : 1L)),
            "P12 records hidden-day expiry and status reconciliation on the exact NPC owner.");
        if (executeCoreOnlyFleeCity)
            Assert.That(coreOnlyFleeCityActionSucceeded, Is.True,
                "FleeCity completed through the Core actor-turn boundary; the subsequent autonomous pass may clear an ineligible plan slot.");
        else if (!executeCoreOnlySteal)
            Assert.That(graph.Npcs.Any(owner => owner.ActionSlot.CurrentAction != null), Is.True,
                "At least one exact admitted actor installed and executed a configured Core action.");
        if (executeCoreOnlyHideAction)
        {
            Assert.That(hider.ActionSlot.CurrentAction.Values.actionType.Value,
                Is.EqualTo(DailyV1NpcDefinitionRules.HideActionTypeValue));
            Assert.That(hider.HiddenState.RemainingDays, Is.EqualTo(2),
                "The existing Core Hide transition installs the authored two-day duration.");
            Assert.That(hider.Status.Contains(hiddenStatus), Is.True,
                "The hidden-status fact is committed through the exact Core NPC owner.");
            Assert.That(hider.Status.Contains(definitions.Statuses[definitions.Configuration.wantedStatus]), Is.True,
                "The existing Justice daily stage synchronizes the actor's wanted status from the canonical warrant.");
            Assert.That(graph.JusticeRecordsOwner.Revision, Is.EqualTo(justiceRevisionBeforeTurn),
                "Hide consumes the exact warrant but does not write the Justice owner.");
            string hiderCrimeSectionId =
                DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(hider.RuntimeId);
            OwnerSectionCensusSnapshot hiderBefore = before[hiderCrimeSectionId];
            OwnerSectionCensusSnapshot hiderAfter = after.Single(row => row.SectionId == hiderCrimeSectionId);
            Assert.That(hiderAfter.Revision, Is.EqualTo(hiderBefore.Revision + 3L),
                "P12 records the Hide duration write, hidden-status write, and existing wanted-status synchronization.");
        }
        if (executeCoreOnlyForcedEscape)
        {
            Assert.That(forcedEscapeDirective.State, Is.EqualTo(ScheduledDirectiveState.Succeeded),
                forcedEscapeDirective.ResultReason);
            Assert.That(forcedEscapeDirective.ProcessedDay,
                Is.EqualTo(composition.SimulationTime.AbsoluteDay));
            Assert.That(graph.JusticeRecordsOwner.ContainsSentence(forcedEscapeSentence), Is.False,
                "The same Core Crime/Justice operation removes the exact active sentence owner.");
            Assert.That(graph.JusticeRecordsOwner.ContainsWantedRecord(forcedEscapeSentence.Warrant), Is.True,
                "Escape preserves the canonical warrant owner even after sentence removal.");
            Assert.That(forcedEscapeActor.Status.Contains(definitions.Statuses[
                definitions.Configuration.arrestedStatus]), Is.False);
            Assert.That(forcedEscapeActor.Status.Contains(definitions.Statuses[
                definitions.Configuration.freeStatus]), Is.True);
            Assert.That(forcedEscapeActor.ActionSlot.CurrentAction.Values.actionType.Value,
                Is.EqualTo(DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue));
            Assert.That(forcedEscapeActor.ActionSlot.Revision,
                Is.EqualTo(before[DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
                    + forcedEscapeActor.RuntimeId].Revision + 1L),
                "The forced outcome installs one exact Core action-slot revision before its shared Crime/Justice operation.");
            Assert.That(composition.DecisionStore.Decisions.Any(decision =>
                string.Equals(decision.ActorRuntimeId, forcedEscapeActor.RuntimeId,
                    StringComparison.Ordinal)), Is.False,
                "ForceOutcome executes the selected actor without allocating an autonomous action decision.");
            OwnerSectionCensusSnapshot directiveBefore = before[ScheduledDirectiveStore.SectionId];
            OwnerSectionCensusSnapshot directiveAfter = after.Single(row =>
                row.SectionId == ScheduledDirectiveStore.SectionId);
            Assert.That(directiveAfter.Revision, Is.EqualTo(directiveBefore.Revision + 1L),
                "P12F records exactly one terminal directive transition.");
            NpcEscapedEvent escapedEvent = composition.EventAuthority.EventStore.Events
                .OfType<NpcEscapedEvent>().Single();
            Assert.That(escapedEvent.ActorRuntimeId, Is.EqualTo(forcedEscapeActor.RuntimeId));
            Assert.That(escapedEvent.LocationRuntimeId,
                Is.EqualTo(graph.Cities.Single(city => string.Equals(
                    city.RuntimeId, forcedEscapeSentence.CityRuntimeId,
                    StringComparison.Ordinal)).LocationRuntimeId));
            Assert.That(escapedEvent.OriginDecisionId,
                Is.EqualTo(forcedEscapeActor.ActionSlot.CurrentPayload.OriginDecisionId));
            Assert.That(escapedEvent.AbsoluteDay, Is.EqualTo(composition.SimulationTime.AbsoluteDay));
            OwnerSectionCensusSnapshot eventsBefore = before[
                RuntimeIdAllocatorCensusProvider.EventsSectionId];
            OwnerSectionCensusSnapshot eventsAfter = after.Single(row =>
                row.SectionId == RuntimeIdAllocatorCensusProvider.EventsSectionId);
            Assert.That(eventsAfter.OwnerInstanceIdentity,
                Is.SameAs(eventsBefore.OwnerInstanceIdentity));
            Assert.That(eventsAfter.Revision, Is.EqualTo(eventsBefore.Revision + 1L),
                "P12 refreshes the exact Event-ID counter witness once for the Escape event.");
            OwnerSectionCensusSnapshot justiceRecordsBefore = before[
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId];
            OwnerSectionCensusSnapshot justiceRecordsAfter = after.Single(row =>
                row.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
            Assert.That(justiceRecordsAfter.OwnerInstanceIdentity,
                Is.SameAs(justiceRecordsBefore.OwnerInstanceIdentity),
                "The Justice record census retains its exact shared identity while its row set changes.");
            Assert.That(justiceRecordsAfter.Cardinality,
                Is.EqualTo(justiceRecordsBefore.Cardinality - 1),
                "Removing the exact sentence changes only the Justice record row cardinality; its warrant remains.");
            Assert.That(justiceRecordsAfter.Revision,
                Is.EqualTo(graph.JusticeRecordsOwner.Revision),
                "P12 publishes the Justice owner's committed revision in its census witness.");
            Assert.That(graph.JusticeRecordsOwner.Revision, Is.GreaterThan(justiceRevisionBeforeTurn),
                "P12 captures the selected Justice sentence day and removal writes.");
        }
        if (executeCoreOnlyGuardArrest)
        {
            Assert.That(guardArrestActor.ActionSlot.CurrentAction.Values.actionType.Value,
                Is.EqualTo(DailyV1NpcDefinitionRules.ArrestActionTypeValue));
            Assert.That(guardArrestActor.ActionSlot.CurrentPayload.TargetNpcRuntimeId,
                Is.EqualTo(guardArrestTarget.RuntimeId));
            DailyV1StatusDefinition arrestedStatus = definitions.Statuses[
                definitions.Configuration.arrestedStatus];
            DailyV1JusticeSentenceOwner sentence = graph.JusticeRecordsOwner.FindActiveSentence(
                guardArrestTarget.RuntimeId);
            Assert.That(sentence, Is.Not.Null);
            Assert.That(sentence.Warrant, Is.SameAs(guardArrestWarrant));
            Assert.That(sentence.CityRuntimeId, Is.EqualTo(guardArrestWarrant.CityRuntimeId));
            Assert.That(guardArrestTarget.Status.Contains(arrestedStatus), Is.True,
                "The Core Guard Arrest action commits the exact target's arrested status.");

            NpcArrestedEvent arrestedEvent = composition.EventAuthority.EventStore.Events
                .OfType<NpcArrestedEvent>().Single();
            Assert.That(arrestedEvent.ActorRuntimeId, Is.EqualTo(guardArrestActor.RuntimeId));
            Assert.That(arrestedEvent.TargetRuntimeId, Is.EqualTo(guardArrestTarget.RuntimeId));
            Assert.That(arrestedEvent.LocationRuntimeId,
                Is.EqualTo(graph.Cities.Single(city => string.Equals(
                    city.RuntimeId, guardArrestWarrant.CityRuntimeId,
                    StringComparison.Ordinal)).LocationRuntimeId),
                "The event retains the canonical LocationId, not a City runtime ID.");
            Assert.That(arrestedEvent.OriginDecisionId,
                Is.EqualTo(guardArrestActor.ActionSlot.CurrentPayload.OriginDecisionId));
            Assert.That(arrestedEvent.AbsoluteDay, Is.EqualTo(composition.SimulationTime.AbsoluteDay));
            Assert.That(arrestedEvent.RecordSequence, Is.GreaterThan(0L));

            OwnerSectionCensusSnapshot eventsBefore = before[
                RuntimeIdAllocatorCensusProvider.EventsSectionId];
            OwnerSectionCensusSnapshot eventsAfter = after.Single(row =>
                row.SectionId == RuntimeIdAllocatorCensusProvider.EventsSectionId);
            Assert.That(eventsAfter.OwnerInstanceIdentity,
                Is.SameAs(eventsBefore.OwnerInstanceIdentity));
            Assert.That(eventsAfter.Revision, Is.EqualTo(eventsBefore.Revision + 1L),
                "P12 refreshes the exact Event-ID counter witness once for the arrest event.");

            OwnerSectionCensusSnapshot justiceRecordsBefore = before[
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId];
            OwnerSectionCensusSnapshot justiceRecordsAfter = after.Single(row =>
                row.SectionId == DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId);
            Assert.That(justiceRecordsAfter.OwnerInstanceIdentity,
                Is.SameAs(justiceRecordsBefore.OwnerInstanceIdentity));
            Assert.That(justiceRecordsAfter.Cardinality,
                Is.EqualTo(justiceRecordsBefore.Cardinality + 1));
            Assert.That(justiceRecordsAfter.Revision,
                Is.EqualTo(graph.JusticeRecordsOwner.Revision));
            Assert.That(graph.JusticeRecordsOwner.Revision,
                Is.EqualTo(justiceRevisionBeforeTurn + 1L),
                "The admitted arrest adds exactly one sentence to the shared Justice owner.");

            string targetStatusSectionId =
                DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(
                    guardArrestTarget.RuntimeId);
            OwnerSectionCensusSnapshot targetStatusBefore = before[targetStatusSectionId];
            OwnerSectionCensusSnapshot targetStatusAfter = after.Single(row =>
                row.SectionId == targetStatusSectionId);
            Assert.That(targetStatusAfter.OwnerInstanceIdentity,
                Is.SameAs(targetStatusBefore.OwnerInstanceIdentity));
            Assert.That(targetStatusAfter.Revision,
                Is.GreaterThan(targetStatusBefore.Revision),
                "The selected P12 target-status owner records the admitted arrest writes.");
        }
        if (executeCoreOnlySteal)
        {
            TheftOutcome[] thefts = stealSocial.TheftOutcomes.Outcomes
                .Skip(stealOutcomesBefore).ToArray();
            Assert.That(thefts, Has.Length.EqualTo(1),
                "The full Core daily actor turn autonomously commits exactly one Steal occurrence.");
            TheftOutcome theft = thefts[0];
            Assert.That(theft.PerpetratorPersonId, Is.EqualTo(stealActorPersonId));
            Assert.That(theft.VictimPersonId, Is.Not.EqualTo(stealActorPersonId));
            Assert.That(graph.PersonStore.TryGet(theft.VictimPersonId,
                out PersonRuntime exactVictimPerson), Is.True);
            Assert.That(exactVictimPerson.IsMaterialized, Is.True);
            Assert.That(graph.TryGetNpcOwner(exactVictimPerson.MaterializedNpcRuntimeId,
                out stealVictim), Is.True);
            Assert.That(stealVictim, Is.Not.Null);
            stealVictimPersonId = exactVictimPerson.PersonId;
            Assert.That(stealVictim.Travel.CurrentCityRuntimeId,
                Is.EqualTo(stealActor.Travel.CurrentCityRuntimeId));
            stealVictimBalanceBefore = stealBalancesBefore[stealVictim.RuntimeId];
            int expectedTheftAmount = UnityMathfCompatibility.Min(
                UnityMathfCompatibility.Max(0, stealAction.Values.crimeSettings.amount),
                UnityMathfCompatibility.FloorToInt(stealVictimBalanceBefore));
            Assert.That(theft.LossAmount, Is.EqualTo(expectedTheftAmount));
            Assert.That(theft.OccurredAbsoluteDay,
                Is.EqualTo(composition.SimulationTime.AbsoluteDay));
            Assert.That(theft.OccurrenceKey, Is.EqualTo(
                "autonomous|day|"
                + composition.SimulationTime.AbsoluteDay.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + "|perp|" + stealActorPersonId.Value
                + "|victim|" + theft.VictimPersonId.Value
                + "|amount|" + theft.LossAmount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + "|action|" + stealAction.DefinitionId));

            int actorNetTransfer = 0;
            int victimNetTransfer = 0;
            foreach (TheftOutcome outcome in thefts)
            {
                if (outcome.PerpetratorPersonId.Equals(stealActorPersonId))
                    actorNetTransfer += outcome.LossAmount;
                if (outcome.VictimPersonId.Equals(stealActorPersonId))
                    actorNetTransfer -= outcome.LossAmount;
                if (outcome.PerpetratorPersonId.Equals(stealVictimPersonId))
                    victimNetTransfer += outcome.LossAmount;
                if (outcome.VictimPersonId.Equals(stealVictimPersonId))
                    victimNetTransfer -= outcome.LossAmount;
            }
            Assert.That(stealActor.MoneyAccount.Balance,
                Is.EqualTo(stealActorBalanceBefore + actorNetTransfer));
            Assert.That(stealVictim.MoneyAccount.Balance,
                Is.EqualTo(stealVictimBalanceBefore + victimNetTransfer));

            foreach (DailyV1NpcRuntimeOwners owner in new[] { stealActor, stealVictim })
            {
                string moneySectionId =
                    DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + owner.RuntimeId;
                OwnerSectionCensusSnapshot moneyBefore = before[moneySectionId];
                OwnerSectionCensusSnapshot moneyAfter = after.Single(row =>
                    row.SectionId == moneySectionId);
                Assert.That(moneyAfter.OwnerInstanceIdentity, Is.SameAs(moneyBefore.OwnerInstanceIdentity));
                Assert.That(moneyAfter.OwnerInstanceIdentity, Is.SameAs(owner.MoneyAccount));
                Assert.That(moneyAfter.Cardinality, Is.EqualTo(1));
                Assert.That(moneyAfter.Revision,
                    Is.GreaterThan(moneyBefore.Revision),
                    "The Core theft operation refreshes the exact changed account census row.");
                Assert.That(moneyAfter.Revision, Is.EqualTo(owner.MoneyAccount.Revision));
            }

            OwnerSectionCensusSnapshot outcomesBefore = before[
                P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId];
            OwnerSectionCensusSnapshot outcomesAfter = after.Single(row =>
                row.SectionId == P12CrimeSocialAppraisalOwnerContract.OutcomesSectionId);
            Assert.That(outcomesAfter.OwnerInstanceIdentity, Is.SameAs(outcomesBefore.OwnerInstanceIdentity));
            Assert.That(outcomesAfter.OwnerInstanceIdentity, Is.SameAs(stealSocial.TheftOutcomes));
            Assert.That(outcomesAfter.Cardinality, Is.EqualTo(stealSocial.TheftOutcomes.Count));
            Assert.That(outcomesAfter.Revision,
                Is.EqualTo(stealSocial.TheftOutcomes.P12CensusRevision));
            Assert.That(stealSocial.TheftOutcomes.Count,
                Is.GreaterThan(stealOutcomesBefore));
            Assert.That(stealSocial.TheftOutcomes.P12CensusRevision,
                Is.GreaterThan(stealOutcomeRevisionBefore));
            foreach ((string sectionId, object ownerIdentity, int cardinality, long revision) in new[]
            {
                (P12CrimeSocialAppraisalOwnerContract.KnowledgeSectionId,
                    (object)stealSocial.CrimeKnowledge,
                    stealSocial.CrimeKnowledge.Count,
                    stealSocial.CrimeKnowledge.P12CensusRevision),
                (P12CrimeSocialAppraisalOwnerContract.ReactionsSectionId,
                    (object)stealSocial.SocialReactions,
                    stealSocial.SocialReactions.Count,
                    stealSocial.SocialReactions.P12CensusRevision)
            })
            {
                OwnerSectionCensusSnapshot baseline = before[sectionId];
                OwnerSectionCensusSnapshot current = after.Single(row => row.SectionId == sectionId);
                Assert.That(current.OwnerInstanceIdentity, Is.SameAs(baseline.OwnerInstanceIdentity));
                Assert.That(current.OwnerInstanceIdentity, Is.SameAs(ownerIdentity));
                Assert.That(current.Cardinality, Is.EqualTo(cardinality));
                Assert.That(current.Revision, Is.EqualTo(revision));
            }
            Assert.That(epoch, Is.GreaterThan(mutationEpochBeforeTurn),
                "The Core Steal transfer, social acceptance, and warrant writes advance the sealed epoch.");
            Assert.That(epoch, Is.EqualTo(state.MutationEpochState.CurrentEpoch));
        }
        Assert.That(epoch, Is.GreaterThan(0L),
            "The complete daily execution publishes its shared mutation epoch.");
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);

        if (executeCoreOnlyLocalSellGoodsChoice)
        {
            Assert.That(localSellChoiceActor.ActionSlot.CurrentAction,
                Is.SameAs(localSellChoiceAction),
                "The successful one-shot action remains in its authoritative owner slot until the next actor turn selects its replacement.");
            Assert.That(DailyV1CoreSelectedActorTurnExecutionBoundary.IsExactCoreLocalSellGoodsActionSlot(
                graph, localSellChoiceActor, localSellChoiceAction), Is.True,
                "The retained action slot still contains the exact admitted Core local SellGoods payload.");
            DailyV1ActorTurnRosterProjection retainedChoiceProjection =
                state.SelectedRuntimeActorRoster.Single(projection => projection != null
                    && string.Equals(projection.RuntimeId, localSellChoiceActor.RuntimeId, StringComparison.Ordinal));
            Assert.That(retainedChoiceProjection.PersonId, Is.EqualTo(localSellChoicePersonId),
                "The exact current P12 actor roster retains the Person-to-runtime identity for the completed choice.");
            DailyV1NpcActionPayloadOwner retainedChoicePayload = localSellChoiceActor.ActionSlot.CurrentPayload;
            Assert.That(state.SelectedActorChoiceStore.TryGet(
                localSellChoiceInput.InputId, out ActorChoiceInput retainedChoiceInput), Is.True);
            Assert.That(retainedChoiceInput.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
            Assert.That(retainedChoiceInput.PersonId, Is.EqualTo(localSellChoicePersonId));
            Assert.That(retainedChoiceInput.ActionDefinitionId,
                Is.EqualTo(localSellChoiceAction.DefinitionId));
            Assert.That(retainedChoiceInput.Origin, Is.EqualTo(WorldCommandOrigin.LocalPlayer));
            Assert.That(retainedChoiceInput.Authority, Is.EqualTo(WorldCommandAuthorityMode.Request));
            Assert.That(retainedChoiceInput.TemporalCapture, Is.Null);
            Assert.That(retainedChoiceInput.Dispositions.Select(disposition => disposition.Kind),
                Is.EqualTo(new[]
                {
                    ActorChoiceDispositionKind.DispatchStarted,
                    ActorChoiceDispositionKind.AttemptReturned
                }));
            Assert.That(retainedChoicePayload.OriginDecisionId,
                Is.EqualTo(retainedChoiceInput.Dispositions[0].DecisionRecordId),
                "The persisted action slot references the exact ActorChoice dispatch decision.");
            Assert.That(state.SelectedDecisionStore.Decisions.Any(decision => decision != null
                    && string.Equals(decision.DecisionId, retainedChoicePayload.OriginDecisionId, StringComparison.Ordinal)
                    && string.Equals(decision.ActorRuntimeId, localSellChoiceActor.RuntimeId, StringComparison.Ordinal)
                    && decision.Origin == NpcDecisionOrigin.ActorChoice
                    && string.Equals(decision.ActionDefinitionId, localSellChoiceAction.DefinitionId, StringComparison.Ordinal)), Is.True,
                "The persisted P11/P12 decision record belongs to the exact actor, action and slot.");
            Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryValidateCoreOnlyStart(
                state, graph, out string retainedChoiceDiagnostic), Is.True, retainedChoiceDiagnostic,
                "A host-neutral Core continuation accepts only the exact completed actor-choice slot and matching P11/P12 decision provenance.");

            int actorChoiceDecisionCount = composition.DecisionStore.Decisions.Count(decision =>
                string.Equals(decision.ActorRuntimeId, localSellChoiceActor.RuntimeId, StringComparison.Ordinal)
                && decision.Origin == NpcDecisionOrigin.ActorChoice);
            long actionSlotRevisionBeforeContinuation = localSellChoiceActor.ActionSlot.Revision;
            Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
            Assert.That(state.TryBeginAdvance(invalidateCaptureToken: false,
                out DailyV1AdvanceLease continuationLease), Is.True);
            Assert.That(state.TryBeginSelectedRuntimeDailyAdvanceOperation(), Is.True);
            try
            {
                DailyV1SelectedDailyExecutionStage.Advance(state, graph, dailyBoundary);
            }
            finally
            {
                continuationLease.Dispose();
                Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
            }

            Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False,
                "The next Core DailyAdvance validates the retained one-shot slot, then completes without reopening the actor-choice dispatch.");
            Assert.That(localSellChoiceActor.ActionSlot.CurrentAction, Is.Null,
                "With no pending choice and no autonomous SellGoods selection, the next actor turn clears the completed one-shot slot.");
            Assert.That(localSellChoiceActor.ActionSlot.Revision,
                Is.EqualTo(actionSlotRevisionBeforeContinuation + 1L));
            Assert.That(composition.DecisionStore.Decisions.Count(decision =>
                    string.Equals(decision.ActorRuntimeId, localSellChoiceActor.RuntimeId, StringComparison.Ordinal)
                    && decision.Origin == NpcDecisionOrigin.ActorChoice),
                Is.EqualTo(actorChoiceDecisionCount),
                "The persisted completed choice is not dispatched or recorded a second time on continuation.");
            Assert.That(state.SelectedActorChoiceStore.TryGet(
                localSellChoiceInput.InputId, out ActorChoiceInput continuedChoice), Is.True);
            Assert.That(continuedChoice.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
            Assert.That(continuedChoice.Dispositions.Select(disposition => disposition.Kind),
                Is.EqualTo(new[]
                {
                    ActorChoiceDispositionKind.DispatchStarted,
                    ActorChoiceDispositionKind.AttemptReturned
                }));
            Assert.That(state.TryValidateCurrentSelectedOwnerSectionCensusBaselines(), Is.True,
                "P12 remains complete and current after the continued Core actor turn.");
        }
    }

    [Test]
    public void StateBackedDayStartUsesCoreBoundRosterInsteadOfHostProjection()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        Assert.That(composition.ExecutionBoundaryState.HasAdmittedSelectedDailyExecution, Is.True,
            "This regression exercises a published selected-profile admission contract.");
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = false,
            HasUnselectedLiveRuntimeMember = true,
            CrimeSystemPresent = false,
            JusticeSystemPresent = false,
            DayStartRuntimeRosterOverride = Array.Empty<DailyV1ActorTurnRosterProjection>()
        };

        DailyV1SelectedDailyExecutionStage.Advance(
            composition.ExecutionBoundaryState, composition.OwnerGraph, boundary);

        Assert.That(boundary.LegacyCrimeJusticeDailyMutations, Is.Zero,
            "The admitted Crime/Justice stages must not delegate their P12 reservation to the Unity host boundary.");
        Assert.That(boundary.CoreCrimeJusticeDailyReservations, Is.Zero,
            "Selected execution does not delegate Crime/Justice epoch reservation to the host adapter.");
        Assert.That(boundary.Calls, Does.Not.Contain("begin-day"));
        Assert.That(boundary.Calls, Does.Not.Contain("day-start-context"),
            "The admitted selected stage derives its day-start inputs from the same Core-bound actor context.");
    }

    [Test]
    public void StateBackedDayStartLetsCoreSelectCompatibilityForUnselectedLiveMember()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> priorRoster =
            state.SelectedRuntimeActorRoster;
        Assert.That(state.TryCreateSelectedRuntimeActorRosterWithAppendedMember(
                new DailyV1ActorTurnRosterProjection("runtime/unselected-dynamic", null),
                composition.OwnerGraph,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> rosterWithUnselectedMember), Is.True,
            "The Core roster can retain an unselected runtime identity without assigning it an owner.");
        Assert.That(state.TryCommitSelectedRuntimeActorRoster(priorRoster, rosterWithUnselectedMember), Is.True);

        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            HasUnselectedLiveRuntimeMember = false,
            CrimeSystemPresent = true,
            JusticeSystemPresent = true,
            DayStartRuntimeRosterOverride = Array.Empty<DailyV1ActorTurnRosterProjection>()
        };

        DailyV1SelectedDailyExecutionStage.Advance(
            composition.ExecutionBoundaryState, composition.OwnerGraph, boundary);

        Assert.That(boundary.Calls.Count(call => call == "begin-day"), Is.EqualTo(1),
            "Core selects the compatibility callback from its exact roster even when the host cannot provide a selected day-start context.");
        Assert.That(boundary.DayStartPhases, Is.Empty,
            "The Core-selected day-start scopes do not run alongside the whole-stage fallback.");
        Assert.That(boundary.Calls.IndexOf("travel-context"), Is.GreaterThan(
            boundary.Calls.IndexOf("begin-day")),
            "A mixed Core roster retains the whole-stage compatibility fallback before the later travel boundary is acquired.");
        Assert.That(boundary.Calls, Does.Not.Contain("merchant-plan"));
    }

    [Test]
    public void AdmittedActorBoundaryUsesExactCoreJusticeOwnerDespiteHostSubstitution()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = false,
            SubstituteJusticeRecordsOwner = new DailyV1JusticeRecordsOwner()
        };

        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            composition.ExecutionBoundaryState, composition.OwnerGraph, boundary,
            out IDailyV1SelectedActorTurnExecutionContext context), Is.True);
        Assert.That(context.Boundary, Is.InstanceOf<IDailyV1SelectedCrimeJusticeDailyBoundary>());
        Assert.That(((IDailyV1SelectedCrimeJusticeDailyBoundary)context.Boundary).JusticeRecordsOwner,
            Is.SameAs(composition.OwnerGraph.JusticeRecordsOwner));
        Assert.That(((IDailyV1SelectedCrimeJusticeDailyBoundary)context.Boundary).JusticeRecordsOwner,
            Is.Not.SameAs(boundary.SubstituteJusticeRecordsOwner),
            "The Core boundary binds Justice identity from its exact selected owner graph.");
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "actor-host-ports" }));
    }

    [Test]
    public void AdmittedStageRejectsMismatchedCrimeJusticeProjectionBeforeLoggerOrMutation()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            CrimeJusticeProjectionOwnerBindingsMatch = false
        };
        long justiceRevision = composition.OwnerGraph.JusticeRecordsOwner.Revision;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                composition.ExecutionBoundaryState, composition.OwnerGraph, boundary));

        Assert.That(failure.Message, Does.Contain("Unity projection bindings"));
        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "actor-host-ports",
            "crime-justice-projection-bindings"
        }), "A stale Unity projection must fail before logger, day-start, or gameplay callbacks.");
        Assert.That(composition.OwnerGraph.JusticeRecordsOwner.Revision, Is.EqualTo(justiceRevision),
            "Projection mismatch rejection does not write the authoritative Core Justice owner.");
    }

    [Test]
    public void AdmittedDayStartUsesCoreBoundaryBeforeEnabledDemographyWriter()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithNaturalMortalityEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), true);
        DailyV1GenesisComposition composition = CreateSelectedGenesis(definitions);
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = false,
            SubstituteJusticeRecordsOwner = new DailyV1JusticeRecordsOwner(),
            ThrowOnDemography = true
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                composition.ExecutionBoundaryState, composition.OwnerGraph, boundary));

        Assert.That(failure.Message, Does.Contain("demography failed"),
            "The Core day-start owner binding is already established before enabled demographic preparation.");
        Assert.That(boundary.Calls, Does.Contain("actor-host-ports"));
        Assert.That(boundary.Calls, Does.Contain("crime-justice-projection-bindings"));
        Assert.That(boundary.Calls, Does.Contain("travel-context"));
        Assert.That(boundary.Calls, Does.Contain("demography-host-ports"));
        Assert.That(boundary.Calls, Does.Not.Contain("actor-context"),
            "An admitted day does not request a second host-owned actor/day-start context.");
        Assert.That(boundary.PublishedDemographyResult, Is.Null,
            "No daily result is published after day-start preflight rejection.");
    }

    [Test]
    public void AdmittedTravelBoundaryFailurePrecedesLoggerAndEnabledDemographyWriter()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithNaturalMortalityEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), true);
        DailyV1GenesisComposition composition = CreateSelectedGenesis(definitions);
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = false,
            RejectTravelContext = true,
            ThrowOnDemography = true
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                composition.ExecutionBoundaryState, composition.OwnerGraph, boundary));

        Assert.That(failure.Message, Does.Contain(
            "The selected Daily-v1 host could not provide its exact travel projection and P12 context."));
        Assert.That(boundary.Calls,
            Is.EqualTo(new[]
            {
                "actor-host-ports",
                "crime-justice-projection-bindings",
                "travel-context"
            }),
            "The admitted selected stage proves its exact travel/P12 boundary before logger or enabled demographic writer callbacks.");
        Assert.That(boundary.PublishedDemographyResult, Is.Null,
            "No daily result is published after travel/P12 boundary preflight rejection.");
    }

    [Test]
    public void AdmittedDailyStageDoesNotLetLegacyHostFlagSelectActorPolicy()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = false
        };
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(
            composition.ExecutionBoundaryState, boundary);
        Assert.That(composition.ExecutionBoundaryState.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());

        Assert.That(boundary.Calls, Does.Contain("actor-host-ports"));
        Assert.That(boundary.Calls, Does.Contain("logger:1"));
        Assert.That(boundary.DecisionOwnerLookupCount, Is.Zero,
            "The Core actor-turn boundary uses its bound decision roots, not a legacy host-selected action policy.");
        Assert.That(boundary.LegacyActorChoiceCallbackCount, Is.Zero);
        Assert.That(boundary.LegacyActionCallbackCount, Is.Zero);
        Assert.That(boundary.LegacyDirectiveCallbackCount, Is.Zero);
        Assert.That(composition.ExecutionBoundaryState.CurrentCaptureToken, Is.Not.Null,
            "The same admitted daily operation still reaches its completed capture boundary.");
    }

    [Test]
    public void AdvanceRejectsDayStartBoundaryBoundToAnotherJusticeOwner()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            new DailyV1JusticeRecordsOwner());
        FakeBoundary boundary = new FakeBoundary
        {
            UseSelectedDayStartContext = true,
            JusticeSystemPresent = true,
            SubstituteJusticeRecordsOwner = new DailyV1JusticeRecordsOwner()
        };

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary));
        Assert.That(boundary.DayStartPhases, Is.Empty,
            "Core must reject a substituted Justice owner before admitting any Crime/Justice scope.");
        Assert.That(boundary.Calls, Does.Not.Contain("begin-day"));
    }

    [Test]
    public void AdvanceUsesAdmittedMerchantConfigurationForPlanUrgency()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithMerchantTradeEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), false);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        Assert.That(graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled, Is.False,
            "The graph carries the selected immutable configuration used by the Core day stage.");
        DailyV1NpcRuntimeOwners localMerchant = graph.Npcs.First(owner =>
            DailyV1NpcDefinitionRules.HasJobType(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.MerchantJobTypeValue)
            && DailyV1NpcDefinitionRules.HasMerchantBehavior(owner.Definition, graph.Definitions,
                DailyV1NpcDefinitionRules.LocalMerchantBehaviorValue));
        DailyV1CityRuntimeOwners origin = graph.Cities.Single(city =>
            string.Equals(city.RuntimeId, localMerchant.Travel.CurrentCityRuntimeId,
                StringComparison.Ordinal));
        DailyV1CityRuntimeOwners target = graph.Cities.First(city => !ReferenceEquals(city, origin));
        DailyV1ItemDefinition item = graph.Definitions.Items.Values.First();
        Assert.That(graph.TryInstallMerchantTradePlan(localMerchant, item,
            origin.RuntimeId, target.RuntimeId, 2, 1f, "decision/disabled-plan"), Is.True);
        long planRevision = localMerchant.MerchantTradePlan.Revision;
        FakeBoundary boundary = new FakeBoundary { UseSelectedDayStartContext = true };

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Not.Contain("merchant-plan"));
        Assert.That(boundary.Calls, Does.Not.Contain("actor-merchant:" + localMerchant.RuntimeId),
            "Core actor-turn eligibility follows the same admitted Merchant setting.");
        Assert.That(localMerchant.MerchantTradePlan.HasData, Is.True);
        Assert.That(localMerchant.MerchantTradePlan.Revision, Is.EqualTo(planRevision));
    }

    [Test]
    public void AdvanceDerivesUniformAndCustomCalendarIdentityFromAdmittedDefinitions()
    {
        DailyV1CalendarDefinition[] calendars =
        {
            new DailyV1CalendarDefinition(12, 4, 7, null),
            new DailyV1CalendarDefinition(3, 4, 5, new[] { 10, 11, 12 })
        };

        foreach (DailyV1CalendarDefinition calendar in calendars)
        {
            DailyV1EffectiveDefinitions definitions = CreateFiniteFlowDefinitions(calendar);
            DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);

            DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, new FakeBoundary());

            string expected = calendar.monthLengths != null && calendar.monthLengths.Count > 0
                ? new SimulationCalendar(new CalendarDefinition(
                    new List<int>(calendar.monthLengths), calendar.daysPerWeek)).SemanticVersion
                : new SimulationCalendar(new CalendarDefinition(
                    calendar.monthsPerYear, calendar.weeksPerMonth, calendar.daysPerWeek)).SemanticVersion;
            Assert.That(graph.Cities, Has.Count.EqualTo(1));
            Assert.That(graph.Cities[0].LastMaterialFlow.CalendarVersion, Is.EqualTo(expected));
        }
    }

    [Test]
    public void AdvanceOmitsEconomyWhenTheSelectedConfigurationDisablesIt()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithEconomyEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), false);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary();

        DailyV1SelectedDailyExecutionStage.Advance(0L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Not.Contain("economy-preflight"));
        Assert.That(boundary.Calls.IndexOf("knowledge-preflight"), Is.EqualTo(
            boundary.Calls.IndexOf("begin-day") + 1),
            "The selected local-knowledge stage remains immediately after day start when Economy is disabled.");
        int actorContext = boundary.Calls.IndexOf("actor-context");
        int merchantPass = boundary.Calls.FindIndex(call =>
            call.StartsWith("actor-merchant:", StringComparison.Ordinal));
        int travelContext = boundary.Calls.IndexOf("travel-context");
        int travelResults = boundary.Calls.IndexOf("travel-results");
        int travelObservation = boundary.Calls.IndexOf("travel-observation");
        Assert.That(actorContext, Is.GreaterThanOrEqualTo(0));
        Assert.That(merchantPass, Is.GreaterThan(actorContext));
        Assert.That(travelContext, Is.GreaterThan(merchantPass));
        Assert.That(travelResults, Is.GreaterThan(travelContext));
        Assert.That(travelObservation, Is.GreaterThan(travelResults));
    }

    [Test]
    public void AdvanceStopsAtTheFirstFailingDomainPhase()
    {
        FakeBoundary boundary = new FakeBoundary
        {
            ThrowOnDemography = true
        };
        DailyV1EffectiveDefinitions definitions = CopyWithNaturalMortalityEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), true);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                9L, graph, null, boundary));

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "logger:9", "demography"
        }));
    }

    [Test]
    public void AdvanceStopsAtDayStartFailureAfterDisabledDemography()
    {
        FakeBoundary boundary = new FakeBoundary { ThrowOnBeginDay = true };
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions());

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(
                9L, graph, null, boundary));

        Assert.That(boundary.Calls, Is.EqualTo(new[]
        {
            "logger:9", "begin-day"
        }));
        Assert.That(boundary.PublishedDemographyResult.AbsoluteDay, Is.EqualTo(9L));
    }

    [Test]
    public void AdvanceDoesNotRequestDisabledDemographyWriterFromTheHost()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(definitions.EffectiveConfiguration.NaturalMortality.Enabled, Is.False);
        Assert.That(definitions.EffectiveConfiguration.AggregateDemography.Enabled, Is.False);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary { ThrowOnDemography = true };

        Assert.DoesNotThrow(() => DailyV1SelectedDailyExecutionStage.Advance(
            23L, graph, null, boundary));

        Assert.That(boundary.Calls, Does.Not.Contain("demography"),
            "The exact admitted profile has no demographic writer or resident-floor operation to perform.");
        Assert.That(boundary.Calls[0], Is.EqualTo("logger:23"));
        Assert.That(boundary.Calls, Does.Contain("actor-context"));
        Assert.That(boundary.Calls, Does.Contain("travel-context"));
        Assert.That(boundary.PublishedDemographyResult.AbsoluteDay, Is.EqualTo(23L));
        Assert.That(boundary.PublishedDemographyResult.NamedDeathsApplied, Is.Zero);
        Assert.That(boundary.PublishedDemographyResult.AggregateBirthsApplied, Is.Zero);
    }

    [Test]
    public void AdvanceRequestsDemographyContextWhenNaturalMortalityIsEnabled()
    {
        DailyV1EffectiveDefinitions definitions = CopyWithNaturalMortalityEnabled(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(), true);
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        FakeBoundary boundary = new FakeBoundary();

        DailyV1SelectedDailyExecutionStage.Advance(23L, graph, null, boundary);

        Assert.That(boundary.Calls, Does.Contain("demography"),
            "An enabled policy still uses the established host writer context.");
        Assert.That(boundary.PublishedDemographyResult.AbsoluteDay, Is.EqualTo(23L));
        Assert.That(boundary.PublishedDemographyResult.NamedPersonsEvaluated, Is.Zero,
            "The empty fixture requests the enabled-policy boundary without inventing Persons.");
    }

    [Test]
    public void RuntimeAdvanceStageInvokesSelectedDailyCoreBeforeCompletionAndTokenPublication()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        long absoluteDay = composition.SimulationTime.AbsoluteDay + 1L;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        Assert.That(state.SimulationTime.AbsoluteDay, Is.EqualTo(absoluteDay - 1L));
        Assert.That(state.HasAdmittedSelectedDailyExecution, Is.True);
        ScheduledDirectiveStore directives = CreateDirectiveForFirstNpc(graph, absoluteDay);
        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = true
        };
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(
            state, dailyBoundary);

        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.True);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.None));
        Assert.That(advanceBoundary.Calls, Is.EqualTo(new[]
        {
            "operation", "execution-boundary", "capture-evidence"
        }));
        Assert.That(directives.TryTakeDirective(graph.Npcs[0].RuntimeId, out _), Is.False,
            "The state-backed Core actor-turn stage consumes its exact graph-bound directive.");
        Assert.That(dailyBoundary.Calls, Does.Not.Contain("directive"),
            "Selected directives execute through Core rather than the legacy host callback.");
        Assert.That(dailyBoundary.Calls, Does.Not.Contain("begin-day"),
            "The admitted selected path runs the Core day-start stages instead of delegating to Unity's whole-day callback.");
        Assert.That(dailyBoundary.Calls, Does.Contain("actor-host-ports"));
        Assert.That(dailyBoundary.Calls.Count(call => call == "actor-host-ports"), Is.EqualTo(1),
            "Core prepares one actor-turn effect bundle before the clock advance and reuses its Core policy boundary.");
        Assert.That(dailyBoundary.Calls, Does.Contain("travel-context"));
        Assert.That(dailyBoundary.Calls.Count(call => call == "travel-context"), Is.EqualTo(1),
            "The selected travel/P12 context prepared before the clock advance is reused for execution.");
        Assert.That(dailyBoundary.Calls.Count(call => call == "crime-justice-projection-bindings"), Is.EqualTo(1));
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(state.SimulationTime.AbsoluteDay, Is.EqualTo(absoluteDay));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
        Assert.That(state.CurrentCaptureToken.AbsoluteDay, Is.EqualTo(absoluteDay));
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void AdvanceUsesStateBoundDecisionRootsWhenHostOwnerLookupFails()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = true,
            FailDecisionRecordOwnerResolution = true
        };

        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(state, boundary);
        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());

        Assert.That(boundary.Calls, Does.Contain("actor-host-ports"));
        Assert.That(boundary.DecisionOwnerLookupCount, Is.Zero,
            "The state-bound Core roots are authoritative; the host's failing owner lookup is not consulted.");
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
    }

    [Test]
    public void AdmittedActorTurnPolicyAndDecisionRootsAreConstructedByCore()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        // This focused fixture declares only the section exercised below; production
        // admission separately seals the complete selected-profile census inventory.
        string actionSlotSectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
            + graph.Npcs[0].RuntimeId;
        Assert.That(state.TryRegisterExpectedOwnerSectionContract(
            actionSlotSectionId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion,
            OwnerSectionRole.Required), Is.True);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        long initialDay = state.SimulationTime.AbsoluteDay;
        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            ActorChoiceSelectedActionExecutionOverride = false
        };
        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, graph, dailyBoundary,
            out IDailyV1SelectedActorTurnExecutionContext context), Is.True);
        Assert.That(context.Boundary, Is.TypeOf<DailyV1CoreSelectedActorTurnExecutionBoundary>());
        Assert.That(ReferenceEquals(context.Boundary, dailyBoundary.LastActorTurnHostPorts), Is.False,
            "Core constructs the policy boundary; the Unity test double is only its effects bundle.");
        Assert.That(context.CoreRoots.RandomSource, Is.SameAs(state.SelectedRandomSource));
        Assert.That(((IDailyV1SelectedActorActionExecutionBoundary)context.Boundary).RandomSource,
            Is.SameAs(state.SelectedRandomSource));
        Assert.That(context.CoreRoots.IdAllocator, Is.SameAs(state.SelectedRuntimeIdAllocator));
        Assert.That(context.CoreRoots.RecordSequence, Is.SameAs(state.SelectedRecordSequence));
        Assert.That(context.CoreRoots.DecisionStore, Is.SameAs(state.SelectedDecisionStore));
        Assert.That(dailyBoundary.DecisionOwnerLookupCount, Is.Zero,
            "The host cannot select decision allocator, sequence, or store roots for an admitted actor.");
        Assert.That(((IDailyV1SelectedActorActionExecutionBoundary)context.Boundary)
            .UsesSelectedDailyV1ActionExecution(graph.Npcs[0]), Is.True,
            "Core derives selected-action eligibility from the exact admitted graph and roster.");
        Assert.That(typeof(IDailyV1SelectedActorTurnHostPorts)
            .GetMethod("CanCommitSelectedActionSlotP12Mutation"), Is.Null,
            "The Unity host port no longer owns selected action-slot P12 admission.");
        Assert.That(typeof(IDailyV1SelectedActorTurnHostPorts)
            .GetMethod("NotifySelectedActionSlotP12MutationCommitted"), Is.Null,
            "The Unity host port no longer publishes the Core action-slot mutation.");
        Assert.That(context.Boundary, Is.Not.InstanceOf<IDailyV1SelectedActorTurnHostPorts>(),
            "The effect bundle cannot be used as the actor-turn policy boundary.");
        Assert.That(typeof(IDailyV1SelectedActorTurnHostPorts).GetMethod("TryProcessActorChoice"), Is.Null);
        Assert.That(typeof(IDailyV1SelectedActorTurnHostPorts).GetMethod("TryGetDecisionRecordOwners"), Is.Null);
        Assert.That(typeof(IDailyV1SelectedActorTurnHostPorts).GetProperty("RandomSource"), Is.Null,
            "Unity effect ports expose neither actor-turn policy nor session decision/random roots.");
        Assert.That(dailyBoundary.Calls, Is.EqualTo(new[] { "actor-host-ports" }));
        DailyV1CoreSelectedActorTurnExecutionBoundary coreBoundary =
            (DailyV1CoreSelectedActorTurnExecutionBoundary)context.Boundary;
        Assert.That(coreBoundary.OwnsSelectedActionSlotP12Mutation, Is.True);
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        try
        {
            dailyBoundary.RejectActionSlotOwnerValidation = true;
            Assert.That(coreBoundary.CanCommitSelectedActionSlotP12Mutation(
                graph, graph.Npcs[0]), Is.False,
                "Core asks the census protocol before admitting the owner-slot commit.");
            dailyBoundary.RejectActionSlotOwnerValidation = false;
            Assert.That(coreBoundary.CanCommitSelectedActionSlotP12Mutation(
                graph, graph.Npcs[0]), Is.True);
            DailyV1ActionDefinition action = graph.Definitions.Actions.Values.First();
            string actionSection = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
                + graph.Npcs[0].RuntimeId;
            Assert.That(graph.TryCommitCurrentActionSlot(
                graph.Npcs[0], action, null,
                () => coreBoundary.CanCommitSelectedActionSlotP12Mutation(graph, graph.Npcs[0])), Is.True);
            Assert.That(coreBoundary.NotifySelectedActionSlotP12MutationCommitted(
                graph, graph.Npcs[0], actionSection, graph.Npcs[0].ActionSlot.Revision), Is.True);
            Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(1L),
                "Core publishes one selected action-slot epoch after the owner revision and host baseline refresh.");
            Assert.That(dailyBoundary.ActionSlotBaselineRefreshCount, Is.EqualTo(1));
            Assert.That(dailyBoundary.ActionSlotOwnerValidationCount, Is.EqualTo(4));
            Assert.That(dailyBoundary.ActionSlotUnchangedValidationCount, Is.EqualTo(2));
            Assert.That(dailyBoundary.LastActionSlotSectionId,
                Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
                    + graph.Npcs[0].RuntimeId));
        }
        finally
        {
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        }
        Assert.That(state.SimulationTime.AbsoluteDay, Is.EqualTo(initialDay));
    }

    [Test]
    public void RuntimeAdvanceRejectsMismatchedCrimeJusticeProjectionBeforeClockOrOwnerMutation()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        long initialDay = state.SimulationTime.AbsoluteDay;
        long initialJusticeRevision = graph.JusticeRecordsOwner.Revision;
        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            CrimeJusticeProjectionOwnerBindingsMatch = false
        };
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(state, dailyBoundary);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            state.TryAdvanceDay(advanceBoundary, out _));

        Assert.That(failure.Message, Does.Contain("Unity projection bindings"));
        Assert.That(dailyBoundary.Calls, Is.EqualTo(new[]
        {
            "actor-host-ports",
            "crime-justice-projection-bindings"
        }));
        Assert.That(state.SimulationTime.AbsoluteDay, Is.EqualTo(initialDay),
            "A stale projection is rejected before the single clock increment.");
        Assert.That(graph.JusticeRecordsOwner.Revision, Is.EqualTo(initialJusticeRevision),
            "No Crime/Justice owner mutation begins before host projection binding is proven.");
        Assert.That(state.CompletedCoreSequence, Is.Zero);
        Assert.That(state.CurrentCaptureToken, Is.Null);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(advanceBoundary.Calls, Does.Contain("fault"));
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("capture-evidence"));
    }

    [Test]
    public void RuntimeAdvanceUsesStateBoundRootsWhenHostOwnerLookupFails()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        FakeBoundary dailyBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            UseSelectedActionExecution = true,
            UseSelectedDayStartContext = true,
            FailDecisionRecordOwnerResolution = true
        };
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(state, dailyBoundary);

        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.True, failure.ToString());

        Assert.That(dailyBoundary.Calls, Does.Contain("actor-host-ports"));
        Assert.That(dailyBoundary.DecisionOwnerLookupCount, Is.Zero,
            "A runtime advance consumes only the roots bound to its execution state.");
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("fault"));
        Assert.That(advanceBoundary.Calls, Does.Contain("capture-evidence"));
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));
        Assert.That(state.CurrentCaptureToken, Is.Not.Null);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void SelectedDailyContextFailureAfterClockAdvanceFaultsAndCannotPublishBoundary()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        Assert.That(state.HasAdmittedSelectedDailyExecution, Is.True);
        FakeRuntimeAdvanceBoundary advanceBoundary = new FakeRuntimeAdvanceBoundary(
            state, new FakeBoundary())
        {
            FailSelectedContext = true
        };

        Assert.That(state.TryAdvanceDay(
            advanceBoundary, out SimulationRuntimeAdvanceFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(advanceBoundary.Calls, Does.Contain("fault"));
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("completed"));
        Assert.That(advanceBoundary.Calls, Does.Not.Contain("capture-evidence"));
        Assert.That(state.CompletedCoreSequence, Is.Zero);
        Assert.That(state.AdvanceLeaseHeld, Is.False);
    }

    [Test]
    public void AdvanceRequiresTheAdmittedDefinitionGraph()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, null, null, new FakeBoundary()));
    }

    [Test]
    public void AdvanceRejectsASubstitutedScheduledDirectiveStore()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions());
        ScheduledDirectiveStore substituted = new ScheduledDirectiveStore(new SimulationTime(4L));
        FakeBoundary boundary = new FakeBoundary();

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, graph, substituted, boundary));
        Assert.That(boundary.Calls, Is.Empty,
            "Owner identity preflight must reject a substitute before any daily callback or write.");
    }

    [Test]
    public void AdvanceRejectsActorContextWithDifferentPersonStoreInstance()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions());
        FakeBoundary boundary = new FakeBoundary { SubstituteActorPersonStore = true };

        Assert.Throws<InvalidOperationException>(() =>
            DailyV1SelectedDailyExecutionStage.Advance(0L, graph, null, boundary));
        Assert.That(boundary.Calls, Does.Contain("actor-context"));
        Assert.That(boundary.Calls, Does.Not.Contain("directive"),
            "The Core actor operation stage must not run with a substituted PersonStore.");
    }

    [Test]
    public void CoreSelectedBoundaryBuildsContextsFromTheExactAdmittedStateAndGraph()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        FakeBoundary hostPorts = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);

        Assert.That(DailyV1CoreSelectedDailyExecutionBoundary.TryCreate(
            state, graph, hostPorts,
            out IDailyV1SelectedDailyExecutionBoundary selectedBoundary), Is.True);
        Assert.That(selectedBoundary,
            Is.TypeOf<DailyV1CoreSelectedDailyExecutionBoundary>());

        Assert.That(selectedBoundary.TryCreateSelectedActorTurnContext(
            graph, out IDailyV1SelectedActorTurnExecutionContext actorContext), Is.True);
        Assert.That(actorContext.RuntimeRoster, Is.SameAs(state.SelectedRuntimeActorRoster));
        Assert.That(actorContext.PersonStore, Is.SameAs(graph.PersonStore));
        Assert.That(actorContext.ActorChoiceStore, Is.SameAs(state.SelectedActorChoiceStore));
        Assert.That(actorContext.CoreRoots.IdAllocator, Is.SameAs(state.SelectedRuntimeIdAllocator));
        Assert.That(actorContext.CoreRoots.RecordSequence, Is.SameAs(state.SelectedRecordSequence));
        selectedBoundary.EnsureSelectedEconomyCanRun(graph);
        selectedBoundary.EnsureSelectedLocalKnowledgeCanRun(graph);
        Assert.That(hostPorts.Calls, Does.Contain("economy-projection-preflight"));
        Assert.That(hostPorts.Calls, Does.Contain("knowledge-projection-preflight"));
        Assert.That(hostPorts.LastMerchantTradeEnabled,
            Is.EqualTo(graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled));
        Assert.That(hostPorts.Calls, Does.Not.Contain("execution-boundary"),
            "Core creates its selected execution boundary instead of asking the host to manufacture one.");

        DailyV1SelectedRuntimeOwnerGraph substitutedGraph = CreateGraph(composition.Definitions);
        Assert.That(DailyV1CoreSelectedDailyExecutionBoundary.TryCreate(
            state, substitutedGraph, hostPorts,
            out IDailyV1SelectedDailyExecutionBoundary rejected), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(selectedBoundary.TryCreateSelectedActorTurnContext(
            substitutedGraph, out _), Is.False,
            "The Core adapter rejects a substituted owner graph.");
    }

    [Test]
    public void CoreDailyBoundaryExecutesResidentDeathWithoutUnityProjection()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        Assert.That(DailyV1ActiveRuntimeSession.TryCreate(
            composition.ExecutionBoundaryState, composition.Definitions, 0L,
            out DailyV1ActiveRuntimeSession session, out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        DailyV1NpcRuntimeOwners npc = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = graph.Cities.FirstOrDefault(candidate =>
            candidate?.Population?.CurrentPopulation > 0);
        Assert.That(city, Is.Not.Null);

        PersonRuntime person = new PersonRuntime(
            new PersonId("detached-composed-resident-death"), state.SimulationTime.AbsoluteDay);
        Assert.That(graph.PersonStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(graph.PersonStore.TryBindMaterializedNpc(
            person.PersonId, npc.RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, city.RuntimeId), Is.True);

        DailyV1ActionDefinition action = composition.Definitions.Actions["DESCANSAR"];
        Assert.That(DailyV1NpcActionPayloadOwner.TryCreate(
            composition.Definitions, action, null, null, null, 0,
            0f, 1f, NpcTravelReason.None, 0f, "detached-composed-death/action",
            null, null, null, out DailyV1NpcActionPayloadOwner payload), Is.True);
        Assert.That(graph.TryCommitCurrentActionSlot(npc, action, payload, null), Is.True);
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, graph.PersonStore, person, state.SimulationTime.AbsoluteDay,
            out DailyV1SelectedPersonDeathPlan plan, out diagnostic), Is.True, diagnostic);
        Assert.That(plan.MaterializedNpc, Is.SameAs(npc));
        Assert.That(plan.ResidenceCity, Is.SameAs(city));

        DailyV1CoreSelectedPersonDeathBoundaryTests.BindCompleteCoreCensus(state, session, plan);
        FakeBoundary hostPorts = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore)
        {
            OmitDemographyBoundary = true
        };
        Assert.That(DailyV1CoreSelectedDailyExecutionBoundary.TryCreate(
            state, graph, hostPorts,
            out IDailyV1SelectedDailyExecutionBoundary selectedBoundary), Is.True);
        Assert.That(selectedBoundary.TryCreateSelectedDemographyContext(
            graph, out IDailyV1SelectedDemographyExecutionContext context), Is.True);
        Assert.That(context.Boundary,
            Is.TypeOf<DailyV1CoreSelectedPersonDeathBoundary>(),
            "Core creates the selected P12 death authority even when a detached host has no Unity projection.");
        Assert.That(hostPorts.Calls, Does.Contain("demography-host-ports"));

        long priorEpoch = state.MutationEpochState.CurrentEpoch;
        long personRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        long npcLifeRevision = npc.Lifecycle.LifeStateRevision;
        long actionRevision = npc.ActionSlot.Revision;
        long populationRevision = city.Population.Revision;
        int populationBefore = city.Population.CurrentPopulation;
        Assert.That(DailyV1SelectedPersonDeathOperation.TryExecute(
            graph, graph.PersonStore, plan,
            DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
            context.Boundary, out diagnostic), Is.True, diagnostic);

        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(plan.AbsoluteDay));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person),
            Is.EqualTo(personRevision + 2L));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(npcLifeRevision + 1L));
        Assert.That(npc.ActionSlot.CurrentAction, Is.Null);
        Assert.That(npc.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevision + 1L));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore - 1));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision + 1L));
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(priorEpoch + 1L));
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False);
        Assert.That(state.ActiveRuntimeOperationCount, Is.Zero);
        Assert.That(state.IsSelectedRuntimeCensusProtocolFaulted, Is.False);
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long capturedEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(capturedEpoch, Is.EqualTo(priorEpoch + 1L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    [Test]
    public void AdmittedActorTurnContextUsesOnlyCoreBoundRosterAndOwnerRoots()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);

        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, graph, boundary,
            out IDailyV1SelectedActorTurnExecutionContext context), Is.True);

        Assert.That(context.RuntimeRoster, Is.SameAs(state.SelectedRuntimeActorRoster));
        Assert.That(context.PersonStore, Is.SameAs(graph.PersonStore));
        Assert.That(context.ActorChoiceStore, Is.SameAs(state.SelectedActorChoiceStore));
        Assert.That(context.ActorChoiceStore.PersonStore, Is.SameAs(context.PersonStore));
        Assert.That(context.CoreRoots.RandomSource, Is.SameAs(state.SelectedRandomSource));
        Assert.That(context.CoreRoots.IdAllocator, Is.SameAs(state.SelectedRuntimeIdAllocator));
        Assert.That(context.CoreRoots.RecordSequence, Is.SameAs(state.SelectedRecordSequence));
        Assert.That(context.CoreRoots.DecisionStore, Is.SameAs(state.SelectedDecisionStore));
        Assert.That(state.SelectedRuntimeActorTurnCoreRoots, Is.SameAs(context.CoreRoots));
        Assert.That(context.CoreRoots.IsBoundTo(
            state, graph, context.PersonStore, context.ActorChoiceStore), Is.True);

        FakeBoundary nextBoundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);
        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, graph, nextBoundary,
            out IDailyV1SelectedActorTurnExecutionContext nextContext), Is.True);
        Assert.That(nextContext.CoreRoots, Is.SameAs(context.CoreRoots),
            "Subsequent daily contexts must reuse the exact Core session-owned roots.");
        Assert.That(boundary.Calls, Is.EqualTo(new[] { "actor-host-ports" }));
    }

    [Test]
    public void AdmittedDemographyContextUsesCoreCalendarAndExactOwnerGraph()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);

        Assert.That(boundary.TryCreateSelectedDemographyHostPorts(
            graph, out IDailyV1SelectedDemographyHostPorts hostPorts), Is.True);
        Assert.That(DailyV1CoreSelectedDemographyExecutionContext.TryCreate(
            state, graph, hostPorts,
            out IDailyV1SelectedDemographyExecutionContext context), Is.True);
        Assert.That(context.Calendar, Is.SameAs(state.CalendarIdentity),
            "An admitted demographic stage uses the calendar bound to its Core execution state.");
        Assert.That(context.Boundary, Is.SameAs(hostPorts.Boundary));
        Assert.That(context.MortalitySamples, Is.SameAs(hostPorts.MortalitySamples));
        Assert.That(context.AggregateProvider, Is.SameAs(hostPorts.AggregateProvider));

        DailyV1SelectedRuntimeOwnerGraph otherGraph = CreateGraph(composition.Definitions);
        Assert.That(DailyV1CoreSelectedDemographyExecutionContext.TryCreate(
            state, otherGraph, hostPorts,
            out IDailyV1SelectedDemographyExecutionContext rejected), Is.False);
        Assert.That(rejected, Is.Null,
            "The admitted context cannot be rebound to a separately reconstructed owner graph.");

        DailyV1ExecutionBoundaryState unboundState = new DailyV1ExecutionBoundaryState(
            new SimulationTime(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        Assert.That(DailyV1CoreSelectedDemographyExecutionContext.TryCreate(
            unboundState, graph, hostPorts,
            out rejected), Is.False,
            "A matching profile label without admitted execution roots is not a Core session.");
        Assert.That(rejected, Is.Null);
    }

    [Test]
    public void AdmittedTravelAvailabilityComesFromSealedCoreOperationInventory()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            hasSoloTravelOperation: true);
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);
        Assert.That(state.HasRuntimeProtocolOperationInventory, Is.True);
        Assert.That(state.HasRuntimeProtocolOperationContract(
            DailyV1RuntimeProtocolOperationContracts.SoloTravelStart), Is.True);
        Assert.That(boundary.TryCreateSelectedTravelDayHostPorts(
            composition.OwnerGraph, out IDailyV1SelectedTravelDayHostPorts hostPorts), Is.True);
        Assert.That(typeof(IDailyV1SelectedTravelDayHostPorts).GetProperty("AdvanceSoloTravel"), Is.Null,
            "The admitted host-port contract no longer exposes travel-stage availability.");

        Assert.That(DailyV1CoreSelectedTravelDayExecutionContext.TryCreate(
            state, composition.OwnerGraph, hostPorts,
            out IDailyV1SelectedTravelDayExecutionContext context), Is.True);
        Assert.That(context.AdvanceSoloTravel, Is.True,
            "The admitted Core session selects solo travel from its sealed operation inventory.");

        DailyV1GenesisComposition incomplete = CreateSelectedGenesis(
            DailyV1CapturedSelectedContentFixture.CreateDefinitions(),
            hasSoloTravelOperation: false,
            sealRuntimeProtocolInventory: false);
        FakeBoundary incompleteBoundary = new FakeBoundary(
            incomplete.RuntimeIdAllocator, incomplete.RandomSource,
            incomplete.RecordSequence, incomplete.DecisionStore,
            incomplete.ActorChoiceStore);
        Assert.That(incompleteBoundary.TryCreateSelectedTravelDayHostPorts(
            incomplete.OwnerGraph, out IDailyV1SelectedTravelDayHostPorts incompletePorts), Is.True);
        Assert.That(DailyV1CoreSelectedTravelDayExecutionContext.TryCreate(
            incomplete.ExecutionBoundaryState, incomplete.OwnerGraph, incompletePorts,
            out IDailyV1SelectedTravelDayExecutionContext rejected), Is.False,
            "An admitted session without a sealed operation inventory cannot take availability from Unity.");
        Assert.That(rejected, Is.Null);
    }

    [Test]
    public void AdmittedTravelContextIsConstructedByCoreAndRejectsAnotherOwnerGraph()
    {
        DailyV1GenesisComposition composition = CreateSelectedGenesis();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        FakeBoundary boundary = new FakeBoundary(
            composition.RuntimeIdAllocator, composition.RandomSource,
            composition.RecordSequence, composition.DecisionStore,
            composition.ActorChoiceStore);
        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            state, graph, boundary, out _), Is.True);
        Assert.That(boundary.TryCreateSelectedTravelDayHostPorts(
            graph, out IDailyV1SelectedTravelDayHostPorts hostPorts), Is.True);

        Assert.That(DailyV1CoreSelectedTravelDayExecutionContext.TryCreate(
            state, graph, hostPorts,
            out IDailyV1SelectedTravelDayExecutionContext context), Is.True);
        Assert.That(context.Boundary, Is.SameAs(hostPorts.Boundary));
        Assert.That(context.P12Protocol, Is.SameAs(hostPorts.P12Protocol));
        Assert.That(context.AdvanceSoloTravel, Is.False,
            "The sealed Core operation inventory omits the optional solo-travel operation in this fixture.");

        DailyV1SelectedRuntimeOwnerGraph otherGraph = CreateGraph(composition.Definitions);
        Assert.That(DailyV1CoreSelectedTravelDayExecutionContext.TryCreate(
            state, otherGraph, hostPorts,
            out IDailyV1SelectedTravelDayExecutionContext rejected), Is.False);
        Assert.That(rejected, Is.Null);
    }

    [Test]
    public void ActorTurnContextFactoryRejectsUnboundStateBeforeHostBoundary()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph(definitions);
        DailyV1ExecutionBoundaryState unboundState = new DailyV1ExecutionBoundaryState(
            new SimulationTime(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        FakeBoundary boundary = new FakeBoundary();

        Assert.That(DailyV1SelectedActorTurnExecutionContextFactory.TryCreate(
            unboundState, graph, boundary,
            out IDailyV1SelectedActorTurnExecutionContext context), Is.False);

        Assert.That(context, Is.Null);
        Assert.That(boundary.Calls, Is.Empty,
            "Core must reject incomplete bound roots without asking the host to create a projection boundary.");
    }

    private static ScheduledDirectiveStore CreateDirectiveForFirstNpc(
        DailyV1SelectedRuntimeOwnerGraph graph,
        long absoluteDay)
    {
        Assert.That(graph.Npcs, Is.Not.Empty);
        ScheduledDirectiveStore directives = graph.ScheduledDirectives
            ?? new ScheduledDirectiveStore(new SimulationTime(absoluteDay));
        Assert.That(directives.Add(new ScheduledDirective(
            "directive/daily-stage", absoluteDay,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            graph.Npcs[0].RuntimeId,
            "action/escape",
            DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)), Is.True);
        Assert.That(graph.TryBindScheduledDirectives(directives), Is.True);
        return directives;
    }

    private static DailyV1GenesisComposition CreateSelectedGenesis() =>
        CreateSelectedGenesis(DailyV1CapturedSelectedContentFixture.CreateDefinitions());

    private static DailyV1GenesisComposition CreateSelectedGenesis(
        DailyV1EffectiveDefinitions definitions) =>
        CreateSelectedGenesis(definitions, hasSoloTravelOperation: false);

    private static DailyV1GenesisComposition CreateSelectedGenesis(
        DailyV1EffectiveDefinitions definitions,
        bool hasSoloTravelOperation,
        bool sealRuntimeProtocolInventory = true)
    {
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition, out string diagnostic), Is.True, diagnostic);
        Assert.That(composition.ExecutionBoundaryState.TryBindOrValidateSelectedRuntimeActorRoster(
            composition.OwnerGraph.Npcs.Select(owner =>
                new DailyV1ActorTurnRosterProjection(owner.RuntimeId, null))), Is.True);
        if (sealRuntimeProtocolInventory)
        {
            IReadOnlyList<string> operationContractIds =
                DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                    hasRuntimeAdmission: true,
                    hasTravelPartySystem: true,
                    hasTravelSystem: hasSoloTravelOperation);
            foreach (string operationContractId in operationContractIds)
                Assert.That(composition.ExecutionBoundaryState.TryRegisterRuntimeProtocolOperationContract(
                    operationContractId), Is.True, operationContractId);
            Assert.That(composition.ExecutionBoundaryState.TrySealRuntimeProtocolOperationInventory(), Is.True);
        }
        return composition;
    }

    private static DailyV1ExecutionBoundaryState CreateCaptureState(
        long absoluteDay,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        Assert.That(graph?.Definitions?.EffectiveConfiguration, Is.Not.Null);
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        DailyV1CalendarDefinition calendarDefinition = graph.Definitions.Calendar;
        CalendarDefinition calendar = calendarDefinition.monthLengths != null
            && calendarDefinition.monthLengths.Count > 0
            ? new CalendarDefinition(
                new List<int>(calendarDefinition.monthLengths), calendarDefinition.daysPerWeek)
            : new CalendarDefinition(
                calendarDefinition.monthsPerYear,
                calendarDefinition.weeksPerMonth,
                calendarDefinition.daysPerWeek);
        SimulationCalendar runtimeCalendar = new SimulationCalendar(calendar);
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            new SimulationTime(absoluteDay), admission, SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        Assert.That(state.TryBindRuntimeConfiguration(
            graph.Definitions.EffectiveConfiguration, runtimeCalendar), Is.True);
        Assert.That(state.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);
        Assert.That(state.TryBindSelectedRuntimeActorRoster(graph.Npcs.Select(owner =>
            new DailyV1ActorTurnRosterProjection(owner.RuntimeId, null))), Is.True);
        return state;
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions)
    {
        return CreateGraph(definitions, null);
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph(
        DailyV1EffectiveDefinitions definitions,
        DailyV1JusticeRecordsOwner justiceRecordsOwner)
    {
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(justiceRecordsOwner, out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.TryBindPersonStore(new PersonStore()), Is.True);
        return graph;
    }

    private static DailyV1EffectiveDefinitions CopyWithGuaranteedSuccessfulAction(
        DailyV1EffectiveDefinitions definitions,
        int actionTypeValue)
    {
        List<DailyV1ActionDefinition> actions = new List<DailyV1ActionDefinition>();
        bool replacedAction = false;
        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
        {
            DailyV1NpcActionData values = action.Values;
            if (values.actionType?.Value == actionTypeValue)
            {
                values = new DailyV1NpcActionData(
                    values.id,
                    values.statusNecessariosParaFazerAcao,
                    values.actionName,
                    values.normalActionLogText,
                    values.actionCategory,
                    values.actionType,
                    values.baseUtility,
                    values.statusModifiers,
                    canFail: false,
                    baseSuccessChance: 1f,
                    values.statusToAdd,
                    values.statusToRemove,
                    values.targetStatusToAdd,
                    values.targetStatusToRemove,
                    values.crimeSettings);
                actions.Add(new DailyV1ActionDefinition(action.TypeToken, action.DefinitionId, values));
                replacedAction = true;
            }
            else
            {
                actions.Add(action);
            }
        }
        Assert.That(replacedAction, Is.True,
            $"The admitted proving graph contains action type '{actionTypeValue}'.");
        return new DailyV1EffectiveDefinitions(
            definitions.Configuration,
            definitions.EffectiveConfiguration,
            definitions.Calendar,
            null,
            definitions.Cities.Values,
            definitions.Items.Values,
            definitions.Npcs.Values,
            definitions.Jobs.Values,
            actions,
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

    private static DailyV1EffectiveDefinitions CopyWithCityMarketItemRemoved(
        DailyV1EffectiveDefinitions definitions,
        string cityDefinitionId,
        string itemDefinitionId)
    {
        List<DailyV1CityDefinition> cities = new List<DailyV1CityDefinition>();
        bool removed = false;
        foreach (DailyV1CityDefinition city in definitions.Cities.Values)
        {
            if (!string.Equals(city.DefinitionId, cityDefinitionId, StringComparison.Ordinal))
            {
                cities.Add(city);
                continue;
            }

            DailyV1CityData values = city.Values;
            DailyV1MarketItemConfig[] marketItems = values.marketItems
                .Where(item => item == null
                    || !string.Equals(item.item, itemDefinitionId, StringComparison.Ordinal))
                .ToArray();
            removed = marketItems.Length != values.marketItems.Count;
            Assert.That(removed, Is.True,
                $"The selected proving City '{cityDefinitionId}' authors the Market row '{itemDefinitionId}' to omit.");
            cities.Add(new DailyV1CityDefinition(city.TypeToken, city.DefinitionId,
                new DailyV1CityData(
                    values.id, values.cityName, values.settlementSemanticId,
                    values.materialFlowLocationId, values.marketStoreSemanticId,
                    values.materialFlowProfile, values.initialPopulation,
                    values.marketLiquidity, values.populationConsumption,
                    marketItems, values.productionConfigs, values.connections)));
        }

        Assert.That(removed, Is.True,
            $"The selected proving City '{cityDefinitionId}' exists in the effective graph.");
        return new DailyV1EffectiveDefinitions(
            definitions.Configuration,
            definitions.EffectiveConfiguration,
            definitions.Calendar,
            null,
            cities,
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

    private static DailyV1EffectiveDefinitions CopyWithConfiguredActions(
        DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<string> actionIds)
    {
        DailyV1SimulationConfigData current = definitions.Configuration;
        DailyV1SimulationConfigData configuration = new DailyV1SimulationConfigData(
            current.simulationName, current.calendar, current.enabledModules, current.cities,
            current.explorableSites, current.npcs, actionIds, current.statuses, current.jobs,
            current.initialWarrants, current.scheduledDirectives,
            current.useAuthoredGeographyProfile, current.authoredHexId,
            current.authoredHexQ, current.authoredHexR, current.authoredTerrainDefinitionId,
            current.authoredTerrainRevisionToken, current.authoredLocationId,
            current.authoredScaleConventionId, current.authoredScaleSourceIdentity,
            current.authoredScaleSourceVersion, current.authoredDistancePerNeighborStep,
            current.authoredScaleUnit, current.authoredP10RuinSite,
            current.genesisProfileContractIdentity, current.p10bStableSiteKey,
            current.travelCostPerDay, current.allowMerchantTradeRepositioning,
            current.maxMerchantTradeAmount, current.localMerchantWholesalePriceMultiplier,
            current.localMerchantReserveRatio, current.maxUnprofitablePlanWaitDays,
            current.crimeAutonomousEnabled, current.commercialKnowledge,
            current.useFixedSimulationSeed, current.simulationSeed,
            current.includeEconomySnapshots, current.economySnapshotIntervalDays,
            current.logSettings, current.naturalMortalityEnabled,
            current.naturalMortalityAnnualProbability, current.aggregateDemographyEnabled,
            current.aggregateAnnualBirthRate, current.aggregateAnnualDeathRate,
            current.freeStatus, current.wantedStatus, current.arrestedStatus,
            current.hiddenStatus);
        return new DailyV1EffectiveDefinitions(
            configuration,
            definitions.EffectiveConfiguration,
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

    private static DailyV1EffectiveDefinitions CopyWithEconomyEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            new EffectiveEconomyConfiguration(enabled),
            current.Travel,
            current.Crime,
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            current.MerchantTrade,
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

    internal static DailyV1EffectiveDefinitions CopyWithNaturalMortalityEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled,
        double annualProbability = 0d)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            current.Economy,
            current.Travel,
            current.Crime,
            current.GuardCrime,
            new EffectiveNaturalMortalityConfiguration(enabled, annualProbability),
            current.AggregateDemography,
            current.MerchantTrade,
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

    private static DailyV1EffectiveDefinitions CopyWithMerchantTradeEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveMerchantTradeConfiguration currentMerchant = current.MerchantTrade;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            current.Economy,
            current.Travel,
            current.Crime,
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            new EffectiveMerchantTradeConfiguration(
                enabled,
                currentMerchant.AllowAutonomousTradeRepositioning,
                currentMerchant.MaxTradeAmount,
                currentMerchant.LocalWholesalePriceMultiplier,
                currentMerchant.LocalReserveRatio,
                currentMerchant.MaxUnprofitablePlanWaitDays),
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

    private static DailyV1EffectiveDefinitions CopyWithCrimeAutonomousEnabled(
        DailyV1EffectiveDefinitions definitions,
        bool enabled)
    {
        EffectiveSimulationConfiguration current = definitions.EffectiveConfiguration;
        EffectiveSimulationConfiguration configuration = new EffectiveSimulationConfiguration(
            current.Population,
            current.Economy,
            current.Travel,
            new EffectiveCrimeConfiguration(current.Crime.Enabled, enabled),
            current.GuardCrime,
            current.NaturalMortality,
            current.AggregateDemography,
            current.MerchantTrade,
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

    private static DailyV1EffectiveDefinitions CreateFiniteFlowDefinitions(
        DailyV1CalendarDefinition calendar)
    {
        DailyV1CityData city = new DailyV1CityData(
            "city/finite", "Finite City", "settlement/finite", "location/finite", "market/finite",
            new DailyV1EnumValue((int)LocalMaterialFlowProfile.FiniteReserveDaily, "FiniteReserveDaily"),
            1000, null,
            new DailyV1PopulationConsumptionConfig(
                new DailyV1EnumValue((int)ConsumptionPaymentMode.Free, "Free"), 0f),
            new[] { new DailyV1MarketItemConfig("item/finite", 5, 100, 0f) },
            new[] { new DailyV1CityProductionConfig("item/finite", 3, 7,
                "source/finite", "content-v1",
                new DailyV1EnumValue((int)CityProductionSourceKind.FiniteReserveDaily, "FiniteReserveDaily")) },
            Array.Empty<DailyV1CityConnection>());
        DailyV1EffectiveDefinitions definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(
            DailyV1TypedDefinitionGraphTests.CreateConfiguration(1f, new[] { "city/finite" }),
            SimulationConfigurationDefaults.Create(),
            calendar,
            new[] { city },
            new[] { new DailyV1ItemData("item/finite", "Iron", 4f, null) },
            Array.Empty<DailyV1NpcData>(),
            Array.Empty<DailyV1NpcJobData>(),
            Array.Empty<DailyV1NpcActionData>(),
            Array.Empty<DailyV1NpcStatusData>(),
            Array.Empty<string>(),
            "test.daily-v1", "test.geography-v1", "test.geography-stage/v1", "hex-v1", "q-r-s");
        return CopyWithEconomyEnabled(definitions, true);
    }

    internal sealed class FakeBoundary : IDailyV1SelectedDailyExecutionBoundary,
        IDailyV1SelectedDailyExecutionHostPorts
    {
        private readonly RuntimeIdAllocator runtimeIdAllocator;
        private readonly IAuthoritativeRandomSource randomSource;
        private readonly SimulationRecordSequence recordSequence;
        private readonly NpcDecisionStore decisionStore;
        private readonly ActorChoiceStore actorChoiceStore;

        internal FakeBoundary() { }

        internal FakeBoundary(
            RuntimeIdAllocator runtimeIdAllocator,
            IAuthoritativeRandomSource randomSource,
            SimulationRecordSequence recordSequence,
            NpcDecisionStore decisionStore,
            ActorChoiceStore actorChoiceStore)
        {
            this.runtimeIdAllocator = runtimeIdAllocator;
            this.randomSource = randomSource;
            this.recordSequence = recordSequence;
            this.decisionStore = decisionStore;
            this.actorChoiceStore = actorChoiceStore;
        }

        internal List<string> Calls { get; } = new List<string>();
        internal List<DailyV1SelectedCrimeJusticeDailyPhase> DayStartPhases { get; } =
            new List<DailyV1SelectedCrimeJusticeDailyPhase>();
        internal int CoreCrimeJusticeDailyReservations { get; set; }
        internal int LegacyCrimeJusticeDailyMutations { get; set; }
        internal DailyV1SelectedDemographyResult PublishedDemographyResult { get; private set; }
        internal bool ThrowOnDemography { get; set; }
        internal bool OmitDemographyBoundary { get; set; }
        internal IPersonNaturalMortalitySampleProvider MortalitySampleProvider { get; set; }
        internal bool ThrowOnBeginDay { get; set; }
        internal bool SubstituteActorPersonStore { get; set; }
        internal bool UseSelectedDayStartContext { get; set; }
        internal bool RejectActorTurnHostPorts { get; set; }
        internal DailyV1NpcRuntimeOwners ExpectedCoreTravelDayActor { get; set; }
        internal DailyV1ExecutionBoundaryState ExecutionState { get; set; }
        internal bool HasUnselectedLiveRuntimeMember { get; set; }
        internal bool CrimeSystemPresent { get; set; }
        internal bool JusticeSystemPresent { get; set; }
        internal IReadOnlyList<DailyV1ActorTurnRosterProjection> DayStartRuntimeRosterOverride { get; set; }
        internal bool RejectTravelContext { get; set; }
        internal bool CrimeJusticeProjectionOwnerBindingsMatch { get; set; } = true;
        internal bool CrimeJusticeAdmissionEnabled { get; set; } = true;
        internal bool UseSelectedActionExecution { get; set; }
        internal bool RequiresActionProjection { get; set; } = true;
        internal bool OmitActionSlotP12MutationProtocol { get; set; }
        internal bool? ActorChoiceSelectedActionExecutionOverride { get; set; }
        internal bool FailDecisionRecordOwnerResolution { get; set; }
        internal IDailyV1SelectedActorTurnHostPorts LastActorTurnHostPorts { get; private set; }
        internal bool RejectActionSlotOwnerValidation { get; set; }
        internal int ActionSlotOwnerValidationCount { get; set; }
        internal int ActionSlotUnchangedValidationCount { get; set; }
        internal int ActionSlotBaselineRefreshCount { get; set; }
        internal string LastActionSlotSectionId { get; set; }
        internal int DecisionOwnerLookupCount { get; private set; }
        internal int SelectedActionDispatchLookupCount { get; private set; }
        internal int LegacyActorChoiceCallbackCount { get; set; }
        internal int LegacyActionCallbackCount { get; set; }
        internal int LegacyDirectiveCallbackCount { get; set; }
        internal DailyV1JusticeRecordsOwner SubstituteJusticeRecordsOwner { get; set; }
        public void BeginLoggerDay(long absoluteDay) => Calls.Add("logger:" + absoluteDay);
        public bool HasExactCrimeJusticeProjectionOwnerBindings(
            DailyV1SelectedRuntimeOwnerGraph graph)
        {
            Assert.That(graph, Is.Not.Null);
            Calls.Add("crime-justice-projection-bindings");
            return CrimeJusticeProjectionOwnerBindingsMatch;
        }
        public bool TryCreateSelectedDemographyContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDemographyExecutionContext context)
        {
            Calls.Add("demography");
            if (ThrowOnDemography) throw new InvalidOperationException("demography failed");
            context = new FakeDemographyContext(
                graph, OmitDemographyBoundary, MortalitySampleProvider);
            return true;
        }
        public bool TryCreateSelectedDemographyHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDemographyHostPorts hostPorts)
        {
            Calls.Add("demography-host-ports");
            if (ThrowOnDemography)
            {
                hostPorts = null;
                throw new InvalidOperationException("demography failed");
            }
            hostPorts = new FakeDemographyContext(
                graph, OmitDemographyBoundary, MortalitySampleProvider);
            return true;
        }
        public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result)
        {
            PublishedDemographyResult = result ?? throw new ArgumentNullException(nameof(result));
        }
        public bool TryCreateSelectedDayStartContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDayStartExecutionContext context)
        {
            if (!UseSelectedDayStartContext)
            {
                context = null;
                return false;
            }
            Calls.Add("day-start-context");
            context = new FakeDayStartContext(this, graph, DayStartRuntimeRosterOverride);
            return true;
        }
        public void BeginSimulationDay()
        {
            Calls.Add("begin-day");
            if (ThrowOnBeginDay) throw new InvalidOperationException("day start failed");
        }
        public void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
        {
            Assert.That(graph, Is.Not.Null);
            Calls.Add("economy-preflight");
        }
        public void EnsureSelectedEconomyProjectionCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
        {
            Assert.That(graph, Is.Not.Null);
            Calls.Add("economy-projection-preflight");
        }
        public void LogSelectedDailyEconomy(
            DailyV1CityRuntimeOwners owner,
            SimulationLogCategory category,
            string message)
        {
            Assert.That(owner, Is.Not.Null);
        }
        public void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
        {
            Assert.That(graph, Is.Not.Null);
            Calls.Add("knowledge-preflight");
        }
        internal bool? LastMerchantTradeEnabled { get; private set; }
        public void EnsureSelectedLocalKnowledgeProjectionCanRun(
            DailyV1SelectedRuntimeOwnerGraph graph,
            bool merchantTradeEnabled)
        {
            Assert.That(graph, Is.Not.Null);
            LastMerchantTradeEnabled = merchantTradeEnabled;
            Calls.Add("knowledge-projection-preflight");
        }
        public bool TryCreateSelectedActorTurnContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedActorTurnExecutionContext context)
        {
            CurrentGraph = graph;
            Calls.Add("actor-context");
            List<DailyV1ActorTurnRosterProjection> runtimeRoster = graph.Npcs
                .Select(npc => new DailyV1ActorTurnRosterProjection(
                    npc.RuntimeId, null))
                .ToList();
            PersonStore people = SubstituteActorPersonStore
                ? new PersonStore()
                : graph.PersonStore;
            ActorChoiceStore choices = !SubstituteActorPersonStore && actorChoiceStore != null
                ? actorChoiceStore
                : new ActorChoiceStore(people);
            context = new FakeActorTurnContext(
                runtimeRoster, people, choices, new FakeActorTurnBoundary(this));
            return true;
        }
        public bool TryCreateSelectedActorTurnHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedActorTurnHostPorts hostPorts)
        {
            Assert.That(graph, Is.Not.Null);
            CurrentGraph = graph;
            Calls.Add("actor-host-ports");
            if (RejectActorTurnHostPorts)
            {
                LastActorTurnHostPorts = null;
                hostPorts = null;
                return false;
            }
            LastActorTurnHostPorts = new FakeActorTurnBoundary(this);
            hostPorts = LastActorTurnHostPorts;
            return true;
        }
        internal DailyV1SelectedRuntimeOwnerGraph CurrentGraph { get; private set; }
        public bool TryCreateSelectedTravelDayContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedTravelDayExecutionContext context)
        {
            Assert.That(graph, Is.SameAs(CurrentGraph));
            Calls.Add("travel-context");
            if (RejectTravelContext)
            {
                context = null;
                return false;
            }
            context = new FakeTravelContext(this);
            return true;
        }

        public bool TryCreateSelectedTravelDayHostPorts(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedTravelDayHostPorts hostPorts)
        {
            hostPorts = null;
            CurrentGraph = graph;
            Calls.Add("travel-context");
            if (RejectTravelContext) return false;
            hostPorts = new FakeTravelHostPorts(this);
            return true;
        }

        private sealed class FakeDemographyContext : IDailyV1SelectedDemographyExecutionContext,
            IDailyV1SelectedDemographyHostPorts
        {
            internal FakeDemographyContext(
                DailyV1SelectedRuntimeOwnerGraph graph,
                bool omitBoundary = false,
                IPersonNaturalMortalitySampleProvider mortalitySamples = null)
            {
                DailyV1CalendarDefinition definition = graph.Definitions.Calendar;
                Calendar = definition.monthLengths != null && definition.monthLengths.Count > 0
                    ? new SimulationCalendar(new CalendarDefinition(
                        new List<int>(definition.monthLengths), definition.daysPerWeek))
                    : new SimulationCalendar(new CalendarDefinition(
                        definition.monthsPerYear, definition.weeksPerMonth, definition.daysPerWeek));
                Boundary = omitBoundary ? null : new FakeDemographyBoundary(graph);
                MortalitySamples = mortalitySamples;
            }

            public SimulationCalendar Calendar { get; }
            public IPersonNaturalMortalitySampleProvider MortalitySamples { get; }
            public IAggregateDemographyProvider AggregateProvider => null;
            public IDailyV1SelectedDemographyBoundary Boundary { get; }
        }

        private sealed class FakeDemographyBoundary : IDailyV1SelectedDemographyBoundary
        {
            private readonly IReadOnlyDictionary<string, int> representedFloors;

            internal FakeDemographyBoundary(DailyV1SelectedRuntimeOwnerGraph graph)
            {
                Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                    floors[city.RuntimeId] = city.Population.CurrentPopulation;
                representedFloors = floors;
            }

            public bool TryBeginPersonDeathProjectionMutation(
                DailyV1SelectedPersonDeathPlan plan,
                string operationId,
                bool currentActionChanged,
                IReadOnlyList<string> sectionIds) => false;

            public bool TryCompletePersonDeathProjectionMutation(
                DailyV1SelectedPersonDeathPlan plan,
                string operationId,
                bool currentActionChanged,
                IReadOnlyList<string> sectionIds,
                bool operationSucceeded) => false;

            public bool TryValidatePersonDeathProjection(
                DailyV1SelectedPersonDeathPlan plan,
                out bool currentActionChanged,
                out string failure)
            {
                currentActionChanged = false;
                failure = "No mortality is expected in this fixture.";
                return false;
            }

            public bool TryBeginPersonDeathOperation(
                DailyV1SelectedPersonDeathPlan plan,
                bool currentActionChanged,
                string operationId,
                IReadOnlyList<string> sectionIds,
                Func<bool> localRevisionCapacity,
                out IDailyV1SelectedPersonDeathP12Scope scope)
            {
                scope = null;
                return false;
            }

            public bool TryPublishPersonDeathProjection(
                DailyV1SelectedPersonDeathPlan plan,
                bool currentActionChanged) => false;

            public void FaultPersonDeathRuntime() { }

            public void AdvancePersonDeathWorldRevision() { }

            public IReadOnlyDictionary<string, int> BuildCompatibilityResidentFloorSupplement(
                IReadOnlyCollection<string> selectedNpcRuntimeIds) => representedFloors;
        }

        private sealed class FakeTravelContext : IDailyV1SelectedTravelDayExecutionContext,
            IDailyV1SelectedTravelDayHostPorts
        {
            private readonly FakeBoundary owner;
            internal FakeTravelContext(FakeBoundary owner) => this.owner = owner;
            public IDailyV1SelectedTravelDayBoundary Boundary =>
                owner.ExpectedCoreTravelDayActor == null
                    ? EmptyTravelBoundary.Instance
                    : new ExpectedCoreTravelDayBoundary(owner);
            public IDailyV1SelectedP12TravelPartyAdvanceProtocol P12Protocol
            {
                get
                {
                    if (owner.ExpectedCoreTravelDayActor == null)
                        return NoOpTravelPartyP12AdvanceProtocol.Instance;
                    bool created = DailyV1CoreSelectedP12TravelPartyAdvanceProtocol.TryCreate(
                        owner.ExecutionState, owner.CurrentGraph,
                        out DailyV1CoreSelectedP12TravelPartyAdvanceProtocol protocol);
                    Assert.That(created, Is.True,
                        "The full DailyAdvance Travel fixture binds the production Core P12 protocol.");
                    return protocol;
                }
            }
            public bool AdvanceSoloTravel => false;
            public bool ExecuteWithinPartyOwnerOperation(Func<bool> operation) => operation?.Invoke() == true;
            public void PublishCompletedTravelResults(
                IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
                IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults)
            {
                Assert.That(partyArrivals, Is.Empty);
                if (owner.ExpectedCoreTravelDayActor == null)
                {
                    Assert.That(soloResults, Is.Empty);
                }
                else
                {
                    Assert.That(soloResults, Has.Count.EqualTo(1));
                    DailyV1SelectedSoloTravelDayResult result = soloResults[0];
                    Assert.That(result.Npc, Is.SameAs(owner.ExpectedCoreTravelDayActor));
                    Assert.That(result.WasCommitted, Is.True);
                    Assert.That(result.ShouldReport, Is.False);
                    Assert.That(result.Arrived, Is.False);
                    Assert.That(result.Npc.Travel.TravelStartedToday, Is.False);
                    Assert.That(result.Npc.Travel.IsTraveling, Is.True);
                }
                owner.Calls.Add("travel-results");
            }
            public void ObserveCompletedTravelResults() => owner.Calls.Add("travel-observation");
        }

        private sealed class FakeTravelHostPorts : IDailyV1SelectedTravelDayHostPorts
        {
            private readonly FakeTravelContext context;

            internal FakeTravelHostPorts(FakeBoundary owner) => context = new FakeTravelContext(owner);

            public IDailyV1SelectedTravelDayBoundary Boundary => context.Boundary;
            public IDailyV1SelectedP12TravelPartyAdvanceProtocol P12Protocol => context.P12Protocol;
            public bool ExecuteWithinPartyOwnerOperation(Func<bool> operation) =>
                context.ExecuteWithinPartyOwnerOperation(operation);
            public void PublishCompletedTravelResults(
                IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> partyArrivals,
                IReadOnlyList<DailyV1SelectedSoloTravelDayResult> soloResults) =>
                context.PublishCompletedTravelResults(partyArrivals, soloResults);
            public void ObserveCompletedTravelResults() => context.ObserveCompletedTravelResults();
        }

        private sealed class ExpectedCoreTravelDayBoundary : IDailyV1SelectedTravelDayBoundary
        {
            private readonly FakeBoundary owner;

            internal ExpectedCoreTravelDayBoundary(FakeBoundary owner) => this.owner = owner;

            public bool TryPrepareTransition(
                DailyV1NpcRuntimeOwners npc,
                DailyV1SelectedTravelTransitionPlan plan,
                bool arriving,
                out object projectionReceipt)
            {
                bool exact = ReferenceEquals(npc, owner.ExpectedCoreTravelDayActor)
                    && plan != null
                    && ReferenceEquals(plan.Npc, npc)
                    && plan.TravelRevisionChanges
                    && !arriving
                    && npc.Travel.TravelStartedToday;
                projectionReceipt = exact ? plan : null;
                return exact;
            }

            public void PublishCommittedTransition(
                DailyV1NpcRuntimeOwners npc,
                DailyV1SelectedTravelTransitionPlan plan,
                bool arriving,
                object projectionReceipt)
            {
                Assert.That(npc, Is.SameAs(owner.ExpectedCoreTravelDayActor));
                Assert.That(plan, Is.SameAs(projectionReceipt));
                Assert.That(arriving, Is.False);
                Assert.That(npc.Travel.TravelStartedToday, Is.False);
                owner.Calls.Add("core-travel-day-cleanup");
            }

            public bool PublishTravelPartyArrival(TravelPartyRuntime party) => false;

            public void PublishSoloTravelResult(
                DailyV1SelectedSoloTravelDayResult result,
                bool arrivalEventHandledByCore)
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Npc, Is.SameAs(owner.ExpectedCoreTravelDayActor));
                Assert.That(result.WasCommitted, Is.True);
                Assert.That(result.ShouldReport, Is.False);
                Assert.That(result.Arrived, Is.False);
                Assert.That(arrivalEventHandledByCore, Is.True);
                owner.Calls.Add("core-travel-day-result");
            }
        }

        private sealed class EmptyTravelBoundary : IDailyV1SelectedTravelDayBoundary
        {
            internal static readonly EmptyTravelBoundary Instance = new EmptyTravelBoundary();
            public bool TryPrepareTransition(DailyV1NpcRuntimeOwners npc,
                DailyV1SelectedTravelTransitionPlan plan, bool arriving, out object projectionReceipt)
            { projectionReceipt = null; return false; }
            public void PublishCommittedTransition(DailyV1NpcRuntimeOwners npc,
                DailyV1SelectedTravelTransitionPlan plan, bool arriving, object projectionReceipt) =>
                throw new AssertionException("No travel transition is expected in this fixture.");
            public bool PublishTravelPartyArrival(TravelPartyRuntime party) => false;
            public void PublishSoloTravelResult(
                DailyV1SelectedSoloTravelDayResult result,
                bool arrivalEventHandledByCore) =>
                throw new AssertionException("No solo travel result is expected in this fixture.");
        }

        private sealed class NoOpTravelPartyP12AdvanceProtocol
            : IDailyV1SelectedP12TravelPartyAdvanceProtocol
        {
            internal static readonly NoOpTravelPartyP12AdvanceProtocol Instance =
                new NoOpTravelPartyP12AdvanceProtocol();

            public bool TryValidateOwnerSections(
                DailyV1SelectedRuntimeOwnerGraph graph,
                IReadOnlyList<string> expectedSectionIds) => graph != null && expectedSectionIds != null;

            public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds) => sectionIds != null;

            public bool TryRefreshCommittedMutationBaselines(IReadOnlyCollection<string> sectionIds) => sectionIds != null;
            public bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds) => sectionIds != null;
            public void FaultClosed() { }
        }

        private sealed class FakeActorTurnContext : IDailyV1SelectedActorTurnExecutionContext
        {
            internal FakeActorTurnContext(
                IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster,
                PersonStore personStore,
                ActorChoiceStore actorChoiceStore,
                IDailyV1ActorTurnExecutionBoundary boundary)
            {
                RuntimeRoster = runtimeRoster;
                PersonStore = personStore;
                ActorChoiceStore = actorChoiceStore;
                Boundary = boundary;
            }

            public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
            public PersonStore PersonStore { get; }
            public ActorChoiceStore ActorChoiceStore { get; }
            public DailyV1ActorTurnCoreRoots CoreRoots => null;
            public IDailyV1ActorTurnExecutionBoundary Boundary { get; }
            public IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary =>
                Boundary as IDailyV1SelectedMerchantTradeStateBoundary;
        }

        private sealed class FakeDayStartContext : IDailyV1SelectedDayStartExecutionContext
        {
            internal FakeDayStartContext(
                FakeBoundary owner,
                DailyV1SelectedRuntimeOwnerGraph graph,
                IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRosterOverride)
            {
                RuntimeRoster = runtimeRosterOverride ?? graph.Npcs.Select(npc =>
                    new DailyV1ActorTurnRosterProjection(npc.RuntimeId, null)).ToList();
                CrimeSystemPresent = owner.CrimeSystemPresent;
                JusticeSystemPresent = owner.JusticeSystemPresent;
                HasUnselectedLiveRuntimeMember = owner.HasUnselectedLiveRuntimeMember;
                FakeDayStartOperationBoundary operationBoundary =
                    new FakeDayStartOperationBoundary(owner, graph);
                CrimeJusticeBoundary = operationBoundary;
                MerchantTradeBoundary = operationBoundary;
            }

            public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
            public bool HasUnselectedLiveRuntimeMember { get; }
            public bool CrimeSystemPresent { get; }
            public bool JusticeSystemPresent { get; }
            public IDailyV1SelectedCrimeJusticeDailyBoundary CrimeJusticeBoundary { get; }
            public IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
        }

        private sealed class FakeDayStartOperationBoundary
            : IDailyV1SelectedCrimeJusticeDailyBoundary,
                IDailyV1SelectedMerchantTradeStateBoundary
        {
            private readonly FakeBoundary owner;
            private readonly DailyV1SelectedRuntimeOwnerGraph graph;

            internal FakeDayStartOperationBoundary(
                FakeBoundary owner,
                DailyV1SelectedRuntimeOwnerGraph graph)
            {
                this.owner = owner;
                this.graph = graph;
            }

            public DailyV1JusticeRecordsOwner JusticeRecordsOwner =>
                owner.SubstituteJusticeRecordsOwner ?? graph.JusticeRecordsOwner;

            public void ExecuteWithinDailyMutation(
                DailyV1SelectedCrimeJusticeDailyPhase phase,
                Action mutation)
            {
                owner.DayStartPhases.Add(phase);
                owner.Calls.Add("crime:" + phase);
                mutation();
            }

            public bool CanProjectStatusChanges(
                DailyV1NpcRuntimeOwners actor,
                IReadOnlyList<DailyV1StatusDefinition> statusToAdd) =>
                graph.ContainsExactNpcOwner(actor);

            public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners actor)
            {
                if (!graph.ContainsExactNpcOwner(actor)
                    || !actor.CanAdvanceCrimeJusticeRevision(1))
                    throw new InvalidOperationException("The fake day-start owner is not admitted.");
            }

            public void NotifyStatusMutationCommitted(
                DailyV1NpcRuntimeOwners actor,
                DailyV1StatusDefinition status,
                bool added) => actor.NotifyCrimeJusticeMutationCommitted();

            public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners actor) =>
                actor.NotifyCrimeJusticeMutationCommitted();

            public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
                graph.JusticeRecordsOwner.ContainsSentence(sentence);

            public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) { }
            public void Log(SimulationLogCategory category, string message) { }
            public void LogWarning(string message) { }

            public bool TryInstallPlanState(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ItemDefinition item,
                string originCityRuntimeId,
                string targetCityRuntimeId,
                int plannedAmount,
                int remainingAmount,
                float purchasePricePerItem,
                int waitDaysAtDestination,
                int pendingTravelDays,
                string originDecisionId)
            {
                owner.Calls.Add("merchant-plan");
                return actor.MerchantTradePlan.TryInstallState(item,
                    originCityRuntimeId, targetCityRuntimeId, plannedAmount,
                    remainingAmount, purchasePricePerItem, waitDaysAtDestination,
                    pendingTravelDays, originDecisionId);
            }

            public bool TrySetTradeTravelPlan(
                DailyV1NpcRuntimeOwners actor,
                string targetCityRuntimeId,
                float utility,
                string originDecisionId) => false;

            public void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor) { }
            public NpcDecisionRecord RecordTradeRedirect(
                DailyV1NpcRuntimeOwners actor,
                string targetLocationRuntimeId,
                CommercialDecisionEvidence evidence) => null;
            public void LogTradeDecisionFailure(string message) { }
            public void LogTrade(string message) { }
        }

        private sealed class FakeActionSlotP12Protocol
            : IDailyV1SelectedP12NpcActionOwnerMutationProtocol
        {
            private readonly FakeBoundary owner;

            internal FakeActionSlotP12Protocol(FakeBoundary owner) =>
                this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

            public bool TryValidateOwnerSection(
                DailyV1SelectedRuntimeOwnerGraph graph,
                DailyV1NpcRuntimeOwners actor,
                string sectionId)
            {
                owner.ActionSlotOwnerValidationCount++;
                owner.LastActionSlotSectionId = sectionId;
                return !owner.RejectActionSlotOwnerValidation
                    && graph != null && graph.ContainsExactNpcOwner(actor)
                    && string.Equals(sectionId,
                        DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
                            + actor.RuntimeId,
                        StringComparison.Ordinal);
            }

            public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds)
            {
                owner.ActionSlotUnchangedValidationCount++;
                return sectionIds != null && sectionIds.Count == 1;
            }

            public bool TryRefreshCompatibilityBaselines(IReadOnlyCollection<string> sectionIds)
            {
                owner.ActionSlotBaselineRefreshCount++;
                return sectionIds != null && sectionIds.Count == 1;
            }

            public void FaultClosed() { }
        }

        private sealed class FakeActorTurnBoundary : IDailyV1ActorTurnExecutionBoundary,
            IDailyV1SelectedActorChoiceExecutionContext,
            IDailyV1CoreActorChoiceExecutionBoundary,
            IDailyV1SelectedMerchantTradeStateBoundary,
            IDailyV1SelectedMerchantTradeStateP12HostPorts,
            IDailyV1SelectedActorTurnHostPorts,
            IDailyV1SelectedCrimeJusticeDailyBoundary
        {
            private readonly FakeBoundary owner;
            private readonly bool? selectedActionExecution;
            private readonly PermissiveCrimeJusticeMutationProtocol crimeJusticeProtocol =
                new PermissiveCrimeJusticeMutationProtocol(null);
            private readonly FakeActionSlotP12Protocol actionSlotP12Protocol;
            internal FakeActorTurnBoundary(
                FakeBoundary owner,
                bool? selectedActionExecution = null)
            {
                this.owner = owner;
                this.selectedActionExecution = selectedActionExecution;
                actionSlotP12Protocol = new FakeActionSlotP12Protocol(owner);
                crimeJusticeProtocol.Owner = owner;
            }
            public bool IsReservedToday(DailyV1NpcRuntimeOwners actor) => false;
            public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive)
            {
                owner.LegacyDirectiveCallbackCount++;
                owner.Calls.Add("directive");
            }
            public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal)
            {
                owner.LegacyActorChoiceCallbackCount++;
                return false;
            }
            public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor) =>
                owner.Calls.Add("actor-merchant:" + actor.RuntimeId);
            public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor) =>
                owner.LegacyActionCallbackCount++;

            public bool TryGetActorChoiceExecutionContext(
                DailyV1NpcRuntimeOwners actor,
                out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
            {
                executionBoundary = owner.ActorChoiceSelectedActionExecutionOverride.HasValue
                    ? new FakeActorTurnBoundary(
                        owner, owner.ActorChoiceSelectedActionExecutionOverride.Value)
                    : this;
                return true;
            }

            public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor) { }
            public bool TryPrepareRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryRefreshRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool CanCommitRequestedActionSlot(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryPublishRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public void CancelPreparedRequestedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) { }
            public void LogRequestedActionDecisionFailure(string message) { }
            public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor) => null;

            public bool TryInstallPlanState(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ItemDefinition item,
                string originCityRuntimeId,
                string targetCityRuntimeId,
                int plannedAmount,
                int remainingAmount,
                float purchasePricePerItem,
                int waitDaysAtDestination,
                int pendingTravelDays,
                string originDecisionId) => true;
            public bool TrySetTradeTravelPlan(
                DailyV1NpcRuntimeOwners actor,
                string targetCityRuntimeId,
                float utility,
                string originDecisionId) => true;
            public void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor) { }
            public NpcDecisionRecord RecordTradeRedirect(
                DailyV1NpcRuntimeOwners actor,
                string targetLocationRuntimeId,
                CommercialDecisionEvidence evidence) => null;
            public void LogTradeDecisionFailure(string message) { }
            public void LogTrade(string message) { }
            public bool CanAdmitSelectedMerchantPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
            public bool NotifySelectedMerchantPlanMutationCommitted(
                DailyV1NpcRuntimeOwners actor, long revision) => true;
            public bool CanProjectSelectedMerchantTravelPlanMutation(
                DailyV1NpcRuntimeOwners actor,
                string targetLocationRuntimeId,
                string targetCityRuntimeId) => true;
            public bool CanAdmitSelectedMerchantTravelPlanMutation(DailyV1NpcRuntimeOwners actor) => true;
            public bool NotifySelectedMerchantTravelPlanMutationCommitted(
                DailyV1NpcRuntimeOwners actor, long revision) => true;
            public bool ExecuteWithinSelectedMerchantDailyOperation(
                DailyV1NpcRuntimeOwners actor,
                Func<bool> operation) => operation != null && operation();
            public IDailyV1SelectedP12NpcActionOwnerMutationProtocol ActionSlotP12MutationProtocol =>
                owner.OmitActionSlotP12MutationProtocol ? null : actionSlotP12Protocol;
            public bool IsSelectedActionSlotProjectionPrepared(
                DailyV1SelectedRuntimeOwnerGraph graph,
                DailyV1NpcRuntimeOwners actor) =>
                graph != null && graph.ContainsExactNpcOwner(actor);

            private FakeDayStartOperationBoundary DayStartBoundary =>
                new FakeDayStartOperationBoundary(owner, owner.CurrentGraph);

            public DailyV1JusticeRecordsOwner JusticeRecordsOwner =>
                DayStartBoundary.JusticeRecordsOwner;

            public bool TryGetSelectedCrimeJusticeDailyMutationHostBinding(
                DailyV1SelectedRuntimeOwnerGraph graph,
                out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding)
            {
                binding = null;
                if (graph == null || !ReferenceEquals(graph, owner.CurrentGraph)) return false;
                object justiceIdentity = graph.JusticeRecordsOwner;
                binding = new DailyV1SelectedCrimeJusticeDailyMutationHostBinding(
                    crimeJusticeProtocol,
                    this, this,
                    graph.JusticeRecordsOwner,
                    justiceIdentity, justiceIdentity,
                    graph.JusticeRecordsOwner,
                    admissionEnabled: owner.CrimeJusticeAdmissionEnabled);
                return true;
            }

            public void ExecuteWithinDailyMutation(
                DailyV1SelectedCrimeJusticeDailyPhase phase,
                Action mutation)
            {
                owner.LegacyCrimeJusticeDailyMutations++;
                DayStartBoundary.ExecuteWithinDailyMutation(phase, mutation);
            }

            public bool CanProjectStatusChanges(
                DailyV1NpcRuntimeOwners actor,
                IReadOnlyList<DailyV1StatusDefinition> statusToAdd) =>
                DayStartBoundary.CanProjectStatusChanges(actor, statusToAdd);

            public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners actor) =>
                DayStartBoundary.EnsureMutationAllowed(actor);

            public void NotifyStatusMutationCommitted(
                DailyV1NpcRuntimeOwners actor,
                DailyV1StatusDefinition status,
                bool added) => DayStartBoundary.NotifyStatusMutationCommitted(actor, status, added);

            public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners actor) =>
                DayStartBoundary.NotifyHiddenMutationCommitted(actor);

            public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
                DayStartBoundary.CanRemoveSentenceProjection(sentence);

            public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence) =>
                DayStartBoundary.CommitSentenceProjectionRemoval(sentence);

            public void Log(SimulationLogCategory category, string message) =>
                DayStartBoundary.Log(category, message);

            public void LogWarning(string message) => DayStartBoundary.LogWarning(message);

            private sealed class PermissiveCrimeJusticeMutationProtocol
                : IDailyV1P12CrimeJusticeMutationProtocol
            {
                internal FakeBoundary Owner { get; set; }

                internal PermissiveCrimeJusticeMutationProtocol(FakeBoundary owner) => Owner = owner;

                public void FaultClosed() { }
                public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds) =>
                    sectionIds != null && sectionIds.Count > 0;
                public bool TryRefreshCommittedMutationBaselines(IReadOnlyCollection<string> sectionIds) =>
                    sectionIds != null && sectionIds.Count > 0;
                public bool TryRefreshCommittedMutationBaselinesForReservation(
                    object reservationToken,
                    IReadOnlyCollection<string> sectionIds) =>
                    reservationToken != null && sectionIds != null && sectionIds.Count > 0;
                public bool TryReserveMutationEpoch(out object reservation)
                {
                    if (Owner != null) Owner.CoreCrimeJusticeDailyReservations++;
                    reservation = new object();
                    return true;
                }
                public bool TryNotifyReservedCommittedMutations(
                    object reservation,
                    IReadOnlyCollection<string> sectionIds) =>
                    reservation != null && sectionIds != null && sectionIds.Count > 0;
                public void ReleaseMutationEpoch(object reservation) { }
                public bool TryValidateMutationEpochCapacity() => true;
                public bool NotifyCommittedMutation(string sectionId) =>
                    !string.IsNullOrWhiteSpace(sectionId);
            }

            public IAuthoritativeRandomSource RandomSource => owner.randomSource;
            public bool RequiresActionProjection => owner.RequiresActionProjection;
            public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor) =>
                selectedActionExecution ?? owner.UseSelectedActionExecution;
            public void LogSkippedDirective(ScheduledDirective directive, string reason) { }
            public bool TryCreateActionProjectionBoundary(
                DailyV1NpcRuntimeOwners actor,
                out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
            { projectionBoundary = null; return false; }
            public bool TryGetDecisionRecordOwners(
                DailyV1NpcRuntimeOwners actor,
                out RuntimeIdAllocator idAllocator,
                out SimulationRecordSequence recordSequence,
                out NpcDecisionStore decisionStore)
            {
                owner.DecisionOwnerLookupCount++;
                idAllocator = owner.runtimeIdAllocator;
                recordSequence = owner.recordSequence;
                decisionStore = owner.decisionStore;
                return !owner.FailDecisionRecordOwnerResolution
                    && idAllocator != null && recordSequence != null && decisionStore != null;
            }
            public bool TryPrepareSelectedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionCandidate selected,
                IDailyV1SelectedActionProjectionBoundary decisionBoundary,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool CanCommitSelectedActionSlot(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryPublishSelectedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) => false;
            public void CancelPreparedSelectedActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcActionPayloadOwner payload) { }
            public void LogSelectedActionDecisionFailure(DailyV1NpcRuntimeOwners actor, string message) { }
            public bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) => false;
            public bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor) => false;
            public bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) => false;
            public void CancelPreparedCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor) { }
            public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor) { }
            public bool TryGetDispatchBoundary(
                DailyV1NpcRuntimeOwners actor,
                out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
            {
                owner.SelectedActionDispatchLookupCount++;
                dispatchBoundary = null;
                return false;
            }
            public bool TryCompleteDispatch(
                DailyV1NpcRuntimeOwners actor,
                DailyV1SelectedActionDispatchResult dispatch,
                out string resultMessage)
            { resultMessage = null; return false; }
            public bool TryGetActionStatusBoundary(
                DailyV1NpcRuntimeOwners actor,
                DailyV1NpcRuntimeOwners target,
                bool reuseExistingOperation,
                out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary)
            { boundary = null; return false; }
            public bool TryPrepareForcedOutcomeActionProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ActionDefinition action,
                DailyV1NpcActionPayloadOwner payload) => false;
            public bool TryGetForcedEscapeBoundary(
                DailyV1NpcRuntimeOwners actor,
                DailyV1ActionDefinition action,
                out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
            { boundary = null; return false; }
        }
    }

    private sealed class FakeRuntimeAdvanceBoundary : IDailyV1RuntimeAdvanceBoundary
    {
        private readonly DailyV1ExecutionBoundaryState state;
        private readonly IDailyV1SelectedDailyExecutionBoundary executionBoundary;
        internal List<string> Calls { get; } = new List<string>();
        internal bool FailSelectedContext { get; set; }

        internal FakeRuntimeAdvanceBoundary(
            DailyV1ExecutionBoundaryState state,
            IDailyV1SelectedDailyExecutionBoundary executionBoundary)
        {
            this.state = state;
            this.executionBoundary = executionBoundary;
        }

        public DailyV1ExecutionBoundaryState ExecutionState => state;
        public bool HasIntradayTimeline => false;
        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
        {
            Calls.Add("operation");
            operationScope = new FakeOperationScope();
            return true;
        }
        public bool TryCreateSelectedDailyExecutionBoundary(
            out IDailyV1SelectedDailyExecutionBoundary boundary)
        {
            Calls.Add("execution-boundary");
            boundary = FailSelectedContext ? null : executionBoundary;
            return !FailSelectedContext;
        }
        public bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure)
        {
            failure = SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
            return false;
        }
        public bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure)
        {
            failure = SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed;
            return false;
        }
        public bool TryCaptureCompletedDailyCaptureEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            Calls.Add("capture-evidence");
            ownerSections = DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
            mutationEpoch = 1L;
            failure = DailyCaptureEligibilityFailure.None;
            return true;
        }

        public bool TryCaptureDailyBoundaryAdmissionEvidence(
            bool sessionReplacement,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            Calls.Add("admission-evidence");
            ownerSections = DailyV1ExecutionBoundaryStateTests.CreateCompleteSnapshot();
            mutationEpoch = 1L;
            failure = DailyCaptureEligibilityFailure.None;
            return true;
        }

        public bool TryCaptureContinuationSaveEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure) =>
            TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false, out ownerSections, out mutationEpoch, out failure);

        public void FaultRuntimeAdmission() => Calls.Add("fault");
    }

    private sealed class FakeOperationScope : IDisposable
    {
        public void Dispose() { }
    }
}
