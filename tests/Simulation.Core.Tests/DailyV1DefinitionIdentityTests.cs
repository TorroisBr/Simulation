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

    private static string Value(IEnumerable<string> lines, string key)
    {
        string prefix = key + "=";
        foreach (string line in lines)
            if (line.StartsWith(prefix, StringComparison.Ordinal)) return line.Substring(prefix.Length);
        throw new InvalidDataException("The retained Unity vector lacks " + key + ".");
    }
}
