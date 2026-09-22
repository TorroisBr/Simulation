using NUnit.Framework;

public sealed class BattleSpatialBindingTests
{
    [Test]
    public void PendingBattle_AllowsMissingAndExplicitSpatialReferences_ButActiveRequiresOne()
    {
        ArmedForceStore forces = CreateForces();
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        SpatialAuthorityStore authority = CreateAuthority();
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);

        BattleId pendingId = new BattleId("battle-pending");
        Assert.That(battles.TryRegister(CreateBattle(pendingId), out _), Is.True);
        long revision = battles.Revision;
        Assert.That(battles.TryStart(pendingId, 1L, out PersistentStateFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(PersistentStateFailureCode.BattleLocationRequired));
        Assert.That(battles.Revision, Is.EqualTo(revision));

        SpatialReference hex = SpatialReference.ForHex(new HexId("hex-main"));
        Assert.That(battles.TryStart(pendingId, 1L, hex, out _), Is.True);
        Assert.That(battles.TryGet(pendingId, out PersistentBattleRecord active), Is.True);
        Assert.That(active.LocationReference, Is.EqualTo(hex));

    }

    [Test]
    public void PendingBattle_AcceptsHexLocationAndSubLocationReferences()
    {
        ArmedForceStore forces = CreateForces();
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        SpatialAuthorityStore authority = CreateSubLocationWorld(out LocalTopologyStore topologyStore, out SpatialReference subLocation);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority, topologyStore);

