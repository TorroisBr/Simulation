using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

public sealed class DailyV1ProcessSeparatedP12ContinuationTests
{
    private const string ProcessHostDll = "Simulation.Core.DailyV1ProcessHost.dll";

    private sealed class ProcessResult
    {
        internal ProcessResult(Dictionary<string, string> fields, string standardOutput, string standardError)
        {
            Fields = fields;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        internal Dictionary<string, string> Fields { get; }
        internal string StandardOutput { get; }
        internal string StandardError { get; }
        internal int Int(string key) => int.Parse(Fields[key], CultureInfo.InvariantCulture);
        internal long Long(string key) => long.Parse(Fields[key], CultureInfo.InvariantCulture);
    }

    [Test]
    public void ProcessSeparatedDailyV1ContinuationMatchesThreeDayGenesisControl()
    {
        string fullTestRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "coref-process-p12-" + Guid.NewGuid().ToString("N")));
        string separatedRoot = Path.Combine(fullTestRoot, "separated");
        string controlRoot = Path.Combine(fullTestRoot, "control");
        Directory.CreateDirectory(fullTestRoot);
        try
        {
            ProcessResult dayOne = RunHost("genesis", separatedRoot, 1);
            Assert.That(dayOne.Fields["start"], Is.EqualTo("NoSave"));
            Assert.That(dayOne.Fields["profile"], Is.EqualTo("UnityBootstrap-Daily-v1"));
            Assert.That(dayOne.Long("day"), Is.GreaterThanOrEqualTo(1L));
            Assert.That(dayOne.Int("sections"), Is.EqualTo(299),
                "The process must persist the exact 299 registered owner section records for this N=10, C=2 profile.");
            Assert.That(dayOne.Int("owners"), Is.GreaterThan(0),
                "The selected profile must preserve materialized owner state alongside its complete census.");
            Assert.That(dayOne.Int("emptySections"), Is.GreaterThan(0),
                "Exact-empty P12 sections remain part of the persisted owner vector.");

            ProcessResult dayTwo = RunHost("restore", separatedRoot, 1);
            ProcessResult dayThree = RunHost("restore", separatedRoot, 1);
            ProcessResult uninterruptedControl = RunHost("genesis", controlRoot, 3);
            foreach (ProcessResult result in new[] { dayOne, dayTwo, dayThree, uninterruptedControl })
            {
                TestContext.WriteLine(
                    "{0}: pid={1}, start={2}, world={3}, day={4}, sequence={5}, epoch={6}->{7}, sections={8}, rows={9}, exactEmpty={10}, stateSha={11}, p12Sha={12}",
                    result.Fields["mode"], result.Int("pid"), result.Fields["start"],
                    result.Fields["world"], result.Long("day"), result.Long("sequence"),
                    result.Long("epochStart"), result.Long("epoch"), result.Int("sections"),
                    result.Int("owners"), result.Int("emptySections"), result.Fields["stateSha"],
                    result.Fields["p12sha"]);
            }

            Assert.That(dayTwo.Fields["start"], Is.EqualTo("Loaded"));
            Assert.That(dayThree.Fields["start"], Is.EqualTo("Loaded"));
            Assert.That(uninterruptedControl.Fields["start"], Is.EqualTo("NoSave"));
            Assert.That(dayTwo.Long("day"), Is.EqualTo(dayOne.Long("day") + 1L));
            Assert.That(dayThree.Long("day"), Is.EqualTo(dayTwo.Long("day") + 1L));
            Assert.That(uninterruptedControl.Long("day"), Is.EqualTo(dayThree.Long("day")));
            Assert.That(dayTwo.Long("sequence"), Is.GreaterThan(dayOne.Long("sequence")));
            Assert.That(dayThree.Long("sequence"), Is.GreaterThan(dayTwo.Long("sequence")));
            Assert.That(uninterruptedControl.Long("sequence"), Is.EqualTo(dayThree.Long("sequence")));
            Assert.That(new[]
            {
                dayOne.Int("pid"), dayTwo.Int("pid"), dayThree.Int("pid"), uninterruptedControl.Int("pid")
            }.Distinct().Count(), Is.EqualTo(4),
                "Genesis, each restore, and the uninterrupted control must execute in distinct OS processes.");

            Assert.That(dayTwo.Fields["world"], Is.EqualTo(dayOne.Fields["world"]));
            Assert.That(dayThree.Fields["world"], Is.EqualTo(dayOne.Fields["world"]));
            Assert.That(uninterruptedControl.Fields["world"], Is.Not.EqualTo(dayOne.Fields["world"]));
            Assert.That(dayTwo.Long("epochStart"), Is.Not.EqualTo(dayOne.Long("epoch")),
                "A restored Core session starts with fresh process-local invalidation epoch state.");
            Assert.That(dayThree.Long("epochStart"), Is.Not.EqualTo(dayTwo.Long("epoch")),
                "The second process restart must also establish fresh invalidation epoch state.");
            Assert.That(uninterruptedControl.Long("epochStart"), Is.Not.EqualTo(dayOne.Long("epoch")),
                "The independent Genesis control begins its own process-local epoch rather than inheriting the resumed run's epoch.");
            Assert.That(dayThree.Long("epoch"), Is.GreaterThan(dayThree.Long("epochStart")),
                "The final restored process must publish committed mutations through its local epoch.");
            Assert.That(uninterruptedControl.Long("epoch"),
                Is.GreaterThan(uninterruptedControl.Long("epochStart")),
                "The uninterrupted control must publish committed mutations through its local epoch.");
            long resumedEpochAdvance =
                (dayOne.Long("epoch") - dayOne.Long("epochStart"))
                + (dayTwo.Long("epoch") - dayTwo.Long("epochStart"))
                + (dayThree.Long("epoch") - dayThree.Long("epochStart"));
            Assert.That(resumedEpochAdvance,
                Is.EqualTo(uninterruptedControl.Long("epoch") - uninterruptedControl.Long("epochStart")),
                "The sum of the three process-local committed mutation epochs must equal the uninterrupted three-day control.");
            Assert.That(dayThree.Int("sections"), Is.EqualTo(299));
            Assert.That(dayThree.Int("owners"), Is.EqualTo(uninterruptedControl.Int("owners")));
            Assert.That(dayThree.Int("emptySections"), Is.EqualTo(uninterruptedControl.Int("emptySections")));
            Assert.That(dayThree.Fields["stateSha"], Is.EqualTo(uninterruptedControl.Fields["stateSha"]),
                "The decoded authoritative P12 state must match: header semantics, all section IDs, owner keys, revisions, exact-empty sections, and row payloads.");
            Assert.That(dayThree.Fields["p12sha"], Is.Not.EqualTo(uninterruptedControl.Fields["p12sha"]),
                "The full images retain their independently Core-generated WorldIds, so their outer P12 hashes are expected to differ.");
        }
        finally
        {
            string baseDirectory = Path.GetFullPath(AppContext.BaseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            Assert.That(fullTestRoot.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase), Is.True,
                "The test may remove only its unique process-host data under the test output root.");
            if (Directory.Exists(fullTestRoot))
                Directory.Delete(fullTestRoot, recursive: true);
        }
    }

    private static ProcessResult RunHost(string mode, string storageRoot, int advanceCount)
    {
        string hostDll = Path.Combine(AppContext.BaseDirectory, "DailyV1ProcessHost", ProcessHostDll);
        Assert.That(File.Exists(hostDll), Is.True, "The fixed .NET process-host output must be copied beside CoreValidation.");
        var startInfo = new ProcessStartInfo
        {
            FileName = DotNetHostPath(),
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(hostDll);
        startInfo.ArgumentList.Add(mode);
        startInfo.ArgumentList.Add(storageRoot);
        startInfo.ArgumentList.Add(advanceCount.ToString(CultureInfo.InvariantCulture));

        using (Process process = Process.Start(startInfo))
        {
            Assert.That(process, Is.Not.Null, "The fixed .NET host process must start.");
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(120000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                Assert.Fail("The fixed .NET host process exceeded its two-minute bound.");
            }
            Task.WaitAll(outputTask, errorTask);
            string standardOutput = outputTask.Result;
            string standardError = errorTask.Result.Replace(storageRoot, "<test-storage-root>", StringComparison.OrdinalIgnoreCase);
            Assert.That(process.ExitCode, Is.Zero,
                "The fixed .NET host process must complete through Core: " + standardError + standardOutput);

            string[] resultLines = standardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith("RESULT|", StringComparison.Ordinal)).ToArray();
            Assert.That(resultLines, Has.Length.EqualTo(1), "Each process must emit one inspectable result record.");
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string field in resultLines[0].Split('|').Skip(1))
            {
                int separator = field.IndexOf('=');
                Assert.That(separator, Is.GreaterThan(0), "Result fields use the fixed key=value protocol.");
                fields.Add(field.Substring(0, separator), field.Substring(separator + 1));
            }
            return new ProcessResult(fields, standardOutput, standardError);
        }
    }

    private static string DotNetHostPath()
    {
        string configuredHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(configuredHost) && File.Exists(configuredHost))
            return configuredHost;

        string dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(dotnetRoot))
        {
            string rootedHost = Path.Combine(dotnetRoot, "dotnet.exe");
            if (File.Exists(rootedHost)) return rootedHost;
        }
        return "dotnet";
    }
}