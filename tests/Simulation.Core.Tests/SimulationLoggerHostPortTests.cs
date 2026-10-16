using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public sealed class SimulationLoggerHostPortTests
{
    [Test]
    public void HostOutputAndPersistentRootAreExplicitPorts()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "simulation-logger-host-" + Guid.NewGuid().ToString("N"));
        List<KeyValuePair<DailyV1LogSeverity, string>> output = new List<KeyValuePair<DailyV1LogSeverity, string>>();
        try
        {
            SimulationLogger logger = new SimulationLogger(
                new SimulationLogSettings { trade = true }, root,
                (severity, message) => output.Add(new KeyValuePair<DailyV1LogSeverity, string>(severity, message)));

            logger.Log(SimulationLogCategory.Trade, "trade entry");
            logger.LogWarning("warning entry");
            logger.LogError("error entry");

            string saved = logger.SaveToFile("host-port-test");
            string expected = Path.Combine(root, "SimulationLogs", "host-port-test.txt");
            string contents = File.ReadAllText(saved);
            Assert.That(saved, Is.EqualTo(expected));
            Assert.That(contents, Does.Contain("trade entry"));
            Assert.That(contents, Does.Contain("[WARNING] warning entry"));
            Assert.That(contents, Does.Contain("[ERROR] error entry"));
            Assert.That(output.Exists(item => item.Key == DailyV1LogSeverity.Log && item.Value == "trade entry"), Is.True);
            Assert.That(output.Exists(item => item.Key == DailyV1LogSeverity.Warning && item.Value == "warning entry"), Is.True);
            Assert.That(output.Exists(item => item.Key == DailyV1LogSeverity.Error && item.Value == "error entry"), Is.True);
            Assert.That(output.Exists(item => item.Key == DailyV1LogSeverity.Log
                && item.Value == "Diario salvo em: " + expected), Is.True);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public void HostLoggerRequiresAnExplicitRootAndOutputSink()
    {
        Assert.Throws<ArgumentException>(() => new SimulationLogger(new SimulationLogSettings(), " ", (severity, message) => { }));
        Assert.Throws<ArgumentNullException>(() => new SimulationLogger(new SimulationLogSettings(), Path.GetTempPath(), null));
    }
}
