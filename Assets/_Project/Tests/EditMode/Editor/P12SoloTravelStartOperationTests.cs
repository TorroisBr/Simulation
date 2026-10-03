using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class P12SoloTravelStartOperationTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void DailySelectedTravelStart_BatchesOwnerCallbacksUnderNestedOperation()
    {
        ActorFixture fixture = CreateFixture();
        ContinuationCensusProtocol protocol = GetProtocol(fixture.Runtime);
        List<int> travelOperationDepths = new List<int>();
        List<bool> soloOperationActiveAtTravelMutation = new List<bool>();
        List<int> accountOperationDepths = new List<int>();
        List<int> planOperationDepths = new List<int>();
        List<int> knowledgeOperationDepths = new List<int>();
        List<int> sequenceOperationDepths = new List<int>();
        WrapTravelStateCommitted(fixture.Npc, changed =>
        {
            soloOperationActiveAtTravelMutation.Add(IsSoloTravelOperationActive(fixture.Runtime));
            travelOperationDepths.Add(ReadActiveOperationCount(protocol));
        });
        WrapAccountCommitted(fixture.Npc.MoneyAccount,
            () => accountOperationDepths.Add(ReadActiveOperationCount(protocol)));
        WrapTravelPlanCommitted(fixture.Npc.TravelPlan,
            () => planOperationDepths.Add(ReadActiveOperationCount(protocol)));
        WrapSpatialKnowledgeCommitted(fixture.Npc.SpatialKnowledge,
            () => knowledgeOperationDepths.Add(ReadActiveOperationCount(protocol)));
        WrapRecordSequenceCommitted(fixture.Records.Sequence,
            () => sequenceOperationDepths.Add(ReadActiveOperationCount(protocol)));

        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());
        Assert.That(fixture.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());
        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());

        Assert.That(fixture.Npc.IsTraveling, Is.True);
        Assert.That(fixture.Npc.TravelStartedToday, Is.False,
            "the later daily travel-progress step clears the start-day flag outside runtime.travel.start");
        Assert.That(fixture.Npc.CurrentCity, Is.Null);
        Assert.That(fixture.Npc.DestinationCity, Is.SameAs(fixture.World.B));
        Assert.That(fixture.World.A.ImportantNpcs, Has.No.Member(fixture.Npc));
        Assert.That(fixture.World.B.ImportantNpcs, Has.No.Member(fixture.Npc),
            "starting travel does not publish target-City presence");
        Assert.That(fixture.Npc.Money, Is.EqualTo(98f));
        Assert.That(fixture.Npc.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.False);
        Assert.That(fixture.Npc.SpatialKnowledge.KnowsLocation(fixture.World.A.Location.RuntimeId), Is.True);
        Assert.That(fixture.Npc.SpatialKnowledge.KnowsRoute(fixture.World.RouteAB.RuntimeId), Is.True);
        Assert.That(fixture.Records.Events.Events, Has.Count.EqualTo(1));
        Assert.That(fixture.Records.Events.Events[0].EventType, Is.EqualTo(DomainEventType.NpcTravelStarted));

        Assert.That(accountOperationDepths, Is.EqualTo(new[] { 2 }),
            "the travel charge is directly owned by runtime.travel.start; no money-transfer child operation is entered");
        Assert.That(planOperationDepths, Is.EqualTo(new[] { 2 }));
        Assert.That(knowledgeOperationDepths, Is.EqualTo(new[] { 1, 2 }),
            "daily origin observation occurs before the action; route discovery commits inside runtime.travel.start");
        Assert.That(soloOperationActiveAtTravelMutation, Is.EqualTo(new[] { true, false }));
        Assert.That(travelOperationDepths, Is.EqualTo(new[] { 2, 1 }),
            "StartTravel is inside the nested travel operation while TravelStartedToday clearing is only in daily advance");
        Assert.That(sequenceOperationDepths, Does.Contain(2),
            "the travel event sequence allocation is part of the nested travel operation");
        Assert.That(after, Is.EqualTo(before + 3),
            "the decision record, bounded travel start, and later start-day clear each publish their own epoch");
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
    }

    [Test]
    public void DailySelectedTravelStart_StaleOwnerBaselineFaultsBeforeCharge()
    {
        ActorFixture fixture = CreateFixture();
        SetPrivateField(fixture.Npc.TravelPlan, "revision", fixture.Npc.TravelPlan.Revision + 1L);

        Assert.Throws<InvalidOperationException>(() => fixture.Runtime.TryAdvanceDay(out _));

        Assert.That(fixture.Npc.Money, Is.EqualTo(100f));
        Assert.That(fixture.Npc.MoneyAccount.Revision, Is.Zero);
        Assert.That(fixture.Npc.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Npc.IsTraveling, Is.False);
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.True);
        Assert.That(fixture.Records.Events.Events, Is.Empty);
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailySelectedTravelStart_WrongBoundProviderFaultsBeforeCharge()
    {
        ActorFixture fixture = CreateFixture(bindProviderToSelectedSystem: false);

        Assert.Throws<InvalidOperationException>(() => fixture.Runtime.TryAdvanceDay(out _));

        Assert.That(fixture.Npc.Money, Is.EqualTo(100f));
        Assert.That(fixture.Npc.MoneyAccount.Revision, Is.Zero);
        Assert.That(fixture.Npc.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Npc.IsTraveling, Is.False);
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.True);
        Assert.That(fixture.Records.Events.Events, Is.Empty);
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void DailySelectedTravelStart_EventSequenceExhaustionKeepsTravelAndBatchesEventId()
    {
        ActorFixture fixture = CreateFixture(recordDecision: false, sequenceNextValue: long.MaxValue);
        IOwnerSectionCensusProvider eventProvider = RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(
            fixture.Records.Allocator);
        IOwnerSectionCensusProvider sequenceProvider = new SimulationRecordSequenceCensusProvider(
            fixture.Records.Sequence);
        long eventRevisionBefore = eventProvider.GetCurrentCensus().Revision;
        long sequenceRevisionBefore = sequenceProvider.GetCurrentCensus().Revision;
        LogAssert.Expect(LogType.Error, "Cannot allocate domain EventId: Simulation record sequence is exhausted.");

        Assert.That(fixture.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());

        Assert.That(fixture.Npc.IsTraveling, Is.True);
        Assert.That(fixture.Npc.Money, Is.EqualTo(98f));
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.False,
            "event-recording failure preserves the provider's existing successful-start plan clear");
        Assert.That(fixture.Records.Events.Events, Is.Empty);
        Assert.That(eventProvider.GetCurrentCensus().Revision, Is.EqualTo(eventRevisionBefore + 1));
        Assert.That(sequenceProvider.GetCurrentCensus().Revision, Is.EqualTo(sequenceRevisionBefore));
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
    }

    [Test]
    public void FailedTravelStartAfterCharge_CompensatesWithinSingleSoloOperation()
    {
        ActorFixture fixture = CreateFixture(
            recordDecision: false,
            travelStateRevision: long.MaxValue);
        ContinuationCensusProtocol protocol = GetProtocol(fixture.Runtime);
        List<int> operationDepths = new List<int>();
        WrapAccountCommitted(fixture.Npc.MoneyAccount,
            () => operationDepths.Add(ReadActiveOperationCount(protocol)));
        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());
        MethodInfo execute = typeof(SimulationRuntime).GetMethod("TryExecuteAction", PrivateInstance);
        Assert.That(execute, Is.Not.Null);
        NpcActionResult result = (NpcActionResult)execute.Invoke(fixture.Runtime, new object[]
        {
            fixture.Npc,
            new NpcActionRuntime(
                fixture.Action,
                fixture.World.B,
                null,
                NpcTravelReason.Trade,
                2f,
                0f),
            fixture.Action
        });

        Assert.That(result.Success, Is.False,
            "travel-state revision saturation rejects StartTravel after TravelSystem's committed charge, then existing compensation restores it");
        Assert.That(fixture.Npc.Money, Is.EqualTo(100f));
        Assert.That(fixture.Npc.MoneyAccount.Revision, Is.EqualTo(2));
        Assert.That(fixture.Npc.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Npc.IsTraveling, Is.False);
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.True);
        Assert.That(fixture.Records.Events.Events, Is.Empty);
        Assert.That(operationDepths, Is.EqualTo(new[] { 1, 1 }),
            "both account writes use the real travel charge/restore path under one operation");
        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1),
            "the same account section is reported once when a committed debit is compensated before scope close");
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
    }

    [Test]
    public void ExceptionAfterCharge_FlushesCommittedAccountBeforeRuntimeFault()
    {
        ActorFixture fixture = CreateFixture(recordDecision: false);
        fixture.Npc.SpatialKnowledge.DiscoverLocation(fixture.World.A.Location.RuntimeId);
        ContinuationCensusProtocol protocol = GetProtocol(fixture.Runtime);
        long before = ReadProtocolEpoch(protocol);
        FieldInfo admission = typeof(NpcRuntime).GetField(
            "p12TravelStateMutationAdmission", PrivateInstance);
        admission.SetValue(fixture.Npc,
            (Func<bool, IReadOnlyList<CityRuntime>, bool>)((_, __) => false));

        Assert.Throws<InvalidOperationException>(() => fixture.Runtime.TryAdvanceDay(out _));

        Assert.That(fixture.Npc.Money, Is.EqualTo(98f),
            "the exceptional owner-admission path preserves the existing partial charge and does not invent rollback");
        Assert.That(fixture.Npc.MoneyAccount.Revision, Is.EqualTo(1));
        Assert.That(fixture.Npc.IsTraveling, Is.False);
        Assert.That(fixture.Npc.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.True);
        Assert.That(fixture.Records.Events.Events, Is.Empty);
        Assert.That(ReadProtocolEpoch(protocol), Is.EqualTo(before + 1),
            "scope unwinding reports the account commit before daily admission is faulted");
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void SelectedTravelNoOp_ClosesWithoutMutationEpoch()
    {
        ActorFixture fixture = CreateFixture(balance: 0f);
        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());
        MethodInfo execute = typeof(SimulationRuntime).GetMethod("TryExecuteAction", PrivateInstance);
        Assert.That(execute, Is.Not.Null);

        NpcActionResult result = (NpcActionResult)execute.Invoke(fixture.Runtime, new object[]
        {
            fixture.Npc,
            new NpcActionRuntime(
                fixture.Action,
                fixture.World.B,
                null,
                NpcTravelReason.Trade,
                2f,
                0f),
            fixture.Action
        });

        Assert.That(result.Success, Is.False);
        Assert.That(fixture.Npc.MoneyAccount.Revision, Is.Zero);
        Assert.That(fixture.Npc.CurrentCity, Is.SameAs(fixture.World.A));
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.True);
        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before));
        Assert.That(fixture.Runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
    }

    [Test]
    public void SelectedTravelFromNonCityLocation_DoesNotInventSourceCitySection()
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 2);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        NpcRuntime npc = new NpcRuntime(
            "p12-non-city-traveler",
            SimulationTestFactory.CreateNpc("p12-non-city-traveler"),
            null,
            100f);
        SetPrivateField(npc, "currentLocation", world.A.Location);
        npc.TravelPlan.Set(world.B, NpcTravelReason.Trade, 100f, 2f, "non-city-travel-plan");
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-non-city-travel-action",
            NpcActionType.Travel,
            NpcActionCategory.Travel);
        action.canFail = false;
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        TravelActionProvider provider = new TravelActionProvider(travel);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B, world.C },
            new[] { npc },
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider> { provider }),
            travelSystem: travel,
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        List<string> sectionsAtTravelCommit = new List<string>();
        WrapTravelPlanCommitted(npc.TravelPlan, () =>
        {
            object context = typeof(SimulationRuntime)
                .GetField("activeP12SoloTravelStartOperationContext", PrivateInstance)
                .GetValue(runtime);
            FieldInfo changedIds = context.GetType().GetField(
                "ChangedSectionIds",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            sectionsAtTravelCommit.AddRange((HashSet<string>)changedIds.GetValue(context));
        });
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());
        MethodInfo execute = typeof(SimulationRuntime).GetMethod("TryExecuteAction", PrivateInstance);
        NpcActionResult result = (NpcActionResult)execute.Invoke(runtime, new object[]
        {
            npc,
            new NpcActionRuntime(action, world.B, null, NpcTravelReason.Trade, 2f, 0f),
            action
        });

        Assert.That(result.Success, Is.True);
        Assert.That(npc.CurrentCity, Is.Null);
        Assert.That(npc.IsTraveling, Is.True);
        Assert.That(world.A.ImportantNpcs, Has.No.Member(npc));
        Assert.That(world.B.ImportantNpcs, Has.No.Member(npc));
        Assert.That(sectionsAtTravelCommit, Does.Contain(NpcTravelStateCensusProvider.SectionIdFor(npc.RuntimeId)));
        Assert.That(sectionsAtTravelCommit, Does.Not.Contain(CityNpcPresenceCensusProvider.SectionIdFor(world.A.RuntimeId)));
        Assert.That(sectionsAtTravelCommit, Does.Not.Contain(CityNpcPresenceCensusProvider.SectionIdFor(world.B.RuntimeId)));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessment),
            Is.True, assessment.ToString());
    }

    [Test]
    public void SelectedTravelWithUnprojectedCurrentCity_DoesNotPreflightOrReportCitySection()
    {
        ActorFixture fixture = CreateFixture(recordDecision: false);
        fixture.World.A.RemoveImportantNpc(fixture.Npc);
        Assert.That(fixture.Npc.CurrentCity, Is.Null);
        Assert.That(fixture.World.A.ImportantNpcs, Has.No.Member(fixture.Npc));
        SetPrivateField(fixture.Npc, "currentLocation", fixture.World.A.Location);
        SetPrivateField(fixture.Npc, "currentCity", fixture.World.A);

        // A changed but unrelated projection must not become an operation
        // precondition when the actor is not a member and travel will not
        // mutate that City section.
        SetPrivateField(
            fixture.World.A,
            "importantNpcRevision",
            fixture.World.A.ImportantNpcRevision + 1L);

        List<string> sectionsAtTravelMutation = new List<string>();
        WrapTravelPlanCommitted(fixture.Npc.TravelPlan, () =>
        {
            object context = typeof(SimulationRuntime)
                .GetField("activeP12SoloTravelStartOperationContext", PrivateInstance)
                .GetValue(fixture.Runtime);
            FieldInfo changedIds = context.GetType().GetField(
                "ChangedSectionIds",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            sectionsAtTravelMutation.AddRange((HashSet<string>)changedIds.GetValue(context));
        });

        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long before, out ContinuationCensusFailure beforeFailure), Is.True, beforeFailure.ToString());
        MethodInfo execute = typeof(SimulationRuntime).GetMethod("TryExecuteAction", PrivateInstance);
        NpcActionResult result = (NpcActionResult)execute.Invoke(fixture.Runtime, new object[]
        {
            fixture.Npc,
            new NpcActionRuntime(
                fixture.Action,
                fixture.World.B,
                null,
                NpcTravelReason.Trade,
                2f,
                0f),
            fixture.Action
        });

        Assert.That(result.Success, Is.True,
            "a stale revision for a non-member City section is outside this operation's owner preflight");
        Assert.That(fixture.Npc.CurrentCity, Is.Null);
        Assert.That(fixture.Npc.IsTraveling, Is.True);
        Assert.That(fixture.Npc.Money, Is.EqualTo(98f));
        Assert.That(fixture.World.A.ImportantNpcs, Has.No.Member(fixture.Npc));
        Assert.That(fixture.World.B.ImportantNpcs, Has.No.Member(fixture.Npc));
        Assert.That(sectionsAtTravelMutation, Does.Not.Contain(
            CityNpcPresenceCensusProvider.SectionIdFor(fixture.World.A.RuntimeId)));
        Assert.That(sectionsAtTravelMutation, Does.Not.Contain(
            CityNpcPresenceCensusProvider.SectionIdFor(fixture.World.B.RuntimeId)));
        Assert.That(fixture.Runtime.TryReadNpcRosterCensusMutationEpoch(
            out long after, out ContinuationCensusFailure afterFailure), Is.True, afterFailure.ToString());
        Assert.That(after, Is.EqualTo(before + 1));
    }

    [Test]
    public void ScheduledRequestActionContract_DoesNotAcceptTravelAsEscapeDirective()
    {
        NpcActionData travelAction = SimulationTestFactory.CreateAction(
            "p12-scheduled-travel-action",
            NpcActionType.Travel,
            NpcActionCategory.Travel);

        Assert.Throws<ArgumentException>(() => new ScheduledDirective(
            "p12-scheduled-travel-directive",
            1L,
            ScheduledDirectiveMode.RequestAction,
            ScheduledDirectiveOperation.EscapePrison,
            "p12-scheduled-travel-actor",
            travelAction));
    }

    [Test]
    public void LegacyTravelExecution_RemainsOutsideP12OperationAdmission()
    {
        ActorFixture fixture = CreateFixture(admitted: false);

        HashSet<string> expectedOperations = (HashSet<string>)typeof(ContinuationCensusProtocol)
            .GetField("expectedOperations", PrivateInstance)
            .GetValue(GetProtocol(fixture.Runtime));
        Assert.That(expectedOperations, Does.Not.Contain("runtime.travel.start"));

        Assert.That(fixture.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure),
            Is.True, advanceFailure.ToString());

        Assert.That(fixture.Npc.IsTraveling, Is.True);
        Assert.That(fixture.Npc.Money, Is.EqualTo(98f));
        Assert.That(fixture.Npc.TravelPlan.IsActive, Is.False);
        Assert.That(fixture.Records.Events.Events, Has.Count.EqualTo(1));
    }

    private static ActorFixture CreateFixture(
        float balance = 100f,
        bool admitted = true,
        bool recordDecision = true,
        bool bindProviderToSelectedSystem = true,
        long? sequenceNextValue = null,
        long? travelStateRevision = null)
    {
        ThreeCityFixture world = new ThreeCityFixture(false, routeABTravelDays: 2);
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        if (sequenceNextValue.HasValue)
            SetPrivateField(records.Sequence, "nextSequence", sequenceNextValue.Value);
        NpcRuntime npc = new NpcRuntime(
            "p12-solo-travel-actor",
            SimulationTestFactory.CreateNpc("p12-solo-travel-actor"),
            world.A,
            balance);
        if (travelStateRevision.HasValue)
            SetPrivateField(npc, "travelStateRevision", travelStateRevision.Value);
        npc.TravelPlan.Set(world.B, NpcTravelReason.Trade, 100f, 2f, "solo-travel-plan");
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-solo-travel-action",
            NpcActionType.Travel,
            NpcActionCategory.Travel);
        action.baseUtility = 100f;
        action.canFail = false;
        TravelSystem travel = world.CreateTravelSystem(records.Time, records.EventRecorder);
        TravelSystem providerTravel = bindProviderToSelectedSystem
            ? travel
            : world.CreateTravelSystem(records.Time, records.EventRecorder);
        TravelActionProvider provider = new TravelActionProvider(providerTravel);
        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { world.A, world.B, world.C },
            new[] { npc },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider> { provider }),
            decisionRecorder: recordDecision ? records.DecisionRecorder : null,
            travelSystem: travel,
            recordSequence: records.Sequence,
            runtimeIdAllocator: records.Allocator,
            runtimeAdmissionContext: admitted
                ? SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1()
                : null);
        return new ActorFixture(world, records, npc, action, travel, provider, runtime);
    }

    private static ContinuationCensusProtocol GetProtocol(SimulationRuntime runtime)
    {
        return (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", PrivateInstance)
            .GetValue(runtime);
    }

    private static int ReadActiveOperationCount(ContinuationCensusProtocol protocol)
    {
        Assert.That(protocol.TryReadActiveOperationCount(
            out int count, out ContinuationCensusFailure failure), Is.True, failure.ToString());
        return count;
    }

    private static long ReadProtocolEpoch(ContinuationCensusProtocol protocol)
    {
        return (long)typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", PrivateInstance)
            .GetValue(protocol);
    }

    private static bool IsSoloTravelOperationActive(SimulationRuntime runtime)
    {
        return typeof(SimulationRuntime)
            .GetField("activeP12SoloTravelStartOperationContext", PrivateInstance)
            .GetValue(runtime) != null;
    }

    private static void WrapTravelStateCommitted(NpcRuntime npc, Action<bool> observe)
    {
        FieldInfo field = typeof(NpcRuntime).GetField("p12TravelStateMutationCommitted", PrivateInstance);
        Action<bool, IReadOnlyList<CityRuntime>> original =
            (Action<bool, IReadOnlyList<CityRuntime>>)field.GetValue(npc);
        field.SetValue(npc, (Action<bool, IReadOnlyList<CityRuntime>>)((changed, cities) =>
        {
            observe(changed);
            original?.Invoke(changed, cities);
        }));
    }

    private static void WrapAccountCommitted(MoneyAccountRuntime account, Action observe)
    {
        FieldInfo field = typeof(MoneyAccountRuntime).GetField("p12MutationCommitted", PrivateInstance);
        Action original = (Action)field.GetValue(account);
        field.SetValue(account, (Action)(() =>
        {
            observe();
            original?.Invoke();
        }));
    }

    private static void WrapTravelPlanCommitted(NpcTravelPlanRuntime plan, Action observe)
    {
        FieldInfo field = typeof(NpcTravelPlanRuntime).GetField("p12MutationCommitted", PrivateInstance);
        Action original = (Action)field.GetValue(plan);
        field.SetValue(plan, (Action)(() =>
        {
            observe();
            original?.Invoke();
        }));
    }

    private static void WrapSpatialKnowledgeCommitted(SpatialKnowledgeRuntime knowledge, Action observe)
    {
        FieldInfo field = typeof(SpatialKnowledgeRuntime).GetField("p12MutationCommitted", PrivateInstance);
        Action original = (Action)field.GetValue(knowledge);
        field.SetValue(knowledge, (Action)(() =>
        {
            observe();
            original?.Invoke();
        }));
    }

    private static void WrapRecordSequenceCommitted(SimulationRecordSequence sequence, Action observe)
    {
        FieldInfo field = typeof(SimulationRecordSequence).GetField("p12MutationCommitted", PrivateInstance);
        Action original = (Action)field.GetValue(sequence);
        field.SetValue(sequence, (Action)(() =>
        {
            observe();
            original?.Invoke();
        }));
    }

    private static void SetPrivateField(object owner, string fieldName, object value)
    {
        owner.GetType().GetField(fieldName, PrivateInstance).SetValue(owner, value);
    }

    private sealed class ActorFixture
    {
        public ThreeCityFixture World { get; }
        public RecordFixture Records { get; }
        public NpcRuntime Npc { get; }
        public NpcActionData Action { get; }
        public TravelSystem Travel { get; }
        public TravelActionProvider Provider { get; }
        public SimulationRuntime Runtime { get; }

        public ActorFixture(
            ThreeCityFixture world,
            RecordFixture records,
            NpcRuntime npc,
            NpcActionData action,
            TravelSystem travel,
            TravelActionProvider provider,
            SimulationRuntime runtime)
        {
            World = world;
            Records = records;
            Npc = npc;
            Action = action;
            Travel = travel;
            Provider = provider;
            Runtime = runtime;
        }
    }
}
