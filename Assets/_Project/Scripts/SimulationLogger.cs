using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class SimulationLogger
{
    private const string BeginDayStepId = "logger-begin-day";
    private const string BeginDayOwnerId = "logger";
    private const string BeginDayOperationKind = "logger.begin-day";
    private const string BeginDayOperationVersion = "1";
    private readonly SimulationLogSettings settings;
    private List<string> reportLines = new List<string>();
    private Dictionary<string, LoggerBeginDayReceipt> beginDayStepReceipts =
        new Dictionary<string, LoggerBeginDayReceipt>(StringComparer.Ordinal);
    private long beginDayStepRevision;

    public string FullLog => string.Join(Environment.NewLine, reportLines);

    public SimulationLogger(SimulationLogSettings settings)
    {
        this.settings = settings ?? new SimulationLogSettings();
    }

    public void BeginSimulation(string simulationName, IEnumerable<SimulationModule> modules, int cityCount, int npcCount)
    {
        reportLines.Clear();
        AddReportLine("SIMULATION: " + (string.IsNullOrEmpty(simulationName) == true ? "Unnamed" : simulationName));
        AddReportLine(string.Empty);
        AddReportLine("Modules:");

        if (modules != null)
        {
            foreach (SimulationModule module in modules)
            {
                AddReportLine("- " + module);
            }
        }

        AddReportLine("Cities: " + Mathf.Max(0, cityCount));
        AddReportLine("NPCs: " + Mathf.Max(0, npcCount));
        AddReportLine(string.Empty);
    }

    public void BeginDay(long absoluteDay)
    {
        if (IsEnabled(SimulationLogCategory.Day) == false)
        {
            return;
        }

        AddReportLine("====================");
        AddReportLine("DIA " + absoluteDay);
        AddReportLine("====================");
        AddReportLine(string.Empty);
    }

    public void AddReportLine(string message)
    {
        reportLines.Add(message ?? string.Empty);
        beginDayStepRevision++;
    }

    /// <summary>Captures the logger-owned daily heading, including a frozen no-output disposition.</summary>
    public bool TryCreateBeginDayStep(
        DailyBoundaryOperation operation,
        int ordinal,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        step = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || ordinal < 0 || beginDayStepRevision == long.MaxValue)
        {
            return false;
        }

        bool outputEnabled = settings.showDay;
        step = new BoundaryContinuationStep(
            ordinal,
            BeginDayStepId,
            BeginDayOwnerId,
            BeginDayOperationKind,
            BeginDayOperationVersion,
            beginDayStepRevision.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            disposition: outputEnabled ? "included" : "no-output");
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Returns the committed occurrence receipt without consulting current logger settings.</summary>
    public bool TryResolveBeginDayReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out LoggerBeginDayReceipt receipt,
        out TimelineFailure failure)
    {
        receipt = null;
        if (!TryGetBeginDayIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (!beginDayStepReceipts.TryGetValue(identity, out LoggerBeginDayReceipt existing))
        {
            failure = TimelineFailure.None;
            return false;
        }

        if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        receipt = existing;
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Prepares the exact daily heading and its occurrence receipt as one logger-owned commit.</summary>
    public bool TryPrepareBeginDayStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryGetBeginDayIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (beginDayStepReceipts.TryGetValue(identity, out LoggerBeginDayReceipt existing))
        {
            if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            prepared = new LoggerBeginDayCommit(this, existing, true, fingerprint,
                beginDayStepRevision, reportLines, beginDayStepReceipts, beginDayStepReceipts);
            failure = TimelineFailure.None;
            return true;
        }

        if (beginDayStepRevision == long.MaxValue
            || !long.TryParse(step.OwnerRevision, NumberStyles.None, CultureInfo.InvariantCulture, out long expectedRevision)
            || expectedRevision != beginDayStepRevision)
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        List<string> output = step.Disposition == "included"
            ? new List<string> { "====================", "DIA " + manifest.AbsoluteDay, "====================", string.Empty }
            : new List<string>();
        LoggerBeginDayReceipt receipt = new LoggerBeginDayReceipt(
            identity,
            fingerprint,
            beginDayStepRevision,
            beginDayStepRevision + 1L,
            step.Disposition);
        Dictionary<string, LoggerBeginDayReceipt> nextReceipts =
            new Dictionary<string, LoggerBeginDayReceipt>(beginDayStepReceipts, StringComparer.Ordinal)
            {
                [identity] = receipt
            };
        prepared = new LoggerBeginDayCommit(this, receipt, false, fingerprint,
            beginDayStepRevision, reportLines, beginDayStepReceipts, nextReceipts, output);
        failure = TimelineFailure.None;
        return true;
    }

    private bool TryGetBeginDayIdentity(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out string identity,
        out string fingerprint)
    {
        identity = null;
        fingerprint = null;
        if (manifest == null || step == null || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || step.StepId != BeginDayStepId || step.OwnerId != BeginDayOwnerId
            || step.OperationKind != BeginDayOperationKind || step.OperationVersion != BeginDayOperationVersion
            || (step.Disposition != "included" && step.Disposition != "no-output"))
        {
            return false;
        }

        string expectedOccurrence = SpatialStableKey.Encode(
            manifest.WorldId, manifest.ProfileId, manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture));
        if (!string.Equals(expectedOccurrence, manifest.BoundaryOccurrenceId, StringComparison.Ordinal))
        {
            return false;
        }

        identity = SpatialStableKey.Encode(manifest.BoundaryOccurrenceId, step.StepId);
        fingerprint = SpatialStableKey.Encode(
            manifest.BoundaryOccurrenceId,
            manifest.ContinuationId,
            manifest.SubphaseKind,
            manifest.SubphaseVersion,
            manifest.ConfigurationIdentity,
            manifest.ContentIdentity,
            step.Ordinal.ToString(CultureInfo.InvariantCulture),
            step.StepId,
            step.OwnerId,
            step.OperationKind,
            step.OperationVersion,
            step.OwnerRevision,
            step.Payload,
            step.PersonId,
            step.Disposition);
        return true;
    }

    internal bool TryCommitBeginDayStep(
        LoggerBeginDayReceipt receipt,
        bool replay,
        string fingerprint,
        long expectedRevision,
        List<string> expectedLines,
        Dictionary<string, LoggerBeginDayReceipt> expectedReceipts,
        Dictionary<string, LoggerBeginDayReceipt> nextReceipts,
        IReadOnlyList<string> output,
        out TimelineFailure failure)
    {
        if (replay)
        {
            if (beginDayStepReceipts.TryGetValue(receipt.ExecutionStepIdentity, out LoggerBeginDayReceipt current)
                && ReferenceEquals(current, receipt)
                && string.Equals(current.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.None;
                return true;
            }

            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (receipt == null || beginDayStepRevision != expectedRevision
            || !ReferenceEquals(reportLines, expectedLines)
            || !ReferenceEquals(beginDayStepReceipts, expectedReceipts)
            || nextReceipts == null || !nextReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || beginDayStepReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || receipt.OwnerRevisionBefore != beginDayStepRevision
            || receipt.OwnerRevisionAfter != beginDayStepRevision + 1L
            || !string.Equals(receipt.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        List<string> nextLines = new List<string>(reportLines.Count + (output?.Count ?? 0));
        nextLines.AddRange(reportLines);
        if (output != null)
        {
            nextLines.AddRange(output);
        }

        // All allocating work is complete; publish the output, receipt, and revision together
        // within the logger's serialized owner boundary.
        reportLines = nextLines;
        beginDayStepReceipts = nextReceipts;
        beginDayStepRevision = receipt.OwnerRevisionAfter;
        failure = TimelineFailure.None;
        return true;
    }

    public void Log(SimulationLogCategory category, string message)
    {
        if (string.IsNullOrEmpty(message) == true || IsEnabled(category) == false)
        {
            return;
        }

        AddReportLine(message);
        Debug.Log(message);
    }

    public void LogWarning(string message)
    {
        if (string.IsNullOrEmpty(message) == false)
        {
            AddReportLine("[WARNING] " + message);
            Debug.LogWarning(message);
        }
    }

    public void LogError(string message)
    {
        if (string.IsNullOrEmpty(message) == false)
        {
            AddReportLine("[ERROR] " + message);
            Debug.LogError(message);
        }
    }

    public string SaveToFile(string fileName)
    {
        string safeFileName = SanitizeFileName(fileName);

        if (string.IsNullOrEmpty(safeFileName) == true)
        {
            safeFileName = "Simulation-Run.txt";
        }

        if (safeFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) == false)
        {
            safeFileName += ".txt";
        }

        string directoryPath = Path.Combine(Application.persistentDataPath, "SimulationLogs");
        string filePath = Path.Combine(directoryPath, safeFileName);

        try
        {
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, FullLog);
            Debug.Log("Diario salvo em: " + filePath);
            return filePath;
        }
        catch (Exception exception)
        {
            LogError("Nao foi possivel salvar o diario em " + filePath + ": " + exception.Message);
            return string.Empty;
        }
    }

    private string SanitizeFileName(string fileName)
    {
        string value = string.IsNullOrEmpty(fileName) == true ? "Simulation-Run.txt" : fileName;
        char[] invalidCharacters = Path.GetInvalidFileNameChars();

        foreach (char invalidCharacter in invalidCharacters)
        {
            value = value.Replace(invalidCharacter, '-');
        }

        return value.Trim();
    }

    public bool IsEnabled(SimulationLogCategory category)
    {
        switch (category)
        {
            case SimulationLogCategory.Day:
                return settings.showDay;
            case SimulationLogCategory.NpcAction:
                return settings.npcActions;
            case SimulationLogCategory.EconomyProduction:
                return settings.economyProduction;
            case SimulationLogCategory.EconomyConsumption:
                return settings.economyConsumption;
            case SimulationLogCategory.Market:
                return settings.market;
            case SimulationLogCategory.Trade:
                return settings.trade;
            case SimulationLogCategory.Travel:
                return settings.travel;
            case SimulationLogCategory.Crime:
                return settings.crime;
            case SimulationLogCategory.Justice:
                return settings.justice;
            default:
                return true;
        }
    }
}

public sealed class LoggerBeginDayReceipt
{
    public string ExecutionStepIdentity { get; }
    public string DescriptorFingerprint { get; }
    public long OwnerRevisionBefore { get; }
    public long OwnerRevisionAfter { get; }
    public string Disposition { get; }

    internal LoggerBeginDayReceipt(
        string executionStepIdentity,
        string descriptorFingerprint,
        long ownerRevisionBefore,
        long ownerRevisionAfter,
        string disposition)
    {
        ExecutionStepIdentity = executionStepIdentity ?? throw new ArgumentNullException(nameof(executionStepIdentity));
        DescriptorFingerprint = descriptorFingerprint ?? throw new ArgumentNullException(nameof(descriptorFingerprint));
        OwnerRevisionBefore = ownerRevisionBefore;
        OwnerRevisionAfter = ownerRevisionAfter;
        Disposition = disposition ?? throw new ArgumentNullException(nameof(disposition));
    }
}

internal sealed class LoggerBeginDayCommit : IBoundaryContinuationStepCommit
{
    private readonly SimulationLogger owner;
    private readonly LoggerBeginDayReceipt receipt;
    private readonly bool replay;
    private readonly string fingerprint;
    private readonly long expectedRevision;
    private readonly List<string> expectedLines;
    private readonly Dictionary<string, LoggerBeginDayReceipt> expectedReceipts;
    private readonly Dictionary<string, LoggerBeginDayReceipt> nextReceipts;
    private readonly IReadOnlyList<string> output;
    private bool completed;

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();

    public LoggerBeginDayCommit(
        SimulationLogger owner,
        LoggerBeginDayReceipt receipt,
        bool replay,
        string fingerprint,
        long expectedRevision,
        List<string> expectedLines,
        Dictionary<string, LoggerBeginDayReceipt> expectedReceipts,
        Dictionary<string, LoggerBeginDayReceipt> nextReceipts,
        IReadOnlyList<string> output = null)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        this.replay = replay;
        this.fingerprint = fingerprint ?? string.Empty;
        this.expectedRevision = expectedRevision;
        this.expectedLines = expectedLines;
        this.expectedReceipts = expectedReceipts;
        this.nextReceipts = nextReceipts;
        this.output = output ?? Array.Empty<string>();
    }

    public bool TryCommit(out TimelineFailure failure)
    {
        if (completed)
        {
            failure = TimelineFailure.None;
            return true;
        }

        completed = owner.TryCommitBeginDayStep(
            receipt,
            replay,
            fingerprint,
            expectedRevision,
            expectedLines,
            expectedReceipts,
            nextReceipts,
            output,
            out failure);
        return completed;
    }
}

[Serializable]
public class SimulationLogSettings
{
    public bool showDay = true;
    public bool npcActions = true;
    public bool trade = true;
    public bool travel = true;
    public bool crime = true;
    public bool justice = true;
    public bool economyProduction = true;
    public bool economyConsumption = true;
    public bool market = true;
}

public enum SimulationLogCategory
{
    Day,
    NpcAction,
    EconomyProduction,
    EconomyConsumption,
    Market,
    Trade,
    Travel,
    Crime,
    Justice
}
