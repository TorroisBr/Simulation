using System;
using NUnit.Framework;

[TestFixture]
public sealed class RuntimeIdAllocatorCoreTests
{
    [Test]
    public void CounterAllocationPreservesStableOwnerAndIndependentRevisions()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        RuntimeIdAllocatorOwnerBridge.CaptureCensusIdentity(
            allocator,
            RuntimeIdAllocatorCensusCounter.Events,
            out object initialOwnerIdentity,
            out long initialEventRevision);

        Assert.That(initialEventRevision, Is.Zero);
        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-000001"));
        Assert.That(allocator.AllocateEventId(), Is.EqualTo("event-000001"));

        Assert.That(
            RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                allocator,
                RuntimeIdAllocatorCensusCounter.Events,
                initialOwnerIdentity,
                initialEventRevision,
                out long eventRevision),
            Is.False);
        Assert.That(eventRevision, Is.EqualTo(1L));

        RuntimeIdAllocatorOwnerBridge.CaptureCensusIdentity(
            allocator,
            RuntimeIdAllocatorCensusCounter.Npcs,
            out object currentOwnerIdentity,
            out long npcRevision);
        Assert.That(currentOwnerIdentity, Is.SameAs(initialOwnerIdentity));
        Assert.That(npcRevision, Is.EqualTo(1L));

        Assert.That(
            RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                allocator,
                RuntimeIdAllocatorCensusCounter.Events,
                currentOwnerIdentity,
                eventRevision,
                out long currentEventRevision),
            Is.True);
        Assert.That(currentEventRevision, Is.EqualTo(1L));
    }

    [Test]
    public void StagedRestorePreservesNextIdsForEveryCounterFamily()
    {
        RuntimeIdAllocator source = new RuntimeIdAllocator();
        source.AllocateNpcId();
        source.AllocateCityId();
        source.AllocateLocationId();
        source.AllocateRouteId();
        source.AllocateEventId();
        source.AllocateDirectiveId();
        source.AllocateDecisionId();
        source.AllocateTravelPartyId();
        source.AllocateOrganizationId();
        source.AllocateExplorableSiteId();
        source.AllocateExpeditionId();
        source.AllocateLocalPlaceId();
        source.AllocateLocalConnectionId();
        source.AllocateNotableItemId();

        Assert.That(
            RuntimeIdAllocatorOwnerBridge.TryCreateStagedFromSnapshot(
                source.CaptureSnapshot(),
                out RuntimeIdAllocator restored,
                out string diagnostic),
            Is.True,
            diagnostic);

        AssertNextIds(source, 2);
        AssertNextIds(restored, 2);
    }

    [Test]
    public void P12CounterBoundariesRejectBeforeAllocationAndAdvanceOnlyOnCommit()
    {
        AssertBoundary(
            RuntimeIdAllocatorCensusCounter.Events,
            (allocator, admission, committed) =>
                RuntimeIdAllocatorOwnerBridge.BindP12EventIdMutationBoundary(allocator, admission, committed),
            allocator => allocator.AllocateEventId(),
            "event-000001");
        AssertBoundary(
            RuntimeIdAllocatorCensusCounter.Decisions,
            (allocator, admission, committed) =>
                RuntimeIdAllocatorOwnerBridge.BindP12DecisionIdMutationBoundary(allocator, admission, committed),
            allocator => allocator.AllocateDecisionId(),
            "decision-000001");
        AssertBoundary(
            RuntimeIdAllocatorCensusCounter.TravelParties,
            (allocator, admission, committed) =>
                RuntimeIdAllocatorOwnerBridge.BindP12TravelPartyIdMutationBoundary(allocator, admission, committed),
            allocator => allocator.AllocateTravelPartyId(),
            "travel-party-000001");
    }

    private static void AssertBoundary(
        RuntimeIdAllocatorCensusCounter counter,
        Action<RuntimeIdAllocator, Func<bool>, Action> bind,
        Func<RuntimeIdAllocator, string> allocate,
        string firstId)
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        bool admitted = false;
        int committed = 0;
        bind(allocator, () => admitted, () => committed++);

        Assert.Throws<InvalidOperationException>(() => allocate(allocator));
        RuntimeIdAllocatorOwnerBridge.CaptureCensusIdentity(
            allocator,
            counter,
            out object ownerIdentity,
            out long revisionAfterReject);
        Assert.That(revisionAfterReject, Is.Zero);
        Assert.That(committed, Is.Zero);

        admitted = true;
        Assert.That(allocate(allocator), Is.EqualTo(firstId));
        Assert.That(committed, Is.EqualTo(1));
        Assert.That(
            RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                allocator,
                counter,
                ownerIdentity,
                revisionAfterReject,
                out long revisionAfterCommit),
            Is.False);
        Assert.That(revisionAfterCommit, Is.EqualTo(1L));
    }

    private static void AssertNextIds(RuntimeIdAllocator allocator, int next)
    {
        string suffix = next.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        Assert.That(allocator.AllocateNpcId(), Is.EqualTo("npc-" + suffix));
        Assert.That(allocator.AllocateCityId(), Is.EqualTo("city-" + suffix));
        Assert.That(allocator.AllocateLocationId(), Is.EqualTo("location-" + suffix));
        Assert.That(allocator.AllocateRouteId(), Is.EqualTo("route-" + suffix));
        Assert.That(allocator.AllocateEventId(), Is.EqualTo("event-" + suffix));
        Assert.That(allocator.AllocateDirectiveId(), Is.EqualTo("directive-" + suffix));
        Assert.That(allocator.AllocateDecisionId(), Is.EqualTo("decision-" + suffix));
        Assert.That(allocator.AllocateTravelPartyId(), Is.EqualTo("travel-party-" + suffix));
        Assert.That(allocator.AllocateOrganizationId(), Is.EqualTo("organization-" + suffix));
        Assert.That(allocator.AllocateExplorableSiteId(), Is.EqualTo("site-" + suffix));
        Assert.That(allocator.AllocateExpeditionId(), Is.EqualTo("expedition-" + suffix));
        Assert.That(allocator.AllocateLocalPlaceId(), Is.EqualTo("local-place-" + suffix));
        Assert.That(allocator.AllocateLocalConnectionId(), Is.EqualTo("local-connection-" + suffix));
        Assert.That(allocator.AllocateNotableItemId(), Is.EqualTo("notable-item-" + suffix));
    }
}
