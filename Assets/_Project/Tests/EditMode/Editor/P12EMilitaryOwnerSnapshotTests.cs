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
        P12EMilitaryForceSnapshotRecord capturedChild = snapshot.Forces[0];
        Assert.That(capturedChild.DisplayName, Is.EqualTo("Child Force"));
        Assert.That(capturedChild.CreatedAbsoluteDay, Is.EqualTo(1L));
        Assert.That(capturedChild.ParentForceIdValue, Is.EqualTo("force-root"));
        Assert.That(capturedChild.OperationalLocationReference, Is.Null);
        Assert.That(capturedChild.CommanderPersonIdValue, Is.Null);
        Assert.That(capturedChild.LifecycleState, Is.EqualTo(ArmedForceLifecycleState.Active));
        Assert.That(capturedChild.TerminatedAbsoluteDay, Is.Null);
        Assert.That(capturedChild.IsDetached, Is.True);
        P12EMilitaryForceSnapshotRecord capturedRoot = snapshot.Forces[1];
        Assert.That(capturedRoot.DisplayName, Is.EqualTo("Root Force"));
        Assert.That(capturedRoot.CreatedAbsoluteDay, Is.EqualTo(0L));
        Assert.That(capturedRoot.ParentForceIdValue, Is.Null);
        Assert.That(capturedRoot.OperationalLocationReference, Is.EqualTo("legacy opaque location"));
        Assert.That(capturedRoot.CommanderPersonIdValue, Is.EqualTo("person-commander"));
        Assert.That(capturedRoot.LifecycleState, Is.EqualTo(ArmedForceLifecycleState.Active));
        Assert.That(capturedRoot.TerminatedAbsoluteDay, Is.Null);
        Assert.That(capturedRoot.IsDetached, Is.False);
        Assert.That(snapshot.ArmedForceRevision, Is.EqualTo(live.Forces.Revision));
        Assert.That(snapshot.Contingents.Count, Is.EqualTo(1));
        Assert.That(snapshot.Contingents[0].ContingentIdValue, Is.EqualTo("contingent-child"));
        Assert.That(snapshot.Contingents[0].ForceIdValue, Is.EqualTo("force-child"));
        Assert.That(snapshot.Contingents[0].Amount, Is.EqualTo(12L));
        Assert.That(snapshot.Contingents[0].OriginDomain, Is.EqualTo("content-domain"));
        Assert.That(snapshot.Contingents[0].OriginValue, Is.EqualTo("origin-value"));
        Assert.That(snapshot.Contingents[0].ServiceType, Is.EqualTo("open-ended-service"));
        Assert.That(snapshot.Contingents[0].Characteristics.Select(row => row.Key), Is.EqualTo(new[] { "branch", "kind" }));
        Assert.That(snapshot.Contingents[0].Characteristics.Select(row => row.Value), Is.EqualTo(new[] { "field", "regular" }));
        Assert.That(snapshot.RelevantPersons.Count, Is.EqualTo(1));
        Assert.That(snapshot.RelevantPersons[0].ForceIdValue, Is.EqualTo("force-root"));
        Assert.That(snapshot.RelevantPersons[0].PersonIdValue, Is.EqualTo("person-officer"));
        Assert.That(snapshot.RelevantPersons[0].RoleKey, Is.EqualTo("officer"));
        Assert.That(snapshot.RelevantPersons[0].ReferenceIdValue, Is.Not.Null.And.Not.Empty);
        Assert.That(snapshot.ManpowerStates[0].ContingentIdValue, Is.EqualTo("contingent-child"));
        Assert.That(snapshot.ManpowerStates[0].SourceIdValue, Is.Null);
        Assert.That(snapshot.ManpowerStates[0].Revision, Is.EqualTo(1L));
        Assert.That(snapshot.ManpowerRevision, Is.EqualTo(1L));
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.InjuryState), Is.EqualTo(new[]
        {
            ManpowerInjuryState.Healthy, ManpowerInjuryState.Healthy, ManpowerInjuryState.Wounded
        }));
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.CustodyState), Is.EqualTo(new[]
        {
            ManpowerCustodyState.Free, ManpowerCustodyState.Captured, ManpowerCustodyState.Free
        }));
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.CustodianForceIdValue), Is.EqualTo(new[]
        {
            null, "force-root", null
        }));
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.AvailabilityState), Is.EqualTo(new[]
        {
            ManpowerAvailabilityState.Available,
            ManpowerAvailabilityState.Unavailable,
            ManpowerAvailabilityState.Unavailable
        }));
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.Amount), Is.EqualTo(new[] { 3L, 2L, 7L }));
        Assert.That(snapshot.PositionCount, Is.EqualTo(1));
        Assert.That(snapshot.PositionRevision, Is.EqualTo(1L));
        Assert.That(snapshot.Positions[0].ForceIdValue, Is.EqualTo("force-child"));
        Assert.That(snapshot.Positions[0].Reference.Kind, Is.EqualTo(SpatialReferenceKind.Location));
        Assert.That(snapshot.Positions[0].Reference.HexIdValue, Is.Null);
        Assert.That(snapshot.Positions[0].Reference.LocationIdValue, Is.EqualTo("snapshot-location"));
        Assert.That(snapshot.Positions[0].Reference.CrossingIdValue, Is.Null);
        Assert.That(snapshot.Positions[0].Reference.TopologyOwnerKind, Is.Null);
        Assert.That(snapshot.Positions[0].Reference.TopologyOwnerRuntimeId, Is.Null);
        Assert.That(snapshot.Positions[0].Reference.SubLocationRuntimeId, Is.Null);

        Assert.That(live.Forces.TryAssignCommander(new ArmedForceId("force-root"), new PersonId("person-officer"),
            out ArmedForceFoundationFailure commanderFailure), Is.True, commanderFailure.ToString());
        Assert.That(live.Positions.TrySetPosition(new ArmedForceId("force-child"),
            SpatialReference.ForHex(new HexId("snapshot-hex")), out ArmedForceSpatialFailure positionFailure),
            Is.True, positionFailure.ToString());
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
        Assert.That(snapshot.Forces[1].CommanderPersonIdValue, Is.EqualTo("person-commander"));
        Assert.That(snapshot.Positions[0].Reference.Kind, Is.EqualTo(SpatialReferenceKind.Location));
        Assert.That(snapshot.Positions[0].Reference.LocationIdValue, Is.EqualTo("snapshot-location"));
        Assert.That(snapshot.ManpowerStates[0].Cohorts.Select(row => row.Amount), Is.EqualTo(new[] { 3L, 2L, 7L }));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<P12EMilitaryForceSnapshotRecord>)snapshot.Forces).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<P12EMilitaryCharacteristicSnapshot>)snapshot.Contingents[0].Characteristics).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<P12EManpowerCohortSnapshotRecord>)snapshot.ManpowerStates[0].Cohorts).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<P12EForcePositionSnapshotRecord>)snapshot.Positions).Clear());

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
        Assert.That(root.CreatedAbsoluteDay, Is.EqualTo(capturedRoot.CreatedAbsoluteDay));
        Assert.That(root.ParentForceId, Is.Null);
        Assert.That(root.OperationalLocationReference, Is.EqualTo("legacy opaque location"));
        Assert.That(root.CommanderPersonId.Value, Is.EqualTo("person-commander"));
        Assert.That(root.LifecycleState, Is.EqualTo(ArmedForceLifecycleState.Active));
        Assert.That(root.TerminatedAbsoluteDay, Is.Null);
        Assert.That(root.IsDetached, Is.False);
        Assert.That(stagedForce.TryGet(new ArmedForceId("force-child"), out ArmedForceRecord child), Is.True);
        Assert.That(child.DisplayName, Is.EqualTo("Child Force"));
        Assert.That(child.CreatedAbsoluteDay, Is.EqualTo(1L));
        Assert.That(child.ParentForceId.Value, Is.EqualTo("force-root"));
        Assert.That(child.OperationalLocationReference, Is.Null);
        Assert.That(child.CommanderPersonId, Is.Null);
        Assert.That(child.LifecycleState, Is.EqualTo(ArmedForceLifecycleState.Active));
        Assert.That(child.TerminatedAbsoluteDay, Is.Null);
        Assert.That(child.IsDetached, Is.True);
        Assert.That(stagedForce.TryGetContingent(new ContingentId("contingent-child"), out ContingentRecord contingent), Is.True);
        Assert.That(contingent.Id.Value, Is.EqualTo("contingent-child"));
        Assert.That(contingent.ForceId.Value, Is.EqualTo("force-child"));
        Assert.That(contingent.Amount, Is.EqualTo(12L));
        Assert.That(contingent.Origin.Domain, Is.EqualTo("content-domain"));
        Assert.That(contingent.Origin.Value, Is.EqualTo("origin-value"));
        Assert.That(contingent.ServiceType, Is.EqualTo("open-ended-service"));
        Assert.That(contingent.Characteristics.Select(row => row.Key), Is.EqualTo(new[] { "branch", "kind" }));
        Assert.That(contingent.Characteristics.Select(row => row.Value), Is.EqualTo(new[] { "field", "regular" }));
        Assert.That(stagedForce.RelevantPersons[0].Id.Value, Is.EqualTo(snapshot.RelevantPersons[0].ReferenceIdValue));
        Assert.That(stagedForce.RelevantPersons[0].ForceId.Value, Is.EqualTo("force-root"));
        Assert.That(stagedForce.RelevantPersons[0].PersonId.Value, Is.EqualTo("person-officer"));
        Assert.That(stagedForce.RelevantPersons[0].RoleKey, Is.EqualTo("officer"));
        Assert.That(stagedManpower.TryGet(new ContingentId("contingent-child"), out ContingentManpowerState state), Is.True);
        Assert.That(state.ContingentId.Value, Is.EqualTo("contingent-child"));
        Assert.That(state.SourceId, Is.Null);
        Assert.That(state.Revision, Is.EqualTo(1L));
        Assert.That(state.LivingRosterAmount, Is.EqualTo(12L));
        Assert.That(state.AvailableAmount, Is.EqualTo(3L));
        Assert.That(state.Cohorts.Select(row => row.InjuryState), Is.EqualTo(new[]
        {
            ManpowerInjuryState.Healthy, ManpowerInjuryState.Healthy, ManpowerInjuryState.Wounded
        }));
        Assert.That(state.Cohorts.Select(row => row.CustodyState), Is.EqualTo(new[]
        {
            ManpowerCustodyState.Free, ManpowerCustodyState.Captured, ManpowerCustodyState.Free
        }));
        Assert.That(state.Cohorts.Select(row => row.CustodianForceId?.Value), Is.EqualTo(new[]
        {
            null, "force-root", null
        }));
        Assert.That(state.Cohorts.Select(row => row.AvailabilityState), Is.EqualTo(new[]
        {
            ManpowerAvailabilityState.Available,
            ManpowerAvailabilityState.Unavailable,
            ManpowerAvailabilityState.Unavailable
        }));
        Assert.That(state.Cohorts.Select(row => row.Amount), Is.EqualTo(new[] { 3L, 2L, 7L }));
        Assert.That(stagedPositions.TryGetPosition(new ArmedForceId("force-child"), out SpatialReference position), Is.True);
        Assert.That(position.Kind, Is.EqualTo(SpatialReferenceKind.Location));
        Assert.That(position.LocationId.Value, Is.EqualTo("snapshot-location"));
        Assert.That(position.HexId, Is.Null);
        Assert.That(position.CrossingId, Is.Null);
        Assert.That(position.TopologyOwnerKind, Is.Null);
        Assert.That(position.TopologyOwnerRuntimeId, Is.Null);
        Assert.That(position.SubLocationRuntimeId, Is.Null);
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
        Assert.That(formerForce.DisplayName, Is.EqualTo("Former Force"));
        Assert.That(formerForce.CreatedAbsoluteDay, Is.EqualTo(0L));
        Assert.That(formerForce.ParentForceId, Is.Null);
        Assert.That(formerForce.OperationalLocationReference, Is.Null);
        Assert.That(formerForce.CommanderPersonId, Is.Null);
        Assert.That(formerForce.LifecycleState, Is.EqualTo(ArmedForceLifecycleState.Terminated));
        Assert.That(formerForce.TerminatedAbsoluteDay, Is.EqualTo(3L));
        Assert.That(formerForce.IsDetached, Is.False);
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
    public void StageRejectsMalformedHierarchyDuplicateOrMissingOwnerRowsAndInvalidPositions()
    {
        MilitaryWorld live = CreateWorld(populated: true);
        P12EMilitaryOwnerSnapshot valid = CaptureOrFail(live);
        P12EMilitaryForceSnapshotRecord child = valid.Forces[0];
        P12EMilitaryForceSnapshotRecord root = valid.Forces[1];

        AssertStageRejected(Copy(valid, forces: valid.Forces.Concat(new[] { child })),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity);

        P12EMilitaryForceSnapshotRecord missingParent = new P12EMilitaryForceSnapshotRecord(
            child.ForceIdValue, child.DisplayName, child.CreatedAbsoluteDay, "force-missing",
            child.OperationalLocationReference, child.CommanderPersonIdValue, child.LifecycleState,
            child.TerminatedAbsoluteDay, child.IsDetached);
        AssertStageRejected(Copy(valid, forces: new[] { missingParent, root }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);

        P12EMilitaryForceSnapshotRecord childCycle = new P12EMilitaryForceSnapshotRecord(
            child.ForceIdValue, child.DisplayName, child.CreatedAbsoluteDay, root.ForceIdValue,
            child.OperationalLocationReference, child.CommanderPersonIdValue, child.LifecycleState,
            child.TerminatedAbsoluteDay, child.IsDetached);
        P12EMilitaryForceSnapshotRecord rootCycle = new P12EMilitaryForceSnapshotRecord(
            root.ForceIdValue, root.DisplayName, root.CreatedAbsoluteDay, child.ForceIdValue,
            root.OperationalLocationReference, root.CommanderPersonIdValue, root.LifecycleState,
            root.TerminatedAbsoluteDay, root.IsDetached);
        AssertStageRejected(Copy(valid, forces: new[] { childCycle, rootCycle }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);

        AssertStageRejected(Copy(valid, contingents: valid.Contingents.Concat(new[] { valid.Contingents[0] })),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity);
        P12EMilitaryContingentSnapshotRecord missingForceContingent = new P12EMilitaryContingentSnapshotRecord(
            valid.Contingents[0].ContingentIdValue, "force-missing", valid.Contingents[0].Amount,
            valid.Contingents[0].OriginDomain, valid.Contingents[0].OriginValue,
            valid.Contingents[0].ServiceType, valid.Contingents[0].Characteristics);
        AssertStageRejected(Copy(valid, contingents: new[] { missingForceContingent }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);

        AssertStageRejected(Copy(valid, relevantPersons: valid.RelevantPersons.Concat(new[] { valid.RelevantPersons[0] })),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Copy(valid, manpowerStates: Array.Empty<P12EManpowerStateSnapshotRecord>()),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);
        AssertStageRejected(Copy(valid, manpowerStates: valid.ManpowerStates.Concat(new[] { valid.ManpowerStates[0] })),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity);

        P12EMilitaryForceSnapshotRecord invalidLifecycle = new P12EMilitaryForceSnapshotRecord(
            child.ForceIdValue, child.DisplayName, child.CreatedAbsoluteDay, child.ParentForceIdValue,
            child.OperationalLocationReference, child.CommanderPersonIdValue,
            (ArmedForceLifecycleState)999, child.TerminatedAbsoluteDay, child.IsDetached);
        AssertStageRejected(Copy(valid, forces: new[] { invalidLifecycle, root }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity);

        AssertStageRejected(Copy(valid, positions: valid.Positions.Concat(new[] { valid.Positions[0] })),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference);
        P12EForcePositionSnapshotRecord unregisteredForcePosition = new P12EForcePositionSnapshotRecord(
            "force-missing", valid.Positions[0].Reference);
        AssertStageRejected(Copy(valid, positions: new[] { unregisteredForcePosition }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference);
        P12EForcePositionSnapshotRecord unresolvedPosition = new P12EForcePositionSnapshotRecord(
            valid.Positions[0].ForceIdValue,
            new P12ESpatialReferenceSnapshot(SpatialReferenceKind.Location,
                null, "location-missing", null, null, null, null));
        AssertStageRejected(Copy(valid, positions: new[] { unresolvedPosition }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference);
    }

    [Test]
    public void StageRejectsMalformedCohortsSourceCustodyMirrorAndSpatialRows()
    {
        MilitaryWorld live = CreateWorld(populated: true);
        P12EMilitaryOwnerSnapshot valid = CaptureOrFail(live);
        P12EManpowerStateSnapshotRecord state = valid.ManpowerStates[0];

        P12EMilitaryContingentSnapshotRecord negativeAmount = new P12EMilitaryContingentSnapshotRecord(
            valid.Contingents[0].ContingentIdValue,
            valid.Contingents[0].ForceIdValue,
            -1L,
            valid.Contingents[0].OriginDomain,
            valid.Contingents[0].OriginValue,
            valid.Contingents[0].ServiceType,
            valid.Contingents[0].Characteristics);
        AssertStageRejected(Copy(valid, contingents: new[] { negativeAmount }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity);
        AssertStageRejected(Copy(valid, armedForceRevision: -1L),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidRevision);
        AssertStageRejected(Copy(valid, manpowerRevision: -1L),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidRevision);
        AssertStageRejected(Copy(valid, positionRevision: -1L),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidRevision);
        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, revision: -1L)
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidIdentity);

        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[]
            {
                new P12EManpowerCohortSnapshotRecord(
                    (ManpowerInjuryState)999, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, 12L)
            })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);
        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[]
            {
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, 0L)
            })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);
        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[]
            {
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, -1L)
            })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);
        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[]
            {
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, long.MaxValue),
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Wounded, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, 1L)
            })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);
        P12EManpowerCohortSnapshotRecord sameTuple = new P12EManpowerCohortSnapshotRecord(
            ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
            ManpowerAvailabilityState.Available, 3L);
        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[] { sameTuple, sameTuple })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidSnapshot);
        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[]
            {
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Wounded, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Unavailable, 7L),
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Free, null,
                    ManpowerAvailabilityState.Available, 5L)
            })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidSnapshot);

        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[]
            {
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Captured, null,
                    ManpowerAvailabilityState.Unavailable, 12L)
            })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);
        AssertStageRejected(Copy(valid, manpowerStates: new[]
        {
            CopyManpower(state, cohorts: new[]
            {
                new P12EManpowerCohortSnapshotRecord(
                    ManpowerInjuryState.Healthy, ManpowerCustodyState.Captured, "force-missing",
                    ManpowerAvailabilityState.Unavailable, 12L)
            })
        }), P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);
        AssertStageRejected(Copy(valid,
            contingents: new[]
            {
                new P12EMilitaryContingentSnapshotRecord(
                    valid.Contingents[0].ContingentIdValue,
                    valid.Contingents[0].ForceIdValue,
                    valid.Contingents[0].Amount - 1L,
                    valid.Contingents[0].OriginDomain,
                    valid.Contingents[0].OriginValue,
                    valid.Contingents[0].ServiceType,
                    valid.Contingents[0].Characteristics)
            }), P12EMilitaryOwnerSnapshotFailureCode.InvalidRelation);

        P12EForcePositionSnapshotRecord malformedReference = new P12EForcePositionSnapshotRecord(
            valid.Positions[0].ForceIdValue,
            new P12ESpatialReferenceSnapshot(SpatialReferenceKind.Location,
                "also-present", "snapshot-location", null, null, null, null));
        AssertStageRejected(Copy(valid, positions: new[] { malformedReference }),
            P12EMilitaryOwnerSnapshotFailureCode.InvalidSpatialReference);
    }

    [Test]
    public void PrivateOwnerFactoriesFailClosedWithoutChangingTheirInputsOrLiveOwners()
    {
        MilitaryWorld live = CreateWorld(populated: true);
        P12EMilitaryOwnerSnapshot snapshot = CaptureOrFail(live);
        long forceRevisionBefore = live.Forces.Revision;
        long manpowerRevisionBefore = live.Manpower.Revision;
        long positionRevisionBefore = live.Positions.Revision;
        ArmedForceRecord rootBefore = GetForceOrFail(live.Forces, "force-root");
        ArmedForceRecord childBefore = GetForceOrFail(live.Forces, "force-child");
        ContingentRecord contingentBefore = GetContingentOrFail(live.Forces, "contingent-child");
        ContingentManpowerState stateBefore = GetManpowerStateOrFail(live.Manpower, "contingent-child");
        SpatialReference positionBefore = GetPositionOrFail(live.Positions, "force-child");

        PersonStore targetPersons = CreatePersons();
        int personCountBefore = targetPersons.Persons.Count;
        Assert.That(ArmedForceStore.TryCreateFromP12EOwnerSnapshot(
            targetPersons,
            new[] { live.Forces.Forces[0], live.Forces.Forces[0] },
            live.Forces.Contingents,
            live.Forces.RelevantPersons,
            live.Forces.Revision,
            out ArmedForceStore failedForce,
            out string forceDiagnostic), Is.False);
        Assert.That(failedForce, Is.Null);
        Assert.That(forceDiagnostic, Is.Not.Null.And.Not.Empty);
        Assert.That(targetPersons.Persons.Count, Is.EqualTo(personCountBefore));

        Assert.That(ArmedForceStore.TryCreateFromP12EOwnerSnapshot(
            targetPersons,
            live.Forces.Forces,
            live.Forces.Contingents,
            live.Forces.RelevantPersons,
            -1L,
            out ArmedForceStore failedNegativeRevisionForce,
            out string negativeForceDiagnostic), Is.False);
        Assert.That(failedNegativeRevisionForce, Is.Null);
        Assert.That(negativeForceDiagnostic, Is.Not.Null.And.Not.Empty);
        Assert.That(targetPersons.Persons.Count, Is.EqualTo(personCountBefore));

        ArmedForceStore stagedForce = StageForceOwnerOrFail(live);
        long stagedForceRevisionBefore = stagedForce.Revision;
        ContingentManpowerState sourceBoundState = new ContingentManpowerState(
            new ContingentId("contingent-child"),
            new ManpowerSourceId("unsupported-source"),
            stateBefore.Cohorts,
            stateBefore.Revision);
        Assert.That(ContingentManpowerStateStore.TryCreateFromP12EOwnerSnapshot(
            stagedForce,
            new[] { sourceBoundState },
            snapshot.ManpowerRevision,
            out ContingentManpowerStateStore failedManpower,
            out string manpowerDiagnostic), Is.False);
        Assert.That(failedManpower, Is.Null);
        Assert.That(manpowerDiagnostic, Is.Not.Null.And.Not.Empty);
        Assert.That(stagedForce.Revision, Is.EqualTo(stagedForceRevisionBefore));
        Assert.That(stagedForce.ValidateInvariants().IsValid, Is.True);

        ContingentManpowerState negativeLocalRevisionState = new ContingentManpowerState(
            new ContingentId("contingent-child"),
            null,
            stateBefore.Cohorts,
            -1L);
        Assert.That(ContingentManpowerStateStore.TryCreateFromP12EOwnerSnapshot(
            stagedForce,
            new[] { negativeLocalRevisionState },
            snapshot.ManpowerRevision,
            out ContingentManpowerStateStore failedNegativeStateRevisionManpower,
            out string negativeStateRevisionDiagnostic), Is.False);
        Assert.That(failedNegativeStateRevisionManpower, Is.Null);
        Assert.That(negativeStateRevisionDiagnostic, Is.Not.Null.And.Not.Empty);

        ContingentManpowerState negativeAmountManpowerState = new ContingentManpowerState(
            new ContingentId("contingent-child"),
            null,
            new[]
            {
                new ContingentManpowerCohort(
                    ManpowerInjuryState.Healthy,
                    ManpowerCustodyState.Free,
                    null,
                    ManpowerAvailabilityState.Available,
                    -1L)
            },
            stateBefore.Revision);
        Assert.That(ContingentManpowerStateStore.TryCreateFromP12EOwnerSnapshot(
            stagedForce,
            new[] { negativeAmountManpowerState },
            snapshot.ManpowerRevision,
            out ContingentManpowerStateStore failedNegativeAmountManpower,
            out string negativeManpowerAmountDiagnostic), Is.False);
        Assert.That(failedNegativeAmountManpower, Is.Null);
        Assert.That(negativeManpowerAmountDiagnostic, Is.Not.Null.And.Not.Empty);

        Assert.That(ContingentManpowerStateStore.TryCreateFromP12EOwnerSnapshot(
            stagedForce,
            new[] { stateBefore },
            -1L,
            out ContingentManpowerStateStore failedNegativeOwnerRevisionManpower,
            out string negativeManpowerRevisionDiagnostic), Is.False);
        Assert.That(failedNegativeOwnerRevisionManpower, Is.Null);
        Assert.That(negativeManpowerRevisionDiagnostic, Is.Not.Null.And.Not.Empty);
        Assert.That(stagedForce.Revision, Is.EqualTo(stagedForceRevisionBefore));
        Assert.That(stagedForce.ValidateInvariants().IsValid, Is.True);

        ArmedForceSpatialPosition unresolvedPosition = new ArmedForceSpatialPosition(
            new ArmedForceId("force-child"), SpatialReference.ForLocation(new LocationId("location-missing")));
        Assert.That(ArmedForceSpatialStateStore.TryCreateFromP12EOwnerSnapshot(
            stagedForce,
            live.Spatial,
            null,
            new[] { unresolvedPosition },
            snapshot.PositionRevision,
            out ArmedForceSpatialStateStore failedPositions,
            out string positionDiagnostic), Is.False);
        Assert.That(failedPositions, Is.Null);
        Assert.That(positionDiagnostic, Is.Not.Null.And.Not.Empty);
        Assert.That(stagedForce.Revision, Is.EqualTo(stagedForceRevisionBefore));
        Assert.That(stagedForce.ValidateInvariants().IsValid, Is.True);

        int spatialHexCountBefore = live.Spatial.HexCount;
        int spatialLocationCountBefore = live.Spatial.LocationCount;
        Assert.That(ArmedForceSpatialStateStore.TryCreateFromP12EOwnerSnapshot(
            stagedForce,
            live.Spatial,
            null,
            live.Positions.Positions,
            -1L,
            out ArmedForceSpatialStateStore failedNegativePositionRevision,
            out string negativePositionRevisionDiagnostic), Is.False);
        Assert.That(failedNegativePositionRevision, Is.Null);
        Assert.That(negativePositionRevisionDiagnostic, Is.Not.Null.And.Not.Empty);
        Assert.That(stagedForce.Revision, Is.EqualTo(stagedForceRevisionBefore));
        Assert.That(stagedForce.ValidateInvariants().IsValid, Is.True);
        Assert.That(live.Spatial.HexCount, Is.EqualTo(spatialHexCountBefore));
        Assert.That(live.Spatial.LocationCount, Is.EqualTo(spatialLocationCountBefore));

        Assert.That(live.Forces.Revision, Is.EqualTo(forceRevisionBefore));
        Assert.That(live.Manpower.Revision, Is.EqualTo(manpowerRevisionBefore));
        Assert.That(live.Positions.Revision, Is.EqualTo(positionRevisionBefore));
        Assert.That(GetForceOrFail(live.Forces, "force-root"), Is.EqualTo(rootBefore));
        Assert.That(GetForceOrFail(live.Forces, "force-child"), Is.EqualTo(childBefore));
        Assert.That(GetContingentOrFail(live.Forces, "contingent-child"), Is.EqualTo(contingentBefore));
        ContingentManpowerState stateAfter = GetManpowerStateOrFail(live.Manpower, "contingent-child");
        Assert.That(stateAfter.Revision, Is.EqualTo(stateBefore.Revision));
        Assert.That(stateAfter.SourceId, Is.EqualTo(stateBefore.SourceId));
        Assert.That(stateAfter.Cohorts.Select(CohortKey), Is.EqualTo(stateBefore.Cohorts.Select(CohortKey)));
        SpatialReference positionAfter = GetPositionOrFail(live.Positions, "force-child");
        Assert.That(positionAfter.Kind, Is.EqualTo(positionBefore.Kind));
        Assert.That(positionAfter.LocationId, Is.EqualTo(positionBefore.LocationId));
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
        long? armedForceRevision = null,
        long? manpowerRevision = null,
        long? positionRevision = null,
        int? forceCount = null,
        int? contingentCount = null,
        int? relevantPersonCount = null,
        int? manpowerStateCount = null,
        int? positionCount = null,
        IEnumerable<P12EMilitaryForceSnapshotRecord> forces = null,
        IEnumerable<P12EMilitaryContingentSnapshotRecord> contingents = null,
        IEnumerable<P12EMilitaryRelevantPersonSnapshotRecord> relevantPersons = null,
        IEnumerable<P12EManpowerStateSnapshotRecord> manpowerStates = null,
        IEnumerable<P12EForcePositionSnapshotRecord> positions = null) =>
        CreateCopy(source, schemaVersion, armedForceRevision, manpowerRevision, positionRevision,
            forceCount, contingentCount, relevantPersonCount, manpowerStateCount, positionCount,
            forces, contingents, relevantPersons, manpowerStates, positions);

    private static P12EMilitaryOwnerSnapshot CreateCopy(
        P12EMilitaryOwnerSnapshot source,
        int? schemaVersion,
        long? armedForceRevision,
        long? manpowerRevision,
        long? positionRevision,
        int? forceCount,
        int? contingentCount,
        int? relevantPersonCount,
        int? manpowerStateCount,
        int? positionCount,
        IEnumerable<P12EMilitaryForceSnapshotRecord> forces,
        IEnumerable<P12EMilitaryContingentSnapshotRecord> contingents,
        IEnumerable<P12EMilitaryRelevantPersonSnapshotRecord> relevantPersons,
        IEnumerable<P12EManpowerStateSnapshotRecord> manpowerStates,
        IEnumerable<P12EForcePositionSnapshotRecord> positions)
    {
        P12EMilitaryForceSnapshotRecord[] forceRows = (forces ?? source.Forces).ToArray();
        P12EMilitaryContingentSnapshotRecord[] contingentRows = (contingents ?? source.Contingents).ToArray();
        P12EMilitaryRelevantPersonSnapshotRecord[] relevantRows = (relevantPersons ?? source.RelevantPersons).ToArray();
        P12EManpowerStateSnapshotRecord[] manpowerRows = (manpowerStates ?? source.ManpowerStates).ToArray();
        P12EForcePositionSnapshotRecord[] positionRows = (positions ?? source.Positions).ToArray();
        return new P12EMilitaryOwnerSnapshot(
            schemaVersion ?? source.SchemaVersion,
            armedForceRevision ?? source.ArmedForceRevision,
            manpowerRevision ?? source.ManpowerRevision,
            positionRevision ?? source.PositionRevision,
            forceCount ?? forceRows.Length,
            contingentCount ?? contingentRows.Length,
            relevantPersonCount ?? relevantRows.Length,
            manpowerStateCount ?? manpowerRows.Length,
            positionCount ?? positionRows.Length,
            forceRows,
            contingentRows,
            relevantRows,
            manpowerRows,
            positionRows);
    }

    private static P12EManpowerStateSnapshotRecord CopyManpower(
        P12EManpowerStateSnapshotRecord source,
        IEnumerable<P12EManpowerCohortSnapshotRecord> cohorts = null,
        long? revision = null) =>
        new P12EManpowerStateSnapshotRecord(
            source.ContingentIdValue,
            source.SourceIdValue,
            revision ?? source.Revision,
            cohorts ?? source.Cohorts);

    private static void AssertStageRejected(
        P12EMilitaryOwnerSnapshot snapshot,
        P12EMilitaryOwnerSnapshotFailureCode expectedCode)
    {
        bool success = snapshot.TryStage(
            CreatePersons(),
            CreateSpatial(),
            null,
            out ArmedForceStore stagedForce,
            out ContingentManpowerStateStore stagedManpower,
            out ArmedForceSpatialStateStore stagedPositions,
            out P12EMilitaryOwnerSnapshotFailure failure);
        Assert.That(success, Is.False);
        Assert.That(stagedForce, Is.Null);
        Assert.That(stagedManpower, Is.Null);
        Assert.That(stagedPositions, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expectedCode), failure.Message);
        Assert.That(failure.Message, Is.Not.Null.And.Not.Empty);
    }

    private static ArmedForceStore StageForceOwnerOrFail(MilitaryWorld source)
    {
        Assert.That(ArmedForceStore.TryCreateFromP12EOwnerSnapshot(
            CreatePersons(), source.Forces.Forces, source.Forces.Contingents,
            source.Forces.RelevantPersons, source.Forces.Revision,
            out ArmedForceStore staged, out string diagnostic), Is.True, diagnostic);
        return staged;
    }

    private static ArmedForceRecord GetForceOrFail(ArmedForceStore store, string id)
    {
        Assert.That(store.TryGet(new ArmedForceId(id), out ArmedForceRecord value), Is.True);
        return value;
    }

    private static ContingentRecord GetContingentOrFail(ArmedForceStore store, string id)
    {
        Assert.That(store.TryGetContingent(new ContingentId(id), out ContingentRecord value), Is.True);
        return value;
    }

    private static ContingentManpowerState GetManpowerStateOrFail(ContingentManpowerStateStore store, string id)
    {
        Assert.That(store.TryGet(new ContingentId(id), out ContingentManpowerState value), Is.True);
        return value;
    }

    private static SpatialReference GetPositionOrFail(ArmedForceSpatialStateStore store, string forceId)
    {
        Assert.That(store.TryGetPosition(new ArmedForceId(forceId), out SpatialReference value), Is.True);
        return value;
    }

    private static string CohortKey(ContingentManpowerCohort cohort) =>
        ((int)cohort.InjuryState) + ":" + ((int)cohort.CustodyState) + ":"
        + (cohort.CustodianForceId?.Value ?? string.Empty) + ":"
        + ((int)cohort.AvailabilityState) + ":" + cohort.Amount;

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
                        ManpowerInjuryState.Healthy,
                        ManpowerCustodyState.Free,
                        null,
                        ManpowerAvailabilityState.Available,
                        3L),
                    new ContingentManpowerCohort(
                        ManpowerInjuryState.Healthy,
                        ManpowerCustodyState.Captured,
                        new ArmedForceId("force-root"),
                        ManpowerAvailabilityState.Unavailable,
                        2L),
                    new ContingentManpowerCohort(
                        ManpowerInjuryState.Wounded,
                        ManpowerCustodyState.Free,
                        null,
                        ManpowerAvailabilityState.Unavailable,
                        7L)
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
        if (populated)
            Assert.That(forces.TryDetach(new ArmedForceId("force-child"),
                out ArmedForceFoundationFailure detachFailure), Is.True, detachFailure.ToString());
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
