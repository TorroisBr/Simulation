using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ArmedForceWorldCompositionTests
{
    [Test]
    public void SimulationRuntime_ComposesArmedForceStoreAgainstAuthoritativePersonStore()
    {
        PersonStore persons = new PersonStore();
        PersonId commanderId = new PersonId("person-world-commander");
        Assert.That(persons.TryRegister(new PersonRuntime(commanderId), out _), Is.True);

        ArmedForceStore source = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("force-world");
        Assert.That(source.TryRegister(
            new ArmedForceRecord(forceId, "World Force", 0L, commanderPersonId: commanderId),
            out _), Is.True);
        Assert.That(source.TryAddRelevantPerson(
            forceId,
            commanderId,
            "commander",
            out _,
            out _), Is.True);
        Assert.That(source.TryRegisterContingent(
            new ContingentRecord(
                new ContingentId("contingent-world"),
                forceId,
                3L,
                new ContingentOriginReference("source", "world"),
                "service"),
            out _), Is.True);

        long sourceRevision = source.Revision;
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L),
            null,
            null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: source);

        Assert.That(runtime.PersonStore, Is.SameAs(persons));
        Assert.That(runtime.ArmedForceStore, Is.Not.SameAs(source));
        Assert.That(runtime.ArmedForceStore.PersonStore, Is.SameAs(runtime.PersonStore));
        Assert.That(runtime.ArmedForceStore.Revision, Is.EqualTo(sourceRevision));
        Assert.That(runtime.ArmedForceStore.TryGet(forceId, out _), Is.True);
        Assert.That(runtime.ArmedForceStore.TryGetContingent(new ContingentId("contingent-world"), out _), Is.True);

        Assert.That(source.TrySetOperationalLocation(forceId, "source-only", out _), Is.True);
        Assert.That(runtime.ArmedForceStore.TryGet(forceId, out ArmedForceRecord composedForce), Is.True);
        Assert.That(composedForce.OperationalLocationReference, Is.Null);
    }

    [Test]
    public void SimulationRuntime_RejectsArmedForcePersonReferencesAbsentFromResolvedPersonStore()
    {
        PersonStore sourcePersons = new PersonStore();
        PersonId commanderId = new PersonId("person-source-only");
        Assert.That(sourcePersons.TryRegister(new PersonRuntime(commanderId), out _), Is.True);
        ArmedForceStore source = new ArmedForceStore(sourcePersons);
        Assert.That(source.TryRegister(
            new ArmedForceRecord(
                new ArmedForceId("force-invalid-world-binding"),
                "Invalid Binding",
                0L,
                commanderPersonId: commanderId),
            out _), Is.True);

        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(0L),
            null,
            null,
            economyEnabled: false,
            personStore: new PersonStore(),
            armedForceStore: source));
    }

    [Test]
    public void SnapshotWriterAndDiff_CaptureStableArmedForceComposition()
    {
        PersonStore persons = new PersonStore();
        PersonId personId = new PersonId("person-diagnostics");
        Assert.That(persons.TryRegister(new PersonRuntime(personId), out _), Is.True);
        ArmedForceStore store = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("force-diagnostics");
        Assert.That(store.TryRegister(new ArmedForceRecord(forceId, "Diagnostic Force", 0L), out _), Is.True);
        Assert.That(store.TryAddRelevantPerson(forceId, personId, "hero", out _, out _), Is.True);
        Assert.That(store.TryRegisterContingent(
            new ContingentRecord(
                new ContingentId("contingent-diagnostics"),
                forceId,
                4L,
                new ContingentOriginReference("summoned", "reference"),
                "service",
                new[] { new ArmedForceCharacteristic("z", "last"), new ArmedForceCharacteristic("a", "first") }),
            out _), Is.True);

        WorldStateSnapshot before = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            personStore: persons,
            armedForceStore: store));
        string canonical = WorldStateCanonicalWriter.Write(before);
        Assert.That(canonical, Does.Contain("ARMED_FORCE|force-diagnostics|"));
        Assert.That(canonical, Does.Contain("ARMED_FORCE_CONTINGENT|contingent-diagnostics|"));
        Assert.That(canonical, Does.Contain("ARMED_FORCE_CONTINGENT_CHARACTERISTIC|contingent-diagnostics|a|first"));
        Assert.That(canonical, Does.Contain("ARMED_FORCE_PERSON_REFERENCE|"));
        Assert.That(before.ArmedForces, Has.Count.EqualTo(1));
        Assert.That(before.ArmedForceContingents[0].Characteristics[0].Key, Is.EqualTo("a"));
        Assert.That(before.ArmedForceRelevantPersons[0].PersonId, Is.EqualTo(personId.Value));
        Assert.That(WorldStateDiagnostics.Validate(before).IsValid, Is.True);

        Assert.That(store.TrySetOperationalLocation(forceId, "changed", out _), Is.True);
        WorldStateSnapshot after = WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            personStore: persons,
            armedForceStore: store));
        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, after);
        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(difference =>
            difference.Section == "ArmedForce"
            && difference.Identity == forceId.Value
            && difference.Field == "OperationalLocationReference"));
    }

    [Test]
    public void SnapshotAndCanonicalOutput_AreIndependentOfInsertionOrder()
    {
        WorldStateSnapshot first = BuildDeterministicSnapshot(false);
        WorldStateSnapshot second = BuildDeterministicSnapshot(true);

        Assert.That(WorldStateCanonicalWriter.Write(first), Is.EqualTo(WorldStateCanonicalWriter.Write(second)));
        Assert.That(WorldStateDiagnostics.Compare(first, second).IsEmpty, Is.True);
    }

    [Test]
    public void Diagnostics_ReportsArmedForceHierarchyAndReferenceViolations()
    {
        WorldStateSnapshot snapshot = new WorldStateSnapshot(
            0L,
            armedForces: new[]
            {
                new WorldStateArmedForceSnapshot(
                    "force-child",
                    "Child",
                    0L,
                    ArmedForceLifecycleState.Active,
                    null,
                    "force-missing",
                    false,
                    null,
                    "person-missing")
            },
            armedForceContingents: new[]
            {
                new WorldStateArmedForceContingentSnapshot(
                    "contingent-orphan",
                    "force-missing",
                    1L,
                    "source",
                    "orphan",
                    "service")
            },
            armedForceRelevantPersons: new[]
            {
                new WorldStateArmedForcePersonReferenceSnapshot(
                    "reference-orphan",
                    "force-missing",
                    "person-missing",
                    "officer")
            },
            armedForceRevision: 1L);

        WorldStateInvariantReport report = WorldStateDiagnostics.Validate(snapshot);
        Assert.That(report.HasErrors, Is.True);
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "ArmedForceParentMissing"));
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "ArmedForceCommanderPersonMissing"));
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "ArmedForceContingentForceMissing"));
        Assert.That(report.Issues, Has.Some.Matches<WorldStateInvariantIssue>(issue => issue.Code == "ArmedForcePersonReferencePersonMissing"));
    }

    [Test]
    public void AdvanceDay_DoesNotProcessOrMutateArmedForceState()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore store = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("force-no-tick");
        Assert.That(store.TryRegister(new ArmedForceRecord(forceId, "No Tick", 0L), out _), Is.True);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L),
            null,
            null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: store);
        long revision = runtime.ArmedForceStore.Revision;

        runtime.AdvanceDay();

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(runtime.ArmedForceStore.Revision, Is.EqualTo(revision));
        Assert.That(runtime.ArmedForceStore.TryGet(forceId, out ArmedForceRecord force), Is.True);
        Assert.That(force.IsActive, Is.True);
    }

    private static WorldStateSnapshot BuildDeterministicSnapshot(bool reverseOrder)
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore store = new ArmedForceStore(persons);
        ArmedForceId root = new ArmedForceId("force-snapshot-root");
        Assert.That(store.TryRegister(new ArmedForceRecord(root, "Root", 0L), out _), Is.True);
        ArmedForceRecord first = new ArmedForceRecord(
            new ArmedForceId("force-snapshot-a"), "A", 0L, root);
        ArmedForceRecord second = new ArmedForceRecord(
            new ArmedForceId("force-snapshot-b"), "B", 0L, root);
        if (reverseOrder)
        {
            Assert.That(store.TryRegister(second, out _), Is.True);
            Assert.That(store.TryRegister(first, out _), Is.True);
        }
        else
        {
            Assert.That(store.TryRegister(first, out _), Is.True);
            Assert.That(store.TryRegister(second, out _), Is.True);
        }

        ContingentRecord a = new ContingentRecord(
            new ContingentId("contingent-snapshot-a"),
            first.Id,
            2L,
            new ContingentOriginReference("origin", "a"),
            "service");
        ContingentRecord b = new ContingentRecord(
            new ContingentId("contingent-snapshot-b"),
            second.Id,
            3L,
            new ContingentOriginReference("origin", "b"),
            "service");
        if (reverseOrder)
        {
            Assert.That(store.TryRegisterContingent(b, out _), Is.True);
            Assert.That(store.TryRegisterContingent(a, out _), Is.True);
        }
        else
        {
            Assert.That(store.TryRegisterContingent(a, out _), Is.True);
            Assert.That(store.TryRegisterContingent(b, out _), Is.True);
        }

        return WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            personStore: persons,
            armedForceStore: store));
    }
}
