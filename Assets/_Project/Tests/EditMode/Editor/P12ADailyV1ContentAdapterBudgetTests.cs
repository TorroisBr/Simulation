using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class P12ADailyV1ContentAdapterBudgetTests
{
    [Test]
    public void CapturesAuthoredListAboveAssetCountWhenItsLegacyBytesFit()
    {
        SimulationConfigData configuration = ScriptableObject.CreateInstance<SimulationConfigData>();
        try
        {
            configuration.calendar.monthLengths = new List<int>(65537);
            for (int i = 0; i < 65537; i++) configuration.calendar.monthLengths.Add(i);
            Assert.That(TryCapture(configuration, out DailyV1SelectedContentProjection projection,
                out string diagnostic), Is.True, diagnostic);
            Assert.That(projection, Is.Not.Null);
            Assert.That(projection.Bytes.Length, Is.GreaterThan(1024 * 1024));
        }
        finally { UnityEngine.Object.DestroyImmediate(configuration); }
    }

    [Test]
    public void RejectsOversizedAuthoredListBeforeCreatingCapturedValueItems()
    {
        SimulationConfigData configuration = ScriptableObject.CreateInstance<SimulationConfigData>();
        try
        {
            configuration.calendar.monthLengths = new List<int>(3200000);
            for (int i = 0; i < 3200000; i++) configuration.calendar.monthLengths.Add(i);
            Assert.That(TryCapture(configuration, out DailyV1SelectedContentProjection projection,
                out string diagnostic), Is.False);
            Assert.That(projection, Is.Null);
            Assert.That(diagnostic, Does.Contain("during Unity capture"));
        }
        finally { UnityEngine.Object.DestroyImmediate(configuration); }
    }

    private static bool TryCapture(SimulationConfigData configuration,
        out DailyV1SelectedContentProjection projection, out string diagnostic)
    {
        MethodInfo method = typeof(P12ACompatibilityDescriptorFactory).Assembly
            .GetType("P12ADailyV1ContentAdapter", throwOnError: true)
            .GetMethod("TryCapture", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { configuration, null, null };
        bool result = (bool)method.Invoke(null, arguments);
        projection = arguments[1] as DailyV1SelectedContentProjection;
        diagnostic = arguments[2] as string;
        return result;
    }
}
