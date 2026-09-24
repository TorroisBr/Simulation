using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;

public sealed class SpatialPassageAuthorityTests
{
    [Test]
    public void BoundaryIdentityIsCanonicalUnorderedAndRequiresDistinctEndpoints()
    {
        HexBoundaryKey forward = new HexBoundaryKey(new HexId("hex.z"), new HexId("hex.a"));
        HexBoundaryKey reverse = new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.z"));

        Assert.That(forward.FirstHexId.Value, Is.EqualTo("hex.a"));
        Assert.That(forward.SecondHexId.Value, Is.EqualTo("hex.z"));
        Assert.That(forward, Is.EqualTo(reverse));
        Assert.That(forward.GetHashCode(), Is.EqualTo(reverse.GetHashCode()));
        Assert.Throws<ArgumentException>(() => new HexBoundaryKey(new HexId("same"), new HexId("same")));
    }

    [Test]
    public void TraversalOptionIdentityIsTypedAndIncludesEveryStableComponent()
    {
        TraversalOptionRef crossing = TraversalOptionRef.ForCrossing(new CrossingId("option-1"));
        TraversalOptionRef connection = TraversalOptionRef.ForConnection(new ConnectionId("option-1"));
        TraversalOptionRef ruleV1 = TraversalOptionRef.ForWildernessRule("rule.basic", "v1");
        TraversalOptionRef ruleV2 = TraversalOptionRef.ForWildernessRule("rule.basic", "v2");

        Assert.That(crossing, Is.Not.EqualTo(connection));
        Assert.That(ruleV1, Is.Not.EqualTo(ruleV2));
        Assert.That(TraversalOptionRef.ForCrossing(new CrossingId("option-1")), Is.EqualTo(crossing));
        Assert.That(TraversalOptionRef.ForConnection(new ConnectionId("option-1")), Is.Not.EqualTo(crossing));
        Assert.That(TraversalOptionRef.ForConnection(new ConnectionId("z")).CompareTo(crossing), Is.LessThan(0));
        Assert.That(ruleV1.CompareTo(ruleV2), Is.LessThan(0));
    }

    [Test]
    public void SpatialAuthorityResolvesCrossingReferenceToRegisteredCrossingAndRegionalHex()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        Assert.That(authority.TryGetGeometricBoundary(
            new HexId("hex.z"), new HexId("hex.a"), out HexBoundaryKey boundary, out SpatialAuthorityFailure failure),
            Is.True, failure.ToString());
        Assert.That(boundary.FirstHexId.Value, Is.EqualTo("hex.a"));
        Assert.That(boundary.SecondHexId.Value, Is.EqualTo("hex.z"));
        Assert.That(authority.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.bridge"), boundary, new HexId("hex.z"), "content.bridge", "bridge-v1", null), out failure),
            Is.True, failure.ToString());

        SpatialReference reference = SpatialReference.ForCrossing(new CrossingId("crossing.bridge"));
        Assert.That(reference.Kind, Is.EqualTo(SpatialReferenceKind.Crossing));
        Assert.That(reference.StableKey, Is.EqualTo("crossing:crossing.bridge"));
        Assert.That(authority.TryResolve(reference, null, out SpatialResolution resolution, out failure), Is.True, failure.ToString());
        Assert.That(resolution.Reference, Is.EqualTo(reference));
        Assert.That(resolution.Crossing.Id, Is.EqualTo(new CrossingId("crossing.bridge")));
        Assert.That(resolution.Crossing.Boundary, Is.EqualTo(boundary));
        Assert.That(resolution.Hex.Id, Is.EqualTo(new HexId("hex.z")));
        Assert.That(resolution.Location, Is.Null);

