using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class ExpeditionTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void RuntimeIdentityAllocator_UsesIndependentExpeditionNamespace()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(allocator.AllocateExpeditionId(), Is.EqualTo("expedition-000001"));
        Assert.That(allocator.AllocateExpeditionId(), Is.EqualTo("expedition-000002"));
        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-000001"));
    }

    [Test]
    public void ExpeditionRuntimeRequiresStableIdentityAndTargetData()
    {
        Assert.Throws<ArgumentException>(() => CreateRuntime(null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", targetSiteRuntimeId: null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", originLocationRuntimeId: null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", targetLocationRuntimeId: null));
        Assert.Throws<ArgumentException>(() => CreateRuntime("expedition-1", outboundRouteRuntimeId: null));
    }

    [Test]
    public void ExpeditionRuntimeRequiresPerformersAndUniqueRoleSnapshots()
    {
        Assert.Throws<ArgumentException>(() => new ExpeditionRuntime(
            "expedition-1",
            "site-1",
            "location-a",
            "location-site",
            "route-a-site",
            null,
            null,
            new[] { "npc-support" },
            Array.Empty<string>(),
            new[] { "npc-support" }));

        Assert.Throws<ArgumentException>(() => new ExpeditionRuntime(
            "expedition-1",
            "site-1",
            "location-a",
            "location-site",
            "route-a-site",
            null,
            null,
            new[] { "npc-performer", "npc-performer" },
            new[] { "npc-performer" },
            Array.Empty<string>()));

        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-1",
            "site-1",
            "location-a",
            "location-site",
            "route-a-site",
            null,
            null,
            new[] { "npc-performer", "npc-support" },
            new[] { "npc-performer" },
            new[] { "npc-support" });

        Assert.That((expedition.MemberRuntimeIds as IList<string>)?.IsReadOnly, Is.True);
        Assert.That((expedition.PerformerRuntimeIds as IList<string>)?.IsReadOnly, Is.True);
        Assert.That((expedition.SupportRuntimeIds as IList<string>)?.IsReadOnly, Is.True);
    }

    [Test]
    public void ExpeditionSystemRejectsSplitTravelPartyStore()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        CreateSystem(fixture);
        TravelPartySystem partySystem = fixture.TravelPartySystem;

        Assert.Throws<ArgumentException>(() => new ExpeditionSystem(
            new ExpeditionStore(),
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Sites,
            partySystem,
            new TravelPartyStore(),
            fixture.Knowledge,
            fixture.Records.Time,
            fixture.Records.EventRecorder));
    }

    [Test]
    public void ReturnAssociationFailureCompensatesPartyStoreAtSaturationBoundary()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("return-race-performer", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        performer.SpatialKnowledge.DiscoverRoute(fixture.SiteToCityRoute.RuntimeId);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);
        Assert.That(system.TryStartExpedition(fixture.Site, CreateContext(fixture, performer).ActionContext, out ExpeditionRuntime expedition), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { performer },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);
        runtime.AdvanceDays(fixture.SiteRoute.TravelDays + 1);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));

        SetTravelPartyRevision(fixture.TravelParties, long.MaxValue - 2);
        List<ActionExecutionParticipant> participants = new List<ActionExecutionParticipant>
        {
            new ActionExecutionParticipant(performer.RuntimeId, ActionExecutionParticipantRole.Performer)
        };
        ActionExecutionContext returnContext = new ActionExecutionContext(
            "return-" + expedition.ExpeditionId,
            participants,
            expedition.OriginLocationRuntimeId,
            fixture.SiteToCityRoute.RuntimeId,
            expedition.OriginDecisionId);

        // No callback exists between the real nested party start and return-party
        // association. Hold the same reentrant owner window, execute those real
        // operations stepwise, and force the documented public lifecycle failure
        // at the precise boundary without scheduler timing or a production hook.
        using (EnterTravelPartyMutationWindow(fixture.TravelParties))
        {
            Assert.That(fixture.TravelPartySystem.CanPlanKnownGroupTravel(returnContext, out _), Is.True);
            Assert.That(expedition.TryBeginReturn(), Is.True);
            Assert.That(fixture.TravelPartySystem.TryStartTravelParty(returnContext, out TravelPartyRuntime returnParty), Is.True);
            Assert.That(fixture.TravelParties.ActiveParties, Has.Count.EqualTo(1));
            Assert.That(fixture.TravelParties.Revision, Is.EqualTo(long.MaxValue - 1));

            Assert.That(expedition.TryComplete(), Is.True);
            bool associated = (bool)typeof(ExpeditionRuntime)
                .GetMethod("TryBeginReturnTravel", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(expedition, new object[] { returnParty.TravelPartyId });
            Assert.That(associated, Is.False);

            typeof(ExpeditionRuntime).GetMethod("TryCancelReturn", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(expedition, null);
            Assert.That(fixture.TravelParties.Remove(returnParty.TravelPartyId), Is.True);
        }

        // This asserts only TravelParty Add+Remove compensation, not cross-owner
        // rollback for Expedition, NPC travel state, or member costs.
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Completed));
        Assert.That(fixture.TravelParties.ActiveParties, Is.Empty);
        Assert.That(fixture.TravelParties.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void ExpeditionStorePreservesOrderAndRejectsDuplicatesAndOverlappingMembers()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime first = CreateRuntime("expedition-1", memberRuntimeId: "npc-1");
        ExpeditionRuntime duplicate = CreateRuntime("expedition-1", memberRuntimeId: "npc-2");
        ExpeditionRuntime overlapping = CreateRuntime("expedition-2", memberRuntimeId: "npc-1");

        Assert.That(store.Add(first), Is.True);
        Assert.That(store.Add(duplicate), Is.False);
        Assert.That(store.Add(overlapping), Is.False);
        Assert.That(store.ActiveExpeditions.Count, Is.EqualTo(1));
        Assert.That(store.ActiveExpeditions[0], Is.SameAs(first));
        Assert.That(store.TryGetExpeditionForNpc("npc-1", out ExpeditionRuntime found), Is.True);
        Assert.That(found, Is.SameAs(first));
        Assert.That(store.TryGetExpeditionForNpc("npc-unknown", out _), Is.False);
    }

    [Test]
    public void ExpeditionStorePublishesPassiveRevisionAndExactCompletionWitness()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionCensusProvider provider = new ExpeditionCensusProvider(store);
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-census", "site-1", "location-a", "location-site", "route-a-site", "party-1", null,
            new[] { "npc-1" }, new[] { "npc-1" }, Array.Empty<string>(), ExpeditionState.Returning);

        OwnerSectionCensusWitness empty = provider.GetCurrentCensus();
        Assert.That(empty.SectionId, Is.EqualTo(ExpeditionCensusProvider.SectionId));
        Assert.That(empty.SchemaVersion, Is.EqualTo(1));
        Assert.That(empty.OwnerInstanceIdentity, Is.SameAs(store));
        Assert.That(empty.Cardinality, Is.Zero);
        Assert.That(empty.Revision, Is.Zero);

        Assert.That(store.Add(expedition), Is.True);
        Assert.That(provider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(1));
        Assert.That(expedition.TryComplete(), Is.True);
        OwnerSectionCensusWitness complete = provider.GetCurrentCensus();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Completed));
        Assert.That(complete.Cardinality, Is.Zero);
        Assert.That(complete.Revision, Is.EqualTo(2));
        Assert.That(store.GetById(expedition.ExpeditionId), Is.Null);
        Assert.That(expedition.TryComplete(), Is.False);
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(2));
    }

    [Test]
    public void AttachedExpeditionLeafMutationAdvancesExactlyOnce()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "expedition-leaf", "site-1", "location-a", "location-site", "route-a-site", null, null,
            new[] { "npc-1" }, new[] { "npc-1" }, Array.Empty<string>(), ExpeditionState.AtSite,
            ExpeditionObjectiveRuntime.Explore(2));
        Assert.That(store.Add(expedition), Is.True);
        Assert.That(store.Revision, Is.EqualTo(1));
        Assert.That(expedition.TryBeginExploration(), Is.True);
        Assert.That(store.Revision, Is.EqualTo(2));
        Assert.That(expedition.TrySetCurrentLocalPlace("place-1", out bool firstVisit), Is.True);
        Assert.That(firstVisit, Is.True);
        Assert.That(store.Revision, Is.EqualTo(3));
        Assert.That(expedition.TrySetCurrentLocalPlace("place-1", out firstVisit), Is.True);
        Assert.That(firstVisit, Is.False);
        Assert.That(store.Revision, Is.EqualTo(3));
    }

    [Test]
    public void ExpeditionStartReservationFencesTheExactOwnerAndProtectsItsReservedWrite()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionCensusProvider provider = new ExpeditionCensusProvider(store);
        ExpeditionRuntime expedition = CreateRuntime("reserved-start", memberRuntimeId: "npc-reserved-start");

        SetExpeditionRevision(store, long.MaxValue - 1);
        Assert.That(TryReserve(store, "TryReserveNew", expedition, out _), Is.False);
        Assert.That(store.Revision, Is.EqualTo(long.MaxValue - 1));
        Assert.That(store.ActiveExpeditions, Is.Empty);

        SetExpeditionRevision(store, long.MaxValue - 2);
        Assert.That(TryReserve(store, "TryReserveNew", expedition, out object reservation), Is.True);
        using ((IDisposable)reservation)
        {
            Assert.That(InvokeStoreBoolean(store, "AddAndFence", reservation), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue - 1));
            Assert.That(provider.GetCurrentCensus().Cardinality, Is.EqualTo(1));

            Assert.That(store.Remove(expedition.ExpeditionId), Is.False);
            Assert.That(InvokeRuntimeBoolean(expedition, "TryBeginTravel", "party-reserved-start"), Is.False);
            Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Preparing));
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue - 1));

            ExpeditionRuntime unrelated = CreateRuntime("unrelated-reserved", memberRuntimeId: "npc-unrelated");
            Assert.That(store.Add(unrelated), Is.False, "an unrelated write must not consume the reserved final revision");
            Assert.That(store.ActiveExpeditions, Has.Count.EqualTo(1));

            Assert.That(CommitReserved(store, reservation,
                () => InvokeRuntimeBoolean(expedition, "TryBeginTravelCore", "party-reserved-start")), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue));
            Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        }

        Assert.That(provider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void ExpeditionReturnReservationFencesCompletionAndConsumesOnlyItsReservedCommit()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionCensusProvider provider = new ExpeditionCensusProvider(store);
        ExpeditionRuntime expedition = CreateActiveAtSiteExpedition("reserved-return", "npc-reserved-return");
        Assert.That(store.Add(expedition), Is.True);

        SetExpeditionRevision(store, long.MaxValue - 1);
        Assert.That(TryReserve(store, "TryReserveExisting", expedition, out _), Is.False);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        Assert.That(store.Revision, Is.EqualTo(long.MaxValue - 1));

        SetExpeditionRevision(store, long.MaxValue - 2);
        Assert.That(TryReserve(store, "TryReserveExisting", expedition, out object reservation), Is.True);
        using ((IDisposable)reservation)
        {
            Assert.That(CommitReserved(store, reservation,
                () => InvokeRuntimeBoolean(expedition, "TryBeginReturnCore")), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue - 1));
            Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));

            Assert.That(expedition.TryComplete(), Is.False);
            Assert.That(InvokeRuntimeBoolean(expedition, "TryBeginReturnTravel", "party-blocked"), Is.False);
            Assert.That(store.GetById(expedition.ExpeditionId), Is.SameAs(expedition));
            Assert.That(provider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
            Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue - 1));

            Assert.That(CommitReserved(store, reservation,
                () => InvokeRuntimeBoolean(expedition, "TryBeginReturnTravelCore", "party-return")), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue));
            Assert.That(expedition.TravelPartyId, Is.EqualTo("party-return"));
        }

        // Reset only the test revision after proving the reserved maximum. This
        // isolates fence release from the independent saturation check above.
        SetExpeditionRevision(store, long.MaxValue - 1);
        Assert.That(expedition.TryComplete(), Is.True);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Completed));
        Assert.That(store.GetById(expedition.ExpeditionId), Is.Null);
    }

    [Test]
    public void ObjectiveReservationBlocksLifecycleWritesAndReleasesOnFailure()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime expedition = CreateActiveAtSiteExpedition(
            "reserved-objective", "npc-reserved-objective", ExpeditionObjectiveRuntime.Retrieve("medicine"));
        Assert.That(store.Add(expedition), Is.True);
        long before = store.Revision;

        Assert.That(TryReserve(store, "TryReserveObjectiveCompletion", expedition, out object reservation), Is.True);
        using ((IDisposable)reservation)
        {
            Assert.That(expedition.TryBeginExploration(), Is.False);
            Assert.That(expedition.TryBeginReturn(), Is.False);
            Assert.That(expedition.TryComplete(), Is.False);
            Assert.That(expedition.IsObjectiveComplete, Is.False);
            Assert.That(store.Revision, Is.EqualTo(before));
        }

        Assert.That(expedition.TryBeginExploration(), Is.True, "disposing a failed external-operation reservation must release its fence");
        Assert.That(store.Revision, Is.EqualTo(before + 1));
    }

    [Test]
    public void ExpeditionStartRequiresTwoReservedRevisionsBeforeTravelAndNeverWraps()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("reserved-start-flow", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);
        float originalMoney = performer.Money;
        long partyRevision = fixture.TravelParties.Revision;
        long eventCount = fixture.Records.Events.Events.Count;

        SetExpeditionRevision(system.Store, long.MaxValue - 1);
        Assert.That(system.TryStartExpedition(fixture.Site, CreateContext(fixture, performer).ActionContext, out _), Is.False);
        Assert.That(system.Store.Revision, Is.EqualTo(long.MaxValue - 1));
        Assert.That(system.Store.ActiveExpeditions, Is.Empty);
        Assert.That(fixture.TravelParties.ActiveParties, Is.Empty);
        Assert.That(fixture.TravelParties.Revision, Is.EqualTo(partyRevision));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(performer.IsTraveling, Is.False);
        Assert.That(performer.Money, Is.EqualTo(originalMoney));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(eventCount));

        SetExpeditionRevision(system.Store, long.MaxValue - 2);
        Assert.That(system.TryStartExpedition(fixture.Site, CreateContext(fixture, performer).ActionContext, out ExpeditionRuntime started), Is.True);
        Assert.That(system.Store.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(started.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        Assert.That(system.Store.GetById(started.ExpeditionId), Is.SameAs(started));
        Assert.That(fixture.TravelParties.ActiveParties, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExpeditionReturnRequiresTwoReservedRevisionsBeforeCreatingReturnParty()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("reserved-return-flow", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        performer.SpatialKnowledge.DiscoverRoute(fixture.SiteToCityRoute.RuntimeId);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);
        Assert.That(system.TryStartExpedition(fixture.Site, CreateContext(fixture, performer).ActionContext, out ExpeditionRuntime expedition), Is.True);

        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { performer },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);
        runtime.AdvanceDays(fixture.SiteRoute.TravelDays + 1);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));

        float moneyBeforeReturn = performer.Money;
        long partyRevision = fixture.TravelParties.Revision;
        int partyCount = fixture.TravelParties.ActiveParties.Count;
        int eventCount = fixture.Records.Events.Events.Count;
        SetExpeditionRevision(system.Store, long.MaxValue - 1);

        Assert.That(system.TryBeginReturn(expedition, out _), Is.False);
        Assert.That(system.Store.Revision, Is.EqualTo(long.MaxValue - 1));
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        Assert.That(fixture.TravelParties.ActiveParties.Count, Is.EqualTo(partyCount));
        Assert.That(fixture.TravelParties.Revision, Is.EqualTo(partyRevision));
        Assert.That(performer.IsTraveling, Is.False);
        Assert.That(performer.Money, Is.EqualTo(moneyBeforeReturn));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(eventCount));

        SetExpeditionRevision(system.Store, long.MaxValue - 2);
        Assert.That(system.TryBeginReturn(expedition, out _), Is.True);
        Assert.That(system.Store.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(fixture.TravelParties.ActiveParties, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExpeditionStartCompensationConsumesTheReservedSecondRevision()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime expedition = CreateRuntime("reserved-compensation", memberRuntimeId: "npc-compensation");
        SetExpeditionRevision(store, long.MaxValue - 2);
        Assert.That(TryReserve(store, "TryReserveNew", expedition, out object reservation), Is.True);

        using ((IDisposable)reservation)
        {
            Assert.That(InvokeStoreBoolean(store, "AddAndFence", reservation), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue - 1));
            Assert.That(InvokeStoreBoolean(store, "RemoveReserved", reservation), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue));
            Assert.That(store.GetById(expedition.ExpeditionId), Is.Null);
            Assert.That(store.ActiveExpeditions, Is.Empty);
        }
    }

    [Test]
    public void ExpeditionReturnCompensationConsumesTheReservedSecondRevision()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime expedition = CreateActiveAtSiteExpedition("reserved-return-compensation", "npc-return-compensation");
        Assert.That(store.Add(expedition), Is.True);
        SetExpeditionRevision(store, long.MaxValue - 2);
        Assert.That(TryReserve(store, "TryReserveExisting", expedition, out object reservation), Is.True);

        using ((IDisposable)reservation)
        {
            Assert.That(CommitReserved(store, reservation,
                () => InvokeRuntimeBoolean(expedition, "TryBeginReturnCore")), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue - 1));
            Assert.That(CommitReserved(store, reservation,
                () => InvokeRuntimeBoolean(expedition, "TryCancelReturnCore")), Is.True);
            Assert.That(store.Revision, Is.EqualTo(long.MaxValue));
            Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
            Assert.That(store.GetById(expedition.ExpeditionId), Is.SameAs(expedition));
        }
    }

    [Test]
    public void ExactExpeditionCompletionRejectsImpostorsAndSaturationWithoutPartialMutation()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime expedition = CreateActiveAtSiteExpedition(
            "exact-completion", "npc-exact-completion", state: ExpeditionState.Returning);
        Assert.That(store.Add(expedition), Is.True);
        ExpeditionRuntime impostor = CreateActiveAtSiteExpedition(
            "exact-completion", "npc-impostor", state: ExpeditionState.Returning);

        Assert.That(store.TryFinalizeCompletion(impostor), Is.False);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(store.GetById(expedition.ExpeditionId), Is.SameAs(expedition));

        SetExpeditionRevision(store, long.MaxValue);
        Assert.That(expedition.TryComplete(), Is.False);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(store.GetById(expedition.ExpeditionId), Is.SameAs(expedition));
        Assert.That(store.ActiveExpeditions, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExpeditionOwnerReadWindowSerializesMutationFromAnotherThread()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionCensusProvider provider = new ExpeditionCensusProvider(store);
        ExpeditionRuntime expedition = CreateActiveAtSiteExpedition("read-window", "npc-read-window");
        Assert.That(store.Add(expedition), Is.True);
        object readWindow = InvokeStoreObject(store, "EnterReadWindow");
        ManualResetEventSlim attempting = new ManualResetEventSlim(false);
        ManualResetEventSlim finished = new ManualResetEventSlim(false);
        bool mutationResult = false;
        Exception workerFailure = null;
        Thread writer = new Thread(() =>
        {
            attempting.Set();
            try
            {
                mutationResult = expedition.TryBeginExploration();
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
            finally
            {
                finished.Set();
            }
        });
        writer.IsBackground = true;

        using ((IDisposable)readWindow)
        {
            writer.Start();
            Assert.That(attempting.Wait(TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(SpinWait.SpinUntil(
                () => (writer.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(2)), Is.True, "the mutation thread should wait on the serialized owner window");
            Assert.That(finished.IsSet, Is.False);
            Assert.That(store.Revision, Is.EqualTo(1));
        }

        Assert.That(writer.Join(TimeSpan.FromSeconds(2)), Is.True);
        if (workerFailure != null) throw workerFailure;
        Assert.That(mutationResult, Is.True);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Exploring));
        Assert.That(provider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(2));
        attempting.Dispose();
        finished.Dispose();
    }

    [Test]
    public void ObjectiveReservationCommitsOnceThenAllowsTheNextLifecycleWrite()
    {
        ExpeditionStore store = new ExpeditionStore();
        ExpeditionRuntime expedition = CreateActiveAtSiteExpedition(
            "objective-commit", "npc-objective-commit", ExpeditionObjectiveRuntime.Retrieve("medicine"));
        Assert.That(store.Add(expedition), Is.True);
        Assert.That(TryReserve(store, "TryReserveObjectiveCompletion", expedition, out object reservation), Is.True);

        using ((IDisposable)reservation)
        {
            Assert.That(CommitReserved(store, reservation,
                () => InvokeRuntimeBoolean(expedition, "TryMarkObjectiveCompleteCore")), Is.True);
            Assert.That(expedition.IsObjectiveComplete, Is.True);
            Assert.That(store.Revision, Is.EqualTo(2));
            Assert.That(expedition.TryBeginReturn(), Is.False, "the matching token still fences the object until its external operation exits");
        }

        Assert.That(expedition.TryBeginReturn(), Is.True);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.Returning));
        Assert.That(store.Revision, Is.EqualTo(3));
    }

    [Test]
    public void SaturatedRetrieveObjectiveRejectsBeforePlaceContentOrInventoryChanges()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("retrieve-capacity", fixture.CityA, 10f);
        ItemData item = SimulationTestFactory.CreateItem("retrieve-capacity-item");
        PlaceContentStore content = new PlaceContentStore(fixture.Records.Allocator, fixture.IdentityRegistry);
        Assert.That(content.TryAddStack(
            fixture.Site,
            item,
            1,
            PlaceContentPersistencePolicy.Durable,
            out _), Is.True);
        ExpeditionSystem system = CreateSystem(fixture, placeContentStore: content);
        ExpeditionRuntime expedition = new ExpeditionRuntime(
            "retrieve-capacity-expedition",
            fixture.Site.RuntimeId,
            fixture.CityA.Location.RuntimeId,
            fixture.Site.Location.RuntimeId,
            fixture.SiteRoute.RuntimeId,
            null,
            null,
            new[] { performer.RuntimeId },
            new[] { performer.RuntimeId },
            Array.Empty<string>(),
            ExpeditionState.Exploring,
            ExpeditionObjectiveRuntime.Retrieve(item.DefinitionId));
        Assert.That(system.Store.Add(expedition), Is.True);
        SetExpeditionRevision(system.Store, long.MaxValue);
        Assert.That(content.TryGet(fixture.Site, out PlaceContentRuntime beforeContent), Is.True);
        int beforePlaceAmount = beforeContent.GetAmount(item);
        int beforeInventoryAmount = performer.Inventory.GetAmount(item);
        int beforeEvents = fixture.Records.Events.Events.Count;

        Assert.That(system.TryRetrieveTargetResource(
            expedition,
            PlaceContentOwnerReference.ForExplorableSite(fixture.Site),
            item,
            1,
            out string reason), Is.False);
        Assert.That(reason, Does.Contain("busy or revision capacity"));
        Assert.That(content.TryGet(fixture.Site, out PlaceContentRuntime afterContent), Is.True);
        Assert.That(afterContent.GetAmount(item), Is.EqualTo(beforePlaceAmount));
        Assert.That(performer.Inventory.GetAmount(item), Is.EqualTo(beforeInventoryAmount));
        Assert.That(expedition.IsObjectiveComplete, Is.False);
        Assert.That(system.Store.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(fixture.Records.Events.Events.Count, Is.EqualTo(beforeEvents));
    }

    [Test]
    public void ExpeditionStartRequiresTargetSiteRouteAndParticipantPresenceTruth()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("expedition-performer", fixture.CityA, 10f);
        ExpeditionTestContext context = CreateContext(fixture, performer);
        ExpeditionSystem system = CreateSystem(fixture);

        Assert.That(system.TryStartExpedition(null, context.ActionContext, out _), Is.False);
        Assert.That(system.TryStartExpedition(fixture.Site, new ActionExecutionContext(
            "expedition",
            context.ActionContext.Participants,
            fixture.CityB.Location.RuntimeId,
            fixture.SiteRoute.RuntimeId), out _), Is.False);

        NpcRuntime elsewhere = fixture.CreateNpc("expedition-elsewhere", fixture.CityB, 10f);
        ActionExecutionContext differentOrigin = CreateContext(
            fixture,
            performer,
            elsewhere).ActionContext;
        Assert.That(system.TryStartExpedition(fixture.Site, differentOrigin, out _), Is.False);
    }

    [Test]
    public void ExpeditionPerformerMustKnowSiteAndAllMembersMustKnowRoute()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("knowledge-performer", fixture.CityA, 10f);
        ExpeditionSystem system = CreateSystem(fixture);
        ExpeditionTestContext context = CreateContext(fixture, performer);

        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        Assert.That(system.TryStartExpedition(fixture.Site, context.ActionContext, out _), Is.False);

        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site, fixture.Records.Time.AbsoluteDay);
        Assert.That(system.TryStartExpedition(fixture.Site, context.ActionContext, out ExpeditionRuntime expedition), Is.True);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
    }

    [Test]
    public void ExpeditionSupportMayLackSemanticSiteKnowledgeButNeedsSpatialKnowledge()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("supported-performer", fixture.CityA, 10f);
        NpcRuntime support = fixture.CreateNpc("supported-support", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        AddSpatialKnowledge(support, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site, fixture.Records.Time.AbsoluteDay);
        ExpeditionSystem system = CreateSystem(fixture);

        ExpeditionTestContext context = CreateContext(fixture, performer, support);
        Assert.That(system.TryStartExpedition(fixture.Site, context.ActionContext, out ExpeditionRuntime expedition), Is.True);
        Assert.That(support.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.False);
        Assert.That(expedition.SupportRuntimeIds.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExpeditionStartIsAtomicWhenTravelMoneyIsInsufficient()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("poor-performer", fixture.CityA, 1f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        TravelPartyStore parties = new TravelPartyStore();
        ExpeditionSystem system = CreateSystem(fixture, parties);
        float originalMoney = performer.Money;
        SetExpeditionRevision(system.Store, long.MaxValue - 2);

        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(fixture, performer).ActionContext,
            out _), Is.False);
        Assert.That(system.Store.ActiveExpeditions.Count, Is.EqualTo(0));
        Assert.That(parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(performer.CurrentCity, Is.SameAs(fixture.CityA));
        Assert.That(performer.Money, Is.EqualTo(originalMoney));
        Assert.That(performer.DestinationLocation, Is.Null);
        Assert.That(system.Store.ActiveExpeditions, Is.Empty);
    }

    [Test]
    public void SuccessfulExpeditionCreatesTravelPartyAndStartedEventExactlyOnce()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("started-performer", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);

        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(fixture, performer, originDecisionId: "decision-expedition").ActionContext,
            out ExpeditionRuntime expedition), Is.True);

        SimulationInvariantValidator.ValidateExpedition(expedition, fixture.IdentityRegistry);
        Assert.That(expedition.ExpeditionId, Is.EqualTo("expedition-000001"));
        Assert.That(expedition.TravelPartyId, Is.Not.Null.And.Not.Empty);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        Assert.That(fixture.TravelParties.ActiveParties.Count, Is.EqualTo(1));
        Assert.That(CountEvents<ExpeditionStartedEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(CountEvents<TravelPartyStartedEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(CountEventsWithDecision(fixture.Records.Events.Events, "decision-expedition"), Is.EqualTo(2));
        Assert.That(performer.Money, Is.EqualTo(8f));
        Assert.That(fixture.IdentityRegistry.TryGetNpc(expedition.ExpeditionId, out _), Is.False);
    }

    [Test]
    public void ExpeditionRemainsTravelingUntilAllMembersArriveAndThenBecomesAtSite()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("lifecycle-performer", fixture.CityA, 10f);
        NpcRuntime support = fixture.CreateNpc("lifecycle-support", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        AddSpatialKnowledge(support, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        TravelPartyStore parties = new TravelPartyStore();
        ExpeditionSystem system = CreateSystem(fixture, parties);
        ExpeditionTestContext expeditionContext = CreateContext(fixture, performer, support);

        Assert.That(system.TryStartExpedition(fixture.Site, expeditionContext.ActionContext, out ExpeditionRuntime expedition), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { performer, support },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);

        runtime.AdvanceDay();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        runtime.AdvanceDay();
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        runtime.AdvanceDay();

        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        Assert.That(parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(support.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(support.CurrentCity, Is.Null);
        Assert.That(performer.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.True);
        Assert.That(support.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.True);
        Assert.That(CountEvents<ExpeditionArrivedAtSiteEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(CountEvents<TravelPartyArrivedEvent>(fixture.Records.Events.Events), Is.EqualTo(1));
        Assert.That(fixture.Site.DefinitionId, Is.EqualTo("spatial-site"));
    }

    [Test]
    public void AtSiteExpeditionMembersDoNotTakeAutonomousActionsOrAutoReturn()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("suppressed-performer", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);
        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(fixture, performer).ActionContext,
            out ExpeditionRuntime expedition), Is.True);
        NpcActionData action = SimulationTestFactory.CreateAction("should-not-run", NpcActionType.Normal);
        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { performer },
            economyEnabled: false,
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            decisionRecorder: fixture.Records.DecisionRecorder,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);

        runtime.AdvanceDays(3);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        int decisionsAfterArrival = fixture.Records.Decisions.Decisions.Count;
        runtime.AdvanceDay();

        Assert.That(fixture.Records.Decisions.Decisions.Count, Is.EqualTo(decisionsAfterArrival));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
    }

    [Test]
    public void AtSiteExpeditionMemberCannotStartExternalTravelParty()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("guarded-performer", fixture.CityA, 10f);
        AddSpatialKnowledge(performer, fixture, includeRoute: true);
        performer.SpatialKnowledge.DiscoverRoute(fixture.SiteToCityRoute.RuntimeId);
        fixture.Knowledge.RecordInitialScenarioKnowledge(performer, fixture.Site);
        ExpeditionSystem system = CreateSystem(fixture);

        Assert.That(system.TryStartExpedition(
            fixture.Site,
            CreateContext(fixture, performer).ActionContext,
            out ExpeditionRuntime expedition), Is.True);

        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { performer },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            travelPartySystem: fixture.TravelPartySystem,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: fixture.Knowledge,
            expeditionSystem: system);

        runtime.AdvanceDays(fixture.SiteRoute.TravelDays + 1);

        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        Assert.That(performer.IsTraveling, Is.False);
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(fixture.TravelParties.ActiveParties.Count, Is.EqualTo(0));

        ActionExecutionContext externalTravel = new ActionExecutionContext(
            "external-site-to-city-travel",
            new[]
            {
                new ActionExecutionParticipant(performer.RuntimeId, ActionExecutionParticipantRole.Performer)
            },
            fixture.CityA.Location.RuntimeId,
            fixture.SiteToCityRoute.RuntimeId);

        Assert.That(
            fixture.TravelPartySystem.CanPlanKnownGroupTravel(externalTravel, out string reason),
            Is.True,
            reason);
        Assert.That(runtime.TryStartTravelParty(externalTravel), Is.False);

        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.AtSite));
        Assert.That(performer.IsTraveling, Is.False);
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(fixture.TravelParties.ActiveParties.Count, Is.EqualTo(0));
    }

    private static ExpeditionSystem CreateSystem(
        SpatialTravelFixture fixture,
        TravelPartyStore parties = null,
        PlaceContentStore placeContentStore = null)
    {
        parties = parties ?? new TravelPartyStore();
        TravelPartySystem partySystem = new TravelPartySystem(
            parties,
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Travel,
            fixture.Records.Time,
            fixture.Records.Sequence,
            fixture.Records.EventRecorder);
        fixture.SetTravelPartySystem(partySystem);
        return new ExpeditionSystem(
            new ExpeditionStore(),
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Sites,
            partySystem,
            parties,
            fixture.Knowledge,
            fixture.Records.Time,
            fixture.Records.EventRecorder,
            placeContentStore: placeContentStore);
    }

    private static void SetTravelPartyRevision(TravelPartyStore store, long revision)
    {
        typeof(TravelPartyStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(store, revision);
    }

    private static IDisposable EnterTravelPartyMutationWindow(TravelPartyStore store)
    {
        return (IDisposable)typeof(TravelPartyStore)
            .GetMethod("EnterMutationWindow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, null);
    }

    private static ExpeditionTestContext CreateContext(
        SpatialTravelFixture fixture,
        NpcRuntime performer,
        NpcRuntime support = null,
        string originDecisionId = null)
    {
        List<ActionExecutionParticipant> participants = new List<ActionExecutionParticipant>
        {
            new ActionExecutionParticipant(performer.RuntimeId, ActionExecutionParticipantRole.Performer)
        };

        if (support != null)
        {
            participants.Add(new ActionExecutionParticipant(support.RuntimeId, ActionExecutionParticipantRole.Support));
        }

        return new ExpeditionTestContext(
            new ActionExecutionContext(
                "expedition-action",
                participants,
                fixture.Site.Location.RuntimeId,
                fixture.SiteRoute.RuntimeId,
                originDecisionId));
    }

    private static void AddSpatialKnowledge(
        NpcRuntime npc,
        SpatialTravelFixture fixture,
        bool includeRoute)
    {
        npc.SpatialKnowledge.DiscoverLocation(fixture.CityA.Location.RuntimeId);
        npc.SpatialKnowledge.DiscoverLocation(fixture.Site.Location.RuntimeId);

        if (includeRoute)
        {
            npc.SpatialKnowledge.DiscoverRoute(fixture.SiteRoute.RuntimeId);
        }
    }

    private static ExpeditionRuntime CreateRuntime(
        string expeditionId,
        string targetSiteRuntimeId = "site-1",
        string originLocationRuntimeId = "location-a",
        string targetLocationRuntimeId = "location-site",
        string outboundRouteRuntimeId = "route-a-site",
        string memberRuntimeId = "npc-performer")
    {
        return new ExpeditionRuntime(
            expeditionId,
            targetSiteRuntimeId,
            originLocationRuntimeId,
            targetLocationRuntimeId,
            outboundRouteRuntimeId,
            null,
            null,
            new[] { memberRuntimeId },
            new[] { memberRuntimeId },
            Array.Empty<string>());
    }

    private static ExpeditionRuntime CreateActiveAtSiteExpedition(
        string expeditionId,
        string memberRuntimeId,
        ExpeditionObjectiveRuntime objective = null,
        ExpeditionState state = ExpeditionState.AtSite)
    {
        return new ExpeditionRuntime(
            expeditionId,
            "site-1",
            "location-a",
            "location-site",
            "route-a-site",
            state == ExpeditionState.Returning ? "party-returning" : "party-outbound",
            null,
            new[] { memberRuntimeId },
            new[] { memberRuntimeId },
            Array.Empty<string>(),
            state,
            objective);
    }

    private static bool TryReserve(
        ExpeditionStore store,
        string methodName,
        ExpeditionRuntime expected,
        out object reservation)
    {
        object[] arguments = { expected, null };
        MethodInfo method = typeof(ExpeditionStore).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        bool reserved = (bool)method.Invoke(store, arguments);
        reservation = arguments[1];
        return reserved;
    }

    private static bool InvokeStoreBoolean(ExpeditionStore store, string methodName, object argument)
    {
        MethodInfo method = typeof(ExpeditionStore).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)method.Invoke(store, new[] { argument });
    }

    private static object InvokeStoreObject(ExpeditionStore store, string methodName)
    {
        MethodInfo method = typeof(ExpeditionStore).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return method.Invoke(store, null);
    }

    private static bool CommitReserved(ExpeditionStore store, object reservation, Func<bool> action)
    {
        MethodInfo method = typeof(ExpeditionStore).GetMethod(
            "CommitReserved",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)method.Invoke(store, new object[] { reservation, action });
    }

    private static bool InvokeRuntimeBoolean(ExpeditionRuntime expedition, string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(ExpeditionRuntime).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (bool)method.Invoke(expedition, arguments);
    }

    private static void SetExpeditionRevision(ExpeditionStore store, long value)
    {
        typeof(ExpeditionStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(store, value);
    }

    private static int CountEvents<T>(IReadOnlyList<DomainEvent> events) where T : DomainEvent
    {
        int count = 0;

        foreach (DomainEvent domainEvent in events)
        {
            if (domainEvent is T)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountEventsWithDecision(
        IReadOnlyList<DomainEvent> events,
        string originDecisionId)
    {
        int count = 0;

        foreach (DomainEvent domainEvent in events)
        {
            if (domainEvent != null && domainEvent.OriginDecisionId == originDecisionId)
            {
                count++;
            }
        }

        return count;
    }
}

internal sealed class ExpeditionTestContext
{
    public ActionExecutionContext ActionContext { get; }

    public ExpeditionTestContext(ActionExecutionContext actionContext)
    {
        ActionContext = actionContext;
    }
}
