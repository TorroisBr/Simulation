using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class SpatialKnowledgeCensusTests
{
    [Test]
    public void ProvidersArePerNpcSortedAndShareOwnerRevisionAcrossSections()
    {
        NpcRuntime second = new NpcRuntime("npc-z", null);
        NpcRuntime first = new NpcRuntime("npc-a", null);
        first.SpatialKnowledge.DiscoverLocation("location-a");
        first.SpatialKnowledge.DiscoverRoute("route-a");
        second.SpatialKnowledge.DiscoverLocation("location-z");

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SpatialKnowledgeCensusProvider.CreateProviders(new[] { second, first });

        Assert.That(providers.Select(provider => provider.GetCurrentCensus().SectionId), Is.EqualTo(new[]
        {
            SpatialKnowledgeCensusProvider.LocationsSectionPrefix + "npc-a",
            SpatialKnowledgeCensusProvider.RoutesSectionPrefix + "npc-a",
            SpatialKnowledgeCensusProvider.LocationsSectionPrefix + "npc-z",
            SpatialKnowledgeCensusProvider.RoutesSectionPrefix + "npc-z"
        }));

        OwnerSectionCensusWitness firstLocations = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness firstRoutes = providers[1].GetCurrentCensus();
        Assert.That(firstLocations.SchemaVersion, Is.EqualTo(SpatialKnowledgeCensusProvider.SchemaVersion));
        Assert.That(firstLocations.OwnerInstanceIdentity, Is.SameAs(first.SpatialKnowledge));
        Assert.That(firstRoutes.OwnerInstanceIdentity, Is.SameAs(first.SpatialKnowledge));
        Assert.That(firstLocations.Cardinality, Is.EqualTo(1));
        Assert.That(firstRoutes.Cardinality, Is.EqualTo(1));
        Assert.That(firstLocations.Revision, Is.EqualTo(2L));
        Assert.That(firstRoutes.Revision, Is.EqualTo(firstLocations.Revision));
        OwnerSectionCensusWitness repeatedFirstLocations = providers[0].GetCurrentCensus();
        Assert.That(repeatedFirstLocations.OwnerInstanceIdentity, Is.SameAs(firstLocations.OwnerInstanceIdentity));
        Assert.That(repeatedFirstLocations.Cardinality, Is.EqualTo(firstLocations.Cardinality));
        Assert.That(repeatedFirstLocations.Revision, Is.EqualTo(firstLocations.Revision));

        OwnerSectionCensusWitness secondLocations = providers[2].GetCurrentCensus();
        OwnerSectionCensusWitness secondRoutes = providers[3].GetCurrentCensus();
        Assert.That(secondLocations.OwnerInstanceIdentity, Is.SameAs(second.SpatialKnowledge));
        Assert.That(secondRoutes.OwnerInstanceIdentity, Is.SameAs(second.SpatialKnowledge));
        Assert.That(secondLocations.Cardinality, Is.EqualTo(1));
        Assert.That(secondRoutes.Cardinality, Is.Zero);
        Assert.That(secondLocations.Revision, Is.EqualTo(1L));
        Assert.That(secondRoutes.Revision, Is.EqualTo(secondLocations.Revision));
    }

    [Test]
    public void KnowledgeViewsAreReadOnlyLiveViewsAndDiscoveryRemainsIdempotent()
    {
        SpatialKnowledgeRuntime owner = new SpatialKnowledgeRuntime("npc-view");
        Assert.That(owner.DiscoverLocation("location-a"), Is.True);
        Assert.That(owner.DiscoverRoute("route-a"), Is.True);

        IReadOnlyList<string> locations = owner.KnownLocationRuntimeIds;
        IReadOnlyList<string> routes = owner.KnownRouteRuntimeIds;
        Assert.That(locations, Is.Not.InstanceOf<List<string>>());
        Assert.That(routes, Is.Not.InstanceOf<List<string>>());
        Assert.Throws<System.NotSupportedException>(() => ((IList<string>)locations).Add("forged-location"));
        Assert.Throws<System.NotSupportedException>(() => ((IList<string>)routes).Clear());

        Assert.That(owner.DiscoverLocation("location-a"), Is.False);
        Assert.That(owner.DiscoverRoute("route-a"), Is.False);
        Assert.That(owner.DiscoverLocation(" "), Is.False);
        Assert.That(owner.DiscoverRoute(null), Is.False);
        Assert.That(owner.DiscoverLocation("location-b"), Is.True);
        Assert.That(owner.DiscoverRoute("route-b"), Is.True);

        Assert.That(locations, Is.EqualTo(new[] { "location-a", "location-b" }));
        Assert.That(routes, Is.EqualTo(new[] { "route-a", "route-b" }));
        Assert.That(owner.Revision, Is.EqualTo(4L));
    }

    [Test]
    public void SaturatedRevisionRejectsNewKnowledgeBeforeChangingCardinality()
    {
        NpcRuntime npc = new NpcRuntime("npc-saturated", null);
        SpatialKnowledgeRuntime owner = npc.SpatialKnowledge;
        Assert.That(owner.DiscoverLocation("location-existing"), Is.True);
        typeof(SpatialKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(owner, long.MaxValue);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SpatialKnowledgeCensusProvider.CreateProviders(new[] { npc });
        OwnerSectionCensusWitness locationsBefore = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness routesBefore = providers[1].GetCurrentCensus();

        Assert.That(owner.DiscoverLocation("location-new"), Is.False);
        Assert.That(owner.DiscoverRoute("route-new"), Is.False);
        Assert.That(providers[0].GetCurrentCensus().Cardinality, Is.EqualTo(locationsBefore.Cardinality));
        Assert.That(providers[1].GetCurrentCensus().Cardinality, Is.EqualTo(routesBefore.Cardinality));
        Assert.That(providers[0].GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
        Assert.That(providers[1].GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
        Assert.That(owner.KnownLocationRuntimeIds, Is.EqualTo(new[] { "location-existing" }));
        Assert.That(owner.KnownRouteRuntimeIds, Is.Empty);
    }
}
