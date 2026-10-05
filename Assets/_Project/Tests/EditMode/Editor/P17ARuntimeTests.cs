using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class P17ARuntimeTests
{
    [Test]
    public void WithdrawalCrossingProvesGoalWithoutEndingWarAndLaterConcessionEndsIt()
    {
        Fixture fixture = new Fixture();
        SimulationRuntime runtime = fixture.CreateRuntime();
        Assert.That(runtime.TryCaptureP17AWarObservation(fixture.WarId,
            out P17AWarObservation before, out PersistentStateFailure beforeFailure), Is.True, beforeFailure.ToString());
        Assert.That(before.GoalStatus, Is.EqualTo(P17AWithdrawalGoalStatus.Pending));
        Assert.That(before.LifecycleState, Is.EqualTo(WarLifecycleState.Active));
        Assert.That(before.P16PositionStableKey, Is.EqualTo("hex:p17-source"));

        long warRevisionBeforeMove = runtime.WarStore.Revision;
        Assert.That(runtime.TryExecuteP17AWithdrawalCrossing(
            fixture.WarId,
            fixture.SelectedForce,
            fixture.SourceHex,
            fixture.DestinationHex,
            fixture.Option,
            "p17a.withdrawal-crossing",
            WorldCommandOrigin.Scenario,
            fixture.Capability,
            out P16ACrossingReceipt receipt,
            out ArmedForceSpatialFailure crossingFailure), Is.True, crossingFailure.ToString());

        Assert.That(receipt.AcceptedOrigin, Is.EqualTo(WorldCommandOrigin.Scenario));
        Assert.That(receipt.AuthorityId, Is.EqualTo(Fixture.AuthorityId));
        Assert.That(runtime.WarStore.Revision, Is.EqualTo(warRevisionBeforeMove));
        Assert.That(runtime.TryCaptureP17AWarObservation(fixture.WarId,
            out P17AWarObservation achieved, out PersistentStateFailure achievedFailure), Is.True, achievedFailure.ToString());
        Assert.That(achieved.GoalStatus, Is.EqualTo(P17AWithdrawalGoalStatus.Achieved));
        Assert.That(achieved.GoalEvidence, Is.SameAs(receipt));
        Assert.That(achieved.LifecycleState, Is.EqualTo(WarLifecycleState.Active));
        Assert.That(achieved.P16PositionStableKey, Is.EqualTo("hex:p17-destination"));
        Assert.That(achieved.P16CurrentSupply, Is.EqualTo(3m));
        PersistentWarStoreSnapshot crossedWarState = runtime.WarStore.CaptureState();
        Assert.That(runtime.WarStore.TryValidateSnapshotForHydration(crossedWarState, out string crossedDiagnostic),
            Is.True, crossedDiagnostic);

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryConcedeP17AWar(
            fixture.WarId,
            fixture.TargetParticipantId,
            "p17a.concession",
            WorldCommandOrigin.GM,
            fixture.Capability,
            out PersistentStateFailure concessionFailure), Is.True, concessionFailure.ToString());
        Assert.That(runtime.TryCaptureP17AWarObservation(fixture.WarId,
            out P17AWarObservation ended, out PersistentStateFailure endedFailure), Is.True, endedFailure.ToString());
        Assert.That(ended.GoalStatus, Is.EqualTo(P17AWithdrawalGoalStatus.Achieved));
        Assert.That(ended.LifecycleState, Is.EqualTo(WarLifecycleState.Ended));
        Assert.That(ended.EndedAbsoluteDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(ended.TerminalConcession.ConcedingParticipantId, Is.EqualTo(fixture.TargetParticipantId));
        Assert.That(ended.TerminalConcession.AcceptedOrigin, Is.EqualTo(WorldCommandOrigin.GM));
        Assert.That(ended.TerminalConcession.AuthorityId, Is.EqualTo(Fixture.AuthorityId));
        Assert.That(runtime.WarStore.ValidateInvariants().IsValid, Is.True, endedFailure.ToString());
        PersistentWarStoreSnapshot endedWarState = runtime.WarStore.CaptureState();
        Assert.That(runtime.WarStore.TryValidateSnapshotForHydration(endedWarState, out string endedDiagnostic),
            Is.True, endedDiagnostic);
    }

    [Test]
    public void ExplicitConcessionMayEndWarWhileWithdrawalGoalRemainsPending()
    {
        Fixture fixture = new Fixture();
        SimulationRuntime runtime = fixture.CreateRuntime();
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True, advanceFailure.ToString());

        Assert.That(runtime.TryConcedeP17AWar(
            fixture.WarId,
            fixture.TargetParticipantId,
            "p17a.concession-with-pending-goal",
            WorldCommandOrigin.Scenario,
            fixture.Capability,
            out PersistentStateFailure concessionFailure), Is.True, concessionFailure.ToString());
        Assert.That(runtime.TryCaptureP17AWarObservation(fixture.WarId,
            out P17AWarObservation observation, out PersistentStateFailure observationFailure), Is.True, observationFailure.ToString());
        Assert.That(observation.GoalStatus, Is.EqualTo(P17AWithdrawalGoalStatus.Pending));
        Assert.That(observation.LifecycleState, Is.EqualTo(WarLifecycleState.Ended));
        Assert.That(observation.GoalEvidence, Is.Null);
    }

    [Test]
    public void RuntimeRejectsWrongAuthorityParticipantRawEndAndInvalidCrossingWithoutMutation()
    {
        Fixture fixture = new Fixture();
        SimulationRuntime runtime = fixture.CreateRuntime();
        P16AStateSnapshot p16Before = runtime.ArmedForceSpatialStateStore.CaptureP16AState();
        long p16RevisionBefore = runtime.ArmedForceSpatialStateStore.Revision;
        long warRevisionBefore = runtime.WarStore.Revision;

        Assert.That(runtime.TryExecuteP17AWithdrawalCrossing(
            fixture.WarId, fixture.SelectedForce, fixture.OtherSourceHex, fixture.DestinationHex,
            fixture.Option, "p17a.wrong-source", WorldCommandOrigin.Scenario, fixture.Capability,
            out _, out ArmedForceSpatialFailure wrongSourceFailure), Is.False);
        Assert.That(wrongSourceFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.InvalidMovementInput));

        Assert.That(runtime.TryExecuteP17AWithdrawalCrossing(
            fixture.WarId, fixture.SelectedForce, fixture.SourceHex, fixture.DestinationHex,
            fixture.Option, "p17a.wrong-origin", WorldCommandOrigin.LocalPlayer, fixture.Capability,
            out _, out ArmedForceSpatialFailure originFailure), Is.False);
        Assert.That(originFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.InvalidMovementInput));

        Assert.That(runtime.TryExecuteP16AMilitaryCrossing(
            fixture.SelectedForce, fixture.SourceHex, fixture.DestinationHex, fixture.Option,
            "p17a.p16-bypass", out _, out ArmedForceSpatialFailure p16BypassFailure), Is.False);
        Assert.That(p16BypassFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.MovementProfileNotConfigured));

        P17AScenarioAuthorityCapability sameIdDifferentInstance = new P17AScenarioAuthorityCapability(Fixture.AuthorityId);
        Assert.That(runtime.TryExecuteP17AWithdrawalCrossing(
            fixture.WarId, fixture.SelectedForce, fixture.SourceHex, fixture.DestinationHex,
            fixture.Option, "p17a.unbound-capability", WorldCommandOrigin.Scenario, sameIdDifferentInstance,
            out _, out ArmedForceSpatialFailure capabilityFailure), Is.False);
        Assert.That(capabilityFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.InvalidMovementInput));

        Assert.That(runtime.WarStore.TryEnd(fixture.WarId, runtime.CurrentDay, out PersistentStateFailure rawEndFailure), Is.False);
        Assert.That(rawEndFailure.Code, Is.EqualTo(PersistentStateFailureCode.InvalidRecord));
        Assert.That(runtime.WarStore.Revision, Is.EqualTo(warRevisionBefore));
        P16AStateSnapshot p16After = runtime.ArmedForceSpatialStateStore.CaptureP16AState();
        Assert.That(p16After.PositionStableKey, Is.EqualTo(p16Before.PositionStableKey));
        Assert.That(p16After.CurrentQuantity, Is.EqualTo(p16Before.CurrentQuantity));
        Assert.That(p16After.Receipt, Is.Null);
        Assert.That(runtime.ArmedForceSpatialStateStore.Revision, Is.EqualTo(p16RevisionBefore));

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryConcedeP17AWar(
            fixture.WarId,
            fixture.OwnerParticipantId,
            "p17a.wrong-participant",
            WorldCommandOrigin.Scenario,
            fixture.Capability,
            out PersistentStateFailure participantFailure), Is.False);
        Assert.That(participantFailure.Code, Is.EqualTo(PersistentStateFailureCode.InvalidRecord));
        Assert.That(runtime.WarStore.Revision, Is.EqualTo(warRevisionBefore));
    }

    [Test]
    public void P17ConfigurationRejectsDuplicateParticipantsFactionsSidesAndMissingBinding()
    {
        Fixture fixture = new Fixture();
        AssertInvalidConfiguration(fixture, "duplicate-participant", warId => new P17AWarStrategicSection(
            Fixture.AuthorityId,
            new[]
            {
                new WarStrategicParticipant(fixture.OwnerParticipantId, warId, fixture.OwnerFactionId, fixture.OwnerSideId),
                new WarStrategicParticipant(fixture.OwnerParticipantId, warId, fixture.TargetFactionId, fixture.TargetSideId)
            },
            fixture.CreateDemand(warId, fixture.OwnerParticipantId, fixture.TargetParticipantId,
                fixture.TargetBindingId)));
        AssertInvalidConfiguration(fixture, "duplicate-faction", warId => new P17AWarStrategicSection(
            Fixture.AuthorityId,
            new[]
            {
                new WarStrategicParticipant(fixture.OwnerParticipantId, warId, fixture.OwnerFactionId, fixture.OwnerSideId),
                new WarStrategicParticipant(fixture.TargetParticipantId, warId, fixture.OwnerFactionId, fixture.TargetSideId)
            },
            fixture.CreateDemand(warId, fixture.OwnerParticipantId, fixture.TargetParticipantId,
                fixture.TargetBindingId)));
        AssertInvalidConfiguration(fixture, "duplicate-side", warId => new P17AWarStrategicSection(
            Fixture.AuthorityId,
            new[]
            {
                new WarStrategicParticipant(fixture.OwnerParticipantId, warId, fixture.OwnerFactionId, fixture.OwnerSideId),
                new WarStrategicParticipant(fixture.TargetParticipantId, warId, fixture.TargetFactionId, fixture.OwnerSideId)
            },
            fixture.CreateDemand(warId, fixture.OwnerParticipantId, fixture.TargetParticipantId,
                fixture.TargetBindingId)));
        AssertInvalidConfiguration(fixture, "missing-binding", warId => new P17AWarStrategicSection(
            Fixture.AuthorityId,
            new[]
            {
                new WarStrategicParticipant(fixture.OwnerParticipantId, warId, fixture.OwnerFactionId, fixture.OwnerSideId),
                new WarStrategicParticipant(fixture.TargetParticipantId, warId, fixture.TargetFactionId, fixture.TargetSideId)
            },
            fixture.CreateDemand(warId, fixture.OwnerParticipantId, fixture.TargetParticipantId,
                new WarParticipantBindingId("p17-missing-binding"))));
    }

    [Test]
    public void P17RuntimeFaultLatchRejectsCrossingAndConcession()
    {
        Fixture movementFixture = new Fixture();
        SimulationRuntime movementRuntime = movementFixture.CreateRuntime();
        FaultRuntime(movementRuntime);

        Assert.That(movementRuntime.TryExecuteP17AWithdrawalCrossing(
            movementFixture.WarId, movementFixture.SelectedForce, movementFixture.SourceHex,
            movementFixture.DestinationHex, movementFixture.Option, "p17a.faulted-crossing",
            WorldCommandOrigin.Scenario, movementFixture.Capability, out _,
            out ArmedForceSpatialFailure movementFailure), Is.False);
        Assert.That(movementFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.RuntimeFaulted));

        Fixture concessionFixture = new Fixture();
        SimulationRuntime concessionRuntime = concessionFixture.CreateRuntime(
            currentDay: Fixture.TargetDay + 1L);
        FaultRuntime(concessionRuntime);
        Assert.That(concessionRuntime.TryConcedeP17AWar(
            concessionFixture.WarId, concessionFixture.TargetParticipantId,
            "p17a.faulted-concession", WorldCommandOrigin.GM, concessionFixture.Capability,
            out PersistentStateFailure concessionFailure), Is.False);
        Assert.That(concessionFailure.Code, Is.EqualTo(PersistentStateFailureCode.RuntimeFaulted));
        Assert.That(concessionRuntime.WarStore.TryGet(concessionFixture.WarId, out PersistentWarRecord unchanged), Is.True);
        Assert.That(unchanged.LifecycleState, Is.EqualTo(WarLifecycleState.Active));
        Assert.That(unchanged.P17A.TerminalConcession, Is.Null);
    }

    [Test]
    public void WarStoreRejectsStaleP17ConcessionRevisionAndBoundaryDayWithoutMutation()
    {
        Fixture fixture = new Fixture();
        long initialRevision = fixture.Wars.Revision;
        WarTerminalConcession staleRevision = fixture.CreateConcession("p17a.stale-revision", Fixture.TargetDay + 1L);
        Assert.That(fixture.Wars.TryConcedeP17A(
            fixture.WarId, initialRevision - 1L, Fixture.TargetDay + 1L, staleRevision,
            out PersistentStateFailure revisionFailure), Is.False);
        Assert.That(revisionFailure.Code, Is.EqualTo(PersistentStateFailureCode.InvalidRecord));
        Assert.That(fixture.Wars.Revision, Is.EqualTo(initialRevision));

        WarTerminalConcession staleDay = fixture.CreateConcession("p17a.stale-day", Fixture.TargetDay);
        Assert.That(fixture.Wars.TryConcedeP17A(
            fixture.WarId, initialRevision, Fixture.TargetDay, staleDay,
            out PersistentStateFailure dayFailure), Is.False);
        Assert.That(dayFailure.Code, Is.EqualTo(PersistentStateFailureCode.InvalidRecord));
        Assert.That(fixture.Wars.Revision, Is.EqualTo(initialRevision));
        Assert.That(fixture.Wars.TryGet(fixture.WarId, out PersistentWarRecord unchanged), Is.True);
        Assert.That(unchanged.LifecycleState, Is.EqualTo(WarLifecycleState.Active));
        Assert.That(unchanged.P17A.TerminalConcession, Is.Null);
    }

    [Test]
    public void ExplicitConcessionProvenanceSurvivesRuntimeWarStoreClone()
    {
        Fixture fixture = new Fixture();
        long concessionDay = Fixture.TargetDay + 1L;
        WarTerminalConcession concession = fixture.CreateConcession("p17a.clone-concession", concessionDay);
        Assert.That(fixture.Wars.TryConcedeP17A(
            fixture.WarId, fixture.Wars.Revision, concessionDay, concession,
            out PersistentStateFailure sourceFailure), Is.True, sourceFailure.ToString());

        SimulationRuntime runtime = fixture.CreateRuntime(currentDay: concessionDay);
        Assert.That(runtime.WarStore, Is.Not.SameAs(fixture.Wars));
        Assert.That(runtime.TryCaptureP17AWarObservation(fixture.WarId,
            out P17AWarObservation observation, out PersistentStateFailure observationFailure),
            Is.True, observationFailure.ToString());
        Assert.That(observation.LifecycleState, Is.EqualTo(WarLifecycleState.Ended));
        Assert.That(observation.EndedAbsoluteDay, Is.EqualTo(concessionDay));
        Assert.That(observation.TerminalConcession.OperationId, Is.EqualTo(concession.OperationId));
        Assert.That(observation.TerminalConcession.ConcedingParticipantId, Is.EqualTo(fixture.TargetParticipantId));
        Assert.That(observation.TerminalConcession.AcceptedOrigin, Is.EqualTo(WorldCommandOrigin.GM));
        Assert.That(observation.TerminalConcession.AuthorityId, Is.EqualTo(Fixture.AuthorityId));
        Assert.That(observation.TerminalConcession.AcceptedAbsoluteDay, Is.EqualTo(concessionDay));
        Assert.That(observation.TerminalConcession.AcceptedOrder, Is.EqualTo(0L));
        Assert.That(runtime.WarStore.TryValidateSnapshotForHydration(
            runtime.WarStore.CaptureState(), out string diagnostic), Is.True, diagnostic);
    }

    [Test]
    public void BattleVictoryAloneLeavesP17WithdrawalGoalPendingAndWarActive()
    {
        Fixture fixture = new Fixture();
        SimulationRuntime runtime = fixture.CreateRuntime();
        long warRevision = runtime.WarStore.Revision;
        BattleId battleId = new BattleId("p17-battle-only");
        BattleSideId winningSide = new BattleSideId("p17-battle-side-a");
        BattleSideId otherSide = new BattleSideId("p17-battle-side-b");
        Assert.That(runtime.BattleStore.TryRegister(new PersistentBattleRecord(
            battleId,
            runtime.CurrentDay,
            warId: fixture.WarId,
            sides: new[]
            {
                new BattleStateSide(battleId, winningSide),
                new BattleStateSide(battleId, otherSide)
            },
            locationReference: SpatialReference.ForHex(fixture.SourceHex)),
            out PersistentStateFailure registerFailure), Is.True, registerFailure.ToString());
        Assert.That(runtime.BattleStore.TryStart(battleId, runtime.CurrentDay,
            SpatialReference.ForHex(fixture.SourceHex), out PersistentStateFailure startFailure),
            Is.True, startFailure.ToString());

        BattleResolutionProvenance d5 = new BattleResolutionProvenance(
            "p17-test-policy", "p17-test-numeric", "p17-test-projection", "p17-test-causal",
            "p17-test-source", "p17-test-capability", "p17-test-random", "p17-test-resolver");
        PersistentBattleOutcomeProvenance provenance = new PersistentBattleOutcomeProvenance(
            d5, "p17-test-d6b2-policy", "p17-test-plan-schema", "p17-test-coverage", "p17-test-plan");
        PersistentBattleTerminalOutcome outcome = new PersistentBattleTerminalOutcome(
            battleId, BattleOutcomeType.Victory, winningSide, runtime.CurrentDay, provenance);
        Assert.That(runtime.BattleStore.TryPrepareTerminalWrite(outcome,
            out PreparedBattleTerminalWrite prepared, out PersistentStateFailure prepareFailure),
            Is.True, prepareFailure.ToString());
        Assert.That(runtime.BattleStore.TryCommitTerminalWrite(prepared,
            out bool writeStarted, out PersistentStateFailure commitFailure), Is.True, commitFailure.ToString());
        Assert.That(writeStarted, Is.True);
        Assert.That(runtime.BattleStore.TryGet(battleId, out PersistentBattleRecord resolved), Is.True);
        Assert.That(resolved.LifecycleState, Is.EqualTo(BattleLifecycleState.Resolved));
        Assert.That(resolved.TerminalOutcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Victory));
        Assert.That(resolved.TerminalOutcome.WinningBattleSideId, Is.EqualTo(winningSide));

        Assert.That(runtime.TryCaptureP17AWarObservation(fixture.WarId,
            out P17AWarObservation observation, out PersistentStateFailure observationFailure),
            Is.True, observationFailure.ToString());
        Assert.That(observation.GoalStatus, Is.EqualTo(P17AWithdrawalGoalStatus.Pending));
        Assert.That(observation.GoalEvidence, Is.Null);
        Assert.That(observation.LifecycleState, Is.EqualTo(WarLifecycleState.Active));
        Assert.That(runtime.WarStore.Revision, Is.EqualTo(warRevision));
    }

    [Test]
    public void P17StateIsRejectedByStandardAndSelectedDailyCompositionsWithOrWithoutP16Input()
    {
        Fixture fixture = new Fixture();
        Assert.Throws<ArgumentException>(() => fixture.CreateRuntime(
            SimulationRuntimeCompositionProfile.Standard));
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(Fixture.TargetDay), null, null,
            armedForceStore: fixture.Forces,
            spatialAuthorityStore: fixture.SpatialAuthority,
            armedForceSpatialStateStore: fixture.Spatial,
            factionStore: fixture.Factions,
            warStore: fixture.Wars,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1()));

        PersistentWarStore emptyWars = new PersistentWarStore(
            fixture.Forces, fixture.Conflicts, fixture.Factions, fixture.SpatialAuthority, fixture.Spatial);
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(Fixture.TargetDay), null, null,
            armedForceStore: fixture.Forces,
            spatialAuthorityStore: fixture.SpatialAuthority,
            armedForceSpatialStateStore: fixture.Spatial,
            factionStore: fixture.Factions,
            warStore: emptyWars,
            compositionProfile: SimulationRuntimeCompositionProfile.P16AOneHopMilitary));

        Assert.That(ArmedForceSpatialStateStore.TryCreateP16A(
            fixture.Forces, fixture.SpatialAuthority, fixture.OwnerForce, fixture.SourceHex,
            "ration.item", "ration-v1", 5m, 2m, Fixture.TargetDay,
            out ArmedForceSpatialStateStore mismatchedP16, out ArmedForceSpatialFailure mismatchedFailure),
            Is.True, mismatchedFailure.ToString());
        Assert.That(mismatchedP16.ConfigureP17AProvenance(Fixture.AuthorityId), Is.True);
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(Fixture.TargetDay), null, null,
            armedForceStore: fixture.Forces,
            spatialAuthorityStore: fixture.SpatialAuthority,
            armedForceSpatialStateStore: mismatchedP16,
            factionStore: fixture.Factions,
            warStore: fixture.Wars,
            compositionProfile: SimulationRuntimeCompositionProfile.P17AWithdrawalWar,
            p17AScenarioAuthorityCapability: fixture.Capability));
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(Fixture.TargetDay), null, null,
            armedForceStore: fixture.Forces,
            spatialAuthorityStore: fixture.SpatialAuthority,
            armedForceSpatialStateStore: fixture.Spatial,
            factionStore: fixture.Factions,
            warStore: fixture.Wars,
            compositionProfile: SimulationRuntimeCompositionProfile.P17AWithdrawalWar,
            p17AScenarioAuthorityCapability: new P17AScenarioAuthorityCapability("different-authority")));
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(Fixture.TargetDay), null, null,
            armedForceStore: fixture.Forces,
            factionStore: fixture.Factions,
            warStore: fixture.Wars,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1()));
    }

    private static void AssertInvalidConfiguration(
        Fixture fixture,
        string warIdSuffix,
        Func<WarId, P17AWarStrategicSection> createSection)
    {
        WarId warId = new WarId("p17-validation-" + warIdSuffix);
        PersistentWarStore store = fixture.CreateUnconfiguredWarStore(warId);
        long revision = store.Revision;
        Assert.That(store.TryConfigureP17A(warId, createSection(warId),
            out PersistentStateFailure failure), Is.False, warIdSuffix + " must be rejected");
        Assert.That(failure.Code, Is.EqualTo(PersistentStateFailureCode.InvalidRecord));
        Assert.That(store.Revision, Is.EqualTo(revision));
        Assert.That(store.TryGet(warId, out PersistentWarRecord unchanged), Is.True);
        Assert.That(unchanged.P17A, Is.Null);
    }

    private static void FaultRuntime(SimulationRuntime runtime)
    {
        FieldInfo guardField = typeof(SimulationRuntime).GetField("mutationGuard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(guardField, Is.Not.Null);
        object guard = guardField.GetValue(runtime);
        MethodInfo markFaulted = guard.GetType().GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(markFaulted, Is.Not.Null);
        markFaulted.Invoke(guard, new object[] { AuthoritativeMutationFaultReason.RollbackRestoreFailed });
        Assert.That(runtime.IsMutationFaulted, Is.True);
    }

    [Test]
    public void P17CommandAndDayAdvanceRejectOffOwnerThreadAndUseOneExclusiveLease()
    {
        Fixture fixture = new Fixture();
        SimulationRuntime runtime = fixture.CreateRuntime();
        bool moved = true;
        ArmedForceSpatialFailure movementFailure = ArmedForceSpatialFailure.None;
        Thread commandThread = new Thread(() => moved = runtime.TryExecuteP17AWithdrawalCrossing(
            fixture.WarId, fixture.SelectedForce, fixture.SourceHex, fixture.DestinationHex,
            fixture.Option, "p17a.off-thread", WorldCommandOrigin.Scenario, fixture.Capability,
            out _, out movementFailure));
        commandThread.Start();
        commandThread.Join();
        Assert.That(moved, Is.False);
        Assert.That(movementFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.RuntimeOperationInProgress));

        bool advanced = true;
        SimulationRuntimeAdvanceFailure dayFailure = SimulationRuntimeAdvanceFailure.None;
        Thread dayThread = new Thread(() => advanced = runtime.TryAdvanceDay(out dayFailure));
        dayThread.Start();
        dayThread.Join();
        Assert.That(advanced, Is.False);
        Assert.That(dayFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(Fixture.TargetDay));

        MethodInfo acquireLease = typeof(SimulationRuntime).GetMethod(
            "TryAcquireAdvanceLease", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(acquireLease, Is.Not.Null);
        object[] arguments = { null };
        Assert.That((bool)acquireLease.Invoke(runtime, arguments), Is.True);
        IDisposable lease = arguments[0] as IDisposable;
        Assert.That(lease, Is.Not.Null);
        using (lease)
        {
            Assert.That(runtime.TryExecuteP17AWithdrawalCrossing(
                fixture.WarId, fixture.SelectedForce, fixture.SourceHex, fixture.DestinationHex,
                fixture.Option, "p17a.lease-held", WorldCommandOrigin.Scenario, fixture.Capability,
                out _, out ArmedForceSpatialFailure leaseFailure), Is.False);
            Assert.That(leaseFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.RuntimeOperationInProgress));
        }
    }

    private sealed class Fixture
    {
        public const string AuthorityId = "scenario-gm-p17";
        public const long TargetDay = 10L;

        public readonly HexId SourceHex = new HexId("p17-source");
        public readonly HexId DestinationHex = new HexId("p17-destination");
        public readonly HexId OtherSourceHex = new HexId("p17-other-source");
        public readonly ArmedForceId SelectedForce = new ArmedForceId("p17-force-b");
        public readonly ArmedForceId OwnerForce = new ArmedForceId("p17-force-a");
        public readonly WarId WarId = new WarId("p17-war-runtime");
        public readonly WarStrategicParticipantId OwnerParticipantId = new WarStrategicParticipantId("strategic-a");
        public readonly WarStrategicParticipantId TargetParticipantId = new WarStrategicParticipantId("strategic-b");
        public readonly FactionId OwnerFactionId = new FactionId("p17-faction-a");
        public readonly FactionId TargetFactionId = new FactionId("p17-faction-b");
        public readonly WarSideId OwnerSideId = new WarSideId("p17-side-a");
        public readonly WarSideId TargetSideId = new WarSideId("p17-side-b");
        public readonly WarParticipantBindingId OwnerBindingId = new WarParticipantBindingId("p17-binding-a");
        public readonly WarParticipantBindingId TargetBindingId = new WarParticipantBindingId("p17-binding-b");
        public readonly TraversalOptionRef Option = TraversalOptionRef.ForConnection(new ConnectionId("p17-road"));
        public readonly P17AScenarioAuthorityCapability Capability = new P17AScenarioAuthorityCapability(AuthorityId);
        public readonly SpatialAuthorityStore SpatialAuthority;
        public readonly ArmedForceStore Forces;
        public readonly ArmedForceSpatialStateStore Spatial;
        public readonly FactionStore Factions;
        public readonly PersistentConflictStore Conflicts;
        public readonly PersistentWarStore Wars;

        public Fixture()
        {
            PersonStore persons = new PersonStore();
            Forces = new ArmedForceStore(persons);
            Assert.That(Forces.TryRegister(new ArmedForceRecord(OwnerForce, "Owner force", 0L), out _), Is.True);
            Assert.That(Forces.TryRegister(new ArmedForceRecord(SelectedForce, "Target force", 0L), out _), Is.True);

            SpatialAuthority = new SpatialAuthorityStore();
            HexRecord[] hexes =
            {
                Hex(SourceHex.Value, 0, 0),
                Hex(DestinationHex.Value, 1, 0),
                Hex(OtherSourceHex.Value, 2, 0)
            };
            Assert.That(SpatialAuthority.TryComposeGeography(new SpatialGeographyDefinition(
                new SpatialWorldScaleContext("scale", "p17-runtime-test", "v1", 1m, "step"), hexes), out _), Is.True);
            Assert.That(SpatialAuthority.PassageAuthority.TryRegisterConnection(
                new PassageOptionRecord(Option,
                    new HexBoundaryKey(SourceHex, DestinationHex), "road", "road-v1"),
                PassageCondition.Available, out _), Is.True);
            Assert.That(ArmedForceSpatialStateStore.TryCreateP16A(
                Forces, SpatialAuthority, SelectedForce, SourceHex,
                "ration.item", "ration-v1", 5m, 2m, TargetDay,
                out ArmedForceSpatialStateStore spatial, out ArmedForceSpatialFailure spatialFailure), Is.True, spatialFailure.ToString());
            Spatial = spatial;
            Assert.That(Spatial.ConfigureP17AProvenance(AuthorityId), Is.True);

            Factions = new FactionStore(persons);
            Assert.That(Factions.TryRegister(new FactionRecord(OwnerFactionId, "Faction A", 0L), out _), Is.True);
            Assert.That(Factions.TryRegister(new FactionRecord(TargetFactionId, "Faction B", 0L), out _), Is.True);

            Conflicts = new PersistentConflictStore(Forces);
            Wars = new PersistentWarStore(Forces, Conflicts, Factions, SpatialAuthority, Spatial);
            WarParticipantBinding bindingA = new WarParticipantBinding(
                OwnerBindingId, WarId, OwnerSideId, OwnerForce);
            WarParticipantBinding bindingB = new WarParticipantBinding(
                TargetBindingId, WarId, TargetSideId, SelectedForce);
            Assert.That(Wars.TryRegister(new PersistentWarRecord(
                WarId,
                0L,
                sides: new[] { new WarStateSide(WarId, OwnerSideId), new WarStateSide(WarId, TargetSideId) },
                participantBindings: new[] { bindingA, bindingB }), out PersistentStateFailure warFailure), Is.True, warFailure.ToString());
            P17AWarStrategicSection section = new P17AWarStrategicSection(
                AuthorityId,
                new[]
                {
                    new WarStrategicParticipant(OwnerParticipantId, WarId, OwnerFactionId, OwnerSideId),
                    new WarStrategicParticipant(TargetParticipantId, WarId, TargetFactionId, TargetSideId)
                },
                CreateDemand(WarId, OwnerParticipantId, TargetParticipantId, bindingB.BindingId));
            Assert.That(Wars.TryConfigureP17A(WarId, section, out PersistentStateFailure configureFailure), Is.True, configureFailure.ToString());
        }

        public SimulationRuntime CreateRuntime(
            SimulationRuntimeCompositionProfile profile = SimulationRuntimeCompositionProfile.P17AWithdrawalWar,
            long? currentDay = null)
        {
            return new SimulationRuntime(
                new SimulationTime(currentDay ?? TargetDay), null, null,
                armedForceStore: Forces,
                spatialAuthorityStore: SpatialAuthority,
                armedForceSpatialStateStore: Spatial,
                factionStore: Factions,
                conflictStore: Conflicts,
                warStore: Wars,
                compositionProfile: profile,
                p17AScenarioAuthorityCapability: profile == SimulationRuntimeCompositionProfile.P17AWithdrawalWar
                    ? Capability
                    : null);
        }

        public PersistentWarStore CreateUnconfiguredWarStore(WarId warId)
        {
            PersistentWarStore store = new PersistentWarStore(
                Forces, Conflicts, Factions, SpatialAuthority, Spatial);
            Assert.That(store.TryRegister(new PersistentWarRecord(
                warId,
                0L,
                sides: new[]
                {
                    new WarStateSide(warId, OwnerSideId),
                    new WarStateSide(warId, TargetSideId)
                },
                participantBindings: new[]
                {
                    new WarParticipantBinding(OwnerBindingId, warId, OwnerSideId, OwnerForce),
                    new WarParticipantBinding(TargetBindingId, warId, TargetSideId, SelectedForce)
                }), out PersistentStateFailure registerFailure), Is.True, registerFailure.ToString());
            return store;
        }

        public WarWithdrawalDemand CreateDemand(
            WarId warId,
            WarStrategicParticipantId ownerParticipantId,
            WarStrategicParticipantId targetParticipantId,
            WarParticipantBindingId targetBindingId) => new WarWithdrawalDemand(
                new WarActualGoalId("p17-leave-source"),
                warId,
                ownerParticipantId,
                targetParticipantId,
                targetBindingId,
                SourceHex,
                0L);

        public WarTerminalConcession CreateConcession(string operationId, long acceptedDay) => new WarTerminalConcession(
            operationId,
            TargetParticipantId,
            WarConcessionReason.Concession,
            WorldCommandOrigin.GM,
            AuthorityId,
            acceptedDay,
            0L);

        private static HexRecord Hex(string id, int q, int r) => new HexRecord(
            new HexId(id),
            new HexCoordinate(q, r),
            new TerrainReference(new TerrainDefinitionId("p17-terrain"), "p17-terrain-v1"));
    }
}
