using System;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12RestoreStageTransactionTests
{
    [Test]
    public void CoreRestoreStagesAreBoundToOneApplicationRootAndOrderedAttempt()
    {
        DailyV1Application application = new DailyV1Application();
        P12AContinuationImage image = CreateImage();
        DailyV1P12RestoreStageTransaction transaction = new DailyV1P12RestoreStageTransaction(
            application, image, null, null);
        P12CStagedCoreContinuationRoot roots = CreateRoots();

        Assert.That(transaction.SourceImage, Is.SameAs(image));
        Assert.That(transaction.TryBindRoots(roots), Is.True);
        Assert.That(transaction.TryBindRoots(CreateRoots()), Is.False,
            "One restore attempt cannot replace its staged causal roots.");
        Assert.That(new DailyV1P12RestoreStageTransaction(
            application, image, null, null).TryBindRoots(roots), Is.False,
            "The exact Core roots cannot be claimed by a second restore attempt.");

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(transaction.TryBeginP12D(new DailyV1Application(), roots, definitions, null, null), Is.Null,
            "A different application cannot continue the attempt.");
        Assert.That(transaction.TryBeginP12D(application, CreateRoots(), definitions, null, null), Is.Null,
            "A different C root cannot continue the attempt.");

        DailyV1P12RestoreStageTransaction.Step firstAttempt = transaction.TryBeginP12D(
            application, roots, definitions, null, null);
        Assert.That(firstAttempt, Is.Not.Null);
        Assert.That(transaction.TryBeginP12D(application, roots, definitions, null, null), Is.Null,
            "A stage cannot be entered twice while its first attempt is active.");
        Assert.That(transaction.TryBeginP12E(application, roots, new PersonStore(),
            Array.Empty<DailyV1CityRuntimeOwners>(), Array.Empty<DailyV1NpcRuntimeOwners>(),
            0L, null, null), Is.Null,
            "P12-E cannot begin before the exact P12-D stage completes.");

        firstAttempt.Dispose();
        using (DailyV1P12RestoreStageTransaction.Step retry = transaction.TryBeginP12D(
            application, roots, definitions, null, null))
        {
            Assert.That(retry, Is.Not.Null,
                "A failed private stage attempt can be retried without publishing or replacing roots.");
        }
    }

    private static P12AContinuationImage CreateImage()
    {
        P12AContinuationHeader header = new P12AContinuationHeader(
            "world:00000000000000000000000000000001", 0L, 1L, -1L,
            "p9.profile", "manifest-fingerprint", "p8.profile", "geography-fingerprint",
            0L, null);
        return new P12AContinuationImage(
            header, new P12ACausalRootPayload(null, null), Array.Empty<P12AOwnerSection>());
    }

    private static P12CStagedCoreContinuationRoot CreateRoots()
    {
        P12CP9GenesisManifestCoreSnapshot manifest = new P12CP9GenesisManifestCoreSnapshot(
            P12CP9GenesisManifestCoreSnapshot.SnapshotContractIdentity,
            P12CP9GenesisManifestCoreSnapshot.CurrentSchemaVersion,
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            "p9.manifest", 1, "manifest-fingerprint", "profile-fingerprint",
            "p9.profile", 1, null, Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<string>(), Array.Empty<string>(), 12, 4, 7,
            new[] { 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            1, "fixture", Array.Empty<string>(), "day:0");
        return new P12CStagedCoreContinuationRoot(
            true,
            new WorldId(new Guid("00000000-0000-0000-0000-000000000001")),
            new RuntimeIdAllocator(),
            new SimulationRecordSequence(),
            new SpatialAuthorityStore(),
            manifest,
            new DeterministicRandomSource(1));
    }
}
