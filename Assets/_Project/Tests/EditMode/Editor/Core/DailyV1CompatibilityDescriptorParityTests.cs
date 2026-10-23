using System.Globalization;
using NUnit.Framework;

public sealed class DailyV1CompatibilityDescriptorParityTests
{
    [Test]
    public void CoreDotNetIdentityUsesTheExistingP8SnapshotFingerprintBytes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        var host = new DailyV1HostEnvironmentIdentity(1, ".NET 8.0", "8.0.1.0", "Windows",
            "win-x64", "x64", "net8.0",
            new[] { new P12AAssemblyIdentityV2("Simulation.Core", "1.0.0.0",
                "00000000-0000-0000-0000-000000000001") }, "ieee754-binary64", 1);

        Assert.That(DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
            definitions, host, out P12ACompatibilityDescriptorV2 descriptor,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SimulationConfigData config = definitions.Configuration;
        decimal distance = decimal.Parse(config.authoredDistancePerNeighborStep,
            NumberStyles.Number, CultureInfo.InvariantCulture);
        var p8Snapshot = new P12CSpatialAuthoritySnapshot(
            P12CSpatialAuthoritySnapshot.CurrentSchemaId,
            P12CSpatialAuthoritySnapshot.CurrentSchemaVersion,
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            definitions.GeographyProfileContractIdentity,
            2,
            1L,
            definitions.HexCoordinateConvention,
            definitions.HexCoordinateCanonicalOrder,
            new[] { new P12CSpatialHexSnapshot(config.authoredHexId,
                config.authoredHexQ, config.authoredHexR,
                config.authoredTerrainDefinitionId, config.authoredTerrainRevisionToken) },
            new[] { new P12CSpatialLocationSnapshot(config.authoredLocationId, config.authoredHexId) },
            new P12CSpatialScaleSnapshot(config.authoredScaleConventionId,
                config.authoredScaleSourceIdentity, config.authoredScaleSourceVersion,
                distance, config.authoredScaleUnit));

        Assert.That(P12ACSpatialAuthorityCodec.TryComputeFingerprint(
            p8Snapshot, out string expectedP8Fingerprint), Is.True);
        Assert.That(descriptor.P8Fingerprint, Is.EqualTo(expectedP8Fingerprint));
    }
}
