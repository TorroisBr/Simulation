using System;
using NUnit.Framework;

public sealed class BattleExecutionContextTests
{
    [Test]
    public void ActiveBattle_WithCurrentPositionsAndDirectContingents_CreatesImmutableContext()
    {
        BattleExecutionFixture fixture = CreateFixture();

        long battleRevision = fixture.Battles.Revision;
        Assert.That(fixture.Builder.TryCreate(
            fixture.BattleId,
            3L,
            null,
            out BattleExecutionContext context,
            out BattleExecutionFailure failure), Is.True, failure.ToString());

        Assert.That(context.BattleId, Is.EqualTo(fixture.BattleId));
        Assert.That(context.ExecutionAbsoluteDay, Is.EqualTo(3L));
        Assert.That(context.Sides, Has.Count.EqualTo(2));
        Assert.That(context.Sides[0].Forces, Has.Count.EqualTo(1));
        Assert.That(context.Sides[0].Forces[0].DirectContingents, Has.Count.EqualTo(1));
        Assert.That(context.Sides[0].Forces[0].DirectContingents[0].Amount, Is.EqualTo(4L));
        Assert.That(context.Sides[0].HasUsableDirectCombatElements, Is.True);
        Assert.That(fixture.Battles.Revision, Is.EqualTo(battleRevision));
        Assert.That(fixture.Persons.Persons, Has.Count.EqualTo(0));
    }

