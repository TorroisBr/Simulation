using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Simulation/Simulation Config")]
public class SimulationConfigData : ScriptableObject
{
    public string simulationName;
    public List<SimulationModule> enabledModules = new List<SimulationModule>();
    public List<CityData> cities = new List<CityData>();
    public List<NpcSimulationConfig> npcs = new List<NpcSimulationConfig>();
    public List<NpcActionData> actions = new List<NpcActionData>();
    public List<NpcStatusData> statuses = new List<NpcStatusData>();
    public List<NpcJobData> jobs = new List<NpcJobData>();

    [Header("Guard Crime")]
    public NpcStatusData wantedStatus;
    public NpcStatusData arrestedStatus;

    public List<SimulationModule> EnabledModules => enabledModules ?? (enabledModules = new List<SimulationModule>());
    public List<CityData> Cities => cities ?? (cities = new List<CityData>());
    public List<NpcSimulationConfig> Npcs => npcs ?? (npcs = new List<NpcSimulationConfig>());
    public List<NpcActionData> Actions => actions ?? (actions = new List<NpcActionData>());
    public List<NpcStatusData> Statuses => statuses ?? (statuses = new List<NpcStatusData>());
    public List<NpcJobData> Jobs => jobs ?? (jobs = new List<NpcJobData>());

    public bool HasModule(SimulationModule module)
    {
        return EnabledModules.Contains(module);
    }
}

public enum SimulationModule
{
    Economy,
    Merchant,
    GuardCrime
}

[Serializable]
public class NpcSimulationConfig
{
    public NpcData npc;
    public CityData startingCity;
    public float initialMoney = 100f;
    public List<NpcInitialInventoryItemConfig> initialInventory = new List<NpcInitialInventoryItemConfig>();

    public List<NpcInitialInventoryItemConfig> InitialInventory => initialInventory ?? (initialInventory = new List<NpcInitialInventoryItemConfig>());
}

[Serializable]
public class NpcInitialInventoryItemConfig
{
    public ItemData item;
    public int amount;
    public float averageUnitCost;
}
