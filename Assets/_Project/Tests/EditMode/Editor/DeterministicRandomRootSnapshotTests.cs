using System.Reflection;
using NUnit.Framework;

public sealed class DeterministicRandomRootSnapshotTests
{
    [Test]
    public void CaptureSnapshotRecordsSupportedMetadataAndExactSeed()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(-123456789);

        DeterministicRandomRootSnapshot snapshot = source.CaptureSnapshot();

        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot.SchemaId, Is.EqualTo("deterministic-random-root"));
        Assert.That(snapshot.SchemaVersion, Is.EqualTo(1));
        Assert.That(snapshot.ProviderId, Is.EqualTo("simulation/deterministic-random-source"));
        Assert.That(snapshot.ProviderVersion, Is.EqualTo(1));
        Assert.That(snapshot.AlgorithmId, Is.EqualTo("fnv1a64-keyed-utf16"));
        Assert.That(snapshot.AlgorithmVersion, Is.EqualTo(1));
        Assert.That(snapshot.Seed, Is.EqualTo(-123456789));
    }

    [Test]
    public void StagedSourcePreservesSeedAndKeyedOutputs()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(123456789);
        DeterministicRandomSource staged;
        string diagnostic;

        bool succeeded = DeterministicRandomSource.TryCreateStagedFromSnapshot(
            source.CaptureSnapshot(),
            out staged,
            out diagnostic);

        Assert.That(succeeded, Is.True);
        Assert.That(diagnostic, Is.Empty);
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged, Is.Not.SameAs(source));
        Assert.That(staged.Seed, Is.EqualTo(source.Seed));

        string[] selectedKeys =
        {
            "npc-decision|npc-root|4",
            "action-success|actor-root|decision-root|action-root|4",
            "crime-steal|thief-root|4"
        };

        for (int i = 0; i < selectedKeys.Length; i++)
        {
            string key = selectedKeys[i];
            Assert.That(staged.NextUnit(key), Is.EqualTo(source.NextUnit(key)));

            Assert.That(
                staged.NextUnit(key, 1L),
                Is.EqualTo(source.NextUnit(key, 1L)));
            Assert.That(
                staged.NextUnit(key, 37L),
                Is.EqualTo(source.NextUnit(key, 37L)));
        }
    }

    [TestCase(int.MinValue)]
    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public void StagedSourcePreservesEveryBoundarySeed(int seed)
    {
        DeterministicRandomSource source = new DeterministicRandomSource(seed);
        DeterministicRandomSource staged;
        string diagnostic;

        bool succeeded = DeterministicRandomSource.TryCreateStagedFromSnapshot(
            source.CaptureSnapshot(),
            out staged,
            out diagnostic);

        Assert.That(succeeded, Is.True, diagnostic);
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged.Seed, Is.EqualTo(seed));
    }

    [Test]
    public void UnsupportedMetadataFailsWithoutChangingActiveSource()
    {
        DeterministicRandomSource activeSource = new DeterministicRandomSource(77);
        DeterministicRandomRootSnapshot valid = activeSource.CaptureSnapshot();
        float expectedActiveValue = activeSource.NextUnit("active-source-probe", 19L);

        DeterministicRandomRootSnapshot[] unsupportedSnapshots =
        {
            null,
            WithMetadata(valid, schemaId: "other-schema"),
            WithMetadata(valid, schemaVersion: valid.SchemaVersion + 1),
            WithMetadata(valid, providerId: "other/provider"),
            WithMetadata(valid, providerVersion: valid.ProviderVersion + 1),
            WithMetadata(valid, algorithmId: "other-algorithm"),
            WithMetadata(valid, algorithmVersion: valid.AlgorithmVersion + 1)
        };

        for (int i = 0; i < unsupportedSnapshots.Length; i++)
        {
            DeterministicRandomSource staged;
            string diagnostic;
            bool succeeded = DeterministicRandomSource.TryCreateStagedFromSnapshot(
                unsupportedSnapshots[i],
                out staged,
                out diagnostic);

            Assert.That(succeeded, Is.False, "Unsupported snapshot case " + i);
            Assert.That(staged, Is.Null, "Unsupported snapshot case " + i);
            Assert.That(string.IsNullOrEmpty(diagnostic), Is.False, "Unsupported snapshot case " + i);
            Assert.That(
                activeSource.NextUnit("active-source-probe", 19L),
                Is.EqualTo(expectedActiveValue),
                "Rejected snapshot changed the active source for case " + i);
        }
    }

    [Test]
    public void SnapshotIsDetachedImmutableAndCaptureOrStagingDoesNotAdvanceSource()
    {
        DeterministicRandomSource source = new DeterministicRandomSource(9);
        float expectedValue = source.NextUnit("snapshot-probe", 12L);

        DeterministicRandomRootSnapshot snapshot = source.CaptureSnapshot();
        DeterministicRandomRootSnapshot secondSnapshot = source.CaptureSnapshot();
        DeterministicRandomSource staged;
        string diagnostic;

        bool succeeded = DeterministicRandomSource.TryCreateStagedFromSnapshot(
            snapshot,
            out staged,
            out diagnostic);

        Assert.That(succeeded, Is.True, diagnostic);
        Assert.That(snapshot, Is.Not.SameAs(secondSnapshot));
        Assert.That(snapshot.Seed, Is.EqualTo(source.Seed));
        Assert.That(secondSnapshot.Seed, Is.EqualTo(source.Seed));
        Assert.That(source.NextUnit("snapshot-probe", 12L), Is.EqualTo(expectedValue));
        Assert.That(staged.NextUnit("snapshot-probe", 12L), Is.EqualTo(expectedValue));

        PropertyInfo[] properties = typeof(DeterministicRandomRootSnapshot).GetProperties();
        Assert.That(properties.Length, Is.EqualTo(7));
        for (int i = 0; i < properties.Length; i++)
        {
            Assert.That(properties[i].CanWrite, Is.False, properties[i].Name);
        }
    }

    private static DeterministicRandomRootSnapshot WithMetadata(
        DeterministicRandomRootSnapshot snapshot,
        string schemaId = null,
        int? schemaVersion = null,
        string providerId = null,
        int? providerVersion = null,
        string algorithmId = null,
        int? algorithmVersion = null)
    {
        return new DeterministicRandomRootSnapshot(
            schemaId ?? snapshot.SchemaId,
            schemaVersion ?? snapshot.SchemaVersion,
            providerId ?? snapshot.ProviderId,
            providerVersion ?? snapshot.ProviderVersion,
            algorithmId ?? snapshot.AlgorithmId,
            algorithmVersion ?? snapshot.AlgorithmVersion,
            snapshot.Seed);
    }
}
