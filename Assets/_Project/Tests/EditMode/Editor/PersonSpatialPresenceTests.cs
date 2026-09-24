using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class PersonSpatialPresenceTests
{
    [Test]
    public void CityAndSiteBindingsAreStableUniqueAndDoNotImplyAccess()
    {
        SpatialAuthorityStore spatial = CreateGeography();
        LegacySpatialAnchorBindingStore bindings = new LegacySpatialAnchorBindingStore(spatial);
        Assert.That(bindings.TryBindCity("city.north", new LocationId("location.a"), out _), Is.True);
        Assert.That(bindings.TryBindSite("site.ruin", new LocationId("location.b"), out _), Is.True);
        Assert.That(bindings.TryBindCity("city.north", new LocationId("location.b"), out SpatialAnchorBindingFailure rebound), Is.False);
        Assert.That(rebound.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.OwnerAlreadyBound));
        Assert.That(bindings.TryBindSite("site.other", new LocationId("location.a"), out SpatialAnchorBindingFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(SpatialAnchorBindingFailureCode.LocationAlreadyBound));
        Assert.That(bindings.Bindings.Select(x => x.StableKey), Is.Ordered);
        Assert.That(bindings.Revision, Is.EqualTo(2));
        Assert.That(bindings.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void PersonIdentityOwnsAtAndInTransitAcrossRepresentationAndDeath()
    {
        SpatialAuthorityStore spatial = CreateGeography();
        PersonStore people = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person.stable"), 0L, 5L);
        PersonId crossingPerson = new PersonId("person.crossing-position");
        Assert.That(people.TryRegister(person, out _), Is.True);
        Assert.That(people.TryRegister(new PersonRuntime(crossingPerson), out _), Is.True);
        FakeTraversalResolver resolver = new FakeTraversalResolver();
        TraversalOptionRef crossingOption = TraversalOptionRef.ForCrossing(new CrossingId("crossing.bridge"));
        HexBoundaryKey boundary = new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b"));
        resolver.Allow(crossingOption, boundary);
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(people, spatial, resolver);

        Assert.That(positions.TrySetAt(crossingPerson, StablePositionReference.ForCrossing(new CrossingId("crossing.bridge")), out _), Is.True,
            "Crossing remains a supported stable factual position reference.");
        Assert.That(positions.TrySetAt(person.PersonId, StablePositionReference.ForHex(new HexId("hex.a")), out _), Is.True);
        Assert.That(positions.TryBeginTransit(person.PersonId, crossingOption, boundary, new HexId("hex.a"), new HexId("hex.b"), out _), Is.True);
        Assert.That(positions.TryGetPosition(new PersonId("person.stable"), out PersonSpatialPosition during), Is.True);
        Assert.That(during.IsInTransit, Is.True);
        Assert.That(during.Transit.LastFullyReachedReference.StableKey, Is.EqualTo("hex:hex.a"));
        Assert.That(during.Transit.ProgressTicks, Is.Zero);
        Assert.That(positions.TryAdvanceTransit(person.PersonId, 375, out _), Is.True);
        Assert.That(positions.TryGetPosition(person.PersonId, out during), Is.True);
        Assert.That(during.Transit.ProgressTicks, Is.EqualTo(375));
        PersonSpatialPositionStore transitClone = Clone(positions, people, spatial, resolver);
        Assert.That(transitClone.TryGetPosition(person.PersonId, out PersonSpatialPosition clonedTransit), Is.True);
        Assert.That(clonedTransit.IsInTransit, Is.True);
        Assert.That(clonedTransit.Transit.Option, Is.EqualTo(during.Transit.Option));
        Assert.That(clonedTransit.Transit.Boundary, Is.EqualTo(during.Transit.Boundary));
        Assert.That(clonedTransit.Transit.FromHexId, Is.EqualTo(during.Transit.FromHexId));
        Assert.That(clonedTransit.Transit.ToHexId, Is.EqualTo(during.Transit.ToHexId));
        Assert.That(clonedTransit.Transit.LastFullyReachedReference, Is.EqualTo(during.Transit.LastFullyReachedReference));
        Assert.That(clonedTransit.Transit.ProgressTicks, Is.EqualTo(during.Transit.ProgressTicks));
        Assert.That(positions.TryAdvanceTransit(person.PersonId, 625, out _), Is.True);
        Assert.That(positions.TryArrive(person.PersonId, StablePositionReference.ForHex(new HexId("hex.b")), out _), Is.True);
        Assert.That(positions.TryGetPosition(new PersonId("person.stable"), out PersonSpatialPosition arrived), Is.True);
        Assert.That(arrived.IsInTransit, Is.False);
        Assert.That(arrived.Position.StableKey, Is.EqualTo("hex:hex.b"));
        Assert.That(person.IsDeadAt(5), Is.True);
        Assert.That(positions.TryGetPosition(person.PersonId, out _), Is.True, "Position authority retains PersonId truth without consulting loaded NPC state or life state.");
        Assert.That(positions.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void DirectAtRelocationDuringTransitIsRejectedWithoutChangingPositionOrRevision()
    {
        SpatialAuthorityStore spatial = CreateGeography();
        PersonStore people = new PersonStore();
        PersonId person = new PersonId("person.no-relocate");
        people.TryRegister(new PersonRuntime(person), out _);
        FakeTraversalResolver resolver = new FakeTraversalResolver();
        HexBoundaryKey boundary = new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b"));
        TraversalOptionRef option = TraversalOptionRef.ForWildernessRule("rule.explicit", "v1");
        resolver.Allow(option, boundary);
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(people, spatial, resolver);
        Assert.That(positions.TrySetAt(person, StablePositionReference.ForHex(new HexId("hex.a")), out _), Is.True);
        Assert.That(positions.TryBeginTransit(person, option, boundary, new HexId("hex.a"), new HexId("hex.b"), out _), Is.True);
        long revision = positions.Revision;
        Assert.That(positions.TrySetAt(person, StablePositionReference.ForHex(new HexId("hex.b")), out PersonSpatialPositionFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PersonSpatialPositionFailureCode.PositionAlreadyRegistered));
        Assert.That(positions.Revision, Is.EqualTo(revision));
        Assert.That(positions.TryGetPosition(person, out PersonSpatialPosition unchanged), Is.True);
        Assert.That(unchanged.IsInTransit, Is.True);
        Assert.That(unchanged.Transit.ProgressTicks, Is.Zero);
        Assert.That(unchanged.Transit.LastFullyReachedReference.StableKey, Is.EqualTo("hex:hex.a"));
    }

    [Test]
    public void RegionalTransitRequiresHexEndpointsAndRejectsEarlyOrNonHexArrivalAtomically()
    {
        SpatialAuthorityStore spatial = CreateGeography();
        PersonStore people = new PersonStore();
        PersonId locationOriginPerson = new PersonId("person.location-origin");
        PersonId traveler = new PersonId("person.regional");
        people.TryRegister(new PersonRuntime(locationOriginPerson), out _);
        people.TryRegister(new PersonRuntime(traveler), out _);
        FakeTraversalResolver resolver = new FakeTraversalResolver();
        HexBoundaryKey boundary = new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b"));
        TraversalOptionRef option = TraversalOptionRef.ForWildernessRule("rule.regional", "v1");
        resolver.Allow(option, boundary);
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(people, spatial, resolver);

        Assert.That(positions.TrySetAt(locationOriginPerson, StablePositionReference.ForLocation(new LocationId("location.a")), out _), Is.True);
        long locationRevision = positions.Revision;
        Assert.That(positions.TryBeginTransit(locationOriginPerson, option, boundary, new HexId("hex.a"), new HexId("hex.b"), out PersonSpatialPositionFailure originFailure), Is.False);
        Assert.That(originFailure.Code, Is.EqualTo(PersonSpatialPositionFailureCode.PositionMismatch));
        Assert.That(positions.Revision, Is.EqualTo(locationRevision));
        Assert.That(positions.TryGetPosition(locationOriginPerson, out PersonSpatialPosition locationAt), Is.True);
        Assert.That(locationAt.Position.StableKey, Is.EqualTo("location:location.a"));

        Assert.That(positions.TrySetAt(traveler, StablePositionReference.ForHex(new HexId("hex.a")), out _), Is.True);
        Assert.That(positions.TryBeginTransit(traveler, option, boundary, new HexId("hex.a"), new HexId("hex.b"), out _), Is.True);
        long transitRevision = positions.Revision;
        Assert.That(positions.TryArrive(traveler, StablePositionReference.ForHex(new HexId("hex.b")), out PersonSpatialPositionFailure early), Is.False);
        Assert.That(early.Code, Is.EqualTo(PersonSpatialPositionFailureCode.ArrivalNotReady));
        Assert.That(positions.Revision, Is.EqualTo(transitRevision));
        Assert.That(positions.TryAdvanceTransit(traveler, TraversalProgress.CompleteProgressTicks, out _), Is.True);
        long readyRevision = positions.Revision;
        Assert.That(positions.TryArrive(traveler, StablePositionReference.ForLocation(new LocationId("location.b")), out PersonSpatialPositionFailure nonHexArrival), Is.False);
        Assert.That(nonHexArrival.Code, Is.EqualTo(PersonSpatialPositionFailureCode.PositionMismatch));
        Assert.That(positions.Revision, Is.EqualTo(readyRevision));
        Assert.That(positions.TryGetPosition(traveler, out PersonSpatialPosition stillTransit), Is.True);
        Assert.That(stillTransit.IsInTransit, Is.True);
        Assert.That(stillTransit.Transit.ProgressTicks, Is.EqualTo(TraversalProgress.CompleteProgressTicks));
        Assert.That(positions.TryArrive(traveler, StablePositionReference.ForHex(new HexId("hex.b")), out _), Is.True);
        Assert.That(positions.TryGetPosition(traveler, out PersonSpatialPosition arrived), Is.True);
        Assert.That(arrived.Position.StableKey, Is.EqualTo("hex:hex.b"));
    }

    [Test]
    public void MutationGuardBlocksPositionChangesAfterWorldFault()
    {
        SpatialAuthorityStore spatial = CreateGeography();
        PersonStore people = new PersonStore();
        PersonId person = new PersonId("person.guarded");
        people.TryRegister(new PersonRuntime(person), out _);
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(people, spatial, new FakeTraversalResolver());
        Assert.That(positions.TrySetAt(person, StablePositionReference.ForHex(new HexId("hex.a")), out _), Is.True);
        long revision = positions.Revision;

        System.Type guardType = typeof(PersonSpatialPositionStore).Assembly.GetType("AuthoritativeMutationGuard");
        object guard = System.Activator.CreateInstance(guardType, true);
        MethodInfo bind = typeof(PersonSpatialPositionStore).GetMethod("TryBindMutationGuard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That((bool)bind.Invoke(positions, new[] { guard }), Is.True);
        MethodInfo fault = guardType.GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic);
        System.Type reasonType = guardType.Assembly.GetType("AuthoritativeMutationFaultReason");
        object reason = System.Enum.Parse(reasonType, "RollbackRestoreFailed");
        fault.Invoke(guard, new[] { reason });

        Assert.That(positions.TrySetAt(person, StablePositionReference.ForHex(new HexId("hex.b")), out PersonSpatialPositionFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PersonSpatialPositionFailureCode.RuntimeFaulted));
        Assert.That(positions.Revision, Is.EqualTo(revision));
        Assert.That(positions.TryGetPosition(person, out PersonSpatialPosition unchanged), Is.True);
        Assert.That(unchanged.Position.StableKey, Is.EqualTo("hex:hex.a"));
    }

    [Test]
    public void RejectedStaleUnknownAndRuntimeOnlyReferencesAreAtomic()
    {
        SpatialAuthorityStore spatial = CreateGeography();
        PersonStore people = new PersonStore();
        PersonId person = new PersonId("person.atomic");
        Assert.That(people.TryRegister(new PersonRuntime(person), out _), Is.True);
        FakeTraversalResolver resolver = new FakeTraversalResolver();
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(people, spatial, resolver);
        Assert.That(positions.TrySetAt(person, SpatialReference.ForSubLocation(LocalTopologyOwnerKind.City, "runtime-city", "runtime-square"), out PersonSpatialPositionFailure subLocationFailure), Is.False);
        Assert.That(subLocationFailure.Code, Is.EqualTo(PersonSpatialPositionFailureCode.InvalidReference));
        Assert.That(positions.TrySetAt(person, StablePositionReference.ForHex(new HexId("hex.missing")), out PersonSpatialPositionFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(PersonSpatialPositionFailureCode.SpatialReferenceNotRegistered));
        Assert.That(positions.TrySetAt(person, StablePositionReference.ForHex(new HexId("hex.a")), out _), Is.True);
        long revision = positions.Revision;
        Assert.That(positions.TryBeginTransit(person, TraversalOptionRef.ForWildernessRule("rule.bad", "v1"),
            new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b")), new HexId("hex.a"), new HexId("hex.b"), out PersonSpatialPositionFailure stale), Is.False);
        Assert.That(stale.Code, Is.EqualTo(PersonSpatialPositionFailureCode.TraversalOptionNotRegistered));
        Assert.That(positions.Revision, Is.EqualTo(revision));
        Assert.That(positions.TryGetPosition(person, out PersonSpatialPosition unchanged), Is.True);
        Assert.That(unchanged.Position.StableKey, Is.EqualTo("hex:hex.a"));
        Assert.That(positions.TryBeginTransit(new PersonId("person.unknown"), TraversalOptionRef.ForWildernessRule("rule.ok", "v1"),
            new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b")), new HexId("hex.a"), new HexId("hex.b"), out PersonSpatialPositionFailure unregistered), Is.False);
        Assert.That(unregistered.Code, Is.EqualTo(PersonSpatialPositionFailureCode.PersonNotRegistered));
        Assert.That(positions.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void PositionOrderingAndCloneAreDeterministic()
    {
        SpatialAuthorityStore spatial = CreateGeography();
        PersonStore people = new PersonStore();
        people.TryRegister(new PersonRuntime(new PersonId("person.z")), out _);
        people.TryRegister(new PersonRuntime(new PersonId("person.a")), out _);
        FakeTraversalResolver resolver = new FakeTraversalResolver();
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(people, spatial, resolver);
        positions.TrySetAt(new PersonId("person.z"), StablePositionReference.ForHex(new HexId("hex.b")), out _);
        positions.TrySetAt(new PersonId("person.a"), StablePositionReference.ForHex(new HexId("hex.a")), out _);
        Assert.That(positions.Positions.Select(x => x.PersonId.Value), Is.EqualTo(new[] { "person.a", "person.z" }));
        MethodInfo cloneMethod = typeof(PersonSpatialPositionStore).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(cloneMethod, Is.Not.Null);
        PersonSpatialPositionStore clone = Clone(positions, people, spatial, resolver);
        Assert.That(clone.Revision, Is.EqualTo(positions.Revision));
        Assert.That(clone.Positions.Select(x => x.StableKey), Is.EqualTo(positions.Positions.Select(x => x.StableKey)));
        Assert.That(clone.Positions[0].Position, Is.Not.SameAs(positions.Positions[0].Position));
        Assert.That(clone.ValidateInvariants().IsValid, Is.True);
    }

    private static PersonSpatialPositionStore Clone(PersonSpatialPositionStore source, PersonStore people, SpatialAuthorityStore spatial, ISpatialTraversalOptionResolver resolver)
    {
        MethodInfo cloneMethod = typeof(PersonSpatialPositionStore).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(cloneMethod, Is.Not.Null);
        return (PersonSpatialPositionStore)cloneMethod.Invoke(source, new object[] { people, spatial, resolver });
    }

    private static SpatialAuthorityStore CreateGeography()
    {
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("fixture.scale", "fixture", "v1", 1m, "unit"),
            new[]
            {
                GeographicHex("hex.a", 0, 0),
                GeographicHex("hex.b", 1, 0)
            },
            new[]
            {
                new LocationRecord(new LocationId("location.a"), new HexId("hex.a")),
                new LocationRecord(new LocationId("location.b"), new HexId("hex.b"))
            });
        Assert.That(spatial.TryComposeGeography(geography, out SpatialAuthorityFailure composeFailure), Is.True, composeFailure.ToString());
        Assert.That(spatial.TryRegisterCrossing(new CrossingRecord(new CrossingId("crossing.bridge"), new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b")), new HexId("hex.a")), out SpatialAuthorityFailure crossingFailure), Is.True, crossingFailure.ToString());
        return spatial;
    }

    private static HexRecord GeographicHex(string id, int q, int r) => new HexRecord(new HexId(id), new HexCoordinate(q, r),
        new TerrainReference(new TerrainDefinitionId("terrain.fixture"), "v1"));

    private sealed class FakeTraversalResolver : ISpatialTraversalOptionResolver
    {
        private readonly HashSet<string> keys = new HashSet<string>();
        public void Allow(TraversalOptionRef option, HexBoundaryKey boundary) => keys.Add(Key(option, boundary));
        public bool TryResolveTraversalOption(TraversalOptionRef option, HexBoundaryKey boundary, out string failure)
        {
            string key = Key(option, boundary);
            bool found = keys.Contains(key);
            failure = found ? string.Empty : "Traversal option is unknown or stale.";
            return found;
        }
        private static string Key(TraversalOptionRef option, HexBoundaryKey boundary) => option.Kind + ":"
            + (option.ConnectionId?.Value ?? option.CrossingId?.Value ?? option.RuleIdentity + "@" + option.RuleVersion) + ":" + boundary;
    }
}
