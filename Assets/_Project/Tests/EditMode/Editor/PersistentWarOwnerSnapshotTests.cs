using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class PersistentWarOwnerSnapshotTests
{
    [Test]
    public void EmptyCaptureUsesExactDailyOwnerWitnessAndStagesAsExactZero()
    {
        WarWorld live = CreateWorld();
        PersistentWarOwnerSnapshot snapshot = CaptureOrFail(live.Wars);
        Assert.That(snapshot.RecordCount, Is.Zero);
        Assert.That(snapshot.Revision, Is.Zero);
        Assert.That(snapshot.Records, Is.Empty);

        WarWorld stagedParents = CreateWorld();
        Assert.That(snapshot.TryStage(stagedParents.Forces, stagedParents.Conflicts,
            out PersistentWarStore staged, out PersistentWarOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(staged, Is.Not.SameAs(live.Wars));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
        Assert.That(staged.ArmedForceStore, Is.SameAs(stagedParents.Forces));
        Assert.That(staged.ConflictStore, Is.SameAs(stagedParents.Conflicts));

        ArmedForceStore unrelatedForces = new ArmedForceStore(new PersonStore());
        PersistentConflictStore unrelatedConflicts = new PersistentConflictStore(unrelatedForces);
        Assert.That(snapshot.TryStage(unrelatedForces, stagedParents.Conflicts,
            out PersistentWarStore wrongParent, out PersistentWarOwnerSnapshotFailure wrongParentFailure), Is.False);
        Assert.That(wrongParent, Is.Null);
        Assert.That(wrongParentFailure.Code, Is.EqualTo(PersistentWarOwnerSnapshotFailureCode.InvalidParentComposition));
        Assert.That(unrelatedConflicts, Is.Not.Null);
    }

    [Test]
    public void PopulatedSnapshotPreservesLifecycleRelationsRevisionAndExactParentIdentity()
    {
        WarWorld live = CreateWorld();
        WarId activeId = new WarId("snapshot-war-active");
        WarId endedId = new WarId("snapshot-war-ended");
        Assert.That(live.Wars.TryRegister(new PersistentWarRecord(
            activeId, 2L, live.ConflictId,
            sides: new[]
            {
                new WarStateSide(activeId, new WarSideId("side-c"), "Gamma"),
                new WarStateSide(activeId, new WarSideId("side-a"), "Alpha"),
                new WarStateSide(activeId, new WarSideId("side-b"), "Beta")
            },
            participantBindings: new[]
            {
                new WarParticipantBinding(new WarParticipantBindingId("binding-b"), activeId,
                    new WarSideId("side-b"), live.SecondForceId),
                new WarParticipantBinding(new WarParticipantBindingId("binding-a"), activeId,
                    new WarSideId("side-a"), live.FirstForceId)
            }), out PersistentStateFailure activeFailure), Is.True, activeFailure.ToString());
        Assert.That(live.Wars.TryRegister(new PersistentWarRecord(
            endedId, 1L, sides: new[]
            {
                new WarStateSide(endedId, new WarSideId("ended-a"), "A"),
                new WarStateSide(endedId, new WarSideId("ended-b"), "B")
            }), out PersistentStateFailure endedFailure), Is.True, endedFailure.ToString());
        Assert.That(live.Wars.TryEnd(endedId, 4L, out PersistentStateFailure endFailure), Is.True, endFailure.ToString());

        PersistentWarOwnerSnapshot snapshot = CaptureOrFail(live.Wars);
        Assert.That(snapshot.RecordCount, Is.EqualTo(2));
        Assert.That(snapshot.Revision, Is.EqualTo(3L));
        Assert.That(snapshot.Records.Select(row => row.WarIdValue), Is.EqualTo(new[] { "snapshot-war-active", "snapshot-war-ended" }));
        Assert.That(snapshot.Records[0].Sides.Select(side => side.SideIdValue), Is.EqualTo(new[] { "side-a", "side-b", "side-c" }));
        Assert.That(snapshot.Records[0].ParticipantBindings.Select(binding => binding.BindingIdValue), Is.EqualTo(new[] { "binding-a", "binding-b" }));

        // Later source writes cannot mutate detached snapshot rows.
        Assert.That(live.Wars.TryAddParticipantBinding(activeId,
            new WarParticipantBinding(new WarParticipantBindingId("binding-c"), activeId,
                new WarSideId("side-c"), live.FirstForceId), out PersistentStateFailure laterFailure), Is.True, laterFailure.ToString());
        Assert.That(snapshot.Records[0].ParticipantBindings, Has.Count.EqualTo(2));

        WarWorld stagedParents = CreateWorld();
        Assert.That(snapshot.TryStage(stagedParents.Forces, stagedParents.Conflicts,
            out PersistentWarStore staged, out PersistentWarOwnerSnapshotFailure stageFailure), Is.True, stageFailure.Message);
        Assert.That(staged.Revision, Is.EqualTo(3L));
        Assert.That(staged.Count, Is.EqualTo(2));
        Assert.That(staged.ArmedForceStore, Is.SameAs(stagedParents.Forces));
        Assert.That(staged.ConflictStore, Is.SameAs(stagedParents.Conflicts));
        Assert.That(staged.TryGet(activeId, out PersistentWarRecord restoredActive), Is.True);
        Assert.That(restoredActive.Id, Is.Not.SameAs(activeId));
        Assert.That(restoredActive.ConflictId, Is.Not.SameAs(live.ConflictId));
        Assert.That(restoredActive.ConflictId, Is.EqualTo(live.ConflictId));
        Assert.That(restoredActive.Sides.Select(side => side.DisplayName), Is.EqualTo(new[] { "Alpha", "Beta", "Gamma" }));
        Assert.That(restoredActive.ParticipantBindings, Has.Count.EqualTo(2));
        Assert.That(restoredActive.ParticipantBindings[0].ArmedForceId, Is.Not.SameAs(live.FirstForceId));
        Assert.That(staged.TryGet(endedId, out PersistentWarRecord restoredEnded), Is.True);
        Assert.That(restoredEnded.ConflictId, Is.Null);
        Assert.That(restoredEnded.LifecycleState, Is.EqualTo(WarLifecycleState.Ended));
        Assert.That(restoredEnded.EndedAbsoluteDay, Is.EqualTo(4L));
    }

    [Test]
    public void InvalidSchemaCardinalityParentAndReferenceAreRejectedWithoutReturningStore()
    {
        WarWorld parents = CreateWorld();
        AssertStageFails(new PersistentWarOwnerSnapshot(2, 0, 0L, Array.Empty<PersistentWarOwnerSnapshotRecord>()),
            parents, PersistentWarOwnerSnapshotFailureCode.UnsupportedSchema);
        AssertStageFails(new PersistentWarOwnerSnapshot(1, 1, 0L, Array.Empty<PersistentWarOwnerSnapshotRecord>()),
            parents, PersistentWarOwnerSnapshotFailureCode.InvalidCardinality);

        WarId id = new WarId("invalid-reference-war");
        PersistentWarOwnerSnapshotRecord missingConflict = new PersistentWarOwnerSnapshotRecord(
            id.Value, 0L, WarLifecycleState.Active, null, "missing-conflict", ValidSides(id.Value),
            Array.Empty<PersistentWarOwnerBindingSnapshot>());
        AssertStageFails(new PersistentWarOwnerSnapshot(1, 1, 0L, new[] { missingConflict }),
            parents, PersistentWarOwnerSnapshotFailureCode.MissingConflict);

        PersistentWarOwnerSnapshotRecord valid = new PersistentWarOwnerSnapshotRecord(
            id.Value, 0L, WarLifecycleState.Active, null, null, ValidSides(id.Value),
            Array.Empty<PersistentWarOwnerBindingSnapshot>());
        AssertStageFails(new PersistentWarOwnerSnapshot(1, 2, 0L, new[] { valid, valid }),
            parents, PersistentWarOwnerSnapshotFailureCode.DuplicateWarIdentity);

        WarId malformedId = new WarId("bad-side-war");
        PersistentWarOwnerSnapshotRecord malformed = new PersistentWarOwnerSnapshotRecord(
            malformedId.Value, 0L, WarLifecycleState.Active, null, null,
            new[] { new PersistentWarOwnerSideSnapshot("wrong-parent", "side-a", "A"),
                new PersistentWarOwnerSideSnapshot(malformedId.Value, "side-b", "B") },
            Array.Empty<PersistentWarOwnerBindingSnapshot>());
        AssertStageFails(new PersistentWarOwnerSnapshot(1, 1, 0L, new[] { malformed }), parents,
            PersistentWarOwnerSnapshotFailureCode.InvalidSide);
    }

    [Test]
    public void StagingPreservesRevisionWithoutReplayingWritesIncludingSaturation()
    {
        WarWorld parents = CreateWorld();
        PersistentWarOwnerSnapshot saturated = new PersistentWarOwnerSnapshot(
            PersistentWarOwnerSnapshot.CurrentSchemaVersion,
            0,
            long.MaxValue,
            Array.Empty<PersistentWarOwnerSnapshotRecord>());
        Assert.That(saturated.TryStage(parents.Forces, parents.Conflicts,
            out PersistentWarStore staged, out PersistentWarOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(staged.TryRegister(new PersistentWarRecord(new WarId("saturated-write"), 0L,
            sides: new[]
            {
                new WarStateSide(new WarId("saturated-write"), new WarSideId("side-a")),
                new WarStateSide(new WarId("saturated-write"), new WarSideId("side-b"))
            }), out PersistentStateFailure writeFailure), Is.False);
        Assert.That(writeFailure.Code, Is.EqualTo(PersistentStateFailureCode.RevisionOverflow));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CaptureRejectsConfiguredP17WarInsteadOfOmittingItsExtension()
    {
        PersistentWarStore owner = CreateConfiguredP17WarStore();
        OwnerSectionCensusWitness witness = new PersistentWarCensusProvider(owner).GetCurrentCensus();
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = new[]
        {
            new OwnerSectionCensusSnapshot(witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision)
        };
        DailyCaptureEligibilityToken token = new DailyCaptureEligibilityToken(
            new object(), SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()), SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 0L, 1L, 0L, sections);

        Assert.That(PersistentWarOwnerSnapshot.TryCapture(owner, token, sections,
            out PersistentWarOwnerSnapshot snapshot, out PersistentWarOwnerSnapshotFailure failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(PersistentWarOwnerSnapshotFailureCode.UnsupportedP17A));
        Assert.That(owner.Count, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(2L));
    }

    private static void AssertStageFails(PersistentWarOwnerSnapshot snapshot, WarWorld parents,
        PersistentWarOwnerSnapshotFailureCode expected)
    {
        Assert.That(snapshot.TryStage(parents.Forces, parents.Conflicts,
            out PersistentWarStore staged, out PersistentWarOwnerSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expected));
    }

    private static PersistentWarOwnerSideSnapshot[] ValidSides(string warId) => new[]
    {
        new PersistentWarOwnerSideSnapshot(warId, "side-a", "A"),
        new PersistentWarOwnerSideSnapshot(warId, "side-b", "B")
    };

    private static PersistentWarOwnerSnapshot CaptureOrFail(PersistentWarStore owner)
    {
        OwnerSectionCensusWitness witness = new PersistentWarCensusProvider(owner).GetCurrentCensus();
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = new[]
        {
            new OwnerSectionCensusSnapshot(witness.SectionId, witness.SchemaVersion, OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision)
        };
        DailyCaptureEligibilityToken token = new DailyCaptureEligibilityToken(
            new object(), SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()), SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 0L, 1L, 0L, sections);
        Assert.That(PersistentWarOwnerSnapshot.TryCapture(owner, token, sections,
            out PersistentWarOwnerSnapshot snapshot, out PersistentWarOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(PersistentWarOwnerSnapshotFailureCode.None));
        return snapshot;
    }

    private static WarWorld CreateWorld()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId first = new ArmedForceId("snapshot-force-a");
        ArmedForceId second = new ArmedForceId("snapshot-force-b");
        Assert.That(forces.TryRegister(new ArmedForceRecord(first, "First", 0L), out _), Is.True);
        Assert.That(forces.TryRegister(new ArmedForceRecord(second, "Second", 0L), out _), Is.True);
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("snapshot-conflict");
        Assert.That(conflicts.TryRegister(new PersistentConflictRecord(conflictId, 0L, sides: new[]
        {
            new ConflictStateSide(conflictId, new ConflictSideId("conflict-a")),
            new ConflictStateSide(conflictId, new ConflictSideId("conflict-b"))
        }), out _), Is.True);
        return new WarWorld(forces, conflicts, new PersistentWarStore(forces, conflicts), first, second, conflictId);
    }

    private static PersistentWarStore CreateConfiguredP17WarStore()
    {
        const string authorityId = "snapshot-p17-authority";
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId ownerForce = new ArmedForceId("snapshot-p17-force-a");
        ArmedForceId targetForce = new ArmedForceId("snapshot-p17-force-b");
        Assert.That(forces.TryRegister(new ArmedForceRecord(ownerForce, "Owner", 0L), out _), Is.True);
        Assert.That(forces.TryRegister(new ArmedForceRecord(targetForce, "Target", 0L), out _), Is.True);

        HexId sourceHex = new HexId("snapshot-p17-source");
        SpatialAuthorityStore spatialAuthority = new SpatialAuthorityStore();
        Assert.That(spatialAuthority.TryComposeGeography(new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("snapshot-scale", "snapshot-p17", "v1", 1m, "step"),
            new[]
            {
                new HexRecord(sourceHex, new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("snapshot-terrain"), "v1"))
            }), out _), Is.True);
        Assert.That(ArmedForceSpatialStateStore.TryCreateP16A(forces, spatialAuthority, targetForce,
            sourceHex, "ration.item", "v1", 5m, 2m, 10L,
            out ArmedForceSpatialStateStore p16, out ArmedForceSpatialFailure p16Failure), Is.True, p16Failure.ToString());
        Assert.That(p16.ConfigureP17AProvenance(authorityId), Is.True);

        FactionStore factions = new FactionStore(persons);
        FactionId ownerFaction = new FactionId("snapshot-p17-faction-a");
        FactionId targetFaction = new FactionId("snapshot-p17-faction-b");
        Assert.That(factions.TryRegister(new FactionRecord(ownerFaction, "Owner", 0L), out _), Is.True);
        Assert.That(factions.TryRegister(new FactionRecord(targetFaction, "Target", 0L), out _), Is.True);

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts, factions, spatialAuthority, p16);
        WarId warId = new WarId("snapshot-p17-war");
        WarSideId ownerSide = new WarSideId("snapshot-p17-side-a");
        WarSideId targetSide = new WarSideId("snapshot-p17-side-b");
        WarStrategicParticipantId ownerParticipant = new WarStrategicParticipantId("snapshot-p17-participant-a");
        WarStrategicParticipantId targetParticipant = new WarStrategicParticipantId("snapshot-p17-participant-b");
        WarParticipantBindingId ownerBinding = new WarParticipantBindingId("snapshot-p17-binding-a");
        WarParticipantBindingId targetBinding = new WarParticipantBindingId("snapshot-p17-binding-b");
        Assert.That(wars.TryRegister(new PersistentWarRecord(warId, 0L,
            sides: new[] { new WarStateSide(warId, ownerSide), new WarStateSide(warId, targetSide) },
            participantBindings: new[]
            {
                new WarParticipantBinding(ownerBinding, warId, ownerSide, ownerForce),
                new WarParticipantBinding(targetBinding, warId, targetSide, targetForce)
            }), out _), Is.True);
        P17AWarStrategicSection section = new P17AWarStrategicSection(authorityId,
            new[]
            {
                new WarStrategicParticipant(ownerParticipant, warId, ownerFaction, ownerSide),
                new WarStrategicParticipant(targetParticipant, warId, targetFaction, targetSide)
            },
            new WarWithdrawalDemand(new WarActualGoalId("snapshot-p17-goal"), warId,
                ownerParticipant, targetParticipant, targetBinding, sourceHex, 0L));
        Assert.That(wars.TryConfigureP17A(warId, section, out PersistentStateFailure configureFailure), Is.True,
            configureFailure.ToString());
        return wars;
    }

    private sealed class WarWorld
    {
        internal ArmedForceStore Forces { get; }
        internal PersistentConflictStore Conflicts { get; }
        internal PersistentWarStore Wars { get; }
        internal ArmedForceId FirstForceId { get; }
        internal ArmedForceId SecondForceId { get; }
        internal ConflictId ConflictId { get; }
        internal WarWorld(ArmedForceStore forces, PersistentConflictStore conflicts, PersistentWarStore wars,
            ArmedForceId firstForceId, ArmedForceId secondForceId, ConflictId conflictId)
        { Forces = forces; Conflicts = conflicts; Wars = wars; FirstForceId = firstForceId; SecondForceId = secondForceId; ConflictId = conflictId; }
    }
}
