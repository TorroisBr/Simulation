using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public sealed class GenealogyFoundationTests
{
    [Test]
    public void ParentageRecord_UsesPersonIdValueSemantics()
    {
        ParentageRecord first = new ParentageRecord(new PersonId("parent"), new PersonId("child"));
        ParentageRecord equal = new ParentageRecord(new PersonId("parent"), new PersonId("child"));
        ParentageRecord differentDirection = new ParentageRecord(new PersonId("child"), new PersonId("parent"));

        Assert.That(first, Is.EqualTo(equal));
        Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        Assert.That(first, Is.Not.EqualTo(differentDirection));
        Assert.That(first.ParentId.Value, Is.EqualTo("parent"));
        Assert.That(first.ChildId.Value, Is.EqualTo("child"));
        Assert.Throws<ArgumentException>(() => new ParentageRecord(new PersonId("same"), new PersonId("same")));
    }

    [Test]
    public void GenealogyStore_RejectsInvalidEndpointsWithStructuredFailures()
    {
        GenealogyStore store = new GenealogyStore();
        PersonId child = new PersonId("child");

        Assert.That(store.TryAddParentage(null, child, out GenealogyFailure invalidParent), Is.False);
        Assert.That(invalidParent.Code, Is.EqualTo(GenealogyFailureCode.InvalidParent));
        Assert.That(store.TryAddParentage(new PersonId("parent"), null, out GenealogyFailure invalidChild), Is.False);
        Assert.That(invalidChild.Code, Is.EqualTo(GenealogyFailureCode.InvalidChild));
        Assert.That(store.Count, Is.EqualTo(0));
    }

    [Test]
    public void DirectParentage_IsQueryableInBothDirections()
    {
        GenealogyStore store = new GenealogyStore();
        PersonId parent = new PersonId("parent");
        PersonId child = new PersonId("child");

        Assert.That(store.TryAddParentage(parent, child, out GenealogyFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.None));
        Assert.That(store.ContainsParentage(new PersonId("parent"), new PersonId("child")), Is.True);
        Assert.That(store.TryGetParentage(parent, child, out ParentageRecord record), Is.True);
        Assert.That(record.ParentId, Is.EqualTo(parent));
        Assert.That(store.IsDirectParent(parent, child), Is.True);
        AssertIds(store.GetParents(child), "parent");
        AssertIds(store.GetChildren(parent), "child");
    }

    [Test]
    public void SelfParent_IsRejectedWithoutChangingTheStore()
    {
        GenealogyStore store = new GenealogyStore();
        PersonId person = new PersonId("person");

        Assert.That(store.TryAddParentage(person, person, out GenealogyFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.SelfParent));
        Assert.That(store.Count, Is.EqualTo(0));
        Assert.That(store.Records, Is.Empty);
    }

    [Test]
    public void DuplicateParentage_IsRejectedAndOnlyOneEdgeRemains()
    {
        GenealogyStore store = new GenealogyStore();
        PersonId parent = new PersonId("parent");
        PersonId child = new PersonId("child");

        Assert.That(store.TryAddParentage(parent, child, out _), Is.True);
        Assert.That(store.TryAddParentage(new PersonId("parent"), new PersonId("child"), out GenealogyFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.DuplicateParentage));
        Assert.That(store.Count, Is.EqualTo(1));
        Assert.That(store.GetParents(child), Has.Count.EqualTo(1));
    }

    [Test]
    public void MultipleParents_AreAllowedWithoutAParentCountLimit()
    {
        GenealogyStore store = new GenealogyStore();
        Assert.That(store.TryAddParentage(new PersonId("parent-b"), new PersonId("child"), out _), Is.True);
        Assert.That(store.TryAddParentage(new PersonId("parent-a"), new PersonId("child"), out _), Is.True);

        AssertIds(store.GetParents(new PersonId("child")), "parent-a", "parent-b");
    }

    [Test]
    public void MultipleChildren_AreAllowedWithoutAChildCountLimit()
    {
        GenealogyStore store = new GenealogyStore();
        Assert.That(store.TryAddParentage(new PersonId("parent"), new PersonId("child-c"), out _), Is.True);
        Assert.That(store.TryAddParentage(new PersonId("parent"), new PersonId("child-a"), out _), Is.True);
        Assert.That(store.TryAddParentage(new PersonId("parent"), new PersonId("child-b"), out _), Is.True);

        AssertIds(store.GetChildren(new PersonId("parent")), "child-a", "child-b", "child-c");
    }

    [Test]
    public void DirectCycle_IsRejectedBeforeMutatingTheStore()
    {
        GenealogyStore store = new GenealogyStore();
        PersonId first = new PersonId("first");
        PersonId second = new PersonId("second");
        Assert.That(store.TryAddParentage(first, second, out _), Is.True);
        IReadOnlyList<ParentageRecord> before = store.Records;

        Assert.That(store.TryAddParentage(second, first, out GenealogyFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.WouldCreateCycle));
        Assert.That(store.Records, Is.EqualTo(before));
        Assert.That(store.Count, Is.EqualTo(1));
    }

    [Test]
    public void DeepCycle_IsRejectedWithIterativeTraversalAndAtomicity()
    {
        GenealogyStore store = new GenealogyStore();
        for (int index = 0; index < 4; index++)
        {
            Assert.That(store.TryAddParentage(
                new PersonId("person-" + index),
                new PersonId("person-" + (index + 1)),
                out _), Is.True);
        }

        IReadOnlyList<ParentageRecord> before = store.Records;
        Assert.That(store.TryAddParentage(
            new PersonId("person-4"),
            new PersonId("person-0"),
            out GenealogyFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.WouldCreateCycle));
        Assert.That(store.Records, Is.EqualTo(before));
        Assert.That(store.Count, Is.EqualTo(4));
    }

    [Test]
    public void DiamondQueries_ReturnUniqueDeterministicallyOrderedAncestorsAndDescendants()
    {
        GenealogyStore store = new GenealogyStore();
        Add(store, "root", "branch-c");
        Add(store, "root", "branch-b");
        Add(store, "branch-c", "leaf");
        Add(store, "branch-b", "leaf");

        AssertIds(store.GetDescendants(new PersonId("root")), "branch-b", "branch-c", "leaf");
        AssertIds(store.GetAncestors(new PersonId("leaf")), "branch-b", "branch-c", "root");
        Assert.That(store.IsAncestorOf(new PersonId("root"), new PersonId("leaf")), Is.True);
        Assert.That(store.IsAncestorOf(new PersonId("leaf"), new PersonId("root")), Is.False);
    }

    [Test]
    public void TransitiveOrdering_DoesNotDependOnInsertionOrder()
    {
        GenealogyStore first = new GenealogyStore();
        GenealogyStore second = new GenealogyStore();
        string[,] edges =
        {
            { "z", "child" },
            { "a", "child" },
            { "m", "other" },
            { "a", "other" }
        };

        for (int index = 0; index < edges.GetLength(0); index++)
        {
            Add(first, edges[index, 0], edges[index, 1]);
        }

        for (int index = edges.GetLength(0) - 1; index >= 0; index--)
        {
            Add(second, edges[index, 0], edges[index, 1]);
        }

        Assert.That(first.Records, Is.EqualTo(second.Records));
        AssertIds(first.GetParents(new PersonId("child")), "a", "z");
        AssertIds(first.GetChildren(new PersonId("a")), "child", "other");
    }

    [Test]
    public void Records_AreOrderedByParentThenChildAndReadOnly()
    {
        GenealogyStore store = new GenealogyStore();
        Add(store, "z", "b");
        Add(store, "a", "z");
        Add(store, "a", "b");

        Assert.That(store.Records[0].ToString(), Is.EqualTo("a -> b"));
        Assert.That(store.Records[1].ToString(), Is.EqualTo("a -> z"));
        Assert.That(store.Records[2].ToString(), Is.EqualTo("z -> b"));
        IList<ParentageRecord> readOnly = store.Records as IList<ParentageRecord>;
        Assert.That(readOnly, Is.Not.Null);
        Assert.That(readOnly.IsReadOnly, Is.True);
        Assert.Throws<NotSupportedException>(() => readOnly.Add(new ParentageRecord(new PersonId("x"), new PersonId("y"))));
    }

    [Test]
    public void RemoveParentage_RemovesOnlyTheRequestedEdge()
    {
        GenealogyStore store = new GenealogyStore();
        Add(store, "parent", "child-a");
        Add(store, "parent", "child-b");

        Assert.That(store.TryRemoveParentage(new PersonId("parent"), new PersonId("child-a"), out GenealogyFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.None));
        Assert.That(store.ContainsParentage(new PersonId("parent"), new PersonId("child-a")), Is.False);
        Assert.That(store.ContainsParentage(new PersonId("parent"), new PersonId("child-b")), Is.True);
        AssertIds(store.GetChildren(new PersonId("parent")), "child-b");

        Assert.That(store.TryRemoveParentage(new PersonId("parent"), new PersonId("child-a"), out GenealogyFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(GenealogyFailureCode.ParentageNotFound));
    }

    [Test]
    public void RemovingAnEdgeBreaksTheTransitiveRelation()
    {
        GenealogyStore store = new GenealogyStore();
        Add(store, "ancestor", "middle");
        Add(store, "middle", "descendant");
        Assert.That(store.IsAncestorOf(new PersonId("ancestor"), new PersonId("descendant")), Is.True);

        Assert.That(store.TryRemoveParentage(new PersonId("middle"), new PersonId("descendant"), out _), Is.True);
        Assert.That(store.IsAncestorOf(new PersonId("ancestor"), new PersonId("descendant")), Is.False);
        Assert.That(store.GetAncestors(new PersonId("descendant")), Is.Empty);
    }

    [Test]
    public void IndependentStores_WithSamePersonIdsDoNotContaminateEachOther()
    {
        GenealogyStore firstWorld = new GenealogyStore();
        GenealogyStore secondWorld = new GenealogyStore();
        Add(firstWorld, "person-a", "person-b");

        Assert.That(secondWorld.Count, Is.EqualTo(0));
        Assert.That(secondWorld.GetParents(new PersonId("person-b")), Is.Empty);
        Assert.That(firstWorld.GetParents(new PersonId("person-b")), Has.Count.EqualTo(1));
    }

    [Test]
    public void LargeChain_SupportsTransitiveQueriesAndCycleDetectionWithoutRecursion()
    {
        const int edgeCount = 250;
        GenealogyStore store = new GenealogyStore();
        for (int index = 0; index < edgeCount; index++)
        {
            Assert.That(store.TryAddParentage(
                new PersonId("person-" + index.ToString("D3")),
                new PersonId("person-" + (index + 1).ToString("D3")),
                out _), Is.True);
        }

        Assert.That(store.GetDescendants(new PersonId("person-000")), Has.Count.EqualTo(edgeCount));
        Assert.That(store.GetAncestors(new PersonId("person-" + edgeCount.ToString("D3"))), Has.Count.EqualTo(edgeCount));
        Assert.That(store.TryAddParentage(
            new PersonId("person-" + edgeCount.ToString("D3")),
            new PersonId("person-000"),
            out GenealogyFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.WouldCreateCycle));
        Assert.That(store.Count, Is.EqualTo(edgeCount));
    }

    [Test]
    public void NullQueries_ReturnReadOnlyEmptyCollections()
    {
        GenealogyStore store = new GenealogyStore();
        Assert.That(store.GetParents(null), Is.Empty);
        Assert.That(store.GetChildren(null), Is.Empty);
        Assert.That(store.GetAncestors(null), Is.Empty);
        Assert.That(store.GetDescendants(null), Is.Empty);
        IList<PersonId> empty = store.GetParents(null) as IList<PersonId>;
        Assert.That(empty, Is.Not.Null);
        Assert.That(empty.IsReadOnly, Is.True);
    }

    [Test]
    public void GenealogyCore_IsPureCSharpAndDoesNotReferenceRuntimeAdapters()
    {
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets",
            "_Project",
            "Scripts",
            "Genealogy");

        string[] files = Directory.GetFiles(directory, "*.cs");
        Assert.That(files, Is.Not.Empty);
        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            Assert.That(source, Does.Not.Contain("using UnityEngine;"), file);
            Assert.That(source, Does.Not.Contain("NpcRuntime"), file);
            Assert.That(source, Does.Not.Contain("SimulationRuntime"), file);
            Assert.That(source, Does.Not.Contain("PersonStore"), file);
            Assert.That(source, Does.Not.Contain("PersonRuntime"), file);
            Assert.That(source, Does.Not.Contain("DateTime.Now"), file);
        }
    }

    [Test]
    public void ParentageRecord_PublicPropertiesAreImmutable()
    {
        foreach (PropertyInfo property in typeof(ParentageRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.That(property.CanWrite, Is.False, property.Name);
        }
    }

    private static void Add(GenealogyStore store, string parent, string child)
    {
        Assert.That(store.TryAddParentage(new PersonId(parent), new PersonId(child), out GenealogyFailure failure), Is.True);
        Assert.That(failure.Code, Is.EqualTo(GenealogyFailureCode.None));
    }

    private static void AssertIds(IReadOnlyList<PersonId> actual, params string[] expected)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Length));
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(actual[index].Value, Is.EqualTo(expected[index]), "index " + index);
        }
    }
}
