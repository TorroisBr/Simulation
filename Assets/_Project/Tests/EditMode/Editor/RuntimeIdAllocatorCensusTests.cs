using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class RuntimeIdAllocatorCensusTests
{
    [Test]
    public void FourteenTypedCounterSectionsShareIdentityAndAdvanceIndependently()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        IReadOnlyList<IOwnerSectionCensusProvider> providers = RuntimeIdAllocatorCensusProvider.CreateProviders(allocator);
        object ownerIdentity = null;
        long[] expectedRevisions = new long[14];

        AssertCensus(Capture(providers), expectedRevisions, ownerIdentity);
        ownerIdentity = Capture(providers)[0].OwnerInstanceIdentity;

        Func<string>[] allocations =
        {
            allocator.AllocateNpcId,
            allocator.AllocateCityId,
            allocator.AllocateLocationId,
            allocator.AllocateRouteId,
            allocator.AllocateEventId,
            allocator.AllocateDirectiveId,
            allocator.AllocateDecisionId,
            allocator.AllocateTravelPartyId,
            allocator.AllocateOrganizationId,
            allocator.AllocateExplorableSiteId,
            allocator.AllocateExpeditionId,
            allocator.AllocateLocalPlaceId,
            allocator.AllocateLocalConnectionId,
            allocator.AllocateNotableItemId
        };

        for (int allocatedCounter = 0; allocatedCounter < allocations.Length; allocatedCounter++)
        {
            Assert.That(allocations[allocatedCounter](), Is.Not.Empty);
            expectedRevisions[allocatedCounter]++;
            AssertCensus(Capture(providers), expectedRevisions, ownerIdentity);
        }

        AssertCensus(Capture(providers), expectedRevisions, ownerIdentity);
    }

    [Test]
    public void AllocationRemainsConsumedWhenLaterRegistrationFails()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        IReadOnlyList<IOwnerSectionCensusProvider> providers = RuntimeIdAllocatorCensusProvider.CreateProviders(allocator);
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterNpc(new NpcRuntime("npc-000001", null)), Is.True);

        string allocatedId = allocator.AllocateNpcId();
        Assert.That(allocatedId, Is.EqualTo("npc-000001"));
        LogAssert.Expect(
            UnityEngine.LogType.Error,
            "Duplicate RuntimeId 'npc-000001' while registering NPC; it is already registered as NPC.");
        Assert.That(registry.RegisterNpc(new NpcRuntime(allocatedId, null)), Is.False);

        long[] expectedRevisions = new long[14];
        expectedRevisions[0] = 1L;
        AssertCensus(Capture(providers), expectedRevisions, Capture(providers)[0].OwnerInstanceIdentity);
    }

    [Test]
    public void ExhaustedCounterThrowsWithoutChangingItsRevision()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        IReadOnlyList<IOwnerSectionCensusProvider> providers = RuntimeIdAllocatorCensusProvider.CreateProviders(allocator);
        typeof(RuntimeIdAllocator)
            .GetField("nextNpcSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(allocator, long.MaxValue);

        OwnerSectionCensusWitness before = Capture(providers)[0];
        Assert.That(before.Revision, Is.EqualTo(long.MaxValue - 1L));
        Assert.Throws<InvalidOperationException>(() => allocator.AllocateNpcId());

        OwnerSectionCensusWitness after = Capture(providers)[0];
        Assert.That(after.OwnerInstanceIdentity, Is.SameAs(before.OwnerInstanceIdentity));
        Assert.That(after.Cardinality, Is.EqualTo(1));
        Assert.That(after.Revision, Is.EqualTo(before.Revision));
        OwnerSectionCensusWitness[] allAfter = Capture(providers);
        for (int i = 1; i < allAfter.Length; i++)
        {
            Assert.That(allAfter[i].Revision, Is.Zero);
            Assert.That(allAfter[i].OwnerInstanceIdentity, Is.SameAs(before.OwnerInstanceIdentity));
        }
    }

    private static readonly string[] ExpectedSectionIds =
    {
        RuntimeIdAllocatorCensusProvider.NpcsSectionId,
        RuntimeIdAllocatorCensusProvider.CitiesSectionId,
        RuntimeIdAllocatorCensusProvider.LocationsSectionId,
        RuntimeIdAllocatorCensusProvider.RoutesSectionId,
        RuntimeIdAllocatorCensusProvider.EventsSectionId,
        RuntimeIdAllocatorCensusProvider.DirectivesSectionId,
        RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
        RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
        RuntimeIdAllocatorCensusProvider.OrganizationsSectionId,
        RuntimeIdAllocatorCensusProvider.ExplorableSitesSectionId,
        RuntimeIdAllocatorCensusProvider.ExpeditionsSectionId,
        RuntimeIdAllocatorCensusProvider.LocalPlacesSectionId,
        RuntimeIdAllocatorCensusProvider.LocalConnectionsSectionId,
        RuntimeIdAllocatorCensusProvider.NotableItemsSectionId
    };

    private static OwnerSectionCensusWitness[] Capture(IReadOnlyList<IOwnerSectionCensusProvider> providers)
    {
        OwnerSectionCensusWitness[] witnesses = new OwnerSectionCensusWitness[providers.Count];
        for (int i = 0; i < providers.Count; i++)
        {
            witnesses[i] = providers[i].GetCurrentCensus();
        }

        return witnesses;
    }

    private static void AssertCensus(OwnerSectionCensusWitness[] witnesses, long[] expectedRevisions, object expectedOwnerIdentity)
    {
        Assert.That(witnesses.Length, Is.EqualTo(ExpectedSectionIds.Length));
        Assert.That(expectedRevisions.Length, Is.EqualTo(ExpectedSectionIds.Length));
        object ownerIdentity = expectedOwnerIdentity;
        for (int i = 0; i < witnesses.Length; i++)
        {
            Assert.That(witnesses[i].SectionId, Is.EqualTo(ExpectedSectionIds[i]));
            Assert.That(witnesses[i].SchemaVersion, Is.EqualTo(RuntimeIdAllocatorCensusProvider.SchemaVersion));
            Assert.That(witnesses[i].Cardinality, Is.EqualTo(1));
            Assert.That(witnesses[i].Revision, Is.EqualTo(expectedRevisions[i]));
            if (ownerIdentity == null)
            {
                ownerIdentity = witnesses[i].OwnerInstanceIdentity;
            }
            else
            {
                Assert.That(witnesses[i].OwnerInstanceIdentity, Is.SameAs(ownerIdentity));
            }
        }
    }
}
