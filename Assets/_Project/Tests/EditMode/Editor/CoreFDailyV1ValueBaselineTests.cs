using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Run on the pre-refactor Unity checkout and retain the emitted values as WP1 parity vectors.
/// This deliberately captures effective configuration and both legacy identity paths separately.
/// </summary>
public sealed class CoreFDailyV1ValueBaselineTests
{
    [Test]
    public void CaptureDailyV1ResolvedValueAndFingerprintBaseline()
    {
        string[] paths = AssetDatabase.FindAssets("t:SimulationConfigData", new[] { "Assets/_Project/Data/Simulations" });
        Assert.That(paths, Has.Length.EqualTo(1), "The selected proving profile must be unambiguous.");
        SimulationConfigData authoring = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            AssetDatabase.GUIDToAssetPath(paths[0]));
        Assert.That(authoring, Is.Not.Null);
        Assert.That(authoring.simulationName, Is.EqualTo("Daily v1"));

        SimulationConfigurationResolutionResult resolution = SimulationConfigurationResolver.Resolve(
            null, null, authoring.CreateConfigurationOverrides());
        Assert.That(resolution.IsValid, Is.True, string.Join("; ", resolution.Validation.Errors));
        CalendarDefinition calendar = CalendarDefinition.CreateValidatedOrDefault(authoring.calendar, out string diagnostic);
        Assert.That(string.IsNullOrEmpty(diagnostic), Is.True, diagnostic);

        string p9 = SimulationGenesisPipeline.CreateFingerprint(authoring, resolution.Configuration, calendar, out _);
        MethodInfo catalogMethod = typeof(P12ACompatibilityDescriptorFactory).GetMethod(
            "FingerprintContentCatalog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(catalogMethod, Is.Not.Null);
        string p12 = (string)catalogMethod.Invoke(null, new object[] { authoring });

        Debug.Log("CORE_F_WP1_VALUES_BASELINE|profile=" + AssetDatabase.GUIDToAssetPath(paths[0])
            + "|p9=" + p9 + "|p12Content=" + p12
            + "|cities=" + authoring.Cities.Count
            + "|npcs=" + authoring.Npcs.Count
            + "|actions=" + authoring.Actions.Count
            + "|statuses=" + authoring.Statuses.Count
            + "|jobs=" + authoring.Jobs.Count
            + "|calendar=" + calendar.monthsPerYear + "," + calendar.weeksPerMonth + "," + calendar.daysPerWeek
            + "|effective=" + resolution.Configuration.Population.RepresentationMode + ","
            + resolution.Configuration.Population.DecisionScope + ","
            + resolution.Configuration.Economy.Enabled + ","
            + resolution.Configuration.Travel.TravelCostPerDay.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }
}
