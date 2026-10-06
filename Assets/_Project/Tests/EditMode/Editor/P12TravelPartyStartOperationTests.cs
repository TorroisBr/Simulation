using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class P12TravelPartyStartOperationTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void RuntimeStartRegistersExactAllocatorWitnessAndPublishesOneSharedEpoch()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        foreach (NpcRuntime member in fixture.Members)
        {
            member.SpatialKnowledge.DiscoverLocation(fixture.World.A.Location.RuntimeId);
            member.SpatialKnowledge.DiscoverRoute(fixture.World.RouteAB.RuntimeId);
        }
        SimulationRuntime runtime = CreateDailyRuntime(fixture);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        OwnerSectionCensusWitness before = GetTravelPartyAllocatorWitness(runtime);
        long epochBefore = ReadProtocolEpoch(protocol);
        long partyStoreRevisionBefore = fixture.Parties.Revision;
        long eventCountBefore = fixture.Records.Events.Events.Count;
        long sequenceRevisionBefore = GetSequenceWitness(runtime).Revision;

        Assert.That(before.SectionId, Is.EqualTo(RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId));
        Assert.That(before.SchemaVersion, Is.EqualTo(RuntimeIdAllocatorCensusProvider.SchemaVersion));
        object allocatorIdentity = typeof(RuntimeIdAllocator)
            .GetProperty("CensusOwnerIdentity", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(fixture.Records.Allocator);
        Assert.That(before.OwnerInstanceIdentity, Is.SameAs(allocatorIdentity));
        Assert.That(before.Cardinality, Is.EqualTo(1));
        Assert.That(before.Revision, Is.EqualTo(0));
        Assert.That(runtime.TryStartTravelParty(CreateContext(fixture)), Is.True);

        OwnerSectionCensusWitness after = GetTravelPartyAllocatorWitness(runtime);
        Assert.That(after.OwnerInstanceIdentity, Is.SameAs(before.OwnerInstanceIdentity));
        Assert.That(after.Cardinality, Is.EqualTo(1));
        Assert.That(after.Revision, Is.EqualTo(before.Revision + 1));
        Assert.That(fixture.Parties.Revision, Is.EqualTo(partyStoreRevisionBefore + 1));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(eventCountBefore + 1));
        Assert.That(GetSequenceWitness(runtime).Revision, Is.EqualTo(sequenceRevisionBefore + 1));
        Assert.That(ReadProtocolEpoch(protocol), Is.EqualTo(epochBefore + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True, failure.ToString());
    }

    [Test]
    public void DirectP12SystemEntryIsRejectedBeforeWritesAndUnboundRuntimeKeepsExistingBehavior()
    {
        TravelPartyFixture guardedFixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime guardedRuntime = CreateDailyRuntime(guardedFixture);
        ContinuationCensusProtocol protocol = GetProtocol(guardedRuntime);
        long epochBefore = ReadProtocolEpoch(protocol);
        float[] balancesBefore = CaptureBalances(guardedFixture);
        long eventCountBefore = guardedFixture.Records.Events.Events.Count;
        long sequenceBefore = GetSequenceWitness(guardedRuntime).Revision;
        long allocatorBefore = GetTravelPartyAllocatorWitness(guardedRuntime).Revision;

        Assert.That(guardedFixture.System.TryStartTravelParty(CreateContext(guardedFixture), out _), Is.False);

        AssertNoStartWrites(guardedFixture, guardedRuntime, balancesBefore, eventCountBefore, sequenceBefore, allocatorBefore);
        Assert.That(ReadProtocolEpoch(protocol), Is.EqualTo(epochBefore));
        Assert.That(guardedRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure healthyFailure), Is.True,
            healthyFailure.ToString());

        TravelPartyFixture unboundFixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime unboundRuntime = new SimulationRuntime(
            unboundFixture.Records.Time,
            new[] { unboundFixture.World.A, unboundFixture.World.B, unboundFixture.World.C },
            unboundFixture.Members,
            economyEnabled: false,
            configuredActions: Array.Empty<NpcActionData>(),
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            travelSystem: unboundFixture.Travel,
            travelPartySystem: unboundFixture.System,
            recordSequence: unboundFixture.Records.Sequence,
            runtimeIdAllocator: unboundFixture.Records.Allocator);
        Assert.That(unboundRuntime.TryStartTravelParty(CreateContext(unboundFixture)), Is.True);
        Assert.That(unboundFixture.Parties.ActiveParties, Has.Count.EqualTo(1));
    }

    [Test]
    public void LaterAccountCommitRejectionRefundsEarlierDebitAndClosesOneStartEpoch()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime runtime = CreateDailyRuntime(fixture);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        long epochBefore = ReadProtocolEpoch(protocol);
        long partyCounterBefore = GetTravelPartyAllocatorWitness(runtime).Revision;
        long eventCounterBefore = GetEventCounterWitness(runtime).Revision;
        long sequenceBefore = GetSequenceWitness(runtime).Revision;
        long storeRevisionBefore = fixture.Parties.Revision;
        float brunoBalanceBefore = fixture.Bruno.MoneyAccount.Balance;
        float caioBalanceBefore = fixture.Caio.MoneyAccount.Balance;
        long brunoAccountRevisionBefore = fixture.Bruno.MoneyAccount.Revision;
        long caioAccountRevisionBefore = fixture.Caio.MoneyAccount.Revision;
        int eventCountBefore = fixture.Records.Events.Events.Count;

        FieldInfo admissionField = typeof(MoneyAccountRuntime).GetField(
            "p12MutationAdmission",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(admissionField, Is.Not.Null);
        Func<bool> originalAdmission = (Func<bool>)admissionField.GetValue(fixture.Caio.MoneyAccount);
        Assert.That(originalAdmission, Is.Not.Null);
        admissionField.SetValue(fixture.Caio.MoneyAccount, (Func<bool>)(() => false));

        bool started;
        try
        {
            started = runtime.TryStartTravelParty(CreateContext(fixture));
        }
        finally
        {
            admissionField.SetValue(fixture.Caio.MoneyAccount, originalAdmission);
        }

        Assert.That(started, Is.False);
        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Parties.Revision, Is.EqualTo(storeRevisionBefore));
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.IsTraveling)).False);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.ActiveTravelPartyId)).Null);
        Assert.That(fixture.Bruno.MoneyAccount.Balance, Is.EqualTo(brunoBalanceBefore));
        Assert.That(fixture.Caio.MoneyAccount.Balance, Is.EqualTo(caioBalanceBefore));
        Assert.That(fixture.Bruno.MoneyAccount.Revision, Is.EqualTo(brunoAccountRevisionBefore + 2));
        Assert.That(fixture.Caio.MoneyAccount.Revision, Is.EqualTo(caioAccountRevisionBefore));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(eventCountBefore));
        Assert.That(GetTravelPartyAllocatorWitness(runtime).Revision, Is.EqualTo(partyCounterBefore + 1));
        Assert.That(GetEventCounterWitness(runtime).Revision, Is.EqualTo(eventCounterBefore));
        Assert.That(GetSequenceWitness(runtime).Revision, Is.EqualTo(sequenceBefore));
        Assert.That(ReadProtocolEpoch(protocol), Is.EqualTo(epochBefore + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True,
            failure.ToString(), "the committed start and compensation are reported under one operation");
        Assert.That(runtime.TryStartTravelParty(CreateContext(fixture)), Is.True,
            "a later supported start is not blocked by a stale operation scope");
    }

    [Test]
    public void EventStoreRejectionPreservesExistingCompensationAndClosesStartScope()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        Assert.That(fixture.Records.EventRecorder.Record((eventId, day, sequence) => new NpcTravelStartedEvent(
            eventId,
            day,
            sequence,
            fixture.Bruno.RuntimeId,
            fixture.World.A.Location.RuntimeId,
            fixture.World.B.Location.RuntimeId,
            fixture.World.RouteAB.RuntimeId)), Is.True);
        SetPrivateLong(fixture.Records.Allocator, "nextEventSequence", 1L);

        SimulationRuntime runtime = CreateDailyRuntime(fixture);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        long epochBefore = ReadProtocolEpoch(protocol);
        long partyCounterBefore = GetTravelPartyAllocatorWitness(runtime).Revision;
        long eventCounterBefore = GetEventCounterWitness(runtime).Revision;
        long sequenceBefore = GetSequenceWitness(runtime).Revision;
        float[] balancesBefore = CaptureBalances(fixture);
        long storeRevisionBefore = fixture.Parties.Revision;

        LogAssert.Expect(LogType.Error, "Cannot record duplicate domain EventId 'event-000001'.");
        Assert.That(runtime.TryStartTravelParty(CreateContext(fixture)), Is.False);

        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Parties.Revision, Is.EqualTo(storeRevisionBefore + 2));
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.IsTraveling)).False);
        Assert.That(CaptureBalances(fixture), Is.EqualTo(balancesBefore));
        Assert.That(GetTravelPartyAllocatorWitness(runtime).Revision, Is.EqualTo(partyCounterBefore + 1));
        Assert.That(GetEventCounterWitness(runtime).Revision, Is.EqualTo(eventCounterBefore + 1));
        Assert.That(GetSequenceWitness(runtime).Revision, Is.EqualTo(sequenceBefore + 1));
        Assert.That(ReadProtocolEpoch(protocol), Is.EqualTo(epochBefore + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterFailure), Is.True,
            afterFailure.ToString(), "the failed domain start still releases its P12 operation scope");
    }

    [Test]
    public void ExceptionAfterPartyIdCommitStillReportsTheIdAndReleasesScope()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime runtime = CreateDailyRuntime(fixture);
        ContinuationCensusProtocol protocol = GetProtocol(runtime);
        long epochBefore = ReadProtocolEpoch(protocol);
        long counterBefore = GetTravelPartyAllocatorWitness(runtime).Revision;
        FieldInfo callbackField = typeof(RuntimeIdAllocator).GetField(
            "p12TravelPartyIdMutationCommitted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Action original = (Action)callbackField.GetValue(fixture.Records.Allocator);
        callbackField.SetValue(fixture.Records.Allocator, (Action)(() =>
        {
            original();
            throw new ApplicationException("injected post-allocation exception");
        }));

        Assert.Throws<ApplicationException>(() => runtime.TryStartTravelParty(CreateContext(fixture)));
        callbackField.SetValue(fixture.Records.Allocator, original);

        Assert.That(GetTravelPartyAllocatorWitness(runtime).Revision, Is.EqualTo(counterBefore + 1));
        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.IsTraveling)).False);
        Assert.That(ReadProtocolEpoch(protocol), Is.EqualTo(epochBefore + 1));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure afterException), Is.True,
            afterException.ToString(), "the wrapper closes the operation scope in finally");
        Assert.That(runtime.TryStartTravelParty(CreateContext(fixture)), Is.True,
            "a later normal start is not blocked by stale request or operation state");
    }

    [TestCase("party-allocator")]
    [TestCase("event-allocator")]
    [TestCase("record-sequence")]
    [TestCase("money-account")]
    [TestCase("spatial-knowledge")]
    [TestCase("npc-travel")]
    [TestCase("city-presence")]
    [TestCase("party-store")]
    public void SaturatedOwnerCapacityRejectsStartWithoutChangingTravelOwners(string owner)
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        switch (owner)
        {
            case "party-allocator":
                SetPrivateLong(fixture.Records.Allocator, "nextTravelPartySequence", long.MaxValue);
                break;
            case "event-allocator":
                SetPrivateLong(fixture.Records.Allocator, "nextEventSequence", long.MaxValue);
                break;
            case "record-sequence":
                SetPrivateLong(fixture.Records.Sequence, "nextSequence", long.MaxValue);
                break;
            case "money-account":
                SetPrivateLong(fixture.Bruno.MoneyAccount, "revision", long.MaxValue - 1L);
                break;
            case "spatial-knowledge":
                SetPrivateLong(fixture.Bruno.SpatialKnowledge, "revision", long.MaxValue - 1L);
                break;
            case "npc-travel":
                SetPrivateLong(fixture.Bruno, "travelStateRevision", long.MaxValue - 3L);
                break;
            case "city-presence":
                SetPrivateLong(fixture.World.A, "importantNpcRevision", long.MaxValue - 1L);
                break;
            case "party-store":
                SetPrivateLong(fixture.Parties, "revision", long.MaxValue - 1L);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(owner));
        }

        SimulationRuntime runtime = CreateDailyRuntime(fixture);
        float[] balancesBefore = CaptureBalances(fixture);
        long eventCountBefore = fixture.Records.Events.Events.Count;
        long partyCountBefore = GetTravelPartyAllocatorWitness(runtime).Revision;
        long eventCounterBefore = GetEventCounterWitness(runtime).Revision;
        long sequenceBefore = GetSequenceWitness(runtime).Revision;
        long storeRevisionBefore = fixture.Parties.Revision;
        bool[] knowledgeLocationsBefore = CaptureLocationKnowledge(fixture);
        bool[] knowledgeRoutesBefore = CaptureRouteKnowledge(fixture);

        Assert.That(runtime.TryStartTravelParty(CreateContext(fixture)), Is.False, owner);

        Assert.That(fixture.Parties.ActiveParties, Is.Empty, owner);
        Assert.That(fixture.Parties.Revision, Is.EqualTo(storeRevisionBefore), owner);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.IsTraveling)).False, owner);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.ActiveTravelPartyId)).Null, owner);
        Assert.That(CaptureBalances(fixture), Is.EqualTo(balancesBefore), owner);
        Assert.That(CaptureLocationKnowledge(fixture), Is.EqualTo(knowledgeLocationsBefore), owner);
        Assert.That(CaptureRouteKnowledge(fixture), Is.EqualTo(knowledgeRoutesBefore), owner);
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(eventCountBefore), owner);
        Assert.That(GetTravelPartyAllocatorWitness(runtime).Revision, Is.EqualTo(partyCountBefore), owner);
        Assert.That(GetEventCounterWitness(runtime).Revision, Is.EqualTo(eventCounterBefore), owner);
        Assert.That(GetSequenceWitness(runtime).Revision, Is.EqualTo(sequenceBefore), owner);
    }

    [Test]
    public void SameIdReplacementOwnerFaultsBeforeAnyTravelStartWrite()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime runtime = CreateDailyRuntime(fixture);
        FieldInfo registryField = typeof(SimulationRuntime).GetField(
            "npcRegistryById",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Dictionary<string, NpcRuntime> registry = (Dictionary<string, NpcRuntime>)registryField.GetValue(runtime);
        NpcRuntime replacement = new NpcRuntime(
            fixture.Bruno.RuntimeId,
            SimulationTestFactory.CreateNpc("same-id-replacement", NpcJobType.Merchant),
            fixture.World.A,
            100f);
        registry[fixture.Bruno.RuntimeId] = replacement;
        long counterBefore = GetTravelPartyAllocatorWitness(runtime).Revision;
        long storeRevisionBefore = fixture.Parties.Revision;
        float[] balancesBefore = CaptureBalances(fixture);

        Assert.Throws<InvalidOperationException>(() => runtime.TryStartTravelParty(CreateContext(fixture)));

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure ownerFailure), Is.False);
        Assert.That(ownerFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(GetTravelPartyAllocatorWitness(runtime).Revision, Is.EqualTo(counterBefore));
        Assert.That(fixture.Parties.Revision, Is.EqualTo(storeRevisionBefore));
        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.IsTraveling)).False);
        Assert.That(CaptureBalances(fixture), Is.EqualTo(balancesBefore));
    }

    [Test]
    public void WrongThreadStartFailsClosedBeforeTravelPartyAllocation()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime runtime = CreateDailyRuntime(fixture);
        long counterBefore = GetTravelPartyAllocatorWitness(runtime).Revision;
        bool result = true;
        Exception threadFailure = null;
        Thread thread = new Thread(() =>
        {
            try { result = runtime.TryStartTravelParty(CreateContext(fixture)); }
            catch (Exception exception)
            {
                result = false;
                threadFailure = exception;
            }
        });
        thread.Start();
        Assert.That(thread.Join(TimeSpan.FromSeconds(5)), Is.True);

        Assert.That(result, Is.False);
        Assert.That(threadFailure, Is.TypeOf<InvalidOperationException>());
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure threadFailureState), Is.False);
        Assert.That(threadFailureState, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(GetTravelPartyAllocatorWitness(runtime).Revision, Is.EqualTo(counterBefore));
        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.IsTraveling)).False);
    }

    private static SimulationRuntime CreateDailyRuntime(TravelPartyFixture fixture)
    {
        return new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.World.A, fixture.World.B, fixture.World.C },
            fixture.Members,
            economyEnabled: false,
            configuredActions: Array.Empty<NpcActionData>(),
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.System,
            recordSequence: fixture.Records.Sequence,
            runtimeIdAllocator: fixture.Records.Allocator,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static ActionExecutionContext CreateContext(TravelPartyFixture fixture)
    {
        return new ActionExecutionContext("p12-travel-party-start", new[]
        {
            new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
            new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer),
            new ActionExecutionParticipant(fixture.Marta.RuntimeId, ActionExecutionParticipantRole.Support)
        }, fixture.World.B.Location.RuntimeId, fixture.World.RouteAB.RuntimeId);
    }

    private static ContinuationCensusProtocol GetProtocol(SimulationRuntime runtime)
    {
        return (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
    }

    private static OwnerSectionCensusWitness GetWitness(
        SimulationRuntime runtime,
        string providerFieldName)
    {
        IOwnerSectionCensusProvider provider = (IOwnerSectionCensusProvider)typeof(SimulationRuntime)
            .GetField(providerFieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        Assert.That(provider, Is.Not.Null, providerFieldName);
        return provider.GetCurrentCensus();
    }

    private static OwnerSectionCensusWitness GetTravelPartyAllocatorWitness(SimulationRuntime runtime)
        => GetWitness(runtime, "runtimeIdAllocatorTravelPartyCounterCensusProvider");

    private static OwnerSectionCensusWitness GetEventCounterWitness(SimulationRuntime runtime)
        => GetWitness(runtime, "runtimeIdAllocatorEventCounterCensusProvider");

    private static OwnerSectionCensusWitness GetSequenceWitness(SimulationRuntime runtime)
        => GetWitness(runtime, "simulationRecordSequenceCensusProvider");

    private static long ReadProtocolEpoch(ContinuationCensusProtocol protocol)
    {
        return (long)typeof(ContinuationCensusProtocol)
            .GetField("mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(protocol);
    }

    private static float[] CaptureBalances(TravelPartyFixture fixture)
    {
        float[] balances = new float[fixture.Members.Count];
        for (int i = 0; i < fixture.Members.Count; i++) balances[i] = fixture.Members[i].MoneyAccount.Balance;
        return balances;
    }

    private static bool[] CaptureLocationKnowledge(TravelPartyFixture fixture)
    {
        bool[] values = new bool[fixture.Members.Count];
        for (int i = 0; i < fixture.Members.Count; i++)
            values[i] = fixture.Members[i].SpatialKnowledge.KnowsLocation(fixture.World.A.Location.RuntimeId);
        return values;
    }

    private static bool[] CaptureRouteKnowledge(TravelPartyFixture fixture)
    {
        bool[] values = new bool[fixture.Members.Count];
        for (int i = 0; i < fixture.Members.Count; i++)
            values[i] = fixture.Members[i].SpatialKnowledge.KnowsRoute(fixture.World.RouteAB.RuntimeId);
        return values;
    }

    private static void AssertNoStartWrites(
        TravelPartyFixture fixture,
        SimulationRuntime runtime,
        float[] balancesBefore,
        long eventCountBefore,
        long sequenceBefore,
        long partyCounterBefore)
    {
        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Parties.Revision, Is.Zero);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.IsTraveling)).False);
        Assert.That(fixture.Members, Has.All.Property(nameof(NpcRuntime.ActiveTravelPartyId)).Null);
        Assert.That(CaptureBalances(fixture), Is.EqualTo(balancesBefore));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(eventCountBefore));
        Assert.That(GetSequenceWitness(runtime).Revision, Is.EqualTo(sequenceBefore));
        Assert.That(GetTravelPartyAllocatorWitness(runtime).Revision, Is.EqualTo(partyCounterBefore));
    }

    private static void SetPrivateLong(object owner, string fieldName, long value)
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, owner.GetType().Name + "." + fieldName);
        field.SetValue(owner, value);
    }
}
