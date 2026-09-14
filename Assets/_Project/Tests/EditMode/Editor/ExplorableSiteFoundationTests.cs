using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ExplorableSiteFoundationTests
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

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void ExplorableSiteRuntime_RequiresRuntimeId(string runtimeId)
    {
        Assert.Throws<ArgumentException>(() => CreateSite(runtimeId));
    }

    [Test]
    public void ExplorableSiteRuntime_RequiresDefinition()
    {
        Assert.Throws<ArgumentNullException>(() => new ExplorableSiteRuntime(
            "site-runtime",
            null,
            new SpatialLocationRuntime("location-runtime")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void ExplorableSiteRuntime_RequiresDefinitionId(string definitionId)
    {
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite(definitionId);

        Assert.Throws<ArgumentException>(() => new ExplorableSiteRuntime(
            "site-runtime",
            definition,
            new SpatialLocationRuntime("location-runtime")));
    }

    [Test]
    public void ExplorableSiteRuntime_RequiresLocation()
    {
        Assert.Throws<ArgumentNullException>(() => new ExplorableSiteRuntime(
            "site-runtime",
            SimulationTestFactory.CreateExplorableSite("site-definition"),
            null));
    }

    [Test]
    public void ExplorableSiteRuntime_RequiresDistinctSiteAndLocationIdentities()
    {
        Assert.Throws<ArgumentException>(() => new ExplorableSiteRuntime(
            "same-runtime-id",
            SimulationTestFactory.CreateExplorableSite("site-definition"),
            new SpatialLocationRuntime("same-runtime-id")));
    }

    [Test]
    public void ExplorableSiteRuntime_SeparatesSiteIdentityFromLocationAndDefinitionDisplay()
    {
        ExplorableSiteData definition = SimulationTestFactory.CreateExplorableSite(
            "ruin-definition",
            ExplorableSiteKind.Ruin,
            "The Old Display");
        ExplorableSiteRuntime site = new ExplorableSiteRuntime(
            "site-runtime",
            definition,
            new SpatialLocationRuntime("location-runtime"));

        Assert.That(site.RuntimeId, Is.Not.EqualTo(site.Location.RuntimeId));
        Assert.That(site.DefinitionId, Is.EqualTo("ruin-definition"));
        Assert.That(site.DefinitionId, Is.Not.EqualTo(definition.DisplayName));
        Assert.That(site.Definition, Is.SameAs(definition));
        SimulationInvariantValidator.ValidateExplorableSite(site);
    }

    [Test]
    public void RuntimeIdAllocator_AllocatesExplorableSiteIdsDeterministically()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();

        Assert.That(allocator.AllocateExplorableSiteId(), Is.EqualTo("site-000001"));
        Assert.That(allocator.AllocateExplorableSiteId(), Is.EqualTo("site-000002"));
    }

    [Test]
    public void RuntimeIdentityRegistry_RegistersAndResolvesExplorableSite()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        ExplorableSiteRuntime site = CreateSite("site-runtime");

        Assert.That(registry.RegisterExplorableSite(site), Is.True);
        Assert.That(registry.TryGetExplorableSite(site.RuntimeId, out ExplorableSiteRuntime resolved), Is.True);
        Assert.That(resolved, Is.SameAs(site));
    }

    [Test]
    public void RuntimeIdentityRegistry_RejectsDuplicateExplorableSiteRuntimeId()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        ExplorableSiteRuntime first = CreateSite("duplicate-site", "location-one");
        ExplorableSiteRuntime duplicate = CreateSite("duplicate-site", "location-two");
        Assert.That(registry.RegisterExplorableSite(first), Is.True);
        LogAssert.Expect(
            LogType.Error,
            "Duplicate RuntimeId 'duplicate-site' while registering ExplorableSite; it is already registered as ExplorableSite.");

        Assert.That(registry.RegisterExplorableSite(duplicate), Is.False);
    }

    [Test]
    public void RuntimeIdentityRegistry_RejectsCrossTypeRuntimeIdCollision()
    {
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        SpatialLocationRuntime location = new SpatialLocationRuntime("shared-runtime-id");
        Assert.That(registry.RegisterLocation(location), Is.True);
        ExplorableSiteRuntime site = CreateSite("shared-runtime-id", "site-location");
        LogAssert.Expect(
            LogType.Error,
            "Duplicate RuntimeId 'shared-runtime-id' while registering ExplorableSite; it is already registered as Location.");

        Assert.That(registry.RegisterExplorableSite(site), Is.False);
    }

    [Test]
    public void ExplorableSiteStore_PreservesInsertionOrderAndLookupDoesNotCreate()
    {
        ExplorableSiteStore store = new ExplorableSiteStore();
        Assert.That(store.GetByRuntimeId("missing"), Is.Null);
        Assert.That(store.TryGetByRuntimeId("missing", out _), Is.False);
        Assert.That(store.GetForLocationRuntimeId("missing-location").Count, Is.EqualTo(0));
        Assert.That(store.Sites.Count, Is.EqualTo(0));

        ExplorableSiteRuntime first = CreateSite("site-one", "location-one");
        ExplorableSiteRuntime second = CreateSite("site-two", "location-two");
        Assert.That(store.Add(first), Is.True);
        Assert.That(store.Add(second), Is.True);

        Assert.That(store.Sites.Count, Is.EqualTo(2));
        Assert.That(store.Sites[0], Is.SameAs(first));
        Assert.That(store.Sites[1], Is.SameAs(second));
        Assert.That(store.GetByRuntimeId(second.RuntimeId), Is.SameAs(second));
    }

    [Test]
    public void ExplorableSiteStore_RejectsDuplicateRuntimeId()
    {
        ExplorableSiteStore store = new ExplorableSiteStore();

        Assert.That(store.Add(CreateSite("duplicate-site", "location-one")), Is.True);
        Assert.That(store.Add(CreateSite("duplicate-site", "location-two")), Is.False);
        Assert.That(store.Sites.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExplorableSiteStore_SupportsMultipleSitesAtOneLocation()
    {
        ExplorableSiteStore store = new ExplorableSiteStore();
        SpatialLocationRuntime sharedLocation = new SpatialLocationRuntime("shared-location");
        ExplorableSiteRuntime first = new ExplorableSiteRuntime(
            "site-one",
            SimulationTestFactory.CreateExplorableSite("definition-one"),
            sharedLocation);
        ExplorableSiteRuntime second = new ExplorableSiteRuntime(
            "site-two",
            SimulationTestFactory.CreateExplorableSite("definition-two"),
            sharedLocation);

        Assert.That(store.Add(first), Is.True);
        Assert.That(store.Add(second), Is.True);
        IReadOnlyList<ExplorableSiteRuntime> sitesAtLocation = store.GetForLocation(sharedLocation);

        Assert.That(sitesAtLocation.Count, Is.EqualTo(2));
        Assert.That(sitesAtLocation[0], Is.SameAs(first));
        Assert.That(sitesAtLocation[1], Is.SameAs(second));
    }

    private static ExplorableSiteRuntime CreateSite(string runtimeId, string locationRuntimeId = "location-runtime")
    {
        return new ExplorableSiteRuntime(
            runtimeId,
            SimulationTestFactory.CreateExplorableSite("site-definition"),
            new SpatialLocationRuntime(locationRuntimeId));
    }
}
