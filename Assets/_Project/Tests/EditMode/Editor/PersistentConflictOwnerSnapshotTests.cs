using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class PersistentConflictOwnerSnapshotTests
{
    [Test]
    public void EmptySnapshot_CapturesExactRequiredWitnessAndStagesExactZero()
    {
        ConflictWorld live = CreateWorld("force-a");
        PersistentConflictOwnerSnapshot first = CaptureOrFail(live.Conflicts);
        PersistentConflictOwnerSnapshot second = CaptureOrFail(live.Conflicts);

        Assert.That(first.SchemaVersion, Is.EqualTo(PersistentConflictOwnerSnapshot.CurrentSchemaVersion));
        Assert.That(first.RecordCount, Is.Zero);
        Assert.That(first.Revision, Is.Zero);
        Assert.That(first.Records, Is.Empty);
        Assert.That(second.Records, Is.Not.SameAs(first.Records));

        ArmedForceStore stagedForces = CreateForces("force-a");
        Assert.That(first.TryStage(stagedForces, out PersistentConflictStore staged,
            out PersistentConflictOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(PersistentConflictOwnerSnapshotFailureCode.None));
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged, Is.Not.SameAs(live.Conflicts));
        Assert.That(staged.ArmedForceStore, Is.SameAs(stagedForces));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
    }

    [Test]
    public void PopulatedSnapshot_RoundTripsDetachedRowsLifecycleBindingsAndExactRevision()
    {
        ConflictWorld live = CreateWorld("force-a", "force-b");
        ConflictId activeId = new ConflictId("conflict-active");
        ConflictSideId activeSideA = new ConflictSideId("active-a");
        ConflictSideId activeSideB = new ConflictSideId("active-b");
        PersistentConflictRecord active = new PersistentConflictRecord(
            activeId,
            2L,
            sides: new[]
            {
                new ConflictStateSide(activeId, activeSideB, "Beta"),
                new ConflictStateSide(activeId, activeSideA, "Alpha")
            },
            participantBindings: new[]
            {
                new ConflictParticipantBinding(
                    new ConflictParticipantBindingId("binding-a"),
                    activeId,
                    activeSideA,
                    new ArmedForceId("force-a"))
            });
        Assert.That(live.Conflicts.TryRegister(active, out PersistentStateFailure registerFailure), Is.True, registerFailure.ToString());

        ConflictId endedId = new ConflictId("conflict-ended");
        Assert.That(live.Conflicts.TryRegister(CreateConflict(endedId, 3L), out PersistentStateFailure secondFailure), Is.True, secondFailure.ToString());
        Assert.That(live.Conflicts.TryEnd(endedId, 5L, out PersistentStateFailure endFailure), Is.True, endFailure.ToString());
        Assert.That(live.Conflicts.Revision, Is.EqualTo(3L));

        PersistentConflictOwnerSnapshot snapshot = CaptureOrFail(live.Conflicts);
        Assert.That(snapshot.RecordCount, Is.EqualTo(2));
        Assert.That(snapshot.Revision, Is.EqualTo(3L));
        Assert.That(snapshot.Records[0].ConflictIdValue, Is.EqualTo("conflict-active"));
        Assert.That(snapshot.Records[0].Sides[0].SideIdValue, Is.EqualTo("active-a"));
        Assert.That(snapshot.Records[0].Sides[0].DisplayName, Is.EqualTo("Alpha"));
        Assert.That(snapshot.Records[0].ParticipantBindings[0].ArmedForceIdValue, Is.EqualTo("force-a"));
        Assert.That(((IList<PersistentConflictOwnerSnapshotRecord>)snapshot.Records).IsReadOnly, Is.True);
        Assert.That(((IList<PersistentConflictOwnerSideSnapshot>)snapshot.Records[0].Sides).IsReadOnly, Is.True);

        // The captured relation remains valid when its Force is now terminal.
        ArmedForceStore stagedForces = CreateForces(
            new ArmedForceRecord(new ArmedForceId("force-a"), "Force A", 0L,
                lifecycleState: ArmedForceLifecycleState.Terminated, terminatedAbsoluteDay: 4L),
            new ArmedForceRecord(new ArmedForceId("force-b"), "Force B", 0L));
        Assert.That(snapshot.TryStage(stagedForces, out PersistentConflictStore staged,
            out PersistentConflictOwnerSnapshotFailure stageFailure), Is.True, stageFailure.Message);
        Assert.That(staged.Count, Is.EqualTo(2));
        Assert.That(staged.Revision, Is.EqualTo(snapshot.Revision));
        Assert.That(staged.ArmedForceStore, Is.SameAs(stagedForces));
        Assert.That(staged.TryGet(new ConflictId("conflict-active"), out PersistentConflictRecord stagedActive), Is.True);
        Assert.That(stagedActive.CreatedAbsoluteDay, Is.EqualTo(2L));
        Assert.That(stagedActive.LifecycleState, Is.EqualTo(ConflictLifecycleState.Active));
        Assert.That(stagedActive.Sides[0].DisplayName, Is.EqualTo("Alpha"));
        Assert.That(stagedActive.ParticipantBindings[0].BindingId.Value, Is.EqualTo("binding-a"));
        Assert.That(stagedActive.ParticipantBindings[0].ArmedForceId.Value, Is.EqualTo("force-a"));
        Assert.That(staged.TryGet(new ConflictId("conflict-ended"), out PersistentConflictRecord stagedEnded), Is.True);
        Assert.That(stagedEnded.LifecycleState, Is.EqualTo(ConflictLifecycleState.Ended));
        Assert.That(stagedEnded.EndedAbsoluteDay, Is.EqualTo(5L));
        Assert.That(staged.ValidateInvariants().IsValid, Is.True);

        Assert.That(live.Conflicts.TryRegister(CreateConflict(new ConflictId("later"), 6L), out _), Is.True);
        Assert.That(snapshot.RecordCount, Is.EqualTo(2));
        Assert.That(snapshot.Revision, Is.EqualTo(3L));
        Assert.That(snapshot.Records, Has.Count.EqualTo(2));
    }

    [Test]
    public void SupportedWritersAdvanceOnceAndRejectedWritesPreserveOwnerValues()
    {
        ArmedForceStore forces = CreateForces(
            new ArmedForceRecord(new ArmedForceId("force-a"), "force-a", 0L),
            new ArmedForceRecord(new ArmedForceId("force-terminal"), "force-terminal", 0L,
                lifecycleState: ArmedForceLifecycleState.Terminated, terminatedAbsoluteDay: 1L));
        PersistentConflictStore owner = new PersistentConflictStore(forces);
        ConflictId id = new ConflictId("writer-conflict");
        PersistentConflictRecord record = CreateConflict(id, 4L);

        Assert.That(owner.TryRegister(record, out _), Is.True);
        Assert.That(owner.Count, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(1L));
        AssertRejectedWritePreserves(owner, () => owner.TryRegister(record, out _));

        ConflictParticipantBinding binding = new ConflictParticipantBinding(
            new ConflictParticipantBindingId("binding-a"), id, new ConflictSideId("side-a"), new ArmedForceId("force-a"));
        Assert.That(owner.TryAddParticipantBinding(id, binding, out _), Is.True);
        Assert.That(owner.Count, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(2L));
        AssertRejectedWritePreserves(owner, () => owner.TryAddParticipantBinding(id, binding, out _));
        AssertRejectedWritePreserves(owner, () => owner.TryAddParticipantBinding(
            new ConflictId("missing-conflict"), binding, out _));
        AssertRejectedWritePreserves(owner, () => owner.TryAddParticipantBinding(id,
            new ConflictParticipantBinding(new ConflictParticipantBindingId("bad-side"), id,
                new ConflictSideId("missing-side"), new ArmedForceId("force-a")), out _));
        AssertRejectedWritePreserves(owner, () => owner.TryAddParticipantBinding(id,
            new ConflictParticipantBinding(new ConflictParticipantBindingId("missing-force"), id,
                new ConflictSideId("side-a"), new ArmedForceId("missing-force")), out _));
        AssertRejectedWritePreserves(owner, () => owner.TryAddParticipantBinding(id,
            new ConflictParticipantBinding(new ConflictParticipantBindingId("inactive-force"), id,
                new ConflictSideId("side-a"), new ArmedForceId("force-terminal")), out _));

        Assert.That(owner.TryEnd(id, 8L, out _), Is.True);
        Assert.That(owner.Count, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(3L));
        AssertRejectedWritePreserves(owner, () => owner.TryEnd(id, 9L, out _));
        AssertRejectedWritePreserves(owner, () => owner.TryAddParticipantBinding(id,
            new ConflictParticipantBinding(new ConflictParticipantBindingId("late"), id,
                new ConflictSideId("side-a"), new ArmedForceId("force-a")), out _));

        ConflictId invalidEndId = new ConflictId("invalid-end-date");
        Assert.That(owner.TryRegister(CreateConflict(invalidEndId, 10L), out _), Is.True);
        AssertRejectedWritePreserves(owner, () => owner.TryEnd(invalidEndId, 9L, out _));
        Assert.That(owner.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void FaultedMutationGuardRejectsAllThreeWritersWithoutChangingRows()
    {
        ArmedForceStore forces = CreateForces("force-a");
        PersistentConflictStore owner = new PersistentConflictStore(forces);
        ConflictId id = new ConflictId("faulted-guard-conflict");
        Assert.That(owner.TryRegister(CreateConflict(id, 0L), out _), Is.True);
        AuthoritativeMutationGuard guard = new AuthoritativeMutationGuard();
        Assert.That(owner.TryBindMutationGuard(guard), Is.True);
        guard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);

        int count = owner.Count;
        long revision = owner.Revision;
        Assert.That(owner.TryRegister(CreateConflict(new ConflictId("blocked-register"), 0L), out _), Is.False);
        Assert.That(owner.TryAddParticipantBinding(id,
            new ConflictParticipantBinding(new ConflictParticipantBindingId("blocked-binding"), id,
                new ConflictSideId("side-a"), new ArmedForceId("force-a")), out _), Is.False);
        Assert.That(owner.TryEnd(id, 1L, out _), Is.False);
        Assert.That(owner.Count, Is.EqualTo(count));
        Assert.That(owner.Revision, Is.EqualTo(revision));
        Assert.That(owner.TryGet(id, out PersistentConflictRecord retained), Is.True);
        Assert.That(retained.ParticipantBindings, Is.Empty);
        Assert.That(retained.LifecycleState, Is.EqualTo(ConflictLifecycleState.Active));
    }

    [Test]
    public void CaptureRejectsTokenAndRequiredWitnessMismatches()
    {
        ConflictWorld world = CreateWorld("force-a");
        IReadOnlyList<OwnerSectionCensusSnapshot> correct = CreateOwnerSections(world.Conflicts);
        DailyCaptureEligibilityToken token = CreateToken(correct);

        AssertCaptureRejected(world.Conflicts, token, correct.ToArray(),
            PersistentConflictOwnerSnapshotFailureCode.InvalidCaptureContext);
        AssertCaptureRejected(world.Conflicts, CreateToken(correct, completedCoreSequence: 0L), correct,
            PersistentConflictOwnerSnapshotFailureCode.UnsupportedProfile);
        IReadOnlyList<OwnerSectionCensusSnapshot> wrongRole = CreateOwnerSections(
            world.Conflicts, role: OwnerSectionRole.ExplicitlyEmpty);
        AssertCaptureRejected(world.Conflicts, CreateToken(wrongRole), wrongRole,
            PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector);

        IReadOnlyList<OwnerSectionCensusSnapshot> missing = Array.Empty<OwnerSectionCensusSnapshot>();
        AssertCaptureRejected(world.Conflicts, CreateToken(missing), missing,
            PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector);

        IReadOnlyList<OwnerSectionCensusSnapshot> duplicate = new[] { correct[0], correct[0] };
        AssertCaptureRejected(world.Conflicts, CreateToken(duplicate), duplicate,
            PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector);

        IReadOnlyList<OwnerSectionCensusSnapshot> wrongOwner = CreateOwnerSections(
            world.Conflicts, ownerIdentity: new object());
        AssertCaptureRejected(world.Conflicts, CreateToken(wrongOwner), wrongOwner,
            PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector);

        IReadOnlyList<OwnerSectionCensusSnapshot> wrongSchema = CreateOwnerSections(
            world.Conflicts, schemaVersion: PersistentConflictCensusProvider.SchemaVersion + 1);
        AssertCaptureRejected(world.Conflicts, CreateToken(wrongSchema), wrongSchema,
            PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector);

        IReadOnlyList<OwnerSectionCensusSnapshot> wrongCardinality = CreateOwnerSections(
            world.Conflicts, cardinality: 1);
        AssertCaptureRejected(world.Conflicts, CreateToken(wrongCardinality), wrongCardinality,
            PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector);

        IReadOnlyList<OwnerSectionCensusSnapshot> wrongRevision = CreateOwnerSections(
            world.Conflicts, revision: 1L);
        AssertCaptureRejected(world.Conflicts, CreateToken(wrongRevision), wrongRevision,
            PersistentConflictOwnerSnapshotFailureCode.InvalidOwnerSectionVector);
    }

    [Test]
    public void StageRejectsMalformedIdentityRelationshipsAndCardinalityWithoutReturningPartialOwner()
    {
        ConflictWorld source = CreateWorld("force-a");
        ConflictId id = new ConflictId("valid-conflict");
        Assert.That(source.Conflicts.TryRegister(CreateConflict(id, 0L,
            new ConflictParticipantBinding(new ConflictParticipantBindingId("binding-a"), id,
                new ConflictSideId("side-a"), new ArmedForceId("force-a"))), out _), Is.True);
        PersistentConflictOwnerSnapshot valid = CaptureOrFail(source.Conflicts);
        ArmedForceStore targetForces = CreateForces("force-a");

        AssertStageRejected(new PersistentConflictOwnerSnapshot(2, 1, valid.Revision, valid.Records), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.UnsupportedSchema);
        AssertStageRejected(new PersistentConflictOwnerSnapshot(valid.SchemaVersion, 1, -1L, valid.Records), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidRevision);
        AssertStageRejected(new PersistentConflictOwnerSnapshot(valid.SchemaVersion, 2, valid.Revision, valid.Records), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidCardinality);

        PersistentConflictOwnerSnapshotRecord row = valid.Records[0];
        AssertStageRejected(SnapshotWith(valid, new[] { row, row }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.DuplicateConflictIdentity);
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, conflictIdValue: " ") }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidConflictIdentity);
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, lifecycleState: (ConflictLifecycleState)99) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidLifecycle);
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, endedAbsoluteDay: 0L, lifecycleState: ConflictLifecycleState.Active, replaceEndedDay: true) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidLifecycle);

        PersistentConflictOwnerSideSnapshot badParentSide = new PersistentConflictOwnerSideSnapshot(
            "other-conflict", "side-a", "Alpha");
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, sides: new[] { badParentSide, row.Sides[1] }) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidSide);
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, sides: new[] { row.Sides[0] }) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidSide);
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, sides: new[] { row.Sides[0], row.Sides[0] }) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.DuplicateSide);

        PersistentConflictOwnerBindingSnapshot missingForce = new PersistentConflictOwnerBindingSnapshot(
            "binding-missing-force", row.ConflictIdValue, row.Sides[0].SideIdValue, "absent-force");
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, bindings: new[] { missingForce }) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.MissingArmedForce);
        PersistentConflictOwnerBindingSnapshot wrongBindingParent = new PersistentConflictOwnerBindingSnapshot(
            "binding-other-parent", "other-conflict", row.Sides[0].SideIdValue, "force-a");
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, bindings: new[] { wrongBindingParent }) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidBinding);
        PersistentConflictOwnerBindingSnapshot missingSide = new PersistentConflictOwnerBindingSnapshot(
            "binding-missing-side", row.ConflictIdValue, "missing-side", "force-a");
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, bindings: new[] { missingSide }) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.InvalidBinding);
        AssertStageRejected(SnapshotWith(valid, new[] { CopyRow(row, bindings: new[] { row.ParticipantBindings[0], row.ParticipantBindings[0] }) }), targetForces,
            PersistentConflictOwnerSnapshotFailureCode.DuplicateBinding);

        ArmedForceStore missingParentForce = CreateForces("another-force");
        long parentRevision = missingParentForce.Revision;
        AssertStageRejected(valid, missingParentForce,
            PersistentConflictOwnerSnapshotFailureCode.MissingArmedForce);
        Assert.That(missingParentForce.Revision, Is.EqualTo(parentRevision));
    }

    [Test]
    public void StagePreservesSaturatedRevisionAndWritesFailWithoutMutation()
    {
        ConflictWorld source = CreateWorld("force-a");
        PersistentConflictOwnerSnapshot empty = CaptureOrFail(source.Conflicts);
        PersistentConflictOwnerSnapshot saturated = new PersistentConflictOwnerSnapshot(
            PersistentConflictOwnerSnapshot.CurrentSchemaVersion,
            0,
            long.MaxValue,
            empty.Records);

        Assert.That(saturated.TryStage(CreateForces("force-a"), out PersistentConflictStore staged,
            out PersistentConflictOwnerSnapshotFailure failure), Is.True, failure.Message);
        PersistentConflictRecord candidate = CreateConflict(new ConflictId("saturated"), 0L);
        Assert.That(staged.TryRegister(candidate, out PersistentStateFailure writeFailure), Is.False);
        Assert.That(writeFailure.Code, Is.EqualTo(PersistentStateFailureCode.RevisionOverflow));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));

        ConflictWorld populated = CreateWorld("force-a");
        ConflictId id = new ConflictId("saturated-populated");
        Assert.That(populated.Conflicts.TryRegister(CreateConflict(id, 0L), out _), Is.True);
        PersistentConflictOwnerSnapshot captured = CaptureOrFail(populated.Conflicts);
        PersistentConflictOwnerSnapshot saturatedPopulated = new PersistentConflictOwnerSnapshot(
            captured.SchemaVersion, captured.RecordCount, long.MaxValue, captured.Records);
        Assert.That(saturatedPopulated.TryStage(CreateForces("force-a"), out PersistentConflictStore populatedStage,
            out PersistentConflictOwnerSnapshotFailure populatedFailure), Is.True, populatedFailure.Message);
        ConflictParticipantBinding binding = new ConflictParticipantBinding(
            new ConflictParticipantBindingId("overflow-binding"), id, new ConflictSideId("side-a"), new ArmedForceId("force-a"));
        Assert.That(populatedStage.TryAddParticipantBinding(id, binding, out PersistentStateFailure bindingFailure), Is.False);
        Assert.That(bindingFailure.Code, Is.EqualTo(PersistentStateFailureCode.RevisionOverflow));
        Assert.That(populatedStage.TryEnd(id, 1L, out PersistentStateFailure endFailure), Is.False);
        Assert.That(endFailure.Code, Is.EqualTo(PersistentStateFailureCode.RevisionOverflow));
        Assert.That(populatedStage.Count, Is.EqualTo(1));
        Assert.That(populatedStage.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(populatedStage.TryGet(id, out PersistentConflictRecord unchanged), Is.True);
        Assert.That(unchanged.ParticipantBindings, Is.Empty);
        Assert.That(unchanged.LifecycleState, Is.EqualTo(ConflictLifecycleState.Active));
    }

    private static PersistentConflictOwnerSnapshot CaptureOrFail(PersistentConflictStore owner)
    {
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = CreateOwnerSections(owner);
        DailyCaptureEligibilityToken token = CreateToken(sections);
        Assert.That(PersistentConflictOwnerSnapshot.TryCapture(owner, token, sections,
            out PersistentConflictOwnerSnapshot snapshot,
            out PersistentConflictOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(PersistentConflictOwnerSnapshotFailureCode.None));
        return snapshot;
    }

    private static IReadOnlyList<OwnerSectionCensusSnapshot> CreateOwnerSections(
        PersistentConflictStore owner,
        OwnerSectionRole role = OwnerSectionRole.Required,
        int? schemaVersion = null,
        object ownerIdentity = null,
        int? cardinality = null,
        long? revision = null)
    {
        OwnerSectionCensusWitness witness = new PersistentConflictCensusProvider(owner).GetCurrentCensus();
        return new[]
        {
            new OwnerSectionCensusSnapshot(
                witness.SectionId,
                schemaVersion ?? witness.SchemaVersion,
                role,
                ownerIdentity ?? witness.OwnerInstanceIdentity,
                cardinality ?? witness.Cardinality,
                revision ?? witness.Revision)
        };
    }

    private static DailyCaptureEligibilityToken CreateToken(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        long completedCoreSequence = 1L)
    {
        return new DailyCaptureEligibilityToken(
            new object(),
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()),
            0L,
            completedCoreSequence,
            0L,
            sections);
    }

    private static void AssertCaptureRejected(
        PersistentConflictStore owner,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        PersistentConflictOwnerSnapshotFailureCode expected)
    {
        Assert.That(PersistentConflictOwnerSnapshot.TryCapture(owner, token, sections,
            out PersistentConflictOwnerSnapshot snapshot,
            out PersistentConflictOwnerSnapshotFailure failure), Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expected), failure.Message);
    }

    private static void AssertStageRejected(
        PersistentConflictOwnerSnapshot snapshot,
        ArmedForceStore targetForces,
        PersistentConflictOwnerSnapshotFailureCode expected)
    {
        Assert.That(snapshot.TryStage(targetForces, out PersistentConflictStore staged,
            out PersistentConflictOwnerSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expected), failure.Message);
    }

    private static PersistentConflictOwnerSnapshot SnapshotWith(
        PersistentConflictOwnerSnapshot source,
        IEnumerable<PersistentConflictOwnerSnapshotRecord> rows) => new PersistentConflictOwnerSnapshot(
            source.SchemaVersion,
            rows is ICollection<PersistentConflictOwnerSnapshotRecord> collection ? collection.Count : 0,
            source.Revision,
            rows);

    private static PersistentConflictOwnerSnapshotRecord CopyRow(
        PersistentConflictOwnerSnapshotRecord source,
        string conflictIdValue = null,
        long? endedAbsoluteDay = null,
        ConflictLifecycleState? lifecycleState = null,
        IReadOnlyList<PersistentConflictOwnerSideSnapshot> sides = null,
        IReadOnlyList<PersistentConflictOwnerBindingSnapshot> bindings = null,
        bool replaceEndedDay = false) => new PersistentConflictOwnerSnapshotRecord(
            conflictIdValue ?? source.ConflictIdValue,
            source.CreatedAbsoluteDay,
            lifecycleState ?? source.LifecycleState,
            replaceEndedDay ? endedAbsoluteDay : source.EndedAbsoluteDay,
            sides ?? source.Sides,
            bindings ?? source.ParticipantBindings);

    private static string SnapshotValues(PersistentConflictOwnerSnapshot snapshot)
    {
        List<string> values = new List<string>();
        foreach (PersistentConflictOwnerSnapshotRecord row in snapshot.Records)
        {
            values.Add(row.ConflictIdValue + "|" + row.CreatedAbsoluteDay + "|" + row.LifecycleState + "|" + row.EndedAbsoluteDay);
            foreach (PersistentConflictOwnerSideSnapshot side in row.Sides)
                values.Add(side.ConflictIdValue + "|" + side.SideIdValue + "|" + side.DisplayName);
            foreach (PersistentConflictOwnerBindingSnapshot binding in row.ParticipantBindings)
                values.Add(binding.BindingIdValue + "|" + binding.ConflictIdValue + "|" + binding.SideIdValue + "|" + binding.ArmedForceIdValue);
        }
        return string.Join("\n", values);
    }
    private static void AssertRejectedWritePreserves(
        PersistentConflictStore owner,
        Func<bool> operation)
    {
        PersistentConflictOwnerSnapshot before = CaptureOrFail(owner);
        Assert.That(operation(), Is.False);
        Assert.That(owner.Count, Is.EqualTo(before.RecordCount));
        Assert.That(owner.Revision, Is.EqualTo(before.Revision));
        PersistentConflictOwnerSnapshot after = CaptureOrFail(owner);
        Assert.That(after.RecordCount, Is.EqualTo(before.RecordCount));
        Assert.That(after.Revision, Is.EqualTo(before.Revision));
        Assert.That(SnapshotValues(after), Is.EqualTo(SnapshotValues(before)));
    }

    private static PersistentConflictRecord CreateConflict(
        ConflictId id,
        long createdDay,
        params ConflictParticipantBinding[] bindings) => new PersistentConflictRecord(
            id,
            createdDay,
            sides: new[]
            {
                new ConflictStateSide(id, new ConflictSideId("side-a"), "Alpha"),
                new ConflictStateSide(id, new ConflictSideId("side-b"), "Beta")
            },
            participantBindings: bindings);

    private static ConflictWorld CreateWorld(params string[] forceIds) =>
        new ConflictWorld(CreateForces(forceIds));

    private static ArmedForceStore CreateForces(params string[] forceIds)
    {
        ArmedForceStore forces = new ArmedForceStore(new PersonStore());
        foreach (string id in forceIds)
        {
            Assert.That(forces.TryRegister(new ArmedForceRecord(new ArmedForceId(id), id, 0L), out _), Is.True);
        }
        return forces;
    }

    private static ArmedForceStore CreateForces(params ArmedForceRecord[] records)
    {
        ArmedForceStore forces = new ArmedForceStore(new PersonStore());
        foreach (ArmedForceRecord record in records)
            Assert.That(forces.TryRegister(record, out _), Is.True);
        return forces;
    }

    private sealed class ConflictWorld
    {
        internal ArmedForceStore Forces { get; }
        internal PersistentConflictStore Conflicts { get; }

        internal ConflictWorld(ArmedForceStore forces)
        {
            Forces = forces;
            Conflicts = new PersistentConflictStore(forces);
        }
    }
}
