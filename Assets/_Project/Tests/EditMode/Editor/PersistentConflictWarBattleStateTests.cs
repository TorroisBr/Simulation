using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class PersistentConflictWarBattleStateTests
{
    [Test]
    public void TypedIdentities_AreStableDistinctAndLookupDoesNotReuseIds()
    {
        Assert.That(new ConflictId("same").Equals(new ConflictId("same")), Is.True);
        Assert.That(new WarId("same").Equals(new WarId("same")), Is.True);
        Assert.That(new BattleId("same").Equals(new BattleId("same")), Is.True);
        Assert.That(new ConflictId("same").Equals(new WarId("same")), Is.False);
        Assert.That(new WarId("same").Equals(new BattleId("same")), Is.False);

        ArmedForceStore forces = CreateForces("force-a");
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("conflict-stable");
        Assert.That(conflicts.TryRegister(CreateConflict(conflictId), out _), Is.True);
        Assert.That(conflicts.TryGet(new ConflictId(conflictId.Value), out PersistentConflictRecord found), Is.True);
        Assert.That(found.Id.Value, Is.EqualTo(conflictId.Value));
        Assert.That(conflicts.TryRegister(CreateConflict(conflictId), out PersistentStateFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(PersistentStateFailureCode.DuplicateIdentity));
    }

    [Test]
    public void ConflictStore_RequiresTwoValidSidesAndExplicitArmedForceBindings()
    {
        ArmedForceStore forces = CreateForces("force-a");
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId id = new ConflictId("conflict-validation");

        PersistentConflictRecord oneSide = new PersistentConflictRecord(
            id,
            0L,
            sides: new[] { new ConflictStateSide(id, new ConflictSideId("only")) });
        Assert.That(conflicts.TryRegister(oneSide, out PersistentStateFailure twoSides), Is.False);
        Assert.That(twoSides.Code, Is.EqualTo(PersistentStateFailureCode.RequiresTwoSides));

        PersistentConflictRecord missingForce = CreateConflict(
            id,
            new ConflictParticipantBinding(
                new ConflictParticipantBindingId("binding-missing"),
                id,
                new ConflictSideId("a"),
                new ArmedForceId("force-missing")));
        Assert.That(conflicts.TryRegister(missingForce, out PersistentStateFailure forceFailure), Is.False);
        Assert.That(forceFailure.Code, Is.EqualTo(PersistentStateFailureCode.ForceNotRegistered));

        PersistentConflictRecord valid = CreateConflict(id);
        Assert.That(conflicts.TryRegister(valid, out _), Is.True);
        ConflictParticipantBinding binding = new ConflictParticipantBinding(
            new ConflictParticipantBindingId("binding-a"), id, new ConflictSideId("a"), new ArmedForceId("force-a"));
        Assert.That(conflicts.TryAddParticipantBinding(id, binding, out _), Is.True);
        Assert.That(conflicts.TryAddParticipantBinding(id, binding, out PersistentStateFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(PersistentStateFailureCode.DuplicateBinding));
        Assert.That(conflicts.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void ParticipantBindings_AreDomainOwnedAndDoNotPropagateThroughArmedForceHierarchy()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId parent = new ArmedForceId("force-parent");
        ArmedForceId child = new ArmedForceId("force-child");
        Assert.That(forces.TryRegister(new ArmedForceRecord(parent, "Parent", 0L), out _), Is.True);
        Assert.That(forces.TryRegister(new ArmedForceRecord(child, "Child", 0L, parent), out _), Is.True);
        Assert.That(forces.TryDetach(child, "separate-operation", out _), Is.True);
        Assert.That(forces.TryReattach(child, out _), Is.True);

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("conflict-explicit");
        ConflictParticipantBinding binding = new ConflictParticipantBinding(
            new ConflictParticipantBindingId("binding-parent"), conflictId, new ConflictSideId("a"), parent);
        Assert.That(conflicts.TryRegister(CreateConflict(conflictId, binding), out _), Is.True);
        Assert.That(conflicts.TryGet(conflictId, out PersistentConflictRecord record), Is.True);
        Assert.That(record.ParticipantBindings, Has.Count.EqualTo(1));
        Assert.That(record.ParticipantBindings[0].ArmedForceId, Is.EqualTo(parent));
        Assert.That(record.ParticipantBindings, Has.None.Matches<ConflictParticipantBinding>(value => value.ArmedForceId == child));
    }

    [Test]
    public void WarAndBattle_KeepSeparateSideModelsAndValidateOptionalReferences()
    {
        ArmedForceStore forces = CreateForces("force-a", "force-b");
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        SpatialAuthorityStore spatialAuthority = new SpatialAuthorityStore();
        Assert.That(spatialAuthority.TryRegisterHex(new HexRecord(new HexId("battle-hex")), out _), Is.True);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, spatialAuthority);
        ConflictId conflictId = new ConflictId("conflict-parent");
        WarId warId = new WarId("war-parent");
        Assert.That(conflicts.TryRegister(CreateConflict(conflictId), out _), Is.True);
        Assert.That(wars.TryRegister(CreateWar(warId, conflictId), out _), Is.True);

        BattleId standaloneId = new BattleId("battle-standalone");
        Assert.That(battles.TryRegister(CreateBattle(standaloneId), out _), Is.True);
        BattleId linkedId = new BattleId("battle-linked");
        Assert.That(battles.TryRegister(CreateBattle(linkedId, conflictId, warId), out _), Is.True);
        Assert.That(battles.TryStart(linkedId, 1L, SpatialReference.ForHex(new HexId("battle-hex")), out _), Is.True);
        Assert.That(battles.TryGet(linkedId, out PersistentBattleRecord linked), Is.True);
        Assert.That(linked.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));

        BattleId missingRefId = new BattleId("battle-missing-ref");
        Assert.That(battles.TryRegister(CreateBattle(missingRefId, new ConflictId("missing")), out PersistentStateFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(PersistentStateFailureCode.ConflictNotRegistered));

        WarId contradictoryWarId = new WarId("war-contradictory");
        ConflictId otherConflictId = new ConflictId("conflict-other");
        Assert.That(conflicts.TryRegister(CreateConflict(otherConflictId), out _), Is.True);
        Assert.That(wars.TryRegister(CreateWar(contradictoryWarId, otherConflictId), out _), Is.True);
        Assert.That(battles.TryRegister(CreateBattle(new BattleId("battle-contradictory"), conflictId, contradictoryWarId), out PersistentStateFailure contradiction), Is.False);
        Assert.That(contradiction.Code, Is.EqualTo(PersistentStateFailureCode.ContradictoryReference));

        Assert.That(battles.TryRegister(new PersistentBattleRecord(
            new BattleId("battle-resolved"), 0L,
            lifecycleState: BattleLifecycleState.Resolved,
            sides: BattleSides("battle-resolved")), out PersistentStateFailure deferred), Is.False);
        Assert.That(deferred.Code, Is.EqualTo(PersistentStateFailureCode.BattleResolutionDeferred));
    }

    [Test]
    public void Lifecycle_UsesAtomicRevisionAndKeepsEndedReferencesQueryable()
    {
        ArmedForceStore forces = CreateForces("force-a");
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("conflict-lifecycle");
        Assert.That(conflicts.TryRegister(CreateConflict(conflictId), out _), Is.True);
        long revision = conflicts.Revision;
        Assert.That(conflicts.TryEnd(conflictId, -1L, out PersistentStateFailure invalidDay), Is.False);
        Assert.That(invalidDay.Code, Is.EqualTo(PersistentStateFailureCode.InvalidDay));
        Assert.That(conflicts.Revision, Is.EqualTo(revision));
        Assert.That(conflicts.TryEnd(conflictId, 3L, out _), Is.True);
        Assert.That(conflicts.TryGet(conflictId, out PersistentConflictRecord ended), Is.True);
        Assert.That(ended.IsActive, Is.False);
        Assert.That(conflicts.TryAddParticipantBinding(
            conflictId,
            new ConflictParticipantBinding(new ConflictParticipantBindingId("late"), conflictId, new ConflictSideId("a"), new ArmedForceId("force-a")),
            out PersistentStateFailure endedFailure), Is.False);
        Assert.That(endedFailure.Code, Is.EqualTo(PersistentStateFailureCode.StateEnded));
        Assert.That(conflicts.TryGet(conflictId, out _), Is.True);
    }

    [Test]
    public void SimulationRuntime_ComposesIndependentWorldBoundStoresWithoutAdvanceDayProcessing()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("force-runtime");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "Runtime Force", 0L), out _), Is.True);
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        ConflictId conflictId = new ConflictId("conflict-runtime");
        Assert.That(conflicts.TryRegister(CreateConflict(conflictId), out _), Is.True);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        WarId warId = new WarId("war-runtime");
        Assert.That(wars.TryRegister(CreateWar(warId, conflictId), out _), Is.True);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars);
        Assert.That(battles.TryRegister(CreateBattle(new BattleId("battle-runtime"), conflictId, warId), out _), Is.True);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L), null, null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: forces,
            conflictStore: conflicts,
            warStore: wars,
            battleStore: battles);
        long conflictRevision = runtime.ConflictStore.Revision;
        long warRevision = runtime.WarStore.Revision;
        long battleRevision = runtime.BattleStore.Revision;
        Assert.That(runtime.ConflictStore, Is.Not.SameAs(conflicts));
        Assert.That(runtime.WarStore.ConflictStore, Is.SameAs(runtime.ConflictStore));
        Assert.That(runtime.BattleStore.WarStore, Is.SameAs(runtime.WarStore));

        runtime.AdvanceDay();

        Assert.That(runtime.ConflictStore.Revision, Is.EqualTo(conflictRevision));
        Assert.That(runtime.WarStore.Revision, Is.EqualTo(warRevision));
        Assert.That(runtime.BattleStore.Revision, Is.EqualTo(battleRevision));
        Assert.That(runtime.BattleStore.TryGet(new BattleId("battle-runtime"), out _), Is.True);
    }

    [Test]
    public void SnapshotCanonicalAndDiff_AreIndependentOfPersistentStoreInsertionOrder()
    {
        WorldStateSnapshot first = BuildSnapshot(false);
        WorldStateSnapshot second = BuildSnapshot(true);

        Assert.That(WorldStateCanonicalWriter.Write(first), Is.EqualTo(WorldStateCanonicalWriter.Write(second)));
        Assert.That(WorldStateDiagnostics.Compare(first, second).IsEmpty, Is.True);
        Assert.That(WorldStateDiagnostics.Validate(first).IsValid, Is.True);
        Assert.That(WorldStateSnapshotFormatter.Format(first), Does.Contain("CONFLICT conflict-a"));
    }

    [Test]
    public void Diagnostics_ReportsPersistentCrossReferenceAndLifecycleViolations()
    {
        WorldStateSnapshot snapshot = new WorldStateSnapshot(
            2L,
            armedForces: new[]
            {
                new WorldStateArmedForceSnapshot("force-known", "Known", 0L, ArmedForceLifecycleState.Active, null, null, false, null, null)
            },
            armedForceRevision: 1L,
            conflicts: new[]
            {
                new WorldStateConflictSnapshot("conflict-known", 0L, ConflictLifecycleState.Active, null)
            },
            conflictSides: new[]
            {
                new WorldStateConflictSideSnapshot("conflict-known", "side-a", "A")
            },
            conflictParticipantBindings: new[]
            {
                new WorldStateConflictParticipantBindingSnapshot("conflict-known", "binding", "side-missing", "force-missing")
            },
            conflictRevision: 1L,
            wars: new[]
            {
                new WorldStateWarSnapshot("war-known", 0L, WarLifecycleState.Active, null, "conflict-other")
            },
            warRevision: 1L,
            battles: new[]
            {
                new WorldStateBattleSnapshot("battle-known", 0L, null, BattleLifecycleState.Active, "conflict-known", "war-known")
            },
            battleRevision: 1L);

        WorldStateInvariantReport report = WorldStateDiagnostics.Validate(snapshot);
        Assert.That(report.HasErrors, Is.True);
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "ConflictBindingSideMissing"));
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "ConflictBindingForceMissing"));
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "WarConflictMissing"));
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "BattleStartDayMissing"));
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

    private static PersistentConflictRecord CreateConflict(ConflictId id, params ConflictParticipantBinding[] bindings)
    {
        return new PersistentConflictRecord(
            id,
            0L,
            sides: new[]
            {
                new ConflictStateSide(id, new ConflictSideId("a"), "A"),
                new ConflictStateSide(id, new ConflictSideId("b"), "B")
            },
            participantBindings: bindings);
    }

    private static PersistentWarRecord CreateWar(WarId id, ConflictId conflictId = null)
    {
        return new PersistentWarRecord(
            id,
            0L,
            conflictId: conflictId,
            sides: new[]
            {
                new WarStateSide(id, new WarSideId("a"), "A"),
                new WarStateSide(id, new WarSideId("b"), "B")
            });
    }

    private static PersistentBattleRecord CreateBattle(BattleId id, ConflictId conflictId = null, WarId warId = null)
    {
        return new PersistentBattleRecord(
            id,
            0L,
            conflictId: conflictId,
            warId: warId,
            sides: BattleSides(id.Value));
    }

    private static BattleStateSide[] BattleSides(string id)
    {
        BattleId battleId = new BattleId(id);
        return new[]
        {
            new BattleStateSide(battleId, new BattleSideId("a"), "A"),
            new BattleStateSide(battleId, new BattleSideId("b"), "B")
        };
    }

    private static WorldStateSnapshot BuildSnapshot(bool reverse)
    {
        ArmedForceStore forces = CreateForces("force-a", "force-b");
        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars);
        PersistentConflictRecord conflictA = CreateConflict(new ConflictId("conflict-a"));
        PersistentConflictRecord conflictB = CreateConflict(new ConflictId("conflict-b"));
        if (reverse)
        {
            Assert.That(conflicts.TryRegister(conflictB, out _), Is.True);
            Assert.That(conflicts.TryRegister(conflictA, out _), Is.True);
        }
        else
        {
            Assert.That(conflicts.TryRegister(conflictA, out _), Is.True);
            Assert.That(conflicts.TryRegister(conflictB, out _), Is.True);
        }

        PersistentWarRecord warA = CreateWar(new WarId("war-a"), new ConflictId("conflict-a"));
        PersistentWarRecord warB = CreateWar(new WarId("war-b"));
        if (reverse)
        {
            Assert.That(wars.TryRegister(warB, out _), Is.True);
            Assert.That(wars.TryRegister(warA, out _), Is.True);
        }
        else
        {
            Assert.That(wars.TryRegister(warA, out _), Is.True);
            Assert.That(wars.TryRegister(warB, out _), Is.True);
        }

        PersistentBattleRecord battleA = CreateBattle(new BattleId("battle-a"), new ConflictId("conflict-a"), new WarId("war-a"));
        PersistentBattleRecord battleB = CreateBattle(new BattleId("battle-b"));
        if (reverse)
        {
            Assert.That(battles.TryRegister(battleB, out _), Is.True);
            Assert.That(battles.TryRegister(battleA, out _), Is.True);
        }
        else
        {
            Assert.That(battles.TryRegister(battleA, out _), Is.True);
            Assert.That(battles.TryRegister(battleB, out _), Is.True);
        }

        return WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            armedForceStore: forces,
            conflictStore: conflicts,
            warStore: wars,
            battleStore: battles));
    }
}
