using System;
using System.Threading;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class P16AMilitaryMovementTests
{
    private static readonly TraversalCostContext Context =
        new TraversalCostContext("P16A-MilitaryOneHop", "v1", 1m, 1m, 1m);

    [Test]
    public void RuntimeOperationPinsCurrentBoundaryAndOrderAndStoreMutationIsNotPublic()
    {
        Assert.That(typeof(ArmedForceSpatialStateStore).GetMethod(
            "TryExecuteP16ACrossing", BindingFlags.Instance | BindingFlags.Public), Is.Null);
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        SimulationRuntime runtime = fixture.CreateRuntime();
        long boundary = runtime.CurrentDay;

        Assert.That(runtime.TryExecuteP16AMilitaryCrossing(fixture.Selected, fixture.HexA, fixture.HexB,
            fixture.Option, "p16a.runtime-owned", out P16ACrossingReceipt receipt,
            out ArmedForceSpatialFailure failure), Is.True, failure.ToString());

        Assert.That(receipt.LogicalBoundary, Is.EqualTo(boundary));
        Assert.That(receipt.AcceptedOrder, Is.Zero);
        Assert.That(receipt.TraversalContextIdentity, Is.EqualTo(P16AMilitaryMovementProfile.TraversalContextIdentity));
        Assert.That(runtime.ArmedForceSpatialStateStore.P16CurrentQuantity, Is.EqualTo(3m));
    }

    [Test]
    public void RuntimeOperationWaitsForItsPreboundLogicalBoundary()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m, targetBoundaryDay: 1L);
        SimulationRuntime runtime = fixture.CreateRuntime(initialDay: 0L);
        P16AStateSnapshot before = fixture.Spatial.CaptureP16AState();
        long ownerRevision = fixture.Spatial.Revision;

        Assert.That(runtime.TryExecuteP16AMilitaryCrossing(fixture.Selected, fixture.HexA, fixture.HexB,
            fixture.Option, "p16a.too-early", out _, out ArmedForceSpatialFailure earlyFailure), Is.False);
        Assert.That(earlyFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.MovementStateStale));
        Assert.That(fixture.Spatial.CaptureP16AState().PositionStableKey, Is.EqualTo(before.PositionStableKey));
        Assert.That(fixture.Spatial.P16CurrentQuantity, Is.EqualTo(before.CurrentQuantity));
        Assert.That(fixture.Spatial.P16Receipt, Is.SameAs(before.Receipt));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(ownerRevision));

        runtime.AdvanceDay();
        Assert.That(runtime.CurrentDay, Is.EqualTo(fixture.TargetBoundaryDay));
        Assert.That(runtime.TryExecuteP16AMilitaryCrossing(fixture.Selected, fixture.HexA, fixture.HexB,
            fixture.Option, "p16a.at-target", out P16ACrossingReceipt receipt,
            out ArmedForceSpatialFailure targetFailure), Is.True, targetFailure.ToString());
        Assert.That(receipt.LogicalBoundary, Is.EqualTo(fixture.TargetBoundaryDay));
        Assert.That(receipt.AcceptedOrder, Is.Zero);

        Fixture missed = new Fixture(initialStock: 5m, debit: 2m, targetBoundaryDay: 0L);
        SimulationRuntime missedRuntime = missed.CreateRuntime();
        missedRuntime.AdvanceDay();
        P16AStateSnapshot missedBefore = missed.Spatial.CaptureP16AState();
        long missedOwnerRevision = missed.Spatial.Revision;
        Assert.That(missedRuntime.TryExecuteP16AMilitaryCrossing(missed.Selected, missed.HexA, missed.HexB,
            missed.Option, "p16a.too-late", out _, out ArmedForceSpatialFailure lateFailure), Is.False);
        Assert.That(lateFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.MovementStateStale));
        Assert.That(missed.Spatial.CaptureP16AState().PositionStableKey, Is.EqualTo(missedBefore.PositionStableKey));
        Assert.That(missed.Spatial.P16CurrentQuantity, Is.EqualTo(missedBefore.CurrentQuantity));
        Assert.That(missed.Spatial.P16Receipt, Is.SameAs(missedBefore.Receipt));
        Assert.That(missed.Spatial.Revision, Is.EqualTo(missedOwnerRevision));
    }

    [Test]
    public void P16OneBoundaryProfileRejectsP18IntradayComposition()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(), null, null,
            armedForceStore: fixture.Forces,
            spatialAuthorityStore: fixture.Authority,
            armedForceSpatialStateStore: fixture.Spatial,
            compositionProfile: SimulationRuntimeCompositionProfile.P16AOneHopMilitary,
            p18dIntradayProfile: new P18DIntradayProfile("world", "profile", "config", "content")));
    }

    [Test]
    public void RuntimeOperationRejectsOffOwnerThreadWithoutOwnerMutation()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        SimulationRuntime runtime = fixture.CreateRuntime();
        bool succeeded = true;
        P16ACrossingReceipt receipt = null;
        ArmedForceSpatialFailure failure = ArmedForceSpatialFailure.None;
        Thread caller = new Thread(() =>
        {
            succeeded = runtime.TryExecuteP16AMilitaryCrossing(fixture.Selected, fixture.HexA, fixture.HexB,
                fixture.Option, "p16a.off-thread", out receipt, out failure);
        });
        caller.Start();
        caller.Join();

        Assert.That(succeeded, Is.False);
        Assert.That(failure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.RuntimeOperationInProgress));
        Assert.That(receipt, Is.Null);
        Assert.That(runtime.ArmedForceSpatialStateStore.TryGetPosition(fixture.Selected, out SpatialReference position), Is.True);
        Assert.That(position.StableKey, Is.EqualTo("hex:hex-a"));
        Assert.That(runtime.ArmedForceSpatialStateStore.P16CurrentQuantity, Is.EqualTo(5m));
        Assert.That(runtime.ArmedForceSpatialStateStore.P16Receipt, Is.Null);
        Assert.That(runtime.ArmedForceSpatialStateStore.Revision, Is.EqualTo(fixture.Spatial.Revision));
    }

    [Test]
    public void P16RuntimeDayAdvancementRejectsOffOwnerThread()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        SimulationRuntime runtime = fixture.CreateRuntime();
        long startingDay = runtime.CurrentDay;
        bool advancedDay = true;
        SimulationRuntimeAdvanceFailure dayFailure = SimulationRuntimeAdvanceFailure.None;
        Thread oneDayCaller = new Thread(() => advancedDay = runtime.TryAdvanceDay(out dayFailure));
        oneDayCaller.Start();
        oneDayCaller.Join();

        Assert.That(advancedDay, Is.False);
        Assert.That(dayFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(startingDay));

        bool advancedDays = true;
        int daysAdvanced = -1;
        SimulationRuntimeAdvanceFailure daysFailure = SimulationRuntimeAdvanceFailure.None;
        Thread manyDaysCaller = new Thread(() =>
            advancedDays = runtime.TryAdvanceDays(1, out daysAdvanced, out daysFailure));
        manyDaysCaller.Start();
        manyDaysCaller.Join();

        Assert.That(advancedDays, Is.False);
        Assert.That(daysAdvanced, Is.Zero);
        Assert.That(daysFailure, Is.EqualTo(SimulationRuntimeAdvanceFailure.RuntimeFaulted));
        Assert.That(runtime.CurrentDay, Is.EqualTo(startingDay));
    }

    [Test]
    public void RuntimeOperationRejectsWhileAdvanceLeaseHeldWithoutOwnerMutation()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        SimulationRuntime runtime = fixture.CreateRuntime();
        MethodInfo acquireLease = typeof(SimulationRuntime).GetMethod(
            "TryAcquireAdvanceLease", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(acquireLease, Is.Not.Null);
        object[] arguments = { null };
        Assert.That((bool)acquireLease.Invoke(runtime, arguments), Is.True);
        IDisposable lease = arguments[0] as IDisposable;
        Assert.That(lease, Is.Not.Null);
        using (lease)
        {
            P16AStateSnapshot before = runtime.ArmedForceSpatialStateStore.CaptureP16AState();
            long ownerRevision = runtime.ArmedForceSpatialStateStore.Revision;
            Assert.That(runtime.TryExecuteP16AMilitaryCrossing(fixture.Selected, fixture.HexA, fixture.HexB,
                fixture.Option, "p16a.lease-rejected", out _, out ArmedForceSpatialFailure failure), Is.False);
            Assert.That(failure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.RuntimeOperationInProgress));
            P16AStateSnapshot after = runtime.ArmedForceSpatialStateStore.CaptureP16AState();
            Assert.That(after.PositionStableKey, Is.EqualTo(before.PositionStableKey));
            Assert.That(after.CurrentQuantity, Is.EqualTo(before.CurrentQuantity));
            Assert.That(after.Receipt, Is.SameAs(before.Receipt));
            Assert.That(runtime.ArmedForceSpatialStateStore.Revision, Is.EqualTo(ownerRevision));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DailyProfileRejectsUnmovedAndMovedP16StateBeforeAdmission(bool crossingCommitted)
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        SimulationRuntime p16Runtime = fixture.CreateRuntime();
        if (crossingCommitted)
        {
            Assert.That(p16Runtime.TryExecuteP16AMilitaryCrossing(fixture.Selected, fixture.HexA, fixture.HexB,
                fixture.Option, "p16a.before-daily-rejection", out _, out ArmedForceSpatialFailure moveFailure),
                Is.True, moveFailure.ToString());
        }

        ArmedForceSpatialStateStore populatedOwner = p16Runtime.ArmedForceSpatialStateStore;
        P16AStateSnapshot before = populatedOwner.CaptureP16AState();
        long ownerRevision = populatedOwner.Revision;
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(), null, null,
            armedForceSpatialStateStore: populatedOwner,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1()));

        P16AStateSnapshot after = populatedOwner.CaptureP16AState();
        Assert.That(after.PositionStableKey, Is.EqualTo(before.PositionStableKey));
        Assert.That(after.CurrentQuantity, Is.EqualTo(before.CurrentQuantity));
        Assert.That(after.Receipt, Is.SameAs(before.Receipt));
        Assert.That(populatedOwner.Revision, Is.EqualTo(ownerRevision));
    }

    [Test]
    public void ZeroP16StateDailyProfileStillComposes()
    {
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

        Assert.That(runtime.ArmedForceSpatialStateStore.P16Profile, Is.Null);
    }

    [Test]
    public void OnePassageAtomicallyMovesSelectedForceAndDebitsItsOneCompatibleItem()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        long ownerRevision = fixture.Spatial.Revision;
        Assert.That(fixture.Execute(fixture.Selected, fixture.HexA, fixture.HexB, fixture.Option,
            Context, "p16a.operation.1", fixture.TargetBoundaryDay, 0L, out P16ACrossingReceipt receipt), Is.True);

        Assert.That(fixture.Spatial.TryGetPosition(fixture.Selected, out SpatialReference position), Is.True);
        Assert.That(position.StableKey, Is.EqualTo("hex:hex-b"));
        Assert.That(fixture.Spatial.P16CurrentQuantity, Is.EqualTo(3m));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(ownerRevision + 1));
        Assert.That(receipt, Is.SameAs(fixture.Spatial.P16Receipt));
        Assert.That(receipt.OperationId, Is.EqualTo("p16a.operation.1"));
        Assert.That(receipt.SourceHexId, Is.EqualTo("hex-a"));
        Assert.That(receipt.DestinationHexId, Is.EqualTo("hex-b"));
        Assert.That(receipt.LogicalBoundary, Is.EqualTo(10L));
        Assert.That(receipt.AcceptedOrder, Is.Zero);
        Assert.That(receipt.OptionContentRevision, Is.EqualTo("road-v1"));
        Assert.That(receipt.TraversalContextIdentity, Is.EqualTo("P16A-MilitaryOneHop"));
        Assert.That(fixture.Spatial.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void ImpairedPassageIsAllowedOnlyByP8AvailabilityAndRetainsTheCondition()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m, initialCondition: PassageCondition.Impaired);
        Assert.That(fixture.Execute(fixture.Selected, fixture.HexA, fixture.HexB, fixture.Option,
            Context, "p16a.impaired", fixture.TargetBoundaryDay, 0L, out P16ACrossingReceipt receipt), Is.True);
        Assert.That(receipt.PassageCondition, Is.EqualTo(PassageCondition.Impaired));
        Assert.That(fixture.Spatial.P16CurrentQuantity, Is.EqualTo(3m));
    }

    [TestCase(PassageCondition.Closed)]
    [TestCase(PassageCondition.Available)]
    public void RejectedClosedOrBlockedPassageLeavesEveryOwnedFieldAndRevisionUnchanged(PassageCondition condition)
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m, initialCondition: condition,
            addUnovercomeBarrier: condition == PassageCondition.Available);
        AssertRejectedUnchanged(fixture, fixture.Selected, fixture.HexA, fixture.HexB, fixture.Option,
            Context, "p16a.rejected", ArmedForceSpatialFailureCode.MovementRejected);
    }

    [Test]
    public void InsufficientSupplyAndIncompatibleContextRejectWithoutMutation()
    {
        Fixture shortStock = new Fixture(initialStock: 1m, debit: 2m);
        AssertRejectedUnchanged(shortStock, shortStock.Selected, shortStock.HexA, shortStock.HexB,
            shortStock.Option, Context, "p16a.short", ArmedForceSpatialFailureCode.InsufficientCarriedSupply);

        Fixture badContext = new Fixture(initialStock: 5m, debit: 2m);
        AssertRejectedUnchanged(badContext, badContext.Selected, badContext.HexA, badContext.HexB,
            badContext.Option, new TraversalCostContext("civil-route", "v1", 1m, 1m, 1m),
            "p16a.context", ArmedForceSpatialFailureCode.InvalidMovementInput);
    }

    [Test]
    public void ZeroInitialSupplyIsValidAndCrossingRejectsWithoutMutation()
    {
        Fixture empty = new Fixture(initialStock: 0m, debit: 2m);
        P16AStateSnapshot before = empty.Spatial.CaptureP16AState();
        long ownerRevision = empty.Spatial.Revision;
        Assert.That(before.InitialQuantity, Is.Zero);
        Assert.That(before.CurrentQuantity, Is.Zero);
        Assert.That(ArmedForceSpatialStateStore.ValidateP16AStateForHydration(
            before, empty.Forces, empty.Authority, out string diagnostic), Is.True, diagnostic);

        AssertRejectedUnchanged(empty, empty.Selected, empty.HexA, empty.HexB, empty.Option,
            Context, "p16a.empty-stock", ArmedForceSpatialFailureCode.InsufficientCarriedSupply);
        Assert.That(empty.Spatial.Revision, Is.EqualTo(ownerRevision));
    }

    [Test]
    public void StaleOwnerOrSourceInvalidOptionAndUnselectedForceRejectUnchanged()
    {
        Fixture staleOwner = new Fixture(initialStock: 5m, debit: 2m);
        AssertRejectedUnchanged(staleOwner, staleOwner.Selected, staleOwner.HexA, staleOwner.HexB,
            staleOwner.Option, Context, "p16a.stale-owner", ArmedForceSpatialFailureCode.MovementStateStale,
            expectedOwnerRevision: staleOwner.Spatial.Revision + 1);

        Fixture staleSource = new Fixture(initialStock: 5m, debit: 2m);
        AssertRejectedUnchanged(staleSource, staleSource.Selected, staleSource.HexB, staleSource.HexA,
            staleSource.Option, Context, "p16a.stale-source", ArmedForceSpatialFailureCode.MovementStateStale);

        Fixture invalidOption = new Fixture(initialStock: 5m, debit: 2m);
        AssertRejectedUnchanged(invalidOption, invalidOption.Selected, invalidOption.HexA, invalidOption.HexB,
            TraversalOptionRef.ForConnection(new ConnectionId("missing-road")), Context,
            "p16a.option", ArmedForceSpatialFailureCode.MovementRejected);

        Fixture unselected = new Fixture(initialStock: 5m, debit: 2m);
        AssertRejectedUnchanged(unselected, unselected.Other, unselected.HexA, unselected.HexB,
            unselected.Option, Context, "p16a.other", ArmedForceSpatialFailureCode.ForceNotSelected);

        Fixture missing = new Fixture(initialStock: 5m, debit: 2m);
        AssertRejectedUnchanged(missing, new ArmedForceId("force-missing"), missing.HexA, missing.HexB,
            missing.Option, Context, "p16a.missing", ArmedForceSpatialFailureCode.ForceNotRegistered);
    }

    [Test]
    public void PassageRevisionChangeRejectsOldExpectedContextWithoutMutation()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        long stalePassageRevision = fixture.Authority.Revision;
        Assert.That(fixture.Authority.PassageAuthority.TryChangePassageCondition(
            new HexBoundaryKey(fixture.HexA, fixture.HexB), fixture.Option, PassageCondition.Impaired, out _), Is.True);
        P16AStateSnapshot before = fixture.Spatial.CaptureP16AState();
        long ownerRevision = fixture.Spatial.Revision;
        Assert.That(fixture.Execute(fixture.Selected, fixture.HexA, fixture.HexB, fixture.Option,
    Context, "p16a.stale-passage", fixture.TargetBoundaryDay, 0L, out _, expectedPassageRevision: stalePassageRevision), Is.False);
        Assert.That(fixture.LastFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.MovementStateStale));
        Assert.That(fixture.Spatial.CaptureP16AState().PositionStableKey, Is.EqualTo(before.PositionStableKey));
        Assert.That(fixture.Spatial.P16CurrentQuantity, Is.EqualTo(before.CurrentQuantity));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(ownerRevision));
    }

    [Test]
    public void TerminatedForceAndNonNeighborEndpointRejectWithoutChangingP16AState()
    {
        Fixture terminated = new Fixture(initialStock: 5m, debit: 2m);
        Assert.That(terminated.Forces.TryTerminate(terminated.Selected, 1L, out _), Is.True);
        AssertRejectedUnchanged(terminated, terminated.Selected, terminated.HexA, terminated.HexB,
            terminated.Option, Context, "p16a.terminated", ArmedForceSpatialFailureCode.ForceTerminated);

        Fixture nonNeighbor = new Fixture(initialStock: 5m, debit: 2m);
        AssertRejectedUnchanged(nonNeighbor, nonNeighbor.Selected, nonNeighbor.HexA, nonNeighbor.HexFar,
            nonNeighbor.Option, Context, "p16a.non-neighbor", ArmedForceSpatialFailureCode.MovementRejected);
    }

    [Test]
    public void DuplicateOrSecondCrossingAndDirectPositionBypassRejectUnchanged()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        Assert.That(fixture.Execute(fixture.Selected, fixture.HexA, fixture.HexB, fixture.Option,
            Context, "p16a.once", fixture.TargetBoundaryDay, 0L, out _), Is.True);
        AssertRejectedUnchanged(fixture, fixture.Selected, fixture.HexB, fixture.HexA,
            fixture.Option, Context, "p16a.twice", ArmedForceSpatialFailureCode.CrossingAlreadyCommitted);

        P16AStateSnapshot before = fixture.Spatial.CaptureP16AState();
        long revision = fixture.Spatial.Revision;
        Assert.That(fixture.Spatial.TrySetPosition(fixture.Selected, SpatialReference.ForHex(fixture.HexA),
            out ArmedForceSpatialFailure setFailure), Is.False);
        Assert.That(setFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.DirectPositionMutationBlocked));
        Assert.That(fixture.Spatial.TryClearPosition(fixture.Selected, out ArmedForceSpatialFailure clearFailure), Is.False);
        Assert.That(clearFailure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.DirectPositionMutationBlocked));
        Assert.That(fixture.Spatial.CaptureP16AState().PositionStableKey, Is.EqualTo(before.PositionStableKey));
        Assert.That(fixture.Spatial.P16CurrentQuantity, Is.EqualTo(before.CurrentQuantity));
        Assert.That(fixture.Spatial.P16Receipt, Is.SameAs(before.Receipt));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void ExactStateCloneAndStagedRelationshipValidationPreserveCommittedMovement()
    {
        Fixture source = new Fixture(initialStock: 5m, debit: 2m);
        Assert.That(source.Spatial.TrySetPosition(source.Other, SpatialReference.ForHex(source.HexFar), out _), Is.True);
        Assert.That(source.Execute(source.Selected, source.HexA, source.HexB, source.Option,
            Context, "p16a.clone", source.TargetBoundaryDay, 0L, out _), Is.True);
        P16AStateSnapshot state = source.Spatial.CaptureP16AState();
        Assert.That(ArmedForceSpatialStateStore.ValidateP16AStateForHydration(
            state, source.Forces, source.Authority, out string validDiagnostic), Is.True, validDiagnostic);

        Fixture target = new Fixture(initialStock: 5m, debit: 2m);
        Assert.That(target.Spatial.TrySetPosition(target.Other, SpatialReference.ForHex(target.HexFar), out _), Is.True);
        MethodInfo cloneMethod = typeof(ArmedForceSpatialStateStore).GetMethod("Clone",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(cloneMethod, Is.Not.Null);
        ArmedForceSpatialStateStore clone = (ArmedForceSpatialStateStore)cloneMethod.Invoke(
            source.Spatial, new object[] { target.Forces, target.Authority, null });
        P16AStateSnapshot clonedState = clone.CaptureP16AState();
        Assert.That(clonedState.ForceId, Is.EqualTo(state.ForceId));
        Assert.That(clonedState.PositionStableKey, Is.EqualTo(state.PositionStableKey));
        Assert.That(clonedState.ItemDefinitionId, Is.EqualTo(state.ItemDefinitionId));
        Assert.That(clonedState.ItemContentRevision, Is.EqualTo(state.ItemContentRevision));
        Assert.That(clonedState.TargetBoundaryDay, Is.EqualTo(state.TargetBoundaryDay));
        Assert.That(clonedState.CurrentQuantity, Is.EqualTo(state.CurrentQuantity));
        Assert.That(clonedState.Receipt.OperationId, Is.EqualTo(state.Receipt.OperationId));
        Assert.That(clonedState.Receipt.PassageAuthorityRevision, Is.EqualTo(state.Receipt.PassageAuthorityRevision));
        Assert.That(clone.Positions[0].StableKey, Is.EqualTo(source.Spatial.Positions[0].StableKey));
        Assert.That(clone.Positions.Select(position => position.StableKey),
            Is.EqualTo(source.Spatial.Positions.Select(position => position.StableKey)));
        Assert.That(clone.ValidateInvariants().IsValid, Is.True);

        P16AStateSnapshot invalid = new P16AStateSnapshot(state.ForceId, "hex:hex-far",
            state.ItemDefinitionId, state.ItemContentRevision, state.InitialQuantity,
            state.QuantityPerCrossing, state.TargetBoundaryDay, state.CurrentQuantity, state.Receipt, state.OwnerRevision);
        Assert.That(ArmedForceSpatialStateStore.ValidateP16AStateForHydration(
            invalid, source.Forces, source.Authority, out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("P16-A staged receipt relationships"));
    }

    private static void AssertRejectedUnchanged(
        Fixture fixture,
        ArmedForceId force,
        HexId source,
        HexId destination,
        TraversalOptionRef option,
        TraversalCostContext context,
        string operationId,
        ArmedForceSpatialFailureCode expected,
        long? expectedOwnerRevision = null)
    {
        P16AStateSnapshot before = fixture.Spatial.CaptureP16AState();
        long ownerRevision = fixture.Spatial.Revision;
        Assert.That(fixture.Execute(force, source, destination, option, context, operationId,
            fixture.TargetBoundaryDay, 0L, out _, expectedOwnerRevision), Is.False);
        Assert.That(fixture.LastFailure.Code, Is.EqualTo(expected));
        P16AStateSnapshot after = fixture.Spatial.CaptureP16AState();
        Assert.That(after.PositionStableKey, Is.EqualTo(before.PositionStableKey));
        Assert.That(after.CurrentQuantity, Is.EqualTo(before.CurrentQuantity));
        Assert.That(after.Receipt, Is.SameAs(before.Receipt));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(ownerRevision));
    }

    private sealed class Fixture
    {
        public readonly HexId HexA = new HexId("hex-a");
        public readonly HexId HexB = new HexId("hex-b");
        public readonly HexId HexFar = new HexId("hex-far");
        public readonly ArmedForceId Selected = new ArmedForceId("force-selected");
        public readonly ArmedForceId Other = new ArmedForceId("force-other");
        public readonly TraversalOptionRef Option = TraversalOptionRef.ForConnection(new ConnectionId("road.ab"));
        public readonly SpatialAuthorityStore Authority;
        public readonly ArmedForceStore Forces;
        public readonly ArmedForceSpatialStateStore Spatial;
        public readonly long TargetBoundaryDay;
        public ArmedForceSpatialFailure LastFailure;

        public Fixture(decimal initialStock, decimal debit,
            PassageCondition initialCondition = PassageCondition.Available, bool addUnovercomeBarrier = false,
            long targetBoundaryDay = 10L)
        {
            TargetBoundaryDay = targetBoundaryDay;
            Authority = new SpatialAuthorityStore();
            HexRecord[] hexes =
            {
                Hex("hex-a", 0, 0), Hex("hex-b", 1, 0), Hex("hex-far", 8, 8)
            };
            Assert.That(Authority.TryComposeGeography(new SpatialGeographyDefinition(
                new SpatialWorldScaleContext("scale", "p16-test", "v1", 1m, "step"), hexes), out _), Is.True);
            HexBoundaryKey boundary = new HexBoundaryKey(HexA, HexB);
            if (addUnovercomeBarrier)
                Assert.That(Authority.PassageAuthority.TryRegisterBarrier(
                    new BarrierRecord(new BarrierId("barrier.ab"), "barrier", "v1", new[] { boundary }),
                    BarrierCondition.Active, out _), Is.True);
            Assert.That(Authority.PassageAuthority.TryRegisterConnection(
                new PassageOptionRecord(Option, boundary, "road", "road-v1"), initialCondition, out _), Is.True);

            Forces = new ArmedForceStore(new PersonStore());
            Assert.That(Forces.TryRegister(new ArmedForceRecord(Selected, "Selected", 0L), out _), Is.True);
            Assert.That(Forces.TryRegister(new ArmedForceRecord(Other, "Other", 0L), out _), Is.True);
            Assert.That(ArmedForceSpatialStateStore.TryCreateP16A(Forces, Authority, Selected,
                HexA, "ration.item", "ration-v3", initialStock, debit,
                TargetBoundaryDay,
                out ArmedForceSpatialStateStore spatial, out ArmedForceSpatialFailure failure), Is.True, failure.ToString());
            Spatial = spatial;
        }

        public SimulationRuntime CreateRuntime(long? initialDay = null)
        {
            return new SimulationRuntime(
                new SimulationTime(initialDay ?? TargetBoundaryDay), null, null,
                armedForceStore: Forces,
                spatialAuthorityStore: Authority,
                armedForceSpatialStateStore: Spatial,
                compositionProfile: SimulationRuntimeCompositionProfile.P16AOneHopMilitary);
        }

        public bool Execute(ArmedForceId force, HexId source, HexId destination, TraversalOptionRef option,
            TraversalCostContext context, string operationId, long boundary, long order,
            out P16ACrossingReceipt receipt, long? expectedOwnerRevision = null, long? expectedPassageRevision = null)
        {
            MethodInfo method = typeof(ArmedForceSpatialStateStore).GetMethod(
                "TryExecuteP16ACrossing", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new InvalidOperationException("P16-A owner operation was not found.");
            object[] arguments =
            {
                force, source, destination, option, context, operationId,
                expectedOwnerRevision ?? Spatial.Revision, Forces.Revision,
                expectedPassageRevision ?? Authority.Revision, boundary, order, null, null
            };
            bool result = (bool)method.Invoke(Spatial, arguments);
            receipt = arguments[11] as P16ACrossingReceipt;
            LastFailure = arguments[12] as ArmedForceSpatialFailure;
            return result;
        }

        private static HexRecord Hex(string id, int q, int r) => new HexRecord(new HexId(id),
            new HexCoordinate(q, r), new TerrainReference(new TerrainDefinitionId("terrain"), "terrain-v1"));
    }
}