        Assert.That(authority.TryResolve(
            SpatialReference.ForCrossing(new CrossingId("crossing.missing")), null, out _, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.CrossingNotRegistered));
    }

    [Test]
    public void CrossingRegistrationRejectsNonAdjacentBoundaryWithoutMutatingRevision()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        HexBoundaryKey nonAdjacent = new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.missing"));
        long revision = authority.Revision;

        Assert.That(authority.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.invalid"), nonAdjacent, new HexId("hex.a"), "content.bridge", "bridge-v1", null),
            out SpatialAuthorityFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.CrossingBoundaryNotAdjacent));
        Assert.That(authority.Revision, Is.EqualTo(revision));
        Assert.That(authority.TryGet(new CrossingId("crossing.invalid"), out _), Is.False);
    }

    [Test]
    public void CrossingCannotClaimToOvercomeAnUnregisteredBarrier()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        long revision = authority.Revision;
        Assert.That(authority.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.invalid"), Boundary(), new HexId("hex.a"),
                "content.bridge", "bridge-v1", new[] { new BarrierId("barrier.missing") }),
            out SpatialAuthorityFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.BarrierNotRegistered));
        Assert.That(authority.Revision, Is.EqualTo(revision));
        Assert.That(authority.TryGet(new CrossingId("crossing.invalid"), out _), Is.False);
    }

    [Test]
    public void OrdinaryConnectionCannotClaimToOvercomeBarrier()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        HexBoundaryKey boundary = Boundary();
        BarrierId barrierId = new BarrierId("barrier.river");
        Assert.That(authority.PassageAuthority.TryRegisterBarrier(
            new BarrierRecord(barrierId, "content.river", "river-v1", new[] { boundary }),
            BarrierCondition.Active, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        long revision = authority.Revision;
        Assert.That(authority.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(
                TraversalOptionRef.ForConnection(new ConnectionId("connection.invalid")),
                boundary, "content.road", "road-v1", new[] { barrierId }),
            PassageCondition.Available, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.InvalidPassageOption));
        Assert.That(authority.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void OptionsRequireExplicitRegistrationAndReturnInStableKindIdentityOrder()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        HexBoundaryKey boundary = Boundary();
        Assert.That(authority.PassageAuthority.TryGetTraversalOptions(boundary, out var options, out _), Is.True);
        Assert.That(options, Is.Empty, "geometric adjacency alone must not create a traversal option");

        Assert.That(authority.PassageAuthority.TryRegisterWildernessRule(
            new PassageOptionRecord(TraversalOptionRef.ForWildernessRule("rule.walk", "v1"), boundary, "rule.walk", "v1"),
            PassageCondition.Available, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        Assert.That(authority.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(TraversalOptionRef.ForConnection(new ConnectionId("connection.z")), boundary, "road", "rev-a"),
            PassageCondition.Available, out failure), Is.True, failure.ToString());
        Assert.That(authority.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.a"), boundary, new HexId("hex.a"), "content.bridge", "bridge-v1", null), out failure), Is.True, failure.ToString());

        Assert.That(authority.PassageAuthority.TryGetTraversalOptions(boundary, out options, out failure), Is.True, failure.ToString());
        Assert.That(options, Is.EqualTo(new[]
        {
            TraversalOptionRef.ForConnection(new ConnectionId("connection.z")),
            TraversalOptionRef.ForCrossing(new CrossingId("crossing.a")),
            TraversalOptionRef.ForWildernessRule("rule.walk", "v1")
        }));
        Assert.That(authority.PassageAuthority.OptionStates.Select(state => state.Condition),
            Is.EqualTo(new[] { PassageCondition.Available, PassageCondition.Available, PassageCondition.Available }));
        Assert.That(authority.PassageAuthority.OptionStates[1].ContentIdentity, Is.EqualTo("content.bridge"));

        SpatialAuthorityStore reverse = CreateAdjacentGeography();
        Assert.That(reverse.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.a"), boundary, new HexId("hex.a"), "content.bridge", "bridge-v1", null), out failure),
            Is.True, failure.ToString());
        Assert.That(reverse.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(TraversalOptionRef.ForConnection(new ConnectionId("connection.z")), boundary, "road", "rev-a"),
            PassageCondition.Available, out failure), Is.True, failure.ToString());
        Assert.That(reverse.PassageAuthority.TryRegisterWildernessRule(
            new PassageOptionRecord(TraversalOptionRef.ForWildernessRule("rule.walk", "v1"), boundary, "rule.walk", "v1"),
            PassageCondition.Available, out failure), Is.True, failure.ToString());
        Assert.That(reverse.PassageAuthority.OptionStates.Select(state => state.Option),
            Is.EqualTo(authority.PassageAuthority.OptionStates.Select(state => state.Option)));
    }

    [Test]
    public void EvaluationUsesOrderedDirectionCurrentConditionsAndExplicitBarrierRelations()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        HexBoundaryKey boundary = Boundary();
        BarrierId barrierId = new BarrierId("barrier.river");
        HexBoundaryKey secondBoundary = new HexBoundaryKey(new HexId("hex.other"), new HexId("hex.z"));
        Assert.That(authority.PassageAuthority.TryRegisterBarrier(
            new BarrierRecord(barrierId, "content.river-barrier", "river-v1", new[] { boundary, secondBoundary }), BarrierCondition.Active, out SpatialAuthorityFailure failure),
            Is.True, failure.ToString());
        TraversalOptionRef road = TraversalOptionRef.ForConnection(new ConnectionId("connection.road"));
        Assert.That(authority.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(road, boundary, "content.road", "road-v1"), PassageCondition.Available, out failure),
            Is.True, failure.ToString());
        Assert.That(authority.TryRegisterCrossing(
            new CrossingRecord(new CrossingId("crossing.bridge"), boundary, new HexId("hex.a"), "content.bridge", "bridge-v1", new[] { barrierId }, 0.5m), out failure),
            Is.True, failure.ToString());
        TraversalOptionRef bridge = TraversalOptionRef.ForCrossing(new CrossingId("crossing.bridge"));
        TraversalCostContext cost = new TraversalCostContext("walker", "profile-v2", 2m, 3m, 1.5m);

        Assert.That(authority.PassageAuthority.TryEvaluatePassage(
            new HexId("hex.a"), new HexId("hex.z"), road, cost, out PassageEvaluation roadResult, out failure), Is.True, failure.ToString());
        Assert.That(roadResult.IsAvailable, Is.False);
        Assert.That(roadResult.BlockingBarrierIds, Is.EqualTo(new[] { barrierId }));
        Assert.That(authority.PassageAuthority.BarrierStates.Single().Condition, Is.EqualTo(BarrierCondition.Active));
        Assert.That(authority.PassageAuthority.BarrierStates.Single().Barrier.Boundaries.Count, Is.EqualTo(2));
        Assert.That(authority.PassageAuthority.OptionStates.Single(state => state.Option.Equals(road)).ContentRevision,
            Is.EqualTo("road-v1"));
        Assert.That(roadResult.EstimatedEffort, Is.EqualTo(6m));
        Assert.That(roadResult.FromTerrainDefinitionId, Is.EqualTo("terrain.test"));
        Assert.That(roadResult.FromTerrainRevision, Is.EqualTo("terrain-v1"));
        Assert.That(roadResult.FromHexId, Is.EqualTo(new HexId("hex.a")));
        Assert.That(roadResult.ToHexId, Is.EqualTo(new HexId("hex.z")));

        Assert.That(authority.PassageAuthority.TryEvaluatePassage(
            new HexId("hex.z"), new HexId("hex.a"), bridge, cost, out PassageEvaluation bridgeResult, out failure), Is.True, failure.ToString());
        Assert.That(bridgeResult.IsAvailable, Is.True);
        Assert.That(bridgeResult.EstimatedEffort, Is.EqualTo(3m));
        Assert.That(bridgeResult.BlockingBarrierIds, Is.Empty);
        Assert.That(bridgeResult.Boundary, Is.EqualTo(roadResult.Boundary));
        Assert.That(bridgeResult.FromHexId, Is.EqualTo(new HexId("hex.z")));

        long revision = authority.Revision;
        Assert.That(authority.PassageAuthority.TryChangePassageCondition(boundary, bridge, PassageCondition.Closed, out failure), Is.True, failure.ToString());
        Assert.That(authority.Revision, Is.EqualTo(revision + 1));
        Assert.That(authority.PassageAuthority.TryEvaluatePassage(
            new HexId("hex.a"), new HexId("hex.z"), bridge, cost, out bridgeResult, out failure), Is.True, failure.ToString());
        Assert.That(bridgeResult.IsAvailable, Is.False);
        Assert.That(bridgeResult.Condition, Is.EqualTo(PassageCondition.Closed));

        Assert.That(authority.PassageAuthority.TryChangeBarrierCondition(barrierId, BarrierCondition.Removed, out failure), Is.True, failure.ToString());
        Assert.That(authority.PassageAuthority.TryEvaluatePassage(
            new HexId("hex.a"), new HexId("hex.z"), road, cost, out roadResult, out failure), Is.True, failure.ToString());
        Assert.That(roadResult.IsAvailable, Is.True);
    }

    [Test]
    public void RejectedMutationsAreAtomicAndRuntimeClonePreservesPassageTruth()
    {
        SpatialAuthorityStore source = CreateAdjacentGeography();
        HexBoundaryKey boundary = Boundary();
        TraversalOptionRef road = TraversalOptionRef.ForConnection(new ConnectionId("connection.road"));
        Assert.That(source.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(road, boundary, "content.road", "road-v1"), PassageCondition.Impaired, out SpatialAuthorityFailure failure),
            Is.True, failure.ToString());
        BarrierId barrierId = new BarrierId("barrier.river");
        Assert.That(source.PassageAuthority.TryRegisterBarrier(
            new BarrierRecord(barrierId, "content.barrier", "barrier-v1", new[] { boundary }), BarrierCondition.Active, out failure),
            Is.True, failure.ToString());
        TraversalOptionRef bridge = TraversalOptionRef.ForCrossing(new CrossingId("crossing.bridge"));
        Assert.That(source.TryRegisterCrossing(
            new CrossingRecord(bridge.CrossingId, boundary, new HexId("hex.a"), "content.bridge", "bridge-v1", new[] { barrierId }),
            out failure), Is.True, failure.ToString());
        Assert.That(source.PassageAuthority.TryChangePassageCondition(boundary, bridge, PassageCondition.Closed, out failure), Is.True, failure.ToString());
        long beforeRejectedMutation = source.Revision;
        Assert.That(source.PassageAuthority.TryChangePassageCondition(
            new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.isolated")), road, PassageCondition.Closed, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.BoundaryNotAdjacent));
        Assert.That(source.Revision, Is.EqualTo(beforeRejectedMutation));

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L), Array.Empty<CityRuntime>(), Array.Empty<NpcRuntime>(), spatialAuthorityStore: source);
        SpatialAuthorityStore clone = runtime.SpatialAuthorityStore;
        Assert.That(clone, Is.Not.SameAs(source));
        Assert.That(clone.Revision, Is.EqualTo(source.Revision));
        Assert.That(clone.PassageAuthority.ValidateInvariants().IsValid, Is.True);
        Assert.That(clone.PassageAuthority.OptionStates.Single(state => state.Option.Equals(bridge)).Condition,
            Is.EqualTo(PassageCondition.Closed));
        Assert.That(clone.PassageAuthority.BarrierStates.Single().Condition, Is.EqualTo(BarrierCondition.Active));
        Assert.That(clone.PassageAuthority.TryEvaluatePassage(
            new HexId("hex.a"), new HexId("hex.z"), road,
            new TraversalCostContext("walker", "profile-v1", 1m, 1m, 1.5m), out PassageEvaluation result, out failure), Is.True, failure.ToString());
        Assert.That(result.Condition, Is.EqualTo(PassageCondition.Impaired));
        Assert.That(result.IsAvailable, Is.False);
        Assert.That(result.BlockingBarrierIds, Is.EqualTo(new[] { barrierId }));
        Assert.That(result.EstimatedEffort, Is.EqualTo(1.5m));
        Assert.That(source.PassageAuthority.TryChangePassageCondition(boundary, road, PassageCondition.Closed, out failure), Is.True, failure.ToString());
        Assert.That(clone.PassageAuthority.TryEvaluatePassage(
            new HexId("hex.a"), new HexId("hex.z"), road,
            new TraversalCostContext("walker", "profile-v1", 1m, 1m, 1.5m), out result, out failure), Is.True, failure.ToString());
        Assert.That(result.Condition, Is.EqualTo(PassageCondition.Impaired), "clone state must not alias source condition state");
        Assert.That(clone.PassageAuthority.TryChangePassageCondition(boundary, bridge, PassageCondition.Available, out failure), Is.True, failure.ToString());
        Assert.That(source.PassageAuthority.TryGetPassageCondition(boundary, bridge, out PassageCondition sourceBridgeCondition, out failure), Is.True, failure.ToString());
        Assert.That(sourceBridgeCondition, Is.EqualTo(PassageCondition.Closed));
    }

    [Test]
    public void FaultedRuntimeGuardsPassageMutationsWhileKeepingPassageQueriesReadable()
    {
        SpatialAuthorityStore source = CreateAdjacentGeography();
        HexBoundaryKey boundary = Boundary();
        TraversalOptionRef road = TraversalOptionRef.ForConnection(new ConnectionId("connection.road"));
        Assert.That(source.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(road, boundary, "content.road", "road-v1"), PassageCondition.Available, out SpatialAuthorityFailure failure),
            Is.True, failure.ToString());
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L), Array.Empty<CityRuntime>(), Array.Empty<NpcRuntime>(), spatialAuthorityStore: source);
        MethodInfo markFaulted = typeof(SimulationRuntime).GetMethod(
            "MarkAuthoritativeMutationFaulted", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(markFaulted, Is.Not.Null);
        markFaulted.Invoke(runtime, new object[] { AuthoritativeMutationFaultReason.IntegrityRestoreFailed });

        SpatialAuthorityStore world = runtime.SpatialAuthorityStore;
        long revision = world.Revision;
        Assert.That(world.PassageAuthority.TryChangePassageCondition(boundary, road, PassageCondition.Closed, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.RuntimeFaulted));
        Assert.That(world.Revision, Is.EqualTo(revision));
        Assert.That(world.PassageAuthority.TryEvaluatePassage(
            new HexId("hex.a"), new HexId("hex.z"), road,
            new TraversalCostContext("walker", "profile-v1", 1m, 1m, 1.5m), out PassageEvaluation result, out failure), Is.True, failure.ToString());
        Assert.That(result.Condition, Is.EqualTo(PassageCondition.Available));
    }

    [Test]
    public void RevisionOverflowRejectsPassageMutationWithoutPublishingOptionState()
    {
        SpatialAuthorityStore authority = CreateAdjacentGeography();
        FieldInfo revisionField = typeof(SpatialAuthorityStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(revisionField, Is.Not.Null);
        revisionField.SetValue(authority, long.MaxValue);
        HexBoundaryKey boundary = Boundary();
        TraversalOptionRef road = TraversalOptionRef.ForConnection(new ConnectionId("connection.overflow"));

        Assert.That(authority.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(road, boundary, "content.road", "road-v1"), PassageCondition.Available,
            out SpatialAuthorityFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialAuthorityFailureCode.RevisionOverflow));
        Assert.That(authority.PassageAuthority.Options, Is.Empty);
        Assert.That(authority.Revision, Is.EqualTo(long.MaxValue));
    }

    private static SpatialAuthorityStore CreateAdjacentGeography()
    {
        HexRecord[] hexes =
        {
            CreateHex("hex.z", 0, 0),
            CreateHex("hex.a", 1, 0),
            CreateHex("hex.other", 0, 1),
            CreateHex("hex.isolated", 9, 9)
        };
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("scale.test", "fixture", "v1", 1m, "step"),
            hexes);
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        return authority;
    }

    private static HexBoundaryKey Boundary() => new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.z"));

    private static HexRecord CreateHex(string id, int q, int r)
    {
        return new HexRecord(
            new HexId(id),
            new HexCoordinate(q, r),
            new TerrainReference(new TerrainDefinitionId("terrain.test"), "terrain-v1"));
    }
}
