using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimulationLogger
{
    private readonly SimulationLogSettings settings;
    private readonly List<string> reportLines = new List<string>();

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
