using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class ContingentManpowerCensusTests
{
    [Test]
    public void WitnessTracksInstalledStateOwnerAndSameCardinalityChanges()
    {
        ArmedForceStore forces = new ArmedForceStore(new PersonStore());
        ArmedForceId forceId = new ArmedForceId("manpower-census-force");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "Census", 0L), out _), Is.True);

        ManpowerSourceId sourceA = new ManpowerSourceId("manpower-census-source-a");
        ManpowerSourceId sourceB = new ManpowerSourceId("manpower-census-source-b");
        TestSourceProvider sources = new TestSourceProvider();
        sources.Set(sourceA, "source-a:v1");
        sources.Set(sourceB, "source-b:v1");
        ContingentManpowerStateStore owner = new ContingentManpowerStateStore(forces, sources);
        ContingentManpowerCensusProvider provider = new ContingentManpowerCensusProvider(owner);

        AssertWitness(provider, owner, 0, 0L);
        ContingentId contingentId = new ContingentId("manpower-census-contingent");
        Assert.That(owner.TryRegisterContingent(new ContingentRecord(
            contingentId,
            forceId,
            0L,
            new ContingentOriginReference("census", "fixture"),
            "service"), out _), Is.True);
        AssertWitness(provider, owner, 1, 1L);

        Assert.That(owner.TrySetSourceBinding(contingentId, sourceA, 0L, "source-a:v1", out _), Is.True);
        AssertWitness(provider, owner, 1, 2L);

        Assert.That(owner.TrySetSourceBinding(contingentId, sourceB, 1L, "source-b:v1", out _), Is.True);
        AssertWitness(provider, owner, 1, 3L);

        Assert.That(owner.TrySetSourceBinding(contingentId, sourceB, 2L, "source-b:v1", out _), Is.True,
            "Reapplying the current source binding is a no-op.");
        AssertWitness(provider, owner, 1, 3L);

        Assert.That(owner.TrySetSourceBinding(contingentId, sourceA, 1L, "source-a:v1", out _), Is.False,
            "A stale contingent revision is rejected.");
        AssertWitness(provider, owner, 1, 3L);
    }

    private static void AssertWitness(
        ContingentManpowerCensusProvider provider,
        ContingentManpowerStateStore owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.SectionId, Is.EqualTo(ContingentManpowerCensusProvider.SectionId));
        Assert.That(witness.SchemaVersion, Is.EqualTo(ContingentManpowerCensusProvider.SchemaVersion));
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private sealed class TestSourceProvider : IManpowerSourceSnapshotProvider
    {
        private readonly Dictionary<string, ManpowerSourceCapacitySnapshot> snapshots =
            new Dictionary<string, ManpowerSourceCapacitySnapshot>(StringComparer.Ordinal);

        public bool TryGetSnapshot(ManpowerSourceId sourceId, out ManpowerSourceCapacitySnapshot snapshot)
        {
            snapshot = null;
            return sourceId != null && snapshots.TryGetValue(sourceId.Value, out snapshot);
        }

        public void Set(ManpowerSourceId sourceId, string fingerprint)
        {
            snapshots[sourceId.Value] = new ManpowerSourceCapacitySnapshot(sourceId, 0L, 0L, fingerprint);
        }
    }
}
