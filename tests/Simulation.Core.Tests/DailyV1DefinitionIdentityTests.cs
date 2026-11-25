using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

public sealed partial class DailyV1DefinitionIdentityTests
{
    [Test]
    public void WorldIdIsAUnityIndependentCanonicalIdentity()
    {
        WorldId worldId = new WorldId(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"));
        Assert.That(worldId.Value, Is.EqualTo("world:00112233445566778899aabbccddeeff"));
        Assert.That(WorldId.TryParse(worldId.Value, out WorldId parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(worldId));
        Assert.That(WorldId.TryParse("world:00112233445566778899AABBCCDDEEFF", out _), Is.False);
        Assert.That(WorldId.TryParse("world:00000000000000000000000000000000", out _), Is.False);
    }

    [Test]
    public void CompatibilityFingerprintUsesOnlyTheTypedSelectedGraphAndSuppliedRoots()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        string[] required = { "p12d.person.membership", "p12c.runtime-identities.npcs" };
        string[] empty = { "p8b.crossings" };
        string[] npcPrefixes = { "p12b.npc-life-state/" };
        string[] cityPrefixes = { "p12e.city-market-stock-rows/" };

        Assert.That(DailyV1CompatibilityFingerprint.TryCreate(definitions, "configuration/v1",
            required, empty, npcPrefixes, cityPrefixes, "p9/fingerprint", "p8/fingerprint",
            out DailyV1CompatibilityFingerprint identity), Is.True);
        Assert.That(identity.ConfigurationFingerprint, Is.EqualTo("configuration/v1"));
        Assert.That(identity.ContentFingerprint, Is.EqualTo(definitions.ContentFingerprint));
        Assert.That(identity.ProviderFingerprint, Is.EqualTo(DailyV1DefinitionIdentity.CreateProviderFingerprint(
            required, empty, npcPrefixes, cityPrefixes)));
        Assert.That(identity.CalendarFingerprint, Is.EqualTo(
            DailyV1DefinitionIdentity.CreateCalendarFingerprint(definitions.Calendar)));
        Assert.That(identity.P9Fingerprint, Is.EqualTo("p9/fingerprint"));
        Assert.That(identity.P8Fingerprint, Is.EqualTo("p8/fingerprint"));

        Assert.That(DailyV1CompatibilityFingerprint.TryCreate(definitions, " ",
            required, empty, npcPrefixes, cityPrefixes, "p9/fingerprint", "p8/fingerprint", out _), Is.False);
        Assert.That(DailyV1CompatibilityFingerprint.TryCreate(definitions, "configuration/v1",
            required, empty, npcPrefixes, cityPrefixes, "p9/fingerprint", " ", out _), Is.False);

        DailyV1HostEnvironmentIdentity host = new DailyV1HostEnvironmentIdentity(1,
            ".NET 8.0", "8.0.1", "Windows", "WIN-X64", "X64", "net8.0",
            new[] { new P12AAssemblyIdentityV2("Simulation.Core", "1.0",
                "00000000-0000-0000-0000-000000000001") }, "numeric", 1);
        P12ACompatibilityDescriptorV2 descriptor = identity.CreateDotNetV2Descriptor(host);
        Assert.That(descriptor.HostKind, Is.EqualTo(1));
        Assert.That(descriptor.RuntimeVersion, Is.EqualTo("8.0.1.0"));
        Assert.That(descriptor.ProcessArchitecture, Is.EqualTo("x64"));
        Assert.That(descriptor.ConfigurationFingerprint, Is.EqualTo(identity.ConfigurationFingerprint));
        Assert.That(descriptor.ContentFingerprint, Is.EqualTo(identity.ContentFingerprint));
        Assert.That(descriptor.ProviderFingerprint, Is.EqualTo(identity.ProviderFingerprint));
        Assert.That(descriptor.CalendarFingerprint, Is.EqualTo(identity.CalendarFingerprint));
        Assert.That(descriptor.P9Fingerprint, Is.EqualTo(identity.P9Fingerprint));
        Assert.That(descriptor.P8Fingerprint, Is.EqualTo(identity.P8Fingerprint));
    }

    [Test]
    public void P9IdentityMatchesCompleteUnityOrderedRecordVector()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CORE_F_WP1_VALUES_BASELINE.txt");
        string[] lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        string expected = Value(lines, "p9");
        int expectedCount = int.Parse(Value(lines, "p9-record-count"), System.Globalization.CultureInfo.InvariantCulture);
        List<string> records = new List<string>();
        foreach (string line in lines)
            if (line.StartsWith("p9-record=", StringComparison.Ordinal))
                records.Add(Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring("p9-record=".Length))));

        Assert.That(records, Has.Count.EqualTo(expectedCount));
        Assert.That(DailyV1DefinitionIdentity.CreateP9Fingerprint(records), Is.EqualTo(expected));
    }

    [Test]
    public void FixedCapturedTypedConstructorsReproduceEveryP9RecordAndFingerprint()
    {
        string[] lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CORE_F_WP1_VALUES_BASELINE.txt"), new UTF8Encoding(false, true));
        List<string> expectedRecords = new List<string>();
        foreach (string line in lines)
            if (line.StartsWith("p9-record=", StringComparison.Ordinal))
                expectedRecords.Add(Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring("p9-record=".Length))));

        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        string fingerprint = DailyV1DefinitionIdentity.CreateP9Fingerprint(definitions, out IReadOnlyList<string> actualRecords);

        Assert.That(actualRecords, Is.EqualTo(expectedRecords));
        Assert.That(actualRecords, Has.Count.EqualTo(184));
        Assert.That(fingerprint, Is.EqualTo(Value(lines, "p9")));
        Assert.That(definitions.ContentFingerprint, Is.EqualTo(Value(lines, "p12-content")));
    }

    private static string Value(IEnumerable<string> lines, string key)
    {
        string prefix = key + "=";
        foreach (string line in lines)
            if (line.StartsWith(prefix, StringComparison.Ordinal)) return line.Substring(prefix.Length);
        throw new InvalidDataException("The retained Unity vector lacks " + key + ".");
    }
}
