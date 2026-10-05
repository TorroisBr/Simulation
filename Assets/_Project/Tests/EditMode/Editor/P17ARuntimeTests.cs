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
        public readonly TraversalOptionRef Option = TraversalOptionRef.ForConnection(new ConnectionId("p17-road"));
        public readonly P17AScenarioAuthorityCapability Capability = new P17AScenarioAuthorityCapability(AuthorityId);
        public readonly SpatialAuthorityStore SpatialAuthority;
        public readonly ArmedForceStore Forces;
        public readonly ArmedForceSpatialStateStore Spatial;
        public readonly FactionStore Factions;
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
            FactionId factionA = new FactionId("p17-faction-a");
            FactionId factionB = new FactionId("p17-faction-b");
            Assert.That(Factions.TryRegister(new FactionRecord(factionA, "Faction A", 0L), out _), Is.True);
            Assert.That(Factions.TryRegister(new FactionRecord(factionB, "Faction B", 0L), out _), Is.True);

            PersistentConflictStore conflicts = new PersistentConflictStore(Forces);
            Wars = new PersistentWarStore(Forces, conflicts, Factions, SpatialAuthority, Spatial);
            WarSideId sideA = new WarSideId("p17-side-a");
            WarSideId sideB = new WarSideId("p17-side-b");
            WarParticipantBinding bindingA = new WarParticipantBinding(
                new WarParticipantBindingId("p17-binding-a"), WarId, sideA, OwnerForce);
            WarParticipantBinding bindingB = new WarParticipantBinding(
                new WarParticipantBindingId("p17-binding-b"), WarId, sideB, SelectedForce);
            Assert.That(Wars.TryRegister(new PersistentWarRecord(
                WarId,
                0L,
                sides: new[] { new WarStateSide(WarId, sideA), new WarStateSide(WarId, sideB) },
                participantBindings: new[] { bindingA, bindingB }), out PersistentStateFailure warFailure), Is.True, warFailure.ToString());
            P17AWarStrategicSection section = new P17AWarStrategicSection(
                AuthorityId,
                new[]
                {
                    new WarStrategicParticipant(OwnerParticipantId, WarId, factionA, sideA),
                    new WarStrategicParticipant(TargetParticipantId, WarId, factionB, sideB)
                },
                new WarWithdrawalDemand(
                    new WarActualGoalId("p17-leave-source"),
                    WarId,
                    OwnerParticipantId,
                    TargetParticipantId,
                    bindingB.BindingId,
                    SourceHex,
                    0L));
            Assert.That(Wars.TryConfigureP17A(WarId, section, out PersistentStateFailure configureFailure), Is.True, configureFailure.ToString());
        }

        public SimulationRuntime CreateRuntime(
            SimulationRuntimeCompositionProfile profile = SimulationRuntimeCompositionProfile.P17AWithdrawalWar)
        {
            return new SimulationRuntime(
                new SimulationTime(TargetDay), null, null,
                armedForceStore: Forces,
                spatialAuthorityStore: SpatialAuthority,
                armedForceSpatialStateStore: Spatial,
                factionStore: Factions,
                warStore: Wars,
                compositionProfile: profile,
                p17AScenarioAuthorityCapability: profile == SimulationRuntimeCompositionProfile.P17AWithdrawalWar
                    ? Capability
                    : null);
        }

        private static HexRecord Hex(string id, int q, int r) => new HexRecord(
            new HexId(id),
            new HexCoordinate(q, r),
            new TerrainReference(new TerrainDefinitionId("p17-terrain"), "p17-terrain-v1"));
    }
}
