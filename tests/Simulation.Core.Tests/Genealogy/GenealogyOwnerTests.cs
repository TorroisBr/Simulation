using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public sealed class GenealogyOwnerTests
{
    [Test]
    public void ParentageRecord_PreservesPersonIdValueSemantics()
    {
        ParentageRecord first = Edge("parent", "child");
        ParentageRecord equal = Edge("parent", "child");
        ParentageRecord reverse = Edge("child", "parent");

        Assert.That(first, Is.EqualTo(equal));
        Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        Assert.That(first, Is.Not.EqualTo(reverse));
        Assert.That(first.ToString(), Is.EqualTo("parent -> child"));
        Assert.Throws<ArgumentNullException>(() => new ParentageRecord(null, new PersonId("child")));
        Assert.Throws<ArgumentNullException>(() => new ParentageRecord(new PersonId("parent"), null));
        Assert.Throws<ArgumentException>(() => Edge("same", "same"));
    }

    [Test]
    public void AddRemoveAndGraphQueries_PreserveFailuresAndDeterministicOrdering()
    {
        GenealogyStore owner = new GenealogyStore();
        PersonId root = Id("root");
        PersonId branchB = Id("branch-b");
        PersonId branchA = Id("branch-a");
        PersonId leaf = Id("leaf");

        Assert.That(owner.TryAddParentage(root, branchB, out GenealogyFailure addB), Is.True);
        Assert.That(addB.Code, Is.EqualTo(GenealogyFailureCode.None));
        Assert.That(owner.TryAddParentage(root, branchA, out _), Is.True);
        Assert.That(owner.TryAddParentage(branchA, leaf, out _), Is.True);
        Assert.That(owner.TryAddParentage(branchB, leaf, out _), Is.True);
        AssertIds(owner.GetParents(leaf), "branch-a", "branch-b");
        AssertIds(owner.GetChildren(root), "branch-a", "branch-b");
        AssertIds(owner.GetAncestors(leaf), "branch-a", "branch-b", "root");
        AssertIds(owner.GetDescendants(root), "branch-a", "branch-b", "leaf");
        Assert.That(owner.IsAncestorOf(root, leaf), Is.True);
        Assert.That(owner.IsDirectParent(root, branchA), Is.True);

        Assert.That(owner.TryAddParentage(Id("root"), Id("branch-a"), out GenealogyFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(GenealogyFailureCode.DuplicateParentage));
        Assert.That(owner.TryAddParentage(leaf, root, out GenealogyFailure cycle), Is.False);
        Assert.That(cycle.Code, Is.EqualTo(GenealogyFailureCode.WouldCreateCycle));
        Assert.That(owner.TryAddParentage(root, root, out GenealogyFailure self), Is.False);
        Assert.That(self.Code, Is.EqualTo(GenealogyFailureCode.SelfParent));
        Assert.That(owner.TryAddParentage(null, leaf, out GenealogyFailure invalidParent), Is.False);
        Assert.That(invalidParent.Code, Is.EqualTo(GenealogyFailureCode.InvalidParent));
        Assert.That(owner.TryAddParentage(root, null, out GenealogyFailure invalidChild), Is.False);
        Assert.That(invalidChild.Code, Is.EqualTo(GenealogyFailureCode.InvalidChild));
        Assert.That(owner.Count, Is.EqualTo(4));
        Assert.That(owner.Revision, Is.EqualTo(4L));

        Assert.That(owner.TryRemoveParentage(root, branchB, out GenealogyFailure removed), Is.True);
        Assert.That(removed.Code, Is.EqualTo(GenealogyFailureCode.None));
        Assert.That(owner.TryRemoveParentage(root, branchB, out GenealogyFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(GenealogyFailureCode.ParentageNotFound));
        AssertIds(owner.GetAncestors(leaf), "branch-a", "root");
        Assert.That(owner.Revision, Is.EqualTo(5L));
    }

    [Test]
    public void Records_AreSortedByParentThenChildAndReadOnly()
    {
        GenealogyStore owner = new GenealogyStore();
        Assert.That(owner.TryAddParentage(Id("z"), Id("b"), out _), Is.True);
        Assert.That(owner.TryAddParentage(Id("a"), Id("z"), out _), Is.True);
        Assert.That(owner.TryAddParentage(Id("a"), Id("b"), out _), Is.True);

        Assert.That(owner.Records[0].ToString(), Is.EqualTo("a -> b"));
        Assert.That(owner.Records[1].ToString(), Is.EqualTo("a -> z"));
        Assert.That(owner.Records[2].ToString(), Is.EqualTo("z -> b"));
        IList<ParentageRecord> readOnly = owner.Records as IList<ParentageRecord>;
        Assert.That(readOnly, Is.Not.Null);
        Assert.That(readOnly.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => readOnly.Add(Edge("x", "y")));
    }

    [Test]
    public void Export_IsDetachedAndSortedAndStagedOwnerPreservesRevisionGap()
    {
        GenealogyStore source = new GenealogyStore();
        Assert.That(source.TryAddParentage(Id("parent"), Id("child"), out _), Is.True);
        Assert.That(source.TryRemoveParentage(Id("parent"), Id("child"), out _), Is.True);
        Assert.That(source.Count, Is.Zero);
        Assert.That(source.Revision, Is.EqualTo(2L));

        GenealogyOwnerExport export = GenealogyOwnerBridge.CaptureOwnerSnapshot(source);
        Assert.That(export.SchemaVersion, Is.EqualTo(GenealogyOwnerExport.CurrentSchemaVersion));
        Assert.That(export.Revision, Is.EqualTo(2L));
        Assert.That(export.Records, Is.Empty);

        Assert.That(GenealogyOwnerBridge.TryCreateStagedOwner(
            export, out GenealogyStore restored, out GenealogyFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.None));
        Assert.That(restored, Is.Not.SameAs(source));
        Assert.That(restored.Count, Is.Zero);
        Assert.That(restored.Revision, Is.EqualTo(2L));
        Assert.That(restored.TryAddParentage(Id("next-parent"), Id("next-child"), out _), Is.True);
        Assert.That(restored.Revision, Is.EqualTo(3L));
        Assert.That(source.Count, Is.Zero);
        Assert.That(source.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void Export_DefensivelyCopiesRowsAndPersonIds()
    {
        ParentageRecord sourceEdge = Edge("a", "b");
        GenealogyOwnerExport export = new GenealogyOwnerExport(
            GenealogyOwnerExport.CurrentSchemaVersion,
            1,
            new[] { Edge("z", "z-child"), sourceEdge });

        Assert.That(export.Records, Has.Count.EqualTo(2));
        Assert.That(export.Records[0].ParentId.Value, Is.EqualTo("a"));
        Assert.That(export.Records[1].ParentId.Value, Is.EqualTo("z"));
        Assert.That(export.Records[0], Is.Not.SameAs(sourceEdge));
        Assert.That(export.Records[0].ParentId, Is.Not.SameAs(sourceEdge.ParentId));
        IList<ParentageRecord> rows = export.Records as IList<ParentageRecord>;
        Assert.That(rows, Is.Not.Null);
        Assert.That(rows.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => rows.Add(Edge("x", "y")));
    }

    [Test]
    public void StagedOwner_RejectsUnsupportedSchemaAndInvalidRevision()
    {
        AssertRejected(
            new GenealogyOwnerExport(2, 0, Array.Empty<ParentageRecord>()),
            GenealogyFailureCode.UnsupportedSnapshotSchema);
        AssertRejected(
            new GenealogyOwnerExport(GenealogyOwnerExport.CurrentSchemaVersion, -1, Array.Empty<ParentageRecord>()),
            GenealogyFailureCode.InvalidSnapshot);
        AssertRejected(
            new GenealogyOwnerExport(GenealogyOwnerExport.CurrentSchemaVersion, 0, null),
            GenealogyFailureCode.InvalidSnapshot);
        AssertRejected(
            new GenealogyOwnerExport(GenealogyOwnerExport.CurrentSchemaVersion, 0, new[] { Edge("a", "b") }),
            GenealogyFailureCode.InvalidSnapshot);
        AssertRejected(null, GenealogyFailureCode.InvalidSnapshot);
    }

    [Test]
    public void StagedOwner_RejectsNullDuplicateAndCyclicRowsAtomically()
    {
        ParentageRecord edge = Edge("a", "b");
        AssertRejected(
            new GenealogyOwnerExport(GenealogyOwnerExport.CurrentSchemaVersion, 1,
                new ParentageRecord[] { null }),
            GenealogyFailureCode.InvalidParentageRecord);
        AssertRejected(
            new GenealogyOwnerExport(GenealogyOwnerExport.CurrentSchemaVersion, 2, new[] { edge, edge }),
            GenealogyFailureCode.DuplicateParentage);
        AssertRejected(
            new GenealogyOwnerExport(GenealogyOwnerExport.CurrentSchemaVersion, 2,
                new[] { edge, Edge("b", "a") }),
            GenealogyFailureCode.WouldCreateCycle);

        GenealogyStore live = new GenealogyStore();
        Assert.That(live.TryAddParentage(Id("stable"), Id("child"), out _), Is.True);
        GenealogyOwnerExport rejected = new GenealogyOwnerExport(
            GenealogyOwnerExport.CurrentSchemaVersion, 2, new[] { edge, Edge("b", "a") });
        Assert.That(GenealogyOwnerBridge.TryCreateStagedOwner(rejected, out GenealogyStore staged, out _), Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(live.Count, Is.EqualTo(1));
        Assert.That(live.Revision, Is.EqualTo(1L));
        Assert.That(live.ContainsParentage(Id("stable"), Id("child")), Is.True);
    }

    [Test]
    public void StagedOwner_CallbackRunsInsideEachEdgeStageAndExceptionsEscape()
    {
        GenealogyOwnerExport export = new GenealogyOwnerExport(
            GenealogyOwnerExport.CurrentSchemaVersion,
            2,
            new[] { Edge("a", "b"), Edge("b", "c") });
        int stagedCallbacks = 0;

        Assert.Throws<InvalidOperationException>(() => GenealogyOwnerBridge.TryCreateStagedOwner(
            export,
            out _,
            out _,
            () =>
            {
                stagedCallbacks++;
                throw new InvalidOperationException("stage observer failure");
            }));
        Assert.That(stagedCallbacks, Is.EqualTo(1));
        Assert.That(export.Records, Has.Count.EqualTo(2));
    }

    [Test]
    public void MutationStatusBinding_IsOneWayIdentityBasedAndUnboundOwnersRemainUsable()
    {
        GenealogyStore owner = new GenealogyStore();
        FakeMutationStatus healthy = new FakeMutationStatus(true);
        FakeMutationStatus different = new FakeMutationStatus(true);

        Assert.That(GenealogyOwnerBridge.CanBindMutationStatus(null, healthy), Is.False);
        Assert.That(GenealogyOwnerBridge.CanBindMutationStatus(owner, null), Is.False);
        Assert.That(GenealogyOwnerBridge.CanBindMutationStatus(owner, healthy), Is.True);
        Assert.That(GenealogyOwnerBridge.TryBindMutationStatus(owner, healthy), Is.True);
        Assert.That(GenealogyOwnerBridge.CanBindMutationStatus(owner, healthy), Is.True);
        Assert.That(GenealogyOwnerBridge.TryBindMutationStatus(owner, healthy), Is.True);
        Assert.That(GenealogyOwnerBridge.CanBindMutationStatus(owner, different), Is.False);
        Assert.That(GenealogyOwnerBridge.TryBindMutationStatus(owner, different), Is.False);
        Assert.That(owner.TryAddParentage(Id("parent"), Id("child"), out _), Is.True);

        GenealogyStore unbound = new GenealogyStore();
        Assert.That(unbound.TryAddParentage(Id("unbound-parent"), Id("unbound-child"), out _), Is.True);
    }

    [Test]
    public void FaultedStatusBlocksMutationsButNullRecordFailurePrecedesGuard()
    {
        GenealogyStore owner = new GenealogyStore();
        Assert.That(GenealogyOwnerBridge.TryBindMutationStatus(owner, new FakeMutationStatus(false)), Is.True);

        Assert.That(owner.TryAddParentage(Id("parent"), Id("child"), out GenealogyFailure addFailure), Is.False);
        Assert.That(addFailure.Code, Is.EqualTo(GenealogyFailureCode.RuntimeFaulted));
        Assert.That(owner.TryRemoveParentage(null, null, out GenealogyFailure removeFailure), Is.False);
        Assert.That(removeFailure.Code, Is.EqualTo(GenealogyFailureCode.RuntimeFaulted));
        Assert.That(owner.TryAddParentage((ParentageRecord)null, out GenealogyFailure nullRecordFailure), Is.False);
        Assert.That(nullRecordFailure.Code, Is.EqualTo(GenealogyFailureCode.InvalidParentageRecord));
        Assert.That(owner.Count, Is.Zero);
        Assert.That(owner.Revision, Is.Zero);
    }

    [Test]
    public void BirthRollbackMayRemoveAtSaturatedRevisionOnlyThroughNamedBridge()
    {
        GenealogyOwnerExport saturated = new GenealogyOwnerExport(
            GenealogyOwnerExport.CurrentSchemaVersion,
            long.MaxValue,
            new[] { Edge("parent", "child") });
        Assert.That(GenealogyOwnerBridge.TryCreateStagedOwner(
            saturated, out GenealogyStore owner, out GenealogyFailure stageFailure), Is.True);
        Assert.That(stageFailure.Code, Is.EqualTo(GenealogyFailureCode.None));

        Assert.That(GenealogyOwnerBridge.TryRollbackParentageForBirth(
            owner, Id("parent"), Id("child"), out GenealogyFailure rollbackFailure), Is.True);
        Assert.That(rollbackFailure.Code, Is.EqualTo(GenealogyFailureCode.None));
        Assert.That(owner.Count, Is.Zero);
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void OrdinaryWritesRejectSaturatedRevisionWithoutChangingOwner()
    {
        GenealogyOwnerExport saturated = new GenealogyOwnerExport(
            GenealogyOwnerExport.CurrentSchemaVersion,
            long.MaxValue,
            new[] { Edge("parent", "child") });
        Assert.That(GenealogyOwnerBridge.TryCreateStagedOwner(
            saturated, out GenealogyStore owner, out _), Is.True);

        Assert.That(owner.TryAddParentage(Id("other-parent"), Id("other-child"),
            out GenealogyFailure addFailure), Is.False);
        Assert.That(addFailure.Code, Is.EqualTo(GenealogyFailureCode.RevisionOverflow));
        Assert.That(owner.TryRemoveParentage(Id("parent"), Id("child"),
            out GenealogyFailure removeFailure), Is.False);
        Assert.That(removeFailure.Code, Is.EqualTo(GenealogyFailureCode.RevisionOverflow));
        Assert.That(owner.Count, Is.EqualTo(1));
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(owner.ContainsParentage(Id("parent"), Id("child")), Is.True);
    }

    private static ParentageRecord Edge(string parent, string child)
    {
        return new ParentageRecord(Id(parent), Id(child));
    }

    private static PersonId Id(string value)
    {
        return new PersonId(value);
    }

    private static void AssertRejected(GenealogyOwnerExport export, GenealogyFailureCode expectedCode)
    {
        bool created = GenealogyOwnerBridge.TryCreateStagedOwner(
            export, out GenealogyStore staged, out GenealogyFailure failure);
        Assert.That(created, Is.False);
        Assert.That(staged, Is.Null);
        Assert.That(failure.Code, Is.EqualTo(expectedCode));
    }

    private static void AssertIds(IReadOnlyList<PersonId> actual, params string[] expected)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Length));
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(actual[index].Value, Is.EqualTo(expected[index]), "index " + index);
        }
    }

    private sealed class FakeMutationStatus : IAuthoritativeMutationStatus
    {
        public bool CanMutate { get; }

        internal FakeMutationStatus(bool canMutate)
        {
            CanMutate = canMutate;
        }
    }
}
