using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public sealed class SimulationLoggerHostPortTests
{
    [Test]
    public void SharedLoggerPreservesLegacyReportAndOutputText()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "simulation-logger-legacy-" + Guid.NewGuid().ToString("N"));
        List<KeyValuePair<DailyV1LogSeverity, string>> output = new List<KeyValuePair<DailyV1LogSeverity, string>>();
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings(), root,
            (severity, message) => output.Add(new KeyValuePair<DailyV1LogSeverity, string>(severity, message)));

        logger.BeginSimulation("Legacy", new[] { "Module" }, 1, 2);
        logger.Log(SimulationLogCategory.Trade, "trade entry");
        logger.LogWarning("warning entry");
        logger.LogError("error entry");

        Assert.That(logger.FullLog, Is.EqualTo(string.Join(Environment.NewLine, new[]
        {
            "SIMULATION: Legacy", string.Empty, "Modules:", "- Module", "Cities: 1", "NPCs: 2",
            string.Empty, "trade entry", "[WARNING] warning entry", "[ERROR] error entry"
        })));
        Assert.That(output, Is.EqualTo(new[]
        {
            new KeyValuePair<DailyV1LogSeverity, string>(DailyV1LogSeverity.Log, "trade entry"),
            new KeyValuePair<DailyV1LogSeverity, string>(DailyV1LogSeverity.Warning, "warning entry"),
            new KeyValuePair<DailyV1LogSeverity, string>(DailyV1LogSeverity.Error, "error entry")
        }));
    }

    [Test]
    public void KeyedOccurrenceReplayDoesNotDeliverTheHostOutputTwice()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "simulation-logger-replay-" + Guid.NewGuid().ToString("N"));
        int outputCount = 0;
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings(), root,
            (severity, message) => outputCount++);

        Assert.That(logger.TryPrepareKeyedOccurrence("operation-1", "fingerprint-1",
            SimulationLogCategory.Trade, "trade receipt", out LoggerKeyedOccurrenceCommit first), Is.True);
        Assert.That(first.TryCommit(out TimelineFailure firstFailure), Is.True);
        Assert.That(firstFailure, Is.EqualTo(TimelineFailure.None));
        Assert.That(logger.TryPrepareKeyedOccurrence("operation-1", "fingerprint-1",
            SimulationLogCategory.Trade, "trade receipt", out LoggerKeyedOccurrenceCommit replay), Is.True);
        Assert.That(replay.TryCommit(out TimelineFailure replayFailure), Is.True);
        Assert.That(replayFailure, Is.EqualTo(TimelineFailure.None));

        Assert.That(outputCount, Is.EqualTo(1));
        Assert.That(logger.FullLog, Is.EqualTo("trade receipt"));
    }

    [Test]
    public void ThrowingHostOutputDoesNotEscapeOrUndoCommittedReportAndReceipt()
    {
        string root = Path.Combine(TestContext.CurrentContext.WorkDirectory,
            "simulation-logger-throwing-sink-" + Guid.NewGuid().ToString("N"));
        int outputCalls = 0;
        SimulationLogger logger = new SimulationLogger(new SimulationLogSettings(), root,
            (severity, message) => { outputCalls++; throw new IOException("host sink failed"); });

        Assert.DoesNotThrow(() => logger.Log(SimulationLogCategory.Trade, "trade entry"));
        Assert.That(logger.TryPrepareKeyedOccurrence("operation-1", "fingerprint-1",
            SimulationLogCategory.Trade, "receipt entry", out LoggerKeyedOccurrenceCommit commit), Is.True);
        Assert.DoesNotThrow(() => Assert.That(commit.TryCommit(out TimelineFailure failure), Is.True));
        Assert.That(logger.FullLog, Is.EqualTo(string.Join(Environment.NewLine,
            new[] { "trade entry", "receipt entry" })));
        Assert.That(outputCalls, Is.EqualTo(2));
    }

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
