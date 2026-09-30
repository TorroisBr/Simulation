using System;
using System.Reflection;
using System.Threading;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TravelPartyCensusTests
{
    [SetUp]
    public void SetUp() { SimulationTestFactory.CleanupDefinitions(); }

    [TearDown]
    public void TearDown() { SimulationTestFactory.CleanupDefinitions(); }

    [Test]
    public void StoreRevisionTracksSuccessfulAddCompleteAndRemoveOnly()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        TravelPartyCensusProvider provider = new TravelPartyCensusProvider(fixture.Parties);
        TravelPartyRuntime first = CreateParty("party-a", "member-a");
        AssertWitness(provider, fixture.Parties, 0, 0);
        Assert.That(fixture.Parties.Add(first), Is.True);
        AssertWitness(provider, fixture.Parties, 1, 1);
        Assert.That(fixture.Parties.Complete(first.TravelPartyId), Is.True);
        Assert.That(first.IsCompleted, Is.True);
        Assert.That(fixture.Parties.Complete(first.TravelPartyId), Is.False);
        AssertWitness(provider, fixture.Parties, 0, 2);

        TravelPartyRuntime second = CreateParty("party-b", "member-b");
        Assert.That(fixture.Parties.Add(second), Is.True);
        Assert.That(fixture.Parties.Remove(second.TravelPartyId), Is.True);
        Assert.That(second.IsActive, Is.True);
        Assert.That(fixture.Parties.Remove(second.TravelPartyId), Is.False);
        AssertWitness(provider, fixture.Parties, 0, 4);
    }

    [Test]
    public void SaturatedStoreMutationsFailWithoutChangingOwnerOrParty()
    {
        TravelPartyStore store = new TravelPartyStore();
        TravelPartyRuntime party = CreateParty("party-saturated", "member-saturated");
        SetRevision(store, long.MaxValue);
        Assert.That(store.Add(party), Is.False);
        SetRevision(store, 0);
        Assert.That(store.Add(party), Is.True);
        SetRevision(store, long.MaxValue);
        Assert.That(store.Complete(party.TravelPartyId), Is.False);
        Assert.That(party.IsCompleted, Is.False);
        Assert.That(store.Remove(party.TravelPartyId), Is.False);
        Assert.That(store.ActiveParties.Count, Is.EqualTo(1));
        Assert.That(store.Revision, Is.EqualTo(long.MaxValue));
    }

    [TestCase("Add")]
    [TestCase("Complete")]
    [TestCase("Remove")]
    public void PublicWriterWaitsForStoreMutationWindow(string operation)
    {
        TravelPartyStore store = new TravelPartyStore();
        TravelPartyRuntime existing = CreateParty("party-existing", "member-existing");
        Assert.That(store.Add(existing), Is.True);
        TravelPartyRuntime added = CreateParty("party-added", "member-added");
        ManualResetEvent started = new ManualResetEvent(false);
        ManualResetEvent finished = new ManualResetEvent(false);
        bool result = false;
        Thread writer;

        using (EnterMutationWindow(store))
        {
            writer = new Thread(() =>
            {
                started.Set();
                result = operation == "Add" ? store.Add(added)
                    : operation == "Complete" ? store.Complete(existing.TravelPartyId)
                    : store.Remove(existing.TravelPartyId);
                finished.Set();
            });
            writer.Start();
            Assert.That(started.WaitOne(TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(finished.WaitOne(TimeSpan.FromMilliseconds(100)), Is.False);
            Assert.That(store.Revision, Is.EqualTo(1));
            Assert.That(store.ActiveParties.Count, Is.EqualTo(1));
        }

        Assert.That(finished.WaitOne(TimeSpan.FromSeconds(2)), Is.True);
        writer.Join();
        Assert.That(result, Is.True);
        Assert.That(store.Revision, Is.EqualTo(2));
        started.Dispose();
        finished.Dispose();
    }

    [Test]
    public void TravelPartySystemUsesOneOwnerAndRevisionsOnStartAndArrival()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(1);
        TravelPartyCensusProvider provider = new TravelPartyCensusProvider(fixture.Parties);
        ActionExecutionContext context = new ActionExecutionContext(
            "census-travel", new[]
            {
                new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(fixture.Marta.RuntimeId, ActionExecutionParticipantRole.Support)
            }, fixture.World.B.Location.RuntimeId, fixture.World.RouteAB.RuntimeId);

        Assert.That(fixture.System.TryStartTravelParty(context, out _), Is.True);
        AssertWitness(provider, fixture.Parties, 1, 1);
        fixture.System.AdvanceParties();
        AssertWitness(provider, fixture.Parties, 1, 1);
        fixture.System.AdvanceParties();
        AssertWitness(provider, fixture.Parties, 0, 2);
    }

    [Test]
    public void StartEventCompensationCommitsAddAndRemoveSeparately()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SetRevision(fixture.Parties, long.MaxValue - 2);
        typeof(SimulationRecordSequence).GetField("nextSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(fixture.Records.Sequence, long.MaxValue);
        ActionExecutionContext context = CreateContext(fixture);

        LogAssert.Expect(LogType.Error, "Cannot allocate domain EventId: Simulation record sequence is exhausted.");
        Assert.That(fixture.System.TryStartTravelParty(context, out _), Is.False);

        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Parties.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(fixture.Members.All(member => member.IsTraveling == false), Is.True);
    }

    [Test]
    public void StartAtInsufficientSaturationRejectsBeforeTravelSideEffects()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SetRevision(fixture.Parties, long.MaxValue - 1);

        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.False);

        Assert.That(fixture.Parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Parties.Revision, Is.EqualTo(long.MaxValue - 1));
        Assert.That(fixture.Members.All(member => member.IsTraveling == false), Is.True);
        Assert.That(fixture.Records.Events.Events, Is.Empty);
    }

    [Test]
    public void SaturatedFinalArrivalDoesNotProgressMembersOrRecordArrival()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture(2);
        Assert.That(fixture.System.TryStartTravelParty(CreateContext(fixture), out _), Is.True);
        fixture.System.AdvanceParties();
        fixture.System.AdvanceParties();
        Assert.That(fixture.Bruno.TravelDaysRemaining, Is.EqualTo(1));
        int eventCount = fixture.Records.Events.Events.Count;
        SetRevision(fixture.Parties, long.MaxValue);

        Assert.That(fixture.System.AdvanceParties(), Is.Empty);

        Assert.That(fixture.Members.All(member => member.IsTraveling && member.TravelDaysRemaining == 1), Is.True);
        Assert.That(fixture.Parties.ActiveParties, Has.Count.EqualTo(1));
        Assert.That(fixture.Records.Events.Events, Has.Count.EqualTo(eventCount));
    }

    private static TravelPartyRuntime CreateParty(string id, string member)
    {
        return new TravelPartyRuntime(id, "origin", "destination", "route", new[] { member }, null, 2, null,
            new[] { new TravelPartyMemberCost(member, 0f) });
    }

    private static void AssertWitness(TravelPartyCensusProvider provider, TravelPartyStore owner, int count, long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo("p12f.travel-parties"));
        Assert.That(witness.SchemaVersion, Is.EqualTo(1));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(count));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private static void SetRevision(TravelPartyStore store, long revision)
    {
        typeof(TravelPartyStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(store, revision);
    }

    private static IDisposable EnterMutationWindow(TravelPartyStore store)
    {
        return (IDisposable)typeof(TravelPartyStore)
            .GetMethod("EnterMutationWindow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(store, null);
    }

    private static ActionExecutionContext CreateContext(TravelPartyFixture fixture)
    {
        return new ActionExecutionContext("census-travel", new[]
        {
            new ActionExecutionParticipant(fixture.Bruno.RuntimeId, ActionExecutionParticipantRole.Performer),
            new ActionExecutionParticipant(fixture.Caio.RuntimeId, ActionExecutionParticipantRole.Performer),
            new ActionExecutionParticipant(fixture.Marta.RuntimeId, ActionExecutionParticipantRole.Support)
        }, fixture.World.B.Location.RuntimeId, fixture.World.RouteAB.RuntimeId);
    }

}
