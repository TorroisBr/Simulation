using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class PersistentBattleOwnerSnapshotTests
{
    [Test]
    public void EmptyOwnerSnapshot_CapturesExactSectionAndStagesAsExactZero()
    {
        BattleWorld live = CreateWorld();
        PersistentBattleStore owner = CreateBattleStore(live);
        PersistentBattleOwnerSnapshot first = CaptureOrFail(owner, 0L);
        PersistentBattleOwnerSnapshot second = CaptureOrFail(owner, 0L);

        Assert.That(first.SchemaVersion, Is.EqualTo(PersistentBattleOwnerSnapshot.CurrentSchemaVersion));
        Assert.That(first.RecordCount, Is.Zero);
        Assert.That(first.Revision, Is.Zero);
        Assert.That(first.Records, Is.Empty);
        Assert.That(second.Records, Is.Not.SameAs(first.Records));
        Assert.That(second.Revision, Is.EqualTo(first.Revision));

        BattleWorld stagedParents = CreateWorld();
        Assert.That(first.TryStage(
            stagedParents.Forces,
            stagedParents.Conflicts,
            stagedParents.Wars,
            stagedParents.Spatial,
            null,
            out PersistentBattleStore staged,
            out PersistentBattleOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.None));
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged, Is.Not.SameAs(owner));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.Zero);
        Assert.That(staged.ArmedForceStore, Is.SameAs(stagedParents.Forces));
        Assert.That(staged.ConflictStore, Is.SameAs(stagedParents.Conflicts));
        Assert.That(staged.WarStore, Is.SameAs(stagedParents.Wars));
        Assert.That(staged.SpatialAuthorityStore, Is.SameAs(stagedParents.Spatial));
        Assert.That(staged.LocalTopologyStore, Is.Null);

        ArmedForceStore unrelatedForces = new ArmedForceStore(new PersonStore());
        PersistentConflictStore unrelatedConflicts = new PersistentConflictStore(unrelatedForces);
        Assert.That(first.TryStage(
            unrelatedForces,
            unrelatedConflicts,
            stagedParents.Wars,
            stagedParents.Spatial,
            null,
            out PersistentBattleStore wrongParentStage,
            out PersistentBattleOwnerSnapshotFailure wrongParentFailure), Is.False);
        Assert.That(wrongParentStage, Is.Null);
        Assert.That(wrongParentFailure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.InvalidParentComposition));
    }

    [Test]
    public void OwnerSnapshot_RoundTripsPendingActiveAndResolvedRowsWithoutReplayingConsequences()
    {
        BattleWorld live = CreateWorld();
        PersistentBattleStore owner = CreateBattleStore(live);
        BattleId battleId = new BattleId("snapshot-battle");
        BattleSideId firstSide = new BattleSideId("side-a");
        BattleSideId secondSide = new BattleSideId("side-b");
        Assert.That(owner.TryRegister(new PersistentBattleRecord(
            battleId,
            2L,
            live.ConflictId,
            live.WarId,
            sides: new[]
            {
                new BattleStateSide(battleId, secondSide, "Second"),
                new BattleStateSide(battleId, firstSide, "First")
            },
            participantBindings: new[]
            {
                new BattleParticipantBinding(
                    new BattleParticipantBindingId("binding-a"), battleId, firstSide, live.FirstForceId)
            },
            locationReference: SpatialReference.ForLocation(new LocationId(live.LocationId.Value))),
            out PersistentStateFailure registerFailure), Is.True, registerFailure.ToString());

        PersistentBattleOwnerSnapshot pending = CaptureOrFail(owner, 2L);
        Assert.That(pending.Records[0].LifecycleState, Is.EqualTo(BattleLifecycleState.Pending));
        Assert.That(pending.Records[0].Sides.Select(side => side.SideIdValue), Is.EqualTo(new[] { "side-a", "side-b" }));
        Assert.That(pending.Records[0].ParticipantBindings[0].ArmedForceIdValue, Is.EqualTo(live.FirstForceId.Value));
        Assert.That(owner.TryAddParticipantBinding(
            battleId,
            new BattleParticipantBinding(
                new BattleParticipantBindingId("binding-b"), battleId, secondSide, live.SecondForceId),
            out PersistentStateFailure bindingFailure), Is.True, bindingFailure.ToString());
        Assert.That(owner.TryStart(
            battleId,
            3L,
            SpatialReference.ForLocation(new LocationId(live.LocationId.Value)),
            out PersistentStateFailure startFailure), Is.True, startFailure.ToString());

        BattleWorld stagedParents = CreateWorld();
        Assert.That(pending.TryStage(
            stagedParents.Forces, stagedParents.Conflicts, stagedParents.Wars, stagedParents.Spatial, null,
            out PersistentBattleStore stagedPending, out PersistentBattleOwnerSnapshotFailure pendingFailure),
            Is.True, pendingFailure.Message);
        Assert.That(stagedPending.TryGet(new BattleId("snapshot-battle"), out PersistentBattleRecord stagedPendingRow), Is.True);
        Assert.That(stagedPendingRow.Id, Is.Not.SameAs(battleId));
        Assert.That(stagedPendingRow.Id, Is.EqualTo(battleId));
        Assert.That(stagedPendingRow.ConflictId, Is.Not.SameAs(live.ConflictId));
        Assert.That(stagedPendingRow.WarId, Is.Not.SameAs(live.WarId));
        Assert.That(stagedPendingRow.LocationReference.LocationId, Is.Not.SameAs(live.LocationId));
        Assert.That(stagedPendingRow.LocationReference, Is.EqualTo(owner.Records[0].LocationReference));
        Assert.That(stagedPendingRow.Sides.Select(side => side.DisplayName), Is.EqualTo(new[] { "First", "Second" }));
        Assert.That(stagedPendingRow.ParticipantBindings, Has.Count.EqualTo(1));
        Assert.That(stagedPendingRow.ParticipantBindings[0].ArmedForceId, Is.Not.SameAs(live.FirstForceId));
        Assert.That(stagedPendingRow.ParticipantBindings[0].ArmedForceId, Is.EqualTo(live.FirstForceId));
        Assert.That(stagedPendingRow.ParticipantBindings[0].BindingId, Is.Not.SameAs(owner.Records[0].ParticipantBindings[0].BindingId));
        Assert.That(stagedPending.ArmedForceStore, Is.SameAs(stagedParents.Forces));
        Assert.That(stagedPending.ConflictStore, Is.SameAs(stagedParents.Conflicts));
        Assert.That(stagedPending.WarStore, Is.SameAs(stagedParents.Wars));
        Assert.That(stagedPending.SpatialAuthorityStore, Is.SameAs(stagedParents.Spatial));

        PersistentBattleOwnerSnapshot active = CaptureOrFail(owner, 3L);
        Assert.That(active.Records[0].LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
        Assert.That(active.Records[0].StartedAbsoluteDay, Is.EqualTo(3L));
        Assert.That(active.Revision, Is.EqualTo(3L));
        Assert.That(active.Records[0].ParticipantBindings, Has.Count.EqualTo(2));
        Assert.That(active.TryStage(
            stagedParents.Forces, stagedParents.Conflicts, stagedParents.Wars, stagedParents.Spatial, null,
            out PersistentBattleStore stagedActive, out PersistentBattleOwnerSnapshotFailure activeFailure),
            Is.True, activeFailure.Message);
        Assert.That(stagedActive.TryGet(battleId, out PersistentBattleRecord stagedActiveRow), Is.True);
        Assert.That(stagedActiveRow.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
        Assert.That(stagedActiveRow.StartedAbsoluteDay, Is.EqualTo(3L));
        Assert.That(stagedActiveRow.LocationReference, Is.EqualTo(owner.Records[0].LocationReference));

        PersistentBattleOutcomeProvenance provenance = CreateProvenance();
        PersistentBattleTerminalOutcome outcome = new PersistentBattleTerminalOutcome(
            battleId, BattleOutcomeType.Victory, firstSide, 5L, provenance);
        Assert.That(owner.TryPrepareTerminalWrite(
            outcome,
            out PreparedBattleTerminalWrite prepared,
            out PersistentStateFailure prepareFailure), Is.True, prepareFailure.ToString());
        Assert.That(owner.TryCommitTerminalWrite(
            prepared,
            out bool authoritativeWriteStarted,
            out PersistentStateFailure commitFailure), Is.True, commitFailure.ToString());
        Assert.That(authoritativeWriteStarted, Is.True);

        long liveRevisionBeforeStage = owner.Revision;
        PersistentBattleOwnerSnapshot resolved = CaptureOrFail(owner, 5L);
        Assert.That(resolved.Records[0].TerminalOutcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Victory));
        Assert.That(resolved.Records[0].TerminalOutcome.WinningBattleSideIdValue, Is.EqualTo("side-a"));
        Assert.That(resolved.Records[0].TerminalOutcome.ResolvedAbsoluteDay, Is.EqualTo(5L));
        Assert.That(resolved.TryStage(
            stagedParents.Forces, stagedParents.Conflicts, stagedParents.Wars, stagedParents.Spatial, null,
            out PersistentBattleStore stagedResolved, out PersistentBattleOwnerSnapshotFailure resolvedFailure),
            Is.True, resolvedFailure.Message);
        Assert.That(stagedResolved.TryGet(battleId, out PersistentBattleRecord stagedResolvedRow), Is.True);
        Assert.That(stagedResolvedRow.TerminalOutcome.BattleId, Is.Not.SameAs(outcome.BattleId));
        Assert.That(stagedResolvedRow.TerminalOutcome.BattleId, Is.EqualTo(outcome.BattleId));
        Assert.That(stagedResolvedRow.TerminalOutcome.WinningBattleSideId, Is.Not.SameAs(outcome.WinningBattleSideId));
        Assert.That(stagedResolvedRow.TerminalOutcome.WinningBattleSideId, Is.EqualTo(outcome.WinningBattleSideId));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.PolicyFingerprint,
            Is.EqualTo("policy-fingerprint"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.NumericExecutionProfileKey,
            Is.EqualTo("numeric-profile"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.ProjectionVersion,
            Is.EqualTo("projection-version"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.CausalResolutionFingerprint,
            Is.EqualTo("causal-fingerprint"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.SourceContextFingerprint,
            Is.EqualTo("source-context"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.CapabilityRuleKey,
            Is.EqualTo("capability-rule"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.RandomAuthorityRuleKey,
            Is.EqualTo("random-authority"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D5Resolution.ResolverSettingsIdentity,
            Is.EqualTo("resolver-settings"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D6B2PolicyFingerprint,
            Is.EqualTo("d6b2-policy"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D6B2PlanSchemaVersion,
            Is.EqualTo("d6b2-schema"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D6B2CoverageVersion,
            Is.EqualTo("d6b2-coverage"));
        Assert.That(stagedResolvedRow.TerminalOutcome.Provenance.D6B2PlanFingerprint,
            Is.EqualTo("d6b2-plan"));
        Assert.That(owner.Revision, Is.EqualTo(liveRevisionBeforeStage),
            "Private staging does not replay terminal writes or mutate the live owner.");
        Assert.That(stagedParents.Wars.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void SnapshotIsDetachedFromLaterSourceWritesAndCollectionsAreReadOnly()
    {
        BattleWorld live = CreateWorld();
        PersistentBattleStore owner = CreateBattleStore(live);
        BattleId id = new BattleId("detached-battle");
        Assert.That(owner.TryRegister(CreateBattle(id), out _), Is.True);
        PersistentBattleOwnerSnapshot snapshot = CaptureOrFail(owner, 0L);
        PersistentBattleOwnerSnapshotRecord savedRow = snapshot.Records[0];

        Assert.That(owner.TryRegister(CreateBattle(new BattleId("later-battle")), out _), Is.True);
        Assert.That(snapshot.RecordCount, Is.EqualTo(1));
        Assert.That(snapshot.Records, Has.Count.EqualTo(1));
        Assert.That(snapshot.Records[0], Is.Not.SameAs(owner.Records[0]));
        Assert.That(snapshot.Records[0].BattleIdValue, Is.EqualTo("detached-battle"));
        Assert.That(snapshot.Records[0].Sides[0].DisplayName, Is.EqualTo("Alpha"));
        Assert.Throws<NotSupportedException>(() => ((IList<PersistentBattleOwnerSnapshotRecord>)snapshot.Records)[0] = null);
        Assert.Throws<NotSupportedException>(() => ((IList<PersistentBattleOwnerSideSnapshot>)savedRow.Sides).Clear());
    }

    [Test]
    public void CaptureRejectsWrongProfileStaleVectorDuplicateWitnessAndComposedLocalTopology()
    {
        BattleWorld world = CreateWorld();
        PersistentBattleStore owner = CreateBattleStore(world);
        IReadOnlyList<OwnerSectionCensusSnapshot> correct = CreateOwnerSections(owner);
        DailyCaptureEligibilityToken token = CreateToken(correct, 0L);

        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(owner, token, CreateOwnerSections(owner),
            out _, out PersistentBattleOwnerSnapshotFailure wrongVector), Is.False);
        Assert.That(wrongVector.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.InvalidCaptureContext));

        IReadOnlyList<OwnerSectionCensusSnapshot> duplicate = new[]
        {
            correct[0], correct[0]
        };
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(owner, CreateToken(duplicate, 0L), duplicate,
            out _, out PersistentBattleOwnerSnapshotFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.InvalidOwnerSectionVector));

        IReadOnlyList<OwnerSectionCensusSnapshot> wrongCardinality = new[]
        {
            new OwnerSectionCensusSnapshot(
                PersistentBattleCensusProvider.SectionId,
                PersistentBattleCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                owner,
                1,
                owner.Revision)
        };
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(owner, CreateToken(wrongCardinality, 0L), wrongCardinality,
            out _, out PersistentBattleOwnerSnapshotFailure cardinalityFailure), Is.False);
        Assert.That(cardinalityFailure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.InvalidOwnerSectionVector));

        IReadOnlyList<OwnerSectionCensusSnapshot> wrongSchema = new[]
        {
            new OwnerSectionCensusSnapshot(
                PersistentBattleCensusProvider.SectionId,
                PersistentBattleCensusProvider.SchemaVersion + 1,
                OwnerSectionRole.Required,
                owner,
                owner.Count,
                owner.Revision)
        };
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(owner, CreateToken(wrongSchema, 0L), wrongSchema,
            out _, out PersistentBattleOwnerSnapshotFailure schemaFailure), Is.False);
        Assert.That(schemaFailure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.InvalidOwnerSectionVector));

        IReadOnlyList<OwnerSectionCensusSnapshot> wrongOwner = new[]
        {
            new OwnerSectionCensusSnapshot(
                PersistentBattleCensusProvider.SectionId,
                PersistentBattleCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                new object(),
                owner.Count,
                owner.Revision)
        };
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(owner, CreateToken(wrongOwner, 0L), wrongOwner,
            out _, out PersistentBattleOwnerSnapshotFailure identityFailure), Is.False);
        Assert.That(identityFailure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.InvalidOwnerSectionVector));

        DailyCaptureEligibilityToken incompleteBoundary = CreateToken(correct, 0L, completedCoreSequence: 0L);
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(owner, incompleteBoundary, correct,
            out _, out PersistentBattleOwnerSnapshotFailure profileFailure), Is.False);
        Assert.That(profileFailure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.UnsupportedProfile));

        PersistentBattleStore injectedTopology = new PersistentBattleStore(
            world.Forces, world.Conflicts, world.Wars, world.Spatial,
            new LocalTopologyStore(new RuntimeIdentityRegistry()));
        IReadOnlyList<OwnerSectionCensusSnapshot> injectedSections = CreateOwnerSections(injectedTopology);
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(injectedTopology,
            CreateToken(injectedSections, 0L), injectedSections,
            out _, out PersistentBattleOwnerSnapshotFailure topologyFailure), Is.False);
        Assert.That(topologyFailure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.LocalTopologyComposed));
    }

    [Test]
    public void StageRejectsMalformedRowsReferencesAndUnsupportedDailyTopologyWithoutChangingParents()
    {
        BattleWorld live = CreateWorld();
        PersistentBattleStore owner = CreateBattleStore(live);
        BattleId id = new BattleId("invalid-shape-battle");
        Assert.That(owner.TryRegister(CreateBattle(id), out _), Is.True);
        Assert.That(owner.TryAddParticipantBinding(
            id,
            new BattleParticipantBinding(
                new BattleParticipantBindingId("invalid-shape-binding"), id,
                new BattleSideId("side-a"), live.FirstForceId),
            out PersistentStateFailure bindingFailure), Is.True, bindingFailure.ToString());
        PersistentBattleOwnerSnapshot valid = CaptureOrFail(owner, 0L);
        PersistentBattleOwnerSnapshotRecord row = valid.Records[0];
        BattleWorld target = CreateWorld();

        AssertStageRejected(new PersistentBattleOwnerSnapshot(2, 1, valid.Revision, valid.Records),
            target, PersistentBattleOwnerSnapshotFailureCode.UnsupportedSchema);
        AssertStageRejected(new PersistentBattleOwnerSnapshot(1, 1, -1L, valid.Records),
            target, PersistentBattleOwnerSnapshotFailureCode.InvalidRevision);
        AssertStageRejected(new PersistentBattleOwnerSnapshot(1, 2, valid.Revision, valid.Records),
            target, PersistentBattleOwnerSnapshotFailureCode.InvalidCardinality);
        AssertStageRejected(new PersistentBattleOwnerSnapshot(1, 2, valid.Revision,
                new[] { row, row }), target, PersistentBattleOwnerSnapshotFailureCode.DuplicateBattleIdentity);

        PersistentBattleOwnerSnapshotRecord missingConflict = CopyRow(
            row, conflictIdValue: "missing-conflict", replaceConflict: true);
        AssertStageRejected(SnapshotWith(valid, missingConflict), target,
            PersistentBattleOwnerSnapshotFailureCode.MissingConflict);

        PersistentBattleOwnerSnapshotRecord missingForce = CopyRow(
            row,
            bindings: new[]
            {
                new PersistentBattleOwnerBindingSnapshot(
                    "invalid-shape-binding", row.BattleIdValue, "side-a", "missing-force")
            });
        AssertStageRejected(SnapshotWith(valid, missingForce), target,
            PersistentBattleOwnerSnapshotFailureCode.MissingForce);

        PersistentBattleOwnerSnapshotRecord missingSide = CopyRow(
            row,
            bindings: new[]
            {
                new PersistentBattleOwnerBindingSnapshot(
                    "invalid-shape-binding", row.BattleIdValue, "missing-side", live.FirstForceId.Value)
            });
        AssertStageRejected(SnapshotWith(valid, missingSide), target,
            PersistentBattleOwnerSnapshotFailureCode.InvalidBinding);

        PersistentBattleOwnerSnapshotRecord duplicateBinding = CopyRow(
            row,
            bindings: new[] { row.ParticipantBindings[0], row.ParticipantBindings[0] });
        AssertStageRejected(SnapshotWith(valid, duplicateBinding), target,
            PersistentBattleOwnerSnapshotFailureCode.DuplicateBinding);

        PersistentBattleOwnerSnapshotRecord duplicateSide = CopyRow(
            row,
            sides: new[] { row.Sides[0], row.Sides[0] });
        AssertStageRejected(SnapshotWith(valid, duplicateSide), target,
            PersistentBattleOwnerSnapshotFailureCode.DuplicateSide);

        PersistentBattleOwnerSnapshotRecord badSpatial = CopyRow(
            row,
            location: new PersistentBattleOwnerSpatialReferenceSnapshot(
                SpatialReferenceKind.Hex, "missing-hex", null, null, null, null, null),
            replaceLocation: true);
        AssertStageRejected(SnapshotWith(valid, badSpatial), target,
            PersistentBattleOwnerSnapshotFailureCode.InvalidSpatialReference);

        PersistentBattleOwnerSnapshotRecord subLocation = CopyRow(
            row,
            location: new PersistentBattleOwnerSpatialReferenceSnapshot(
                SpatialReferenceKind.SubLocation, null, null, null,
                LocalTopologyOwnerKind.ExplorableSite, "ruin-site", "inner-room"),
            replaceLocation: true);
        AssertStageRejected(SnapshotWith(valid, subLocation), target,
            PersistentBattleOwnerSnapshotFailureCode.UnsupportedSubLocation);

        PersistentBattleOwnerSnapshotRecord contradictoryParent = CopyRow(
            row,
            conflictIdValue: live.ConflictId.Value,
            warIdValue: live.WarId.Value,
            replaceConflict: true,
            replaceWar: true);
        AssertStageRejected(SnapshotWith(valid, contradictoryParent), CreateContradictoryWarWorld(),
            PersistentBattleOwnerSnapshotFailureCode.ContradictoryParents);

        AssertStageRejected(valid, target, PersistentBattleOwnerSnapshotFailureCode.LocalTopologyComposed,
            new LocalTopologyStore(new RuntimeIdentityRegistry()));
        Assert.That(owner.Count, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(2L));
        Assert.That(target.Forces.Count, Is.EqualTo(2));
        Assert.That(target.Conflicts.Count, Is.EqualTo(1));
        Assert.That(target.Wars.Count, Is.EqualTo(1));
    }

    [Test]
    public void SnapshotPreservesSaturatedRevisionWithoutReplayingWrites()
    {
        BattleWorld world = CreateWorld();
        PersistentBattleOwnerSnapshot snapshot = new PersistentBattleOwnerSnapshot(
            PersistentBattleOwnerSnapshot.CurrentSchemaVersion,
            0,
            long.MaxValue,
            Array.Empty<PersistentBattleOwnerSnapshotRecord>());
        Assert.That(snapshot.TryStage(world.Forces, world.Conflicts, world.Wars, world.Spatial, null,
            out PersistentBattleStore staged, out PersistentBattleOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(staged.TryRegister(CreateBattle(new BattleId("saturated-write")),
            out PersistentStateFailure writeFailure), Is.False);
        Assert.That(writeFailure.Code, Is.EqualTo(PersistentStateFailureCode.RevisionOverflow));
        Assert.That(staged.Count, Is.Zero);
        Assert.That(staged.Revision, Is.EqualTo(long.MaxValue));
    }

    private static void AssertStageRejected(
        PersistentBattleOwnerSnapshot snapshot,
        BattleWorld target,
        PersistentBattleOwnerSnapshotFailureCode expected,
        LocalTopologyStore localTopologyStore = null)
    {
        Assert.That(snapshot.TryStage(
            target.Forces,
            target.Conflicts,
            target.Wars,
            target.Spatial,
            localTopologyStore,
            out PersistentBattleStore staged,
            out PersistentBattleOwnerSnapshotFailure failure), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expected), failure.Message);
    }

    private static PersistentBattleOwnerSnapshot SnapshotWith(
        PersistentBattleOwnerSnapshot source,
        PersistentBattleOwnerSnapshotRecord row) => new PersistentBattleOwnerSnapshot(
            source.SchemaVersion,
            1,
            source.Revision,
            new[] { row });

    private static PersistentBattleOwnerSnapshotRecord CopyRow(
        PersistentBattleOwnerSnapshotRecord source,
        string conflictIdValue = null,
        string warIdValue = null,
        PersistentBattleOwnerSpatialReferenceSnapshot location = null,
        IReadOnlyList<PersistentBattleOwnerBindingSnapshot> bindings = null,
        IReadOnlyList<PersistentBattleOwnerSideSnapshot> sides = null,
        bool replaceConflict = false,
        bool replaceWar = false,
        bool replaceLocation = false) => new PersistentBattleOwnerSnapshotRecord(
            source.BattleIdValue,
            source.CreatedAbsoluteDay,
            source.StartedAbsoluteDay,
            source.LifecycleState,
            replaceConflict ? conflictIdValue : source.ConflictIdValue,
            replaceWar ? warIdValue : source.WarIdValue,
            replaceLocation ? location : source.LocationReference,
            sides ?? source.Sides,
            bindings ?? source.ParticipantBindings,
            source.TerminalOutcome);

    private static PersistentBattleOwnerSnapshot CaptureOrFail(PersistentBattleStore owner, long absoluteDay)
    {
        IReadOnlyList<OwnerSectionCensusSnapshot> sections = CreateOwnerSections(owner);
        DailyCaptureEligibilityToken token = CreateToken(sections, absoluteDay);
        Assert.That(PersistentBattleOwnerSnapshot.TryCapture(
            owner,
            token,
            sections,
            out PersistentBattleOwnerSnapshot snapshot,
            out PersistentBattleOwnerSnapshotFailure failure), Is.True, failure.Message);
        Assert.That(failure.Code, Is.EqualTo(PersistentBattleOwnerSnapshotFailureCode.None));
        return snapshot;
    }

    private static IReadOnlyList<OwnerSectionCensusSnapshot> CreateOwnerSections(PersistentBattleStore owner)
    {
        OwnerSectionCensusWitness witness = new PersistentBattleCensusProvider(owner).GetCurrentCensus();
        return new[]
        {
            new OwnerSectionCensusSnapshot(
                witness.SectionId,
                witness.SchemaVersion,
                OwnerSectionRole.Required,
                witness.OwnerInstanceIdentity,
                witness.Cardinality,
                witness.Revision)
        };
    }

    private static DailyCaptureEligibilityToken CreateToken(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        long absoluteDay,
        SimulationRuntimeAdmissionContext admissionContext = null,
        long completedCoreSequence = 1L)
    {
        return new DailyCaptureEligibilityToken(
            new object(),
            admissionContext ?? SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            new EffectiveSimulationConfiguration(null, null, null, null, null),
            new SimulationCalendar(CalendarDefinition.CreateDefault()),
            SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()),
            absoluteDay,
            completedCoreSequence,
            0L,
            sections);
    }

    private static BattleWorld CreateWorld()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId firstForceId = new ArmedForceId("snapshot-force-a");
        ArmedForceId secondForceId = new ArmedForceId("snapshot-force-b");
        Assert.That(forces.TryRegister(new ArmedForceRecord(firstForceId, "First Force", 0L), out _), Is.True);
        Assert.That(forces.TryRegister(new ArmedForceRecord(secondForceId, "Second Force", 0L), out _), Is.True);

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("snapshot-conflict");
        Assert.That(conflicts.TryRegister(new PersistentConflictRecord(
            conflictId,
            0L,
            sides: new[]
            {
                new ConflictStateSide(conflictId, new ConflictSideId("conflict-side-a")),
                new ConflictStateSide(conflictId, new ConflictSideId("conflict-side-b"))
            }), out PersistentStateFailure conflictFailure), Is.True, conflictFailure.ToString());

        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        WarId warId = new WarId("snapshot-war");
        Assert.That(wars.TryRegister(new PersistentWarRecord(
            warId,
            0L,
            conflictId,
            sides: new[]
            {
                new WarStateSide(warId, new WarSideId("war-side-a")),
                new WarStateSide(warId, new WarSideId("war-side-b"))
            }), out PersistentStateFailure warFailure), Is.True, warFailure.ToString());

        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId hexId = new HexId("snapshot-hex");
        LocationId locationId = new LocationId("snapshot-location");
        Assert.That(spatial.TryRegisterHex(new HexRecord(hexId), out SpatialAuthorityFailure hexFailure), Is.True, hexFailure.ToString());
        Assert.That(spatial.TryRegisterLocation(new LocationRecord(locationId, hexId), out SpatialAuthorityFailure locationFailure), Is.True, locationFailure.ToString());

        return new BattleWorld(persons, forces, conflicts, wars, spatial,
            firstForceId, secondForceId, conflictId, warId, hexId, locationId);
    }

    private static BattleWorld CreateContradictoryWarWorld()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId firstForceId = new ArmedForceId("snapshot-force-a");
        ArmedForceId secondForceId = new ArmedForceId("snapshot-force-b");
        Assert.That(forces.TryRegister(new ArmedForceRecord(firstForceId, "First Force", 0L), out _), Is.True);
        Assert.That(forces.TryRegister(new ArmedForceRecord(secondForceId, "Second Force", 0L), out _), Is.True);

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("snapshot-conflict");
        ConflictId otherConflictId = new ConflictId("snapshot-other-conflict");
        foreach (ConflictId id in new[] { conflictId, otherConflictId })
        {
            Assert.That(conflicts.TryRegister(new PersistentConflictRecord(
                id,
                0L,
                sides: new[]
                {
                    new ConflictStateSide(id, new ConflictSideId("conflict-side-a")),
                    new ConflictStateSide(id, new ConflictSideId("conflict-side-b"))
                }), out PersistentStateFailure conflictFailure), Is.True, conflictFailure.ToString());
        }

        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        WarId warId = new WarId("snapshot-war");
        Assert.That(wars.TryRegister(new PersistentWarRecord(
            warId,
            0L,
            otherConflictId,
            sides: new[]
            {
                new WarStateSide(warId, new WarSideId("war-side-a")),
                new WarStateSide(warId, new WarSideId("war-side-b"))
            }), out PersistentStateFailure warFailure), Is.True, warFailure.ToString());

        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        HexId hexId = new HexId("snapshot-hex");
        LocationId locationId = new LocationId("snapshot-location");
        Assert.That(spatial.TryRegisterHex(new HexRecord(hexId), out SpatialAuthorityFailure hexFailure), Is.True, hexFailure.ToString());
        Assert.That(spatial.TryRegisterLocation(new LocationRecord(locationId, hexId), out SpatialAuthorityFailure locationFailure), Is.True, locationFailure.ToString());
        return new BattleWorld(persons, forces, conflicts, wars, spatial,
            firstForceId, secondForceId, conflictId, warId, hexId, locationId);
    }

    private static PersistentBattleStore CreateBattleStore(BattleWorld world) =>
        new PersistentBattleStore(world.Forces, world.Conflicts, world.Wars, world.Spatial);

    private static PersistentBattleRecord CreateBattle(BattleId id) => new PersistentBattleRecord(
        id,
        0L,
        sides: new[]
        {
            new BattleStateSide(id, new BattleSideId("side-a"), "Alpha"),
            new BattleStateSide(id, new BattleSideId("side-b"), "Beta")
        },
        locationReference: SpatialReference.ForHex(new HexId("snapshot-hex")));

    private static PersistentBattleOutcomeProvenance CreateProvenance() =>
        new PersistentBattleOutcomeProvenance(
            new BattleResolutionProvenance(
                "policy-fingerprint",
                "numeric-profile",
                "projection-version",
                "causal-fingerprint",
                "source-context",
                "capability-rule",
                "random-authority",
                "resolver-settings"),
            "d6b2-policy",
            "d6b2-schema",
            "d6b2-coverage",
            "d6b2-plan");

    private sealed class BattleWorld
    {
        internal PersonStore Persons { get; }
        internal ArmedForceStore Forces { get; }
        internal PersistentConflictStore Conflicts { get; }
        internal PersistentWarStore Wars { get; }
        internal SpatialAuthorityStore Spatial { get; }
        internal ArmedForceId FirstForceId { get; }
        internal ArmedForceId SecondForceId { get; }
        internal ConflictId ConflictId { get; }
        internal WarId WarId { get; }
        internal HexId HexId { get; }
        internal LocationId LocationId { get; }

        internal BattleWorld(
            PersonStore persons,
            ArmedForceStore forces,
            PersistentConflictStore conflicts,
            PersistentWarStore wars,
            SpatialAuthorityStore spatial,
            ArmedForceId firstForceId,
            ArmedForceId secondForceId,
            ConflictId conflictId,
            WarId warId,
            HexId hexId,
            LocationId locationId)
        {
            Persons = persons;
            Forces = forces;
            Conflicts = conflicts;
            Wars = wars;
            Spatial = spatial;
            FirstForceId = firstForceId;
            SecondForceId = secondForceId;
            ConflictId = conflictId;
            WarId = warId;
            HexId = hexId;
            LocationId = locationId;
        }
    }
}
