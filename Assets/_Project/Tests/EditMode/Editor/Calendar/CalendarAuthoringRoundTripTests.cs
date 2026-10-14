using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CalendarAuthoringRoundTripTests
{
    private string temporaryFolder;
    private string temporaryAssetPath;

    [SetUp]
    public void SetUp()
    {
        temporaryFolder = "Assets/__CoreACalendarRoundTrip_" + Guid.NewGuid().ToString("N");
        string folderName = temporaryFolder.Substring("Assets/".Length);
        Assert.That(AssetDatabase.CreateFolder("Assets", folderName), Is.Not.Empty);
    }

    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrWhiteSpace(temporaryFolder))
        {
            AssetDatabase.DeleteAsset(temporaryFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }

    [Test]
    public void UniformCalendarDefinitionRoundTripsThroughSimulationConfigAsset()
    {
        CalendarDefinition reloaded = RoundTrip(new CalendarDefinition(12, 4, 7));

        Assert.That(reloaded.monthsPerYear, Is.EqualTo(12));
        Assert.That(reloaded.weeksPerMonth, Is.EqualTo(4));
        Assert.That(reloaded.daysPerWeek, Is.EqualTo(7));
        Assert.That(reloaded.monthLengths, Is.Empty);

        SimulationCalendar calendar = new SimulationCalendar(reloaded);
        Assert.That(calendar.SemanticIdentity, Is.EqualTo("simulation-calendar"));
        Assert.That(calendar.SemanticVersion, Is.EqualTo("12/7/336/uniform:28"));
        Assert.That(calendar.GetDate(335L).Month, Is.EqualTo(12));
        Assert.That(calendar.GetDate(335L).DayOfMonth, Is.EqualTo(28));
    }

    [Test]
    public void UnequalMonthCalendarDefinitionRoundTripsThroughSimulationConfigAsset()
    {
        CalendarDefinition reloaded = RoundTrip(new CalendarDefinition(new[] { 10, 20, 5 }, 7));

        Assert.That(reloaded.monthsPerYear, Is.EqualTo(3));
        Assert.That(reloaded.weeksPerMonth, Is.EqualTo(1));
        Assert.That(reloaded.daysPerWeek, Is.EqualTo(7));
        Assert.That(reloaded.monthLengths, Is.EqualTo(new[] { 10, 20, 5 }));

        SimulationCalendar calendar = new SimulationCalendar(reloaded);
        Assert.That(calendar.SemanticIdentity, Is.EqualTo("simulation-calendar"));
        Assert.That(calendar.SemanticVersion, Is.EqualTo("3/7/35/custom:10,20,5"));
        Assert.That(calendar.GetDate(30L).Month, Is.EqualTo(3));
        Assert.That(calendar.GetDate(30L).DayOfMonth, Is.EqualTo(1));
        Assert.That(calendar.GetAbsoluteDay(calendar.GetDate(30L)), Is.EqualTo(30L));
    }

    [Test]
    public void SelectedDailyV1AssetRetainsItsCalendarAndProfileReferences()
    {
        const string assetPath = "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset";
        SimulationConfigData profile = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(assetPath);

        Assert.That(profile, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(profile), Is.EqualTo(assetPath));
        Assert.That(profile.simulationName, Is.EqualTo("Daily v1"));
        Assert.That(profile.useAuthoredGeographyProfile, Is.True);
        Assert.That(profile.authoredHexId, Is.EqualTo("hex/sample-origin"));
        Assert.That(profile.authoredLocationId, Is.EqualTo("location/sample-origin"));
        Assert.That(profile.authoredScaleConventionId, Is.EqualTo("world-scale/Simulation-DailyV1/v1"));
        Assert.That(profile.authoredP10RuinSite, Is.Null);
        Assert.That(profile.Cities, Has.Count.EqualTo(2));
        Assert.That(profile.Npcs, Is.Not.Empty);
        foreach (NpcSimulationConfig npc in profile.Npcs)
        {
            Assert.That(npc, Is.Not.Null);
            Assert.That(npc.npc, Is.Not.Null);
            Assert.That(npc.startingCity, Is.Not.Null);
        }

        Assert.That(profile.calendar, Is.Not.Null);
        Assert.That(profile.calendar.monthsPerYear, Is.EqualTo(1));
        Assert.That(profile.calendar.weeksPerMonth, Is.EqualTo(1));
        Assert.That(profile.calendar.daysPerWeek, Is.EqualTo(1));
        Assert.That(typeof(CalendarDefinition).Assembly.GetName().Name, Is.EqualTo("Simulation.Core"));
        Assert.That(typeof(SimulationCalendar).Assembly, Is.EqualTo(typeof(CalendarDefinition).Assembly));

        Type descriptorFactory = typeof(SimulationRuntime).Assembly.GetType("P12ACompatibilityDescriptorFactory");
        MethodInfo assemblyDiscovery = descriptorFactory?.GetMethod("TryGetProjectAssemblyMvids",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(assemblyDiscovery, Is.Not.Null);
        object[] discoveryArguments = { null };
        Assert.That(assemblyDiscovery.Invoke(null, discoveryArguments), Is.EqualTo(true));
        string coreMvid = typeof(CalendarDefinition).Assembly.ManifestModule.ModuleVersionId
            .ToString("D").ToLowerInvariant();
        Assert.That((string[])discoveryArguments[0], Does.Contain(coreMvid));
    }

    private CalendarDefinition RoundTrip(CalendarDefinition definition)
    {
        SimulationConfigData authored = ScriptableObject.CreateInstance<SimulationConfigData>();
        authored.simulationName = "CORE-A calendar round-trip";
        authored.calendar = definition;
        temporaryAssetPath = temporaryFolder + "/Calendar.asset";

        AssetDatabase.CreateAsset(authored, temporaryAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(temporaryAssetPath, ImportAssetOptions.ForceSynchronousImport);

        SimulationConfigData reloaded = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(temporaryAssetPath);
        Assert.That(reloaded, Is.Not.Null);
        Assert.That(reloaded.simulationName, Is.EqualTo("CORE-A calendar round-trip"));
        Assert.That(reloaded.calendar, Is.Not.Null);
        return reloaded.calendar;
    }
}
