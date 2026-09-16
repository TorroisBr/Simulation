using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PlaceContentFoundationTests
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
    public void AbstractSiteCanHoldPersistentContentWithoutTopology()
    {
        ExplorableSiteRuntime site = CreateSite();
        PlaceContentStore store = new PlaceContentStore();
        ItemData ore = SimulationTestFactory.CreateItem("ore");

        Assert.That(store.TryAddStack(site, ore, 8, PlaceContentPersistencePolicy.Durable, out _), Is.True);
        Assert.That(store.TryGet(site, out PlaceContentRuntime content), Is.True);
        Assert.That(content.Owner.OwnerKind, Is.EqualTo(PlaceContentOwnerKind.ExplorableSite));
        Assert.That(content.GetAmount(ore), Is.EqualTo(8));
    }

    [Test]
    public void DetailedTopologyCanLocateContentAtLocalPlace()
    {
        LocalPlaceRuntime localPlace = CreateDetailedPlace(out _);
        PlaceContentStore store = new PlaceContentStore();
        ItemData herb = SimulationTestFactory.CreateItem("herb");

        Assert.That(store.TryAddStack(localPlace, herb, 3, PlaceContentPersistencePolicy.Perishable, out _, 1), Is.True);
        Assert.That(store.TryGet(localPlace, out PlaceContentRuntime content), Is.True);
        Assert.That(content.Owner.OwnerKind, Is.EqualTo(PlaceContentOwnerKind.LocalPlace));
        Assert.That(content.Owner.OwnerRuntimeId, Is.EqualTo(localPlace.RuntimeId));
        Assert.That(content.GetAmount(herb), Is.EqualTo(3));
    }

    [Test]
    public void DetailedContentIsNotDuplicatedAsContradictoryOwnerTruth()
    {
        LocalPlaceRuntime localPlace = CreateDetailedPlace(out _);
        PlaceContentStore store = new PlaceContentStore();

        PlaceContentRuntime first = store.GetOrCreate(localPlace);
        PlaceContentRuntime second = store.GetOrCreate(localPlace);

        Assert.That(second, Is.SameAs(first));
        Assert.That(store.Places, Has.Count.EqualTo(1));
    }

    [Test]
    public void CommonStackResourceCanExistAtPlace()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("content-city", "content-city-location");
        ItemData wood = SimulationTestFactory.CreateItem("wood");
        PlaceContentStore store = new PlaceContentStore();

        Assert.That(store.TryAddStack(city, wood, 12, PlaceContentPersistencePolicy.Durable, out PlaceContentStackRuntime stack), Is.True);
        Assert.That(stack.Inventory.GetAmount(wood), Is.EqualTo(12));
        Assert.That(store.GetOrCreate(city).CommonStacks, Contains.Item(stack));
    }

    [Test]
    public void NotableItemCanHaveStableRuntimeIdentity()
    {
        NotableItemRuntime notable = new NotableItemRuntime("relic-001", SimulationTestFactory.CreateItem("relic"));

        Assert.That(notable.RuntimeId, Is.EqualTo("relic-001"));
        Assert.That(notable.PersistencePolicy, Is.EqualTo(PlaceContentPersistencePolicy.Notable));
    }

    [Test]
    public void SameNotableItemCannotExistInTwoPlaces()
    {
        CityRuntime firstCity = SimulationTestFactory.CreateCity("notable-a", "notable-location-a");
        CityRuntime secondCity = SimulationTestFactory.CreateCity("notable-b", "notable-location-b");
        NotableItemRuntime notable = new NotableItemRuntime("crown-001", SimulationTestFactory.CreateItem("crown"));
        PlaceContentStore store = CreateNotableStore();

        Assert.That(store.TryAddNotable(firstCity, notable, out string firstDiagnostic), Is.True, firstDiagnostic);
        Assert.That(store.TryAddNotable(secondCity, notable, out string secondDiagnostic), Is.False);
        Assert.That(secondDiagnostic, Does.Contain("only one place"));
    }

    [Test]
    public void TakingNotableItemRemovesItFromPreviousPlace()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("notable-take", "notable-take-location");
        NotableItemRuntime notable = new NotableItemRuntime("key-001", SimulationTestFactory.CreateItem("key"));
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        PlaceContentStore store = new PlaceContentStore(new RuntimeIdAllocator(), registry);
        NpcRuntime custodian = new NpcRuntime("key-custodian", SimulationTestFactory.CreateNpc("key-custodian"));
        Assert.That(registry.RegisterNpc(custodian), Is.True);
        Assert.That(store.TryAddNotable(city, notable, out string addDiagnostic), Is.True, addDiagnostic);

        Assert.That(store.TryTakeNotable(
            PlaceContentOwnerReference.ForCity(city), notable.RuntimeId, custodian, out NotableItemRuntime taken), Is.True);
        Assert.That(taken, Is.SameAs(notable));
        Assert.That(notable.IsHeldByNpc, Is.True);
        Assert.That(notable.CustodianNpcRuntimeId, Is.EqualTo(custodian.RuntimeId));
        Assert.That(store.GetOrCreate(city).NotableContent, Is.Empty);
    }

    [Test]
    public void TakingStackedResourceReducesAvailableQuantity()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("stack-take", "stack-take-location");
        ItemData stone = SimulationTestFactory.CreateItem("stone");
        PlaceContentStore store = new PlaceContentStore();
        Assert.That(store.TryAddStack(city, stone, 10, PlaceContentPersistencePolicy.Durable, out _), Is.True);

        Assert.That(store.TryTakeStack(PlaceContentOwnerReference.ForCity(city), stone, 4, out int removed), Is.True);
        Assert.That(removed, Is.EqualTo(4));
        Assert.That(store.GetOrCreate(city).GetAmount(stone), Is.EqualTo(6));
    }

    [Test]
    public void ContentMutationIsAtomic()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("atomic-content", "atomic-content-location");
        ItemData food = SimulationTestFactory.CreateItem("food");
        PlaceContentStore store = new PlaceContentStore();
        Assert.That(store.TryAddStack(city, food, 5, PlaceContentPersistencePolicy.Durable, out _), Is.True);

        Assert.That(store.TryTakeStack(PlaceContentOwnerReference.ForCity(city), food, 6, out int removed), Is.False);
        Assert.That(removed, Is.EqualTo(0));
        Assert.That(store.GetOrCreate(city).GetAmount(food), Is.EqualTo(5));
    }

    [Test]
    public void OppositionCanContainNamedAndAggregateParticipants()
    {
        PlaceOppositionRuntime opposition = CreateOpposition(out NpcRuntime named);

        Assert.That(opposition.TryAddNamedParticipant(named, out _), Is.True);
        Assert.That(opposition.TryAddAggregateParticipant(new AggregateParticipantSnapshot("bandits", 12f, 5), out _), Is.True);
        Assert.That(opposition.NamedParticipants, Has.Count.EqualTo(1));
        Assert.That(opposition.AggregateParticipants, Has.Count.EqualTo(1));
    }

    [Test]
    public void ResolvingOppositionUsesGenericConflictFoundation()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("conflict-place", "conflict-place-location");
        PlaceContentStore store = new PlaceContentStore();
        PlaceContentRuntime content = store.GetOrCreate(city);
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("raiders", "Raiders");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("raider-band", 10f, 20));
        Assert.That(store.TryAddOpposition(PlaceContentOwnerReference.ForCity(city), opposition, out string addDiagnostic), Is.True, addDiagnostic);

        NpcRuntime hero = new NpcRuntime("hero-conflict", SimulationTestFactory.CreateNpc("hero-conflict"));
        Conflict conflict = opposition.CreateConflict("place-conflict", new[] { hero });
        ConflictResolutionService resolver = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));

        Assert.That(store.TryResolveOpposition(
            PlaceContentOwnerReference.ForCity(city), opposition, conflict, resolver,
            out ConflictResolutionResult result, out string reason), Is.True, reason);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.WinningSideId, Is.EqualTo("expedition"));
        Assert.That(opposition.IsResolved, Is.True);
    }

    [Test]
    public void ConflictVictoryDoesNotCreateResourcesFromNothing()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("no-spawn", "no-spawn-location");
        PlaceContentStore store = new PlaceContentStore();
        PlaceContentRuntime content = store.GetOrCreate(city);
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("no-spawn-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("empty-band", 1f));
        Assert.That(store.TryAddOpposition(PlaceContentOwnerReference.ForCity(city), opposition, out _), Is.True);
        NpcRuntime hero = new NpcRuntime("no-spawn-hero", SimulationTestFactory.CreateNpc("no-spawn-hero"));

        ConflictResolutionService resolver = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));
        Assert.That(store.TryResolveOpposition(
            PlaceContentOwnerReference.ForCity(city), opposition,
            opposition.CreateConflict("no-spawn-conflict", new[] { hero }), resolver,
            out _, out string reason), Is.True, reason);

        Assert.That(content.StackedContent, Is.Empty);
        Assert.That(content.NotableContent, Is.Empty);
    }

    [Test]
    public void ExistingResourceCanBecomeAccessibleAfterConflict()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("access-after", "access-after-location");
        ItemData silver = SimulationTestFactory.CreateItem("silver");
        PlaceContentStore store = new PlaceContentStore();
        Assert.That(store.TryAddStack(city, silver, 7, PlaceContentPersistencePolicy.Durable, out _), Is.True);
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("access-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("access-band", 1f));
        Assert.That(store.TryAddOpposition(PlaceContentOwnerReference.ForCity(city), opposition, out _), Is.True);
        NpcRuntime hero = new NpcRuntime("access-hero", SimulationTestFactory.CreateNpc("access-hero"));
        ConflictResolutionService resolver = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));

        Assert.That(store.TryResolveOpposition(
            PlaceContentOwnerReference.ForCity(city), opposition,
            opposition.CreateConflict("access-conflict", new[] { hero }), resolver,
            out _, out string reason), Is.True, reason);
        Assert.That(store.GetOrCreate(city).IsAccessible, Is.True);
        Assert.That(store.GetOrCreate(city).GetAmount(silver), Is.EqualTo(7));
    }

    [Test]
    public void ClearedDoesNotMeanSecured()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("cleared", "cleared-location");
        PlaceContentStore store = new PlaceContentStore();
        PlaceContentRuntime content = store.GetOrCreate(city);

        Assert.That(content.SiteState, Is.EqualTo(PlaceSiteState.Cleared));
        Assert.That(content.MarkSecured(), Is.True);
        Assert.That(content.SiteState, Is.EqualTo(PlaceSiteState.Secured));
        Assert.That(content.SiteState == PlaceSiteState.Cleared, Is.False);
    }

    [Test]
    public void SecuredSiteCanLaterBecomeThreatenedByNewOpposition()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("reoccupied", "reoccupied-location");
        PlaceContentStore store = new PlaceContentStore();
        PlaceContentRuntime content = store.GetOrCreate(city);
        Assert.That(content.MarkSecured(), Is.True);

        Assert.That(store.TryAddOpposition(
            PlaceContentOwnerReference.ForCity(city),
            new PlaceOppositionRuntime("new-opposition"), out _), Is.True);
        Assert.That(content.SiteState, Is.EqualTo(PlaceSiteState.Threatened));
        Assert.That(content.AccessState, Is.EqualTo(PlaceAccessState.Contested));
    }

    [Test]
    public void OriginalResolvedOppositionDoesNotRespawn()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("no-respawn", "no-respawn-location");
        PlaceContentStore store = new PlaceContentStore();
        PlaceOppositionRuntime opposition = new PlaceOppositionRuntime("original-opposition");
        opposition.AddAggregateParticipant(new AggregateParticipantSnapshot("original-band", 1f));
        Assert.That(store.TryAddOpposition(PlaceContentOwnerReference.ForCity(city), opposition, out _), Is.True);
        NpcRuntime hero = new NpcRuntime("no-respawn-hero", SimulationTestFactory.CreateNpc("no-respawn-hero"));
        ConflictResolutionService resolver = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(100f), new SequenceConflictRandomSource(0.5f, 0.5f)));
        Assert.That(store.TryResolveOpposition(
            PlaceContentOwnerReference.ForCity(city), opposition,
            opposition.CreateConflict("no-respawn-conflict", new[] { hero }), resolver,
            out _, out string reason), Is.True, reason);

        Assert.That(store.GetOrCreate(city).ActiveOppositions, Is.Empty);
        Assert.That(store.GetOrCreate(city).Oppositions, Has.Count.EqualTo(1));
        Assert.That(opposition.IsResolved, Is.True);
    }

    [Test]
    public void PersistencePolicyIsStructuredNotDisplayText()
    {
        ItemData item = SimulationTestFactory.CreateItem("policy-item");
        PlaceContentStackRuntime stack = new PlaceContentStackRuntime(
            item, 2, PlaceContentPersistencePolicy.Perishable, 1);

        Assert.That(stack.PersistencePolicy, Is.EqualTo(PlaceContentPersistencePolicy.Perishable));
        Assert.That(stack.DecayPerDay, Is.EqualTo(1));
        Assert.That(stack.GetType().GetProperty("PersistencePolicy"), Is.Not.Null);
    }

    [Test]
    public void PerishableContentCanAdvanceDecayDeterministically()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("perishable", "perishable-location");
        ItemData fruit = SimulationTestFactory.CreateItem("fruit");
        PlaceContentStore store = new PlaceContentStore();
        Assert.That(store.TryAddStack(city, fruit, 10, PlaceContentPersistencePolicy.Perishable, out _, 3), Is.True);

        store.AdvanceDays(2);
        Assert.That(store.GetOrCreate(city).GetAmount(fruit), Is.EqualTo(4));
    }

    [Test]
    public void NotableContentDoesNotDisappearThroughGenericDecay()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("notable-decay", "notable-decay-location");
        NotableItemRuntime notable = new NotableItemRuntime("notable-stable", SimulationTestFactory.CreateItem("notable-stable-item"));
        PlaceContentStore store = CreateNotableStore();
        Assert.That(store.TryAddNotable(city, notable, out _), Is.True);

        store.AdvanceDays(1000);
        Assert.That(store.GetOrCreate(city).GetNotable(notable.RuntimeId), Is.SameAs(notable));
        Assert.That(notable.IsPresent, Is.True);
    }

    [Test]
    public void ExistingTravelAndConflictRegressionsRemainGreen()
    {
        Conflict conflict = new Conflict("regression-conflict");
        ConflictSide firstSide = conflict.AddSide("a", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        ConflictSide secondSide = conflict.AddSide("b", ConflictObjectiveType.Defeat, ConflictStakes.Low);
        firstSide.AddAggregate(new AggregateParticipantSnapshot("a", 2f));
        secondSide.AddAggregate(new AggregateParticipantSnapshot("b", 1f));

        ConflictResolutionResult result = new ConflictResolutionService(
            new ConflictResolver(new FixedCapabilityModel(1f), new SequenceConflictRandomSource(0.5f, 0.5f)))
            .Compute(conflict);

        Assert.That(result.WinningSideId, Is.EqualTo("a"));
        Assert.That(conflict.TryValidate(out string diagnostic), Is.True, diagnostic);
    }

    private static ExplorableSiteRuntime CreateSite()
    {
        return new ExplorableSiteRuntime(
            "content-site",
            SimulationTestFactory.CreateExplorableSite("content-site-definition"),
            new SpatialLocationRuntime("content-site-location"));
    }

    private static LocalPlaceRuntime CreateDetailedPlace(out LocalTopologyRuntime topology)
    {
        CityRuntime city = SimulationTestFactory.CreateCity("topology-owner", "topology-owner-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        LocalTopologyStore topologyStore = new LocalTopologyStore(registry);
        Assert.That(registry.RegisterCity(city), Is.True);
        Assert.That(registry.RegisterLocation(city.Location), Is.True);
        topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
        LocalPlaceRuntime root = new LocalPlaceRuntime("detailed-root", "Root");
        LocalPlaceRuntime room = new LocalPlaceRuntime("detailed-room", "Room");
        Assert.That(topology.AddPlace(root, null, true), Is.True);
        Assert.That(topologyStore.TryAddTopology(topology, out string topologyDiagnostic), Is.True, topologyDiagnostic);
        Assert.That(topologyStore.TryAddPlace(topology, room, root, out topologyDiagnostic), Is.True, topologyDiagnostic);
        return room;
    }

    private static PlaceOppositionRuntime CreateOpposition(out NpcRuntime named)
    {
        named = new NpcRuntime("named-opposition", SimulationTestFactory.CreateNpc("named-opposition"));
        return new PlaceOppositionRuntime("mixed-opposition", "Mixed Opposition");
    }

    private static PlaceContentStore CreateNotableStore()
    {
        return new PlaceContentStore(new RuntimeIdAllocator(), new RuntimeIdentityRegistry());
    }

    private sealed class FixedCapabilityModel : ICapabilityModel
    {
        private readonly float value;

        public FixedCapabilityModel(float value)
        {
            this.value = value;
        }

        public CapabilityEvaluationResult Evaluate(NpcRuntime participant, CapabilityEvaluationContext context = null)
        {
            return new CapabilityEvaluationResult(value, value, null);
        }
    }
}
