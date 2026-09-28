using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class IdentitySequenceSnapshotTests
{
    private static readonly string[] FamilyIds =
    {
        "npc", "city", "location", "route", "event", "directive", "decision",
        "travel-party", "organization", "site", "expedition", "local-place",
        "local-connection", "notable-item"
    };

    private static readonly Func<RuntimeIdAllocator, string>[] Allocators =
    {
        allocator => allocator.AllocateNpcId(),
        allocator => allocator.AllocateCityId(),
        allocator => allocator.AllocateLocationId(),
        allocator => allocator.AllocateRouteId(),
        allocator => allocator.AllocateEventId(),
        allocator => allocator.AllocateDirectiveId(),
        allocator => allocator.AllocateDecisionId(),
        allocator => allocator.AllocateTravelPartyId(),
        allocator => allocator.AllocateOrganizationId(),
        allocator => allocator.AllocateExplorableSiteId(),
        allocator => allocator.AllocateExpeditionId(),
        allocator => allocator.AllocateLocalPlaceId(),
        allocator => allocator.AllocateLocalConnectionId(),
        allocator => allocator.AllocateNotableItemId()
    };

    [Test]
    public void RuntimeIdAllocatorSnapshot_RestoresAllFourteenFamiliesAtExactNextValues()
    {
        RuntimeIdAllocator untouched = new RuntimeIdAllocator();
        AssertNextIds(TryRestore(untouched.CaptureSnapshot()), 1L);

        RuntimeIdAllocator advanced = new RuntimeIdAllocator();
        for (int familyIndex = 0; familyIndex < FamilyIds.Length; familyIndex++)
        {
            Allocators[familyIndex](advanced);
            Allocators[familyIndex](advanced);
        }

        RuntimeIdAllocatorSnapshot advancedSnapshot = advanced.CaptureSnapshot();
        RuntimeIdAllocator advancedRestored = TryRestore(advancedSnapshot);
        AssertNextIds(advancedRestored, 3L);
        AssertNextIds(advanced, 3L);

        RuntimeIdAllocatorSnapshot gappedSnapshot = CreateAllocatorSnapshot(index => 41L + index);
        RuntimeIdAllocator gappedOwner = TryRestore(gappedSnapshot);
        RuntimeIdAllocatorSnapshot recapturedGappedSnapshot = gappedOwner.CaptureSnapshot();
        RuntimeIdAllocator gappedRestored = TryRestore(recapturedGappedSnapshot);
        AssertNextIds(gappedRestored, 41L, useFamilyOffsets: true);
        AssertNextIds(gappedOwner, 41L, useFamilyOffsets: true);

        Assert.That(recapturedGappedSnapshot.Counters.Count, Is.EqualTo(14));
        for (int index = 0; index < FamilyIds.Length; index++)
        {
            Assert.That(recapturedGappedSnapshot.Counters[index].FamilyId, Is.EqualTo(FamilyIds[index]));
            Assert.That(recapturedGappedSnapshot.Counters[index].NextSequence, Is.EqualTo(41L + index));
        }
    }

    [Test]
    public void RuntimeIdAllocatorSnapshot_IsDetachedAndImmutable()
    {
        List<RuntimeIdAllocatorCounterSnapshot> source = CreateCounters(_ => 7L);
        RuntimeIdAllocatorSnapshot snapshot = new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            source);

        source[0] = new RuntimeIdAllocatorCounterSnapshot("npc", 99L);
        Assert.That(snapshot.Counters[0].NextSequence, Is.EqualTo(7L));
        Assert.Throws<NotSupportedException>(() => ((IList<RuntimeIdAllocatorCounterSnapshot>)snapshot.Counters)[0] = null);
    }

    [Test]
    public void RuntimeIdAllocatorSnapshot_RejectsMalformedFamilySetsAndCountersWithoutMutatingAnActiveOwner()
    {
        RuntimeIdAllocator active = new RuntimeIdAllocator();
        active.AllocateNpcId();
        RuntimeIdAllocatorSnapshot valid = active.CaptureSnapshot();
        List<RuntimeIdAllocatorSnapshot> invalidSnapshots = new List<RuntimeIdAllocatorSnapshot>
        {
            null,
            new RuntimeIdAllocatorSnapshot("unknown-schema", 1, CreateCounters(_ => 1L)),
            new RuntimeIdAllocatorSnapshot(RuntimeIdAllocatorSnapshot.CurrentSchemaId, 2, CreateCounters(_ => 1L)),
            new RuntimeIdAllocatorSnapshot(RuntimeIdAllocatorSnapshot.CurrentSchemaId, 1, null),
            CreateSnapshotWithoutFamily(valid, "route"),
            CreateSnapshotWithCounter(valid, new RuntimeIdAllocatorCounterSnapshot("future-family", 1L)),
            CreateSnapshotWithDuplicateFamily(valid, "npc"),
            CreateSnapshotWithCounter(valid, null),
            CreateSnapshotWithCounter(valid, new RuntimeIdAllocatorCounterSnapshot("", 1L)),
            CreateSnapshotWithCounter(valid, new RuntimeIdAllocatorCounterSnapshot("city", 0L)),
            CreateSnapshotWithCounter(valid, new RuntimeIdAllocatorCounterSnapshot("city", -4L)),
            CreateSnapshotWithCounter(valid, new RuntimeIdAllocatorCounterSnapshot("city", long.MaxValue))
        };

        foreach (RuntimeIdAllocatorSnapshot invalid in invalidSnapshots)
        {
            Assert.That(TryRestore(invalid, out RuntimeIdAllocator restored, out string diagnostic), Is.False, diagnostic);
            Assert.That(restored, Is.Null);
            Assert.That(diagnostic, Is.Not.Empty);
        }

        Assert.That(active.AllocateNpcId(), Is.EqualTo("npc-000002"));
        Assert.That(active.AllocateCityId(), Is.EqualTo("city-000001"));
    }

    [Test]
    public void SimulationRecordSequenceSnapshot_RestoresExactNextValueAndRejectsInvalidValues()
    {
        SimulationRecordSequence active = new SimulationRecordSequence();
        Assert.That(active.Allocate(), Is.EqualTo(1L));
        Assert.That(active.Allocate(), Is.EqualTo(2L));

        SimulationRecordSequence restored = TryRestore(active.CaptureSnapshot());
        Assert.That(restored.Allocate(), Is.EqualTo(3L));
        Assert.That(active.Allocate(), Is.EqualTo(3L));

        SimulationRecordSequence gapped = TryRestore(new SimulationRecordSequenceSnapshot(
            SimulationRecordSequenceSnapshot.CurrentSchemaId,
            SimulationRecordSequenceSnapshot.CurrentSchemaVersion,
            19L));
        SimulationRecordSequenceSnapshot recaptured = gapped.CaptureSnapshot();
        Assert.That(recaptured.NextSequence, Is.EqualTo(19L));
        Assert.That(TryRestore(recaptured).Allocate(), Is.EqualTo(19L));

        SimulationRecordSequence unchanged = new SimulationRecordSequence();
        unchanged.Allocate();
        SimulationRecordSequenceSnapshot[] invalidSnapshots =
        {
            null,
            new SimulationRecordSequenceSnapshot("unknown-schema", 1, 1L),
            new SimulationRecordSequenceSnapshot(SimulationRecordSequenceSnapshot.CurrentSchemaId, 2, 1L),
            new SimulationRecordSequenceSnapshot(SimulationRecordSequenceSnapshot.CurrentSchemaId, 1, 0L),
            new SimulationRecordSequenceSnapshot(SimulationRecordSequenceSnapshot.CurrentSchemaId, 1, -1L),
            new SimulationRecordSequenceSnapshot(SimulationRecordSequenceSnapshot.CurrentSchemaId, 1, long.MaxValue)
        };

        foreach (SimulationRecordSequenceSnapshot invalid in invalidSnapshots)
        {
            Assert.That(TryRestore(invalid, out SimulationRecordSequence failed, out string diagnostic), Is.False, diagnostic);
            Assert.That(failed, Is.Null);
            Assert.That(diagnostic, Is.Not.Empty);
        }

        Assert.That(unchanged.Allocate(), Is.EqualTo(2L));
    }

    private static void AssertNextIds(RuntimeIdAllocator allocator, long firstNextSequence, bool useFamilyOffsets = false)
    {
        for (int index = 0; index < FamilyIds.Length; index++)
        {
            long expectedSequence = firstNextSequence + (useFamilyOffsets ? index : 0L);
            string expectedId = FamilyIds[index] + "-" + expectedSequence.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(Allocators[index](allocator), Is.EqualTo(expectedId), FamilyIds[index]);
        }
    }

    private static RuntimeIdAllocatorSnapshot CreateAllocatorSnapshot(Func<int, long> nextValue)
    {
        return new RuntimeIdAllocatorSnapshot(
            RuntimeIdAllocatorSnapshot.CurrentSchemaId,
            RuntimeIdAllocatorSnapshot.CurrentSchemaVersion,
            CreateCounters(nextValue));
    }

    private static List<RuntimeIdAllocatorCounterSnapshot> CreateCounters(Func<int, long> nextValue)
    {
        List<RuntimeIdAllocatorCounterSnapshot> counters = new List<RuntimeIdAllocatorCounterSnapshot>();
        for (int index = 0; index < FamilyIds.Length; index++)
        {
            counters.Add(new RuntimeIdAllocatorCounterSnapshot(FamilyIds[index], nextValue(index)));
        }

        return counters;
    }

    private static RuntimeIdAllocatorSnapshot CreateSnapshotWithoutFamily(RuntimeIdAllocatorSnapshot source, string omittedFamily)
    {
        List<RuntimeIdAllocatorCounterSnapshot> counters = new List<RuntimeIdAllocatorCounterSnapshot>();
        foreach (RuntimeIdAllocatorCounterSnapshot counter in source.Counters)
        {
            if (counter.FamilyId != omittedFamily)
            {
                counters.Add(counter);
            }
        }

        return new RuntimeIdAllocatorSnapshot(source.SchemaId, source.SchemaVersion, counters);
    }

    private static RuntimeIdAllocatorSnapshot CreateSnapshotWithCounter(
        RuntimeIdAllocatorSnapshot source,
        RuntimeIdAllocatorCounterSnapshot replacement)
    {
        List<RuntimeIdAllocatorCounterSnapshot> counters = new List<RuntimeIdAllocatorCounterSnapshot>(source.Counters);
        if (replacement != null && string.IsNullOrWhiteSpace(replacement.FamilyId) == false)
        {
            for (int index = 0; index < counters.Count; index++)
            {
                if (counters[index].FamilyId == replacement.FamilyId)
                {
                    counters.RemoveAt(index);
                    break;
                }
            }
        }

        counters.Add(replacement);
        return new RuntimeIdAllocatorSnapshot(source.SchemaId, source.SchemaVersion, counters);
    }

    private static RuntimeIdAllocatorSnapshot CreateSnapshotWithDuplicateFamily(
        RuntimeIdAllocatorSnapshot source,
        string duplicatedFamily)
    {
        List<RuntimeIdAllocatorCounterSnapshot> counters = new List<RuntimeIdAllocatorCounterSnapshot>(source.Counters);
        counters.Add(new RuntimeIdAllocatorCounterSnapshot(duplicatedFamily, 1L));
        return new RuntimeIdAllocatorSnapshot(source.SchemaId, source.SchemaVersion, counters);
    }

    private static RuntimeIdAllocator TryRestore(RuntimeIdAllocatorSnapshot snapshot)
    {
        Assert.That(TryRestore(snapshot, out RuntimeIdAllocator allocator, out string diagnostic), Is.True, diagnostic);
        return allocator;
    }

    private static bool TryRestore(
        RuntimeIdAllocatorSnapshot snapshot,
        out RuntimeIdAllocator allocator,
        out string diagnostic)
    {
        MethodInfo factory = typeof(RuntimeIdAllocator).GetMethod(
            "TryCreateStagedFromSnapshot",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(factory, Is.Not.Null);

        object[] arguments = { snapshot, null, null };
        bool result = (bool)factory.Invoke(null, arguments);
        allocator = arguments[1] as RuntimeIdAllocator;
        diagnostic = arguments[2] as string;
        return result;
    }

    private static SimulationRecordSequence TryRestore(SimulationRecordSequenceSnapshot snapshot)
    {
        Assert.That(TryRestore(snapshot, out SimulationRecordSequence sequence, out string diagnostic), Is.True, diagnostic);
        return sequence;
    }

    private static bool TryRestore(
        SimulationRecordSequenceSnapshot snapshot,
        out SimulationRecordSequence sequence,
        out string diagnostic)
    {
        MethodInfo factory = typeof(SimulationRecordSequence).GetMethod(
            "TryCreateStagedFromSnapshot",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(factory, Is.Not.Null);

        object[] arguments = { snapshot, null, null };
        bool result = (bool)factory.Invoke(null, arguments);
        sequence = arguments[1] as SimulationRecordSequence;
        diagnostic = arguments[2] as string;
        return result;
    }
}
