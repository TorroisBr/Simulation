using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class PopulationOwnerContractTests
{
    [Test]
    public void MutationStatusBindingRetainsIdentityAndBlocksFaultedOwnerWrites()
    {
        SettlementPopulationRuntime population = new SettlementPopulationRuntime("city-a", 8);
        TestMutationStatus healthy = new TestMutationStatus(true);
        TestMutationStatus other = new TestMutationStatus(true);

        Assert.That(PopulationOwnerBridge.CanBindMutationStatus(population, null), Is.False);
        Assert.That(PopulationOwnerBridge.CanBindMutationStatus(population, healthy), Is.True);
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(population, healthy), Is.True);
        Assert.That(PopulationOwnerBridge.CanBindMutationStatus(population, healthy), Is.True);
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(population, healthy), Is.True);
        Assert.That(PopulationOwnerBridge.CanBindMutationStatus(population, other), Is.False);
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(population, other), Is.False);

        SettlementPopulationRuntime faulted = new SettlementPopulationRuntime("city-b", 8);
        TestMutationStatus faultedStatus = new TestMutationStatus(false);
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(faulted, faultedStatus), Is.True);
        Assert.That(SettlementPopulationSystem.TryPropose(
            faulted,
            new PopulationChangeSet(1, 0, 0, 0),
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure proposalFailure), Is.True);

        Assert.That(SettlementPopulationSystem.TryApply(
            faulted,
            transition,
            out PopulationTransitionFailure applyFailure), Is.False);
        Assert.That(applyFailure, Is.EqualTo(PopulationTransitionFailure.RuntimeFaulted));
        Assert.That(faulted.CurrentPopulation, Is.EqualTo(8));
        Assert.That(faulted.Revision, Is.EqualTo(0));
    }

    [Test]
    public void PairMigrationAcceptsUnboundOrIdenticallyBoundOwnersAndRejectsMismatchedBindingAtomically()
    {
        SettlementPopulationRuntime unboundOrigin = new SettlementPopulationRuntime("origin-unbound", 3);
        SettlementPopulationRuntime unboundDestination = new SettlementPopulationRuntime("destination-unbound", 4);
        Assert.That(PopulationOwnerBridge.TryApplyPairedMigration(
            unboundOrigin, unboundDestination, 0, 0, 3, 4,
            out PopulationTransitionFailure unboundFailure), Is.True);
        Assert.That(unboundFailure, Is.EqualTo(PopulationTransitionFailure.None));
        Assert.That(unboundOrigin.CurrentPopulation, Is.EqualTo(2));
        Assert.That(unboundDestination.CurrentPopulation, Is.EqualTo(5));

        SettlementPopulationRuntime origin = new SettlementPopulationRuntime("origin", 3);
        SettlementPopulationRuntime destination = new SettlementPopulationRuntime("destination", 4);
        TestMutationStatus shared = new TestMutationStatus(true);
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(origin, shared), Is.True);
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(destination, shared), Is.True);
        Assert.That(PopulationOwnerBridge.TryApplyPairedMigration(
            origin, destination, 0, 0, 3, 4,
            out PopulationTransitionFailure sharedFailure), Is.True);
        Assert.That(sharedFailure, Is.EqualTo(PopulationTransitionFailure.None));

        SettlementPopulationRuntime mismatchedOrigin = new SettlementPopulationRuntime("origin-mismatch", 3);
        SettlementPopulationRuntime mismatchedDestination = new SettlementPopulationRuntime("destination-mismatch", 4);
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(mismatchedOrigin, new TestMutationStatus(true)), Is.True);
        Assert.That(PopulationOwnerBridge.TryApplyPairedMigration(
            mismatchedOrigin, mismatchedDestination, 0, 0, 3, 4,
            out PopulationTransitionFailure mismatchFailure), Is.False);
        Assert.That(mismatchFailure, Is.EqualTo(PopulationTransitionFailure.RuntimeOwnershipMismatch));
        Assert.That(mismatchedOrigin.CurrentPopulation, Is.EqualTo(3));
        Assert.That(mismatchedOrigin.Revision, Is.EqualTo(0));
        Assert.That(mismatchedDestination.CurrentPopulation, Is.EqualTo(4));
        Assert.That(mismatchedDestination.Revision, Is.EqualTo(0));
    }

    [Test]
    public void ReceiptSnapshotIsSortedDetachedAndHydratesAnUnpublishedOwner()
    {
        SettlementPopulationRuntime population = new SettlementPopulationRuntime("city", 10);
        ApplyReceipt(population, "z-operation", "fingerprint-z", new PopulationChangeSet(1, 0, 0, 0));
        ApplyReceipt(population, "a-operation", "fingerprint-a", new PopulationChangeSet(1, 0, 0, 0));

        Assert.That(PopulationOwnerBridge.TryCaptureOperationReceiptSnapshot(
            population,
            out IReadOnlyList<PopulationOperationReceiptSnapshot> receipts,
            out long receiptRevision), Is.True);
        Assert.That(receiptRevision, Is.EqualTo(2));
        Assert.That(receipts.Select(receipt => receipt.Identity), Is.EqualTo(new[] { "a-operation", "z-operation" }));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<PopulationOperationReceiptSnapshot>)receipts).Add(null));

        Assert.That(PopulationOwnerBridge.TryCreateFromOwnerSnapshot(
            population.SettlementRuntimeId,
            population.CurrentPopulation,
            population.Revision,
            receipts,
            receiptRevision,
            out SettlementPopulationRuntime staged), Is.True);
        Assert.That(staged, Is.Not.SameAs(population));
        Assert.That(staged.CurrentPopulation, Is.EqualTo(population.CurrentPopulation));
        Assert.That(staged.Revision, Is.EqualTo(population.Revision));
        Assert.That(PopulationOwnerBridge.IsP12PopulationBound(staged), Is.False);

        PopulationOwnerBridge.GetOperationReceiptCensus(staged, out int cardinality, out long stagedReceiptRevision);
        Assert.That(cardinality, Is.EqualTo(2));
        Assert.That(stagedReceiptRevision, Is.EqualTo(2));
        Assert.That(PopulationOwnerBridge.TryCreateFromOwnerSnapshot(
            population.SettlementRuntimeId,
            population.CurrentPopulation,
            population.Revision,
            receipts,
            1,
            out SettlementPopulationRuntime invalidStage), Is.False);
        Assert.That(invalidStage, Is.Null);
    }

    [Test]
    public void P12AdmissionAndCommitCallbacksKeepOwnerIdentityFlagsAndOrdering()
    {
        SettlementPopulationRuntime population = new SettlementPopulationRuntime("city", 8);
        List<string> callbacks = new List<string>();
        Func<SettlementPopulationRuntime, bool, bool, bool> admission = (owner, aggregateChanged, receiptChanged) =>
        {
            callbacks.Add("admit:" + ReferenceEquals(owner, population) + ":" + owner.CurrentPopulation + ":" +
                owner.Revision + ":" + aggregateChanged + ":" + receiptChanged);
            return true;
        };
        Action<SettlementPopulationRuntime, bool, bool> committed = (owner, aggregateChanged, receiptChanged) =>
        {
            callbacks.Add("commit:" + ReferenceEquals(owner, population) + ":" + owner.CurrentPopulation + ":" +
                owner.Revision + ":" + aggregateChanged + ":" + receiptChanged);
        };

        PopulationOwnerBridge.BindP12PopulationMutationBoundary(population, admission, committed);
        Assert.Throws<InvalidOperationException>(() =>
            PopulationOwnerBridge.BindP12PopulationMutationBoundary(population, admission, committed));
        ApplyReceipt(population, "operation", "fingerprint", new PopulationChangeSet(1, 0, 0, 0));

        Assert.That(callbacks, Is.EqualTo(new[]
        {
            "admit:True:8:0:True:True",
            "commit:True:9:1:True:True"
        }));
    }

    [Test]
    public void RollbackPointIsOwnerBoundAndPreservesReceiptPruningBehavior()
    {
        SettlementPopulationRuntime owner = new SettlementPopulationRuntime("city", 10);
        SettlementPopulationRuntime other = new SettlementPopulationRuntime("other", 4);
        PopulationRollbackPoint point = PopulationOwnerBridge.CaptureRollbackPoint(owner);

        ApplyReceipt(owner, "operation", "fingerprint", new PopulationChangeSet(0, 1, 0, 0));
        Assert.That(owner.CurrentPopulation, Is.EqualTo(9));
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(PopulationOwnerBridge.TryRestoreRollbackPoint(other, point), Is.False);
        Assert.That(other.CurrentPopulation, Is.EqualTo(4));
        Assert.That(other.Revision, Is.EqualTo(0));

        Assert.That(PopulationOwnerBridge.TryRestoreRollbackPoint(owner, point), Is.True);
        Assert.That(owner.CurrentPopulation, Is.EqualTo(10));
        Assert.That(owner.Revision, Is.EqualTo(0));
        PopulationOwnerBridge.GetOperationReceiptCensus(owner, out int cardinality, out long receiptRevision);
        Assert.That(cardinality, Is.EqualTo(0));
        Assert.That(receiptRevision, Is.EqualTo(2));
    }

    private static void ApplyReceipt(
        SettlementPopulationRuntime population,
        string identity,
        string fingerprint,
        PopulationChangeSet changes)
    {
        Assert.That(SettlementPopulationSystem.TryPropose(
            population,
            changes,
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(PopulationOwnerBridge.TryApplyTransitionWithReceipt(
            population,
            identity,
            fingerprint,
            transition,
            out bool newlyApplied,
            out PopulationTransitionFailure applyFailure), Is.True, applyFailure.ToString());
        Assert.That(newlyApplied, Is.True);
    }

    private sealed class TestMutationStatus : IAuthoritativeMutationStatus
    {
        public bool CanMutate { get; }

        public TestMutationStatus(bool canMutate)
        {
            CanMutate = canMutate;
        }
    }
}
