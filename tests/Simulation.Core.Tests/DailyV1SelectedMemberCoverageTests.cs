using System;
using System.IO;
using NUnit.Framework;

public sealed class DailyV1SelectedMemberCoverageTests
{
    [Test]
    public void EveryApprovedSourceMemberHasATypedCoreValueProperty()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CORE_F_SELECTED_VALUE_MEMBERS.csv");
        string adapterPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "DailyV1DefinitionAdapter.source.cs");
        string adapter = File.ReadAllText(adapterPath);
        string[] rows = File.ReadAllLines(path);
        Assert.That(rows, Has.Length.EqualTo(165));
        int checkedMembers = 0;
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');
            Assert.That(columns.Length, Is.GreaterThanOrEqualTo(3), "CSV row " + (i + 1));
            string owner = columns[0];
            string member = columns[1];
            Type ownerType = typeof(DailyV1SimulationConfigData).Assembly.GetType("DailyV1" + owner);
            Assert.That(ownerType, Is.Not.Null, "No typed Core owner exists for " + owner);
            Assert.That(ownerType.GetProperty(member), Is.Not.Null,
                "Typed value missing for approved source member " + owner + "." + member);
            string method = ConverterName(owner);
            string body = MethodBody(adapter, method);
            Assert.That(body, Does.Contain("\"" + member + "\""),
                "Unity adapter missing the approved source member mapping " + owner + "." + member);
            checkedMembers++;
        }
        Assert.That(checkedMembers, Is.EqualTo(164));
    }

    private static string ConverterName(string sourceOwner)
    {
        return "Convert" + sourceOwner;
    }

    private static string MethodBody(string source, string methodName)
    {
        int signature = source.IndexOf(methodName + "(object source)", StringComparison.Ordinal);
        Assert.That(signature, Is.GreaterThanOrEqualTo(0), "Missing adapter method " + methodName);
        int open = source.IndexOf('{', signature);
        Assert.That(open, Is.GreaterThanOrEqualTo(0), "Missing body for adapter method " + methodName);
        int depth = 0;
        for (int index = open; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0) return source.Substring(open, index - open + 1);
        }
        Assert.Fail("Unclosed adapter method " + methodName);
        return string.Empty;
    }
}
