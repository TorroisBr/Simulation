using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

public sealed partial class DailyV1DefinitionIdentityTests
{
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
