using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EMilitaryOwnerSnapshotTests
{
    [Test]
    public void EmptySnapshot_CapturesFiveExactSectionsAndStagesWithoutChangingLiveOwners()
    {
        MilitaryWorld live = CreateWorld(populated: false);
        P12EMilitaryOwnerSnapshot first = CaptureOrFail(live);
        P12EMilitaryOwnerSnapshot second = CaptureOrFail(live);

        Assert.That(first.SchemaVersion, Is.EqualTo(P12EMilitaryOwnerSnapshot.CurrentSchemaVersion));
        Assert.That(first.ForceCount, Is.Zero);
        Assert.That(first.ContingentCount, Is.Zero);
        Assert.That(first.RelevantPersonCount, Is.Zero);
        Assert.That(first.ManpowerStateCount, Is.Zero);
        Assert.That(first.PositionCount, Is.Zero);
        Assert.That(first.ArmedForceRevision, Is.Zero);
        Assert.That(first.ManpowerRevision, Is.Zero);
        Assert.That(first.PositionRevision, Is.Zero);
        Assert.That(first.Forces, Is.Empty);
        Assert.That(first.Contingents, Is.Empty);
        Assert.That(first.RelevantPersons, Is.Empty);
        Assert.That(first.ManpowerStates, Is.Empty);
        Assert.That(first.Positions, Is.Empty);
        Assert.That(first.Forces, Is.Not.SameAs(second.Forces));

        PersonStore stagedPersons = new PersonStore();
        SpatialAuthorityStore stagedSpatial = CreateSpatial();
        Assert.That(first.TryStage(stagedPersons, stagedSpatial, null,
            out ArmedForceStore stagedForce,
            out ContingentManpowerStateStore stagedManpower,
            out ArmedForceSpatialStateStore stagedPositions,
            out P12EMilitaryOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(stagedForce, Is.Not.SameAs(live.Forces));
        Assert.That(stagedManpower, Is.Not.SameAs(live.Manpower));
        Assert.That(stagedPositions, Is.Not.SameAs(live.Positions));
        Assert.That(stagedManpower.ArmedForceStore, Is.SameAs(stagedForce));
        Assert.That(stagedPositions.ArmedForceStore, Is.SameAs(stagedForce));
        Assert.That(stagedPositions.SpatialAuthorityStore, Is.SameAs(stagedSpatial));
        Assert.That(stagedPositions.LocalTopologyStore, Is.Null);
        Assert.That(stagedManpower.SourceProvider, Is.Null);
        Assert.That(stagedForce.Revision, Is.Zero);
        Assert.That(stagedManpower.Revision, Is.Zero);
        Assert.That(stagedPositions.Revision, Is.Zero);
        Assert.That(live.Forces.Revision, Is.Zero);
        Assert.That(live.Manpower.Revision, Is.Zero);
        Assert.That(live.Positions.Revision, Is.Zero);
    }

    [Test]
    public void PopulatedSnapshot_RoundTripsOwnerFieldsTypedReferencesAndLocalRevisions()
    {
        MilitaryWorld live = CreateWorld(populated: true);
        P12EMilitaryOwnerSnapshot snapshot = CaptureOrFail(live);

        Assert.That(snapshot.Forces.Select(row => row.ForceIdValue), Is.EqualTo(new[] { "force-child", "force-root" }));
        Assert.That(snapshot.ArmedForceRevision, Is.EqualTo(live.Forces.Revision));
        Assert.That(snapshot.Contingents.Count, Is.EqualTo(1));
        Assert.That(snapshot.Contingents[0].Characteristics.Select(row => row.Key), Is.EqualTo(new[] { "branch", "kind" }));
        Assert.That(snapshot.RelevantPersons.Count, Is.EqualTo(1));
        Assert.That(snapshot.ManpowerStates[0].Revision, Is.EqualTo(1L));
        Assert.That(snapshot.ManpowerRevision, Is.EqualTo(1L));
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.Amount), Is.EqualTo(new[] { 5L, 7L }));
        Assert.That(snapshot.PositionCount, Is.EqualTo(1));
        Assert.That(snapshot.PositionRevision, Is.EqualTo(1L));
        Assert.That(snapshot.Positions[0].Reference.Kind, Is.EqualTo(SpatialReferenceKind.Location));
        Assert.That(snapshot.Positions[0].Reference.LocationIdValue, Is.EqualTo("snapshot-location"));

        Assert.That(live.Manpower.TryRedistribute(
            new ContingentId("contingent-child"),
            new[] { new ContingentManpowerCohort(
                ManpowerInjuryState.Healthy,
                ManpowerCustodyState.Free,
                null,
                ManpowerAvailabilityState.Available,
                12L) },
            1L,
            out ContingentManpowerFailure redistributionFailure), Is.True, redistributionFailure.ToString());
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.Amount), Is.EqualTo(new[] { 5L, 7L }));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<P12EMilitaryForceSnapshotRecord>)snapshot.Forces).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<P12EMilitaryCharacteristicSnapshot>)snapshot.Contingents[0].Characteristics).Clear());

        PersonStore stagedPersons = CreatePersons();
        SpatialAuthorityStore stagedSpatial = CreateSpatial();
        Assert.That(snapshot.TryStage(stagedPersons, stagedSpatial, null,
            out ArmedForceStore stagedForce,
            out ContingentManpowerStateStore stagedManpower,
            out ArmedForceSpatialStateStore stagedPositions,
            out P12EMilitaryOwnerSnapshotFailure stageFailure), Is.True, stageFailure.Message);
        Assert.That(stagedForce.Revision, Is.EqualTo(snapshot.ArmedForceRevision));
        Assert.That(stagedManpower.Revision, Is.EqualTo(snapshot.ManpowerRevision));
        Assert.That(stagedPositions.Revision, Is.EqualTo(snapshot.PositionRevision));
        Assert.That(stagedForce.TryGet(new ArmedForceId("force-root"), out ArmedForceRecord root), Is.True);
        Assert.That(root.DisplayName, Is.EqualTo("Root Force"));
        Assert.That(root.OperationalLocationReference, Is.EqualTo("legacy opaque location"));
        Assert.That(root.CommanderPersonId.Value, Is.EqualTo("person-commander"));
        Assert.That(stagedForce.TryGet(new ArmedForceId("force-child"), out ArmedForceRecord child), Is.True);
        Assert.That(child.ParentForceId.Value, Is.EqualTo("force-root"));
        Assert.That(stagedForce.TryGetContingent(new ContingentId("contingent-child"), out ContingentRecord contingent), Is.True);
        Assert.That(contingent.Amount, Is.EqualTo(12L));
        Assert.That(contingent.Origin.Domain, Is.EqualTo("content-domain"));
        Assert.That(contingent.Origin.Value, Is.EqualTo("origin-value"));
        Assert.That(contingent.ServiceType, Is.EqualTo("open-ended-service"));
        Assert.That(contingent.Characteristics.Select(row => row.Key), Is.EqualTo(new[] { "branch", "kind" }));
        Assert.That(stagedForce.RelevantPersons[0].Id.Value, Is.EqualTo(snapshot.RelevantPersons[0].ReferenceIdValue));
        Assert.That(stagedForce.RelevantPersons[0].RoleKey, Is.EqualTo("officer"));
        Assert.That(stagedManpower.TryGet(new ContingentId("contingent-child"), out ContingentManpowerState state), Is.True);
        Assert.That(state.SourceId, Is.Null);
        Assert.That(state.Revision, Is.EqualTo(1L));
        Assert.That(state.LivingRosterAmount, Is.EqualTo(12L));
        Assert.That(state.AvailableAmount, Is.EqualTo(5L));
        Assert.That(state.Cohorts.Select(row => row.Amount), Is.EqualTo(new[] { 5L, 7L }));
        Assert.That(stagedPositions.TryGetPosition(new ArmedForceId("force-child"), out SpatialReference position), Is.True);
        Assert.That(position.Kind, Is.EqualTo(SpatialReferenceKind.Location));
        Assert.That(position.LocationId.Value, Is.EqualTo("snapshot-location"));
        Assert.That(stagedForce.ValidateInvariants().IsValid, Is.True);
        Assert.That(stagedManpower.ValidateInvariants().IsValid, Is.True);
        Assert.That(stagedPositions.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void Snapshot_RetainsTypedPositionForTerminatedRegisteredForce()
    {
        PersonStore persons = CreatePersons();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId formerForceId = new ArmedForceId("force-former");
        Assert.That(forces.TryRegister(new ArmedForceRecord(formerForceId, "Former Force", 0L), out _), Is.True);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces);
        SpatialAuthorityStore spatial = CreateSpatial();
        ArmedForceSpatialStateStore positions = new ArmedForceSpatialStateStore(forces, spatial);
        Assert.That(positions.TrySetPosition(formerForceId,
            SpatialReference.ForLocation(new LocationId("snapshot-location")), out _), Is.True);
        Assert.That(forces.TryTerminate(formerForceId, 3L, out ArmedForceFoundationFailure terminateFailure),
            Is.True, terminateFailure.ToString());

        MilitaryWorld live = new MilitaryWorld(persons, forces, manpower, positions, spatial);
        P12EMilitaryOwnerSnapshot snapshot = CaptureOrFail(live);
        Assert.That(snapshot.Positions.Select(row => row.ForceIdValue), Is.EqualTo(new[] { "force-former" }));

        PersonStore stagedPersons = CreatePersons();
        SpatialAuthorityStore stagedSpatial = CreateSpatial();
        Assert.That(snapshot.TryStage(stagedPersons, stagedSpatial, null,
            out ArmedForceStore stagedForce,
            out ContingentManpowerStateStore stagedManpower,
            out ArmedForceSpatialStateStore stagedPositions,
            out P12EMilitaryOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(stagedForce.TryGet(formerForceId, out ArmedForceRecord formerForce), Is.True);
        Assert.That(formerForce.LifecycleState, Is.EqualTo(ArmedForceLifecycleState.Terminated));
        Assert.That(stagedPositions.TryGetPosition(formerForceId, out SpatialReference restoredPosition), Is.True);
        Assert.That(restoredPosition.LocationId.Value, Is.EqualTo("snapshot-location"));
        Assert.That(stagedForce.ValidateInvariants().IsValid, Is.True);
        Assert.That(stagedManpower.ValidateInvariants().IsValid, Is.True);
        Assert.That(stagedPositions.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void CaptureRejectsStaleVectorProviderAndP10TopologyWithoutRetainingAnything()
    {
        MilitaryWorld live = CreateWorld(populated: false);
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = CreateSections(live);
        DailyCaptureEligibilityToken correctToken = CreateToken(sections);
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            live.Forces, live.Manpower, live.Positions, correctToken, CreateSections(live),
            out _, out P12EMilitaryOwnerSnapshotFailure wrongVector), Is.False);
        Assert.That(wrongVector.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.InvalidCaptureContext));

        MilitaryWorld withProvider = CreateWorld(populated: false, sourceProvider: new EmptyProvider());
        IReadOnlyList<OwnerSectionCensusSnapshot> providerSections = CreateSections(withProvider);
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            withProvider.Forces, withProvider.Manpower, withProvider.Positions,
            CreateToken(providerSections), providerSections,
            out _, out P12EMilitaryOwnerSnapshotFailure providerFailure), Is.False);
        Assert.That(providerFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSourceBinding));

        ArmedForceSpatialStateStore injectedTopology = new ArmedForceSpatialStateStore(
            live.Forces, live.Spatial, CreateEmptyTopology());
        MilitaryWorld topologyWorld = new MilitaryWorld(
            live.Persons, live.Forces, live.Manpower, injectedTopology, live.Spatial);
        IReadOnlyList<OwnerSectionCensusSnapshot> topologySections = CreateSections(topologyWorld);
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            topologyWorld.Forces, topologyWorld.Manpower, topologyWorld.Positions,
            CreateToken(topologySections), topologySections,
            out _, out P12EMilitaryOwnerSnapshotFailure topologyFailure), Is.False);
        Assert.That(topologyFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.LocalTopologyComposed));
    }

    [Test]
    public void CaptureRejectsP16ProfileAndP17ProvenanceExtensions()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("p16-force");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "P16 Force", 0L), out _), Is.True);
        SpatialAuthorityStore spatial = CreateSpatial();
        Assert.That(ArmedForceSpatialStateStore.TryCreateP16A(
            forces,
            spatial,
            forceId,
            new HexId("snapshot-hex"),
            "ration.item",
            "ration-v1",
            2m,
            1m,
            1L,
            out ArmedForceSpatialStateStore positions,
            out ArmedForceSpatialFailure p16Failure), Is.True, p16Failure.ToString());
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces);
        MilitaryWorld world = new MilitaryWorld(persons, forces, manpower, positions, spatial);

        IReadOnlyList<OwnerSectionCensusSnapshot> p16Sections = CreateSections(world);
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            forces, manpower, positions, CreateToken(p16Sections), p16Sections,
            out _, out P12EMilitaryOwnerSnapshotFailure p16CaptureFailure), Is.False);
        Assert.That(p16CaptureFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.P16ExtensionState));

        Assert.That(positions.ConfigureP17AProvenance("trusted-war-authority"), Is.True);
        IReadOnlyList<OwnerSectionCensusSnapshot> p17Sections = CreateSections(world);
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            forces, manpower, positions, CreateToken(p17Sections), p17Sections,
            out _, out P12EMilitaryOwnerSnapshotFailure p17CaptureFailure), Is.False);
        Assert.That(p17CaptureFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.P17ProvenanceState));
    }

    [Test]
    public void StageRejectsSchemaCardinalityTopologyAndSourceBoundRowsBeforeReturningOwners()
    {
        MilitaryWorld live = CreateWorld(populated: true);
        P12EMilitaryOwnerSnapshot valid = CaptureOrFail(live);
        PersonStore persons = CreatePersons();
        SpatialAuthorityStore spatial = CreateSpatial();

        P12EMilitaryOwnerSnapshot badSchema = Copy(valid, schemaVersion: valid.SchemaVersion + 1);
        Assert.That(badSchema.TryStage(persons, spatial, null, out ArmedForceStore schemaForce,
            out ContingentManpowerStateStore schemaManpower, out ArmedForceSpatialStateStore schemaPositions,
            out P12EMilitaryOwnerSnapshotFailure schemaFailure), Is.False);
        Assert.That(schemaForce, Is.Null);
        Assert.That(schemaManpower, Is.Null);
        Assert.That(schemaPositions, Is.Null);
        Assert.That(schemaFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSchema));

        P12EMilitaryOwnerSnapshot badCount = Copy(valid, positionCount: valid.PositionCount + 1);
        Assert.That(badCount.TryStage(persons, spatial, null, out _, out _, out _,
            out P12EMilitaryOwnerSnapshotFailure countFailure), Is.False);
        Assert.That(countFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.InvalidCardinality));

        Assert.That(valid.TryStage(persons, spatial, CreateEmptyTopology(), out _, out _, out _,
            out P12EMilitaryOwnerSnapshotFailure topologyFailure), Is.False);
        Assert.That(topologyFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.LocalTopologyComposed));

        P12EManpowerStateSnapshotRecord sourceBoundRow = new P12EManpowerStateSnapshotRecord(
            valid.ManpowerStates[0].ContingentIdValue,
            "unsupported-source",
            valid.ManpowerStates[0].Revision,
            valid.ManpowerStates[0].Cohorts);
        P12EMilitaryOwnerSnapshot sourceBound = Copy(valid, manpowerStates: new[] { sourceBoundRow });
        Assert.That(sourceBound.TryStage(persons, spatial, null, out _, out _, out _,
            out P12EMilitaryOwnerSnapshotFailure sourceFailure), Is.False);
        Assert.That(sourceFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.UnsupportedSourceBinding));

        P12EForcePositionSnapshotRecord topologyPosition = new P12EForcePositionSnapshotRecord(
            valid.Positions[0].ForceIdValue,
            new P12ESpatialReferenceSnapshot(
                SpatialReferenceKind.Location,
                null,
                "snapshot-location",
                null,
                LocalTopologyOwnerKind.ExplorableSite,
                "site-runtime",
                "entrance"));
        P12EMilitaryOwnerSnapshot topologyBound = Copy(valid, positions: new[] { topologyPosition });
        Assert.That(topologyBound.TryStage(persons, spatial, null, out _, out _, out _,
            out P12EMilitaryOwnerSnapshotFailure topologyReferenceFailure), Is.False);
        Assert.That(topologyReferenceFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference));

        Assert.That(live.Forces.Revision, Is.EqualTo(valid.ArmedForceRevision));
        Assert.That(live.Manpower.Revision, Is.EqualTo(valid.ManpowerRevision));
        Assert.That(live.Positions.Revision, Is.EqualTo(valid.PositionRevision));
    }

    [Test]
    public void CaptureRequiresTheExactFiveRequiredTokenBoundOwnerStamps()
    {
        MilitaryWorld world = CreateWorld(populated: false);
        IReadOnlyList<OwnerSectionCensusSnapshot> correct = CreateSections(world);
        OwnerSectionCensusSnapshot first = correct[0];
        OwnerSectionCensusSnapshot wrongOwner = new OwnerSectionCensusSnapshot(
            first.SectionId, first.SchemaVersion, OwnerSectionRole.Required, new object(), first.Cardinality, first.Revision);
        OwnerSectionCensusSnapshot[] wrong = new OwnerSectionCensusSnapshot[correct.Count];
        for (int index = 0; index < correct.Count; index++) wrong[index] = correct[index];
        wrong[0] = wrongOwner;
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            world.Forces, world.Manpower, world.Positions, CreateToken(wrong), wrong,
            out _, out P12EMilitaryOwnerSnapshotFailure ownerFailure), Is.False);
        Assert.That(ownerFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerSectionVector));

        OwnerSectionCensusSnapshot[] missing = correct.Take(correct.Count - 1).ToArray();
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            world.Forces, world.Manpower, world.Positions, CreateToken(missing), missing,
            out _, out P12EMilitaryOwnerSnapshotFailure missingFailure), Is.False);
        Assert.That(missingFailure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.InvalidOwnerSectionVector));
    }

    private static P12EMilitaryOwnerSnapshot Copy(
        P12EMilitaryOwnerSnapshot source,
        int? schemaVersion = null,
        int? positionCount = null,
        IEnumerable<P12EManpowerStateSnapshotRecord> manpowerStates = null,
        IEnumerable<P12EForcePositionSnapshotRecord> positions = null) =>
        new P12EMilitaryOwnerSnapshot(
            schemaVersion ?? source.SchemaVersion,
            source.ArmedForceRevision,
            source.ManpowerRevision,
            source.PositionRevision,
            source.ForceCount,
            source.ContingentCount,
            source.RelevantPersonCount,
            manpowerStates?.Count() ?? source.ManpowerStateCount,
            positionCount ?? source.PositionCount,
            source.Forces,
            source.Contingents,
            source.RelevantPersons,
            manpowerStates ?? source.ManpowerStates,
            positions ?? source.Positions);

    private static P12EMilitaryOwnerSnapshot CaptureOrFail(MilitaryWorld world)
    {
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = CreateSections(world);
        Assert.That(P12EMilitaryOwnerSnapshot.TryCapture(
            world.Forces,
            world.Manpower,
            world.Positions,
            CreateToken(sections),
            sections,
            out P12EMilitaryOwnerSnapshot snapshot,
            out P12EMilitaryOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(P12EMilitaryOwnerSnapshotFailureCode.None));
        return snapshot;
    }

    private static IReadOnlyList<OwnerSectionCensusSnapshot> CreateSections(MilitaryWorld world)
    {
        List<OwnerSectionCensusSnapshot> result = new List<OwnerSectionCensusSnapshot>();
        foreach (IOwnerSectionCensusProvider provider in ArmedForceStoreCensusProvider.CreateProviders(world.Forces))
            Add(provider.GetCurrentCensus(), result);
        Add(new ContingentManpowerCensusProvider(world.Manpower).GetCurrentCensus(), result);
        Add(new ArmedForceSpatialCensusProvider(world.Positions).GetCurrentCensus(), result);
        return result.AsReadOnly();
    }

    private static void Add(OwnerSectionCensusWitness witness, List<OwnerSectionCensusSnapshot> result) =>
        result.Add(new OwnerSectionCensusSnapshot(
            witness.SectionId,
            witness.SchemaVersion,
            OwnerSectionRole.Required,
            witness.OwnerInstanceIdentity,
            witness.Cardinality,
            witness.Revision));

    private static DailyCaptureEligibilityToken CreateToken(IReadOnlyList<OwnerSectionCensusSnapshot> sections) =>
        new DailyCaptureEligibilityToken(
            new object(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()),
            0L,
            1L,
            0L,
            sections);

    private static MilitaryWorld CreateWorld(bool populated, IManpowerSourceSnapshotProvider sourceProvider = null)
    {
        PersonStore persons = CreatePersons();
        ArmedForceStore forces = new ArmedForceStore(persons);
        if (populated)
        {
            ArmedForceId rootId = new ArmedForceId("force-root");
            ArmedForceId childId = new ArmedForceId("force-child");
            Assert.That(forces.TryRegister(new ArmedForceRecord(
                rootId, "Root Force", 0L, operationalLocationReference: "legacy opaque location",
                commanderPersonId: new PersonId("person-commander")), out _), Is.True);
            Assert.That(forces.TryRegister(new ArmedForceRecord(childId, "Child Force", 1L, rootId), out _), Is.True);
            Assert.That(forces.TryRegisterContingent(new ContingentRecord(
                new ContingentId("contingent-child"),
                childId,
                12L,
                new ContingentOriginReference("content-domain", "origin-value"),
                "open-ended-service",
                new[]
                {
                    new ArmedForceCharacteristic("kind", "regular"),
                    new ArmedForceCharacteristic("branch", "field")
                }), out _), Is.True);
            Assert.That(forces.TryAddRelevantPerson(
                rootId,
                new PersonId("person-officer"),
                "officer",
                out _, out _), Is.True);
        }

        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, sourceProvider);
        if (populated)
            Assert.That(manpower.TryRedistribute(
                new ContingentId("contingent-child"),
                new[]
                {
                    new ContingentManpowerCohort(
                        ManpowerInjuryState.Wounded,
                        ManpowerCustodyState.Free,
                        null,
                        ManpowerAvailabilityState.Unavailable,
                        7L),
                    new ContingentManpowerCohort(
                        ManpowerInjuryState.Healthy,
                        ManpowerCustodyState.Free,
                        null,
                        ManpowerAvailabilityState.Available,
                        5L)
                },
                0L,
                out ContingentManpowerFailure failure), Is.True, failure.ToString());

        SpatialAuthorityStore spatial = CreateSpatial();
        ArmedForceSpatialStateStore positions = new ArmedForceSpatialStateStore(forces, spatial);
        if (populated)
            Assert.That(positions.TrySetPosition(
                new ArmedForceId("force-child"),
                SpatialReference.ForLocation(new LocationId("snapshot-location")),
                out ArmedForceSpatialFailure positionFailure), Is.True, positionFailure.ToString());
        return new MilitaryWorld(persons, forces, manpower, positions, spatial);
    }

    private static PersonStore CreatePersons()
    {
        PersonStore persons = new PersonStore();
        Assert.That(persons.TryRegister(new PersonRuntime(new PersonId("person-commander")), out _), Is.True);
        Assert.That(persons.TryRegister(new PersonRuntime(new PersonId("person-officer")), out _), Is.True);
        return persons;
    }

    private static SpatialAuthorityStore CreateSpatial()
    {
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId hex = new HexId("snapshot-hex");
        LocationId location = new LocationId("snapshot-location");
        Assert.That(spatial.TryRegisterHex(new HexRecord(hex), out _), Is.True);
        Assert.That(spatial.TryRegisterLocation(new LocationRecord(location, hex), out _), Is.True);
        return spatial;
    }

    private static LocalTopologyStore CreateEmptyTopology() =>
        new LocalTopologyStore(new RuntimeIdentityRegistry());

    private sealed class EmptyProvider : IManpowerSourceSnapshotProvider
    {
        public bool TryGetSnapshot(ManpowerSourceId sourceId, out ManpowerSourceCapacitySnapshot snapshot)
        {
            snapshot = null;
            return false;
        }
    }

    private sealed class MilitaryWorld
    {
        internal PersonStore Persons { get; }
        internal ArmedForceStore Forces { get; }
        internal ContingentManpowerStateStore Manpower { get; }
        internal ArmedForceSpatialStateStore Positions { get; }
        internal SpatialAuthorityStore Spatial { get; }

        internal MilitaryWorld(
            PersonStore persons,
            ArmedForceStore forces,
            ContingentManpowerStateStore manpower,
            ArmedForceSpatialStateStore positions,
            SpatialAuthorityStore spatial)
        {
            Persons = persons;
            Forces = forces;
            Manpower = manpower;
            Positions = positions;
            Spatial = spatial;
        }
    }
}