    [Test]
    public void Eligibility_RejectsPendingMissingPositionWrongPlaceAndInvalidDay()
    {
        BattleExecutionFixture fixture = CreateFixture(startBattle: false);
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 0L, null, out _, out BattleExecutionFailure pending), Is.False);
        Assert.That(pending.Code, Is.EqualTo(BattleExecutionFailureCode.BattleNotActive));

        fixture = CreateFixture(setPositions: false);
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, null, out _, out BattleExecutionFailure missingPosition), Is.False);
        Assert.That(missingPosition.Code, Is.EqualTo(BattleExecutionFailureCode.ParticipantPositionMissing));

        fixture = CreateFixture(positionHex: "hex-other");
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, null, out _, out BattleExecutionFailure wrongPlace), Is.False);
        Assert.That(wrongPlace.Code, Is.EqualTo(BattleExecutionFailureCode.ParticipantSpatiallyIncompatible));

        fixture = CreateFixture();
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, -1L, null, out _, out BattleExecutionFailure invalidDay), Is.False);
        Assert.That(invalidDay.Code, Is.EqualTo(BattleExecutionFailureCode.InvalidExecutionDay));
    }

    [Test]
    public void SideComposition_UsesOnlyExplicitDirectContingents()
    {
        BattleExecutionFixture fixture = CreateFixture(commandOnlyParent: true);
        Assert.That(fixture.Builder.TryCreate(
            fixture.BattleId,
            1L,
            null,
            out BattleExecutionContext context,
            out BattleExecutionFailure failure), Is.True, failure.ToString());

        Assert.That(context.Sides[0].Forces, Has.Count.EqualTo(2));
        Assert.That(context.Sides[0].Forces[0].ForceId, Is.EqualTo(new ArmedForceId("force-a")));
        Assert.That(context.Sides[0].Forces[0].DirectContingents, Has.Count.EqualTo(1));
        Assert.That(context.Sides[0].Forces[1].ForceId, Is.EqualTo(new ArmedForceId("force-parent")));
        Assert.That(context.Sides[0].Forces[1].DirectContingents, Has.Count.EqualTo(0));
        Assert.That(context.Sides[0].HasUsableDirectCombatElements, Is.True);

        BattleExecutionFixture noCombat = CreateFixture(commandOnlyParent: true, childHasContingent: false);
        Assert.That(noCombat.Builder.TryCreate(noCombat.BattleId, 1L, null, out _, out BattleExecutionFailure failureNoCombat), Is.False);
        Assert.That(failureNoCombat.Code, Is.EqualTo(BattleExecutionFailureCode.SideHasNoCombatElements));
    }

    [Test]
    public void DuplicateForceBinding_IsRejectedWithoutMutatingBattle()
    {
        BattleExecutionFixture fixture = CreateFixture(duplicateForceBinding: true);
        long revision = fixture.Battles.Revision;

        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, null, out _, out BattleExecutionFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(BattleExecutionFailureCode.DuplicateParticipantForce));
        Assert.That(fixture.Battles.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void CommandPlan_ReferencesPersonIdWithoutNpcRuntimeAndDoesNotInferCommander()
    {
        BattleExecutionFixture fixture = CreateFixture();
        PersonId commanderId = new PersonId("person-commander");
        Assert.That(fixture.Persons.TryRegister(new PersonRuntime(commanderId), out _), Is.True);

        BattleExecutionPlan plan = new BattleExecutionPlan(new[]
        {
            new BattleExecutionSideCommander(new BattleSideId("side-a"), commanderId)
        });
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, plan, out BattleExecutionContext context, out BattleExecutionFailure failure), Is.True, failure.ToString());
        Assert.That(context.Sides[0].SideCommanderPersonId, Is.EqualTo(commanderId));
        Assert.That(fixture.Persons.Persons[0].IsMaterialized, Is.False);

        BattleExecutionPlan invalidPlan = new BattleExecutionPlan(new[]
        {
            new BattleExecutionSideCommander(new BattleSideId("side-a"), new PersonId("missing"))
        });
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, invalidPlan, out _, out BattleExecutionFailure invalid), Is.False);
        Assert.That(invalid.Code, Is.EqualTo(BattleExecutionFailureCode.InvalidSideCommander));
    }

    [Test]
    public void SnapshotAndStaleness_TrackOnlyRelevantPositionAndComposition()
    {
        BattleExecutionFixture fixture = CreateFixture();
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 2L, null, out BattleExecutionContext context, out _), Is.True);
        string capturedKey = context.StableKey;

        Assert.That(fixture.Forces.TryReplaceContingent(new ContingentRecord(
            new ContingentId("contingent-a"),
            new ArmedForceId("force-a"),
            9L,
            new ContingentOriginReference("origin", "a"),
            "service-a"), out _), Is.True);
        Assert.That(context.Sides[0].Forces[0].DirectContingents[0].Amount, Is.EqualTo(4L));

        Assert.That(fixture.Spatial.TrySetPosition(
            new ArmedForceId("force-a"),
            SpatialReference.ForHex(new HexId("hex-other")),
            out _), Is.True);
        Assert.That(fixture.Builder.TryValidateCurrent(context, 2L, out BattleExecutionValidationReport report, out BattleExecutionFailure failure), Is.False);
        Assert.That(report.IsStale, Is.True);
        Assert.That(report.Reasons, Does.Contain(BattleExecutionStalenessReason.ForcePositionChanged));
        Assert.That(report.Reasons, Does.Contain(BattleExecutionStalenessReason.ForceNoLongerSpatiallyCompatible));
        Assert.That(report.Reasons, Does.Contain(BattleExecutionStalenessReason.DirectContingentCompositionChanged));
        Assert.That(failure.Code, Is.EqualTo(BattleExecutionFailureCode.ContextStale));
        Assert.That(context.StableKey, Is.EqualTo(capturedKey));
    }

    [Test]
    public void Staleness_IgnoresUnrelatedForceHierarchyLegacyLocationAndCommanderChanges()
    {
        BattleExecutionFixture fixture = CreateFixture(includeUnrelatedForce: true);
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 1L, null, out BattleExecutionContext context, out _), Is.True);

        PersonId commander = new PersonId("person-unrelated");
        Assert.That(fixture.Persons.TryRegister(new PersonRuntime(commander), out _), Is.True);
        Assert.That(fixture.Forces.TryAssignCommander(new ArmedForceId("force-a"), commander, out _), Is.True);
        Assert.That(fixture.Forces.TrySetOperationalLocation(new ArmedForceId("force-a"), "legacy-only", out _), Is.True);
        Assert.That(fixture.Forces.TryReparent(new ArmedForceId("force-a"), new ArmedForceId("force-unrelated"), out _), Is.True);

        Assert.That(fixture.Builder.TryValidateCurrent(context, 1L, out BattleExecutionValidationReport report, out BattleExecutionFailure failure), Is.True, failure.ToString());
        Assert.That(report.IsCurrent, Is.True);
    }

    [Test]
    public void TerminatedParticipant_MakesContextCreationFailAndDoesNotDeleteBattle()
    {
        BattleExecutionFixture fixture = CreateFixture();
        long revision = fixture.Battles.Revision;
        Assert.That(fixture.Forces.TryTerminate(new ArmedForceId("force-a"), 4L, out _), Is.True);
        Assert.That(fixture.Builder.TryCreate(fixture.BattleId, 4L, null, out _, out BattleExecutionFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(BattleExecutionFailureCode.ParticipantForceTerminated));
        Assert.That(fixture.Battles.Revision, Is.EqualTo(revision));
        Assert.That(fixture.Battles.TryGet(fixture.BattleId, out PersistentBattleRecord battle), Is.True);
        Assert.That(battle.LifecycleState, Is.EqualTo(BattleLifecycleState.Active));
    }

    [Test]
    public void EquivalentInsertionOrders_ProduceSameContextFingerprint()
    {
        BattleExecutionFixture first = CreateFixture(reverseInsertion: false);
        BattleExecutionFixture second = CreateFixture(reverseInsertion: true);
        Assert.That(first.Builder.TryCreate(first.BattleId, 1L, null, out BattleExecutionContext firstContext, out _), Is.True);
        Assert.That(second.Builder.TryCreate(second.BattleId, 1L, null, out BattleExecutionContext secondContext, out _), Is.True);
        Assert.That(firstContext.StableKey, Is.EqualTo(secondContext.StableKey));
        Assert.That(firstContext.Sides[0].Forces[0].DirectContingents[0].StableKey, Is.EqualTo("contingent-a"));
        Assert.That(secondContext.Sides[0].Forces[0].DirectContingents[0].StableKey, Is.EqualTo("contingent-a"));
    }

    [Test]
    public void SimulationRuntime_ExposesBuilderAgainstItsClonedWorldStores()
    {
        BattleExecutionFixture fixture = CreateFixture();
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(1L),
            null,
            null,
            economyEnabled: false,
            personStore: fixture.Persons,
            armedForceStore: fixture.Forces,
            battleStore: fixture.Battles,
            spatialAuthorityStore: fixture.Spatial.SpatialAuthorityStore,
            armedForceSpatialStateStore: fixture.Spatial);

        Assert.That(runtime.BattleExecutionContextBuilder.TryCreate(
            fixture.BattleId,
            1L,
            null,
            out BattleExecutionContext context,
            out BattleExecutionFailure failure), Is.True, failure.ToString());
        Assert.That(runtime.BattleStore, Is.Not.SameAs(fixture.Battles));
        Assert.That(context.BattleId, Is.EqualTo(fixture.BattleId));
    }

    private static BattleExecutionFixture CreateFixture(
        bool startBattle = true,
        bool setPositions = true,
        string positionHex = "hex-main",
        bool commandOnlyParent = false,
        bool childHasContingent = true,
        bool duplicateForceBinding = false,
        bool includeUnrelatedForce = false,
        bool reverseInsertion = false)
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId parentId = new ArmedForceId("force-parent");
        ArmedForceId forceA = new ArmedForceId("force-a");
        ArmedForceId forceB = new ArmedForceId("force-b");
        if (commandOnlyParent)
        {
            Assert.That(forces.TryRegister(new ArmedForceRecord(parentId, "Parent", 0L), out _), Is.True);
            Assert.That(forces.TryRegister(new ArmedForceRecord(forceA, "Child", 0L, parentId), out _), Is.True);
        }
        else
        {
            Assert.That(forces.TryRegister(new ArmedForceRecord(forceA, "A", 0L), out _), Is.True);
        }

        Assert.That(forces.TryRegister(new ArmedForceRecord(forceB, "B", 0L), out _), Is.True);
        if (includeUnrelatedForce)
        {
            Assert.That(forces.TryRegister(new ArmedForceRecord(new ArmedForceId("force-unrelated"), "Unrelated", 0L), out _), Is.True);
        }

        if (!commandOnlyParent)
        {
            Assert.That(forces.TryRegisterContingent(new ContingentRecord(
                new ContingentId("contingent-a"),
                forceA,
                4L,
                new ContingentOriginReference("origin", "a"),
                "service-a"), out _), Is.True);
        }

        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            new ContingentId("contingent-b"),
            forceB,
            2L,
            new ContingentOriginReference("origin", "b"),
            "service-b"), out _), Is.True);
        if (commandOnlyParent)
        {
            Assert.That(forces.TryRegisterContingent(new ContingentRecord(
                new ContingentId("contingent-child"),
                forceA,
                childHasContingent ? 4L : 0L,
                new ContingentOriginReference("origin", "child"),
                "service-child"), out _), Is.True);
        }

        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-main")), out _), Is.True);
        Assert.That(authority.TryRegisterHex(new HexRecord(new HexId("hex-other")), out _), Is.True);
        ArmedForceSpatialStateStore spatial = new ArmedForceSpatialStateStore(forces, authority);
        if (setPositions)
        {
            SpatialReference position = SpatialReference.ForHex(new HexId(positionHex));
            foreach (ArmedForceRecord force in forces.Forces)
            {
                if (force.IsActive && (force.Id == forceA || force.Id == forceB || force.Id == parentId))
                {
                    Assert.That(spatial.TrySetPosition(force.Id, position, out _), Is.True);
                }
            }
        }

        PersistentConflictStore conflicts = new PersistentConflictStore(forces);
        PersistentWarStore wars = new PersistentWarStore(forces, conflicts);
        PersistentBattleStore battles = new PersistentBattleStore(forces, conflicts, wars, authority);
        BattleId battleId = new BattleId("battle-execution");
        BattleParticipantBinding[] bindings;
        if (commandOnlyParent)
        {
            bindings = new[]
            {
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-parent"), battleId, new BattleSideId("side-a"), parentId),
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-child"), battleId, new BattleSideId("side-a"), forceA),
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-b"), battleId, new BattleSideId("side-b"), forceB)
            };
        }
        else if (duplicateForceBinding)
        {
            bindings = new[]
            {
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-a"), battleId, new BattleSideId("side-a"), forceA),
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-a2"), battleId, new BattleSideId("side-b"), forceA),
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-b"), battleId, new BattleSideId("side-b"), forceB)
            };
        }
        else
        {
            bindings = new[]
            {
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-a"), battleId, new BattleSideId("side-a"), forceA),
                new BattleParticipantBinding(new BattleParticipantBindingId("binding-b"), battleId, new BattleSideId("side-b"), forceB)
            };
        }

        PersistentBattleRecord record = new PersistentBattleRecord(
            battleId,
            0L,
            lifecycleState: BattleLifecycleState.Pending,
            sides: new[]
            {
                new BattleStateSide(battleId, new BattleSideId("side-b"), "B"),
                new BattleStateSide(battleId, new BattleSideId("side-a"), "A")
            },
            participantBindings: reverseInsertion ? Reverse(bindings) : bindings,
            locationReference: SpatialReference.ForHex(new HexId("hex-main")));
        Assert.That(battles.TryRegister(record, out _), Is.True);
        if (startBattle) Assert.That(battles.TryStart(battleId, 1L, out _), Is.True);

        return new BattleExecutionFixture(
            persons,
            forces,
            spatial,
            battles,
            new BattleExecutionContextBuilder(battles, forces, spatial, authority),
            battleId);
    }

    private static T[] Reverse<T>(T[] values)
    {
        T[] result = new T[values.Length];
        for (int index = 0; index < values.Length; index++) result[index] = values[values.Length - 1 - index];
        return result;
    }

    private sealed class BattleExecutionFixture
    {
        public BattleExecutionFixture(
            PersonStore persons,
            ArmedForceStore forces,
            ArmedForceSpatialStateStore spatial,
            PersistentBattleStore battles,
            BattleExecutionContextBuilder builder,
            BattleId battleId)
        {
            Persons = persons;
            Forces = forces;
            Spatial = spatial;
            Battles = battles;
            Builder = builder;
            BattleId = battleId;
        }

        public PersonStore Persons { get; }
        public ArmedForceStore Forces { get; }
        public ArmedForceSpatialStateStore Spatial { get; }
        public PersistentBattleStore Battles { get; }
        public BattleExecutionContextBuilder Builder { get; }
        public BattleId BattleId { get; }
    }
}