        Assert.That(battles.TryRegister(CreateBattle(new BattleId("battle-hex"), SpatialReference.ForHex(new HexId("hex-main"))), out _), Is.True);
        Assert.That(battles.TryRegister(CreateBattle(new BattleId("battle-location"), SpatialReference.ForLocation(new LocationId("location-main"))), out _), Is.True);
        Assert.That(battles.TryRegister(CreateBattle(new BattleId("battle-sublocation"), subLocation), out _), Is.True);
        long revision = battles.Revision;
        SpatialReference missingPlace = SpatialReference.ForSubLocation(
            subLocation.TopologyOwnerKind.Value,
            subLocation.TopologyOwnerRuntimeId,
            "missing-place");
        Assert.That(battles.TryRegister(CreateBattle(new BattleId("battle-bad-sublocation"), missingPlace), out PersistentStateFailure badSubLocation), Is.False);
        Assert.That(badSubLocation.Code, Is.EqualTo(PersistentStateFailureCode.BattleLocationInvalid));
        Assert.That(battles.Revision, Is.EqualTo(revision));
        Assert.That(battles.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void InvalidSpatialReference_IsRejectedAtomically_AndParticipantLocationIsNotInferred()
    {
        ArmedForceStore forces = CreateForces("force-participant");
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, CreateAuthority());

        BattleId missingHexId = new BattleId("battle-missing-hex");
        long revision = battles.Revision;
        Assert.That(battles.TryRegister(CreateBattle(missingHexId, SpatialReference.ForHex(new HexId("missing-hex"))), out PersistentStateFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(PersistentStateFailureCode.BattleLocationInvalid));
        Assert.That(battles.Revision, Is.EqualTo(revision));
        Assert.That(battles.TryGet(missingHexId, out _), Is.False);

        BattleId inferredId = new BattleId("battle-no-inference");
        Assert.That(battles.TryRegister(CreateBattle(inferredId, null, new BattleParticipantBinding(
            new BattleParticipantBindingId("binding-force"),
            inferredId,
            new BattleSideId("side-a"),
            new ArmedForceId("force-participant"))), out _), Is.True);
        revision = battles.Revision;
        Assert.That(battles.TryStart(inferredId, 1L, out PersistentStateFailure noInference), Is.False);
        Assert.That(noInference.Code, Is.EqualTo(PersistentStateFailureCode.BattleLocationRequired));
        Assert.That(battles.Revision, Is.EqualTo(revision));
        Assert.That(battles.TryGet(inferredId, out PersistentBattleRecord stillPending), Is.True);
        Assert.That(stillPending.LifecycleState, Is.EqualTo(BattleLifecycleState.Pending));
        Assert.That(stillPending.LocationReference, Is.Null);
    }

    [Test]
    public void ActiveBattleLocation_IsImmutable_AndResolutionRemainsDeferred()
    {
        ArmedForceStore forces = CreateForces();
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        SpatialAuthorityStore authority = CreateAuthority();
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);
        BattleId id = new BattleId("battle-immutable");
        Assert.That(battles.TryRegister(CreateBattle(id), out _), Is.True);
        SpatialReference location = SpatialReference.ForLocation(new LocationId("location-main"));
        Assert.That(battles.TryStart(id, 1L, location, out _), Is.True);
        long revision = battles.Revision;
        Assert.That(battles.TryStart(id, 2L, SpatialReference.ForHex(new HexId("hex-main")), out PersistentStateFailure secondStart), Is.False);
        Assert.That(secondStart.Code, Is.EqualTo(PersistentStateFailureCode.InvalidLifecycle));
        Assert.That(battles.Revision, Is.EqualTo(revision));
        Assert.That(battles.TryGet(id, out PersistentBattleRecord record), Is.True);
        Assert.That(record.LocationReference, Is.EqualTo(location));
        Assert.That(record.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
    }

    [Test]
    public void SimulationRuntime_ComposesBattleAgainstClonedWorldSpatialAuthority()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        Assert.That(forces.TryRegister(new ArmedForceRecord(new ArmedForceId("force-runtime"), "Force", 0L), out _), Is.True);
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        SpatialAuthorityStore authority = CreateAuthority();
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);
        BattleId id = new BattleId("battle-runtime-spatial");
        Assert.That(battles.TryRegister(CreateBattle(id, SpatialReference.ForHex(new HexId("hex-main"))), out _), Is.True);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L),
            null,
            null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: forces,
            conflictStore: conflicts,
            warStore: wars,
            battleStore: battles,
            spatialAuthorityStore: authority);

        Assert.That(runtime.BattleStore.SpatialAuthorityStore, Is.Not.SameAs(authority));
        Assert.That(runtime.BattleStore.SpatialAuthorityStore.TryGet(new HexId("hex-main"), out _), Is.True);
        Assert.That(runtime.BattleStore.TryGet(id, out PersistentBattleRecord composed), Is.True);
        Assert.That(composed.LocationReference.StableKey, Is.EqualTo("hex:hex-main"));
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("external-only")), out _), Is.True);
        Assert.That(runtime.BattleStore.SpatialAuthorityStore.TryGet(new HexId("external-only"), out _), Is.False);
    }

    [Test]
    public void Diagnostics_IncludeBattleLocation_AndDetectPendingLocationChanges()
    {
        SpatialAuthorityStore authority = CreateAuthority();
        ArmedForceStore forces = CreateForces();
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);
        BattleId id = new BattleId("battle-diagnostics");
        SpatialReference location = SpatialReference.ForLocation(new LocationId("location-main"));
        Assert.That(battles.TryRegister(CreateBattle(id, location), out _), Is.True);

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            armedForceStore: forces,
            conflictStore: conflicts,
            warStore: wars,
            battleStore: battles,
            spatialAuthorityStore: authority));
        Assert.That(snapshot.Battles[0].LocationReferenceKey, Is.EqualTo("location:location-main"));
        Assert.That(WorldStateCanonicalWriter.Write(snapshot), Does.Contain("location:location-main"));
        Assert.That(WorldStateSnapshotFormatter.Format(snapshot), Does.Contain("location:location-main"));
        Assert.That(WorldStateDiagnostics.Validate(snapshot).IsValid, Is.True);

        WorldStateSnapshot before = new WorldStateSnapshot(
            0L,
            spatial: snapshot.Spatial,
            battles: new[] { new WorldStateBattleSnapshot(id.Value, 0L, null, BattleLifecycleState.Pending, null, null, SpatialReference.ForHex(new HexId("hex-main"))) },
            battleRevision: 1L);
        WorldStateSnapshot after = new WorldStateSnapshot(
            0L,
            spatial: snapshot.Spatial,
            battles: new[] { new WorldStateBattleSnapshot(id.Value, 0L, null, BattleLifecycleState.Pending, null, null, location) },
            battleRevision: 2L);
        Assert.That(WorldStateDiagnostics.Compare(before, after).IsEmpty, Is.False);

        WorldStateSnapshot activeWithoutLocation = new WorldStateSnapshot(
            0L,
            battles: new[] { new WorldStateBattleSnapshot("battle-active-missing-location", 0L, 0L, BattleLifecycleState.Active, null, null) },
            battleRevision: 1L);
        Assert.That(WorldStateDiagnostics.Validate(activeWithoutLocation).Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "ActiveBattleLocationMissing"));

        WorldStateSnapshot malformedLocation = new WorldStateSnapshot(
            0L,
            battles: new[] { new WorldStateBattleSnapshot("battle-malformed-location", 0L, null, BattleLifecycleState.Pending, null, null, locationReferenceKey: "location:raw-only") },
            battleRevision: 1L);
        Assert.That(WorldStateDiagnostics.Validate(malformedLocation).Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "BattleLocationMalformed"));
    }

    [Test]
    public void SpatialBattleCanonicalProjection_IsIndependentOfInsertionOrder()
    {
        WorldStateSnapshot first = BuildOrderedSnapshot(false);
        WorldStateSnapshot second = BuildOrderedSnapshot(true);
        Assert.That(WorldStateCanonicalWriter.Write(first), Is.EqualTo(WorldStateCanonicalWriter.Write(second)));
        Assert.That(WorldStateDiagnostics.Compare(first, second).IsEmpty, Is.True);
    }

    private static PersistentBattleRecord CreateBattle(
        BattleId id,
        SpatialReference location = null,
        BattleParticipantBinding binding = null)
    {
        return new PersistentBattleRecord(
            id,
            0L,
            sides: new[]
            {
                new BattleStateSide(id, new BattleSideId("side-a"), "A"),
                new BattleStateSide(id, new BattleSideId("side-b"), "B")
            },
            participantBindings: binding == null ? null : new[] { binding },
            locationReference: location);
    }

    private static ArmedForceStore CreateForces(params string[] ids)
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        foreach (string id in ids)
        {
            Assert.That(store.TryRegister(new ArmedForceRecord(new ArmedForceId(id), id, 0L), out _), Is.True);
        }
        return store;
    }

    private static SpatialAuthorityStore CreateAuthority()
    {
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-main")), out _), Is.True);
        Assert.That(authority.TryRegisterLocation(new LocationRecord(new LocationId("location-main"), new HexId("hex-main")), out _), Is.True);
        return authority;
    }

    private static SpatialAuthorityStore CreateSubLocationWorld(out LocalTopologyStore topologyStore, out SpatialReference subLocation)
    {
        CityRuntime city = SimulationTestFactory.CreateCity("battle-city", "legacy-location");
        RuntimeIdentityRegistry registry = new RuntimeIdentityRegistry();
        Assert.That(registry.RegisterCity(city), Is.True);
        LocalTopologyRuntime topology = new LocalTopologyRuntime(LocalTopologyOwnerReference.ForCity(city), registry);
        LocalPlaceRuntime place = new LocalPlaceRuntime("battle-place");
        Assert.That(topology.AddPlace(place, null, true), Is.True);
        topologyStore = new LocalTopologyStore(registry);
        Assert.That(topologyStore.Add(topology), Is.True);
        SpatialAuthorityStore authority = CreateAuthority();
        Assert.That(authority.TryBindLocalTopology(topology, new LocationId("location-main"), out _), Is.True);
        subLocation = SpatialReference.ForSubLocation(LocalTopologyOwnerKind.City, city.RuntimeId, place.RuntimeId);
        return authority;
    }

    private static WorldStateSnapshot BuildOrderedSnapshot(bool reverse)
    {
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        if (reverse)
        {
            Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-b")), out _), Is.True);
            Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        }
        else
        {
            Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
            Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-b")), out _), Is.True);
        }

        ArmedForceStore forces = CreateForces();
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);
        BattleId firstId = new BattleId("battle-a");
        BattleId secondId = new BattleId("battle-b");
        PersistentBattleRecord first = CreateBattle(firstId, SpatialReference.ForHex(new HexId("hex-a")));
        PersistentBattleRecord second = CreateBattle(secondId, SpatialReference.ForHex(new HexId("hex-b")));
        if (reverse)
        {
            Assert.That(battles.TryRegister(second, out _), Is.True);
            Assert.That(battles.TryRegister(first, out _), Is.True);
        }
        else
        {
            Assert.That(battles.TryRegister(first, out _), Is.True);
            Assert.That(battles.TryRegister(second, out _), Is.True);
        }

        return WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            armedForceStore: forces,
            conflictStore: conflicts,
            warStore: wars,
            battleStore: battles,
            spatialAuthorityStore: authority));
    }
}
