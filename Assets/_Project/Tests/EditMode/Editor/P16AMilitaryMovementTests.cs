using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class P16AMilitaryMovementTests
{
    private static readonly TraversalCostContext Context =
        new TraversalCostContext("P16A-MilitaryOneHop", "v1", 1m, 1m, 1m);

    [Test]
    public void OnePassageAtomicallyMovesSelectedForceAndDebitsItsOneCompatibleItem()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m);
        long ownerRevision = fixture.Spatial.Revision;
        Assert.That(fixture.Execute(fixture.Selected, fixture.HexA, fixture.HexB, fixture.Option,
            Context, "p16a.operation.1", 10L, 1L, out P16ACrossingReceipt receipt), Is.True);

        Assert.That(fixture.Spatial.TryGetPosition(fixture.Selected, out SpatialReference position), Is.True);
        Assert.That(position.StableKey, Is.EqualTo("hex:hex-b"));
        Assert.That(fixture.Spatial.P16CurrentQuantity, Is.EqualTo(3m));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(ownerRevision + 1));
        Assert.That(receipt, Is.SameAs(fixture.Spatial.P16Receipt));
        Assert.That(receipt.OperationId, Is.EqualTo("p16a.operation.1"));
        Assert.That(receipt.SourceHexId, Is.EqualTo("hex-a"));
        Assert.That(receipt.DestinationHexId, Is.EqualTo("hex-b"));
        Assert.That(receipt.LogicalBoundary, Is.EqualTo(10L));
        Assert.That(receipt.AcceptedOrder, Is.EqualTo(1L));
        Assert.That(receipt.OptionContentRevision, Is.EqualTo("road-v1"));
        Assert.That(receipt.TraversalContextIdentity, Is.EqualTo("P16A-MilitaryOneHop"));
        Assert.That(fixture.Spatial.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void ImpairedPassageIsAllowedOnlyByP8AvailabilityAndRetainsTheCondition()
    {
        Fixture fixture = new Fixture(initialStock: 5m, debit: 2m, initialCondition: PassageCondition.Impaired);
        Assert.That(fixture.Execute(fixture.Selected, fixture.HexA, fixture.HexB, fixture.Option,
            Context, "p16a.impaired", 10L, 1L, out P16ACrossingReceipt receipt), Is.True);
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
        Assert.That(fixture.Spatial.TryExecuteP16ACrossing(fixture.Selected, fixture.HexA, fixture.HexB,
            fixture.Option, Context, "p16a.stale-passage", ownerRevision, fixture.Forces.Revision,
            stalePassageRevision, 10L, 1L, out _, out ArmedForceSpatialFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(ArmedForceSpatialFailureCode.MovementStateStale));
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
            Context, "p16a.once", 10L, 1L, out _), Is.True);
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
            Context, "p16a.clone", 10L, 1L, out _), Is.True);
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
        Assert.That(clonedState.CurrentQuantity, Is.EqualTo(state.CurrentQuantity));
        Assert.That(clonedState.Receipt.OperationId, Is.EqualTo(state.Receipt.OperationId));
        Assert.That(clonedState.Receipt.PassageAuthorityRevision, Is.EqualTo(state.Receipt.PassageAuthorityRevision));
        Assert.That(clone.Positions[0].StableKey, Is.EqualTo(source.Spatial.Positions[0].StableKey));
        Assert.That(clone.Positions.Select(position => position.StableKey),
            Is.EqualTo(source.Spatial.Positions.Select(position => position.StableKey)));
        Assert.That(clone.ValidateInvariants().IsValid, Is.True);

        P16AStateSnapshot invalid = new P16AStateSnapshot(state.ForceId, "hex:hex-far",
            state.ItemDefinitionId, state.ItemContentRevision, state.InitialQuantity,
            state.QuantityPerCrossing, state.CurrentQuantity, state.Receipt, state.OwnerRevision);
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
            10L, 1L, out _, expectedOwnerRevision), Is.False);
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
        public ArmedForceSpatialFailure LastFailure;

        public Fixture(decimal initialStock, decimal debit,
            PassageCondition initialCondition = PassageCondition.Available, bool addUnovercomeBarrier = false)
        {
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
                out ArmedForceSpatialStateStore spatial, out ArmedForceSpatialFailure failure), Is.True, failure.ToString());
            Spatial = spatial;
        }

        public bool Execute(ArmedForceId force, HexId source, HexId destination, TraversalOptionRef option,
            TraversalCostContext context, string operationId, long boundary, long order,
            out P16ACrossingReceipt receipt, long? expectedOwnerRevision = null)
        {
            bool result = Spatial.TryExecuteP16ACrossing(force, source, destination, option, context,
                operationId, expectedOwnerRevision ?? Spatial.Revision, Forces.Revision, Authority.Revision,
                boundary, order, out receipt, out LastFailure);
            return result;
        }

        private static HexRecord Hex(string id, int q, int r) => new HexRecord(new HexId(id),
            new HexCoordinate(q, r), new TerrainReference(new TerrainDefinitionId("terrain"), "terrain-v1"));
    }
}
