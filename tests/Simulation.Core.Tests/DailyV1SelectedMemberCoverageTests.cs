using System;
using System.IO;
using NUnit.Framework;

public sealed class DailyV1SelectedMemberCoverageTests
{
    [Test]
    public void EveryApprovedSourceMemberHasATypedCoreValueProperty()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "CORE_F_SELECTED_VALUE_MEMBERS.csv");
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
            checkedMembers++;
        }
        Assert.That(checkedMembers, Is.EqualTo(164));
    }
}
