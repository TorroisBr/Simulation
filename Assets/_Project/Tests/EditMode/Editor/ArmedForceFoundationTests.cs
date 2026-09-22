using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ArmedForceFoundationTests
{
    [Test]
    public void Identity_IsStableDistinctAndLookupIsStoreOwned()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore store = new ArmedForceStore(persons);
        ArmedForceId firstId = new ArmedForceId("force-first");
        ArmedForceId secondId = new ArmedForceId("force-second");

        Assert.That(firstId, Is.Not.EqualTo(secondId));
        Assert.That(store.TryRegister(new ArmedForceRecord(firstId, "First", 0L), out _), Is.True);
        Assert.That(store.TryRegister(new ArmedForceRecord(secondId, "Second", 0L), out _), Is.True);
        Assert.That(store.TryGet(new ArmedForceId("force-first"), out ArmedForceRecord resolved), Is.True);
        Assert.That(resolved.Id, Is.EqualTo(firstId));
        Assert.That(resolved.Id.ToString(), Is.EqualTo("force-first"));
        Assert.That(store.Forces[0].Id.Value, Is.EqualTo("force-first"));
        Assert.That(store.Forces[1].Id.Value, Is.EqualTo("force-second"));
    }

    [Test]
    public void Hierarchy_ValidatesParentExistenceSelfParentCyclesAndTraversal()
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        ArmedForceId root = new ArmedForceId("force-root");
        ArmedForceId child = new ArmedForceId("force-child");
        ArmedForceId grandchild = new ArmedForceId("force-grandchild");

        Assert.That(store.TryRegister(
            new ArmedForceRecord(new ArmedForceId("force-missing-parent"), "", 0L, new ArmedForceId("missing")),
            out ArmedForceFoundationFailure missingParent), Is.False);
        Assert.That(missingParent.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ParentNotRegistered));

        Assert.That(store.TryRegister(
            new ArmedForceRecord(root, "Root", 0L, root),
            out ArmedForceFoundationFailure selfParent), Is.False);
        Assert.That(selfParent.Code, Is.EqualTo(ArmedForceFoundationFailureCode.SelfParent));

        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Root", 0L), out _), Is.True);
        Assert.That(store.TryRegister(new ArmedForceRecord(child, "Child", 0L, root), out _), Is.True);
        Assert.That(store.TryRegister(new ArmedForceRecord(grandchild, "Grandchild", 0L, child), out _), Is.True);

        Assert.That(store.TryReparent(root, grandchild, out ArmedForceFoundationFailure cycle), Is.False);
        Assert.That(cycle.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ParentWouldCreateCycle));
        Assert.That(store.TryReparent(child, new ArmedForceId("missing"), out ArmedForceFoundationFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ParentNotRegistered));

        AssertForceIds(
            store.GetHierarchyPreOrder(root),
            "force-root",
            "force-child",
            "force-grandchild");
        Assert.That(store.GetChildren(root)[0].Id, Is.EqualTo(child));
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void DetachAndReattach_PreserveIdentityParentAndComposition()
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        ArmedForceId root = new ArmedForceId("force-main");
        ArmedForceId subforce = new ArmedForceId("force-sub");
        Assert.That(store.TryRegister(
            new ArmedForceRecord(root, "Main", 0L, operationalLocationReference: "origin"),
            out _), Is.True);
        Assert.That(store.TryRegister(
            new ArmedForceRecord(subforce, "Sub", 0L, root, "origin"),
            out _), Is.True);

        ContingentId contingentId = new ContingentId("contingent-sub");
        Assert.That(store.TryRegisterContingent(
            new ContingentRecord(
                contingentId,
                subforce,
                12L,
                new ContingentOriginReference("content", "sub-source"),
                "service-a"),
            out _), Is.True);

        Assert.That(store.TryDetach(
            subforce,
            "separate-location",
            out ArmedForceFoundationFailure detachFailure), Is.True, detachFailure.ToString());
        Assert.That(store.TryGet(subforce, out ArmedForceRecord detached), Is.True);
        Assert.That(detached.Id, Is.EqualTo(subforce));
        Assert.That(detached.ParentForceId, Is.EqualTo(root));
        Assert.That(detached.IsDetached, Is.True);
        Assert.That(detached.OperationalLocationReference, Is.EqualTo("separate-location"));

        Assert.That(store.TryReattach(subforce, out ArmedForceFoundationFailure reattachFailure), Is.True, reattachFailure.ToString());
        Assert.That(store.TryGet(subforce, out ArmedForceRecord reattached), Is.True);
        Assert.That(reattached.Id, Is.EqualTo(subforce));
        Assert.That(reattached.ParentForceId, Is.EqualTo(root));
        Assert.That(reattached.IsDetached, Is.False);
        Assert.That(store.TryGetContingent(contingentId, out ContingentRecord contingent), Is.True);
        Assert.That(contingent.ForceId, Is.EqualTo(subforce));
        Assert.That(store.Count, Is.EqualTo(2));
    }

    [Test]
    public void Contingents_AggregateSourcesAndTypesWithoutCreatingPersonsOrRuntimes()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore store = new ArmedForceStore(persons);
        ArmedForceId root = new ArmedForceId("force-root");
        ArmedForceId child = new ArmedForceId("force-child");
        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Root", 0L), out _), Is.True);
        Assert.That(store.TryRegister(new ArmedForceRecord(child, "Child", 0L, root), out _), Is.True);

        ContingentRecord first = new ContingentRecord(
            new ContingentId("contingent-b"),
            root,
            7L,
            new ContingentOriginReference("pool", "alpha"),
            "service-beta",
            new[] { new ArmedForceCharacteristic("form", "aggregate") });
        ContingentRecord second = new ContingentRecord(
            new ContingentId("contingent-a"),
            child,
            5L,
            new ContingentOriginReference("summoning", "ritual-1"),
            "service-alpha",
            new[] { new ArmedForceCharacteristic("origin-kind", "non-person") });
        Assert.That(store.TryRegisterContingent(first, out _), Is.True);
        Assert.That(store.TryRegisterContingent(second, out _), Is.True);

        Assert.That(store.GetContingents(root)[0].Id.Value, Is.EqualTo("contingent-b"));
        Assert.That(store.TryGetOrganizationalAggregateContingentAmount(
            root,
            out long amount,
            out ArmedForceFoundationFailure aggregateFailure), Is.True, aggregateFailure.ToString());
        Assert.That(amount, Is.EqualTo(12L));
        Assert.That(second.Origin.Domain, Is.EqualTo("summoning"));
        Assert.That(second.ServiceType, Is.EqualTo("service-alpha"));
        Assert.That(second.Characteristics[0].Key, Is.EqualTo("origin-kind"));
        Assert.That(store.TryReplaceContingent(
            new ContingentRecord(
                first.Id,
                root,
                9L,
                new ContingentOriginReference("pool", "alpha"),
                "service-beta",
                new[] { new ArmedForceCharacteristic("form", "updated") }),
            out ArmedForceFoundationFailure replacementFailure), Is.True, replacementFailure.ToString());
        Assert.That(store.TryGetContingent(first.Id, out ContingentRecord replaced), Is.True);
        Assert.That(replaced.Id, Is.EqualTo(first.Id));
        Assert.That(replaced.Amount, Is.EqualTo(9L));
        Assert.That(persons.Persons, Is.Empty);
    }

    [Test]
    public void ContingentProvenance_IsImmutablePerIdentityAndFailuresAreAtomic()
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        ArmedForceId forceId = new ArmedForceId("force-provenance");
        ContingentRecord original = new ContingentRecord(
            new ContingentId("contingent-provenance"),
            forceId,
            4L,
            new ContingentOriginReference("origin", "stable"),
            "service",
            new[] { new ArmedForceCharacteristic("kind", "initial") });
        Assert.That(store.TryRegister(new ArmedForceRecord(forceId, "Provenance", 0L), out _), Is.True);
        Assert.That(store.TryRegisterContingent(original, out _), Is.True);

        long revision = store.Revision;
        Assert.That(store.TryReplaceContingent(
            new ContingentRecord(
                original.Id,
                forceId,
                8L,
                original.Origin,
                original.ServiceType,
                new[] { new ArmedForceCharacteristic("kind", "changed") }),
            out _), Is.True);
        Assert.That(store.Revision, Is.EqualTo(revision + 1L));

        long failedRevision = store.Revision;
        Assert.That(store.TryReplaceContingent(
            new ContingentRecord(
                original.Id,
                forceId,
                11L,
                new ContingentOriginReference("origin", "mutated"),
                original.ServiceType),
            out ArmedForceFoundationFailure originFailure), Is.False);
        Assert.That(originFailure.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ContingentOriginMutation));
        Assert.That(store.Revision, Is.EqualTo(failedRevision));
        Assert.That(store.TryGetContingent(original.Id, out ContingentRecord afterOriginFailure), Is.True);
        Assert.That(afterOriginFailure.Amount, Is.EqualTo(8L));
        Assert.That(afterOriginFailure.Origin, Is.EqualTo(original.Origin));

        Assert.That(store.TryReplaceContingent(
            new ContingentRecord(
                original.Id,
                forceId,
                12L,
                original.Origin,
                "service-mutated"),
            out ArmedForceFoundationFailure serviceFailure), Is.False);
        Assert.That(serviceFailure.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ContingentServiceTypeMutation));
        Assert.That(store.Revision, Is.EqualTo(failedRevision));
    }

    [Test]
    public void OrganizationalAggregate_IncludesDetachedAndTerminatedDescendants()
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        ArmedForceId root = new ArmedForceId("force-aggregate-root");
        ArmedForceId child = new ArmedForceId("force-aggregate-child");
        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Root", 0L), out _), Is.True);
        Assert.That(store.TryRegister(new ArmedForceRecord(child, "Child", 0L, root), out _), Is.True);
        Assert.That(store.TryRegisterContingent(
            new ContingentRecord(
                new ContingentId("contingent-child"),
                child,
                6L,
                new ContingentOriginReference("source", "child"),
                "service"),
            out _), Is.True);

        Assert.That(store.TryDetach(child, "remote", out _), Is.True);
        Assert.That(store.TryGetOrganizationalAggregateContingentAmount(
            root,
            out long detachedAmount,
            out _), Is.True);
        Assert.That(detachedAmount, Is.EqualTo(6L));

        Assert.That(store.TryTerminate(child, 1L, out _), Is.True);
        Assert.That(store.TryGetOrganizationalAggregateContingentAmount(
            root,
            out long historicalAmount,
            out _), Is.True);
        Assert.That(historicalAmount, Is.EqualTo(6L));
    }

    [Test]
    public void RelevantPersons_UsePersonIdWithoutNpcMaterializationAndCommanderIsSeparate()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime commander = new PersonRuntime(new PersonId("person-commander"));
        PersonRuntime officer = new PersonRuntime(new PersonId("person-officer"));
        Assert.That(persons.TryRegister(commander, out _), Is.True);
        Assert.That(persons.TryRegister(officer, out _), Is.True);
        ArmedForceStore store = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("force-relevant");
        Assert.That(store.TryRegister(new ArmedForceRecord(forceId, "Relevant", 0L), out _), Is.True);

        Assert.That(store.TryAssignCommander(forceId, commander.PersonId, out _), Is.True);
        Assert.That(store.TryAddRelevantPerson(
            forceId,
            officer.PersonId,
            "officer",
            out ArmedForcePersonReference reference,
            out ArmedForceFoundationFailure personFailure), Is.True, personFailure.ToString());

        Assert.That(store.TryGet(forceId, out ArmedForceRecord force), Is.True);
        Assert.That(force.CommanderPersonId, Is.EqualTo(commander.PersonId));
        Assert.That(reference.PersonId, Is.EqualTo(officer.PersonId));
        Assert.That(reference.Id, Is.EqualTo(ArmedForcePersonReferenceId.BuildStableId(
            forceId,
            officer.PersonId,
            "officer")));
        Assert.That(commander.IsMaterialized, Is.False);
        Assert.That(officer.IsMaterialized, Is.False);
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void DeterministicOrdering_IsIndependentOfRegistrationOrder()
    {
        ArmedForceStore first = BuildDeterministicStore(false);
        ArmedForceStore second = BuildDeterministicStore(true);

        AssertForceIds(first.Forces, "force-a", "force-b", "force-root", "force-z");
        AssertForceIds(second.Forces, "force-a", "force-b", "force-root", "force-z");
        AssertForceIds(
            first.GetHierarchyPreOrder(new ArmedForceId("force-root")),
            "force-root",
            "force-a",
            "force-z",
            "force-b");
        AssertForceIds(
            second.GetHierarchyPreOrder(new ArmedForceId("force-root")),
            "force-root",
            "force-a",
            "force-z",
            "force-b");
        Assert.That(ContingentIds(first.GetContingents(new ArmedForceId("force-root"))),
            Is.EqualTo(new[] { "contingent-a", "contingent-z" }));
        Assert.That(ContingentIds(second.GetContingents(new ArmedForceId("force-root"))),
            Is.EqualTo(new[] { "contingent-a", "contingent-z" }));
    }

    [Test]
    public void Lifecycle_TerminationRetainsIdentityRejectsReuseAndPreservesReferences()
    {
        PersonStore persons = new PersonStore();
        PersonRuntime relevant = new PersonRuntime(new PersonId("person-history"));
        Assert.That(persons.TryRegister(relevant, out _), Is.True);
        ArmedForceStore store = new ArmedForceStore(persons);
        ArmedForceId root = new ArmedForceId("force-history");
        ArmedForceId child = new ArmedForceId("force-history-child");
        Assert.That(store.TryRegister(new ArmedForceRecord(root, "History", 2L), out _), Is.True);
        Assert.That(store.TryRegister(new ArmedForceRecord(child, "Child", 2L, root), out _), Is.True);
        Assert.That(store.TryAddRelevantPerson(root, relevant.PersonId, "hero", out _, out _), Is.True);

        Assert.That(store.TryTerminate(root, 3L, out ArmedForceFoundationFailure blocked), Is.False);
        Assert.That(blocked.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ActiveChildrenPreventTermination));
        Assert.That(store.TryGet(root, out ArmedForceRecord stillActive), Is.True);
        Assert.That(stillActive.IsActive, Is.True);

        Assert.That(store.TryTerminate(child, 4L, out _), Is.True);
        Assert.That(store.TryTerminate(root, 5L, out ArmedForceFoundationFailure terminateFailure), Is.True, terminateFailure.ToString());
        Assert.That(store.TryGet(root, out ArmedForceRecord terminated), Is.True);
        Assert.That(terminated.Id, Is.EqualTo(root));
        Assert.That(terminated.IsActive, Is.False);
        Assert.That(terminated.TerminatedAbsoluteDay, Is.EqualTo(5L));
        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Reused", 6L), out ArmedForceFoundationFailure duplicate), Is.False);
        Assert.That(duplicate.Code, Is.EqualTo(ArmedForceFoundationFailureCode.DuplicateForceId));
        Assert.That(store.GetRelevantPersons(root), Has.Count.EqualTo(1));
        Assert.That(store.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void FailedMutations_AreAtomicAndDoNotChangeRevisionOrState()
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        ArmedForceId root = new ArmedForceId("force-atomic");
        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Atomic", 0L), out _), Is.True);
        long revision = store.Revision;

        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Duplicate", 0L), out _), Is.False);
        Assert.That(store.Revision, Is.EqualTo(revision));
        Assert.That(store.TryGet(root, out ArmedForceRecord force), Is.True);
        Assert.That(force.DisplayName, Is.EqualTo("Atomic"));

        Assert.That(store.TryDetach(root, "elsewhere", out _), Is.False);
        Assert.That(store.Revision, Is.EqualTo(revision));
        Assert.That(store.TryGet(root, out force), Is.True);
        Assert.That(force.IsDetached, Is.False);
        Assert.That(force.OperationalLocationReference, Is.Null);
    }

    private static ArmedForceStore BuildDeterministicStore(bool reverseOrder)
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        ArmedForceId root = new ArmedForceId("force-root");
        ArmedForceId a = new ArmedForceId("force-a");
        ArmedForceId b = new ArmedForceId("force-b");
        ArmedForceId z = new ArmedForceId("force-z");

        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Root", 0L), out _), Is.True);
        if (reverseOrder)
        {
            Assert.That(store.TryRegister(new ArmedForceRecord(b, "B", 0L, root), out _), Is.True);
            Assert.That(store.TryRegister(new ArmedForceRecord(a, "A", 0L, root), out _), Is.True);
        }
        else
        {
            Assert.That(store.TryRegister(new ArmedForceRecord(a, "A", 0L, root), out _), Is.True);
            Assert.That(store.TryRegister(new ArmedForceRecord(b, "B", 0L, root), out _), Is.True);
        }

        Assert.That(store.TryRegister(new ArmedForceRecord(z, "Z", 0L, a), out _), Is.True);
        ContingentRecord contingentA = new ContingentRecord(
            new ContingentId("contingent-a"),
            root,
            2L,
            new ContingentOriginReference("pool", "a"),
            "service");
        ContingentRecord contingentZ = new ContingentRecord(
            new ContingentId("contingent-z"),
            root,
            3L,
            new ContingentOriginReference("pool", "z"),
            "service");
        if (reverseOrder)
        {
            Assert.That(store.TryRegisterContingent(contingentZ, out _), Is.True);
            Assert.That(store.TryRegisterContingent(contingentA, out _), Is.True);
        }
        else
        {
            Assert.That(store.TryRegisterContingent(contingentA, out _), Is.True);
            Assert.That(store.TryRegisterContingent(contingentZ, out _), Is.True);
        }

        return store;
    }

    private static string[] ContingentIds(IReadOnlyList<ContingentRecord> contingents)
    {
        List<string> ids = new List<string>();
        foreach (ContingentRecord contingent in contingents) ids.Add(contingent.Id.Value);
        return ids.ToArray();
    }

    private static void AssertForceIds(IReadOnlyList<ArmedForceRecord> forces, params string[] expected)
    {
        Assert.That(forces, Has.Count.EqualTo(expected.Length));
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(forces[index].Id.Value, Is.EqualTo(expected[index]));
        }
    }
}
